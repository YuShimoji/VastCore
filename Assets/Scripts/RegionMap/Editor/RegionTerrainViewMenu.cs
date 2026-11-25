using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap.Editor
{
    /// <summary>
    /// 崖と洞窟の 3D 表示（3D 化の試作）の入口。メニュー「Vastcore/Region Map」から開くと、新しいシーンに地形を並べ、
    /// Scene ビューを崖の正面へ向ける（シーンは保存しない前提）。コマンドライン（-executeMethod）からは、決まった 3 つの視点を画像に残す。
    /// </summary>
    public static class RegionTerrainViewMenu
    {
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

        /// <summary>新しいシーンに、地形・太陽・カメラを置く。</summary>
        public static RegionTerrainView.Result CreateScene(RegionSpec regionSpec, RegionTerrainViewSpec viewSpec, out Camera camera)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RegionData region = RegionGenerator.Generate(regionSpec);
            RegionTerrainView.Result result = RegionTerrainView.Build(region, viewSpec);

            var sun = new GameObject("Sun");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.97f, 0.92f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = RegionTerrainView.SunRotation();

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.62f, 0.72f);
            RenderSettings.ambientEquatorColor = new Color(0.42f, 0.44f, 0.46f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.21f, 0.19f);
            RenderSettings.fog = false;

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = 5000f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.55f, 0.68f, 0.86f);
            return result;
        }

        /// <summary>
        /// コマンドライン用。崖の正面・近く・引いた所の 3 枚を -regionOut のフォルダへ書き出して終了する。
        /// 指定: -regionSeed 9 -regionOut 出力先のフォルダ（画面を使うので -nographics は付けない）
        /// </summary>
        public static void CaptureFromCommandLine()
        {
            int exitCode = 0;
            try
            {
                string output = Path.GetFullPath(RegionMapMenu.Argument("-regionOut", "artifacts/region-map/3d-unity"));
                Directory.CreateDirectory(output);
                // 最初の 1 枚が、コンパイル待ちの仮の色で写らないようにする
                ShaderUtil.allowAsyncCompilation = false;
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

        private static string Describe(RegionTerrainView.Result result)
        {
            string shader = RegionTerrainView.ShaderResource + (Resources.Load<Shader>(RegionTerrainView.ShaderResource) != null ? "" : "（見つからず、URP の Lit で代用）");
            if (result.Patch == null) return "体積で作る崖が見つからなかった（地形だけを並べた）。シェーダー " + shader;
            return result.Volume.Note + " / 体積の頂点 " + result.Patch.VertexCount + " 三角形 " + result.Patch.TriangleCount
                + " / シェーダー " + shader;
        }

        private static void Shot(Camera camera, RegionTerrainView.Result result, string output, string name, float yawOffset, float pitch, float distance)
        {
            const int Width = 1600;
            const int Height = 900;
            RegionTerrainView.Place(camera.transform, result, yawOffset, pitch, distance);
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                image.Apply();
                RenderTexture.active = previous;
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
