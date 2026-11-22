using System;
using System.Collections.Generic;

namespace Vastcore.RegionMap
{
    /// <summary>名前を持てる地物の種類（エリアと探索地点の他に、地域そのもの・川・峰・湾）。</summary>
    public enum RegionFeatureKind
    {
        Region = 0,
        River = 1,
        Peak = 2,
        Bay = 3,
    }

    /// <summary>名前を持てる地物 1 つ。地形から取り出す（RegionFeatureBuilder）。</summary>
    [Serializable]
    public sealed class RegionFeature
    {
        /// <summary>種類の中での番号（1 から。地域そのものは 0）。</summary>
        public int Id;
        public RegionFeatureKind Kind;

        /// <summary>名前を置く位置（川は本流の中ほど、峰は頂、湾は入り込んだ海の中ほど）。</summary>
        public float MapX;
        public float MapY;

        /// <summary>属するエリアの番号（川は河口のエリア、湾は最寄りの陸のエリア）。無ければ 0。</summary>
        public int AreaId;

        /// <summary>川の本流の長さ（km）。川以外は 0。</summary>
        public float LengthKm;

        /// <summary>峰の標高（m）。峰以外は 0。</summary>
        public float HeightMeters;

        /// <summary>湾の広さ（km²）。湾以外は 0。</summary>
        public float AreaKm2;

        /// <summary>川の本流の通り道（地図座標。河口から源へ、間引いた点）。川以外は null。</summary>
        public float[] PathX;
        public float[] PathY;

        /// <summary>画面に出す名前。名前を付ける前は null。</summary>
        public string Name;
        public string NameOrigin;
        public List<RegionPlaceName> Names = new List<RegionPlaceName>();
        public RegionLandHistory History;
    }

    /// <summary>
    /// 地形から、名前を持てる地物（地域そのもの・大きな川・高い峰・湾）を取り出す。
    /// 数を絞るのは、名前の数を地図で読める範囲に収めるため（川 6・峰 6・湾 4 まで）。
    /// </summary>
    public static class RegionFeatureBuilder
    {
        public const int MaxRivers = 6;
        public const int MaxPeaks = 6;
        public const int MaxBays = 4;

        /// <summary>名前を付ける川の本流の長さの下限（km）。</summary>
        public const float MinRiverKm = 20f;

        public static void Build(RegionData data)
        {
            data.Features.Clear();
            data.Features.Add(new RegionFeature { Id = 0, Kind = RegionFeatureKind.Region, MapX = 50f, MapY = 50f });
            if (data.Downstream != null) AddRivers(data);
            AddPeaks(data);
            AddBays(data);
        }

        private static void AddRivers(RegionData data)
        {
            int n = data.Resolution;
            float stream = RegionGenerator.RiverThreshold(data);
            float broad = stream * 2.5f;

            // 各セルへ流れ込む上流のうち、流量が最も多いセル（本流をさかのぼるため）
            var bestUp = new int[n * n];
            for (int c = 0; c < bestUp.Length; c++) bestUp[c] = -1;
            for (int c = 0; c < bestUp.Length; c++)
            {
                int d = data.Downstream[c];
                if (d < 0 || !data.IsLand(c)) continue;
                if (bestUp[d] < 0 || data.Flow[c] > data.Flow[bestUp[d]]) bestUp[d] = c;
            }

            var mouths = new List<int>();
            for (int c = 0; c < bestUp.Length; c++)
            {
                if (!data.IsLand(c) || data.Flow[c] <= broad) continue;
                int d = data.Downstream[c];
                if (d < 0 || !data.IsLand(d)) mouths.Add(c);
            }
            mouths.Sort((a, b) => data.Flow[b] != data.Flow[a] ? data.Flow[b].CompareTo(data.Flow[a]) : a.CompareTo(b));

            float cellKm = data.SizeKm / n;
            int stride = Math.Max(1, n / 128);
            var made = new List<RegionFeature>();
            for (int m = 0; m < mouths.Count && made.Count < MaxRivers; m++)
            {
                int mouth = mouths[m];
                float mouthX = data.MapXFromCell(mouth);
                float mouthY = data.MapYFromCell(mouth);
                bool crowded = false;
                for (int k = 0; k < made.Count; k++)
                {
                    float dx = made[k].PathX[0] - mouthX;
                    float dy = made[k].PathY[0] - mouthY;
                    if (dx * dx + dy * dy < 6f * 6f) crowded = true;
                }
                if (crowded) continue;

                var path = new List<int>();
                float length = 0f;
                int cur = mouth;
                while (cur >= 0 && path.Count < n * 4 && data.Flow[cur] > stream)
                {
                    path.Add(cur);
                    int up = bestUp[cur];
                    if (up >= 0)
                    {
                        int dx = up % n - cur % n;
                        int dy = up / n - cur / n;
                        length += cellKm * (float)Math.Sqrt(dx * dx + dy * dy);
                    }
                    cur = up;
                }
                if (length < MinRiverKm) continue;

                var xs = new List<float>();
                var ys = new List<float>();
                for (int p = 0; p < path.Count; p += stride)
                {
                    xs.Add(data.MapXFromCell(path[p]));
                    ys.Add(data.MapYFromCell(path[p]));
                }
                int middle = path[path.Count / 2];
                made.Add(new RegionFeature
                {
                    Id = made.Count + 1,
                    Kind = RegionFeatureKind.River,
                    MapX = data.MapXFromCell(middle),
                    MapY = data.MapYFromCell(middle),
                    AreaId = data.Area[mouth],
                    LengthKm = length,
                    PathX = xs.ToArray(),
                    PathY = ys.ToArray(),
                });
            }
            data.Features.AddRange(made);
        }

