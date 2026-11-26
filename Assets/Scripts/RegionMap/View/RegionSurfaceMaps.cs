using System;
using System.IO;
using UnityEngine;

namespace Vastcore.RegionMap
{
    /// <summary>砂岩の台地の表面の役（docs/02_design/REGION_SURFACE_MATERIALS_SPEC.md 3.1 節）。並びはシェーダーの番号と同じ。</summary>
    public enum RegionSurfaceRole
    {
        /// <summary>崖の岩肌（帽子の砂岩）</summary>
        Cliff,
        /// <summary>泥岩の帯</summary>
        Mudstone,
        /// <summary>岩屑</summary>
        Talus,
        /// <summary>砂と土</summary>
        Sand,
        /// <summary>地被（生物土壌クラスト）</summary>
        Crust,
        /// <summary>転石</summary>
        LooseRock,
    }

    /// <summary>
    /// 表面の写真素材の置き場所と、取り込み時に作る 3 枚（細部・法線・高さ粗さAO）の計算。
    /// <c>Assets/Resources/RegionMap3D/Surfaces/&lt;役&gt;/</c> に素材の画像を置くと、取り込み時の処理（Editor の RegionSurfaceImport）が
    /// 同じフォルダへ 3 枚を作り、場面を作る時に RegionSurfaceMaterials が配列へ束ねる。置かれていない役は今の手続きの色で描く。
    /// 計算は画素の配列だけで行う（行 0 が画像の下。Unity の GetPixels と同じ並び）。
    /// </summary>
    public static class RegionSurfaceMaps
    {
        /// <summary>Resources の中の置き場所。</summary>
        public const string Root = "RegionMap3D/Surfaces";

        /// <summary>取り込み時に作る画像の名前（拡張子なし）。</summary>
        public const string DetailName = "surface-detail", NormalName = "surface-normal", HraName = "surface-hra";

        /// <summary>作る画像の一辺の画素数。配列は 1 本の中で同じ大きさが要るので揃える。</summary>
        public const int Texels = 1024;

        /// <summary>役ごとのフォルダの名前（RegionSurfaceRole の並び）。</summary>
        public static readonly string[] Folders = { "Cliff", "Mudstone", "Talus", "Sand", "Crust", "LooseRock" };

        /// <summary>素材のファイルが何の画像か。</summary>
        public enum MapKind
        {
            /// <summary>使わない（見本の画像など）</summary>
            None,
            /// <summary>取り込み時に作った画像</summary>
            Generated,
            Color,
            /// <summary>法線（OpenGL 向き、緑が上）</summary>
            NormalGL,
            /// <summary>法線（DirectX 向き、緑が下）</summary>
            NormalDX,
            /// <summary>高さ（Displacement）</summary>
            Height,
            Roughness,
            Occlusion,
            /// <summary>赤 AO・緑 粗さ・青 金属（Poly Haven の arm）</summary>
            Arm,
        }

        /// <summary>
        /// ファイル名から画像の種類を決める。ambientCG（Ground054_1K-JPG_NormalGL.jpg）と Poly Haven（red_sandstone_wall_nor_gl_2k.jpg）の名付けを読む。
        /// 読める形式は PNG と JPG（取り込み時に元のファイルを読み直すため）。
        /// </summary>
        public static MapKind Classify(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (extension != ".png" && extension != ".jpg" && extension != ".jpeg") return MapKind.None;
            string name = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant();
            if (name == DetailName || name == NormalName || name == HraName) return MapKind.Generated;
            string[] tokens = name.Split('_', '-', ' ', '.');
            if (Has(tokens, "normaldx") || name.Contains("nor_dx") || name.Contains("normal_dx")) return MapKind.NormalDX;
            if (Has(tokens, "normalgl", "normal", "nor", "norgl")) return MapKind.NormalGL;
            if (Has(tokens, "displacement", "disp", "height", "bump")) return MapKind.Height;
            if (Has(tokens, "roughness", "rough")) return MapKind.Roughness;
            if (Has(tokens, "ambientocclusion", "ao", "occlusion")) return MapKind.Occlusion;
            if (Has(tokens, "arm")) return MapKind.Arm;
            if (Has(tokens, "color", "colour", "diff", "diffuse", "albedo", "basecolor", "col")) return MapKind.Color;
            return MapKind.None;
        }

        private static bool Has(string[] tokens, params string[] keys)
        {
            foreach (string token in tokens)
                if (Array.IndexOf(keys, token) >= 0) return true;
            return false;
        }

