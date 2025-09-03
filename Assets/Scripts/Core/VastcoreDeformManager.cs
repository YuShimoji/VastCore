using System.Collections.Generic;
using UnityEngine;
using Vastcore.Utils;

#if DEFORM_AVAILABLE
using Deform;
#endif

namespace Vastcore.Core
{
    /// <summary>
    /// Vastcore統合Deform管理システム
    /// Deformパッケージの機能を統合し、プロジェクト全体のメッシュ変形を管理する
    /// </summary>
    public class VastcoreDeformManager : MonoBehaviour
    {
        [Header("システム設定")]
        [SerializeField] private bool enableDeformSystem = true;
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private int maxConcurrentDeformations = 10;
        
        [Header("品質設定")]
        [SerializeField] private DeformQualityLevel defaultQualityLevel = DeformQualityLevel.High;
        [SerializeField] private bool enableLODOptimization = true;
        [SerializeField] private float lodDistanceThreshold = 100f;
        
        [Header("パフォーマンス設定")]
        [SerializeField] private bool enableFrameDistribution = true;
        [SerializeField] private int maxDeformationsPerFrame = 3;
        [SerializeField] private float frameTimeLimit = 16.67f; // 60FPS target
        
        // シングルトンインスタンス
        private static VastcoreDeformManager instance;
        public static VastcoreDeformManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<VastcoreDeformManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("VastcoreDeformManager");
                        instance = go.AddComponent<VastcoreDeformManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
#if DEFORM_AVAILABLE
        // 管理対象のDeformable一覧
        private readonly List<Deformable> managedDeformables = new List<Deformable>();
        private readonly Queue<DeformRequest> deformQueue = new Queue<DeformRequest>();
        private readonly Dictionary<Deformable, DeformQualityLevel> qualityOverrides = new Dictionary<Deformable, DeformQualityLevel>();
#else
        // Deformパッケージが利用できない場合のダミー
        private readonly List<object> managedDeformables = new List<object>();
        private readonly Queue<object> deformQueue = new Queue<object>();
        private readonly Dictionary<object, DeformQualityLevel> qualityOverrides = new Dictionary<object, DeformQualityLevel>();
#endif
        
        // パフォーマンス監視
        private float frameStartTime;
        private int deformationsThisFrame;
        
        /// <summary>
        /// Deform品質レベル
        /// </summary>
        public enum DeformQualityLevel
        {
            Low = 0,
            Medium = 1,
            High = 2,
            Ultra = 3
        }
        
        /// <summary>
        /// Deform処理リクエスト
        /// </summary>
        private class DeformRequest
        {
#if DEFORM_AVAILABLE
            public Deformable target;
#else
            public object target;
#endif
            public DeformQualityLevel quality;
            public System.Action<bool> onComplete;
            public float priority;
            
#if DEFORM_AVAILABLE
            public DeformRequest(Deformable target, DeformQualityLevel quality, float priority = 1f, System.Action<bool> onComplete = null)
#else
            public DeformRequest(object target, DeformQualityLevel quality, float priority = 1f, System.Action<bool> onComplete = null)
#endif
            {
                this.target = target;
                this.quality = quality;
                this.priority = priority;
                this.onComplete = onComplete;
            }
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeSystem();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Update()
        {
            if (!enableDeformSystem) return;
            
            frameStartTime = Time.realtimeSinceStartup;
            deformationsThisFrame = 0;
            
            ProcessDeformQueue();
            
            if (enableLODOptimization)
            {
                UpdateLODOptimization();
            }
        }
        
        /// <summary>
        /// システム初期化
        /// </summary>
        private void InitializeSystem()
        {
            VastcoreLogger.Log("VastcoreDeformManager initialized", VastcoreLogger.LogLevel.Info);
            
            // Deformパッケージの依存関係チェック
            if (!CheckDeformDependencies())
            {
                VastcoreLogger.Log("Deform package dependencies not found. Disabling deform system.", VastcoreLogger.LogLevel.Warning);
                enableDeformSystem = false;
                return;
            }
            
            // デフォルトDeformableManagerの設定
            var defaultManager = DeformableManager.GetDefaultManager(true);
            if (defaultManager != null)
            {
                VastcoreLogger.Log("Default DeformableManager configured", VastcoreLogger.LogLevel.Info);
            }
        }
        
