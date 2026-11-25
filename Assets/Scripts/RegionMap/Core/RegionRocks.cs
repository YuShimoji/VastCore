using System;
using System.Collections.Generic;

namespace Vastcore.RegionMap
{
    /// <summary>置いた岩 1 つ。位置は、渡した原点からの m（X 東・Y 上・Z 南）。</summary>
    public struct RegionRock
    {
        /// <summary>岩の中心の真下の地面。</summary>
        public float X, Y, Z;

        /// <summary>いちばん長い辺（m）。</summary>
        public float Size;

        /// <summary>地面から岩の上端までの高さ（m）。</summary>
        public float Top;

        public int FirstVertex, VertexCount;
    }

    /// <summary>
    /// 崖の下の岩屑の斜面に置く、角ばった岩の集まり（見え方の想定の条件 M3・N6）。
    /// 置く場所は RegionData.Talus（台地を入れた拡大した地形が持つ）で決まり、礫の 0.2 m から、背丈を超える 9 m までを、
    /// 小さい物ほど数多く置く。大きい岩は斜面の裾に多い（落ちた岩は大きいほど遠くまで転がる）。礫は崖の足もとの近くにだけ置く（数が多いので）。
    /// 位置・大きさ・向きは地域全体の座標（m）の升目から決めるので、窓を変えても同じ場所には同じ岩が出る。
    /// 形は、箱を平面で数回切り落とした凸の多面体（0.8 m より小さい物は、数が多いので 8 面の形）。面ごとに向きを持つ（角が立って見える）。
    /// </summary>
    public sealed class RegionRockField
    {
        /// <summary>頂点ごとの X・Y・Z（m）と、面の外向きの向き。長さは頂点数 × 3。</summary>
        public float[] Positions;
        public float[] Normals;

        /// <summary>三角形ごとの頂点番号 3 つ。回り方は RegionTerrainMesh と同じ（X 東・Z 南の座標で、外から見て時計回り）。</summary>
        public int[] Indices;

        /// <summary>頂点ごとの色のむら（0〜1。岩ごとに 1 つの値）。</summary>
        public float[] Tint;

        /// <summary>頂点ごとの、岩の中での高さ（0 が下端、1 が上端）。地面に近い所を暗くするのに使う。</summary>
        public float[] Lift;

        public RegionRock[] Rocks;

        public int VertexCount
        {
            get { return Positions.Length / 3; }
        }

        public int TriangleCount
        {
            get { return Indices.Length / 3; }
        }

        private const int ShapeCount = 40;
        private const int TierCount = 6;
        private const float SmallestMeters = 0.2f;
        private static Shape[] _shapes;
        private static Shape[] _simpleShapes;

        private sealed class Shape
        {
            public float[] Positions;
            public float[] Normals;
        }

