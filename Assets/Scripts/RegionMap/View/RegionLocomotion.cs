using UnityEngine;

namespace Vastcore.RegionMap
{
    /// <summary>徒歩・簡易ビークルを共通に扱う移動主体インターフェース。</summary>
    public interface ILocomotionAgent
    {
        Transform Root { get; }
        Vector3 Velocity { get; }
        float CameraDistance { get; }
        float CameraHeightOffset { get; }
        bool IsActive { get; }
        void SetActive(bool active);
        void Teleport(Vector3 localPos, float yawDegrees);
        void ApplyOriginShift(Vector3 localDelta);
    }

    /// <summary>
    /// オープンワールド徒歩移動コントローラ。
    /// CharacterController＋地形標高フォールバックで接地し、RegionTraversal の崖・水域を回避する。
    /// </summary>
    public sealed class RegionFootController : MonoBehaviour, ILocomotionAgent
    {
        public float WalkSpeed = 7.5f;
        public float SprintSpeed = 14f;
        public float JumpSpeed = 7.5f;
        public float Gravity = 22f;

        public Transform Root { get { return transform; } }
        public Vector3 Velocity { get { return _velocity; } }
        public float CameraDistance { get { return 5.5f; } }
        public float CameraHeightOffset { get { return 1.7f; } }
        public bool IsActive { get; private set; }

        private CharacterController _cc;
        private RegionChunkStreamer _streamer;
        private Transform _cameraTransform;
        private Vector3 _velocity;
        private GameObject _visual;

        public void Initialize(RegionChunkStreamer streamer, Transform cameraTransform)
        {
            _streamer = streamer;
            _cameraTransform = cameraTransform;
            _cc = gameObject.AddComponent<CharacterController>();
            _cc.height = 1.8f;
            _cc.radius = 0.38f;
            _cc.center = new Vector3(0f, 0.9f, 0f);
            _cc.slopeLimit = 52f;
            _cc.stepOffset = 0.5f;

            _visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _visual.name = "Walker Body";
            Destroy(_visual.GetComponent<Collider>());
            _visual.transform.SetParent(transform, false);
            _visual.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            _visual.transform.localScale = new Vector3(0.7f, 0.95f, 0.7f);
            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(s) { hideFlags = HideFlags.DontSave };
            m.color = new Color(0.86f, 0.54f, 0.24f);
            _visual.GetComponent<MeshRenderer>().sharedMaterial = m;
        }

        public void SetActive(bool active)
        {
            IsActive = active;
            if (_cc != null) _cc.enabled = active;
            if (_visual != null) _visual.SetActive(active);
            if (!active) _velocity = Vector3.zero;
        }

        public void Teleport(Vector3 localPos, float yawDegrees)
        {
            bool wasEnabled = _cc != null && _cc.enabled;
            if (_cc != null) _cc.enabled = false;
            transform.position = localPos;
            transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
            _velocity = Vector3.zero;
            if (_cc != null) _cc.enabled = wasEnabled;
        }

        public void ApplyOriginShift(Vector3 localDelta)
        {
            bool wasEnabled = _cc != null && _cc.enabled;
            if (_cc != null) _cc.enabled = false;
            transform.position += localDelta;
            if (_cc != null) _cc.enabled = wasEnabled;
        }

