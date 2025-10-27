using UnityEngine;
using System.Collections.Generic;
using Vastcore.Core;

namespace Vastcore.Terrain
{
    /// <summary>
    /// プリミティブ地形管理システム
    /// 地形生成時にプリミティブオブジェクトを配置・管理
    /// </summary>
    public class PrimitiveTerrainManager : MonoBehaviour
    {
        [Header("プリミティブ設定")]
        [SerializeField] private int maxPrimitivesPerFrame = 5;
        [SerializeField] private float primitiveCleanupDistance = 2000f;
        [SerializeField] private bool enablePrimitivePooling = true;

        [Header("LOD設定")]
        [SerializeField] private float lodDistance1 = 500f;
        [SerializeField] private float lodDistance2 = 1000f;
        [SerializeField] private float lodScale1 = 0.8f;
        [SerializeField] private float lodScale2 = 0.5f;
        [SerializeField] private bool enableLOD = true;

        // 内部データ
        private Dictionary<Vector2Int, List<GameObject>> activePrimitives;
        private Queue<GameObject> primitivePool;
        private Transform primitiveContainer;
        private int primitivesSpawnedThisFrame;
        private int lastSpawnFrame = -1;
        private Transform playerTransform;

        private void Awake()
        {
            InitializeManager();
        }

        private void Update()
        {
            if (enableLOD && playerTransform != null)
            {
                ApplyLODToPrimitives();
            }
        }

        /// <summary>
        /// マネージャーの初期化
        /// </summary>
        private void InitializeManager()
        {
            activePrimitives = new Dictionary<Vector2Int, List<GameObject>>();
            primitivePool = new Queue<GameObject>();

            // プリミティブコンテナを作成
            primitiveContainer = new GameObject("PrimitiveContainer").transform;
            primitiveContainer.SetParent(transform);

            // プレイヤー参照を取得
            playerTransform = FindObjectOfType<Vastcore.Player.Controllers.PlayerController>()?.transform;
            if (playerTransform == null)
            {
                VastcoreLogger.Instance.LogWarning("PrimitiveTerrainManager", "PlayerControllerが見つかりません。LOD機能が動作しません。");
            }

            VastcoreLogger.Instance.LogInfo("PrimitiveTerrainManager", "プリミティブ地形マネージャーが初期化されました");
        }

        /// <summary>
        /// プリミティブ地形を生成
        /// </summary>
        public GameObject SpawnPrimitiveTerrain(PrimitiveTerrainRule rule, Vector3 position)
        {
            if (lastSpawnFrame != Time.frameCount)
            {
                lastSpawnFrame = Time.frameCount;
                primitivesSpawnedThisFrame = 0;
            }

            if (primitivesSpawnedThisFrame >= maxPrimitivesPerFrame)
            {
                VastcoreLogger.Instance.LogDebug("PrimitiveTerrainManager", "1フレーム当たりの生成上限に達しました");
                return null;
            }

            if (rule == null)
            {
                VastcoreLogger.Instance.LogWarning("PrimitiveTerrainManager", "プリミティブルールがnullです");
                return null;
            }

            // プリミティブオブジェクトを作成またはプールから取得
            GameObject primitiveObject = GetPrimitiveObject(rule.primitiveType);

            if (primitiveObject != null)
            {
                // 位置とスケールを設定
                primitiveObject.transform.position = position;
                primitiveObject.transform.localScale = Vector3.one * rule.scale;
                primitiveObject.transform.SetParent(primitiveContainer);

                // マテリアルを設定
                if (rule.materialOverride != null)
                {
                    var renderer = primitiveObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = rule.materialOverride;
                    }
                }

                // 色変異を適用
                ApplyColorVariation(primitiveObject, rule);

                // アクティブプリミティブに登録
                Vector2Int tileCoord = WorldToTileCoordinate(position);
                if (!activePrimitives.ContainsKey(tileCoord))
                {
                    activePrimitives[tileCoord] = new List<GameObject>();
                }
                activePrimitives[tileCoord].Add(primitiveObject);

                primitiveObject.SetActive(true);

                VastcoreLogger.Instance.LogDebug("PrimitiveTerrainManager",
                    $"プリミティブ生成: {rule.primitiveType} at {position}, tile {tileCoord}");

                primitivesSpawnedThisFrame++;
            }

