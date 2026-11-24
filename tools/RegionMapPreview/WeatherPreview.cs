using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;

namespace Vastcore.RegionMap.Preview
{
    /// <summary>SP-021 の広域図で、雲と地表の天気に同じ風と水分を使う試作。</summary>
    internal sealed class WeatherPreview
    {
        private const float RainThreshold = 0.16f;
        private const int TextureSize = 384;

        private readonly RegionData _region;
        private readonly float[] _low;
        private readonly float[] _high;
        private readonly Vector2 _prevailingWind;

        private WeatherPreview(RegionData region)
        {
            _region = region;
            // 比較用として西寄りの風にし、地域ごとに角度を変える。座標は東・南が正。
            uint seed = unchecked((uint)region.Spec.Seed);
            float angle = (seed * 2654435761u % 101u / 100f - 0.5f) * 0.65f;
            _prevailingWind = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            RegionClouds.BuildLayerFields(TextureSize, region.Spec.Seed, 0.64f, out _low, out _high);
        }

        public static int Write(RegionData region, string stem, int frames)
        {
            var preview = new WeatherPreview(region);
            var options = new RegionRenderOptions { Style = RegionMapStyle.Satellite, CloudCover = 0f };
            byte[] ground = RegionMapRenderer.Render(region, options);
            int size = region.Resolution;
            var pixels = new byte[ground.Length * 2];
            using var report = new StreamWriter(stem + "-weather.csv");
            report.WriteLine("scenario,progress,rain_area_percent,cloud_area_percent,upslope_cloud,downslope_cloud,eye_cloud,eyewall_cloud,eye_rain,eyewall_rain,center_x,center_y,point_x,point_y,point_height_m,point_cloud,point_rain,point_wind,prevailing_east,prevailing_south");
            var trackPoint = new Vector2(0.50f, 0.51f);
            preview.Render("prevailing", preview._prevailingWind, false, 0f, ground, pixels, report, trackPoint);
            PngWriter.Write(stem + "-prevailing.png", pixels, size * 2, size);
            preview.Render("reversed", -preview._prevailingWind, false, 0f, ground, pixels, report, trackPoint);
            PngWriter.Write(stem + "-reversed.png", pixels, size * 2, size);

            string directory = stem + "-storm";
            Directory.CreateDirectory(directory);
            for (int frame = 0; frame < frames; frame++)
            {
                float progress = frames == 1 ? 0.5f : frame / (float)(frames - 1);
                preview.Render("storm", preview._prevailingWind, true, progress, ground, pixels, report, trackPoint);
                PngWriter.Write(Path.Combine(directory, $"{frame:D4}.png"), pixels, size * 2, size);
            }
            Console.WriteLine("雲と地上の天気 → " + Path.GetFullPath(stem + "-weather.csv"));
            return 0;
        }

        private void Render(string scenario, Vector2 prevailing, bool storm, float progress,
            byte[] ground, byte[] rendered, StreamWriter report, Vector2 trackPoint)
        {
            int size = _region.Resolution;
            var center = new Vector2(-0.10f + 1.20f * progress, 0.60f - 0.18f * progress);
            var samples = new Weather[size * size];
            Parallel.For(0, size, y =>
            {
                for (int x = 0; x < size; x++)
                {
                    var position = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                    samples[x + y * size] = Sample(position, prevailing, storm, center, progress);
                }
            });

            Parallel.For(0, size, y =>
            {
                for (int x = 0; x < size; x++)
                {
                    int index = x + y * size;
                    int cloudPixel = 4 * (x + y * size * 2);
                    Weather weather = samples[index];
                    float relief = samples[Math.Min(x + 2, size - 1) + Math.Min(y + 2, size - 1) * size].Cloud
                        - samples[Math.Max(x - 2, 0) + Math.Max(y - 2, 0) * size].Cloud;
                    float tone = Math.Clamp(218f + weather.Cloud * 34f + relief * 65f, 175f, 255f);
                    float alpha = RegionNoise.Smooth(0.10f, 0.85f, weather.Cloud) * 0.91f;
                    Blend(ground, rendered, index * 4, cloudPixel, tone, alpha);
                    DrawRain(ground, rendered, index, cloudPixel + size * 4, weather.Rain);
                }
            });

            WriteStats(scenario, progress, storm, center, prevailing, trackPoint, samples, report);
        }

