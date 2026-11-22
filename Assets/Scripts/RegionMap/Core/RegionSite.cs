using System;
using System.Collections.Generic;

namespace Vastcore.RegionMap
{
    /// <summary>探索地点に立つ構造物の種類。VastCore の巨大人工構造物の系統に対応させる。</summary>
    public enum RegionSiteKind
    {
        Monolith = 0,
        Arch = 1,
        Ring = 2,
        Tower = 3,
        Dome = 4,
        Terrace = 5,
        Sunken = 6,
    }

    /// <summary>地図上に隠れている探索地点。</summary>
    [Serializable]
    public sealed class RegionSite
    {
        public int Id;
        public RegionSiteKind Kind;
        public string Name;
        public float MapX;
        public float MapY;
        public float HeightMeters;
        public RegionBiome Biome;

        /// <summary>地点そのものの仮の呼び名（例「山頂のモノリス」）。Name は「エリア名・この呼び名」。</summary>
        public string Feature;

        /// <summary>名前の由来（使った型・語根と意味・選んだ根拠）。規則から作った名前（Systematic）の時だけ入る。</summary>
        public string NameOrigin;

        /// <summary>付いている名前の全部（先頭が Name）。規則から作った名前の時だけ入る。</summary>
        public List<RegionPlaceName> Names = new List<RegionPlaceName>();

        /// <summary>土地の経緯（属するエリアと同じ）。付ける名前の種類を決める。</summary>
        public RegionLandHistory History;

        /// <summary>属するエリアの番号（RegionArea.Id）。外洋なら 0。</summary>
        public int AreaId;

        /// <summary>この場所がその種類に合う度合い（採点の値。種類ごとに尺度が違うので、種類をまたいで比べない）。</summary>
        public float Score;

        /// <summary>同じ種類の候補の中での順位（1 が最も合う）と、候補の総数。</summary>
        public int Rank;
        public int Candidates;

        /// <summary>ここに置いた根拠（採点に使った地形の値を文にしたもの）。</summary>
        public string Reason;
    }

    /// <summary>
    /// 地形の特徴に合う場所へ探索地点を置く。山頂にモノリス、海岸にアーチ、乾燥地に環、のように
    /// 種類ごとに置ける地形を決め、互いに一定の距離を空ける。
    /// </summary>
    public static class RegionSiteGenerator
    {
        private const float MinSpacing = 9f;
        private const int CandidateCount = 9000;

        /// <summary>川の上に来た地点を、川の中心からどれだけ離すか（m）。構造物の差し渡しの半分と川幅の半分より大きくする。</summary>
        private const float BankMeters = 520f;

        /// <summary>川岸へ移した地点の周りで、川の升が 1 つも無いことを確かめる半径（m）。曲がった川が寄ってくる場所を避ける。</summary>
        private const float BankClearMeters = 450f;

        private struct Candidate
        {
            public int Cell;
            public float Score;
        }

