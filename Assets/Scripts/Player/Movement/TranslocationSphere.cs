using UnityEngine;

namespace Vastcore.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class TranslocationSphere : MonoBehaviour
    {
        [Header("基本設定")]
        public float lifeTime = 5f;
        public float bounceForce = 0.7f;
        public int maxBounces = 3;
        
        [Header("視覚効果")]
        public float glowIntensity = 2f;
        public Color sphereColor = Color.cyan;
        public bool showTrajectory = true;
        public float trajectoryWidth = 0.1f;
        
        [Header("着弾予測")]
        public GameObject landingPreviewPrefab;
        public LayerMask groundLayer = -1;
        
        // このイベントを使って、着弾位置をPlayerControllerに通知する
        public static event System.Action<Vector3> OnSphereCollision;
        
        private Rigidbody rb;
        private SphereCollider sphereCollider;
        private TrailRenderer trailRenderer;
        private Light sphereLight;
        private int bounceCount = 0;
        private GameObject landingPreview;
        private bool hasLanded = false;
        
        // 軌道予測
        private Vector3[] trajectoryPoints;
        private LineRenderer trajectoryLine;

        #region Unity生命周期
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            sphereCollider = GetComponent<SphereCollider>();
            SetupVisualEffects();
            SetupTrajectoryPrediction();
        }

        private void Start()
        {
            // 指定時間後に自動で消滅する
            Destroy(gameObject, lifeTime);
            
            // 着弾予測を開始
            StartTrajectoryPrediction();
        }

        private void Update()
        {
            UpdateTrajectoryPrediction();
            UpdateVisualEffects();
        }

        private void OnDestroy()
        {
            // 着弾予測オブジェクトがある場合は削除
            if (landingPreview != null)
            {
                Destroy(landingPreview);
            }
        }
        #endregion

        #region 視覚効果設定
        private void SetupVisualEffects()
        {
            // トレイルレンダラーの設定
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
            trailRenderer.time = 1f;
            trailRenderer.startWidth = trajectoryWidth;
            trailRenderer.endWidth = 0.05f;
            trailRenderer.material = CreateTrailMaterial();
            trailRenderer.startColor = sphereColor;
            trailRenderer.endColor = new Color(sphereColor.r, sphereColor.g, sphereColor.b, 0);

            // 球体のライト効果
            sphereLight = gameObject.AddComponent<Light>();
            sphereLight.type = LightType.Point;
            sphereLight.color = sphereColor;
            sphereLight.intensity = glowIntensity;
            sphereLight.range = 5f;
            sphereLight.shadows = LightShadows.None;

            // 球体の見た目設定
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = sphereColor;
                mat.SetFloat("_Metallic", 0.5f);
                mat.SetFloat("_Smoothness", 0.8f);
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", sphereColor * glowIntensity);
                renderer.material = mat;
            }
        }

        private Material CreateTrailMaterial()
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = sphereColor;
            return mat;
        }
        #endregion

        #region 軌道予測システム
        private void SetupTrajectoryPrediction()
        {
            if (showTrajectory)
            {
                // LineRendererを作成
                GameObject trajectoryObject = new GameObject("TrajectoryLine");
                trajectoryObject.transform.SetParent(transform);
                
                trajectoryLine = trajectoryObject.AddComponent<LineRenderer>();
                trajectoryLine.material = CreateTrailMaterial();
                Color trajectoryColor = new Color(sphereColor.r, sphereColor.g, sphereColor.b, 0.5f);
                trajectoryLine.startColor = trajectoryColor;
                trajectoryLine.endColor = trajectoryColor;
                trajectoryLine.startWidth = trajectoryWidth * 0.5f;
                trajectoryLine.endWidth = trajectoryWidth * 0.5f;
                trajectoryLine.useWorldSpace = true;
                
                // 軌道予測ポイントを初期化
                trajectoryPoints = new Vector3[50];
            }
        }

        private void StartTrajectoryPrediction()
        {
            if (landingPreviewPrefab != null)
            {
                // 着弾予測地点を計算
                Vector3 predictedLanding = PredictLandingPoint();
                
                // 着弾予測オブジェクトを配置
                landingPreview = Instantiate(landingPreviewPrefab);
                landingPreview.transform.position = predictedLanding;
                
                // 着弾予測の見た目設定
                SetupLandingPreview();
            }
        }

        private void UpdateTrajectoryPrediction()
        {
            if (showTrajectory && trajectoryLine != null && !hasLanded)
            {
                // 現在の軌道を計算
                CalculateTrajectory();
                
                // LineRendererを更新
                trajectoryLine.positionCount = trajectoryPoints.Length;
                trajectoryLine.SetPositions(trajectoryPoints);
            }
        }

        private Vector3 PredictLandingPoint()
        {
            Vector3 velocity = rb.linearVelocity;
            Vector3 position = transform.position;
            Vector3 gravity = Physics.gravity;
            
            // 簡単な物理計算で着弾点を予測
            for (int i = 0; i < 100; i++)
            {
                Vector3 nextPosition = position + velocity * Time.fixedDeltaTime;
                velocity += gravity * Time.fixedDeltaTime;
                
                // 地面との衝突チェック
                RaycastHit hit;
                if (Physics.Raycast(position, nextPosition - position, out hit, 
                    Vector3.Distance(position, nextPosition), groundLayer))
                {
                    return hit.point;
                }
                
                position = nextPosition;
            }
            
            return position;
        }

        private void CalculateTrajectory()
        {
            Vector3 velocity = rb.linearVelocity;
            Vector3 position = transform.position;
            Vector3 gravity = Physics.gravity;
            
            for (int i = 0; i < trajectoryPoints.Length; i++)
            {
                trajectoryPoints[i] = position;
                
                // 次の位置を計算
                position += velocity * Time.fixedDeltaTime * 5f; // 少し時間を進める
                velocity += gravity * Time.fixedDeltaTime * 5f;
                
                // 地面にぶつかったら終了
                if (Physics.Raycast(trajectoryPoints[i], position - trajectoryPoints[i], 
                    Vector3.Distance(trajectoryPoints[i], position), groundLayer))
                {
                    // 残りのポイントを現在位置に設定
                    for (int j = i + 1; j < trajectoryPoints.Length; j++)
                    {
                        trajectoryPoints[j] = trajectoryPoints[i];
                    }
                    break;
                }
            }
        }

        private void SetupLandingPreview()
        {
            if (landingPreview != null)
            {
                // 着弾予測の見た目設定
                Renderer renderer = landingPreview.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Standard"));
                    mat.color = new Color(sphereColor.r, sphereColor.g, sphereColor.b, 0.3f);
                    mat.SetFloat("_Mode", 3); // Transparent mode
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                    renderer.material = mat;
                }

                // 回転アニメーション
                LandingPreviewRotator rotator = landingPreview.AddComponent<LandingPreviewRotator>();
                rotator.rotationSpeed = 50f;
            }
        }
        #endregion

        #region 視覚効果更新
        private void UpdateVisualEffects()
        {
            // ライトの強度を時間と共に変化
            if (sphereLight != null)
            {
                float lifetimeRatio = (lifeTime - (Time.time - Time.fixedTime)) / lifeTime;
                sphereLight.intensity = glowIntensity * (0.5f + 0.5f * Mathf.Sin(Time.time * 5f)) * lifetimeRatio;
            }

            // 球体の回転
            transform.Rotate(Vector3.up * 180f * Time.deltaTime);
        }
        #endregion

        #region 衝突処理
        private void OnCollisionEnter(Collision collision)
        {
            if (hasLanded) return;

            // 衝突点を取得
            Vector3 collisionPoint = collision.contacts[0].point;
            
            // バウンス処理
            if (bounceCount < maxBounces && collision.gameObject.layer != LayerMask.NameToLayer("Player"))
            {
                bounceCount++;
                
                // バウンス力を適用
                Vector3 bounceDirection = Vector3.Reflect(rb.linearVelocity.normalized, collision.contacts[0].normal);
                rb.linearVelocity = bounceDirection * rb.linearVelocity.magnitude * bounceForce;
                
                // バウンス音やエフェクトを追加可能
                CreateBounceEffect(collisionPoint);
                
                // 着弾予測を更新
                if (landingPreview != null)
                {
                    landingPreview.transform.position = PredictLandingPoint();
                }
            }
            else
            {
                // 最終着弾
                hasLanded = true;
                
                // 軌道線を非表示
                if (trajectoryLine != null)
                {
                    trajectoryLine.enabled = false;
                }
                
                // 着弾エフェクト
                CreateLandingEffect(collisionPoint);

            // プレイヤーに衝突位置を通知
            OnSphereCollision?.Invoke(collisionPoint);

            // 着弾したら自身を破棄する
                Destroy(gameObject, 0.5f); // 少し遅延してエフェクトを見せる
            }
        }

        private void CreateBounceEffect(Vector3 position)
        {
            // バウンス時のパーティクル効果（簡易版）
            GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            effect.transform.position = position;
            effect.transform.localScale = Vector3.one * 0.2f;
            
            Renderer renderer = effect.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = sphereColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", sphereColor * 2f);
            renderer.material = mat;
            
            // 自動削除
            Destroy(effect, 1f);
        }

        private void CreateLandingEffect(Vector3 position)
        {
            // 着弾時のより派手なエフェクト
            for (int i = 0; i < 5; i++)
            {
                GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                effect.transform.position = position + Random.insideUnitSphere;
                effect.transform.localScale = Vector3.one * Random.Range(0.1f, 0.3f);
                
                Renderer renderer = effect.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = sphereColor;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", sphereColor * 3f);
                renderer.material = mat;
                
                // ランダムな方向に飛ばす
                Rigidbody effectRb = effect.AddComponent<Rigidbody>();
                effectRb.AddForce(Random.insideUnitSphere * 5f, ForceMode.Impulse);
                
                // 自動削除
                Destroy(effect, 2f);
            }
        }
        #endregion
    }

    // 着弾予測オブジェクトの回転コンポーネント
    public class LandingPreviewRotator : MonoBehaviour
    {
        public float rotationSpeed = 50f;
        
        private void Update()
        {
            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        }
    }
} 