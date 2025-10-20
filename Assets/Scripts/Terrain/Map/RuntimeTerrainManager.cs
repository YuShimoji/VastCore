using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Vastcore.Utils;
using Vastcore.Generation;
using Vastcore.Core;

namespace Vastcore.Generation
{
    /// <summary>
    /// 実行時地形管理システム
    /// プレイヤー位置に基づくタイルの動的ロード/アンロード、メモリ監視、簡易統計を行う。
    /// VastcoreLogger を用いた軽量トレースログを出力する。
    /// </summary>
    public class RuntimeTerrainManager : MonoBehaviour
    {
        #region 設定/参照
        [Header("動的生成設定")]
        public bool enableDynamicGeneration = true;
        public bool enableFrameTimeControl = true;
        public int maxGenerationsPerFrame = 4;
        public int maxDeletionsPerFrame = 6;
        public int maxTilesPerUpdate = 8;   // フレーム制御時の上限
        public int minTilesPerUpdate = 1;   // フレーム制御時の最低処理数
        public float maxFrameTimeMs = 4f;   // 1フレームで許容する処理時間(ms)
        public float updateInterval = 0.1f; // 動的生成の更新間隔

        [Header("半径設定(タイル単位)")]
        public int immediateLoadRadius = 1;
        public int preloadRadius = 3;
        public int keepAliveRadius = 5;
        public int forceUnloadRadius = 7;

        [Header("メモリ管理")]
        public float memoryLimitMB = 1024f;
        public float memoryWarningThresholdMB = 768f; // 警告しきい値
        public float cleanupInterval = 2f;
        public bool enableAggressiveCleanup = false;

        [Header("デバッグ")]
        public bool showDebugInfo = false;
        public bool logTileOperations = false;
        public bool predictPlayerMovement = true;

        [Header("参照")]
        public Transform playerTransform;
        private TileManager tileManager;
        #endregion

        #region 内部状態
        // キューと状態
        private readonly Queue<TileGenerationRequest> generationQueue = new Queue<TileGenerationRequest>();
        private readonly Queue<Vector2Int> deletionQueue = new Queue<Vector2Int>();
        private readonly Dictionary<Vector2Int, TilePriority> tilePriorities = new Dictionary<Vector2Int, TilePriority>();
        private readonly HashSet<Vector2Int> processingTiles = new HashSet<Vector2Int>();

        private Coroutine dynamicGenerationCoroutine;
        private Coroutine memoryManagementCoroutine;
        private float lastUpdateTime = 0f;
        private Vector3 lastPlayerPosition = Vector3.zero;
        private Vector3 predictedPlayerPosition = Vector3.zero;

        private PerformanceStats performanceStats;
        // 一度の更新サイクル内で重い全削除を多重実行しないためのフラグ
        private bool didFullUnloadThisCycle = false;
        #endregion

        void Start()
        {
            InitializeRuntimeManager();
        }

        /// <summary>
        /// ランタイムマネージャーを初期化
        /// </summary>
        private void InitializeRuntimeManager()
        {
            Debug.Log("Initializing RuntimeTerrainManager...");
            VastcoreLogger.Instance.LogInfo("RuntimeTerrain", $"Init start dyn={(enableDynamicGeneration?1:0)} upd={updateInterval}s maxGenPerFrame={maxGenerationsPerFrame} maxDelPerFrame={maxDeletionsPerFrame}");

            // TileManagerを取得または作成
            tileManager = GetComponent<TileManager>();
            if (tileManager == null)
            {
                tileManager = gameObject.AddComponent<TileManager>();
                VastcoreLogger.Instance.LogInfo("RuntimeTerrain", "TileManager component added by RuntimeTerrainManager");
            }

            if (playerTransform == null)
            {
                playerTransform = ResolvePlayerTransform();
            }

            // TileManagerにプレイヤー参照を連携
            if (tileManager.playerTransform == null && playerTransform != null)
            {
                tileManager.playerTransform = playerTransform;
            }

            if (playerTransform != null)
            {
                lastPlayerPosition = playerTransform.position;
            }

            // コルーチンを開始
            StartDynamicGeneration();
            StartMemoryManagement();

            Debug.Log("RuntimeTerrainManager initialized successfully");
            VastcoreLogger.Instance.LogInfo("RuntimeTerrain", "Init done");
        }

