using System.Collections.Generic;
using UnityEngine;
using Vastcore.WorldGen.FieldEngine;
using Vastcore.WorldGen.Pipeline;
using Vastcore.WorldGen.Recipe;

namespace Vastcore.WorldGen.DeformationEngine
{
    /// <summary>
    /// 変形エンジンのスタブ実装。
    /// </summary>
    public sealed class DeformationEngineStub : IDeformationEngine
    {
        /// <inheritdoc />
        public void ApplyVisual(WorldGenRecipe recipe, WorldGenContext context, Mesh mesh)
        {
            // M5 で実装。
        }

        /// <inheritdoc />
        public List<DirtyRegion> ApplyPhysical(WorldGenRecipe recipe, WorldGenContext context, IDensityField field)
        {
            return new List<DirtyRegion>();
        }
    }
}