        /// <summary>
        /// Deformパッケージの依存関係をチェック
        /// </summary>
        private bool CheckDeformDependencies()
        {
#if DEFORM_AVAILABLE
            try
            {
                // Deform名前空間のクラスにアクセスしてみる
                var testManager = DeformableManager.GetDefaultManager(false);
                return true;
            }
            catch (System.Exception ex)
            {
                VastcoreLogger.Log($"Deform dependency check failed: {ex.Message}", VastcoreLogger.LogLevel.Error);
                return false;
            }
#else
            VastcoreLogger.Log("Deform package not available. Deform system disabled.", VastcoreLogger.LogLevel.Warning);
            return false;
#endif
        }
        
        /// <summary>
        /// Deformableを管理対象に登録
        /// </summary>
#if DEFORM_AVAILABLE
        public void RegisterDeformable(Deformable deformable, DeformQualityLevel qualityLevel = DeformQualityLevel.High)
#else
        public void RegisterDeformable(object deformable, DeformQualityLevel qualityLevel = DeformQualityLevel.High)
#endif
        {
            if (!enableDeformSystem || deformable == null) return;
            
            if (!managedDeformables.Contains(deformable))
            {
                managedDeformables.Add(deformable);
                qualityOverrides[deformable] = qualityLevel;
                
                // 品質レベルに応じた設定を適用
                ApplyQualitySettings(deformable, qualityLevel);
                
#if DEFORM_AVAILABLE
                VastcoreLogger.Log($"Registered Deformable: {deformable.name} with quality {qualityLevel}", VastcoreLogger.LogLevel.Debug);
#else
                VastcoreLogger.Log($"Registered object with quality {qualityLevel}", VastcoreLogger.LogLevel.Debug);
#endif
            }
        }
        
        /// <summary>
        /// Deformableの登録解除
        /// </summary>
#if DEFORM_AVAILABLE
        public void UnregisterDeformable(Deformable deformable)
#else
        public void UnregisterDeformable(object deformable)
#endif
        {
            if (deformable == null) return;
            
            managedDeformables.Remove(deformable);
            qualityOverrides.Remove(deformable);
            
#if DEFORM_AVAILABLE
            VastcoreLogger.Log($"Unregistered Deformable: {deformable.name}", VastcoreLogger.LogLevel.Debug);
#else
            VastcoreLogger.Log("Unregistered object", VastcoreLogger.LogLevel.Debug);
#endif
        }
        
        /// <summary>
        /// Deform処理をキューに追加
        /// </summary>
#if DEFORM_AVAILABLE
        public void QueueDeformation(Deformable target, DeformQualityLevel quality = DeformQualityLevel.High, float priority = 1f, System.Action<bool> onComplete = null)
#else
        public void QueueDeformation(object target, DeformQualityLevel quality = DeformQualityLevel.High, float priority = 1f, System.Action<bool> onComplete = null)
#endif
        {
            if (!enableDeformSystem || target == null) return;
            
            var request = new DeformRequest(target, quality, priority, onComplete);
            deformQueue.Enqueue(request);
        }
        
        /// <summary>
        /// Deformキューの処理
        /// </summary>
        private void ProcessDeformQueue()
        {
            while (deformQueue.Count > 0 && CanProcessMoreDeformations())
            {
                var request = deformQueue.Dequeue();
                ProcessDeformRequest(request);
            }
        }
        
        /// <summary>
        /// フレーム内でさらにDeform処理が可能かチェック
        /// </summary>
        private bool CanProcessMoreDeformations()
        {
            if (!enableFrameDistribution) return true;
            
            bool withinFrameLimit = deformationsThisFrame < maxDeformationsPerFrame;
            bool withinTimeLimit = (Time.realtimeSinceStartup - frameStartTime) * 1000f < frameTimeLimit;
            
            return withinFrameLimit && withinTimeLimit;
        }
        
