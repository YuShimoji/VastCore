using System;
using System.Collections.Generic;
using System.IO;

namespace Vastcore.RegionMap.Preview
{
    internal static class TerrainMeasurements
    {
        internal sealed class Trace
        {
            private readonly RegionSpec _spec;
            private readonly List<(string Phase, int Resolution, float[] Height)> _snapshots =
                new List<(string, int, float[])>();

            public Trace(RegionSpec spec) { _spec = spec; }

            public void Record(string phase, int resolution, float[] height)
            {
                _snapshots.Add((phase, resolution, (float[])height.Clone()));
            }

            public void Write(string stem)
            {
                using var metrics = new StreamWriter(stem + "-terrain-trace.csv");
                metrics.WriteLine("phase,resolution,outlets,land_basins,land_km2,max_depth_m,closed_sea_basins,closed_sea_km2,plain_km2,stripe_km2,plain_in_base_basin_km2,stripe_in_base_basin_km2,plain_coherence,streak_km2");
                // 侵食の前（base）に閉じていた窪地。筋がそこへ寄るかを各工程で数える。
                byte[] baseBasins = null;
                int baseResolution = 0;
                foreach (var snapshot in _snapshots)
                {
                    RegionSpec spec = _spec.Clone();
                    spec.Resolution = snapshot.Resolution;
                    var data = new RegionData(spec);
                    Array.Copy(snapshot.Height, data.Height, snapshot.Height.Length);
                    double cellKm = data.CellMeters / 1000.0;
                    double cellArea = cellKm * cellKm;
                    var plainMask = new bool[data.Height.Length];
                    var stripeMask = new bool[data.Height.Length];
                    var (plain, striped) = FindStripes(data, stripeMask, collectPlain: plainMask);
                    if (baseBasins == null)
                    {
                        baseBasins = new byte[data.Height.Length];
                        baseResolution = snapshot.Resolution;
                        FindBasins(data, baseBasins, cellArea);
                    }
                    WriteShade(stem + "-trace-" + snapshot.Phase + ".png", data, stripeMask);
                    var (coherence, streakCells) = FindStreaks(data, plainMask);
                    int plainInBasin = 0;
                    int stripeInBasin = 0;
                    int n = snapshot.Resolution;
                    for (int i = 0; i < plainMask.Length; i++)
                    {
                        if (!plainMask[i]) continue;
                        int bx = (i % n) * baseResolution / n;
                        int by = (i / n) * baseResolution / n;
                        if (baseBasins[bx + by * baseResolution] == 0) continue;
                        plainInBasin++;
                        if (stripeMask[i]) stripeInBasin++;
                    }
                    foreach (bool inlandSeaIsOutlet in new[] { false, true })
                    {
                        var basin = FindBasins(data, new byte[data.Height.Length], cellArea, inlandSeaIsOutlet);
                        string outlets = inlandSeaIsOutlet ? "all-low-cells" : "boundary";
                        metrics.WriteLine(FormattableString.Invariant($"{snapshot.Phase},{snapshot.Resolution},{outlets},{basin.Count},{basin.Cells * cellArea:F4},{basin.MaximumDepth:F3},{basin.SeaLevelCount},{basin.SeaLevelCells * cellArea:F4},{plain * cellArea:F4},{striped * cellArea:F4},{plainInBasin * cellArea:F4},{stripeInBasin * cellArea:F4},{coherence:F4},{streakCells * cellArea:F4}"));
                    }
                }
            }

            // 工程ごとの標高を北西からの光の陰影で描き、筋候補を桃色で重ねる（海は紺）
            private static void WriteShade(string file, RegionData data, bool[] stripes)
            {
                int n = data.Resolution;
                float cell = data.CellMeters;
                float[] h = data.Height;
                var pixels = new byte[n * n * 4];
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int i = x + y * n;
                    int p = 4 * i;
                    pixels[p + 3] = 255;
                    if (h[i] <= 0f) { pixels[p] = 20; pixels[p + 1] = 30; pixels[p + 2] = 70; continue; }
                    if (stripes[i]) { pixels[p] = 255; pixels[p + 1] = 60; pixels[p + 2] = 220; continue; }
                    float a = Math.Max(0f, h[Math.Max(0, x - 1) + y * n]);
                    float b = Math.Max(0f, h[Math.Min(n - 1, x + 1) + y * n]);
                    float c = Math.Max(0f, h[x + Math.Max(0, y - 1) * n]);
                    float d = Math.Max(0f, h[x + Math.Min(n - 1, y + 1) * n]);
                    float gx = (b - a) / (2f * cell) * 4f;
                    float gy = (d - c) / (2f * cell) * 4f;
                    // 光は北西（左上）から
                    float lit = (-gx * -0.7071f + -gy * -0.7071f + 1f) / (float)Math.Sqrt(gx * gx + gy * gy + 1f);
                    byte v = (byte)Math.Max(0, Math.Min(255, 60 + 170 * lit));
                    pixels[p] = v; pixels[p + 1] = v; pixels[p + 2] = v;
                }
                PngWriter.Write(file, pixels, n, n);
            }
        }