        /// <summary>
        /// 画像を size×size の正方形にする。縦横の比が整数倍なら短い方を繰り返して並べ（写っている物の縦横の比を保つ）、
        /// それ以外は引き伸ばす。縮める時は画素の平均、それ以外は繰り返しを前提にした線形補間。
        /// </summary>
        public static Color[] ToSquare(Color[] pixels, int width, int height, int size)
        {
            int side = Math.Max(width, height);
            bool tiles = side % width == 0 && side % height == 0;
            var square = new Color[side * side];
            for (int y = 0; y < side; y++)
            for (int x = 0; x < side; x++)
            {
                int sx = tiles ? x % width : x * width / side;
                int sy = tiles ? y % height : y * height / side;
                square[y * side + x] = pixels[sy * width + sx];
            }
            if (side == size) return square;
            var result = new Color[size * size];
            if (side > size && side % size == 0)
            {
                int f = side / size;
                float inv = 1f / (f * f);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Color sum = Color.clear;
                    for (int j = 0; j < f; j++)
                    for (int i = 0; i < f; i++)
                        sum += square[(y * f + j) * side + x * f + i];
                    result[y * size + x] = sum * inv;
                }
                return result;
            }
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) * side / size - 0.5f, v = (y + 0.5f) * side / size - 0.5f;
                int x0 = Mathf.FloorToInt(u), y0 = Mathf.FloorToInt(v);
                float fx = u - x0, fy = v - y0;
                Color a = square[Wrap(y0, side) * side + Wrap(x0, side)], b = square[Wrap(y0, side) * side + Wrap(x0 + 1, side)];
                Color c = square[Wrap(y0 + 1, side) * side + Wrap(x0, side)], d = square[Wrap(y0 + 1, side) * side + Wrap(x0 + 1, side)];
                result[y * size + x] = Color.Lerp(Color.Lerp(a, b, fx), Color.Lerp(c, d, fx), fy);
            }
            return result;
        }

        private static int Wrap(int i, int n) => ((i % n) + n) % n;

        /// <summary>
        /// 色の画像から、明るさと色のむらを「平均に対する比」で取り出す（平均が 0.5、2 倍以上の明るさは 1 で止める）。
        /// 色相は場所の模様（今の手続きの地層の色）に任せ、写真からは粒・礫の明暗と色のむらだけを借りる（設計 3.1 節）。
        /// 値は表示の色（sRGB）のまま比を取る。シェーダーが表示の色の上で掛けるため。
        /// </summary>
        public static Color[] Detail(Color[] color)
        {
            double r = 0, g = 0, b = 0;
            foreach (Color c in color) { r += c.r; g += c.g; b += c.b; }
            float n = color.Length;
            var mean = new Vector3((float)(r / n), (float)(g / n), (float)(b / n));
            var result = new Color[color.Length];
            for (int i = 0; i < color.Length; i++)
            {
                Color c = color[i];
                result[i] = new Color(Mathf.Clamp01(0.5f * c.r / Mathf.Max(mean.x, 1e-3f)),
                    Mathf.Clamp01(0.5f * c.g / Mathf.Max(mean.y, 1e-3f)), Mathf.Clamp01(0.5f * c.b / Mathf.Max(mean.z, 1e-3f)), 1f);
            }
            return result;
        }

        /// <summary>
        /// 色の画像から高さを推定する（m、平均 0）。明るい所を高いとみなし、実寸 blurMetres のぼかしとの差で写真の大きな明暗を除き、
        /// 標準偏差が heightMetres / 4 になるよう揃える。影と凹みを取り違えるので、高さか法線の画像がある素材はそちらを使う。
        /// </summary>
        public static float[] HeightFromColor(Color[] color, int size, float metresPerPixel, float heightMetres, float blurMetres = 0.5f)
        {
            var luma = new float[color.Length];
            for (int i = 0; i < luma.Length; i++) luma[i] = 0.2126f * color[i].r + 0.7152f * color[i].g + 0.0722f * color[i].b;
            // 画素ごとの細かな色の粒は高さではないので、先に 2 画素ほどぼかす
            float[] fine = BoxBlur(luma, size, 2);
            float[] broad = BoxBlur(luma, size, Mathf.Max(1, Mathf.RoundToInt(blurMetres / metresPerPixel)));
            double sum = 0;
            for (int i = 0; i < fine.Length; i++) { fine[i] -= broad[i]; sum += fine[i] * (double)fine[i]; }
            float scale = 0.25f * heightMetres / Mathf.Max(1e-5f, (float)Math.Sqrt(sum / fine.Length));
            for (int i = 0; i < fine.Length; i++) fine[i] = Mathf.Clamp(fine[i] * scale, -0.5f * heightMetres, 0.5f * heightMetres);
            return fine;
        }

        /// <summary>高さの画像（0〜1、0.5 が平均の面）を m にする。heightMetres が 0 から 1 までの幅。</summary>
        public static float[] HeightFromDisplacement(Color[] displacement, float heightMetres)
        {
            var result = new float[displacement.Length];
            for (int i = 0; i < result.Length; i++) result[i] = (displacement[i].r - 0.5f) * heightMetres;
            return result;
        }

        /// <summary>
        /// 繰り返す箱のぼかし（半径 radius 画素）。縦と横に分けて、累積和で行う。
        /// </summary>
        public static float[] BoxBlur(float[] values, int size, int radius)
        {
            var temp = new float[values.Length];
            var result = new float[values.Length];
            BlurLines(values, temp, size, radius, true);
            BlurLines(temp, result, size, radius, false);
            return result;
        }

        private static void BlurLines(float[] source, float[] target, int size, int radius, bool rows)
        {
            radius = Math.Min(radius, size / 2 - 1);
            float inv = 1f / (2 * radius + 1);
            for (int line = 0; line < size; line++)
            {
                float sum = 0f;
                for (int k = -radius; k <= radius; k++) sum += source[Index(line, Wrap(k, size), size, rows)];
                for (int i = 0; i < size; i++)
                {
                    target[Index(line, i, size, rows)] = sum * inv;
                    sum += source[Index(line, Wrap(i + radius + 1, size), size, rows)] - source[Index(line, Wrap(i - radius, size), size, rows)];
                }
            }
        }

        private static int Index(int line, int i, int size, bool rows) => rows ? line * size + i : i * size + line;

        /// <summary>
        /// 高さ（m）から傾き（m/m、x は画像の右、y は画像の上へ高くなる向き）を出す。半径 2・4・8 画素の差を重ねる
        /// （1 画素の差は写真の粒の揺れを拾いすぎる）。
        /// </summary>
        public static Vector2[] Slopes(float[] height, int size, float metresPerPixel)
        {
            var result = new Vector2[height.Length];
            int[] radii = { 2, 4, 8 };
            float[] weights = { 0.25f, 0.35f, 0.40f };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                Vector2 s = Vector2.zero;
                for (int k = 0; k < radii.Length; k++)
                {
                    int r = radii[k];
                    float span = 2f * r * metresPerPixel;
                    s.x += weights[k] * (height[y * size + Wrap(x + r, size)] - height[y * size + Wrap(x - r, size)]) / span;
                    s.y += weights[k] * (height[Wrap(y + r, size) * size + x] - height[Wrap(y - r, size) * size + x]) / span;
                }
                result[y * size + x] = s;
            }
            return result;
        }

        /// <summary>法線の画像（0〜1 の色）から傾きを出す。DirectX 向きは緑を反転して読む。</summary>
        public static Vector2[] SlopesFromNormals(Color[] normal, bool directX)
        {
            var result = new Vector2[normal.Length];
            for (int i = 0; i < normal.Length; i++)
            {
                float nx = normal[i].r * 2f - 1f, ny = normal[i].g * 2f - 1f, nz = normal[i].b * 2f - 1f;
                if (directX) ny = -ny;
                nz = Mathf.Max(nz, 0.2f);
                result[i] = new Vector2(-nx / nz, -ny / nz);
            }
            return result;
        }

        /// <summary>傾きを法線の画像（赤 x・緑 y、0.5 が平ら。OpenGL 向き）にする。傾きは 2 で止める。</summary>
        public static Color[] EncodeNormals(Vector2[] slopes)
        {
            var result = new Color[slopes.Length];
            for (int i = 0; i < slopes.Length; i++)
            {
                Vector2 s = Vector2.ClampMagnitude(slopes[i], 2f);
                Vector3 n = new Vector3(-s.x, -s.y, 1f).normalized;
                result[i] = new Color(0.5f + 0.5f * n.x, 0.5f + 0.5f * n.y, 0.5f + 0.5f * n.z, 1f);
            }
            return result;
        }

        /// <summary>
        /// 高さ・粗さ・AO を 1 枚にする（赤 高さ 0〜1 で 0.5 が平均の面、緑 粗さ、青 AO）。
        /// 粗さと AO が無い時は roughness と 1（遮りなし）を使う。
        /// </summary>
        public static Color[] PackHeightRoughnessOcclusion(float[] height, float heightMetres, Color[] roughness, Color[] occlusion, float defaultRoughness)
        {
            var result = new Color[height.Length];
            float inv = 1f / Mathf.Max(heightMetres, 1e-4f);
            for (int i = 0; i < height.Length; i++)
                result[i] = new Color(Mathf.Clamp01(0.5f + height[i] * inv), roughness != null ? roughness[i].r : defaultRoughness,
                    occlusion != null ? occlusion[i].r : 1f, 1f);
            return result;
        }
    }
}
