using UnityEngine;
using System.Collections.Generic;
using Vastcore.Core;

namespace Vastcore.Generation
{
    public static partial class BiomeSpecificTerrainGenerator
    {
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
