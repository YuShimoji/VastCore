using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Vastcore.Terrain.Facade;
using Vastcore.Terrain.MeshExtraction;
using Vastcore.Utilities;
using Vastcore.WorldGen.Common;
using Vastcore.WorldGen.FieldEngine;
using Vastcore.WorldGen.GraphEngine;
using Vastcore.WorldGen.Pipeline;
using Vastcore.WorldGen.Recipe;

namespace Vastcore.Terrain.Volumetric
{
    /// <summary>
    /// IDensityField から 3D チャンクメッシュを抽出し、ストリーミング管理する。
    /// </summary>
    public sealed class VolumetricStreamingController : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private WorldGenRecipe _recipe;
        [SerializeField] private Transform _target;

        [Header("Chunk")]
        [SerializeField, Min(0)] private int _horizontalLoadRadius = 1;
        [SerializeField, Min(0)] private int _verticalLoadRadius = 1;
        [SerializeField] private Vector3 _worldOrigin = Vector3.zero;
        [SerializeField, Min(0.01f)] private float _updateThreshold = 8f;
        [SerializeField, Min(0)] private int _maxCreatePerFrame = 2;
        [SerializeField, Min(0)] private int _maxRegeneratePerFrame = 2;
        [SerializeField] private float _isoLevel = 0f;

        [Header("Scheduling")]
        [SerializeField, Min(0.1f)] private float _maxMeshingTimeMsPerFrame = 6f;
        [SerializeField] private bool _distancePriority = true;

        [Header("Seam Mitigation")]
        [SerializeField] private bool _snapBorderVertices = true;
        [SerializeField] private bool _quantizeVertices = false;
        [SerializeField, Min(0.00001f)] private float _vertexQuantizeStep = 0.01f;
        [SerializeField, Min(0.00001f)] private float _borderSnapEpsilon = 0.15f;

        [Header("Rendering")]
        [SerializeField] private Material _chunkMaterial;

        [Header("Observability")]
        [SerializeField] private bool _enableStatsLogging = true;
        [SerializeField, Min(0.1f)] private float _statsLogInterval = 2f;

        private readonly Dictionary<VolumetricChunkCoord, VolumetricChunk> _active =
            new Dictionary<VolumetricChunkCoord, VolumetricChunk>();
        private readonly VolumetricDirtyRegionTracker _dirtyTracker = new VolumetricDirtyRegionTracker();
        private readonly DensityGridPool _gridPool = new DensityGridPool();

        private VolumetricChunkPool _pool;
        private IMeshExtractor _meshExtractor;
        private IChunkSeamProcessor _seamProcessor;
        private FieldEngineManager _fieldEngine;
        private GraphEngineManager _graphEngine;
        private WorldGenPipeline _pipeline;
        private WorldGenContext _context;

        private VolumetricChunkCoord _centerCoord;
        private Vector3 _lastTargetPosition;
        private bool _initialized;
        private float _lastStatsLogTime;

        /// <summary>
        /// 現在のコンテキスト。
        /// </summary>
        public WorldGenContext Context => _context;

        /// <summary>
        /// アクティブなボリューメトリックチャンク数。
        /// </summary>
        public int ActiveChunkCount => _active.Count;

        /// <summary>
        /// Dirty 判定されているチャンク数。
        /// </summary>
        public int DirtyChunkCount => _dirtyTracker.Count;

        /// <summary>
        /// 1フレームあたりのメッシュ生成予算 (ms)。
        /// </summary>
        public float MeshingBudgetMsPerFrame => _maxMeshingTimeMsPerFrame;

        /// <summary>
        /// レシピと追従対象を設定する。
        /// </summary>
        public void Configure(WorldGenRecipe recipe, Transform target)
        {
            _recipe = recipe;
            _target = target;
        }

