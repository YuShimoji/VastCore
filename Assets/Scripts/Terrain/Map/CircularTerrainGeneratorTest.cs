using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Generation.Tests
{
    /// <summary>
    /// CircularTerrainGeneratorのテストクラス
    /// </summary>
    public class CircularTerrainGeneratorTest : MonoBehaviour
    {
        [Header("テスト設定")]
        public bool runTestOnStart = true;
        public bool generateTestTerrain = true;
        public Material testMaterial;
        
        [Header("地形パラメータ")]
        public MeshGenerator.TerrainGenerationParams baseParams = MeshGenerator.TerrainGenerationParams.Default();
        public CircularTerrainGenerator.CircularTerrainParams circularParams = CircularTerrainGenerator.CircularTerrainParams.Default();
        
        [Header("テスト結果")]
        public GameObject generatedTerrain;
        public CircularTerrainGenerator.CircularTerrainStats terrainStats;
        
        void Start()
        {
            if (runTestOnStart)
            {
                RunCircularTerrainTest();
            }
        }
        
        [ContextMenu("Run Circular Terrain Test")]
        public void RunCircularTerrainTest()
        {
            Debug.Log("=== CircularTerrainGenerator Test Started ===");
            
            try
            {
                // 1. 基本的な円形地形生成テスト
                TestBasicCircularGeneration();
                
                // 2. パラメータ変更テスト
                TestParameterVariations();
                
                // 3. 境界ブレンドテスト
                TestBoundaryBlending();
                
                // 4. 統計情報テスト
                TestStatistics();
                
                // 5. 実際の地形生成
                if (generateTestTerrain)
                {
                    GenerateTestTerrain();
                }
                
                Debug.Log("=== CircularTerrainGenerator Test Completed Successfully ===");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"CircularTerrainGenerator Test Failed: {e.Message}");
                Debug.LogError(e.StackTrace);
            }
        }
        
        private void TestBasicCircularGeneration()
        {
            Debug.Log("Testing basic circular generation...");
            
            // 基本パラメータでテスト
            var testParams = MeshGenerator.TerrainGenerationParams.Default();
            testParams.resolution = 256; // テスト用に小さく
            testParams.size = 1000f;
            
            var mesh = CircularTerrainGenerator.GenerateCircularTerrain(testParams);
            
            Assert(mesh != null, "Generated mesh should not be null");
            Assert(mesh.vertices.Length > 0, "Generated mesh should have vertices");
            Assert(mesh.triangles.Length > 0, "Generated mesh should have triangles");
            
            Debug.Log($"✓ Basic generation test passed. Vertices: {mesh.vertices.Length}, Triangles: {mesh.triangles.Length / 3}");
        }
        
        private void TestParameterVariations()
        {
            Debug.Log("Testing parameter variations...");
            
            var baseParams = MeshGenerator.TerrainGenerationParams.Default();
            baseParams.resolution = 128; // 小さくしてテスト高速化
            
            var circularParams = CircularTerrainGenerator.CircularTerrainParams.Default();
            
            // 異なる半径でテスト
            float[] testRadii = { 500f, 1000f, 1500f };
            foreach (float radius in testRadii)
            {
                circularParams.radius = radius;
                var mesh = CircularTerrainGenerator.GenerateCircularTerrain(baseParams, circularParams);
                Assert(mesh != null, $"Mesh generation should succeed with radius {radius}");
            }
            
            // 異なるフォールオフ強度でテスト
            float[] testFalloffs = { 0.5f, 1.0f, 2.0f };
            foreach (float falloff in testFalloffs)
            {
                circularParams.falloffStrength = falloff;
                var mesh = CircularTerrainGenerator.GenerateCircularTerrain(baseParams, circularParams);
                Assert(mesh != null, $"Mesh generation should succeed with falloff {falloff}");
            }
            
            Debug.Log("✓ Parameter variation tests passed");
        }
        
        private void TestBoundaryBlending()
        {
            Debug.Log("Testing boundary blending...");
            
            var baseParams = MeshGenerator.TerrainGenerationParams.Default();
            baseParams.resolution = 128;
            
            var circularParams = CircularTerrainGenerator.CircularTerrainParams.Default();
            circularParams.enableBoundaryBlend = true;
            circularParams.blendDistance = 50f;
            
            var mesh = CircularTerrainGenerator.GenerateCircularTerrain(baseParams, circularParams);
            Assert(mesh != null, "Mesh generation with boundary blending should succeed");
            
            // ブレンドなしでも生成できることを確認
            circularParams.enableBoundaryBlend = false;
            var meshNoBlend = CircularTerrainGenerator.GenerateCircularTerrain(baseParams, circularParams);
            Assert(meshNoBlend != null, "Mesh generation without boundary blending should succeed");
            
            Debug.Log("✓ Boundary blending tests passed");
        }
        
        private void TestStatistics()
        {
            Debug.Log("Testing statistics calculation...");
            
            var baseParams = MeshGenerator.TerrainGenerationParams.Default();
            baseParams.resolution = 128;
            
            var circularParams = CircularTerrainGenerator.CircularTerrainParams.Default();
            
            // ハイトマップを生成
            var heightmap = MeshGenerator.GenerateHeightmap(baseParams);
            
            // 統計を計算
            var stats = CircularTerrainGenerator.GetCircularTerrainStats(heightmap, circularParams);
            
            Assert(stats.radius > 0, "Radius should be positive");
            Assert(stats.circularArea > 0, "Circular area should be positive");
            Assert(stats.baseStats.resolution > 0, "Resolution should be positive");
            
            Debug.Log($"✓ Statistics test passed. Radius: {stats.radius}, Area: {stats.circularArea:F2}");
        }
        
        private void GenerateTestTerrain()
        {
            Debug.Log("Generating test terrain...");
            
            // 既存の地形を削除
            if (generatedTerrain != null)
            {
                DestroyImmediate(generatedTerrain);
            }
            
            // 新しい地形を生成
            var mesh = CircularTerrainGenerator.GenerateCircularTerrain(baseParams, circularParams);
            
            generatedTerrain = new GameObject("Generated Circular Terrain");
            generatedTerrain.transform.position = Vector3.zero;
            
            var meshFilter = generatedTerrain.AddComponent<MeshFilter>();
            var meshRenderer = generatedTerrain.AddComponent<MeshRenderer>();
            var meshCollider = generatedTerrain.AddComponent<MeshCollider>();
            
            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;
            
            if (testMaterial != null)
            {
                meshRenderer.material = testMaterial;
            }
            else
            {
                // デフォルトマテリアルを作成
                var material = new Material(Shader.Find("Standard"));
                material.color = Color.green;
                meshRenderer.material = material;
            }
            
            // 統計情報を更新
            var heightmap = MeshGenerator.GenerateHeightmap(baseParams);
            terrainStats = CircularTerrainGenerator.GetCircularTerrainStats(heightmap, circularParams);
            
            Debug.Log($"✓ Test terrain generated successfully at {generatedTerrain.transform.position}");
        }
        
        private void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new System.Exception($"Assertion failed: {message}");
            }
        }
        
        [ContextMenu("Test Distance Falloff")]
        public void TestDistanceFalloff()
        {
            Debug.Log("Testing distance falloff calculation...");
            
            Vector2 center = Vector2.zero;
            float radius = 100f;
            AnimationCurve curve = AnimationCurve.EaseInOut(0, 1, 1, 0);
            
            // 異なる距離でテスト
            float[] testDistances = { 0f, 25f, 50f, 75f, 100f, 125f };
            
            foreach (float distance in testDistances)
            {
                Vector2 testPos = new Vector2(distance, 0);
                float falloff = CircularTerrainGenerator.CalculateDistanceFalloff(testPos, center, radius, curve);
                Debug.Log($"Distance: {distance}, Falloff: {falloff:F3}");
            }
            
            Debug.Log("✓ Distance falloff test completed");
        }
        
        [ContextMenu("Test Boundary Detection")]
        public void TestBoundaryDetection()
        {
            Debug.Log("Testing boundary detection...");
            
            var heightmap = MeshGenerator.GenerateHeightmap(baseParams);
            var boundaries = CircularTerrainGenerator.DetectCircularBoundary(heightmap, 0.1f);
            
            Debug.Log($"✓ Detected {boundaries.Count} boundary points");
        }
    }
}