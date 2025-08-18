using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vastcore.Generation;

namespace Vastcore.Generation
{
    /// <summary>
    /// 実行時地形管理システム
    /// 要求6.2, 6.5: プレイヤー位置に基づく動的タイル生成・削除とメモリリーク防止
    /// </summary>
    public class RuntimeTerrainManager : MonoBehaviour
    {
        #region 設定パラメータ
        [Header("動的生成設定")]
        public bool enableDynamicGeneration = true;         // 動的生成有効化
        public float updateInterval = 0.2f;                 // 更新間隔（秒）
        public int maxGenerationsPerFrame = 1;              // フレーム毎の最大生成数
        public int maxDeletionsPerFrame = 3;                // フレーム毎の最大削除数
        
        [Header("プレイヤー追跡")]
        public Transform playerTransform;                   // プレイヤーのTransform
        public float playerMoveThreshold = 50f;             // プレイヤー移動検知閾値
        public bool predictPlayerMovement = true;           // プレイヤー移動予測
        public float predictionTime = 2f;                   // 予測時間（秒）
        
        [Header("タイル範囲設定")]
        public int immediateLoadRadius = 2;                 // 即座に読み込む半径
        public int preloadRadius = 4;                       // 事前読み込み半径
        public int keepAliveRadius = 6;                     // 保持半径
        public int forceUnloadRadius = 8;                   // 強制削除半径
        
        [Header("メモリ管理")]
        public float memoryLimitMB = 800f;                  // メモリ制限（MB）
        public float memoryWarningThresholdMB = 600f;       // メモリ警告閾値（MB）
        public bool enableAggressiveCleanup = true;         // 積極的クリーンアップ
        public float cleanupInterval = 5f;                  // クリーンアップ間隔（秒）
        
        [Header("パフォーマンス制御")]
        public float maxFrameTimeMs = 16f;                  // 最大フレーム時間（ミリ秒）
        public bool enableFrameTimeControl = true;          // フレーム時間制御有効化
        public int minTilesPerUpdate = 1;                   // 更新毎の最小タイル数
        public int maxTilesPerUpdate = 5;                   // 更新毎の最大タイル数
        
        [Header("デバッグ設定")]
        public bool showDebugInfo = true;                   // デバッグ情報表示
        public bool logTileOperations = false;              // タイル操作ログ
        public bool showPerformanceStats = true;            // パフォーマンス統計表示
        #endregion

        #region プライベート変数
        private TileManager tileManager;
        private Vector3 lastPlayerPosition;
        private Vector3 playerVelocity;
        private Vector3 predictedPlayerPosition;
        
        // 動的生成キュー
        private Queue<TileGenerationRequest> generationQueue = new Queue<TileGenerationRequest>();
        private Queue<Vector2Int> deletionQueue = new Queue<Vector2Int>();
        private HashSet<Vector2Int> processingTiles = new HashSet<Vector2Int>();
        
        // タイル優先度管理
        private Dictionary<Vector2Int, TilePriority> tilePriorities = new Dictionary<Vector2Int, TilePriority>();
        
        // パフォーマンス統計
        private PerformanceStats performanceStats = new PerformanceStats();
        private float lastCleanupTime = 0f;
        private float lastUpdateTime = 0f;
        
        // コルーチン管理
        private Coroutine dynamicGenerationCoroutine;
        private Coroutine memoryManagementCoroutine;
        #endregion

        #region Unity イベント
        void Start()
        {
            InitializeRuntimeManager();
        }
        
        void Update()
        {
            if (!enableDynamicGeneration)
                return;
            
            UpdatePlayerTracking();
            
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateTileGeneration();
                lastUpdateTime = Time.time;
            }
        }
        
        void OnDestroy()
        {
            StopAllCoroutines();
        }
        
        void OnDrawGizmos()
        {
            if (!showDebugInfo || !Application.isPlaying || playerTransform == null)
                return;
            
            DrawDebugInfo();
        }
        #endregion

        #region 初期化
        /// <summary>
        /// ランタイムマネージャーを初期化
        /// </summary>
        private void InitializeRuntimeManager()
        {
            Debug.Log("Initializing RuntimeTerrainManager...");
            
            // TileManagerを取得または作成
            tileManager = GetComponent<TileManager>();
            if (tileManager == null)
            {
                tileManager = gameObject.AddComponent<TileManager>();
            }
            
            // プレイヤーを設定
            if (playerTransform == null)
            {
                playerTransform = tileManager.playerTransform;
            }
            
            if (playerTransform != null)
            {
                lastPlayerPosition = playerTransform.position;
            }
            
            // コルーチンを開始
            StartDynamicGeneration();
            StartMemoryManagement();
            
            Debug.Log("RuntimeTerrainManager initialized successfully");
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
        }
        #endregion

