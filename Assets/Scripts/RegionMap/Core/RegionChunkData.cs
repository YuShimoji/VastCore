using System;
using System.Threading.Tasks;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// ランタイムストリーミング用の単一チャンクデータ（CPU 影ベイク省略・シームレス境界・エッジスカート付き）。
    /// 座標はチャンク北西角基準のローカルメートル座標（X 東、Y 上、Z 南）。UnityEngine 非依存。
    /// </summary>
    public sealed class RegionChunkData
    {
        public RegionChunkId Id;
        public RegionChunkRingSpec Spec;
        public double MinEastM;
        public double MinSouthM;
        public float SizeMeters;
        public float CellMeters;
        public int Resolution;

        public RegionData TerrainData;
        public RegionBiome PrimaryBiome;
        public RegionBiomeSurfaceProfile BiomeSurface;
        public RegionLookPart MeshPart;
        public byte[] AlbedoPixels;
        public int AlbedoSize;

        /// <summary>チャンク内の任意絶対座標における地面標高（m、海は 0）。</summary>
        public float HeightAt(double eastM, double southM)
        {
            if (TerrainData == null) return 0f;
            float x = (float)Math.Clamp(eastM - MinEastM, 0.0, SizeMeters);
            float z = (float)Math.Clamp(southM - MinSouthM, 0.0, SizeMeters);
            float gx = x / CellMeters;
            float gz = z / CellMeters;
            int i = Math.Min(Resolution - 2, (int)gx);
            int j = Math.Min(Resolution - 2, (int)gz);
            if (MeshPart != null && MeshPart.TryGridHeight(x, z, i + j * (Resolution - 1), out float height))
                return height;
            return Math.Max(0f, SampleBilinear(TerrainData.Height, Resolution, gx, gz));
        }

        /// <summary>チャンク内の任意絶対座標における地表勾配（高さ/水平距離）。</summary>
        public float SlopeAt(double eastM, double southM)
        {
            if (TerrainData == null) return 0f;
            float step = Math.Max(1f, CellMeters);
            double left = Math.Max(MinEastM, eastM - step), right = Math.Min(MinEastM + SizeMeters, eastM + step);
            double north = Math.Max(MinSouthM, southM - step), south = Math.Min(MinSouthM + SizeMeters, southM + step);
            float dx = (HeightAt(right, southM) - HeightAt(left, southM)) / (float)(right - left);
            float dz = (HeightAt(eastM, south) - HeightAt(eastM, north)) / (float)(south - north);
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        public bool Contains(double eastM, double southM)
        {
            return eastM >= MinEastM && eastM <= MinEastM + SizeMeters &&
                   southM >= MinSouthM && southM <= MinSouthM + SizeMeters;
        }

        internal static float SampleBilinear(float[] values, int n, float x, float y)
        {
            if (x < 0f) x = 0f; else if (x > n - 1) x = n - 1;
            if (y < 0f) y = 0f; else if (y > n - 1) y = n - 1;
            int x0 = (int)x;
            int y0 = (int)y;
            if (x0 > n - 2) x0 = n - 2;
            if (y0 > n - 2) y0 = n - 2;
            float fx = x - x0;
            float fy = y - y0;
            int i = x0 + y0 * n;
            float top = values[i] + (values[i + 1] - values[i]) * fx;
            float bottom = values[i + n] + (values[i + n + 1] - values[i + n]) * fx;
            return top + (bottom - top) * fy;
        }
    }

    /// <summary>
    /// ランタイム向け高速チャンクビルダー。
    /// 重い CPU BakeSun / BakeSky と谷刻み（GullyPasses）を省略し、隣接チャンクとの共有辺で
    /// 同一ワールド座標サンプルを行うことでシーム（割れ）を防ぐ。
    /// </summary>
    public static class RegionChunkBuilder
    {
        public static RegionChunkData Build(RegionData region, RegionChunkPlanner planner, RegionChunkId id,
            bool renderAlbedo = true)
        {
            if (region == null) throw new ArgumentNullException("region");
            if (planner == null) throw new ArgumentNullException("planner");

            RegionChunkRingSpec spec = planner.GetSpec(id.Ring);
            planner.ChunkBoundsMeters(id, out double minEastM, out double minSouthM, out float sizeMeters);
            int n = Math.Max(17, spec.Resolution);
            float cell = sizeMeters / (n - 1);

            Func<int, int, float> sampleOutside;
            RegionData terrainData = id.Ring == RegionChunkRing.Far
                ? BuildFarData(region, minEastM, minSouthM, sizeMeters, n, out sampleOutside)
                : BuildDetailData(region, minEastM, minSouthM, sizeMeters, n, cell, out sampleOutside);

            double centerEastM = minEastM + sizeMeters * 0.5;
            double centerSouthM = minSouthM + sizeMeters * 0.5;
            double metersPerMapUnit = region.SizeKm * 1000.0 / RegionData.MapUnits;
            float centerMapX = (float)(centerEastM / metersPerMapUnit);
            float centerMapY = (float)(centerSouthM / metersPerMapUnit);
            RegionBiome biome = region.BiomeAtMap(centerMapX, centerMapY);
            if (!RegionBiomeSurface.IsLandBiome(biome))
                biome = RegionBiomeSurface.ResolvePrimaryBiome(terrainData, sizeMeters * 0.5f, sizeMeters * 0.5f, cell);

            RegionLookPart part = BuildMeshPart(id.ToString(), terrainData, cell, spec.SkirtDepthMeters, sampleOutside);

            byte[] albedo = null;
            int albedoSize = 0;
            if (renderAlbedo)
            {
                var plain = new RegionRenderOptions { CloudCover = 0f, Exaggeration = 0f, CastShadow = 0f };
                albedo = RegionMapRenderer.Render(terrainData, plain, id.X * (n - 1), id.Z * (n - 1), sampleOutside);
                albedoSize = terrainData.Resolution;
            }

            return new RegionChunkData
            {
                Id = id,
                Spec = spec,
                MinEastM = minEastM,
                MinSouthM = minSouthM,
                SizeMeters = sizeMeters,
                CellMeters = cell,
                Resolution = n,
                TerrainData = terrainData,
                PrimaryBiome = biome,
                BiomeSurface = RegionBiomeSurface.ProfileFor(biome),
                MeshPart = part,
                AlbedoPixels = albedo,
                AlbedoSize = albedoSize,
            };
        }

        /// <summary>
        /// Ring 0 / Ring 1 用：広域 RegionData から決定論的に細部標高をサンプリングする。
        /// 頂点 (i, j) を正確に (minEastM + i * cell, minSouthM + j * cell) で評価するため、
        /// 隣接チャンクの共有辺は完全に同じ標高値になる。
        /// </summary>
        private static RegionData BuildDetailData(RegionData region, double minEastM, double minSouthM,
            float sizeMeters, int n, float cell, out Func<int, int, float> sampleOutside)
        {
            RegionSpec windowSpec = region.Spec.Clone();
            windowSpec.Resolution = n;
            windowSpec.SizeKm = sizeMeters / 1000f;
            var data = new RegionData(windowSpec);
            data.WaterCoverage = new float[n * n];
            data.MaxHeight = region.MaxHeight;
            data.RetainedLakes = region.RetainedLakes;

            int coarse = region.Resolution;
            float coarseCell = region.CellMeters;
            uint seed = unchecked((uint)region.Spec.Seed * 1597334677u + 3812015801u);

            float[] coarseHeight = region.Height;
            float[] coarseTemp = region.Temperature;
            float[] coarseMoist = region.Moisture;
            float[] coarseLake = region.LakeDepth;
            float[] coarseFlow = region.Flow;
            byte[] coarseArea = region.Area;
            float riverThreshold = RegionGenerator.RiverThreshold(region);

            float[] height = data.Height;
            float[] water = data.WaterCoverage;
            float[] temp = data.Temperature;
            float[] moist = data.Moisture;
            float[] lake = data.LakeDepth;
            float[] flow = data.Flow;
            byte[] area = data.Area;
            RegionBiome[] biome = data.Biome;

            // Light and terrain color use the same central difference on both sides of a shared edge.
            sampleOutside = (x, y) =>
            {
                double worldX = minEastM + x * (double)cell;
                double worldY = minSouthM + y * (double)cell;
                WarpDetailPoint(worldX, worldY, coarseCell, seed, out float qx, out float qy);
                int sx = ClampInt((int)Math.Floor(qx + 0.5f), 0, coarse - 1);
                int sy = ClampInt((int)Math.Floor(qy + 0.5f), 0, coarse - 1);
                float baseH = SampleCoarseBilinear(coarseHeight, coarse, qx, qy);
                return DetailHeight(baseH, coarseHeight, coarse, sx, sy, worldX, worldY, seed);
            };

            Parallel.For(0, n, y =>
            {
                double worldY = minSouthM + y * (double)cell;
                for (int x = 0; x < n; x++)
                {
                    int idx = x + y * n;
                    double worldX = minEastM + x * (double)cell;
                    WarpDetailPoint(worldX, worldY, coarseCell, seed, out float qx, out float qy);

                    float baseH = SampleCoarseBilinear(coarseHeight, coarse, qx, qy);
                    float nearT = SampleCoarseBilinear(coarseTemp, coarse, qx, qy);
                    float nearM = SampleCoarseBilinear(coarseMoist, coarse, qx, qy);
                    float nearLake = SampleCoarseBilinear(coarseLake, coarse, qx, qy);
                    float nearFlow = SampleCoarseBilinear(coarseFlow, coarse, qx, qy);

                    int sx = ClampInt((int)Math.Floor(qx + 0.5f), 0, coarse - 1);
                    int sy = ClampInt((int)Math.Floor(qy + 0.5f), 0, coarse - 1);
                    int src = sx + sy * coarse;
                    area[idx] = coarseArea[src];

                    if (nearLake > 0.05f && baseH > 0f)
                    {
                        lake[idx] = nearLake;
                        water[idx] = 1f;
                    }
                    else if (nearFlow > riverThreshold && baseH > 0f)
                    {
                        water[idx] = Math.Min(1f, nearFlow / (riverThreshold * 3f));
                    }

                    height[idx] = DetailHeight(baseH, coarseHeight, coarse, sx, sy, worldX, worldY, seed);
                    temp[idx] = nearT;
                    moist[idx] = nearM;
                    flow[idx] = nearFlow;
                }
            });

            int landCount = 0;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = x + y * n;
                    if (height[i] <= 0f) { biome[i] = RegionBiome.Ocean; continue; }
                    landCount++;
                    if (lake[i] > 0f) { biome[i] = RegionBiome.Lake; continue; }
                    if (water[i] > 0.5f) { biome[i] = RegionBiome.River; continue; }
                    float slope = RegionGenerator.SlopeAt(height, n, x, y, cell);
                    biome[i] = RegionGenerator.LandBiome(temp[i], moist[i], slope, height[i]);
                }
            }
            data.LandFraction = landCount / (float)(n * n);
            return data;
        }

        private static void WarpDetailPoint(double worldX, double worldY, float coarseCell, uint seed,
            out float qx, out float qy)
        {
            float ax = (float)(worldX / 1100.0);
            float ay = (float)(worldY / 1100.0);
            qx = (float)(worldX / coarseCell - 0.5) +
                0.35f * RegionNoise.Fbm(ax + 31.7f, ay + 12.9f, 3, seed + 201u);
            qy = (float)(worldY / coarseCell - 0.5) +
                0.35f * RegionNoise.Fbm(ax + 7.3f, ay + 44.2f, 3, seed + 202u);
        }

        private static float DetailHeight(float baseH, float[] coarseHeight, int coarse, int sx, int sy,
            double worldX, double worldY, uint seed)
        {
            if (baseH <= 0f) return baseH;
            float localRelief = CoarseRelief(coarseHeight, coarse, sx, sy);
            float broad = RegionNoise.Fbm((float)(worldX / 420.0), (float)(worldY / 420.0), 4, seed + 211u);
            float ridged = RegionNoise.Ridged((float)(worldX / 260.0), (float)(worldY / 260.0), 3, seed + 212u);
            float fine = RegionNoise.Fbm((float)(worldX / 46.0), (float)(worldY / 46.0), 2, seed + 213u);
            float coastRamp = RegionNoise.Smooth(0f, 18f, baseH);
            float amp = (0.8f + 0.24f * localRelief) * coastRamp;
            float height = baseH + amp * (0.65f * broad + 0.55f * (ridged - 0.45f)) +
                (0.4f + 0.04f * localRelief) * coastRamp * fine;
            return Math.Max(0.1f, height);
        }

        /// <summary>Ring 2（遠景スカート）用：広域 RegionData を高速バイリニアサンプリングする。</summary>
        private static RegionData BuildFarData(RegionData region, double minEastM, double minSouthM,
            float sizeMeters, int n, out Func<int, int, float> sampleOutside)
        {
            RegionSpec windowSpec = region.Spec.Clone();
            windowSpec.Resolution = n;
            windowSpec.SizeKm = sizeMeters / 1000f;
            var data = new RegionData(windowSpec);
            data.MaxHeight = region.MaxHeight;

            int coarse = region.Resolution;
            float coarseCell = region.CellMeters;
            float cell = sizeMeters / (n - 1);
            sampleOutside = (x, y) => SampleCoarseBilinear(region.Height, coarse,
                (float)((minEastM + x * (double)cell) / coarseCell - 0.5),
                (float)((minSouthM + y * (double)cell) / coarseCell - 0.5));

            Parallel.For(0, n, y =>
            {
                float gy = (float)((minSouthM + y * (double)cell) / coarseCell - 0.5);
                for (int x = 0; x < n; x++)
                {
                    int idx = x + y * n;
                    float gx = (float)((minEastM + x * (double)cell) / coarseCell - 0.5);
                    float h = SampleCoarseBilinear(region.Height, coarse, gx, gy);
                    data.Height[idx] = h;
                    data.Temperature[idx] = SampleCoarseBilinear(region.Temperature, coarse, gx, gy);
                    data.Moisture[idx] = SampleCoarseBilinear(region.Moisture, coarse, gx, gy);
                    data.LakeDepth[idx] = SampleCoarseBilinear(region.LakeDepth, coarse, gx, gy);
                    int sx = ClampInt((int)Math.Floor(gx + 0.5f), 0, coarse - 1);
                    int sy = ClampInt((int)Math.Floor(gy + 0.5f), 0, coarse - 1);
                    int src = sx + sy * coarse;
                    data.Biome[idx] = region.Biome[src];
                    data.Area[idx] = region.Area[src];
                    data.Flow[idx] = region.Flow[src];
                }
            });
            return data;
        }

        /// <summary>
        /// 高速メッシュ構築（CPU BakeSun/BakeSky を省略し、法線ベースの簡易空開け度と外周スカートを付与）。
        /// </summary>
        private static RegionLookPart BuildMeshPart(string name, RegionData data, float cell, float skirtDepth,
            Func<int, int, float> sampleOutside)
        {
            int n = data.Resolution;
            float[] height = data.Height;
            int gridVerts = n * n;
            bool hasSkirt = skirtDepth > 0f;
            int skirtVerts = hasSkirt ? 4 * n : 0;
            int totalVerts = gridVerts + skirtVerts;

            var positions = new float[totalVerts * 3];
            var normals = new float[totalVerts * 3];
            var uv = new float[totalVerts * 2];
            var sun = new float[totalVerts];
            var sky = new float[totalVerts];
            var rock = new float[totalVerts];
            var tint = new float[totalVerts];

            float invNMinus1 = 1f / (n - 1);

            Parallel.For(0, n, j =>
            {
                for (int i = 0; i < n; i++)
                {
                    int v = i + j * n;
                    float h = height[v] > 0f ? height[v] : 0f;
                    positions[v * 3] = i * cell;
                    positions[v * 3 + 1] = h;
                    positions[v * 3 + 2] = j * cell;

                    float hL = Math.Max(0f, i > 0 ? height[v - 1] : sampleOutside(-1, j));
                    float hR = Math.Max(0f, i < n - 1 ? height[v + 1] : sampleOutside(n, j));
                    float hU = Math.Max(0f, j > 0 ? height[v - n] : sampleOutside(i, -1));
                    float hD = Math.Max(0f, j < n - 1 ? height[v + n] : sampleOutside(i, n));
                    float slopeX = (hR - hL) / (2f * cell);
                    float slopeZ = (hD - hU) / (2f * cell);
                    float invLen = 1f / (float)Math.Sqrt(slopeX * slopeX + 1f + slopeZ * slopeZ);
                    float nx = -slopeX * invLen;
                    float ny = invLen;
                    float nz = -slopeZ * invLen;

                    normals[v * 3] = nx;
                    normals[v * 3 + 1] = ny;
                    normals[v * 3 + 2] = nz;
                    uv[v * 2] = i * invNMinus1;
                    uv[v * 2 + 1] = j * invNMinus1;

                    // CPU レイマーチング影を省略し、GPU 光源/影に委ねる（急斜面ほど岩肌・やや暗め）
                    sun[v] = 1f;
                    sky[v] = 0.72f + 0.28f * ny;
                    float slopeMag = (float)Math.Sqrt(slopeX * slopeX + slopeZ * slopeZ);
                    rock[v] = RegionNoise.Smooth(0.45f, 1.05f, slopeMag);
                    tint[v] = 0.5f;
                }
            });

            int gridQuads = (n - 1) * (n - 1);
            int skirtQuads = hasSkirt ? 4 * (n - 1) : 0;
            var indices = new int[(gridQuads + skirtQuads) * 6];
            int t = 0;

            for (int j = 0; j < n - 1; j++)
            {
                for (int i = 0; i < n - 1; i++)
                {
                    int a = i + j * n;
                    int b = a + 1;
                    int c = a + n;
                    int d = c + 1;
                    if (Math.Abs(positions[a * 3 + 1] - positions[d * 3 + 1]) <= Math.Abs(positions[b * 3 + 1] - positions[c * 3 + 1]))
                    {
                        indices[t++] = a; indices[t++] = b; indices[t++] = d;
                        indices[t++] = a; indices[t++] = d; indices[t++] = c;
                    }
                    else
                    {
                        indices[t++] = a; indices[t++] = b; indices[t++] = c;
                        indices[t++] = b; indices[t++] = d; indices[t++] = c;
                    }
                }
            }

            if (hasSkirt)
            {
                int sv = gridVerts;
                // 4辺: North (j=0), South (j=n-1), West (i=0), East (i=n-1)
                AddSkirtEdge(positions, normals, uv, sun, sky, rock, tint, indices, ref sv, ref t,
                    n, skirtDepth, k => k, false);
                AddSkirtEdge(positions, normals, uv, sun, sky, rock, tint, indices, ref sv, ref t,
                    n, skirtDepth, k => k + (n - 1) * n, true);
                AddSkirtEdge(positions, normals, uv, sun, sky, rock, tint, indices, ref sv, ref t,
                    n, skirtDepth, k => k * n, true);
                AddSkirtEdge(positions, normals, uv, sun, sky, rock, tint, indices, ref sv, ref t,
                    n, skirtDepth, k => (n - 1) + k * n, false);
            }

            return new RegionLookPart
            {
                Name = name,
                Data = data,
                Positions = positions,
                Normals = normals,
                Uv = uv,
                Indices = indices,
                Sun = sun,
                Sky = sky,
                Rock = rock,
                Tint = tint,
            };
        }

        private static void AddSkirtEdge(float[] positions, float[] normals, float[] uv,
            float[] sun, float[] sky, float[] rock, float[] tint, int[] indices,
            ref int sv, ref int t, int n, float skirtDepth, Func<int, int> borderIndex, bool flipWinding)
        {
            int startSv = sv;
            for (int k = 0; k < n; k++)
            {
                int b = borderIndex(k);
                positions[sv * 3] = positions[b * 3];
                positions[sv * 3 + 1] = positions[b * 3 + 1] - skirtDepth;
                positions[sv * 3 + 2] = positions[b * 3 + 2];
                normals[sv * 3] = normals[b * 3];
                normals[sv * 3 + 1] = normals[b * 3 + 1];
                normals[sv * 3 + 2] = normals[b * 3 + 2];
                uv[sv * 2] = uv[b * 2];
                uv[sv * 2 + 1] = uv[b * 2 + 1];
                sun[sv] = sun[b];
                sky[sv] = sky[b];
                rock[sv] = rock[b];
                tint[sv] = tint[b];
                sv++;
            }

            for (int k = 0; k < n - 1; k++)
            {
                int b0 = borderIndex(k);
                int b1 = borderIndex(k + 1);
                int s0 = startSv + k;
                int s1 = startSv + k + 1;
                if (!flipWinding)
                {
                    indices[t++] = b0; indices[t++] = s0; indices[t++] = s1;
                    indices[t++] = b0; indices[t++] = s1; indices[t++] = b1;
                }
                else
                {
                    indices[t++] = b0; indices[t++] = s1; indices[t++] = s0;
                    indices[t++] = b0; indices[t++] = b1; indices[t++] = s1;
                }
            }
        }

        private static float SampleCoarseBilinear(float[] values, int n, float x, float y)
        {
            if (x < 0f) x = 0f; else if (x > n - 1) x = n - 1;
            if (y < 0f) y = 0f; else if (y > n - 1) y = n - 1;
            int x0 = (int)x;
            int y0 = (int)y;
            if (x0 > n - 2) x0 = n - 2;
            if (y0 > n - 2) y0 = n - 2;
            float fx = x - x0;
            float fy = y - y0;
            int i = x0 + y0 * n;
            float top = values[i] + (values[i + 1] - values[i]) * fx;
            float bottom = values[i + n] + (values[i + n + 1] - values[i + n]) * fx;
            return top + (bottom - top) * fy;
        }

        private static float CoarseRelief(float[] h, int n, int x, int y)
        {
            float here = h[x + y * n];
            if (here <= 0f) return 0f;
            float most = 0f;
            if (x > 0) most = Math.Max(most, Math.Abs(here - Math.Max(0f, h[x - 1 + y * n])));
            if (x < n - 1) most = Math.Max(most, Math.Abs(here - Math.Max(0f, h[x + 1 + y * n])));
            if (y > 0) most = Math.Max(most, Math.Abs(here - Math.Max(0f, h[x + (y - 1) * n])));
            if (y < n - 1) most = Math.Max(most, Math.Abs(here - Math.Max(0f, h[x + (y + 1) * n])));
            return most;
        }

        private static int ClampInt(int v, int min, int max)
        {
            return v < min ? min : (v > max ? max : v);
        }
    }
}
