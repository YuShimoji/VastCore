#if VASTCORE_INTEGRATION_TEST_ENABLED
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vastcore.Generation;

namespace Vastcore.Testing
{
    /// <summary>
    /// メモリ管理システムのテストケース
    /// メモリリーク、オブジェクトプール、ガベージコレクションの検証
    /// </summary>
    public class MemoryManagementTestCase : ITestCase
    {
        private MemoryTestMetrics metrics;
        
        public IEnumerator Execute(VastcoreIntegrationTestManager testManager)
        {
            var logger = testManager.Logger;
            
            logger.Log("Testing memory management system...");
            
            metrics = new MemoryTestMetrics();
            
            // メモリリークテスト
            yield return TestMemoryLeaks(testManager, logger);
            
            // オブジェクトプールテスト
            yield return TestObjectPooling(testManager, logger);
            
            // ガベージコレクション負荷テスト
            yield return TestGarbageCollectionLoad(testManager, logger);
            
            // 長時間実行テスト
            yield return TestLongRunningMemoryStability(testManager, logger);
            
            // メモリ制限テスト
            yield return TestMemoryLimits(testManager, logger);
            
            EvaluateMemoryManagement(logger);
            
            logger.Log("Memory management test completed");
        }
        
        private IEnumerator TestMemoryLeaks(VastcoreIntegrationTestManager testManager, TestLogger logger)
        {
            logger.Log("Testing memory leak detection...");
            
            var testPlayer = testManager.TestPlayer;
            var runtimeManager = testManager.RuntimeTerrainManager;
            var primitiveManager = testManager.PrimitiveTerrainManager;
            
            Vector3 originalPosition = testPlayer.position;
            
            // 初期メモリ状態を記録
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            yield return new WaitForSeconds(1f);
            
            long baselineMemory = System.GC.GetTotalMemory(false);
            metrics.baselineMemory = baselineMemory;
            
            // メモリリークテストサイクル
            for (int cycle = 0; cycle < 5; cycle++)
            {
                logger.Log($"Memory leak test cycle {cycle + 1}/5");
                
                // 大量のオブジェクト生成
                for (int i = 0; i < 10; i++)
                {
                    Vector3 testPosition = originalPosition + new Vector3(
                        Random.Range(-5000f, 5000f),
                        0,
                        Random.Range(-5000f, 5000f)
                    );
                    
                    testPlayer.position = testPosition;
                    yield return new WaitForSeconds(0.5f);
                }
                
                // 元の位置に戻してクリーンアップ
                testPlayer.position = originalPosition;
                yield return new WaitForSeconds(2f);
                
                // 強制クリーンアップ
                if (runtimeManager != null)
                    // runtimeManager.ForceCleanup();
                if (primitiveManager != null)
                    // primitiveManager.ForceCleanup();
                
                // ガベージコレクション実行
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();
                yield return new WaitForSeconds(1f);
                
                // メモリ使用量を記録
                long currentMemory = System.GC.GetTotalMemory(false);
                long memoryIncrease = currentMemory - baselineMemory;
                
                metrics.memoryCycleData.Add(new MemoryCycleData
                {
                    cycle = cycle,
                    memoryUsage = currentMemory,
                    memoryIncrease = memoryIncrease
                });
                
                logger.Log($"Cycle {cycle + 1} memory: {currentMemory / 1024f / 1024f:F1}MB (+{memoryIncrease / 1024f / 1024f:F1}MB)");
            }
            
            // メモリリークの評価
            long finalMemoryIncrease = metrics.memoryCycleData[metrics.memoryCycleData.Count - 1].memoryIncrease;
            float memoryLeakMB = finalMemoryIncrease / 1024f / 1024f;
            
            if (memoryLeakMB > 100f) // 100MB以上の増加は問題
            {
                throw new System.Exception($"Significant memory leak detected: {memoryLeakMB:F1}MB increase");
            }
            
            // メモリ増加の傾向をチェック
            bool hasIncreasingTrend = CheckIncreasingMemoryTrend();
            if (hasIncreasingTrend)
            {
                logger.LogWarning("Increasing memory usage trend detected - potential memory leak");
            }
            
            logger.Log($"✓ Memory leak test completed: final increase {memoryLeakMB:F1}MB");
        }
        
