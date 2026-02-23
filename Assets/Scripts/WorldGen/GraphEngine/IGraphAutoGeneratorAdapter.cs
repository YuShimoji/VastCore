using Vastcore.WorldGen.Recipe;

namespace Vastcore.WorldGen.GraphEngine
{
    /// <summary>
    /// 外部 Graph 自動生成器の差し替えインターフェース。
    /// </summary>
    public interface IGraphAutoGeneratorAdapter
    {
        /// <summary>
        /// 外部生成を試行し、成功時は true を返す。
        /// 生成結果は outGraph に格納する。
        /// </summary>
        bool TryGenerate(WorldGenRecipe recipe, GraphGenerationSettings settings, int seed, out ConnectivityGraph outGraph);
    }
}
