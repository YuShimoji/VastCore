using System;
using System.Collections.Generic;

namespace Vastcore.RegionMap
{
    /// <summary>エリアの地形の種類。仮の呼び名の後ろ半分（山地・平野など）を決める。</summary>
    public enum RegionAreaKind
    {
        Mountains = 0,
        Highland = 1,
        Hills = 2,
        Plain = 3,
        Basin = 4,
        Island = 5,
        Snowfield = 6,
        Desert = 7,
    }

    /// <summary>地域の中の、地形でひとまとまりになる区画。</summary>
    [Serializable]
    public sealed class RegionArea
    {
        /// <summary>1 から始まる番号。RegionData.Area の値と同じ。</summary>
        public int Id;

        public RegionAreaKind Kind;

        /// <summary>
        /// 方角と地形から機械的に作る仮の呼び名（例「北東の山地」）。作品としての地名は人が決めるもので、
        /// ここへ別の名前を入れれば画面にもその名前が出る。
        /// </summary>
        public string Name;

        /// <summary>このエリアを何で区切ったか（山地のまとまり・流域など）。</summary>
        public string Basis;

        /// <summary>名前を置く位置（地図座標）。エリアの中で、境界からいちばん離れた所。</summary>
        public float LabelX;
        public float LabelY;

        public float AreaKm2;
        public float MeanHeight;
        public float MaxHeight;

        /// <summary>いちばん広い土地の種類（川と海岸は数えない）。</summary>
        public RegionBiome MainBiome;

        /// <summary>このエリア（沖を含む）にある探索地点の数。</summary>
        public int SiteCount;
    }

    /// <summary>
    /// 陸をエリアへ分ける。山地は「ならした標高が高い、ひと続きの土地」、低地は「同じ河口へ注ぐ流域」で区切る。
    /// 流域の境は分水嶺になるので、線が尾根に沿う。小さな流域は、境をいちばん長く接する隣の流域へ、流域ごと合わせる。
    /// 岸に近い海は、いちばん近い陸のエリアの「沖」として同じ番号を持つ。それより遠い海は 0（外洋）。
    /// </summary>
    internal static class RegionAreaBuilder
    {
        /// <summary>低地のエリアとして独り立ちする最小の広さ（陸全体に対する割合）。これより狭い流域は隣へ合わせる。</summary>
        private const float LowlandMinFraction = 0.045f;

        /// <summary>山地のエリアとして独り立ちする最小の広さ（陸全体に対する割合）。</summary>
        private const float MountainMinFraction = 0.02f;

        /// <summary>これより狭い陸地（地図全体に対する割合）は、分けずに 1 つの島として扱う。</summary>
        private const float IslandMaxFraction = 0.03f;

        /// <summary>これより狭い陸地は島として数えず、最寄りのエリアへ入れる。</summary>
        private const float IsletMinFraction = 0.0006f;

        /// <summary>「沖」として陸のエリアに入れる海の幅（地図の幅に対する割合）。</summary>
        private const float CoastalWatersFraction = 0.035f;

        /// <summary>山地とみなす高さ（地域の最高標高に対する割合）と、その下限（m）。</summary>
        private const float MountainLineFraction = 0.28f;
        private const float MountainLineFloor = 450f;

        private static readonly int[] Dx = { 1, 0, -1, 0, 1, -1, -1, 1 };
        private static readonly int[] Dy = { 0, 1, 0, -1, 1, 1, -1, -1 };

        private enum Source
        {
            Mountains,
            River,
            Island,
        }

        private sealed class Draft
        {
            public Source Source;
            public int Cells;
            public double SumHeight;
            public float MaxHeight;
            public int Border;
            public int MountainBorder;
            public int BestDepth = -1;
            public int LabelCell;
            public readonly int[] Biomes = new int[13];
            public RegionArea Area;
        }

