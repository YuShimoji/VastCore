using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace VastCore.Testing
{
    /// <summary>
    /// メモリ使用量の詳細監視とリーク検出システム
    /// </summary>
    public class MemoryMonitor : MonoBehaviour
    {
        [Header("監視設定")]
        [SerializeField] private bool enableContinuousMonitoring = true;
        [SerializeField] private float monitoringInterval = 5f;
        [SerializeField] private int maxHistoryEntries = 1000;
        
        [Header("リーク検出設定")]
        [SerializeField] private long leakThresholdMB = 100;
        [SerializeField] private int leakDetectionSamples = 10;
        [SerializeField] private bool enableAutomaticGC = false;
        
        // メモリ使用量履歴
        private List<MemorySnapshot> memoryHistory;
        private long baselineMemory;
        private DateTime monitoringStartTime;
        
        // リーク検出
        private Queue<long> recentMemoryReadings;
        private bool leakDetected = false;
        
        // イベント
        public event Action<MemorySnapshot> OnMemorySnapshotTaken;
        public event Action<long> OnMemoryLeakDetected;
        public event Action<long> OnMemoryThresholdExceeded;
        
        private void Start()
        {
            InitializeMonitoring();
            
            if (enableContinuousMonitoring)
            {
                InvokeRepeating(nameof(TakeMemorySnapshot), 0f, monitoringInterval);
            }
        }
        
        private void InitializeMonitoring()
        {
            memoryHistory = new List<MemorySnapshot>();
            recentMemoryReadings = new Queue<long>();
            monitoringStartTime = DateTime.Now;
            
            // ベースラインメモリの設定
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            baselineMemory = GetTotalMemoryUsage();
            
            Debug.Log($"MemoryMonitor initialized. Baseline memory: {baselineMemory / (1024 * 1024)}MB");
        }
        
        public void TakeMemorySnapshot()
        {
            var snapshot = new MemorySnapshot
            {
                timestamp = DateTime.Now,
                elapsedTime = (float)(DateTime.Now - monitoringStartTime).TotalSeconds,
                totalAllocatedMemory = GetTotalMemoryUsage(),
                totalReservedMemory = GetTotalReservedMemory(),
                totalUnusedReservedMemory = GetTotalUnusedReservedMemory(),
                monoHeapSize = GetMonoHeapSize(),
                monoUsedSize = GetMonoUsedSize(),
                tempAllocatorSize = GetTempAllocatorSize(),
                gfxDriverAllocatedMemory = GetGfxDriverMemory(),
                audioMemory = GetAudioMemory(),
                videoMemory = GetVideoMemory()
            };
            
            // 履歴に追加
            memoryHistory.Add(snapshot);
            
            // 履歴サイズの制限
            if (memoryHistory.Count > maxHistoryEntries)
            {
                memoryHistory.RemoveAt(0);
            }
            
            // リーク検出用のキューに追加
            recentMemoryReadings.Enqueue(snapshot.totalAllocatedMemory);
            if (recentMemoryReadings.Count > leakDetectionSamples)
            {
                recentMemoryReadings.Dequeue();
            }
            
            // リーク検出
            DetectMemoryLeak(snapshot);
            
            // 閾値チェック
            CheckMemoryThresholds(snapshot);
            
            // イベント発火
            OnMemorySnapshotTaken?.Invoke(snapshot);
        }
        
        private void DetectMemoryLeak(MemorySnapshot snapshot)
        {
            if (recentMemoryReadings.Count < leakDetectionSamples)
                return;
            
            // 最近のメモリ使用量の傾向を分析
            long[] readings = new long[recentMemoryReadings.Count];
            recentMemoryReadings.CopyTo(readings, 0);
            
            // 線形回帰による傾向分析
            float slope = CalculateMemoryTrend(readings);
            long currentIncrease = snapshot.totalAllocatedMemory - baselineMemory;
            
            // リーク判定
            bool isLeaking = slope > 0 && currentIncrease > leakThresholdMB * 1024 * 1024;
            
            if (isLeaking && !leakDetected)
            {
                leakDetected = true;
                long leakSizeMB = currentIncrease / (1024 * 1024);
                
                Debug.LogWarning($"Memory leak detected! Current increase: {leakSizeMB}MB, Trend: {slope:F2}MB/s");
                OnMemoryLeakDetected?.Invoke(leakSizeMB);
                
                if (enableAutomaticGC)
                {
                    ForceGarbageCollection();
                }
            }
            else if (!isLeaking && leakDetected)
            {
                leakDetected = false;
                Debug.Log("Memory leak condition resolved");
            }
        }
        
        private float CalculateMemoryTrend(long[] readings)
        {
            if (readings.Length < 2) return 0f;
            
            float n = readings.Length;
            float sumX = 0f, sumY = 0f, sumXY = 0f, sumX2 = 0f;
            
            for (int i = 0; i < readings.Length; i++)
            {
                float x = i;
                float y = readings[i] / (1024f * 1024f); // MB単位
                
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
            }
            
            // 線形回帰の傾き計算
            float slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            return slope * monitoringInterval; // MB/秒に変換
        }
        
        private void CheckMemoryThresholds(MemorySnapshot snapshot)
        {
            long totalMemoryMB = snapshot.totalAllocatedMemory / (1024 * 1024);
            
            // 各種閾値チェック
            if (totalMemoryMB > 1024) // 1GB
            {
                OnMemoryThresholdExceeded?.Invoke(totalMemoryMB);
            }
        }
        
        public void ForceGarbageCollection()
        {
            Debug.Log("Forcing garbage collection...");
            
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            // Unity固有のクリーンアップ
            Resources.UnloadUnusedAssets();
            
            Debug.Log("Garbage collection completed");
        }
        
        public MemoryReport GenerateMemoryReport()
        {
            if (memoryHistory.Count == 0)
            {
                return new MemoryReport { isValid = false };
            }
            
            var report = new MemoryReport
            {
                isValid = true,
                monitoringDuration = (float)(DateTime.Now - monitoringStartTime).TotalHours,
                totalSnapshots = memoryHistory.Count,
                baselineMemoryMB = baselineMemory / (1024 * 1024),
                currentMemoryMB = GetTotalMemoryUsage() / (1024 * 1024),
                peakMemoryMB = GetPeakMemoryUsage() / (1024 * 1024),
                averageMemoryMB = GetAverageMemoryUsage() / (1024 * 1024),
                memoryIncreaseMB = (GetTotalMemoryUsage() - baselineMemory) / (1024 * 1024),
                leakDetected = leakDetected,
                gcCollections = System.GC.CollectionCount(0) + System.GC.CollectionCount(1) + System.GC.CollectionCount(2)
            };
            
            return report;
        }
        
        private long GetPeakMemoryUsage()
        {
            long peak = 0;
            foreach (var snapshot in memoryHistory)
            {
                if (snapshot.totalAllocatedMemory > peak)
                    peak = snapshot.totalAllocatedMemory;
            }
            return peak;
        }
        
        private long GetAverageMemoryUsage()
        {
            if (memoryHistory.Count == 0) return 0;
            
            long total = 0;
            foreach (var snapshot in memoryHistory)
            {
                total += snapshot.totalAllocatedMemory;
            }
            return total / memoryHistory.Count;
        }
        
        // Unity Profiler APIを使用したメモリ情報取得
        private long GetTotalMemoryUsage() => Profiler.GetTotalAllocatedMemoryLong();
        private long GetTotalReservedMemory() => Profiler.GetTotalReservedMemoryLong();
        private long GetTotalUnusedReservedMemory() => Profiler.GetTotalUnusedReservedMemoryLong();
<<<<<<< HEAD
        private long GetMonoHeapSize() => Profiler.GetMonoHeapSizeLong();
        private long GetMonoUsedSize() => Profiler.GetMonoUsedSizeLong();
=======
        private long GetMonoHeapSize() => Profiler.GetMonoHeapSize();
        private long GetMonoUsedSize() => Profiler.GetMonoUsedSize();
>>>>>>> origin/develop
        private long GetTempAllocatorSize() => Profiler.GetTempAllocatorSize();
        private long GetGfxDriverMemory() => Profiler.GetAllocatedMemoryForGraphicsDriver();
        private long GetAudioMemory() => 0; // Unity 2019.3以降で利用可能
        private long GetVideoMemory() => 0; // プラットフォーム依存
        
        private void OnDestroy()
        {
            if (enableContinuousMonitoring)
            {
                CancelInvoke(nameof(TakeMemorySnapshot));
            }
        }
    }
    
    [System.Serializable]
    public struct MemorySnapshot
    {
        public DateTime timestamp;
        public float elapsedTime;
        public long totalAllocatedMemory;
        public long totalReservedMemory;
        public long totalUnusedReservedMemory;
        public long monoHeapSize;
        public long monoUsedSize;
        public long tempAllocatorSize;
        public long gfxDriverAllocatedMemory;
        public long audioMemory;
        public long videoMemory;
    }
    
    [System.Serializable]
    public struct MemoryReport
    {
        public bool isValid;
        public float monitoringDuration;
        public int totalSnapshots;
        public long baselineMemoryMB;
        public long currentMemoryMB;
        public long peakMemoryMB;
        public long averageMemoryMB;
        public long memoryIncreaseMB;
        public bool leakDetected;
        public int gcCollections;
    }
}