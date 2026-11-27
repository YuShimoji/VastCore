using System;

namespace Vastcore.RegionMap
{
    /// <summary>ドローンのオートパイロット状態。</summary>
    public enum DroneFlightState : byte
    {
        Idle = 0,
        Launching = 1,
        EnRoute = 2,
        ArrivedLoitering = 3,
        Returning = 4,
    }

    /// <summary>ドローンの現在テレメトリ（絶対メートル座標・速度・姿勢）。UnityEngine 非依存。</summary>
    public struct DroneTelemetry
    {
        public double EastM;
        public double SouthM;
        public float AltitudeM;
        public float GroundHeightM;
        public float VelocityEast;
        public float VelocitySouth;
        public float VelocityUp;
        public float HeadingDeg;
        public float PitchDeg;
        public float RollDeg;
        public DroneFlightState State;

        public float AltitudeAgl
        {
            get { return AltitudeM - GroundHeightM; }
        }

        public float HorizontalSpeed
        {
            get { return (float)Math.Sqrt(VelocityEast * VelocityEast + VelocitySouth * VelocitySouth); }
        }
    }

    /// <summary>アイドリング旋回挙動の設定プロファイル。</summary>
    [Serializable]
    public sealed class DroneIdleSettings
    {
        public float OrbitRadiusMeters = 220f;
        public float OrbitSpeedMps = 28f;
        public float CruiseAglMeters = 120f;
        public float MinClearanceMeters = 45f;
        public bool Clockwise = true;

        public DroneIdleSettings Clone()
        {
            return (DroneIdleSettings)MemberwiseClone();
        }
    }

    /// <summary>IDroneIdleBehavior がフレームごとに返す誘導指令。</summary>
    public struct DroneControlCommand
    {
        public float DesiredVelEast;
        public float DesiredVelSouth;
        public float DesiredAltitudeM;
        public double LookAtEastM;
        public double LookAtSouthM;
        public float LookAtHeightM;
        public bool HasLookTarget;
    }

    /// <summary>
    /// 目的地到達後のアイドリング挙動を差し替えるためのインターフェース。
    /// OrbitLoiterBehavior（既定）の他に、8 の字・螺旋・定点ホバー等へ差し替え可能。
    /// </summary>
    public interface IDroneIdleBehavior
    {
        string Id { get; }
        string DisplayName { get; }
        void Enter(DroneTelemetry state, double targetEastM, double targetSouthM, DroneIdleSettings settings);
        DroneControlCommand Evaluate(DroneTelemetry state, float deltaTime, Func<double, double, float> sampleGroundHeight);
    }

    /// <summary>
    /// 既定のアイドリング挙動：目的地で停止せず、指定半径・対地高度で周囲を連続旋回し続ける。
    /// 軌道前方の高地を先読みし、崖や尾根に接触しないよう対地高度を引き上げる。
    /// </summary>
    public sealed class OrbitLoiterBehavior : IDroneIdleBehavior
    {
        public string Id { get { return "orbit"; } }
        public string DisplayName { get { return "周回旋回 (Orbit)"; } }

        private double _centerEastM;
        private double _centerSouthM;
        private DroneIdleSettings _settings = new DroneIdleSettings();
        private double _angleRad;

        public void Enter(DroneTelemetry state, double targetEastM, double targetSouthM, DroneIdleSettings settings)
        {
            _centerEastM = targetEastM;
            _centerSouthM = targetSouthM;
            _settings = settings != null ? settings.Clone() : new DroneIdleSettings();
            double dx = state.EastM - _centerEastM;
            double dz = state.SouthM - _centerSouthM;
            _angleRad = (dx * dx + dz * dz) > 1.0 ? Math.Atan2(dz, dx) : 0.0;
        }

