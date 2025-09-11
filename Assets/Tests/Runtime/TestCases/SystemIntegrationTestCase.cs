using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Vastcore.Testing
{
    /// <summary>
    /// システム統合テストケース
    /// 全システムの連携動作と統合機能の検証
    /// </summary>
    public class SystemIntegrationTestCase : ITestCase
    {
        public IEnumerator Execute(VastcoreIntegrationTestManager testManager)
        {
            var logger = testManager.Logger;
            
            logger.Log("Testing system integration...");
            
            // 全システムの初期化確認
            yield return TestSystemInitialization(testManager, logger);
            
            // システム間連携テスト
            yield return TestSystemInteraction(testManager, logger);
            
            // エンドツーエンドシナリオテスト
            yield return TestEndToEndScenarios(testManager, logger);
            
            // システム状態の一貫性テスト
            yield return TestSystemConsistency(testManager, logger);
            
            // 障害回復テスト
            yield return TestFailureRecovery(testManager, logger);
            
            logger.Log("System integration test completed");
        }
        
        private IEnumerator TestSystemInitialization(VastcoreIntegrationTestManager testManager, TestLogger logger)
        {
            logger.Log("Testing system initialization...");
            
            var systems = new Dictionary<string, Component>
            {
                {"RuntimeTerrainManager", testManager.RuntimeTerrainManager},
                {"PrimitiveTerrainManager", testManager.PrimitiveTerrainManager},
                {"BiomePresetManager", testManager.BiomePresetManager},
                {"SliderBasedUISystem", testManager.UISystem},
                {"PerformanceMonitor", testManager.PerformanceMonitor}
            };
            
            List<string> failedSystems = new List<string>();
            
            foreach (var system in systems)
            {
                if (system.Value == null)
                {
                    failedSystems.Add(system.Key);
                    logger.LogWarning($"System not initialized: {system.Key}");
                }
                else
                {
                    // システムの基本機能をテスト
                    bool isWorking = TestSystemBasicFunction(system.Value, logger);
                    if (!isWorking)
                    {
                        failedSystems.Add(system.Key);
                    }
                }
            }
            
            yield return new WaitForSeconds(1f);
            
            if (failedSystems.Count > 0)
            {
                throw new System.Exception($"Failed to initialize systems: {string.Join(", ", failedSystems)}");
            }
            
            logger.Log($"✓ System initialization successful: {systems.Count - failedSystems.Count}/{systems.Count} systems");
        }
        
        private IEnumerator TestSystemInteraction(VastcoreIntegrationTestManager testManager, TestLogger logger)
        {
            logger.Log("Testing system interaction...");
            
            var testPlayer = testManager.TestPlayer;
            var runtimeManager = testManager.RuntimeTerrainManager;
            var primitiveManager = testManager.PrimitiveTerrainManager;
            var uiSystem = testManager.UISystem;
            
            Vector3 originalPosition = testPlayer.position;
            
            // 地形生成とプリミティブ配置の連携テスト
            if (runtimeManager != null && primitiveManager != null)
            {
                yield return TestTerrainPrimitiveInteraction(runtimeManager, primitiveManager, testPlayer, logger);
            }
            
            // UIとシステムの連携テスト
            if (uiSystem != null && runtimeManager != null)
            {
                yield return TestUISystemInteraction(uiSystem, runtimeManager, logger);
            }
            
            // プレイヤー移動とシステム応答の連携テスト
            yield return TestPlayerSystemInteraction(testManager, logger);
            
            testPlayer.position = originalPosition;
            
            logger.Log("✓ System interaction test successful");
        }
        
        private IEnumerator TestTerrainPrimitiveInteraction(
            RuntimeTerrainManager runtimeManager,
            PrimitiveTerrainManager primitiveManager,
            Transform testPlayer,
            TestLogger logger)
        {
            logger.Log("Testing terrain-primitive interaction...");
            
            Vector3 testPosition = testPlayer.position + Vector3.right * 2000f;
            testPlayer.position = testPosition;
            
            yield return new WaitForSeconds(3f); // 両システムの生成を待機
            
            // 地形が生成されているかチェック
            var activeTiles = runtimeManager.GetActiveTiles();
            bool hasTerrainAtPosition = activeTiles.Count > 0;
            
            // プリミティブが配置されているかチェック
            int primitivesNearPosition = primitiveManager.GetPrimitivesInRadius(testPosition, 1000f).Count;
            
            if (!hasTerrainAtPosition)
            {
                throw new System.Exception("Terrain not generated for primitive placement");
            }
            
            if (primitivesNearPosition == 0)
            {
                logger.LogWarning("No primitives placed on generated terrain");
            }
            
            // プリミティブが地形に適切に配置されているかチェック
            var nearbyPrimitives = primitiveManager.GetPrimitivesInRadius(testPosition, 1000f);
            foreach (var primitive in nearbyPrimitives)
            {
                if (primitive.transform.position.y < -100f) // 地形より大幅に下にある
                {
                    logger.LogWarning($"Primitive {primitive.name} may be improperly placed: Y={primitive.transform.position.y}");
                }
            }
            
            logger.Log($"Terrain-primitive interaction: {activeTiles.Count} tiles, {primitivesNearPosition} primitives");
        }
        
        private IEnumerator TestUISystemInteraction(
            SliderBasedUISystem uiSystem,
            RuntimeTerrainManager runtimeManager,
            TestLogger logger)
        {
            logger.Log("Testing UI-system interaction...");
            
            bool parameterChanged = false;
            float newValue = 0f;
            
            // UIスライダーでシステムパラメータを変更
            bool sliderCreated = uiSystem.CreateSliderUI(
                "TerrainScale",
                50f, 200f, 100f,
                (value) => {
                    parameterChanged = true;
                    newValue = value;
                    
                    // 地形システムに反映
                    var settings = runtimeManager.GetCurrentSettings();
                    settings.terrainScale = value;
                    runtimeManager.UpdateSettings(settings);
                }
            );
            
            if (!sliderCreated)
            {
                throw new System.Exception("Failed to create UI slider for system interaction test");
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // スライダー値を変更
            uiSystem.SetSliderValue("TerrainScale", 150f);
            yield return new WaitForSeconds(1f);
            
            // パラメータ変更が反映されているかチェック
            if (!parameterChanged)
            {
                throw new System.Exception("UI parameter change not propagated to system");
            }
            
            if (Mathf.Abs(newValue - 150f) > 0.1f)
            {
                throw new System.Exception($"UI parameter value mismatch: expected 150, got {newValue}");
            }
            
            // システム設定が実際に更新されているかチェック
            var currentSettings = runtimeManager.GetCurrentSettings();
            if (Mathf.Abs(currentSettings.terrainScale - 150f) > 0.1f)
            {
                throw new System.Exception("System settings not updated from UI");
            }
            
            logger.Log("UI-system interaction successful");
        }
        
        private IEnumerator TestPlayerSystemInteraction(VastcoreIntegrationTestManager testManager, TestLogger logger)
        {
            logger.Log("Testing player-system interaction...");
            
            var testPlayer = testManager.TestPlayer;
            var runtimeManager = testManager.RuntimeTerrainManager;
            var primitiveManager = testManager.PrimitiveTerrainManager;
            
            Vector3 originalPosition = testPlayer.position;
            
            // プレイヤー移動による動的生成テスト
            Vector3[] testPositions = {
                originalPosition + Vector3.right * 3000f,
                originalPosition + Vector3.forward * 3000f,
                originalPosition + new Vector3(2000f, 0, 2000f),
                originalPosition
            };
            
            List<int> tileCountsAtPositions = new List<int>();
            List<int> primitiveCountsAtPositions = new List<int>();
            
            foreach (var position in testPositions)
            {
                testPlayer.position = position;
                yield return new WaitForSeconds(2f);
                
                // 各位置でのシステム応答を記録
                int tileCount = runtimeManager != null ? runtimeManager.GetActiveTiles().Count : 0;
                int primitiveCount = primitiveManager != null ? primitiveManager.GetActivePrimitiveCount() : 0;
                
                tileCountsAtPositions.Add(tileCount);
                primitiveCountsAtPositions.Add(primitiveCount);
                
                logger.Log($"Position {position}: {tileCount} tiles, {primitiveCount} primitives");
            }
            
            // システムがプレイヤー移動に適切に応答しているかチェック
            bool systemResponding = false;
            for (int i = 1; i < tileCountsAtPositions.Count; i++)
            {
                if (tileCountsAtPositions[i] != tileCountsAtPositions[0] || 
                    primitiveCountsAtPositions[i] != primitiveCountsAtPositions[0])
                {
                    systemResponding = true;
                    break;
                }
            }
            
            if (!systemResponding)
            {
                logger.LogWarning("Systems may not be responding to player movement");
            }
            
            logger.Log("Player-system interaction test completed");
        }
        
        private IEnumerator TestEndToEndScenarios(VastcoreIntegrationTestManager testManager, TestLogger logger)
        {
            logger.Log("Testing end-to-end scenarios...");
            
            // シナリオ1: 新しいエリアの探索
            yield return TestExplorationScenario(testManager, logger);
            
            // シナリオ2: 設定変更とリアルタイム反映
            yield return TestConfigurationScenario(testManager, logger);
            
            // シナリオ3: 高負荷状況での安定性
            yield return TestHighLoadScenario(testManager, logger);
            
            logger.Log("End-to-end scenarios test completed");
        }
        
        private IEnumerator TestExplorationScenario(VastcoreIntegrationTestManager testManager, TestLogger logger)
        {
            logger.Log("Testing exploration scenario...");
            
            var testPlayer = testManager.TestPlayer;
            Vector3 startPosition = testPlayer.position;
            
            // 探索ルートを定義
            Vector3[] explorationRoute = {
                startPosition,
                startPosition + Vector3.right * 5000f,
                startPosition + new Vector3(5000f, 0, 5000f),
                startPosition + Vector3.forward * 5000f,
                startPosition + new Vector3(-2000f, 0, 3000f),
                startPosition
            };
            
            List<SystemSnapshot> snapshots = new List<SystemSnapshot>();
            
            foreach (var waypoint in explorationRoute)
            {
                testPlayer.position = waypoint;
                yield return new WaitForSeconds(3f); // システム応答を待機
                
                // システム状態のスナップショットを取得
                var snapshot = TakeSystemSnapshot(testManager, waypoint);
                snapshots.Add(snapshot);
                
                logger.Log($"Waypoint {waypoint}: T{snapshot.terrainTiles} P{snapshot.primitives} M{snapshot.memoryMB:F1}MB");
            }
            
            // 探索シナリオの評価
            bool explorationSuccessful = true;
            
            // 各地点で適切なコンテンツが生成されているか
            foreach (var snapshot in snapshots)
            {
                if (snapshot.terrainTiles == 0)
                {
                    logger.LogWarning($"No terrain generated at {snapshot.position}");
                    explorationSuccessful = false;
                }
            }
            
            // メモリ使用量が適切に管理されているか
            float maxMemory = 0f;
            float minMemory = float.MaxValue;
            foreach (var snapshot in snapshots)
            {
                if (snapshot.memoryMB > maxMemory) maxMemory = snapshot.memoryMB;
                if (snapshot.memoryMB < minMemory) minMemory = snapshot.memoryMB;
            }
            
            if (maxMemory - minMemory > 500f) // 500MB以上の変動
            {
                logger.LogWarning($"High memory variation during exploration: {maxMemory - minMemory:F1}MB");
            }
            
            if (!explorationSuccessful)
            {
                throw new System.Exception("Exploration scenario failed");
            }
            
            logger.Log("Exploration scenario successful");
        }
        
        private IEnumerator TestConfigurationScenario(VastcoreIntegrationTestManager testManager, TestLogger logger)
        {
            logger.Log("Testing configuration scenario...");
            
            var runtimeManager = testManager.RuntimeTerrainManager;
            var uiSystem = testManager.UISystem;
            
            if (runtimeManager == null || uiSystem == null)
            {
                logger.LogWarning("Required systems not available for configuration test");
                return;
            }
            
            // 初期設定を記録
            var initialSettings = runtimeManager.GetCurrentSettings();
            
            // UI経由で設定を変更
            var testConfigurations = new[]
            {
                new { name = "HighDetail", scale = 200f, radius = 3 },
                new { name = "Performance", scale = 100f, radius = 2 },
                new { name = "LowMemory", scale = 75f, radius = 1 }
            };
            
            foreach (var config in testConfigurations)
            {
                logger.Log($"Applying configuration: {config.name}");
                
                // UI経由で設定変更
                uiSystem.SetSliderValue("TerrainScale", config.scale);
                uiSystem.SetSliderValue("LoadRadius", config.radius);
                
                yield return new WaitForSeconds(2f);
                
                // 設定が反映されているかチェック
                var currentSettings = runtimeManager.GetCurrentSettings();
                
                if (Mathf.Abs(currentSettings.terrainScale - config.scale) > 0.1f)
                {
                    throw new System.Exception($"Configuration {config.name} not applied correctly");
                }
                
                // システムが新しい設定で動作しているかチェック
                var stats = runtimeManager.GetPerformanceStats();
                logger.Log($"Configuration {config.name} applied: {stats.totalTilesGenerated} tiles generated");
            }
            
            // 元の設定に戻す
            runtimeManager.UpdateSettings(initialSettings);
            
            logger.Log("Configuration scenario successful");
        }
        
        private IEnumerator TestHighLoadScenario(VastcoreIntegrationTestManager testManager, TestLogger logger)
        {
            logger.Log("Testing high load scenario...");
            
            var testPlayer = testManager.TestPlayer;
            Vector3 centerPosition = testPlayer.position;
            
            float testDuration = 15f;
            float endTime = Time.time + testDuration;
            
            List<float> frameTimes = new List<float>();
            int systemErrors = 0;
            
            while (Time.time < endTime)
            {
                // 高速ランダム移動
                Vector3 randomPosition = centerPosition + new Vector3(
                    Random.Range(-4000f, 4000f),
                    0,
                    Random.Range(-4000f, 4000f)
                );
                
                testPlayer.position = randomPosition;
                
                float frameStartTime = Time.realtimeSinceStartup;
                yield return null;
                float frameTime = Time.realtimeSinceStartup - frameStartTime;
                
                frameTimes.Add(frameTime);
                
                // システムエラーをチェック
                try
                {
                    var snapshot = TakeSystemSnapshot(testManager, randomPosition);
                    if (snapshot.hasErrors)
                    {
                        systemErrors++;
                    }
                }
                catch
                {
                    systemErrors++;
                }
                
                yield return new WaitForSeconds(0.1f);
            }
            
            testPlayer.position = centerPosition;
            
            // 高負荷シナリオの評価
            float averageFrameTime = frameTimes.Count > 0 ? frameTimes.Sum() / frameTimes.Count : 0f;
            int lowFrameCount = frameTimes.Count(ft => ft > 0.033f); // 30FPS以下
            
            if (averageFrameTime > 0.025f) // 40FPS以下
            {
                throw new System.Exception($"High load performance insufficient: {averageFrameTime * 1000f:F1}ms average frame time");
            }
            
            if (systemErrors > frameTimes.Count * 0.05f) // 5%以上のエラー
            {
                throw new System.Exception($"Too many system errors under high load: {systemErrors}");
            }
            
            logger.Log($"High load scenario successful: {averageFrameTime * 1000f:F1}ms avg, {systemErrors} errors");
        }
        
        private IEnumerator TestSystemConsistency(VastcoreIntegrationTestManager testManager, TestLogger logger)
        {
            logger.Log("Testing system consistency...");
            
            // 複数回同じ操作を行い、結果の一貫性をチェック
            var testPlayer = testManager.TestPlayer;
            Vector3 testPosition = testPlayer.position + Vector3.right * 2000f;
            
            List<SystemSnapshot> snapshots = new List<SystemSnapshot>();
            
            for (int i = 0; i < 3; i++)
            {
                // 同じ位置に移動
                testPlayer.position = testPosition;
                yield return new WaitForSeconds(3f);
                
                var snapshot = TakeSystemSnapshot(testManager, testPosition);
                snapshots.Add(snapshot);
                
                // 位置をリセット
                testPlayer.position = Vector3.zero;
                yield return new WaitForSeconds(2f);
            }
            
            // 一貫性をチェック
            bool consistent = true;
            var firstSnapshot = snapshots[0];
            
            foreach (var snapshot in snapshots.Skip(1))
            {
                if (Mathf.Abs(snapshot.terrainTiles - firstSnapshot.terrainTiles) > 1 ||
                    Mathf.Abs(snapshot.primitives - firstSnapshot.primitives) > 2)
                {
                    consistent = false;
                    logger.LogWarning($"Inconsistent system behavior: T{snapshot.terrainTiles} vs T{firstSnapshot.terrainTiles}, P{snapshot.primitives} vs P{firstSnapshot.primitives}");
                }
            }
            
            if (!consistent)
            {
                throw new System.Exception("System behavior inconsistent across multiple runs");
            }
            
            logger.Log("System consistency test successful");
        }
        
        private IEnumerator TestFailureRecovery(VastcoreIntegrationTestManager testManager, TestLogger logger)
        {
            logger.Log("Testing failure recovery...");
            
            var runtimeManager = testManager.RuntimeTerrainManager;
            if (runtimeManager == null)
            {
                logger.LogWarning("RuntimeTerrainManager not available for failure recovery test");
                return;
            }
            
            // 意図的にシステムに負荷をかけて障害を誘発
            var extremeSettings = new RuntimeTerrainManager.RuntimeTerrainSettings
            {
                memoryLimitMB = 50f, // 非常に低いメモリ制限
                maxFrameTimeMs = 5f,  // 非常に短いフレーム時間制限
                immediateLoadRadius = 10 // 非常に大きな読み込み範囲
            };
            
            runtimeManager.UpdateSettings(extremeSettings);
            yield return new WaitForSeconds(1f);
            
            var testPlayer = testManager.TestPlayer;
            Vector3 originalPosition = testPlayer.position;
            
            // 高負荷をかける
            for (int i = 0; i < 5; i++)
            {
                testPlayer.position = originalPosition + new Vector3(i * 3000f, 0, i * 3000f);
                yield return new WaitForSeconds(1f);
            }
            
            // システムが回復可能かチェック
            var stats = runtimeManager.GetPerformanceStats();
            
            if (stats.emergencyCleanups == 0)
            {
                logger.LogWarning("No emergency cleanups triggered during stress test");
            }
            
            // 正常な設定に戻す
            var normalSettings = new RuntimeTerrainManager.RuntimeTerrainSettings
            {
                memoryLimitMB = 500f,
                maxFrameTimeMs = 16f,
                immediateLoadRadius = 2
            };
            
            runtimeManager.UpdateSettings(normalSettings);
            testPlayer.position = originalPosition;
            yield return new WaitForSeconds(3f);
            
            // システムが回復したかチェック
            var recoveryStats = runtimeManager.GetPerformanceStats();
            
            if (recoveryStats.generationErrors > stats.generationErrors + 10)
            {
                throw new System.Exception("System failed to recover from stress conditions");
            }
            
            logger.Log($"Failure recovery successful: {stats.emergencyCleanups} cleanups, {recoveryStats.generationErrors - stats.generationErrors} additional errors");
        }
        
        private bool TestSystemBasicFunction(Component system, TestLogger logger)
        {
            try
            {
                // 基本的な機能テスト（システムタイプに応じて）
                if (system is RuntimeTerrainManager rtm)
                {
                    return rtm.GetPerformanceStats() != null;
                }
                else if (system is PrimitiveTerrainManager ptm)
                {
                    return ptm.GetActivePrimitiveCount() >= 0;
                }
                else if (system is SliderBasedUISystem ui)
                {
                    return ui.GetActiveSliders() != null;
                }
                
                return true; // その他のシステムは存在すれば OK
            }
            catch (System.Exception e)
            {
                logger.LogError($"System basic function test failed: {e.Message}");
                return false;
            }
        }
        
        private SystemSnapshot TakeSystemSnapshot(VastcoreIntegrationTestManager testManager, Vector3 position)
        {
            var snapshot = new SystemSnapshot
            {
                position = position,
                timestamp = Time.time,
                memoryMB = System.GC.GetTotalMemory(false) / 1024f / 1024f
            };
            
            try
            {
                if (testManager.RuntimeTerrainManager != null)
                {
                    snapshot.terrainTiles = testManager.RuntimeTerrainManager.GetActiveTiles().Count;
                }
                
                if (testManager.PrimitiveTerrainManager != null)
                {
                    snapshot.primitives = testManager.PrimitiveTerrainManager.GetActivePrimitiveCount();
                }
                
                snapshot.hasErrors = false;
            }
            catch
            {
                snapshot.hasErrors = true;
            }
            
            return snapshot;
        }
        
        private class SystemSnapshot
        {
            public Vector3 position;
            public float timestamp;
            public int terrainTiles;
            public int primitives;
            public float memoryMB;
            public bool hasErrors;
        }
    }
}