        private void OnDisable()
        {
            StopCoroutinesSafely();
        }

        private void OnDestroy()
        {
            StopCoroutinesSafely();
        }

        private void StopCoroutinesSafely()
        {
            if (dynamicGenerationCoroutine != null)
            {
                StopCoroutine(dynamicGenerationCoroutine);
                dynamicGenerationCoroutine = null;
            }
            if (memoryManagementCoroutine != null)
            {
                StopCoroutine(memoryManagementCoroutine);
                memoryManagementCoroutine = null;
            }
        }

        /// <summary>
        /// 動的生成コルーチンを開始
        /// </summary>
        private void StartDynamicGeneration()
        {
            if (dynamicGenerationCoroutine != null)
            {
                StopCoroutine(dynamicGenerationCoroutine);
            }

            dynamicGenerationCoroutine = StartCoroutine(DynamicGenerationCoroutine());
            VastcoreLogger.Instance.LogDebug("RuntimeTerrain", "DynamicGeneration coroutine started");
        }

        /// <summary>
        /// メモリ管理コルーチンを開始
        /// </summary>
        private void StartMemoryManagement()
        {
            if (memoryManagementCoroutine != null)
            {
                StopCoroutine(memoryManagementCoroutine);
            }

            memoryManagementCoroutine = StartCoroutine(MemoryManagementCoroutine());
            VastcoreLogger.Instance.LogDebug("RuntimeTerrain", "MemoryManagement coroutine started");
        }

        #region プレイヤー追跡
        /// <summary>
        /// プレイヤー追跡を更新
        /// </summary>
        private void UpdatePlayerTracking()
        {
            if (playerTransform == null)
                return;

            // TileManager にプレイヤー参照を反映
            if (tileManager != null && tileManager.playerTransform != playerTransform)
            {
                tileManager.playerTransform = playerTransform;
            }

            // 予測（簡易: 直近速度ベース）
            if (predictPlayerMovement)
            {
                Vector3 velocity = (playerTransform.position - lastPlayerPosition) / Mathf.Max(Time.deltaTime, 1e-4f);
                predictedPlayerPosition = playerTransform.position + velocity * 0.25f; // 250ms 先を推定
            }

            // メモ: TileManager 側の Update が読込/削除を実施するため、ここでは要求準備のみ
            lastPlayerPosition = playerTransform.position;
        }

        #region 動的生成コルーチン
        /// <summary>
        /// 動的生成メインコルーチン
        /// </summary>
        private IEnumerator DynamicGenerationCoroutine()
        {
            while (enableDynamicGeneration)
            {
                yield return new WaitForSeconds(updateInterval);
                
                // プレイヤー追跡と周辺タイル要求の更新
                UpdatePlayerTracking();
                UpdateTileGeneration();

                // 本サイクルでの重複全削除を防止
                didFullUnloadThisCycle = false;

                if (enableFrameTimeControl)
                {
                    VastcoreLogger.Instance.LogDebug("RuntimeTerrain", "ProcessGenerationQueueWithFrameLimit start");
                    // StartCoroutine を重ねずにそのままイテレーターを返す
                    yield return ProcessGenerationQueueWithFrameLimit();
                }
                else
                {
                    VastcoreLogger.Instance.LogDebug("RuntimeTerrain", "ProcessGenerationQueue start");
                    ProcessGenerationQueue();
                }

                VastcoreLogger.Instance.LogDebug("RuntimeTerrain", "ProcessDeletionQueue start");
                ProcessDeletionQueue();
                UpdatePerformanceStats();
            }
        }

