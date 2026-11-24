using System;
using System.Collections.Generic;

namespace Vastcore.RegionMap
{
    /// <summary>施設と街区の面、道路の中心線を、広域・拡大共通の座標から画像へ描く。</summary>
    internal sealed class RegionDevelopmentRendering
    {
        private readonly byte[] _pixels;
        private readonly int _size;
        private readonly float _cell;
        private readonly RegionPosition _origin;

        private RegionDevelopmentRendering(RegionData data)
        {
            _size = data.Resolution;
            _pixels = new byte[_size * _size * 4];
            _cell = data.CellMeters;
            _origin = data.DevelopmentOrigin;
        }

        internal static byte[] Draw(RegionData data)
        {
            RegionDevelopment plan = data.Development;
            if (plan == null) return null;
            var layer = new RegionDevelopmentRendering(data);
            if (data.CellMeters <= RegionDevelopmentSiteRendering.MaxCellMeters)
            {
                // 写真の縮尺：街区とその建物・道路は細部の描画へ任せ、空港と郊外だけ従来の面で描いて下に敷く
                bool inBlock = false;
                foreach (RegionDevelopmentFeature feature in plan.Features)
                {
                    if (feature.Kind == RegionDevelopmentFeatureKind.Block) { inBlock = true; continue; }
                    if (feature.Kind != RegionDevelopmentFeatureKind.Roof) inBlock = false;
                    else if (inBlock) continue;
                    layer.DrawFeature(feature);
                }
                byte[] site = RegionDevelopmentSiteRendering.Draw(data);
                for (int p = 0; p < site.Length; p += 4)
                {
                    int keep = 255 - site[p + 3];
                    for (int c = 0; c < 4; c++)
                        layer._pixels[p + c] = (byte)Math.Min(255, site[p + c] + (layer._pixels[p + c] * keep + 127) / 255);
                }
                return layer._pixels;
            }

            foreach (RegionDevelopmentFeature feature in plan.Features)
                layer.DrawFeature(feature);

            bool wide = data.CellMeters > 38f;
            if (!wide)
            {
                foreach (var street in plan.Streets)
                {
                    layer.Line(street.Start, street.End, street.Width + 7f, 86f, 85f, 77f, 0.78f);
                    layer.Line(street.Start, street.End, street.Width, 147f, 143f, 127f, 0.91f);
                }
            }
            layer.DrawRoad(plan.Highway, 38f, wide ? 1.45f : 0f);
            layer.DrawRoad(plan.AirportAccess, 20f, wide ? 0.86f : 0f);
            return layer._pixels;
        }

        private void DrawFeature(RegionDevelopmentFeature feature)
        {
            float variation = RegionNoise.Hash01((int)feature.Center.X, (int)feature.Center.Y, 19473u);
            switch (feature.Kind)
            {
                case RegionDevelopmentFeatureKind.Block:
                    Rect(feature.Center, feature.LengthMeters, feature.WidthMeters, feature.Angle,
                        119f + variation * 46f, 119f + variation * 39f, 112f + variation * 40f, 0.86f);
                    break;
                case RegionDevelopmentFeatureKind.AirportGround:
                    Rect(feature.Center, feature.LengthMeters, feature.WidthMeters, feature.Angle, 109f, 128f, 94f, 0.89f);
                    break;
                case RegionDevelopmentFeatureKind.Runway:
                    Rect(feature.Center, feature.LengthMeters, Math.Max(feature.WidthMeters, _cell * 1.08f), feature.Angle, 66f, 70f, 74f, 0.99f);
                    if (_cell < 38f)
                    {
                        for (float distance = -1550f; distance <= 1550f; distance += 175f)
                            Rect(feature.Offset(distance, 0f), 50f, 5f, feature.Angle, 223f, 224f, 215f, 0.92f);
                        Rect(feature.Offset(-1640f, 0f), 17f, 60f, feature.Angle, 220f, 221f, 218f, 0.94f);
                        Rect(feature.Offset(1640f, 0f), 17f, 60f, feature.Angle, 220f, 221f, 218f, 0.94f);
                    }
                    break;
                case RegionDevelopmentFeatureKind.Apron:
                    Rect(feature.Center, feature.LengthMeters, feature.WidthMeters, feature.Angle, 148f, 149f, 145f, 0.92f);
                    break;
                case RegionDevelopmentFeatureKind.RuralLot:
                    Rect(feature.Center, feature.LengthMeters, feature.WidthMeters, feature.Angle, 146f, 137f, 111f, 0.71f);
                    break;
                default:
                    if (_cell < 70f)
                    {
                        var shadow = new RegionPosition(feature.Center.X + 17f, feature.Center.Y + 21f);
                        Rect(shadow, feature.LengthMeters, feature.WidthMeters, feature.Angle, 51f, 58f, 55f, 0.68f);
                    }
                    Rect(feature.Center, feature.LengthMeters, feature.WidthMeters, feature.Angle,
                        150f + 71f * variation, 126f + 78f * variation, 112f + 83f * variation, 0.97f);
                    break;
            }
        }

