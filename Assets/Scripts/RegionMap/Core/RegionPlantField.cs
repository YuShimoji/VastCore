using System;
using System.Collections.Generic;

namespace Vastcore.RegionMap
{
    public enum RegionPlantKind { Grass, Shrub, Juniper, RiparianTree }

    public struct RegionPlant
    {
        public RegionPlantKind Kind;
        public float X, Y, Z, Height, Yaw, Sun, Sky;
        public int Variant;
    }

    /// <summary>砂岩の窓に生える株。座標は岩と同じ東・上・南の m。配置はカメラから独立。</summary>
    public static class RegionPlantField
    {
        public static List<RegionPlant> Build(RegionLookScene scene)
        {
            var plants = new List<RegionPlant>();
            if (!scene.Spec.Vegetation || scene.Spec.Mesa <= 0f) return plants;
            var outer = new Ground(scene.OuterTerrain, scene.OuterRocks, scene.OuterCell);
            var inner = new Ground(scene.InnerTerrain, scene.InnerRocks, scene.InnerCell);
            RegionPosition origin = outer.Data.DevelopmentOrigin;
            float extent = outer.Data.SizeKm * 1000f;
            uint seed = unchecked((uint)outer.Data.Spec.Seed * 2654435761u);
            for (int tier = 0; tier < 3; tier++)
            {
                float spacing = tier == 0 ? 4.7f : tier == 1 ? 11f : 27f;
                uint salt = seed + (uint)(709 + tier * 239);
                int firstX = (int)Math.Floor(origin.X / spacing), lastX = (int)Math.Floor((origin.X + extent) / spacing);
                int firstZ = (int)Math.Floor(origin.Y / spacing), lastZ = (int)Math.Floor((origin.Y + extent) / spacing);
                for (int iz = firstZ; iz <= lastZ; iz++)
                for (int ix = firstX; ix <= lastX; ix++)
                {
                    float pick = RegionNoise.Hash01(ix, iz, salt);
                    if (pick > 0.85f) continue;
                    float wx = (ix + RegionNoise.Hash01(ix, iz, salt + 1)) * spacing;
                    float wz = (iz + RegionNoise.Hash01(ix, iz, salt + 2)) * spacing;
                    float x = wx - origin.X, z = wz - origin.Y;
                    bool inInner = x >= scene.InnerMinX && x <= scene.InnerMaxX && z >= scene.InnerMinZ && z <= scene.InnerMaxZ;
                    Ground ground = inInner ? inner : outer;
                    if (!ground.Supports(x, z, out int i, out float y, out float slope)) continue;
                    RegionData data = ground.Data;
                    float rubble = data.Talus[i];
                    float shelter = 1f - ground.Terrain.Sky[i];
                    float bank = RegionNoise.Clamp01(data.Flow[i] / ground.RiverThreshold);
                    float patch = RegionNoise.Smooth(-0.32f, 0.34f, RegionNoise.Fbm(wx / 79f, wz / 79f, 2, seed + 453u));
                    float size = RegionNoise.Hash01(ix, iz, salt + 3);
                    RegionPlantKind kind;
                    float density, height;
                    if (tier == 0)
                    {
                        kind = RegionPlantKind.Grass;
                        density = (0.045f + 0.48f * patch * patch + 0.3f * bank) * (1f - 0.77f * rubble);
                        height = 0.22f + 0.49f * size;
                    }
                    else if (tier == 1)
                    {
                        kind = RegionPlantKind.Shrub;
                        density = (0.06f + 0.43f * patch + 0.30f * bank) * (1f - 0.66f * rubble);
                        height = 0.50f + 1.05f * size;
                    }
                    else
                    {
                        bool alongWash = bank > 0.18f && data.Caprock[i] == 0 && slope < 0.38f;
                        kind = alongWash ? RegionPlantKind.RiparianTree : RegionPlantKind.Juniper;
                        float woodland = data.Caprock[i] != 0 ? 0.26f : 0.015f;
                        density = alongWash ? 0.28f + 0.55f * bank :
                            (woodland + 0.55f * shelter + 0.15f * rubble) * (0.42f + patch);
                        height = alongWash ? 3.8f + 5.2f * size : 2.0f + 3.3f * size;
                    }
                    if (pick >= density * (1f - RegionNoise.Smooth(0.59f, 0.94f, slope))) continue;
                    plants.Add(new RegionPlant
                    {
                        Kind = kind, Height = height, X = x, Y = y, Z = z,
                        Yaw = 6.2831853f * RegionNoise.Hash01(ix, iz, salt + 4),
                        Variant = (int)(4f * RegionNoise.Hash01(ix, iz, salt + 5)),
                        Sun = ground.Terrain.Sun[i], Sky = ground.Terrain.Sky[i]
                    });
                }
            }
            return plants;
        }

