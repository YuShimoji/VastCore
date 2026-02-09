using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Vastcore.Generation;

namespace Vastcore.Generation
{
    /// <summary>
    /// プリミティブ地形オブジェクトのメモリプール管理システム
    /// オブジェクトの再利用によりガベージコレクション負荷を軽減
    /// </summary>
    public class PrimitiveTerrainObjectPool : MonoBehaviour
    {
        [Header("プール設定")]
        public int initialPoolSize = 50;
        public int maxPoolSize = 200;
        public bool enableDynamicExpansion = true;
        public float cleanupInterval = 30f; // 未使用オブジェクトのクリーンアップ間隔（秒）
        
        [Header("プリファブ設定")]
        public GameObject primitiveTerrainPrefab;
        public Transform poolParent; // プールオブジェクトの親Transform
        
        [Header("パフォーマンス監視")]
        public bool enablePerformanceMonitoring = true;
        public bool logPoolOperations = false;
        
        // プールデータ構造
        private Queue<PrimitiveTerrainObject> availableObjects;
        private HashSet<PrimitiveTerrainObject> activeObjects;
        private Dictionary<PrimitiveTerrainGenerator.PrimitiveType, Queue<PrimitiveTerrainObject>> typeSpecificPools;
        
        // パフォーマンス統計
        private int totalCreated = 0;
        private int totalReused = 0;
        private int totalReturned = 0;
        private int peakActiveCount = 0;
        
        // 内部管理
        private Coroutine cleanupCoroutine;
        private static PrimitiveTerrainObjectPool instance;
        
        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static PrimitiveTerrainObjectPool Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<PrimitiveTerrainObjectPool>();
                    if (instance == null)
                    {
                        var go = new GameObject("PrimitiveTerrainObjectPool");
                        instance = go.AddComponent<PrimitiveTerrainObjectPool>();
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
                InitializePool();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            if (cleanupInterval > 0)
            {
                cleanupCoroutine = StartCoroutine(PeriodicCleanup());
            }
        }

        void OnDestroy()
        {
            if (cleanupCoroutine != null)
            {
                StopCoroutine(cleanupCoroutine);
            }
        }

        /// <summary>
        /// プールを初期化
        /// </summary>
        private void InitializePool()
        {
            availableObjects = new Queue<PrimitiveTerrainObject>();
            activeObjects = new HashSet<PrimitiveTerrainObject>();
            typeSpecificPools = new Dictionary<PrimitiveTerrainGenerator.PrimitiveType, Queue<PrimitiveTerrainObject>>();
            
            // 各プリミティブタイプ用のプールを初期化
            var primitiveTypes = System.Enum.GetValues(typeof(PrimitiveTerrainGenerator.PrimitiveType));
            foreach (PrimitiveTerrainGenerator.PrimitiveType type in primitiveTypes)
            {
                typeSpecificPools[type] = new Queue<PrimitiveTerrainObject>();
            }
            
            // プール親オブジェクトの設定
            if (poolParent == null)
            {
                var poolParentGO = new GameObject("PooledObjects");
                poolParentGO.transform.SetParent(transform);
                poolParent = poolParentGO.transform;
            }
            
            // 初期オブジェクトを作成
            PrewarmPool(initialPoolSize);
            
            if (logPoolOperations)
            {
                Debug.Log($"PrimitiveTerrainObjectPool initialized with {initialPoolSize} objects");
            }
        }

