using UnityEngine;

namespace Vastcore.Player
{
    public class TranslocationSphere : MonoBehaviour
    {
        public float lifeTime = 5f;
        
        // このイベントを使って、着弾位置をPlayerControllerに通知する
        public static event System.Action<Vector3> OnSphereCollision;

        private void Start()
        {
            // 指定時間後に自動で消滅する
            Destroy(gameObject, lifeTime);
        }

        private void OnCollisionEnter(Collision collision)
        {
            // 衝突点を取得
            Vector3 collisionPoint = collision.contacts[0].point;

            // プレイヤーに衝突位置を通知
            OnSphereCollision?.Invoke(collisionPoint);

            // 着弾したら自身を破棄する
            Destroy(gameObject);
        }
    }
} 