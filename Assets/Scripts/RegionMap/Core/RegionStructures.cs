using System;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 拡大した地形へ置く巨大構造物の、形の方向の案。どれを採るか・寸法・素材は人が決める。
    /// 真上から見る絵に使うので、どれも「地面から立ち上がる塊」（高さの場）として持つ。
    /// 下をくぐれる形（アーチの開口部など）は、この持ち方では表せない。
    /// </summary>
    public enum RegionStructureForm
    {
        /// <summary>一枚の板のような巨石。</summary>
        Slab = 0,

        /// <summary>高さの違う柱の群れ。</summary>
        Cluster = 1,

        /// <summary>段を重ねた塊。</summary>
        Stepped = 2,
    }

    /// <summary>拡大した地形へ置いた構造物 1 つ。</summary>
    [Serializable]
    public sealed class RegionStructure
    {
        public RegionSiteKind Kind;
        public RegionStructureForm Form;

        /// <summary>中心（地域の地図座標）。</summary>
        public float MapX;
        public float MapY;

        /// <summary>足元の地面の標高（m）、差し渡し（m）、地面からの高さ（m）。</summary>
        public float BaseMeters;
        public float SpanMeters;
        public float HeightMeters;
    }

    /// <summary>
    /// 構造物の形を、拡大した地形の標高へ足す。足した範囲は RegionData.StructureCoverage に残り、描画で石の色になる。
    /// 起伏の陰影と落ち影は地形と同じ計算で付くので、真上から見ても高さが分かる。
    /// </summary>
    public static class RegionStructureStamp
    {
        /// <summary>
        /// 構造物を置く。scale は寸法の倍率（1 で差し渡し 260〜460 m、高さ 350〜520 m）。海の上と窓の外には置かず、null を返す。
        /// </summary>
        public static RegionStructure Place(RegionDetail detail, float mapX, float mapY, RegionSiteKind kind, RegionStructureForm form, float scale = 1f)
        {
            RegionData data = detail.Data;
            int n = data.Resolution;
            float cell = data.CellMeters;
            float centerX = detail.LocalX(mapX);
            float centerY = detail.LocalY(mapY);
            int cx = (int)centerX;
            int cy = (int)centerY;
            if (cx < 0 || cy < 0 || cx >= n || cy >= n) return null;
            float baseHeight = data.Height[cx + cy * n];
            if (baseHeight <= 0f) return null;

            if (data.StructureCoverage == null) data.StructureCoverage = new float[n * n];
            uint seed = unchecked((uint)data.Spec.Seed * 374761393u + (uint)(mapX * 97f) * 668265263u + (uint)(mapY * 89f) * 2147483647u + (uint)form * 40503u);
            float turn = RegionNoise.Hash01(1, 1, seed) * (float)Math.PI;
            float cos = (float)Math.Cos(turn);
            float sin = (float)Math.Sin(turn);
            float reach = 260f * scale;
            int radius = (int)Math.Ceiling(reach / cell) + 2;
            float top = 0f;
            float span = 0f;

            for (int y = cy - radius; y <= cy + radius; y++)
            {
                if (y < 0 || y >= n) continue;
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (x < 0 || x >= n) continue;
                    // セルを 2×2 に分けて平均する（縁の段々を和らげる）
                    float sum = 0f;
                    int covered = 0;
                    for (int s = 0; s < 4; s++)
                    {
                        float mx = (x + 0.25f + 0.5f * (s & 1) - centerX) * cell / scale;
                        float my = (y + 0.25f + 0.5f * (s >> 1) - centerY) * cell / scale;
                        float rx = mx * cos + my * sin;
                        float ry = -mx * sin + my * cos;
                        float rise = Shape(form, rx, ry, seed) * scale;
                        if (rise <= 0f) continue;
                        sum += rise;
                        covered++;
                        if (rise > top) top = rise;
                        float distance = (float)Math.Sqrt(mx * mx + my * my) * 2f * scale;
                        if (distance > span) span = distance;
                    }
                    if (covered == 0) continue;
                    int i = x + y * n;
                    float target = baseHeight + sum / 4f;
                    if (target > data.Height[i]) data.Height[i] = target;
                    float cover = covered / 4f;
                    if (cover > data.StructureCoverage[i]) data.StructureCoverage[i] = cover;
                    if (data.WaterCoverage != null) data.WaterCoverage[i] *= 1f - cover;
                }
            }

            var placed = new RegionStructure
            {
                Kind = kind,
                Form = form,
                MapX = mapX,
                MapY = mapY,
                BaseMeters = baseHeight,
                SpanMeters = span,
                HeightMeters = top,
            };
            detail.Structures.Add(placed);
            return placed;
        }

        /// <summary>形ごとの、足元からの高さ（m）。x・y は構造物の向きに合わせた中心からの位置（m）。外は 0。</summary>
        private static float Shape(RegionStructureForm form, float x, float y, uint seed)
        {
            float ax = Math.Abs(x);
            float ay = Math.Abs(y);
            switch (form)
            {
                case RegionStructureForm.Slab:
                    // 低い台座の上に、薄く長い板が 1 枚立つ
                    if (ax < 48f && ay < 130f) return 520f;
                    if (ax < 86f && ay < 168f) return 16f;
                    return 0f;

                case RegionStructureForm.Cluster:
                {
                    // 真ん中の高い柱と、周りに不揃いな柱が 6 本
                    float best = 0f;
                    if (x * x + y * y < 40f * 40f) best = 520f;
                    for (int k = 0; k < 6; k++)
                    {
                        double angle = k * Math.PI / 3.0 + RegionNoise.Hash01(k, 2, seed) * 0.7;
                        float ring = 92f + RegionNoise.Hash01(k, 3, seed) * 62f;
                        float px = (float)Math.Cos(angle) * ring;
                        float py = (float)Math.Sin(angle) * ring;
                        float size = 22f + RegionNoise.Hash01(k, 4, seed) * 14f;
                        float dx = x - px;
                        float dy = y - py;
                        if (dx * dx + dy * dy >= size * size) continue;
                        float rise = 170f + RegionNoise.Hash01(k, 5, seed) * 260f;
                        if (rise > best) best = rise;
                    }
                    return best;
                }

                default:
                {
                    // 正方形の段を 5 つ重ねる（1 段 70 m）
                    float m = ax > ay ? ax : ay;
                    if (m >= 230f) return 0f;
                    int level = 5 - (int)((m - 5f) / 45f);
                    if (level > 5) level = 5;
                    if (level < 1) level = 1;
                    return level * 70f;
                }
            }
        }
    }
}
