using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

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
            bool cloudTile = false;
            bool areas = false;
            bool suitability = false;
            bool measure = false;
            bool traceTerrain = false;
            bool lakeDetail = false;
            bool compareBaseline = false;
            bool baselineTerrain = false;
            bool developmentDetails = false;
            int cloudFrames = 0;
            float cloudStep = 4f;
            bool cloudBefore = false;
            bool weatherPreview = false;
            bool names = false;
            string detailSites = null;
            var detailSpec = new RegionDetailSpec();
            bool forms = false;
            float formScale = 1f;
            string detailSizes = "8";
            float detailExaggeration = 1.5f;

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
                    case "--jitter": spec.FlowJitter = Float(value); a++; break;
                    case "--rough": spec.RoughnessMeters = Float(value); a++; break;
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
                    case "--cloudtile": cloudTile = true; break;
                    case "--areas": areas = true; break;
                    case "--suitability": suitability = true; break;
                    case "--measure": measure = true; break;
                    case "--trace-terrain": traceTerrain = true; break;
                    case "--lake-detail": lakeDetail = true; break;
                    case "--compare-baseline": compareBaseline = true; break;
                    case "--baseline-terrain": baselineTerrain = true; break;
                    case "--modern-development": spec.ModernDevelopment = true; break;
                    case "--development-details": spec.ModernDevelopment = true; developmentDetails = true; break;
                    case "--cloudframes": cloudFrames = int.Parse(value); a++; break;
                    case "--cloudstep": cloudStep = Float(value); a++; break;
                    case "--cloudbefore": cloudBefore = true; break;
                    case "--weather-preview": weatherPreview = true; break;
                    case "--names": names = true; break;
                    case "--naming":
                        spec.NamingRulesText = File.ReadAllText(value, System.Text.Encoding.UTF8);
                        a++;
                        break;
                    case "--naming-export":
                        File.WriteAllText(value, RegionNamingRules.DefaultText.Replace("\r\n", "\n"), new System.Text.UTF8Encoding(false));
                        Console.WriteLine("命名の規則（既定）→ " + Path.GetFullPath(value));
                        a++;
                        break;
                    case "--sound": spec.NamingSound = value; a++; break;
                    case "--namestyle": spec.NameStyle = (RegionNameStyle)Enum.Parse(typeof(RegionNameStyle), value, true); a++; break;
                    case "--detail": detailSites = value; a++; break;
                    case "--detailsize": detailSizes = value; a++; break;
                    case "--detailexag": detailExaggeration = Float(value); a++; break;
                    case "--detailres": detailSpec.Resolution = int.Parse(value); a++; break;
                    case "--detailrough": detailSpec.Roughness = Float(value); a++; break;
                    case "--gully": detailSpec.GullyPasses = int.Parse(value); a++; break;
                    case "--forms": forms = true; break;
                    case "--formscale": formScale = Float(value); a++; break;
                    default:
                        Console.Error.WriteLine("不明な指定: " + key);
                        return 2;
                }
            }

            Directory.CreateDirectory(outDir);
            var watch = Stopwatch.StartNew();
            if (cloudTile) return WriteCloudTile(spec, options, outDir, name);
            string phaseName = null;
            long phaseStart = 0;
            long erosionMs = 0;
            var phaseLog = new System.Text.StringBuilder();
            var terrain = traceTerrain ? new TerrainMeasurements.Trace(spec) : null;
            RegionData data = RegionGenerator.Generate(spec, next =>
            {
                long now = watch.ElapsedMilliseconds;
                if (phaseName != null) phaseLog.Append("  ").Append(phaseName).Append("=").Append(now - phaseStart).Append("ms");
                if (phaseName == "隆起と侵食") erosionMs = now - phaseStart;
                phaseName = next;
                phaseStart = now;
            }, terrain == null ? null : (Action<string, int, float[]>)terrain.Record, baselineTerrain);
            long generateMs = watch.ElapsedMilliseconds;
            if (spec.ModernDevelopment && data.Development == null)
            {
                Console.Error.WriteLine("この地形では2都市・幹線・空港を配置できる場所が見つかりませんでした。");
                return 1;
            }
            Console.WriteLine("工程:" + phaseLog);
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "seed={0} res={1} 生成={2}ms 陸={3:P1} 最高={4:F0}m 地点={5}",
                spec.Seed, spec.Resolution, generateMs, data.LandFraction, data.MaxHeight, data.Sites.Count));
            string stem = Path.Combine(outDir, name ?? ("region-seed" + spec.Seed));
            if (weatherPreview) return WeatherPreview.Write(data, stem, Math.Max(1, cloudFrames));
            if (data.Development != null) DescribeDevelopment(data.Development);
            terrain?.Write(stem);
            if (measure) TerrainMeasurements.Write(data, stem, generateMs, erosionMs);
            if (lakeDetail) TerrainMeasurements.WriteLakeDetail(data, stem);
            if (compareBaseline) TerrainMeasurements.ComparePlains(data, stem);

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
                watch.Restart();
                PngWriter.Write(file, pixels, size, size);
                Console.WriteLine("描画=" + renderMs + "ms PNG保存=" + watch.ElapsedMilliseconds + "ms → " + Path.GetFullPath(file));
            }

            if (areas || suitability)
            {
                foreach (RegionArea area in data.Areas)
                {
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "  エリア{0:00} {1}  {2:F0}km2 平均{3:F0}m 最高{4:F0}m 地点{5}  X{6:F1} Y{7:F1}  {8}",
                        area.Id, area.Name, area.AreaKm2, area.MeanHeight, area.MaxHeight, area.SiteCount, area.LabelX, area.LabelY, area.Basis));
                }
                // 下地は、雲の無い衛星写真風を暗くしたもの
                options.Style = RegionMapStyle.Satellite;
                float cover = options.CloudCover;
                options.CloudCover = 0f;
                byte[] ground = RegionMapRenderer.Render(data, options);
                options.CloudCover = cover;
                if (areas) WriteAreas(data, ground, stem, shrink);
                if (suitability) WriteSuitability(data, ground, stem, shrink);
                File.WriteAllText(stem + "-areas.json", DescribeJson(data), new System.Text.UTF8Encoding(false));
                Console.WriteLine("エリアと地点の一覧 → " + Path.GetFullPath(stem + "-areas.json"));
            }
            if (cloudFrames > 0)
            {
                WriteCloudFrames(data, options, stem, cloudFrames, cloudStep, shrink, false);
                if (cloudBefore) WriteCloudFrames(data, options, stem, cloudFrames, cloudStep, shrink, true);
            }
            if (names) WriteNames(data, stem);
            if (detailSites != null)
            {
                options.Exaggeration = detailExaggeration;
                WriteDetails(data, options, stem, detailSites, detailSizes, detailSpec, forms, formScale);
            }
            if (developmentDetails) WriteDevelopmentDetails(data, options.Style, stem, detailSpec.Resolution);
            using var process = Process.GetCurrentProcess();
            Console.WriteLine("peak_working_set_bytes=" + process.PeakWorkingSet64);
            return 0;
        }

        private static void DescribeDevelopment(RegionDevelopment development)
        {
            foreach ((string label, RegionPosition center) in development.Views())
                Console.WriteLine(FormattableString.Invariant($"{label}: x={center.X / 1000f:F3}km y={center.Y / 1000f:F3}km"));
            float length = 0f;
            for (int i = 1; i < development.Highway.Count; i++)
                length += development.Highway[i - 1].DistanceTo(development.Highway[i]);
            Console.WriteLine(FormattableString.Invariant($"highway={length / 1000f:F2}km city_blocks={development.Features.Count(f => f.Kind == RegionDevelopmentFeatureKind.Block)} rural_buildings={development.Features.Count(f => f.Kind == RegionDevelopmentFeatureKind.RuralRoof)}"));
        }

        private static void WriteDevelopmentDetails(RegionData data, RegionMapStyle style, string stem, int resolution)
        {
            var render = new RegionRenderOptions { Style = style, CloudCover = 0f };
            foreach ((string label, RegionPosition center) in data.Development.Views())
            {
                RegionDetail view = RegionDetailBuilder.Build(data, new RegionDetailSpec
                {
                    CenterMapX = center.X / (data.KmPerMapUnit * 1000f),
                    CenterMapY = center.Y / (data.KmPerMapUnit * 1000f),
                    SizeKm = 8f,
                    Resolution = resolution
                });
                int n = view.Data.Resolution;
                PngWriter.Write($"{stem}-{label}.png", RegionMapRenderer.Render(view.Data, render), n, n);
            }
        }

        private static string Json(string value)
        {
            return value == null ? "" : value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ");
        }

        /// <summary>
        /// 規則から付けた名前を、音の質感ごとに書き出す（骨格は同じで音だけが変わることを比べるための一覧）。
        /// 地域・エリア・川・峰・湾・地点のそれぞれに、付いた名前の全部（種類・骨格・由来）を入れる。
        /// </summary>
        private static void WriteNames(RegionData data, string stem)
        {
            RegionNameStyle originalStyle = data.Spec.NameStyle;
            string originalSound = data.Spec.NamingSound;
            RegionNamingRules rules = RegionNamingRules.Parse(data.Spec.NamingRulesText);
            var text = new System.Text.StringBuilder();
            text.Append("{\n  \"seed\": ").Append(data.Spec.Seed).Append(",\n  \"sounds\": [\n");
            var order = new System.Collections.Generic.List<string>();
            order.Add(string.IsNullOrEmpty(originalSound) ? rules.SoundName : originalSound);
            for (int s = 0; s < rules.Sounds.Count; s++)
            {
                if (!order.Contains(rules.Sounds[s].Name)) order.Add(rules.Sounds[s].Name);
            }
            for (int s = 0; s < order.Count; s++)
            {
                data.Spec.NamingSound = order[s];
                RegionNames.Apply(data, RegionNameStyle.Systematic);
                RegionNamingReport report = data.Naming;
                Console.WriteLine("音の質感 " + report.Sound + "（古い呼び名は " + report.OldSound + "）");
                for (int p = 0; p < report.Problems.Count; p++) Console.WriteLine("  問題 " + report.Problems[p]);
                text.Append("    {\"sound\": \"").Append(Json(report.Sound)).Append("\", \"oldSound\": \"").Append(Json(report.OldSound)).Append("\", \"problems\": [");
                for (int p = 0; p < report.Problems.Count; p++) text.Append(p > 0 ? ", " : "").Append("\"").Append(Json(report.Problems[p])).Append("\"");
                text.Append("],\n      \"roots\": [");
                AppendRoots(text, report.Roots);
                text.Append("],\n      \"oldRoots\": [");
                AppendRoots(text, report.OldRoots);
                text.Append("],\n      \"people\": [");
                for (int p = 0; p < report.People.Count; p++)
                {
                    text.Append(p > 0 ? ", " : "").Append("{\"name\": \"").Append(Json(report.People[p].Name)).Append("\", \"role\": \"").Append(Json(report.People[p].Role))
                        .Append("\", \"uses\": ").Append(report.People[p].Uses).Append("}");
                }
                text.Append("],\n      \"places\": [\n");
                bool any = false;
                for (int f = 0; f < data.Features.Count; f++)
                {
                    RegionFeature feature = data.Features[f];
                    if (feature.Kind != RegionFeatureKind.Region) continue;
                    AppendPlace(text, ref any, "地域", 0, "Region", feature.History, 0, feature.MapX, feature.MapY, "", feature.Names);
                }
                for (int a = 0; a < data.Areas.Count; a++)
                {
                    RegionArea area = data.Areas[a];
                    AppendPlace(text, ref any, "エリア", area.Id, area.Kind.ToString(), area.History, area.Id, area.LabelX, area.LabelY, Math.Round(area.AreaKm2) + " km²", area.Names);
                }
                for (int f = 0; f < data.Features.Count; f++)
                {
                    RegionFeature feature = data.Features[f];
                    if (feature.Kind == RegionFeatureKind.Region) continue;
                    string type = feature.Kind == RegionFeatureKind.River ? "川" : (feature.Kind == RegionFeatureKind.Peak ? "山" : "湾");
                    string size = feature.Kind == RegionFeatureKind.River ? "本流 約 " + Math.Round(feature.LengthKm) + " km"
                        : (feature.Kind == RegionFeatureKind.Peak ? Math.Round(feature.HeightMeters) + " m" : "約 " + Math.Round(feature.AreaKm2) + " km²");
                    AppendPlace(text, ref any, type, feature.Id, feature.Kind.ToString(), feature.History, feature.AreaId, feature.MapX, feature.MapY, size, feature.Names);
                }
                for (int k = 0; k < data.Sites.Count; k++)
                {
                    RegionSite site = data.Sites[k];
                    AppendPlace(text, ref any, "地点", site.Id, site.Kind.ToString(), site.History, site.AreaId, site.MapX, site.MapY, "順位 " + site.Rank + "/" + site.Candidates, site.Names);
                }
                text.Append("\n      ]}").Append(s + 1 < order.Count ? "," : "").Append("\n");
            }
            text.Append("  ]\n}\n");
            data.Spec.NamingSound = originalSound;
            RegionNames.Apply(data, originalStyle);
            File.WriteAllText(stem + "-names.json", text.ToString(), new System.Text.UTF8Encoding(false));
            Console.WriteLine("名前の一覧 → " + Path.GetFullPath(stem + "-names.json"));
        }

        private static void AppendRoots(System.Text.StringBuilder text, System.Collections.Generic.List<RegionNameRoot> roots)
        {
            for (int r = 0; r < roots.Count; r++)
            {
                text.Append(r > 0 ? ", " : "").Append("{\"meaning\": \"").Append(Json(roots[r].Meaning)).Append("\", \"sound\": \"").Append(Json(roots[r].Sound))
                    .Append("\", \"written\": ").Append(roots[r].Written ? "true" : "false").Append(", \"mood\": ").Append(roots[r].Mood ? "true" : "false")
                    .Append(", \"event\": ").Append(roots[r].Event ? "true" : "false").Append(", \"uses\": ").Append(roots[r].Uses).Append("}");
            }
        }

        private static void AppendPlace(System.Text.StringBuilder text, ref bool any, string type, int id, string kind, RegionLandHistory history, int area,
            float mapX, float mapY, string size, System.Collections.Generic.List<RegionPlaceName> names)
        {
            var invariant = System.Globalization.CultureInfo.InvariantCulture;
            text.Append(any ? ",\n" : "").Append("        {\"type\": \"").Append(type).Append("\", \"id\": ").Append(id).Append(", \"kind\": \"").Append(kind)
                .Append("\", \"history\": \"").Append(RegionNaming.HistoryLabel(history)).Append("\", \"area\": ").Append(area)
                .Append(", \"x\": ").Append(mapX.ToString("0.0", invariant)).Append(", \"y\": ").Append(mapY.ToString("0.0", invariant))
                .Append(", \"size\": \"").Append(Json(size)).Append("\", \"names\": [");
            any = true;
            var line = new System.Text.StringBuilder("  " + type + id.ToString("00") + " [" + RegionNaming.HistoryLabel(history) + "] ");
            for (int n = 0; n < names.Count; n++)
            {
                text.Append(n > 0 ? ", " : "").Append("{\"kind\": \"").Append(RegionNaming.KindLabel(names[n].Kind)).Append("\", \"text\": \"").Append(Json(names[n].Text))
                    .Append("\", \"skeleton\": \"").Append(Json(names[n].Skeleton)).Append("\", \"gloss\": \"").Append(Json(names[n].Gloss)).Append("\", \"origin\": \"").Append(Json(names[n].Origin)).Append("\"}");
                line.Append(n > 0 ? " ／ " : "").Append(names[n].Text);
            }
            text.Append("]}");
            if (names.Count > 0) line.Append("  ← ").Append(names[0].Skeleton);
            Console.WriteLine(line.ToString());
        }

        /// <summary>
        /// 指定した探索地点の周りを拡大して書き出す。地点ごとに、拡大した絵（-detail-sNN.png）と、
        /// 同じ範囲を広域の地図から切り出して引き伸ばした絵（-broad.png）を並べて比べられる。
        /// forms を付けると、構造物の形の案を 3 通り置いた絵も書き出す。最後に、窓の位置を示す広域の図を書き出す。
        /// </summary>
        private static void WriteDetails(RegionData data, RegionRenderOptions options, string stem, string list, string sizes,
            RegionDetailSpec detailSpec, bool forms, float formScale)
        {
            // 拡大した絵は雲を描かない。起伏の強調は広域より弱くする（広域の 2.6 倍のままだと影が実際の 2.6 倍に伸びる）
            var render = new RegionRenderOptions
            {
                Style = RegionMapStyle.Satellite,
                CloudCover = 0f,
                Exaggeration = options.Exaggeration,
                CastShadow = options.CastShadow,
                SunAltitude = options.SunAltitude,
            };
            int n = data.Resolution;
            byte[] ground = RegionMapRenderer.Render(data, new RegionRenderOptions { Style = RegionMapStyle.Satellite, CloudCover = 0f });
            var locator = (byte[])ground.Clone();
            var watch = new Stopwatch();

            foreach (string part in list.Split(','))
            foreach (string sizeText in sizes.Split(','))
            {
                int id = int.Parse(part);
                RegionSite site = data.Sites[id - 1];
                detailSpec.CenterMapX = site.MapX;
                detailSpec.CenterMapY = site.MapY;
                detailSpec.SizeKm = Float(sizeText);
                watch.Restart();
                RegionDetail detail = RegionDetailBuilder.Build(data, detailSpec);
                long buildMs = watch.ElapsedMilliseconds;
                int size = detail.Data.Resolution;
                watch.Restart();
                byte[] pixels = RegionMapRenderer.Render(detail.Data, render);
                long renderMs = watch.ElapsedMilliseconds;
                string file = string.Format(CultureInfo.InvariantCulture, "{0}-detail-s{1:00}-{2}km", stem, id, sizeText);
                PngWriter.Write(file + ".png", pixels, size, size);

                float low = float.MaxValue;
                float high = float.MinValue;
                for (int i = 0; i < detail.Data.Height.Length; i++)
                {
                    float value = detail.Data.Height[i];
                    if (value < low) low = value;
                    if (value > high) high = value;
                }
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "拡大 地点{0:00} {1} X{2:F1} Y{3:F1} 窓{4:F1}km {5}px（1px={6:F1}m） 作成={7}ms 描画={8}ms 標高{9:F0}〜{10:F0}m 海陸の一致={11:P1} 標高の差の平均={12:F1}m 川の一致={13:P0} → {14}",
                    id, site.Name, site.MapX, site.MapY, detailSpec.SizeKm, size, detail.Data.CellMeters, buildMs, renderMs, low, high,
                    detail.CoastAgreement, detail.MeanHeightDifference, detail.RiverAgreement, Path.GetFullPath(file + ".png")));

                // 同じ範囲を広域の地図から切り出す（引き伸ばすだけ。広域の 1 セルが四角に見える）
                var broad = new byte[size * size * 4];
                for (int y = 0; y < size; y++)
                {
                    int sy = Math.Min(n - 1, (int)((detail.OriginMapY + (y + 0.5f) / size * detail.SizeMap) / RegionData.MapUnits * n));
                    for (int x = 0; x < size; x++)
                    {
                        int sx = Math.Min(n - 1, (int)((detail.OriginMapX + (x + 0.5f) / size * detail.SizeMap) / RegionData.MapUnits * n));
                        Buffer.BlockCopy(ground, (sx + sy * n) * 4, broad, (x + y * size) * 4, 4);
                    }
                }
                PngWriter.Write(file + "-broad.png", broad, size, size);

                if (forms)
                {
                    Array all = Enum.GetValues(typeof(RegionStructureForm));
                    for (int f = 0; f < all.Length; f++)
                    {
                        var form = (RegionStructureForm)all.GetValue(f);
                        RegionDetail with = RegionDetailBuilder.Build(data, detailSpec);
                        RegionStructure placed = RegionStructureStamp.Place(with, site.MapX, site.MapY, site.Kind, form, formScale);
                        if (placed == null)
                        {
                            Console.WriteLine("  形 " + form + ": 置けない（海の上）");
                            continue;
                        }
                        string formFile = file + "-form-" + form.ToString().ToLowerInvariant() + ".png";
                        PngWriter.Write(formFile, RegionMapRenderer.Render(with.Data, render), size, size);
                        Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "  形 {0}: 差し渡し {1:F0}m 高さ {2:F0}m 足元の標高 {3:F0}m → {4}",
                            form, placed.SpanMeters, placed.HeightMeters, placed.BaseMeters, Path.GetFullPath(formFile)));
                    }
                }

                // 広域の図へ窓の枠を描く
                int x0 = (int)(detail.OriginMapX / RegionData.MapUnits * n);
                int y0 = (int)(detail.OriginMapY / RegionData.MapUnits * n);
                int span = Math.Max(2, (int)(detail.SizeMap / RegionData.MapUnits * n));
                for (int t = -3; t < span + 3; t++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        Plot(locator, n, x0 + t, y0 - 1 - k);
                        Plot(locator, n, x0 + t, y0 + span + k);
                        Plot(locator, n, x0 - 1 - k, y0 + t);
                        Plot(locator, n, x0 + span + k, y0 + t);
                    }
                }
            }
            PngWriter.Write(stem + "-detail-locator.png", locator, n, n);
            Console.WriteLine("窓の位置 → " + Path.GetFullPath(stem + "-detail-locator.png"));
        }

        private static void Plot(byte[] pixels, int n, int x, int y)
        {
            if (x < 0 || y < 0 || x >= n || y >= n) return;
            int p = (x + y * n) * 4;
            pixels[p] = 255;
            pixels[p + 1] = 236;
            pixels[p + 2] = 90;
        }

        private static readonly byte[][] AreaColors =
        {
            new byte[] { 230, 92, 80 }, new byte[] { 90, 170, 240 }, new byte[] { 250, 200, 70 }, new byte[] { 120, 210, 130 },
            new byte[] { 200, 130, 235 }, new byte[] { 250, 150, 70 }, new byte[] { 90, 215, 215 }, new byte[] { 240, 120, 180 },
            new byte[] { 170, 200, 80 }, new byte[] { 150, 150, 245 }, new byte[] { 235, 235, 235 }, new byte[] { 200, 160, 110 },
        };

        /// <summary>エリアごとに色を重ね、境界線を引いた図。沖は薄く、外洋は色を付けない。</summary>
        private static void WriteAreas(RegionData data, byte[] ground, string stem, int shrink)
        {
            int n = data.Resolution;
            var pixels = (byte[])ground.Clone();
            byte[] lines = RegionAreas.RenderBoundaries(data);
            for (int i = 0; i < n * n; i++)
            {
                int p = i * 4;
                int id = data.Area[i];
                if (id > 0 && data.Height[i] > 0f)
                {
                    byte[] c = AreaColors[(id - 1) % AreaColors.Length];
                    float t = 0.42f;
                    for (int ch = 0; ch < 3; ch++) pixels[p + ch] = (byte)(pixels[p + ch] * (1f - t) + c[ch] * t);
                }
                float line = lines[p + 3] / 255f;
                if (line > 0f)
                {
                    for (int ch = 0; ch < 3; ch++) pixels[p + ch] = (byte)(pixels[p + ch] * (1f - line) + 255f * line);
                }
            }
            MarkSites(pixels, n, data);
            int size = n;
            if (shrink > 1) pixels = Shrink(pixels, n, shrink, out size);
            PngWriter.Write(stem + "-areas.png", pixels, size, size);
            Console.WriteLine("エリアの図 → " + Path.GetFullPath(stem + "-areas.png"));
        }

        private static readonly byte[][] KindColors =
        {
            new byte[] { 255, 80, 70 },   // Monolith 山頂
            new byte[] { 255, 160, 40 },  // Arch 海岸
            new byte[] { 250, 230, 70 },  // Ring 乾燥地
            new byte[] { 90, 220, 110 },  // Tower 森や草原の丘
            new byte[] { 235, 245, 255 }, // Dome 雪原
            new byte[] { 70, 150, 255 },  // Terrace 川
            new byte[] { 200, 110, 250 }, // Sunken 浅い海
        };

        /// <summary>
        /// 探索地点の採点を地図にしたもの。暗くした地図の上に、その種類に合う所を色で塗り、合う度合いが高いほど濃くする。
        /// 色の無い所は、どの種類の規則にも当てはまらない。丸は実際に選ばれた地点。
        /// </summary>
        private static void WriteSuitability(RegionData data, byte[] ground, string stem, int shrink)
        {
            int n = data.Resolution;
            int kinds = KindColors.Length;
            var kindAt = new sbyte[n * n];
            var scoreAt = new float[n * n];
            var min = new float[kinds];
            var max = new float[kinds];
            var cells = new int[kinds];
            for (int k = 0; k < kinds; k++) { min[k] = float.MaxValue; max[k] = float.MinValue; }
            int margin = Math.Max(2, n / 28);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = x + y * n;
                    kindAt[i] = -1;
                    if (x < margin || y < margin || x >= n - margin || y >= n - margin) continue;
                    RegionSiteKind kind;
                    float score;
                    if (!RegionSiteGenerator.Rate(data, x, y, out kind, out score)) continue;
                    int k = (int)kind;
                    kindAt[i] = (sbyte)k;
                    scoreAt[i] = score;
                    cells[k]++;
                    if (score < min[k]) min[k] = score;
                    if (score > max[k]) max[k] = score;
                }
            }
            var pixels = new byte[n * n * 4];
            for (int i = 0; i < n * n; i++)
            {
                int p = i * 4;
                float gray = (ground[p] * 0.3f + ground[p + 1] * 0.5f + ground[p + 2] * 0.2f) * 0.5f + 12f;
                float r = gray, g = gray, b = gray + 6f;
                int k = kindAt[i];
                if (k >= 0)
                {
                    float t = max[k] > min[k] ? (scoreAt[i] - min[k]) / (max[k] - min[k]) : 1f;
                    float alpha = 0.18f + 0.72f * t * t;
                    r += (KindColors[k][0] - r) * alpha;
                    g += (KindColors[k][1] - g) * alpha;
                    b += (KindColors[k][2] - b) * alpha;
                }
                pixels[p] = (byte)r; pixels[p + 1] = (byte)g; pixels[p + 2] = (byte)b; pixels[p + 3] = 255;
            }
            for (int k = 0; k < kinds; k++)
            {
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "  採点 {0,-9} 当てはまるセル {1:P1}  値 {2:F2}〜{3:F2}",
                    (RegionSiteKind)k, cells[k] / (double)(n * n), cells[k] > 0 ? min[k] : 0f, cells[k] > 0 ? max[k] : 0f));
            }
            MarkSites(pixels, n, data, true);
            int size = n;
            if (shrink > 1) pixels = Shrink(pixels, n, shrink, out size);
            PngWriter.Write(stem + "-suitability.png", pixels, size, size);
            Console.WriteLine("採点の図 → " + Path.GetFullPath(stem + "-suitability.png"));
        }

        /// <summary>
        /// 雲の動きを連番の画像にする（探索画面と同じ計算：32×32 の升目の角でずれを求め、間をなめらかにつなぐ）。
        /// uniform が true の時は、前の動き（1 枚の雲を一様に流す）を同じ条件で書き出す。
        /// </summary>
        private static void WriteCloudFrames(RegionData data, RegionRenderOptions options, string stem, int frames, float step, int shrink, bool uniform)
        {
            int n = data.Resolution;
            float cover = options.CloudCover > 0f ? options.CloudCover : 0.58f;
            options.Style = RegionMapStyle.Satellite;
            options.CloudCover = 0f;
            byte[] ground = RegionMapRenderer.Render(data, options);
            options.CloudCover = cover;
            int size = n;
            if (shrink > 1) ground = Shrink(ground, n, shrink, out size);

            int cs = (n + 1) / 2;
            float[] low;
            float[] high;
            RegionClouds.BuildLayerFields(cs, data.Spec.Seed, cover, out low, out high);
            if (uniform)
            {
                low = RegionClouds.BuildField(cs, data.Spec.Seed, cover);
                high = new float[cs * cs];
            }
            byte[] lowPixels, lowShadow, highPixels, unused;
            RegionClouds.RenderLayers(low, cs, out lowPixels, out lowShadow);
            RegionClouds.RenderLayers(high, cs, out highPixels, out unused);

            const int grid = 32;
            const float cross = 420f;
            string dir = stem + (uniform ? "-clouds-before" : "-clouds");
            Directory.CreateDirectory(dir);
            var du = new float[2][];
            var dv = new float[2][];
            var fade = new float[2][];
            for (int l = 0; l < 2; l++)
            {
                du[l] = new float[(grid + 1) * (grid + 1)];
                dv[l] = new float[(grid + 1) * (grid + 1)];
                fade[l] = new float[(grid + 1) * (grid + 1)];
            }
            if (!uniform) PrintFlowStats(cross);
            var pixels = new byte[size * size * 4];
            for (int f = 0; f < frames; f++)
            {
                float time = f * step;
                for (int l = 0; l < 2; l++)
                {
                    for (int gy = 0; gy <= grid; gy++)
                    {
                        for (int gx = 0; gx <= grid; gx++)
                        {
                            int g = gx + gy * (grid + 1);
                            if (uniform)
                            {
                                RegionCloudFlow.Drift(RegionCloudFlow.Low, time, cross, out du[l][g], out dv[l][g]);
                                fade[l][g] = 1f;
                            }
                            else
                            {
                                RegionCloudFlow.Sample(l, gx / (float)grid, gy / (float)grid, time, cross, out du[l][g], out dv[l][g], out fade[l][g]);
                            }
                        }
                    }
                }
                System.Threading.Tasks.Parallel.For(0, size, y =>
                {
                    float v = 1f - (y + 0.5f) / size;
                    for (int x = 0; x < size; x++)
                    {
                        float u = (x + 0.5f) / size;
                        int p = (x + y * size) * 4;
                        float r = ground[p], g = ground[p + 1], b = ground[p + 2];
                        float ou, ov, of;
                        GridAt(du[0], dv[0], fade[0], grid, u, v, out ou, out ov, out of);
                        float shade = Texel(lowShadow, cs, u - ou - RegionClouds.ShadowOffset, v - ov + RegionClouds.ShadowOffset, 3) / 255f * of;
                        r *= 1f - shade; g *= 1f - shade; b = b * (1f - shade) + 8f * shade;
                        Over(lowPixels, cs, u - ou, v - ov, of, ref r, ref g, ref b);
                        GridAt(du[1], dv[1], fade[1], grid, u, v, out ou, out ov, out of);
                        Over(highPixels, cs, u - ou, v - ov, of, ref r, ref g, ref b);
                        pixels[p] = (byte)r; pixels[p + 1] = (byte)g; pixels[p + 2] = (byte)b; pixels[p + 3] = 255;
                    }
                });
                PngWriter.Write(Path.Combine(dir, string.Format(CultureInfo.InvariantCulture, "f-{0:0000}.png", f)), pixels, size, size);
            }
            Console.WriteLine("雲の連番 " + frames + " 枚（1 枚あたり " + step.ToString(CultureInfo.InvariantCulture) + " 秒） → " + Path.GetFullPath(dir));
        }

        /// <summary>
        /// 流れの数字を出す：場所ごとの速さの幅（一様な流れを 1 として）、向きの幅、絵の引き伸ばしの最大（隣り合う位置のずれの差）。
        /// </summary>
        private static void PrintFlowStats(float cross)
        {
            const int grid = 32;
            for (int layer = 0; layer < 2; layer++)
            {
                float baseU, baseV, nextU, nextV;
                RegionCloudFlow.Drift(layer, 100f, cross, out baseU, out baseV);
                RegionCloudFlow.Drift(layer, 101f, cross, out nextU, out nextV);
                double baseSpeed = Math.Sqrt((nextU - baseU) * (nextU - baseU) + (nextV - baseV) * (nextV - baseV));
                double baseAngle = Math.Atan2(nextV - baseV, nextU - baseU);
                double minSpeed = double.MaxValue, maxSpeed = 0, maxTurn = 0, maxStretch = 0, maxShift = 0, minFade = 1;
                for (int t = 0; t < 6; t++)
                {
                    float time = 100f + t * 37f;
                    for (int gy = 0; gy < grid; gy++)
                    {
                        for (int gx = 0; gx < grid; gx++)
                        {
                            float u = gx / (float)grid, v = gy / (float)grid;
                            float du0, dv0, f0, du1, dv1, f1, du2, dv2, f2, d0u, d0v;
                            RegionCloudFlow.Sample(layer, u, v, time, cross, out du0, out dv0, out f0);
                            RegionCloudFlow.Sample(layer, u, v, time + 1f, cross, out du1, out dv1, out f1);
                            RegionCloudFlow.Sample(layer, u + 1f / grid, v, time, cross, out du2, out dv2, out f2);
                            RegionCloudFlow.Drift(layer, time, cross, out d0u, out d0v);
                            double vu = du1 - du0, vv = dv1 - dv0;
                            if (Math.Abs(vu) > 0.5 || Math.Abs(vv) > 0.5) continue; // 一周して 0 へ戻る瞬間は数えない
                            double speed = Math.Sqrt(vu * vu + vv * vv) / baseSpeed;
                            double turn = Math.Abs(Math.Atan2(vv, vu) - baseAngle) * 180.0 / Math.PI;
                            if (turn > 180.0) turn = 360.0 - turn;
                            double stretch = Math.Sqrt((du2 - du0) * (du2 - du0) + (dv2 - dv0) * (dv2 - dv0)) * grid;
                            double shift = Math.Sqrt((du0 - d0u) * (du0 - d0u) + (dv0 - d0v) * (dv0 - d0v));
                            if (speed < minSpeed) minSpeed = speed;
                            if (speed > maxSpeed) maxSpeed = speed;
                            if (turn > maxTurn) maxTurn = turn;
                            if (stretch > maxStretch) maxStretch = stretch;
                            if (shift > maxShift) maxShift = shift;
                            if (f0 < minFade) minFade = f0;
                        }
                    }
                }
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "  流れ {0}: 速さ {1:F2}〜{2:F2} 倍、向きの振れ 最大 {3:F0} 度、渦のずれ 最大 {4:P1}、引き伸ばし 最大 {5:P0}、濃さ {6:F2}〜1",
                    layer == 0 ? "低い雲" : "高い雲", minSpeed, maxSpeed, maxTurn, maxShift, maxStretch, minFade));
            }
        }

        private static void GridAt(float[] du, float[] dv, float[] fade, int grid, float u, float v, out float ou, out float ov, out float of)
        {
            float fx = u * grid;
            float fy = v * grid;
            int x0 = Math.Min(grid - 1, Math.Max(0, (int)fx));
            int y0 = Math.Min(grid - 1, Math.Max(0, (int)fy));
            float tx = fx - x0;
            float ty = fy - y0;
            int a = x0 + y0 * (grid + 1);
            int c = a + grid + 1;
            ou = RegionNoise.Lerp(RegionNoise.Lerp(du[a], du[a + 1], tx), RegionNoise.Lerp(du[c], du[c + 1], tx), ty);
            ov = RegionNoise.Lerp(RegionNoise.Lerp(dv[a], dv[a + 1], tx), RegionNoise.Lerp(dv[c], dv[c + 1], tx), ty);
            of = RegionNoise.Lerp(RegionNoise.Lerp(fade[a], fade[a + 1], tx), RegionNoise.Lerp(fade[c], fade[c + 1], tx), ty);
        }

        /// <summary>層の絵を (u, v)（v は北が正、端は反対側へ回り込む）で読む。</summary>
        private static float Texel(byte[] layer, int size, float u, float v, int channel)
        {
            float fx = u * size - 0.5f;
            float fy = (1f - v) * size - 0.5f;
            int x0 = (int)Math.Floor(fx);
            int y0 = (int)Math.Floor(fy);
            float tx = fx - x0;
            float ty = fy - y0;
            x0 = ((x0 % size) + size) % size;
            y0 = ((y0 % size) + size) % size;
            int x1 = x0 + 1 < size ? x0 + 1 : 0;
            int y1 = y0 + 1 < size ? y0 + 1 : 0;
            float a = layer[(x0 + y0 * size) * 4 + channel];
            float b = layer[(x1 + y0 * size) * 4 + channel];
            float c = layer[(x0 + y1 * size) * 4 + channel];
            float d = layer[(x1 + y1 * size) * 4 + channel];
            return RegionNoise.Lerp(RegionNoise.Lerp(a, b, tx), RegionNoise.Lerp(c, d, tx), ty);
        }

        private static void Over(byte[] layer, int size, float u, float v, float opacity, ref float r, ref float g, ref float b)
        {
            float alpha = Texel(layer, size, u, v, 3) / 255f * opacity;
            if (alpha <= 0f) return;
            r += (Texel(layer, size, u, v, 0) - r) * alpha;
            g += (Texel(layer, size, u, v, 1) - g) * alpha;
            b += (Texel(layer, size, u, v, 2) - b) * alpha;
        }

        private static string DescribeJson(RegionData data)
        {
            var text = new System.Text.StringBuilder();
            text.Append("{\n  \"seed\": ").Append(data.Spec.Seed).Append(",\n  \"areas\": [\n");
            for (int a = 0; a < data.Areas.Count; a++)
            {
                RegionArea area = data.Areas[a];
                text.Append(string.Format(CultureInfo.InvariantCulture,
                    "    {{\"id\": {0}, \"name\": \"{1}\", \"kind\": \"{2}\", \"basis\": \"{3}\", \"x\": {4:F2}, \"y\": {5:F2}, \"km2\": {6:F0}, \"mean\": {7:F0}, \"max\": {8:F0}, \"sites\": {9}}}{10}\n",
                    area.Id, area.Name, area.Kind, area.Basis, area.LabelX, area.LabelY, area.AreaKm2, area.MeanHeight, area.MaxHeight, area.SiteCount,
                    a + 1 < data.Areas.Count ? "," : ""));
            }
            text.Append("  ],\n  \"sites\": [\n");
            for (int s = 0; s < data.Sites.Count; s++)
            {
                RegionSite site = data.Sites[s];
                text.Append(string.Format(CultureInfo.InvariantCulture,
                    "    {{\"id\": {0}, \"name\": \"{1}\", \"kind\": \"{2}\", \"x\": {3:F2}, \"y\": {4:F2}, \"height\": {5:F0}, \"area\": {6}, \"score\": {7:F3}, \"rank\": {8}, \"candidates\": {9}, \"reason\": \"{10}\"}}{11}\n",
                    site.Id, site.Name, site.Kind, site.MapX, site.MapY, site.HeightMeters, site.AreaId, site.Score, site.Rank, site.Candidates, site.Reason,
                    s + 1 < data.Sites.Count ? "," : ""));
            }
            text.Append("  ]\n}\n");
            return text.ToString();
        }

        /// <summary>
        /// 地形を作らずに、雲の層だけを 2×2 に並べて書き出す。量と、端のつながり（継ぎ目が見えないこと）を確かめる。
        /// 左半分は海の色、右半分は陸の色の上に重ねる。
        /// </summary>
        private static int WriteCloudTile(RegionSpec spec, RegionRenderOptions options, string outDir, string name)
        {
            int size = spec.Resolution / 2;
            var watch = Stopwatch.StartNew();
            float[] field = RegionClouds.BuildField(size, spec.Seed, options.CloudCover);
            byte[] clouds;
            byte[] shadows;
            RegionClouds.RenderLayers(field, size, out clouds, out shadows);
            long ms = watch.ElapsedMilliseconds;
            int n = size * 2;
            var pixels = new byte[n * n * 4];
            int offset = (int)(RegionClouds.ShadowOffset * size);
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float r = x < n / 2 ? 14f : 62f;
                    float g = x < n / 2 ? 40f : 84f;
                    float b = x < n / 2 ? 74f : 52f;
                    int sx = ((x - offset) % size + size) % size;
                    int sy = ((y - offset) % size + size) % size;
                    float dim = 1f - shadows[(sx + sy * size) * 4 + 3] / 255f;
                    r *= dim; g *= dim; b *= dim;
                    int c = (x % size + (y % size) * size) * 4;
                    float alpha = clouds[c + 3] / 255f;
                    r += (clouds[c] - r) * alpha;
                    g += (clouds[c + 1] - g) * alpha;
                    b += (clouds[c + 2] - b) * alpha;
                    int p = (x + y * n) * 4;
                    pixels[p] = (byte)r; pixels[p + 1] = (byte)g; pixels[p + 2] = (byte)b; pixels[p + 3] = 255;
                }
            }
            string file = Path.Combine(outDir, (name ?? ("clouds-seed" + spec.Seed)) + "-tile.png");
            PngWriter.Write(file, pixels, n, n);
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "雲 seed={0} 量={1:F2} 作成={2}ms 薄い雲を含む割合={3:P0} はっきり白い割合={4:P0} → {5}",
                spec.Seed, options.CloudCover, ms, RegionClouds.Coverage(field, 0.05f), RegionClouds.Coverage(field, 0.25f), Path.GetFullPath(file)));
            return 0;
        }

        private static float Float(string value)
        {
            return float.Parse(value, CultureInfo.InvariantCulture);
        }

        private static void MarkSites(byte[] pixels, int n, RegionData data, bool byKind = false)
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
                        // 採点の図では白い丸（色は下の塗りが示す）。ほかの図では赤い丸
                        pixels[p] = 255;
                        pixels[p + 1] = byKind ? (byte)255 : (byte)70;
                        pixels[p + 2] = byKind ? (byte)255 : (byte)60;
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
