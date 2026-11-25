using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

namespace Vastcore.RegionMap.Preview
{
    /// <summary>
    /// 砂岩の台地（見え方の想定の第1区切り）の確認。台地が立つ場所の候補の一覧、台地を入れた窓の数値、
    /// 決まった 3 つの視点（遠景・中景・近景）の絵を出す。絵は Unity を起動せずに形を確かめるための簡易描画で、製品の描画ではない。
    /// </summary>
    internal static class LookView
    {
        /// <summary>
        /// 台地が立つ条件（乾き・平ら・内陸）を満たす広域のセルを数え、6 km 四方でいちばん多い場所を上から並べる。
        /// </summary>
        internal static void DescribeMesaSites(RegionData data)
        {
            int n = data.Resolution;
            float cell = data.CellMeters;
            var gate = new float[n * n];
            int total = 0;
            for (int y = 1; y < n - 1; y++)
            {
                for (int x = 1; x < n - 1; x++)
                {
                    int i = x + y * n;
                    float here = data.Height[i];
                    if (here <= 12f) continue;
                    float most = Math.Max(Math.Max(Math.Abs(here - Math.Max(0f, data.Height[i - 1])), Math.Abs(here - Math.Max(0f, data.Height[i + 1]))),
                        Math.Max(Math.Abs(here - Math.Max(0f, data.Height[i - n])), Math.Abs(here - Math.Max(0f, data.Height[i + n]))));
                    float value = RegionNoise.Smooth(0.34f, 0.24f, data.Moisture[i]) * RegionNoise.Smooth(1f, 6f, data.Temperature[i])
                        * RegionNoise.Smooth(0.16f, 0.07f, most / cell);
                    gate[i] = value;
                    if (value > 0.5f) total++;
                }
            }
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "台地が立つ条件（乾き・平ら）を半分以上満たす広域のセル: {0}（陸の {1:P2}）", total, total / (double)(n * n * data.LandFraction)));

