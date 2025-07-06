using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("設定")]
    [Tooltip("マウス感度")]
    public float mouseSensitivity = 1000f;

    [Tooltip("追従対象のプレイヤー")]
    public Transform playerBody;

    private float xRotation = 0f;
    private float yRotation = 0f;

    void Start()
    {
        // ゲーム中はカーソルを画面中央にロックして非表示にする
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerBody == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                playerBody = player.transform;
            }
            else
            {
                Debug.LogError("CameraController could not find an object with the PlayerController script.", this);
            }
        }
    }

    void LateUpdate()
    {
        if (playerBody == null) return;

        // マウスの入力を取得
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Y軸の回転（左右の視点移動）
        yRotation += mouseX;
        // X軸の回転（上下の視点移動）
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -40f, 80f); // 見上げ/見下ろし角度を制限

        // カメラの回転を更新
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        
        // カメラの位置をプレイヤーの少し後ろに更新
        // （注：将来的には壁の裏に隠れないような処理を追加する）
        Vector3 targetPosition = playerBody.position - transform.forward * 5f + Vector3.up * 2f;
        transform.position = targetPosition;
    }
}
