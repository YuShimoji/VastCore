using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Vastcore.RegionMap.Editor
{
    public static class RegionDuneBeachMenu
    {
        private const string Menu = "Vastcore/Region Map/Dune Beach Sample/";
        private static readonly string[] Shots = { "far", "mid", "wrack" };

        [MenuItem(Menu + "Seed 8", false, 100)]
        private static void Open() => OpenSample(8);

        [MenuItem(Menu + "Seed 9", false, 101)]
        private static void OpenSecond() => OpenSample(9);

        [MenuItem(Menu + "Low Tide", false, 120)]
        private static void LowTide() => FindSample()?.SetTide(false);

        [MenuItem(Menu + "High Tide", false, 121)]
        private static void HighTide() => FindSample()?.SetTide(true);

        [MenuItem(Menu + "Low Tide", true)]
        [MenuItem(Menu + "High Tide", true)]
        private static bool HasSample() => FindSample() != null;

        private static RegionDuneBeach FindSample() => UnityEngine.Object.FindFirstObjectByType<RegionDuneBeach>();

        private static void OpenSample(int seed)
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                EditorUtility.DisplayProgressBar("Region Map", "砂浜と砂丘を生成しています…", 0.35f);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.nearClipPlane = 0.15f;
                camera.farClipPlane = 11800f;
                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                SetSun(camera, sun, 38f);
                RegionData data = RegionGenerator.Generate(new RegionSpec { Seed = seed });
                var clock = Stopwatch.StartNew();
                var beach = RegionDuneBeach.Build(data, RegionPropCatalog.LoadDefault(), seed);
                Debug.Log(FormattableString.Invariant($"[DuneBeach] seed={seed} regionMap={beach.RegionalContact:F5} buildMs={clock.ElapsedMilliseconds} grass={beach.GrassCount} wrack={beach.WrackCount} wood={beach.DriftwoodCount} cobbles={beach.CobbleCount} lowestWrack={beach.LowestWrack:F2} grassMargin={beach.ClosestGrassToWrack:F2} connected={beach.ConnectedAtBothTides} dryLow={beach.LowDrySand:F1} dryHigh={beach.HighDrySand:F1} shoreShift={beach.HighWaterline - beach.LowWaterline:F2}"));
                Place(camera, beach, 1);
                SceneView.lastActiveSceneView?.AlignViewToObject(camera.transform);
                Selection.activeGameObject = beach.gameObject;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public static void CaptureFromCommandLine()
        {
            try
            {
                int seed = int.Parse(RegionMapMenu.Argument("-regionSeed", "8"), CultureInfo.InvariantCulture);
                string entry = Menu + "Seed " + seed;
                ShaderUtil.allowAsyncCompilation = false;
                if (!EditorApplication.ExecuteMenuItem(entry)) throw new InvalidOperationException("Could not open " + entry);
                var beach = FindSample();
                var camera = Camera.main;
                var sun = RenderSettings.sun != null ? RenderSettings.sun : GameObject.Find("Sun")?.GetComponent<Light>();
                if (beach == null || camera == null || sun == null)
                    throw new InvalidOperationException("The normal menu did not build the beach, camera and sun.");
                if (!beach.ConnectedAtBothTides || beach.HighWaterline - beach.LowWaterline < 6f ||
                    beach.LowDrySand <= beach.HighDrySand || beach.LowestWrack <= RegionPocketBeach.HighWater ||
                    beach.ClosestGrassToWrack < 3f || beach.GrassCount < 200 || beach.WrackCount < 60 ||
                    beach.DriftwoodCount < 1 || beach.CobbleCount < 2)
                    throw new InvalidOperationException("Expected a moving waterline, fixed wrack above high water, and clustered landward grass.");

                string output = Path.GetFullPath(RegionMapMenu.Argument("-regionOut", "artifacts/region-map/dune-beach"));
                Directory.CreateDirectory(output);
                beach.SetWaveTime(4.5f);
                RegionTerrainViewMenu.Capture(camera, output, "opened-from-menu.png");
                foreach (string name in RegionMapMenu.Argument("-sunAltitudes", "38,60,20,10").Split(','))
                {
                    float angle = float.Parse(name, CultureInfo.InvariantCulture);
                    SetSun(camera, sun, angle);
                    string path = Path.Combine(output, "sun-" + name);
                    Directory.CreateDirectory(path);
                    for (int i = 0; i < Shots.Length; i++)
                    {
                        Place(camera, beach, i);
                        Debug.Log(FormattableString.Invariant($"[DuneBeach] view={Shots[i]} angle={angle} eye={camera.transform.position:F3} pitch={camera.transform.eulerAngles.x:F2} yaw={camera.transform.eulerAngles.y:F2} fov={camera.fieldOfView:F1}"));
                        if (!EditorApplication.ExecuteMenuItem(Menu + "Low Tide") || beach.IsHighTide)
                            throw new InvalidOperationException("The low-tide menu did not move the shore.");
                        RegionTerrainViewMenu.Capture(camera, path, Shots[i] + "-low.png");
                        if (!EditorApplication.ExecuteMenuItem(Menu + "High Tide") || !beach.IsHighTide)
                            throw new InvalidOperationException("The high-tide menu did not move the shore.");
                        RegionTerrainViewMenu.Capture(camera, path, Shots[i] + "-high.png");
                    }
                }
                Debug.Log($"[DuneBeach] PASS normal menu {entry}; both tide menu items and matched views.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void SetSun(Camera camera, Light sun, float angle)
        {
            var air = RegionAtmosphere.Create(RegionAtmosphere.Preset.ClearAfternoon);
            air.SunAltitudeDegrees = angle;
            air.PrepareSun();
            air.Apply(camera, sun, 0f);
            sun.transform.rotation = Quaternion.Euler(angle, 67f, 0f);
        }

        private static void Place(Camera camera, RegionDuneBeach beach, int shot)
        {
            Vector3 eye, look;
            switch (shot)
            {
                case 0:
                    camera.fieldOfView = 53f;
                    eye = new Vector3(161f, 65f, -109f);
                    look = new Vector3(-249f, -64f, 142f);
                    break;
                case 1:
                    camera.fieldOfView = 48f;
                    eye = new Vector3(116f, 5.55f, beach.ShoreAt(116f) + 13.5f);
                    look = new Vector3(-100f, -12.1f, -10.7f);
                    break;
                case 2:
                    camera.fieldOfView = 51f;
                    // Heights recorded at the initial wrack views keep both takes on the same tripod.
                    eye = new Vector3(101f, beach.Seed == 8 ? 3.363f : 3.379f, beach.ShoreAt(101f) + 20.3f);
                    look = new Vector3(-25f, -2.4f, -2.1f);
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(shot));
            }
            camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look));
        }
    }
}
