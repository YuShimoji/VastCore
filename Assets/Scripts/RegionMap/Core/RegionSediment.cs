using System;
using System.Threading.Tasks;

namespace Vastcore.RegionMap
{
    /// <summary>土砂の場（RegionSediment.Apply）で動いた土砂の総量（m3）。試験と確認用。</summary>
    public struct RegionSedimentBudget
    {
        /// <summary>崖から落ちて足もとに加わった岩屑。</summary>
        public double Supplied;

        /// <summary>水が削った量（ゆるい土砂と、床より上の岩屑の斜面の両方）。</summary>
        public double Eroded;

        /// <summary>水が運べなくなってためた量。</summary>
        public double Deposited;

        /// <summary>川・湖・海・窓の外へ出て、窓から消えた量。</summary>
        public double Outflow;
    }

    /// <summary>
    /// 土砂の場（docs/02_design/REGION_SURFACE_MATERIALS_SPEC.md 6.2 節）。台地（RegionDetailSpec.Mesa）を載せた拡大した地形に、
    /// 重力の崩れと水による運搬を短く流し、地形を変えて 4 つの地図（RegionData.Deposit・Wear・SedimentFlow・Scree）を作る。
    ///
    /// 1. 崖からの供給：帽子の岩の崖の足もとに岩屑を加える。量は崖の高さに比べ、帽子の上面の水が崖を越えて落ちる所
    ///    （割れ目や縁の凹みに水が集まる所）ほど多い。落ち口の下に錐ができ、落ち口の間は溝のように取り残される。
    /// 2. 崩れ（重力）：安息角（約 36 度）を超えた所から、ゆるい土砂だけを高い所から順に下へ移す。元の岩屑の斜面のでこぼこは崩さない。
    /// 3. 運搬（水）：既存の FlowRouter で流れ先と流量を決め、流量と傾きから運べる量を出す。運べる量に満たなければ削り
    ///    （台地を載せる前の標高より下は削らない）、超えれば超えた分をためる。削りとためは数 m の幅へならす（溝の幅と扇の広がり）。
    ///    斜面の裾で傾きが落ちる所に扇状地ができる。
    ///
    /// 帽子の岩・海・湖・水面が一部でもかかる川のセルは高さを変えない。水面のセルに入った土砂は川が運び去った扱いで消える。
    /// 量は m3 と m で決めるので、格子の細かさが違う窓でも同じ場所の形が近くなる（細かい窓ほど細い溝が出る）。
    /// 崩れは順番に、ならしは前の段の値だけから計算するので、結果は実行環境によらず同じ。
    /// </summary>
    public static class RegionSediment
    {
        /// <summary>ゆるい土砂の安息角の傾き（高さ / 水平距離。約 36 度）。台地の岩屑の斜面（約 34 度）より少し急。</summary>
        public const float ReposeTan = 0.72f;

        // 供給：凹んだ崖の長さ 1 m・崖の高さ 86 m あたりの量（m3。まっすぐな崖は 0.08 倍）と、崖を越える水の集まる面積（ha）の 0.85 乗あたりの量（m3）
        private const float EdgeSupply = 90f;
        private const float ChuteSupply = 4000f;
        private const float ChuteExponent = 0.85f;

        // 崖から落ちた岩屑を、供給する所に積む厚さの上限（m）
        private const float MaxScree = 12f;

        // 崖の凹みを測る四角の半径（m）
        private const float EnclosureMeters = 8f;

        // 供給を何回に分けて足すか（1 回ごとに崩れを流す）
        private const int SupplySteps = 3;

        // 運搬：流量（ha）の平方根 × 傾きの 1.5 乗 × 幅（m）に掛ける係数（1 回あたり m3）
        private const float Capacity = 50f;

        // 1 回の運搬で削る・ためる厚さの上限（m。ならす前）
        private const float MaxErode = 0.8f;
        private const float MaxDeposit = 0.6f;

