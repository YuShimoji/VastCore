using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vastcore.RegionMap.Editor
{
    /// <summary>
    /// 鳥瞰図の書き出しと探索シーンの入口。メニュー「Vastcore/Region Map」と、
    /// コマンドライン（-executeMethod）の両方から使う。
    /// </summary>
    public static class RegionMapMenu
    {
        public const string ExplorerScenePath = "Assets/Scenes/RegionExplorer.unity";

        [MenuItem("Vastcore/Region Map/Open Explorer Scene")]
        public static void OpenExplorerScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureExplorerScene();
            EditorSceneManager.OpenScene(ExplorerScenePath);
        }

        [MenuItem("Vastcore/Region Map/Export Bird's-Eye PNG...")]
        public static void ExportPngWithDialog()
        {
            var spec = new RegionSpec();
            string path = EditorUtility.SaveFilePanel("鳥瞰図を書き出す", "", "region-seed" + spec.Seed + ".png", "png");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                EditorUtility.DisplayProgressBar("Region Map", "地域を生成しています…", 0.3f);
                Export(spec, new RegionRenderOptions(), path);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            EditorUtility.RevealInFinder(path);
        }

        /// <summary>
        /// コマンドライン用。指定: -regionSeed 7 -regionRes 1024 -regionStyle satellite -regionOut 出力先.png
        /// </summary>
        public static void ExportPngBatch()
        {
            var spec = new RegionSpec();
            var options = new RegionRenderOptions();
            spec.Seed = int.Parse(Argument("-regionSeed", spec.Seed.ToString()));
            spec.Resolution = int.Parse(Argument("-regionRes", spec.Resolution.ToString()));
            options.Style = (RegionMapStyle)Enum.Parse(typeof(RegionMapStyle), Argument("-regionStyle", options.Style.ToString()), true);
            string path = Argument("-regionOut", "artifacts/region-map/unity-region-seed" + spec.Seed + ".png");
            RegionData data = Export(spec, options, path);
            Debug.Log(string.Format("[RegionMap] 書き出し完了 seed={0} res={1} 陸={2:P1} 最高={3:F0}m 地点={4} → {5}",
                spec.Seed, spec.Resolution, data.LandFraction, data.MaxHeight, data.Sites.Count, Path.GetFullPath(path)));
        }

        /// <summary>コマンドライン用。探索シーンが無ければ作る。</summary>
        public static void CreateExplorerSceneBatch()
        {
            bool created = EnsureExplorerScene();
            Debug.Log("[RegionMap] 探索シーン " + ExplorerScenePath + (created ? " を作成" : " は既にある"));
        }

        /// <summary>コマンドライン用。探索シーンの用意と鳥瞰図の書き出しを続けて行う。</summary>
        public static void SetupBatch()
        {
            CreateExplorerSceneBatch();
            ExportPngBatch();
        }

        public static RegionData Export(RegionSpec spec, RegionRenderOptions options, string path)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            RegionData data = RegionGenerator.Generate(spec);
            long generateMs = watch.ElapsedMilliseconds;
            watch.Restart();
            byte[] pixels = RegionMapRenderer.Render(data, options);
            long renderMs = watch.ElapsedMilliseconds;
            Texture2D texture = RegionMapTexture.Create(pixels, data.Resolution);
            try
            {
                string directory = Path.GetDirectoryName(Path.GetFullPath(path));
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
            Debug.Log(string.Format("[RegionMap] 生成 {0}ms / 描画 {1}ms", generateMs, renderMs));
            return data;
        }

        /// <summary>探索シーンを作る。既にあれば何もしない。作ったら true。</summary>
        public static bool EnsureExplorerScene()
        {
            if (File.Exists(ExplorerScenePath)) return false;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(10, 13, 18, 255);
            camera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var explorer = new GameObject("Region Explorer");
            explorer.AddComponent<RegionExplorer>();

            EditorSceneManager.SaveScene(scene, ExplorerScenePath);
            AssetDatabase.Refresh();
            return true;
        }

        internal static string Argument(string name, string fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int a = 0; a < args.Length - 1; a++)
            {
                if (args[a] == name) return args[a + 1];
            }
            return fallback;
        }
    }
}
