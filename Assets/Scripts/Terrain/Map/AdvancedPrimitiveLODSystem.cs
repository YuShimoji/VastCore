using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// 高度なプリミティブオブジェクトLOD最適化システム
    /// 距離別メッシュ品質調整、インポスターシステム、視界外オブジェクト管理
    /// 要求: 6.5 メモリ効率とパフォーマンス
    /// </summary>
    public class AdvancedPrimitiveLODSystem : MonoBehaviour
    {
        [Header("LOD設定")]
        [SerializeField] private bool enableAdvancedLOD = true;
        [SerializeField] private float[] lodDistances = { 100f, 300f, 600f, 1200f, 2500f };
        [SerializeField] private float[] meshQualityFactors = { 1.0f, 0.75f, 0.5f, 0.25f, 0.1f };
        [SerializeField] private float updateInterval = 0.1f;
        
        [Header("インポスターシステム")]
        [SerializeField] private bool enableImpostorSystem = true;
        [SerializeField] private float impostorDistance = 1500f;
        [SerializeField] private int impostorTextureSize = 256;
        [SerializeField] private LayerMask impostorLayer = 1 << 31;
        [SerializeField] private Material impostorMaterial;
        
        [Header("視界外最適化")]
        [SerializeField] private bool enableFrustumCulling = true;
        [SerializeField] private bool enableOcclusionCulling = true;
        [SerializeField] private float offScreenUpdateInterval = 1f;
        [SerializeField] private float maxOffScreenDistance = 3000f;
        
        [Header("動的品質調整")]
        [SerializeField] private bool enableDynamicQuality = true;
        [SerializeField] private float targetFrameRate = 60f;
        [SerializeField] private float qualityAdjustmentSpeed = 0.1f;
        [SerializeField] private int maxObjectsPerFrame = 10;
        
        [Header("メモリ管理")]
        [SerializeField] private bool enableMemoryOptimization = true;
        [SerializeField] private int maxCachedMeshes = 100;
        [SerializeField] private float meshCacheTimeout = 30f;
        
        // 内部状態
        private Dictionary<PrimitiveTerrainObject, AdvancedLODData> lodDataMap;
        private Queue<PrimitiveTerrainObject> updateQueue;
        private Dictionary<string, CachedMeshData> meshCache;
        private Dictionary<PrimitiveTerrainObject, ImpostorData> impostorCache;
        
        private Transform playerTransform;
        private Camera playerCamera;
        private Coroutine lodUpdateCoroutine;
        private Coroutine offScreenUpdateCoroutine;
        
        private float currentQualityMultiplier = 1f;
        private int frameUpdateCount = 0;
        
        // データ構造
        [System.Serializable]
        public class AdvancedLODData
        {
            public int currentLOD;
            public float lastUpdateTime;
            public float distanceToPlayer;
            public bool isVisible;
            public bool isInFrustum;
            public bool isOccluded;
            public bool useImpostor;
            public Mesh[] qualityMeshes;
            public float lastVisibilityCheck;
            public Vector3 lastKnownPosition;
        }
        
        [System.Serializable]
        public class CachedMeshData
        {
            public Mesh mesh;
            public float lastAccessTime;
            public int referenceCount;
            public string meshKey;
        }
        
        [System.Serializable]
        public class ImpostorData
        {
            public Texture2D impostorTexture;
            public GameObject impostorQuad;
            public Vector3 capturePosition;
            public Quaternion captureRotation;
            public float lastUpdateTime;
            public bool needsUpdate;
        }
        
        private void Awake()
        {
            lodDataMap = new Dictionary<PrimitiveTerrainObject, AdvancedLODData>();
            updateQueue = new Queue<PrimitiveTerrainObject>();
            meshCache = new Dictionary<string, CachedMeshData>();
            impostorCache = new Dictionary<PrimitiveTerrainObject, ImpostorData>();
        }
        
        private void Start()
        {
            FindPlayerComponents();
            
            if (enableAdvancedLOD)
            {
                StartLODSystem();
            }
        }
        
        private void Update()
        {
            if (enableDynamicQuality)
            {
                AdjustQualityBasedOnPerformance();
            }
        }
        
        /// <summary>
        /// プレイヤーコンポーネントを検索
        /// </summary>
        private void FindPlayerComponents()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerCamera = player.GetComponentInChildren<Camera>();
            }
            
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }
        
        /// <summary>
        /// LODシステムを開始
        /// </summary>
        public void StartLODSystem()
        {
            if (lodUpdateCoroutine == null)
            {
                lodUpdateCoroutine = StartCoroutine(LODUpdateCoroutine());
            }
            
            if (offScreenUpdateCoroutine == null)
            {
                offScreenUpdateCoroutine = StartCoroutine(OffScreenUpdateCoroutine());
            }
        }
        
        /// <summary>
        /// LODシステムを停止
        /// </summary>
        public void StopLODSystem()
        {
            if (lodUpdateCoroutine != null)
            {
                StopCoroutine(lodUpdateCoroutine);
                lodUpdateCoroutine = null;
            }
            
            if (offScreenUpdateCoroutine != null)
            {
                StopCoroutine(offScreenUpdateCoroutine);
                offScreenUpdateCoroutine = null;
            }
        }
        
        /// <summary>
        /// プリミティブオブジェクトを登録
        /// </summary>
        public void RegisterPrimitiveObject(PrimitiveTerrainObject primitiveObject)
        {
            if (primitiveObject == null || lodDataMap.ContainsKey(primitiveObject))
                return;
            
            var lodData = new AdvancedLODData
            {
                currentLOD = 0,
                lastUpdateTime = 0f,
                distanceToPlayer = float.MaxValue,
                isVisible = true,
                isInFrustum = true,
                isOccluded = false,
                useImpostor = false,
                lastVisibilityCheck = 0f,
                lastKnownPosition = primitiveObject.transform.position
            };
            
            // 品質別メッシュを生成
            lodData.qualityMeshes = GenerateQualityMeshes(primitiveObject);
            
            lodDataMap[primitiveObject] = lodData;
            updateQueue.Enqueue(primitiveObject);
        }
        
        /// <summary>
        /// プリミティブオブジェクトを登録解除
        /// </summary>
        public void UnregisterPrimitiveObject(PrimitiveTerrainObject primitiveObject)
        {
            if (!lodDataMap.ContainsKey(primitiveObject))
                return;
            
            var lodData = lodDataMap[primitiveObject];
            
            // メッシュキャッシュから参照を削除
            if (lodData.qualityMeshes != null)
            {
                foreach (var mesh in lodData.qualityMeshes)
                {
                    if (mesh != null)
                    {
                        ReleaseCachedMesh(mesh.name);
                    }
                }
            }
            
            // インポスターデータを削除
            if (impostorCache.ContainsKey(primitiveObject))
            {
                DestroyImpostorData(primitiveObject);
            }
            
            lodDataMap.Remove(primitiveObject);
        }
        
        /// <summary>
        /// LOD更新コルーチン
        /// </summary>
        private IEnumerator LODUpdateCoroutine()
        {
            while (enableAdvancedLOD)
            {
                frameUpdateCount = 0;
                var startTime = Time.realtimeSinceStartup;
                
                // キューからオブジェクトを処理
                while (updateQueue.Count > 0 && frameUpdateCount < maxObjectsPerFrame)
                {
                    var primitiveObject = updateQueue.Dequeue();
                    
                    if (primitiveObject != null && lodDataMap.ContainsKey(primitiveObject))
                    {
                        UpdatePrimitiveLOD(primitiveObject);
                        frameUpdateCount++;
                        
                        // フレーム時間制限
                        if ((Time.realtimeSinceStartup - startTime) > (1f / targetFrameRate) * 0.5f)
                        {
                            break;
                        }
                    }
                }
                
                // 残りのオブジェクトを再キューイング
                var remainingObjects = new List<PrimitiveTerrainObject>(lodDataMap.Keys);
                foreach (var obj in remainingObjects)
                {
                    if (ShouldUpdateLOD(obj))
                    {
                        updateQueue.Enqueue(obj);
                    }
                }
                
                yield return new WaitForSeconds(updateInterval);
            }
        }
        
        /// <summary>
        /// 視界外オブジェクト更新コルーチン
        /// </summary>
        private IEnumerator OffScreenUpdateCoroutine()
        {
            while (enableAdvancedLOD)
            {
                foreach (var kvp in lodDataMap)
                {
                    var primitiveObject = kvp.Key;
                    var lodData = kvp.Value;
                    
                    if (primitiveObject != null && !lodData.isInFrustum)
                    {
                        UpdateOffScreenObject(primitiveObject, lodData);
                    }
                }
                
                yield return new WaitForSeconds(offScreenUpdateInterval);
            }
        }
        
        /// <summary>
        /// プリミティブオブジェクトのLODを更新
        /// </summary>
        private void UpdatePrimitiveLOD(PrimitiveTerrainObject primitiveObject)
        {
            if (!lodDataMap.ContainsKey(primitiveObject))
                return;
            
            var lodData = lodDataMap[primitiveObject];
            
            // 距離計算
            float distance = CalculateDistance(primitiveObject);
            lodData.distanceToPlayer = distance;
            
            // 視界判定
            bool wasInFrustum = lodData.isInFrustum;
            lodData.isInFrustum = IsInCameraFrustum(primitiveObject);
            lodData.isVisible = lodData.isInFrustum && distance <= maxOffScreenDistance;
            
            // オクルージョン判定
            if (enableOcclusionCulling && lodData.isInFrustum)
            {
                lodData.isOccluded = IsOccluded(primitiveObject);
                lodData.isVisible = lodData.isVisible && !lodData.isOccluded;
            }
            
            // インポスター判定
            bool shouldUseImpostor = enableImpostorSystem && distance > impostorDistance && lodData.isVisible;
            
            if (shouldUseImpostor != lodData.useImpostor)
            {
                if (shouldUseImpostor)
                {
                    CreateImpostor(primitiveObject);
                }
                else
                {
                    DestroyImpostor(primitiveObject);
                }
                lodData.useImpostor = shouldUseImpostor;
            }
            
            // LODレベル計算と適用
            if (!lodData.useImpostor && lodData.isVisible)
            {
                int newLOD = CalculateOptimalLOD(distance);
                if (newLOD != lodData.currentLOD)
                {
                    ApplyLODToPrimitive(primitiveObject, newLOD);
                    lodData.currentLOD = newLOD;
                }
            }
            else if (!lodData.isVisible)
            {
                // 非表示オブジェクトの処理
                DisablePrimitiveRendering(primitiveObject);
            }
            
            lodData.lastUpdateTime = Time.time;
            lodData.lastKnownPosition = primitiveObject.transform.position;
        }
        
        /// <summary>
        /// 視界外オブジェクトの更新
        /// </summary>
        private void UpdateOffScreenObject(PrimitiveTerrainObject primitiveObject, AdvancedLODData lodData)
        {
            float distance = CalculateDistance(primitiveObject);
            
            // 遠距離オブジェクトは完全に無効化
            if (distance > maxOffScreenDistance)
            {
                DisablePrimitiveRendering(primitiveObject);
                return;
            }
            
            // 位置が大きく変わった場合は再評価
            float positionDelta = Vector3.Distance(primitiveObject.transform.position, lodData.lastKnownPosition);
            if (positionDelta > 50f)
            {
                updateQueue.Enqueue(primitiveObject);
            }
        }
        
        /// <summary>
        /// 品質別メッシュを生成
        /// </summary>
        private Mesh[] GenerateQualityMeshes(PrimitiveTerrainObject primitiveObject)
        {
            var meshFilter = primitiveObject.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
                return new Mesh[0];
            
            var originalMesh = meshFilter.sharedMesh;
            var qualityMeshes = new Mesh[meshQualityFactors.Length];
            
            for (int i = 0; i < meshQualityFactors.Length; i++)
            {
                float qualityFactor = meshQualityFactors[i] * currentQualityMultiplier;
                string meshKey = $"{originalMesh.name}_Q{qualityFactor:F2}";
                
                // キャッシュから取得または生成
                if (meshCache.ContainsKey(meshKey))
                {
                    qualityMeshes[i] = meshCache[meshKey].mesh;
                    meshCache[meshKey].referenceCount++;
                    meshCache[meshKey].lastAccessTime = Time.time;
                }
                else
                {
                    qualityMeshes[i] = GenerateQualityMesh(originalMesh, qualityFactor, meshKey);
                    CacheMesh(meshKey, qualityMeshes[i]);
                }
            }
            
            return qualityMeshes;
        }
        
        /// <summary>
        /// 品質調整されたメッシュを生成
        /// </summary>
        private Mesh GenerateQualityMesh(Mesh originalMesh, float qualityFactor, string meshKey)
        {
            if (qualityFactor >= 1f)
                return originalMesh;
            
            var vertices = originalMesh.vertices;
            var triangles = originalMesh.triangles;
            var uvs = originalMesh.uv;
            var normals = originalMesh.normals;
            
            // 品質に基づいて頂点を間引く
            int targetVertexCount = Mathf.RoundToInt(vertices.Length * qualityFactor);
            targetVertexCount = Mathf.Max(targetVertexCount, 12); // 最小頂点数を保証
            
            var reducedMesh = ReduceMeshComplexity(vertices, triangles, uvs, normals, targetVertexCount);
            reducedMesh.name = meshKey;
            
            return reducedMesh;
        }
        
        /// <summary>
        /// メッシュの複雑さを削減
        /// </summary>
        private Mesh ReduceMeshComplexity(Vector3[] vertices, int[] triangles, Vector2[] uvs, Vector3[] normals, int targetVertexCount)
        {
            // 簡易的な頂点削減アルゴリズム
            var vertexImportance = CalculateVertexImportance(vertices, triangles);
            var keepVertices = SelectImportantVertices(vertexImportance, targetVertexCount);
            
            var newVertices = new List<Vector3>();
            var newUVs = new List<Vector2>();
            var newNormals = new List<Vector3>();
            var vertexMap = new Dictionary<int, int>();
            
            // 重要な頂点を保持
            for (int i = 0; i < vertices.Length; i++)
            {
                if (keepVertices.Contains(i))
                {
                    vertexMap[i] = newVertices.Count;
                    newVertices.Add(vertices[i]);
                    if (i < uvs.Length) newUVs.Add(uvs[i]);
                    if (i < normals.Length) newNormals.Add(normals[i]);
                }
            }
            
            // 三角形を再構築
            var newTriangles = new List<int>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v1 = triangles[i];
                int v2 = triangles[i + 1];
                int v3 = triangles[i + 2];
                
                if (vertexMap.ContainsKey(v1) && vertexMap.ContainsKey(v2) && vertexMap.ContainsKey(v3))
                {
                    newTriangles.Add(vertexMap[v1]);
                    newTriangles.Add(vertexMap[v2]);
                    newTriangles.Add(vertexMap[v3]);
                }
            }
            
            var reducedMesh = new Mesh();
            reducedMesh.vertices = newVertices.ToArray();
            reducedMesh.triangles = newTriangles.ToArray();
            reducedMesh.uv = newUVs.ToArray();
            
            if (newNormals.Count == newVertices.Count)
            {
                reducedMesh.normals = newNormals.ToArray();
            }
            else
            {
                reducedMesh.RecalculateNormals();
            }
            
            reducedMesh.RecalculateBounds();
            return reducedMesh;
        }
        
        /// <summary>
        /// 頂点の重要度を計算
        /// </summary>
        private float[] CalculateVertexImportance(Vector3[] vertices, int[] triangles)
        {
            var importance = new float[vertices.Length];
            
            // 各頂点が使用される三角形の数をカウント
            for (int i = 0; i < triangles.Length; i++)
            {
                importance[triangles[i]]++;
            }
            
            // 正規化
            float maxImportance = Mathf.Max(importance);
            if (maxImportance > 0)
            {
                for (int i = 0; i < importance.Length; i++)
                {
                    importance[i] /= maxImportance;
                }
            }
            
            return importance;
        }
        
        /// <summary>
        /// 重要な頂点を選択
        /// </summary>
        private HashSet<int> SelectImportantVertices(float[] importance, int targetCount)
        {
            var vertexImportancePairs = new List<System.Tuple<int, float>>();
            
            for (int i = 0; i < importance.Length; i++)
            {
                vertexImportancePairs.Add(new System.Tuple<int, float>(i, importance[i]));
            }
            
            // 重要度でソート
            vertexImportancePairs.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            
            var selectedVertices = new HashSet<int>();
            int count = Mathf.Min(targetCount, vertexImportancePairs.Count);
            
            for (int i = 0; i < count; i++)
            {
                selectedVertices.Add(vertexImportancePairs[i].Item1);
            }
            
            return selectedVertices;
        }
        
        /// <summary>
        /// インポスターを作成
        /// </summary>
        private void CreateImpostor(PrimitiveTerrainObject primitiveObject)
        {
            if (impostorCache.ContainsKey(primitiveObject))
                return;
            
            var impostorData = new ImpostorData
            {
                capturePosition = playerCamera.transform.position,
                captureRotation = playerCamera.transform.rotation,
                lastUpdateTime = Time.time,
                needsUpdate = true
            };
            
            // インポスタークアッドを作成
            impostorData.impostorQuad = CreateImpostorQuad(primitiveObject);
            
            // テクスチャをキャプチャ
            impostorData.impostorTexture = CaptureImpostorTexture(primitiveObject);
            
            // インポスターマテリアルを適用
            ApplyImpostorMaterial(impostorData.impostorQuad, impostorData.impostorTexture);
            
            impostorCache[primitiveObject] = impostorData;
            
            // 元のオブジェクトを非表示
            primitiveObject.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// インポスタークアッドを作成
        /// </summary>
        private GameObject CreateImpostorQuad(PrimitiveTerrainObject primitiveObject)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = $"Impostor_{primitiveObject.name}";
            quad.transform.position = primitiveObject.transform.position;
            quad.transform.rotation = primitiveObject.transform.rotation;
            
            // サイズを調整
            var bounds = primitiveObject.GetComponent<MeshRenderer>().bounds;
            float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            quad.transform.localScale = Vector3.one * size;
            
            // レイヤーを設定
            quad.layer = (int)Mathf.Log(impostorLayer.value, 2);
            
            return quad;
        }
        
        /// <summary>
        /// インポスターテクスチャをキャプチャ
        /// </summary>
        private Texture2D CaptureImpostorTexture(PrimitiveTerrainObject primitiveObject)
        {
            // 一時的なカメラを作成
            var tempCamera = new GameObject("TempImpostorCamera").AddComponent<Camera>();
            tempCamera.transform.position = playerCamera.transform.position;
            tempCamera.transform.LookAt(primitiveObject.transform.position);
            
            // レンダーテクスチャを設定
            var renderTexture = new RenderTexture(impostorTextureSize, impostorTextureSize, 24);
            tempCamera.targetTexture = renderTexture;
            tempCamera.cullingMask = ~impostorLayer; // インポスターレイヤー以外をレンダリング
            
            // レンダリング実行
            tempCamera.Render();
            
            // テクスチャに変換
            RenderTexture.active = renderTexture;
            var texture = new Texture2D(impostorTextureSize, impostorTextureSize, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, impostorTextureSize, impostorTextureSize), 0, 0);
            texture.Apply();
            
            // クリーンアップ
            RenderTexture.active = null;
            tempCamera.targetTexture = null;
            DestroyImmediate(tempCamera.gameObject);
            DestroyImmediate(renderTexture);
            
            return texture;
        }
        
        /// <summary>
        /// インポスターマテリアルを適用
        /// </summary>
        private void ApplyImpostorMaterial(GameObject quad, Texture2D texture)
        {
            var renderer = quad.GetComponent<MeshRenderer>();
            if (impostorMaterial != null)
            {
                var material = new Material(impostorMaterial);
                material.mainTexture = texture;
                renderer.material = material;
            }
            else
            {
                renderer.material.mainTexture = texture;
            }
        }
        
        /// <summary>
        /// インポスターを削除
        /// </summary>
        private void DestroyImpostor(PrimitiveTerrainObject primitiveObject)
        {
            if (!impostorCache.ContainsKey(primitiveObject))
                return;
            
            var impostorData = impostorCache[primitiveObject];
            
            if (impostorData.impostorQuad != null)
            {
                DestroyImmediate(impostorData.impostorQuad);
            }
            
            if (impostorData.impostorTexture != null)
            {
                DestroyImmediate(impostorData.impostorTexture);
            }
            
            impostorCache.Remove(primitiveObject);
            
            // 元のオブジェクトを再表示
            primitiveObject.gameObject.SetActive(true);
        }
        
        /// <summary>
        /// インポスターデータを削除
        /// </summary>
        private void DestroyImpostorData(PrimitiveTerrainObject primitiveObject)
        {
            DestroyImpostor(primitiveObject);
        }
        
        /// <summary>
        /// パフォーマンスに基づく品質調整
        /// </summary>
        private void AdjustQualityBasedOnPerformance()
        {
            float currentFrameRate = 1f / Time.deltaTime;
            float targetDelta = targetFrameRate - currentFrameRate;
            
            if (Mathf.Abs(targetDelta) > 5f) // 5FPS以上の差がある場合
            {
                float adjustment = targetDelta > 0 ? qualityAdjustmentSpeed : -qualityAdjustmentSpeed;
                currentQualityMultiplier = Mathf.Clamp(currentQualityMultiplier + adjustment * Time.deltaTime, 0.1f, 1f);
            }
        }
        
        /// <summary>
        /// メッシュをキャッシュ
        /// </summary>
        private void CacheMesh(string meshKey, Mesh mesh)
        {
            if (!enableMemoryOptimization) return;
            
            if (meshCache.Count >= maxCachedMeshes)
            {
                CleanupMeshCache();
            }
            
            meshCache[meshKey] = new CachedMeshData
            {
                mesh = mesh,
                lastAccessTime = Time.time,
                referenceCount = 1,
                meshKey = meshKey
            };
        }
        
        /// <summary>
        /// キャッシュされたメッシュを解放
        /// </summary>
        private void ReleaseCachedMesh(string meshKey)
        {
            if (meshCache.ContainsKey(meshKey))
            {
                var cachedData = meshCache[meshKey];
                cachedData.referenceCount--;
                
                if (cachedData.referenceCount <= 0)
                {
                    if (cachedData.mesh != null)
                    {
                        DestroyImmediate(cachedData.mesh);
                    }
                    meshCache.Remove(meshKey);
                }
            }
        }
        
        /// <summary>
        /// メッシュキャッシュをクリーンアップ
        /// </summary>
        private void CleanupMeshCache()
        {
            var expiredKeys = new List<string>();
            float currentTime = Time.time;
            
            foreach (var kvp in meshCache)
            {
                if (kvp.Value.referenceCount <= 0 && 
                    currentTime - kvp.Value.lastAccessTime > meshCacheTimeout)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }
            
            foreach (var key in expiredKeys)
            {
                ReleaseCachedMesh(key);
            }
        }
        
        /// <summary>
        /// 距離を計算
        /// </summary>
        private float CalculateDistance(PrimitiveTerrainObject primitiveObject)
        {
            if (playerTransform == null || primitiveObject == null)
                return float.MaxValue;
            
            return Vector3.Distance(playerTransform.position, primitiveObject.transform.position);
        }
        
        /// <summary>
        /// カメラフラスタム内にあるかチェック
        /// </summary>
        private bool IsInCameraFrustum(PrimitiveTerrainObject primitiveObject)
        {
            if (!enableFrustumCulling || playerCamera == null || primitiveObject == null)
                return true;
            
            var renderer = primitiveObject.GetComponent<MeshRenderer>();
            if (renderer == null) return false;
            
            var planes = GeometryUtility.CalculateFrustumPlanes(playerCamera);
            return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
        }
        
        /// <summary>
        /// オクルージョンされているかチェック
        /// </summary>
        private bool IsOccluded(PrimitiveTerrainObject primitiveObject)
        {
            if (!enableOcclusionCulling || playerCamera == null || primitiveObject == null)
                return false;
            
            Vector3 directionToObject = (primitiveObject.transform.position - playerCamera.transform.position).normalized;
            float distanceToObject = Vector3.Distance(playerCamera.transform.position, primitiveObject.transform.position);
            
            // レイキャストでオクルージョンチェック
            RaycastHit hit;
            if (Physics.Raycast(playerCamera.transform.position, directionToObject, out hit, distanceToObject))
            {
                return hit.collider.gameObject != primitiveObject.gameObject;
            }
            
            return false;
        }
        
        /// <summary>
        /// 最適なLODレベルを計算
        /// </summary>
        private int CalculateOptimalLOD(float distance)
        {
            for (int i = 0; i < lodDistances.Length; i++)
            {
                if (distance < lodDistances[i])
                {
                    return i;
                }
            }
            return lodDistances.Length - 1;
        }
        
        /// <summary>
        /// プリミティブにLODを適用
        /// </summary>
        private void ApplyLODToPrimitive(PrimitiveTerrainObject primitiveObject, int lodLevel)
        {
            if (!lodDataMap.ContainsKey(primitiveObject))
                return;
            
            var lodData = lodDataMap[primitiveObject];
            var meshFilter = primitiveObject.GetComponent<MeshFilter>();
            
            if (meshFilter != null && lodLevel < lodData.qualityMeshes.Length)
            {
                meshFilter.mesh = lodData.qualityMeshes[lodLevel];
            }
        }
        
        /// <summary>
        /// プリミティブのレンダリングを無効化
        /// </summary>
        private void DisablePrimitiveRendering(PrimitiveTerrainObject primitiveObject)
        {
            var renderer = primitiveObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }
        }
        
        /// <summary>
        /// LOD更新が必要かチェック
        /// </summary>
        private bool ShouldUpdateLOD(PrimitiveTerrainObject primitiveObject)
        {
            if (!lodDataMap.ContainsKey(primitiveObject))
                return false;
            
            var lodData = lodDataMap[primitiveObject];
            return Time.time - lodData.lastUpdateTime > updateInterval;
        }
        
        /// <summary>
        /// システム統計を取得
        /// </summary>
        public LODSystemStatistics GetSystemStatistics()
        {
            var stats = new LODSystemStatistics();
            
            foreach (var kvp in lodDataMap)
            {
                stats.totalObjects++;
                if (kvp.Value.isVisible) stats.visibleObjects++;
                if (kvp.Value.useImpostor) stats.impostorObjects++;
                if (kvp.Value.isInFrustum) stats.frustumObjects++;
            }
            
            stats.cachedMeshes = meshCache.Count;
            stats.qualityMultiplier = currentQualityMultiplier;
            
            return stats;
        }
        
        private void OnDestroy()
        {
            StopLODSystem();
            
            // キャッシュクリーンアップ
            foreach (var kvp in meshCache)
            {
                if (kvp.Value.mesh != null)
                {
                    DestroyImmediate(kvp.Value.mesh);
                }
            }
            meshCache.Clear();
            
            // インポスタークリーンアップ
            foreach (var kvp in impostorCache)
            {
                DestroyImpostorData(kvp.Key);
            }
            impostorCache.Clear();
        }
    }
    
    /// <summary>
    /// LODシステム統計情報
    /// </summary>
    [System.Serializable]
    public struct LODSystemStatistics
    {
        public int totalObjects;
        public int visibleObjects;
        public int impostorObjects;
        public int frustumObjects;
        public int cachedMeshes;
        public float qualityMultiplier;
    }
}