using System;
using System.Collections.Generic;
using System.Text;

namespace Vastcore.RegionMap
{
    /// <summary>名前の種類。1 つの場所が複数の名前を持てる。</summary>
    public enum RegionNameKind
    {
        /// <summary>記号名（種類の記号と番号・座標で作る。文明が無くても付く）。</summary>
        Designation = 0,

        /// <summary>土地の名（その土地のことばの語根で作る）。</summary>
        Proper = 1,

        /// <summary>古い呼び名（前のことばの語根で作る）。</summary>
        Old = 2,

        /// <summary>人が決めた名前（規則の [決めた名前]）。</summary>
        Decided = 3,
    }

    /// <summary>土地の経緯。付ける名前の種類を決める。</summary>
    public enum RegionLandHistory
    {
        Unexplored = 0,
        Settled = 1,
        Ancient = 2,
    }

    /// <summary>場所に付いた名前 1 つ。</summary>
    [Serializable]
    public sealed class RegionPlaceName
    {
        public RegionNameKind Kind;
        public string Text;

        /// <summary>骨格（音を付け替えても変わらない部分）。例「[高い][山]山地」「〈エリア〉のモノリス」。記号名では型。</summary>
        public string Skeleton;

        /// <summary>由来（使った型、語根と意味、選んだ根拠、つなぎの音の変化）。</summary>
        public string Origin;

        /// <summary>画面に出す短い由来。例「カト（霧）＋ヨサ（山）」「エリア「コテデマ平野」から」「モノリス・座標 横 72 縦 56」。</summary>
        public string Gloss;
    }

    /// <summary>語根の表の 1 行（確認用の一覧に出す）。</summary>
    public sealed class RegionNameRoot
    {
        public string Meaning;
        public string Sound;

        /// <summary>規則に人が書いた語根なら true。seed から作った語根なら false。</summary>
        public bool Written;

        /// <summary>土地の事実に結びつかない語（型の {雰囲気} で使う）なら true。</summary>
        public bool Mood;

        /// <summary>経緯に置く出来事の語（型の {出来事} で使う）なら true。</summary>
        public bool Event;

        /// <summary>この語根を使った名前の数。</summary>
        public int Uses;
    }

    /// <summary>経緯に置いた人物（型の {人名} で使う）。</summary>
    public sealed class RegionNamePerson
    {
        public string Name;
        public string Role;
        public int Uses;
    }

    /// <summary>名前を付けた時の記録（使った音の質感、語根の表、人物、問題）。</summary>
    public sealed class RegionNamingReport
    {
        public string Sound;
        public string OldSound;

        /// <summary>音の質感を、規則の「自動」で seed が選んだなら true。</summary>
        public bool SoundFromSeed;

        /// <summary>地域の経緯（「未踏」「文明あり」「古い文明」）。エリアごとに分かれる時は「混在」。</summary>
        public string History;

        /// <summary>地域の経緯を、規則の「地域ごと」で seed が選んだなら true。</summary>
        public bool HistoryFromSeed;

        /// <summary>未踏の土地にあるが、文明のあるエリアから近くて高いので土地の名も付けた峰の数。</summary>
        public int PeaksNamedFromNearby;
        public readonly List<RegionNameRoot> Roots = new List<RegionNameRoot>();
        public readonly List<RegionNameRoot> OldRoots = new List<RegionNameRoot>();
        public readonly List<RegionNamePerson> People = new List<RegionNamePerson>();
        public readonly List<string> Problems = new List<string>();
    }

    /// <summary>
    /// 規則から地名を作る。順に、(1) 土地の経緯を決める、(2) 場所ごとに骨格（型と意味の並び）を決める、
    /// (3) 音の質感から語根を割り当てる、(4) 名前と由来を組み立てる。
    /// 骨格は音に依らないので、音の質感を付け替えても、どの意味がどの型で並ぶかは変わらない。
    /// 1 つの場所は複数の名前（記号名・土地の名・古い呼び名）を持てる。どれを付けるかは土地の経緯で決まる。
    /// </summary>
    public static class RegionNaming
    {
        private struct Fact
        {
            public string Key;
            public string Meaning;
            public string Why;

            /// <summary>選ばれやすさ。同じ意味の事実を持つ場所が少ないほど大きい（区別になる事実を優先する）。</summary>
            public float Weight;
        }

        private sealed class Slot
        {
            /// <summary>L = 字、N = 類の語、R = 語根、P = 人名、A = エリアの固有名、F = 峰・川の固有名、O = 古い呼び名。</summary>
            public char Type;
            public string Text;
            public Fact Fact;
            public int Person;
            public Place Ref;
        }

        private sealed class Skeleton
        {
            public RegionNamePattern Pattern;
            public readonly List<Slot> Slots = new List<Slot>();
        }

        private sealed class Place
        {
            /// <summary>0 = 地域、1 = エリア、2 = 川、3 = 峰、4 = 湾、5 = 地点。</summary>
            public int Type;
            public int Number;
            public int Key;
            public string KindKey;
            public string FixedKey;
            public string DesignationKey;
            public RegionArea Area;
            public RegionSite Site;
            public RegionFeature Feature;
            public float MapX;
            public float MapY;
            public RegionLandHistory History;
            public bool HasLand;
            public Fact Land;
            public readonly List<Fact> Traits = new List<Fact>();
            public readonly HashSet<string> Conditions = new HashSet<string>();
            public string Noun;
            public string Code;
            public Place AreaPlace;
            public Place NearPeak;
            public Place NearRiver;

            /// <summary>未踏の土地にある峰に土地の名を付けた時の、近くの文明のあるエリアと、そこまでの距離。</summary>
            public Place Lender;
            public float LenderKm;
            public Skeleton Proper;
            public Skeleton Old;
            public string Decided;
            public string FullText;
            public string ProperText;
            public string OldText;
        }

        /// <summary>1 つのことば（音の質感と、意味ごとの語根）。</summary>
        private sealed class Language
        {
            private readonly RegionNamingRules _rules;
            private readonly uint _seed;
            private readonly bool _useWritten;
            private readonly Dictionary<string, string> _sounds = new Dictionary<string, string>();
            private readonly HashSet<string> _taken = new HashSet<string>();
            private readonly List<string[]> _runs = new List<string[]>();
            private readonly HashSet<string> _runKeys = new HashSet<string>();
            private readonly List<string> _done = new List<string>();
            private readonly List<string> _people = new List<string>();
            public readonly RegionSoundProfile Profile;
            public readonly List<string> Problems = new List<string>();

            public Language(RegionNamingRules rules, RegionSoundProfile profile, uint seed, bool useWritten)
            {
                _rules = rules;
                Profile = profile;
                _seed = seed;
                _useWritten = useWritten;
            }

            /// <summary>名前の中で続けて並ぶ語根（意味の並び）を登録する。語根を作る時に、つないだ結果が読めるかを確かめる。</summary>
            public void Watch(string[] meanings)
            {
                if (meanings.Length > 0 && _runKeys.Add(string.Join("|", meanings))) _runs.Add(meanings);
            }

            public void Build()
            {
                if (_useWritten)
                {
                    for (int r = 0; r < _rules.RootOrder.Count; r++)
                    {
                        string written = _rules.Roots[_rules.RootOrder[r]];
                        if (written.Length == 0) continue;
                        _sounds[_rules.RootOrder[r]] = written;
                        _taken.Add(written);
                    }
                    for (int k = 0; k < _runs.Count; k++)
                    {
                        if (!Complete(_runs[k])) continue;
                        string joined = Join(_runs[k], null);
                        if (!Readable(joined, _rules, 1, _rules.NameMax)) Problems.Add("人が決めた語根の並び「" + joined + "」が、読みにくい並びか長さの外");
                        _done.Add(joined);
                    }
                }
                for (int r = 0; r < _rules.RootOrder.Count; r++) Assign(_rules.RootOrder[r]);
                for (int p = 0; p < _rules.People; p++) _people.Add(MakePerson(p));
            }

