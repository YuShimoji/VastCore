using System;
using System.Threading.Tasks;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// RegionSpec から地域を一つ作る。順序は「海陸と隆起の下地 → 隆起と河川侵食の繰り返し → 気候 → 川 → 土地の種類 → 探索地点」。
    /// </summary>
    public static class RegionGenerator
    {
        private const float ReferenceResolution = 1024f;

        /// <summary>地域を作る。phase には、各工程に入る時にその名前が渡る（進み具合の表示と時間の計測に使う）。</summary>
        public static RegionData Generate(RegionSpec spec, Action<string> phase = null)
        {
            return Generate(spec, phase, null);
        }

        // 標高は同期で観測する。次の工程で同じ配列を更新するため、保存する側で複製する。
        internal static RegionData Generate(RegionSpec spec, Action<string> phase, Action<string, int, float[]> terrain,
            bool baselineTerrain = false)
        {
            var data = new RegionData(spec.Clone());
            int n = data.Resolution;
            int count = n * n;
            uint seed = unchecked((uint)spec.Seed * 747796405u + 2891336453u);

            var uplift = new float[count];
            var erodibility = new float[count];
            var land = new bool[count];
            var router = new FlowRouter(n) { Jitter = spec.FlowJitter, JitterSeed = seed + 61u };
            var scratch = new float[count];

            if (phase != null) phase("海と陸の下地");
            BuildBase(data, seed, uplift, erodibility, land);
            terrain?.Invoke("base", n, data.Height);
            if (phase != null) phase("隆起と侵食");
            Evolve(data, seed, uplift, erodibility, land, router, scratch, terrain, baselineTerrain);
            terrain?.Invoke("final-erosion", n, data.Height);
            if (phase != null) phase("気候");
            BuildClimate(data, seed, land, scratch);
            if (phase != null) phase("川");
            BuildRivers(data, land, router, scratch);
            if (phase != null) phase("土地の種類と探索地点");
            ClassifyBiomes(data, land);
            // 川を作った時の流れ先（router）をそのまま使い、流域でエリアを分ける
            RegionAreaBuilder.Build(data, router, land);
            RegionSiteGenerator.Populate(data);
            if (spec.NameStyle != RegionNameStyle.Descriptive) RegionNames.Apply(data, spec.NameStyle);
            if (phase != null) phase("完了");
            return data;
        }

        /// <summary>川として描く流量の下限。解像度が変わっても同じ流域面積になるように換算する。</summary>
        public static float RiverThreshold(RegionData data)
        {
            float scale = data.Resolution / ReferenceResolution;
            return 320f * scale * scale;
        }

        private static void BuildBase(RegionData data, uint seed, float[] uplift, float[] erodibility, bool[] land)
        {
            RegionSpec spec = data.Spec;
            int n = data.Resolution;
            float inv = 1f / (n - 1);
            float[] h = data.Height;

            // 行ごとに独立なので並列に計算する（どのセルも自分の位置と seed だけで決まる）
            Parallel.For(0, n, y =>
            {
                float v = y * inv;
                for (int x = 0; x < n; x++)
                {
                    float u = x * inv;
                    int i = x + y * n;

                    // 座標をゆがめて、海岸線と山並みを直線的にしない
                    float wx = RegionNoise.Fbm(u * 2.4f + 11.3f, v * 2.4f + 4.7f, 4, seed + 1u);
                    float wy = RegionNoise.Fbm(u * 2.4f + 31.9f, v * 2.4f + 17.1f, 4, seed + 2u);
                    float qx = u + 0.14f * wx;
                    float qy = v + 0.14f * wy;

                    float continent = RegionNoise.Fbm(qx * 2.6f, qy * 2.6f, 6, seed + 3u, 2f, 0.56f);
                    float islands = RegionNoise.Fbm(qx * 7.5f, qy * 7.5f, 5, seed + 4u);

                    float ex = Math.Abs(qx - 0.5f) * 2f;
                    float ey = Math.Abs(qy - 0.5f) * 2f;
                    float edge = (float)Math.Sqrt(ex * ex + ey * ey) * 0.7f + Math.Max(ex, ey) * 0.3f;
                    float falloff = RegionNoise.Smooth(0.66f, 1.08f, edge);
                    float shore = RegionNoise.Fbm(qx * 19f, qy * 19f, 5, seed + 10u);

                    // 地図の外周 2 セルは必ず海にする
                    int border = Math.Min(Math.Min(x, y), Math.Min(n - 1 - x, n - 1 - y));
                    float landness = continent * 1.3f + islands * 0.4f + shore * 0.085f + spec.LandBias - falloff * spec.EdgeFalloff;
                    if (border < 2) landness = -0.5f;

                    if (landness > 0f)
                    {
                        land[i] = true;
                        float detail = RegionNoise.Fbm(qx * 22f, qy * 22f, 4, seed + 7u);
                        h[i] = 4f + landness * 320f + (detail + 1f) * 14f + RegionNoise.Hash01(x, y, seed + 12u) * spec.RoughnessMeters;

                        float belt = RegionNoise.Smooth(-0.18f, 0.32f, RegionNoise.Fbm(qx * 1.7f + 7.1f, qy * 1.7f + 3.3f, 4, seed + 5u));
                        float ridge = RegionNoise.Ridged(qx * 4.2f, qy * 4.2f, 5, seed + 6u);
                        float inland = RegionNoise.Smooth(0f, 0.2f, landness);
                        float hills = RegionNoise.Ridged(qx * 11f + 3.7f, qy * 11f + 1.9f, 4, seed + 8u);
                        // 丘陵が広がる所と平野のままの所を分ける
                        float rolling = RegionNoise.Smooth(-0.05f, 0.4f, RegionNoise.Fbm(qx * 3.4f + 19.1f, qy * 3.4f + 6.6f, 3, seed + 11u));
                        uplift[i] = inland * (0.015f + 0.20f * hills * rolling + 1.3f * belt * ridge * ridge * spec.MountainAmount) + 0.012f;

                        float rock = 0.5f + 0.5f * RegionNoise.Fbm(qx * 3.1f + 2.2f, qy * 3.1f + 8.8f, 3, seed + 9u);
                        erodibility[i] = spec.ErosionStrength * (0.55f + 0.9f * rock);
                    }
                    else
                    {
                        land[i] = false;
                        float depth = -landness;
                        h[i] = -(6f + 4800f * RegionNoise.Smooth(0f, 0.62f, depth) + 260f * depth);
                    }
                }
            });
        }

        /// <summary>
        /// 隆起と侵食を、まず半分の解像度で大半の回数だけ進め、その結果を元の解像度へ広げてから残りを進める。
        /// 大きな谷筋は粗い格子で決まり、細かな刻みだけを細かい格子で作るので、全部を細かい格子で回すより速い。
        /// </summary>
        private static void Evolve(RegionData data, uint seed, float[] uplift, float[] erodibility, bool[] land,
            FlowRouter router, float[] area, Action<string, int, float[]> terrain, bool baselineTerrain)
        {
            RegionSpec spec = data.Spec;
            int n = data.Resolution;
            float[] h = data.Height;
            int fineIterations = spec.ErosionIterations;

            if (n >= 512 && (n & 1) == 0)
            {
                int half = n / 2;
                int halfCount = half * half;
                var coarseHeight = new float[halfCount];
                var coarseUplift = new float[halfCount];
                var coarseErodibility = new float[halfCount];
                var coarseLand = new bool[halfCount];
                for (int y = 0; y < half; y++)
                {
                    for (int x = 0; x < half; x++)
                    {
                        int source = x * 2 + y * 2 * n;
                        int target = x + y * half;
                        coarseHeight[target] = h[source];
                        coarseUplift[target] = uplift[source];
                        coarseErodibility[target] = erodibility[source];
                        coarseLand[target] = land[source];
                    }
                }

                int coarseIterations = spec.ErosionIterations * 3 / 4;
                fineIterations = spec.ErosionIterations - coarseIterations;
                EvolveGrid(coarseHeight, coarseUplift, coarseErodibility, coarseLand, half, data.SizeKm * 1000f / half,
                    spec, coarseIterations, new FlowRouter(half) { Jitter = spec.FlowJitter, JitterSeed = seed + 62u }, new float[halfCount]);
                terrain?.Invoke("coarse-erosion", half, coarseHeight);

                for (int y = 0; y < n; y++)
                {
                    float fy = y * 0.5f;
                    int y0 = (int)fy;
                    int y1 = y0 + 1 < half ? y0 + 1 : y0;
                    float ty = fy - y0;
                    for (int x = 0; x < n; x++)
                    {
                        int i = x + y * n;
                        if (!land[i]) continue;
                        float fx = x * 0.5f;
                        int x0 = (int)fx;
                        int x1 = x0 + 1 < half ? x0 + 1 : x0;
                        float tx = fx - x0;
                        float a = coarseHeight[x0 + y0 * half];
                        float b = coarseHeight[x1 + y0 * half];
                        float c = coarseHeight[x0 + y1 * half];
                        float d = coarseHeight[x1 + y1 * half];
                        if (a < 0f) a = 0f;
                        if (b < 0f) b = 0f;
                        if (c < 0f) c = 0f;
                        if (d < 0f) d = 0f;
                        float top = a + (b - a) * tx;
                        float bottom = c + (d - c) * tx;
                        // 広げた面はなめらかなので、細かい格子でも流れが合流するように凹凸を足す
                        float value = top + (bottom - top) * ty + RegionNoise.Hash01(x, y, seed + 13u) * spec.RoughnessMeters;
                        h[i] = value > 0.5f ? value : 0.5f;
                    }
                }
                terrain?.Invoke("interpolated", n, h);
            }

            bool refineTerrain = !baselineTerrain && n >= 512 && fineIterations > 0;
            RegionLake lake = refineTerrain ? RegionLake.Select(data) : null;
            EvolveGrid(h, uplift, erodibility, land, n, data.CellMeters, spec, fineIterations, router, area,
                lake == null ? null : data.LakeDepth, refineTerrain);
            lake?.Finish(data);
        }

        /// <summary>
        /// 隆起させ、川の流量に応じて削ることを繰り返す。削る量は下流から順に陰解法で解くので、
        /// 1 回の変化を大きく取っても発散しない。
        /// </summary>
        private static void EvolveGrid(float[] h, float[] uplift, float[] erodibility, bool[] land, int n, float cell,
            RegionSpec spec, int iterations, FlowRouter router, float[] area, float[] retainedLake = null,
            bool softenPlains = false)
        {
            int count = n * n;
            float scale = n / ReferenceResolution;
            float areaScale = 1f / (scale * scale);
            float diffusion = Math.Min(0.2f, spec.HillslopeDiffusion * scale * scale);
            var previous = new float[count];

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                for (int i = 0; i < count; i++)
                {
                    if (land[i] && (retainedLake == null || retainedLake[i] <= 0f)) h[i] += uplift[i] * spec.UpliftMeters;
                }

                router.Route(h, land);
                router.Accumulate(null, land, area);

                for (int o = 0; o < router.Count; o++)
                {
                    int i = router.Order[o];
                    if (retainedLake != null && retainedLake[i] > 0f) continue;
                    int r = router.Receiver[i];
                    if (r < 0) continue;
                    float downstream = land[r] ? h[r] : 0f;
                    if (retainedLake != null) downstream += retainedLake[r];
                    if (h[i] <= downstream)
                    {
                        // 窪地は土砂で埋まったものとして、流れ先より少し高くする
                        h[i] = downstream + 0.05f;
                        continue;
                    }
                    int diff = r - i;
                    bool diagonal = diff == n + 1 || diff == n - 1 || diff == -n + 1 || diff == -n - 1;
                    float f = erodibility[i] * (float)Math.Pow(area[i] * areaScale, 0.45) * scale;
                    if (diagonal) f *= 0.70710678f;
                    float next = (h[i] + f * downstream) / (1f + f);

                    // 斜面は安息角より急にならない（崩れて流れ先へ落ちる）
                    float maxDrop = spec.MaxSlope * cell * (diagonal ? 1.4142135f : 1f);
                    if (next - downstream > maxDrop) next = downstream + maxDrop;
                    h[i] = next;
                }

                if (diffusion > 0f)
                {
                    Array.Copy(h, previous, count);
                    for (int y = 1; y < n - 1; y++)
                    {
                        for (int x = 1; x < n - 1; x++)
                        {
                            int i = x + y * n;
                            if (!land[i] || retainedLake != null && retainedLake[i] > 0f) continue;
                            float a = previous[i - 1];
                            float b = previous[i + 1];
                            float c = previous[i - n];
                            float d = previous[i + n];
                            if (a < 0f) a = 0f;
                            if (b < 0f) b = 0f;
                            if (c < 0f) c = 0f;
                            if (d < 0f) d = 0f;
                            float height = previous[i];
                            float blend = diffusion;
                            if (softenPlains && height < 350f && land[i - 1] && land[i + 1] && land[i - n] && land[i + n])
                            {
                                // 低地だけで薄い溝をならす。標高と傾きの境目には幅を持たせる。
                                float slope = Math.Max(Math.Abs(b - a), Math.Abs(d - c)) / (2f * cell);
                                float low = 1f - RegionNoise.Smooth(250f, 350f, height);
                                float flat = 1f - RegionNoise.Smooth(0.035f, 0.065f, slope);
                                blend = Math.Min(0.2f, diffusion * (1f + low * flat));
                            }
                            float next = height + blend * (a + b + c + d - 4f * height);
                            h[i] = next > 0.05f ? next : 0.05f;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 気温は緯度と標高から。湿り気は西からの風が海で湿り、陸と山で雨を落として乾く過程をたどって作る。
        /// 山の風下が乾くので、同じ地域に森と乾燥地ができる。
        /// </summary>
        private static void BuildClimate(RegionData data, uint seed, bool[] land, float[] rain)
        {
            RegionSpec spec = data.Spec;
            int n = data.Resolution;
            int count = n * n;
            float inv = 1f / (n - 1);
            float cellKm = data.SizeKm / n;
            float[] h = data.Height;
            float recharge = 1f - (float)Math.Exp(-cellKm / 14f);

            // 西の列から東へ。各行が運ぶ水分は、進むたびに南北の隣と混ざる
            var carried = new float[n];
            var mixed = new float[n];
            for (int y = 0; y < n; y++) carried[y] = 1f;
            for (int x = 0; x < n; x++)
            {
                for (int y = 0; y < n; y++)
                {
                    int i = x + y * n;
                    float ground = h[i] > 0f ? h[i] : 0f;
                    float previous = x > 0 && h[i - 1] > 0f ? h[i - 1] : 0f;
                    float air = carried[y];
                    if (!land[i])
                    {
                        air += (1f - air) * recharge;
                        rain[i] = air * 0.0035f;
                    }
                    else
                    {
                        float rise = ground - previous;
                        if (rise < 0f) rise = 0f;
                        float fall = air * (0.0026f * cellKm + rise * 0.00042f);
                        if (fall > air) fall = air;
                        air -= fall;
                        // 陸の上でも少しずつ湿り気が戻る
                        air += (0.42f - air) * recharge * 0.12f;
                        rain[i] = fall / cellKm;
                    }
                    carried[y] = air;
                }
                for (int pass = 0; pass < 2; pass++)
                {
                    for (int y = 0; y < n; y++)
                    {
                        float up = carried[y > 0 ? y - 1 : y];
                        float down = carried[y < n - 1 ? y + 1 : y];
                        mixed[y] = carried[y] * 0.5f + (up + down) * 0.25f;
                    }
                    float[] swap = carried;
                    carried = mixed;
                    mixed = swap;
                }
            }

            int radius = Math.Max(1, n / 140);
            var temp = new float[count];
            BoxBlur(rain, temp, n, radius);
            BoxBlur(rain, temp, n, radius);

            Parallel.For(0, n, y =>
            {
                float v = y * inv;
                for (int x = 0; x < n; x++)
                {
                    float u = x * inv;
                    int i = x + y * n;
                    float wet = 1f - (float)Math.Exp(-rain[i] / 0.0042f);
                    float variation = RegionNoise.Fbm(u * 3.3f + 41.7f, v * 3.3f + 13.9f, 4, seed + 21u);
                    data.Moisture[i] = RegionNoise.Clamp01(wet * 0.9f + variation * 0.22f + (spec.Humidity - 0.5f) * 0.8f);

                    float ground = h[i] > 0f ? h[i] : 0f;
                    float warm = RegionNoise.Lerp(spec.TemperatureNorth, spec.TemperatureSouth, v);
                    float wobble = RegionNoise.Fbm(u * 2.2f + 3.1f, v * 2.2f + 27.5f, 3, seed + 22u) * 3.5f;
                    data.Temperature[i] = warm + wobble - 6.5f * ground / 1000f;
                }
            });
        }

        private static void BuildRivers(RegionData data, bool[] land, FlowRouter router, float[] weight)
        {
            int n = data.Resolution;
            int count = n * n;
            float[] h = data.Height;
            for (int i = 0; i < count; i++)
            {
                weight[i] = 0.25f + data.Moisture[i] * 1.3f;
            }
            router.Route(h, land);
            RegionLake.ConstrainOutlet(data, router);
            router.Accumulate(weight, land, data.Flow);
            data.Downstream = new int[count];
            Array.Copy(router.Receiver, data.Downstream, count);
            if (!float.IsNaN(data.LakeSurfaceLevel)) return;
            for (int i = 0; i < count; i++)
            {
                float depth = land[i] ? router.Filled[i] - h[i] : 0f;
                data.LakeDepth[i] = depth > 1.5f ? depth : 0f;
            }
        }

        private static void ClassifyBiomes(RegionData data, bool[] land)
        {
            int n = data.Resolution;
            int count = n * n;
            float[] h = data.Height;
            float cell = data.CellMeters;
            float riverThreshold = RiverThreshold(data);
            float max = 0f;
            int landCount = 0;

            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = x + y * n;
                    if (!land[i])
                    {
                        data.Biome[i] = RegionBiome.Ocean;
                        continue;
                    }
                    landCount++;
                    if (h[i] > max) max = h[i];

                    float slope = SlopeAt(h, n, x, y, cell);
                    float t = data.Temperature[i];
                    float m = data.Moisture[i];
                    RegionBiome biome;
                    if (data.LakeDepth[i] > 0f) biome = RegionBiome.Lake;
                    else if (data.Flow[i] > riverThreshold * 2.5f) biome = RegionBiome.River;
                    else biome = LandBiome(t, m, slope, h[i]);
                    data.Biome[i] = biome;
                }
            }

            data.MaxHeight = max;
            data.LandFraction = landCount / (float)count;
        }

        /// <summary>気温・湿り気・傾き・標高から陸の土地の種類を決める（川と湖は呼ぶ側で先に分ける）。</summary>
        internal static RegionBiome LandBiome(float t, float m, float slope, float height)
        {
            if (t < -3f) return RegionBiome.Snow;
            if (slope > 0.42f) return RegionBiome.Rock;
            if (height < 10f) return RegionBiome.Coast;
            if (t < 3f) return RegionBiome.Tundra;
            if (t < 9f) return m > 0.38f ? RegionBiome.Taiga : RegionBiome.Tundra;
            if (t < 20f) return m < 0.2f ? RegionBiome.Desert : (m < 0.45f ? RegionBiome.Grassland : RegionBiome.Forest);
            return m < 0.22f ? RegionBiome.Desert : (m < 0.45f ? RegionBiome.Savanna : (m < 0.72f ? RegionBiome.Forest : RegionBiome.Rainforest));
        }

        /// <summary>地面の傾き（高さ / 水平距離）。海は海面として扱う。</summary>
        internal static float SlopeAt(float[] h, int n, int x, int y, float cell)
        {
            int xl = x > 0 ? x - 1 : x;
            int xr = x < n - 1 ? x + 1 : x;
            int yu = y > 0 ? y - 1 : y;
            int yd = y < n - 1 ? y + 1 : y;
            float a = h[xl + y * n];
            float b = h[xr + y * n];
            float c = h[x + yu * n];
            float d = h[x + yd * n];
            if (a < 0f) a = 0f;
            if (b < 0f) b = 0f;
            if (c < 0f) c = 0f;
            if (d < 0f) d = 0f;
            float gx = (b - a) / (2f * cell);
            float gy = (d - c) / (2f * cell);
            return (float)Math.Sqrt(gx * gx + gy * gy);
        }

        private static void BoxBlur(float[] values, float[] temp, int n, int radius)
        {
            float norm = 1f / (radius * 2 + 1);
            for (int y = 0; y < n; y++)
            {
                int row = y * n;
                float sum = 0f;
                for (int k = -radius; k <= radius; k++)
                {
                    int xx = k < 0 ? 0 : (k >= n ? n - 1 : k);
                    sum += values[row + xx];
                }
                for (int x = 0; x < n; x++)
                {
                    temp[row + x] = sum * norm;
                    int add = x + radius + 1;
                    int remove = x - radius;
                    if (add >= n) add = n - 1;
                    if (remove < 0) remove = 0;
                    sum += values[row + add] - values[row + remove];
                }
            }
            for (int x = 0; x < n; x++)
            {
                float sum = 0f;
                for (int k = -radius; k <= radius; k++)
                {
                    int yy = k < 0 ? 0 : (k >= n ? n - 1 : k);
                    sum += temp[x + yy * n];
                }
                for (int y = 0; y < n; y++)
                {
                    values[x + y * n] = sum * norm;
                    int add = y + radius + 1;
                    int remove = y - radius;
                    if (add >= n) add = n - 1;
                    if (remove < 0) remove = 0;
                    sum += temp[x + add * n] - temp[x + remove * n];
                }
            }
        }
    }
}
