using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class SimplePlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5.0f;
    public float sprintSpeed = 8.0f;
    public float jumpHeight = 2.0f;

    [Header("Camera Control")]
    public float mouseSensitivity = 2.0f;

    [Header("Physics")]
    public float gravity = -9.81f;

    private CharacterController characterController;
    private Vector3 velocity;
    private float verticalRotation = 0f;
    private Transform cameraTransform;

    private const float VERTICAL_ANGLE_MIN = -90f;
    private const float VERTICAL_ANGLE_MAX = 90f;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        SetupCursor();
        FindMainCamera();
    }

    void Update()
    {
        HandleMovement();
        HandleLook();
    }

    private void SetupCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void FindMainCamera()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("Main Camera not found. Please ensure a camera is in the scene and tagged as 'MainCamera'.");
        }
    }

    private void HandleMovement()
    {
        // Ground Check
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        // Calculate speed
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;

        // Move
        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

        // Jump
        if (Input.GetButtonDown("Jump") && characterController.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply Gravity
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
    
    private void HandleLook()
    {
        if (cameraTransform == null) return;
        
        // Input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Player Body (Y-axis)
        transform.Rotate(Vector3.up * mouseX);

        // Camera (X-axis)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, VERTICAL_ANGLE_MIN, VERTICAL_ANGLE_MAX);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }
}
