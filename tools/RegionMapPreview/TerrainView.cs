using System;
using System.Threading.Tasks;

namespace Vastcore.RegionMap.Preview
{
    /// <summary>
    /// 地形メッシュ（RegionTerrainMesh）を斜めから見た絵にする確認用の簡易描画。Unity を起動せずに3D化の見え方を確かめるためのもので、
    /// 製品の描画ではない。真上から見た画像を地形に貼り、遠くを霞ませる。
    /// 体積で作り直した部分（RegionVolumePatch）を渡すと、その範囲の地形を抜いて、代わりにそれを描く。
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

            /// <summary>注視点の高さ（m）。NaN なら、その位置の地面の高さ。</summary>
            public float TargetHeight = float.NaN;

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
            return Render(data, mesh, albedo, camera, null, out width, out height);
        }

        internal static byte[] Render(RegionData data, RegionTerrainMesh mesh, byte[] albedo, Camera camera, RegionVolumePatch patch,
            out int width, out int height)
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
            float ty = float.IsNaN(camera.TargetHeight) ? SampleHeight(mesh, tx, tz) : camera.TargetHeight;

            float cosP = (float)Math.Cos(pitch);
            var frame = new Frame { W = w, H = h };
            frame.Px = tx + camera.Distance * cosP * (float)Math.Sin(yaw);
            frame.Py = ty + camera.Distance * (float)Math.Sin(pitch);
            frame.Pz = tz + camera.Distance * cosP * (float)Math.Cos(yaw);

            Normalize(tx - frame.Px, ty - frame.Py, tz - frame.Pz, out frame.Fx, out frame.Fy, out frame.Fz);
            // 右 = 前 × 上(0,1,0)、上 = 右 × 前
            Normalize(frame.Fy * 0f - frame.Fz * 1f, frame.Fz * 0f - frame.Fx * 0f, frame.Fx * 1f - frame.Fy * 0f, out frame.Rx, out frame.Ry, out frame.Rz);
            frame.Ux = frame.Ry * frame.Fz - frame.Rz * frame.Fy;
            frame.Uy = frame.Rz * frame.Fx - frame.Rx * frame.Fz;
            frame.Uz = frame.Rx * frame.Fy - frame.Ry * frame.Fx;
            frame.Focal = h * 0.5f / (float)Math.Tan(camera.VerticalFovDegrees * 0.5f * Math.PI / 180f);
            // 近くから見る時は、手前の切り捨ての距離を縮める
            frame.Near = camera.Distance < 400f ? Math.Max(1f, camera.Distance * 0.04f) : 50f;
            frame.HazeDistance = camera.Distance * 2.2f;
            frame.Albedo = albedo;
            frame.AlbedoSize = data.Resolution;
            frame.Zbuffer = new float[w * h];
            for (int i = 0; i < frame.Zbuffer.Length; i++) frame.Zbuffer[i] = float.MaxValue;
            frame.Color = new float[w * h * 3];
            frame.Covered = new bool[w * h];
            const float SunAltitude = RegionVolumePatch.SunAltitudeDegrees * (float)Math.PI / 180f;
            frame.SunX = -(float)Math.Cos(SunAltitude) * 0.70710678f;
            frame.SunY = (float)Math.Sin(SunAltitude);
            frame.SunZ = -(float)Math.Cos(SunAltitude) * 0.70710678f;

            // 体積で作り直した範囲の地形は描かない（縁は 1.5 升ぶん重ねて、隙間を見せない）
            float holeMinX = float.MaxValue, holeMaxX = float.MinValue, holeMinZ = float.MaxValue, holeMaxZ = float.MinValue;
            if (patch != null)
            {
                float overlap = 1.5f * patch.Spec.VoxelMeters;
                holeMinX = patch.MinX + overlap; holeMaxX = patch.MaxX - overlap;
                holeMinZ = patch.MinZ + overlap; holeMaxZ = patch.MaxZ - overlap;
            }
            Draw(frame, mesh.Positions, mesh.Indices, camera.Lit ? BuildNormals(mesh) : null, mesh.Uv, null, null,
                holeMinX, holeMaxX, holeMinZ, holeMaxZ);

            if (patch != null)
            {
                int count = patch.VertexCount;
                var uv = new float[count * 2];
                float span = (data.Resolution - 1) * data.CellMeters;
                for (int v = 0; v < count; v++)
                {
                    uv[v * 2] = patch.Positions[v * 3] / span;
                    uv[v * 2 + 1] = patch.Positions[v * 3 + 2] / span;
                }
                Draw(frame, patch.Positions, patch.Indices, patch.Normals, uv, patch.Sun, patch.Sky,
                    float.MaxValue, float.MinValue, float.MaxValue, float.MinValue);
            }

            // 空
            float[] color = frame.Color;
            for (int y = 0; y < h; y++)
            {
                float k = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    int p = x + y * w;
                    if (frame.Covered[p]) continue;
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

        private sealed class Frame
        {
            public int W, H;
            public float Px, Py, Pz;
            public float Fx, Fy, Fz, Rx, Ry, Rz, Ux, Uy, Uz;
            public float Focal, Near, HazeDistance;
            public float SunX, SunY, SunZ;
            public byte[] Albedo;
            public int AlbedoSize;
            public float[] Zbuffer;
            public float[] Color;
            public bool[] Covered;
        }

        /// <summary>
        /// 三角形の集まりを描く。normals が null なら光を計算せず、貼る画像の色のまま（陰影は画像に焼き込み済みのもの）。
        /// sun・sky は頂点ごとの「日が当たるか」「真上が空か」（null なら全部 1）。
        /// 3 頂点とも hole の範囲（X・Z）に入る三角形は描かない。
        /// </summary>
        private static void Draw(Frame frame, float[] positions, int[] indices, float[] normals, float[] uv, float[] sun, float[] sky,
            float holeMinX, float holeMaxX, float holeMinZ, float holeMaxZ)
        {
            int w = frame.W, h = frame.H;
            int vertexCount = positions.Length / 3;
            var sx = new float[vertexCount];
            var sy = new float[vertexCount];
            var depth = new float[vertexCount];
            Parallel.For(0, vertexCount, v =>
            {
                float dx = positions[v * 3] - frame.Px;
                float dy = positions[v * 3 + 1] - frame.Py;
                float dz = positions[v * 3 + 2] - frame.Pz;
                float cz = dx * frame.Fx + dy * frame.Fy + dz * frame.Fz;
                depth[v] = cz;
                if (cz < frame.Near) { sx[v] = float.NaN; return; }
                sx[v] = w * 0.5f + (dx * frame.Rx + dy * frame.Ry + dz * frame.Rz) / cz * frame.Focal;
                sy[v] = h * 0.5f - (dx * frame.Ux + dy * frame.Uy + dz * frame.Uz) / cz * frame.Focal;
            });

            float[] zbuffer = frame.Zbuffer;
            float[] color = frame.Color;
            bool[] covered = frame.Covered;
            byte[] albedo = frame.Albedo;
            int n = frame.AlbedoSize;
            bool hole = holeMinX < holeMaxX;

            for (int t = 0; t < indices.Length; t += 3)
            {
                int ia = indices[t], ib = indices[t + 1], ic = indices[t + 2];
                float ax = sx[ia], bx = sx[ib], cx = sx[ic];
                if (float.IsNaN(ax) || float.IsNaN(bx) || float.IsNaN(cx)) continue;
                if (hole && Inside(positions, ia, holeMinX, holeMaxX, holeMinZ, holeMaxZ)
                    && Inside(positions, ib, holeMinX, holeMaxX, holeMinZ, holeMaxZ)
                    && Inside(positions, ic, holeMinX, holeMaxX, holeMinZ, holeMaxZ)) continue;
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
                float ua = uv[ia * 2] * wa, ub = uv[ib * 2] * wb, uc = uv[ic * 2] * wc;
                float va = uv[ia * 2 + 1] * wa, vb = uv[ib * 2 + 1] * wb, vc = uv[ic * 2 + 1] * wc;

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
                            // 40 度より急な面から岩の色を混ぜ、58 度で岩だけにする。岩には、高さに沿った地層の縞を薄く入れる
                            float rock = (0.766f - ny) / 0.236f;
                            if (rock > 0f)
                            {
                                if (rock > 1f) rock = 1f;
                                float worldY = l0 * positions[ia * 3 + 1] + l1 * positions[ib * 3 + 1] + l2 * positions[ic * 3 + 1];
                                float band = 0.9f + 0.1f * (float)Math.Sin(worldY * 0.55f + 1.3f * Math.Sin(worldY * 0.13f));
                                r += (150f * band - r) * rock;
                                g += (141f * band - g) * rock;
                                b += (128f * band - b) * rock;
                            }
                            float direct = 1f;
                            float open = 1f;
                            if (sun != null)
                            {
                                open = l0 * sky[ia] + l1 * sky[ib] + l2 * sky[ic];
                                // 「近くの岩の影」は、岩の下でだけ使う（開けた崖の面は周りの地形と同じ明るさにする）
                                direct = Math.Max(open, l0 * sun[ia] + l1 * sun[ib] + l2 * sun[ic]);
                            }
                            float facing = nx * frame.SunX + ny * frame.SunY + nz * frame.SunZ;
                            // 岩の下（張り出しの下・洞窟の中）は、空からの光も減らす
                            float light = 0.38f * (0.35f + 0.65f * open) + 1.05f * (facing > 0f ? facing : 0f) * direct;
                            r *= light; g *= light; b *= light;
                        }
                        // 遠くを霞ませる
                        float haze = 1f - (float)Math.Exp(-z / frame.HazeDistance);
                        haze *= 0.75f;
                        color[p * 3] = r + (176f - r) * haze;
                        color[p * 3 + 1] = g + (196f - g) * haze;
                        color[p * 3 + 2] = b + (216f - b) * haze;
                    }
                }
            }
        }

        private static bool Inside(float[] positions, int vertex, float minX, float maxX, float minZ, float maxZ)
        {
            float x = positions[vertex * 3];
            float z = positions[vertex * 3 + 2];
            return x > minX && x < maxX && z > minZ && z < maxZ;
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
