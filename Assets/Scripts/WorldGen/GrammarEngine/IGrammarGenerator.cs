using System.Collections.Generic;
using Vastcore.WorldGen.Pipeline;
using Vastcore.WorldGen.Recipe;

namespace Vastcore.WorldGen.GrammarEngine
{
    /// <summary>
    /// 建築文法ジェネレータの抽象。
    /// </summary>
    public interface IGrammarGenerator
    {
        /// <summary>
        /// 構造設計図を生成する。
        /// </summary>
        List<StructureBlueprint> Generate(WorldGenRecipe recipe, WorldGenContext context);
    }
}
