using UnityEngine;
using System.Collections.Generic;
using Vastcore.Core;

namespace Vastcore.Generation
{
    public static partial class BiomeSpecificTerrainGenerator
    {
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
    }
}