        #region プレイヤー追跡
        /// <summary>
        /// プレイヤー追跡を更新
        /// </summary>
        private void UpdatePlayerTracking()
        {
            if (playerTransform == null)
                return;
            
            Vector3 currentPosition = playerTransform.position;
            
            // プレイヤー速度を計算
            playerVelocity = (currentPosition - lastPlayerPosition) / Time.deltaTime;
            
            // 移動予測
            if (predictPlayerMovement)
            {
                predictedPlayerPosition = currentPosition + playerVelocity * predictionTime;
            }
            else
            {
                predictedPlayerPosition = currentPosition;
            }
            
            // 大きな移動があった場合の処理
            float moveDistance = Vector3.Distance(currentPosition, lastPlayerPosition);
            if (moveDistance > playerMoveThreshold)
            {
                OnPlayerMoved(currentPosition, lastPlayerPosition);
            }
            
            lastPlayerPosition = currentPosition;
        }
        
        /// <summary>
        /// プレイヤーが移動した時の処理
        /// </summary>
        private void OnPlayerMoved(Vector3 newPosition, Vector3 oldPosition)
        {
            if (logTileOperations)
            {
                Debug.Log($"Player moved significantly: {Vector3.Distance(newPosition, oldPosition):F1}m");
            }
            
            // 緊急タイル生成をトリガー
            TriggerEmergencyTileGeneration(newPosition);
            
            // 不要なタイルの削除をトリガー
            TriggerTileCleanup(newPosition);
        }
        #endregion

        #region タイル生成更新
        /// <summary>
        /// タイル生成を更新
        /// </summary>
        private void UpdateTileGeneration()
        {
            if (playerTransform == null)
                return;
            
            // 現在のプレイヤータイル座標
            Vector2Int playerTile = tileManager.WorldToTileCoordinate(playerTransform.position);
            Vector2Int predictedTile = tileManager.WorldToTileCoordinate(predictedPlayerPosition);
            
            // 必要なタイルを特定
            IdentifyRequiredTiles(playerTile, predictedTile);
            
            // 不要なタイルを特定
            IdentifyUnneededTiles(playerTile);
            
            // 優先度を更新
            UpdateTilePriorities(playerTile);
        }
        
        /// <summary>
        /// 必要なタイルを特定
        /// </summary>
        private void IdentifyRequiredTiles(Vector2Int playerTile, Vector2Int predictedTile)
        {
            // 即座に読み込むべきタイル
            var immediateTiles = GetTilesInRadius(playerTile, immediateLoadRadius);
            foreach (var tile in immediateTiles)
            {
                RequestTileGeneration(tile, TilePriority.Immediate);
            }
            
            // 事前読み込みタイル
            var preloadTiles = GetTilesInRadius(playerTile, preloadRadius);
            foreach (var tile in preloadTiles)
            {
                if (!immediateTiles.Contains(tile))
                {
                    RequestTileGeneration(tile, TilePriority.High);
                }
            }
            
            // 予測位置周辺のタイル
            if (predictedTile != playerTile)
            {
                var predictedTiles = GetTilesInRadius(predictedTile, immediateLoadRadius);
                foreach (var tile in predictedTiles)
                {
                    RequestTileGeneration(tile, TilePriority.Medium);
                }
            }
        }
        
        /// <summary>
        /// 不要なタイルを特定
        /// </summary>
        private void IdentifyUnneededTiles(Vector2Int playerTile)
        {
            var activeTiles = GetActiveTileCoordinates();
            
            foreach (var tile in activeTiles)
            {
                float distance = Vector2Int.Distance(playerTile, tile);
                
                if (distance > forceUnloadRadius)
                {
                    RequestTileDeletion(tile, TilePriority.Immediate);
                }
                else if (distance > keepAliveRadius)
                {
                    RequestTileDeletion(tile, TilePriority.Low);
                }
            }
        }
        
