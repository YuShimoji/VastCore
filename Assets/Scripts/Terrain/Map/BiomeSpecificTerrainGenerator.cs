// LEGACY: Do not modify. See docs/design/LegacyIsolation_Design.md
// Phase 3 莉･髯阪〒蜀崎ｨｭ險井ｺ亥ｮ・

using UnityEngine;
using System.Collections.Generic;
using Vastcore.Core;
using Vastcore.Utilities;

namespace Vastcore.Generation
{
    /// <summary>
    /// 繝舌う繧ｪ繝ｼ繝迚ｹ譛牙慍蠖｢逕滓・繧ｷ繧ｹ繝・Β
    /// 繝・じ繧､繝翫・繝・Φ繝励Ξ繝ｼ繝医→繝励Ο繧ｷ繝ｼ繧ｸ繝｣繝ｫ逕滓・繧堤ｵ・∩蜷医ｏ縺帙◆繝上う繝悶Μ繝・ラ繧｢繝励Ο繝ｼ繝・
    /// </summary>
    public static partial class BiomeSpecificTerrainGenerator
    {
        #region 繝・じ繧､繝翫・繝・Φ繝励Ξ繝ｼ繝育ｮ｡逅・

        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝縺斐→縺ｮ繝・じ繧､繝翫・繝・Φ繝励Ξ繝ｼ繝医Λ繧､繝悶Λ繝ｪ
        /// </summary>
        private static readonly Dictionary<BiomeType, List<DesignerTerrainTemplate>> templateLibraries = new Dictionary<BiomeType, List<DesignerTerrainTemplate>>();

        /// <summary>
        /// 繝・Φ繝励Ξ繝ｼ繝医ｒ繝ｩ繧､繝悶Λ繝ｪ縺ｫ逋ｻ骭ｲ
        /// </summary>
        public static void RegisterTemplate(DesignerTerrainTemplate template)
        {
            if (template == null) return;

            if (!templateLibraries.ContainsKey(template.associatedBiome))
            {
                templateLibraries[template.associatedBiome] = new List<DesignerTerrainTemplate>();
            }

            if (!templateLibraries[template.associatedBiome].Contains(template))
            {
                templateLibraries[template.associatedBiome].Add(template);
            }
        }

        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝縺ｫ驕ｩ縺励◆繝・Φ繝励Ξ繝ｼ繝医ｒ蜿門ｾ・
        /// </summary>
        public static DesignerTerrainTemplate GetRandomTemplateForBiome(BiomeType biomeType, Vector3 worldPosition)
        {
            if (!templateLibraries.ContainsKey(biomeType) || templateLibraries[biomeType].Count == 0)
            {
                return null;
            }

            var templates = templateLibraries[biomeType];
            int seed = Mathf.FloorToInt(worldPosition.x + worldPosition.z);
            System.Random random = new System.Random(seed);

            return templates[random.Next(templates.Count)];
        }

        #endregion

        #region 繝｡繧､繝ｳ逕滓・繝｡繧ｽ繝・ラ

        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝繧ｿ繧､繝励↓蠢懊§縺溷慍蠖｢逕滓・繧貞ｮ溯｡・
        /// 繝・じ繧､繝翫・繝・Φ繝励Ξ繝ｼ繝亥━蜈医√ヵ繧ｩ繝ｼ繝ｫ繝舌ャ繧ｯ縺ｧ繝励Ο繧ｷ繝ｼ繧ｸ繝｣繝ｫ逕滓・
        /// </summary>
        public static void GenerateTerrainForBiome(float[,] heightmap, BiomeType biomeType, BiomeDefinition biomeDefinition, Vector3 worldPosition)
        {
            // 繝・じ繧､繝翫・繝・Φ繝励Ξ繝ｼ繝医・繝ｼ繧ｹ縺ｮ逕滓・繧定ｩｦ陦・
            bool templateApplied = TryApplyDesignerTemplates(heightmap, biomeType, worldPosition);

            // 繝・Φ繝励Ξ繝ｼ繝医′驕ｩ逕ｨ縺ｧ縺阪↑縺九▲縺溷ｴ蜷医・縺ｿ繝励Ο繧ｷ繝ｼ繧ｸ繝｣繝ｫ逕滓・
            if (!templateApplied)
            {
                GenerateProceduralTerrain(heightmap, biomeType, biomeDefinition, worldPosition);
            }

            // 蜈ｱ騾壹・蠕悟・逅・
            ApplyBiomePostProcessing(heightmap, biomeDefinition, worldPosition);
        }

        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝蜈ｱ騾壹・蠕悟・逅・ｼ医せ繧ｿ繝門ｮ溯｣・ｼ峨・
        /// 蠢・ｦ√↓蠢懊§縺ｦ繧ｹ繝繝ｼ繧ｸ繝ｳ繧ｰ繧・｢・阜繝悶Ξ繝ｳ繝臥ｭ峨ｒ螳溯｣・庄閭ｽ縲・
        /// </summary>
        private static void ApplyBiomePostProcessing(float[,] heightmap, BiomeDefinition biomeDefinition, Vector3 worldPosition)
        {
            // 迴ｾ谿ｵ髫弱〒縺ｯ菴輔ｂ縺励↑縺・ｼ亥ｰ・擂諡｡蠑ｵ逕ｨ・峨・
        }

