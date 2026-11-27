using System;
using System.Collections.Generic;
using System.Globalization;

namespace Vastcore.TexturePipeline
{
    /// <summary>
    /// 画素を読む品質の検査。入力は 0〜1 の値の配列で、行は下から上（Unity の GetPixels と同じ並び）。
    /// しきい値は TEXTURE_PIPELINE_SPEC.md 4.2節（開発担当の初期値。実物で見直す）。
    /// </summary>
    public static class TextureQuality
    {
        /// <summary>継ぎ目の比がこれを超えると、繰り返した時に継ぎ目が見える。</summary>
        public const double SeamLimit = 1.6;
        /// <summary>真っ黒・真っ白の画素の割合の上限。</summary>
        public const double ClipLimit = 0.01;
        /// <summary>色の画像の明るさ（sRGB 0〜255）の 1% 点の下限と 99% 点の上限（DS-011 3.2節: 暗部は 30〜40 を下回らない）。</summary>
        public const double AlbedoLow = 30, AlbedoHigh = 243;
        /// <summary>8×8 の区画の平均の明るさの変動係数の上限（写真の照明の焼き込み・大きなむら）。</summary>
        public const double LightingLimit = 0.12;
        /// <summary>法線の向きの判定: GL と DX の回転の比がこれより偏れば向きを決める。</summary>
        public const double ConventionMargin = 1.25;

