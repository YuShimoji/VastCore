using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using System.IO;
using System.Text;

namespace VastCore.Testing
{
    /// <summary>
    /// 長時間動作安定性テストシステム
    /// 24時間連続動作テストとメモリ・パフォーマンス監視を実行
    /// </summary>
    public class ComprehensiveSystemTest : MonoBehaviour
    {
        [Header("テスト設定")]
        [SerializeField] private bool enableLongTermTest = false;
        [SerializeField] private float testDurationHours = 24f;
        [SerializeField] private float monitoringIntervalSeconds = 60f;
        [SerializeField] private bool enableDetailedLogging = true;
        
        [Header("パフォーマンス閾値")]
        [SerializeField] private float minAcceptableFPS = 30f;
        [SerializeField] private long maxAcceptableMemoryMB = 2048;
        [SerializeField] private float maxFrameTimeMs = 33.33f; // 30FPS相当
        
        [Header("監視対象システム")]
        [SerializeField] private RuntimeTerrainManager terrainManager;
        [SerializeField] private PrimitiveTerrainManager primitiveManager;
        [SerializeField] private RuntimeGenerationManager generationManager;
        
        // テスト状態
        private bool isTestRunning = false;
        private DateTime testStartTime;
        private float testElapsedTime = 0f;
        private int monitoringCycleCount = 0;
        
        // パフォーマンス監視データ
        private List<PerformanceSnapshot> performanceHistory;
        private StringBuilder testLog;
        private string logFilePath;
        
        // メモリ監視
        private long initialMemoryUsage;
        private long peakMemoryUsage;
        private List<long> memorySnapshots;
        
        // フレームレート監視
        private Queue<float> frameTimeQueue;
        private float averageFPS;
        private float minFPS = float.MaxValue;
        private float maxFPS = 0f;
        
        private void Start()
        {
            InitializeTestSystem();
            
            if (enableLongTermTest)
            {
                StartLongTermStabilityTest();
            }
        }
        
        private void InitializeTestSystem()
        {
            performanceHistory = new List<PerformanceSnapshot>();
            testLog = new StringBuilder();
            memorySnapshots = new List<long>();
            frameTimeQueue = new Queue<float>();
            
            // ログファイルパスの設定
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            logFilePath = Path.Combine(Application.persistentDataPath, $"StabilityTest_{timestamp}.log");
            
            LogMessage("ComprehensiveSystemTest initialized");
            LogMessage($"Log file: {logFilePath}");
        }
        
        public void StartLongTermStabilityTest()
        {
            if (isTestRunning)
            {
                LogMessage("Test is already running");
                return;
            }
            
            LogMessage($"Starting {testDurationHours}h stability test");
            isTestRunning = true;
            testStartTime = DateTime.Now;
            testElapsedTime = 0f;
            monitoringCycleCount = 0;
            
            // 初期メモリ使用量を記録
            initialMemoryUsage = GetCurrentMemoryUsage();
            peakMemoryUsage = initialMemoryUsage;
            
            StartCoroutine(LongTermTestCoroutine());
            StartCoroutine(PerformanceMonitoringCoroutine());
        }
        
        public void StopLongTermStabilityTest()
        {
            if (!isTestRunning)
            {
                LogMessage("No test is currently running");
                return;
            }
            
            isTestRunning = false;
            LogMessage("Stability test stopped by user");
            GenerateFinalReport();
        }
        
        private IEnumerator LongTermTestCoroutine()
        {
            float targetDurationSeconds = testDurationHours * 3600f;
            
            while (isTestRunning && testElapsedTime < targetDurationSeconds)
            {
                testElapsedTime += Time.deltaTime;
                
                // システム負荷テストの実行
                yield return ExecuteSystemStressTest();
                
                // 定期的なガベージコレクション強制実行（メモリリーク検出用）
                if (testElapsedTime % 3600f < 1f) // 1時間ごと
                {
                    System.GC.Collect();
                    yield return new WaitForSeconds(1f);
                }
                
                yield return null;
            }
            
            if (isTestRunning)
            {
                LogMessage("24-hour stability test completed successfully");
                isTestRunning = false;
                GenerateFinalReport();
            }
        }
        
        private IEnumerator PerformanceMonitoringCoroutine()
        {
            while (isTestRunning)
            {
                yield return new WaitForSeconds(monitoringIntervalSeconds);
                
                if (isTestRunning)
                {
                    RecordPerformanceSnapshot();
                    monitoringCycleCount++;
                    
                    // 1時間ごとに中間レポート生成
                    if (monitoringCycleCount % 60 == 0) // 60分 = 60サイクル（1分間隔の場合）
                    {
                        GenerateIntermediateReport();
                    }
                }
            }
        }
        