        private void DrawRoad(IReadOnlyList<RegionPosition> points, float width, float minPixels)
        {
            float fill = Math.Max(width, minPixels * _cell);
            float border = fill + Math.Max(10f, 0.72f * _cell);
            for (int i = 1; i < points.Count; i++) Line(points[i - 1], points[i], border, 56f, 62f, 56f, 0.9f);
            for (int i = 1; i < points.Count; i++) Line(points[i - 1], points[i], fill, 193f, 187f, 159f, 0.94f);
        }

        private void Rect(RegionPosition center, float length, float width, float angle, float r, float g, float b, float opacity)
        {
            float halfLength = length * 0.5f;
            float halfWidth = width * 0.5f;
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            float padX = halfLength * Math.Abs(cos) + halfWidth * Math.Abs(sin);
            float padY = halfLength * Math.Abs(sin) + halfWidth * Math.Abs(cos);
            int x0 = Lower(center.X - padX, _origin.X), x1 = Upper(center.X + padX, _origin.X);
            int y0 = Lower(center.Y - padY, _origin.Y), y1 = Upper(center.Y + padY, _origin.Y);
            // 4つの点で画素内の面積を見積もり、離れた建物も面積に応じて薄く写す。
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                int hits = 0;
                for (int s = 0; s < 4; s++)
                {
                    float dx = _origin.X + (x + 0.25f + 0.5f * (s & 1)) * _cell - center.X;
                    float dy = _origin.Y + (y + 0.25f + 0.5f * (s >> 1)) * _cell - center.Y;
                    if (Math.Abs(dx * cos + dy * sin) <= halfLength && Math.Abs(dy * cos - dx * sin) <= halfWidth) hits++;
                }
                if (hits != 0) Blend(x, y, r, g, b, opacity * hits * 0.25f);
            }
        }

        private void Line(RegionPosition a, RegionPosition b, float width, float r, float g, float blue, float opacity)
        {
            float radius = width * 0.5f;
            float dx = b.X - a.X, dy = b.Y - a.Y;
            float lengthSquared = dx * dx + dy * dy;
            int x0 = Lower(Math.Min(a.X, b.X) - radius, _origin.X), x1 = Upper(Math.Max(a.X, b.X) + radius, _origin.X);
            int y0 = Lower(Math.Min(a.Y, b.Y) - radius, _origin.Y), y1 = Upper(Math.Max(a.Y, b.Y) + radius, _origin.Y);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float px = _origin.X + (x + 0.5f) * _cell - a.X;
                float py = _origin.Y + (y + 0.5f) * _cell - a.Y;
                float t = RegionNoise.Clamp01((px * dx + py * dy) / Math.Max(1f, lengthSquared));
                float u = px - t * dx, v = py - t * dy;
                float coverage = RegionNoise.Clamp01(0.5f + (radius - (float)Math.Sqrt(u * u + v * v)) / _cell);
                if (coverage > 0f) Blend(x, y, r, g, blue, opacity * coverage);
            }
        }

        private int Lower(float position, float origin) => Math.Max(0, (int)Math.Floor((position - origin) / _cell - 0.5f));
        private int Upper(float position, float origin) => Math.Min(_size - 1, (int)Math.Ceiling((position - origin) / _cell + 0.5f));

        private void Blend(int x, int y, float r, float g, float b, float alpha)
        {
            int p = (x + y * _size) * 4;
            float keep = 1f - alpha;
            _pixels[p] = (byte)(r * alpha + _pixels[p] * keep + 0.5f);
            _pixels[p + 1] = (byte)(g * alpha + _pixels[p + 1] * keep + 0.5f);
            _pixels[p + 2] = (byte)(b * alpha + _pixels[p + 2] * keep + 0.5f);
            _pixels[p + 3] = (byte)(255f * alpha + _pixels[p + 3] * keep + 0.5f);
        }
    }
}
