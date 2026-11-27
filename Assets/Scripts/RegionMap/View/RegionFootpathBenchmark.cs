#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace Vastcore.RegionMap
{
    internal sealed class RegionFootpathBenchmark : MonoBehaviour
    {
        [Serializable] private sealed class Readings
        {
            public string gpu, api, pipeline, quality, unity;
            public int width = 1280, height = 800, points = 40, repeats = 3, fps = 12, videoFrames = 289;
            public float previousWalkMetres, pbrWalkMetres;
            public float routeOffset, rockClearance;
            public int previousLod0, previousLod1, pbrLod0, pbrLod1;
            public List<Pass> passes = new List<Pass>();
            public List<Probe> probes = new List<Probe>();
        }

        [Serializable] private sealed class Pass
        {
            public string variant;
            public int repeat;
            public double[] ms;
            public long[] draws, triangles;
        }

        [Serializable] private sealed class Probe
        {
            public string variant, image;
            public float progress, metres;
            public int difference, toHigh, toLow;
        }

        private string _output;
        private RenderTexture _target, _fence;
        private Texture2D _image;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartRequestedRun()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-measureFootpath");
            if (index < 0 || index + 1 >= args.Length) return;
            var host = new GameObject("Footpath benchmark");
            DontDestroyOnLoad(host);
            host.AddComponent<RegionFootpathBenchmark>()._output = args[index + 1];
        }

        private IEnumerator Start()
        {
            using var run = Measure();
            int exitCode = 0;
            while (true)
            {
                object step;
                try
                {
                    if (!run.MoveNext()) break;
                    step = run.Current;
                }
                catch (Exception error)
                {
                    Debug.LogException(error);
                    exitCode = 1;
                    break;
                }
                yield return step;
            }
            Application.Quit(exitCode);
        }

        private IEnumerator<object> Measure()
        {
            var map = FindFirstObjectByType<RegionExplorer>();
            if (!map) throw new InvalidOperationException("The Player did not start in regional exploration");
            while (!map.IsReady) yield return null;
            if (!map.OpenFootpath()) throw new InvalidOperationException("The regional explorer could not open the footpath");
            RegionFootpath walk;
            float deadline = Time.realtimeSinceStartup + 180f;
            while (!(walk = FindFirstObjectByType<RegionFootpath>()))
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException("The footpath did not open");
                yield return null;
            }

            Directory.CreateDirectory(_output);
            var report = new Readings
            {
                gpu = SystemInfo.graphicsDeviceName, api = SystemInfo.graphicsDeviceType.ToString(),
                pipeline = GraphicsSettings.currentRenderPipeline?.name, unity = Application.unityVersion,
                quality = QualitySettings.names[QualitySettings.GetQualityLevel()],
                routeOffset = walk.RouteOffset, rockClearance = walk.RockClearance
            };
            if (!SystemInfo.supportsAsyncGPUReadback || report.pipeline != "PC_RPAsset")
                throw new InvalidOperationException("The product PC URP asset and an active GPU are required");
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            walk.View.enabled = false;
            SetSize(report.width, report.height);
            _fence = new RenderTexture(1, 1, 0, RenderTextureFormat.ARGB32);
            _fence.Create();
            walk.Place(.50f, true);
            Color32[] sample = Capture(walk.View, "render-check");
            if (sample.Count(p => p.r >= 248 && p.g <= 8 && p.b >= 248) > sample.Length / 10000)
                throw new InvalidDataException("The regional footpath contains a visible error shader");

            using var draws = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 1);
            using var triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count", 1);
            for (int repeat = 0; repeat < report.repeats; repeat++)
            for (int order = 0; order < 2; order++)
            {
                bool pbr = (repeat + order) % 2 != 0;
                walk.UsePbr(pbr);
                var pass = new Pass { variant = Variant(pbr), repeat = repeat, ms = new double[report.points],
                    draws = new long[report.points], triangles = new long[report.points] };
                for (int point = -12; point < report.points; point++)
                {
                    float t = Mathf.Max(0, point) / (report.points - 1f);
                    walk.Place(t, true);
                    long start = Stopwatch.GetTimestamp();
                    Render(walk.View);
                    Graphics.Blit(_target, _fence);
                    var readback = AsyncGPUReadback.Request(_fence);
                    readback.WaitForCompletion();
                    if (readback.hasError) throw new InvalidOperationException("GPU readback failed");
                    double time = (Stopwatch.GetTimestamp() - start) * 1000d / Stopwatch.Frequency;
                    yield return null;
                    if (point < 0) continue;
                    pass.ms[point] = time;
                    pass.draws[point] = draws.Valid ? draws.LastValue : 0;
                    pass.triangles[point] = triangles.Valid ? triangles.LastValue : 0;
                }
                report.passes.Add(pass);
            }

            foreach (bool pbr in new[] { false, true })
            {
                walk.UsePbr(pbr);
                string variant = Variant(pbr);
                var lods = walk.Shelf.GetLODs();
                if (pbr) { report.pbrLod0 = MeshTriangles(lods[0]); report.pbrLod1 = MeshTriangles(lods[1]); }
                else { report.previousLod0 = MeshTriangles(lods[0]); report.previousLod1 = MeshTriangles(lods[1]); }
                foreach (float t in new[] { 0f, .35f, .47f, .50f, .53f, .56f, .59f, .62f, .65f, .68f, .71f, 1f })
                {
                    walk.Place(t, true);
                    string prefix = t == 0f ? variant + "-near" : t == 1f ? variant + "-far" : t == .56f ? variant + "-middle" : null;
                    var automatic = Capture(walk.View, prefix);
                    walk.Shelf.ForceLOD(0);
                    var high = Capture(walk.View, null);
                    walk.Shelf.ForceLOD(1);
                    var low = Capture(walk.View, null);
                    walk.Shelf.ForceLOD(-1);
                    report.probes.Add(new Probe { variant = variant, image = prefix, progress = t,
                        metres = walk.DistanceToShelf, difference = Difference(high, low),
                        toHigh = Difference(automatic, high), toLow = Difference(automatic, low) });
                }
            }

            SetSize(640, 400);
            foreach (bool pbr in new[] { false, true })
            {
                walk.UsePbr(pbr);
                walk.Place(0f, true);
                string frames = Path.Combine(_output, Variant(pbr) + "-frames");
                Directory.CreateDirectory(frames);
                for (int frame = 0; frame < report.videoFrames; frame++)
                {
                    if (frame > 0) walk.Move((frame <= 144 ? 1f : -1f) * 2.5f / report.fps);
                    if (frame == 144)
                    {
                        if (pbr) report.pbrWalkMetres = walk.WalkingMetres;
                        else report.previousWalkMetres = walk.WalkingMetres;
                    }
                    Capture(walk.View, Path.Combine(frames, $"walk-{frame:0000}"));
                    if (frame % 12 == 0) yield return null;
                }
            }

            walk.ReturnToMap();
            yield return null;
            if (!map.isActiveAndEnabled) throw new InvalidOperationException("The exploration screen did not return");
            File.WriteAllText(Path.Combine(_output, "native-results.json"), JsonUtility.ToJson(report, true));
        }

        private static string Variant(bool pbr) => pbr ? "pbr" : "previous";
        private static int MeshTriangles(LOD lod) => lod.renderers.Sum(item => item.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3);

        private void SetSize(int width, int height)
        {
            if (_target) { _target.Release(); Destroy(_target); }
            if (_image) Destroy(_image);
            _target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            if (!_target.Create()) throw new InvalidOperationException("Could not create the footpath target");
            _image = new Texture2D(width, height, TextureFormat.RGB24, false);
        }

        private void Render(Camera camera)
        {
            var request = new RenderPipeline.StandardRequest { destination = _target };
            RenderPipeline.SubmitRenderRequest(camera, request);
        }

        private Color32[] Capture(Camera camera, string file)
        {
            Render(camera);
            RenderTexture last = RenderTexture.active;
            RenderTexture.active = _target;
            _image.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0, false);
            RenderTexture.active = last;
            if (file != null) File.WriteAllBytes(Path.Combine(_output, file + ".png"), _image.EncodeToPNG());
            return _image.GetPixels32();
        }

        private static int Difference(Color32[] a, Color32[] b)
        {
            int count = 0;
            for (int i = 0; i < a.Length; i++)
                if (Mathf.Abs(a[i].r - b[i].r) > 8 || Mathf.Abs(a[i].g - b[i].g) > 8 || Mathf.Abs(a[i].b - b[i].b) > 8) count++;
            return count;
        }
    }
}
#endif
