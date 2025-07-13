using UnityEngine;

namespace Vastcore.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class AdvancedPlayerController : MonoBehaviour
    {
        #region 基本移動パラメータ
        [Header("Movement")]
        public float moveSpeed = 10f;
        public float gravity = -9.81f;
        public float jumpHeight = 8f;
        #endregion

        #region カメラ設定
        [Header("Looking")]
        public float lookSpeed = 2f;
        public float lookXLimit = 45.0f;
        public Transform cameraTransform;
        #endregion

        #region グライドシステム
        [Header("Gliding")]
        public float glideGravity = -2f;
        public float glideForwardForce = 5f;
        public float glideMaxSpeed = 25f;
        #endregion

        #region 特殊ダッシュ（夢の飛行）システム
        [Header("Dream Flight Dash")]
        public float dreamFlightBaseSpeed = 15f;
        public float dreamFlightMaxSpeed = 80f;
        public float dreamFlightAcceleration = 3f;
        public float dreamFlightEnergy = 100f;
        public float dreamFlightEnergyConsumption = 25f;
        public float dreamFlightEnergyRecharge = 10f;
        public KeyCode dreamFlightKey = KeyCode.Q;
        #endregion

        #region グラインドシステム
        [Header("Grind System")]
        public float grindSpeed = 20f;
        public float grindDetectionRadius = 2f;
        public float grindForce = 15f;
        public LayerMask grindableLayer = -1;
        public float grindExitForce = 10f;
        #endregion

        #region ワープ移動システム
        [Header("Translocation")]
        public GameObject translocationSpherePrefab;
        public float sphereLaunchForce = 50f;
        public float sphereLifetime = 8f;
        public float warpCooldown = 2f;
        #endregion

        #region 環境インタラクション
        [Header("Environment Interaction")]
        public float wallKickForce = 15f;
        public float wallKickRadius = 1f;
        public float wallKickCooldown = 0.5f;
        public LayerMask wallLayer = -1;
        #endregion

        #region 内部変数
        private CharacterController characterController;
        private Vector3 velocity;
        private float rotationX = 0;

        // 状態管理
        private bool isGliding = false;
        private bool isDreamFlying = false;
        private bool isGrinding = false;
        
        // タイマー
        private float warpCooldownTimer = 0f;
        private float wallKickCooldownTimer = 0f;
        
        // 特殊ダッシュ
        private float currentDreamFlightSpeed = 0f;
        private float currentEnergy = 0f;
        
        // グラインド
        private Transform currentGrindSurface;
        private Vector3 grindDirection;
        
        // UI表示用
        [Header("Debug Info")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private float currentSpeed = 0f;
        [SerializeField] private string currentState = "Ground";
        #endregion

        #region Unity生命周期
        private void Start()
        {
            characterController = GetComponent<CharacterController>();
            currentEnergy = dreamFlightEnergy;

            if (cameraTransform == null)
            {
                cameraTransform = GetComponentInChildren<Camera>()?.transform;
                if (cameraTransform == null)
                {
                    Debug.LogError("No camera found as a child of the player. Please assign one.");
                    enabled = false;
                    return;
                }
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnEnable()
        {
            TranslocationSphere.OnSphereCollision += TeleportPlayer;
        }

        private void OnDisable()
        {
            TranslocationSphere.OnSphereCollision -= TeleportPlayer;
        }

        private void Update()
        {
            HandleCooldowns();
            HandleMovement();
            HandleLooking();
            HandleGliding();
            HandleDreamFlight();
            HandleGrinding();
            HandleWallKick();
            HandleTranslocation();
            UpdateDebugInfo();
        }
        #endregion

        #region 基本システム
        private void HandleCooldowns()
        {
            if (warpCooldownTimer > 0)
                warpCooldownTimer -= Time.deltaTime;
            
            if (wallKickCooldownTimer > 0)
                wallKickCooldownTimer -= Time.deltaTime;
                
            // エネルギー回復
            if (!isDreamFlying && currentEnergy < dreamFlightEnergy)
            {
                currentEnergy = Mathf.Min(dreamFlightEnergy, currentEnergy + dreamFlightEnergyRecharge * Time.deltaTime);
            }
        }

        private void HandleMovement()
        {
            Vector3 moveDirection = Vector3.zero;
            float currentSpeed = moveSpeed;

            // 特殊ダッシュ中の処理
            if (isDreamFlying)
            {
                moveDirection = transform.forward;
                currentSpeed = currentDreamFlightSpeed;
            }
            // グラインド中の処理
            else if (isGrinding)
            {
                moveDirection = grindDirection;
                currentSpeed = grindSpeed;
            }
            // 通常移動
            else
            {
                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");
                moveDirection = transform.right * horizontal + transform.forward * vertical;
            }

            // 移動実行
            if (moveDirection != Vector3.zero)
            {
                characterController.Move(moveDirection * currentSpeed * Time.deltaTime);
            }

            // 重力処理
            HandleGravity();
        }

        private void HandleGravity()
        {
            if (isGliding)
            {
                velocity.y += glideGravity * Time.deltaTime;
                // グライド中は前方にも推進力を加える
                characterController.Move(transform.forward * glideForwardForce * Time.deltaTime);
            }
            else if (isDreamFlying)
            {
                // 夢の飛行中は重力を軽減
                velocity.y += gravity * 0.3f * Time.deltaTime;
            }
            else if (isGrinding)
            {
                // グラインド中は重力を無効化
                velocity.y = 0;
            }
            else
            {
                // 通常の重力
                if (characterController.isGrounded && velocity.y < 0)
                {
                    velocity.y = -2f;
                }
                velocity.y += gravity * Time.deltaTime;
            }

            // ジャンプ処理
            if (Input.GetButtonDown("Jump") && characterController.isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            characterController.Move(velocity * Time.deltaTime);
        }

        private void HandleLooking()
        {
            float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

            rotationX += -mouseY;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            cameraTransform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, mouseX, 0);
        }
        #endregion

        #region 特殊移動システム
        private void HandleGliding()
        {
            // スペースキーを押している間、かつ空中にいる場合にグライド
            if (Input.GetKey(KeyCode.Space) && !characterController.isGrounded)
            {
                isGliding = true;
                // グライド中は最大速度を制限
                Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
                if (horizontalVelocity.magnitude > glideMaxSpeed)
                {
                    horizontalVelocity = horizontalVelocity.normalized * glideMaxSpeed;
                    velocity.x = horizontalVelocity.x;
                    velocity.z = horizontalVelocity.z;
                }
            }
            else
            {
                isGliding = false;
            }
        }

        private void HandleDreamFlight()
        {
            // 夢の飛行の開始・終了処理
            if (Input.GetKey(dreamFlightKey) && currentEnergy > 0)
            {
                if (!isDreamFlying)
                {
                    isDreamFlying = true;
                    currentDreamFlightSpeed = dreamFlightBaseSpeed;
                }
                
                // 指数関数的加速
                currentDreamFlightSpeed = Mathf.Min(dreamFlightMaxSpeed, 
                    currentDreamFlightSpeed + dreamFlightAcceleration * Time.deltaTime * currentDreamFlightSpeed * 0.1f);
                
                // エネルギー消費
                currentEnergy = Mathf.Max(0, currentEnergy - dreamFlightEnergyConsumption * Time.deltaTime);
            }
            else
            {
                isDreamFlying = false;
                currentDreamFlightSpeed = 0;
            }
        }

        private void HandleGrinding()
        {
            // グラインド可能な表面を検出
            if (!isGrinding)
            {
                DetectGrindableSurface();
            }
            else
            {
                // グラインド中の処理
                if (Input.GetKeyDown(KeyCode.E) || !IsValidGrindSurface())
                {
                    ExitGrind();
                }
                else
                {
                    UpdateGrindDirection();
                }
            }
        }

        private void DetectGrindableSurface()
        {
            // 周囲のグラインド可能な表面を検出
            Collider[] grindableObjects = Physics.OverlapSphere(transform.position, grindDetectionRadius, grindableLayer);
            
            foreach (Collider col in grindableObjects)
            {
                if (Input.GetKeyDown(KeyCode.E) && !characterController.isGrounded)
                {
                    StartGrind(col.transform);
                    break;
                }
            }
        }

        private void StartGrind(Transform surface)
        {
            isGrinding = true;
            currentGrindSurface = surface;
            
            // グラインド方向を計算（表面に沿った方向）
            Vector3 surfaceDirection = surface.forward;
            grindDirection = Vector3.ProjectOnPlane(surfaceDirection, Vector3.up).normalized;
            
            // グラインド開始時の推進力
            velocity += grindDirection * grindForce;
        }

        private void ExitGrind()
        {
            isGrinding = false;
            currentGrindSurface = null;
            
            // グラインド終了時の推進力
            velocity += transform.forward * grindExitForce;
        }

        private bool IsValidGrindSurface()
        {
            if (currentGrindSurface == null) return false;
            
            float distance = Vector3.Distance(transform.position, currentGrindSurface.position);
            return distance <= grindDetectionRadius * 1.5f;
        }

        private void UpdateGrindDirection()
        {
            if (currentGrindSurface != null)
            {
                Vector3 toSurface = (currentGrindSurface.position - transform.position).normalized;
                grindDirection = Vector3.ProjectOnPlane(toSurface, Vector3.up).normalized;
            }
        }

        private void HandleWallKick()
        {
            if (Input.GetKeyDown(KeyCode.F) && wallKickCooldownTimer <= 0)
            {
                PerformWallKick();
            }
        }

        private void PerformWallKick()
        {
            // 前方の壁を検出
            Vector3 kickDirection = transform.forward;
            RaycastHit hit;
            
            if (Physics.Raycast(transform.position, kickDirection, out hit, wallKickRadius, wallLayer))
            {
                // 壁の法線方向に反発
                Vector3 reflectDirection = Vector3.Reflect(kickDirection, hit.normal);
                velocity += reflectDirection * wallKickForce;
                
                wallKickCooldownTimer = wallKickCooldown;
            }
        }

        private void HandleTranslocation()
        {
            if (Input.GetMouseButtonDown(1) && warpCooldownTimer <= 0) // 右クリック
            {
                LaunchSphere();
            }
        }

        private void LaunchSphere()
        {
            if (translocationSpherePrefab == null)
            {
                Debug.LogError("Translocation Sphere Prefab is not assigned.");
                return;
            }

            Transform cameraTransform = Camera.main.transform;
            GameObject sphere = Instantiate(translocationSpherePrefab, cameraTransform.position + cameraTransform.forward, Quaternion.identity);
            
            // 球体の設定
            Rigidbody rb = sphere.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(cameraTransform.forward * sphereLaunchForce, ForceMode.Impulse);
            }
            
            // 球体の生存時間設定
            TranslocationSphere sphereScript = sphere.GetComponent<TranslocationSphere>();
            if (sphereScript != null)
            {
                sphereScript.lifeTime = sphereLifetime;
            }
            
            warpCooldownTimer = warpCooldown;
        }

        private void TeleportPlayer(Vector3 targetPosition)
        {
            // キャラクターコントローラーを一時無効化してテレポート
            characterController.enabled = false;
            transform.position = targetPosition + Vector3.up * 2f; // 少し上に配置
            characterController.enabled = true;

            // 縦方向の速度をリセット
            velocity.y = 0;
            
            // テレポート後の推進力
            velocity += transform.forward * 5f;
        }
        #endregion

        #region デバッグ・UI
        private void UpdateDebugInfo()
        {
            if (showDebugInfo)
            {
                currentSpeed = characterController.velocity.magnitude;
                
                if (isDreamFlying)
                    currentState = "Dream Flight";
                else if (isGrinding)
                    currentState = "Grinding";
                else if (isGliding)
                    currentState = "Gliding";
                else if (characterController.isGrounded)
                    currentState = "Ground";
                else
                    currentState = "Air";
            }
        }

        private void OnDrawGizmosSelected()
        {
            // グラインド検出範囲
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, grindDetectionRadius);
            
            // 壁キック検出範囲
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * wallKickRadius);
        }

        private void OnGUI()
        {
            if (showDebugInfo)
            {
                GUI.Label(new Rect(10, 10, 200, 30), $"Speed: {currentSpeed:F1}");
                GUI.Label(new Rect(10, 40, 200, 30), $"State: {currentState}");
                GUI.Label(new Rect(10, 70, 200, 30), $"Energy: {currentEnergy:F0}/{dreamFlightEnergy:F0}");
                
                if (isDreamFlying)
                {
                    GUI.Label(new Rect(10, 100, 200, 30), $"Dream Flight: {currentDreamFlightSpeed:F1}");
                }
            }
        }
        #endregion
    }
} 