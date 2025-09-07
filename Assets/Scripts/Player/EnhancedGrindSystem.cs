using UnityEngine;
using System.Collections.Generic;
using Vastcore.Generation; // Correct namespace for PrimitiveTerrainObject

namespace Vastcore.Player
{
    /// <summary>
    /// 強化されたグラインドシステム
    /// プリミティブ地形オブジェクトとの統合グラインド機能を提供
    /// </summary>
    public class EnhancedGrindSystem : MonoBehaviour
    {
        [Header("グラインド基本設定")]
        [SerializeField] private float grindSpeed = 20f;
        [SerializeField] private float grindAcceleration = 2f;
        [SerializeField] private float maxGrindSpeed = 30f;
        [SerializeField] private float grindDetectionRadius = 2f;
        [SerializeField] private float grindExitForce = 10f;
        
        [Header("グラインド物理")]
        [SerializeField] private AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float gravityReduction = 0.8f;
        [SerializeField] private float momentumTransfer = 0.9f;
        [SerializeField] private float edgeSnapDistance = 1f;
        
        [Header("表面検出")]
        [SerializeField] private LayerMask grindableLayer = -1;
        
        [Header("エフェクト")]
        [SerializeField] private ParticleSystem grindEffect;
        [SerializeField] private AudioSource grindAudioSource;
        [SerializeField] private AudioClip grindStartSound;
        [SerializeField] private AudioClip grindLoopSound;
        [SerializeField] private AudioClip grindEndSound;
        
        [Header("デバッグ")]
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private bool showGrindGizmos = false;
        [SerializeField] private bool showGrindInfo = false;
        
        // プライベート変数
        private AdvancedPlayerController playerController;
        private CharacterController characterController;
        private PrimitiveInteractionSystem interactionSystem;
        
        // グラインド状態
        private bool isGrinding = false;
        private PrimitiveInteractionSystem.GrindEdge? currentGrindEdge;
        private Transform currentGrindSurface;
        private Vector3 grindDirection;
        private Vector3 grindStartPosition;
        private float currentGrindSpeed;
        private float grindStartTime;
        private Vector3 grindMomentum;
        
        // 検出されたグラインド可能オブジェクト
        private List<PrimitiveInteractionSystem.GrindEdge> nearbyGrindEdges = new List<PrimitiveInteractionSystem.GrindEdge>();
        private PrimitiveInteractionSystem.GrindEdge? bestGrindEdge;
        
        // パフォーマンス最適化
        private float lastDetectionTime;
        private const float detectionInterval = 0.1f;
        
        #region Unity生命周期
        void Start()
        {
            InitializeSystem();
        }
        
        void Update()
        {
            UpdateGrindSystem();
        }
        
        void FixedUpdate()
        {
            if (isGrinding)
            {
                ApplyGrindPhysics();
            }
        }
        #endregion
        
        #region 初期化
        /// <summary>
        /// システムを初期化
        /// </summary>
        private void InitializeSystem()
        {
            // 必要なコンポーネントを取得
            playerController = GetComponent<AdvancedPlayerController>();
            characterController = GetComponent<CharacterController>();
            interactionSystem = PrimitiveInteractionSystem.Instance;
            
            if (playerController == null)
            {
                Debug.LogError("AdvancedPlayerController not found on " + gameObject.name);
                enabled = false;
                return;
            }
            
            if (characterController == null)
            {
                Debug.LogError("CharacterController not found on " + gameObject.name);
                enabled = false;
                return;
            }
            
            // オーディオソースを設定
            if (grindAudioSource == null)
            {
                grindAudioSource = gameObject.AddComponent<AudioSource>();
                grindAudioSource.spatialBlend = 1f; // 3D音響
                grindAudioSource.volume = 0.7f;
            }
            
            // パーティクルエフェクトを設定
            if (grindEffect == null)
            {
                grindEffect = CreateGrindParticleEffect();
            }
            
            Debug.Log("EnhancedGrindSystem initialized");
        }
        