            return primitiveObject;
        }

        /// <summary>
        /// プリミティブオブジェクトを取得（新規作成またはプールから）
        /// </summary>
        private GameObject GetPrimitiveObject(PrimitiveType primitiveType)
        {
            GameObject primitiveObject = null;

            // プールから取得を試行
            if (enablePrimitivePooling && primitivePool.Count > 0)
            {
                primitiveObject = primitivePool.Dequeue();
                VastcoreLogger.Instance.LogDebug("PrimitiveTerrainManager", "プールからプリミティブを取得");
            }

            // プールにない場合は新規作成
            if (primitiveObject == null)
            {
                primitiveObject = CreatePrimitiveObject(primitiveType);
                VastcoreLogger.Instance.LogDebug("PrimitiveTerrainManager", $"新規プリミティブ作成: {primitiveType}");
            }

            return primitiveObject;
        }

        /// <summary>
        /// プリミティブオブジェクトを新規作成
        /// </summary>
        private GameObject CreatePrimitiveObject(PrimitiveType primitiveType)
        {
            GameObject primitiveObject = new GameObject($"Primitive_{primitiveType}");

            // 基本コンポーネントを追加
            var meshFilter = primitiveObject.AddComponent<MeshFilter>();
            var meshRenderer = primitiveObject.AddComponent<MeshRenderer>();
            var collider = primitiveObject.AddComponent<MeshCollider>();

            // プリミティブタイプに応じたメッシュを設定
            Mesh primitiveMesh = null;
            switch (primitiveType)
            {
                case PrimitiveType.Sphere:
                    primitiveMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
                    break;
                case PrimitiveType.Capsule:
                    primitiveMesh = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
                    break;
                case PrimitiveType.Cylinder:
                    primitiveMesh = Resources.GetBuiltinResource<Mesh>("Cylinder.fbx");
                    break;
                case PrimitiveType.Cube:
                    primitiveMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                    break;
                case PrimitiveType.Plane:
                    primitiveMesh = Resources.GetBuiltinResource<Mesh>("Plane.fbx");
                    break;
                case PrimitiveType.Quad:
                    primitiveMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                    break;
                default:
                    // デフォルトは球体
                    primitiveMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
                    break;
            }

            if (primitiveMesh != null)
            {
                meshFilter.mesh = primitiveMesh;
                collider.sharedMesh = primitiveMesh;
            }

            // デフォルトマテリアルを設定（URP対応）
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader != null)
            {
                meshRenderer.material = new Material(litShader);
            }
            else
            {
                // Fallback to Standard shader
                meshRenderer.material = new Material(Shader.Find("Standard"));
            }

            // ライティング設定
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            meshRenderer.receiveShadows = true;

