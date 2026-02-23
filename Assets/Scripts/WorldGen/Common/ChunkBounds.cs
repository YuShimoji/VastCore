using UnityEngine;

namespace Vastcore.WorldGen.Common
{
    /// <summary>
    /// ボリューメトリックチャンクのワールド空間 AABB。
    /// </summary>
    public struct ChunkBounds
    {
        public Vector3 Min;
        public Vector3 Max;

        /// <summary>
        /// チャンク座標とチャンクサイズから AABB を計算する。
        /// </summary>
        public static ChunkBounds FromCoordAndSize(VolumetricChunkCoord coord, float chunkWorldSize)
        {
            return new ChunkBounds
            {
                Min = new Vector3(
                    coord.X * chunkWorldSize,
                    coord.Y * chunkWorldSize,
                    coord.Z * chunkWorldSize),
                Max = new Vector3(
                    (coord.X + 1) * chunkWorldSize,
                    (coord.Y + 1) * chunkWorldSize,
                    (coord.Z + 1) * chunkWorldSize)
            };
        }

        /// <summary>
        /// AABB のサイズ。
        /// </summary>
        public Vector3 Size => Max - Min;

        /// <summary>
        /// AABB の中心。
        /// </summary>
        public Vector3 Center => (Min + Max) * 0.5f;

        /// <summary>
        /// 指定した AABB と交差するかを判定する。
        /// </summary>
        public bool Intersects(ChunkBounds other)
        {
            return Min.x <= other.Max.x && Max.x >= other.Min.x
                && Min.y <= other.Max.y && Max.y >= other.Min.y
                && Min.z <= other.Max.z && Max.z >= other.Min.z;
        }

        /// <summary>
        /// 指定した点が AABB 内に含まれるかを判定する。
        /// </summary>
        public bool Contains(Vector3 point)
        {
            return point.x >= Min.x && point.x <= Max.x
                && point.y >= Min.y && point.y <= Max.y
                && point.z >= Min.z && point.z <= Max.z;
        }

        /// <summary>
        /// Unity の Bounds に変換する。
        /// </summary>
        public Bounds ToBounds()
        {
            return new Bounds(Center, Size);
        }
    }
}
