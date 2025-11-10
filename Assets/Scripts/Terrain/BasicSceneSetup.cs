using UnityEngine;

namespace Vastcore.Terrain
{
    /// <summary>
    /// 基本的なシーンセットアップ
    /// カメラとライト、地形表示を自動設定
    /// </summary>
    public class BasicSceneSetup : MonoBehaviour
    {
        [Header("カメラ設定")]
        [SerializeField] private bool setupCamera = true;
        [SerializeField] private Vector3 cameraPosition = new Vector3(128, 150, 128);
        [SerializeField] private Vector3 cameraLookAt = new Vector3(128, 0, 128);

        [Header("ライト設定")]
        [SerializeField] private bool setupLighting = true;
        [SerializeField] private Color lightColor = new Color(1f, 0.95f, 0.8f);

        [Header("地形設定")]
        [SerializeField] private bool setupTerrain = true;
        [SerializeField] private GameObject terrainDisplayPrefab;

        void Start()
        {
            SetupScene();
        }

        [ContextMenu("Setup Scene")]
        public void SetupScene()
        {
            Debug.Log("Setting up basic scene...");

            // カメラのセットアップ
            if (setupCamera)
            {
                SetupCamera();
            }

            // ライトのセットアップ
            if (setupLighting)
            {
                SetupLighting();
            }

            // 地形のセットアップ
            if (setupTerrain)
            {
                SetupTerrain();
            }

            Debug.Log("Scene setup completed!");
        }

        private void SetupCamera()
        {
            // メインカメラを取得または作成
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
                cameraObject.tag = "MainCamera";
            }

            // カメラの位置と向きを設定
            mainCamera.transform.position = cameraPosition;
            mainCamera.transform.LookAt(cameraLookAt);

            Debug.Log("Camera setup completed");
        }

        private void SetupLighting()
        {
            // ディレクショナルライトを取得または作成
            Light directionalLight = FindFirstObjectByType<Light>();
            if (directionalLight == null || directionalLight.type != LightType.Directional)
            {
                GameObject lightObject = new GameObject("Directional Light");
                directionalLight = lightObject.AddComponent<Light>();
                directionalLight.type = LightType.Directional;
            }

            // ライトの設定
            directionalLight.color = lightColor;
            directionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            directionalLight.intensity = 1f;

            // 環境ライトの設定
            RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.2f);

            Debug.Log("Lighting setup completed");
        }

        private void SetupTerrain()
        {
            // 既存の地形を確認
            BasicTerrainDisplay existingTerrain = FindFirstObjectByType<BasicTerrainDisplay>();
            if (existingTerrain != null)
            {
                Debug.Log("Terrain already exists in scene");
                return;
            }

            // 地形表示オブジェクトを作成
            GameObject terrainObject = new GameObject("Terrain Display");

            BasicTerrainDisplay terrainDisplay;
            if (terrainDisplayPrefab != null)
            {
                // プレハブを使用
                GameObject instance = Instantiate(terrainDisplayPrefab);
                instance.name = "Terrain Display";
                terrainDisplay = instance.GetComponent<BasicTerrainDisplay>();
            }
            else
            {
                // コンポーネントを直接追加
                terrainDisplay = terrainObject.AddComponent<BasicTerrainDisplay>();
            }

            // 地形を生成
            if (terrainDisplay != null)
            {
                terrainDisplay.GenerateBasicTerrain();
            }

            Debug.Log("Terrain setup completed");
        }

        [ContextMenu("Clear Scene")]
        public void ClearScene()
        {
            // 作成したオブジェクトを削除（オプション）
            Debug.Log("Scene cleared (manual cleanup may be needed)");
        }
    }
}
