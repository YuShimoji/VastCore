using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap.Editor
{
    /// <summary>
    /// 3D 表示（3D 化の試作）の入口。メニュー「Vastcore/Region Map」から開くと、新しいシーンに地形を並べる（シーンは保存しない前提）。
    /// ・砂岩の台地の場面（見え方の想定 docs/02_design/REGION_TERRAIN_LOOK_SPEC.md と見比べる。視点は遠景・中景・近景の 3 つで固定）
    /// ・崖と洞窟（局所の体積の試作）
    /// コマンドライン（-executeMethod）からは、決まった 3 つの視点を画像に残す。
    /// </summary>
    public static class RegionTerrainViewMenu
    {
        [MenuItem("Vastcore/Region Map/Open 3D Mesa Look (far, mid, near)")]
        public static void OpenLook() { OpenLook(RegionAtmosphere.Preset.ClearAfternoon); }

        [MenuItem("Vastcore/Region Map/3D Mesa Look Atmosphere/Clear afternoon")]
        public static void OpenLookClear() { OpenLook(RegionAtmosphere.Preset.ClearAfternoon); }

        [MenuItem("Vastcore/Region Map/3D Mesa Look Atmosphere/Golden hour")]
        public static void OpenLookGolden() { OpenLook(RegionAtmosphere.Preset.GoldenHour); }

        [MenuItem("Vastcore/Region Map/3D Mesa Look Atmosphere/Dust haze")]
        public static void OpenLookDust() { OpenLook(RegionAtmosphere.Preset.DustHaze); }

        private static void OpenLook(RegionAtmosphere.Preset preset)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                EditorUtility.DisplayProgressBar("Region Map", "地域と、砂岩の台地の場面を作っています…", 0.3f);
                RegionTerrainView.LookResult look = CreateLookScene(new RegionSpec { Seed = 9 }, new RegionLookSpec(), RegionAtmosphere.Create(preset), out Camera camera);
                RegionLookViewpoint mid = look.Scene.Spec.Mid;
                RegionTerrainView.PlaceLook(camera.transform, look.Scene, mid);
                camera.fieldOfView = mid.FovDegrees;
                SceneView view = SceneView.lastActiveSceneView;
                if (view != null) view.AlignViewToObject(camera.transform);
                Selection.activeGameObject = look.Root;
                Debug.Log("[RegionMap] 3D: " + DescribeLook(look) + "。視点は「Region Terrain Look/Viewpoints」の far・mid・near と、適地がある場合は alcove-mid・alcove-near・alcove-shelf");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Vastcore/Region Map/Open 3D Cliff And Cave Prototype")]
        public static void OpenPrototype()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                EditorUtility.DisplayProgressBar("Region Map", "地域と、崖・洞窟の地形を作っています…", 0.3f);
                RegionTerrainView.Result result = CreateScene(new RegionSpec { Seed = 9 }, new RegionTerrainViewSpec(), out Camera camera);
                RegionTerrainView.Place(camera.transform, result, 20f, 7f, 150f);
                SceneView view = SceneView.lastActiveSceneView;
                if (view != null) view.LookAt(result.Focus, camera.transform.rotation, 60f);
                Selection.activeGameObject = result.Root;
                Debug.Log("[RegionMap] 3D: " + Describe(result));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>新しいシーンに、地形・太陽・カメラを置く（光と空気は「澄んだ午後」）。</summary>
        public static RegionTerrainView.Result CreateScene(RegionSpec regionSpec, RegionTerrainViewSpec viewSpec, out Camera camera)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RegionAtmosphere atmosphere = RegionAtmosphere.Create(RegionAtmosphere.Preset.ClearAfternoon);
            atmosphere.PrepareSun();
            RegionData region = RegionGenerator.Generate(regionSpec);
            RegionTerrainView.Result result = RegionTerrainView.Build(region, viewSpec);
            camera = CreateStage(5000f, atmosphere, result.Focus.y);
            return result;
        }

        /// <summary>新しいシーンに、砂岩の台地の場面・太陽・カメラ・光と空気を置く。</summary>
        public static RegionTerrainView.LookResult CreateLookScene(RegionSpec regionSpec, RegionLookSpec lookSpec, RegionAtmosphere atmosphere, out Camera camera)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // 頂点に持たせる影は、場面を作る時の太陽の高さで計算する
            atmosphere.PrepareSun();
            RegionData region = RegionGenerator.Generate(regionSpec);
            RegionTerrainView.LookResult look = RegionTerrainView.BuildLook(region, lookSpec);
            float ground = look.Scene.HeightAt(look.Scene.CenterX, look.Scene.CenterZ);
            // 外側の地形の対角線（6 km 四方で約 8.5 km）が収まる距離まで描く
            camera = CreateStage(lookSpec.OuterKm * 1000f * 1.5f + 1000f, atmosphere, ground);
            return look;
        }

        /// <summary>太陽・カメラを置き、光と空気（空・霞・空からの光・後処理）を掛ける。</summary>
        private static Camera CreateStage(float farClip, RegionAtmosphere atmosphere, float groundHeight)
        {
            var sun = new GameObject("Sun");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = farClip;
            atmosphere.Apply(camera, light, groundHeight);
            return camera;
        }

        /// <summary>
        /// コマンドライン用。砂岩の台地の場面を、決まった 3 つの視点（遠景・中景・近景）で撮り、
        /// look-far.png・look-mid.png・look-near.png を -regionOut のフォルダへ書き出して終了する。毎回同じ 3 枚になる。
        /// 指定: -regionSeed 9 -regionOut 出力先のフォルダ（画面を使うので -nographics は付けない）
        /// 空・霞・雲: -lookSky auto（既定。Weather Maker が入っていれば使い、無ければ自作の空と霞）・weathermaker・ours。
        /// Weather Maker の時は -cloudTime（探索画面の雲の時刻、秒。既定 auto は場面の周りに雲と晴れ間が半々に出る時刻）、
        /// -skyCompare true で、見上げた空と同じ範囲の 2D の地図を sky-3d.png・sky-2d.png に並べて撮る。
        /// </summary>
        public static void CaptureFromCommandLine()
        {
            int exitCode = 0;
            try
            {
                string output = Path.GetFullPath(RegionMapMenu.Argument("-regionOut", "artifacts/region-map/3d-look"));
                Directory.CreateDirectory(output);
                // 最初の 1 枚が、コンパイル待ちの仮の色で写らないようにする
                ShaderUtil.allowAsyncCompilation = false;
                UseBuildQuality();
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var regionSpec = new RegionSpec { Seed = int.Parse(RegionMapMenu.Argument("-regionSeed", "9")) };
                var spec = new RegionLookSpec();
                string center = RegionMapMenu.Argument("-lookCenter", "");
                if (center.Length > 0)
                {
                    string[] xy = center.Split(',');
                    if (xy.Length != 2) throw new ArgumentException("-lookCenter X,Y");
                    spec.CenterMapX = float.Parse(xy[0], CultureInfo.InvariantCulture);
                    spec.CenterMapY = float.Parse(xy[1], CultureInfo.InvariantCulture);
                }
                // -lookPreset clear,golden,dust のように複数を書くと、条件ごとに場面を作り直して <出力>/<条件> へ撮る
                string[] presets = RegionMapMenu.Argument("-lookPreset", "clear").Split(',');
                if (UseWeatherMaker())
                {
                    // 終わると、手順の最後で終了する
                    CaptureWithWeatherMaker(regionSpec, spec, presets, output);
                    return;
                }
                foreach (string presetName in presets)
                {
                    if (!RegionAtmosphere.TryParse(presetName, out RegionAtmosphere.Preset preset))
                        throw new ArgumentException("-lookPreset は clear・golden・dust: " + presetName);
                    RegionAtmosphere atmosphere = RegionAtmosphere.Create(preset);
                    string folder = presets.Length > 1 ? Path.Combine(output, atmosphere.Name) : output;
                    Directory.CreateDirectory(folder);
                    watch.Restart();
                    RegionTerrainView.LookResult look = CreateLookScene(regionSpec, spec, atmosphere, out Camera camera);
                    Debug.Log("[RegionMap] 3D: " + DescribeLook(look) + " 光と空気 " + atmosphere.Name + " 作成 " + watch.ElapsedMilliseconds + "ms");
                    // 作った直後の最初の 1 枚は、地面の画像が貼られる前の白い地面で写る（2026-11-25 に、最初と最後に同じ視点を撮って確かめた）。
                    // 捨てコマを 1 枚描いてから撮る
                    RegionTerrainView.PlaceLook(camera.transform, look.Scene, look.Scene.Spec.Far);
                    Capture(camera, null, null);
                    if (look.Alcove != null && RegionMapMenu.Argument("-alcoveReview", "false") == "true")
                        CaptureAlcoveComparison(camera, look, folder);
                    CaptureLookViews(camera, look, folder);
                    if (RegionMapMenu.Argument("-reliefCompare", "false") == "true")
                        CaptureReliefComparison(camera, look, folder);
                    if (look.Alcove == null) DescribeNearestCap(look.Scene);
                    CaptureSunSweep(camera, look, folder);
                }
                RegionVolumePatch.SunAltitudeDegrees = RegionVolumePatch.DefaultSunAltitudeDegrees;
            }
            catch (Exception exception)
            {
                Debug.LogError("[RegionMap] 3D の撮影に失敗: " + exception);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        /// <summary>
        /// コマンドライン用。崖と洞窟（局所の体積の試作）の、崖の正面・近く・引いた所の 3 枚を -regionOut のフォルダへ書き出して終了する。
        /// 指定: -regionSeed 9 -regionOut 出力先のフォルダ（画面を使うので -nographics は付けない）
        /// </summary>
        public static void CaptureCaveFromCommandLine()
        {
            int exitCode = 0;
            try
            {
                string output = Path.GetFullPath(RegionMapMenu.Argument("-regionOut", "artifacts/region-map/3d-unity"));
                Directory.CreateDirectory(output);
                // 最初の 1 枚が、コンパイル待ちの仮の色で写らないようにする
                ShaderUtil.allowAsyncCompilation = false;
                UseBuildQuality();
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var regionSpec = new RegionSpec { Seed = int.Parse(RegionMapMenu.Argument("-regionSeed", "9")) };
                RegionTerrainView.Result result = CreateScene(regionSpec, new RegionTerrainViewSpec(), out Camera camera);
                Debug.Log("[RegionMap] 3D: " + Describe(result) + " 作成 " + watch.ElapsedMilliseconds + "ms");
                Shot(camera, result, output, "unity-front.png", 20f, 7f, 150f);
                Shot(camera, result, output, "unity-near.png", -25f, 3f, 50f);
                Shot(camera, result, output, "unity-wide.png", 40f, 18f, 420f);
            }
            catch (Exception exception)
            {
                Debug.LogError("[RegionMap] 3D の撮影に失敗: " + exception);
                exitCode = 1;
            }
            EditorApplication.Exit(exitCode);
        }

        private static bool UseWeatherMaker()
        {
            string mode = RegionMapMenu.Argument("-lookSky", "auto");
            if (mode == "ours") return false;
            string reason;
            if (RegionLookSky.CanUse(out reason)) return true;
            if (mode == "weathermaker") throw new InvalidOperationException("-lookSky weathermaker: " + reason);
            Debug.Log("[RegionMap] 空・霞は自作のまま: " + reason);
            return false;
        }

        /// <summary>
        /// Weather Maker の空・霞・雲で撮る。Weather Maker は編集時の更新を回すと落ち着くので、手順を 1 コマずつ進める（RegionEditorSteps）。
        /// </summary>
        private static void CaptureWithWeatherMaker(RegionSpec regionSpec, RegionLookSpec spec, string[] presets, string output)
        {
            string timeArgument = RegionMapMenu.Argument("-cloudTime", "auto");
            bool compare = RegionMapMenu.Argument("-skyCompare", "false") == "true";
            RegionWeatherMaker.CoverFullDensity = float.Parse(RegionMapMenu.Argument("-cloudCoverFull", "0.42"), CultureInfo.InvariantCulture);
            var steps = new RegionEditorSteps();
            RegionTerrainView.LookResult look = null;
            Camera camera = null;
            float cloudTime = 0f;
            var watch = new System.Diagnostics.Stopwatch();
            foreach (string presetName in presets)
            {
                if (!RegionAtmosphere.TryParse(presetName, out RegionAtmosphere.Preset preset))
                    throw new ArgumentException("-lookPreset は clear・golden・dust: " + presetName);
                RegionAtmosphere atmosphere = RegionAtmosphere.Create(preset);
                string folder = presets.Length > 1 ? Path.Combine(output, atmosphere.Name) : output;
                Directory.CreateDirectory(folder);
                steps.Add(() =>
                {
                    watch.Restart();
                    look = CreateLookScene(regionSpec, spec, atmosphere, out camera);
                    cloudTime = timeArgument == "auto" ? RegionLookSky.MixedCloudTime(look.Scene) : float.Parse(timeArgument, CultureInfo.InvariantCulture);
                    RegionLookSky.Use(camera, look, atmosphere, cloudTime);
                    Debug.Log("[RegionMap] 3D: " + DescribeLook(look) + " 光と空気 " + atmosphere.Name + "（空 Weather Maker）作成 " + watch.ElapsedMilliseconds + "ms");
                }, 240);
                foreach (RegionLookViewpoint viewpoint in spec.Viewpoints())
                {
                    RegionLookViewpoint view = viewpoint;
                    steps.Add(() =>
                    {
                        RegionTerrainView.PlaceLook(camera.transform, look.Scene, view);
                        camera.fieldOfView = view.FovDegrees;
                        Capture(camera, null, null);
                    });
                    steps.Add(() => Capture(camera, folder, "look-" + view.Name + ".png"));
                }
                if (compare) RegionLookSky.AddComparisonSteps(steps, () => camera, () => look, () => cloudTime, folder);
            }
            steps.Run(1800, error =>
            {
                RegionWeatherMaker.RemoveRenderFeature();
                RegionVolumePatch.SunAltitudeDegrees = RegionVolumePatch.DefaultSunAltitudeDegrees;
                if (error != null) Debug.LogError("[RegionMap] 3D の撮影に失敗: " + error);
                EditorApplication.Exit(error == null ? 0 : 1);
            });
        }

        /// <summary>
        /// 実行ファイル（Standalone）の既定の品質で撮る。編集時の品質は Mobile（ProjectSettings の m_CurrentQuality 0）で、
        /// そのままでは描画の縮小 0.8・深度画像なし・SSAO なしの Mobile_RPAsset で写る（2026-11-26 に写しの試験で確かめた）。
        /// </summary>
        private static void UseBuildQuality()
        {
            int index = Array.IndexOf(QualitySettings.names, "PC");
            if (index >= 0 && index != QualitySettings.GetQualityLevel()) QualitySettings.SetQualityLevel(index, true);
            Debug.Log("[RegionMap] 品質 " + QualitySettings.names[QualitySettings.GetQualityLevel()] + "（描画 " +
                      (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null ? UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.name : "なし") + "）");
        }

        private static void CaptureLookViews(Camera camera, RegionTerrainView.LookResult look, string output)
        {
            Directory.CreateDirectory(output);
            foreach (RegionLookViewpoint viewpoint in look.Scene.Spec.Viewpoints())
            {
                RegionTerrainView.PlaceLook(camera.transform, look.Scene, viewpoint);
                camera.fieldOfView = viewpoint.FovDegrees;
                Capture(camera, output, "look-" + viewpoint.Name + ".png");
            }
            if (look.Alcove == null) return;
            foreach (bool mid in new[] { true, false })
            {
                look.Alcove.Place(camera.transform, mid, 0.57f, true);
                camera.fieldOfView = mid ? 48f : 67f;
                Capture(camera, output, mid ? "alcove-mid.png" : "alcove-near.png");
            }
        }

        /// <summary>
        /// -reliefCompare true で、同じ場面・視点のまま表面の凹凸（RegionSurfaceRelief）を切った絵を &lt;出力&gt;/relief-off へ撮り、
        /// 描画 1 回の時間（GPU の終わりを待つまで）を、切と入を交互に繰り返して測る（relief-timing.txt）。
        /// </summary>
        private static void CaptureReliefComparison(Camera camera, RegionTerrainView.LookResult look, string output)
        {
            if (!RegionSurfaceRelief.Baked)
            {
                Debug.LogWarning("[RegionMap] 凹凸を焼いていないので、比較を撮らない");
                return;
            }
            string off = Path.Combine(output, "relief-off");
            Directory.CreateDirectory(off);
            var views = new List<(string name, Action place, float fov)>();
            foreach (RegionLookViewpoint viewpoint in look.Scene.Spec.Viewpoints())
            {
                RegionLookViewpoint v = viewpoint;
                views.Add(("look-" + v.Name, () => RegionTerrainView.PlaceLook(camera.transform, look.Scene, v), v.FovDegrees));
            }
            if (look.Alcove != null)
            {
                views.Add(("alcove-mid", () => look.Alcove.Place(camera.transform, true, 0.57f, true), 48f));
                views.Add(("alcove-near", () => look.Alcove.Place(camera.transform, false, 0.57f, true), 67f));
            }
            var report = new System.Text.StringBuilder();
            report.AppendLine("視点	大きさ	切 ms	入 ms	差 ms（MSAA4、各 " + TimingRounds * TimingBatch + " 回の中央値。3840x2160 は GPU が律速になる条件）");
            var targets = new[]
            {
                new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 },
                new RenderTexture(3840, 2160, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 },
            };
            var pixel = new Texture2D(1, 1, TextureFormat.RGB24, false);
            try
            {
                foreach ((string name, Action place, float fov) in views)
                {
                    place();
                    camera.fieldOfView = fov;
                    RegionSurfaceRelief.SetActive(false);
                    Capture(camera, off, name + ".png");
                    foreach (RenderTexture target in targets)
                    {
                        var timesOff = new List<double>();
                        var timesOn = new List<double>();
                        camera.targetTexture = target;
                        for (int i = 0; i < 6; i++) TimedRender(camera, target, pixel);
                        for (int round = 0; round < TimingRounds; round++)
                        {
                            foreach (bool on in new[] { false, true })
                            {
                                RegionSurfaceRelief.SetActive(on);
                                TimedRender(camera, target, pixel);
                                for (int i = 0; i < TimingBatch; i++) (on ? timesOn : timesOff).Add(TimedRender(camera, target, pixel));
                            }
                        }
                        camera.targetTexture = null;
                        RegionSurfaceRelief.SetActive(true);
                        double a = Median(timesOff), b = Median(timesOn);
                        string size = target.width + "x" + target.height;
                        report.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}	{1}	{2:F2}	{3:F2}	{4:F2}", name, size, a, b, b - a));
                        Debug.Log(string.Format(CultureInfo.InvariantCulture, "[RegionMap] 凹凸の重さ {0} {1}: 切 {2:F2} ms 入 {3:F2} ms 差 {4:F2} ms", name, size, a, b, b - a));
                    }
                }
            }
            finally
            {
                camera.targetTexture = null;
                RegionSurfaceRelief.SetActive(true);
                foreach (RenderTexture target in targets)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
                UnityEngine.Object.DestroyImmediate(pixel);
            }
            File.WriteAllText(Path.Combine(output, "relief-timing.txt"), report.ToString());
        }

        private const int TimingRounds = 8, TimingBatch = 6;

        // 描いて、1 画素を読み戻して GPU の終わりを待つまでの時間（ms）
        private static double TimedRender(Camera camera, RenderTexture target, Texture2D pixel)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            pixel.ReadPixels(new Rect(0, 0, 1, 1), 0, 0);
            RenderTexture.active = previous;
            return watch.Elapsed.TotalMilliseconds;
        }

        private static double Median(List<double> values)
        {
            values.Sort();
            int n = values.Count;
            return n % 2 == 1 ? values[n / 2] : 0.5 * (values[n / 2 - 1] + values[n / 2]);
        }

        /// <summary>
        /// -sunSweep 60,38,20,10 のように太陽の高さ（度）を並べると、場面を作り直さずに太陽だけを回して中景を撮る（sun-<高さ>.png）。
        /// 地形の影が描画のたびに計算されていることを確かめる。
        /// </summary>
        private static void CaptureSunSweep(Camera camera, RegionTerrainView.LookResult look, string output)
        {
            string sweep = RegionMapMenu.Argument("-sunSweep", "");
            if (sweep.Length == 0 || RenderSettings.sun == null) return;
            Quaternion original = RenderSettings.sun.transform.rotation;
            RegionTerrainView.PlaceLook(camera.transform, look.Scene, look.Scene.Spec.Mid);
            camera.fieldOfView = look.Scene.Spec.Mid.FovDegrees;
            foreach (string value in sweep.Split(','))
            {
                float altitude = float.Parse(value, CultureInfo.InvariantCulture);
                RenderSettings.sun.transform.rotation = RegionTerrainView.SunRotation(altitude);
                Capture(camera, output, "sun-" + altitude.ToString("00", CultureInfo.InvariantCulture) + ".png");
            }
            RenderSettings.sun.transform.rotation = original;
        }

        private static void CaptureAlcoveComparison(Camera camera, RegionTerrainView.LookResult look, string output)
        {
            var alcove = look.Alcove;
            var vegetation = look.Root.transform.Find("Vegetation");
            alcove.Show(false, false);
            CaptureLookViews(camera, look, Path.Combine(output, "original"));
            if (vegetation) vegetation.gameObject.SetActive(false);
            CaptureLookViews(camera, look, Path.Combine(output, "before"));
            alcove.Show(true, false);
            CaptureLookViews(camera, look, Path.Combine(output, "shape"));
            alcove.Show(true, true);
            if (vegetation) vegetation.gameObject.SetActive(true);
            string walk = Path.Combine(output, "walk");
            Directory.CreateDirectory(walk);
            camera.fieldOfView = 67f;
            for (int frame = 0; frame <= 32; frame++)
            {
                alcove.Place(camera.transform, false, frame / 32f);
                Capture(camera, walk, frame.ToString("D3") + ".png", 960, 540);
            }
        }

        private static void DescribeNearestCap(RegionLookScene scene)
        {
            var data = scene.Outer.Data;
            int nearest = -1;
            float distance = float.MaxValue;
            if (data.Caprock == null) return;
            int n = data.Resolution;
            // A lone cap cell can be an isolated pinnacle. Look for a broad
            // remnant (roughly 140 m across) before suggesting a new window.
            const int radius = 12;
            for (int z = radius; z < n - radius; z += 4)
            for (int x = radius; x < n - radius; x += 4)
            {
                int i = x + z * n;
                if (data.Caprock[i] == 0) continue;
                float dx = (x + 0.5f) * scene.OuterCell - scene.CenterX;
                float dz = (z + 0.5f) * scene.OuterCell - scene.CenterZ;
                float d = dx * dx + dz * dz;
                if (d >= distance) continue;
                int coverage = 0;
                for (int j = -radius; j <= radius; j += 2)
                for (int k = -radius; k <= radius; k += 2)
                    coverage += data.Caprock[i + k + j * n];
                if (coverage < 140) continue;
                nearest = i;
                distance = d;
            }
            if (nearest < 0) return;
            float mapCell = scene.Outer.SizeMap / data.Resolution;
            Debug.Log("[RegionMap] broad outer cap X" + (scene.Outer.OriginMapX + (nearest % data.Resolution + 0.5f) * mapCell).ToString("F5", CultureInfo.InvariantCulture)
                + " Y" + (scene.Outer.OriginMapY + (nearest / data.Resolution + 0.5f) * mapCell).ToString("F5", CultureInfo.InvariantCulture));
        }

        private static string DescribeLook(RegionTerrainView.LookResult look)
        {
            RegionLookScene scene = look.Scene;
            string shader = RegionTerrainView.ShaderResource + (Resources.Load<Shader>(RegionTerrainView.ShaderResource) != null ? "" : "（見つからず、URP の Lit で代用）");
            int rocks = scene.InnerRocks != null ? scene.InnerRocks.Rocks.Length + scene.OuterRocks.Rocks.Length : 0;
            return "砂岩の台地の場面 中心 X" + scene.Spec.CenterMapX.ToString("F2") + " Y" + scene.Spec.CenterMapY.ToString("F2")
                + " / 内側 " + scene.Inner.Data.SizeKm.ToString("F2") + " km（1 セル " + scene.InnerCell.ToString("F2") + " m）外側 " + scene.Outer.Data.SizeKm.ToString("F1") + " km"
                + " / 広域の地図との標高の差の平均 内側 " + scene.Inner.MeanHeightDifference.ToString("F1") + " m 外側 " + scene.Outer.MeanHeightDifference.ToString("F1") + " m"
                + " / 岩 " + rocks + " 個 / 草株・低木・針葉樹・谷筋の広葉樹 " + string.Join("/", look.PlantCounts) + " / シェーダー " + shader
                + (look.Alcove == null ? " / アルコーブ適地なし" : " / アルコーブ 内側 X" + look.Alcove.Site.Volume.FocusX.ToString("F1")
                + " Z" + look.Alcove.Site.Volume.FocusZ.ToString("F1") + " m 床 " + look.Alcove.Site.Floor.ToString("F1")
                + " m 天井頂点率 " + look.Alcove.Patch.DownFacingShare.ToString("P2") + " しみ出し " + look.Alcove.SeepPlants
                + " 株 崩落岩 " + look.Alcove.FallenBlocks + " 個");
        }

        private static string Describe(RegionTerrainView.Result result)
        {
            string shader = RegionTerrainView.ShaderResource + (Resources.Load<Shader>(RegionTerrainView.ShaderResource) != null ? "" : "（見つからず、URP の Lit で代用）");
            if (result.Patch == null) return "体積で作る崖が見つからなかった（地形だけを並べた）。シェーダー " + shader;
            return result.Volume.Note + " / 体積の頂点 " + result.Patch.VertexCount + " 三角形 " + result.Patch.TriangleCount
                + " / シェーダー " + shader;
        }

        private static void Shot(Camera camera, RegionTerrainView.Result result, string output, string name, float yawOffset, float pitch, float distance)
        {
            RegionTerrainView.Place(camera.transform, result, yawOffset, pitch, distance);
            Capture(camera, output, name);
        }

        /// <summary>カメラの絵を 1600×900 で描き、output のフォルダへ name で書き出す。name が null なら、描くだけで書き出さない。</summary>
        private static void Capture(Camera camera, string output, string name, int width = 1600, int height = 900)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                RenderTexture.active = previous;
                if (name == null) return;
                string path = Path.Combine(output, name);
                File.WriteAllBytes(path, image.EncodeToPNG());
                Debug.Log("[RegionMap] 3D の絵 → " + path);
            }
            finally
            {
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}
