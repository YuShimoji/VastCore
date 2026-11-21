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

        public readonly List<RegionSite> Sites = new List<RegionSite>();

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
    }
}
