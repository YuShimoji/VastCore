using Vastcore.WorldGen.FieldEngine;
using Vastcore.WorldGen.Pipeline;
using Vastcore.WorldGen.Recipe;

namespace Vastcore.WorldGen.GraphEngine
{
    /// <summary>
    /// 道路・河川ネットワーク生成エンジンの抽象。
    /// </summary>
    public interface IGraphEngine
    {
        /// <summary>
        /// グラフを生成する。
        /// </summary>
        ConnectivityGraph GenerateGraph(WorldGenRecipe recipe, WorldGenContext context);

        /// <summary>
        /// グラフを密度場へ焼き付けた新しい密度場を返す。
        /// </summary>
        IDensityField BurnIntoField(IDensityField baseField, ConnectivityGraph graph, WorldGenRecipe recipe);
    }
}
