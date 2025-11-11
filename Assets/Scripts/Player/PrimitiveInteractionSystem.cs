using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Vastcore.Core;
using Vastcore.Utils;
using Vastcore.Terrain.Map;

namespace Vastcore.Player
{
    /// <summary>
    /// プリミティブ地形オブジェクトとのインタラクション管理システム
    /// グラインド、クライミング、ワープ着地ポイントの自動検出と設定を担当
    /// </summary>
    public class PrimitiveInteractionSystem : MonoBehaviour
    {
        [Header("グラインド設定")]
        [SerializeField] private LayerMask grindableLayer = -1;
        [SerializeField] private float minGrindEdgeLength = 5f;
        [SerializeField] private float grindEdgeThickness = 0.5f;
        [SerializeField] private bool showGrindGizmos = false;
        
        [Header("クライミング設定")]
        [SerializeField] private LayerMask climbableLayer = -1;
        [SerializeField] private float minClimbSurfaceArea = 10f;
        [SerializeField] private float maxClimbSurfaceAngle = 85f;
        [SerializeField] private bool showClimbGizmos = false;
        
        [Header("物理コライダー設定")]
        [SerializeField] private bool enableDetailedColliders = true;
        [SerializeField] private bool enableInteractionColliders = true;
        [SerializeField] private PhysicsMaterial grindPhysicsMaterial;
        [SerializeField] private PhysicsMaterial climbPhysicsMaterial;
        
        [Header("デバッグ")]
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private bool showInteractionInfo = false;
        
        // プライベート変数
        private Dictionary<GameObject, InteractionData> primitiveInteractions = new Dictionary<GameObject, InteractionData>();
        private List<GrindEdge> detectedGrindEdges = new List<GrindEdge>();
        private List<ClimbSurface> detectedClimbSurfaces = new List<ClimbSurface>();
        
        // 静的参照
        public static PrimitiveInteractionSystem Instance { get; private set; }
        
        #region データ構造
        [System.Serializable]
        public struct InteractionData
        {
            public List<GrindEdge> grindEdges;
            public List<ClimbSurface> climbSurfaces;
            public List<Collider> interactionColliders;
            public bool isProcessed;
        }
        
        [System.Serializable]
        public struct GrindEdge
        {
            public Vector3 startPoint;
            public Vector3 endPoint;
            public Vector3 direction;
            public Vector3 normal;
            public float length;
            public Transform parentTransform;
            public Collider edgeCollider;
            
            public Vector3 Center => (startPoint + endPoint) * 0.5f;
        }
        
        [System.Serializable]
        public struct ClimbSurface
        {
            public Vector3 center;
            public Vector3 normal;
            public Vector3 up;
            public float area;
            public Bounds bounds;
            public Transform parentTransform;
            public Collider surfaceCollider;
        }
        #endregion
        
        #region Unity生命周期
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            InitializeSystem();
        }
        
        void Update()
        {
            if (enableDebugLogs)
            {
                UpdateDebugInfo();
            }
        }
        #endregion
        
        #region 初期化
        /// <summary>
        /// システムを初期化
        /// </summary>
        private void InitializeSystem()
        {
            // 物理マテリアルを作成（存在しない場合）
            if (grindPhysicsMaterial == null)
            {
                grindPhysicsMaterial = CreateGrindPhysicsMaterial();
            }
            
            if (climbPhysicsMaterial == null)
            {
                climbPhysicsMaterial = CreateClimbPhysicsMaterial();
            }
            
            // 既存のプリミティブオブジェクトを検索して処理
            ProcessExistingPrimitives();
            
            Debug.Log("PrimitiveInteractionSystem initialized");
        }
        
        /// <summary>
        /// グラインド用物理マテリアルを作成
        /// </summary>
        private PhysicsMaterial CreateGrindPhysicsMaterial()
        {
            var material = new PhysicsMaterial("GrindMaterial");
            material.dynamicFriction = 0.1f;  // 低摩擦でスムーズなグラインド
            material.staticFriction = 0.05f;
            material.bounciness = 0.2f;
            material.frictionCombine = PhysicsMaterialCombine.Minimum;
            material.bounceCombine = PhysicsMaterialCombine.Average;
            return material;
        }
        
        /// <summary>
        /// クライミング用物理マテリアルを作成
        /// </summary>
        private PhysicsMaterial CreateClimbPhysicsMaterial()
        {
            var material = new PhysicsMaterial("ClimbMaterial");
            material.dynamicFriction = 0.8f;  // 高摩擦でクライミングしやすく
            material.staticFriction = 0.9f;
            material.bounciness = 0.0f;
            material.frictionCombine = PhysicsMaterialCombine.Maximum;
            material.bounceCombine = PhysicsMaterialCombine.Minimum;
            return material;
        }
        