        private const double MinimumBasinKm2 = 1.0;
        private const float MinimumBasinDepth = 2f;
        private const float PlainHeightLimit = 350f;
        private const float PlainSlopeLimit = 0.05f;
        private static readonly (int X, int Y)[] Directions = { (1, 0), (1, 1), (0, 1), (-1, 1) };

        internal static void WriteLakeDetail(RegionData region, string stem)
        {
            if (!region.RetainedLakes) throw new InvalidOperationException("No retained lake in this region.");
            int center = 0;
            int lakeCells = 0;
            for (int i = 0; i < region.LakeDepth.Length; i++)
            {
                if (region.LakeDepth[i] > 0f) lakeCells++;
                if (region.LakeDepth[i] > region.LakeDepth[center]) center = i;
            }
            float mapX = region.MapXFromCell(center);
            float mapY = region.MapYFromCell(center);
            var detail = RegionDetailBuilder.Build(region, new RegionDetailSpec
            {
                CenterMapX = mapX,
                CenterMapY = mapY,
                SizeKm = 8f
            });
            double cellKm = region.CellMeters / 1000.0;
            int detailLakeCells = Array.FindAll(detail.Data.Biome, biome => biome == RegionBiome.Lake).Length;
            byte[] pixels = RegionMapRenderer.Render(detail.Data, new RegionRenderOptions { CloudCover = 0f });
            int resolution = detail.Data.Resolution;
            PngWriter.Write(stem + "-lake-detail.png", pixels, resolution, resolution);
            Console.WriteLine(FormattableString.Invariant($"lake center={mapX:F2},{mapY:F2} level={region.Height[center] + region.LakeDepth[center]:F2}m area={lakeCells * cellKm * cellKm:F2}km2 depth={region.LakeDepth[center]:F2}m detail_lake_cells={detailLakeCells}"));
        }

        internal static void ComparePlains(RegionData data, string stem)
        {
            RegionData baseline = RegionGenerator.Generate(data.Spec, null, null, baselineTerrain: true);
            var plain = new bool[data.Height.Length];
            var stripes = new bool[data.Height.Length];
            var (beforePlain, originalStripes) = FindStripes(baseline, stripes, collectPlain: plain);
            for (int i = 0; i < plain.Length; i++)
                plain[i] = plain[i] && data.Height[i] > 0f && data.Biome[i] != RegionBiome.Lake && data.Biome[i] != RegionBiome.River;

            Array.Clear(stripes);
            var (sharedPlain, beforeStripes) = FindStripes(baseline, stripes, fixedPlain: plain);
            Array.Clear(stripes);
            var (_, afterStripes) = FindStripes(data, stripes, fixedPlain: plain);
            double cellKm = data.CellMeters / 1000.0;
            double area = cellKm * cellKm;
            File.WriteAllLines(stem + "-plain-comparison.csv", new[]
            {
                "seed,resolution,baseline_plain_km2,shared_plain_km2,excluded_km2,baseline_stripe_km2,shared_before_stripe_km2,shared_after_stripe_km2",
                FormattableString.Invariant($"{data.Spec.Seed},{data.Resolution},{beforePlain * area:F4},{sharedPlain * area:F4},{(beforePlain - sharedPlain) * area:F4},{originalStripes * area:F4},{beforeStripes * area:F4},{afterStripes * area:F4}")
            });
        }

