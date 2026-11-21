using System;

namespace Vastcore.RegionMap
{
    /// <summary>鳥瞰図の描き方。同じ地域データから見た目だけを変える。</summary>
    public enum RegionMapStyle
    {
        /// <summary>衛星写真のような自然色。雲がかかる。</summary>
        Satellite = 0,

        /// <summary>地図帳のような標高別の色分けと等高線。</summary>
        Atlas = 1,

        /// <summary>色を抑え、起伏の陰影だけで見せる。</summary>
        Relief = 2,
    }

    [Serializable]
    public sealed class RegionRenderOptions
    {
        public RegionMapStyle Style = RegionMapStyle.Satellite;

        /// <summary>雲の量（0 で無し、1 で多い）。</summary>
        public float CloudCover = 0.35f;

        /// <summary>起伏の強調。大きいほど陰影が強い。</summary>
        public float Exaggeration = 2.6f;

        /// <summary>山が落とす影の濃さ（0〜1）。</summary>
        public float CastShadow = 0.3f;

        /// <summary>太陽の高さ（度）。光は北西から当たる。</summary>
        public float SunAltitude = 38f;

        public bool Rivers = true;
    }

    /// <summary>
    /// 地域データを真上から見た画像にする。出力は RGBA32 で、北西の角から行ごとに並ぶ。
    /// 光源は地図の慣例に合わせて北西（方位 315 度）に置く。
    /// </summary>
    public static class RegionMapRenderer
    {
        // 気温（行: -8, 5, 17, 30 ℃）と湿り気（列: 0, 1/3, 2/3, 1）から地表の色を引く表
        private static readonly float[] ClimateColors =
        {
            150, 143, 132,   140, 138, 122,   122, 130, 112,   114, 126, 110,
            152, 141, 106,   120, 128,  84,    64,  90,  62,    50,  80,  58,
            188, 166, 120,   138, 144,  82,    72, 106,  58,    48,  92,  54,
            216, 190, 140,   172, 160,  94,    86, 120,  58,    40,  94,  48,
        };

