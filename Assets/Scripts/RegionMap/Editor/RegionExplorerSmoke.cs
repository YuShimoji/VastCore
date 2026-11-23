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
        private const int MotionWaitFrames = 600;
        private static int _motionWait;
        private static double _startedAt;
        private static double _readyAt;
        // 連続撮影（-regionClip 秒数）。開始直後の画面を 1 秒に ClipFps 枚ずつ clip/ へ残す
        private const string ClipKey = "Vastcore.RegionMap.ExplorerSmoke.Clip";
        private const double ClipFps = 10.0;
        private static double _clipUntil;
        private static double _clipNext;
        private static int _clipFrame;
        private static Vector2 _driftAtReady;
        // 細部の地形（RegionDetailLayer）ができて見えるまで待つ。拡大してから見えるまでの秒数を記録する
        private const double DetailTimeoutSeconds = 240.0;
        private static bool _awaitDetail;
        private static double _detailStartedAt;
        private static double _detailDeadline;
        private static string _detailLabel;
        // 細部を作っている間の、更新と更新の最長の間隔（画面が止まっていないかの目安）
        private static double _lastTickAt;
        private static double _longestGap;
        private static double _idleLongestGap;
        private static readonly System.Text.StringBuilder DetailLog = new System.Text.StringBuilder();

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
            double tickAt = EditorApplication.timeSinceStartup;
            if (_awaitDetail && _lastTickAt > 0.0) _longestGap = System.Math.Max(_longestGap, tickAt - _lastTickAt);
            else if (!_awaitDetail && _lastTickAt > 0.0 && _readyAt > 0.0) _idleLongestGap = System.Math.Max(_idleLongestGap, tickAt - _lastTickAt);
            _lastTickAt = tickAt;
            if (_startedAt <= 0.0)
            {
                _startedAt = EditorApplication.timeSinceStartup;
                _firstStartedAt = _startedAt;
            }
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
                Debug.Log("[RegionMap] 差し替えた絵: " + (string.IsNullOrEmpty(explorer.CustomSkin) ? "なし（既定の絵）" : explorer.CustomSkin));
            }

            if (_wait > 0)
            {
                _wait--;
                return;
            }

            // 動き（SP-021 軸 E）が落ち着いてから次の操作と撮影をする。入力は動きを待たないが、撮る画面は終わりの姿にそろえる
            if (explorer.MotionBusy && _motionWait < MotionWaitFrames)
            {
                _motionWait++;
                return;
            }
            if (_motionWait >= MotionWaitFrames) Debug.Log("[RegionMap] 動きが " + MotionWaitFrames + " フレームで落ち着かなかった（手順 " + step + "）");
            _motionWait = 0;

            if (_awaitDetail)
            {
                RegionDetailLayer layer = explorer.Detail;
                bool done = layer.Phase == RegionDetailPhase.Ready && layer.Alpha >= 1f;
                double now = EditorApplication.timeSinceStartup;
                if (layer.Phase == RegionDetailPhase.Failed) Finish(output, "FAIL: 細部の作成に失敗した（" + layer.Failure + "）", 1);
                if (!done)
                {
                    if (now > _detailDeadline) Finish(output, "FAIL: 細部が時間内に出なかった（" + _detailLabel + "）", 1);
                    return;
                }
                _awaitDetail = false;
                RegionDetail d = layer.Current;
                string line = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{0}: 窓 {1:0.#} km・{2:0.0} m/セル、拡大してから見えるまで {3:0.0} 秒（作成 {4} ms、描画 {5} ms）、海陸の一致 {6:P1}、川の一致 {7:P0}、標高の差の平均 {8:0.0} m、待つ間の更新の最長間隔 {9:0} ms",
                    _detailLabel, d.Data.SizeKm, d.Data.CellMeters, now - _detailStartedAt, layer.LastBuildMs, layer.LastRenderMs,
                    d.CoastAgreement, d.RiverAgreement, d.MeanHeightDifference, _longestGap * 1000.0);
                _longestGap = 0.0;
                Debug.Log("[RegionMap] 細部 " + line);
                DetailLog.Append(line).Append('\n');
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
            // 細部の確認は、まだ発見していない 2 つ目の地点の上で行う（拡大した表示でのサーチと発見を確かめるため）
            RegionSite second = explorer.Region.Sites.Count > 1 ? explorer.Region.Sites[1] : target;
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
                case 11:
                    explorer.NextStyle();
                    break;
                case 12:
                    explorer.NextStyle();
                    break;
                case 13:
                    // 細部の確認の準備: 地点の上へ拡大 ×10。まず細部を切った広域の絵を撮る（比べる材料）
                    if (second != null)
                    {
                        explorer.Detail.Enabled = false;
                        explorer.MoveCursor(second.MapX, second.MapY);
                        explorer.SetView(10f, second.MapX, second.MapY);
                    }
                    break;
                case 14:
                    Capture(output, "detail-0-broad-x10.png");
                    break;
                case 15:
                    explorer.Detail.Enabled = true;
                    _detailLabel = "×10";
                    _detailStartedAt = EditorApplication.timeSinceStartup;
                    break;
                case 16:
                    // 細部を作っている最中の画面（広域の絵のまま、札に作成中と出る）
                    Capture(output, "detail-1-building-x10.png");
                    _awaitDetail = true;
                    _detailDeadline = EditorApplication.timeSinceStartup + DetailTimeoutSeconds;
                    break;
                case 17:
                    Capture(output, "detail-2-ready-x10.png");
                    break;
                case 18:
                    // 拡大した表示でも、サーチと発見が同じように働く
                    if (second != null)
                    {
                        explorer.MoveCursor(second.MapX + 1.0f, second.MapY);
                        SearchResult near2 = explorer.SearchAtCursor();
                        Debug.Log("[RegionMap] 細部 ×10 で 1 離れた位置のサーチ: " + near2.Signal);
                        Debug.Log("[RegionMap] 細部 ×10 で発見した地点: " + (near2.Site != null ? near2.Site.Name : "なし"));
                    }
                    break;
                case 19:
                    Capture(output, "detail-3-found-x10.png");
                    break;
                case 20:
                    if (second != null) explorer.SetView(40f, second.MapX, second.MapY);
                    _detailLabel = "×40";
                    _detailStartedAt = EditorApplication.timeSinceStartup;
                    break;
                case 21:
                    Capture(output, "detail-4-building-x40.png");
                    _awaitDetail = true;
                    _detailDeadline = EditorApplication.timeSinceStartup + DetailTimeoutSeconds;
                    break;
                case 22:
                    Capture(output, "detail-5-ready-x40.png");
                    break;
                case 23:
                    if (second != null) explorer.SetView(64f, second.MapX, second.MapY);
                    _detailLabel = "×64";
                    _detailStartedAt = EditorApplication.timeSinceStartup;
                    break;
                case 24:
                    Capture(output, "detail-6-building-x64.png");
                    _awaitDetail = true;
                    _detailDeadline = EditorApplication.timeSinceStartup + DetailTimeoutSeconds;
                    break;
                case 25:
                    Capture(output, "detail-7-ready-x64.png");
                    break;
                case 26:
                    // 描き方を替えると、細部も同じ描き方に描き直される
                    explorer.NextStyle();
                    _detailLabel = "×64 地図帳";
                    _detailStartedAt = EditorApplication.timeSinceStartup;
                    _awaitDetail = true;
                    _detailDeadline = EditorApplication.timeSinceStartup + DetailTimeoutSeconds;
                    break;
                case 27:
                    Capture(output, "detail-8-atlas-x64.png");
                    break;
                case 28:
                    // 縮めると広域へ戻る
                    explorer.SetView(2f, 50f, 50f);
                    break;
                case 29:
                    Capture(output, "detail-9-back-x2.png");
                    Debug.Log("[RegionMap] 縮めた後の細部: " + explorer.Detail.Phase + "、濃さ " + explorer.Detail.Alpha.ToString("0.00"));
                    break;
                case 30:
                    // 縮めてすぐ同じ場所を同じ拡大率へ戻すと、残っている窓がそのまま出る（作り直さない）
                    explorer.SetView(64f, second.MapX, second.MapY);
                    break;
                case 31:
                    Capture(output, "detail-10-again-x64.png");
                    Debug.Log("[RegionMap] 縮めて拡大し直した後の細部: " + explorer.Detail.Phase + "、作成 " + explorer.Detail.BuildCount + " 回");
                    // 窓の外へ動かすと、見える範囲が窓から出る分だけ作り直す
                    explorer.SetView(64f, second.MapX + 1.6f, second.MapY);
                    _detailLabel = "×64 動かした後";
                    _detailStartedAt = EditorApplication.timeSinceStartup;
                    break;
                case 32:
                    Capture(output, "detail-11-panned-building-x64.png");
                    _awaitDetail = true;
                    _detailDeadline = EditorApplication.timeSinceStartup + DetailTimeoutSeconds;
                    break;
                case 33:
                    Capture(output, "detail-12-panned-ready-x64.png");
                    break;
                case 34:
                    // 名前の確認の準備: 描き方を衛星写真へ戻す（地図帳 → 立体 → 衛星写真）
                    explorer.NextStyle();
                    break;
                case 35:
                    explorer.NextStyle();
                    break;
                case 36:
                    // 名前が 2 つ以上ある地点（土地の名と、古い呼び名や記号名）を発見して、カードの別名と由来を撮る
                    RegionSite named = NamedSite(explorer);
                    if (named != null)
                    {
                        explorer.SetView(1f, named.MapX, named.MapY);
                        explorer.MoveCursor(named.MapX, named.MapY);
                        SearchResult foundNamed = explorer.SearchAtCursor();
                        Debug.Log("[RegionMap] 名前が複数ある地点のサーチ: " + foundNamed.Signal + " / " + named.Name + "（名前 " + named.Names.Count + " つ）");
                    }
                    else
                    {
                        Debug.Log("[RegionMap] 名前が複数ある未発見の地点は無い");
                    }
                    break;
                case 37:
                    Capture(output, "explorer-8-named.png");
                    break;
                case 38:
                    // 遠征の規則（SP-021 軸 H）：同じ地域ではじめからやり直し、始めの画面を撮る
                    explorer.SetRules(SearchRules.Expedition);
                    explorer.RestartExpedition();
                    explorer.ShowFullWidth();
                    ExpeditionLine("始め: 野営地 " + explorer.Expedition.StartCamp.Name + "、残り " + explorer.Expedition.DaysLeft + " 日、読める手掛かり " + explorer.Expedition.OpenClues().Count + " 件");
                    break;
                case 39:
                    Capture(output, "expedition-1-start.png");
                    break;
                case 40:
                    {
                        // 読める手掛かりの地点から東へ 5 ずらした所でサーチする（強い反応と方角）
                        RegionClue clue = explorer.Expedition.OpenClues()[0];
                        explorer.MoveCursor(clue.Site.MapX + 5f, clue.Site.MapY);
                        SearchResult near = explorer.SearchAtCursor();
                        ExpeditionLine("手掛かり「" + clue.Title + "」の東 5 でサーチ: " + near.Signal + "・" + RegionCompass.Label(explorer.LastDirection) + "、残り " + explorer.Expedition.DaysLeft + " 日");
                    }
                    break;
                case 41:
                    Capture(output, "expedition-2-direction.png");
                    break;
                case 42:
                    {
                        RegionClue clue = explorer.Expedition.OpenClues()[0];
                        bool read = explorer.ReadClue(clue);
                        ExpeditionLine("手掛かり「" + clue.Title + "」を詳しく読む: " + read + "「" + clue.DetailLine + "」、残り " + explorer.Expedition.DaysLeft + " 日");
                    }
                    break;
                case 43:
                    Capture(output, "expedition-3-clue-read.png");
                    break;
                case 44:
                    {
                        RegionClue clue = explorer.Expedition.OpenClues()[0];
                        explorer.MoveCursor(clue.Site.MapX + 0.8f, clue.Site.MapY);
                        SearchResult found = explorer.SearchAtCursor();
                        ExpeditionLine("その地点でサーチ: " + found.Signal + "、拠点 " + explorer.Expedition.Camps.Count + "、残り " + explorer.Expedition.DaysLeft + " 日、読める手掛かり " + explorer.Expedition.OpenClues().Count + " 件");
                    }
                    break;
                case 45:
                    Capture(output, "expedition-4-found-camp.png");
                    break;
                case 46:
                    {
                        // 拠点から遠い所へカーソルだけ動かす（日数の内訳と、拠点からの線）
                        ExpeditionCamp start = explorer.Expedition.StartCamp;
                        explorer.MoveCursor(Mathf.Clamp(start.MapX + 30f, 5f, 95f), Mathf.Clamp(start.MapY - 22f, 5f, 95f));
                        ExpeditionCost cost = explorer.Expedition.CostAt(explorer.CursorMapPosition.x, explorer.CursorMapPosition.y);
                        ExpeditionLine("遠い所の日数: " + cost.Total + " 日（拠点「" + cost.Camp.Name + "」から " + explorer.Expedition.Km(cost.CampDistance) + " km）");
                    }
                    break;
                case 47:
                    Capture(output, "expedition-5-cost-far.png");
                    break;
                case 48:
                    {
                        // 日数が尽きるまでサーチする。足りない時は出発の野営地へ戻ってサーチする
                        RegionExpedition ex = explorer.Expedition;
                        for (int k = 0; k < 60 && !ex.IsOver; k++)
                        {
                            int before = ex.DaysLeft;
                            explorer.SearchAtCursor();
                            if (ex.DaysLeft == before && !ex.IsOver) explorer.MoveCursor(ex.StartCamp.MapX + 1f, ex.StartCamp.MapY);
                        }
                        ExpeditionLine("使い切り: 終わり " + ex.IsOver + "、発見 " + explorer.Session.Discovered.Count + "、サーチ " + explorer.Session.SearchCount + " 回、使った日数 " + ex.DaysUsed + "、残り " + ex.DaysLeft);
                    }
                    break;
                case 49:
                    Capture(output, "expedition-6-end.png");
                    break;
                case 50:
                    // 前の規則（反応だけ）へ戻せる
                    explorer.SetRules(SearchRules.Signal);
                    if (target != null)
                    {
                        explorer.MoveCursor(target.MapX + 6f, target.MapY);
                        ExpeditionLine("前の規則で 6 離れた位置のサーチ: " + explorer.SearchAtCursor().Signal + "、規則 " + explorer.Rules);
                    }
                    break;
                case 51:
                    Capture(output, "expedition-7-signal-rules.png");
                    break;
                case 52:
                    // 画面の保存はフレームの終わりに行われるので、規則を戻すのは撮った次の段にする
                    explorer.SetRules(SearchRules.Expedition);
                    File.WriteAllText(Path.Combine(output, "expedition-result.txt"), ExpeditionLog.ToString());
                    // この後で別の地域へ移ると数え直しになるので、結果の行に出す数はここで控える
                    _summary = string.Format("発見 {0} / {1}、サーチ {2} 回", explorer.Session.Discovered.Count, explorer.Session.TotalSites, explorer.Session.SearchCount);
                    NamingLog.Length = 0;
                    NamingLine(explorer);
                    break;
                case 53:
                case 56:
                    // 名前の出し方：土地の経緯と音の質感は地域ごとに 1 つ。別の地域を 2 つ続けて作り、全体表示で撮る
                    explorer.NextRegion();
                    _startedAt = EditorApplication.timeSinceStartup;
                    break;
                case 54:
                case 57:
                    explorer.ShowWholeRegion();
                    NamingLine(explorer);
                    break;
                case 55:
                    Capture(output, "naming-1-next-region.png");
                    break;
                case 58:
                    Capture(output, "naming-2-next-region.png");
                    break;
                case 59:
                    File.WriteAllText(Path.Combine(output, "naming-result.txt"), NamingLog.ToString());
                    break;
                default:
                    Vector2 drift = explorer.CloudDrift - _driftAtReady;
                    DetailLog.Append("作っていない間の更新の最長間隔 ").Append((_idleLongestGap * 1000.0).ToString("0")).Append(" ms").Append('\n');
                    Debug.Log("[RegionMap] 作っていない間の更新の最長間隔 " + (_idleLongestGap * 1000.0).ToString("0") + " ms");
                    File.WriteAllText(Path.Combine(output, "explorer-detail-result.txt"), DetailLog.ToString());
                    Finish(output, string.Format("PASS: {0}、画面 {1}x{2}、地図が出るまで {3:0.0} 秒、雲は {4:0.0} 秒で東へ {5:0.0000}・南へ {6:0.0000}（地図の幅を 1 として）",
                        _summary, Screen.width, Screen.height,
                        _readyAt - _firstStartedAt, EditorApplication.timeSinceStartup - _readyAt, drift.x, -drift.y), 0);
                    return;
            }
            SessionState.SetInt(StepKey, step + 1);
            _wait = FramesBetweenSteps;
        }

        /// <summary>まだ発見していない地点のうち、名前がいちばん多い物（2 つ以上ある時だけ）。</summary>
        private static RegionSite NamedSite(RegionExplorer explorer)
        {
            RegionSite best = null;
            foreach (RegionSite site in explorer.Region.Sites)
            {
                if (site.Names.Count < 2 || explorer.Session.IsDiscovered(site)) continue;
                if (best == null || site.Names.Count > best.Names.Count) best = site;
            }
            return best;
        }

        private static readonly System.Text.StringBuilder ExpeditionLog = new System.Text.StringBuilder();
        private static readonly System.Text.StringBuilder NamingLog = new System.Text.StringBuilder();
        private static string _summary = "";

        /// <summary>再生を始めた時刻。_startedAt は別の地域を作るたびに取り直す（生成の待ち時間の上限に使う）ので、分けて持つ。</summary>
        private static double _firstStartedAt;

        /// <summary>今の地域の、経緯・音の質感・地域の名・最初のエリアと峰の名を 1 行にして残す。</summary>
        private static void NamingLine(RegionExplorer explorer)
        {
            RegionData data = explorer.Region;
            RegionFeature region = data.Features.Find(f => f.Kind == RegionFeatureKind.Region);
            RegionFeature peak = data.Features.Find(f => f.Kind == RegionFeatureKind.Peak);
            string line = "seed " + data.Spec.Seed + ": 経緯 " + (data.Naming != null ? data.Naming.History : "（規則から付けた名前でない）")
                + "、ことばの音 " + (data.Naming != null ? data.Naming.Sound : "-")
                + "、地域の名 " + (region != null ? region.Name : "-")
                + "、エリア 1 " + (data.Areas.Count > 0 ? data.Areas[0].Name : "-")
                + "、峰 1 " + (peak != null ? peak.Name : "-");
            Debug.Log("[RegionMap] 名前の出し方 " + line);
            NamingLog.Append(line).Append('\n');
        }

        private static void ExpeditionLine(string line)
        {
            Debug.Log("[RegionMap] 遠征 " + line);
            ExpeditionLog.Append(line).Append('\n');
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