        // 水が集める面積がこれ（ha）より小さい所は削らない（尾根と斜面の上はそのまま）
        private const float MinErodeHectares = 0.005f;

        // 運べる量を超えた分のうち、流れ 1 m あたりにためる割合を決める長さ（m）
        private const float SettleMeters = 18f;

        // 流れ先を決める前に地形をならす幅（m）
        private const float RouteSmoothMeters = 2.5f;

        // 削りとためをならす幅（m。標準偏差の目安）
        private const float SpreadMeters = 1.4f;

        private const int Passes = 8;

        // 地図の 0〜1 への直し方：1 - exp(-厚さ / この値)
        private const float DepositScale = 0.4f;
        private const float WearScale = 0.4f;
        private const float ScreeScale = 1.5f;
        // 土砂の通り道は、1 回の運搬で幅 1 m を通る量（m3）をセルの一辺（m）で割った値で測る（格子の細かさで値が変わりにくい）
        private const float FlowScale = 0.6f;

        // 崩れを止める、安息角からの超え方の許し（m）
        private const float SettleTolerance = 0.02f;

        // セルごとに安息角の傾きを最大この割合だけ緩くする（岩屑の面が格子の向きの平らな面にそろわないように）
        private const float ReposeJitter = 0.18f;

        private const byte Movable = 0;
        private const byte Sink = 1;
        private const byte Wall = 2;

        /// <summary>
        /// 土砂の場を流す。data は台地を載せた拡大した地形（Caprock を持つ）。floor は台地を載せる前の標高で、削れる深さの下限。
        /// amount は全体の強さ（1 が標準、0 なら何もしない）。gravity は崖からの供給と崩れ、water は水の運搬の強さ（どちらも 1 が中くらい）。
        /// 地形（data.Height）を書き換え、4 つの地図を data に持たせる。
        /// </summary>
        public static RegionSedimentBudget Apply(RegionData data, float[] floor, float amount, float gravity, float water, uint seed)
        {
            var budget = new RegionSedimentBudget();
            if (data == null) throw new ArgumentNullException("data");
            if (floor == null) throw new ArgumentNullException("floor");
            if (amount <= 0f || data.Caprock == null) return budget;
            if (gravity < 0f) gravity = 0f;
            if (water < 0f) water = 0f;

            int n = data.Resolution;
            int count = n * n;
            float cell = data.CellMeters;
            float cellArea = cell * cell;
            float[] h = data.Height;
            byte[] kind = new byte[count];
            var land = new bool[count];
            float[] coverage = data.WaterCoverage;
            for (int i = 0; i < count; i++)
            {
                land[i] = h[i] > 0f;
                if (!land[i] || data.LakeDepth[i] > 0f || (coverage != null && coverage[i] > 0f)) kind[i] = Sink;
                else if (data.Caprock[i] != 0) kind[i] = Wall;
            }

            var s = new State(n, cell, h, floor, kind, land);
            for (int i = 0; i < count; i++) s.Repose[i] = ReposeTan * (1f - ReposeJitter * RegionNoise.Hash01(i % n, i / n, seed + 409u));
            var router = new FlowRouter(n) { Jitter = 0.6f, JitterSeed = seed + 401u };
            var area = new float[count];

            // 1. 崖からの供給（m3）。帽子の上面の水がどこで崖を越えるかを、台地を載せた地形で流して決める
            float[] supply = gravity > 0f ? Supply(s, router, area, amount * gravity) : null;
            int supplySteps = supply != null ? SupplySteps : 0;
            float capacity = Capacity * amount * water;
            var avalanche = new Avalanche(count);
            // 崩れで増えた分を横へならす回数（1 セル幅の筋が格子の向きにまっすぐ伸びないように）
            int widen = (int)Math.Round(1.5f * (SpreadMeters / cell) * (SpreadMeters / cell));
            if (widen > 10) widen = 10;
            for (int pass = 0; pass < Passes; pass++)
            {
                if (supply != null)
                {
                    for (int step = 0; step < supplySteps; step++)
                    {
                        budget.Supplied += Inject(s, supply, 1f / (Passes * supplySteps));
                        budget.Outflow += avalanche.Run(s, widen);
                    }
                }
                if (capacity > 0f)
                {
                    Carry(s, router, area, capacity, ref budget);
                    // 削って急になったゆるい土砂の壁を崩す
                    budget.Outflow += avalanche.Run(s, widen);
                }
            }
            // 最後はならさずに崩し、安息角を超える所を残さない
            budget.Outflow += avalanche.Run(s, 0);

            var depositMap = new float[count];
            var wearMap = new float[count];
            var screeMap = new float[count];
            var flowMap = new float[count];
            for (int i = 0; i < count; i++)
            {
                if (kind[i] != Movable) continue;
                if (s.Deposit[i] > 0f) depositMap[i] = 1f - (float)Math.Exp(-s.Deposit[i] / DepositScale);
                if (s.Wear[i] > 0f) wearMap[i] = 1f - (float)Math.Exp(-s.Wear[i] / WearScale);
                if (s.Scree[i] > 0f) screeMap[i] = 1f - (float)Math.Exp(-s.Scree[i] / ScreeScale);
                if (s.Flux[i] > 0f) flowMap[i] = 1f - (float)Math.Exp(-s.Flux[i] / (FlowScale * cell));
            }
            data.Deposit = depositMap;
            data.Wear = wearMap;
            data.Scree = screeMap;
            data.SedimentFlow = flowMap;
            return budget;
        }

