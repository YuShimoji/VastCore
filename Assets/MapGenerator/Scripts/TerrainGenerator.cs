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
        [Header("Terrain Settings")]
        [SerializeField] private int m_Width = 2048;
        [SerializeField] private int m_Height = 2048;
        [SerializeField] private int m_Depth = 600;
        [SerializeField] private int m_Resolution = 513;
        [SerializeField] private Material m_TerrainMaterial;

        [Header("Generation Mode")]
        [SerializeField] private GenerationMode m_GenerationMode = GenerationMode.Noise;

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

        public enum GenerationMode
        {
            Noise,
            HeightMap,
            NoiseAndHeightMap
        }

        private float[,] GenerateHeights()
        {
            switch (m_GenerationMode)
            {
                case GenerationMode.HeightMap:
                    return GenerateFromHeightMap();
                case GenerationMode.NoiseAndHeightMap:
                    return CombineNoiseAndHeightMap();
                case GenerationMode.Noise:
                default:
                    return GenerateFromNoise();
            }
        }

        private float[,] GenerateFromNoise()
        {
            float[,] heights = new float[m_Resolution, m_Resolution];
            for (int x = 0; x < m_Resolution; x++)
            {
                for (int y = 0; y < m_Resolution; y++)
                {
                    heights[x, y] = CalculateHeight(x, y);
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

        float[,] heights = new float[m_Resolution, m_Resolution];
        Color[] pixels = m_HeightMap.GetPixels();
        int width = m_HeightMap.width;
        int height = m_HeightMap.height;

        for (int y = 0; y < m_Resolution; y++)
        {
            int sourceY = m_FlipHeightMapVertically ? height - 1 - (y * height / m_Resolution) : y * height / m_Resolution;
            for (int x = 0; x < m_Resolution; x++)
            {
                int sourceX = x * width / m_Resolution;
                float heightValue = pixels[sourceY * width + sourceX].grayscale;
                heights[x, y] = heightValue * m_HeightMapScale + m_HeightMapOffset;
            }
        }

        return heights;
    }

    private float[,] CombineNoiseAndHeightMap()
    {
        float[,] noiseHeights = GenerateFromNoise();
        float[,] heightMapHeights = GenerateFromHeightMap();
        float[,] combinedHeights = new float[m_Resolution, m_Resolution];

        for (int y = 0; y < m_Resolution; y++)
        {
            for (int x = 0; x < m_Resolution; x++)
            {
                combinedHeights[x, y] = (noiseHeights[x, y] + heightMapHeights[x, y]) * 0.5f;
            }
        }

        return combinedHeights;
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