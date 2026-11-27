using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Vastcore.ModernCity.Editor
{
    public static class CityReview
    {
        private const int AlternateSeed = 1031;

        public static void BuildSampleFromCommandLine()
        {
            string output = OutputDirectory();
            var timer = Stopwatch.StartNew();
            var kit = new CityKitAssets();
            var palette = new CityPalette();
            CityBuildingKit.Bake(kit, palette);
            CityStreetKit.Bake(kit, palette);
            CityProps.Bake(kit, palette);
            AssetDatabase.SaveAssets();
            long baking = timer.ElapsedMilliseconds;

            timer.Restart();
            CityBlock sample = Create(kit, CityBlock.DefaultSeed, CityStreetPlan.CornerShops, CityDetail.Furnished);
            long assembly = timer.ElapsedMilliseconds;
            PersistBuildings(sample);
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

        private static CityBlock Create(CityKitAssets kit, int seed, CityStreetPlan street, CityDetail detail)
        {
            CityBlock block = CityBlock.Assemble(kit, seed, street);
            CityFurnishing.Dress(kit, block, detail, seed);
            return block;
        }

        private static int Prefabs(string category) =>
            Directory.GetFiles(CityKitAssets.Root + "/Prefabs/" + category, "*.prefab").Length;

        private static void PersistBuildings(CityBlock block)
        {
            string folder = CityKitAssets.Root + "/Prefabs/Assembled";
            CityKitAssets.Folder(folder);
            foreach (var plot in block.Plots.Take(3))
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

        private static void Shot(Camera camera, string output, string name, Vector3 from, Vector3 at, float fov)
        {
            camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(at - from, Vector3.up));
            camera.fieldOfView = fov;
            SaveFrame(camera, Path.Combine(output, name + ".png"));
        }

        internal static void SaveFrame(Camera camera, string path)
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
