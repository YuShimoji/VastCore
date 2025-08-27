using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Vastcore.Generation
{
    /// <summary>
    /// プリミティブ地形システムのメモリ管理とガベージコレクション最適化
    /// </summary>
    public class PrimitiveMemoryManager : MonoBehaviour
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
        
        // メモリ統計
        private long lastMemoryUsage = 0;
        private int lastActiveObjectCount = 0;
        private float lastGCTime = 0;
        private int gcCallCount = 0;
        
        // 管理対象オブジェクト
        private List<PrimitiveTerrainObject> managedObjects;
        private Dictionary<int, float> objectLastAccessTime;
        
        // コルーチン参照
        private Coroutine memoryMonitorCoroutine;
        private Coroutine performanceMonitorCoroutine;
        
        // シングルトンインスタンス
        private static PrimitiveMemoryManager instance;
        
        public static PrimitiveMemoryManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<PrimitiveMemoryManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("PrimitiveMemoryManager");
                        instance = go.AddComponent<PrimitiveMemoryManager>();
                    }
                }
                return instance;
            }
        }

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeMemoryManager();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            if (enableMemoryOptimization)
            {
                memoryMonitorCoroutine = StartCoroutine(MemoryMonitorLoop());
            }
            
            if (enablePerformanceMonitoring)
            {
                performanceMonitorCoroutine = StartCoroutine(PerformanceMonitorLoop());
            }
        }

        void OnDestroy()
        {
            if (memoryMonitorCoroutine != null)
            {
                StopCoroutine(memoryMonitorCoroutine);
            }
            
            if (performanceMonitorCoroutine != null)
            {
                StopCoroutine(performanceMonitorCoroutine);
            }
        }

        /// <summary>
        /// メモリマネージャーを初期化
        /// </summary>
        private void InitializeMemoryManager()
        {
            managedObjects = new List<PrimitiveTerrainObject>();
            objectLastAccessTime = new Dictionary<int, float>();
            
            // 初期メモリ使用量を記録
            lastMemoryUsage = System.GC.GetTotalMemory(false);
            
            Debug.Log("PrimitiveMemoryManager initialized");
        }

        /// <summary>
        /// オブジェクトを管理対象に追加
        /// </summary>
        public void RegisterObject(PrimitiveTerrainObject obj)
        {
            if (obj != null && !managedObjects.Contains(obj))
            {
                managedObjects.Add(obj);
                objectLastAccessTime[obj.GetInstanceID()] = Time.time;
                
                if (logMemoryUsage)
                {
                    Debug.Log($"Registered object: {obj.name} (Total managed: {managedObjects.Count})");
                }
            }
        }

        /// <summary>
        /// オブジェクトを管理対象から削除
        /// </summary>
        public void UnregisterObject(PrimitiveTerrainObject obj)
        {
            if (obj != null)
            {
                managedObjects.Remove(obj);
                objectLastAccessTime.Remove(obj.GetInstanceID());
                
                if (logMemoryUsage)
                {
                    Debug.Log($"Unregistered object: {obj.name} (Total managed: {managedObjects.Count})");
                }
            }
        }

        /// <summary>
        /// オブジェクトのアクセス時間を更新
        /// </summary>
        public void UpdateObjectAccessTime(PrimitiveTerrainObject obj)
        {
            if (obj != null)
            {
                objectLastAccessTime[obj.GetInstanceID()] = Time.time;
            }
        }

        /// <summary>
        /// メモリ監視ループ
        /// </summary>
        private IEnumerator MemoryMonitorLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(memoryCheckInterval);
                
                CheckMemoryUsage();
                PerformMemoryOptimization();
            }
        }

        /// <summary>
        /// パフォーマンス監視ループ
        /// </summary>
        private IEnumerator PerformanceMonitorLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(performanceLogInterval);
                
                LogPerformanceMetrics();
            }
        }

        /// <summary>
        /// メモリ使用量をチェック
        /// </summary>
        private void CheckMemoryUsage()
        {
            long currentMemory = System.GC.GetTotalMemory(false);
            long memoryDelta = currentMemory - lastMemoryUsage;
            
            // メモリ使用量が閾値を超えた場合
            float memoryUsageRatio = (float)currentMemory / (maxMemoryUsageMB * 1024 * 1024);
            
            if (memoryUsageRatio > gcTriggerThreshold)
            {
                TriggerGarbageCollection();
            }
            
            lastMemoryUsage = currentMemory;
            
            if (logMemoryUsage)
            {
                Debug.Log($"Memory usage: {currentMemory / (1024 * 1024)}MB (Delta: {memoryDelta / 1024}KB, Ratio: {memoryUsageRatio:F2})");
            }
        }

        /// <summary>
        /// メモリ最適化を実行
        /// </summary>
        private void PerformMemoryOptimization()
        {
            // 無効なオブジェクトを削除
            CleanupInvalidObjects();
            
            // 自動カリングが有効な場合、遠距離オブジェクトを処理
            if (enableAutomaticCulling)
            {
                CullDistantObjects();
            }
            
            // アクティブオブジェクト数が上限を超えた場合
            if (managedObjects.Count > maxActiveObjects)
            {
                CullLeastRecentlyUsedObjects();
            }
        }

        /// <summary>
        /// 無効なオブジェクトをクリーンアップ
        /// </summary>
        private void CleanupInvalidObjects()
        {
            int removedCount = 0;
            
            for (int i = managedObjects.Count - 1; i >= 0; i--)
            {
                if (managedObjects[i] == null)
                {
                    int instanceId = managedObjects[i]?.GetInstanceID() ?? 0;
                    managedObjects.RemoveAt(i);
                    objectLastAccessTime.Remove(instanceId);
                    removedCount++;
                }
            }
            
            if (removedCount > 0 && logMemoryUsage)
            {
                Debug.Log($"Cleaned up {removedCount} invalid objects");
            }
        }

        /// <summary>
        /// 遠距離オブジェクトをカリング
        /// </summary>
        private void CullDistantObjects()
        {
            var playerTransform = GetPlayerTransform();
            if (playerTransform == null) return;
            
            var objectsToCull = new List<PrimitiveTerrainObject>();
            
            foreach (var obj in managedObjects)
            {
                if (obj != null)
                {
                    float distance = Vector3.Distance(obj.transform.position, playerTransform.position);
                    if (distance > objectCullingDistance)
                    {
                        objectsToCull.Add(obj);
                    }
                }
            }
            
            foreach (var obj in objectsToCull)
            {
                CullObject(obj);
            }
            
            if (objectsToCull.Count > 0 && logMemoryUsage)
            {
                Debug.Log($"Culled {objectsToCull.Count} distant objects");
            }
        }

        /// <summary>
        /// 最も使用されていないオブジェクトをカリング
        /// </summary>
        private void CullLeastRecentlyUsedObjects()
        {
            // アクセス時間でソート
            managedObjects.Sort((a, b) =>
            {
                float timeA = objectLastAccessTime.ContainsKey(a.GetInstanceID()) ? objectLastAccessTime[a.GetInstanceID()] : 0;
                float timeB = objectLastAccessTime.ContainsKey(b.GetInstanceID()) ? objectLastAccessTime[b.GetInstanceID()] : 0;
                return timeA.CompareTo(timeB);
            });
            
            int objectsToCull = managedObjects.Count - maxActiveObjects;
            int culledCount = 0;
            
            for (int i = 0; i < objectsToCull && i < managedObjects.Count; i++)
            {
                if (managedObjects[i] != null)
                {
                    CullObject(managedObjects[i]);
                    culledCount++;
                }
            }
            
            if (culledCount > 0 && logMemoryUsage)
            {
                Debug.Log($"Culled {culledCount} least recently used objects");
            }
        }

        /// <summary>
        /// オブジェクトをカリング（プールに戻す）
        /// </summary>
        private void CullObject(PrimitiveTerrainObject obj)
        {
            if (obj != null)
            {
                // プールに戻す
                var pool = PrimitiveTerrainObjectPool.Instance;
                if (pool != null)
                {
                    pool.ReturnToPool(obj);
                }
                
                // 管理対象から削除
                UnregisterObject(obj);
            }
        }

        /// <summary>
        /// ガベージコレクションを実行
        /// </summary>
        private void TriggerGarbageCollection()
        {
            float startTime = Time.realtimeSinceStartup;
            
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            
            float gcTime = Time.realtimeSinceStartup - startTime;
            lastGCTime = gcTime;
            gcCallCount++;
            
            if (logMemoryUsage)
            {
                long memoryAfterGC = System.GC.GetTotalMemory(false);
                Debug.Log($"Garbage collection completed in {gcTime:F3}s. Memory after GC: {memoryAfterGC / (1024 * 1024)}MB");
            }
        }

        /// <summary>
        /// パフォーマンスメトリクスをログ出力
        /// </summary>
        private void LogPerformanceMetrics()
        {
            var metrics = GetPerformanceMetrics();
            
            Debug.Log($"Performance Metrics - " +
                     $"Managed Objects: {metrics.managedObjectCount}, " +
                     $"Memory: {metrics.memoryUsageMB:F1}MB, " +
                     $"GC Calls: {metrics.gcCallCount}, " +
                     $"Avg GC Time: {metrics.averageGCTime:F3}s");
        }

        /// <summary>
        /// プレイヤーのTransformを取得
        /// </summary>
        private Transform GetPlayerTransform()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            return player?.transform;
        }

        /// <summary>
        /// パフォーマンスメトリクスを取得
        /// </summary>
        public PerformanceMetrics GetPerformanceMetrics()
        {
            return new PerformanceMetrics
            {
                managedObjectCount = managedObjects.Count,
                memoryUsageMB = System.GC.GetTotalMemory(false) / (1024f * 1024f),
                gcCallCount = gcCallCount,
                averageGCTime = gcCallCount > 0 ? lastGCTime : 0f,
                maxActiveObjects = maxActiveObjects,
                cullingDistance = objectCullingDistance
            };
        }

        /// <summary>
        /// メモリ使用量を強制的に最適化
        /// </summary>
        public void ForceMemoryOptimization()
        {
            PerformMemoryOptimization();
            TriggerGarbageCollection();
            
            Debug.Log("Forced memory optimization completed");
        }

        /// <summary>
        /// 全管理オブジェクトをクリア
        /// </summary>
        public void ClearAllManagedObjects()
        {
            foreach (var obj in managedObjects)
            {
                if (obj != null)
                {
                    CullObject(obj);
                }
            }
            
            managedObjects.Clear();
            objectLastAccessTime.Clear();
            
            Debug.Log("All managed objects cleared");
        }
    }

    /// <summary>
    /// パフォーマンスメトリクス情報
    /// </summary>
    [System.Serializable]
    public struct PerformanceMetrics
    {
        public int managedObjectCount;
        public float memoryUsageMB;
        public int gcCallCount;
        public float averageGCTime;
        public int maxActiveObjects;
        public float cullingDistance;
    }
}