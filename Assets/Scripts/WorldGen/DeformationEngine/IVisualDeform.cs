using UnityEngine;

namespace Vastcore.WorldGen.DeformationEngine
{
    /// <summary>
    /// 描画メッシュ専用の変形抽象。
    /// </summary>
    public interface IVisualDeform
    {
        /// <summary>
        /// 頂点を変形する。
        /// </summary>
        void Apply(Mesh mesh);
    }
}