        /// <summary>
        /// data は台地を入れた拡大した地形。worldX・worldZ は、data のセル (0,0) の中心の、地域全体の座標（m）。
        /// originX・originZ は、同じ点を出力の座標でどこに置くか。minSize より小さい岩は置かない。
        /// skip を渡すと、その範囲（出力の座標の minX, minZ, maxX, maxZ）には置かない。
        /// </summary>
        public static RegionRockField Build(RegionData data, double worldX, double worldZ, float originX, float originZ,
            float minSize, float[] skip)
        {
            if (data == null) throw new ArgumentNullException("data");
            var positions = new List<float>();
            var normals = new List<float>();
            var tint = new List<float>();
            var lift = new List<float>();
            var rocks = new List<RegionRock>();
            float[] talus = data.Talus;
            if (talus != null)
            {
                Shape[] shapes = Shapes(false);
                // 小さい岩は 8 面の形にする（数が多いので）
                Shape[] simple = Shapes(true);
                int n = data.Resolution;
                float cell = data.CellMeters;
                uint seed = unchecked((uint)data.Spec.Seed * 2654435761u + 977u);
                double span = (n - 1) * (double)cell;
                for (int tier = 0; tier < TierCount; tier++)
                {
                    float smallest = SmallestMeters * (1 << tier);
                    if (smallest * 2f <= minSize) continue;
                    double grid = 0.95 * (1 << tier);
                    uint tierSeed = seed + (uint)tier * 7919u;
                    int ix0 = (int)Math.Floor(worldX / grid), ix1 = (int)Math.Floor((worldX + span) / grid);
                    int iz0 = (int)Math.Floor(worldZ / grid), iz1 = (int)Math.Floor((worldZ + span) / grid);
                    for (int iz = iz0; iz <= iz1; iz++)
                    {
                        for (int ix = ix0; ix <= ix1; ix++)
                        {
                            double wx = (ix + RegionNoise.Hash01(ix, iz, tierSeed + 1u)) * grid;
                            double wz = (iz + RegionNoise.Hash01(ix, iz, tierSeed + 2u)) * grid;
                            float lx = (float)((wx - worldX) / cell);
                            float lz = (float)((wz - worldZ) / cell);
                            if (lx < 1f || lz < 1f || lx >= n - 2 || lz >= n - 2) continue;
                            float amount = Sample(talus, n, lx, lz);
                            if (amount <= 0.01f) continue;
                            // まとまって落ちている所と、まばらな所がある
                            float clump = 0.35f + 1.3f * RegionNoise.Smooth(-0.35f, 0.45f,
                                RegionNoise.Fbm((float)(wx / 33.0) + tier * 5.1f, (float)(wz / 33.0) - tier * 3.7f, 2, seed + 31u));
                            if (RegionNoise.Hash01(ix, iz, tierSeed + 3u) >= Density(tier, amount) * clump) continue;

                            int cx = (int)(lx + 0.5f), cz = (int)(lz + 0.5f);
                            int c = cx + cz * n;
                            if (data.Height[c] <= 0f) continue;
                            if (data.WaterCoverage != null && data.WaterCoverage[c] > 0f) continue;
                            float size = smallest * (1f + RegionNoise.Hash01(ix, iz, tierSeed + 4u));
                            if (size < minSize) continue;
                            if (size > 9f) size = 9f;
                            float x = originX + lx * cell;
                            float z = originZ + lz * cell;
                            if (skip != null && x > skip[0] && z > skip[1] && x < skip[2] && z < skip[3]) continue;

                            // 崖の面（急すぎる所）には置かない
                            float gx = (Sample(data.Height, n, lx + 1f, lz) - Sample(data.Height, n, lx - 1f, lz)) / (2f * cell);
                            float gz = (Sample(data.Height, n, lx, lz + 1f) - Sample(data.Height, n, lx, lz - 1f)) / (2f * cell);
                            if (gx * gx + gz * gz > 1.6f) continue;

                            float h1 = RegionNoise.Hash01(ix, iz, tierSeed + 5u);
                            float h2 = RegionNoise.Hash01(ix, iz, tierSeed + 6u);
                            float h3 = RegionNoise.Hash01(ix, iz, tierSeed + 7u);
                            Shape shape = (tier < 2 ? simple : shapes)[(int)(RegionNoise.Hash(ix, iz, tierSeed + 8u) % ShapeCount)];
                            // 板のように平たい物から、さいころに近い物まで
                            float sx = size, sy = size * (0.3f + 0.55f * h1 * h1), sz = size * (0.5f + 0.5f * h2);
                            float sink = sy * (0.22f + 0.2f * h3);
                            float ground = Sample(data.Height, n, lx, lz);
                            var rock = new RegionRock
                            {
                                X = x, Y = ground, Z = z, Size = size, Top = sy - sink,
                                FirstVertex = positions.Count / 3,
                            };
                            Append(shape, positions, normals, x, ground + sy * 0.5f - sink, z, sx, sy, sz,
                                h3 * 6.2831853f, -gx * 0.7f, -gz * 0.7f, (h2 - 0.5f) * 0.7f, (h3 - 0.5f) * 0.7f);
                            rock.VertexCount = positions.Count / 3 - rock.FirstVertex;
                            float shade = RegionNoise.Hash01(ix, iz, tierSeed + 9u);
                            for (int v = 0; v < rock.VertexCount; v++)
                            {
                                tint.Add(shade);
                                lift.Add(shape.Positions[v * 3 + 1] + 0.5f);
                            }
                            rocks.Add(rock);
                        }
                    }
                }
            }

            var field = new RegionRockField
            {
                Positions = positions.ToArray(),
                Normals = normals.ToArray(),
                Tint = tint.ToArray(),
                Lift = lift.ToArray(),
                Rocks = rocks.ToArray(),
            };
            // 形の頂点は三角形ごとに 3 つずつ並んでいる
            field.Indices = new int[field.VertexCount];
            for (int i = 0; i < field.Indices.Length; i++) field.Indices[i] = i;
            return field;
        }