        /// <summary>
        /// タイル優先度を更新
        /// </summary>
        private void UpdateTilePriorities(Vector2Int playerTile)
        {
            var updatedPriorities = new Dictionary<Vector2Int, TilePriority>();
            
            foreach (var kvp in tilePriorities)
            {
                var tile = kvp.Key;
                float distance = Vector2Int.Distance(playerTile, tile);
                
                TilePriority newPriority = CalculateTilePriority(distance);
                updatedPriorities[tile] = newPriority;
            }
            
            tilePriorities = updatedPriorities;
        }
        #endregion

        #region 動的生成コルーチン
        /// <summary>
        /// 動的生成メインコルーチン
        /// </summary>
        private IEnumerator DynamicGenerationCoroutine()
        {
            while (enableDynamicGeneration)
            {
                yield return new WaitForSeconds(updateInterval);
                
                if (enableFrameTimeControl)
                {
                    yield return StartCoroutine(ProcessGenerationQueueWithFrameLimit());
                }
                else
                {
                    ProcessGenerationQueue();
                }
                
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
            
            while (generationQueue.Count > 0 && processedCount < maxTilesPerUpdate)
            {
                // フレーム時間をチェック
                float elapsedTime = (Time.realtimeSinceStartup - frameStartTime) * 1000f;
                if (elapsedTime > maxFrameTimeMs && processedCount >= minTilesPerUpdate)
                {
                    yield return null; // 次のフレームに延期
                    frameStartTime = Time.realtimeSinceStartup;
                }
                
                var request = generationQueue.Dequeue();
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
            
            while (generationQueue.Count > 0 && processedCount < maxGenerationsPerFrame)
            {
                var request = generationQueue.Dequeue();
                ProcessTileGenerationRequest(request);
                processedCount++;
            }
        }
        
        /// <summary>
        /// 削除キューを処理
        /// </summary>
        private void ProcessDeletionQueue()
        {
            int processedCount = 0;
            
            while (deletionQueue.Count > 0 && processedCount < maxDeletionsPerFrame)
            {
                var tileCoord = deletionQueue.Dequeue();
                ProcessTileDeletion(tileCoord);
                processedCount++;
            }
        }
        #endregion

        #region タイル操作
        /// <summary>
        /// タイル生成をリクエスト
        /// </summary>
        private void RequestTileGeneration(Vector2Int tileCoord, TilePriority priority)
        {
            // 既に存在するか処理中の場合はスキップ
            if (IsTileActive(tileCoord) || processingTiles.Contains(tileCoord))
            {
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
                return;
            }
            
            // 優先度に基づいて削除キューに追加
            if (priority == TilePriority.Immediate)
            {
                // 即座に削除
                ProcessTileDeletion(tileCoord);
            }
            else
            {
                deletionQueue.Enqueue(tileCoord);
            }
            
            if (logTileOperations)
            {
                Debug.Log($"Requested tile deletion: {tileCoord} (Priority: {priority})");
            }
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
            
            try
            {
                // TileManagerを通じてタイルを生成
                var tile = tileManager.GetTileAtWorldPosition(
                    tileManager.TileCoordinateToWorldPosition(request.coordinate));
                
                if (tile == null)
                {
                    // 新しいタイルを生成する必要がある
                    GenerateNewTile(request.coordinate);
                }
                
                performanceStats.totalTilesGenerated++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to process tile generation request for {request.coordinate}: {e.Message}");
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
                    tile.UnloadTile();
                    performanceStats.totalTilesDeleted++;
                    
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
                performanceStats.deletionErrors++;
            }
        }
        
        /// <summary>
        /// 新しいタイルを生成
        /// </summary>
        private void GenerateNewTile(Vector2Int tileCoord)
        {
            // TileManagerの内部メソッドを呼び出すため、
            // ここでは直接的な生成は行わず、TileManagerに委譲
            var worldPos = tileManager.TileCoordinateToWorldPosition(tileCoord);
            
            // プライベートメソッドにアクセスできないため、
            // パブリックAPIを通じて間接的に生成をトリガー
            tileManager.GetHeightAtWorldPosition(worldPos);
        }
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
                TriggerEmergencyCleanup();
            }
            else if (currentMemoryMB > memoryWarningThresholdMB)
            {
                Debug.LogWarning($"Memory usage ({currentMemoryMB:F1}MB) approaching limit ({memoryLimitMB}MB)");
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
            
            // プレイヤーから遠いタイルを強制削除
            var tilesToDelete = activeTiles
                .Where(tile => Vector2Int.Distance(playerTile, tile) > keepAliveRadius)
                .OrderByDescending(tile => Vector2Int.Distance(playerTile, tile))
                .Take(activeTiles.Count / 2) // 半分のタイルを削除
                .ToList();
            
            foreach (var tile in tilesToDelete)
            {
                RequestTileDeletion(tile, TilePriority.Immediate);
            }
            
            performanceStats.emergencyCleanups++;
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
            
            // 最も遠いタイルを削除
            var tilesToDelete = activeTiles
                .Where(tile => Vector2Int.Distance(playerTile, tile) > preloadRadius)
                .OrderByDescending(tile => Vector2Int.Distance(playerTile, tile))
                .Take(3) // 3つのタイルを削除
                .ToList();
            
            foreach (var tile in tilesToDelete)
            {
                RequestTileDeletion(tile, TilePriority.Low);
            }
        }
        
        /// <summary>
        /// 積極的クリーンアップを実行
        /// </summary>
        private void PerformAggressiveCleanup()
        {
            // 使用されていないリソースを強制的にクリーンアップ
            System.GC.Collect();
            Resources.UnloadUnusedAssets();
            
            performanceStats.aggressiveCleanups++;
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
        }
        #endregion

        #region 緊急処理
        /// <summary>
        /// 緊急タイル生成をトリガー
        /// </summary>
        private void TriggerEmergencyTileGeneration(Vector3 playerPosition)
        {
            Vector2Int playerTile = tileManager.WorldToTileCoordinate(playerPosition);
            var immediateTiles = GetTilesInRadius(playerTile, immediateLoadRadius);
            
            foreach (var tile in immediateTiles)
            {
                if (!IsTileActive(tile))
                {
                    RequestTileGeneration(tile, TilePriority.Immediate);
                }
            }
        }
        
        /// <summary>
        /// タイルクリーンアップをトリガー
        /// </summary>
        private void TriggerTileCleanup(Vector3 playerPosition)
        {
            Vector2Int playerTile = tileManager.WorldToTileCoordinate(playerPosition);
            var activeTiles = GetActiveTileCoordinates();
            
            foreach (var tile in activeTiles)
            {
                float distance = Vector2Int.Distance(playerTile, tile);
                if (distance > forceUnloadRadius)
                {
                    RequestTileDeletion(tile, TilePriority.High);
                }
            }
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
                    var tileCoord = new Vector2Int(x, y);
                    float distance = Vector2Int.Distance(center, tileCoord);
                    
                    if (distance <= radius)
                    {
                        tiles.Add(tileCoord);
                    }
                }
            }
            
