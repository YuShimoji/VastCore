using UnityEngine;
using System.Collections;
using Vastcore.Generation;

namespace Vastcore.Generation.Tests
{
    /// <summary>
    /// RuntimeTerrainManagerのテストクラス
    /// </summary>
    public class RuntimeTerrainManagerTest : MonoBehaviour
    {
        [Header("テスト設定")]
        public bool runTestOnStart = true;
        public bool enableStressTest = false;
        public float testDuration = 60f;
        
        [Header("プレイヤーシミュレーション")]
        public bool simulatePlayerMovement = true;
        public float movementSpeed = 50f;
        public float movementRadius = 5000f;
        public Transform testPlayer;
        
        [Header("テスト結果")]
        public RuntimeTerrainManager.PerformanceStats lastStats;
        public int totalTestsRun = 0;
        public int testsPasssed = 0;
        public int testsFailed = 0;
        
        private RuntimeTerrainManager runtimeManager;
        private TileManager tileManager;
        private Vector3 initialPlayerPosition;
        private float testStartTime;
        
        void Start()
        {
            if (runTestOnStart)
            {
                StartCoroutine(RunAllTests());
            }
        }
        
        /// <summary>
        /// 全テストを実行
        /// </summary>
        public IEnumerator RunAllTests()
        {
            Debug.Log("=== RuntimeTerrainManager Test Suite Started ===");
            
            bool hasError = false;
            string errorMessage = "";
            
            // 初期化テスト
            yield return StartCoroutine(TestInitializationSafe(result => { hasError = result.hasError; errorMessage = result.errorMessage; }));
            if (hasError) { Debug.LogError($"TestInitialization failed: {errorMessage}"); testsFailed++; }
            
            // 基本機能テスト
            yield return StartCoroutine(TestBasicFunctionalitySafe(result => { hasError = result.hasError; errorMessage = result.errorMessage; }));
            if (hasError) { Debug.LogError($"TestBasicFunctionality failed: {errorMessage}"); testsFailed++; }
            
            // プレイヤー追跡テスト
            yield return StartCoroutine(TestPlayerTrackingSafe(result => { hasError = result.hasError; errorMessage = result.errorMessage; }));
            if (hasError) { Debug.LogError($"TestPlayerTracking failed: {errorMessage}"); testsFailed++; }
            
            // 動的生成テスト
            yield return StartCoroutine(TestDynamicGenerationSafe(result => { hasError = result.hasError; errorMessage = result.errorMessage; }));
            if (hasError) { Debug.LogError($"TestDynamicGeneration failed: {errorMessage}"); testsFailed++; }
            
            // メモリ管理テスト
            yield return StartCoroutine(TestMemoryManagementSafe(result => { hasError = result.hasError; errorMessage = result.errorMessage; }));
            if (hasError) { Debug.LogError($"TestMemoryManagement failed: {errorMessage}"); testsFailed++; }
            
            // パフォーマンステスト
            yield return StartCoroutine(TestPerformanceSafe(result => { hasError = result.hasError; errorMessage = result.errorMessage; }));
            if (hasError) { Debug.LogError($"TestPerformance failed: {errorMessage}"); testsFailed++; }
            
            // ストレステスト（オプション）
            if (enableStressTest)
            {
                yield return StartCoroutine(TestStressConditionsSafe(result => { hasError = result.hasError; errorMessage = result.errorMessage; }));
                if (hasError) { Debug.LogError($"TestStressConditions failed: {errorMessage}"); testsFailed++; }
            }
            
            LogTestResults();
            Debug.Log("=== RuntimeTerrainManager Test Suite Completed ===");
        }
        