        /// <summary>
        /// 升目 1 つに岩を置く確率。amount は RegionData.Talus（崖の足もとで 1、斜面の裾で 0.5、その先で 0 へ）。
        /// 小さい岩は斜面の全体に、大きい岩は裾の近くに多い。
        /// </summary>
        private static float Density(int tier, float amount)
        {
            switch (tier)
            {
                case 0: return 0.48f * RegionNoise.Smooth(0.60f, 0.85f, amount);
                case 1: return 0.44f * Math.Min(1f, 2.4f * amount);
                case 2: return 0.42f * Math.Min(1f, 2.2f * amount);
                case 3: return 0.38f * (0.25f * amount + Bump(amount, 0.5f, 0.35f));
                case 4: return 0.30f * Bump(amount, 0.48f, 0.30f);
                default: return 0.14f * Bump(amount, 0.45f, 0.25f);
            }
        }

        private static float Bump(float value, float center, float width)
        {
            float d = Math.Abs(value - center) / width;
            return d >= 1f ? 0f : 1f - d;
        }

        private static float Sample(float[] values, int n, float x, float y)
        {
            int x0 = (int)x, y0 = (int)y;
            if (x0 < 0) x0 = 0; else if (x0 > n - 2) x0 = n - 2;
            if (y0 < 0) y0 = 0; else if (y0 > n - 2) y0 = n - 2;
            float fx = x - x0, fy = y - y0;
            int i = x0 + y0 * n;
            float top = values[i] + (values[i + 1] - values[i]) * fx;
            float bottom = values[i + n] + (values[i + n + 1] - values[i + n]) * fx;
            return top + (bottom - top) * fy;
        }

        /// <summary>
        /// 形を、大きさ・向き・位置を決めて書き足す。yaw は真上から見た回転。slopeX・slopeZ は地面の傾きに沿わせる量、
        /// tiltX・tiltZ は岩ごとの傾き（どれも高さ / 水平距離）。
        /// </summary>
        private static void Append(Shape shape, List<float> positions, List<float> normals, float x, float y, float z,
            float sx, float sy, float sz, float yaw, float slopeX, float slopeZ, float tiltX, float tiltZ)
        {
            float cos = (float)Math.Cos(yaw), sin = (float)Math.Sin(yaw);
            // 岩の上向きの軸（地面の向きと、岩ごとの傾きを混ぜる）と、それに直交する 2 軸
            float ux = -slopeX + tiltX, uy = 1f, uz = -slopeZ + tiltZ;
            Normalize(ref ux, ref uy, ref uz);
            float ax = cos, ay = 0f, az = sin;
            float dot = ax * ux + ay * uy + az * uz;
            ax -= dot * ux; ay -= dot * uy; az -= dot * uz;
            Normalize(ref ax, ref ay, ref az);
            float bx = ay * uz - az * uy, by = az * ux - ax * uz, bz = ax * uy - ay * ux;

            float[] p = shape.Positions;
            float[] q = shape.Normals;
            for (int v = 0; v < p.Length; v += 3)
            {
                float px = p[v] * sx, py = p[v + 1] * sy, pz = p[v + 2] * sz;
                positions.Add(x + ax * px + ux * py + bx * pz);
                positions.Add(y + ay * px + uy * py + by * pz);
                positions.Add(z + az * px + uz * py + bz * pz);
                // 面の向きは、伸ばした分だけ逆に縮めてから回す
                float nx = q[v] / sx, ny = q[v + 1] / sy, nz = q[v + 2] / sz;
                float wx = ax * nx + ux * ny + bx * nz, wy = ay * nx + uy * ny + by * nz, wz = az * nx + uz * ny + bz * nz;
                Normalize(ref wx, ref wy, ref wz);
                normals.Add(wx);
                normals.Add(wy);
                normals.Add(wz);
            }
        }

        private static void Normalize(ref float x, ref float y, ref float z)
        {
            float length = (float)Math.Sqrt(x * x + y * y + z * z);
            if (length < 1e-9f) { x = 0f; y = 1f; z = 0f; return; }
            x /= length; y /= length; z /= length;
        }

        private static Shape[] Shapes(bool simple)
        {
            Shape[] shapes = simple ? _simpleShapes : _shapes;
            if (shapes != null) return shapes;
            shapes = new Shape[ShapeCount];
            for (int s = 0; s < ShapeCount; s++)
                shapes[s] = simple ? Pebble(9001u + (uint)s * 137u) : CutBox(7001u + (uint)s * 131u, 5, 5f);
            if (simple) _simpleShapes = shapes; else _shapes = shapes;
            return shapes;
        }

