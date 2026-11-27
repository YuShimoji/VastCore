using System;
using UnityEngine;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 2D 見下ろし画面 ⇄ 3D 空間（OpenWorld3D / DroneRecon3D）のシームレス降下・上昇遷移を制御する。
    /// 雲通過フォグと 2D オーバーレイフェードを同期させる。
    /// </summary>
    public sealed class DiveTransitionController : MonoBehaviour
    {
        public float DiveDurationSeconds = 2.1f;
        public float AscendDurationSeconds = 1.6f;

        public bool IsTransitioning { get { return _active; } }
        public bool IsAscending { get { return _ascending; } }
        public float Progress { get { return _progress; } }

        private readonly DiveTransitionCurve _curve = new DiveTransitionCurve();
        private bool _active;
        private bool _ascending;
        private float _elapsed;
        private float _duration;
        private float _progress;
        private double _targetEastM;
        private double _targetSouthM;
        private float _cloudCover;
        private float _baseFogDensity;
        private float _yawDegrees;
        private Pose _handoff;
        private Pose _approachStart;
        private bool _approach;

        private Camera _camera;
        private CanvasGroup _overlay2D;
        private RegionChunkStreamer _streamer;
        private Action _onComplete;

        public void BeginDive(Camera camera, CanvasGroup overlay2D, RegionChunkStreamer streamer,
            double targetEastM, double targetSouthM, float visibleWidthMeters,
            Pose arrival, float cloudCover, float baseFogDensity,
            Action onComplete)
        {
            _camera = camera;
            _overlay2D = overlay2D;
            _streamer = streamer;
            _targetEastM = targetEastM;
            _targetSouthM = targetSouthM;
            _cloudCover = cloudCover;
            _baseFogDensity = baseFogDensity;
            _yawDegrees = 0f;
            _onComplete = onComplete;

            SetCurve(visibleWidthMeters, arrival);

            _approach = false;
            _ascending = false;
            _elapsed = 0f;
            _duration = Mathf.Max(0.4f, DiveDurationSeconds);
            _progress = 0f;
            _active = true;

            ApplyFrame(0f);
        }

        public void BeginAscend(Camera camera, CanvasGroup overlay2D, RegionChunkStreamer streamer,
            double currentEastM, double currentSouthM, float visibleWidthMeters,
            Pose departure,
            float cloudCover, float baseFogDensity, Action onComplete)
        {
            _camera = camera;
            _overlay2D = overlay2D;
            _streamer = streamer;
            _targetEastM = currentEastM;
            _targetSouthM = currentSouthM;
            _cloudCover = cloudCover;
            _baseFogDensity = baseFogDensity;
            _yawDegrees = departure.rotation.eulerAngles.y;
            _onComplete = onComplete;

            SetCurve(visibleWidthMeters, departure);

            _approach = false;
            _ascending = true;
            _elapsed = 0f;
            _duration = Mathf.Max(0.4f, AscendDurationSeconds);
            _progress = 0f;
            _active = true;

            ApplyFrame(0f);
        }

        public void BeginGroundApproach(Camera camera, Pose departure, Pose arrival, float baseFogDensity, Action onComplete)
        {
            _camera = camera;
            _approachStart = departure;
            _handoff = arrival;
            _baseFogDensity = baseFogDensity;
            _onComplete = onComplete;
            _approach = true;
            _ascending = false;
            _elapsed = 0f;
            _progress = 0f;
            _duration = 1.2f;
            _active = true;
            ApplyFrame(0f);
        }

        private void SetCurve(float visibleWidthMeters, Pose pose)
        {
            _handoff = pose;
            float ground = _streamer.SampleGroundHeight(_targetEastM, _targetSouthM);
            // 広域表示からも、準備済みの遠景を越える高さで地図を透かさない。
            float maxBackdropAltitude = 12000f / (2f * Mathf.Tan(_camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * _camera.aspect);
            _curve.StartAglMeters = Mathf.Min(maxBackdropAltitude,
                DiveTransitionCurve.ComputeSyncAltitude(visibleWidthMeters, _camera.fieldOfView, _camera.aspect));
            _curve.TargetAglMeters = Mathf.Max(1.5f, pose.position.y - ground);
            _curve.TargetPitchDegrees = Mathf.Clamp(Mathf.DeltaAngle(0f, pose.rotation.eulerAngles.x), -15f, 90f);
        }

        public void Cancel()
        {
            if (_active) RestoreBaseFog();
            _active = false;
            _onComplete = null;
        }

        private void Update()
        {
            if (!_active) return;

            _elapsed += Time.unscaledDeltaTime;
            _progress = Mathf.Clamp01(_elapsed / _duration);
            ApplyFrame(_progress);

            if (_progress >= 1f)
            {
                _active = false;
                RestoreBaseFog();
                Action done = _onComplete;
                _onComplete = null;
                done?.Invoke();
            }
        }

        private void ApplyFrame(float t)
        {
            if (_approach)
            {
                float blend = Mathf.SmoothStep(0f, 1f, t);
                _camera.transform.SetPositionAndRotation(Vector3.Lerp(_approachStart.position, _handoff.position, blend),
                    Quaternion.Slerp(_approachStart.rotation, _handoff.rotation, blend));
                return;
            }
            if (_streamer == null || _streamer.Coordinates == null || _camera == null) return;
            float groundH = _streamer.SampleGroundHeight(_targetEastM, _targetSouthM);
            _curve.Evaluate(t, groundH, _cloudCover, _ascending,
                out float altM, out float pitchDeg, out float alpha2D, out float cloudPassFog);

            _streamer.Coordinates.WorldToLocal(_targetEastM, altM, _targetSouthM,
                out float lx, out float ly, out float lz);
            float handoff = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1f, _ascending ? 1f - t : t));
            _camera.transform.SetPositionAndRotation(Vector3.Lerp(new Vector3(lx, ly, lz), _handoff.position, handoff),
                Quaternion.Slerp(Quaternion.Euler(pitchDeg, _yawDegrees, 0f), _handoff.rotation, handoff));

            if (_overlay2D != null) _overlay2D.alpha = alpha2D;

            // 雲層通過時の柔らかなフォグ加算
            Vector4 fogParams = Shader.GetGlobalVector("_RegionFogParams");
            fogParams.x = _baseFogDensity + cloudPassFog * 0.00055f;
            Shader.SetGlobalVector("_RegionFogParams", fogParams);
        }

        private void RestoreBaseFog()
        {
            Vector4 fogParams = Shader.GetGlobalVector("_RegionFogParams");
            fogParams.x = _baseFogDensity;
            Shader.SetGlobalVector("_RegionFogParams", fogParams);
        }
    }
}