        internal static void Write(RegionData data, string stem, long generateMs, long erosionMs)
        {
            double cellKm = data.CellMeters / 1000.0;
            double cellKm2 = cellKm * cellKm;
            var basins = new byte[data.Height.Length];
            var stripes = new bool[data.Height.Length];
            var plainMask = new bool[data.Height.Length];
            var basin = FindBasins(data, basins, cellKm2);
            var (plainCells, stripeCells) = FindStripes(data, stripes, collectPlain: plainMask);
            var (coherence, streakCells) = FindStreaks(data, plainMask);
            var (lakes, lakeCells, largestLake) = CountLakes(data);
            var (riverCells, mouthFlow, terminalCells) = CountRivers(data);
            var (meanLand, lowland, snow) = Elevation(data);

            File.WriteAllLines(stem + "-measurements.csv", new[]
            {
                "seed,resolution,size_km,generation_ms,erosion_ms,basins,basin_km2,deepest_basin_m,sea_level_basins,sea_level_basin_km2,lake_km2,plain_km2,stripe_km2,stripe_plain_pct,lakes,largest_lake_km2,plain_coherence,streak_km2,streak_plain_pct,river_km2,mouth_flow_max,terminal_cells,mean_land_m,lowland_pct,snow_pct",
                FormattableString.Invariant($"{data.Spec.Seed},{data.Resolution},{data.SizeKm},{generateMs},{erosionMs},{basin.Count},{basin.Cells * cellKm2:F4},{basin.MaximumDepth:F3},{basin.SeaLevelCount},{basin.SeaLevelCells * cellKm2:F4},{lakeCells * cellKm2:F4},{plainCells * cellKm2:F4},{stripeCells * cellKm2:F4},{100.0 * stripeCells / Math.Max(1, plainCells):F4},{lakes},{largestLake * cellKm2:F4},{coherence:F4},{streakCells * cellKm2:F4},{100.0 * streakCells / Math.Max(1, plainCells):F4},{riverCells * cellKm2:F4},{mouthFlow:F2},{terminalCells},{meanLand:F1},{lowland:F2},{snow:F2}")
            });

            byte[] pixels = RegionMapRenderer.Render(data, new RegionRenderOptions { CloudCover = 0f });
            for (int i = 0; i < data.Height.Length; i++)
            {
                if (basins[i] == 0 && !stripes[i]) continue;
                pixels[4 * i] = basins[i] == 1 ? (byte)0 : (byte)255;
                pixels[4 * i + 1] = basins[i] == 0 ? (byte)90 : (byte)210;
                pixels[4 * i + 2] = basins[i] == 2 ? (byte)40 : (byte)255;
            }
            PngWriter.Write(stem + "-measurements.png", pixels, data.Resolution, data.Resolution);
        }

        // 陸の平均標高、標高 150 m 未満の陸の割合（%）、雪の土地の割合（%）。侵食の強さを変えた時に地域全体が持ち上がっていないかを見る
        internal static (double Mean, double Lowland, double Snow) Elevation(RegionData data)
        {
            double sum = 0;
            int land = 0, low = 0, snow = 0;
            for (int i = 0; i < data.Height.Length; i++)
            {
                float h = data.Height[i];
                if (h <= 0f) continue;
                land++;
                sum += h;
                if (h < 150f) low++;
                if (data.Biome[i] == RegionBiome.Snow) snow++;
            }
            land = Math.Max(1, land);
            return (sum / land, 100.0 * low / land, 100.0 * snow / land);
        }

        // 川として描くセル（流量が川の下限を超える陸、湖を除く）、海へ出る最大の流量（川の下限の倍数）、流れ先の無い陸のセル
        internal static (int River, double Mouth, int Terminal) CountRivers(RegionData data)
        {
            float threshold = RegionGenerator.RiverThreshold(data);
            int river = 0, terminal = 0;
            double mouth = 0;
            for (int i = 0; i < data.Height.Length; i++)
            {
                if (data.Height[i] <= 0f) continue;
                int r = data.Downstream[i];
                if (r < 0) terminal++;
                else if (data.Height[r] <= 0f) mouth = Math.Max(mouth, data.Flow[i] / threshold);
                if (data.LakeDepth[i] <= 0f && data.Flow[i] > threshold) river++;
            }
            return (river, mouth, terminal);
        }