        /// <summary>
        /// ボリューメトリック生成を初期化する。
        /// </summary>
        public void Initialize()
        {
            if (_recipe == null)
            {
                VastcoreLogger.Instance.LogError("VolumetricStreaming", "Initialize failed: recipe is null.");
                return;
            }

            EnsureDependencies();
            ClearAll();

            _fieldEngine = new FieldEngineManager(
                new TerrainHeightmapFieldFactory(
                    Mathf.Max(33, _recipe.chunkResolution),
                    Mathf.Max(1f, _recipe.chunkWorldSize)));

            _graphEngine = new GraphEngineManager();
            _graphEngine.GraphUpdated += OnGraphUpdated;

            _pipeline = new WorldGenPipeline(_fieldEngine, _graphEngine);
            _context = _pipeline.Execute(_recipe);

            if (_context?.GraphAffectedBounds != null)
                _dirtyTracker.MarkDirty(_context.GraphAffectedBounds, _recipe.chunkWorldSize, _worldOrigin);

            _initialized = true;
            Vector3 pos = _target != null ? _target.position : transform.position;
            UpdateStreaming(pos, true);

            VastcoreLogger.Instance.LogInfo("VolumetricStreaming", "Initialized volumetric streaming.");
        }

        private void Update()
        {
            if (!_initialized || _recipe == null || _target == null)
                return;

            Vector3 pos = _target.position;
            if ((pos - _lastTargetPosition).sqrMagnitude >= _updateThreshold * _updateThreshold)
            {
                UpdateStreaming(pos, false);
            }
            else
            {
                RegenerateDirtyChunks(_maxMeshingTimeMsPerFrame);
            }

            LogStatsIfNeeded();
        }

        /// <summary>
        /// ストリーミング更新を行う。
        /// </summary>
        public void UpdateStreaming(Vector3 targetPosition, bool force)
        {
            if (!_initialized || _recipe == null || _context == null)
                return;

            float chunkSize = Mathf.Max(1f, _recipe.chunkWorldSize);
            VolumetricChunkCoord center = WorldToCoord(targetPosition, chunkSize);

            if (!force && center == _centerCoord)
            {
                RegenerateDirtyChunks(_maxMeshingTimeMsPerFrame);
                return;
            }

            _centerCoord = center;
            _lastTargetPosition = targetPosition;

            HashSet<VolumetricChunkCoord> needed = BuildNeededSet(center);

            List<VolumetricChunkCoord> toRelease = new List<VolumetricChunkCoord>();
            foreach (KeyValuePair<VolumetricChunkCoord, VolumetricChunk> kv in _active)
            {
                if (!needed.Contains(kv.Key))
                    toRelease.Add(kv.Key);
            }

            for (int i = 0; i < toRelease.Count; i++)
            {
                VolumetricChunkCoord key = toRelease[i];
                _pool.Release(_active[key]);
                _active.Remove(key);
            }

            List<VolumetricChunkCoord> toCreate = new List<VolumetricChunkCoord>();
            foreach (VolumetricChunkCoord coord in needed)
            {
                if (!_active.ContainsKey(coord))
                    toCreate.Add(coord);
            }

            if (_distancePriority)
                SortCoordsByDistance(toCreate);

            float spentMs = 0f;
            int created = 0;
            for (int i = 0; i < toCreate.Count; i++)
            {
                VolumetricChunkCoord coord = toCreate[i];
                if (_maxCreatePerFrame > 0 && created >= _maxCreatePerFrame)
                    break;
                if (spentMs >= _maxMeshingTimeMsPerFrame)
                    break;

                Vector3 chunkOrigin = CoordToWorldOrigin(coord, chunkSize);
                VolumetricChunk chunk = _pool.Acquire(coord, chunkOrigin, _chunkMaterial, transform);
                _active.Add(coord, chunk);

                spentMs += GenerateChunkMesh(coord, chunk);
                _dirtyTracker.ClearDirty(coord);
                created++;
            }

            float remaining = Mathf.Max(0f, _maxMeshingTimeMsPerFrame - spentMs);
            RegenerateDirtyChunks(remaining);
        }

