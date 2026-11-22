using System;
using System.Collections.Generic;

namespace Vastcore.RegionMap
{
    /// <summary>エリアと探索地点の呼び名の作り方。響きの方向は人が選ぶ。</summary>
    public enum RegionNameStyle
    {
        /// <summary>方角と地形から機械的に作る（「北東の山地」「北東の山地・山頂のモノリス」）。</summary>
        Descriptive = 0,

        /// <summary>seed から作るカタカナの造語。母音と流れる音が多い（やわらかい響き）。</summary>
        SoftKana = 1,

        /// <summary>seed から作るカタカナの造語。濁音と詰まる音が多い（硬い響き）。</summary>
        HardKana = 2,

        /// <summary>seed から作る漢字二字の地名（自然の語の組み合わせ）。</summary>
        Kanji = 3,

        /// <summary>
        /// 命名の規則（音・語根・型）から作る。固有名は意味を持つ語根の並びで、由来を NameOrigin に残す（RegionNaming）。
        /// </summary>
        Systematic = 4,
    }

    /// <summary>
    /// seed から固有の地名を作る。同じ seed と作り方からは同じ名前が出る。地域の中で名前は重ならない。
    /// 作った語が実在の地名や既存の作品の名前と偶然重なることは防いでいない。
    /// </summary>
    public static class RegionNames
    {
        private static readonly string[] SoftStart =
        {
            "ア", "イ", "エ", "オ", "ル", "リ", "レ", "ラ", "ミ", "メ", "マ", "ナ", "ネ", "ノ", "セ", "サ", "シ", "ソ",
            "ユ", "ヨ", "ハ", "ヒ", "フィ", "ウィ", "エル", "アル", "イル", "ニ", "ロ", "ヤ",
        };

        private static readonly string[] SoftMid =
        {
            "ラ", "リ", "ル", "レ", "ロ", "ナ", "ニ", "ネ", "ノ", "マ", "ミ", "ム", "メ", "モ", "サ", "シ", "ス", "セ", "ソ",
            "ヤ", "ユ", "ヨ", "ア", "イ", "エ", "オ", "ー", "ン", "フ", "ハ",
        };

        private static readonly string[] SoftEnd =
        {
            "ア", "ナ", "ラ", "リア", "レア", "ネ", "ノ", "ル", "リ", "ミア", "エ", "イ", "ス", "ン", "ユ", "ロ", "サ",
            "ーラ", "ーナ", "ニア", "レ", "ミ", "オ", "シア", "ーネ", "ラン", "レン", "リス",
        };

        private static readonly string[] HardStart =
        {
            "ガ", "グ", "ゴ", "ド", "ダ", "デ", "バ", "ボ", "ブ", "ザ", "ゾ", "カ", "ク", "ケ", "コ", "タ", "ト", "テ",
            "ヴァ", "ヴォ", "ヴェ", "ジ", "ギ", "ゲ", "ベ", "ズ",
        };

        private static readonly string[] HardMid =
        {
            "ル", "ラ", "ロ", "ン", "ー", "ガ", "グ", "ド", "ダ", "ザ", "ゾ", "バ", "ボ", "カ", "ク", "ト", "タ", "ゴ",
            "デ", "ズ", "ブ", "レ", "リ",
        };

        private static readonly string[] HardEnd =
        {
            "ク", "ト", "ド", "グ", "ス", "ズ", "ン", "ル", "ム", "ガ", "ダ", "ゾ", "ック", "ット", "ルド", "ルグ", "ンド",
            "ンガ", "ーク", "ード", "ロス", "ラド", "ルク", "ゴス", "ッド", "ッグ", "ルザ", "ンク",
        };

        // 漢字の地名の一字目（どの地形にも使う自然の語）
        private static readonly string[] KanjiFirst =
        {
            "霧", "白", "鷹", "天", "黒", "蒼", "雲", "嵐", "鉄", "銀", "風", "月", "星", "氷", "赤", "雷", "朝", "夕",
            "深", "遠", "灰", "碧", "鳴", "千",
        };

        // 二字目は地形ごとに変える（後ろに付く「山地」「平野」などと字が重ならないもの）
        private static readonly string[] KanjiMountain = { "嶺", "峰", "岳", "根", "倉", "森", "岩", "尾" };
        private static readonly string[] KanjiHill = { "坂", "根", "森", "谷", "沢", "岡", "木", "立" };
        private static readonly string[] KanjiPlain = { "瀬", "里", "沢", "川", "津", "浦", "田", "江" };
        private static readonly string[] KanjiIsland = { "凪", "浦", "磯", "波", "汐", "崎", "瀬", "根" };
        private static readonly string[] KanjiCold = { "見", "根", "野", "岳", "沢", "滝", "峰", "森" };
        private static readonly string[] KanjiDry = { "岩", "谷", "原", "根", "塚", "路", "崎", "台" };
        private static readonly string[] KanjiSite = { "見", "守", "鳴", "立", "越", "待", "渡", "映", "隠", "呼" };

        // 並ぶと別の意味に読める並びは作らない
        private static readonly string[] Blocked =
        {
            "バカ", "アホ", "シネ", "クソ", "ウンコ", "チンコ", "マンコ", "エロ", "ハゲ", "デブ", "ゲロ", "キモ", "ウザ",
            "コロス", "シヌ", "ガン", "ゴミ", "ザコ", "ボケ", "カス", "サイレン", "バット",
        };

