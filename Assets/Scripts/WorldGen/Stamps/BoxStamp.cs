using UnityEngine;
using Vastcore.WorldGen.Common;

namespace Vastcore.WorldGen.Stamps
{
    /// <summary>
    /// 箱 SDF スタンプ。
    /// </summary>
    [CreateAssetMenu(fileName = "BoxStamp", menuName = "Vastcore/WorldGen/Stamps/Box")]
    public sealed class BoxStamp : StampBase
    {
        public Vector3 halfExtents = new Vector3(4f, 4f, 4f);

        /// <inheritdoc />
        public override float Evaluate(Vector3 localPosition)
        {
            Vector3 he = new Vector3(
                Mathf.Max(0.001f, halfExtents.x),
                Mathf.Max(0.001f, halfExtents.y),
                Mathf.Max(0.001f, halfExtents.z));
            return SdfMath.Box(localPosition, he);
        }

        /// <inheritdoc />
        public override Bounds GetLocalBounds()
        {
            Vector3 size = halfExtents * 2f;
            return new Bounds(Vector3.zero, size);
        }
    }
}