        private static void AddPeaks(RegionData data)
        {
            int n = data.Resolution;
            int step = Math.Max(1, n / 256);
            int g = n / step;
            // 粗い升ごとの最高点（升の中で最も高いセル）
            var top = new int[g * g];
            for (int gy = 0; gy < g; gy++)
            {
                for (int gx = 0; gx < g; gx++)
                {
                    int best = -1;
                    for (int oy = 0; oy < step; oy++)
                    {
                        for (int ox = 0; ox < step; ox++)
                        {
                            int c = (gx * step + ox) + (gy * step + oy) * n;
                            if (best < 0 || data.Height[c] > data.Height[best]) best = c;
                        }
                    }
                    top[gx + gy * g] = best;
                }
            }

            int window = Math.Max(2, g * 3 / 100);
            float floor = data.MaxHeight * 0.45f;
            var peaks = new List<int>();
            for (int gy = 0; gy < g; gy++)
            {
                for (int gx = 0; gx < g; gx++)
                {
                    int c = top[gx + gy * g];
                    float h = data.Height[c];
                    if (h < floor || !data.IsLand(c)) continue;
                    bool highest = true;
                    for (int oy = -window; oy <= window && highest; oy++)
                    {
                        for (int ox = -window; ox <= window; ox++)
                        {
                            int x = gx + ox;
                            int y = gy + oy;
                            if (x < 0 || y < 0 || x >= g || y >= g || (ox == 0 && oy == 0)) continue;
                            int other = top[x + y * g];
                            if (data.Height[other] > h || (data.Height[other] == h && other < c))
                            {
                                highest = false;
                                break;
                            }
                        }
                    }
                    if (highest) peaks.Add(c);
                }
            }
            peaks.Sort((a, b) => data.Height[b] != data.Height[a] ? data.Height[b].CompareTo(data.Height[a]) : a.CompareTo(b));

            var made = new List<RegionFeature>();
            for (int p = 0; p < peaks.Count && made.Count < MaxPeaks; p++)
            {
                float x = data.MapXFromCell(peaks[p]);
                float y = data.MapYFromCell(peaks[p]);
                bool crowded = false;
                for (int k = 0; k < made.Count; k++)
                {
                    float dx = made[k].MapX - x;
                    float dy = made[k].MapY - y;
                    if (dx * dx + dy * dy < 8f * 8f) crowded = true;
                }
                if (crowded) continue;
                made.Add(new RegionFeature
                {
                    Id = made.Count + 1,
                    Kind = RegionFeatureKind.Peak,
                    MapX = x,
                    MapY = y,
                    AreaId = data.Area[peaks[p]],
                    HeightMeters = data.Height[peaks[p]],
                });
            }
            data.Features.AddRange(made);
        }

