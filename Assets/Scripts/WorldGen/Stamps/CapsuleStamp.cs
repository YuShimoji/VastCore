using UnityEngine;
using Vastcore.WorldGen.Common;

namespace Vastcore.WorldGen.Stamps
{
    /// <summary>
    /// Y 軸向きカプセル SDF スタンプ。
    /// </summary>
    [CreateAssetMenu(fileName = "CapsuleStamp", menuName = "Vastcore/WorldGen/Stamps/Capsule")]
    public sealed class CapsuleStamp : StampBase
    {
        [Min(0.001f)] public float radius = 2f;
        [Min(0.001f)] public float height = 8f;

        /// <inheritdoc />
        public override float Evaluate(Vector3 localPosition)
        {
            float halfHeight = Mathf.Max(radius, height * 0.5f);
            Vector3 a = new Vector3(0f, -halfHeight, 0f);
            Vector3 b = new Vector3(0f, halfHeight, 0f);
            return SdfMath.Capsule(localPosition, a, b, radius);
        }

        /// <inheritdoc />
        public override Bounds GetLocalBounds()
        {
            float y = Mathf.Max(height, radius * 2f);
            float d = radius * 2f;
            return new Bounds(Vector3.zero, new Vector3(d, y, d));
        }
    }
}
