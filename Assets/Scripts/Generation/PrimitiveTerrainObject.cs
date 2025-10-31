using UnityEngine;
using System.Collections.Generic;

#if DEFORM_AVAILABLE
using Deform;
#endif
using Vastcore.Core;

namespace Vastcore.Generation
{
    /// <summary>
    /// プリミティブ地形オブジェクトのコンポーネント
    /// 高度なLOD管理、インタラクション設定、動的調整を担当
    /// </summary>
    public class PrimitiveTerrainObject : MonoBehaviour
    {
        [Header("プリミティブ情報")]
        public GenerationPrimitiveType primitiveType;
        public float scale;
        public Vector3 originalPosition;
        public bool isAlignedToTerrain;
        
        [Header("高度なLOD設定")]
        public bool enableLOD = true;
        public float[] lodDistances = { 200f, 500f, 1000f, 2000f };
        public Mesh[] lodMeshes;
        
        // プール関連
        private bool isInPool;
        
        // コンポーネント参照
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private MeshFilter meshFilter;
        
        // インタラクション設定
        public bool hasCollision = true;
        public bool isClimbable;
        public bool isGrindable;
        
        // LOD管理
        private int currentLOD = -1;
        private float lastLODUpdateTime;
        private int lodChangeCount;
        private float totalDistanceChecked;
        private bool isVisible;
        
        // カリング設定
        private bool enableDistanceCulling = true;
        private float maxRenderDistance = 5000f;
        private bool enableFrustumCulling = true;
        
        // デバッグ設定
        private bool logLODChanges;
        private bool enableInteractionLOD;
        private bool showLODInfo;
        private float lodBias = 1f;
        
        [Header("Deform設定")]
        [SerializeField] private bool enableDeform = true;
        [SerializeField] private VastcoreDeformManager.DeformQualityLevel deformQualityLevel = VastcoreDeformManager.DeformQualityLevel.High;
        [SerializeField] private bool autoAddDeformableComponent = true;
        
        // Deformコンポーネント参照
#if DEFORM_AVAILABLE
        private Deformable deformableComponent;
        private List<Deformer> activeDeformers = new List<Deformer>();
#endif
        
        // プレイヤー参照
        private Transform playerTransform;
        private Camera playerCamera;
        
        // グローバル統計用
        private static Dictionary<GameObject, PrimitiveTerrainObject> activeObjects = new Dictionary<GameObject, PrimitiveTerrainObject>();
        
        void Awake()
        {
            SetupComponents();
            FindPlayerTransform();
            
            // グローバルリストに追加
            activeObjects[gameObject] = this;
        }

        void Start()
        {
            if (enableLOD && lodMeshes == null)
            {
                GenerateLODMeshes();
            }
        }

        void OnDestroy()
        {
            // グローバルリストから削除
            activeObjects.Remove(gameObject);
        }
        public void InitializeFromPool(GenerationPrimitiveType type, Vector3 position, float objectScale)
        {
            primitiveType = type;
            transform.position = position;
            originalPosition = position;
            scale = objectScale;
            transform.localScale = Vector3.one * objectScale;
            isInPool = false;
            
            // コンポーネントの再有効化
            if (meshRenderer != null) meshRenderer.enabled = true;
            if (meshCollider != null) meshCollider.enabled = hasCollision;
            
            // LODリセット
            currentLOD = -1; // 強制的に更新させる
            lastLODUpdateTime = 0f;
            
            gameObject.SetActive(true);
        }

