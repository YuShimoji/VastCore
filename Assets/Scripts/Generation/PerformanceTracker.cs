using System.Collections;
using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// パフォーマンス追跡クラス
    /// </summary>
    public class PerformanceTracker
    {
        private MemoryManagerConfig config;
        private long lastMemoryUsage = 0;
        private int lastActiveObjectCount = 0;
        private float lastGCTime = 0;
        private int gcCallCount = 0;

        public PerformanceTracker(MemoryManagerConfig config)
        {
            this.config = config;
        }

        public IEnumerator StartTracking()
        {
            while (true)
            {
                UpdateStatistics();
                LogPerformanceMetrics();
                yield return new WaitForSeconds(config.performanceLogInterval);
            }
        }

        private void UpdateStatistics()
        {
            lastMemoryUsage = System.GC.GetTotalMemory(false) / (1024 * 1024);
            lastActiveObjectCount = Object.FindObjectsOfType<GameObject>().Length;
            lastGCTime = Time.realtimeSinceStartup;
            gcCallCount++;
        }

        private void LogPerformanceMetrics()
        {
            if (config.logMemoryUsage)
            {
                Debug.Log($"Memory: {lastMemoryUsage}MB, Objects: {lastActiveObjectCount}, GC Calls: {gcCallCount}");
            }
        }

        public void RecordGCCall()
        {
            gcCallCount++;
        }
    }
}
