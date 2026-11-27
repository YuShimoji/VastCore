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
                string folder = Path.GetFullPath(args[output + 1]);
                if (args.Contains("-coastalContactStudy"))
                    CaptureContactStudy(folder, args.Contains("-coastalRoadOnly"), args.Contains("-coastalFrames"));
                else
                    Capture(folder, args.Contains("-coastalPreview"), args.Contains("-coastalFrames"));
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = previous;
            }
        }

        private static void CaptureContactStudy(string output, bool roadOnly, bool frames)
        {
            Directory.CreateDirectory(output);
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var sample = scene.GetRootGameObjects().Select(root => root.GetComponent<CoastalWaterSample>())
                .First(view => view);
            Camera camera = sample.GetComponent<Camera>();
            camera.aspect = 16f / 9f;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            using var shot = new FrameCapture(camera, 1280, 720);

            // Frame zero and the 20 Hz clock match the videos filmed from 6487a81.
            var road = sample.Sites[1];
            int stormIndex = road.DefaultSea;
            SeaState storm = sample.SeaStates[stormIndex];
            float roadStart = ClipStart(road, storm);
            sample.OpenSite(1);
            CaptureSite(sample, shot, 1, "road", roadStart, output, frames,
                new[] { 5, 16, 23, 29, 36, 44, 52, 64, 79, 98, 113, 130, 149 });

            float strike = roadStart + 24f / 20f;
            float wash = roadStart + 53f / 20f;
            float receded = roadStart + 118f / 20f;
            var trial = UnityEngine.Object.Instantiate(storm);
            float foam = road.Shore.FilmFoam;
            float wet = road.Shore.WetHoldSeconds;
            try
            {
                sample.SeaStates[stormIndex] = trial;
                foreach (bool stronger in new[] { false, true })
                {
                    string label = stronger ? "b" : "a";
                    road.Shore.FilmFoam = foam;
                    sample.OpenSite(1);
                    trial.BreakerCurl = stronger ? 0.97f : 0.17f;
                    StudyFrame(shot, sample, stormIndex, strike, output, "road-curl-" + label);
                    trial.BreakerCurl = storm.BreakerCurl;
                    trial.Spray = stronger ? 0.97f : 0.12f;
                    StudyFrame(shot, sample, stormIndex, strike + 0.26f, output, "road-spray-" + label);
                    trial.Spray = storm.Spray;

                    road.Shore.FilmFoam = stronger ? 0.89f : 0.24f;
                    sample.OpenSite(1);
                    StudyFrame(shot, sample, stormIndex, wash, output, "road-film-" + label);
                }
                road.Shore.FilmFoam = foam;
                road.Shore.WetHoldSeconds = 0f;
                sample.OpenSite(1);
                StudyFrame(shot, sample, stormIndex, receded, output, "road-trace-off");
                road.Shore.WetHoldSeconds = wet;
                sample.OpenSite(1);
                StudyFrame(shot, sample, stormIndex, receded, output, "road-trace-on");
                StudyFrame(shot, sample, stormIndex, wash, output, "road-low-barrier");
                sample.OpenSite(1, 6.9f);
                StudyFrame(shot, sample, stormIndex, wash, output, "road-high-barrier");
                StudyFrame(shot, sample, stormIndex, receded, output, "road-high-barrier-receded");
            }
            finally
            {
                road.Shore.FilmFoam = foam;
                road.Shore.WetHoldSeconds = wet;
                sample.SeaStates[stormIndex] = storm;
                UnityEngine.Object.DestroyImmediate(trial);
            }

            if (!roadOnly)
            {
                var beach = sample.Sites[0];
                sample.OpenSite(0);
                float beachStart = ClipStart(beach, sample.SeaStates[beach.DefaultSea]);
                CaptureSite(sample, shot, 0, "beach", beachStart, output, frames,
                    new[] { 5, 15, 27, 34, 45, 58, 70, 86, 102, 114, 126, 140 });
                float deflection = beach.Shore.SwashDeflection;
                try
                {
                    StudyFrame(shot, sample, beach.DefaultSea, beachStart + 3.50f, output, "beach-rocks-on");
                    StudyFrame(shot, sample, beach.DefaultSea, beachStart + 4.88f, output, "beach-backwash-rocks-on");
                    beach.Shore.SwashDeflection = 0f;
                    sample.OpenSite(0);
                    StudyFrame(shot, sample, beach.DefaultSea, beachStart + 3.50f, output, "beach-rocks-off");
                    StudyFrame(shot, sample, beach.DefaultSea, beachStart + 4.88f, output, "beach-backwash-rocks-off");
                }
                finally
                {
                    beach.Shore.SwashDeflection = deflection;
                    sample.OpenSite(0);
                }
                StudyFrame(shot, sample, 0, ClipStart(beach, sample.SeaStates[0]) + 2.65f, output, "beach-calm");
                sample.OpenSite(2);
                var curve = sample.Sites[2];
                float curveTime = ClipStart(curve, sample.SeaStates[curve.DefaultSea]) + 2.64f;
                StudyFrame(shot, sample, curve.DefaultSea, curveTime, output, "curved-storm");
                StudyFrame(shot, sample, 0, curveTime, output, "curved-calm");
            }

            File.WriteAllLines(Path.Combine(output, "contact-comparisons.txt"), new[]
            {
                "1280x720, 20 fps. Simulation starts 1.8 s before the same contact point as 6487a81.",
                $"Road curl 0.17 / 0.97 at frame 24; spray 0.12 / 0.97 at t={Format(strike + 0.26f)} s.",
                $"Road film foam 0.24 / 0.89 at frame 53; residual wetness 0 / {Format(wet)} s at frame 118.",
                $"Barrier normal {Format(road.Shore.CrestHeight)} m / high 6.90 m, at the same frame 53.",
                $"Default settings: curl {Format(storm.BreakerCurl)}, spray {Format(storm.Spray)}, road film foam {Format(foam)}."
            });
        }

        private static float ClipStart(CoastalWaterSample.Site site, SeaState sea) =>
            site.Shore.CrestArrival(sea, site.FocusZ) + sea.Period - 1.8f;

        private static void StudyFrame(FrameCapture capture, CoastalWaterSample sample, int sea, float seconds,
            string output, string name)
        {
            sample.SetFrame(sea, seconds, false);
            capture.Save(Path.Combine(output, name + ".png"));
        }

        private static void CaptureSite(CoastalWaterSample sample, FrameCapture capture, int site, string name,
            float start, string output, bool frames, int[] stillFrames)
        {
            int profile = sample.Sites[site].DefaultSea;
            foreach (int i in stillFrames)
                StudyFrame(capture, sample, profile, start + i / 20f, output, $"{name}-{i:0000}");
            if (!frames) return;
            string directory = Path.Combine(output, name + "-frames");
            Directory.CreateDirectory(directory);
            int count = Mathf.CeilToInt(sample.SeaStates[profile].Period * 1.13f * 20f);
            for (int i = 0; i < count; i++)
                StudyFrame(capture, sample, profile, start + i / 20f, directory, i.ToString("0000", CultureInfo.InvariantCulture));
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
                    CaptureSite(sample, video, site, site == 0 ? "beach" : "road", ClipStart(layout, sea),
                        output, true, Array.Empty<int>());
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