        // 湖（Lake の土地）を 8 近傍でつながる塊に分けて数える
        internal static (int Count, int Cells, int Largest) CountLakes(RegionData data)
        {
            int n = data.Resolution;
            var seen = new bool[data.Biome.Length];
            var pending = new Stack<int>();
            int count = 0, cells = 0, largest = 0;
            for (int start = 0; start < seen.Length; start++)
            {
                if (seen[start] || data.Biome[start] != RegionBiome.Lake) continue;
                count++;
                int size = 0;
                seen[start] = true;
                pending.Push(start);
                while (pending.Count > 0)
                {
                    int i = pending.Pop();
                    size++;
                    int x = i % n, y = i / n;
                    for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= n || ny >= n) continue;
                        int j = nx + ny * n;
                        if (seen[j] || data.Biome[j] != RegionBiome.Lake) continue;
                        seen[j] = true;
                        pending.Push(j);
                    }
                }
                cells += size;
                largest = Math.Max(largest, size);
            }
            return (count, cells, largest);
        }

        private const double StreakCoherence = 0.5;

        /// <summary>
        /// 向きに依らない筋の計測。標高から 1 km の平均を引いた細かい起伏の勾配で、半径 2 km の窓の構造テンソルを作り、
        /// 向きのそろい具合 (λ1-λ2)/(λ1+λ2) を平野のセルごとに求める。ばらばらな起伏は 0 に近く、
        /// 平行な刻みは 1 に近い。平野の平均と、0.5 以上のセルの数を返す。
        /// </summary>
        internal static (double MeanCoherence, int StreakCells) FindStreaks(RegionData data, bool[] plain)
        {
            int n = data.Resolution;
            float cell = data.CellMeters;
            int count = n * n;
            var ground = new double[count];
            for (int i = 0; i < count; i++) ground[i] = Math.Max(0f, data.Height[i]);
            double[] mean = BoxMean(ground, n, Math.Max(1, (int)Math.Round(1000 / cell)));
            var detail = new double[count];
            for (int i = 0; i < count; i++) detail[i] = ground[i] - mean[i];
            var xx = new double[count];
            var yy = new double[count];
            var xy = new double[count];
            for (int y = 1; y < n - 1; y++)
            for (int x = 1; x < n - 1; x++)
            {
                int i = x + y * n;
                double gx = detail[i + 1] - detail[i - 1];
                double gy = detail[i + n] - detail[i - n];
                xx[i] = gx * gx;
                yy[i] = gy * gy;
                xy[i] = gx * gy;
            }
            int window = Math.Max(2, (int)Math.Round(2000 / cell));
            xx = BoxMean(xx, n, window);
            yy = BoxMean(yy, n, window);
            xy = BoxMean(xy, n, window);
            double sum = 0;
            int plainCount = 0, streaks = 0;
            for (int i = 0; i < count; i++)
            {
                if (!plain[i]) continue;
                double trace = xx[i] + yy[i];
                double c = trace <= 1e-12 ? 0 : Math.Sqrt((xx[i] - yy[i]) * (xx[i] - yy[i]) + 4 * xy[i] * xy[i]) / trace;
                sum += c;
                plainCount++;
                if (c >= StreakCoherence) streaks++;
            }
            return (sum / Math.Max(1, plainCount), streaks);
        }

        // 一辺 2r+1 の正方形の平均（端は内側だけで割る）。積分画像で求める。
        private static double[] BoxMean(double[] values, int n, int r)
        {
            var integral = new double[(n + 1) * (n + 1)];
            for (int y = 0; y < n; y++)
            {
                double row = 0;
                for (int x = 0; x < n; x++)
                {
                    row += values[x + y * n];
                    integral[(x + 1) + (y + 1) * (n + 1)] = integral[(x + 1) + y * (n + 1)] + row;
                }
            }
            var result = new double[n * n];
            for (int y = 0; y < n; y++)
            {
                int y0 = Math.Max(0, y - r), y1 = Math.Min(n, y + r + 1);
                for (int x = 0; x < n; x++)
                {
                    int x0 = Math.Max(0, x - r), x1 = Math.Min(n, x + r + 1);
                    double s = integral[x1 + y1 * (n + 1)] - integral[x0 + y1 * (n + 1)] - integral[x1 + y0 * (n + 1)] + integral[x0 + y0 * (n + 1)];
                    result[x + y * n] = s / ((x1 - x0) * (y1 - y0));
                }
            }
            return result;
        }

        internal static (int Count, int Cells, float MaximumDepth, int SeaLevelCount, int SeaLevelCells)
            FindBasins(RegionData data, byte[] mask, double cellKm2, bool inlandSeaIsOutlet = false)
        {
            int count = 0;
            int cells = 0;
            float maximumDepth = 0f;
            int seaLevelCount = 0;
            int seaLevelCells = 0;

            RegionLake.VisitBasins(data.Height, data.Resolution, inlandSeaIsOutlet, (_, component, depth, reachesSeaLevel) =>
            {
                if (depth < MinimumBasinDepth || component.Count * cellKm2 < MinimumBasinKm2) return;
                if (reachesSeaLevel)
                {
                    seaLevelCount++;
                    seaLevelCells += component.Count;
                }
                else
                {
                    count++;
                    cells += component.Count;
                    maximumDepth = Math.Max(maximumDepth, depth);
                }
                foreach (int index in component) mask[index] = reachesSeaLevel ? (byte)2 : (byte)1;
            });

            return (count, cells, maximumDepth, seaLevelCount, seaLevelCells);
        }

        internal static (int Plain, int Striped) FindStripes(RegionData data, bool[] stripes, bool[] collectPlain = null,
            bool[] fixedPlain = null)
        {
            int n = data.Resolution;
            float cell = data.CellMeters;
            float[] height = data.Height;
            int slopeRadius = Math.Max(1, (int)Math.Round(1000 / cell));
            float slopeDistance = 2f * cell * slopeRadius;
            int north = slopeRadius * n;
            var plain = fixedPlain ?? collectPlain ?? new bool[height.Length];
            int plainCount = 0;
            if (fixedPlain != null)
            {
                foreach (bool isPlain in fixedPlain)
                    if (isPlain) plainCount++;
            }
            else
            {
                for (int y = slopeRadius; y < n - slopeRadius; y++)
                for (int x = slopeRadius; x < n - slopeRadius; x++)
                {
                    int i = x + y * n;
                    if (height[i] <= 0f || height[i] > PlainHeightLimit || data.Biome[i] == RegionBiome.River || data.Biome[i] == RegionBiome.Lake) continue;
                    if (height[i - slopeRadius] <= 0f || height[i + slopeRadius] <= 0f || height[i - north] <= 0f || height[i + north] <= 0f) continue;
                    float dx = height[i + slopeRadius] - height[i - slopeRadius];
                    float dy = height[i + north] - height[i - north];
                    float limit = slopeDistance * PlainSlopeLimit;
                    if (dx * dx + dy * dy > limit * limit) continue;
                    plain[i] = true;
                    plainCount++;
                }
            }

            var grooves = new bool[height.Length];
            var straight = new bool[height.Length];
            foreach ((int x, int y) in Directions)
            {
                Array.Clear(grooves);
                Array.Clear(straight);
                double step = cell * Math.Sqrt(x * x + y * y);
                int across = x * n - y;
                int along = x + y * n;
                int halfWidth = Math.Max(1, (int)Math.Round(500 / step));
                int sample = Math.Max(1, (int)Math.Round(750 / step));
                int firstNeighbor = Math.Max(1, (int)Math.Ceiling(1000 / step));
                int lastNeighbor = (int)Math.Floor(2500 / step);

                for (int row = halfWidth; row < n - halfWidth; row++)
                for (int col = halfWidth; col < n - halfWidth; col++)
                {
                    int i = col + row * n;
                    int offset = across * halfWidth;
                    if (!plain[i] || !plain[i - offset] || !plain[i + offset]) continue;
                    float left = height[i - offset] - height[i];
                    float right = height[i + offset] - height[i];
                    grooves[i] = Math.Min(left, right) >= 0.1f && left + right >= 1.2f;
                }

                int margin = 2 * sample + halfWidth;
                int stride = along * sample;
                for (int row = margin; row < n - margin; row++)
                for (int col = margin; col < n - margin; col++)
                {
                    int i = col + row * n;
                    if (!grooves[i]) continue;
                    int votes = 0;
                    for (int offset = -2; offset <= 2; offset++)
                        if (grooves[i + offset * stride]) votes++;
                    straight[i] = votes >= 4;
                }

                for (int row = lastNeighbor; row < n - lastNeighbor; row++)
                for (int col = lastNeighbor; col < n - lastNeighbor; col++)
                {
                    int i = col + row * n;
                    if (!straight[i] || stripes[i]) continue;
                    for (int distance = firstNeighbor; distance <= lastNeighbor; distance++)
                    {
                        int offset = across * distance;
                        if (!straight[i - offset] && !straight[i + offset]) continue;
                        stripes[i] = true;
                        break;
                    }
                }
            }

            int stripeCount = 0;
            foreach (bool stripe in stripes)
                if (stripe) stripeCount++;
            return (plainCount, stripeCount);
        }
    }
}
