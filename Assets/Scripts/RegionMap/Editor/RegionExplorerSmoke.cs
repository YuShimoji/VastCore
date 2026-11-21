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
        private static double _readyAt;
        // 連続撮影（-regionClip 秒数）。開始直後の画面を 1 秒に ClipFps 枚ずつ clip/ へ残す
        private const string ClipKey = "Vastcore.RegionMap.ExplorerSmoke.Clip";
        private const double ClipFps = 10.0;
        private static double _clipUntil;
        private static double _clipNext;
        private static int _clipFrame;
        private static Vector2 _driftAtReady;

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
            SessionState.SetFloat(ClipKey, float.Parse(RegionMapMenu.Argument("-regionClip", "0"), System.Globalization.CultureInfo.InvariantCulture));
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

            if (_readyAt <= 0.0)
            {
                _readyAt = EditorApplication.timeSinceStartup;
                _driftAtReady = explorer.CloudDrift;
                Debug.Log(string.Format("[RegionMap] 再生を始めてから地図が出るまで {0:0.0} 秒", _readyAt - _startedAt));
            }

            if (_wait > 0)
            {
                _wait--;
                return;
            }

            if (_clipUntil > 0.0)
            {
                double now = EditorApplication.timeSinceStartup;
                if (now >= _clipNext)
                {
                    _clipFrame++;
                    Capture(output, string.Format("clip/f-{0:0000}.png", _clipFrame));
                    _clipNext += 1.0 / ClipFps;
                }
                if (now < _clipUntil) return;
                _clipUntil = 0.0;
                Debug.Log("[RegionMap] 連続撮影 " + _clipFrame + " 枚");
            }

            RegionSite target = explorer.Region.Sites.Count > 0 ? explorer.Region.Sites[0] : null;
            switch (step)
            {
                case 0:
                    Capture(output, "explorer-1-start.png");
                    float clipSeconds = SessionState.GetFloat(ClipKey, 0f);
                    if (clipSeconds > 0f)
                    {
                        Directory.CreateDirectory(Path.Combine(output, "clip"));
                        _clipFrame = 0;
                        _clipNext = EditorApplication.timeSinceStartup + 0.5;
                        _clipUntil = _clipNext + clipSeconds;
                    }
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
                    explorer.ShowWholeRegion();
                    break;
                case 4:
                    Capture(output, "explorer-6-whole.png");
                    break;
                case 5:
                    // 地名を「名前と境界」にして、全体表示のまま撮る
                    explorer.NextAreaDisplay();
                    break;
                case 6:
                    Capture(output, "explorer-7-areas.png");
                    break;
                case 7:
                    // 地名を「名前」へ戻す（名前と境界 → なし → 名前）
                    explorer.NextAreaDisplay();
                    explorer.NextAreaDisplay();
                    explorer.ShowFullWidth();
                    explorer.NextCloudOpacity();
                    break;
                case 8:
                    Capture(output, "explorer-4-clouds-thin.png");
                    break;
                case 9:
                    explorer.NextStyle();
                    break;
                case 10:
                    Capture(output, "explorer-5-atlas.png");
                    break;
                default:
                    Vector2 drift = explorer.CloudDrift - _driftAtReady;
                    Finish(output, string.Format("PASS: 発見 {0} / {1}、サーチ {2} 回、画面 {3}x{4}、地図が出るまで {5:0.0} 秒、雲は {6:0.0} 秒で東へ {7:0.0000}・南へ {8:0.0000}（地図の幅を 1 として）",
                        explorer.Session.Discovered.Count, explorer.Session.TotalSites, explorer.Session.SearchCount, Screen.width, Screen.height,
                        _readyAt - _startedAt, EditorApplication.timeSinceStartup - _readyAt, drift.x, -drift.y), 0);
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
            SessionState.EraseFloat(ClipKey);
            File.WriteAllText(Path.Combine(output, "explorer-smoke-result.txt"), result);
            Debug.Log("[RegionMap] " + result);
            EditorApplication.Exit(exitCode);
        }
    }
}
