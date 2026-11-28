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
    public static class RegionPocketBeachMenu
    {
        private const string Menu = "Vastcore/Region Map/Pocket Beach Sample/";
        private static readonly string[] Shots = { "far", "mid", "pool" };

        [MenuItem(Menu + "Seed 8", false, 100)]
        public static void Open() => OpenSample(8);

        [MenuItem(Menu + "Seed 9", false, 101)]
        public static void OpenSecond() => OpenSample(9);

        [MenuItem(Menu + "Low Tide", false, 120)]
        private static void LowTide() => FindSample()?.SetTide(false);

        [MenuItem(Menu + "High Tide", false, 121)]
        private static void HighTide() => FindSample()?.SetTide(true);

        [MenuItem(Menu + "Low Tide", true)]
        [MenuItem(Menu + "High Tide", true)]
        private static bool HasSample() => FindSample() != null;

        private static RegionPocketBeach FindSample() => UnityEngine.Object.FindFirstObjectByType<RegionPocketBeach>();

        private static void OpenSample(int seed)
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                EditorUtility.DisplayProgressBar("Region Map", "岩礁とポケットビーチを生成しています…", 0.35f);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.nearClipPlane = 0.15f;
                camera.farClipPlane = 11800f;
                var sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
                var air = RegionAtmosphere.Create(RegionAtmosphere.Preset.ClearAfternoon);
                air.SunAltitudeDegrees = 38f;
                air.PrepareSun();
                air.Apply(camera, sun, 0f);
                sun.transform.rotation = Quaternion.Euler(38f, 43f, 0f);
                RegionData data = RegionGenerator.Generate(new RegionSpec { Seed = seed });
                var clock = Stopwatch.StartNew();
                RegionPocketBeach coast = RegionPocketBeach.Build(data, RegionPropCatalog.LoadDefault(), seed);
                Debug.Log(FormattableString.Invariant($"[PocketBeach] seed={seed} regionMap={coast.RegionalContact:F5} buildMs={clock.ElapsedMilliseconds} shelves={coast.ShelfModels} cobbles={coast.CobbleModels} reef={coast.ReefHeight:F2} lowReefChannel={coast.ReefChannelAtLow} poolLowConnected={coast.BasinConnectedAtLow} poolHighConnected={coast.BasinConnectedAtHigh} drySandLow={coast.LowDrySand:F1} drySandHigh={coast.HighDrySand:F1}"));
                Place(camera, coast, 1);
                SceneView.lastActiveSceneView?.AlignViewToObject(camera.transform);
                Selection.activeGameObject = coast.gameObject;
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
                string menu = Menu + "Seed " + seed;
                ShaderUtil.allowAsyncCompilation = false;
                if (!EditorApplication.ExecuteMenuItem(menu)) throw new InvalidOperationException("Could not open " + menu);
                var coast = FindSample();
                var camera = Camera.main;
                var sun = RenderSettings.sun != null ? RenderSettings.sun : GameObject.Find("Sun")?.GetComponent<Light>();
                if (coast == null || camera == null || sun == null)
                    throw new InvalidOperationException("The normal menu did not build the pocket beach, camera and sun.");

                string output = Path.GetFullPath(RegionMapMenu.Argument("-regionOut", "artifacts/region-map/pocket-beach"));
                Directory.CreateDirectory(output);
                coast.SetWaveTime(4.5f);
                RegionTerrainViewMenu.Capture(camera, output, "opened-from-menu.png");
                if (coast.BasinConnectedAtLow || !coast.BasinConnectedAtHigh || !coast.ReefChannelAtLow ||
                    coast.ReefHeight <= RegionPocketBeach.HighWater || coast.LowDrySand <= coast.HighDrySand ||
                    coast.ShelfModels < 4 || coast.CobbleModels < 3)
                    throw new InvalidOperationException("Expected an offshore reef, a pool that opens at high tide, a smaller high-tide beach and imported Props.");

                var pool = coast.transform.Find("Retained tide pool").gameObject;
                var air = RegionAtmosphere.Create(RegionAtmosphere.Preset.ClearAfternoon);
                foreach (string name in RegionMapMenu.Argument("-sunAltitudes", "38,60,20,10").Split(','))
                {
                    float angle = float.Parse(name, CultureInfo.InvariantCulture);
                    air.SunAltitudeDegrees = angle;
                    air.PrepareSun();
                    sun.transform.rotation = Quaternion.Euler(angle, 43f, 0f);
                    string path = Path.Combine(output, "sun-" + name);
                    Directory.CreateDirectory(path);
                    for (int i = 0; i < Shots.Length; i++)
                    {
                        Place(camera, coast, i);
                        Debug.Log(FormattableString.Invariant($"[PocketBeach] view={Shots[i]} angle={angle} eye={camera.transform.position:F3} fov={camera.fieldOfView:F1}"));
                        if (!EditorApplication.ExecuteMenuItem(Menu + "Low Tide") || !pool.activeSelf || coast.IsHighTide)
                            throw new InvalidOperationException("The low-tide menu did not reveal the retained pool.");
                        RegionTerrainViewMenu.Capture(camera, path, Shots[i] + "-low.png");
                        if (!EditorApplication.ExecuteMenuItem(Menu + "High Tide") || pool.activeSelf || !coast.IsHighTide)
                            throw new InvalidOperationException("The high-tide menu retained a separate pool surface.");
                        RegionTerrainViewMenu.Capture(camera, path, Shots[i] + "-high.png");
                    }
                }
                Debug.Log($"[PocketBeach] PASS normal menu {menu}; both tide menu items and matched views.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Place(Camera camera, RegionPocketBeach coast, int shot)
        {
            Vector3 eye, target;
            switch (shot)
            {
                case 0:
                    camera.fieldOfView = 51f;
                    eye = new Vector3(47f, 60f, -144f);
                    target = new Vector3(-5f, 0f, -14f);
                    break;
                case 1:
                    camera.fieldOfView = 51f;
                    eye = new Vector3(90f, coast.Seed == 9 ? 17.166f : 15.5f, 0f);
                    target = new Vector3(-4f, 0.85f, -19f);
                    break;
                case 2:
                    camera.fieldOfView = 48f;
                    Vector2 basin = coast.TidePool;
                    eye = new Vector3(basin.x + 9f, 4.8f, basin.y);
                    target = new Vector3(basin.x - 5f, 0.25f, basin.y - 5f);
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(shot));
            }
            eye.y = Mathf.Max(eye.y, coast.GroundHeight(eye.x, eye.z) + 1.6f);
            camera.transform.position = eye;
            camera.transform.LookAt(target);
        }
    }
}
