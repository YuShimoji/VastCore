using System.Collections.Generic;
using NUnit.Framework;
using Vastcore.RegionMap;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// 命名の規則と、川の上の地点を川岸へ移す処理の、後から依存される約束だけを確かめる
    /// （同じ seed は同じ名前、名前は重ならない、同じ意味は同じ語根、音を付け替えても骨格は同じ、
    /// 土地の経緯で名前の種類が変わる、規則の書き換えが効く、由来が残る、
    /// 経緯と音の質感は地域ごとに 1 つ、文明のあるエリアから近い高い峰には土地の名も付く）。名前の響きの良し悪しは対象外。
    /// </summary>
    public class RegionNamingTests
    {
        private static RegionSpec SmallSpec(int seed)
        {
            return new RegionSpec { Seed = seed, Resolution = 128, ErosionIterations = 8, SiteCount = 10, NameStyle = RegionNameStyle.Systematic };
        }

        private static string Rules(string from, string to)
        {
            string text = RegionNamingRules.DefaultText.Replace("\r\n", "\n");
            StringAssert.Contains(from, text);
            return text.Replace(from, to);
        }

        private static RegionData Generate(int seed, string rules, string sound = null)
        {
            RegionSpec spec = SmallSpec(seed);
            spec.NamingRulesText = rules;
            spec.NamingSound = sound;
            return RegionGenerator.Generate(spec);
        }

        /// <summary>地域・エリア・川・峰・湾・地点の、名前の並びを 1 列にする。</summary>
        private static List<List<RegionPlaceName>> AllNames(RegionData data)
        {
            var all = new List<List<RegionPlaceName>>();
            for (int k = 0; k < data.Features.Count; k++) all.Add(data.Features[k].Names);
            for (int k = 0; k < data.Areas.Count; k++) all.Add(data.Areas[k].Names);
            for (int k = 0; k < data.Sites.Count; k++) all.Add(data.Sites[k].Names);
            return all;
        }

        [Test]
        public void Systematic_IsDeterministic_Unique_AndRecordsOrigin()
        {
            RegionData a = RegionGenerator.Generate(SmallSpec(7));
            RegionData b = RegionGenerator.Generate(SmallSpec(7));
            var names = new HashSet<string>();
            Assert.Greater(a.Areas.Count, 0);
            Assert.Greater(a.Sites.Count, 0);
            Assert.AreEqual(0, a.Naming.Problems.Count, string.Join(" / ", a.Naming.Problems.ToArray()));
            for (int k = 0; k < a.Areas.Count; k++)
            {
                Assert.AreEqual(a.Areas[k].Name, b.Areas[k].Name);
                Assert.IsTrue(names.Add(a.Areas[k].Name), "同じ名前のエリアがある: " + a.Areas[k].Name);
                Assert.IsFalse(string.IsNullOrEmpty(a.Areas[k].NameOrigin), "由来が無い: " + a.Areas[k].Name);
            }
            for (int k = 0; k < a.Sites.Count; k++)
            {
                Assert.AreEqual(a.Sites[k].Name, b.Sites[k].Name);
                Assert.IsTrue(names.Add(a.Sites[k].Name), "同じ名前の地点がある: " + a.Sites[k].Name);
                Assert.IsFalse(string.IsNullOrEmpty(a.Sites[k].NameOrigin), "由来が無い: " + a.Sites[k].Name);
            }

            // 地域そのものにも名前が付く。どの名前にも、種類・骨格・由来・短い由来がある
            Assert.AreEqual(RegionFeatureKind.Region, a.Features[0].Kind);
            Assert.IsFalse(string.IsNullOrEmpty(a.Features[0].Name));
            List<List<RegionPlaceName>> all = AllNames(a);
            List<List<RegionPlaceName>> again = AllNames(b);
            for (int p = 0; p < all.Count; p++)
            {
                Assert.Greater(all[p].Count, 0);
                Assert.AreEqual(all[p].Count, again[p].Count);
                for (int n = 0; n < all[p].Count; n++)
                {
                    Assert.AreEqual(all[p][n].Text, again[p][n].Text);
                    Assert.IsFalse(string.IsNullOrEmpty(all[p][n].Text));
                    Assert.IsFalse(string.IsNullOrEmpty(all[p][n].Origin), "由来が無い: " + all[p][n].Text);
                    Assert.IsFalse(string.IsNullOrEmpty(all[p][n].Gloss), "短い由来が無い: " + all[p][n].Text);
                    Assert.IsFalse(string.IsNullOrEmpty(all[p][n].Skeleton), "骨格が無い: " + all[p][n].Text);
                }
            }

            // 前の作り方へ戻すと、由来と名前の並びは消える
            RegionNames.Apply(a, RegionNameStyle.Descriptive);
            Assert.IsNull(a.Areas[0].NameOrigin);
            Assert.IsNull(a.Sites[0].NameOrigin);
            Assert.AreEqual(0, a.Areas[0].Names.Count);
            Assert.IsNull(a.Naming);
        }

        [Test]
        public void Systematic_SameMeaning_UsesSameRoot_InEveryName()
        {
            RegionData data = Generate(7, Rules("地域 = 地域ごと", "地域 = 文明あり"));
            List<RegionNameRoot> roots = data.Naming.Roots;
            var sounds = new HashSet<string>();
            int checkedNames = 0;
            List<List<RegionPlaceName>> all = AllNames(data);
            for (int r = 0; r < roots.Count; r++)
            {
                Assert.IsTrue(sounds.Add(roots[r].Sound), "同じ音の語根が 2 つある: " + roots[r].Sound);
                string mark = roots[r].Sound + "（" + roots[r].Meaning + "）";
                int used = 0;
                for (int p = 0; p < all.Count; p++)
                {
                    for (int n = 0; n < all[p].Count; n++)
                    {
                        if (all[p][n].Kind != RegionNameKind.Proper) continue;
                        if (all[p][n].Origin.IndexOf("（" + roots[r].Meaning + "）", System.StringComparison.Ordinal) < 0) continue;
                        StringAssert.Contains(mark, all[p][n].Origin);
                        used++;
                    }
                }
                Assert.AreEqual(roots[r].Uses, used, "語根「" + roots[r].Meaning + "」を使った名前の数");
                checkedNames += used;
            }
            Assert.Greater(checkedNames, 0);
        }

        [Test]
        public void Skeleton_StaysTheSame_WhenTheSoundIsSwapped()
        {
            RegionData standard = Generate(7, null, "標準");
            RegionData hard = Generate(7, null, "かたい");
            Assert.AreEqual("標準", standard.Naming.Sound);
            Assert.AreEqual("かたい", hard.Naming.Sound);
            List<List<RegionPlaceName>> a = AllNames(standard);
            List<List<RegionPlaceName>> b = AllNames(hard);
            Assert.AreEqual(a.Count, b.Count);
            int changed = 0;
            for (int p = 0; p < a.Count; p++)
            {
                Assert.AreEqual(a[p].Count, b[p].Count);
                for (int n = 0; n < a[p].Count; n++)
                {
                    Assert.AreEqual(a[p][n].Kind, b[p][n].Kind);
                    Assert.AreEqual(a[p][n].Skeleton, b[p][n].Skeleton, "音を付け替えたら骨格が変わった: " + a[p][n].Text);
                    if (a[p][n].Kind == RegionNameKind.Designation) Assert.AreEqual(a[p][n].Text, b[p][n].Text);
                    if (a[p][n].Kind == RegionNameKind.Proper && a[p][n].Text != b[p][n].Text) changed++;
                }
            }
            Assert.Greater(changed, 0, "音を付け替えても土地の名が 1 つも変わらなかった");
        }

        [Test]
        public void History_Decides_WhichKindsOfNames_APlaceGets()
        {
            RegionData unexplored = Generate(7, Rules("地域 = 地域ごと", "地域 = 未踏"));
            foreach (List<RegionPlaceName> names in AllNames(unexplored))
            {
                Assert.AreEqual(1, names.Count);
                Assert.AreEqual(RegionNameKind.Designation, names[0].Kind);
            }
            for (int k = 0; k < unexplored.Areas.Count; k++) Assert.AreEqual(RegionLandHistory.Unexplored, unexplored.Areas[k].History);

            RegionData settled = Generate(7, Rules("地域 = 地域ごと", "地域 = 文明あり"));
            for (int k = 0; k < settled.Areas.Count; k++)
            {
                List<RegionPlaceName> names = settled.Areas[k].Names;
                Assert.AreEqual(RegionNameKind.Proper, names[0].Kind, "文明のあるエリアの表の名前が土地の名でない: " + names[0].Text);
                Assert.AreEqual(RegionNameKind.Designation, names[names.Count - 1].Kind);
                Assert.IsFalse(names.Exists(n => n.Kind == RegionNameKind.Old));
            }

            RegionData ancient = Generate(7, Rules("地域 = 地域ごと", "地域 = 古い文明"));
            int old = 0;
            foreach (List<RegionPlaceName> names in AllNames(ancient)) old += names.FindAll(n => n.Kind == RegionNameKind.Old).Count;
            Assert.Greater(old, 0, "古い文明の土地に古い呼び名が 1 つも無い");

            // エリアごとに決めた経緯は、地域の経緯より優先する
            RegionData mixed = Generate(7, Rules("# エリアごとに決める時", "7/エリア1 = 未踏\n# エリアごとに決める時").Replace("地域 = 地域ごと", "地域 = 文明あり"));
            Assert.AreEqual(RegionLandHistory.Unexplored, mixed.Areas[0].History);
            Assert.AreEqual(RegionNameKind.Designation, mixed.Areas[0].Names[0].Kind);
            Assert.AreEqual(RegionLandHistory.Settled, mixed.Areas[1].History);
        }

        [Test]
        public void History_AndSound_AreChosenOncePerRegion()
        {
            var histories = new HashSet<string>();
            var sounds = new HashSet<string>();
            for (int seed = 1; seed <= 16; seed++)
            {
                RegionData data = RegionGenerator.Generate(SmallSpec(seed));
                Assert.IsTrue(data.Naming.HistoryFromSeed);
                Assert.IsTrue(data.Naming.SoundFromSeed);
                Assert.GreaterOrEqual(System.Array.IndexOf(RegionNamingRules.Histories, data.Naming.History), 0, "地域の経緯が 1 つに決まっていない: " + data.Naming.History);
                // 地域の中のどのエリアも、地域の経緯と同じ
                for (int k = 0; k < data.Areas.Count; k++) Assert.AreEqual(data.Naming.History, RegionNaming.HistoryLabel(data.Areas[k].History), "seed " + seed + " のエリア " + data.Areas[k].Id);
                Assert.AreEqual(0, data.Naming.PeaksNamedFromNearby);
                histories.Add(data.Naming.History);
                sounds.Add(data.Naming.Sound);

                RegionData again = RegionGenerator.Generate(SmallSpec(seed));
                Assert.AreEqual(data.Naming.History, again.Naming.History);
                Assert.AreEqual(data.Naming.Sound, again.Naming.Sound);
            }
            Assert.AreEqual(3, histories.Count, "16 の地域で、3 つの経緯がそろわない");
            Assert.Greater(sounds.Count, 1, "16 の地域で、音の質感が 1 つしか選ばれない");

            // 人が決めた地域の経緯と音の質感は、seed の選択より優先する
            RegionData decided = Generate(7, Rules("# エリアごとに決める時", "7/地域 = 古い文明\n# エリアごとに決める時"), "かたい");
            Assert.AreEqual("古い文明", decided.Naming.History);
            Assert.IsFalse(decided.Naming.HistoryFromSeed);
            Assert.AreEqual("かたい", decided.Naming.Sound);
            Assert.IsFalse(decided.Naming.SoundFromSeed);
            for (int k = 0; k < decided.Areas.Count; k++) Assert.AreEqual(RegionLandHistory.Ancient, decided.Areas[k].History);

            // 規則に音の質感の名前を書けば、どの地域もその質感
            for (int seed = 1; seed <= 6; seed++)
            {
                RegionData written = Generate(seed, Rules("音の質感 = 自動", "音の質感 = 標準"));
                Assert.AreEqual("標準", written.Naming.Sound);
                Assert.IsFalse(written.Naming.SoundFromSeed);
            }
        }

        [Test]
        public void Peaks_NearSettledAreas_GetAProperName_EvenOnUnexploredLand()
        {
            string mixed = Rules("地域 = 地域ごと", "地域 = 混在");
            string off = mixed.Replace("峰に名を付ける距離 = 8", "峰に名を付ける距離 = 0");
            int named = 0;
            for (int seed = 1; seed <= 8; seed++)
            {
                RegionData data = Generate(seed, mixed);
                RegionData plain = Generate(seed, off);
                Assert.AreEqual("混在", data.Naming.History);
                Assert.AreEqual(0, plain.Naming.PeaksNamedFromNearby);
                int here = 0;
                for (int f = 0; f < data.Features.Count; f++)
                {
                    RegionFeature peak = data.Features[f];
                    if (peak.Kind != RegionFeatureKind.Peak) continue;
                    RegionArea area = data.AreaById(peak.AreaId);
                    bool unexploredLand = area == null || area.History == RegionLandHistory.Unexplored;
                    RegionPlaceName proper = peak.Names.Find(n => n.Kind == RegionNameKind.Proper);

                    // 規則を切ると、未踏の土地の峰は記号名だけ
                    RegionFeature same = plain.Features[f];
                    if (unexploredLand)
                    {
                        Assert.AreEqual(1, same.Names.Count, "seed " + seed + " 峰 " + peak.Id);
                        Assert.AreEqual(RegionNameKind.Designation, same.Names[0].Kind);
                    }
                    if (!unexploredLand || peak.History == RegionLandHistory.Unexplored) continue;

                    // 未踏の土地にあって名前を借りた峰: 高く、土地の名が表に出て、記号名が別名に残り、由来に相手のエリアが書かれる
                    here++;
                    Assert.AreEqual(RegionLandHistory.Settled, peak.History);
                    Assert.GreaterOrEqual(peak.HeightMeters, data.MaxHeight * 0.5f);
                    Assert.IsNotNull(proper, "seed " + seed + " 峰 " + peak.Id + " に土地の名が無い");
                    Assert.AreEqual(RegionNameKind.Proper, peak.Names[0].Kind);
                    Assert.IsTrue(peak.Names.Exists(n => n.Kind == RegionNameKind.Designation));
                    StringAssert.Contains("土地の名も付けた", proper.Origin);
                    StringAssert.Contains("文明のあるエリア「", proper.Origin);
                }
                Assert.AreEqual(data.Naming.PeaksNamedFromNearby, here, "seed " + seed);
                named += here;
            }
            Assert.Greater(named, 0, "名前を借りた峰が 1 つも無かった");
        }

        [Test]
        public void Rules_WrittenRoot_FixedName_Noun_AndFacts_AreUsed()
        {
            string text = RegionNamingRules.DefaultText.Replace("\r\n", "\n")
                .Replace("地域 = 地域ごと", "地域 = 文明あり")
                .Replace("[語根]\n", "[語根]\n川 = ミナ\n")
                .Replace("Terrace = 段丘", "Terrace = 棚")
                .Replace("森が広い = 緑の 森", "森が広い = 茂み")
                + "\n7/エリア1 = 試しの名\n7/地点1 = 試しの地点\n";
            RegionNamingRules rules = RegionNamingRules.Parse(text);
            Assert.AreEqual(0, rules.Problems.Count, string.Join(" / ", rules.Problems.ToArray()));

            RegionData plain = RegionGenerator.Generate(SmallSpec(7));
            RegionData data = Generate(7, text);
            Assert.AreEqual("試しの名", data.Areas[0].Name);
            Assert.AreEqual(RegionNameKind.Decided, data.Areas[0].Names[0].Kind);
            Assert.AreEqual("試しの地点", data.Sites[0].Name);
            Assert.AreEqual(plain.Sites.Count, data.Sites.Count);

            RegionNameRoot river = data.Naming.Roots.Find(r => r.Meaning == "川");
            Assert.AreEqual("ミナ", river.Sound);
            Assert.IsTrue(river.Written);
            // 事実から意味への対応を書き換えると、新しい意味に語根が付き、外した意味は使われない
            Assert.IsNotNull(data.Naming.Roots.Find(r => r.Meaning == "茂み"));
            RegionNameRoot green = data.Naming.Roots.Find(r => r.Meaning == "緑の");
            Assert.IsTrue(green == null || green.Uses == 0);

            foreach (List<RegionPlaceName> names in AllNames(data))
            {
                for (int n = 0; n < names.Count; n++)
                {
                    if (names[n].Kind == RegionNameKind.Proper && names[n].Origin.IndexOf("（川）", System.StringComparison.Ordinal) >= 0) StringAssert.Contains("ミナ（川）", names[n].Origin);
                }
            }
            for (int k = 1; k < data.Sites.Count; k++)
            {
                RegionPlaceName first = data.Sites[k].Names[0];
                if (data.Sites[k].Kind == RegionSiteKind.Terrace && first.Kind == RegionNameKind.Proper) StringAssert.EndsWith("棚", first.Text);
            }
        }

        [Test]
        public void Rules_WithOnlyOneSection_AreFilledFromTheDefault()
        {
            // 経緯だけを書いた規則。音・型・雰囲気・出来事は既定で補われ、問題は出ない
            RegionNamingRules rules = RegionNamingRules.Parse("[土地の経緯]\n地域 = 古い文明\n");
            Assert.AreEqual(0, rules.Problems.Count, string.Join(" / ", rules.Problems.ToArray()));
            Assert.AreEqual(RegionNamingRules.Default.Sounds.Count, rules.Sounds.Count);
            Assert.AreEqual(RegionNamingRules.Default.AreaPatterns.Count, rules.AreaPatterns.Count);
            Assert.AreEqual(RegionNamingRules.Default.SitePatterns.Count, rules.SitePatterns.Count);
            Assert.AreEqual(RegionNamingRules.Default.Mood.Count, rules.Mood.Count);
            Assert.AreEqual(RegionNamingRules.Default.Events.Count, rules.Events.Count);

            RegionData data = Generate(7, "[土地の経緯]\n地域 = 古い文明\n");
            Assert.AreEqual(0, data.Naming.Problems.Count, string.Join(" / ", data.Naming.Problems.ToArray()));
            for (int k = 0; k < data.Areas.Count; k++)
            {
                Assert.AreEqual(RegionNameKind.Proper, data.Areas[k].Names[0].Kind, "古い文明のエリアの表の名前が土地の名でない: " + data.Areas[k].Names[0].Text);
                Assert.IsTrue(data.Areas[k].Names.Exists(n => n.Kind == RegionNameKind.Old), "古い呼び名が無い: " + data.Areas[k].Name);
            }

            // 見出しを書いて中身を空にした節は補わない（その部品を使わない、という指定）
            RegionNamingRules noMood = RegionNamingRules.Parse("[雰囲気]\n");
            Assert.AreEqual(0, noMood.Mood.Count);
            Assert.AreEqual(RegionNamingRules.Default.Events.Count, noMood.Events.Count);

            // 音の質感を 1 つだけ書いた規則。既定の質感も残り、付け替えられる
            RegionNamingRules ownSound = RegionNamingRules.Parse("[全体]\n音の質感 = ためし\n[音 ためし]\n頭 = ア カ サ タ ナ ハ マ ヤ ラ ワ\n続き = イ キ シ チ ニ ヒ ミ リ ン\n");
            Assert.AreEqual(0, ownSound.Problems.Count, string.Join(" / ", ownSound.Problems.ToArray()));
            Assert.AreEqual("ためし", ownSound.SoundName);
            Assert.IsNotNull(ownSound.Sound("かたい"));
        }

        [Test]
        public void Rules_Parse_ReportsProblems_WithLineNumbers()
        {
            RegionNamingRules rules = RegionNamingRules.Parse("[音]\n頭 = ア カ\n続き = ラ ン\n速さ = 3\n[エリアの型]\n1 : {特徴}{類}\n[地点の型]\n1 : {構造物}\n2 : {特徴}の{構造物} : 雨の日\nx : {特徴}\n");
            string all = string.Join(" / ", rules.Problems.ToArray());
            StringAssert.Contains("行 4", all);
            StringAssert.Contains("行 8", all);
            StringAssert.Contains("行 9", all);
            StringAssert.Contains("行 10", all);
            Assert.AreEqual(1, rules.AreaPatterns.Count);
            Assert.AreEqual(0, rules.SitePatterns.Count);
            // 書かれていない節（事実と意味、経緯、記号名など）は既定で補う
            Assert.Greater(rules.Facts.Count, 0);
            Assert.Greater(rules.NameKinds.Count, 0);

            Assert.AreEqual(0, RegionNamingRules.Default.Problems.Count, string.Join(" / ", RegionNamingRules.Default.Problems.ToArray()));
            Assert.AreSame(RegionNamingRules.Default, RegionNamingRules.Parse(null));
            Assert.AreEqual(3, RegionNamingRules.Default.Sounds.Count);
        }

        [Test]
        public void SoundProfile_Join_ChangesTheSound_AtTheSeam()
        {
            var profile = new RegionSoundProfile { MergeSame = true, VowelMode = 1, VoiceAfterN = true };
            string note;
            Assert.AreEqual("ケノラ", profile.Join("ケノ", "ノラ", out note));
            Assert.IsNotNull(note);
            Assert.AreEqual("ネローネ", profile.Join("ネロ", "オネ", out note));
            Assert.AreEqual("カンガル", profile.Join("カン", "カル", out note));
            Assert.AreEqual("ケノミナ", profile.Join("ケノ", "ミナ", out note));
            Assert.IsNull(note);
        }

        [Test]
        public void Terrace_Sites_StandOnTheBank_NotOnTheRiver()
        {
            int terraces = 0;
            for (int seed = 5; seed <= 9; seed++)
            {
                var spec = new RegionSpec { Seed = seed, Resolution = 256, ErosionIterations = 12, SiteCount = 18 };
                RegionData data = RegionGenerator.Generate(spec);
                float stream = RegionGenerator.RiverThreshold(data);
                for (int s = 0; s < data.Sites.Count; s++)
                {
                    RegionSite site = data.Sites[s];
                    if (site.Kind != RegionSiteKind.Terrace) continue;
                    terraces++;
                    int cell = data.CellFromMap(site.MapX, site.MapY);
                    Assert.Greater(data.Height[cell], 0f, "段丘が海の上にある");
                    Assert.AreNotEqual(RegionBiome.River, data.Biome[cell], "段丘が川の升の上にある");
                    Assert.LessOrEqual(data.Flow[cell], stream, "段丘が細い流れの上にある");

                    // 川のそばではある（4 升以内に流れがある）
                    int n = data.Resolution;
                    int cx = cell % n;
                    int cy = cell / n;
                    bool near = false;
                    for (int oy = -4; oy <= 4 && !near; oy++)
                    {
                        for (int ox = -4; ox <= 4; ox++)
                        {
                            int x = cx + ox;
                            int y = cy + oy;
                            if (x < 0 || y < 0 || x >= n || y >= n) continue;
                            if (data.Height[x + y * n] > 0f && data.Flow[x + y * n] > stream) near = true;
                        }
                    }
                    Assert.IsTrue(near, "段丘の近くに川が無い");
                }
            }
            Assert.Greater(terraces, 0, "確かめる段丘が 1 つも無かった");
        }
    }
}
