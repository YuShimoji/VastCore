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
                metrics.WriteLine("phase,resolution,outlets,land_basins,land_km2,max_depth_m,closed_sea_basins,closed_sea_km2,plain_km2,stripe_km2");
                foreach (var snapshot in _snapshots)
                {
                    RegionSpec spec = _spec.Clone();
                    spec.Resolution = snapshot.Resolution;
                    var data = new RegionData(spec);
                    Array.Copy(snapshot.Height, data.Height, snapshot.Height.Length);
                    double cellKm = data.CellMeters / 1000.0;
                    double cellArea = cellKm * cellKm;
                    var (plain, striped) = FindStripes(data, new bool[data.Height.Length]);
                    foreach (bool inlandSeaIsOutlet in new[] { false, true })
                    {
                        var basin = FindBasins(data, new byte[data.Height.Length], cellArea, inlandSeaIsOutlet);
                        string outlets = inlandSeaIsOutlet ? "all-low-cells" : "boundary";
                        metrics.WriteLine(FormattableString.Invariant($"{snapshot.Phase},{snapshot.Resolution},{outlets},{basin.Count},{basin.Cells * cellArea:F4},{basin.MaximumDepth:F3},{basin.SeaLevelCount},{basin.SeaLevelCells * cellArea:F4},{plain * cellArea:F4},{striped * cellArea:F4}"));
                    }
                }
                WriteProfile(stem);
            }

            private void WriteProfile(string stem)
            {
                var snapshot = _snapshots.Find(row => row.Phase == "interpolated");
                if (snapshot.Height == null) return;
                var candidate = new RegionData(_spec);
                Array.Copy(snapshot.Height, candidate.Height, snapshot.Height.Length);
                if (RegionLake.Select(candidate) == null) return;
                int center = 0;
                for (int i = 1; i < candidate.LakeDepth.Length; i++)
                    if (candidate.LakeDepth[i] > candidate.LakeDepth[center]) center = i;

                float mapX = candidate.MapXFromCell(center);
                float mapY = candidate.MapYFromCell(center);
                float level = candidate.Height[center] + candidate.LakeDepth[center];
                using var profile = new StreamWriter(stem + "-terrain-profile.csv");
                profile.WriteLine("phase,axis,offset_km,map_x,map_y,height_m,proposed_lake_m");
                foreach (var row in _snapshots)
                for (int axis = 0; axis < 2; axis++)
                for (int sample = -16; sample <= 16; sample++)
                {
                    float distance = sample * 0.25f;
                    float offset = distance * RegionData.MapUnits / _spec.SizeKm;
                    int x = Math.Clamp((int)((mapX + (axis == 0 ? offset : 0f)) * row.Resolution / RegionData.MapUnits), 0, row.Resolution - 1);
                    int y = Math.Clamp((int)((mapY + (axis == 1 ? offset : 0f)) * row.Resolution / RegionData.MapUnits), 0, row.Resolution - 1);
                    float h = row.Height[x + y * row.Resolution];
                    float xMap = (x + 0.5f) * RegionData.MapUnits / row.Resolution;
                    float yMap = (y + 0.5f) * RegionData.MapUnits / row.Resolution;
                    profile.WriteLine(FormattableString.Invariant($"{row.Phase},{(axis == 0 ? 'x' : 'y')},{distance:F2},{xMap:F4},{yMap:F4},{h:F3},{level:F3}"));
                }
            }
        }

        private const double MinimumBasinKm2 = 1.0;
        private const float MinimumBasinDepth = 2f;
        private const float PlainHeightLimit = 350f;
        private const float PlainSlopeLimit = 0.05f;
        private static readonly (int X, int Y)[] Directions = { (1, 0), (1, 1), (0, 1), (-1, 1) };

        internal static void WriteLakeDetail(RegionData region, string stem)
        {
            if (float.IsNaN(region.LakeSurfaceLevel)) throw new InvalidOperationException("No retained lake in this region.");
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
            Console.WriteLine(FormattableString.Invariant($"lake center={mapX:F2},{mapY:F2} level={region.LakeSurfaceLevel:F2}m area={lakeCells * cellKm * cellKm:F2}km2 depth={region.LakeDepth[center]:F2}m detail_lake_cells={detailLakeCells}"));
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
