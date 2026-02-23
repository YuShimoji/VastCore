using Vastcore.WorldGen.Recipe;

namespace Vastcore.WorldGen.GraphEngine
{
    /// <summary>
    /// Recipe からグラフ表示用データを生成するユーティリティ。
    /// </summary>
    public static class GraphPreviewUtility
    {
        /// <summary>
        /// グラフのプレビューを生成する。
        /// </summary>
        public static ConnectivityGraph BuildPreviewGraph(WorldGenRecipe recipe, GraphAsset graphAssetOverride = null)
        {
            if (graphAssetOverride != null)
                return graphAssetOverride.CreateRuntimeCopy();

            if (recipe == null)
                return null;

            GraphGenerationSettings settings = recipe.graphSettings;
            if (settings == null || !settings.enableGraph)
                return null;

            if (settings.useGraphAssetWhenAvailable && recipe.graphAsset != null)
                return recipe.graphAsset.CreateRuntimeCopy();

            ConnectivityGraph graph = new ConnectivityGraph();
            if (settings.generateRoads)
            {
                RoadGraphGenerator road = new RoadGraphGenerator();
                road.AppendRoads(graph, settings, recipe.seed + settings.roadSeedOffset);
            }
            if (settings.generateRivers)
            {
                RiverGraphGenerator river = new RiverGraphGenerator();
                river.AppendRivers(graph, settings, recipe.seed + settings.riverSeedOffset);
            }
            return graph;
        }
    }
}
