using UnityEngine;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 3D ワールド上のドローン本体・オートパイロット・別カメラリグを統合制御するコンポーネント。
    /// IDroneIdleBehavior の差し替え口を持ち、Floating Origin に追従する。
    /// </summary>
    public sealed class DroneController : MonoBehaviour
    {
        public DroneAutopilot Autopilot { get; private set; }
        public DroneCameraRig CameraRig { get; private set; }
        public bool IsActive { get; private set; }

        private RegionChunkStreamer _streamer;
        private Transform _bodyVisual;
        private Transform[] _rotors;
        private int _idlePresetIndex;
        internal bool FlightPaused { get; set; }

        private void Awake()
        {
            Autopilot = new DroneAutopilot(new OrbitLoiterBehavior());
            CameraRig = gameObject.AddComponent<DroneCameraRig>();
            CameraRig.EnsureInitialized();
            BuildVisualProxy();
        }

        public void Bind(RegionChunkStreamer streamer)
        {
            _streamer = streamer;
        }

        public void Dispatch(double startEastM, double startSouthM, float startAltM,
            double targetEastM, double targetSouthM, DroneDisplayMode displayMode)
        {
            IsActive = true;
            FlightPaused = false;
            if (_bodyVisual != null) _bodyVisual.gameObject.SetActive(true);
            Autopilot.Dispatch(startEastM, startSouthM, startAltM, targetEastM, targetSouthM);
            CameraRig.ResetGimbal();
            CameraRig.SetDisplayMode(displayMode);
            SyncTransform(0f);
        }

        public void SetTarget(double targetEastM, double targetSouthM)
        {
            IsActive = true;
            if (_bodyVisual != null) _bodyVisual.gameObject.SetActive(true);
            Autopilot.SetTarget(targetEastM, targetSouthM);
        }

        public void ReturnHome(double homeEastM, double homeSouthM)
        {
            if (!IsActive) return;
            Autopilot.ReturnTo(homeEastM, homeSouthM);
        }

        public void SetDisplayMode(DroneDisplayMode mode)
        {
            CameraRig.SetDisplayMode(mode);
        }

        /// <summary>到着後アイドリング挙動を任意の IDroneIdleBehavior 実装へ差し替える。</summary>
        public void SetIdleBehavior(IDroneIdleBehavior behavior)
        {
            Autopilot.SetIdleBehavior(behavior);
        }

        /// <summary>組み込みアイドリング挙動（周回旋回 ⇄ 8の字巡回）を切り替える。</summary>
        public string CycleIdleBehavior()
        {
            _idlePresetIndex = (_idlePresetIndex + 1) % 2;
            IDroneIdleBehavior next = _idlePresetIndex == 0
                ? (IDroneIdleBehavior)new OrbitLoiterBehavior()
                : new FigureEightLoiterBehavior();
            Autopilot.SetIdleBehavior(next);
            return next.DisplayName;
        }

        public void Deactivate()
        {
            IsActive = false;
            FlightPaused = false;
            CameraRig.SetDisplayMode(DroneDisplayMode.Hidden);
            if (_bodyVisual != null) _bodyVisual.gameObject.SetActive(false);
        }

        public void ApplyOriginShift(Vector3 localDelta)
        {
            if (_bodyVisual != null) _bodyVisual.position += localDelta;
            CameraRig.ApplyOriginShift(localDelta);
        }

        private void Update()
        {
            if (!IsActive || FlightPaused || _streamer == null || _streamer.Coordinates == null) return;
            Autopilot.Step(Time.deltaTime, _streamer.SampleGroundHeight, _streamer.Coordinates.WorldSizeMeters);
            SyncTransform(Time.deltaTime);
        }

        private void SyncTransform(float dt)
        {
            if (_streamer == null || _streamer.Coordinates == null) return;
            DroneTelemetry t = Autopilot.Telemetry;
            _streamer.Coordinates.WorldToLocal(t.EastM, t.AltitudeM, t.SouthM,
                out float lx, out float ly, out float lz);
            var localPos = new Vector3(lx, ly, lz);

            if (_bodyVisual != null)
            {
                _bodyVisual.position = localPos;
                _bodyVisual.rotation = Quaternion.Euler(t.PitchDeg, t.HeadingDeg, t.RollDeg);
                if (_rotors != null)
                {
                    float spin = dt * 1440f;
                    for (int i = 0; i < _rotors.Length; i++)
                        if (_rotors[i] != null) _rotors[i].Rotate(0f, spin, 0f, Space.Self);
                }
            }

            float lookHeight = dt > 0f ? Autopilot.GimbalLookHeightM : _streamer.SampleGroundHeight(Autopilot.TargetEastM, Autopilot.TargetSouthM) + 8f;
            _streamer.Coordinates.WorldToLocal(Autopilot.GimbalLookEastM, lookHeight, Autopilot.GimbalLookSouthM,
                out float lookX, out float lookY, out float lookZ);
            CameraRig.UpdateGimbal(localPos, new Vector3(lookX, lookY, lookZ), dt);
        }

        private void BuildVisualProxy()
        {
            var root = new GameObject("Drone Visual");
            root.transform.SetParent(transform, false);
            _bodyVisual = root.transform;

            Material bodyMat = CreateSimpleMaterial(new Color(0.18f, 0.21f, 0.25f));
            Material accentMat = CreateSimpleMaterial(new Color(0.92f, 0.56f, 0.16f));

            var hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.name = "Hull";
            Destroy(hull.GetComponent<Collider>());
            hull.transform.SetParent(_bodyVisual, false);
            hull.transform.localScale = new Vector3(0.9f, 0.28f, 1.35f);
            hull.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;

            var nose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nose.name = "Gimbal";
            Destroy(nose.GetComponent<Collider>());
            nose.transform.SetParent(_bodyVisual, false);
            nose.transform.localPosition = new Vector3(0f, -0.22f, 0.45f);
            nose.transform.localScale = new Vector3(0.32f, 0.32f, 0.32f);
            nose.GetComponent<MeshRenderer>().sharedMaterial = accentMat;

            Vector3[] armPos =
            {
                new Vector3(0.9f, 0.08f, 0.85f),
                new Vector3(-0.9f, 0.08f, 0.85f),
                new Vector3(0.9f, 0.08f, -0.85f),
                new Vector3(-0.9f, 0.08f, -0.85f),
            };
            _rotors = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                var rotor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rotor.name = "Rotor " + i;
                Destroy(rotor.GetComponent<Collider>());
                rotor.transform.SetParent(_bodyVisual, false);
                rotor.transform.localPosition = armPos[i];
                rotor.transform.localScale = new Vector3(0.75f, 0.03f, 0.75f);
                rotor.GetComponent<MeshRenderer>().sharedMaterial = accentMat;
                _rotors[i] = rotor.transform;
            }
            _bodyVisual.gameObject.SetActive(false);
        }

        private static Material CreateSimpleMaterial(Color color)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(s) { hideFlags = HideFlags.DontSave };
            m.color = color;
            return m;
        }
    }
}
