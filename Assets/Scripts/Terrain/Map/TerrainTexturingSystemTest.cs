using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    /// <summary>
    /// 地形テクスチャリングシステムのテストクラス
    /// 要求1.5, 2.1の実装を検証
    /// </summary>
    public class TerrainTexturingSystemTest : MonoBehaviour
    {
        #region テスト設定
        [Header("テスト設定")]
        public bool runTestsOnStart = true;
        public bool enableVisualTests = true;
        public bool enablePerformanceTests = true;
        public bool enableIntegrationTests = true;
        
        [Header("テスト対象")]
        public TerrainTexturingSystem texturingSystem;
        public DynamicMaterialBlendingSystem blendingSystem;
        public RuntimeTerrainManager terrainManager;
        
        [Header("テストデータ")]
        public BiomePreset[] testBiomePresets;
        public int testTileCount = 5;
        public float testDuration = 30f;
        #endregion

        #region プライベート変数
        private List<TerrainTile> testTiles = new List<TerrainTile>();
        private TestResults testResults = new TestResults();
        private bool testsRunning = false;
        #endregion

        #region Unity イベント
        void Start()
        {
            if (runTestsOnStart)
            {
                StartCoroutine(RunAllTests());
            }
        }
        
        void Update()
        {
            if (testsRunning)
            {
                UpdateTestMonitoring();
            }
        }
        
        void OnGUI()
        {
            if (enableVisualTests)
            {
                DrawTestGUI();
            }
        }
        #endregion

        #region テスト実行
        /// <summary>
        /// すべてのテストを実行
        /// </summary>
        public IEnumerator RunAllTests()
        {
            Debug.Log("Starting TerrainTexturingSystem tests...");
            testsRunning = true;
            testResults.Reset();
            
            // システム初期化テスト
            yield return StartCoroutine(TestSystemInitialization());
            
            // 高度ベーステクスチャリングテスト
            yield return StartCoroutine(TestAltitudeBasedTexturing());
            
            // 傾斜ベーステクスチャリングテスト
            yield return StartCoroutine(TestSlopeBasedTexturing());
            
            // 動的ブレンディングテスト
            yield return StartCoroutine(TestDynamicBlending());
            
            // LODシステムテスト
            yield return StartCoroutine(TestLODSystem());
            
            // バイオーム統合テスト
            yield return StartCoroutine(TestBiomeIntegration());
            
            // パフォーマンステスト
            if (enablePerformanceTests)
            {
                yield return StartCoroutine(TestPerformance());
            }
            
            // 統合テスト
            if (enableIntegrationTests)
            {
                yield return StartCoroutine(TestSystemIntegration());
            }
            
            testsRunning = false;
            LogTestResults();
            
            Debug.Log("TerrainTexturingSystem tests completed!");
        }
        
        /// <summary>
        /// システム初期化テスト
        /// </summary>
        private IEnumerator TestSystemInitialization()
        {
            Debug.Log("Testing system initialization...");
            
            // TerrainTexturingSystemの初期化テスト
            if (texturingSystem == null)
            {
                texturingSystem = gameObject.AddComponent<TerrainTexturingSystem>();
            }
            
            yield return new WaitForSeconds(0.1f);
            
            bool initSuccess = texturingSystem != null && 
                              texturingSystem.altitudeLayers.Count > 0 && 
                              texturingSystem.slopeLayers.Count > 0;
            
            testResults.systemInitialization = initSuccess;
            Debug.Log($"System initialization: {(initSuccess ? "PASS" : "FAIL")}");
            
            // DynamicMaterialBlendingSystemの初期化テスト
            if (blendingSystem == null)
            {
                blendingSystem = gameObject.AddComponent<DynamicMaterialBlendingSystem>();
            }
            
            yield return new WaitForSeconds(0.1f);
            
            bool blendInitSuccess = blendingSystem != null && blendingSystem.enableDynamicBlending;
            testResults.blendingSystemInitialization = blendInitSuccess;
            Debug.Log($"Blending system initialization: {(blendInitSuccess ? "PASS" : "FAIL")}");
        }
        
        /// <summary>
        /// 高度ベーステクスチャリングテスト
        /// </summary>
        private IEnumerator TestAltitudeBasedTexturing()
        {
            Debug.Log("Testing altitude-based texturing...");
            
            // テスト用タイルを作成
            var testTile = CreateTestTile("AltitudeTest", 0, 0);
            
            // 異なる高度でのテクスチャ適用をテスト
            bool altitudeTestPassed = true;
            
            try
            {
                // 低高度テスト
                SetTestTileHeight(testTile, 10f);
                texturingSystem.ApplyTextureToTile(testTile);
                
                // 中高度テスト
                SetTestTileHeight(testTile, 100f);
                texturingSystem.ApplyTextureToTile(testTile);
                
                // 高高度テスト
                SetTestTileHeight(testTile, 200f);
                texturingSystem.ApplyTextureToTile(testTile);
                
                Debug.Log("Altitude-based texturing: PASS");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Altitude-based texturing failed: {e.Message}");
                altitudeTestPassed = false;
            }
            
            // Wait operations outside try-catch
            yield return new WaitForSeconds(0.1f);
            yield return new WaitForSeconds(0.1f);
            yield return new WaitForSeconds(0.1f);
            
            testResults.altitudeBasedTexturing = altitudeTestPassed;
            CleanupTestTile(testTile);
        }
        
        /// <summary>
        /// 傾斜ベーステクスチャリングテスト
        /// </summary>
        private IEnumerator TestSlopeBasedTexturing()
        {
            Debug.Log("Testing slope-based texturing...");
            
            var testTile = CreateTestTile("SlopeTest", 1, 0);
            bool slopeTestPassed = true;
            
            try
            {
                // 平坦地形テスト
                SetTestTileSlope(testTile, 5f);
                texturingSystem.ApplyTextureToTile(testTile);
                
                // 緩斜面テスト
                SetTestTileSlope(testTile, 25f);
                texturingSystem.ApplyTextureToTile(testTile);
                
                // 急斜面テスト
                SetTestTileSlope(testTile, 50f);
                texturingSystem.ApplyTextureToTile(testTile);
                
                Debug.Log("Slope-based texturing: PASS");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Slope-based texturing failed: {e.Message}");
                slopeTestPassed = false;
            }
            
            // Wait operations outside try-catch
            yield return new WaitForSeconds(0.1f);
            yield return new WaitForSeconds(0.1f);
            yield return new WaitForSeconds(0.1f);
            
            testResults.slopeBasedTexturing = slopeTestPassed;
            CleanupTestTile(testTile);
        }
        
        /// <summary>
        /// 動的ブレンディングテスト
        /// </summary>
        private IEnumerator TestDynamicBlending()
        {
            Debug.Log("Testing dynamic blending...");
            
            var testTile = CreateTestTile("BlendTest", 2, 0);
            bool blendTestPassed = true;
            
            try
            {
                // 距離LODブレンドテスト
                blendingSystem.ApplyDistanceLODBlend(testTile);
                
                // 環境ブレンドテスト
                var conditions = new EnvironmentalConditions
                {
                    temperature = 0.8f,
                    moisture = 0.6f,
                    timeOfDay = 0.5f
                };
                blendingSystem.ApplyEnvironmentalBlend(testTile, conditions);
                
                // 季節ブレンドテスト
                blendingSystem.ApplySeasonalBlend(testTile, Season.Summer);
                
                Debug.Log("Dynamic blending: PASS");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Dynamic blending failed: {e.Message}");
                blendTestPassed = false;
            }
            
            // Wait operations outside try-catch
            yield return new WaitForSeconds(0.5f);
            yield return new WaitForSeconds(0.5f);
            yield return new WaitForSeconds(0.5f);
            
            testResults.dynamicBlending = blendTestPassed;
            CleanupTestTile(testTile);
        }
        
        /// <summary>
        /// LODシステムテスト
        /// </summary>
        private IEnumerator TestLODSystem()
        {
            Debug.Log("Testing LOD system...");
            
            var testTile = CreateTestTile("LODTest", 3, 0);
            bool lodTestPassed = true;
            
            try
            {
                // 異なる距離でのLOD適用をテスト
                testTile.distanceFromPlayer = 100f;
                blendingSystem.ApplyDistanceLODBlend(testTile);
                
                testTile.distanceFromPlayer = 800f;
                blendingSystem.ApplyDistanceLODBlend(testTile);
                
                testTile.distanceFromPlayer = 1500f;
                blendingSystem.ApplyDistanceLODBlend(testTile);
                
                Debug.Log("LOD system: PASS");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"LOD system failed: {e.Message}");
                lodTestPassed = false;
            }
            
            // Wait operations outside try-catch
            yield return new WaitForSeconds(0.2f);
            yield return new WaitForSeconds(0.2f);
            yield return new WaitForSeconds(0.2f);
            
            testResults.lodSystem = lodTestPassed;
            CleanupTestTile(testTile);
        }
        
        /// <summary>
        /// バイオーム統合テスト
        /// </summary>
        private IEnumerator TestBiomeIntegration()
        {
            Debug.Log("Testing biome integration...");
            
            var testTile = CreateTestTile("BiomeTest", 4, 0);
            bool biomeTestPassed = true;
            
            try
            {
                if (testBiomePresets != null && testBiomePresets.Length > 0)
                {
                    foreach (var biomePreset in testBiomePresets)
                    {
                        if (biomePreset != null)
                        {
                            texturingSystem.ApplyBiomeTextures(testTile, biomePreset);
                            blendingSystem.ApplyBiomeBlend(testTile, biomePreset);
                        }
                    }
                }
                else
                {
                    // デフォルトバイオームでテスト
                    var defaultBiome = ScriptableObject.CreateInstance<BiomePreset>();
                    defaultBiome.InitializeDefault();
                    texturingSystem.ApplyBiomeTextures(testTile, defaultBiome);
                }
                
                Debug.Log("Biome integration: PASS");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Biome integration failed: {e.Message}");
                biomeTestPassed = false;
            }
            
            // Wait operations outside try-catch
            if (testBiomePresets != null && testBiomePresets.Length > 0)
            {
                foreach (var biomePreset in testBiomePresets)
                {
                    if (biomePreset != null)
                    {
                        yield return new WaitForSeconds(0.3f);
                    }
                }
            }
            else
            {
                yield return new WaitForSeconds(0.3f);
            }
            
            testResults.biomeIntegration = biomeTestPassed;
            CleanupTestTile(testTile);
        }
        
        /// <summary>
        /// パフォーマンステスト
        /// </summary>
        private IEnumerator TestPerformance()
        {
            Debug.Log("Testing performance...");
            
            float startTime = Time.realtimeSinceStartup;
            
            // 複数タイルでの同時処理テスト
            var performanceTestTiles = new List<TerrainTile>();
            
            for (int i = 0; i < testTileCount; i++)
            {
                var tile = CreateTestTile($"PerfTest_{i}", i, 1);
                performanceTestTiles.Add(tile);
                
                texturingSystem.ApplyTextureToTile(tile);
                blendingSystem.ApplyDistanceLODBlend(tile);
            }
            
            yield return new WaitForSeconds(1f);
            
            float endTime = Time.realtimeSinceStartup;
            float processingTime = endTime - startTime;
            
            bool performanceTestPassed = processingTime < 2f; // 2秒以内で完了
            testResults.performance = performanceTestPassed;
            testResults.processingTime = processingTime;
            
            Debug.Log($"Performance test: {(performanceTestPassed ? "PASS" : "FAIL")} ({processingTime:F3}s)");
            
            // クリーンアップ
            foreach (var tile in performanceTestTiles)
            {
                CleanupTestTile(tile);
            }
        }
        
        /// <summary>
        /// システム統合テスト
        /// </summary>
        private IEnumerator TestSystemIntegration()
        {
            Debug.Log("Testing system integration...");
            
            bool integrationTestPassed = true;
            
            try
            {
                // RuntimeTerrainManagerとの統合テスト
                if (terrainManager != null)
                {
                    // 実際の地形タイルでテスト
                }
                
                // 長時間動作テスト
                float testStartTime = Time.time;
                while (Time.time - testStartTime < testDuration && integrationTestPassed)
                {
                    // システムが正常に動作しているかチェック
                    if (texturingSystem == null || blendingSystem == null)
                    {
                        integrationTestPassed = false;
                        break;
                    }
                }
                
                Debug.Log("System integration: PASS");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"System integration failed: {e.Message}");
                integrationTestPassed = false;
            }
            
            // Wait operations outside try-catch
            yield return new WaitForSeconds(0.5f);
            float waitStartTime = Time.time;
            while (Time.time - waitStartTime < testDuration)
            {
                yield return new WaitForSeconds(1f);
            }
            
            testResults.systemIntegration = integrationTestPassed;
        }
        #endregion

        #region テストヘルパー
        /// <summary>
        /// テスト用タイルを作成
        /// </summary>
        private TerrainTile CreateTestTile(string name, int x, int y)
        {
            var tile = new TerrainTile(new Vector2Int(x, y), 1000f);
            
            // テスト用のハイトマップを作成
            int resolution = 64;
            tile.heightmap = new float[resolution, resolution];
            
            for (int py = 0; py < resolution; py++)
            {
                for (int px = 0; px < resolution; px++)
                {
                    tile.heightmap[py, px] = Mathf.PerlinNoise(px * 0.1f, py * 0.1f);
                }
            }
            
            // テスト用の地形パラメータを設定
            tile.terrainParams = MeshGenerator.TerrainGenerationParams.Default();
            tile.terrainParams.maxHeight = 100f;
            
            // GameObjectを作成
            tile.tileObject = new GameObject($"TestTile_{name}");
            tile.tileObject.transform.position = tile.worldPosition;
            
            var meshFilter = tile.tileObject.AddComponent<MeshFilter>();
            var meshRenderer = tile.tileObject.AddComponent<MeshRenderer>();
            
            // 簡易メッシュを作成
            var mesh = new Mesh();
            mesh.vertices = new Vector3[] { Vector3.zero, Vector3.right, Vector3.forward };
            mesh.triangles = new int[] { 0, 1, 2 };
            mesh.RecalculateNormals();
            
            meshFilter.mesh = mesh;
            tile.terrainMesh = mesh;
            
            // デフォルトマテリアルを設定
            meshRenderer.material = new Material(Shader.Find("Standard"));
            tile.terrainMaterial = meshRenderer.material;
            
            testTiles.Add(tile);
            return tile;
        }
        
        /// <summary>
        /// テストタイルの高度を設定
        /// </summary>
        private void SetTestTileHeight(TerrainTile tile, float height)
        {
            if (tile.heightmap != null)
            {
                int resolution = tile.heightmap.GetLength(0);
                float normalizedHeight = height / tile.terrainParams.maxHeight;
                
                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        tile.heightmap[y, x] = normalizedHeight;
                    }
                }
            }
        }
        
        /// <summary>
        /// テストタイルの傾斜を設定
        /// </summary>
        private void SetTestTileSlope(TerrainTile tile, float slopeDegrees)
        {
            if (tile.heightmap != null)
            {
                int resolution = tile.heightmap.GetLength(0);
                float slopeRadians = slopeDegrees * Mathf.Deg2Rad;
                float slopeHeight = Mathf.Tan(slopeRadians);
                
                for (int y = 0; y < resolution; y++)
                {
                    for (int x = 0; x < resolution; x++)
                    {
                        float normalizedX = (float)x / (resolution - 1);
                        tile.heightmap[y, x] = normalizedX * slopeHeight;
                    }
                }
            }
        }
        
        /// <summary>
        /// テストタイルをクリーンアップ
        /// </summary>
        private void CleanupTestTile(TerrainTile tile)
        {
            if (tile != null)
            {
                if (tile.tileObject != null)
                {
                    DestroyImmediate(tile.tileObject);
                }
                
                texturingSystem.CleanupTextureData(tile);
                testTiles.Remove(tile);
            }
        }
        
        /// <summary>
        /// テスト監視を更新
        /// </summary>
        private void UpdateTestMonitoring()
        {
            // メモリ使用量監視
            long memoryUsage = System.GC.GetTotalMemory(false);
            testResults.memoryUsage = memoryUsage / (1024 * 1024); // MB
            
            // フレームレート監視
            testResults.frameRate = 1f / Time.deltaTime;
        }
        
        /// <summary>
        /// テスト結果をログ出力
        /// </summary>
        private void LogTestResults()
        {
            Debug.Log("=== TerrainTexturingSystem Test Results ===");
            Debug.Log($"System Initialization: {(testResults.systemInitialization ? "PASS" : "FAIL")}");
            Debug.Log($"Blending System Initialization: {(testResults.blendingSystemInitialization ? "PASS" : "FAIL")}");
            Debug.Log($"Altitude-Based Texturing: {(testResults.altitudeBasedTexturing ? "PASS" : "FAIL")}");
            Debug.Log($"Slope-Based Texturing: {(testResults.slopeBasedTexturing ? "PASS" : "FAIL")}");
            Debug.Log($"Dynamic Blending: {(testResults.dynamicBlending ? "PASS" : "FAIL")}");
            Debug.Log($"LOD System: {(testResults.lodSystem ? "PASS" : "FAIL")}");
            Debug.Log($"Biome Integration: {(testResults.biomeIntegration ? "PASS" : "FAIL")}");
            Debug.Log($"Performance: {(testResults.performance ? "PASS" : "FAIL")} ({testResults.processingTime:F3}s)");
            Debug.Log($"System Integration: {(testResults.systemIntegration ? "PASS" : "FAIL")}");
            Debug.Log($"Memory Usage: {testResults.memoryUsage}MB");
            Debug.Log($"Frame Rate: {testResults.frameRate:F1}fps");
            
            int passedTests = 0;
            int totalTests = 9;
            
            if (testResults.systemInitialization) passedTests++;
            if (testResults.blendingSystemInitialization) passedTests++;
            if (testResults.altitudeBasedTexturing) passedTests++;
            if (testResults.slopeBasedTexturing) passedTests++;
            if (testResults.dynamicBlending) passedTests++;
            if (testResults.lodSystem) passedTests++;
            if (testResults.biomeIntegration) passedTests++;
            if (testResults.performance) passedTests++;
            if (testResults.systemIntegration) passedTests++;
            
            Debug.Log($"Overall Result: {passedTests}/{totalTests} tests passed ({(float)passedTests / totalTests * 100:F1}%)");
        }
        
        /// <summary>
        /// テストGUIを描画
        /// </summary>
        private void DrawTestGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.Label("Terrain Texturing System Tests", GUI.skin.box);
            
            if (testsRunning)
            {
                GUILayout.Label("Tests Running...", GUI.skin.box);
            }
            else
            {
                if (GUILayout.Button("Run Tests"))
                {
                    StartCoroutine(RunAllTests());
                }
            }
            
            GUILayout.Space(10);
            
            // テスト結果表示
            GUILayout.Label("Test Results:", GUI.skin.box);
            GUILayout.Label($"System Init: {GetTestStatusText(testResults.systemInitialization)}");
            GUILayout.Label($"Blend Init: {GetTestStatusText(testResults.blendingSystemInitialization)}");
            GUILayout.Label($"Altitude: {GetTestStatusText(testResults.altitudeBasedTexturing)}");
            GUILayout.Label($"Slope: {GetTestStatusText(testResults.slopeBasedTexturing)}");
            GUILayout.Label($"Blending: {GetTestStatusText(testResults.dynamicBlending)}");
            GUILayout.Label($"LOD: {GetTestStatusText(testResults.lodSystem)}");
            GUILayout.Label($"Biome: {GetTestStatusText(testResults.biomeIntegration)}");
            GUILayout.Label($"Performance: {GetTestStatusText(testResults.performance)}");
            GUILayout.Label($"Integration: {GetTestStatusText(testResults.systemIntegration)}");
            
            GUILayout.Space(10);
            GUILayout.Label($"Memory: {testResults.memoryUsage}MB");
            GUILayout.Label($"FPS: {testResults.frameRate:F1}");
            
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// テスト状態テキストを取得
        /// </summary>
        private string GetTestStatusText(bool testResult)
        {
            return testResult ? "PASS" : "FAIL";
        }
        #endregion

        #region テスト結果構造
        [System.Serializable]
        private class TestResults
        {
            public bool systemInitialization = false;
            public bool blendingSystemInitialization = false;
            public bool altitudeBasedTexturing = false;
            public bool slopeBasedTexturing = false;
            public bool dynamicBlending = false;
            public bool lodSystem = false;
            public bool biomeIntegration = false;
            public bool performance = false;
            public bool systemIntegration = false;
            
            public float processingTime = 0f;
            public long memoryUsage = 0;
            public float frameRate = 0f;
            
            public void Reset()
            {
                systemInitialization = false;
                blendingSystemInitialization = false;
                altitudeBasedTexturing = false;
                slopeBasedTexturing = false;
                dynamicBlending = false;
                lodSystem = false;
                biomeIntegration = false;
                performance = false;
                systemIntegration = false;
                
                processingTime = 0f;
                memoryUsage = 0;
                frameRate = 0f;
            }
        }
        #endregion
    }
}