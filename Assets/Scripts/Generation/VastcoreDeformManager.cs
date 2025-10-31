using System.Collections.Generic;
using UnityEngine;
using Vastcore.Utils;

#if DEFORM_AVAILABLE
using Deform;
#endif

namespace Vastcore.Generation
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
                    instance = FindFirstObjectByType<VastcoreDeformManager>();
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
            public object target;
            public DeformQualityLevel quality;
            public System.Action<bool> onComplete;
            public float priority;
            
            public DeformRequest(object target, DeformQualityLevel quality, float priority = 1f, System.Action<bool> onComplete = null)
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
            VastcoreLogger.Instance.LogInfo("VastcoreDeformManager", "VastcoreDeformManager initialized");
            
            // Deformパッケージの依存関係チェック
            if (!CheckDeformDependencies())
            {
                VastcoreLogger.Instance.LogWarning("VastcoreDeformManager", "Deform package dependencies not found. Disabling deform system.");
                enableDeformSystem = false;
                return;
            }
            
#if DEFORM_AVAILABLE
                // Deformパッケージが利用できない場合のダミー処理
                var defaultManager = new object(); // ダミーオブジェクト
#endif
        }
        
        /// <summary>
        /// Deformパッケージの依存関係をチェック
        /// </summary>
        private bool CheckDeformDependencies()
        {
#if DEFORM_AVAILABLE
            try
            {
                // Deformパッケージの基本クラスにアクセス
                var testType = System.Type.GetType("Deform.Deformable, Assembly-CSharp");
                if (testType != null)
                {
                    return true;
                }
                else
                {
                    VastcoreLogger.Instance.LogWarning("VastcoreDeformManager", "Deform.Deformable type not found");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                VastcoreLogger.Instance.LogError("VastcoreDeformManager", $"Deform dependency check failed: {ex.Message}");
                return false;
            }
#else
            VastcoreLogger.Instance.LogWarning("VastcoreDeformManager", "Deform package not available. Deform system disabled.");
            return false;
#endif
        }
        
        /// <summary>
        /// Deformableを管理対象に登録
        /// </summary>
        public void RegisterDeformable(object deformable, DeformQualityLevel qualityLevel = DeformQualityLevel.High)
        {
            if (!enableDeformSystem || deformable == null) return;

            // 最大同時変形数のチェック
            if (managedDeformables.Count >= maxConcurrentDeformations)
            {
                VastcoreLogger.Instance.LogWarning("VastcoreDeformManager", $"Maximum concurrent deformations reached: {maxConcurrentDeformations}");
                return;
            }

            if (!managedDeformables.Contains(deformable))
            {
                managedDeformables.Add(deformable);
                // デフォルト品質レベルを使用
                qualityOverrides[deformable] = qualityLevel == DeformQualityLevel.High ? defaultQualityLevel : qualityLevel;

                // 品質レベルに応じた設定を適用
                ApplyQualitySettings(deformable, qualityOverrides[deformable]);

#if DEFORM_AVAILABLE
                if (deformable is Deformable deformableComponent)
                {
                    VastcoreLogger.Instance.LogDebug("VastcoreDeformManager", $"Registered Deformable: {deformableComponent.name} with quality {qualityOverrides[deformable]}");
                }
                else
#endif
                {
                    VastcoreLogger.Instance.LogDebug("VastcoreDeformManager", $"Registered object with quality {qualityOverrides[deformable]}");
                }
            }
        }
        
        /// <summary>
        /// Deformableの登録解除
        /// </summary>
        public void UnregisterDeformable(object deformable)
        {
            if (deformable == null) return;
            
            managedDeformables.Remove(deformable);
            qualityOverrides.Remove(deformable);
            
#if DEFORM_AVAILABLE
            if (deformable is Deformable deformableComponent)
            {
                VastcoreLogger.Instance.LogDebug("VastcoreDeformManager", $"Unregistered Deformable: {deformableComponent.name}");
            }
            else
#endif
            {
                VastcoreLogger.Instance.LogDebug("VastcoreDeformManager", "Unregistered object");
            }
        }
        
        /// <summary>
        /// Deform処理をキューに追加
        /// </summary>
        public void QueueDeformation(object target, DeformQualityLevel quality = DeformQualityLevel.High, float priority = 1f, System.Action<bool> onComplete = null)
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
        private void ProcessDeformRequest(object requestObj)
        {
            // 型チェックとキャスト
            if (!(requestObj is DeformRequest request))
            {
                VastcoreLogger.Instance.LogError("VastcoreDeformManager", "Invalid request type");
                return;
            }

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
                    if (request.target is Deformable deformableTarget)
                    {
                        using (LoadProfiler.Measure($"Deform Processing: {deformableTarget.name}"))
                        {
                            // Deform処理の実行
                            deformableTarget.Complete();
                            deformableTarget.Schedule();
                        }
                    }
                    else
#endif
                    {
                        using (LoadProfiler.Measure("Deform Processing (dummy)"))
                        {
                            // ダミー処理
                        }
                    }
                }
                else
                {
#if DEFORM_AVAILABLE
                    if (request.target is Deformable deformableTarget)
                    {
                        deformableTarget.Complete();
                        deformableTarget.Schedule();
                    }
#endif
                }

                request.onComplete?.Invoke(true);
#if DEFORM_AVAILABLE
                if (request.target is Deformable deformableTarget)
                {
                    VastcoreLogger.Instance.LogDebug("VastcoreDeformManager", $"Processed deformation for: {deformableTarget.name}");
                }
                else
#endif
                {
                    VastcoreLogger.Instance.LogDebug("VastcoreDeformManager", "Processed deformation (dummy)");
                }
            }
            catch (System.Exception ex)
            {
#if DEFORM_AVAILABLE
                if (request.target is Deformable deformableTarget)
                {
                    VastcoreLogger.Instance.LogError("VastcoreDeformManager", $"Deformation failed for {deformableTarget.name}: {ex.Message}");
                }
                else
#endif
                {
                    VastcoreLogger.Instance.LogError("VastcoreDeformManager", $"Deformation failed: {ex.Message}");
                }
                request.onComplete?.Invoke(false);
            }
        }
        
        /// <summary>
        /// 品質設定の適用
        /// </summary>
        private void ApplyQualitySettings(object deformable, DeformQualityLevel quality)
        {
            if (deformable == null) return;
            
#if DEFORM_AVAILABLE
            if (deformable is Deformable deformableComponent)
            {
                switch (quality)
                {
                        case DeformQualityLevel.Low:
                        deformableComponent.UpdateMode = UpdateMode.Stop;
                        break;
                    case DeformQualityLevel.Medium:
                        deformableComponent.UpdateMode = UpdateMode.Custom;
                        break;
                    case DeformQualityLevel.High:
                    case DeformQualityLevel.Ultra:
                        deformableComponent.UpdateMode = UpdateMode.Auto;
                        break;
                }
            }
#else
            // Deformパッケージが利用できない場合は何もしない
            VastcoreLogger.Instance.LogDebug("VastcoreDeformManager", $"Quality settings applied (dummy): {quality}");
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
