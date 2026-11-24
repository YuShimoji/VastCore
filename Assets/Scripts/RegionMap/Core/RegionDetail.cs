using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Vastcore.RegionMap
{
    /// <summary>地域の一部を拡大して作り直すための入力。</summary>
    [Serializable]
    public sealed class RegionDetailSpec
    {
        /// <summary>窓の中心（地域の地図座標 0〜100）。窓が地域からはみ出す時は内側へ寄せる。</summary>
        public float CenterMapX = 50f;
        public float CenterMapY = 50f;

        /// <summary>窓の一辺（km）。</summary>
        public float SizeKm = 8f;

        /// <summary>窓の一辺のセル数。</summary>
        public int Resolution = 1024;

        /// <summary>細かな起伏の強さ（1 が標準）。</summary>
        public float Roughness = 1f;

        /// <summary>細かい格子で谷を刻む回数（0 で刻まない）。</summary>
        public int GullyPasses = 5;

        public RegionDetailSpec Clone()
        {
            return (RegionDetailSpec)MemberwiseClone();
        }
    }

    /// <summary>
    /// 地域の一部を拡大した地形。Data は窓だけを収めた RegionData で、広域の地域と同じ描画（RegionMapRenderer）へ渡せる。
    /// </summary>
    public sealed class RegionDetail
    {
        public RegionData Region;
        public RegionData Data;
        public RegionDetailSpec Spec;

        /// <summary>窓の北西の角と一辺（地域の地図座標）。</summary>
        public float OriginMapX;
        public float OriginMapY;
        public float SizeMap;

        /// <summary>海か陸かが、広域の同じ場所のセルと一致したセルの割合（0〜1）。</summary>
        public float CoastAgreement;

        /// <summary>広域のセルの中心で比べた、陸の標高の差の平均（m）。</summary>
        public float MeanHeightDifference;

        /// <summary>窓の中を通る広域の川のセルのうち、拡大した地形でも水面が通ったものの割合（0〜1）。川が無ければ 1。</summary>
        public float RiverAgreement;

        public readonly List<RegionStructure> Structures = new List<RegionStructure>();

        /// <summary>地域の地図座標から、窓の中のセルの位置（小数）へ。</summary>
        public float LocalX(float mapX)
        {
            return (mapX - OriginMapX) / SizeMap * Data.Resolution;
        }

        public float LocalY(float mapY)
        {
            return (mapY - OriginMapY) / SizeMap * Data.Resolution;
        }

        public bool Contains(float mapX, float mapY)
        {
            return mapX >= OriginMapX && mapY >= OriginMapY && mapX < OriginMapX + SizeMap && mapY < OriginMapY + SizeMap;
        }
    }

    /// <summary>
    /// 広域の地域（1 セル 250 m 前後）の一部を、細かい格子で作り直す。広域の地図と食い違わないように、次の順で作る。
    /// 1. 海岸：陸を +1、海を -1 とした場をなめらかに補間し、その 0 の線を海岸にする（広域のセルの境と同じ位置に来る）。
    /// 2. 標高：広域の標高を、行き過ぎない 3 次補間で広げる。広域のセルの中心では広域の値に近い。
    /// 3. 細かな起伏：地域全体の座標（m）で引くノイズを足す。強さは、その場所の広域での高低差に比例させる。
    ///    窓の位置や大きさを変えても、同じ場所には同じ起伏が出る。
    /// 4. 川：広域で決まった流れ先（RegionData.Downstream）の経路を曲線でつなぎ、川床を掘って水面を置く。
    ///    窓の中だけで水を流し直すと、窓の外から来る水量が分からず位置がずれるので、そうしない。
    /// 5. 谷の刻み：最後に、細かい格子で斜面の水を集め、集まった所を少し削る。海岸と川の水面は動かさない。
    /// </summary>
    public static class RegionDetailBuilder
    {
        private const int Margin = 4;
        private const int RiverSteps = 6;
        private const float SmallStreamRatio = 0.12f;

        private struct RiverPiece
        {
            public float Ax, Ay, Bx, By;
            public float Za, Zb;
            public float Wa, Wb;
        }

        public static RegionDetail Build(RegionData region, RegionDetailSpec spec)
        {
            if (region == null) throw new ArgumentNullException("region");
            spec = spec != null ? spec.Clone() : new RegionDetailSpec();
            if (spec.SizeKm > region.SizeKm) spec.SizeKm = region.SizeKm;
            if (spec.SizeKm < 0.5f) spec.SizeKm = 0.5f;
            if (spec.Resolution < 64) spec.Resolution = 64;

            int coarse = region.Resolution;
            int res = spec.Resolution;
            float coarseCell = region.CellMeters;
            float sizeMap = spec.SizeKm / region.SizeKm * RegionData.MapUnits;
            float originX = Clamp(spec.CenterMapX - sizeMap * 0.5f, 0f, RegionData.MapUnits - sizeMap);
            float originY = Clamp(spec.CenterMapY - sizeMap * 0.5f, 0f, RegionData.MapUnits - sizeMap);

            RegionSpec windowSpec = region.Spec.Clone();
            windowSpec.Resolution = res;
            windowSpec.SizeKm = spec.SizeKm;
            var data = new RegionData(windowSpec);
            data.WaterCoverage = new float[res * res];
            data.MaxHeight = region.MaxHeight;
            float cell = data.CellMeters;

            var detail = new RegionDetail
            {
                Region = region,
                Data = data,
                Spec = spec,
                OriginMapX = originX,
                OriginMapY = originY,
                SizeMap = sizeMap,
            };

            // 窓にかかる広域のセルを、余白つきで切り出す（添字は cx0・cy0 からの相対）
            float toCoarse = coarse / RegionData.MapUnits;
            float gx0 = originX * toCoarse - 0.5f;
            float gy0 = originY * toCoarse - 0.5f;
            float step = sizeMap * toCoarse / res;
            int cx0 = (int)Math.Floor(gx0) - Margin;
            int cy0 = (int)Math.Floor(gy0) - Margin;
            int w = (int)Math.Ceiling(gx0 + step * res) + Margin - cx0 + 1;
            int h = (int)Math.Ceiling(gy0 + step * res) + Margin - cy0 + 1;
            int count = w * h;

            var sign = new float[count];
            var landHeight = new float[count];
            var seaDepth = new float[count];
            var ground = new float[count];
            var relief = new float[count];
            var temperature = new float[count];
            var moisture = new float[count];
            var isLand = new bool[count];
            var source = new int[count];
            float lakeLevel = region.LakeSurfaceLevel;
            float[] lakeBank = float.IsNaN(lakeLevel) ? null : new float[count];
            float riverThreshold = RegionGenerator.RiverThreshold(region);

            for (int y = 0; y < h; y++)
            {
                int ry = ClampInt(cy0 + y, 0, coarse - 1);
                for (int x = 0; x < w; x++)
                {
                    int rx = ClampInt(cx0 + x, 0, coarse - 1);
                    int r = rx + ry * coarse;
                    int i = x + y * w;
                    float value = region.Height[r];
                    source[i] = r;
                    isLand[i] = value > 0f;
                    sign[i] = value > 0f ? 1f : -1f;
                    landHeight[i] = value > 0f ? value : 0f;
                    seaDepth[i] = value > 0f ? 0f : -value;
                    ground[i] = value > 0f ? value : 0f;
                    if (lakeBank != null)
                        lakeBank[i] = region.LakeDepth[r] > 0f ? region.LakeDepth[r] : -Math.Max(0.1f, Math.Abs(value - lakeLevel));
                    temperature[i] = region.Temperature[r];
                    moisture[i] = region.Moisture[r];
                }
            }
            // 海のセルへ岸の標高を、陸のセルへ岸の水深を広げておく（補間が海岸をまたいでも値が飛ばないように）
            Extend(landHeight, isLand, true, w, h);
            Extend(seaDepth, isLand, false, w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = x + y * w;
                    if (!isLand[i]) continue;
                    float here = ground[i];
                    float most = 0f;
                    if (x > 0) most = Math.Max(most, Math.Abs(here - ground[i - 1]));
                    if (x < w - 1) most = Math.Max(most, Math.Abs(here - ground[i + 1]));
                    if (y > 0) most = Math.Max(most, Math.Abs(here - ground[i - w]));
                    if (y < h - 1) most = Math.Max(most, Math.Abs(here - ground[i + w]));
                    relief[i] = most;
                }
            }

            List<RiverPiece> pieces = BuildRiverPieces(region, cx0, cy0, w, h, riverThreshold, coarseCell);
            List<int>[] bins = BinRiverPieces(pieces, w, h, coarseCell);

            uint seed = unchecked((uint)region.Spec.Seed * 1597334677u + 3812015801u);
            float[] height = data.Height;
            float[] water = data.WaterCoverage;
            float detailThreshold = RegionGenerator.RiverThreshold(data);
            float roughness = spec.Roughness;
            data.LakeSurfaceLevel = lakeLevel;

            Parallel.For(0, res, y =>
            {
                for (int x = 0; x < res; x++)
                {
                    int i = x + y * res;
                    // 広域の格子での位置（セルの中心が整数）と、地域全体での位置（m）
                    float gx = gx0 + (x + 0.5f) * step;
                    float gy = gy0 + (y + 0.5f) * step;
                    double worldX = (gx + 0.5) * coarseCell;
                    double worldY = (gy + 0.5) * coarseCell;

                    // 読む位置を少しずらす。海岸・尾根・川が、広域の升目の向きに揃わなくなる（ずれは最大で広域の 0.4 セルほど）
                    float ax = (float)(worldX / 1100.0);
                    float ay = (float)(worldY / 1100.0);
                    float qx = gx - cx0 + 0.42f * RegionNoise.Fbm(ax + 31.7f, ay + 12.9f, 3, seed + 201u)
                        + 0.09f * RegionNoise.Fbm(ax * 5.3f + 4.1f, ay * 5.3f + 9.6f, 2, seed + 203u);
                    float qy = gy - cy0 + 0.42f * RegionNoise.Fbm(ax + 7.3f, ay + 44.2f, 3, seed + 202u)
                        + 0.09f * RegionNoise.Fbm(ax * 5.3f + 21.5f, ay * 5.3f + 2.8f, 2, seed + 204u);

                    float coast = Cubic(sign, w, h, qx, qy, false);
                    if (coast > 1f) coast = 1f; else if (coast < -1f) coast = -1f;
                    coast += 0.34f * RegionNoise.Fbm((float)(worldX / 300.0), (float)(worldY / 300.0), 5, seed + 205u)
                        * (1f - Math.Abs(coast));

                    float broad = RegionNoise.Fbm((float)(worldX / 420.0), (float)(worldY / 420.0), 5, seed + 211u);
                    float nearT = Bilinear(temperature, w, h, qx, qy);
                    float nearM = Bilinear(moisture, w, h, qx, qy);
                    float nearGround = Bilinear(ground, w, h, qx, qy);
                    float value;
                    float wet = 0f;

                    if (coast <= 0f)
                    {
                        float t = Math.Min(1f, -coast / 0.7f);
                        float ramp = 1f - (1f - t) * (1f - t);
                        float depth = Cubic(seaDepth, w, h, qx, qy, true) * ramp * (1f + 0.12f * broad);
                        value = -(depth > 0.2f ? depth : 0.2f);
                    }
                    else
                    {
                        float t = Math.Min(1f, coast / 0.7f);
                        float ramp = 1f - (1f - t) * (1f - t);
                        float baseHeight = Cubic(landHeight, w, h, qx, qy, true);
                        float localRelief = Bilinear(relief, w, h, qx, qy);
                        float ridged = RegionNoise.Ridged((float)(worldX / 260.0), (float)(worldY / 260.0), 4, seed + 212u);
                        float fine = RegionNoise.Fbm((float)(worldX / 46.0), (float)(worldY / 46.0), 3, seed + 213u);
                        float amplitude = (1.0f + 0.30f * localRelief) * roughness;
                        float bumps = amplitude * (0.75f * broad + 0.7f * (ridged - 0.45f)) + (0.5f + 0.05f * localRelief) * roughness * fine;
                        value = (baseHeight + bumps) * ramp;
                        if (value < 0.15f) value = 0.15f;

                        // 川：いちばん近い川筋を探し、川床を掘って水面を置く
                        float level, edge, width;
                        if (NearestRiver(pieces, bins, w, h, qx, qy, coarseCell, out level, out edge, out width))
                        {
                            float bank = Math.Max(14f, 2.2f * width);
                            // 川岸の湿り。岸から離れるほど薄れ、細い沢ほど弱い
                            float damp = 1f - (edge > 0f ? edge : 0f) / (40f + 2.5f * width);
                            if (damp > 0f) wet = damp * damp * Math.Min(1f, width / 9f);
                            if (edge < bank)
                            {
                                if (level < 0.3f) level = 0.3f;
                                if (edge <= 0f)
                                {
                                    value = level - Math.Min(2.5f, 0.5f + 0.03f * width);
                                    if (value < 0.1f) value = 0.1f;
                                }
                                else
                                {
                                    float k = RegionNoise.Smooth(0f, bank, edge);
                                    value = level + 0.4f + (value - level - 0.4f) * k;
                                }
                            }
                            // 岸の線をまたぐセルは、覆われた割合だけ水にする。1 セルより細い沢は薄く写る
                            float cover = 0.5f - edge / cell;
                            if (cover > 0f)
                            {
                                if (cover > 1f) cover = 1f;
                                water[i] = cover * Clamp(width / (1.1f * cell), 0.3f, 0.92f);
                            }
                        }
                    }

                    if (coast > 0f && lakeBank != null)
                    {
                        float depth = Bilinear(lakeBank, w, h, qx, qy);
                        if (depth > 0f)
                        {
                            value = lakeLevel - depth;
                            data.LakeDepth[i] = depth;
                            water[i] = 1f;
                        }
                    }
                    height[i] = value;
                    float groundNow = value > 0f ? value : 0f;
                    data.Temperature[i] = nearT - 6.5f * (groundNow - nearGround) / 1000f;
                    data.Moisture[i] = nearM;
                    // 流量は「谷底の湿り」の描画にだけ使う。広域の流量を補間すると升目の形が出るので、川岸からの距離で決める
                    data.Flow[i] = wet * detailThreshold;
                    int sx = ClampInt((int)Math.Floor(qx + 0.5f), 0, w - 1);
                    int sy = ClampInt((int)Math.Floor(qy + 0.5f), 0, h - 1);
                    data.Area[i] = region.Area[source[sx + sy * w]];
                }
            });

            if (spec.GullyPasses > 0) CarveGullies(data, spec.GullyPasses, seed);
            Classify(data);
            Measure(detail, gx0, gy0, step);
            return detail;
        }

        /// <summary>
        /// 斜面の水を細かい格子で集め、集まった所を少し削る。削るだけで盛らないので、海岸は動かない。
        /// 水面のあるセル（広域から引いた川）は高さを変えず、そこが周りの谷の出口になる。
        /// </summary>
        private static void CarveGullies(RegionData data, int passes, uint seed)
        {
            int n = data.Resolution;
            int count = n * n;
            float[] h = data.Height;
            float[] water = data.WaterCoverage;
            float cell = data.CellMeters;
            var land = new bool[count];
            for (int i = 0; i < count; i++) land[i] = h[i] > 0f;
            var router = new FlowRouter(n) { Jitter = 0.6f, JitterSeed = seed + 221u };
            var area = new float[count];
            // 1 セルの面積（ha）。集まる水の量を面積で数えるので、格子の細かさを変えても同じ場所が同じだけ削れる
            float cellHectares = cell * cell / 10000f;
            // 流れ先を決め直すのは 3 回に 1 回。その間は同じ流れ先のまま削る（流れ先の計算がいちばん重い）
            const int SweepsPerRoute = 3;

            for (int pass = 0; pass < passes; pass++)
            {
                if (pass % SweepsPerRoute == 0)
                {
                    router.Route(h, land);
                    router.Accumulate(null, land, area);
                }
                for (int o = 0; o < router.Count; o++)
                {
                    int i = router.Order[o];
                    int r = router.Receiver[i];
                    if (r < 0 || water[i] > 0.05f) continue;
                    float downstream = land[r] ? h[r] : 0f;
                    if (h[i] <= downstream) continue;
                    int diff = r - i;
                    bool diagonal = diff == n + 1 || diff == n - 1 || diff == -n + 1 || diff == -n - 1;
                    float hectares = area[i] * cellHectares;
                    // 1 ha より少ない水は削らない（尾根や斜面の上はそのまま）
                    if (hectares < 1f) continue;
                    float f = 0.6f * (float)Math.Sqrt(hectares);
                    if (diagonal) f *= 0.70710678f;
                    if (f > 5f) f = 5f;
                    float next = (h[i] + f * downstream) / (1f + f);
                    // 1 回で削る深さに上限を置く（広域の谷の形を保つ）
                    float limit = 2f + 0.35f * (float)Math.Sqrt(hectares);
                    if (limit > 8f) limit = 8f;
                    if (h[i] - next > limit) next = h[i] - limit;
                    if (next < 0.1f) next = 0.1f;
                    h[i] = next;
                }
            }

            // 削った溝は 1 セル幅で壁が立つので、周りとならして谷の形にする。水面と海岸のセルは動かさない。
            // ならす幅をメートルで揃えるため、回数はセルが細かいほど増やす（1 セル 8 m で 2 回、2 m で 30 回）
            int smoothing = (int)Math.Round(2.0 * (7.8 / cell) * (7.8 / cell));
            if (smoothing < 1) smoothing = 1; else if (smoothing > 40) smoothing = 40;
            var previous = new float[count];
            for (int pass = 0; pass < smoothing; pass++)
            {
                Array.Copy(h, previous, count);
                Parallel.For(1, n - 1, y =>
                {
                    for (int x = 1; x < n - 1; x++)
                    {
                        int i = x + y * n;
                        if (!land[i] || water[i] > 0.05f) continue;
                        float a = previous[i - 1];
                        float b = previous[i + 1];
                        float c = previous[i - n];
                        float d = previous[i + n];
                        if (a <= 0f || b <= 0f || c <= 0f || d <= 0f) continue;
                        h[i] = previous[i] + 0.2f * (a + b + c + d - 4f * previous[i]);
                    }
                });
            }

            // 谷底の湿り（描画で緑が濃くなる）に、窓の中で集まった水も数える。2,000 ha が広域の川の下限にあたる
            float threshold = RegionGenerator.RiverThreshold(data);
            for (int i = 0; i < count; i++)
            {
                if (!land[i]) continue;
                float local = area[i] * cellHectares / 2000f * threshold;
                if (local > data.Flow[i]) data.Flow[i] = local;
            }
        }

        private static void Classify(RegionData data)
        {
            int n = data.Resolution;
            float[] h = data.Height;
            float cell = data.CellMeters;
            int landCount = 0;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = x + y * n;
                    if (h[i] <= 0f)
                    {
                        data.Biome[i] = RegionBiome.Ocean;
                        continue;
                    }
                    landCount++;
                    if (data.LakeDepth[i] > 0f)
                    {
                        data.Biome[i] = RegionBiome.Lake;
                        continue;
                    }
                    if (data.WaterCoverage[i] > 0.5f)
                    {
                        data.Biome[i] = RegionBiome.River;
                        continue;
                    }
                    float slope = RegionGenerator.SlopeAt(h, n, x, y, cell);
                    data.Biome[i] = RegionGenerator.LandBiome(data.Temperature[i], data.Moisture[i], slope, h[i]);
                }
            }
            data.LandFraction = landCount / (float)(n * n);
        }

        /// <summary>広域の地図との一致を数える（海陸、標高、川）。</summary>
        private static void Measure(RegionDetail detail, float gx0, float gy0, float step)
        {
            RegionData region = detail.Region;
            RegionData data = detail.Data;
            int coarse = region.Resolution;
            int res = data.Resolution;
            float threshold = RegionGenerator.RiverThreshold(region);

            int same = 0;
            for (int y = 0; y < res; y++)
            {
                int ry = ClampInt((int)Math.Floor(gy0 + (y + 0.5f) * step + 0.5f), 0, coarse - 1);
                for (int x = 0; x < res; x++)
                {
                    int rx = ClampInt((int)Math.Floor(gx0 + (x + 0.5f) * step + 0.5f), 0, coarse - 1);
                    if ((data.Height[x + y * res] > 0f) == (region.Height[rx + ry * coarse] > 0f)) same++;
                }
            }
            detail.CoastAgreement = same / (float)(res * res);

            double sum = 0.0;
            int samples = 0;
            int rivers = 0;
            int riversKept = 0;
            int reach = Math.Max(1, (int)Math.Ceiling(0.75f / step));
            int rx0 = (int)Math.Ceiling(gx0);
            int ry0 = (int)Math.Ceiling(gy0);
            for (int ry = ry0; ry <= (int)Math.Floor(gy0 + step * res) ; ry++)
            {
                for (int rx = rx0; rx <= (int)Math.Floor(gx0 + step * res); rx++)
                {
                    if (rx < 0 || ry < 0 || rx >= coarse || ry >= coarse) continue;
                    int r = rx + ry * coarse;
                    if (region.Height[r] <= 0f) continue;
                    int px = (int)((rx - gx0) / step);
                    int py = (int)((ry - gy0) / step);
                    if (px < 0 || py < 0 || px >= res || py >= res) continue;
                    bool river = region.Flow[r] > threshold;
                    if (!river)
                    {
                        float value = data.Height[px + py * res];
                        sum += Math.Abs((value > 0f ? value : 0f) - region.Height[r]);
                        samples++;
                        continue;
                    }
                    if (px < reach || py < reach || px >= res - reach || py >= res - reach) continue;
                    rivers++;
                    bool found = false;
                    for (int dy = -reach; dy <= reach && !found; dy++)
                    {
                        for (int dx = -reach; dx <= reach && !found; dx++)
                        {
                            found = data.WaterCoverage[px + dx + (py + dy) * res] > 0.05f;
                        }
                    }
                    if (found) riversKept++;
                }
            }
            detail.MeanHeightDifference = samples > 0 ? (float)(sum / samples) : 0f;
            detail.RiverAgreement = rivers > 0 ? riversKept / (float)rivers : 1f;
        }

        /// <summary>
        /// 広域の川のセルを、流れ先へ向かう曲線の短い線分にする。位置は切り出した格子の添字（セルの中心が整数）。
        /// 曲線は、上流の主なセル・自分・流れ先・その先の 4 点を通る Catmull-Rom。
        /// </summary>
        private static List<RiverPiece> BuildRiverPieces(RegionData region, int cx0, int cy0, int w, int h, float threshold, float coarseCell)
        {
            var pieces = new List<RiverPiece>();
            int n = region.Resolution;
            int[] downstream = region.Downstream;
            if (downstream == null) return pieces;
            float[] flow = region.Flow;
            float[] height = region.Height;
            float least = threshold * SmallStreamRatio;

            for (int y = 0; y < h; y++)
            {
                int ry = cy0 + y;
                if (ry < 0 || ry >= n) continue;
                for (int x = 0; x < w; x++)
                {
                    int rx = cx0 + x;
                    if (rx < 0 || rx >= n) continue;
                    int i = rx + ry * n;
                    if (height[i] <= 0f || flow[i] <= least) continue;
                    int r = downstream[i];
                    if (r < 0) continue;

                    float p1x = x;
                    float p1y = y;
                    float p2x = r % n - cx0;
                    float p2y = r / n - cy0;
                    float p0x = p1x - (p2x - p1x);
                    float p0y = p1y - (p2y - p1y);
                    float best = least * 0.5f;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int jx = rx + dx;
                            int jy = ry + dy;
                            if (jx < 0 || jy < 0 || jx >= n || jy >= n) continue;
                            int j = jx + jy * n;
                            if (downstream[j] != i || height[j] <= 0f || flow[j] <= best) continue;
                            best = flow[j];
                            p0x = x + dx;
                            p0y = y + dy;
                        }
                    }
                    float p3x = p2x + (p2x - p1x);
                    float p3y = p2y + (p2y - p1y);
                    bool receiverLand = height[r] > 0f;
                    if (receiverLand && downstream[r] >= 0)
                    {
                        p3x = downstream[r] % n - cx0;
                        p3y = downstream[r] / n - cy0;
                    }

                    float z1 = height[i];
                    float z2 = receiverLand ? height[r] : 0f;
                    if (z2 > z1) z2 = z1;
                    float w1 = RiverWidth(flow[i] / threshold);
                    float w2 = receiverLand ? RiverWidth(flow[r] / threshold) : w1;
                    float lastX = p1x;
                    float lastY = p1y;
                    for (int s = 1; s <= RiverSteps; s++)
                    {
                        float t = s / (float)RiverSteps;
                        float t2 = t * t;
                        float t3 = t2 * t;
                        float nextX = 0.5f * (2f * p1x + (p2x - p0x) * t + (2f * p0x - 5f * p1x + 4f * p2x - p3x) * t2 + (3f * p1x - p0x - 3f * p2x + p3x) * t3);
                        float nextY = 0.5f * (2f * p1y + (p2y - p0y) * t + (2f * p0y - 5f * p1y + 4f * p2y - p3y) * t2 + (3f * p1y - p0y - 3f * p2y + p3y) * t3);
                        float t0 = (s - 1) / (float)RiverSteps;
                        pieces.Add(new RiverPiece
                        {
                            Ax = lastX, Ay = lastY, Bx = nextX, By = nextY,
                            Za = z1 + (z2 - z1) * t0, Zb = z1 + (z2 - z1) * t,
                            Wa = w1 + (w2 - w1) * t0, Wb = w1 + (w2 - w1) * t,
                        });
                        lastX = nextX;
                        lastY = nextY;
                    }
                }
            }
            return pieces;
        }

        /// <summary>
        /// 川の幅（m）。ratio は「広域の地図に川として描く下限」に対する流量の比。
        /// 下限ちょうどで 9 m、その 80 倍で 79 m。下限より少ない沢は 3〜9 m。
        /// </summary>
        private static float RiverWidth(float ratio)
        {
            if (ratio < 1f) return 9f * (float)Math.Sqrt(ratio);
            float k = (float)(Math.Log(ratio) / Math.Log(80.0));
            if (k > 1f) k = 1f;
            return 9f + 70f * (float)Math.Pow(k, 1.2);
        }

        private static float RiverReach(float width, float coarseCell)
        {
            return (width * 0.5f + Math.Max(14f, 2.2f * width)) / coarseCell + 0.05f;
        }

        private static List<int>[] BinRiverPieces(List<RiverPiece> pieces, int w, int h, float coarseCell)
        {
            var bins = new List<int>[w * h];
            for (int p = 0; p < pieces.Count; p++)
            {
                RiverPiece piece = pieces[p];
                float reach = RiverReach(Math.Max(piece.Wa, piece.Wb), coarseCell);
                int x0 = ClampInt((int)Math.Floor(Math.Min(piece.Ax, piece.Bx) - reach + 0.5f), 0, w - 1);
                int x1 = ClampInt((int)Math.Floor(Math.Max(piece.Ax, piece.Bx) + reach + 0.5f), 0, w - 1);
                int y0 = ClampInt((int)Math.Floor(Math.Min(piece.Ay, piece.By) - reach + 0.5f), 0, h - 1);
                int y1 = ClampInt((int)Math.Floor(Math.Max(piece.Ay, piece.By) + reach + 0.5f), 0, h - 1);
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        int b = x + y * w;
                        if (bins[b] == null) bins[b] = new List<int>();
                        bins[b].Add(p);
                    }
                }
            }
            return bins;
        }

        /// <summary>
        /// いちばん近い川筋。level は水面の高さ（m）、edge は岸からの距離（m。川の中は負）、width は川幅（m）。
        /// </summary>
        private static bool NearestRiver(List<RiverPiece> pieces, List<int>[] bins, int w, int h, float qx, float qy, float coarseCell,
            out float level, out float edge, out float width)
        {
            level = 0f;
            edge = float.MaxValue;
            width = 0f;
            int bx = (int)Math.Floor(qx + 0.5f);
            int by = (int)Math.Floor(qy + 0.5f);
            if (bx < 0 || by < 0 || bx >= w || by >= h) return false;
            List<int> bin = bins[bx + by * w];
            if (bin == null) return false;
            for (int b = 0; b < bin.Count; b++)
            {
                RiverPiece piece = pieces[bin[b]];
                float dx = piece.Bx - piece.Ax;
                float dy = piece.By - piece.Ay;
                float length = dx * dx + dy * dy;
                float t = length > 1e-8f ? ((qx - piece.Ax) * dx + (qy - piece.Ay) * dy) / length : 0f;
                if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
                float ox = qx - (piece.Ax + dx * t);
                float oy = qy - (piece.Ay + dy * t);
                float pieceWidth = piece.Wa + (piece.Wb - piece.Wa) * t;
                float distance = (float)Math.Sqrt(ox * ox + oy * oy) * coarseCell - pieceWidth * 0.5f;
                if (distance < edge)
                {
                    edge = distance;
                    width = pieceWidth;
                    level = piece.Za + (piece.Zb - piece.Za) * t;
                }
            }
            return edge < float.MaxValue;
        }

        /// <summary>fill が true なら海のセルへ、false なら陸のセルへ、隣の値の平均を 2 回広げる。</summary>
        private static void Extend(float[] values, bool[] isLand, bool fillSea, int w, int h)
        {
            int count = w * h;
            var known = new bool[count];
            for (int i = 0; i < count; i++) known[i] = isLand[i] == fillSea;
            var next = new float[count];
            var nextKnown = new bool[count];
            for (int pass = 0; pass < 2; pass++)
            {
                Array.Copy(values, next, count);
                Array.Copy(known, nextKnown, count);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int i = x + y * w;
                        if (known[i]) continue;
                        float sum = 0f;
                        int found = 0;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx;
                                int ny = y + dy;
                                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                                int j = nx + ny * w;
                                if (!known[j]) continue;
                                sum += values[j];
                                found++;
                            }
                        }
                        if (found == 0) continue;
                        next[i] = sum / found;
                        nextKnown[i] = true;
                    }
                }
                Array.Copy(next, values, count);
                Array.Copy(nextKnown, known, count);
            }
        }

        /// <summary>
        /// Catmull-Rom の 3 次補間。limit が true なら、結果を真ん中の 4 点の最小〜最大に収める
        /// （海岸のような段差の近くで、補間が行き過ぎて凹みやこぶを作らないように）。
        /// </summary>
        private static float Cubic(float[] values, int w, int h, float x, float y, bool limit)
        {
            int x1 = (int)Math.Floor(x);
            int y1 = (int)Math.Floor(y);
            float fx = x - x1;
            float fy = y - y1;
            float wx0 = ((-0.5f * fx + 1f) * fx - 0.5f) * fx;
            float wx1 = (1.5f * fx - 2.5f) * fx * fx + 1f;
            float wx2 = ((-1.5f * fx + 2f) * fx + 0.5f) * fx;
            float wx3 = (0.5f * fx - 0.5f) * fx * fx;
            float wy0 = ((-0.5f * fy + 1f) * fy - 0.5f) * fy;
            float wy1 = (1.5f * fy - 2.5f) * fy * fy + 1f;
            float wy2 = ((-1.5f * fy + 2f) * fy + 0.5f) * fy;
            float wy3 = (0.5f * fy - 0.5f) * fy * fy;

            int xa = ClampInt(x1 - 1, 0, w - 1);
            int xb = ClampInt(x1, 0, w - 1);
            int xc = ClampInt(x1 + 1, 0, w - 1);
            int xd = ClampInt(x1 + 2, 0, w - 1);
            float result = 0f;
            for (int k = 0; k < 4; k++)
            {
                int row = ClampInt(y1 - 1 + k, 0, h - 1) * w;
                float line = values[row + xa] * wx0 + values[row + xb] * wx1 + values[row + xc] * wx2 + values[row + xd] * wx3;
                result += line * (k == 0 ? wy0 : (k == 1 ? wy1 : (k == 2 ? wy2 : wy3)));
            }
            if (!limit) return result;

            int ya = ClampInt(y1, 0, h - 1) * w;
            int yb = ClampInt(y1 + 1, 0, h - 1) * w;
            float a = values[ya + xb];
            float b = values[ya + xc];
            float c = values[yb + xb];
            float d = values[yb + xc];
            float low = Math.Min(Math.Min(a, b), Math.Min(c, d));
            float high = Math.Max(Math.Max(a, b), Math.Max(c, d));
            return result < low ? low : (result > high ? high : result);
        }

        private static float Bilinear(float[] values, int w, int h, float x, float y)
        {
            int x0 = (int)Math.Floor(x);
            int y0 = (int)Math.Floor(y);
            float fx = x - x0;
            float fy = y - y0;
            int xa = ClampInt(x0, 0, w - 1);
            int xb = ClampInt(x0 + 1, 0, w - 1);
            int ya = ClampInt(y0, 0, h - 1) * w;
            int yb = ClampInt(y0 + 1, 0, h - 1) * w;
            float top = values[ya + xa] + (values[ya + xb] - values[ya + xa]) * fx;
            float bottom = values[yb + xa] + (values[yb + xb] - values[yb + xa]) * fx;
            return top + (bottom - top) * fy;
        }

        private static float Clamp(float v, float low, float high)
        {
            return v < low ? low : (v > high ? high : v);
        }

        private static int ClampInt(int v, int low, int high)
        {
            return v < low ? low : (v > high ? high : v);
        }
    }
}
