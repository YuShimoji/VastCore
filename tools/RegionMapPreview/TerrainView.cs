using System;
using System.Threading.Tasks;

namespace Vastcore.RegionMap.Preview
{
    /// <summary>
    /// 地形メッシュ（RegionTerrainMesh）を斜めから見た絵にする確認用の簡易描画。Unity を起動せずに3D化の見え方を確かめるためのもので、
    /// 製品の描画ではない。真上から見た画像（陰影が焼き込み済み）を地形に貼り、遠くを霞ませる。光の計算はしない。
    /// </summary>
    internal static class TerrainView
    {
        internal sealed class Camera
        {
            /// <summary>カメラのいる方位（度）。0 で南から北を見る。90 で東から西を見る。</summary>
            public float Yaw = 0f;

            /// <summary>見下ろす角度（度）。90 で真上。</summary>
            public float Pitch = 35f;

            /// <summary>注視点から見たカメラまでの距離（m）。</summary>
            public float Distance = 200000f;

            public float VerticalFovDegrees = 40f;

            /// <summary>注視点（地図の中心からのずれではなく、地図座標 0〜100）。</summary>
            public float TargetMapX = 50f;
            public float TargetMapY = 50f;

            public int Width = 1600;
            public int Height = 900;
            public int Supersample = 2;

            /// <summary>
            /// 地形の面の向きから光を計算する（太陽は北西、高さ 38 度）。貼る画像は陰影なしで描いたものを渡す。
            /// 急な面は真上からの画像が引き伸ばされて縞になるので、岩の色へ寄せる。崖の形を見るための指定。
            /// </summary>
            public bool Lit = false;
        }

