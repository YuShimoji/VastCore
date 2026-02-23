using Vastcore.WorldGen.FieldEngine;
using Vastcore.WorldGen.Pipeline;
using Vastcore.WorldGen.Recipe;

namespace Vastcore.WorldGen.GraphEngine
{
    /// <summary>
    /// Graph Engine の最小スタブ実装。
    /// </summary>
    public sealed class GraphEngineStub : IGraphEngine
    {
        /// <inheritdoc />
        public ConnectivityGraph GenerateGraph(WorldGenRecipe recipe, WorldGenContext context)
        {
            return new ConnectivityGraph();
        }

        /// <inheritdoc />
        public IDensityField BurnIntoField(IDensityField baseField, ConnectivityGraph graph, WorldGenRecipe recipe)
        {
            return baseField;
        }
    }
}