        public static byte[] Render(RegionData data, RegionRenderOptions options)
        {
            if (options == null) options = new RegionRenderOptions();
            int n = data.Resolution;
            int count = n * n;
            float[] h = data.Height;
            float cell = data.CellMeters;
            uint seed = unchecked((uint)data.Spec.Seed * 362437u + 521288629u);
            var pixels = new byte[count * 4];
            float inv = 1f / (n - 1);
            float riverThreshold = RegionGenerator.RiverThreshold(data);

            float altitude = options.SunAltitude * (float)Math.PI / 180f;
            float lz = (float)Math.Sin(altitude);
            float horizontal = (float)Math.Cos(altitude) * 0.70710678f;
            float lx = -horizontal;
            float ly = -horizontal;

            float[] shadow = options.CastShadow > 0f ? BuildShadow(data, options, altitude) : null;
            bool clouds = options.Style == RegionMapStyle.Satellite && options.CloudCover > 0f;
            float cloudOffset = 0.014f;

            for (int y = 0; y < n; y++)
            {
                float v = y * inv;
                int yu = y > 0 ? y - 1 : y;
                int yd = y < n - 1 ? y + 1 : y;
                for (int x = 0; x < n; x++)
                {
                    float u = x * inv;
                    int i = x + y * n;
                    int xl = x > 0 ? x - 1 : x;
                    int xr = x < n - 1 ? x + 1 : x;
                    float height = h[i];
                    float r, g, b;

                    if (height <= 0f)
                    {
                        OceanColor(options.Style, -height, u, v, n, seed, out r, out g, out b);
                    }
                    else
                    {
                        float a = h[xl + y * n];
                        float bb = h[xr + y * n];
                        float c = h[x + yu * n];
                        float d = h[x + yd * n];
                        if (a < 0f) a = 0f;
                        if (bb < 0f) bb = 0f;
                        if (c < 0f) c = 0f;
                        if (d < 0f) d = 0f;
                        float gx = (bb - a) / (2f * cell);
                        float gy = (d - c) / (2f * cell);
                        float slope = (float)Math.Sqrt(gx * gx + gy * gy);

                        float nx = -gx * options.Exaggeration;
                        float ny = -gy * options.Exaggeration;
                        float len = (float)Math.Sqrt(nx * nx + ny * ny + 1f);
                        float lit = (nx * lx + ny * ly + lz) / (len * lz);

                        switch (options.Style)
                        {
                            case RegionMapStyle.Atlas:
                                AtlasLand(data, i, height, out r, out g, out b);
                                break;
                            case RegionMapStyle.Relief:
                                ReliefLand(data, i, height, slope, out r, out g, out b);
                                break;
                            default:
                                SatelliteLand(data, i, x, y, height, slope, seed, out r, out g, out b);
                                break;
                        }

                        float contrast = options.Style == RegionMapStyle.Atlas ? 0.4f : (options.Style == RegionMapStyle.Relief ? 0.8f : 0.58f);
                        float light = 1f + (lit - 1f) * contrast;
                        if (light < 0.42f) light = 0.42f;
                        if (light > 1.45f) light = 1.45f;
                        if (shadow != null) light *= 1f - shadow[i] * options.CastShadow;
                        r *= light;
                        g *= light;
                        b *= light;

                        if (options.Rivers)
                        {
                            float water = 0f;
                            float flow = data.Flow[i];
                            if (flow > riverThreshold) water = RegionNoise.Smooth(riverThreshold, riverThreshold * 5f, flow) * 0.55f + 0.35f;
                            if (data.LakeDepth[i] > 0f) water = 0.92f;
                            if (water > 0f)
                            {
                                float wr, wg, wb;
                                RiverColor(options.Style, out wr, out wg, out wb);
                                r += (wr - r) * water;
                                g += (wg - g) * water;
                                b += (wb - b) * water;
                            }
                        }

                        if (options.Style == RegionMapStyle.Atlas)
                        {
                            float line = ContourLine(height, slope, cell);
                            r *= 1f - line * 0.32f;
                            g *= 1f - line * 0.34f;
                            b *= 1f - line * 0.36f;
                        }
                    }

                    if (clouds)
                    {
                        float shade = CloudAmount(u - cloudOffset, v - cloudOffset, seed, options.CloudCover);
                        float dim = 1f - shade * 0.42f;
                        r *= dim;
                        g *= dim;
                        b *= dim;
                        float cover = CloudAmount(u, v, seed, options.CloudCover);
                        if (cover > 0f)
                        {
                            // 雲の上面は北西が明るく、南東が少し暗い
                            float inner = CloudAmount(u + 0.004f, v + 0.004f, seed, options.CloudCover);
                            float tone = 236f + 16f * RegionNoise.Clamp01(cover - inner + 0.5f) - 14f * inner;
                            float alpha = cover * 0.93f;
                            r += (tone - r) * alpha;
                            g += (tone - g) * alpha;
                            b += (tone + 3f - b) * alpha;
                        }
                    }

                    int p = i * 4;
                    pixels[p] = ToByte(r);
                    pixels[p + 1] = ToByte(g);
                    pixels[p + 2] = ToByte(b);
                    pixels[p + 3] = 255;
                }
            }
            return pixels;
        }

        private static byte ToByte(float v)
        {
            if (v <= 0f) return 0;
            if (v >= 255f) return 255;
            return (byte)(v + 0.5f);
        }

        private static void OceanColor(RegionMapStyle style, float depth, float u, float v, int n, uint seed, out float r, out float g, out float b)
        {
            float deep = 1f - (float)Math.Exp(-depth / 1100f);
            float shelf = (float)Math.Exp(-depth / 90f);
            if (style == RegionMapStyle.Atlas)
            {
                // 水深を段にして、地図帳の海のように見せる
                float band = (float)Math.Floor(deep * 6f) / 6f;
                r = 206f - 70f * band;
                g = 226f - 52f * band;
                b = 236f - 30f * band;
                return;
            }
            if (style == RegionMapStyle.Relief)
            {
                r = 74f - 34f * deep + 30f * shelf;
                g = 84f - 36f * deep + 30f * shelf;
                b = 96f - 34f * deep + 28f * shelf;
                return;
            }
            float ripple = RegionNoise.Fbm(u * 90f, v * 90f, 3, seed + 31u) * 3f;
            r = 24f - 12f * deep + 58f * shelf + ripple;
            g = 66f - 36f * deep + 92f * shelf + ripple;
            b = 112f - 40f * deep + 46f * shelf + ripple;
        }

