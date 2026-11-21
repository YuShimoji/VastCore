using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vastcore.RegionMap.Editor
{
    /// <summary>
    /// 探索シーンを通常の Editor で再生し、決まった操作をして Game ビューを画像に残す確認用の入口。
    /// 起動例: Unity.exe -projectPath . -executeMethod Vastcore.RegionMap.Editor.RegionExplorerSmoke.RunFromCommandLine -regionOut artifacts/region-map
    /// 再生開始でスクリプトが読み込み直されるので、進み具合は SessionState に持つ。
    /// </summary>
    [InitializeOnLoad]
    public static class RegionExplorerSmoke
    {
        private const string OutputKey = "Vastcore.RegionMap.Smoke.Output";
        private const string StepKey = "Vastcore.RegionMap.Smoke.Step";
        private const int FramesBetweenSteps = 45;
        private const double TimeoutSeconds = 240.0;

        private static int _wait;
        private static double _startedAt;

        static RegionExplorerSmoke()
        {
            if (!string.IsNullOrEmpty(SessionState.GetString(OutputKey, ""))) EditorApplication.update += Tick;
        }

        public static void RunFromCommandLine()
        {
            string output = Path.GetFullPath(RegionMapMenu.Argument("-regionOut", "artifacts/region-map"));
            Directory.CreateDirectory(output);
            RegionMapMenu.EnsureExplorerScene();
            EditorSceneManager.OpenScene(RegionMapMenu.ExplorerScenePath);
            SessionState.SetString(OutputKey, output);
            SessionState.SetInt(StepKey, 0);
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            // Game ビューの枠の大きさに関係なく、基準の画面サイズで描かせて撮る
            PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "RegionExplorer 1920x1080");
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        /// <summary>
        /// 探索シーンを開き、Game ビューを広げて再生を始める。自動の操作も終了もしない（人が触って確かめる時の入口）。
        /// 起動例: Unity.exe -projectPath . -executeMethod Vastcore.RegionMap.Editor.RegionExplorerSmoke.PlayFromCommandLine
        /// </summary>
        public static void PlayFromCommandLine()
        {
            RegionMapMenu.EnsureExplorerScene();
            EditorSceneManager.OpenScene(RegionMapMenu.ExplorerScenePath);
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            System.Type gameView = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameView != null) EditorWindow.GetWindow(gameView).maximized = true;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            if (_startedAt <= 0.0) _startedAt = EditorApplication.timeSinceStartup;
            string output = SessionState.GetString(OutputKey, "");
            int step = SessionState.GetInt(StepKey, 0);

            RegionExplorer explorer = Object.FindFirstObjectByType<RegionExplorer>();
            if (explorer == null || !explorer.IsReady)
            {
                if (EditorApplication.timeSinceStartup - _startedAt > TimeoutSeconds) Finish(output, "FAIL: 地域の生成が時間内に終わらなかった", 1);
                return;
            }

            if (_wait > 0)
            {
                _wait--;
                return;
            }

            RegionSite target = explorer.Region.Sites.Count > 0 ? explorer.Region.Sites[0] : null;
            switch (step)
            {
                case 0:
                    Capture(output, "explorer-1-start.png");
                    break;
                case 1:
                    if (target != null)
                    {
                        explorer.MoveCursor(target.MapX + 6f, target.MapY);
                        SearchResult near = explorer.SearchAtCursor();
                        Debug.Log("[RegionMap] 6 離れた位置のサーチ: " + near.Signal);
                    }
                    Capture(output, "explorer-2-signal.png");
                    break;
                case 2:
                    if (target != null)
                    {
                        explorer.MoveCursor(target.MapX, target.MapY);
                        SearchResult found = explorer.SearchAtCursor();
                        Debug.Log("[RegionMap] 地点の位置のサーチ: " + found.Signal + " / " + (found.Site != null ? found.Site.Name : "なし"));
                    }
                    Capture(output, "explorer-3-found.png");
                    break;
                case 3:
                    explorer.NextStyle();
                    break;
                case 4:
                    Capture(output, "explorer-4-atlas.png");
                    break;
                default:
                    Finish(output, string.Format("PASS: 発見 {0} / {1}、サーチ {2} 回、画面 {3}x{4}",
                        explorer.Session.Discovered.Count, explorer.Session.TotalSites, explorer.Session.SearchCount, Screen.width, Screen.height), 0);
                    return;
            }
            SessionState.SetInt(StepKey, step + 1);
            _wait = FramesBetweenSteps;
        }

        private static void Capture(string output, string name)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(output, name));
        }

        private static void Finish(string output, string result, int exitCode)
        {
            EditorApplication.update -= Tick;
            SessionState.EraseString(OutputKey);
            SessionState.EraseInt(StepKey);
            File.WriteAllText(Path.Combine(output, "explorer-smoke-result.txt"), result);
            Debug.Log("[RegionMap] " + result);
            EditorApplication.Exit(exitCode);
        }
    }
}
