using UnityEngine;

namespace Vastcore.WorldGen.Stamps
{
    /// <summary>
    /// ScriptableObject ベースの Stamp 抽象クラス。
    /// </summary>
    public abstract class StampBase : ScriptableObject, IStamp
    {
        /// <inheritdoc />
        public abstract float Evaluate(Vector3 localPosition);

        /// <inheritdoc />
        public abstract Bounds GetLocalBounds();
    }
}
