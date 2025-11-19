using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Testing
{
    /// <summary>
    /// 基本的なプリミティブオブジェクト生成テストコンポーネント
    /// Scene上でボタンクリックでプリミティブオブジェクトを生成可能
    /// </summary>
    public class PrimitiveObjectSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject primitivePrefab;
        [SerializeField] private int spawnCount = 10;
        [SerializeField] private float spawnRadius = 50f;
        [SerializeField] private float minHeight = 10f;
        [SerializeField] private float maxHeight = 50f;
        [SerializeField] private LayerMask terrainLayer = -1;

        [Header("Object Types")]
        [SerializeField] private bool spawnClimbable = true;
        [SerializeField] private bool spawnGrindable = true;

        private UnityEngine.Terrain currentTerrain;

        void Start()
        {
            currentTerrain = FindObjectOfType<UnityEngine.Terrain>();
        }

        [ContextMenu("Spawn Primitive Objects")]
        public void SpawnPrimitiveObjects()
        {
            if (primitivePrefab == null)
            {
                Debug.LogError("Primitive prefab not set!");
                return;
            }

            if (currentTerrain == null)
            {
                currentTerrain = FindObjectOfType<UnityEngine.Terrain>();
                if (currentTerrain == null)
                {
                    Debug.LogError("No terrain found! Generate terrain first.");
                    return;
                }
            }

            ClearPrimitiveObjects();

            for (int i = 0; i < spawnCount; i++)
            {
                SpawnSinglePrimitive();
            }

            Debug.Log($"Spawned {spawnCount} primitive objects.");
        }

        private void SpawnSinglePrimitive()
        {
            // ランダム位置を生成
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = new Vector3(randomCircle.x, 0f, randomCircle.y) + transform.position;

            // Terrainの高さを取得
            float terrainHeight = currentTerrain.SampleHeight(spawnPosition);
            spawnPosition.y = terrainHeight + Random.Range(minHeight, maxHeight);

            // オブジェクト生成
            GameObject primitive = Instantiate(primitivePrefab, spawnPosition, Quaternion.identity);
            primitive.name = $"Primitive_{Random.Range(1000, 9999)}";

            // レイヤー設定
            primitive.layer = LayerMask.NameToLayer("Primitive");

            // タグ設定（オプション）
            primitive.tag = "PrimitiveObject";

            // ランダム回転
            primitive.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // スケールバリエーション
            float scale = Random.Range(0.8f, 1.5f);
            primitive.transform.localScale = Vector3.one * scale;
        }

        [ContextMenu("Clear Primitive Objects")]
        public void ClearPrimitiveObjects()
        {
            GameObject[] primitives = GameObject.FindGameObjectsWithTag("PrimitiveObject");
            foreach (GameObject primitive in primitives)
            {
                DestroyImmediate(primitive);
            }
            Debug.Log("Cleared all primitive objects.");
        }

        void OnDrawGizmosSelected()
        {
            // スポーン範囲を表示
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
    }
}