        private static void RiverColor(RegionMapStyle style, out float r, out float g, out float b)
        {
            if (style == RegionMapStyle.Atlas) { r = 92f; g = 142f; b = 190f; }
            else if (style == RegionMapStyle.Relief) { r = 96f; g = 108f; b = 120f; }
            else { r = 38f; g = 74f; b = 98f; }
        }

        private static void SatelliteLand(RegionData data, int i, int x, int y, float height, float slope, uint seed, out float r, out float g, out float b)
        {
            float t = data.Temperature[i];
            float m = data.Moisture[i];
            ClimateColor(t, m, out r, out g, out b);

            // 森は細かなむらで暗く、草地や砂地は淡く
            float grain = RegionNoise.Hash01(x, y, seed + 41u);
            float patch = RegionNoise.Fbm(x * 0.045f, y * 0.045f, 3, seed + 42u);
            float forest = RegionNoise.Smooth(0.42f, 0.7f, m) * RegionNoise.Smooth(1f, 8f, t);
            float mottle = 1f - forest * (0.10f * grain + 0.07f) + patch * 0.07f;
            r *= mottle;
            g *= mottle;
            b *= mottle;

            // 急な斜面は岩肌
            float rock = RegionNoise.Smooth(0.3f, 0.56f, slope);
            r += (128f - r) * rock * 0.7f;
            g += (120f - g) * rock * 0.7f;
            b += (110f - b) * rock * 0.7f;

            // 暖かい海岸の砂浜
            if (height < 9f)
            {
                float sand = (1f - height / 9f) * RegionNoise.Smooth(6f, 16f, t) * 0.6f;
                r += (214f - r) * sand;
                g += (200f - g) * sand;
                b += (160f - b) * sand;
            }

            // 雪は寒い所に積もり、急な斜面では薄い
            float snow = 1f - RegionNoise.Smooth(-5f, 0.5f, t + (grain - 0.5f) * 1.5f);
            snow *= 1f - rock * 0.45f;
            r += (238f - r) * snow;
            g += (242f - g) * snow;
            b += (248f - b) * snow;
        }

        private static void AtlasLand(RegionData data, int i, float height, out float r, out float g, out float b)
        {
            // 低地の緑から、黄、褐色、白へ（標高別の色分け）
            float k = height / Math.Max(1f, data.MaxHeight);
            float[] stops = { 0f, 0.06f, 0.2f, 0.42f, 0.7f, 1f };
            float[] colors =
            {
                168, 198, 150,
                196, 214, 160,
                232, 226, 172,
                214, 184, 140,
                176, 148, 124,
                244, 242, 238,
            };
            int s = 0;
            while (s < stops.Length - 2 && k > stops[s + 1]) s++;
            float f = RegionNoise.Clamp01((k - stops[s]) / (stops[s + 1] - stops[s]));
            r = colors[s * 3] + (colors[s * 3 + 3] - colors[s * 3]) * f;
            g = colors[s * 3 + 1] + (colors[s * 3 + 4] - colors[s * 3 + 1]) * f;
            b = colors[s * 3 + 2] + (colors[s * 3 + 5] - colors[s * 3 + 2]) * f;

            // 乾いた土地は少し黄みへ、森は少し緑へ寄せる
            float m = data.Moisture[i];
            r += (0.5f - m) * 14f;
            b -= (0.5f - m) * 10f;
        }

        private static void ReliefLand(RegionData data, int i, float height, float slope, out float r, out float g, out float b)
        {
            float k = RegionNoise.Clamp01(height / Math.Max(1f, data.MaxHeight));
            float tone = 206f + 34f * k;
            r = tone;
            g = tone - 4f;
            b = tone - 12f;
            float t = data.Temperature[i];
            float snow = 1f - RegionNoise.Smooth(-5f, 0.5f, t);
            r += (250f - r) * snow;
            g += (250f - g) * snow;
            b += (252f - b) * snow;
        }

