using System;
using System.Collections.Generic;

namespace Vastcore.RegionMap
{
    public enum RegionWoodlandPreset { Forest, Rainforest, NaturePark }
    public enum RegionWoodlandForm { Conifer, SpreadingTree, TallBroadleaf, Shrub, Fern, BroadleafHerb, Grass }

    public readonly struct RegionWoodlandPlant
    {
        public readonly RegionWoodlandForm Form;
        public readonly float East, South, Ground, Height, Yaw;
        public readonly int Shape;

        internal RegionWoodlandPlant(RegionWoodlandForm form, float east, float south, float ground, float height, float yaw, int shape)
        {
            Form = form;
            East = east;
            South = south;
            Ground = ground;
            Height = height;
            Yaw = yaw;
            Shape = shape;
        }
    }

    // Distances and heights are metres. The layout seed controls the stand composition, including
    // each plant's size and position. The shape seed changes its mesh while keeping that layout.
    public sealed class RegionWoodlandLayout
    {
        public const float TrailLength = 360f;
        private const float StandRadius = 367f;
        private readonly uint layoutSeed;

        public RegionWoodlandPreset Preset { get; }
        public float TrailHalfWidth { get; }
        public List<RegionWoodlandPlant> Canopy { get; } = new List<RegionWoodlandPlant>();
        public List<RegionWoodlandPlant> Understory { get; } = new List<RegionWoodlandPlant>();

        private RegionWoodlandLayout(RegionWoodlandPreset preset, int seed)
        {
            Preset = preset;
            layoutSeed = unchecked((uint)seed);
            TrailHalfWidth = preset == RegionWoodlandPreset.NaturePark ? 1.65f : preset == RegionWoodlandPreset.Forest ? 0.95f : 0.72f;
        }

        public static RegionLookSpec LookSpec(RegionWoodlandPreset preset, float mapX, float mapY)
        {
            RegionBiome biome = preset switch
            {
                RegionWoodlandPreset.Rainforest => RegionBiome.Rainforest,
                RegionWoodlandPreset.NaturePark => RegionBiome.Grassland,
                _ => RegionBiome.Forest,
            };
            var spec = RegionLookSpec.ForBiome(biome, mapX, mapY);
            spec.Resolution = 384;
            spec.InnerKm = 0.82f;
            spec.OuterKm = 1.55f;
            spec.GrasslandCover = false;
            return spec;
        }

        public static RegionWoodlandLayout Build(RegionLookScene scene, RegionWoodlandPreset preset, int layoutSeed, int shapeSeed)
        {
            var layout = new RegionWoodlandLayout(preset, layoutSeed);
            bool rainforest = preset == RegionWoodlandPreset.Rainforest;
            float spacing = rainforest ? 8f : 9.7f;
            layout.Scatter(scene, layout.Canopy, StandRadius, spacing, unchecked((uint)shapeSeed), true);
            layout.Scatter(scene, layout.Understory, 100f, rainforest ? 2.05f : 2.4f, unchecked((uint)shapeSeed), false);
            return layout;
        }

        // Use the same terrain when comparing presets. Prefer forest cells with gentle relief so
        // the reference trail is visible and the sample does not put a woodland on a lake or cliff.
        public static int SelectSite(RegionData region)
        {
            int n = region.Resolution;
            int step = Math.Max(1, n / 224);
            int reach = Math.Max(1, (int)Math.Ceiling(510f / region.CellMeters));
            int margin = Math.Max(2 * reach, n / 14);
            float best = float.NegativeInfinity;
            int result = -1;
            for (int z = margin; z < n - margin; z += step)
            for (int x = margin; x < n - margin; x += step)
            {
                int i = region.Index(x, z);
                if (!IsDryGround(region, i) || region.Temperature[i] < 7f) continue;
                float h = region.Height[i];
                float relief = 0f;
                bool supported = true;
                for (int dz = -reach; dz <= reach; dz += reach)
                for (int dx = -reach; dx <= reach; dx += reach)
                {
                    int neighbor = region.Index(x + dx, z + dz);
                    supported &= IsDryGround(region, neighbor);
                    relief = Math.Max(relief, Math.Abs(region.Height[neighbor] - h));
                }
                if (!supported || h < 45f || relief > 85f) continue;
                float biomeFit = region.Biome[i] == RegionBiome.Forest ? 1f : region.Biome[i] == RegionBiome.Grassland ? 0.5f : 0f;
                float score = 1.2f * biomeFit - 0.34f * Math.Abs(relief - 27f) / 27f -
                    0.55f * Math.Abs(region.Moisture[i] - 0.62f) - 0.25f * h / 800f;
                if (score <= best) continue;
                best = score;
                result = i;
            }
            if (result < 0) throw new InvalidOperationException("No gently sloping inland woodland site was found for this seed.");
            return result;
        }

