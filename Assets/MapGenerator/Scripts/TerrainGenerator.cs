using UnityEngine;
using System.IO;
using System.Collections;

namespace Vastcore.Generation
{
    /// <summary>
    /// 高度な地形生成システム
    /// Perlin Noiseをベースに、広大で自然な地形を生成する
    /// </summary>
    public class TerrainGenerator : MonoBehaviour
    {
        #region Public Properties
        public int Width { get => m_Width; set => m_Width = value; }
        public int Height { get => m_Height; set => m_Height = value; }
        public int Depth { get => m_Depth; set => m_Depth = value; }
        public int Resolution { get => m_Resolution; set => m_Resolution = value; }
        public Material TerrainMaterial { get => m_TerrainMaterial; set => m_TerrainMaterial = value; }
        public TerrainGenerationMode GenerationMode { get => m_GenerationMode; set => m_GenerationMode = value; }
        public Texture2D HeightMap { get => m_HeightMap; set => m_HeightMap = value; }
        public float HeightMapScale { get => m_HeightMapScale; set => m_HeightMapScale = value; }
        public float HeightMapOffset { get => m_HeightMapOffset; set => m_HeightMapOffset = value; }
        public bool FlipHeightMapVertically { get => m_FlipHeightMapVertically; set => m_FlipHeightMapVertically = value; }
        public float Scale { get => m_Scale; set => m_Scale = value; }
        public int Octaves { get => m_Octaves; set => m_Octaves = value; }
        public float Persistence { get => m_Persistence; set => m_Persistence = value; }
        public float Lacunarity { get => m_Lacunarity; set => m_Lacunarity = value; }
        public Vector2 Offset { get => m_Offset; set => m_Offset = value; }
        #endregion

        #region Serialized Fields
        [Header("Terrain Settings")]
        [SerializeField] private int m_Width = 2048;
        [SerializeField] private int m_Height = 2048;
        [SerializeField] private int m_Depth = 600;
        [SerializeField] private int m_Resolution = 513;
        [SerializeField] private Material m_TerrainMaterial;

        [Header("Generation Mode")]
        [SerializeField] private TerrainGenerationMode m_GenerationMode = TerrainGenerationMode.Noise;

        [Header("Height Map Settings")]
        [SerializeField] private Texture2D m_HeightMap;
        [SerializeField] private float m_HeightMapScale = 1.0f;
        [SerializeField] private float m_HeightMapOffset = 0.0f;
        [SerializeField] private bool m_FlipHeightMapVertically = false;

        [Header("Noise Settings")]
        [SerializeField] private float m_Scale = 50f;
        [SerializeField] private int m_Octaves = 8;
        [SerializeField] [Range(0,1)] private float m_Persistence = 0.5f;
        [SerializeField] private float m_Lacunarity = 2f;
        [SerializeField] private Vector2 m_Offset;
        #endregion

        public Vector3 terrainSize => new Vector3(m_Width, m_Depth, m_Height);

        public Terrain GeneratedTerrain { get; private set; }

        public IEnumerator GenerateTerrain()
        {
            Debug.Log("[TerrainGenerator] Starting terrain generation...");

            var terrainData = new TerrainData();
            terrainData.heightmapResolution = m_Resolution;
            terrainData.size = new Vector3(m_Width, m_Depth, m_Height);

            var heights = GenerateHeights();
            terrainData.SetHeights(0, 0, heights);

            GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = "Generated_Vastcore_Terrain";
            GeneratedTerrain = terrainObject.GetComponent<Terrain>();
            if (m_TerrainMaterial == null)
            {
                m_TerrainMaterial = Resources.Load<Material>("GroundMaterial");
                Debug.LogWarning("Terrain material was not set. Loading default 'GroundMaterial'.");
            }
            GeneratedTerrain.materialTemplate = m_TerrainMaterial;

            int terrainLayer = LayerMask.NameToLayer("Terrain");
            if (terrainLayer != -1)
            {
                GeneratedTerrain.gameObject.layer = terrainLayer;
            }
            else
            {
                Debug.LogWarning("Layer 'Terrain' does not exist. Please create it in the Tag and Layer Manager.");
            }

            Debug.Log("[TerrainGenerator] Terrain generation completed.");
            yield return null;
        }

        public enum TerrainGenerationMode
        {
            Noise,
            HeightMap,
            NoiseAndHeightMap
        }

        private float[,] GenerateHeights()
        {
            switch (m_GenerationMode)
            {
                case TerrainGenerationMode.HeightMap:
                    return GenerateFromHeightMap();
                case TerrainGenerationMode.NoiseAndHeightMap:
                    return CombineNoiseAndHeightMap();
                case TerrainGenerationMode.Noise:
                default:
                    return GenerateFromNoise();
            }
        }

        private float[,] GenerateFromNoise()
        {
            float[,] heights = new float[m_Resolution, m_Resolution];
            float maxPossibleHeight = 0;
            float amplitude = 1;
            
            // 最大高さを計算（正規化用）
            for (int i = 0; i < m_Octaves; i++)
            {
                maxPossibleHeight += amplitude;
                amplitude *= m_Persistence;
            }
            
            // ノイズ生成
            for (int y = 0; y < m_Resolution; y++)
            {
                for (int x = 0; x < m_Resolution; x++)
                {
                    amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;
                    float weight = 1;
                    
                    // オクターブごとにノイズを重ね合わせる
                    for (int i = 0; i < m_Octaves; i++)
                    {
                        float sampleX = (x - m_Resolution / 2f + m_Offset.x) / m_Scale * frequency;
                        float sampleY = (y - m_Resolution / 2f + m_Offset.y) / m_Scale * frequency;
                        
                        // パーリンノイズの値を取得（-1〜1の範囲）
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        
                        // 高さを重み付けして加算
                        noiseHeight += perlinValue * weight;
                        
                        // 重みを減衰
                        weight = Mathf.Clamp01(weight * m_Persistence);
                        
                        // 周波数を増加
                        frequency *= m_Lacunarity;
                    }
                    
                    // 正規化して0〜1の範囲に収める
                    heights[x, y] = Mathf.Clamp01((noiseHeight / maxPossibleHeight + 1f) * 0.5f);
                }
            }
            
            return heights;
        }