        /// <summary>
        /// 既存のプリミティブオブジェクトを処理
        /// </summary>
        private void ProcessExistingPrimitives()
        {
            var existingPrimitives = FindObjectsByType<GameObject>(FindObjectsSortMode.None).Where(go => go.CompareTag("TerrainPrimitive"));
            foreach (var primitive in existingPrimitives)
            {
                ProcessPrimitiveInteraction(primitive);
            }
        }
        #endregion
        
        #region メインインタラクション処理
        /// <summary>
        /// プリミティブオブジェクトのインタラクションを処理
        /// </summary>
        public void ProcessPrimitiveInteraction(GameObject primitive)
        {
            if (primitive == null) return;
            
            // 既に処理済みかチェック
            if (primitiveInteractions.ContainsKey(primitive) && primitiveInteractions[primitive].isProcessed)
            {
                return;
            }
            
            var interactionData = new InteractionData
            {
                grindEdges = new List<GrindEdge>(),
                climbSurfaces = new List<ClimbSurface>(),
                interactionColliders = new List<Collider>(),
                isProcessed = false
            };
            
            // グラインド可能エッジを検出
            var primitiveObject = primitive.GetComponent<PrimitiveTerrainObject>();
            if (primitiveObject != null && primitiveObject.isGrindable)
            {
                DetectGrindableEdges(primitive, ref interactionData);
            }
            
            // クライミング可能表面を検出
            if (primitiveObject != null && primitiveObject.isClimbable)
            {
                DetectClimbableSurfaces(primitive, ref interactionData);
            }
            
            // 物理コライダーを設定
            if (enableDetailedColliders)
            {
                SetupDetailedColliders(primitive, ref interactionData);
            }
            
            interactionData.isProcessed = true;
            primitiveInteractions[primitive] = interactionData;
            
            if (enableDebugLogs)
            {
                Debug.Log($"Processed interactions for {primitive.name}: {interactionData.grindEdges.Count} grind edges, {interactionData.climbSurfaces.Count} climb surfaces");
            }
        }
        
        /// <summary>
        /// グラインド可能エッジを自動検出
        /// </summary>
        private void DetectGrindableEdges(GameObject primitive, ref InteractionData interactionData)
        {
            var meshFilter = primitive.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null) return;
            
            var mesh = meshFilter.sharedMesh;
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var transform = primitive.transform;
            
            // エッジ検出アルゴリズム
            var edges = new Dictionary<string, EdgeInfo>();
            
            // 三角形からエッジを抽出
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v1 = triangles[i];
                int v2 = triangles[i + 1];
                int v3 = triangles[i + 2];
                
