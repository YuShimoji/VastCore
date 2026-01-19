using UnityEngine;
using System.Collections;
using Vastcore.Terrain.Map;

namespace Vastcore.Generation
{
    /// <summary>
    /// LODシステムとメモリプール管理のテストクラス
    /// </summary>
    public class LODMemorySystemTest : MonoBehaviour
    {
        [Header("テスト設定")]
        public bool runAutomaticTests = true;
        public float testInterval = 5f;
        public int testObjectCount = 10;
        public bool logTestResults = true;
        
        [Header("LODテスト")]
        public bool testLODSystem = true;
        public float[] testDistances = { 100f, 500f, 1000f, 2000f, 3000f };
        
        [Header("メモリプールテスト")]
        public bool testMemoryPool = true;
        public int poolStressTestCount = 50;
        
        [Header("パフォーマンステスト")]
        public bool testPerformance = true;
        public int performanceTestIterations = 100;
        
        private Coroutine testCoroutine;
        private PrimitiveTerrainObjectPool pool;
        private PrimitiveMemoryManager memoryManager;
        private Transform testPlayer;

        void Start()
        {
            InitializeTest();
            
            if (runAutomaticTests)
            {
                testCoroutine = StartCoroutine(RunAutomaticTests());
            }
        }

        void OnDestroy()
        {
            if (testCoroutine != null)
            {
                StopCoroutine(testCoroutine);
            }
        }

        /// <summary>
        /// テストを初期化
        /// </summary>
        private void InitializeTest()
        {
            pool = PrimitiveTerrainObjectPool.Instance;
            memoryManager = PrimitiveMemoryManager.Instance;
            
            // テスト用プレイヤーを作成
            var playerGO = new GameObject("TestPlayer");
            playerGO.tag = "Player";
            testPlayer = playerGO.transform;
            testPlayer.position = Vector3.zero;
            
            Debug.Log("LOD Memory System Test initialized");
        }

        /// <summary>
        /// 自動テストを実行
        /// </summary>
        private IEnumerator RunAutomaticTests()
        {
            while (true)
            {
                if (testLODSystem)
                {
                    yield return StartCoroutine(TestLODSystem());
                }
                
                if (testMemoryPool)
                {
                    yield return StartCoroutine(TestMemoryPool());
                }
                
                if (testPerformance)
                {
                    yield return StartCoroutine(TestPerformance());
                }
                
                yield return new WaitForSeconds(testInterval);
            }
        }

        /// <summary>
        /// LODシステムをテスト
        /// </summary>
        private IEnumerator TestLODSystem()
        {
            if (logTestResults)
            {
                Debug.Log("=== LOD System Test Started ===");
            }
            
            var testObjects = new PrimitiveTerrainObject[testObjectCount];
            
            // テストオブジェクトを作成
            for (int i = 0; i < testObjectCount; i++)
            {
                var position = new Vector3(i * 100f, 0, 0);
                var obj = pool.GetFromPool(PrimitiveTerrainGenerator.PrimitiveType.Cube, position, 50f);
                testObjects[i] = obj;
                
                if (obj != null)
                {
                    memoryManager.RegisterObject(obj);
                }
            }
            
            // 各距離でLODをテスト
            foreach (var distance in testDistances)
            {
                testPlayer.position = new Vector3(-distance, 0, 0);
                
                yield return new WaitForSeconds(0.5f); // LOD更新を待つ
                
                // LOD統計を取得
                var lodStats = PrimitiveTerrainObject.GetGlobalLODStatistics();
                
                if (logTestResults)
                {
                    Debug.Log($"Distance {distance}m: Visible={lodStats.visibleObjects}/{lodStats.totalObjects}, " +
                             $"LOD0={lodStats.lodCounts[0]}, LOD1={lodStats.lodCounts[1]}, LOD2={lodStats.lodCounts[2]}");
                }
            }
            
            // テストオブジェクトをクリーンアップ
            for (int i = 0; i < testObjects.Length; i++)
            {
                if (testObjects[i] != null)
                {
                    memoryManager.UnregisterObject(testObjects[i]);
                    pool.ReturnToPool(testObjects[i]);
                }
            }
            
            if (logTestResults)
            {
                Debug.Log("=== LOD System Test Completed ===");
            }
        }

