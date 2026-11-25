using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Vastcore.RegionMap.Preview
{
    /// <summary>
    /// 通行の区分と道筋（RegionTraversal、3D 化の試作・段階 C）を、地図に重ねた図と数値で出す確認用の道具。
    /// 図の色：橙＝手を使って登る斜面、赤＝崖（通れない）、暗い青＝出発点から歩いて着けない陸、白い細線＝着くまでの時間が等しい線、
    /// 黄の太線＝最も早く着く道筋、緑の輪＝出発点、白の輪＝行き先。
    /// </summary>
    internal static class RouteView
    {
        internal sealed class Result
        {
            public RegionTraversal Traversal;
            public RegionTravelField Field;
            public RegionRoute Route;
            public int Source;
            public int Target;
            public float IsochroneHours;
        }

        /// <summary>
        /// 拡大した地形で、出発点（窓の縁でいちばん低い陸＝谷の入口）と行き先（窓の中でいちばん高い所）を選ぶ。
        /// </summary>
        internal static void PickEnds(RegionData terrain, out int source, out int target)
        {
            int n = terrain.Resolution;
            source = -1;
            target = -1;
            float lowest = float.MaxValue;
            float highest = float.MinValue;
            const int Edge = 3;
            const int Inset = 12;
            for (int y = Edge; y < n - Edge; y++)
            {
                for (int x = Edge; x < n - Edge; x++)
                {
                    int i = x + y * n;
                    float h = terrain.Height[i];
                    if (h <= 0f || terrain.LakeDepth[i] > 0f) continue;
                    bool border = x == Edge || y == Edge || x == n - Edge - 1 || y == n - Edge - 1;
                    if (border && h < lowest) { lowest = h; source = i; }
                    bool inside = x >= Inset && y >= Inset && x < n - Inset && y < n - Inset;
                    if (inside && h > highest) { highest = h; target = i; }
                }
            }
        }

        /// <summary>source から歩いて行ける範囲と、target までの道筋を求めて、数値を 1 行ずつ出す。</summary>
        internal static Result Analyze(RegionData terrain, int source, int target, string label)
        {
            RegionTraversal traversal = RegionTraversal.Build(terrain, null);
            int reach = Math.Max(4, terrain.Resolution / 64);
            var result = new Result
            {
                Traversal = traversal,
                Source = source >= 0 ? traversal.NearestStandable(source, reach) : -1,
                Target = target >= 0 ? traversal.NearestStandable(target, reach) : -1,
            };
            var watch = System.Diagnostics.Stopwatch.StartNew();
            result.Field = traversal.Travel(result.Source);
            result.Route = result.Target >= 0 ? result.Field.RouteTo(result.Target) : null;

            int standable = traversal.Counts[(int)RegionGround.Walk] + traversal.Counts[(int)RegionGround.Steep]
                + traversal.Counts[(int)RegionGround.Scramble] + traversal.Counts[(int)RegionGround.Ford];
            float longest = 0f;
            foreach (float h in result.Field.Hours) if (!float.IsPositiveInfinity(h) && h > longest) longest = h;
            result.IsochroneHours = NiceStep(longest / 12f);
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "通行（{0}）: 歩ける={1:P1} 急な斜面={2:P1} 手を使う={3:P1} 崖={4:P2} / 出発点から着ける陸={5:P1} 計算={6}ms",
                label, traversal.LandShare(RegionGround.Walk), traversal.LandShare(RegionGround.Steep),
                traversal.LandShare(RegionGround.Scramble), traversal.LandShare(RegionGround.Cliff),
                standable > 0 ? result.Field.Reached / (double)standable : 0.0, watch.ElapsedMilliseconds));
            Console.WriteLine("道筋（" + label + "）: 谷の入口 → いちばん高い所  " + Describe(result.Route));
            return result;
        }

        internal static string Describe(RegionRoute route)
        {
            if (route == null) return "着けない";
            return string.Format(CultureInfo.InvariantCulture,
                "{0:F0}分 道のり{1:F2}km 直線{2:F2}km 迂回{3:F2}倍 登り{4:F0}m 下り{5:F0}m",
                route.Hours * 60f, route.LengthMeters / 1000f, route.StraightMeters / 1000f, route.Detour, route.AscentMeters, route.DescentMeters);
        }

        /// <summary>区分・着けない陸・時間の等しい線・道筋・両端を、真上から見た画像（RGBA、地形と同じ解像度）に描き込む。</summary>
        internal static void Paint(byte[] pixels, Result result)
        {
            RegionTraversal traversal = result.Traversal;
            int n = traversal.Data.Resolution;
            float[] hours = result.Field.Hours;
            float step = result.IsochroneHours;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = x + y * n;
                    RegionGround ground = traversal.Ground[i];
                    if (ground == RegionGround.Water) continue;
                    if (ground == RegionGround.Cliff) { Blend(pixels, i, 225, 45, 40, 0.8f); continue; }
                    if (float.IsPositiveInfinity(hours[i])) { Blend(pixels, i, 36, 40, 86, 0.6f); continue; }
                    if (ground == RegionGround.Scramble) Blend(pixels, i, 255, 165, 50, 0.45f);
                    if (step <= 0f || x >= n - 1 || y >= n - 1) continue;
                    int band = (int)(hours[i] / step);
                    float right = hours[i + 1];
                    float down = hours[i + n];
                    if ((!float.IsPositiveInfinity(right) && (int)(right / step) != band)
                        || (!float.IsPositiveInfinity(down) && (int)(down / step) != band))
                        Blend(pixels, i, 255, 255, 255, 0.5f);
                }
            }
            float width = Math.Max(1.2f, n / 420f);
            if (result.Route != null) DrawRoute(pixels, n, result.Route, width);
            if (result.Source >= 0) Ring(pixels, n, result.Source % n, result.Source / n, width * 5f, 60, 230, 90);
            if (result.Target >= 0) Ring(pixels, n, result.Target % n, result.Target / n, width * 5f, 255, 255, 255);
        }

        /// <summary>
        /// 広域の地域で、陸の探索地点どうしを歩いて結ぶ道筋を出す。地点ごとに、早く着ける順に 2 つの地点へ線を引く。
        /// </summary>
        internal static void WriteRegion(RegionData data, RegionMapStyle style, string stem)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            RegionTraversal traversal = RegionTraversal.Build(data, null);
            int n = data.Resolution;
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "通行（広域、1セル={0:F0}m）: 歩ける={1:P1} 急な斜面={2:P1} 手を使う={3:P1} 崖={4:P2} 川={5:P1}",
                data.CellMeters, traversal.LandShare(RegionGround.Walk), traversal.LandShare(RegionGround.Steep),
                traversal.LandShare(RegionGround.Scramble), traversal.LandShare(RegionGround.Cliff), traversal.LandShare(RegionGround.Ford)));

            var sites = new List<RegionSite>();
            var cells = new List<int>();
            foreach (RegionSite site in data.Sites)
            {
                int cell = traversal.NearestStandable(data.CellFromMap(site.MapX, site.MapY), 3);
                if (cell < 0)
                {
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "  地点{0:00} は陸に立てる場所が無い（海の地点）", site.Id));
                    continue;
                }
                sites.Add(site);
                cells.Add(cell);
            }

            byte[] pixels = RegionMapRenderer.Render(data, new RegionRenderOptions { Style = style, CloudCover = 0f });
            for (int i = 0; i < n * n; i++)
            {
                if (traversal.Ground[i] == RegionGround.Cliff) Blend(pixels, i, 225, 45, 40, 0.8f);
                else if (traversal.Ground[i] == RegionGround.Scramble) Blend(pixels, i, 255, 165, 50, 0.45f);
            }

            var drawn = new HashSet<long>();
            float width = Math.Max(1.2f, n / 420f);
            double detourSum = 0.0;
            int routeCount = 0;
            int unreachable = 0;
            for (int s = 0; s < sites.Count; s++)
            {
                RegionTravelField field = traversal.Travel(cells[s]);
                var order = new List<int>();
                for (int t = 0; t < sites.Count; t++)
                {
                    if (t == s) continue;
                    if (field.CanReach(cells[t])) order.Add(t); else unreachable++;
                }
                order.Sort((p, q) => field.Hours[cells[p]].CompareTo(field.Hours[cells[q]]));
                for (int k = 0; k < order.Count && k < 2; k++)
                {
                    int t = order[k];
                    RegionRoute route = field.RouteTo(cells[t]);
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "  地点{0:00} → 地点{1:00}  {2:F1}時間 道のり{3:F0}km 直線{4:F0}km 迂回{5:F2}倍 登り{6:F0}m",
                        sites[s].Id, sites[t].Id, route.Hours, route.LengthMeters / 1000f, route.StraightMeters / 1000f, route.Detour, route.AscentMeters));
                    detourSum += route.Detour;
                    routeCount++;
                    long key = Math.Min(s, t) * 1000L + Math.Max(s, t);
                    if (drawn.Add(key)) DrawRoute(pixels, n, route, width);
                }
            }
            foreach (int cell in cells) Ring(pixels, n, cell % n, cell / n, width * 4f, 255, 255, 255);
            string file = stem + "-routes.png";
            PngWriter.Write(file, pixels, n, n);
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "道筋（広域）: 陸の地点={0} 線={1}本 迂回の平均={2:F2}倍 歩いて着けない地点の組={3} 計算={4}ms → {5}",
                sites.Count, drawn.Count, routeCount > 0 ? detourSum / routeCount : 0.0, unreachable / 2, watch.ElapsedMilliseconds, Path.GetFullPath(file)));
        }

        private static float NiceStep(float hours)
        {
            float[] minutes = { 1f, 2f, 5f, 10f, 15f, 20f, 30f, 60f, 120f, 240f };
            foreach (float m in minutes) if (hours * 60f <= m) return m / 60f;
            return 8f;
        }

        private static void DrawRoute(byte[] pixels, int n, RegionRoute route, float width)
        {
            foreach (int cell in route.Cells) Disc(pixels, n, cell % n, cell / n, width + 1.2f, 20, 20, 20);
            foreach (int cell in route.Cells) Disc(pixels, n, cell % n, cell / n, width, 255, 235, 90);
        }

        private static void Disc(byte[] pixels, int n, int cx, int cy, float radius, byte r, byte g, byte b)
        {
            int reach = (int)Math.Ceiling(radius);
            for (int dy = -reach; dy <= reach; dy++)
            {
                for (int dx = -reach; dx <= reach; dx++)
                {
                    if (dx * dx + dy * dy > radius * radius) continue;
                    int x = cx + dx;
                    int y = cy + dy;
                    if (x < 0 || y < 0 || x >= n || y >= n) continue;
                    int p = (x + y * n) * 4;
                    pixels[p] = r; pixels[p + 1] = g; pixels[p + 2] = b;
                }
            }
        }

        private static void Ring(byte[] pixels, int n, int cx, int cy, float radius, byte r, byte g, byte b)
        {
            int reach = (int)Math.Ceiling(radius) + 2;
            float thickness = Math.Max(1.6f, radius * 0.3f);
            for (int dy = -reach; dy <= reach; dy++)
            {
                for (int dx = -reach; dx <= reach; dx++)
                {
                    float d = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (d > radius + 1.2f || d < radius - thickness - 1.2f) continue;
                    int x = cx + dx;
                    int y = cy + dy;
                    if (x < 0 || y < 0 || x >= n || y >= n) continue;
                    int p = (x + y * n) * 4;
                    bool outline = d > radius || d < radius - thickness;
                    pixels[p] = outline ? (byte)20 : r;
                    pixels[p + 1] = outline ? (byte)20 : g;
                    pixels[p + 2] = outline ? (byte)20 : b;
                }
            }
        }

        private static void Blend(byte[] pixels, int cell, float r, float g, float b, float amount)
        {
            int p = cell * 4;
            pixels[p] = (byte)(pixels[p] + (r - pixels[p]) * amount);
            pixels[p + 1] = (byte)(pixels[p + 1] + (g - pixels[p + 1]) * amount);
            pixels[p + 2] = (byte)(pixels[p + 2] + (b - pixels[p + 2]) * amount);
        }
    }
}
