using UnityEngine;

namespace Vastcore.Generation
{
    public static class DetailGenerator
    {
        public static void ConfigureDetailMap(TerrainGenerator generator, TerrainData terrainData)
        {
            if (generator.DetailPrototypes == null || generator.DetailPrototypes.Length == 0)
            {
                return;
            }

            // プロトタイプ適用
            terrainData.detailPrototypes = generator.DetailPrototypes;
            terrainData.wavingGrassStrength = 0.4f;
            terrainData.wavingGrassSpeed = 0.4f;
            terrainData.wavingGrassAmount = 0.4f;
            terrainData.wavingGrassTint = Color.white;

            // 解像度の適用
            int desiredRes = Mathf.Clamp(generator.DetailResolution, 32, 4096);
            int perPatch = Mathf.Clamp(generator.DetailResolutionPerPatch, 8, 128);
            terrainData.SetDetailResolution(desiredRes, perPatch);

            int detailResolution = terrainData.detailResolution;
            int layers = terrainData.detailPrototypes.Length;

            // 各レイヤーごとに配置（単純なルール: 低傾斜かつ中高度に密度高）
            for (int layer = 0; layer < layers; layer++)
            {
                int[,] detailLayer = new int[detailResolution, detailResolution];

                for (int y = 0; y < detailResolution; y++)
                {
                    float v = (y + 0.5f) / detailResolution;
                    for (int x = 0; x < detailResolution; x++)
                    {
                        float u = (x + 0.5f) / detailResolution;

                        // 高さを 0..1 に正規化
                        float worldHeight = terrainData.GetInterpolatedHeight(u, v);
                        float normHeight = Mathf.Clamp01(worldHeight / terrainData.size.y);
                        float slope = terrainData.GetSteepness(u, v); // 0..90+ deg
                        float slopeNorm = Mathf.Clamp01(slope / 45f);

                        // 密度ルール: 中高度(0.2..0.7)かつ低傾斜ほど高密度
                        float heightWeight = Mathf.InverseLerp(0.1f, 0.5f, normHeight) * (1f - Mathf.InverseLerp(0.5f, 0.9f, normHeight));
                        heightWeight = Mathf.Clamp01(heightWeight * 2f);
                        float slopeWeight = 1f - slopeNorm; // 平坦ほど 1

                        float density = heightWeight * slopeWeight;
                        density *= Mathf.Clamp01(generator.DetailDensity);

                        // レイヤー別の微調整（とりあえず layer index による弱い変化）
                        float layerMul = 1f - (layer * 0.1f);
                        density *= Mathf.Clamp01(layerMul);

                        // 確率的配置
                        if (Random.value < density)
                        {
                            detailLayer[x, y] = 1;
                        }
                        else
                        {
                            detailLayer[x, y] = 0;
                        }
                    }
                }
                terrainData.SetDetailLayer(0, 0, layer, detailLayer);
            }
        }
    }
}