        /// <summary>
        /// 繝・じ繧､繝翫・繝・Φ繝励Ξ繝ｼ繝医・驕ｩ逕ｨ繧定ｩｦ陦・
        /// </summary>
        private static bool TryApplyDesignerTemplates(float[,] heightmap, BiomeType biomeType, Vector3 worldPosition)
        {
            var template = GetRandomTemplateForBiome(biomeType, worldPosition);

            if (template != null && template.CanApplyAt(worldPosition, GetTerrainHeightAt(worldPosition), GetTerrainSlopeAt(worldPosition)))
            {
                // 繝・Φ繝励Ξ繝ｼ繝医・繝ｼ繧ｹ逕滓・繧貞ｮ溯｡・
                float seed = worldPosition.x * 0.01f + worldPosition.z * 0.01f;
                ApplyDesignerTemplate(heightmap, template, worldPosition, seed);
                return true;
            }

            return false;
        }
        private static void ApplyDesignerTemplate(float[,] heightmap, DesignerTerrainTemplate template, Vector3 worldPosition, float seed)
        {
            if (heightmap == null || template == null)
            {
                return;
            }

            var templateData = template.GetHeightmapData();
            if (templateData == null)
            {
                return;
            }

            int targetWidth = heightmap.GetLength(0);
            int targetHeight = heightmap.GetLength(1);
            int sourceWidth = templateData.GetLength(0);
            int sourceHeight = templateData.GetLength(1);

            if (sourceWidth == 0 || sourceHeight == 0)
            {
                return;
            }

            for (int y = 0; y < targetHeight; y++)
            {
                float v = targetHeight > 1 ? (float)y / (targetHeight - 1) : 0f;
                int sy = Mathf.Clamp(Mathf.RoundToInt(v * (sourceHeight - 1)), 0, sourceHeight - 1);

                for (int x = 0; x < targetWidth; x++)
                {
                    float u = targetWidth > 1 ? (float)x / (targetWidth - 1) : 0f;
                    int sx = Mathf.Clamp(Mathf.RoundToInt(u * (sourceWidth - 1)), 0, sourceWidth - 1);

                    float templateHeight = templateData[sx, sy];
                    float noise = Mathf.PerlinNoise((worldPosition.x + x + seed) * 0.01f, (worldPosition.z + y + seed) * 0.01f);
                    float blend = Mathf.Clamp01(template.variationStrength * 0.5f + noise * template.variationStrength * 0.5f);

                    heightmap[x, y] = Mathf.Clamp01(Mathf.Lerp(heightmap[x, y], templateHeight, blend));
                }
            }
        }