        private void Update()
        {
            if (!IsActive || _cc == null || _streamer == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 moveDir = Vector3.zero;
            if (_cameraTransform != null)
            {
                Vector3 fwd = _cameraTransform.forward;
                fwd.y = 0f;
                fwd.Normalize();
                Vector3 right = _cameraTransform.right;
                right.y = 0f;
                right.Normalize();
                moveDir = fwd * v + right * h;
            }
            else
            {
                moveDir = new Vector3(h, 0f, v);
            }
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

            float speed = Input.GetKey(KeyCode.LeftShift) ? SprintSpeed : WalkSpeed;
            Vector3 horiz = moveDir * speed;

            // 進行先の RegionTraversal 区分で崖・水域への進入を抑制
            if (horiz.sqrMagnitude > 0.01f && _streamer.Coordinates != null)
            {
                Vector3 probe = transform.position + moveDir * 1.4f;
                _streamer.Coordinates.LocalToWorld(probe.x, probe.y, probe.z,
                    out double pe, out _, out double ps);
                RegionGround ground = _streamer.IsTargetNearReady(pe, ps) ? _streamer.SampleGroundType(pe, ps) : RegionGround.Cliff;
                float mult = RegionLocomotionMath.SpeedMultiplier(ground, isVehicle: false);
                horiz *= mult;
            }

            if (horiz.sqrMagnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(horiz.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Min(1f, dt * 10f));
            }

            bool grounded = _cc.isGrounded;
            if (grounded && _velocity.y < 0f) _velocity.y = -2f;
            if (grounded && Input.GetKeyDown(KeyCode.Space)) _velocity.y = JumpSpeed;
            _velocity.y -= Gravity * dt;

            _velocity.x = horiz.x;
            _velocity.z = horiz.z;
            _cc.Move(_velocity * dt);

            // Collider 未ロード時の落下防止（地形標高フォールバック）
            if (_streamer.Coordinates != null)
            {
                _streamer.Coordinates.LocalToWorld(transform.position.x, transform.position.y, transform.position.z,
                    out double we, out _, out double ws);
                float groundY = _streamer.SampleGroundHeight(we, ws);
                if (!_streamer.IsTargetNearReady(we, ws) && transform.position.y < groundY - 0.1f)
                {
                    _cc.enabled = false;
                    transform.position = new Vector3(transform.position.x, groundY + 0.05f, transform.position.z);
                    _cc.enabled = true;
                    if (_velocity.y < 0f) _velocity.y = 0f;
                }
            }
        }
    }

    /// <summary>
    /// 推奨簡易ビークル：4輪 Raycast サスペンション・バギー。
    /// 起伏に応じて自然に傾き・バウンドし、RegionTraversal の傾斜/水域減速と R キー横転復帰を備える。
    /// </summary>
    public sealed class RegionBuggyController : MonoBehaviour, ILocomotionAgent
    {
        public float MaxSpeedMps = 34f;
        public float MotorForce = 24000f;
        public float BrakeForce = 18000f;
        public float MaxSteerDegrees = 28f;
        public float SuspensionRestDist = 0.85f;
        public float SpringStrength = 28000f;
        public float SpringDamper = 3400f;
        public float TireGripFactor = 6.5f;

        public Transform Root { get { return transform; } }
        public Vector3 Velocity { get { return _rb != null ? _rb.linearVelocity : Vector3.zero; } }
        public float CameraDistance { get { return 10.5f; } }
        public float CameraHeightOffset { get { return 2.8f; } }
        public bool IsActive { get; private set; }
        public bool IsSpawned { get { return gameObject.activeSelf; } }

        private Rigidbody _rb;
        private RegionChunkStreamer _streamer;
        private Transform[] _wheelAnchors;
        private Transform[] _wheelVisuals;
        private float _steerInput;
        private float _throttleInput;
        private bool _brakeInput;
        private RegionWorldPoint? _recoveryPoint;

        public void Initialize(RegionChunkStreamer streamer)
        {
            _streamer = streamer;
            _rb = gameObject.AddComponent<Rigidbody>();
            _rb.mass = 1150f;
            _rb.linearDamping = 0.18f;
            _rb.angularDamping = 1.6f;
            _rb.centerOfMass = new Vector3(0f, -0.35f, 0f);
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

            var box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(2.1f, 0.85f, 3.8f);
            box.center = new Vector3(0f, 0.55f, 0f);

            BuildBuggyVisuals();
        }

        public void SetActive(bool active)
        {
            IsActive = active;
            _rb.constraints = active ? RigidbodyConstraints.None :
                RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationY;
            if (!active)
            {
                _throttleInput = 0f;
                _steerInput = 0f;
                _brakeInput = true;
            }
        }

        public void Teleport(Vector3 localPos, float yawDegrees)
        {
            gameObject.SetActive(true);
            _rb.position = localPos + Vector3.up * 0.6f;
            _rb.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(_rb.position, _rb.rotation);
            _streamer.Coordinates.LocalToWorld(localPos.x, localPos.y, localPos.z, out double east, out _, out double south);
            _recoveryPoint = new RegionWorldPoint(east, localPos.y, south);
        }

