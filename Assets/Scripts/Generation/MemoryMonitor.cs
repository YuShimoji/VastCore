using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// メモリ監視クラス
    /// </summary>
    public class MemoryMonitor
    {
        private MemoryManagerConfig config;
        private System.Action onMemoryThresholdExceeded;

        public MemoryMonitor(MemoryManagerConfig config, System.Action onMemoryThresholdExceeded)
        {
            this.config = config;
            this.onMemoryThresholdExceeded = onMemoryThresholdExceeded;
        }

        public IEnumerator StartMonitoring()
        {
            while (true)
            {
                CheckMemoryUsage();
                yield return new WaitForSeconds(config.memoryCheckInterval);
            }
        }

        private void CheckMemoryUsage()
        {
            long currentMemoryMB = System.GC.GetTotalMemory(false) / (1024 * 1024);

            if (currentMemoryMB > config.maxMemoryUsageMB)
            {
                if (config.logMemoryUsage)
                {
                    Debug.LogWarning($"Memory usage exceeded threshold: {currentMemoryMB}MB / {config.maxMemoryUsageMB}MB");
                }

                onMemoryThresholdExceeded?.Invoke();
            }
        }
    }
}