        /// <summary>計算中の配列。Scree（崖から落ちた岩屑）と Deposit（水がためた土砂）の和が、崩れで動けるゆるい土砂。</summary>
        private sealed class State
        {
            public readonly int N;
            public readonly float Cell;
            public readonly float[] H, Floor, Scree, Deposit, Wear, Flux, WallTop;
            // セルごとの安息角の傾き（ReposeTan 以下）
            public readonly float[] Repose;
            public readonly byte[] Kind;
            public readonly bool[] Land;

            public State(int n, float cell, float[] h, float[] floor, byte[] kind, bool[] land)
            {
                int count = n * n;
                N = n;
                Cell = cell;
                H = h;
                Floor = floor;
                Kind = kind;
                Land = land;
                Scree = new float[count];
                Deposit = new float[count];
                Wear = new float[count];
                Flux = new float[count];
                WallTop = new float[count];
                Repose = new float[count];
            }

            /// <summary>ゆるい土砂を thickness（m）だけ減らす。2 種類は今の割合で減る。</summary>
            public void RemoveLoose(int i, float thickness)
            {
                float loose = Scree[i] + Deposit[i];
                if (loose <= 0f) return;
                float share = Scree[i] / loose;
                Scree[i] -= thickness * share;
                Deposit[i] -= thickness * (1f - share);
                if (Scree[i] < 0f) Scree[i] = 0f;
                if (Deposit[i] < 0f) Deposit[i] = 0f;
            }
        }