            public bool IsWritten(string meaning)
            {
                string written;
                return _useWritten && _rules.Roots.TryGetValue(meaning, out written) && written.Length > 0;
            }

            public string Root(string meaning)
            {
                string sound;
                if (_sounds.TryGetValue(meaning, out sound)) return sound;
                Assign(meaning);
                return _sounds[meaning];
            }

            public string Person(int index)
            {
                return index >= 0 && index < _people.Count ? _people[index] : "？";
            }

            /// <summary>意味の並びを語根にしてつなぐ。音が変わった継ぎ目は notes に入る。</summary>
            public string Join(string[] meanings, List<string> notes)
            {
                string text = "";
                for (int m = 0; m < meanings.Length; m++)
                {
                    string root = Root(meanings[m]);
                    if (m == 0)
                    {
                        text = root;
                        continue;
                    }
                    string note;
                    string joined = Profile.Join(text, root, out note);
                    if (note != null && notes != null) notes.Add(text + "＋" + root + "→" + joined + "（" + note + "）");
                    text = joined;
                }
                return text;
            }

            private bool Complete(string[] run)
            {
                for (int m = 0; m < run.Length; m++)
                {
                    if (!_sounds.ContainsKey(run[m])) return false;
                }
                return true;
            }

            private void Assign(string meaning)
            {
                if (_sounds.ContainsKey(meaning)) return;
                int key = StableHash(meaning);
                string firstValid = null;
                var pending = new List<string>();
                for (int attempt = 0; attempt < 200; attempt++)
                {
                    uint local = _seed + (uint)attempt * 40503u;
                    string sound = Make(Profile.RootMin, Profile.RootMax, key, local);
                    if (_taken.Contains(sound) || !Readable(sound, _rules, 1, 99)) continue;
                    if (firstValid == null) firstValid = sound;

                    // この語根で全部そろう並びを、つないで確かめる
                    _sounds[meaning] = sound;
                    pending.Clear();
                    bool fits = true;
                    for (int k = 0; k < _runs.Count && fits; k++)
                    {
                        string[] run = _runs[k];
                        if (Array.IndexOf(run, meaning) < 0 || !Complete(run)) continue;
                        string joined = Join(run, null);
                        if (!Readable(joined, _rules, 1, _rules.NameMax) || _done.Contains(joined) || pending.Contains(joined)
                            || TooClose(joined, _done) || TooClose(joined, pending))
                        {
                            fits = false;
                        }
                        else
                        {
                            pending.Add(joined);
                        }
                    }
                    if (!fits)
                    {
                        _sounds.Remove(meaning);
                        continue;
                    }
                    _taken.Add(sound);
                    _done.AddRange(pending);
                    return;
                }
                // 条件を満たす語根が見つからない時は、語根だけで読めるものを使う
                string fallback = firstValid ?? ("？" + meaning);
                _sounds[meaning] = fallback;
                _taken.Add(fallback);
                Problems.Add("意味「" + meaning + "」の語根「" + fallback + "」で、読みにくい並びか見分けにくい名前が残る");
            }

            private string MakePerson(int index)
            {
                for (int attempt = 0; attempt < 200; attempt++)
                {
                    uint local = _seed + 977u + (uint)attempt * 40503u;
                    string name = Make(_rules.PersonMin, _rules.PersonMax, 50000 + index, local);
                    if (_taken.Contains(name) || !Readable(name, _rules, 1, 99) || TooClose(name, _done) || TooClose(name, _people)) continue;
                    return name;
                }
                return "人物" + (index + 1);
            }

            private string Make(int min, int max, int key, uint local)
            {
                int span = max - min + 1;
                int length = min + Math.Min(span - 1, (int)(RegionNoise.Hash01(key, 1, local) * span));
                var text = new StringBuilder(Pick(Profile.Head, key, 2, local));
                for (int m = 1; m < length; m++) text.Append(Pick(Profile.Body, key, 2 + m, local));
                return text.ToString();
            }
        }

        public static string KindLabel(RegionNameKind kind)
        {
            switch (kind)
            {
                case RegionNameKind.Designation: return "記号名";
                case RegionNameKind.Proper: return "土地の名";
                case RegionNameKind.Old: return "古い呼び名";
                default: return "決めた名前";
            }
        }

        public static string HistoryLabel(RegionLandHistory history)
        {
            return RegionNamingRules.Histories[(int)history];
        }

