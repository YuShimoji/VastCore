using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Vastcore.RegionMap.Preview
{
    /// <summary>
    /// 使い方: dotnet run --project tools/RegionMapPreview -- --seed 7 --res 1024 --style satellite --out artifacts/region-map
    /// 追加の指定: --clouds 0.35  --sites（探索地点に印を付ける）  --scale 2（画像を 1/2 に縮めて保存）
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var spec = new RegionSpec();
            var options = new RegionRenderOptions();
            string outDir = "artifacts/region-map";
            string styles = "satellite";
            bool markSites = false;
            int shrink = 1;
            string name = null;

            for (int a = 0; a < args.Length; a++)
            {
                string key = args[a];
                string value = a + 1 < args.Length ? args[a + 1] : "";
                switch (key)
                {
                    case "--seed": spec.Seed = int.Parse(value); a++; break;
                    case "--res": spec.Resolution = int.Parse(value); a++; break;
                    case "--size": spec.SizeKm = Float(value); a++; break;
                    case "--land": spec.LandBias = Float(value); a++; break;
                    case "--mountain": spec.MountainAmount = Float(value); a++; break;
                    case "--iterations": spec.ErosionIterations = int.Parse(value); a++; break;
                    case "--uplift": spec.UpliftMeters = Float(value); a++; break;
                    case "--erosion": spec.ErosionStrength = Float(value); a++; break;
                    case "--maxslope": spec.MaxSlope = Float(value); a++; break;
                    case "--diffusion": spec.HillslopeDiffusion = Float(value); a++; break;
                    case "--falloff": spec.EdgeFalloff = Float(value); a++; break;
                    case "--humidity": spec.Humidity = Float(value); a++; break;
                    case "--north": spec.TemperatureNorth = Float(value); a++; break;
                    case "--south": spec.TemperatureSouth = Float(value); a++; break;
                    case "--style": styles = value; a++; break;
                    case "--clouds": options.CloudCover = Float(value); a++; break;
                    case "--exaggeration": options.Exaggeration = Float(value); a++; break;
                    case "--shadow": options.CastShadow = Float(value); a++; break;
                    case "--out": outDir = value; a++; break;
                    case "--name": name = value; a++; break;
                    case "--scale": shrink = int.Parse(value); a++; break;
                    case "--sites": markSites = true; break;
                    default:
                        Console.Error.WriteLine("不明な指定: " + key);
                        return 2;
                }
            }

            Directory.CreateDirectory(outDir);
            var watch = Stopwatch.StartNew();
            RegionData data = RegionGenerator.Generate(spec);
            long generateMs = watch.ElapsedMilliseconds;
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "seed={0} res={1} 生成={2}ms 陸={3:P1} 最高={4:F0}m 地点={5}",
                spec.Seed, spec.Resolution, generateMs, data.LandFraction, data.MaxHeight, data.Sites.Count));

            var biomeCount = new int[Enum.GetValues(typeof(RegionBiome)).Length];
            for (int i = 0; i < data.Biome.Length; i++) biomeCount[(int)data.Biome[i]]++;
            for (int k = 1; k < biomeCount.Length; k++)
            {
                if (biomeCount[k] == 0) continue;
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "  {0,-10} {1:P1}",
                    (RegionBiome)k, biomeCount[k] / (double)data.Biome.Length));
            }
            foreach (RegionSite site in data.Sites)
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "  地点{0:00} X{1:F1} Y{2:F1} {3:F0}m {4}",
                    site.Id, site.MapX, site.MapY, site.HeightMeters, site.Name));
            }

            foreach (string styleName in styles.Split(','))
            {
                options.Style = (RegionMapStyle)Enum.Parse(typeof(RegionMapStyle), styleName, true);
                watch.Restart();
                byte[] pixels = RegionMapRenderer.Render(data, options);
                long renderMs = watch.ElapsedMilliseconds;
                int size = data.Resolution;
                if (markSites) MarkSites(pixels, size, data);
                if (shrink > 1) pixels = Shrink(pixels, size, shrink, out size);
                string file = Path.Combine(outDir, (name ?? ("region-seed" + spec.Seed)) + "-" + options.Style.ToString().ToLowerInvariant() + ".png");
                PngWriter.Write(file, pixels, size, size);
                Console.WriteLine("描画=" + renderMs + "ms → " + Path.GetFullPath(file));
            }
            return 0;
        }

        private static float Float(string value)
        {
            return float.Parse(value, CultureInfo.InvariantCulture);
        }

        private static void MarkSites(byte[] pixels, int n, RegionData data)
        {
            int radius = Math.Max(4, n / 110);
            foreach (RegionSite site in data.Sites)
            {
                int cx = (int)(site.MapX / RegionData.MapUnits * n);
                int cy = (int)(site.MapY / RegionData.MapUnits * n);
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        float d = (float)Math.Sqrt(dx * dx + dy * dy);
                        if (d > radius || d < radius - 2.2f) continue;
                        int x = cx + dx;
                        int y = cy + dy;
                        if (x < 0 || y < 0 || x >= n || y >= n) continue;
                        int p = (x + y * n) * 4;
                        pixels[p] = 255;
                        pixels[p + 1] = 70;
                        pixels[p + 2] = 60;
                    }
                }
            }
        }

        private static byte[] Shrink(byte[] pixels, int n, int factor, out int size)
        {
            size = n / factor;
            var result = new byte[size * size * 4];
            int area = factor * factor;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    for (int ch = 0; ch < 4; ch++)
                    {
                        int sum = 0;
                        for (int sy = 0; sy < factor; sy++)
                        {
                            for (int sx = 0; sx < factor; sx++)
                            {
                                sum += pixels[((x * factor + sx) + (y * factor + sy) * n) * 4 + ch];
                            }
                        }
                        result[(x + y * size) * 4 + ch] = (byte)(sum / area);
                    }
                }
            }
            return result;
        }
    }
}