        /// <summary>
        /// 崖の足もとのセルごとの供給の全量（m3）。崖の長さと高さに比べた一様な分と、帽子の上面の水が崖を越えて落ちる所の分。
        /// 帽子の岩に囲まれて出口の無い凹みには、帽子の上面の高さを超えて積まない（WallTop）。
        /// </summary>
        private static float[] Supply(State s, FlowRouter router, float[] area, float strength)
        {
            int n = s.N;
            int count = n * n;
            float[] h = s.H;
            byte[] kind = s.Kind;
            float cell = s.Cell;
            float hectaresPerCell = cell * cell / 10000f;
            router.Route(h, s.Land);
            router.Accumulate(null, s.Land, area);
            var spill = new float[count];
            for (int o = 0; o < router.Count; o++)
            {
                int c = router.Order[o];
                int r = router.Receiver[c];
                if (kind[c] != Wall || r < 0 || kind[r] != Movable) continue;
                spill[r] += area[c];
            }
            // 帽子の岩の数を足し上げた表（半径 EnclosureMeters の四角の中の数を O(1) で引く）
            int box = (int)Math.Round(EnclosureMeters / cell);
            if (box < 1) box = 1;
            var table = new int[(n + 1) * (n + 1)];
            for (int y = 0; y < n; y++)
            {
                int row = 0;
                for (int x = 0; x < n; x++)
                {
                    if (kind[x + y * n] == Wall) row++;
                    table[(x + 1) + (y + 1) * (n + 1)] = table[(x + 1) + y * (n + 1)] + row;
                }
            }
            var supply = new float[count];
            for (int y = 1; y < n - 1; y++)
            {
                for (int x = 1; x < n - 1; x++)
                {
                    int i = x + y * n;
                    if (kind[i] != Movable) continue;
                    float top = float.NegativeInfinity;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int j = i + dx + dy * n;
                            if (kind[j] == Wall && h[j] > top) top = h[j];
                        }
                    }
                    s.WallTop[i] = top > float.NegativeInfinity ? top : float.PositiveInfinity;
                    float drop = top - h[i];
                    if (drop < 3f && spill[i] <= 0f) continue;
                    float tall = drop / RegionDetailBuilder.MesaCapThickness;
                    if (tall < 0f) tall = 0f; else if (tall > 1f) tall = 1f;
                    float chute = spill[i] > 0f ? ChuteSupply * (float)Math.Pow(spill[i] * hectaresPerCell, ChuteExponent) : 0f;
                    // 崖の凹み（割れ目の口・入り江）ほど岩屑が集まり、張り出した所にはあまり残らない。
                    // 周りの四角の中で帽子の岩が占める割合で決める（まっすぐな崖の足もとで約 0.47）
                    int x0 = Math.Max(0, x - box), x1 = Math.Min(n, x + box + 1);
                    int y0 = Math.Max(0, y - box), y1 = Math.Min(n, y + box + 1);
                    int inside = table[x1 + y1 * (n + 1)] - table[x0 + y1 * (n + 1)] - table[x1 + y0 * (n + 1)] + table[x0 + y0 * (n + 1)];
                    float enclosed = inside / (float)((x1 - x0) * (y1 - y0));
                    float funnel = (enclosed - 0.45f) / 0.2f;
                    if (funnel < 0f) funnel = 0f; else if (funnel > 1f) funnel = 1f;
                    funnel = 0.08f + 0.92f * funnel * (float)Math.Sqrt(funnel);
                    supply[i] = strength * (EdgeSupply * cell * tall * funnel + chute);
                }
            }