        private Weather Sample(Vector2 position, Vector2 prevailing, bool storm, Vector2 center, float progress)
        {
            Vector2 delta = position - center;
            float radius = delta.Length();
            float stormWeight = storm ? 1f - RegionNoise.Smooth(0.21f, 0.43f, radius) : 0f;
            Vector2 radial = radius > 0f ? delta / radius : Vector2.Zero;
            Vector2 tangent = new Vector2(radial.Y, -radial.X);
            float circulation = stormWeight * RegionNoise.Smooth(0.012f, 0.065f, radius);
            Vector2 wind = prevailing * (1f - stormWeight) + (tangent * 2.7f - radial * 0.65f) * circulation;
            float speed = wind.Length();
            Vector2 direction = speed > 0.01f ? wind / speed : prevailing;

            float distance = 5f / _region.SizeKm;
            float height = GroundHeight(position);
            var crosswind = new Vector2(-direction.Y, direction.X);
            float sideSlope = (GroundHeight(position + crosswind * distance) - GroundHeight(position - crosswind * distance)) / 1000f;
            // 下層の風は横に急な斜面があるほど低い側へ回り込む。
            direction = Vector2.Normalize(direction - crosswind * Math.Clamp(sideSlope, -0.55f, 0.55f));
            float upwindHeight = GroundHeight(position - direction * distance);
            float downwindHeight = GroundHeight(position + direction * distance);
            float lift = Math.Clamp((downwindHeight - upwindHeight) / 630f, -1f, 1f);
            int local = Cell(position);
            int upwind = Cell(position - direction * distance * 2.5f);
            float moisture = 0.60f * Moisture(local) + 0.40f * Moisture(upwind);

            // 地形側の雲の増減は地図に固定し、雲の細かな模様は風に沿って動かす。
            Vector2 source = position - direction * (0.025f + 0.065f * progress);
            float texture = RegionClouds.Sample(_low, TextureSize, source.X * TextureSize, source.Y * TextureSize);
            float high = RegionClouds.Sample(_high, TextureSize, source.X * TextureSize, source.Y * TextureSize);
            float low = RegionNoise.Clamp01(texture * 0.72f + (moisture - 0.52f) * 0.42f
                + Math.Max(lift, 0f) * (0.30f + 0.30f * moisture) - Math.Max(-lift, 0f) * 0.29f);
            float rain = RegionNoise.Smooth(0.36f, 0.85f, low + moisture * 0.16f)
                * RegionNoise.Smooth(0.38f, 0.78f, moisture + Math.Max(lift, 0f) * 0.19f);
            float cloud = Math.Max(low, high);

            if (stormWeight > 0f)
            {
                float angle = MathF.Atan2(delta.Y, delta.X);
                float rotation = progress * (7.5f - 7f * radius);
                float phase = (angle + rotation + 3.1f * MathF.Log(Math.Max(0.025f, radius) / 0.09f)) * 2f;
                float bands = RegionNoise.Smooth(0.12f, 0.82f, MathF.Cos(phase));
                float ringOffset = 0.006f * MathF.Sin(3f * (angle + rotation));
                float ring = 1f - RegionNoise.Smooth(0.009f, 0.032f, MathF.Abs(radius - 0.073f + ringOffset));
                float eyeMask = RegionNoise.Smooth(0.030f, 0.056f, radius);
                float stormCloud = (0.13f + Math.Max(bands * 0.64f, ring * 0.86f)) * (0.82f + texture * 0.27f);
                cloud = RegionNoise.Lerp(cloud, Math.Max(cloud * 0.6f, stormCloud) * eyeMask, stormWeight);
                float stormRain = Math.Max(ring * 0.89f, bands * 0.62f) + Math.Max(lift, 0f) * 0.20f;
                rain = RegionNoise.Lerp(rain, RegionNoise.Clamp01(stormRain) * eyeMask, stormWeight);
            }
            return new Weather(cloud, rain, lift, speed, height);
        }

