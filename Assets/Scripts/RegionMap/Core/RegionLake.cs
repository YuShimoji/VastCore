using System;
using System.Collections.Generic;

namespace Vastcore.RegionMap
{
    /// <summary>広域の最終侵食で残す陸上の湖を一つ選び、湖底と流路を分けて扱う。</summary>
    internal sealed class RegionLake
    {
        private readonly List<int> _cells;
        private readonly float _level;

        private RegionLake(List<int> cells, float level)
        {
            _cells = cells;
            _level = level;
        }

        internal static RegionLake Select(RegionData data)
        {
            float cellKm = data.CellMeters / 1000f;
            float cellArea = cellKm * cellKm;
            RegionLake best = null;
            double bestVolume = 0;
            VisitBasins(data.Height, data.Resolution, false, (spill, cells, depth, touchesSea) =>
            {
                float area = cells.Count * cellArea;
                if (touchesSea || depth < 2f || area < 1f || area > 16f) return;

                // 小さな凹凸や広域を覆う水面を除き、堰の高さに余裕を残す。
                float level = spill - Math.Min(1f, depth * 0.1f);
                var wet = new List<int>();
                double volume = 0;
                foreach (int i in cells)
                {
                    float water = level - data.Height[i];
                    if (water <= 0f) continue;
                    wet.Add(i);
                    volume += water;
                }
                if (wet.Count * cellArea < 1f || volume <= bestVolume) return;
                best = new RegionLake(wet, level);
                bestVolume = volume;
            });
            if (best != null)
            {
                foreach (int i in best._cells) data.LakeDepth[i] = best._level - data.Height[i];
            }
            return best;
        }

        internal void Finish(RegionData data)
        {
            int n = data.Resolution;
            float level = _level;
            foreach (int i in _cells)
            {
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int j = i + dx + dy * n;
                    if (data.LakeDepth[j] == 0f) level = Math.Min(level, data.Height[j]);
                }
            }

            // 湖面が下がって複数の水たまりに分かれた場合、最も深い部分につながる岸を使う。
            int deepest = -1;
            foreach (int i in _cells)
            {
                data.LakeDepth[i] = Math.Max(0f, level - data.Height[i]);
                if (data.LakeDepth[i] > 0f && (deepest < 0 || data.Height[i] < data.Height[deepest])) deepest = i;
            }
            var connected = new HashSet<int>();
            if (deepest >= 0)
            {
                var pending = new Queue<int>();
                connected.Add(deepest);
                pending.Enqueue(deepest);
                while (pending.Count > 0)
                {
                    int i = pending.Dequeue();
                    for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int j = i + dx + dy * n;
                        if (data.LakeDepth[j] > 0f && connected.Add(j)) pending.Enqueue(j);
                    }
                }
                data.LakeSurfaceLevel = level;
            }
            foreach (int i in _cells)
                if (!connected.Contains(i)) data.LakeDepth[i] = 0f;
        }

        // 流路計算の仮想的な勾配と、実際に越えられる岸の高さを分ける。
        internal static void ConstrainOutlet(RegionData data, FlowRouter router)
        {
            if (float.IsNaN(data.LakeSurfaceLevel)) return;
            for (int o = 0; o < router.Count; o++)
            {
                int i = router.Order[o];
                int r = router.Receiver[i];
                if (r >= 0 && data.LakeDepth[i] > 0f && data.LakeDepth[r] <= 0f &&
                    data.Height[r] > data.LakeSurfaceLevel)
                    router.Receiver[i] = -1;
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
        private static float[] FillToOutlet(float[] height, int n, bool inlandSeaIsOutlet)
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