        public void ApplyOriginShift(Vector3 localDelta)
        {
            if (_rb != null)
                _rb.position += localDelta;
            else
                transform.position += localDelta;
        }

        private void Update()
        {
            if (!IsActive) return;
            _steerInput = Input.GetAxis("Horizontal");
            _throttleInput = Input.GetAxis("Vertical");
            _brakeInput = Input.GetKey(KeyCode.Space);

            // R キーまたは横転・スタック時のリカバー
            if (Input.GetKeyDown(KeyCode.R))
                RecoverUpright();
        }

        public void RecoverUpright()
        {
            if (!_recoveryPoint.HasValue || _streamer.Coordinates == null) return;
            RegionWorldPoint point = _recoveryPoint.Value;
            if (!_streamer.IsTargetNearReady(point.EastM, point.SouthM)) return;
            Vector3 fwd = transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            float yaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
            float height = _streamer.SampleGroundHeight(point.EastM, point.SouthM);
            _streamer.Coordinates.WorldToLocal(point.EastM, height, point.SouthM, out float x, out float y, out float z);
            Teleport(new Vector3(x, y, z), yaw);
        }

        private void FixedUpdate()
        {
            if (_rb == null || _streamer == null || _streamer.Coordinates == null || _wheelAnchors == null) return;

            // 地表区分による走行係数
            float groundMult = 1f;
            bool brake = _brakeInput || !IsActive;
            {
                Vector3 position = _rb.position;
                _streamer.Coordinates.LocalToWorld(position.x, position.y, position.z,
                    out double we, out _, out double ws);
                if (!_streamer.Coordinates.ContainsWorld(we, ws))
                {
                    RecoverUpright();
                    return;
                }
                RegionGround ground = _streamer.SampleGroundType(we, ws);
                groundMult = RegionLocomotionMath.SpeedMultiplier(ground, isVehicle: true);
                float terrainY = _streamer.SampleGroundHeight(we, ws);
                bool loaded = _streamer.IsTargetNearReady(we, ws);
                if (loaded && (ground == RegionGround.Walk || ground == RegionGround.Steep) &&
                    Vector3.Dot(transform.up, Vector3.up) > 0.65f && Mathf.Abs(position.y - terrainY) < 1.5f)
                    _recoveryPoint = new RegionWorldPoint(we, terrainY, ws);

                Vector3 velocity = Vector3.ProjectOnPlane(_rb.linearVelocity, Vector3.up);
                Vector3 direction = velocity.sqrMagnitude > 1f ? velocity.normalized :
                    Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized * Mathf.Sign(_throttleInput);
                float deceleration = Mathf.Max(1f, BrakeForce / _rb.mass * 0.6f);
                float brakingDistance = 2.2f + velocity.sqrMagnitude / (2f * deceleration);
                brake |= !loaded || groundMult <= 0f || MustBrakeForTerrain(position, direction, brakingDistance);

                // Collider が無い一時的な間だけ、地形標高で落下を止める。
                if (!loaded && position.y < terrainY + 0.1f)
                {
                    _rb.position = new Vector3(_rb.position.x, terrainY + 0.35f, _rb.position.z);
                    if (_rb.linearVelocity.y < 0f)
                        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
                }
            }

            float steerAngle = _steerInput * MaxSteerDegrees;
            for (int i = 0; i < 4; i++)
            {
                Transform anchor = _wheelAnchors[i];
                bool isFront = i < 2;
                if (isFront)
                    anchor.localRotation = Quaternion.Euler(0f, steerAngle, 0f);

                Vector3 origin = anchor.position;
                Vector3 down = -transform.up;
                float rayDist = SuspensionRestDist + 0.45f;

                if (Physics.Raycast(origin, down, out RaycastHit hit, rayDist))
                {
                    Vector3 wheelVel = _rb.GetPointVelocity(origin);
                    float offset = SuspensionRestDist - hit.distance;
                    float velAlongSpring = Vector3.Dot(transform.up, wheelVel);
                    float springForce = Mathf.Max(0f, offset * SpringStrength - velAlongSpring * SpringDamper);
                    _rb.AddForceAtPosition(transform.up * springForce, origin);

                    // 横方向グリップ
                    Vector3 sideDir = anchor.right;
                    float sideVel = Vector3.Dot(sideDir, wheelVel);
                    float sideForce = -sideVel * TireGripFactor * (_rb.mass * 0.25f);
                    _rb.AddForceAtPosition(sideDir * sideForce, origin);

                    // 駆動・ブレーキ
                    Vector3 fwdDir = anchor.forward;
                    float fwdSpeed = Vector3.Dot(fwdDir, wheelVel);
                    if (!brake && Mathf.Abs(_throttleInput) > 0.01f)
                    {
                        float effectiveMax = MaxSpeedMps * groundMult;
                        if (Mathf.Abs(fwdSpeed) < effectiveMax || Mathf.Sign(_throttleInput) != Mathf.Sign(fwdSpeed))
                        {
                            float drive = _throttleInput * (MotorForce * 0.25f) * groundMult;
                            _rb.AddForceAtPosition(fwdDir * drive, origin);
                        }
                    }
                    else if (brake)
                    {
                        float brakeForce = -Mathf.Clamp(fwdSpeed, -1f, 1f) * (BrakeForce * 0.25f);
                        _rb.AddForceAtPosition(fwdDir * brakeForce, origin);
                    }

                    if (_wheelVisuals[i] != null)
                        _wheelVisuals[i].localPosition = new Vector3(0f, -(hit.distance - 0.38f), 0f);
                }
                else if (_wheelVisuals[i] != null)
                {
                    _wheelVisuals[i].localPosition = new Vector3(0f, -(SuspensionRestDist - 0.38f), 0f);
                }
            }
        }

