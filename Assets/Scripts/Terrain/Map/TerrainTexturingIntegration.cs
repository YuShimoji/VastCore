using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Vastcore.Generation.Map;


namespace Vastcore.Generation
{
    /// <summary>
    /// 蝨ｰ蠖｢繝・け繧ｹ繝√Ε繝ｪ繝ｳ繧ｰ繧ｷ繧ｹ繝・Β邨ｱ蜷医け繝ｩ繧ｹ
    /// 譌｢蟄倥・RuntimeTerrainManager縺ｨBiomePresetManager縺ｨ縺ｮ邨ｱ蜷医ｒ邂｡逅・
    /// 隕∵ｱ・.1: 繝ｪ繧｢繝ｫ繧ｿ繧､繝縺ｧ縺ｮ迺ｰ蠅・､牙喧縺ｮ蜿肴丐
    /// </summary>
    public class TerrainTexturingIntegration : MonoBehaviour
    {
        #region 險ｭ螳壹ヱ繝ｩ繝｡繝ｼ繧ｿ
        [Header("邨ｱ蜷郁ｨｭ螳・)]
        public bool enableAutoIntegration = true;
        public float integrationUpdateInterval = 0.5f;
        public float textureUpdateRadius = 2000f;
        
        [Header("繧ｷ繧ｹ繝・Β蜿ら・")]
        public RuntimeTerrainManager terrainManager;
        public BiomePresetManager biomePresetManager;
        public TerrainTexturingSystem texturingSystem;
        public DynamicMaterialBlendingSystem blendingSystem;
        
        [Header("閾ｪ蜍輔ユ繧ｯ繧ｹ繝√Ε驕ｩ逕ｨ")]
        public bool autoApplyTexturesOnTileGeneration = true;
        public bool autoApplyBiomeTextures = true;
        public bool autoUpdateEnvironmentalTextures = true;
        public bool autoUpdateLODTextures = true;
        
        [Header("繝代ヵ繧ｩ繝ｼ繝槭Φ繧ｹ蛻ｶ蠕｡")]
        public int maxTextureUpdatesPerFrame = 3;
        public float maxFrameTimeMs = 10f;
        public bool enableFrameTimeControl = true;
        #endregion

        #region 繝励Λ繧､繝吶・繝亥､画焚
        private Dictionary<TerrainTile, TextureIntegrationData> tileTextureData = new Dictionary<TerrainTile, TextureIntegrationData>();
        private Queue<TextureIntegrationRequest> integrationQueue = new Queue<TextureIntegrationRequest>();
        private Transform playerTransform;
        private float lastIntegrationUpdate = 0f;
        
        // 繝代ヵ繧ｩ繝ｼ繝槭Φ繧ｹ邨ｱ險・
        private IntegrationStatistics statistics = new IntegrationStatistics();
        #endregion

        #region Unity 繧､繝吶Φ繝・
        void Start()
        {
            InitializeIntegration();
        }
        
        void Update()
        {
            if (enableAutoIntegration && Time.time - lastIntegrationUpdate >= integrationUpdateInterval)
            {
                UpdateIntegration();
                lastIntegrationUpdate = Time.time;
            }
            
            ProcessIntegrationQueue();
        }
        
        void OnDestroy()
        {
            CleanupIntegration();
        }
        #endregion

        #region 蛻晄悄蛹・
        /// <summary>
        /// 邨ｱ蜷医す繧ｹ繝・Β繧貞・譛溷喧
        /// </summary>
        private void InitializeIntegration()
        {
            Debug.Log("Initializing TerrainTexturingIntegration...");
            
            // 蠢・ｦ√↑繧ｳ繝ｳ繝昴・繝阪Φ繝医ｒ蜿門ｾ励∪縺溘・菴懈・
            if (terrainManager == null)
                terrainManager = FindFirstObjectByType<RuntimeTerrainManager>();
            
            if (biomePresetManager == null)
                biomePresetManager = FindFirstObjectByType<BiomePresetManager>();
            
            if (texturingSystem == null)
            {
                texturingSystem = GetComponent<TerrainTexturingSystem>();
                if (texturingSystem == null)
                    texturingSystem = gameObject.AddComponent<TerrainTexturingSystem>();
            }
            
            if (blendingSystem == null)
            {
                blendingSystem = GetComponent<DynamicMaterialBlendingSystem>();
                if (blendingSystem == null)
                    blendingSystem = gameObject.AddComponent<DynamicMaterialBlendingSystem>();
            }
            
            // 繝励Ξ繧､繝､繝ｼTransform繧貞叙蠕・
            var playerController = FindFirstObjectByType<AdvancedPlayerController>();
            if (playerController != null)
            {
                playerTransform = playerController.transform;
            }
            
            // 繧､繝吶Φ繝医ワ繝ｳ繝峨Λ繝ｼ繧堤匳骭ｲ
            RegisterEventHandlers();
            
            Debug.Log("TerrainTexturingIntegration initialized successfully");
        }
        
        /// <summary>
        /// 繧､繝吶Φ繝医ワ繝ｳ繝峨Λ繝ｼ繧堤匳骭ｲ
        /// </summary>
        private void RegisterEventHandlers()
        {
            // RuntimeTerrainManager縺ｮ繧､繝吶Φ繝医↓逋ｻ骭ｲ・亥庄閭ｽ縺ｪ蝣ｴ蜷茨ｼ・
            // 豕ｨ諢・ 螳滄圀縺ｮ繧､繝吶Φ繝医す繧ｹ繝・Β縺悟ｮ溯｣・＆繧後※縺・ｋ蝣ｴ蜷医・縺ｿ
            
            // 莉｣譖ｿ縺ｨ縺励※縲∝ｮ壽悄逧・↑繝昴・繝ｪ繝ｳ繧ｰ縺ｧ譁ｰ縺励＞繧ｿ繧､繝ｫ繧呈､懷・
            StartCoroutine(MonitorNewTiles());
        }
        
        /// <summary>
        /// 譁ｰ縺励＞繧ｿ繧､繝ｫ繧堤屮隕・
        /// </summary>
        private IEnumerator MonitorNewTiles()
        {
            while (true)
            {
                yield return new WaitForSeconds(integrationUpdateInterval);
                
                if (terrainManager != null)
                {
                    CheckForNewTiles();
                }
            }
        }
        #endregion

        #region 邨ｱ蜷亥・逅・
        /// <summary>
        /// 邨ｱ蜷医ｒ譖ｴ譁ｰ
        /// </summary>
        private void UpdateIntegration()
        {
            if (playerTransform == null)
                return;
            
            // 繝励Ξ繧､繝､繝ｼ蜻ｨ霎ｺ縺ｮ繧ｿ繧､繝ｫ繧呈峩譁ｰ
            UpdateNearbyTileTextures();
            
            // 迺ｰ蠅・､牙喧繧帝←逕ｨ
            if (autoUpdateEnvironmentalTextures)
            {
                UpdateEnvironmentalTextures();
            }
            
            // LOD繧呈峩譁ｰ
            if (autoUpdateLODTextures)
            {
                UpdateLODTextures();
            }
            
            // 邨ｱ險医ｒ譖ｴ譁ｰ
            UpdateStatistics();
        }
        
        /// <summary>
        /// 譁ｰ縺励＞繧ｿ繧､繝ｫ繧偵メ繧ｧ繝・け
        /// </summary>
        private void CheckForNewTiles()
        {
            // RuntimeTerrainManager縺九ｉ迴ｾ蝨ｨ繧｢繧ｯ繝・ぅ繝悶↑繧ｿ繧､繝ｫ繧貞叙蠕・
            // 豕ｨ諢・ 螳滄圀縺ｮAPI縺ｫ蠢懊§縺ｦ螳溯｣・ｒ隱ｿ謨ｴ
            
            if (playerTransform == null)
                return;
            
            Vector3 playerPos = playerTransform.position;
            
            // 繝励Ξ繧､繝､繝ｼ蜻ｨ霎ｺ縺ｮ遽・峇縺ｧ繧ｿ繧､繝ｫ繧偵メ繧ｧ繝・け
            int tileRadius = Mathf.CeilToInt(textureUpdateRadius / 1000f); // 繧ｿ繧､繝ｫ繧ｵ繧､繧ｺ繧・000m縺ｨ莉ｮ螳・
            
            for (int x = -tileRadius; x <= tileRadius; x++)
            {
                for (int y = -tileRadius; y <= tileRadius; y++)
                {
                    Vector3 tileWorldPos = playerPos + new Vector3(x * 1000f, 0f, y * 1000f);
                    
                    // 縺薙・菴咲ｽｮ縺ｫ繧ｿ繧､繝ｫ縺悟ｭ伜惠縺吶ｋ縺九メ繧ｧ繝・け・井ｻｮ諠ｳ逧・↑螳溯｣・ｼ・
                    var tile = GetTileAtWorldPosition(tileWorldPos);
                    if (tile != null && !tileTextureData.ContainsKey(tile))
                    {
                        OnNewTileDetected(tile);
                    }
                }
            }
        }
        
        /// <summary>
        /// 譁ｰ縺励＞繧ｿ繧､繝ｫ縺梧､懷・縺輔ｌ縺滓凾縺ｮ蜃ｦ逅・
        /// </summary>
        private void OnNewTileDetected(TerrainTile tile)
        {
            Debug.Log($"New tile detected: {tile.coordinate}");
            
            // 繝・け繧ｹ繝√Ε邨ｱ蜷医ョ繝ｼ繧ｿ繧剃ｽ懈・
            var integrationData = new TextureIntegrationData(tile);
            tileTextureData[tile] = integrationData;
            
            // 閾ｪ蜍輔ユ繧ｯ繧ｹ繝√Ε驕ｩ逕ｨ
            if (autoApplyTexturesOnTileGeneration)
            {
                RequestTextureApplication(tile, TextureApplicationType.Initial);
            }
            
            // 繝舌う繧ｪ繝ｼ繝繝・け繧ｹ繝√Ε驕ｩ逕ｨ
            if (autoApplyBiomeTextures)
            {
                ApplyBiomeTextureToTile(tile);
            }
            
            statistics.tilesProcessed++;
        }
        
        /// <summary>
        /// 霑代￥縺ｮ繧ｿ繧､繝ｫ繝・け繧ｹ繝√Ε繧呈峩譁ｰ
        /// </summary>
        private void UpdateNearbyTileTextures()
        {
            Vector3 playerPos = playerTransform.position;
            
            foreach (var kvp in tileTextureData.ToArray())
            {
                var tile = kvp.Key;
                var integrationData = kvp.Value;
                
                if (tile == null || tile.tileObject == null)
                {
                    tileTextureData.Remove(tile);
                    continue;
                }
                
                float distance = Vector3.Distance(playerPos, tile.worldPosition);
                
                if (distance <= textureUpdateRadius)
                {
                    // 譖ｴ譁ｰ縺悟ｿ・ｦ√°繝√ぉ繝・け
                    if (ShouldUpdateTileTexture(tile, integrationData))
                    {
                        RequestTextureApplication(tile, TextureApplicationType.Update);
                    }
                }
                else if (distance > textureUpdateRadius * 2f)
                {
                    // 驕縺吶℃繧九ち繧､繝ｫ縺ｯ蜑企勁
                    tileTextureData.Remove(tile);
                    texturingSystem.CleanupTextureData(tile);
                }
            }
        }
        
        /// <summary>
        /// 繧ｿ繧､繝ｫ繝・け繧ｹ繝√Ε縺ｮ譖ｴ譁ｰ縺悟ｿ・ｦ√°繝√ぉ繝・け
        /// </summary>
        private bool ShouldUpdateTileTexture(TerrainTile tile, TextureIntegrationData integrationData)
        {
            float timeSinceLastUpdate = Time.time - integrationData.lastTextureUpdate;
            
            // 螳壽悄譖ｴ譁ｰ
            if (timeSinceLastUpdate > 10f)
                return true;
            
            // 霍晞屬螟牙喧縺ｫ繧医ｋ譖ｴ譁ｰ
            float currentDistance = Vector3.Distance(playerTransform.position, tile.worldPosition);
            float distanceChange = Mathf.Abs(currentDistance - integrationData.lastUpdateDistance);
            if (distanceChange > 200f)
                return true;
            
            // 繝舌う繧ｪ繝ｼ繝螟牙喧縺ｫ繧医ｋ譖ｴ譁ｰ
            if (integrationData.needsBiomeUpdate)
                return true;
            
            return false;
        }
        
        /// <summary>
        /// 迺ｰ蠅・ユ繧ｯ繧ｹ繝√Ε繧呈峩譁ｰ
        /// </summary>
        private void UpdateEnvironmentalTextures()
        {
            var currentConditions = GetCurrentEnvironmentalConditions();
            
            foreach (var kvp in tileTextureData)
            {
                var tile = kvp.Key;
                var integrationData = kvp.Value;
                
                if (tile != null && HasEnvironmentalChange(integrationData, currentConditions))
                {
                    blendingSystem.ApplyEnvironmentalBlend(tile, currentConditions);
                    integrationData.lastEnvironmentalConditions = currentConditions;
                    integrationData.lastEnvironmentalUpdate = Time.time;
                }
            }
        }
        
        /// <summary>
        /// LOD繝・け繧ｹ繝√Ε繧呈峩譁ｰ
        /// </summary>
        private void UpdateLODTextures()
        {
            Vector3 playerPos = playerTransform.position;
            
            foreach (var kvp in tileTextureData)
            {
                var tile = kvp.Key;
                var integrationData = kvp.Value;
                
                if (tile != null)
                {
                    float currentDistance = Vector3.Distance(playerPos, tile.worldPosition);
                    float distanceChange = Mathf.Abs(currentDistance - integrationData.lastLODDistance);
                    
                    if (distanceChange > 100f) // LOD譖ｴ譁ｰ髢ｾ蛟､
                    {
                        tile.distanceFromPlayer = currentDistance;
                        blendingSystem.ApplyDistanceLODBlend(tile);
                        integrationData.lastLODDistance = currentDistance;
                        integrationData.lastLODUpdate = Time.time;
                    }
                }
            }
        }
        #endregion

        #region 繝・け繧ｹ繝√Ε驕ｩ逕ｨ
        /// <summary>
        /// 繝・け繧ｹ繝√Ε驕ｩ逕ｨ繧偵Μ繧ｯ繧ｨ繧ｹ繝・
        /// </summary>
        private void RequestTextureApplication(TerrainTile tile, TextureApplicationType applicationType)
        {
            var request = new TextureIntegrationRequest
            {
                tile = tile,
                applicationType = applicationType,
                priority = CalculateRequestPriority(tile),
                requestTime = Time.time
            };
            
            integrationQueue.Enqueue(request);
        }
        
        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝繝・け繧ｹ繝√Ε繧偵ち繧､繝ｫ縺ｫ驕ｩ逕ｨ
        /// </summary>
        private void ApplyBiomeTextureToTile(TerrainTile tile)
        {
            if (biomePresetManager == null)
                return;
            
            // 繧ｿ繧､繝ｫ菴咲ｽｮ縺ｫ蝓ｺ縺･縺・※繝舌う繧ｪ繝ｼ繝繧呈ｱｺ螳・
            var biomePreset = DetermineBiomeForTile(tile);
            
            if (biomePreset != null)
            {
                texturingSystem.ApplyBiomeTextures(tile, biomePreset);
                blendingSystem.ApplyBiomeBlend(tile, biomePreset);
                
                if (tileTextureData.ContainsKey(tile))
                {
                    tileTextureData[tile].currentBiome = biomePreset;
                    tileTextureData[tile].lastBiomeUpdate = Time.time;
                    tileTextureData[tile].needsBiomeUpdate = false;
                }
            }
        }
        
        /// <summary>
        /// 繧ｿ繧､繝ｫ縺ｮ繝舌う繧ｪ繝ｼ繝繧呈ｱｺ螳・
        /// </summary>
        private BiomePreset DetermineBiomeForTile(TerrainTile tile)
        {
            if (biomePresetManager == null || biomePresetManager.availablePresets.Count == 0)
                return null;
            
            // 邁｡譏灘ｮ溯｣・ 繧ｿ繧､繝ｫ蠎ｧ讓吶↓蝓ｺ縺･縺・※繝舌う繧ｪ繝ｼ繝繧帝∈謚・
            int biomeIndex = (Mathf.Abs(tile.coordinate.x) + Mathf.Abs(tile.coordinate.y)) % biomePresetManager.availablePresets.Count;
            return biomePresetManager.availablePresets[biomeIndex];
        }
        
        /// <summary>
        /// 邨ｱ蜷医く繝･繝ｼ繧貞・逅・
        /// </summary>
        private void ProcessIntegrationQueue()
        {
            float frameStartTime = Time.realtimeSinceStartup;
            int processedCount = 0;
            
            while (integrationQueue.Count > 0 && processedCount < maxTextureUpdatesPerFrame)
            {
                // 繝輔Ξ繝ｼ繝譎る俣蛻ｶ蠕｡
                if (enableFrameTimeControl)
                {
                    float elapsedTime = (Time.realtimeSinceStartup - frameStartTime) * 1000f;
                    if (elapsedTime > maxFrameTimeMs && processedCount > 0)
                    {
                        break;
                    }
                }
                
                var request = integrationQueue.Dequeue();
                ProcessIntegrationRequest(request);
                processedCount++;
                
                statistics.requestsProcessed++;
            }
        }
        
        /// <summary>
        /// 邨ｱ蜷医Μ繧ｯ繧ｨ繧ｹ繝医ｒ蜃ｦ逅・
        /// </summary>
        private void ProcessIntegrationRequest(TextureIntegrationRequest request)
        {
            if (request.tile == null || request.tile.tileObject == null)
                return;
            
            try
            {
                switch (request.applicationType)
                {
                    case TextureApplicationType.Initial:
                        texturingSystem.ApplyTextureToTile(request.tile);
                        ApplyBiomeTextureToTile(request.tile);
                        break;
                        
                    case TextureApplicationType.Update:
                        texturingSystem.ApplyTextureToTile(request.tile);
                        break;
                        
                    case TextureApplicationType.BiomeChange:
                        ApplyBiomeTextureToTile(request.tile);
                        break;
                        
                    case TextureApplicationType.Environmental:
                        var conditions = GetCurrentEnvironmentalConditions();
                        blendingSystem.ApplyEnvironmentalBlend(request.tile, conditions);
                        break;
                        
                    case TextureApplicationType.LOD:
                        blendingSystem.ApplyDistanceLODBlend(request.tile);
                        break;
                }
                
                // 邨ｱ蜷医ョ繝ｼ繧ｿ繧呈峩譁ｰ
                if (tileTextureData.ContainsKey(request.tile))
                {
                    var integrationData = tileTextureData[request.tile];
                    integrationData.lastTextureUpdate = Time.time;
                    integrationData.lastUpdateDistance = Vector3.Distance(playerTransform.position, request.tile.worldPosition);
                    integrationData.updateCount++;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to process texture integration request for tile {request.tile.coordinate}: {e.Message}");
                statistics.processingErrors++;
            }
        }
        #endregion

        #region 繝ｦ繝ｼ繝・ぅ繝ｪ繝・ぅ
        /// <summary>
        /// 繝ｯ繝ｼ繝ｫ繝我ｽ咲ｽｮ縺ｮ繧ｿ繧､繝ｫ繧貞叙蠕暦ｼ井ｻｮ諠ｳ螳溯｣・ｼ・
        /// </summary>
        private TerrainTile GetTileAtWorldPosition(Vector3 worldPos)
        {
            // 螳滄圀縺ｮ螳溯｣・〒縺ｯ縲ヽuntimeTerrainManager縺ｮAPI繧剃ｽｿ逕ｨ
            // 縺薙％縺ｧ縺ｯ莉ｮ諠ｳ逧・↑螳溯｣・
            return null;
        }
        
        /// <summary>
        /// 繝ｪ繧ｯ繧ｨ繧ｹ繝亥━蜈亥ｺｦ繧定ｨ育ｮ・
        /// </summary>
        private int CalculateRequestPriority(TerrainTile tile)
        {
            if (playerTransform == null)
                return 1;
            
            float distance = Vector3.Distance(playerTransform.position, tile.worldPosition);
            
            if (distance < 500f)
                return 3; // 鬮伜━蜈亥ｺｦ
            else if (distance < 1000f)
                return 2; // 荳ｭ蜆ｪ蜈亥ｺｦ
            else
                return 1; // 菴主━蜈亥ｺｦ
        }
        
        /// <summary>
        /// 迴ｾ蝨ｨ縺ｮ迺ｰ蠅・擅莉ｶ繧貞叙蠕・
        /// </summary>
        private EnvironmentalConditions GetCurrentEnvironmentalConditions()
        {
            return new EnvironmentalConditions
            {
                season = GetCurrentSeason(),
                temperature = GetCurrentTemperature(),
                moisture = GetCurrentMoisture(),
                timeOfDay = GetCurrentTimeOfDay()
            };
        }
        
        /// <summary>
        /// 迺ｰ蠅・､牙喧縺後≠繧九°繝√ぉ繝・け
        /// </summary>
        private bool HasEnvironmentalChange(TextureIntegrationData integrationData, EnvironmentalConditions currentConditions)
        {
            var lastConditions = integrationData.lastEnvironmentalConditions;
            
            return Mathf.Abs(currentConditions.temperature - lastConditions.temperature) > 0.1f ||
                   Mathf.Abs(currentConditions.moisture - lastConditions.moisture) > 0.1f ||
                   Mathf.Abs(currentConditions.timeOfDay - lastConditions.timeOfDay) > 0.1f ||
                   currentConditions.season != lastConditions.season;
        }
        
        /// <summary>
        /// 邨ｱ險医ｒ譖ｴ譁ｰ
        /// </summary>
        private void UpdateStatistics()
        {
            statistics.activeTiles = tileTextureData.Count;
            statistics.queuedRequests = integrationQueue.Count;
            statistics.frameRate = 1f / Time.deltaTime;
            statistics.memoryUsage = System.GC.GetTotalMemory(false) / (1024 * 1024);
        }
        
        // 迺ｰ蠅・ョ繝ｼ繧ｿ蜿門ｾ励Γ繧ｽ繝・ラ・育ｰ｡譏灘ｮ溯｣・ｼ・
        private Season GetCurrentSeason()
        {
            float seasonTime = (Time.time / 300f) % 4f;
            return (Season)Mathf.FloorToInt(seasonTime);
        }
        
        private float GetCurrentTemperature()
        {
            Season season = GetCurrentSeason();
            float baseTemp = season == Season.Summer ? 0.8f : season == Season.Winter ? 0.2f : 0.5f;
            return Mathf.Clamp01(baseTemp + Mathf.Sin(Time.time * 0.1f) * 0.1f);
        }
        
        private float GetCurrentMoisture()
        {
            return Mathf.Clamp01(0.5f + Mathf.Sin(Time.time * 0.05f) * 0.3f);
        }
        
        private float GetCurrentTimeOfDay()
        {
            return (Time.time * 0.01f) % 1f;
        }
        #endregion

        #region 繝代ヶ繝ｪ繝・けAPI
        /// <summary>
        /// 謇句虚縺ｧ繧ｿ繧､繝ｫ繝・け繧ｹ繝√Ε繧呈峩譁ｰ
        /// </summary>
        public void UpdateTileTexture(TerrainTile tile)
        {
            if (tile != null)
            {
                RequestTextureApplication(tile, TextureApplicationType.Update);
            }
        }
        
        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝螟画峩繧帝←逕ｨ
        /// </summary>
        public void ApplyBiomeChange(TerrainTile tile, BiomePreset newBiome)
        {
            if (tile != null && newBiome != null)
            {
                texturingSystem.ApplyBiomeTextures(tile, newBiome);
                blendingSystem.ApplyBiomeBlend(tile, newBiome);
                
                if (tileTextureData.ContainsKey(tile))
                {
                    tileTextureData[tile].currentBiome = newBiome;
                    tileTextureData[tile].needsBiomeUpdate = false;
                }
            }
        }
        
        /// <summary>
        /// 邨ｱ險域ュ蝣ｱ繧貞叙蠕・
        /// </summary>
        public IntegrationStatistics GetStatistics()
        {
            return statistics;
        }
        
        /// <summary>
        /// 邨ｱ蜷医ｒ譛牙柑/辟｡蜉ｹ蛹・
        /// </summary>
        public void SetIntegrationEnabled(bool enabled)
        {
            enableAutoIntegration = enabled;
        }
        #endregion

        #region 繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・
        /// <summary>
        /// 邨ｱ蜷医ｒ繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・
        /// </summary>
        private void CleanupIntegration()
        {
            foreach (var kvp in tileTextureData)
            {
                texturingSystem.CleanupTextureData(kvp.Key);
            }
            
            tileTextureData.Clear();
            integrationQueue.Clear();
        }
        #endregion
    }
    
    /// <summary>
    /// 繝・け繧ｹ繝√Ε邨ｱ蜷医ョ繝ｼ繧ｿ
    /// </summary>
    [System.Serializable]
    public class TextureIntegrationData
    {
        public TerrainTile tile;
        public BiomePreset currentBiome;
        public EnvironmentalConditions lastEnvironmentalConditions = new EnvironmentalConditions();
        
        public float lastTextureUpdate = 0f;
        public float lastBiomeUpdate = 0f;
        public float lastEnvironmentalUpdate = 0f;
        public float lastLODUpdate = 0f;
        
        public float lastUpdateDistance = 0f;
        public float lastLODDistance = 0f;
        
        public bool needsBiomeUpdate = false;
        public int updateCount = 0;
        
        public TextureIntegrationData(TerrainTile associatedTile)
        {
            tile = associatedTile;
            lastTextureUpdate = Time.time;
        }
    }
    
    /// <summary>
    /// 繝・け繧ｹ繝√Ε邨ｱ蜷医Μ繧ｯ繧ｨ繧ｹ繝・
    /// </summary>
    [System.Serializable]
    public class TextureIntegrationRequest
    {
        public TerrainTile tile;
        public TextureApplicationType applicationType;
        public int priority;
        public float requestTime;
    }
    
    /// <summary>
    /// 邨ｱ蜷育ｵｱ險・
    /// </summary>
    [System.Serializable]
    public class IntegrationStatistics
    {
        public int tilesProcessed = 0;
        public int requestsProcessed = 0;
        public int processingErrors = 0;
        public int activeTiles = 0;
        public int queuedRequests = 0;
        public float frameRate = 0f;
        public long memoryUsage = 0;
    }
    
    /// <summary>
    /// 繝・け繧ｹ繝√Ε驕ｩ逕ｨ繧ｿ繧､繝・
    /// </summary>
    public enum TextureApplicationType
    {
        Initial,        // 蛻晄悄驕ｩ逕ｨ
        Update,         // 譖ｴ譁ｰ
        BiomeChange,    // 繝舌う繧ｪ繝ｼ繝螟画峩
        Environmental,  // 迺ｰ蠅・､牙喧
        LOD            // LOD螟画峩
    }
}