        /// <summary>地域そのもの・エリア・川・峰・湾・探索地点へ、規則から名前を付ける。記録は data.Naming に入る。</summary>
        public static void Apply(RegionData data, RegionNamingRules rules)
        {
            if (rules == null) rules = RegionNamingRules.Default;
            if (data.Features.Count == 0) RegionFeatureBuilder.Build(data);
            var report = new RegionNamingReport();
            report.Problems.AddRange(rules.Problems);

            RegionSoundProfile sound = rules.Sound(rules.SoundName);
            if (rules.SoundName == RegionNamingRules.AutoSound)
            {
                // 地域（seed）ごとに、使える音の質感から 1 つを選ぶ
                var usable = new List<RegionSoundProfile>();
                for (int s = 0; s < rules.Sounds.Count; s++)
                {
                    if (rules.Sounds[s].Head.Count > 0 && rules.Sounds[s].Body.Count > 0) usable.Add(rules.Sounds[s]);
                }
                if (usable.Count > 0)
                {
                    sound = usable[Math.Min(usable.Count - 1, (int)(RegionNoise.Hash01(data.Spec.Seed, 9173, 0x51ed270bu) * usable.Count))];
                    report.SoundFromSeed = true;
                }
            }
            if (!string.IsNullOrEmpty(data.Spec.NamingSound))
            {
                RegionSoundProfile asked = rules.Sound(data.Spec.NamingSound);
                if (asked != null)
                {
                    sound = asked;
                    report.SoundFromSeed = false;
                }
                else report.Problems.Add("音の質感「" + data.Spec.NamingSound + "」が規則に無い（規則の既定を使う）");
            }
            if (sound == null || sound.Head.Count == 0 || sound.Body.Count == 0) sound = RegionNamingRules.Default.Sounds[0];
            RegionSoundProfile oldSound = rules.Sound(rules.OldSoundName);
            if (oldSound == null || oldSound.Head.Count == 0 || oldSound.Body.Count == 0 || oldSound == sound)
            {
                oldSound = sound;
                for (int s = 0; s < rules.Sounds.Count; s++)
                {
                    if (rules.Sounds[s] != sound && rules.Sounds[s].Head.Count > 0 && rules.Sounds[s].Body.Count > 0) oldSound = rules.Sounds[s];
                }
            }
            report.Sound = sound.Name;
            report.OldSound = oldSound.Name;

            uint seed = unchecked((uint)data.Spec.Seed * 3266489917u + 2654435761u);
            var loans = new Dictionary<RegionFeature, KeyValuePair<RegionArea, float>>();
            AssignHistory(data, rules, report, loans);
            List<Place> places = BuildPlaces(data, rules, loans);

            // (2) 骨格
            var keys = new HashSet<string>();
            for (int p = 0; p < places.Count; p++)
            {
                Place place = places[p];
                string decided;
                if (rules.Fixed.TryGetValue(data.Spec.Seed + "/" + place.FixedKey, out decided))
                {
                    place.Decided = decided;
                    continue;
                }
                List<string> kinds = Kinds(rules, place.History);
                List<RegionNamePattern> patterns = Patterns(rules, place.Type);
                if (kinds.Contains("古い呼び名")) place.Old = Compose(rules, place, patterns, seed ^ 0x9e3779b9u, true, keys);
                if (kinds.Contains("土地の名")) place.Proper = Compose(rules, place, patterns, seed, false, keys);
            }

            // (3) 音
            var language = new Language(rules, sound, unchecked((uint)data.Spec.Seed * 2246822519u + 374761393u), true);
            var oldLanguage = new Language(rules, oldSound, unchecked((uint)data.Spec.Seed * 668265263u + 1013904223u), false);
            for (int p = 0; p < places.Count; p++)
            {
                if (places[p].Proper != null)
                {
                    foreach (string[] run in Runs(places[p].Proper)) language.Watch(run);
                }
                if (places[p].Old != null) oldLanguage.Watch(Meanings(places[p].Old));
            }
            language.Build();
            oldLanguage.Build();
            report.Problems.AddRange(language.Problems);
            report.Problems.AddRange(oldLanguage.Problems);

            // (4) 名前と由来
            var uses = new Dictionary<string, int>();
            var oldUses = new Dictionary<string, int>();
            var personUses = new int[Math.Max(0, rules.People)];
            var texts = new HashSet<string>();
            for (int p = 0; p < places.Count; p++)
            {
                Place place = places[p];
                var names = new List<RegionPlaceName>();
                RegionPlaceName old = null;
                if (place.Old != null)
                {
                    old = RenderOld(place, oldLanguage, oldUses);
                    if (!texts.Add("古:" + old.Text)) old = null;
                    else place.OldText = old.Text;
                }
                if (place.Decided != null)
                {
                    place.FullText = place.Decided;
                    place.ProperText = place.Decided.EndsWith(place.Noun, StringComparison.Ordinal) && place.Decided.Length > place.Noun.Length
                        ? place.Decided.Substring(0, place.Decided.Length - place.Noun.Length)
                        : place.Decided;
                    texts.Add(place.Decided);
                    names.Add(new RegionPlaceName { Kind = RegionNameKind.Decided, Text = place.Decided, Skeleton = "", Origin = "人が決めた名前", Gloss = "人が決めた名前" });
                }
                List<string> kinds = Kinds(rules, place.History);
                for (int k = 0; k < kinds.Count; k++)
                {
                    if (kinds[k] == "土地の名" && place.Decided == null && place.Proper != null && (place.Proper.Slots.Find(s => s.Type == 'O') == null || place.OldText != null))
                    {
                        string own;
                        RegionPlaceName proper = RenderProper(rules, place, language, uses, personUses, out own);
                        if (!texts.Add(proper.Text)) continue;
                        place.FullText = proper.Text;
                        place.ProperText = own;
                        names.Add(proper);
                    }
                    else if (kinds[k] == "古い呼び名" && old != null)
                    {
                        names.Add(old);
                    }
                    else if (kinds[k] == "記号名")
                    {
                        names.Add(Designation(rules, place, data.Spec.Seed, texts, null));
                    }
                }
                if (names.Count == 0) names.Add(Designation(rules, place, data.Spec.Seed, texts, "規則から土地の名を作れなかったので、記号名だけを付けた"));
                WriteBack(place, names);
            }

            for (int r = 0; r < rules.RootOrder.Count; r++)
            {
                string meaning = rules.RootOrder[r];
                int count;
                uses.TryGetValue(meaning, out count);
                report.Roots.Add(new RegionNameRoot
                {
                    Meaning = meaning,
                    Sound = language.Root(meaning),
                    Written = language.IsWritten(meaning),
                    Mood = rules.Mood.Contains(meaning),
                    Event = rules.Events.Contains(meaning),
                    Uses = count,
                });
                if (oldUses.TryGetValue(meaning, out count))
                {
                    report.OldRoots.Add(new RegionNameRoot
                    {
                        Meaning = meaning,
                        Sound = oldLanguage.Root(meaning),
                        Mood = rules.Mood.Contains(meaning),
                        Event = rules.Events.Contains(meaning),
                        Uses = count,
                    });
                }
            }
            for (int i = 0; i < personUses.Length; i++)
            {
                report.People.Add(new RegionNamePerson { Name = language.Person(i), Role = Role(rules, i), Uses = personUses[i] });
            }
            data.Naming = report;
        }

        /// <summary>型を「{部品}」とそれ以外の字の並びに分ける。</summary>
        internal static List<string> Tokens(string template)
        {
            var tokens = new List<string>();
            int at = 0;
            while (at < template.Length)
            {
                int open = template.IndexOf('{', at);
                if (open < 0)
                {
                    tokens.Add(template.Substring(at));
                    break;
                }
                if (open > at) tokens.Add(template.Substring(at, open - at));
                int close = template.IndexOf('}', open);
                if (close < 0)
                {
                    tokens.Add(template.Substring(open));
                    break;
                }
                tokens.Add(template.Substring(open, close - open + 1));
                at = close + 1;
            }
            return tokens;
        }

        // ---- (1) 土地の経緯 ----

        /// <summary>
        /// 経緯を決める。規則の「地域」が「地域ごと」なら seed が 1 つ選び（人が「seed/地域 = …」と書けばそれ）、地域全体をその経緯にする。
        /// 「混在」ならエリアを住みやすい順に並べて分ける。エリアごとに人が決めた経緯は、どちらよりも優先する。
        /// 最後に、未踏の土地にある峰のうち、文明のあるエリアから近くて高いものを「文明あり」にする（loans に相手のエリアと距離が入る）。
        /// </summary>
        private static void AssignHistory(RegionData data, RegionNamingRules rules, RegionNamingReport report, Dictionary<RegionFeature, KeyValuePair<RegionArea, float>> loans)
        {
            int regionIndex = Array.IndexOf(RegionNamingRules.Histories, rules.RegionHistory);
            string decidedRegion;
            if (rules.HistoryFixed.TryGetValue(data.Spec.Seed + "/地域", out decidedRegion))
            {
                regionIndex = Array.IndexOf(RegionNamingRules.Histories, decidedRegion);
            }
            else if (rules.RegionHistory == RegionNamingRules.PerRegion)
            {
                float pick = RegionNoise.Hash01(data.Spec.Seed, 4409, 0x2f6b1d93u);
                regionIndex = pick < rules.AncientShare ? (int)RegionLandHistory.Ancient
                    : (pick < rules.SettledShare ? (int)RegionLandHistory.Settled : (int)RegionLandHistory.Unexplored);
                report.HistoryFromSeed = true;
            }
            report.History = regionIndex >= 0 ? RegionNamingRules.Histories[regionIndex] : RegionNamingRules.Mixed;

            int n = data.Areas.Count;
            var order = new List<RegionArea>(data.Areas);
            order.Sort((a, b) =>
            {
                float ha = Habitability(a);
                float hb = Habitability(b);
                return ha != hb ? hb.CompareTo(ha) : a.Id.CompareTo(b.Id);
            });
            RegionLandHistory highest = RegionLandHistory.Unexplored;
            for (int r = 0; r < n; r++)
            {
                RegionArea area = order[r];
                RegionLandHistory history;
                int fixedIndex = regionIndex;
                if (fixedIndex >= 0)
                {
                    history = (RegionLandHistory)fixedIndex;
                }
                else
                {
                    float position = (r + 0.5f) / n;
                    history = position < rules.AncientShare ? RegionLandHistory.Ancient
                        : (position < rules.SettledShare ? RegionLandHistory.Settled : RegionLandHistory.Unexplored);
                }
                string decided;
                if (rules.HistoryFixed.TryGetValue(data.Spec.Seed + "/エリア" + area.Id, out decided))
                {
                    history = (RegionLandHistory)Array.IndexOf(RegionNamingRules.Histories, decided);
                }
                area.History = history;
                if (history > highest) highest = history;
            }
            for (int s = 0; s < data.Sites.Count; s++)
            {
                RegionArea area = data.AreaById(data.Sites[s].AreaId);
                data.Sites[s].History = area != null ? area.History : RegionLandHistory.Unexplored;
            }
            for (int f = 0; f < data.Features.Count; f++)
            {
                RegionFeature feature = data.Features[f];
                RegionArea area = data.AreaById(feature.AreaId);
                feature.History = feature.Kind == RegionFeatureKind.Region ? highest : (area != null ? area.History : RegionLandHistory.Unexplored);
            }

            // 未踏の土地にある峰でも、文明のあるエリアから近くて高ければ、土地の名も付ける
            if (rules.PeakNameDistance <= 0f) return;
            for (int f = 0; f < data.Features.Count; f++)
            {
                RegionFeature peak = data.Features[f];
                if (peak.Kind != RegionFeatureKind.Peak || peak.History != RegionLandHistory.Unexplored) continue;
                if (peak.HeightMeters < data.MaxHeight * rules.PeakNameHeight) continue;
                float distance;
                RegionArea near = NearestSettledArea(data, peak.MapX, peak.MapY, rules.PeakNameDistance, out distance);
                if (near == null) continue;
                peak.History = RegionLandHistory.Settled;
                loans[peak] = new KeyValuePair<RegionArea, float>(near, distance * data.SizeKm / 100f);
                report.PeaksNamedFromNearby++;
            }
        }