        /// <summary>
        /// Deformリクエストの処理
        /// </summary>
        private void ProcessDeformRequest(DeformRequest request)
        {
            if (request.target == null)
            {
                request.onComplete?.Invoke(false);
                return;
            }
            
            try
            {
                ApplyQualitySettings(request.target, request.quality);
                deformationsThisFrame++;
                
                if (enablePerformanceMonitoring)
                {
#if DEFORM_AVAILABLE
                    using (LoadProfiler.Measure($"Deform Processing: {request.target.name}"))
#else
                    using (LoadProfiler.Measure("Deform Processing (dummy)"))
#endif
                    {
#if DEFORM_AVAILABLE
                        // Deform処理の実行
                        request.target.Complete();
                        request.target.Schedule();
#endif
                    }
                }
                else
                {
#if DEFORM_AVAILABLE
                    request.target.Complete();
                    request.target.Schedule();
#endif
                }
                
                request.onComplete?.Invoke(true);
#if DEFORM_AVAILABLE
                VastcoreLogger.Log($"Processed deformation for: {request.target.name}", VastcoreLogger.LogLevel.Debug);
#else
                VastcoreLogger.Log("Processed deformation (dummy)", VastcoreLogger.LogLevel.Debug);
#endif
            }
            catch (System.Exception ex)
            {
#if DEFORM_AVAILABLE
                VastcoreLogger.Log($"Deformation failed for {request.target.name}: {ex.Message}", VastcoreLogger.LogLevel.Error);
#else
                VastcoreLogger.Log($"Deformation failed: {ex.Message}", VastcoreLogger.LogLevel.Error);
#endif
                request.onComplete?.Invoke(false);
            }
        }
        
        /// <summary>
        /// 品質設定の適用
        /// </summary>
#if DEFORM_AVAILABLE
        private void ApplyQualitySettings(Deformable deformable, DeformQualityLevel quality)
#else
        private void ApplyQualitySettings(object deformable, DeformQualityLevel quality)
#endif
        {
            if (deformable == null) return;
            
#if DEFORM_AVAILABLE
            switch (quality)
            {
                case DeformQualityLevel.Low:
                    deformable.UpdateMode = UpdateMode.Stop;
                    break;
                case DeformQualityLevel.Medium:
                    deformable.UpdateMode = UpdateMode.Custom;
                    break;
                case DeformQualityLevel.High:
                case DeformQualityLevel.Ultra:
                    deformable.UpdateMode = UpdateMode.Auto;
                    break;
            }
#else
            // Deformパッケージが利用できない場合は何もしない
            VastcoreLogger.Log($"Quality settings applied (dummy): {quality}", VastcoreLogger.LogLevel.Debug);
#endif
        }
        
        /// <summary>
        /// LOD最適化の更新
        /// </summary>
        private void UpdateLODOptimization()
        {
            var cameraPosition = Camera.main?.transform.position ?? Vector3.zero;
            
#if DEFORM_AVAILABLE
            foreach (var deformable in managedDeformables)
            {
                if (deformable == null) continue;
                
                float distance = Vector3.Distance(cameraPosition, deformable.transform.position);
                DeformQualityLevel targetQuality = CalculateLODQuality(distance);
                
                if (qualityOverrides.ContainsKey(deformable) && qualityOverrides[deformable] != targetQuality)
                {
                    qualityOverrides[deformable] = targetQuality;
                    ApplyQualitySettings(deformable, targetQuality);
                }
            }
#endif
        }
        
        /// <summary>
        /// 距離に基づくLOD品質の計算
        /// </summary>
        private DeformQualityLevel CalculateLODQuality(float distance)
        {
            if (distance < lodDistanceThreshold * 0.25f)
                return DeformQualityLevel.Ultra;
            else if (distance < lodDistanceThreshold * 0.5f)
                return DeformQualityLevel.High;
            else if (distance < lodDistanceThreshold)
                return DeformQualityLevel.Medium;
            else
                return DeformQualityLevel.Low;
        }
        
        /// <summary>
        /// 統計情報の取得
        /// </summary>
        public DeformStats GetStats()
        {
            return new DeformStats
            {
                managedDeformablesCount = managedDeformables.Count,
                queuedRequestsCount = deformQueue.Count,
                deformationsThisFrame = deformationsThisFrame,
                systemEnabled = enableDeformSystem
            };
        }
        
        /// <summary>
        /// Deformシステムの統計情報
        /// </summary>
        public struct DeformStats
        {
            public int managedDeformablesCount;
            public int queuedRequestsCount;
            public int deformationsThisFrame;
            public bool systemEnabled;
        }
        
        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
