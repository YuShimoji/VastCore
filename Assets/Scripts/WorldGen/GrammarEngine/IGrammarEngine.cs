using System.Collections.Generic;
using Vastcore.WorldGen.Pipeline;
using Vastcore.WorldGen.Recipe;

namespace Vastcore.WorldGen.GrammarEngine
{
    /// <summary>
    /// Grammar Engine の抽象。
    /// </summary>
    public interface IGrammarEngine
    {
        /// <summary>
        /// 構造設計図を生成する。
        /// </summary>
        List<StructureBlueprint> GenerateStructures(WorldGenRecipe recipe, WorldGenContext context);
    }
}
