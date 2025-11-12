using UnityEngine;
using System.Collections.Generic;
using System.IO;

using Vastcore.Utils;
using Vastcore.Generation.GPU;

namespace Vastcore.Generation.Cache
{
    /// <summary>
    /// 蝨ｰ蠖｢繧ｭ繝｣繝・す繝･繝槭ロ繝ｼ繧ｸ繝｣繝ｼ
    /// IntelligentCacheSystem縺ｨRuntimeTerrainManager縺ｮ邨ｱ蜷・
    /// </summary>
    public class TerrainCacheManager : MonoBehaviour
    {
        [Header("邨ｱ蜷郁ｨｭ螳・)]
        [SerializeField] private bool enableIntegratedCaching = true;
        [SerializeField] private float cacheCheckInterval = 2f;
        [SerializeField] private int maxSimultaneousLoads = 3;
        [SerializeField] private float maxMemoryCacheSize = 512f; // MB
        
        [Header("繧ｭ繝｣繝・す繝･蜆ｪ蜈亥ｺｦ")]
        [SerializeField] private float recentAccessWeight = 2f;
        [SerializeField] private float distanceWeight = 1f;
        [SerializeField] private float frequencyWeight = 1.5f;
        
        // 邨ｱ蜷医さ繝ｳ繝昴・繝阪Φ繝・
        private IntelligentCacheSystem cacheSystem;
        private RuntimeTerrainManager terrainManager;
        private GPUTerrainGenerator gpuGenerator;
        
        // 蜃ｦ逅・ｮ｡逅・
        private Dictionary<Vector2Int, TerrainLoadRequest> activeLoadRequests;
        private Queue<TerrainLoadRequest> loadQueue;
        private float lastCacheCheck;
        
        public struct TerrainLoadRequest
        {
            public Vector2Int coordinate;
            public float priority;
            public System.Action<TerrainTile> onComplete;
            public bool isFromCache;
            public float requestTime;
        }
        
        public struct TerrainTile
        {
            public Vector2Int coordinate;
            public GameObject terrainObject;
            public Mesh terrainMesh;
            public float[,] heightData;
            public List<GameObject> structures;
            public bool isFromCache;
        }
        
        private void Awake()
        {
            InitializeComponents();
            activeLoadRequests = new Dictionary<Vector2Int, TerrainLoadRequest>();
            loadQueue = new Queue<TerrainLoadRequest>();
        }
        
        private void InitializeComponents()
        {
            cacheSystem = GetComponent<IntelligentCacheSystem>();
            if (cacheSystem == null)
            {
                cacheSystem = gameObject.AddComponent<IntelligentCacheSystem>();
            }
            
            terrainManager = FindFirstObjectByType<RuntimeTerrainManager>();
            if (terrainManager == null)
            {
                Debug.LogWarning("RuntimeTerrainManager not found. Cache integration limited.");
            }
            
            gpuGenerator = FindFirstObjectByType<GPUTerrainGenerator>();
            if (gpuGenerator == null)
            {
                Debug.LogWarning("GPUTerrainGenerator not found. Using CPU fallback.");
            }
        }
        
        /// <summary>
        /// 蝨ｰ蠖｢繧ｿ繧､繝ｫ縺ｮ隕∵ｱゑｼ医く繝｣繝・す繝･邨ｱ蜷茨ｼ・
        /// </summary>
        public void RequestTerrainTile(Vector2Int coordinate, System.Action<TerrainTile> onComplete, float priority = 1f)
        {
            VastcoreLogger.Instance.LogInfo("TerrainCache", $"RequestTerrainTile start coord={coordinate} priority={priority}");
            if (!enableIntegratedCaching)
            {
                // 繧ｭ繝｣繝・す繝･縺ｪ縺励〒逶ｴ謗･逕滓・
                RequestDirectGeneration(coordinate, onComplete);
                return;
            }
            
            // 繧ｭ繝｣繝・す繝･縺九ｉ讀懃ｴ｢
            if (cacheSystem.TryGetCachedTerrainData(coordinate, out var cachedData))
            {
                // 繧ｭ繝｣繝・す繝･繝偵ャ繝・
                VastcoreLogger.Instance.LogInfo("TerrainCache", $"Cache hit coord={coordinate}");
                StartCoroutine(LoadFromCacheAsync(coordinate, cachedData, onComplete));
                return;
            }
            
            var request = new TerrainLoadRequest
            {
                coordinate = coordinate,
                priority = priority,
                onComplete = onComplete,
                isFromCache = false,
                requestTime = Time.time
            };
            
            // 蜆ｪ蜈亥ｺｦ險育ｮ暦ｼ・ecentAccessWeight, distanceWeight, frequencyWeight繧剃ｽｿ逕ｨ・・
            if (terrainManager != null)
            {
                Vector3 playerPos = terrainManager.playerTransform != null ? terrainManager.playerTransform.position : Vector3.zero;
                Vector3 tilePos = new Vector3(coordinate.x * 2000f, 0, coordinate.y * 2000f);
                float distance = Vector3.Distance(playerPos, tilePos);
                float requestTime = Time.time; // 迴ｾ蝨ｨ縺ｮ譎る俣繧貞叙蠕・
                float timeSinceRequest = Time.time - requestTime;
                
                request.priority = (recentAccessWeight * (1f / (1f + timeSinceRequest))) + 
                                 (distanceWeight * (1f / (1f + distance / 1000f))) + 
                                 (frequencyWeight * priority);
            }
            
            if (!activeLoadRequests.ContainsKey(coordinate))
            {
                activeLoadRequests[coordinate] = request;
                loadQueue.Enqueue(request);
                VastcoreLogger.Instance.LogInfo("TerrainCache", $"Cache miss -> enqueued coord={coordinate} queue={loadQueue.Count}");
            }
        }
        
        private System.Collections.IEnumerator LoadFromCacheAsync(Vector2Int coordinate, IntelligentCacheSystem.CachedTerrainData cachedData, System.Action<TerrainTile> onComplete)
        {
            yield return null; // 繝輔Ξ繝ｼ繝蛻・淵
            
            try
            {
                VastcoreLogger.Instance.LogDebug("TerrainCache", $"LoadFromCacheAsync start coord={coordinate}");
                // 繧ｭ繝｣繝・す繝･繝・・繧ｿ縺九ｉTerrainTile繧呈ｧ狗ｯ・
                var terrainTile = new TerrainTile
                {
                    coordinate = coordinate,
                    heightData = cachedData.heightmap,
                    isFromCache = true
                };
                
                // 繝｡繝・す繝･逕滓・
                terrainTile.terrainMesh = GenerateMeshFromHeightmap(cachedData.heightmap);
                
                // GameObject縺ｮ菴懈・
                terrainTile.terrainObject = CreateTerrainGameObject(coordinate, terrainTile.terrainMesh);
                
                // 繝励Μ繝溘ユ繧｣繝悶が繝悶ず繧ｧ繧ｯ繝医・蠕ｩ蜈・
                terrainTile.structures = RestorePrimitiveObjects(cachedData.primitiveObjects, terrainTile.terrainObject.transform);
                
                VastcoreLogger.Instance.LogInfo("TerrainCache", $"LoadFromCacheAsync complete coord={coordinate}");
                onComplete?.Invoke(terrainTile);
                
                Debug.Log($"Loaded terrain tile {coordinate} from cache");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load terrain tile {coordinate} from cache: {e.Message}");
                VastcoreLogger.Instance.LogError("TerrainCache", $"LoadFromCacheAsync error coord={coordinate}: {e.Message}", e);
                
                // 繝輔か繝ｼ繝ｫ繝舌ャ繧ｯ: 譁ｰ隕冗函謌・
                RequestDirectGeneration(coordinate, onComplete);
            }
        }
        
        private void RequestDirectGeneration(Vector2Int coordinate, System.Action<TerrainTile> onComplete)
        {
            if (gpuGenerator != null)
            {
                // GPU逕滓・
                VastcoreLogger.Instance.LogInfo("TerrainCache", $"RequestDirectGeneration GPU path coord={coordinate}");
                var gpuParams = new GPUTerrainGenerator.TerrainGenerationParams
                {
                    scale = 1f,
                    octaves = 4,
                    persistence = 0.5f,
                    lacunarity = 2f,
                    amplitude = 100f,
                    frequency = 0.01f,
                    applyErosion = true,
                    erosionIterations = 5
                };
                
                gpuGenerator.RequestTerrainGeneration(coordinate, gpuParams, (heightmap) =>
                {
                    StartCoroutine(ProcessGeneratedTerrain(coordinate, heightmap, gpuParams, onComplete));
                });
            }
            else
            {
                // CPU 繝輔か繝ｼ繝ｫ繝舌ャ繧ｯ
                VastcoreLogger.Instance.LogInfo("TerrainCache", $"RequestDirectGeneration CPU fallback coord={coordinate}");
                StartCoroutine(GenerateTerrainCPU(coordinate, onComplete));
            }
        }
        
        private System.Collections.IEnumerator ProcessGeneratedTerrain(Vector2Int coordinate, float[,] heightmap, GPUTerrainGenerator.TerrainGenerationParams gpuParams, System.Action<TerrainTile> onComplete)
        {
            VastcoreLogger.Instance.LogDebug("TerrainCache", $"ProcessGeneratedTerrain start coord={coordinate}");
            yield return null;
            
            // TerrainTile縺ｮ讒狗ｯ・
            var terrainTile = new TerrainTile
            {
                coordinate = coordinate,
                heightData = heightmap,
                isFromCache = false
            };
            
            // 繝｡繝・す繝･逕滓・
            terrainTile.terrainMesh = GenerateMeshFromHeightmap(heightmap);
            
            // GameObject縺ｮ菴懈・
            terrainTile.terrainObject = CreateTerrainGameObject(coordinate, terrainTile.terrainMesh);
            
            // 繝励Μ繝溘ユ繧｣繝悶が繝悶ず繧ｧ繧ｯ繝医・逕滓・
            terrainTile.structures = GeneratePrimitiveObjects(coordinate, terrainTile.terrainObject.transform);
            
            // 繧ｭ繝｣繝・す繝･縺ｫ菫晏ｭ・
            var metadata = new IntelligentCacheSystem.TerrainMetadata
            {
                generationTime = Time.time,
                seed = GetSeedFromCoordinate(coordinate),
                biomeType = "default",
                noiseParams = new Vector4(gpuParams.scale, gpuParams.octaves, gpuParams.persistence, gpuParams.lacunarity),
                hasErosion = gpuParams.applyErosion
            };
            
            var primitiveData = ConvertToPrimitiveData(terrainTile.structures);
            cacheSystem.CacheTerrainData(coordinate, heightmap, metadata, primitiveData);
            
            VastcoreLogger.Instance.LogInfo("TerrainCache", $"ProcessGeneratedTerrain complete coord={coordinate} cached=true");
            onComplete?.Invoke(terrainTile);
            
            // 繧｢繧ｯ繝・ぅ繝悶Μ繧ｯ繧ｨ繧ｹ繝医°繧牙炎髯､
            activeLoadRequests.Remove(coordinate);
        }
        
        private System.Collections.IEnumerator GenerateTerrainCPU(Vector2Int coordinate, System.Action<TerrainTile> onComplete)
        {
            // CPU 繝輔か繝ｼ繝ｫ繝舌ャ繧ｯ螳溯｣・
            const int resolution = 256;
            var heightmap = new float[resolution, resolution];
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float nx = (float)x / resolution + coordinate.x;
                    float ny = (float)y / resolution + coordinate.y;
                    
                    heightmap[x, y] = Mathf.PerlinNoise(nx * 0.1f, ny * 0.1f) * 100f;
                }
                
                if (y % 16 == 0) yield return null; // 雋闕ｷ蛻・淵
            }
            
            VastcoreLogger.Instance.LogInfo("TerrainCache", $"GenerateTerrainCPU complete coord={coordinate}");
            yield return StartCoroutine(ProcessGeneratedTerrain(coordinate, heightmap, default, onComplete));
        }
        