        public DroneControlCommand Evaluate(DroneTelemetry state, float deltaTime, Func<double, double, float> sampleGroundHeight)
        {
            float radius = Math.Max(40f, _settings.OrbitRadiusMeters);
            float speed = Math.Max(6f, _settings.OrbitSpeedMps);
            float dir = _settings.Clockwise ? 1f : -1f;

            double dx = state.EastM - _centerEastM;
            double dz = state.SouthM - _centerSouthM;
            double dist = Math.Sqrt(dx * dx + dz * dz);
            double currentAngle = dist > 1.0 ? Math.Atan2(dz, dx) : _angleRad;
            _angleRad = currentAngle + dir * (speed / radius) * deltaTime;

            // 接線方向ベクトル + 半径誤差を滑らかに補正する法線成分
            double radialX = Math.Cos(currentAngle);
            double radialZ = Math.Sin(currentAngle);
            double tangentX = -dir * radialZ;
            double tangentZ = dir * radialX;

            double radialError = dist - radius;
            double radialCorr = ClampDouble(-radialError * 0.35, -speed * 0.65, speed * 0.65);

            double vx = tangentX * speed + radialX * radialCorr;
            double vz = tangentZ * speed + radialZ * radialCorr;
            double vMag = Math.Sqrt(vx * vx + vz * vz);
            if (vMag > 0.01)
            {
                vx = vx / vMag * speed;
                vz = vz / vMag * speed;
            }

            // 軌道前方の標高を先読みして安全高度を算出
            float maxGround = sampleGroundHeight(state.EastM, state.SouthM);
            float[] lookAheadSec = { 2f, 5f, 9f };
            for (int i = 0; i < lookAheadSec.Length; i++)
            {
                double a = currentAngle + dir * (speed / radius) * lookAheadSec[i];
                double pe = _centerEastM + Math.Cos(a) * radius;
                double ps = _centerSouthM + Math.Sin(a) * radius;
                float g = sampleGroundHeight(pe, ps);
                if (g > maxGround) maxGround = g;
            }

            float targetGround = sampleGroundHeight(_centerEastM, _centerSouthM);
            float desiredAlt = Math.Max(maxGround + _settings.CruiseAglMeters,
                state.GroundHeightM + _settings.MinClearanceMeters);

            return new DroneControlCommand
            {
                DesiredVelEast = (float)vx,
                DesiredVelSouth = (float)vz,
                DesiredAltitudeM = desiredAlt,
                LookAtEastM = _centerEastM,
                LookAtSouthM = _centerSouthM,
                LookAtHeightM = targetGround + 8f,
                HasLookTarget = true,
            };
        }

        private static double ClampDouble(double v, double min, double max)
        {
            return v < min ? min : (v > max ? max : v);
        }
    }

    /// <summary>
    /// 差し替え用の別アイドリング挙動：目的地を中心に 8 の字（レムニスケート）軌道で連続巡回する。
    /// </summary>
    public sealed class FigureEightLoiterBehavior : IDroneIdleBehavior
    {
        public string Id { get { return "figure8"; } }
        public string DisplayName { get { return "8の字巡回 (Figure-8)"; } }

        private double _centerEastM;
        private double _centerSouthM;
        private DroneIdleSettings _settings = new DroneIdleSettings();
        private double _phase;

        public void Enter(DroneTelemetry state, double targetEastM, double targetSouthM, DroneIdleSettings settings)
        {
            _centerEastM = targetEastM;
            _centerSouthM = targetSouthM;
            _settings = settings != null ? settings.Clone() : new DroneIdleSettings();
            _phase = 0.0;
        }

        public DroneControlCommand Evaluate(DroneTelemetry state, float deltaTime, Func<double, double, float> sampleGroundHeight)
        {
            float scale = Math.Max(60f, _settings.OrbitRadiusMeters * 1.25f);
            float speed = Math.Max(6f, _settings.OrbitSpeedMps);
            float omega = speed / (scale * 1.3f);
            _phase += omega * deltaTime;

            double lookPhase = _phase + 0.35;
            double goalE = _centerEastM + Math.Sin(lookPhase) * scale;
            double goalS = _centerSouthM + Math.Sin(lookPhase) * Math.Cos(lookPhase) * scale;

            double dx = goalE - state.EastM;
            double dz = goalS - state.SouthM;
            double dist = Math.Sqrt(dx * dx + dz * dz);
            float vx = dist > 0.1 ? (float)(dx / dist * speed) : speed;
            float vz = dist > 0.1 ? (float)(dz / dist * speed) : 0f;

            float gHere = sampleGroundHeight(state.EastM, state.SouthM);
            float gGoal = sampleGroundHeight(goalE, goalS);
            float targetGround = sampleGroundHeight(_centerEastM, _centerSouthM);
            float desiredAlt = Math.Max(Math.Max(gHere, gGoal) + _settings.CruiseAglMeters,
                gHere + _settings.MinClearanceMeters);

            return new DroneControlCommand
            {
                DesiredVelEast = vx,
                DesiredVelSouth = vz,
                DesiredAltitudeM = desiredAlt,
                LookAtEastM = _centerEastM,
                LookAtSouthM = _centerSouthM,
                LookAtHeightM = targetGround + 8f,
                HasLookTarget = true,
            };
        }
    }

