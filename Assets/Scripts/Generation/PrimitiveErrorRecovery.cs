using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vastcore.Core;

namespace Vastcore.Generation
{
    /// <summary>
    /// プリミティブ地形オブジェクト専用のエラー回復システム
    /// プリミティブ配置エラー、メッシュ生成失敗、配置衝突の回復処理
    /// </summary>
    public class PrimitiveErrorRecovery : MonoBehaviour
    {
        [Header("プリミティブエラー回復設定")]
        public bool enablePositionRecovery = true;
        public bool enableMeshRecovery = true;
        public bool enableCollisionRecovery = true;
        public int maxPositionAttempts = 10;
        
        [Header("配置回復設定")]
        public float positionSearchRadius = 100f;
        public float minDistanceFromOthers = 50f;
        public LayerMask obstacleLayerMask = -1;
        
        [Header("メッシュ回復設定")]
        public bool useFallbackPrimitives = true;
        public PrimitiveType[] fallbackPrimitiveTypes = { PrimitiveType.Cube, PrimitiveType.Sphere, PrimitiveType.Cylinder };
        
        private static PrimitiveErrorRecovery instance;
        public static PrimitiveErrorRecovery Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<PrimitiveErrorRecovery>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("PrimitiveErrorRecovery");
                        instance = go.AddComponent<PrimitiveErrorRecovery>();
                    }
                }
                return instance;
            }
        }
        
        private List<Vector3> occupiedPositions = new List<Vector3>();
        private Dictionary<PrimitiveType, Mesh> fallbackMeshCache = new Dictionary<PrimitiveType, Mesh>();
        
        private void Start()
        {
            InitializeFallbackMeshes();
        }
        
        /// <summary>
        /// プリミティブ配置エラーの回復
        /// </summary>
        public IEnumerator RecoverPrimitiveSpawn(Vector3 originalPosition, PrimitiveType primitiveType, 
            float scale, System.Action<GameObject> onSuccess, System.Action onFailure)
        {
            VastcoreLogger.Instance.LogInfo("PrimitiveRecovery", 
                $"プリミティブ配置回復を開始: {primitiveType} at {originalPosition}");
            
            // 1. 位置の回復を試行
            Vector3 recoveredPosition = originalPosition;
            if (enablePositionRecovery)
            {
                recoveredPosition = yield return StartCoroutine(FindValidPosition(originalPosition, scale));
                if (recoveredPosition == Vector3.zero)
                {
                    VastcoreLogger.Instance.LogWarning("PrimitiveRecovery", "有効な配置位置が見つかりませんでした");
                    onFailure?.Invoke();
                    yield break;
                }
            }
            
            // 2. メッシュ生成の回復を試行
            GameObject recoveredPrimitive = null;
            if (enableMeshRecovery)
            {
                recoveredPrimitive = yield return StartCoroutine(CreateRecoveredPrimitive(
                    recoveredPosition, primitiveType, scale));
            }
            
            if (recoveredPrimitive != null)
            {
                // 3. 衝突検出の設定
                if (enableCollisionRecovery)
                {
                    SetupCollisionRecovery(recoveredPrimitive);
                }
                
                // 配置位置を記録
                occupiedPositions.Add(recoveredPosition);
                
                VastcoreLogger.Instance.LogInfo("PrimitiveRecovery", 
                    $"プリミティブ回復成功: {primitiveType} at {recoveredPosition}");
                onSuccess?.Invoke(recoveredPrimitive);
            }
            else
            {
                VastcoreLogger.Instance.LogError("PrimitiveRecovery", "プリミティブ回復に失敗しました");
                onFailure?.Invoke();
            }
        }
        
        private IEnumerator FindValidPosition(Vector3 originalPosition, float scale)
        {
            for (int attempt = 0; attempt < maxPositionAttempts; attempt++)
            {
                Vector3 testPosition = originalPosition;
                
                if (attempt > 0)
                {
                    // ランダムな位置をテスト
                    Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * positionSearchRadius;
                    testPosition = originalPosition + new Vector3(randomOffset.x, 0, randomOffset.y);
                }
                
                // 地形の高さに合わせて調整
                if (Physics.Raycast(testPosition + Vector3.up * 1000f, Vector3.down, out RaycastHit hit, 2000f))
                {
                    testPosition.y = hit.point.y;
                }
                
                // 位置の有効性をチェック
                if (IsValidPosition(testPosition, scale))
                {
                    yield return testPosition;
                    yield break;
                }
                
                yield return null; // フレーム分散
            }
            
            yield return Vector3.zero; // 有効な位置が見つからない
        }
        
        private bool IsValidPosition(Vector3 position, float scale)
        {
            try
            {
                // 他のオブジェクトとの距離チェック
                foreach (var occupied in occupiedPositions)
                {
                    if (Vector3.Distance(position, occupied) < minDistanceFromOthers + scale)
                    {
                        return false;
                    }
                }
                
                // 障害物との衝突チェック
                Collider[] overlapping = Physics.OverlapSphere(position, scale, obstacleLayerMask);
                if (overlapping.Length > 0)
                {
                    return false;
                }
                
                // 地形の傾斜チェック
                if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
                {
                    float slope = Vector3.Angle(hit.normal, Vector3.up);
                    if (slope > 45f) // 45度以上の傾斜は無効
                    {
                        return false;
                    }
                }
                
                return true;
            }
            catch (Exception error)
            {
                VastcoreLogger.Instance.LogError("PrimitiveRecovery", 
                    $"位置検証中にエラー: {error.Message}");
                return false;
            }
        }
        
        private IEnumerator CreateRecoveredPrimitive(Vector3 position, PrimitiveType primitiveType, float scale)
        {
            try
            {
                GameObject primitive = null;
                
                // まず標準的な方法で生成を試行
                try
                {
                    primitive = GameObject.CreatePrimitive(primitiveType);
                    primitive.transform.position = position;
                    primitive.transform.localScale = Vector3.one * scale;
                    primitive.name = $"Recovered_{primitiveType}_{Time.time}";
                }
                catch (Exception error)
                {
                    VastcoreLogger.Instance.LogWarning("PrimitiveRecovery", 
                        $"標準プリミティブ生成失敗: {error.Message}");
                    
                    if (primitive != null)
                    {
                        DestroyImmediate(primitive);
                        primitive = null;
                    }
                }
                
                // 標準生成が失敗した場合、フォールバック生成を試行
                if (primitive == null && useFallbackPrimitives)
                {
                    primitive = CreateFallbackPrimitive(position, primitiveType, scale);
                }
                
                // 生成されたプリミティブの検証と修正
                if (primitive != null)
                {
                    yield return StartCoroutine(ValidateAndFixPrimitive(primitive));
                }
                
                yield return primitive;
            }
            catch (Exception error)
            {
                VastcoreLogger.Instance.LogError("PrimitiveRecovery", 
                    $"回復プリミティブ作成中にエラー: {error.Message}", error);
                yield return null;
            }
        }
        
        private GameObject CreateFallbackPrimitive(Vector3 position, PrimitiveType originalType, float scale)
        {
            try
            {
                // フォールバックタイプを選択
                PrimitiveType fallbackType = GetFallbackType(originalType);
                
                GameObject fallbackPrimitive = new GameObject($"Fallback_{originalType}_{Time.time}");
                fallbackPrimitive.transform.position = position;
                fallbackPrimitive.transform.localScale = Vector3.one * scale;
                
                // メッシュコンポーネントの追加
                var meshFilter = fallbackPrimitive.AddComponent<MeshFilter>();
                var meshRenderer = fallbackPrimitive.AddComponent<MeshRenderer>();
                var meshCollider = fallbackPrimitive.AddComponent<MeshCollider>();
                
                // フォールバックメッシュの設定
                if (fallbackMeshCache.ContainsKey(fallbackType))
                {
                    meshFilter.mesh = fallbackMeshCache[fallbackType];
                    meshCollider.sharedMesh = fallbackMeshCache[fallbackType];
                }
                
                // フォールバック用マテリアルの設定
                meshRenderer.material = CreateFallbackMaterial(originalType);
                
                VastcoreLogger.Instance.LogInfo("PrimitiveRecovery", 
                    $"フォールバックプリミティブを生成: {originalType} -> {fallbackType}");
                
                return fallbackPrimitive;
            }
            catch (Exception error)
            {
                VastcoreLogger.Instance.LogError("PrimitiveRecovery", 
                    $"フォールバックプリミティブ生成中にエラー: {error.Message}");
                return null;
            }
        }
        
        private PrimitiveType GetFallbackType(PrimitiveType originalType)
        {
            // 複雑なプリミティブから簡単なものへのフォールバック
            switch (originalType)
            {
                case PrimitiveType.Capsule:
                case PrimitiveType.Cylinder:
                    return PrimitiveType.Cylinder;
                case PrimitiveType.Sphere:
                    return PrimitiveType.Sphere;
                default:
                    return PrimitiveType.Cube; // 最も安全なフォールバック
            }
        }
        
        private Material CreateFallbackMaterial(PrimitiveType originalType)
        {
            Material material = new Material(Shader.Find("Standard"));
            
            // 元のタイプに応じて色を変更
            switch (originalType)
            {
                case PrimitiveType.Cube:
                    material.color = Color.gray;
                    break;
                case PrimitiveType.Sphere:
                    material.color = Color.blue;
                    break;
                case PrimitiveType.Cylinder:
                    material.color = Color.green;
                    break;
                case PrimitiveType.Capsule:
                    material.color = Color.yellow;
                    break;
                default:
                    material.color = Color.magenta; // 未知のタイプ
                    break;
            }
            
            material.name = $"FallbackMaterial_{originalType}";
            return material;
        }
        
        private IEnumerator ValidateAndFixPrimitive(GameObject primitive)
        {
            try
            {
                // 必要なコンポーネントの確認と追加
                if (primitive.GetComponent<MeshFilter>() == null)
                {
                    primitive.AddComponent<MeshFilter>();
                }
                
                if (primitive.GetComponent<MeshRenderer>() == null)
                {
                    primitive.AddComponent<MeshRenderer>();
                }
                
                if (primitive.GetComponent<MeshCollider>() == null)
                {
                    primitive.AddComponent<MeshCollider>();
                }
                
                // メッシュの有効性確認
                var meshFilter = primitive.GetComponent<MeshFilter>();
                if (meshFilter.mesh == null || meshFilter.mesh.vertexCount == 0)
                {
                    VastcoreLogger.Instance.LogWarning("PrimitiveRecovery", "無効なメッシュを検出、修正中");
                    meshFilter.mesh = fallbackMeshCache[PrimitiveType.Cube];
                }
                
                // マテリアルの確認
                var meshRenderer = primitive.GetComponent<MeshRenderer>();
                if (meshRenderer.material == null)
                {
                    meshRenderer.material = CreateFallbackMaterial(PrimitiveType.Cube);
                }
                
                yield return null;
            }
            catch (Exception error)
            {
                VastcoreLogger.Instance.LogError("PrimitiveRecovery", 
                    $"プリミティブ検証中にエラー: {error.Message}");
            }
        }
        
        private void SetupCollisionRecovery(GameObject primitive)
        {
            try
            {
                // 衝突回復コンポーネントの追加
                var collisionRecovery = primitive.AddComponent<PrimitiveCollisionRecovery>();
                collisionRecovery.Initialize(this);
            }
            catch (Exception error)
            {
                VastcoreLogger.Instance.LogError("PrimitiveRecovery", 
                    $"衝突回復設定中にエラー: {error.Message}");
            }
        }
        
        private void InitializeFallbackMeshes()
        {
            try
            {
                foreach (PrimitiveType type in fallbackPrimitiveTypes)
                {
                    GameObject temp = GameObject.CreatePrimitive(type);
                    fallbackMeshCache[type] = temp.GetComponent<MeshFilter>().mesh;
                    DestroyImmediate(temp);
                }
                
                VastcoreLogger.Instance.LogInfo("PrimitiveRecovery", "フォールバックメッシュキャッシュを初期化しました");
            }
            catch (Exception error)
            {
                VastcoreLogger.Instance.LogError("PrimitiveRecovery", 
                    $"フォールバックメッシュ初期化中にエラー: {error.Message}");
            }
        }
        
        /// <summary>
        /// 配置済み位置のクリーンアップ
        /// </summary>
        public void CleanupOccupiedPositions(float maxDistance)
        {
            if (Camera.main == null) return;
            
            Vector3 playerPosition = Camera.main.transform.position;
            occupiedPositions.RemoveAll(pos => Vector3.Distance(pos, playerPosition) > maxDistance);
        }
        
        /// <summary>
        /// エラー回復統計の取得
        /// </summary>
        public int GetOccupiedPositionCount()
        {
            return occupiedPositions.Count;
        }
    }
    
    /// <summary>
    /// プリミティブオブジェクトの衝突回復コンポーネント
    /// </summary>
    public class PrimitiveCollisionRecovery : MonoBehaviour
    {
        private PrimitiveErrorRecovery parentRecovery;
        private Vector3 lastValidPosition;
        private float stuckCheckInterval = 1f;
        private float lastStuckCheck;
        
        public void Initialize(PrimitiveErrorRecovery recovery)
        {
            parentRecovery = recovery;
            lastValidPosition = transform.position;
        }
        
        private void Update()
        {
            if (Time.time - lastStuckCheck > stuckCheckInterval)
            {
                CheckForStuckState();
                lastStuckCheck = Time.time;
            }
        }
        
        private void CheckForStuckState()
        {
            try
            {
                // オブジェクトが地面に埋まっていないかチェック
                if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 1f))
                {
                    if (hit.distance < 0.1f) // 地面に埋まっている
                    {
                        RecoverFromStuckState();
                    }
                }
                
                lastValidPosition = transform.position;
            }
            catch (Exception error)
            {
                VastcoreLogger.Instance.LogError("CollisionRecovery", 
                    $"スタック状態チェック中にエラー: {error.Message}");
            }
        }
        
        private void RecoverFromStuckState()
        {
            try
            {
                VastcoreLogger.Instance.LogWarning("CollisionRecovery", 
                    $"スタック状態を検出、回復中: {gameObject.name}");
                
                // 最後の有効な位置に戻す
                transform.position = lastValidPosition + Vector3.up * 5f;
                
                // 物理的な力をリセット
                var rigidbody = GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.velocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                }
            }
            catch (Exception error)
            {
                VastcoreLogger.Instance.LogError("CollisionRecovery", 
                    $"スタック状態回復中にエラー: {error.Message}");
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            // 異常な衝突を検出した場合の処理
            if (collision.relativeVelocity.magnitude > 100f)
            {
                VastcoreLogger.Instance.LogWarning("CollisionRecovery", 
                    "異常な衝突を検出、速度を制限します");
                
                var rigidbody = GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    rigidbody.velocity = Vector3.ClampMagnitude(rigidbody.velocity, 50f);
                }
            }
        }
    }
}