        /// <summary>
        /// 8 面の形（上下・前後・左右の 6 点を少しずらして結んだ物）。大きさは一辺 1 の箱に収まる。三角形ごとに頂点を 3 つずつ持つ。
        /// </summary>
        private static Shape Pebble(uint seed)
        {
            var corner = new double[6][];
            for (int k = 0; k < 6; k++)
            {
                int axis = k / 2;
                double sign = k % 2 == 0 ? 0.5 : -0.5;
                var p = new double[3];
                for (int a = 0; a < 3; a++)
                    p[a] = a == axis ? sign * (0.7 + 0.3 * RegionNoise.Hash01(k, a, seed)) : 0.5 * (RegionNoise.Hash01(k, a, seed) - 0.5);
                corner[k] = p;
            }
            // 0:+X 1:-X 2:+Y 3:-Y 4:+Z 5:-Z。外から見て反時計回りに並べる
            int[][] faces =
            {
                new[] { 0, 2, 4 }, new[] { 2, 1, 4 }, new[] { 1, 3, 4 }, new[] { 3, 0, 4 },
                new[] { 2, 0, 5 }, new[] { 1, 2, 5 }, new[] { 3, 1, 5 }, new[] { 0, 3, 5 },
            };
            var positions = new List<float>();
            var normals = new List<float>();
            foreach (int[] face in faces)
            {
                double[] a = corner[face[0]], b = corner[face[1]], c = corner[face[2]];
                double nx = (b[1] - a[1]) * (c[2] - a[2]) - (b[2] - a[2]) * (c[1] - a[1]);
                double ny = (b[2] - a[2]) * (c[0] - a[0]) - (b[0] - a[0]) * (c[2] - a[2]);
                double nz = (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]);
                double l = Math.Sqrt(nx * nx + ny * ny + nz * nz);
                if (l < 1e-9) continue;
                // 回り方を RegionTerrainMesh に合わせる（外向きと逆回り）
                foreach (double[] p in new[] { a, c, b })
                {
                    positions.Add((float)p[0]);
                    positions.Add((float)p[1]);
                    positions.Add((float)p[2]);
                    normals.Add((float)(nx / l));
                    normals.Add((float)(ny / l));
                    normals.Add((float)(nz / l));
                }
            }
            return new Shape { Positions = positions.ToArray(), Normals = normals.ToArray() };
        }

        /// <summary>
        /// 一辺 1 の箱（中心が原点）を、平面で least〜least + extra 回切り落とした凸の多面体。三角形ごとに頂点を 3 つずつ持つ。
        /// </summary>
        private static Shape CutBox(uint seed, int least, float extra)
        {
            var faces = new List<List<double[]>>();
            double[][] c =
            {
                new[] { -0.5, -0.5, -0.5 }, new[] { 0.5, -0.5, -0.5 }, new[] { 0.5, 0.5, -0.5 }, new[] { -0.5, 0.5, -0.5 },
                new[] { -0.5, -0.5, 0.5 }, new[] { 0.5, -0.5, 0.5 }, new[] { 0.5, 0.5, 0.5 }, new[] { -0.5, 0.5, 0.5 },
            };
            int[][] quads =
            {
                new[] { 0, 3, 2, 1 }, new[] { 4, 5, 6, 7 }, new[] { 0, 1, 5, 4 }, new[] { 2, 3, 7, 6 }, new[] { 1, 2, 6, 5 }, new[] { 0, 4, 7, 3 },
            };
            foreach (int[] quad in quads)
            {
                var face = new List<double[]>();
                foreach (int k in quad) face.Add(c[k]);
                faces.Add(face);
            }

            int cuts = least + (int)(RegionNoise.Hash01(1, 0, seed) * extra);
            for (int k = 0; k < cuts; k++)
            {
                double nx = RegionNoise.Hash01(k, 1, seed) * 2.0 - 1.0;
                // 下の面はあまり切らない（地面に据わって見える）
                double ny = RegionNoise.Hash01(k, 2, seed) * 1.3 - 0.25;
                double nz = RegionNoise.Hash01(k, 3, seed) * 2.0 - 1.0;
                double length = Math.Sqrt(nx * nx + ny * ny + nz * nz);
                if (length < 0.2) continue;
                nx /= length; ny /= length; nz /= length;
                double support = 0.5 * (Math.Abs(nx) + Math.Abs(ny) + Math.Abs(nz));
                double offset = support * (0.42 + 0.42 * RegionNoise.Hash01(k, 4, seed));
                faces = Clip(faces, nx, ny, nz, offset);
            }

            var positions = new List<float>();
            var normals = new List<float>();
            foreach (List<double[]> face in faces)
            {
                if (face.Count < 3) continue;
                // 面の外向き（頂点の並びから）
                double ax = 0, ay = 0, az = 0;
                for (int i = 0; i < face.Count; i++)
                {
                    double[] p = face[i], q = face[(i + 1) % face.Count];
                    ax += (p[1] - q[1]) * (p[2] + q[2]);
                    ay += (p[2] - q[2]) * (p[0] + q[0]);
                    az += (p[0] - q[0]) * (p[1] + q[1]);
                }
                double l = Math.Sqrt(ax * ax + ay * ay + az * az);
                if (l < 1e-9) continue;
                ax /= l; ay /= l; az /= l;
                for (int i = 1; i < face.Count - 1; i++)
                {
                    // 回り方を RegionTerrainMesh に合わせる（外向きと逆回り）
                    double[][] triangle = { face[0], face[i + 1], face[i] };
                    foreach (double[] p in triangle)
                    {
                        positions.Add((float)p[0]);
                        positions.Add((float)p[1]);
                        positions.Add((float)p[2]);
                        normals.Add((float)ax);
                        normals.Add((float)ay);
                        normals.Add((float)az);
                    }
                }
            }
            return new Shape { Positions = positions.ToArray(), Normals = normals.ToArray() };
        }

