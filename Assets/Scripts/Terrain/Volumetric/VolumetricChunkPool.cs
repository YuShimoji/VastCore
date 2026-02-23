using System.Collections.Generic;
using UnityEngine;
using Vastcore.WorldGen.Common;

namespace Vastcore.Terrain.Volumetric
{
    /// <summary>
    /// VolumetricChunk のプール管理。
    /// </summary>
    public sealed class VolumetricChunkPool : MonoBehaviour
    {
        private readonly Stack<VolumetricChunk> _pool = new Stack<VolumetricChunk>();
        private readonly HashSet<VolumetricChunk> _active = new HashSet<VolumetricChunk>();
        private int _created;

        /// <summary>作成総数。</summary>
        public int CreatedCount => _created;

        /// <summary>アクティブ数。</summary>
        public int ActiveCount => _active.Count;

        /// <summary>
        /// チャンクを取得する。
        /// </summary>
        public VolumetricChunk Acquire(VolumetricChunkCoord coord, Vector3 worldOrigin, Material material, Transform parent)
        {
            VolumetricChunk chunk;
            if (_pool.Count > 0)
            {
                chunk = _pool.Pop();
            }
            else
            {
                GameObject go = new GameObject("PooledVolumetricChunk");
                chunk = go.AddComponent<VolumetricChunk>();
                _created++;
            }

            chunk.transform.SetParent(parent != null ? parent : transform, false);
            chunk.Initialize(material);
            chunk.SetLocation(coord, worldOrigin);
            chunk.gameObject.SetActive(true);
            _active.Add(chunk);
            return chunk;
        }

        /// <summary>
        /// チャンクを返却する。
        /// </summary>
        public void Release(VolumetricChunk chunk)
        {
            if (chunk == null)
                return;
            if (_active.Remove(chunk))
            {
                chunk.ClearMesh();
                chunk.gameObject.SetActive(false);
                chunk.transform.SetParent(transform, false);
                _pool.Push(chunk);
            }
        }

        /// <summary>
        /// 全アクティブチャンクを返却する。
        /// </summary>
        public void ReleaseAll()
        {
            if (_active.Count == 0)
                return;

            List<VolumetricChunk> active = new List<VolumetricChunk>(_active);
            for (int i = 0; i < active.Count; i++)
                Release(active[i]);
            _active.Clear();
        }

        private void OnDestroy()
        {
            ReleaseAll();
            while (_pool.Count > 0)
            {
                VolumetricChunk chunk = _pool.Pop();
                if (chunk == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(chunk.gameObject);
                else
                    DestroyImmediate(chunk.gameObject);
            }
        }
    }
}
