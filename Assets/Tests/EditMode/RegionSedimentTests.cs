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
            float[][] maps = { after.Deposit, after.Wear, after.SedimentFlow, after.Scree };
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
        public void Apply_GravityOnly_ConservesVolume_AndLeavesNoLooseSlopeSteeperThanRepose()
        {
            RegionData data = SyntheticButte(out float[] floor);
            var before = (float[])data.Height.Clone();
            RegionSedimentBudget budget = RegionSediment.Apply(data, floor, 1f, 1f, 0f, 5u);
            Assert.Greater(budget.Supplied, 0.0);
            Assert.AreEqual(0.0, budget.Eroded);
            double change = VolumeChange(before, data.Height, data.CellMeters);
            Assert.AreEqual(budget.Supplied - budget.Outflow, change, 1e-3 * budget.Supplied + 1.0);

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
