using System;
using System.Collections.Generic;
using System.Text;

namespace Vastcore.RegionMap
{
    /// <summary>名付けの型の 1 行（重み・型・条件）。</summary>
    public sealed class RegionNamePattern
    {
        public float Weight = 1f;

        /// <summary>例「{特徴}{地形}{類}」「{エリア}の{構造物}」。</summary>
        public string Template;

        /// <summary>使える場面。空は「いつでも」。</summary>
        public string Condition = "";

        public int Line;
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
    }

    /// <summary>
    /// 命名の規則。音（語根を作る材料）、語根（意味と音の対応）、型（語根の並べ方と使える場面）、
    /// 類の語（山地・塔など）、人が決めた名前を持つ。テキストから読む（<see cref="Parse"/>）。
    /// 既定の中身（<see cref="DefaultText"/>）は仕組みを動かすための仮で、音・語・型の採否は人が決める。
    /// </summary>
    public sealed class RegionNamingRules
    {
        public readonly List<string> Head = new List<string>();
        public readonly List<string> Body = new List<string>();
        public int RootMin = 2;
        public int RootMax = 2;
        public int NameMin = 3;
        public int NameMax = 6;
        public readonly List<string> Blocked = new List<string>();
        public readonly Dictionary<string, string> Roots = new Dictionary<string, string>();
        public readonly List<string> RootOrder = new List<string>();
        public readonly List<string> Mood = new List<string>();
        public readonly List<RegionNamePattern> AreaPatterns = new List<RegionNamePattern>();
        public readonly List<RegionNamePattern> SitePatterns = new List<RegionNamePattern>();
        public readonly Dictionary<string, string> Nouns = new Dictionary<string, string>();
        public readonly Dictionary<string, string> Fixed = new Dictionary<string, string>();

        /// <summary>読み込みで見つけた問題（「行 12: …」）。空なら問題なし。</summary>
        public readonly List<string> Problems = new List<string>();

        /// <summary>土地の事実から選ぶ意味。規則の [語根] に無ければ seed から語根を作る。</summary>
        public static readonly string[] FactMeanings =
        {
            "山", "丘", "野", "谷", "島", "川", "海", "岸", "雪", "砂", "森",
            "高い", "低い", "深い", "白い", "赤い", "緑の", "乾いた", "大きい", "小さい", "北", "南", "東", "西", "中",
        };

        public static readonly string[] AreaSlots = { "特徴", "地形", "雰囲気", "類" };
        public static readonly string[] SiteSlots = { "特徴", "地形", "雰囲気", "エリア", "構造物" };
        public static readonly string[] Conditions = { "", "いつでも", "種類がエリアに1つ", "種類がエリアに複数", "エリアなし" };