            // 割れ目や縁の凹みの中に落ちた岩屑は、水の流れ先をたどって凹みの外（帽子の岩に接しないセル）まで出してから積む。
            // 割れ目は埋めず、錐は割れ目の出口の下にできる
            var placed = new float[count];
            int reach = (int)Math.Ceiling(40f / cell);
            for (int i = 0; i < count; i++)
            {
                if (supply[i] <= 0f) continue;
                int at = i;
                for (int step = 0; step < reach && s.WallTop[at] < float.PositiveInfinity; step++)
                {
                    int r = router.Receiver[at];
                    if (r < 0 || kind[r] != Movable) break;
                    at = r;
                }
                placed[at] += supply[i];
            }
            return placed;
        }

        /// <summary>
        /// 供給の fraction を足す。崖から落ちた岩屑が MaxScree（m）より厚い所と、帽子の上面より 1 m 下まで埋まった所には足さない
        /// （出口の無い入り江や割れ目を埋め尽くさない）。足した量（m3）を返す。
        /// </summary>
        private static double Inject(State s, float[] supply, float fraction)
        {
            float cellArea = s.Cell * s.Cell;
            double added = 0.0;
            for (int i = 0; i < supply.Length; i++)
            {
                float volume = supply[i];
                if (volume <= 0f) continue;
                float thickness = volume * fraction / cellArea;
                float room = s.WallTop[i] - 1f - s.H[i];
                float left = MaxScree - s.Scree[i];
                if (left < room) room = left;
                if (room <= 0f) continue;
                if (thickness > room) thickness = room;
                s.H[i] += thickness;
                s.Scree[i] += thickness;
                added += thickness * cellArea;
            }
            return added;
        }

        /// <summary>
        /// 崩れ。ゆるい土砂のあるセルを高い順に取り出し、安息角を超える隣へ、超えた高さの差に比べて分ける（最も急な隣が安息角に戻るまで）。
        /// 受けたセルと、下がったセルの上側の隣は取り出す列に入るので、1 回の実行で土砂は斜面の下まで届き、安息角を超える所は残らない。
        /// 帽子の岩へは移さず、水面のセルへ出た分は消える。
        /// 取り出す順は高さとセルの番号で決まるので、結果は毎回同じ。
        /// </summary>
        private sealed class Avalanche
        {
            private int[] _cell;
            private float[] _key;
            private int _size;
            private readonly bool[] _queued;
            private readonly int[] _neighbor = new int[8];
            private readonly float[] _excess = new float[8];

            private readonly float[] _scree, _deposit;

            public Avalanche(int count)
            {
                _scree = new float[count];
                _deposit = new float[count];
                _cell = new int[1024];
                _key = new float[1024];
                _queued = new bool[count];
            }

            public double Run(State s, int widen)
            {
                if (widen > 0)
                {
                    Array.Copy(s.Scree, _scree, _scree.Length);
                    Array.Copy(s.Deposit, _deposit, _deposit.Length);
                }
                double result = Settle(s);
                if (widen > 0) Widen(s, widen);
                return result;
            }

            /// <summary>
            /// 崩れで増えた分（2 種類それぞれ）だけを横へならす。減った分は動かさないので、総量は変わらない。
            /// </summary>
            private void Widen(State s, int passes)
            {
                float[] h = s.H;
                byte[] kind = s.Kind;
                int count = h.Length;
                var gainScree = new float[count];
                var gainDeposit = new float[count];
                for (int i = 0; i < count; i++)
                {
                    if (kind[i] != Movable) continue;
                    float a = s.Scree[i] - _scree[i];
                    float b = s.Deposit[i] - _deposit[i];
                    if (a > 0f) { gainScree[i] = a; s.Scree[i] -= a; h[i] -= a; }
                    if (b > 0f) { gainDeposit[i] = b; s.Deposit[i] -= b; h[i] -= b; }
                }
                Spread(gainScree, kind, s.N, passes);
                Spread(gainDeposit, kind, s.N, passes);
                for (int i = 0; i < count; i++)
                {
                    float a = gainScree[i], b = gainDeposit[i];
                    if (a == 0f && b == 0f) continue;
                    s.Scree[i] += a;
                    s.Deposit[i] += b;
                    h[i] += a + b;
                }
            }

            private double Settle(State s)
            {
                int n = s.N;
                float cell = s.Cell;
                float diagonal = cell * 1.41421356f;
                float[] h = s.H;
                byte[] kind = s.Kind;
                _size = 0;
                for (int i = 0; i < h.Length; i++)
                {
                    if (kind[i] == Movable && s.Scree[i] + s.Deposit[i] > 0f && Steep(s, i, cell, diagonal)) Push(i, h[i]);
                }
                double lost = 0.0;
                // 異常な入力で止まらないように、取り出す回数に上限を置く
                long budget = 64L * h.Length;
                while (_size > 0 && budget-- > 0)
                {
                    int i = Pop();
                    _queued[i] = false;
                    float loose = s.Scree[i] + s.Deposit[i];
                    if (loose <= 1e-5f) continue;
                    int x = i % n, y = i / n;
                    int found = 0;
                    float most = 0f, sum = 0f;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int ny = y + dy;
                        if (ny < 0 || ny >= n) continue;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx;
                            if ((dx == 0 && dy == 0) || nx < 0 || nx >= n) continue;
                            int j = nx + ny * n;
                            if (kind[j] == Wall) continue;
                            float excess = h[i] - h[j] - s.Repose[i] * (dx != 0 && dy != 0 ? diagonal : cell);
                            if (excess <= SettleTolerance) continue;
                            _neighbor[found] = j;
                            _excess[found] = excess;
                            found++;
                            sum += excess;
                            if (excess > most) most = excess;
                        }
                    }
                    if (found == 0) continue;
                    // 最も急な隣との差が安息角に戻る量（移した分は超えた差に比べて分けるので、隣も同じだけ上がる）
                    float move = most * sum / (sum + most);
                    if (move > loose) move = loose;
                    float share = s.Scree[i] / loose;
                    h[i] -= move;
                    s.RemoveLoose(i, move);
                    for (int k = 0; k < found; k++)
                    {
                        int j = _neighbor[k];
                        float part = move * _excess[k] / sum;
                        if (kind[j] == Sink)
                        {
                            lost += part;
                            continue;
                        }
                        h[j] += part;
                        s.Scree[j] += part * share;
                        s.Deposit[j] += part * (1f - share);
                        if (!_queued[j]) Push(j, h[j]);
                    }
                    // まだ急なら、もう一度取り出す
                    if (move < loose && move > 1e-4f && !_queued[i]) Push(i, h[i]);
                    // 下がったセルの上側の隣は急になっているかもしれないので、ゆるい土砂があれば取り出し直す
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int ny = y + dy;
                        if (ny < 0 || ny >= n) continue;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx;
                            if ((dx == 0 && dy == 0) || nx < 0 || nx >= n) continue;
                            int u = nx + ny * n;
                            if (_queued[u] || kind[u] != Movable || s.Scree[u] + s.Deposit[u] <= 0f) continue;
                            // 2 cm 未満の超え方は崩さない（細かい移しを繰り返さない）
                            if (h[u] - h[i] - s.Repose[u] * (dx != 0 && dy != 0 ? diagonal : cell) <= SettleTolerance) continue;
                            Push(u, h[u]);
                        }
                    }
                }
                while (_size > 0) _queued[Pop()] = false;
                return lost * cell * cell;
            }

            /// <summary>安息角を超える隣があるか。</summary>
            private static bool Steep(State s, int i, float cell, float diagonal)
            {
                int n = s.N;
                int x = i % n, y = i / n;
                float[] h = s.H;
                for (int dy = -1; dy <= 1; dy++)
                {
                    int ny = y + dy;
                    if (ny < 0 || ny >= n) continue;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx;
                        if ((dx == 0 && dy == 0) || nx < 0 || nx >= n) continue;
                        int j = nx + ny * n;
                        if (s.Kind[j] == Wall) continue;
                        if (h[i] - h[j] - s.Repose[i] * (dx != 0 && dy != 0 ? diagonal : cell) > SettleTolerance) return true;
                    }
                }
                return false;
            }

            private void Push(int cell, float key)
            {
                if (_size == _cell.Length)
                {
                    Array.Resize(ref _cell, _size * 2);
                    Array.Resize(ref _key, _size * 2);
                }
                _queued[cell] = true;
                int at = _size++;
                while (at > 0)
                {
                    int parent = (at - 1) >> 1;
                    if (!Before(cell, key, _cell[parent], _key[parent])) break;
                    _cell[at] = _cell[parent];
                    _key[at] = _key[parent];
                    at = parent;
                }
                _cell[at] = cell;
                _key[at] = key;
            }

            private int Pop()
            {
                int top = _cell[0];
                _size--;
                if (_size > 0)
                {
                    int cell = _cell[_size];
                    float key = _key[_size];
                    int at = 0;
                    while (true)
                    {
                        int child = at * 2 + 1;
                        if (child >= _size) break;
                        if (child + 1 < _size && Before(_cell[child + 1], _key[child + 1], _cell[child], _key[child])) child++;
                        if (!Before(_cell[child], _key[child], cell, key)) break;
                        _cell[at] = _cell[child];
                        _key[at] = _key[child];
                        at = child;
                    }
                    _cell[at] = cell;
                    _key[at] = key;
                }
                return top;
            }

            // 高い方が先。同じ高さなら番号の小さい方が先
            private static bool Before(int a, float keyA, int b, float keyB)
            {
                return keyA > keyB || (keyA == keyB && a < b);
            }
        }

        /// <summary>
        /// 水の運搬 1 回。流れ先を決め直し、上流から下流へ、運んでいる土砂と運べる量を比べて削るかためる量を決める。
        /// 決めた量は数 m の幅へならしてから地形に入れる（1 セル幅の溝と筋をなくし、扇を横へ広げる）。
        /// 削るのはゆるい土砂から先で、足りなければ床（台地を載せる前の標高）までの岩屑の斜面を削る（Wear）。
        /// </summary>
        private static void Carry(State s, FlowRouter router, float[] area, float capacity, ref RegionSedimentBudget budget)
        {
            int n = s.N;
            int count = n * n;
            float cell = s.Cell;
            float cellArea = cell * cell;
            float hectaresPerCell = cellArea / 10000f;
            float[] h = s.H;
            byte[] kind = s.Kind;
            bool[] land = s.Land;
            // 流れ先・流量・傾きは、数 m でならした地形で決める（岩屑の面の 1 m の粒で流れが途切れず、溝が斜面を通して続く）
            int smooth = (int)Math.Round(1.5f * (RouteSmoothMeters / cell) * (RouteSmoothMeters / cell));
            if (smooth > 12) smooth = 12;
            float[] g = smooth > 0 ? Blur(h, land, n, smooth) : h;
            router.Route(g, land);
            router.Accumulate(null, land, area);
            var inflow = new float[count];
            var change = new float[count];
            double outflow = 0.0;
            for (int o = router.Count - 1; o >= 0; o--)
            {
                int i = router.Order[o];
                float load = inflow[i];
                if (kind[i] == Sink)
                {
                    outflow += load;
                    continue;
                }
                int r = router.Receiver[i];
                if (kind[i] == Wall)
                {
                    if (r >= 0) inflow[r] += load; else outflow += load;
                    continue;
                }
                float below = r >= 0 ? (land[r] ? g[r] : 0f) : g[i];
                int diff = r - i;
                bool diagonal = r >= 0 && (diff == n + 1 || diff == n - 1 || diff == -n + 1 || diff == -n - 1);
                float run = diagonal ? cell * 1.41421356f : cell;
                float slope = (g[i] - below) / run;
                if (slope < 0f) slope = 0f;
                float hectares = area[i] * hectaresPerCell;
                float carry = capacity * cell * (float)Math.Sqrt(hectares) * slope * (float)Math.Sqrt(slope);

                if (load < carry)
                {
                    if (hectares >= MinErodeHectares)
                    {
                        float depth = (carry - load) / cellArea;
                        if (depth > MaxErode) depth = MaxErode;
                        // 流れ先より下げない（窪みを作らない）。床より下げない
                        float room = 0.5f * (g[i] - below);
                        if (depth > room) depth = room;
                        float above = h[i] - s.Floor[i];
                        if (depth > above) depth = above;
                        if (depth > 0f)
                        {
                            change[i] = -depth;
                            load += depth * cellArea;
                        }
                    }
                }
                else if (load > 0f)
                {
                    float settle = 1f - (float)Math.Exp(-run / SettleMeters);
                    float depth = (load - carry) * settle / cellArea;
                    if (depth > MaxDeposit) depth = MaxDeposit;
                    if (depth > 0f)
                    {
                        change[i] = depth;
                        load -= depth * cellArea;
                    }
                }

                float perWidth = load / cell;
                if (perWidth > s.Flux[i]) s.Flux[i] = perWidth;
                if (r >= 0) inflow[r] += load; else outflow += load;
            }

            int spread = (int)Math.Round(1.5f * (SpreadMeters / cell) * (SpreadMeters / cell));
            if (spread > 10) spread = 10;
            Spread(change, kind, n, spread);

            double eroded = 0.0, deposited = 0.0;
            for (int i = 0; i < count; i++)
            {
                float d = change[i];
                if (d == 0f || kind[i] != Movable) continue;
                if (d > 0f)
                {
                    h[i] += d;
                    s.Deposit[i] += d;
                    deposited += d;
                    continue;
                }
                float depth = -d;
                float above = h[i] - s.Floor[i];
                if (depth > above) depth = above;
                if (depth <= 0f) continue;
                float loose = s.Scree[i] + s.Deposit[i];
                float fromLoose = depth < loose ? depth : loose;
                s.RemoveLoose(i, fromLoose);
                s.Wear[i] += depth - fromLoose;
                h[i] -= depth;
                eroded += depth;
            }
            budget.Eroded += eroded * cellArea;
            budget.Deposited += deposited * cellArea;
            budget.Outflow += outflow;
        }

        /// <summary>陸のセルだけで 3×3 の平均を passes 回とった新しい配列。陸でないセルは元の値。</summary>
        private static float[] Blur(float[] source, bool[] land, int n, int passes)
        {
            int count = n * n;
            var a = (float[])source.Clone();
            var b = new float[count];
            for (int pass = 0; pass < passes; pass++)
            {
                float[] from = a, to = b;
                Parallel.For(0, n, y =>
                {
                    for (int x = 0; x < n; x++)
                    {
                        int i = x + y * n;
                        if (!land[i]) { to[i] = from[i]; continue; }
                        float sum = 0f;
                        int found = 0;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int ny = y + dy;
                            if (ny < 0 || ny >= n) continue;
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx;
                                if (nx < 0 || nx >= n) continue;
                                int j = nx + ny * n;
                                if (!land[j]) continue;
                                sum += from[j];
                                found++;
                            }
                        }
                        to[i] = sum / found;
                    }
                });
                a = to;
                b = from;
            }
            return a;
        }

        /// <summary>
        /// 値を、動かせるセルの間で 3×3 に分けることを passes 回くり返す（総量は変わらない）。
        /// 各セルは自分と動かせる隣へ等分に配る。並列でも、前の回の値だけから計算する。
        /// </summary>
        private static void Spread(float[] values, byte[] kind, int n, int passes)
        {
            if (passes <= 0) return;
            int count = n * n;
            var share = new float[count];
            var next = new float[count];
            for (int pass = 0; pass < passes; pass++)
            {
                Parallel.For(0, n, y =>
                {
                    for (int x = 0; x < n; x++)
                    {
                        int i = x + y * n;
                        if (kind[i] != Movable || values[i] == 0f) { share[i] = 0f; continue; }
                        int around = 0;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int ny = y + dy;
                            if (ny < 0 || ny >= n) continue;
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx;
                                if (nx < 0 || nx >= n) continue;
                                if (kind[nx + ny * n] == Movable) around++;
                            }
                        }
                        share[i] = values[i] / around;
                    }
                });
                Parallel.For(0, n, y =>
                {
                    for (int x = 0; x < n; x++)
                    {
                        int i = x + y * n;
                        if (kind[i] != Movable) { next[i] = values[i]; continue; }
                        float sum = 0f;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            int ny = y + dy;
                            if (ny < 0 || ny >= n) continue;
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                int nx = x + dx;
                                if (nx < 0 || nx >= n) continue;
                                sum += share[nx + ny * n];
                            }
                        }
                        next[i] = sum;
                    }
                });
                Array.Copy(next, values, count);
            }
        }
    }
}
