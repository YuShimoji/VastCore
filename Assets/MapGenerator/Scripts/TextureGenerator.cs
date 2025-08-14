using UnityEngine;

namespace Vastcore.Generation
{
    public static class TextureGenerator
    {
        public static void ConfigureTextureLayers(TerrainGenerator generator, TerrainData terrainData, float[,] heights)
        {
            if (generator.TerrainLayers == null || generator.TerrainLayers.Length == 0)
            {
                return;
            }

            terrainData.terrainLayers = generator.TerrainLayers;

            int resolution = terrainData.heightmapResolution;
            float[,,] splatmapData = new float[resolution, resolution, terrainData.alphamapLayers];

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float height = heights[x, y];
                    float slope = terrainData.GetSteepness((float)y / resolution, (float)x / resolution);

                    float[] splatWeights = new float[terrainData.alphamapLayers];

                    // ルールに基づいてテクスチャの重みを計算
                    // Layer 0: Base (Grass)
                    splatWeights[0] = 1.0f - Mathf.Clamp01(slope / 40.0f); // なだらかな場所ほど強く
                    // Layer 1: Cliff
                    splatWeights[1] = Mathf.Clamp01(slope / 40.0f); // 急な場所ほど強く
                    // Layer 2: Snow
                    if (height > 0.7f) {
                        splatWeights[2] = Mathf.InverseLerp(0.7f, 0.9f, height);
                    }

                    // 重みを正規化
                    float totalWeight = 0;
                    foreach (float w in splatWeights) totalWeight += w;
                    if (totalWeight > 0)
                    {
                        for (int i = 0; i < splatWeights.Length; i++)
                        {
                            splatmapData[y, x, i] = splatWeights[i] / totalWeight;
                        }
                    }
                }
            }

            terrainData.SetAlphamaps(0, 0, splatmapData);
        }
    }
}
