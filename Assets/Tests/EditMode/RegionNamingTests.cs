using System.Collections.Generic;
using NUnit.Framework;
using Vastcore.RegionMap;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// 命名の規則と、川の上の地点を川岸へ移す処理の、後から依存される約束だけを確かめる
    /// （同じ seed は同じ名前、名前は重ならない、同じ意味は同じ語根、規則の書き換えが効く、由来が残る）。
    /// 名前の響きの良し悪しは対象外。
    /// </summary>
    public class RegionNamingTests
    {
        private static RegionSpec SmallSpec(int seed)
        {
            return new RegionSpec { Seed = seed, Resolution = 128, ErosionIterations = 8, SiteCount = 10, NameStyle = RegionNameStyle.Systematic };
        }

        [Test]
        public void Systematic_IsDeterministic_Unique_AndRecordsOrigin()
        {
            RegionData a = RegionGenerator.Generate(SmallSpec(7));
            RegionData b = RegionGenerator.Generate(SmallSpec(7));
            var names = new HashSet<string>();
            Assert.Greater(a.Areas.Count, 0);
            Assert.Greater(a.Sites.Count, 0);
            for (int k = 0; k < a.Areas.Count; k++)
            {
                Assert.AreEqual(a.Areas[k].Name, b.Areas[k].Name);
                Assert.IsTrue(names.Add(a.Areas[k].Name), "同じ名前のエリアがある: " + a.Areas[k].Name);
                Assert.IsFalse(string.IsNullOrEmpty(a.Areas[k].NameOrigin), "由来が無い: " + a.Areas[k].Name);
                StringAssert.DoesNotContain("番号で代用", a.Areas[k].NameOrigin);
            }
            for (int k = 0; k < a.Sites.Count; k++)
            {
                Assert.AreEqual(a.Sites[k].Name, b.Sites[k].Name);
                Assert.IsTrue(names.Add(a.Sites[k].Name), "同じ名前の地点がある: " + a.Sites[k].Name);
                Assert.IsFalse(string.IsNullOrEmpty(a.Sites[k].NameOrigin), "由来が無い: " + a.Sites[k].Name);
                StringAssert.DoesNotContain("番号で代用", a.Sites[k].NameOrigin);
            }

            // 前の作り方へ戻すと、由来は消える
            RegionNames.Apply(a, RegionNameStyle.Descriptive);
            Assert.IsNull(a.Areas[0].NameOrigin);
            Assert.IsNull(a.Sites[0].NameOrigin);
        }

        [Test]
        public void Systematic_SameMeaning_UsesSameRoot_InEveryName()
        {
            RegionData data = RegionGenerator.Generate(SmallSpec(7));
            List<RegionNameRoot> roots = RegionNaming.Roots(RegionNamingRules.Default, 7);
            var sounds = new HashSet<string>();
            int checkedNames = 0;
            for (int r = 0; r < roots.Count; r++)
            {
                Assert.IsTrue(sounds.Add(roots[r].Sound), "同じ音の語根が 2 つある: " + roots[r].Sound);
                string mark = roots[r].Sound + "（" + roots[r].Meaning + "）";
                for (int k = 0; k < data.Areas.Count; k++)
                {
                    if (data.Areas[k].NameOrigin.IndexOf("（" + roots[r].Meaning + "）", System.StringComparison.Ordinal) < 0) continue;
                    StringAssert.Contains(mark, data.Areas[k].NameOrigin);
                    StringAssert.Contains(roots[r].Sound, data.Areas[k].Name);
                    checkedNames++;
                }
                for (int k = 0; k < data.Sites.Count; k++)
                {
                    if (data.Sites[k].NameOrigin.IndexOf("（" + roots[r].Meaning + "）", System.StringComparison.Ordinal) < 0) continue;
                    StringAssert.Contains(mark, data.Sites[k].NameOrigin);
                    StringAssert.Contains(roots[r].Sound, data.Sites[k].Name);
                    checkedNames++;
                }
            }
            Assert.Greater(checkedNames, 0);
        }

        [Test]
        public void Rules_WrittenRoot_FixedName_AndNoun_AreUsed()
        {
            RegionData plain = RegionGenerator.Generate(SmallSpec(7));
            string text = RegionNamingRules.DefaultText
                .Replace("\n川 =", "\n川 = ミナ")
                .Replace("Terrace = 段丘", "Terrace = 棚")
                + "\n7/エリア1 = 試しの名\n7/地点1 = 試しの地点\n";
            RegionNamingRules rules = RegionNamingRules.Parse(text);
            Assert.AreEqual(0, rules.Problems.Count, string.Join(" / ", rules.Problems.ToArray()));

            RegionSpec spec = SmallSpec(7);
            spec.NamingRulesText = text;
            RegionData data = RegionGenerator.Generate(spec);
            Assert.AreEqual("試しの名", data.Areas[0].Name);
            Assert.AreEqual("試しの地点", data.Sites[0].Name);
            Assert.AreEqual(plain.Sites.Count, data.Sites.Count);

            List<RegionNameRoot> roots = RegionNaming.Roots(rules, 7);
            RegionNameRoot river = roots.Find(r => r.Meaning == "川");
            Assert.AreEqual("ミナ", river.Sound);
            Assert.IsTrue(river.Written);
            for (int k = 0; k < data.Sites.Count; k++)
            {
                if (data.Sites[k].NameOrigin.IndexOf("（川）", System.StringComparison.Ordinal) >= 0) StringAssert.Contains("ミナ", data.Sites[k].Name);
                if (data.Sites[k].Kind == RegionSiteKind.Terrace && k > 0) StringAssert.EndsWith("棚", data.Sites[k].Name);
            }
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

            Assert.AreEqual(0, RegionNamingRules.Default.Problems.Count, string.Join(" / ", RegionNamingRules.Default.Problems.ToArray()));
            Assert.AreSame(RegionNamingRules.Default, RegionNamingRules.Parse(null));
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
