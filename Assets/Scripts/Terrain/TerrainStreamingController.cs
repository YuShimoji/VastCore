using System.Collections.Generic;
using UnityEngine;
using Vastcore.Terrain.Config;

namespace Vastcore.Terrain
{
    /// <summary>
    /// カメラやターゲット位置に基づいてチャンクのストリーミングを制御するコンポーネント。
    /// </summary>
    public sealed class TerrainStreamingController : MonoBehaviour
    {
        [Header("Streaming Settings")]
        public Transform target;
        public TerrainGenerationConfig config;
        [Min(0)] public int loadRadius = 1;
        [Tooltip("チャンクのワールド原点。TerrainGenerationConfig.worldSize 単位で軸方向に増減する。")]
        public Vector2 worldOrigin = Vector2.zero;
        [Tooltip("ターゲット移動がこの距離を超えたらストリーミング判定を行う。")]
        [Min(0.01f)] public float updateThreshold = 10f;
        [Tooltip("1フレームで生成するチャンク数の上限。0 で制限なし。")]
        [Min(0)] public int maxLoadPerFrame = 0;

        private TerrainChunkPool _pool;
        private readonly Dictionary<Vector2Int, TerrainChunk> _active = new Dictionary<Vector2Int, TerrainChunk>();

        private Vector3 _lastTargetPos;
        private Vector2Int _currentCenter;
        private bool _initialized;

        public IReadOnlyDictionary<Vector2Int, TerrainChunk> ActiveChunks => _active;
        public Vector2Int CurrentCenter => _currentCenter;
        public TerrainChunkPool Pool => _pool;

        private void Awake()
        {
            EnsurePool();
        }

        private void Start()
        {
            if (target == null && Camera.main != null)
            {
                target = Camera.main.transform;
            }

            if (!_initialized)
            {
                Initialize();
            }
        }

        private void Update()
        {
            if (!_initialized) return;
            if (target == null) return;

            var pos = target.position;
            float dist = (pos - _lastTargetPos).sqrMagnitude;
            if (dist >= updateThreshold * updateThreshold)
            {
                UpdateStreaming(pos);
            }
        }

        public void Initialize()
        {
            if (config == null)
            {
                Debug.LogError("TerrainStreamingController: config is null");
                return;
            }

            EnsurePool();

            _pool.Initialize(config);
            _active.Clear();
            _initialized = true;

            var pos = target != null ? target.position : transform.position;
            UpdateStreaming(pos, force: true);
        }

        public void UpdateStreaming(Vector3 targetPosition, bool force = false)
        {
            if (!_initialized || config == null)
                return;

            float size = Mathf.Max(1f, config.worldSize);
            Vector2 relative = new Vector2(targetPosition.x - worldOrigin.x, targetPosition.z - worldOrigin.y);
            int cx = Mathf.FloorToInt(relative.x / size);
            int cz = Mathf.FloorToInt(relative.y / size);
            var newCenter = new Vector2Int(cx, cz);

            if (!force && newCenter == _currentCenter)
                return;

            _currentCenter = newCenter;
            _lastTargetPos = targetPosition;

            var needed = new HashSet<Vector2Int>();
            for (int dz = -loadRadius; dz <= loadRadius; dz++)
            {
                for (int dx = -loadRadius; dx <= loadRadius; dx++)
                {
                    needed.Add(new Vector2Int(cx + dx, cz + dz));
                }
            }

            var toRelease = new List<Vector2Int>();
            foreach (var kv in _active)
            {
                if (!needed.Contains(kv.Key))
                {
                    toRelease.Add(kv.Key);
                }
            }

            foreach (var coord in toRelease)
            {
                _pool.Release(_active[coord]);
                _active.Remove(coord);
            }

            int loadedThisFrame = 0;
            foreach (var coord in needed)
            {
                if (_active.ContainsKey(coord))
                    continue;

                if (maxLoadPerFrame > 0 && loadedThisFrame >= maxLoadPerFrame)
                    break;

                Vector2 origin = new Vector2(worldOrigin.x + coord.x * size, worldOrigin.y + coord.y * size);
                var chunk = _pool.Acquire(origin, transform);
                if (chunk != null)
                {
                    _active.Add(coord, chunk);
                    loadedThisFrame++;
                }
            }
        }

        public void ClearAll()
        {
            foreach (var kv in _active)
            {
                _pool.Release(kv.Value);
            }
            _active.Clear();
        }

        private void EnsurePool()
        {
            if (_pool == null)
            {
                _pool = GetComponent<TerrainChunkPool>();
                if (_pool == null)
                {
                    _pool = gameObject.AddComponent<TerrainChunkPool>();
                }
            }
        }
    }
}
