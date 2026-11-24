using System;
using System.Collections.Generic;
using System.IO;

namespace Vastcore.RegionMap.Preview
{
    internal static class TerrainMeasurements
    {
        private const double MinimumBasinKm2 = 1.0;
        private const float MinimumBasinDepth = 2f;
        private const float PlainHeightLimit = 350f;
        private const float PlainSlopeLimit = 0.05f;
        private static readonly (int X, int Y)[] Directions = { (1, 0), (1, 1), (0, 1), (-1, 1) };

        internal static void Write(RegionData data, string stem, long generateMs, long erosionMs)
        {
            double cellKm = data.CellMeters / 1000.0;
            double cellKm2 = cellKm * cellKm;
            var basins = new byte[data.Height.Length];
            var stripes = new bool[data.Height.Length];
            var basin = FindBasins(data, basins, cellKm2);
            var (plainCells, stripeCells) = FindStripes(data, stripes);
            int lakeCells = 0;
            foreach (RegionBiome biome in data.Biome)
                if (biome == RegionBiome.Lake) lakeCells++;

            File.WriteAllLines(stem + "-measurements.csv", new[]
            {
                "seed,resolution,size_km,generation_ms,erosion_ms,basins,basin_km2,deepest_basin_m,sea_level_basins,sea_level_basin_km2,lake_km2,plain_km2,stripe_km2,stripe_plain_pct",
                FormattableString.Invariant($"{data.Spec.Seed},{data.Resolution},{data.SizeKm},{generateMs},{erosionMs},{basin.Count},{basin.Cells * cellKm2:F4},{basin.MaximumDepth:F3},{basin.SeaLevelCount},{basin.SeaLevelCells * cellKm2:F4},{lakeCells * cellKm2:F4},{plainCells * cellKm2:F4},{stripeCells * cellKm2:F4},{100.0 * stripeCells / Math.Max(1, plainCells):F4}")
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

        internal static (int Count, int Cells, float MaximumDepth, int SeaLevelCount, int SeaLevelCells)
            FindBasins(RegionData data, byte[] mask, double cellKm2)
        {
            int n = data.Resolution;
            float[] height = data.Height;
            float[] filled = FillToOutlet(height, n);
            var component = new List<int>();
            int count = 0;
            int cells = 0;
            float maximumDepth = 0f;
            int seaLevelCount = 0;
            int seaLevelCells = 0;

            for (int i = 0; i < height.Length; i++)
            {
                float spill = filled[i];
                if (spill <= 0f || spill <= height[i]) continue;

                component.Clear();
                component.Add(i);
                filled[i] = height[i];
                float depth = spill - height[i];
                for (int next = 0; next < component.Count; next++)
                {
                    int current = component[next];
                    int x = current % n;
                    int y = current / n;
                    // 外周は流出口で、窪地に入るセルは内側に限られる。
                    for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int neighbor = x + dx + (y + dy) * n;
                        if (filled[neighbor] != spill || height[neighbor] >= spill) continue;
                        component.Add(neighbor);
                        filled[neighbor] = height[neighbor];
                        depth = Math.Max(depth, spill - height[neighbor]);
                    }
                }

                if (depth < MinimumBasinDepth || component.Count * cellKm2 < MinimumBasinKm2) continue;
                bool reachesSeaLevel = depth >= spill;
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
            }

            return (count, cells, maximumDepth, seaLevelCount, seaLevelCells);
        }

        // Barnes et al. (2014), Priority-Flood: 計測では流路用の微小勾配を加えず、流出口の標高で止める。
        private static float[] FillToOutlet(float[] height, int n)
        {
            var open = new PriorityQueue<int, float>();
            var pit = new Queue<int>();
            var filled = new float[height.Length];
            Array.Fill(filled, float.NaN);

            for (int i = 0; i < height.Length; i++)
            {
                int x = i % n;
                int y = i / n;
                if (x != 0 && x != n - 1 && y != 0 && y != n - 1) continue;
                filled[i] = height[i];
                open.Enqueue(i, height[i]);
            }

            while (pit.Count > 0 || open.Count > 0)
            {
                int current = pit.Count > 0 ? pit.Dequeue() : open.Dequeue();
                float level = filled[current];
                int x = current % n;
                int y = current / n;
                for (int row = Math.Max(0, y - 1); row <= Math.Min(n - 1, y + 1); row++)
                for (int col = Math.Max(0, x - 1); col <= Math.Min(n - 1, x + 1); col++)
                {
                    int neighbor = col + row * n;
                    if (!float.IsNaN(filled[neighbor])) continue;
                    float ground = height[neighbor];
                    filled[neighbor] = Math.Max(ground, level);
                    if (ground <= level) pit.Enqueue(neighbor);
                    else open.Enqueue(neighbor, ground);
                }
            }

            return filled;
        }

        internal static (int Plain, int Striped) FindStripes(RegionData data, bool[] stripes)
        {
            int n = data.Resolution;
            float cell = data.CellMeters;
            float[] height = data.Height;
            int slopeRadius = Math.Max(1, (int)Math.Round(1000 / cell));
            float slopeDistance = 2f * cell * slopeRadius;
            int north = slopeRadius * n;
            var plain = new bool[height.Length];
            int plainCount = 0;
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