        private static float ContourLine(float height, float slope, float cell)
        {
            const float interval = 250f;
            float phase = height / interval;
            float distance = Math.Abs(phase - (float)Math.Round(phase)) * interval;
            // 1 セルで変わる高さに合わせて線の太さを保つ
            float width = Math.Max(4f, slope * cell * 0.55f);
            return 1f - RegionNoise.Smooth(width * 0.6f, width, distance);
        }

        private static void ClimateColor(float temperature, float moisture, out float r, out float g, out float b)
        {
            float ty = (temperature + 8f) / 38f * 3f;
            float mx = moisture * 3f;
            if (ty < 0f) ty = 0f; else if (ty > 2.999f) ty = 2.999f;
            if (mx < 0f) mx = 0f; else if (mx > 2.999f) mx = 2.999f;
            int iy = (int)ty;
            int ix = (int)mx;
            float fy = ty - iy;
            float fx = mx - ix;
            int a = (ix + iy * 4) * 3;
            int bIndex = a + 3;
            int c = a + 12;
            int d = c + 3;
            r = Bilinear(ClimateColors[a], ClimateColors[bIndex], ClimateColors[c], ClimateColors[d], fx, fy);
            g = Bilinear(ClimateColors[a + 1], ClimateColors[bIndex + 1], ClimateColors[c + 1], ClimateColors[d + 1], fx, fy);
            b = Bilinear(ClimateColors[a + 2], ClimateColors[bIndex + 2], ClimateColors[c + 2], ClimateColors[d + 2], fx, fy);
        }

        private static float Bilinear(float a, float b, float c, float d, float fx, float fy)
        {
            float top = a + (b - a) * fx;
            float bottom = c + (d - c) * fx;
            return top + (bottom - top) * fy;
        }

        /// <summary>雲の濃さ（0〜1）。大きな雲のまとまりの中に、細かな雲の粒を作る。</summary>
        private static float CloudAmount(float u, float v, uint seed, float cover)
        {
            float wx = RegionNoise.Fbm(u * 3f + 5.2f, v * 3f + 1.3f, 3, seed + 51u);
            float wy = RegionNoise.Fbm(u * 3f + 9.7f, v * 3f + 6.1f, 3, seed + 52u);
            float system = RegionNoise.Fbm(u * 1.6f + wx * 0.3f, v * 1.6f + wy * 0.3f, 3, seed + 53u);
            float puffs = RegionNoise.Fbm(u * 10f + wx * 0.3f, v * 10f + wy * 0.3f, 6, seed + 54u);
            float density = puffs * 0.75f + system * 0.9f + (cover - 0.5f) * 0.7f;
            return RegionNoise.Smooth(0.16f, 0.5f, density);
        }

        /// <summary>
        /// 山が落とす影。光が北西から来るので、北西から南東への斜めの列を順にたどり、
        /// それまでの地形が作る影の高さより低いセルを影にする。
        /// </summary>
        private static float[] BuildShadow(RegionData data, RegionRenderOptions options, float altitude)
        {
            int n = data.Resolution;
            float[] h = data.Height;
            var shadow = new float[n * n];
            float drop = (float)Math.Tan(altitude) * data.CellMeters * 1.4142135f / options.Exaggeration;
            for (int start = -(n - 1); start < n; start++)
            {
                int x = start > 0 ? start : 0;
                int y = start > 0 ? 0 : -start;
                float ceiling = 0f;
                while (x < n && y < n)
                {
                    int i = x + y * n;
                    float ground = h[i] > 0f ? h[i] : 0f;
                    ceiling -= drop;
                    if (ground >= ceiling)
                    {
                        ceiling = ground;
                    }
                    else if (h[i] > 0f)
                    {
                        shadow[i] = RegionNoise.Smooth(0f, 220f, ceiling - ground);
                    }
                    x++;
                    y++;
                }
            }
            return shadow;
        }
    }
}
