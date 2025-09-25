using UnityEngine;

namespace Vastcore.Generation
{
    public static class BiomeSpecificTerrainGenerator
    {
        public static void GenerateDesertTerrain(float[,] heightmap, BiomeDefinition biome, Vector3 origin)
        {
            ApplyHeightMultiplier(heightmap, biome?.terrainModifiers?.heightMultiplier ?? 1f, 0.8f);
        }

        public static void GenerateForestTerrain(float[,] heightmap, BiomeDefinition biome, Vector3 origin)
        {
            ApplyHeightMultiplier(heightmap, biome?.terrainModifiers?.heightMultiplier ?? 1f, 1.05f);
        }

        public static void GenerateMountainTerrain(float[,] heightmap, BiomeDefinition biome, Vector3 origin)
        {
            ApplyHeightMultiplier(heightmap, biome?.terrainModifiers?.heightMultiplier ?? 1f, 1.2f);
        }

        public static void GenerateCoastalTerrain(float[,] heightmap, BiomeDefinition biome, Vector3 origin)
        {
            // Push down near "sea level"
            int w = heightmap.GetLength(0);
            int h = heightmap.GetLength(1);
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    heightmap[x, y] *= 0.95f;
                    if (heightmap[x, y] < 1f) heightmap[x, y] = Mathf.Min(heightmap[x, y], 0.5f);
                }
            }
        }

        public static void GeneratePolarTerrain(float[,] heightmap, BiomeDefinition biome, Vector3 origin)
        {
            // Smooth terrain
            Smooth(heightmap, 1);
        }

        public static void GenerateGrasslandTerrain(float[,] heightmap, BiomeDefinition biome, Vector3 origin)
        {
            ApplyHeightMultiplier(heightmap, biome?.terrainModifiers?.heightMultiplier ?? 1f, 0.9f);
            Smooth(heightmap, 1);
        }

        private static void ApplyHeightMultiplier(float[,] map, float biomeMul, float extraMul)
        {
            int w = map.GetLength(0);
            int h = map.GetLength(1);
            float mul = biomeMul * extraMul;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    map[x, y] *= mul;
                }
            }
        }

        private static void Smooth(float[,] map, int iterations)
        {
            int w = map.GetLength(0);
            int h = map.GetLength(1);
            for (int it = 0; it < iterations; it++)
            {
                for (int x = 1; x < w - 1; x++)
                {
                    for (int y = 1; y < h - 1; y++)
                    {
                        float avg = (map[x, y] + map[x - 1, y] + map[x + 1, y] + map[x, y - 1] + map[x, y + 1]) / 5f;
                        map[x, y] = Mathf.Lerp(map[x, y], avg, 0.5f);
                    }
                }
            }
        }
    }
}
