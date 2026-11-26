using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Vastcore.RegionMap
{
    /// <summary>土砂の場（RegionSediment.Apply）で動いた土砂の総量（m3）。試験と確認用。</summary>
    public struct RegionSedimentBudget
    {
        /// <summary>崖から落ちて足もとに加わった岩屑。</summary>
        public double Supplied;

        /// <summary>水と岩屑の溝が削った量（ゆるい土砂と、床より上の岩屑の斜面の両方）。溝が削った分は溝の下の錐へ積み直す。</summary>
        public double Eroded;

        /// <summary>水が運べなくなってためた量。</summary>
        public double Deposited;

        /// <summary>川・湖・海・窓の外へ出て、窓から消えた量。</summary>
        public double Outflow;
    }

    /// <summary>
    /// 土砂の場（docs/02_design/REGION_SURFACE_MATERIALS_SPEC.md 6.2 節）。台地（RegionDetailSpec.Mesa）を載せた拡大した地形に、
    /// 重力の崩れと水による運搬を短く流し、地形を変えて 5 つの地図（RegionData.Deposit・Wear・SedimentFlow・Scree・Coarse）を作る。
    ///
    /// 1. 崖からの供給：帽子の岩の崖の足もとに岩屑を加える。量は崖の高さに比べ、帽子の上面の水が崖を越えて落ちる所
    ///    （割れ目や縁の凹みに水が集まる所）ほど多い。落ち口の下に錐ができ、落ち口の間は溝のように取り残される。
    ///    落ち口に多く集まる分は、岩屑の溝（chute）を刻みながら斜面を下り、傾きが緩む所で錐になる。
    ///    岩屑が動いた距離から、大きい岩の割合（分級。斜面の裾とその先で大きい）を Coarse に出す。
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

        // 水が集める面積がこれ（ha）より小さい所は削らず、その 2 倍で削りきる（流れの集まる筋だけを削り、斜面に斑点を作らない）
        private const float MinErodeHectares = 0.03f;

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

        // 岩屑の溝（chute）：落ち口に集まる供給（m3）がこれを超える所から、超えた分の一部（最大 ChuteShare）を溝を通して下の錐へ運ぶ
        private const float ChuteMinVolume = 250f;
        private const float ChuteShare = 0.8f;
        // 溝の深さ（m。落ち口の供給 1000 m3 で。0.4〜1.6 倍）。傾きが ChuteStopTan より急な所を刻み、ChuteEndTan まで緩んだ所を錐の頂にする
        private const float ChuteDepth = 4f;
        private const float ChuteStopTan = 0.4f;
        private const float ChuteEndTan = 0.25f;
        // 溝の長さの下限と上限（m）。下限より短い溝（割れ目の中で行き止まる所）は作らず、その分は落ち口に積む
        private const float ChuteMinMeters = 12f;
        private const float ChuteMaxMeters = 400f;
        // 溝の傾きをとる長さ（m）と、溝の V 字の壁の傾き（崩れで埋まらないように安息角よりかなり緩い）
        private const float ChuteSlopeMeters = 4f;
        private const float ChuteWallTan = 0.35f;
        // 溝の半幅の上限（溝の長さに対する割合）。短い溝を深く刻むと、丸いすり鉢（seed 8 の窪地の Wear の塊）になるので、細長い溝に保つ
        private const float ChuteAspect = 0.15f;
        // 溝の両脇の土手の高さ（溝の深さに対する割合）。土手は溝が削った土砂で作る
        private const float LeveeShare = 0.35f;
        // 溝の下の錐の面の傾き（高さ / 水平距離。安息角より緩い円錐を、溝が運んだ量の合計に合わせて描く）
        private const float ConeTan = 0.2f;
        // 錐の縁で厚さを 0 へ滑らかにつなぐ幅（厚さ m）。縁で傾きが折れると、回ごとの錐の縁が丸い輪の跡に見える
        private const float ConeToe = 0.6f;

        // 水の運搬で運べる量を決める傾きは、この幅（m）でならした地形でとる（ためた土砂が上流の傾きを変えて、等高線のような波を作らない）
        private const float SlopeSmoothMeters = 6f;
        // ためた土砂をならす幅（m）。削りより広くして、扇を横へ広げる
        private const float DepositSpreadMeters = 2.4f;

        // 分級：岩屑が崖の足もとから動いた距離（m）がこの長さで、大きい岩の割合が 0.5
        private const float CoarseMeters = 20f;
        // 岩屑の斜面の裾の先へ、大きい岩が転がり出る距離（m）
        private const float RunoutMeters = 25f;

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
            // 落ち口に多く集まる供給の一部を、岩屑の溝を通して運ぶ分に分ける
            float[] chute = null, stay = null, moved = null;
            float[] cone = null, coneTravel = null;
            if (supply != null)
            {
                chute = new float[count];
                stay = new float[count];
                moved = new float[count];
                cone = new float[count];
                coneTravel = new float[count];
                for (int i = 0; i < count; i++)
                {
                    float volume = supply[i];
                    if (volume <= ChuteMinVolume) continue;
                    float share = (volume - ChuteMinVolume) / (2f * ChuteMinVolume);
                    if (share > 1f) share = 1f;
                    chute[i] = volume * ChuteShare * share;
                    supply[i] = volume - chute[i];
                }
            }
            float capacity = Capacity * amount * water;
            var avalanche = new Avalanche(count);
            // 崩れで増えた分を横へならす回数（1 セル幅の筋が格子の向きにまっすぐ伸びないように）
            int widen = (int)Math.Round(1.5f * (SpreadMeters / cell) * (SpreadMeters / cell));
            if (widen > 10) widen = 10;
            for (int pass = 0; pass < Passes; pass++)
            {
                if (supply != null)
                {
                    // 溝が運ぶ分を錐へ（溝はまだ刻まない。刻んでも次の岩屑が埋めるので、最後に刻む）
                    Chutes(s, router, chute, 1f / Passes, 0f, moved, stay, cone, coneTravel);
                    for (int step = 0; step < supplySteps; step++)
                    {
                        budget.Supplied += Inject(s, supply, 1f / (Passes * supplySteps));
                        budget.Supplied += Inject(s, stay, 1f / supplySteps);
                        budget.Supplied += InjectCone(s, cone, coneTravel, 1f / supplySteps);
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
            // 岩屑の溝を最後の地形に刻み、削った土砂を両脇の土手と溝の下の錐へ積む
            if (supply != null)
            {
                double carved = Chutes(s, router, chute, 0f, 1f, moved, stay, cone, coneTravel);
                budget.Eroded += carved;
                // 錐に積んだ量のうち、溝が削った分は移しただけなので供給に数えない
                budget.Supplied += InjectCone(s, cone, coneTravel, 1f) - carved;
            }
            // 最後はならさずに崩し、安息角を超える所を残さない
            budget.Outflow += avalanche.Run(s, 0);

            var depositMap = new float[count];
            var wearMap = new float[count];
            var screeMap = new float[count];
            var flowMap = new float[count];
            float[] coarseMap = Coarse(s, router);
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
            data.Coarse = coarseMap;
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
            // 崖から落ちた岩屑の厚さ × 崖の足もとから動いた距離（m2）。Travel / Scree が動いた距離の平均（分級に使う）
            public readonly float[] Travel;
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
                Travel = new float[count];
            }

            /// <summary>ゆるい土砂を thickness（m）だけ減らす。2 種類は今の割合で減る。</summary>
            public void RemoveLoose(int i, float thickness)
            {
                float loose = Scree[i] + Deposit[i];
                if (loose <= 0f) return;
                float share = Scree[i] / loose;
                float before = Scree[i];
                Scree[i] -= thickness * share;
                Deposit[i] -= thickness * (1f - share);
                if (Scree[i] < 0f) Scree[i] = 0f;
                if (Deposit[i] < 0f) Deposit[i] = 0f;
                // 残った岩屑の動いた距離の平均は変わらない
                Travel[i] = before > 0f ? Travel[i] * (Scree[i] / before) : 0f;
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
            private readonly float[] _step = new float[8];

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
                var gainTravel = new float[count];
                for (int i = 0; i < count; i++)
                {
                    if (kind[i] != Movable) continue;
                    float a = s.Scree[i] - _scree[i];
                    float b = s.Deposit[i] - _deposit[i];
                    if (a > 0f)
                    {
                        float t = s.Travel[i] * (a / s.Scree[i]);
                        gainScree[i] = a; s.Scree[i] -= a; h[i] -= a;
                        gainTravel[i] = t; s.Travel[i] -= t;
                    }
                    if (b > 0f) { gainDeposit[i] = b; s.Deposit[i] -= b; h[i] -= b; }
                }
                Spread(gainScree, kind, s.N, passes);
                Spread(gainDeposit, kind, s.N, passes);
                Spread(gainTravel, kind, s.N, passes);
                for (int i = 0; i < count; i++)
                {
                    float a = gainScree[i], b = gainDeposit[i];
                    if (a == 0f && b == 0f) continue;
                    s.Scree[i] += a;
                    s.Deposit[i] += b;
                    s.Travel[i] += gainTravel[i];
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
                            _step[found] = dx != 0 && dy != 0 ? diagonal : cell;
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
                    float travelled = s.Scree[i] > 0f ? s.Travel[i] / s.Scree[i] : 0f;
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
                        s.Travel[j] += part * share * (travelled + _step[k]);
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
            // 運べる量を決める傾きは、さらに広くならした地形でとる
            int reach = (int)Math.Round(SlopeSmoothMeters * 0.5f / cell);
            float[] gs = reach > 0 ? BoxBlur(BoxBlur(g, land, n, reach), land, n, reach) : g;
            router.Route(g, land);
            router.Accumulate(null, land, area);
            var inflow = new float[count];
            var change = new float[count];
            var settled = new float[count];
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
                float belowSmooth = r >= 0 ? (land[r] ? gs[r] : 0f) : gs[i];
                float slope = (gs[i] - belowSmooth) / run;
                if (slope < 0f) slope = 0f;
                float hectares = area[i] * hectaresPerCell;
                float carry = capacity * cell * (float)Math.Sqrt(hectares) * slope * (float)Math.Sqrt(slope);

                if (load < carry)
                {
                    if (hectares > MinErodeHectares)
                    {
                        float gather = (hectares - MinErodeHectares) / MinErodeHectares;
                        if (gather > 1f) gather = 1f;
                        float depth = gather * (carry - load) / cellArea;
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
                        settled[i] = depth;
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
            int widen = (int)Math.Round(1.5f * (DepositSpreadMeters / cell) * (DepositSpreadMeters / cell));
            if (widen > 12) widen = 12;
            Spread(settled, kind, n, widen);
            for (int i = 0; i < count; i++) change[i] += settled[i];

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

        /// <summary>
        /// 岩屑の溝。落ち口ごとに、ならした地形の流れ先をたどって傾きが ChuteEndTan に緩む所まで下る。
        /// fraction はこの回に溝が運ぶ供給の割合。carve が 0 より大きければ、急な所に V 字の溝（深さ ChuteDepth × carve）を刻み、両脇に土手を置く。
        /// 溝の終わり（錐の頂）には、その溝が運んだ量の合計（moved、落ち口ごとに積み上げる）に合う緩い円錐との差を、cone（厚さ m）として渡す。
        /// 土手も cone に入る。coneTravel は cone の厚さに動いた距離を掛けた値。短い溝や崖のそばで終わる溝の分は stay（落ち口の量）に回す。
        /// 刻んだ量（m3）を返す。
        /// </summary>
        private static double Chutes(State s, FlowRouter router, float[] chute, float fraction, float carve, float[] moved, float[] stay, float[] cone, float[] coneTravel)
        {
            int n = s.N;
            int count = n * n;
            float cell = s.Cell;
            float cellArea = cell * cell;
            float[] h = s.H;
            byte[] kind = s.Kind;
            bool[] land = s.Land;
            Array.Clear(cone, 0, count);
            Array.Clear(coneTravel, 0, count);
            Array.Clear(stay, 0, count);
            int smooth = (int)Math.Round(1.5f * (RouteSmoothMeters / cell) * (RouteSmoothMeters / cell));
            if (smooth > 12) smooth = 12;
            float[] g = smooth > 0 ? Blur(h, land, n, smooth) : h;
            router.Route(g, land);

            var cut = new float[count];
            var levee = new float[count];
            var leveeTravel = new float[count];
            var path = new List<int>();
            var along = new List<float>();
            var starts = new List<int>();
            var sources = new List<int>();
            var ends = new List<int>();
            var volumes = new List<float>();
            var lengths = new List<float>();
            var wants = new List<float>();
            int window = Math.Max(1, (int)Math.Round(ChuteSlopeMeters / cell));
            const float taperMeters = 6f;
            for (int i = 0; i < count; i++)
            {
                if (chute[i] <= 0f) continue;
                float scale = (float)Math.Sqrt(chute[i] / 1000f);
                if (scale < 0.4f) scale = 0.4f; else if (scale > 1.6f) scale = 1.6f;
                int first = path.Count;
                path.Add(i);
                along.Add(0f);
                int at = i;
                float length = 0f;
                while (true)
                {
                    int r = router.Receiver[at];
                    if (r < 0 || kind[r] != Movable) break;
                    int diff = r - at;
                    bool diagonal = diff == n + 1 || diff == n - 1 || diff == -n + 1 || diff == -n - 1;
                    float next = length + (diagonal ? cell * 1.41421356f : cell);
                    int k = path.Count - first;
                    int back = k - window < 0 ? 0 : k - window;
                    float slope = (g[path[first + back]] - g[r]) / (next - along[first + back]);
                    if (next > ChuteMaxMeters || (next > taperMeters && slope < ChuteEndTan)) break;
                    length = next;
                    path.Add(r);
                    along.Add(length);
                    at = r;
                }
                int last = path.Count - 1;
                if (along[last] < ChuteMinMeters || s.WallTop[path[last]] < float.PositiveInfinity)
                {
                    stay[i] += chute[i] * fraction;
                    path.RemoveRange(first, path.Count - first);
                    along.RemoveRange(first, along.Count - first);
                    continue;
                }
                float want = 0f;
                for (int k = first; k <= last; k++)
                {
                    int c = path[k];
                    // 溝は落ち口から数 m で深くなり、錐の頂に向けて浅くなる
                    float t0 = along[k] / taperMeters;
                    if (t0 > 1f) t0 = 1f;
                    int back = k - window < first ? first : k - window;
                    int ahead = k + window > last ? last : k + window;
                    float slope = ahead > back ? (g[path[back]] - g[path[ahead]]) / (along[ahead] - along[back]) : 0f;
                    float t1 = (slope - ChuteStopTan) / 0.2f;
                    if (t1 < 0f) t1 = 0f; else if (t1 > 1f) t1 = 1f;
                    float depth = ChuteDepth * scale * carve * t0 * t1;
                    if (depth <= 0f) continue;
                    // V 字の断面（壁の傾き ChuteWallTan）と、その外側の土手
                    // 溝の頭と終わりでは、そこまでの長さより幅を広げない（端に丸い窪みを作らない）
                    float half = depth / ChuteWallTan;
                    float room = Math.Min(Math.Min(along[k], along[last] - along[k]), ChuteAspect * along[last]);
                    if (half > room) half = room;
                    if (half < cell) half = cell;
                    if (depth > half * ChuteWallTan) depth = half * ChuteWallTan;
                    int reach = (int)Math.Ceiling(1.7f * half / cell);
                    int cx = c % n, cy = c / n;
                    for (int y = Math.Max(0, cy - reach); y <= Math.Min(n - 1, cy + reach); y++)
                    {
                        for (int x = Math.Max(0, cx - reach); x <= Math.Min(n - 1, cx + reach); x++)
                        {
                            int q = x + y * n;
                            if (kind[q] != Movable) continue;
                            float distance = cell * (float)Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                            if (distance < half)
                            {
                                float d = depth * (1f - distance / half);
                                if (d > cut[q]) { want += (d - cut[q]) * cellArea; cut[q] = d; }
                            }
                            else
                            {
                                float bank = LeveeShare * depth * (1f - Math.Abs(distance - 1.35f * half) / (0.35f * half));
                                if (bank > levee[q]) { levee[q] = bank; leveeTravel[q] = bank * along[k]; }
                            }
                        }
                    }
                }
                starts.Add(first);
                sources.Add(i);
                ends.Add(path[last]);
                volumes.Add(chute[i] * fraction);
                lengths.Add(along[last]);
                wants.Add(want);
            }
            if (ends.Count == 0) return 0.0;

            // 溝の中には土手を置かない
            for (int i = 0; i < count; i++) if (cut[i] > 0f) { levee[i] = 0f; leveeTravel[i] = 0f; }
            double carved = 0.0;
            for (int i = 0; i < count; i++)
            {
                float depth = cut[i];
                if (depth <= 0f || kind[i] != Movable) continue;
                float above = h[i] - s.Floor[i];
                if (depth > above) depth = above;
                if (depth <= 0f) continue;
                float loose = s.Scree[i] + s.Deposit[i];
                float fromLoose = depth < loose ? depth : loose;
                s.RemoveLoose(i, fromLoose);
                s.Wear[i] += depth - fromLoose;
                h[i] -= depth;
                carved += depth;
            }
            carved *= cellArea;
            // 土手は削った量から作り、残りを錐へ運ぶ
            double banked = 0.0;
            for (int i = 0; i < count; i++) banked += levee[i];
            banked *= cellArea;
            float bankScale = banked > carved ? (float)(carved / banked) : 1f;
            for (int i = 0; i < count; i++)
            {
                if (levee[i] <= 0f) continue;
                cone[i] += levee[i] * bankScale;
                coneTravel[i] += leveeTravel[i] * bankScale;
            }
            double toCones = carved - banked * bankScale;
            double wanted = 0.0;
            for (int k = 0; k < wants.Count; k++) wanted += wants[k];
            for (int k = 0; k < ends.Count; k++)
            {
                float volume = volumes[k] + (wanted > 0.0 ? (float)(toCones * wants[k] / wanted) : 0f);
                int source = sources[k];
                float before = moved[source];
                moved[source] = before + volume;
                // 溝の通り道（1 回に幅 1 m を通る量）
                int stop = k + 1 < starts.Count ? starts[k + 1] : path.Count;
                float perWidth = volume / cell;
                for (int p = starts[k]; p < stop; p++)
                {
                    int c = path[p];
                    if (perWidth > s.Flux[c]) s.Flux[c] = perWidth;
                }
                // 溝の終わりを頂にした円錐（体積 V の頂の高さは (3 V tan^2 / pi)^(1/3)）の、運んだ量が増えた分
                float top0 = (float)Math.Pow(3.0 * before * ConeTan * ConeTan / Math.PI, 1.0 / 3.0);
                float top1 = (float)Math.Pow(3.0 * moved[source] * ConeTan * ConeTan / Math.PI, 1.0 / 3.0);
                int end = ends[k];
                int ex = end % n, ey = end / n;
                int radius = (int)Math.Ceiling((top1 + ConeToe) / ConeTan / cell);
                for (int y = Math.Max(0, ey - radius); y <= Math.Min(n - 1, ey + radius); y++)
                {
                    for (int x = Math.Max(0, ex - radius); x <= Math.Min(n - 1, ex + radius); x++)
                    {
                        int c = x + y * n;
                        if (kind[c] != Movable) continue;
                        float distance = cell * (float)Math.Sqrt((x - ex) * (x - ex) + (y - ey) * (y - ey));
                        float t1 = Toe(top1 - ConeTan * distance);
                        if (t1 <= 0f) continue;
                        float add = before > 0f ? t1 - Toe(top0 - ConeTan * distance) : t1;
                        cone[c] += add;
                        coneTravel[c] += add * (lengths[k] + distance);
                    }
                }
            }
            return carved;
        }

        /// <summary>
        /// 円錐の厚さ x（m。縁の外で負）を、縁の前後 ConeToe で 0 へ滑らかにつないだ値（傾きが折れない）。
        /// 回ごとに増えた分は Toe(新しい厚さ) − Toe(前の厚さ) なので、足し合わせると最後の錐の形になる。
        /// </summary>
        private static float Toe(float x)
        {
            if (x >= ConeToe) return x;
            if (x <= -ConeToe) return 0f;
            float t = x + ConeToe;
            return t * t / (4f * ConeToe);
        }

        /// <summary>溝の下の錐へ、cone（厚さ m）の fraction を岩屑として足す。帽子の上面より 1 m 下までで止める。足した量（m3）を返す。</summary>
        private static double InjectCone(State s, float[] cone, float[] coneTravel, float fraction)
        {
            float cellArea = s.Cell * s.Cell;
            double added = 0.0;
            for (int i = 0; i < cone.Length; i++)
            {
                float thickness = cone[i] * fraction;
                if (thickness <= 0f || s.Kind[i] != Movable) continue;
                float room = s.WallTop[i] - 1f - s.H[i];
                float keep = 1f;
                if (thickness > room)
                {
                    if (room <= 0f) continue;
                    keep = room / thickness;
                    thickness = room;
                }
                s.H[i] += thickness;
                s.Scree[i] += thickness;
                s.Travel[i] += coneTravel[i] * fraction * keep;
                added += thickness * cellArea;
            }
            return added;
        }

        /// <summary>
        /// 大きい岩の割合（0〜1）。岩屑が崖の足もとから動いた距離の平均で決め（落ち口の下で小さく、斜面の裾で大きい）、
        /// 岩屑の斜面の裾から流れ先へ RunoutMeters まで、裾の値から薄れる転がり出しを足す。
        /// </summary>
        private static float[] Coarse(State s, FlowRouter router)
        {
            int n = s.N;
            int count = n * n;
            float cell = s.Cell;
            byte[] kind = s.Kind;
            const float thin = 0.05f;
            var coarse = new float[count];
            for (int i = 0; i < count; i++)
            {
                if (kind[i] != Movable || s.Scree[i] <= thin) continue;
                float travelled = s.Travel[i] / s.Scree[i];
                // 岩屑の薄い縁では割合を下げる（錐の縁で値が切れない）
                float cover = s.Scree[i] / 0.5f;
                if (cover > 1f) cover = 1f;
                coarse[i] = cover * travelled / (travelled + CoarseMeters);
            }
            int smooth = (int)Math.Round(1.5f * (RouteSmoothMeters / cell) * (RouteSmoothMeters / cell));
            if (smooth > 12) smooth = 12;
            float[] g = smooth > 0 ? Blur(s.H, s.Land, n, smooth) : s.H;
            router.Route(g, s.Land);
            var runout = new float[count];
            int steps = (int)Math.Ceiling(RunoutMeters / cell);
            for (int i = 0; i < count; i++)
            {
                if (coarse[i] <= 0f) continue;
                int r = router.Receiver[i];
                if (r < 0 || kind[r] != Movable || s.Scree[r] > thin) continue;
                int at = r;
                for (int step = 1; step <= steps && at >= 0 && kind[at] == Movable; step++)
                {
                    float value = coarse[i] * (1f - step * cell / (RunoutMeters + cell));
                    if (value > runout[at]) runout[at] = value;
                    at = router.Receiver[at];
                }
            }
            int widen = (int)Math.Round(2f / cell);
            if (widen > 0) runout = BoxBlur(runout, s.Land, n, widen);
            for (int i = 0; i < count; i++)
            {
                if (kind[i] != Movable) { coarse[i] = 0f; continue; }
                if (runout[i] > coarse[i]) coarse[i] = runout[i];
                if (coarse[i] > 1f) coarse[i] = 1f;
            }
            return coarse;
        }

        /// <summary>陸のセルだけで、一辺 2 × radius + 1 の四角の平均をとった新しい配列（足し上げた表で O(1)）。陸でないセルは元の値。</summary>
        private static float[] BoxBlur(float[] source, bool[] land, int n, int radius)
        {
            int stride = n + 1;
            var sum = new double[stride * stride];
            var number = new int[stride * stride];
            for (int y = 0; y < n; y++)
            {
                double row = 0.0;
                int rowCount = 0;
                for (int x = 0; x < n; x++)
                {
                    int i = x + y * n;
                    if (land[i]) { row += source[i]; rowCount++; }
                    int t = (x + 1) + (y + 1) * stride;
                    sum[t] = sum[t - stride] + row;
                    number[t] = number[t - stride] + rowCount;
                }
            }
            var result = new float[n * n];
            Parallel.For(0, n, y =>
            {
                int y0 = Math.Max(0, y - radius), y1 = Math.Min(n, y + radius + 1);
                for (int x = 0; x < n; x++)
                {
                    int i = x + y * n;
                    if (!land[i]) { result[i] = source[i]; continue; }
                    int x0 = Math.Max(0, x - radius), x1 = Math.Min(n, x + radius + 1);
                    int a = x1 + y1 * stride, b = x0 + y1 * stride, c = x1 + y0 * stride, d = x0 + y0 * stride;
                    int found = number[a] - number[b] - number[c] + number[d];
                    result[i] = (float)((sum[a] - sum[b] - sum[c] + sum[d]) / found);
                }
            });
            return result;
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