        public float TrailEast(float south) => 10f * (float)Math.Sin(south / 33f) + 3f * (float)Math.Sin(south / 14f);

        // A conservative lower bound on clearance from the curve, including where it bends.
        public float TrailClearance(float east, float south) =>
            Math.Abs(east - TrailEast(south)) * 0.88f - TrailHalfWidth;

        public float Meadow(float east, float south)
        {
            if (Preset != RegionWoodlandPreset.NaturePark) return 0f;
            float x = (east - TrailEast(south) - 35f) / 57f;
            float z = (south - 8f) / 85f;
            float edge = 1f - x * x - z * z + 0.29f * RegionNoise.Gradient(east / 15f, south / 18f, layoutSeed + 11);
            return RegionNoise.Smooth(-0.14f, 0.34f, edge);
        }

        public bool TryGround(RegionLookScene scene, float east, float south, out float height, out float moisture)
        {
            float x = scene.CenterX + east, z = scene.CenterZ + south;
            int ix = (int)Math.Round((x - scene.InnerMinX) / scene.InnerCell);
            int iz = (int)Math.Round((z - scene.InnerMinZ) / scene.InnerCell);
            RegionData data = scene.Inner.Data;
            height = moisture = 0f;
            if (ix < 1 || iz < 1 || ix >= data.Resolution - 1 || iz >= data.Resolution - 1) return false;
            int index = data.Index(ix, iz);
            if (!IsDryGround(data, index) || (data.WaterCoverage != null && data.WaterCoverage[index] > 0.2f)) return false;
            float relief = Math.Max(Math.Abs(data.Height[index - 1] - data.Height[index + 1]),
                Math.Abs(data.Height[index - data.Resolution] - data.Height[index + data.Resolution]));
            if (relief > 1.6f * scene.InnerCell) return false;
            height = scene.RenderedHeightAt(x, z);
            moisture = data.Moisture[index];
            return true;
        }

        private static bool IsDryGround(RegionData data, int index) =>
            data.Height[index] > 0f && data.LakeDepth[index] <= 0f && data.Biome[index] != RegionBiome.River;

