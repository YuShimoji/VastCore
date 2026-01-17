using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Vastcore.Core;
using Vastcore.Utilities;
using Vastcore.Player;
using Vastcore.UI;
using Vastcore.UI.Menus;
using Vastcore.Camera.Cinematic;
using Vastcore.Generation;
using Vastcore.UI.Menus;

namespace Vastcore.Game.Managers
{
    /// ゲームの起動シーケンス、各マネージャーの連携を統括する
    /// </summary>
    public class VastcoreGameManager : MonoBehaviour
    {
        #region シングルトン
        public static VastcoreGameManager Instance { get; private set; }
        #endregion

        #region マネージャー参照
        [Header("Managers")]
        // [SerializeField] private TerrainGenerator m_TerrainGenerator; // TODO: TerrainGeneratorクラスが未実装
        [SerializeField] private TitleScreenManager m_TitleScreenManager;
        #endregion

        #region プレイヤー設定
        [Header("Player Settings")]
        [SerializeField] private GameObject m_PlayerPrefab;
        [SerializeField] private bool m_AutoFindPlayer = true;
        #endregion

        #region カメラ・演出設定
        [Header("Camera & Cinematics")]
        [SerializeField] private GameObject m_CinematicCameraPrefab;
        [SerializeField] private bool m_EnableCinematics = true;
        #endregion

        #region 環境設定
        [Header("Environment")]
        [SerializeField] private Gradient m_SkyboxTint = new Gradient();
        [SerializeField] private Light m_SunLight;
        [SerializeField] private bool m_EnableDynamicLighting = true;
        [SerializeField] private AnimationCurve m_LightIntensityCurve = AnimationCurve.Linear(0, 1, 1, 1);
        #endregion

        #region 内部変数
        private GameObject m_CurrentPlayer;
        private CinematicCameraController m_CinematicCamera;
        private bool m_IsInitialized = false;
        private Coroutine m_EnvironmentUpdateCoroutine;
        #endregion

