using UnityEngine;

namespace Vastcore.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class AdvancedPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 10f;
        public float gravity = -9.81f;

        [Header("Looking")]
        public float lookSpeed = 2f;
        public float lookXLimit = 45.0f;
        public Transform cameraTransform;

        [Header("Gliding")]
        public float glideGravity = -2f;
        public float glideForwardForce = 5f;

        [Header("Dashing")]
        public float dashSpeed = 30f;
        public float dashDuration = 0.2f;
        public float dashCooldown = 1f;

        [Header("Translocation")]
        public GameObject translocationSpherePrefab;
        public float sphereLaunchForce = 50f;

        private CharacterController characterController;
        private Vector3 velocity;
        private float rotationX = 0;

        private bool isGliding = false;
        private bool isDashing = false;
        private float dashTimer = 0f;
        private float dashCooldownTimer = 0f;

        private void Start()
        {
            characterController = GetComponent<CharacterController>();

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
            HandleDashing();
            HandleTranslocation();
        }

        private void HandleCooldowns()
        {
            if (dashCooldownTimer > 0)
            {
                dashCooldownTimer -= Time.deltaTime;
            }
        }

        private void HandleMovement()
        {
            float currentSpeed = moveSpeed;
            Vector3 moveDirection;

            if (isDashing)
            {
                // Dash logic
                currentSpeed = dashSpeed;
                moveDirection = transform.forward; // For simplicity, dash is always forward for now
                dashTimer -= Time.deltaTime;
                if (dashTimer <= 0)
                {
                    isDashing = false;
                }
            }
            else
            {
                // Basic WASD movement
                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");
                moveDirection = transform.right * horizontal + transform.forward * vertical;
            }

            characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

            // Gravity
            if (isGliding)
            {
                velocity.y += glideGravity * Time.deltaTime;
                characterController.Move(transform.forward * glideForwardForce * Time.deltaTime);
            }
            else
            {
                if (characterController.isGrounded && velocity.y < 0)
                {
                    velocity.y = -2f;
                }
                velocity.y += gravity * Time.deltaTime;
            }

            characterController.Move(velocity * Time.deltaTime);
        }

        private void HandleLooking()
        {
            // Mouse look
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            cameraTransform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }

        private void HandleGliding()
        {
            // Check for input (holding space while not on the ground)
            if (Input.GetKey(KeyCode.Space) && !characterController.isGrounded)
            {
                isGliding = true;
            }
            else
            {
                isGliding = false;
            }
        }

        private void HandleDashing()
        {
            // Check for input and cooldown
            if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing && dashCooldownTimer <= 0)
            {
                isDashing = true;
                dashTimer = dashDuration;
                dashCooldownTimer = dashCooldown;
            }
        }

        private void HandleTranslocation()
        {
            // TODO: Implement translocation sphere logic
            // - Check for input (e.g., right mouse button)
            // - Instantiate and launch a sphere prefab
            // - On sphere collision, teleport the player
            if (Input.GetMouseButtonDown(1)) // 1 is for the right mouse button
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
            Rigidbody rb = sphere.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(cameraTransform.forward * sphereLaunchForce, ForceMode.Impulse);
            }
        }

        private void TeleportPlayer(Vector3 targetPosition)
        {
            // Temporarily disable the character controller to teleport
            characterController.enabled = false;
            transform.position = targetPosition;
            characterController.enabled = true;

            // Reset vertical velocity to avoid carrying over momentum
            velocity.y = 0;
        }
    }
} 