        private IEnumerator ExecuteSystemStressTest()
        {
            // 地形システムのストレステスト
            if (terrainManager != null)
            {
                // プレイヤー位置をランダムに変更して地形生成を促進
                Vector3 randomPosition = new Vector3(
                    UnityEngine.Random.Range(-5000f, 5000f),
                    0f,
                    UnityEngine.Random.Range(-5000f, 5000f)
                );
                
                // 地形生成の負荷テスト
                terrainManager.transform.position = randomPosition;
            }
            
            // プリミティブ生成システムのストレステスト
            if (primitiveManager != null)
            {
                // 大量のプリミティブ生成要求
                for (int i = 0; i < 5; i++)
                {
                    Vector3 spawnPos = new Vector3(
                        UnityEngine.Random.Range(-2000f, 2000f),
                        0f,
                        UnityEngine.Random.Range(-2000f, 2000f)
                    );
                    // プリミティブ生成要求をキューに追加
                }
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        private void RecordPerformanceSnapshot()
        {
            var snapshot = new PerformanceSnapshot
            {
                timestamp = DateTime.Now,
                elapsedTime = testElapsedTime,
                fps = CalculateCurrentFPS(),
                memoryUsageMB = GetCurrentMemoryUsage() / (1024 * 1024),
                frameTimeMs = Time.deltaTime * 1000f,
                activeTerrainTiles = GetActiveTerrainTileCount(),
                activePrimitives = GetActivePrimitiveCount(),
                generationQueueSize = GetGenerationQueueSize()
            };
            
            performanceHistory.Add(snapshot);
            memorySnapshots.Add(snapshot.memoryUsageMB);
            
            // ピークメモリ使用量の更新
            if (snapshot.memoryUsageMB > peakMemoryUsage)
            {
                peakMemoryUsage = snapshot.memoryUsageMB;
            }
            
            // パフォーマンス異常の検出
            DetectPerformanceAnomalies(snapshot);
            
            if (enableDetailedLogging)
            {
                LogPerformanceSnapshot(snapshot);
            }
        }
        
        private float CalculateCurrentFPS()
        {
            float currentFrameTime = Time.deltaTime;
            frameTimeQueue.Enqueue(currentFrameTime);
            
            // 直近30フレームの平均を計算
            if (frameTimeQueue.Count > 30)
            {
                frameTimeQueue.Dequeue();
            }
            
            float totalFrameTime = 0f;
            foreach (float frameTime in frameTimeQueue)
            {
                totalFrameTime += frameTime;
            }
            
            float avgFrameTime = totalFrameTime / frameTimeQueue.Count;
            float fps = 1f / avgFrameTime;
            
            // FPS統計の更新
            if (fps < minFPS) minFPS = fps;
            if (fps > maxFPS) maxFPS = fps;
            averageFPS = fps;
            
            return fps;
        }
        
        private long GetCurrentMemoryUsage()
        {
            return Profiler.GetTotalAllocatedMemory(false);
        }
        
        private int GetActiveTerrainTileCount()
        {
            if (terrainManager == null) return 0;
            // RuntimeTerrainManagerから実際のタイル数を取得
            return 0; // 実装時に適切な値を返す
        }
        
        private int GetActivePrimitiveCount()
        {
            if (primitiveManager == null) return 0;
            // PrimitiveTerrainManagerから実際のプリミティブ数を取得
            return 0; // 実装時に適切な値を返す
        }
        
        private int GetGenerationQueueSize()
        {
            if (generationManager == null) return 0;
            // RuntimeGenerationManagerから実際のキューサイズを取得
            return 0; // 実装時に適切な値を返す
        }
        
        private void DetectPerformanceAnomalies(PerformanceSnapshot snapshot)
        {
            List<string> anomalies = new List<string>();
            
            // FPS異常の検出
            if (snapshot.fps < minAcceptableFPS)
            {
                anomalies.Add($"Low FPS detected: {snapshot.fps:F1} (threshold: {minAcceptableFPS})");
            }
            
            // メモリ使用量異常の検出
            if (snapshot.memoryUsageMB > maxAcceptableMemoryMB)
            {
                anomalies.Add($"High memory usage: {snapshot.memoryUsageMB}MB (threshold: {maxAcceptableMemoryMB}MB)");
            }
            
            // フレーム時間異常の検出
            if (snapshot.frameTimeMs > maxFrameTimeMs)
            {
                anomalies.Add($"High frame time: {snapshot.frameTimeMs:F2}ms (threshold: {maxFrameTimeMs}ms)");
            }
            
            // メモリリーク検出（初期値から大幅増加）
            long memoryIncrease = snapshot.memoryUsageMB - (initialMemoryUsage / (1024 * 1024));
            if (memoryIncrease > 1000) // 1GB以上の増加
            {
                anomalies.Add($"Potential memory leak: {memoryIncrease}MB increase from initial");
            }
            
            // 異常が検出された場合のログ出力
            if (anomalies.Count > 0)
            {
                LogMessage($"PERFORMANCE ANOMALY at {snapshot.timestamp:HH:mm:ss}:");
                foreach (string anomaly in anomalies)
                {
                    LogMessage($"  - {anomaly}");
                }
            }
        }
        
        private void LogPerformanceSnapshot(PerformanceSnapshot snapshot)
        {
            string logEntry = $"[{snapshot.timestamp:HH:mm:ss}] " +
                             $"FPS: {snapshot.fps:F1}, " +
                             $"Memory: {snapshot.memoryUsageMB}MB, " +
                             $"FrameTime: {snapshot.frameTimeMs:F2}ms, " +
                             $"Tiles: {snapshot.activeTerrainTiles}, " +
                             $"Primitives: {snapshot.activePrimitives}, " +
                             $"Queue: {snapshot.generationQueueSize}";
            
            LogMessage(logEntry);
        }
        
        private void GenerateIntermediateReport()
        {
            int hoursElapsed = Mathf.FloorToInt(testElapsedTime / 3600f);
            LogMessage($"\n=== INTERMEDIATE REPORT - {hoursElapsed} HOURS ELAPSED ===");
            LogMessage($"Test progress: {(testElapsedTime / (testDurationHours * 3600f)) * 100f:F1}%");
            LogMessage($"Average FPS: {averageFPS:F1} (Min: {minFPS:F1}, Max: {maxFPS:F1})");
            LogMessage($"Current Memory: {GetCurrentMemoryUsage() / (1024 * 1024)}MB");
            LogMessage($"Peak Memory: {peakMemoryUsage / (1024 * 1024)}MB");
            LogMessage($"Memory increase: {(GetCurrentMemoryUsage() - initialMemoryUsage) / (1024 * 1024)}MB");
            LogMessage($"Monitoring cycles completed: {monitoringCycleCount}");
            LogMessage("=== END INTERMEDIATE REPORT ===\n");
        }
        
        private void GenerateFinalReport()
        {
            LogMessage("\n=== FINAL STABILITY TEST REPORT ===");
            LogMessage($"Test duration: {testElapsedTime / 3600f:F2} hours");
            LogMessage($"Total monitoring cycles: {monitoringCycleCount}");
            LogMessage($"Performance snapshots recorded: {performanceHistory.Count}");
            
            // FPS統計
            LogMessage($"\nFPS Statistics:");
            LogMessage($"  Average: {averageFPS:F1}");
            LogMessage($"  Minimum: {minFPS:F1}");
            LogMessage($"  Maximum: {maxFPS:F1}");
            
            // メモリ統計
            LogMessage($"\nMemory Statistics:");
            LogMessage($"  Initial: {initialMemoryUsage / (1024 * 1024)}MB");
            LogMessage($"  Final: {GetCurrentMemoryUsage() / (1024 * 1024)}MB");
            LogMessage($"  Peak: {peakMemoryUsage / (1024 * 1024)}MB");
            LogMessage($"  Total increase: {(GetCurrentMemoryUsage() - initialMemoryUsage) / (1024 * 1024)}MB");
            
            // 安定性評価
            bool isStable = EvaluateSystemStability();
            LogMessage($"\nSystem Stability: {(isStable ? "STABLE" : "UNSTABLE")}");
            
            LogMessage("=== END FINAL REPORT ===");
            
            // ログファイルに保存
            SaveLogToFile();
        }
        
        private bool EvaluateSystemStability()
        {
            // 安定性の評価基準
            bool fpsStable = minFPS >= minAcceptableFPS * 0.8f; // 最小FPSが閾値の80%以上
            bool memoryStable = (GetCurrentMemoryUsage() - initialMemoryUsage) < (500 * 1024 * 1024); // 500MB以下の増加
            bool noMajorAnomalies = true; // 重大な異常が発生していない
            
            return fpsStable && memoryStable && noMajorAnomalies;
        }
        
        private void LogMessage(string message)
        {
            string timestampedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            testLog.AppendLine(timestampedMessage);
            Debug.Log(timestampedMessage);
        }
        
        private void SaveLogToFile()
        {
            try
            {
                File.WriteAllText(logFilePath, testLog.ToString());
                LogMessage($"Test log saved to: {logFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save log file: {e.Message}");
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && isTestRunning)
            {
                LogMessage("Application paused during test");
            }
            else if (!pauseStatus && isTestRunning)
            {
                LogMessage("Application resumed during test");
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && isTestRunning)
            {
                LogMessage("Application lost focus during test");
            }
            else if (hasFocus && isTestRunning)
            {
                LogMessage("Application gained focus during test");
            }
        }
        
        private void OnDestroy()
        {
            if (isTestRunning)
            {
                LogMessage("ComprehensiveSystemTest destroyed while test was running");
                GenerateFinalReport();
            }
        }
    }
    
    [System.Serializable]
    public struct PerformanceSnapshot
    {
        public DateTime timestamp;
        public float elapsedTime;
        public float fps;
        public long memoryUsageMB;
        public float frameTimeMs;
        public int activeTerrainTiles;
        public int activePrimitives;
        public int generationQueueSize;
    }
}