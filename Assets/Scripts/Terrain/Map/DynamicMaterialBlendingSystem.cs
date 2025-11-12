using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Vastcore.Utilities;
using Vastcore.Player;

namespace Vastcore.Generation
{
    /// <summary>
    /// 蜍慕噪繝槭ユ繝ｪ繧｢繝ｫ繝悶Ξ繝ｳ繝・ぅ繝ｳ繧ｰ繧ｷ繧ｹ繝・Β
    /// 隕∵ｱ・.1: 隍・焚繝・け繧ｹ繝√Ε縺ｮ閾ｪ辟ｶ縺ｪ繝悶Ξ繝ｳ繝・ぅ繝ｳ繧ｰ縺ｨ繝ｪ繧｢繝ｫ繧ｿ繧､繝�迺ｰ蠅・､牙喧縺ｮ蜿肴丐
    /// </summary>
    public class DynamicMaterialBlendingSystem : MonoBehaviour
    {
        #region 險ｭ螳壹ヱ繝ｩ繝｡繝ｼ繧ｿ
        [Header("Material Blending Settings")]
        public bool enableDynamicBlending = true;
        public float blendTransitionSpeed = 2f;
        public int maxSimultaneousBlends = 4;
        public AnimationCurve blendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Distance LOD")]
        public bool enableDistanceLOD = true;
        public float[] lodDistances = { 500f, 1000f, 2000f, 4000f };
        public float[] lodTextureScales = { 1f, 0.75f, 0.5f, 0.25f };
        public float[] lodBlendSpeeds = { 1f, 0.8f, 0.6f, 0.4f };
        
        [Header("Realtime Updates")]
        public bool enableRealtimeUpdates = true;
        public float updateInterval = 0.1f;
        public int maxUpdatesPerFrame = 5;
        public float updateRadius = 1500f;
        
        [Header("Environmental Blending")]
        public bool enableEnvironmentalBlending = true;
        public float environmentalBlendSpeed = 1f;
        public bool enableSeasonalTransitions = true;
        public float seasonalTransitionDuration = 10f;
        
        [Header("Frame Rate Control")]
        public bool enableFrameRateControl = true;
        public float targetFrameTime = 16.67f; // 60FPS
        public int minBlendsPerFrame = 1;
        public int maxBlendsPerFrame = 10;
        #endregion

        #region 繝励Λ繧､繝吶・繝亥､画焚
        private Dictionary<TerrainTile, MaterialBlendData> activeMaterialBlends = new Dictionary<TerrainTile, MaterialBlendData>();
        private Queue<MaterialBlendRequest> blendRequestQueue = new Queue<MaterialBlendRequest>();
        private Transform playerTransform;
        
        private TerrainTexturingSystem texturingSystem;
        
        // 繝代ヵ繧ｩ繝ｼ繝槭Φ繧ｹ邨ｱ險・
        private float lastUpdateTime = 0f;
        private int blendsProcessedThisFrame = 0;
        private float frameStartTime = 0f;
        
        // 繧ｳ繝ｫ繝ｼ繝√Φ邂｡逅・
        private Coroutine blendProcessingCoroutine;
        private Coroutine environmentalUpdateCoroutine;
        #endregion

        #region Unity 繧､繝吶Φ繝・
        void Start()
        {
            InitializeBlendingSystem();
        }
        
        void Update()
        {
            if (enableRealtimeUpdates && Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateMaterialBlends();
                lastUpdateTime = Time.time;
            }
            
            ProcessBlendRequests();
        }
        
        void OnDestroy()
        {
            StopAllCoroutines();
            CleanupAllBlends();
        }
        #endregion