        public static void Build(RegionData data, FlowRouter router, bool[] land)
        {
            data.Areas.Clear();
            int n = data.Resolution;
            int count = n * n;
            int landCells = 0;
            for (int i = 0; i < count; i++)
            {
                if (land[i]) landCells++;
            }
            int lowlandMin = Math.Max(24, (int)(landCells * LowlandMinFraction));
            int mountainMin = Math.Max(16, (int)(landCells * MountainMinFraction));
            int islandMax = Math.Max(lowlandMin, (int)(count * IslandMaxFraction));
            int isletMin = Math.Max(6, (int)(count * IsletMinFraction));

            var queue = new int[count];
            var label = new int[count];
            var drafts = new List<Draft>();

            // 1. 陸地のまとまり（本土・島・小島）
            var mass = new int[count];
            List<int> massSize = Components(land, n, mass, queue);
            // 0 小島、1 島、2 分ける大きさの陸地
            var massClass = new byte[massSize.Count];
            for (int m = 0; m < massSize.Count; m++)
            {
                massClass[m] = (byte)(massSize[m] >= islandMax ? 2 : (massSize[m] >= isletMin ? 1 : 0));
            }
            for (int i = 0; i < count; i++) label[i] = land[i] ? -1 : -2;

            // 2. 山地：ならした標高が線を超える、ひと続きの土地
            float mountainLine = Math.Max(MountainLineFloor, data.MaxHeight * MountainLineFraction);
            float[] smooth = Smooth(data.Height, n, Math.Max(2, n / 160));
            var mask = new bool[count];
            for (int i = 0; i < count; i++)
            {
                mask[i] = land[i] && massClass[mass[i]] == 2 && smooth[i] > mountainLine;
            }
            var part = new int[count];
            List<int> partSize = Components(mask, n, part, queue);
            var partDraft = new int[partSize.Count];
            for (int p = 0; p < partSize.Count; p++)
            {
                partDraft[p] = -1;
                if (partSize[p] < mountainMin) continue;
                partDraft[p] = drafts.Count;
                drafts.Add(new Draft { Source = Source.Mountains });
            }
            var massHasArea = new bool[massSize.Count];
            for (int i = 0; i < count; i++)
            {
                if (!mask[i] || partDraft[part[i]] < 0) continue;
                label[i] = partDraft[part[i]];
                massHasArea[mass[i]] = true;
            }

            // 3. 低地：同じ河口へ注ぐ流域。下流から上流の順に、流れ先の河口を引き継ぐ
            var outlet = new int[count];
            var basinAtOutlet = new int[count];
            var cellBasin = part;
            for (int i = 0; i < count; i++)
            {
                basinAtOutlet[i] = -1;
                cellBasin[i] = -1;
            }
            var basinSize = new List<int>();
            var basinMass = new List<int>();
            for (int o = 0; o < router.Count; o++)
            {
                int i = router.Order[o];
                int r = router.Receiver[i];
                outlet[i] = r >= 0 && land[r] ? outlet[r] : i;
                if (label[i] != -1 || massClass[mass[i]] != 2) continue;
                int b = basinAtOutlet[outlet[i]];
                if (b < 0)
                {
                    b = basinSize.Count;
                    basinAtOutlet[outlet[i]] = b;
                    basinSize.Add(0);
                    basinMass.Add(mass[i]);
                }
                basinSize[b]++;
                cellBasin[i] = b;
            }

            // 4. 小さな流域は、境をいちばん長く接する隣の流域へ、流域ごと合わせる。境界は分水嶺のまま残る
            int basins = basinSize.Count;
            var size = basinSize.ToArray();
            var parent = new int[basins];
            var adjacency = new Dictionary<int, int>[basins];
            for (int b = 0; b < basins; b++)
            {
                parent[b] = b;
                adjacency[b] = new Dictionary<int, int>();
            }
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = x + y * n;
                    int a = cellBasin[i];
                    if (a < 0) continue;
                    if (x + 1 < n) Touch(adjacency, a, cellBasin[i + 1]);
                    if (y + 1 < n) Touch(adjacency, a, cellBasin[i + n]);
                }
            }
            var bySize = new SortedSet<long>();
            for (int b = 0; b < basins; b++) bySize.Add(Key(size[b], b));
            while (bySize.Count > 0)
            {
                long first = bySize.Min;
                int small = (int)(first & 0xFFFFFFFFL);
                if (size[small] >= lowlandMin) break;
                bySize.Remove(first);
                Dictionary<int, int> edges = adjacency[small];
                if (edges.Count == 0) continue;
                int best = -1;
                int bestBorder = -1;
                foreach (KeyValuePair<int, int> edge in edges)
                {
                    bool better = edge.Value > bestBorder
                        || (edge.Value == bestBorder && (size[edge.Key] > size[best] || (size[edge.Key] == size[best] && edge.Key < best)));
                    if (!better) continue;
                    best = edge.Key;
                    bestBorder = edge.Value;
                }
                bySize.Remove(Key(size[best], best));
                parent[small] = best;
                size[best] += size[small];
                Dictionary<int, int> target = adjacency[best];
                foreach (KeyValuePair<int, int> edge in edges)
                {
                    int neighbour = edge.Key;
                    if (neighbour == best) continue;
                    Dictionary<int, int> other = adjacency[neighbour];
                    other.Remove(small);
                    int shared;
                    target.TryGetValue(neighbour, out shared);
                    target[neighbour] = shared + edge.Value;
                    other[best] = shared + edge.Value;
                }
                target.Remove(small);
                edges.Clear();
                bySize.Add(Key(size[best], best));
            }