        internal static byte[] Render(RegionData data, RegionTerrainMesh mesh, byte[] albedo, Camera camera, out int width, out int height)
        {
            int ss = Math.Max(1, camera.Supersample);
            int w = camera.Width * ss;
            int h = camera.Height * ss;
            width = camera.Width;
            height = camera.Height;

            float yaw = camera.Yaw * (float)Math.PI / 180f;
            float pitch = camera.Pitch * (float)Math.PI / 180f;
            float extent = data.SizeKm * 1000f;
            float tx = camera.TargetMapX / RegionData.MapUnits * extent;
            float tz = camera.TargetMapY / RegionData.MapUnits * extent;
            float ty = SampleHeight(mesh, tx, tz);

            float cosP = (float)Math.Cos(pitch);
            float px = tx + camera.Distance * cosP * (float)Math.Sin(yaw);
            float py = ty + camera.Distance * (float)Math.Sin(pitch);
            float pz = tz + camera.Distance * cosP * (float)Math.Cos(yaw);

            Normalize(tx - px, ty - py, tz - pz, out float fx, out float fy, out float fz);
            // 右 = 前 × 上(0,1,0)、上 = 右 × 前
            Normalize(fy * 0f - fz * 1f, fz * 0f - fx * 0f, fx * 1f - fy * 0f, out float rx, out float ry, out float rz);
            float ux = ry * fz - rz * fy;
            float uy = rz * fx - rx * fz;
            float uz = rx * fy - ry * fx;
            float focal = h * 0.5f / (float)Math.Tan(camera.VerticalFovDegrees * 0.5f * Math.PI / 180f);
            const float Near = 50f;

            int vertexCount = mesh.VertexCount;
            var sx = new float[vertexCount];
            var sy = new float[vertexCount];
            var depth = new float[vertexCount];
            Parallel.For(0, vertexCount, v =>
            {
                float dx = mesh.Positions[v * 3] - px;
                float dy = mesh.Positions[v * 3 + 1] - py;
                float dz = mesh.Positions[v * 3 + 2] - pz;
                float cz = dx * fx + dy * fy + dz * fz;
                depth[v] = cz;
                if (cz < Near) { sx[v] = float.NaN; return; }
                sx[v] = w * 0.5f + (dx * rx + dy * ry + dz * rz) / cz * focal;
                sy[v] = h * 0.5f - (dx * ux + dy * uy + dz * uz) / cz * focal;
            });

            float[] normals = camera.Lit ? BuildNormals(mesh) : null;
            const float SunAltitude = 38f * (float)Math.PI / 180f;
            float sunX = -(float)Math.Cos(SunAltitude) * 0.70710678f;
            float sunY = (float)Math.Sin(SunAltitude);
            float sunZ = -(float)Math.Cos(SunAltitude) * 0.70710678f;

            var zbuffer = new float[w * h];
            for (int i = 0; i < zbuffer.Length; i++) zbuffer[i] = float.MaxValue;
            var color = new float[w * h * 3];
            var covered = new bool[w * h];
            int n = data.Resolution;

            int[] ind = mesh.Indices;
            for (int t = 0; t < ind.Length; t += 3)
            {
                int ia = ind[t], ib = ind[t + 1], ic = ind[t + 2];
                float ax = sx[ia], bx = sx[ib], cx = sx[ic];
                if (float.IsNaN(ax) || float.IsNaN(bx) || float.IsNaN(cx)) continue;
                float ay = sy[ia], by = sy[ib], cy = sy[ic];
                int minX = Math.Max(0, (int)Math.Floor(Math.Min(ax, Math.Min(bx, cx))));
                int maxX = Math.Min(w - 1, (int)Math.Ceiling(Math.Max(ax, Math.Max(bx, cx))));
                int minY = Math.Max(0, (int)Math.Floor(Math.Min(ay, Math.Min(by, cy))));
                int maxY = Math.Min(h - 1, (int)Math.Ceiling(Math.Max(ay, Math.Max(by, cy))));
                if (minX > maxX || minY > maxY) continue;
                float area = (bx - ax) * (cy - ay) - (cx - ax) * (by - ay);
                if (Math.Abs(area) < 1e-6f) continue;
                float invArea = 1f / area;
                float wa = 1f / depth[ia], wb = 1f / depth[ib], wc = 1f / depth[ic];
                float ua = mesh.Uv[ia * 2] * wa, ub = mesh.Uv[ib * 2] * wb, uc = mesh.Uv[ic * 2] * wc;
                float va = mesh.Uv[ia * 2 + 1] * wa, vb = mesh.Uv[ib * 2 + 1] * wb, vc = mesh.Uv[ic * 2 + 1] * wc;

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        float qx = x + 0.5f - ax;
                        float qy = y + 0.5f - ay;
                        float l1 = (qx * (cy - ay) - (cx - ax) * qy) * invArea;
                        float l2 = ((bx - ax) * qy - qx * (by - ay)) * invArea;
                        float l0 = 1f - l1 - l2;
                        if (l0 < 0f || l1 < 0f || l2 < 0f) continue;
                        float invW = l0 * wa + l1 * wb + l2 * wc;
                        float z = 1f / invW;
                        int p = x + y * w;
                        if (z >= zbuffer[p]) continue;
                        zbuffer[p] = z;
                        covered[p] = true;
                        float u = (l0 * ua + l1 * ub + l2 * uc) * z;
                        float v = (l0 * va + l1 * vb + l2 * vc) * z;
                        Sample(albedo, n, u, v, out float r, out float g, out float b);
                        if (normals != null)
                        {
                            float nx = l0 * normals[ia * 3] + l1 * normals[ib * 3] + l2 * normals[ic * 3];
                            float ny = l0 * normals[ia * 3 + 1] + l1 * normals[ib * 3 + 1] + l2 * normals[ic * 3 + 1];
                            float nz = l0 * normals[ia * 3 + 2] + l1 * normals[ib * 3 + 2] + l2 * normals[ic * 3 + 2];
                            Normalize(nx, ny, nz, out nx, out ny, out nz);
                            // 40 度より急な面から岩の色を混ぜ、58 度で岩だけにする
                            float rock = (0.766f - ny) / 0.236f;
                            if (rock > 0f)
                            {
                                if (rock > 1f) rock = 1f;
                                r += (150f - r) * rock;
                                g += (141f - g) * rock;
                                b += (128f - b) * rock;
                            }
                            float facing = nx * sunX + ny * sunY + nz * sunZ;
                            float light = 0.38f + 1.05f * (facing > 0f ? facing : 0f);
                            r *= light; g *= light; b *= light;
                        }
                        // 遠くを霞ませる
                        float haze = 1f - (float)Math.Exp(-z / (camera.Distance * 2.2f));
                        haze *= 0.75f;
                        color[p * 3] = r + (176f - r) * haze;
                        color[p * 3 + 1] = g + (196f - g) * haze;
                        color[p * 3 + 2] = b + (216f - b) * haze;
                    }
                }
            }