        /// <summary>
        /// メモリプールをテスト
        /// </summary>
        private IEnumerator TestMemoryPool()
        {
            if (logTestResults)
            {
                Debug.Log("=== Memory Pool Test Started ===");
            }
            
            var initialStats = pool.GetPoolStatistics();
            var testObjects = new PrimitiveTerrainObject[poolStressTestCount];
            
            // 大量のオブジェクトを取得
            for (int i = 0; i < poolStressTestCount; i++)
            {
                var position = Random.insideUnitSphere * 1000f;
                var primitiveType = (PrimitiveTerrainGenerator.PrimitiveType)Random.Range(0, 8);
                var obj = pool.GetFromPool(primitiveType, position, Random.Range(10f, 100f));
                testObjects[i] = obj;
                
                if (i % 10 == 0)
                {
                    yield return null; // フレーム分散
                }
            }
            
            var afterGetStats = pool.GetPoolStatistics();
            
            // オブジェクトを返却
            for (int i = 0; i < testObjects.Length; i++)
            {
                if (testObjects[i] != null)
                {
                    pool.ReturnToPool(testObjects[i]);
                }
                
                if (i % 10 == 0)
                {
                    yield return null; // フレーム分散
                }
            }
            
            var finalStats = pool.GetPoolStatistics();
            
            if (logTestResults)
            {
                Debug.Log($"Pool Test Results:");
                Debug.Log($"  Initial: Active={initialStats.activeCount}, Available={initialStats.availableCount}");
                Debug.Log($"  After Get: Active={afterGetStats.activeCount}, Available={afterGetStats.availableCount}");
                Debug.Log($"  Final: Active={finalStats.activeCount}, Available={finalStats.availableCount}");
                Debug.Log($"  Reuse Ratio: {finalStats.reuseRatio:F2}");
            }
            
            if (logTestResults)
            {
                Debug.Log("=== Memory Pool Test Completed ===");
            }
        }

        /// <summary>
        /// パフォーマンステスト
        /// </summary>
        private IEnumerator TestPerformance()
        {
            if (logTestResults)
            {
                Debug.Log("=== Performance Test Started ===");
            }
            
            var startTime = Time.realtimeSinceStartup;
            var startMemory = System.GC.GetTotalMemory(false);
            
            // パフォーマンステストを実行
            for (int i = 0; i < performanceTestIterations; i++)
            {
                var obj = pool.GetFromPool(PrimitiveTerrainGenerator.PrimitiveType.Sphere, 
                                         Random.insideUnitSphere * 500f, Random.Range(20f, 80f));
                
                if (obj != null)
                {
                    memoryManager.RegisterObject(obj);
                    
                    // 短時間後に返却
                    yield return new WaitForSeconds(0.01f);
                    
                    memoryManager.UnregisterObject(obj);
                    pool.ReturnToPool(obj);
                }
                
                if (i % 20 == 0)
                {
                    yield return null; // フレーム分散
                }
            }
            
            var endTime = Time.realtimeSinceStartup;
            var endMemory = System.GC.GetTotalMemory(false);
            
            var totalTime = endTime - startTime;
            var memoryDelta = endMemory - startMemory;
            var avgTimePerOperation = totalTime / performanceTestIterations;
            
            if (logTestResults)
            {
                Debug.Log($"Performance Test Results:");
                Debug.Log($"  Total Time: {totalTime:F3}s");
                Debug.Log($"  Avg Time per Operation: {avgTimePerOperation * 1000:F3}ms");
                Debug.Log($"  Memory Delta: {memoryDelta / 1024}KB");
                Debug.Log($"  Operations per Second: {performanceTestIterations / totalTime:F1}");
            }
            
            if (logTestResults)
            {
                Debug.Log("=== Performance Test Completed ===");
            }
        }

        /// <summary>
        /// 手動テスト実行（インスペクターから呼び出し可能）
        /// </summary>
        [ContextMenu("Run LOD Test")]
        public void RunLODTest()
        {
            StartCoroutine(TestLODSystem());
        }

        [ContextMenu("Run Memory Pool Test")]
        public void RunMemoryPoolTest()
        {
            StartCoroutine(TestMemoryPool());
        }

        [ContextMenu("Run Performance Test")]
        public void RunPerformanceTest()
        {
            StartCoroutine(TestPerformance());
        }

        [ContextMenu("Force Memory Optimization")]
        public void ForceMemoryOptimization()
        {
            if (memoryManager != null)
            {
                memoryManager.ForceMemoryOptimization();
            }
        }

        [ContextMenu("Reset Pool")]
        public void ResetPool()
        {
            if (pool != null)
            {
                pool.ResetPool();
            }
        }

        /// <summary>
        /// 統計情報を表示
        /// </summary>
        void OnGUI()
        {
            if (!logTestResults) return;
            
            var rect = new Rect(10, 10, 400, 200);
            GUI.Box(rect, "LOD & Memory System Stats");
            
            var poolStats = pool?.GetPoolStatistics() ?? new PoolStatistics();
            var memoryStats = memoryManager?.GetPerformanceMetrics() ?? new PerformanceMetrics();
            var lodStats = PrimitiveTerrainObject.GetGlobalLODStatistics();
            
            var content = $"Pool: Active={poolStats.activeCount}, Available={poolStats.availableCount}\n" +
                         $"Memory: {memoryStats.memoryUsageMB:F1}MB, GC Calls={memoryStats.gcCallCount}\n" +
                         $"LOD: Visible={lodStats.visibleObjects}/{lodStats.totalObjects}\n" +
                         $"Reuse Ratio: {poolStats.reuseRatio:F2}";
            
            GUI.Label(new Rect(15, 35, 390, 160), content);
        }
    }
}