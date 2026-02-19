// LEGACY: Do not modify. See docs/design/LegacyIsolation_Design.md
// Phase 3 莉･髯阪〒蜀崎ｨｭ險井ｺ亥ｮ・

using UnityEngine;
using System.Collections.Generic;
using Vastcore.Core;

namespace Vastcore.Generation
{
    /// <summary>
    /// 繝舌う繧ｪ繝ｼ繝迚ｹ譛牙慍蠖｢逕滓・繧ｷ繧ｹ繝・Β
    /// 繝・じ繧､繝翫・繝・Φ繝励Ξ繝ｼ繝医→繝励Ο繧ｷ繝ｼ繧ｸ繝｣繝ｫ逕滓・繧堤ｵ・∩蜷医ｏ縺帙◆繝上う繝悶Μ繝・ラ繧｢繝励Ο繝ｼ繝・
    /// </summary>
    public static class BiomeSpecificTerrainGenerator
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
                    Debug.LogWarning($"Unknown biome type: {biomeType}");
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
        
        #region 遐よｼ蝨ｰ蠖｢逕滓・
        
        /// <summary>
        /// 遐よｼ蝨ｰ蠖｢繧堤函謌撰ｼ育ゆｸ倥√が繧｢繧ｷ繧ｹ縲∝ｲｩ遏ｳ髴ｲ蜃ｺ・・
        /// </summary>
        public static void GenerateDesertTerrain(float[,] heightmap, BiomeDefinition biomeDefinition, Vector3 worldPosition)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 遐ゆｸ倥す繧ｹ繝・Β縺ｮ逕滓・
            GenerateDuneSystems(heightmap, worldPosition);
            
            // 蟯ｩ遏ｳ髴ｲ蜃ｺ縺ｮ逕滓・
            GenerateRockOutcrops(heightmap, worldPosition, 0.05f);
            
            // 繧ｪ繧｢繧ｷ繧ｹ縺ｮ逕滓・
            GenerateOases(heightmap, worldPosition, 0.02f);
            
            // 遐よｼ迚ｹ譛峨・豬ｸ鬟溘ヱ繧ｿ繝ｼ繝ｳ
            ApplyDesertErosion(heightmap, biomeDefinition.terrainModifiers.erosionStrength);
            
