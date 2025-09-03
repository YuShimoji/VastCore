using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    #region 移動パラメータ
    [Header("基本移動")]
    [Tooltip("プレイヤーに加える力の大きさ")]
    public float moveForce = 70f;
    [Tooltip("プレイヤーの最高速度")]
    public float maxSpeed = 15f;
    [Tooltip("空中でのコントロールのしやすさ（0に近いほど操作不能）")]
    [Range(0.0f, 1.0f)]
    public float airControlFactor = 0.5f;
    #endregion

    #region スプリント
    [Header("スプリント")]
    [Tooltip("スプリント時の最高速度")]
    public float sprintMaxSpeed = 25f;
    [Tooltip("スプリントの持続時間（秒）")]
    public float sprintDuration = 1.5f;
    [Tooltip("スプリント入力キー")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    #endregion

    #region ジャンプ
    [Header("ジャンプ")]
    [Tooltip("ジャンプの強さ")]
    public float jumpForce = 8f;
    [Tooltip("ジャンプ入力キー")]
    public KeyCode jumpKey = KeyCode.Space;
    #endregion
    
    #region 接地判定
    [Header("接地判定")]
    [Tooltip("地面とみなすレイヤー")]
    public LayerMask groundLayer;
    [Tooltip("接地判定の球体の半径")]
    public float groundCheckRadius = 0.4f;
    [Tooltip("接地判定の球体をキャストする距離")]
    public float groundCheckDistance = 0.2f;
    #endregion

    #region コヨーテタイム
    [Header("コヨーテタイム")]
    [Tooltip("地面から離れてもジャンプできる猶予時間")]
    public float coyoteTimeDuration = 0.15f;
    #endregion

    #region 内部変数
    private Rigidbody rb;
    private CapsuleCollider capsule;
    private Vector2 moveInput;
    private bool jumpRequested;
    private Coroutine sprintCoroutine;
    private float coyoteTimeCounter;
    private Camera mainCamera;
    #endregion

    #region 状態プロパティ（デバッグ用）
    [Header("デバッグ情報")]
    [SerializeField, Tooltip("接地状態")]
    private bool isGrounded;
    [SerializeField, Tooltip("現在の最高速度")]
    private float currentMaxSpeed;
    #endregion

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        currentMaxSpeed = maxSpeed;

        // Rigidbodyの不要な回転・移動を制限し、Unity標準の重力を確実に有効化する
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = true;

        // レイヤーマスクが未設定の場合、警告を出す
        if (groundLayer.value == 0)
        {
            Debug.LogWarning("Ground Layerが設定されていません。PlayerのInspectorから設定してください。", this);
        }

        // パフォーマンス向上のため、メインカメラをキャッシュ
        mainCamera = Camera.main;
    }

    void Update()
    {
        // 入力受付
        moveInput.x = Input.GetAxis("Horizontal");
        moveInput.y = Input.GetAxis("Vertical");

        if (Input.GetKeyDown(jumpKey))
        {
            jumpRequested = true;
        }

        if (Input.GetKeyDown(sprintKey))
        {
            TrySprint();
        }
    }

    void FixedUpdate()
    {
        // 1. 接地判定
        CheckGrounded();

        // コヨーテタイムのカウンターを更新
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTimeDuration;
        }
        else
        {
            coyoteTimeCounter -= Time.fixedDeltaTime;
        }

        // 2. 移動処理
        Vector3 controlDirection = GetControlDirection();
        if (controlDirection.sqrMagnitude > 0.1f)
        {
            float forceMultiplier = isGrounded ? 1.0f : airControlFactor;
            rb.AddForce(controlDirection * moveForce * forceMultiplier);

            // キャラクターを移動方向へスムーズに向ける
            Quaternion targetRotation = Quaternion.LookRotation(controlDirection, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 10f));
        }

        // 3. ジャンプ処理
        HandleJump();
        
        // 4. 速度制限
        LimitVelocity();
    }

    private Vector3 GetControlDirection()
    {
        if (mainCamera == null) return Vector3.zero;

        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;
        cameraForward.y = 0;
        cameraRight.y = 0;
        return (cameraForward.normalized * moveInput.y + cameraRight.normalized * moveInput.x).normalized;
        }

    private void HandleJump()
    {
        // 接地しているか、またはコヨーテタイム中であればジャンプ可能
        if (jumpRequested && coyoteTimeCounter > 0f)
        {
            // Y軸の速度を一度リセットしてから力を加える
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

            // ジャンプしたらコヨーテタイムは即時終了
            coyoteTimeCounter = 0f;
        }
        jumpRequested = false;
    }

    private void TrySprint()
    {
        if (sprintCoroutine != null)
        {
            StopCoroutine(sprintCoroutine);
        }
        sprintCoroutine = StartCoroutine(SprintCoroutine());
    }

    private IEnumerator SprintCoroutine()
    {
        currentMaxSpeed = sprintMaxSpeed;
        yield return new WaitForSeconds(sprintDuration);
        currentMaxSpeed = maxSpeed;
        sprintCoroutine = null;
    }
    
    private void LimitVelocity()
        {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        if (horizontalVelocity.magnitude > currentMaxSpeed)
        {
            Vector3 limitedVel = horizontalVelocity.normalized * currentMaxSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }
    }
    
    private void CheckGrounded()
    {
        Vector3 capsuleBottom = transform.position + capsule.center - Vector3.up * (capsule.height / 2 - groundCheckRadius);
        isGrounded = Physics.SphereCast(capsuleBottom, groundCheckRadius, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
    }
    
    // デバッグ用にギズモを描画
    private void OnDrawGizmosSelected()
    {
        if (capsule == null) capsule = GetComponent<CapsuleCollider>();
        Vector3 capsuleBottom = transform.position + capsule.center - Vector3.up * (capsule.height / 2 - groundCheckRadius);
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(capsuleBottom + Vector3.down * groundCheckDistance, groundCheckRadius);
    }
}