        private void WriteStats(string scenario, float progress, bool storm, Vector2 center, Vector2 prevailing,
            Vector2 trackPoint, Weather[] samples, StreamWriter report)
        {
            int rains = 0, cloudy = 0;
            int rises = 0, falls = 0, eye = 0, ring = 0;
            double risingCloud = 0, fallingCloud = 0, eyeCloud = 0, ringCloud = 0, eyeRain = 0, ringRain = 0;
            int size = _region.Resolution;
            for (int i = 0; i < samples.Length; i++)
            {
                Weather weather = samples[i];
                if (weather.Rain >= RainThreshold) rains++;
                if (weather.Cloud >= 0.35f) cloudy++;
                if (weather.Height > 100f && weather.Lift > 0.3f) { rises++; risingCloud += weather.Cloud; }
                if (weather.Height > 100f && weather.Lift < -0.3f) { falls++; fallingCloud += weather.Cloud; }
                var position = new Vector2((i % size + 0.5f) / size, (i / size + 0.5f) / size);
                float radius = Vector2.Distance(position, center);
                if (storm && radius < 0.03f) { eye++; eyeCloud += weather.Cloud; eyeRain += weather.Rain; }
                if (storm && MathF.Abs(radius - 0.073f) < 0.012f) { ring++; ringCloud += weather.Cloud; ringRain += weather.Rain; }
            }
            Weather point = samples[Cell(trackPoint)];
            string stormCenter = storm ? FormattableString.Invariant($"{center.X:F4},{center.Y:F4}") : ",";
            report.WriteLine(FormattableString.Invariant($"{scenario},{progress:F4},{100d * rains / samples.Length:F2},{100d * cloudy / samples.Length:F2},{Mean(risingCloud, rises)},{Mean(fallingCloud, falls)},{Mean(eyeCloud, eye)},{Mean(ringCloud, ring)},{Mean(eyeRain, eye)},{Mean(ringRain, ring)},{stormCenter},{trackPoint.X:F4},{trackPoint.Y:F4},{point.Height:F1},{point.Cloud:F3},{point.Rain:F3},{point.Wind:F3},{prevailing.X:F3},{prevailing.Y:F3}"));
        }

        private float Moisture(int cell) => _region.IsLand(cell) ? _region.Moisture[cell] : 0.84f;

        private int Cell(Vector2 position) => _region.CellFromMap(position.X * RegionData.MapUnits, position.Y * RegionData.MapUnits);

        private float GroundHeight(Vector2 position) => Math.Max(0f, _region.Height[Cell(position)]);

        private static string Mean(double sum, int count) => count == 0 ? "" : (sum / count).ToString("F3", CultureInfo.InvariantCulture);

        private static void Blend(byte[] ground, byte[] output, int source, int destination, float tone, float alpha)
        {
            for (int channel = 0; channel < 3; channel++)
                output[destination + channel] = (byte)RegionNoise.Lerp(ground[source + channel], tone, alpha);
            output[destination + 3] = 255;
        }

        private static void DrawRain(byte[] ground, byte[] output, int index, int pixel, float rain)
        {
            int source = index * 4;
            float alpha = RegionNoise.Smooth(0.10f, 0.85f, rain) * 0.93f;
            output[pixel] = (byte)RegionNoise.Lerp(ground[source] * 0.78f, 36f, alpha);
            output[pixel + 1] = (byte)RegionNoise.Lerp(ground[source + 1] * 0.78f, 183f, alpha);
            output[pixel + 2] = (byte)RegionNoise.Lerp(ground[source + 2] * 0.78f, 255f, alpha);
            output[pixel + 3] = 255;
        }

        private readonly struct Weather
        {
            public readonly float Cloud, Rain, Lift, Wind, Height;

            public Weather(float cloud, float rain, float lift, float wind, float height)
            {
                Cloud = cloud;
                Rain = rain;
                Lift = lift;
                Wind = wind;
                Height = height;
            }
        }
    }
}
