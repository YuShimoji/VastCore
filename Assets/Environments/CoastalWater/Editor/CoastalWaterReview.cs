using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Vastcore.Water;

namespace Vastcore.CoastalWater.Editor
{
    public static class CoastalWaterReview
    {
        private const string Folder = "Assets/Environments/CoastalWater";
        private const string ScenePath = Folder + "/CoastalWater.unity";

        [MenuItem("Vastcore/Water/Open Coastal Water Sample")]
        public static void OpenSample()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        // The scripted capture uses the authored scene and the same controller as Play Mode.
        public static void CaptureFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            int output = Array.IndexOf(args, "-coastalOutput");
            if (output < 0 || output + 1 >= args.Length) throw new ArgumentException("Set -coastalOutput to a directory.");
            bool previous = ShaderUtil.allowAsyncCompilation;
            try
            {
                ShaderUtil.allowAsyncCompilation = false;
                Capture(Path.GetFullPath(args[output + 1]), args.Contains("-coastalPreview"), args.Contains("-coastalFrames"));
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = previous;
            }
        }

        private static void Capture(string output, bool preview, bool frames)
        {
            Directory.CreateDirectory(output);
            var scene = EditorSceneManager.OpenScene(ScenePath);
            CoastalWaterSample sample = scene.GetRootGameObjects().Select(root => root.GetComponent<CoastalWaterSample>())
                .FirstOrDefault(view => view);
            if (!sample) throw new InvalidOperationException("The Coastal Water sample camera is missing.");
            if (sample.Sites.Length < 3 || sample.SeaStates.Length < 3)
                throw new InvalidOperationException("The comparison scene needs its three sites and sea states.");
            Shader[] shaders = { sample.OceanShader, sample.ShoreShader, sample.SkyShader, sample.GroundShader };
            if (shaders.Any(shader => !shader || !shader.isSupported))
                throw new InvalidOperationException("A sample shader is missing or unsupported.");
            Camera camera = sample.GetComponent<Camera>();
            camera.aspect = 16f / 9f;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;

            var metrics = new StringBuilder();
            metrics.AppendLine(SystemInfo.graphicsDeviceType + " / " + SystemInfo.graphicsDeviceName
                + " / " + QualitySettings.names[QualitySettings.GetQualityLevel()]);
            if (!preview)
                metrics.AppendLine("1600x900, median ms for camera submission plus a blocking pixel readback (CPU + GPU).");
            Light sun = scene.GetRootGameObjects().Select(root => root.GetComponent<Light>()).FirstOrDefault(light => light);
            var stopwatch = Stopwatch.StartNew();
            using (var still = new FrameCapture(camera, preview ? 1280 : 1600, preview ? 720 : 900))
            {
                for (int site = 0; site < sample.Sites.Length; site++)
                {
                    stopwatch.Restart();
                    sample.OpenSite(site);
                    stopwatch.Stop();
                    var layout = sample.Sites[site];
                    var sea = sample.SeaStates[layout.DefaultSea];
                    string name = site == 0 ? "beach" : site == 1 ? "road" : site == 2 ? "curved" : $"site-{site + 1}";
                    metrics.AppendLine($"{name} setup: {Format(stopwatch.Elapsed.TotalMilliseconds)} ms");
                    float eventTime = layout.Shore.CrestArrival(sea, layout.FocusZ) + sea.Period;
                    float impact = eventTime + sea.Period * 0.135f;
                    float wash = eventTime + sea.Period * 0.343f;
                    sample.SetFrame(layout.DefaultSea, impact, true);
                    still.Save(Path.Combine(output, name + "-flat.png"));
                    sample.SetFrame(layout.DefaultSea, impact, false);
                    still.Save(Path.Combine(output, name + "-impact.png"));
                    sample.SetFrame(layout.DefaultSea, wash, false);
                    still.Save(Path.Combine(output, name + "-wash.png"));
                    if (!preview)
                    {
                        sample.SetFrame(layout.DefaultSea, impact, false);
                        sample.PlaceCamera(1);
                        still.Save(Path.Combine(output, name + "-mid.png"));
                        sample.SetFrame(layout.DefaultSea, impact, true);
                        still.Save(Path.Combine(output, name + "-mid-flat.png"));
                        sample.PlaceCamera(2);
                        sample.SetFrame(layout.DefaultSea, impact, false);
                        still.Save(Path.Combine(output, name + "-far.png"));
                        sample.SetFrame(layout.DefaultSea, impact, true);
                        still.Save(Path.Combine(output, name + "-far-flat.png"));
                        sample.PlaceCamera(0);

                        foreach (int stateIndex in Enumerable.Range(0, sample.SeaStates.Length))
                        {
                            var state = sample.SeaStates[stateIndex];
                            float at = layout.Shore.CrestArrival(state, layout.FocusZ) + state.Period * 1.135f;
                            sample.SetFrame(stateIndex, at, false);
                            still.Save(Path.Combine(output, $"{name}-state-{stateIndex + 1}.png"));
                        }
                        if (site < 2 && sun)
                            SunSweep(sample, sun, still, layout.DefaultSea, impact, name, output);
                        CompareTiming(sample, still, layout.DefaultSea, impact, name, metrics);
                    }
                }

                var road = sample.Sites[1];
                var storms = sample.SeaStates[road.DefaultSea];
                float atRoad = road.Shore.CrestArrival(storms, road.FocusZ) + storms.Period;
                float highCrest = 6.9f;
                if (road.Shore.Overtopping(storms, road.FocusZ) <= 0.10f || storms.Amplitude * 1.62f >= highCrest)
                    throw new InvalidOperationException("The road fixture no longer distinguishes a low barrier from a high one.");
                sample.OpenSite(1, highCrest);
                sample.SetFrame(road.DefaultSea, atRoad + storms.Period * 0.343f, false);
                still.Save(Path.Combine(output, "road-high-barrier.png"));

                if (!preview)
                {
                    var curve = sample.Sites[2];
                    float late = curve.Shore.CrestArrival(storms, curve.FocusZ + 15f);
                    float early = curve.Shore.CrestArrival(storms, curve.FocusZ);
                    metrics.AppendLine($"Curved bank, crest at z={curve.FocusZ} / {curve.FocusZ + 15f}: {Format(early)} / {Format(late)} s");
                    if (Mathf.Abs(late - early) < 0.20f)
                        throw new InvalidOperationException("The second site no longer exercises alongshore variation in the arrival phase.");
                }
            }

            if (frames)
            {
                using var video = new FrameCapture(camera, 1280, 720);
                for (int site = 0; site < 2; site++)
                {
                    sample.OpenSite(site);
                    var layout = sample.Sites[site];
                    var sea = sample.SeaStates[layout.DefaultSea];
                    float start = layout.Shore.CrestArrival(sea, layout.FocusZ) + sea.Period - 1.8f;
                    string directory = Path.Combine(output, site == 0 ? "beach-frames" : "road-frames");
                    Directory.CreateDirectory(directory);
                    const int fps = 20;
                    int count = Mathf.CeilToInt(sea.Period * 1.13f * fps);
                    for (int frame = 0; frame < count; frame++)
                    {
                        sample.SetFrame(layout.DefaultSea, start + frame / (float)fps, false);
                        video.Save(Path.Combine(directory, frame.ToString("0000", CultureInfo.InvariantCulture) + ".png"));
                    }
                }
            }

            File.WriteAllText(Path.Combine(output, "measurements.txt"), metrics.ToString());
        }

