namespace Vastcore.WorldGen.Recipe
{
    /// <summary>
    /// SDF / 密度場の Boolean 演算タイプ。
    /// </summary>
    public enum BooleanOp
    {
        /// <summary>
        /// 和集合: min(d1, d2)
        /// </summary>
        Union,
        /// <summary>
        /// 差集合: max(d1, -d2)
        /// </summary>
        Subtract,
        /// <summary>
        /// 積集合: max(d1, d2)
        /// </summary>
        Intersect,
        /// <summary>
        /// 滑らかな和集合。
        /// </summary>
        SmoothUnion,
        /// <summary>
        /// 滑らかな差集合。
        /// </summary>
        SmoothSubtract,
        /// <summary>
        /// 滑らかな積集合。
        /// </summary>
        SmoothIntersect
    }
}
