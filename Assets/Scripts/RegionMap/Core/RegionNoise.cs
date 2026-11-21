using System;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// seed で決まる 2D 勾配ノイズ。UnityEngine に依存しないので、Unity とコンソールの両方で同じ値を返す。
    /// </summary>
    public static class RegionNoise
    {
        private const int GradientCount = 256;
        private static readonly float[] GradX = new float[GradientCount];
        private static readonly float[] GradY = new float[GradientCount];

        static RegionNoise()
        {
            for (int i = 0; i < GradientCount; i++)
            {
                double a = (i + 0.5) * (Math.PI * 2.0 / GradientCount);
                GradX[i] = (float)Math.Cos(a);
                GradY[i] = (float)Math.Sin(a);
            }
        }

        public static uint Hash(int x, int y, uint seed)
        {
            unchecked
            {
                uint h = seed ^ 0x9E3779B9u;
                h ^= (uint)x * 0x85EBCA6Bu;
                h = (h << 13) | (h >> 19);
                h *= 0xC2B2AE35u;
                h ^= (uint)y * 0x27D4EB2Fu;
                h = (h << 15) | (h >> 17);
                h *= 0x165667B1u;
                h ^= h >> 16;
                h *= 0x85EBCA6Bu;
                h ^= h >> 13;
                h *= 0xC2B2AE35u;
                h ^= h >> 16;
                return h;
            }
        }

        /// <summary>0 以上 1 未満の一様乱数。</summary>
        public static float Hash01(int x, int y, uint seed)
        {
            return (Hash(x, y, seed) >> 8) * (1f / 16777216f);
        }

        private static int FastFloor(float v)
        {
            int i = (int)v;
            return v < i ? i - 1 : i;
        }

        private static float Corner(int ix, int iy, float dx, float dy, uint seed)
        {
            uint h = Hash(ix, iy, seed) & (GradientCount - 1);
            return GradX[h] * dx + GradY[h] * dy;
        }

        /// <summary>おおよそ -1〜1 の勾配ノイズ。</summary>
        public static float Gradient(float x, float y, uint seed)
        {
            int x0 = FastFloor(x);
            int y0 = FastFloor(y);
            float fx = x - x0;
            float fy = y - y0;
            float u = fx * fx * fx * (fx * (fx * 6f - 15f) + 10f);
            float v = fy * fy * fy * (fy * (fy * 6f - 15f) + 10f);
            float n00 = Corner(x0, y0, fx, fy, seed);
            float n10 = Corner(x0 + 1, y0, fx - 1f, fy, seed);
            float n01 = Corner(x0, y0 + 1, fx, fy - 1f, seed);
            float n11 = Corner(x0 + 1, y0 + 1, fx - 1f, fy - 1f, seed);
            float a = n00 + (n10 - n00) * u;
            float b = n01 + (n11 - n01) * u;
            return (a + (b - a) * v) * 1.4142135f;
        }

        /// <summary>fBm。振幅の合計で割るので、おおよそ -1〜1。</summary>
        public static float Fbm(float x, float y, int octaves, uint seed, float lacunarity = 2f, float gain = 0.5f)
        {
            float sum = 0f;
            float amp = 1f;
            float norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Gradient(x, y, seed + (uint)i * 1013u) * amp;
                norm += amp;
                amp *= gain;
                x = x * lacunarity + 17.13f;
                y = y * lacunarity + 5.71f;
            }
            return sum / norm;
        }

        /// <summary>
        /// 重ねるたびに向きを回す fBm。回さないと、細かい周波数で格子の向きが揃って縞や升目に見える。
        /// 地表や海面の模様のように、細かさがそのまま目に入る用途に使う。
        /// </summary>
        public static float FbmTurned(float x, float y, int octaves, uint seed, float gain = 0.5f)
        {
            float sum = 0f;
            float amp = 1f;
            float norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Gradient(x, y, seed + (uint)i * 1013u) * amp;
                norm += amp;
                amp *= gain;
                float nx = (x * 0.8f - y * 0.6f) * 2.03f + 17.13f;
                y = (x * 0.6f + y * 0.8f) * 2.03f + 5.71f;
                x = nx;
            }
            return sum / norm;
        }

        /// <summary>尾根状のノイズ。0〜1 で、1 が尾根。</summary>
        public static float Ridged(float x, float y, int octaves, uint seed, float lacunarity = 2f, float gain = 0.5f)
        {
            float sum = 0f;
            float amp = 1f;
            float norm = 0f;
            float weight = 1f;
            for (int i = 0; i < octaves; i++)
            {
                float n = 1f - Math.Abs(Gradient(x, y, seed + (uint)i * 2029u));
                n *= n;
                sum += n * amp * weight;
                norm += amp;
                weight = n < 1f ? n : 1f;
                amp *= gain;
                x = x * lacunarity + 9.37f;
                y = y * lacunarity + 23.9f;
            }
            return sum / norm;
        }

        private static int Wrap(int i, int period)
        {
            int m = i % period;
            return m < 0 ? m + period : m;
        }

        /// <summary>
        /// 格子の番号を周期で折り返す勾配ノイズ。x を periodX、y を periodY だけ進めると同じ値に戻るので、
        /// 継ぎ目なく繰り返せる模様（流れる雲など）を作れる。
        /// </summary>
        public static float GradientWrapped(float x, float y, int periodX, int periodY, uint seed)
        {
            int x0 = FastFloor(x);
            int y0 = FastFloor(y);
            float fx = x - x0;
            float fy = y - y0;
            int xa = Wrap(x0, periodX);
            int xb = Wrap(x0 + 1, periodX);
            int ya = Wrap(y0, periodY);
            int yb = Wrap(y0 + 1, periodY);
            float u = fx * fx * fx * (fx * (fx * 6f - 15f) + 10f);
            float v = fy * fy * fy * (fy * (fy * 6f - 15f) + 10f);
            float n00 = Corner(xa, ya, fx, fy, seed);
            float n10 = Corner(xb, ya, fx - 1f, fy, seed);
            float n01 = Corner(xa, yb, fx, fy - 1f, seed);
            float n11 = Corner(xb, yb, fx - 1f, fy - 1f, seed);
            float a = n00 + (n10 - n00) * u;
            float b = n01 + (n11 - n01) * u;
            return (a + (b - a) * v) * 1.4142135f;
        }

        /// <summary>繰り返せる fBm。細かさを 2 倍にするたびに周期も 2 倍にする。大きくゆるやかな模様に使う。</summary>
        public static float FbmWrapped(float x, float y, int octaves, int periodX, int periodY, uint seed, float gain = 0.5f)
        {
            float sum = 0f;
            float amp = 1f;
            float norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += GradientWrapped(x, y, periodX, periodY, seed + (uint)i * 1013u) * amp;
                norm += amp;
                amp *= gain;
                x = x * 2f + 17.13f;
                y = y * 2f + 5.71f;
                periodX *= 2;
                periodY *= 2;
            }
            return sum / norm;
        }

        /// <summary>
        /// 繰り返せて、重ねるたびに向きも回す fBm。回転を整数の行列 (2,-1; 1,2) にすると、格子点が格子点へ移るので
        /// 周期が保たれる（1 回で 26.6 度回り、約 2.24 倍細かくなる）。細かさがそのまま目に入る模様に使う。
        /// </summary>
        public static float FbmTurnedWrapped(float x, float y, int octaves, int period, uint seed, float gain = 0.5f)
        {
            float sum = 0f;
            float amp = 1f;
            float norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += GradientWrapped(x, y, period, period, seed + (uint)i * 1013u) * amp;
                norm += amp;
                amp *= gain;
                float nx = 2f * x - y + 17.13f;
                y = x + 2f * y + 5.71f;
                x = nx;
            }
            return sum / norm;
        }

        /// <summary>繰り返せる尾根状のノイズ（0〜1、1 が尾根）。向きの回し方は FbmTurnedWrapped と同じ。</summary>
        public static float RidgedWrapped(float x, float y, int octaves, int period, uint seed, float gain = 0.5f)
        {
            float sum = 0f;
            float amp = 1f;
            float norm = 0f;
            float weight = 1f;
            for (int i = 0; i < octaves; i++)
            {
                float n = 1f - Math.Abs(GradientWrapped(x, y, period, period, seed + (uint)i * 2029u));
                n *= n;
                sum += n * amp * weight;
                norm += amp;
                weight = n < 1f ? n : 1f;
                amp *= gain;
                float nx = 2f * x - y + 9.37f;
                y = x + 2f * y + 23.9f;
                x = nx;
            }
            return sum / norm;
        }

        public static float Smooth(float edge0, float edge1, float x)
        {
            float t = (x - edge0) / (edge1 - edge0);
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return t * t * (3f - 2f * t);
        }

        public static float Clamp01(float v)
        {
            return v < 0f ? 0f : (v > 1f ? 1f : v);
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }
}
