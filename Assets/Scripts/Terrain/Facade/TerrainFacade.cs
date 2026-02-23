using UnityEngine;
using UnityEngine.Serialization;
using Vastcore.Terrain;
using Vastcore.Terrain.Config;
using Vastcore.Terrain.Volumetric;
using Vastcore.Utilities;
using Vastcore.WorldGen.Recipe;

namespace Vastcore.Terrain.Facade
{
    /// <summary>
    /// 地形システムの統一ファサード。
    /// Classic / Volumetric / Hybrid の初期化と更新を一本化する。
    /// </summary>
    public sealed class TerrainFacade : MonoBehaviour
    {
        [Header("Mode")]
        [SerializeField] private TerrainMode _mode = TerrainMode.Classic;

        [Header("Classic")]
        [SerializeField] private TerrainGenerationConfig _classicConfig;
        [SerializeField] private TerrainStreamingController _classicStreamingController;

        [Header("Volumetric")]
        [FormerlySerializedAs("_recipe")]
        [SerializeField] private WorldGenRecipe _worldGenRecipe;
        [SerializeField] private VolumetricStreamingController _volumetricStreamingController;

        [Header("Common")]
        [SerializeField] private Transform _streamTarget;

        /// <summary>現在のモード。</summary>
        public TerrainMode Mode => _mode;

        /// <summary>Classic 設定。</summary>
        public TerrainGenerationConfig ClassicConfig => _classicConfig;

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// モードに応じて地形システムを初期化する。
        /// </summary>
        public void Initialize()
        {
            switch (_mode)
            {
                case TerrainMode.Classic:
                    InitializeClassic();
                    break;
                case TerrainMode.Volumetric:
                    InitializeVolumetric();
                    break;
                case TerrainMode.Hybrid:
                    InitializeHybrid();
                    break;
            }
        }

        /// <summary>
        /// ターゲット位置に基づいてストリーミング更新を行う。
        /// </summary>
        public void UpdateStreaming(Vector3 targetPosition)
        {
            switch (_mode)
            {
                case TerrainMode.Classic:
                    if (_classicStreamingController != null)
                        _classicStreamingController.UpdateStreaming(targetPosition);
                    break;
                case TerrainMode.Volumetric:
                    if (_volumetricStreamingController != null)
                        _volumetricStreamingController.UpdateStreaming(targetPosition, false);
                    break;
                case TerrainMode.Hybrid:
                    if (_classicStreamingController != null)
                        _classicStreamingController.UpdateStreaming(targetPosition);
                    if (_volumetricStreamingController != null)
                        _volumetricStreamingController.UpdateStreaming(targetPosition, false);
                    break;
            }
        }

        private void InitializeClassic()
        {
            if (_classicConfig == null)
            {
                VastcoreLogger.Instance.LogError("TerrainFacade", "Classic mode requires TerrainGenerationConfig.");
                return;
            }

            if (_classicStreamingController == null)
            {
                _classicStreamingController = GetComponent<TerrainStreamingController>();
                if (_classicStreamingController == null)
                    _classicStreamingController = gameObject.AddComponent<TerrainStreamingController>();
            }

            _classicStreamingController.config = _classicConfig;
            _classicStreamingController.target = ResolveTarget();
            _classicStreamingController.Initialize();

            VastcoreLogger.Instance.LogInfo("TerrainFacade", "Initialized Classic streaming.");
        }

        private void InitializeVolumetric()
        {
            if (_worldGenRecipe == null)
            {
                VastcoreLogger.Instance.LogError("TerrainFacade", "Volumetric mode requires WorldGenRecipe.");
                return;
            }

            if (_volumetricStreamingController == null)
            {
                _volumetricStreamingController = GetComponent<VolumetricStreamingController>();
                if (_volumetricStreamingController == null)
                    _volumetricStreamingController = gameObject.AddComponent<VolumetricStreamingController>();
            }

            _volumetricStreamingController.Configure(_worldGenRecipe, ResolveTarget());
            _volumetricStreamingController.Initialize();

            VastcoreLogger.Instance.LogInfo("TerrainFacade", "Initialized Volumetric streaming.");
        }

        private void InitializeHybrid()
        {
            InitializeClassic();
            InitializeVolumetric();
            VastcoreLogger.Instance.LogInfo("TerrainFacade", "Initialized Hybrid streaming.");
        }

        private Transform ResolveTarget()
        {
            if (_streamTarget != null)
                return _streamTarget;

            if (Camera.main != null)
                return Camera.main.transform;

            return null;
        }
    }
}
