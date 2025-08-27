using UnityEngine;
using Vastcore.Player;
using System.Collections.Generic;
using System.Linq;
using Vastcore.Generation;

namespace Vastcore.Generation
{
    /// <summary>
    /// タイル管理システム
    /// 要求6.1: アクティブタイルの辞書管理とタイル座標系変換
    /// </summary>
    public class TileManager : MonoBehaviour
    {
        #region 設定パラメータ
        [Header("タイル設定")]
        public float tileSize = 2000f;                      // タイルサイズ
        public int maxActiveTiles = 25;                     // 最大アクティブタイル数
        public int loadRadius = 3;                          // 読み込み半径（タイル数）
        public int unloadRadius = 5;                        // 削除半径（タイル数）
        
        [Header("地形生成設定")]
        public MeshGenerator.TerrainGenerationParams defaultTerrainParams = MeshGenerator.TerrainGenerationParams.Default();
        public CircularTerrainGenerator.CircularTerrainParams defaultCircularParams = CircularTerrainGenerator.CircularTerrainParams.Default();
        public Material defaultTerrainMaterial;
        
        [Header("最適化設定")]
        public bool enableLOD = true;                       // LOD有効化
        public bool enableMemoryOptimization = true;        // メモリ最適化有効化
        public float memoryLimitMB = 500f;                  // メモリ制限（MB）
        public int maxTilesPerFrame = 2;                    // フレーム毎の最大処理タイル数
        
        [Header("デバッグ設定")]
        public bool showDebugInfo = true;                   // デバッグ情報表示
        public bool showTileBounds = false;                 // タイル境界表示
        public Color tileBoundsColor = Color.yellow;        // タイル境界色
        
        [Header("プレイヤー追跡")]
        public Transform playerTransform;                   // プレイヤーのTransform
        public bool autoFindPlayer = true;                  // プレイヤー自動検索
        #endregion

        #region プライベート変数
        private Dictionary<Vector2Int, TerrainTile> activeTiles = new Dictionary<Vector2Int, TerrainTile>();
        private Dictionary<Vector2Int, TerrainTile> loadingTiles = new Dictionary<Vector2Int, TerrainTile>();
        private Queue<Vector2Int> tilesToLoad = new Queue<Vector2Int>();
        private Queue<Vector2Int> tilesToUnload = new Queue<Vector2Int>();
        
        private Vector2Int currentPlayerTile = Vector2Int.zero;
        private Vector2Int lastPlayerTile = Vector2Int.zero;
        
        private float lastUpdateTime = 0f;
        private const float UPDATE_INTERVAL = 0.1f; // 更新間隔（秒）
        
        // 統計情報
        private int totalTilesGenerated = 0;
        private int totalTilesUnloaded = 0;
        private float totalGenerationTime = 0f;
        private long currentMemoryUsage = 0;
        #endregion

        #region Unity イベント
        void Start()
        {
            InitializeTileManager();
        }
        
        void Update()
        {
            if (Time.time - lastUpdateTime < UPDATE_INTERVAL)
                return;
            
            lastUpdateTime = Time.time;
            
            UpdatePlayerPosition();
            ProcessTileQueue();
            UpdateTileLOD();
            
            if (enableMemoryOptimization)
            {
                CheckMemoryUsage();
            }
        }
        
        void OnDrawGizmos()
        {
            if (!showTileBounds || !Application.isPlaying)
                return;
            
            DrawTileBounds();
        }
        #endregion

        #region 初期化
        /// <summary>
        /// タイルマネージャーを初期化
        /// </summary>
        private void InitializeTileManager()
        {
            Debug.Log("Initializing TileManager...");
            
            // プレイヤーを自動検索
            if (autoFindPlayer && playerTransform == null)
            {
                FindPlayerTransform();
            }
            
            // デフォルト設定を調整
            defaultTerrainParams.size = tileSize;
            defaultCircularParams.radius = tileSize * 0.4f;
            
            // 初期タイルを生成
            if (playerTransform != null)
            {
                currentPlayerTile = WorldToTileCoordinate(playerTransform.position);
                GenerateInitialTiles();
            }
            
            Debug.Log($"TileManager initialized. Tile size: {tileSize}m, Load radius: {loadRadius}, Max tiles: {maxActiveTiles}");
        }
        
        /// <summary>
        /// プレイヤーのTransformを検索
        /// </summary>
        private void FindPlayerTransform()
        {
            // AdvancedPlayerControllerを検索
            var playerController = FindObjectOfType<Vastcore.Player.AdvancedPlayerController>();
            if (playerController != null)
            {
                playerTransform = playerController.transform;
                Debug.Log("Found AdvancedPlayerController");
                return;
            }
            
            // "Player"タグのオブジェクトを検索
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
                Debug.Log("Found Player by tag");
                return;
            }
            
