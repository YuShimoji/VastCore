using UnityEngine;
using System.Collections.Generic;
using Vastcore.Generation;

namespace Vastcore.Player
{
    /// <summary>
    /// 強化されたワープ移動システム
    /// プリミティブ地形オブジェクトとの統合ワープ機能を提供
    /// </summary>
    public class EnhancedTranslocationSystem : MonoBehaviour
    {
        [Header("ワープ基本設定")]
        [SerializeField] private GameObject translocationSpherePrefab;
        [SerializeField] private float sphereLaunchForce = 50f;
        [SerializeField] private float sphereLifetime = 8f;
        [SerializeField] private float warpCooldown = 2f;
        [SerializeField] private float maxWarpDistance = 100f;
        
        [Header("軌道予測改良")]
        [SerializeField] private LineRenderer trajectoryLine;
        [SerializeField] private int trajectoryPoints = 50;
        [SerializeField] private float trajectoryTimeStep = 0.1f;
        [SerializeField] private Material trajectoryMaterial;
        
        [Header("着地プレビュー")]
        [SerializeField] private GameObject landingPreview;
        [SerializeField] private float previewUpdateRate = 0.1f;
        [SerializeField] private LayerMask landingSurfaceLayer = -1;
        
        [Header("プリミティブ統合")]
        [SerializeField] private float primitiveDetectionRadius = 5f;
        [SerializeField] private LayerMask primitiveLayer = -1;
        [SerializeField] private GameObject landingPointIndicator;
        
        [Header("安全性検証")]
        [SerializeField] private float safetyCheckRadius = 2f;
        [SerializeField] private float minLandingSpace = 3f;
        [SerializeField] private LayerMask obstacleLayer = -1;
        
        // プライベート変数
        private Camera playerCamera;
        private Transform playerTransform;
        private Rigidbody playerRigidbody;
        private GameObject currentSphere;
        private float lastWarpTime;
        private Vector3[] trajectoryPositions;
        private List<GameObject> activeLandingPoints = new List<GameObject>();
        
        // イベント
        public System.Action<Vector3> OnWarpInitiated;
        public System.Action<Vector3> OnWarpCompleted;
        public System.Action<Vector3> OnLandingPointDetected;
        
        private void Awake()
        {
            InitializeComponents();
            SetupTrajectoryLine();
            trajectoryPositions = new Vector3[trajectoryPoints];
        }
        
        private void InitializeComponents()
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                playerCamera = FindObjectOfType<Camera>();
                
            playerTransform = transform;
            playerRigidbody = GetComponent<Rigidbody>();
            
            if (playerRigidbody == null)
                playerRigidbody = GetComponentInParent<Rigidbody>();
        }
        
        private void SetupTrajectoryLine()
        {
            if (trajectoryLine == null)
            {
                GameObject trajectoryObject = new GameObject("TrajectoryLine");
                trajectoryObject.transform.SetParent(transform);
                trajectoryLine = trajectoryObject.AddComponent<LineRenderer>();
            }
            
            trajectoryLine.positionCount = trajectoryPoints;
            trajectoryLine.startWidth = 0.1f;
            trajectoryLine.endWidth = 0.05f;
            trajectoryLine.useWorldSpace = true;
            trajectoryLine.enabled = false;
            
            if (trajectoryMaterial != null)
                trajectoryLine.material = trajectoryMaterial;
        }
        
