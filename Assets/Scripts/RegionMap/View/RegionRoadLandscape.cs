using System;
using System.Collections.Generic;
using NVector2 = System.Numerics.Vector2;
using UnityEngine;

namespace Vastcore.RegionMap
{
    /// <summary>Plants for the farmland sample, using the existing RegionMap atlas and batched tiles.</summary>
    public static class RegionRoadLandscape
    {
        public static void PlantFarmland(Transform parent, RegionRoadPlan plan, Func<float, float, float> height)
        {
            var plants = new List<RegionPlant>();
            uint seed = (uint)plan.Seed ^ 0xa7c1u;
            const float treeSpacing = 16f;
            const int treeLimit = 136;
            for (int z = -treeLimit; z <= treeLimit; z++)
            for (int x = -treeLimit; x <= treeLimit; x++)
            {
                var p = new NVector2(treeSpacing * (x + RegionNoise.Hash01(x, z, seed)), treeSpacing * (z + RegionNoise.Hash01(x, z, seed + 1)));
                float valley = Math.Abs(p.X + 65f * Mathf.Sin(p.Y / 680f));
                float patch = RegionNoise.Fbm(p.X / 155f, p.Y / 215f, 2, seed + 2);
                float forest = RegionNoise.Smooth(430f, 1010f, valley) * RegionNoise.Smooth(-0.11f, 0.21f, patch);
                float crest = 0.36f * RegionNoise.Smooth(1190f, 1770f, p.Y) * RegionNoise.Smooth(-0.4f, 0.24f, patch);
                if (RegionNoise.Hash01(x, z, seed + 3) > Mathf.Max(forest, crest) * 0.91f || Reserved(plan, p)) continue;
                RegionPlantKind kind = patch > 0.16f ? RegionPlantKind.Juniper : RegionPlantKind.RiparianTree;
                Add(plants, height, p, kind, 4.2f + 7.8f * RegionNoise.Hash01(x, z, seed + 4), seed, x, z);
            }

            // Hedgerows follow selected field boundaries. Short runs and gaps keep the broad fields legible.
            for (int row = -15; row <= 15; row++)
            for (int x = -277; x <= 277; x++)
            {
                float px = 3.1f * (x + RegionNoise.Hash01(x, row, seed + 21));
                float pz = RegionRoadPlan.FieldDepth * (row - px * RegionRoadPlan.FieldSkew) +
                    5f + 5f * RegionNoise.Hash01(x, row, seed + 22);
                var p = new NVector2(px, pz);
                float patch = RegionNoise.Fbm(px / 92f, row / 2f, 2, seed + 24);
                if (patch < 0.015f || RegionNoise.Hash01(x, row, seed + 25) < 0.10f || Reserved(plan, p)) continue;
                float scale = RegionNoise.Hash01(x, row, seed + 26);
                Add(plants, height, p, scale > 0.78f ? RegionPlantKind.RiparianTree : RegionPlantKind.Juniper,
                    scale > 0.78f ? 4.5f + 2.1f * scale : 1.7f + 2.7f * scale, seed + 22, x, row);
            }

            RegionPlantView.AddPlants(parent, plants, "Field boundaries and woodland", new Color(0.77f, 0.85f, 0.74f), "Farmland canopy");
        }

        public static void PlantVerges(Transform parent, RegionRoadPlan plan, Func<float, float, float> height)
        {
            var plants = new List<RegionPlant>();
            uint seed = (uint)plan.Seed ^ 0xf421u;
            foreach (RegionRoadCorridor road in new[] { plan.Highway, plan.Arterial })
            {
                for (int i = 2; i * 3.8f < road.Length - 8f; i++)
                for (int side = -1; side <= 1; side += 2)
                {
                    float distance = (i + RegionNoise.Hash01(i, side, seed)) * 3.8f;
                    RegionRoadStation frame = road.At(distance);
                    float stretch = RegionNoise.Fbm(distance / 87f, side, 2, seed);
                    if (RegionNoise.Hash01(i, side, seed + 21) > 0.53f + 0.35f * stretch) continue;
                    if (road == plan.Arterial && side == plan.Site.Side && Math.Abs(distance - plan.Site.Station.Distance) < 13f) continue;
                    for (int tuft = 0; tuft < 3; tuft++)
                    {
                        NVector2 p = frame.AtOffset(side * (road.Profile.HalfWidth + 2.25f +
                            (tuft + RegionNoise.Hash01(i, side, seed + (uint)tuft + 4)) * 1.45f));
                        p += frame.Forward * ((RegionNoise.Hash01(i, side, seed + (uint)tuft + 31) - 0.5f) * 3f);
                        Add(plants, height, p, RegionPlantKind.Grass,
                            0.35f + 0.59f * RegionNoise.Hash01(i, side, seed + (uint)tuft + 9), seed + (uint)tuft, i, side);
                    }
                }
                seed += 2783u;
            }
            RegionPlantView.AddPlants(parent, plants, "Grass on the road shoulders", new Color(0.79f, 0.93f, 0.72f), "Roadside grass");
        }

        private static bool Reserved(RegionRoadPlan plan, NVector2 point) =>
            plan.Highway.TryProject(point, out _, out _) || plan.Arterial.TryProject(point, out _, out _) ||
            NVector2.DistanceSquared(point, plan.Site.PlotCenter) < 69f * 69f;

        private static void Add(List<RegionPlant> plants, Func<float, float, float> height, NVector2 point,
            RegionPlantKind kind, float size, uint seed, int x, int z)
        {
            plants.Add(new RegionPlant
            {
                Kind = kind, X = point.X, Y = height(point.X, point.Y), Z = -point.Y, Height = size,
                Yaw = RegionNoise.Hash01(x, z, seed + 12) * (2f * Mathf.PI),
                Variant = (int)(4f * RegionNoise.Hash01(x, z, seed + 13)), Sun = 1f, Sky = 0.86f,
            });
        }
    }
}