        /// <summary>
        /// プールに戻す準備
        /// </summary>
        public void PrepareForPool()
        {
            isInPool = true;
            
            // コンポーネントの無効化
            if (meshRenderer != null) meshRenderer.enabled = false;
            if (meshCollider != null) meshCollider.enabled = false;
            
            // 統計リセット
            lodChangeCount = 0;
            totalDistanceChecked = 0f;
            
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 必要なコンポーネントを設定
        /// </summary>
        private void SetupComponents()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
            meshFilter = GetComponent<MeshFilter>();
            
#if DEFORM_AVAILABLE
            if (enableDeform)
            {
                SetupDeformComponents();
            }
#endif
            
            if (meshRenderer == null)
            {
                Debug.LogWarning($"MeshRenderer not found on {gameObject.name}");
            }
            
            if (meshFilter == null)
            {
                Debug.LogWarning($"MeshFilter not found on {gameObject.name}");
            }
            
            if (meshCollider == null && hasCollision)
            {
                Debug.LogWarning($"MeshCollider not found on {gameObject.name}");
            }
        }

#if DEFORM_AVAILABLE
        /// <summary>
        /// Deform関連コンポーネントを設定
        /// </summary>
        private void SetupDeformComponents()
        {
            // Deformableコンポーネントを取得または追加
            deformableComponent = GetComponent<Deformable>();
            if (deformableComponent == null && autoAddDeformableComponent)
            {
                deformableComponent = gameObject.AddComponent<Deformable>();
                deformableComponent.Quality = deformQualityLevel;
                
                // VastcoreDeformManagerに登録
                if (VastcoreDeformManager.Instance != null)
                {
                    VastcoreDeformManager.Instance.RegisterDeformable(this);
                }
            }
            else if (deformableComponent != null)
            {
                deformableComponent.Quality = deformQualityLevel;
            }
        }
#endif

        /// <summary>
        /// 基本的なノイズ変形を適用
        /// </summary>
        public void ApplyNoiseDeformation(float intensity = 0.1f, float frequency = 1f)
        {
#if DEFORM_AVAILABLE
            if (!enableDeform || deformableComponent == null) return;

            // Undo記録
            Undo.RecordObject(this, "Apply Noise Deformation");

            // 既存のNoiseDeformerを削除
            RemoveDeformer<NoiseDeformer>();

            // 新しいNoiseDeformerを追加
            var noiseDeformer = gameObject.AddComponent<NoiseDeformer>();
            noiseDeformer.Intensity = intensity;
            noiseDeformer.Frequency = frequency;
            activeDeformers.Add(noiseDeformer);

            // Deformシステムに通知
            if (VastcoreDeformManager.Instance != null)
            {
                VastcoreDeformManager.Instance.QueueDeformation(this, deformQualityLevel, 1);
            }
#endif
        }

        /// <summary>
        /// 基本的なディスプレイス変形を適用
        /// </summary>
        public void ApplyDisplaceDeformation(float strength = 0.5f, Texture2D displaceMap = null)
        {
#if DEFORM_AVAILABLE
            if (!enableDeform || deformableComponent == null) return;

            // Undo記録
            Undo.RecordObject(this, "Apply Displace Deformation");

            // 既存のDisplaceDeformerを削除
            RemoveDeformer<DisplaceDeformer>();

            // 新しいDisplaceDeformerを追加
            var displaceDeformer = gameObject.AddComponent<DisplaceDeformer>();
            displaceDeformer.Strength = strength;
            if (displaceMap != null)
            {
                displaceDeformer.Texture = displaceMap;
            }
            activeDeformers.Add(displaceDeformer);

            // Deformシステムに通知
            if (VastcoreDeformManager.Instance != null)
            {
                VastcoreDeformManager.Instance.QueueDeformation(this, deformQualityLevel, 1);
            }
#endif
        }

        /// <summary>
        /// 指定されたタイプのDeformerを削除
        /// </summary>
        private void RemoveDeformer<T>() where T : class
        {
#if DEFORM_AVAILABLE
            var existingDeformer = GetComponent<T>();
            if (existingDeformer != null)
            {
                activeDeformers.Remove(existingDeformer as Deformer);
                Destroy(existingDeformer as UnityEngine.Object);
            }
#endif
        }

