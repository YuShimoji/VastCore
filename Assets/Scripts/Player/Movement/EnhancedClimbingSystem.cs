using UnityEngine;
using System.Collections.Generic;
using Vastcore.Terrain.Map;

namespace Vastcore.Player
{
    /// <summary>
    /// 強化されたクライミングシステム
    /// プリミティブ地形オブジェクトとの統合クライミング機能を提供
    /// </summary>
    public class EnhancedClimbingSystem : MonoBehaviour
    {
        [Header("クライミング基本設定")]
        [SerializeField] private float climbSpeed = 8f;
        [SerializeField] private float climbAcceleration = 10f;
        [SerializeField] private float maxClimbSpeed = 15f;
        [SerializeField] private float climbDetectionRadius = 1.5f;
        [SerializeField] private float climbStamina = 100f;
        [SerializeField] private float staminaConsumption = 20f;
        [SerializeField] private float staminaRegenRate = 15f;
        
        [Header("クライミング物理")]
        [SerializeField] private float wallStickForce = 10f;
        [SerializeField] private float climbGravityReduction = 0.9f;
        [SerializeField] private float wallJumpForce = 12f;
        [SerializeField] private float surfaceSnapDistance = 0.5f;
        [SerializeField] private float minClimbAngle = 45f;
        [SerializeField] private float maxClimbAngle = 85f;
        
        [Header("表面検出")]
        [SerializeField] private LayerMask climbableLayer = -1;
        [SerializeField] private float surfaceCheckDistance = 2f;
        [SerializeField] private int surfaceCheckRays = 8;
        [SerializeField] private float minSurfaceArea = 5f;
        
        [Header("入力設定")]
        [SerializeField] private KeyCode climbKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode wallJumpKey = KeyCode.Space;
        [SerializeField] private bool requireHoldToClimb = true;
        [SerializeField] private float inputSensitivity = 1f;
        
        [Header("エフェクト")]
        [SerializeField] private ParticleSystem climbEffect;
        [SerializeField] private AudioSource climbAudioSource;
        [SerializeField] private AudioClip climbStartSound;
        [SerializeField] private AudioClip climbLoopSound;
        [SerializeField] private AudioClip wallJumpSound;
        
        [Header("デバッグ")]
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private bool showClimbGizmos = false;
        [SerializeField] private bool showClimbInfo = false;
        
        // プライベート変数
        private AdvancedPlayerController playerController;
        private CharacterController characterController;
        private PrimitiveInteractionSystem interactionSystem;
        
        // クライミング状態
        private bool isClimbing = false;
        private PrimitiveInteractionSystem.ClimbSurface? currentClimbSurface;
        private Vector3 climbDirection;
        private Vector3 surfaceNormal;
        private Vector3 climbStartPosition;
        private float currentClimbSpeed;
        private float climbStartTime;
        private float currentStamina;
        
        // 検出されたクライミング可能オブジェクト
        private List<PrimitiveInteractionSystem.ClimbSurface> nearbyClimbSurfaces = new List<PrimitiveInteractionSystem.ClimbSurface>();
        private PrimitiveInteractionSystem.ClimbSurface? bestClimbSurface;
        
        // 表面検出データ
        private List<RaycastHit> surfaceHits = new List<RaycastHit>();
        private Vector3 averageSurfaceNormal;
        private Vector3 averageSurfacePoint;
        
        // パフォーマンス最適化
        private float lastDetectionTime;
        private const float detectionInterval = 0.1f;
        
        // 壁ジャンプ
        private bool canWallJump = false;
        private float wallJumpCooldown = 0.5f;
        private float lastWallJumpTime;
        
        #region Unity生命周期
        void Start()
        {
            InitializeSystem();
        }
        
        void Update()
        {
            UpdateClimbingSystem();
        }
        
