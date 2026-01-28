using UnityEngine;

namespace Vastcore.Generation
{
    public static class HeightMapGenerator
    {
        public static float[,] GenerateHeights(TerrainGenerator generator)
        {
            switch (generator.GenerationMode)
            {
                case TerrainGenerator.TerrainGenerationMode.HeightMap:
                    return GenerateFromHeightMap(generator);
                case TerrainGenerator.TerrainGenerationMode.NoiseAndHeightMap:
                    return CombineNoiseAndHeightMap(generator);
                case TerrainGenerator.TerrainGenerationMode.Noise:
                default:
                    return GenerateFromNoise(generator);
            }
        }

        private static float[,] GenerateFromNoise(TerrainGenerator generator)
        {
            float[,] heights = new float[generator.Resolution, generator.Resolution];
            float maxPossibleHeight = 0;
            float amplitude = 1;
            
            for (int i = 0; i < generator.Octaves; i++)
            {
                maxPossibleHeight += amplitude;
                amplitude *= generator.Persistence;
            }
            
            for (int y = 0; y < generator.Resolution; y++)
            {
                for (int x = 0; x < generator.Resolution; x++)
                {
                    amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;
                    float weight = 1;
                    
                    for (int i = 0; i < generator.Octaves; i++)
                    {
                        float sampleX = (x - generator.Resolution / 2f + generator.Offset.x) / generator.Scale * frequency;
                        float sampleY = (y - generator.Resolution / 2f + generator.Offset.y) / generator.Scale * frequency;
                        
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        
                        noiseHeight += perlinValue * weight;
                        
                        weight = Mathf.Clamp01(weight * generator.Persistence);
                        
                        frequency *= generator.Lacunarity;
                    }
                    
                    heights[x, y] = Mathf.Clamp01((noiseHeight / maxPossibleHeight + 1f) * TerrainGenerationConstants.HeightNormalizationFactor);
                }
            }
            
            return heights;
        }

        private static float[,] GenerateFromHeightMap(TerrainGenerator generator)
        {
            if (generator.HeightMap == null)
            {
                Debug.LogError("[TerrainGenerator] Height map is not assigned!");
                return new float[generator.Resolution, generator.Resolution];
            }

            Color[] pixels = generator.HeightMap.GetPixels();
            int sourceWidth = generator.HeightMap.width;
            int sourceHeight = generator.HeightMap.height;
            
            float[,] heights = new float[generator.Resolution, generator.Resolution];
            
            for (int y = 0; y < generator.Resolution; y++)
            {
                float v = (float)y / (generator.Resolution - 1);
                if (generator.FlipHeightMapVertically) v = 1 - v;
                
                float sourceY = v * (sourceHeight - 1);
                int y1 = Mathf.FloorToInt(sourceY);
                int y2 = Mathf.Min(y1 + 1, sourceHeight - 1);
                float fy = sourceY - y1;
                
                for (int x = 0; x < generator.Resolution; x++)
                {
                    float u = (float)x / (generator.Resolution - 1);
                    float sourceX = u * (sourceWidth - 1);
                    int x1 = Mathf.FloorToInt(sourceX);
                    int x2 = Mathf.Min(x1 + 1, sourceWidth - 1);
                    float fx = sourceX - x1;
                    
                    float c00 = pixels[y1 * sourceWidth + x1].grayscale;
                    float c10 = pixels[y1 * sourceWidth + x2].grayscale;
                    float c01 = pixels[y2 * sourceWidth + x1].grayscale;
                    float c11 = pixels[y2 * sourceWidth + x2].grayscale;
                    
                    float height = Mathf.Lerp(
                        Mathf.Lerp(c00, c10, fx),
                        Mathf.Lerp(c01, c11, fx),
                        fy
                    );
                    
                    heights[x, y] = Mathf.Clamp01(height * generator.HeightMapScale + generator.HeightMapOffset);
                }
            }
            
            return heights;
        }

        private static float[,] CombineNoiseAndHeightMap(TerrainGenerator generator)
        {
            float[,] noiseHeights = GenerateFromNoise(generator);
            float[,] heightMapHeights = GenerateFromHeightMap(generator);
            float[,] combinedHeights = new float[generator.Resolution, generator.Resolution];
            
            for (int y = 0; y < generator.Resolution; y++)
            {
                for (int x = 0; x < generator.Resolution; x++)
                {
                    float gradient = 0;
                    int samples = 0;
                    int radius = TerrainGenerationConstants.GradientSampleRadius;
                    
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int nx = Mathf.Clamp(x + dx, 0, generator.Resolution - 1);
                            int ny = Mathf.Clamp(y + dy, 0, generator.Resolution - 1);
                            gradient += Mathf.Abs(heightMapHeights[x, y] - heightMapHeights[nx, ny]);
                            samples++;
                        }
                    }
                    
                    gradient /= samples;
                    
                    float noiseInfluence = Mathf.Lerp(
                        TerrainGenerationConstants.MaxNoiseInfluence, 
                        TerrainGenerationConstants.MinNoiseInfluence, 
                        Mathf.Clamp01(gradient * TerrainGenerationConstants.GradientMultiplier)
                    );
                    
                    combinedHeights[x, y] = Mathf.Lerp(
                        heightMapHeights[x, y],
                        noiseHeights[x, y],
                        noiseInfluence
                    );
                }
            }
            
            return combinedHeights;
        }
    }
}