        private sealed class Ground
        {
            public readonly RegionLookPart Terrain;
            public RegionData Data => Terrain.Data;
            public readonly float RiverThreshold;
            private readonly float _cell, _minX, _minZ;
            private readonly bool[] _occupied;

            public Ground(RegionLookPart terrain, RegionRockField rocks, float cell)
            {
                Terrain = terrain;
                _cell = cell;
                _minX = terrain.Positions[0];
                _minZ = terrain.Positions[2];
                _occupied = new bool[Data.Height.Length];
                RiverThreshold = RegionGenerator.RiverThreshold(Data);
                if (rocks != null)
                    foreach (RegionRock rock in rocks.Rocks)
                        if (rock.Size >= 1f) Reserve(rock.X, rock.Z, rock.X, rock.Z, 0.50f * rock.Size);
                RegionDevelopment plan = Data.Development;
                if (plan == null) return;
                ReserveRoad(plan.Highway, 19f);
                ReserveRoad(plan.AirportAccess, 10f);
                foreach (var street in plan.Streets) ReserveWorld(street.Start, street.End, street.Width * 0.5f);
            }

            private void ReserveRoad(IReadOnlyList<RegionPosition> points, float radius)
            {
                for (int i = 1; i < points.Count; i++) ReserveWorld(points[i - 1], points[i], radius);
            }

            private void ReserveWorld(RegionPosition a, RegionPosition b, float radius)
            {
                // DevelopmentOrigin is the north-west corner, half a cell outside the first vertex.
                float ox = _minX - 0.5f * _cell - Data.DevelopmentOrigin.X;
                float oz = _minZ - 0.5f * _cell - Data.DevelopmentOrigin.Y;
                Reserve(a.X + ox, a.Y + oz, b.X + ox, b.Y + oz, radius);
            }

            private void Reserve(float ax, float az, float bx, float bz, float radius)
            {
                int n = Data.Resolution;
                radius += 0.71f * _cell;
                int x0 = Math.Max(0, (int)Math.Ceiling((Math.Min(ax, bx) - radius - _minX) / _cell));
                int z0 = Math.Max(0, (int)Math.Ceiling((Math.Min(az, bz) - radius - _minZ) / _cell));
                int x1 = Math.Min(n - 1, (int)Math.Floor((Math.Max(ax, bx) + radius - _minX) / _cell));
                int z1 = Math.Min(n - 1, (int)Math.Floor((Math.Max(az, bz) + radius - _minZ) / _cell));
                float dx = bx - ax, dz = bz - az;
                float length2 = Math.Max(0.001f, dx * dx + dz * dz);
                for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                {
                    float ex = _minX + x * _cell - ax, ez = _minZ + z * _cell - az;
                    float t = RegionNoise.Clamp01((ex * dx + ez * dz) / length2);
                    ex -= t * dx;
                    ez -= t * dz;
                    if (ex * ex + ez * ez <= radius * radius) _occupied[x + z * n] = true;
                }
            }

            public bool Supports(float x, float z, out int i, out float height, out float slope)
            {
                i = 0;
                height = slope = 0f;
                int n = Data.Resolution;
                float gx = (x - _minX) / _cell, gz = (z - _minZ) / _cell;
                if (gx < 0f || gz < 0f || gx >= n - 1 || gz >= n - 1 || Data.Talus == null) return false;
                int ix = (int)gx, iz = (int)gz;
                i = (int)(gx + 0.5f) + (int)(gz + 0.5f) * n;
                int a = ix + iz * n, b = a + 1, c = a + n, d = c + 1;
                if (_occupied[i] || Data.StructureCoverage?[i] > 0f ||
                    Flooded(a) || Flooded(b) || Flooded(c) || Flooded(d)) return false;
                // Use the same diagonal as TerrainPart. Bilinear height could float above an uneven triangle.
                float[] h = Data.Height;
                float u = gx - ix, v = gz - iz, du, dv;
                if (Math.Abs(h[a] - h[d]) <= Math.Abs(h[b] - h[c]))
                {
                    du = u > v ? h[b] - h[a] : h[d] - h[c];
                    dv = u > v ? h[d] - h[b] : h[c] - h[a];
                    height = h[a] + du * u + dv * v;
                }
                else if (u + v <= 1f)
                {
                    du = h[b] - h[a]; dv = h[c] - h[a];
                    height = h[a] + du * u + dv * v;
                }
                else
                {
                    du = h[d] - h[c]; dv = h[d] - h[b];
                    height = h[d] - du * (1f - u) - dv * (1f - v);
                }
                slope = (float)Math.Sqrt(du * du + dv * dv) / _cell;
                return slope < 0.94f;
            }

            private bool Flooded(int i)
            {
                return Data.Height[i] <= 0f || Data.LakeDepth[i] > 0f || Data.WaterCoverage?[i] > 0f;
            }
        }
    }
}
