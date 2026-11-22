using System;
using System.Collections.Generic;

namespace Vastcore.RegionMap
{
    /// <summary>セルごとの土地の種類。探索地点の配置規則と表示の説明に使う。</summary>
    public enum RegionBiome : byte
    {
        Ocean = 0,
        Coast = 1,
        Desert = 2,
        Savanna = 3,
        Grassland = 4,
        Forest = 5,
        Rainforest = 6,
        Taiga = 7,
        Tundra = 8,
        Snow = 9,
        Rock = 10,
        River = 11,
        Lake = 12,
    }

    /// <summary>
    /// 生成した地域の全データ。配列は北西の角から行ごとに並ぶ（添字 = x + y * Resolution、y は南向き）。
    /// 地図座標は X・Y とも 0〜100 で、X は東向き、Y は南向き。
    /// </summary>
    public sealed class RegionData
    {
        public const float MapUnits = 100f;

        public readonly int Resolution;
        public readonly float SizeKm;
        public readonly RegionSpec Spec;

        /// <summary>標高（m）。海面は 0、海は負の値。</summary>
        public readonly float[] Height;

        /// <summary>上流から集まる水の量（降水で重み付けしたセル数）。</summary>
        public readonly float[] Flow;

        /// <summary>気温（℃）。</summary>
        public readonly float[] Temperature;

        /// <summary>湿り気（0〜1）。</summary>
        public readonly float[] Moisture;

        /// <summary>湖の深さ（m）。湖でなければ 0。</summary>
        public readonly float[] LakeDepth;

        public readonly RegionBiome[] Biome;

        /// <summary>
        /// セルが属するエリアの番号（RegionArea.Id）。陸はどれかのエリアに入る。岸に近い海は最寄りの陸のエリアの番号を持ち
        /// （そのエリアの「沖」）、それより遠い海は 0（外洋）。
        /// </summary>
        public readonly byte[] Area;

        /// <summary>
        /// 水の流れ先のセル（海や地図の外へ出るなら -1）。川を作った時の向きをそのまま持つ。
        /// 拡大した地形で、川を広域の地図と同じ経路に通すために使う。拡大した地形のデータでは null。
        /// </summary>
        public int[] Downstream;

        /// <summary>
        /// 水面が各セルを覆う割合（0〜1）。拡大した地形だけが持ち、広域の地域では null（描画の時に流量から作る）。
        /// </summary>
        public float[] WaterCoverage;

        /// <summary>構造物が各セルを覆う割合（0〜1）。拡大した地形に構造物を置いた時だけ持つ。</summary>
        public float[] StructureCoverage;

        public readonly List<RegionArea> Areas = new List<RegionArea>();

        public readonly List<RegionSite> Sites = new List<RegionSite>();

        /// <summary>名前を持てる地物（地域そのもの・川・峰・湾）。規則から名前を付けた時（RegionNaming）に作る。</summary>
        public readonly List<RegionFeature> Features = new List<RegionFeature>();

        /// <summary>規則から名前を付けた時の記録（音の質感、語根の表、人物、問題）。付けていなければ null。</summary>
        public RegionNamingReport Naming;

        public float MaxHeight;
        public float LandFraction;

        public RegionData(RegionSpec spec)
        {
            Spec = spec;
            Resolution = spec.Resolution;
            SizeKm = spec.SizeKm;
            int count = Resolution * Resolution;
            Height = new float[count];
            Flow = new float[count];
            Temperature = new float[count];
            Moisture = new float[count];
            LakeDepth = new float[count];
            Biome = new RegionBiome[count];
            Area = new byte[count];
        }

        /// <summary>1 セルの一辺（m）。</summary>
        public float CellMeters
        {
            get { return SizeKm * 1000f / Resolution; }
        }

        public int Index(int x, int y)
        {
            return x + y * Resolution;
        }

        public bool IsLand(int index)
        {
            return Height[index] > 0f;
        }

        /// <summary>地図座標（0〜100）からセルへ。範囲外は端へ丸める。</summary>
        public int CellFromMap(float mapX, float mapY)
        {
            int x = (int)(mapX / MapUnits * Resolution);
            int y = (int)(mapY / MapUnits * Resolution);
            if (x < 0) x = 0; else if (x >= Resolution) x = Resolution - 1;
            if (y < 0) y = 0; else if (y >= Resolution) y = Resolution - 1;
            return Index(x, y);
        }

        public float MapXFromCell(int index)
        {
            return ((index % Resolution) + 0.5f) / Resolution * MapUnits;
        }

        public float MapYFromCell(int index)
        {
            return ((index / Resolution) + 0.5f) / Resolution * MapUnits;
        }

        /// <summary>地図座標 1 単位あたりの距離（km）。</summary>
        public float KmPerMapUnit
        {
            get { return SizeKm / MapUnits; }
        }

        public float HeightAtMap(float mapX, float mapY)
        {
            return Height[CellFromMap(mapX, mapY)];
        }

        public RegionBiome BiomeAtMap(float mapX, float mapY)
        {
            return Biome[CellFromMap(mapX, mapY)];
        }

        /// <summary>番号からエリアを引く。0 や範囲外は null。</summary>
        public RegionArea AreaById(int id)
        {
            return id >= 1 && id <= Areas.Count ? Areas[id - 1] : null;
        }

        /// <summary>その位置のエリア。外洋は null。海の上では、最寄りの陸のエリア（その沖）を返す。</summary>
        public RegionArea AreaAtMap(float mapX, float mapY)
        {
            return AreaById(Area[CellFromMap(mapX, mapY)]);
        }

        /// <summary>その位置の呼び名。陸は「北東の山地」、岸に近い海は「北東の山地の沖」、遠い海は「北の外洋」。</summary>
        public string PlaceNameAtMap(float mapX, float mapY)
        {
            int cell = CellFromMap(mapX, mapY);
            RegionArea area = AreaById(Area[cell]);
            if (area == null) return RegionAreaBuilder.Direction(mapX, mapY) + "の外洋";
            return Height[cell] > 0f ? area.Name : area.Name + "の沖";
        }
    }
}
