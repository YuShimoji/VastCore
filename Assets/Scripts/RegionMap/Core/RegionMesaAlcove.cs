using System;

namespace Vastcore.RegionMap
{
    /// <summary>One exposed bedding contact in the high resolution window. Coordinates are inner-grid metres.</summary>
    public sealed class RegionMesaAlcove
    {
        public RegionVolumeSpec Volume;
        public float InX, InZ;
        public float Floor => Volume.Cavities[0].FloorY;

        public void Point(float along, float inward, out float x, out float z)
        {
            x = Volume.FocusX - InZ * along + InX * inward;
            z = Volume.FocusZ + InX * along + InZ * inward;
        }

        public static RegionMesaAlcove Find(RegionData data)
        {
            if (data?.MesaContactHeight == null || data.Caprock == null || data.Cliff == null) return null;
            float cell = data.CellMeters;
            if (cell > 2f || cell < 0.25f) return null;
            // Try the inexpensive two-cell survey first, then include one-cell-wide faces.
            return Find(data, cell, 2) ?? Find(data, cell, 1);
        }

        private static RegionMesaAlcove Find(RegionData data, float cell, int step)
        {
            int n = data.Resolution, margin = (int)Math.Ceiling(65f / cell);
            float best = float.NegativeInfinity;
            RegionMesaAlcove site = null;
            for (int iz = margin; iz < n - margin; iz += step)
            for (int ix = margin; ix < n - margin; ix += step)
            {
                int i = ix + iz * n;
                if (data.Cliff[i] < 0.65f || data.Height[i] < 1f || data.LakeDepth[i] > 0f) continue;
                float x = ix * cell, z = iz * cell;
                float H(float dx, float dz) => RegionVolumePatch.SampleHeight(data, x + dx, z + dz);
                float gx = H(5, 0) - H(-5, 0), gz = H(0, 5) - H(0, -5);
                float rise = (float)Math.Sqrt(gx * gx + gz * gz);
                if (rise < 20f) continue;
                float ux = gx / rise, uz = gz / rise;
                float Profile(float along, float depth) => H(ux * depth - uz * along, uz * depth + ux * along);
                int capX = (int)Math.Round(ix + ux * 8f / cell);
                int capZ = (int)Math.Round(iz + uz * 8f / cell);
                if (data.Caprock[capX + capZ * n] == 0) continue;
                float lip = Profile(0, -4);
                if (Math.Abs(lip - data.MesaContactHeight[i]) > 16f) continue;
                // The lip must open onto talus and keep solid caprock above both ends.
                float spread = 0f;
                for (int along = -16; along <= 16; along += 4)
                {
                    if (Profile(along, 8) < lip + 18f || Profile(along, -9) > lip + 2f)
                    { spread = 1000f; break; }
                    spread = Math.Max(spread, Math.Abs(Profile(along, -6) - lip));
                }
                if (spread > 10f) continue;
                // A narrow spire can have a high lip but no rock beside or behind the cut.
                // Keep load-bearing shoulders beyond both rounded ends and behind the back wall.
                bool SolidCap(float along, float depth)
                {
                    int cx = (int)Math.Round(ix + (ux * depth - uz * along) / cell);
                    int cz = (int)Math.Round(iz + (uz * depth + ux * along) / cell);
                    int sample = cx + cz * n;
                    return data.Caprock[sample] != 0 && data.Height[sample] > lip + 16f;
                }
                if (!SolidCap(0f, 21f) || !SolidCap(-34f, 16f) || !SolidCap(34f, 16f)) continue;
                // Prefer a continuous face on the near side of the window, lit from the northwest.
                float east = (ix / (float)n - 0.40f), south = (iz / (float)n - 0.43f);
                float score = -150f * (float)Math.Sqrt(east * east + south * south) - spread + 3f * ux - uz;
                if (score <= best) continue;
                best = score;
                int half = (int)Math.Ceiling(48f / cell);
                var volume = new RegionVolumeSpec
                {
                    CenterX = x, CenterZ = z, SizeMeters = (half * 2 + 1) * cell,
                    VoxelMeters = cell, JoinHeightGrid = true, FocusX = x, FocusZ = z, FocusY = lip + 3f,
                    Seed = unchecked((uint)data.Spec.Seed * 2654435761u + 97u)
                };
                site = new RegionMesaAlcove { Volume = volume, InX = ux, InZ = uz };
                site.Point(-17f, 1f, out float ax, out float az);
                site.Point(16f, 2f, out float bx, out float bz);
                volume.Cavities.Add(new RegionVolumeCavity
                {
                    Ax = ax, Az = az, Ay = lip + 2.6f, Bx = bx, Bz = bz, By = lip + 3.4f,
                    RadiusA = 11f, RadiusB = 14f, Flatten = 0.39f, Bedded = true, FloorY = lip - 0.4f, Roughness = 1.5f
                });
            }
            return site;
        }
    }
}
