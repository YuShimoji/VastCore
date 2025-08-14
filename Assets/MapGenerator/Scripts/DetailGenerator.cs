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

            terrainData.detailPrototypes = generator.DetailPrototypes;
            terrainData.wavingGrassStrength = 0.4f;
            terrainData.wavingGrassSpeed = 0.4f;
            terrainData.wavingGrassAmount = 0.4f;
            terrainData.wavingGrassTint = Color.white;

            int detailResolution = terrainData.detailResolution;
            for (int i = 0; i < terrainData.detailPrototypes.Length; i++)
            {
                int[,] detailLayer = new int[detailResolution, detailResolution];
                for (int y = 0; y < detailResolution; y++)
                {
                    for (int x = 0; x < detailResolution; x++)
                    {
                        // ここで詳細オブジェクトの配置ロジックを実装します。
                        // 例えば、特定のテクスチャが塗られている場所にのみ草を生やすなど。
                        // この例では、ランダムに配置します。
                        if (Random.value > 0.95f) 
                        {
                           detailLayer[x, y] = 1;
                        }
                    }
                }
                terrainData.SetDetailLayer(0, 0, i, detailLayer);
            }
        }
    }
}
