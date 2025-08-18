using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Vastcore.Testing
{
    /// <summary>
    /// パフォーマンステストケース
    /// 要求6: 実行時動的生成システムのパフォーマンス検証
    /// </summary>
    public class PerformanceTestCase : ITestCase
    {
        private PerformanceMetrics metrics;
        
        public IEnumerator Execute(VastcoreIntegrationTestManager testManager)
        {
            var logger = testManager.Logger;
            var performanceMonitor = testManager.PerformanceMonitor;
            
            logger.Log("Testing system performance...");
            
            metrics = new PerformanceMetrics();
            
            // パフォーマンス監視開始
            if (performanceMonitor != null)
            {
                performanceMonitor.StartMonitoring();
            }
            
            // 要求6.4: 動的生成の負荷分散
            yield return TestLoadBalancing(testManager, logger);
            
            // 要求6.5: フレームレート維持
            yield return TestFrameRateMaintenance(testManager, logger);
            
            // メモリ使用量テスト
            yield return TestMemoryUsage(testManager, logger);
            
            // 生成時間測定テスト
            yield return TestGenerationTiming(testManager, logger);
            
            // 最終パフォーマンス評価
            EvaluateOverallPerformance(logger);
            
            logger.Log("Performance test completed");
        }
        
        private IEnumerator TestLoadBalancing(VastcoreIntegrationTestManager testManager, TestLogger logger)
        {
            logger.Log("Testing load balancing system...");
            
            var runtimeManager = testManager.RuntimeTerrainManager;
            var testPlayer = testManager.TestPlayer;
            
            if (runtimeManager == null)
            {
                throw new System.Exception("RuntimeTerrainManager required for load balancing test");
            }
            
            // 負荷分散設定を適用
            var settings = new RuntimeTerrainManager.RuntimeTerrainSettings
            {
                maxFrameTimeMs = 16f, // 60FPS維持のため
                maxGenerationPerFrame = 3,
                enableLoadBalancing = true
            };
            
            runtimeManager.UpdateSettings(settings);
            
            Vector3 originalPosition = testPlayer.position;
            float testStartTime = Time.time;
            List<float> frameTimes = new List<float>();
            
            // 高負荷シナリオ：急速な移動
            for (int i = 0; i < 20; i++)
            {
                float angle = i * 18f * Mathf.Deg2Rad;
                Vector3 newPosition = originalPosition + new Vector3(
                    Mathf.Cos(angle) * 2000f,
                    0,
                    Mathf.Sin(angle) * 2000f
                );
                
                testPlayer.position = newPosition;
                
                float frameStartTime = Time.realtimeSinceStartup;
                yield return null;
                float frameTime = Time.realtimeSinceStartup - frameStartTime;
                
                frameTimes.Add(frameTime);
                
                // フレーム時間が制限を超えていないかチェック
                if (frameTime > 0.02f) // 50FPS以下
                {
                    logger.LogWarning($"Frame time exceeded limit: {frameTime * 1000f:F1}ms");
                }
            }
            
            testPlayer.position = originalPosition;
            
            // 負荷分散の効果を評価
            float averageFrameTime = 0f;
            foreach (var frameTime in frameTimes)
            {
                averageFrameTime += frameTime;
            }
            averageFrameTime /= frameTimes.Count;
            
            metrics.averageFrameTime = averageFrameTime;
            metrics.maxFrameTime = Mathf.Max(frameTimes.ToArray());
            
            if (averageFrameTime > 0.025f) // 40FPS以下
            {
                throw new System.Exception($"Load balancing insufficient: average frame time {averageFrameTime * 1000f:F1}ms");
            }
            
            logger.Log($"✓ Load balancing test successful: avg {averageFrameTime * 1000f:F1}ms, max {metrics.maxFrameTime * 1000f:F1}ms");
        }
        
        private IEnumerator TestFrameRateMaintenance(VastcoreIntegrationTestManager testManager, TestLogger logger)
        {
            logger.Log("Testing frame rate maintenance...");
            
            var testPlayer = testManager.TestPlayer;
            Vector3 originalPosition = testPlayer.position;
            
            List<float> frameRates = new List<float>();
            float testDuration = 10f;
            float endTime = Time.time + testDuration;
            
            // 連続的な負荷をかけながらフレームレートを監視
            while (Time.time < endTime)
            {
                // ランダムな移動で負荷をかける
                Vector3 randomOffset = new Vector3(
                    Random.Range(-3000f, 3000f),
                    0,
                    Random.Range(-3000f, 3000f)
                );
                
                testPlayer.position = originalPosition + randomOffset;
                
                float currentFrameRate = 1f / Time.deltaTime;
                frameRates.Add(currentFrameRate);
                
                yield return null;
            }
            
            testPlayer.position = originalPosition;
            
            // フレームレート統計を計算
            float averageFrameRate = 0f;
            float minFrameRate = float.MaxValue;
            int lowFrameCount = 0;
            
            foreach (var frameRate in frameRates)
            {
                averageFrameRate += frameRate;
                if (frameRate < minFrameRate)
                    minFrameRate = frameRate;
                if (frameRate < 30f)
                    lowFrameCount++;
            }
            
            averageFrameRate /= frameRates.Count;
            float lowFramePercentage = (float)lowFrameCount / frameRates.Count * 100f;
            
            metrics.averageFrameRate = averageFrameRate;
            metrics.minFrameRate = minFrameRate;
            metrics.lowFramePercentage = lowFramePercentage;
            
            // フレームレート維持の評価
            if (averageFrameRate < 45f)
            {
                throw new System.Exception($"Average frame rate too low: {averageFrameRate:F1}FPS");
            }
            
            if (lowFramePercentage > 10f) // 10%以上が30FPS以下は問題
            {
                throw new System.Exception($"Too many low frame rate instances: {lowFramePercentage:F1}%");
            }
            
            logger.Log($"✓ Frame rate maintenance successful: avg {averageFrameRate:F1}FPS, min {minFrameRate:F1}FPS, low frames {lowFramePercentage:F1}%");
        }
        
        private IEnumerator TestMemoryUsage(VastcoreIntegrationTestManager testManager, TestLogger logger)
        {
            logger.Log("Testing memory usage...");
            
            var testPlayer = testManager.TestPlayer;
            Vector3 originalPosition = testPlayer.position;
            
            // 初期メモリ使用量
            System.GC.Collect();
            yield return new WaitForSeconds(1f);
            long initialMemory = System.GC.GetTotalMemory(false);
            
            // 大量の生成を行う
            for (int i = 0; i < 15; i++)
            {
                testPlayer.position = originalPosition + new Vector3(i * 1500f, 0, i * 1500f);
                yield return new WaitForSeconds(0.5f);
            }
            
            // ピーク時のメモリ使用量
            long peakMemory = System.GC.GetTotalMemory(false);
            
            // 元の位置に戻してクリーンアップ
            testPlayer.position = originalPosition;
            yield return new WaitForSeconds(3f);
            
            // 強制クリーンアップ
            var runtimeManager = testManager.RuntimeTerrainManager;
            var primitiveManager = testManager.PrimitiveTerrainManager;
            
            if (runtimeManager != null)
                runtimeManager.ForceCleanup();
            if (primitiveManager != null)
                primitiveManager.ForceCleanup();
            
            System.GC.Collect();
            yield return new WaitForSeconds(2f);
            
            // 最終メモリ使用量
            long finalMemory = System.GC.GetTotalMemory(false);
            
            // メモリ使用量の評価
            long memoryIncrease = finalMemory - initialMemory;
            long peakIncrease = peakMemory - initialMemory;
            
            metrics.initialMemoryMB = initialMemory / 1024f / 1024f;
            metrics.peakMemoryMB = peakMemory / 1024f / 1024f;
            metrics.finalMemoryMB = finalMemory / 1024f / 1024f;
            metrics.memoryLeakMB = memoryIncrease / 1024f / 1024f;
            
            // メモリリークのチェック
            if (memoryIncrease > peakIncrease * 0.3f) // 30%以上残っている場合は問題
            {
                logger.LogWarning($"Potential memory leak: {metrics.memoryLeakMB:F1}MB increase after cleanup");
            }
            
            // 過度なメモリ使用のチェック
            if (peakIncrease > 1024 * 1024 * 1024) // 1GB以上
            {
                throw new System.Exception($"Excessive memory usage: {peakIncrease / 1024f / 1024f:F1}MB peak increase");
            }
            
            logger.Log($"✓ Memory usage test successful: peak +{peakIncrease / 1024f / 1024f:F1}MB, final +{memoryIncrease / 1024f / 1024f:F1}MB");
        }
        
        private IEnumerator TestGenerationTiming(VastcoreIntegrationTestManager testManager, TestLogger logger)
        {
            logger.Log("Testing generation timing...");
            
            var runtimeManager = testManager.RuntimeTerrainManager;
            var primitiveManager = testManager.PrimitiveTerrainManager;
            var testPlayer = testManager.TestPlayer;
            
            List<float> terrainGenerationTimes = new List<float>();
            List<float> primitiveGenerationTimes = new List<float>();
            
            Vector3 originalPosition = testPlayer.position;
            
            // 地形生成時間の測定
            for (int i = 0; i < 5; i++)
            {
                Vector3 newPosition = originalPosition + new Vector3(i * 2000f, 0, 0);
                testPlayer.position = newPosition;
                
                float startTime = Time.realtimeSinceStartup;
                yield return new WaitForSeconds(2f); // 生成完了まで待機
                float generationTime = Time.realtimeSinceStartup - startTime;
                
                terrainGenerationTimes.Add(generationTime);
            }
            
            // プリミティブ生成時間の測定
            if (primitiveManager != null)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector3 newPosition = originalPosition + new Vector3(0, 0, i * 2000f);
                    testPlayer.position = newPosition;
                    
                    float startTime = Time.realtimeSinceStartup;
                    yield return new WaitForSeconds(1f); // 生成完了まで待機
                    float generationTime = Time.realtimeSinceStartup - startTime;
                    
                    primitiveGenerationTimes.Add(generationTime);
                }
            }
            
            testPlayer.position = originalPosition;
            
            // 生成時間の評価
            float avgTerrainTime = CalculateAverage(terrainGenerationTimes);
            float avgPrimitiveTime = primitiveGenerationTimes.Count > 0 ? CalculateAverage(primitiveGenerationTimes) : 0f;
            
            metrics.averageTerrainGenerationTime = avgTerrainTime;
            metrics.averagePrimitiveGenerationTime = avgPrimitiveTime;
            
            // 生成時間の制限チェック
            if (avgTerrainTime > 3f) // 3秒以上は遅すぎる
            {
                throw new System.Exception($"Terrain generation too slow: {avgTerrainTime:F2}s average");
            }
            
            if (avgPrimitiveTime > 2f) // 2秒以上は遅すぎる
            {
                throw new System.Exception($"Primitive generation too slow: {avgPrimitiveTime:F2}s average");
            }
            
            logger.Log($"✓ Generation timing test successful: terrain {avgTerrainTime:F2}s, primitives {avgPrimitiveTime:F2}s");
        }
        
        private void EvaluateOverallPerformance(TestLogger logger)
        {
            logger.Log("Evaluating overall performance...");
            
            float performanceScore = 100f;
            
            // フレームレートスコア
            if (metrics.averageFrameRate < 60f)
                performanceScore -= (60f - metrics.averageFrameRate) * 0.5f;
            
            // フレーム時間スコア
            if (metrics.averageFrameTime > 0.016f)
                performanceScore -= (metrics.averageFrameTime - 0.016f) * 1000f;
            
            // メモリリークスコア
            if (metrics.memoryLeakMB > 50f)
                performanceScore -= metrics.memoryLeakMB * 0.2f;
            
            // 生成時間スコア
            if (metrics.averageTerrainGenerationTime > 1f)
                performanceScore -= (metrics.averageTerrainGenerationTime - 1f) * 10f;
            
            performanceScore = Mathf.Max(0f, performanceScore);
            
            logger.Log("=== Performance Summary ===");
            logger.Log($"Overall Score: {performanceScore:F1}/100");
            logger.Log($"Average Frame Rate: {metrics.averageFrameRate:F1}FPS");
            logger.Log($"Average Frame Time: {metrics.averageFrameTime * 1000f:F1}ms");
            logger.Log($"Memory Usage: {metrics.peakMemoryMB:F1}MB peak, {metrics.memoryLeakMB:F1}MB leak");
            logger.Log($"Generation Times: Terrain {metrics.averageTerrainGenerationTime:F2}s, Primitives {metrics.averagePrimitiveGenerationTime:F2}s");
            logger.Log("===========================");
            
            if (performanceScore < 70f)
            {
                throw new System.Exception($"Overall performance insufficient: {performanceScore:F1}/100");
            }
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
        
        private class PerformanceMetrics
        {
            public float averageFrameTime;
            public float maxFrameTime;
            public float averageFrameRate;
            public float minFrameRate;
            public float lowFramePercentage;
            public float initialMemoryMB;
            public float peakMemoryMB;
            public float finalMemoryMB;
            public float memoryLeakMB;
            public float averageTerrainGenerationTime;
            public float averagePrimitiveGenerationTime;
        }
    }
}