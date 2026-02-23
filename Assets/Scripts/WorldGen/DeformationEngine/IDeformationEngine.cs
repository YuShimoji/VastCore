using System.Collections.Generic;
using UnityEngine;
using Vastcore.WorldGen.FieldEngine;
using Vastcore.WorldGen.Pipeline;
using Vastcore.WorldGen.Recipe;

namespace Vastcore.WorldGen.DeformationEngine
{
    /// <summary>
    /// 変形エンジン抽象。
    /// </summary>
    public interface IDeformationEngine
    {
        /// <summary>
        /// 視覚レイヤーへ変形を適用する。
        /// </summary>
        void ApplyVisual(WorldGenRecipe recipe, WorldGenContext context, Mesh mesh);

        /// <summary>
        /// 物理レイヤーへ変形を適用し dirty region を返す。
        /// </summary>
        List<DirtyRegion> ApplyPhysical(WorldGenRecipe recipe, WorldGenContext context, IDensityField field);
    }
}