        public const string DefaultText = @"# VastCore 命名の規則（仕組みを動かすための仮の中身。音・語・型は人が書き換える）
# 行頭の # は注記。「名前 = 値」の形で書く。並びは空白で区切る。

[音]
# 語根は「頭」の音 1 つに「続き」の音をつないで作る。
頭 = ア イ ウ エ オ カ キ ク ケ コ サ シ ス セ ソ タ テ ト ナ ニ ネ ノ ハ ヒ フ ホ マ ミ ム メ モ ヤ ユ ヨ ラ リ ル レ ロ ワ ガ ギ グ ゴ ザ ジ ゼ ダ デ ド バ ビ ベ ボ
続き = ア イ ウ エ オ カ キ ク ケ コ サ シ ス セ ソ タ テ ト ナ ニ ネ ノ マ ミ ム メ モ ラ リ ル レ ロ ン ガ グ ザ ジ ダ ド バ ビ
語根の長さ = 2-2
固有名の長さ = 3-6
避ける並び = バカ アホ シネ クソ ウンコ チンコ マンコ エロ ハゲ デブ ゲロ キモ ウザ コロス シヌ ガン ゴミ ザコ ボケ カス サイレン バット
# 日本語の語として読めて、表の意味と食い違って見える並び
避ける並び = シロ クロ アカ アオ ヤマ カワ ウミ モリ ユキ スナ シマ オカ タニ キタ ニシ ナカ タカ ヒク イシ コケ クサ ハナ ソラ ホシ ツキ カゼ カゲ キリ ウタ テツ ムシ クモ アメ ヒト イヌ ネコ トリ サル ウシ ウマ

[語根]
# 意味 = 語根。右を空にすると seed から作る（同じ seed なら同じ語根）。人が決めた語根は右に書く。
# 土地の事実から選ぶ意味
山 =
丘 =
野 =
谷 =
島 =
川 =
海 =
岸 =
雪 =
砂 =
森 =
高い =
低い =
深い =
白い =
赤い =
緑の =
乾いた =
大きい =
小さい =
北 =
南 =
東 =
西 =
中 =

[雰囲気]
# 土地の事実に結びつかない語。型の {雰囲気} で使う。行を足し引きしてよい。
霧 =
星 =
月 =
風 =
影 =
光 =
歌 =
鉄 =
古い =
静かな =
眠る =
遠い =

[エリアの型]
# 重み : 型
# {特徴}=そのエリアの事実から選ぶ語根 {地形}=エリアの種類の語根 {雰囲気}=上の表から選ぶ語根 {類}=山地・平野など
4 : {特徴}{地形}{類}
2 : {雰囲気}{地形}{類}
1 : {特徴}{雰囲気}{類}

[地点の型]
# 重み : 型 : 条件
# {エリア}=属するエリアの固有名 {構造物}=塔・環など
# 条件 = いつでも / 種類がエリアに1つ / 種類がエリアに複数 / エリアなし
5 : {エリア}の{構造物} : 種類がエリアに1つ
3 : {特徴}{雰囲気}の{構造物} : いつでも
3 : {雰囲気}{地形}の{構造物} : いつでも
2 : {特徴}{地形}の{構造物} : いつでも

[類の語]
Mountains = 山地
Highland = 高地
Hills = 丘陵
Plain = 平野
Basin = 盆地
Island = 島
Snowfield = 雪原
Desert = 砂漠
Monolith = モノリス
Arch = アーチ
Ring = 環
Tower = 塔
Dome = ドーム
Terrace = 段丘
Sunken = 沈んだ遺構

[決めた名前]
# 人が決めた名前。「seed/エリア番号 = 名前」「seed/地点番号 = 名前」。例: 7/エリア3 = …
";

        private static RegionNamingRules _default;

        /// <summary>既定の規則（<see cref="DefaultText"/> を読んだもの）。</summary>
        public static RegionNamingRules Default
        {
            get { return _default ?? (_default = Parse(DefaultText)); }
        }

        /// <summary>規則のテキストを読む。null か空なら既定。問題は <see cref="Problems"/> に入る。</summary>
        public static RegionNamingRules Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return Default;
            var rules = new RegionNamingRules();
            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            string section = null;
            for (int l = 0; l < lines.Length; l++)
            {
                string line = lines[l].Replace('＝', '=').Replace('：', ':').Replace('　', ' ').Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                if (line[0] == '[')
                {
                    section = line.Trim('[', ']', ' ');
                    continue;
                }
                int number = l + 1;
                switch (section)
                {
                    case "音": rules.ReadSound(line, number); break;
                    case "語根": rules.ReadRoot(line, number, false); break;
                    case "雰囲気": rules.ReadRoot(line, number, true); break;
                    case "エリアの型": rules.ReadPattern(line, number, rules.AreaPatterns, AreaSlots); break;
                    case "地点の型": rules.ReadPattern(line, number, rules.SitePatterns, SiteSlots); break;
                    case "類の語": rules.ReadPair(line, number, rules.Nouns, false); break;
                    case "決めた名前": rules.ReadPair(line, number, rules.Fixed, false); break;
                    default: rules.Problems.Add("行 " + number + ": 知らない節「" + section + "」の中の行"); break;
                }
            }
            if (rules.Head.Count == 0) rules.Problems.Add("[音] に「頭」が無い");
            if (rules.Body.Count == 0) rules.Problems.Add("[音] に「続き」が無い");
            if (rules.AreaPatterns.Count == 0) rules.Problems.Add("[エリアの型] が空");
            if (rules.SitePatterns.Count == 0) rules.Problems.Add("[地点の型] が空");
            bool always = false;
            for (int p = 0; p < rules.SitePatterns.Count; p++)
            {
                string c = rules.SitePatterns[p].Condition;
                if ((c.Length == 0 || c == "いつでも") && rules.SitePatterns[p].Template.IndexOf("{エリア}", StringComparison.Ordinal) < 0) always = true;
            }
            if (!always && rules.SitePatterns.Count > 0) rules.Problems.Add("[地点の型] に、条件が「いつでも」で {エリア} を使わない型が 1 つ要る");
            return rules;
        }