        /// 繝励Ο繧ｷ繝ｼ繧ｸ繝｣繝ｫ蝨ｰ蠖｢逕滓・・医ヵ繧ｩ繝ｼ繝ｫ繝舌ャ繧ｯ・・
        /// </summary>
        private static void GenerateProceduralTerrain(float[,] heightmap, BiomeType biomeType, BiomeDefinition biomeDefinition, Vector3 worldPosition)
        {
            switch (biomeType)
            {
                case BiomeType.Desert:
                    GenerateDesertTerrain(heightmap, biomeDefinition, worldPosition);
                    break;
                case BiomeType.Forest:
                    GenerateForestTerrain(heightmap, biomeDefinition, worldPosition);
                    break;
                case BiomeType.Mountain:
                    GenerateMountainTerrain(heightmap, biomeDefinition, worldPosition);
                    break;
                case BiomeType.Coastal:
                    GenerateCoastalTerrain(heightmap, biomeDefinition, worldPosition);
                    break;
                case BiomeType.Polar:
                    GeneratePolarTerrain(heightmap, biomeDefinition, worldPosition);
                    break;
                case BiomeType.Grassland:
                    GenerateGrasslandTerrain(heightmap, biomeDefinition, worldPosition);
                    break;
                default:
                    VastcoreLogger.Instance.LogWarning("BiomeTerrain", $"Unknown biome type: {biomeType}");
                    break;
            }
        }

        /// <summary>
        /// 蝨ｰ蠖｢菫ｮ豁｣繧帝←逕ｨ
        /// </summary>
        private static void ApplyTerrainModifiers(float[,] heightmap, TerrainModificationData modifiers)
        {
            if (modifiers == null) return;

            // 鬮伜ｺｦ荵玲焚繧帝←逕ｨ
            if (modifiers.heightMultiplier != 1f)
            {
                ApplyHeightMultiplier(heightmap, modifiers.heightMultiplier);
            }

            // 邊励＆隱ｿ謨ｴ
            if (modifiers.roughnessMultiplier != 1f)
            {
                ApplyRoughness(heightmap, modifiers.roughnessMultiplier);
            }

            // 迚ｹ谿雁慍蠖｢逕滓・
            if (modifiers.enableDuneGeneration)
            {
                GenerateDunesForBiome(heightmap, modifiers);
            }

            if (modifiers.enableRidgeGeneration)
            {
                GenerateRidgesForBiome(heightmap, modifiers);
            }

            if (modifiers.enablePeakGeneration)
            {
                GeneratePeaksForBiome(heightmap, modifiers);
            }

            if (modifiers.enableBeachGeneration)
            {
                GenerateBeachForBiome(heightmap, modifiers);
            }

            if (modifiers.enableGlacialGeneration)
            {
                GenerateGlacialTerrainForBiome(heightmap, modifiers);
            }

            if (modifiers.enableRollingHills)
            {
                GenerateRollingHillsForBiome(heightmap, modifiers);
            }

            // 豬ｸ鬟溘・蝣・ｩ・
            if (modifiers.erosionStrength > 0f)
            {
                ApplyErosion(heightmap, modifiers.erosionStrength);
            }

            if (modifiers.sedimentationRate > 0f)
            {
                ApplySedimentation(heightmap, modifiers.sedimentationRate);
            }
        }