        /// <summary>
        /// 繝槭ユ繝ｪ繧｢繝ｫ繝悶Ξ繝ｳ繝峨ｒ譖ｴ譁ｰ
        /// </summary>
        private void UpdateMaterialBlends()
        {
            if (!enableDynamicBlending)
                return;

            // 繧｢繧ｯ繝・ぅ繝悶↑繝悶Ξ繝ｳ繝峨ｒ譖ｴ譁ｰ
            UpdateActiveBlends();

            // 螳御ｺ・＠縺溘ヶ繝ｬ繝ｳ繝峨ｒ繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・
            CleanupCompletedBlends();

            // 繝輔Ξ繝ｼ繝�繝ｬ繝ｼ繝亥宛蠕｡
            if (enableFrameRateControl)
            {
                float elapsedTime = (Time.realtimeSinceStartup - frameStartTime) * 1000f;
                if (elapsedTime > targetFrameTime)
                {
                    // 谺｡縺ｮ繝輔Ξ繝ｼ繝�縺ｾ縺ｧ蠕・ｩ・
                    return;
                }
            }
        }

        #region 蛻晄悄蛹・
        /// <summary>
        /// 繝悶Ξ繝ｳ繝・ぅ繝ｳ繧ｰ繧ｷ繧ｹ繝・Β繧貞・譛溷喧
        /// </summary>
        private void InitializeBlendingSystem()
        {
            Debug.Log("Initializing DynamicMaterialBlendingSystem...");
            
            // 蠢・ｦ√↑繧ｳ繝ｳ繝昴・繝阪Φ繝医ｒ蜿門ｾ・
            texturingSystem = GetComponent<TerrainTexturingSystem>();
            if (texturingSystem == null)
            {
                texturingSystem = gameObject.AddComponent<TerrainTexturingSystem>();
            }
            
            // プレイヤーTransformを取得
            var playerController = FindFirstObjectByType<AdvancedPlayerController>();
            if (playerController != null)
            {
                playerTransform = playerController.transform;
            }
            else if (playerTransform == null)
            {
                playerTransform = ResolvePlayerTransform();
            }
            
            // 繧ｳ繝ｫ繝ｼ繝√Φ繧帝幕蟋・
            StartBlendProcessing();
            
            if (enableEnvironmentalBlending)
            {
                StartEnvironmentalUpdates();
            }
            
            Debug.Log("DynamicMaterialBlendingSystem initialized successfully");
        }
        
        /// <summary>
        /// 繝悶Ξ繝ｳ繝牙・逅・さ繝ｫ繝ｼ繝√Φ繧帝幕蟋・
        /// </summary>
        private void StartBlendProcessing()
        {
            if (blendProcessingCoroutine != null)
            {
                StopCoroutine(blendProcessingCoroutine);
            }
            
            blendProcessingCoroutine = StartCoroutine(BlendProcessingCoroutine());
        }
        
        /// <summary>
        /// 迺ｰ蠅・峩譁ｰ繧ｳ繝ｫ繝ｼ繝√Φ繧帝幕蟋・
        /// </summary>
        private void StartEnvironmentalUpdates()
        {
            if (environmentalUpdateCoroutine != null)
            {
                StopCoroutine(environmentalUpdateCoroutine);
            }
            
            environmentalUpdateCoroutine = StartCoroutine(EnvironmentalUpdateCoroutine());
        }
        #endregion

        #region 繝代ヶ繝ｪ繝・けAPI
        /// <summary>
        /// 繝槭ユ繝ｪ繧｢繝ｫ繝悶Ξ繝ｳ繝峨ｒ繝ｪ繧ｯ繧ｨ繧ｹ繝・
        /// </summary>
        public void RequestMaterialBlend(TerrainTile tile, MaterialBlendType blendType, object blendData = null)
        {
            if (tile == null)
                return;
            
            var request = new MaterialBlendRequest
            {
                tile = tile,
                blendType = blendType,
                blendData = blendData,
                priority = CalculateBlendPriority(tile),
                requestTime = Time.time
            };
            
            blendRequestQueue.Enqueue(request);
        }
        
        /// <summary>
        /// 霍晞屬繝吶・繧ｹLOD繝悶Ξ繝ｳ繝峨ｒ驕ｩ逕ｨ
        /// </summary>
        public void ApplyDistanceLODBlend(TerrainTile tile)
        {
            if (!enableDistanceLOD || tile == null)
                return;
            
            float distance = CalculateDistanceToPlayer(tile);
            int lodLevel = CalculateLODLevel(distance);
            
            RequestMaterialBlend(tile, MaterialBlendType.DistanceLOD, lodLevel);
        }
        