        /// <summary>
        /// フレーム時間制限付きで生成キューを処理
        /// </summary>
        private IEnumerator ProcessGenerationQueueWithFrameLimit()
        {
            float frameStartTime = Time.realtimeSinceStartup;
            int processedCount = 0;
            int safetyFrameYields = 0;
            const int maxSafetyFrameYields = 300; // 約5秒(60FPS想定)の安全弁

            while (generationQueue.Count > 0 && processedCount < maxTilesPerUpdate)
            {
                // フレーム時間をチェック
                float elapsedTime = (Time.realtimeSinceStartup - frameStartTime) * 1000f;
                if (elapsedTime > maxFrameTimeMs && processedCount >= minTilesPerUpdate)
                {
                    VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"GenQueue frame limit hit elapsed={elapsedTime:F2}ms processed={processedCount}/{maxTilesPerUpdate} q={generationQueue.Count}");
                    yield return null; // 次のフレームに延期
                    safetyFrameYields++;
                    if (!enableDynamicGeneration || !gameObject.activeInHierarchy)
                    {
                        VastcoreLogger.Instance.LogWarning("RuntimeTerrain", "GenQueue canceled due to disabled manager or inactive object");
                        yield break;
                    }
                    if (safetyFrameYields > maxSafetyFrameYields)
                    {
                        VastcoreLogger.Instance.LogError("RuntimeTerrain", $"GenQueue watchdog triggered. Breaking to avoid long-running step. processed={processedCount} remaining={generationQueue.Count}");
                        break;
                    }
                    frameStartTime = Time.realtimeSinceStartup;
                }

                var request = generationQueue.Dequeue();
                VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"GenQueueWL dequeue coord={request.coordinate} pri={request.priority} remain={generationQueue.Count}");
                ProcessTileGenerationRequest(request);
                processedCount++;

