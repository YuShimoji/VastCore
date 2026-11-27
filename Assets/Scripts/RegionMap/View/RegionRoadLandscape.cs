using System;
using System.Collections.Generic;
using NVector2 = System.Numerics.Vector2;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap
{
    /// <summary>Plants for the farmland sample, using the existing RegionMap atlas and batched tiles.</summary>
    public static class RegionRoadLandscape
    {
        public static void PlantFarmland(Transform parent, RegionRoadPlan plan, Func<float, float, float> height,
            bool detailed = false, bool refined = false)
        {
            var plants = new List<RegionPlant>();
            uint seed = (uint)plan.Seed ^ 0xa7c1u;
            if (refined) PlantWoodland(parent, plan, height, plants, seed);
            else PlantPreviousWoodland(plan, height, plants, seed, detailed);

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
                if (detailed)
                {
                    // On the same bank, separate long occupied runs with shorter openings and low margins.
                    float cover = RegionNoise.Fbm(px / 26f, row / 3f, 2, seed + 43);
                    if (cover < -0.09f) continue;
                    float top = 1.1f + 2.2f * RegionNoise.Smooth(-0.06f, 0.23f, cover);
                    Add(plants, height, p, RegionPlantKind.Juniper, top * (0.60f + 0.55f * scale), seed + 22, x, row);
                    if (cover > 0.10f)
                        Add(plants, height, p + new NVector2(1.15f, -1.4f), RegionPlantKind.Shrub,
                            top * 0.62f, seed + 51u, x, row);
                    continue;
                }
                Add(plants, height, p, scale > 0.78f ? RegionPlantKind.RiparianTree : RegionPlantKind.Juniper,
                    scale > 0.78f ? 4.5f + 2.1f * scale : 1.7f + 2.7f * scale, seed + 22, x, row);
            }

            RegionPlantView.AddPlants(parent, plants, "Field boundaries and woodland",
                detailed ? new Color(0.70f, 0.79f, 0.67f) : new Color(0.77f, 0.85f, 0.74f), "Farmland canopy");
        }

        private static void PlantPreviousWoodland(RegionRoadPlan plan, Func<float, float, float> height,
            List<RegionPlant> plants, uint seed, bool detailed)
        {
            float treeSpacing = detailed ? 12.5f : 16f;
            int treeLimit = (int)(2176f / treeSpacing);
            for (int z = -treeLimit; z <= treeLimit; z++)
            for (int x = -treeLimit; x <= treeLimit; x++)
            {
                var p = new NVector2(treeSpacing * (x + RegionNoise.Hash01(x, z, seed)), treeSpacing * (z + RegionNoise.Hash01(x, z, seed + 1)));
                float valley = Math.Abs(p.X + 65f * Mathf.Sin(p.Y / 680f));
                float patch = RegionNoise.Fbm(p.X / 155f, p.Y / 215f, 2, seed + 2);
                float forest = detailed
                    ? RegionNoise.Smooth(290f, 860f, valley) * RegionNoise.Smooth(-0.07f, 0.15f, patch)
                    : RegionNoise.Smooth(430f, 1010f, valley) * RegionNoise.Smooth(-0.11f, 0.21f, patch);
                float crest = 0.36f * RegionNoise.Smooth(1190f, 1770f, p.Y) * RegionNoise.Smooth(-0.4f, 0.24f, patch);
                if (RegionNoise.Hash01(x, z, seed + 3) > Mathf.Max(forest, crest) * 0.91f || Reserved(plan, p)) continue;
                RegionPlantKind kind = patch > 0.16f ? RegionPlantKind.Juniper : RegionPlantKind.RiparianTree;
                float size = 4.2f + 7.8f * RegionNoise.Hash01(x, z, seed + 4) +
                    (detailed ? 3.2f * RegionNoise.Smooth(0.28f, 0.73f, forest) : 0f);
                Add(plants, height, p, kind, size, seed, x, z);
                if (!detailed || forest <= 0.67f) continue;
                var neighbor = p + 9f * new NVector2(RegionNoise.Hash01(x, z, seed + 5) - 0.5f,
                    RegionNoise.Hash01(x, z, seed + 6) - 0.5f);
                if (!Reserved(plan, neighbor))
                    Add(plants, height, neighbor, kind, size * 0.72f, seed + 91u, x, z);
            }
        }

        private static void PlantWoodland(Transform parent, RegionRoadPlan plan, Func<float, float, float> height,
            List<RegionPlant> edgeTrees, uint seed)
        {
            var shader = Resources.Load<Shader>("RegionMap3D/RegionWoodland");
            if (!shader) throw new InvalidOperationException("The woodland shader is missing.");
            var tiles = new Dictionary<Vector2Int, RegionWoodlandGeometry>();
            const int variations = 8;
            var crowns = new RegionWoodlandGeometry[2 * variations];
            for (int i = 0; i < crowns.Length; i++)
            {
                int variation = i % variations;
                bool distant = i >= variations;
                var rng = new System.Random(1681 + 193 * variation);
                var crown = new RegionWoodlandGeometry();
                float spread = Mathf.Lerp(0.39f, 0.57f, (float)rng.NextDouble());
                float depth = Mathf.Lerp(0.66f, 0.99f, (float)rng.NextDouble());
                float skirt = Mathf.Lerp(0.49f, 0.66f, (float)rng.NextDouble());
                var leaves = Color32.Lerp(new Color32(57, 83, 56, 255), new Color32(78, 101, 62, 255), variation / 7f);
                if (!distant)
                    crown.Tube(Vector3.down * 0.025f, Vector3.up * 0.53f, 0.027f, 0.010f, new Color32(72, 61, 45, 255), 3);
                crown.Blob(new Vector3(-0.10f, skirt, 0.06f), new Vector3(spread, 0.30f, spread * depth), leaves, rng,
                    distant ? 5 : 6, distant ? 2 : 3);
                crown.Blob(new Vector3(0.16f, skirt + 0.20f, -0.10f), new Vector3(spread * 0.65f, 0.23f, spread * 0.72f), leaves, rng,
                    distant ? 4 : 5, distant ? 2 : 3);
                crowns[i] = crown;
            }

            const float spacing = 16f;
            const float tileSize = 256f;
            int count = (int)(2176f / spacing);
            for (int z = -count; z <= count; z++)
            for (int x = -count; x <= count; x++)
            {
                float east = spacing * (x + 1.48f * (RegionNoise.Hash01(x, z, seed) - 0.5f));
                float north = spacing * (z + 1.48f * (RegionNoise.Hash01(x, z, seed + 1) - 0.5f));
                var p = new NVector2(east + 19f * RegionNoise.Gradient(east / 83f, north / 139f, seed + 8u),
                    north + 19f * RegionNoise.Gradient(east / 121f, north / 93f, seed + 9u));
                float valley = Math.Abs(p.X + 65f * Mathf.Sin(p.Y / 680f));
                if (valley < 380f && p.Y < 1300f) continue;
                float hill = RegionNoise.Smooth(405f, 1100f, valley);
                float largePatch = RegionNoise.Fbm(p.X / 310f, p.Y / 390f, 2, seed + 2);
                float smallPatch = RegionNoise.Fbm(p.X / 95f, p.Y / 121f, 2, seed + 63u);
                float cover = RegionNoise.Smooth(-0.33f, 0.35f, largePatch + 0.48f * smallPatch + 0.34f * hill);
                float forest = hill * cover;
                float rim = 0.41f * RegionNoise.Smooth(1300f, 2020f, p.Y) * cover;
                if (RegionNoise.Hash01(x, z, seed + 3) > Mathf.Max(forest, rim) || Reserved(plan, p)) continue;
                float size = Mathf.Lerp(8.6f, 21.4f, RegionNoise.Hash01(x, z, seed + 4)) + 4.1f * forest + 4.3f * smallPatch;
                if (forest < 0.45f)
                {
                    Add(edgeTrees, height, p, smallPatch > 0.15f ? RegionPlantKind.Juniper : RegionPlantKind.RiparianTree,
                        size * 0.78f, seed, x, z);
                    continue;
                }
                var key = new Vector2Int(Mathf.FloorToInt(p.X / tileSize), Mathf.FloorToInt(p.Y / tileSize));
                if (!tiles.TryGetValue(key, out RegionWoodlandGeometry tile))
                    tiles.Add(key, tile = new RegionWoodlandGeometry());
                int form = Mathf.Min(variations - 1, (int)(RegionNoise.Hash01(x, z, seed + 5) * variations));
                if (valley > 1050f) form += variations;
                tile.Append(crowns[form], new Vector3(p.X, height(p.X, p.Y), p.Y), size,
                    360f * RegionNoise.Hash01(x, z, seed + 12));
            }

            AddGeometryTiles(parent, "Woodland crowns above the fields", shader, tiles);
        }

        private static void AddGeometryTiles(Transform parent, string groupName, Shader shader,
            Dictionary<Vector2Int, RegionWoodlandGeometry> tiles)
        {
            if (tiles.Count == 0) return;
            var material = new Material(shader) { name = groupName, hideFlags = HideFlags.DontSave };
            var group = new GameObject(groupName);
            group.transform.SetParent(parent, false);
            foreach (var batch in tiles)
            {
                string name = $"{batch.Key.x}, {batch.Key.y}";
                var tile = new GameObject(name);
                tile.transform.SetParent(group.transform, false);
                var mesh = batch.Value.ToMesh(name);
                mesh.UploadMeshData(true);
                tile.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = tile.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                // Vertex colour and crown normals carry the distant shade beyond the shadow range.
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        public static void PlantMargins(Transform parent, RegionRoadPlan plan, Func<float, float, float> height, bool refined = false)
        {
            var grasses = new List<RegionPlant>();
            var hedges = new List<RegionPlant>();
            uint seed = (uint)plan.Seed ^ 0xb3c4u;
            foreach (RegionRoadCorridor road in new[] { plan.Highway, plan.Arterial })
            {
                for (int side = -1; side <= 1; side += 2)
                for (int i = 4; i * 2.9f < road.Length - 9f; i++)
                {
                    float distance = (i + RegionNoise.Hash01(i, side, seed)) * 2.9f;
                    RegionRoadStation frame = road.At(distance);
                    float run = RegionNoise.Fbm(distance / 28f, 1.1f * side, 2, seed);
                    float open = RegionNoise.Hash01(i, side, seed + 71);
                    float mix = RegionNoise.Smooth(-0.30f, 0.21f, run);
                    for (int plant = 0; !refined && plant < 5; plant++)
                    {
                        uint offsetSeed = seed + (uint)(11 * plant);
                        float which = RegionNoise.Hash01(i, side, offsetSeed + 2);
                        if (which > 0.34f + 0.53f * mix) continue;
                        float edge = 1.74f + 5.65f * RegionNoise.Hash01(i, side, offsetSeed + 3);
                        var p = MarginPoint(road, frame, side, edge, (which - 0.5f) * 2.9f);
                        if (RegionRoadMargin.InSite(plan.Site, p, 1.5f)) continue;
                        float size = 0.18f + (0.22f + 0.58f * mix) * RegionNoise.Hash01(i, side, offsetSeed + 4);
                        Add(grasses, height, p, RegionPlantKind.Grass, size, offsetSeed, i, side);
                    }

                    // Leave open stretches and keep the ditch bed visible in front of the shrubs.
                    if (run < 0.012f || open < 0.12f) continue;
                    var bank = MarginPoint(road, frame, side, RegionRoadMargin.BankCrest + (open - 0.5f) * 3.2f, 0f);
                    if (RegionRoadMargin.InSite(plan.Site, bank, 3.5f)) continue;
                    float crown = 1.2f + 2.8f * RegionNoise.Smooth(0.01f, 0.35f, run) * open;
                    Add(hedges, height, bank, RegionPlantKind.Juniper, crown, seed + 29, i, side);
                    if (crown > 1.9f)
                    {
                        var foot = bank + frame.Right * (side * 1.1f) + frame.Forward * 1.35f;
                        if (!RegionRoadMargin.InSite(plan.Site, foot, 2.8f))
                            Add(hedges, height, foot, RegionPlantKind.Shrub, crown * 0.67f, seed + 33, i, side);
                        if (refined && run > 0.105f)
                        {
                            var skirt = bank + frame.Right * (side * 1.9f) - frame.Forward * 0.78f;
                            if (!RegionRoadMargin.InSite(plan.Site, skirt, 2.8f))
                                Add(hedges, height, skirt, RegionPlantKind.Shrub, crown * 0.51f, seed + 39, i, side);
                        }
                    }
                }
                seed += 2783u;
            }
            if (refined) PlantVergeCover(parent, plan, height);
            else RegionPlantView.AddPlants(parent, grasses, "Short and tall verge grass", new Color(0.77f, 0.88f, 0.69f), "Roadside grass");
            RegionPlantView.AddPlants(parent, hedges, "Broken hedges above the ditch", new Color(0.73f, 0.83f, 0.66f), "Roadside shrubs");
        }

        private static void PlantVergeCover(Transform parent, RegionRoadPlan plan, Func<float, float, float> height)
        {
            var tufts = new List<RegionPlant>();
            uint seed = (uint)plan.Seed ^ 0x4421u;
            const float spacing = 1.02f;
            foreach (RegionRoadCorridor road in new[] { plan.Highway, plan.Arterial })
            {
                for (int i = 8; i * spacing < road.Length - 9f; i++)
                {
                    RegionRoadStation frame = road.At((i + RegionNoise.Hash01(i, 3, seed)) * spacing);
                    for (int side = -1; side <= 1; side += 2)
                    for (int row = 0; row < 9; row++)
                    {
                        int stream = row * side + (side > 0 ? 1 : -1);
                        float jitter = RegionNoise.Hash01(i, stream, seed + 15u);
                        float edge = 1.45f + (row + jitter) * 0.72f;
                        var p = MarginPoint(road, frame, side, edge, (jitter - 0.5f) * spacing * 0.9f);
                        float growth = RegionRoadMargin.VergeGrowth(p, plan.Seed);
                        float turf = RegionNoise.Smooth(1.55f, 4.0f, edge + 1.55f * (growth - 0.5f));
                        float density = turf * Mathf.Lerp(0.31f, 0.98f, growth);
                        if (RegionNoise.Hash01(i, stream, seed + 16u) > density ||
                            RegionRoadMargin.InSite(plan.Site, p, 0.70f)) continue;
                        AddTuft(p, i, stream, growth, turf);
                    }
                }
                seed += 3613u;
            }

            RegionRoadsideSite site = plan.Site;
            float near = RegionRoadsideSite.Setback, far = near + RegionRoadsideSite.Depth;
            float half = RegionRoadsideSite.Frontage * 0.5f;
            NVector2 Point(float reach, float along) => site.Entrance + site.Outward * reach + site.Station.Forward * along;
            PlantPlotEdge(Point(near, -half), Point(far, -half), -site.Station.Forward);
            PlantPlotEdge(Point(far, -half), Point(far, half), site.Outward);
            PlantPlotEdge(Point(far, half), Point(near, half), site.Station.Forward);
            PlantPlotEdge(Point(near, half), Point(near, -half), -site.Outward);
            RegionPlantView.AddMeadowTufts(parent, tufts, height, "Grass colonies on the gravel fringe", new Color(0.95f, 1.0f, 0.84f));
            AddVergeBlades(parent, plan, tufts);

            void AddTuft(NVector2 p, int i, int stream, float growth, float turf)
            {
                float size = RegionNoise.Hash01(i, stream, seed + 17u);
                float tall = growth * turf * RegionNoise.Smooth(0.55f, 1f, size);
                float metres = Mathf.Lerp(0.16f + 0.19f * size, 0.72f, tall);
                tufts.Add(new RegionPlant
                {
                    X = p.X, Y = height(p.X, p.Y), Z = -p.Y, Height = metres,
                    Yaw = (2f * Mathf.PI) * RegionNoise.Hash01(i, stream, seed + 18u),
                    Variant = tall < 0.14f ? (size < 0.58f ? 0 : 1) : (size < 0.90f ? 2 : 3),
                    Sun = 1f, Sky = Mathf.Lerp(0.64f, 0.96f, turf),
                });
            }

            void PlantPlotEdge(NVector2 from, NVector2 to, NVector2 outward)
            {
                seed += 797u;
                int steps = Mathf.CeilToInt(NVector2.Distance(from, to) / 0.92f);
                for (int i = 0; i < steps; i++)
                for (int row = 0; row < 4; row++)
                {
                    float run = RegionNoise.Hash01(i, row, seed + 5u);
                    float edge = 0.60f + 0.94f * (row + RegionNoise.Hash01(i, row, seed + 6u));
                    var point = NVector2.Lerp(from, to, (i + run) / steps) + outward * edge;
                    if (RegionRoadMargin.InSite(site, point, 0.58f)) continue;
                    float growth = RegionRoadMargin.VergeGrowth(point, plan.Seed);
                    float turf = RegionNoise.Smooth(0.45f, 2.7f, edge + growth - 0.5f);
                    if (run < turf * Mathf.Lerp(0.36f, 0.94f, growth)) AddTuft(point, i, row, growth, turf);
                }
            }
        }

        private static void AddVergeBlades(Transform parent, RegionRoadPlan plan, List<RegionPlant> tufts)
        {
            var shader = Resources.Load<Shader>("RegionMap3D/RegionWoodland");
            if (!shader) throw new InvalidOperationException("The woodland shader is missing.");
            var forms = new RegionWoodlandGeometry[6];
            for (int i = 0; i < forms.Length; i++)
                forms[i] = RegionWoodlandGeometry.Plant(RegionWoodlandForm.Grass, 12 + i, false);

            const float frontageReach = 420f;
            const float tileSize = 64f;
            uint salt = (uint)plan.Seed ^ 0x19d73u;
            var tiles = new Dictionary<Vector2Int, RegionWoodlandGeometry>();
            foreach (RegionPlant tuft in tufts)
            {
                float north = -tuft.Z;
                // The adjacent field frontage gets leaf geometry; other reaches still use the tuft atlas.
                float fade = 1f - RegionNoise.Smooth(frontageReach - 65f, frontageReach,
                    Mathf.Abs(north - plan.Site.Entrance.Y));
                int x = Mathf.FloorToInt(tuft.X * 10f), z = Mathf.FloorToInt(north * 10f);
                float variation = RegionNoise.Hash01(x, z, salt);
                if (variation > fade * 0.69f) continue;
                var key = new Vector2Int(Mathf.FloorToInt(tuft.X / tileSize), Mathf.FloorToInt(north / tileSize));
                if (!tiles.TryGetValue(key, out RegionWoodlandGeometry tile))
                    tiles.Add(key, tile = new RegionWoodlandGeometry());
                int form = (int)(forms.Length * RegionNoise.Hash01(x, z, salt + 1u));
                float metres = Mathf.Clamp(tuft.Height * 1.14f, 0.22f, 0.64f);
                tile.Append(forms[form], new Vector3(tuft.X, tuft.Y - 0.025f, north), metres, tuft.Yaw * Mathf.Rad2Deg);
            }
            AddGeometryTiles(parent, "Grass blades by the field frontage", shader, tiles);
        }

        private static NVector2 MarginPoint(RegionRoadCorridor road, RegionRoadStation frame, int side, float edge, float advance) =>
            frame.AtOffset(side * (road.Profile.HalfWidth + edge)) + frame.Forward * advance;

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
