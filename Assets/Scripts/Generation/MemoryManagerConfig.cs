using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// メモリ管理設定クラス
    /// </summary>
    [CreateAssetMenu(fileName = "MemoryManagerConfig", menuName = "VastCore/Memory Manager Config")]
    public class MemoryManagerConfig : ScriptableObject
    {
        [Header("メモリ管理設定")]
        public bool enableMemoryOptimization = true;
        public float memoryCheckInterval = 5f; // メモリチェック間隔（秒）
        public long maxMemoryUsageMB = 512; // 最大メモリ使用量（MB）
        public float gcTriggerThreshold = 0.8f; // GC実行閾値（メモリ使用率）

        [Header("オブジェクト管理")]
        public int maxActiveObjects = 100;
        public float objectCullingDistance = 2000f;
        public bool enableAutomaticCulling = true;

        [Header("パフォーマンス監視")]
        public bool enablePerformanceMonitoring = true;
        public bool logMemoryUsage = false;
        public float performanceLogInterval = 10f;
    }
}
