using System.Collections.Generic;
using Vastcore.WorldGen.Pipeline;
using Vastcore.WorldGen.Recipe;

namespace Vastcore.WorldGen.GrammarEngine
{
    /// <summary>
    /// Grammar Engine のスタブ実装。
    /// </summary>
    public sealed class GrammarEngineStub : IGrammarEngine
    {
        /// <summary>
        /// 文法エンジンが有効かどうか。
        /// </summary>
        public bool IsAvailable => false;

        /// <inheritdoc />
        public List<StructureBlueprint> GenerateStructures(WorldGenRecipe recipe, WorldGenContext context)
        {
            return new List<StructureBlueprint>();
        }
    }
}