    /// <summary>
    /// 3D ドローンのオートパイロット航法とステートマシン（Launching → EnRoute → ArrivedLoitering → Returning）。
    /// 進行方向前方の標高先読みで崖・山脈を自動回避し、到着後は IDroneIdleBehavior に制御を委譲する。
    /// </summary>
    public sealed class DroneAutopilot
    {
        public float CruiseSpeedMps = 65f;
        public float MaxAccelMps2 = 30f;
        public float MaxClimbSpeedMps = 36f;
        public float ArrivalRadiusFactor = 1.25f;

        public DroneIdleSettings IdleSettings = new DroneIdleSettings();
        public IDroneIdleBehavior IdleBehavior { get; private set; }
        public DroneTelemetry Telemetry { get; private set; }

        public double TargetEastM { get; private set; }
        public double TargetSouthM { get; private set; }
        public double HomeEastM { get; private set; }
        public double HomeSouthM { get; private set; }

        public double GimbalLookEastM { get; private set; }
        public double GimbalLookSouthM { get; private set; }
        public float GimbalLookHeightM { get; private set; }

        private float _launchTimer;

        public DroneAutopilot(IDroneIdleBehavior initialIdleBehavior = null)
        {
            IdleBehavior = initialIdleBehavior ?? new OrbitLoiterBehavior();
        }

        /// <summary>到着後アイドリング挙動を差し替える。旋回中であれば即座に新しい挙動へ移行する。</summary>
        public void SetIdleBehavior(IDroneIdleBehavior behavior)
        {
            if (behavior == null) throw new ArgumentNullException("behavior");
            IdleBehavior = behavior;
            if (Telemetry.State == DroneFlightState.ArrivedLoitering)
                IdleBehavior.Enter(Telemetry, TargetEastM, TargetSouthM, IdleSettings);
        }

        /// <summary>出発地から指定目的地へドローンを派遣する。</summary>
        public void Dispatch(double startEastM, double startSouthM, float startAltM,
            double targetEastM, double targetSouthM)
        {
            HomeEastM = startEastM;
            HomeSouthM = startSouthM;
            TargetEastM = targetEastM;
            TargetSouthM = targetSouthM;
            _launchTimer = 0f;

            double dx = targetEastM - startEastM;
            double dz = targetSouthM - startSouthM;
            float heading = (dx * dx + dz * dz) > 1.0 ? (float)(Math.Atan2(dx, -dz) * 180.0 / Math.PI) : 0f;

            var t = Telemetry;
            t.EastM = startEastM;
            t.SouthM = startSouthM;
            t.AltitudeM = startAltM;
            t.VelocityEast = 0f;
            t.VelocitySouth = 0f;
            t.VelocityUp = 0f;
            t.HeadingDeg = heading;
            t.PitchDeg = 0f;
            t.RollDeg = 0f;
            t.State = DroneFlightState.Launching;
            Telemetry = t;

            GimbalLookEastM = targetEastM;
            GimbalLookSouthM = targetSouthM;
        }

        /// <summary>飛行中の現在地から新しい目的地へオートパイロットを再設定する。</summary>
        public void SetTarget(double targetEastM, double targetSouthM)
        {
            TargetEastM = targetEastM;
            TargetSouthM = targetSouthM;
            var t = Telemetry;
            if (t.State == DroneFlightState.Idle)
            {
                t.State = DroneFlightState.Launching;
                _launchTimer = 0f;
            }
            else
            {
                t.State = DroneFlightState.EnRoute;
            }
            Telemetry = t;
        }