        private IEnumerator TestObjectPooling(VastcoreIntegrationTestManager testManager, TestLogger logger)
        {
            logger.Log("Testing object pooling system...");
            
            var primitiveManager = testManager.PrimitiveTerrainManager;
            if (primitiveManager == null)
            {
                logger.LogWarning("PrimitiveTerrainManager not available, skipping object pooling test");
                yield break;
            }
            
            // オブジェクトプールの取得
            // var objectPool = primitiveManager.GetComponent<PrimitiveTerrainObjectPool>();
            object objectPool = null; // TODO: PrimitiveTerrainObjectPool not implemented
            if (objectPool == null)
            {
                logger.LogWarning("PrimitiveTerrainObjectPool not found, skipping pooling test");
                yield break;
            }
            
            // int initialPoolSize = objectPool.GetPoolSize();
            // int initialActiveCount = objectPool.GetActiveObjectCount();
            int initialPoolSize = 0;
            int initialActiveCount = 0;
            
            // プールからオブジェクトを大量取得
            List<GameObject> borrowedObjects = new List<GameObject>();
            
            for (int i = 0; i < 20; i++)
            {
                GameObject obj = null; // TODO: GetPooledObject not implemented
                if (obj != null)
                {
                    borrowedObjects.Add(obj);
                    obj.SetActive(true);
                }
                yield return null;
            }
            
            // int midTestActiveCount = objectPool.GetActiveObjectCount();
            int midTestActiveCount = 0;
            
            // オブジェクトをプールに返却
            foreach (var obj in borrowedObjects)
            {
                // objectPool.ReturnToPool(obj);
                yield return null;
            }
            
            yield return new WaitForSeconds(1f);
            
            // int finalActiveCount = objectPool.GetActiveObjectCount();
            // int finalPoolSize = objectPool.GetPoolSize();
            int finalActiveCount = 0;
            int finalPoolSize = 0;
            
            // プールの動作を検証
            if (midTestActiveCount <= initialActiveCount)
            {
                throw new System.Exception("Object pool not providing objects correctly");
            }
            
            if (finalActiveCount > initialActiveCount + 5) // 多少の誤差は許容
            {
                throw new System.Exception($"Objects not returned to pool correctly: {finalActiveCount} vs {initialActiveCount}");
            }
            
            // プールサイズの適切な管理
            if (finalPoolSize < initialPoolSize)
            {
                logger.LogWarning("Pool size decreased unexpectedly");
            }
            
            logger.Log($"✓ Object pooling test successful: borrowed {borrowedObjects.Count}, returned properly");
        }
        
        private IEnumerator TestGarbageCollectionLoad(VastcoreIntegrationTestManager testManager, TestLogger logger)
        {
            logger.Log("Testing garbage collection load...");
            
            var testPlayer = testManager.TestPlayer;
            Vector3 originalPosition = testPlayer.position;
            
            List<float> gcTimes = new List<float>();
            List<int> gcCounts = new List<int>();
            
            // GC統計の初期値
            int initialGCCount = System.GC.CollectionCount(0);
            
            // 高負荷シナリオでGC負荷を測定
            for (int i = 0; i < 10; i++)
            {
                // 大量の一時オブジェクト生成をトリガー
                testPlayer.position = originalPosition + new Vector3(i * 2000f, 0, i * 2000f);
                
                float gcStartTime = Time.realtimeSinceStartup;
                yield return new WaitForSeconds(1f);
                
                // 手動GCを実行して時間を測定
                System.GC.Collect();
                float gcTime = Time.realtimeSinceStartup - gcStartTime;
                
                gcTimes.Add(gcTime);
                gcCounts.Add(System.GC.CollectionCount(0));
                
                yield return null;
            }
            
            testPlayer.position = originalPosition;
            
            // GC負荷の評価
            float averageGCTime = CalculateAverage(gcTimes);
            int totalGCCount = System.GC.CollectionCount(0) - initialGCCount;
            
            metrics.averageGCTime = averageGCTime;
            metrics.totalGCCount = totalGCCount;
            
            // GC時間が長すぎる場合は問題
            if (averageGCTime > 0.1f) // 100ms以上
            {
                logger.LogWarning($"High GC time detected: {averageGCTime * 1000f:F1}ms average");
            }
            
            // GC頻度が高すぎる場合は問題
            if (totalGCCount > 50) // 10回のテストで50回以上のGC
            {
                logger.LogWarning($"High GC frequency: {totalGCCount} collections during test");
            }
            
            logger.Log($"✓ GC load test completed: avg {averageGCTime * 1000f:F1}ms, {totalGCCount} collections");
        }
        
