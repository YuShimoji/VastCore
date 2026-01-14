using System.Collections.Generic;
using UnityEngine;
using Vastcore.Utils;

namespace Vastcore.UI
{
    /// <summary>
    /// UIリアルタイム最適化マネージャー
    /// UIコンポーネントのリアルタイム更新とパフォーマンス最適化を管理
    /// </summary>
    public class UIRealtimeManager : MonoBehaviour
    {
        #region Singleton
        private static UIRealtimeManager _instance;
        public static UIRealtimeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<UIRealtimeManager>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("UIRealtimeManager");
                        _instance = obj.AddComponent<UIRealtimeManager>();
                        DontDestroyOnLoad(obj);
                    }
                }
                return _instance;
            }
        }
        #endregion

        [Header("リアルタイム設定")]
        [SerializeField] private float updateInterval = 1f / 60f; // 60FPS
        [SerializeField] private int maxUIUpdatesPerFrame = 10;
        [SerializeField] private bool enableAdaptiveQuality = true;
        [SerializeField] private float targetFrameTime = 16.67f; // 60FPS in ms

        [Header("パフォーマンス設定")]
        [SerializeField] private int maxActiveUIElements = 50;
        [SerializeField] private float memoryThresholdMB = 100f;
        [SerializeField] private float cpuThresholdPercent = 80f;

        // UIコンポーネント管理
        private List<IRealtimeUIComponent> activeComponents = new List<IRealtimeUIComponent>();
        private Queue<IRealtimeUIComponent> updateQueue = new Queue<IRealtimeUIComponent>();
        private Dictionary<string, UIComponentPool> componentPools = new Dictionary<string, UIComponentPool>();

        // パフォーマンス監視
        private float lastUpdateTime;
        private int updatesThisFrame;
        private PerformanceMetrics currentMetrics;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            VastcoreLogger.Instance.LogInfo("UIRealtimeManager", "Initializing UI realtime optimization system");

            // パフォーマンス監視の初期化
            currentMetrics = new PerformanceMetrics();

            // 基本的なコンポーネントプールの作成
            CreateComponentPools();

            VastcoreLogger.Instance.LogInfo("UIRealtimeManager", $"UI realtime manager initialized with {componentPools.Count} component pools");
        }

        private void CreateComponentPools()
        {
            // 基本的なUIコンポーネントプールを作成
            componentPools["UIText"] = new UIComponentPool("UIText", 20);
            componentPools["UIImage"] = new UIComponentPool("UIImage", 15);
            componentPools["UIButton"] = new UIComponentPool("UIButton", 10);
            componentPools["UISlider"] = new UIComponentPool("UISlider", 5);
        }

        private void Update()
        {
            float currentTime = Time.time;

            if (currentTime - lastUpdateTime >= updateInterval)
            {
                ProcessRealtimeUpdates();
                UpdatePerformanceMetrics();
                lastUpdateTime = currentTime;
                updatesThisFrame = 0;
            }

            // アダプティブクオリティ調整
            if (enableAdaptiveQuality)
            {
                AdjustQualityBasedOnPerformance();
            }
        }

        private void ProcessRealtimeUpdates()
        {
            // キューからUIコンポーネントを更新
            while (updateQueue.Count > 0 && updatesThisFrame < maxUIUpdatesPerFrame)
            {
                IRealtimeUIComponent component = updateQueue.Dequeue();
                if (component != null && component.IsActive)
                {
                    component.UpdateRealtime();
                    updatesThisFrame++;
                }
            }
        }

        private void UpdatePerformanceMetrics()
        {
            currentMetrics.frameTime = Time.deltaTime * 1000f; // ms
            currentMetrics.activeUIElements = activeComponents.Count;
            currentMetrics.memoryUsage = GetMemoryUsage();
            currentMetrics.cpuUsage = GetCPUUsage();

            // パフォーマンス警告
            if (currentMetrics.frameTime > targetFrameTime * 1.5f)
            {
                VastcoreLogger.Instance.LogWarning("UIRealtimeManager", $"Frame time exceeded target: {currentMetrics.frameTime:F2}ms");
            }

            if (currentMetrics.memoryUsage > memoryThresholdMB)
            {
                VastcoreLogger.Instance.LogWarning("UIRealtimeManager", $"Memory usage high: {currentMetrics.memoryUsage:F1}MB");
            }
        }

        private void AdjustQualityBasedOnPerformance()
        {
            if (currentMetrics.frameTime > targetFrameTime * 1.2f)
            {
                // パフォーマンスが悪い場合、更新間隔を増加
                updateInterval = Mathf.Min(updateInterval * 1.1f, 1f / 30f); // 最低30FPS
                VastcoreLogger.Instance.LogInfo("UIRealtimeManager", $"Reduced update frequency to maintain performance: {1f/updateInterval:F1} FPS");
            }
            else if (currentMetrics.frameTime < targetFrameTime * 0.8f && updateInterval > 1f / 60f)
            {
                // パフォーマンスに余裕がある場合、更新間隔を減少
                updateInterval = Mathf.Max(updateInterval * 0.9f, 1f / 60f);
                VastcoreLogger.Instance.LogInfo("UIRealtimeManager", $"Increased update frequency: {1f/updateInterval:F1} FPS");
            }
        }

        /// <summary>
        /// リアルタイムUIコンポーネントを登録
        /// </summary>
        public void RegisterComponent(IRealtimeUIComponent component)
        {
            if (!activeComponents.Contains(component))
            {
                activeComponents.Add(component);
                VastcoreLogger.Instance.LogInfo("UIRealtimeManager", $"Registered realtime UI component: {component.GetType().Name}");
            }
        }

        /// <summary>
        /// リアルタイムUIコンポーネントを解除
        /// </summary>
        public void UnregisterComponent(IRealtimeUIComponent component)
        {
            activeComponents.Remove(component);
            VastcoreLogger.Instance.LogInfo("UIRealtimeManager", $"Unregistered realtime UI component: {component.GetType().Name}");
        }

        /// <summary>
        /// UI更新をキューに追加
        /// </summary>
        public void QueueUpdate(IRealtimeUIComponent component)
        {
            if (!updateQueue.Contains(component))
            {
                updateQueue.Enqueue(component);
            }
        }

        /// <summary>
        /// UIコンポーネントをプールから取得
        /// </summary>
        public GameObject GetPooledComponent(string componentType)
        {
            if (componentPools.TryGetValue(componentType, out UIComponentPool pool))
            {
                return pool.GetPooledObject();
            }

            VastcoreLogger.Instance.LogWarning("UIRealtimeManager", $"No pool found for component type: {componentType}");
            return null;
        }

        /// <summary>
        /// UIコンポーネントをプールに返却
        /// </summary>
        public void ReturnToPool(string componentType, GameObject obj)
        {
            if (componentPools.TryGetValue(componentType, out UIComponentPool pool))
            {
                pool.ReturnToPool(obj);
            }
        }

        /// <summary>
        /// 現在のメモリ使用量を取得
        /// </summary>
        private float GetMemoryUsage()
        {
            // Unityのメモリ統計を取得
            return UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver() / (1024f * 1024f);
        }

        /// <summary>
        /// 現在のCPU使用率を取得
        /// </summary>
        private float GetCPUUsage()
        {
            // 簡易的なCPU使用率推定
            return (Time.deltaTime / Time.fixedDeltaTime) * 100f;
        }

        /// <summary>
        /// パフォーマンス指標を取得
        /// </summary>
        public PerformanceMetrics GetPerformanceMetrics()
        {
            return currentMetrics;
        }

        /// <summary>
        /// UIシステムの統計情報を取得
        /// </summary>
        public UIStatistics GetStatistics()
        {
            return new UIStatistics
            {
                activeComponents = activeComponents.Count,
                queuedUpdates = updateQueue.Count,
                updateInterval = updateInterval,
                targetFPS = 1f / updateInterval,
                performanceMetrics = currentMetrics
            };
        }

        /// <summary>
        /// UIシステムをリセット
        /// </summary>
        public void ResetSystem()
        {
            activeComponents.Clear();
            updateQueue.Clear();

            foreach (var pool in componentPools.Values)
            {
                pool.Reset();
            }

            updateInterval = 1f / 60f;
            lastUpdateTime = 0f;
            updatesThisFrame = 0;

            VastcoreLogger.Instance.LogInfo("UIRealtimeManager", "UI realtime system reset");
        }
    }

    /// <summary>
    /// パフォーマンス指標
    /// </summary>
    [System.Serializable]
    public struct PerformanceMetrics
    {
        public float frameTime; // ms
        public int activeUIElements;
        public float memoryUsage; // MB
        public float cpuUsage; // %
    }

    /// <summary>
    /// UI統計情報
    /// </summary>
    [System.Serializable]
    public struct UIStatistics
    {
        public int activeComponents;
        public int queuedUpdates;
        public float updateInterval;
        public float targetFPS;
        public PerformanceMetrics performanceMetrics;
    }
}