        /// <summary>
        /// 迺ｰ蠅・､牙喧繝悶Ξ繝ｳ繝峨ｒ驕ｩ逕ｨ
        /// </summary>
        public void ApplyEnvironmentalBlend(TerrainTile tile, EnvironmentalConditions conditions)
        {
            if (!enableEnvironmentalBlending || tile == null)
                return;
            
            RequestMaterialBlend(tile, MaterialBlendType.Environmental, conditions);
        }
        
        /// <summary>
        /// 蟄｣遽螟牙喧繝悶Ξ繝ｳ繝峨ｒ驕ｩ逕ｨ
        /// </summary>
        public void ApplySeasonalBlend(TerrainTile tile, Season targetSeason)
        {
            if (!enableSeasonalTransitions || tile == null)
                return;
            
            RequestMaterialBlend(tile, MaterialBlendType.Seasonal, targetSeason);
        }
        
        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝�繝悶Ξ繝ｳ繝峨ｒ驕ｩ逕ｨ
        /// </summary>
        public void ApplyBiomeBlend(TerrainTile tile, BiomePreset biomePreset)
        {
            if (tile == null || biomePreset == null)
                return;
            
            RequestMaterialBlend(tile, MaterialBlendType.Biome, biomePreset);
        }
        #endregion

        #region 繝悶Ξ繝ｳ繝牙・逅・
        /// <summary>
        /// 繝悶Ξ繝ｳ繝牙・逅・Γ繧､繝ｳ繧ｳ繝ｫ繝ｼ繝√Φ
        /// </summary>
        private IEnumerator BlendProcessingCoroutine()
        {
            while (enableDynamicBlending)
            {
                yield return new WaitForSeconds(updateInterval);
                
                // 繧｢繧ｯ繝・ぅ繝悶↑繝悶Ξ繝ｳ繝峨ｒ譖ｴ譁ｰ
                UpdateActiveBlends();
                
                // 螳御ｺ・＠縺溘ヶ繝ｬ繝ｳ繝峨ｒ繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・
                CleanupCompletedBlends();
            }
        }
        
        /// <summary>
        /// 繝悶Ξ繝ｳ繝峨Μ繧ｯ繧ｨ繧ｹ繝医ｒ蜃ｦ逅・
        /// </summary>
        private void ProcessBlendRequests()
        {
            frameStartTime = Time.realtimeSinceStartup;
            blendsProcessedThisFrame = 0;
            
            while (blendRequestQueue.Count > 0 && blendsProcessedThisFrame < maxBlendsPerFrame)
            {
                // 繝輔Ξ繝ｼ繝�譎る俣蛻ｶ蠕｡
                if (enableFrameRateControl)
                {
                    float elapsedTime = (Time.realtimeSinceStartup - frameStartTime) * 1000f;
                    if (elapsedTime > targetFrameTime && blendsProcessedThisFrame >= minBlendsPerFrame)
                    {
                        break;
                    }
                }
                
                var request = blendRequestQueue.Dequeue();
                ProcessBlendRequest(request);
                blendsProcessedThisFrame++;
            }
        }
        
        /// <summary>
        /// 繝悶Ξ繝ｳ繝峨Μ繧ｯ繧ｨ繧ｹ繝医ｒ蜃ｦ逅・
        /// </summary>
        private void ProcessBlendRequest(MaterialBlendRequest request)
        {
            if (request.tile == null || request.tile.tileObject == null)
                return;
            
            // 譌｢蟄倥・繝悶Ξ繝ｳ繝峨ョ繝ｼ繧ｿ繧貞叙蠕励∪縺溘・菴懈・
            if (!activeMaterialBlends.ContainsKey(request.tile))
            {
                activeMaterialBlends[request.tile] = new MaterialBlendData(request.tile);
            }
            
            var blendData = activeMaterialBlends[request.tile];
            
            // 繝悶Ξ繝ｳ繝峨ち繧､繝励↓蠢懊§縺ｦ蜃ｦ逅・
            switch (request.blendType)
            {
                case MaterialBlendType.DistanceLOD:
                    ProcessDistanceLODBlend(blendData, (int)request.blendData);
                    break;
                    
                case MaterialBlendType.Environmental:
                    ProcessEnvironmentalBlend(blendData, (EnvironmentalConditions)request.blendData);
                    break;
                    
                case MaterialBlendType.Seasonal:
                    ProcessSeasonalBlend(blendData, (Season)request.blendData);
                    break;
                    
                case MaterialBlendType.Biome:
                    ProcessBiomeBlend(blendData, (BiomePreset)request.blendData);
                    break;
                    
                case MaterialBlendType.Texture:
                    ProcessTextureBlend(blendData, request.blendData);
                    break;
            }
        }
        