            // メインカメラを使用
            if (Camera.main != null)
            {
                playerTransform = Camera.main.transform;
                Debug.Log("Using Main Camera as player");
                return;
            }
            
            Debug.LogWarning("Could not find player transform");
        }
        
        /// <summary>
        /// 初期タイルを生成
        /// </summary>
        private void GenerateInitialTiles()
        {
            var tilesToGenerate = GetTilesInRadius(currentPlayerTile, loadRadius);
            
            foreach (var tileCoord in tilesToGenerate)
            {
                QueueTileForLoading(tileCoord);
            }
        }
        #endregion

        #region プレイヤー位置管理
        /// <summary>
        /// プレイヤー位置を更新
        /// </summary>
        private void UpdatePlayerPosition()
        {
            if (playerTransform == null)
                return;
            
            currentPlayerTile = WorldToTileCoordinate(playerTransform.position);
            
            if (currentPlayerTile != lastPlayerTile)
            {
                OnPlayerTileChanged();
                lastPlayerTile = currentPlayerTile;
            }
        }
        
        /// <summary>
        /// プレイヤーのタイルが変更された時の処理
        /// </summary>
        private void OnPlayerTileChanged()
        {
            Debug.Log($"Player moved to tile {currentPlayerTile}");
            
            // 新しく読み込むべきタイルを特定
            var tilesToLoad = GetTilesInRadius(currentPlayerTile, loadRadius);
            var tilesToUnload = GetTilesOutsideRadius(currentPlayerTile, unloadRadius);
            
            // 読み込みキューに追加
            foreach (var tileCoord in tilesToLoad)
            {
                if (!activeTiles.ContainsKey(tileCoord) && !loadingTiles.ContainsKey(tileCoord))
                {
                    QueueTileForLoading(tileCoord);
                }
            }
            
            // 削除キューに追加
            foreach (var tileCoord in tilesToUnload)
            {
                if (activeTiles.ContainsKey(tileCoord))
                {
                    QueueTileForUnloading(tileCoord);
                }
            }
        }
        #endregion