        private bool MustBrakeForTerrain(Vector3 position, Vector3 direction, float meters)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(meters / 4f));
            for (int step = 1; step <= steps; step++)
            {
                Vector3 probe = position + direction * (meters * step / steps);
                _streamer.Coordinates.LocalToWorld(probe.x, probe.y, probe.z, out double east, out _, out double south);
                if (!_streamer.IsTargetNearReady(east, south) || !RegionLocomotionMath.CanStand(_streamer.SampleGroundType(east, south)))
                    return true;
            }
            return false;
        }

        private void BuildBuggyVisuals()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var bodyMat = new Material(s) { hideFlags = HideFlags.DontSave };
            bodyMat.color = new Color(0.84f, 0.36f, 0.14f);
            var frameMat = new Material(s) { hideFlags = HideFlags.DontSave };
            frameMat.color = new Color(0.16f, 0.18f, 0.21f);
            var tireMat = new Material(s) { hideFlags = HideFlags.DontSave };
            tireMat.color = new Color(0.10f, 0.10f, 0.11f);

            var chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "Chassis";
            Destroy(chassis.GetComponent<Collider>());
            chassis.transform.SetParent(transform, false);
            chassis.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            chassis.transform.localScale = new Vector3(1.9f, 0.55f, 3.6f);
            chassis.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;

            var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.name = "Rollcage";
            Destroy(cabin.GetComponent<Collider>());
            cabin.transform.SetParent(transform, false);
            cabin.transform.localPosition = new Vector3(0f, 1.15f, -0.25f);
            cabin.transform.localScale = new Vector3(1.55f, 0.68f, 1.65f);
            cabin.GetComponent<MeshRenderer>().sharedMaterial = frameMat;

            Vector3[] offsets =
            {
                new Vector3(-1.05f, 0.45f, 1.35f),  // FL
                new Vector3(1.05f, 0.45f, 1.35f),   // FR
                new Vector3(-1.05f, 0.45f, -1.35f), // RL
                new Vector3(1.05f, 0.45f, -1.35f),  // RR
            };
            _wheelAnchors = new Transform[4];
            _wheelVisuals = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                var anchor = new GameObject("WheelAnchor " + i);
                anchor.transform.SetParent(transform, false);
                anchor.transform.localPosition = offsets[i];
                _wheelAnchors[i] = anchor.transform;