        /// <summary>
        /// 霍晞屬LOD繝悶Ξ繝ｳ繝峨ｒ蜃ｦ逅・
        /// </summary>
        private void ProcessDistanceLODBlend(MaterialBlendData blendData, int lodLevel)
        {
            if (lodLevel < 0 || lodLevel >= lodTextureScales.Length)
                return;
            
            float targetScale = lodTextureScales[lodLevel];
            float targetBlendSpeed = lodBlendSpeeds[lodLevel];
            
            // LOD繝悶Ξ繝ｳ繝峨ｒ髢句ｧ・
            StartBlendTransition(blendData, "LOD_Scale", blendData.currentLODScale, targetScale, targetBlendSpeed);
            
            blendData.targetLODLevel = lodLevel;
            blendData.currentLODScale = targetScale;
        }
        
        /// <summary>
        /// 迺ｰ蠅・ヶ繝ｬ繝ｳ繝峨ｒ蜃ｦ逅・
        /// </summary>
        private void ProcessEnvironmentalBlend(MaterialBlendData blendData, EnvironmentalConditions conditions)
        {
            // 貂ｩ蠎ｦ縺ｫ蝓ｺ縺･縺剰牡隱ｿ螟牙喧
            Color temperatureColor = CalculateTemperatureColor(conditions.temperature);
            StartBlendTransition(blendData, "Temperature_Color", blendData.currentTemperatureColor, temperatureColor, environmentalBlendSpeed);
            
            // 貉ｿ蠎ｦ縺ｫ蝓ｺ縺･縺丞ｽｩ蠎ｦ螟牙喧
            float moistureSaturation = CalculateMoistureSaturation(conditions.moisture);
            StartBlendTransition(blendData, "Moisture_Saturation", blendData.currentMoistureSaturation, moistureSaturation, environmentalBlendSpeed);
            
            // 譎ょ綾縺ｫ蝓ｺ縺･縺乗・蠎ｦ螟牙喧
            float timeBrightness = CalculateTimeBrightness(conditions.timeOfDay);
            StartBlendTransition(blendData, "Time_Brightness", blendData.currentTimeBrightness, timeBrightness, environmentalBlendSpeed);
            
            blendData.currentEnvironmentalConditions = conditions;
        }
        
        /// <summary>
        /// 蟄｣遽繝悶Ξ繝ｳ繝峨ｒ蜃ｦ逅・
        /// </summary>
        private void ProcessSeasonalBlend(MaterialBlendData blendData, Season targetSeason)
        {
            if (blendData.currentSeason == targetSeason)
                return;
            
            // 蟄｣遽螟牙喧縺ｮ濶ｲ隱ｿ繧定ｨ育ｮ・
            Color seasonalColor = CalculateSeasonalColor(targetSeason);
            float seasonalBrightness = CalculateSeasonalBrightness(targetSeason);
            float seasonalSaturation = CalculateSeasonalSaturation(targetSeason);
            
            // 蟄｣遽螟牙喧繝悶Ξ繝ｳ繝峨ｒ髢句ｧ・
            float transitionSpeed = 1f / seasonalTransitionDuration;
            
            StartBlendTransition(blendData, "Seasonal_Color", blendData.currentSeasonalColor, seasonalColor, transitionSpeed);
            StartBlendTransition(blendData, "Seasonal_Brightness", blendData.currentSeasonalBrightness, seasonalBrightness, transitionSpeed);
            StartBlendTransition(blendData, "Seasonal_Saturation", blendData.currentSeasonalSaturation, seasonalSaturation, transitionSpeed);
            
            blendData.targetSeason = targetSeason;
        }
        
        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝�繝悶Ξ繝ｳ繝峨ｒ蜃ｦ逅・
        /// </summary>
        private void ProcessBiomeBlend(MaterialBlendData blendData, BiomePreset biomePreset)
        {
            if (biomePreset.materialSettings == null)
                return;
            
            // 繝舌う繧ｪ繝ｼ繝�濶ｲ隱ｿ繧帝←逕ｨ
            Color biomeColor = biomePreset.materialSettings.terrainTint;
            Color biomeAmbient = biomePreset.materialSettings.ambientColor;
            
            StartBlendTransition(blendData, "Biome_Color", blendData.currentBiomeColor, biomeColor, blendTransitionSpeed);
            StartBlendTransition(blendData, "Biome_Ambient", blendData.currentBiomeAmbient, biomeAmbient, blendTransitionSpeed);
            
            blendData.currentBiomePreset = biomePreset;
        }
        