        #region 開発設定
        [Header("Development Settings")]
        [SerializeField] private bool m_SkipIntroCinematic = false;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (m_IsInitialized)
            {
                StartCoroutine(GameStartSequence());
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region 初期化
        private void InitializeGame()
        {
            Debug.Log("Initializing Vastcore Game Manager...");

            SetupQualitySettings();
            SetupEnvironment();
            m_IsInitialized = true;
        }

        private void SetupQualitySettings()
        {
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 1000f;
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.antiAliasing = 4;
            QualitySettings.lodBias = 1.5f;
            QualitySettings.maximumLODLevel = 0;
        }

        private void SetupEnvironment()
        {
            if (m_SunLight == null)
            {
                Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
                foreach (Light light in lights)
                {
                    if (light.type == LightType.Directional)
                    {                        m_SunLight = light;
                        break;
                    }
                }

                if (m_SunLight == null)
                {
                    CreateSunLight();
                }
            }

            if (m_EnableDynamicLighting)
            {
                StartDynamicLighting();
            }
        }

        private void CreateSunLight()
        {
            GameObject sunObject = new GameObject("Sun Light");
            m_SunLight = sunObject.AddComponent<Light>();
            m_SunLight.type = LightType.Directional;
            m_SunLight.intensity = 1.2f;
            m_SunLight.color = Color.white;
            m_SunLight.shadows = LightShadows.Soft;
            m_SunLight.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        }
        #endregion

        #region ゲーム開始シーケンス
        private IEnumerator GameStartSequence()
        {
            Debug.Log("[VastcoreGameManager] Starting Game Sequence...");

            // if (m_TerrainGenerator == null)
            // {
            //     m_TerrainGenerator = FindFirstObjectByType<TerrainGenerator>();
            // }

            // if (m_TerrainGenerator == null)
            // {
            //     Debug.LogError("TerrainGenerator is not assigned!");
            //     yield break;
            // }
            // yield return StartCoroutine(m_TerrainGenerator.GenerateTerrain());
            // Debug.Log("[VastcoreGameManager] Terrain Generation Completed.");

            yield return StartCoroutine(SetupPlayer());
            Debug.Log("[VastcoreGameManager] Player Setup Completed.");

            yield return StartCoroutine(SetupCinematics());
            Debug.Log("[VastcoreGameManager] Cinematics Setup Completed.");

            if (!m_SkipIntroCinematic && m_CinematicCamera != null)
            {
                yield return StartCoroutine(m_CinematicCamera.PlayInitialCinematicSequence());
            }
            else
            {
                var playerController = m_CurrentPlayer.GetComponent<AdvancedPlayerController>();
                if (playerController != null) playerController.EnablePlayerControl();
            }

            if (m_TitleScreenManager != null)
            {
                m_TitleScreenManager.Setup(m_CurrentPlayer.GetComponent<AdvancedPlayerController>());
                m_TitleScreenManager.ShowTitle();
            }

            Debug.Log("[VastcoreGameManager] Game Sequence Completed! Player is ready.");
        }

        private IEnumerator SetupPlayer()
        {
            if (m_AutoFindPlayer)
            {
                m_CurrentPlayer = GameObject.FindGameObjectWithTag("Player");
            }

            if (m_CurrentPlayer == null && m_PlayerPrefab != null)
            {
                Vector3 spawnPosition = Vector3.zero;
                // if (m_TerrainGenerator != null)
                // {
                //     // A more robust spawn position logic is needed here.
                //     // For now, spawn at a safe height above the center.
                //     spawnPosition = new Vector3(m_TerrainGenerator.terrainSize.x / 2, 100, m_TerrainGenerator.terrainSize.z / 2);
                // }
                m_CurrentPlayer = Instantiate(m_PlayerPrefab, spawnPosition, Quaternion.identity);
            }

            if (m_CurrentPlayer == null)
            {
                Debug.LogError("Player could not be found or instantiated!");
                yield break;
            }

            var playerController = m_CurrentPlayer.GetComponent<AdvancedPlayerController>();
            if (playerController != null) playerController.DisablePlayerControl();
        }

        private IEnumerator SetupCinematics()
        {
            if (m_EnableCinematics && m_CinematicCameraPrefab != null)
            {
                GameObject camObj = Instantiate(m_CinematicCameraPrefab);
                m_CinematicCamera = camObj.GetComponent<CinematicCameraController>();
                if (m_CinematicCamera != null)
                {
                    // m_CinematicCamera.Setup(m_CurrentPlayer.GetComponent<AdvancedPlayerController>(), m_TerrainGenerator.gameObject);
                    m_CinematicCamera.Setup(m_CurrentPlayer.GetComponent<AdvancedPlayerController>(), null); // TODO: TerrainGenerator実装後に修正
                }
            }

            if (m_TitleScreenManager == null)
            {
                m_TitleScreenManager = FindFirstObjectByType<TitleScreenManager>();
            }
            yield return null;
        }

        private void StartDynamicLighting()
        {
            if (m_EnvironmentUpdateCoroutine != null)
            {
                StopCoroutine(m_EnvironmentUpdateCoroutine);
            }
            m_EnvironmentUpdateCoroutine = StartCoroutine(UpdateEnvironment());
        }

        private IEnumerator UpdateEnvironment()
        {
            while (m_EnableDynamicLighting)
            {
                // Example: Update light intensity based on player's altitude
                if (m_CurrentPlayer != null)
                {
                    float normalizedHeight = Mathf.InverseLerp(0, 500, m_CurrentPlayer.transform.position.y);
                    m_SunLight.intensity = m_LightIntensityCurve.Evaluate(normalizedHeight);
                    RenderSettings.skybox.SetColor("_Tint", m_SkyboxTint.Evaluate(normalizedHeight));
                }
                yield return new WaitForSeconds(1f); // Update every second
            }
        }
        #endregion
    }
}