            return tiles;
        }
        
        /// <summary>
        /// タイルがアクティブかどうかを判定
        /// </summary>
        private bool IsTileActive(Vector2Int tileCoord)
        {
            var worldPos = tileManager.TileCoordinateToWorldPosition(tileCoord);
            return tileManager.GetTileAtWorldPosition(worldPos) != null;
        }
        
        /// <summary>
        /// アクティブなタイル座標を取得
        /// </summary>
        private List<Vector2Int> GetActiveTileCoordinates()
        {
            var coords = new List<Vector2Int>();
            
            // TileManagerから直接取得できないため、推定で実装
            if (playerTransform != null)
            {
                Vector2Int playerTile = tileManager.WorldToTileCoordinate(playerTransform.position);
                
                // 現在のアクティブ範囲を推定
                for (int x = playerTile.x - forceUnloadRadius; x <= playerTile.x + forceUnloadRadius; x++)
                {
                    for (int y = playerTile.y - forceUnloadRadius; y <= playerTile.y + forceUnloadRadius; y++)
                    {
                        var coord = new Vector2Int(x, y);
                        if (IsTileActive(coord))
                        {
                            coords.Add(coord);
                        }
                    }
                }
            }
            
            return coords;
        }
        
        /// <summary>
        /// タイル優先度を計算
        /// </summary>
        private TilePriority CalculateTilePriority(float distance)
        {
            if (distance <= immediateLoadRadius)
                return TilePriority.Immediate;
            else if (distance <= preloadRadius)
                return TilePriority.High;
            else if (distance <= keepAliveRadius)
                return TilePriority.Medium;
            else
                return TilePriority.Low;
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
                ProcessTileGenerationRequest(request);
            }
            else
            {
                generationQueue.Enqueue(request);
            }
        }
        
        /// <summary>
        /// パフォーマンス統計を更新
        /// </summary>
        private void UpdatePerformanceStats()
        {
            performanceStats.frameCount++;
            performanceStats.averageFrameTime = Time.deltaTime;
            
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