        private static void SunSweep(CoastalWaterSample sample, Light sun, FrameCapture still, int sea, float time,
            string name, string output)
        {
            Quaternion original = sun.transform.rotation;
            try
            {
                Vector3 angle = original.eulerAngles;
                foreach (int elevation in new[] { 60, 38, 20, 10 })
                {
                    sun.transform.rotation = Quaternion.Euler(elevation, angle.y, angle.z);
                    string stem = Path.Combine(output, $"{name}-sun{elevation}");
                    sample.SetFrame(sea, time, true);
                    still.Save(stem + "-flat.png");
                    sample.SetFrame(sea, time, false);
                    still.Save(stem + ".png");
                }
            }
            finally
            {
                sun.transform.rotation = original;
            }
        }

        private static void CompareTiming(CoastalWaterSample sample, FrameCapture capture, int profile, float t,
            string name, StringBuilder metrics)
        {
            const int count = 7;
            const int repeats = 5;
            var flat = new double[count];
            var waves = new double[count];
            for (int pass = 0; pass < count; pass++)
            {
                for (int order = 0; order < 2; order++)
                {
                    bool flatWater = (pass + order) % 2 == 0;
                    sample.SetFrame(profile, t, flatWater);
                    capture.TimedRender();
                    double sum = 0;
                    for (int i = 0; i < repeats; i++) sum += capture.TimedRender();
                    (flatWater ? flat : waves)[pass] = sum / repeats;
                }
            }
            Array.Sort(flat);
            Array.Sort(waves);
            double before = flat[count / 2];
            double after = waves[count / 2];
            metrics.AppendLine($"{name} flat / waves / delta: {Format(before)} / {Format(after)} / {Format(after - before)} ms");
        }

        private static string Format(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);

        private sealed class FrameCapture : IDisposable
        {
            private readonly Camera camera;
            private readonly RenderTexture target;
            private readonly Texture2D frame;
            private readonly Texture2D pixel;

            internal FrameCapture(Camera camera, int width, int height)
            {
                this.camera = camera;
                target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 1,
                    name = "Coastal Water review target"
                };
                frame = new Texture2D(width, height, TextureFormat.RGB24, false);
                pixel = new Texture2D(1, 1, TextureFormat.RGB24, false);
            }

            internal void Save(string path)
            {
                RenderInto(frame, new Rect(0, 0, target.width, target.height));
                File.WriteAllBytes(path, frame.EncodeToPNG());
            }

            internal double TimedRender()
            {
                var watch = Stopwatch.StartNew();
                RenderInto(pixel, new Rect(target.width / 2, target.height / 2, 1, 1));
                return watch.Elapsed.TotalMilliseconds;
            }

            private void RenderInto(Texture2D destination, Rect area)
            {
                RenderTexture previous = RenderTexture.active;
                RenderTexture previousTarget = camera.targetTexture;
                try
                {
                    camera.targetTexture = target;
                    camera.Render();
                    RenderTexture.active = target;
                    destination.ReadPixels(area, 0, 0, false);
                }
                finally
                {
                    RenderTexture.active = previous;
                    camera.targetTexture = previousTarget;
                }
            }

            public void Dispose()
            {
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(frame);
                UnityEngine.Object.DestroyImmediate(pixel);
            }
        }
    }
}