        /// <summary>地域のエリアと探索地点へ、指定の作り方で呼び名を付け直す。</summary>
        public static void Apply(RegionData data, RegionNameStyle style)
        {
            for (int a = 0; a < data.Areas.Count; a++) data.Areas[a].NameOrigin = null;
            for (int s = 0; s < data.Sites.Count; s++) data.Sites[s].NameOrigin = null;
            if (style == RegionNameStyle.Systematic)
            {
                RegionNaming.Apply(data, RegionNamingRules.Parse(data.Spec.NamingRulesText));
                return;
            }
            if (style == RegionNameStyle.Descriptive)
            {
                RegionAreaBuilder.NameByDirection(data.Areas);
                RegionSiteGenerator.NameByFeature(data);
                return;
            }

            uint seed = unchecked((uint)data.Spec.Seed * 2246822519u + 3266489917u + (uint)style * 668265263u);
            var used = new HashSet<string>();
            for (int a = 0; a < data.Areas.Count; a++)
            {
                RegionArea area = data.Areas[a];
                area.Name = Unique(style, seed, area.Id, area.Kind, false, used) + RegionAreaBuilder.KindNoun(area.Kind);
            }
            for (int s = 0; s < data.Sites.Count; s++)
            {
                RegionSite site = data.Sites[s];
                site.Feature = Unique(style, seed, 1000 + site.Id, RegionAreaKind.Plain, true, used) + "の" + StructureNoun(site.Kind);
                site.Name = site.Feature;
            }
        }

        /// <summary>構造物の種類の語（呼び名の後ろ半分）。</summary>
        public static string StructureNoun(RegionSiteKind kind)
        {
            switch (kind)
            {
                case RegionSiteKind.Monolith: return "モノリス";
                case RegionSiteKind.Arch: return "アーチ";
                case RegionSiteKind.Ring: return "環";
                case RegionSiteKind.Tower: return "塔";
                case RegionSiteKind.Dome: return "ドーム";
                case RegionSiteKind.Terrace: return "段丘";
                default: return "沈んだ遺構";
            }
        }

        private static string Unique(RegionNameStyle style, uint seed, int key, RegionAreaKind kind, bool site, HashSet<string> used)
        {
            for (int attempt = 0; attempt < 64; attempt++)
            {
                string name = Make(style, seed, key, attempt, kind, site);
                if (name == null || used.Contains(name)) continue;
                used.Add(name);
                return name;
            }
            // 64 回とも重なることは無いはずだが、止まらないように番号で逃がす
            string fallback = Make(style, seed, key, 0, kind, site) + key;
            used.Add(fallback);
            return fallback;
        }

        private static string Make(RegionNameStyle style, uint seed, int key, int attempt, RegionAreaKind kind, bool site)
        {
            uint local = seed + (uint)attempt * 40503u;
            if (style == RegionNameStyle.Kanji)
            {
                string[] second = site ? KanjiSite : KanjiSecond(kind);
                string a = Pick(KanjiFirst, key, 1, local);
                string b = Pick(second, key, 2, local);
                return a == b ? null : a + b;
            }

            bool soft = style == RegionNameStyle.SoftKana;
            string[] start = soft ? SoftStart : HardStart;
            string[] mid = soft ? SoftMid : HardMid;
            string[] end = soft ? SoftEnd : HardEnd;
            // 真ん中の音は 0〜2 個。エリアは少し長め、地点は少し短め
            float roll = RegionNoise.Hash01(key, 7, local);
            int middle = site ? (roll < 0.45f ? 0 : (roll < 0.9f ? 1 : 2)) : (roll < 0.2f ? 0 : (roll < 0.8f ? 1 : 2));
            string name = Pick(start, key, 1, local);
            for (int m = 0; m < middle; m++) name += Pick(mid, key, 10 + m, local);
            name += Pick(end, key, 2, local);
            return Sounds(name) ? name : null;
        }

        private static string[] KanjiSecond(RegionAreaKind kind)
        {
            switch (kind)
            {
                case RegionAreaKind.Mountains:
                case RegionAreaKind.Highland: return KanjiMountain;
                case RegionAreaKind.Hills: return KanjiHill;
                case RegionAreaKind.Island: return KanjiIsland;
                case RegionAreaKind.Snowfield: return KanjiCold;
                case RegionAreaKind.Desert: return KanjiDry;
                default: return KanjiPlain;
            }
        }

        private static string Pick(string[] list, int key, int slot, uint seed)
        {
            int index = (int)(RegionNoise.Hash01(key, slot, seed) * list.Length);
            if (index >= list.Length) index = list.Length - 1;
            return list[index];
        }

        /// <summary>読める並びか（同じ音の連続、長音や「ン」の重なり、短すぎ・長すぎ、避ける語を落とす）。</summary>
        private static bool Sounds(string name)
        {
            if (name.Length < 3 || name.Length > 6) return false;
            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                char p = name[i - 1];
                if (c == p) return false;
                if (c == 'ー' && (p == 'ン' || p == 'ッ')) return false;
                if (c == 'ッ' && (p == 'ー' || p == 'ン')) return false;
                if (c == 'ン' && p == 'ー') return false;
                // 小さい字と長音は、前の音が同じ段でないと読みにくいので、母音の直後には置かない
                if ((c == 'ィ' || c == 'ァ' || c == 'ェ' || c == 'ォ') && (p == 'ー' || p == 'ン')) return false;
            }
            if (name[name.Length - 1] == 'ー' || name[name.Length - 1] == 'ッ') return false;
            for (int b = 0; b < Blocked.Length; b++)
            {
                if (name.IndexOf(Blocked[b], StringComparison.Ordinal) >= 0) return false;
            }
            return true;
        }
    }
}
