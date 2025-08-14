using UnityEngine;

namespace Vastcore.Generation
{
    public static class TerrainOptimizer
    {
        public static void OptimizeTerrainSettings(TerrainGenerator generator)
        {
            if (generator.GeneratedTerrain == null) return;

            generator.GeneratedTerrain.treeDistance = generator.TreeDistance;
            generator.GeneratedTerrain.treeBillboardDistance = generator.TreeBillboardDistance;
            generator.GeneratedTerrain.treeCrossFadeLength = generator.TreeCrossFadeLength;
            generator.GeneratedTerrain.treeMaximumFullLODCount = generator.TreeMaximumFullLODCount;
            generator.GeneratedTerrain.detailObjectDistance = generator.DetailDistance;
        }
    }
}
