using System;
using System.Threading.Tasks;

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

        /// <summary>雲の量（0 で無し、0.5 前後で空の半分ほど、1 でほぼ全面）。</summary>
        public float CloudCover = 0.58f;

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
            134, 127, 116,   124, 121, 107,   106, 111,  96,    98, 106,  94,
            146, 133, 102,   116, 117,  80,    60,  80,  54,    45,  69,  49,
            180, 160, 118,   132, 132,  80,    66,  95,  53,    43,  79,  47,
            208, 184, 138,   164, 152,  92,    80, 109,  55,    37,  83,  43,
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
            bool satellite = options.Style == RegionMapStyle.Satellite;
            bool clouds = satellite && options.CloudCover > 0f;
            int cloudSize = (n + 1) / 2;
            float[] cloud = clouds ? RegionClouds.BuildField(cloudSize, data.Spec.Seed, options.CloudCover) : null;
            float cloudScale = cloudSize / (float)n;
            float cloudOffset = RegionClouds.ShadowOffset * cloudSize;
            float cloudRim = Math.Max(0.75f, cloudSize / 400f);
            // 拡大した地形は水面の割合を自分で持つ。広域の地域では流量から作る
            float[] detailWater = options.Rivers ? data.WaterCoverage : null;
            float[] river = detailWater != null ? detailWater : (satellite && options.Rivers ? BuildRiverCoverage(data, riverThreshold) : null);
            float[] structure = data.StructureCoverage;
            byte[] development = RegionDevelopmentRendering.Draw(data);
            float castStrength = Math.Min(1f, options.CastShadow * 2.2f);

            // 行ごとに独立なので並列に描く
            Parallel.For(0, n, y =>
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
                        OceanColor(options.Style, -height, x, y, u, v, seed, out r, out g, out b);
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
                                SatelliteLand(data, i, x, y, u, v, height, slope, seed, riverThreshold, out r, out g, out b);
                                break;
                        }

                        if (structure != null && structure[i] > 0f)
                        {
                            // 構造物は石の色にする（仮の色。素材と色は人が決める）
                            float cover = structure[i];
                            float stone = 172f + (RegionNoise.Hash01(x, y, seed + 51u) - 0.5f) * 7f;
                            r += (stone + 4f - r) * cover;
                            g += (stone - g) * cover;
                            b += (stone - 8f - b) * cover;
                        }

                        if (satellite)
                        {
                            // 直射光（やや暖かい色）と空からの光（青み）を分ける。日陰は黒く潰れず、青黒くなる
                            float direct = lit;
                            if (direct < 0f) direct = 0f;
                            if (direct > 1.8f) direct = 1.8f;
                            direct = 1f + (direct - 1f) * 0.82f;
                            if (shadow != null) direct *= 1f - shadow[i] * castStrength;
                            r *= 0.31f + 0.69f * direct * 1.03f;
                            g *= 0.34f + 0.66f * direct;
                            b *= 0.40f + 0.62f * direct * 0.96f;

                            if (river != null)
                            {
                                float water = river[i];
                                if (data.LakeDepth[i] > 0f) water = 0.92f;
                                if (water > 0f)
                                {
                                    r += (30f - r) * water;
                                    g += (60f - g) * water;
                                    b += (86f - b) * water;
                                }
                            }
                        }
                        else
                        {
                            float contrast = options.Style == RegionMapStyle.Atlas ? 0.4f : 0.8f;
                            float light = 1f + (lit - 1f) * contrast;
                            if (light < 0.42f) light = 0.42f;
                            if (light > 1.45f) light = 1.45f;
                            if (shadow != null) light *= 1f - shadow[i] * options.CastShadow;
                            r *= light;
                            g *= light;
                            b *= light;
                        }

                        if (!satellite && options.Rivers)
                        {
                            float water = 0f;
                            float flow = data.Flow[i];
                            if (detailWater != null) water = detailWater[i];
                            else if (flow > riverThreshold) water = RegionNoise.Smooth(riverThreshold, riverThreshold * 5f, flow) * 0.55f + 0.35f;
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

                    int p = i * 4;
                    if (development != null && development[p + 3] > 0 && height > 0f && data.LakeDepth[i] <= 0f)
                    {
                        float keep = 1f - development[p + 3] / 255f;
                        r = r * keep + development[p];
                        g = g * keep + development[p + 1];
                        b = b * keep + development[p + 2];
                    }

                    if (clouds)
                    {
                        // 雲の影は、北西から来る光の向きへずらした位置の雲から落ちる
                        float cx = x * cloudScale;
                        float cy = y * cloudScale;
                        float dim = 1f - RegionClouds.ShadowStrength(RegionClouds.Sample(cloud, cloudSize, cx - cloudOffset, cy - cloudOffset));
                        r *= dim;
                        g *= dim;
                        b *= dim * 1.03f;
                        float tone;
                        float alpha = RegionClouds.Shade(cloud, cloudSize, cx, cy, cloudRim, out tone);
                        if (alpha > 0f)
                        {
                            r += (tone - 3f - r) * alpha;
                            g += (tone - g) * alpha;
                            b += (tone + 4f - b) * alpha;
                        }
                    }

                    pixels[p] = ToByte(r);
                    pixels[p + 1] = ToByte(g);
                    pixels[p + 2] = ToByte(b);
                    pixels[p + 3] = 255;
                }
            });
            return pixels;
        }

        private static byte ToByte(float v)
        {
            if (v <= 0f) return 0;
            if (v >= 255f) return 255;
            return (byte)(v + 0.5f);
        }

        private static void OceanColor(RegionMapStyle style, float depth, int x, int y, float u, float v, uint seed, out float r, out float g, out float b)
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
            // 深い海は暗い紺。浅瀬の明るい青緑は岸のごく近くだけに出す
            float shallow = (float)Math.Exp(-depth / 34f);
            float slopeWater = (float)Math.Exp(-depth / 320f);
            float drift = RegionNoise.FbmTurned(u * 5f + 2.3f, v * 5f + 8.1f, 4, seed + 31u);
            float grain = (RegionNoise.Hash01(x, y, seed + 32u) - 0.5f) * 1.6f;
            r = 9f + 7f * slopeWater + 46f * shallow + drift * 2.5f + grain;
            g = 29f + 24f * slopeWater + 98f * shallow + drift * 5f + grain;
            b = 62f + 34f * slopeWater + 62f * shallow + drift * 6f + grain;
        }

        private static void RiverColor(RegionMapStyle style, out float r, out float g, out float b)
        {
            if (style == RegionMapStyle.Atlas) { r = 92f; g = 142f; b = 190f; }
            else if (style == RegionMapStyle.Relief) { r = 96f; g = 108f; b = 120f; }
            else { r = 38f; g = 74f; b = 98f; }
        }

        private static void SatelliteLand(RegionData data, int i, int x, int y, float u, float v, float height, float slope,
            uint seed, float riverThreshold, out float r, out float g, out float b)
        {
            float t = data.Temperature[i];
            float m = data.Moisture[i];

            // 植生のむら（広いまとまり、細かなまだら、1 セルごとの粒）と、谷底の湿り
            float broad = RegionNoise.FbmTurned(u * 11f + 3.1f, v * 11f + 7.7f, 4, seed + 42u);
            float fine = RegionNoise.FbmTurned(u * 64f + 1.9f, v * 64f + 4.3f, 3, seed + 43u);
            float grain = RegionNoise.Hash01(x, y, seed + 41u);
            float valley = RegionNoise.Smooth(riverThreshold * 0.03f, riverThreshold, data.Flow[i]);
            float wet = m + broad * 0.2f + fine * 0.08f + valley * 0.12f;
            ClimateColor(t + fine * 1.5f, wet, out r, out g, out b);

            // 森は木の粒で暗く、開けた土地は淡く
            float forest = RegionNoise.Smooth(0.42f, 0.7f, wet) * RegionNoise.Smooth(1f, 8f, t);
            float tone = 1.07f + broad * 0.05f + fine * 0.06f + (grain - 0.5f) * 0.05f - forest * (0.13f * grain + 0.03f);
            r *= tone;
            g *= tone;
            b *= tone;

            // 急な斜面は岩肌。森の斜面は木に覆われるので出にくい
            float steep = RegionNoise.Smooth(0.3f, 0.62f, slope);
            float rock = steep * (1f - 0.55f * forest);
            r += (124f - r) * rock * 0.75f;
            g += (116f - g) * rock * 0.75f;
            b += (106f - b) * rock * 0.75f;

            // 暖かい海岸の砂浜
            if (height < 8f)
            {
                float sand = (1f - height / 8f) * RegionNoise.Smooth(6f, 16f, t) * 0.55f;
                r += (212f - r) * sand;
                g += (198f - g) * sand;
                b += (160f - b) * sand;
            }

            // 雪は寒い所に積もる。境目はむらがあり、急な斜面では薄い
            float snow = 1f - RegionNoise.Smooth(-4.5f, 1.5f, t + (grain - 0.5f) * 1.2f + fine * 1.6f + broad * 0.8f);
            snow *= 1f - steep * 0.4f;
            r += (240f - r) * snow;
            g += (243f - g) * snow;
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

        /// <summary>
        /// 川が各セルを覆う割合（0〜1）。流量が多いほど太く濃くし、最後に軽くぼかして線の段々を消す。
        /// 細い川は薄くしか写らないので、地図の線ではなく写真に写る川に見える。
        /// </summary>
        private static float[] BuildRiverCoverage(RegionData data, float riverThreshold)
        {
            int n = data.Resolution;
            int count = n * n;
            var raw = new float[count];
            float[] flow = data.Flow;
            float[] h = data.Height;
            float logSpan = (float)Math.Log(80.0);
            for (int y = 1; y < n - 1; y++)
            {
                for (int x = 1; x < n - 1; x++)
                {
                    int i = x + y * n;
                    if (h[i] <= 0f || flow[i] <= riverThreshold) continue;
                    float k = (float)Math.Log(flow[i] / riverThreshold) / logSpan;
                    if (k > 1f) k = 1f;
                    float width = 0.55f + 1.7f * k;
                    float center = width < 1f ? width : 1f;
                    if (raw[i] < center) raw[i] = center;
                    if (width > 1f)
                    {
                        float side = (width - 1f) * 0.5f;
                        if (raw[i - 1] < side) raw[i - 1] = side;
                        if (raw[i + 1] < side) raw[i + 1] = side;
                        if (raw[i - n] < side) raw[i - n] = side;
                        if (raw[i + n] < side) raw[i + n] = side;
                    }
                }
            }

            var result = new float[count];
            for (int y = 1; y < n - 1; y++)
            {
                for (int x = 1; x < n - 1; x++)
                {
                    int i = x + y * n;
                    if (h[i] <= 0f) continue;
                    float sum = raw[i] * 0.52f
                        + (raw[i - 1] + raw[i + 1] + raw[i - n] + raw[i + n]) * 0.09f
                        + (raw[i - n - 1] + raw[i - n + 1] + raw[i + n - 1] + raw[i + n + 1]) * 0.03f;
                    result[i] = sum < 0.9f ? sum : 0.9f;
                }
            }
            return result;
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