        /// <summary>
        /// グラインドパーティクルエフェクトを作成
        /// </summary>
        private ParticleSystem CreateGrindParticleEffect()
        {
            var effectObject = new GameObject("GrindEffect");
            effectObject.transform.SetParent(transform);
            effectObject.transform.localPosition = Vector3.zero;
            
            var particles = effectObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 5f;
            main.startSize = 0.1f;
            main.startColor = Color.yellow;
            main.maxParticles = 50;
            
            var emission = particles.emission;
            emission.rateOverTime = 30f;
            
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.5f, 0.1f, 0.5f);
            
            var velocityOverLifetime = particles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(2f);
            
            particles.Stop();
            return particles;
        }
        #endregion
        
        #region メインシステム更新
        /// <summary>
        /// グラインドシステムを更新
        /// </summary>
        public void UpdateGrindSystem()
        {
            // グラインド可能表面を定期的に検出
            if (Time.time - lastDetectionTime > detectionInterval)
            {
                DetectGrindableSurfaces();
                lastDetectionTime = Time.time;
            }
            
            if (!isGrinding)
            {
                // グラインド開始の判定
                if (Input.GetKeyDown(KeyCode.E) && CanStartGrind())
                {
                    StartGrind();
                }
            }
            else
            {
                // グラインド中の処理
                UpdateGrindState();
                
                // グラインド終了の判定
                if (Input.GetKeyDown(KeyCode.E) || !IsValidGrindState())
                {
                    ExitGrind();
                }
            }
            
            UpdateGrindEffects();
        }
        
        /// <summary>
        /// グラインド可能表面を検出
        /// </summary>
        private void DetectGrindableSurfaces()
        {
            nearbyGrindEdges.Clear();
            bestGrindEdge = null;
            
            if (interactionSystem == null) return;
            
            // プリミティブインタラクションシステムから近くのグラインドエッジを取得
            var edges = interactionSystem.GetNearbyGrindEdges(transform.position, grindDetectionRadius);
            nearbyGrindEdges.AddRange(edges);
            
            // 従来のコライダーベース検出も併用
            DetectTraditionalGrindSurfaces();
            
            // 最適なグラインドエッジを選択
            SelectBestGrindEdge();
        }
        
        /// <summary>
        /// 従来のコライダーベースグラインド表面検出
        /// </summary>
        private void DetectTraditionalGrindSurfaces()
        {
            Collider[] grindableObjects = Physics.OverlapSphere(transform.position, grindDetectionRadius, grindableLayer);
            
            foreach (Collider col in grindableObjects)
            {
                // プリミティブオブジェクトでない場合の処理
                if (col.GetComponent<PrimitiveTerrainObject>() == null)
                {
                    // 従来のグラインド表面として処理
                    ProcessTraditionalGrindSurface(col);
                }
            }
        }
        
        /// <summary>
        /// 従来のグラインド表面を処理
        /// </summary>
        private void ProcessTraditionalGrindSurface(Collider surface)
        {
            // 表面の最も近い点を取得
            Vector3 closestPoint = surface.ClosestPoint(transform.position);
            Vector3 surfaceDirection = (closestPoint - transform.position).normalized;
            
            // 人工的なグラインドエッジを作成
            var artificialEdge = new PrimitiveInteractionSystem.GrindEdge
            {
                startPoint = closestPoint - surfaceDirection * 5f,
                endPoint = closestPoint + surfaceDirection * 5f,
                direction = surfaceDirection,
                normal = Vector3.up,
                length = 10f,
                parentTransform = surface.transform,
                edgeCollider = surface
            };
            
            nearbyGrindEdges.Add(artificialEdge);
        }
        
        /// <summary>
        /// 最適なグラインドエッジを選択
        /// </summary>
        private void SelectBestGrindEdge()
        {
            if (nearbyGrindEdges.Count == 0) return;
            
            float bestScore = float.MinValue;
            PrimitiveInteractionSystem.GrindEdge? best = null;
            
            foreach (var edge in nearbyGrindEdges)
            {
                float score = CalculateGrindEdgeScore(edge);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = edge;
                }
            }
            