        /// <summary>その位置から reach（地図の座標）以内にある、未踏でないエリアの陸のうち最も近いもの。無ければ null。</summary>
        private static RegionArea NearestSettledArea(RegionData data, float mapX, float mapY, float reach, out float distance)
        {
            const float Step = 0.5f;
            RegionArea best = null;
            distance = reach + Step;
            int steps = (int)Math.Ceiling(reach / Step);
            for (int oy = -steps; oy <= steps; oy++)
            {
                for (int ox = -steps; ox <= steps; ox++)
                {
                    float x = mapX + ox * Step;
                    float y = mapY + oy * Step;
                    if (x < 0f || y < 0f || x > 100f || y > 100f) continue;
                    float d = (float)Math.Sqrt(ox * ox + oy * oy) * Step;
                    if (d > reach || d >= distance) continue;
                    if (data.Height[data.CellFromMap(x, y)] <= 0f) continue;
                    RegionArea area = data.AreaAtMap(x, y);
                    if (area == null || area.History == RegionLandHistory.Unexplored) continue;
                    best = area;
                    distance = d;
                }
            }
            return best;
        }

        /// <summary>
        /// 住みやすさの目安（仮）。平野・盆地・丘陵が高く、雪原・砂漠・山地が低い。草原と森で上がり、標高で下がる。
        /// 経緯を「混在」にした時に、エリアを文明のある側から並べるためだけに使う。
        /// </summary>
        private static float Habitability(RegionArea area)
        {
            float value;
            switch (area.Kind)
            {
                case RegionAreaKind.Plain: value = 1f; break;
                case RegionAreaKind.Basin: value = 0.9f; break;
                case RegionAreaKind.Hills: value = 0.7f; break;
                case RegionAreaKind.Island: value = 0.5f; break;
                case RegionAreaKind.Highland: value = 0.4f; break;
                case RegionAreaKind.Mountains: value = 0.25f; break;
                case RegionAreaKind.Desert: value = 0.15f; break;
                default: value = 0.1f; break;
            }
            switch (area.MainBiome)
            {
                case RegionBiome.Grassland:
                case RegionBiome.Forest:
                    value += 0.15f;
                    break;
                case RegionBiome.Rainforest:
                case RegionBiome.Savanna:
                    value += 0.05f;
                    break;
                case RegionBiome.Tundra:
                case RegionBiome.Snow:
                case RegionBiome.Desert:
                case RegionBiome.Rock:
                    value -= 0.15f;
                    break;
            }
            return value - area.MeanHeight / 4000f;
        }

        private static List<string> Kinds(RegionNamingRules rules, RegionLandHistory history)
        {
            List<string> kinds;
            return rules.NameKinds.TryGetValue(RegionNamingRules.Histories[(int)history], out kinds) ? kinds : DesignationOnly;
        }

        private static readonly List<string> DesignationOnly = new List<string> { "記号名" };

        private static List<RegionNamePattern> Patterns(RegionNamingRules rules, int type)
        {
            switch (type)
            {
                case 0: return rules.RegionPatterns;
                case 1: return rules.AreaPatterns;
                case 2: return rules.RiverPatterns;
                case 3: return rules.PeakPatterns;
                case 4: return rules.BayPatterns;
                default: return rules.SitePatterns;
            }
        }

        // ---- 場所と事実 ----