        /// <summary>
        /// 繰り返しの継ぎ目の比。端どうし（右端と左端、上端と下端）の差の平均 ÷ 中の隣り合う列（行）の差の平均の 99% 点。横と縦の大きい方。
        /// 平均ではなく 99% 点で割るのは、目地や割れ目の線がちょうど端に来る素材（舗石・煉瓦）を継ぎ目と取り違えないため。
        /// 継ぎ目の無い素材は 1 前後、継ぎ目の見える写真は 1.6 以上（ambientCG の 1K 15 枚と、それを切り出した 15 枚で確かめた値。仕様 4.2節）。
        /// </summary>
        public static double SeamRatio(float[] value, int width, int height)
        {
            if (width < 4 || height < 4) return 1;
            var columns = new double[width - 1];
            double wrap = 0;
            for (int y = 0; y < height; y++)
            {
                int row = y * width;
                for (int x = 0; x < width - 1; x++) columns[x] += Math.Abs(value[row + x + 1] - value[row + x]);
                wrap += Math.Abs(value[row] - value[row + width - 1]);
            }
            double ratioX = wrap / Math.Max(1e-9, Percentile(columns, 0.99));
            var rows = new double[height - 1];
            wrap = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height - 1; y++) rows[y] += Math.Abs(value[(y + 1) * width + x] - value[y * width + x]);
                wrap += Math.Abs(value[x] - value[(height - 1) * width + x]);
            }
            double ratioY = wrap / Math.Max(1e-9, Percentile(rows, 0.99));
            return Math.Max(ratioX, ratioY);
        }

        private static double Percentile(double[] values, double q)
        {
            var sorted = (double[])values.Clone();
            Array.Sort(sorted);
            int index = Math.Min(sorted.Length - 1, (int)Math.Ceiling(q * sorted.Length) - 1);
            return sorted[Math.Max(0, index)];
        }

        /// <summary>真っ黒（1/255 以下）と真っ白（254/255 以上）の画素の割合。</summary>
        public static double ClippedFraction(float[] value)
        {
            int clipped = 0;
            foreach (float v in value)
                if (v <= 1f / 255f || v >= 254f / 255f) clipped++;
            return value.Length == 0 ? 0 : clipped / (double)value.Length;
        }

        /// <summary>値の分位点（0〜1 の q）。</summary>
        public static double Percentile(float[] value, double q)
        {
            if (value.Length == 0) return 0;
            var histogram = new int[256];
            foreach (float v in value) histogram[Math.Max(0, Math.Min(255, (int)Math.Round(v * 255)))]++;
            long target = (long)Math.Ceiling(q * value.Length);
            long seen = 0;
            for (int i = 0; i < 256; i++)
            {
                seen += histogram[i];
                if (seen >= target) return i;
            }
            return 255;
        }

        /// <summary>画像を 8×8 の区画に分けた平均の明るさの、変動係数（標準偏差 ÷ 平均）。</summary>
        public static double LightingVariation(float[] value, int width, int height)
        {
            const int blocks = 8;
            if (width < blocks || height < blocks) return 0;
            var sums = new double[blocks * blocks];
            var counts = new int[blocks * blocks];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int b = (y * blocks / height) * blocks + x * blocks / width;
                sums[b] += value[y * width + x];
                counts[b]++;
            }
            double mean = 0;
            for (int i = 0; i < sums.Length; i++) mean += sums[i] /= Math.Max(1, counts[i]);
            mean /= sums.Length;
            double variance = 0;
            foreach (double s in sums) variance += (s - mean) * (s - mean);
            variance /= sums.Length;
            return mean <= 1e-6 ? 0 : Math.Sqrt(variance) / mean;
        }

        /// <summary>
        /// 法線の画像の向き（緑が上の GL か、下の DX か）を中身から推定する。
        /// 高さの場から作った法線の傾きは回転（curl）を持たない。緑の向きを取り違えると回転が出る。
        /// 両方の向きで回転の大きさを測り、小さい方を返す。差が小さければ "unknown"。
        /// </summary>
        public static string NormalConvention(float[] red, float[] green, float[] blue, int width, int height, out double ratio)
        {
            ratio = 1;
            if (width < 8 || height < 8) return "unknown";
            int n = width * height;
            var gx = new double[n];
            var gy = new double[n];
            for (int i = 0; i < n; i++)
            {
                double nx = red[i] * 2 - 1, ny = green[i] * 2 - 1, nz = Math.Max(0.05, blue[i] * 2 - 1);
                gx[i] = -nx / nz;
                gy[i] = -ny / nz;
            }
            double curlGl = 0, curlDx = 0;
            for (int y = 0; y < height; y++)
            {
                int up = ((y + 1) % height) * width, down = ((y - 1 + height) % height) * width, row = y * width;
                for (int x = 0; x < width; x++)
                {
                    int right = (x + 1) % width, left = (x - 1 + width) % width;
                    double dGyDx = (gy[row + right] - gy[row + left]) * 0.5;
                    double dGxDy = (gx[up + x] - gx[down + x]) * 0.5;
                    curlGl += Math.Abs(dGyDx - dGxDy);
                    curlDx += Math.Abs(-dGyDx - dGxDy);
                }
            }
            ratio = curlGl / Math.Max(1e-9, curlDx);
            if (ratio * ConventionMargin < 1) return "gl";
            if (ratio > ConventionMargin) return "dx";
            return "unknown";
        }

        /// <summary>sRGB の値の明るさ（Rec.709 の重み。検査の目安なので Linear へ戻さない）。</summary>
        public static float[] Luminance(float[] red, float[] green, float[] blue)
        {
            var result = new float[red.Length];
            for (int i = 0; i < red.Length; i++) result[i] = 0.2126f * red[i] + 0.7152f * green[i] + 0.0722f * blue[i];
            return result;
        }

        /// <summary>
        /// 1 枚の画像の画素の検査をまとめて行い、指摘を返す。kind は TextureKindRule.name（null は種類不明）、tiling は繰り返して貼る画像か。
        /// </summary>
        public static List<TextureFinding> Check(string path, string kind, bool tiling, float[] red, float[] green, float[] blue, int width, int height)
        {
            var findings = new List<TextureFinding>();
            Action<string, string, string> add = (code, severity, message) =>
                findings.Add(new TextureFinding { code = code, severity = severity, path = path, message = message });
            CultureInfo inv = CultureInfo.InvariantCulture;
            bool normal = kind == TextureKindRule.Normal || kind == TextureKindRule.NormalDX;
            float[] luminance = normal ? green : Luminance(red, green, blue);

            if (tiling)
            {
                double seam = SeamRatio(luminance, width, height);
                if (seam > SeamLimit)
                    add("TQ101", TextureSeverity.Warning, "繰り返しの継ぎ目が見える（端の差が、中の隣り合う列の差の 99% 点の " + seam.ToString("0.00", inv) + " 倍、上限 " + SeamLimit.ToString("0.0", inv)
                        + "）。継ぎ目の無い版（tileable）を選ぶか、端を合わせてから置く");
            }
            if (kind == TextureKindRule.Color)
            {
                double clipped = ClippedFraction(luminance);
                if (clipped > ClipLimit)
                    add("TQ102", TextureSeverity.Warning, "真っ黒・真っ白の画素が " + (clipped * 100).ToString("0.0", inv) + "%（上限 " + (ClipLimit * 100).ToString("0", inv)
                        + "%）。写真の白飛び・黒つぶれが残っている");
                double low = Percentile(luminance, 0.01), high = Percentile(luminance, 0.99);
                if (low < AlbedoLow || high > AlbedoHigh)
                    add("TQ103", TextureSeverity.Warning, "色の明るさの 1%〜99% が sRGB " + low.ToString("0", inv) + "〜" + high.ToString("0", inv) + "（目安 "
                        + AlbedoLow.ToString("0", inv) + "〜" + AlbedoHigh.ToString("0", inv) + "）。影や照りが焼き込まれた色は、光を当てると暗すぎ・明るすぎになる");
                double lighting = LightingVariation(luminance, width, height);
                if (lighting > LightingLimit)
                    add("TQ104", TextureSeverity.Warning, "画像の場所による明るさのむらが " + (lighting * 100).ToString("0", inv) + "%（上限 " + (LightingLimit * 100).ToString("0", inv)
                        + "%）。撮影時の照明の焼き込み。繰り返すと模様に見える");
            }
            if (normal)
            {
                string convention = NormalConvention(red, green, blue, width, height, out double ratio);
                string expected = kind == TextureKindRule.NormalDX ? "dx" : "gl";
                if (convention != "unknown" && convention != expected)
                    add("TQ105", TextureSeverity.Error, "名前は " + expected.ToUpperInvariant() + " 向きだが、中身は " + convention.ToUpperInvariant() + " 向き（回転の比 "
                        + ratio.ToString("0.00", inv) + "）。緑が反転して凹凸が逆に見える");
            }
            return findings;
        }
    }
}
