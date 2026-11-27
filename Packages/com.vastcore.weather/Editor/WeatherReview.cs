using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Vastcore.Weather.Editor
{
    public static class WeatherReview
    {
        [MenuItem("Vastcore/Weather/Open Environment Sample")]
        public static void OpenEnvironmentSample()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            WeatherEnvironmentSample sample = CreateScene();
            Selection.activeGameObject = sample.gameObject;
            SceneView view = SceneView.lastActiveSceneView;
            if (view)
                view.AlignViewToObject(sample.transform);
        }

        [MenuItem("Vastcore/Weather/Save Current Preset")]
        public static void SaveCurrentPreset()
        {
            var sample = UnityEngine.Object.FindFirstObjectByType<WeatherEnvironmentSample>();
            if (!sample)
                return;
            string path = EditorUtility.SaveFilePanelInProject("Save Weather Preset", sample.Site.ToString(), "asset",
                "Choose a folder for the editable weather preset.");
            if (string.IsNullOrEmpty(path))
                return;

            WeatherPreset profile = WeatherSamplePresets.Create(sample.Site);
            profile.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = profile;
        }

        [MenuItem("Vastcore/Weather/Save Current Preset", true)]
        private static bool CanSaveCurrentPreset() =>
            !EditorApplication.isPlaying && UnityEngine.Object.FindFirstObjectByType<WeatherEnvironmentSample>();

        private static WeatherEnvironmentSample CreateScene(bool detailedStreet = true)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var viewer = new GameObject("Weather Environment Sample", typeof(Camera), typeof(Skybox));
            viewer.SetActive(false);
            viewer.tag = "MainCamera";
            viewer.GetComponent<Camera>().GetUniversalAdditionalCameraData().requiresDepthTexture = true;
            var sample = viewer.AddComponent<WeatherEnvironmentSample>();
            if (detailedStreet)
                sample.SetStreetAssets(LoadStreetAssets());
            viewer.SetActive(true);
            return sample;
        }

        private static WeatherStreetAssets LoadStreetAssets()
        {
            const string maps = "Assets/Resources/MapGenerator/Textures/";
            const string prefabs = "Assets/Art/ModernCity/Prefabs/";
            Texture2D Map(string set, string kind)
            {
                string name = set + "_1K-JPG";
                return AssetDatabase.LoadAssetAtPath<Texture2D>(maps + name + "/" + name + "_" + kind + ".jpg");
            }
            GameObject Prefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(prefabs + name + ".prefab");
            return new WeatherStreetAssets
            {
                AsphaltColor = Map("Asphalt010", "Color"),
                AsphaltNormal = Map("Asphalt010", "NormalGL"),
                AsphaltRoughness = Map("Asphalt010", "Roughness"),
                PavingColor = Map("PavingStones073", "Color"),
                PavingNormal = Map("PavingStones073", "NormalGL"),
                PavingRoughness = Map("PavingStones073", "Roughness"),
                CornerCafe = Prefab("Assembled/Corner cafe"),
                BrickShops = Prefab("Assembled/Brick shops"),
                CornerOffices = Prefab("Assembled/Corner offices"),
                Drain = Prefab("Props/Storm drain"),
                StreetLamp = Prefab("Props/Street lamp")
            };
        }

        public static void CaptureStreetComparisonFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            string output = Argument(args, "-weatherOut");
            if (string.IsNullOrWhiteSpace(output))
                throw new ArgumentException("Specify -weatherOut with the street comparison directory.");
            bool draft = Array.IndexOf(args, "-weatherDraft") >= 0;
            int width = draft ? 1088 : 1600;
            int height = draft ? 612 : 900;
            RequireShaders();
            Directory.CreateDirectory(output);

            var report = new StringBuilder();
            report.AppendLine(Application.unityVersion + " / " + SystemInfo.graphicsDeviceType + " / " + SystemInfo.graphicsDeviceName);
            report.AppendLine(width + " x " + height + " / median of seven camera renders with synchronous pixel readback, ms");
            var poses = new StringBuilder().AppendLine("Version\tView\tPosition\tRotation\tVertical FOV\tNear\tFar");
            foreach (bool detailed in new[] { false, true })
            {
                string look = detailed ? "after" : "before";
                string folder = Path.Combine(output, look);
                Directory.CreateDirectory(folder);
                WeatherEnvironmentSample sample = CreateScene(detailed);
                if (sample.HasDetailedStreet != detailed)
                    throw new InvalidOperationException("The detailed sample requires the Modern City prefabs and both pavement material sets.");
                sample.ShowControls = false;
                var timer = Stopwatch.StartNew();
                sample.Open(WeatherSampleSite.StreetThunderstorm, 17);
                timer.Stop();
                report.AppendLine(look + " setup=" + Format(timer.Elapsed.TotalMilliseconds) + " ms");
                report.AppendLine("View\tClear\tRain\tPassed\tRain with reflection disabled\tRain with reflection cached");
                bool reflect = true;
                bool forceRefresh = true;
                using var frame = new Capture(sample.SampleCamera, width, height,
                    () => sample.PrepareStreetReflection(reflect, forceRefresh));
                foreach (int view in new[] { 2, 0, 1 })
                {
                    var camera = sample.SampleCamera;
                    sample.PlaceView(view);
                    poses.AppendLine(string.Join("\t", look, view.ToString(CultureInfo.InvariantCulture),
                        camera.transform.position.ToString("F5"), camera.transform.rotation.ToString("F6"),
                        Format(camera.fieldOfView), Format(camera.nearClipPlane), Format(camera.farClipPlane)));
                    report.Append(view);
                    foreach (string phase in new[] { "clear", "rain", "passed" })
                    {
                        sample.SetFrame(phase == "passed" ? 77f : 42f, phase == "clear");
                        frame.Save(Path.Combine(folder, $"view{view}-{phase}.png"));
                        if (detailed && view == 2 && phase != "clear" && !sample.HasPuddleReflection)
                            throw new InvalidOperationException("The low camera did not receive a puddle reflection.");
                        report.Append('\t').Append(draft ? "-" : Format(frame.MedianRenderMilliseconds()));
                    }
                    if (detailed && view == 2)
                    {
                        reflect = false;
                        sample.SetFrame(42f, false);
                        frame.Save(Path.Combine(folder, "view2-rain-sky-only.png"));
                        report.Append('\t').Append(draft ? "-" : Format(frame.MedianRenderMilliseconds()));
                        reflect = true;
                        if (!draft)
                        {
                            forceRefresh = false;
                            report.Append('\t').Append(Format(frame.MedianRenderMilliseconds()));
                            forceRefresh = true;
                        }
                        WeatherRainCell rain = sample.Field.Preset.Rain;
                        float extinction = rain.CurtainExtinctionPerKilometer;
                        rain.CurtainExtinctionPerKilometer = 0f;
                        sample.SetFrame(42f, false);
                        frame.Save(Path.Combine(folder, "view2-rain-no-curtain.png"));
                        rain.CurtainExtinctionPerKilometer = extinction;
                        sample.SetFrame(42f, true);
                        CaptureStreetForm(frame, Path.Combine(folder, "view2-form.png"));
                    }
                    report.AppendLine();
                }
            }
            File.WriteAllText(Path.Combine(output, "camera-poses.tsv"), poses.ToString());
            File.WriteAllText(Path.Combine(output, "capture-timing.tsv"), report.ToString());
        }

        private static void CaptureStreetForm(Capture frame, string path)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var renderers = System.Array.FindAll(Resources.FindObjectsOfTypeAll<MeshRenderer>(),
                renderer => renderer.gameObject.scene == scene && renderer.gameObject.activeInHierarchy);
            var originals = new Material[renderers.Length][];
            var clay = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            clay.SetColor("_BaseColor", new Color(0.65f, 0.65f, 0.65f));
            clay.SetFloat("_Smoothness", 0.08f);
            try
            {
                for (int index = 0; index < renderers.Length; index++)
                {
                    MeshRenderer renderer = renderers[index];
                    Material[] slots = renderer.sharedMaterials;
                    originals[index] = slots;
                    var substitute = new Material[slots.Length];
                    Array.Fill(substitute, clay);
                    renderer.sharedMaterials = substitute;
                }
                frame.Save(path);
            }
            finally
            {
                for (int index = 0; index < renderers.Length; index++)
                {
                    if (originals[index] != null)
                        renderers[index].sharedMaterials = originals[index];
                }
                UnityEngine.Object.DestroyImmediate(clay);
            }
        }

        public static void CaptureFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            string output = Argument(args, "-weatherOut");
            if (string.IsNullOrWhiteSpace(output))
                throw new ArgumentException("Specify -weatherOut with the capture directory.");
            Directory.CreateDirectory(output);
            bool draft = Array.IndexOf(args, "-weatherDraft") >= 0;
            string filter = Argument(args, "-weatherSite");
            string seedValue = Argument(args, "-weatherSeed");
            int seed = seedValue == null ? 17 : int.Parse(seedValue, CultureInfo.InvariantCulture);
            var sample = CreateScene();
            sample.ShowControls = false;
            RequireShaders();

            int width = draft ? 1088 : 1600;
            int height = draft ? 612 : 900;
            var report = new StringBuilder();
            report.AppendLine(SystemInfo.graphicsDeviceType + " / " + SystemInfo.graphicsDeviceName);
            report.AppendLine(width + " x " + height + " / median milliseconds for Camera.Render plus synchronous pixel readback");
            report.AppendLine("Preset\tSetup ms\tClear render ms\tWeather render ms");
            int captured = 0;
            using (var frame = new Capture(sample.SampleCamera, width, height, () => sample.PrepareStreetReflection()))
            {
                foreach (WeatherSampleSite site in Enum.GetValues(typeof(WeatherSampleSite)))
                {
                    if (filter != null && !site.ToString().Equals(filter, StringComparison.OrdinalIgnoreCase))
                        continue;
                    captured++;
                    var watch = Stopwatch.StartNew();
                    sample.Open(site, seed);
                    watch.Stop();
                    double setup = watch.Elapsed.TotalMilliseconds;
                    string stem = Path.Combine(output, site.ToString());
                    if (site == WeatherSampleSite.StreetThunderstorm)
                        CheckStreet(sample);

                    sample.SetFrame(sample.FeaturedTime, true);
                    frame.Save(stem + "-clear.png");
                    double baseline = draft ? 0 : frame.MedianRenderMilliseconds();
                    sample.SetFrame(sample.FeaturedTime, false);
                    frame.Save(stem + "-weather.png");
                    double withWeather = draft ? 0 : frame.MedianRenderMilliseconds();
                    sample.PlaceView(1);
                    frame.Save(stem + "-alternate.png");
                    sample.PlaceView(2);
                    frame.Save(stem + "-detail.png");
                    if (site == WeatherSampleSite.StreetThunderstorm)
                    {
                        sample.SetFrame(17f, false);
                        frame.Save(stem + "-arriving.png");
                        sample.SetFrame(77f, false);
                        frame.Save(stem + "-passed.png");
                        sample.PlaceView(1);
                        sample.SetFrame(40.31f, false);
                        frame.Save(stem + "-lightning.png");
                    }
                    if (site == WeatherSampleSite.MorningGlory || site == WeatherSampleSite.GreatPlainsSupercell ||
                        site == WeatherSampleSite.RidgeCloudBands)
                    {
                        sample.PlaceView(0);
                        sample.SetFrame(104f, false);
                        frame.Save(stem + "-later.png");
                    }

                    report.AppendLine(string.Join("\t", site.ToString(), Format(setup),
                        draft ? "-" : Format(baseline), draft ? "-" : Format(withWeather)));
                }
            }
            if (captured == 0)
                throw new ArgumentException("Unknown weather sample: " + filter);
            File.WriteAllText(Path.Combine(output, "capture-timing.tsv"), report.ToString());
        }

        private static void RequireShaders()
        {
            foreach (string resource in new[] { "Volume", "Precipitation", "Puddle", "SampleSurface", "SampleSky", "StreetPaving" })
            {
                Shader shader = Resources.Load<Shader>("VastcoreWeather/" + resource);
                if (!shader || !shader.isSupported || ShaderUtil.ShaderHasError(shader))
                    throw new InvalidOperationException("Weather shader is missing or invalid: " + resource);
            }
        }

        private static void CheckStreet(WeatherEnvironmentSample sample)
        {
            Vector3 curb = new Vector3(-4f, 0f, -27f);
            sample.SetFrame(2f, false);
            if (sample.Field.RainAt(curb) > 0f || sample.Field.WetnessAt(curb) > 0f)
                throw new InvalidOperationException("The curb is wet before the cell arrives.");
            sample.SetFrame(42f, false);
            if (sample.Field.RainAt(curb) < 0.8f || sample.Field.WetnessAt(curb) < 0.8f ||
                sample.Field.RainAt(curb + Vector3.right * 350f) > 0f)
                throw new InvalidOperationException("The street's rainfall should be strong and localized.");
            sample.SetFrame(77f, false);
            if (sample.Field.RainAt(curb) > 0f || sample.Field.WetnessAt(curb) < 0.2f)
                throw new InvalidOperationException("Standing water should persist after the rain passes.");
            sample.Field.enabled = false;
            if (sample.Field.RainAt(curb) != 0f || sample.Field.WetnessAt(curb) != 0f)
                throw new InvalidOperationException("A disabled weather field should stop driving puddles.");
            sample.Field.enabled = true;
        }

        private static string Argument(string[] args, string key)
        {
            int index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        private static string Format(double value) => value.ToString("0.00", CultureInfo.InvariantCulture);

        private sealed class Capture : IDisposable
        {
            private readonly Camera camera;
            private readonly RenderTexture target;
            private readonly Texture2D image;
            private readonly RenderTexture previous;
            private readonly Action prepareFrame;

            public Capture(Camera camera, int width, int height, Action prepareFrame)
            {
                this.camera = camera;
                this.prepareFrame = prepareFrame;
                previous = camera.targetTexture;
                target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                target.Create();
                image = new Texture2D(width, height, TextureFormat.RGB24, false);
                camera.targetTexture = target;
                camera.aspect = width / (float)height;
            }

            public void Save(string path)
            {
                Render(new Rect(0, 0, target.width, target.height));
                File.WriteAllBytes(path, image.EncodeToPNG());
            }

            public double MedianRenderMilliseconds()
            {
                var sample = new double[7];
                var pixel = new Rect(target.width / 2, target.height / 2, 1, 1);
                Render(pixel);
                for (int index = 0; index < sample.Length; index++)
                {
                    var watch = Stopwatch.StartNew();
                    Render(pixel);
                    sample[index] = watch.Elapsed.TotalMilliseconds;
                }
                Array.Sort(sample);
                return sample[sample.Length / 2];
            }

            private void Render(Rect readback)
            {
                prepareFrame?.Invoke();
                camera.Render();
                RenderTexture previousTarget = RenderTexture.active;
                RenderTexture.active = target;
                try
                {
                    image.ReadPixels(readback, 0, 0, false);
                }
                finally
                {
                    RenderTexture.active = previousTarget;
                }
            }

            public void Dispose()
            {
                camera.targetTexture = previous;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