        /// <summary>出発地（または指定位置）へ帰還させる。</summary>
        public void ReturnTo(double homeEastM, double homeSouthM)
        {
            HomeEastM = homeEastM;
            HomeSouthM = homeSouthM;
            TargetEastM = homeEastM;
            TargetSouthM = homeSouthM;
            var t = Telemetry;
            t.State = DroneFlightState.Returning;
            Telemetry = t;
        }

        public double HorizontalDistanceToTarget()
        {
            double dx = TargetEastM - Telemetry.EastM;
            double dz = TargetSouthM - Telemetry.SouthM;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        public void Step(float deltaTime, Func<double, double, float> sampleGroundHeight, double worldSizeMeters = double.PositiveInfinity)
        {
            if (deltaTime <= 0f || sampleGroundHeight == null) return;
            if (deltaTime > 0.1f) deltaTime = 0.1f;

            DroneTelemetry t = Telemetry;
            if (t.State == DroneFlightState.Idle) return;

            float groundHere = sampleGroundHeight(t.EastM, t.SouthM);
            t.GroundHeightM = groundHere;

            DroneControlCommand cmd = default(DroneControlCommand);

            switch (t.State)
            {
                case DroneFlightState.Launching:
                    _launchTimer += deltaTime;
                    cmd.DesiredVelEast = 0f;
                    cmd.DesiredVelSouth = 0f;
                    cmd.DesiredAltitudeM = groundHere + IdleSettings.CruiseAglMeters;
                    cmd.LookAtEastM = TargetEastM;
                    cmd.LookAtSouthM = TargetSouthM;
                    cmd.LookAtHeightM = sampleGroundHeight(TargetEastM, TargetSouthM) + 8f;
                    cmd.HasLookTarget = true;
                    if (t.AltitudeM >= groundHere + IdleSettings.MinClearanceMeters || _launchTimer >= 0.7f)
                    {
                        t.State = DroneFlightState.EnRoute;
                    }
                    break;

                case DroneFlightState.EnRoute:
                case DroneFlightState.Returning:
                {
                    double goalE = t.State == DroneFlightState.Returning ? HomeEastM : TargetEastM;
                    double goalS = t.State == DroneFlightState.Returning ? HomeSouthM : TargetSouthM;
                    double dx = goalE - t.EastM;
                    double dz = goalS - t.SouthM;
                    double dist = Math.Sqrt(dx * dx + dz * dz);
                    double arriveDist = Math.Max(50.0, IdleSettings.OrbitRadiusMeters * ArrivalRadiusFactor);

                    if (dist <= arriveDist)
                    {
                        t.State = DroneFlightState.ArrivedLoitering;
                        Telemetry = t;
                        IdleBehavior.Enter(t, goalE, goalS, IdleSettings);
                        cmd = IdleBehavior.Evaluate(t, deltaTime, sampleGroundHeight);
                    }
                    else
                    {
                        double dirE = dx / dist;
                        double dirS = dz / dist;
                        float speed = CruiseSpeedMps;
                        cmd.DesiredVelEast = (float)(dirE * speed);
                        cmd.DesiredVelSouth = (float)(dirS * speed);

                        // 前方 60m / 180m / 360m の標高を先読みして地形衝突を回避
                        float aheadMax = groundHere;
                        float[] probes = { 60f, 180f, 360f };
                        for (int i = 0; i < probes.Length; i++)
                        {
                            float g = sampleGroundHeight(t.EastM + dirE * probes[i], t.SouthM + dirS * probes[i]);
                            if (g > aheadMax) aheadMax = g;
                        }
                        cmd.DesiredAltitudeM = Math.Max(aheadMax + IdleSettings.CruiseAglMeters,
                            groundHere + IdleSettings.MinClearanceMeters);
                        cmd.LookAtEastM = t.EastM + dirE * Math.Min(dist, 420.0);
                        cmd.LookAtSouthM = t.SouthM + dirS * Math.Min(dist, 420.0);
                        cmd.LookAtHeightM = sampleGroundHeight(cmd.LookAtEastM, cmd.LookAtSouthM) + 6f;
                        cmd.HasLookTarget = true;
                    }
                    break;
                }

                case DroneFlightState.ArrivedLoitering:
                    cmd = IdleBehavior.Evaluate(t, deltaTime, sampleGroundHeight);
                    break;
            }

            // 水平速度を滑らかに追従
            float dvE = cmd.DesiredVelEast - t.VelocityEast;
            float dvS = cmd.DesiredVelSouth - t.VelocitySouth;
            float dvMag = (float)Math.Sqrt(dvE * dvE + dvS * dvS);
            float maxDv = MaxAccelMps2 * deltaTime;
            float appliedAx = dvE / deltaTime;
            float appliedAz = dvS / deltaTime;
            if (dvMag > maxDv && dvMag > 1e-4f)
            {
                float scale = maxDv / dvMag;
                dvE *= scale;
                dvS *= scale;
                appliedAx = dvE / deltaTime;
                appliedAz = dvS / deltaTime;
            }
            t.VelocityEast += dvE;
            t.VelocitySouth += dvS;

            // 垂直速度（地形追従高度制御）
            float altErr = cmd.DesiredAltitudeM - t.AltitudeM;
            float desiredVy = ClampFloat(altErr * 1.8f, -MaxClimbSpeedMps, MaxClimbSpeedMps);
            float dVy = ClampFloat(desiredVy - t.VelocityUp, -MaxAccelMps2 * deltaTime, MaxAccelMps2 * deltaTime);
            t.VelocityUp += dVy;

            // 位置積分
            t.EastM += t.VelocityEast * deltaTime;
            t.SouthM += t.VelocitySouth * deltaTime;
            t.AltitudeM += t.VelocityUp * deltaTime;
            if (!double.IsPositiveInfinity(worldSizeMeters))
            {
                double east = Math.Clamp(t.EastM, 0.0, worldSizeMeters);
                double south = Math.Clamp(t.SouthM, 0.0, worldSizeMeters);
                if (east != t.EastM) t.VelocityEast = 0f;
                if (south != t.SouthM) t.VelocitySouth = 0f;
                t.EastM = east;
                t.SouthM = south;
            }

            // 最低安全高度（ハードフロア）
            float newGround = sampleGroundHeight(t.EastM, t.SouthM);
            t.GroundHeightM = newGround;
            float hardFloor = newGround + 8f;
            if (t.AltitudeM < hardFloor)
            {
                t.AltitudeM = hardFloor;
                if (t.VelocityUp < 0f) t.VelocityUp = 0f;
            }

            // 機体姿勢（方位・ピッチ・旋回バンク角）
            float hSpeed = t.HorizontalSpeed;
            if (hSpeed > 1f)
            {
                // Unity 座標 (X=東, Z=北=-South) での Yaw（北=0, 東=90）
                t.HeadingDeg = (float)(Math.Atan2(t.VelocityEast, -t.VelocitySouth) * 180.0 / Math.PI);
                float fwdX = t.VelocityEast / hSpeed;
                float fwdZ = t.VelocitySouth / hSpeed;
                // 右方向ベクトル (East, South) 平面で 90°時計回り: (-fwdZ, fwdX)
                float lateralAccel = (-fwdZ * appliedAx) + (fwdX * appliedAz);
                float targetRoll = ClampFloat(-lateralAccel * 1.4f, -32f, 32f);
                t.RollDeg += (targetRoll - t.RollDeg) * Math.Min(1f, deltaTime * 5f);
                float targetPitch = ClampFloat(hSpeed / Math.Max(1f, CruiseSpeedMps) * 12f - t.VelocityUp * 0.35f, -18f, 18f);
                t.PitchDeg += (targetPitch - t.PitchDeg) * Math.Min(1f, deltaTime * 5f);
            }
            else
            {
                t.RollDeg *= Math.Max(0f, 1f - deltaTime * 4f);
                t.PitchDeg *= Math.Max(0f, 1f - deltaTime * 4f);
            }

            if (cmd.HasLookTarget)
            {
                GimbalLookEastM = cmd.LookAtEastM;
                GimbalLookSouthM = cmd.LookAtSouthM;
                GimbalLookHeightM = cmd.LookAtHeightM;
            }

            Telemetry = t;
        }

        private static float ClampFloat(float v, float min, float max)
        {
            return v < min ? min : (v > max ? max : v);
        }
    }
}