        private static List<Place> BuildPlaces(RegionData data, RegionNamingRules rules, Dictionary<RegionFeature, KeyValuePair<RegionArea, float>> loans)
        {
            var places = new List<Place>();
            var areaPlaces = new Dictionary<int, Place>();

            float largest = 0f;
            float smallest = float.MaxValue;
            RegionArea widest = null;
            for (int a = 0; a < data.Areas.Count; a++)
            {
                if (data.Areas[a].AreaKm2 > largest)
                {
                    largest = data.Areas[a].AreaKm2;
                    widest = data.Areas[a];
                }
                smallest = Math.Min(smallest, data.Areas[a].AreaKm2);
            }

            RegionFeature regionFeature = data.Features.Find(f => f.Kind == RegionFeatureKind.Region);
            if (regionFeature != null)
            {
                var region = new Place
                {
                    Type = 0, Number = 0, Key = 1, KindKey = "Region", FixedKey = "地域", DesignationKey = "地域",
                    Feature = regionFeature, MapX = 50f, MapY = 50f, History = regionFeature.History,
                };
                if (widest != null)
                {
                    region.HasLand = Land(rules, RegionAreaBuilder.KindNoun(widest.Kind), "最も広いエリアが" + RegionAreaBuilder.KindNoun(widest.Kind), out region.Land);
                    AddBiome(rules, region.Traits, widest.MainBiome, "最も広いエリアで最も広い");
                }
                if (data.LandFraction < 0.4f) Emit(rules, region.Traits, "海が広い", "陸が地図の 4 割未満");
                if (data.LandFraction >= 0.6f) Emit(rules, region.Traits, "陸が広い", "陸が地図の 6 割以上");
                places.Add(region);
            }

            var group = new List<Place>();
            for (int a = 0; a < data.Areas.Count; a++)
            {
                RegionArea area = data.Areas[a];
                var place = new Place
                {
                    Type = 1, Number = area.Id, Key = 100 + area.Id, KindKey = area.Kind.ToString(), FixedKey = "エリア" + area.Id, DesignationKey = "エリア",
                    Area = area, MapX = area.LabelX, MapY = area.LabelY, History = area.History,
                };
                string kind = RegionAreaBuilder.KindNoun(area.Kind);
                place.HasLand = Land(rules, kind, kind, out place.Land);
                AddDirection(rules, place.Traits, area.LabelX, area.LabelY, "エリアの位置");
                if (area.MaxHeight >= data.MaxHeight * 0.7f) Emit(rules, place.Traits, "最高点が高い", "最高点が地域の最高の 7 割以上");
                if (area.MeanHeight < 150f && area.Kind != RegionAreaKind.Island) Emit(rules, place.Traits, "標高が低い", "平均の標高が 150 m 未満");
                AddBiome(rules, place.Traits, area.MainBiome, "最も広い");
                if (data.Areas.Count > 1 && area.AreaKm2 >= largest) Emit(rules, place.Traits, "最も広い", "地域で最も広い");
                if (data.Areas.Count > 1 && area.AreaKm2 <= smallest) Emit(rules, place.Traits, "最も狭い", "地域で最も狭い");
                areaPlaces[area.Id] = place;
                group.Add(place);
            }
            Finish(rules, group, places);

            var rivers = new List<Place>();
            var peaks = new List<Place>();
            var bays = new List<Place>();
            int riverCount = 0;
            int peakCount = 0;
            int bayCount = 0;
            float longest = 0f;
            for (int f = 0; f < data.Features.Count; f++)
            {
                RegionFeature feature = data.Features[f];
                if (feature.Kind == RegionFeatureKind.River)
                {
                    riverCount++;
                    longest = Math.Max(longest, feature.LengthKm);
                }
                if (feature.Kind == RegionFeatureKind.Peak) peakCount++;
                if (feature.Kind == RegionFeatureKind.Bay) bayCount++;
            }
            for (int f = 0; f < data.Features.Count; f++)
            {
                RegionFeature feature = data.Features[f];
                if (feature.Kind == RegionFeatureKind.Region) continue;
                var place = new Place { Number = feature.Id, Feature = feature, MapX = feature.MapX, MapY = feature.MapY, History = feature.History };
                areaPlaces.TryGetValue(feature.AreaId, out place.AreaPlace);
                switch (feature.Kind)
                {
                    case RegionFeatureKind.River:
                        place.Type = 2;
                        place.Key = 300 + feature.Id;
                        place.KindKey = "River";
                        place.FixedKey = "川" + feature.Id;
                        place.DesignationKey = "川";
                        place.HasLand = Land(rules, "川である", "川", out place.Land);
                        AddDirection(rules, place.Traits, feature.MapX, feature.MapY, "本流の中ほど");
                        if (riverCount > 1 && feature.LengthKm >= longest) Emit(rules, place.Traits, "最も長い川", "本流が地域で最も長い（約 " + Math.Round(feature.LengthKm) + " km）");
                        rivers.Add(place);
                        break;
                    case RegionFeatureKind.Peak:
                        place.Type = 3;
                        place.Key = 400 + feature.Id;
                        place.KindKey = "Peak";
                        place.FixedKey = "山" + feature.Id;
                        place.DesignationKey = "山";
                        place.HasLand = Land(rules, "峰である", "峰", out place.Land);
                        AddDirection(rules, place.Traits, feature.MapX, feature.MapY, "頂");
                        KeyValuePair<RegionArea, float> loan;
                        if (loans.TryGetValue(feature, out loan) && areaPlaces.TryGetValue(loan.Key.Id, out place.Lender)) place.LenderKm = loan.Value;
                        if (peakCount > 1 && feature.Id == 1) Emit(rules, place.Traits, "最も高い峰", "地域で最も高い（" + Math.Round(feature.HeightMeters) + " m）");
                        AddBiome(rules, place.Traits, data.BiomeAtMap(feature.MapX, feature.MapY), "頂が");
                        peaks.Add(place);
                        break;
                    default:
                        place.Type = 4;
                        place.Key = 500 + feature.Id;
                        place.KindKey = "Bay";
                        place.FixedKey = "湾" + feature.Id;
                        place.DesignationKey = "湾";
                        place.HasLand = Land(rules, "湾である", "湾", out place.Land);
                        AddDirection(rules, place.Traits, feature.MapX, feature.MapY, "湾の位置");
                        if (bayCount > 1 && feature.Id == 1) Emit(rules, place.Traits, "最も広い", "地域で最も広い湾");
                        bays.Add(place);
                        break;
                }
            }
            Finish(rules, rivers, places);
            Finish(rules, peaks, places);
            Finish(rules, bays, places);

            var sameKind = new Dictionary<int, int>();
            var lead = new Dictionary<int, RegionSite>();
            for (int s = 0; s < data.Sites.Count; s++)
            {
                RegionSite site = data.Sites[s];
                int key = site.AreaId * 16 + (int)site.Kind;
                int count;
                sameKind.TryGetValue(key, out count);
                sameKind[key] = count + 1;
                RegionSite best;
                if (site.AreaId > 0 && (!lead.TryGetValue(site.AreaId, out best) || site.Rank < best.Rank)) lead[site.AreaId] = site;
            }

            group = new List<Place>();
            for (int s = 0; s < data.Sites.Count; s++)
            {
                RegionSite site = data.Sites[s];
                var place = new Place
                {
                    Type = 5, Number = site.Id, Key = 1000 + site.Id, KindKey = site.Kind.ToString(), FixedKey = "地点" + site.Id, DesignationKey = "地点",
                    Site = site, MapX = site.MapX, MapY = site.MapY, History = site.History,
                };
                areaPlaces.TryGetValue(site.AreaId, out place.AreaPlace);
                switch (site.Kind)
                {
                    case RegionSiteKind.Monolith: place.HasLand = Land(rules, "山頂に立つ", "山頂に立つ", out place.Land); break;
                    case RegionSiteKind.Arch: place.HasLand = Land(rules, "海のそばに立つ", "海のそばに立つ", out place.Land); break;
                    case RegionSiteKind.Ring: place.HasLand = Land(rules, "乾いた土地に立つ", "乾いた土地に立つ", out place.Land); break;
                    case RegionSiteKind.Tower: place.HasLand = Land(rules, "丘に立つ", "丘に立つ", out place.Land); break;
                    case RegionSiteKind.Dome: place.HasLand = Land(rules, "雪原に立つ", "雪原に立つ", out place.Land); break;
                    case RegionSiteKind.Terrace: place.HasLand = Land(rules, "川のそばに立つ", "川のそばに立つ", out place.Land); break;
                    default: place.HasLand = Land(rules, "浅い海に沈む", "浅い海に沈む", out place.Land); break;
                }
                AddDirection(rules, place.Traits, site.MapX, site.MapY, "地点の位置");
                if (site.Kind == RegionSiteKind.Sunken) Emit(rules, place.Traits, "海の中にある", "海の中");
                else if (site.HeightMeters >= data.MaxHeight * 0.5f) Emit(rules, place.Traits, "最高点が高い", "標高が地域の最高の 5 割以上");
                else if (site.HeightMeters < 60f) Emit(rules, place.Traits, "標高が低い", "標高 60 m 未満");
                AddBiome(rules, place.Traits, site.Biome, "立つ場所が");

                if (site.AreaId == 0) place.Conditions.Add("エリアなし");
                else place.Conditions.Add(sameKind[site.AreaId * 16 + (int)site.Kind] == 1 ? "種類がエリアに1つ" : "種類がエリアに複数");
                RegionSite first;
                if (lead.TryGetValue(site.AreaId, out first) && first == site) place.Conditions.Add("エリアの代表");

                place.NearPeak = Nearest(peaks, site.MapX, site.MapY, 2.5f);
                float riverDistance = 2f;
                for (int r = 0; r < rivers.Count; r++)
                {
                    RegionFeature river = rivers[r].Feature;
                    for (int i = 0; i < river.PathX.Length; i++)
                    {
                        float dx = river.PathX[i] - site.MapX;
                        float dy = river.PathY[i] - site.MapY;
                        float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (distance < riverDistance)
                        {
                            riverDistance = distance;
                            place.NearRiver = rivers[r];
                        }
                    }
                }
                group.Add(place);
            }
            Finish(rules, group, places);
            for (int p = 0; p < places.Count; p++)
            {
                string word;
                places[p].Noun = rules.Nouns.TryGetValue(places[p].KindKey, out word) ? word : places[p].KindKey;
                places[p].Code = rules.Codes.TryGetValue(places[p].KindKey, out word) ? word : places[p].KindKey;
            }
            return places;
        }

