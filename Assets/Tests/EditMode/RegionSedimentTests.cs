using System;
using NUnit.Framework;
using Vastcore.RegionMap;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// 土砂の場（RegionSediment、RegionDetailSpec.Sediment）の約束を確かめる。
    /// 0 では今の地形とバイト一致、帽子の岩・水面・海岸は動かず台地を載せる前の標高より下を削らない、
    /// 土砂の総量は供給から窓の外へ出た分を引いた量に近い、崩れの後のゆるい土砂は安息角を超えない。見た目は対象外。
    /// </summary>
    public class RegionSedimentTests
    {
        private static RegionData _dryRegion;

        /// <summary>seed 9 の地域（南に、台地が立つ乾いた平原がある）。</summary>
        private static RegionData DryRegion()
        {
            if (_dryRegion == null) _dryRegion = RegionGenerator.Generate(new RegionSpec { Seed = 9 });
            return _dryRegion;
        }

        private static RegionDetailSpec MesaWindow()
        {
            return new RegionDetailSpec { CenterMapX = 43.61f, CenterMapY = 76.92f, SizeKm = 0.6f, Resolution = 256, Mesa = 1f };
        }

        [Test]
        public void Sediment_IsOffByDefault_AndZeroLeavesTheTerrainByteIdentical()
        {
            RegionData region = DryRegion();
            RegionDetailSpec spec = MesaWindow();
            Assert.AreEqual(0f, spec.Sediment);
            RegionData plain = RegionDetailBuilder.Build(region, spec).Data;

            // 重力と水の強さを変えても、Sediment が 0 なら何も変わらない
            RegionDetailSpec zero = MesaWindow();
            zero.Sediment = 0f;
            zero.SedimentGravity = 3f;
            zero.SedimentWater = 3f;
            RegionData same = RegionDetailBuilder.Build(region, zero).Data;
            for (int i = 0; i < plain.Height.Length; i++)
            {
                Assert.AreEqual(BitConverter.SingleToInt32Bits(plain.Height[i]), BitConverter.SingleToInt32Bits(same.Height[i]));
                Assert.AreEqual(plain.Biome[i], same.Biome[i]);
            }
            Assert.IsNull(same.Deposit);
            Assert.IsNull(same.Wear);
            Assert.IsNull(same.SedimentFlow);
            Assert.IsNull(same.Scree);
            Assert.IsNull(same.Coarse);

            // 台地の無い窓では、Sediment を入れても地図を持たない
            RegionDetailSpec noMesa = MesaWindow();
            noMesa.Mesa = 0f;
            noMesa.Sediment = 1f;
            Assert.IsNull(RegionDetailBuilder.Build(region, noMesa).Data.Deposit);
        }

        [Test]
        public void Sediment_KeepsCaprockWaterAndCoast_AndNeverCutsBelowTheFloor()
        {
            RegionData region = DryRegion();
            RegionDetailSpec spec = MesaWindow();
            RegionDetail beforeDetail = RegionDetailBuilder.Build(region, spec);
            RegionData before = beforeDetail.Data;
            spec.Mesa = 0f;
            RegionData floor = RegionDetailBuilder.Build(region, spec).Data;
            spec.Mesa = 1f;
            spec.Sediment = 1f;
            RegionDetail afterDetail = RegionDetailBuilder.Build(region, spec);
            RegionData after = afterDetail.Data;

            Assert.AreEqual(beforeDetail.CoastAgreement, afterDetail.CoastAgreement);
            Assert.AreEqual(beforeDetail.RiverAgreement, afterDetail.RiverAgreement);
            CollectionAssert.AreEqual(before.WaterCoverage, after.WaterCoverage);
            float[][] maps = { after.Deposit, after.Wear, after.SedimentFlow, after.Scree, after.Coarse };
            foreach (float[] map in maps)
            {
                Assert.IsNotNull(map);
                Assert.AreEqual(after.Height.Length, map.Length);
            }

            int changed = 0;
            for (int i = 0; i < after.Height.Length; i++)
            {
                Assert.AreEqual(before.Height[i] > 0f, after.Height[i] > 0f);
                bool still = after.Caprock[i] != 0 || after.WaterCoverage[i] > 0f || after.LakeDepth[i] > 0f || after.Height[i] <= 0f;
                if (still)
                {
                    Assert.AreEqual(before.Height[i], after.Height[i]);
                    foreach (float[] map in maps) Assert.AreEqual(0f, map[i]);
                }
                else
                {
                    // 台地を載せる前の標高より下は削らない
                    Assert.GreaterOrEqual(after.Height[i], floor.Height[i] - 1e-3f);
                }
                foreach (float[] map in maps)
                {
                    Assert.GreaterOrEqual(map[i], 0f);
                    Assert.LessOrEqual(map[i], 1f);
                }
                if (Math.Abs(after.Height[i] - before.Height[i]) > 0.05f) changed++;
            }
            Assert.Greater(changed, after.Height.Length / 50);
            Assert.Greater(Strong(after.Scree), 50);
            Assert.Greater(Strong(after.Wear), 50);
            Assert.Greater(Strong(after.Deposit), 50);
        }

        [Test]
        public void RockField_WithTheCoarseMap_SortsLargeRocksTowardTheToes_AndWithoutItIsUnchanged()
        {
            RegionData region = DryRegion();
            RegionDetailSpec spec = MesaWindow();
            RegionData plain = RegionDetailBuilder.Build(region, spec).Data;
            // 地図が無い窓では、分級を頼んでも今の置き方と同じ
            CollectionAssert.AreEqual(RegionRockField.Build(plain, 1000.0, 2000.0, 0f, 0f, 0f, null).Positions,
                RegionRockField.Build(plain, 1000.0, 2000.0, 0f, 0f, 0f, null, true).Positions);

            spec.Sediment = 1f;
            RegionData data = RegionDetailBuilder.Build(region, spec).Data;
            float before = CoarseGap(data, RegionRockField.Build(data, 1000.0, 2000.0, 0f, 0f, 0f, null));
            float after = CoarseGap(data, RegionRockField.Build(data, 1000.0, 2000.0, 0f, 0f, 0f, null, true));
            // 背丈を超える岩は、礫より Coarse の大きい所（錐の裾とその先）に多い
            Assert.Greater(after, 0.2f);
            Assert.Greater(after, before + 0.15f);
        }

        [Test]
        public void RockGround_SettlesTheRootsAndBandsTheRocks_OnlyWithSedimentMaps()
        {
            RegionData region = DryRegion();
            RegionLookSpec Spec(float sediment, bool rockGround)
            {
                return new RegionLookSpec { OuterKm = 1.5f, InnerKm = 0.5f, Resolution = 128, Sediment = sediment, RockGround = rockGround, Vegetation = false };
            }

            RegionLookScene off = RegionLookScene.Build(region, Spec(1f, false));
            RegionLookScene on = RegionLookScene.Build(region, Spec(1f, true));
            Assert.IsNull(off.Rocks.Surface);
            Assert.IsNotNull(on.Rocks.Surface);
            Assert.AreEqual(on.Rocks.VertexCount * 4, on.Rocks.Surface.Length);
            int banded = 0;
            for (int v = 0; v < on.Rocks.VertexCount; v++)
            {
                Assert.GreaterOrEqual(on.Rocks.Surface[v * 4 + 3], 0f);
                if (on.Rocks.Surface[v * 4 + 3] > 0f) banded++;
            }
            Assert.Greater(banded, on.Rocks.VertexCount / 2);

            // 根元では地面を上げて土砂を足すだけ（下げない・減らさない）。岩の無い所は変えない
            RegionData a = off.Inner.Data, b = on.Inner.Data;
            int raised = 0;
            for (int i = 0; i < a.Height.Length; i++)
            {
                Assert.GreaterOrEqual(b.Height[i], a.Height[i]);
                Assert.GreaterOrEqual(b.Deposit[i], a.Deposit[i]);
                if (b.Height[i] > a.Height[i]) raised++;
            }
            Assert.Greater(raised, 0);
            CollectionAssert.AreEqual(off.Outer.Data.Height, on.Outer.Data.Height);

            UnityEngine.Mesh mesh = RegionTerrainView.LookMesh(on.Rocks);
            try
            {
                var surface = new System.Collections.Generic.List<UnityEngine.Vector4>();
                mesh.GetUVs(1, surface);
                Assert.AreEqual(on.Rocks.VertexCount, surface.Count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }

            // Sediment 0 では、なじませを入れても T3 までと同じ岩と地形
            RegionLookScene zeroOff = RegionLookScene.Build(region, Spec(0f, false));
            RegionLookScene zeroOn = RegionLookScene.Build(region, Spec(0f, true));
            Assert.IsNull(zeroOn.Rocks.Surface);
            CollectionAssert.AreEqual(zeroOff.Inner.Data.Height, zeroOn.Inner.Data.Height);
            CollectionAssert.AreEqual(zeroOff.Rocks.Positions, zeroOn.Rocks.Positions);
        }

        /// <summary>1.6 m 以上の岩と 0.4 m 未満の礫の、置かれた所の Coarse の平均の差。</summary>
        private static float CoarseGap(RegionData data, RegionRockField field)
        {
            int n = data.Resolution;
            float cell = data.CellMeters;
            double big = 0, small = 0;
            int bigCount = 0, smallCount = 0;
            foreach (RegionRock rock in field.Rocks)
            {
                int x = Math.Min(n - 1, (int)(rock.X / cell + 0.5f)), z = Math.Min(n - 1, (int)(rock.Z / cell + 0.5f));
                float c = data.Coarse[x + z * n];
                if (rock.Size >= 1.6f) { big += c; bigCount++; }
                else if (rock.Size < 0.4f) { small += c; smallCount++; }
            }
            Assert.Greater(bigCount, 5);
            Assert.Greater(smallCount, 5);
            return (float)(big / bigCount - small / smallCount);
        }

        [Test]
        public void Apply_GravityOnly_ConservesVolume_AndLeavesNoLooseSlopeSteeperThanRepose()
        {
            RegionData data = SyntheticButte(out float[] floor);
            var before = (float[])data.Height.Clone();
            RegionSedimentBudget budget = RegionSediment.Apply(data, floor, 1f, 1f, 0f, 5u);
            Assert.Greater(budget.Supplied, 0.0);
            // 岩屑の溝が削った分は溝の下の錐へ積み直すので、総量は供給から窓の外へ出た分を引いた量
            double change = VolumeChange(before, data.Height, data.CellMeters);
            Assert.AreEqual(budget.Supplied - budget.Outflow, change, 1e-3 * (budget.Supplied + budget.Eroded) + 1.0);

            // 分級：崖の足もとの近く（6 m 以内）より、斜面の下の方（15 m より先）の岩屑に大きい岩が多い
            double near = 0.0, far = 0.0;
            int nearCount = 0, farCount = 0;
            for (int i = 0; i < data.Height.Length; i++)
            {
                if (data.Scree[i] <= 0.05f) continue;
                int x = i % data.Resolution, y = i / data.Resolution;
                int distance = Math.Max(Math.Max(36 - x, x - 59), Math.Max(36 - y, y - 59));
                if (distance <= 0) continue;
                if (distance <= 6) { near += data.Coarse[i]; nearCount++; }
                else if (distance > 15) { far += data.Coarse[i]; farCount++; }
            }
            Assert.Greater(nearCount, 0);
            Assert.Greater(farCount, 0);
            Assert.Greater(far / farCount, near / nearCount);

            // ゆるい土砂のあるセルの組で、安息角を明らかに超える組は残らない
            int n = data.Resolution;
            float cell = data.CellMeters;
            int pairs = 0, steep = 0;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n - 1; x++)
                {
                    int i = x + y * n;
                    if (data.Scree[i] <= 0.05f || data.Scree[i + 1] <= 0.05f) continue;
                    pairs++;
                    if (Math.Abs(data.Height[i] - data.Height[i + 1]) > RegionSediment.ReposeTan * cell * 1.05f + 0.01f) steep++;
                }
            }
            Assert.Greater(pairs, 20);
            Assert.LessOrEqual(steep, pairs / 50);
        }

        [Test]
        public void Apply_WithWater_KeepsTheBudgetBalanced_AndTheCaprockStill()
        {
            RegionData data = SyntheticButte(out float[] floor);
            var before = (float[])data.Height.Clone();
            RegionSedimentBudget budget = RegionSediment.Apply(data, floor, 1f, 1f, 1f, 5u);
            Assert.Greater(budget.Eroded, 0.0);
            Assert.Greater(budget.Deposited, 0.0);
            double change = VolumeChange(before, data.Height, data.CellMeters);
            // 床で止めた削りの分だけずれる。供給と削りの合計の 3% 以内
            Assert.AreEqual(budget.Supplied - budget.Outflow, change, 0.03 * (budget.Supplied + budget.Eroded) + 1.0);
            for (int i = 0; i < data.Height.Length; i++)
            {
                if (data.Caprock[i] != 0) Assert.AreEqual(before[i], data.Height[i]);
                Assert.GreaterOrEqual(data.Height[i], floor[i] - 1e-3f);
            }
        }

        /// <summary>
        /// 1 m の格子 96×96 に、40 m の崖を持つ四角い帽子の岩（縁に割れ目の凹みが 1 つ）と、そのまわりの 34 度の岩屑の斜面を置く。床は 10 m。
        /// </summary>
        private static RegionData SyntheticButte(out float[] floor)
        {
            const int N = 96;
            var data = new RegionData(new RegionSpec { Resolution = N, SizeKm = N / 1000f });
            data.Caprock = new byte[N * N];
            data.WaterCoverage = new float[N * N];
            floor = new float[N * N];
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    int i = x + y * N;
                    floor[i] = 10f + 0.02f * x;
                    bool cap = x >= 36 && x < 60 && y >= 36 && y < 60 && !(x >= 46 && x < 49 && y < 44);
                    float distance = Math.Max(Math.Max(36 - x, x - 59), Math.Max(36 - y, y - 59));
                    float talus = 34f - 0.67f * distance;
                    data.Height[i] = cap ? 74f : Math.Max(floor[i], floor[i] + talus);
                    if (cap) data.Caprock[i] = 1;
                    data.Moisture[i] = 0.1f;
                }
            }
            return data;
        }

        private static double VolumeChange(float[] before, float[] after, float cell)
        {
            double sum = 0.0;
            for (int i = 0; i < before.Length; i++) sum += after[i] - before[i];
            return sum * cell * cell;
        }

        private static int Strong(float[] map)
        {
            int count = 0;
            for (int i = 0; i < map.Length; i++) if (map[i] > 0.5f) count++;
            return count;
        }
    }
}
