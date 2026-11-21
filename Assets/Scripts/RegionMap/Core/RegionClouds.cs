using System;
using System.Threading.Tasks;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 雲の層。地図と同じ範囲を 1 枚で覆い、上下左右がつながる（繰り返せる）ので、ずらしながら重ねると流れて見える。
    /// 鳥瞰図の画像へ焼き込む時（RegionMapRenderer）と、探索画面で地図の上に重ねる時（RenderLayers）の両方がここを使う。
    /// </summary>
    public static class RegionClouds
    {
        /// <summary>雲の影が落ちる位置のずれ（地図の幅に対する割合）。光が北西から来るので、影は南東へずれる。</summary>
        public const float ShadowOffset = 0.012f;

        /// <summary>
        /// 雲の濃さ（0〜1）を size × size で作る。cover は量（0 で無し、0.5 前後で空の半分ほど、1 でほぼ全面）。
        /// 大きなまとまり、その中のもこもこした塊とちぎれた縁、高い所の薄い筋雲を重ねる。
        /// </summary>
        public static float[] BuildField(int size, int regionSeed, float cover)
        {
            var field = new float[size * size];
            if (cover <= 0f) return field;
            uint seed = unchecked((uint)regionSeed * 2246822519u + 3266489917u);
            float inv = 1f / size;
            float amount = (cover - 0.5f) * 1.5f;

            Parallel.For(0, size, y =>
            {
                float v = y * inv;
                for (int x = 0; x < size; x++)
                {
                    float u = x * inv;
                    float wx = RegionNoise.FbmWrapped(u * 3f + 5.2f, v * 3f + 1.3f, 3, 3, 3, seed + 1u);
                    float wy = RegionNoise.FbmWrapped(u * 3f + 9.7f, v * 3f + 6.1f, 3, 3, 3, seed + 2u);
                    float cu = u + wx * 0.04f;
                    float cv = v + wy * 0.04f;

                    // 雲が集まる所と晴れ間を分ける大きな模様
                    float system = RegionNoise.FbmWrapped(cu * 2f + 0.37f, cv * 2f + 0.81f, 4, 2, 2, seed + 3u);
                    float bias = system * 1.25f + amount - 0.05f;

                    float puffs = RegionNoise.FbmTurnedWrapped(cu * 15f, cv * 15f, 4, 15, seed + 4u, 0.55f);
                    float billow = 1f - RegionNoise.RidgedWrapped(cu * 31f + 3.3f, cv * 31f + 8.8f, 3, 31, seed + 5u);
                    float density = bias + puffs * 0.9f + (billow - 0.7f) * 0.35f;
                    // 厚い雲の中でも濃淡が残るように、上限へゆっくり近づく形にする
                    float thick = density > 0f ? density / (density + 0.3f) : 0f;

                    // 高い所の薄い雲。風の向き（東西）に長く、細い繊維の束になる。塊の雲より大きくうねらせる
                    float vu = u + wx * 0.07f;
                    float vv = v + wy * 0.07f;
                    float patch = RegionNoise.FbmWrapped(vu * 2f + 4.4f, vv * 3f + 1.9f, 3, 2, 3, seed + 7u);
                    float streak = RegionNoise.FbmWrapped(vu * 4f + 2.1f, vv * 22f + 7.7f, 4, 4, 22, seed + 6u);
                    float fibre = RegionNoise.FbmWrapped(vu * 9f + 6.3f, vv * 80f + 2.9f, 3, 9, 80, seed + 8u);
                    float sheet = RegionNoise.Smooth(0.05f, 0.75f, patch * 1.1f + streak * 0.55f + amount * 0.7f);
                    float veil = sheet * (0.72f + 0.28f * RegionNoise.Smooth(-0.5f, 0.5f, fibre)) * 0.21f;

                    field[x + y * size] = thick > veil ? thick : veil;
                }
            });
            return field;
        }

        /// <summary>雲の濃さを、セルの間をなめらかにつないで読む。範囲の外は反対側へ回り込む。</summary>
        public static float Sample(float[] field, int size, float fx, float fy)
        {
            int x0 = (int)Math.Floor(fx);
            int y0 = (int)Math.Floor(fy);
            float tx = fx - x0;
            float ty = fy - y0;
            x0 %= size;
            if (x0 < 0) x0 += size;
            y0 %= size;
            if (y0 < 0) y0 += size;
            int x1 = x0 + 1 < size ? x0 + 1 : 0;
            int y1 = y0 + 1 < size ? y0 + 1 : 0;
            float a = field[x0 + y0 * size];
            float b = field[x1 + y0 * size];
            float c = field[x0 + y1 * size];
            float d = field[x1 + y1 * size];
            float top = a + (b - a) * tx;
            float bottom = c + (d - c) * tx;
            return top + (bottom - top) * ty;
        }

        /// <summary>その濃さの雲が地表を暗くする割合（0〜0.46）。</summary>
        public static float ShadowStrength(float density)
        {
            return RegionNoise.Smooth(0.03f, 0.6f, density) * 0.46f;
        }

        /// <summary>
        /// 雲の見え方。戻り値は不透明さ（0〜0.93）、tone は明るさ（0〜255 の目安）。
        /// 厚い所ほど白く、薄い縁は地表が透ける。北西の縁が明るく、南東の縁が陰る。rim は縁を見る距離（セル数）。
        /// </summary>
        public static float Shade(float[] field, int size, float fx, float fy, float rim, out float tone)
        {
            float density = Sample(field, size, fx, fy);
            if (density <= 0.01f)
            {
                tone = 232f;
                return 0f;
            }
            float towardSun = Sample(field, size, fx - rim, fy - rim);
            float awayFromSun = Sample(field, size, fx + rim, fy + rim);
            float relief = awayFromSun - towardSun;
            if (relief < -0.25f) relief = -0.25f; else if (relief > 0.25f) relief = 0.25f;
            tone = 214f + 36f * RegionNoise.Smooth(0.1f, 0.7f, density) + 150f * relief;
            return RegionNoise.Smooth(0f, 0.42f, density) * 0.93f;
        }

        /// <summary>
        /// 地図の上に重ねる 2 枚（雲と、雲の影）を RGBA32 で作る。北西の角から行ごとに並ぶ。
        /// 影は雲と同じ位置で作るので、重ねる側で ShadowOffset だけ南東へずらす。
        /// </summary>
        public static void RenderLayers(float[] field, int size, out byte[] clouds, out byte[] shadows)
        {
            var cloudBytes = new byte[size * size * 4];
            var shadowBytes = new byte[size * size * 4];
            float rim = Math.Max(0.75f, size / 400f);
            Parallel.For(0, size, y =>
            {
                for (int x = 0; x < size; x++)
                {
                    float tone;
                    float alpha = Shade(field, size, x, y, rim, out tone);
                    int p = (x + y * size) * 4;
                    cloudBytes[p] = ToByte(tone - 3f);
                    cloudBytes[p + 1] = ToByte(tone);
                    cloudBytes[p + 2] = ToByte(tone + 4f);
                    cloudBytes[p + 3] = ToByte(alpha * 255f);
                    shadowBytes[p] = 0;
                    shadowBytes[p + 1] = 0;
                    shadowBytes[p + 2] = 8;
                    shadowBytes[p + 3] = ToByte(ShadowStrength(field[x + y * size]) * 255f);
                }
            });
            clouds = cloudBytes;
            shadows = shadowBytes;
        }

        /// <summary>濃さが threshold を超えるセルの割合（0〜1）。量の確認に使う。</summary>
        public static float Coverage(float[] field, float threshold)
        {
            int hit = 0;
            for (int i = 0; i < field.Length; i++)
            {
                if (field[i] > threshold) hit++;
            }
            return field.Length > 0 ? hit / (float)field.Length : 0f;
        }

        private static byte ToByte(float v)
        {
            if (v <= 0f) return 0;
            if (v >= 255f) return 255;
            return (byte)(v + 0.5f);
        }
    }
}
