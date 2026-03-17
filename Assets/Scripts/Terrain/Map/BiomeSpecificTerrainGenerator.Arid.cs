using UnityEngine;
using System.Collections.Generic;
using Vastcore.Core;

namespace Vastcore.Generation
{
    public static partial class BiomeSpecificTerrainGenerator
    {
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
    }
}