        private float[,] GenerateFromHeightMap()
        {
            if (m_HeightMap == null)
            {
                Debug.LogError("[TerrainGenerator] Height map is not assigned!");
                return new float[m_Resolution, m_Resolution];
            }

            // ハイトマップのピクセルデータを取得
            Color[] pixels = m_HeightMap.GetPixels();
            int sourceWidth = m_HeightMap.width;
            int sourceHeight = m_HeightMap.height;
            
            float[,] heights = new float[m_Resolution, m_Resolution];
            
            // バイリニアフィルタリングを使用して高解像度のハイトマップを生成
            for (int y = 0; y < m_Resolution; y++)
            {
                float v = (float)y / (m_Resolution - 1);
                if (m_FlipHeightMapVertically) v = 1 - v;
                
                float sourceY = v * (sourceHeight - 1);
                int y1 = Mathf.FloorToInt(sourceY);
                int y2 = Mathf.Min(y1 + 1, sourceHeight - 1);
                float fy = sourceY - y1;
                
                for (int x = 0; x < m_Resolution; x++)
                {
                    float u = (float)x / (m_Resolution - 1);
                    float sourceX = u * (sourceWidth - 1);
                    int x1 = Mathf.FloorToInt(sourceX);
                    int x2 = Mathf.Min(x1 + 1, sourceWidth - 1);
                    float fx = sourceX - x1;
                    
                    // バイリニア補間
                    float c00 = pixels[y1 * sourceWidth + x1].grayscale;
                    float c10 = pixels[y1 * sourceWidth + x2].grayscale;
                    float c01 = pixels[y2 * sourceWidth + x1].grayscale;
                    float c11 = pixels[y2 * sourceWidth + x2].grayscale;
                    
                    // バイリニア補間を適用
                    float height = Mathf.Lerp(
                        Mathf.Lerp(c00, c10, fx),
                        Mathf.Lerp(c01, c11, fx),
                        fy
                    );
                    
                    // スケールとオフセットを適用
                    heights[x, y] = Mathf.Clamp01(height * m_HeightMapScale + m_HeightMapOffset);
                }
            }
            
            return heights;
        }
    }

        private float[,] CombineNoiseAndHeightMap()
        {
            float[,] noiseHeights = GenerateFromNoise();
            float[,] heightMapHeights = GenerateFromHeightMap();
            float[,] combinedHeights = new float[m_Resolution, m_Resolution];
            
            // ハイトマップの勾配を計算して、ノイズの影響を調整
            for (int y = 0; y < m_Resolution; y++)
            {
                for (int x = 0; x < m_Resolution; x++)
                {
                    // ハイトマップの勾配を計算（簡易的に周囲の高さの差分から）
                    float gradient = 0;
                    int samples = 0;
                    int radius = 1;
                    
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int nx = Mathf.Clamp(x + dx, 0, m_Resolution - 1);
                            int ny = Mathf.Clamp(y + dy, 0, m_Resolution - 1);
                            gradient += Mathf.Abs(heightMapHeights[x, y] - heightMapHeights[nx, ny]);
                            samples++;
                        }
                    }
                    
                    gradient /= samples;
                    
                    // 勾配が大きい場所（急な斜面）ではノイズの影響を小さくする
                    float noiseInfluence = Mathf.Lerp(0.5f, 0.1f, Mathf.Clamp01(gradient * 10f));
                    
                    // ハイトマップとノイズをブレンド
                    combinedHeights[x, y] = Mathf.Lerp(
                        heightMapHeights[x, y],
                        noiseHeights[x, y],
                        noiseInfluence
                    );
                }
            }
            
            return combinedHeights;
        }
        }

        private float CalculateHeight(int x, int y)
        {
            float amplitude = 1;
            float frequency = 1;
            float noiseHeight = 0;

            for (int i = 0; i < m_Octaves; i++)
            {
                float sampleX = (x + m_Offset.x) / m_Scale * frequency;
                float sampleY = (y + m_Offset.y) / m_Scale * frequency;

                float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                noiseHeight += perlinValue * amplitude;

                amplitude *= m_Persistence;
                frequency *= m_Lacunarity;
            }

            return noiseHeight;
        }

        public Vector3 GetHighestPoint()
        {
            if (GeneratedTerrain == null) return Vector3.zero;

            int resolution = GeneratedTerrain.terrainData.heightmapResolution;
            float maxHeight = 0;
            Vector3 highestPoint = Vector3.zero;

            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    float currentHeight = GeneratedTerrain.terrainData.GetHeight(x, y);
                    if (currentHeight > maxHeight)
                    {
                        maxHeight = currentHeight;
                        highestPoint = new Vector3(y, currentHeight, x);
                    }
                }
            }

            // Convert from local terrain coordinates to world coordinates
            return GeneratedTerrain.transform.TransformPoint(highestPoint);
        }
    }
}