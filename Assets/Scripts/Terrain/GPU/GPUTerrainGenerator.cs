using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using Vastcore.Utilities;
using Vastcore.Core;

namespace Vastcore.Generation.GPU
{
    /// <summary>
    /// GPU並列処理による高速地形生成システム
    /// ComputeShaderを使用してGPU上で地形生成を実行
    /// </summary>
    public class GPUTerrainGenerator : MonoBehaviour
    {
        [Header("GPU設定")]
        [SerializeField] private ComputeShader terrainComputeShader;
        [SerializeField] private bool useGPUGeneration = true;
        [SerializeField] private int textureResolution = 512;
        
        [Header("地形パラメータ")]
        [SerializeField] private Vector4 noiseParams = new Vector4(1f, 4f, 0.5f, 2f); // scale, octaves, persistence, lacunarity
        [SerializeField] private Vector4 erosionParams = new Vector4(10f, 0.1f, 0.3f, 4f); // iterations, evaporation, deposition, capacity
        [SerializeField] private Vector4 terrainParams = new Vector4(512f, 512f, 100f, 1f); // width, height, amplitude, frequency
        
        [Header("パフォーマンス")]
        [SerializeField] private bool enableAsyncGeneration = true;
        [SerializeField] private int maxConcurrentGenerations = 4;
        
        // GPU リソース
        private RenderTexture heightmapTexture;
        private RenderTexture erosionTexture;
        private RenderTexture noiseTexture;
        private ComputeBuffer resultBuffer;
        
        // 非同期処理管理
        private Queue<TerrainGenerationRequest> generationQueue;
        private List<TerrainGenerationRequest> activeGenerations;
        private int currentSeed;
        
        // Compute Shader カーネル
        private int generateHeightmapKernel;
        private int applyErosionKernel;
        private int generateNoiseKernel;
        
        public struct TerrainGenerationRequest
        {
            public Vector2Int coordinate;
            public TerrainGenerationParams parameters;
            public System.Action<float[,]> onComplete;
            public bool isProcessing;
            public AsyncGPUReadbackRequest readbackRequest;
        }
        
        [System.Serializable]
        public struct TerrainGenerationParams
        {
            public float scale;
            public int octaves;
            public float persistence;
            public float lacunarity;
            public float amplitude;
            public float frequency;
            public bool applyErosion;
            public int erosionIterations;
        }
        
        private void Awake()
        {
            InitializeGPUResources();
            generationQueue = new Queue<TerrainGenerationRequest>();
            activeGenerations = new List<TerrainGenerationRequest>();
        }
        
        private void InitializeGPUResources()
        {
            if (!SystemInfo.supportsComputeShaders)
            {
                Debug.LogWarning("ComputeShaders not supported. Falling back to CPU generation.");
                useGPUGeneration = false;
                return;
            }
            
            if (terrainComputeShader == null)
            {
                Debug.LogError("Terrain ComputeShader not assigned!");
                useGPUGeneration = false;
                return;
            }
            
            // カーネルの取得
            generateHeightmapKernel = terrainComputeShader.FindKernel("GenerateHeightmap");
            applyErosionKernel = terrainComputeShader.FindKernel("ApplyErosion");
            generateNoiseKernel = terrainComputeShader.FindKernel("GenerateNoise");
            
            // RenderTextureの作成
            CreateRenderTextures();
            
            Debug.Log("GPU Terrain Generator initialized successfully");
        }
        
        private void CreateRenderTextures()
        {
            // Heightmap texture
            heightmapTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.RFloat);
            heightmapTexture.enableRandomWrite = true;
            heightmapTexture.Create();
            
            // Erosion texture
            erosionTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.RFloat);
            erosionTexture.enableRandomWrite = true;
            erosionTexture.Create();
            
