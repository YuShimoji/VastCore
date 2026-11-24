using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 1 画素が 3 m より細かい窓で、街区の中を敷地・建物・屋根・樹木・車・路面の線まで描く。
    /// 配置（RegionDevelopment）は街区と 90 m 級の建物の枠までしか持たないので、その枠の中身を
    /// 世界座標のハッシュから毎回同じ形に展開する。窓の位置や大きさを変えても同じ場所に同じ物が出る。
    /// 色と一緒に物の高さを書き込み、最後に太陽の向きへ辿って影を落とす。
    /// </summary>
    internal sealed class RegionDevelopmentSiteRendering
    {
        internal const float MaxCellMeters = 3f;

        // 太陽へ向かう水平の向き（従来の落ち影のずれ +17, +21 m と同じ向き）と、高度 38 度の正接
        private const float SunX = -0.629f;
        private const float SunY = -0.777f;
        private const float SunTan = 0.78f;

        // RegionDevelopmentBuilder.AddCity が 1 街区に 3×3 で置く建物の枠の間隔
        private const float SlotU = 154f;
        private const float SlotV = 144f;
        private const float LaneHalf = 3.5f;
        private const float Walk = 2f;

        private enum Surface { Plain, Asphalt, Paving, Grass, Dirt, FlatRoof, GableRoof, HipRoof, RibbedRoof }

        private struct Frame
        {
            public float Ox, Oy, Cos, Sin;
        }

        private struct Road
        {
            public RegionPosition A, B;
            public float Width;
            public bool Highway;
        }

        private static readonly float[] FlatRoofColors =
        {
            176, 176, 172,   142, 144, 148,   204, 202, 196,   116, 113, 110,   152, 138, 124,   188, 190, 196,
        };

        private static readonly float[] HouseRoofColors =
        {
            146, 98, 82,   92, 92, 96,   124, 104, 88,   104, 112, 122,   172, 166, 156,   128, 82, 70,   78, 84, 80,
        };

        private static readonly float[] ShedRoofColors =
        {
            190, 194, 198,   152, 162, 174,   174, 152, 130,   212, 212, 206,   120, 140, 132,
        };

        private static readonly float[] CarColors =
        {
            226, 226, 224,   42, 44, 48,   164, 166, 170,   150, 44, 38,   52, 72, 122,   112, 114, 116,   196, 188, 160,
        };

        private readonly int _n;
        private readonly int _pad;
        private readonly int _m;
        private readonly float _cell;
        private readonly float _ox;
        private readonly float _oy;
        private readonly uint _seed;
        private readonly float[] _r;
        private readonly float[] _g;
        private readonly float[] _b;
        private readonly float[] _a;
        private readonly float[] _h;
        private readonly List<Road> _roads = new List<Road>();
        private float _maxHeight;

        private RegionDevelopmentSiteRendering(RegionData data)
        {
            _n = data.Resolution;
            _cell = data.CellMeters;
            // 窓の外の建物が窓の中へ落とす影のために、外側へ余白を取る
            _pad = Math.Min(192, (int)Math.Ceiling(150f / _cell));
            _m = _n + _pad * 2;
            _ox = data.DevelopmentOrigin.X - _pad * _cell;
            _oy = data.DevelopmentOrigin.Y - _pad * _cell;
            _seed = unchecked((uint)data.Spec.Seed * 747796405u + 2891336453u);
            int count = _m * _m;
            _r = new float[count];
            _g = new float[count];
            _b = new float[count];
            _a = new float[count];
            _h = new float[count];
        }

        /// <summary>街区・街路・幹線を描いた RGBA（色は不透明度を掛けた値）。従来の層の上に重ねる。</summary>
        internal static byte[] Draw(RegionData data)
        {
            RegionDevelopment plan = data.Development;
            var site = new RegionDevelopmentSiteRendering(data);
            site.CollectRoads(plan);
            site.DrawBlocks(plan);
            site.DrawRoads();
            return site.Resolve(site.CastShadows());
        }

        // ---- 街区 ----

        private void DrawBlocks(RegionDevelopment plan)
        {
            RegionDevelopmentFeature? block = null;
            var roofs = new RegionDevelopmentFeature?[9];
            foreach (RegionDevelopmentFeature feature in plan.Features)
            {
                if (feature.Kind == RegionDevelopmentFeatureKind.Block)
                {
                    if (block.HasValue) DrawBlock(plan, block.Value, roofs);
                    block = feature;
                    Array.Clear(roofs, 0, roofs.Length);
                }
                else if (feature.Kind == RegionDevelopmentFeatureKind.Roof && block.HasValue)
                {
                    Frame f = MakeFrame(block.Value.Center, block.Value.Angle);
                    ToLocal(f, feature.Center.X, feature.Center.Y, out float u, out float v);
                    int ix = Math.Max(0, Math.Min(2, (int)Math.Round(u / SlotU) + 1));
                    int iy = Math.Max(0, Math.Min(2, (int)Math.Round(v / SlotV) + 1));
                    roofs[ix + iy * 3] = feature;
                }
                else
                {
                    if (block.HasValue) DrawBlock(plan, block.Value, roofs);
                    block = null;
                }
            }
            if (block.HasValue) DrawBlock(plan, block.Value, roofs);
        }

        private void DrawBlock(RegionDevelopment plan, RegionDevelopmentFeature block, RegionDevelopmentFeature?[] roofs)
        {
            if (!Near(block.Center, 440f)) return;
            Frame f = MakeFrame(block.Center, block.Angle);
            float hl = block.LengthMeters * 0.5f;
            float hw = block.WidthMeters * 0.5f;

            // 中心からの近さで、塔・中層・戸建ての割合を変える
            float toPrimary = block.Center.DistanceTo(plan.PrimaryCity) / RegionDevelopmentBuilder.PrimaryRadius;
            float toSecondary = block.Center.DistanceTo(plan.SecondaryCity) / RegionDevelopmentBuilder.SecondaryRadius;
            float radial = Math.Min(toPrimary, toSecondary);

            // 街区の外の帯（街路との間）、街区の舗装、街区の中の細い道
            Rect(f, -hl - 30f, -hw - 43f, hl + 30f, hw + 43f, Surface.Grass, 102f, 112f, 70f, 0f);
            Rect(f, -hl, -hw, hl, hw, Surface.Paving, 158f, 156f, 149f, 0f);
            for (int k = -1; k <= 1; k += 2)
            {
                Rect(f, k * SlotU * 0.5f - LaneHalf, -hw + 2f, k * SlotU * 0.5f + LaneHalf, hw - 2f, Surface.Asphalt, 76f, 78f, 81f, 0f);
                Rect(f, -hl + 2f, k * SlotV * 0.5f - LaneHalf, hl - 2f, k * SlotV * 0.5f + LaneHalf, Surface.Asphalt, 76f, 78f, 81f, 0f);
            }

            float inner = LaneHalf + Walk;
            for (int iy = 0; iy < 3; iy++)
            for (int ix = 0; ix < 3; ix++)
            {
                float u0 = ix == 0 ? -hl + 3.5f : (ix - 1.5f) * SlotU + inner;
                float u1 = ix == 2 ? hl - 3.5f : (ix - 0.5f) * SlotU - inner;
                float v0 = iy == 0 ? -hw + 3.5f : (iy - 1.5f) * SlotV + inner;
                float v1 = iy == 2 ? hw - 3.5f : (iy - 0.5f) * SlotV - inner;
                ToWorld(f, (u0 + u1) * 0.5f, (v0 + v1) * 0.5f, out float wx, out float wy);
                uint key = RegionNoise.Hash((int)Math.Round(wx), (int)Math.Round(wy), _seed);
                float mix = radial + (Unit(key, 1) - 0.5f) * 0.24f;

                RegionDevelopmentFeature? roof = roofs[ix + iy * 3];
                if (roof.HasValue)
                {
                    ToLocal(f, roof.Value.Center.X, roof.Value.Center.Y, out float uc, out float vc);
                    float c0 = Math.Max(u0 + 1.5f, uc - roof.Value.LengthMeters * 0.5f);
                    float c1 = Math.Min(u1 - 1.5f, uc + roof.Value.LengthMeters * 0.5f);
                    float d0 = Math.Max(v0 + 1.5f, vc - roof.Value.WidthMeters * 0.5f);
                    float d1 = Math.Min(v1 - 1.5f, vc + roof.Value.WidthMeters * 0.5f);
                    if (c1 - c0 >= 14f && d1 - d0 >= 14f)
                    {
                        Built(f, u0, v0, u1, v1, c0, d0, c1, d1, mix, key);
                        continue;
                    }
                }
                Open(f, u0, v0, u1, v1, mix, key);
            }
        }

        /// <summary>建物の枠が無い区画：駐車場・緑地・空き地。</summary>
        private void Open(Frame f, float u0, float v0, float u1, float v1, float mix, uint key)
        {
            float kind = Unit(key, 2);
            // 中心部の空きは駐車場が多い。外側の空きは草地と林にして、駐車場はほとんど置かない
            bool central = mix < 0.45f;
            if (kind < (central ? 0.45f : 0.04f))
            {
                Parking(f, u0, v0, u1, v1, key);
            }
            else if (!central && kind < 0.5f)
            {
                Rect(f, u0, v0, u1, v1, Surface.Grass, 112f, 118f, 74f, 0f);
                Trees(f, u0, v0, u1, v1, 3 + (int)(Unit(key, 21) * 9f), key + 29u, 2.2f, 2.6f);
            }
            else if (kind < (central ? 0.9f : 0.94f))
            {
                Rect(f, u0, v0, u1, v1, Surface.Grass, 84f, 112f, 60f, 0f);
                // 斜めの小道と、ばらついた樹木
                ToWorld(f, u0 + 4f, v0 + 4f, out float ax, out float ay);
                ToWorld(f, u1 - 4f, v1 - 4f, out float bx, out float by);
                Capsule(ax, ay, bx, by, 2.6f, 178f, 166f, 138f, 0.85f, 0f);
                ToWorld(f, u0 + 4f, v1 - 4f, out ax, out ay);
                ToWorld(f, (u0 + u1) * 0.5f, (v0 + v1) * 0.5f, out bx, out by);
                Capsule(ax, ay, bx, by, 2.2f, 178f, 166f, 138f, 0.85f, 0f);
                Trees(f, u0, v0, u1, v1, (int)((u1 - u0) * (v1 - v0) / 150f), key + 31u, 2.6f, 3.4f);
            }
            else
            {
                Rect(f, u0, v0, u1, v1, Surface.Dirt, 146f, 128f, 98f, 0f);
                Trees(f, u0, v0, u1, v1, 4, key + 37u, 1.8f, 1.6f);
            }
        }

        /// <summary>建物の枠がある区画。枠（c0..c1, d0..d1）の中に建物を、残りに庭・広場・駐車場を置く。</summary>
        private void Built(Frame f, float u0, float v0, float u1, float v1, float c0, float d0, float c1, float d1, float mix, uint key)
        {
            // 長い辺が u になるように、枠を 90 度回した座標へ移す
            if (d1 - d0 > c1 - c0)
            {
                f = Turned(f);
                float t;
                t = c0; c0 = d0; d0 = -c1; c1 = d1; d1 = -t;
                t = u0; u0 = v0; v0 = -u1; u1 = v1; v1 = -t;
            }

            float area = (c1 - c0) * (d1 - d0);
            if (Unit(key, 3) < 0.11f && area > 4200f)
            {
                Parking(f, u0, v0, u1, v1, key + 5u);
                Shed(f, c0, d0, c1, d1, key);
            }
            else if (mix < 0.34f)
            {
                Trees(f, u0, v0, u1, v1, 7, key + 11u, 2.2f, 1.6f);
                // 枠の外の 4 つの帯に、低い建物を並べる
                Row(f, u0, v0, c0 - 5f, v1, mix, key + 41u);
                Row(f, c1 + 5f, v0, u1, v1, mix, key + 43u);
                Row(f, c0, v0, c1, d0 - 5f, mix, key + 47u);
                Row(f, c0, d1 + 5f, c1, v1, mix, key + 53u);
                Tower(f, c0, d0, c1, d1, mix, key);
            }
            else if (mix < 0.68f)
            {
                Rect(f, u0, v0, u1, v1, Surface.Grass, 90f, 112f, 64f, 0f);
                Trees(f, u0, v0, u1, v1, 14, key + 13u, 2.4f, 2.2f);
                Midrise(f, c0, d0, c1, d1, key);
            }
            else
            {
                Rect(f, u0, v0, u1, v1, Surface.Grass, 100f, 116f, 68f, 0f);
                Houses(f, u0 + 1f, v0 + 1f, u1 - 1f, v1 - 1f, key);
            }
        }

        /// <summary>帯の長い辺に沿って、間を空けながら低い建物を並べる。</summary>
        private void Row(Frame f, float u0, float v0, float u1, float v1, float mix, uint key)
        {
            if (v1 - v0 > u1 - u0)
            {
                f = Turned(f);
                float t = u0; u0 = v0; v0 = -u1; u1 = v1; v1 = -t;
            }
            float room = v1 - v0;
            if (room < 11f || u1 - u0 < 14f) return;
            float u = u0;
            for (int k = 0; u + 12f <= u1; k++)
            {
                uint building = key + (uint)k * 4099u;
                float w = Math.Min(u1 - u, 14f + 22f * Unit(building, 1));
                if (u1 - (u + w) < 12f) w = u1 - u;
                if (Unit(building, 2) < 0.84f)
                {
                    float depth = Math.Min(room, 11f + 12f * Unit(building, 3));
                    float b0 = Unit(building, 4) < 0.5f ? v0 : v1 - depth;
                    Pick(FlatRoofColors, building, 5, out float r, out float g, out float b);
                    float height = 7f + 26f * Unit(building, 6) * (1.2f - mix);
                    Rect(f, u, b0, u + w, b0 + depth, Surface.FlatRoof, r, g, b, height);
                    RoofUnits(f, u, b0, u + w, b0 + depth, height, building + 7u);
                }
                u += w + 3f + 4f * Unit(building, 8);
            }
        }

        private void Tower(Frame f, float u0, float v0, float u1, float v1, float mix, uint key)
        {
            float w = u1 - u0;
            float d = v1 - v0;
            Pick(FlatRoofColors, key, 4, out float r, out float g, out float b);
            float podium = 9f + 12f * Unit(key, 5);
            if (w < 44f || d < 40f)
            {
                float single = 24f + 70f * Unit(key, 6) * (1f - mix);
                Rect(f, u0, v0, u1, v1, Surface.FlatRoof, r, g, b, single);
                RoofUnits(f, u0, v0, u1, v1, single, key + 7u);
                return;
            }

            Rect(f, u0, v0, u1, v1, Surface.FlatRoof, r, g, b, podium);
            RoofUnits(f, u0, v0, u1, v1, podium, key + 7u);

            float tw = Math.Max(24f, w * (0.42f + 0.3f * Unit(key, 8)));
            float td = Math.Max(22f, d * (0.44f + 0.3f * Unit(key, 9)));
            float tu = u0 + 3f + (w - tw - 6f) * Unit(key, 10);
            float tv = v0 + 3f + (d - td - 6f) * Unit(key, 11);
            float closeness = 1f - RegionNoise.Clamp01(mix / 0.34f);
            float height = 42f + 30f * Unit(key, 12) + 110f * closeness * Unit(key, 13);
            Pick(FlatRoofColors, key, 14, out r, out g, out b);
            Rect(f, tu, tv, tu + tw, tv + td, Surface.FlatRoof, r, g, b, height);
            RoofUnits(f, tu, tv, tu + tw, tv + td, height, key + 15u);
        }

        private void Midrise(Frame f, float u0, float v0, float u1, float v1, uint key)
        {
            float d = v1 - v0;
            int bars = Math.Max(1, Math.Min(3, (int)((d + 12f) / 32f)));
            float depth = Math.Min(16f, d / bars - 2f);
            float pitch = bars > 1 ? (d - depth) / (bars - 1) : 0f;
            for (int k = 0; k < bars; k++)
            {
                float b0 = bars > 1 ? v0 + pitch * k : v0 + (d - depth) * 0.5f;
                uint barKey = key + (uint)k * 101u;
                Pick(FlatRoofColors, barKey, 4, out float r, out float g, out float b);
                float height = 14f + 22f * Unit(barKey, 5);
                bool split = u1 - u0 > 62f && Unit(barKey, 6) < 0.6f;
                if (split)
                {
                    float cut = u0 + (u1 - u0) * (0.4f + 0.2f * Unit(barKey, 7));
                    Rect(f, u0, b0, cut - 4.5f, b0 + depth, Surface.FlatRoof, r, g, b, height);
                    RoofUnits(f, u0, b0, cut - 4.5f, b0 + depth, height, barKey + 8u);
                    Rect(f, cut + 4.5f, b0, u1, b0 + depth, Surface.FlatRoof, r, g, b, height - 3f);
                    RoofUnits(f, cut + 4.5f, b0, u1, b0 + depth, height - 3f, barKey + 9u);
                }
                else
                {
                    Rect(f, u0, b0, u1, b0 + depth, Surface.FlatRoof, r, g, b, height);
                    RoofUnits(f, u0, b0, u1, b0 + depth, height, barKey + 8u);
                }
            }
        }

        /// <summary>区画いっぱいに宅地を割る。2 行を背中合わせにした帯を、路地を挟んで並べる。</summary>
        private void Houses(Frame f, float u0, float v0, float u1, float v1, uint key)
        {
            const float alley = 6f;
            float depth = v1 - v0;
            int bands = Math.Max(1, (int)((depth + alley) / 52f));
            float bandDepth = (depth - alley * (bands - 1)) / bands;
            float rowDepth = bandDepth * 0.5f;
            int cols = Math.Max(1, (int)((u1 - u0) / 15.5f));
            float colWidth = (u1 - u0) / cols;
            for (int band = 0; band < bands; band++)
            {
                float top = v0 + band * (bandDepth + alley);
                if (band > 0) Rect(f, u0, top - alley, u1, top, Surface.Asphalt, 84f, 85f, 86f, 0f);
                Fence(f, u0, top + rowDepth, u1, top + rowDepth);
                for (int half = 0; half < 2; half++)
                for (int col = 0; col < cols; col++)
                {
                    uint lot = key + (uint)((band * 2 + half) * 53 + col) * 2654435761u;
                    float p0 = u0 + col * colWidth;
                    float q0 = top + half * rowDepth;
                    if (col > 0) Fence(f, p0, q0, p0, q0 + rowDepth);
                    if (Unit(lot, 1) < 0.06f)
                    {
                        Trees(f, p0, q0, p0 + colWidth, q0 + rowDepth, 3, lot + 3u, 2f, 2f);
                        continue;
                    }

                    float w = Math.Min(colWidth - 3.5f, 8.5f + 4.5f * Unit(lot, 2));
                    float d = Math.Min(rowDepth - 9f, 7.5f + 4f * Unit(lot, 3));
                    if (w < 5f || d < 5f) continue;
                    float hu = p0 + (colWidth - w) * (0.3f + 0.4f * Unit(lot, 4));
                    float setback = 4f + 2.5f * Unit(lot, 5);
                    // 上の行は上の道、下の行は下の道（路地か区画の外の道）を向く
                    bool faceTop = half == 0;
                    float hv = faceTop ? q0 + setback : q0 + rowDepth - setback - d;

                    // 道から家までの車寄せと、停めてある車
                    float du = hu + w * (Unit(lot, 11) < 0.5f ? 0.12f : 0.58f);
                    Rect(f, du, faceTop ? q0 : hv + d, du + 2.6f, faceTop ? hv : q0 + rowDepth, Surface.Paving, 150f, 148f, 142f, 0f);
                    if (Unit(lot, 12) < 0.55f)
                    {
                        ToWorld(f, du + 1.3f, faceTop ? q0 + 2.6f : q0 + rowDepth - 2.6f, out float cx, out float cy);
                        Car(cx, cy, f.Cos, f.Sin, true, lot + 13u);
                    }

                    Pick(HouseRoofColors, lot, 6, out float r, out float g, out float b);
                    Surface roof = Unit(lot, 7) < 0.68f ? Surface.GableRoof : Surface.HipRoof;
                    Rect(f, hu, hv, hu + w, hv + d, roof, r, g, b, 6f + 2.4f * Unit(lot, 8));

                    // 裏庭：物置と庭木
                    float y0 = faceTop ? hv + d + 1f : q0 + 0.8f;
                    float y1 = faceTop ? q0 + rowDepth - 0.8f : hv - 1f;
                    if (Unit(lot, 9) < 0.42f && y1 - y0 > 6f)
                    {
                        float gu = Unit(lot, 10) < 0.5f ? p0 + 1.2f : p0 + colWidth - 4.4f;
                        float gv = faceTop ? y1 - 4.4f : y0;
                        Rect(f, gu, gv, gu + 3.2f, gv + 4.4f, Surface.FlatRoof, 150f, 150f, 146f, 2.6f);
                    }
                    Trees(f, p0, y0, p0 + colWidth, y1, (int)(Unit(lot, 14) * 3f), lot + 15u, 1.7f, 1.8f);
                }
            }
        }

        private void Fence(Frame f, float ua, float va, float ub, float vb)
        {
            ToWorld(f, ua, va, out float ax, out float ay);
            ToWorld(f, ub, vb, out float bx, out float by);
            Capsule(ax, ay, bx, by, 0.5f, 70f, 84f, 58f, 0.55f, 0f);
        }

        private void Shed(Frame f, float u0, float v0, float u1, float v1, uint key)
        {
            Pick(ShedRoofColors, key, 4, out float r, out float g, out float b);
            float height = 9f + 4f * Unit(key, 5);
            Rect(f, u0, v0, u1, v1, Surface.RibbedRoof, r, g, b, height);
            // 明かり取りの列
            for (float v = v0 + 9f; v < v1 - 8f; v += 13f)
            for (float u = u0 + 8f; u < u1 - 9f; u += 11f)
                Rect(f, u, v, u + 3.2f, v + 1.3f, Surface.Plain, 222f, 230f, 234f, height + 0.2f);
            RoofUnits(f, u0, v0, u1, v1, height, key + 6u);
        }

        /// <summary>平らな屋根の上の設備（空調機・塔屋）。</summary>
        private void RoofUnits(Frame f, float u0, float v0, float u1, float v1, float height, uint key)
        {
            float w = u1 - u0 - 6f;
            float d = v1 - v0 - 6f;
            if (w < 4f || d < 4f) return;
            int count = Math.Max(2, Math.Min(16, (int)(w * d / 260f)));
            for (int k = 0; k < count; k++)
            {
                uint unit = key + (uint)k * 7919u;
                float sw = 1.6f + 3.6f * Unit(unit, 1);
                float sd = 1.6f + 2.8f * Unit(unit, 2);
                float su = u0 + 3f + (w - sw) * Unit(unit, 3);
                float sv = v0 + 3f + (d - sd) * Unit(unit, 4);
                float tone = Unit(unit, 5);
                float c = tone < 0.4f ? 98f : (tone < 0.75f ? 206f : 134f);
                Rect(f, su, sv, su + sw, sv + sd, Surface.Plain, c, c + 1f, c + 3f, height + 1.6f + 2.4f * Unit(unit, 6));
            }
        }

        private void Parking(Frame f, float u0, float v0, float u1, float v1, uint key)
        {
            Rect(f, u0, v0, u1, v1, Surface.Asphalt, 70f, 72f, 75f, 0f);
            float occupancy = 0.25f + 0.55f * Unit(key, 20);
            // 車室 5 m・通路 6.5 m・車室 5 m を 1 組として、v の向きに並べる
            for (float v = v0 + 2f; v + 16.5f <= v1 - 1f; v += 16.5f)
            for (int side = 0; side < 2; side++)
            {
                float s0 = side == 0 ? v : v + 11.5f;
                int index = 0;
                for (float u = u0 + 2.5f; u + 2.6f <= u1 - 2f; u += 2.6f, index++)
                {
                    ToWorld(f, u, s0, out float ax, out float ay);
                    ToWorld(f, u, s0 + 5f, out float bx, out float by);
                    Capsule(ax, ay, bx, by, 0.26f, 214f, 214f, 208f, 0.8f, 0f);
                    uint stall = key + (uint)(index * 131 + side * 17) + (uint)(int)(v - v0) * 977u;
                    if (Unit(stall, 1) > occupancy) continue;
                    ToWorld(f, u + 1.3f, s0 + 2.5f, out float cx, out float cy);
                    Car(cx, cy, f.Cos, f.Sin, true, stall);
                }
            }
        }

        // ---- 道路 ----

        private void CollectRoads(RegionDevelopment plan)
        {
            foreach (var street in plan.Streets)
                AddRoad(street.Start, street.End, street.Width, false);
            for (int i = 1; i < plan.Highway.Count; i++)
                AddRoad(plan.Highway[i - 1], plan.Highway[i], 38f, true);
            for (int i = 1; i < plan.AirportAccess.Count; i++)
                AddRoad(plan.AirportAccess[i - 1], plan.AirportAccess[i], 20f, false);
        }

        private void AddRoad(RegionPosition a, RegionPosition b, float width, bool highway)
        {
            float margin = 60f;
            float size = _m * _cell;
            if (Math.Max(a.X, b.X) < _ox - margin || Math.Min(a.X, b.X) > _ox + size + margin ||
                Math.Max(a.Y, b.Y) < _oy - margin || Math.Min(a.Y, b.Y) > _oy + size + margin) return;
            _roads.Add(new Road { A = a, B = b, Width = width, Highway = highway });
        }

        private void DrawRoads()
        {
            // 路肩・歩道 → 路面 → 線 → 車と並木 の順に、全部の道で 1 段ずつ描く（交差点で下の段が上に出ない）
            // 幹線は街区の上を通るので、両脇を草の法面で覆ってから路肩を敷く
            foreach (Road road in _roads)
                if (road.Highway) Capsule(road.A.X, road.A.Y, road.B.X, road.B.Y, road.Width + 36f, 104f, 110f, 70f, 1f, 0f, true);
            foreach (Road road in _roads)
            {
                if (road.Highway) Capsule(road.A.X, road.A.Y, road.B.X, road.B.Y, road.Width + 7f, 124f, 124f, 114f, 1f, 0f);
                else Capsule(road.A.X, road.A.Y, road.B.X, road.B.Y, road.Width + 6f, 156f, 154f, 147f, 1f, 0f);
            }
            foreach (Road road in _roads)
                Capsule(road.A.X, road.A.Y, road.B.X, road.B.Y, road.Width, 74f, 76f, 79f, 1f, 0f, true);
            foreach (Road road in _roads) Markings(road);
            foreach (Road road in _roads) Traffic(road);
            foreach (Road road in _roads)
                if (!road.Highway) StreetTrees(road);
        }

        private void Markings(Road road)
        {
            float dx = road.B.X - road.A.X, dy = road.B.Y - road.A.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 1f) return;
            float ux = dx / length, uy = dy / length;
            float nx = -uy, ny = ux;
            if (road.Highway)
            {
                // 中央の分離帯、両端の実線、片側 3 本の破線
                Capsule(road.A.X, road.A.Y, road.B.X, road.B.Y, 1.6f, 150f, 150f, 144f, 1f, 0f);
                for (int side = -1; side <= 1; side += 2)
                {
                    float edge = side * (road.Width * 0.5f - 1.6f);
                    Capsule(road.A.X + nx * edge, road.A.Y + ny * edge, road.B.X + nx * edge, road.B.Y + ny * edge, 0.3f, 222f, 222f, 216f, 0.8f, 0f);
                    for (int lane = 1; lane <= 3; lane++)
                        Dashes(road, ux, uy, nx, ny, length, side * (1.6f + lane * 3.7f), 6f, 12f);
                }
            }
            else if (road.Width > 20f)
            {
                Capsule(road.A.X, road.A.Y, road.B.X, road.B.Y, 0.5f, 204f, 176f, 70f, 0.85f, 0f);
                Dashes(road, ux, uy, nx, ny, length, road.Width * 0.25f, 4f, 9f);
                Dashes(road, ux, uy, nx, ny, length, -road.Width * 0.25f, 4f, 9f);
            }
            else
            {
                Dashes(road, ux, uy, nx, ny, length, 0f, 3f, 9f);
            }
        }

        private void Dashes(Road road, float ux, float uy, float nx, float ny, float length, float offset, float dash, float period)
        {
            float ox = road.A.X + nx * offset, oy = road.A.Y + ny * offset;
            for (float s = 2f; s + dash < length; s += period)
            {
                float ax = ox + ux * s, ay = oy + uy * s;
                if (!InBuffer(ax, ay, 12f)) continue;
                Capsule(ax, ay, ax + ux * dash, ay + uy * dash, 0.3f, 222f, 222f, 216f, 0.8f, 0f);
            }
        }

        private void Traffic(Road road)
        {
            float dx = road.B.X - road.A.X, dy = road.B.Y - road.A.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 20f) return;
            float ux = dx / length, uy = dy / length;
            uint key = RegionNoise.Hash((int)road.A.X, (int)road.A.Y, _seed + 71u) ^ RegionNoise.Hash((int)road.B.X, (int)road.B.Y, _seed + 73u);
            int lanes = road.Highway ? 3 : 1;
            for (int side = -1; side <= 1; side += 2)
            for (int lane = 0; lane < lanes; lane++)
            {
                float offset = road.Highway ? side * (3.4f + lane * 3.7f) : side * road.Width * 0.24f;
                uint laneKey = key + (uint)(side + 2) * 31u + (uint)lane * 57u;
                float s = 6f + 40f * Unit(laneKey, 0);
                for (int k = 1; s < length - 6f; k++)
                {
                    float cx = road.A.X + ux * s - uy * offset;
                    float cy = road.A.Y + uy * s + ux * offset;
                    if (InBuffer(cx, cy, 6f)) Car(cx, cy, ux, uy, false, laneKey + (uint)k * 193u);
                    s += (road.Highway ? 18f : 14f) + (road.Highway ? 90f : 70f) * Unit(laneKey, k);
                }
            }
        }

        private void StreetTrees(Road road)
        {
            float dx = road.B.X - road.A.X, dy = road.B.Y - road.A.Y;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 20f) return;
            float ux = dx / length, uy = dy / length;
            uint key = RegionNoise.Hash((int)road.A.X, (int)road.A.Y, _seed + 83u) ^ RegionNoise.Hash((int)road.B.X, (int)road.B.Y, _seed + 89u);
            for (int side = -1; side <= 1; side += 2)
            {
                float offset = side * (road.Width * 0.5f + 7.5f);
                int index = 0;
                for (float s = 9f; s < length - 9f; s += 10.5f, index++)
                {
                    uint tree = key + (uint)(index * 2 + side + 1) * 613u;
                    if (Unit(tree, 1) < 0.22f) continue;
                    float cx = road.A.X + ux * (s + 3f * Unit(tree, 2)) - uy * offset;
                    float cy = road.A.Y + uy * (s + 3f * Unit(tree, 2)) + ux * offset;
                    if (!InBuffer(cx, cy, 8f) || OnRoad(cx, cy, 4.5f)) continue;
                    Tree(cx, cy, 2.5f + 1.5f * Unit(tree, 3), 6f + 5f * Unit(tree, 4), tree);
                }
            }
        }

        private bool OnRoad(float x, float y, float clearance)
        {
            foreach (Road road in _roads)
            {
                float dx = road.B.X - road.A.X, dy = road.B.Y - road.A.Y;
                float t = RegionNoise.Clamp01(((x - road.A.X) * dx + (y - road.A.Y) * dy) / Math.Max(1f, dx * dx + dy * dy));
                float px = x - road.A.X - dx * t, py = y - road.A.Y - dy * t;
                float limit = road.Width * 0.5f + clearance;
                if (px * px + py * py < limit * limit) return true;
            }
            return false;
        }

        // ---- 小さな物 ----

        private void Trees(Frame f, float u0, float v0, float u1, float v1, int count, uint key, float radius, float spread)
        {
            if (u1 - u0 < 6f || v1 - v0 < 6f) return;
            for (int k = 0; k < count; k++)
            {
                uint tree = key + (uint)k * 3571u;
                ToWorld(f, u0 + 2.5f + (u1 - u0 - 5f) * Unit(tree, 1), v0 + 2.5f + (v1 - v0 - 5f) * Unit(tree, 2), out float x, out float y);
                Tree(x, y, radius + spread * Unit(tree, 3), 4.5f + 7f * Unit(tree, 4), tree);
            }
        }

        private void Tree(float wx, float wy, float radius, float height, uint key)
        {
            int x0 = PixelFloor(wx - radius - _cell, _ox), x1 = PixelCeil(wx + radius + _cell, _ox);
            int y0 = PixelFloor(wy - radius - _cell, _oy), y1 = PixelCeil(wy + radius + _cell, _oy);
            float phase = Unit(key, 5) * 6.2832f;
            float tone = Unit(key, 6);
            float r = 46f + 26f * tone, g = 80f + 26f * tone, b = 38f + 16f * tone;
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = _ox + (x + 0.5f) * _cell - wx;
                float dy = _oy + (y + 0.5f) * _cell - wy;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                // 樹冠の縁を少し波打たせる
                float edge = radius * (0.86f + 0.14f * (float)Math.Sin(Math.Atan2(dy, dx) * 5f + phase));
                float coverage = RegionNoise.Clamp01(0.5f + (edge - distance) / _cell);
                if (coverage <= 0f) continue;
                // 太陽の側を明るく、葉のかたまりごとに明暗を付ける
                float lit = (dx * SunX + dy * SunY) / radius;
                float clump = RegionNoise.Hash01((int)Math.Floor((wx + dx) * 0.9f), (int)Math.Floor((wy + dy) * 0.9f), key) - 0.5f;
                float shade = 0.92f + 0.24f * lit + 0.3f * clump;
                Put(x, y, r * shade, g * shade, b * shade, coverage, height);
            }
        }

        private void Car(float wx, float wy, float alongX, float alongY, bool acrossFrame, uint key)
        {
            // acrossFrame: 駐車場では車室の向き（枠の v）に沿って停める
            Frame f;
            f.Ox = wx;
            f.Oy = wy;
            f.Cos = acrossFrame ? -alongY : alongX;
            f.Sin = acrossFrame ? alongX : alongY;
            Pick(CarColors, key, 2, out float r, out float g, out float b);
            Rect(f, -2.2f, -0.95f, 2.2f, 0.95f, Surface.Plain, r, g, b, 1.4f);
            Rect(f, -1.1f, -0.78f, 0.9f, 0.78f, Surface.Plain, 36f, 41f, 50f, 1.5f);
        }

        // ---- 描画の基本 ----

        private void Rect(Frame f, float u0, float v0, float u1, float v1, Surface surface, float r, float g, float b, float height)
        {
            if (u1 - u0 < 0.05f || v1 - v0 < 0.05f) return;
            ToWorld(f, u0, v0, out float ax, out float ay);
            ToWorld(f, u1, v0, out float bx, out float by);
            ToWorld(f, u1, v1, out float cx, out float cy);
            ToWorld(f, u0, v1, out float dx, out float dy);
            int x0 = PixelFloor(Math.Min(Math.Min(ax, bx), Math.Min(cx, dx)), _ox);
            int x1 = PixelCeil(Math.Max(Math.Max(ax, bx), Math.Max(cx, dx)), _ox);
            int y0 = PixelFloor(Math.Min(Math.Min(ay, by), Math.Min(cy, dy)), _oy);
            int y1 = PixelCeil(Math.Max(Math.Max(ay, by), Math.Max(cy, dy)), _oy);
            if (x0 > x1 || y0 > y1) return;

            // 枠の座標での太陽の向き（屋根の面の明暗に使う）
            float sunU = SunX * f.Cos + SunY * f.Sin;
            float sunV = SunY * f.Cos - SunX * f.Sin;
            float uc = (u0 + u1) * 0.5f, vc = (v0 + v1) * 0.5f;
            bool alongU = u1 - u0 >= v1 - v0;
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                int hits = 0;
                for (int s = 0; s < 4; s++)
                {
                    ToLocal(f, _ox + (x + 0.25f + 0.5f * (s & 1)) * _cell, _oy + (y + 0.25f + 0.5f * (s >> 1)) * _cell, out float su, out float sv);
                    if (su >= u0 && su <= u1 && sv >= v0 && sv <= v1) hits++;
                }
                if (hits == 0) continue;

                float wx = _ox + (x + 0.5f) * _cell;
                float wy = _oy + (y + 0.5f) * _cell;
                ToLocal(f, wx, wy, out float u, out float v);
                float grain = RegionNoise.Hash01((int)Math.Floor(wx * 1.7f), (int)Math.Floor(wy * 1.7f), _seed + 5u) - 0.5f;
                float mul = 1f, add = 0f;
                switch (surface)
                {
                    case Surface.Plain:
                        add = grain * 6f;
                        break;
                    case Surface.Asphalt:
                        add = grain * 9f + RegionNoise.Fbm(wx * 0.05f, wy * 0.05f, 2, _seed + 7u) * 7f;
                        break;
                    case Surface.Paving:
                        add = grain * 7f + RegionNoise.Fbm(wx * 0.04f, wy * 0.04f, 2, _seed + 9u) * 5f;
                        break;
                    case Surface.Grass:
                        mul = 1f + 0.17f * RegionNoise.Fbm(wx * 0.035f, wy * 0.035f, 3, _seed + 11u);
                        add = grain * 14f;
                        break;
                    case Surface.Dirt:
                        mul = 1f + 0.14f * RegionNoise.Fbm(wx * 0.05f, wy * 0.05f, 3, _seed + 13u);
                        add = grain * 10f;
                        break;
                    case Surface.FlatRoof:
                    {
                        mul = 1f + 0.05f * RegionNoise.Fbm(wx * 0.08f, wy * 0.08f, 2, _seed + 15u);
                        add = grain * 5f;
                        // 縁の立ち上がりは明るく、太陽の側の縁の内側には立ち上がりの影が落ちる
                        float edgeU = Math.Min(u - u0, u1 - u), edgeV = Math.Min(v - v0, v1 - v);
                        if (Math.Min(edgeU, edgeV) < 0.7f) mul *= 1.1f;
                        else
                        {
                            float nearU = u - u0 < u1 - u ? -sunU : sunU;
                            float nearV = v - v0 < v1 - v ? -sunV : sunV;
                            if ((edgeU < 2.1f && nearU > 0.2f) || (edgeV < 2.1f && nearV > 0.2f)) mul *= 0.74f;
                        }
                        break;
                    }
                    case Surface.GableRoof:
                    {
                        float side = alongU ? v - vc : u - uc;
                        float lit = (side > 0f ? 1f : -1f) * (alongU ? sunV : sunU);
                        mul = 1f + 0.24f * lit;
                        if (Math.Abs(side) < 0.32f) mul *= 0.84f;
                        add = grain * 8f;
                        break;
                    }
                    case Surface.HipRoof:
                    {
                        float edgeU = Math.Min(u - u0, u1 - u), edgeV = Math.Min(v - v0, v1 - v);
                        float lit = edgeV <= edgeU ? (v > vc ? sunV : -sunV) : (u > uc ? sunU : -sunU);
                        mul = 1f + 0.24f * lit;
                        if (Math.Abs(edgeU - edgeV) < 0.3f) mul *= 0.87f;
                        add = grain * 8f;
                        break;
                    }
                    case Surface.RibbedRoof:
                        mul = 1f + 0.045f * (float)Math.Sin((alongU ? u : v) * 2.6f);
                        add = grain * 4f;
                        break;
                }
                Put(x, y, r * mul + add, g * mul + add, b * mul + add, hits * 0.25f, height);
            }
        }

        private void Capsule(float ax, float ay, float bx, float by, float width, float r, float g, float b, float opacity, float height, bool textured = false)
        {
            float radius = width * 0.5f;
            float dx = bx - ax, dy = by - ay;
            float lengthSquared = dx * dx + dy * dy;
            int x0 = PixelFloor(Math.Min(ax, bx) - radius - _cell, _ox), x1 = PixelCeil(Math.Max(ax, bx) + radius + _cell, _ox);
            int y0 = PixelFloor(Math.Min(ay, by) - radius - _cell, _oy), y1 = PixelCeil(Math.Max(ay, by) + radius + _cell, _oy);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float wx = _ox + (x + 0.5f) * _cell, wy = _oy + (y + 0.5f) * _cell;
                float px = wx - ax, py = wy - ay;
                float t = RegionNoise.Clamp01((px * dx + py * dy) / Math.Max(0.01f, lengthSquared));
                float qx = px - t * dx, qy = py - t * dy;
                // 画素より細い線は、幅に応じた薄さで写す
                float coverage = RegionNoise.Clamp01(Math.Min(width / _cell, 0.5f + (radius - (float)Math.Sqrt(qx * qx + qy * qy)) / _cell));
                if (coverage <= 0f) continue;
                float add = 0f;
                if (textured)
                    add = (RegionNoise.Hash01((int)Math.Floor(wx * 1.7f), (int)Math.Floor(wy * 1.7f), _seed + 5u) - 0.5f) * 9f
                        + RegionNoise.Fbm(wx * 0.05f, wy * 0.05f, 2, _seed + 7u) * 7f;
                Put(x, y, r + add, g + add, b + add, coverage * opacity, height);
            }
        }

        private void Put(int x, int y, float r, float g, float b, float alpha, float height)
        {
            int i = x + y * _m;
            float keep = 1f - alpha;
            _r[i] = _r[i] * keep + r * alpha;
            _g[i] = _g[i] * keep + g * alpha;
            _b[i] = _b[i] * keep + b * alpha;
            _a[i] = _a[i] * keep + alpha;
            if (alpha < 0.5f) return;
            _h[i] = height;
            if (height > _maxHeight) _maxHeight = height;
        }

        /// <summary>各画素から太陽の向きへ辿り、より高い物に遮られていれば影にする。</summary>
        private bool[] CastShadows()
        {
            var shadow = new bool[_m * _m];
            float rise = _cell * SunTan;
            int limit = (int)Math.Ceiling(_maxHeight / rise);
            if (limit < 1) return shadow;
            float top = _maxHeight;
            Parallel.For(0, _m, y =>
            {
                for (int x = 0; x < _m; x++)
                {
                    float own = _h[x + y * _m];
                    for (int k = 1; k <= limit; k++)
                    {
                        float clearance = own + k * rise + 0.25f;
                        if (clearance > top) break;
                        int sx = (int)(x + 0.5f + SunX * k);
                        int sy = (int)(y + 0.5f + SunY * k);
                        if (sx < 0 || sy < 0 || sx >= _m || sy >= _m) break;
                        if (_h[sx + sy * _m] <= clearance) continue;
                        shadow[x + y * _m] = true;
                        break;
                    }
                }
            });
            return shadow;
        }

        private byte[] Resolve(bool[] shadow)
        {
            // 影は、青みのある暗い色を 0.52 の濃さで重ねる
            Parallel.For(0, _m, y =>
            {
                for (int x = 0; x < _m; x++)
                {
                    int i = x + y * _m;
                    if (!shadow[i]) continue;
                    const float s = 0.52f;
                    _r[i] = _r[i] * (1f - s) + 10f * s;
                    _g[i] = _g[i] * (1f - s) + 14f * s;
                    _b[i] = _b[i] * (1f - s) + 24f * s;
                    _a[i] = _a[i] * (1f - s) + s;
                }
            });

            // 航空写真の柔らかさに寄せて、隣の画素と少し混ぜる
            var pixels = new byte[_n * _n * 4];
            Parallel.For(0, _n, y =>
            {
                for (int x = 0; x < _n; x++)
                {
                    float r = 0f, g = 0f, b = 0f, a = 0f;
                    for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        float weight = (dx == 0 ? 0.64f : 0.18f) * (dy == 0 ? 0.64f : 0.18f);
                        int i = x + _pad + dx + (y + _pad + dy) * _m;
                        r += _r[i] * weight;
                        g += _g[i] * weight;
                        b += _b[i] * weight;
                        a += _a[i] * weight;
                    }
                    int p = (x + y * _n) * 4;
                    pixels[p] = ToByte(r);
                    pixels[p + 1] = ToByte(g);
                    pixels[p + 2] = ToByte(b);
                    pixels[p + 3] = ToByte(a * 255f);
                }
            });
            return pixels;
        }

        // ---- 座標 ----

        private static Frame MakeFrame(RegionPosition center, float angle)
        {
            Frame f;
            f.Ox = center.X;
            f.Oy = center.Y;
            f.Cos = (float)Math.Cos(angle);
            f.Sin = (float)Math.Sin(angle);
            return f;
        }

        /// <summary>90 度回した枠。元の (u, v) は新しい枠の (v, -u) になる。</summary>
        private static Frame Turned(Frame f)
        {
            Frame turned;
            turned.Ox = f.Ox;
            turned.Oy = f.Oy;
            turned.Cos = -f.Sin;
            turned.Sin = f.Cos;
            return turned;
        }

        private static void ToWorld(Frame f, float u, float v, out float x, out float y)
        {
            x = f.Ox + u * f.Cos - v * f.Sin;
            y = f.Oy + u * f.Sin + v * f.Cos;
        }

        private static void ToLocal(Frame f, float x, float y, out float u, out float v)
        {
            float dx = x - f.Ox, dy = y - f.Oy;
            u = dx * f.Cos + dy * f.Sin;
            v = dy * f.Cos - dx * f.Sin;
        }

        private bool Near(RegionPosition point, float margin)
        {
            return InBuffer(point.X, point.Y, margin);
        }

        private bool InBuffer(float x, float y, float margin)
        {
            float size = _m * _cell;
            return x > _ox - margin && y > _oy - margin && x < _ox + size + margin && y < _oy + size + margin;
        }

        private int PixelFloor(float position, float origin)
        {
            return Math.Max(0, (int)Math.Floor((position - origin) / _cell));
        }

        private int PixelCeil(float position, float origin)
        {
            return Math.Min(_m - 1, (int)Math.Ceiling((position - origin) / _cell));
        }

        private static float Unit(uint key, int channel)
        {
            return RegionNoise.Hash01(channel, 0, key);
        }

        private static void Pick(float[] palette, uint key, int channel, out float r, out float g, out float b)
        {
            int index = (int)(Unit(key, channel) * (palette.Length / 3)) * 3;
            r = palette[index];
            g = palette[index + 1];
            b = palette[index + 2];
        }

        private static byte ToByte(float value)
        {
            return (byte)(value <= 0f ? 0f : (value >= 255f ? 255f : value + 0.5f));
        }
    }
}
