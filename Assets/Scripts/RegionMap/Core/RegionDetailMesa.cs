using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 砂岩の台地（RegionDetailSpec.Mesa）。見え方の想定（docs/02_design/REGION_TERRAIN_LOOK_SPEC.md）の断面を、
    /// 拡大した地形の上に載せる。上から順に、平らな上面 → 帽子の硬い層の崖 → やわらかい層の岩屑の斜面 → 棚 → 次の硬い層の崖 → 床。
    ///
    /// 作り方:
    /// 1. 帽子の岩が残っている範囲を、地域全体の座標（m）で引くノイズから決める（広い台地）。台地の近くには、離れ岩（ビュートと尖塔）を置く。
    ///    範囲は、2 方向の縦の割れ目で区切った岩の塊ごとに決めるので、崖の線は直線の板と直角の角でできる。
    ///    縁の塊は、低く欠けたり、段になって崩れ落ちたり、割れ目で壁から離れたりする。
    /// 2. 帽子の岩からの距離を測り、距離から断面を作る。岩屑の斜面は安息角（約 34 度）で一定に下る。
    /// 3. 床（広域から引いた標高に細部を足したもの）より高い所だけを置き換える。海・川・湖のセルは動かさない。
    ///
    /// 帽子の上面の高さは、広域の標高を広くならした面の上 156 m。台地ごとに高さが揃う（同じ層が残った物なので）。
    /// 乾いていて（湿り気 0.24 以下で全面、0.34 以上で無し）、広域で平らで、海岸と川から離れた所にだけ立つ。
    /// 位置はすべて地域全体の座標で決めるので、窓の位置や大きさを変えても、同じ場所には同じ台地が出る
    /// （格子が粗い窓では、幅 1〜4 m の割れ目は写らない）。
    /// </summary>
    public static partial class RegionDetailBuilder
    {
        /// <summary>帽子の硬い砂岩の厚さ＝崖の高さ（m）。</summary>
        public const float MesaCapThickness = 86f;

        /// <summary>帽子の下の、やわらかい層の厚さ（m）。岩屑の斜面になる。</summary>
        public const float MesaSoftThickness = 34f;

        /// <summary>棚の縁の、次の硬い層の崖の高さ（m）。</summary>
        public const float MesaLowerCliff = 20f;

        /// <summary>次の崖の足もとから、ならした床までの高さ（m）。</summary>
        private const float MesaFootClearance = 16f;

        /// <summary>岩屑の斜面の傾き（高さ / 水平距離。約 34 度）。</summary>
        private const float MesaTalusTan = 0.67f;

        /// <summary>帽子の岩から、裾（斜面・棚・次の崖）が届く最大の距離（m）。窓の外をこの幅だけ余分に調べる。</summary>
        private const float MesaSkirtMeters = 270f;

        private const float MesaThreshold = 0.17f;

        /// <summary>「縁からどれだけ内側か」を、ノイズの値の差から距離へ直す目安（1 m あたり）。</summary>
        private const float MesaMarginPerMeter = 0.0011f;

        private const float MesaTotalHeight = MesaFootClearance + MesaLowerCliff + MesaSoftThickness + MesaCapThickness;

        private sealed class MesaInput
        {
            public RegionData Region;
            public float[] Sign, LandHeight, Ground, Relief, Moisture, Temperature;
            public int W, H, Cx0, Cy0;
            public float Gx0, Gy0, Step, CoarseCell;
            public uint Seed;
            public List<RiverPiece> Pieces;
            public List<int>[] Bins;
        }

        private struct MesaOutlier
        {
            public double X, Y;
            public float Radius;
        }

        private static void ApplyMesas(RegionData data, RegionDetailSpec spec, MesaInput input)
        {
            int res = data.Resolution;
            float cell = data.CellMeters;
            uint seed = input.Seed;
            float amount = spec.Mesa > 1f ? 1f : spec.Mesa;
            bool anyClimate = spec.MesaAnyClimate;
            int pad = (int)Math.Ceiling(MesaSkirtMeters / cell);
            int n = res + 2 * pad;
            int w = input.W, h = input.H;
            float coarseCell = input.CoarseCell;

            // 帽子の上面の基準：広域の標高を広くならした面（広域の配列から直接ならすので、窓によらない）
            float[] capBase = MesaCapBase(input);

            // 割れ目の向き（地域ごとに seed で決まる）。2 組は直角に交わる
            double jointAngle = RegionNoise.Hash01(5, 11, seed + 260u) * Math.PI;
            double ca = Math.Cos(jointAngle), sa = Math.Sin(jointAngle);
            // 幅 1〜4 m の割れ目は、格子が粗いと点々になるので薄める
            float slotScale = RegionNoise.Clamp01((3.2f - cell) / 1.6f);

            // 離れ岩の候補を、窓にかかる升目のぶんだけ先に作る
            const double OutlierGrid = 230.0;
            double worldLeft = (input.Gx0 + (0.5f - pad) * input.Step + 0.5) * coarseCell;
            double worldTop = (input.Gy0 + (0.5f - pad) * input.Step + 0.5) * coarseCell;
            double worldSpan = n * (double)cell;
            int ox0 = (int)Math.Floor(worldLeft / OutlierGrid) - 1;
            int oy0 = (int)Math.Floor(worldTop / OutlierGrid) - 1;
            int ow = (int)Math.Floor((worldLeft + worldSpan) / OutlierGrid) + 2 - ox0;
            int oh = (int)Math.Floor((worldTop + worldSpan) / OutlierGrid) + 2 - oy0;
            var outliers = new MesaOutlier[ow * oh];
            for (int gy = 0; gy < oh; gy++)
            {
                for (int gx = 0; gx < ow; gx++)
                {
                    int cx = ox0 + gx, cy = oy0 + gy;
                    double px = (cx + 0.2 + 0.6 * RegionNoise.Hash01(cx, cy, seed + 252u)) * OutlierGrid;
                    double py = (cy + 0.2 + 0.6 * RegionNoise.Hash01(cx, cy, seed + 253u)) * OutlierGrid;
                    float field = MesaField(px, py, seed);
                    // 台地の中と、台地から遠い所には置かない（台地の縁から痩せて離れた岩なので）
                    float chance = field > -0.025f ? 0f : 0.36f * RegionNoise.Smooth(-0.30f, -0.07f, field);
                    float radius = 0f;
                    if (RegionNoise.Hash01(cx, cy, seed + 251u) < chance)
                    {
                        float size = RegionNoise.Hash01(cx, cy, seed + 255u);
                        radius = RegionNoise.Hash01(cx, cy, seed + 254u) < 0.42f ? 8f + 9f * size : 22f + 60f * size * size;
                    }
                    outliers[gx + gy * ow] = new MesaOutlier { X = px, Y = py, Radius = radius };
                }
            }

            var standing = new byte[n * n];
            var outside = new byte[n * n];
            var capTop = new float[n * n];
            var capLevel = new float[n * n];
            var skirt = new float[n * n];
            int found = 0;

            // 1 段目：帽子の岩が残る、なめらかな範囲
            var smoothIn = new byte[n * n];
            var smoothOut = new byte[n * n];
            Parallel.For(0, n, () => 0, (py, state, local) =>
            {
                for (int px = 0; px < n; px++)
                {
                    int j = px + py * n;
                    capTop[j] = float.NegativeInfinity;
                    outside[j] = 1;
                    smoothOut[j] = 1;
                    float gx = input.Gx0 + (px - pad + 0.5f) * input.Step;
                    float gy = input.Gy0 + (py - pad + 0.5f) * input.Step;
                    double worldX = (gx + 0.5) * coarseCell;
                    double worldY = (gy + 0.5) * coarseCell;

                    // 読む位置のずらし方は、地形の本体（Build）と同じ式。川と海岸の位置を本体と揃えるため
                    float ax = (float)(worldX / 1100.0);
                    float ay = (float)(worldY / 1100.0);
                    float qx = gx - input.Cx0 + 0.42f * RegionNoise.Fbm(ax + 31.7f, ay + 12.9f, 3, seed + 201u)
                        + 0.09f * RegionNoise.Fbm(ax * 5.3f + 4.1f, ay * 5.3f + 9.6f, 2, seed + 203u);
                    float qy = gy - input.Cy0 + 0.42f * RegionNoise.Fbm(ax + 7.3f, ay + 44.2f, 3, seed + 202u)
                        + 0.09f * RegionNoise.Fbm(ax * 5.3f + 21.5f, ay * 5.3f + 2.8f, 2, seed + 204u);

                    float coast = Cubic(input.Sign, w, h, qx, qy, false);
                    if (coast <= 0.05f) continue;
                    float level = Cubic(capBase, w, h, qx, qy, true) + MesaTotalHeight;
                    capLevel[j] = level;

                    // 裾は、海岸と川岸の手前で消える（海と川のセルは埋めない）
                    float riverLevel, riverEdge, riverWidth;
                    bool river = NearestRiver(input.Pieces, input.Bins, w, h, qx, qy, coarseCell, out riverLevel, out riverEdge, out riverWidth);
                    float reach = RegionNoise.Smooth(0.15f, 0.5f, coast);
                    if (river) reach *= RegionNoise.Smooth(3f, 28f, riverEdge);
                    skirt[j] = reach;
                    if (reach <= 0f) continue;

                    // 台地が立てる場所か（乾き・平ら・内陸・川から離れている）
                    float gate = RegionNoise.Smooth(0.5f, 0.9f, coast);
                    gate *= RegionNoise.Smooth(0.16f, 0.07f, Bilinear(input.Relief, w, h, qx, qy) / coarseCell);
                    if (!anyClimate)
                    {
                        gate *= RegionNoise.Smooth(0.34f, 0.24f, Bilinear(input.Moisture, w, h, qx, qy));
                        gate *= RegionNoise.Smooth(1f, 6f, Bilinear(input.Temperature, w, h, qx, qy));
                    }
                    if (river) gate *= RegionNoise.Smooth(35f + 1.5f * riverWidth, 95f + 2f * riverWidth, riverEdge);
                    gate *= amount;
                    if (gate <= 0.02f) continue;

                    float margin = MesaField(worldX, worldY, seed);
                    float lone = MesaOutlierMargin(outliers, ox0, oy0, ow, oh, OutlierGrid, worldX, worldY, seed);
                    if (lone > margin) margin = lone;
                    if (margin - (1f - gate) * 0.7f <= 0f) continue;
                    smoothIn[j] = 1;
                    smoothOut[j] = 0;
                    local++;
                }
                return local;
            }, local => System.Threading.Interlocked.Add(ref found, local));

            if (found == 0) return;

            // なめらかな縁からの距離（m。内側が正）を測り、割れ目で区切った岩の塊ごとに縁を前後させる。
            // 距離で前後させるので、ノイズの傾きがゆるい所でも、縁の出入りの幅は変わらない
            float[] inward = MesaDistance(smoothOut, n, null, null, cell);
            float[] outward = MesaDistance(smoothIn, n, null, null, cell);
            Parallel.For(0, n, py =>
            {
                for (int px = 0; px < n; px++)
                {
                    int j = px + py * n;
                    if (skirt[j] <= 0f) continue;
                    if (inward[j] > 14f) { outside[j] = 0; continue; }
                    if (outward[j] > 14f) continue;
                    double worldX = worldLeft + px * (double)cell;
                    double worldY = worldTop + py * (double)cell;
                    // 割れ目の座標（2 方向）と、その場所の岩の塊
                    double u = worldX * ca + worldY * sa + 9f * RegionNoise.Fbm((float)(worldX / 520.0) + 2.2f, (float)(worldY / 520.0) + 6.4f, 2, seed + 261u);
                    double v = -worldX * sa + worldY * ca + 9f * RegionNoise.Fbm((float)(worldX / 520.0) + 8.8f, (float)(worldY / 520.0) + 1.5f, 2, seed + 262u);
                    // 塊は 3 段：大きな板（34×48 m）、柱（9×12.5 m）、柱の面の出入り（4.2×5.5 m）
                    int fu, fv, bu, bv, mu, mv;
                    float fUc, fVc, fUd, fVd, bUc, bVc, bUd, bVd, mUc, mVc, mUd, mVd;
                    JointBlock(u, 4.2f, seed + 275u, out fu, out fUc, out fUd);
                    JointBlock(v, 5.5f, seed + 276u, out fv, out fVc, out fVd);
                    JointBlock(u, 9f, seed + 271u, out bu, out bUc, out bUd);
                    JointBlock(v, 12.5f, seed + 272u, out bv, out bVc, out bVd);
                    JointBlock(u, 34f, seed + 273u, out mu, out mUc, out mUd);
                    JointBlock(v, 48f, seed + 274u, out mv, out mVc, out mVd);
                    // いちばん細かい塊の中心で、縁からの距離を読む
                    int centerPx = ClampInt(px + (int)Math.Round((fUc * ca - fVc * sa) / cell), 0, n - 1);
                    int centerPy = ClampInt(py + (int)Math.Round((fUc * sa + fVc * ca) / cell), 0, n - 1);
                    int c = centerPx + centerPy * n;
                    if (skirt[c] <= 0f) continue;
                    // 板・柱・面ごとに、縁の位置が前後する（±10 m、±5 m、±1.4 m）
                    float shift = (RegionNoise.Hash01(mu, mv, seed + 266u) - 0.5f) * 20f + (RegionNoise.Hash01(bu, bv, seed + 263u) - 0.5f) * 10f
                        + (RegionNoise.Hash01(fu, fv, seed + 269u) - 0.5f) * 2.8f;
                    if (inward[c] - outward[c] + shift > 0f) outside[j] = 0;
                }
            });

            // 2 段目：縁からの距離（m）で、欠け・崩れ・割れ目を決める。縁から離れた上面には手を入れない（平らなまま）
            float[] toEdge = MesaDistance(outside, n, null, null, cell);
            // 幅 4 m ほどより細い板や針は立っていられないので取り除く（柱 1 本ぶんの太さがあれば残る）
            if (cell < 2.2f)
            {
                var core = new byte[n * n];
                for (int j = 0; j < core.Length; j++) core[j] = (byte)(toEdge[j] > 2.2f ? 1 : 0);
                float[] toCore = MesaDistance(core, n, null, null, cell);
                for (int j = 0; j < core.Length; j++) if (outside[j] == 0 && toCore[j] > 2.2f + 0.5f * cell) outside[j] = 1;
            }
            else
            {
                // 粗い格子では、1 セルだけ立った物を取り除く
                for (int pass = 0; pass < 2; pass++)
                {
                    var lone = new List<int>();
                    for (int py = 1; py < n - 1; py++)
                    {
                        for (int px = 1; px < n - 1; px++)
                        {
                            int j = px + py * n;
                            if (outside[j] != 0) continue;
                            int around = (outside[j - 1] == 0 ? 1 : 0) + (outside[j + 1] == 0 ? 1 : 0) + (outside[j - n] == 0 ? 1 : 0) + (outside[j + n] == 0 ? 1 : 0);
                            if (around < 2) lone.Add(j);
                        }
                    }
                    foreach (int j in lone) outside[j] = 1;
                }
            }
            toEdge = MesaDistance(outside, n, null, null, cell);
            Parallel.For(0, n, py =>
            {
                for (int px = 0; px < n; px++)
                {
                    int j = px + py * n;
                    if (outside[j] != 0) continue;
                    double worldX = worldLeft + px * (double)cell;
                    double worldY = worldTop + py * (double)cell;
                    double u = worldX * ca + worldY * sa + 9f * RegionNoise.Fbm((float)(worldX / 520.0) + 2.2f, (float)(worldY / 520.0) + 6.4f, 2, seed + 261u);
                    double v = -worldX * sa + worldY * ca + 9f * RegionNoise.Fbm((float)(worldX / 520.0) + 8.8f, (float)(worldY / 520.0) + 1.5f, 2, seed + 262u);
                    int fu, fv, bu, bv, mu, mv;
                    float fUc, fVc, fUd, fVd, bUc, bVc, bUd, bVd, mUc, mVc, mUd, mVd;
                    JointBlock(u, 4.2f, seed + 275u, out fu, out fUc, out fUd);
                    JointBlock(v, 5.5f, seed + 276u, out fv, out fVc, out fVd);
                    JointBlock(u, 9f, seed + 271u, out bu, out bUc, out bUd);
                    JointBlock(v, 12.5f, seed + 272u, out bv, out bVc, out bVd);
                    JointBlock(u, 34f, seed + 273u, out mu, out mUc, out mUd);
                    JointBlock(v, 48f, seed + 274u, out mv, out mVc, out mVd);
                    float hashBlock2 = RegionNoise.Hash01(bu, bv, seed + 264u);
                    float hashBlock3 = RegionNoise.Hash01(bu, bv, seed + 265u);
                    float hashFine = RegionNoise.Hash01(fu, fv, seed + 269u);
                    float edge = toEdge[j];
                    // 柱ごとに決める物は、柱の中心で縁からの距離を読む（柱の途中で切れないように）
                    int columnPx = ClampInt(px + (int)Math.Round((bUc * ca - bVc * sa) / cell), 0, n - 1);
                    int columnPy = ClampInt(py + (int)Math.Round((bUc * sa + bVc * ca) / cell), 0, n - 1);
                    float columnEdge = toEdge[columnPx + columnPy * n];

                    // 縁の柱は欠ける。低く欠ける物、途中の段になる物、段々に崩れ落ちる所
                    float drop = 0f;
                    if (columnEdge < 12f)
                    {
                        if (hashBlock2 < 0.26f) drop = MesaCapThickness * (0.02f + 0.10f * hashBlock3);
                        else if (hashBlock2 < 0.33f) drop = MesaCapThickness * (0.28f + 0.35f * hashBlock3);
                    }
                    // 上端の線を、細かな塊でぎざぎざにする
                    if (edge < 6f) drop += 2.5f * hashFine * hashFine;
                    if (columnEdge < 46f)
                    {
                        double columnX = worldX + bUc * ca - bVc * sa;
                        double columnY = worldY + bUc * sa + bVc * ca;
                        float breach = RegionNoise.Fbm((float)(columnX / 430.0) + 7.7f, (float)(columnY / 430.0) + 3.3f, 2, seed + 267u);
                        if (breach > 0.22f)
                        {
                            float fall = RegionNoise.Smooth(0.22f, 0.34f, breach) * (1f - columnEdge / 46f) * (0.8f + 0.5f * hashBlock3);
                            if (fall > 1f) fall = 1f;
                            if (fall * MesaCapThickness > drop) drop = fall * MesaCapThickness;
                        }
                    }
                    // 縦の割れ目。縁に近いほど深く開き、壁から柱や板を切り離す
                    if (slotScale > 0f && edge < 24f)
                    {
                        float deep = (0.92f - 0.75f * RegionNoise.Smooth(0f, 24f, edge)) * slotScale;
                        float slot = Math.Max(
                            JointSlot(mUd, mu, (int)Math.Floor(v / 26.0), 1.4f, 2.4f, 0.55f, seed + 281u),
                            JointSlot(mVd, mv, (int)Math.Floor(u / 26.0), 1.4f, 2.4f, 0.55f, seed + 282u));
                        if (slot > 0f) drop = Math.Max(drop, MesaCapThickness * deep * slot);
                        if (edge < 9f)
                        {
                            float crack = Math.Max(
                                JointSlot(bUd, bu, (int)Math.Floor(v / 12.0), 0.9f, 0.6f, 0.45f, seed + 283u),
                                JointSlot(bVd, bv, (int)Math.Floor(u / 12.0), 0.9f, 0.6f, 0.45f, seed + 284u));
                            if (crack > 0f) drop = Math.Max(drop, MesaCapThickness * (0.08f + 0.5f * hashBlock3) * crack * slotScale);
                        }
                    }

                    // 上面は平らで、ゆるい起伏だけがある
                    capTop[j] = capLevel[j] - drop + 0.9f * RegionNoise.Fbm((float)(worldX / 38.0), (float)(worldY / 38.0), 3, seed + 268u);
                    if (drop < 0.72f * MesaCapThickness) standing[j] = 1;
                }
            });

            float[] toCap = MesaDistance(standing, n, null, null, cell);
            // 棚の範囲：帽子の岩から、斜面の幅 + 棚の幅まで。棚の幅は場所で変わり、無い所もある
            var benchWidth = new float[n * n];
            Parallel.For(0, n, py =>
            {
                for (int px = 0; px < n; px++)
                {
                    int j = px + py * n;
                    if (skirt[j] <= 0f) { benchWidth[j] = -1f; continue; }
                    double worldX = worldLeft + px * (double)cell;
                    double worldY = worldTop + py * (double)cell;
                    float wide = 95f * (RegionNoise.Fbm((float)(worldX / 360.0) + 4.9f, (float)(worldY / 360.0) + 0.6f, 2, seed + 291u) + 0.30f);
                    // 広い台地から離れた離れ岩には、棚を付けない（岩屑の斜面だけで床へ下りる）
                    wide *= RegionNoise.Smooth(-0.13f, -0.04f, MesaField(worldX, worldY, seed));
                    if (wide < 8f) wide = 0f; else if (wide > 125f) wide = 125f;
                    // 縁の線は、数十 m の出入りでゆるく曲げる
                    if (wide > 0f) wide += 9f * RegionNoise.Fbm((float)(worldX / 45.0) + 2.6f, (float)(worldY / 45.0) + 8.3f, 2, seed + 292u);
                    benchWidth[j] = wide > 0f ? MesaSoftThickness / MesaTalusTan + wide : 0f;
                }
            });
            float[] toBench = MesaDistance(standing, n, benchWidth, toCap, cell);

            float[] height = data.Height;
            float[] water = data.WaterCoverage;
            var talus = new float[res * res];
            var caprock = new byte[res * res];
            var contactHeight = new float[res * res];
            var changed = new bool[res * res];
            Parallel.For(0, res, y =>
            {
                for (int x = 0; x < res; x++)
                {
                    int i = x + y * res;
                    int j = x + pad + (y + pad) * n;
                    float floor = height[i];
                    contactHeight[i] = capLevel[j] - MesaCapThickness;
                    if (floor <= 0f || skirt[j] <= 0f) continue;
                    if (water[i] > 0f || data.LakeDepth[i] > 0f) continue;
                    double worldX = worldLeft + (x + pad) * (double)cell;
                    double worldY = worldTop + (y + pad) * (double)cell;
                    float level = capLevel[j];
                    float foot = level - MesaCapThickness;
                    float bench = foot - MesaSoftThickness;
                    float d = toCap[j];
                    float value;
                    float rubble = 0f;

                    float tan = MesaTalusTan * (0.9f + 0.2f * RegionNoise.Fbm((float)(worldX / 150.0) + 1.1f, (float)(worldY / 150.0) + 7.9f, 2, seed + 294u));
                    // 斜面の上端は、崖の足もとより少し下がる所がある（足もとの岩が見える）
                    float bare = 14f * RegionNoise.Smooth(-0.2f, 0.5f, RegionNoise.Fbm((float)(worldX / 70.0) + 3.7f, (float)(worldY / 70.0) + 5.3f, 2, seed + 295u));
                    float slope = foot - bare - tan * d;
                    float shelf = bench - 0.03f * d + 0.6f * RegionNoise.Fbm((float)(worldX / 21.0), (float)(worldY / 21.0), 2, seed + 296u);
                    // 斜面が棚に届くまでの距離
                    float run = (foot - bare - bench) / (tan - 0.03f);
                    // 岩屑の面のでこぼこ：数 m のこぶと、1 m ごとの粒（粒は細かい格子でだけ入れる）
                    float roughness = 0.9f * Math.Abs(RegionNoise.Fbm((float)(worldX / 3.2), (float)(worldY / 3.2), 2, seed + 297u))
                        + 1.0f * RegionNoise.Fbm((float)(worldX / 9.5), (float)(worldY / 9.5), 2, seed + 298u)
                        + 0.45f * slotScale * (RegionNoise.Hash01((int)Math.Floor(worldX), (int)Math.Floor(worldY), seed + 301u) - 0.5f);
                    if (benchWidth[j] <= 0f)
                    {
                        // 棚の無い所：岩屑の斜面のまま床へ下りる
                        value = slope + roughness;
                        float beyond = (floor - value) / tan;
                        rubble = beyond <= 0f ? 1f - 0.5f * RegionNoise.Clamp01(d / run) : 0.3f * (1f - beyond / 45f);
                    }
                    else if (d < benchWidth[j])
                    {
                        if (slope >= shelf)
                        {
                            value = slope + roughness;
                            rubble = 1f - 0.5f * RegionNoise.Clamp01(d / run);
                        }
                        else
                        {
                            value = shelf;
                            rubble = 0.5f * (1f - (d - run) / 55f);
                        }
                    }
                    else
                    {
                        // 棚の縁の崖と、その下の岩屑。棚が狭い所では崖も低い
                        float wide = benchWidth[j] - MesaSoftThickness / MesaTalusTan;
                        float lower = MesaLowerCliff * RegionNoise.Smooth(0f, 30f, wide) * (0.55f + 0.45f * RegionNoise.Smooth(-0.3f, 0.3f,
                            RegionNoise.Fbm((float)(worldX / 120.0) + 6.6f, (float)(worldY / 120.0) + 9.1f, 2, seed + 299u)));
                        value = bench - 0.03f * d - lower - tan * toBench[j] + 0.6f * roughness;
                        float beyond = (floor - value) / tan;
                        rubble = beyond <= 0f ? 0.45f : 0.3f * (1f - beyond / 45f);
                    }
                    if (rubble < 0f) rubble = 0f;

                    bool cap = false;
                    if (capTop[j] > value)
                    {
                        cap = standing[j] != 0;
                        if (cap)
                        {
                            value = capTop[j];
                            rubble = 0f;
                        }
                        else
                        {
                            // 崩れ落ちた塊は、岩屑の斜面から 4〜13 m だけ顔を出す（裾の無い柱として立たせない）
                            float above = 4f + 9f * RegionNoise.Hash01((int)Math.Floor(worldX / 9.0), (int)Math.Floor(worldY / 12.0), seed + 300u);
                            float ground = value > floor ? value : floor;
                            value = Math.Min(capTop[j], ground + above);
                            rubble = Math.Max(rubble, 0.8f);
                        }
                    }
                    float reach = skirt[j];
                    talus[i] = rubble * reach;
                    if (value <= floor) continue;
                    value = floor + (value - floor) * reach;
                    height[i] = value;
                    changed[i] = true;
                    if (cap) caprock[i] = 1;
                    data.Temperature[i] -= 6.5f * (value - floor) / 1000f;
                }
            });

            // 崖の面にあたるセル（60 度前後より急な所）
            float[] cliff = data.Cliff ?? new float[res * res];
            Parallel.For(1, res - 1, y =>
            {
                for (int x = 1; x < res - 1; x++)
                {
                    int i = x + y * res;
                    if (!changed[i]) continue;
                    float steep = RegionNoise.Smooth(1.2f, 2.4f, RegionGenerator.SlopeAt(height, res, x, y, cell));
                    if (steep > cliff[i]) cliff[i] = steep;
                }
            });
            data.Cliff = cliff;
            data.Talus = talus;
            data.Caprock = caprock;
            data.MesaContactHeight = contactHeight;
        }

        /// <summary>
        /// 帽子の岩が残る度合い。正の所が台地で、値が大きいほど縁から内側（1 m あたり MesaMarginPerMeter ほど増える）。
        /// 広い台地の形（約 2 km の起伏）を、数百 m の曲がりで入り組ませ、尾根状のノイズで細い入り江（峡谷）を切り込む。
        /// </summary>
        private static float MesaField(double worldX, double worldY, uint seed)
        {
            float wx = (float)(worldX / 760.0), wy = (float)(worldY / 760.0);
            float fx = (float)(worldX / 150.0), fy = (float)(worldY / 150.0);
            double sx = worldX + 120f * RegionNoise.Fbm(wx + 3.1f, wy + 8.7f, 3, seed + 241u) + 26f * RegionNoise.Fbm(fx + 1.7f, fy + 4.4f, 2, seed + 242u);
            double sy = worldY + 120f * RegionNoise.Fbm(wx + 31.9f, wy + 2.3f, 3, seed + 243u) + 26f * RegionNoise.Fbm(fx + 9.2f, fy + 6.1f, 2, seed + 244u);
            float big = RegionNoise.Fbm((float)(sx / 2100.0), (float)(sy / 2100.0), 3, seed + 245u);
            float cut = RegionNoise.Ridged((float)(sx / 700.0) + 5.5f, (float)(sy / 700.0) + 1.2f, 3, seed + 246u);
            return big - MesaThreshold - 0.32f * RegionNoise.Smooth(0.60f, 0.90f, cut);
        }

        /// <summary>離れ岩（ビュートと尖塔）の中なら正。値の単位は MesaField と同じ。</summary>
        private static float MesaOutlierMargin(MesaOutlier[] outliers, int ox0, int oy0, int ow, int oh, double grid,
            double worldX, double worldY, uint seed)
        {
            int gx = (int)Math.Floor(worldX / grid) - ox0;
            int gy = (int)Math.Floor(worldY / grid) - oy0;
            float best = float.NegativeInfinity;
            for (int dy = -1; dy <= 1; dy++)
            {
                int y = gy + dy;
                if (y < 0 || y >= oh) continue;
                for (int dx = -1; dx <= 1; dx++)
                {
                    int x = gx + dx;
                    if (x < 0 || x >= ow) continue;
                    MesaOutlier o = outliers[x + y * ow];
                    if (o.Radius <= 0f) continue;
                    double ex = worldX - o.X, ey = worldY - o.Y;
                    float distance = (float)Math.Sqrt(ex * ex + ey * ey);
                    if (distance > o.Radius * 1.5f) continue;
                    // 輪郭を丸にしない
                    distance += 0.28f * o.Radius * RegionNoise.Fbm((float)(worldX / (o.Radius * 1.1)) + x * 3.1f, (float)(worldY / (o.Radius * 1.1)) + y * 1.7f, 2, seed + 256u);
                    float margin = (o.Radius - distance) * MesaMarginPerMeter;
                    if (margin > best) best = margin;
                }
            }
            return best;
        }

        /// <summary>
        /// 1 方向の割れ目の並び。割れ目は spacing ごとに 1 本で、位置は 1 本ずつずれる。
        /// index は、その座標が入る「割れ目と割れ目の間」の番号。toCenter は間の中心までの差、toLine は近い方の割れ目までの距離（m）。
        /// </summary>
        private static void JointBlock(double coordinate, float spacing, uint seed, out int index, out float toCenter, out float toLine)
        {
            int k = (int)Math.Floor(coordinate / spacing);
            double line = JointLine(k, spacing, seed);
            double lower, upper;
            if (coordinate >= line)
            {
                index = k;
                lower = line;
                upper = JointLine(k + 1, spacing, seed);
            }
            else
            {
                index = k - 1;
                lower = JointLine(k - 1, spacing, seed);
                upper = line;
            }
            toCenter = (float)((lower + upper) * 0.5 - coordinate);
            float below = (float)(coordinate - lower), above = (float)(upper - coordinate);
            toLine = below < above ? below : -above;
        }

        private static double JointLine(int k, float spacing, uint seed)
        {
            return (k + 0.5 + 0.7 * (RegionNoise.Hash01(k, 0, seed) - 0.5)) * spacing;
        }

        /// <summary>
        /// 割れ目の開き具合（0〜1）。toLine は JointBlock の値（負なら上側の割れ目）。
        /// 割れ目は segment ごとに、開いている区間と閉じている区間がある。幅は minWidth〜minWidth + extraWidth（m）。
        /// </summary>
        private static float JointSlot(float toLine, int block, int segment, float minWidth, float extraWidth, float chance, uint seed)
        {
            int line = toLine >= 0f ? block : block + 1;
            float distance = toLine >= 0f ? toLine : -toLine;
            float half = 0.5f * (minWidth + extraWidth * RegionNoise.Hash01(line, 7, seed));
            if (distance >= half) return 0f;
            if (RegionNoise.Hash01(line, segment, seed + 1u) >= chance) return 0f;
            float t = distance / half;
            float open = t < 0.5f ? 1f : (1f - t) * 2f;
            return open * (0.55f + 0.45f * RegionNoise.Hash01(line, segment, seed + 2u));
        }

        /// <summary>
        /// 広域の標高（海は 0）を、前後 3 セル（約 1.75 km 四方）で平均した面。並びは切り出した格子と同じ。
        /// 広域の配列から直接ならすので、窓の位置や大きさによらず、同じ場所は同じ値になる。
        /// </summary>
        private static float[] MesaCapBase(MesaInput input)
        {
            int w = input.W, h = input.H;
            const int Radius = 3;
            RegionData region = input.Region;
            int coarse = region.Resolution;
            float[] source = region.Height;
            var result = new float[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float sum = 0f;
                    for (int dy = -Radius; dy <= Radius; dy++)
                    {
                        int row = ClampInt(input.Cy0 + y + dy, 0, coarse - 1) * coarse;
                        for (int dx = -Radius; dx <= Radius; dx++)
                        {
                            float value = source[row + ClampInt(input.Cx0 + x + dx, 0, coarse - 1)];
                            if (value > 0f) sum += value;
                        }
                    }
                    result[x + y * w] = sum / ((Radius * 2 + 1) * (Radius * 2 + 1));
                }
            }
            return result;
        }

        /// <summary>
        /// 印の付いたセルまでの距離（m）。印のセルは 0。reach を渡すと、「印からの距離 from が reach 未満のセル」も印として扱う
        /// （棚の範囲からの距離を測るのに使う）。Felzenszwalb と Huttenlocher の、行ごと・列ごとの 2 回で求める方法。
        /// </summary>
        private static float[] MesaDistance(byte[] marked, int n, float[] reach, float[] from, float cell)
        {
            const float Far = 1e12f;
            var squared = new float[n * n];
            Parallel.For(0, n, () => new MesaScan(n), (y, state, scan) =>
            {
                for (int x = 0; x < n; x++)
                {
                    int j = x + y * n;
                    bool on = marked[j] != 0 || (reach != null && reach[j] >= 0f && from[j] < reach[j]);
                    scan.Input[x] = on ? 0f : Far;
                }
                scan.Run(n);
                for (int x = 0; x < n; x++) squared[x + y * n] = scan.Output[x];
                return scan;
            }, scan => { });
            Parallel.For(0, n, () => new MesaScan(n), (x, state, scan) =>
            {
                for (int y = 0; y < n; y++) scan.Input[y] = squared[x + y * n];
                scan.Run(n);
                for (int y = 0; y < n; y++) squared[x + y * n] = scan.Output[y];
                return scan;
            }, scan => { });
            Parallel.For(0, n * n, j => { squared[j] = squared[j] >= Far ? Far : (float)Math.Sqrt(squared[j]) * cell; });
            return squared;
        }

        /// <summary>1 列ぶんの距離の変換（放物線の下側の包み）。</summary>
        private sealed class MesaScan
        {
            public readonly float[] Input;
            public readonly float[] Output;
            private readonly int[] _site;
            private readonly float[] _bound;

            public MesaScan(int n)
            {
                Input = new float[n];
                Output = new float[n];
                _site = new int[n];
                _bound = new float[n + 1];
            }

            public void Run(int n)
            {
                const float Far = 1e12f;
                int k = -1;
                for (int q = 0; q < n; q++)
                {
                    if (Input[q] >= Far) continue;
                    if (k < 0)
                    {
                        k = 0;
                        _site[0] = q;
                        _bound[0] = float.NegativeInfinity;
                        _bound[1] = float.PositiveInfinity;
                        continue;
                    }
                    float s;
                    while (true)
                    {
                        int p = _site[k];
                        s = ((Input[q] + (float)q * q) - (Input[p] + (float)p * p)) / (2f * (q - p));
                        if (s > _bound[k]) break;
                        k--;
                    }
                    k++;
                    _site[k] = q;
                    _bound[k] = s;
                    _bound[k + 1] = float.PositiveInfinity;
                }
                if (k < 0)
                {
                    for (int q = 0; q < n; q++) Output[q] = Far;
                    return;
                }
                int at = 0;
                for (int q = 0; q < n; q++)
                {
                    while (_bound[at + 1] < q) at++;
                    int p = _site[at];
                    Output[q] = (float)(q - p) * (q - p) + Input[p];
                }
            }
        }
    }
}