        void FixedUpdate()
        {
            if (isClimbing)
            {
                ApplyClimbPhysics();
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
            
            // スタミナを初期化
            currentStamina = climbStamina;
            
            // オーディオソースを設定
            if (climbAudioSource == null)
            {
                climbAudioSource = gameObject.AddComponent<AudioSource>();
                climbAudioSource.spatialBlend = 1f; // 3D音響
                climbAudioSource.volume = 0.6f;
            }
            
            // パーティクルエフェクトを設定
            if (climbEffect == null)
            {
                climbEffect = CreateClimbParticleEffect();
            }
            
            Debug.Log("EnhancedClimbingSystem initialized");
        }
        
        /// <summary>
        /// クライミングパーティクルエフェクトを作成
        /// </summary>
        private ParticleSystem CreateClimbParticleEffect()
        {
            var effectObject = new GameObject("ClimbEffect");
            effectObject.transform.SetParent(transform);
            effectObject.transform.localPosition = Vector3.zero;
            
            var particles = effectObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 0.3f;
            main.startSpeed = 2f;
            main.startSize = 0.05f;
            main.startColor = Color.white;
            main.maxParticles = 20;
            
            var emission = particles.emission;
            emission.rateOverTime = 15f;
            
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.3f;
            
            particles.Stop();
            return particles;
        }
        #endregion
        
        #region メインシステム更新
        /// <summary>
        /// クライミングシステムを更新
        /// </summary>
        public void UpdateClimbingSystem()
        {
            // スタミナ管理
            UpdateStamina();
            
            // クライミング可能表面を定期的に検出
            if (Time.time - lastDetectionTime > detectionInterval)
            {
                DetectClimbableSurfaces();
                lastDetectionTime = Time.time;
            }
            
            if (!isClimbing)
            {
                // クライミング開始の判定
                if (ShouldStartClimbing() && CanStartClimb())
                {
                    StartClimb();
                }
                
                // 壁ジャンプの判定
                if (Input.GetKeyDown(wallJumpKey) && canWallJump && CanWallJump())
                {
                    PerformWallJump();
                }
            }
            else
            {
                // クライミング中の処理
                UpdateClimbState();
                
                // クライミング終了の判定
                if (!ShouldContinueClimbing() || !IsValidClimbState())
                {
                    ExitClimb();
                }
                
                // 壁ジャンプの判定（クライミング中）
                if (Input.GetKeyDown(wallJumpKey))
                {
                    ExitClimb();
                    PerformWallJump();
                }
            }
            
            UpdateClimbEffects();
        }
        
        /// <summary>
        /// スタミナを更新
        /// </summary>
        private void UpdateStamina()
        {
            if (isClimbing)
            {
                // クライミング中はスタミナを消費
                currentStamina -= staminaConsumption * Time.deltaTime;
                currentStamina = Mathf.Max(0f, currentStamina);
            }
            else
            {
                // 非クライミング中はスタミナを回復
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(climbStamina, currentStamina);
            }
        }
        
        /// <summary>
        /// クライミング可能表面を検出
        /// </summary>
        private void DetectClimbableSurfaces()
        {
            nearbyClimbSurfaces.Clear();
            bestClimbSurface = null;
            surfaceHits.Clear();
            canWallJump = false;
            
            if (interactionSystem == null) return;
            
            // プリミティブインタラクションシステムから近くのクライミング表面を取得
            var surfaces = interactionSystem.GetNearbyClimbSurfaces(transform.position, climbDetectionRadius);
            nearbyClimbSurfaces.AddRange(surfaces);
            
            // レイキャストベースの表面検出
            DetectSurfacesWithRaycast();
            
            // 従来のコライダーベース検出も併用
            DetectTraditionalClimbSurfaces();
            
            // 最適なクライミング表面を選択
            SelectBestClimbSurface();
            
            // 壁ジャンプ可能性をチェック
            CheckWallJumpAvailability();
        }
        
        /// <summary>
        /// レイキャストによる表面検出
        /// </summary>
        private void DetectSurfacesWithRaycast()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            
            for (int i = 0; i < surfaceCheckRays; i++)
            {
                float angle = (360f / surfaceCheckRays) * i;
                Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                
                RaycastHit hit;
                if (Physics.Raycast(origin, direction, out hit, surfaceCheckDistance, climbableLayer))
                {
                    // クライミング可能な角度かチェック
                    float surfaceAngle = Vector3.Angle(Vector3.up, hit.normal);
                    if (surfaceAngle >= minClimbAngle && surfaceAngle <= maxClimbAngle)
                    {
                        surfaceHits.Add(hit);
                    }
                }
            }
            
            // 平均表面法線と位置を計算
            if (surfaceHits.Count > 0)
            {
                Vector3 totalNormal = Vector3.zero;
                Vector3 totalPoint = Vector3.zero;
                
                foreach (var hit in surfaceHits)
                {
                    totalNormal += hit.normal;
                    totalPoint += hit.point;
                }
                
                averageSurfaceNormal = (totalNormal / surfaceHits.Count).normalized;
                averageSurfacePoint = totalPoint / surfaceHits.Count;
                
                // 人工的なクライミング表面を作成
                if (surfaceHits.Count >= 3) // 十分な表面データがある場合
                {
                    var artificialSurface = new PrimitiveInteractionSystem.ClimbSurface
                    {
                        center = averageSurfacePoint,
                        normal = averageSurfaceNormal,
                        up = Vector3.Cross(averageSurfaceNormal, Vector3.right).normalized,
                        area = surfaceHits.Count * 2f, // 検出点数に基づく面積推定
                        bounds = new Bounds(averageSurfacePoint, Vector3.one * 2f),
                        parentTransform = surfaceHits[0].transform,
                        surfaceCollider = surfaceHits[0].collider
                    };
                    
                    nearbyClimbSurfaces.Add(artificialSurface);
                }
            }
        }
        
