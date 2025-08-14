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
                    float slope = terrainData.GetSteepness(y / (float)resolution, x / (float)resolution);

                    float[] splatWeights = new float[terrainData.alphamapLayers];

                    for (int i = 0; i < generator.TerrainLayers.Length; i++)
                    {
                        // ここでレイヤーのブレンドロジックを実装します。
                        // 例えば、高さや傾斜に基づいてテクスチャをブレンドします。
                        // この例では、単純に最初のレイヤーを適用します。
                        if (i == 0) splatWeights[i] = 1;
                    }

                    // 重みを正規化
                    float totalWeight = 0;
                    foreach (float w in splatWeights) totalWeight += w;
                    if (totalWeight > 0)
                    {
                        for (int i = 0; i < splatWeights.Length; i++)
                        {
                            splatmapData[x, y, i] = splatWeights[i] / totalWeight;
                        }
                    }
                }
            }

            terrainData.SetAlphamaps(0, 0, splatmapData);
        }
    }
}
