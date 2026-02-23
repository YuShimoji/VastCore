using UnityEngine;

namespace Vastcore.WorldGen.Stamps
{
    /// <summary>
    /// SDF スタンプの抽象。
    /// </summary>
    public interface IStamp
    {
        /// <summary>
        /// ローカル座標で SDF 値を返す。
        /// </summary>
        float Evaluate(Vector3 localPosition);

        /// <summary>
        /// ローカル有効範囲を返す。
        /// size == Vector3.zero は無限。
        /// </summary>
        Bounds GetLocalBounds();
    }
}
