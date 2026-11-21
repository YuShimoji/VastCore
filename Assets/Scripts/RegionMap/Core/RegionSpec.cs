using System;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 地域を一つ生成するための入力。同じ値からは同じ地域ができる。
    /// </summary>
    [Serializable]
    public sealed class RegionSpec
    {
        /// <summary>地域を決める番号。</summary>
        public int Seed = 7;

        /// <summary>一辺のセル数。</summary>
        public int Resolution = 1024;

        /// <summary>一辺の長さ（km）。</summary>
        public float SizeKm = 256f;

        /// <summary>陸の多さ。大きいほど陸が増える。</summary>
        public float LandBias = 0.17f;

        /// <summary>外周を海へ落とす強さ。外周は必ず海にする（川の出口になる）。</summary>
        public float EdgeFalloff = 1.3f;

        /// <summary>山脈の強さ。</summary>
        public float MountainAmount = 1f;

        /// <summary>隆起と侵食の繰り返し回数。多いほど谷が発達する。</summary>
        public int ErosionIterations = 40;

        /// <summary>1 回あたりの最大隆起量（m）。</summary>
        public float UpliftMeters = 150f;

        /// <summary>川が地面を削る強さ。</summary>
        public float ErosionStrength = 0.3f;

        /// <summary>斜面の傾きの上限（高さ / 水平距離）。これより急な斜面は崩れる。</summary>
        public float MaxSlope = 0.55f;

        /// <summary>斜面のならし。</summary>
        public float HillslopeDiffusion = 0.012f;

        /// <summary>水の流れ先を選ぶときのゆらぎ（0〜0.9）。0 だと谷が平行な直線になり、大きいほど枝分かれする。</summary>
        public float FlowJitter = 0.8f;

        /// <summary>侵食の前に地面へ足す細かな凹凸（m）。流れを合流させるきっかけになる。</summary>
        public float RoughnessMeters = 14f;

        /// <summary>北端・南端の海面気温（℃）。</summary>
        public float TemperatureNorth = -3f;
        public float TemperatureSouth = 25f;

        /// <summary>全体の湿り気（0〜1）。</summary>
        public float Humidity = 0.5f;

        /// <summary>探索地点の数。</summary>
        public int SiteCount = 18;

        public RegionSpec Clone()
        {
            return (RegionSpec)MemberwiseClone();
        }
    }
}
