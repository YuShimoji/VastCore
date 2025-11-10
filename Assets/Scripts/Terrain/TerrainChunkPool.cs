using System.Collections.Generic;
using UnityEngine;
using Vastcore.Terrain.Config;
using Vastcore.Terrain.Providers;

namespace Vastcore.Terrain
{
    /// <summary>
    /// TerrainChunk をプールし再利用するシンプルなオブジェクトプール。
    /// </summary>
    public sealed class TerrainChunkPool : MonoBehaviour
    {
        [SerializeField] private TerrainGenerationConfig _config;

        private readonly Stack<TerrainChunk> _pool = new Stack<TerrainChunk>();
        private readonly HashSet<TerrainChunk> _active = new HashSet<TerrainChunk>();

        private IHeightmapProvider _provider;
        private int _totalCreated;

        public TerrainGenerationConfig Config => _config;
        public int TotalCreated => _totalCreated;
        public int ActiveCount => _active.Count;
        public IReadOnlyCollection<TerrainChunk> ActiveChunks => _active;

        public void Initialize(TerrainGenerationConfig config)
        {
            _config = config;
            ReleaseAll();
            _provider = _config != null ? _config.CreateHeightProvider() : null;
        }

        public TerrainChunk Acquire(Vector2 worldOrigin, Transform parent = null)
        {
            if (_config == null || _provider == null)
            {
                Debug.LogError("TerrainChunkPool.Acquire called without config/provider");
                return null;
            }

            TerrainChunk chunk;
            if (_pool.Count > 0)
            {
                chunk = _pool.Pop();
            }
            else
            {
                var go = new GameObject("PooledTerrainChunk");
                chunk = go.AddComponent<TerrainChunk>();
                _totalCreated++;
            }

            var chunkTransform = chunk.transform;
            chunkTransform.SetParent(parent != null ? parent : transform, false);
            chunk.gameObject.SetActive(true);
            chunk.Build(_config, _provider, worldOrigin);
            _active.Add(chunk);
            return chunk;
        }

        public void Release(TerrainChunk chunk)
        {
            if (chunk == null) return;
            if (_active.Remove(chunk))
            {
                chunk.gameObject.SetActive(false);
                chunk.transform.SetParent(transform, false);
                _pool.Push(chunk);
            }
        }

        public void ReleaseAll()
        {
            if (_active.Count == 0 && _pool.Count == 0) return;

            foreach (var chunk in _active)
            {
                if (chunk == null) continue;
                chunk.gameObject.SetActive(false);
                chunk.transform.SetParent(transform, false);
                _pool.Push(chunk);
            }
            _active.Clear();
        }

        private void OnDestroy()
        {
            foreach (var chunk in _pool)
            {
                if (chunk != null)
                {
                    if (Application.isPlaying)
                        Destroy(chunk.gameObject);
                    else
                        DestroyImmediate(chunk.gameObject);
                }
            }
            _pool.Clear();
            _active.Clear();
        }
    }
}