        public static void Populate(RegionData data)
        {
            data.Sites.Clear();
            int n = data.Resolution;
            uint seed = unchecked((uint)data.Spec.Seed * 2654435761u + 97u);
            int kinds = Enum.GetValues(typeof(RegionSiteKind)).Length;
            var lists = new List<Candidate>[kinds];
            for (int k = 0; k < kinds; k++) lists[k] = new List<Candidate>();

            int margin = Math.Max(2, n / 28);
            int span = n - margin * 2;
            for (int c = 0; c < CandidateCount; c++)
            {
                int x = margin + (int)(RegionNoise.Hash01(c, 1, seed) * span);
                int y = margin + (int)(RegionNoise.Hash01(c, 2, seed) * span);
                int i = x + y * n;
                float jitter = RegionNoise.Hash01(c, 3, seed) * 0.2f;
                RegionSiteKind kind;
                float score;
                if (TryRate(data, x, y, out kind, out score))
                {
                    lists[(int)kind].Add(new Candidate { Cell = i, Score = score + jitter });
                }
            }

            for (int k = 0; k < kinds; k++)
            {
                lists[k].Sort((a, b) => b.Score != a.Score ? b.Score.CompareTo(a.Score) : a.Cell.CompareTo(b.Cell));
            }

            var cursor = new int[kinds];
            int target = data.Spec.SiteCount;
            bool progressed = true;
            while (data.Sites.Count < target && progressed)
            {
                progressed = false;
                for (int k = 0; k < kinds && data.Sites.Count < target; k++)
                {
                    List<Candidate> list = lists[k];
                    while (cursor[k] < list.Count)
                    {
                        Candidate candidate = list[cursor[k]++];
                        float mx = data.MapXFromCell(candidate.Cell);
                        float my = data.MapYFromCell(candidate.Cell);
                        int stand = candidate.Cell;
                        // 川の上の候補は川岸へ移す（置ける岸が無ければ、その候補は使わない）
                        if (k == (int)RegionSiteKind.Terrace && !TryBank(data, candidate.Cell, out mx, out my, out stand)) continue;
                        if (!IsFarFromOthers(data.Sites, mx, my)) continue;
                        data.Sites.Add(new RegionSite
                        {
                            Kind = (RegionSiteKind)k,
                            MapX = mx,
                            MapY = my,
                            HeightMeters = data.Height[stand],
                            Biome = data.Biome[stand],
                            AreaId = data.Area[stand],
                            Score = candidate.Score,
                            Rank = cursor[k],
                            Candidates = list.Count,
                            Reason = Explain(data, (RegionSiteKind)k, candidate.Cell),
                        });
                        progressed = true;
                        break;
                    }
                }
            }

            data.Sites.Sort((a, b) => a.MapY != b.MapY ? a.MapY.CompareTo(b.MapY) : a.MapX.CompareTo(b.MapX));
            for (int s = 0; s < data.Sites.Count; s++)
            {
                RegionSite site = data.Sites[s];
                site.Id = s + 1;
                RegionArea area = data.AreaById(site.AreaId);
                if (area != null) area.SiteCount++;
            }
            NameByFeature(data);
        }

        /// <summary>「エリア名・地形と種類」の機械的な呼び名を付ける（RegionNameStyle.Descriptive）。</summary>
        internal static void NameByFeature(RegionData data)
        {
            for (int s = 0; s < data.Sites.Count; s++)
            {
                RegionSite site = data.Sites[s];
                site.Feature = FeatureName(site.Kind);
                site.Name = data.PlaceNameAtMap(site.MapX, site.MapY).Replace("の沖", "沖") + "・" + site.Feature;
            }
        }

        /// <summary>
        /// そのセルが探索地点に合うかを採点する。合う種類が無ければ false。
        /// 規則は上から順に当てはめ、最初に合った 1 種類だけを返す（浅い海→川→高い山頂→雪原→海岸→乾燥地→森や草原の丘）。
        /// </summary>
        public static bool Rate(RegionData data, int x, int y, out RegionSiteKind kind, out float score)
        {
            return TryRate(data, x, y, out kind, out score);
        }

        /// <summary>採点に使った地形の値を、読める文にする。</summary>
        private static string Explain(RegionData data, RegionSiteKind kind, int cell)
        {
            int n = data.Resolution;
            int x = cell % n;
            int y = cell / n;
            float h = data.Height[cell];
            float prominence = Prominence(data, x, y, Math.Max(3, n / 90));
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            switch (kind)
            {
                case RegionSiteKind.Sunken:
                    return "岸のそばの浅い海（水深 " + (-h).ToString("#,0", culture) + " m。水深 220 m 前後を最も高く採点）";
                case RegionSiteKind.Terrace:
                    return "川岸（川の中心から約 " + BankMeters.ToString("0", culture) + " m。上流から集まる水の量が多いほど高く採点）";
                case RegionSiteKind.Monolith:
                    return "標高 " + h.ToString("#,0", culture) + " m（地域の最高の " + (h / data.MaxHeight * 100f).ToString("0", culture)
                        + "%）で、周りより " + prominence.ToString("#,0", culture) + " m 高い山頂";
                case RegionSiteKind.Dome:
                    return "雪原・ツンドラの中で、周りより " + prominence.ToString("#,0", culture) + " m 高い所";
                case RegionSiteKind.Arch:
                    return "標高 " + h.ToString("0", culture) + " m の、海のすぐそば（低いほど高く採点）";
                case RegionSiteKind.Ring:
                    return "乾いた土地（湿り気 " + data.Moisture[cell].ToString("0.00", culture) + "。乾くほど高く採点）";
                default:
                    return "森や草原の中で、周りより " + prominence.ToString("#,0", culture) + " m 高い丘";
            }
        }

