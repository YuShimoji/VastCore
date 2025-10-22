using UnityEngine;
using Vastcore.Generation.GPU;
using Vastcore.Generation.Cache;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Vastcore.Generation.Optimization
{
    /// <summary>
    /// パフォーマンス最適化統合コントローラー
    /// GPU並列処理、インテリジェントキャッシュ、負荷分散を統合管理
    /// </summary>
    public class PerformanceOptimizationController : MonoBehaviour
    {
        [Header("最適化設定")]
        [SerializeField] private bool enableAutoOptimization = true;
        [SerializeField] private float optimizationInterval = 5f;
        [SerializeField] private float targetFrameRate = 60f;
        [SerializeField] private float performanceThreshold = 0.8f;
        
        [Header("GPU最適化")]
        [SerializeField] private bool enableGPUOptimization = true;
        [SerializeField] private int minTextureResolution = 256;
        [SerializeField] private int maxTextureResolution = 1024;
        [SerializeField] private int minConcurrentGenerations = 1;
        [SerializeField] private int maxConcurrentGenerations = 8;
        
        [Header("キャッシュ最適化")]
        [SerializeField] private bool enableCacheOptimization = true;
        [SerializeField] private float minCacheHitRatio = 0.6f;
        [SerializeField] private float maxMemoryUsageRatio = 0.8f;
        
        [Header("負荷分散")]
        [SerializeField] private bool enableLoadBalancing = true;
        [SerializeField] private int maxTasksPerFrame = 3;
        [SerializeField] private float maxFrameTimeMs = 16.67f;
        
        // コンポーネント参照
        private GPUTerrainGenerator gpuGenerator;
        private GPUPerformanceMonitor gpuMonitor;
        private IntelligentCacheSystem cacheSystem;
        private TerrainCacheManager cacheManager;
        private RuntimeTerrainManager terrainManager;
        
        // 最適化状態
        private OptimizationState currentState;
        private float lastOptimizationTime;
        private PerformanceMetrics currentMetrics;
        
        public enum OptimizationState
        {
            Optimal,        // 最適状態
            Degraded,       // 性能低下
            Critical,       // 重大な性能問題
            Recovery        // 回復中
        }
        
        public struct PerformanceMetrics
        {
            public float frameRate;
            public float frameTime;
            public float gpuMemoryUsage;
            public float cacheHitRatio;
            public int activeGenerations;
            public int queuedTasks;
            public OptimizationState state;
        }
        
        private void Awake()
        {
            InitializeComponents();
            currentState = OptimizationState.Optimal;
        }
        
        private void InitializeComponents()
        {
            // GPU コンポーネント
            gpuGenerator = FindFirstObjectByType<GPUTerrainGenerator>();
            gpuMonitor = FindFirstObjectByType<GPUPerformanceMonitor>();
            
            if (gpuGenerator == null)
            {
                Debug.LogWarning("GPUTerrainGenerator not found. GPU optimization disabled.");
                enableGPUOptimization = false;
            }
            
            // キャッシュコンポーネント
            cacheSystem = FindFirstObjectByType<IntelligentCacheSystem>();
            cacheManager = FindFirstObjectByType<TerrainCacheManager>();
            
            if (cacheSystem == null)
            {
                Debug.LogWarning("IntelligentCacheSystem not found. Cache optimization disabled.");
                enableCacheOptimization = false;
            }
            
            // 地形管理コンポーネント
            terrainManager = FindFirstObjectByType<RuntimeTerrainManager>();
            
            Debug.Log("Performance Optimization Controller initialized");
        }
        
        private void Update()
        {
            if (enableAutoOptimization && Time.time - lastOptimizationTime > optimizationInterval)
            {
                PerformOptimizationCycle();
                lastOptimizationTime = Time.time;
            }
        }
        
        /// <summary>
        /// 最適化サイクルの実行
        /// </summary>
        public void PerformOptimizationCycle()
        {
            // パフォーマンス指標の収集
            CollectPerformanceMetrics();
            
            // 状態の評価
            EvaluatePerformanceState();
            
            // 最適化の実行
            switch (currentState)
            {
                case OptimizationState.Optimal:
                    // 予防的最適化
                    PerformPreventiveOptimization();
                    break;
                    
                case OptimizationState.Degraded:
                    // 軽度の最適化
                    PerformLightOptimization();
                    break;
                    
                case OptimizationState.Critical:
                    // 緊急最適化
                    PerformEmergencyOptimization();
                    break;
                    
                case OptimizationState.Recovery:
                    // 回復処理
                    PerformRecoveryOptimization();
                    break;
            }
            
            LogOptimizationResults();
        }
        
        private void CollectPerformanceMetrics()
        {
            currentMetrics = new PerformanceMetrics
            {
                frameRate = 1f / Time.deltaTime,
                frameTime = Time.deltaTime * 1000f,
                state = currentState
            };
            
            // GPU メトリクス
            if (enableGPUOptimization && gpuGenerator != null)
            {
                var gpuInfo = gpuGenerator.GetPerformanceInfo();
                currentMetrics.activeGenerations = gpuInfo.activeGenerations;
                currentMetrics.queuedTasks = gpuInfo.queuedGenerations;
                
                if (gpuMonitor != null)
                {
                    var gpuStats = gpuMonitor.GetCurrentStats();
                    currentMetrics.gpuMemoryUsage = gpuStats.averageGPUMemory;
                }
            }
            
            // キャッシュメトリクス
            if (enableCacheOptimization && cacheSystem != null)
            {
                var cacheStats = cacheSystem.GetStatistics();
                currentMetrics.cacheHitRatio = cacheStats.hitRatio;
            }
        }
        
        private void EvaluatePerformanceState()
        {
            float performanceScore = CalculatePerformanceScore();
            
            OptimizationState newState = currentState;
            
            if (performanceScore >= 0.9f)
            {
                newState = OptimizationState.Optimal;
            }
            else if (performanceScore >= 0.7f)
            {
                newState = OptimizationState.Degraded;
            }
            else if (performanceScore >= 0.4f)
            {
                newState = OptimizationState.Critical;
            }
            else
            {
                newState = OptimizationState.Recovery;
            }
            
            if (newState != currentState)
            {
                Debug.Log($"Performance state changed: {currentState} -> {newState} (Score: {performanceScore:F2})");
                currentState = newState;
            }
        }
        
        private float CalculatePerformanceScore()
        {
            float score = 1f;
            
            // フレームレート評価
            float frameRateRatio = currentMetrics.frameRate / targetFrameRate;
            score *= Mathf.Clamp01(frameRateRatio);
            
            // GPU メモリ評価
            if (currentMetrics.gpuMemoryUsage > 0)
            {
                float memoryRatio = 1f - (currentMetrics.gpuMemoryUsage / 1024f); // 1GB基準
                score *= Mathf.Clamp01(memoryRatio);
            }
            
            // キャッシュ効率評価
            if (currentMetrics.cacheHitRatio > 0)
            {
                score *= currentMetrics.cacheHitRatio;
            }
            
            // 負荷評価
            if (currentMetrics.activeGenerations > 0)
            {
                float loadRatio = 1f - ((float)currentMetrics.activeGenerations / maxConcurrentGenerations);
                score *= Mathf.Clamp01(loadRatio);
            }
            
            return score;
        }
        
        private void PerformPreventiveOptimization()
        {
            // 予防的最適化: 小さな調整
            if (enableCacheOptimization && cacheManager != null)
            {
                cacheManager.OptimizeCacheEfficiency();
            }
        }
        
        private void PerformLightOptimization()
        {
            Debug.Log("Performing light optimization");
            
            // GPU最適化
            if (enableGPUOptimization && currentMetrics.frameTime > maxFrameTimeMs * 1.2f)
            {
                OptimizeGPUSettings(0.9f); // 10%削減
            }
            
            // キャッシュ最適化
            if (enableCacheOptimization && currentMetrics.cacheHitRatio < minCacheHitRatio)
            {
                OptimizeCacheSettings(1.1f); // 10%増加
            }
        }
        
        private void PerformEmergencyOptimization()
        {
            Debug.LogWarning("Performing emergency optimization");
            
            // 大幅なGPU設定削減
            if (enableGPUOptimization)
            {
                OptimizeGPUSettings(0.7f); // 30%削減
            }
            
            // キャッシュクリーンアップ
            if (enableCacheOptimization && cacheSystem != null)
            {
                StartCoroutine(EmergencyCacheCleanup());
            }
            
            // 負荷分散の強化
            if (enableLoadBalancing)
            {
                maxTasksPerFrame = Mathf.Max(1, maxTasksPerFrame / 2);
            }
        }
        
        private void PerformRecoveryOptimization()
        {
            Debug.Log("Performing recovery optimization");
            
            // 段階的な設定復旧
            if (currentMetrics.frameRate > targetFrameRate * 0.8f)
            {
                // GPU設定の段階的復旧
                if (enableGPUOptimization)
                {
                    OptimizeGPUSettings(1.05f); // 5%増加
                }
                
                // タスク数の段階的復旧
                if (enableLoadBalancing)
                {
                    maxTasksPerFrame = Mathf.Min(maxTasksPerFrame + 1, 3);
                }
            }
        }
        
        private void OptimizeGPUSettings(float factor)
        {
            if (gpuGenerator == null) return;
            
            var perfInfo = gpuGenerator.GetPerformanceInfo();
            
            // 同時生成数の調整
            int newConcurrent = Mathf.RoundToInt(perfInfo.maxConcurrentGenerations * factor);
            newConcurrent = Mathf.Clamp(newConcurrent, minConcurrentGenerations, maxConcurrentGenerations);
            
            // テクスチャ解像度の調整
            int newResolution = Mathf.RoundToInt(perfInfo.textureResolution * factor);
            newResolution = Mathf.Clamp(newResolution, minTextureResolution, maxTextureResolution);
            
            Debug.Log($"GPU optimization: Concurrent={newConcurrent}, Resolution={newResolution}");
        }
        
        private void OptimizeCacheSettings(float factor)
        {
            if (cacheSystem == null) return;
            
            // キャッシュ設定の調整（実装は IntelligentCacheSystem に依存）
            Debug.Log($"Cache optimization: Factor={factor}");
        }
        
        private IEnumerator EmergencyCacheCleanup()
        {
            yield return null;
            
            if (cacheSystem != null)
            {
                // メモリキャッシュの部分クリア
                Debug.Log("Emergency cache cleanup initiated");
                // cacheSystem.ClearCache(false); // メモリのみクリア
            }
        }
        
        private void LogOptimizationResults()
        {
            Debug.Log($"Optimization Results:\n" +
                     $"State: {currentState}\n" +
                     $"Frame Rate: {currentMetrics.frameRate:F1} FPS\n" +
                     $"Frame Time: {currentMetrics.frameTime:F2} ms\n" +
                     $"GPU Memory: {currentMetrics.gpuMemoryUsage:F1} MB\n" +
                     $"Cache Hit Ratio: {currentMetrics.cacheHitRatio:F2}\n" +
                     $"Active Generations: {currentMetrics.activeGenerations}");
        }
        
        /// <summary>
        /// 手動最適化の実行
        /// </summary>
        public void ForceOptimization()
        {
            PerformOptimizationCycle();
        }
        
        /// <summary>
        /// 最適化設定のリセット
        /// </summary>
        public void ResetOptimizationSettings()
        {
            currentState = OptimizationState.Optimal;
            maxTasksPerFrame = 3;
            
            Debug.Log("Optimization settings reset to defaults");
        }
        
        /// <summary>
        /// 現在のパフォーマンス指標を取得
        /// </summary>
        public PerformanceMetrics GetCurrentMetrics()
        {
            return currentMetrics;
        }
        
        /// <summary>
        /// 最適化状態の取得
        /// </summary>
        public OptimizationState GetOptimizationState()
        {
            return currentState;
        }
        
        private void OnGUI()
        {
#if UNITY_EDITOR
            if (!enableAutoOptimization) return;
            
            GUILayout.BeginArea(new Rect(Screen.width - 320, 10, 300, 250));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Performance Optimization", EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).label);
            GUILayout.Space(5);
            
            // 状態表示
            Color originalColor = GUI.color;
            switch (currentState)
            {
                case OptimizationState.Optimal:
                    GUI.color = Color.green;
                    break;
                case OptimizationState.Degraded:
                    GUI.color = Color.yellow;
                    break;
                case OptimizationState.Critical:
                    GUI.color = Color.red;
                    break;
                case OptimizationState.Recovery:
                    GUI.color = Color.cyan;
                    break;
            }
            GUILayout.Label($"State: {currentState}");
            GUI.color = originalColor;
            
            GUILayout.Space(5);
            
            // メトリクス表示
            GUILayout.Label($"FPS: {currentMetrics.frameRate:F1} / {targetFrameRate:F1}");
            GUILayout.Label($"Frame Time: {currentMetrics.frameTime:F2} ms");
            GUILayout.Label($"GPU Memory: {currentMetrics.gpuMemoryUsage:F1} MB");
            GUILayout.Label($"Cache Hit: {currentMetrics.cacheHitRatio:F2}");
            GUILayout.Label($"Active Gen: {currentMetrics.activeGenerations}");
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Force Optimization"))
            {
                ForceOptimization();
            }
            
            if (GUILayout.Button("Reset Settings"))
            {
                ResetOptimizationSettings();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
#endif
        }
    }
}