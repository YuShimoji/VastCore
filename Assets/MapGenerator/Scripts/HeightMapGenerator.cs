using UnityEngine;
using System;

namespace Vastcore.Generation
{
    public static class HeightMapGenerator
    {
        /// <summary>
        /// 指定されたチャンネルから高さ値を取得
        /// </summary>
        private static float GetChannelValue(Color color, HeightMapChannel channel)
        {
            return channel switch
            {
                HeightMapChannel.R => color.r,
                HeightMapChannel.G => color.g,
                HeightMapChannel.B => color.b,
                HeightMapChannel.A => color.a,
                HeightMapChannel.Luminance => color.grayscale,
                _ => color.grayscale
            };
        }

        /// <summary>
        /// Seed から決定論的な Offset を生成する
        /// 同一Seedは同一結果、異Seedは概ね異なる結果を返す
        /// </summary>
        private static Vector2 GetDeterministicOffsetFromSeed(int seed)
        {
            System.Random rng = new System.Random(seed);
            // 0-1000 の範囲でランダムなオフセットを生成（決定論的）
            float offsetX = (float)(rng.NextDouble() * 1000.0);
            float offsetY = (float)(rng.NextDouble() * 1000.0);
            return new Vector2(offsetX, offsetY);
        }

        public static float[,] GenerateHeights(TerrainGenerator generator)
        {
            switch (generator.GenerationMode)
            {
                case TerrainGenerationMode.HeightMap:
                    return GenerateFromHeightMap(generator);
                case TerrainGenerationMode.NoiseAndHeightMap:
                    return CombineNoiseAndHeightMap(generator);
                case TerrainGenerationMode.Noise:
                default:
                    return GenerateFromNoise(generator);
            }
        }

        private static float[,] GenerateFromNoise(TerrainGenerator generator)
        {
            // Seed から決定論的な Offset を生成し、既存の Offset に加算
            Vector2 seedOffset = GetDeterministicOffsetFromSeed(generator.Seed);
            Vector2 effectiveOffset = generator.Offset + seedOffset;

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
                        float sampleX = (x - generator.Resolution / 2f + effectiveOffset.x) / generator.Scale * frequency;
                        float sampleY = (y - generator.Resolution / 2f + effectiveOffset.y) / generator.Scale * frequency;
                        
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
                // UV Tiling と UV Offset を適用
                float v = (float)y / (generator.Resolution - 1);
                v = v * generator.UVTiling.y + generator.UVOffset.y;
                if (generator.FlipHeightMapVertically) v = 1 - v;
                
                // UV座標をテクスチャ座標に変換（繰り返しを考慮）
                float sourceY = (v % 1f) * (sourceHeight - 1);
                if (sourceY < 0) sourceY += sourceHeight - 1;
                int y1 = Mathf.FloorToInt(sourceY);
                int y2 = Mathf.Min(y1 + 1, sourceHeight - 1);
                float fy = sourceY - y1;
                
                for (int x = 0; x < generator.Resolution; x++)
                {
                    // UV Tiling と UV Offset を適用
                    float u = (float)x / (generator.Resolution - 1);
                    u = u * generator.UVTiling.x + generator.UVOffset.x;
                    
                    // UV座標をテクスチャ座標に変換（繰り返しを考慮）
                    float sourceX = (u % 1f) * (sourceWidth - 1);
                    if (sourceX < 0) sourceX += sourceWidth - 1;
                    int x1 = Mathf.FloorToInt(sourceX);
                    int x2 = Mathf.Min(x1 + 1, sourceWidth - 1);
                    float fx = sourceX - x1;
                    
                    // 指定されたチャンネルから値を取得
                    Color c00 = pixels[y1 * sourceWidth + x1];
                    Color c10 = pixels[y1 * sourceWidth + x2];
                    Color c01 = pixels[y2 * sourceWidth + x1];
                    Color c11 = pixels[y2 * sourceWidth + x2];
                    
                    float h00 = GetChannelValue(c00, generator.HeightMapChannel);
                    float h10 = GetChannelValue(c10, generator.HeightMapChannel);
                    float h01 = GetChannelValue(c01, generator.HeightMapChannel);
                    float h11 = GetChannelValue(c11, generator.HeightMapChannel);
                    
                    float height = Mathf.Lerp(
                        Mathf.Lerp(h00, h10, fx),
                        Mathf.Lerp(h01, h11, fx),
                        fy
                    );
                    
                    // InvertHeight を適用
                    if (generator.InvertHeight)
                    {
                        height = 1f - height;
                    }
                    
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