            // 鬚ｨ縺ｫ繧医ｋ遐ゅ・蝣・ｩ・
            ApplyWindDeposition(heightmap, worldPosition);
        }
        
        /// <summary>
        /// 遐ゆｸ倥す繧ｹ繝・Β繧堤函謌・
        /// </summary>
        private static void GenerateDuneSystems(float[,] heightmap, Vector3 worldPosition)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 荳ｻ隕・｢ｨ蜷代″・郁･ｿ縺九ｉ譚ｱ縺ｸ・・
            Vector2 windDirection = new Vector2(1f, 0.2f).normalized;
            
            // 隍・焚縺ｮ遐ゆｸ倥メ繧ｧ繝ｼ繝ｳ繧堤函謌・
            int duneChainCount = Random.Range(3, 7);
            
            for (int chain = 0; chain < duneChainCount; chain++)
            {
                // 遐ゆｸ倥メ繧ｧ繝ｼ繝ｳ縺ｮ髢句ｧ狗せ
                Vector2 chainStart = new Vector2(
                    Random.Range(0f, width * 0.3f),
                    Random.Range(0f, height)
                );
                
                // 遐ゆｸ倥・謨ｰ
                int dunesInChain = Random.Range(5, 12);
                float duneSpacing = Random.Range(80f, 150f);
                
                for (int dune = 0; dune < dunesInChain; dune++)
                {
                    Vector2 duneCenter = chainStart + windDirection * (dune * duneSpacing);
                    
                    // 蠅・阜繝√ぉ繝・け
                    if (duneCenter.x < 0 || duneCenter.x >= width || duneCenter.y < 0 || duneCenter.y >= height)
                        continue;
                    
                    // 遐ゆｸ倥・蠖｢迥ｶ繝代Λ繝｡繝ｼ繧ｿ
                    float duneHeight = Random.Range(15f, 35f);
                    float duneWidth = Random.Range(60f, 120f);
                    float duneLength = Random.Range(100f, 200f);
                    
                    // 髱槫ｯｾ遘ｰ縺ｪ遐ゆｸ伜ｽ｢迥ｶ・磯｢ｨ荳雁・縺ｯ邱ｩ繧・°縲・｢ｨ荳句・縺ｯ諤･・・
                    GenerateAsymmetricDune(heightmap, duneCenter, duneHeight, duneWidth, duneLength, windDirection);
                }
            }
        }
        
        /// <summary>
        /// 髱槫ｯｾ遘ｰ遐ゆｸ倥ｒ逕滓・
        /// </summary>
        private static void GenerateAsymmetricDune(float[,] heightmap, Vector2 center, float height, float width, float length, Vector2 windDirection)
        {
            int mapWidth = heightmap.GetLength(0);
            int mapHeight = heightmap.GetLength(1);
            
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    Vector2 point = new Vector2(x, y);
                    Vector2 offset = point - center;
                    
                    // 鬚ｨ蜷代″縺ｫ蟇ｾ縺吶ｋ逶ｸ蟇ｾ菴咲ｽｮ
                    float alongWind = Vector2.Dot(offset, windDirection);
                    float acrossWind = Vector2.Dot(offset, new Vector2(-windDirection.y, windDirection.x));
                    
                    // 遐ゆｸ倥・蠖ｱ髻ｿ遽・峇繝√ぉ繝・け
                    if (Mathf.Abs(alongWind) > length * 0.5f || Mathf.Abs(acrossWind) > width * 0.5f)
                        continue;
                    
                    // 髱槫ｯｾ遘ｰ繝励Ο繝輔ぃ繧､繝ｫ
                    float windwardSlope = 0.3f;  // 鬚ｨ荳雁・縺ｮ邱ｩ繧・°縺ｪ蛯ｾ譁・
                    float leewardSlope = 0.8f;   // 鬚ｨ荳句・縺ｮ諤･縺ｪ蛯ｾ譁・
                    
                    float normalizedAlong = alongWind / (length * 0.5f);
                    float normalizedAcross = acrossWind / (width * 0.5f);
                    
                    // 讓ｪ譁ｹ蜷代・繝励Ο繝輔ぃ繧､繝ｫ・医ぎ繧ｦ繧ｷ繧｢繝ｳ・・
                    float crossProfile = Mathf.Exp(-normalizedAcross * normalizedAcross * 2f);
                    
                    // 邵ｦ譁ｹ蜷代・繝励Ο繝輔ぃ繧､繝ｫ・磯撼蟇ｾ遘ｰ・・
                    float longProfile;
                    if (normalizedAlong < 0) // 鬚ｨ荳雁・
                    {
                        longProfile = Mathf.Exp(-normalizedAlong * normalizedAlong / (windwardSlope * windwardSlope));
                    }
                    else // 鬚ｨ荳句・
                    {
                        longProfile = Mathf.Exp(-normalizedAlong * normalizedAlong / (leewardSlope * leewardSlope));
                    }
                    
                    float duneHeight = height * crossProfile * longProfile;
                    heightmap[x, y] += duneHeight;
                }
            }
        }
        
        /// <summary>
        /// 蟯ｩ遏ｳ髴ｲ蜃ｺ繧堤函謌・
        /// </summary>
        private static void GenerateRockOutcrops(float[,] heightmap, Vector3 worldPosition, float density)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            int outcropCount = Mathf.RoundToInt(width * height * density / 10000f);
            
            for (int i = 0; i < outcropCount; i++)
            {
                Vector2 outcropCenter = new Vector2(
                    Random.Range(0, width),
                    Random.Range(0, height)
                );
                
                float outcropRadius = Random.Range(20f, 60f);
                float outcropHeight = Random.Range(25f, 80f);
                
                // 荳崎ｦ丞援縺ｪ蟯ｩ遏ｳ蠖｢迥ｶ
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), outcropCenter);
                        
                        if (distance < outcropRadius)
                        {
                            // 繝弱う繧ｺ縺ｫ繧医ｋ荳崎ｦ丞援諤ｧ
                            float noise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                            float effectiveRadius = outcropRadius * (0.7f + noise * 0.6f);
                            
                            if (distance < effectiveRadius)
                            {
                                float falloff = 1f - (distance / effectiveRadius);
                                falloff = falloff * falloff; // 莠梧ｬ｡髢｢謨ｰ逧・ｸ幄｡ｰ
                                
                                float rockHeight = outcropHeight * falloff * noise;
                                heightmap[x, y] += rockHeight;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 繧ｪ繧｢繧ｷ繧ｹ繧堤函謌・
        /// </summary>
        private static void GenerateOases(float[,] heightmap, Vector3 worldPosition, float density)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            int oasisCount = Mathf.RoundToInt(width * height * density / 10000f);
            
            for (int i = 0; i < oasisCount; i++)
            {
                Vector2 oasisCenter = new Vector2(
                    Random.Range(width * 0.2f, width * 0.8f),
                    Random.Range(height * 0.2f, height * 0.8f)
                );
                
                float oasisRadius = Random.Range(40f, 100f);
                float depressionDepth = Random.Range(5f, 15f);
                
                // 繧ｪ繧｢繧ｷ繧ｹ蜻ｨ霎ｺ縺ｮ遯ｪ蝨ｰ
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), oasisCenter);
                        
                        if (distance < oasisRadius)
                        {
                            float falloff = 1f - (distance / oasisRadius);
                            falloff = Mathf.SmoothStep(0f, 1f, falloff);
                            
                            heightmap[x, y] -= depressionDepth * falloff;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 遐よｼ迚ｹ譛峨・豬ｸ鬟溘ｒ驕ｩ逕ｨ
        /// </summary>
        private static void ApplyDesertErosion(float[,] heightmap, float erosionStrength)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 鬚ｨ縺ｫ繧医ｋ豬ｸ鬟滂ｼ井ｸｻ縺ｫ鬮倥＞驛ｨ蛻・ｼ・
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float currentHeight = heightmap[x, y];
                    
                    // 蜻ｨ霎ｺ縺ｮ蟷ｳ蝮・ｫ伜ｺｦ
                    float avgHeight = (
                        heightmap[x-1, y] + heightmap[x+1, y] +
                        heightmap[x, y-1] + heightmap[x, y+1]
                    ) / 4f;
                    
                    // 鬮倥＞驛ｨ蛻・⊇縺ｩ豬ｸ鬟溘＆繧後ｄ縺吶＞
                    if (currentHeight > avgHeight)
                    {
                        float erosion = (currentHeight - avgHeight) * erosionStrength * 0.1f;
                        heightmap[x, y] -= erosion;
                    }
                }
            }
        }
        
        /// <summary>
        /// 鬚ｨ縺ｫ繧医ｋ遐ゅ・蝣・ｩ阪ｒ驕ｩ逕ｨ
        /// </summary>
        private static void ApplyWindDeposition(float[,] heightmap, Vector3 worldPosition)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            Vector2 windDirection = new Vector2(1f, 0.2f).normalized;
            
            // 鬚ｨ荳句・縺ｸ縺ｮ遐ゅ・蝣・ｩ・
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    // 鬚ｨ荳雁・縺ｮ鬮伜ｺｦ繧偵メ繧ｧ繝・け
                    int windUpX = x - Mathf.RoundToInt(windDirection.x * 3f);
                    int windUpY = y - Mathf.RoundToInt(windDirection.y * 3f);
                    
                    if (windUpX >= 0 && windUpX < width && windUpY >= 0 && windUpY < height)
                    {
                        float windUpHeight = heightmap[windUpX, windUpY];
                        float currentHeight = heightmap[x, y];
                        
                        // 鬚ｨ荳翫′鬮倥＞蝣ｴ蜷医∫ゅ′蝣・ｩ・
                        if (windUpHeight > currentHeight + 5f)
                        {
                            float deposition = (windUpHeight - currentHeight) * 0.02f;
                            heightmap[x, y] += deposition;
                        }
                    }
                }
            }
        }
        
        #endregion
        
        #region 譽ｮ譫怜慍蠖｢逕滓・
        
        /// <summary>
        /// 譽ｮ譫怜慍蠖｢繧堤函謌撰ｼ亥ｰｾ譬ｹ縲∬ｰｷ縲∵｣ｮ譫励・襍ｷ莨擾ｼ・
        /// </summary>
        public static void GenerateForestTerrain(float[,] heightmap, BiomeDefinition biomeDefinition, Vector3 worldPosition)
        {
            // 譽ｮ譫礼音譛峨・襍ｷ莨上ヱ繧ｿ繝ｼ繝ｳ
            GenerateForestRidges(heightmap, worldPosition);
            
            // 豐ｳ蟾晁ｰｷ縺ｮ逕滓・
            GenerateForestValleys(heightmap, worldPosition);
            
            // 譽ｮ譫励・蟆上＆縺ｪ襍ｷ莨・
            GenerateForestUndulation(heightmap);
            
            // 豌ｴ縺ｫ繧医ｋ豬ｸ鬟滂ｼ域｣ｮ譫励・豬ｸ鬟溘′蟆代↑縺・ｼ・
            ApplyForestErosion(heightmap, biomeDefinition.terrainModifiers.erosionStrength);
            
            // 譛画ｩ溽黄縺ｫ繧医ｋ蝨溷｣悟・ｩ・
            ApplyOrganicSedimentation(heightmap);
        }
        
        /// <summary>
        /// 譽ｮ譫励・蟆ｾ譬ｹ繧堤函謌・
        /// </summary>
        private static void GenerateForestRidges(float[,] heightmap, Vector3 worldPosition)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 隍・焚縺ｮ蟆ｾ譬ｹ繧ｷ繧ｹ繝・Β
            int ridgeCount = Random.Range(2, 5);
            
            for (int ridge = 0; ridge < ridgeCount; ridge++)
            {
                // 蟆ｾ譬ｹ縺ｮ譁ｹ蜷托ｼ医Λ繝ｳ繝繝・・
                float ridgeAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector2 ridgeDirection = new Vector2(Mathf.Cos(ridgeAngle), Mathf.Sin(ridgeAngle));
                
                // 蟆ｾ譬ｹ縺ｮ髢句ｧ狗せ
                Vector2 ridgeStart = new Vector2(
                    Random.Range(0f, width),
                    Random.Range(0f, height)
                );
                
                float ridgeLength = Random.Range(200f, 400f);
                float ridgeHeight = Random.Range(30f, 60f);
                float ridgeWidth = Random.Range(80f, 150f);
                
                // 蟆ｾ譬ｹ縺ｫ豐ｿ縺｣縺ｦ鬮伜ｺｦ繧定ｿｽ蜉
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Vector2 point = new Vector2(x, y);
                        Vector2 toPoint = point - ridgeStart;
                        
                        // 蟆ｾ譬ｹ譁ｹ蜷代∈縺ｮ謚募ｽｱ
                        float alongRidge = Vector2.Dot(toPoint, ridgeDirection);
                        float acrossRidge = Vector2.Dot(toPoint, new Vector2(-ridgeDirection.y, ridgeDirection.x));
                        
                        // 蟆ｾ譬ｹ縺ｮ遽・峇蜀・°繝√ぉ繝・け
                        if (alongRidge >= 0 && alongRidge <= ridgeLength && Mathf.Abs(acrossRidge) <= ridgeWidth * 0.5f)
                        {
                            // 蟆ｾ譬ｹ繝励Ο繝輔ぃ繧､繝ｫ
                            float longProfile = 1f - Mathf.Abs(alongRidge - ridgeLength * 0.5f) / (ridgeLength * 0.5f);
                            float crossProfile = 1f - Mathf.Abs(acrossRidge) / (ridgeWidth * 0.5f);
                            
                            // 繝弱う繧ｺ縺ｫ繧医ｋ閾ｪ辟ｶ縺ｪ螟牙虚
                            float noise = Mathf.PerlinNoise(x * 0.02f, y * 0.02f);
                            
                            float elevation = ridgeHeight * longProfile * crossProfile * (0.7f + noise * 0.6f);
                            heightmap[x, y] += elevation;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 譽ｮ譫励・隹ｷ繧堤函謌・
        /// </summary>
        private static void GenerateForestValleys(float[,] heightmap, Vector3 worldPosition)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 豐ｳ蟾晁ｰｷ縺ｮ逕滓・
            int valleyCount = Random.Range(1, 3);
            
            for (int valley = 0; valley < valleyCount; valley++)
            {
                // 隹ｷ縺ｮ邨瑚ｷｯ繧堤函謌撰ｼ郁寐陦後☆繧句ｷ晢ｼ・
                List<Vector2> valleyPath = GenerateMeanderingPath(width, height, Random.Range(5, 10));
                
                float valleyDepth = Random.Range(15f, 30f);
                float valleyWidth = Random.Range(60f, 120f);
                
                // 隹ｷ縺ｫ豐ｿ縺｣縺ｦ蝨ｰ蠖｢繧貞炎繧・
                foreach (Vector2 pathPoint in valleyPath)
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            float distance = Vector2.Distance(new Vector2(x, y), pathPoint);
                            
                            if (distance < valleyWidth * 0.5f)
                            {
                                float falloff = 1f - (distance / (valleyWidth * 0.5f));
                                falloff = Mathf.SmoothStep(0f, 1f, falloff);
                                
                                heightmap[x, y] -= valleyDepth * falloff;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 陋・｡後☆繧狗ｵ瑚ｷｯ繧堤函謌・
        /// </summary>
        private static List<Vector2> GenerateMeanderingPath(int width, int height, int segmentCount)
        {
            List<Vector2> path = new List<Vector2>();
            
            Vector2 start = new Vector2(Random.Range(0f, width * 0.2f), Random.Range(height * 0.2f, height * 0.8f));
            Vector2 end = new Vector2(Random.Range(width * 0.8f, width), Random.Range(height * 0.2f, height * 0.8f));
            
            path.Add(start);
            
            for (int i = 1; i < segmentCount; i++)
            {
                float t = (float)i / segmentCount;
                Vector2 basePoint = Vector2.Lerp(start, end, t);
                
                // 陋・｡後・縺溘ａ縺ｮ繧ｪ繝輔そ繝・ヨ
                float meanderAmplitude = Vector2.Distance(start, end) * 0.2f;
                float meanderFreq = 3f;
                Vector2 perpendicular = Vector2.Perpendicular(end - start).normalized;
                Vector2 meanderOffset = perpendicular * Mathf.Sin(t * meanderFreq * Mathf.PI) * meanderAmplitude;
                
                path.Add(basePoint + meanderOffset);
            }
            
            path.Add(end);
            return path;
        }
        
        /// <summary>
        /// 譽ｮ譫励・蟆上＆縺ｪ襍ｷ莨上ｒ逕滓・
        /// </summary>
        private static void GenerateForestUndulation(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 隍・焚繧ｹ繧ｱ繝ｼ繝ｫ縺ｮ繝弱う繧ｺ繧帝㍾縺ｭ蜷医ｏ縺・
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float undulation = 0f;
                    
                    // 螟ｧ縺阪↑襍ｷ莨・
                    undulation += Mathf.PerlinNoise(x * 0.008f, y * 0.008f) * 20f;
                    
                    // 荳ｭ遞句ｺｦ縺ｮ襍ｷ莨・
                    undulation += Mathf.PerlinNoise(x * 0.02f, y * 0.02f) * 8f;
                    
                    // 蟆上＆縺ｪ襍ｷ莨・
                    undulation += Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 3f;
                    
                    heightmap[x, y] += undulation - 15f; // 蟷ｳ蝮・ｒ荳九￡繧・
                }
            }
        }
        
        /// <summary>
        /// 譽ｮ譫礼音譛峨・豬ｸ鬟溘ｒ驕ｩ逕ｨ
        /// </summary>
        private static void ApplyForestErosion(float[,] heightmap, float erosionStrength)
        {
            // 譽ｮ譫励・讀咲函縺ｫ繧医ｊ豬ｸ鬟溘′謚大宛縺輔ｌ繧・
            float reducedErosion = erosionStrength * 0.3f;
            
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
                        float erosion = (currentHeight - avgNeighbor) * reducedErosion * 0.05f;
                        heightmap[x, y] -= erosion;
                    }
                }
            }
        }
        
        /// <summary>
        /// 譛画ｩ溽黄縺ｫ繧医ｋ蝨溷｣悟・ｩ阪ｒ驕ｩ逕ｨ
        /// </summary>
        private static void ApplyOrganicSedimentation(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 菴主慍縺ｸ縺ｮ譛画ｩ溽黄蝣・ｩ・
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float currentHeight = heightmap[x, y];
                    float avgNeighbor = (
                        heightmap[x-1, y] + heightmap[x+1, y] +
                        heightmap[x, y-1] + heightmap[x, y+1]
                    ) / 4f;
                    
                    // 菴主慍縺ｫ譛画ｩ溽黄縺悟・ｩ・
                    if (currentHeight < avgNeighbor)
                    {
                        float deposition = (avgNeighbor - currentHeight) * 0.1f;
                        heightmap[x, y] += deposition;
                    }
                }
            }
        }
        
        #endregion
        
        #region 螻ｱ蟯ｳ蝨ｰ蠖｢逕滓・
        
        /// <summary>
        /// 螻ｱ蟯ｳ蝨ｰ蠖｢繧堤函謌撰ｼ亥ｱｱ鬆ゅ∝ｰｾ譬ｹ縲∵･蟲ｻ縺ｪ隹ｷ・・
        /// </summary>
        public static void GenerateMountainTerrain(float[,] heightmap, BiomeDefinition biomeDefinition, Vector3 worldPosition)
        {
            // 荳ｻ隕∝ｱｱ鬆ゅ・逕滓・
            GenerateMountainPeaks(heightmap, worldPosition);
            
            // 螻ｱ閼亥ｰｾ譬ｹ縺ｮ逕滓・
            GenerateMountainRidges(heightmap, worldPosition);
            
            // 諤･蟲ｻ縺ｪ隹ｷ縺ｮ逕滓・
            GenerateMountainValleys(heightmap, worldPosition);
            
            // 蟯ｩ遏ｳ蟠ｩ關ｽ蝨ｰ蠖｢
            GenerateRockfallTerrain(heightmap);
            
            // 螻ｱ蟯ｳ迚ｹ譛峨・豬ｸ鬟・
            ApplyMountainErosion(heightmap, biomeDefinition.terrainModifiers.erosionStrength);
        }
        
        /// <summary>
        /// 螻ｱ鬆ゅｒ逕滓・
        /// </summary>
        private static void GenerateMountainPeaks(float[,] heightmap, Vector3 worldPosition)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            int peakCount = Random.Range(2, 5);
            
            for (int peak = 0; peak < peakCount; peak++)
            {
                Vector2 peakCenter = new Vector2(
                    Random.Range(width * 0.2f, width * 0.8f),
                    Random.Range(height * 0.2f, height * 0.8f)
                );
                
                float peakHeight = Random.Range(150f, 300f);
                float peakRadius = Random.Range(80f, 150f);
                
                // 螻ｱ鬆ゅ・蠖｢迥ｶ・域･蟲ｻ・・
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), peakCenter);
                        
                        if (distance < peakRadius)
                        {
                            // 諤･蟲ｻ縺ｪ螻ｱ鬆ゅ・繝ｭ繝輔ぃ繧､繝ｫ
                            float falloff = 1f - (distance / peakRadius);
                            falloff = Mathf.Pow(falloff, 0.5f); // 繧医ｊ諤･蟲ｻ縺ｫ
                            
                            // 繝弱う繧ｺ縺ｫ繧医ｋ蟯ｩ遏ｳ縺ｮ荳崎ｦ丞援諤ｧ
                            float rockNoise = Mathf.PerlinNoise(x * 0.03f, y * 0.03f);
                            
                            float elevation = peakHeight * falloff * (0.8f + rockNoise * 0.4f);
                            heightmap[x, y] += elevation;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 螻ｱ閼亥ｰｾ譬ｹ繧堤函謌・
        /// </summary>
        private static void GenerateMountainRidges(float[,] heightmap, Vector3 worldPosition)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 荳ｻ隕∝ｰｾ譬ｹ縺ｮ逕滓・
            int ridgeCount = Random.Range(1, 3);
            
            for (int ridge = 0; ridge < ridgeCount; ridge++)
            {
                // 蟆ｾ譬ｹ縺ｮ邨瑚ｷｯ・医ｈ繧顔峩邱夂噪・・
                Vector2 start = new Vector2(Random.Range(0f, width), Random.Range(0f, height * 0.3f));
                Vector2 end = new Vector2(Random.Range(0f, width), Random.Range(height * 0.7f, height));
                
                float ridgeHeight = Random.Range(80f, 150f);
                float ridgeWidth = Random.Range(40f, 80f);
                
                // 蟆ｾ譬ｹ縺ｫ豐ｿ縺｣縺ｦ鬮伜ｺｦ繧定ｿｽ蜉
                int segments = 20;
                for (int seg = 0; seg < segments; seg++)
                {
                    float t = (float)seg / (segments - 1);
                    Vector2 ridgePoint = Vector2.Lerp(start, end, t);
                    
                    // 蜷・そ繧ｰ繝｡繝ｳ繝医〒蜀・ｽ｢縺ｮ鬮伜ｺｦ繧定ｿｽ蜉
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            float distance = Vector2.Distance(new Vector2(x, y), ridgePoint);
                            
                            if (distance < ridgeWidth * 0.5f)
                            {
                                float falloff = 1f - (distance / (ridgeWidth * 0.5f));
                                falloff = falloff * falloff; // 莠梧ｬ｡髢｢謨ｰ逧・ｸ幄｡ｰ
                                
                                float segmentHeight = ridgeHeight * (1f - t * 0.3f); // 鬮伜ｺｦ縺悟ｾ舌・↓荳九′繧・
                                heightmap[x, y] += segmentHeight * falloff;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 螻ｱ蟯ｳ縺ｮ諤･蟲ｻ縺ｪ隹ｷ繧堤函謌・
        /// </summary>
        private static void GenerateMountainValleys(float[,] heightmap, Vector3 worldPosition)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            int valleyCount = Random.Range(2, 4);
            
            for (int valley = 0; valley < valleyCount; valley++)
            {
                // V蟄苓ｰｷ縺ｮ邨瑚ｷｯ
                Vector2 start = new Vector2(Random.Range(width * 0.2f, width * 0.8f), 0f);
                Vector2 end = new Vector2(Random.Range(width * 0.2f, width * 0.8f), height);
                
                float valleyDepth = Random.Range(60f, 120f);
                float valleyWidth = Random.Range(30f, 60f); // 迢ｭ縺・ｰｷ
                
                // 隹ｷ縺ｫ豐ｿ縺｣縺ｦ蝨ｰ蠖｢繧貞炎繧具ｼ・蟄怜梛・・
                int segments = 30;
                for (int seg = 0; seg < segments; seg++)
                {
                    float t = (float)seg / (segments - 1);
                    Vector2 valleyPoint = Vector2.Lerp(start, end, t);
                    
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            float distance = Vector2.Distance(new Vector2(x, y), valleyPoint);
                            
                            if (distance < valleyWidth * 0.5f)
                            {
                                // V蟄怜梛繝励Ο繝輔ぃ繧､繝ｫ
                                float normalizedDistance = distance / (valleyWidth * 0.5f);
                                float vProfile = 1f - normalizedDistance * normalizedDistance;
                                
                                heightmap[x, y] -= valleyDepth * vProfile;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 蟯ｩ遏ｳ蟠ｩ關ｽ蝨ｰ蠖｢繧堤函謌・
        /// </summary>
        private static void GenerateRockfallTerrain(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 諤･譁憺擇縺ｧ縺ｮ蟯ｩ遏ｳ蟠ｩ關ｽ繧偵す繝溘Η繝ｬ繝ｼ繝・
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    // 蛯ｾ譁懊ｒ險育ｮ・
                    float slopeX = heightmap[x + 1, y] - heightmap[x - 1, y];
                    float slopeY = heightmap[x, y + 1] - heightmap[x, y - 1];
                    float slope = Mathf.Sqrt(slopeX * slopeX + slopeY * slopeY);
                    
                    // 諤･譁憺擇・・lope > 30・峨〒蟠ｩ關ｽ
                    if (slope > 30f)
                    {
                        // 蟠ｩ關ｽ縺ｫ繧医ｋ蟷ｳ貊大喧
                        float avgHeight = (
                            heightmap[x-1, y] + heightmap[x+1, y] +
                            heightmap[x, y-1] + heightmap[x, y+1]
                        ) / 4f;
                        
                        heightmap[x, y] = Mathf.Lerp(heightmap[x, y], avgHeight, 0.3f);
                    }
                }
            }
        }
        
        /// <summary>
        /// 螻ｱ蟯ｳ迚ｹ譛峨・豬ｸ鬟溘ｒ驕ｩ逕ｨ
        /// </summary>
        private static void ApplyMountainErosion(float[,] heightmap, float erosionStrength)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 豌ｷ豐ｳ豬ｸ鬟溘→豌ｴ縺ｫ繧医ｋ豬ｸ鬟・
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float currentHeight = heightmap[x, y];
                    
                    // 鬮伜ｺｦ縺ｫ繧医ｋ豬ｸ鬟溷ｼｷ蠎ｦ
                    float altitudeFactor = Mathf.InverseLerp(0, 500, currentHeight); // 0-500m縺ｮ遽・峇縺ｧ豁｣隕丞喧
                    float altitudeErosion = erosionStrength * (1f + altitudeFactor * 2f);
                    
                    // 蜻ｨ霎ｺ縺ｮ蟷ｳ蝮・ｫ伜ｺｦ
                    float avgNeighbor = (
                        heightmap[x-1, y] + heightmap[x+1, y] +
                        heightmap[x, y-1] + heightmap[x, y+1]
                    ) / 4f;
                    
                    // 鬮倥＞驛ｨ蛻・⊇縺ｩ蠑ｷ縺乗ｵｸ鬟・
                    if (currentHeight > avgNeighbor)
                    {
                        float erosion = (currentHeight - avgNeighbor) * altitudeErosion * 0.08f;
                        heightmap[x, y] -= erosion;
                    }
                }
            }
        }
        
        #endregion
        
        #region 闕牙次蝨ｰ蠖｢逕滓・
        
        /// <summary>
        /// 闕牙次蝨ｰ蠖｢繧堤函謌撰ｼ医↑縺繧峨°縺ｪ荳倬匏縲∵ｲｳ蟾昴∵ｹｿ蝨ｰ・・
        /// </summary>
        public static void GenerateGrasslandTerrain(float[,] heightmap, BiomeDefinition biomeDefinition, Vector3 worldPosition)
        {
            // 縺ｪ縺繧峨°縺ｪ荳倬匏縺ｮ逕滓・
            GenerateRollingHills(heightmap);
            
            // 豐ｳ蟾昴・逕滓・
            GenerateGrasslandRivers(heightmap);
            
            // 貉ｿ蝨ｰ縺ｮ逕滓・
            GenerateWetlands(heightmap);
            
            // 闕牙次迚ｹ譛峨・豬ｸ鬟・
            ApplyGrasslandErosion(heightmap, biomeDefinition.terrainModifiers.erosionStrength);
        }
        
        /// <summary>
        /// 縺ｪ縺繧峨°縺ｪ荳倬匏繧堤函謌・
        /// </summary>
        private static void GenerateRollingHills(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 隍・焚繧ｹ繧ｱ繝ｼ繝ｫ縺ｮ荳倬匏
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float hills = 0f;
                    
                    // 螟ｧ縺阪↑荳倬匏
                    hills += Mathf.PerlinNoise(x * 0.005f, y * 0.005f) * 30f;
                    
                    // 荳ｭ遞句ｺｦ縺ｮ荳倬匏
                    hills += Mathf.PerlinNoise(x * 0.012f, y * 0.012f) * 15f;
                    
                    // 蟆上＆縺ｪ襍ｷ莨・
                    hills += Mathf.PerlinNoise(x * 0.025f, y * 0.025f) * 5f;
                    
                    heightmap[x, y] += hills;
                }
            }
        }
        
        /// <summary>
        /// 闕牙次縺ｮ豐ｳ蟾昴ｒ逕滓・
        /// </summary>
        private static void GenerateGrasslandRivers(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 陋・｡後☆繧区ｲｳ蟾・
            int riverCount = Random.Range(1, 3);
            
            for (int river = 0; river < riverCount; river++)
            {
                List<Vector2> riverPath = GenerateMeanderingPath(width, height, Random.Range(8, 15));
                
                float riverDepth = Random.Range(8f, 15f);
                float riverWidth = Random.Range(20f, 40f);
                
                // 豐ｳ蟾昴↓豐ｿ縺｣縺ｦ蝨ｰ蠖｢繧貞炎繧・
                foreach (Vector2 pathPoint in riverPath)
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            float distance = Vector2.Distance(new Vector2(x, y), pathPoint);
                            
                            if (distance < riverWidth * 0.5f)
                            {
                                float falloff = 1f - (distance / (riverWidth * 0.5f));
                                falloff = Mathf.SmoothStep(0f, 1f, falloff);
                                
                                heightmap[x, y] -= riverDepth * falloff;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 貉ｿ蝨ｰ繧堤函謌・
        /// </summary>
        private static void GenerateWetlands(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            int wetlandCount = Random.Range(2, 5);
            
            for (int wetland = 0; wetland < wetlandCount; wetland++)
            {
                Vector2 wetlandCenter = new Vector2(
                    Random.Range(width * 0.2f, width * 0.8f),
                    Random.Range(height * 0.2f, height * 0.8f)
                );
                
                float wetlandRadius = Random.Range(60f, 120f);
                float wetlandDepth = Random.Range(3f, 8f);
                
                // 荳崎ｦ丞援縺ｪ貉ｿ蝨ｰ蠖｢迥ｶ
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), wetlandCenter);
                        
                        if (distance < wetlandRadius)
                        {
                            // 繝弱う繧ｺ縺ｫ繧医ｋ荳崎ｦ丞援縺ｪ蠅・阜
                            float noise = Mathf.PerlinNoise(x * 0.02f, y * 0.02f);
                            float effectiveRadius = wetlandRadius * (0.6f + noise * 0.8f);
                            
                            if (distance < effectiveRadius)
                            {
                                float falloff = 1f - (distance / effectiveRadius);
                                falloff = Mathf.SmoothStep(0f, 1f, falloff);
                                
                                heightmap[x, y] -= wetlandDepth * falloff;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 闕牙次迚ｹ譛峨・豬ｸ鬟溘ｒ驕ｩ逕ｨ
        /// </summary>
        private static void ApplyGrasslandErosion(float[,] heightmap, float erosionStrength)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 闕峨↓繧医ｋ豬ｸ鬟滓椛蛻ｶ・域｣ｮ譫励⊇縺ｩ縺ｧ縺ｯ縺ｪ縺・ｼ・
            float grassErosion = erosionStrength * 0.6f;
            
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
                        float erosion = (currentHeight - avgNeighbor) * grassErosion * 0.06f;
                        heightmap[x, y] -= erosion;
                    }
                }
            }
        }
        
        #endregion
        
        #region 豬ｷ蟯ｸ蝨ｰ蠖｢逕滓・
        
        /// <summary>
        /// 豬ｷ蟯ｸ蝨ｰ蠖｢繧堤函謌撰ｼ域ｵｷ蟯ｸ邱壹∫よｵ懊∵ｵｷ鬟溷ｴ厄ｼ・
        /// </summary>
        public static void GenerateCoastalTerrain(float[,] heightmap, BiomeDefinition biomeDefinition, Vector3 worldPosition)
        {
            // 豬ｷ蟯ｸ邱壹・逕滓・
            GenerateCoastline(heightmap, worldPosition);
            
            // 遐よｵ懊・逕滓・
            GenerateBeaches(heightmap);
            
            // 豬ｷ鬟溷ｴ悶・逕滓・
            GenerateSeaCliffs(heightmap);
            
            // 豬ｷ蟯ｸ豬ｸ鬟溘・驕ｩ逕ｨ
            ApplyCoastalErosion(heightmap, biomeDefinition.terrainModifiers.erosionStrength);
        }
        
        /// <summary>
        /// 豬ｷ蟯ｸ邱壹ｒ逕滓・
        /// </summary>
        private static void GenerateCoastline(float[,] heightmap, Vector3 worldPosition)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 豬ｷ髱｢繝ｬ繝吶Ν・・m・峨ｒ蝓ｺ貅悶→縺励◆豬ｷ蟯ｸ邱・
            float seaLevel = 0f;
            
            // 豬ｷ蟯ｸ邱壹・螟ｧ縺ｾ縺九↑菴咲ｽｮ・亥慍蝗ｳ縺ｮ荳霎ｺ・・
            bool isWestCoast = Random.value > 0.5f;
            
            for (int y = 0; y < height; y++)
            {
                // 豬ｷ蟯ｸ邱壹・陋・｡・
                float coastlineNoise = Mathf.PerlinNoise(y * 0.01f, worldPosition.z * 0.001f);
                int coastlineX = isWestCoast ? 
                    Mathf.RoundToInt(width * 0.2f + coastlineNoise * width * 0.3f) :
                    Mathf.RoundToInt(width * 0.8f + coastlineNoise * width * 0.3f);
                
                coastlineX = Mathf.Clamp(coastlineX, 0, width - 1);
                
                // 豬ｷ蟯ｸ邱壹°繧画ｵｷ蛛ｴ繧呈ｵｷ髱｢繝ｬ繝吶Ν縺ｫ險ｭ螳・
                for (int x = 0; x < width; x++)
                {
                    bool isSeaSide = isWestCoast ? (x < coastlineX) : (x > coastlineX);
                    
                    if (isSeaSide)
                    {
                        heightmap[x, y] = Mathf.Min(heightmap[x, y], seaLevel);
                    }
                }
            }
        }
        
        /// <summary>
        /// 遐よｵ懊ｒ逕滓・
        /// </summary>
        private static void GenerateBeaches(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float currentHeight = heightmap[x, y];
                    
                    // 豬ｷ髱｢霑代￥縺ｮ菴主慍繧堤よｵ懊↓
                    if (currentHeight >= -2f && currentHeight <= 5f)
                    {
                        // 豬ｷ縺九ｉ縺ｮ霍晞屬繧定ｨ育ｮ・
                        float distanceFromSea = CalculateDistanceFromSea(heightmap, x, y);
                        
                        if (distanceFromSea < 50f)
                        {
                            // 遐よｵ懊・邱ｩ繧・°縺ｪ蛯ｾ譁・
                            float beachSlope = distanceFromSea * 0.1f;
                            heightmap[x, y] = Mathf.Max(heightmap[x, y], beachSlope);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 豬ｷ鬟溷ｴ悶ｒ逕滓・
        /// </summary>
        private static void GenerateSeaCliffs(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float currentHeight = heightmap[x, y];
                    
                    // 豬ｷ縺ｫ霑代＞鬮伜慍繧貞ｴ悶↓
                    if (currentHeight > 20f)
                    {
                        float distanceFromSea = CalculateDistanceFromSea(heightmap, x, y);
                        
                        if (distanceFromSea < 30f)
                        {
                            // 諤･蟲ｻ縺ｪ蟠悶ｒ菴懈・
                            float cliffHeight = currentHeight + Random.Range(20f, 60f);
                            heightmap[x, y] = cliffHeight;
                            
                            // 蟠悶・豬ｸ鬟溘↓繧医ｋ荳崎ｦ丞援諤ｧ
                            float erosionNoise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                            heightmap[x, y] += erosionNoise * 15f;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 豬ｷ縺九ｉ縺ｮ霍晞屬繧定ｨ育ｮ・
        /// </summary>
        private static float CalculateDistanceFromSea(float[,] heightmap, int x, int y)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            float minDistance = float.MaxValue;
            
            // 蜻ｨ霎ｺ縺ｮ豬ｷ髱｢繝ｬ繝吶Ν・・m莉･荳具ｼ峨・轤ｹ繧呈､懃ｴ｢
            int searchRadius = 50;
            for (int dx = -searchRadius; dx <= searchRadius; dx++)
            {
                for (int dy = -searchRadius; dy <= searchRadius; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        if (heightmap[nx, ny] <= 0f)
                        {
                            float distance = Mathf.Sqrt(dx * dx + dy * dy);
                            minDistance = Mathf.Min(minDistance, distance);
                        }
                    }
                }
            }
            
            return minDistance == float.MaxValue ? 1000f : minDistance;
        }
        
        /// <summary>
        /// 豬ｷ蟯ｸ豬ｸ鬟溘ｒ驕ｩ逕ｨ
        /// </summary>
        private static void ApplyCoastalErosion(float[,] heightmap, float erosionStrength)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 豕｢縺ｫ繧医ｋ豬ｸ鬟・
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float distanceFromSea = CalculateDistanceFromSea(heightmap, x, y);
                    
                    // 豬ｷ縺ｫ霑代＞縺ｻ縺ｩ豬ｸ鬟溘′蠑ｷ縺・
                    if (distanceFromSea < 100f)
                    {
                        float waveErosion = erosionStrength * (1f - distanceFromSea / 100f);
                        
                        float currentHeight = heightmap[x, y];
                        float avgNeighbor = (
                            heightmap[x-1, y] + heightmap[x+1, y] +
                            heightmap[x, y-1] + heightmap[x, y+1]
                        ) / 4f;
                        
                        if (currentHeight > avgNeighbor)
                        {
                            float erosion = (currentHeight - avgNeighbor) * waveErosion * 0.15f;
                            heightmap[x, y] -= erosion;
                        }
                    }
                }
            }
        }
        
        #endregion
        
        #region 讌ｵ蝨ｰ蝨ｰ蠖｢逕滓・
        
        /// <summary>
        /// 讌ｵ蝨ｰ蝨ｰ蠖｢繧堤函謌撰ｼ域ｰｷ豐ｳ蝨ｰ蠖｢縲√ヤ繝ｳ繝峨Λ縲∵ｰｸ荵・㍾蝨滂ｼ・
        /// </summary>
        public static void GeneratePolarTerrain(float[,] heightmap, BiomeDefinition biomeDefinition, Vector3 worldPosition)
        {
            // 豌ｷ豐ｳ蝨ｰ蠖｢縺ｮ逕滓・
            GenerateGlacialTerrain(heightmap);
            
            // 繝・Φ繝峨Λ縺ｮ襍ｷ莨・
            GenerateTundraUndulation(heightmap);
            
            // 豌ｸ荵・㍾蝨溘・蠖ｱ髻ｿ
            ApplyPermafrostEffects(heightmap);
            
            // 豌ｷ豐ｳ豬ｸ鬟・
            ApplyGlacialErosion(heightmap, biomeDefinition.terrainModifiers.erosionStrength);
        }
        
        /// <summary>
        /// 豌ｷ豐ｳ蝨ｰ蠖｢繧堤函謌・
        /// </summary>
        private static void GenerateGlacialTerrain(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 豌ｷ豐ｳ縺ｫ繧医ｋ蟷ｳ貊大喧
            float[,] smoothedMap = new float[width, height];
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float sum = 0f;
                    int count = 0;
                    int radius = 8; // 螟ｧ縺阪↑蟷ｳ貊大喧蜊雁ｾ・
                    
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dy = -radius; dy <= radius; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                sum += heightmap[nx, ny];
                                count++;
                            }
                        }
                    }
                    
                    smoothedMap[x, y] = sum / count;
                }
            }
            
            // 蟷ｳ貊大喧繧帝←逕ｨ
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    heightmap[x, y] = Mathf.Lerp(heightmap[x, y], smoothedMap[x, y], 0.7f);
                }
            }
            
            // 豌ｷ豐ｳ隹ｷ・・蟄苓ｰｷ・峨・逕滓・
            GenerateGlacialValleys(heightmap);
        }
        
        /// <summary>
        /// 豌ｷ豐ｳ隹ｷ繧堤函謌・
        /// </summary>
        private static void GenerateGlacialValleys(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            int valleyCount = Random.Range(1, 3);
            
            for (int valley = 0; valley < valleyCount; valley++)
            {
                Vector2 start = new Vector2(Random.Range(0f, width), 0f);
                Vector2 end = new Vector2(Random.Range(0f, width), height);
                
                float valleyDepth = Random.Range(40f, 80f);
                float valleyWidth = Random.Range(100f, 200f);
                
                // U蟄怜梛縺ｮ隹ｷ繧剃ｽ懈・
                int segments = 20;
                for (int seg = 0; seg < segments; seg++)
                {
                    float t = (float)seg / (segments - 1);
                    Vector2 valleyPoint = Vector2.Lerp(start, end, t);
                    
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            float distance = Vector2.Distance(new Vector2(x, y), valleyPoint);
                            
                            if (distance < valleyWidth * 0.5f)
                            {
                                // U蟄怜梛繝励Ο繝輔ぃ繧､繝ｫ
                                float normalizedDistance = distance / (valleyWidth * 0.5f);
                                float uProfile = 1f - normalizedDistance * normalizedDistance;
                                uProfile = Mathf.Sqrt(uProfile); // 繧医ｊ蟷ｳ繧峨↑蠎・
                                
                                heightmap[x, y] -= valleyDepth * uProfile;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 繝・Φ繝峨Λ縺ｮ襍ｷ莨上ｒ逕滓・
        /// </summary>
        private static void GenerateTundraUndulation(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 蟆上＆縺ｪ襍ｷ莨擾ｼ医・繝ｪ繧ｴ繝ｳ蝨ｰ蠖｢・・
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // 豌ｸ荵・㍾蝨溘↓繧医ｋ蟆上＆縺ｪ髫・ｵｷ
                    float polygonNoise = Mathf.PerlinNoise(x * 0.03f, y * 0.03f);
                    if (polygonNoise > 0.6f)
                    {
                        heightmap[x, y] += (polygonNoise - 0.6f) * 10f;
                    }
                    
                    // 蜈ｨ菴鍋噪縺ｫ菴弱＞襍ｷ莨・
                    float tundraUndulation = Mathf.PerlinNoise(x * 0.01f, y * 0.01f) * 5f;
                    heightmap[x, y] += tundraUndulation;
                }
            }
        }
        
        /// <summary>
        /// 豌ｸ荵・㍾蝨溘・蠖ｱ髻ｿ繧帝←逕ｨ
        /// </summary>
        private static void ApplyPermafrostEffects(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 豌ｸ荵・㍾蝨溘↓繧医ｋ蝨ｰ蠖｢縺ｮ螳牙ｮ壼喧
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    // 諤･豼縺ｪ鬮伜ｺｦ螟牙喧繧呈椛蛻ｶ
                    float currentHeight = heightmap[x, y];
                    float avgNeighbor = (
                        heightmap[x-1, y] + heightmap[x+1, y] +
                        heightmap[x, y-1] + heightmap[x, y+1]
                    ) / 4f;
                    
                    float heightDiff = Mathf.Abs(currentHeight - avgNeighbor);
                    if (heightDiff > 10f)
                    {
                        // 豌ｸ荵・㍾蝨溘↓繧医ｋ螳牙ｮ壼喧
                        heightmap[x, y] = Mathf.Lerp(currentHeight, avgNeighbor, 0.3f);
                    }
                }
            }
        }
        
        /// <summary>
        /// 豌ｷ豐ｳ豬ｸ鬟溘ｒ驕ｩ逕ｨ
        /// </summary>
        private static void ApplyGlacialErosion(float[,] heightmap, float erosionStrength)
        {
            // 豌ｷ豐ｳ豬ｸ鬟溘・髱槫ｸｸ縺ｫ蠑ｷ蜉帙□縺後∫樟蝨ｨ縺ｯ豌ｷ縺ｫ隕・ｏ繧後※縺・ｋ縺溘ａ豬ｸ鬟溘・蟆代↑縺・
            float glacialErosion = erosionStrength * 0.1f;
            
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
                        float erosion = (currentHeight - avgNeighbor) * glacialErosion * 0.02f;
                        heightmap[x, y] -= erosion;
                    }
                }
            }
        }
        
        #endregion
    }
}