        /// <summary>凸の多面体から、平面 n·p = offset の外側（n の向き）を切り落とす。切り口は新しい面になる。</summary>
        private static List<List<double[]>> Clip(List<List<double[]>> faces, double nx, double ny, double nz, double offset)
        {
            var result = new List<List<double[]>>();
            var cut = new List<double[]>();
            foreach (List<double[]> face in faces)
            {
                var kept = new List<double[]>();
                for (int i = 0; i < face.Count; i++)
                {
                    double[] a = face[i], b = face[(i + 1) % face.Count];
                    double da = nx * a[0] + ny * a[1] + nz * a[2] - offset;
                    double db = nx * b[0] + ny * b[1] + nz * b[2] - offset;
                    if (da <= 0.0) kept.Add(a);
                    if ((da < 0.0 && db > 0.0) || (da > 0.0 && db < 0.0))
                    {
                        double t = da / (da - db);
                        var p = new[] { a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t };
                        kept.Add(p);
                        cut.Add(p);
                    }
                }
                if (kept.Count >= 3) result.Add(kept);
            }
            if (cut.Count < 3) return result;

            // 切り口の点を、面の中心のまわりの角度の順に並べる
            double cx = 0, cy = 0, cz = 0;
            foreach (double[] p in cut) { cx += p[0]; cy += p[1]; cz += p[2]; }
            cx /= cut.Count; cy /= cut.Count; cz /= cut.Count;
            double ex = Math.Abs(nx) < 0.8 ? 1.0 : 0.0, ey = Math.Abs(nx) < 0.8 ? 0.0 : 1.0, ez = 0.0;
            double d = ex * nx + ey * ny + ez * nz;
            ex -= d * nx; ey -= d * ny; ez -= d * nz;
            double el = Math.Sqrt(ex * ex + ey * ey + ez * ez);
            ex /= el; ey /= el; ez /= el;
            double fx = ny * ez - nz * ey, fy = nz * ex - nx * ez, fz = nx * ey - ny * ex;
            cut.Sort((p, q) =>
            {
                double pa = Math.Atan2((p[0] - cx) * fx + (p[1] - cy) * fy + (p[2] - cz) * fz, (p[0] - cx) * ex + (p[1] - cy) * ey + (p[2] - cz) * ez);
                double qa = Math.Atan2((q[0] - cx) * fx + (q[1] - cy) * fy + (q[2] - cz) * fz, (q[0] - cx) * ex + (q[1] - cy) * ey + (q[2] - cz) * ez);
                return pa.CompareTo(qa);
            });
            // 同じ点（隣り合う面から 2 回出る）を 1 つにする
            var cap = new List<double[]>();
            foreach (double[] p in cut)
            {
                if (cap.Count > 0)
                {
                    double[] last = cap[cap.Count - 1];
                    if (Math.Abs(last[0] - p[0]) + Math.Abs(last[1] - p[1]) + Math.Abs(last[2] - p[2]) < 1e-7) continue;
                }
                cap.Add(p);
            }
            if (cap.Count >= 3) result.Add(cap);
            return result;
        }
    }
}