        /// <summary>
        /// 任意 Bounds を dirty 登録する。
        /// </summary>
        public void MarkDirty(Bounds bounds)
        {
            if (_recipe == null)
                return;
            _dirtyTracker.MarkDirty(bounds, _recipe.chunkWorldSize, _worldOrigin);
        }

        /// <summary>
        /// 複数 Bounds を dirty 登録する。
        /// </summary>
        public void MarkDirty(IEnumerable<Bounds> bounds)
        {
            if (_recipe == null)
                return;
            _dirtyTracker.MarkDirty(bounds, _recipe.chunkWorldSize, _worldOrigin);
        }

        /// <summary>
        /// 全チャンクを解放する。
        /// </summary>
        public void ClearAll()
        {
            if (_graphEngine != null)
                _graphEngine.GraphUpdated -= OnGraphUpdated;

            if (_pool != null)
                _pool.ReleaseAll();
            _active.Clear();
            _dirtyTracker.ClearAll();
            _gridPool.Clear();
            _initialized = false;
        }

        private void OnDestroy()
        {
            if (_graphEngine != null)
                _graphEngine.GraphUpdated -= OnGraphUpdated;
        }

        private void EnsureDependencies()
        {
            if (_pool == null)
            {
                _pool = GetComponent<VolumetricChunkPool>();
                if (_pool == null)
                    _pool = gameObject.AddComponent<VolumetricChunkPool>();
            }

            if (_meshExtractor == null)
                _meshExtractor = new MarchingCubesMeshExtractor();

            _seamProcessor = new ChunkSeamProcessor(new ChunkSeamProcessor.Options
            {
                snapBorderVertices = _snapBorderVertices,
                quantizeVertices = _quantizeVertices,
                vertexQuantizeStep = _vertexQuantizeStep,
                borderSnapEpsilon = _borderSnapEpsilon
            });
        }

        private HashSet<VolumetricChunkCoord> BuildNeededSet(VolumetricChunkCoord center)
        {
            HashSet<VolumetricChunkCoord> set = new HashSet<VolumetricChunkCoord>();
            for (int z = -_horizontalLoadRadius; z <= _horizontalLoadRadius; z++)
            {
                for (int y = -_verticalLoadRadius; y <= _verticalLoadRadius; y++)
                {
                    for (int x = -_horizontalLoadRadius; x <= _horizontalLoadRadius; x++)
                    {
                        set.Add(VolumetricChunkCoord.Create(center.X + x, center.Y + y, center.Z + z));
                    }
                }
            }
            return set;
        }

        private void RegenerateDirtyChunks(float budgetMs)
        {
            if (_dirtyTracker.Count == 0 || _active.Count == 0)
                return;
            if (budgetMs <= 0f)
                return;

            List<VolumetricChunkCoord> dirtyActive = new List<VolumetricChunkCoord>();
            foreach (KeyValuePair<VolumetricChunkCoord, VolumetricChunk> kv in _active)
            {
                if (_dirtyTracker.IsDirty(kv.Key))
                    dirtyActive.Add(kv.Key);
            }

            if (dirtyActive.Count == 0)
                return;

            if (_distancePriority)
                SortCoordsByDistance(dirtyActive);

            float spentMs = 0f;
            int regenerated = 0;
            for (int i = 0; i < dirtyActive.Count; i++)
            {
                if (_maxRegeneratePerFrame > 0 && regenerated >= _maxRegeneratePerFrame)
                    break;
                if (spentMs >= budgetMs)
                    break;

                VolumetricChunkCoord coord = dirtyActive[i];
                if (!_active.TryGetValue(coord, out VolumetricChunk chunk))
                    continue;

                spentMs += GenerateChunkMesh(coord, chunk);
                _dirtyTracker.ClearDirty(coord);
                regenerated++;
            }
        }

