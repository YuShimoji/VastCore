#if VASTCORE_PERFORMANCE_TESTING_ENABLED
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace Vastcore.Testing
{
    /// <summary>
    /// パフォーマンス測定システム
    /// フレームレート、メモリ使用量、生成時間の測定とログ出力
    /// 要求6.5: パフォーマンス測定とログ出力の実装
    /// </summary>
    public class PerformanceTestingSystem : MonoBehaviour
    {
        [Header("測定設定")]
        [SerializeField] private bool enableContinuousMonitoring = true;
        [SerializeField] private float measurementInterval = 1f;
        [SerializeField] private int maxDataPoints = 1000;
        [SerializeField] private bool enableDetailedLogging = true;
        
        [Header("フレームレート監視")]
        [SerializeField] private bool monitorFrameRate = true;
        [SerializeField] private float frameRateWarningThreshold = 30f;
        [SerializeField] private float frameRateCriticalThreshold = 20f;
        
        [Header("メモリ監視")]
        [SerializeField] private bool monitorMemoryUsage = true;
        [SerializeField] private float memoryWarningThresholdMB = 800f;
        [SerializeField] private float memoryCriticalThresholdMB = 1200f;
        
        [Header("生成時間監視")]
        [SerializeField] private bool monitorGenerationTimes = true;
        [SerializeField] private float generationTimeWarningThreshold = 2f;
        [SerializeField] private float generationTimeCriticalThreshold = 5f;
        
        [Header("ログ出力")]
        [SerializeField] private bool saveLogsToFile = true;
        [SerializeField] private string logDirectory = "PerformanceLogs";
        [SerializeField] private bool enableCSVExport = true;
        
        // パフォーマンスデータ
        private List<PerformanceDataPoint> performanceData = new List<PerformanceDataPoint>();
        private PerformanceMetrics currentMetrics = new PerformanceMetrics();
        private PerformanceMetrics sessionMetrics = new PerformanceMetrics();
        
        // 測定状態
        private bool isMonitoring = false;
        private float lastMeasurementTime = 0f;
        private Coroutine monitoringCoroutine;
        
        // フレームレート計算用
        private Queue<float> frameTimeQueue = new Queue<float>();
        private float frameTimeSum = 0f;
        
        // メモリ測定用
        private long lastMemoryMeasurement = 0L;
        private List<long> memorySnapshots = new List<long>();
        
        // 生成時間測定用
        private Dictionary<string, float> activeGenerationTasks = new Dictionary<string, float>();
        private List<GenerationTimeData> generationTimes = new List<GenerationTimeData>();
        
        void Start()
        {
            InitializePerformanceSystem();
            
            if (enableContinuousMonitoring)
            {
                StartMonitoring();
            }
        }
        
        void Update()
        {
            if (isMonitoring)
            {
                UpdateFrameRateCalculation();
                CheckPerformanceThresholds();
            }
        }
        
        void OnDestroy()
        {
            StopMonitoring();
            
            if (saveLogsToFile)
            {
                SavePerformanceLogsToFile();
            }
        }
        
        /// <summary>
        /// パフォーマンスシステムの初期化
        /// </summary>
        private void InitializePerformanceSystem()
        {
            // ログディレクトリの作成
            if (saveLogsToFile)
            {
                string fullLogPath = Path.Combine(Application.persistentDataPath, logDirectory);
                if (!Directory.Exists(fullLogPath))
                {
                    Directory.CreateDirectory(fullLogPath);
                }
            }
            
            // 初期メトリクスの設定
            currentMetrics.Reset();
            sessionMetrics.Reset();
            sessionMetrics.sessionStartTime = Time.time;
            
            Debug.Log("Performance Testing System initialized");
        }
        
        /// <summary>
        /// パフォーマンス監視開始
        /// </summary>
        public void StartMonitoring()
        {
            if (isMonitoring)
            {
                Debug.LogWarning("Performance monitoring already running");
                return;
            }
            
            isMonitoring = true;
            lastMeasurementTime = Time.time;
            
            monitoringCoroutine = StartCoroutine(PerformanceMonitoringLoop());
            
            Debug.Log("Performance monitoring started");
        }
        
        /// <summary>
        /// パフォーマンス監視停止
        /// </summary>
        public void StopMonitoring()
        {
            if (!isMonitoring)
                return;
            
            isMonitoring = false;
            
            if (monitoringCoroutine != null)
            {
                StopCoroutine(monitoringCoroutine);
                monitoringCoroutine = null;
            }
            
            sessionMetrics.sessionEndTime = Time.time;
            sessionMetrics.totalSessionTime = sessionMetrics.sessionEndTime - sessionMetrics.sessionStartTime;
            
            Debug.Log("Performance monitoring stopped");
        }
        
        /// <summary>
        /// パフォーマンス監視ループ
        /// </summary>
        private IEnumerator PerformanceMonitoringLoop()
        {
            while (isMonitoring)
            {
                yield return new WaitForSeconds(measurementInterval);
                
                if (Time.time - lastMeasurementTime >= measurementInterval)
                {
                    CollectPerformanceData();
                    lastMeasurementTime = Time.time;
                }
            }
        }
        
        /// <summary>
        /// パフォーマンスデータの収集
        /// </summary>
        private void CollectPerformanceData()
        {
            var dataPoint = new PerformanceDataPoint
            {
                timestamp = Time.time,
                frameRate = GetCurrentFrameRate(),
                frameTime = Time.deltaTime,
                memoryUsageMB = GetCurrentMemoryUsageMB(),
                activeObjects = GetActiveObjectCount(),
                drawCalls = GetDrawCallCount()
            };
            
            // 生成時間データの追加
            if (generationTimes.Count > 0)
            {
                var recentGenerations = generationTimes.FindAll(gt => Time.time - gt.timestamp < measurementInterval);
                if (recentGenerations.Count > 0)
                {
                    float totalTime = 0f;
                    foreach (var gen in recentGenerations)
                    {
                        totalTime += gen.generationTime;
                    }
                    dataPoint.averageGenerationTime = totalTime / recentGenerations.Count;
                }
            }
            
            // データポイントを追加
            performanceData.Add(dataPoint);
            
            // データポイント数の制限
            if (performanceData.Count > maxDataPoints)
            {
                performanceData.RemoveAt(0);
            }
            
            // メトリクスの更新
            UpdateMetrics(dataPoint);
            
            // 詳細ログ出力
            if (enableDetailedLogging)
            {
                LogPerformanceData(dataPoint);
            }
        }
        
        /// <summary>
        /// フレームレート計算の更新
        /// </summary>
        private void UpdateFrameRateCalculation()
        {
            float currentFrameTime = Time.deltaTime;
            
            frameTimeQueue.Enqueue(currentFrameTime);
            frameTimeSum += currentFrameTime;
            
            // 1秒間のフレーム時間を保持
            while (frameTimeQueue.Count > 0 && frameTimeSum > 1f)
            {
                frameTimeSum -= frameTimeQueue.Dequeue();
            }
        }
        
        /// <summary>
        /// 現在のフレームレートを取得
        /// </summary>
        private float GetCurrentFrameRate()
        {
            if (frameTimeQueue.Count == 0)
                return 0f;
            
            return frameTimeQueue.Count / frameTimeSum;
        }
        
        /// <summary>
        /// 現在のメモリ使用量を取得（MB）
        /// </summary>
        private float GetCurrentMemoryUsageMB()
        {
            long currentMemory = System.GC.GetTotalMemory(false);
            lastMemoryMeasurement = currentMemory;
            
            // メモリスナップショットを記録
            memorySnapshots.Add(currentMemory);
            if (memorySnapshots.Count > 100) // 最新100個を保持
            {
                memorySnapshots.RemoveAt(0);
            }
            
            return currentMemory / 1024f / 1024f;
        }
        
        /// <summary>
        /// アクティブオブジェクト数を取得
        /// </summary>
        private int GetActiveObjectCount()
        {
            return FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
        }
        
        /// <summary>
        /// ドローコール数を取得
        /// </summary>
        private int GetDrawCallCount()
        {
            // Unity統計情報から取得（エディタでのみ利用可能）
            #if UNITY_EDITOR
            return UnityEditor.UnityStats.drawCalls;
            #else
            return 0; // ビルド版では取得不可
            #endif
        }
        
        /// <summary>
        /// メトリクスの更新
        /// </summary>
        private void UpdateMetrics(PerformanceDataPoint dataPoint)
        {
            // 現在のメトリクス
            currentMetrics.currentFrameRate = dataPoint.frameRate;
            currentMetrics.currentMemoryUsageMB = dataPoint.memoryUsageMB;
            currentMetrics.currentFrameTime = dataPoint.frameTime;
            
            // セッションメトリクス
            sessionMetrics.totalFrames++;
            sessionMetrics.totalFrameTime += dataPoint.frameTime;
            
            if (dataPoint.frameRate > sessionMetrics.maxFrameRate)
                sessionMetrics.maxFrameRate = dataPoint.frameRate;
            if (dataPoint.frameRate < sessionMetrics.minFrameRate || sessionMetrics.minFrameRate == 0f)
                sessionMetrics.minFrameRate = dataPoint.frameRate;
            
            if (dataPoint.memoryUsageMB > sessionMetrics.peakMemoryUsageMB)
                sessionMetrics.peakMemoryUsageMB = dataPoint.memoryUsageMB;
            
            sessionMetrics.averageFrameRate = sessionMetrics.totalFrames / (Time.time - sessionMetrics.sessionStartTime);
            sessionMetrics.averageFrameTime = sessionMetrics.totalFrameTime / sessionMetrics.totalFrames;
            
            // 低フレームレートカウント
            if (dataPoint.frameRate < frameRateWarningThreshold)
            {
                sessionMetrics.lowFrameRateCount++;
            }
        }
        
        /// <summary>
        /// パフォーマンス閾値のチェック
        /// </summary>
        private void CheckPerformanceThresholds()
        {
            // フレームレート警告
            if (monitorFrameRate && currentMetrics.currentFrameRate < frameRateCriticalThreshold)
            {
                Debug.LogError($"Critical frame rate: {currentMetrics.currentFrameRate:F1}FPS");
            }
            else if (monitorFrameRate && currentMetrics.currentFrameRate < frameRateWarningThreshold)
            {
                Debug.LogWarning($"Low frame rate: {currentMetrics.currentFrameRate:F1}FPS");
            }
            
            // メモリ使用量警告
            if (monitorMemoryUsage && currentMetrics.currentMemoryUsageMB > memoryCriticalThresholdMB)
            {
                Debug.LogError($"Critical memory usage: {currentMetrics.currentMemoryUsageMB:F1}MB");
            }
            else if (monitorMemoryUsage && currentMetrics.currentMemoryUsageMB > memoryWarningThresholdMB)
            {
                Debug.LogWarning($"High memory usage: {currentMetrics.currentMemoryUsageMB:F1}MB");
            }
        }
        
        /// <summary>
        /// 生成時間の測定開始
        /// </summary>
        public void StartGenerationTiming(string taskName)
        {
            if (!monitorGenerationTimes)
                return;
            
            activeGenerationTasks[taskName] = Time.realtimeSinceStartup;
        }
        
        /// <summary>
        /// 生成時間の測定終了
        /// </summary>
        public void EndGenerationTiming(string taskName)
        {
            if (!monitorGenerationTimes || !activeGenerationTasks.ContainsKey(taskName))
                return;
            
            float startTime = activeGenerationTasks[taskName];
            float generationTime = Time.realtimeSinceStartup - startTime;
            
            var generationData = new GenerationTimeData
            {
                taskName = taskName,
                generationTime = generationTime,
                timestamp = Time.time
            };
            
            generationTimes.Add(generationData);
            activeGenerationTasks.Remove(taskName);
            
            // 生成時間の警告チェック
            if (generationTime > generationTimeCriticalThreshold)
            {
                Debug.LogError($"Critical generation time for {taskName}: {generationTime:F2}s");
            }
            else if (generationTime > generationTimeWarningThreshold)
            {
                Debug.LogWarning($"Slow generation time for {taskName}: {generationTime:F2}s");
            }
            
            // 古いデータの削除
            generationTimes.RemoveAll(gt => Time.time - gt.timestamp > 300f); // 5分以上古いデータを削除
        }
        
        /// <summary>
        /// パフォーマンスデータのログ出力
        /// </summary>
        private void LogPerformanceData(PerformanceDataPoint dataPoint)
        {
            string logMessage = $"[Performance] FPS: {dataPoint.frameRate:F1}, " +
                              $"Frame: {dataPoint.frameTime * 1000f:F1}ms, " +
                              $"Memory: {dataPoint.memoryUsageMB:F1}MB, " +
                              $"Objects: {dataPoint.activeObjects}";
            
            if (dataPoint.averageGenerationTime > 0f)
            {
                logMessage += $", GenTime: {dataPoint.averageGenerationTime:F2}s";
            }
            
            Debug.Log(logMessage);
        }
        
        /// <summary>
        /// パフォーマンスログをファイルに保存
        /// </summary>
        private void SavePerformanceLogsToFile()
        {
            if (performanceData.Count == 0)
                return;
            
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string logPath = Path.Combine(Application.persistentDataPath, logDirectory);
            
            // テキストログの保存
            string textLogFile = Path.Combine(logPath, $"performance_log_{timestamp}.txt");
            SaveTextLog(textLogFile);
            
            // CSVファイルの保存
            if (enableCSVExport)
            {
                string csvLogFile = Path.Combine(logPath, $"performance_data_{timestamp}.csv");
                SaveCSVLog(csvLogFile);
            }
            
            // サマリーレポートの保存
            string summaryFile = Path.Combine(logPath, $"performance_summary_{timestamp}.txt");
            SaveSummaryReport(summaryFile);
            
            Debug.Log($"Performance logs saved to: {logPath}");
        }
        
        /// <summary>
        /// テキストログの保存
        /// </summary>
        private void SaveTextLog(string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Vastcore Performance Log ===");
            sb.AppendLine($"Session Start: {System.DateTime.Now.AddSeconds(-sessionMetrics.totalSessionTime)}");
            sb.AppendLine($"Session End: {System.DateTime.Now}");
            sb.AppendLine($"Total Session Time: {sessionMetrics.totalSessionTime:F1}s");
            sb.AppendLine();
            
            sb.AppendLine("=== Performance Data ===");
            foreach (var dataPoint in performanceData)
            {
                sb.AppendLine($"[{dataPoint.timestamp:F1}s] FPS: {dataPoint.frameRate:F1}, " +
                             $"Frame: {dataPoint.frameTime * 1000f:F1}ms, " +
                             $"Memory: {dataPoint.memoryUsageMB:F1}MB, " +
                             $"Objects: {dataPoint.activeObjects}");
            }
            
            File.WriteAllText(filePath, sb.ToString());
        }
        
        /// <summary>
        /// CSVログの保存
        /// </summary>
        private void SaveCSVLog(string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,FrameRate,FrameTime,MemoryUsageMB,ActiveObjects,DrawCalls,GenerationTime");
            
            foreach (var dataPoint in performanceData)
            {
                sb.AppendLine($"{dataPoint.timestamp:F2}," +
                             $"{dataPoint.frameRate:F2}," +
                             $"{dataPoint.frameTime:F4}," +
                             $"{dataPoint.memoryUsageMB:F2}," +
                             $"{dataPoint.activeObjects}," +
                             $"{dataPoint.drawCalls}," +
                             $"{dataPoint.averageGenerationTime:F3}");
            }
            
            File.WriteAllText(filePath, sb.ToString());
        }
        
        /// <summary>
        /// サマリーレポートの保存
        /// </summary>
        private void SaveSummaryReport(string filePath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Vastcore Performance Summary ===");
            sb.AppendLine($"Generated: {System.DateTime.Now}");
            sb.AppendLine();
            
            sb.AppendLine("=== Session Statistics ===");
            sb.AppendLine($"Total Session Time: {sessionMetrics.totalSessionTime:F1}s");
            sb.AppendLine($"Total Frames: {sessionMetrics.totalFrames}");
            sb.AppendLine($"Average Frame Rate: {sessionMetrics.averageFrameRate:F1}FPS");
            sb.AppendLine($"Min Frame Rate: {sessionMetrics.minFrameRate:F1}FPS");
            sb.AppendLine($"Max Frame Rate: {sessionMetrics.maxFrameRate:F1}FPS");
            sb.AppendLine($"Average Frame Time: {sessionMetrics.averageFrameTime * 1000f:F1}ms");
            sb.AppendLine($"Low Frame Rate Count: {sessionMetrics.lowFrameRateCount}");
            sb.AppendLine($"Peak Memory Usage: {sessionMetrics.peakMemoryUsageMB:F1}MB");
            sb.AppendLine();
            
            // 生成時間統計
            if (generationTimes.Count > 0)
            {
                sb.AppendLine("=== Generation Time Statistics ===");
                var groupedTimes = new Dictionary<string, List<float>>();
                
                foreach (var genTime in generationTimes)
                {
                    if (!groupedTimes.ContainsKey(genTime.taskName))
                        groupedTimes[genTime.taskName] = new List<float>();
                    groupedTimes[genTime.taskName].Add(genTime.generationTime);
                }
                
                foreach (var kvp in groupedTimes)
                {
                    float avgTime = kvp.Value.Sum() / kvp.Value.Count;
                    float minTime = kvp.Value.Min();
                    float maxTime = kvp.Value.Max();
                    
                    sb.AppendLine($"{kvp.Key}: Avg {avgTime:F2}s, Min {minTime:F2}s, Max {maxTime:F2}s ({kvp.Value.Count} samples)");
                }
            }
            
            File.WriteAllText(filePath, sb.ToString());
        }
        
        /// <summary>
        /// 現在のパフォーマンスメトリクスを取得
        /// </summary>
        public PerformanceMetrics GetCurrentMetrics()
        {
            return currentMetrics;
        }
        
        /// <summary>
        /// セッションメトリクスを取得
        /// </summary>
        public PerformanceMetrics GetSessionMetrics()
        {
            return sessionMetrics;
        }
        
        /// <summary>
        /// パフォーマンスデータを取得
        /// </summary>
        public List<PerformanceDataPoint> GetPerformanceData()
        {
            return new List<PerformanceDataPoint>(performanceData);
        }
        
        /// <summary>
        /// 生成時間データを取得
        /// </summary>
        public List<GenerationTimeData> GetGenerationTimeData()
        {
            return new List<GenerationTimeData>(generationTimes);
        }
        
        // コンテキストメニュー
        [ContextMenu("Start Monitoring")]
        public void StartMonitoringManual()
        {
            StartMonitoring();
        }
        
        [ContextMenu("Stop Monitoring")]
        public void StopMonitoringManual()
        {
            StopMonitoring();
        }
        
        [ContextMenu("Save Logs Now")]
        public void SaveLogsManual()
        {
            SavePerformanceLogsToFile();
        }
        
        [ContextMenu("Log Current Stats")]
        public void LogCurrentStats()
        {
            Debug.Log($"Current Performance: FPS {currentMetrics.currentFrameRate:F1}, Memory {currentMetrics.currentMemoryUsageMB:F1}MB");
            Debug.Log($"Session Stats: Avg FPS {sessionMetrics.averageFrameRate:F1}, Peak Memory {sessionMetrics.peakMemoryUsageMB:F1}MB");
        }
        
        void OnGUI()
        {
            if (!isMonitoring)
                return;
            
            // パフォーマンス情報の表示
            GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 150));
            GUILayout.Label("Performance Monitor", GUI.skin.box);
            GUILayout.Label($"FPS: {currentMetrics.currentFrameRate:F1}");
            GUILayout.Label($"Frame Time: {currentMetrics.currentFrameTime * 1000f:F1}ms");
            GUILayout.Label($"Memory: {currentMetrics.currentMemoryUsageMB:F1}MB");
            GUILayout.Label($"Session Avg FPS: {sessionMetrics.averageFrameRate:F1}");
            GUILayout.Label($"Peak Memory: {sessionMetrics.peakMemoryUsageMB:F1}MB");
            GUILayout.EndArea();
        }
    }
    
    /// <summary>
    /// パフォーマンスデータポイント
    /// </summary>
    [System.Serializable]
    public class PerformanceDataPoint
    {
        public float timestamp;
        public float frameRate;
        public float frameTime;
        public float memoryUsageMB;
        public int activeObjects;
        public int drawCalls;
        public float averageGenerationTime;
    }
    
    /// <summary>
    /// パフォーマンスメトリクス
    /// </summary>
    [System.Serializable]
    public class PerformanceMetrics
    {
        public float currentFrameRate;
        public float currentFrameTime;
        public float currentMemoryUsageMB;
        
        public float sessionStartTime;
        public float sessionEndTime;
        public float totalSessionTime;
        
        public int totalFrames;
        public float totalFrameTime;
        public float averageFrameRate;
        public float averageFrameTime;
        public float minFrameRate;
        public float maxFrameRate;
        
        public float peakMemoryUsageMB;
        public int lowFrameRateCount;
        
        public void Reset()
        {
            currentFrameRate = 0f;
            currentFrameTime = 0f;
            currentMemoryUsageMB = 0f;
            
            totalFrames = 0;
            totalFrameTime = 0f;
            averageFrameRate = 0f;
            averageFrameTime = 0f;
            minFrameRate = 0f;
            maxFrameRate = 0f;
            
            peakMemoryUsageMB = 0f;
            lowFrameRateCount = 0;
        }
    }
    
    /// <summary>
    /// 生成時間データ
    /// </summary>
    [System.Serializable]
    public class GenerationTimeData
    {
        public string taskName;
        public float generationTime;
        public float timestamp;
    }
}
#endif