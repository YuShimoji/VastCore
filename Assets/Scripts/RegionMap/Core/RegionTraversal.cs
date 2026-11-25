using System;
using System.Collections.Generic;

namespace Vastcore.RegionMap
{
    /// <summary>歩いて通る時の地面の区分。</summary>
    public enum RegionGround : byte
    {
        /// <summary>海と湖。通れない。</summary>
        Water = 0,
        /// <summary>歩ける。</summary>
        Walk = 1,
        /// <summary>急な斜面。歩けるが遅い。</summary>
        Steep = 2,
        /// <summary>手を使って登る斜面。とても遅い。</summary>
        Scramble = 3,
        /// <summary>崖。通れない。</summary>
        Cliff = 4,
        /// <summary>川。渡れるが遅い。</summary>
        Ford = 5,
    }

    /// <summary>
    /// 通行の区分と道筋の計算の値（3D 化の試作、段階 C）。どれも地形を調べるための仮の値で、ゲームの規則ではない。
    /// 傾きは 高さ / 水平距離。
    /// </summary>
    [Serializable]
    public sealed class RegionTraversalSpec
    {
        /// <summary>これより急なセルは「急な斜面」（約 20 度）。</summary>
        public float WalkSlope = 0.36f;

        /// <summary>これより急なセルは「手を使って登る斜面」（約 35 度）。</summary>
        public float SteepSlope = 0.70f;

        /// <summary>これより急なセルは「崖」で、通れない（約 50 度）。隣のセルへ進む時の傾きもこれを超えると通れない。</summary>
        public float CliffSlope = 1.19f;

        /// <summary>かかる時間の倍率（急な斜面・手を使って登る斜面・川）。</summary>
        public float SteepFactor = 1.6f;
        public float ScrambleFactor = 4f;
        public float FordFactor = 3f;

        /// <summary>道の無い所を歩く時の速さの倍率（Tobler 1993 の値）。</summary>
        public float OffPathFactor = 0.6f;

        public RegionTraversalSpec Clone()
        {
            return (RegionTraversalSpec)MemberwiseClone();
        }
    }

    /// <summary>ある場所から別の場所までの道筋。</summary>
    public sealed class RegionRoute
    {
        /// <summary>通るセル（出発から到着の順）。</summary>
        public int[] Cells;

        public float Hours;
        public float LengthMeters;
        public float StraightMeters;
        public float AscentMeters;
        public float DescentMeters;

        /// <summary>道のり ÷ 直線距離。1 に近いほどまっすぐ行ける。</summary>
        public float Detour
        {
            get { return StraightMeters > 0f ? LengthMeters / StraightMeters : 1f; }
        }
    }

    /// <summary>1 か所から歩いて行ける範囲。Hours は着くまでの時間（着けないセルは無限大）。</summary>
    public sealed class RegionTravelField
    {
        public RegionTraversal Traversal;
        public int Source;
        public float[] Hours;

        /// <summary>そのセルへ来る 1 つ前のセル（出発点と着けないセルは -1）。</summary>
        public int[] From;

        /// <summary>着けたセルの数（出発点を含む）。</summary>
        public int Reached;

        public bool CanReach(int cell)
        {
            return !float.IsPositiveInfinity(Hours[cell]);
        }

        /// <summary>出発点からそのセルまでの道筋。着けなければ null。</summary>
        public RegionRoute RouteTo(int cell)
        {
            if (!CanReach(cell)) return null;
            var cells = new List<int>();
            for (int c = cell; c >= 0; c = From[c]) cells.Add(c);
            cells.Reverse();

            RegionData data = Traversal.Data;
            int n = data.Resolution;
            float cellMeters = data.CellMeters;
            var route = new RegionRoute { Cells = cells.ToArray(), Hours = Hours[cell] };
            for (int k = 1; k < cells.Count; k++)
            {
                int a = cells[k - 1];
                int b = cells[k];
                bool diagonal = a % n != b % n && a / n != b / n;
                route.LengthMeters += cellMeters * (diagonal ? 1.4142135f : 1f);
                float climb = data.Height[b] - data.Height[a];
                if (climb > 0f) route.AscentMeters += climb; else route.DescentMeters -= climb;
            }
            float dx = (cell % n - Source % n) * cellMeters;
            float dy = (cell / n - Source / n) * cellMeters;
            route.StraightMeters = (float)Math.Sqrt(dx * dx + dy * dy);
            return route;
        }
    }

