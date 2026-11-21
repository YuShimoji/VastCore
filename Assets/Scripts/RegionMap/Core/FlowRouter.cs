using System;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 陸のセルを「海に近い順（下流から上流）」に並べ、各セルの水の流れ先を決める。
    /// Priority-Flood（Barnes ほか 2014）で窪地を埋めた水面を作り、その水面を下る向きへ流す。
    /// 海と地図の外周が出口になる。
    /// 「低い順に取り出す」待ち行列は、高さで区切った箱（バケット）の列で持つ。新しく入るセルは必ず今のセルより
    /// 少し高いので、箱を低い方から順に空けていけば低い順になる。二分ヒープより 1 セルあたりの手間が小さい。
    /// </summary>
    internal sealed class FlowRouter
    {
        private static readonly int[] Dx = { 1, 1, 0, -1, -1, -1, 0, 1 };
        private static readonly int[] Dy = { 0, 1, 1, 1, 0, -1, -1, -1 };
        private const float Diagonal = 1.4142135f;
        private const float Epsilon = 0.01f;

        private readonly int _n;
        private readonly bool[] _closed;
        // 海と陸の配置は侵食の途中で変わらないので、出口になるセルの一覧と最初の状態は 1 回だけ作って使い回す
        private bool[] _seedLand;
        private bool[] _closedAtStart;
        private int[] _seeds;
        private int _seedCount;
        private uint[] _jitterBits;
        // 箱 1 つの高さの幅は 1 / BucketsPerMeter（5 mm）。箱の中は、取り出す時に高さの順へ並べ直す
        private const float BucketsPerMeter = 200f;
        private int[] _bucketHead;
        private readonly int[] _bucketNext;
        private int _maxBucket;
        private int[] _run = new int[256];
        private float[] _runKey = new float[256];

        /// <summary>陸のセルを下流から上流の順に並べたもの。</summary>
        public readonly int[] Order;
        public int Count;

        /// <summary>水の流れ先のセル。出口（地図の外）なら -1。</summary>
        public readonly int[] Receiver;

        /// <summary>窪地を埋めた後の水面の高さ（m）。</summary>
        public readonly float[] Filled;

        /// <summary>
        /// 流れ先を選ぶときのゆらぎ（0 で無し）。8 方向から最も急な 1 つを選ぶだけだと、なめらかな斜面で
        /// 流れが平行な直線の束になる。セルごとに決まった量だけ比べ方をずらすと、流れがふらついて合流し、枝分かれした谷になる。
        /// </summary>
        public float Jitter;

        public uint JitterSeed;

        public FlowRouter(int resolution)
        {
            _n = resolution;
            int count = resolution * resolution;
            _closed = new bool[count];
            Order = new int[count];
            Receiver = new int[count];
            Filled = new float[count];
            _bucketNext = new int[count];
            _bucketHead = new int[1 << 16];
            for (int b = 0; b < _bucketHead.Length; b++) _bucketHead[b] = -1;
        }

        public void Route(float[] height, bool[] land)
        {
            int n = _n;
            int count = n * n;
            if (!ReferenceEquals(_seedLand, land)) PrepareSeeds(land);
            Array.Copy(_closedAtStart, _closed, count);
            _maxBucket = -1;
            Count = 0;

            // 出口（陸に接する海と、地図の外周の陸）を、セルの並び順のまま積む
            for (int s = 0; s < _seedCount; s++)
            {
                int i = _seeds[s];
                if (land[i]) Filled[i] = height[i];
                Push(Filled[i], i, 0);
            }

            // 箱を低い方から順に空ける。処理中に入るセルは必ず後ろの箱へ入る（_maxBucket は途中で伸びる）
            for (int bucket = 0; bucket <= _maxBucket; bucket++)
            {
                int head = _bucketHead[bucket];
                if (head < 0) continue;
                _bucketHead[bucket] = -1;
                int length = 0;
                for (int c = head; c >= 0; c = _bucketNext[c])
                {
                    if (length == _run.Length)
                    {
                        Array.Resize(ref _run, length * 2);
                        Array.Resize(ref _runKey, length * 2);
                    }
                    _run[length] = c;
                    _runKey[length] = Filled[c];
                    length++;
                }
                if (length > 1) SortRun(length);

                for (int q = 0; q < length; q++)
                {
                    int c = _run[q];
                    float key = _runKey[q];
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
                        Push(f, j, bucket + 1);
                    }
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
                uint bits = Jitter > 0f ? _jitterBits[i] : 0u;
                for (int k = 0; k < 8; k++)
                {
                    int nx = cx + Dx[k];
                    int ny = cy + Dy[k];
                    if (nx < 0 || ny < 0 || nx >= n || ny >= n) continue;
                    int j = nx + ny * n;
                    float s = fi - (land[j] ? Filled[j] : 0f);
                    if (s <= 0f) continue;
                    if ((k & 1) != 0) s *= 0.70710678f;
                    if (Jitter > 0f)
                    {
                        // 8 方向それぞれに 4 ビットずつ使い、傾きの比べ方をセルごとに少しずらす
                        float roll = (((bits >> (k * 4)) & 15u) + 0.5f) * (1f / 8f) - 1f;
                        s *= 1f + Jitter * roll;
                    }
                    if (s > bestSlope)
                    {
                        bestSlope = s;
                        best = j;
                    }
                }
                Receiver[i] = best;
            }
        }

        private void PrepareSeeds(bool[] land)
        {
            int n = _n;
            int count = n * n;
            _seedLand = land;
            _closedAtStart = new bool[count];
            _seeds = new int[count];
            _seedCount = 0;
            if (Jitter > 0f) _jitterBits = new uint[count];

            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = x + y * n;
                    Receiver[i] = -1;
                    if (_jitterBits != null) _jitterBits[i] = RegionNoise.Hash(x, y, JitterSeed);
                    if (!land[i])
                    {
                        _closedAtStart[i] = true;
                        Filled[i] = 0f;
                        bool coastal = false;
                        for (int k = 0; k < 8 && !coastal; k++)
                        {
                            int nx = x + Dx[k];
                            int ny = y + Dy[k];
                            if (nx < 0 || ny < 0 || nx >= n || ny >= n) continue;
                            coastal = land[nx + ny * n];
                        }
                        if (coastal) _seeds[_seedCount++] = i;
                    }
                    else if (x == 0 || y == 0 || x == n - 1 || y == n - 1)
                    {
                        _closedAtStart[i] = true;
                        _seeds[_seedCount++] = i;
                    }
                }
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

        /// <summary>セルを、その高さの箱へ入れる。floor は入れてよい最も低い箱（今空けている箱より後ろ）。</summary>
        private void Push(float key, int cell, int floor)
        {
            int bucket = (int)(key * BucketsPerMeter);
            if (bucket < floor) bucket = floor;
            if (bucket >= _bucketHead.Length)
            {
                int old = _bucketHead.Length;
                int size = old;
                while (size <= bucket) size *= 2;
                Array.Resize(ref _bucketHead, size);
                for (int b = old; b < size; b++) _bucketHead[b] = -1;
            }
            _bucketNext[cell] = _bucketHead[bucket];
            _bucketHead[bucket] = cell;
            if (bucket > _maxBucket) _maxBucket = bucket;
        }

        /// <summary>
        /// 取り出した箱の中身を高さの順に並べる（シェルソート）。実行環境の並べ替えに頼らないので、
        /// 同じ高さ同士の順番も、Unity とコンソールで同じになる。
        /// </summary>
        private void SortRun(int length)
        {
            int gap = 1;
            while (gap < length / 3) gap = gap * 3 + 1;
            for (; gap >= 1; gap /= 3)
            {
                for (int i = gap; i < length; i++)
                {
                    float key = _runKey[i];
                    int cell = _run[i];
                    int j = i;
                    while (j >= gap && _runKey[j - gap] > key)
                    {
                        _runKey[j] = _runKey[j - gap];
                        _run[j] = _run[j - gap];
                        j -= gap;
                    }
                    _runKey[j] = key;
                    _run[j] = cell;
                }
            }
        }
    }
}
