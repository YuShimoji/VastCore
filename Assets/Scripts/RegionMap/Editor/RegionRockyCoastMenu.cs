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
    public static class RegionRockyCoastMenu
    {
        [MenuItem("Vastcore/Region Map/Rocky Coast Sample/Seed 8")]
        public static void Open() => OpenSample(8);

        [MenuItem("Vastcore/Region Map/Rocky Coast Sample/Seed 9")]
        public static void OpenSecond() => OpenSample(9);

        private static void OpenSample(int seed)
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                EditorUtility.DisplayProgressBar("Region Map", "硬岩の岬と入り江を生成しています…", 0.35f);
                OpenStage(out Camera camera, out _, out _);
                RegionData region = RegionGenerator.Generate(new RegionSpec { Seed = seed });
                RegionRockyCoast.Result coast = Build(region, SeedSpec(seed), true, RegionPropCatalog.LoadDefault(), true);
                Place(camera, coast, 1);
                SceneView view = SceneView.lastActiveSceneView;
                if (view != null) view.AlignViewToObject(camera.transform);
                Selection.activeGameObject = coast.Root;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        public static void CaptureFromCommandLine() => Capture(false);

        public static void CaptureRockfallFromCommandLine() => Capture(true);

        private static void Capture(bool compareRockfall)
        {
            try
            {
                int seed = int.Parse(RegionMapMenu.Argument("-regionSeed", "8"), CultureInfo.InvariantCulture);
                var spec = SeedSpec(seed);
                spec.ShapeSeed = int.Parse(RegionMapMenu.Argument("-coastShapeSeed", seed.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
                spec.PropsSeed = int.Parse(RegionMapMenu.Argument("-coastPropsSeed", seed.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
                spec.CliffHeight = Mathf.Clamp(Number("-coastHeight", "35"), 12f, 60f);
                spec.ShelfWidth = Mathf.Clamp(Number("-coastShelfWidth", "27"), 12f, 55f);
                spec.FixedWaveTime = 0f;
                string output = Path.GetFullPath(RegionMapMenu.Argument("-regionOut", "artifacts/region-map/rocky-coast"));
                Directory.CreateDirectory(output);
                ShaderUtil.allowAsyncCompilation = false;
                OpenStage(out Camera camera, out Light sun, out RegionAtmosphere air);
                var watch = Stopwatch.StartNew();
                RegionData region = RegionGenerator.Generate(new RegionSpec { Seed = seed });
                Debug.Log($"[RockyCoast] seed={seed} shape={spec.ShapeSeed} props={spec.PropsSeed} region_ms={watch.ElapsedMilliseconds}");
                RegionPropCatalog catalog = RegionPropCatalog.LoadDefault();
                var before = Build(region, spec, compareRockfall, compareRockfall ? catalog : null, false);
                before.Root.SetActive(false);
                var after = Build(region, spec, true, catalog, true);
                RegionPropView.Result props = after.Props;
                if (after.RaisedCells == 0 || props == null || props.PlacedCount == 0 || props.RealModelCount != props.PlacedCount)
                    throw new InvalidOperationException("The coast sample needs formed cliffs and imported Props.");
                if (!Resources.Load<Shader>("RegionMap3D/RegionCoastWater").isSupported)
                    throw new InvalidOperationException("The coast water shader is unsupported.");
                if (compareRockfall && (before.View.Focus != after.View.Focus || after.LocalHeightDelta < 1f ||
                    after.RockfallProps == null || after.RockfallProps.RealModelCount < 5 ||
                    after.RockfallProps.RealModelCount != after.RockfallProps.PlacedCount))
                    throw new InvalidOperationException("The comparison needs a shared coast contact, a scar and imported debris.");

                foreach (string value in RegionMapMenu.Argument("-sunAltitudes", "60,38,20,10").Split(','))
                {
                    float altitude = float.Parse(value, CultureInfo.InvariantCulture);
                    air.SunAltitudeDegrees = altitude;
                    air.PrepareSun();
                    sun.transform.rotation = compareRockfall
                        ? Quaternion.Euler(altitude, -spec.SeaYaw + 37f, 0f)
                        : RegionTerrainView.SunRotation(altitude);
                    string folder = Path.Combine(output, "sun-" + value);
                    Directory.CreateDirectory(folder);
                    for (int i = 0; i < ViewNames.Length; i++)
                    {
                        Place(camera, after, i);
                        Debug.Log(FormattableString.Invariant($"[RockyCoast] sun={altitude} view={ViewNames[i]} eye={camera.transform.position:F3} target_shore={after.View.Focus:F3}"));
                        before.Root.SetActive(true);
                        after.Root.SetActive(false);
                        RegionTerrainViewMenu.Capture(camera, folder, ViewNames[i] + "-before.png");
                        before.Root.SetActive(false);
                        after.Root.SetActive(true);
                        RegionTerrainViewMenu.Capture(camera, folder, ViewNames[i] + "-after.png");
                    }
                }
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static readonly string[] ViewNames = { "far", "mid", "near" };

        private static void Place(Camera camera, RegionRockyCoast.Result coast, int view)
        {
            camera.fieldOfView = view == 2 ? 53f : 45f;
            switch (view)
            {
                case 0: RegionTerrainView.Place(camera.transform, coast.View, -28f, 22f, 1160f); break;
                case 1:
                    RegionTerrainView.Place(camera.transform, coast.View, -12f, 15f, 215f);
                    camera.transform.LookAt(coast.View.Focus + Vector3.up * 14f);
                    break;
                case 2:
                    float yaw = coast.View.FacingYawDegrees * Mathf.Deg2Rad;
                    Vector3 inland = new Vector3(-Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
                    camera.transform.position = coast.View.Focus - inland * 20f + Vector3.up * 3.4f;
                    camera.transform.LookAt(coast.View.Focus + inland * (coast.CliffFootMeters * 0.50f) + Vector3.up * 7f);
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(view));
            }
        }

        private static RegionRockyCoastSpec SeedSpec(int seed)
        {
            switch (seed)
            {
                case 8: return new RegionRockyCoastSpec();
                case 9: return new RegionRockyCoastSpec
                {
                    MapX = 4.345703f, MapY = 57.128906f, SeaYaw = -38.66f, ShapeSeed = 9, PropsSeed = 9,
                };
                default: throw new ArgumentOutOfRangeException(nameof(seed), "The rocky coast samples use surveyed sites for seeds 8 and 9.");
            }
        }

        private static float Number(string key, string fallback) =>
            float.Parse(RegionMapMenu.Argument(key, fallback), CultureInfo.InvariantCulture);

        private static void OpenStage(out Camera camera, out Light sun, out RegionAtmosphere air)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = 45f;
            camera.nearClipPlane = 0.35f;
            camera.farClipPlane = 14000f;
            sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            air = RegionAtmosphere.Create(RegionAtmosphere.Preset.ClearAfternoon);
            air.PrepareSun();
            air.Apply(camera, sun, 0f);
        }

        private static RegionRockyCoast.Result Build(RegionData region, RegionRockyCoastSpec spec, bool shape, RegionPropCatalog catalog, bool refine)
        {
            var watch = Stopwatch.StartNew();
            RegionRockyCoast.Result result = RegionRockyCoast.Build(region, spec, shape, catalog, refine);
            RegionPropView.Result props = result.Props;
            Debug.Log(FormattableString.Invariant(
                $"[RockyCoast] shape={shape} refined={refine} ms={watch.ElapsedMilliseconds} shore_agreement={result.ShoreAgreement:P2} foot_m={result.CliffFootMeters:F1} rise_m={result.CliffRiseMeters:F1} joint_yaw={result.JointYaw:F1} grid_m={result.LocalStepMeters:F2} added_triangles={result.LocalTriangles} max_local_change_m={result.LocalHeightDelta:F1} props={props?.RealModelCount ?? 0} rockfall={result.RockfallProps?.RealModelCount ?? 0} {result.RockfallProps?.RealSummary}"));
            return result;
        }
    }
}
