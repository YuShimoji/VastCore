using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Vastcore.Utils;


namespace Vastcore.Generation
{
    /// <summary>
    /// 動的マテリアルブレンチE��ングシスチE��
    /// 要汁E.1: 褁E��チE��スチャの自然なブレンチE��ングとリアルタイム環墁E��化の反映
    /// </summary>
    public class DynamicMaterialBlendingSystem : MonoBehaviour
    {
        #region 設定パラメータ
        [Header("ブレンチE��ング設宁E)]
        public bool enableDynamicBlending = true;
        public float blendTransitionSpeed = 2f;
        public int maxSimultaneousBlends = 4;
        public AnimationCurve blendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("距離ベ�EスLOD")]
        public bool enableDistanceLOD = true;
        public float[] lodDistances = { 500f, 1000f, 2000f, 4000f };
        public float[] lodTextureScales = { 1f, 0.75f, 0.5f, 0.25f };
        public float[] lodBlendSpeeds = { 1f, 0.8f, 0.6f, 0.4f };
        
        [Header("リアルタイム更新")]
        public bool enableRealtimeUpdates = true;
        public float updateInterval = 0.1f;
        public int maxUpdatesPerFrame = 5;
        public float updateRadius = 1500f;
        
        [Header("環墁E��化対忁E)]
        public bool enableEnvironmentalBlending = true;
        public float environmentalBlendSpeed = 1f;
        public bool enableSeasonalTransitions = true;
        public float seasonalTransitionDuration = 10f;
        
        [Header("パフォーマンス制御")]
        public bool enableFrameRateControl = true;
        public float targetFrameTime = 16.67f; // 60FPS
        public int minBlendsPerFrame = 1;
        public int maxBlendsPerFrame = 10;
        #endregion

        #region プライベ�Eト変数
        private Dictionary<TerrainTile, MaterialBlendData> activeMaterialBlends = new Dictionary<TerrainTile, MaterialBlendData>();
        private Queue<MaterialBlendRequest> blendRequestQueue = new Queue<MaterialBlendRequest>();
        private Transform playerTransform;
        private TerrainTexturingSystem texturingSystem;
        
        // パフォーマンス統訁E
        private float lastUpdateTime = 0f;
        private int blendsProcessedThisFrame = 0;
        private float frameStartTime = 0f;
        
        // コルーチン管琁E
        private Coroutine blendProcessingCoroutine;
        private Coroutine environmentalUpdateCoroutine;
        #endregion

        #region Unity イベンチE
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
        /// マテリアルブレンドを更新
        /// </summary>
        private void UpdateMaterialBlends()
        {
            if (!enableDynamicBlending)
                return;

            // アクチE��ブなブレンドを更新
            UpdateActiveBlends();

            // 完亁E��たブレンドをクリーンアチE�E
            CleanupCompletedBlends();

            // フレームレート制御
            if (enableFrameRateControl)
            {
                float elapsedTime = (Time.realtimeSinceStartup - frameStartTime) * 1000f;
                if (elapsedTime > targetFrameTime)
                {
                    // 次のフレームまで征E��E
                    return;
                }
            }
        }

        #region 初期匁E
        /// <summary>
        /// ブレンチE��ングシスチE��を�E期化
        /// </summary>
        private void InitializeBlendingSystem()
        {
            Debug.Log("Initializing DynamicMaterialBlendingSystem...");
            
            // 忁E��なコンポ�Eネントを取征E
            texturingSystem = GetComponent<TerrainTexturingSystem>();
            if (texturingSystem == null)
            {
                texturingSystem = gameObject.AddComponent<TerrainTexturingSystem>();
            }
            
            // プレイヤーTransformを取征E
            var playerController = FindFirstObjectByType<AdvancedPlayerController>();
            if (playerController != null)
            {
                playerTransform = playerController.transform;
            }
            
            // コルーチンを開姁E
            StartBlendProcessing();
            
            if (enableEnvironmentalBlending)
            {
                StartEnvironmentalUpdates();
            }
            
            Debug.Log("DynamicMaterialBlendingSystem initialized successfully");
        }
        
        /// <summary>
        /// ブレンド�E琁E��ルーチンを開姁E
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
        /// 環墁E��新コルーチンを開姁E
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

        #region パブリチE��API
        /// <summary>
        /// マテリアルブレンドをリクエスチE
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
        /// 距離ベ�EスLODブレンドを適用
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
        /// 環墁E��化ブレンドを適用
        /// </summary>
        public void ApplyEnvironmentalBlend(TerrainTile tile, EnvironmentalConditions conditions)
        {
            if (!enableEnvironmentalBlending || tile == null)
                return;
            
            RequestMaterialBlend(tile, MaterialBlendType.Environmental, conditions);
        }
        
        /// <summary>
        /// 季節変化ブレンドを適用
        /// </summary>
        public void ApplySeasonalBlend(TerrainTile tile, Season targetSeason)
        {
            if (!enableSeasonalTransitions || tile == null)
                return;
            
            RequestMaterialBlend(tile, MaterialBlendType.Seasonal, targetSeason);
        }
        
        /// <summary>
        /// バイオームブレンドを適用
        /// </summary>
        public void ApplyBiomeBlend(TerrainTile tile, BiomePreset biomePreset)
        {
            if (tile == null || biomePreset == null)
                return;
            
            RequestMaterialBlend(tile, MaterialBlendType.Biome, biomePreset);
        }
        #endregion

        #region ブレンド�E琁E
        /// <summary>
        /// ブレンド�E琁E��インコルーチン
        /// </summary>
        private IEnumerator BlendProcessingCoroutine()
        {
            while (enableDynamicBlending)
            {
                yield return new WaitForSeconds(updateInterval);
                
                // アクチE��ブなブレンドを更新
                UpdateActiveBlends();
                
                // 完亁E��たブレンドをクリーンアチE�E
                CleanupCompletedBlends();
            }
        }
        
        /// <summary>
        /// ブレンドリクエストを処琁E
        /// </summary>
        private void ProcessBlendRequests()
        {
            frameStartTime = Time.realtimeSinceStartup;
            blendsProcessedThisFrame = 0;
            
            while (blendRequestQueue.Count > 0 && blendsProcessedThisFrame < maxBlendsPerFrame)
            {
                // フレーム時間制御
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
        /// ブレンドリクエストを処琁E
        /// </summary>
        private void ProcessBlendRequest(MaterialBlendRequest request)
        {
            if (request.tile == null || request.tile.tileObject == null)
                return;
            
            // 既存�Eブレンドデータを取得また�E作�E
            if (!activeMaterialBlends.ContainsKey(request.tile))
            {
                activeMaterialBlends[request.tile] = new MaterialBlendData(request.tile);
            }
            
            var blendData = activeMaterialBlends[request.tile];
            
            // ブレンドタイプに応じて処琁E
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
        /// 距離LODブレンドを処琁E
        /// </summary>
        private void ProcessDistanceLODBlend(MaterialBlendData blendData, int lodLevel)
        {
            if (lodLevel < 0 || lodLevel >= lodTextureScales.Length)
                return;
            
            float targetScale = lodTextureScales[lodLevel];
            float targetBlendSpeed = lodBlendSpeeds[lodLevel];
            
            // LODブレンドを開姁E
            StartBlendTransition(blendData, "LOD_Scale", blendData.currentLODScale, targetScale, targetBlendSpeed);
            
            blendData.targetLODLevel = lodLevel;
            blendData.currentLODScale = targetScale;
        }
        
        /// <summary>
        /// 環墁E��レンドを処琁E
        /// </summary>
        private void ProcessEnvironmentalBlend(MaterialBlendData blendData, EnvironmentalConditions conditions)
        {
            // 温度に基づく色調変化
            Color temperatureColor = CalculateTemperatureColor(conditions.temperature);
            StartBlendTransition(blendData, "Temperature_Color", blendData.currentTemperatureColor, temperatureColor, environmentalBlendSpeed);
            
            // 湿度に基づく彩度変化
            float moistureSaturation = CalculateMoistureSaturation(conditions.moisture);
            StartBlendTransition(blendData, "Moisture_Saturation", blendData.currentMoistureSaturation, moistureSaturation, environmentalBlendSpeed);
            
            // 時刻に基づく�E度変化
            float timeBrightness = CalculateTimeBrightness(conditions.timeOfDay);
            StartBlendTransition(blendData, "Time_Brightness", blendData.currentTimeBrightness, timeBrightness, environmentalBlendSpeed);
            
            blendData.currentEnvironmentalConditions = conditions;
        }
        
        /// <summary>
        /// 季節ブレンドを処琁E
        /// </summary>
        private void ProcessSeasonalBlend(MaterialBlendData blendData, Season targetSeason)
        {
            if (blendData.currentSeason == targetSeason)
                return;
            
            // 季節変化の色調を計箁E
            Color seasonalColor = CalculateSeasonalColor(targetSeason);
            float seasonalBrightness = CalculateSeasonalBrightness(targetSeason);
            float seasonalSaturation = CalculateSeasonalSaturation(targetSeason);
            
            // 季節変化ブレンドを開姁E
            float transitionSpeed = 1f / seasonalTransitionDuration;
            
            StartBlendTransition(blendData, "Seasonal_Color", blendData.currentSeasonalColor, seasonalColor, transitionSpeed);
            StartBlendTransition(blendData, "Seasonal_Brightness", blendData.currentSeasonalBrightness, seasonalBrightness, transitionSpeed);
            StartBlendTransition(blendData, "Seasonal_Saturation", blendData.currentSeasonalSaturation, seasonalSaturation, transitionSpeed);
            
            blendData.targetSeason = targetSeason;
        }
        
        /// <summary>
        /// バイオームブレンドを処琁E
        /// </summary>
        private void ProcessBiomeBlend(MaterialBlendData blendData, BiomePreset biomePreset)
        {
            if (biomePreset.materialSettings == null)
                return;
            
            // バイオーム色調を適用
            Color biomeColor = biomePreset.materialSettings.terrainTint;
            Color biomeAmbient = biomePreset.materialSettings.ambientColor;
            
            StartBlendTransition(blendData, "Biome_Color", blendData.currentBiomeColor, biomeColor, blendTransitionSpeed);
            StartBlendTransition(blendData, "Biome_Ambient", blendData.currentBiomeAmbient, biomeAmbient, blendTransitionSpeed);
            
            blendData.currentBiomePreset = biomePreset;
        }
        
        /// <summary>
        /// チE��スチャブレンドを処琁E
        /// </summary>
        private void ProcessTextureBlend(MaterialBlendData blendData, object textureData)
        {
            // チE��スチャブレンド�E実裁E
            // 褁E��チE��スチャの重み付きブレンチE��ング
        }
        #endregion

        #region ブレンド�E移
        /// <summary>
        /// ブレンド�E移を開姁E
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
        /// ブレンド�E移を開始！Eolor版！E
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
        /// アクチE��ブなブレンドを更新
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
        /// ブレンド�E移を更新
        /// </summary>
        private void UpdateBlendTransitions(MaterialBlendData blendData)
        {
            float deltaTime = Time.deltaTime;
            
            // Float値の遷移を更新
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
            
            // Color値の遷移を更新
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
        /// ブレンドをマテリアルに適用
        /// </summary>
        private void ApplyBlendToMaterial(TerrainTile tile, MaterialBlendData blendData)
        {
            var meshRenderer = tile.tileObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null || meshRenderer.material == null)
                return;
            
            var material = meshRenderer.material;
            
            // LODスケールを適用
            if (blendData.activeTransitions.ContainsKey("LOD_Scale"))
            {
                float scale = blendData.activeTransitions["LOD_Scale"].currentValue;
                material.mainTextureScale = Vector2.one * scale;
            }
            
            // 色調変化を適用
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
            
            // 明度・彩度調整
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

        #region 環墁E��新
        /// <summary>
        /// 環墁E��新コルーチン
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
        /// 環墁E��件を更新
        /// </summary>
        private void UpdateEnvironmentalConditions()
        {
            var currentConditions = GetCurrentEnvironmentalConditions();
            
            // プレイヤー周辺のタイルに環墁E��化を適用
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
        /// 現在の環墁E��件を取征E
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

        #region 計算ユーチE��リチE��
        /// <summary>
        /// ブレンド優先度を計箁E
        /// </summary>
        private int CalculateBlendPriority(TerrainTile tile)
        {
            float distance = CalculateDistanceToPlayer(tile);
            
            if (distance < 500f)
                return 3; // 高優先度
            else if (distance < 1000f)
                return 2; // 中優先度
            else if (distance < 2000f)
                return 1; // 低優先度
            else
                return 0; // 最低優先度
        }
        
        /// <summary>
        /// プレイヤーからの距離を計箁E
        /// </summary>
        private float CalculateDistanceToPlayer(TerrainTile tile)
        {
            if (playerTransform == null)
                return float.MaxValue;
            
            return Vector3.Distance(playerTransform.position, tile.worldPosition);
        }
        
        /// <summary>
        /// LODレベルを計箁E
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
        /// 温度色を計箁E
        /// </summary>
        private Color CalculateTemperatureColor(float temperature)
        {
            return Color.Lerp(Color.blue, Color.red, temperature);
        }
        
        /// <summary>
        /// 湿度彩度を計箁E
        /// </summary>
        private float CalculateMoistureSaturation(float moisture)
        {
            return Mathf.Lerp(0.5f, 1.2f, moisture);
        }
        
        /// <summary>
        /// 時刻明度を計箁E
        /// </summary>
        private float CalculateTimeBrightness(float timeOfDay)
        {
            // 0.25 = 朁E晁E 0.5 = 正十E 0.75 = 夕方6晁E 0.0/1.0 = 深夁E
            if (timeOfDay < 0.25f || timeOfDay > 0.75f)
                return 0.3f; // 夜間
            else if (timeOfDay >= 0.4f && timeOfDay <= 0.6f)
                return 1.2f; // 昼閁E
            else
                return 0.8f; // 朝夁E
        }
        
        /// <summary>
        /// 季節色を計箁E
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
        /// 季節明度を計箁E
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
        /// 季節彩度を計箁E
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

        #region 環墁E��ータ取征E
        private Season GetCurrentSeason()
        {
            // 簡易実裁E��時間�Eースの季節変化
            float seasonTime = (Time.time / 300f) % 4f; // 5刁E��1季節
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
            return (Time.time * 0.01f) % 1f; // 100秒で1日
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

        #region クリーンアチE�E
        /// <summary>
        /// 完亁E��たブレンドをクリーンアチE�E
        /// </summary>
        private void CleanupCompletedBlends()
        {
            foreach (var kvp in activeMaterialBlends.ToArray())
            {
                var blendData = kvp.Value;
                
                // 完亁E��た�E移を削除
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
                
                // アクチE��ブな遷移がなぁE��合�E削除
                if (blendData.activeTransitions.Count == 0 && blendData.activeColorTransitions.Count == 0)
                {
                    activeMaterialBlends.Remove(kvp.Key);
                }
            }
        }
        
        /// <summary>
        /// すべてのブレンドをクリーンアチE�E
        /// </summary>
        private void CleanupAllBlends()
        {
            activeMaterialBlends.Clear();
            blendRequestQueue.Clear();
        }
        #endregion
    }
}
