using NUnit.Framework;
using Vastcore.RegionMap;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// 地域生成と探索の、後から依存される約束だけを確かめる
    /// （同じ seed は同じ地域、外周は海、地点は離して置く、サーチの判定）。見た目は対象外。
    /// </summary>
    public class RegionMapTests
    {
        private static RegionSpec SmallSpec(int seed)
        {
            return new RegionSpec { Seed = seed, Resolution = 128, ErosionIterations = 8, SiteCount = 10 };
        }

        [Test]
        public void Generate_SameSeed_ProducesSameRegion()
        {
            RegionData a = RegionGenerator.Generate(SmallSpec(11));
            RegionData b = RegionGenerator.Generate(SmallSpec(11));
            CollectionAssert.AreEqual(a.Height, b.Height);
            Assert.AreEqual(a.Sites.Count, b.Sites.Count);
            for (int s = 0; s < a.Sites.Count; s++)
            {
                Assert.AreEqual(a.Sites[s].MapX, b.Sites[s].MapX);
                Assert.AreEqual(a.Sites[s].MapY, b.Sites[s].MapY);
                Assert.AreEqual(a.Sites[s].Name, b.Sites[s].Name);
            }
        }

        [Test]
        public void Generate_DifferentSeed_ProducesDifferentRegion()
        {
            RegionData a = RegionGenerator.Generate(SmallSpec(11));
            RegionData b = RegionGenerator.Generate(SmallSpec(12));
            CollectionAssert.AreNotEqual(a.Height, b.Height);
        }

        [Test]
        public void Generate_OuterEdgeIsSea_AndLandExists()
        {
            RegionData data = RegionGenerator.Generate(SmallSpec(7));
            int n = data.Resolution;
            for (int k = 0; k < n; k++)
            {
                Assert.LessOrEqual(data.Height[data.Index(k, 0)], 0f);
                Assert.LessOrEqual(data.Height[data.Index(k, n - 1)], 0f);
                Assert.LessOrEqual(data.Height[data.Index(0, k)], 0f);
                Assert.LessOrEqual(data.Height[data.Index(n - 1, k)], 0f);
            }
            Assert.Greater(data.LandFraction, 0.05f);
            Assert.Greater(data.MaxHeight, 100f);
        }

        [Test]
        public void Sites_AreSpacedApart_AndInsideMap()
        {
            RegionData data = RegionGenerator.Generate(SmallSpec(7));
            Assert.Greater(data.Sites.Count, 0);
            for (int a = 0; a < data.Sites.Count; a++)
            {
                RegionSite site = data.Sites[a];
                Assert.That(site.MapX, Is.InRange(0f, RegionData.MapUnits));
                Assert.That(site.MapY, Is.InRange(0f, RegionData.MapUnits));
                Assert.AreEqual(a + 1, site.Id);
                for (int b = a + 1; b < data.Sites.Count; b++)
                {
                    float dx = site.MapX - data.Sites[b].MapX;
                    float dy = site.MapY - data.Sites[b].MapY;
                    Assert.Greater(dx * dx + dy * dy, 8.9f * 8.9f);
                }
            }
        }

        [Test]
        public void Search_ReportsSignalByDistance_AndRecordsDiscovery()
        {
            RegionData data = RegionGenerator.Generate(SmallSpec(7));
            var session = new ExplorationSession(data);
            RegionSite site = data.Sites[0];

            // 他の地点は 9 以上離れているので、東西どちらかへずらした位置では site がいちばん近い
            float direction = site.MapX < 50f ? 1f : -1f;
            Assert.AreEqual(SearchSignal.Strong, session.Search(site.MapX + direction * 4f, site.MapY).Signal);
            Assert.AreEqual(0, session.Discovered.Count);

            SearchResult found = session.Search(site.MapX + direction * 1f, site.MapY);
            Assert.AreEqual(SearchSignal.Found, found.Signal);
            Assert.AreSame(site, found.Site);
            Assert.AreEqual(1, session.Discovered.Count);
            Assert.IsTrue(session.IsDiscovered(site));
            Assert.AreEqual(2, session.SearchCount);

            // 発見済みの地点には二度と反応しない
            Assert.AreNotEqual(SearchSignal.Found, session.Search(site.MapX, site.MapY).Signal);
        }

        [Test]
        public void Session_RestoresDiscoveredSites()
        {
            RegionData data = RegionGenerator.Generate(SmallSpec(7));
            var first = new ExplorationSession(data);
            first.Search(data.Sites[0].MapX, data.Sites[0].MapY);
            first.Search(data.Sites[1].MapX, data.Sites[1].MapY);

            var second = new ExplorationSession(data);
            second.Restore(first.SaveDiscoveredIds());
            Assert.AreEqual(2, second.Discovered.Count);
            Assert.IsTrue(second.IsDiscovered(data.Sites[0]));
            Assert.IsTrue(second.IsDiscovered(data.Sites[1]));
        }

        [Test]
        public void Render_ReturnsOpaqueImageOfRegionSize()
        {
            RegionData data = RegionGenerator.Generate(SmallSpec(7));
            foreach (RegionMapStyle style in System.Enum.GetValues(typeof(RegionMapStyle)))
            {
                byte[] pixels = RegionMapRenderer.Render(data, new RegionRenderOptions { Style = style });
                Assert.AreEqual(data.Resolution * data.Resolution * 4, pixels.Length);
                Assert.AreEqual(255, pixels[3]);
                Assert.AreEqual(255, pixels[pixels.Length - 1]);
            }
        }
    }
}
