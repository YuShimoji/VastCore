using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Vastcore.RegionMap;

namespace Vastcore.Tests.PlayMode
{
    public class RegionSeamlessWorldPlayModeTests
    {
        private GameObject _root;
        private Scene _explorerScene;
        private Scene _previousScene;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }
            if (_explorerScene.IsValid() && _explorerScene.isLoaded)
            {
                SceneManager.SetActiveScene(_previousScene);
                yield return SceneManager.UnloadSceneAsync(_explorerScene);
                _explorerScene = default;
            }
            yield return null;
        }

        [UnityTest]
        [Timeout(150000)]
        public IEnumerator ExplorerButtons_DiveAndReconRoundTrip_PreserveMapAndGround()
        {
            const string scenePath = "Assets/Scenes/RegionExplorer.unity";
            _previousScene = SceneManager.GetActiveScene();
            yield return SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
            _explorerScene = SceneManager.GetSceneByPath(scenePath);
            SceneManager.SetActiveScene(_explorerScene);

            RegionExplorer explorer = Object.FindFirstObjectByType<RegionExplorer>();
            yield return WaitFor(() => explorer.IsReady, 90f, "The exploration scene should finish generating its map");
            RegionWorldSession world = explorer.WorldSession;
            CanvasGroup map = explorer.transform.Find("Region Explorer Canvas").GetComponent<CanvasGroup>();
            RegionData region = explorer.Region;
            explorer.MoveCursor(0f, 0f);
            ClickTool(map, "3Dへダイブ");
            Assert.That(world.Mode, Is.EqualTo(RegionWorldMode.Overview2D), "An ocean point must keep the map in control");
            Assert.IsTrue(map.interactable && map.blocksRaycasts);

            RegionSite site = region.Sites[0];
            explorer.MoveCursor(site.MapX, site.MapY);

            world.Streamer.MaxConcurrentBuilds = 0;
            ClickTool(map, "3Dへダイブ");
            yield return null;
            Assert.That(world.Mode, Is.EqualTo(RegionWorldMode.Diving));
            Assert.IsFalse(world.Locomotion.Foot.IsActive, "The walker must stay out of the world while its collider is missing");
            world.AscendToOverview2D();
            Assert.That(world.Mode, Is.EqualTo(RegionWorldMode.Overview2D));
            Assert.IsTrue(map.interactable && map.blocksRaycasts);
            world.Streamer.MaxConcurrentBuilds = 3;

            ClickTool(map, "3Dへダイブ");
            Assert.That(world.Mode, Is.EqualTo(RegionWorldMode.Diving));
            Assert.IsFalse(map.interactable || map.blocksRaycasts, "The map must release input as soon as the dive begins");
            Assert.IsFalse(explorer.OpenFootpath(), "The footpath cannot start during the dive");
            yield return WaitFor(() => world.Mode == RegionWorldMode.OpenWorld3D, 25f, "The dive should hand off to a grounded walker");
            Assert.IsTrue(world.Locomotion.Foot.IsActive);
            Assert.IsTrue(world.Locomotion.SummonBuggyNearby());

            var follow = world.WorldCamera.GetComponent<RegionOpenWorldCamera>();
            follow.YawDegrees = 112f;
            follow.Distance = 24f;
            yield return null;
            Vector3 foot = world.Locomotion.Foot.Root.position;
            world.Streamer.Coordinates.LocalToWorld(foot.x, foot.y, foot.z, out double east, out _, out double south);
            Assert.IsTrue(world.Streamer.IsTargetNearReady(east, south));
            world.Streamer.Coordinates.WorldMetersToMap(east, south, out float mapX, out float mapY);
            Vector3 departure = world.WorldCamera.transform.position;
            Quaternion departureRotation = world.WorldCamera.transform.rotation;
            world.AscendToOverview2D();
            Assert.That(Vector3.Distance(world.WorldCamera.transform.position, departure), Is.LessThan(0.01f));
            Assert.That(Quaternion.Angle(world.WorldCamera.transform.rotation, departureRotation), Is.LessThan(0.05f));
            Assert.IsFalse(map.interactable || map.blocksRaycasts);
            yield return WaitFor(() => world.Mode == RegionWorldMode.Overview2D, 15f, "The ascent should return control to the same map");
            Assert.IsTrue(map.interactable && map.blocksRaycasts);
            Assert.AreSame(region, explorer.Region);
            Assert.IsFalse(world.WorldCamera.enabled);
            Assert.That(explorer.CursorMapPosition.x, Is.EqualTo(mapX).Within(0.0001f));
            Assert.That(explorer.CursorMapPosition.y, Is.EqualTo(mapY).Within(0.0001f));

            ClickTool(map, "ドローン派遣");
            yield return WaitFor(() => world.Mode == RegionWorldMode.DroneRecon3D, 25f, "The map button should start full-screen recon");
            Assert.IsTrue(world.Drone.CameraRig.DroneCamera.enabled);
            world.ToggleDroneAndOpenWorld();
            yield return WaitFor(() => world.Mode == RegionWorldMode.OpenWorld3D, 25f, "Recon should hand off to the ground with terrain ready");
            Assert.That(world.Drone.CameraRig.DisplayMode, Is.EqualTo(DroneDisplayMode.PictureInPicture));
            Assert.IsTrue(world.Locomotion.Foot.IsActive);
            world.AscendToOverview2D();
            yield return WaitFor(() => world.Mode == RegionWorldMode.Overview2D, 15f, "Recon and ground should both return to the map");
            Assert.IsTrue(map.interactable && map.blocksRaycasts);
            Assert.AreSame(region, explorer.Region);
            Assert.IsTrue(world.Drone.IsActive);
            ClickTool(map, "別の地域");
            Assert.IsFalse(world.Drone.IsActive, "The previous region's feed must end before generating another map");
            Assert.IsNull(world.Streamer.Region);
            yield return WaitFor(() => explorer.IsReady, 90f, "A second region should become available");
            Assert.AreNotSame(region, explorer.Region);
        }

        private static IEnumerator WaitFor(System.Func<bool> condition, float seconds, string message)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsTrue(condition(), message);
        }

        private static void ClickTool(CanvasGroup map, string label)
        {
            Canvas.ForceUpdateCanvases();
            Button button = map.transform.Find(label).GetComponent<Button>();
            var rect = (RectTransform)button.transform;
            Vector2 position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var click = new PointerEventData(EventSystem.current) { position = position, button = PointerEventData.InputButton.Left };
            var targets = new List<RaycastResult>();
            EventSystem.current.RaycastAll(click, targets);
            Assert.IsNotEmpty(targets, label);
            Assert.AreEqual(button.gameObject, ExecuteEvents.GetEventHandler<IPointerClickHandler>(targets[0].gameObject), label);
            ExecuteEvents.Execute(button.gameObject, click, ExecuteEvents.pointerClickHandler);
        }

        [UnityTest]
        public IEnumerator Streamer_BuildsColliderChunks_AndHandlesFloatingOriginShift()
        {
            RegionData region = RegionGenerator.Generate(new RegionSpec { Seed = 9, Resolution = 128, SizeKm = 256f });
            var coords = RegionWorldCoordinates.ForRegion(region, 50f, 50f, 512.0);
            coords.MapToWorldMeters(50f, 50f, out double eastM, out double southM);

            _root = new GameObject("PlayMode Streamer Test");
            var streamer = _root.AddComponent<RegionChunkStreamer>();
            streamer.FrameBudgetMs = 20f;
            streamer.Initialize(region, coords);
            streamer.PrefetchDive(eastM, southM);

            float timeout = 10f;
            while (!streamer.IsTargetNearReady(eastM, southM) && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsTrue(streamer.IsTargetNearReady(eastM, southM), "Near chunk should finish loading in PlayMode");
            Assert.That(streamer.LoadedChunkCount, Is.GreaterThanOrEqualTo(1));

            // Verify MeshCollider exists and can be raycasted from above
            coords.WorldToLocal(eastM, 0f, southM, out float lx, out _, out float lz);
            float expectedY = streamer.SampleGroundHeight(eastM, southM);
            bool hitGround = Physics.Raycast(new Vector3(lx, expectedY + 120f, lz), Vector3.down, out RaycastHit hit, 300f);
            Assert.IsTrue(hitGround, "Streamed Ring 0 MeshCollider should receive physics raycasts");
            Assert.That(hit.point.y, Is.EqualTo(expectedY).Within(2.5f));

            // Verify Floating Origin shift fires and moves loaded chunks
            bool shiftedEvent = false;
            Vector3 shiftDelta = Vector3.zero;
            streamer.OnOriginShifted += d => { shiftedEvent = true; shiftDelta = d; };
            bool recentered = streamer.CheckAndRecenter(new Vector3(2500f, 0f, -1500f));
            Assert.IsTrue(recentered);
            Assert.IsTrue(shiftedEvent);
            Assert.That(shiftDelta.sqrMagnitude, Is.GreaterThan(1000000f));
        }

        [UnityTest]
        public IEnumerator DroneController_Dispatches_Loiters_AndSwitchesPiPAndFullScreen()
        {
            RegionData region = RegionGenerator.Generate(new RegionSpec { Seed = 9, Resolution = 128, SizeKm = 256f });
            var coords = RegionWorldCoordinates.ForRegion(region, 50f, 50f, 512.0);
            coords.MapToWorldMeters(50f, 50f, out double baseE, out double baseS);

            _root = new GameObject("PlayMode Drone Test");
            var streamer = _root.AddComponent<RegionChunkStreamer>();
            streamer.Initialize(region, coords);

            var droneGo = new GameObject("Drone");
            droneGo.transform.SetParent(_root.transform, false);
            var drone = droneGo.AddComponent<DroneController>();
            drone.Bind(streamer);

            double targetE = baseE + 200.0;
            double targetS = baseS;
            float startAlt = streamer.SampleGroundHeight(baseE, baseS) + 120f;
            drone.Dispatch(baseE, baseS, startAlt, targetE, targetS, DroneDisplayMode.PictureInPicture);

            Assert.IsTrue(drone.IsActive);
            Assert.That(drone.CameraRig.DisplayMode, Is.EqualTo(DroneDisplayMode.PictureInPicture));
            Assert.IsNotNull(drone.CameraRig.PipTexture);
            Assert.AreSame(drone.CameraRig.PipTexture, drone.CameraRig.DroneCamera.targetTexture);

            // Step several frames and switch idle behavior + display mode
            for (int i = 0; i < 15; i++) yield return null;

            string switchedName = drone.CycleIdleBehavior();
            Assert.IsNotEmpty(switchedName);
            Assert.That(drone.Autopilot.IdleBehavior.Id, Is.EqualTo("figure8"));

            drone.SetDisplayMode(DroneDisplayMode.FullScreen);
            Assert.IsNull(drone.CameraRig.DroneCamera.targetTexture);
            Assert.IsTrue(drone.CameraRig.DroneCamera.enabled);

            drone.Deactivate();
            Assert.IsFalse(drone.IsActive);
            Assert.IsFalse(drone.CameraRig.DroneCamera.enabled);
        }

        [UnityTest]
        public IEnumerator LocomotionManager_EntersOpenWorld_SummonsAndMountsBuggy()
        {
            RegionData region = RegionGenerator.Generate(new RegionSpec { Seed = 9, Resolution = 128, SizeKm = 256f });
            var coords = RegionWorldCoordinates.ForRegion(region, 50f, 50f, 512.0);
            coords.MapToWorldMeters(50f, 50f, out double eastM, out double southM);

            _root = new GameObject("PlayMode Locomotion Test");
            var streamer = _root.AddComponent<RegionChunkStreamer>();
            streamer.FrameBudgetMs = 20f;
            streamer.Initialize(region, coords);
            streamer.PrefetchDive(eastM, southM);

            float timeout = 10f;
            while (!streamer.IsTargetNearReady(eastM, southM) && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            var camGo = new GameObject("Follow Camera");
            camGo.transform.SetParent(_root.transform, false);
            camGo.AddComponent<Camera>();
            var followCam = camGo.AddComponent<RegionOpenWorldCamera>();

            var loco = _root.AddComponent<RegionLocomotionManager>();
            loco.Initialize(streamer, followCam);

            float gy = streamer.SampleGroundHeight(eastM, southM);
            coords.WorldToLocal(eastM, gy + 0.5f, southM, out float lx, out float ly, out float lz);
            loco.EnterOpenWorld(new Vector3(lx, ly, lz), 0f);
            Assert.That(loco.Mode, Is.EqualTo(LocomotionMode.OnFoot));
            Assert.IsTrue(loco.Foot.IsActive);

            // Summon buggy and mount
            Assert.IsTrue(loco.SummonBuggyNearby());
            Assert.IsTrue(loco.Buggy.IsSpawned);

            // Move foot next to buggy so mount is within MountRangeMeters
            loco.Foot.Teleport(loco.Buggy.Root.position + new Vector3(1.5f, 0f, 0f), 0f);
            Assert.IsTrue(loco.ToggleMountVehicle());
            Assert.That(loco.Mode, Is.EqualTo(LocomotionMode.Vehicle));
            Assert.IsTrue(loco.Buggy.IsActive);

            // Run a few physics steps to ensure 4-wheel Raycast suspension doesn't throw or sink
            for (int i = 0; i < 6; i++) yield return new WaitForFixedUpdate();
            Assert.That(loco.Buggy.Root.position.y, Is.GreaterThanOrEqualTo(gy - 10f));

            // Dismount back to OnFoot
            Assert.IsTrue(loco.ToggleMountVehicle());
            Assert.That(loco.Mode, Is.EqualTo(LocomotionMode.OnFoot));
            Assert.IsTrue(loco.Foot.IsActive);
        }
    }
}