                var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Wheel " + i;
                Destroy(wheel.GetComponent<Collider>());
                wheel.transform.SetParent(anchor.transform, false);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheel.transform.localScale = new Vector3(0.76f, 0.24f, 0.76f);
                wheel.GetComponent<MeshRenderer>().sharedMaterial = tireMat;
                _wheelVisuals[i] = wheel.transform;
            }
        }
    }

    /// <summary>SphereCast 地形貫通防止付き 3 人称追従カメラ。</summary>
    public sealed class RegionOpenWorldCamera : MonoBehaviour
    {
        public float YawDegrees;
        public float PitchDegrees = 18f;
        public float Distance = 6f;
        public float Sensitivity = 2.4f;
        public bool Active;

        private ILocomotionAgent _target;

        public void SetTarget(ILocomotionAgent target, float? yaw = null, float? pitch = null)
        {
            _target = target;
            if (target != null) Distance = target.CameraDistance;
            if (yaw.HasValue) YawDegrees = yaw.Value;
            if (pitch.HasValue) PitchDegrees = Mathf.Clamp(pitch.Value, -15f, 75f);
        }

        private void LateUpdate()
        {
            if (!Active || _target == null || _target.Root == null) return;

            if (Input.GetMouseButton(1))
            {
                YawDegrees += Input.GetAxis("Mouse X") * Sensitivity * 2.5f;
                PitchDegrees = Mathf.Clamp(PitchDegrees - Input.GetAxis("Mouse Y") * Sensitivity * 2.0f, -10f, 78f);
            }
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
                Distance = Mathf.Clamp(Distance - scroll * 1.2f, 2.5f, 26f);

            SnapToTarget();
        }

        internal void SnapToTarget()
        {
            Pose pose = FollowPose(_target.Root.position, _target.CameraHeightOffset, Distance, YawDegrees, PitchDegrees);
            transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        internal static Pose FollowPose(Vector3 position, float height, float distance, float yaw, float pitch)
        {
            Vector3 pivot = position + Vector3.up * height;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = rot * new Vector3(0f, 0f, -distance);
            Vector3 desiredPos = pivot + offset;

            // SphereCast で地形・岩へのカメラ貫通を防止
            Vector3 dir = desiredPos - pivot;
            float dist = dir.magnitude;
            if (dist > 0.1f && Physics.SphereCast(pivot, 0.35f, dir / dist, out RaycastHit hit, dist,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                desiredPos = pivot + (dir / dist) * Mathf.Max(0.8f, hit.distance - 0.25f);
            }

            return new Pose(desiredPos, rot);
        }
    }

    /// <summary>徒歩（RegionFootController）と簡易ビークル（RegionBuggyController）の乗降・呼び出しを管理する。</summary>
    public sealed class RegionLocomotionManager : MonoBehaviour
    {
        public float MountRangeMeters = 6f;

        public LocomotionMode Mode { get; private set; } = LocomotionMode.OnFoot;
        public RegionFootController Foot { get; private set; }
        public RegionBuggyController Buggy { get; private set; }
        public ILocomotionAgent ActiveAgent { get { return Mode == LocomotionMode.Vehicle ? (ILocomotionAgent)Buggy : Foot; } }
        public bool IsEnabled { get; private set; }
        internal event System.Action<string> OperationRejected;

        private RegionChunkStreamer _streamer;
        private RegionOpenWorldCamera _followCamera;

        public void Initialize(RegionChunkStreamer streamer, RegionOpenWorldCamera followCamera)
        {
            _streamer = streamer;
            _followCamera = followCamera;

            var footGo = new GameObject("OpenWorld Walker");
            footGo.transform.SetParent(transform, false);
            Foot = footGo.AddComponent<RegionFootController>();
            Foot.Initialize(streamer, followCamera != null ? followCamera.transform : null);
            Foot.SetActive(false);

            var buggyGo = new GameObject("OpenWorld Buggy");
            buggyGo.transform.SetParent(transform, false);
            Buggy = buggyGo.AddComponent<RegionBuggyController>();
            Buggy.Initialize(streamer);
            Buggy.SetActive(false);
            buggyGo.SetActive(false);
        }

        public void EnterOpenWorld(Vector3 localPos, float yawDegrees)
        {
            IsEnabled = true;
            Mode = LocomotionMode.OnFoot;
            Foot.Teleport(localPos, yawDegrees);
            Foot.SetActive(true);
            Buggy.SetActive(false);
            if (_followCamera != null)
            {
                _followCamera.SetTarget(Foot, yawDegrees, 18f);
                _followCamera.SnapToTarget();
                _followCamera.Active = true;
            }
        }

        public void ExitOpenWorld()
        {
            IsEnabled = false;
            if (Foot != null) Foot.SetActive(false);
            if (Buggy != null) Buggy.SetActive(false);
            if (_followCamera != null) _followCamera.Active = false;
        }

        internal void ResetWorld()
        {
            ExitOpenWorld();
            Buggy.gameObject.SetActive(false);
            Mode = LocomotionMode.OnFoot;
        }

        public bool SummonBuggyNearby()
        {
            if (!IsEnabled || Mode != LocomotionMode.OnFoot || _streamer == null || _streamer.Coordinates == null) return false;
            Vector3 refPos = ActiveAgent.Root.position;
            _streamer.Coordinates.LocalToWorld(refPos.x, refPos.y, refPos.z,
                out double we, out _, out double ws);

            if (!RegionLocomotionMath.FindNearestStandable(we, ws, 5f, 30f, 3f,
                    LoadedGround, out double be, out double bs))
                return Reject("近くにバギーを呼び出せる地面がありません。");
            float gy = _streamer.SampleGroundHeight(be, bs);
            _streamer.Coordinates.WorldToLocal(be, gy, bs, out float lx, out float ly, out float lz);
            Buggy.Teleport(new Vector3(lx, ly, lz), ActiveAgent.Root.eulerAngles.y);
            return true;
        }

        public bool ToggleMountVehicle()
        {
            if (!IsEnabled) return false;
            if (Mode == LocomotionMode.OnFoot)
            {
                if (!Buggy.IsSpawned && !SummonBuggyNearby()) return false;
                float dist = Vector3.Distance(Foot.Root.position, Buggy.Root.position);
                if (dist > MountRangeMeters) return Reject("バギーに近づくと乗車できます。");

                Foot.SetActive(false);
                Buggy.SetActive(true);
                Mode = LocomotionMode.Vehicle;
                if (_followCamera != null) _followCamera.SetTarget(Buggy);
                return true;
            }
            else
            {
                Vector3 side = Vector3.ProjectOnPlane(Buggy.Root.right, Vector3.up).normalized * 2.4f;
                if (!TryStandingPosition(Buggy.Root.position + side, out Vector3 dismount) &&
                    !TryStandingPosition(Buggy.Root.position - side, out dismount))
                    return Reject("降車できる地面がありません。平らな場所に移動してください。");
                Buggy.SetActive(false);
                Foot.Teleport(dismount, Buggy.Root.eulerAngles.y);
                Foot.SetActive(true);
                Mode = LocomotionMode.OnFoot;
                if (_followCamera != null) _followCamera.SetTarget(Foot);
                return true;
            }
        }

        private RegionGround LoadedGround(double east, double south)
        {
            return _streamer.IsTargetNearReady(east, south) ? _streamer.SampleGroundType(east, south) : RegionGround.Cliff;
        }

        private bool TryStandingPosition(Vector3 position, out Vector3 standing)
        {
            standing = position;
            _streamer.Coordinates.LocalToWorld(position.x, position.y, position.z, out double east, out _, out double south);
            if (!RegionLocomotionMath.CanStand(LoadedGround(east, south))) return false;
            standing.y = _streamer.SampleGroundHeight(east, south) + 0.15f;
            return !Physics.CheckCapsule(standing + Vector3.up * 0.5f, standing + Vector3.up * 1.4f, 0.35f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        private bool Reject(string message)
        {
            OperationRejected?.Invoke(message);
            return false;
        }

        public void ApplyOriginShift(Vector3 localDelta)
        {
            if (Foot != null) Foot.ApplyOriginShift(localDelta);
            if (Buggy != null && Buggy.IsSpawned) Buggy.ApplyOriginShift(localDelta);
        }

        private void Update()
        {
            if (!IsEnabled) return;
            if (Input.GetKeyDown(KeyCode.F)) ToggleMountVehicle();
            if (Mode == LocomotionMode.OnFoot && Input.GetKeyDown(KeyCode.B)) SummonBuggyNearby();
        }
    }
}