        /// <summary>同じ種類の場所の中で事実の重みを付け、全体の並びへ足す。</summary>
        private static void Finish(RegionNamingRules rules, List<Place> group, List<Place> places)
        {
            var counts = new Dictionary<string, int>();
            for (int p = 0; p < group.Count; p++)
            {
                for (int f = 0; f < group[p].Traits.Count; f++)
                {
                    int count;
                    counts.TryGetValue(group[p].Traits[f].Meaning, out count);
                    counts[group[p].Traits[f].Meaning] = count + 1;
                }
            }
            for (int p = 0; p < group.Count; p++)
            {
                Place place = group[p];
                for (int f = 0; f < place.Traits.Count; f++)
                {
                    Fact fact = place.Traits[f];
                    fact.Weight = 1f / counts[fact.Meaning];
                    place.Traits[f] = fact;
                }
            }
            places.AddRange(group);
        }

        private static Place Nearest(List<Place> candidates, float mapX, float mapY, float within)
        {
            Place nearest = null;
            for (int c = 0; c < candidates.Count; c++)
            {
                float dx = candidates[c].MapX - mapX;
                float dy = candidates[c].MapY - mapY;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                if (distance >= within) continue;
                within = distance;
                nearest = candidates[c];
            }
            return nearest;
        }

        private static void Emit(RegionNamingRules rules, List<Fact> into, string key, string why)
        {
            List<string> meanings;
            if (!rules.Facts.TryGetValue(key, out meanings)) return;
            for (int m = 0; m < meanings.Count; m++) into.Add(new Fact { Key = key, Meaning = meanings[m], Why = why });
        }

        private static bool Land(RegionNamingRules rules, string key, string why, out Fact fact)
        {
            List<string> meanings;
            if (!rules.Facts.TryGetValue(key, out meanings) || meanings.Count == 0)
            {
                fact = default(Fact);
                return false;
            }
            fact = new Fact { Key = key, Meaning = meanings[0], Why = why };
            return true;
        }

        private static void AddDirection(RegionNamingRules rules, List<Fact> traits, float mapX, float mapY, string what)
        {
            int col = mapX < 38f ? 0 : (mapX < 62f ? 1 : 2);
            int row = mapY < 38f ? 0 : (mapY < 62f ? 1 : 2);
            if (row == 0) Emit(rules, traits, "北にある", what + "が地域の北");
            if (row == 2) Emit(rules, traits, "南にある", what + "が地域の南");
            if (col == 0) Emit(rules, traits, "西にある", what + "が地域の西");
            if (col == 2) Emit(rules, traits, "東にある", what + "が地域の東");
            if (row == 1 && col == 1) Emit(rules, traits, "中央にある", what + "が地域の中央");
        }

        private static void AddBiome(RegionNamingRules rules, List<Fact> traits, RegionBiome biome, string what)
        {
            switch (biome)
            {
                case RegionBiome.Snow:
                case RegionBiome.Tundra:
                    Emit(rules, traits, "雪とツンドラが広い", what + "雪・ツンドラ");
                    break;
                case RegionBiome.Desert:
                case RegionBiome.Savanna:
                    Emit(rules, traits, "砂漠とサバンナが広い", what + "砂漠・サバンナ");
                    break;
                case RegionBiome.Forest:
                case RegionBiome.Taiga:
                case RegionBiome.Rainforest:
                    Emit(rules, traits, "森が広い", what + "森");
                    break;
            }
        }

        // ---- (2) 骨格 ----

        private static bool HasProper(Place place)
        {
            return place != null && (place.Proper != null || place.Decided != null);
        }

        /// <summary>
        /// 条件に合う型から重みで 1 つ選び、部品へ意味を入れて骨格にする。音は使わない。
        /// 同じ骨格・同じ固有の部分が既にあれば、別の型や意味で作り直す。old は古い呼び名の骨格（語根の並びだけ）。
        /// </summary>
        private static Skeleton Compose(RegionNamingRules rules, Place place, List<RegionNamePattern> patterns, uint seed, bool old, HashSet<string> keys)
        {
            var events = new List<string>();
            for (int e = 0; e < rules.Events.Count; e++)
            {
                List<string> needs = rules.EventNeeds[rules.Events[e]];
                bool fits = needs.Count == 0 || (place.HasLand && needs.Contains(place.Land.Meaning));
                for (int f = 0; f < place.Traits.Count && !fits; f++) fits = needs.Contains(place.Traits[f].Meaning);
                if (fits) events.Add(rules.Events[e]);
            }

            var usable = new List<RegionNamePattern>();
            float total = 0f;
            for (int p = 0; p < patterns.Count; p++)
            {
                RegionNamePattern pattern = patterns[p];
                bool always = pattern.Condition.Length == 0 || pattern.Condition == "いつでも";
                if (!always && !(pattern.Condition == "古い文明" ? place.History == RegionLandHistory.Ancient : place.Conditions.Contains(pattern.Condition))) continue;
                bool fits = true;
                List<string> tokens = Tokens(pattern.Template);
                for (int t = 0; t < tokens.Count && fits; t++)
                {
                    switch (tokens[t])
                    {
                        case "{特徴}": fits = place.Traits.Count > 0; break;
                        case "{地形}": fits = place.HasLand; break;
                        case "{雰囲気}": fits = rules.Mood.Count > 0; break;
                        case "{人名}": fits = !old && rules.People > 0; break;
                        case "{出来事}": fits = !old && events.Count > 0; break;
                        case "{古名}": fits = !old && place.Old != null; break;
                        case "{エリア}": fits = !old && HasProper(place.AreaPlace); break;
                        case "{山}": fits = !old && HasProper(place.NearPeak); break;
                        case "{川}": fits = !old && HasProper(place.NearRiver); break;
                    }
                }
                if (!fits) continue;
                usable.Add(pattern);
                total += pattern.Weight;
            }
            if (usable.Count == 0) return null;

            int key = place.Key;
            for (int attempt = 0; attempt < 96; attempt++)
            {
                uint local = seed + (uint)attempt * 40503u;
                float roll = RegionNoise.Hash01(key, 1, local) * total;
                RegionNamePattern chosen = usable[usable.Count - 1];
                for (int p = 0; p < usable.Count; p++)
                {
                    roll -= usable[p].Weight;
                    if (roll < 0f)
                    {
                        chosen = usable[p];
                        break;
                    }
                }

                var skeleton = new Skeleton { Pattern = chosen };
                List<string> tokens = Tokens(chosen.Template);
                for (int t = 0; t < tokens.Count; t++)
                {
                    string token = tokens[t];
                    if (token[0] != '{')
                    {
                        if (!old) skeleton.Slots.Add(new Slot { Type = 'L', Text = token });
                        continue;
                    }
                    switch (token)
                    {
                        case "{類}":
                        case "{構造物}":
                            if (!old) skeleton.Slots.Add(new Slot { Type = 'N', Text = place.Noun });
                            break;
                        case "{地形}": skeleton.Slots.Add(new Slot { Type = 'R', Fact = place.Land }); break;
                        case "{特徴}": skeleton.Slots.Add(new Slot { Type = 'R', Fact = PickFact(place.Traits, key, 10 + t, local) }); break;
                        case "{雰囲気}":
                            skeleton.Slots.Add(new Slot { Type = 'R', Fact = new Fact { Key = "雰囲気", Meaning = rules.Mood[Index(rules.Mood.Count, key, 20 + t, local)] } });
                            break;
                        case "{出来事}":
                            skeleton.Slots.Add(new Slot { Type = 'R', Fact = new Fact { Key = "出来事", Meaning = events[Index(events.Count, key, 30 + t, local)] } });
                            break;
                        case "{人名}": skeleton.Slots.Add(new Slot { Type = 'P', Person = Index(rules.People, key, 40 + t, local) }); break;
                        case "{古名}": skeleton.Slots.Add(new Slot { Type = 'O' }); break;
                        case "{エリア}": skeleton.Slots.Add(new Slot { Type = 'A', Ref = place.AreaPlace }); break;
                        case "{山}": skeleton.Slots.Add(new Slot { Type = 'F', Ref = place.NearPeak }); break;
                        case "{川}": skeleton.Slots.Add(new Slot { Type = 'F', Ref = place.NearRiver }); break;
                    }
                }

                // 同じ意味を 2 回並べない
                bool repeated = false;
                for (int a = 0; a < skeleton.Slots.Count && !repeated; a++)
                {
                    for (int b = a + 1; b < skeleton.Slots.Count; b++)
                    {
                        if (skeleton.Slots[a].Type == 'R' && skeleton.Slots[b].Type == 'R' && skeleton.Slots[a].Fact.Meaning == skeleton.Slots[b].Fact.Meaning) repeated = true;
                    }
                }
                if (repeated) continue;

                string full = (old ? "古:" : "名:") + Describe(skeleton, place, true);
                if (keys.Contains(full)) continue;
                // エリア・川・峰・湾・地域は、固有の部分（類の語を除いた部分）も重ならないようにする（{エリア} などで参照されるため）
                string own = (old ? "古固有:" : "固有:") + Describe(skeleton, place, false);
                if (place.Type != 5 && keys.Contains(own)) continue;
                keys.Add(full);
                if (place.Type != 5) keys.Add(own);
                return skeleton;
            }
            return null;
        }

