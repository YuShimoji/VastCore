using UnityEngine;

namespace Vastcore.WorldGen.Common
{
    /// <summary>
    /// SDF (Signed Distance Field) プリミティブと Boolean 演算のユーティリティ。
    /// 正 = 表面の外側、負 = 内部、0 = 表面上。
    /// </summary>
    public static class SdfMath
    {
        // ---- Boolean 演算 ----

        public static float Union(float d1, float d2)
        {
            return Mathf.Min(d1, d2);
        }

        public static float Subtract(float d1, float d2)
        {
            return Mathf.Max(d1, -d2);
        }

        public static float Intersect(float d1, float d2)
        {
            return Mathf.Max(d1, d2);
        }

        public static float SmoothUnion(float d1, float d2, float k)
        {
            if (k <= 0f) return Union(d1, d2);
            float h = Mathf.Clamp01(0.5f + 0.5f * (d2 - d1) / k);
            return Mathf.Lerp(d2, d1, h) - k * h * (1f - h);
        }

        public static float SmoothSubtract(float d1, float d2, float k)
        {
            if (k <= 0f) return Subtract(d1, d2);
            float h = Mathf.Clamp01(0.5f - 0.5f * (d2 + d1) / k);
            return Mathf.Lerp(d1, -d2, h) + k * h * (1f - h);
        }

        public static float SmoothIntersect(float d1, float d2, float k)
        {
            if (k <= 0f) return Intersect(d1, d2);
            float h = Mathf.Clamp01(0.5f - 0.5f * (d2 - d1) / k);
            return Mathf.Lerp(d2, d1, h) + k * h * (1f - h);
        }

        // ---- SDF プリミティブ ----

        /// <summary>
        /// 原点中心の球。
        /// </summary>
        public static float Sphere(Vector3 p, float radius)
        {
            return p.magnitude - radius;
        }

        /// <summary>
        /// 原点中心の箱。halfExtents は各軸の半分サイズ。
        /// </summary>
        public static float Box(Vector3 p, Vector3 halfExtents)
        {
            Vector3 q = new Vector3(
                Mathf.Abs(p.x) - halfExtents.x,
                Mathf.Abs(p.y) - halfExtents.y,
                Mathf.Abs(p.z) - halfExtents.z);

            float outsideDist = new Vector3(
                Mathf.Max(q.x, 0f),
                Mathf.Max(q.y, 0f),
                Mathf.Max(q.z, 0f)).magnitude;

            float insideDist = Mathf.Min(Mathf.Max(q.x, Mathf.Max(q.y, q.z)), 0f);

            return outsideDist + insideDist;
        }

        /// <summary>
        /// 点 a から点 b を結ぶカプセル。
        /// </summary>
        public static float Capsule(Vector3 p, Vector3 a, Vector3 b, float radius)
        {
            Vector3 pa = p - a;
            Vector3 ba = b - a;
            float h = Mathf.Clamp01(Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba));
            return (pa - ba * h).magnitude - radius;
        }

        /// <summary>
        /// Y 軸に沿った無限高さの円柱。
        /// </summary>
        public static float CylinderY(Vector3 p, float radius)
        {
            return new Vector2(p.x, p.z).magnitude - radius;
        }

        /// <summary>
        /// Y 軸に沿った有限高さの円柱。
        /// </summary>
        public static float CylinderY(Vector3 p, float radius, float halfHeight)
        {
            float dx = new Vector2(p.x, p.z).magnitude - radius;
            float dy = Mathf.Abs(p.y) - halfHeight;
            float outsideDist = new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude;
            float insideDist = Mathf.Min(Mathf.Max(dx, dy), 0f);
            return outsideDist + insideDist;
        }
    }
}