        /// <summary>
        /// 初期化テスト
        /// </summary>
        private IEnumerator TestInitialization()
        {
            Debug.Log("Testing initialization...");
            totalTestsRun++;
            
            try
            {
                // RuntimeTerrainManagerを作成
                var testObject = new GameObject("RuntimeTerrainManager_Test");
                runtimeManager = testObject.AddComponent<RuntimeTerrainManager>();
                tileManager = testObject.GetComponent<TileManager>();
                
                // テストプレイヤーを作成
                if (testPlayer == null)
                {
                    var playerObject = new GameObject("TestPlayer");
                    testPlayer = playerObject.transform;
                    testPlayer.position = Vector3.zero;
                }
                
                runtimeManager.playerTransform = testPlayer;
                initialPlayerPosition = testPlayer.position;
                
                yield return new WaitForSeconds(1f); // 初期化待機
                
                Assert(runtimeManager != null, "RuntimeTerrainManager should be created");
                Assert(tileManager != null, "TileManager should be created");
                Assert(runtimeManager.enableDynamicGeneration, "Dynamic generation should be enabled by default");
                
                testsPasssed++;
                Debug.Log("✓ Initialization test passed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Initialization test failed: {e.Message}");
                testsFailed++;
            }
        }
        
        /// <summary>
        /// 基本機能テスト
        /// </summary>
        private IEnumerator TestBasicFunctionality()
        {
            Debug.Log("Testing basic functionality...");
            totalTestsRun++;
            
            try
            {
                // 動的生成の有効/無効化テスト
                runtimeManager.SetDynamicGenerationEnabled(false);
                yield return new WaitForSeconds(0.5f);
                
                runtimeManager.SetDynamicGenerationEnabled(true);
                yield return new WaitForSeconds(0.5f);
                
                // パフォーマンス統計取得テスト
                var stats = runtimeManager.GetPerformanceStats();
                Assert(stats.frameCount >= 0, "Frame count should be non-negative");
                
                // 設定更新テスト
                var settings = new RuntimeTerrainManager.RuntimeTerrainSettings
                {
                    immediateLoadRadius = 2,
                    preloadRadius = 4,
                    keepAliveRadius = 6,
                    forceUnloadRadius = 8,
                    memoryLimitMB = 500f,
                    maxFrameTimeMs = 16f,
                    updateInterval = 0.2f
                };
                
                runtimeManager.UpdateSettings(settings);
                
                testsPasssed++;
                Debug.Log("✓ Basic functionality test passed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Basic functionality test failed: {e.Message}");
                testsFailed++;
            }
        }
        
        /// <summary>
        /// プレイヤー追跡テスト
        /// </summary>
        private IEnumerator TestPlayerTracking()
        {
            Debug.Log("Testing player tracking...");
            totalTestsRun++;
            
            try
            {
                Vector3 startPosition = testPlayer.position;
                
                // プレイヤーを移動
                testPlayer.position = startPosition + Vector3.right * 1000f;
                yield return new WaitForSeconds(2f);
                
                // さらに移動
                testPlayer.position = startPosition + Vector3.forward * 1000f;
                yield return new WaitForSeconds(2f);
                
                // 元の位置に戻す
                testPlayer.position = startPosition;
                yield return new WaitForSeconds(1f);
                
                var stats = runtimeManager.GetPerformanceStats();
                Assert(stats.totalTilesGenerated > 0, "Some tiles should have been generated during movement");
                
                testsPasssed++;
                Debug.Log("✓ Player tracking test passed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Player tracking test failed: {e.Message}");
                testsFailed++;
            }
        }
        
        /// <summary>
        /// 動的生成テスト
        /// </summary>
        private IEnumerator TestDynamicGeneration()
        {
            Debug.Log("Testing dynamic generation...");
            totalTestsRun++;
            
            try
            {
                var initialStats = runtimeManager.GetPerformanceStats();
                int initialTileCount = tileManager.GetActiveTileCount();
                
                // プレイヤーを大きく移動させて動的生成をトリガー
                Vector3 originalPos = testPlayer.position;
                testPlayer.position = originalPos + Vector3.right * 3000f;
                
                yield return new WaitForSeconds(3f);
                
                var newStats = runtimeManager.GetPerformanceStats();
                int newTileCount = tileManager.GetActiveTileCount();
                
                Assert(newStats.totalTilesGenerated > initialStats.totalTilesGenerated, 
                       "New tiles should have been generated");
                
                // 元の位置に戻す
                testPlayer.position = originalPos;
                yield return new WaitForSeconds(2f);
                
                testsPasssed++;
                Debug.Log("✓ Dynamic generation test passed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Dynamic generation test failed: {e.Message}");
                testsFailed++;
            }
        }
        