            bestGrindEdge = best;
        }
        
        /// <summary>
        /// グラインドエッジのスコアを計算
        /// </summary>
        private float CalculateGrindEdgeScore(PrimitiveInteractionSystem.GrindEdge edge)
        {
            float distance = Vector3.Distance(transform.position, edge.Center);
            float distanceScore = Mathf.Max(0f, 1f - (distance / grindDetectionRadius));
            
            // プレイヤーの移動方向との一致度
            Vector3 playerVelocity = characterController.velocity.normalized;
            float directionScore = Vector3.Dot(playerVelocity, edge.direction);
            directionScore = Mathf.Max(0f, directionScore); // 逆方向は無視
            
            // エッジの長さスコア
            float lengthScore = Mathf.Min(1f, edge.length / 10f);
            
            // 角度スコア（水平に近いほど高スコア）
            float angle = Vector3.Angle(edge.direction, Vector3.right);
            angle = Mathf.Min(angle, Vector3.Angle(edge.direction, Vector3.left));
            float angleScore = 1f - (angle / 90f);
            
            return distanceScore * 0.4f + directionScore * 0.3f + lengthScore * 0.2f + angleScore * 0.1f;
        }
        #endregion
        
        #region グラインド制御
        /// <summary>
        /// グラインドを開始できるかチェック
        /// </summary>
        private bool CanStartGrind()
        {
            // 空中にいる必要がある
            if (characterController.isGrounded) return false;
            
            // 適切なグラインドエッジが存在する
            if (!bestGrindEdge.HasValue) return false;
            
            // プレイヤーが十分な速度を持っている
            if (characterController.velocity.magnitude < 5f) return false;
            
            return true;
        }
        
        /// <summary>
        /// グラインドを開始
        /// </summary>
        private void StartGrind()
        {
            if (!bestGrindEdge.HasValue) return;
            
            isGrinding = true;
            currentGrindEdge = bestGrindEdge.Value;
            currentGrindSurface = currentGrindEdge.Value.parentTransform;
            grindStartPosition = transform.position;
            grindStartTime = Time.time;
            
            // グラインド方向を設定
            Vector3 playerVelocity = characterController.velocity;
            Vector3 edgeDirection = currentGrindEdge.Value.direction;
            
            // プレイヤーの移動方向に最も近いエッジ方向を選択
            if (Vector3.Dot(playerVelocity, edgeDirection) < 0)
            {
                edgeDirection = -edgeDirection;
            }
            
            grindDirection = edgeDirection;
            
            // 初期グラインド速度を設定
            currentGrindSpeed = Mathf.Max(grindSpeed, playerVelocity.magnitude * momentumTransfer);
            currentGrindSpeed = Mathf.Min(currentGrindSpeed, maxGrindSpeed);
            
            // 現在の運動量を保存
            grindMomentum = playerVelocity;
            
            // エフェクトを開始
            StartGrindEffects();
            
            if (enableDebugLogs)
            {
                Debug.Log($"Started grinding on edge: {currentGrindEdge.Value.length:F1}m, speed: {currentGrindSpeed:F1}");
            }
        }
        
        /// <summary>
        /// グラインド状態を更新
        /// </summary>
        private void UpdateGrindState()
        {
            if (!currentGrindEdge.HasValue) return;
            
            // エッジに沿って位置を調整
            SnapToGrindEdge();
            
            // グラインド速度を更新
            UpdateGrindSpeed();
            
            // グラインド方向を更新
            UpdateGrindDirection();
        }
        
        /// <summary>
        /// グラインドエッジに位置をスナップ
        /// </summary>
        private void SnapToGrindEdge()
        {
            var edge = currentGrindEdge.Value;
            
            // エッジ上の最も近い点を計算
            Vector3 edgeVector = edge.endPoint - edge.startPoint;
            Vector3 playerToStart = transform.position - edge.startPoint;
            
            float projection = Vector3.Dot(playerToStart, edgeVector.normalized);
            projection = Mathf.Clamp(projection, 0f, edge.length);
            
            Vector3 targetPosition = edge.startPoint + edgeVector.normalized * projection;
            
            // エッジから少し上に配置
            targetPosition += edge.normal * 0.5f;
            
            // スムーズに位置を調整
            Vector3 currentPosition = transform.position;
            Vector3 snapDirection = (targetPosition - currentPosition);
            
            if (snapDirection.magnitude > edgeSnapDistance)
            {
                snapDirection = snapDirection.normalized * edgeSnapDistance;
            }
            
            characterController.Move(snapDirection * Time.deltaTime * 10f);
        }
        
        /// <summary>
        /// グラインド速度を更新
        /// </summary>
        private void UpdateGrindSpeed()
        {
            float grindTime = Time.time - grindStartTime;
            
            // 速度カーブを適用
            float speedMultiplier = speedCurve.Evaluate(grindTime / 5f); // 5秒でカーブを完了
            
            // 加速を適用
            currentGrindSpeed += grindAcceleration * Time.deltaTime;
            currentGrindSpeed = Mathf.Min(currentGrindSpeed, maxGrindSpeed);
            
            // カーブによる調整
            currentGrindSpeed *= speedMultiplier;
        }
        
        /// <summary>
        /// グラインド方向を更新
        /// </summary>
        private void UpdateGrindDirection()
        {
            if (!currentGrindEdge.HasValue) return;
            
            var edge = currentGrindEdge.Value;
            
            // エッジの方向を基準にする
            Vector3 baseDirection = edge.direction;
            
            // プレイヤーの入力による微調整
            float horizontal = Input.GetAxis("Horizontal");
            if (Mathf.Abs(horizontal) > 0.1f)
            {
                // 左右入力でグラインド方向を微調整
                Vector3 sideDirection = Vector3.Cross(baseDirection, Vector3.up).normalized;
                baseDirection += sideDirection * horizontal * 0.2f;
                baseDirection.Normalize();
            }
            
            grindDirection = baseDirection;
        }
        
        /// <summary>
        /// グラインド状態が有効かチェック
        /// </summary>
        private bool IsValidGrindState()
        {
            if (!currentGrindEdge.HasValue) return false;
            
            var edge = currentGrindEdge.Value;
            
            // エッジからの距離をチェック
            float distance = Vector3.Distance(transform.position, edge.Center);
            if (distance > grindDetectionRadius * 1.5f)
            {
                return false;
            }
            
            // エッジの範囲内にいるかチェック
            Vector3 edgeVector = edge.endPoint - edge.startPoint;
            Vector3 playerToStart = transform.position - edge.startPoint;
            float projection = Vector3.Dot(playerToStart, edgeVector.normalized);
            
            if (projection < -2f || projection > edge.length + 2f)
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// グラインドを終了
        /// </summary>
        private void ExitGrind()
        {
            if (!isGrinding) return;
            
            isGrinding = false;
            
            // 終了時の推進力を適用
            Vector3 exitVelocity = grindDirection * currentGrindSpeed;
            exitVelocity += transform.forward * grindExitForce;
            
            // CharacterControllerに速度を適用（次のフレームで反映）
            StartCoroutine(ApplyExitVelocity(exitVelocity));
            
            // エフェクトを停止
            StopGrindEffects();
            
            // 状態をリセット
            currentGrindEdge = null;
            currentGrindSurface = null;
            currentGrindSpeed = 0f;
            grindMomentum = Vector3.zero;
            
            if (enableDebugLogs)
            {
                Debug.Log("Exited grind");
            }
        }
        
        /// <summary>
        /// 終了時の速度を適用
        /// </summary>
        private System.Collections.IEnumerator ApplyExitVelocity(Vector3 velocity)
        {
            yield return null; // 1フレーム待機
            
            // AdvancedPlayerControllerの速度に影響を与える
            if (playerController != null)
            {
                // プライベートフィールドにアクセスするためリフレクションを使用
                var velocityField = typeof(AdvancedPlayerController).GetField("velocity", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (velocityField != null)
                {
                    Vector3 currentVelocity = (Vector3)velocityField.GetValue(playerController);
                    currentVelocity.x = velocity.x;
                    currentVelocity.z = velocity.z;
                    currentVelocity.y += velocity.y * 0.5f; // Y方向は半分の影響
                    velocityField.SetValue(playerController, currentVelocity);
                }
            }
        }
        #endregion
        
        #region 物理処理
        /// <summary>
        /// グラインド物理を適用
        /// </summary>
        private void ApplyGrindPhysics()
        {
            if (!isGrinding || !currentGrindEdge.HasValue) return;
            
            // グラインド方向に移動
            Vector3 grindMovement = grindDirection * currentGrindSpeed * Time.fixedDeltaTime;
            characterController.Move(grindMovement);
            
            // 重力を軽減
            var velocityField = typeof(AdvancedPlayerController).GetField("velocity", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (velocityField != null)
            {
                Vector3 currentVelocity = (Vector3)velocityField.GetValue(playerController);
                currentVelocity.y *= gravityReduction; // 重力を軽減
                velocityField.SetValue(playerController, currentVelocity);
            }
        }
        #endregion
        
        #region エフェクト管理
        /// <summary>
        /// グラインドエフェクトを開始
        /// </summary>
        private void StartGrindEffects()
        {
            // パーティクルエフェクト開始
            if (grindEffect != null)
            {
                grindEffect.Play();
            }
            
            // 音響エフェクト開始
            if (grindAudioSource != null)
            {
                if (grindStartSound != null)
                {
                    grindAudioSource.PlayOneShot(grindStartSound);
                }
                
                if (grindLoopSound != null)
                {
                    grindAudioSource.clip = grindLoopSound;
                    grindAudioSource.loop = true;
                    grindAudioSource.Play();
                }
            }
        }
        
        /// <summary>
        /// グラインドエフェクトを更新
        /// </summary>
        private void UpdateGrindEffects()
        {
            if (isGrinding && grindEffect != null)
            {
                // パーティクルエフェクトの位置を更新
                grindEffect.transform.position = transform.position;
                
                // 速度に応じてエフェクトの強度を調整
                var emission = grindEffect.emission;
                emission.rateOverTime = currentGrindSpeed * 2f;
            }
        }
        
        /// <summary>
        /// グラインドエフェクトを停止
        /// </summary>
        private void StopGrindEffects()
        {
            // パーティクルエフェクト停止
            if (grindEffect != null)
            {
                grindEffect.Stop();
            }
            
            // 音響エフェクト停止
            if (grindAudioSource != null)
            {
                if (grindEndSound != null)
                {
                    grindAudioSource.PlayOneShot(grindEndSound);
                }
                
                grindAudioSource.loop = false;
                grindAudioSource.Stop();
            }
        }
        #endregion
        
        #region パブリックAPI
        /// <summary>
        /// 現在グラインド中かどうか
        /// </summary>
        public bool IsGrinding => isGrinding;
        
        /// <summary>
        /// 現在のグラインド速度
        /// </summary>
        public float CurrentGrindSpeed => currentGrindSpeed;
        
        /// <summary>
        /// 現在のグラインドエッジ
        /// </summary>
        public PrimitiveInteractionSystem.GrindEdge? CurrentGrindEdge => currentGrindEdge;
        
        /// <summary>
        /// 強制的にグラインドを終了
        /// </summary>
        public void ForceExitGrind()
        {
            if (isGrinding)
            {
                ExitGrind();
            }
        }
        #endregion
        
        #region デバッグ・可視化
        void OnDrawGizmosSelected()
        {
            if (!showGrindGizmos) return;
            
            // グラインド検出範囲
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, grindDetectionRadius);
            
            // 現在のグラインドエッジ
            if (isGrinding && currentGrindEdge.HasValue)
            {
                var edge = currentGrindEdge.Value;
                Gizmos.color = Color.green;
                Gizmos.DrawLine(edge.startPoint, edge.endPoint);
                Gizmos.DrawWireSphere(edge.Center, 1f);
                
                // グラインド方向
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, grindDirection * 3f);
            }
            
            // 検出されたグラインドエッジ
            Gizmos.color = Color.cyan;
            foreach (var edge in nearbyGrindEdges)
            {
                Gizmos.DrawLine(edge.startPoint, edge.endPoint);
            }
            
            // 最適なグラインドエッジ
            if (bestGrindEdge.HasValue)
            {
                var best = bestGrindEdge.Value;
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(best.Center, 0.5f);
            }
        }
        
        void OnGUI()
        {
            if (showGrindInfo)
            {
                var rect = new Rect(10, 150, 300, 100);
                string info = $"Grind System:\n";
                info += $"Is Grinding: {isGrinding}\n";
                info += $"Grind Speed: {currentGrindSpeed:F1}\n";
                info += $"Nearby Edges: {nearbyGrindEdges.Count}\n";
                info += $"Best Edge: {(bestGrindEdge.HasValue ? "Yes" : "No")}";
                
                GUI.Box(rect, info);
            }
        }
        #endregion
    }
}