        private IEnumerator TestLongRunningMemoryStability(VastcoreIntegrationTestManager testManager, TestLogger logger)
        {
            logger.Log("Testing long-running memory stability...");
            
            var testPlayer = testManager.TestPlayer;
            Vector3 originalPosition = testPlayer.position;
            
            float testDuration = 30f; // 30秒間のテスト
            float endTime = Time.time + testDuration;
            
            List<long> memorySnapshots = new List<long>();
            
            while (Time.time < endTime)
            {
                // ランダムな移動で継続的な負荷
                Vector3 randomPosition = originalPosition + new Vector3(
                    Random.Range(-3000f, 3000f),
                    0,
                    Random.Range(-3000f, 3000f)
                );
                
                testPlayer.position = randomPosition;
                
                // 定期的にメモリ使用量を記録
                if (memorySnapshots.Count == 0 || Time.time - (memorySnapshots.Count * 2f) >= 2f)
                {
                    long currentMemory = System.GC.GetTotalMemory(false);
                    memorySnapshots.Add(currentMemory);
                }
                
                yield return new WaitForSeconds(0.5f);
            }
            
            testPlayer.position = originalPosition;
            
            // メモリ安定性の評価
            if (memorySnapshots.Count < 2)
            {
                throw new System.Exception("Insufficient memory snapshots for stability analysis");
            }
            
            long initialMemory = memorySnapshots[0];
            long finalMemory = memorySnapshots[memorySnapshots.Count - 1];
            long maxMemory = memorySnapshots.Max();
            long minMemory = memorySnapshots.Min();
            
            float memoryVariation = (maxMemory - minMemory) / 1024f / 1024f;
            float memoryGrowth = (finalMemory - initialMemory) / 1024f / 1024f;
            
            metrics.memoryVariationMB = memoryVariation;
            metrics.memoryGrowthMB = memoryGrowth;
            
            // 過度なメモリ変動は問題
            if (memoryVariation > 500f) // 500MB以上の変動
            {
                logger.LogWarning($"High memory variation: {memoryVariation:F1}MB");
            }
            
            // 継続的なメモリ増加は問題
            if (memoryGrowth > 200f) // 200MB以上の増加
            {
                throw new System.Exception($"Excessive memory growth: {memoryGrowth:F1}MB over {testDuration}s");
            }
            
            logger.Log($"✓ Long-running stability test completed: variation {memoryVariation:F1}MB, growth {memoryGrowth:F1}MB");
        }
        