        /// <summary>
        /// 従来のコライダーベースクライミング表面検出
        /// </summary>
        private void DetectTraditionalClimbSurfaces()
        {
            Collider[] climbableObjects = Physics.OverlapSphere(transform.position, climbDetectionRadius, climbableLayer);
            
            foreach (Collider col in climbableObjects)
            {
                // プリミティブオブジェクトでない場合の処理
                if (col.GetComponent<PrimitiveTerrainObject>() == null)
                {
                    ProcessTraditionalClimbSurface(col);
                }
            }
        }
        
        /// <summary>
        /// 従来のクライミング表面を処理
        /// </summary>
        private void ProcessTraditionalClimbSurface(Collider surface)
        {
            Vector3 closestPoint = surface.ClosestPoint(transform.position);
            Vector3 surfaceNormal = (transform.position - closestPoint).normalized;
            
            // 人工的なクライミング表面を作成
            var artificialSurface = new PrimitiveInteractionSystem.ClimbSurface
            {
                center = closestPoint,
                normal = surfaceNormal,
                up = Vector3.up,
                area = 10f, // デフォルト面積
                bounds = new Bounds(closestPoint, Vector3.one * 3f),
                parentTransform = surface.transform,
                surfaceCollider = surface
            };
            
            nearbyClimbSurfaces.Add(artificialSurface);
        }
        
        /// <summary>
        /// 最適なクライミング表面を選択
        /// </summary>
        private void SelectBestClimbSurface()
        {
            if (nearbyClimbSurfaces.Count == 0) return;
            
            float bestScore = float.MinValue;
            PrimitiveInteractionSystem.ClimbSurface? best = null;
            
            foreach (var surface in nearbyClimbSurfaces)
            {
                float score = CalculateClimbSurfaceScore(surface);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = surface;
                }
            }
            
            bestClimbSurface = best;
        }
        
        /// <summary>
        /// クライミング表面のスコアを計算
        /// </summary>
        private float CalculateClimbSurfaceScore(PrimitiveInteractionSystem.ClimbSurface surface)
        {
            float distance = Vector3.Distance(transform.position, surface.center);
            float distanceScore = Mathf.Max(0f, 1f - (distance / climbDetectionRadius));
            
            // プレイヤーの向きとの一致度
            Vector3 playerForward = transform.forward;
            float directionScore = Vector3.Dot(playerForward, -surface.normal);
            directionScore = Mathf.Max(0f, directionScore);
            
            // 表面の面積スコア
            float areaScore = Mathf.Min(1f, surface.area / 20f);
            
            // 角度スコア（クライミングに適した角度）
            float angle = Vector3.Angle(Vector3.up, surface.normal);
            float optimalAngle = 75f; // 最適角度
            float angleDiff = Mathf.Abs(angle - optimalAngle);
            float angleScore = 1f - (angleDiff / 45f);
            angleScore = Mathf.Max(0f, angleScore);
            
            return distanceScore * 0.3f + directionScore * 0.3f + areaScore * 0.2f + angleScore * 0.2f;
        }
        