        #region タイル座標変換
        /// <summary>
        /// ワールド座標をタイル座標に変換
        /// </summary>
        public Vector2Int WorldToTileCoordinate(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / tileSize),
                Mathf.FloorToInt(worldPosition.z / tileSize)
            );
        }
        
        /// <summary>
        /// タイル座標をワールド座標に変換
        /// </summary>
        public Vector3 TileCoordinateToWorldPosition(Vector2Int tileCoordinate)
        {
            return new Vector3(
                (tileCoordinate.x + 0.5f) * tileSize,
                0f,
                (tileCoordinate.y + 0.5f) * tileSize
            );
        }
        
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
        /// 指定半径外のタイル座標を取得
        /// </summary>
        private List<Vector2Int> GetTilesOutsideRadius(Vector2Int center, int radius)
        {
            var tiles = new List<Vector2Int>();
            
            foreach (var kvp in activeTiles)
            {
                float distance = Vector2Int.Distance(center, kvp.Key);
                if (distance > radius)
                {
                    tiles.Add(kvp.Key);
                }
            }
            
            return tiles;
        }
        #endregion

        #region タイル読み込み・削除
        /// <summary>
        /// タイルを読み込みキューに追加
        /// </summary>
        private void QueueTileForLoading(Vector2Int tileCoordinate)
        {
            if (!tilesToLoad.Contains(tileCoordinate))
            {
                tilesToLoad.Enqueue(tileCoordinate);
            }
        }
        
        /// <summary>
        /// タイルを削除キューに追加
        /// </summary>
        private void QueueTileForUnloading(Vector2Int tileCoordinate)
        {
            if (!tilesToUnload.Contains(tileCoordinate))
            {
                tilesToUnload.Enqueue(tileCoordinate);
            }
        }
        
        /// <summary>
        /// タイルキューを処理
        /// </summary>
        private void ProcessTileQueue()
        {
            int processedCount = 0;
            
            // 削除処理（優先）
            while (tilesToUnload.Count > 0 && processedCount < maxTilesPerFrame)
            {
                var tileCoord = tilesToUnload.Dequeue();
                UnloadTile(tileCoord);
                processedCount++;
            }
            
            // 読み込み処理
            while (tilesToLoad.Count > 0 && processedCount < maxTilesPerFrame && activeTiles.Count < maxActiveTiles)
            {
                var tileCoord = tilesToLoad.Dequeue();
                LoadTile(tileCoord);
                processedCount++;
            }
        }
        
        /// <summary>
        /// タイルを読み込む
        /// </summary>
        private void LoadTile(Vector2Int tileCoordinate)
        {
            if (activeTiles.ContainsKey(tileCoordinate) || loadingTiles.ContainsKey(tileCoordinate))
            {
                return;
            }
            
            // タイル生成パラメータを調整
            var terrainParams = defaultTerrainParams;
            terrainParams.offset = new Vector2(tileCoordinate.x * 123.45f, tileCoordinate.y * 67.89f);
            
            var circularParams = defaultCircularParams;
            circularParams.center = new Vector2(tileCoordinate.x * tileSize, tileCoordinate.y * tileSize);
            
            // TerrainTileを作成
            var tile = new TerrainTile(tileCoordinate, tileSize, terrainParams, circularParams);
            tile.terrainMaterial = defaultTerrainMaterial;
            
            loadingTiles[tileCoordinate] = tile;
            
            // タイルを生成
            float startTime = Time.realtimeSinceStartup;
            tile.GenerateTile(this.transform);
            float generationTime = Time.realtimeSinceStartup - startTime;
            
            // 統計を更新
            totalTilesGenerated++;
            totalGenerationTime += generationTime;
            
            // アクティブタイルに移動
            loadingTiles.Remove(tileCoordinate);
            activeTiles[tileCoordinate] = tile;
            
            Debug.Log($"Loaded tile {tileCoordinate} in {generationTime:F3}s");
        }
        
        /// <summary>
        /// タイルを削除する
        /// </summary>
        private void UnloadTile(Vector2Int tileCoordinate)
        {
            if (!activeTiles.ContainsKey(tileCoordinate))
            {
                return;
            }
            
            var tile = activeTiles[tileCoordinate];
            tile.UnloadTile();
            
            activeTiles.Remove(tileCoordinate);
            totalTilesUnloaded++;
            
            Debug.Log($"Unloaded tile {tileCoordinate}");
        }
        #endregion

        #region LOD管理
        /// <summary>
        /// タイルのLODを更新
        /// </summary>
        private void UpdateTileLOD()
        {
            if (!enableLOD || playerTransform == null)
                return;
            
            foreach (var kvp in activeTiles)
            {
                var tile = kvp.Value;
                float distance = Vector3.Distance(playerTransform.position, tile.worldPosition);
                tile.UpdateLOD(distance);
            }
        }
        #endregion

        #region メモリ管理
        /// <summary>
        /// メモリ使用量をチェック
        /// </summary>
        private void CheckMemoryUsage()
        {
            currentMemoryUsage = CalculateCurrentMemoryUsage();
            long memoryLimitBytes = (long)(memoryLimitMB * 1024 * 1024);
            
            if (currentMemoryUsage > memoryLimitBytes)
            {
                Debug.LogWarning($"Memory usage ({currentMemoryUsage / 1024 / 1024}MB) exceeds limit ({memoryLimitMB}MB)");
                FreeMemory();
            }
        }
        
        /// <summary>
        /// 現在のメモリ使用量を計算
        /// </summary>
        private long CalculateCurrentMemoryUsage()
        {
            long totalUsage = 0;
            
            foreach (var kvp in activeTiles)
            {
                totalUsage += kvp.Value.GetMemoryUsage();
            }
            
            return totalUsage;
        }
        
        /// <summary>
        /// メモリを解放
        /// </summary>
        private void FreeMemory()
        {
            if (playerTransform == null)
                return;
            
            // プレイヤーから最も遠いタイルを削除
            var sortedTiles = activeTiles.Values
                .OrderByDescending(tile => Vector3.Distance(playerTransform.position, tile.worldPosition))
                .ToList();
            
            int tilesToRemove = Mathf.Max(1, activeTiles.Count / 4); // 25%のタイルを削除
            
            for (int i = 0; i < tilesToRemove && i < sortedTiles.Count; i++)
            {
                var tile = sortedTiles[i];
                QueueTileForUnloading(tile.coordinate);
            }
        }
        #endregion

        #region パブリックAPI
        /// <summary>
        /// 指定座標の高度を取得
        /// </summary>
        public float GetHeightAtWorldPosition(Vector3 worldPosition)
        {
            var tileCoord = WorldToTileCoordinate(worldPosition);
            
            if (activeTiles.ContainsKey(tileCoord))
            {
                return activeTiles[tileCoord].GetHeightAtWorldPosition(worldPosition);
            }
            
            return 0f;
        }
        
        /// <summary>
        /// 指定座標のタイルを取得
        /// </summary>
        public TerrainTile GetTileAtWorldPosition(Vector3 worldPosition)
        {
            var tileCoord = WorldToTileCoordinate(worldPosition);
            
            if (activeTiles.ContainsKey(tileCoord))
            {
                return activeTiles[tileCoord];
            }
            
            return null;
        }
        
        /// <summary>
        /// アクティブなタイル数を取得
        /// </summary>
        public int GetActiveTileCount()
        {
            return activeTiles.Count;
        }
        
        /// <summary>
        /// アクティブなタイルのリストを取得
        /// </summary>
        public List<TerrainTile> GetActiveTiles()
        {
            return activeTiles.Values.ToList();
        }
        
        /// <summary>
        /// 読み込み中のタイル数を取得
        /// </summary>
        public int GetLoadingTileCount()
        {
            return loadingTiles.Count;
        }
        
        /// <summary>
        /// 統計情報を取得
        /// </summary>
        public TileManagerStats GetStats()
        {
            return new TileManagerStats
            {
                activeTileCount = activeTiles.Count,
                loadingTileCount = loadingTiles.Count,
                totalTilesGenerated = totalTilesGenerated,
                totalTilesUnloaded = totalTilesUnloaded,
                averageGenerationTime = totalTilesGenerated > 0 ? totalGenerationTime / totalTilesGenerated : 0f,
                currentMemoryUsageMB = currentMemoryUsage / 1024f / 1024f,
                currentPlayerTile = currentPlayerTile
            };
        }
        
        /// <summary>
        /// 全タイルを強制削除
        /// </summary>
        public void UnloadAllTiles()
        {
            Debug.Log("Unloading all tiles...");
            
            var tilesToUnload = activeTiles.Keys.ToList();
            foreach (var tileCoord in tilesToUnload)
            {
                UnloadTile(tileCoord);
            }
            
            // キューもクリア
            tilesToLoad.Clear();
            tilesToUnload.Clear();
            loadingTiles.Clear();
        }
        
        /// <summary>
        /// 設定を更新
        /// </summary>
        public void UpdateSettings(float newTileSize, int newLoadRadius, int newMaxTiles)
        {
            bool needsReload = (newTileSize != tileSize);
            
            tileSize = newTileSize;
            loadRadius = newLoadRadius;
            maxActiveTiles = newMaxTiles;
            
            defaultTerrainParams.size = tileSize;
            defaultCircularParams.radius = tileSize * 0.4f;
            
            if (needsReload)
            {
                UnloadAllTiles();
                if (playerTransform != null)
                {
                    currentPlayerTile = WorldToTileCoordinate(playerTransform.position);
                    GenerateInitialTiles();
                }
            }
        }
        #endregion

        #region デバッグ機能
        /// <summary>
        /// タイル境界を描画
        /// </summary>
        private void DrawTileBounds()
        {
            Gizmos.color = tileBoundsColor;
            
            foreach (var kvp in activeTiles)
            {
                var tile = kvp.Value;
                Gizmos.DrawWireCube(tile.worldPosition, new Vector3(tileSize, 0f, tileSize));
            }
            
            // プレイヤーの現在タイルをハイライト
            if (playerTransform != null)
            {
                Gizmos.color = Color.red;
                var playerTilePos = TileCoordinateToWorldPosition(currentPlayerTile);
                Gizmos.DrawWireCube(playerTilePos, new Vector3(tileSize, 10f, tileSize));
            }
        }
        
        /// <summary>
        /// デバッグ情報をログ出力
        /// </summary>
        [ContextMenu("Log Debug Info")]
        public void LogDebugInfo()
        {
            var stats = GetStats();
            Debug.Log($"=== TileManager Debug Info ===");
            Debug.Log($"Active Tiles: {stats.activeTileCount}");
            Debug.Log($"Loading Tiles: {stats.loadingTileCount}");
            Debug.Log($"Total Generated: {stats.totalTilesGenerated}");
            Debug.Log($"Total Unloaded: {stats.totalTilesUnloaded}");
            Debug.Log($"Average Generation Time: {stats.averageGenerationTime:F3}s");
            Debug.Log($"Memory Usage: {stats.currentMemoryUsageMB:F1}MB");
            Debug.Log($"Player Tile: {stats.currentPlayerTile}");
            Debug.Log($"==============================");
        }
        #endregion

        #region データ構造
        /// <summary>
        /// タイルマネージャーの統計情報
        /// </summary>
        [System.Serializable]
        public struct TileManagerStats
        {
            public int activeTileCount;
            public int loadingTileCount;
            public int totalTilesGenerated;
            public int totalTilesUnloaded;
            public float averageGenerationTime;
            public float currentMemoryUsageMB;
            public Vector2Int currentPlayerTile;
        }
        #endregion
    }
}