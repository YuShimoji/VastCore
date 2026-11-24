using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Vastcore.RegionMap;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// 探索画面で拡大した時の細部の地形（RegionDetailLayer）の、窓の決め方・縁の溶かし方・作った結果と、
    /// 一定の太さの輪（RegionRingGraphic）。画面に出た見え方は対象外。
    /// </summary>
    public class RegionDetailLayerTests
    {
        private static RegionData SmallRegion()
        {
            return RegionGenerator.Generate(new RegionSpec { Seed = 7, Resolution = 128, ErosionIterations = 8, SiteCount = 10 });
        }

        [Test]
        public void Window_LevelDoublesWithZoom_AndWindowCoversTheVisibleWidth()
        {
            Assert.AreEqual(2, RegionDetailLayer.LevelOf(RegionDetailLayer.MinZoom));
            Assert.AreEqual(2, RegionDetailLayer.LevelOf(7.99f));
            Assert.AreEqual(3, RegionDetailLayer.LevelOf(8f));
            Assert.AreEqual(6, RegionDetailLayer.LevelOf(64f));

            // 段の下限の拡大率で見える幅（地域の幅 ÷ 拡大率）の 1.25 倍。上の段ほど狭い
            Assert.AreEqual(80f, RegionDetailLayer.WindowKmFor(256f, 2), 0.001f);
            Assert.AreEqual(40f, RegionDetailLayer.WindowKmFor(256f, 3), 0.001f);
            Assert.AreEqual(5f, RegionDetailLayer.WindowKmFor(256f, 6), 0.001f);
            for (int level = 2; level <= 6; level++)
            {
                float lowestZoom = Mathf.Pow(2f, level);
                float visibleWidthKm = 256f / lowestZoom;
                Assert.GreaterOrEqual(RegionDetailLayer.WindowKmFor(256f, level), visibleWidthKm, "窓が見える幅に足りない（段 " + level + "）");
            }
            // 地域より広い窓は作らない
            Assert.AreEqual(256f, RegionDetailLayer.WindowKmFor(256f, 0), 0.001f);
        }

        [Test]
        public void Window_ReachesPhotoScaleCells_FromZoom128()
        {
            Assert.AreEqual(7, RegionDetailLayer.LevelOf(128f));
            Assert.AreEqual(9, RegionDetailLayer.LevelOf(512f));
            Assert.AreEqual(2.5f, RegionDetailLayer.WindowKmFor(256f, 7), 0.001f);
            Assert.AreEqual(0.625f, RegionDetailLayer.WindowKmFor(256f, 9), 0.001f);

            // 現代試作の街の細部（RegionDevelopmentSiteRendering）は 1 セル 3 m 以下の窓で働く。×64 の段は届かず、×128 の段から届く
            const float SiteDetailCellMeters = 3f;
            Assert.Greater(RegionDetailLayer.WindowKmFor(256f, 6) * 1000f / RegionDetailLayer.Resolution, SiteDetailCellMeters);
            Assert.LessOrEqual(RegionDetailLayer.WindowKmFor(256f, 7) * 1000f / RegionDetailLayer.Resolution, SiteDetailCellMeters);

            // 小さい地域を深く拡大しても、作れる下限（0.5 km）より狭い窓は求めない
            Assert.AreEqual(0.5f, RegionDetailLayer.WindowKmFor(64f, 9), 0.001f);
        }

        [Test]
        public void Feather_FadesOnlyTheRim_AndKeepsTheInsideOpaque()
        {
            const int Size = 100;
            var pixels = new byte[Size * Size * 4];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = 255;
            RegionDetailLayer.Feather(pixels, Size);

            Assert.AreEqual(255, pixels[(50 + 50 * Size) * 4 + 3], "中心は不透明のまま");
            int edge = pixels[(0 + 50 * Size) * 4 + 3];
            int nearEdge = pixels[(2 + 50 * Size) * 4 + 3];
            Assert.Less(edge, 120, "縁はほぼ透明");
            Assert.Less(edge, nearEdge, "内側ほど濃い");
            Assert.Less(nearEdge, 255);
            Assert.AreEqual(255, pixels[(10 + 50 * Size) * 4 + 3], "溶かす幅の外は不透明");
            // 色は変えない
            Assert.AreEqual(255, pixels[(0 + 50 * Size) * 4]);
        }

        [Test]
        public void Make_ReturnsAFeatheredWindow_ThatCoversItsCenter_AndKeepsTheCoast()
        {
            RegionData region = SmallRegion();
            RegionSite onLand = null;
            for (int s = 0; s < region.Sites.Count && onLand == null; s++)
            {
                if (region.Sites[s].HeightMeters > 20f) onLand = region.Sites[s];
            }
            Assert.IsNotNull(onLand);

            var render = new RegionRenderOptions { Style = RegionMapStyle.Atlas, CloudCover = 0.58f, Exaggeration = 2.6f };
            RegionDetailLayer.Result result = RegionDetailLayer.Make(region, onLand.MapX, onLand.MapY, 40f, 160, render);
            Assert.AreEqual(160 * 160 * 4, result.Pixels.Length);
            Assert.AreEqual(RegionMapStyle.Atlas, result.Style);
            Assert.Less(result.Pixels[3], 30, "窓の角はほぼ透明");
            Assert.AreEqual(255, result.Pixels[(80 + 80 * 160) * 4 + 3], "窓の中心は不透明");
            Assert.Greater(result.Detail.CoastAgreement, 0.9f);

            float halfMap = result.Detail.SizeMap * 0.5f;
            Assert.IsTrue(RegionDetailLayer.Covers(result.Detail, onLand.MapX, onLand.MapY, halfMap * 0.5f, halfMap * 0.5f));
            Assert.IsFalse(RegionDetailLayer.Covers(result.Detail, onLand.MapX, onLand.MapY, halfMap * 3f, halfMap * 0.5f), "窓より広い範囲は覆えない");
            Assert.IsFalse(RegionDetailLayer.Covers(null, 50f, 50f, 1f, 1f));

            // 描き直しは窓を作り直さず、描き方だけを替える
            RegionDetailLayer.Result redrawn = RegionDetailLayer.Redraw(result.Detail, new RegionRenderOptions { Style = RegionMapStyle.Satellite });
            Assert.AreSame(result.Detail, redrawn.Detail);
            Assert.IsTrue(redrawn.Restyled);
            Assert.AreEqual(RegionMapStyle.Satellite, redrawn.Style);
            CollectionAssert.AreNotEqual(result.Pixels, redrawn.Pixels);
        }

        [Test]
        public void DetailOptions_DropClouds_AndSoftenTheRelief()
        {
            RegionRenderOptions options = RegionDetailLayer.DetailOptions(new RegionRenderOptions { Style = RegionMapStyle.Relief, CloudCover = 0.9f, Exaggeration = 2.6f });
            Assert.AreEqual(RegionMapStyle.Relief, options.Style);
            Assert.AreEqual(0f, options.CloudCover);
            Assert.AreEqual(RegionDetailLayer.DetailExaggeration, options.Exaggeration);
        }

        [Test]
        public void Ring_KeepsItsLineWidth_AtAnySize()
        {
            float small = RingSpan(400f);
            float large = RingSpan(6000f);
            Assert.AreEqual(small, large, 0.05f, "輪の太さが大きさに比例している");
            Assert.AreEqual(3f + 2f * 1.5f, large, 0.05f);
        }

        private static float RingSpan(float diameter)
        {
            var go = new GameObject("ring", typeof(RectTransform), typeof(RegionRingGraphic));
            try
            {
                var ring = go.GetComponent<RegionRingGraphic>();
                ring.rectTransform.sizeDelta = new Vector2(diameter, diameter);
                MethodInfo populate = typeof(RegionRingGraphic).GetMethod("OnPopulateMesh", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(VertexHelper) }, null);
                Assert.IsNotNull(populate);
                using (var helper = new VertexHelper())
                {
                    populate.Invoke(ring, new object[] { helper });
                    Assert.Greater(helper.currentVertCount, 0);
                    float low = float.MaxValue;
                    float high = float.MinValue;
                    var vertex = new UIVertex();
                    for (int v = 0; v < helper.currentVertCount; v++)
                    {
                        helper.PopulateUIVertex(ref vertex, v);
                        float radius = ((Vector2)vertex.position).magnitude;
                        if (radius < low) low = radius;
                        if (radius > high) high = radius;
                    }
                    Assert.AreEqual(diameter * 0.5f + 1.5f, high, 0.05f, "外の縁");
                    return high - low;
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
