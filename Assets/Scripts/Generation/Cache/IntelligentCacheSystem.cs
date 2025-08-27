using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Vastcore.Generation.Cache
{
    /// <summary>
    /// インテリジェントキャッシュシステム
    /// 生成済み地形・オブジェクトの効率的なキャッシュと予測的プリロード
    /// </summary>
    public class IntelligentCacheSystem : MonoBehaviour
    {
        [Header("キャッシュ設定")]
        [SerializeField] private bool enableCaching = true;
        [SerializeField] private bool enablePersistentCache = true;
        [SerializeField] private bool enablePredictivePreload = true;
        [SerializeField] private string cacheDirectory = "TerrainCache";
        
        [Header("メモリ管理")]
        [SerializeField] private int maxMemoryCacheSize = 100; // MB
        [SerializeField] private int maxCachedTiles = 50;
        [SerializeField] private float cacheEvictionThreshold = 0.8f;
        
        [Header("予測プリロード")]
        [SerializeField] private float preloadRadius = 1500f;
        [SerializeField] private int maxPreloadTasks = 3;
        [SerializeField] private float playerVelocityPredictionTime = 5f;
        
        // キャッシュデータ構造
        private Dictionary<Vector2Int, CachedTerrainData> memoryCache;
        private Dictionary<Vector2Int, string> diskCacheIndex;
        private Queue<Vector2Int> accessOrder;
        private HashSet<Vector2Int> preloadingTiles;
        
        // プレイヤー追跡
        private Transform playerTransform;
        private Vector3 lastPlayerPosition;
        private Vector3 playerVelocity;
        private List<Vector3> playerPositionHistory;
        
        // 統計情報
        private CacheStatistics statistics;
        
        [System.Serializable]
        public struct CachedTerrainData
        {
            public Vector2Int coordinate;
            public float[,] heightmap;
            public TerrainMetadata metadata;
            public List<PrimitiveObjectData> primitiveObjects;
            public float lastAccessTime;
            public int accessCount;
            public long memorySize;
        }
        
        [System.Serializable]
        public struct TerrainMetadata
        {
            public float generationTime;
            public int seed;
            public string biomeType;
            public Vector4 noiseParams;
            public bool hasErosion;
        }
        
        [System.Serializable]
        public struct PrimitiveObjectData
        {
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 scale;
            public string primitiveType;
            public string materialName;
        }
        
        public struct CacheStatistics
        {
            public int totalCacheHits;
            public int totalCacheMisses;
            public int memoryEvictions;
            public int diskWrites;
            public int diskReads;
            public float hitRatio;
            public long totalMemoryUsed;
            public int preloadedTiles;
        }
        
        private void Awake()
        {
            InitializeCache();
            playerPositionHistory = new List<Vector3>();
        }
        
        private void InitializeCache()
        {
            memoryCache = new Dictionary<Vector2Int, CachedTerrainData>();
            diskCacheIndex = new Dictionary<Vector2Int, string>();
            accessOrder = new Queue<Vector2Int>();
            preloadingTiles = new HashSet<Vector2Int>();
            
            statistics = new CacheStatistics();
            
            // プレイヤーの検索
            var player = FindObjectOfType<AdvancedPlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
            
            // ディスクキャッシュディレクトリの作成
            if (enablePersistentCache)
            {
                string cachePath = Path.Combine(Application.persistentDataPath, cacheDirectory);
                if (!Directory.Exists(cachePath))
                {
                    Directory.CreateDirectory(cachePath);
                }
                
                LoadDiskCacheIndex();
            }
            
            Debug.Log("Intelligent Cache System initialized");
        }
        
        /// <summary>
        /// 地形データをキャッシュに保存
        /// </summary>
        public void CacheTerrainData(Vector2Int coordinate, float[,] heightmap, TerrainMetadata metadata, List<PrimitiveObjectData> primitives = null)
        {
            if (!enableCaching) return;
            
            var cachedData = new CachedTerrainData
            {
                coordinate = coordinate,
                heightmap = heightmap,
                metadata = metadata,
                primitiveObjects = primitives ?? new List<PrimitiveObjectData>(),
                lastAccessTime = Time.time,
                accessCount = 1,
                memorySize = CalculateMemorySize(heightmap, primitives)
            };
            
            // メモリキャッシュに追加
            if (memoryCache.ContainsKey(coordinate))
            {
                memoryCache[coordinate] = cachedData;
            }
            else
            {
                memoryCache.Add(coordinate, cachedData);
                accessOrder.Enqueue(coordinate);
            }
            
            statistics.totalMemoryUsed += cachedData.memorySize;
            
            // メモリ制限チェック
            if (ShouldEvictMemoryCache())
            {
                EvictLeastRecentlyUsed();
            }
            
            // ディスクキャッシュに非同期保存
            if (enablePersistentCache)
            {
                StartCoroutine(SaveToDiskAsync(coordinate, cachedData));
            }
        }
        
        /// <summary>
        /// キャッシュから地形データを取得
        /// </summary>
        public bool TryGetCachedTerrainData(Vector2Int coordinate, out CachedTerrainData cachedData)
        {
            cachedData = default;
            
            if (!enableCaching) return false;
            
            // メモリキャッシュから検索
            if (memoryCache.TryGetValue(coordinate, out cachedData))
            {
                // アクセス情報更新
                cachedData.lastAccessTime = Time.time;
                cachedData.accessCount++;
                memoryCache[coordinate] = cachedData;
                
                statistics.totalCacheHits++;
                return true;
            }
            
            // ディスクキャッシュから検索
            if (enablePersistentCache && diskCacheIndex.ContainsKey(coordinate))
            {
                StartCoroutine(LoadFromDiskAsync(coordinate, (loadedData) =>
                {
                    if (loadedData.HasValue)
                    {
                        cachedData = loadedData.Value;
                        // メモリキャッシュに昇格
                        CacheTerrainData(coordinate, cachedData.heightmap, cachedData.metadata, cachedData.primitiveObjects);
                        statistics.totalCacheHits++;
                    }
                }));
                
                return false; // 非同期読み込みのため即座にはfalse
            }
            
            statistics.totalCacheMisses++;
            return false;
        }
        
        /// <summary>
        /// 予測的プリロードの実行
        /// </summary>
        public void UpdatePredictivePreload()
        {
            if (!enablePredictivePreload || playerTransform == null) return;
            
            UpdatePlayerTracking();
            
            var predictedPositions = PredictPlayerMovement();
            var tilesToPreload = GetTilesToPreload(predictedPositions);
            
            foreach (var tileCoord in tilesToPreload)
            {
                if (!preloadingTiles.Contains(tileCoord) && preloadingTiles.Count < maxPreloadTasks)
                {
                    StartCoroutine(PreloadTileAsync(tileCoord));
                }
            }
        }
        
        private void UpdatePlayerTracking()
        {
            Vector3 currentPosition = playerTransform.position;
            
            // 速度計算
            if (lastPlayerPosition != Vector3.zero)
            {
                playerVelocity = (currentPosition - lastPlayerPosition) / Time.deltaTime;
            }
            
            // 位置履歴の更新
            playerPositionHistory.Add(currentPosition);
            if (playerPositionHistory.Count > 10)
            {
                playerPositionHistory.RemoveAt(0);
            }
            
            lastPlayerPosition = currentPosition;
        }
        
        private List<Vector3> PredictPlayerMovement()
        {
            var predictions = new List<Vector3>();
            
            if (playerVelocity.magnitude < 0.1f) return predictions;
            
            // 現在の速度に基づく予測
            Vector3 currentPos = playerTransform.position;
            Vector3 predictedPos = currentPos + playerVelocity * playerVelocityPredictionTime;
            predictions.Add(predictedPos);
            
            // 移動パターンの分析
            if (playerPositionHistory.Count >= 3)
            {
                Vector3 trend = AnalyzeMovementTrend();
                Vector3 trendPrediction = currentPos + trend * playerVelocityPredictionTime;
                predictions.Add(trendPrediction);
            }
            
            return predictions;
        }
        
        private Vector3 AnalyzeMovementTrend()
        {
            if (playerPositionHistory.Count < 3) return Vector3.zero;
            
            Vector3 totalTrend = Vector3.zero;
            int count = 0;
            
            for (int i = 1; i < playerPositionHistory.Count; i++)
            {
                Vector3 direction = playerPositionHistory[i] - playerPositionHistory[i - 1];
                totalTrend += direction;
                count++;
            }
            
            return count > 0 ? totalTrend / count : Vector3.zero;
        }
        
        private List<Vector2Int> GetTilesToPreload(List<Vector3> predictedPositions)
        {
            var tilesToPreload = new List<Vector2Int>();
            
            foreach (var position in predictedPositions)
            {
                var tileCoord = WorldToTileCoordinate(position);
                
                // 周辺タイルも含める
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        var neighborCoord = new Vector2Int(tileCoord.x + x, tileCoord.y + y);
                        
                        if (!memoryCache.ContainsKey(neighborCoord) && 
                            !preloadingTiles.Contains(neighborCoord))
                        {
                            tilesToPreload.Add(neighborCoord);
                        }
                    }
                }
            }
            
            return tilesToPreload;
        }
        
        private IEnumerator PreloadTileAsync(Vector2Int coordinate)
        {
            preloadingTiles.Add(coordinate);
            
            // ディスクキャッシュから読み込み試行
            bool foundInDisk = false;
            if (enablePersistentCache && diskCacheIndex.ContainsKey(coordinate))
            {
                yield return StartCoroutine(LoadFromDiskAsync(coordinate, (loadedData) =>
                {
                    if (loadedData.HasValue)
                    {
                        var data = loadedData.Value;
                        CacheTerrainData(coordinate, data.heightmap, data.metadata, data.primitiveObjects);
                        foundInDisk = true;
                        statistics.preloadedTiles++;
                    }
                }));
            }
            
            // ディスクにない場合は新規生成をリクエスト
            if (!foundInDisk)
            {
                var terrainGenerator = FindObjectOfType<RuntimeTerrainManager>();
                if (terrainGenerator != null)
                {
                    // 非同期生成リクエスト（実装は RuntimeTerrainManager に依存）
                    Debug.Log($"Requesting preload generation for tile {coordinate}");
                }
            }
            
            preloadingTiles.Remove(coordinate);
        }
        
        private Vector2Int WorldToTileCoordinate(Vector3 worldPosition)
        {
            const float tileSize = 2000f; // RuntimeTerrainManager の tileSize と同期
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / tileSize),
                Mathf.FloorToInt(worldPosition.z / tileSize)
            );
        }
        
        private bool ShouldEvictMemoryCache()
        {
            long maxMemoryBytes = maxMemoryCacheSize * 1024 * 1024;
            return statistics.totalMemoryUsed > maxMemoryBytes * cacheEvictionThreshold ||
                   memoryCache.Count > maxCachedTiles;
        }
        
        private void EvictLeastRecentlyUsed()
        {
            if (accessOrder.Count == 0) return;
            
            var evictCoordinate = accessOrder.Dequeue();
            
            if (memoryCache.TryGetValue(evictCoordinate, out var evictData))
            {
                statistics.totalMemoryUsed -= evictData.memorySize;
                memoryCache.Remove(evictCoordinate);
                statistics.memoryEvictions++;
                
                Debug.Log($"Evicted tile {evictCoordinate} from memory cache");
            }
        }
        
        private long CalculateMemorySize(float[,] heightmap, List<PrimitiveObjectData> primitives)
        {
            long size = 0;
            
            if (heightmap != null)
            {
                size += heightmap.Length * sizeof(float);
            }
            
            if (primitives != null)
            {
                size += primitives.Count * 100; // 概算
            }
            
            return size;
        }
        
        private IEnumerator SaveToDiskAsync(Vector2Int coordinate, CachedTerrainData data)
        {
            yield return null; // フレーム分散
            
            try
            {
                string fileName = $"terrain_{coordinate.x}_{coordinate.y}.cache";
                string filePath = Path.Combine(Application.persistentDataPath, cacheDirectory, fileName);
                
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(fileStream, data);
                }
                
                diskCacheIndex[coordinate] = fileName;
                statistics.diskWrites++;
                
                Debug.Log($"Saved tile {coordinate} to disk cache");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save tile {coordinate} to disk: {e.Message}");
            }
        }
        
        private IEnumerator LoadFromDiskAsync(Vector2Int coordinate, System.Action<CachedTerrainData?> onComplete)
        {
            yield return null; // フレーム分散
            
            try
            {
                if (!diskCacheIndex.TryGetValue(coordinate, out string fileName))
                {
                    onComplete(null);
                    yield break;
                }
                
                string filePath = Path.Combine(Application.persistentDataPath, cacheDirectory, fileName);
                
                if (!File.Exists(filePath))
                {
                    onComplete(null);
                    yield break;
                }
                
                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    var formatter = new BinaryFormatter();
                    var data = (CachedTerrainData)formatter.Deserialize(fileStream);
                    
                    statistics.diskReads++;
                    onComplete(data);
                }
                
                Debug.Log($"Loaded tile {coordinate} from disk cache");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load tile {coordinate} from disk: {e.Message}");
                onComplete(null);
            }
        }
        
        private void LoadDiskCacheIndex()
        {
            try
            {
                string cachePath = Path.Combine(Application.persistentDataPath, cacheDirectory);
                var files = Directory.GetFiles(cachePath, "*.cache");
                
                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);
                    if (TryParseCoordinateFromFileName(fileName, out Vector2Int coordinate))
                    {
                        diskCacheIndex[coordinate] = fileName;
                    }
                }
                
                Debug.Log($"Loaded {diskCacheIndex.Count} entries from disk cache index");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load disk cache index: {e.Message}");
            }
        }
        
        private bool TryParseCoordinateFromFileName(string fileName, out Vector2Int coordinate)
        {
            coordinate = Vector2Int.zero;
            
            if (!fileName.StartsWith("terrain_") || !fileName.EndsWith(".cache"))
                return false;
            
            string coords = fileName.Substring(8, fileName.Length - 14); // "terrain_" と ".cache" を除去
            string[] parts = coords.Split('_');
            
            if (parts.Length == 2 && 
                int.TryParse(parts[0], out int x) && 
                int.TryParse(parts[1], out int y))
            {
                coordinate = new Vector2Int(x, y);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// キャッシュ統計情報の取得
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            statistics.hitRatio = statistics.totalCacheHits + statistics.totalCacheMisses > 0 
                ? (float)statistics.totalCacheHits / (statistics.totalCacheHits + statistics.totalCacheMisses)
                : 0f;
            
            return statistics;
        }
        
        /// <summary>
        /// キャッシュのクリア
        /// </summary>
        public void ClearCache(bool includeDisk = false)
        {
            memoryCache.Clear();
            accessOrder.Clear();
            preloadingTiles.Clear();
            statistics.totalMemoryUsed = 0;
            
            if (includeDisk)
            {
                diskCacheIndex.Clear();
                
                try
                {
                    string cachePath = Path.Combine(Application.persistentDataPath, cacheDirectory);
                    if (Directory.Exists(cachePath))
                    {
                        Directory.Delete(cachePath, true);
                        Directory.CreateDirectory(cachePath);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to clear disk cache: {e.Message}");
                }
            }
            
            Debug.Log("Cache cleared");
        }
        
        private void Update()
        {
            if (enablePredictivePreload)
            {
                UpdatePredictivePreload();
            }
        }
        
        private void OnDestroy()
        {
            // 未完了の非同期処理をクリーンアップ
            StopAllCoroutines();
        }
    }
}