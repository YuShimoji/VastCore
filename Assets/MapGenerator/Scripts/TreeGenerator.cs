using UnityEngine;

namespace Vastcore.Generation
{
    public static class TreeGenerator
    {
        public static void ConfigureTrees(TerrainGenerator generator, TerrainData terrainData)
        {
            if (generator.TreePrototypes == null || generator.TreePrototypes.Length == 0)
            {
                return;
            }

            terrainData.treePrototypes = generator.TreePrototypes;
            terrainData.RefreshPrototypes();

            // 既存のツリーをクリア
            terrainData.treeInstances = new TreeInstance[0];

            // ここでツリーの配置ロジックを実装します。
            // この例では、ランダムにツリーを配置します。
            for (int i = 0; i < 500; i++)
            {
                float x = Random.value;
                float z = Random.value;
                float height = terrainData.GetHeight((int)(x * terrainData.heightmapResolution), (int)(z * terrainData.heightmapResolution));

                if (height / terrainData.size.y < 0.4f) // 標高が低い場所にのみ木を配置
                {
                    TreeInstance treeInstance = new TreeInstance();
                    treeInstance.position = new Vector3(x, height / terrainData.size.y, z);
                    treeInstance.prototypeIndex = Random.Range(0, terrainData.treePrototypes.Length);
                    treeInstance.widthScale = 1f;
                    treeInstance.heightScale = 1f;
                    treeInstance.color = Color.white;
                    treeInstance.lightmapColor = Color.white;
                    terrainData.AddTreeInstance(treeInstance);
                }
            }
        }
    }
}