        /// <summary>
        /// メモリ管理テスト
        /// </summary>
        private IEnumerator TestMemoryManagement()
        {
            Debug.Log("Testing memory management...");
            totalTestsRun++;
            
            try
            {
                // メモリ制限を低く設定
                var settings = new RuntimeTerrainManager.RuntimeTerrainSettings
                {
                    immediateLoadRadius = 2,
                    preloadRadius = 4,
                    keepAliveRadius = 6,
                    forceUnloadRadius = 8,
                    memoryLimitMB = 100f, // 低いメモリ制限
                    maxFrameTimeMs = 16f,
                    updateInterval = 0.1f
                };
                
                runtimeManager.UpdateSettings(settings);
                
                // 大量のタイル生成をトリガー
                Vector3 originalPos = testPlayer.position;
                
                for (int i = 0; i < 10; i++)
                {
                    testPlayer.position = originalPos + new Vector3(i * 1000f, 0, i * 1000f);
                    yield return new WaitForSeconds(0.5f);
                }
                
                var stats = runtimeManager.GetPerformanceStats();
                
                // メモリ制限により削除が発生しているはず
                Assert(stats.totalTilesDeleted > 0, "Some tiles should have been deleted due to memory limits");
                
                // 強制クリーンアップテスト
                runtimeManager.ForceCleanup();
                yield return new WaitForSeconds(1f);
                
                testPlayer.position = originalPos;
                
                testsPasssed++;
                Debug.Log("✓ Memory management test passed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Memory management test failed: {e.Message}");
                testsFailed++;
            }
        }
        
        /// <summary>
        /// パフォーマンステスト
        /// </summary>
        private IEnumerator TestPerformance()
        {
            Debug.Log("Testing performance...");
            totalTestsRun++;
            
            try
            {
                testStartTime = Time.time;
                var initialStats = runtimeManager.GetPerformanceStats();
                
                // 連続的な移動でパフォーマンスをテスト
                Vector3 startPos = testPlayer.position;
                
                for (int i = 0; i < 20; i++)
                {
                    float angle = i * 18f * Mathf.Deg2Rad; // 18度ずつ回転
                    Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 2000f;
                    testPlayer.position = startPos + offset;
                    
                    yield return new WaitForSeconds(0.1f);
                }
                
                var finalStats = runtimeManager.GetPerformanceStats();
                
                Assert(finalStats.averageFrameTime < 0.033f, "Average frame time should be under 33ms");
                Assert(finalStats.generationErrors == 0, "No generation errors should occur");
                Assert(finalStats.deletionErrors == 0, "No deletion errors should occur");
                
                testPlayer.position = startPos;
                
                testsPasssed++;
                Debug.Log("✓ Performance test passed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Performance test failed: {e.Message}");
                testsFailed++;
            }
        }
        
        /// <summary>
        /// ストレステスト
        /// </summary>
        private IEnumerator TestStressConditions()
        {
            Debug.Log("Testing stress conditions...");
            totalTestsRun++;
            
            try
            {
                Debug.Log($"Running stress test for {testDuration} seconds...");
                
                float endTime = Time.time + testDuration;
                Vector3 centerPos = testPlayer.position;
                
                while (Time.time < endTime)
                {
                    // ランダムな移動
                    Vector3 randomOffset = new Vector3(
                        Random.Range(-movementRadius, movementRadius),
                        0,
                        Random.Range(-movementRadius, movementRadius)
                    );
                    
                    testPlayer.position = centerPos + randomOffset;
                    
                    yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
                }
                
                var stats = runtimeManager.GetPerformanceStats();
                
                Assert(stats.totalTilesGenerated > 0, "Tiles should have been generated during stress test");
                Assert(stats.averageFrameTime < 0.05f, "Frame time should remain reasonable under stress");
                
                testPlayer.position = centerPos;
                
                testsPasssed++;
                Debug.Log("✓ Stress test passed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Stress test failed: {e.Message}");
                testsFailed++;
            }
        }
        
