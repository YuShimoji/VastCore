using System;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// バイオーム別の 3D 地形表面・崖色・植生色調・Props 環境順応（GroundColor / MossMask / TopDust）の純粋データ定義。
    /// RegionMapPreview（.NET 9 コンソール）からもコンパイルされるため UnityEngine には依存しない。
    /// </summary>
    public sealed class RegionBiomeSurfaceProfile
    {
        public RegionBiome Biome;
        public bool UsesSandstonePipeline;

        public float CliffR = 0.77f, CliffG = 0.45f, CliffB = 0.30f;
        public float GroundSkirtR = 0.72f, GroundSkirtG = 0.56f, GroundSkirtB = 0.40f;
        public float MossR = 0.22f, MossG = 0.34f, MossB = 0.14f;
        public float PlantTintR = 1f, PlantTintG = 1f, PlantTintB = 1f;

        public float TopDustScale = 1f;
        public float MossMaskScale = 0f;

        public float DetailStrength = 1f;
        public float NormalStrength = 1f;
        public float DefaultRoughness = 0.88f;
        public float SpecularStrength = 0.12f;

        public string GroundColorPath = "";
        public string GroundNormalPath = "";
        public string GroundRoughnessPath = "";
        public float GroundTileMetres = 2.6f;

        public string SubsoilColorPath = "";
        public string SubsoilNormalPath = "";
        public string SubsoilRoughnessPath = "";
        public float SubsoilTileMetres = 2.2f;

        public string CliffColorPath = "";
        public string CliffNormalPath = "";
        public string CliffRoughnessPath = "";
        public float CliffTileMetres = 2.4f;
    }

    /// <summary>
    /// RegionBiome に応じた 3D 表面プロファイル解決と、頂点ごとのバイオーム表面チャンネル計算。
    /// </summary>
    public static class RegionBiomeSurface
    {
        private const string MeadowTurfRoot = "Assets/Art/Textures/NAT/grassland_meadow_turf/";
        private const string MeadowLoamRoot = "Assets/Art/Textures/NAT/grassland_loam_soil/";
        private const string GraniteOutcropRoot = "Assets/Art/Textures/NAT/mossy_granite_outcrop/";

        public static RegionBiomeSurfaceProfile ProfileFor(RegionBiome biome)
        {
            switch (biome)
            {
                case RegionBiome.Grassland:
                    return new RegionBiomeSurfaceProfile
                    {
                        Biome = RegionBiome.Grassland,
                        UsesSandstonePipeline = false,
                        CliffR = 0.42f, CliffG = 0.43f, CliffB = 0.39f,
                        GroundSkirtR = 0.34f, GroundSkirtG = 0.41f, GroundSkirtB = 0.24f,
                        MossR = 0.22f, MossG = 0.34f, MossB = 0.14f,
                        PlantTintR = 0.58f, PlantTintG = 0.80f, PlantTintB = 0.56f,
                        TopDustScale = 0.08f,
                        MossMaskScale = 1.0f,
                        DetailStrength = 0.88f,
                        NormalStrength = 1.15f,
                        DefaultRoughness = 0.88f,
                        SpecularStrength = 0.12f,
                        GroundColorPath = MeadowTurfRoot + "Grass005_1K-JPG_Color.jpg",
                        GroundNormalPath = MeadowTurfRoot + "Grass005_1K-JPG_NormalGL.jpg",
                        GroundRoughnessPath = MeadowTurfRoot + "Grass005_1K-JPG_Roughness.jpg",
                        GroundTileMetres = 2.4f,
                        SubsoilColorPath = MeadowLoamRoot + "Ground009_1K-JPG_Color.jpg",
                        SubsoilNormalPath = MeadowLoamRoot + "Ground009_1K-JPG_NormalGL.jpg",
                        SubsoilRoughnessPath = MeadowLoamRoot + "Ground009_1K-JPG_Roughness.jpg",
                        SubsoilTileMetres = 2.2f,
                        CliffColorPath = GraniteOutcropRoot + "mossy_granite_outcrop_diff_1k.jpg",
                        CliffNormalPath = GraniteOutcropRoot + "mossy_granite_outcrop_nor_gl_1k.jpg",
                        CliffRoughnessPath = GraniteOutcropRoot + "mossy_granite_outcrop_rough_1k.jpg",
                        CliffTileMetres = 2.8f,
                    };
                case RegionBiome.Forest:
                case RegionBiome.Rainforest:
                    return new RegionBiomeSurfaceProfile
                    {
                        Biome = biome,
                        UsesSandstonePipeline = false,
                        CliffR = 0.36f, CliffG = 0.38f, CliffB = 0.34f,
                        GroundSkirtR = biome == RegionBiome.Rainforest ? 0.19f : 0.24f,
                        GroundSkirtG = biome == RegionBiome.Rainforest ? 0.31f : 0.33f,
                        GroundSkirtB = biome == RegionBiome.Rainforest ? 0.17f : 0.19f,
                        MossR = 0.18f, MossG = 0.33f, MossB = 0.13f,
                        PlantTintR = 0.66f, PlantTintG = 0.96f, PlantTintB = 0.64f,
                        TopDustScale = 0.0f,
                        MossMaskScale = biome == RegionBiome.Rainforest ? 1.35f : 1.2f,
                        DetailStrength = 0.85f,
                        NormalStrength = 1.1f,
                        DefaultRoughness = 0.86f,
                        SpecularStrength = 0.14f,
                        GroundColorPath = MeadowTurfRoot + "Grass005_1K-JPG_Color.jpg",
                        GroundNormalPath = MeadowTurfRoot + "Grass005_1K-JPG_NormalGL.jpg",
                        GroundRoughnessPath = MeadowTurfRoot + "Grass005_1K-JPG_Roughness.jpg",
                        GroundTileMetres = 2.4f,
                        SubsoilColorPath = MeadowLoamRoot + "Ground009_1K-JPG_Color.jpg",
                        SubsoilNormalPath = MeadowLoamRoot + "Ground009_1K-JPG_NormalGL.jpg",
                        SubsoilRoughnessPath = MeadowLoamRoot + "Ground009_1K-JPG_Roughness.jpg",
                        SubsoilTileMetres = 2.0f,
                        CliffColorPath = GraniteOutcropRoot + "mossy_granite_outcrop_diff_1k.jpg",
                        CliffNormalPath = GraniteOutcropRoot + "mossy_granite_outcrop_nor_gl_1k.jpg",
                        CliffRoughnessPath = GraniteOutcropRoot + "mossy_granite_outcrop_rough_1k.jpg",
                        CliffTileMetres = 2.8f,
                    };
                case RegionBiome.Savanna:
                    return new RegionBiomeSurfaceProfile
                    {
                        Biome = RegionBiome.Savanna,
                        UsesSandstonePipeline = false,
                        CliffR = 0.58f, CliffG = 0.46f, CliffB = 0.35f,
                        GroundSkirtR = 0.48f, GroundSkirtG = 0.44f, GroundSkirtB = 0.27f,
                        MossR = 0.28f, MossG = 0.35f, MossB = 0.16f,
                        PlantTintR = 0.96f, PlantTintG = 0.92f, PlantTintB = 0.72f,
                        TopDustScale = 0.45f,
                        MossMaskScale = 0.10f,
                        DetailStrength = 0.78f,
                        NormalStrength = 1.0f,
                        DefaultRoughness = 0.90f,
                        SpecularStrength = 0.10f,
                        GroundColorPath = MeadowTurfRoot + "Grass005_1K-JPG_Color.jpg",
                        GroundNormalPath = MeadowTurfRoot + "Grass005_1K-JPG_NormalGL.jpg",
                        GroundRoughnessPath = MeadowTurfRoot + "Grass005_1K-JPG_Roughness.jpg",
                        GroundTileMetres = 2.6f,
                        SubsoilColorPath = MeadowLoamRoot + "Ground009_1K-JPG_Color.jpg",
                        SubsoilNormalPath = MeadowLoamRoot + "Ground009_1K-JPG_NormalGL.jpg",
                        SubsoilRoughnessPath = MeadowLoamRoot + "Ground009_1K-JPG_Roughness.jpg",
                        SubsoilTileMetres = 2.2f,
                        CliffColorPath = GraniteOutcropRoot + "mossy_granite_outcrop_diff_1k.jpg",
                        CliffNormalPath = GraniteOutcropRoot + "mossy_granite_outcrop_nor_gl_1k.jpg",
                        CliffRoughnessPath = GraniteOutcropRoot + "mossy_granite_outcrop_rough_1k.jpg",
                        CliffTileMetres = 2.8f,
                    };
                case RegionBiome.Taiga:
                case RegionBiome.Tundra:
                    return new RegionBiomeSurfaceProfile
                    {
                        Biome = biome,
                        UsesSandstonePipeline = false,
                        CliffR = 0.45f, CliffG = 0.46f, CliffB = 0.47f,
                        GroundSkirtR = biome == RegionBiome.Taiga ? 0.27f : 0.42f,
                        GroundSkirtG = biome == RegionBiome.Taiga ? 0.33f : 0.40f,
                        GroundSkirtB = biome == RegionBiome.Taiga ? 0.24f : 0.33f,
                        MossR = 0.24f, MossG = 0.34f, MossB = 0.18f,
                        PlantTintR = biome == RegionBiome.Taiga ? 0.66f : 0.80f,
                        PlantTintG = biome == RegionBiome.Taiga ? 0.86f : 0.82f,
                        PlantTintB = biome == RegionBiome.Taiga ? 0.66f : 0.68f,
                        TopDustScale = biome == RegionBiome.Taiga ? 0.04f : 0.12f,
                        MossMaskScale = biome == RegionBiome.Taiga ? 0.90f : 0.35f,
                        DetailStrength = 0.72f,
                        NormalStrength = 0.95f,
                        DefaultRoughness = 0.90f,
                        SpecularStrength = 0.10f,
                        GroundColorPath = MeadowTurfRoot + "Grass005_1K-JPG_Color.jpg",
                        GroundNormalPath = MeadowTurfRoot + "Grass005_1K-JPG_NormalGL.jpg",
                        GroundRoughnessPath = MeadowTurfRoot + "Grass005_1K-JPG_Roughness.jpg",
                        GroundTileMetres = 2.5f,
                        SubsoilColorPath = MeadowLoamRoot + "Ground009_1K-JPG_Color.jpg",
                        SubsoilNormalPath = MeadowLoamRoot + "Ground009_1K-JPG_NormalGL.jpg",
                        SubsoilRoughnessPath = MeadowLoamRoot + "Ground009_1K-JPG_Roughness.jpg",
                        SubsoilTileMetres = 2.2f,
                        CliffColorPath = GraniteOutcropRoot + "mossy_granite_outcrop_diff_1k.jpg",
                        CliffNormalPath = GraniteOutcropRoot + "mossy_granite_outcrop_nor_gl_1k.jpg",
                        CliffRoughnessPath = GraniteOutcropRoot + "mossy_granite_outcrop_rough_1k.jpg",
                        CliffTileMetres = 2.8f,
                    };
                case RegionBiome.Coast:
                case RegionBiome.Rock:
                case RegionBiome.Snow:
                    return new RegionBiomeSurfaceProfile
                    {
                        Biome = biome,
                        UsesSandstonePipeline = false,
                        CliffR = biome == RegionBiome.Snow ? 0.72f : (biome == RegionBiome.Coast ? 0.56f : 0.45f),
                        CliffG = biome == RegionBiome.Snow ? 0.75f : (biome == RegionBiome.Coast ? 0.52f : 0.44f),
                        CliffB = biome == RegionBiome.Snow ? 0.80f : (biome == RegionBiome.Coast ? 0.46f : 0.41f),
                        GroundSkirtR = biome == RegionBiome.Snow ? 0.82f : (biome == RegionBiome.Coast ? 0.62f : 0.45f),
                        GroundSkirtG = biome == RegionBiome.Snow ? 0.84f : (biome == RegionBiome.Coast ? 0.56f : 0.44f),
                        GroundSkirtB = biome == RegionBiome.Snow ? 0.88f : (biome == RegionBiome.Coast ? 0.44f : 0.41f),
                        MossR = 0.24f, MossG = 0.34f, MossB = 0.16f,
                        PlantTintR = 0.82f, PlantTintG = 0.95f, PlantTintB = 0.74f,
                        TopDustScale = biome == RegionBiome.Snow ? 0f : 0.22f,
                        MossMaskScale = biome == RegionBiome.Coast ? 0.15f : 0.20f,
                        DetailStrength = biome == RegionBiome.Snow ? 0.35f : 0.65f,
                        NormalStrength = 0.9f,
                        DefaultRoughness = 0.92f,
                        SpecularStrength = 0.08f,
                        GroundColorPath = MeadowLoamRoot + "Ground009_1K-JPG_Color.jpg",
                        GroundNormalPath = MeadowLoamRoot + "Ground009_1K-JPG_NormalGL.jpg",
                        GroundRoughnessPath = MeadowLoamRoot + "Ground009_1K-JPG_Roughness.jpg",
                        GroundTileMetres = 2.4f,
                        SubsoilColorPath = MeadowLoamRoot + "Ground009_1K-JPG_Color.jpg",
                        SubsoilNormalPath = MeadowLoamRoot + "Ground009_1K-JPG_NormalGL.jpg",
                        SubsoilRoughnessPath = MeadowLoamRoot + "Ground009_1K-JPG_Roughness.jpg",
                        SubsoilTileMetres = 2.2f,
                        CliffColorPath = GraniteOutcropRoot + "mossy_granite_outcrop_diff_1k.jpg",
                        CliffNormalPath = GraniteOutcropRoot + "mossy_granite_outcrop_nor_gl_1k.jpg",
                        CliffRoughnessPath = GraniteOutcropRoot + "mossy_granite_outcrop_rough_1k.jpg",
                        CliffTileMetres = 2.8f,
                    };
                default:
                    return new RegionBiomeSurfaceProfile
                    {
                        Biome = RegionBiome.Desert,
                        UsesSandstonePipeline = true,
                        CliffR = RegionLookScene.RockRed,
                        CliffG = RegionLookScene.RockGreen,
                        CliffB = RegionLookScene.RockBlue,
                        GroundSkirtR = 0.72f,
                        GroundSkirtG = 0.56f,
                        GroundSkirtB = 0.40f,
                        PlantTintR = 1f,
                        PlantTintG = 1f,
                        PlantTintB = 1f,
                        TopDustScale = 1f,
                        MossMaskScale = 0f,
                        DetailStrength = 0f,
                        NormalStrength = 0f,
                        DefaultRoughness = 0.92f,
                        SpecularStrength = 0.18f,
                    };
            }
        }

        /// <summary>
        /// 拡大地形の頂点ごとのバイオーム表面チャンネル（TEXCOORD2）を計算する。
        /// x: 草地度合い (0..1), y: 湿り気 (0..1), z: ローム/土露出度合い (0..1), w: バイオーム表面適用重み (0..1)
        /// </summary>
        public static void ComputeTerrainChannels(RegionData data, int i, bool hasSandstone,
            out float grass, out float moisture, out float subsoil, out float weight)
        {
            if (data == null || data.Height == null || i < 0 || i >= data.Height.Length)
            {
                grass = moisture = subsoil = weight = 0f;
                return;
            }

            float h = data.Height[i];
            float water = data.WaterCoverage != null ? data.WaterCoverage[i] : 0f;
            bool flooded = h <= 0f || (data.LakeDepth != null && data.LakeDepth[i] > 0f) || water >= 0.85f;
            if (flooded)
            {
                grass = moisture = subsoil = weight = 0f;
                return;
            }

            RegionBiome biome = data.Biome != null && i < data.Biome.Length ? data.Biome[i] : RegionBiome.Desert;
            float dryLand = hasSandstone
                ? (1f - RegionNoise.Clamp01(water)) * RegionNoise.Smooth(0.42f, 0.30f, data.Moisture[i])
                : 0f;

            moisture = RegionNoise.Clamp01((data.Moisture[i] - 0.14f) / 0.46f);
            float veg = BiomeVegetationFactor(biome, data.Moisture[i], data.Temperature[i]);
            float wetFade = 1f - RegionNoise.Clamp01(water);
            float structure = data.StructureCoverage != null ? data.StructureCoverage[i] : 0f;
            float nonUrban = 1f - RegionNoise.Clamp01(structure);

            grass = veg * nonUrban;
            subsoil = RegionNoise.Clamp01(1f - grass * 0.85f);

            // 砂岩表面が支配的な乾いた場所では 0 にして、既存の砂岩描画と 100% 互換を保つ
            bool sandstoneOwned = hasSandstone && (biome == RegionBiome.Desert || biome == RegionBiome.Rock);
            float baseWeight = biome == RegionBiome.Ocean || biome == RegionBiome.Lake || biome == RegionBiome.River
                ? 0f
                : (sandstoneOwned ? 0f : 1f);
            if (biome == RegionBiome.Desert && !hasSandstone)
                baseWeight = 0.65f;

            weight = baseWeight * wetFade * nonUrban * (1f - dryLand);
        }

        private static float BiomeVegetationFactor(RegionBiome biome, float moisture, float temperature)
        {
            switch (biome)
            {
                case RegionBiome.Grassland:
                    return 0.72f + 0.28f * RegionNoise.Smooth(0.18f, 0.45f, moisture);
                case RegionBiome.Forest:
                case RegionBiome.Rainforest:
                    return 0.85f + 0.15f * RegionNoise.Smooth(0.35f, 0.75f, moisture);
                case RegionBiome.Savanna:
                    return 0.42f + 0.30f * RegionNoise.Smooth(0.18f, 0.45f, moisture);
                case RegionBiome.Taiga:
                    return 0.58f + 0.25f * RegionNoise.Smooth(0.25f, 0.60f, moisture);
                case RegionBiome.Tundra:
                    return 0.28f + 0.22f * RegionNoise.Smooth(0.20f, 0.50f, moisture);
                case RegionBiome.Coast:
                    return temperature > 6f ? 0.35f * RegionNoise.Smooth(0.15f, 0.45f, moisture) : 0.18f;
                case RegionBiome.Rock:
                case RegionBiome.Snow:
                case RegionBiome.Desert:
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 拡大地形の中心付近から代表バイオーム（陸があれば陸を優先）を判定する。
        /// </summary>
        public static RegionBiome ResolvePrimaryBiome(RegionData data, float localCenterX, float localCenterZ, float cellMeters)
        {
            if (data == null || data.Biome == null || data.Biome.Length == 0) return RegionBiome.Desert;
            int n = data.Resolution;
            int cx = ClampInt((int)Math.Round(localCenterX / Math.Max(0.01f, cellMeters)), 0, n - 1);
            int cz = ClampInt((int)Math.Round(localCenterZ / Math.Max(0.01f, cellMeters)), 0, n - 1);
            RegionBiome centerBiome = data.Biome[cx + cz * n];
            if (data.MesaContactHeight != null && (centerBiome == RegionBiome.Desert || centerBiome == RegionBiome.Rock) && data.Moisture[cx + cz * n] < 0.34f)
                return RegionBiome.Desert;
            if (IsLandBiome(centerBiome)) return centerBiome;

            // 中心が川・湖・海のときは周囲半径から代表の陸バイオームを拾う
            var counts = new int[16];
            int radius = Math.Max(4, n / 32);
            int bestBiome = (int)centerBiome;
            int bestCount = 0;
            for (int dz = -radius; dz <= radius; dz += 2)
            {
                int z = cz + dz;
                if (z < 0 || z >= n) continue;
                for (int dx = -radius; dx <= radius; dx += 2)
                {
                    int x = cx + dx;
                    if (x < 0 || x >= n) continue;
                    RegionBiome b = data.Biome[x + z * n];
                    if (!IsLandBiome(b)) continue;
                    int idx = (int)b;
                    counts[idx]++;
                    if (counts[idx] > bestCount)
                    {
                        bestCount = counts[idx];
                        bestBiome = idx;
                    }
                }
            }
            return (RegionBiome)bestBiome;
        }

        public static bool IsLandBiome(RegionBiome biome)
        {
            return biome != RegionBiome.Ocean && biome != RegionBiome.River && biome != RegionBiome.Lake;
        }

        private static int ClampInt(int v, int low, int high)
        {
            return v < low ? low : (v > high ? high : v);
        }
    }
}