            return primitiveObject;
        }

        /// <summary>
        /// 色変異を適用
        /// </summary>
        private void ApplyColorVariation(GameObject primitiveObject, PrimitiveTerrainRule rule)
        {
            var renderer = primitiveObject.GetComponent<Renderer>();
            if (renderer != null && rule.colorVariationStrength > 0f)
            {
                Color baseColor = renderer.material.color;
                Color variation = rule.colorVariation * rule.colorVariationStrength;
                Color finalColor = Color.Lerp(baseColor, variation, Random.value);
                renderer.material.color = finalColor;
            }
        }

        /// <summary>
        /// 指定タイルのプリミティブをクリーンアップ
        /// </summary>
        public void CleanupPrimitivesInTile(Vector2Int tileCoord)
        {
            if (activePrimitives.TryGetValue(tileCoord, out List<GameObject> primitives))
            {
                foreach (GameObject primitive in primitives)
                {
                    if (enablePrimitivePooling)
                    {
                        // プールに戻す
                        primitive.SetActive(false);
                        primitive.transform.SetParent(null);
                        primitivePool.Enqueue(primitive);
                    }
                    else
                    {
                        // 破棄
                        Destroy(primitive);
                    }
                }

                activePrimitives.Remove(tileCoord);

                VastcoreLogger.Instance.LogDebug("PrimitiveTerrainManager",
                    $"タイル {tileCoord} のプリミティブをクリーンアップ: {primitives.Count}個");
            }
        }

        /// <summary>
        /// 距離に基づいてプリミティブをクリーンアップ
        /// </summary>
        public void CleanupDistantPrimitives(Vector3 playerPosition, float maxDistance = -1f)
        {
            if (maxDistance < 0f)
            {
                maxDistance = primitiveCleanupDistance;
            }

            List<Vector2Int> tilesToRemove = new List<Vector2Int>();

            foreach (var kvp in activePrimitives)
            {
                Vector2Int tileCoord = kvp.Key;
                Vector3 tileWorldPos = TileCoordinateToWorldPosition(tileCoord);
                float distance = Vector3.Distance(playerPosition, tileWorldPos);

                if (distance > maxDistance)
                {
                    CleanupPrimitivesInTile(tileCoord);
                    tilesToRemove.Add(tileCoord);
                }
            }

            foreach (Vector2Int tileCoord in tilesToRemove)
            {
                activePrimitives.Remove(tileCoord);
            }

            if (tilesToRemove.Count > 0)
            {
                VastcoreLogger.Instance.LogInfo("PrimitiveTerrainManager",
                    $"距離ベースクリーンアップ実行: {tilesToRemove.Count}タイル");
            }
        }

        /// <summary>
        /// ワールド座標をタイル座標に変換
        /// </summary>
        private Vector2Int WorldToTileCoordinate(Vector3 worldPosition)
        {
            // 簡易実装：1000ユニットごとに1タイル
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / 1000f),
                Mathf.FloorToInt(worldPosition.z / 1000f)
            );
        }

        /// <summary>
        /// タイル座標をワールド座標に変換
        /// </summary>
        private Vector3 TileCoordinateToWorldPosition(Vector2Int tileCoord)
        {
            return new Vector3(
                tileCoord.x * 1000f + 500f, // タイル中央
                0f,
                tileCoord.y * 1000f + 500f
            );
        }

        /// <summary>
        /// 距離に基づいてLODを適用
        /// </summary>
        private void ApplyLODToPrimitives()
        {
            foreach (var tilePrimitives in activePrimitives.Values)
            {
                foreach (GameObject primitive in tilePrimitives)
                {
                    if (primitive == null) continue;

                    float distance = Vector3.Distance(playerTransform.position, primitive.transform.position);
                    float targetScale = 1.0f;

                    if (distance > lodDistance2)
                    {
                        targetScale = lodScale2;
                    }
                    else if (distance > lodDistance1)
                    {
                        targetScale = lodScale1;
                    }

                    // スムーズにスケールを変更
                    primitive.transform.localScale = Vector3.Lerp(primitive.transform.localScale, Vector3.one * targetScale, Time.deltaTime * 2f);
                }
            }
        }

        /// <summary>
        /// アクティブなプリミティブ数を取得
        /// </summary>
        public int GetActivePrimitiveCount()
        {
            int total = 0;
            foreach (var primitives in activePrimitives.Values)
            {
                total += primitives.Count;
            }
            return total;
        }

        /// <summary>
        /// プール内のプリミティブ数を取得
        /// </summary>
        public int GetPooledPrimitiveCount()
        {
            return primitivePool.Count;
        }

        /// <summary>
        /// デバッグ情報を取得
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Active Primitives: {GetActivePrimitiveCount()}, Pooled: {GetPooledPrimitiveCount()}, Tiles: {activePrimitives.Count}";
        }
    }
}
