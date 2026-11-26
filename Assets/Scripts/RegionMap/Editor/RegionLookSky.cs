using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

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

        private const string PrefabName = "WeatherMakerPrefab";
        private const string SkyProfileName = "WeatherMakerSkyProfile_ProceduralMilkyWayPhysicallyBased";
        private const string CloudProfileName = "WeatherMakerCloudProfile_CloudBox";

        /// <summary>
        /// Weather Maker を入れた直後に 1 回行う、URP で描くための切り替え（Weather Maker の手順のうち、シェーダーの GrabPass の置き換えと UNITY_URP の定義）。
        /// 描画パイプラインの資産は差し替えない（Weather Maker の既定の手順は自分の資産へ差し替える）。描画の機能は撮影の時にメモリの中だけで足す。
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

        /// <summary>
        /// 場面の自作の空・霞・太陽を外し、Weather Maker を置く。cloudTime は探索画面の雲の時刻（秒）。
        /// 置いた後、空と太陽が落ち着くまで編集時の更新を数百コマ回してから撮る（RegionEditorSteps）。
        /// </summary>
        public static void Use(Camera camera, RegionTerrainView.LookResult look, RegionAtmosphere atmosphere, float cloudTime)
        {
            RegionWeatherMaker.AddRenderFeature();

            // 自作の霞と空を切る。空は Weather Maker の空の球が描くので、カメラは黒で消す（Weather Maker の手順）
            Shader.SetGlobalVector("_RegionFogParams", Vector4.zero);
            RenderSettings.skybox = null;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            // 自作の太陽を外し、Weather Maker の太陽を主光源にする
            GameObject ourSun = GameObject.Find("Sun");
            if (ourSun != null) UnityEngine.Object.DestroyImmediate(ourSun);

            PrefabUtility.InstantiatePrefab(FindAsset<GameObject>(PrefabName, "Prefab"));
            RegionWeatherMaker.AllowCamera(camera);
            RegionWeatherMaker.UseFullQualityClouds();
            RegionWeatherMaker.SetSky(FindAsset<ScriptableObject>(SkyProfileName, "ScriptableObject"));

            Vector3 toSun = -(RegionTerrainView.SunRotation(atmosphere.SunAltitudeDegrees) * Vector3.forward);
            RegionWeatherMaker.SetSun(atmosphere.SunAltitudeDegrees, Mathf.Atan2(toSun.x, toSun.z) * Mathf.Rad2Deg);

            var watch = System.Diagnostics.Stopwatch.StartNew();
            float cover = new RegionRenderOptions().CloudCover;
            RegionCloudSkySpec spec = RegionCloudSky.ForScene(look.Scene, CloudBoxSize, CloudMapSize, cover, cloudTime);
            float[] low;
            float[] high;
            RegionCloudSky.Build(spec, out low, out high);
            var pixels = new Color[low.Length];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = RegionWeatherMaker.CloudMapPixel(low[i]);
            var map = new Texture2D(spec.Resolution, spec.Resolution, TextureFormat.RGBAHalf, false, true)
            {
                name = "Region Cloud Map", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave,
            };
            map.SetPixels(pixels);
            map.Apply(false, false);

            float ground = look.Scene.HeightAt(look.Scene.CenterX, look.Scene.CenterZ);
            var boxObject = new GameObject("Region Cloud Box");
            boxObject.transform.position = new Vector3(spec.MinX + spec.Size * 0.5f, ground + (CloudBase + CloudTop) * 0.5f, spec.MinZ + spec.Size * 0.5f);
            var box = boxObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(spec.Size, CloudTop - CloudBase, spec.Size);
            RegionWeatherMaker.SetClouds(map, box, FindAsset<ScriptableObject>(CloudProfileName, "ScriptableObject"));
            Debug.Log("[RegionMap] 空 Weather Maker: 太陽 " + atmosphere.SunAltitudeDegrees + " 度（北西）、雲 2D の地図と同じ（seed " + spec.RegionSeed +
                      "、時刻 " + cloudTime + " 秒、" + (spec.Size / 1000f) + " km 四方、雲が見える割合 " + RegionClouds.Coverage(low, 0.1f).ToString("0.00") +
                      "、" + watch.ElapsedMilliseconds + " ms）");
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
            for (float time = 0f; time < 420f; time += 10f)
            {
                RegionCloudSkySpec spec = RegionCloudSky.ForScene(scene, CompareHalf * 2f, 48, cover, time);
                float[] low;
                float[] high;
                RegionCloudSky.Build(spec, out low, out high);
                float score = Mathf.Abs(RegionClouds.Coverage(low, 0.1f) - 0.5f);
                if (score < bestScore) { bestScore = score; best = time; }
            }
            return best;
        }


        // 地上から見上げた空を雲の高さの平面へ投影する範囲（中心から各辺まで、m）と、平面の高さ（地面から）
        private const float CompareHalf = 15000f, ComparePlane = 2500f;
        private const int FaceSize = 1024, CompareSize = 900;
        private static readonly Vector3[] FaceForward = { Vector3.up, Vector3.forward, Vector3.right, Vector3.back, Vector3.left };

        /// <summary>
        /// 場面の中心の地面から見上げた空（上と東西南北の 5 方向）を、雲の高さの平面へ投影して sky-3d.png に、
        /// 同じ範囲の 2D の地図（探索画面と同じ雲の重ね方、場面の 6 km 四方の枠付き）を sky-2d.png に書く。どちらも上が北。
        /// </summary>
        public static void WriteComparison(Camera camera, RegionTerrainView.LookResult look, float cloudTime, string output)
        {
            RegionLookScene scene = look.Scene;
            float ground = scene.HeightAt(scene.CenterX, scene.CenterZ);
            var eye = new Vector3(scene.CenterX, ground + 2f, -scene.CenterZ);
            var pixels = new Color32[FaceForward.Length][];
            var matrices = new Matrix4x4[FaceForward.Length];
            var target = new RenderTexture(FaceSize, FaceSize, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(FaceSize, FaceSize, TextureFormat.RGBA32, false);
            float far = camera.farClipPlane, fov = camera.fieldOfView;
            // 空だけを写す。地形は隠し、後処理（周辺減光など）は切る（5 枚のつなぎ目に段が出ないように）
            bool post = RegionWeatherMaker.SetPostProcessing(camera, false);
            look.Root.SetActive(false);
            try
            {
                camera.targetTexture = target;
                camera.aspect = 1f;
                camera.fieldOfView = 90f;
                camera.transform.position = eye;
                for (int f = 0; f < FaceForward.Length; f++)
                {
                    camera.transform.rotation = Quaternion.LookRotation(FaceForward[f], f == 0 ? Vector3.forward : Vector3.up);
                    for (int i = 0; i < 3; i++) camera.Render();
                    RenderTexture previous = RenderTexture.active;
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, FaceSize, FaceSize), 0, 0);
                    image.Apply();
                    RenderTexture.active = previous;
                    pixels[f] = image.GetPixels32();
                    matrices[f] = camera.projectionMatrix * camera.worldToCameraMatrix;
                }
            }
            finally
            {
                camera.targetTexture = null;
                camera.ResetAspect();
                look.Root.SetActive(true);
                RegionWeatherMaker.SetPostProcessing(camera, post);
                camera.fieldOfView = fov;
                camera.farClipPlane = far;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(image);
            }

            var sky = new Color32[CompareSize * CompareSize];
            for (int j = 0; j < CompareSize; j++)
            {
                for (int i = 0; i < CompareSize; i++)
                {
                    // 行 0 が南（画像は下の行から並ぶ）、列 0 が西
                    var point = new Vector3(eye.x + Offset(i), ground + ComparePlane, eye.z + Offset(j));
                    Vector3 direction = (point - eye).normalized;
                    int best = 0;
                    for (int f = 1; f < FaceForward.Length; f++)
                        if (Vector3.Dot(direction, FaceForward[f]) > Vector3.Dot(direction, FaceForward[best])) best = f;
                    Vector4 clip = matrices[best] * new Vector4(point.x, point.y, point.z, 1f);
                    int x = Mathf.Clamp((int)((clip.x / clip.w + 1f) * 0.5f * FaceSize), 0, FaceSize - 1);
                    int y = Mathf.Clamp((int)((clip.y / clip.w + 1f) * 0.5f * FaceSize), 0, FaceSize - 1);
                    sky[i + j * CompareSize] = pixels[best][x + y * FaceSize];
                }
            }
            Save(sky, Path.Combine(output, "sky-3d.png"));

            // Weather Maker へ渡した雲の量（雲の地図の R）。白いほど雲が多い
            RegionCloudSkySpec spec = RegionCloudSky.ForScene(scene, CloudBoxSize, CloudMapSize, new RegionRenderOptions().CloudCover, cloudTime);
            var cover = new Color32[CompareSize * CompareSize];
            for (int j = 0; j < CompareSize; j++)
            {
                for (int i = 0; i < CompareSize; i++)
                {
                    float low;
                    float high;
                    RegionCloudSky.SampleAt(spec, eye.x + Offset(i), eye.z + Offset(j), out low, out high);
                    byte value = (byte)(RegionWeatherMaker.CloudMapPixel(low).r * 255f);
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

            float mpu = scene.MetersPerMapUnit;
            float centerMapX = scene.Outer.OriginMapX + scene.CenterX / mpu;
            float centerMapY = scene.Outer.OriginMapY + scene.CenterZ / mpu;
            float half = scene.Spec.OuterKm * 500f, edge = CompareHalf / CompareSize * 3f;
            var colors = new Color32[CompareSize * CompareSize];
            for (int j = 0; j < CompareSize; j++)
            {
                for (int i = 0; i < CompareSize; i++)
                {
                    float east = Offset(i), north = Offset(j);
                    float mapX = centerMapX + east / mpu, mapY = centerMapY - north / mpu;
                    float u = mapX / RegionData.MapUnits, v = 1f - mapY / RegionData.MapUnits;
                    Color c = Read(basePixels, region.Resolution, u, 1f - v);
                    c = Over(c, Layer(shadowPixels, cloudSize, RegionCloudFlow.Low, u, v, cloudTime, RegionClouds.ShadowOffset, -RegionClouds.ShadowOffset));
                    c = Over(c, Layer(lowPixels, cloudSize, RegionCloudFlow.Low, u, v, cloudTime, 0f, 0f));
                    c = Over(c, Layer(highPixels, cloudSize, RegionCloudFlow.High, u, v, cloudTime, 0f, 0f));
                    bool frame = (Mathf.Abs(Mathf.Abs(east) - half) < edge && Mathf.Abs(north) < half) ||
                                 (Mathf.Abs(Mathf.Abs(north) - half) < edge && Mathf.Abs(east) < half);
                    colors[i + j * CompareSize] = frame ? new Color(1f, 0.85f, 0.2f) : c;
                }
            }
            return colors;
        }

        // 探索画面の雲の層と同じ読み方（地図の位置 (u, v)、v は北が正。画像は北西の角から行ごと。shift は影のずれ）
        private static Color Layer(byte[] pixels, int size, int layer, float u, float v, float time, float shiftU, float shiftV)
        {
            float du, dv, fade;
            RegionCloudFlow.Sample(layer, u, v, time, 420f, out du, out dv, out fade);
            Color c = Read(pixels, size, u - du - shiftU, 1f - (v - dv - shiftV));
            c.a *= fade;
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
