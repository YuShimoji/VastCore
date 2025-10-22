using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// プリミティブ地形オブジェクトの基本クラス
    /// プール管理される地形要素の共通インターフェースを提供
    /// </summary>
    public class PrimitiveTerrainObject : MonoBehaviour
    {
        [Header("プリミティブ設定")]
        public GenerationPrimitiveType primitiveType;
        public bool isClimbable = false;
        public float scale = 1f;
        public Vector3 offset = Vector3.zero;

        [Header("物理設定")]
        public bool usePhysics = true;
        public float mass = 1f;
        public PhysicMaterial physicsMaterial;

        [Header("ビジュアル設定")]
        public Material material;
        public Color tintColor = Color.white;

        // プール管理用
        private bool isPooled = false;
        private Vector3 originalScale;
        private Material originalMaterial;

        void Awake()
        {
            InitializePrimitive();
        }

        /// <summary>
        /// プリミティブを初期化
        /// </summary>
        private void InitializePrimitive()
        {
            // 物理コンポーネントの設定
            if (usePhysics)
            {
                var rigidbody = GetComponent<Rigidbody>();
                if (rigidbody == null)
                {
                    rigidbody = gameObject.AddComponent<Rigidbody>();
                }
                rigidbody.mass = mass;
                rigidbody.isKinematic = false;

                var collider = GetComponent<Collider>();
                if (collider != null && physicsMaterial != null)
                {
                    collider.material = physicsMaterial;
                }
            }

            // マテリアルの設定
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                originalMaterial = renderer.material;
                if (material != null)
                {
                    renderer.material = material;
                }
                renderer.material.color = tintColor;
            }

            originalScale = transform.localScale;
        }

        /// <summary>
        /// プールから初期化
        /// </summary>
        public void InitializeFromPool(GenerationPrimitiveType type, Vector3 position, float objectScale)
        {
            primitiveType = type;
            transform.position = position;
            scale = objectScale;
            transform.localScale = originalScale * scale;
            gameObject.SetActive(true);
            isPooled = true;

            // 位置オフセットの適用
            transform.position += offset;

            // タイプ固有の初期化
            OnPrimitiveTypeSet();
        }

        /// <summary>
        /// プールに戻す準備
        /// </summary>
        public void PrepareForPool()
        {
            gameObject.SetActive(false);
            transform.localScale = originalScale;
            isPooled = false;

            // Rigidbodyの停止
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            OnReturnToPool();
        }

        /// <summary>
        /// プリミティブタイプが設定された時の処理
        /// </summary>
        protected virtual void OnPrimitiveTypeSet()
        {
            // サブクラスでオーバーライド可能
            switch (primitiveType)
            {
                case GenerationPrimitiveType.Cube:
                    // キューブ固有の設定
                    isClimbable = true;
                    break;
                case GenerationPrimitiveType.Sphere:
                    // 球体固有の設定
                    isClimbable = false;
                    break;
                case GenerationPrimitiveType.Cylinder:
                    // 円柱固有の設定
                    isClimbable = true;
                    break;
                case GenerationPrimitiveType.Capsule:
                    // カプセル固有の設定
                    isClimbable = true;
                    break;
            }
        }

        /// <summary>
        /// プールに戻る時の処理
        /// </summary>
        protected virtual void OnReturnToPool()
        {
            // サブクラスでオーバーライド可能
        }

        /// <summary>
        /// プリミティブの更新処理
        /// </summary>
        protected virtual void Update()
        {
            // プール状態での追加処理
        }

        /// <summary>
        /// プールされているかどうか
        /// </summary>
        public bool IsPooled => isPooled;
    }

    /// <summary>
    /// プリミティブ地形オブジェクトのタイプ
    /// </summary>
    public enum GenerationPrimitiveType
    {
        Cube = 0,
        Sphere = 1,
        Cylinder = 2,
        Capsule = 3,
        Plane = 4
    }
}