            // Noise texture
            noiseTexture = new RenderTexture(textureResolution, textureResolution, 0, RenderTextureFormat.RFloat);
            noiseTexture.enableRandomWrite = true;
            noiseTexture.Create();
        }
        
        /// <summary>
        /// GPU並列処理による地形生成をリクエスト
        /// </summary>
        public void RequestTerrainGeneration(Vector2Int coordinate, TerrainGenerationParams parameters, System.Action<float[,]> onComplete)
        {
            VastcoreLogger.Instance.LogInfo("GPUTerrain", $"Request start coord={coordinate} gpu={(useGPUGeneration ? 1:0)} async={(enableAsyncGeneration ? 1:0)}");
            if (!useGPUGeneration)
            {
                // CPUフォールバック
                VastcoreLogger.Instance.LogInfo("GPUTerrain", $"GPU disabled -> CPU fallback coord={coordinate}");
                StartCoroutine(GenerateTerrainCPU(coordinate, parameters, onComplete));
                return;
            }
            
            var request = new TerrainGenerationRequest
            {
                coordinate = coordinate,
                parameters = parameters,
                onComplete = onComplete,
                isProcessing = false
            };
            
            generationQueue.Enqueue(request);
            VastcoreLogger.Instance.LogDebug("GPUTerrain", $"Enqueued coord={coordinate} q={generationQueue.Count} act={activeGenerations.Count}");
            
            if (enableAsyncGeneration)
            {
                StartCoroutine(ProcessGenerationQueue());
            }
        }
        
        private IEnumerator ProcessGenerationQueue()
        {
            VastcoreLogger.Instance.LogDebug("GPUTerrain", $"ProcessQueue start q={generationQueue.Count} act={activeGenerations.Count}");
            while (generationQueue.Count > 0 && activeGenerations.Count < maxConcurrentGenerations)
            {
                var request = generationQueue.Dequeue();
                activeGenerations.Add(request);
                VastcoreLogger.Instance.LogDebug("GPUTerrain", $"Dequeued coord={request.coordinate} act={activeGenerations.Count} remain={generationQueue.Count}");
                
                StartCoroutine(GenerateTerrainGPU(request));
                
                yield return null; // フレーム分散
            }
            VastcoreLogger.Instance.LogDebug("GPUTerrain", $"ProcessQueue end q={generationQueue.Count} act={activeGenerations.Count}");
        }
        
        private IEnumerator GenerateTerrainGPU(TerrainGenerationRequest request)
        {
            request.isProcessing = true;
            VastcoreLogger.Instance.LogDebug("GPUTerrain", $"GenerateGPU start coord={request.coordinate}");
            
            // シード値の設定
            currentSeed = GetSeedFromCoordinate(request.coordinate);
            
            // ComputeShaderパラメータの設定
            SetComputeShaderParameters(request.parameters);
            
            // ノイズ生成
            terrainComputeShader.SetTexture(generateNoiseKernel, "NoiseResult", noiseTexture);
            terrainComputeShader.Dispatch(generateNoiseKernel, textureResolution / 8, textureResolution / 8, 1);
            VastcoreLogger.Instance.LogDebug("GPUTerrain", $"Noise dispatched coord={request.coordinate}");
            
            yield return null;
            
            // 基本地形生成
            terrainComputeShader.SetTexture(generateHeightmapKernel, "HeightmapResult", heightmapTexture);
            terrainComputeShader.Dispatch(generateHeightmapKernel, textureResolution / 8, textureResolution / 8, 1);
            VastcoreLogger.Instance.LogDebug("GPUTerrain", $"Heightmap dispatched coord={request.coordinate}");
            
            yield return null;
            
            // 浸食処理（オプション）
            if (request.parameters.applyErosion)
            {
                VastcoreLogger.Instance.LogDebug("GPUTerrain", $"Erosion begin iter={request.parameters.erosionIterations} coord={request.coordinate}");
                for (int i = 0; i < request.parameters.erosionIterations; i++)
                {
                    terrainComputeShader.SetTexture(applyErosionKernel, "HeightmapResult", heightmapTexture);
                    terrainComputeShader.SetTexture(applyErosionKernel, "ErosionResult", erosionTexture);
                    terrainComputeShader.Dispatch(applyErosionKernel, textureResolution / 8, textureResolution / 8, 1);
                    
                    // 結果をコピー
                    Graphics.CopyTexture(erosionTexture, heightmapTexture);
                    
                    if (i % 2 == 0) yield return null; // 負荷分散
                }
                VastcoreLogger.Instance.LogDebug("GPUTerrain", $"Erosion end coord={request.coordinate}");
            }
            
            // GPU→CPUデータ転送
            request.readbackRequest = AsyncGPUReadback.Request(heightmapTexture, 0, TextureFormat.RFloat);
            VastcoreLogger.Instance.LogDebug("GPUTerrain", $"Readback requested coord={request.coordinate}");
            
            yield return new WaitUntil(() => request.readbackRequest.done);
            
            if (!request.readbackRequest.hasError)
            {
                var data = request.readbackRequest.GetData<float>();
                var heightmap = ConvertToHeightmap(data, textureResolution);
                request.onComplete?.Invoke(heightmap);
                VastcoreLogger.Instance.LogInfo("GPUTerrain", $"Readback ok coord={request.coordinate}");
            }
            else
            {
                Debug.LogError("GPU Readback failed for terrain generation");
                // CPUフォールバックを実行
                VastcoreLogger.Instance.LogError("GPUTerrain", $"Readback error -> CPU fallback coord={request.coordinate}");
                StartCoroutine(GenerateTerrainCPU(request.coordinate, request.parameters, request.onComplete));
            }
            
            // アクティブリストから削除
            activeGenerations.Remove(request);
            VastcoreLogger.Instance.LogDebug("GPUTerrain", $"GenerateGPU end coord={request.coordinate} act={activeGenerations.Count}");
        }
        
        private void SetComputeShaderParameters(TerrainGenerationParams parameters)
        {
            terrainComputeShader.SetVector("NoiseParams", new Vector4(
                parameters.scale,
                parameters.octaves,
                parameters.persistence,
                parameters.lacunarity
            ));
            
            terrainComputeShader.SetVector("ErosionParams", erosionParams);
            
            terrainComputeShader.SetVector("TerrainParams", new Vector4(
                textureResolution,
                textureResolution,
                parameters.amplitude,
                parameters.frequency
            ));
            
            terrainComputeShader.SetFloat("Time", Time.time);
            terrainComputeShader.SetInt("Seed", currentSeed);
        }
        
        private int GetSeedFromCoordinate(Vector2Int coordinate)
        {
            // 座標からシード値を生成
            return coordinate.x * 73856093 ^ coordinate.y * 19349663;
        }
        
        private float[,] ConvertToHeightmap(Unity.Collections.NativeArray<float> data, int resolution)
        {
            var heightmap = new float[resolution, resolution];
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    heightmap[x, y] = data[y * resolution + x];
                }
            }
            
            return heightmap;
        }
        
        private IEnumerator GenerateTerrainCPU(Vector2Int coordinate, TerrainGenerationParams parameters, System.Action<float[,]> onComplete)
        {
            // CPUフォールバック実装
            VastcoreLogger.Instance.LogInfo("GPUTerrain", $"CPU start coord={coordinate}");
            var heightmap = new float[textureResolution, textureResolution];
            
            for (int y = 0; y < textureResolution; y++)
            {
                for (int x = 0; x < textureResolution; x++)
                {
                    float nx = (float)x / textureResolution;
                    float ny = (float)y / textureResolution;
                    
                    float height = GenerateNoiseValue(nx, ny, parameters);
                    heightmap[x, y] = height;
                }
                
                if (y % 32 == 0) yield return null; // 負荷分散
            }
            
            onComplete?.Invoke(heightmap);
            VastcoreLogger.Instance.LogInfo("GPUTerrain", $"CPU done coord={coordinate}");
        }
        
        private float GenerateNoiseValue(float x, float y, TerrainGenerationParams parameters)
        {
            float value = 0f;
            float amplitude = 1f;
            float frequency = parameters.frequency;
            
            for (int i = 0; i < parameters.octaves; i++)
            {
                value += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
                amplitude *= parameters.persistence;
                frequency *= parameters.lacunarity;
            }
            
            return value * parameters.amplitude;
        }
        
        /// <summary>
        /// GPU使用状況の取得
        /// </summary>
        public GPUPerformanceInfo GetPerformanceInfo()
        {
            return new GPUPerformanceInfo
            {
                isGPUEnabled = useGPUGeneration,
                activeGenerations = activeGenerations.Count,
                queuedGenerations = generationQueue.Count,
                maxConcurrentGenerations = maxConcurrentGenerations,
                textureResolution = textureResolution
            };
        }
        
        public struct GPUPerformanceInfo
        {
            public bool isGPUEnabled;
            public int activeGenerations;
            public int queuedGenerations;
            public int maxConcurrentGenerations;
            public int textureResolution;
        }
        
        private void OnDestroy()
        {
            // GPU リソースの解放
            if (heightmapTexture != null) heightmapTexture.Release();
            if (erosionTexture != null) erosionTexture.Release();
            if (noiseTexture != null) noiseTexture.Release();
            if (resultBuffer != null) resultBuffer.Release();
        }
        
        private void Update()
        {
            // 非同期生成キューの処理
            if (enableAsyncGeneration && generationQueue.Count > 0 && activeGenerations.Count < maxConcurrentGenerations)
            {
                VastcoreLogger.Instance.LogDebug("GPUTerrain", $"Update tick trigger q={generationQueue.Count} act={activeGenerations.Count}");
                StartCoroutine(ProcessGenerationQueue());
            }
        }
    }
}