        private void ReadSound(string line, int number)
        {
            string key;
            string value;
            if (!Split(line, '=', out key, out value))
            {
                Problems.Add("行 " + number + ": 「名前 = 値」の形でない");
                return;
            }
            switch (key)
            {
                case "頭": AddWords(Head, value); break;
                case "続き": AddWords(Body, value); break;
                case "避ける並び": AddWords(Blocked, value); break;
                case "語根の長さ":
                    if (!ReadRange(value, out RootMin, out RootMax) || RootMin < 1) Problems.Add("行 " + number + ": 語根の長さは「2-3」の形で書く");
                    break;
                case "固有名の長さ":
                    if (!ReadRange(value, out NameMin, out NameMax) || NameMin < 1) Problems.Add("行 " + number + ": 固有名の長さは「3-6」の形で書く");
                    break;
                default: Problems.Add("行 " + number + ": [音] に「" + key + "」という項目は無い"); break;
            }
        }

        private void ReadRoot(string line, int number, bool mood)
        {
            string key;
            string value;
            if (!Split(line, '=', out key, out value) || key.Length == 0)
            {
                Problems.Add("行 " + number + ": 「意味 = 語根」の形でない");
                return;
            }
            if (Roots.ContainsKey(key))
            {
                Problems.Add("行 " + number + ": 意味「" + key + "」が 2 回ある");
                return;
            }
            Roots.Add(key, value);
            RootOrder.Add(key);
            if (mood) Mood.Add(key);
        }

        private void ReadPattern(string line, int number, List<RegionNamePattern> list, string[] slots)
        {
            string[] parts = line.Split(':');
            float weight;
            if (parts.Length < 2 || !float.TryParse(parts[0].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out weight) || weight <= 0f)
            {
                Problems.Add("行 " + number + ": 「重み : 型」の形でない");
                return;
            }
            var pattern = new RegionNamePattern { Weight = weight, Template = parts[1].Trim(), Line = number };
            if (parts.Length > 2) pattern.Condition = parts[2].Trim();
            if (Array.IndexOf(Conditions, pattern.Condition) < 0)
            {
                Problems.Add("行 " + number + ": 知らない条件「" + pattern.Condition + "」");
                return;
            }
            List<string> tokens = RegionNaming.Tokens(pattern.Template);
            bool named = false;
            for (int t = 0; t < tokens.Count; t++)
            {
                if (tokens[t][0] != '{') continue;
                string slot = tokens[t].Substring(1, tokens[t].Length - 2);
                if (Array.IndexOf(slots, slot) < 0)
                {
                    Problems.Add("行 " + number + ": この型では使えない部品「" + tokens[t] + "」");
                    return;
                }
                if (slot != "類" && slot != "構造物") named = true;
            }
            if (!named)
            {
                Problems.Add("行 " + number + ": 固有名になる部品（{特徴} {地形} {雰囲気} {エリア}）が 1 つも無い");
                return;
            }
            list.Add(pattern);
        }