        private IEnumerator TestMemoryLimits(VastcoreIntegrationTestManager testManager, TestLogger logger)
        {
            logger.Log("Testing memory limit enforcement...");
            
            var runtimeManager = testManager.RuntimeTerrainManager;
            if (runtimeManager == null)
            {
                logger.LogWarning("RuntimeTerrainManager not available, skipping memory limit test");
                yield break;
            }
            
            // 低いメモリ制限を設定
            var settings = new RuntimeTerrainManager.RuntimeTerrainSettings
            {
                memoryLimitMB = 200f, // 200MBの制限
                // enableMemoryManagement = true
            };
            
            runtimeManager.UpdateSettings(settings);
            yield return new WaitForSeconds(1f);
            
            var testPlayer = testManager.TestPlayer;
            Vector3 originalPosition = testPlayer.position;
            
            // メモリ制限を超える負荷をかける
            for (int i = 0; i < 20; i++)
            {
                testPlayer.position = originalPosition + new Vector3(i * 1500f, 0, i * 1500f);
                yield return new WaitForSeconds(0.5f);
                
                // メモリ使用量をチェック
                long currentMemory = System.GC.GetTotalMemory(false);
                float currentMemoryMB = currentMemory / 1024f / 1024f;
                
                // 制限を大幅に超えていないかチェック
                if (currentMemoryMB > settings.memoryLimitMB * 1.5f) // 50%のオーバーは許容
                {
                    logger.LogWarning($"Memory limit exceeded: {currentMemoryMB:F1}MB > {settings.memoryLimitMB}MB");
                }
            }
            
            testPlayer.position = originalPosition;
            
            // 統計を確認
            var stats = runtimeManager.GetPerformanceStats();
            
            if (stats.emergencyCleanups == 0)
            {
                logger.LogWarning("No emergency cleanups triggered despite memory pressure");
            }
            else
            {
                logger.Log($"Memory management triggered {stats.emergencyCleanups} emergency cleanups");
            }
            
            logger.Log("✓ Memory limit test completed");
        }
        
        private void EvaluateMemoryManagement(TestLogger logger)
        {
            logger.Log("Evaluating memory management performance...");
            
            float score = 100f;
            
            // メモリリークスコア
            if (metrics.memoryCycleData.Count > 0)
            {
                long finalIncrease = metrics.memoryCycleData[metrics.memoryCycleData.Count - 1].memoryIncrease;
                float leakMB = finalIncrease / 1024f / 1024f;
                if (leakMB > 50f)
                    score -= leakMB * 0.5f;
            }
            
            // GC負荷スコア
            if (metrics.averageGCTime > 0.05f)
                score -= (metrics.averageGCTime - 0.05f) * 1000f;
            
            // メモリ変動スコア
            if (metrics.memoryVariationMB > 300f)
                score -= (metrics.memoryVariationMB - 300f) * 0.1f;
            
            score = Mathf.Max(0f, score);
            
            logger.Log("=== Memory Management Summary ===");
            logger.Log($"Overall Score: {score:F1}/100");
            logger.Log($"Memory Cycles: {metrics.memoryCycleData.Count}");
            logger.Log($"Average GC Time: {metrics.averageGCTime * 1000f:F1}ms");
            logger.Log($"Total GC Count: {metrics.totalGCCount}");
            logger.Log($"Memory Variation: {metrics.memoryVariationMB:F1}MB");
            logger.Log($"Memory Growth: {metrics.memoryGrowthMB:F1}MB");
            logger.Log("=================================");
            
            if (score < 70f)
            {
                throw new System.Exception($"Memory management performance insufficient: {score:F1}/100");
            }
        }
        
        private bool CheckIncreasingMemoryTrend()
        {
            if (metrics.memoryCycleData.Count < 3)
                return false;
            
            int increasingCount = 0;
            for (int i = 1; i < metrics.memoryCycleData.Count; i++)
            {
                if (metrics.memoryCycleData[i].memoryIncrease > metrics.memoryCycleData[i - 1].memoryIncrease)
                {
                    increasingCount++;
                }
            }
            
            return increasingCount >= metrics.memoryCycleData.Count * 0.6f; // 60%以上が増加傾向
        }
        
        private float CalculateAverage(List<float> values)
        {
            if (values.Count == 0) return 0f;
            
            float sum = 0f;
            foreach (var value in values)
            {
                sum += value;
            }
            return sum / values.Count;
        }
        
        private class MemoryTestMetrics
        {
            public long baselineMemory;
            public List<MemoryCycleData> memoryCycleData = new List<MemoryCycleData>();
            public float averageGCTime;
            public int totalGCCount;
            public float memoryVariationMB;
            public float memoryGrowthMB;
        }
        
        private class MemoryCycleData
        {
            public int cycle;
            public long memoryUsage;
            public long memoryIncrease;
        }
    }
}
#endif