        /// <summary>
        /// すべてのアクティブなDeformerをクリア
        /// </summary>
        public void ClearAllDeformers()
        {
#if DEFORM_AVAILABLE
            // Undo記録
            Undo.RecordObject(this, "Clear All Deformers");

            foreach (var deformer in activeDeformers.ToArray())
            {
                if (deformer != null)
                {
                    Destroy(deformer);
                }
            }
            activeDeformers.Clear();

            // Deformシステムに通知
            if (VastcoreDeformManager.Instance != null)
            {
                VastcoreDeformManager.Instance.QueueDeformation(this, deformQualityLevel, 1);
            }
#endif
        }

        /// <summary>
        /// Deform設定を動的に更新
        /// </summary>
        public void UpdateDeformSettings(bool enable, VastcoreDeformManager.DeformQualityLevel quality = VastcoreDeformManager.DeformQualityLevel.High)
        {
            enableDeform = enable;
            deformQualityLevel = quality;

#if DEFORM_AVAILABLE
            if (deformableComponent != null)
            {
                deformableComponent.Quality = (Deform.DeformQualityLevel)(int)quality;
            }

            // Deformシステムに通知
            if (VastcoreDeformManager.Instance != null)
            {
                if (enable)
                {
                    VastcoreDeformManager.Instance.RegisterDeformable(this, quality);
                }
                else
                {
                    VastcoreDeformManager.Instance.UnregisterDeformable(this);
                }
            }
#endif
        }

        /// <summary>
        /// 地形タイプに応じた固有の変形エフェクトを適用
        /// </summary>
        public void ApplyTerrainSpecificDeformation()
        {
#if DEFORM_AVAILABLE
            if (!enableDeform || deformableComponent == null) return;

            // 既存のDeformerをクリア
            ClearAllDeformers();

            // 地形タイプに応じた変形を適用
            switch (primitiveType)
            {
                case GenerationPrimitiveType.Crystal:
                    ApplyCrystalDeformation();
                    break;
                case GenerationPrimitiveType.Boulder:
                    ApplyBoulderDeformation();
                    break;
                case GenerationPrimitiveType.Mesa:
                    ApplyMesaDeformation();
                    break;
                case GenerationPrimitiveType.Spire:
                    ApplySpireDeformation();
                    break;
                case GenerationPrimitiveType.Formation:
                    ApplyFormationDeformation();
                    break;
                case GenerationPrimitiveType.Mountain:
                case GenerationPrimitiveType.Canyon:
                    ApplyGeologicalDeformation();
                    break;
                default:
                    // デフォルトの軽いノイズ変形
                    ApplyNoiseDeformation(0.05f, 0.5f);
                    break;
            }
#endif
        }

        /// <summary>
        /// 結晶構造の変形を適用
        /// </summary>
        private void ApplyCrystalDeformation()
        {
#if DEFORM_AVAILABLE
            // 結晶特有の鋭いエッジとファセット
            ApplyNoiseDeformation(0.15f, 2.0f);
#endif
        }

        /// <summary>
        /// 巨石の変形を適用
        /// </summary>
        private void ApplyBoulderDeformation()
        {
#if DEFORM_AVAILABLE
            // 岩石の不規則な表面
            ApplyNoiseDeformation(0.2f, 1.5f);
#endif
        }

        /// <summary>
        /// メサ（台地）の変形を適用
        /// </summary>
        private void ApplyMesaDeformation()
        {
#if DEFORM_AVAILABLE
            // 台地の平らな頂上部を保ちつつ、エッジを強調
            ApplyNoiseDeformation(0.08f, 0.8f);
#endif
        }

        /// <summary>
        /// 尖塔の変形を適用
        /// </summary>
        private void ApplySpireDeformation()
        {
#if DEFORM_AVAILABLE
            // 尖塔の細かい表面ディテール
            ApplyNoiseDeformation(0.1f, 1.2f);
#endif
        }

        /// <summary>
        /// 岩石層の変形を適用
        /// </summary>
        private void ApplyFormationDeformation()
        {
#if DEFORM_AVAILABLE
            // 層状構造の変形
            ApplyNoiseDeformation(0.12f, 0.7f);
#endif
        }

