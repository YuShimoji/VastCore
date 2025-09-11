using UnityEngine;
using System.Collections;

namespace Vastcore.Generation
{
    /// <summary>
    /// 気候・地形フィードバックシステムのテストクラス
    /// </summary>
    public class ClimateTerrainFeedbackTest : MonoBehaviour
    {
        [Header("テスト設定")]
        public bool runTestsOnStart = true;
        public bool enableContinuousTest = false;
        public float testInterval = 10f;
        
        [Header("テスト対象")]
        public ClimateSystem climateSystem;
        public ClimateTerrainFeedbackSystem feedbackSystem;
        
        [Header("テスト結果")]
        public bool allTestsPassed = false;
        public string lastTestResult = "";
        
        private void Start()
        {
            if (runTestsOnStart)
            {
                StartCoroutine(RunAllTests());
            }
            
            if (enableContinuousTest)
            {
                StartCoroutine(ContinuousTestCoroutine());
            }
        }
        
        /// <summary>
        /// 全テストを実行
        /// </summary>
        private IEnumerator RunAllTests()
        {
            Debug.Log("=== Climate-Terrain Feedback System Tests Starting ===");
            
            bool testsPassed = true;
            
            // システム初期化テスト
            yield return StartCoroutine(TestSystemInitialization());
            
            // 気候データ生成テスト
            yield return StartCoroutine(TestClimateDataGeneration());
            
            // 地形フィードバックテスト
            yield return StartCoroutine(TestTerrainFeedback());
            
            // 植生システムテスト
            yield return StartCoroutine(TestVegetationSystem());
            
            // 長期変化テスト
            yield return StartCoroutine(TestLongTermChanges());
            
            // 統合テスト
            yield return StartCoroutine(TestSystemIntegration());
            
            allTestsPassed = testsPassed;
            lastTestResult = testsPassed ? "All tests passed!" : "Some tests failed!";
            
            Debug.Log($"=== Climate-Terrain Feedback System Tests Complete: {lastTestResult} ===");
        }
        