            // 独り立ちした流域をエリアにする。山地も流域も無い陸地には、いちばん広い流域を 1 つ残す
            var basinDraft = new int[basins];
            var massBest = new int[massSize.Count];
            for (int m = 0; m < massBest.Length; m++) massBest[m] = -1;
            for (int b = 0; b < basins; b++)
            {
                basinDraft[b] = -1;
                if (parent[b] != b) continue;
                int m = basinMass[b];
                if (size[b] >= lowlandMin)
                {
                    basinDraft[b] = drafts.Count;
                    drafts.Add(new Draft { Source = Source.River });
                    massHasArea[m] = true;
                }
                else if (massBest[m] < 0 || size[b] > size[massBest[m]])
                {
                    massBest[m] = b;
                }
            }
            for (int m = 0; m < massSize.Count; m++)
            {
                if (massClass[m] != 2 || massHasArea[m] || massBest[m] < 0) continue;
                basinDraft[massBest[m]] = drafts.Count;
                drafts.Add(new Draft { Source = Source.River });
            }
            for (int i = 0; i < count; i++)
            {
                int b = cellBasin[i];
                if (b < 0) continue;
                while (parent[b] != b) b = parent[b];
                if (basinDraft[b] >= 0) label[i] = basinDraft[b];
            }

            // 5. 島
            var massDraft = new int[massSize.Count];
            for (int m = 0; m < massSize.Count; m++)
            {
                massDraft[m] = -1;
                if (massClass[m] != 1) continue;
                massDraft[m] = drafts.Count;
                drafts.Add(new Draft { Source = Source.Island });
            }
            for (int i = 0; i < count; i++)
            {
                if (land[i] && massDraft[mass[i]] >= 0) label[i] = massDraft[mass[i]];
            }

            if (drafts.Count == 0) return;
            if (drafts.Count > 255) drafts.RemoveRange(255, drafts.Count - 255);

