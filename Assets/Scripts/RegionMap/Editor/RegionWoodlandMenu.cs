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
    public static class RegionWoodlandMenu
    {
        [MenuItem("Vastcore/Region Map/Woodland Samples/Forest")]
        public static void OpenForest() => Open(RegionWoodlandPreset.Forest);

        [MenuItem("Vastcore/Region Map/Woodland Samples/Rainforest")]
        public static void OpenRainforest() => Open(RegionWoodlandPreset.Rainforest);

        [MenuItem("Vastcore/Region Map/Woodland Samples/Nature Park")]
        public static void OpenPark() => Open(RegionWoodlandPreset.NaturePark);

        private static void Open(RegionWoodlandPreset preset)
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                EditorUtility.DisplayProgressBar("Woodland Samples", "Generating the regional terrain and vegetation", 0.4f);
                int seed = Number("-regionSeed", 8);
                RegionData region = RegionGenerator.Generate(new RegionSpec { Seed = seed });
                Build(region, preset, Number("-woodlandLayoutSeed", seed),
                    Number("-woodlandShapeSeed", seed), out RegionTerrainView.LookResult look, out Camera camera);
                Aim(camera, look.Scene, look.Scene.Spec.Mid);
                if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.AlignViewToObject(camera.transform);
                Selection.activeGameObject = look.Root;
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
                string output = Path.GetFullPath(RegionMapMenu.Argument("-regionOut", "artifacts/region-map/woodland"));
                Directory.CreateDirectory(output);
                string preset = RegionMapMenu.Argument("-woodlandPreset", "all");
                RegionWoodlandPreset[] presets = string.Equals(preset, "all", StringComparison.OrdinalIgnoreCase) ?
                    (RegionWoodlandPreset[])Enum.GetValues(typeof(RegionWoodlandPreset)) :
                    new[] { Enum.Parse<RegionWoodlandPreset>(preset, true) };
                string[] sun = RegionMapMenu.Argument("-sunAltitudes", "60,38,20,10").Split(',');
                string[] seeds = RegionMapMenu.Argument("-woodlandSeeds", RegionMapMenu.Argument("-regionSeed", "8,9")).Split(',');
                ShaderUtil.allowAsyncCompilation = false;
                RegionTerrainViewMenu.UseBuildQuality();

                foreach (string value in seeds)
                {
                    int seed = int.Parse(value, CultureInfo.InvariantCulture);
                    var watch = Stopwatch.StartNew();
                    RegionData region = RegionGenerator.Generate(new RegionSpec { Seed = seed });
                    Debug.Log($"[Woodland] region seed={seed} build_ms={watch.ElapsedMilliseconds}");
                    foreach (RegionWoodlandPreset profile in presets)
                    {
                        RegionWoodlandView.Result sample = Build(region, profile, Number("-woodlandLayoutSeed", seed),
                            Number("-woodlandShapeSeed", seed), out RegionTerrainView.LookResult look, out Camera camera);
                        string folder = Path.Combine(output, $"seed-{seed}", profile.ToString());
                        Capture(folder, sun, sample, look, camera);
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

        private static RegionWoodlandView.Result Build(RegionData region, RegionWoodlandPreset preset, int layoutSeed,
            int shapeSeed, out RegionTerrainView.LookResult look, out Camera camera)
        {
            int site = RegionWoodlandLayout.SelectSite(region);
            var spec = RegionWoodlandLayout.LookSpec(preset, region.MapXFromCell(site), region.MapYFromCell(site));
            spec.Far = new RegionLookViewpoint("far", -178f, 267f, 107f, 1f, -65f, 8f, 45f);
            spec.Mid = new RegionLookViewpoint("mid", 7.85f, 24f, 1.70f, 0.15f, -5f, 3.30f, 53f);
            spec.Near = new RegionLookViewpoint("near", 6.8f, 7.1f, 1.46f, -2.7f, 13.6f, 0.51f, 49f);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorUtility.UnloadUnusedAssetsImmediate();
            RegionAtmosphere atmosphere = RegionAtmosphere.Create(RegionAtmosphere.Preset.ClearAfternoon);
            atmosphere.PrepareSun();
            var timer = Stopwatch.StartNew();
            look = RegionTerrainViewMenu.CreateLookStage(region, spec, atmosphere, out camera);
            long terrainMs = timer.ElapsedMilliseconds;
            timer.Restart();
            var layout = RegionWoodlandLayout.Build(look.Scene, preset, layoutSeed, shapeSeed);
            RegionWoodlandView.Result sample = RegionWoodlandView.AddTo(look, layout);
            if (layout.Canopy.Count < 50 || layout.Understory.Count < 50)
                throw new InvalidOperationException("The selected site has insufficient supported woodland placements.");
            Debug.Log(FormattableString.Invariant(
                $"[Woodland] {preset} seed={region.Spec.Seed} layout={layoutSeed} shapes={shapeSeed} source={region.Biome[site]} x={spec.CenterMapX:F4} y={spec.CenterMapY:F4} ") +
                $"canopy={layout.Canopy.Count} understory={layout.Understory.Count} canopy_tris={sample.CanopyTriangles} " +
                $"understory_tris={sample.UnderstoryTriangles} real_props={sample.RealProps} base_ms={terrainMs} woodland_ms={timer.ElapsedMilliseconds}");
            return sample;
        }

        private static void Capture(string folder, string[] sun, RegionWoodlandView.Result sample,
            RegionTerrainView.LookResult look, Camera camera)
        {
            Aim(camera, look.Scene, look.Scene.Spec.Mid);
            RegionTerrainViewMenu.Capture(camera, folder, null);
            foreach (string value in sun)
            {
                float degrees = float.Parse(value, CultureInfo.InvariantCulture);
                GameObject.Find("Sun").transform.rotation = RegionTerrainView.SunRotation(degrees);
                string destination = Path.Combine(folder, "sun-" + value);
                Directory.CreateDirectory(destination);
                foreach (RegionLookViewpoint view in look.Scene.Spec.Viewpoints())
                {
                    Aim(camera, look.Scene, view);
                    sample.Show(false, false);
                    RegionTerrainViewMenu.Capture(camera, destination, $"{view.Name}-baseline.png");
                    if (degrees == 38f)
                    {
                        sample.Show(true, false);
                        RegionTerrainViewMenu.Capture(camera, destination, $"{view.Name}-canopy.png");
                    }
                    sample.Show(true, true);
                    RegionTerrainViewMenu.Capture(camera, destination, $"{view.Name}-full.png");
                }
                if (degrees == 38f && look.Scene.PrimaryBiome == RegionBiome.Forest)
                {
                    var forestFloor = new RegionLookViewpoint("forest-floor", 1.9f, 7.4f, 0.98f, 4.65f, 1.7f, 0.31f, 52f);
                    Aim(camera, look.Scene, forestFloor);
                    RegionTerrainViewMenu.Capture(camera, destination, "forest-floor-full.png");
                }
            }
        }

        private static void Aim(Camera camera, RegionLookScene scene, RegionLookViewpoint view)
        {
            RegionTerrainView.PlaceLook(camera.transform, scene, view);
            camera.fieldOfView = view.FovDegrees;
        }

        private static int Number(string flag, int fallback) =>
            int.Parse(RegionMapMenu.Argument(flag, fallback.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
    }
}