        private void ReadPair(string line, int number, Dictionary<string, string> into, bool allowEmpty)
        {
            string key;
            string value;
            if (!Split(line, '=', out key, out value) || key.Length == 0 || (!allowEmpty && value.Length == 0))
            {
                Problems.Add("行 " + number + ": 「名前 = 値」の形でない");
                return;
            }
            into[key] = value;
        }

        private static bool Split(string line, char mark, out string key, out string value)
        {
            int at = line.IndexOf(mark);
            if (at < 0)
            {
                key = line;
                value = "";
                return false;
            }
            key = line.Substring(0, at).Trim();
            value = line.Substring(at + 1).Trim();
            return true;
        }

        private static void AddWords(List<string> into, string value)
        {
            string[] words = value.Split(new[] { ' ', '\t', '、', ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int w = 0; w < words.Length; w++)
            {
                if (!into.Contains(words[w])) into.Add(words[w]);
            }
        }

        private static bool ReadRange(string value, out int min, out int max)
        {
            min = 0;
            max = 0;
            string[] parts = value.Split('-', '〜', '~');
            if (parts.Length == 1) return int.TryParse(parts[0].Trim(), out min) && (max = min) > 0;
            return parts.Length == 2 && int.TryParse(parts[0].Trim(), out min) && int.TryParse(parts[1].Trim(), out max) && max >= min;
        }
    }

    /// <summary>
    /// 規則から地名を作る。名前は「区別する部分（固有名）＋類を表す語」で組み、固有名は意味を持つ語根の並びにする。
    /// 同じ地域では同じ意味に同じ語根を使うので、川に関わる名前はどれも川の語根を含む。
    /// 名前ごとに、使った型・語根と意味・選んだ根拠を NameOrigin に残す。
    /// </summary>
    public static class RegionNaming
    {
        private struct Fact
        {
            public string Meaning;
            public string Why;

            /// <summary>選ばれやすさ。同じ事実を持つ場所が少ないほど大きい（区別になる事実を優先する）。</summary>
            public float Weight;
        }

        private sealed class Language
        {
            private readonly RegionNamingRules _rules;
            private readonly uint _seed;
            private readonly Dictionary<string, string> _sounds = new Dictionary<string, string>();
            private readonly HashSet<string> _taken = new HashSet<string>();

            public Language(RegionNamingRules rules, int seed)
            {
                _rules = rules;
                _seed = unchecked((uint)seed * 2246822519u + 374761393u);
                // 人が書いた語根を先に登録し、seed から作る語根が重ならないようにする
                foreach (KeyValuePair<string, string> pair in rules.Roots)
                {
                    if (pair.Value.Length == 0) continue;
                    _sounds[pair.Key] = pair.Value;
                    _taken.Add(pair.Value);
                }
                // 表に書かれた順に作る（どの名前から作り始めても同じ語根になる）
                for (int r = 0; r < rules.RootOrder.Count; r++) Root(rules.RootOrder[r]);
                for (int f = 0; f < RegionNamingRules.FactMeanings.Length; f++) Root(RegionNamingRules.FactMeanings[f]);
            }

            public bool IsWritten(string meaning)
            {
                string written;
                return _rules.Roots.TryGetValue(meaning, out written) && written.Length > 0;
            }

            public string Root(string meaning)
            {
                string sound;
                if (_sounds.TryGetValue(meaning, out sound)) return sound;
                int key = StableHash(meaning);
                for (int attempt = 0; attempt < 200; attempt++)
                {
                    uint local = _seed + (uint)attempt * 40503u;
                    int span = _rules.RootMax - _rules.RootMin + 1;
                    int length = _rules.RootMin + Math.Min(span - 1, (int)(RegionNoise.Hash01(key, 1, local) * span));
                    var text = new StringBuilder(Pick(_rules.Head, key, 2, local));
                    for (int m = 1; m < length; m++) text.Append(Pick(_rules.Body, key, 2 + m, local));
                    sound = text.ToString();
                    if (_taken.Contains(sound) || !Readable(sound, _rules, 1, 99)) continue;
                    _sounds[meaning] = sound;
                    _taken.Add(sound);
                    return sound;
                }
                sound = "？" + meaning;
                _sounds[meaning] = sound;
                return sound;
            }
        }

