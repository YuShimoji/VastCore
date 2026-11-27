using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Vastcore.ModernCity.Editor
{
    public static class CityReview
    {
        private const int AlternateSeed = 1031;
        private const string StreetCubemap = CityKitAssets.Root + "/Textures/Storefront/Street_reflection.exr";
        private const string ImportReviewScene = "Assets/ModernCity Import Check.unity";

        public static void BuildSampleFromCommandLine() => BuildSample(rebuildAllExamples: true);

        public static void ExportSampleFromCommandLine() =>
            CityKitMenu.Export(Path.Combine(OutputDirectory(), "ModernCity.unitypackage"));

        public static void VerifyImportedLayoutFromCommandLine()
        {
            string output = OutputDirectory();
            var sample = OpenSavedSample();
            var sampleCheck = CityVerification.Walk(sample, CityBlock.Design(CityBlock.DefaultSeed, CityStreetPlan.CornerShops));
            if (!EditorApplication.ExecuteMenuItem("Vastcore/Modern City/Variants..."))
                throw new InvalidOperationException("The imported kit could not open the Variants window.");
            var variant = CityKitMenu.GenerateStreet(AlternateSeed, CityStreetPlan.SideStreet, CityDetail.Furnished);
            var lobby = new CityPlot("Reassembled 12 m lobby", 82, 10.45f, 3, 3, 2, 0,
                CityFacade.Glass, CityRoom.Reception, CityRoom.Office);
            var shop = new CityPlot("Reassembled 16 m shop", 100, 10.65f, 4, 3, 2, 0,
                CityFacade.Stone, CityRoom.Retail, CityRoom.Storage);
            var lobbyRoot = CityKitMenu.PlaceBuilding(lobby, CityDetail.Furnished, AlternateSeed);
            var shopRoot = CityKitMenu.PlaceBuilding(shop, CityDetail.Furnished, AlternateSeed);
            var parts = new[] { lobbyRoot, shopRoot }.SelectMany(t => t.GetComponentsInChildren<Transform>())
                .Where(t => PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))
                .Select(t => PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject))
                .Distinct().ToArray();
            if (parts.Length < 25 || parts.Any(p => !p.StartsWith(CityKitAssets.Root + "/Prefabs/", StringComparison.Ordinal)))
                throw new InvalidOperationException("The reassembled buildings should be made from imported Prefabs.");
            string layoutCheck = CityVerification.Walk(variant.Scene, variant.Plots.Concat(new[] { lobby, shop }).ToArray());
            if (!EditorSceneManager.SaveScene(variant.Scene, ImportReviewScene))
                throw new IOException("Could not save the reassembled recipient scene.");

            File.WriteAllLines(Path.Combine(output, "import-layout.txt"), new[]
            {
                $"{Application.unityVersion}; {GraphicsSettings.currentRenderPipeline?.name}; {PlayerSettings.colorSpace}.",
                sampleCheck, layoutCheck,
                $"{parts.Length} distinct imported Prefabs across the 12 m lobby and 16 m shop.",
                $"Saved {ImportReviewScene} in the recipient project."
            });
        }

        public static void CaptureImportedLayoutFromCommandLine()
        {
            // Reopen the saved scene in a fresh editor after VerifyImportedLayoutFromCommandLine.
            EditorSceneManager.OpenScene(ImportReviewScene);
            var camera = new GameObject("Imported arrangement camera").AddComponent<Camera>();
            camera.nearClipPlane = 0.07f;
            camera.farClipPlane = 245;
            camera.gameObject.AddComponent<UniversalAdditionalCameraData>().antialiasing =
                AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            try
            {
                Shot(camera, OutputDirectory(), "reopened-layout",
                    new Vector3(75f, 6.6f, -8.0f), new Vector3(98, 3.0f, 14), 68, warmupFrames: 3);
            }
            finally
            {
                Object.DestroyImmediate(camera.gameObject);
            }
        }

        private static void BuildSample(bool rebuildAllExamples)
        {
            string output = OutputDirectory();
            var timer = Stopwatch.StartNew();
            var kit = new CityKitAssets();
            var palette = new CityPalette();
            CityBuildingKit.Bake(kit, palette);
            CityStorefrontKit.Bake(kit, palette);
            CityStreetKit.Bake(kit, palette);
            CityProps.Bake(kit, palette);
            CityCafeFurnishings.Bake(kit, palette);
            CityPalette.BakeSky();
            AssetDatabase.SaveAssets();
            long baking = timer.ElapsedMilliseconds;

            timer.Restart();
            CityBlock sample = Create(kit, CityBlock.DefaultSeed, CityStreetPlan.CornerShops, CityDetail.Furnished);
            long assembly = timer.ElapsedMilliseconds;
            PersistBuildings(sample, rebuildAllExamples);
            BakeStreetReflection();
            if (!EditorSceneManager.SaveScene(sample.Scene, CityKitAssets.SampleScene))
                throw new IOException("Could not save the Modern City sample scene.");
            Capture(sample, output, "sample", CityBlock.DefaultSun);
            Hash128 signature = CityVerification.Signature(sample.Root);
            var savedScene = EditorSceneManager.OpenScene(CityKitAssets.SampleScene);
            string sampleCheck = CityVerification.Walk(savedScene, sample.Plots);
            var alternate = Create(kit, AlternateSeed, CityStreetPlan.SideStreet, CityDetail.Furnished);
            Hash128 alternateSignature = CityVerification.Signature(alternate.Root);
            string alternateCheck = CityVerification.Walk(alternate.Scene, alternate.Plots);
            var differentSeed = Create(kit, AlternateSeed, CityStreetPlan.CornerShops, CityDetail.Furnished);
            if (signature == CityVerification.Signature(differentSeed.Root))
                throw new InvalidOperationException("The seed did not change the geometry or Props on the same street plan.");
            var rebuilt = Create(kit, CityBlock.DefaultSeed, CityStreetPlan.CornerShops, CityDetail.Furnished);
            if (signature != CityVerification.Signature(rebuilt.Root) || signature == alternateSignature)
                throw new InvalidOperationException("City variant signatures do not match the seed contract.");

            CityKitMenu.Export(Path.Combine(output, "ModernCity.unitypackage"));
            File.WriteAllLines(Path.Combine(output, "verification.txt"), new[]
            {
                $"Prefab baking: {baking} ms; sample assembly: {assembly} ms (including Props).",
                sampleCheck,
                alternateCheck,
                $"Sample {signature}; alternate {alternateSignature}; rebuild of sample matches. Seed alone changes placement.",
                $"Prefab counts: buildings {Prefabs(CityKitAssets.Buildings)}, streets {Prefabs(CityKitAssets.Street)}, props {Prefabs(CityKitAssets.Props)}.",
                $"Sample renderers: {rebuilt.Root.GetComponentsInChildren<MeshRenderer>().Length}."
            });
        }

        private static void BakeStreetReflection()
        {
            var probe = CityKitAssets.Group("Street reflection", null, new Vector3(-7f, 2.6f, 0.95f))
                .AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Custom;
            probe.renderDynamicObjects = true;
            probe.resolution = 128;
            probe.hdr = true;
            probe.size = new Vector3(88, 13, 25);
            probe.boxProjection = true;
            probe.intensity = 0.65f;
            probe.nearClipPlane = 0.28f;
            probe.farClipPlane = 130;
            if (!Lightmapping.BakeReflectionProbe(probe, StreetCubemap))
                throw new InvalidOperationException("Could not capture the street reflection.");
            AssetDatabase.ImportAsset(StreetCubemap, ImportAssetOptions.ForceSynchronousImport);
            probe.customBakedTexture = AssetDatabase.LoadAssetAtPath<Cubemap>(StreetCubemap);
            if (!probe.customBakedTexture)
                throw new InvalidOperationException("Unity did not import the street reflection as a cubemap.");
        }

        public static void CaptureComparisonFromCommandLine()
        {
            string output = OutputDirectory();
            var kit = new CityKitAssets();
            var timings = new List<string>
            {
                "variant\tdetail\tscene_ms\trenderers\tcolliders\tstreet_render_with_readback_ms"
            };
            foreach (var (site, seed, street) in new[]
            {
                ("corner", CityBlock.DefaultSeed, CityStreetPlan.CornerShops),
                ("side", AlternateSeed, CityStreetPlan.SideStreet)
            })
            {
                foreach (var detail in new[] { CityDetail.Essential, CityDetail.Furnished })
                {
                    var timer = Stopwatch.StartNew();
                    CityBlock block = Create(kit, seed, street, detail);
                    long assembly = timer.ElapsedMilliseconds;
                    double streetTime = 0;
                    foreach (float sun in new[] { 60f, 38f, 20f, 10f })
                    {
                        string prefix = site + "-" + detail.ToString().ToLowerInvariant() + "-" +
                            sun.ToString("F0", CultureInfo.InvariantCulture);
                        double timing = Capture(block, output, prefix, sun, sun == 38, sun == 38);
                        if (sun == 38)
                            streetTime = timing;
                    }
                    int renderers = block.Root.GetComponentsInChildren<MeshRenderer>().Length;
                    int colliders = block.Root.GetComponentsInChildren<Collider>().Length;
                    timings.Add(FormattableString.Invariant($"{site}\t{detail}\t{assembly}\t{renderers}\t{colliders}\t{streetTime:F2}"));
                }
            }
            File.WriteAllLines(Path.Combine(output, "comparison.tsv"), timings);
        }

        public static void BuildCaptureAndPlayFromCommandLine()
        {
            BuildSampleFromCommandLine();
            CaptureComparisonFromCommandLine();
            CityPlayVerification.RunFromCommandLine();
        }

        public static void CaptureStorefrontFromCommandLine()
        {
            string output = OutputDirectory();
            var kit = new CityKitAssets();
            var palette = new CityPalette();
            CityStorefrontKit.Bake(kit, palette);
            CityCafeFurnishings.Bake(kit, palette);
            AssetDatabase.SaveAssets();
            var scene = Create(kit, CityBlock.DefaultSeed, CityStreetPlan.CornerShops, CityDetail.Furnished);
            Capture(scene, output, "corner-furnished-38", CityBlock.DefaultSun);
        }

        public static void BuildCafeReviewAndPlayFromCommandLine()
        {
            BuildSample(rebuildAllExamples: false);
            string output = OutputDirectory();
            var alternate = Create(new CityKitAssets(), AlternateSeed, CityStreetPlan.SideStreet, CityDetail.Furnished);
            var shop = alternate.Plots.Single(plot => plot.RecessedStorefront);
            var usedParts = VerifyStorefrontPrefabs(alternate.Buildings[shop]);
            var saved = EditorSceneManager.OpenScene(CityKitAssets.SampleScene);
            var cafe = saved.GetRootGameObjects().Select(root => root.transform.Find("Corner cafe")).First(x => x != null);
            var cornerParts = VerifyStorefrontPrefabs(cafe);
            if (!usedParts.SequenceEqual(cornerParts))
                throw new InvalidOperationException("The narrow store should use the same doorway, awning and furnishings.");

            alternate = Create(new CityKitAssets(), AlternateSeed, CityStreetPlan.SideStreet, CityDetail.Furnished);
            CaptureNarrowShop(alternate, shop, output);
            File.WriteAllLines(Path.Combine(output, "prefab-reuse.txt"),
                new[] { $"Seed {AlternateSeed}, {shop.Name}: {shop.Width:F0} m. Corner cafe: 16 m." }.Concat(usedParts));
            CityPlayVerification.RunFromCommandLine();
        }

        private static string[] VerifyStorefrontPrefabs(Transform building)
        {
            var expected = new[]
            {
                CityStorefrontKit.Window, CityStorefrontKit.Entry, CityStorefrontKit.Canopy, CityStorefrontKit.StairPortal,
                CityCafeFurnishings.Order, CityCafeFurnishings.Shelves, CityCafeFurnishings.WindowDisplay
            };
            return expected.Select(name =>
            {
                var placed = building.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == name);
                string path = placed != null ? PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(placed.gameObject) : "";
                if (string.IsNullOrEmpty(path))
                    throw new InvalidOperationException($"{building.name} is missing the prefab for {name}.");
                return path;
            }).ToArray();
        }

        private static CityBlock Create(CityKitAssets kit, int seed, CityStreetPlan street, CityDetail detail)
        {
            CityBlock block = CityBlock.Assemble(kit, seed, street);
            CityFurnishing.Dress(kit, block, detail, seed);
            return block;
        }

        private static int Prefabs(string category) =>
            Directory.GetFiles(CityKitAssets.Root + "/Prefabs/" + category, "*.prefab").Length;

        private static void PersistBuildings(CityBlock block, bool rebuildAllExamples)
        {
            string folder = CityKitAssets.Root + "/Prefabs/Assembled";
            CityKitAssets.Folder(folder);
            foreach (var plot in block.Plots.Take(3).Where(plot => rebuildAllExamples || plot.RecessedStorefront))
            {
                var building = block.Buildings[plot];
                if (PrefabUtility.SaveAsPrefabAsset(building.gameObject, folder + "/" + plot.Name + ".prefab") == null)
                    throw new IOException("Could not save assembled " + plot.Name);
            }
        }

        public static void CaptureMassingFromCommandLine()
        {
            string output = OutputDirectory();
            var kit = new CityKitAssets();
            var palette = new CityPalette();
            CityBuildingKit.Bake(kit, palette);
            CityStorefrontKit.Bake(kit, palette);
            CityStreetKit.Bake(kit, palette);
            AssetDatabase.SaveAssets();
            CityBlock block = CityBlock.Assemble(kit, CityBlock.DefaultSeed, CityStreetPlan.CornerShops);
            var clay = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            try
            {
                clay.color = new Color(0.57f, 0.56f, 0.54f);
                foreach (var renderer in block.Root.GetComponentsInChildren<MeshRenderer>())
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                        if (materials[i].name != "Glazing")
                            materials[i] = clay;
                    renderer.sharedMaterials = materials;
                }
                Capture(block, output, "massing", CityBlock.DefaultSun);
            }
            finally
            {
                Object.DestroyImmediate(clay);
            }
        }

        internal static string OutputDirectory()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-cityOutput");
            if (index < 0 || index == args.Length - 1)
                throw new ArgumentException("Provide -cityOutput <directory>.");
            return Directory.CreateDirectory(Path.GetFullPath(args[index + 1])).FullName;
        }

        internal static double Capture(CityBlock block, string output, string prefix, float sun, bool interiors = true,
            bool measure = false)
        {
            var plot = block.Plots[0];
            var office = block.Plots[2];
            float junction = office.Origin.x - 8;
            block.Sun.transform.rotation = Quaternion.Euler(sun, -36, 0);
            var position = new GameObject("Review camera");
            var camera = position.AddComponent<Camera>();
            camera.nearClipPlane = 0.07f;
            camera.farClipPlane = 245;
            position.AddComponent<UniversalAdditionalCameraData>().antialiasing =
                AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            try
            {
                Shot(camera, output, prefix + "-district",
                    new Vector3(junction + 50, 35, -55), new Vector3(junction - 8, 4, 10), 47);
                Shot(camera, output, prefix + "-street",
                    new Vector3(junction - 8.95f, 1.78f, 7.58f), new Vector3(junction - 30, 3.6f, 10.6f), 68);
                double timing = measure ? Measure(camera) : 0;
                Shot(camera, output, prefix + "-entry",
                    plot.World(plot.EntranceX - 2.2f, 1.64f, -4), plot.World(plot.EntranceX, 1.72f, 2.5f), 70);
                if (!interiors)
                    return timing;
                Shot(camera, output, prefix + "-cafe",
                    plot.World(8.2f, 1.68f, 2.77f), plot.World(0.93f, 1.31f, 2.14f), 69);
                Shot(camera, output, prefix + "-inside",
                    plot.World(9.2f, 1.64f, 0.99f), plot.World(plot.StairX - 0.7f, 1.52f, plot.StairZ + 1.25f), 73);
                Shot(camera, output, prefix + "-stair",
                    plot.World(plot.StairX - 0.7f, 1.64f, plot.StairZ - 2.2f),
                    plot.World(plot.StairX - 0.52f, 2.24f, plot.StairZ + 2.68f), 69);
                Shot(camera, output, prefix + "-office",
                    office.World(10.8f, CityBuildingKit.Storey + 1.64f, 5.83f),
                    office.World(3.6f, CityBuildingKit.Storey + 1.26f, 1.37f), 68);
                return timing;
            }
            finally
            {
                Object.DestroyImmediate(position);
            }
        }

        public static void CaptureDistrictFromCommandLine()
        {
            string output = OutputDirectory();
            var plots = CityBlock.Design(CityBlock.DefaultSeed, CityStreetPlan.CornerShops);
            var scene = OpenSavedSample();
            var camera = Camera.main;
            if (!camera)
                throw new InvalidOperationException("The saved sample has no walkthrough camera.");

            SaveFrame(camera, Path.Combine(output, "launch.png"));
            var rig = new GameObject("Fixed district review");
            var review = rig.AddComponent<Camera>();
            review.nearClipPlane = 0.07f;
            review.farClipPlane = 245;
            rig.AddComponent<UniversalAdditionalCameraData>().antialiasing =
                AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var timings = new List<string> { "view\trender_with_readback_ms\tforced_near_trees_ms" };
            var trees = Object.FindObjectsByType<LODGroup>(FindObjectsSortMode.None);
            try
            {
                var cafe = plots[0];
                var office = plots[2];
                var retail = plots[3];
                var shots = new (string Name, Vector3 From, Vector3 At, float FieldOfView)[]
                {
                    ("far", new Vector3(50, 35, -55), new Vector3(-8, 4, 10), 47),
                    ("junction", new Vector3(2, 1.79f, 7.55f), new Vector3(-32, 3.0f, 10.9f), 68),
                    ("east-walk", new Vector3(30, 1.79f, 7.6f), new Vector3(74, 2.65f, 9.5f), 66),
                    ("retail-entry", retail.World(3.25f, 1.65f, -4.8f), retail.World(retail.EntranceX, 1.7f, 1.65f), 66),
                    ("retail-inside", retail.World(retail.EntranceX - 0.25f, 1.65f, 1.05f),
                        retail.World(retail.EntranceX - 2.95f, 1.5f, 5.75f), 70),
                    ("office-entry", office.World(8.1f, 1.65f, -5.1f), office.World(10, 1.75f, 2.65f), 66),
                    ("office-lobby", office.World(10.05f, 1.65f, 1.12f), office.World(8.3f, 1.42f, 6.2f), 69),
                    ("office-upper", office.World(10.8f, CityBuildingKit.Storey + 1.64f, 5.83f),
                        office.World(3.6f, CityBuildingKit.Storey + 1.26f, 1.37f), 68),
                    ("service", office.World(5.55f, 1.65f, office.Length + 5.3f),
                        office.World(2.1f, 1.57f, office.Length - 0.5f), 69),
                    ("cafe-window", cafe.World(8.2f, 1.68f, 2.77f), cafe.World(0.93f, 1.31f, 2.14f), 69)
                };
                foreach (var shot in shots)
                {
                    Shot(review, output, shot.Name, shot.From, shot.At, shot.FieldOfView);
                    if (shot.Name == "far" || shot.Name == "junction" || shot.Name == "office-lobby")
                    {
                        double active = Measure(review);
                        foreach (var tree in trees)
                            tree.ForceLOD(0);
                        double allNear = trees.Length == 0 ? active : Measure(review);
                        foreach (var tree in trees)
                            tree.ForceLOD(-1);
                        timings.Add(FormattableString.Invariant($"{shot.Name}\t{active:F2}\t{allNear:F2}"));
                    }
                }

                File.WriteAllLines(Path.Combine(output, "render-timings.tsv"), timings);
                File.WriteAllLines(Path.Combine(output, "capture.txt"), new[]
                {
                    $"{scene.path} via Vastcore/Modern City/Open Sample; seed {CityBlock.DefaultSeed}.",
                    $"{Application.unityVersion}; {SystemInfo.graphicsDeviceName}; {SystemInfo.graphicsDeviceType}; " +
                    $"quality {QualitySettings.names[QualitySettings.GetQualityLevel()]}; " +
                    $"{GraphicsSettings.currentRenderPipeline?.name}; {QualitySettings.activeColorSpace}.",
                    $"Directional light {RenderSettings.sun.transform.eulerAngles:F2}; 1600 × 900, fixed cameras, " +
                    "unmodified scene exposure; render timing uses nine samples and a 1 px readback."
                });
            }
            finally
            {
                Object.DestroyImmediate(rig);
            }
        }

        internal static Scene OpenSavedSample()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            if (!EditorApplication.ExecuteMenuItem("Vastcore/Modern City/Open Sample"))
                throw new InvalidOperationException("Could not open the Modern City sample through its menu.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != CityKitAssets.SampleScene)
                throw new InvalidOperationException($"Unexpected sample scene: {scene.path}");
            return scene;
        }

        internal static void Shot(Camera camera, string output, string name, Vector3 from, Vector3 at, float fov,
            int warmupFrames = 0)
        {
            camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(at - from, Vector3.up));
            camera.fieldOfView = fov;
            SaveFrame(camera, Path.Combine(output, name + ".png"), warmupFrames);
        }

        private static void CaptureNarrowShop(CityBlock block, CityPlot shop, string output)
        {
            block.Sun.transform.rotation = Quaternion.Euler(CityBlock.DefaultSun, -36, 0);
            var rig = new GameObject("Narrow shop review camera");
            var camera = rig.AddComponent<Camera>();
            rig.AddComponent<UniversalAdditionalCameraData>().antialiasing =
                AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            try
            {
                Shot(camera, output, "seed-1031-shopfront",
                    shop.World(shop.EntranceX - 2.8f, 1.64f, -3.89f),
                    shop.World(shop.EntranceX + 0.62f, 1.76f, 0.97f), 65);
                Shot(camera, output, "seed-1031-shop",
                    shop.World(shop.EntranceX, 1.64f, 1.45f),
                    shop.World(shop.EntranceX + 0.10f, 1.49f, 5.95f), 73);
            }
            finally
            {
                Object.DestroyImmediate(rig);
            }
        }

        internal static void SaveFrame(Camera camera, string path, int warmupFrames = 0)
        {
            const int width = 1600;
            const int height = 900;
            var render = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var png = new Texture2D(width, height, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            var oldTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = render;
                for (int frame = 0; frame < warmupFrames; frame++)
                    camera.Render();
                camera.Render();
                RenderTexture.active = render;
                png.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                File.WriteAllBytes(path, png.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(render);
                Object.DestroyImmediate(png);
            }
        }

        private static double Measure(Camera camera)
        {
            var target = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32);
            var pixel = new Texture2D(1, 1, TextureFormat.RGB24, false);
            var oldTarget = camera.targetTexture;
            var previous = RenderTexture.active;
            var times = new double[9];
            try
            {
                camera.targetTexture = target;
                for (int frame = -4; frame < times.Length; frame++)
                {
                    var stopwatch = Stopwatch.StartNew();
                    camera.Render();
                    RenderTexture.active = target;
                    pixel.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
                    if (frame >= 0)
                        times[frame] = stopwatch.Elapsed.TotalMilliseconds;
                }
                Array.Sort(times);
                return times[times.Length / 2];
            }
            finally
            {
                camera.targetTexture = oldTarget;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(pixel);
            }
        }
    }
}
