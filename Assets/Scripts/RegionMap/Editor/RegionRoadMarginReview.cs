using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;
using NVector2 = System.Numerics.Vector2;

namespace Vastcore.RegionMap.Editor
{
    public sealed partial class RegionRoadEnvironment
    {
        private static void CaptureMarginShape(string folder, int seed)
        {
            Directory.CreateDirectory(folder);
            foreach (bool detailed in new[] { false, true })
            using (Stage stage = Build(seed, RegionRoadPreset.Country, true, detailed))
            {
                int checks = VerifyRoadSurface(stage);
                Place(stage, 1);
                RegionTerrainViewMenu.Capture(stage.Camera, null, null);
                string prefix = detailed ? "after" : "before";
                RegionTerrainViewMenu.Capture(stage.Camera, folder, prefix + "-mid.png");
                File.WriteAllText(Path.Combine(folder, prefix + "-section.csv"), SampleMidSection(stage));
                Debug.Log(FormattableString.Invariant($"[RegionMap] margin shape {prefix} surface checks={checks}"));
            }
        }

        private static void CaptureMargins(string output, int seed, RegionRoadPreset preset, bool refinement = false)
        {
            string folder = Path.Combine(output, $"{preset.ToString().ToLowerInvariant()}-{seed}");
            Directory.CreateDirectory(folder);
            var setup = new double[][] { new double[3], new double[3] };
            // Warm common resources once, then alternate build order.
            for (int state = 0; state < 2; state++)
                using (Stage warmed = Build(seed, preset, false, refinement || state == 1, refinement && state == 1)) { }
            for (int trial = 0; trial < 2; trial++)
            for (int i = 0; i < 2; i++)
            {
                int state = i ^ trial;
                using (Stage sample = BuildTimed(seed, preset, refinement || state == 1, refinement && state == 1,
                    out setup[state][trial])) { }
            }
            using (Stage before = BuildTimed(seed, preset, refinement, false, out setup[0][2]))
            {
                before.Root.SetActive(false);
                using (Stage after = BuildTimed(seed, preset, true, refinement, out setup[1][2]))
                {
                    after.Root.SetActive(false);
                    Stage[] stages = { before, after };
                    for (int i = 0; i < stages.Length; i++)
                    {
                        Stage stage = stages[i];
                        Activate(stages, i);
                        string prefix = i == 0 ? "before" : "after";
                        int points = VerifyRoadSurface(stage);
                        string site = MeasureSite(stage);
                        File.WriteAllText(Path.Combine(folder, prefix + "-contact.txt"), $"Road surface checks: {points}\n" + site);
                        File.WriteAllText(Path.Combine(folder, prefix + "-section.csv"), SampleMidSection(stage));
                        Place(stage, 0);
                        RegionTerrainViewMenu.Capture(stage.Camera, null, null);
                        CaptureViews(stage, folder, prefix, Views.Length);
                        foreach (float sun in new[] { 60f, 20f, 10f })
                        {
                            stage.Sun.transform.rotation = RegionTerrainView.SunRotation(sun);
                            CaptureViews(stage, folder, prefix + "-sun-" + sun.ToString("00", CultureInfo.InvariantCulture), 2);
                        }
                        stage.Sun.transform.rotation = RegionTerrainView.SunRotation(38f);
                    }
                    for (int view = 0; view < Views.Length; view++) VerifyMatchedView(before, after, view);
                    Array.Sort(setup[0]);
                    Array.Sort(setup[1]);
                    string report = FormattableString.Invariant($"setup_ms_before={setup[0][1]:F1} setup_ms_after={setup[1][1]:F1} delta_ms={setup[1][1] - setup[0][1]:F1}\n") +
                        MeasurePair(stages);
                    File.WriteAllText(Path.Combine(folder, "comparison.txt"), report);
                    Debug.Log($"[RegionMap] {preset} seed {seed} margins\n{report}");
                }
            }
        }

        private static Stage BuildTimed(int seed, RegionRoadPreset preset, bool detailed, bool refined, out double elapsed)
        {
            Stopwatch watch = Stopwatch.StartNew();
            Stage stage = Build(seed, preset, false, detailed, refined);
            elapsed = watch.Elapsed.TotalMilliseconds;
            return stage;
        }

        private static void Activate(Stage[] stages, int active)
        {
            stages[1 - active].Root.SetActive(false);
            Stage stage = stages[active];
            stage.Root.SetActive(true);
            RenderSettings.skybox = stage.Sky;
            RenderSettings.sun = stage.Sun;
        }

        private static void VerifyMatchedView(Stage before, Stage after, int view)
        {
            Place(before, view);
            Place(after, view);
            if (before.Camera.transform.position != after.Camera.transform.position ||
                before.Camera.transform.rotation != after.Camera.transform.rotation ||
                before.Camera.fieldOfView != after.Camera.fieldOfView ||
                before.Sun.transform.rotation != after.Sun.transform.rotation)
                throw new InvalidOperationException($"Camera or light changed in {Views[view]}.");
        }

