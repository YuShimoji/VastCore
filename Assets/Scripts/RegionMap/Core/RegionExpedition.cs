using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Vastcore.RegionMap
{
    /// <summary>探索画面のサーチの規則。</summary>
    public enum SearchRules
    {
        /// <summary>反応の強さだけを返す（2026-11-21 からの規則）。回数の限りも見返りも無い。</summary>
        Signal = 0,

        /// <summary>拠点から日数を使って足を伸ばし、手掛かりを読んで探す（SP-021 軸 H の最小の実装）。</summary>
        Expedition = 1,
    }

    /// <summary>8 方位。東から反時計回りに 45 度ずつ（地図の上が北）。</summary>
    public enum CompassPoint
    {
        None = -1,
        East = 0,
        NorthEast = 1,
        North = 2,
        NorthWest = 3,
        West = 4,
        SouthWest = 5,
        South = 6,
        SouthEast = 7,
    }

    public static class RegionCompass
    {
        private static readonly string[] Labels = { "東", "北東", "北", "北西", "西", "南西", "南", "南東" };

        /// <summary>地図座標の from から to への 8 方位（Y は南へ増える）。同じ位置なら None。</summary>
        public static CompassPoint Toward(float fromX, float fromY, float toX, float toY)
        {
            float dx = toX - fromX;
            float dy = fromY - toY; // 北を正にする
            if (dx * dx + dy * dy < 1e-8f) return CompassPoint.None;
            double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
            if (angle < 0.0) angle += 360.0;
            return (CompassPoint)((int)Math.Round(angle / 45.0) % 8);
        }

        public static string Label(CompassPoint point)
        {
            return point == CompassPoint.None ? string.Empty : Labels[(int)point];
        }

        /// <summary>方位の角度（度。東が 0、北が 90）。</summary>
        public static float Degrees(CompassPoint point)
        {
            return point == CompassPoint.None ? 0f : (int)point * 45f;
        }
    }

    /// <summary>移動の日数を数え始める所。出発の野営地と、発見した地点。</summary>
    public sealed class ExpeditionCamp
    {
        public float MapX;
        public float MapY;
        public string Name;

        /// <summary>発見して拠点になった地点。出発の野営地は null。</summary>
        public RegionSite Site;
    }

    /// <summary>1 回のサーチにかかる日数の内訳の 1 行。</summary>
    public struct ExpeditionCostPart
    {
        public string Label;
        public int Days;
    }

    public sealed class ExpeditionCost
    {
        public int Total;
        public readonly List<ExpeditionCostPart> Parts = new List<ExpeditionCostPart>();

        /// <summary>日数を数えた拠点（その位置にいちばん近い拠点）。</summary>
        public ExpeditionCamp Camp;
        public float CampDistance;
    }

    public enum ClueState
    {
        /// <summary>まだ読めない。</summary>
        Locked = 0,

        /// <summary>呼び名と場所の文が読める。</summary>
        Open = 1,

        /// <summary>日数を使って、目印からの方角まで読んだ。</summary>
        Read = 2,

        /// <summary>その地点を発見した。</summary>
        Solved = 3,
    }

    /// <summary>地点 1 つの手掛かり。文は地点のデータ（名前・エリア・置かれた理由・目印）から作る仮の文。</summary>
    public sealed class RegionClue
    {
        public RegionSite Site;
        public ClueState State;

        /// <summary>見出し。土地の名があればその名、記号名だけなら十の位だけを見せた記号名（例「MN-5?5?」）。</summary>
        public string Title;

        /// <summary>1 行目：呼び名と種類。</summary>
        public string NameLine;

        /// <summary>2 行目：エリアと、地点が置かれた理由（採点の説明を除いた文）。</summary>
        public string PlaceLine;

        /// <summary>3 行目（詳しく読むと出る）：目印からの方角と距離、記号名だけの地点は記号名の全体。</summary>
        public string DetailLine;

        /// <summary>読めるようになった順（0 から。まだ読めなければ -1）。</summary>
        public int OpenedOrder = -1;
    }

    /// <summary>
    /// 発見の見返り。今は拠点・日数・手掛かり。
    /// 資材・通貨・部品などを足す時は、ここに項目を足し、RegionExpedition.Reward で値を入れる。
    /// </summary>
    public sealed class ExpeditionReward
    {
        public int Days;
        public ExpeditionCamp Camp;
        public readonly List<RegionClue> OpenedClues = new List<RegionClue>();
    }

    /// <summary>遠征の規則でのサーチ 1 回の結果。</summary>
    public struct ExpeditionStep
    {
        /// <summary>サーチをしたか。日数が足りない・遠征が終わっている時は false で、Refusal に理由が入る。</summary>
        public bool Done;
        public string Refusal;
        public SearchResult Result;
        public int DaysSpent;

        /// <summary>強い・弱い反応の時の、いちばん近い未発見地点への方角。</summary>
        public CompassPoint Direction;

        /// <summary>発見した時の見返り。それ以外は null。</summary>
        public ExpeditionReward Reward;

        /// <summary>このサーチで遠征が終わったか。</summary>
        public bool EndedNow;
    }

    /// <summary>
    /// 1 回の遠征（SP-021 軸 H、C 拠点から足を伸ばす＋B 手掛かりを読む の最小の実装）。
    /// サーチには日数がかかり、いちばん近い拠点から遠いほど、雪・岩・高地や海の上ほど多くかかる。
    /// 発見した地点は拠点になり、日数が戻り、近くの地点の手掛かりが読めるようになる。日数が尽きると終わる。
    /// 土地の経緯（RegionLandHistory）は使わない（経緯の決まり方を地域ごとに 1 つへ変える作業と切り離すため）。
    /// 表示や入力には依存しない。数値はすべて仮（seed 7・3・21 で、手掛かりを読んで探す素直な遊び方が 18 地点のうち平均 13 前後を見つける値）。
    /// </summary>
    public sealed class RegionExpedition
    {
        private static readonly Regex ScoreTail = new Regex("。[^（）]*採点）");
        private static readonly Regex ScoreNote = new Regex("（[^（）]*採点）");

        private readonly ExplorationSession _session;
        private readonly RegionData _data;
        private readonly List<ExpeditionCamp> _camps = new List<ExpeditionCamp>();
        private readonly List<RegionClue> _clues = new List<RegionClue>();
        private readonly List<Landmark> _landmarks = new List<Landmark>();
        private int _openedCount;

        /// <summary>始めの日数。</summary>
        public int StartDays = 30;

        /// <summary>拠点からこの距離（地図座標）ごとに、移動の日数が 1 日増える。256 km 四方の地域で 10 は約 26 km。</summary>
        public float CampStep = 10f;

        /// <summary>雪・岩・ツンドラ、この標高（m）より高い所、海の上で足す日数。</summary>
        public int RoughDays = 1;
        public float HighlandMeters = 1500f;

        /// <summary>発見で戻る日数。</summary>
        public int FindDays = 2;

        /// <summary>手掛かりを詳しく読むのにかかる日数。</summary>
        public int ReadDays = 1;

        /// <summary>始めに読める手掛かりの数（出発の野営地に近い順。土地の名を持つ地点と、記号名だけの地点）。</summary>
        public int StartNamedClues = 3;
        public int StartCodeClues = 1;

        /// <summary>発見で読めるようになる手掛かりの数（発見した地点に近い順）。</summary>
        public int CluesPerFind = 2;

        public ExpeditionCamp StartCamp { get; private set; }
        public int DaysLeft { get; private set; }
        public int DaysUsed { get; private set; }
        public int DaysGained { get; private set; }
        public bool IsOver { get; private set; }

        /// <summary>手掛かりや拠点が変わるたびに増える（画面の作り直しの判断に使う）。</summary>
        public int Version { get; private set; }

        public RegionExpedition(ExplorationSession session)
        {
            if (session == null) throw new ArgumentNullException("session");
            _session = session;
            _data = session.Region;
            CollectLandmarks();
            Begin();
        }

        public ExplorationSession Session
        {
            get { return _session; }
        }

        public IReadOnlyList<ExpeditionCamp> Camps
        {
            get { return _camps; }
        }

        /// <summary>全地点の手掛かり（地点の並びと同じ順）。</summary>
        public IReadOnlyList<RegionClue> Clues
        {
            get { return _clues; }
        }

        /// <summary>地図座標 1 が何 km か。</summary>
        public float KmPerUnit
        {
            get { return _data.SizeKm / RegionData.MapUnits; }
        }

        /// <summary>はじめから遠征をやり直す。発見済みとサーチの回数も消す。数値を変えた後にも呼ぶ。</summary>
        public void Begin()
        {
            _session.Reset();
            _camps.Clear();
            DaysLeft = StartDays;
            DaysUsed = 0;
            DaysGained = 0;
            IsOver = false;
            StartCamp = ChooseStartCamp();
            _camps.Add(StartCamp);
            BuildClues();
            Version++;
        }

        /// <summary>読める手掛かり（まだ発見していないもの）を、読めるようになった順に返す。</summary>
        public List<RegionClue> OpenClues()
        {
            var open = new List<RegionClue>();
            for (int c = 0; c < _clues.Count; c++)
            {
                ClueState state = _clues[c].State;
                if (state == ClueState.Open || state == ClueState.Read) open.Add(_clues[c]);
            }
            open.Sort((a, b) => a.OpenedOrder.CompareTo(b.OpenedOrder));
            return open;
        }

        public RegionClue ClueFor(RegionSite site)
        {
            for (int c = 0; c < _clues.Count; c++) if (_clues[c].Site == site) return _clues[c];
            return null;
        }

        /// <summary>その位置でサーチすると何日かかるか。</summary>
        public ExpeditionCost CostAt(float mapX, float mapY)
        {
            var cost = new ExpeditionCost();
            float best = float.MaxValue;
            for (int c = 0; c < _camps.Count; c++)
            {
                float dx = _camps[c].MapX - mapX;
                float dy = _camps[c].MapY - mapY;
                float d = dx * dx + dy * dy;
                if (d < best)
                {
                    best = d;
                    cost.Camp = _camps[c];
                }
            }
            cost.CampDistance = (float)Math.Sqrt(best);
            Add(cost, "サーチ", 1);
            int far = (int)Math.Floor(cost.CampDistance / CampStep);
            if (far > 0) Add(cost, "拠点から " + Km(cost.CampDistance) + " km", far);
            if (_data.HeightAtMap(mapX, mapY) <= 0f) Add(cost, "海の上", RoughDays);
            else if (IsRough(mapX, mapY)) Add(cost, "雪・岩・高地", RoughDays);
            return cost;
        }

        /// <summary>陸の上で、雪・岩・ツンドラか、HighlandMeters より高い所。</summary>
        private bool IsRough(float mapX, float mapY)
        {
            float height = _data.HeightAtMap(mapX, mapY);
            if (height <= 0f) return false;
            RegionBiome biome = _data.BiomeAtMap(mapX, mapY);
            return biome == RegionBiome.Snow || biome == RegionBiome.Tundra || biome == RegionBiome.Rock || height > HighlandMeters;
        }

        /// <summary>日数を使ってサーチする。日数が足りなければサーチせず、理由を返す。</summary>
        public ExpeditionStep Search(float mapX, float mapY)
        {
            var step = new ExpeditionStep { Direction = CompassPoint.None };
            if (IsOver)
            {
                step.Refusal = "遠征は終わった";
                return step;
            }
            ExpeditionCost cost = CostAt(mapX, mapY);
            if (cost.Total > DaysLeft)
            {
                step.Refusal = "日数が足りない（ここは " + cost.Total + " 日、残り " + DaysLeft + " 日）。拠点の近くでサーチする";
                return step;
            }

            DaysLeft -= cost.Total;
            DaysUsed += cost.Total;
            step.Done = true;
            step.DaysSpent = cost.Total;
            step.Result = _session.Search(mapX, mapY);
            SearchResult result = step.Result;
            if (result.Signal == SearchSignal.Found)
            {
                step.Reward = Reward(result.Site);
            }
            else if ((result.Signal == SearchSignal.Strong || result.Signal == SearchSignal.Faint) && result.Nearest != null)
            {
                step.Direction = RegionCompass.Toward(mapX, mapY, result.Nearest.MapX, result.Nearest.MapY);
            }
            step.EndedNow = CheckEnd();
            Version++;
            return step;
        }

        /// <summary>手掛かりを詳しく読む（目印からの方角と距離が出る）。日数を使う。</summary>
        public bool ReadClue(RegionClue clue)
        {
            if (IsOver || clue == null || clue.State != ClueState.Open || DaysLeft < ReadDays) return false;
            DaysLeft -= ReadDays;
            DaysUsed += ReadDays;
            clue.State = ClueState.Read;
            CheckEnd();
            Version++;
            return true;
        }

        /// <summary>拠点のどこでサーチしても日数が足りないか、全部を見つけたら、遠征を終える。</summary>
        private bool CheckEnd()
        {
            if (IsOver) return false;
            bool all = _session.Discovered.Count >= _session.TotalSites;
            int cheapest = int.MaxValue;
            for (int c = 0; c < _camps.Count; c++) cheapest = Math.Min(cheapest, CostAt(_camps[c].MapX, _camps[c].MapY).Total);
            if (all || DaysLeft < cheapest)
            {
                IsOver = true;
                return true;
            }
            return false;
        }

        /// <summary>すべての地点を見つけて終わったか。</summary>
        public bool Completed
        {
            get { return IsOver && _session.Discovered.Count >= _session.TotalSites; }
        }

        private ExpeditionReward Reward(RegionSite site)
        {
            var reward = new ExpeditionReward();
            var camp = new ExpeditionCamp { MapX = site.MapX, MapY = site.MapY, Name = site.Name, Site = site };
            _camps.Add(camp);
            reward.Camp = camp;
            DaysLeft += FindDays;
            DaysGained += FindDays;
            reward.Days = FindDays;

            RegionClue own = ClueFor(site);
            if (own != null) own.State = ClueState.Solved;

            // 発見した地点に近い、まだ読めない地点の手掛かりが読めるようになる
            var locked = new List<RegionClue>();
            for (int c = 0; c < _clues.Count; c++)
            {
                if (_clues[c].State == ClueState.Locked && !_session.IsDiscovered(_clues[c].Site)) locked.Add(_clues[c]);
            }
            locked.Sort((a, b) => Distance(a.Site, site.MapX, site.MapY).CompareTo(Distance(b.Site, site.MapX, site.MapY)));
            for (int c = 0; c < locked.Count && c < CluesPerFind; c++)
            {
                Open(locked[c]);
                reward.OpenedClues.Add(locked[c]);
            }
            return reward;
        }

        // ---- 出発の野営地 ----

        /// <summary>
        /// 地点をいちばん多く含む陸のエリア（同じ数なら広い方）の名前の位置の少し南に置く。
        /// 名前の位置が海・雪・岩・高地のエリアは、他に無い時だけ使う。地点の発見の範囲に入る位置や海の上は避ける。
        /// </summary>
        private ExpeditionCamp ChooseStartCamp()
        {
            RegionArea best = null;
            bool bestRough = true;
            for (int a = 0; a < _data.Areas.Count; a++)
            {
                RegionArea area = _data.Areas[a];
                if (_data.HeightAtMap(area.LabelX, area.LabelY) <= 0f) continue;
                bool rough = IsRough(area.LabelX, area.LabelY);
                bool better = best == null
                    || (bestRough && !rough)
                    || (rough == bestRough && (area.SiteCount > best.SiteCount || (area.SiteCount == best.SiteCount && area.AreaKm2 > best.AreaKm2)));
                if (!better) continue;
                best = area;
                bestRough = rough;
            }
            float baseX = best != null ? best.LabelX : RegionData.MapUnits * 0.5f;
            float baseY = best != null ? best.LabelY : RegionData.MapUnits * 0.5f;
            float[] offsets = { 0f, 3f, 0f, -3f, 3f, 0f, -3f, 0f, 0f, 0f, 0f, 6f, 0f, -6f, 6f, 0f, -6f, 0f };
            float x = baseX;
            float y = baseY;
            for (int o = 0; o < offsets.Length; o += 2)
            {
                float cx = Clamp(baseX + offsets[o]);
                float cy = Clamp(baseY + offsets[o + 1]);
                if (_data.HeightAtMap(cx, cy) <= 0f || NearSite(cx, cy, _session.FoundRadius + 1f)) continue;
                x = cx;
                y = cy;
                break;
            }
            string name = (best != null ? best.Name : _data.PlaceNameAtMap(x, y)) + "の野営地";
            return new ExpeditionCamp { MapX = x, MapY = y, Name = name };
        }

        private bool NearSite(float x, float y, float radius)
        {
            for (int s = 0; s < _data.Sites.Count; s++) if (Distance(_data.Sites[s], x, y) <= radius) return true;
            return false;
        }

        // ---- 手掛かり ----

        private struct Landmark
        {
            public float X;
            public float Y;
            public string Text;
        }

        /// <summary>地図に名前が出ている物（エリア・川・峰・湾）を、手掛かりの目印にする。</summary>
        private void CollectLandmarks()
        {
            _landmarks.Clear();
            for (int a = 0; a < _data.Areas.Count; a++)
            {
                RegionArea area = _data.Areas[a];
                if (!string.IsNullOrEmpty(area.Name)) _landmarks.Add(new Landmark { X = area.LabelX, Y = area.LabelY, Text = area.Name });
            }
            for (int f = 0; f < _data.Features.Count; f++)
            {
                RegionFeature feature = _data.Features[f];
                if (feature.Kind == RegionFeatureKind.Region || string.IsNullOrEmpty(feature.Name)) continue;
                _landmarks.Add(new Landmark { X = feature.MapX, Y = feature.MapY, Text = feature.Name });
            }
        }

        private void BuildClues()
        {
            _clues.Clear();
            _openedCount = 0;
            for (int s = 0; s < _data.Sites.Count; s++) _clues.Add(MakeClue(_data.Sites[s]));

            // 始めは、出発の野営地に近い、土地の名を持つ地点と記号名だけの地点
            var named = new List<RegionClue>();
            var coded = new List<RegionClue>();
            for (int c = 0; c < _clues.Count; c++) (HasProperName(_clues[c].Site) ? named : coded).Add(_clues[c]);
            Comparison<RegionClue> byNear = (a, b) => Distance(a.Site, StartCamp.MapX, StartCamp.MapY).CompareTo(Distance(b.Site, StartCamp.MapX, StartCamp.MapY));
            named.Sort(byNear);
            coded.Sort(byNear);
            for (int c = 0; c < named.Count && c < StartNamedClues; c++) Open(named[c]);
            for (int c = 0; c < coded.Count && c < StartCodeClues; c++) Open(coded[c]);
        }

        private void Open(RegionClue clue)
        {
            clue.State = ClueState.Open;
            clue.OpenedOrder = _openedCount++;
        }

        private static bool HasProperName(RegionSite site)
        {
            return NameOf(site, RegionNameKind.Proper) != null;
        }

        private static string NameOf(RegionSite site, RegionNameKind kind)
        {
            for (int n = 0; n < site.Names.Count; n++) if (site.Names[n].Kind == kind) return site.Names[n].Text;
            return null;
        }

        private RegionClue MakeClue(RegionSite site)
        {
            var clue = new RegionClue { Site = site, State = ClueState.Locked };
            string kind = RegionSiteKinds.Label(site.Kind);
            string proper = NameOf(site, RegionNameKind.Proper);
            string old = NameOf(site, RegionNameKind.Old);
            string code = NameOf(site, RegionNameKind.Designation) ?? site.Name;
            if (proper != null)
            {
                clue.Title = proper;
                clue.NameLine = "「" + proper + "」と呼ばれる" + kind + "がある" + (old != null ? "。古い呼び名は「" + old + "」" : string.Empty) + "。";
            }
            else
            {
                clue.Title = MaskCode(code);
                clue.NameLine = "記号名だけが残る" + kind + "。記号名は「" + MaskCode(code) + "」（数字は座標の十の位）。";
            }

            bool land = _data.HeightAtMap(site.MapX, site.MapY) > 0f;
            string place = _data.AreaById(site.AreaId) != null ? _data.PlaceNameAtMap(site.MapX, site.MapY) + (land ? "の中" : string.Empty) + "。" : string.Empty;
            string reason = string.IsNullOrEmpty(site.Reason) ? string.Empty : ScoreNote.Replace(ScoreTail.Replace(site.Reason, "）"), string.Empty);
            clue.PlaceLine = place + (reason.Length > 0 ? reason + "。" : string.Empty);

            if (proper != null)
            {
                Landmark mark;
                float d;
                if (NearestLandmark(site, out mark, out d))
                {
                    clue.DetailLine = "「" + mark.Text + "」の名がある所から、" + RegionCompass.Label(RegionCompass.Toward(mark.X, mark.Y, site.MapX, site.MapY)) + "へ約 " + Km(d) + " km。";
                }
                else
                {
                    clue.DetailLine = "目印になる名前が近くに無い。";
                }
            }
            else
            {
                clue.DetailLine = "記号名の全体は「" + code + "」。";
            }
            return clue;
        }

        /// <summary>自分の位置に重なる目印（峰と山頂の地点など、1.5 以内）は使わない。</summary>
        private bool NearestLandmark(RegionSite site, out Landmark mark, out float distance)
        {
            mark = default(Landmark);
            distance = float.MaxValue;
            bool found = false;
            for (int l = 0; l < _landmarks.Count; l++)
            {
                float dx = _landmarks[l].X - site.MapX;
                float dy = _landmarks[l].Y - site.MapY;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d < 1.5f || d >= distance) continue;
                distance = d;
                mark = _landmarks[l];
                found = true;
            }
            return found;
        }

        /// <summary>「MN-5659」→「MN-5?5?」（座標の十の位だけ見せる）。形が違えばそのまま。</summary>
        public static string MaskCode(string code)
        {
            Match m = Regex.Match(code ?? string.Empty, "^([A-Z]+)-(\\d)(\\d)(\\d)(\\d)$");
            return m.Success ? m.Groups[1].Value + "-" + m.Groups[2].Value + "?" + m.Groups[4].Value + "?" : code;
        }

        // ---- 小さな計算 ----

        private static void Add(ExpeditionCost cost, string label, int days)
        {
            cost.Parts.Add(new ExpeditionCostPart { Label = label, Days = days });
            cost.Total += days;
        }

        /// <summary>距離（地図座標）を km の丸めた値にする。10 km 未満は 1 km 単位、それ以上は 5 km 単位。</summary>
        public int Km(float mapDistance)
        {
            float k = mapDistance * KmPerUnit;
            if (k < 10f) return Math.Max(1, (int)Math.Round(k));
            return (int)Math.Round(k / 5f) * 5;
        }

        private static float Distance(RegionSite site, float x, float y)
        {
            float dx = site.MapX - x;
            float dy = site.MapY - y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static float Clamp(float v)
        {
            return Math.Max(0f, Math.Min(RegionData.MapUnits, v));
        }
    }

    /// <summary>地点の種類の呼び方。</summary>
    public static class RegionSiteKinds
    {
        public static string Label(RegionSiteKind kind)
        {
            switch (kind)
            {
                case RegionSiteKind.Monolith: return "モノリス";
                case RegionSiteKind.Arch: return "アーチ";
                case RegionSiteKind.Ring: return "環";
                case RegionSiteKind.Tower: return "塔";
                case RegionSiteKind.Dome: return "ドーム";
                case RegionSiteKind.Terrace: return "段丘";
                case RegionSiteKind.Sunken: return "沈んだ遺構";
                default: return "遺構";
            }
        }
    }
}