        /// <summary>
        /// 湾＝周りの半径約 10 km の 58% 以上が陸である海のまとまり。粗い升（128×128）で数える。
        /// 細い入り江や河口の前の海も拾うことがある。
        /// </summary>
        private static void AddBays(RegionData data)
        {
            int n = data.Resolution;
            const int g = 128;
            if (n < g) return;
            int step = n / g;
            var land = new bool[g * g];
            var sum = new int[(g + 1) * (g + 1)];
            for (int gy = 0; gy < g; gy++)
            {
                for (int gx = 0; gx < g; gx++)
                {
                    int c = (gx * step + step / 2) + (gy * step + step / 2) * n;
                    bool isLand = data.IsLand(c);
                    land[gx + gy * g] = isLand;
                    sum[(gx + 1) + (gy + 1) * (g + 1)] = (isLand ? 1 : 0) + sum[gx + (gy + 1) * (g + 1)] + sum[(gx + 1) + gy * (g + 1)] - sum[gx + gy * (g + 1)];
                }
            }

            int radius = Math.Max(2, (int)Math.Round(10f / (data.SizeKm / g)));
            var enclosed = new bool[g * g];
            for (int gy = 0; gy < g; gy++)
            {
                for (int gx = 0; gx < g; gx++)
                {
                    if (land[gx + gy * g]) continue;
                    int x0 = Math.Max(0, gx - radius);
                    int y0 = Math.Max(0, gy - radius);
                    int x1 = Math.Min(g, gx + radius + 1);
                    int y1 = Math.Min(g, gy + radius + 1);
                    int cells = (x1 - x0) * (y1 - y0);
                    int lands = sum[x1 + y1 * (g + 1)] - sum[x0 + y1 * (g + 1)] - sum[x1 + y0 * (g + 1)] + sum[x0 + y0 * (g + 1)];
                    // 地図の端で窓が欠けた分は海として数える（端の海を湾にしない）
                    int full = (2 * radius + 1) * (2 * radius + 1);
                    enclosed[gx + gy * g] = lands >= full * 0.58f && cells > 0;
                }
            }

            var seen = new bool[g * g];
            var groups = new List<List<int>>();
            var stack = new Stack<int>();
            for (int start = 0; start < enclosed.Length; start++)
            {
                if (!enclosed[start] || seen[start]) continue;
                var group = new List<int>();
                seen[start] = true;
                stack.Push(start);
                while (stack.Count > 0)
                {
                    int c = stack.Pop();
                    group.Add(c);
                    int x = c % g;
                    int y = c / g;
                    if (x > 0) Visit(c - 1, enclosed, seen, stack);
                    if (x < g - 1) Visit(c + 1, enclosed, seen, stack);
                    if (y > 0) Visit(c - g, enclosed, seen, stack);
                    if (y < g - 1) Visit(c + g, enclosed, seen, stack);
                }
                if (group.Count >= 5) groups.Add(group);
            }
            groups.Sort((a, b) => b.Count != a.Count ? b.Count.CompareTo(a.Count) : a[0].CompareTo(b[0]));

            float cellKm = data.SizeKm / g;
            int made = 0;
            for (int k = 0; k < groups.Count && made < MaxBays; k++)
            {
                List<int> group = groups[k];
                float cx = 0f;
                float cy = 0f;
                for (int i = 0; i < group.Count; i++)
                {
                    cx += group[i] % g;
                    cy += group[i] / g;
                }
                cx /= group.Count;
                cy /= group.Count;
                // 重心が陸に落ちることがあるので、まとまりの中で重心に最も近い升へ寄せる
                int nearest = group[0];
                float nearestDistance = float.MaxValue;
                for (int i = 0; i < group.Count; i++)
                {
                    float dx = group[i] % g - cx;
                    float dy = group[i] / g - cy;
                    float distance = dx * dx + dy * dy;
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = group[i];
                    }
                }
                int cell = (nearest % g * step + step / 2) + (nearest / g * step + step / 2) * n;
                made++;
                data.Features.Add(new RegionFeature
                {
                    Id = made,
                    Kind = RegionFeatureKind.Bay,
                    MapX = data.MapXFromCell(cell),
                    MapY = data.MapYFromCell(cell),
                    AreaId = data.Area[cell],
                    AreaKm2 = group.Count * cellKm * cellKm,
                });
            }
        }

        private static void Visit(int cell, bool[] enclosed, bool[] seen, Stack<int> stack)
        {
            if (!enclosed[cell] || seen[cell]) return;
            seen[cell] = true;
            stack.Push(cell);
        }
    }
}