        /// <summary>地域のエリアと探索地点へ、規則から名前を付ける。</summary>
        public static void Apply(RegionData data, RegionNamingRules rules)
        {
            if (rules == null) rules = RegionNamingRules.Default;
            var language = new Language(rules, data.Spec.Seed);
            uint seed = unchecked((uint)data.Spec.Seed * 3266489917u + 2654435761u);
            var used = new HashSet<string>();
            var proper = new Dictionary<int, string>();

            float largest = 0f;
            float smallest = float.MaxValue;
            for (int a = 0; a < data.Areas.Count; a++)
            {
                largest = Math.Max(largest, data.Areas[a].AreaKm2);
                smallest = Math.Min(smallest, data.Areas[a].AreaKm2);
            }

            var areaTraits = new List<List<Fact>>();
            for (int a = 0; a < data.Areas.Count; a++) areaTraits.Add(AreaTraits(data, data.Areas[a], largest, smallest));
            Weigh(areaTraits);

            for (int a = 0; a < data.Areas.Count; a++)
            {
                RegionArea area = data.Areas[a];
                string noun = Noun(rules, area.Kind.ToString(), RegionAreaBuilder.KindNoun(area.Kind));
                string decided;
                if (rules.Fixed.TryGetValue(data.Spec.Seed + "/エリア" + area.Id, out decided))
                {
                    area.Name = decided;
                    area.NameOrigin = "人が決めた名前";
                    proper[area.Id] = decided.EndsWith(noun, StringComparison.Ordinal) && decided.Length > noun.Length
                        ? decided.Substring(0, decided.Length - noun.Length)
                        : decided;
                    used.Add(proper[area.Id]);
                    continue;
                }
                List<Fact> traits = areaTraits[a];
                Fact land = AreaLand(area);
                string own;
                string full;
                string origin;
                if (!Compose(rules, language, rules.AreaPatterns, seed, area.Id, traits, land, null, noun, "", used, out own, out full, out origin))
                {
                    own = "エリア" + area.Id;
                    full = own + noun;
                    origin = "規則から名前を作れなかった（番号で代用）";
                }
                used.Add(own);
                proper[area.Id] = own;
                area.Name = full;
                area.NameOrigin = origin;
            }

            var sameKind = new Dictionary<int, int>();
            for (int s = 0; s < data.Sites.Count; s++)
            {
                int key = data.Sites[s].AreaId * 16 + (int)data.Sites[s].Kind;
                int count;
                sameKind.TryGetValue(key, out count);
                sameKind[key] = count + 1;
            }

            var siteTraits = new List<List<Fact>>();
            for (int s = 0; s < data.Sites.Count; s++) siteTraits.Add(SiteTraits(data, data.Sites[s]));
            Weigh(siteTraits);

            for (int s = 0; s < data.Sites.Count; s++)
            {
                RegionSite site = data.Sites[s];
                string noun = Noun(rules, site.Kind.ToString(), RegionNames.StructureNoun(site.Kind));
                string decided;
                if (rules.Fixed.TryGetValue(data.Spec.Seed + "/地点" + site.Id, out decided))
                {
                    site.Feature = decided;
                    site.Name = decided;
                    site.NameOrigin = "人が決めた名前";
                    used.Add(decided);
                    continue;
                }
                string areaName;
                proper.TryGetValue(site.AreaId, out areaName);
                string condition = areaName == null ? "エリアなし" : (sameKind[site.AreaId * 16 + (int)site.Kind] == 1 ? "種類がエリアに1つ" : "種類がエリアに複数");
                string own;
                string full;
                string origin;
                if (!Compose(rules, language, rules.SitePatterns, seed, 1000 + site.Id, siteTraits[s], SiteLand(site), areaName, noun, condition, used, out own, out full, out origin))
                {
                    full = "地点" + site.Id + "の" + noun;
                    origin = "規則から名前を作れなかった（番号で代用）";
                }
                used.Add(full);
                if (own != null) used.Add(own);
                site.Feature = full;
                site.Name = full;
                site.NameOrigin = origin;
            }
        }