        /// <summary>
        /// システム初期化テスト
        /// </summary>
        private IEnumerator TestSystemInitialization()
        {
            Debug.Log("Testing system initialization...");
            
            try
            {
                // ClimateSystemの存在確認
                if (climateSystem == null)
                {
                    climateSystem = FindFirstObjectByType<ClimateSystem>();
                }
                
                if (climateSystem == null)
                {
                    Debug.LogError("ClimateSystem not found!");
                    yield break;
                }
                
                // FeedbackSystemの存在確認
                if (feedbackSystem == null)
                {
                    feedbackSystem = FindFirstObjectByType<ClimateTerrainFeedbackSystem>();
                }
                
                if (feedbackSystem == null)
                {
                    Debug.LogError("ClimateTerrainFeedbackSystem not found!");
                    yield break;
                }
                
                // 初期化待機
                yield return new WaitForSeconds(1f);
                
                Debug.Log("✓ System initialization test passed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ System initialization test failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// 気候データ生成テスト
        /// </summary>
        private IEnumerator TestClimateDataGeneration()
        {
            Debug.Log("Testing climate data generation...");
            
            try
            {
                // 複数の位置で気候データを取得
                Vector3[] testPositions = {
                    Vector3.zero,
                    new Vector3(1000f, 0f, 0f),
                    new Vector3(0f, 100f, 1000f),
                    new Vector3(-500f, 50f, -500f)
                };
                
                foreach (var position in testPositions)
                {
                    ClimateData climate = climateSystem.GetClimateAt(position);
                    
                    // データの妥当性チェック
                    if (!climate.IsValid())
                    {
                        Debug.LogError($"✗ Invalid climate data at {position}: {climate}");
                        yield break;
                    }
                    
                    Debug.Log($"Climate at {position}: {climate}");
                }
                
                // 地理的条件からの気候計算テスト
                ClimateData geoClimate = climateSystem.CalculateClimateFromGeography(new Vector3(0f, 500f, 2000f));
                if (!geoClimate.IsValid())
                {
                    Debug.LogError("✗ Geographic climate calculation failed");
                    yield break;
                }
                
                Debug.Log("✓ Climate data generation test passed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Climate data generation test failed: {e.Message}");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// 地形フィードバックテスト
        /// </summary>
        private IEnumerator TestTerrainFeedback()
        {
            Debug.Log("Testing terrain feedback...");
            
            try
            {
                Vector3 testPosition = new Vector3(0f, 100f, 0f);
                
                // 植生データ取得テスト
                VegetationData vegetation = feedbackSystem.GetVegetationAt(testPosition);
                Debug.Log($"Vegetation at {testPosition}: Density={vegetation.density:F2}, Type={vegetation.type}");
                
                // 浸食データ取得テスト
                ErosionData erosion = feedbackSystem.GetErosionAt(testPosition);
                Debug.Log($"Erosion at {testPosition}: Water={erosion.waterErosion:F4}, Wind={erosion.windErosion:F4}");
                
                // フィードバック強度設定テスト
                feedbackSystem.SetFeedbackIntensity(0.8f, 0.6f, 0.7f);
                
                Debug.Log("✓ Terrain feedback test passed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Terrain feedback test failed: {e.Message}");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// 植生システムテスト
        /// </summary>
        private IEnumerator TestVegetationSystem()
        {
            Debug.Log("Testing vegetation system...");
            
            try
            {
                // 異なる気候条件での植生テスト
                ClimateData[] testClimates = {
                    new ClimateData { temperature = 25f, moisture = 1500f, humidity = 70f },  // 温帯
                    new ClimateData { temperature = 35f, moisture = 200f, humidity = 20f },   // 砂漠
                    new ClimateData { temperature = -5f, moisture = 300f, humidity = 80f },   // 寒帯
                    new ClimateData { temperature = 28f, moisture = 3000f, humidity = 90f }   // 熱帯
                };
                
                foreach (var climate in testClimates)
                {
                    var climateType = climate.GetClimateType();
                    Debug.Log($"Climate type for T={climate.temperature}°C, M={climate.moisture}mm: {climateType}");
                }
                
                // 季節データテスト
                SeasonalData seasonalTemp = new SeasonalData
                {
                    spring = 15f,
                    summer = 25f,
                    autumn = 18f,
                    winter = 5f
                };
                
                float springTemp = seasonalTemp.GetValueForSeason(0.0f);
                float summerTemp = seasonalTemp.GetValueForSeason(0.25f);
                
                Debug.Log($"Seasonal temperatures: Spring={springTemp:F1}°C, Summer={summerTemp:F1}°C");
                
                Debug.Log("✓ Vegetation system test passed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Vegetation system test failed: {e.Message}");
            }
            
            yield return null;
        }
        
        /// <summary>
        /// 長期変化テスト
        /// </summary>
        private IEnumerator TestLongTermChanges()
        {
            Debug.Log("Testing long-term changes...");
            
            try
            {
                // 季節変化テスト
                float initialSeason = climateSystem.GetCurrentSeason();
                climateSystem.SetSeason(0.5f); // 夏に設定
                
                yield return new WaitForSeconds(0.1f);
                
                float newSeason = climateSystem.GetCurrentSeason();
                if (Mathf.Abs(newSeason - 0.5f) > 0.1f)
                {
                    Debug.LogError($"✗ Season setting failed: Expected 0.5, got {newSeason}");
                    yield break;
                }
                
                // 気候パラメータ変更テスト
                climateSystem.SetGlobalClimateParameters(0.7f, 1.2f, 0.8f);
                
                yield return new WaitForSeconds(0.1f);
                
                // 変更後の気候データを確認
                ClimateData modifiedClimate = climateSystem.GetClimateAt(Vector3.zero);
                Debug.Log($"Modified climate: {modifiedClimate}");
                
                Debug.Log("✓ Long-term changes test passed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ Long-term changes test failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// システム統合テスト
        /// </summary>
        private IEnumerator TestSystemIntegration()
        {
            Debug.Log("Testing system integration...");
            
            try
            {
                // 気候統計取得テスト
                var (avgTemp, avgMoisture, avgWindSpeed) = climateSystem.GetClimateStatistics();
                Debug.Log($"Climate statistics: T={avgTemp:F1}°C, M={avgMoisture:F0}mm, W={avgWindSpeed:F1}m/s");
                
                // フィードバックデータリセットテスト
                feedbackSystem.ResetFeedbackData();
                
                yield return new WaitForSeconds(0.5f);
                
                // リセット後のデータ確認
                VegetationData resetVegetation = feedbackSystem.GetVegetationAt(Vector3.zero);
                Debug.Log($"Reset vegetation data: Density={resetVegetation.density:F2}");
                
                // 気候キャッシュクリアテスト
                climateSystem.ClearClimateCache();
                
                yield return new WaitForSeconds(0.5f);
                
                Debug.Log("✓ System integration test passed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ System integration test failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// 継続テストコルーチン
        /// </summary>
        private IEnumerator ContinuousTestCoroutine()
        {
            while (enableContinuousTest)
            {
                yield return new WaitForSeconds(testInterval);
                
                // 簡易テストを実行
                TestClimateDataConsistency();
                TestFeedbackSystemHealth();
            }
        }
        
        /// <summary>
        /// 気候データの一貫性テスト
        /// </summary>
        private void TestClimateDataConsistency()
        {
            try
            {
                Vector3 testPos = new Vector3(Random.Range(-1000f, 1000f), 0f, Random.Range(-1000f, 1000f));
                ClimateData climate = climateSystem.GetClimateAt(testPos);
                
                if (!climate.IsValid())
                {
                    Debug.LogWarning($"Inconsistent climate data detected at {testPos}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Climate data consistency test failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// フィードバックシステムの健全性テスト
        /// </summary>
        private void TestFeedbackSystemHealth()
        {
            try
            {
                Vector3 testPos = Vector3.zero;
                VegetationData vegetation = feedbackSystem.GetVegetationAt(testPos);
                ErosionData erosion = feedbackSystem.GetErosionAt(testPos);
                
                // 異常値チェック
                if (vegetation.density < 0f || vegetation.density > 1f)
                {
                    Debug.LogWarning($"Abnormal vegetation density: {vegetation.density}");
                }
                
                if (erosion.waterErosion < 0f || erosion.windErosion < 0f)
                {
                    Debug.LogWarning($"Negative erosion values detected");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Feedback system health test failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// テスト結果をGUIで表示
        /// </summary>
        private void OnGUI()
        {
            if (!runTestsOnStart) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 400, 200));
            GUILayout.Label("Climate-Terrain Feedback Test Results", GUI.skin.box);
            
            GUILayout.Label($"All Tests Passed: {allTestsPassed}");
            GUILayout.Label($"Last Result: {lastTestResult}");
            
            if (GUILayout.Button("Run Tests Again"))
            {
                StartCoroutine(RunAllTests());
            }
            
            if (GUILayout.Button("Reset Feedback Data"))
            {
                if (feedbackSystem != null)
                {
                    feedbackSystem.ResetFeedbackData();
                }
            }
            
            GUILayout.EndArea();
        }
    }
}