            // 6. 残った陸（独り立ちしない小さな高地・山の裏の狭い海岸・小島）と海を、いちばん近いエリアへ寄せる
            var near = new int[count];
            var distance = new int[count];
            int tail = 0;
            for (int i = 0; i < count; i++)
            {
                if (label[i] >= drafts.Count) label[i] = -1;
                near[i] = label[i] >= 0 ? label[i] : -1;
                if (near[i] >= 0) queue[tail++] = i;
            }
            for (int head = 0; head < tail; head++)
            {
                int c = queue[head];
                int cx = c % n;
                int cy = c / n;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + Dx[k];
                    int ny = cy + Dy[k];
                    if (nx < 0 || ny < 0 || nx >= n || ny >= n) continue;
                    int j = nx + ny * n;
                    if (near[j] >= 0) continue;
                    near[j] = near[c];
                    if (land[j]) label[j] = near[c];
                    queue[tail++] = j;
                }
            }

            // 7. エリアごとの広さ・高さ・土地の種類・境界
            float[] height = data.Height;
            for (int i = 0; i < count; i++)
            {
                int d = label[i];
                if (d < 0) continue;
                Draft draft = drafts[d];
                draft.Cells++;
                draft.SumHeight += height[i];
                if (height[i] > draft.MaxHeight) draft.MaxHeight = height[i];
                draft.Biomes[(int)data.Biome[i]]++;
            }

            // 境界からの深さ。名前は、エリアの中でいちばん深い所に置く
            tail = 0;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = x + y * n;
                    distance[i] = -1;
                    int d = label[i];
                    if (d < 0) continue;
                    bool edge = false;
                    for (int k = 0; k < 4; k++)
                    {
                        int nx = x + Dx[k];
                        int ny = y + Dy[k];
                        int other = nx < 0 || ny < 0 || nx >= n || ny >= n ? -2 : label[nx + ny * n];
                        if (other == d) continue;
                        edge = true;
                        drafts[d].Border++;
                        if (other >= 0 && drafts[other].Source == Source.Mountains) drafts[d].MountainBorder++;
                    }
                    if (!edge) continue;
                    distance[i] = 0;
                    queue[tail++] = i;
                }
            }
            for (int head = 0; head < tail; head++)
            {
                int c = queue[head];
                int cx = c % n;
                int cy = c / n;
                Draft draft = drafts[label[c]];
                if (distance[c] > draft.BestDepth)
                {
                    draft.BestDepth = distance[c];
                    draft.LabelCell = c;
                }
                for (int k = 0; k < 4; k++)
                {
                    int nx = cx + Dx[k];
                    int ny = cy + Dy[k];
                    if (nx < 0 || ny < 0 || nx >= n || ny >= n) continue;
                    int j = nx + ny * n;
                    if (label[j] < 0 || distance[j] >= 0) continue;
                    distance[j] = distance[c] + 1;
                    queue[tail++] = j;
                }
            }

            // 8. 種類と仮の呼び名を決め、北から南・西から東の順に番号を振る
            float cellKm2 = data.CellMeters * data.CellMeters / 1000000f;
            var ordered = new List<Draft>();
            for (int d = 0; d < drafts.Count; d++)
            {
                Draft draft = drafts[d];
                if (draft.Cells == 0) continue;
                var area = new RegionArea();
                area.LabelX = data.MapXFromCell(draft.LabelCell);
                area.LabelY = data.MapYFromCell(draft.LabelCell);
                area.AreaKm2 = draft.Cells * cellKm2;
                area.MeanHeight = (float)(draft.SumHeight / draft.Cells);
                area.MaxHeight = draft.MaxHeight;
                area.MainBiome = MainBiome(draft.Biomes);
                area.Kind = Classify(draft, area, data.MaxHeight);
                area.Basis = Basis(draft, area.Kind, mountainLine);
                draft.Area = area;
                ordered.Add(draft);
            }
            ordered.Sort((a, b) =>
            {
                if (a.Area.LabelY != b.Area.LabelY) return a.Area.LabelY.CompareTo(b.Area.LabelY);
                if (a.Area.LabelX != b.Area.LabelX) return a.Area.LabelX.CompareTo(b.Area.LabelX);
                return a.LabelCell.CompareTo(b.LabelCell);
            });
            for (int a = 0; a < ordered.Count; a++)
            {
                ordered[a].Area.Id = a + 1;
                data.Areas.Add(ordered[a].Area);
            }
            Name(data.Areas);

            byte[] result = data.Area;
            for (int i = 0; i < count; i++)
            {
                int d = land[i] ? label[i] : near[i];
                result[i] = d >= 0 && drafts[d].Area != null ? (byte)drafts[d].Area.Id : (byte)0;
            }
            TrimOpenSea(data, land, queue, Math.Max(2, (int)(n * CoastalWatersFraction)));
        }

        private static long Key(int size, int basin)
        {
            return ((long)size << 32) | (uint)basin;
        }

        /// <summary>隣り合う 2 つの流域の、接する長さを 1 増やす。</summary>
        private static void Touch(Dictionary<int, int>[] adjacency, int a, int b)
        {
            if (b < 0 || a == b) return;
            int shared;
            adjacency[a].TryGetValue(b, out shared);
            adjacency[a][b] = shared + 1;
            adjacency[b][a] = shared + 1;
        }

        /// <summary>岸から coastal セルより遠い海を 0（外洋）へ戻す。</summary>
        private static void TrimOpenSea(RegionData data, bool[] land, int[] queue, int coastal)
        {
            int n = data.Resolution;
            int count = n * n;
            var reach = new int[count];
            int tail = 0;
            for (int i = 0; i < count; i++)
            {
                reach[i] = land[i] ? 0 : -1;
                if (land[i]) queue[tail++] = i;
            }
            for (int head = 0; head < tail; head++)
            {
                int c = queue[head];
                if (reach[c] >= coastal) continue;
                int cx = c % n;
                int cy = c / n;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + Dx[k];
                    int ny = cy + Dy[k];
                    if (nx < 0 || ny < 0 || nx >= n || ny >= n) continue;
                    int j = nx + ny * n;
                    if (reach[j] >= 0) continue;
                    reach[j] = reach[c] + 1;
                    queue[tail++] = j;
                }
            }
            byte[] area = data.Area;
            for (int i = 0; i < count; i++)
            {
                if (reach[i] < 0) area[i] = 0;
            }
        }

        private static RegionBiome MainBiome(int[] biomes)
        {
            int best = (int)RegionBiome.Grassland;
            int bestCount = -1;
            for (int b = 0; b < biomes.Length; b++)
            {
                if (b == (int)RegionBiome.Ocean || b == (int)RegionBiome.Coast || b == (int)RegionBiome.River || b == (int)RegionBiome.Lake) continue;
                if (biomes[b] > bestCount)
                {
                    bestCount = biomes[b];
                    best = b;
                }
            }
            return (RegionBiome)best;
        }

        /// <summary>
        /// 地形の種類。まず成り立ち（山地・島・山に囲まれた盆地）、次に極端な土地（雪原・砂漠）、最後に平均の標高
        /// （高地・丘陵・平野）で決める。森か草原かは地形の名前にしない（RegionArea.MainBiome が持つ）。
        /// </summary>
        private static RegionAreaKind Classify(Draft draft, RegionArea area, float regionMaxHeight)
        {
            if (draft.Source == Source.Mountains) return RegionAreaKind.Mountains;
            if (draft.Source == Source.Island) return RegionAreaKind.Island;

            int[] b = draft.Biomes;
            float total = 0f;
            for (int k = 0; k < b.Length; k++)
            {
                if (k == (int)RegionBiome.River || k == (int)RegionBiome.Lake || k == (int)RegionBiome.Coast) continue;
                total += b[k];
            }
            if (total > 0f)
            {
                float cold = (b[(int)RegionBiome.Snow] + b[(int)RegionBiome.Tundra]) / total;
                float dry = b[(int)RegionBiome.Desert] / total;
                if (cold >= 0.4f) return RegionAreaKind.Snowfield;
                if (dry >= 0.35f) return RegionAreaKind.Desert;
            }
            if (draft.Border > 0 && draft.MountainBorder > draft.Border * 0.6f) return RegionAreaKind.Basin;
            if (area.MeanHeight >= regionMaxHeight * 0.2f) return RegionAreaKind.Highland;
            if (area.MeanHeight >= regionMaxHeight * 0.07f) return RegionAreaKind.Hills;
            return RegionAreaKind.Plain;
        }

        private static string Basis(Draft draft, RegionAreaKind kind, float mountainLine)
        {
            switch (draft.Source)
            {
                case Source.Mountains:
                    return "ならした標高が " + ((int)mountainLine).ToString("#,0", System.Globalization.CultureInfo.InvariantCulture) + " m を超える、ひと続きの高地";
                case Source.Island:
                    return "本土から離れた、ひと続きの陸地";
                default:
                    return kind == RegionAreaKind.Basin
                        ? "同じ河口へ注ぐ流域で、周りの大半を山地に囲まれる"
                        : "同じ河口へ注ぐ流域（境は分水嶺。隣の小さな流域を含む）";
            }
        }

        /// <summary>地形の種類の呼び方。</summary>
        public static string KindNoun(RegionAreaKind kind)
        {
            switch (kind)
            {
                case RegionAreaKind.Mountains: return "山地";
                case RegionAreaKind.Highland: return "高地";
                case RegionAreaKind.Hills: return "丘陵";
                case RegionAreaKind.Basin: return "盆地";
                case RegionAreaKind.Island: return "島";
                case RegionAreaKind.Snowfield: return "雪原";
                case RegionAreaKind.Desert: return "砂漠";
                default: return "平野";
            }
        }

        /// <summary>地図を 3×3 に分けた方角の呼び方（中央の升は 38〜62）。</summary>
        public static string Direction(float mapX, float mapY)
        {
            int col = mapX < 38f ? 0 : (mapX < 62f ? 1 : 2);
            int row = mapY < 38f ? 0 : (mapY < 62f ? 1 : 2);
            string[] names =
            {
                "北西", "北", "北東",
                "西", "中央", "東",
                "南西", "南", "南東",
            };
            return names[col + row * 3];
        }

        /// <summary>「方角＋の＋地形」。同じ呼び名が重なる時は、広い順に 第二・第三 を付ける。</summary>
        private static void Name(List<RegionArea> areas)
        {
            var groups = new Dictionary<string, List<RegionArea>>();
            for (int a = 0; a < areas.Count; a++)
            {
                RegionArea area = areas[a];
                string key = Direction(area.LabelX, area.LabelY) + "の|" + KindNoun(area.Kind);
                List<RegionArea> group;
                if (!groups.TryGetValue(key, out group))
                {
                    group = new List<RegionArea>();
                    groups.Add(key, group);
                }
                group.Add(area);
            }
            string[] ordinal = { "", "第二", "第三", "第四", "第五", "第六", "第七", "第八", "第九" };
            foreach (KeyValuePair<string, List<RegionArea>> pair in groups)
            {
                List<RegionArea> group = pair.Value;
                group.Sort((a, b) => a.AreaKm2 != b.AreaKm2 ? b.AreaKm2.CompareTo(a.AreaKm2) : a.Id.CompareTo(b.Id));
                string[] halves = pair.Key.Split('|');
                for (int g = 0; g < group.Count; g++)
                {
                    string order = g < ordinal.Length ? ordinal[g] : "第" + (g + 1);
                    group[g].Name = halves[0] + order + halves[1];
                }
            }
        }

        /// <summary>mask が true のセルを、上下左右につながるまとまりへ分ける。戻り値は各まとまりのセル数。</summary>
        private static List<int> Components(bool[] mask, int n, int[] component, int[] queue)
        {
            int count = n * n;
            var sizes = new List<int>();
            for (int i = 0; i < count; i++) component[i] = -1;
            for (int start = 0; start < count; start++)
            {
                if (!mask[start] || component[start] >= 0) continue;
                int id = sizes.Count;
                int tail = 0;
                queue[tail++] = start;
                component[start] = id;
                for (int head = 0; head < tail; head++)
                {
                    int c = queue[head];
                    int cx = c % n;
                    int cy = c / n;
                    for (int k = 0; k < 4; k++)
                    {
                        int nx = cx + Dx[k];
                        int ny = cy + Dy[k];
                        if (nx < 0 || ny < 0 || nx >= n || ny >= n) continue;
                        int j = nx + ny * n;
                        if (!mask[j] || component[j] >= 0) continue;
                        component[j] = id;
                        queue[tail++] = j;
                    }
                }
                sizes.Add(tail);
            }
            return sizes;
        }

        /// <summary>標高（海は 0 として）を、半径 radius の箱で 2 回ならす。</summary>
        private static float[] Smooth(float[] height, int n, int radius)
        {
            int count = n * n;
            var a = new float[count];
            var b = new float[count];
            for (int i = 0; i < count; i++) a[i] = height[i] > 0f ? height[i] : 0f;
            for (int pass = 0; pass < 2; pass++)
            {
                Blur(a, b, n, radius, true);
                Blur(b, a, n, radius, false);
            }
            return a;
        }

        private static void Blur(float[] source, float[] target, int n, int radius, bool horizontal)
        {
            int stepIn = horizontal ? 1 : n;
            int stepOut = horizontal ? n : 1;
            float inv = 1f / (radius * 2 + 1);
            for (int line = 0; line < n; line++)
            {
                int origin = line * stepOut;
                float sum = 0f;
                for (int k = -radius; k <= radius; k++)
                {
                    int p = k < 0 ? 0 : (k >= n ? n - 1 : k);
                    sum += source[origin + p * stepIn];
                }
                for (int p = 0; p < n; p++)
                {
                    target[origin + p * stepIn] = sum * inv;
                    int leave = p - radius;
                    int enter = p + radius + 1;
                    if (leave < 0) leave = 0;
                    if (enter >= n) enter = n - 1;
                    sum += source[origin + enter * stepIn] - source[origin + leave * stepIn];
                }
            }
        }
    }

    /// <summary>エリアの境界を地図へ重ねる層を作る。</summary>
    public static class RegionAreas
    {
        /// <summary>
        /// 陸のエリアの境界線を RGBA32 で作る（北西の角から行順、線は白、それ以外は透明）。
        /// 海岸線は引かない（地図の絵で分かるため）。
        /// </summary>
        public static byte[] RenderBoundaries(RegionData data)
        {
            int n = data.Resolution;
            var pixels = new byte[n * n * 4];
            byte[] area = data.Area;
            float[] height = data.Height;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = x + y * n;
                    if (height[i] <= 0f || area[i] == 0) continue;
                    bool edge = false;
                    if (x + 1 < n)
                    {
                        int j = i + 1;
                        edge = height[j] > 0f && area[j] != 0 && area[j] != area[i];
                    }
                    if (!edge && y + 1 < n)
                    {
                        int j = i + n;
                        edge = height[j] > 0f && area[j] != 0 && area[j] != area[i];
                    }
                    if (!edge) continue;
                    Mark(pixels, n, x, y, 235);
                    Mark(pixels, n, x + 1, y, 120);
                    Mark(pixels, n, x, y + 1, 120);
                    Mark(pixels, n, x - 1, y, 120);
                    Mark(pixels, n, x, y - 1, 120);
                }
            }
            return pixels;
        }

        private static void Mark(byte[] pixels, int n, int x, int y, byte alpha)
        {
            if (x < 0 || y < 0 || x >= n || y >= n) return;
            int p = (x + y * n) * 4;
            pixels[p] = 255;
            pixels[p + 1] = 255;
            pixels[p + 2] = 255;
            if (pixels[p + 3] < alpha) pixels[p + 3] = alpha;
        }
    }
}