        /// <summary>
        /// 川の上のセルから、流れに直交する向きへ BankMeters（1.5 升以上）離れた岸を探す。
        /// 川・湖・海でない陸で、周り BankClearMeters の中に川の升（細い流れを含む）が無い場所だけを選ぶ。
        /// 両岸とも置けるなら川面との高さの差が小さい側にする。見つからなければ少しずつ遠ざけ、それでも無ければ false。
        /// </summary>
        internal static bool TryBank(RegionData data, int cell, out float mapX, out float mapY, out int bankCell)
        {
            int n = data.Resolution;
            int x = cell % n;
            int y = cell / n;
            float fx = 0f;
            float fy = 0f;
            int down = data.Downstream != null ? data.Downstream[cell] : -1;
            if (down >= 0 && down != cell)
            {
                fx = down % n - x;
                fy = down / n - y;
                int next = data.Downstream[down];
                if (next >= 0 && next != down)
                {
                    // 2 升先まで見て、升目の 8 方向より実際の流れに近い向きにする
                    fx = next % n - x;
                    fy = next / n - y;
                }
            }
            float length = (float)Math.Sqrt(fx * fx + fy * fy);
            if (length < 0.5f)
            {
                fx = 1f;
                fy = 0f;
                length = 1f;
            }
            float nx = -fy / length;
            float ny = fx / length;
            float baseDistance = Math.Max(1.5f, BankMeters / data.CellMeters);
            float water = data.Height[cell];
            mapX = 0f;
            mapY = 0f;
            bankCell = cell;
            float clear = BankClearMeters / data.CellMeters;
            // 拡大した地形は、広域で川と塗る升より細い流れ（流量がしきい値を超える升）も川として引く
            float stream = RegionGenerator.RiverThreshold(data);
            int reach = (int)Math.Ceiling(clear) + 1;
            for (int step = 0; step < 6; step++)
            {
                float distance = baseDistance + step * 0.5f;
                float best = float.MaxValue;
                bool found = false;
                for (int side = -1; side <= 1; side += 2)
                {
                    float px = x + 0.5f + nx * distance * side;
                    float py = y + 0.5f + ny * distance * side;
                    int cx = (int)Math.Floor(px);
                    int cy = (int)Math.Floor(py);
                    if (cx < 1 || cy < 1 || cx >= n - 1 || cy >= n - 1) continue;
                    int index = cx + cy * n;
                    RegionBiome biome = data.Biome[index];
                    if (data.Height[index] <= 0f || biome == RegionBiome.River || biome == RegionBiome.Lake || data.Flow[index] > stream) continue;
                    bool nearWater = false;
                    for (int oy = -reach; oy <= reach && !nearWater; oy++)
                    {
                        for (int ox = -reach; ox <= reach; ox++)
                        {
                            int wx = cx + ox;
                            int wy = cy + oy;
                            if (wx < 0 || wy < 0 || wx >= n || wy >= n) continue;
                            int w = wx + wy * n;
                            RegionBiome other = data.Biome[w];
                            if (other != RegionBiome.River && other != RegionBiome.Lake && !(data.Height[w] > 0f && data.Flow[w] > stream)) continue;
                            float ddx = wx + 0.5f - px;
                            float ddy = wy + 0.5f - py;
                            if (ddx * ddx + ddy * ddy < clear * clear)
                            {
                                nearWater = true;
                                break;
                            }
                        }
                    }
                    if (nearWater) continue;
                    float rise = Math.Abs(data.Height[index] - water);
                    if (rise >= best) continue;
                    best = rise;
                    found = true;
                    mapX = px / n * RegionData.MapUnits;
                    mapY = py / n * RegionData.MapUnits;
                    bankCell = index;
                }
                if (found) return true;
            }
            return false;
        }

        private static bool IsFarFromOthers(List<RegionSite> sites, float mx, float my)
        {
            for (int s = 0; s < sites.Count; s++)
            {
                float dx = sites[s].MapX - mx;
                float dy = sites[s].MapY - my;
                if (dx * dx + dy * dy < MinSpacing * MinSpacing) return false;
            }
            return true;
        }