        /// <summary>その地域で使う語根の表（意味と音）。確認用の一覧に出す。</summary>
        public static List<RegionNameRoot> Roots(RegionNamingRules rules, int seed)
        {
            if (rules == null) rules = RegionNamingRules.Default;
            var language = new Language(rules, seed);
            var list = new List<RegionNameRoot>();
            var seen = new HashSet<string>();
            for (int r = 0; r < rules.RootOrder.Count; r++) AddRoot(list, seen, rules, language, rules.RootOrder[r]);
            for (int f = 0; f < RegionNamingRules.FactMeanings.Length; f++) AddRoot(list, seen, rules, language, RegionNamingRules.FactMeanings[f]);
            return list;
        }

        private static void AddRoot(List<RegionNameRoot> list, HashSet<string> seen, RegionNamingRules rules, Language language, string meaning)
        {
            if (!seen.Add(meaning)) return;
            list.Add(new RegionNameRoot
            {
                Meaning = meaning,
                Sound = language.Root(meaning),
                Written = language.IsWritten(meaning),
                Mood = rules.Mood.Contains(meaning),
            });
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

        /// <summary>
        /// 条件に合う型から重みで 1 つ選び、部品へ語根を入れて名前にする。
        /// 読みにくい並び・長さの外・既にある名前になったら、別の型や語根で作り直す。
        /// own は語根で作った固有の部分（{エリア} だけの型では null）、full は表示する名前。
        /// </summary>
        private static bool Compose(RegionNamingRules rules, Language language, List<RegionNamePattern> patterns, uint seed, int key,
            List<Fact> traits, Fact land, string areaName, string noun, string condition, HashSet<string> used,
            out string own, out string full, out string origin)
        {
            own = null;
            full = null;
            origin = null;
            var usable = new List<RegionNamePattern>();
            float total = 0f;
            for (int p = 0; p < patterns.Count; p++)
            {
                RegionNamePattern pattern = patterns[p];
                bool always = pattern.Condition.Length == 0 || pattern.Condition == "いつでも";
                if (!always && pattern.Condition != condition) continue;
                if (areaName == null && pattern.Template.IndexOf("{エリア}", StringComparison.Ordinal) >= 0) continue;
                if (traits.Count == 0 && pattern.Template.IndexOf("{特徴}", StringComparison.Ordinal) >= 0) continue;
                if (rules.Mood.Count == 0 && pattern.Template.IndexOf("{雰囲気}", StringComparison.Ordinal) >= 0) continue;
                usable.Add(pattern);
                total += pattern.Weight;
            }
            if (usable.Count == 0) return false;

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

                var shown = new StringBuilder();
                var made = new StringBuilder();
                var parts = new StringBuilder();
                var reasons = new StringBuilder();
                bool fromArea = false;
                List<string> tokens = Tokens(chosen.Template);
                for (int t = 0; t < tokens.Count; t++)
                {
                    string token = tokens[t];
                    if (token[0] != '{')
                    {
                        shown.Append(token);
                        continue;
                    }
                    switch (token)
                    {
                        case "{類}":
                        case "{構造物}":
                            shown.Append(noun);
                            break;
                        case "{エリア}":
                            shown.Append(areaName);
                            fromArea = true;
                            break;
                        case "{地形}":
                            Add(language, land, shown, made, parts, reasons);
                            break;
                        case "{特徴}":
                            Add(language, PickFact(traits, key, 10 + t, local), shown, made, parts, reasons);
                            break;
                        default:
                            string mood = rules.Mood[Index(rules.Mood.Count, key, 20 + t, local)];
                            Add(language, new Fact { Meaning = mood, Why = null }, shown, made, parts, reasons);
                            break;
                    }
                }

                string candidate = shown.ToString();
                string proper = made.Length > 0 ? made.ToString() : null;
                if (used.Contains(candidate)) continue;
                if (proper != null && (TooClose(proper, used) || !Readable(proper, rules, rules.NameMin, rules.NameMax))) continue;
                own = proper;
                full = candidate;
                var text = new StringBuilder("型「").Append(chosen.Template).Append("」");
                if (parts.Length > 0) text.Append("。").Append(parts);
                if (fromArea) text.Append("。エリアの固有名「").Append(areaName).Append("」から（このエリアにこの種類は 1 つ）");
                if (reasons.Length > 0) text.Append("。").Append(reasons);
                origin = text.ToString();
                return true;
            }
            return false;
        }