        /// <summary>
        /// 繝・け繧ｹ繝√Ε繝悶Ξ繝ｳ繝峨ｒ蜃ｦ逅・
        /// </summary>
        private void ProcessTextureBlend(MaterialBlendData blendData, object textureData)
        {
            // 繝・け繧ｹ繝√Ε繝悶Ξ繝ｳ繝峨・螳溯｣・
            // 隍・焚繝・け繧ｹ繝√Ε縺ｮ驥阪∩莉倥″繝悶Ξ繝ｳ繝・ぅ繝ｳ繧ｰ
        }
        #endregion

        #region 繝悶Ξ繝ｳ繝蛾・遘ｻ
        /// <summary>
        /// 繝悶Ξ繝ｳ繝蛾・遘ｻ繧帝幕蟋・
        /// </summary>
        private void StartBlendTransition(MaterialBlendData blendData, string propertyName, float fromValue, float toValue, float speed)
        {
            var transition = new BlendTransition
            {
                propertyName = propertyName,
                fromValue = fromValue,
                toValue = toValue,
                currentValue = fromValue,
                speed = speed,
                startTime = Time.time,
                isComplete = false
            };
            
            blendData.activeTransitions[propertyName] = transition;
        }
        
        /// <summary>
        /// 繝悶Ξ繝ｳ繝蛾・遘ｻ繧帝幕蟋具ｼ・olor迚茨ｼ・
        /// </summary>
        private void StartBlendTransition(MaterialBlendData blendData, string propertyName, Color fromColor, Color toColor, float speed)
        {
            var transition = new ColorBlendTransition
            {
                propertyName = propertyName,
                fromColor = fromColor,
                toColor = toColor,
                currentColor = fromColor,
                speed = speed,
                startTime = Time.time,
                isComplete = false
            };
            
            blendData.activeColorTransitions[propertyName] = transition;
        }
        
        /// <summary>
        /// 繧｢繧ｯ繝・ぅ繝悶↑繝悶Ξ繝ｳ繝峨ｒ譖ｴ譁ｰ
        /// </summary>
        private void UpdateActiveBlends()
        {
            foreach (var kvp in activeMaterialBlends.ToArray())
            {
                var tile = kvp.Key;
                var blendData = kvp.Value;
                
                if (tile == null || tile.tileObject == null)
                {
                    activeMaterialBlends.Remove(tile);
                    continue;
                }
                
                UpdateBlendTransitions(blendData);
                ApplyBlendToMaterial(tile, blendData);
            }
        }
        
