using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vastcore.RegionMap.Editor
{
    /// <summary>
    /// 探索画面の動き（SP-021 軸 E）を、決まった台本で動かして 1/30 秒ごとに Game ビューを画像に残す確認用の入口。
    /// 描くのが遅れても動きを飛ばさないよう、ゲームの時間（Time.captureDeltaTime）と動きの時計（MotionClockStep）を 1 フレーム 1/30 秒に固定する。
    /// 起動例: Unity.exe -projectPath . -executeMethod Vastcore.RegionMap.Editor.RegionExplorerMotionCapture.RunFromCommandLine
    ///         -regionOut D:/out/motion -regionMotionSet full|bonly|off
    /// full＝案 B と C の 3 要素、bonly＝案 B だけ、off＝動きなし。画像は &lt;out&gt;/&lt;set&gt;/f-0000.png から順に出る。
    /// </summary>
    [InitializeOnLoad]
    public static class RegionExplorerMotionCapture
    {
        private const string OutputKey = "Vastcore.RegionMap.MotionCapture.Output";
        private const string SetKey = "Vastcore.RegionMap.MotionCapture.Set";
        private const float Fps = 30f;
        // 始めの知らせ（地域ができた時の 8 秒、規則を替えた時の 4 秒）が消えるまで、撮らずに進めるフレーム数
        private const int WarmFrames = 300;
        private const double TimeoutSeconds = 300.0;

        private sealed class Cue
        {
            public float At;
            public string Label;
            public System.Action<RegionExplorer> Run;
        }

        private static readonly List<Cue> Cues = new List<Cue>();
        private static readonly StringBuilder Log = new StringBuilder();
        private static double _startedAt;
        private static int _setupFrame = -1;
        private static int _lastFrame = -1;
        private static int _lastIndex = -1;
        private static int _next;
        private static int _dropped;
        private static float _end;
        private static RegionSite _clueSite;

        static RegionExplorerMotionCapture()
        {
            if (!string.IsNullOrEmpty(SessionState.GetString(OutputKey, ""))) EditorApplication.update += Tick;
        }

        public static void RunFromCommandLine()
        {
            string output = Path.GetFullPath(RegionMapMenu.Argument("-regionOut", "artifacts/region-map/motion"));
            string set = RegionMapMenu.Argument("-regionMotionSet", "full");
            Directory.CreateDirectory(Path.Combine(output, set));
            RegionMapMenu.EnsureExplorerScene();
            EditorSceneManager.OpenScene(RegionMapMenu.ExplorerScenePath);
            SessionState.SetString(OutputKey, output);
            SessionState.SetString(SetKey, set);
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            PlayModeWindow.SetCustomRenderingResolution(1920, 1080, "RegionExplorer 1920x1080");
            EditorApplication.update += Tick;
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!EditorApplication.isPlaying) return;
            if (_startedAt <= 0.0) _startedAt = EditorApplication.timeSinceStartup;
            string output = SessionState.GetString(OutputKey, "");
            string set = SessionState.GetString(SetKey, "full");
            RegionExplorer explorer = Object.FindFirstObjectByType<RegionExplorer>();
            if (explorer == null || !explorer.IsReady)
            {
                if (EditorApplication.timeSinceStartup - _startedAt > TimeoutSeconds) Finish(output, "FAIL: 地域の生成が時間内に終わらなかった", 1);
                return;
            }

            int frame = Time.frameCount;
            if (frame == _lastFrame)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                return;
            }
            _lastFrame = frame;

            if (_setupFrame < 0)
            {
                Setup(explorer, set);
                _setupFrame = frame;
                return;
            }

            int index = frame - _setupFrame - WarmFrames;
            if (index < 0) return;
            if (_lastIndex >= 0 && index > _lastIndex + 1) _dropped += index - _lastIndex - 1;
            _lastIndex = index;
            float now = index / Fps;
            while (_next < Cues.Count && Cues[_next].At <= now)
            {
                Cue cue = Cues[_next++];
                cue.Run(explorer);
                Line(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.00} 秒（f-{1:0000}）: {2}", now, index, cue.Label));
            }
            ScreenCapture.CaptureScreenshot(Path.Combine(output, set, string.Format("f-{0:0000}.png", index)));
            if (now >= _end)
            {
                Finish(output, string.Format("PASS: {0} の {1} 枚を撮った（飛んだフレーム {2}）", set, index + 1, _dropped), 0);
            }
        }

        private static void Setup(RegionExplorer explorer, string set)
        {
            Application.runInBackground = true;
            Time.captureDeltaTime = 1f / Fps;
            explorer.MotionClockStep = 1f / Fps;
            explorer.MotionEnabled = set != "off";
            bool extras = set == "full";
            explorer.SetMotionExtras(extras, extras, extras);
            // 画面の上にある OS のマウスで読み取りが替わらないようにする。雲は既存の確認画像と同じ「薄い」
            explorer.PointerEnabled = false;
            for (int k = 0; k < 3 && explorer.CloudLevel != 1; k++) explorer.NextCloudOpacity();
            explorer.SetRules(SearchRules.Expedition);
            explorer.ShowFullWidth();
            _clueSite = explorer.Expedition.OpenClues()[0].Site;
            _angle = Approach(explorer, _clueSite);
            Line("組: " + set + "、動きの倍率 " + explorer.MotionLength + "、手掛かりの地点 " + _clueSite.Name + "（" + _clueSite.MapX + ", " + _clueSite.MapY + "）、近づく向き " + _angle + " 度");
            explorer.MoveCursor(At(24f, 40f).x, At(24f, 40f).y);
            BuildCues();
        }

        private static float _angle;

        /// <summary>地点から距離 13 で弱い反応、距離 5 で強い反応になる向き（度）を探す。無ければ 0 度。</summary>
        private static float Approach(RegionExplorer explorer, RegionSite site)
        {
            for (int a = 0; a < 360; a += 15)
            {
                Vector2 faint = Offset(site, 13f, a);
                Vector2 strong = Offset(site, 5f, a);
                Vector2 start = Offset(site, 24f, a + 40f);
                if (!Inside(faint) || !Inside(start)) continue;
                if (explorer.Session.Sense(faint.x, faint.y).Signal == SearchSignal.Faint
                    && explorer.Session.Sense(strong.x, strong.y).Signal == SearchSignal.Strong) return a;
            }
            return 0f;
        }

        private static bool Inside(Vector2 map)
        {
            return map.x > 4f && map.x < 96f && map.y > 4f && map.y < 96f;
        }

        private static Vector2 Offset(RegionSite site, float distance, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            return new Vector2(site.MapX + Mathf.Cos(r) * distance, site.MapY + Mathf.Sin(r) * distance);
        }

        /// <summary>今の地点から、近づく向きを基準に角度 turn だけずらした向きへ distance 離れた位置。</summary>
        private static Vector2 At(float distance, float turn)
        {
            return Offset(_clueSite, distance, _angle + turn);
        }

        /// <summary>台本（秒）。場面の区切りは docs と確認ページの「場面」と同じ。</summary>
        private static void BuildCues()
        {
            Cues.Clear();
            // 1 カーソルの移動（0〜4.6 秒）
            Move(0.4f, 18f, 28f);
            Move(1.8f, 12f, -30f);
            Move(3.2f, 13f, 0f);
            // 2 サーチの反応（4.6〜11.6 秒）：弱い反応、近づいて強い反応
            Search(5.0f);
            Move(7.6f, 5f, 0f);
            Search(8.6f);
            // 3 発見（11.6〜17.0 秒）：印・カード・一覧への追加・知らせの帯
            Move(12.0f, 0.8f, 0f);
            Search(13.0f);
            // 4 遠征の部品（17.0〜22.6 秒）：手掛かりを読む（帯が縮む・文が出る）、離れてサーチ（カードが閉じる・矢印）
            Add(17.4f, "手掛かりを読む", e =>
            {
                List<RegionClue> open = e.Expedition.OpenClues();
                RegionClue target = open.Find(c => c.State != ClueState.Read);
                Line("  読む: " + (target != null ? target.Title + " → " + e.ReadClue(target) : "読める物なし"));
            });
            Move(18.6f, 9f, 120f);
            Search(19.4f);
            // 5 遠征の終わり（22.6〜26.0 秒）
            Add(23.0f, "日数が尽きるまでサーチ", e =>
            {
                RegionExpedition ex = e.Expedition;
                for (int k = 0; k < 60 && !ex.IsOver; k++)
                {
                    int before = ex.DaysLeft;
                    e.SearchAtCursor();
                    if (ex.DaysLeft == before && !ex.IsOver) e.MoveCursor(ex.StartCamp.MapX + 1f, ex.StartCamp.MapY);
                }
                Line("  終わり " + ex.IsOver + "、発見 " + e.Session.Discovered.Count + "、残り " + ex.DaysLeft);
            });
            // 6 前の規則（反応だけ）でも同じ動きが出る（26.0〜38.0 秒）
            Add(26.0f, "前の規則へ", e =>
            {
                e.SetRules(SearchRules.Signal);
                RegionSite site = e.Region.Sites[0];
                _clueSite = site;
                _angle = Approach(e, site);
                Line("  前の規則の地点 " + site.Name + "、近づく向き " + _angle + " 度");
                e.SetView(1f, site.MapX, site.MapY);
                e.ShowFullWidth();
                e.MoveCursor(At(20f, 40f).x, At(20f, 40f).y);
            });
            Move(27.0f, 13f, 0f);
            Search(28.4f);
            Move(31.0f, 5f, 0f);
            Search(32.0f);
            Move(34.4f, 0.8f, 0f);
            Search(35.2f);
            _end = 38.0f;
        }

        private static void Move(float at, float distance, float turn)
        {
            Add(at, string.Format(System.Globalization.CultureInfo.InvariantCulture, "地点から距離 {0}・向き +{1} 度へ移動", distance, turn),
                e => e.MoveCursor(At(distance, turn).x, At(distance, turn).y));
        }

        private static void Search(float at)
        {
            Add(at, "サーチ", e =>
            {
                SearchResult result = e.SearchAtCursor();
                Line("  結果: " + result.Signal + "・" + RegionCompass.Label(e.LastDirection) + (e.Expedition != null && e.Rules == SearchRules.Expedition ? "、残り " + e.Expedition.DaysLeft + " 日" : string.Empty));
            });
        }

        private static void Add(float at, string label, System.Action<RegionExplorer> run)
        {
            Cues.Add(new Cue { At = at, Label = label, Run = run });
        }

        private static void Line(string line)
        {
            Debug.Log("[RegionMap] 動きの撮影 " + line);
            Log.Append(line).Append('\n');
        }

        private static void Finish(string output, string result, int exitCode)
        {
            EditorApplication.update -= Tick;
            string set = SessionState.GetString(SetKey, "full");
            SessionState.EraseString(OutputKey);
            SessionState.EraseString(SetKey);
            Time.captureDeltaTime = 0f;
            Line(result);
            File.WriteAllText(Path.Combine(output, set + "-capture.txt"), Log.ToString());
            EditorApplication.Exit(exitCode);
        }
    }
}
