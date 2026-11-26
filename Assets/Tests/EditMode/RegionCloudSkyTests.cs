using System;
using NUnit.Framework;
using Vastcore.RegionMap;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// 3D の空の雲（RegionCloudSky）が、2D の地図に重なる雲と同じ所に同じ形で出ることを確かめる
    /// （2D で見つけた形の雲を、その地域へ降りても見つけられる。本人の前提条件、2026-11-26）。
    /// </summary>
    public class RegionCloudSkyTests
    {
        private const int Seed = 9;
        private const float Cover = 0.58f;

        [Test]
        public void SampleLayers_MatchesEveryCellOfTheMapField()
        {
            const int size = 96;
            float[] low;
            float[] high;
            RegionClouds.BuildLayerFields(size, Seed, Cover, out low, out high);
            uint seed = RegionClouds.CloudSeed(Seed);
            float amount = RegionClouds.CloudAmount(Cover);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float thick;
                    float veil;
                    RegionClouds.SampleLayers(x * (1f / size), y * (1f / size), seed, amount, out thick, out veil);
                    Assert.AreEqual(low[x + y * size], thick, 0f, "低い雲 " + x + "," + y);
                    Assert.AreEqual(high[x + y * size], veil, 0f, "高い雲 " + x + "," + y);
                }
            }
        }

        [Test]
        public void SampleAt_ShowsTheSameCloudsAsTheExplorerMap()
        {
            // 探索画面と同じ大きさの雲の画像を、探索画面と同じ読み方（流れのずれ・上下の入れ替え・画素の中心）で読んだ値と比べる
            const int size = 512;
            float[] low;
            float[] high;
            RegionClouds.BuildLayerFields(size, Seed, Cover, out low, out high);
            var spec = new RegionCloudSkySpec
            {
                RegionSeed = Seed, Cover = Cover, Time = 137f, CrossSeconds = 420f, MapCloudSize = size,
                OriginMapX = 40.5f, OriginMapY = 73.25f, MetersPerMapUnit = 2560f,
            };
            var random = new Random(5);
            double error = 0;
            double flippedError = 0;
            const int count = 400;
            for (int i = 0; i < count; i++)
            {
                float mapX = 40.5f + (float)random.NextDouble() * 6f;
                float mapY = 73.25f + (float)random.NextDouble() * 6f;
                // 場面は (x, 高さ, -z) で置く。z は地図の Y と同じく南へ増える
                float x = (mapX - spec.OriginMapX) * spec.MetersPerMapUnit;
                float z = -(mapY - spec.OriginMapY) * spec.MetersPerMapUnit;
                float skyLow;
                float skyHigh;
                RegionCloudSky.SampleAt(spec, x, z, out skyLow, out skyHigh);

                float u = mapX / RegionData.MapUnits;
                float v = 1f - mapY / RegionData.MapUnits;
                error += Math.Abs(skyLow - Shown(low, size, RegionCloudFlow.Low, spec, u, v, false));
                error += Math.Abs(skyHigh - Shown(high, size, RegionCloudFlow.High, spec, u, v, false));
                flippedError += Math.Abs(skyLow - Shown(low, size, RegionCloudFlow.Low, spec, u, v, true));
            }
            error /= count * 2;
            flippedError /= count;
            // 一致しない時は、画素の間をつなぐ読み方の違いだけが残る。上下を取り違えると、別の場所の雲になって大きくずれる
            Assert.Less(error, 0.02, "2D の雲との差の平均");
            Assert.Greater(flippedError, 0.1, "上下を取り違えた時の差の平均（試験が向きの誤りを見分けられること）");
        }

        [Test]
        public void Build_RowsRunFromSouthToNorth()
        {
            var spec = new RegionCloudSkySpec
            {
                RegionSeed = Seed, Cover = Cover, MapCloudSize = 512,
                OriginMapX = 40f, OriginMapY = 70f, MetersPerMapUnit = 2560f,
                MinX = 1000f, MinZ = -9000f, Size = 8000f, Resolution = 16,
            };
            float[] low;
            float[] high;
            RegionCloudSky.Build(spec, out low, out high);
            float step = spec.Size / spec.Resolution;
            foreach (int c in new[] { 0, 7, 15 })
            {
                foreach (int r in new[] { 0, 9, 15 })
                {
                    float a;
                    float b;
                    RegionCloudSky.SampleAt(spec, spec.MinX + (c + 0.5f) * step, spec.MinZ + (r + 0.5f) * step, out a, out b);
                    Assert.AreEqual(a, low[c + r * spec.Resolution], 0f);
                    Assert.AreEqual(b, high[c + r * spec.Resolution], 0f);
                }
            }
        }

        // 探索画面（RegionCloudLayer）が地図の位置 (u, v) に描く雲の濃さ。画像は北の行が上で、繰り返して読む
        private static float Shown(float[] field, int size, int layer, RegionCloudSkySpec spec, float u, float v, bool flipped)
        {
            float du;
            float dv;
            float fade;
            RegionCloudFlow.Sample(layer, u, v, spec.Time, spec.CrossSeconds, out du, out dv, out fade);
            float tu = Wrap(u - du);
            float tv = Wrap(v - dv);
            float fx = tu * size - 0.5f;
            float fy = flipped ? tv * size - 0.5f : (1f - tv) * size - 0.5f;
            return RegionClouds.Sample(field, size, fx, fy) * fade;
        }

        private static float Wrap(float value)
        {
            return value - (float)Math.Floor(value);
        }
    }
}