            // 空
            for (int y = 0; y < h; y++)
            {
                float k = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    int p = x + y * w;
                    if (covered[p]) continue;
                    color[p * 3] = 120f + (196f - 120f) * k;
                    color[p * 3 + 1] = 160f + (212f - 160f) * k;
                    color[p * 3 + 2] = 214f + (226f - 214f) * k;
                }
            }

            // 縮小して滑らかにする
            var output = new byte[width * height * 4];
            float norm = 1f / (ss * ss);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float r = 0f, g = 0f, b = 0f;
                    for (int sy2 = 0; sy2 < ss; sy2++)
                    for (int sx2 = 0; sx2 < ss; sx2++)
                    {
                        int p = (x * ss + sx2) + (y * ss + sy2) * w;
                        r += color[p * 3]; g += color[p * 3 + 1]; b += color[p * 3 + 2];
                    }
                    int o = (x + y * width) * 4;
                    output[o] = (byte)Math.Min(255f, r * norm + 0.5f);
                    output[o + 1] = (byte)Math.Min(255f, g * norm + 0.5f);
                    output[o + 2] = (byte)Math.Min(255f, b * norm + 0.5f);
                    output[o + 3] = 255;
                }
            }
            return output;
        }

        /// <summary>頂点ごとの面の向き（上向きの単位ベクトル）。格子の隣の頂点との差から作る。</summary>
        private static float[] BuildNormals(RegionTerrainMesh mesh)
        {
            int cols = mesh.Columns;
            int rows = mesh.Rows;
            var normals = new float[cols * rows * 3];
            float[] p = mesh.Positions;
            Parallel.For(0, rows, j =>
            {
                for (int i = 0; i < cols; i++)
                {
                    int left = (i > 0 ? i - 1 : i) + j * cols;
                    int right = (i < cols - 1 ? i + 1 : i) + j * cols;
                    int up = i + (j > 0 ? j - 1 : j) * cols;
                    int down = i + (j < rows - 1 ? j + 1 : j) * cols;
                    float dx = p[right * 3] - p[left * 3];
                    float dz = p[down * 3 + 2] - p[up * 3 + 2];
                    float slopeX = dx > 0f ? (p[right * 3 + 1] - p[left * 3 + 1]) / dx : 0f;
                    float slopeZ = dz > 0f ? (p[down * 3 + 1] - p[up * 3 + 1]) / dz : 0f;
                    int v = (i + j * cols) * 3;
                    Normalize(-slopeX, 1f, -slopeZ, out normals[v], out normals[v + 1], out normals[v + 2]);
                }
            });
            return normals;
        }

        private static float SampleHeight(RegionTerrainMesh mesh, float worldX, float worldZ)
        {
            float last = mesh.Positions[(mesh.VertexCount - 1) * 3];
            float t = Math.Max(1f, last);
            int i = (int)Math.Round(worldX / t * (mesh.Columns - 1));
            int j = (int)Math.Round(worldZ / t * (mesh.Rows - 1));
            i = Math.Max(0, Math.Min(mesh.Columns - 1, i));
            j = Math.Max(0, Math.Min(mesh.Rows - 1, j));
            return mesh.Positions[(i + j * mesh.Columns) * 3 + 1];
        }

        private static void Normalize(float x, float y, float z, out float ox, out float oy, out float oz)
        {
            float length = (float)Math.Sqrt(x * x + y * y + z * z);
            if (length < 1e-9f) length = 1f;
            ox = x / length; oy = y / length; oz = z / length;
        }

        private static void Sample(byte[] rgba, int n, float u, float v, out float r, out float g, out float b)
        {
            float fx = Math.Max(0f, Math.Min(n - 1.001f, u * (n - 1)));
            float fy = Math.Max(0f, Math.Min(n - 1.001f, v * (n - 1)));
            int x0 = (int)fx, y0 = (int)fy;
            float tx = fx - x0, ty = fy - y0;
            int p00 = (x0 + y0 * n) * 4, p10 = p00 + 4, p01 = p00 + n * 4, p11 = p01 + 4;
            r = Mix(rgba[p00], rgba[p10], rgba[p01], rgba[p11], tx, ty);
            g = Mix(rgba[p00 + 1], rgba[p10 + 1], rgba[p01 + 1], rgba[p11 + 1], tx, ty);
            b = Mix(rgba[p00 + 2], rgba[p10 + 2], rgba[p01 + 2], rgba[p11 + 2], tx, ty);
        }

        private static float Mix(float a, float b, float c, float d, float tx, float ty)
        {
            return (a + (b - a) * tx) * (1f - ty) + (c + (d - c) * tx) * ty;
        }
    }
}