        /// <summary>
        /// 壁ジャンプ可能性をチェック
        /// </summary>
        private void CheckWallJumpAvailability()
        {
            canWallJump = false;
            
            if (Time.time - lastWallJumpTime < wallJumpCooldown) return;
            
            // 前方に壁があるかチェック
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            RaycastHit hit;
            
            if (Physics.Raycast(origin, transform.forward, out hit, 1.5f, climbableLayer))
            {
                float angle = Vector3.Angle(Vector3.up, hit.normal);
                if (angle >= 60f && angle <= 90f) // 壁ジャンプに適した角度
                {
                    canWallJump = true;
                }
            }
        }
        #endregion
        
        #region クライミング制御
        /// <summary>
        /// クライミングを開始すべきかチェック
        /// </summary>
        private bool ShouldStartClimbing()
        {
            if (requireHoldToClimb)
            {
                return Input.GetKey(climbKey);
            }
            else
            {
                return Input.GetKeyDown(climbKey);
            }
        }
        
        /// <summary>
        /// クライミングを継続すべきかチェック
        /// </summary>
        private bool ShouldContinueClimbing()
        {
            if (requireHoldToClimb)
            {
                return Input.GetKey(climbKey);
            }
            else
            {
                return !Input.GetKeyDown(climbKey);
            }
        }
        
        /// <summary>
        /// クライミングを開始できるかチェック
        /// </summary>
        private bool CanStartClimb()
        {
            // スタミナが十分にある
            if (currentStamina < 10f) return false;
            
            // 適切なクライミング表面が存在する
            if (!bestClimbSurface.HasValue) return false;
            
            // 地面にいない（空中または壁に接触）
            if (characterController.isGrounded && characterController.velocity.y <= 0) return false;
            
            return true;
        }
        
        /// <summary>
        /// 壁ジャンプが可能かチェック
        /// </summary>
        private bool CanWallJump()
        {
            return Time.time - lastWallJumpTime >= wallJumpCooldown;
        }
        
        /// <summary>
        /// クライミングを開始
        /// </summary>
        private void StartClimb()
        {
            if (!bestClimbSurface.HasValue) return;
            
            isClimbing = true;
            currentClimbSurface = bestClimbSurface.Value;
            climbStartPosition = transform.position;
            climbStartTime = Time.time;
            
            // 表面法線を設定
            surfaceNormal = currentClimbSurface.Value.normal;
            
            // 初期クライミング速度を設定
            currentClimbSpeed = climbSpeed;
            
            // エフェクトを開始
            StartClimbEffects();
            
            if (enableDebugLogs)
            {
                Debug.Log($"Started climbing on surface: {currentClimbSurface.Value.area:F1}m2");
            }
        }
        
        /// <summary>
        /// クライミング状態を更新
        /// </summary>
        private void UpdateClimbState()
        {
            if (!currentClimbSurface.HasValue) return;
            
            // 表面に沿って位置を調整
            SnapToClimbSurface();
            
            // クライミング方向を更新
            UpdateClimbDirection();
            
            // クライミング速度を更新
            UpdateClimbSpeed();
        }
        
        /// <summary>
        /// クライミング表面に位置をスナップ
        /// </summary>
        private void SnapToClimbSurface()
        {
            var surface = currentClimbSurface.Value;
            
            // 表面から適切な距離を保つ
            Vector3 targetPosition = surface.center - surface.normal * surfaceSnapDistance;
            
            Vector3 currentPosition = transform.position;
            Vector3 snapDirection = (targetPosition - currentPosition);
            
            // 表面に近すぎる場合は調整
            if (snapDirection.magnitude > 0.1f)
            {
                characterController.Move(snapDirection * Time.deltaTime * 5f);
            }
        }
        
        /// <summary>
        /// クライミング方向を更新
        /// </summary>
        private void UpdateClimbDirection()
        {
            if (!currentClimbSurface.HasValue) return;
            
            var surface = currentClimbSurface.Value;
            
            // 入力を取得
            float horizontal = Input.GetAxis("Horizontal") * inputSensitivity;
            float vertical = Input.GetAxis("Vertical") * inputSensitivity;
            
            // 表面に沿った移動方向を計算
            Vector3 rightDirection = Vector3.Cross(surface.up, surface.normal).normalized;
            Vector3 upDirection = surface.up;
            
            climbDirection = (rightDirection * horizontal + upDirection * vertical).normalized;
        }
        
        /// <summary>
        /// クライミング速度を更新
        /// </summary>
        private void UpdateClimbSpeed()
        {
            float inputMagnitude = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).magnitude;
            