        /// <summary>
        /// 地質学的変形を適用（山岳・峡谷）
        /// </summary>
        private void ApplyGeologicalDeformation()
        {
#if DEFORM_AVAILABLE
            // 大規模な地質変動をシミュレート
            ApplyNoiseDeformation(0.25f, 0.3f);
#endif
        }

        /// <summary>
        /// アニメーション付きノイズ変形を適用
        /// </summary>
        public void ApplyNoiseDeformationAnimated(float targetIntensity, float targetFrequency, float duration = 1f)
        {
#if DEFORM_AVAILABLE
            if (!enableDeform || deformableComponent == null) return;

            StartCoroutine(AnimateNoiseDeformation(targetIntensity, targetFrequency, duration));
#endif
        }

        /// <summary>
        /// アニメーション付きディスプレイス変形を適用
        /// </summary>
        public void ApplyDisplaceDeformationAnimated(float targetStrength, Texture2D displaceMap, float duration = 1f)
        {
#if DEFORM_AVAILABLE
            if (!enableDeform || deformableComponent == null) return;

            StartCoroutine(AnimateDisplaceDeformation(targetStrength, displaceMap, duration));
#endif
        }

        /// <summary>
        /// ノイズ変形アニメーションのコルーチン
        /// </summary>
        private System.Collections.IEnumerator AnimateNoiseDeformation(float targetIntensity, float targetFrequency, float duration)
        {
#if DEFORM_AVAILABLE
            var noiseDeformer = GetComponent<NoiseDeformer>();
            if (noiseDeformer == null)
            {
                noiseDeformer = gameObject.AddComponent<NoiseDeformer>();
                activeDeformers.Add(noiseDeformer);
            }

            float startIntensity = noiseDeformer.Intensity;
            float startFrequency = noiseDeformer.Frequency;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // イーズイン・イーズアウト補間
                t = t * t * (3f - 2f * t);
                
                noiseDeformer.Intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
                noiseDeformer.Frequency = Mathf.Lerp(startFrequency, targetFrequency, t);
                
                // Deformシステムに通知
                if (VastcoreDeformManager.Instance != null)
                {
                    VastcoreDeformManager.Instance.QueueDeformation(this, deformQualityLevel, 0.5f);
                }
                
                yield return null;
            }

            // 最終値を設定
            noiseDeformer.Intensity = targetIntensity;
            noiseDeformer.Frequency = targetFrequency;

            // 最終通知
            if (VastcoreDeformManager.Instance != null)
            {
                VastcoreDeformManager.Instance.QueueDeformation(this, deformQualityLevel, 1);
            }
#endif
            yield break;
        }

        /// <summary>
        /// ディスプレイス変形アニメーションのコルーチン
        /// </summary>
        private System.Collections.IEnumerator AnimateDisplaceDeformation(float targetStrength, Texture2D displaceMap, float duration)
        {
#if DEFORM_AVAILABLE
            var displaceDeformer = GetComponent<DisplaceDeformer>();
            if (displaceDeformer == null)
            {
                displaceDeformer = gameObject.AddComponent<DisplaceDeformer>();
                activeDeformers.Add(displaceDeformer);
            }

            float startStrength = displaceDeformer.Strength;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // イーズイン・イーズアウト補間
                t = t * t * (3f - 2f * t);
                
                displaceDeformer.Strength = Mathf.Lerp(startStrength, targetStrength, t);
                if (displaceMap != null)
                {
                    displaceDeformer.Texture = displaceMap;
                }
                
                // Deformシステムに通知
                if (VastcoreDeformManager.Instance != null)
                {
                    VastcoreDeformManager.Instance.QueueDeformation(this, deformQualityLevel, 0.5f);
                }
                
                yield return null;
            }

            // 最終値を設定
            displaceDeformer.Strength = targetStrength;
            if (displaceMap != null)
            {
                displaceDeformer.Texture = displaceMap;
            }

