using UnityEngine;
using Vastcore.WorldGen.Common;

namespace Vastcore.WorldGen.Stamps
{
    /// <summary>
    /// 球 SDF スタンプ。
    /// </summary>
    [CreateAssetMenu(fileName = "SphereStamp", menuName = "Vastcore/WorldGen/Stamps/Sphere")]
    public sealed class SphereStamp : StampBase
    {
        [Min(0.001f)] public float radius = 8f;

        /// <inheritdoc />
        public override float Evaluate(Vector3 localPosition)
        {
            return SdfMath.Sphere(localPosition, radius);
        }

        /// <inheritdoc />
        public override Bounds GetLocalBounds()
        {
            float d = radius * 2f;
            return new Bounds(Vector3.zero, new Vector3(d, d, d));
        }
    }
}