        /// <summary>
        /// プレイヤー移動シミュレーション
        /// </summary>
        private IEnumerator SimulatePlayerMovement()
        {
            if (!simulatePlayerMovement || testPlayer == null)
                yield break;
            
            Vector3 centerPos = initialPlayerPosition;
            float time = 0f;
            
            while (Application.isPlaying)
            {
                time += Time.deltaTime;
                
                // 円形の移動パターン
                float angle = time * movementSpeed / movementRadius;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * movementRadius * 0.5f,
                    0,
                    Mathf.Sin(angle) * movementRadius * 0.5f
                );
                
                testPlayer.position = centerPos + offset;
                
                yield return null;
            }
        }
        
        /// <summary>
        /// テスト結果をログ出力
        /// </summary>
        private void LogTestResults()
        {
            lastStats = runtimeManager.GetPerformanceStats();
            
            Debug.Log("=== Test Results ===");
            Debug.Log($"Total Tests: {totalTestsRun}");
            Debug.Log($"Passed: {testsPasssed}");
            Debug.Log($"Failed: {testsFailed}");
            Debug.Log($"Success Rate: {(float)testsPasssed / totalTestsRun * 100f:F1}%");
            Debug.Log("===================");
            
            Debug.Log("=== Performance Stats ===");
            Debug.Log($"Total Tiles Generated: {lastStats.totalTilesGenerated}");
            Debug.Log($"Total Tiles Deleted: {lastStats.totalTilesDeleted}");
            Debug.Log($"Generation Errors: {lastStats.generationErrors}");
            Debug.Log($"Deletion Errors: {lastStats.deletionErrors}");
            Debug.Log($"Emergency Cleanups: {lastStats.emergencyCleanups}");
            Debug.Log($"Memory Usage: {lastStats.currentMemoryUsageMB:F1}MB");
            Debug.Log($"Average Frame Time: {lastStats.averageFrameTime * 1000f:F1}ms");
            Debug.Log("=========================");
        }
        
        /// <summary>
        /// アサーション
        /// </summary>
        private void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new System.Exception($"Assertion failed: {message}");
            }
        }
        
        /// <summary>
        /// 手動テスト実行
        /// </summary>
        [ContextMenu("Run Manual Test")]
        public void RunManualTest()
        {
            StartCoroutine(RunAllTests());
        }
        
        /// <summary>
        /// プレイヤー移動シミュレーション開始
        /// </summary>
        [ContextMenu("Start Player Movement Simulation")]
        public void StartPlayerMovementSimulation()
        {
            StartCoroutine(SimulatePlayerMovement());
        }
        
        /// <summary>
        /// 統計情報をログ出力
        /// </summary>
        [ContextMenu("Log Current Stats")]
        public void LogCurrentStats()
        {
            if (runtimeManager != null)
            {
                runtimeManager.LogPerformanceStats();
            }
        }
        
        void OnGUI()
        {
            if (!showPerformanceStats || runtimeManager == null)
                return;
            
            var stats = runtimeManager.GetPerformanceStats();
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("Runtime Terrain Manager Stats", GUI.skin.box);
            GUILayout.Label($"Tiles Generated: {stats.totalTilesGenerated}");
            GUILayout.Label($"Tiles Deleted: {stats.totalTilesDeleted}");
            GUILayout.Label($"Memory Usage: {stats.currentMemoryUsageMB:F1}MB");
            GUILayout.Label($"Frame Time: {stats.averageFrameTime * 1000f:F1}ms");
            GUILayout.Label($"Tiles/Sec: {stats.tilesPerSecond}");
            GUILayout.Label($"Errors: G{stats.generationErrors} D{stats.deletionErrors}");
            GUILayout.EndArea();
        }
        
        [Header("GUI設定")]
        public bool showPerformanceStats = true;
    }
}