        private void Update()
        {
            HandleInput();
            UpdateTrajectoryPrediction();
            UpdateLandingPreview();
            CleanupLandingPoints();
        }
        
        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(1) && CanWarp()) // 右クリック
            {
                LaunchTranslocationSphere();
            }
            
            if (Input.GetMouseButton(1) && currentSphere != null)
            {
                ShowTrajectoryPrediction(true);
            }
            else
            {
                ShowTrajectoryPrediction(false);
            }
        }
        
        private bool CanWarp()
        {
            return Time.time - lastWarpTime >= warpCooldown && currentSphere == null;
        }
        
        private void LaunchTranslocationSphere()
        {
            if (translocationSpherePrefab == null) return;
            
            Vector3 launchPosition = playerCamera.transform.position + playerCamera.transform.forward * 2f;
            Vector3 launchDirection = playerCamera.transform.forward;
            
            currentSphere = Instantiate(translocationSpherePrefab, launchPosition, Quaternion.identity);
            
            // 球体に物理を適用
            Rigidbody sphereRb = currentSphere.GetComponent<Rigidbody>();
            if (sphereRb == null)
                sphereRb = currentSphere.AddComponent<Rigidbody>();
                
            sphereRb.AddForce(launchDirection * sphereLaunchForce, ForceMode.Impulse);
            
            // 球体のコンポーネント設定
            SetupTranslocationSphere(currentSphere);
            
            // 自動削除タイマー
            Destroy(currentSphere, sphereLifetime);
            
            OnWarpInitiated?.Invoke(launchPosition);
        }
        
        private void SetupTranslocationSphere(GameObject sphere)
        {
            // コライダー設定
            SphereCollider collider = sphere.GetComponent<SphereCollider>();
            if (collider == null)
                collider = sphere.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            
            // ワープトリガーコンポーネント追加
            TranslocationTrigger trigger = sphere.GetComponent<TranslocationTrigger>();
            if (trigger == null)
                trigger = sphere.AddComponent<TranslocationTrigger>();
            trigger.Initialize(this);
        }
        
        public void ExecuteWarp(Vector3 targetPosition)
        {
            if (!IsValidLandingPosition(targetPosition))
            {
                Debug.LogWarning("Invalid landing position detected, warp cancelled");
                return;
            }
            
            // 安全な着地位置を調整
            Vector3 safeLandingPosition = GetSafeLandingPosition(targetPosition);
            
            // プレイヤーをワープ
            playerTransform.position = safeLandingPosition;
            
            // 物理的な速度をリセット
            if (playerRigidbody != null)
            {
                playerRigidbody.velocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }
            
            // 球体を削除
            if (currentSphere != null)
            {
                Destroy(currentSphere);
                currentSphere = null;
            }
            
            lastWarpTime = Time.time;
            OnWarpCompleted?.Invoke(safeLandingPosition);
            
            // 着地エフェクト
            CreateLandingEffect(safeLandingPosition);
        }
        
        private bool IsValidLandingPosition(Vector3 position)
        {
            // 障害物チェック
            Collider[] obstacles = Physics.OverlapSphere(position, safetyCheckRadius, obstacleLayer);
            if (obstacles.Length > 0)
                return false;
            
            // 着地面チェック
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out hit, 10f, landingSurfaceLayer))
            {
                float distanceToGround = Vector3.Distance(position, hit.point);
                return distanceToGround <= minLandingSpace;
            }
            
            return false;
        }
        
        private Vector3 GetSafeLandingPosition(Vector3 targetPosition)
        {
            // 地面への投影
            RaycastHit hit;
            if (Physics.Raycast(targetPosition + Vector3.up * 10f, Vector3.down, out hit, 20f, landingSurfaceLayer))
            {
                return hit.point + Vector3.up * 1f; // 地面から1m上
            }
            
            return targetPosition;
        }
        
        private void UpdateTrajectoryPrediction()
        {
            if (currentSphere == null || !trajectoryLine.enabled) return;
            
            Vector3[] trajectory = CalculateTrajectory();
            trajectoryLine.SetPositions(trajectory);
            
            // 着地点の予測
            Vector3 predictedLandingPoint = GetPredictedLandingPoint(trajectory);
            UpdateLandingPointIndicators(predictedLandingPoint);
        }
        
        private Vector3[] CalculateTrajectory()
        {
            if (currentSphere == null) return trajectoryPositions;
            
            Vector3 startPosition = currentSphere.transform.position;
            Vector3 startVelocity = currentSphere.GetComponent<Rigidbody>().velocity;
            
            for (int i = 0; i < trajectoryPoints; i++)
            {
                float time = i * trajectoryTimeStep;
                Vector3 position = startPosition + startVelocity * time;
                position += 0.5f * Physics.gravity * time * time;
                
                trajectoryPositions[i] = position;
                
                // 地面との衝突チェック
                RaycastHit hit;
                if (i > 0 && Physics.Raycast(trajectoryPositions[i-1], 
                    (position - trajectoryPositions[i-1]).normalized, 
                    out hit, 
                    Vector3.Distance(trajectoryPositions[i-1], position),
                    landingSurfaceLayer))
                {
                    trajectoryPositions[i] = hit.point;
                    // 残りの点を着地点に設定
                    for (int j = i + 1; j < trajectoryPoints; j++)
                    {
                        trajectoryPositions[j] = hit.point;
                    }
                    break;
                }
            }
            
            return trajectoryPositions;
        }
        
        private Vector3 GetPredictedLandingPoint(Vector3[] trajectory)
        {
            for (int i = 1; i < trajectory.Length; i++)
            {
                RaycastHit hit;
                if (Physics.Raycast(trajectory[i-1], (trajectory[i] - trajectory[i-1]).normalized, 
                    out hit, Vector3.Distance(trajectory[i-1], trajectory[i]), landingSurfaceLayer))
                {
                    return hit.point;
                }
            }
            
            return trajectory[trajectory.Length - 1];
        }
        
        private void UpdateLandingPointIndicators(Vector3 landingPoint)
        {
            // プリミティブ地形オブジェクトの検出
            DetectPrimitiveLandingPoints(landingPoint);
            
            // メインの着地プレビュー更新
            if (landingPreview != null)
            {
                landingPreview.SetActive(true);
                landingPreview.transform.position = landingPoint;
                
                // 着地の安全性に応じて色を変更
                Renderer previewRenderer = landingPreview.GetComponent<Renderer>();
                if (previewRenderer != null)
                {
                    bool isSafe = IsValidLandingPosition(landingPoint);
                    previewRenderer.material.color = isSafe ? Color.green : Color.red;
                }
            }
        }
        
        private void DetectPrimitiveLandingPoints(Vector3 centerPoint)
        {
            // 既存の着地ポイントをクリア
            ClearLandingPoints();
            
            // プリミティブオブジェクトを検索
            Collider[] primitives = Physics.OverlapSphere(centerPoint, primitiveDetectionRadius, primitiveLayer);
            
            foreach (Collider primitive in primitives)
            {
                PrimitiveTerrainObject primitiveObject = primitive.GetComponent<PrimitiveTerrainObject>();
                if (primitiveObject != null && primitiveObject.isClimbable)
                {
                    Vector3[] landingPoints = GenerateLandingPoints(primitive);
                    CreateLandingPointIndicators(landingPoints);
                }
            }
        }
        
        private Vector3[] GenerateLandingPoints(Collider primitive)
        {
            List<Vector3> points = new List<Vector3>();
            Bounds bounds = primitive.bounds;
            
            // 上面の着地ポイント
            Vector3 topCenter = new Vector3(bounds.center.x, bounds.max.y + 1f, bounds.center.z);
            if (IsValidLandingPosition(topCenter))
            {
                points.Add(topCenter);
            }
            
            // 側面の着地ポイント（プラットフォーム状の突起がある場合）
            Vector3[] sidePoints = {
                new Vector3(bounds.max.x + 1f, bounds.center.y, bounds.center.z),
                new Vector3(bounds.min.x - 1f, bounds.center.y, bounds.center.z),
                new Vector3(bounds.center.x, bounds.center.y, bounds.max.z + 1f),
                new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - 1f)
            };
            
            foreach (Vector3 point in sidePoints)
            {
                if (IsValidLandingPosition(point))
                {
                    points.Add(point);
                }
            }
            
            return points.ToArray();
        }
        
        private void CreateLandingPointIndicators(Vector3[] points)
        {
            foreach (Vector3 point in points)
            {
                if (landingPointIndicator != null)
                {
                    GameObject indicator = Instantiate(landingPointIndicator, point, Quaternion.identity);
                    activeLandingPoints.Add(indicator);
                    
                    OnLandingPointDetected?.Invoke(point);
                }
            }
        }
        
        private void UpdateLandingPreview()
        {
            if (currentSphere == null && landingPreview != null)
            {
                landingPreview.SetActive(false);
            }
        }
        
        private void ShowTrajectoryPrediction(bool show)
        {
            if (trajectoryLine != null)
            {
                trajectoryLine.enabled = show;
            }
        }
        
        private void CreateLandingEffect(Vector3 position)
        {
            // パーティクルエフェクトやサウンドエフェクトを追加
            // 実装は後で追加可能
        }
        
        private void ClearLandingPoints()
        {
            foreach (GameObject point in activeLandingPoints)
            {
                if (point != null)
                    Destroy(point);
            }
            activeLandingPoints.Clear();
        }
        
        private void CleanupLandingPoints()
        {
            if (currentSphere == null)
            {
                ClearLandingPoints();
            }
        }
        
        private void OnDestroy()
        {
            ClearLandingPoints();
        }
    }
    
    /// <summary>
    /// ワープ球体のトリガーコンポーネント
    /// </summary>
    public class TranslocationTrigger : MonoBehaviour
    {
        private EnhancedTranslocationSystem translocationSystem;
        
        public void Initialize(EnhancedTranslocationSystem system)
        {
            translocationSystem = system;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            // 地面や壁に当たったらワープ実行
            if (IsValidSurface(other))
            {
                translocationSystem.ExecuteWarp(transform.position);
            }
        }
        
        private bool IsValidSurface(Collider other)
        {
            // プレイヤー自身は除外
            if (other.transform.root == translocationSystem.transform.root)
                return false;
                
            // 地形やプリミティブオブジェクトかチェック
            return other.gameObject.layer == LayerMask.NameToLayer("Terrain") ||
                   other.gameObject.layer == LayerMask.NameToLayer("Primitive") ||
                   other.GetComponent<PrimitiveTerrainObject>() != null;
        }
    }
}