        /// <summary>
        /// プールを事前に温める（初期オブジェクト作成）
        /// </summary>
        private void PrewarmPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = CreateNewPoolObject();
                if (obj != null)
                {
                    ReturnToPool(obj);
                }
            }
        }

        /// <summary>
        /// 新しいプールオブジェクトを作成
        /// </summary>
        private PrimitiveTerrainObject CreateNewPoolObject()
        {
            if (primitiveTerrainPrefab == null)
            {
                Debug.LogError("PrimitiveTerrainPrefab is not assigned!");
                return null;
            }
            
            var go = Instantiate(primitiveTerrainPrefab, poolParent);
            go.SetActive(false);
            
            var primitiveObj = go.GetComponent<PrimitiveTerrainObject>();
            if (primitiveObj == null)
            {
                primitiveObj = go.AddComponent<PrimitiveTerrainObject>();
            }
            
            totalCreated++;
            
            if (logPoolOperations)
            {
                Debug.Log($"Created new pool object: {go.name} (Total created: {totalCreated})");
            }
            
            return primitiveObj;
        }
        /// <summary>
        /// プールからオブジェクトを取得
        /// </summary>
        public PrimitiveTerrainObject GetFromPool(PrimitiveTerrainGenerator.PrimitiveType primitiveType, Vector3 position, float scale)
        {
            PrimitiveTerrainObject obj = null;
            
            // タイプ固有のプールから取得を試行
            if (typeSpecificPools.ContainsKey(primitiveType) && typeSpecificPools[primitiveType].Count > 0)
            {
                obj = typeSpecificPools[primitiveType].Dequeue();
                totalReused++;
            }
            // 汎用プールから取得を試行
            else if (availableObjects.Count > 0)
            {
                obj = availableObjects.Dequeue();
                totalReused++;
            }
            // 新しいオブジェクトを作成
            else if (enableDynamicExpansion && activeObjects.Count < maxPoolSize)
            {
                obj = CreateNewPoolObject();
            }
            
            if (obj != null)
            {
                // オブジェクトを初期化してアクティブリストに追加
                obj.InitializeFromPool((Vastcore.Core.GenerationPrimitiveType)(int)primitiveType, position, Vector3.one * scale);
                activeObjects.Add(obj);
                
                // ピーク使用数を更新
                if (activeObjects.Count > peakActiveCount)
                {
                    peakActiveCount = activeObjects.Count;
                }
                
                if (logPoolOperations)
                {
                    Debug.Log($"Retrieved object from pool: {obj.name} (Active: {activeObjects.Count})");
                }
            }
            else
            {
                Debug.LogWarning($"Failed to get object from pool. Active: {activeObjects.Count}, Available: {availableObjects.Count}");
            }
            
            return obj;
        }

        /// <summary>
        /// オブジェクトをプールに戻す
        /// </summary>
        public void ReturnToPool(PrimitiveTerrainObject obj)
        {
            if (obj == null) return;
            
            if (activeObjects.Contains(obj))
            {
                activeObjects.Remove(obj);
            }
            
            // オブジェクトをプール用に準備
            obj.PrepareForPool();
            obj.transform.SetParent(poolParent);
            
            // タイプ固有のプールに戻す
            var primitiveType = (PrimitiveTerrainGenerator.PrimitiveType)(int)obj.primitiveType;
            if (typeSpecificPools.ContainsKey(primitiveType))
            {
                typeSpecificPools[primitiveType].Enqueue(obj);
            }
            else
            {
                availableObjects.Enqueue(obj);
            }
            
            totalReturned++;
            
            if (logPoolOperations)
            {
                Debug.Log($"Returned object to pool: {obj.name} (Available: {GetTotalAvailableCount()})");
            }
        }

        /// <summary>
        /// 複数のオブジェクトを一括でプールに戻す
        /// </summary>
        public void ReturnMultipleToPool(IEnumerable<PrimitiveTerrainObject> objects)
        {
            foreach (var obj in objects)
            {
                ReturnToPool(obj);
            }
        }

        /// <summary>
        /// 指定した距離より遠いオブジェクトをプールに戻す
        /// </summary>
        public int ReturnDistantObjects(Vector3 centerPosition, float maxDistance)
        {
            var objectsToReturn = new List<PrimitiveTerrainObject>();
            
            foreach (var obj in activeObjects)
            {
                if (obj != null && Vector3.Distance(obj.transform.position, centerPosition) > maxDistance)
                {
                    objectsToReturn.Add(obj);
                }
            }
            
            foreach (var obj in objectsToReturn)
            {
                ReturnToPool(obj);
            }
            
            return objectsToReturn.Count;
        }

        /// <summary>
        /// 定期的なクリーンアップ処理
        /// </summary>
        private IEnumerator PeriodicCleanup()
        {
            while (true)
            {
                yield return new WaitForSeconds(cleanupInterval);
                
                PerformCleanup();
                
                if (enablePerformanceMonitoring)
                {
                    LogPerformanceStatistics();
                }
            }
        }

        /// <summary>
        /// クリーンアップ処理を実行
        /// </summary>
        private void PerformCleanup()
        {
            int cleanedUp = 0;
            
            // 無効なオブジェクトをアクティブリストから削除
            activeObjects.RemoveWhere(obj => obj == null);
            
            // 過剰なプールオブジェクトを削除
            int totalAvailable = GetTotalAvailableCount();
            int excessCount = totalAvailable - initialPoolSize;
            
            if (excessCount > 0)
            {
                cleanedUp += CleanupExcessObjects(excessCount);
            }
            
            if (logPoolOperations && cleanedUp > 0)
            {
                Debug.Log($"Cleaned up {cleanedUp} excess pool objects");
            }
        }

        /// <summary>
        /// 過剰なオブジェクトをクリーンアップ
        /// </summary>
        private int CleanupExcessObjects(int excessCount)
        {
            int cleaned = 0;
            
            // 汎用プールから削除
            while (availableObjects.Count > 0 && cleaned < excessCount)
            {
                var obj = availableObjects.Dequeue();
                if (obj != null)
                {
                    Destroy(obj.gameObject);
                    cleaned++;
                }
            }
            
            // タイプ固有プールから削除
            foreach (var pool in typeSpecificPools.Values)
            {
                while (pool.Count > 0 && cleaned < excessCount)
                {
                    var obj = pool.Dequeue();
                    if (obj != null)
                    {
                        Destroy(obj.gameObject);
                        cleaned++;
                    }
                }
            }
            
            return cleaned;
        }

        /// <summary>
        /// 利用可能なオブジェクトの総数を取得
        /// </summary>
        private int GetTotalAvailableCount()
        {
            int total = availableObjects.Count;
            foreach (var pool in typeSpecificPools.Values)
            {
                total += pool.Count;
            }
            return total;
        }

        /// <summary>
        /// パフォーマンス統計をログ出力
        /// </summary>
        private void LogPerformanceStatistics()
        {
            var stats = GetPoolStatistics();
            Debug.Log($"Pool Stats - Active: {stats.activeCount}, Available: {stats.availableCount}, " +
                     $"Created: {stats.totalCreated}, Reused: {stats.totalReused}, Peak: {stats.peakActiveCount}");
        }

        /// <summary>
        /// プール統計情報を取得
        /// </summary>
        public PoolStatistics GetPoolStatistics()
        {
            return new PoolStatistics
            {
                activeCount = activeObjects.Count,
                availableCount = GetTotalAvailableCount(),
                totalCreated = totalCreated,
                totalReused = totalReused,
                totalReturned = totalReturned,
                peakActiveCount = peakActiveCount,
                reuseRatio = totalCreated > 0 ? (float)totalReused / totalCreated : 0f
            };
        }

        /// <summary>
        /// プールをリセット（全オブジェクトを削除）
        /// </summary>
        public void ResetPool()
        {
            // アクティブオブジェクトを削除
            foreach (var obj in activeObjects)
            {
                if (obj != null)
                {
                    Destroy(obj.gameObject);
                }
            }
            activeObjects.Clear();
            
            // 利用可能オブジェクトを削除
            while (availableObjects.Count > 0)
            {
                var obj = availableObjects.Dequeue();
                if (obj != null)
                {
                    Destroy(obj.gameObject);
                }
            }
            
            foreach (var pool in typeSpecificPools.Values)
            {
                while (pool.Count > 0)
                {
                    var obj = pool.Dequeue();
                    if (obj != null)
                    {
                        Destroy(obj.gameObject);
                    }
                }
            }
            
            // 統計をリセット
            totalCreated = 0;
            totalReused = 0;
            totalReturned = 0;
            peakActiveCount = 0;
            
            Debug.Log("Pool has been reset");
        }
    }

    /// <summary>
    /// プール統計情報を格納する構造体
    /// </summary>
    [System.Serializable]
    public struct PoolStatistics
    {
        public int activeCount;
        public int availableCount;
        public int totalCreated;
        public int totalReused;
        public int totalReturned;
        public int peakActiveCount;
        public float reuseRatio;
    }
}