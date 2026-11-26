using System;
using System.Threading.Tasks;

namespace Vastcore.RegionMap
{
    /// <summary>3D の場面の空に置く雲の範囲と、2D の地図の雲の条件。</summary>
    public sealed class RegionCloudSkySpec
    {
        /// <summary>地域の seed（RegionSpec.Seed）。2D の雲と同じ模様になる。</summary>
        public int RegionSeed;

        /// <summary>雲の量。2D の地図（RegionRenderOptions.CloudCover）と同じ値にする。</summary>
        public float Cover = 0.58f;

        /// <summary>探索画面の雲の時刻（秒）と、低い雲が地図の幅を横切る秒数（RegionExplorer と同じ）。</summary>
        public float Time;
        public float CrossSeconds = RegionCloudFlow.DefaultCrossSeconds;

        /// <summary>2D の雲の画像の一辺（(RegionSpec.Resolution + 1) / 2）。画素の中心の半画素ぶんのずれを合わせる。0 ならずらさない。</summary>
        public int MapCloudSize;

        /// <summary>場面の座標の原点の、地図の座標（RegionDetail.OriginMapX・Y）と、地図の 1 単位の長さ（m）。</summary>
        public float OriginMapX, OriginMapY, MetersPerMapUnit;

        /// <summary>雲を置く正方形の範囲。Unity の座標（X は東、Z は北）の最小の角と一辺（m）。</summary>
        public float MinX, MinZ, Size;

        /// <summary>雲の地図の一辺の画素数。</summary>
        public int Resolution = 512;
    }

    /// <summary>
    /// 3D の場面の空の雲を、2D の地図に重なる雲（RegionClouds・RegionCloudFlow）と同じ所に同じ形で置くための計算。UnityEngine に依存しない。
    /// 2D で見つけた形の雲は、その地域の 3D の空でも同じ位置に見える（本人の前提条件、2026-11-26）。
    /// </summary>
    public static class RegionCloudSky
    {
        /// <summary>場面の中心を真ん中にした一辺 sizeMeters の範囲の条件。cover・time は 2D の地図と同じ値を渡す。</summary>
        public static RegionCloudSkySpec ForScene(RegionLookScene scene, float sizeMeters, int resolution, float cover, float time)
        {
            if (scene == null) throw new ArgumentNullException("scene");
            return new RegionCloudSkySpec
            {
                RegionSeed = scene.RegionSeed,
                Cover = cover,
                Time = time,
                MapCloudSize = (scene.RegionResolution + 1) / 2,
                OriginMapX = scene.Outer.OriginMapX,
                OriginMapY = scene.Outer.OriginMapY,
                MetersPerMapUnit = scene.MetersPerMapUnit,
                // 場面の Z は南が正（Unity では -Z に置く）
                MinX = scene.CenterX - sizeMeters * 0.5f,
                MinZ = -scene.CenterZ - sizeMeters * 0.5f,
                Size = sizeMeters,
                Resolution = resolution,
            };
        }

        /// <summary>
        /// 範囲を Resolution × Resolution に分け、各画素の中心の 2 枚の雲の濃さ（0〜1）を作る。
        /// 行は南（MinZ）から北へ、列は西（MinX）から東へ並ぶ（Unity のテクスチャの行の順）。
        /// 濃さには、2D で雲を薄く見せる場所ごとのゆらぎ（RegionCloudFlow の fade）を掛けてある。
        /// </summary>
        public static void Build(RegionCloudSkySpec spec, out float[] low, out float[] high)
        {
            if (spec == null) throw new ArgumentNullException("spec");
            if (spec.Resolution < 1) throw new ArgumentOutOfRangeException("spec", "Resolution は 1 以上");
            int n = spec.Resolution;
            var lowField = new float[n * n];
            var highField = new float[n * n];
            low = lowField;
            high = highField;
            if (spec.Cover <= 0f) return;
            float step = spec.Size / n;
            Parallel.For(0, n, r =>
            {
                float z = spec.MinZ + (r + 0.5f) * step;
                for (int c = 0; c < n; c++)
                {
                    float x = spec.MinX + (c + 0.5f) * step;
                    float a;
                    float b;
                    SampleAt(spec, x, z, out a, out b);
                    lowField[c + r * n] = a;
                    highField[c + r * n] = b;
                }
            });
        }

        /// <summary>Unity の座標 (x, z) の真上の、低い塊の雲と高い筋雲の濃さ。2D の地図が今の時刻にその地点へ描く雲と同じもの。</summary>
        public static void SampleAt(RegionCloudSkySpec spec, float x, float z, out float low, out float high)
        {
            low = 0f;
            high = 0f;
            if (spec.Cover <= 0f) return;
            uint seed = RegionClouds.CloudSeed(spec.RegionSeed);
            float amount = RegionClouds.CloudAmount(spec.Cover);
            // 場面は (x, 高さ, -z) で置くので、地図の Y（南へ増える）は -Z から戻す
            float mapX = spec.OriginMapX + x / spec.MetersPerMapUnit;
            float mapY = spec.OriginMapY - z / spec.MetersPerMapUnit;
            // 探索画面の地図の上の位置（u は東、v は北が正）
            float u = mapX / RegionData.MapUnits;
            float v = 1f - mapY / RegionData.MapUnits;
            low = LayerAt(RegionCloudFlow.Low, spec, u, v, seed, amount);
            high = LayerAt(RegionCloudFlow.High, spec, u, v, seed, amount);
        }

        private static float LayerAt(int layer, RegionCloudSkySpec spec, float u, float v, uint seed, float amount)
        {
            float du;
            float dv;
            float fade;
            RegionCloudFlow.Sample(layer, u, v, spec.Time, spec.CrossSeconds, out du, out dv, out fade);
            // 探索画面は雲の画像を (u - du, v - dv) で読む。画像は北の行が上になるよう上下を入れ替えてあり、
            // 画素 (列, 行) の中心には雲の濃さの (列 / size, 行 / size) の値が載る
            float half = spec.MapCloudSize > 0 ? 0.5f / spec.MapCloudSize : 0f;
            float fu = Wrap(u - du - half);
            float fv = Wrap(1f - (v - dv) - half);
            float thick;
            float veil;
            RegionClouds.SampleLayers(fu, fv, seed, amount, out thick, out veil);
            return (layer == RegionCloudFlow.Low ? thick : veil) * fade;
        }

        private static float Wrap(float value)
        {
            return value - (float)Math.Floor(value);
        }
    }
}