        /// <summary>骨格を文字にする。withNoun が false なら、類の語とその直前の字（「の」）を除く。</summary>
        private static string Describe(Skeleton skeleton, Place place, bool withNoun)
        {
            var text = new StringBuilder();
            for (int s = 0; s < skeleton.Slots.Count; s++)
            {
                Slot slot = skeleton.Slots[s];
                bool beforeNoun = s + 1 < skeleton.Slots.Count && skeleton.Slots[s + 1].Type == 'N';
                switch (slot.Type)
                {
                    case 'L':
                        if (withNoun || !beforeNoun) text.Append(slot.Text);
                        break;
                    case 'N':
                        if (withNoun) text.Append(slot.Text);
                        break;
                    case 'R': text.Append('[').Append(slot.Fact.Meaning).Append(']'); break;
                    case 'P': text.Append("〈人名").Append(slot.Person + 1).Append('〉'); break;
                    case 'O': text.Append("〈古名〉"); break;
                    case 'A': text.Append("〈エリア").Append(slot.Ref.Number).Append('〉'); break;
                    default: text.Append(slot.Ref.Type == 3 ? "〈山" : "〈川").Append(slot.Ref.Number).Append('〉'); break;
                }
            }
            return text.ToString();
        }

        /// <summary>骨格の中で、続けて並ぶ語根の意味の並び。</summary>
        private static List<string[]> Runs(Skeleton skeleton)
        {
            var runs = new List<string[]>();
            var run = new List<string>();
            for (int s = 0; s <= skeleton.Slots.Count; s++)
            {
                if (s < skeleton.Slots.Count && skeleton.Slots[s].Type == 'R')
                {
                    run.Add(skeleton.Slots[s].Fact.Meaning);
                    continue;
                }
                if (run.Count > 0) runs.Add(run.ToArray());
                run.Clear();
            }
            return runs;
        }

        private static string[] Meanings(Skeleton skeleton)
        {
            var list = new List<string>();
            for (int s = 0; s < skeleton.Slots.Count; s++)
            {
                if (skeleton.Slots[s].Type == 'R') list.Add(skeleton.Slots[s].Fact.Meaning);
            }
            return list.ToArray();
        }

        // ---- (4) 名前と由来 ----

        private static RegionPlaceName RenderProper(RegionNamingRules rules, Place place, Language language, Dictionary<string, int> uses, int[] personUses, out string own)
        {
            Skeleton skeleton = place.Proper;
            var shown = new StringBuilder();
            var proper = new StringBuilder();
            var parts = new List<string>();
            var reasons = new List<string>();
            var notes = new List<string>();
            var joins = new List<string>();
            var gloss = new List<string>();
            for (int s = 0; s < skeleton.Slots.Count; s++)
            {
                Slot slot = skeleton.Slots[s];
                bool beforeNoun = s + 1 < skeleton.Slots.Count && skeleton.Slots[s + 1].Type == 'N';
                string piece;
                switch (slot.Type)
                {
                    case 'L':
                        shown.Append(slot.Text);
                        if (!beforeNoun) proper.Append(slot.Text);
                        continue;
                    case 'N':
                        shown.Append(slot.Text);
                        continue;
                    case 'R':
                        var run = new List<string>();
                        var runGloss = new List<string>();
                        int end = s;
                        while (end < skeleton.Slots.Count && skeleton.Slots[end].Type == 'R')
                        {
                            Fact fact = skeleton.Slots[end].Fact;
                            run.Add(fact.Meaning);
                            parts.Add(language.Root(fact.Meaning) + "（" + fact.Meaning + "）");
                            runGloss.Add(language.Root(fact.Meaning) + "（" + (fact.Key == "出来事" ? "出来事・" : "") + fact.Meaning + "）");
                            if (fact.Key == "出来事") reasons.Add("経緯に置いた出来事「" + fact.Meaning + "」に由来");
                            else if (fact.Why != null && fact.Why != fact.Meaning) reasons.Add(fact.Meaning + "＝" + fact.Why);
                            Count(uses, fact.Meaning);
                            end++;
                        }
                        s = end - 1;
                        piece = language.Join(run.ToArray(), joins);
                        gloss.Add(string.Join("＋", runGloss.ToArray()));
                        break;
                    case 'P':
                        piece = language.Person(slot.Person);
                        parts.Add(piece + "（人名）");
                        string role = Role(rules, slot.Person);
                        notes.Add("経緯に置いた人物「" + piece + "」" + (role != null ? "（" + role + "）" : "") + "に由来");
                        gloss.Add("人物「" + piece + "」" + (role != null ? "（" + role + "）" : "") + "から");
                        if (slot.Person < personUses.Length) personUses[slot.Person]++;
                        break;
                    case 'O':
                        piece = place.OldText;
                        notes.Add("古い呼び名「" + piece + "」に、今のことばの類の語を付けた");
                        gloss.Add("古い呼び名「" + piece + "」から");
                        break;
                    case 'A':
                        piece = slot.Ref.ProperText;
                        notes.Add("エリア「" + slot.Ref.FullText + "」の固有名から" + (place.Conditions.Contains("エリアの代表") ? "（このエリアで種類に最も合う地点）" : ""));
                        gloss.Add("エリア「" + slot.Ref.FullText + "」から");
                        break;
                    default:
                        piece = slot.Ref.ProperText;
                        notes.Add((slot.Ref.Type == 3 ? "そばの峰「" : "そばの川「") + slot.Ref.FullText + "」の固有名から");
                        gloss.Add((slot.Ref.Type == 3 ? "峰「" : "川「") + slot.Ref.FullText + "」から");
                        break;
                }
                shown.Append(piece);
                proper.Append(piece);
            }

            var origin = new StringBuilder("型「").Append(skeleton.Pattern.Template).Append("」");
            if (parts.Count > 0) origin.Append("。").Append(string.Join("＋", parts.ToArray()));
            if (notes.Count > 0) origin.Append("。").Append(string.Join("。", notes.ToArray()));
            if (reasons.Count > 0) origin.Append("。").Append(string.Join("、", reasons.ToArray()));
            if (joins.Count > 0) origin.Append("。つなぎの音: ").Append(string.Join("、", joins.ToArray()));
            if (place.Lender != null && place.Feature != null)
            {
                origin.Append("。未踏の土地にあるが、文明のあるエリア「").Append(place.Lender.FullText).Append("」から約 ")
                    .Append(Math.Max(1, (int)Math.Round(place.LenderKm))).Append(" km の高い峰（").Append((int)Math.Round(place.Feature.HeightMeters)).Append(" m）なので、土地の名も付けた");
            }
            own = proper.ToString();
            return new RegionPlaceName
            {
                Kind = RegionNameKind.Proper, Text = shown.ToString(), Skeleton = Describe(skeleton, place, true), Origin = origin.ToString(),
                Gloss = string.Join("、", gloss.ToArray()),
            };
        }

