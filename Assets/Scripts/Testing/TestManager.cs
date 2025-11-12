using UnityEngine;
using UnityEngine.UI;
using Vastcore.Testing;

namespace Vastcore.Testing
{
    /// <summary>
    /// テストマネージャー：Scene上でTerrainとプリミティブオブジェクトの生成を制御
    /// UIボタンで操作可能
    /// </summary>
    public class TestManager : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private TerrainSpawner terrainSpawner;
        [SerializeField] private PrimitiveObjectSpawner primitiveSpawner;

        [Header("UI References")]
        [SerializeField] private Button generateTerrainButton;
        [SerializeField] private Button clearTerrainButton;
        [SerializeField] private Button spawnPrimitivesButton;
        [SerializeField] private Button clearPrimitivesButton;
        [SerializeField] private Text statusText;

        void Start()
        {
            // UIイベント設定
            if (generateTerrainButton != null)
                generateTerrainButton.onClick.AddListener(OnGenerateTerrainClicked);

            if (clearTerrainButton != null)
                clearTerrainButton.onClick.AddListener(OnClearTerrainClicked);

            if (spawnPrimitivesButton != null)
                spawnPrimitivesButton.onClick.AddListener(OnSpawnPrimitivesClicked);

            if (clearPrimitivesButton != null)
                clearPrimitivesButton.onClick.AddListener(OnClearPrimitivesClicked);

            UpdateStatus("Ready to generate terrain and primitives.");
        }

        private void OnGenerateTerrainClicked()
        {
            if (terrainSpawner != null)
            {
                terrainSpawner.GenerateTerrain();
                UpdateStatus("Terrain generation started...");
            }
            else
            {
                UpdateStatus("TerrainSpawner not assigned!");
            }
        }

        private void OnClearTerrainClicked()
        {
            if (terrainSpawner != null)
            {
                terrainSpawner.ClearTerrain();
                UpdateStatus("Terrain cleared.");
            }
        }

        private void OnSpawnPrimitivesClicked()
        {
            if (primitiveSpawner != null)
            {
                primitiveSpawner.SpawnPrimitiveObjects();
                UpdateStatus("Primitive objects spawned.");
            }
            else
            {
                UpdateStatus("PrimitiveObjectSpawner not assigned!");
            }
        }

        private void OnClearPrimitivesClicked()
        {
            if (primitiveSpawner != null)
            {
                primitiveSpawner.ClearPrimitiveObjects();
                UpdateStatus("Primitive objects cleared.");
            }
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log(message);
        }

        [ContextMenu("Auto Setup")]
        private void AutoSetup()
        {
            // TerrainSpawnerを探すか作成
            terrainSpawner = FindObjectOfType<TerrainSpawner>();
            if (terrainSpawner == null)
            {
                GameObject spawnerObj = new GameObject("TerrainSpawner");
                terrainSpawner = spawnerObj.AddComponent<TerrainSpawner>();
            }

            // PrimitiveObjectSpawnerを探すか作成
            primitiveSpawner = FindObjectOfType<PrimitiveObjectSpawner>();
            if (primitiveSpawner == null)
            {
                GameObject spawnerObj = new GameObject("PrimitiveObjectSpawner");
                primitiveSpawner = spawnerObj.AddComponent<PrimitiveObjectSpawner>();
            }

            Debug.Log("Auto setup completed.");
        }
    }
}
