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

        [TestCase(69f)]
        [TestCase(75f)]
        public void SampleAt_ShowsTheSameCloudsAsTheExplorerMap(float time)
        {
            // 探索画面と同じ大きさの雲の画像を、探索画面と同じ読み方（流れのずれ・上下の入れ替え・画素の中心）で読んだ値と比べる
            const int size = 512;
            float[] low;
            float[] high;
            RegionClouds.BuildLayerFields(size, Seed, Cover, out low, out high);
            var spec = new RegionCloudSkySpec
            {
                RegionSeed = Seed, Cover = Cover, Time = time, CrossSeconds = 420f, MapCloudSize = size,
                OriginMapX = 43.61f, OriginMapY = 76.92f, MetersPerMapUnit = 2560f,
            };
            var sampler = new RegionCloudSky.Sampler(spec);
            var random = new Random(5);
            double maxError = 0;
            double flippedError = 0;
            int pointsWithHighClouds = 0;
            const int count = 400;
            for (int i = 0; i < count; i++)
            {
                // 見比べの範囲を取り、場面の真上だけでは見つからない高い薄雲も拾う。
                float mapX = spec.OriginMapX + ((float)random.NextDouble() - 0.5f) * 46f;
                float mapY = spec.OriginMapY + ((float)random.NextDouble() - 0.5f) * 46f;
                // 場面は (x, 高さ, -z) で置く。z は地図の Y と同じく南へ増える
                float x = (mapX - spec.OriginMapX) * spec.MetersPerMapUnit;
                float z = -(mapY - spec.OriginMapY) * spec.MetersPerMapUnit;
                float skyLow;
                float skyHigh;
                sampler.SampleAt(x, z, out skyLow, out skyHigh);

                float u = mapX / RegionData.MapUnits;
                float v = 1f - mapY / RegionData.MapUnits;
                if (Shown(high, size, RegionCloudFlow.High, spec, u, v, false) > 0.01f) pointsWithHighClouds++;
                maxError = Math.Max(maxError, Math.Abs(skyLow - Shown(low, size, RegionCloudFlow.Low, spec, u, v, false)));
                maxError = Math.Max(maxError, Math.Abs(skyHigh - Shown(high, size, RegionCloudFlow.High, spec, u, v, false)));
                flippedError += Math.Abs(skyLow - Shown(low, size, RegionCloudFlow.Low, spec, u, v, true));
            }
            flippedError /= count;
            Assert.Less(maxError, 0.00005, "2D の雲との差の最大値");
            Assert.Greater(pointsWithHighClouds, 50, "高い雲がある点も照合できていること");
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
            var sampler = new RegionCloudSky.Sampler(spec);
            float step = spec.Size / spec.Resolution;
            foreach (int c in new[] { 0, 7, 15 })
            {
                foreach (int r in new[] { 0, 9, 15 })
                {
                    float a;
                    float b;
                    sampler.SampleAt(spec.MinX + (c + 0.5f) * step, spec.MinZ + (r + 0.5f) * step, out a, out b);
                    Assert.AreEqual(a, low[c + r * spec.Resolution], 0f);
                    Assert.AreEqual(b, high[c + r * spec.Resolution], 0f);
                }
            }
        }

        // 探索画面（RegionCloudLayer）が地図の位置 (u, v) に描く雲の濃さ。画像は北の行が上で、繰り返して読む
        private static float Shown(float[] field, int size, int layer, RegionCloudSkySpec spec, float u, float v, bool flipped)
        {
            const int cells = 32;
            int x = (int)(u * cells), y = (int)(v * cells);
            float fxCell = u * cells - x, fyCell = v * cells - y;
            var a = Vertex(layer, spec, x / (float)cells, y / (float)cells);
            var d = Vertex(layer, spec, (x + 1) / (float)cells, (y + 1) / (float)cells);
            var b = fxCell > fyCell
                ? Vertex(layer, spec, (x + 1) / (float)cells, y / (float)cells)
                : Vertex(layer, spec, x / (float)cells, (y + 1) / (float)cells);
            float along = Math.Min(fxCell, fyCell), across = Math.Abs(fxCell - fyCell);
            float tu = Wrap(a.U + (d.U - a.U) * along + (b.U - a.U) * across);
            float tv = Wrap(a.V + (d.V - a.V) * along + (b.V - a.V) * across);
            float fade = a.Fade + (d.Fade - a.Fade) * along + (b.Fade - a.Fade) * across;
            float fx = tu * size - 0.5f;
            float fy = flipped ? tv * size - 0.5f : (1f - tv) * size - 0.5f;
            return RegionClouds.Sample(field, size, fx, fy) * fade;
        }

        private static (float U, float V, float Fade) Vertex(int layer, RegionCloudSkySpec spec, float u, float v)
        {
            RegionCloudFlow.Sample(layer, u, v, spec.Time, spec.CrossSeconds, out float du, out float dv, out float fade);
            return (u - du, v - dv, fade);
        }

        private static float Wrap(float value)
        {
            return value - (float)Math.Floor(value);
        }
    }
}