        private static bool TryRate(RegionData data, int x, int y, out RegionSiteKind kind, out float score)
        {
            int n = data.Resolution;
            int i = x + y * n;
            float h = data.Height[i];
            RegionBiome biome = data.Biome[i];
            kind = RegionSiteKind.Monolith;
            score = 0f;

            if (h <= 0f)
            {
                // 岸に近い浅い海だけ
                if (h < -700f || h > -25f) return false;
                if (!HasBiomeNear(data, x, y, n / 60, false)) return false;
                kind = RegionSiteKind.Sunken;
                score = 1f - Math.Abs(h + 220f) / 700f;
                return true;
            }

            if (biome == RegionBiome.River || biome == RegionBiome.Lake)
            {
                kind = RegionSiteKind.Terrace;
                score = RegionNoise.Clamp01((float)Math.Log10(1f + data.Flow[i]) / 5f);
                return true;
            }

            float prominence = Prominence(data, x, y, Math.Max(3, n / 90));
            if (h > data.MaxHeight * 0.5f && prominence > 60f)
            {
                kind = RegionSiteKind.Monolith;
                score = h / data.MaxHeight + prominence / 800f;
                return true;
            }

            if (biome == RegionBiome.Snow || biome == RegionBiome.Tundra)
            {
                kind = RegionSiteKind.Dome;
                score = 0.5f + prominence / 600f;
                return true;
            }

            if (h < 60f && HasBiomeNear(data, x, y, Math.Max(2, n / 200), true))
            {
                kind = RegionSiteKind.Arch;
                score = 1f - h / 60f;
                return true;
            }

            if (biome == RegionBiome.Desert || biome == RegionBiome.Savanna)
            {
                kind = RegionSiteKind.Ring;
                score = 1f - data.Moisture[i];
                return true;
            }

            if (biome == RegionBiome.Forest || biome == RegionBiome.Grassland || biome == RegionBiome.Taiga || biome == RegionBiome.Rainforest)
            {
                if (prominence < 15f) return false;
                kind = RegionSiteKind.Tower;
                score = prominence / 300f;
                return true;
            }

            return false;
        }

        /// <summary>周囲の平均よりどれだけ高いか（m）。</summary>
        private static float Prominence(RegionData data, int x, int y, int radius)
        {
            int n = data.Resolution;
            float sum = 0f;
            int samples = 0;
            for (int k = 0; k < 8; k++)
            {
                double a = k * Math.PI / 4.0;
                int sx = x + (int)Math.Round(Math.Cos(a) * radius);
                int sy = y + (int)Math.Round(Math.Sin(a) * radius);
                if (sx < 0 || sy < 0 || sx >= n || sy >= n) continue;
                float v = data.Height[sx + sy * n];
                sum += v > 0f ? v : 0f;
                samples++;
            }
            if (samples == 0) return 0f;
            return data.Height[x + y * n] - sum / samples;
        }

        /// <summary>半径内に海（wantOcean=true）または陸（false）があるか。</summary>
        private static bool HasBiomeNear(RegionData data, int x, int y, int radius, bool wantOcean)
        {
            int n = data.Resolution;
            for (int k = 0; k < 8; k++)
            {
                double a = k * Math.PI / 4.0;
                int sx = x + (int)Math.Round(Math.Cos(a) * radius);
                int sy = y + (int)Math.Round(Math.Sin(a) * radius);
                if (sx < 0 || sy < 0 || sx >= n || sy >= n) continue;
                bool ocean = data.Height[sx + sy * n] <= 0f;
                if (ocean == wantOcean) return true;
            }
            return false;
        }

        /// <summary>
        /// 位置と地形から機械的に作る仮の呼び名。作品としての地名は人が決めるもので、ここでは決めない。
        /// </summary>
        private static string FeatureName(RegionSiteKind kind)
        {
            switch (kind)
            {
                case RegionSiteKind.Monolith: return "山頂のモノリス";
                case RegionSiteKind.Arch: return "海岸のアーチ";
                case RegionSiteKind.Ring: return "乾いた平原の環";
                case RegionSiteKind.Tower: return "丘の塔";
                case RegionSiteKind.Dome: return "雪原のドーム";
                case RegionSiteKind.Terrace: return "川辺の段丘";
                default: return "浅瀬に沈んだ遺構";
            }
        }
    }
}
