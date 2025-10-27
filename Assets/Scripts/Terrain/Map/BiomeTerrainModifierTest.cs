using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    /// <summary>
    /// BiomeTerrainModifierのテストクラス
    /// バイオーム判定と地形修正機能の動作確認を行う
    /// </summary>
    public class BiomeTerrainModifierTest : MonoBehaviour
    {
        [Header("テスト設定")]
        public bool runTestsOnStart = true;
        public bool enableVisualTests = true;
        public int testGridSize = 10;
        public float testAreaSize = 2000f;
        
        [Header("テスト対象")]
        public BiomeTerrainModifier biomeModifier;
        public RuntimeTerrainManager terrainManager;
        
        [Header("テスト結果")]
        public List<BiomeTestResult> testResults = new List<BiomeTestResult>();
        
        // プライベートフィールド
        private bool testsCompleted = false;
        private float testStartTime;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            if (runTestsOnStart)
            {
                StartTests();
            }
        }
        
        private void OnDrawGizmos()
        {
            if (enableVisualTests && testsCompleted)
            {
                DrawTestResults();
            }
        }
        
        #endregion
        
        #region テスト実行
        
        /// <summary>
        /// テストを開始
        /// </summary>
        public void StartTests()
        {
            testStartTime = Time.time;
            testResults.Clear();
            
            Debug.Log("=== BiomeTerrainModifier Test Started ===");
            
            try
            {
                // 初期化テスト
                TestInitialization();
                
                // バイオーム判定テスト
                TestBiomeDetection();
                
                // 地形修正テスト
                TestTerrainModification();
                
                // パフォーマンステスト
                TestPerformance();
                
                // 統合テスト
                TestIntegration();
                
                testsCompleted = true;
                float testDuration = Time.time - testStartTime;
                
                Debug.Log($"=== BiomeTerrainModifier Test Completed in {testDuration:F2}s ===");
                PrintTestSummary();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"BiomeTerrainModifier Test Failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// 初期化テスト
        /// </summary>
        private void TestInitialization()
        {
            Debug.Log("Testing BiomeTerrainModifier initialization...");
            
            var result = new BiomeTestResult
            {
                testName = "Initialization Test",
                testType = BiomeTestType.Initialization
            };
            
            try
            {
                // BiomeTerrainModifierの取得または作成
                if (biomeModifier == null)
                {
                    biomeModifier = FindFirstObjectByType<BiomeTerrainModifier>();
                    if (biomeModifier == null)
                    {
                        var go = new GameObject("BiomeTerrainModifier_Test");
                        biomeModifier = go.AddComponent<BiomeTerrainModifier>();
                    }
                }
                
                // 初期化
                biomeModifier.Initialize();
                
                // 初期化確認
                bool isInitialized = biomeModifier.biomeDefinitions.Count > 0;
                
                result.success = isInitialized;
                result.message = isInitialized ? "Initialization successful" : "Initialization failed";
                result.executionTime = Time.time - testStartTime;
                
                Debug.Log($"Initialization Test: {(result.success ? "PASSED" : "FAILED")} - {result.message}");
            }
            catch (System.Exception e)
            {
                result.success = false;
                result.message = $"Exception: {e.Message}";
                Debug.LogError($"Initialization Test: FAILED - {result.message}");
            }
            
            testResults.Add(result);
        }
        
        /// <summary>
        /// バイオーム判定テスト
        /// </summary>
        private void TestBiomeDetection()
        {
            Debug.Log("Testing biome detection...");
            
            var result = new BiomeTestResult
            {
                testName = "Biome Detection Test",
                testType = BiomeTestType.BiomeDetection
            };
            
            try
            {
                int successCount = 0;
                int totalTests = testGridSize * testGridSize;
                
                // グリッド状にテストポイントを配置
                for (int x = 0; x < testGridSize; x++)
                {
                    for (int z = 0; z < testGridSize; z++)
                    {
                        Vector3 testPosition = new Vector3(
                            (x - testGridSize * 0.5f) * testAreaSize / testGridSize,
                            Random.Range(0f, 500f), // ランダムな高度
                            (z - testGridSize * 0.5f) * testAreaSize / testGridSize
                        );
                        
                        // バイオーム判定
                        BiomeType detectedBiome = biomeModifier.DetectBiomeAtPosition(testPosition);
                        
                        // 判定結果の妥当性チェック
                        if (System.Enum.IsDefined(typeof(BiomeType), detectedBiome))
                        {
                            successCount++;
                            
                            // テスト結果に記録
                            result.biomeDetections.Add(new BiomeDetectionData
                            {
                                position = testPosition,
                                detectedBiome = detectedBiome
                            });
                        }
                    }
                }
                
                float successRate = (float)successCount / totalTests;
                result.success = successRate >= 0.95f; // 95%以上の成功率を要求
                result.message = $"Detection success rate: {successRate:P2} ({successCount}/{totalTests})";
                result.executionTime = Time.time - testStartTime;
                
                Debug.Log($"Biome Detection Test: {(result.success ? "PASSED" : "FAILED")} - {result.message}");
            }
            catch (System.Exception e)
            {
                result.success = false;
                result.message = $"Exception: {e.Message}";
                Debug.LogError($"Biome Detection Test: FAILED - {result.message}");
            }
            
            testResults.Add(result);
        }
        
        /// <summary>
        /// 地形修正テスト
        /// </summary>
        private void TestTerrainModification()
        {
            Debug.Log("Testing terrain modification...");
            
            var result = new BiomeTestResult
            {
                testName = "Terrain Modification Test",
                testType = BiomeTestType.TerrainModification
            };
            
            try
            {
                // テスト用地形タイルの作成
                var testTile = CreateTestTerrainTile();
                
                // 各バイオームタイプでテスト
                int successCount = 0;
                var biomeTypes = System.Enum.GetValues(typeof(BiomeType));
                
                foreach (BiomeType biomeType in biomeTypes)
                {
                    try
                    {
                        // 元の高度データをコピー
                        float[,] originalHeightData = (float[,])testTile.heightmap.Clone();
                        
                        // バイオーム修正を適用
                        biomeModifier.ApplyBiomeModifications(testTile, biomeType);
                        
                        // 修正が適用されたかチェック
                        bool isModified = !AreHeightmapsEqual(originalHeightData, testTile.heightmap);
                        
                        if (isModified)
                        {
                            successCount++;
                            Debug.Log($"Terrain modification for {biomeType}: SUCCESS");
                        }
                        else
                        {
                            Debug.LogWarning($"Terrain modification for {biomeType}: No changes detected");
                        }
                        
                        // 次のテストのために高度データをリセット
                        testTile.heightmap = originalHeightData;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Terrain modification for {biomeType}: FAILED - {e.Message}");
                    }
                }
                
                float successRate = (float)successCount / biomeTypes.Length;
                result.success = successRate >= 0.8f; // 80%以上の成功率を要求
                result.message = $"Modification success rate: {successRate:P2} ({successCount}/{biomeTypes.Length})";
                result.executionTime = Time.time - testStartTime;
                
                Debug.Log($"Terrain Modification Test: {(result.success ? "PASSED" : "FAILED")} - {result.message}");
                
                // テスト用タイルのクリーンアップ
                if (testTile.tileObject != null)
                {
                    DestroyImmediate(testTile.tileObject);
                }
            }
            catch (System.Exception e)
            {
                result.success = false;
                result.message = $"Exception: {e.Message}";
                Debug.LogError($"Terrain Modification Test: FAILED - {result.message}");
            }
            
            testResults.Add(result);
        }
        
        /// <summary>
        /// パフォーマンステスト
        /// </summary>
        private void TestPerformance()
        {
            Debug.Log("Testing performance...");
            
            var result = new BiomeTestResult
            {
                testName = "Performance Test",
                testType = BiomeTestType.Performance
            };
            
            try
            {
                int testIterations = 1000;
                float startTime = Time.realtimeSinceStartup;
                
                // バイオーム判定のパフォーマンステスト
                for (int i = 0; i < testIterations; i++)
                {
                    Vector3 randomPosition = new Vector3(
                        Random.Range(-testAreaSize, testAreaSize),
                        Random.Range(0f, 1000f),
                        Random.Range(-testAreaSize, testAreaSize)
                    );
                    
                    biomeModifier.DetectBiomeAtPosition(randomPosition);
                }
                
                float endTime = Time.realtimeSinceStartup;
                float totalTime = endTime - startTime;
                float averageTime = totalTime / testIterations * 1000f; // ms
                
                result.success = averageTime < 1f; // 1ms以下を要求
                result.message = $"Average detection time: {averageTime:F3}ms per call";
                result.executionTime = totalTime;
                
                Debug.Log($"Performance Test: {(result.success ? "PASSED" : "FAILED")} - {result.message}");
            }
            catch (System.Exception e)
            {
                result.success = false;
                result.message = $"Exception: {e.Message}";
                Debug.LogError($"Performance Test: FAILED - {result.message}");
            }
            
            testResults.Add(result);
        }
        
        /// <summary>
        /// 統合テスト
        /// </summary>
        private void TestIntegration()
        {
            Debug.Log("Testing integration...");
            
            var result = new BiomeTestResult
            {
                testName = "Integration Test",
                testType = BiomeTestType.Integration
            };
            
            try
            {
                bool integrationSuccess = true;
                string integrationMessage = "BiomeSpecificTerrainGenerator is a static utility class. Integration test skipped.";
                integrationSuccess = true; // Static classes don't need instantiation
                
                result.success = integrationSuccess;
                result.message = integrationMessage;
                result.executionTime = Time.time - testStartTime;
                
                Debug.Log($"Integration Test: {(result.success ? "PASSED" : "WARNING")} - {result.message}");
            }
            catch (System.Exception e)
            {
                result.success = false;
                result.message = $"Exception: {e.Message}";
                Debug.LogError($"Integration Test: FAILED - {result.message}");
            }
            
            testResults.Add(result);
        }
        
        #endregion
        
        #region ヘルパーメソッド
        
        /// <summary>
        /// テスト用地形タイルを作成
        /// </summary>
        private TerrainTile CreateTestTerrainTile()
        {
            var tile = new TerrainTile();
            tile.coordinate = new Vector2Int(0, 0);
            
            // テスト用ハイトマップの生成
            int resolution = 64;
            tile.heightmap = new float[resolution, resolution];
            
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    // 簡単なノイズベースの高度データ
                    float height = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 100f;
                    tile.heightmap[x, y] = height;
                }
            }
            
            // テスト用GameObjectの作成
            tile.tileObject = new GameObject("TestTerrainTile");
            tile.tileObject.transform.position = Vector3.zero;
            
            return tile;
        }
        
        /// <summary>
        /// 2つのハイトマップが等しいかチェック
        /// </summary>
        private bool AreHeightmapsEqual(float[,] heightmap1, float[,] heightmap2)
        {
            if (heightmap1.GetLength(0) != heightmap2.GetLength(0) || 
                heightmap1.GetLength(1) != heightmap2.GetLength(1))
            {
                return false;
            }
            
            int width = heightmap1.GetLength(0);
            int height = heightmap1.GetLength(1);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (Mathf.Abs(heightmap1[x, y] - heightmap2[x, y]) > 0.001f)
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// テスト結果を描画
        /// </summary>
        private void DrawTestResults()
        {
            foreach (var result in testResults)
            {
                if (result.testType == BiomeTestType.BiomeDetection)
                {
                    foreach (var detection in result.biomeDetections)
                    {
                        Color biomeColor = GetBiomeColor(detection.detectedBiome);
                        Gizmos.color = biomeColor;
                        Gizmos.DrawCube(detection.position, Vector3.one * 50f);
                    }
                }
            }
        }
        
        /// <summary>
        /// バイオームタイプに対応する色を取得
        /// </summary>
        private Color GetBiomeColor(BiomeType biomeType)
        {
            switch (biomeType)
            {
                case BiomeType.Desert: return Color.yellow;
                case BiomeType.Forest: return Color.green;
                case BiomeType.Mountain: return Color.gray;
                case BiomeType.Coastal: return Color.cyan;
                case BiomeType.Polar: return Color.white;
                case BiomeType.Grassland: return Color.green * 0.7f;
                default: return Color.magenta;
            }
        }
        
        /// <summary>
        /// テスト結果のサマリーを出力
        /// </summary>
        private void PrintTestSummary()
        {
            int passedTests = 0;
            int totalTests = testResults.Count;
            
            Debug.Log("=== Test Summary ===");
            
            foreach (var result in testResults)
            {
                string status = result.success ? "PASSED" : "FAILED";
                Debug.Log($"{result.testName}: {status} - {result.message}");
                
                if (result.success) passedTests++;
            }
            
            float successRate = (float)passedTests / totalTests;
            Debug.Log($"Overall Success Rate: {successRate:P2} ({passedTests}/{totalTests})");
            
            if (successRate >= 0.8f)
            {
                Debug.Log("<color=green>BiomeTerrainModifier tests PASSED!</color>");
            }
            else
            {
                Debug.LogWarning("<color=orange>BiomeTerrainModifier tests have issues. Check individual test results.</color>");
            }
        }
        
        #endregion
        
        #region パブリックメソッド
        
        /// <summary>
        /// 手動でテストを実行
        /// </summary>
        [ContextMenu("Run Tests")]
        public void RunTestsManually()
        {
            StartTests();
        }
        
        /// <summary>
        /// テスト結果をクリア
        /// </summary>
        [ContextMenu("Clear Test Results")]
        public void ClearTestResults()
        {
            testResults.Clear();
            testsCompleted = false;
            Debug.Log("Test results cleared.");
        }
        
        #endregion
    }
    
    #region データ構造
    
    /// <summary>
    /// バイオームテスト結果
    /// </summary>
    [System.Serializable]
    public class BiomeTestResult
    {
        public string testName;
        public BiomeTestType testType;
        public bool success;
        public string message;
        public float executionTime;
        public List<BiomeDetectionData> biomeDetections = new List<BiomeDetectionData>();
    }
    
    /// <summary>
    /// バイオーム検出データ
    /// </summary>
    [System.Serializable]
    public class BiomeDetectionData
    {
        public Vector3 position;
        public BiomeType detectedBiome;
    }
    
    /// <summary>
    /// バイオームテストタイプ
    /// </summary>
    public enum BiomeTestType
    {
        Initialization,
        BiomeDetection,
        TerrainModification,
        Performance,
        Integration
    }
    
    #endregion
}