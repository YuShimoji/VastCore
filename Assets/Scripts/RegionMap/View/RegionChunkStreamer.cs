using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Unity.Profiling;
using UnityEngine;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// Ring 0（近景・Collider有効）/ Ring 1（中景）/ Ring 2（遠景スカート）を
    /// バックグラウンドスレッドで非同期生成し、メインスレッドで時間予算制（FrameBudgetMs）適用するストリーマー。
    /// Floating Origin リセンタリングも一元管理する。
    /// </summary>
    public sealed class RegionChunkStreamer : MonoBehaviour
    {
        private static readonly ProfilerMarker StreamMarker = new ProfilerMarker(ProfilerCategory.Scripts, "VastCore.Region.Stream");
        private static readonly ProfilerMarker AttachMarker = new ProfilerMarker(ProfilerCategory.Scripts, "VastCore.Region.AttachChunk");
        // Parallel.For inside each build also uses this scheduler; leave capacity for Unity's render and input threads.
        private static readonly TaskScheduler BackgroundBuilds = new ConcurrentExclusiveSchedulerPair(
            TaskScheduler.Default, Math.Max(1, Environment.ProcessorCount / 2)).ConcurrentScheduler;

        [Tooltip("1 フレームあたりのチャンク実体化・破棄に使う最大時間（ms）。")]
        public float FrameBudgetMs = 3.5f;

        [Tooltip("同時にバックグラウンド生成する最大タスク数。")]
        public int MaxConcurrentBuilds = 3;

        public event Action<Vector3> OnOriginShifted;

        public RegionData Region { get; private set; }
        public RegionWorldCoordinates Coordinates { get; private set; }
        public RegionChunkPlanner Planner { get; private set; }
        public RegionTraversalSpec TraversalSpec { get; private set; } = new RegionTraversalSpec();

        public int LoadedChunkCount { get { return _loaded.Count; } }
        public int PendingBuildCount { get { return _inFlight.Count + _buildOrder.Count + _scheduledCoverage.Count; } }

        private readonly Dictionary<RegionChunkId, RegionChunkView> _loaded = new Dictionary<RegionChunkId, RegionChunkView>();
        private readonly HashSet<RegionChunkId> _inFlight = new HashSet<RegionChunkId>();
        private readonly HashSet<RegionChunkId> _desired = new HashSet<RegionChunkId>();
        private readonly List<RegionChunkId> _buildOrder = new List<RegionChunkId>();
        private ConcurrentQueue<(RegionChunkId Id, RegionChunkView.PreparedChunk Data, Exception Error)> _readyQueue =
            new ConcurrentQueue<(RegionChunkId, RegionChunkView.PreparedChunk, Exception)>();
        private readonly Queue<RegionChunkView> _coverageQueue = new Queue<RegionChunkView>();
        private readonly HashSet<RegionChunkView> _scheduledCoverage = new HashSet<RegionChunkView>();
        private readonly Stopwatch _frameWatch = new Stopwatch();

        private Transform _chunkRoot;
        private CancellationTokenSource _cts;
        private RegionChunkId _planCenter;
        private RegionChunkId? _planDroneCenter;
        private List<RegionChunkId> _divePriority;
        private bool _hasNormalPlan;
        private bool _hasFocus;
        private bool _materialsBound;
        private RegionGrasslandCover _grassland;

        internal void BindGrasslandCamera(Camera camera)
        {
            _grassland = RegionGrasslandCover.AddTo(transform, camera, this);
        }

        internal RegionChunkData NearChunkAt(double eastM, double southM)
        {
            if (Coordinates == null || !Coordinates.ContainsWorld(eastM, southM)) return null;
            return _loaded.TryGetValue(ChunkAt(RegionChunkRing.Near, eastM, southM), out RegionChunkView view)
                ? view.Data : null;
        }

        public void Initialize(RegionData region, RegionWorldCoordinates coordinates, RegionChunkPlanner planner = null)
        {
            ClearAll();
            Region = region ?? throw new ArgumentNullException("region");
            Coordinates = coordinates ?? RegionWorldCoordinates.ForRegion(region);
            Planner = planner ?? new RegionChunkPlanner();
            _cts = new CancellationTokenSource();

            if (_chunkRoot == null)
            {
                var rootGo = new GameObject("Streamed Chunks");
                rootGo.transform.SetParent(transform, false);
                _chunkRoot = rootGo.transform;
            }

            if (!_materialsBound)
            {
                RegionSurfaceRelief.Bind();
                RegionSurfaceMaterials.Bind();
                _materialsBound = true;
            }
        }

        /// <summary>現在のフォーカス絶対座標を更新し、必要チャンクのロード・不要チャンクのアンロードを計画する。</summary>
        public void SetFocusWorld(double eastM, double southM, DroneTelemetry? visibleDrone = null)
        {
            if (Region == null || Planner == null) return;
            RegionChunkId center = ChunkAt(RegionChunkRing.Near, eastM, southM);
            RegionChunkId? droneCenter = visibleDrone.HasValue
                ? ChunkAt(RegionChunkRing.Mid, visibleDrone.Value.EastM, visibleDrone.Value.SouthM)
                : (RegionChunkId?)null;
            if (_hasNormalPlan && center == _planCenter && droneCenter == _planDroneCenter) return;
            _hasNormalPlan = true;
            _planCenter = center;
            _planDroneCenter = droneCenter;

            _hasFocus = true;
            _desired.Clear();
            _buildOrder.Clear();
            AddToPlan(Planner.PlanRequiredChunks(eastM, southM, Coordinates.WorldSizeMeters));
            if (visibleDrone.HasValue)
            {
                DroneTelemetry drone = visibleDrone.Value;
                AddToPlan(Planner.PlanRequiredChunks(drone.EastM, drone.SouthM, Coordinates.WorldSizeMeters), RegionChunkRing.Mid);
            }
        }

        /// <summary>ダイブ直前の最優先プリフェッチ（着地チャンク＋周辺中景・遠景）を投入する。</summary>
        public void PrefetchDive(double targetEastM, double targetSouthM, bool backdropOnly = false)
        {
            if (Region == null || Planner == null) return;
            _hasFocus = true;
            _hasNormalPlan = false;
            _divePriority = Planner.PlanDivePrefetch(targetEastM, targetSouthM, Coordinates.WorldSizeMeters);
            _desired.Clear();
            _buildOrder.Clear();
            AddToPlan(_divePriority);
            if (!backdropOnly)
                AddToPlan(Planner.PlanRequiredChunks(targetEastM, targetSouthM, Coordinates.WorldSizeMeters));
        }

        private void AddToPlan(List<RegionChunkId> plan, RegionChunkRing nearest = RegionChunkRing.Near)
        {
            foreach (RegionChunkId id in plan)
            {
                if (id.Ring >= nearest && _desired.Add(id) && !_loaded.ContainsKey(id) && !_inFlight.Contains(id))
                    _buildOrder.Add(id);
            }
        }

        public bool IsDiveBackdropReady()
        {
            if (_divePriority == null) return false;
            foreach (RegionChunkId id in _divePriority)
                if (!_loaded.TryGetValue(id, out RegionChunkView view) || (view.Data.Spec.HasCollider && view.Collider == null) ||
                    _scheduledCoverage.Contains(view))
                    return false;
            return true;
        }

        /// <summary>着地地点直下の Ring 0 チャンクが実体化済みか。</summary>
        public bool IsTargetNearReady(double targetEastM, double targetSouthM)
        {
            if (Coordinates == null || !Coordinates.ContainsWorld(targetEastM, targetSouthM)) return false;
            return _loaded.TryGetValue(ChunkAt(RegionChunkRing.Near, targetEastM, targetSouthM), out RegionChunkView view) &&
                   view.Collider != null;
        }

        private RegionChunkId ChunkAt(RegionChunkRing ring, double eastM, double southM)
        {
            return Planner.ChunkWithinWorld(ring, eastM, southM, Coordinates.WorldSizeMeters);
        }

        /// <summary>
        /// フォーカス対象がローカル原点から閾値を超えた場合に Floating Origin をリセンタリングする。
        /// </summary>
        public bool CheckAndRecenter(Vector3 focusLocalPos)
        {
            if (Coordinates == null || !Coordinates.NeedsRecenter(focusLocalPos.x, focusLocalPos.z))
                return false;

            Coordinates.LocalToWorld(focusLocalPos.x, focusLocalPos.y, focusLocalPos.z,
                out double eastM, out _, out double southM);
            RegionWorldCoordinates.SnapToGrid(eastM, southM, 512.0, out double newOriginE, out double newOriginS);
            if (!Coordinates.Recenter(newOriginE, newOriginS, out float shiftX, out float shiftZ))
                return false;

            var delta = new Vector3(shiftX, 0f, shiftZ);
            foreach (var kv in _loaded)
                kv.Value.ApplyOriginShift(delta);

            OnOriginShifted?.Invoke(delta);
            return true;
        }

        private void Update()
        {
            if (Region == null || !_hasFocus) return;
            TickStreaming();
        }

        public void TickStreaming()
        {
            if (Region == null || _cts == null) return;
            using var streamScope = StreamMarker.Auto();
            _frameWatch.Restart();

            foreach (RegionChunkView view in _loaded.Values)
            {
                view.AttachColliderWhenReady();
                if (_frameWatch.Elapsed.TotalMilliseconds >= FrameBudgetMs) return;
            }

            // Each coarse tile needs only one update even if several neighbouring tiles arrived or left.
            UpdateCoverageWithinBudget();

            while (_frameWatch.Elapsed.TotalMilliseconds < FrameBudgetMs && _readyQueue.TryDequeue(out var result))
            {
                _inFlight.Remove(result.Id);
                if (!_desired.Contains(result.Id) || _loaded.ContainsKey(result.Id)) continue;
                if (result.Error != null)
                {
                    UnityEngine.Debug.LogException(result.Error);
                    continue;
                }
                RegionChunkView view;
                using (AttachMarker.Auto()) view = RegionChunkView.Create(_chunkRoot, result.Data, Coordinates);
                _loaded[result.Id] = view;
                if (result.Id.Ring != RegionChunkRing.Near) ScheduleCoverage(view);
                ScheduleCoarserCoverage(view);
            }

            UpdateCoverageWithinBudget();

            // 2. 不要になったチャンクを時間予算内でアンロード
            if (_frameWatch.Elapsed.TotalMilliseconds < FrameBudgetMs)
            {
                List<RegionChunkId> toRemove = null;
                foreach (var kv in _loaded)
                {
                    if (_desired.Contains(kv.Key)) continue;
                    if (toRemove == null) toRemove = new List<RegionChunkId>();
                    toRemove.Add(kv.Key);
                }
                if (toRemove != null)
                {
                    for (int i = 0; i < toRemove.Count; i++)
                    {
                        RegionChunkId id = toRemove[i];
                        RegionChunkView view = _loaded[id];
                        _loaded.Remove(id);
                        _scheduledCoverage.Remove(view);
                        view.Dispose();
                        ScheduleCoarserCoverage(view);
                        if (_frameWatch.Elapsed.TotalMilliseconds >= FrameBudgetMs)
                            break;
                    }
                }
            }

            // 3. バックグラウンド生成タスクを空きスロットへ投入
            while (_inFlight.Count < MaxConcurrentBuilds && _buildOrder.Count > 0)
            {
                RegionChunkId next = _buildOrder[0];
                _buildOrder.RemoveAt(0);
                if (_loaded.ContainsKey(next) || _inFlight.Contains(next) || !_desired.Contains(next))
                    continue;
                _inFlight.Add(next);
                RegionData region = Region;
                RegionChunkPlanner planner = Planner;
                CancellationToken token = _cts.Token;
                var queue = _readyQueue;
                Task.Factory.StartNew(() =>
                {
                    if (token.IsCancellationRequested) return;
                    try
                    {
                        RegionChunkData built = RegionChunkBuilder.Build(region, planner, next, renderAlbedo: true);
                        if (token.IsCancellationRequested) return;
                        RegionChunkView.PreparedChunk prepared = RegionChunkView.Prepare(built);
                        if (!token.IsCancellationRequested) queue.Enqueue((next, prepared, null));
                    }
                    catch (Exception error)
                    {
                        if (!token.IsCancellationRequested) queue.Enqueue((next, null, error));
                    }
                }, token, TaskCreationOptions.DenyChildAttach, BackgroundBuilds);
            }
        }

        private void ScheduleCoarserCoverage(RegionChunkView changed)
        {
            foreach (RegionChunkView view in _loaded.Values)
                if (view.Data.Id.Ring > changed.Data.Id.Ring && _desired.Contains(view.Data.Id) && view.TouchesOrOverlaps(changed))
                    ScheduleCoverage(view);
        }

        private void ScheduleCoverage(RegionChunkView view)
        {
            if (_scheduledCoverage.Add(view)) _coverageQueue.Enqueue(view);
        }

        private void UpdateCoverageWithinBudget()
        {
            while (_coverageQueue.Count != 0 && _frameWatch.Elapsed.TotalMilliseconds < FrameBudgetMs)
            {
                RegionChunkView view = _coverageQueue.Dequeue();
                if (_scheduledCoverage.Remove(view) && _desired.Contains(view.Data.Id))
                    view.UpdateTerrainCoverage(_loaded.Values);
            }
        }

        /// <summary>
        /// ロード済みチャンク（Near → Mid → Far の優先順）または広域 RegionData から標高（m）をサンプリングする。
        /// </summary>
        public float SampleGroundHeight(double eastM, double southM)
        {
            if (Coordinates != null && Coordinates.ContainsWorld(eastM, southM))
            {
                for (int r = 0; r < 3; r++)
                {
                    RegionChunkId id = ChunkAt((RegionChunkRing)r, eastM, southM);
                    if (_loaded.TryGetValue(id, out RegionChunkView view))
                        return view.Data.HeightAt(eastM, southM);
                }
            }
            return SampleCoarseHeight(eastM, southM);
        }

        /// <summary>任意絶対座標の通行区分（RegionTraversal 基準）を返す。</summary>
        public RegionGround SampleGroundType(double eastM, double southM)
        {
            if (Coordinates == null || !Coordinates.ContainsWorld(eastM, southM)) return RegionGround.Water;
            float h = SampleGroundHeight(eastM, southM);
            float slope = 0f;
            float lakeDepth = 0f;
            float waterCov = 0f;

            if (Planner != null)
            {
                RegionChunkId nearId = ChunkAt(RegionChunkRing.Near, eastM, southM);
                if (_loaded.TryGetValue(nearId, out RegionChunkView view))
                {
                    slope = view.Data.SlopeAt(eastM, southM);
                    RegionData td = view.Data.TerrainData;
                    int gx = Mathf.Clamp(Mathf.RoundToInt((float)((eastM - view.Data.MinEastM) / view.Data.CellMeters)), 0, td.Resolution - 1);
                    int gz = Mathf.Clamp(Mathf.RoundToInt((float)((southM - view.Data.MinSouthM) / view.Data.CellMeters)), 0, td.Resolution - 1);
                    int idx = gx + gz * td.Resolution;
                    lakeDepth = td.LakeDepth[idx];
                    waterCov = td.WaterCoverage != null ? td.WaterCoverage[idx] : 0f;
                    return RegionLocomotionMath.ClassifyGround(h, lakeDepth, waterCov, slope, TraversalSpec);
                }
            }

            Coordinates.WorldMetersToMap(eastM, southM, out float mapX, out float mapY);
            int cell = Region.CellFromMap(mapX, mapY);
            lakeDepth = Region.LakeDepth[cell];
            float step = Math.Max(4f, Region.CellMeters * 0.25f);
            float dx = (SampleCoarseHeight(eastM + step, southM) - SampleCoarseHeight(eastM - step, southM)) / (2f * step);
            float dz = (SampleCoarseHeight(eastM, southM + step) - SampleCoarseHeight(eastM, southM - step)) / (2f * step);
            slope = Mathf.Sqrt(dx * dx + dz * dz);
            return RegionLocomotionMath.ClassifyGround(h, lakeDepth, waterCov, slope, TraversalSpec);
        }

        private float SampleCoarseHeight(double eastM, double southM)
        {
            if (Region == null) return 0f;
            float gx = (float)(eastM / Region.CellMeters - 0.5);
            float gz = (float)(southM / Region.CellMeters - 0.5);
            float h = RegionChunkData.SampleBilinear(Region.Height, Region.Resolution, gx, gz);
            return h > 0f ? h : 0f;
        }

        internal bool IsOpenSea(double eastM, double southM)
        {
            // The detail sampler warps by a fraction of a coarse cell. Only reject water early
            // when all surrounding coarse samples are below sea level.
            int resolution = Region.Resolution;
            int x = Mathf.FloorToInt((float)(eastM / Region.CellMeters - 0.5));
            int z = Mathf.FloorToInt((float)(southM / Region.CellMeters - 0.5));
            for (int y = z - 1; y <= z + 2; y++)
            {
                for (int i = x - 1; i <= x + 2; i++)
                {
                    int index = Mathf.Clamp(i, 0, resolution - 1) + Mathf.Clamp(y, 0, resolution - 1) * resolution;
                    if (Region.Height[index] > 0f) return false;
                }
            }
            return true;
        }

        public void ClearAll()
        {
            if (_grassland != null) _grassland.Clear();
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
            _readyQueue = new ConcurrentQueue<(RegionChunkId, RegionChunkView.PreparedChunk, Exception)>();
            _coverageQueue.Clear();
            _scheduledCoverage.Clear();
            _inFlight.Clear();
            _desired.Clear();
            _buildOrder.Clear();
            foreach (var kv in _loaded)
                kv.Value.Dispose();
            _loaded.Clear();
            _hasFocus = false;
            _hasNormalPlan = false;
            _divePriority = null;
            Region = null;
            Coordinates = null;
            Planner = null;
        }

        private void OnDestroy()
        {
            ClearAll();
        }
    }
}
