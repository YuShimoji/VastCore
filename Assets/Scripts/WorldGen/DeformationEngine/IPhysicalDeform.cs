using System.Collections.Generic;
using Vastcore.WorldGen.FieldEngine;

namespace Vastcore.WorldGen.DeformationEngine
{
    /// <summary>
    /// 物理密度場へ反映する変形抽象。
    /// </summary>
    public interface IPhysicalDeform
    {
        /// <summary>
        /// 密度場へ変形を適用し、dirty region を返す。
        /// </summary>
        List<DirtyRegion> Apply(IDensityField field);
    }
}