        private static void Add(Language language, Fact fact, StringBuilder shown, StringBuilder made, StringBuilder parts, StringBuilder reasons)
        {
            string sound = language.Root(fact.Meaning);
            shown.Append(sound);
            made.Append(sound);
            if (parts.Length > 0) parts.Append("＋");
            parts.Append(sound).Append("（").Append(fact.Meaning).Append("）");
            if (fact.Why != null)
            {
                if (reasons.Length > 0) reasons.Append("、");
                reasons.Append(fact.Meaning).Append("＝").Append(fact.Why);
            }
        }

        private static string Noun(RegionNamingRules rules, string key, string fallback)
        {
            string noun;
            return rules.Nouns.TryGetValue(key, out noun) ? noun : fallback;
        }

        private static Fact AreaLand(RegionArea area)
        {
            switch (area.Kind)
            {
                case RegionAreaKind.Mountains: return new Fact { Meaning = "山", Why = "山地" };
                case RegionAreaKind.Highland: return new Fact { Meaning = "山", Why = "高地" };
                case RegionAreaKind.Hills: return new Fact { Meaning = "丘", Why = "丘陵" };
                case RegionAreaKind.Basin: return new Fact { Meaning = "谷", Why = "盆地" };
                case RegionAreaKind.Island: return new Fact { Meaning = "島", Why = "島" };
                case RegionAreaKind.Snowfield: return new Fact { Meaning = "雪", Why = "雪原" };
                case RegionAreaKind.Desert: return new Fact { Meaning = "砂", Why = "砂漠" };
                default: return new Fact { Meaning = "野", Why = "平野" };
            }
        }

        private static List<Fact> AreaTraits(RegionData data, RegionArea area, float largest, float smallest)
        {
            var traits = new List<Fact>();
            AddDirection(traits, area.LabelX, area.LabelY, "エリアの位置");
            if (area.MaxHeight >= data.MaxHeight * 0.7f) traits.Add(new Fact { Meaning = "高い", Why = "最高点が地域の最高の 7 割以上" });
            if (area.MeanHeight < 150f && area.Kind != RegionAreaKind.Island) traits.Add(new Fact { Meaning = "低い", Why = "平均の標高が 150 m 未満" });
            AddBiome(traits, area.MainBiome, "最も広い");
            if (data.Areas.Count > 1 && area.AreaKm2 >= largest) traits.Add(new Fact { Meaning = "大きい", Why = "地域で最も広い" });
            if (data.Areas.Count > 1 && area.AreaKm2 <= smallest) traits.Add(new Fact { Meaning = "小さい", Why = "地域で最も狭い" });
            return traits;
        }

