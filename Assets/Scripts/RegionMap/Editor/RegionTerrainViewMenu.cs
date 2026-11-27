using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

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
        public static void OpenLook() { OpenLook(RegionAtmosphere.Preset.ClearAfternoon, false); }

        [MenuItem("Vastcore/Region Map/Open 3D Mesa Look + DS-011 Props Proof")]
        public static void OpenLookWithPropsProof() { OpenLook(RegionAtmosphere.Preset.ClearAfternoon, true); }

        [MenuItem("Vastcore/Region Map/Open 3D Grassland Env Sample")]
        public static void OpenGrasslandSample()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                EditorUtility.DisplayProgressBar("Region Map", "草原の環境セットサンプルを作っています…", 0.3f);
                RegionAtmosphere atmosphere = RegionAtmosphere.Create(RegionAtmosphere.Preset.ClearAfternoon);
                var lookSpec = RegionLookSpec.CreateGrasslandSample();
                RegionTerrainView.LookResult look = CreateLookScene(new RegionSpec { Seed = 8 }, lookSpec, atmosphere, out Camera camera);
                RegionLookViewpoint startView = look.Scene.PropsSlice?.GrasslandEyeViewpoint ?? look.Scene.Spec.Mid;
                RegionTerrainView.PlaceLook(camera.transform, look.Scene, startView);
                camera.fieldOfView = startView.FovDegrees;
                RegionLookSky.ApplyInteractive(camera, look, atmosphere, RegionLookSky.MixedCloudTime(look.Scene));
                SceneView view = SceneView.lastActiveSceneView;
                if (view != null) view.AlignViewToObject(camera.transform);
                Selection.activeGameObject = look.Props?.RealRoot ?? look.Root;
                Debug.Log("[RegionMap] 3D 草原サンプル: " + DescribeLook(look));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        [MenuItem("Vastcore/Region Map/3D Mesa Look Atmosphere/Clear afternoon")]
        public static void OpenLookClear() { OpenLook(RegionAtmosphere.Preset.ClearAfternoon, false); }

        [MenuItem("Vastcore/Region Map/3D Mesa Look Atmosphere/Golden hour")]
        public static void OpenLookGolden() { OpenLook(RegionAtmosphere.Preset.GoldenHour, false); }

        [MenuItem("Vastcore/Region Map/3D Mesa Look Atmosphere/Dust haze")]
        public static void OpenLookDust() { OpenLook(RegionAtmosphere.Preset.DustHaze, false); }

        private static void OpenLook(RegionAtmosphere.Preset preset, bool withPropsProof)
        {
            if (EditorApplication.isPlaying)
            {
                OpenLookFromExplorer(preset);
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                EditorUtility.DisplayProgressBar("Region Map", "地域と、砂岩の台地の場面を作っています…", 0.3f);
                RegionAtmosphere atmosphere = RegionAtmosphere.Create(preset);
                var lookSpec = new RegionLookSpec { Props = withPropsProof, PropsRealAssets = withPropsProof };
                RegionTerrainView.LookResult look = CreateLookScene(new RegionSpec { Seed = 9 }, lookSpec,
                    atmosphere, out Camera camera);
                RegionLookViewpoint startView = withPropsProof && look.Scene.PropsSlice != null
                    ? look.Scene.PropsSlice.CratesViewpoint
                    : look.Scene.Spec.Mid;
                RegionTerrainView.PlaceLook(camera.transform, look.Scene, startView);
                camera.fieldOfView = startView.FovDegrees;
                if (preset != RegionAtmosphere.Preset.DustHaze)
                    RegionLookSky.ApplyInteractive(camera, look, atmosphere, RegionLookSky.MixedCloudTime(look.Scene));
                SceneView view = SceneView.lastActiveSceneView;
                if (view != null) view.AlignViewToObject(camera.transform);
                Selection.activeGameObject = withPropsProof && look.Props != null ? look.Props.RealRoot ?? look.Props.Root : look.Root;
                Debug.Log("[RegionMap] 3D: " + DescribeLook(look) + "。視点は「Region Terrain Look/Viewpoints」の far・mid・near と、適地がある場合は alcove-mid・alcove-near・alcove-shelf" +
                    (withPropsProof ? "、Props Proof は props-crates・props-rubble" : ""));
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void OpenLookFromExplorer(RegionAtmosphere.Preset preset)
        {
            var explorer = UnityEngine.Object.FindFirstObjectByType<RegionExplorer>();
            if (explorer == null || !explorer.TryGetGroundViewInput(out RegionData region, out Vector2 position,
                out float cover, out float time, out float crossSeconds))
            {
                Debug.LogWarning("[RegionMap] 探索地図の生成が終わってから、地上で見る地点を選んでください。");
                return;
            }

            Scene previous = SceneManager.GetActiveScene();
            Scene preview = SceneManager.CreateScene("Region Mesa Look");
            SceneManager.SetActiveScene(preview);
            try
            {
                EditorUtility.DisplayProgressBar("Region Map", "選んだ地点と雲を地上で描いています…", 0.5f);
                RegionBiome biome = region.BiomeAtMap(position.x, position.y);
                // 草原の固定配置は専用のサンプルメニューで開く。
                RegionLookSpec spec = RegionLookSpec.ForBiome(biome, position.x, position.y);
                RegionAtmosphere atmosphere = RegionAtmosphere.Create(preset);
                atmosphere.PrepareSun();
                RegionTerrainView.LookResult look = CreateLookStage(region, spec, atmosphere, out Camera camera);
                float x = look.Scene.CenterX;
                float z = look.Scene.CenterZ;
                camera.transform.SetPositionAndRotation(new Vector3(x, look.Scene.HeightAt(x, z) + 1.7f, -z),
                    Quaternion.LookRotation(new Vector3(0f, 0.25f, 1f)));
                camera.fieldOfView = 60f;
                camera.gameObject.AddComponent<RegionGroundLook>();
                RegionLookSky.ApplyInteractive(camera, look, atmosphere, time, cover, crossSeconds,
                    useWeatherMaker: preset != RegionAtmosphere.Preset.DustHaze);
                foreach (GameObject root in explorer.gameObject.scene.GetRootGameObjects())
                    root.SetActive(false);
                Selection.activeGameObject = look.Root;
                Debug.Log(FormattableString.Invariant(
                    $"[RegionMap] 探索の雲とバイオーム({biome})を引継ぎ: seed {region.Spec.Seed}, X {position.x:F4}, Y {position.y:F4}, t {time:F3}s, cover {cover:F3}, cross {crossSeconds:F3}s"));
            }
            catch
            {
                SceneManager.SetActiveScene(previous);
                SceneManager.UnloadSceneAsync(preview);
                throw;
            }
            finally
            {
                RegionVolumePatch.SunAltitudeDegrees = RegionVolumePatch.DefaultSunAltitudeDegrees;
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
            return CreateLookStage(region, lookSpec, atmosphere, out camera);
        }

        private static RegionTerrainView.LookResult CreateLookStage(RegionData region, RegionLookSpec lookSpec,
            RegionAtmosphere atmosphere, out Camera camera)
        {
            RegionTerrainView.LookResult look = RegionTerrainView.BuildLook(region, lookSpec);
            float ground = look.Scene.HeightAt(look.Scene.CenterX, look.Scene.CenterZ);
            // 外側の地形の対角線（6 km 四方で約 8.5 km）が収まる距離まで描く
            camera = CreateStage(lookSpec.OuterKm * 1000f * 1.5f + 1000f, atmosphere, ground);
            look.BindLookCamera(camera);
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
        /// 空・霞・雲: -lookSky auto（既定。Weather Maker が入っていれば使い、無ければ自作の空と霞。砂塵は常に自作）・weathermaker・ours。
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
                bool grasslandSample = RegionMapMenu.Argument("-grasslandSample", "false") == "true";
                string lookBiomeArg = RegionMapMenu.Argument("-lookBiome", "");
                var regionSpec = new RegionSpec { Seed = int.Parse(RegionMapMenu.Argument("-regionSeed", grasslandSample ? "8" : "9")) };
                bool propsCompare = RegionMapMenu.Argument("-propsCompare", "false") == "true";
                bool propsProof = propsCompare || grasslandSample || RegionMapMenu.Argument("-propsProof", "false") == "true";
                bool propsReal = propsProof && (grasslandSample || RegionMapMenu.Argument("-propsReal", "false") == "true");
                string center = RegionMapMenu.Argument("-lookCenter", "");
                RegionLookSpec spec;
                if (grasslandSample)
                {
                    spec = RegionLookSpec.CreateGrasslandSample();
                    if (center.Length > 0)
                    {
                        string[] xy = center.Split(',');
                        if (xy.Length != 2) throw new ArgumentException("-lookCenter X,Y");
                        spec.CenterMapX = float.Parse(xy[0], CultureInfo.InvariantCulture);
                        spec.CenterMapY = float.Parse(xy[1], CultureInfo.InvariantCulture);
                    }
                }
                else if (lookBiomeArg.Length > 0)
                {
                    float cx = 43.61f, cy = 76.92f;
                    if (center.Length > 0)
                    {
                        string[] xy = center.Split(',');
                        if (xy.Length != 2) throw new ArgumentException("-lookCenter X,Y");
                        cx = float.Parse(xy[0], CultureInfo.InvariantCulture);
                        cy = float.Parse(xy[1], CultureInfo.InvariantCulture);
                    }
                    RegionBiome resolvedBiome;
                    if (string.Equals(lookBiomeArg, "auto", StringComparison.OrdinalIgnoreCase))
                    {
                        RegionData probe = RegionGenerator.Generate(regionSpec);
                        resolvedBiome = probe.BiomeAtMap(cx, cy);
                    }
                    else if (!Enum.TryParse(lookBiomeArg, true, out resolvedBiome))
                    {
                        throw new ArgumentException("-lookBiome は auto または RegionBiome 名: " + lookBiomeArg);
                    }
                    spec = RegionLookSpec.ForBiome(resolvedBiome, cx, cy, propsProof, propsReal);
                }
                else
                {
                    spec = new RegionLookSpec();
                    spec.Props = propsProof;
                    spec.PropsRealAssets = propsReal;
                    if (center.Length > 0)
                    {
                        string[] xy = center.Split(',');
                        if (xy.Length != 2) throw new ArgumentException("-lookCenter X,Y");
                        spec.CenterMapX = float.Parse(xy[0], CultureInfo.InvariantCulture);
                        spec.CenterMapY = float.Parse(xy[1], CultureInfo.InvariantCulture);
                    }
                }
                // -lookSediment 0 で土砂の場を流さない（T3 より前の地形。地図が無いので層も出ない）
                spec.GrasslandCover = RegionMapMenu.Argument("-grasslandCover", "true") != "false";
                string style = RegionMapMenu.Argument("-grasslandStyle", spec.GrasslandStyle.ToString());
                if (!Enum.TryParse(style, true, out spec.GrasslandStyle) || !Enum.IsDefined(typeof(RegionGrasslandStyle), spec.GrasslandStyle))
                    throw new ArgumentException("-grasslandStyle expects " + string.Join(", ", Enum.GetNames(typeof(RegionGrasslandStyle))) + ". Got " + style);
                string sediment = RegionMapMenu.Argument("-lookSediment", "");
                if (sediment.Length > 0) spec.Sediment = float.Parse(sediment, CultureInfo.InvariantCulture);
                // -lookRockGround false で岩と地面のなじませと分級（T4）を入れない（T3 までの岩の置き方と描き方）
                spec.RockGround = RegionMapMenu.Argument("-lookRockGround", "true") != "false";
                // -lookPreset clear,golden,dust のように複数を書くと、条件ごとに場面を作り直して <出力>/<条件> へ撮る
                string[] presets = RegionMapMenu.Argument("-lookPreset", "clear").Split(',');
                bool subfolders = presets.Length > 1;
                // Weather Maker を使う時も、砂塵は自作の空と霞で撮る（RegionLookSky.UsesOwnSky）。自作の分を先に撮り、Weather Maker の分は後の手順で撮る
                string[] ownSky = presets;
                string[] weatherMaker = new string[0];
                if (UseWeatherMaker())
                {
                    ownSky = Array.FindAll(presets, RegionLookSky.UsesOwnSky);
                    weatherMaker = Array.FindAll(presets, name => !RegionLookSky.UsesOwnSky(name));
                }
                foreach (string presetName in ownSky)
                {
                    if (!RegionAtmosphere.TryParse(presetName, out RegionAtmosphere.Preset preset))
                        throw new ArgumentException("-lookPreset は clear・golden・dust: " + presetName);
                    RegionAtmosphere atmosphere = RegionAtmosphere.Create(preset);
                    string folder = subfolders ? Path.Combine(output, atmosphere.Name) : output;
                    Directory.CreateDirectory(folder);
                    watch.Restart();
                    RegionTerrainView.LookResult look = CreateLookScene(regionSpec, spec, atmosphere, out Camera camera);
                    float cloudTime = 0f;
                    if (preset != RegionAtmosphere.Preset.DustHaze)
                    {
                        string timeArgument = RegionMapMenu.Argument("-cloudTime", "auto");
                        cloudTime = timeArgument == "auto" ? RegionLookSky.MixedCloudTime(look.Scene)
                            : float.Parse(timeArgument, CultureInfo.InvariantCulture);
                        RegionLookSky.UseOwnClouds(camera, look, cloudTime, highOnly: RegionLookSky.HighOnly);
                    }
                    Debug.Log("[RegionMap] 3D: " + DescribeLook(look) + " 光と空気 " + atmosphere.Name + " 作成 " + watch.ElapsedMilliseconds + "ms");
                    string blendDepth = RegionMapMenu.Argument("-blendDepth", "");
                    if (blendDepth.Length > 0)
                    {
                        RegionSurfaceMaterials.BlendDepthOverride = float.Parse(blendDepth, CultureInfo.InvariantCulture);
                        RegionSurfaceMaterials.SetActive(true);
                    }
                    Debug.Log("[RegionMap] 表面の写真素材: " + RegionSurfaceMaterials.Summary);
                    Debug.Log("[RegionMap] 土砂の場 " + spec.Sediment.ToString(CultureInfo.InvariantCulture) + "、土砂の地図を結び付けた窓 " + RegionSedimentLayers.Bound
                        + "、岩と地面のなじませ " + (RegionSedimentLayers.RockGroundMaterial != null ? "入" : "切"));
                    // 作った直後の最初の 1 枚は、地面の画像が貼られる前の白い地面で写る（2026-11-25 に、最初と最後に同じ視点を撮って確かめた）。
                    // 捨てコマを 1 枚描いてから撮る
                    RegionTerrainView.PlaceLook(camera.transform, look.Scene, look.Scene.Spec.Far);
                    Capture(camera, null, null);
                    if (look.Alcove != null && RegionMapMenu.Argument("-alcoveReview", "false") == "true")
                        CaptureAlcoveComparison(camera, look, folder);
                    if (propsCompare)
                        CapturePropsComparison(camera, look, folder);
                    else if ((grasslandSample || look.Scene?.PropsSlice?.UseGrasslandVariants == true) && look.Props != null && look.Scene?.PropsSlice != null)
                        CaptureGrasslandSampleViews(camera, look, look.Scene.PropsSlice, folder);
                    CaptureLookViews(camera, look, folder);
                    if (RegionMapMenu.Argument("-grasslandTiming", "false") == "true")
                    {
                        CaptureToggleComparison(camera, look, folder, "grassland-blades", "草の立体描画",
                            look.HasGrasslandCover, look.SetGrasslandBladesVisible, GrasslandGroundViews(look));
                        CaptureGrasslandMasks(camera, look, folder);
                    }
                    if (RegionMapMenu.Argument("-reliefCompare", "false") == "true")
                        CaptureToggleComparison(camera, look, folder, "relief", "凹凸", RegionSurfaceRelief.Baked, RegionSurfaceRelief.SetActive);
                    if (RegionMapMenu.Argument("-surfaceCompare", "false") == "true")
                        CaptureToggleComparison(camera, look, folder, "surface", "写真素材", RegionSurfaceMaterials.Count > 0, RegionSurfaceMaterials.SetActive);
                    if (RegionMapMenu.Argument("-tileBreakCompare", "false") == "true")
                        CaptureToggleComparison(camera, look, folder, "tilebreak", "繰り返しの崩し", RegionSurfaceMaterials.Count > 0, on =>
                        {
                            RegionSurfaceMaterials.TileBreak = on;
                            RegionSurfaceMaterials.SetActive(true);
                        });
                    if (RegionMapMenu.Argument("-sedimentCompare", "false") == "true")
                        CaptureToggleComparison(camera, look, folder, "sediment", "土砂の層", RegionSedimentLayers.Bound > 0, RegionSedimentLayers.SetActive);
                    if (RegionMapMenu.Argument("-rockGroundCompare", "false") == "true")
                        CaptureToggleComparison(camera, look, folder, "rockground", "岩の下の方の地面の色", RegionSedimentLayers.RockGroundMaterial != null,
                            RegionSedimentLayers.SetRockGround);
                    if (RegionMapMenu.Argument("-blendCompare", "false") == "true")
                        CaptureToggleComparison(camera, look, folder, "blend", "高さの混合", RegionSurfaceMaterials.Count > 0,
                            on => { RegionSurfaceMaterials.HeightBlend = on; RegionSurfaceMaterials.SetActive(true); });
                    if (look.Alcove == null) DescribeNearestCap(look.Scene);
                    if (preset != RegionAtmosphere.Preset.DustHaze && ownSky.Length == 1 && weatherMaker.Length == 0
                        && RegionMapMenu.Argument("-skyCompare", "false") == "true")
                    {
                        var steps = new RegionEditorSteps();
                        RegionLookSky.AddComparisonSteps(steps, () => camera, () => look, () => cloudTime, folder,
                            RegionLookSky.OwnCloudComparisonPlane);
                        string nextTime = RegionMapMenu.Argument("-skyCompareNext", "");
                        bool secondSnapshot = !string.IsNullOrEmpty(nextTime);
                        if (secondSnapshot)
                        {
                            float secondTime = float.Parse(nextTime, CultureInfo.InvariantCulture);
                            string secondFolder = Path.Combine(folder, "second");
                            Directory.CreateDirectory(secondFolder);
                            steps.Add(() =>
                            {
                                RegionLookSky.UseOwnClouds(camera, look, secondTime, highOnly: RegionLookSky.HighOnly);
                                CaptureLookViews(camera, look, secondFolder);
                            }, 0);
                            RegionLookSky.AddComparisonSteps(steps, () => camera, () => look, () => secondTime, secondFolder,
                                RegionLookSky.OwnCloudComparisonPlane);
                        }
                        steps.Run(secondSnapshot ? 600 : 300, error =>
                        {
                            if (error != null) Debug.LogError("[RegionMap] 雲の比較に失敗: " + error);
                            EditorApplication.Exit(error == null ? 0 : 1);
                        });
                        return;
                    }
                    CaptureSunSweep(camera, look, folder);
                }
                RegionVolumePatch.SunAltitudeDegrees = RegionVolumePatch.DefaultSunAltitudeDegrees;
                if (weatherMaker.Length > 0)
                {
                    // 終わると、手順の最後で終了する
                    CaptureWithWeatherMaker(regionSpec, spec, weatherMaker, subfolders, output);
                    return;
                }
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
        private static void CaptureWithWeatherMaker(RegionSpec regionSpec, RegionLookSpec spec, string[] presets, bool subfolders, string output)
        {
            string timeArgument = RegionMapMenu.Argument("-cloudTime", "auto");
            bool compare = RegionMapMenu.Argument("-skyCompare", "false") == "true";
            bool propsCompare = RegionMapMenu.Argument("-propsCompare", "false") == "true";
            bool sunView = RegionMapMenu.Argument("-sunView", "false") == "true";
            bool hdrProbe = RegionMapMenu.Argument("-hdrProbe", "false") == "true";
            RegionWeatherMaker.CoverFullDensity = float.Parse(RegionMapMenu.Argument("-cloudCoverFull",
                RegionWeatherMaker.CoverFullDensity.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
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
                string folder = subfolders ? Path.Combine(output, atmosphere.Name) : output;
                Directory.CreateDirectory(folder);
                steps.Add(() =>
                {
                    watch.Restart();
                    look = CreateLookScene(regionSpec, spec, atmosphere, out camera);
                    cloudTime = timeArgument == "auto" ? RegionLookSky.MixedCloudTime(look.Scene) : float.Parse(timeArgument, CultureInfo.InvariantCulture);
                    RegionLookSky.Use(camera, look, atmosphere, cloudTime);
                    Debug.Log("[RegionMap] 3D: " + DescribeLook(look) + " 光と空気 " + atmosphere.Name + "（空 Weather Maker）作成 " + watch.ElapsedMilliseconds + "ms");
                }, 240);
                RegionLookSky.AddHazeSteps(steps, () => camera, () => look, atmosphere);
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
                    if (hdrProbe && view.Name == "mid") steps.Add(() => ProbeHdr(camera, atmosphere.Name + " 中景"));
                }
                if (sunView)
                {
                    // 太陽の側の雲（夕日を受けた雲の縁）を見る 1 枚。中景の位置から、太陽の方位の 35 度横を 12 度見上げる
                    steps.Add(() =>
                    {
                        // 向きは場面で実際に光っている太陽（最も強い平行光源）から取り、狙った方位とのずれを記録する
                        Vector3 planned = -(RegionTerrainView.SunRotation(atmosphere.SunAltitudeDegrees) * Vector3.forward);
                        Light brightest = null;
                        foreach (Light candidate in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                        {
                            if (candidate.type != LightType.Directional || !candidate.isActiveAndEnabled) continue;
                            if (brightest == null || candidate.intensity > brightest.intensity) brightest = candidate;
                        }
                        Vector3 toSun = brightest != null ? -brightest.transform.forward : planned;
                        float yaw = Mathf.Atan2(toSun.x, toSun.z) * Mathf.Rad2Deg + 35f;
                        Debug.Log("[RegionMap] 太陽側の視点: 光っている太陽 " + (brightest != null ? brightest.name + "（色 " + brightest.color + "・強さ " + brightest.intensity.ToString("0.00") + "）" : "なし") + " 方位 " +
                                  (Mathf.Atan2(toSun.x, toSun.z) * Mathf.Rad2Deg).ToString("0.0") + " 度・高さ " + (Mathf.Asin(Mathf.Clamp(toSun.y, -1f, 1f)) * Mathf.Rad2Deg).ToString("0.0") +
                                  " 度、狙った方位 " + (Mathf.Atan2(planned.x, planned.z) * Mathf.Rad2Deg).ToString("0.0") + " 度・高さ " + atmosphere.SunAltitudeDegrees + " 度");
                        RegionTerrainView.PlaceLook(camera.transform, look.Scene, spec.Mid);
                        camera.transform.rotation = Quaternion.Euler(-12f, yaw, 0f);
                        camera.fieldOfView = spec.Mid.FovDegrees;
                        Capture(camera, null, null);
                    });
                    steps.Add(() => Capture(camera, folder, "look-sun.png"));
                    if (hdrProbe) steps.Add(() => ProbeHdr(camera, atmosphere.Name + " 太陽側"));
                }
                if (compare) RegionLookSky.AddComparisonSteps(steps, () => camera, () => look, () => cloudTime, folder);
                if (RegionMapMenu.Argument("-grasslandTiming", "false") == "true")
                    steps.Add(() => CaptureToggleComparison(camera, look, folder, "grassland-blades", "草の立体描画",
                        look.HasGrasslandCover, look.SetGrasslandBladesVisible, GrasslandGroundViews(look)));
                if (propsCompare) steps.Add(() => CapturePropsComparison(camera, look, folder));
                else if (spec.GrasslandSample) steps.Add(() =>
                {
                    if (look?.Props != null && look.Scene?.PropsSlice != null)
                        CaptureGrasslandSampleViews(camera, look, look.Scene.PropsSlice, folder);
                });
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
        internal static void UseBuildQuality()
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
        /// 同じ場面・視点のまま、ある描き方を切った絵を &lt;出力&gt;/&lt;key&gt;-off へ撮り、
        /// 描画 1 回の時間（GPU の終わりを待つまで）を、切と入を交互に繰り返して測る（&lt;key&gt;-timing.txt）。
        /// -reliefCompare true は表面の凹凸（RegionSurfaceRelief）、-surfaceCompare true は表面の写真素材（RegionSurfaceMaterials）、
        /// -tileBreakCompare true は写真素材の繰り返しの崩し（RegionSurfaceMaterials.TileBreak）。
        /// </summary>
        private static void CaptureToggleComparison(Camera camera, RegionTerrainView.LookResult look, string output,
            string key, string label, bool available, Action<bool> setActive, RegionLookViewpoint[] groundViews = null)
        {
            if (!available)
            {
                Debug.LogWarning("[RegionMap] " + label + "が無いので、比較を撮らない");
                return;
            }
            string off = Path.Combine(output, key + "-off");
            Directory.CreateDirectory(off);
            var views = new List<(string name, Action place, float fov)>();
            foreach (RegionLookViewpoint viewpoint in look.Scene.Spec.Viewpoints())
            {
                RegionLookViewpoint v = viewpoint;
                views.Add(("look-" + v.Name, () => RegionTerrainView.PlaceLook(camera.transform, look.Scene, v), v.FovDegrees));
            }
            if (groundViews != null)
                foreach (RegionLookViewpoint view in groundViews)
                    views.Add((view.Name, () => RegionTerrainView.PlaceLook(camera.transform, look.Scene, view), view.FovDegrees));
            if (look.Alcove != null)
            {
                views.Add(("alcove-mid", () => look.Alcove.Place(camera.transform, true, 0.57f, true), 48f));
                views.Add(("alcove-near", () => look.Alcove.Place(camera.transform, false, 0.57f, true), 67f));
            }
            var report = new System.Text.StringBuilder();
            report.AppendLine("視点	大きさ	切 ms	入 ms	差 ms（MSAA4、各 " + TimingRounds * TimingBatch + " 回の中央値。GPU の同期を含む）");
            var geometry = key == "grassland-blades" ? new System.Text.StringBuilder("視点\tタイル\t三角形（視錐台と重なるタイルの合計）\n") : null;
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
                    setActive(false);
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
                                setActive(on);
                                TimedRender(camera, target, pixel);
                                for (int i = 0; i < TimingBatch; i++) (on ? timesOn : timesOff).Add(TimedRender(camera, target, pixel));
                            }
                        }
                        camera.targetTexture = null;
                        setActive(true);
                        double a = Median(timesOff), b = Median(timesOn);
                        string size = target.width + "x" + target.height;
                        report.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}	{1}	{2:F2}	{3:F2}	{4:F2}", name, size, a, b, b - a));
                        Debug.Log(string.Format(CultureInfo.InvariantCulture, "[RegionMap] " + label + "の重さ {0} {1}: 切 {2:F2} ms 入 {3:F2} ms 差 {4:F2} ms", name, size, a, b, b - a));
                    }
                    if (geometry != null)
                    {
                        (int tiles, long triangles) = look.GrasslandGeometryInView(camera);
                        geometry.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0}\t{1}\t{2}", name, tiles, triangles));
                    }
                }
            }
            finally
            {
                camera.targetTexture = null;
                setActive(true);
                foreach (RenderTexture target in targets)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
                UnityEngine.Object.DestroyImmediate(pixel);
            }
            File.WriteAllText(Path.Combine(output, key + "-timing.txt"), report.ToString());
            if (geometry != null) File.WriteAllText(Path.Combine(output, "grassland-geometry.txt"), geometry.ToString());
        }

        private const int TimingRounds = 8, TimingBatch = 6;

        private static RegionLookViewpoint[] GrasslandGroundViews(RegionTerrainView.LookResult look)
        {
            RegionPropProofSlice slice = look.Scene.PropsSlice;
            return slice?.GrasslandFootViewpoint != null
                ? new[] { slice.GrasslandFootViewpoint, slice.GrasslandEyeViewpoint } : null;
        }

        private static void CaptureGrasslandMasks(Camera camera, RegionTerrainView.LookResult look, string output)
        {
            RegionLookViewpoint[] ground = GrasslandGroundViews(look);
            if (!look.HasGrasslandCover || ground == null) return;
            look.SetGrasslandCoverageMask(true);
            try
            {
                foreach (RegionLookViewpoint view in ground)
                {
                    RegionTerrainView.PlaceLook(camera.transform, look.Scene, view);
                    camera.fieldOfView = view.FovDegrees;
                    Capture(camera, output, view.Name + "-mask.png");
                }
            }
            finally
            {
                look.SetGrasslandCoverageMask(false);
            }
        }

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

        private static void CapturePropsComparison(Camera camera, RegionTerrainView.LookResult look, string output)
        {
            if (look?.Props == null || look.Scene?.PropsSlice == null)
            {
                Debug.LogWarning("[RegionMap] DS-011 Props Proof が無いので比較を撮らない");
                return;
            }
            Directory.CreateDirectory(output);
            RegionPropProofSlice slice = look.Scene.PropsSlice;
            RegionLookViewpoint near = look.Scene.Spec.Near;

            GameObject real = look.Props.RealRoot;
            look.Props.Root.SetActive(false);
            if (real != null) real.SetActive(false);
            RegionTerrainView.PlaceLook(camera.transform, look.Scene, near);
            camera.fieldOfView = near.FovDegrees;
            Capture(camera, output, "props-near-before.png");

            look.Props.Root.SetActive(true);
            CapturePropsViews(camera, look, slice, output, "");
            if (real != null)
            {
                // 同じ場面・同じ視点で、プレースホルダの組を実アセットの組に入れ替えて撮る
                look.Props.Root.SetActive(false);
                real.SetActive(true);
                if (slice.UseGrasslandVariants)
                {
                    CaptureGrasslandViews(camera, look, slice, output);
                }
                else
                {
                    CapturePropsViews(camera, look, slice, output, "-real");
                    CaptureRubbleMasks(camera, look, slice, output, real);
                }
                Debug.Log("[RegionMap] DS-011 実アセット: モデル " + look.Props.RealModelCount + " 個、LOD0 の三角形 " + look.Props.RealTriangles
                    + "、変種 " + look.Props.RealSummary);
            }

            var report = new System.Text.StringBuilder();
            report.AppendLine(FormattableString.Invariant($"DS-011 Props Proof Slice (seed {look.Scene.RegionSeed}, count {slice.Instances.Count})"));
            for (int i = 0; i < slice.Instances.Count; i++)
            {
                RegionPropInstance inst = slice.Instances[i];
                report.AppendLine(FormattableString.Invariant(
                    $"[{i:D2}] {inst.PropId} role={inst.Role} layer={inst.StackLayer} pos=({inst.X:F2},{inst.Y:F2},{inst.Z:F2}) rot=({inst.PitchDeg:F1},{inst.YawDeg:F1},{inst.RollDeg:F1}) v2={inst.DamageState} v3={inst.ConstructionVariant} v4={inst.MaterialVariant} mesh={inst.MeshVariant} dust={inst.TopDust:F2}"));
            }
            if (real != null)
                report.AppendLine(FormattableString.Invariant($"real assets: models {look.Props.RealModelCount}, LOD0 triangles {look.Props.RealTriangles}, variants {look.Props.RealSummary}"));
            foreach (string contact in look.Props.ContactMeasurements)
                report.AppendLine(contact);
            File.WriteAllText(Path.Combine(output, "props-proof-report.txt"), report.ToString());
        }

        private static void CaptureGrasslandSampleViews(Camera camera, RegionTerrainView.LookResult look, RegionPropProofSlice slice, string output)
        {
            Directory.CreateDirectory(output);
            if (look.Props.RealRoot != null)
            {
                look.Props.Root.SetActive(false);
                look.Props.RealRoot.SetActive(true);
            }
            CaptureGrasslandViews(camera, look, slice, output);
            var report = new System.Text.StringBuilder();
            report.AppendLine(FormattableString.Invariant($"DS-012 Grassland Env Sample (seed {look.Scene.RegionSeed}, biome {look.Scene.PrimaryBiome}, count {slice.Instances.Count}, plantCounts {string.Join("/", look.PlantCounts ?? Array.Empty<int>())})"));
            for (int i = 0; i < slice.Instances.Count; i++)
            {
                RegionPropInstance inst = slice.Instances[i];
                report.AppendLine(FormattableString.Invariant(
                    $"[{i:D2}] {inst.PropId} variant={inst.MeshVariant} role={inst.Role} pos=({inst.X:F2},{inst.Y:F2},{inst.Z:F2}) dust={inst.TopDust:F2} moss={inst.MossMask:F2}"));
            }
            if (look.Props.RealRoot != null)
                report.AppendLine(FormattableString.Invariant($"real assets: models {look.Props.RealModelCount}, LOD0 triangles {look.Props.RealTriangles}, variants {look.Props.RealSummary}"));
            File.WriteAllText(Path.Combine(output, "grassland-sample-report.txt"), report.ToString());
        }

        private static void CaptureGrasslandViews(Camera camera, RegionTerrainView.LookResult look, RegionPropProofSlice slice, string output)
        {
            foreach (RegionLookViewpoint vp in new[]
            {
                slice.GrasslandEyeViewpoint,
                slice.GrasslandFootViewpoint,
                slice.GrasslandFenceViewpoint,
                slice.GrasslandPanoramaViewpoint,
                slice.GrasslandOutcropViewpoint,
                slice.GrasslandTrailViewpoint
            })
            {
                if (vp == null) continue;
                RegionTerrainView.PlaceLook(camera.transform, look.Scene, vp);
                camera.fieldOfView = vp.FovDegrees;
                Capture(camera, output, vp.Name + ".png");
                if (vp == slice.GrasslandFootViewpoint && look.HasGrasslandCover)
                {
                    look.SetGrasslandBladesVisible(false);
                    try { Capture(camera, output, "grassland-foot-without-blades.png"); }
                    finally { look.SetGrasslandBladesVisible(true); }
                }
            }
        }

        private static void CapturePropsViews(Camera camera, RegionTerrainView.LookResult look, RegionPropProofSlice slice, string output, string suffix)
        {
            RegionLookViewpoint near = look.Scene.Spec.Near;
            RegionTerrainView.PlaceLook(camera.transform, look.Scene, near);
            camera.fieldOfView = near.FovDegrees;
            Capture(camera, output, "props-near-after" + suffix + ".png");
            if (slice.CratesViewpoint != null)
            {
                RegionTerrainView.PlaceLook(camera.transform, look.Scene, slice.CratesViewpoint);
                camera.fieldOfView = slice.CratesViewpoint.FovDegrees;
                Capture(camera, output, "props-crates" + suffix + ".png");
            }
            RegionPropInstance upper = slice.Instances.Find(inst => inst.StackSupportMemberIndex >= 0);
            if (upper != null)
            {
                RegionPropInstance lower = slice.Instances.Find(inst => inst.PropId == upper.PropId &&
                    inst.ClusterIndex == upper.ClusterIndex && inst.MemberIndex == upper.StackSupportMemberIndex && inst.StackLayer == 0);
                if (lower != null)
                {
                    var focus = new Vector3(lower.X, lower.Y + 0.9f, -lower.Z);
                    var offset = new Vector3(3f, 1.15f, -3.2f);
                    camera.transform.SetPositionAndRotation(focus + offset, Quaternion.LookRotation(-offset));
                    camera.fieldOfView = 38f;
                    Capture(camera, output, "props-stack" + suffix + ".png");
                }
            }
            if (slice.RubbleViewpoint != null)
            {
                RegionTerrainView.PlaceLook(camera.transform, look.Scene, slice.RubbleViewpoint);
                camera.fieldOfView = slice.RubbleViewpoint.FovDegrees;
                Capture(camera, output, "props-rubble" + suffix + ".png");
            }
        }

        /// <summary>
        /// 瓦礫の視点で、色を測るための対を撮る（DS-011 8.1節）。影を落とさない物と、その物が無い絵の差が、物の画素だけになる。
        /// `props-rubble-mask-props.png`／`-none.png` は実アセットの組、`-rocks.png`／`-norocks.png` は地形の岩（Props は出さない）。
        /// </summary>
        private static void CaptureRubbleMasks(Camera camera, RegionTerrainView.LookResult look, RegionPropProofSlice slice, string output, GameObject real)
        {
            if (slice.RubbleViewpoint == null) return;
            RegionTerrainView.PlaceLook(camera.transform, look.Scene, slice.RubbleViewpoint);
            camera.fieldOfView = slice.RubbleViewpoint.FovDegrees;
            Transform rocks = look.Root.transform.Find("Rocks");
            SetShadows(real, false);
            Capture(camera, output, "props-rubble-mask-props.png");
            SetShadows(real, true);
            real.SetActive(false);
            Capture(camera, output, "props-rubble-mask-none.png");
            if (rocks != null)
            {
                SetShadows(rocks.gameObject, false);
                Capture(camera, output, "props-rubble-mask-rocks.png");
                SetShadows(rocks.gameObject, true);
                rocks.gameObject.SetActive(false);
                Capture(camera, output, "props-rubble-mask-norocks.png");
                rocks.gameObject.SetActive(true);
            }
            real.SetActive(true);
        }

        private static void SetShadows(GameObject root, bool on)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.shadowCastingMode = on ? ShadowCastingMode.On : ShadowCastingMode.Off;
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
            return scene.PrimaryBiome + " 中心 X" + scene.Spec.CenterMapX.ToString("F2") + " Y" + scene.Spec.CenterMapY.ToString("F2")
                + " / 内側 " + scene.Inner.Data.SizeKm.ToString("F2") + " km（1 セル " + scene.InnerCell.ToString("F2") + " m）外側 " + scene.Outer.Data.SizeKm.ToString("F1") + " km"
                + " / 広域の地図との標高の差の平均 内側 " + scene.Inner.MeanHeightDifference.ToString("F1") + " m 外側 " + scene.Outer.MeanHeightDifference.ToString("F1") + " m"
                + " / 岩 " + rocks + " 個 / 草株・低木・針葉樹・谷筋の広葉樹 " + string.Join("/", look.PlantCounts) + " / シェーダー " + shader
                + (look.HasGrasslandCover ? " / 草原の描画 " + scene.Spec.GrasslandStyle : "")
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

        /// <summary>
        /// 後処理の前（HDR・線形の値）と後（PNG と同じ sRGB の値）で、同じ画素を比べてログに書く。画面の上 1/4 のうち、
        /// 後処理の後で最も明るい 5% の画素（明るい雲）の平均を出す。明るい雲の色が後処理で白へ寄っているかを確かめる計測。
        /// 雲は前の絵を使って描くので、どちらも 2 回描いて 2 回目を読む。
        /// </summary>
        private static void ProbeHdr(Camera camera, string label)
        {
            const int width = 1600, height = 900;
            var hdr = new RenderTexture(width, height, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
            var ldr = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var hdrImage = new Texture2D(width, height, TextureFormat.RGBAHalf, false, true);
            var ldrImage = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            bool post = RegionAtmosphere.SetPostProcessing(camera, false);
            try
            {
                camera.targetTexture = hdr;
                camera.Render();
                camera.Render();
                RenderTexture.active = hdr;
                hdrImage.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                hdrImage.Apply();
                RegionAtmosphere.SetPostProcessing(camera, post);
                camera.targetTexture = ldr;
                camera.Render();
                camera.Render();
                RenderTexture.active = ldr;
                ldrImage.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                ldrImage.Apply();
            }
            finally
            {
                RegionAtmosphere.SetPostProcessing(camera, post);
                RenderTexture.active = previous;
                camera.targetTexture = null;
                hdr.Release();
                ldr.Release();
                UnityEngine.Object.DestroyImmediate(hdr);
                UnityEngine.Object.DestroyImmediate(ldr);
            }
            Color[] before = hdrImage.GetPixels();
            Color32[] after = ldrImage.GetPixels32();
            UnityEngine.Object.DestroyImmediate(hdrImage);
            UnityEngine.Object.DestroyImmediate(ldrImage);

            // 読んだ画像の行 0 は下端。上 1/4 は行 height*3/4 から上
            var top = new List<int>();
            for (int y = height * 3 / 4; y < height; y++)
                for (int x = 0; x < width; x++) top.Add(y * width + x);
            top.Sort((a, b) => Luma(after[b]).CompareTo(Luma(after[a])));
            int count = Mathf.Max(1, top.Count / 20);
            Vector3 sumBefore = Vector3.zero, sumAfter = Vector3.zero;
            float maxBefore = 0f;
            int over = 0;
            for (int i = 0; i < count; i++)
            {
                Color b = before[top[i]];
                Color32 a = after[top[i]];
                sumBefore += new Vector3(b.r, b.g, b.b);
                sumAfter += new Vector3(a.r, a.g, a.b);
                float m = Mathf.Max(b.r, Mathf.Max(b.g, b.b));
                maxBefore = Mathf.Max(maxBefore, m);
                if (m > 1f) over++;
            }
            Vector3 meanBefore = sumBefore / count, meanAfter = sumAfter / count;
            Color afterLinear = new Color(meanAfter.x / 255f, meanAfter.y / 255f, meanAfter.z / 255f).linear;
            Debug.Log("[RegionMap] HDR の計測 " + label + "：上 1/4 の明るい 5%（" + count + " 画素）。後処理の前（線形）RGB (" +
                      meanBefore.x.ToString("0.000") + ", " + meanBefore.y.ToString("0.000") + ", " + meanBefore.z.ToString("0.000") + ") R/B " +
                      (meanBefore.x / Mathf.Max(meanBefore.z, 1e-4f)).ToString("0.00") + "・最大 " + maxBefore.ToString("0.00") + "・1 を超える画素 " +
                      (100f * over / count).ToString("0") + "%。後処理の後（sRGB）RGB (" + meanAfter.x.ToString("0") + ", " + meanAfter.y.ToString("0") + ", " +
                      meanAfter.z.ToString("0") + ") R/B " + (meanAfter.x / Mathf.Max(meanAfter.z, 1e-4f)).ToString("0.00") + "・線形に直すと R/B " +
                      (afterLinear.r / Mathf.Max(afterLinear.b, 1e-4f)).ToString("0.00"));
        }

        private static float Luma(Color32 c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;

        /// <summary>カメラの絵を 1600×900 で描き、output のフォルダへ name で書き出す。name が null なら、描くだけで書き出さない。</summary>
        internal static void Capture(Camera camera, string output, string name, int width = 1600, int height = 900)
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