        /// <summary>
        /// 鬮伜ｺｦ荵玲焚繧帝←逕ｨ
        /// </summary>
        private static void ApplyHeightMultiplier(float[,] heightmap, float multiplier)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    heightmap[x, y] *= multiplier;
                }
            }
        }

        /// <summary>
        /// 邊励＆繧帝←逕ｨ
        /// </summary>
        private static void ApplyRoughness(float[,] heightmap, float roughnessMultiplier)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) - 0.5f;
                    heightmap[x, y] += noise * roughnessMultiplier * 10f;
                }
            }
        }
        #endregion

        #region 繝ｦ繝ｼ繝・ぅ繝ｪ繝・ぅ繝｡繧ｽ繝・ラ

        /// <summary>
        /// 謖・ｮ壻ｽ咲ｽｮ縺ｮ蝨ｰ蠖｢鬮伜ｺｦ繧貞叙蠕暦ｼ育ｰ｡譏灘ｮ溯｣・ｼ・
        /// </summary>
        private static float GetTerrainHeightAt(Vector3 worldPosition)
        {
            // 螳滄圀縺ｮ螳溯｣・〒縺ｯTerrain.SampleHeight繧剃ｽｿ逕ｨ
            return worldPosition.y;
        }

        /// <summary>
        /// 謖・ｮ壻ｽ咲ｽｮ縺ｮ蝨ｰ蠖｢蛯ｾ譁懊ｒ蜿門ｾ暦ｼ育ｰ｡譏灘ｮ溯｣・ｼ・
        /// </summary>
        private static float GetTerrainSlopeAt(Vector3 worldPosition)
        {
            // 螳滄圀縺ｮ螳溯｣・〒縺ｯTerrain.SampleSlope繧剃ｽｿ逕ｨ
            return 0f;
        }

        /// <summary>
        /// 豬ｸ鬟溘ｒ驕ｩ逕ｨ
        /// </summary>
        private static void ApplyErosion(float[,] heightmap, float strength)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float currentHeight = heightmap[x, y];
                    float avgNeighbor = (
                        heightmap[x-1, y] + heightmap[x+1, y] +
                        heightmap[x, y-1] + heightmap[x, y+1]
                    ) / 4f;

                    if (currentHeight > avgNeighbor)
                    {
                        float erosion = (currentHeight - avgNeighbor) * strength * 0.1f;
                        heightmap[x, y] -= erosion;
                    }
                }
            }
        }

        /// <summary>
        /// 蝣・ｩ阪ｒ驕ｩ逕ｨ
        /// </summary>
        private static void ApplySedimentation(float[,] heightmap, float rate)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float currentHeight = heightmap[x, y];
                    float avgNeighbor = (
                        heightmap[x-1, y] + heightmap[x+1, y] +
                        heightmap[x, y-1] + heightmap[x, y+1]
                    ) / 4f;

                    if (currentHeight < avgNeighbor)
                    {
                        float deposition = (avgNeighbor - currentHeight) * rate * 0.1f;
                        heightmap[x, y] += deposition;
                    }
                }
            }
        }

        #endregion

        #region 迚ｹ谿雁慍蠖｢逕滓・繝｡繧ｽ繝・ラ

        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝逕ｨ縺ｮ遐ゆｸ倡函謌・
        /// </summary>
        private static void GenerateDunesForBiome(float[,] heightmap, TerrainModificationData modifiers)
        {
            Vector3 worldPosition = Vector3.zero; // 莉ｮ螳壼､
            GenerateDuneSystems(heightmap, worldPosition);
        }

        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝逕ｨ縺ｮ蟆ｾ譬ｹ逕滓・
        /// </summary>
        private static void GenerateRidgesForBiome(float[,] heightmap, TerrainModificationData modifiers)
        {
            Vector3 worldPosition = Vector3.zero;
            GenerateForestRidges(heightmap, worldPosition);
        }

        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝逕ｨ縺ｮ螻ｱ鬆ら函謌・
        /// </summary>
        private static void GeneratePeaksForBiome(float[,] heightmap, TerrainModificationData modifiers)
        {
            Vector3 worldPosition = Vector3.zero;
            GenerateMountainPeaks(heightmap, worldPosition);
        }

        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝逕ｨ縺ｮ豬ｷ蟯ｸ逕滓・
        /// </summary>
        private static void GenerateBeachForBiome(float[,] heightmap, TerrainModificationData modifiers)
        {
            GenerateCoastalTerrain(heightmap, null, Vector3.zero);
        }

        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝逕ｨ縺ｮ豌ｷ豐ｳ蝨ｰ蠖｢逕滓・
        /// </summary>
        private static void GenerateGlacialTerrainForBiome(float[,] heightmap, TerrainModificationData modifiers)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            // 邁｡譏鍋噪縺ｪ豌ｷ豐ｳ蟷ｳ貊大喧
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float avg = (
                        heightmap[x-1, y] + heightmap[x+1, y] +
                        heightmap[x, y-1] + heightmap[x, y+1]
                    ) / 4f;

                    heightmap[x, y] = Mathf.Lerp(heightmap[x, y], avg, modifiers.glacialSmoothness);
                    heightmap[x, y] -= modifiers.glacialDepth;
                }
            }
        }

        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝逕ｨ縺ｮ襍ｷ莨丈ｸ倬匏逕滓・
        /// </summary>
        private static void GenerateRollingHillsForBiome(float[,] heightmap, TerrainModificationData modifiers)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float hillNoise1 = Mathf.PerlinNoise(x * modifiers.hillFrequency, y * modifiers.hillFrequency);
                    float hillNoise2 = Mathf.PerlinNoise(x * modifiers.hillFrequency * 2f, y * modifiers.hillFrequency * 2f) * 0.5f;
                    float hillHeight = (hillNoise1 + hillNoise2) * modifiers.hillAmplitude;

                    heightmap[x, y] += hillHeight;
                }
            }
        }

        #endregion
        
    }
}