        private float GenerateChunkMesh(VolumetricChunkCoord coord, VolumetricChunk chunk)
        {
            if (chunk == null || _context == null || _context.DensityField == null || _recipe == null)
                return 0f;

            int resolution = Mathf.Max(8, _recipe.chunkResolution);
            float chunkSize = Mathf.Max(1f, _recipe.chunkWorldSize);
            float voxelSize = chunkSize / (resolution - 1f);

            DensityGrid grid = _gridPool.Acquire(resolution);
            ChunkBounds bounds = BuildChunkBounds(coord, chunkSize);

            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                _fieldEngine.FillDensityGrid(_context.DensityField, grid, bounds);
                Mesh mesh = _meshExtractor.ExtractMesh(grid, _isoLevel, voxelSize);
                _seamProcessor?.Process(mesh, chunkSize, voxelSize);

                chunk.SetLocation(coord, bounds.Min);
                chunk.BuildMesh(mesh);

                if (_context.Stats != null)
                {
                    int vertices = mesh != null ? mesh.vertexCount : 0;
                    int triangles = mesh != null ? mesh.triangles.Length / 3 : 0;
                    _context.Stats.RecordChunkGeneration(vertices, triangles, (float)sw.Elapsed.TotalMilliseconds);
                }
            }
            finally
            {
                sw.Stop();
                _gridPool.Release(grid);
            }

            return (float)sw.Elapsed.TotalMilliseconds;
        }

        private ChunkBounds BuildChunkBounds(VolumetricChunkCoord coord, float chunkSize)
        {
            ChunkBounds bounds = ChunkBounds.FromCoordAndSize(coord, chunkSize);
            bounds.Min += _worldOrigin;
            bounds.Max += _worldOrigin;
            return bounds;
        }

        private VolumetricChunkCoord WorldToCoord(Vector3 worldPosition, float chunkSize)
        {
            Vector3 local = worldPosition - _worldOrigin;
            return VolumetricChunkCoord.Create(
                Mathf.FloorToInt(local.x / chunkSize),
                Mathf.FloorToInt(local.y / chunkSize),
                Mathf.FloorToInt(local.z / chunkSize));
        }

        private Vector3 CoordToWorldOrigin(VolumetricChunkCoord coord, float chunkSize)
        {
            return _worldOrigin + new Vector3(coord.X * chunkSize, coord.Y * chunkSize, coord.Z * chunkSize);
        }

        private void OnGraphUpdated(GraphUpdateInfo info)
        {
            if (info == null || info.affectedBounds == null || _recipe == null)
                return;

            _dirtyTracker.MarkDirty(info.affectedBounds, _recipe.chunkWorldSize, _worldOrigin);
        }

        private void SortCoordsByDistance(List<VolumetricChunkCoord> coords)
        {
            if (coords == null || coords.Count <= 1)
                return;

            coords.Sort((a, b) =>
            {
                int dax = a.X - _centerCoord.X;
                int day = a.Y - _centerCoord.Y;
                int daz = a.Z - _centerCoord.Z;
                int dbx = b.X - _centerCoord.X;
                int dby = b.Y - _centerCoord.Y;
                int dbz = b.Z - _centerCoord.Z;
                int da = dax * dax + day * day + daz * daz;
                int db = dbx * dbx + dby * dby + dbz * dbz;
                return da.CompareTo(db);
            });
        }

        private void LogStatsIfNeeded()
        {
            if (!_enableStatsLogging || _context == null || _context.Stats == null)
                return;
            if (Time.realtimeSinceStartup - _lastStatsLogTime < _statsLogInterval)
                return;

            _lastStatsLogTime = Time.realtimeSinceStartup;
            VastcoreLogger.Instance.LogInfo(
                "VolumetricStreaming",
                $"activeChunks={_active.Count}, dirty={_dirtyTracker.Count}, meshBudgetMs={_maxMeshingTimeMsPerFrame:F2}, stats={_context.Stats}");
        }
    }
}
