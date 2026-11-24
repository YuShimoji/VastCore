using NUnit.Framework;
using UnityEngine;
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
            Assert.IsNull(a.Development);
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
        public void Lake_KeepsOneWaterLevelAcrossTheRegionAndTheDetailWindow()
        {
            RegionData region = RegionGenerator.Generate(new RegionSpec { Seed = 8 });
            int center = 0;
            for (int i = 1; i < region.LakeDepth.Length; i++)
                if (region.LakeDepth[i] > region.LakeDepth[center]) center = i;
            Assert.Greater(region.LakeDepth[center], 2f);
            float lakeLevel = region.Height[center] + region.LakeDepth[center];
            for (int i = 0; i < region.LakeDepth.Length; i++)
            {
                if (region.LakeDepth[i] <= 0f) continue;
                Assert.AreEqual(RegionBiome.Lake, region.Biome[i]);
                Assert.AreEqual(lakeLevel, region.Height[i] + region.LakeDepth[i], 0.001f);
                int receiver = region.Downstream[i];
                if (receiver >= 0 && region.LakeDepth[receiver] <= 0f)
                    Assert.LessOrEqual(region.Height[receiver], lakeLevel);
            }

            RegionDetail detail = RegionDetailBuilder.Build(region, new RegionDetailSpec
            {
                CenterMapX = region.MapXFromCell(center),
                CenterMapY = region.MapYFromCell(center),
                SizeKm = 8f,
                Resolution = 256
            });
            int detailCenter = detail.Data.Index(128, 128);
            Assert.AreEqual(RegionBiome.Lake, detail.Data.Biome[detailCenter]);
            Assert.AreEqual(lakeLevel, detail.Data.Height[detailCenter] + detail.Data.LakeDepth[detailCenter], 0.001f);
            Assert.AreEqual(RegionBiome.Ocean, region.Biome[0]);
        }

        [Test]
        public void ModernDevelopment_DrawsSiteDetailWhenACellIsFinerThanThreeMeters()
        {
            RegionData region = RegionGenerator.Generate(new RegionSpec { Seed = 9, ModernDevelopment = true });
            RegionPosition city = region.Development.SecondaryCity;
            var spec = new RegionDetailSpec
            {
                CenterMapX = (city.X + 2800f) / (region.KmPerMapUnit * 1000f),
                CenterMapY = city.Y / (region.KmPerMapUnit * 1000f),
                SizeKm = 0.6f,
                Resolution = 256,
            };
            var options = new RegionRenderOptions { Style = RegionMapStyle.Satellite, CloudCover = 0f };
            byte[] first = RegionMapRenderer.Render(RegionDetailBuilder.Build(region, spec).Data, options);
            byte[] second = RegionMapRenderer.Render(RegionDetailBuilder.Build(region, spec).Data, options);
            CollectionAssert.AreEqual(first, second);

            // 同じ場所を 1 画素 3 m より粗く描くと、街区と屋根の面だけになる。細部の描画は色の種類が大きく増える
            spec.SizeKm = 1.6f;
            byte[] coarse = RegionMapRenderer.Render(RegionDetailBuilder.Build(region, spec).Data, options);
            Assert.Greater(Colors(first), Colors(coarse) * 2);

            int Colors(byte[] pixels)
            {
                var seen = new System.Collections.Generic.HashSet<int>();
                for (int p = 0; p < pixels.Length; p += 4)
                    seen.Add((pixels[p] >> 3) | (pixels[p + 1] >> 3) << 5 | (pixels[p + 2] >> 3) << 10);
                return seen.Count;
            }
        }

        [Test]
        public void ModernDevelopment_ConnectsTwoCitiesToAnAirportAndSurvivesZoom()
        {
            RegionData region = RegionGenerator.Generate(new RegionSpec { Seed = 9, ModernDevelopment = true });
            RegionDevelopment plan = region.Development;
            Assert.IsNotNull(plan);
            Assert.AreEqual(plan.PrimaryCity, plan.Highway[0]);
            Assert.AreEqual(plan.SecondaryCity, plan.Highway[plan.Highway.Count - 1]);
            Assert.Contains(plan.AirportAccess[0], new System.Collections.Generic.List<RegionPosition>(plan.Highway));

            bool primaryStreet = false;
            bool secondaryStreet = false;
            foreach (var street in plan.Streets)
            {
                primaryStreet |= street.Start.Equals(plan.PrimaryCity);
                secondaryStreet |= street.Start.Equals(plan.SecondaryCity);
            }
            Assert.IsTrue(primaryStreet && secondaryStreet);
            AssertDryPath(plan.Highway);
            AssertDryPath(plan.AirportAccess);

            int runways = 0;
            int ruralBuildings = 0;
            foreach (RegionDevelopmentFeature feature in plan.Features)
            {
                if (feature.Kind == RegionDevelopmentFeatureKind.Runway) runways++;
                if (feature.Kind == RegionDevelopmentFeatureKind.RuralRoof) ruralBuildings++;
                AssertDry(feature.Center);
            }
            Assert.AreEqual(1, runways);
            Assert.Greater(ruralBuildings, 1);

            float xMap = plan.Airport.Center.X / (region.KmPerMapUnit * 1000f);
            float yMap = plan.Airport.Center.Y / (region.KmPerMapUnit * 1000f);
            RegionDetail detail = RegionDetailBuilder.Build(region, new RegionDetailSpec
            {
                CenterMapX = xMap, CenterMapY = yMap, Resolution = 256,
            });
            Assert.AreSame(plan, detail.Data.Development);
            Assert.IsTrue(detail.Contains(xMap, yMap));

            void AssertDryPath(System.Collections.Generic.IReadOnlyList<RegionPosition> points)
            {
                for (int i = 1; i < points.Count; i++)
                {
                    RegionPosition a = points[i - 1], b = points[i];
                    int steps = Mathf.CeilToInt(Vector2.Distance(new Vector2(a.X, a.Y), new Vector2(b.X, b.Y)) / 125f);
                    for (int step = 0; step <= steps; step++)
                    {
                        float t = step / (float)steps;
                        AssertDry(new RegionPosition(Mathf.Lerp(a.X, b.X, t), Mathf.Lerp(a.Y, b.Y, t)));
                    }
                }
            }

            void AssertDry(RegionPosition p)
            {
                int cell = region.Index((int)(p.X / region.CellMeters), (int)(p.Y / region.CellMeters));
                Assert.Greater(region.Height[cell], 0f);
                Assert.AreEqual(0f, region.LakeDepth[cell]);
            }
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

        [Test]
        public void Clouds_SameSeedSameLayer_AndMoreCoverMeansMoreCloud()
        {
            const int size = 96;
            CollectionAssert.AreEqual(RegionClouds.BuildField(size, 7, 0.58f), RegionClouds.BuildField(size, 7, 0.58f));
            Assert.AreEqual(0f, RegionClouds.Coverage(RegionClouds.BuildField(size, 7, 0f), 0f));
            float few = RegionClouds.Coverage(RegionClouds.BuildField(size, 7, 0.3f), 0.2f);
            float many = RegionClouds.Coverage(RegionClouds.BuildField(size, 7, 0.7f), 0.2f);
            Assert.Greater(many, few);
        }

        /// <summary>探索画面は雲の層をずらして繰り返すので、左右と上下の端がつながっていること。</summary>
        [Test]
        public void Clouds_LayerRepeatsWithoutSeam()
        {
            const int size = 128;
            float[] field = RegionClouds.BuildField(size, 7, 0.58f);
            double inside = 0;
            double acrossX = 0;
            double acrossY = 0;
            for (int k = 0; k < size; k++)
            {
                for (int x = 1; x < size; x++) inside += System.Math.Abs(field[x + k * size] - field[x - 1 + k * size]);
                acrossX += System.Math.Abs(field[k * size] - field[size - 1 + k * size]);
                acrossY += System.Math.Abs(field[k] - field[k + (size - 1) * size]);
            }
            inside /= size * (size - 1);
            acrossX /= size;
            acrossY /= size;
            // 端をまたぐ隣どうしの差が、中の隣どうしの差と同じくらいに収まる（継ぎ目があると何倍にもなる）
            Assert.Less(acrossX, inside * 2.5 + 0.01);
            Assert.Less(acrossY, inside * 2.5 + 0.01);
        }

        /// <summary>雲の流れは場所ごとに違い、渦のずれは小さな上限に収まる（伸び続けると雲の絵が筋になる）。</summary>
        [Test]
        public void CloudFlow_DiffersByPlace_AndSwirlStaysBounded()
        {
            const float time = 90f;
            const float cross = 420f;
            for (int layer = RegionCloudFlow.Low; layer <= RegionCloudFlow.High; layer++)
            {
                float driftU;
                float driftV;
                RegionCloudFlow.Drift(layer, time, cross, out driftU, out driftV);
                float least = float.MaxValue;
                float most = float.MinValue;
                for (int gy = 0; gy <= 16; gy++)
                {
                    for (int gx = 0; gx <= 16; gx++)
                    {
                        float du;
                        float dv;
                        float fade;
                        RegionCloudFlow.Sample(layer, gx / 16f, gy / 16f, time, cross, out du, out dv, out fade);
                        float swirlU = du - driftU;
                        float swirlV = dv - driftV;
                        Assert.Less(System.Math.Abs(swirlU), 0.05f);
                        Assert.Less(System.Math.Abs(swirlV), 0.05f);
                        Assert.That(fade, Is.InRange(0f, 1f));
                        if (swirlU < least) least = swirlU;
                        if (swirlU > most) most = swirlU;
                    }
                }
                Assert.Greater(most - least, 0.002f);
            }
        }

        /// <summary>エリア：陸のセルはどれかのエリアに入り、名前は重ならず、同じ seed なら同じ区分けになる。</summary>
        [Test]
        public void Areas_CoverLand_HaveUniqueNames_AndAreDeterministic()
        {
            RegionData a = RegionGenerator.Generate(SmallSpec(7));
            RegionData b = RegionGenerator.Generate(SmallSpec(7));
            CollectionAssert.AreEqual(a.Area, b.Area);
            Assert.Greater(a.Areas.Count, 0);
            Assert.AreEqual(a.Areas.Count, b.Areas.Count);

            var names = new System.Collections.Generic.HashSet<string>();
            for (int k = 0; k < a.Areas.Count; k++)
            {
                RegionArea area = a.Areas[k];
                Assert.AreEqual(area.Name, b.Areas[k].Name);
                Assert.IsTrue(names.Add(area.Name), "同じ名前のエリアがある: " + area.Name);
                Assert.AreSame(area, a.AreaById(area.Id));
                Assert.That(area.LabelX, Is.InRange(0f, RegionData.MapUnits));
                Assert.That(area.LabelY, Is.InRange(0f, RegionData.MapUnits));
            }

            for (int i = 0; i < a.Height.Length; i++)
            {
                if (a.Height[i] > 0f) Assert.IsNotNull(a.AreaById(a.Area[i]), "エリアに入っていない陸のセルがある");
            }

            for (int s = 0; s < a.Sites.Count; s++)
            {
                RegionSite site = a.Sites[s];
                Assert.IsFalse(string.IsNullOrEmpty(site.Reason));
                Assert.GreaterOrEqual(site.Rank, 1);
                if (site.AreaId != 0) Assert.IsNotNull(a.AreaById(site.AreaId));
            }
        }

        /// <summary>seed から作る呼び名：地域の中で重ならず、同じ seed なら同じ名前になり、機械的な呼び名へ戻せる。</summary>
        [Test]
        public void Names_GeneratedStyles_AreUniqueAndDeterministic_AndCanBeRestored()
        {
            RegionData a = RegionGenerator.Generate(SmallSpec(7));
            RegionData b = RegionGenerator.Generate(SmallSpec(7));
            string firstArea = a.Areas[0].Name;
            string firstSite = a.Sites[0].Name;

            var styles = new[] { RegionNameStyle.SoftKana, RegionNameStyle.HardKana, RegionNameStyle.Kanji };
            for (int s = 0; s < styles.Length; s++)
            {
                RegionNames.Apply(a, styles[s]);
                RegionNames.Apply(b, styles[s]);
                var names = new System.Collections.Generic.HashSet<string>();
                for (int k = 0; k < a.Areas.Count; k++)
                {
                    Assert.AreEqual(a.Areas[k].Name, b.Areas[k].Name);
                    Assert.IsTrue(names.Add(a.Areas[k].Name), styles[s] + " で同じ名前のエリアがある: " + a.Areas[k].Name);
                }
                for (int k = 0; k < a.Sites.Count; k++)
                {
                    Assert.AreEqual(a.Sites[k].Name, b.Sites[k].Name);
                    Assert.IsTrue(names.Add(a.Sites[k].Name), styles[s] + " で同じ名前の地点がある: " + a.Sites[k].Name);
                }
                Assert.AreNotEqual(firstArea, a.Areas[0].Name);
            }

            RegionNames.Apply(a, RegionNameStyle.Descriptive);
            Assert.AreEqual(firstArea, a.Areas[0].Name);
            Assert.AreEqual(firstSite, a.Sites[0].Name);
        }

        /// <summary>
        /// 拡大した地形：同じ入力からは同じ地形になり、海か陸かは広域の同じ場所とほぼ一致し、広域の川は拡大しても水面として通る。
        /// 構造物を置くと、その場所の地面が高くなる。
        /// </summary>
        [Test]
        public void Detail_IsDeterministic_KeepsCoastAndRivers_AndTakesStructure()
        {
            RegionData region = RegionGenerator.Generate(SmallSpec(7));
            Assert.IsNotNull(region.Downstream);
            RegionSite onLand = null;
            for (int s = 0; s < region.Sites.Count && onLand == null; s++)
            {
                if (region.Sites[s].HeightMeters > 20f) onLand = region.Sites[s];
            }
            Assert.IsNotNull(onLand, "陸の上の地点が無い");

            var spec = new RegionDetailSpec { CenterMapX = onLand.MapX, CenterMapY = onLand.MapY, SizeKm = 24f, Resolution = 192 };
            RegionDetail a = RegionDetailBuilder.Build(region, spec);
            RegionDetail b = RegionDetailBuilder.Build(region, spec);
            Assert.AreEqual(192 * 192, a.Data.Height.Length);
            CollectionAssert.AreEqual(a.Data.Height, b.Data.Height);
            CollectionAssert.AreEqual(a.Data.WaterCoverage, b.Data.WaterCoverage);
            Assert.Greater(a.CoastAgreement, 0.9f, "海陸が広域と合っていない");
            Assert.Greater(a.RiverAgreement, 0.9f, "広域の川が拡大した地形に通っていない");
            Assert.IsTrue(a.Contains(onLand.MapX, onLand.MapY));

            int n = a.Data.Resolution;
            int center = (int)a.LocalX(onLand.MapX) + (int)a.LocalY(onLand.MapY) * n;
            float before = a.Data.Height[center];
            Assert.Greater(before, 0f);
            RegionStructure placed = RegionStructureStamp.Place(a, onLand.MapX, onLand.MapY, onLand.Kind, RegionStructureForm.Stepped);
            Assert.IsNotNull(placed);
            Assert.Greater(a.Data.Height[center], before + 100f);
            Assert.Greater(a.Data.StructureCoverage[center], 0f);
            Assert.AreEqual(a.Data.Height.Length * 4, RegionMapRenderer.Render(a.Data, new RegionRenderOptions { CloudCover = 0f }).Length);
        }

        [Test]
        public void Skin_NothingPlaced_LoadsNull_AndTextureBecomesCenteredSprite()
        {
            // 差し替えの入口: 置いていない名前は null（探索画面は既定の絵を使う）
            Assert.IsNull(RegionMapSkin.Load("NoSuchImage"));
            Assert.IsNull(RegionMapSkin.FromTexture(null));
            CollectionAssert.AllItemsAreUnique(RegionMapSkin.Names);
            Assert.AreEqual(4, RegionMapSkin.Names.Length);

            var texture = new Texture2D(8, 4, TextureFormat.RGBA32, false);
            try
            {
                Sprite sprite = RegionMapSkin.FromTexture(texture);
                Assert.AreEqual(8f, sprite.rect.width);
                Assert.AreEqual(4f, sprite.rect.height);
                Assert.AreEqual(4f, sprite.pivot.x, 0.001f);
                Assert.AreEqual(2f, sprite.pivot.y, 0.001f);
                Object.DestroyImmediate(sprite);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }
    }
}
