using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 岩をくり抜く空洞 1 つ。太さのある線分（両端で太さを変えられる）で、座標は m。
    /// X は東、Y は上、Z は南（RegionTerrainMesh と同じ。地形の北西の角が原点）。
    /// </summary>
    [Serializable]
    public sealed class RegionVolumeCavity
    {
        public float Ax, Ay, Az;
        public float Bx, By, Bz;
        public float RadiusA = 3f;
        public float RadiusB = 3f;

        /// <summary>縦の半径の倍率。1 より小さいと、上下につぶれた形になる。</summary>
        public float Flatten = 1f;

        /// <summary>この高さより下は掘らない（床を平らにする）。</summary>
        public float FloorY = float.NegativeInfinity;

        /// <summary>壁の凹凸の大きさ（m）。</summary>
        public float Roughness = 0f;

        /// <summary>空洞の壁までの距離（m）。空洞の中は負。</summary>
        internal float Distance(float x, float y, float z, uint seed)
        {
            float dx = Bx - Ax, dy = By - Ay, dz = Bz - Az;
            float length = dx * dx + dy * dy + dz * dz;
            float t = length > 1e-6f ? ((x - Ax) * dx + (y - Ay) * dy + (z - Az) * dz) / length : 0f;
            if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
            float ox = x - (Ax + dx * t);
            float oy = (y - (Ay + dy * t)) / (Flatten > 0.05f ? Flatten : 0.05f);
            float oz = z - (Az + dz * t);
            float distance = (float)Math.Sqrt(ox * ox + oy * oy + oz * oz) - (RadiusA + (RadiusB - RadiusA) * t);
            if (Roughness > 0f && distance > -4f && distance < 4f)
            {
                // 3 つの面で引いた 2 次元のノイズを足して、3 次元の凹凸の代わりにする
                float noise = RegionNoise.Fbm(x / 6f, y / 6f, 2, seed + 301u)
                    + RegionNoise.Fbm(y / 6f + 17.3f, z / 6f + 5.1f, 2, seed + 302u)
                    + RegionNoise.Fbm(z / 6f + 9.7f, x / 6f + 31.9f, 2, seed + 303u);
                distance += Roughness * noise * 0.67f;
            }
            float floor = FloorY - y;
            return floor > distance ? floor : distance;
        }
    }

    /// <summary>地形の一部を体積で作り直す範囲と、そこに入れる空洞。</summary>
    [Serializable]
    public sealed class RegionVolumeSpec
    {
        /// <summary>箱の中心（m）。箱が地形からはみ出す時は内側へ寄せる。</summary>
        public float CenterX;
        public float CenterZ;

        /// <summary>箱の水平の一辺（m）。高さは、中の地形と空洞が収まるように決まる。</summary>
        public float SizeMeters = 124f;

        /// <summary>密度を測る間隔（m）。</summary>
        public float VoxelMeters = 1f;

        public uint Seed;

        public readonly List<RegionVolumeCavity> Cavities = new List<RegionVolumeCavity>();
        // Optional local formation, sampled by meshing, collision and light rays alike.
        [NonSerialized] internal Func<float, float, float, float, float> LocalFormation;

        /// <summary>見どころの位置（m）と、そこを正面から見る時にカメラを置く方位（度。0 で南から北を見る）。置き方の規則が入れる。</summary>
        public float FocusX, FocusY, FocusZ;
        public float FacingYawDegrees;

        /// <summary>置き方の規則が見つけた崖の説明。</summary>
        public string Note;
    }

    /// <summary>
    /// 地形の一部を体積（密度の場）で作り直したメッシュ（3D 化の試作、段階 D）。高さ場では作れない、張り出し・崖下のえぐれ・洞窟を出す。
    /// 密度は「地面の高さ − Y」（岩の中が正）で、空洞の中だけ負にする。空洞から離れた所の面は高さ場と同じ位置に出るので、
    /// 箱の縁は周りの高さ場のメッシュと同じ形になる（頂点は一致しない。重ねて描く）。
    /// 面の取り出しは Surface Nets（符号の変わる升目に頂点を 1 つ置き、符号の変わる辺ごとに四角を張る）。
    /// UnityEngine に依存しない配列だけを持つ。
    /// </summary>
    public sealed class RegionVolumePatch
    {
        /// <summary>太陽の高さ（度）。光は北西から。RegionRenderOptions.SunAltitude の既定と同じ。</summary>
        public const float SunAltitudeDegrees = 38f;

        public RegionVolumeSpec Spec;
        public float MinX, MinY, MinZ;
        public float MaxX, MaxY, MaxZ;
        public int SamplesX, SamplesY, SamplesZ;

        /// <summary>頂点ごとの X・Y・Z（m）と、面の外向きの向き。長さは頂点数 × 3。</summary>
        public float[] Positions;
        public float[] Normals;

        /// <summary>三角形ごとの頂点番号 3 つ。外から見て反時計回り。</summary>
        public int[] Indices;

        /// <summary>頂点ごとに、太陽が当たれば 1、近くの岩（16 升目以内。張り出しや洞窟の壁）の影なら 0。遠くの山の影は数えない。</summary>
        public float[] Sun;

        /// <summary>頂点ごとに、真上が空なら 1、岩の下（張り出しの下・洞窟の中）なら 0。</summary>
        public float[] Sky;

        /// <summary>下を向いた面（向きの Y が負）の頂点の割合。高さ場だけなら 0。</summary>
        public float DownFacingShare;

        private RegionData terrain;

        public int VertexCount
        {
            get { return Positions.Length / 3; }
        }

        public int TriangleCount
        {
            get { return Indices.Length / 3; }
        }

        public static RegionVolumePatch Build(RegionData terrain, RegionVolumeSpec spec)
        {
            if (terrain == null) throw new ArgumentNullException("terrain");
            if (spec == null) throw new ArgumentNullException("spec");
            var patch = new RegionVolumePatch
            {
                Spec = spec,
                terrain = terrain,
            };
            float v = Math.Max(0.25f, spec.VoxelMeters);
            float extent = (terrain.Resolution - 1) * terrain.CellMeters;
            float size = Math.Min(spec.SizeMeters, extent);
            int nx = Math.Max(4, (int)Math.Round(size / v) + 1);
            size = (nx - 1) * v;
            patch.MinX = Clamp(spec.CenterX - size * 0.5f, 0f, extent - size);
            patch.MinZ = Clamp(spec.CenterZ - size * 0.5f, 0f, extent - size);
            patch.MaxX = patch.MinX + size;
            patch.MaxZ = patch.MinZ + size;
            int nz = nx;

            // 高さの範囲は、中の地形と空洞が収まるように決める
            float low = float.MaxValue;
            float high = float.MinValue;
            for (int k = 0; k < nz; k++)
            {
                for (int i = 0; i < nx; i++)
                {
                    float h = patch.HeightAt(patch.MinX + i * v, patch.MinZ + k * v);
                    if (h < low) low = h;
                    if (h > high) high = h;
                }
            }
            foreach (RegionVolumeCavity cavity in spec.Cavities)
            {
                float reach = Math.Max(cavity.RadiusA, cavity.RadiusB) * Math.Max(1f, cavity.Flatten) + cavity.Roughness;
                float bottom = Math.Min(cavity.Ay, cavity.By) - reach;
                if (!float.IsNegativeInfinity(cavity.FloorY) && cavity.FloorY > bottom) bottom = cavity.FloorY;
                if (bottom < low) low = bottom;
            }
            patch.MinY = (float)Math.Floor((low - 3f * v) / v) * v;
            int ny = Math.Max(4, (int)Math.Ceiling((high + 3f * v - patch.MinY) / v) + 1);
            patch.MaxY = patch.MinY + (ny - 1) * v;
            patch.SamplesX = nx;
            patch.SamplesY = ny;
            patch.SamplesZ = nz;

            var density = new float[nx * ny * nz];
            Parallel.For(0, nz, k =>
            {
                float z = patch.MinZ + k * v;
                for (int i = 0; i < nx; i++)
                {
                    float x = patch.MinX + i * v;
                    float ground = patch.HeightAt(x, z);
                    for (int j = 0; j < ny; j++)
                    {
                        float y = patch.MinY + j * v;
                        density[i + nx * (j + ny * k)] = patch.Carve(ground - y, x, y, z);
                    }
                }
            });

            patch.Extract(density, v);
            patch.Light(v);
            return patch;
        }

        /// <summary>地面の高さ（m）。高さ場をなめらかに読む。海は 0。</summary>
        public float HeightAt(float x, float z)
        {
            return SampleHeight(terrain, x, z);
        }

        internal static float SampleHeight(RegionData terrain, float x, float z)
        {
            int n = terrain.Resolution;
            float cell = terrain.CellMeters;
            float fx = Clamp(x / cell, 0f, n - 1.001f);
            float fz = Clamp(z / cell, 0f, n - 1.001f);
            int x0 = (int)fx;
            int z0 = (int)fz;
            float tx = fx - x0;
            float tz = fz - z0;
            float[] h = terrain.Height;
            int i = x0 + z0 * n;
            float a = h[i], b = h[i + 1], c = h[i + n], d = h[i + n + 1];
            if (a < 0f) a = 0f;
            if (b < 0f) b = 0f;
            if (c < 0f) c = 0f;
            if (d < 0f) d = 0f;
            return (a + (b - a) * tx) * (1f - tz) + (c + (d - c) * tx) * tz;
        }

        /// <summary>密度。岩の中が正、空気が負。箱の外でも読める。</summary>
        public float Density(float x, float y, float z)
        {
            return Carve(HeightAt(x, z) - y, x, y, z);
        }

        private float Carve(float density, float x, float y, float z)
        {
            List<RegionVolumeCavity> cavities = Spec.Cavities;
            for (int c = 0; c < cavities.Count; c++)
            {
                float wall = cavities[c].Distance(x, y, z, Spec.Seed);
                if (wall < density) density = wall;
            }
            return Spec.LocalFormation == null ? density : Spec.LocalFormation(x, y, z, density);
        }

        private void Extract(float[] density, float v)
        {
            int nx = SamplesX, ny = SamplesY, nz = SamplesZ;
            int cx = nx - 1, cy = ny - 1, cz = nz - 1;
            var cellVertex = new int[cx * cy * cz];
            var positions = new List<float>(cx * cz * 6);
            var normals = new List<float>(cx * cz * 6);
            var corner = new float[8];

            for (int k = 0; k < cz; k++)
            {
                for (int j = 0; j < cy; j++)
                {
                    for (int i = 0; i < cx; i++)
                    {
                        int cellIndex = i + cx * (j + cy * k);
                        cellVertex[cellIndex] = -1;
                        int inside = 0;
                        for (int c = 0; c < 8; c++)
                        {
                            float d = density[(i + (c & 1)) + nx * ((j + ((c >> 1) & 1)) + ny * (k + ((c >> 2) & 1)))];
                            corner[c] = d;
                            if (d > 0f) inside++;
                        }
                        if (inside == 0 || inside == 8) continue;

                        // 符号の変わる辺の交点の平均に頂点を置く
                        float sx = 0f, sy = 0f, sz = 0f;
                        int crossings = 0;
                        for (int a = 0; a < 8; a++)
                        {
                            for (int axis = 0; axis < 3; axis++)
                            {
                                int bit = 1 << axis;
                                if ((a & bit) != 0) continue;
                                int b = a | bit;
                                float da = corner[a];
                                float db = corner[b];
                                if ((da > 0f) == (db > 0f)) continue;
                                float t = da / (da - db);
                                float px = a & 1, py = (a >> 1) & 1, pz = (a >> 2) & 1;
                                if (axis == 0) px += t; else if (axis == 1) py += t; else pz += t;
                                sx += px; sy += py; sz += pz;
                                crossings++;
                            }
                        }
                        float gx = corner[1] - corner[0] + corner[3] - corner[2] + corner[5] - corner[4] + corner[7] - corner[6];
                        float gy = corner[2] - corner[0] + corner[3] - corner[1] + corner[6] - corner[4] + corner[7] - corner[5];
                        float gz = corner[4] - corner[0] + corner[5] - corner[1] + corner[6] - corner[2] + corner[7] - corner[3];
                        float length = (float)Math.Sqrt(gx * gx + gy * gy + gz * gz);
                        if (length < 1e-9f) length = 1f;

                        cellVertex[cellIndex] = positions.Count / 3;
                        positions.Add(MinX + (i + sx / crossings) * v);
                        positions.Add(MinY + (j + sy / crossings) * v);
                        positions.Add(MinZ + (k + sz / crossings) * v);
                        // 密度は岩の中が正なので、外向きは勾配の逆
                        normals.Add(-gx / length);
                        normals.Add(-gy / length);
                        normals.Add(-gz / length);
                    }
                }
            }

            Positions = positions.ToArray();
            Normals = normals.ToArray();
            var indices = new List<int>(Positions.Length * 2);
            for (int k = 0; k < nz; k++)
            {
                for (int j = 0; j < ny; j++)
                {
                    for (int i = 0; i < nx; i++)
                    {
                        bool here = density[i + nx * (j + ny * k)] > 0f;
                        if (i < cx && j >= 1 && j < cy && k >= 1 && k < cz && here != (density[i + 1 + nx * (j + ny * k)] > 0f))
                            Quad(indices, cellVertex[i + cx * (j - 1 + cy * (k - 1))], cellVertex[i + cx * (j + cy * (k - 1))],
                                cellVertex[i + cx * (j + cy * k)], cellVertex[i + cx * (j - 1 + cy * k)]);
                        if (j < cy && i >= 1 && i < cx && k >= 1 && k < cz && here != (density[i + nx * (j + 1 + ny * k)] > 0f))
                            Quad(indices, cellVertex[i - 1 + cx * (j + cy * (k - 1))], cellVertex[i + cx * (j + cy * (k - 1))],
                                cellVertex[i + cx * (j + cy * k)], cellVertex[i - 1 + cx * (j + cy * k)]);
                        if (k < cz && i >= 1 && i < cx && j >= 1 && j < cy && here != (density[i + nx * (j + ny * (k + 1))] > 0f))
                            Quad(indices, cellVertex[i - 1 + cx * (j - 1 + cy * k)], cellVertex[i + cx * (j - 1 + cy * k)],
                                cellVertex[i + cx * (j + cy * k)], cellVertex[i - 1 + cx * (j + cy * k)]);
                    }
                }
            }
            Indices = indices.ToArray();

            int down = 0;
            for (int p = 1; p < Normals.Length; p += 3) if (Normals[p] < 0f) down++;
            DownFacingShare = VertexCount > 0 ? down / (float)VertexCount : 0f;
        }

        private void Quad(List<int> indices, int a, int b, int c, int d)
        {
            if (a < 0 || b < 0 || c < 0 || d < 0) return;
            Triangle(indices, a, b, c);
            Triangle(indices, a, c, d);
        }

        /// <summary>三角形を、頂点の外向きの向きに合わせた回り方で足す。</summary>
        private void Triangle(List<int> indices, int a, int b, int c)
        {
            float[] p = Positions;
            float ux = p[b * 3] - p[a * 3], uy = p[b * 3 + 1] - p[a * 3 + 1], uz = p[b * 3 + 2] - p[a * 3 + 2];
            float wx = p[c * 3] - p[a * 3], wy = p[c * 3 + 1] - p[a * 3 + 1], wz = p[c * 3 + 2] - p[a * 3 + 2];
            float fx = uy * wz - uz * wy;
            float fy = uz * wx - ux * wz;
            float fz = ux * wy - uy * wx;
            float[] n = Normals;
            float outward = fx * (n[a * 3] + n[b * 3] + n[c * 3]) + fy * (n[a * 3 + 1] + n[b * 3 + 1] + n[c * 3 + 1])
                + fz * (n[a * 3 + 2] + n[b * 3 + 2] + n[c * 3 + 2]);
            indices.Add(a);
            if (outward >= 0f) { indices.Add(b); indices.Add(c); }
            else { indices.Add(c); indices.Add(b); }
        }

        /// <summary>頂点ごとに、太陽の方向と真上へ密度をたどり、岩に当たるかを調べる（影と、岩の下かどうか）。</summary>
        private void Light(float v)
        {
            int count = VertexCount;
            Sun = new float[count];
            Sky = new float[count];
            double altitude = SunAltitudeDegrees * Math.PI / 180.0;
            float sunX = -(float)Math.Cos(altitude) * 0.70710678f;
            float sunY = (float)Math.Sin(altitude);
            float sunZ = -(float)Math.Cos(altitude) * 0.70710678f;
            Parallel.For(0, count, i =>
            {
                float x = Positions[i * 3] + Normals[i * 3] * 1.2f * v;
                float y = Positions[i * 3 + 1] + Normals[i * 3 + 1] * 1.2f * v;
                float z = Positions[i * 3 + 2] + Normals[i * 3 + 2] * 1.2f * v;
                float sun = 1f;
                for (int s = 1; s <= 16; s++)
                {
                    float step = s * v;
                    if (Density(x + sunX * step, y + sunY * step, z + sunZ * step) <= 0f) continue;
                    sun = 0f;
                    break;
                }
                // 真上の判定は、面から 3 升目離れた所から始める（ほぼ垂直な崖で、壁の出っ張りを「頭上の岩」と数えないように）
                float skyX = Positions[i * 3] + Normals[i * 3] * 3f * v;
                float skyY = Positions[i * 3 + 1] + Normals[i * 3 + 1] * 3f * v;
                float skyZ = Positions[i * 3 + 2] + Normals[i * 3 + 2] * 3f * v;
                float sky = 1f;
                for (int s = 0; s <= 48; s++)
                {
                    if (Density(skyX, skyY + s * v, skyZ) <= 0f) continue;
                    sky = 0f;
                    break;
                }
                Sun[i] = sun;
                Sky[i] = sky;
            });
        }

        private static float Clamp(float value, float low, float high)
        {
            if (high < low) high = low;
            return value < low ? low : (value > high ? high : value);
        }
    }

    /// <summary>体積で作る特徴を、地形のどこに置くかの規則（3D 化の試作、段階 D）。</summary>
    public static class RegionVolumeFeatures
    {
        /// <summary>
        /// 拡大した地形の崖（RegionData.Cliff）を 1 つ選び、崖下のえぐれ（やわらかい層が先に削れて、硬い層が張り出した形）と、
        /// そこから奥へ入る洞窟（曲がった横穴と、奥の広間）を置く。崖が無い、または低すぎる時は null。
        /// 選ぶのは、崖の面がまとまっていて、窓の中心に近い所。大きさは崖の高さに合わせて縮める。
        /// </summary>
        public static RegionVolumeSpec CliffShelterAndCave(RegionData terrain, float voxelMeters)
        {
            if (terrain == null || terrain.Cliff == null) return null;
            int n = terrain.Resolution;
            float cell = terrain.CellMeters;
            float[] cliff = terrain.Cliff;
            int margin = (int)(72f / cell);
            if (n - 2 * margin < 4) return null;
            int stride = Math.Max(1, (int)(4f / cell));
            int radius = Math.Max(2, (int)(16f / cell));
            int inner = Math.Max(1, radius / 8);
            int bestCell = -1;
            float bestScore = 0f;
            for (int y = margin; y < n - margin; y += stride)
            {
                for (int x = margin; x < n - margin; x += stride)
                {
                    if (cliff[x + y * n] < 0.6f) continue;
                    int faces = 0;
                    for (int dy = -radius; dy <= radius; dy += inner)
                        for (int dx = -radius; dx <= radius; dx += inner)
                            if (cliff[x + dx + (y + dy) * n] > 0.5f) faces++;
                    float fromCenterX = (x - n * 0.5f) / n;
                    float fromCenterY = (y - n * 0.5f) / n;
                    float score = faces * (1f - 1.2f * (float)Math.Sqrt(fromCenterX * fromCenterX + fromCenterY * fromCenterY));
                    if (score <= bestScore) continue;
                    bestScore = score;
                    bestCell = x + y * n;
                }
            }
            if (bestCell < 0) return null;

            var spec = new RegionVolumeSpec { VoxelMeters = voxelMeters, Seed = unchecked((uint)terrain.Spec.Seed * 2654435761u + 97u) };
            float cx = (bestCell % n) * cell;
            float cz = (bestCell / n) * cell;

            // 登る向き（水平の単位ベクトル）と、崖に沿う向き
            float gx = RegionVolumePatch.SampleHeight(terrain, cx + 12f, cz) - RegionVolumePatch.SampleHeight(terrain, cx - 12f, cz);
            float gz = RegionVolumePatch.SampleHeight(terrain, cx, cz + 12f) - RegionVolumePatch.SampleHeight(terrain, cx, cz - 12f);
            float steepness = (float)Math.Sqrt(gx * gx + gz * gz);
            if (steepness < 1e-3f) return null;
            float ux = gx / steepness, uz = gz / steepness;
            float ax = -uz, az = ux;

            // 登る向きの断面で、崖の足もとと上端を探す（1 m ごと。傾き 0.9 より急な所が続く範囲）
            int steepest = 0;
            float most = 0f;
            for (int t = -8; t <= 8; t++)
            {
                float slope = Slope(terrain, cx, cz, ux, uz, t);
                if (slope > most) { most = slope; steepest = t; }
            }
            int foot = steepest;
            while (foot > -40 && Slope(terrain, cx, cz, ux, uz, foot - 1) > 0.9f) foot--;
            int top = steepest;
            while (top < 40 && Slope(terrain, cx, cz, ux, uz, top + 1) > 0.9f) top++;
            float footX = cx + ux * (foot - 1), footZ = cz + uz * (foot - 1);
            float footY = RegionVolumePatch.SampleHeight(terrain, footX, footZ);
            float topY = RegionVolumePatch.SampleHeight(terrain, cx + ux * (top + 1), cz + uz * (top + 1));
            float height = topY - footY;
            if (height < 12f) return null;
            float k = height / 40f;
            if (k < 0.6f) k = 0.6f; else if (k > 1f) k = 1f;

            // 崖下のえぐれ：崖に沿って横に長く、上下につぶれた空洞。床は足もとの高さで平らにする
            spec.Cavities.Add(new RegionVolumeCavity
            {
                Ax = footX + ux * 2f * k - ax * 14f * k, Ay = footY + 2.6f * k, Az = footZ + uz * 2f * k - az * 14f * k,
                Bx = footX + ux * 2f * k + ax * 14f * k, By = footY + 2.6f * k, Bz = footZ + uz * 2f * k + az * 14f * k,
                RadiusA = 8f * k, RadiusB = 8f * k, Flatten = 0.55f, FloorY = footY - 0.4f, Roughness = 1.2f,
            });
            // 洞窟：えぐれの奥から入り、曲がりながら少し登って、奥で広間になる。上に岩が十分ある時だけ足す
            float midX = footX + ux * 24f * k + ax * 7f * k, midY = footY + 4.5f * k, midZ = footZ + uz * 24f * k + az * 7f * k;
            float endX = footX + ux * 42f * k - ax * 3f * k, endY = footY + 7f * k, endZ = footZ + uz * 42f * k - az * 3f * k;
            bool tunnel = RegionVolumePatch.SampleHeight(terrain, midX, midZ) > midY + 3.4f * k + 4f;
            bool chamber = tunnel && RegionVolumePatch.SampleHeight(terrain, endX, endZ) > endY + 7.5f * k * 0.75f + 4f;
            if (tunnel)
            {
                spec.Cavities.Add(new RegionVolumeCavity
                {
                    Ax = footX + ux * 6f * k, Ay = footY + 2.2f * k, Az = footZ + uz * 6f * k,
                    Bx = midX, By = midY, Bz = midZ,
                    RadiusA = 2.8f * k, RadiusB = 3.4f * k, Flatten = 0.9f, Roughness = 0.9f,
                });
            }
            if (chamber)
            {
                spec.Cavities.Add(new RegionVolumeCavity
                {
                    Ax = midX, Ay = midY, Az = midZ,
                    Bx = endX, By = endY, Bz = endZ,
                    RadiusA = 3.4f * k, RadiusB = 7.5f * k, Flatten = 0.75f, Roughness = 1.4f,
                });
            }

            spec.CenterX = footX + ux * 10f;
            spec.CenterZ = footZ + uz * 10f;
            spec.FocusX = footX;
            spec.FocusY = footY + 4f * k;
            spec.FocusZ = footZ;
            // カメラは崖の下手（登る向きの逆）に置く
            spec.FacingYawDegrees = (float)(Math.Atan2(-ux, -uz) * 180.0 / Math.PI);
            spec.Note = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "崖の高さ {0:F0} m（足もと {1:F0} m）、位置 X{2:F0} Z{3:F0} m、空洞 {4} 個（えぐれ{5}{6}）",
                height, footY, footX, footZ, spec.Cavities.Count, tunnel ? "・横穴" : "", chamber ? "・広間" : "");
            return spec;
        }

        private static float Slope(RegionData terrain, float cx, float cz, float ux, float uz, int t)
        {
            return (RegionVolumePatch.SampleHeight(terrain, cx + ux * (t + 1), cz + uz * (t + 1)) - RegionVolumePatch.SampleHeight(terrain, cx + ux * (t - 1), cz + uz * (t - 1))) * 0.5f;
        }
    }
}
