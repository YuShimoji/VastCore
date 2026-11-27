using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Vastcore.RegionMap.Preview
{
    /// <summary>広域の海陸境界から形の違う岸を選び、現行の拡大地形を同じ距離から撮る。</summary>
    internal static class CoastSurvey
    {
        private const float WindowKm = 4.4f;
        private const int DetailResolution = 512;
        private const int RayCount = 7;
        private const int RayReachMeters = 2000;

        private readonly struct Site
        {
            internal readonly float X, Y, Yaw, InlandHeight;
            internal readonly int OpenRays;

            internal Site(float x, float y, float yaw, float inlandHeight, int openRays)
            {
                X = x;
                Y = y;
                Yaw = yaw;
                InlandHeight = inlandHeight;
                OpenRays = openRays;
            }
        }

        internal static void Write(RegionData region, string stem)
        {
            var watch = Stopwatch.StartNew();
            List<Site> sites = FindSites(region);
            var selected = new List<Site>();
            float minSpacing = WindowKm / region.KmPerMapUnit;
            using var csv = new StreamWriter(stem + "-coast.csv");
            csv.WriteLine("candidate,map_x,map_y,sea_yaw_deg,inland_max_height_m,open_rays,coast_agreement,river_agreement,detail_ms");

            var orders = new (string Name, IEnumerable<Site> Ranked)[]
            {
                ("high-relief", sites.OrderByDescending(site => site.InlandHeight).ThenByDescending(site => site.OpenRays)),
                ("low-open", sites.Where(site => site.OpenRays >= 5).OrderBy(site => site.InlandHeight)),
                ("sheltered", sites.Where(site => site.OpenRays < 5).OrderBy(site => site.OpenRays).ThenBy(site => site.InlandHeight)),
            };
            foreach (var order in orders)
            {
                bool found = false;
                foreach (Site site in order.Ranked)
                {
                    if (selected.Any(other => DistanceSquared(site, other) < minSpacing * minSpacing)) continue;
                    selected.Add(site);
                    WriteSite(region, site, order.Name, stem, csv);
                    found = true;
                    break;
                }
                if (!found) Console.WriteLine("海岸基準: " + order.Name + " の離れた候補なし");
            }
            Console.WriteLine($"海岸基準: {sites.Count}候補から{selected.Count}地点、比較出力 {watch.ElapsedMilliseconds}ms");
        }

        private static List<Site> FindSites(RegionData data)
        {
            var sites = new List<Site>();
            int n = data.Resolution;
            bool[] sea = RegionOceanMask.ConnectedSea(data);
            int margin = Math.Max(2, (int)Math.Ceiling(WindowKm * 500f / data.CellMeters) + 1);
            for (int y = margin; y < n - margin; y++)
            {
                for (int x = margin; x < n - margin; x++)
                {
                    int index = data.Index(x, y);
                    bool land = data.IsLand(index);
                    if ((land && sea[index + 1]) || (sea[index] && data.IsLand(index + 1)))
                        AddSite(data, sea, x + 0.5f, y, land ? 1 : -1, 0, sites);
                    if ((land && sea[index + n]) || (sea[index] && data.IsLand(index + n)))
                        AddSite(data, sea, x, y + 0.5f, 0, land ? 1 : -1, sites);
                }
            }
            return sites;
        }

        private static void AddSite(RegionData data, bool[] sea, float x, float y, int edgeSeaX, int edgeSeaY, List<Site> sites)
        {
            // 周囲の海側へ向く。狭い突端で方向が定まらなければ、交差したセル境界の向きに戻す。
            float seaX = 0f, seaY = 0f;
            int cellX = (int)x, cellY = (int)y;
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    if (!sea[data.Index(cellX + dx, cellY + dy)]) continue;
                    seaX += cellX + dx - x;
                    seaY += cellY + dy - y;
                }
            }
            if (seaX * edgeSeaX + seaY * edgeSeaY <= 0f)
            {
                seaX = edgeSeaX;
                seaY = edgeSeaY;
            }
            float yaw = MathF.Atan2(seaX, seaY);
            float ux = MathF.Sin(yaw), uy = MathF.Cos(yaw);
            float inlandHeight = 0f;
            int drySamples = 0;
            for (int distance = 250; distance <= 1000; distance += 250)
            {
                int index = Sample(data, x, y, -ux, -uy, distance);
                if (!data.IsLand(index) || data.Biome[index] == RegionBiome.Lake || data.Biome[index] == RegionBiome.River) continue;
                inlandHeight = Math.Max(inlandHeight, data.Height[index]);
                drySamples++;
            }
            if (drySamples < 3) return;

            int openRays = 0;
            for (int ray = 0; ray < RayCount; ray++)
            {
                float angle = yaw + (ray - RayCount / 2) * MathF.PI / 8f;
                float dx = MathF.Sin(angle), dy = MathF.Cos(angle);
                bool open = true;
                for (int distance = 500; distance <= RayReachMeters && open; distance += 250)
                    open = sea[Sample(data, x, y, dx, dy, distance)];
                if (open) openRays++;
            }
            float mapPerCell = RegionData.MapUnits / data.Resolution;
            sites.Add(new Site((x + 0.5f) * mapPerCell, (y + 0.5f) * mapPerCell,
                yaw * 180f / MathF.PI, inlandHeight, openRays));
        }

        private static int Sample(RegionData data, float x, float y, float dx, float dy, int meters)
        {
            float cells = meters / data.CellMeters;
            return data.Index((int)MathF.Floor(x + dx * cells + 0.5f), (int)MathF.Floor(y + dy * cells + 0.5f));
        }

        private static float DistanceSquared(Site a, Site b)
        {
            float dx = a.X - b.X, dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        private static void WriteSite(RegionData region, Site site, string name, string stem, StreamWriter csv)
        {
            var watch = Stopwatch.StartNew();
            RegionDetail detail = RegionDetailBuilder.Build(region, new RegionDetailSpec
            {
                CenterMapX = site.X,
                CenterMapY = site.Y,
                SizeKm = WindowKm,
                Resolution = DetailResolution,
            });
            long detailMs = watch.ElapsedMilliseconds;
            string prefix = stem + "-coast-" + name;
            RegionData terrain = detail.Data;
            var options = new RegionRenderOptions { Style = RegionMapStyle.Satellite, CastShadow = 0f, CloudCover = 0f };
            byte[] color = RegionMapRenderer.Render(terrain, options);
            PngWriter.Write(prefix + "-plan.png", color, terrain.Resolution, terrain.Resolution);

            RegionTerrainMesh mesh = RegionTerrainMesh.Build(terrain, 1, 1f);
            var target = RefineShore(terrain, site.Yaw, region.CellMeters);
            var shots = new (string Name, float Distance, float Pitch)[]
            {
                ("far", 1800f, 28f),
                ("mid", 450f, 15f),
                ("near", 55f, 7f),
            };
            foreach (var shot in shots)
            {
                var camera = new TerrainView.Camera
                {
                    Yaw = site.Yaw, Pitch = shot.Pitch, Distance = shot.Distance,
                    TargetMapX = target.X, TargetMapY = target.Y, TargetHeight = 1f,
                    Width = 960, Height = 540, Supersample = 1, Lit = true,
                };
                byte[] frame = TerrainView.Render(terrain, mesh, color, camera, out int width, out int height);
                PngWriter.Write(prefix + "-" + shot.Name + ".png", frame, width, height);
            }

            csv.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0},{1:R},{2:R},{3:F2},{4:F2},{5},{6:F4},{7:F4},{8}",
                name, site.X, site.Y, site.Yaw, site.InlandHeight, site.OpenRays, detail.CoastAgreement, detail.RiverAgreement, detailMs));
        }

        private static (float X, float Y) RefineShore(RegionData data, float yawDegrees, float coarseCellMeters)
        {
            // 広域の升目の境から拡大後の境へ注視点を寄せる。撮影位置が陸に埋まる境は避ける。
            int n = data.Resolution;
            int center = n / 2;
            int radius = Math.Clamp((int)Math.Ceiling(coarseCellMeters / data.CellMeters * 1.5f), 4, center / 2);
            float yaw = yawDegrees * MathF.PI / 180f;
            float dx = MathF.Sin(yaw), dy = MathF.Cos(yaw);
            int stepX = Math.Sign(dx), stepY = Math.Sign(dy);
            bool[] sea = RegionOceanMask.ConnectedSea(data);
            float best = float.PositiveInfinity;
            float bestX = center, bestY = center;
            void Consider(float x, float y)
            {
                float ox = x - center, oy = y - center;
                float distance = ox * ox + oy * oy;
                if (distance >= best || !sea[Sample(data, x, y, dx, dy, 54)] ||
                    !data.IsLand(Sample(data, x, y, -dx, -dy, 20))) return;
                best = distance;
                bestX = x;
                bestY = y;
            }
            for (int y = center - radius; y <= center + radius; y++)
            {
                for (int x = center - radius; x <= center + radius; x++)
                {
                    int i = data.Index(x, y);
                    if (!data.IsLand(i)) continue;
                    if (sea[i + stepX]) Consider(x + stepX * 0.5f, y);
                    if (sea[i + stepY * n]) Consider(x, y + stepY * 0.5f);
                }
            }
            if (float.IsPositiveInfinity(best))
                Console.WriteLine("海岸基準: 拡大窓で近景を撮る岸が見つからず、窓の中心を撮影");
            return (bestX * RegionData.MapUnits / n, bestY * RegionData.MapUnits / n);
        }
    }
}
