using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vastcore.RegionMap.Editor
{
    /// <summary>
    /// 砂岩の台地の場面の空・霞・雲を、Weather Maker（購入済みの素材。git の外に置く）で描く。素材が無い環境では使わず、自作の空と霞のまま。
    /// 雲は 2D の地図と同じ雲（RegionCloudSky。同じ seed・時刻）から雲の地図を作って貼るので、2D で見た雲がその地域の空に同じ形で出る。
    /// 太陽は光と空気の条件（RegionAtmosphere）の高さと、地図と同じ北西の向きに置く。
    /// </summary>
    public static class RegionLookSky
    {
        /// <summary>雲を置く範囲（場面の中心を真ん中にした正方形の一辺、m）と、雲の地図の画素数。</summary>
        public const float CloudBoxSize = 160000f;
        public const int CloudMapSize = 1024;

        /// <summary>塊の雲の層の底と頂（地面から、m）。</summary>
        public const float CloudBase = 1500f, CloudTop = 5000f;
        private const float OwnCloudTop = 2600f;
        public const float OwnCloudComparisonPlane = CloudBase + 450f;

        /// <summary>高い薄雲（2D の地図の上の層）の平らな層の高さ（地面から、m）。</summary>
        public const float HighCloudHeight = 9000f;

        /// <summary>
        /// 雲が地平線へ向けて空の色に溶け、ほぼ見えなくなる水平の距離（m）。雲を置く範囲の端（中心から 80 km）に合わせる。
        /// Weather Maker の既定（層の厚さの 8 倍＝約 24 km を基準に溶ける）では、10 km 先の雲が 3 割ほどの濃さになり 17 km 先でほぼ消えるので、
        /// 2D の地図で見える 10〜20 km 先の雲が空に出ない。
        /// </summary>
        public static float CloudFadeMeters = 80000f;

        private const string PrefabName = "WeatherMakerPrefab";
        private const string SkyProfileName = "WeatherMakerSkyProfile_ProceduralMilkyWayPhysicallyBased";
        private const string CloudProfileName = "WeatherMakerCloudProfile_CloudBox";

        /// <summary>
        /// Weather Maker を入れた直後に 1 回行う、URP で描くための切り替え（Weather Maker の手順のうち、シェーダーの GrabPass の置き換えと UNITY_URP の定義）。
        /// 描画パイプラインの資産は差し替えない（Weather Maker の既定の手順は自分の資産へ差し替える）。描画の機能は撮影の時にメモリの中だけで足す。
        /// 取り込み時に Weather Maker が出す URP 有効化の確認画面も、既定と Mobile の描画を WeatherMakerURPProfile へ差し替える（2026-11-26 に本体で起き、戻した）。こちらを使う。
        /// コマンドラインからは -executeMethod Vastcore.RegionMap.Editor.RegionLookSky.SetUpFromCommandLine。
        /// </summary>
        [MenuItem("Vastcore/Region Map/Set Up Weather Maker For URP")]
        public static void SetUp()
        {
            Type utility = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("DigitalRuby.WeatherMaker.WeatherMakerEditorUtility", false)).FirstOrDefault(t => t != null);
            if (utility == null) throw new InvalidOperationException("Weather Maker が入っていない（Assets/WeatherMaker）");
            utility.GetMethod("ReplaceGrabPassInAllShaders").Invoke(null, new object[] { false });
            utility.GetMethod("UpdatePreProcessor").Invoke(null, new object[] { true, "UNITY_URP", null });
            AssetDatabase.SaveAssets();
            Debug.Log("[RegionMap] Weather Maker を URP 用に切り替えた（再コンパイルの後に使える）");
        }

        public static void SetUpFromCommandLine()
        {
            int code = 0;
            try { SetUp(); }
            catch (Exception exception) { Debug.LogError("[RegionMap] Weather Maker の切り替えに失敗: " + exception); code = 1; }
            EditorApplication.Exit(code);
        }

        /// <summary>
        /// Weather Maker が使える時も、自作の空と霞で撮る光と空気の条件か（-lookPreset の名前）。砂塵は自作（本人の選択、2026-11-26）。
        /// Weather Maker の空は砂塵でも晴れと同じに描かれ、空の濁りを上げると太陽の側が青緑に転んだ（m17・m22）。
        /// </summary>
        public static bool UsesOwnSky(string presetName)
        {
            return RegionAtmosphere.TryParse(presetName, out RegionAtmosphere.Preset preset) && preset == RegionAtmosphere.Preset.DustHaze;
        }

        /// <summary>Weather Maker の素材が、このプロジェクトに入っていて使える状態か。使えない時は理由を返す。</summary>
        public static bool CanUse(out string reason)
        {
            if (!RegionWeatherMaker.Available)
            {
                reason = RegionWeatherMaker.Find("WeatherMakerScript") == null
                    ? "Weather Maker が入っていない（Assets/WeatherMaker。購入済みの素材で、git には置かない）"
                    : "Weather Maker の URP 用の描画が無い（Scripting Define Symbols に UNITY_URP が要る）";
                return false;
            }
            if (FindAsset<GameObject>(PrefabName, "Prefab") == null)
            {
                reason = "Weather Maker の Prefab（" + PrefabName + "）が見つからない";
                return false;
            }
            reason = null;
            return true;
        }

        public static void ApplyInteractive(Camera camera, RegionTerrainView.LookResult look, RegionAtmosphere atmosphere,
            float cloudTime, float? cover = null, float crossSeconds = RegionCloudFlow.DefaultCrossSeconds,
            bool useWeatherMaker = true)
        {
            if (useWeatherMaker && CanUse(out _))
            {
                try
                {
                    Use(camera, look, atmosphere, cloudTime, cover, crossSeconds);
                }
                catch
                {
                    RegionWeatherMaker.RemoveRenderFeature();
                    throw;
                }
                ReleaseWithScene(camera.gameObject.scene, RegionWeatherMaker.RemoveRenderFeature);
                var steps = new RegionEditorSteps();
                steps.Add(() => { }, 240);
                steps.Add(() =>
                {
                    if (camera == null || look.Root == null || !camera.gameObject.scene.isLoaded) return;
                    var (away, toward) = ReadInteractiveHorizon(camera, look.Root, Eye(look.Scene), HazeSunYaw(atmosphere));
                    if (!ValidHorizon(away) || !ValidHorizon(toward))
                        throw new InvalidOperationException("地平線の空の色を読めなかったため、場面の霞を使用する");
                    SetHazeColors(atmosphere, away, toward);
                }, 0);
                steps.Run(120, error =>
                {
                    if (error != null && camera != null)
                        Debug.LogWarning("[RegionMap] Weather Maker の霞を合わせられなかった: " + error.Message);
                });
            }
            else
            {
                UseOwnClouds(camera, look, cloudTime, cover, crossSeconds);
            }
        }

        public static void UseOwnClouds(Camera camera, RegionTerrainView.LookResult look, float cloudTime,
            float? cover = null, float crossSeconds = RegionCloudFlow.DefaultCrossSeconds, bool highOnly = false)
        {
            Material sky = RenderSettings.skybox;
            if (sky == null || !sky.HasProperty("_RegionCloudMap"))
                throw new InvalidOperationException("共有の雲を描く Region Sky の素材が見つからない");
            RegionCloudSkySpec spec = CloudSpec(look.Scene, cloudTime, cover, crossSeconds);
            RegionCloudSky.Build(spec, out float[] low, out float[] high);
            var pixels = new Color[low.Length];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(highOnly ? 0f : low[i], high[i], 0f, 1f);
            var density = new Texture2D(spec.Resolution, spec.Resolution, TextureFormat.RGBAHalf, true, true)
            {
                name = "Region Cloud Density", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear,
                hideFlags = HideFlags.DontSave,
            };
            density.SetPixels(pixels);
            density.Apply(true, true);
            sky.SetTexture("_RegionCloudMap", density);
            sky.SetVector("_RegionCloudTransform", new Vector4(spec.MinX, spec.MinZ, 1f / spec.Size, 1f));
            float ground = look.Scene.HeightAt(look.Scene.CenterX, look.Scene.CenterZ);
            sky.SetVector("_RegionCloudView", new Vector4(ground + CloudBase, ground + OwnCloudTop,
                ground + HighCloudHeight, CloudFadeMeters));
            ReleaseWithScene(camera.gameObject.scene, () => UnityEngine.Object.DestroyImmediate(density));
            Debug.Log(FormattableString.Invariant(
                $"[RegionMap] 空 Region Sky: 共有の雲 seed {spec.RegionSeed}, t {spec.Time:F3}s, cover {spec.Cover:F3}, cross {spec.CrossSeconds:F3}s"));
        }

        private static RegionCloudSkySpec CloudSpec(RegionLookScene scene, float time, float? cover, float crossSeconds)
        {
            RegionCloudSkySpec spec = RegionCloudSky.ForScene(scene, CloudBoxSize, CloudMapSize,
                cover ?? new RegionRenderOptions().CloudCover, time);
            spec.CrossSeconds = crossSeconds;
            return spec;
        }

        private static void ReleaseWithScene(Scene scene, Action release)
        {
            void Closed(Scene closed)
            {
                if (closed != scene) return;
                SceneManager.sceneUnloaded -= Closed;
                EditorSceneManager.sceneClosed -= Closed;
                release();
            }
            SceneManager.sceneUnloaded += Closed;
            EditorSceneManager.sceneClosed += Closed;
        }

        /// <summary>
        /// 場面の自作の空・霞・太陽を外し、Weather Maker を置く。cloudTime は探索画面の雲の時刻（秒）。
        /// 置いた後、空と太陽が落ち着くまで編集時の更新を数百コマ回してから撮る（RegionEditorSteps）。
        /// </summary>
        public static void Use(Camera camera, RegionTerrainView.LookResult look, RegionAtmosphere atmosphere,
            float cloudTime, float? cover = null, float crossSeconds = RegionCloudFlow.DefaultCrossSeconds)
        {
            RegionWeatherMaker.AddRenderFeature();

            // 自作の空を切る。空は Weather Maker の空の球が描くので、カメラは黒で消す（Weather Maker の手順）。
            // 地形の霞は自作のまま使い、色だけ Weather Maker の地平線の空に合わせる（AddHazeSteps）
            RenderSettings.skybox = null;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            // 自作の太陽を外し、Weather Maker の太陽を主光源にする
            GameObject ourSun = RenderSettings.sun != null ? RenderSettings.sun.gameObject : null;
            if (ourSun != null) UnityEngine.Object.DestroyImmediate(ourSun);

            GameObject weatherPrefab = FindAsset<GameObject>(PrefabName, "Prefab");
            if (Application.isPlaying) UnityEngine.Object.Instantiate(weatherPrefab);
            else PrefabUtility.InstantiatePrefab(weatherPrefab);
            RegionWeatherMaker.AllowCamera(camera);
            RegionWeatherMaker.UseFullQualityClouds();
            RegionWeatherMaker.SetSky(FindAsset<ScriptableObject>(SkyProfileName, "ScriptableObject"));

            Vector3 toSun = -(RegionTerrainView.SunRotation(atmosphere.SunAltitudeDegrees) * Vector3.forward);
            // 太陽の色は条件の値（線形の値で決めてある）にそろえる。Linear 色空間では Unity が Light.color を線形に直すので、先にガンマへ戻す
            Color sunColor = QualitySettings.activeColorSpace == ColorSpace.Linear ? atmosphere.SunColor.gamma : atmosphere.SunColor;
            RegionWeatherMaker.SetSun(atmosphere.SunAltitudeDegrees, Mathf.Atan2(toSun.x, toSun.z) * Mathf.Rad2Deg, sunColor);

            var watch = System.Diagnostics.Stopwatch.StartNew();
            RegionCloudSkySpec spec = CloudSpec(look.Scene, cloudTime, cover, crossSeconds);
            float[] low;
            float[] high;
            RegionCloudSky.Build(spec, out low, out high);
            var pixels = new Color[low.Length];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = RegionWeatherMaker.CloudMapPixel(HighOnly ? 0f : low[i]);
            var map = new Texture2D(spec.Resolution, spec.Resolution, TextureFormat.RGBAHalf, false, true)
            {
                name = "Region Cloud Map", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave,
            };
            map.SetPixels(pixels);
            map.Apply(false, false);
            var highPixels = new Color[high.Length];
            for (int i = 0; i < highPixels.Length; i++) highPixels[i] = RegionWeatherMaker.HighCloudPixel(high[i]);
            var highMap = new Texture2D(spec.Resolution, spec.Resolution, TextureFormat.RGBA32, false, true)
            {
                name = "Region High Cloud Map", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave,
            };
            highMap.SetPixels(highPixels);
            highMap.Apply(false, false);

            float ground = look.Scene.HeightAt(look.Scene.CenterX, look.Scene.CenterZ);
            var boxObject = new GameObject("Region Cloud Box");
            boxObject.transform.position = new Vector3(spec.MinX + spec.Size * 0.5f, ground + (CloudBase + CloudTop) * 0.5f, spec.MinZ + spec.Size * 0.5f);
            var box = boxObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(spec.Size, CloudTop - CloudBase, spec.Size);
            RegionWeatherMaker.SetClouds(map, box, FindAsset<ScriptableObject>(CloudProfileName, "ScriptableObject"), ground + CloudBase, ground + CloudTop, CloudFadeMeters,
                                       highMap, ground + HighCloudHeight);
            Debug.Log("[RegionMap] 空 Weather Maker: 太陽 " + atmosphere.SunAltitudeDegrees + " 度（北西）、雲 2D の地図と同じ（seed " + spec.RegionSeed +
                      "、時刻 " + cloudTime + " 秒、" + (spec.Size / 1000f) + " km 四方、雲が見える割合 " + RegionClouds.Coverage(low, 0.1f).ToString("0.00") +
                      "、" + watch.ElapsedMilliseconds + " ms）");
        }

        // ---- 地形の霞の色を、Weather Maker の地平線の空に合わせる ----
        private const int HazeWidth = 256, HazeHeight = 128;
        private const float HazeFov = 20f, HazeLowDegrees = 0.5f, HazeHighDegrees = 3f;

        // 見回し中のカメラと地形を変えるのは手動描画の間だけ。描画前の向きへ同じ更新内で戻す。
        internal static (Color Away, Color Toward) ReadInteractiveHorizon(Camera camera, GameObject terrain, Vector3 eye, float sunYaw)
        {
            Vector3 position = camera.transform.position;
            Quaternion rotation = camera.transform.rotation;
            float fov = camera.fieldOfView;
            bool terrainActive = terrain.activeSelf;
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            bool post = RegionWeatherMaker.SetPostProcessing(camera, false);
            RenderTexture target = null;
            try
            {
                target = RenderTexture.GetTemporary(HazeWidth, HazeHeight, 24, RenderTextureFormat.ARGBHalf);
                terrain.SetActive(false);
                camera.targetTexture = target;
                camera.fieldOfView = HazeFov;
                camera.transform.SetPositionAndRotation(eye, Quaternion.Euler(0f, sunYaw + 180f, 0f));
                camera.Render();
                Color away = HorizonColor(target);
                camera.transform.rotation = Quaternion.Euler(0f, sunYaw, 0f);
                camera.Render();
                return (away, HorizonColor(target));
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.fieldOfView = fov;
                camera.transform.SetPositionAndRotation(position, rotation);
                RegionWeatherMaker.SetPostProcessing(camera, post);
                terrain.SetActive(terrainActive);
                RenderTexture.active = previousActive;
                if (target != null) RenderTexture.ReleaseTemporary(target);
            }
        }

        private static float HazeSunYaw(RegionAtmosphere atmosphere)
        {
            Vector3 toSun = -(RegionTerrainView.SunRotation(atmosphere.SunAltitudeDegrees) * Vector3.forward);
            return Mathf.Atan2(toSun.x, toSun.z) * Mathf.Rad2Deg;
        }

        private static bool ValidHorizon(Color color)
        {
            float brightness = color.grayscale;
            return brightness > 0f && !float.IsInfinity(brightness);
        }

        private static void SetHazeColors(RegionAtmosphere atmosphere, Color awaySample, Color towardSample)
        {
            // シェーダーへは線形で渡す。Gamma 色空間では読んだ値は画面の値（sRGB）なので線形に直す。
            // Linear 色空間では描画先（ARGBHalf）に線形の値のまま入るので、そのまま使う（直すと二重になり、霞が暗く色が転ぶ。2026-11-27 m28）
            bool gamma = QualitySettings.activeColorSpace == ColorSpace.Gamma;
            Color away = gamma ? awaySample.linear : awaySample, toward = gamma ? towardSample.linear : towardSample;
            Shader.SetGlobalVector("_RegionFogColor", new Vector4(away.r, away.g, away.b, 0f));
            Shader.SetGlobalVector("_RegionFogSunColor", new Vector4(toward.r, toward.g, toward.b, atmosphere.FogTowardSunSharpness));
            Debug.Log("[RegionMap] 霞の色を Weather Maker の地平線の空に合わせた（線形）: 太陽と反対 " + away + "、太陽の側 " + toward);
        }

        /// <summary>
        /// 地形を隠して、地平線のすぐ上の空（高さ 0.5〜3 度）を太陽の側と反対の側で撮り、自作の霞の 2 色（RegionAtmosphere.hlsl）にする手順を足す。
        /// 遠い台地が地平線の空と同じ色へ溶けるので、空と地形の間に色の段が出ない。霞の濃さと高さは光と空気の条件のまま。
        /// 雲が地平線の帯に入っても色が引っ張られないよう、明るさの順に並べた中ほどの半分の平均を使う。
        /// </summary>
        public static void AddHazeSteps(RegionEditorSteps steps, Func<Camera> camera, Func<RegionTerrainView.LookResult> look, RegionAtmosphere atmosphere)
        {
            float sunYaw = HazeSunYaw(atmosphere);
            var colors = new Color[2];
            RenderTexture target = null;
            bool post = true;
            float fov = 0f;
            steps.Add(() =>
            {
                post = RegionWeatherMaker.SetPostProcessing(camera(), false);
                fov = camera().fieldOfView;
                look().Root.SetActive(false);
                // 太陽の側の地平線は 1 を超えるので、HDR のまま読む（8 bit では切れて色が変わる）
                target = new RenderTexture(HazeWidth, HazeHeight, 24, RenderTextureFormat.ARGBHalf);
            }, 0);
            for (int s = 0; s < 2; s++)
            {
                int side = s;
                steps.Add(() =>
                {
                    Camera c = camera();
                    c.transform.position = Eye(look().Scene);
                    c.transform.rotation = Quaternion.Euler(0f, sunYaw + (side == 0 ? 180f : 0f), 0f);
                    c.fieldOfView = HazeFov;
                    c.targetTexture = target;
                    c.aspect = (float)HazeWidth / HazeHeight;
                }, 24);
                steps.Add(() =>
                {
                    camera().Render();
                    colors[side] = HorizonColor(target);
                }, 0);
            }
            steps.Add(() =>
            {
                Camera c = camera();
                c.targetTexture = null;
                c.ResetAspect();
                c.fieldOfView = fov;
                RegionWeatherMaker.SetPostProcessing(c, post);
                look().Root.SetActive(true);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                SetHazeColors(atmosphere, colors[0], colors[1]);
            });
        }

        private static Color HorizonColor(RenderTexture target)
        {
            var image = new Texture2D(HazeWidth, HazeHeight, TextureFormat.RGBAFloat, false);
            RenderTexture previous = RenderTexture.active;
            Color[] pixels;
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, HazeWidth, HazeHeight), 0, 0);
                pixels = image.GetPixels();
            }
            finally
            {
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);
            }

            // 水平に向けたカメラで、高さ e 度の行は tan(e) / tan(視野の半分) の位置（行 0 が下）
            float halfTan = Mathf.Tan(HazeFov * 0.5f * Mathf.Deg2Rad);
            int low = Mathf.FloorToInt((Mathf.Tan(HazeLowDegrees * Mathf.Deg2Rad) / halfTan + 1f) * 0.5f * HazeHeight);
            int high = Mathf.CeilToInt((Mathf.Tan(HazeHighDegrees * Mathf.Deg2Rad) / halfTan + 1f) * 0.5f * HazeHeight);
            var band = new List<Color>();
            for (int y = low; y <= high; y++)
                for (int x = 0; x < HazeWidth; x++) band.Add(pixels[x + y * HazeWidth]);
            band.Sort((a, b) => a.grayscale.CompareTo(b.grayscale));
            Color sum = Color.clear;
            int from = band.Count / 4, to = band.Count * 3 / 4;
            for (int i = from; i < to; i++) sum += band[i];
            Color mean = sum / Mathf.Max(to - from, 1);
            mean.a = 1f;
            return mean;
        }

        // ---- 2D の地図との見比べ ----
        /// <summary>
        /// 探索画面の雲の時刻（0〜420 秒の 10 秒ごと）のうち、場面の周り（見比べの範囲）で雲のある所が半分に近い時刻。
        /// 一面の雲や一面の晴れでは形を見比べられないので、見比べの絵はこの時刻で撮る。
        /// </summary>
        public static float MixedCloudTime(RegionLookScene scene)
        {
            float cover = new RegionRenderOptions().CloudCover;
            float best = 0f, bestScore = float.MaxValue;
            RegionCloudSkySpec spec = RegionCloudSky.ForScene(scene, CompareHalf * 2f, 48, cover, 0f);
            var sampler = new RegionCloudSky.Sampler(spec);
            for (float time = 0f; time < spec.CrossSeconds; time += 10f)
            {
                sampler.SetTime(time, spec.CrossSeconds);
                sampler.Build(spec.MinX, spec.MinZ, spec.Size, spec.Resolution, out float[] low, out _);
                float score = Mathf.Abs(RegionClouds.Coverage(low, 0.1f) - 0.5f);
                if (score < bestScore) { bestScore = score; best = time; }
            }
            return best;
        }


        // 地上から見上げた空を雲の高さの平面へ投影する範囲（中心から各辺まで、m）と、平面の高さ（地面から）。
        // 地上から見えるのは雲の底なので、平面は層の底に置く（底より上に置くと、遠い雲ほど外へずれて絵からはみ出す）
        private const float ComparePlane = CloudBase;
        // 高い薄雲（9 km）だけの見比べでは、地平線の近くに見える 50〜80 km 先まで入るよう広げる
        private static float CompareHalf => HighOnly ? 60000f : 15000f;

        /// <summary>
        /// 撮影の指定 -skyLayer high で、塊の雲を空にして高い薄雲だけを描き、見比べの絵を薄雲の高さ・2D の上の層で作る（薄雲の位置の確かめ）。
        /// </summary>
        internal static readonly bool HighOnly = RegionMapMenu.Argument("-skyLayer", "both") == "high";
        private const int FaceSize = 1024, CompareSize = 900;
        // 5 方向の絵は隣と重なるように 90 度より広く撮り、重なりの中で混ぜてつなぎ目を消す
        private const float FaceFov = 120f;
        private static readonly Vector3[] FaceForward = { Vector3.up, Vector3.forward, Vector3.right, Vector3.back, Vector3.left };

        /// <summary>
        /// 場面の中心の地面から見上げた空（上と東西南北の 5 方向）を、雲の高さの平面へ投影して sky-3d.png に、
        /// 同じ範囲の 2D の地図（探索画面と同じ雲の重ね方、場面の 6 km 四方の枠付き）を sky-2d.png に、
        /// Weather Maker へ渡した雲の量を sky-cover.png に書く手順を足す。どれも上が北。
        /// Weather Maker はカメラの向きに合わせた準備を毎コマの更新で行うので、向きを変えるたびにコマを回してから撮る。
        /// </summary>
        public static void AddComparisonSteps(RegionEditorSteps steps, Func<Camera> camera,
            Func<RegionTerrainView.LookResult> look, Func<float> cloudTime, string output, float lowCloudPlane = ComparePlane)
        {
            var pixels = new Color32[FaceForward.Length][];
            var matrices = new Matrix4x4[FaceForward.Length];
            RenderTexture target = null;
            bool post = true;
            float fov = 0f;
            steps.Add(() =>
            {
                // 空だけを写す。地形は隠し、後処理（周辺減光など）は切る（5 枚のつなぎ目に段が出ないように）
                post = RegionWeatherMaker.SetPostProcessing(camera(), false);
                fov = camera().fieldOfView;
                look().Root.SetActive(false);
                target = new RenderTexture(FaceSize, FaceSize, 24, RenderTextureFormat.ARGB32);
            }, 0);
            for (int f = 0; f < FaceForward.Length; f++)
            {
                int face = f;
                steps.Add(() =>
                {
                    Camera c = camera();
                    c.transform.position = Eye(look().Scene);
                    c.transform.rotation = Quaternion.LookRotation(FaceForward[face], face == 0 ? Vector3.forward : Vector3.up);
                    c.fieldOfView = FaceFov;
                    c.targetTexture = target;
                    c.aspect = 1f;
                }, 24);
                steps.Add(() =>
                {
                    Camera c = camera();
                    c.Render();
                    var image = new Texture2D(FaceSize, FaceSize, TextureFormat.RGBA32, false);
                    RenderTexture previous = RenderTexture.active;
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, FaceSize, FaceSize), 0, 0);
                    image.Apply();
                    RenderTexture.active = previous;
                    pixels[face] = image.GetPixels32();
                    matrices[face] = c.projectionMatrix * c.worldToCameraMatrix;
                    UnityEngine.Object.DestroyImmediate(image);
                }, 0);
            }
            steps.Add(() =>
            {
                Camera c = camera();
                c.targetTexture = null;
                c.ResetAspect();
                c.fieldOfView = fov;
                RegionWeatherMaker.SetPostProcessing(c, post);
                look().Root.SetActive(true);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                WriteComparison(look().Scene, pixels, matrices, cloudTime(), output, lowCloudPlane);
            });
        }

        private static Vector3 Eye(RegionLookScene scene)
        {
            return new Vector3(scene.CenterX, scene.HeightAt(scene.CenterX, scene.CenterZ) + 2f, -scene.CenterZ);
        }

        private static void WriteComparison(RegionLookScene scene, Color32[][] pixels, Matrix4x4[] matrices,
            float cloudTime, string output, float lowCloudPlane)
        {
            Vector3 eye = Eye(scene);
            float ground = eye.y - 2f;
            var sky = new Color32[CompareSize * CompareSize];
            for (int j = 0; j < CompareSize; j++)
            {
                for (int i = 0; i < CompareSize; i++)
                {
                    // 行 0 が南（画像は下の行から並ぶ）、列 0 が西
                    var point = new Vector3(eye.x + Offset(i), ground + (HighOnly ? HighCloudHeight : lowCloudPlane), eye.z + Offset(j));
                    // 写っている方向の絵を、枠の端から重なりの幅の中で 0 へ下がる重みで混ぜる
                    float overlap = 1f - Mathf.Tan(45f * Mathf.Deg2Rad) / Mathf.Tan(FaceFov * 0.5f * Mathf.Deg2Rad);
                    Color sum = Color.clear;
                    float weights = 0f;
                    for (int f = 0; f < FaceForward.Length; f++)
                    {
                        Vector4 clip = matrices[f] * new Vector4(point.x, point.y, point.z, 1f);
                        if (clip.w <= 0f) continue;
                        float nx = clip.x / clip.w, ny = clip.y / clip.w;
                        float weight = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny))) / overlap));
                        if (weight <= 0f) continue;
                        int x = Mathf.Clamp((int)((nx + 1f) * 0.5f * FaceSize), 0, FaceSize - 1);
                        int y = Mathf.Clamp((int)((ny + 1f) * 0.5f * FaceSize), 0, FaceSize - 1);
                        sum += (Color)pixels[f][x + y * FaceSize] * weight;
                        weights += weight;
                    }
                    Color color = weights > 0f ? sum / weights : Color.black;
                    color.a = 1f;
                    sky[i + j * CompareSize] = color;
                }
            }
            Save(sky, Path.Combine(output, "sky-3d.png"));

            // Weather Maker へ渡した雲の量（雲の地図の R）。白いほど雲が多い
            RegionCloudSkySpec spec = RegionCloudSky.ForScene(scene, CloudBoxSize, CloudMapSize, new RegionRenderOptions().CloudCover, cloudTime);
            var sampler = new RegionCloudSky.Sampler(spec);
            var cover = new Color32[CompareSize * CompareSize];
            for (int j = 0; j < CompareSize; j++)
            {
                for (int i = 0; i < CompareSize; i++)
                {
                    float low;
                    float high;
                    sampler.SampleAt(eye.x + Offset(i), eye.z + Offset(j), out low, out high);
                    float amount = HighOnly ? RegionWeatherMaker.HighCloudPixel(high).a : RegionWeatherMaker.CloudMapPixel(low).r;
                    byte value = (byte)(amount * 255f);
                    cover[i + j * CompareSize] = new Color32(value, value, value, 255);
                }
            }
            Save(cover, Path.Combine(output, "sky-cover.png"));
            Save(MapView(scene, cloudTime), Path.Combine(output, "sky-2d.png"));
        }

        private static float Offset(int index)
        {
            return ((index + 0.5f) / CompareSize * 2f - 1f) * CompareHalf;
        }

        /// <summary>探索画面（RegionExplorer・RegionCloudLayer）と同じ重ね方で、場面の周りの 2D の地図を描く。</summary>
        private static Color32[] MapView(RegionLookScene scene, float cloudTime)
        {
            RegionData region = RegionGenerator.Generate(new RegionSpec { Seed = scene.RegionSeed, Resolution = scene.RegionResolution });
            byte[] basePixels = RegionMapRenderer.Render(region, new RegionRenderOptions { CloudCover = 0f });
            int cloudSize = (region.Spec.Resolution + 1) / 2;
            float cover = new RegionRenderOptions().CloudCover;
            float[] low;
            float[] high;
            byte[] lowPixels, highPixels, shadowPixels, unused;
            RegionClouds.BuildLayerFields(cloudSize, region.Spec.Seed, cover, out low, out high);
            RegionClouds.RenderLayers(low, cloudSize, out lowPixels, out shadowPixels);
            RegionClouds.RenderLayers(high, cloudSize, out highPixels, out unused);
            var lowFlow = new RegionCloudFlow.Grid(RegionCloudFlow.Low, cloudTime, RegionCloudFlow.DefaultCrossSeconds);
            var highFlow = new RegionCloudFlow.Grid(RegionCloudFlow.High, cloudTime, RegionCloudFlow.DefaultCrossSeconds);

            float mpu = scene.MetersPerMapUnit;
            float centerMapX = scene.Outer.OriginMapX + scene.CenterX / mpu;
            float centerMapY = scene.Outer.OriginMapY + scene.CenterZ / mpu;
            float half = scene.Spec.OuterKm * 500f, edge = CompareHalf / CompareSize * 3f;
            bool highOnly = HighOnly;
            var colors = new Color32[CompareSize * CompareSize];
            for (int j = 0; j < CompareSize; j++)
            {
                for (int i = 0; i < CompareSize; i++)
                {
                    float east = Offset(i), north = Offset(j);
                    float mapX = centerMapX + east / mpu, mapY = centerMapY - north / mpu;
                    float u = mapX / RegionData.MapUnits, v = 1f - mapY / RegionData.MapUnits;
                    Color c = Read(basePixels, region.Resolution, u, 1f - v);
                    if (!highOnly)
                    {
                        c = Over(c, Layer(shadowPixels, cloudSize, lowFlow, u, v, RegionClouds.ShadowOffset, -RegionClouds.ShadowOffset));
                        c = Over(c, Layer(lowPixels, cloudSize, lowFlow, u, v, 0f, 0f));
                    }
                    c = Over(c, Layer(highPixels, cloudSize, highFlow, u, v, 0f, 0f));
                    bool frame = (Mathf.Abs(Mathf.Abs(east) - half) < edge && Mathf.Abs(north) < half) ||
                                 (Mathf.Abs(Mathf.Abs(north) - half) < edge && Mathf.Abs(east) < half);
                    colors[i + j * CompareSize] = frame ? new Color(1f, 0.85f, 0.2f) : c;
                }
            }
            return colors;
        }

        // 探索画面の雲の層と同じ読み方（地図の位置 (u, v)、v は北が正。画像は北西の角から行ごと。shift は影のずれ）
        private static Color Layer(byte[] pixels, int size, RegionCloudFlow.Grid flow, float u, float v, float shiftU, float shiftV)
        {
            var offset = flow.At(u, v);
            Color c = Read(pixels, size, u - offset.Du - shiftU, 1f - (v - offset.Dv - shiftV));
            c.a *= offset.Fade;
            return c;
        }

        // 北西の角から行ごとの RGBA32 を、(u, 南へ増える v) で双線形に読む（繰り返し）
        private static Color Read(byte[] pixels, int size, float u, float vSouth)
        {
            float fx = (u - Mathf.Floor(u)) * size - 0.5f;
            float fy = (vSouth - Mathf.Floor(vSouth)) * size - 0.5f;
            int x0 = Mathf.FloorToInt(fx), y0 = Mathf.FloorToInt(fy);
            float tx = fx - x0, ty = fy - y0;
            Color a = Pixel(pixels, size, x0, y0), b = Pixel(pixels, size, x0 + 1, y0);
            Color c = Pixel(pixels, size, x0, y0 + 1), d = Pixel(pixels, size, x0 + 1, y0 + 1);
            return Color.Lerp(Color.Lerp(a, b, tx), Color.Lerp(c, d, tx), ty);
        }

        private static Color Pixel(byte[] pixels, int size, int x, int y)
        {
            x = ((x % size) + size) % size;
            y = ((y % size) + size) % size;
            int p = (x + y * size) * 4;
            return new Color32(pixels[p], pixels[p + 1], pixels[p + 2], pixels[p + 3]);
        }

        private static Color Over(Color under, Color over)
        {
            return new Color(Mathf.Lerp(under.r, over.r, over.a), Mathf.Lerp(under.g, over.g, over.a), Mathf.Lerp(under.b, over.b, over.a), 1f);
        }

        private static void Save(Color32[] colors, string path)
        {
            var image = new Texture2D(CompareSize, CompareSize, TextureFormat.RGBA32, false);
            image.SetPixels32(colors);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            Debug.Log("[RegionMap] 空の見比べ → " + path);
        }

        private static T FindAsset<T>(string name, string typeFilter) where T : UnityEngine.Object
        {
            foreach (string guid in AssetDatabase.FindAssets(name + " t:" + typeFilter))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == name) return AssetDatabase.LoadAssetAtPath<T>(path);
            }
            return null;
        }
    }

    /// <summary>
    /// 編集時の更新（ExecuteInEditMode の Update）を 1 コマずつ回しながら、決めた手順を順に行う。
    /// Weather Maker は時刻や雲を変えると数百コマかけて落ち着くので、撮影はこの上で進める。
    /// </summary>
    public sealed class RegionEditorSteps
    {
        private readonly Queue<KeyValuePair<Action, int>> steps = new Queue<KeyValuePair<Action, int>>();
        private int wait;
        private double start;
        private double timeout;
        private Action<Exception> finished;

        /// <summary>手順を足す。waitAfter はこの手順の後に回すコマ数。</summary>
        public void Add(Action action, int waitAfter = 12)
        {
            steps.Enqueue(new KeyValuePair<Action, int>(action, waitAfter));
        }

        /// <summary>手順を始める。終わると finished(null)、失敗か時間切れで finished(例外)。</summary>
        public void Run(double timeoutSeconds, Action<Exception> onFinished)
        {
            timeout = timeoutSeconds;
            finished = onFinished;
            start = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        private void Tick()
        {
            try
            {
                EditorApplication.QueuePlayerLoopUpdate();
                if (EditorApplication.timeSinceStartup - start > timeout) throw new TimeoutException("撮影が " + timeout + " 秒を超えた");
                if (wait-- > 0) return;
                if (steps.Count == 0)
                {
                    EditorApplication.update -= Tick;
                    finished(null);
                    return;
                }
                KeyValuePair<Action, int> step = steps.Dequeue();
                step.Key();
                wait = step.Value;
            }
            catch (Exception exception)
            {
                EditorApplication.update -= Tick;
                finished(exception);
            }
        }
    }
}
