using UnityEngine;

namespace Vastcore.WorldGen.FieldEngine
{
    /// <summary>
    /// 連続密度場インターフェース。
    /// 正値を地形内部、負値を空間として扱う。
    /// </summary>
    public interface IDensityField
    {
        /// <summary>
        /// ワールド座標の密度をサンプルする。
        /// </summary>
        float Sample(Vector3 worldPosition);

        /// <summary>
        /// 有効範囲を返す。
        /// size == Vector3.zero は無限範囲を表す。
        /// </summary>
        Bounds GetBounds();
    }
}
