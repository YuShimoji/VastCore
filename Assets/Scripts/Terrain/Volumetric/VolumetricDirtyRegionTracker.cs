using System.Collections.Generic;
using UnityEngine;
using Vastcore.WorldGen.Common;

namespace Vastcore.Terrain.Volumetric
{
    /// <summary>
    /// Dirty region から影響チャンク座標を管理するトラッカー。
    /// </summary>
    public sealed class VolumetricDirtyRegionTracker
    {
        private readonly HashSet<VolumetricChunkCoord> _dirty = new HashSet<VolumetricChunkCoord>();

        /// <summary>未処理 dirty チャンク数。</summary>
        public int Count => _dirty.Count;

        /// <summary>
        /// 単一 Bounds を dirty 登録する。
        /// </summary>
        public void MarkDirty(Bounds bounds, float chunkWorldSize, Vector3 worldOrigin)
        {
            float size = Mathf.Max(0.0001f, chunkWorldSize);
            Vector3 localMin = bounds.min - worldOrigin;
            Vector3 localMax = bounds.max - worldOrigin;

            int minX = Mathf.FloorToInt(localMin.x / size);
            int minY = Mathf.FloorToInt(localMin.y / size);
            int minZ = Mathf.FloorToInt(localMin.z / size);
            int maxX = Mathf.FloorToInt(localMax.x / size);
            int maxY = Mathf.FloorToInt(localMax.y / size);
            int maxZ = Mathf.FloorToInt(localMax.z / size);

            for (int z = minZ; z <= maxZ; z++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        _dirty.Add(VolumetricChunkCoord.Create(x, y, z));
                    }
                }
            }
        }

        /// <summary>
        /// 複数 Bounds を dirty 登録する。
        /// </summary>
        public void MarkDirty(IEnumerable<Bounds> bounds, float chunkWorldSize, Vector3 worldOrigin)
        {
            if (bounds == null)
                return;
            foreach (Bounds b in bounds)
                MarkDirty(b, chunkWorldSize, worldOrigin);
        }

        /// <summary>
        /// 指定チャンクが dirty かどうか。
        /// </summary>
        public bool IsDirty(VolumetricChunkCoord coord)
        {
            return _dirty.Contains(coord);
        }

        /// <summary>
        /// 指定チャンクの dirty を解消する。
        /// </summary>
        public void ClearDirty(VolumetricChunkCoord coord)
        {
            _dirty.Remove(coord);
        }

        /// <summary>
        /// 全 dirty をクリアする。
        /// </summary>
        public void ClearAll()
        {
            _dirty.Clear();
        }
    }
}
