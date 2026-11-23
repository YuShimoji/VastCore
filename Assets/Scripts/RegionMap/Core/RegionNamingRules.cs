using System;
using System.Collections.Generic;
using System.Globalization;

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

    /// <summary>
    /// 音の質感 1 つ。語根を作る材料の音と、語根がつながる時の音の変化を持つ。
    /// 名前の骨格（どの意味をどの型で並べるか）とは別に持ち、付け替えても骨格は変わらない。
    /// </summary>
    public sealed class RegionSoundProfile
    {
        public string Name;
        public readonly List<string> Head = new List<string>();
        public readonly List<string> Body = new List<string>();
        public int RootMin = 2;
        public int RootMax = 2;

        /// <summary>前の語根の終わりと後ろの語根の頭が同じ音なら 1 つにする。</summary>
        public bool MergeSame = true;

        /// <summary>前の終わりの母音と、後ろの頭の母音（ア行）が同じ時。0 = そのまま、1 = 伸ばす（ー）、2 = 落とす。</summary>
        public int VowelMode;

        /// <summary>前の語根が「ン」で終わる時、後ろの語根の頭のカ・サ・タ・ハ行を濁らせる。</summary>
        public bool VoiceAfterN;

        /// <summary>継ぎ目をまたぐ 2 字の書き換え（「ンア」→「ナ」など）。</summary>
        public readonly List<KeyValuePair<string, string>> Rewrites = new List<KeyValuePair<string, string>>();

        private const string VowelA = "アカサタナハマヤラワガザダバパァャ";
        private const string VowelI = "イキシチニヒミリギジヂビピィ";
        private const string VowelU = "ウクスツヌフムユルグズヅブプゥュ";
        private const string VowelE = "エケセテネヘメレゲゼデベペェ";
        private const string VowelO = "オコソトノホモヨロヲゴゾドボポォョ";
        private const string Plain = "カキクケコサシスセソタチツテトハヒフヘホ";
        private const string Voiced = "ガギグゲゴザジズゼゾダヂヅデドバビブベボ";

        private static int Vowel(char kana)
        {
            if (VowelA.IndexOf(kana) >= 0) return 0;
            if (VowelI.IndexOf(kana) >= 0) return 1;
            if (VowelU.IndexOf(kana) >= 0) return 2;
            if (VowelE.IndexOf(kana) >= 0) return 3;
            if (VowelO.IndexOf(kana) >= 0) return 4;
            return -1;
        }

        /// <summary>2 つの語根をつなぐ。音が変わったら note に何が起きたかを入れる（変わらなければ null）。</summary>
        public string Join(string a, string b, out string note)
        {
            note = null;
            if (a.Length == 0) return b;
            if (b.Length == 0) return a;
            char last = a[a.Length - 1];
            char first = b[0];
            for (int r = 0; r < Rewrites.Count; r++)
            {
                string from = Rewrites[r].Key;
                if (from.Length != 2 || from[0] != last || from[1] != first) continue;
                note = "書き換え「" + from + "→" + Rewrites[r].Value + "」";
                return a.Substring(0, a.Length - 1) + Rewrites[r].Value + b.Substring(1);
            }
            if (VoiceAfterN && last == 'ン')
            {
                int at = Plain.IndexOf(first);
                if (at >= 0)
                {
                    b = Voiced[at] + b.Substring(1);
                    first = b[0];
                    note = "「ン」の後で濁る";
                }
            }
            if (b.Length > 1)
            {
                if (MergeSame && last == first)
                {
                    note = (note != null ? note + "、" : "") + "同じ音の重なりを 1 つにする";
                    return a + b.Substring(1);
                }
                int vowel = "アイウエオ".IndexOf(first);
                if (VowelMode != 0 && vowel >= 0 && Vowel(last) == vowel)
                {
                    note = (note != null ? note + "、" : "") + (VowelMode == 1 ? "重なる母音を伸ばす" : "重なる母音を落とす");
                    return VowelMode == 1 ? a + "ー" + b.Substring(1) : a + b.Substring(1);
                }
            }
            return a + b;
        }
    }

    /// <summary>
    /// 命名の規則。骨格（どの事実からどの意味を選び、どの型で並べるか。土地の経緯ごとにどの種類の名前を付けるか）と、
    /// 音の質感（付け替えられる）を持つ。テキストから読む（<see cref="Parse"/>）。
    /// 既定の中身（<see cref="DefaultText"/>）は仕組みを動かすための仮で、音・語・型・経緯の採否は人が決める。
    /// </summary>
    public sealed class RegionNamingRules
    {
        public readonly List<RegionSoundProfile> Sounds = new List<RegionSoundProfile>();
        public string SoundName = "";
        public string OldSoundName = "";
        public int People = 4;
        public int PersonMin = 3;
        public int PersonMax = 4;
        public int NameMin = 2;
        public int NameMax = 7;
        public readonly List<string> Blocked = new List<string>();

        /// <summary>地域の経緯（地域ごと／未踏／文明あり／古い文明／混在）。</summary>
        public string RegionHistory = "";
        public float AncientShare = -1f;
        public float SettledShare = -1f;
        public readonly Dictionary<string, string> HistoryFixed = new Dictionary<string, string>();

        /// <summary>未踏の土地にある峰へ土地の名も付ける、文明のあるエリアからの距離（地図の座標）。0 で付けない。</summary>
        public float PeakNameDistance = -1f;

        /// <summary>その時に峰に要る高さ（地域の最高点に対する割合）。</summary>
        public float PeakNameHeight = -1f;

        /// <summary>経緯ごとに付ける名前の種類（左が画面に出る名前）。</summary>
        public readonly Dictionary<string, List<string>> NameKinds = new Dictionary<string, List<string>>();
        public readonly Dictionary<string, string> Designations = new Dictionary<string, string>();
        public readonly Dictionary<string, string> Codes = new Dictionary<string, string>();

        /// <summary>事実（仕組みが土地から測る）→ その事実から選べる意味。</summary>
        public readonly Dictionary<string, List<string>> Facts = new Dictionary<string, List<string>>();

        public readonly Dictionary<string, string> Roots = new Dictionary<string, string>();

        /// <summary>規則に出てくる意味の、出てきた順（語根はこの順に作る）。</summary>
        public readonly List<string> RootOrder = new List<string>();
        public readonly List<string> Mood = new List<string>();
        public readonly List<string> Events = new List<string>();

        /// <summary>出来事を使える土地（その意味の事実を 1 つでも持つ場所）。空ならどこでも。</summary>
        public readonly Dictionary<string, List<string>> EventNeeds = new Dictionary<string, List<string>>();
        public readonly List<string> Roles = new List<string>();

        public readonly List<RegionNamePattern> RegionPatterns = new List<RegionNamePattern>();
        public readonly List<RegionNamePattern> AreaPatterns = new List<RegionNamePattern>();
        public readonly List<RegionNamePattern> RiverPatterns = new List<RegionNamePattern>();
        public readonly List<RegionNamePattern> PeakPatterns = new List<RegionNamePattern>();
        public readonly List<RegionNamePattern> BayPatterns = new List<RegionNamePattern>();
        public readonly List<RegionNamePattern> SitePatterns = new List<RegionNamePattern>();
        public readonly Dictionary<string, string> Nouns = new Dictionary<string, string>();
        public readonly Dictionary<string, string> Fixed = new Dictionary<string, string>();

        /// <summary>読み込みで見つけた問題（「行 12: …」）。空なら問題なし。</summary>
        public readonly List<string> Problems = new List<string>();

        /// <summary>仕組みが土地から測る事実（[事実と意味] の左に書ける語）。</summary>
        public static readonly string[] FactKeys =
        {
            "山地", "高地", "丘陵", "平野", "盆地", "島", "雪原", "砂漠",
            "山頂に立つ", "海のそばに立つ", "乾いた土地に立つ", "丘に立つ", "雪原に立つ", "川のそばに立つ", "浅い海に沈む",
            "川である", "峰である", "湾である",
            "北にある", "南にある", "東にある", "西にある", "中央にある",
            "最高点が高い", "標高が低い", "海の中にある",
            "雪とツンドラが広い", "砂漠とサバンナが広い", "森が広い",
            "最も広い", "最も狭い", "最も長い川", "最も高い峰", "海が広い", "陸が広い",
        };

        public static readonly string[] Histories = { "未踏", "文明あり", "古い文明" };

        /// <summary>[全体] の「音の質感」に書くと、地域（seed）ごとに [音 …] から 1 つを選ぶ。</summary>
        public const string AutoSound = "自動";

        /// <summary>[土地の経緯] の「地域」に書くと、地域（seed）ごとに経緯を 1 つ選び、地域全体をその経緯にする。</summary>
        public const string PerRegion = "地域ごと";

        /// <summary>1 つの地域の中で、エリアごとに経緯が分かれる。</summary>
        public const string Mixed = "混在";
        public static readonly string[] KindNames = { "記号名", "土地の名", "古い呼び名" };
        public static readonly string[] AreaSlots = { "特徴", "地形", "雰囲気", "類", "人名", "出来事", "古名" };
        public static readonly string[] FeatureSlots = { "特徴", "地形", "雰囲気", "類", "エリア", "人名", "出来事", "古名" };
        public static readonly string[] RegionSlots = { "特徴", "地形", "雰囲気", "類", "人名", "出来事" };
        public static readonly string[] SiteSlots = { "特徴", "地形", "雰囲気", "エリア", "構造物", "人名", "出来事", "古名", "山", "川" };

        public static readonly string[] Conditions =
        {
            "", "いつでも", "エリアの代表", "種類がエリアに1つ", "種類がエリアに複数", "エリアなし", "古い文明",
        };

        public const string DefaultText = @"# VastCore 命名の規則 第2版
# 骨格（事実→意味→型、経緯ごとの名前の種類）と、音の質感（付け替えられる）を分けて書く。中身は仕組みを動かすための仮。
# 行頭の # は注記。「名前 = 値」の形で書く。並びは空白で区切る。

[全体]
# 今のことばの音の質感と、古い呼び名に使う前のことばの音の質感（下の [音 …] の名前）
# 「自動」と書くと、地域（seed）ごとに下の [音 …] から 1 つを選ぶ
音の質感 = 自動
古い音の質感 = かたい
人物の数 = 4
人名の長さ = 3-4
固有名の長さ = 2-7
避ける並び = バカ アホ シネ クソ ウンコ チンコ マンコ エロ ハゲ デブ ゲロ キモ ウザ コロス シヌ ガン ゴミ ザコ ボケ カス サイレン バット
# 日本語の語として読めて、表の意味と食い違って見える並び
避ける並び = シロ クロ アカ アオ ヤマ カワ ウミ モリ ユキ スナ シマ オカ タニ キタ ニシ ナカ タカ ヒク イシ コケ クサ ハナ ソラ ホシ ツキ カゼ カゲ キリ ウタ テツ ムシ クモ アメ ヒト イヌ ネコ トリ サル ウシ ウマ

[音 標準]
# 語根は「頭」の音 1 つに「続き」の音をつないで作る。
頭 = ア イ ウ エ オ カ キ ク ケ コ サ シ ス セ ソ タ テ ト ナ ニ ネ ノ ハ ヒ フ ホ マ ミ ム メ モ ヤ ユ ヨ ラ リ ル レ ロ ワ ガ ギ グ ゴ ザ ジ ゼ ダ デ ド バ ビ ベ ボ
続き = ア イ ウ エ オ カ キ ク ケ コ サ シ ス セ ソ タ テ ト ナ ニ ネ ノ マ ミ ム メ モ ラ リ ル レ ロ ン ガ グ ザ ジ ダ ド バ ビ
語根の長さ = 2-2
# 語がつながる時の音の変化
同じ音の重なり = 落とす
母音の重なり = そのまま
ンの後の濁り = なし

[音 やわらか]
頭 = ア イ ウ エ オ サ シ ス セ ソ ナ ニ ネ ノ ハ ヒ フ ホ マ ミ ム メ モ ヤ ユ ヨ ラ リ ル レ ロ ワ
続き = ア イ エ オ ナ ニ ネ ノ マ ミ ム メ モ ラ リ ル レ ロ ヤ ユ ン サ シ セ
語根の長さ = 2-3
同じ音の重なり = 落とす
母音の重なり = 伸ばす
ンの後の濁り = なし

[音 かたい]
頭 = ガ ギ グ ゴ ザ ジ ゼ ゾ ダ デ ド バ ビ ブ ベ ボ カ ク ケ コ タ テ ト
続き = ガ グ ゴ ザ ズ ダ ド バ ブ ク ト ル ラ ロ ン ッ
語根の長さ = 2-3
同じ音の重なり = 落とす
母音の重なり = そのまま
ンの後の濁り = あり

[土地の経緯]
# 地域 に書ける値: 地域ごと / 未踏 / 文明あり / 古い文明 / 混在
#   地域ごと: 地域（seed）ごとに「未踏」「文明あり」「古い文明」のどれか 1 つを選び、地域全体をその経緯にする
#   混在: 1 つの地域の中で、エリアごとに経緯が分かれる
地域 = 地域ごと
# 地域ごとの時: この割合の地域が「古い文明」、次のこの割合までの地域が「文明あり」、残りの地域が「未踏」になる
# 混在の時: エリアを住みやすい順に並べ、上からこの割合までを「古い文明」、次のこの割合までを「文明あり」、残りを「未踏」にする
古い文明の割合 = 0.25
文明のある割合 = 0.65
# 地域の経緯を人が決める時: 「seed/地域 = 古い文明」。例: 7/地域 = 古い文明
# エリアごとに決める時: 「seed/エリア番号 = 未踏」。例: 7/エリア3 = 未踏
# 未踏の土地にある峰でも、文明のあるエリアから近くて高ければ、土地の名も付ける
# 距離は地図の座標（0〜100）で、0 と書くと付けない。高さは地域の最高点に対する割合
峰に名を付ける距離 = 8
峰に名を付ける高さ = 0.5

[経緯と名前]
# 経緯 = 付ける名前の種類（左が画面に出る名前、その右は別名）
未踏 = 記号名
文明あり = 土地の名 記号名
古い文明 = 土地の名 古い呼び名 記号名

[記号名]
# {種}=種類の記号 {番}=番号（2 桁） {横}{縦}=地図の座標（各 2 桁。この座標でサーチすると当たる） {seed}=地域の seed
地域 = R{seed}
エリア = {種}-{番}
地点 = {種}-{横}{縦}
川 = {種}-{番}
山 = {種}-{横}{縦}
湾 = {種}-{横}{縦}

[種類の記号]
Region = R
Mountains = MT
Highland = HL
Hills = HI
Plain = PL
Basin = BS
Island = IS
Snowfield = SN
Desert = DS
River = FL
Peak = PK
Bay = BY
Monolith = MN
Arch = AR
Ring = RG
Tower = TW
Dome = DM
Terrace = TR
Sunken = SK

[事実と意味]
# 左 = 仕組みが土地から測る事実（左の語は増やせない）。右 = その事実から選べる意味（書き換え・追加できる）。
# 右に新しい意味を書くと、その語根は seed から作る。右を空にすると、その事実は名前に使わない。
# 種類の事実（型の {地形} になる。右の先頭の意味を使う）
山地 = 山
高地 = 山
丘陵 = 丘
平野 = 野
盆地 = 谷
島 = 島
雪原 = 雪
砂漠 = 砂
山頂に立つ = 山
海のそばに立つ = 岸
乾いた土地に立つ = 砂
丘に立つ = 丘
雪原に立つ = 雪
川のそばに立つ = 川
浅い海に沈む = 海
川である = 川
峰である = 山
湾である = 海
# 特徴の事実（型の {特徴} になる。右のどれかを使う）
北にある = 北
南にある = 南
東にある = 東
西にある = 西
中央にある = 中
最高点が高い = 高い
標高が低い = 低い
海の中にある = 深い
雪とツンドラが広い = 白い
砂漠とサバンナが広い = 赤い 乾いた
森が広い = 緑の 森
最も広い = 大きい
最も狭い = 小さい
最も長い川 = 大きい 長い
最も高い峰 = 高い 大きい
海が広い = 海
陸が広い = 大きい

[語根]
# 意味 = 語根。語根を人が決める時だけ書く（例: 山 = ガル）。書かない意味の語根は seed から作る。

[雰囲気]
# 土地の事実に結びつかない語。型の {雰囲気} で使う。行を足し引きしてよい。右に語根を書くと、その語根を使う。
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

[出来事]
# 経緯に置く出来事。型の {出来事} で使う。「意味 = 語根 : 使える土地」。使える土地は、その意味の事実を持つ場所。空ならどこでも。
戦 =
大水 = : 川 岸 海 低い
火 = : 森 乾いた
落ちた星 =
渡り = : 岸 海 島
祭り =

[人物]
# 型の {人名} で使う人物の役。人名は音から作る。
役 = 開いた者 治めた者 見つけた者 渡ってきた者 守った者

[地域の型]
# 重み : 型
2 : {特徴}{地形}{類}
1 : {雰囲気}{地形}{類}

[エリアの型]
# 重み : 型 : 条件
# {特徴}=事実から選ぶ語根 {地形}=種類の語根 {雰囲気}=上の表から選ぶ語根 {類}=山地・平野など
# {人名}=経緯に置いた人物 {出来事}=経緯に置いた出来事 {古名}=その場所の古い呼び名
4 : {特徴}{地形}{類}
2 : {雰囲気}{地形}{類}
1 : {特徴}{雰囲気}{類}
1 : {人名}{類}
1 : {出来事}{地形}{類}
3 : {古名}{類} : 古い文明

[川の型]
# {エリア}=河口のエリアの固有名
3 : {特徴}{地形}{類}
2 : {雰囲気}{地形}{類}
2 : {エリア}{類}
1 : {人名}{類}
2 : {古名}{類} : 古い文明

[山の型]
3 : {特徴}{地形}{類}
2 : {雰囲気}{類}
1 : {人名}{類}
1 : {出来事}{類}
2 : {古名}{類} : 古い文明

[湾の型]
3 : {特徴}{地形}{類}
2 : {エリア}{類}
1 : {雰囲気}{類}

[地点の型]
# {エリア}=属するエリアの固有名 {山}{川}=そばにある名のある峰・川の固有名 {構造物}=塔・環など
# 条件 = いつでも / エリアの代表（そのエリアで種類に最も合う地点）/ 種類がエリアに1つ / 種類がエリアに複数 / エリアなし / 古い文明
200 : {エリア}の{構造物} : エリアの代表
4 : {山}の{構造物} : いつでも
4 : {川}の{構造物} : いつでも
3 : {特徴}{雰囲気}の{構造物} : いつでも
3 : {雰囲気}{地形}の{構造物} : いつでも
2 : {特徴}{地形}の{構造物} : いつでも
2 : {人名}の{構造物} : いつでも
1 : {出来事}の{構造物} : いつでも
3 : {古名}の{構造物} : 古い文明

[類の語]
Region = 地方
Mountains = 山地
Highland = 高地
Hills = 丘陵
Plain = 平野
Basin = 盆地
Island = 島
Snowfield = 雪原
Desert = 砂漠
River = 川
Peak = 山
Bay = 湾
Monolith = モノリス
Arch = アーチ
Ring = 環
Tower = 塔
Dome = ドーム
Terrace = 段丘
Sunken = 沈んだ遺構

[決めた名前]
# 人が決めた名前。「seed/エリア番号 = 名前」「seed/地点番号」「seed/川番号」「seed/山番号」「seed/湾番号」「seed/地域」。例: 7/エリア3 = …
";

        private static RegionNamingRules _default;

        /// <summary>既定の規則（<see cref="DefaultText"/> を読んだもの）。</summary>
        public static RegionNamingRules Default
        {
            get { return _default ?? (_default = Read(DefaultText, true)); }
        }

        /// <summary>規則のテキストを読む。null か空なら既定。問題は <see cref="Problems"/> に入る。</summary>
        public static RegionNamingRules Parse(string text)
        {
            return string.IsNullOrEmpty(text) ? Default : Read(text, false);
        }

        public RegionSoundProfile Sound(string name)
        {
            for (int s = 0; s < Sounds.Count; s++)
            {
                if (Sounds[s].Name == name) return Sounds[s];
            }
            return null;
        }

        private static RegionNamingRules Read(string text, bool isDefault)
        {
            var rules = new RegionNamingRules();
            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            string section = null;
            RegionSoundProfile sound = null;
            // 見出しが書かれていた節。書かれていない節だけを、後で既定の中身で補う
            var written = new HashSet<string>();
            for (int l = 0; l < lines.Length; l++)
            {
                string line = lines[l].Replace('＝', '=').Replace('：', ':').Replace('　', ' ').Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                int number = l + 1;
                if (line[0] == '[')
                {
                    section = line.Trim('[', ']', ' ');
                    sound = null;
                    if (section == "音" || section.StartsWith("音 ", StringComparison.Ordinal))
                    {
                        string name = section.Length > 2 ? section.Substring(2).Trim() : "標準";
                        if (rules.Sound(name) != null) rules.Problems.Add("行 " + number + ": 音の質感「" + name + "」が 2 回ある");
                        sound = new RegionSoundProfile { Name = name };
                        rules.Sounds.Add(sound);
                        section = "音";
                    }
                    written.Add(section);
                    continue;
                }
                switch (section)
                {
                    case "全体": rules.ReadGlobal(line, number); break;
                    case "音": rules.ReadSound(sound, line, number); break;
                    case "土地の経緯": rules.ReadHistory(line, number); break;
                    case "経緯と名前": rules.ReadKinds(line, number); break;
                    case "記号名": rules.ReadPair(line, number, rules.Designations); break;
                    case "種類の記号": rules.ReadPair(line, number, rules.Codes); break;
                    case "事実と意味": rules.ReadFact(line, number); break;
                    case "語根": rules.ReadRoot(line, number, 0); break;
                    case "雰囲気": rules.ReadRoot(line, number, 1); break;
                    case "出来事": rules.ReadRoot(line, number, 2); break;
                    case "人物": rules.ReadPeople(line, number); break;
                    case "地域の型": rules.ReadPattern(line, number, rules.RegionPatterns, RegionSlots); break;
                    case "エリアの型": rules.ReadPattern(line, number, rules.AreaPatterns, AreaSlots); break;
                    case "川の型": rules.ReadPattern(line, number, rules.RiverPatterns, FeatureSlots); break;
                    case "山の型": rules.ReadPattern(line, number, rules.PeakPatterns, FeatureSlots); break;
                    case "湾の型": rules.ReadPattern(line, number, rules.BayPatterns, FeatureSlots); break;
                    case "地点の型": rules.ReadPattern(line, number, rules.SitePatterns, SiteSlots); break;
                    case "類の語": rules.ReadPair(line, number, rules.Nouns); break;
                    case "決めた名前": rules.ReadPair(line, number, rules.Fixed); break;
                    default: rules.Problems.Add("行 " + number + ": 知らない節「" + section + "」の中の行"); break;
                }
            }

            if (!isDefault) rules.FillFrom(Default, written);
            if (rules.Sounds.Count == 0) rules.Problems.Add("[音] が無い");
            for (int s = 0; s < rules.Sounds.Count; s++)
            {
                string label = rules.Sounds.Count > 1 || rules.Sounds[s].Name != "標準" ? "[音 " + rules.Sounds[s].Name + "]" : "[音]";
                if (rules.Sounds[s].Head.Count == 0) rules.Problems.Add(label + " に「頭」が無い");
                if (rules.Sounds[s].Body.Count == 0) rules.Problems.Add(label + " に「続き」が無い");
            }
            if (rules.AreaPatterns.Count == 0) rules.Problems.Add("[エリアの型] が空");
            if (rules.SitePatterns.Count == 0) rules.Problems.Add("[地点の型] が空");
            bool always = false;
            for (int p = 0; p < rules.SitePatterns.Count; p++)
            {
                RegionNamePattern pattern = rules.SitePatterns[p];
                if (pattern.Condition.Length != 0 && pattern.Condition != "いつでも") continue;
                bool plain = true;
                foreach (string needs in new[] { "{エリア}", "{山}", "{川}", "{古名}", "{人名}", "{出来事}" })
                {
                    if (pattern.Template.IndexOf(needs, StringComparison.Ordinal) >= 0) plain = false;
                }
                if (plain) always = true;
            }
            if (!always && rules.SitePatterns.Count > 0) rules.Problems.Add("[地点の型] に、条件が「いつでも」で {特徴} {地形} {雰囲気} だけを使う型が 1 つ要る");

            if (rules.SoundName.Length == 0 && rules.Sounds.Count > 0) rules.SoundName = rules.Sounds[0].Name;
            if (rules.SoundName.Length > 0 && rules.SoundName != AutoSound && rules.Sound(rules.SoundName) == null) rules.Problems.Add("[全体] の音の質感「" + rules.SoundName + "」に当たる [音 …] が無い");
            if (rules.OldSoundName.Length == 0 || rules.Sound(rules.OldSoundName) == null)
            {
                if (rules.OldSoundName.Length > 0) rules.Problems.Add("[全体] の古い音の質感「" + rules.OldSoundName + "」に当たる [音 …] が無い");
                rules.OldSoundName = rules.SoundName;
                for (int s = 0; s < rules.Sounds.Count; s++)
                {
                    if (rules.Sounds[s].Name != rules.SoundName) rules.OldSoundName = rules.Sounds[s].Name;
                }
            }
            return rules;
        }

        /// <summary>
        /// 書かれていない節を、既定の中身で補う（経緯だけ・型だけ・音だけを書いた規則でも動くようにする）。
        /// 音の質感は名前ごとに補う（人が書いた名前の質感はそのまま、書かれていない名前の質感を足す）。
        /// 雰囲気・出来事・エリアの型・地点の型は、見出しを書いて中身を空にした時は補わない（使わない、という指定）。
        /// </summary>
        private void FillFrom(RegionNamingRules source, HashSet<string> written)
        {
            for (int s = 0; s < source.Sounds.Count; s++)
            {
                if (Sound(source.Sounds[s].Name) == null) Sounds.Add(source.Sounds[s]);
            }
            if (!written.Contains("雰囲気")) CopyRoots(source, source.Mood, Mood);
            if (!written.Contains("出来事"))
            {
                CopyRoots(source, source.Events, Events);
                foreach (KeyValuePair<string, List<string>> pair in source.EventNeeds)
                {
                    if (!EventNeeds.ContainsKey(pair.Key)) EventNeeds[pair.Key] = pair.Value;
                }
            }
            if (!written.Contains("エリアの型")) AreaPatterns.AddRange(source.AreaPatterns);
            if (!written.Contains("地点の型")) SitePatterns.AddRange(source.SitePatterns);
            if (Blocked.Count == 0) Blocked.AddRange(source.Blocked);
            // [全体] も [音 …] も書いていない規則は、既定の音の質感の選び方（地域ごとに自動）を引き継ぐ
            if (SoundName.Length == 0 && !written.Contains("全体") && !written.Contains("音")) SoundName = source.SoundName;
            if (PeakNameDistance < 0f) PeakNameDistance = source.PeakNameDistance;
            if (PeakNameHeight < 0f) PeakNameHeight = source.PeakNameHeight;
            if (RegionHistory.Length == 0) RegionHistory = source.RegionHistory;
            if (AncientShare < 0f) AncientShare = source.AncientShare;
            if (SettledShare < 0f) SettledShare = source.SettledShare;
            if (NameKinds.Count == 0) foreach (KeyValuePair<string, List<string>> pair in source.NameKinds) NameKinds[pair.Key] = pair.Value;
            if (Designations.Count == 0) foreach (KeyValuePair<string, string> pair in source.Designations) Designations[pair.Key] = pair.Value;
            if (Codes.Count == 0) foreach (KeyValuePair<string, string> pair in source.Codes) Codes[pair.Key] = pair.Value;
            if (Facts.Count == 0)
            {
                foreach (KeyValuePair<string, List<string>> pair in source.Facts) Facts[pair.Key] = pair.Value;
                for (int r = 0; r < source.RootOrder.Count; r++)
                {
                    string meaning = source.RootOrder[r];
                    if (source.Mood.Contains(meaning) || source.Events.Contains(meaning)) continue;
                    Note(meaning);
                }
            }
            if (Roles.Count == 0) Roles.AddRange(source.Roles);
            if (RegionPatterns.Count == 0) RegionPatterns.AddRange(source.RegionPatterns);
            if (RiverPatterns.Count == 0) RiverPatterns.AddRange(source.RiverPatterns);
            if (PeakPatterns.Count == 0) PeakPatterns.AddRange(source.PeakPatterns);
            if (BayPatterns.Count == 0) BayPatterns.AddRange(source.BayPatterns);
            foreach (KeyValuePair<string, string> pair in source.Nouns)
            {
                if (!Nouns.ContainsKey(pair.Key)) Nouns[pair.Key] = pair.Value;
            }
        }

        /// <summary>既定の雰囲気・出来事の語を写す。同じ意味を人が [語根] に書いていれば、その語根を残す。</summary>
        private void CopyRoots(RegionNamingRules source, List<string> from, List<string> to)
        {
            for (int m = 0; m < from.Count; m++)
            {
                string meaning = from[m];
                if (to.Contains(meaning)) continue;
                to.Add(meaning);
                Note(meaning);
                string root;
                if (Roots[meaning].Length == 0 && source.Roots.TryGetValue(meaning, out root)) Roots[meaning] = root;
            }
        }

        private void Note(string meaning)
        {
            if (!RootOrder.Contains(meaning)) RootOrder.Add(meaning);
            if (!Roots.ContainsKey(meaning)) Roots.Add(meaning, "");
        }

        private void ReadGlobal(string line, int number)
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
                case "音の質感": SoundName = value; break;
                case "古い音の質感": OldSoundName = value; break;
                case "人物の数":
                    if (!int.TryParse(value, out People) || People < 0) Problems.Add("行 " + number + ": 人物の数は 0 以上の整数で書く");
                    break;
                case "人名の長さ":
                    if (!ReadRange(value, out PersonMin, out PersonMax) || PersonMin < 2) Problems.Add("行 " + number + ": 人名の長さは「3-4」の形で書く（2 以上）");
                    break;
                case "固有名の長さ":
                    if (!ReadRange(value, out NameMin, out NameMax) || NameMin < 1) Problems.Add("行 " + number + ": 固有名の長さは「2-7」の形で書く");
                    break;
                case "避ける並び": AddWords(Blocked, value); break;
                default: Problems.Add("行 " + number + ": [全体] に「" + key + "」という項目は無い"); break;
            }
        }

        private void ReadSound(RegionSoundProfile sound, string line, int number)
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
                case "頭": AddWords(sound.Head, value); break;
                case "続き": AddWords(sound.Body, value); break;
                case "避ける並び": AddWords(Blocked, value); break;
                case "語根の長さ":
                    if (!ReadRange(value, out sound.RootMin, out sound.RootMax) || sound.RootMin < 1) Problems.Add("行 " + number + ": 語根の長さは「2-3」の形で書く");
                    break;
                case "固有名の長さ":
                    if (!ReadRange(value, out NameMin, out NameMax) || NameMin < 1) Problems.Add("行 " + number + ": 固有名の長さは「2-7」の形で書く");
                    break;
                case "同じ音の重なり":
                    if (value == "落とす") sound.MergeSame = true;
                    else if (value == "そのまま") sound.MergeSame = false;
                    else Problems.Add("行 " + number + ": 同じ音の重なりは「落とす」か「そのまま」");
                    break;
                case "母音の重なり":
                    if (value == "そのまま") sound.VowelMode = 0;
                    else if (value == "伸ばす") sound.VowelMode = 1;
                    else if (value == "落とす") sound.VowelMode = 2;
                    else Problems.Add("行 " + number + ": 母音の重なりは「そのまま」「伸ばす」「落とす」のどれか");
                    break;
                case "ンの後の濁り":
                    if (value == "あり") sound.VoiceAfterN = true;
                    else if (value == "なし") sound.VoiceAfterN = false;
                    else Problems.Add("行 " + number + ": ンの後の濁りは「あり」か「なし」");
                    break;
                case "書き換え":
                    string[] pairs = value.Split(new[] { ' ', '\t', '、', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int p = 0; p < pairs.Length; p++)
                    {
                        int at = pairs[p].IndexOf('>');
                        if (at != 2 || pairs[p].Length < 4) Problems.Add("行 " + number + ": 書き換えは「ンア>ナ」の形で書く（左は継ぎ目をまたぐ 2 字）");
                        else sound.Rewrites.Add(new KeyValuePair<string, string>(pairs[p].Substring(0, 2), pairs[p].Substring(3)));
                    }
                    break;
                default: Problems.Add("行 " + number + ": [音] に「" + key + "」という項目は無い"); break;
            }
        }

        private void ReadHistory(string line, int number)
        {
            string key;
            string value;
            if (!Split(line, '=', out key, out value) || value.Length == 0)
            {
                Problems.Add("行 " + number + ": 「名前 = 値」の形でない");
                return;
            }
            switch (key)
            {
                case "地域":
                    if (value != Mixed && value != PerRegion && Array.IndexOf(Histories, value) < 0) Problems.Add("行 " + number + ": 地域の経緯は「地域ごと」「未踏」「文明あり」「古い文明」「混在」のどれか");
                    else RegionHistory = value;
                    break;
                case "古い文明の割合":
                    if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out AncientShare)) Problems.Add("行 " + number + ": 割合は 0〜1 の数で書く");
                    break;
                case "文明のある割合":
                    if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out SettledShare)) Problems.Add("行 " + number + ": 割合は 0〜1 の数で書く");
                    break;
                case "峰に名を付ける距離":
                    if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out PeakNameDistance) || PeakNameDistance < 0f) Problems.Add("行 " + number + ": 距離は 0 以上の数で書く（地図の座標。0 で付けない）");
                    break;
                case "峰に名を付ける高さ":
                    if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out PeakNameHeight) || PeakNameHeight < 0f || PeakNameHeight > 1f) Problems.Add("行 " + number + ": 高さは 0〜1 の数で書く（地域の最高点に対する割合）");
                    break;
                default:
                    bool area = key.IndexOf("/エリア", StringComparison.Ordinal) > 0;
                    bool region = key.Length > 3 && key.EndsWith("/地域", StringComparison.Ordinal);
                    if ((area || region) && Array.IndexOf(Histories, value) >= 0) HistoryFixed[key] = value;
                    else Problems.Add("行 " + number + ": [土地の経緯] の行として読めない");
                    break;
            }
        }

        private void ReadKinds(string line, int number)
        {
            string key;
            string value;
            if (!Split(line, '=', out key, out value) || Array.IndexOf(Histories, key) < 0)
            {
                Problems.Add("行 " + number + ": 左は「未踏」「文明あり」「古い文明」のどれか");
                return;
            }
            var kinds = new List<string>();
            AddWords(kinds, value);
            for (int k = 0; k < kinds.Count; k++)
            {
                if (Array.IndexOf(KindNames, kinds[k]) >= 0) continue;
                Problems.Add("行 " + number + ": 名前の種類は「記号名」「土地の名」「古い呼び名」のどれか");
                return;
            }
            if (kinds.Count == 0)
            {
                Problems.Add("行 " + number + ": 名前の種類が 1 つも無い");
                return;
            }
            NameKinds[key] = kinds;
        }

        private void ReadFact(string line, int number)
        {
            string key;
            string value;
            if (!Split(line, '=', out key, out value) || Array.IndexOf(FactKeys, key) < 0)
            {
                Problems.Add("行 " + number + ": 「" + key + "」は仕組みが測る事実に無い");
                return;
            }
            var meanings = new List<string>();
            AddWords(meanings, value);
            Facts[key] = meanings;
            for (int m = 0; m < meanings.Count; m++) Note(meanings[m]);
        }

        /// <summary>kind: 0 = [語根]、1 = [雰囲気]、2 = [出来事]。</summary>
        private void ReadRoot(string line, int number, int kind)
        {
            string needs = "";
            if (kind == 2)
            {
                int colon = line.IndexOf(':');
                if (colon >= 0)
                {
                    needs = line.Substring(colon + 1).Trim();
                    line = line.Substring(0, colon).Trim();
                }
            }
            string key;
            string value;
            if (!Split(line, '=', out key, out value) || key.Length == 0)
            {
                Problems.Add("行 " + number + ": 「意味 = 語根」の形でない");
                return;
            }
            bool twice = kind == 0 ? (Roots.ContainsKey(key) && Roots[key].Length > 0) : (Mood.Contains(key) || Events.Contains(key));
            if (twice)
            {
                Problems.Add("行 " + number + ": 意味「" + key + "」が 2 回ある");
                return;
            }
            Note(key);
            if (value.Length > 0) Roots[key] = value;
            if (kind == 1) Mood.Add(key);
            if (kind == 2)
            {
                Events.Add(key);
                var list = new List<string>();
                AddWords(list, needs);
                EventNeeds[key] = list;
            }
        }

        private void ReadPeople(string line, int number)
        {
            string key;
            string value;
            if (!Split(line, '=', out key, out value) || key != "役")
            {
                Problems.Add("行 " + number + ": [人物] には「役 = …」を書く");
                return;
            }
            AddWords(Roles, value);
        }

        private void ReadPattern(string line, int number, List<RegionNamePattern> list, string[] slots)
        {
            string[] parts = line.Split(':');
            float weight;
            if (parts.Length < 2 || !float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out weight) || weight <= 0f)
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
                Problems.Add("行 " + number + ": 固有名になる部品（{特徴} {地形} {雰囲気} など）が 1 つも無い");
                return;
            }
            list.Add(pattern);
        }

        private void ReadPair(string line, int number, Dictionary<string, string> into)
        {
            string key;
            string value;
            if (!Split(line, '=', out key, out value) || key.Length == 0 || value.Length == 0)
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
}
