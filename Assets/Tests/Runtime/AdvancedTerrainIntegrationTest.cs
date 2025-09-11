using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Testing
{
    /// <summary>
    /// 高度地形アルゴリズムの統合テスト
    /// 既存システムとの連携確認
    /// </summary>
    public class AdvancedTerrainIntegrationTest : MonoBehaviour
    {
        [Header("統合テスト設定")]
        public bool runOnStart = true;
        public bool testMeshGeneratorIntegration = true;
        public bool testTerrainTileIntegration = true;
        public bool testRuntimeGeneration = true;
        
        [Header("テスト地形設定")]
        public int testResolution = 256;
        public float testSize = 1000f;
        public float testMaxHeight = 100f;
        
        [Header("浸食テスト設定")]
        [Range(0f, 1f)]
        public float lightErosionStrength = 0.1f;
        [Range(0f, 1f)]
        public float mediumErosionStrength = 0.5f;
        [Range(0f, 1f)]
        public float heavyErosionStrength = 0.8f;
        
        [Header("結果表示")]
        public Material testTerrainMaterial;
        public Transform displayParent;
        
        private void Start()
        {
            if (runOnStart)
            {
                RunIntegrationTests();
            }
        }
        
        [ContextMenu("Run Integration Tests")]
        public void RunIntegrationTests()
        {
            Debug.Log("=== 高度地形アルゴリズム統合テスト開始 ===");
            
            if (testMeshGeneratorIntegration)
            {
                TestMeshGeneratorIntegration();
            }
            
            if (testTerrainTileIntegration)
            {
                TestTerrainTileIntegration();
            }
            
            if (testRuntimeGeneration)
            {
                TestRuntimeGeneration();
            }
            
            Debug.Log("=== 統合テスト完了 ===");
        }
        
        /// <summary>
        /// MeshGeneratorとの統合テスト
        /// </summary>
        private void TestMeshGeneratorIntegration()
        {
            Debug.Log("MeshGenerator統合テスト開始...");
            
            var terrainParams = MeshGenerator.TerrainGenerationParams.Default();
            terrainParams.resolution = testResolution;
            terrainParams.size = testSize;
            terrainParams.maxHeight = testMaxHeight;
            terrainParams.enableErosion = true;
            
            // 軽い浸食テスト
            terrainParams.erosionStrength = lightErosionStrength;
            Mesh lightErosionMesh = MeshGenerator.GenerateAdvancedTerrain(terrainParams);
            CreateTestTerrain("Light Erosion", lightErosionMesh, 0);
            
            // 中程度の浸食テスト
            terrainParams.erosionStrength = mediumErosionStrength;
            Mesh mediumErosionMesh = MeshGenerator.GenerateAdvancedTerrain(terrainParams);
            CreateTestTerrain("Medium Erosion", mediumErosionMesh, 1);
            
            // 強い浸食テスト
            terrainParams.erosionStrength = heavyErosionStrength;
            Mesh heavyErosionMesh = MeshGenerator.GenerateAdvancedTerrain(terrainParams);
            CreateTestTerrain("Heavy Erosion", heavyErosionMesh, 2);
            
            // 長期進化テスト
            Mesh evolutionMesh = MeshGenerator.GenerateEvolutionaryTerrain(terrainParams, 3);
            CreateTestTerrain("Evolution", evolutionMesh, 3);
            
            Debug.Log("MeshGenerator統合テスト完了");
        }
        
        /// <summary>
        /// TerrainTileとの統合テスト
        /// </summary>
        private void TestTerrainTileIntegration()
        {
            Debug.Log("TerrainTile統合テスト開始...");
            
            // テスト用タイルを作成
            var testTile = new TerrainTile(Vector2Int.zero, testSize);
            
            // 地形パラメータを設定
            testTile.terrainParams = MeshGenerator.TerrainGenerationParams.Default();
            testTile.terrainParams.resolution = testResolution;
            testTile.terrainParams.size = testSize;
            testTile.terrainParams.maxHeight = testMaxHeight;
            testTile.terrainParams.enableErosion = true;
            testTile.terrainParams.erosionStrength = mediumErosionStrength;
            
            // 円形パラメータを設定
            testTile.circularParams = CircularTerrainGenerator.CircularTerrainParams.Default();
            testTile.circularParams.radius = testSize * 0.4f;
            
            // タイルを生成
            testTile.GenerateTile(displayParent);
            
            if (testTile.tileObject != null)
            {
                testTile.tileObject.name = "TerrainTile Integration Test";
                testTile.tileObject.transform.position = new Vector3(testSize * 5, 0, 0);
                Debug.Log($"TerrainTile生成成功: 生成時間 {testTile.generationTime:F3}秒");
            }
            else
            {
                Debug.LogError("TerrainTile生成失敗");
            }
            
            Debug.Log("TerrainTile統合テスト完了");
        }
        
        /// <summary>
        /// 実行時生成テスト
        /// </summary>
        private void TestRuntimeGeneration()
        {
            Debug.Log("実行時生成テスト開始...");
            
            // 複数の浸食レベルで実行時生成をテスト
            float[] erosionLevels = { 0.1f, 0.3f, 0.6f, 0.9f };
            
            for (int i = 0; i < erosionLevels.Length; i++)
            {
                var terrainParams = MeshGenerator.TerrainGenerationParams.Default();
                terrainParams.resolution = testResolution / 2; // 実行時は軽量化
                terrainParams.size = testSize;
                terrainParams.maxHeight = testMaxHeight;
                terrainParams.enableErosion = true;
                terrainParams.erosionStrength = erosionLevels[i];
                
                float startTime = Time.realtimeSinceStartup;
                Mesh runtimeMesh = MeshGenerator.GenerateAdvancedTerrain(terrainParams);
                float generationTime = Time.realtimeSinceStartup - startTime;
                
                CreateTestTerrain($"Runtime {erosionLevels[i]:F1}", runtimeMesh, i + 10);
                
                Debug.Log($"実行時生成 (浸食{erosionLevels[i]:F1}): {generationTime:F3}秒");
            }
            
            Debug.Log("実行時生成テスト完了");
        }
        
        /// <summary>
        /// テスト地形を作成
        /// </summary>
        private void CreateTestTerrain(string name, Mesh mesh, int index)
        {
            if (mesh == null || displayParent == null)
                return;
            
            GameObject terrainObject = new GameObject(name);
            terrainObject.transform.SetParent(displayParent);
            terrainObject.transform.position = new Vector3(index * testSize * 1.2f, 0, 0);
            
            // MeshFilter
            MeshFilter meshFilter = terrainObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;
            
            // MeshRenderer
            MeshRenderer meshRenderer = terrainObject.AddComponent<MeshRenderer>();
            meshRenderer.material = testTerrainMaterial;
            
            // MeshCollider
            MeshCollider meshCollider = terrainObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            
            // ラベル作成
            CreateLabel(terrainObject, name);
        }
        
        /// <summary>
        /// ラベルを作成
        /// </summary>
        private void CreateLabel(GameObject parent, string text)
        {
            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(parent.transform);
            labelObject.transform.localPosition = new Vector3(0, testMaxHeight + 50f, 0);
            
            // TextMeshがある場合はラベルを作成
            var textMesh = labelObject.AddComponent<TextMesh>();
            if (textMesh != null)
            {
                textMesh.text = text;
                textMesh.fontSize = 20;
                textMesh.anchor = TextAnchor.MiddleCenter;
                textMesh.color = Color.white;
            }
        }
        
        [ContextMenu("Clear Test Terrains")]
        public void ClearTestTerrains()
        {
            if (displayParent != null)
            {
                for (int i = displayParent.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(displayParent.GetChild(i).gameObject);
                }
            }
        }
        
        [ContextMenu("Performance Test")]
        public void PerformanceTest()
        {
            Debug.Log("=== パフォーマンステスト開始 ===");
            
            var terrainParams = MeshGenerator.TerrainGenerationParams.Default();
            terrainParams.resolution = 512;
            terrainParams.enableErosion = true;
            
            // 各浸食レベルでのパフォーマンステスト
            float[] erosionLevels = { 0.1f, 0.3f, 0.5f, 0.7f, 0.9f };
            
            foreach (float erosionLevel in erosionLevels)
            {
                terrainParams.erosionStrength = erosionLevel;
                
                float startTime = Time.realtimeSinceStartup;
                Mesh testMesh = MeshGenerator.GenerateAdvancedTerrain(terrainParams);
                float generationTime = Time.realtimeSinceStartup - startTime;
                
                Debug.Log($"浸食レベル {erosionLevel:F1}: {generationTime:F3}秒, 頂点数: {testMesh.vertexCount}");
                
                // メモリクリーンアップ
                DestroyImmediate(testMesh);
            }
            
            Debug.Log("=== パフォーマンステスト完了 ===");
        }
    }
}