        /// <summary>
        /// 繝悶Ξ繝ｳ繝蛾・遘ｻ繧呈峩譁ｰ
        /// </summary>
        private void UpdateBlendTransitions(MaterialBlendData blendData)
        {
            float deltaTime = Time.deltaTime;
            
            // Float蛟､縺ｮ驕ｷ遘ｻ繧呈峩譁ｰ
            foreach (var kvp in blendData.activeTransitions.ToArray())
            {
                var transition = kvp.Value;
                
                if (!transition.isComplete)
                {
                    float progress = (Time.time - transition.startTime) * transition.speed;
                    progress = blendCurve.Evaluate(Mathf.Clamp01(progress));
                    
                    transition.currentValue = Mathf.Lerp(transition.fromValue, transition.toValue, progress);
                    
                    if (progress >= 1f)
                    {
                        transition.currentValue = transition.toValue;
                        transition.isComplete = true;
                    }
                    
                    blendData.activeTransitions[kvp.Key] = transition;
                }
            }
            
            // Color蛟､縺ｮ驕ｷ遘ｻ繧呈峩譁ｰ
            foreach (var kvp in blendData.activeColorTransitions.ToArray())
            {
                var transition = kvp.Value;
                
                if (!transition.isComplete)
                {
                    float progress = (Time.time - transition.startTime) * transition.speed;
                    progress = blendCurve.Evaluate(Mathf.Clamp01(progress));
                    
                    transition.currentColor = Color.Lerp(transition.fromColor, transition.toColor, progress);
                    
                    if (progress >= 1f)
                    {
                        transition.currentColor = transition.toColor;
                        transition.isComplete = true;
                    }
                    
                    blendData.activeColorTransitions[kvp.Key] = transition;
                }
            }
        }
        
        /// <summary>
        /// 繝悶Ξ繝ｳ繝峨ｒ繝槭ユ繝ｪ繧｢繝ｫ縺ｫ驕ｩ逕ｨ
        /// </summary>
        private void ApplyBlendToMaterial(TerrainTile tile, MaterialBlendData blendData)
        {
            var meshRenderer = tile.tileObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null || meshRenderer.material == null)
                return;
            
            var material = meshRenderer.material;
            
            // LOD繧ｹ繧ｱ繝ｼ繝ｫ繧帝←逕ｨ
            if (blendData.activeTransitions.ContainsKey("LOD_Scale"))
            {
                float scale = blendData.activeTransitions["LOD_Scale"].currentValue;
                material.mainTextureScale = Vector2.one * scale;
            }
            
            // 濶ｲ隱ｿ螟牙喧繧帝←逕ｨ
            Color finalColor = Color.white;
            
            if (blendData.activeColorTransitions.ContainsKey("Temperature_Color"))
            {
                finalColor *= blendData.activeColorTransitions["Temperature_Color"].currentColor;
            }
            
            if (blendData.activeColorTransitions.ContainsKey("Seasonal_Color"))
            {
                finalColor *= blendData.activeColorTransitions["Seasonal_Color"].currentColor;
            }
            
            if (blendData.activeColorTransitions.ContainsKey("Biome_Color"))
            {
                finalColor *= blendData.activeColorTransitions["Biome_Color"].currentColor;
            }
            
            material.color = finalColor;
            
            // 譏主ｺｦ繝ｻ蠖ｩ蠎ｦ隱ｿ謨ｴ
            if (blendData.activeTransitions.ContainsKey("Time_Brightness"))
            {
                float brightness = blendData.activeTransitions["Time_Brightness"].currentValue;
                material.SetFloat("_Brightness", brightness);
            }
            
            if (blendData.activeTransitions.ContainsKey("Moisture_Saturation"))
            {
                float saturation = blendData.activeTransitions["Moisture_Saturation"].currentValue;
                material.SetFloat("_Saturation", saturation);
            }
        }
        #endregion

        #region 迺ｰ蠅・峩譁ｰ
        /// <summary>
        /// 迺ｰ蠅・峩譁ｰ繧ｳ繝ｫ繝ｼ繝√Φ
        /// </summary>
        private IEnumerator EnvironmentalUpdateCoroutine()
        {
            while (enableEnvironmentalBlending)
            {
                yield return new WaitForSeconds(updateInterval * 2f);
                
                if (playerTransform != null)
                {
                    UpdateEnvironmentalConditions();
                }
            }
        }
        
