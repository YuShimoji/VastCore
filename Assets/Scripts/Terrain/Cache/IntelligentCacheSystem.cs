using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;


namespace Vastcore.Generation.Cache
{
    /// <summary>
    /// 繧､繝ｳ繝・Μ繧ｸ繧ｧ繝ｳ繝医く繝｣繝・す繝･繧ｷ繧ｹ繝・Β
    /// 逕滓・貂医∩蝨ｰ蠖｢繝ｻ繧ｪ繝悶ず繧ｧ繧ｯ繝医・蜉ｹ邇・噪縺ｪ繧ｭ繝｣繝・す繝･縺ｨ莠域ｸｬ逧・・繝ｪ繝ｭ繝ｼ繝・
    /// </summary>
    public class IntelligentCacheSystem : MonoBehaviour
    {
        #region Cache Settings
        [Header("Cache Settings")]
        [SerializeField] private bool enableCaching = true;
        [SerializeField] private bool enablePersistentCache = true;
        [SerializeField] private bool enablePredictivePreload = true;
        [SerializeField] private string cacheDirectory = "TerrainCache";
        #endregion

        #region Memory Management
        [Header("Memory Management")]
        [SerializeField] private int maxMemoryCacheSize = 100; // MB
        [SerializeField] private int maxCachedTiles = 50;
        [SerializeField] private float cacheEvictionThreshold = 0.8f;
        #endregion

        #region Predictive Preload
        [Header("Predictive Preload")]
        [SerializeField] private float preloadRadius = 1500f;
        [SerializeField] private int maxPreloadTasks = 3;
        [SerializeField] private float playerVelocityPredictionTime = 5f;
        #endregion

        // 繧ｭ繝｣繝・す繝･繝・・繧ｿ讒矩�
        private Dictionary<Vector2Int, CachedTerrainData> memoryCache;
        private Dictionary<Vector2Int, string> diskCacheIndex;
        private Queue<Vector2Int> accessOrder;
        private HashSet<Vector2Int> preloadingTiles;
        
        // 繝励Ξ繧､繝､繝ｼ霑ｽ霍｡
        private Transform playerTransform;
        private Vector3 lastPlayerPosition;
        private Vector3 playerVelocity;
        private List<Vector3> playerPositionHistory;
        
        // 邨ｱ險域ュ蝣ｱ
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
            
            // 繝励Ξ繧､繝､繝ｼ縺ｮ讀懃ｴ｢
            var player = FindFirstObjectByType<AdvancedPlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
            
            // 繝・ぅ繧ｹ繧ｯ繧ｭ繝｣繝・す繝･繝・ぅ繝ｬ繧ｯ繝医Μ縺ｮ菴懈・
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
        /// 蝨ｰ蠖｢繝・・繧ｿ繧偵く繝｣繝・す繝･縺ｫ菫晏ｭ・
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
            
            // 繝｡繝｢繝ｪ繧ｭ繝｣繝・す繝･縺ｫ霑ｽ蜉�
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
            
            // 繝｡繝｢繝ｪ蛻ｶ髯舌メ繧ｧ繝・け
            if (ShouldEvictMemoryCache())
            {
                EvictLeastRecentlyUsed();
            }
            
            // 繝・ぅ繧ｹ繧ｯ繧ｭ繝｣繝・す繝･縺ｫ髱槫酔譛滉ｿ晏ｭ・
            if (enablePersistentCache)
            {
                StartCoroutine(SaveToDiskAsync(coordinate, cachedData));
            }
        }
        
        /// <summary>
        /// 繧ｭ繝｣繝・す繝･縺九ｉ蝨ｰ蠖｢繝・・繧ｿ繧貞叙蠕・
        /// </summary>
        public bool TryGetCachedTerrainData(Vector2Int coordinate, out CachedTerrainData cachedData)
        {
            cachedData = default;
            
            if (!enableCaching) return false;
            
            // 繝｡繝｢繝ｪ繧ｭ繝｣繝・す繝･縺九ｉ讀懃ｴ｢
            if (memoryCache.TryGetValue(coordinate, out cachedData))
            {
                // 繧｢繧ｯ繧ｻ繧ｹ諠・�ｱ譖ｴ譁ｰ
                cachedData.lastAccessTime = Time.time;
                cachedData.accessCount++;
                memoryCache[coordinate] = cachedData;
                
                statistics.totalCacheHits++;
                return true;
            }
            
            // 繝・ぅ繧ｹ繧ｯ繧ｭ繝｣繝・す繝･縺九ｉ讀懃ｴ｢
            if (enablePersistentCache && diskCacheIndex.ContainsKey(coordinate))
            {
                // 蜷梧悄逧・↓繝・ぅ繧ｹ繧ｯ縺九ｉ隱ｭ縺ｿ霎ｼ縺ｿ
                var loadedData = LoadFromDiskSync(coordinate);
                if (loadedData.HasValue)
                {
                    cachedData = loadedData.Value;
                    // 繝｡繝｢繝ｪ繧ｭ繝｣繝・す繝･縺ｫ譏・�ｼ
                    CacheTerrainData(coordinate, cachedData.heightmap, cachedData.metadata, cachedData.primitiveObjects);
                    statistics.totalCacheHits++;
                    return true;
                }
            }
            
            statistics.totalCacheMisses++;
            return false;
        }
        