                performanceStats.tilesGeneratedThisFrame++;
            }
        }

        /// <summary>
        /// 生成キューを処理
        /// </summary>
        private void ProcessGenerationQueue()
        {
            int processedCount = 0;

            VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"GenQueue start q={generationQueue.Count}");
            while (generationQueue.Count > 0 && processedCount < maxGenerationsPerFrame)
            {
                var request = generationQueue.Dequeue();
                VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"GenQueue dequeue coord={request.coordinate} pri={request.priority} remain={generationQueue.Count}");
                ProcessTileGenerationRequest(request);
                processedCount++;
            }
            VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"GenQueue end processed={processedCount}");
        }

        /// <summary>
        /// 削除キューを処理
        /// </summary>
        private void ProcessDeletionQueue()
        {
            int processedCount = 0;

            VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"DelQueue start q={deletionQueue.Count}");
            while (deletionQueue.Count > 0 && processedCount < maxDeletionsPerFrame)
            {
                var tileCoord = deletionQueue.Dequeue();
                VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"DelQueue dequeue coord={tileCoord} remain={deletionQueue.Count}");
                ProcessTileDeletion(tileCoord);
                processedCount++;
            }
            VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"DelQueue end processed={processedCount}");
        }

        /// <summary>
        /// タイル生成リクエストを処理
        /// </summary>
        private void ProcessTileGenerationRequest(TileGenerationRequest request)
        {
            if (IsTileActive(request.coordinate))
            {
                return;
            }

            processingTiles.Add(request.coordinate);
            VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"ProcessGen start coord={request.coordinate} pri={request.priority}");

            try
            {
                // TileManagerが自動でロードを行うため、ここではトリガのみ
                var tile = tileManager.GetTileAtWorldPosition(
                    tileManager.TileCoordinateToWorldPosition(request.coordinate));

                if (tile == null)
                {
                    // 明示的な生成は行わず、TileManagerの更新に委譲
                    GenerateNewTile(request.coordinate);
                }

                performanceStats.totalTilesGenerated++;
                VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"ProcessGen done coord={request.coordinate}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to process tile generation request for {request.coordinate}: {e.Message}");
                VastcoreLogger.Instance.LogError("RuntimeTerrain", $"ProcessGen error coord={request.coordinate} msg={e.Message}");
                performanceStats.generationErrors++;
            }
            finally
            {
                processingTiles.Remove(request.coordinate);
            }
        }

        /// <summary>
        /// タイル削除を処理
        /// </summary>
        private void ProcessTileDeletion(Vector2Int tileCoord)
        {
            try
            {
                var tile = tileManager.GetTileAtWorldPosition(
                    tileManager.TileCoordinateToWorldPosition(tileCoord));

                if (tile != null)
                {
                    // 現状のフォールバック: 全削除。ただしサイクル内で一度だけ実行
                    if (!didFullUnloadThisCycle)
                    {
                        tileManager.UnloadAllTiles();
                        didFullUnloadThisCycle = true;
                        performanceStats.totalTilesDeleted++;
                        VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"ProcessDel requested coord={tileCoord} -> UnloadAllTiles() (first in cycle)");
                    }
                    else
                    {
                        VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"ProcessDel skipped full unload (already done this cycle) coord={tileCoord}");
                    }

                    if (logTileOperations)
                    {
                        Debug.Log($"Deleted tile: {tileCoord}");
                    }
                }

                tilePriorities.Remove(tileCoord);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete tile {tileCoord}: {e.Message}");
                VastcoreLogger.Instance.LogError("RuntimeTerrain", $"ProcessDel error coord={tileCoord} msg={e.Message}");
                performanceStats.deletionErrors++;
            }
        }

        /// <summary>
        /// 新しいタイルを生成
        /// </summary>
        private void GenerateNewTile(Vector2Int tileCoord)
        {
            // 個別生成の直接APIはないため、TileManager の自動処理に委譲
            // ここでは軽量ログのみを出力
            VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"GenerateNewTile requested coord={tileCoord} (delegated to TileManager)");
        }

        #endregion
        #endregion

        #region メモリ管理
        /// <summary>
        /// メモリ管理コルーチン
        /// </summary>
        private IEnumerator MemoryManagementCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(cleanupInterval);

                CheckMemoryUsage();

                if (enableAggressiveCleanup)
                {
                    PerformAggressiveCleanup();
                }

                CleanupUnusedResources();
            }
        }

        /// <summary>
        /// メモリ使用量をチェック
        /// </summary>
        private void CheckMemoryUsage()
        {
            var stats = tileManager.GetStats();
            float currentMemoryMB = stats.currentMemoryUsageMB;

            performanceStats.currentMemoryUsageMB = currentMemoryMB;

            if (currentMemoryMB > memoryLimitMB)
            {
                Debug.LogWarning($"Memory usage ({currentMemoryMB:F1}MB) exceeds limit ({memoryLimitMB}MB)");
                VastcoreLogger.Instance.LogError("RuntimeTerrain", $"Memory exceed current={currentMemoryMB:F1}MB limit={memoryLimitMB}MB");
                TriggerEmergencyCleanup();
            }
            else if (currentMemoryMB > memoryWarningThresholdMB)
            {
                Debug.LogWarning($"Memory usage ({currentMemoryMB:F1}MB) approaching limit ({memoryLimitMB}MB)");
                VastcoreLogger.Instance.LogWarning("RuntimeTerrain", $"Memory warning current={currentMemoryMB:F1}MB warn={memoryWarningThresholdMB}MB limit={memoryLimitMB}MB");
                TriggerPreventiveCleanup();
            }
        }

        /// <summary>
        /// 緊急クリーンアップをトリガー
        /// </summary>
        private void TriggerEmergencyCleanup()
        {
            if (playerTransform == null)
                return;

            Vector2Int playerTile = tileManager.WorldToTileCoordinate(playerTransform.position);
            var activeTiles = GetActiveTileCoordinates();

            // プレイヤーから遠いタイルを強制削除（フォールバックとして全削除）
            if (activeTiles.Count > 0)
            {
                tileManager.UnloadAllTiles();
            }

            performanceStats.emergencyCleanups++;
            VastcoreLogger.Instance.LogWarning("RuntimeTerrain", $"EmergencyCleanup executed (active={activeTiles.Count})");
        }

        /// <summary>
        /// 予防的クリーンアップをトリガー
        /// </summary>
        private void TriggerPreventiveCleanup()
        {
            if (playerTransform == null)
                return;

            Vector2Int playerTile = tileManager.WorldToTileCoordinate(playerTransform.position);
            var activeTiles = GetActiveTileCoordinates();

            // 最も遠いタイルからの削除は TileManager 内に委譲。ここでは全削除を回避し、ログのみ。
            VastcoreLogger.Instance.LogInfo("RuntimeTerrain", $"PreventiveCleanup check active={activeTiles.Count} around tile={playerTile}");
        }

        /// <summary>
        /// 積極的クリーンアップを実行
        /// </summary>
        private void PerformAggressiveCleanup()
        {
            tileManager.UnloadAllTiles();
            performanceStats.aggressiveCleanups++;
            VastcoreLogger.Instance.LogWarning("RuntimeTerrain", "AggressiveCleanup executed -> UnloadAllTiles()");
        }

        /// <summary>
        /// 未使用リソースをクリーンアップ
        /// </summary>
        private void CleanupUnusedResources()
        {
            // 古い優先度エントリを削除
            var activeCoords = GetActiveTileCoordinates();
            var keysToRemove = tilePriorities.Keys.Where(key => !activeCoords.Contains(key)).ToList();

            foreach (var key in keysToRemove)
            {
                tilePriorities.Remove(key);
            }

            // 処理中タイルリストをクリーンアップ
            processingTiles.RemoveWhere(coord => !activeCoords.Contains(coord));
            VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"CleanupUnusedResources removedPriorities={keysToRemove.Count}");
        }
        #endregion

        #region ユーティリティ
        /// <summary>
        /// 指定半径内のタイル座標を取得
        /// </summary>
        private List<Vector2Int> GetTilesInRadius(Vector2Int center, int radius)
        {
            var tiles = new List<Vector2Int>();
            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                for (int y = center.y - radius; y <= center.y + radius; y++)
                {
                    var coord = new Vector2Int(x, y);
                    if (Vector2Int.Distance(center, coord) <= radius)
                    {
                        tiles.Add(coord);
                    }
                }
            }
            return tiles;
        }

        /// <summary>
        /// アクティブタイル座標一覧
        /// </summary>
        private List<Vector2Int> GetActiveTileCoordinates()
        {
            var list = new List<Vector2Int>();
            if (tileManager == null) return list;
            var tiles = tileManager.GetActiveTiles();
            foreach (var t in tiles)
            {
                list.Add(t.coordinate);
            }
            return list;
        }

        /// <summary>
        /// タイルがアクティブか
        /// </summary>
        private bool IsTileActive(Vector2Int coord)
        {
            if (tileManager == null) return false;
            return tileManager.GetActiveTiles().Any(t => t.coordinate == coord);
        }

        /// <summary>
        /// 周辺のタイル要求を更新
        /// </summary>
        private void UpdateTileGeneration()
        {
            if (playerTransform == null || tileManager == null) return;
            var center = tileManager.WorldToTileCoordinate(playerTransform.position);

            // 即時/プリロード要求
            foreach (var coord in GetTilesInRadius(center, immediateLoadRadius))
            {
                RequestTileGeneration(coord, TilePriority.Immediate);
            }
            foreach (var coord in GetTilesInRadius(center, preloadRadius))
            {
                if (!IsTileActive(coord))
                    RequestTileGeneration(coord, TilePriority.High);
            }

            // 強制アンロード範囲外を削除要求
            TriggerTileCleanup(playerTransform.position);
        }

        /// <summary>
        /// アンロード対象タイルをキューに追加
        /// </summary>
        private void TriggerTileCleanup(Vector3 playerPosition)
        {
            if (tileManager == null) return;
            var center = tileManager.WorldToTileCoordinate(playerPosition);
            var active = GetActiveTileCoordinates();
            foreach (var coord in active)
            {
                float dist = Vector2Int.Distance(center, coord);
                if (dist > forceUnloadRadius)
                {
                    // 即時削除対象
                    RequestTileDeletion(coord, TilePriority.Immediate);
                }
                else if (dist > keepAliveRadius)
                {
                    // 余裕があるタイミングで削除
                    RequestTileDeletion(coord, TilePriority.Low);
                }
            }
        }

        /// <summary>
        /// タイル生成をリクエスト
        /// </summary>
        private void RequestTileGeneration(Vector2Int tileCoord, TilePriority priority)
        {
            // 既に存在するか処理中の場合はスキップ
            if (IsTileActive(tileCoord) || processingTiles.Contains(tileCoord))
            {
                VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"RequestGen skip existing/processing coord={tileCoord}");
                return;
            }

            var request = new TileGenerationRequest
            {
                coordinate = tileCoord,
                priority = priority,
                requestTime = Time.time
            };

            // 優先度に基づいてキューに挿入
            InsertGenerationRequestByPriority(request);
            tilePriorities[tileCoord] = priority;

            VastcoreLogger.Instance.LogInfo("RuntimeTerrain", $"RequestGen coord={tileCoord} pri={priority}");
            if (logTileOperations)
            {
                Debug.Log($"Requested tile generation: {tileCoord} (Priority: {priority})");
            }
        }

        /// <summary>
        /// タイル削除をリクエスト
        /// </summary>
        private void RequestTileDeletion(Vector2Int tileCoord, TilePriority priority)
        {
            if (!IsTileActive(tileCoord))
            {
                VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"RequestDel skip inactive coord={tileCoord}");
                return;
            }

            // 優先度に基づいて削除キューに追加
            if (priority == TilePriority.Immediate)
            {
                // 即座に削除
                ProcessTileDeletion(tileCoord);
                VastcoreLogger.Instance.LogInfo("RuntimeTerrain", $"RequestDel immediate coord={tileCoord}");
            }
            else
            {
                deletionQueue.Enqueue(tileCoord);
                VastcoreLogger.Instance.LogInfo("RuntimeTerrain", $"RequestDel enqueued coord={tileCoord} pri={priority} q={deletionQueue.Count}");
            }

            if (logTileOperations)
            {
                Debug.Log($"Requested tile deletion: {tileCoord} (Priority: {priority})");
            }
        }

        /// <summary>
        /// 優先度に基づいて生成リクエストを挿入
        /// </summary>
        private void InsertGenerationRequestByPriority(TileGenerationRequest request)
        {
            // 簡易実装：優先度の高いものを先頭に追加
            if (request.priority == TilePriority.Immediate)
            {
                // 即座に処理するため、キューの先頭に挿入する代わりに直接処理
                VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"InsertReq immediate -> direct process coord={request.coordinate}");
                ProcessTileGenerationRequest(request);
            }
            else
            {
                generationQueue.Enqueue(request);
                VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"InsertReq enqueued coord={request.coordinate} pri={request.priority} q={generationQueue.Count}");
            }
        }

        /// <summary>
        /// パフォーマンス統計を更新
        /// </summary>
        private void UpdatePerformanceStats()
        {
            performanceStats.frameCount++;
            performanceStats.averageFrameTime = Time.deltaTime;

            // TileManager から統計を取り込み（生成/削除の正確な反映に寄与）
            if (tileManager != null)
            {
                var stats = tileManager.GetStats();
                // 合計値の下駄を履かせないよう、最小限の同期（加算しない）
                performanceStats.currentMemoryUsageMB = stats.currentMemoryUsageMB;
            }

            if (Time.time - performanceStats.lastStatsUpdate > 1f)
            {
                performanceStats.tilesPerSecond = performanceStats.tilesGeneratedThisFrame;
                performanceStats.tilesGeneratedThisFrame = 0;
                performanceStats.lastStatsUpdate = Time.time;
            }
        }
        #endregion

        #region パブリックAPI
        /// <summary>
        /// 動的生成を有効/無効化
        /// </summary>
        public void SetDynamicGenerationEnabled(bool enabled)
        {
            enableDynamicGeneration = enabled;

            if (enabled)
            {
                StartDynamicGeneration();
                StartMemoryManagement();
            }
            else
            {
                if (dynamicGenerationCoroutine != null)
                {
                    StopCoroutine(dynamicGenerationCoroutine);
                }
                if (memoryManagementCoroutine != null)
                {
                    StopCoroutine(memoryManagementCoroutine);
                }
            }
        }

        /// <summary>
        /// パフォーマンス統計を取得
        /// </summary>
        public PerformanceStats GetPerformanceStats()
        {
            return performanceStats;
        }

        /// <summary>
        /// 設定を更新
        /// </summary>
        public void UpdateSettings(RuntimeTerrainSettings settings)
        {
            immediateLoadRadius = settings.immediateLoadRadius;
            preloadRadius = settings.preloadRadius;
            keepAliveRadius = settings.keepAliveRadius;
            forceUnloadRadius = settings.forceUnloadRadius;
            memoryLimitMB = settings.memoryLimitMB;
            maxFrameTimeMs = settings.maxFrameTimeMs;
            updateInterval = settings.updateInterval;
        }

        /// <summary>
        /// 強制クリーンアップを実行
        /// </summary>
        public void ForceCleanup()
        {
            TriggerEmergencyCleanup();
            PerformAggressiveCleanup();
        }

        /// <summary>
        /// アクティブなタイルのリストを取得
        /// </summary>
        public List<TerrainTile> GetActiveTiles()
        {
            if (tileManager != null)
            {
                return tileManager.GetActiveTiles();
            }
            return new List<TerrainTile>();
        }
        #endregion

        #region デバッグ機能
        private void OnDrawGizmos()
        {
            if (!showDebugInfo) return;
            if (!Application.isPlaying) return;
            DrawDebugInfo();
        }
        /// <summary>
        /// デバッグ情報を描画
        /// </summary>
        private void DrawDebugInfo()
        {
            if (playerTransform == null)
                return;

            Vector2Int playerTile = tileManager.WorldToTileCoordinate(playerTransform.position);
            Vector3 playerWorldPos = tileManager.TileCoordinateToWorldPosition(playerTile);

            // 各半径を描画
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerWorldPos, immediateLoadRadius * tileManager.tileSize);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerWorldPos, preloadRadius * tileManager.tileSize);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerWorldPos, keepAliveRadius * tileManager.tileSize);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(playerWorldPos, forceUnloadRadius * tileManager.tileSize);

            // 予測位置を描画
            if (predictPlayerMovement)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(predictedPlayerPosition, 50f);
                Gizmos.DrawLine(playerTransform.position, predictedPlayerPosition);
            }
        }

        /// <summary>
        /// デバッグ情報をログ出力
        /// </summary>
        [ContextMenu("Log Performance Stats")]
        public void LogPerformanceStats()
        {
            Debug.Log($"=== RuntimeTerrainManager Performance Stats ===");
            Debug.Log($"Total Tiles Generated: {performanceStats.totalTilesGenerated}");
            Debug.Log($"Total Tiles Deleted: {performanceStats.totalTilesDeleted}");
            Debug.Log($"Generation Errors: {performanceStats.generationErrors}");
            Debug.Log($"Deletion Errors: {performanceStats.deletionErrors}");
            Debug.Log($"Emergency Cleanups: {performanceStats.emergencyCleanups}");
            Debug.Log($"Aggressive Cleanups: {performanceStats.aggressiveCleanups}");
            Debug.Log($"Current Memory Usage: {performanceStats.currentMemoryUsageMB:F1}MB");
            Debug.Log($"Average Frame Time: {performanceStats.averageFrameTime * 1000f:F1}ms");
            Debug.Log($"Tiles Per Second: {performanceStats.tilesPerSecond}");
            Debug.Log($"===============================================");
        }
        #endregion

        #region データ構造
        /// <summary>
        /// タイル生成リクエスト
        /// </summary>
        [System.Serializable]
        public struct TileGenerationRequest
        {
            public Vector2Int coordinate;
            public TilePriority priority;
            public float requestTime;
        }

        /// <summary>
        /// タイル優先度
        /// </summary>
        public enum TilePriority
        {
            Low = 0,
            Medium = 1,
            High = 2,
            Immediate = 3
        }

        /// <summary>
        /// パフォーマンス統計
        /// </summary>
        [System.Serializable]
        public struct PerformanceStats
        {
            public int totalTilesGenerated;
            public int totalTilesDeleted;
            public int generationErrors;
            public int deletionErrors;
            public int emergencyCleanups;
            public int aggressiveCleanups;
            public float currentMemoryUsageMB;
            public float averageFrameTime;
            public int tilesPerSecond;
            public int tilesGeneratedThisFrame;
            public int frameCount;
            public float lastStatsUpdate;
        }

        /// <summary>
        /// ランタイム地形設定
        /// </summary>
        [System.Serializable]
        public struct RuntimeTerrainSettings
        {
            public int immediateLoadRadius;
            public int preloadRadius;
            public int keepAliveRadius;
            public int forceUnloadRadius;
            public float memoryLimitMB;
            public float maxFrameTimeMs;
            public float updateInterval;
        }
        #endregion
    }
}