using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Testing
{
    /// <summary>
    /// 基本的なTerrain生成テストコンポーネント
    /// Scene上でボタンクリックでTerrainを生成可能
    /// </summary>
    public class TerrainSpawner : MonoBehaviour
    {
        [Header("Terrain Settings")]
        [SerializeField] private int width = 512;
        [SerializeField] private int height = 512;
        [SerializeField] private int depth = 300;
        [SerializeField] private int resolution = 257;
        [SerializeField] private float scale = 50f;
        [SerializeField] private int octaves = 4;
        [SerializeField] private float persistence = 0.5f;
        [SerializeField] private float lacunarity = 2f;
        [SerializeField] private Material terrainMaterial;

        private TerrainGenerator terrainGenerator;

        void Start()
        {
            InitializeTerrainGenerator();
        }

        private void InitializeTerrainGenerator()
        {
            GameObject generatorObject = new GameObject("TerrainGenerator");
            terrainGenerator = generatorObject.AddComponent<TerrainGenerator>();

            // 設定を適用
            terrainGenerator.Width = width;
            terrainGenerator.Height = height;
            terrainGenerator.Depth = depth;
            terrainGenerator.Resolution = resolution;
            terrainGenerator.Scale = scale;
            terrainGenerator.Octaves = octaves;
            terrainGenerator.Persistence = persistence;
            terrainGenerator.Lacunarity = lacunarity;
            terrainGenerator.TerrainMaterial = terrainMaterial;
            terrainGenerator.GenerationMode = TerrainGenerator.TerrainGenerationMode.Noise;
        }

        [ContextMenu("Generate Terrain")]
        public void GenerateTerrain()
        {
            if (terrainGenerator == null)
            {
                InitializeTerrainGenerator();
            }

            Debug.Log("Generating terrain...");
            StartCoroutine(terrainGenerator.GenerateTerrain());
        }

        [ContextMenu("Clear Terrain")]
        public void ClearTerrain()
        {
            UnityEngine.Terrain existingTerrain = FindObjectOfType<UnityEngine.Terrain>();
            if (existingTerrain != null)
            {
                DestroyImmediate(existingTerrain.gameObject);
                Debug.Log("Existing terrain cleared.");
            }
        }
    }
}