        private void Scatter(RegionLookScene scene, List<RegionWoodlandPlant> plants, float extent, float spacing, uint formSeed, bool canopy)
        {
            bool rainforest = Preset == RegionWoodlandPreset.Rainforest;
            bool forest = Preset == RegionWoodlandPreset.Forest;
            uint salt = layoutSeed + (canopy ? 111u : 409u);
            int radius = (int)(extent / spacing);
            for (int iz = -radius; iz <= radius; iz++)
            for (int ix = -radius; ix <= radius; ix++)
            {
                float jitter = canopy && !rainforest ? 0.81f : 0.67f;
                float east = (ix + jitter * (RegionNoise.Hash01(ix, iz, salt) - 0.5f)) * spacing;
                float south = (iz + jitter * (RegionNoise.Hash01(ix, iz, salt + 1) - 0.5f)) * spacing;
                float clearance = TrailClearance(east, south);
                if (clearance < (canopy ? rainforest ? 3.2f : 2.2f : 0.70f)) continue;
                if (!TryGround(scene, east, south, out float height, out float moisture)) continue;

                float grove = RegionNoise.Smooth(-0.39f, 0.27f, RegionNoise.Gradient(east / 42f, south / 49f, layoutSeed + 17));
                float patch = RegionNoise.Smooth(-0.45f, 0.35f, RegionNoise.Gradient(east / 11f, south / 13f, layoutSeed + 71));
                float thicket = 0f;
                if (rainforest && !canopy)
                {
                    // The young trees and fern colonies share a broader pattern of thickets and gaps.
                    float stand = RegionNoise.FbmTurned(east / 29f, south / 34f, 2, layoutSeed + 503u);
                    thicket = RegionNoise.Smooth(-0.22f, 0.20f, stand + 0.26f * (patch - 0.5f));
                }
                float meadow = Meadow(east, south);
                float edge = 1f - RegionNoise.Smooth(3f, 23f, clearance);
                if (forest)
                {
                    // Let a whole stand share its canopy density and maturity, with smaller gaps within it.
                    float stand = RegionNoise.FbmTurned(east / 97f, south / 112f, 2, layoutSeed + 239);
                    grove = RegionNoise.Smooth(-0.23f, 0.22f, stand + 0.32f * (grove - 0.5f));
                }
                float treeDensity = rainforest ? 0.47f + grove * 0.49f :
                    forest ? 0.064f + grove * 0.91f : 0.075f + grove * grove * 0.88f;
                float understory = rainforest ? 0.18f + 0.51f * thicket + 0.13f * patch + 0.11f * edge
                    : 0.15f + 0.45f * patch + 0.18f * edge;
                understory *= 0.85f + 0.24f * moisture;
                float probability = canopy ? treeDensity * (1f - meadow) : understory;
                if (RegionNoise.Hash01(ix, iz, salt + 2) >= probability) continue;

                float pick = RegionNoise.Hash01(ix, iz, salt + 3);
                float size = RegionNoise.Hash01(ix, iz, salt + 4);
                float needleBias = 0.43f + 0.24f * (1f - moisture);
                if (forest)
                {
                    float composition = RegionNoise.Gradient(east / 102f, south / 81f, layoutSeed + 347);
                    needleBias += 0.38f * composition;
                    size = size * 0.64f + grove * 0.36f;
                }
                var form = canopy
                    ? rainforest && pick < 0.82f ? RegionWoodlandForm.TallBroadleaf
                        : forest && pick < needleBias ? RegionWoodlandForm.Conifer : RegionWoodlandForm.SpreadingTree
                    : meadow > 0.3f || pick < (rainforest ? 0.14f : 0.20f) ? RegionWoodlandForm.Grass
                        : rainforest && pick > 0.986f - 0.16f * thicket && clearance > 2.8f ? RegionWoodlandForm.SpreadingTree
                        : rainforest && pick > 0.66f ? RegionWoodlandForm.BroadleafHerb
                        : forest && pick > 0.94f && grove > 0.42f && clearance > 3.8f ? RegionWoodlandForm.SpreadingTree
                        : pick > 0.73f && clearance > 2.2f ? RegionWoodlandForm.Shrub : RegionWoodlandForm.Fern;
                float metres = form switch
                {
                    RegionWoodlandForm.TallBroadleaf => 19f + 14f * size,
                    RegionWoodlandForm.Conifer => 11f + 22f * size,
                    RegionWoodlandForm.SpreadingTree => canopy ? 9f + (forest ? 17f : 13f) * size
                        : (rainforest ? 4.1f : 3.4f) + 4f * size,
                    RegionWoodlandForm.Shrub => 0.85f + 1.0f * size,
                    RegionWoodlandForm.Fern => rainforest ? 0.48f + 1.14f * size : 0.38f + 0.78f * size,
                    RegionWoodlandForm.BroadleafHerb => 0.66f + 1.3f * size,
                    _ => 0.15f + 0.40f * size,
                };
                float horizontalReach = form == RegionWoodlandForm.Grass ? 0.78f
                    : rainforest && form == RegionWoodlandForm.SpreadingTree ? 0.58f : 1.25f;
                if (!canopy && clearance < metres * horizontalReach + 0.20f) continue;
                int shape = (int)(6f * RegionNoise.Hash01(ix, iz, formSeed + (canopy ? 811u : 1717u)));
                float yaw = 360f * RegionNoise.Hash01(ix, iz, salt + 5);
                plants.Add(new RegionWoodlandPlant(form, east, south, height, metres, yaw, shape));

                if (rainforest && form == RegionWoodlandForm.Fern && clearance < 9f &&
                    RegionNoise.Hash01(ix, iz, salt + 6) < 0.58f)
                {
                    // A smaller neighbouring rosette overlaps the existing fronds on the trail shoulder.
                    double angle = yaw * Math.PI / 180.0;
                    float smaller = metres * (0.52f + 0.22f * RegionNoise.Hash01(ix, iz, salt + 7));
                    float neighborEast = east + (float)Math.Cos(angle) * metres * 0.63f;
                    float neighborSouth = south + (float)Math.Sin(angle) * metres * 0.63f;
                    if (TrailClearance(neighborEast, neighborSouth) > smaller * 1.25f + 0.20f &&
                        TryGround(scene, neighborEast, neighborSouth, out float neighborHeight, out _))
                    {
                        int neighborShape = (int)(6f * RegionNoise.Hash01(ix, iz, formSeed + 1879u));
                        plants.Add(new RegionWoodlandPlant(form, neighborEast, neighborSouth, neighborHeight,
                            smaller, (yaw + 143f) % 360f, neighborShape));
                    }
                }
            }
        }
    }
}
