using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 2D 探索画面（RegionExplorer）、シームレス・ダイブ遷移、3D ドローン旋回モード、
    /// および 3D オープンワールド移動モード（徒歩＋簡易ビークル）を単一セッションで統合管理する。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RegionWorldSession : MonoBehaviour
    {
        private static readonly ProfilerMarker AtmosphereMarker = new ProfilerMarker(ProfilerCategory.Scripts, "VastCore.Region.Atmosphere");
        private static readonly ProfilerMarker CloudUploadMarker = new ProfilerMarker(ProfilerCategory.Scripts, "VastCore.Region.UploadClouds");
        public RegionWorldMode Mode { get; private set; } = RegionWorldMode.Overview2D;
        public RegionChunkStreamer Streamer { get; private set; }
        public DiveTransitionController DiveController { get; private set; }
        public DroneController Drone { get; private set; }
        public RegionLocomotionManager Locomotion { get; private set; }
        public Camera WorldCamera { get; private set; }

        private RegionExplorer _explorer;
        private CanvasGroup _explorerCanvasGroup;
        private Camera _overviewCamera;
        private RegionOpenWorldCamera _followCamera;
        private Light _sun;
        private GameObject _atmosphereVolume;
        private Texture2D _cloudDensityTex;
        private Material _worldSkybox;
        private Material _overviewSkybox;
        private Task<(Color[] Pixels, int Side)> _cloudBuild;
        private Coroutine _terrainPreparation;
        private string _statusMessage;
        private float _statusUntil;
        private const float TerrainTimeoutSeconds = 15f;
        private const string CannotLand = "この地点には着地できません。水域や崖を避けて地点を選んでください。";
        private float _baseFogDensity = 0.00008f;
        private float _cloudCover = 0.58f;
        private float _cloudOpacity;
        private float _cloudTime;
        private float _cloudCrossSeconds = RegionCloudFlow.DefaultCrossSeconds;

        // 3D HUD & ドローン PiP 小窓 UI
        private Canvas _hudCanvas;
        private GameObject _hudBar;
        private Text _hudModeText;
        private Text _hudStatusText;
        private GameObject _pipPanel;
        private RawImage _pipImage;
        private Text _pipCaption;

        public void BindExplorer(RegionExplorer explorer, Canvas explorerCanvas)
        {
            _explorer = explorer;
            Camera main = Camera.main;
            if (main != null && main.gameObject.scene == gameObject.scene) _overviewCamera = main;
            if (explorerCanvas != null)
            {
                _explorerCanvasGroup = explorerCanvas.GetComponent<CanvasGroup>();
                if (_explorerCanvasGroup == null)
                    _explorerCanvasGroup = explorerCanvas.gameObject.AddComponent<CanvasGroup>();
            }
            EnsureSubsystems();
        }

        private void EnsureSubsystems()
        {
            if (Streamer != null) return;

            var streamerGo = new GameObject("RegionChunkStreamer");
            streamerGo.transform.SetParent(transform, false);
            Streamer = streamerGo.AddComponent<RegionChunkStreamer>();
            Streamer.OnOriginShifted += HandleOriginShifted;

            var diveGo = new GameObject("DiveTransitionController");
            diveGo.transform.SetParent(transform, false);
            DiveController = diveGo.AddComponent<DiveTransitionController>();

            var camGo = new GameObject("Region World 3D Camera");
            camGo.transform.SetParent(transform, false);
            WorldCamera = camGo.AddComponent<Camera>();
            WorldCamera.fieldOfView = 55f;
            WorldCamera.nearClipPlane = 0.25f;
            WorldCamera.farClipPlane = 65000f;
            WorldCamera.allowHDR = true;
            WorldCamera.depth = 1f;
            WorldCamera.enabled = false;
            Streamer.BindGrasslandCamera(WorldCamera);
            UniversalAdditionalCameraData urp = WorldCamera.GetUniversalAdditionalCameraData();
            if (urp != null)
            {
                urp.renderPostProcessing = true;
                urp.renderShadows = true;
            }
            _followCamera = camGo.AddComponent<RegionOpenWorldCamera>();

            var droneGo = new GameObject("Region Drone");
            droneGo.transform.SetParent(transform, false);
            Drone = droneGo.AddComponent<DroneController>();
            Drone.Bind(Streamer);

            var locoGo = new GameObject("Region Locomotion");
            locoGo.transform.SetParent(transform, false);
            Locomotion = locoGo.AddComponent<RegionLocomotionManager>();
            Locomotion.Initialize(Streamer, _followCamera);
            Locomotion.OperationRejected += ReportStatus;

            BuildHudAndPipUi();
        }

        /// <summary>2D 探索画面の指定地点へシームレスにダイブし、オープンワールド移動モードへ移行する。</summary>
        public bool StartDiveToOpenWorld(float mapX, float mapY)
        {
            return StartDiveInternal(mapX, mapY, RegionWorldMode.OpenWorld3D);
        }

        /// <summary>2D 探索画面の指定地点へドローンを派遣し、上空旋回モード（または PiP）を開始する。</summary>
        public bool StartDiveToDrone(float mapX, float mapY, bool fullScreen = true)
        {
            if (fullScreen)
                return StartDiveInternal(mapX, mapY, RegionWorldMode.DroneRecon3D);

            // 2D 画面を維持したまま PiP 小窓でドローン派遣
            if (!PrepareWorldAt(mapX, mapY, out double targetE, out double targetS)) return false;
            QueueCloudSnapshot();
            EnsureAtmosphere(Streamer.SampleGroundHeight(targetE, targetS));
            DispatchDrone(targetE - 750.0, targetS + 550.0, targetE, targetS, DroneDisplayMode.PictureInPicture);
            UpdatePipVisibility();
            return true;
        }

        /// <summary>3D 空間から 2D 見下ろし画面へシームレス復帰する。</summary>
        public void AscendToOverview2D()
        {
            if (Mode == RegionWorldMode.Overview2D || Mode == RegionWorldMode.Ascending) return;
            if (Streamer == null || Streamer.Coordinates == null) return;
            if (Mode == RegionWorldMode.Diving)
            {
                CancelPreparation();
                DiveController.Cancel();
                if (Drone.FlightPaused) Drone.Deactivate();
                FinishOverview();
                return;
            }

            CancelPreparation();
            bool fromDrone = Mode == RegionWorldMode.DroneRecon3D;
            Pose departure = CameraPose(fromDrone ? Drone.CameraRig.DroneCamera : WorldCamera);
            double curE, curS;
            if (fromDrone)
            {
                DroneTelemetry telemetry = Drone.Autopilot.Telemetry;
                curE = telemetry.EastM;
                curS = telemetry.SouthM;
            }
            else
            {
                Vector3 pos = Locomotion.ActiveAgent.Root.position;
                Streamer.Coordinates.LocalToWorld(pos.x, pos.y, pos.z, out curE, out _, out curS);
            }

            Locomotion.ExitOpenWorld();
            if (fromDrone)
                Drone.SetDisplayMode(DroneDisplayMode.PictureInPicture);

            WorldCamera.enabled = true;
            Mode = RegionWorldMode.Ascending;
            SetHudVisible(false);

            // 2D カーソルを現在 3D 位置へ同期
            Streamer.Coordinates.WorldMetersToMap(curE, curS, out float mapX, out float mapY);
            if (_explorer != null) _explorer.SyncWorldCursor(mapX, mapY);

            float visibleWidthM = GetVisible2DWidthMeters();
            DiveController.BeginAscend(WorldCamera, _explorerCanvasGroup, Streamer,
                curE, curS, visibleWidthM, departure, _cloudCover * _cloudOpacity, _baseFogDensity, FinishOverview);
        }

        /// <summary>ドローンモードとオープンワールド地上モードを相互切替する。</summary>
        public void ToggleDroneAndOpenWorld()
        {
            if (_terrainPreparation != null) return;
            if (Mode == RegionWorldMode.OpenWorld3D)
            {
                if (!Drone.IsActive) DispatchDroneFromPlayer(DroneDisplayMode.Hidden);
                Pose departure = CameraPose(WorldCamera);
                Locomotion.ExitOpenWorld();
                Drone.FlightPaused = true;
                Drone.SetDisplayMode(DroneDisplayMode.Hidden);
                Mode = RegionWorldMode.Diving;
                SetHudVisible(false);
                UpdatePipVisibility();
                DiveController.BeginGroundApproach(WorldCamera, departure, CameraPose(Drone.CameraRig.DroneCamera),
                    _baseFogDensity, EnterDroneView);
            }
            else if (Mode == RegionWorldMode.DroneRecon3D)
            {
                double dropE = Drone.Autopilot.TargetEastM;
                double dropS = Drone.Autopilot.TargetSouthM;
                if (DefinitelyCannotLand(dropE, dropS))
                {
                    ReportStatus(CannotLand);
                    return;
                }
                ReportStatus("降下地点の地形を準備しています…");
                Streamer.PrefetchDive(dropE, dropS);
                _terrainPreparation = StartCoroutine(PrepareDive(RegionWorldMode.OpenWorld3D, dropE, dropS, fromDrone: true));
            }
        }

        /// <summary>ドローン PiP 小窓の表示/非表示を切り替える。</summary>
        public void ToggleDronePiP()
        {
            if (Drone == null || (Mode != RegionWorldMode.Overview2D && Mode != RegionWorldMode.OpenWorld3D)) return;
            if (!Drone.IsActive)
            {
                if (Mode == RegionWorldMode.OpenWorld3D) DispatchDroneFromPlayer(DroneDisplayMode.PictureInPicture);
                else if (_explorer != null && _explorer.CanEnterWorld)
                    StartDiveToDrone(_explorer.CursorMapPosition.x, _explorer.CursorMapPosition.y, fullScreen: false);
                UpdatePipVisibility();
                return;
            }
            DroneDisplayMode next = Drone.CameraRig.DisplayMode == DroneDisplayMode.PictureInPicture
                ? DroneDisplayMode.Hidden
                : DroneDisplayMode.PictureInPicture;
            Drone.SetDisplayMode(next);
            if (Mode == RegionWorldMode.Overview2D && next == DroneDisplayMode.Hidden) ClearOverviewWorld();
            UpdatePipVisibility();
        }

        private bool StartDiveInternal(float mapX, float mapY, RegionWorldMode destination)
        {
            if (!PrepareWorldAt(mapX, mapY, out double targetE, out double targetS)) return false;
            if (destination == RegionWorldMode.OpenWorld3D && DefinitelyCannotLand(targetE, targetS))
            {
                ReportStatus(CannotLand);
                return false;
            }

            Mode = RegionWorldMode.Diving;
            Streamer.PrefetchDive(targetE, targetS, backdropOnly: true);
            QueueCloudSnapshot();
            SetMapInput(false);
            UpdatePipVisibility();
            Gamepad gamepad = Gamepad.current;
            string cancel = gamepad == null ? "Esc" : RegionGamepad.Label(gamepad.buttonEast);
            ReportStatus($"3D地形を準備しています…  [{cancel}] キャンセル");
            _terrainPreparation = StartCoroutine(PrepareDive(destination, targetE, targetS, fromDrone: false));
            return true;
        }

        private IEnumerator PrepareDive(RegionWorldMode destination, double east, double south, bool fromDrone)
        {
            float deadline = Time.realtimeSinceStartup + TerrainTimeoutSeconds;
            yield return null;
            while (!ReadyToDive(east, south, fromDrone) &&
                   Time.realtimeSinceStartup < deadline)
                yield return null;
            _terrainPreparation = null;

            if (!ReadyToDive(east, south, fromDrone))
            {
                if (!fromDrone) FinishOverview();
                ReportStatus("3D地形の準備が間に合いませんでした。もう一度お試しください。");
                yield break;
            }

            if (destination == RegionWorldMode.OpenWorld3D && !CanLandAt(east, south))
            {
                if (!fromDrone) FinishOverview();
                ReportStatus(CannotLand);
                yield break;
            }

            float ground = Streamer.SampleGroundHeight(east, south);
            if (!fromDrone)
            {
                EnsureAtmosphere(ground);
                ApplyCloudSnapshot();
            }
            if (_overviewCamera != null) _overviewCamera.enabled = false;
            WorldCamera.enabled = true;
            HideWorldMessage();
            Mode = RegionWorldMode.Diving;
            SetHudVisible(false);

            if (destination == RegionWorldMode.DroneRecon3D)
            {
                DispatchDrone(east - 750.0, south + 550.0, east, south, DroneDisplayMode.Hidden);
                Drone.FlightPaused = true;
                UpdatePipVisibility();
                DiveController.BeginDive(WorldCamera, _explorerCanvasGroup, Streamer, east, south,
                    GetVisible2DWidthMeters(), CameraPose(Drone.CameraRig.DroneCamera), _cloudCover * _cloudOpacity, _baseFogDensity, EnterDroneView);
                yield break;
            }

            Streamer.Coordinates.WorldToLocal(east, ground + 0.15f, south, out float x, out float y, out float z);
            var landing = new Vector3(x, y, z);
            float yaw = fromDrone ? Drone.Autopilot.Telemetry.HeadingDeg : 0f;
            Pose arrival = RegionOpenWorldCamera.FollowPose(landing, Locomotion.Foot.CameraHeightOffset,
                Locomotion.Foot.CameraDistance, yaw, 18f);
            Action enterGround = () =>
            {
                Locomotion.EnterOpenWorld(landing, yaw);
                Mode = RegionWorldMode.OpenWorld3D;
                SetHudVisible(true);
                UpdatePipVisibility();
            };
            if (fromDrone)
            {
                Pose departure = CameraPose(Drone.CameraRig.DroneCamera);
                Drone.SetDisplayMode(DroneDisplayMode.PictureInPicture);
                DiveController.BeginGroundApproach(WorldCamera, departure, arrival, _baseFogDensity, enterGround);
            }
            else
            {
                DiveController.BeginDive(WorldCamera, _explorerCanvasGroup, Streamer, east, south,
                    GetVisible2DWidthMeters(), arrival, _cloudCover * _cloudOpacity, _baseFogDensity, enterGround);
            }
        }

        private bool ReadyToDive(double east, double south, bool fromDrone)
        {
            return fromDrone ? Streamer.IsTargetNearReady(east, south) :
                Streamer.IsDiveBackdropReady() && (_cloudBuild == null || _cloudBuild.IsCompleted);
        }

        private void EnterDroneView()
        {
            Drone.SetDisplayMode(DroneDisplayMode.FullScreen);
            Drone.FlightPaused = false;
            WorldCamera.enabled = false;
            Mode = RegionWorldMode.DroneRecon3D;
            SetHudVisible(true);
            UpdatePipVisibility();
        }

        private bool CanLandAt(double east, double south)
        {
            return RegionLocomotionMath.CanStand(Streamer.SampleGroundType(east, south));
        }

        private bool DefinitelyCannotLand(double east, double south)
        {
            return !Streamer.Coordinates.ContainsWorld(east, south) || Streamer.IsOpenSea(east, south);
        }

        private void DispatchDroneFromPlayer(DroneDisplayMode display)
        {
            Vector3 pos = Locomotion.ActiveAgent.Root.position;
            Streamer.Coordinates.LocalToWorld(pos.x, pos.y, pos.z, out double east, out _, out double south);
            DispatchDrone(east, south, east + 500.0, south - 300.0, display);
        }

        private void DispatchDrone(double startE, double startS, double targetE, double targetS, DroneDisplayMode display)
        {
            double limit = Streamer.Coordinates.WorldSizeMeters;
            startE = Math.Clamp(startE, 0.0, limit);
            startS = Math.Clamp(startS, 0.0, limit);
            targetE = Math.Clamp(targetE, 0.0, limit);
            targetS = Math.Clamp(targetS, 0.0, limit);
            float altitude = Streamer.SampleGroundHeight(startE, startS) + 140f;
            Drone.Dispatch(startE, startS, altitude, targetE, targetS, display);
        }

        private static Pose CameraPose(Camera camera)
        {
            return new Pose(camera.transform.position, camera.transform.rotation);
        }

        private void SetMapInput(bool available)
        {
            if (_explorerCanvasGroup == null) return;
            _explorerCanvasGroup.interactable = available;
            _explorerCanvasGroup.blocksRaycasts = available;
        }

        private void FinishOverview()
        {
            Mode = RegionWorldMode.Overview2D;
            WorldCamera.enabled = false;
            if (_overviewCamera != null) _overviewCamera.enabled = true;
            if (_explorerCanvasGroup != null) _explorerCanvasGroup.alpha = 1f;
            SetMapInput(true);
            SetHudVisible(false);
            Locomotion.ResetWorld();
            HideWorldMessage();
            if (!Drone.IsActive || Drone.CameraRig.DisplayMode != DroneDisplayMode.PictureInPicture) ClearOverviewWorld();
            UpdatePipVisibility();
        }

        private void CancelPreparation()
        {
            if (_terrainPreparation == null) return;
            StopCoroutine(_terrainPreparation);
            _terrainPreparation = null;
        }

        internal void ClearOverviewWorld()
        {
            _cloudBuild = null;
            Drone.Deactivate();
            Locomotion.ResetWorld();
            Streamer.ClearAll();
            ReleaseAtmosphere();
            UpdatePipVisibility();
        }

        private void ReportStatus(string message)
        {
            if (Mode == RegionWorldMode.Overview2D || (_explorerCanvasGroup != null && _explorerCanvasGroup.alpha >= 1f))
                _explorer.ReportWorldMessage(message);
            else
            {
                _statusMessage = message;
                _statusUntil = Time.unscaledTime + 4f;
            }
        }

        private void HideWorldMessage()
        {
            _statusUntil = 0f;
            if (_explorer != null) _explorer.HideWorldMessage();
        }

        private bool PrepareWorldAt(float mapX, float mapY, out double targetEastM, out double targetSouthM)
        {
            targetEastM = 0.0;
            targetSouthM = 0.0;
            EnsureSubsystems();
            if (_explorer == null || !_explorer.CanEnterWorld || !_explorer.TryGetGroundViewInput(out RegionData region, out _,
                out _cloudCover, out _cloudTime, out _cloudCrossSeconds))
                return false;
            _cloudOpacity = _explorer.WorldCloudOpacity;

            if (Streamer.Region != region)
            {
                ClearOverviewWorld();
                var coords = RegionWorldCoordinates.ForRegion(region, mapX, mapY, 512.0);
                Streamer.Initialize(region, coords);
            }
            Streamer.Coordinates.MapToWorldMeters(mapX, mapY, out targetEastM, out targetSouthM);
            Streamer.Coordinates.WorldToLocal(targetEastM, 0f, targetSouthM, out float x, out _, out float z);
            Streamer.CheckAndRecenter(new Vector3(x, 0f, z));
            return Streamer.Coordinates.ContainsWorld(targetEastM, targetSouthM);
        }

        private void EnsureAtmosphere(float groundH)
        {
            using var atmosphereScope = AtmosphereMarker.Auto();
            ReleaseAtmosphere();
            _overviewSkybox = RenderSettings.skybox;
            var atmosphere = RegionAtmosphere.Create(RegionAtmosphere.Preset.ClearAfternoon);
            _baseFogDensity = atmosphere.FogDensity;
            atmosphere.PrepareSun();

            if (_sun == null)
            {
                var sunGo = new GameObject("Region World Sun");
                sunGo.transform.SetParent(transform, false);
                _sun = sunGo.AddComponent<Light>();
                _sun.type = LightType.Directional;
            }
            _sun.enabled = true;
            _atmosphereVolume = atmosphere.Apply(WorldCamera, _sun, groundH);
            _atmosphereVolume.transform.SetParent(transform, false);
            if (RenderSettings.skybox != _overviewSkybox) _worldSkybox = RenderSettings.skybox;

            if (_worldSkybox != null)
                _worldSkybox.SetVector("_RegionCloudView", new Vector4(groundH + 1500f, groundH + 2600f, groundH + 9000f, 60000f));
        }

        private void QueueCloudSnapshot()
        {
            _cloudBuild = null;
            if (_cloudOpacity <= 0f || _cloudCover <= 0.01f) return;
            RegionData region = Streamer.Region;
            int side = (region.Spec.Resolution + 1) / 2;
            var spec = new RegionCloudSkySpec
            {
                RegionSeed = region.Spec.Seed,
                Cover = _cloudCover,
                Time = _cloudTime,
                CrossSeconds = _cloudCrossSeconds,
                MapCloudSize = side,
                MetersPerMapUnit = (float)Streamer.Coordinates.MetersPerMapUnit,
                MinZ = -(float)Streamer.Coordinates.WorldSizeMeters,
                Size = (float)Streamer.Coordinates.WorldSizeMeters,
                Resolution = side,
            };
            float opacity = _cloudOpacity;
            _cloudBuild = Task.Run(() =>
            {
                RegionCloudSky.Build(spec, out float[] low, out float[] high);
                var pixels = new Color[low.Length];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color(low[i] * opacity, high[i] * opacity, 0f, 1f);
                return (pixels, side);
            });
        }

        private void ApplyCloudSnapshot()
        {
            if (_worldSkybox == null || _cloudBuild == null || !_cloudBuild.IsCompleted) return;
            using var cloudUploadScope = CloudUploadMarker.Auto();
            Task<(Color[] Pixels, int Side)> build = _cloudBuild;
            _cloudBuild = null;
            if (build.IsFaulted)
            {
                Debug.LogException(build.Exception.GetBaseException());
                return;
            }
            var (pixels, side) = build.Result;
            if (_cloudDensityTex != null) Destroy(_cloudDensityTex);
            _cloudDensityTex = new Texture2D(side, side, TextureFormat.RGBAHalf, true, true)
            {
                name = "Session Cloud Density",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                hideFlags = HideFlags.DontSave,
            };
            _cloudDensityTex.SetPixels(pixels);
            _cloudDensityTex.Apply(true, true);
            _worldSkybox.SetTexture("_RegionCloudMap", _cloudDensityTex);
            UpdateCloudCoordinates();
        }

        private void UpdateCloudCoordinates()
        {
            if (_worldSkybox == null || _cloudDensityTex == null) return;
            RegionWorldCoordinates coordinates = Streamer.Coordinates;
            float east = (float)coordinates.OriginEastM;
            float south = (float)coordinates.OriginSouthM;
            float size = (float)coordinates.WorldSizeMeters;
            _worldSkybox.SetVector("_RegionCloudTransform", new Vector4(-east, south - size, 1f / size, 1f));
            _worldSkybox.SetVector("_RegionCloudNoiseOffset", new Vector4(east, -south, 0f, 0f));
        }

        private void UpdateCloudDrift()
        {
            if (_worldSkybox == null || _cloudDensityTex == null) return;
            float elapsed = _explorer.WorldCloudTime - _cloudTime;
            RegionCloudFlow.Drift(RegionCloudFlow.Low, elapsed, _cloudCrossSeconds, out float lowX, out float lowZ);
            RegionCloudFlow.Drift(RegionCloudFlow.High, elapsed, _cloudCrossSeconds, out float highX, out float highZ);
            _worldSkybox.SetVector("_RegionCloudDrift", new Vector4(lowX, lowZ, highX, highZ));
        }

        private void ReleaseAtmosphere()
        {
            if (_sun != null) _sun.enabled = false;
            if (_atmosphereVolume != null)
            {
                Volume volume = _atmosphereVolume.GetComponent<Volume>();
                if (volume != null && volume.sharedProfile != null) Destroy(volume.sharedProfile);
                _atmosphereVolume.SetActive(false);
                Destroy(_atmosphereVolume);
                _atmosphereVolume = null;
            }
            if (_worldSkybox != null)
            {
                if (RenderSettings.skybox == _worldSkybox) RenderSettings.skybox = _overviewSkybox;
                Destroy(_worldSkybox);
                _worldSkybox = null;
            }
            if (_cloudDensityTex != null)
            {
                Destroy(_cloudDensityTex);
                _cloudDensityTex = null;
            }
        }

        private float GetVisible2DWidthMeters()
        {
            if (_explorer == null || _explorer.Region == null) return 30000f;
            float zoom = Mathf.Max(1f, _explorer.Zoom);
            return (_explorer.Region.SizeKm * 1000f) / zoom;
        }

        private void HandleOriginShifted(Vector3 localDelta)
        {
            if (WorldCamera != null) WorldCamera.transform.position += localDelta;
            if (Locomotion != null) Locomotion.ApplyOriginShift(localDelta);
            if (Drone != null) Drone.ApplyOriginShift(localDelta);
            Physics.SyncTransforms();

            UpdateCloudCoordinates();
        }

        private void Update()
        {
            if (Mode == RegionWorldMode.Overview2D) ApplyCloudSnapshot();
            UpdateCloudDrift();
            if (Mode == RegionWorldMode.Diving)
            {
                if (ReturnKeyPressed()) AscendToOverview2D();
            }
            else if (Mode == RegionWorldMode.OpenWorld3D)
            {
                Vector3 pos = Locomotion.ActiveAgent.Root.position;
                Streamer.CheckAndRecenter(pos);
                pos = Locomotion.ActiveAgent.Root.position;
                Streamer.Coordinates.LocalToWorld(pos.x, pos.y, pos.z,
                    out double we, out _, out double ws);
                DroneTelemetry? pip = Drone.IsActive
                    ? Drone.Autopilot.Telemetry : (DroneTelemetry?)null;
                Streamer.SetFocusWorld(we, ws, pip);

                Handle3DHotkeys();
                UpdateHudText(we, ws);
            }
            else if (Mode == RegionWorldMode.DroneRecon3D)
            {
                DroneTelemetry t = Drone.Autopilot.Telemetry;
                if (_terrainPreparation == null)
                {
                    Streamer.Coordinates.WorldToLocal(t.EastM, t.AltitudeM, t.SouthM,
                        out float lx, out float ly, out float lz);
                    Streamer.CheckAndRecenter(new Vector3(lx, ly, lz));
                    Streamer.SetFocusWorld(t.EastM, t.SouthM);
                }

                Handle3DHotkeys();
                UpdateHudText(t.EastM, t.SouthM);
            }
            else if (Mode == RegionWorldMode.Overview2D && Drone.IsActive && Drone.CameraRig.DisplayMode == DroneDisplayMode.PictureInPicture)
            {
                DroneTelemetry t = Drone.Autopilot.Telemetry;
                Streamer.Coordinates.WorldToLocal(t.EastM, t.AltitudeM, t.SouthM,
                    out float x, out float y, out float z);
                Streamer.CheckAndRecenter(new Vector3(x, y, z));
                Streamer.SetFocusWorld(t.EastM, t.SouthM);
            }

            if (_pipPanel != null && _pipPanel.activeSelf && Drone != null && Drone.IsActive)
            {
                DroneTelemetry dt = Drone.Autopilot.Telemetry;
                Gamepad gamepad = Gamepad.current;
                string close = gamepad == null ? "P" : RegionGamepad.Label(gamepad.rightShoulder);
                _pipCaption.text = "DRONE [" + dt.State + "]  AGL " + Mathf.RoundToInt(dt.AltitudeAgl)
                    + "m  |  " + Drone.Autopilot.IdleBehavior.DisplayName + $"  [{close}] 閉じる";
            }
        }

        private void Handle3DHotkeys()
        {
            if (ReturnKeyPressed())
            {
                AscendToOverview2D();
                return;
            }
            Gamepad gamepad = Gamepad.current;
            if (Input.GetKeyDown(KeyCode.V) || gamepad?.buttonNorth.wasPressedThisFrame == true)
            {
                ToggleDroneAndOpenWorld();
                return;
            }
            if (Input.GetKeyDown(KeyCode.P) || gamepad?.rightShoulder.wasPressedThisFrame == true)
            {
                ToggleDronePiP();
                return;
            }
            if (Drone != null && Drone.IsActive &&
                (Input.GetKeyDown(KeyCode.Tab) || gamepad?.leftShoulder.wasPressedThisFrame == true))
            {
                Drone.CycleIdleBehavior();
            }
        }

        private static bool ReturnKeyPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape) ||
                   keyboard?.mKey.wasPressedThisFrame == true || keyboard?.escapeKey.wasPressedThisFrame == true ||
                   Gamepad.current?.buttonEast.wasPressedThisFrame == true;
        }

        private void UpdateHudText(double eastM, double southM)
        {
            if (_hudModeText == null || Streamer == null || Streamer.Coordinates == null) return;
            Streamer.Coordinates.WorldMetersToMap(eastM, southM, out float mx, out float my);
            Gamepad gamepad = Gamepad.current;
            if (Mode == RegionWorldMode.DroneRecon3D)
            {
                DroneTelemetry t = Drone.Autopilot.Telemetry;
                _hudModeText.text = "ドローン上空モード  (" + t.State + " / " + Drone.Autopilot.IdleBehavior.DisplayName + ")";
                _hudStatusText.text = "X " + mx.ToString("0.00") + "  Y " + my.ToString("0.00")
                    + "  |  AGL " + Mathf.RoundToInt(t.AltitudeAgl) + " m  |  "
                    + (gamepad == null
                        ? "[V] 地上へ降下  [Tab] 旋回切替  [M/Esc] 2D地図へ戻る"
                        : $"[{RegionGamepad.Label(gamepad.buttonNorth)}] 地上へ降下  " +
                          $"[{RegionGamepad.Label(gamepad.leftShoulder)}] 旋回切替  [{RegionGamepad.Label(gamepad.buttonEast)}] 地図へ戻る");
            }
            else
            {
                bool driving = Locomotion.Mode == LocomotionMode.Vehicle;
                string loco = driving ? "バギー乗車中" : "徒歩";
                _hudModeText.text = "オープンワールド 3D  (" + loco + ")";
                string controls = gamepad == null
                    ? "[WASD] 移動  [右ドラッグ] 視点  "
                      + (driving ? "[F] 降車  [Space] ブレーキ  [R] 復帰  " : "[F] 乗車  [B] バギー呼出  ")
                      + "[V] ドローン  [P] PiP  [M/Esc] 地図へ戻る"
                    : DescribeGroundGamepad(gamepad, driving);
                _hudStatusText.text = "X " + mx.ToString("0.00") + "  Y " + my.ToString("0.00") + "  |  " + controls;
            }
            if (_statusUntil > Time.unscaledTime) _hudStatusText.text = _statusMessage;
        }

        private static string DescribeGroundGamepad(Gamepad gamepad, bool driving)
        {
            string action = RegionGamepad.Label(gamepad.buttonSouth);
            string interact = RegionGamepad.Label(gamepad.buttonWest);
            string movement = driving
                ? $"左スティック ハンドル  [{RegionGamepad.Label(gamepad.leftTrigger)}/{RegionGamepad.Label(gamepad.rightTrigger)}] 後退/前進  " +
                  $"[{action}] ブレーキ  [{interact}] 降車  [十字↓] 復帰"
                : $"左スティック 移動  [左押込] ダッシュ  [{action}] ジャンプ  [{interact}] 乗車  [十字↓] バギー呼出";
            return movement + $"  右スティック 視点  [{RegionGamepad.Label(gamepad.buttonNorth)}] ドローン  " +
                   $"[{RegionGamepad.Label(gamepad.rightShoulder)}] PiP  [{RegionGamepad.Label(gamepad.buttonEast)}] 地図へ戻る";
        }

        private void SetHudVisible(bool visible)
        {
            if (_hudBar != null) _hudBar.SetActive(visible);
        }

        private void UpdatePipVisibility()
        {
            if (_pipPanel == null || Drone == null) return;
            bool show = (Mode == RegionWorldMode.Overview2D || Mode == RegionWorldMode.OpenWorld3D) &&
                        Drone.IsActive && Drone.CameraRig.DisplayMode == DroneDisplayMode.PictureInPicture;
            _pipPanel.SetActive(show);
            if (show && _pipImage != null)
                _pipImage.texture = Drone.CameraRig.PipTexture;
        }

        private void BuildHudAndPipUi()
        {
            var canvasGo = new GameObject("RegionWorld3D HUD", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            _hudCanvas = canvasGo.GetComponent<Canvas>();
            _hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _hudCanvas.sortingOrder = 50;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            Font font = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic UI", "Meiryo UI", "Arial" }, 20);

            // 上部 HUD バー
            _hudBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
            _hudBar.transform.SetParent(canvasGo.transform, false);
            var barRect = (RectTransform)_hudBar.transform;
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.sizeDelta = new Vector2(0f, 64f);
            barRect.anchoredPosition = Vector2.zero;
            _hudBar.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.10f, 0.72f);

            _hudModeText = CreateHudText(barRect, "Mode", 18f, -6f, 900f, 26f, 20, new Color(0.95f, 0.72f, 0.30f), font);
            _hudStatusText = CreateHudText(barRect, "Status", 18f, -34f, 1880f, 24f, 15, new Color(0.92f, 0.92f, 0.90f), font);
            _hudBar.SetActive(false);

            // 右下 ドローン PiP 小窓
            _pipPanel = new GameObject("DronePiP", typeof(RectTransform), typeof(Image));
            _pipPanel.transform.SetParent(canvasGo.transform, false);
            var pipRect = (RectTransform)_pipPanel.transform;
            pipRect.anchorMin = new Vector2(1f, 0f);
            pipRect.anchorMax = new Vector2(1f, 0f);
            pipRect.pivot = new Vector2(1f, 0f);
            pipRect.sizeDelta = new Vector2(400f, 254f);
            pipRect.anchoredPosition = new Vector2(-24f, 82f);
            _pipPanel.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.10f, 0.85f);

            var imgGo = new GameObject("Feed", typeof(RectTransform), typeof(RawImage));
            imgGo.transform.SetParent(pipRect, false);
            var imgRect = (RectTransform)imgGo.transform;
            imgRect.anchorMin = new Vector2(0f, 0f);
            imgRect.anchorMax = new Vector2(1f, 1f);
            imgRect.offsetMin = new Vector2(4f, 28f);
            imgRect.offsetMax = new Vector2(-4f, -4f);
            _pipImage = imgGo.GetComponent<RawImage>();

            _pipCaption = CreateHudText(pipRect, "Caption", 8f, -228f, 384f, 22f, 13, Color.white, font);
            _pipPanel.SetActive(false);
        }

        private static Text CreateHudText(RectTransform parent, string name, float x, float y, float w, float h,
            int size, Color color, Font font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(w, h);
            var txt = go.GetComponent<Text>();
            txt.font = font;
            txt.fontSize = size;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.raycastTarget = false;
            return txt;
        }

        private void OnDestroy()
        {
            ReleaseAtmosphere();
        }
    }
}