        /// <summary>
        /// 莠域ｸｬ逧・・繝ｪ繝ｭ繝ｼ繝峨・螳溯｡・
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
            
            // 騾溷ｺｦ險育ｮ・
            if (lastPlayerPosition != Vector3.zero)
            {
                playerVelocity = (currentPosition - lastPlayerPosition) / Time.deltaTime;
            }
            
            // 菴咲ｽｮ螻･豁ｴ縺ｮ譖ｴ譁ｰ
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
            
            // 迴ｾ蝨ｨ縺ｮ騾溷ｺｦ縺ｫ蝓ｺ縺･縺丈ｺ域ｸｬ
            Vector3 currentPos = playerTransform.position;
            Vector3 predictedPos = currentPos + playerVelocity * playerVelocityPredictionTime;
            predictions.Add(predictedPos);
            
            // 遘ｻ蜍輔ヱ繧ｿ繝ｼ繝ｳ縺ｮ蛻・梵
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
                
                // preloadRadius縺ｫ蝓ｺ縺･縺・※蜻ｨ霎ｺ繧ｿ繧､繝ｫ繧ょ性繧√ｋ
                int radius = Mathf.CeilToInt(preloadRadius / 2000f); // tileSize = 2000f
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
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
            
            // 繝・ぅ繧ｹ繧ｯ繧ｭ繝｣繝・す繝･縺九ｉ隱ｭ縺ｿ霎ｼ縺ｿ隧ｦ陦・
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
            
            // 繝・ぅ繧ｹ繧ｯ縺ｫ縺ｪ縺・�ｴ蜷医・譁ｰ隕冗函謌舌ｒ繝ｪ繧ｯ繧ｨ繧ｹ繝・
            if (!foundInDisk)
            {
                var terrainGenerator = FindFirstObjectByType<RuntimeTerrainManager>();
                if (terrainGenerator != null)
                {
                    // 髱槫酔譛溽函謌舌Μ繧ｯ繧ｨ繧ｹ繝茨ｼ亥ｮ溯｣・・ RuntimeTerrainManager 縺ｫ萓晏ｭ假ｼ・
                    Debug.Log($"Requesting preload generation for tile {coordinate}");
                }
            }
            
            preloadingTiles.Remove(coordinate);
        }
        
        private Vector2Int WorldToTileCoordinate(Vector3 worldPosition)
        {
            const float tileSize = 2000f; // RuntimeTerrainManager 縺ｮ tileSize 縺ｨ蜷梧悄
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
                size += primitives.Count * 100; // 讎らｮ・
            }
            
            return size;
        }
        
        private IEnumerator SaveToDiskAsync(Vector2Int coordinate, CachedTerrainData data)
        {
            yield return null; // 繝輔Ξ繝ｼ繝�蛻・淵
            
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
            yield return null; // 繝輔Ξ繝ｼ繝�蛻・淵
            
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
        
        private CachedTerrainData? LoadFromDiskSync(Vector2Int coordinate)
        {
            try
            {
                if (!diskCacheIndex.TryGetValue(coordinate, out string fileName))
                {
                    return null;
                }
                
                string filePath = Path.Combine(Application.persistentDataPath, cacheDirectory, fileName);
                
                if (!File.Exists(filePath))
                {
                    return null;
                }
                
                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    var formatter = new BinaryFormatter();
                    var data = (CachedTerrainData)formatter.Deserialize(fileStream);
                    
                    statistics.diskReads++;
                    return data;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load tile {coordinate} from disk: {e.Message}");
                return null;
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
            
            string coords = fileName.Substring(8, fileName.Length - 14); // "terrain_" 縺ｨ ".cache" 繧帝勁蜴ｻ
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
        /// 繧ｭ繝｣繝・す繝･邨ｱ險域ュ蝣ｱ縺ｮ蜿門ｾ・
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            statistics.hitRatio = statistics.totalCacheHits + statistics.totalCacheMisses > 0 
                ? (float)statistics.totalCacheHits / (statistics.totalCacheHits + statistics.totalCacheMisses)
                : 0f;
            
            return statistics;
        }
        
        /// <summary>
        /// 繧ｭ繝｣繝・す繝･縺ｮ繧ｯ繝ｪ繧｢
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
            // 譛ｪ螳御ｺ・・髱槫酔譛溷・逅・ｒ繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・
            StopAllCoroutines();
        }
    }
}
