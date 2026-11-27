using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.RegionMap
{
    /// <summary>外洋に面した崖一箇所の節理、崩落面と堆積を、共通の座標で組み立てる。</summary>
    internal sealed class RegionCoastRockfall
    {
        private const int Subdivision = 4;
        private const float FootReach = 11f;
        private const float PrimaryJoint = 0.52f;
        private const float SecondaryJoint = -0.78f;
        private readonly RegionData terrain;
        private readonly float[] ground;
        private readonly int[] coarseIndices;
        private readonly float cell;
        private readonly int n;
        private readonly bool exposedStone;
        private int minX, minZ, fineWidth, fineRows;
        private float[] fineGround;
        private float lowerBenchHeight;

        internal Vector2 Waterline { get; private set; }
        internal Vector2 Sea { get; }
        internal Vector2 Inland => -Sea;
        internal Vector2 Along => new Vector2(Sea.y, -Sea.x);
        internal float FootDistance { get; private set; }
        internal float CliffRise { get; private set; }
        internal float StepMeters => cell / Subdivision;
        internal float HeightDelta { get; private set; }
        internal int AddedTriangles { get; private set; }
        internal float JointYaw => Yaw(Inland + PrimaryJoint * Along);

        private RegionCoastRockfall(RegionData data, float[] ground, int[] coarseIndices, float seaYaw, bool exposedStone)
        {
            terrain = data;
            this.ground = ground;
            this.coarseIndices = coarseIndices;
            this.exposedStone = exposedStone;
            n = data.Resolution;
            cell = data.CellMeters;
            float radians = seaYaw * Mathf.Deg2Rad;
            Sea = new Vector2(Mathf.Sin(radians), Mathf.Cos(radians));
        }

        internal static RegionCoastRockfall Locate(RegionData region, RegionDetail detail, float[] ground, int[] coarseIndices, float yaw, bool exposedStone)
        {
            var site = new RegionCoastRockfall(detail.Data, ground, coarseIndices, yaw, exposedStone);
            bool[] regionalSea = RegionOceanMask.ConnectedSea(region);
            float perCell = detail.SizeMap / site.n;
            bool[] localSea = RegionOceanMask.ConnectedSea(detail.Data, i =>
                regionalSea[region.CellFromMap(detail.OriginMapX + (i % site.n + 0.5f) * perCell,
                    detail.OriginMapY + (i / site.n + 0.5f) * perCell)]);
            return site.TrySelectContact(localSea) ? site : null;
        }

        private bool TrySelectContact(bool[] sea)
        {
            int center = n / 2;
            int radius = Mathf.CeilToInt(215f / cell);
            float best = float.NegativeInfinity;
            for (int z = center - radius; z <= center + radius; z++)
            {
                for (int x = center - radius; x <= center + radius; x++)
                {
                    int i = x + z * n;
                    if (ground[i] <= 0f || IsWatercourse(i)) continue;
                    int xStep = Sea.x >= 0f ? 1 : -1;
                    int zStep = Sea.y >= 0f ? 1 : -1;
                    Vector2 p = new Vector2(x, z) * cell;
                    if (Mathf.Abs(Sea.x) > 0.26f && sea[i + xStep])
                        Consider(i, i + xStep, p, new Vector2(xStep, 0f));
                    if (Mathf.Abs(Sea.y) > 0.26f && sea[i + zStep * n])
                        Consider(i, i + zStep * n, p, new Vector2(0f, zStep));
                }
            }
            if (float.IsNegativeInfinity(best)) return false;
            lowerBenchHeight = BaseHeight(Waterline + Inland * (FootDistance - 17f)) + 4.2f;
            return true;

            void Consider(int land, int water, Vector2 p, Vector2 edge)
            {
                float crossing = ground[land] / (ground[land] - ground[water]);
                Vector2 contact = p + edge * cell * crossing;
                // Check the actual near-camera approach on the fine window's ocean component.
                for (int meters = 8; meters <= 56; meters += 8)
                {
                    Vector2 offshore = contact + meters * Sea;
                    if (!sea[Nearest(offshore)] || BaseHeight(offshore) > 0f) return;
                }

                float rise = 0f, foot = 0f;
                for (float depth = 16f; depth <= 64f; depth += 4f)
                {
                    Vector2 start = contact + Inland * depth;
                    if (BaseHeight(start - Inland * FootReach) < 0.6f || IsWatercourse(Nearest(start))) continue;
                    float vertical = BaseHeight(start + 12f * Inland) - BaseHeight(start);
                    if (vertical <= rise) continue;
                    rise = vertical;
                    foot = depth;
                }
                if (rise < 3f) return;
                float score = rise - 0.044f * (contact - Vector2.one * center * cell).magnitude;
                if (score <= best) return;
                best = score;
                Waterline = contact;
                FootDistance = foot;
                CliffRise = rise;
            }
        }

        internal Mesh Refine(Mesh baseMesh)
        {
            Vector2 center = Waterline + Inland * (FootDistance + 5f);
            const float radius = 86f;
            minX = Mathf.Max(0, Mathf.FloorToInt((center.x - radius) / cell));
            minZ = Mathf.Max(0, Mathf.FloorToInt((center.y - radius) / cell));
            int maxX = Mathf.Min(n - 1, Mathf.CeilToInt((center.x + radius) / cell));
            int maxZ = Mathf.Min(n - 1, Mathf.CeilToInt((center.y + radius) / cell));
            fineWidth = (maxX - minX) * Subdivision + 1;
            fineRows = (maxZ - minZ) * Subdivision + 1;
            fineGround = new float[fineWidth * fineRows];
            var positions = new Vector3[fineGround.Length];
            var uv = new Vector2[fineGround.Length];
            var indices = new int[(fineWidth - 1) * (fineRows - 1) * 6];
            float extent = (n - 1) * cell;
            int count = 0;
            for (int z = 0; z < fineRows; z++)
            {
                for (int x = 0; x < fineWidth; x++)
                {
                    int i = x + z * fineWidth;
                    Vector2 p = new Vector2(minX + (float)x / Subdivision, minZ + (float)z / Subdivision) * cell;
                    float height = BaseHeight(p);
                    float edgeDistance = Mathf.Min(Mathf.Min(x, fineWidth - 1 - x), Mathf.Min(z, fineRows - 1 - z)) * StepMeters;
                    float changed = RefinedHeight(p, height, RegionNoise.Smooth(0f, 12f, edgeDistance));
                    HeightDelta = Mathf.Max(HeightDelta, Mathf.Abs(changed - height));
                    fineGround[i] = changed;
                    positions[i] = new Vector3(p.x, changed, -p.y);
                    uv[i] = new Vector2(p.x / extent, 1f - p.y / extent);
                    if (x == fineWidth - 1 || z == fineRows - 1) continue;
                    int c = i + fineWidth, d = c + 1;
                    indices[count++] = i;
                    indices[count++] = i + 1;
                    if (DiagonalAD(minX + x / Subdivision, minZ + z / Subdivision))
                    {
                        indices[count++] = d; indices[count++] = i; indices[count++] = d; indices[count++] = c;
                    }
                    else
                    {
                        indices[count++] = c; indices[count++] = i + 1; indices[count++] = d; indices[count++] = c;
                    }
                }
            }

            int removed = (maxX - minX) * (maxZ - minZ) * 6;
            var remaining = new int[coarseIndices.Length - removed];
            int target = 0;
            for (int z = 0; z < n - 1; z++)
            {
                int row = z * (n - 1) * 6;
                if (z >= minZ && z < maxZ)
                {
                    Copy(row, minX * 6);
                    Copy(row + maxX * 6, (n - 1 - maxX) * 6);
                }
                else Copy(row, (n - 1) * 6);
            }
            baseMesh.SetTriangles(remaining, 0, false);

            var mesh = new Mesh { name = "Jointed cliff and rockfall ledge", hideFlags = HideFlags.DontSave };
            mesh.vertices = positions;
            mesh.uv = uv;
            mesh.triangles = indices;
            mesh.RecalculateNormals();
            MatchEdgeNormals(mesh, baseMesh.normals);
            if (exposedStone) SetSurfaceChannels(mesh, positions);
            AddedTriangles = (indices.Length - removed) / 3;
            return mesh;

            void Copy(int source, int length)
            {
                Array.Copy(coarseIndices, source, remaining, target, length);
                target += length;
            }
        }

        private void SetSurfaceChannels(Mesh mesh, Vector3[] positions)
        {
            var channels = new List<Vector4>(positions.Length);
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 vertex = positions[i];
                Vector2 local = new Vector2(vertex.x, -vertex.z) - Waterline;
                float side = Vector2.Dot(local, Along);
                float run = Vector2.Dot(local, Inland) - FootDistance;
                int x = i % fineWidth, z = i / fineWidth;
                float edgeDistance = Mathf.Min(Mathf.Min(x, fineWidth - 1 - x), Mathf.Min(z, fineRows - 1 - z)) * StepMeters;
                float weight = RegionNoise.Smooth(0f, 16f, edgeDistance) * RegionNoise.Smooth(-2.8f, 0.5f, vertex.y)
                    * (1f - RegionNoise.Smooth(42f, 72f, Mathf.Abs(side))) * (1f - RegionNoise.Smooth(26f, 51f, run));
                // Bedrock shelves keep the same material as the cliff, even where their surface is almost flat.
                float exposed = RegionNoise.Smooth(3.1f, 5.8f, vertex.y) * (1f - RegionNoise.Smooth(68f, 79f, vertex.y));
                float wetHeight = 1.07f + 0.46f * RegionNoise.Fbm(side / 10f, run / 7f, 2, 371u);
                float wet = 1f - RegionNoise.Smooth(0.08f, wetHeight, vertex.y);
                channels.Add(new Vector4(exposed, wet, 1f, weight));
            }
            mesh.SetUVs(2, channels);
        }

        private void MatchEdgeNormals(Mesh mesh, Vector3[] coarseNormals)
        {
            Vector3[] normals = mesh.normals;
            for (int z = 0; z < fineRows; z++)
            {
                for (int x = 0; x < fineWidth; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, fineWidth - 1 - x), Mathf.Min(z, fineRows - 1 - z));
                    if (edge >= Subdivision) continue;
                    float gx = minX + (float)x / Subdivision, gz = minZ + (float)z / Subdivision;
                    int ix = Mathf.Min(n - 2, (int)gx), iz = Mathf.Min(n - 2, (int)gz);
                    int start = ix + iz * n, idx = x + z * fineWidth;
                    Vector3 a = Vector3.Lerp(coarseNormals[start], coarseNormals[start + 1], gx - ix);
                    Vector3 b = Vector3.Lerp(coarseNormals[start + n], coarseNormals[start + n + 1], gx - ix);
                    normals[idx] = Vector3.Lerp(Vector3.Lerp(a, b, gz - iz), normals[idx], edge / (float)Subdivision).normalized;
                }
            }
            mesh.normals = normals;
        }

        private float RefinedHeight(Vector2 p, float old, float edge)
        {
            Vector2 relative = p - Waterline;
            float depth = Vector2.Dot(relative, Inland);
            float side = Vector2.Dot(relative, Along);
            float protection = RegionNoise.Smooth(0.4f, 1.2f, old) * RegionNoise.Smooth(4f, 12f, depth) * edge;
            if (protection <= 0f || IsWatercourse(Nearest(p))) return old;
            float run = depth - FootDistance;
            float patch = (1f - RegionNoise.Smooth(31f, 58f, Mathf.Abs(side)))
                * (1f - RegionNoise.Smooth(30f, 58f, run));
            if (patch <= 0f) return old;

            float primary = side - PrimaryJoint * run;
            float secondary = side - SecondaryJoint * run;
            float scar = 1f - RegionNoise.Smooth(2.7f, 10f, Mathf.Abs(primary));
            float cross = 1f - RegionNoise.Smooth(0.7f, 4.5f, Mathf.Abs(secondary + 4f));
            float joints = Mathf.Max(1f - RegionNoise.Smooth(0.6f, 2.6f, Mathf.Abs(primary - 17f)),
                1f - RegionNoise.Smooth(0.7f, 3.5f, Mathf.Abs(secondary - 21f)));
            float face = RegionNoise.Smooth(-8f, 3f, run) * (1f - RegionNoise.Smooth(22f, 46f, run));
            float retreat = face * (7.7f * scar + 2.9f * cross + 4.4f * joints);
            float cut = Mathf.Min(old, BaseHeight(p - Inland * retreat));

            // Leave a slightly dipped rock platform cut back at the fall and bounded by the crossing joint.
            float ledgeFront = -17f + 6.4f * scar + 2.6f * cross - 0.18f * side;
            float shelf = RegionNoise.Smooth(-2.1f, 0.5f, run - ledgeFront) * (1f - RegionNoise.Smooth(-2f, 7f, run));
            float stratum = RegionNoise.Smooth(-14f, -10f, secondary) * (1f - RegionNoise.Smooth(18f, 23f, primary));
            float ledgeTop = lowerBenchHeight + 0.057f * side + 0.024f * run;
            float brokenLedge = Mathf.Lerp(cut, Mathf.Max(cut, ledgeTop), shelf * stratum * (1f - 0.65f * scar));
            float facet = RegionNoise.Smooth(-1.2f, 1.5f, primary - 8.5f) * (1f - RegionNoise.Smooth(15f, 20f, primary));
            float shaped = brokenLedge + 1.35f * face * facet;

            // Low, gently dipping bedding leaves short shelves where the joints cut through it.
            float dip = 0.10f * side - 0.07f * run;
            float layer = (shaped - dip) / 7.8f;
            float baseLayer = Mathf.Floor(layer);
            float bed = dip + 7.8f * (baseLayer + RegionNoise.Smooth(0.24f, 0.79f, layer - baseLayer));
            float lowerWall = RegionNoise.Smooth(3.5f, 9f, shaped) * (1f - RegionNoise.Smooth(19f, 32f, shaped));
            shaped = Mathf.Lerp(shaped, bed, 0.74f * lowerWall * face);
            if (exposedStone)
            {
                // Cross-joint benches terminate at the splits. Compress the slope at the plane without forming a spike.
                float leftPanel = RegionNoise.Smooth(-31f, -23f, primary) * (1f - RegionNoise.Smooth(-9f, -2f, primary));
                float rightPanel = RegionNoise.Smooth(0f, 6f, secondary) * (1f - RegionNoise.Smooth(20f, 28f, secondary));
                shaped = JointBench(shaped, 11.2f - 0.16f * side + 0.041f * run, leftPanel * face);
                shaped = JointBench(shaped, 18.2f + 0.19f * side - 0.035f * run, rightPanel * face);
            }
            return Mathf.Max(0.06f, Mathf.Lerp(old, shaped, patch * protection));
        }

        private static float JointBench(float height, float level, float widthFade)
        {
            float delta = height - level;
            float shelf = 1f - RegionNoise.Smooth(1.2f, 5.5f, Mathf.Abs(delta));
            return Mathf.Lerp(height, level + delta * 0.12f, widthFade * shelf);
        }

        internal RegionPropProofSlice Deposits(RegionPropCatalog catalog, int seed)
        {
            var slab = catalog.GetProp("nat_stratified_slab");
            var gravel = catalog.GetProp("nat_pebble_cluster");
            var result = new RegionPropProofSlice
            {
                Biome = RegionBiome.Coast, UseRealAssets = true, HasGroundColor = true,
                GroundRed = 0.51f, GroundGreen = 0.51f, GroundBlue = 0.48f,
            };
            var random = new System.Random(unchecked(seed * 2909 + 43));
            var positions = new List<Vector2>();
            Add(slab, "fractured_boulder__split_cleft", FootDistance - 5.0f, -2.3f, 1.7f, JointYaw + 4f);
            Add(slab, "fractured_boulder__wedge_monolith", FootDistance - 10.7f, 2.6f, 1.2f, JointYaw - 8f);
            Add(slab, "stratified_ledge__fractured_edge", FootDistance - 14.8f, -1.6f, 0.85f, JointYaw + 14f);
            for (int i = 0; i < 43; i++)
            {
                float run = Mathf.Lerp(5.2f, 23f, (float)random.NextDouble());
                float d = Mathf.Max(4.8f, FootDistance - run);
                float width = Mathf.Lerp(3.3f, 12f, run / 23f);
                float side = ((float)random.NextDouble() - 0.5f) * 2f * width - 0.17f * run;
                string variant = run < 13f ? "nat_pebble_cluster__dense_talus_patch" : "nat_pebble_cluster__sparse_angular_gravel";
                float scale = Mathf.Lerp(1.46f, 0.59f, run / 23f) * Mathf.Lerp(0.78f, 1.18f, (float)random.NextDouble());
                Add(gravel, variant, d, side, scale, JointYaw + Mathf.Lerp(-31f, 36f, (float)random.NextDouble()));
            }
            return result;

            void Add(RegionPropDefinition def, string variant, float depth, float side, float scale, float yaw)
            {
                Vector2 p = Waterline + depth * Inland + side * Along;
                float surface = HeightAt(p.x, p.y);
                float dx = (HeightAt(p.x + 0.65f, p.y) - HeightAt(p.x - 0.65f, p.y)) / 1.3f;
                float dz = (HeightAt(p.x, p.y + 0.65f) - HeightAt(p.x, p.y - 0.65f)) / 1.3f;
                float slope = Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;
                if (surface < 0.45f || slope > 31f || IsWatercourse(Nearest(p))) return;
                float spacing = def == slab ? 2.2f : 1.25f;
                if (positions.Exists(other => (p - other).sqrMagnitude < spacing * spacing)) return;
                var item = RegionPropPlacementSolver.ResolveSingle(def, unchecked((uint)seed), result.Instances.Count,
                    p.x, surface, p.y, slope, dx, dz, scaleOverride: scale, yawOverride: yaw, biome: RegionBiome.Coast);
                // Keep the anchor tied to the scar and fit the selected mesh, including its LOD.
                item.X = p.x;
                item.Z = p.y;
                item.Y = surface - Mathf.Max(0.07f, item.DimensionsY * item.ScaleY * (def == slab ? 0.22f : 0.16f));
                item.MeshVariant = variant;
                item.DamageState = item.ConstructionVariant = "";
                positions.Add(p);
                result.Instances.Add(item);
            }
        }

        internal RegionPropProofSlice ShoreCobbles(RegionPropCatalog catalog, int seed)
        {
            var result = new RegionPropProofSlice
            {
                Biome = RegionBiome.Coast, UseRealAssets = true, HasGroundColor = true,
                GroundRed = 0.36f, GroundGreen = 0.35f, GroundBlue = 0.34f,
            };
            var cobbles = catalog.GetProp("nat_pebble_cluster");
            var random = new System.Random(unchecked(seed * 1777 + 6823));
            var accepted = new List<Vector2>();
            for (int group = 0; group < 9; group++)
            {
                float center = -38f + 9.5f * group + 4.6f * ((float)random.NextDouble() - 0.5f);
                int placed = 0;
                for (int attempt = 0; attempt < 10 && placed < 4; attempt++)
                {
                    float side = center + Mathf.Lerp(-3.7f, 3.7f, (float)random.NextDouble());
                    float target = Mathf.Lerp(0.19f, 1.14f, (float)random.NextDouble());
                    Vector2 column = Waterline + side * Along;
                    Vector2 p = column;
                    float best = 0.28f, height = 0f;
                    for (float depth = -13f; depth <= 19f; depth += 0.65f)
                    {
                        Vector2 point = column + depth * Inland;
                        float y = HeightAt(point.x, point.y);
                        float delta = Mathf.Abs(y - target);
                        if (delta >= best || y < 0.09f || y > 1.27f || IsWatercourse(Nearest(point))) continue;
                        best = delta;
                        p = point;
                        height = y;
                    }
                    if (height <= 0f || accepted.Exists(other => (p - other).sqrMagnitude < 2.1f)) continue;
                    float sx = (HeightAt(p.x + 0.44f, p.y) - HeightAt(p.x - 0.44f, p.y)) / 0.88f;
                    float sz = (HeightAt(p.x, p.y + 0.44f) - HeightAt(p.x, p.y - 0.44f)) / 0.88f;
                    float slope = Mathf.Atan(Mathf.Sqrt(sx * sx + sz * sz)) * Mathf.Rad2Deg;
                    if (slope > 25f) continue;
                    float scale = Mathf.Lerp(1.6f, 3.2f, (float)random.NextDouble());
                    float yaw = Yaw(Along) + Mathf.Lerp(-60f, 60f, (float)random.NextDouble());
                    var cobble = RegionPropPlacementSolver.ResolveSingle(cobbles, unchecked((uint)seed), result.Instances.Count,
                        p.x, height, p.y, slope, sx, sz, scaleOverride: scale, yawOverride: yaw, biome: RegionBiome.Coast);
                    cobble.X = p.x;
                    cobble.Z = p.y;
                    Vector3 facing = (Quaternion.FromToRotation(Vector3.up, new Vector3(-sx, 1f, sz).normalized)
                        * Quaternion.Euler(0f, yaw, 0f)).eulerAngles;
                    cobble.PitchDeg = facing.x;
                    cobble.YawDeg = facing.y;
                    cobble.RollDeg = -facing.z;
                    cobble.ScaleY *= 0.76f;
                    cobble.Y = height - Mathf.Clamp(cobble.DimensionsY * cobble.ScaleY * 0.18f, 0.023f, 0.082f);
                    cobble.MeshVariant = "nat_pebble_cluster__wash_sorted_cobbles";
                    cobble.DamageState = cobble.ConstructionVariant = "";
                    cobble.TopDust = 0f;
                    result.Instances.Add(cobble);
                    accepted.Add(p);
                    placed++;
                }
            }
            return result;
        }

        internal bool InDeposit(float x, float z)
        {
            Vector2 relative = new Vector2(x, z) - Waterline;
            float depth = Vector2.Dot(relative, Inland);
            return depth > 1f && depth < FootDistance + 2f && Mathf.Abs(Vector2.Dot(relative, Along)) < 14f;
        }

        internal float HeightAt(float x, float z)
        {
            var point = new Vector2(x, z);
            if (fineGround == null) return BaseHeight(point);
            float gx = (x / cell - minX) * Subdivision, gz = (z / cell - minZ) * Subdivision;
            if (gx < 0f || gz < 0f || gx >= fineWidth - 1 || gz >= fineRows - 1) return BaseHeight(point);
            int ix = (int)gx, iz = (int)gz, i = ix + iz * fineWidth;
            return Triangle(fineGround, i, fineWidth, gx - ix, gz - iz, DiagonalAD(minX + ix / Subdivision, minZ + iz / Subdivision));
        }

        private float BaseHeight(Vector2 p)
        {
            float x = Mathf.Clamp(p.x / cell, 0f, n - 1), z = Mathf.Clamp(p.y / cell, 0f, n - 1);
            int ix = Mathf.Min(n - 2, (int)x), iz = Mathf.Min(n - 2, (int)z);
            return Triangle(ground, ix + iz * n, n, x - ix, z - iz, DiagonalAD(ix, iz));
        }

        private bool DiagonalAD(int x, int z) => coarseIndices[(x + z * (n - 1)) * 6 + 2] == x + z * n + n + 1;

        private static float Triangle(float[] heights, int i, int row, float x, float z, bool ad)
        {
            float a = heights[i], b = heights[i + 1], c = heights[i + row], d = heights[i + row + 1];
            if (ad) return x >= z ? a + (b - a) * x + (d - b) * z : a + (d - c) * x + (c - a) * z;
            return x + z <= 1f ? a + (b - a) * x + (c - a) * z : d + (c - d) * (1f - x) + (b - d) * (1f - z);
        }

        private int Nearest(Vector2 p) =>
            Mathf.Clamp(Mathf.RoundToInt(p.x / cell), 0, n - 1) + Mathf.Clamp(Mathf.RoundToInt(p.y / cell), 0, n - 1) * n;

        private bool IsWatercourse(int i) => terrain.Biome[i] == RegionBiome.Lake || terrain.Biome[i] == RegionBiome.River
            || (terrain.WaterCoverage != null && terrain.WaterCoverage[i] > 0.04f);

        private static float Yaw(Vector2 heading) => Mathf.Atan2(heading.x, -heading.y) * Mathf.Rad2Deg;
    }
}