        /// <summary>
        /// 迺ｰ蠅・擅莉ｶ繧呈峩譁ｰ
        /// </summary>
        private void UpdateEnvironmentalConditions()
        {
            var currentConditions = GetCurrentEnvironmentalConditions();
            
            // 繝励Ξ繧､繝､繝ｼ蜻ｨ霎ｺ縺ｮ繧ｿ繧､繝ｫ縺ｫ迺ｰ蠅・､牙喧繧帝←逕ｨ
            foreach (var kvp in activeMaterialBlends.ToArray())
            {
                var tile = kvp.Key;
                
                if (tile == null || tile.tileObject == null)
                {
                    activeMaterialBlends.Remove(tile);
                    continue;
                }
                
                float distance = CalculateDistanceToPlayer(tile);
                if (distance <= updateRadius)
                {
                    ApplyEnvironmentalBlend(tile, currentConditions);
                }
            }
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
                timeOfDay = GetCurrentTimeOfDay(),
                windStrength = GetCurrentWindStrength(),
                precipitation = GetCurrentPrecipitation()
            };
        }
        #endregion

        #region 險育ｮ励Θ繝ｼ繝・ぅ繝ｪ繝・ぅ
        /// <summary>
        /// 繝悶Ξ繝ｳ繝牙━蜈亥ｺｦ繧定ｨ育ｮ・
        /// </summary>
        private int CalculateBlendPriority(TerrainTile tile)
        {
            float distance = CalculateDistanceToPlayer(tile);
            
            if (distance < 500f)
                return 3; // 鬮伜━蜈亥ｺｦ
            else if (distance < 1000f)
                return 2; // 荳ｭ蜆ｪ蜈亥ｺｦ
            else if (distance < 2000f)
                return 1; // 菴主━蜈亥ｺｦ
            else
                return 0; // 譛菴主━蜈亥ｺｦ
        }
        
        /// <summary>
        /// 繝励Ξ繧､繝､繝ｼ縺九ｉ縺ｮ霍晞屬繧定ｨ育ｮ・
        /// </summary>
        private float CalculateDistanceToPlayer(TerrainTile tile)
        {
            if (playerTransform == null)
                return float.MaxValue;
            
            return Vector3.Distance(playerTransform.position, tile.worldPosition);
        }
        
        /// <summary>
        /// LOD繝ｬ繝吶Ν繧定ｨ育ｮ・
        /// </summary>
        private int CalculateLODLevel(float distance)
        {
            for (int i = 0; i < lodDistances.Length; i++)
            {
                if (distance <= lodDistances[i])
                    return i;
            }
            return lodDistances.Length - 1;
        }
        
        /// <summary>
        /// 貂ｩ蠎ｦ濶ｲ繧定ｨ育ｮ・
        /// </summary>
        private Color CalculateTemperatureColor(float temperature)
        {
            return Color.Lerp(Color.blue, Color.red, temperature);
        }
        
        /// <summary>
        /// 貉ｿ蠎ｦ蠖ｩ蠎ｦ繧定ｨ育ｮ・
        /// </summary>
        private float CalculateMoistureSaturation(float moisture)
        {
            return Mathf.Lerp(0.5f, 1.2f, moisture);
        }
        
        /// <summary>
        /// 譎ょ綾譏主ｺｦ繧定ｨ育ｮ・
        /// </summary>
        private float CalculateTimeBrightness(float timeOfDay)
        {
            // 0.25 = 譛・譎・ 0.5 = 豁｣蜊・ 0.75 = 螟墓婿6譎・ 0.0/1.0 = 豺ｱ螟・
            if (timeOfDay < 0.25f || timeOfDay > 0.75f)
                return 0.3f; // 螟憺俣
            else if (timeOfDay >= 0.4f && timeOfDay <= 0.6f)
                return 1.2f; // 譏ｼ髢・
            else
                return 0.8f; // 譛晏､・
        }
        
        /// <summary>
        /// 蟄｣遽濶ｲ繧定ｨ育ｮ・
        /// </summary>
        private Color CalculateSeasonalColor(Season season)
        {
            switch (season)
            {
                case Season.Spring: return Color.Lerp(Color.white, Color.green, 0.2f);
                case Season.Summer: return Color.Lerp(Color.white, Color.yellow, 0.1f);
                case Season.Autumn: return Color.Lerp(Color.white, new Color(1f, 0.8f, 0.4f), 0.3f);
                case Season.Winter: return Color.Lerp(Color.white, Color.cyan, 0.2f);
                default: return Color.white;
            }
        }
        
        /// <summary>
        /// 蟄｣遽譏主ｺｦ繧定ｨ育ｮ・
        /// </summary>
        private float CalculateSeasonalBrightness(Season season)
        {
            switch (season)
            {
                case Season.Spring: return 1.1f;
                case Season.Summer: return 1.3f;
                case Season.Autumn: return 0.9f;
                case Season.Winter: return 0.7f;
                default: return 1f;
            }
        }
        
        /// <summary>
        /// 蟄｣遽蠖ｩ蠎ｦ繧定ｨ育ｮ・
        /// </summary>
        private float CalculateSeasonalSaturation(Season season)
        {
            switch (season)
            {
                case Season.Spring: return 1.2f;
                case Season.Summer: return 1.1f;
                case Season.Autumn: return 1.3f;
                case Season.Winter: return 0.8f;
                default: return 1f;
            }
        }
        #endregion

        #region 迺ｰ蠅・ョ繝ｼ繧ｿ蜿門ｾ・
        private Season GetCurrentSeason()
        {
            // 邁｡譏灘ｮ溯｣・ｼ壽凾髢薙・繝ｼ繧ｹ縺ｮ蟄｣遽螟牙喧
            float seasonTime = (Time.time / 300f) % 4f; // 5蛻・〒1蟄｣遽
            return (Season)Mathf.FloorToInt(seasonTime);
        }
        
        private float GetCurrentTemperature()
        {
            Season season = GetCurrentSeason();
            float baseTemp = season == Season.Summer ? 0.8f : season == Season.Winter ? 0.2f : 0.5f;
            float timeVariation = Mathf.Sin(Time.time * 0.1f) * 0.1f;
            return Mathf.Clamp01(baseTemp + timeVariation);
        }
        
        private float GetCurrentMoisture()
        {
            return Mathf.Clamp01(0.5f + Mathf.Sin(Time.time * 0.05f) * 0.3f);
        }
        
        private float GetCurrentTimeOfDay()
        {
            return (Time.time * 0.01f) % 1f; // 100遘偵〒1譌･
        }
        
        private float GetCurrentWindStrength()
        {
            return Mathf.Clamp01(0.3f + Mathf.Sin(Time.time * 0.2f) * 0.4f);
        }
        
        private float GetCurrentPrecipitation()
        {
            return Mathf.Clamp01(Mathf.Max(0f, Mathf.Sin(Time.time * 0.03f) - 0.7f) * 3f);
        }
        #endregion

        #region 繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・
        /// <summary>
        /// 螳御ｺ・＠縺溘ヶ繝ｬ繝ｳ繝峨ｒ繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・
        /// </summary>
        private void CleanupCompletedBlends()
        {
            foreach (var kvp in activeMaterialBlends.ToArray())
            {
                var blendData = kvp.Value;
                
                // 螳御ｺ・＠縺滄・遘ｻ繧貞炎髯､
                var completedTransitions = blendData.activeTransitions.Where(t => t.Value.isComplete).ToArray();
                foreach (var transition in completedTransitions)
                {
                    blendData.activeTransitions.Remove(transition.Key);
                }
                
                var completedColorTransitions = blendData.activeColorTransitions.Where(t => t.Value.isComplete).ToArray();
                foreach (var transition in completedColorTransitions)
                {
                    blendData.activeColorTransitions.Remove(transition.Key);
                }
                
                // 繧｢繧ｯ繝・ぅ繝悶↑驕ｷ遘ｻ縺後↑縺・�ｴ蜷医・蜑企勁
                if (blendData.activeTransitions.Count == 0 && blendData.activeColorTransitions.Count == 0)
                {
                    activeMaterialBlends.Remove(kvp.Key);
                }
            }
        }
        
        /// <summary>
        /// 縺吶∋縺ｦ縺ｮ繝悶Ξ繝ｳ繝峨ｒ繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・
        /// </summary>
        private void CleanupAllBlends()
        {
            activeMaterialBlends.Clear();
            blendRequestQueue.Clear();
        }
        #endregion
    }
}