        private static Fact SiteLand(RegionSite site)
        {
            switch (site.Kind)
            {
                case RegionSiteKind.Monolith: return new Fact { Meaning = "山", Why = "山頂に立つ" };
                case RegionSiteKind.Arch: return new Fact { Meaning = "岸", Why = "海のそばに立つ" };
                case RegionSiteKind.Ring: return new Fact { Meaning = "砂", Why = "乾いた土地に立つ" };
                case RegionSiteKind.Tower: return new Fact { Meaning = "丘", Why = "丘に立つ" };
                case RegionSiteKind.Dome: return new Fact { Meaning = "雪", Why = "雪原に立つ" };
                case RegionSiteKind.Terrace: return new Fact { Meaning = "川", Why = "川のそばに立つ" };
                default: return new Fact { Meaning = "海", Why = "浅い海に沈む" };
            }
        }

        private static List<Fact> SiteTraits(RegionData data, RegionSite site)
        {
            var traits = new List<Fact>();
            AddDirection(traits, site.MapX, site.MapY, "地点の位置");
            if (site.Kind == RegionSiteKind.Sunken) traits.Add(new Fact { Meaning = "深い", Why = "海の中" });
            else if (site.HeightMeters >= data.MaxHeight * 0.5f) traits.Add(new Fact { Meaning = "高い", Why = "標高が地域の最高の 5 割以上" });
            else if (site.HeightMeters < 60f) traits.Add(new Fact { Meaning = "低い", Why = "標高 60 m 未満" });
            AddBiome(traits, site.Biome, "立つ場所が");
            return traits;
        }

        private static void AddDirection(List<Fact> traits, float mapX, float mapY, string what)
        {
            int col = mapX < 38f ? 0 : (mapX < 62f ? 1 : 2);
            int row = mapY < 38f ? 0 : (mapY < 62f ? 1 : 2);
            if (row == 0) traits.Add(new Fact { Meaning = "北", Why = what + "が地域の北" });
            if (row == 2) traits.Add(new Fact { Meaning = "南", Why = what + "が地域の南" });
            if (col == 0) traits.Add(new Fact { Meaning = "西", Why = what + "が地域の西" });
            if (col == 2) traits.Add(new Fact { Meaning = "東", Why = what + "が地域の東" });
            if (row == 1 && col == 1) traits.Add(new Fact { Meaning = "中", Why = what + "が地域の中央" });
        }

        private static void AddBiome(List<Fact> traits, RegionBiome biome, string what)
        {
            switch (biome)
            {
                case RegionBiome.Snow:
                case RegionBiome.Tundra:
                    traits.Add(new Fact { Meaning = "白い", Why = what + "雪・ツンドラ" });
                    break;
                case RegionBiome.Desert:
                case RegionBiome.Savanna:
                    traits.Add(new Fact { Meaning = "赤い", Why = what + "砂漠・サバンナ" });
                    traits.Add(new Fact { Meaning = "乾いた", Why = what + "砂漠・サバンナ" });
                    break;
                case RegionBiome.Forest:
                case RegionBiome.Taiga:
                case RegionBiome.Rainforest:
                    traits.Add(new Fact { Meaning = "緑の", Why = what + "森" });
                    traits.Add(new Fact { Meaning = "森", Why = what + "森" });
                    break;
            }
        }

        /// <summary>同じ意味の事実を持つ場所の数を数え、少ない事実ほど選ばれやすくする。</summary>
        private static void Weigh(List<List<Fact>> all)
        {
            var counts = new Dictionary<string, int>();
            for (int i = 0; i < all.Count; i++)
            {
                for (int f = 0; f < all[i].Count; f++)
                {
                    int count;
                    counts.TryGetValue(all[i][f].Meaning, out count);
                    counts[all[i][f].Meaning] = count + 1;
                }
            }
            for (int i = 0; i < all.Count; i++)
            {
                for (int f = 0; f < all[i].Count; f++)
                {
                    Fact fact = all[i][f];
                    fact.Weight = 1f / counts[fact.Meaning];
                    all[i][f] = fact;
                }
            }
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
        private static bool TooClose(string name, HashSet<string> used)
        {
            foreach (string other in used)
            {
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

        /// <summary>読める並びか（長さ、同じ字の連続、「ン」「ー」の重なり、避ける並び）。</summary>
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
                if (c == 'ン' && p == 'ー') return false;
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
