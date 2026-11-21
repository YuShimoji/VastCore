using System;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 陸のセルを「海に近い順（下流から上流）」に並べ、各セルの水の流れ先を決める。
    /// Priority-Flood（Barnes ほか 2014）で窪地を埋めた水面を作り、その水面を下る向きへ流す。
    /// 海と地図の外周が出口になる。
    /// </summary>
    internal sealed class FlowRouter
    {
        private static readonly int[] Dx = { 1, 1, 0, -1, -1, -1, 0, 1 };
        private static readonly int[] Dy = { 0, 1, 1, 1, 0, -1, -1, -1 };
        private const float Diagonal = 1.4142135f;
        private const float Epsilon = 0.01f;

        private readonly int _n;
        private readonly bool[] _closed;
        private float[] _heapKey;
        private int[] _heapValue;
        private int _heapCount;

        /// <summary>陸のセルを下流から上流の順に並べたもの。</summary>
        public readonly int[] Order;
        public int Count;

        /// <summary>水の流れ先のセル。出口（地図の外）なら -1。</summary>
        public readonly int[] Receiver;

        /// <summary>窪地を埋めた後の水面の高さ（m）。</summary>
        public readonly float[] Filled;

        public FlowRouter(int resolution)
        {
            _n = resolution;
            int count = resolution * resolution;
            _closed = new bool[count];
            Order = new int[count];
            Receiver = new int[count];
            Filled = new float[count];
            _heapKey = new float[1024];
            _heapValue = new int[1024];
        }

        public void Route(float[] height, bool[] land)
        {
            int n = _n;
            int count = n * n;
            Array.Clear(_closed, 0, count);
            _heapCount = 0;
            Count = 0;

            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = x + y * n;
                    Receiver[i] = -1;
                    if (!land[i])
                    {
                        _closed[i] = true;
                        Filled[i] = 0f;
                        bool coastal = false;
                        for (int k = 0; k < 8 && !coastal; k++)
                        {
                            int nx = x + Dx[k];
                            int ny = y + Dy[k];
                            if (nx < 0 || ny < 0 || nx >= n || ny >= n) continue;
                            coastal = land[nx + ny * n];
                        }
                        if (coastal) Push(0f, i);
                    }
                    else if (x == 0 || y == 0 || x == n - 1 || y == n - 1)
                    {
                        _closed[i] = true;
                        Filled[i] = height[i];
                        Push(height[i], i);
                    }
                }
            }

            while (_heapCount > 0)
            {
                float key;
                int c;
                Pop(out key, out c);
                if (land[c]) Order[Count++] = c;
                int cx = c % n;
                int cy = c / n;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + Dx[k];
                    int ny = cy + Dy[k];
                    if (nx < 0 || ny < 0 || nx >= n || ny >= n) continue;
                    int j = nx + ny * n;
                    if (_closed[j]) continue;
                    _closed[j] = true;
                    float min = key + ((k & 1) == 0 ? Epsilon : Epsilon * Diagonal);
                    float f = height[j];
                    if (f < min) f = min;
                    Filled[j] = f;
                    Push(f, j);
                }
            }

            for (int o = 0; o < Count; o++)
            {
                int i = Order[o];
                int cx = i % n;
                int cy = i / n;
                float fi = Filled[i];
                int best = -1;
                float bestSlope = 0f;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + Dx[k];
                    int ny = cy + Dy[k];
                    if (nx < 0 || ny < 0 || nx >= n || ny >= n) continue;
                    int j = nx + ny * n;
                    float s = fi - (land[j] ? Filled[j] : 0f);
                    if ((k & 1) != 0) s *= 0.70710678f;
                    if (s > bestSlope)
                    {
                        bestSlope = s;
                        best = j;
                    }
                }
                Receiver[i] = best;
            }
        }

        /// <summary>各セルへ上流から集まる量を求める。weight が null なら 1 セル = 1。</summary>
        public void Accumulate(float[] weight, bool[] land, float[] result)
        {
            Array.Clear(result, 0, result.Length);
            for (int o = 0; o < Count; o++)
            {
                int i = Order[o];
                result[i] = weight != null ? weight[i] : 1f;
            }
            for (int o = Count - 1; o >= 0; o--)
            {
                int i = Order[o];
                int r = Receiver[i];
                if (r >= 0 && land[r]) result[r] += result[i];
            }
        }

        private void Push(float key, int value)
        {
            if (_heapCount == _heapKey.Length)
            {
                Array.Resize(ref _heapKey, _heapCount * 2);
                Array.Resize(ref _heapValue, _heapCount * 2);
            }
            int i = _heapCount++;
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (_heapKey[parent] <= key) break;
                _heapKey[i] = _heapKey[parent];
                _heapValue[i] = _heapValue[parent];
                i = parent;
            }
            _heapKey[i] = key;
            _heapValue[i] = value;
        }

        private void Pop(out float key, out int value)
        {
            key = _heapKey[0];
            value = _heapValue[0];
            _heapCount--;
            if (_heapCount == 0) return;
            float lastKey = _heapKey[_heapCount];
            int lastValue = _heapValue[_heapCount];
            int i = 0;
            while (true)
            {
                int child = i * 2 + 1;
                if (child >= _heapCount) break;
                if (child + 1 < _heapCount && _heapKey[child + 1] < _heapKey[child]) child++;
                if (_heapKey[child] >= lastKey) break;
                _heapKey[i] = _heapKey[child];
                _heapValue[i] = _heapValue[child];
                i = child;
            }
            _heapKey[i] = lastKey;
            _heapValue[i] = lastValue;
        }
    }
}
