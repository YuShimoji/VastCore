using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

using Vastcore.Utilities;
using Vastcore.Generation;
using Vastcore.Core;

namespace Vastcore.Generation
{
    /// <summary>
    /// 螳溯｡梧凾蝨ｰ蠖｢邂｡逅・す繧ｹ繝・Β
    /// 繝励Ξ繧､繝､繝ｼ菴咲ｽｮ縺ｫ蝓ｺ縺･縺上ち繧､繝ｫ縺ｮ蜍慕噪繝ｭ繝ｼ繝・繧｢繝ｳ繝ｭ繝ｼ繝峨√Γ繝｢繝ｪ逶｣隕悶∫ｰ｡譏鍋ｵｱ險医ｒ陦後≧縲・
    /// VastcoreLogger 繧堤畑縺・◆霆ｽ驥上ヨ繝ｬ繝ｼ繧ｹ繝ｭ繧ｰ繧貞・蜉帙☆繧九・
    /// </summary>
    public class RuntimeTerrainManager : MonoBehaviour
    {
        #region 險ｭ螳・蜿ら・
        [Header("蜍慕噪逕滓・險ｭ螳・)]
        public bool enableDynamicGeneration = true;
        public bool enableFrameTimeControl = true;
        public int maxGenerationsPerFrame = 4;
        public int maxDeletionsPerFrame = 6;
        public int maxTilesPerUpdate = 8;   // 繝輔Ξ繝ｼ繝蛻ｶ蠕｡譎ゅ・荳企剞
        public int minTilesPerUpdate = 1;   // 繝輔Ξ繝ｼ繝蛻ｶ蠕｡譎ゅ・譛菴主・逅・焚
        public float maxFrameTimeMs = 4f;   // 1繝輔Ξ繝ｼ繝縺ｧ險ｱ螳ｹ縺吶ｋ蜃ｦ逅・凾髢・ms)
        public float updateInterval = 0.1f; // 蜍慕噪逕滓・縺ｮ譖ｴ譁ｰ髢馴囈

        [Header("蜊雁ｾ・ｨｭ螳・繧ｿ繧､繝ｫ蜊倅ｽ・")]
        public int immediateLoadRadius = 1;
        public int preloadRadius = 3;
        public int keepAliveRadius = 5;
        public int forceUnloadRadius = 7;

        [Header("繝｡繝｢繝ｪ邂｡逅・)]
        public float memoryLimitMB = 1024f;
        public float memoryWarningThresholdMB = 768f; // 隴ｦ蜻翫＠縺阪＞蛟､
        public float cleanupInterval = 2f;
        public bool enableAggressiveCleanup = false;

        [Header("繝・ヰ繝・げ")]
        public bool showDebugInfo = false;
        public bool logTileOperations = false;
        public bool predictPlayerMovement = true;

        [Header("蜿ら・")]
        public Transform playerTransform;
        private TileManager tileManager;
        #endregion

        #region 蜀・Κ迥ｶ諷・
        // 繧ｭ繝･繝ｼ縺ｨ迥ｶ諷・
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
        // 荳蠎ｦ縺ｮ譖ｴ譁ｰ繧ｵ繧､繧ｯ繝ｫ蜀・〒驥阪＞蜈ｨ蜑企勁繧貞､夐㍾螳溯｡後＠縺ｪ縺・◆繧√・繝輔Λ繧ｰ
        private bool didFullUnloadThisCycle = false;
        #endregion

        void Update()
        {
            // 譖ｴ譁ｰ髢馴囈繝√ぉ繝・け
            if (Time.time - lastUpdateTime >= 0.1f)
            {
                Debug.Log($"Update called after {Time.time - lastUpdateTime:F3} seconds");
                lastUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// 繝ｩ繝ｳ繧ｿ繧､繝繝槭ロ繝ｼ繧ｸ繝｣繝ｼ繧貞・譛溷喧
        /// </summary>
        private void InitializeRuntimeManager()
        {
            Debug.Log("Initializing RuntimeTerrainManager...");
            VastcoreLogger.Instance.LogInfo("RuntimeTerrain", $"Init start dyn={(enableDynamicGeneration?1:0)} upd={updateInterval}s maxGenPerFrame={maxGenerationsPerFrame} maxDelPerFrame={maxDeletionsPerFrame}");

            // TileManager繧貞叙蠕励∪縺溘・菴懈・
            tileManager = GetComponent<TileManager>();
            if (tileManager == null)
            {
                tileManager = gameObject.AddComponent<TileManager>();
                VastcoreLogger.Instance.LogInfo("RuntimeTerrain", "TileManager component added by RuntimeTerrainManager");
            }

            // TileManager縺ｫ繝励Ξ繧､繝､繝ｼ蜿ら・繧帝｣謳ｺ
            if (tileManager.playerTransform == null && playerTransform != null)
            {
                tileManager.playerTransform = playerTransform;
            }

            if (playerTransform != null)
            {
                lastPlayerPosition = playerTransform.position;
            }

            // 繧ｳ繝ｫ繝ｼ繝√Φ繧帝幕蟋・
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
        /// 蜍慕噪逕滓・繧ｳ繝ｫ繝ｼ繝√Φ繧帝幕蟋・
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
        /// 繝｡繝｢繝ｪ邂｡逅・さ繝ｫ繝ｼ繝√Φ繧帝幕蟋・
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

        #region 繝励Ξ繧､繝､繝ｼ霑ｽ霍｡
        /// <summary>
        /// 繝励Ξ繧､繝､繝ｼ霑ｽ霍｡繧呈峩譁ｰ
        /// </summary>
        private void UpdatePlayerTracking()
        {
            if (playerTransform == null)
                return;

            // TileManager 縺ｫ繝励Ξ繧､繝､繝ｼ蜿ら・繧貞渚譏
            if (tileManager != null && tileManager.playerTransform != playerTransform)
            {
                tileManager.playerTransform = playerTransform;
            }

            // 莠域ｸｬ・育ｰ｡譏・ 逶ｴ霑鷹溷ｺｦ繝吶・繧ｹ・・
            if (predictPlayerMovement)
            {
                Vector3 velocity = (playerTransform.position - lastPlayerPosition) / Mathf.Max(Time.deltaTime, 1e-4f);
                predictedPlayerPosition = playerTransform.position + velocity * 0.25f; // 250ms 蜈医ｒ謗ｨ螳・
            }

            // 繝｡繝｢: TileManager 蛛ｴ縺ｮ Update 縺瑚ｪｭ霎ｼ/蜑企勁繧貞ｮ滓命縺吶ｋ縺溘ａ縲√％縺薙〒縺ｯ隕∵ｱよｺ門ｙ縺ｮ縺ｿ
            lastPlayerPosition = playerTransform.position;
        }

        #region 蜍慕噪逕滓・繧ｳ繝ｫ繝ｼ繝√Φ
        /// <summary>
        /// 蜍慕噪逕滓・繝｡繧､繝ｳ繧ｳ繝ｫ繝ｼ繝√Φ
        /// </summary>
        private IEnumerator DynamicGenerationCoroutine()
        {
            while (enableDynamicGeneration)
            {
                yield return new WaitForSeconds(updateInterval);
                
                // 繝励Ξ繧､繝､繝ｼ霑ｽ霍｡縺ｨ蜻ｨ霎ｺ繧ｿ繧､繝ｫ隕∵ｱゅ・譖ｴ譁ｰ
                UpdatePlayerTracking();
                UpdateTileGeneration();

                // 譛ｬ繧ｵ繧､繧ｯ繝ｫ縺ｧ縺ｮ驥崎､・・蜑企勁繧帝亟豁｢
                didFullUnloadThisCycle = false;

                if (enableFrameTimeControl)
                {
                    VastcoreLogger.Instance.LogDebug("RuntimeTerrain", "ProcessGenerationQueueWithFrameLimit start");
                    // StartCoroutine 繧帝㍾縺ｭ縺壹↓縺昴・縺ｾ縺ｾ繧､繝・Ξ繝ｼ繧ｿ繝ｼ繧定ｿ斐☆
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
        /// 繝輔Ξ繝ｼ繝譎る俣蛻ｶ髯蝉ｻ倥″縺ｧ逕滓・繧ｭ繝･繝ｼ繧貞・逅・
        /// </summary>
        private IEnumerator ProcessGenerationQueueWithFrameLimit()
        {
            float frameStartTime = Time.realtimeSinceStartup;
            int processedCount = 0;
            int safetyFrameYields = 0;
            const int maxSafetyFrameYields = 300; // 邏・遘・60FPS諠ｳ螳・縺ｮ螳牙・蠑・

            while (generationQueue.Count > 0 && processedCount < maxTilesPerUpdate)
            {
                // 繝輔Ξ繝ｼ繝譎る俣繧偵メ繧ｧ繝・け
                float elapsedTime = (Time.realtimeSinceStartup - frameStartTime) * 1000f;
                if (elapsedTime > maxFrameTimeMs && processedCount >= minTilesPerUpdate)
                {
                    VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"GenQueue frame limit hit elapsed={elapsedTime:F2}ms processed={processedCount}/{maxTilesPerUpdate} q={generationQueue.Count}");
                    yield return null; // 谺｡縺ｮ繝輔Ξ繝ｼ繝縺ｫ蟒ｶ譛・
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
        /// 逕滓・繧ｭ繝･繝ｼ繧貞・逅・
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
        /// 蜑企勁繧ｭ繝･繝ｼ繧貞・逅・
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
        /// 繧ｿ繧､繝ｫ逕滓・繝ｪ繧ｯ繧ｨ繧ｹ繝医ｒ蜃ｦ逅・
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
                // TileManager縺瑚・蜍輔〒繝ｭ繝ｼ繝峨ｒ陦後≧縺溘ａ縲√％縺薙〒縺ｯ繝医Μ繧ｬ縺ｮ縺ｿ
                var tile = tileManager.GetTileAtWorldPosition(
                    tileManager.TileCoordinateToWorldPosition(request.coordinate));

                if (tile == null)
                {
                    // 譏守､ｺ逧・↑逕滓・縺ｯ陦後ｏ縺壹ゝileManager縺ｮ譖ｴ譁ｰ縺ｫ蟋碑ｭｲ
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
        /// 繧ｿ繧､繝ｫ蜑企勁繧貞・逅・
        /// </summary>
        private void ProcessTileDeletion(Vector2Int tileCoord)
        {
            try
            {
                var tile = tileManager.GetTileAtWorldPosition(
                    tileManager.TileCoordinateToWorldPosition(tileCoord));

                if (tile != null)
                {
                    // 迴ｾ迥ｶ縺ｮ繝輔か繝ｼ繝ｫ繝舌ャ繧ｯ: 蜈ｨ蜑企勁縲ゅ◆縺縺励し繧､繧ｯ繝ｫ蜀・〒荳蠎ｦ縺縺大ｮ溯｡・
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
        /// 譁ｰ縺励＞繧ｿ繧､繝ｫ繧堤函謌・
        /// </summary>
        private void GenerateNewTile(Vector2Int tileCoord)
        {
            // 蛟句挨逕滓・縺ｮ逶ｴ謗･API縺ｯ縺ｪ縺・◆繧√ゝileManager 縺ｮ閾ｪ蜍募・逅・↓蟋碑ｭｲ
            // 縺薙％縺ｧ縺ｯ霆ｽ驥上Ο繧ｰ縺ｮ縺ｿ繧貞・蜉・
            VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"GenerateNewTile requested coord={tileCoord} (delegated to TileManager)");
        }

        #endregion
        #endregion

        #region 繝｡繝｢繝ｪ邂｡逅・
        /// <summary>
        /// 繝｡繝｢繝ｪ邂｡逅・さ繝ｫ繝ｼ繝√Φ
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
        /// 繝｡繝｢繝ｪ菴ｿ逕ｨ驥上ｒ繝√ぉ繝・け
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
        /// 邱頑･繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・繧偵ヨ繝ｪ繧ｬ繝ｼ
        /// </summary>
        private void TriggerEmergencyCleanup()
        {
            if (playerTransform == null)
                return;

            Vector2Int playerTile = tileManager.WorldToTileCoordinate(playerTransform.position);
            var activeTiles = GetActiveTileCoordinates();

            // 繝励Ξ繧､繝､繝ｼ縺九ｉ驕縺・ち繧､繝ｫ繧貞ｼｷ蛻ｶ蜑企勁・医ヵ繧ｩ繝ｼ繝ｫ繝舌ャ繧ｯ縺ｨ縺励※蜈ｨ蜑企勁・・
            if (activeTiles.Count > 0)
            {
                tileManager.UnloadAllTiles();
            }

            performanceStats.emergencyCleanups++;
            VastcoreLogger.Instance.LogWarning("RuntimeTerrain", $"EmergencyCleanup executed (active={activeTiles.Count})");
        }

        /// <summary>
        /// 莠磯亟逧・け繝ｪ繝ｼ繝ｳ繧｢繝・・繧偵ヨ繝ｪ繧ｬ繝ｼ
        /// </summary>
        private void TriggerPreventiveCleanup()
        {
            if (playerTransform == null)
                return;

            Vector2Int playerTile = tileManager.WorldToTileCoordinate(playerTransform.position);
            var activeTiles = GetActiveTileCoordinates();

            // 譛繧る□縺・ち繧､繝ｫ縺九ｉ縺ｮ蜑企勁縺ｯ TileManager 蜀・↓蟋碑ｭｲ縲ゅ％縺薙〒縺ｯ蜈ｨ蜑企勁繧貞屓驕ｿ縺励√Ο繧ｰ縺ｮ縺ｿ縲・
            VastcoreLogger.Instance.LogInfo("RuntimeTerrain", $"PreventiveCleanup check active={activeTiles.Count} around tile={playerTile}");
        }

        /// <summary>
        /// 遨肴･ｵ逧・け繝ｪ繝ｼ繝ｳ繧｢繝・・繧貞ｮ溯｡・
        /// </summary>
        private void PerformAggressiveCleanup()
        {
            tileManager.UnloadAllTiles();
            performanceStats.aggressiveCleanups++;
            VastcoreLogger.Instance.LogWarning("RuntimeTerrain", "AggressiveCleanup executed -> UnloadAllTiles()");
        }

        /// <summary>
        /// 譛ｪ菴ｿ逕ｨ繝ｪ繧ｽ繝ｼ繧ｹ繧偵け繝ｪ繝ｼ繝ｳ繧｢繝・・
        /// </summary>
        private void CleanupUnusedResources()
        {
            // 蜿､縺・━蜈亥ｺｦ繧ｨ繝ｳ繝医Μ繧貞炎髯､
            var activeCoords = GetActiveTileCoordinates();
            var keysToRemove = tilePriorities.Keys.Where(key => !activeCoords.Contains(key)).ToList();

            foreach (var key in keysToRemove)
            {
                tilePriorities.Remove(key);
            }

            // 蜃ｦ逅・ｸｭ繧ｿ繧､繝ｫ繝ｪ繧ｹ繝医ｒ繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・
            processingTiles.RemoveWhere(coord => !activeCoords.Contains(coord));
            VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"CleanupUnusedResources removedPriorities={keysToRemove.Count}");
        }
        #endregion

        #region 繝ｦ繝ｼ繝・ぅ繝ｪ繝・ぅ
        /// <summary>
        /// 謖・ｮ壼濠蠕・・縺ｮ繧ｿ繧､繝ｫ蠎ｧ讓吶ｒ蜿門ｾ・
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
        /// 繧｢繧ｯ繝・ぅ繝悶ち繧､繝ｫ蠎ｧ讓吩ｸ隕ｧ
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
        /// 繧ｿ繧､繝ｫ縺後い繧ｯ繝・ぅ繝悶°
        /// </summary>
        private bool IsTileActive(Vector2Int coord)
        {
            if (tileManager == null) return false;
            return tileManager.GetActiveTiles().Any(t => t.coordinate == coord);
        }

        /// <summary>
        /// 蜻ｨ霎ｺ縺ｮ繧ｿ繧､繝ｫ隕∵ｱゅｒ譖ｴ譁ｰ
        /// </summary>
        private void UpdateTileGeneration()
        {
            if (playerTransform == null || tileManager == null) return;
            var center = tileManager.WorldToTileCoordinate(playerTransform.position);

            // 蜊ｳ譎・繝励Μ繝ｭ繝ｼ繝芽ｦ∵ｱ・
            foreach (var coord in GetTilesInRadius(center, immediateLoadRadius))
            {
                RequestTileGeneration(coord, TilePriority.Immediate);
            }
            foreach (var coord in GetTilesInRadius(center, preloadRadius))
            {
                if (!IsTileActive(coord))
                    RequestTileGeneration(coord, TilePriority.High);
            }

            // 蠑ｷ蛻ｶ繧｢繝ｳ繝ｭ繝ｼ繝臥ｯ・峇螟悶ｒ蜑企勁隕∵ｱ・
            TriggerTileCleanup(playerTransform.position);
        }

        /// <summary>
        /// 繧｢繝ｳ繝ｭ繝ｼ繝牙ｯｾ雎｡繧ｿ繧､繝ｫ繧偵く繝･繝ｼ縺ｫ霑ｽ蜉
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
                    // 蜊ｳ譎ょ炎髯､蟇ｾ雎｡
                    RequestTileDeletion(coord, TilePriority.Immediate);
                }
                else if (dist > keepAliveRadius)
                {
                    // 菴呵｣輔′縺ゅｋ繧ｿ繧､繝溘Φ繧ｰ縺ｧ蜑企勁
                    RequestTileDeletion(coord, TilePriority.Low);
                }
            }
        }

        /// <summary>
        /// 繧ｿ繧､繝ｫ逕滓・繧偵Μ繧ｯ繧ｨ繧ｹ繝・
        /// </summary>
        private void RequestTileGeneration(Vector2Int tileCoord, TilePriority priority)
        {
            // 譌｢縺ｫ蟄伜惠縺吶ｋ縺句・逅・ｸｭ縺ｮ蝣ｴ蜷医・繧ｹ繧ｭ繝・・
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

            // 蜆ｪ蜈亥ｺｦ縺ｫ蝓ｺ縺･縺・※繧ｭ繝･繝ｼ縺ｫ謖ｿ蜈･
            InsertGenerationRequestByPriority(request);
            tilePriorities[tileCoord] = priority;

            VastcoreLogger.Instance.LogInfo("RuntimeTerrain", $"RequestGen coord={tileCoord} pri={priority}");
            if (logTileOperations)
            {
                Debug.Log($"Requested tile generation: {tileCoord} (Priority: {priority})");
            }
        }

        /// <summary>
        /// 繧ｿ繧､繝ｫ蜑企勁繧偵Μ繧ｯ繧ｨ繧ｹ繝・
        /// </summary>
        private void RequestTileDeletion(Vector2Int tileCoord, TilePriority priority)
        {
            if (!IsTileActive(tileCoord))
            {
                VastcoreLogger.Instance.LogDebug("RuntimeTerrain", $"RequestDel skip inactive coord={tileCoord}");
                return;
            }

            // 蜆ｪ蜈亥ｺｦ縺ｫ蝓ｺ縺･縺・※蜑企勁繧ｭ繝･繝ｼ縺ｫ霑ｽ蜉
            if (priority == TilePriority.Immediate)
            {
                // 蜊ｳ蠎ｧ縺ｫ蜑企勁
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
        /// 蜆ｪ蜈亥ｺｦ縺ｫ蝓ｺ縺･縺・※逕滓・繝ｪ繧ｯ繧ｨ繧ｹ繝医ｒ謖ｿ蜈･
        /// </summary>
        private void InsertGenerationRequestByPriority(TileGenerationRequest request)
        {
            // 邁｡譏灘ｮ溯｣・ｼ壼━蜈亥ｺｦ縺ｮ鬮倥＞繧ゅ・繧貞・鬆ｭ縺ｫ霑ｽ蜉
            if (request.priority == TilePriority.Immediate)
            {
                // 蜊ｳ蠎ｧ縺ｫ蜃ｦ逅・☆繧九◆繧√√く繝･繝ｼ縺ｮ蜈磯ｭ縺ｫ謖ｿ蜈･縺吶ｋ莉｣繧上ｊ縺ｫ逶ｴ謗･蜃ｦ逅・
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
        /// 繝代ヵ繧ｩ繝ｼ繝槭Φ繧ｹ邨ｱ險医ｒ譖ｴ譁ｰ
        /// </summary>
        private void UpdatePerformanceStats()
        {
            performanceStats.frameCount++;
            performanceStats.averageFrameTime = Time.deltaTime;

            // TileManager 縺九ｉ邨ｱ險医ｒ蜿悶ｊ霎ｼ縺ｿ・育函謌・蜑企勁縺ｮ豁｣遒ｺ縺ｪ蜿肴丐縺ｫ蟇・ｸ趣ｼ・
            if (tileManager != null)
            {
                var stats = tileManager.GetStats();
                // 蜷郁ｨ亥､縺ｮ荳矩ｧ・ｒ螻･縺九○縺ｪ縺・ｈ縺・∵怙蟆城剞縺ｮ蜷梧悄・亥刈邂励＠縺ｪ縺・ｼ・
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

        #region 繝代ヶ繝ｪ繝・けAPI
        /// <summary>
        /// 蜍慕噪逕滓・繧呈怏蜉ｹ/辟｡蜉ｹ蛹・
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
        /// 繝代ヵ繧ｩ繝ｼ繝槭Φ繧ｹ邨ｱ險医ｒ蜿門ｾ・
        /// </summary>
        public PerformanceStats GetPerformanceStats()
        {
            return performanceStats;
        }

        /// <summary>
        /// 險ｭ螳壹ｒ譖ｴ譁ｰ
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
        /// 蠑ｷ蛻ｶ繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・繧貞ｮ溯｡・
        /// </summary>
        public void ForceCleanup()
        {
            TriggerEmergencyCleanup();
            PerformAggressiveCleanup();
        }

        /// <summary>
        /// 謖・ｮ壼ｺｧ讓吶・繧ｿ繧､繝ｫ繧貞叙蠕・
        /// </summary>
        public TerrainTile GetTerrainTile(Vector2Int coordinate)
        {
            if (tileManager != null)
            {
                return tileManager.GetActiveTiles().FirstOrDefault(t => t.coordinate == coordinate);
            }
            return null;
        }

        /// <summary>
        /// 繧｢繧ｯ繝・ぅ繝悶↑繧ｿ繧､繝ｫ縺ｮ繝ｪ繧ｹ繝医ｒ蜿門ｾ・
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

        #region 繝・ヰ繝・げ讖溯・
        private void OnDrawGizmos()
        {
            if (!showDebugInfo) return;
            if (!Application.isPlaying) return;
            DrawDebugInfo();
        }
        /// <summary>
        /// 繝・ヰ繝・げ諠・ｱ繧呈緒逕ｻ
        /// </summary>
        private void DrawDebugInfo()
        {
            if (playerTransform == null)
                return;

            Vector2Int playerTile = tileManager.WorldToTileCoordinate(playerTransform.position);
            Vector3 playerWorldPos = tileManager.TileCoordinateToWorldPosition(playerTile);

            // 蜷・濠蠕・ｒ謠冗判
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(playerWorldPos, immediateLoadRadius * tileManager.tileSize);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerWorldPos, preloadRadius * tileManager.tileSize);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerWorldPos, keepAliveRadius * tileManager.tileSize);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(playerWorldPos, forceUnloadRadius * tileManager.tileSize);

            // 莠域ｸｬ菴咲ｽｮ繧呈緒逕ｻ
            if (predictPlayerMovement)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(predictedPlayerPosition, 50f);
                Gizmos.DrawLine(playerTransform.position, predictedPlayerPosition);
            }
        }

        /// <summary>
        /// 繝・ヰ繝・げ諠・ｱ繧偵Ο繧ｰ蜃ｺ蜉・
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

        #region 繝・・繧ｿ讒矩
        /// <summary>
        /// 繧ｿ繧､繝ｫ逕滓・繝ｪ繧ｯ繧ｨ繧ｹ繝・
        /// </summary>
        [System.Serializable]
        public struct TileGenerationRequest
        {
            public Vector2Int coordinate;
            public TilePriority priority;
            public float requestTime;
        }

        /// <summary>
        /// 繧ｿ繧､繝ｫ蜆ｪ蜈亥ｺｦ
        /// </summary>
        public enum TilePriority
        {
            Low = 0,
            Medium = 1,
            High = 2,
            Immediate = 3
        }

        /// <summary>
        /// 繝代ヵ繧ｩ繝ｼ繝槭Φ繧ｹ邨ｱ險・
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
        /// 繝ｩ繝ｳ繧ｿ繧､繝蝨ｰ蠖｢險ｭ螳・
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