        private Mesh GenerateMeshFromHeightmap(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            var vertices = new Vector3[width * height];
            var triangles = new int[(width - 1) * (height - 1) * 6];
            var uvs = new Vector2[width * height];
            
            // 鬆らせ逕滓・
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    vertices[index] = new Vector3(x, heightmap[x, y], y);
                    uvs[index] = new Vector2((float)x / width, (float)y / height);
                }
            }
            
            // 荳芽ｧ貞ｽ｢逕滓・
            int triangleIndex = 0;
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int bottomLeft = y * width + x;
                    int bottomRight = bottomLeft + 1;
                    int topLeft = (y + 1) * width + x;
                    int topRight = topLeft + 1;
                    
                    // 隨ｬ1荳芽ｧ貞ｽ｢
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = bottomRight;
                    
                    // 隨ｬ2荳芽ｧ貞ｽ｢
                    triangles[triangleIndex++] = bottomRight;
                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = topRight;
                }
            }
            
            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            return mesh;
        }
        
        private GameObject CreateTerrainGameObject(Vector2Int coordinate, Mesh mesh)
        {
            var terrainObject = new GameObject($"Terrain_{coordinate.x}_{coordinate.y}");
            
            var meshFilter = terrainObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            
            var meshRenderer = terrainObject.AddComponent<MeshRenderer>();
            meshRenderer.material = Resources.Load<Material>("Materials/TerrainMaterial");
            
            var meshCollider = terrainObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            
            // 菴咲ｽｮ險ｭ螳・
            const float tileSize = 2000f;
            terrainObject.transform.position = new Vector3(coordinate.x * tileSize, 0, coordinate.y * tileSize);
            
            return terrainObject;
        }
        
        private List<GameObject> GeneratePrimitiveObjects(Vector2Int coordinate, Transform parent)
        {
            var primitives = new List<GameObject>();
            
            // 邁｡蜊倥↑繝励Μ繝溘ユ繧｣繝也函謌撰ｼ亥ｮ滄圀縺ｮ螳溯｣・〒縺ｯ PrimitiveTerrainManager 繧剃ｽｿ逕ｨ・・
            int primitiveCount = Random.Range(1, 4);
            
            for (int i = 0; i < primitiveCount; i++)
            {
                var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
                primitive.transform.parent = parent;
                
                // 繝ｩ繝ｳ繝繝驟咲ｽｮ
                float x = Random.Range(-1000f, 1000f);
                float z = Random.Range(-1000f, 1000f);
                primitive.transform.localPosition = new Vector3(x, 50f, z);
                primitive.transform.localScale = Vector3.one * Random.Range(20f, 100f);
                
                primitives.Add(primitive);
            }
            
            return primitives;
        }
        
        private List<GameObject> RestorePrimitiveObjects(List<IntelligentCacheSystem.PrimitiveObjectData> primitiveData, Transform parent)
        {
            var primitives = new List<GameObject>();
            
            foreach (var data in primitiveData)
            {
                var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube); // 邁｡逡･蛹・
                primitive.transform.parent = parent;
                primitive.transform.localPosition = data.position;
                primitive.transform.localRotation = Quaternion.Euler(data.rotation);
                primitive.transform.localScale = data.scale;
                
                primitives.Add(primitive);
            }
            
            return primitives;
        }
        
        private List<IntelligentCacheSystem.PrimitiveObjectData> ConvertToPrimitiveData(List<GameObject> primitives)
        {
            var data = new List<IntelligentCacheSystem.PrimitiveObjectData>();
            
            foreach (var primitive in primitives)
            {
                data.Add(new IntelligentCacheSystem.PrimitiveObjectData
                {
                    position = primitive.transform.localPosition,
                    rotation = primitive.transform.localRotation.eulerAngles,
                    scale = primitive.transform.localScale,
                    primitiveType = "Cube", // 邁｡逡･蛹・
                    materialName = "Default"
                });
            }
            
            return data;
        }
        
        private int GetSeedFromCoordinate(Vector2Int coordinate)
        {
            return coordinate.x * 73856093 ^ coordinate.y * 19349663;
        }
        
        /// <summary>
        /// 繧ｭ繝｣繝・す繝･蜉ｹ邇・・譛驕ｩ蛹・
        /// </summary>
        public void OptimizeCacheEfficiency()
        {
            var stats = cacheSystem.GetStatistics();
            
            if (stats.hitRatio < 0.7f)
            {
                Debug.Log("Low cache hit ratio detected. Increasing preload radius.");
                // 繝励Μ繝ｭ繝ｼ繝牙濠蠕・・隱ｿ謨ｴ・亥ｮ溯｣・・ IntelligentCacheSystem 縺ｫ萓晏ｭ假ｼ・
            }
            
            if (stats.totalMemoryUsed > maxMemoryCacheSize * 1024 * 1024 * 0.9f)
            {
                Debug.Log("High memory usage detected. Triggering cache cleanup.");
                // 繝｡繝｢繝ｪ繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・縺ｮ螳溯｡・
            }
        }
        
        private void Update()
        {
            // 螳壽悄逧・↑繧ｭ繝｣繝・す繝･譛驕ｩ蛹・
            if (Time.time - lastCacheCheck > cacheCheckInterval)
            {
                OptimizeCacheEfficiency();
                lastCacheCheck = Time.time;
            }
            
            // 繝ｭ繝ｼ繝峨く繝･繝ｼ縺ｮ蜃ｦ逅・
            ProcessLoadQueue();
        }
        
        private void ProcessLoadQueue()
        {
            if (loadQueue.Count == 0 || activeLoadRequests.Count >= maxSimultaneousLoads) return;
            
            var request = loadQueue.Dequeue();
            VastcoreLogger.Instance.LogDebug("TerrainCache", $"ProcessLoadQueue dequeued coord={request.coordinate} active={activeLoadRequests.Count} remaining={loadQueue.Count}");
            StartCoroutine(ProcessLoadRequest(request));
        }
        
        private System.Collections.IEnumerator ProcessLoadRequest(TerrainLoadRequest request)
        {
            VastcoreLogger.Instance.LogDebug("TerrainCache", $"ProcessLoadRequest start coord={request.coordinate}");
            yield return StartCoroutine(GenerateTerrainCPU(request.coordinate, request.onComplete));
        }
        
        /// <summary>
        /// 繧ｭ繝｣繝・す繝･邨ｱ險医・蜿門ｾ・
        /// </summary>
        public IntelligentCacheSystem.CacheStatistics GetCacheStatistics()
        {
            return cacheSystem.GetStatistics();
        }
        
        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
}