        private static string MeasurePair(Stage[] stages)
        {
            var report = new StringBuilder("view\tbefore_ms\tafter_ms\tdelta_ms (CPU + GPU sync, 1600x900 MSAA4)\n");
            var target = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var pixel = new Texture2D(1, 1, TextureFormat.RGB24, false);
            var samples = new[] { new double[24], new double[24] };
            RenderTexture previous = RenderTexture.active;
            try
            {
                foreach (Stage stage in stages) stage.Camera.targetTexture = target;
                for (int view = 0; view < Views.Length; view++)
                {
                    foreach (Stage stage in stages) Place(stage, view);
                    for (int run = 0; run < 6; run++)
                    for (int i = 0; i < 2; i++)
                    {
                        int state = i ^ (run & 1);
                        Activate(stages, state);
                        Camera camera = stages[state].Camera;
                        TimedDraw(camera, target, pixel);
                        TimedDraw(camera, target, pixel);
                        for (int n = 0; n < 4; n++) samples[state][run * 4 + n] = TimedDraw(camera, target, pixel);
                    }
                    Array.Sort(samples[0]);
                    Array.Sort(samples[1]);
                    double before = (samples[0][11] + samples[0][12]) * 0.5;
                    double after = (samples[1][11] + samples[1][12]) * 0.5;
                    report.AppendLine(FormattableString.Invariant($"{Views[view]}\t{before:F2}\t{after:F2}\t{after - before:F2}"));
                }
            }
            finally
            {
                foreach (Stage stage in stages) stage.Camera.targetTexture = null;
                RenderTexture.active = previous;
                target.Release();
                DestroyImmediate(target);
                DestroyImmediate(pixel);
            }
            return report.ToString();
        }

        private static string MeasureSite(Stage stage)
        {
            Physics.SyncTransforms();
            var terrain = stage.Terrain.GetComponent<TerrainCollider>();
            RegionRoadsideSite site = stage.Plan.Site;
            float minCover = float.PositiveInfinity, maxCover = float.NegativeInfinity;
            float minPad = float.PositiveInfinity, maxPad = float.NegativeInfinity;
            float maxLip = 0f;
            int count = 0;
            foreach (float reach in new[] { -0.16f, 0.15f, 0.5f, 1.25f, 2.5f, 4.25f, 7.3f, 12f, 17f, 22f, 26.6f })
            {
                float width = RegionNoise.Lerp(RegionRoadsideSite.MouthHalfWidth, RegionRoadsideSite.DriveHalfWidth,
                    RegionNoise.Smooth(0f, 8f, reach));
                foreach (float along in new[] { -0.85f * width, -0.4f * width, 0f, 0.4f * width, 0.85f * width })
                    Sample(reach, along, false);
            }
            foreach (float reach in new[] { 27.45f, 32f, 48f, 65f, 68.55f })
            foreach (float along in new[] { -31.55f, -16f, 0f, 16f, 31.55f })
                Sample(reach, along, true);
            foreach (float along in new[] { -3f, 0f, 3f })
            {
                float road = Height(-0.46f, along, out _);
                float entry = Height(0.16f, along, out _);
                maxLip = Mathf.Max(maxLip, Mathf.Abs(road - entry));
            }
            if (maxPad - minPad > 0.01f || maxLip > 0.075f)
                throw new InvalidOperationException("The plot is no longer flat or the road connection has a raised lip.");
            return FormattableString.Invariant($"Site surface checks: {count}\nMesh above terrain: {minCover * 100f:F2} to {maxCover * 100f:F2} cm\nPlot elevation range: {(maxPad - minPad) * 100f:F3} cm\nRoad-to-entrance height difference (0.62 m span): {maxLip * 100f:F2} cm\n");

            void Sample(float reach, float along, bool pad)
            {
                float y = Height(reach, along, out RaycastHit ground);
                float cover = y - ground.point.y;
                minCover = Mathf.Min(minCover, cover);
                maxCover = Mathf.Max(maxCover, cover);
                if (pad) { minPad = Mathf.Min(minPad, y); maxPad = Mathf.Max(maxPad, y); }
                count++;
            }

            float Height(float reach, float along, out RaycastHit ground)
            {
                NVector2 point = site.Entrance + site.Outward * reach + site.Station.Forward * along;
                var ray = new Ray(Position(point, site.EntranceElevation + 8f), Vector3.down);
                if (!Physics.Raycast(ray, out RaycastHit hit, 18f) || !hit.collider.transform.IsChildOf(stage.Roads.transform) ||
                    !terrain.Raycast(ray, out ground, 18f))
                    throw new InvalidOperationException(FormattableString.Invariant($"The terrain breaks the site surface at reach={reach:F2}, along={along:F2}."));
                return hit.point.y;
            }
        }
    }
}