            // 最終通知
            if (VastcoreDeformManager.Instance != null)
            {
                VastcoreDeformManager.Instance.QueueDeformation(this, deformQualityLevel, 1);
            }
#endif
            yield break;
        }

        /// <summary>
        /// Deformプリセットを適用
        /// </summary>
        public void ApplyDeformationPreset(Vastcore.Generation.DeformationPreset preset)
        {
            if (preset != null)
            {
                preset.ApplyToTerrainObject(this);
            }
        }

        /// <summary>
        /// プレイヤーのTransformとCameraを検索
        /// </summary>
        private void FindPlayerTransform()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                
                // プレイヤーのカメラを取得
                playerCamera = player.GetComponentInChildren<Camera>();
                if (playerCamera == null)
                {
                    playerCamera = Camera.main;
                }
            }
            else
            {
                Debug.LogWarning("Player not found for LOD calculation");
                playerCamera = Camera.main;
            }
        }

        /// <summary>
        /// LODメッシュを生成
        /// </summary>
        private void GenerateLODMeshes()
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                return;
            }

            lodMeshes = new Mesh[lodDistances.Length + 1];
            lodMeshes[0] = meshFilter.sharedMesh; // 最高品質

            // 簡易的なLODメッシュ生成（実際の実装では専用のメッシュ簡略化アルゴリズムを使用）
            for (int i = 1; i < lodMeshes.Length; i++)
            {
                lodMeshes[i] = CreateSimplifiedMesh(meshFilter.sharedMesh, i);
            }
        }

        /// <summary>
        /// 簡略化されたメッシュを作成
        /// </summary>
        private Mesh CreateSimplifiedMesh(Mesh originalMesh, int lodLevel)
        {
            // 簡易的な実装：頂点数を減らす
            var vertices = originalMesh.vertices;
            var triangles = originalMesh.triangles;
            var uvs = originalMesh.uv;

            // LODレベルに応じて頂点を間引く
            int skipFactor = lodLevel + 1;
            var simplifiedVertices = new System.Collections.Generic.List<Vector3>();
            var simplifiedUVs = new System.Collections.Generic.List<Vector2>();
            var vertexMap = new System.Collections.Generic.Dictionary<int, int>();

            for (int i = 0; i < vertices.Length; i += skipFactor)
            {
                vertexMap[i] = simplifiedVertices.Count;
                simplifiedVertices.Add(vertices[i]);
                if (i < uvs.Length)
                {
                    simplifiedUVs.Add(uvs[i]);
                }
            }

            // 三角形を再構築
            var simplifiedTriangles = new System.Collections.Generic.List<int>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v1 = triangles[i];
                int v2 = triangles[i + 1];
                int v3 = triangles[i + 2];

                if (vertexMap.ContainsKey(v1) && vertexMap.ContainsKey(v2) && vertexMap.ContainsKey(v3))
                {
                    simplifiedTriangles.Add(vertexMap[v1]);
                    simplifiedTriangles.Add(vertexMap[v2]);
                    simplifiedTriangles.Add(vertexMap[v3]);
                }
            }

            var simplifiedMesh = new Mesh();
            simplifiedMesh.vertices = simplifiedVertices.ToArray();
            simplifiedMesh.triangles = simplifiedTriangles.ToArray();
            simplifiedMesh.uv = simplifiedUVs.ToArray();
            simplifiedMesh.RecalculateNormals();
            simplifiedMesh.name = $"{originalMesh.name}_LOD{lodLevel}";

            return simplifiedMesh;
        }
        private void UpdateLODSystem()
        {
            if (playerTransform == null || isInPool) return;

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            totalDistanceChecked += distanceToPlayer;

            // 距離カリング
            if (enableDistanceCulling && distanceToPlayer > maxRenderDistance)
            {
                SetObjectVisibility(false);
                return;
            }

            // フラスタムカリング
            if (enableFrustumCulling && playerCamera != null)
            {
                if (!IsInCameraFrustum())
                {
                    SetObjectVisibility(false);
                    return;
                }
            }

            SetObjectVisibility(true);
            UpdateLOD(distanceToPlayer);
        }

        /// <summary>
        /// プレイヤーとの距離に基づいてLODを更新
        /// </summary>
        public void UpdateLOD(float distanceToPlayer)
        {
            int newLOD = CalculateLOD(distanceToPlayer);
            if (newLOD != currentLOD)
            {
                ApplyLOD(newLOD);
                
                if (logLODChanges)
                {
                    Debug.Log($"{gameObject.name}: LOD changed from {currentLOD} to {newLOD} at distance {distanceToPlayer:F1}m");
                }
                
                currentLOD = newLOD;
                lodChangeCount++;
            }
        }

        /// <summary>
        /// オブジェクトの可視性を設定
        /// </summary>
        private void SetObjectVisibility(bool visible)
        {
            if (isVisible == visible) return;
            
            isVisible = visible;
            
            if (meshRenderer != null)
            {
                meshRenderer.enabled = visible;
            }
            
            // 遠距離では物理コライダーも無効化してパフォーマンス向上
            if (meshCollider != null && hasCollision)
            {
                meshCollider.enabled = visible;
            }
        }

        /// <summary>
        /// カメラのフラスタム内にあるかチェック
        /// </summary>
        private bool IsInCameraFrustum()
        {
            if (playerCamera == null) return true;

            var bounds = GetObjectBounds();
            var planes = GeometryUtility.CalculateFrustumPlanes(playerCamera);
            return GeometryUtility.TestPlanesAABB(planes, bounds);
        }

        /// <summary>
        /// オブジェクトの境界ボックスを取得
        /// </summary>
        private Bounds GetObjectBounds()
        {
            if (meshRenderer != null)
            {
                return meshRenderer.bounds;
            }
            
            // フォールバック：transform位置を中心とした境界
            return new Bounds(transform.position, Vector3.one * scale);
        }

        /// <summary>
        /// 距離に基づいてLODレベルを計算（バイアス適用）
        /// </summary>
        private int CalculateLOD(float distance)
        {
            // LODバイアスを適用
            float adjustedDistance = distance / lodBias;
            
            for (int i = 0; i < lodDistances.Length; i++)
            {
                if (adjustedDistance < lodDistances[i])
                    return i;
            }
            return lodDistances.Length; // 最遠距離
        }

        /// <summary>
        /// 指定されたLODレベルを適用
        /// </summary>
        private void ApplyLOD(int lodLevel)
        {
            if (lodLevel < lodMeshes.Length && lodMeshes[lodLevel] != null)
            {
                currentLOD = lodLevel;
                lodChangeCount++;
                
                if (meshFilter != null && lodMeshes != null && lodLevel < lodMeshes.Length)
                {
                    meshFilter.sharedMesh = lodMeshes[lodLevel];
                }
                
                lastLODUpdateTime = Time.time;
                
                // コライダーメッシュも更新（パフォーマンス重視の場合は低LODメッシュを使用）
                if (meshCollider != null && hasCollision)
                {
                    // 高LOD時は詳細コライダー、低LOD時は簡略コライダー
                    int colliderLOD = Mathf.Min(lodLevel + 1, lodMeshes.Length - 1);
                    if (colliderLOD < lodMeshes.Length && lodMeshes[colliderLOD] != null)
                    {
                        meshCollider.sharedMesh = lodMeshes[colliderLOD];
                    }
                    meshCollider.enabled = true;
                }

                // レンダラーを有効化
                if (meshRenderer != null)
                {
                    meshRenderer.enabled = isVisible;
                }
            }
            else
            {
                // 最遠距離では非表示
                SetObjectVisibility(false);
            }
        }

        /// <summary>
        /// インタラクション可能かどうかを判定
        /// </summary>
        public bool CanInteract(string interactionType)
        {
            // 遠距離ではインタラクション無効
            if (enableInteractionLOD && currentLOD > 2)
            {
                return false;
            }

            switch (interactionType.ToLower())
            {
                case "climb":
                    return isClimbable && isVisible;
                case "grind":
                    return isGrindable && isVisible;
                case "collision":
                    return hasCollision && isVisible;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 現在のLOD情報を取得
        /// </summary>
        public LODInfo GetLODInfo()
        {
            float distance = playerTransform != null ? 
                Vector3.Distance(transform.position, playerTransform.position) : 0f;
                
            return new LODInfo
            {
                currentLOD = currentLOD,
                distance = distance,
                isVisible = isVisible,
                lodChangeCount = lodChangeCount,
                averageDistance = lodChangeCount > 0 ? totalDistanceChecked / lodChangeCount : distance
            };
        }

        /// <summary>
        /// LOD設定を動的に調整
        /// </summary>
        public void AdjustLODSettings(float[] newDistances, float newBias = 1.0f)
        {
            lodDistances = newDistances;
            lodBias = newBias;
            
            // 現在のLODを再計算
            if (playerTransform != null)
            {
                float distance = Vector3.Distance(transform.position, playerTransform.position);
                currentLOD = -1; // 強制更新
                UpdateLOD(distance);
            }
        }

        /// <summary>
        /// 静的メソッド：全アクティブオブジェクトのLOD統計を取得
        /// </summary>
        public static LODStatistics GetGlobalLODStatistics()
        {
            var stats = new LODStatistics();
            
            foreach (var obj in activeObjects.Values)
            {
                if (obj != null && !obj.isInPool)
                {
                    stats.totalObjects++;
                    stats.lodCounts[Mathf.Clamp(obj.currentLOD, 0, stats.lodCounts.Length - 1)]++;
                    
                    if (obj.isVisible)
                        stats.visibleObjects++;
                }
            }
            
            return stats;
        }

        /// <summary>
        /// デバッグ情報を描画
        /// </summary>
        void OnDrawGizmosSelected()
        {
            // LOD距離を可視化
            Gizmos.color = Color.yellow;
            for (int i = 0; i < lodDistances.Length; i++)
            {
                Gizmos.DrawWireSphere(transform.position, lodDistances[i]);
            }

            // 現在のLODレベルを色で表示
            switch (currentLOD)
            {
                case 0: Gizmos.color = Color.green; break;   // 最高品質
                case 1: Gizmos.color = Color.yellow; break;  // 高品質
                case 2: Gizmos.color = Color.orange; break;  // 中品質
                case 3: Gizmos.color = Color.red; break;     // 低品質
                default: Gizmos.color = Color.gray; break;   // 非表示
            }
            
            Gizmos.DrawWireCube(transform.position, Vector3.one * (scale * 0.1f));

            // 最大描画距離を表示
            if (enableDistanceCulling)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, maxRenderDistance);
            }
        }

        void OnGUI()
        {
            if (showLODInfo && playerTransform != null)
            {
                var screenPos = Camera.main.WorldToScreenPoint(transform.position);
                if (screenPos.z > 0)
                {
                    var rect = new Rect(screenPos.x, Screen.height - screenPos.y, 200, 60);
                    var info = GetLODInfo();
                    
                    GUI.Box(rect, $"LOD: {info.currentLOD}\nDist: {info.distance:F1}m\nVisible: {info.isVisible}");
                }
            }
        }
    }

    /// <summary>
    /// LOD情報を格納する構造体
    /// </summary>
    [System.Serializable]
    public struct LODInfo
    {
        public int currentLOD;
        public float distance;
        public bool isVisible;
        public int lodChangeCount;
        public float averageDistance;
    }

    /// <summary>
    /// 全体のLOD統計情報
    /// </summary>
    [System.Serializable]
    public struct LODStatistics
    {
        public int totalObjects;
        public int visibleObjects;
        public int[] lodCounts;

        public LODStatistics(int lodLevels = 5)
        {
            totalObjects = 0;
            visibleObjects = 0;
            lodCounts = new int[lodLevels];
        }
    }
}