        private static RegionPlaceName RenderOld(Place place, Language language, Dictionary<string, int> uses)
        {
            var parts = new List<string>();
            var reasons = new List<string>();
            var joins = new List<string>();
            string[] meanings = Meanings(place.Old);
            for (int s = 0; s < place.Old.Slots.Count; s++)
            {
                Fact fact = place.Old.Slots[s].Fact;
                parts.Add(language.Root(fact.Meaning) + "（" + fact.Meaning + "）");
                if (fact.Why != null && fact.Why != fact.Meaning) reasons.Add(fact.Meaning + "＝" + fact.Why);
                Count(uses, fact.Meaning);
            }
            var origin = new StringBuilder("前のことば（音の質感「").Append(language.Profile.Name).Append("」）の語根。").Append(string.Join("＋", parts.ToArray()));
            if (reasons.Count > 0) origin.Append("。").Append(string.Join("、", reasons.ToArray()));
            string text = language.Join(meanings, joins);
            if (joins.Count > 0) origin.Append("。つなぎの音: ").Append(string.Join("、", joins.ToArray()));
            return new RegionPlaceName
            {
                Kind = RegionNameKind.Old, Text = text, Skeleton = Describe(place.Old, place, true), Origin = origin.ToString(),
                Gloss = "前のことば: " + string.Join("＋", parts.ToArray()),
            };
        }

        private static RegionPlaceName Designation(RegionNamingRules rules, Place place, int seed, HashSet<string> texts, string remark)
        {
            string template;
            if (!rules.Designations.TryGetValue(place.DesignationKey, out template)) template = "{種}-{番}";
            int x = Math.Max(0, Math.Min(99, (int)Math.Round(place.MapX)));
            int y = Math.Max(0, Math.Min(99, (int)Math.Round(place.MapY)));
            var text = new StringBuilder();
            var notes = new List<string>();
            List<string> tokens = Tokens(template);
            bool coordinates = false;
            for (int t = 0; t < tokens.Count; t++)
            {
                switch (tokens[t])
                {
                    case "{種}":
                        text.Append(place.Code);
                        notes.Add(place.Code + "＝" + place.Noun);
                        break;
                    case "{番}":
                        text.Append(place.Number.ToString("00"));
                        notes.Add(place.Number.ToString("00") + "＝番号");
                        break;
                    case "{横}":
                        text.Append(x.ToString("00"));
                        coordinates = true;
                        break;
                    case "{縦}":
                        text.Append(y.ToString("00"));
                        coordinates = true;
                        break;
                    case "{seed}":
                        text.Append(seed);
                        notes.Add(seed + "＝地域の seed");
                        break;
                    default: text.Append(tokens[t]); break;
                }
            }
            if (coordinates) notes.Add("数字＝地図の座標（横 " + x + "・縦 " + y + "）");
            string gloss = place.Noun + (coordinates ? "・座標 横 " + x + " 縦 " + y : (place.Type == 0 ? "・seed " + seed : "・番号 " + place.Number));
            string made = text.ToString();
            string unique = made;
            for (int suffix = 0; !texts.Add(unique); suffix++) unique = made + (char)('b' + suffix);
            string origin = "型「" + template + "」。" + string.Join("、", notes.ToArray());
            if (remark != null) origin += "。" + remark;
            return new RegionPlaceName { Kind = RegionNameKind.Designation, Text = unique, Skeleton = template, Origin = origin, Gloss = gloss };
        }

        private static void WriteBack(Place place, List<RegionPlaceName> names)
        {
            RegionPlaceName first = names[0];
            if (place.FullText == null) place.FullText = first.Text;
            if (place.Area != null)
            {
                place.Area.Names = names;
                place.Area.Name = first.Text;
                place.Area.NameOrigin = first.Origin;
            }
            else if (place.Site != null)
            {
                place.Site.Names = names;
                place.Site.Feature = first.Text;
                place.Site.Name = first.Text;
                place.Site.NameOrigin = first.Origin;
            }
            else
            {
                place.Feature.Names = names;
                place.Feature.Name = first.Text;
                place.Feature.NameOrigin = first.Origin;
            }
        }

        private static string Role(RegionNamingRules rules, int person)
        {
            return rules.Roles.Count > 0 ? rules.Roles[person % rules.Roles.Count] : null;
        }

        private static void Count(Dictionary<string, int> uses, string meaning)
        {
            int count;
            uses.TryGetValue(meaning, out count);
            uses[meaning] = count + 1;
        }

        private static Fact PickFact(List<Fact> facts, int key, int slot, uint seed)
        {
            float total = 0f;
            for (int f = 0; f < facts.Count; f++) total += facts[f].Weight > 0f ? facts[f].Weight : 1f;
            float roll = RegionNoise.Hash01(key, slot, seed) * total;
            for (int f = 0; f < facts.Count; f++)
            {
                roll -= facts[f].Weight > 0f ? facts[f].Weight : 1f;
                if (roll < 0f) return facts[f];
            }
            return facts[facts.Count - 1];
        }

        /// <summary>既にある名前と同じか、同じ長さで 1 字だけ違う（見分けにくい）か。</summary>
        private static bool TooClose(string name, List<string> used)
        {
            for (int u = 0; u < used.Count; u++)
            {
                string other = used[u];
                if (other.Length != name.Length) continue;
                int different = 0;
                for (int i = 0; i < name.Length && different < 2; i++)
                {
                    if (name[i] != other[i]) different++;
                }
                if (different < 2) return true;
            }
            return false;
        }

        private static string Pick(List<string> list, int key, int slot, uint seed)
        {
            return list[Index(list.Count, key, slot, seed)];
        }

        private static int Index(int count, int key, int slot, uint seed)
        {
            int index = (int)(RegionNoise.Hash01(key, slot, seed) * count);
            return index >= count ? count - 1 : index;
        }

        /// <summary>実行する環境が違っても同じ値になる文字列のハッシュ（string.GetHashCode は実行ごとに変わる）。</summary>
        private static int StableHash(string text)
        {
            uint hash = 2166136261u;
            for (int i = 0; i < text.Length; i++) hash = unchecked((hash ^ text[i]) * 16777619u);
            return (int)(hash & 0x7fffffff);
        }

        /// <summary>読める並びか（長さ、同じ字の連続、「ン」「ー」「ッ」の重なり、避ける並び）。</summary>
        private static bool Readable(string name, RegionNamingRules rules, int min, int max)
        {
            if (name.Length < min || name.Length > max) return false;
            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                char p = name[i - 1];
                if (c == p) return false;
                if (c == 'ー' && (p == 'ン' || p == 'ッ')) return false;
                if (c == 'ッ' && (p == 'ー' || p == 'ン')) return false;
                if (c == 'ン' && (p == 'ー' || p == 'ッ')) return false;
            }
            char last = name[name.Length - 1];
            if (last == 'ー' || last == 'ッ') return false;
            for (int b = 0; b < rules.Blocked.Count; b++)
            {
                if (name.IndexOf(rules.Blocked[b], StringComparison.Ordinal) >= 0) return false;
            }
            return true;
        }
    }
}
