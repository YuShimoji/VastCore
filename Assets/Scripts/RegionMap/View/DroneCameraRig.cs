using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Vastcore.RegionMap
{
    /// <summary>ドローン別カメラの表示形態。</summary>
    public enum DroneDisplayMode : byte
    {
        Hidden = 0,
        PictureInPicture = 1,
        FullScreen = 2,
    }

    /// <summary>
    /// プレイヤー／2Dカメラとは独立したドローン用別カメラリグ。
    /// 全画面表示と RenderTexture による PiP（小窓）表示の両方に対応し、ロール水平安定化ジンバルを持つ。
    /// </summary>
    public sealed class DroneCameraRig : MonoBehaviour
    {
        public int PipWidth = 480;
        public int PipHeight = 270;
        public float FieldOfView = 55f;
        public float GimbalSmoothing = 6f;

        public Camera DroneCamera { get; private set; }
        public RenderTexture PipTexture { get; private set; }
        public DroneDisplayMode DisplayMode { get; private set; } = DroneDisplayMode.Hidden;

        private Quaternion _gimbalRotation = Quaternion.identity;
        private bool _gimbalInitialized;

        internal void ResetGimbal()
        {
            _gimbalInitialized = false;
        }

        public void EnsureInitialized()
        {
            if (DroneCamera != null) return;
            var camGo = new GameObject("Drone Camera");
            camGo.transform.SetParent(transform, false);
            DroneCamera = camGo.AddComponent<Camera>();
            DroneCamera.fieldOfView = FieldOfView;
            DroneCamera.nearClipPlane = 0.5f;
            DroneCamera.farClipPlane = 60000f;
            DroneCamera.allowHDR = true;
            DroneCamera.depth = 5f;
            DroneCamera.enabled = false;

            UniversalAdditionalCameraData urp = DroneCamera.GetUniversalAdditionalCameraData();
            if (urp != null)
            {
                urp.renderPostProcessing = true;
                urp.renderShadows = true;
            }
        }

        public void SetDisplayMode(DroneDisplayMode mode)
        {
            EnsureInitialized();
            DisplayMode = mode;

            switch (mode)
            {
                case DroneDisplayMode.Hidden:
                    DroneCamera.targetTexture = null;
                    DroneCamera.enabled = false;
                    break;

                case DroneDisplayMode.PictureInPicture:
                    EnsurePipTexture();
                    DroneCamera.targetTexture = PipTexture;
                    DroneCamera.enabled = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
                    break;

                case DroneDisplayMode.FullScreen:
                    DroneCamera.targetTexture = null;
                    DroneCamera.enabled = true;
                    break;
            }
        }

        /// <summary>
        /// 機体のローカル位置と注視ターゲットから、水平安定化（ロール 0°）されたジンバル姿勢を更新する。
        /// </summary>
        public void UpdateGimbal(Vector3 droneLocalPos, Vector3 lookAtLocalPos, float deltaTime)
        {
            EnsureInitialized();
            Vector3 mountPos = droneLocalPos + new Vector3(0f, -0.6f, 0f);
            DroneCamera.transform.position = mountPos;

            Vector3 dir = lookAtLocalPos - mountPos;
            if (dir.sqrMagnitude < 1f) dir = Vector3.forward + Vector3.down * 0.35f;
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            if (!_gimbalInitialized)
            {
                _gimbalRotation = targetRot;
                _gimbalInitialized = true;
            }
            else
            {
                float t = 1f - Mathf.Exp(-GimbalSmoothing * Mathf.Max(0.001f, deltaTime));
                _gimbalRotation = Quaternion.Slerp(_gimbalRotation, targetRot, t);
            }
            DroneCamera.transform.rotation = _gimbalRotation;
        }

        public void ApplyOriginShift(Vector3 localDelta)
        {
            if (DroneCamera != null)
                DroneCamera.transform.position += localDelta;
        }

        private void EnsurePipTexture()
        {
            if (PipTexture != null && PipTexture.width == PipWidth && PipTexture.height == PipHeight)
                return;
            ReleasePipTexture();
            PipTexture = new RenderTexture(PipWidth, PipHeight, 24, RenderTextureFormat.ARGB32)
            {
                name = "Drone PiP Texture",
                hideFlags = HideFlags.DontSave,
            };
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                PipTexture.Create();
        }

        private void ReleasePipTexture()
        {
            if (PipTexture == null) return;
            if (DroneCamera != null && DroneCamera.targetTexture == PipTexture)
                DroneCamera.targetTexture = null;
            PipTexture.Release();
            Destroy(PipTexture);
            PipTexture = null;
        }

        private void OnDestroy()
        {
            ReleasePipTexture();
        }
    }
}