    /// <summary>
    /// 地形を歩いて通る時の区分と、最も早く着く道筋（3D 化の試作、段階 C）。広域の地域にも拡大した地形にも使える。
    /// 区分はセルの傾きで決める。歩く速さは Tobler のハイキング関数（速さ km/h = 6·exp(-3.5·|進む向きの傾き + 0.05|)）に、
    /// 道の無い所の倍率と区分ごとの倍率を掛ける。上りと下りで速さが違うので、行きと帰りで道筋が変わることがある。
    /// 崖と水のセル、進む向きの傾きが崖の値を超える隣どうしは通れない。
    /// </summary>
    public sealed class RegionTraversal
    {
        public RegionData Data;
        public RegionTraversalSpec Spec;

        /// <summary>セルごとの区分。</summary>
        public RegionGround[] Ground;

        /// <summary>区分ごとのセルの数（添字は RegionGround）。</summary>
        public readonly int[] Counts = new int[6];

        private static readonly int[] StepX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] StepY = { 0, 0, 1, -1, 1, -1, 1, -1 };

        public static RegionTraversal Build(RegionData data, RegionTraversalSpec spec)
        {
            if (data == null) throw new ArgumentNullException("data");
            spec = spec != null ? spec.Clone() : new RegionTraversalSpec();
            int n = data.Resolution;
            float cell = data.CellMeters;
            var traversal = new RegionTraversal { Data = data, Spec = spec, Ground = new RegionGround[n * n] };
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = x + y * n;
                    RegionGround ground;
                    if (data.Height[i] <= 0f || data.LakeDepth[i] > 0f)
                    {
                        ground = RegionGround.Water;
                    }
                    else if (data.WaterCoverage != null ? data.WaterCoverage[i] > 0.5f : data.Biome[i] == RegionBiome.River)
                    {
                        ground = RegionGround.Ford;
                    }
                    else
                    {
                        float slope = RegionGenerator.SlopeAt(data.Height, n, x, y, cell);
                        ground = slope > spec.CliffSlope ? RegionGround.Cliff
                            : (slope > spec.SteepSlope ? RegionGround.Scramble
                            : (slope > spec.WalkSlope ? RegionGround.Steep : RegionGround.Walk));
                    }
                    traversal.Ground[i] = ground;
                    traversal.Counts[(int)ground]++;
                }
            }
            return traversal;
        }

        /// <summary>陸のセル（水以外）のうち、その区分が占める割合。</summary>
        public float LandShare(RegionGround ground)
        {
            int land = Ground.Length - Counts[(int)RegionGround.Water];
            return land > 0 ? Counts[(int)ground] / (float)land : 0f;
        }

        public bool CanStand(int cell)
        {
            RegionGround ground = Ground[cell];
            return ground != RegionGround.Water && ground != RegionGround.Cliff;
        }

        /// <summary>その場所にいちばん近い、立てるセル。reach セル以内に無ければ -1。</summary>
        public int NearestStandable(int cell, int reach)
        {
            if (CanStand(cell)) return cell;
            int n = Data.Resolution;
            int cx = cell % n;
            int cy = cell / n;
            int best = -1;
            int bestDistance = int.MaxValue;
            for (int dy = -reach; dy <= reach; dy++)
            {
                for (int dx = -reach; dx <= reach; dx++)
                {
                    int x = cx + dx;
                    int y = cy + dy;
                    if (x < 0 || y < 0 || x >= n || y >= n) continue;
                    int distance = dx * dx + dy * dy;
                    if (distance >= bestDistance || !CanStand(x + y * n)) continue;
                    bestDistance = distance;
                    best = x + y * n;
                }
            }
            return best;
        }

        private float Factor(int cell)
        {
            switch (Ground[cell])
            {
                case RegionGround.Steep: return Spec.SteepFactor;
                case RegionGround.Scramble: return Spec.ScrambleFactor;
                case RegionGround.Ford: return Spec.FordFactor;
                default: return 1f;
            }
        }

        /// <summary>
        /// source から歩いて、各セルへ最も早く着く時間と道筋を求める（ダイクストラ法、8 方向）。
        /// source が立てないセルなら、どこへも着けない。
        /// </summary>
        public RegionTravelField Travel(int source)
        {
            int n = Data.Resolution;
            int count = n * n;
            float[] height = Data.Height;
            float cell = Data.CellMeters;
            var hours = new float[count];
            var from = new int[count];
            for (int i = 0; i < count; i++)
            {
                hours[i] = float.PositiveInfinity;
                from[i] = -1;
            }
            var field = new RegionTravelField { Traversal = this, Source = source, Hours = hours, From = from };
            if (source < 0 || source >= count || !CanStand(source)) return field;

            var done = new bool[count];
            var heapKey = new float[1024];
            var heapCell = new int[1024];
            int heapSize = 0;
            hours[source] = 0f;
            Push(ref heapKey, ref heapCell, ref heapSize, 0f, source);
            float cliff = Spec.CliffSlope;
            float offPath = Spec.OffPathFactor;

            while (heapSize > 0)
            {
                float key = heapKey[0];
                int a = heapCell[0];
                Pop(heapKey, heapCell, ref heapSize);
                if (done[a] || key > hours[a]) continue;
                done[a] = true;
                field.Reached++;
                int ax = a % n;
                int ay = a / n;
                float factorA = Factor(a);
                for (int k = 0; k < 8; k++)
                {
                    int bx = ax + StepX[k];
                    int by = ay + StepY[k];
                    if (bx < 0 || by < 0 || bx >= n || by >= n) continue;
                    int b = bx + by * n;
                    if (done[b] || !CanStand(b)) continue;
                    float distance = k < 4 ? cell : cell * 1.4142135f;
                    float slope = (height[b] - height[a]) / distance;
                    if (slope > cliff || slope < -cliff) continue;
                    float speed = 6f * (float)Math.Exp(-3.5 * Math.Abs(slope + 0.05f)) * offPath;
                    float next = key + distance / 1000f / speed * 0.5f * (factorA + Factor(b));
                    if (next >= hours[b]) continue;
                    hours[b] = next;
                    from[b] = a;
                    Push(ref heapKey, ref heapCell, ref heapSize, next, b);
                }
            }
            return field;
        }

        private static void Push(ref float[] keys, ref int[] cells, ref int size, float key, int cell)
        {
            if (size == keys.Length)
            {
                Array.Resize(ref keys, size * 2);
                Array.Resize(ref cells, size * 2);
            }
            int i = size++;
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (keys[parent] <= key) break;
                keys[i] = keys[parent];
                cells[i] = cells[parent];
                i = parent;
            }
            keys[i] = key;
            cells[i] = cell;
        }

        private static void Pop(float[] keys, int[] cells, ref int size)
        {
            size--;
            if (size == 0) return;
            float key = keys[size];
            int cell = cells[size];
            int i = 0;
            while (true)
            {
                int child = i * 2 + 1;
                if (child >= size) break;
                if (child + 1 < size && keys[child + 1] < keys[child]) child++;
                if (keys[child] >= key) break;
                keys[i] = keys[child];
                cells[i] = cells[child];
                i = child;
            }
            keys[i] = key;
            cells[i] = cell;
        }
    }
}
