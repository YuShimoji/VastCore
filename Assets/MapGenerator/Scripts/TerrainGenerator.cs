using UnityEngine;
// using Vastcore.Diagnostics;
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
        public TerrainLayer[] TerrainLayers { get => m_TerrainLayers; set => m_TerrainLayers = value; }
        public float[] TextureBlendFactors { get => m_TextureBlendFactors; set => m_TextureBlendFactors = value; }
        public Vector2[] TextureTiling { get => m_TextureTiling; set => m_TextureTiling = value; }
        public float Persistence { get => m_Persistence; set => m_Persistence = value; }
        public float Lacunarity { get => m_Lacunarity; set => m_Lacunarity = value; }
        public Vector2 Offset { get => m_Offset; set => m_Offset = value; }

        public DetailPrototype[] DetailPrototypes { get => m_DetailPrototypes; set => m_DetailPrototypes = value; }
        public int DetailResolution { get => m_DetailResolution; set => m_DetailResolution = value; }
        public int DetailResolutionPerPatch { get => m_DetailResolutionPerPatch; set => m_DetailResolutionPerPatch = value; }
        public float DetailDensity { get => m_DetailDensity; set => m_DetailDensity = value; }
        public float DetailDistance { get => m_DetailDistance; set => m_DetailDistance = value; }

        public TreePrototype[] TreePrototypes { get => m_TreePrototypes; set => m_TreePrototypes = value; }
        public int TreeDistance { get => m_TreeDistance; set => m_TreeDistance = value; }
        public int TreeBillboardDistance { get => m_TreeBillboardDistance; set => m_TreeBillboardDistance = value; }
        public int TreeCrossFadeLength { get => m_TreeCrossFadeLength; set => m_TreeCrossFadeLength = value; }
        public int TreeMaximumFullLODCount { get => m_TreeMaximumFullLODCount; set => m_TreeMaximumFullLODCount = value; }
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

        [Header("Texture Settings")]
        [SerializeField] private TerrainLayer[] m_TerrainLayers;
        [SerializeField] private float[] m_TextureBlendFactors = new float[0];
        [SerializeField] private Vector2[] m_TextureTiling = { new Vector2(100, 100) };

        [Header("Detail Settings")]
        [SerializeField] private DetailPrototype[] m_DetailPrototypes;
        [SerializeField] private int m_DetailResolution = 1024;
        [SerializeField] private int m_DetailResolutionPerPatch = 8;
        [SerializeField] private float m_DetailDensity = 1.0f;
        [SerializeField] private float m_DetailDistance = 200f;

        [Header("Tree Settings")]
        [SerializeField] private TreePrototype[] m_TreePrototypes;
        [SerializeField] private int m_TreeDistance = 2000;
        [SerializeField] private int m_TreeBillboardDistance = 300;
        [SerializeField] private int m_TreeCrossFadeLength = 50;
        [SerializeField] private int m_TreeMaximumFullLODCount = 50;
        #endregion

        public Vector3 terrainSize => new Vector3(m_Width, m_Depth, m_Height);

        public UnityEngine.Terrain GeneratedTerrain { get; private set; }

        public IEnumerator GenerateTerrain()
        {
            Debug.Log("[TerrainGenerator] Starting terrain generation...");

            // テレインデータの作成
            var terrainData = new TerrainData();
            terrainData.heightmapResolution = m_Resolution;
            terrainData.size = new Vector3(m_Width, m_Depth, m_Height);

            // 高さマップの生成
            var heights = HeightMapGenerator.GenerateHeights(this);
            // using (LoadProfiler.Measure("TerrainData.SetHeights (TerrainGenerator)"))
            {
                // 大規模Terrainをバッチ処理で設定（メモリスパイク軽減）
                SetHeightsInBatches(terrainData, heights);
            }

            // テレインオブジェクトの作成
            GameObject terrainObject = UnityEngine.Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = "Generated_Vastcore_Terrain";
            GeneratedTerrain = terrainObject.GetComponent<UnityEngine.Terrain>();

            // マテリアルの設定
            if (m_TerrainMaterial == null)
            {
                m_TerrainMaterial = Resources.Load<Material>("GroundMaterial");
                Debug.LogWarning("Terrain material was not set. Loading default 'GroundMaterial'.");
            }
            GeneratedTerrain.materialTemplate = m_TerrainMaterial;

            // テクスチャレイヤーの設定
            if (m_TerrainLayers != null && m_TerrainLayers.Length > 0)
            {
                terrainData.terrainLayers = m_TerrainLayers;
                ConfigureTextureLayers(terrainData, heights);
            }

            // 詳細マップの設定
            if (m_DetailPrototypes != null && m_DetailPrototypes.Length > 0)
            {
                ConfigureDetailMap(terrainData);
            }

            // ツリーの設定
            if (m_TreePrototypes != null && m_TreePrototypes.Length > 0)
            {
                ConfigureTrees(terrainData);
            }

            // テレイン設定の最適化
            OptimizeTerrainSettings();

            // レイヤーの設定
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



        private void ConfigureTextureLayers(TerrainData terrainData, float[,] heights)
        {
            TextureGenerator.ConfigureTextureLayers(this, terrainData, heights);
        }

        private void ConfigureDetailMap(TerrainData terrainData)
        {
            DetailGenerator.ConfigureDetailMap(this, terrainData);
        }

        private void ConfigureTrees(TerrainData terrainData)
        {
            TreeGenerator.ConfigureTrees(this, terrainData);
        }

        private void OptimizeTerrainSettings()
        {
            TerrainOptimizer.OptimizeTerrainSettings(this);
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

        /// <summary>
        /// 高さマップをバッチ処理で設定（メモリスパイク軽減）
        /// </summary>
        private void SetHeightsInBatches(TerrainData terrainData, float[,] heights)
        {
            int height = heights.GetLength(0);
            int width = heights.GetLength(1);
            int batchSize = 256; // 256x256のバッチサイズ

            for (int yStart = 0; yStart < height; yStart += batchSize)
            {
                for (int xStart = 0; xStart < width; xStart += batchSize)
                {
                    int yEnd = Mathf.Min(yStart + batchSize, height);
                    int xEnd = Mathf.Min(xStart + batchSize, width);
                    int batchHeight = yEnd - yStart;
                    int batchWidth = xEnd - xStart;

                    float[,] batchHeights = new float[batchHeight, batchWidth];
                    for (int y = 0; y < batchHeight; y++)
                    {
                        for (int x = 0; x < batchWidth; x++)
                        {
                            batchHeights[y, x] = heights[yStart + y, xStart + x];
                        }
                    }

                    terrainData.SetHeights(yStart, xStart, batchHeights);
                    // フレーム分散（必要に応じてyield return null;）
                }
            }
        }
    }
}