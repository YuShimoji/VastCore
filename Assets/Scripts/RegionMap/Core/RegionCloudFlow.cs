using System;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 雲の層を、場所ごとに違う向きと速さで動かすための計算。UnityEngine に依存しない。
    /// 層の絵そのものは作り直さず、絵を読む位置を「一様な流れ＋場所ごとの渦のずれ」でずらす。
    /// 渦のずれは大きさに上限があるので、時間が経っても絵が引き伸ばされ続けることは無い
    /// （flow map の手法では、ずれが伸び続けるのを 2 つの位相の入れ替えで隠す。ここは上限のあるずれにして、入れ替えを要らなくした）。
    /// </summary>
    public static class RegionCloudFlow
    {
        /// <summary>低い所の塊の雲。</summary>
        public const int Low = 0;

        /// <summary>高い所の薄い筋雲。</summary>
        public const int High = 1;
        public const float DefaultCrossSeconds = 420f;

        /// <summary>探索地図のメッシュと同じ升目で雲の流れと濃さを補間する。</summary>
        public sealed class Grid
        {
            public const int Cells = 32;
            public const int Side = Cells + 1;

            private readonly int _layer;
            private readonly float _time, _crossSeconds;
            private readonly (float Du, float Dv, float Fade)[] _points = new (float, float, float)[Side * Side];

            public Grid(int layer, float time, float crossSeconds)
            {
                _layer = layer;
                _time = time;
                _crossSeconds = crossSeconds;
                for (int y = 0; y < Side; y++)
                {
                    for (int x = 0; x < Side; x++)
                    {
                        RegionCloudFlow.Sample(layer, x / (float)Cells, y / (float)Cells, time, crossSeconds,
                            out float du, out float dv, out float fade);
                        _points[x + y * Side] = (du, dv, fade);
                    }
                }
            }

            public (float Du, float Dv, float Fade) this[int x, int y] => _points[x + y * Side];

            public (float Du, float Dv, float Fade) At(float u, float v)
            {
                if (u < 0f || u > 1f || v < 0f || v > 1f)
                {
                    // 地図の外側も地上からは見える。地図の縁から流れをそのまま延ばす。
                    RegionCloudFlow.Sample(_layer, u, v, _time, _crossSeconds, out float du, out float dv, out float fade);
                    return (du, dv, fade);
                }

                int x = Math.Min((int)(u * Cells), Cells - 1);
                int y = Math.Min((int)(v * Cells), Cells - 1);
                float fx = u * Cells - x;
                float fy = v * Cells - y;
                var a = this[x, y];
                var d = this[x + 1, y + 1];
                bool upper = fy >= fx;
                var corner = upper ? this[x, y + 1] : this[x + 1, y];
                float diagonalWeight = Math.Min(fx, fy);
                float aWeight = 1f - Math.Max(fx, fy);
                float cornerWeight = Math.Abs(fx - fy);
                return (a.Du * aWeight + d.Du * diagonalWeight + corner.Du * cornerWeight,
                    a.Dv * aWeight + d.Dv * diagonalWeight + corner.Dv * cornerWeight,
                    a.Fade * aWeight + d.Fade * diagonalWeight + corner.Fade * cornerWeight);
            }
        }

        // 一様な流れの向き（u は東、v は北が正）と、基準の速さに対する倍率。高い雲ほど速く、向きも少し違う
        private static readonly float[] WindU = { 1f, 1f };
        private static readonly float[] WindV = { -0.22f, 0.17f };
        private static readonly float[] Speed = { 1f, 1.7f };

        // 渦のずれ：模様の細かさ（地図の幅あたりの山の数）、ずれの大きさ、模様が移り変わる速さ
        // ずれが大きすぎると雲の絵が筋状に引き伸ばされる。隣り合う位置のずれの差が、位置の差の 3 割ほどに収まる大きさにしてある
        private static readonly float[] SwirlScale = { 2.4f, 1.6f };
        private static readonly float[] SwirlAmount = { 0.0034f, 0.0052f };
        private static readonly float[] SwirlRate = { 0.03f, 0.02f };

        // 場所ごとの濃淡のゆらぎ（湧いたり薄れたりして見える）。0 で無し
        private static readonly float[] FadeDepth = { 0.38f, 0.55f };

        private const float Epsilon = 0.01f;

        /// <summary>
        /// 地図上の位置 (u, v)（0〜1、v は北が正）で、雲の絵を読む位置のずれと濃さの倍率を返す。
        /// 絵は (u - du, v - dv) を読む。time は秒、crossSeconds は低い雲が地図の幅を横切る秒数（0 以下なら一様な流れは無し）。
        /// </summary>
        public static void Sample(int layer, float u, float v, float time, float crossSeconds, out float du, out float dv, out float fade)
        {
            uint seed = layer == Low ? 9173u : 4421u;

            float driftU = 0f;
            float driftV = 0f;
            if (crossSeconds > 0f)
            {
                double turns = time / (double)crossSeconds * Speed[layer];
                double a = turns * WindU[layer];
                double b = turns * WindV[layer];
                driftU = (float)(a - Math.Floor(a));
                driftV = (float)(b - Math.Floor(b));
            }

            // 渦：なめらかな模様（ポテンシャル）の傾きを 90 度回したものをずれにする。集まりも散りもしない流れになる
            float scale = SwirlScale[layer];
            float shift = time * SwirlRate[layer];
            float x = u * scale;
            float y = v * scale;
            float e = Epsilon * scale;
            float px = (Potential(x + e, y, shift, seed) - Potential(x - e, y, shift, seed)) / (2f * Epsilon);
            float py = (Potential(x, y + e, shift, seed) - Potential(x, y - e, shift, seed)) / (2f * Epsilon);
            float amount = SwirlAmount[layer];
            du = driftU + py * amount;
            dv = driftV - px * amount;

            float depth = FadeDepth[layer];
            float thin = RegionNoise.Gradient(u * 2.3f + shift * 0.9f + 31.7f, v * 2.3f - shift * 0.6f + 7.3f, seed + 5u);
            fade = 1f - depth * RegionNoise.Smooth(-0.15f, 0.55f, thin);
        }

        /// <summary>渦のもとになる模様。向きの違う 2 枚を別々の向きへ動かして足すので、形そのものが時間で移り変わる。</summary>
        private static float Potential(float x, float y, float shift, uint seed)
        {
            float a = RegionNoise.Gradient(x + shift, y + shift * 0.4f, seed);
            float b = RegionNoise.Gradient(x * 1.9f - shift * 1.3f + 13.1f, y * 1.9f + shift * 0.7f + 4.9f, seed + 1u);
            return a + b * 0.3f;
        }

        /// <summary>一様な流れだけのずれ（地図の幅を 1 とした割合。1 で一周して 0 へ戻る）。</summary>
        public static void Drift(int layer, float time, float crossSeconds, out float du, out float dv)
        {
            du = 0f;
            dv = 0f;
            if (crossSeconds <= 0f) return;
            double turns = time / (double)crossSeconds * Speed[layer];
            double a = turns * WindU[layer];
            double b = turns * WindV[layer];
            du = (float)(a - Math.Floor(a));
            dv = (float)(b - Math.Floor(b));
        }
    }
}