                ProcessTriangleEdge(v1, v2, vertices, transform, edges);
                ProcessTriangleEdge(v2, v3, vertices, transform, edges);
                ProcessTriangleEdge(v3, v1, vertices, transform, edges);
            }
            
            // 境界エッジ（1つの三角形にのみ属するエッジ）を特定
            foreach (var edge in edges.Values)
            {
                if (edge.triangleCount == 1 && edge.length >= minGrindEdgeLength)
                {
                    var grindEdge = CreateGrindEdge(edge, primitive);
                    interactionData.grindEdges.Add(grindEdge);
                    detectedGrindEdges.Add(grindEdge);
                }
            }
        }
        
        /// <summary>
        /// 三角形のエッジを処理
        /// </summary>
        private void ProcessTriangleEdge(int v1, int v2, Vector3[] vertices, Transform transform, Dictionary<string, EdgeInfo> edges)
        {
            // エッジのキーを作成（頂点インデックスの小さい方を先に）
            string edgeKey = v1 < v2 ? $"{v1}-{v2}" : $"{v2}-{v1}";
            
            Vector3 worldPos1 = transform.TransformPoint(vertices[v1]);
            Vector3 worldPos2 = transform.TransformPoint(vertices[v2]);
            float length = Vector3.Distance(worldPos1, worldPos2);
            
            if (edges.ContainsKey(edgeKey))
            {
                var edgeInfo = edges[edgeKey];
                edgeInfo.triangleCount++;
                edges[edgeKey] = edgeInfo;
            }
            else
            {
                edges[edgeKey] = new EdgeInfo
                {
                    vertex1 = worldPos1,
                    vertex2 = worldPos2,
                    length = length,
                    triangleCount = 1
                };
            }
        }
        
        /// <summary>
        /// グラインドエッジを作成
        /// </summary>
        private GrindEdge CreateGrindEdge(EdgeInfo edgeInfo, GameObject primitive)
        {
            Vector3 direction = (edgeInfo.vertex2 - edgeInfo.vertex1).normalized;
            Vector3 center = (edgeInfo.vertex1 + edgeInfo.vertex2) * 0.5f;
            
            // エッジの法線を計算（上向きを基準）
            Vector3 normal = Vector3.Cross(direction, Vector3.up).normalized;
            if (normal == Vector3.zero)
            {
                normal = Vector3.Cross(direction, Vector3.forward).normalized;
            }
            
            var grindEdge = new GrindEdge
            {
                startPoint = edgeInfo.vertex1,
                endPoint = edgeInfo.vertex2,
                direction = direction,
                normal = normal,
                length = edgeInfo.length,
                parentTransform = primitive.transform,
                edgeCollider = null
            };
            
            // グラインド用コライダーを作成
            if (enableInteractionColliders)
            {
                grindEdge.edgeCollider = CreateGrindEdgeCollider(grindEdge, primitive.gameObject);
            }
            
            return grindEdge;
        }
        
        /// <summary>
        /// クライミング可能表面を検出
        /// </summary>
        private void DetectClimbableSurfaces(GameObject primitive, ref InteractionData interactionData)
        {
            var meshFilter = primitive.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null) return;
            
            var mesh = meshFilter.sharedMesh;
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;
            var normals = mesh.normals;
            var transform = primitive.transform;
            
            // 表面を三角形グループごとに分析
            var surfaces = new List<SurfaceGroup>();
            
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v1 = triangles[i];
                int v2 = triangles[i + 1];
                int v3 = triangles[i + 2];
                
                Vector3 worldPos1 = transform.TransformPoint(vertices[v1]);
                Vector3 worldPos2 = transform.TransformPoint(vertices[v2]);
                Vector3 worldPos3 = transform.TransformPoint(vertices[v3]);
                
                Vector3 worldNormal = transform.TransformDirection(normals[v1]).normalized;
                
                // クライミング可能な角度かチェック
                float angle = Vector3.Angle(Vector3.up, worldNormal);
                if (angle >= 45f && angle <= maxClimbSurfaceAngle)
                {
                    float area = CalculateTriangleArea(worldPos1, worldPos2, worldPos3);
                    
                    var surfaceGroup = new SurfaceGroup
                    {
                        center = (worldPos1 + worldPos2 + worldPos3) / 3f,
                        normal = worldNormal,
                        area = area,
                        vertices = new List<Vector3> { worldPos1, worldPos2, worldPos3 }
                    };
                    
                    surfaces.Add(surfaceGroup);
                }
            }
            
            // 近接する表面をグループ化
            var groupedSurfaces = GroupNearSurfaces(surfaces);
            
            // 十分な面積を持つ表面のみをクライミング表面として登録
            foreach (var surface in groupedSurfaces)
            {
                if (surface.area >= minClimbSurfaceArea)
                {
                    var climbSurface = CreateClimbSurface(surface, primitive);
                    interactionData.climbSurfaces.Add(climbSurface);
                    detectedClimbSurfaces.Add(climbSurface);
                }
            }
        }
        
        /// <summary>
        /// クライミング表面を作成
        /// </summary>
        private ClimbSurface CreateClimbSurface(SurfaceGroup surfaceGroup, GameObject primitive)
        {
            var bounds = CalculateBounds(surfaceGroup.vertices);
            Vector3 up = Vector3.Cross(surfaceGroup.normal, Vector3.right).normalized;
            if (up == Vector3.zero)
            {
                up = Vector3.Cross(surfaceGroup.normal, Vector3.forward).normalized;
            }
            
            var climbSurface = new ClimbSurface
            {
                center = surfaceGroup.center,
                normal = surfaceGroup.normal,
                up = up,
                area = surfaceGroup.area,
                bounds = bounds,
                parentTransform = primitive.transform,
                surfaceCollider = null
            };
            
            // クライミング用コライダーを作成
            if (enableInteractionColliders)
            {
                climbSurface.surfaceCollider = CreateClimbSurfaceCollider(climbSurface, primitive.gameObject);
            }
            
            return climbSurface;
        }
        #endregion
        
        #region コライダー生成
        /// <summary>
        /// グラインドエッジ用コライダーを作成
        /// </summary>
        private Collider CreateGrindEdgeCollider(GrindEdge edge, GameObject parent)
        {
            var edgeObject = new GameObject($"GrindEdge_{edge.length:F1}m");
            edgeObject.transform.SetParent(parent.transform);
            edgeObject.transform.position = edge.Center;
            edgeObject.transform.LookAt(edge.endPoint, edge.normal);
            
            // カプセルコライダーでエッジを表現
            var capsuleCollider = edgeObject.AddComponent<CapsuleCollider>();
            capsuleCollider.direction = 2; // Z軸方向
            capsuleCollider.height = edge.length;
            capsuleCollider.radius = grindEdgeThickness;
            capsuleCollider.isTrigger = false;
            capsuleCollider.material = grindPhysicsMaterial;
            
            // グラインド識別用タグを設定
            edgeObject.tag = "GrindableEdge";
            edgeObject.layer = LayerMask.NameToLayer("Grindable");
            
            return capsuleCollider;
        }
        
        /// <summary>
        /// クライミング表面用コライダーを作成
        /// </summary>
        private Collider CreateClimbSurfaceCollider(ClimbSurface surface, GameObject parent)
        {
            var surfaceObject = new GameObject($"ClimbSurface_{surface.area:F1}m2");
            surfaceObject.transform.SetParent(parent.transform);
            surfaceObject.transform.position = surface.center;
            surfaceObject.transform.rotation = Quaternion.LookRotation(-surface.normal, surface.up);
            
            // ボックスコライダーで表面を表現
            var boxCollider = surfaceObject.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(surface.bounds.size.x, surface.bounds.size.y, 0.1f);
            boxCollider.isTrigger = false;
            boxCollider.material = climbPhysicsMaterial;
            
            // クライミング識別用タグを設定
            surfaceObject.tag = "ClimbableSurface";
            surfaceObject.layer = LayerMask.NameToLayer("Climbable");
            
            return boxCollider;
        }
        
        /// <summary>
        /// 詳細コライダーを設定
        /// </summary>
        private void SetupDetailedColliders(GameObject primitive, ref InteractionData interactionData)
        {
            var meshCollider = primitive.GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                // 既存のメッシュコライダーを高精度に設定
                meshCollider.convex = false;
                meshCollider.cookingOptions = MeshColliderCookingOptions.EnableMeshCleaning | 
                                             MeshColliderCookingOptions.WeldColocatedVertices;
                
                interactionData.interactionColliders.Add(meshCollider);
            }
        }
        #endregion
        
        #region ヘルパーメソッド
        /// <summary>
        /// 三角形の面積を計算
        /// </summary>
        private float CalculateTriangleArea(Vector3 a, Vector3 b, Vector3 c)
        {
            return Vector3.Cross(b - a, c - a).magnitude * 0.5f;
        }
        
        /// <summary>
        /// 頂点リストから境界ボックスを計算
        /// </summary>
        private Bounds CalculateBounds(List<Vector3> vertices)
        {
            if (vertices.Count == 0) return new Bounds();
            
            Vector3 min = vertices[0];
            Vector3 max = vertices[0];
            
            foreach (var vertex in vertices)
            {
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
            }
            
            return new Bounds((min + max) * 0.5f, max - min);
        }
        
        /// <summary>
        /// 近接する表面をグループ化
        /// </summary>
        private List<SurfaceGroup> GroupNearSurfaces(List<SurfaceGroup> surfaces)
        {
            var grouped = new List<SurfaceGroup>();
            var processed = new bool[surfaces.Count];
            
            for (int i = 0; i < surfaces.Count; i++)
            {
                if (processed[i]) continue;
                
                var group = surfaces[i];
                processed[i] = true;
                
                // 近接する表面を探してマージ
                for (int j = i + 1; j < surfaces.Count; j++)
                {
                    if (processed[j]) continue;
                    
                    float distance = Vector3.Distance(group.center, surfaces[j].center);
                    float normalDot = Vector3.Dot(group.normal, surfaces[j].normal);
                    
                    // 距離が近く、法線が似ている場合はマージ
                    if (distance < 5f && normalDot > 0.8f)
                    {
                        group = MergeSurfaceGroups(group, surfaces[j]);
                        processed[j] = true;
                    }
                }
                
                grouped.Add(group);
            }
            
            return grouped;
        }
        
        /// <summary>
        /// 表面グループをマージ
        /// </summary>
        private SurfaceGroup MergeSurfaceGroups(SurfaceGroup group1, SurfaceGroup group2)
        {
            var merged = new SurfaceGroup
            {
                center = (group1.center + group2.center) * 0.5f,
                normal = (group1.normal + group2.normal).normalized,
                area = group1.area + group2.area,
                vertices = new List<Vector3>(group1.vertices)
            };
            
            merged.vertices.AddRange(group2.vertices);
            
            return merged;
        }
        #endregion
        
        #region パブリックAPI
        /// <summary>
        /// 指定位置の近くのグラインドエッジを取得
        /// </summary>
        public List<GrindEdge> GetNearbyGrindEdges(Vector3 position, float radius)
        {
            var nearbyEdges = new List<GrindEdge>();
            
            foreach (var edge in detectedGrindEdges)
            {
                float distance = Vector3.Distance(position, edge.Center);
                if (distance <= radius)
                {
                    nearbyEdges.Add(edge);
                }
            }
            
            return nearbyEdges;
        }
        
        /// <summary>
        /// 指定位置の近くのクライミング表面を取得
        /// </summary>
        public List<ClimbSurface> GetNearbyClimbSurfaces(Vector3 position, float radius)
        {
            var nearbySurfaces = new List<ClimbSurface>();
            
            foreach (var surface in detectedClimbSurfaces)
            {
                float distance = Vector3.Distance(position, surface.center);
                if (distance <= radius)
                {
                    nearbySurfaces.Add(surface);
                }
            }
            
            return nearbySurfaces;
        }
        
        /// <summary>
        /// プリミティブのインタラクションデータを取得
        /// </summary>
        public InteractionData? GetInteractionData(GameObject primitive)
        {
            if (primitiveInteractions.ContainsKey(primitive))
            {
                return primitiveInteractions[primitive];
            }
            return null;
        }
        
        /// <summary>
        /// プリミティブのインタラクションを削除
        /// </summary>
        public void RemovePrimitiveInteraction(GameObject primitive)
        {
            if (primitiveInteractions.ContainsKey(primitive))
            {
                var data = primitiveInteractions[primitive];
                
                // 作成したコライダーを削除
                foreach (var collider in data.interactionColliders)
                {
                    if (collider != null && collider.gameObject != primitive.gameObject)
                    {
                        DestroyImmediate(collider.gameObject);
                    }
                }
                
                // グラインドエッジのコライダーを削除
                foreach (var edge in data.grindEdges)
                {
                    if (edge.edgeCollider != null)
                    {
                        DestroyImmediate(edge.edgeCollider.gameObject);
                    }
                }
                
                // クライミング表面のコライダーを削除
                foreach (var surface in data.climbSurfaces)
                {
                    if (surface.surfaceCollider != null)
                    {
                        DestroyImmediate(surface.surfaceCollider.gameObject);
                    }
                }
                
                primitiveInteractions.Remove(primitive);
            }
        }
        #endregion
        
        #region デバッグ・可視化
        /// <summary>
        /// デバッグ情報を更新
        /// </summary>
        private void UpdateDebugInfo()
        {
            if (showInteractionInfo)
            {
                Debug.Log($"Detected interactions - Grind edges: {detectedGrindEdges.Count}, Climb surfaces: {detectedClimbSurfaces.Count}");
            }
        }
        
        void OnDrawGizmos()
        {
            if (showGrindGizmos)
            {
                DrawGrindEdgeGizmos();
            }
            
            if (showClimbGizmos)
            {
                DrawClimbSurfaceGizmos();
            }
        }
        
        /// <summary>
        /// グラインドエッジのギズモを描画
        /// </summary>
        private void DrawGrindEdgeGizmos()
        {
            Gizmos.color = Color.cyan;
            foreach (var edge in detectedGrindEdges)
            {
                Gizmos.DrawLine(edge.startPoint, edge.endPoint);
                Gizmos.DrawWireSphere(edge.Center, grindEdgeThickness);
                
                // 方向を示す矢印
                Gizmos.color = Color.yellow;
                Vector3 arrowEnd = edge.Center + edge.direction * 2f;
                Gizmos.DrawLine(edge.Center, arrowEnd);
                Gizmos.color = Color.cyan;
            }
        }
        
        /// <summary>
        /// クライミング表面のギズモを描画
        /// </summary>
        private void DrawClimbSurfaceGizmos()
        {
            Gizmos.color = Color.green;
            foreach (var surface in detectedClimbSurfaces)
            {
                Gizmos.DrawWireCube(surface.center, surface.bounds.size);
                
                // 法線を示す矢印
                Gizmos.color = Color.red;
                Vector3 normalEnd = surface.center + surface.normal * 3f;
                Gizmos.DrawLine(surface.center, normalEnd);
                Gizmos.color = Color.green;
            }
        }
        #endregion
        
        #region 内部データ構造
        private struct EdgeInfo
        {
            public Vector3 vertex1;
            public Vector3 vertex2;
            public float length;
            public int triangleCount;
        }
        
        private struct SurfaceGroup
        {
            public Vector3 center;
            public Vector3 normal;
            public float area;
            public List<Vector3> vertices;
        }
        #endregion
    }
}