            int half = (int)Math.Round(3000f / cell);
            var picked = new List<(float score, int x, int y)>();
            for (int y = half; y < n - half; y += 4)
            {
                for (int x = half; x < n - half; x += 4)
                {
                    float sum = 0f;
                    bool sea = false;
                    for (int dy = -half; dy <= half && !sea; dy += 2)
                    for (int dx = -half; dx <= half; dx += 2)
                    {
                        int j = x + dx + (y + dy) * n;
                        if (data.Height[j] <= 0f) { sea = true; break; }
                        sum += gate[j];
                    }
                    if (sea || sum <= 0f) continue;
                    picked.Add((sum / ((half + 1) * (half + 1)), x, y));
                }
            }
            picked.Sort((a, b) => b.score.CompareTo(a.score));
            var shown = new List<(int x, int y)>();
            foreach (var p in picked)
            {
                bool near = false;
                foreach (var s in shown) if (Math.Abs(s.x - p.x) < half * 2 && Math.Abs(s.y - p.y) < half * 2) near = true;
                if (near) continue;
                shown.Add((p.x, p.y));
                int i = p.x + p.y * n;
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "  候補 X{0:F1} Y{1:F1}  条件を満たす割合 {2:P0}  標高 {3:F0} m  湿り気 {4:F2}  気温 {5:F1}℃  {6}",
                    data.MapXFromCell(i), data.MapYFromCell(i), p.score, data.Height[i], data.Moisture[i], data.Temperature[i], data.PlaceNameAtMap(data.MapXFromCell(i), data.MapYFromCell(i))));
                if (shown.Count >= 8) break;
            }
        }

        /// <summary>台地を入れた窓の内訳（帽子の岩・岩屑の斜面の割合、上面の高さ）。</summary>
        internal static void DescribeMesa(RegionData terrain)
        {
            if (terrain.Caprock == null) return;
            int count = terrain.Caprock.Length;
            int cap = 0, rubble = 0, land = 0;
            float top = float.MinValue, low = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (terrain.Height[i] <= 0f) continue;
                land++;
                if (terrain.Height[i] < low) low = terrain.Height[i];
                if (terrain.Caprock[i] != 0)
                {
                    cap++;
                    if (terrain.Height[i] > top) top = terrain.Height[i];
                }
                if (terrain.Talus[i] > 0.4f) rubble++;
            }
            if (land == 0) return;
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "台地: 帽子の岩={0:P1} 岩屑の斜面={1:P1} 上面の最高={2:F0}m 窓の最低={3:F0}m",
                cap / (double)land, rubble / (double)land, cap > 0 ? top : 0f, low));
        }

        /// <summary>
        /// 場面（RegionLookScene）を作り、視点の絵を書き出す。look は "all"・"far"・"mid"・"near"・"map" をカンマで並べる。
        /// lookAt は試しの視点で、"東,南,地面からの高さ,見る先の東,南,地面からの高さ,画角" を ";" で並べる（東・南は場面の中心からの m）。
        /// center は場面の中心（"地図X,地図Y"）。書かなければ RegionLookSpec の既定。
        /// </summary>
        internal static void Write(RegionData data, string stem, string look, string lookAt, string center)
        {
            var spec = new RegionLookSpec();
            if (center != null)
            {
                string[] c = center.Split(',');
                spec.CenterMapX = Float(c[0]);
                spec.CenterMapY = Float(c[1]);
            }
            var watch = Stopwatch.StartNew();
            RegionLookScene scene = RegionLookScene.Build(data, spec);
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "場面: 中心 X{0:F2} Y{1:F2} 外側 {2:F2} km（1セル {3:F2} m） 内側 {4:F3} km（1セル {5:F3} m） 作成 {6}ms",
                spec.CenterMapX, spec.CenterMapY, scene.Outer.Data.SizeKm, scene.OuterCell, scene.Inner.Data.SizeKm, scene.InnerCell, watch.ElapsedMilliseconds));
            Describe("外側", data, scene.Outer);
            Describe("内側", data, scene.Inner);
            if (scene.Rocks != null)
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "岩: 内側 {0} 個（{1}）・外側 {2} 個 / 頂点 {3} 三角形 {4}",
                    scene.InnerRocks.Rocks.Length, Sizes(scene.InnerRocks), scene.OuterRocks.Rocks.Length, scene.Rocks.VertexCount, scene.Rocks.TriangleCount));
            }

            var views = new List<RegionLookViewpoint>();
            bool map = false;
            foreach (string name in look.Split(','))
            {
                if (name == "all") views.AddRange(spec.Viewpoints());
                else if (name == "far") views.Add(spec.Far);
                else if (name == "mid") views.Add(spec.Mid);
                else if (name == "near") views.Add(spec.Near);
                else if (name == "map") map = true;
            }
            if (lookAt != null)
            {
                int index = 1;
                foreach (string item in lookAt.Split(';'))
                {
                    string[] v = item.Split(',');
                    views.Add(new RegionLookViewpoint("try" + index++, Float(v[0]), Float(v[1]), Float(v[2]), Float(v[3]), Float(v[4]), Float(v[5]),
                        v.Length > 6 ? Float(v[6]) : 40f));
                }
            }

            var plain = new RegionRenderOptions { CloudCover = 0f, Exaggeration = 0f, CastShadow = 0f };
            byte[] outerGround = RegionMapRenderer.Render(scene.Outer.Data, plain);
            byte[] innerGround = RegionMapRenderer.Render(scene.Inner.Data, plain);
            var eye = new float[3];
            var target = new float[3];
            foreach (RegionLookViewpoint view in views)
            {
                watch.Restart();
                scene.Resolve(view, eye, target);
                var frame = new Frame(1600, 900, 2, eye, target, view.FovDegrees);
                frame.Draw(scene.OuterTerrain, outerGround, scene.Outer.Data.Resolution);
                frame.Draw(scene.InnerTerrain, innerGround, scene.Inner.Data.Resolution);
                if (scene.Rocks != null) frame.Draw(scene.Rocks, null, 0);
                string file = stem + "-look-" + view.Name + ".png";
                PngWriter.Write(file, frame.Resolve(), frame.Width, frame.Height);
                float dx = target[0] - eye[0], dy = target[1] - eye[1], dz = target[2] - eye[2];
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "視点 {0}: 目 東{1:F0} 南{2:F0} 標高{3:F0}m（地面から {4:F1} m） → 先 東{5:F0} 南{6:F0} 標高{7:F0}m 距離 {8:F0} m 画角 {9:F0}° 描画 {10}ms → {11}",
                    view.Name, view.EyeEast, view.EyeSouth, eye[1], view.EyeAbove, view.TargetEast, view.TargetSouth, target[1],
                    Math.Sqrt(dx * dx + dy * dy + dz * dz), view.FovDegrees, watch.ElapsedMilliseconds, System.IO.Path.GetFullPath(file)));
            }
            if (map) WriteMaps(scene, stem, views);
        }

        private static void Describe(string name, RegionData region, RegionDetail detail)
        {
            // 同じ窓を、台地を入れずに作って比べる（広域の地図との一致がどれだけ下がるか）
            RegionDetailSpec plainSpec = detail.Spec.Clone();
            plainSpec.Mesa = 0f;
            RegionDetail plain = RegionDetailBuilder.Build(region, plainSpec);
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "{0}: 海陸の一致 {1:P2}→{2:P2} 標高の差の平均 {3:F1}→{4:F1} m 川の一致 {5:P1}→{6:P1}（台地なし→あり）",
                name, plain.CoastAgreement, detail.CoastAgreement, plain.MeanHeightDifference, detail.MeanHeightDifference, plain.RiverAgreement, detail.RiverAgreement));
            Console.Write("  " + name + " ");
            Program.DescribeSlopes(detail.Data);
            Console.Write("  " + name + " ");
            DescribeMesa(detail.Data);
        }

        private static string Sizes(RegionRockField field)
        {
            int small = 0, mid = 0, tall = 0;
            foreach (RegionRock rock in field.Rocks)
            {
                if (rock.Size < 0.8f) small++;
                else if (rock.Size < 2f) mid++;
                else tall++;
            }
            return "0.8 m 未満 " + small + "・2 m 未満 " + mid + "・2 m 以上 " + tall;
        }

        /// <summary>上から見た地図 2 枚（外側の全体と、内側）。視点の位置（丸）と見る向き（線）を重ねる。</summary>
        private static void WriteMaps(RegionLookScene scene, string stem, List<RegionLookViewpoint> views)
        {
            var shaded = new RegionRenderOptions { CloudCover = 0f, Exaggeration = 1f };
            for (int pass = 0; pass < 2; pass++)
            {
                RegionData data = pass == 0 ? scene.Outer.Data : scene.Inner.Data;
                int n = data.Resolution;
                byte[] pixels = RegionMapRenderer.Render(data, shaded);
                float minX = pass == 0 ? 0f : scene.InnerMinX, minZ = pass == 0 ? 0f : scene.InnerMinZ;
                float scale = pass == 0 ? 1f / scene.OuterCell : 1f / scene.InnerCell;
                if (pass == 0)
                {
                    float[] box = { scene.InnerMinX, scene.InnerMinZ, scene.InnerMaxX, scene.InnerMaxZ };
                    Line(pixels, n, box[0] * scale, box[1] * scale, box[2] * scale, box[1] * scale, 255, 255, 255);
                    Line(pixels, n, box[2] * scale, box[1] * scale, box[2] * scale, box[3] * scale, 255, 255, 255);
                    Line(pixels, n, box[2] * scale, box[3] * scale, box[0] * scale, box[3] * scale, 255, 255, 255);
                    Line(pixels, n, box[0] * scale, box[3] * scale, box[0] * scale, box[1] * scale, 255, 255, 255);
                }
                var eye = new float[3];
                var target = new float[3];
                foreach (RegionLookViewpoint view in views)
                {
                    scene.Resolve(view, eye, target);
                    float ex = (eye[0] - minX) * scale, ez = (eye[2] - minZ) * scale;
                    float tx = (target[0] - minX) * scale, tz = (target[2] - minZ) * scale;
                    byte r = (byte)(view.Name == "far" ? 255 : 40), g = (byte)(view.Name == "mid" ? 255 : 60), b = (byte)(view.Name == "near" ? 255 : 40);
                    if (view.Name.StartsWith("try")) { r = 255; g = 0; b = 255; }
                    Line(pixels, n, ex, ez, tx, tz, r, g, b);
                    for (int dy = -4; dy <= 4; dy++)
                    for (int dx = -4; dx <= 4; dx++)
                        if (dx * dx + dy * dy <= 16) Dot(pixels, n, (int)ex + dx, (int)ez + dy, r, g, b);
                }
                string file = stem + (pass == 0 ? "-look-map-outer.png" : "-look-map-inner.png");
                PngWriter.Write(file, pixels, n, n);
                Console.WriteLine("地図 → " + System.IO.Path.GetFullPath(file));
            }
        }

        private static void Line(byte[] pixels, int n, float x0, float y0, float x1, float y1, byte r, byte g, byte b)
        {
            int steps = (int)Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0)) + 1;
            if (steps > 8000) steps = 8000;
            for (int s = 0; s <= steps; s++)
            {
                float t = s / (float)steps;
                Dot(pixels, n, (int)(x0 + (x1 - x0) * t), (int)(y0 + (y1 - y0) * t), r, g, b);
            }
        }

        private static void Dot(byte[] pixels, int n, int x, int y, byte r, byte g, byte b)
        {
            if (x < 0 || y < 0 || x >= n || y >= n) return;
            int o = (x + y * n) * 4;
            pixels[o] = r; pixels[o + 1] = g; pixels[o + 2] = b;
        }

        private static float Float(string value)
        {
            return float.Parse(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 地形と視点の確認用に絵 1 枚を簡易描画する。Unity の砂岩材質は固定視点の view3d 撮影で確認する。
        /// 地面は真上から見た画像、急な面と岩は岩肌の色。明るさ = 空からの光 × 空の開け具合 + 太陽 × 面の向き × 日当たり。
        /// 霞と空の色は入れない（光と空気は次の区切り）。
        /// </summary>
        private sealed class Frame
        {
            public readonly int Width, Height;
            private readonly int _w, _h, _ss;
            private readonly float _px, _py, _pz, _fx, _fy, _fz, _rx, _ry, _rz, _ux, _uy, _uz, _focal;
            private readonly float _sunX, _sunY, _sunZ;
            private readonly float[] _depth;
            private readonly float[] _color;

            public Frame(int width, int height, int supersample, float[] eye, float[] target, float fovDegrees)
            {
                Width = width; Height = height; _ss = supersample;
                _w = width * supersample; _h = height * supersample;
                _px = eye[0]; _py = eye[1]; _pz = eye[2];
                Unit(target[0] - _px, target[1] - _py, target[2] - _pz, out _fx, out _fy, out _fz);
                // 右 = 前 × 上(0,1,0)、上 = 右 × 前
                Unit(-_fz, 0f, _fx, out _rx, out _ry, out _rz);
                _ux = _ry * _fz - _rz * _fy;
                _uy = _rz * _fx - _rx * _fz;
                _uz = _rx * _fy - _ry * _fx;
                _focal = _h * 0.5f / (float)Math.Tan(fovDegrees * 0.5 * Math.PI / 180.0);
                RegionLookScene.SunDirection(out _sunX, out _sunY, out _sunZ);
                _depth = new float[_w * _h];
                for (int i = 0; i < _depth.Length; i++) _depth[i] = float.MaxValue;
                _color = new float[_w * _h * 3];
                for (int i = 0; i < _w * _h; i++)
                {
                    _color[i * 3] = 140f; _color[i * 3 + 1] = 173f; _color[i * 3 + 2] = 219f;
                }
            }

            public void Draw(RegionLookPart part, byte[] albedo, int albedoSize)
            {
                const float Near = 0.3f;
                int count = part.VertexCount;
                float[] p = part.Positions, q = part.Normals;
                var sx = new float[count];
                var sy = new float[count];
                var depth = new float[count];
                var light = new float[count];
                var rock = new float[count];
                Parallel.For(0, count, v =>
                {
                    float dx = p[v * 3] - _px, dy = p[v * 3 + 1] - _py, dz = p[v * 3 + 2] - _pz;
                    float cz = dx * _fx + dy * _fy + dz * _fz;
                    depth[v] = cz;
                    if (cz < Near) { sx[v] = float.NaN; return; }
                    sx[v] = _w * 0.5f + (dx * _rx + dy * _ry + dz * _rz) / cz * _focal;
                    sy[v] = _h * 0.5f - (dx * _ux + dy * _uy + dz * _uz) / cz * _focal;
                    float facing = q[v * 3] * _sunX + q[v * 3 + 1] * _sunY + q[v * 3 + 2] * _sunZ;
                    light[v] = 0.42f * (0.35f + 0.65f * part.Sky[v]) + 1.05f * (facing > 0f ? facing : 0f) * part.Sun[v];
                    // 40 度より急な面から岩の色を混ぜ、58 度で岩だけにする
                    float steep = (0.766f - q[v * 3 + 1]) / 0.236f;
                    if (steep < 0f) steep = 0f; else if (steep > 1f) steep = 1f;
                    rock[v] = Math.Max(steep, part.Rock[v]);
                });

                int[] indices = part.Indices;
                float[] uv = part.Uv;
                for (int t = 0; t < indices.Length; t += 3)
                {
                    int ia = indices[t], ib = indices[t + 1], ic = indices[t + 2];
                    float ax = sx[ia], bx = sx[ib], cx = sx[ic];
                    if (float.IsNaN(ax) || float.IsNaN(bx) || float.IsNaN(cx)) continue;
                    float ay = sy[ia], by = sy[ib], cy = sy[ic];
                    int minX = Math.Max(0, (int)Math.Floor(Math.Min(ax, Math.Min(bx, cx))));
                    int maxX = Math.Min(_w - 1, (int)Math.Ceiling(Math.Max(ax, Math.Max(bx, cx))));
                    int minY = Math.Max(0, (int)Math.Floor(Math.Min(ay, Math.Min(by, cy))));
                    int maxY = Math.Min(_h - 1, (int)Math.Ceiling(Math.Max(ay, Math.Max(by, cy))));
                    if (minX > maxX || minY > maxY) continue;
                    float area = (bx - ax) * (cy - ay) - (cx - ax) * (by - ay);
                    if (Math.Abs(area) < 1e-6f) continue;
                    float invArea = 1f / area;
                    float wa = 1f / depth[ia], wb = 1f / depth[ib], wc = 1f / depth[ic];
                    for (int y = minY; y <= maxY; y++)
                    {
                        for (int x = minX; x <= maxX; x++)
                        {
                            float qx = x + 0.5f - ax, qy = y + 0.5f - ay;
                            float l1 = (qx * (cy - ay) - (cx - ax) * qy) * invArea;
                            float l2 = ((bx - ax) * qy - qx * (by - ay)) * invArea;
                            float l0 = 1f - l1 - l2;
                            if (l0 < 0f || l1 < 0f || l2 < 0f) continue;
                            float k0 = l0 * wa, k1 = l1 * wb, k2 = l2 * wc;
                            float z = 1f / (k0 + k1 + k2);
                            int o = x + y * _w;
                            if (z >= _depth[o]) continue;
                            _depth[o] = z;
                            k0 *= z; k1 *= z; k2 *= z;
                            float amount = k0 * rock[ia] + k1 * rock[ib] + k2 * rock[ic];
                            float r = 0f, g = 0f, b = 0f;
                            if (albedo != null)
                            {
                                Texel(albedo, albedoSize, k0 * uv[ia * 2] + k1 * uv[ib * 2] + k2 * uv[ic * 2],
                                    k0 * uv[ia * 2 + 1] + k1 * uv[ib * 2 + 1] + k2 * uv[ic * 2 + 1], out r, out g, out b);
                            }
                            if (amount > 0f)
                            {
                                float worldY = k0 * p[ia * 3 + 1] + k1 * p[ib * 3 + 1] + k2 * p[ic * 3 + 1];
                                float band = 1f - RegionLookScene.BandAmount + RegionLookScene.BandAmount * (float)Math.Sin(worldY * 0.55f + 1.3f * Math.Sin(worldY * 0.13f));
                                band *= 0.66f + 0.34f * (k0 * part.Tint[ia] + k1 * part.Tint[ib] + k2 * part.Tint[ic]);
                                r += (255f * RegionLookScene.RockRed * band - r) * amount;
                                g += (255f * RegionLookScene.RockGreen * band - g) * amount;
                                b += (255f * RegionLookScene.RockBlue * band - b) * amount;
                            }
                            float shade = k0 * light[ia] + k1 * light[ib] + k2 * light[ic];
                            _color[o * 3] = r * shade;
                            _color[o * 3 + 1] = g * shade;
                            _color[o * 3 + 2] = b * shade;
                        }
                    }
                }
            }

            public byte[] Resolve()
            {
                var output = new byte[Width * Height * 4];
                float norm = 1f / (_ss * _ss);
                Parallel.For(0, Height, y =>
                {
                    for (int x = 0; x < Width; x++)
                    {
                        float r = 0f, g = 0f, b = 0f;
                        for (int j = 0; j < _ss; j++)
                        for (int i = 0; i < _ss; i++)
                        {
                            int o = (x * _ss + i + (y * _ss + j) * _w) * 3;
                            r += _color[o]; g += _color[o + 1]; b += _color[o + 2];
                        }
                        int at = (x + y * Width) * 4;
                        output[at] = (byte)Math.Min(255f, r * norm + 0.5f);
                        output[at + 1] = (byte)Math.Min(255f, g * norm + 0.5f);
                        output[at + 2] = (byte)Math.Min(255f, b * norm + 0.5f);
                        output[at + 3] = 255;
                    }
                });
                return output;
            }

            private static void Texel(byte[] rgba, int n, float u, float v, out float r, out float g, out float b)
            {
                float fx = Math.Max(0f, Math.Min(n - 1.001f, u * (n - 1)));
                float fy = Math.Max(0f, Math.Min(n - 1.001f, v * (n - 1)));
                int x0 = (int)fx, y0 = (int)fy;
                float tx = fx - x0, ty = fy - y0;
                int a = (x0 + y0 * n) * 4, c = a + n * 4;
                r = (rgba[a] + (rgba[a + 4] - rgba[a]) * tx) * (1f - ty) + (rgba[c] + (rgba[c + 4] - rgba[c]) * tx) * ty;
                g = (rgba[a + 1] + (rgba[a + 5] - rgba[a + 1]) * tx) * (1f - ty) + (rgba[c + 1] + (rgba[c + 5] - rgba[c + 1]) * tx) * ty;
                b = (rgba[a + 2] + (rgba[a + 6] - rgba[a + 2]) * tx) * (1f - ty) + (rgba[c + 2] + (rgba[c + 6] - rgba[c + 2]) * tx) * ty;
            }

            private static void Unit(float x, float y, float z, out float ox, out float oy, out float oz)
            {
                float length = (float)Math.Sqrt(x * x + y * y + z * z);
                if (length < 1e-9f) length = 1f;
                ox = x / length; oy = y / length; oz = z / length;
            }
        }
    }
}
