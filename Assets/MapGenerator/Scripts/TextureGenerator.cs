using UnityEngine;
using Vastcore.Utils;

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

            // TerrainLayers を適用しつつ、必要に応じてタイルサイズを上書き
            var layers = generator.TerrainLayers;
            if (generator.TextureTiling != null && generator.TextureTiling.Length > 0)
            {
                for (int i = 0; i < layers.Length; i++)
                {
                    if (i < generator.TextureTiling.Length)
                    {
                        Vector2 tiling = generator.TextureTiling[i];
                        // Unity の TerrainLayer は tileSize でスケール指定（x:U, y:V）
                        layers[i].tileSize = new Vector2(Mathf.Max(1f, tiling.x), Mathf.Max(1f, tiling.y));
                    }
                }
            }
            terrainData.terrainLayers = layers;

            int resolution = terrainData.heightmapResolution;
            float[,,] splatmapData = new float[resolution, resolution, terrainData.alphamapLayers];

            int layersCount = terrainData.alphamapLayers;
            // レイヤー別乗数（不足時は 1.0f）
            float GetFactor(int index)
            {
                if (generator.TextureBlendFactors == null || generator.TextureBlendFactors.Length == 0) return 1f;
                return index < generator.TextureBlendFactors.Length ? Mathf.Max(0f, generator.TextureBlendFactors[index]) : 1f;
            }
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float height = heights[x, y];
                    float slope = terrainData.GetSteepness((float)y / resolution, (float)x / resolution);

                    float[] splatWeights = new float[layersCount];

                    // ルールに基づいてテクスチャの重みを計算
                    float slopeNorm = Mathf.Clamp01(slope / 40.0f);
                    if (layersCount >= 1)
                    {
                        // Layer 0: Base (Grass)
                        splatWeights[0] = (1.0f - slopeNorm) * GetFactor(0); // なだらかな場所ほど強く
                    }
                    if (layersCount >= 2)
                    {
                        // Layer 1: Cliff
                        splatWeights[1] = slopeNorm * GetFactor(1); // 急な場所ほど強く
                    }
                    if (layersCount >= 3)
                    {
                        // Layer 2: Snow (高標高)
                        if (height > 0.7f)
                        {
                            splatWeights[2] = Mathf.InverseLerp(0.7f, 0.9f, height) * GetFactor(2);
                        }
                    }

                    // 重みを正規化
                    float totalWeight = 0f;
                    for (int i = 0; i < splatWeights.Length; i++) totalWeight += splatWeights[i];
                    if (totalWeight <= 1e-5f)
                    {
                        // すべて 0 の場合はベースレイヤーに割り当てて破綻回避
                        if (layersCount > 0) splatmapData[y, x, 0] = 1f;
                    }
                    else
                    {
                        for (int i = 0; i < splatWeights.Length; i++)
                        {
                            splatmapData[y, x, i] = splatWeights[i] / totalWeight;
                        }
                    }
                }
            }

            // using (LoadProfiler.Measure("TerrainData.SetAlphamaps (TextureGenerator)"))
            {
                // 大規模Terrainをバッチ処理で設定（メモリスパイク軽減）
                SetAlphamapsInBatches(terrainData, splatmapData);
            }
        }

        /// <summary>
        /// アルファマップをバッチ処理で設定（メモリスパイク軽減）
        /// </summary>
        private static void SetAlphamapsInBatches(TerrainData terrainData, float[,,] splatmapData)
        {
            int height = splatmapData.GetLength(0);
            int width = splatmapData.GetLength(1);
            int layers = splatmapData.GetLength(2);
            int batchSize = 256;

            for (int yStart = 0; yStart < height; yStart += batchSize)
            {
                for (int xStart = 0; xStart < width; xStart += batchSize)
                {
                    int yEnd = Mathf.Min(yStart + batchSize, height);
                    int xEnd = Mathf.Min(xStart + batchSize, width);
                    int batchHeight = yEnd - yStart;
                    int batchWidth = xEnd - xStart;

                    float[,,] batchSplatmap = new float[batchHeight, batchWidth, layers];
                    for (int y = 0; y < batchHeight; y++)
                    {
                        for (int x = 0; x < batchWidth; x++)
                        {
                            for (int l = 0; l < layers; l++)
                            {
                                batchSplatmap[y, x, l] = splatmapData[yStart + y, xStart + x, l];
                            }
                        }
                    }

                    terrainData.SetAlphamaps(yStart, xStart, batchSplatmap);
                }
            }
        }
    }
}