            if (inputMagnitude > 0.1f)
            {
                // 入力がある場合は加速
                currentClimbSpeed += climbAcceleration * Time.deltaTime;
                currentClimbSpeed = Mathf.Min(currentClimbSpeed, maxClimbSpeed);
            }
            else
            {
                // 入力がない場合は減速
                currentClimbSpeed = Mathf.Lerp(currentClimbSpeed, 0f, Time.deltaTime * 5f);
            }
        }
        
        /// <summary>
        /// クライミング状態が有効かチェック
        /// </summary>
        private bool IsValidClimbState()
        {
            if (!currentClimbSurface.HasValue) return false;
            
            // スタミナが残っている
            if (currentStamina <= 0f) return false;
            
            var surface = currentClimbSurface.Value;
            
            // 表面からの距離をチェック
            float distance = Vector3.Distance(transform.position, surface.center);
            if (distance > climbDetectionRadius * 1.5f) return false;
            
            return true;
        }
        
        /// <summary>
        /// クライミングを終了
        /// </summary>
        private void ExitClimb()
        {
            if (!isClimbing) return;
            
            isClimbing = false;
            
            // エフェクトを停止
            StopClimbEffects();
            
            // 状態をリセット
            currentClimbSurface = null;
            currentClimbSpeed = 0f;
            climbDirection = Vector3.zero;
            surfaceNormal = Vector3.zero;
            
            if (enableDebugLogs)
            {
                Debug.Log("Exited climbing");
            }
        }
        
        /// <summary>
        /// 壁ジャンプを実行
        /// </summary>
        private void PerformWallJump()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            RaycastHit hit;
            
            if (Physics.Raycast(origin, transform.forward, out hit, 1.5f, climbableLayer))
            {
                // 壁から離れる方向にジャンプ
                Vector3 jumpDirection = -hit.normal + Vector3.up;
                jumpDirection.Normalize();
                
                // AdvancedPlayerControllerの速度に影響を与える
                var velocityField = typeof(AdvancedPlayerController).GetField("velocity", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (velocityField != null)
                {
                    Vector3 jumpVelocity = jumpDirection * wallJumpForce;
                    velocityField.SetValue(playerController, jumpVelocity);
                }
                
                lastWallJumpTime = Time.time;
                
                // 音響エフェクト
                if (climbAudioSource != null && wallJumpSound != null)
                {
                    climbAudioSource.PlayOneShot(wallJumpSound);
                }
                
                if (enableDebugLogs)
                {
                    Debug.Log("Performed wall jump");
                }
            }
        }
        #endregion
        
        #region 物理処理
        /// <summary>
        /// クライミング物理を適用
        /// </summary>
        private void ApplyClimbPhysics()
        {
            if (!isClimbing || !currentClimbSurface.HasValue) return;
            
            // クライミング方向に移動
            if (climbDirection.magnitude > 0.1f)
            {
                Vector3 climbMovement = climbDirection * currentClimbSpeed * Time.fixedDeltaTime;
                characterController.Move(climbMovement);
            }
            
            // 壁に張り付く力を適用
            Vector3 stickForce = -surfaceNormal * wallStickForce * Time.fixedDeltaTime;
            characterController.Move(stickForce);
            
            // 重力を軽減
            var velocityField = typeof(AdvancedPlayerController).GetField("velocity", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (velocityField != null)
            {
                Vector3 currentVelocity = (Vector3)velocityField.GetValue(playerController);
                currentVelocity.y *= climbGravityReduction; // 重力を大幅に軽減
                velocityField.SetValue(playerController, currentVelocity);
            }
        }
        #endregion
        
        #region エフェクト管理
        /// <summary>
        /// クライミングエフェクトを開始
        /// </summary>
        private void StartClimbEffects()
        {
            // パーティクルエフェクト開始
            if (climbEffect != null)
            {
                climbEffect.Play();
            }
            
            // 音響エフェクト開始
            if (climbAudioSource != null)
            {
                if (climbStartSound != null)
                {
                    climbAudioSource.PlayOneShot(climbStartSound);
                }
                
                if (climbLoopSound != null)
                {
                    climbAudioSource.clip = climbLoopSound;
                    climbAudioSource.loop = true;
                    climbAudioSource.Play();
                }
            }
        }
        
        /// <summary>
        /// クライミングエフェクトを更新
        /// </summary>
        private void UpdateClimbEffects()
        {
            if (isClimbing && climbEffect != null)
            {
                // パーティクルエフェクトの位置と方向を更新
                climbEffect.transform.position = transform.position;
                climbEffect.transform.rotation = Quaternion.LookRotation(-surfaceNormal);
                
                // 速度に応じてエフェクトの強度を調整
                var emission = climbEffect.emission;
                emission.rateOverTime = currentClimbSpeed * 1.5f;
            }
        }
        
        /// <summary>
        /// クライミングエフェクトを停止
        /// </summary>
        private void StopClimbEffects()
        {
            // パーティクルエフェクト停止
            if (climbEffect != null)
            {
                climbEffect.Stop();
            }
            
            // 音響エフェクト停止
            if (climbAudioSource != null)
            {
                climbAudioSource.loop = false;
                climbAudioSource.Stop();
            }
        }
        #endregion
        
        #region パブリックAPI
        /// <summary>
        /// 現在クライミング中かどうか
        /// </summary>
        public bool IsClimbing => isClimbing;
        
        /// <summary>
        /// 現在のクライミング速度
        /// </summary>
        public float CurrentClimbSpeed => currentClimbSpeed;
        
        /// <summary>
        /// 現在のスタミナ
        /// </summary>
        public float CurrentStamina => currentStamina;
        
        /// <summary>
        /// スタミナの割合
        /// </summary>
        public float StaminaRatio => currentStamina / climbStamina;
        
        /// <summary>
        /// 壁ジャンプが可能かどうか
        /// </summary>
        public bool CanPerformWallJump => canWallJump && CanWallJump();
        
        /// <summary>
        /// 強制的にクライミングを終了
        /// </summary>
        public void ForceExitClimb()
        {
            if (isClimbing)
            {
                ExitClimb();
            }
        }
        #endregion
        
        #region デバッグ・可視化
        void OnDrawGizmosSelected()
        {
            if (!showClimbGizmos) return;
            
            // クライミング検出範囲
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, climbDetectionRadius);
            
            // 表面検出レイ
            if (surfaceHits.Count > 0)
            {
                Gizmos.color = Color.yellow;
                Vector3 origin = transform.position + Vector3.up * 0.5f;
                
                for (int i = 0; i < surfaceCheckRays; i++)
                {
                    float angle = (360f / surfaceCheckRays) * i;
                    Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                    Gizmos.DrawRay(origin, direction * surfaceCheckDistance);
                }
                
                // 平均表面法線
                Gizmos.color = Color.red;
                Gizmos.DrawRay(averageSurfacePoint, averageSurfaceNormal * 2f);
            }
            
            // 現在のクライミング表面
            if (isClimbing && currentClimbSurface.HasValue)
            {
                var surface = currentClimbSurface.Value;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(surface.center, surface.bounds.size);
                
                // 表面法線
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(surface.center, surface.normal * 2f);
                
                // クライミング方向
                if (climbDirection.magnitude > 0.1f)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawRay(transform.position, climbDirection * 2f);
                }
            }
            
            // 検出されたクライミング表面
            Gizmos.color = Color.white;
            foreach (var surface in nearbyClimbSurfaces)
            {
                Gizmos.DrawWireCube(surface.center, surface.bounds.size * 0.8f);
            }
            
            // 最適なクライミング表面
            if (bestClimbSurface.HasValue)
            {
                var best = bestClimbSurface.Value;
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(best.center, 0.5f);
            }
            
            // 壁ジャンプ可能範囲
            if (canWallJump)
            {
                Gizmos.color = Color.orange;
                Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, transform.forward * 1.5f);
            }
        }
        
        void OnGUI()
        {
            if (showClimbInfo)
            {
                var rect = new Rect(10, 250, 300, 120);
                string info = $"Climb System:\n";
                info += $"Is Climbing: {isClimbing}\n";
                info += $"Climb Speed: {currentClimbSpeed:F1}\n";
                info += $"Stamina: {currentStamina:F1}/{climbStamina:F1}\n";
                info += $"Nearby Surfaces: {nearbyClimbSurfaces.Count}\n";
                info += $"Can Wall Jump: {canWallJump}";
                
                GUI.Box(rect, info);
            }
        }
        #endregion
    }
}