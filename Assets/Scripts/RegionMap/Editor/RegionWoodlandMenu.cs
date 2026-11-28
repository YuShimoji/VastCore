using System;
using System.Collections.Generic;
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
        private const string ParkMenuPath = "Vastcore/Region Map/Woodland Samples/Nature Park";
        private const string ParkCheckOutput = "Vastcore.Woodland.ParkCheckOutput";
        private const string ParkCheckMeshes = "Vastcore.Woodland.ParkCheckMeshes";
        private const string ParkCheckDeadline = "Vastcore.Woodland.ParkCheckDeadline";
        private static RegionParkWalk activeParkCheck;
        private static bool capturedParkMiddle;

        [MenuItem("Vastcore/Region Map/Woodland Samples/Forest")]
        public static void OpenForest() => Open(RegionWoodlandPreset.Forest);

        [MenuItem("Vastcore/Region Map/Woodland Samples/Rainforest")]
        public static void OpenRainforest() => Open(RegionWoodlandPreset.Rainforest);

        [MenuItem(ParkMenuPath)]
        public static void OpenPark() => Open(RegionWoodlandPreset.NaturePark);

        // Domain reload can replace procedural scene instances before the first Play frame.
        [InitializeOnLoadMethod]
        private static void ResumeParkCheck()
        {
            if (string.IsNullOrEmpty(SessionState.GetString(ParkCheckOutput, ""))) return;
            EditorApplication.update -= CheckParkInPlay;
            EditorApplication.update += CheckParkInPlay;
        }

        public static void VerifyParkWalkFromCommandLine()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run this Park verification in a batch Editor.");
            try
            {
                string output = Path.GetFullPath(RegionMapMenu.Argument("-regionOut", "artifacts/region-map/park-play"));
                Directory.CreateDirectory(output);
                ShaderUtil.allowAsyncCompilation = false;
                RegionTerrainViewMenu.UseBuildQuality();
                if (!EditorApplication.ExecuteMenuItem(ParkMenuPath) ||
                    !UnityEngine.Object.FindFirstObjectByType<RegionParkWalk>())
                    throw new InvalidOperationException("The Nature Park menu did not open a park with a walking camera.");
                int meshes = AvailableParkTreeMeshes();
                if (meshes == 0) throw new InvalidOperationException("The Nature Park menu did not build its trees.");
                SessionState.SetString(ParkCheckOutput, output);
                SessionState.SetInt(ParkCheckMeshes, meshes);
                SessionState.SetFloat(ParkCheckDeadline, (float)EditorApplication.timeSinceStartup + 90f);
                ResumeParkCheck();
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                FinishParkCheck(exception);
            }
        }

        private static void CheckParkInPlay()
        {
            try
            {
                if (EditorApplication.timeSinceStartup >= SessionState.GetFloat(ParkCheckDeadline, 0f))
                    throw new TimeoutException("The Nature Park walk did not reach its endpoint in Play mode.");
                if (!EditorApplication.isPlaying || Time.frameCount < 2) return;
                string output = SessionState.GetString(ParkCheckOutput, "");
                if (!activeParkCheck)
                {
                    activeParkCheck = UnityEngine.Object.FindFirstObjectByType<RegionParkWalk>();
                    int expectedMeshes = SessionState.GetInt(ParkCheckMeshes, -1);
                    int treeMeshes = AvailableParkTreeMeshes();
                    bool hasTrail = HasParkTrailMaterial();
                    if (!activeParkCheck || activeParkCheck.Length <= 0f || treeMeshes != expectedMeshes || !hasTrail)
                        throw new InvalidOperationException(
                            $"Missing Park content in Play mode. Camera present: {(bool)activeParkCheck}, tree meshes: {treeMeshes}/{expectedMeshes}, trail surface: {hasTrail}.");
                    RegionTerrainViewMenu.Capture(activeParkCheck.GetComponent<Camera>(), output, "play-start.png");
                    activeParkCheck.StartWalking();
                    // Use the ordinary walking update at an accelerated clock for batch verification.
                    Time.timeScale = 8f;
                }
                if (!capturedParkMiddle && activeParkCheck.Progress >= 0.48f)
                {
                    RegionTerrainViewMenu.Capture(activeParkCheck.GetComponent<Camera>(), output, "play-meadow.png");
                    capturedParkMiddle = true;
                }
                if (activeParkCheck.Progress >= 1f)
                {
                    RegionTerrainViewMenu.Capture(activeParkCheck.GetComponent<Camera>(), output, "play-woods.png");
                    Debug.Log(FormattableString.Invariant(
                        $"[Woodland] park Play reached the end seed={Number("-regionSeed", 8)} metres={activeParkCheck.Length:F1}"));
                    FinishParkCheck(null);
                }
            }
            catch (Exception exception)
            {
                FinishParkCheck(exception);
            }
        }

        private static int AvailableParkTreeMeshes()
        {
            GameObject trees = GameObject.Find("Trees");
            if (!trees) return 0;
            int count = 0;
            foreach (MeshFilter filter in trees.GetComponentsInChildren<MeshFilter>())
                if (filter.sharedMesh && filter.TryGetComponent(out MeshRenderer renderer) && renderer.sharedMaterial) count++;
            return count;
        }

        private static bool HasParkTrailMaterial()
        {
            foreach (MeshRenderer renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                Material material = renderer.sharedMaterial;
                if (material && material.IsKeywordEnabled("REGION_WOODLAND_TRAIL") &&
                    material.GetTexture("_WoodlandTrailRows") is Texture rows && rows.width > 20) return true;
            }
            return false;
        }

        private static void FinishParkCheck(Exception failure)
        {
            EditorApplication.update -= CheckParkInPlay;
            SessionState.EraseString(ParkCheckOutput);
            SessionState.EraseInt(ParkCheckMeshes);
            SessionState.EraseFloat(ParkCheckDeadline);
            Time.timeScale = 1f;
            if (failure != null) Debug.LogException(failure);
            EditorApplication.Exit(failure == null ? 0 : 1);
        }

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
                if (preset == RegionWoodlandPreset.NaturePark) PreserveParkPreviewAssets(look.Root);
                if (camera.TryGetComponent<RegionParkWalk>(out _))
                    Debug.Log("Nature Park walk: use Play and focus Game. Space walks along the trail, W/S moves, A/D looks, Home restarts.");
                else
                    Aim(camera, look.Scene, look.Scene.Spec.Mid);
                if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.AlignViewToObject(camera.transform);
                Selection.activeGameObject = look.Root;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void PreserveParkPreviewAssets(GameObject root)
        {
            var materials = new HashSet<Material>();
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
                KeepDuringPlayReload(filter.sharedMesh);
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            foreach (Material material in renderer.sharedMaterials)
                KeepMaterial(material);
            KeepMaterial(RenderSettings.skybox);

            void KeepMaterial(Material material)
            {
                if (!material || EditorUtility.IsPersistent(material) || !materials.Add(material)) return;
                KeepDuringPlayReload(material);
                foreach (string property in material.GetTexturePropertyNames())
                    KeepDuringPlayReload(material.GetTexture(property));
            }

            static void KeepDuringPlayReload(UnityEngine.Object asset)
            {
                // The editor serializes this generated stage when entering Play.
                if (asset && !EditorUtility.IsPersistent(asset))
                    asset.hideFlags &= ~HideFlags.DontSaveInEditor;
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
            if (preset == RegionWoodlandPreset.NaturePark)
            {
                Vector3[] points = RegionParkWalk.BuildRoute(look.Scene, layout);
                if (points.Length >= 2)
                {
                    var walk = camera.gameObject.AddComponent<RegionParkWalk>();
                    walk.Configure(points);
                    camera.fieldOfView = 59f;
                    Debug.Log(FormattableString.Invariant($"[Woodland] park walk seed={region.Spec.Seed} metres={walk.Length:F1} points={points.Length}"));
                }
                else
                    Debug.LogWarning("This Nature Park site has no continuous trail section for the walking camera.");
            }
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
                if (degrees == 38f && camera.TryGetComponent(out RegionParkWalk walk))
                {
                    camera.fieldOfView = 59f;
                    walk.ShowProgress(0f);
                    RegionTerrainViewMenu.Capture(camera, destination, "walk-start.png");
                    walk.ShowProgress(0.48f);
                    RegionTerrainViewMenu.Capture(camera, destination, "walk-meadow.png");
                    walk.ShowProgress(1f);
                    RegionTerrainViewMenu.Capture(camera, destination, "walk-woods.png");
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
