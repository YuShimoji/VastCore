using System;
using System.Collections.Generic;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 侵食の前の地形にある大きな窪地を湖として残す。侵食は窪地を土砂で埋めるので、何もしなければ湖は残らない。
    /// 窪地は隆起させない（沈む盆地）。水面は侵食の前に窪地が溢れる高さのまま変えず、上流の川はそこを下流の基準にする。
    /// 水面より下の湖底は削らず埋めず、湖の周り（岸）のセルは水面より下へ削らせない。水が引いた所は陸として削る。
    /// 粗い格子と細かい格子で同じ湖を使う。
    /// </summary>
    internal sealed class RegionLakeMask
    {
        // 湖にする窪地の下限。ノイズの凹凸でできる小さな穴は除き、地形の起伏でできた窪地だけを残す。
        internal const float MinimumAreaKm2 = 2f;
        internal const float MinimumDepthMeters = 6f;
        // 湖にしてよい隆起の強さの上限。山地・丘陵は湖にすると隆起が止まり山が育たないので、平野の窪地だけを湖にする
        internal const float MaximumUplift = 0.06f;
        // 侵食の後に残す水たまりの下限。これより小さい・浅いものは土砂で埋まったものとする（岸の点々を残さない）。
        internal const float MinimumPoolKm2 = 0.5f;
        internal const float MinimumPoolDepthMeters = 0.5f;

        internal readonly int Resolution;
        /// <summary>セルの湖の番号。湖でなければ -1。</summary>
        internal readonly int[] Id;
        /// <summary>湖ごとの水面の標高（m）。侵食の間は変えない。</summary>
        internal readonly float[] Level;
        // 湖ごとの岸のセル（湖に 8 近傍で接する、湖でないセル）
        private readonly int[][] _rims;

        private RegionLakeMask(int resolution, int[] id, int lakes)
        {
            Resolution = resolution;
            Id = id;
            Level = new float[lakes];
            var rims = new List<int>[lakes];
            for (int l = 0; l < lakes; l++) rims[l] = new List<int>();
            int n = resolution;
            var marked = new HashSet<long>();
            for (int i = 0; i < id.Length; i++)
            {
                int l = id[i];
                if (l < 0) continue;
                int x = i % n, y = i / n;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= n || ny >= n) continue;
                    int j = nx + ny * n;
                    if (id[j] >= 0) continue;
                    if (marked.Add((long)l * id.Length + j)) rims[l].Add(j);
                }
            }
            _rims = new int[lakes][];
            for (int l = 0; l < lakes; l++) _rims[l] = rims[l].ToArray();
        }

        internal int Count => Level.Length;

        /// <summary>
        /// ノイズの凹凸を除いた下地（smooth）から窪地を探し、下限を満たすものを湖にする。海とつながる低地は湖にしない。
        /// 隆起の強い所（uplift が MaximumUplift を超える丘陵・山地）は湖から外し、他と同じく隆起させる。
        /// </summary>
        internal static RegionLakeMask Find(float[] smooth, float[] uplift, int n, float cellMeters)
        {
            float cellKm2 = cellMeters * cellMeters / 1e6f;
            var id = new int[smooth.Length];
            for (int i = 0; i < id.Length; i++) id[i] = -1;
            var levels = new List<float>();
            RegionLake.VisitBasins(smooth, n, true, (spill, cells, depth, touchesSea) =>
            {
                if (touchesSea || depth < MinimumDepthMeters) return;
                int kept = 0;
                foreach (int i in cells) if (uplift[i] <= MaximumUplift) kept++;
                if (kept * cellKm2 < MinimumAreaKm2) return;
                foreach (int i in cells) if (uplift[i] <= MaximumUplift) id[i] = levels.Count;
                levels.Add(spill);
            });
            var mask = new RegionLakeMask(n, id, levels.Count);
            for (int l = 0; l < levels.Count; l++) mask.Level[l] = levels[l];
            return mask;
        }

        /// <summary>半分の解像度の格子へ写す（粗い格子のセル (x, y) は細かい格子の (2x, 2y)）。</summary>
        internal RegionLakeMask Half()
        {
            int half = Resolution / 2;
            var id = new int[half * half];
            for (int y = 0; y < half; y++)
            for (int x = 0; x < half; x++)
                id[x + y * half] = Id[x * 2 + y * 2 * Resolution];
            var mask = new RegionLakeMask(half, id, Level.Length);
            Array.Copy(Level, mask.Level, Level.Length);
            return mask;
        }

        internal bool IsLake(int i) => Id[i] >= 0;

        /// <summary>流れの計算に使う水面（湖のセルは水面、それ以外は地面）。</summary>
        internal void Surface(float[] h, float[] surface)
        {
            Array.Copy(h, surface, h.Length);
            for (int i = 0; i < Id.Length; i++)
            {
                int l = Id[i];
                if (l >= 0 && surface[i] < Level[l]) surface[i] = Level[l];
            }
        }

        /// <summary>岸のセルを水面より下へ下げない（流出口が削れて湖が抜けるのを防ぐ）。</summary>
        internal void HoldRims(float[] h, bool[] land)
        {
            for (int l = 0; l < _rims.Length; l++)
            {
                float level = Level[l];
                foreach (int j in _rims[l])
                    if (land[j] && h[j] < level) h[j] = level;
            }
        }

        /// <summary>
        /// 湖の深さを書き込む。水面は、侵食の後の地形で水が溢れ出す高さ（窪地を埋めた高さ）にする。
        /// 水が引いて湖が分かれた時は、それぞれが自分の流出口の高さを持つ。
        /// 湖の範囲から始まる水のつながりが小さい・浅い時は、土砂で埋まったものとして水面の高さの陸にする。
        /// </summary>
        internal void Write(RegionData data)
        {
            float[] h = data.Height;
            int n = Resolution;
            float cellKm2 = data.CellMeters * data.CellMeters / 1e6f;
            float[] filled = RegionLake.FillToOutlet(h, n, true);
            var seen = new bool[h.Length];
            var pool = new List<int>();
            int lakes = 0;
            for (int start = 0; start < Id.Length; start++)
            {
                if (Id[start] < 0 || seen[start] || h[start] <= 0f || filled[start] <= h[start]) continue;
                pool.Clear();
                pool.Add(start);
                seen[start] = true;
                float deepest = 0f;
                for (int cursor = 0; cursor < pool.Count; cursor++)
                {
                    int i = pool[cursor];
                    deepest = Math.Max(deepest, filled[i] - h[i]);
                    int x = i % n, y = i / n;
                    for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= n || ny >= n) continue;
                        int j = nx + ny * n;
                        if (seen[j] || h[j] <= 0f || filled[j] <= h[j]) continue;
                        seen[j] = true;
                        pool.Add(j);
                    }
                }
                bool keep = pool.Count * cellKm2 >= MinimumPoolKm2 && deepest >= MinimumPoolDepthMeters;
                foreach (int i in pool)
                {
                    if (keep) data.LakeDepth[i] = filled[i] - h[i];
                    else h[i] = filled[i];
                }
                if (keep) lakes++;
            }
            data.RetainedLakes = lakes > 0;
        }
    }

    internal static class RegionLake
    {
        /// <summary>水面は湖ごとに違うので、湖のセルの水面は 地面 + 深さ で読む。</summary>
        internal static float SurfaceAt(RegionData data, int i) => data.Height[i] + data.LakeDepth[i];

        /// <summary>
        /// 湖のセルの流れ先を付け直し、流量を数える。流路の計算は湖面に 1 マス 1 cm の勾配を足すので、湖の奥のセルが
        /// 水面よりわずかに高い岸を流れ先に選び、偽の流出口ができる。湖ごとに、水面以下の岸のうちいちばん低い所を
        /// 本当の流出口とし、湖の中は流出口からの幅優先探索の木に沿って流す（流出口が無い湖は、いちばん深い所で止まる）。
        /// 流れ先を変えたので、流量は取り出し順ではなく、流れ先の木の葉から順に足す。
        /// </summary>
        internal static void RouteThroughLakes(RegionData data, FlowRouter router, float[] weight, bool[] land, float[] result)
        {
            int n = data.Resolution;
            int count = data.Height.Length;
            int[] receiver = router.Receiver;
            var seen = new bool[count];
            var lake = new List<int>();
            for (int start = 0; start < count; start++)
            {
                if (seen[start] || data.LakeDepth[start] <= 0f) continue;
                lake.Clear();
                lake.Add(start);
                seen[start] = true;
                for (int cursor = 0; cursor < lake.Count; cursor++)
                {
                    int i = lake[cursor];
                    int x = i % n, y = i / n;
                    for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= n || ny >= n) continue;
                        int j = nx + ny * n;
                        if (seen[j] || data.LakeDepth[j] <= 0f) continue;
                        seen[j] = true;
                        lake.Add(j);
                    }
                }

                // 本当の流出口：水面以下（丸めの 1 cm まで）の岸のうち、いちばん低い所。海に面していれば海へ
                int outlet = -1, exit = -1;
                float exitHeight = float.MaxValue;
                int deepest = start;
                foreach (int i in lake)
                {
                    if (data.LakeDepth[i] > data.LakeDepth[deepest]) deepest = i;
                    float surface = SurfaceAt(data, i);
                    int x = i % n, y = i / n;
                    for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= n || ny >= n) continue;
                        int j = nx + ny * n;
                        if (data.LakeDepth[j] > 0f) continue;
                        float g = data.Height[j] > 0f ? data.Height[j] : 0f;
                        if (g > surface + 0.01f || g >= exitHeight) continue;
                        exitHeight = g;
                        outlet = i;
                        exit = j;
                    }
                }

                int root = outlet >= 0 ? outlet : deepest;
                receiver[root] = exit;
                // 流出口から湖の中へ幅優先にたどり、たどってきた向きを流れ先にする
                var queue = new Queue<int>();
                var reached = new HashSet<int> { root };
                queue.Enqueue(root);
                while (queue.Count > 0)
                {
                    int i = queue.Dequeue();
                    int x = i % n, y = i / n;
                    for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= n || ny >= n) continue;
                        int j = nx + ny * n;
                        if (data.LakeDepth[j] <= 0f || !reached.Add(j)) continue;
                        receiver[j] = i;
                        queue.Enqueue(j);
                    }
                }
            }

            // 流量は流れ先の木の葉から順に足す（受け取る流れが残っていないセルから流す）
            var pending = new int[count];
            Array.Clear(result, 0, count);
            for (int o = 0; o < router.Count; o++)
            {
                int i = router.Order[o];
                result[i] = weight != null ? weight[i] : 1f;
                int r = receiver[i];
                if (r >= 0 && land[r]) pending[r]++;
            }
            var ready = new Queue<int>();
            for (int o = 0; o < router.Count; o++)
            {
                int i = router.Order[o];
                if (pending[i] == 0) ready.Enqueue(i);
            }
            while (ready.Count > 0)
            {
                int i = ready.Dequeue();
                int r = receiver[i];
                if (r < 0 || !land[r]) continue;
                result[r] += result[i];
                if (--pending[r] == 0) ready.Enqueue(r);
            }
        }

        internal static void VisitBasins(float[] height, int n, bool inlandSeaIsOutlet,
            Action<float, List<int>, float, bool> visit)
        {
            float[] filled = FillToOutlet(height, n, inlandSeaIsOutlet);
            var cells = new List<int>();
            for (int i = 0; i < height.Length; i++)
            {
                float spill = filled[i];
                if (spill <= 0f || spill <= height[i]) continue;
                cells.Clear();
                cells.Add(i);
                filled[i] = height[i];
                float bottom = height[i];
                for (int cursor = 0; cursor < cells.Count; cursor++)
                {
                    int cell = cells[cursor];
                    // 外周の値は埋めていないので、窪地のセルは必ず内側にある。
                    for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int j = cell + dx + dy * n;
                        if (filled[j] != spill || height[j] >= spill) continue;
                        cells.Add(j);
                        filled[j] = height[j];
                        bottom = Math.Min(bottom, height[j]);
                    }
                }
                visit(spill, cells, spill - bottom, bottom <= 0f);
            }
        }

        // Barnes et al. (2014), Priority-Flood。水面には流路用の微小勾配を加えない。
        internal static float[] FillToOutlet(float[] height, int n, bool inlandSeaIsOutlet)
        {
            var filled = new float[height.Length];
            for (int i = 0; i < filled.Length; i++) filled[i] = float.NaN;
            var open = new SurfaceQueue(filled);
            var pit = new Queue<int>();

            for (int i = 0; i < height.Length; i++)
            {
                int x = i % n;
                int y = i / n;
                if (x != 0 && x != n - 1 && y != 0 && y != n - 1 && !(inlandSeaIsOutlet && height[i] <= 0f)) continue;
                filled[i] = height[i];
                open.Push(i);
            }

            while (pit.Count > 0 || open.Count > 0)
            {
                int i = pit.Count > 0 ? pit.Dequeue() : open.Pop();
                float level = filled[i];
                int x = i % n;
                int y = i / n;
                for (int row = Math.Max(0, y - 1); row <= Math.Min(n - 1, y + 1); row++)
                for (int col = Math.Max(0, x - 1); col <= Math.Min(n - 1, x + 1); col++)
                {
                    int j = col + row * n;
                    if (!float.IsNaN(filled[j])) continue;
                    float ground = height[j];
                    filled[j] = Math.Max(ground, level);
                    if (ground <= level) pit.Enqueue(j);
                    else open.Push(j);
                }
            }
            return filled;
        }

        private sealed class SurfaceQueue
        {
            private readonly float[] _height;
            private readonly List<int> _heap = new List<int>();

            public SurfaceQueue(float[] height) { _height = height; }
            public int Count => _heap.Count;

            public void Push(int cell)
            {
                int at = _heap.Count;
                _heap.Add(cell);
                while (at > 0)
                {
                    int parent = (at - 1) / 2;
                    if (_height[_heap[parent]] <= _height[cell]) break;
                    _heap[at] = _heap[parent];
                    at = parent;
                }
                _heap[at] = cell;
            }

            public int Pop()
            {
                int first = _heap[0];
                int last = _heap.Count - 1;
                int cell = _heap[last];
                _heap.RemoveAt(last);
                if (last == 0) return first;
                int at = 0;
                while (at < last / 2)
                {
                    int left = at * 2 + 1;
                    int right = left + 1;
                    int child = right < last && _height[_heap[right]] < _height[_heap[left]] ? right : left;
                    if (_height[cell] <= _height[_heap[child]]) break;
                    _heap[at] = _heap[child];
                    at = child;
                }
                _heap[at] = cell;
                return first;
            }
        }
    }
}
