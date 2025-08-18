using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    /// <summary>
    /// BiomeSpecificTerrainGeneratorのテストクラス
    /// 各バイオーム特有の地形生成機能をテスト
    /// </summary>
    public class BiomeSpecificTerrainGeneratorTest : MonoBehaviour
    {
        [Header("テスト設定")]
        public bool runTestsOnStart = true;
        public bool enableVisualTests = true;
        public int testHeightmapSize = 128;
        
        [Header("テスト結果")]
        public List<BiomeTerrainTestResult> testResults = new List<BiomeTerrainTestResult>();
        
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
            
            Debug.Log("=== BiomeSpecificTerrainGenerator Test Started ===");
            
            try
            {
                // 各バイオームタイプのテスト
                TestDesertTerrain();
                TestForestTerrain();
                TestMountainTerrain();
                TestCoastalTerrain();
                TestPolarTerrain();
                TestGrasslandTerrain();
                
                // パフォーマンステスト
                TestPerformance();
                
                testsCompleted = true;
                float testDuration = Time.time - testStartTime;
                
                Debug.Log($"=== BiomeSpecificTerrainGenerator Test Completed in {testDuration:F2}s ===");
                PrintTestSummary();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"BiomeSpecificTerrainGenerator Test Failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// 砂漠地形テスト
        /// </summary>
        private void TestDesertTerrain()
        {
            Debug.Log("Testing Desert terrain generation...");
            
            var result = new BiomeTerrainTestResult
            {
                biomeType = BiomeType.Desert,
                testName = "Desert Terrain Generation"
            };
            
            try
            {
                // テスト用ハイトマップ作成
                float[,] heightmap = CreateTestHeightmap();
                float[,] originalHeightmap = (float[,])heightmap.Clone();
                
                // バイオーム定義作成
                var biomeDefinition = CreateTestBiomeDefinition(BiomeType.Desert);
                
                // 砂漠地形生成
                BiomeSpecificTerrainGenerator.GenerateDesertTerrain(heightmap, biomeDefinition, Vector3.zero);
                
                // 結果検証
                bool hasChanges = !AreHeightmapsEqual(originalHeightmap, heightmap);
                bool hasDesertFeatures = ValidateDesertFeatures(heightmap);
                
                result.success = hasChanges && hasDesertFeatures;
                result.message = hasChanges ? 
                    (hasDesertFeatures ? "Desert features generated successfully" : "Changes detected but no desert-specific features") :
                    "No changes detected in heightmap";
                result.heightmapData = heightmap;
                
                Debug.Log($"Desert Terrain Test: {(result.success ? "PASSED" : "FAILED")} - {result.message}");
            }
            catch (System.Exception e)
            {
                result.success = false;
                result.message = $"Exception: {e.Message}";
                Debug.LogError($"Desert Terrain Test: FAILED - {result.message}");
            }
            
            testResults.Add(result);
        }
        
        /// <summary>
        /// 森林地形テスト
        /// </summary>
        private void TestForestTerrain()
        {
            Debug.Log("Testing Forest terrain generation...");
            
            var result = new BiomeTerrainTestResult
            {
                biomeType = BiomeType.Forest,
                testName = "Forest Terrain Generation"
            };
            
            try
            {
                float[,] heightmap = CreateTestHeightmap();
                float[,] originalHeightmap = (float[,])heightmap.Clone();
                
                var biomeDefinition = CreateTestBiomeDefinition(BiomeType.Forest);
                
                BiomeSpecificTerrainGenerator.GenerateForestTerrain(heightmap, biomeDefinition, Vector3.zero);
                
                bool hasChanges = !AreHeightmapsEqual(originalHeightmap, heightmap);
                bool hasForestFeatures = ValidateForestFeatures(heightmap);
                
                result.success = hasChanges && hasForestFeatures;
                result.message = hasChanges ? 
                    (hasForestFeatures ? "Forest features generated successfully" : "Changes detected but no forest-specific features") :
                    "No changes detected in heightmap";
                result.heightmapData = heightmap;
                
                Debug.Log($"Forest Terrain Test: {(result.success ? "PASSED" : "FAILED")} - {result.message}");
            }
            catch (System.Exception e)
            {
                result.success = false;
                result.message = $"Exception: {e.Message}";
                Debug.LogError($"Forest Terrain Test: FAILED - {result.message}");
            }
            
            testResults.Add(result);
        }
        
        /// <summary>
        /// 山岳地形テスト
        /// </summary>
        private void TestMountainTerrain()
        {
            Debug.Log("Testing Mountain terrain generation...");
            
            var result = new BiomeTerrainTestResult
            {
                biomeType = BiomeType.Mountain,
                testName = "Mountain Terrain Generation"
            };
            
            try
            {
                float[,] heightmap = CreateTestHeightmap();
                float[,] originalHeightmap = (float[,])heightmap.Clone();
                
                var biomeDefinition = CreateTestBiomeDefinition(BiomeType.Mountain);
                
                BiomeSpecificTerrainGenerator.GenerateMountainTerrain(heightmap, biomeDefinition, Vector3.zero);
                
                bool hasChanges = !AreHeightmapsEqual(originalHeightmap, heightmap);
                bool hasMountainFeatures = ValidateMountainFeatures(heightmap);
                
                result.success = hasChanges && hasMountainFeatures;
                result.message = hasChanges ? 
                    (hasMountainFeatures ? "Mountain features generated successfully" : "Changes detected but no mountain-specific features") :
                    "No changes detected in heightmap";
                result.heightmapData = heightmap;
                
                Debug.Log($"Mountain Terrain Test: {(result.success ? "PASSED" : "FAILED")} - {result.message}");
            }
            catch (System.Exception e)
            {
                result.success = false;
                result.message = $"Exception: {e.Message}";
                Debug.LogError($"Mountain Terrain Test: FAILED - {result.message}");
            }
            
            testResults.Add(result);
        }
        
        /// <summary>
        /// 海岸地形テスト
        /// </summary>
        private void TestCoastalTerrain()
        {
            Debug.Log("Testing Coastal terrain generation...");
            
            var result = new BiomeTerrainTestResult
            {
                biomeType = BiomeType.Coastal,
                testName = "Coastal Terrain Generation"
            };
            
            try
            {
                float[,] heightmap = CreateTestHeightmap();
                float[,] originalHeightmap = (float[,])heightmap.Clone();
                
                var biomeDefinition = CreateTestBiomeDefinition(BiomeType.Coastal);
                
                BiomeSpecificTerrainGenerator.GenerateCoastalTerrain(heightmap, biomeDefinition, Vector3.zero);
                
                bool hasChanges = !AreHeightmapsEqual(originalHeightmap, heightmap);
                bool hasCoastalFeatures = ValidateCoastalFeatures(heightmap);
                
                result.success = hasChanges && hasCoastalFeatures;
                result.message = hasChanges ? 
                    (hasCoastalFeatures ? "Coastal features generated successfully" : "Changes detected but no coastal-specific features") :
                    "No changes detected in heightmap";
                result.heightmapData = heightmap;
                
                Debug.Log($"Coastal Terrain Test: {(result.success ? "PASSED" : "FAILED")} - {result.message}");
            }
            catch (System.Exception e)
            {
                result.success = false;
                result.message = $"Exception: {e.Message}";
                Debug.LogError($"Coastal Terrain Test: FAILED - {result.message}");
            }
            
            testResults.Add(result);
        }
        
        /// <summary>
        /// 極地地形テスト
        /// </summary>
        private void TestPolarTerrain()
        {
            Debug.Log("Testing Polar terrain generation...");
            
            var result = new BiomeTerrainTestResult
            {
                biomeType = BiomeType.Polar,
                testName = "Polar Terrain Generation"
            };
            
            try
            {
                float[,] heightmap = CreateTestHeightmap();
                float[,] originalHeightmap = (float[,])heightmap.Clone();
                
                var biomeDefinition = CreateTestBiomeDefinition(BiomeType.Polar);
                
                BiomeSpecificTerrainGenerator.GeneratePolarTerrain(heightmap, biomeDefinition, Vector3.zero);
                
                bool hasChanges = !AreHeightmapsEqual(originalHeightmap, heightmap);
                bool hasPolarFeatures = ValidatePolarFeatures(heightmap);
                
                result.success = hasChanges && hasPolarFeatures;
                result.message = hasChanges ? 
                    (hasPolarFeatures ? "Polar features generated successfully" : "Changes detected but no polar-specific features") :
                    "No changes detected in heightmap";
                result.heightmapData = heightmap;
                
                Debug.Log($"Polar Terrain Test: {(result.success ? "PASSED" : "FAILED")} - {result.message}");
            }
            catch (System.Exception e)
            {
                result.success = false;
                result.message = $"Exception: {e.Message}";
                Debug.LogError($"Polar Terrain Test: FAILED - {result.message}");
            }
            
            testResults.Add(result);
        }
        
        /// <summary>
        /// 草原地形テスト
        /// </summary>
        private void TestGrasslandTerrain()
        {
            Debug.Log("Testing Grassland terrain generation...");
            
            var result = new BiomeTerrainTestResult
            {
                biomeType = BiomeType.Grassland,
                testName = "Grassland Terrain Generation"
            };
            
            try
            {
                float[,] heightmap = CreateTestHeightmap();
                float[,] originalHeightmap = (float[,])heightmap.Clone();
                
                var biomeDefinition = CreateTestBiomeDefinition(BiomeType.Grassland);
                
                BiomeSpecificTerrainGenerator.GenerateGrasslandTerrain(heightmap, biomeDefinition, Vector3.zero);
                
                bool hasChanges = !AreHeightmapsEqual(originalHeightmap, heightmap);
                bool hasGrasslandFeatures = ValidateGrasslandFeatures(heightmap);
                
                result.success = hasChanges && hasGrasslandFeatures;
                result.message = hasChanges ? 
                    (hasGrasslandFeatures ? "Grassland features generated successfully" : "Changes detected but no grassland-specific features") :
                    "No changes detected in heightmap";
                result.heightmapData = heightmap;
                
                Debug.Log($"Grassland Terrain Test: {(result.success ? "PASSED" : "FAILED")} - {result.message}");
            }
            catch (System.Exception e)
            {
                result.success = false;
                result.message = $"Exception: {e.Message}";
                Debug.LogError($"Grassland Terrain Test: FAILED - {result.message}");
            }
            
            testResults.Add(result);
        }
        
        /// <summary>
        /// パフォーマンステスト
        /// </summary>
        private void TestPerformance()
        {
            Debug.Log("Testing performance...");
            
            var result = new BiomeTerrainTestResult
            {
                biomeType = BiomeType.Desert, // 代表としてDesertを使用
                testName = "Performance Test"
            };
            
            try
            {
                int testIterations = 10;
                float totalTime = 0f;
                
                for (int i = 0; i < testIterations; i++)
                {
                    float[,] heightmap = CreateTestHeightmap();
                    var biomeDefinition = CreateTestBiomeDefinition(BiomeType.Desert);
                    
                    float startTime = Time.realtimeSinceStartup;
                    BiomeSpecificTerrainGenerator.GenerateDesertTerrain(heightmap, biomeDefinition, Vector3.zero);
                    float endTime = Time.realtimeSinceStartup;
                    
                    totalTime += (endTime - startTime);
                }
                
                float averageTime = totalTime / testIterations * 1000f; // ms
                
                result.success = averageTime < 100f; // 100ms以下を要求
                result.message = $"Average generation time: {averageTime:F2}ms per call";
                
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
        
        #endregion
        
        #region ヘルパーメソッド
        
        /// <summary>
        /// テスト用ハイトマップを作成
        /// </summary>
        private float[,] CreateTestHeightmap()
        {
            float[,] heightmap = new float[testHeightmapSize, testHeightmapSize];
            
            // 基本的なノイズで初期化
            for (int x = 0; x < testHeightmapSize; x++)
            {
                for (int y = 0; y < testHeightmapSize; y++)
                {
                    heightmap[x, y] = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 50f;
                }
            }
            
            return heightmap;
        }
        
        /// <summary>
        /// テスト用バイオーム定義を作成
        /// </summary>
        private BiomeDefinition CreateTestBiomeDefinition(BiomeType biomeType)
        {
            var definition = new BiomeDefinition
            {
                biomeType = biomeType,
                name = biomeType.ToString(),
                temperatureRange = new Vector2(0f, 30f),
                moistureRange = new Vector2(0f, 1000f),
                elevationRange = new Vector2(0f, 500f),
                terrainModifiers = new TerrainModificationData
                {
                    heightMultiplier = 1f,
                    roughnessMultiplier = 1f,
                    erosionStrength = 0.5f,
                    sedimentationRate = 0.3f
                }
            };
            
            return definition;
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
        /// 砂漠特有の特徴を検証
        /// </summary>
        private bool ValidateDesertFeatures(float[,] heightmap)
        {
            // 砂丘のような起伏があるかチェック
            return HasSignificantVariation(heightmap, 10f);
        }
        
        /// <summary>
        /// 森林特有の特徴を検証
        /// </summary>
        private bool ValidateForestFeatures(float[,] heightmap)
        {
            // 森林の起伏があるかチェック
            return HasSignificantVariation(heightmap, 5f);
        }
        
        /// <summary>
        /// 山岳特有の特徴を検証
        /// </summary>
        private bool ValidateMountainFeatures(float[,] heightmap)
        {
            // 高い山頂があるかチェック
            return HasHighPeaks(heightmap, 100f);
        }
        
        /// <summary>
        /// 海岸特有の特徴を検証
        /// </summary>
        private bool ValidateCoastalFeatures(float[,] heightmap)
        {
            // 海面レベル（0m）付近があるかチェック
            return HasSeaLevel(heightmap);
        }
        
        /// <summary>
        /// 極地特有の特徴を検証
        /// </summary>
        private bool ValidatePolarFeatures(float[,] heightmap)
        {
            // 平滑化された地形があるかチェック
            return HasSmoothTerrain(heightmap);
        }
        
        /// <summary>
        /// 草原特有の特徴を検証
        /// </summary>
        private bool ValidateGrasslandFeatures(float[,] heightmap)
        {
            // なだらかな起伏があるかチェック
            return HasGentleUndulation(heightmap);
        }
        
        /// <summary>
        /// 有意な高度変化があるかチェック
        /// </summary>
        private bool HasSignificantVariation(float[,] heightmap, float threshold)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    minHeight = Mathf.Min(minHeight, heightmap[x, y]);
                    maxHeight = Mathf.Max(maxHeight, heightmap[x, y]);
                }
            }
            
            return (maxHeight - minHeight) > threshold;
        }
        
        /// <summary>
        /// 高い山頂があるかチェック
        /// </summary>
        private bool HasHighPeaks(float[,] heightmap, float threshold)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (heightmap[x, y] > threshold)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 海面レベルがあるかチェック
        /// </summary>
        private bool HasSeaLevel(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (heightmap[x, y] <= 0f)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 平滑化された地形があるかチェック
        /// </summary>
        private bool HasSmoothTerrain(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            float totalVariation = 0f;
            int sampleCount = 0;
            
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float center = heightmap[x, y];
                    float avgNeighbor = (
                        heightmap[x-1, y] + heightmap[x+1, y] +
                        heightmap[x, y-1] + heightmap[x, y+1]
                    ) / 4f;
                    
                    totalVariation += Mathf.Abs(center - avgNeighbor);
                    sampleCount++;
                }
            }
            
            float averageVariation = totalVariation / sampleCount;
            return averageVariation < 5f; // 平滑化されている
        }
        
        /// <summary>
        /// なだらかな起伏があるかチェック
        /// </summary>
        private bool HasGentleUndulation(float[,] heightmap)
        {
            return HasSignificantVariation(heightmap, 20f) && !HasHighPeaks(heightmap, 100f);
        }
        
        /// <summary>
        /// テスト結果を描画
        /// </summary>
        private void DrawTestResults()
        {
            Vector3 basePosition = transform.position;
            float spacing = 200f;
            
            for (int i = 0; i < testResults.Count; i++)
            {
                var result = testResults[i];
                if (result.heightmapData == null) continue;
                
                Vector3 offset = new Vector3((i % 3) * spacing, 0f, (i / 3) * spacing);
                Vector3 centerPos = basePosition + offset;
                
                // バイオームタイプに応じた色
                Color biomeColor = GetBiomeColor(result.biomeType);
                Gizmos.color = result.success ? biomeColor : Color.red;
                
                // ハイトマップの可視化
                int width = result.heightmapData.GetLength(0);
                int height = result.heightmapData.GetLength(1);
                
                for (int x = 0; x < width; x += 4)
                {
                    for (int y = 0; y < height; y += 4)
                    {
                        Vector3 pos = centerPos + new Vector3(x - width/2, result.heightmapData[x, y] * 0.1f, y - height/2);
                        Gizmos.DrawCube(pos, Vector3.one * 2f);
                    }
                }
                
                // テスト名を表示（デバッグ用）
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(centerPos + Vector3.up * 50f, new Vector3(width, 10f, height));
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
                Debug.Log("<color=green>BiomeSpecificTerrainGenerator tests PASSED!</color>");
            }
            else
            {
                Debug.LogWarning("<color=orange>BiomeSpecificTerrainGenerator tests have issues. Check individual test results.</color>");
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
    /// バイオーム地形テスト結果
    /// </summary>
    [System.Serializable]
    public class BiomeTerrainTestResult
    {
        public BiomeType biomeType;
        public string testName;
        public bool success;
        public string message;
        public float[,] heightmapData;
    }
    
    #endregion
}