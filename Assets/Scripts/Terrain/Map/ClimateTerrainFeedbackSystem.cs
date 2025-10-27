using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Generation
{
    /// <summary>
    /// 気候・地形フィードバックシステム
    /// 地形が気候に与える影響と気候が地形に与える影響を管理
    /// 要求: 1.4 リアルタイム更新
    /// </summary>
    public class ClimateTerrainFeedbackSystem : MonoBehaviour
    {
        [Header("フィードバック設定")]
        public bool enableTerrainToClimate = true;    // 地形→気候の影響
        public bool enableClimateToTerrain = true;    // 気候→地形の影響
        public bool enableVegetationFeedback = true;  // 植生フィードバック
        public float feedbackUpdateInterval = 5f;     // フィードバック更新間隔
        
        [Header("雨陰効果設定")]
        public float rainShadowDistance = 2000f;      // 雨陰効果の影響距離
        public float rainShadowIntensity = 0.8f;      // 雨陰効果の強度
        public float orographicLiftHeight = 500f;     // 地形性上昇の高度閾値
        
        [Header("地形変化設定")]
        public float erosionRate = 0.001f;            // 浸食率
        public float depositionRate = 0.0005f;        // 堆積率
        public float weatheringRate = 0.0002f;        // 風化率
        public float vegetationStabilization = 0.7f;  // 植生による地形安定化
        
        [Header("植生システム")]
        public float vegetationGrowthRate = 0.01f;    // 植生成長率
        public float vegetationDecayRate = 0.005f;    // 植生減衰率
        public float temperatureToleranceRange = 20f; // 植生の温度耐性範囲
        public float moistureRequirement = 500f;      // 植生の最低湿度要求
        
        [Header("長期変化設定")]
        public bool enableLongTermChanges = true;     // 長期変化の有効化
        public float longTermChangeRate = 0.0001f;    // 長期変化率
        public float climateMemoryDuration = 3600f;   // 気候記憶期間（秒）
        
        [Header("デバッグ設定")]
        public bool showFeedbackVisualization = false;
        public bool logFeedbackEvents = false;
        public int visualizationSamples = 100;
        
        // システム参照
        private ClimateSystem climateSystem;
        private RuntimeTerrainManager terrainManager;
        private BiomeTerrainModifier biomeModifier;
        private NaturalTerrainFeatures terrainFeatures;
        
        // フィードバックデータ
        private Dictionary<Vector2Int, TerrainClimateData> feedbackCache;
        private Dictionary<Vector2Int, VegetationData> vegetationMap;
        private Dictionary<Vector2Int, ErosionData> erosionMap;
        
        // 長期変化追跡
        private Queue<ClimateHistoryEntry> climateHistory;
        private float lastFeedbackUpdate;
        
        // イベント
        public System.Action<Vector3, float> OnTerrainErosion;
        public System.Action<Vector3, float> OnVegetationChange;
        public System.Action<Vector3, ClimateData> OnClimateModification;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            feedbackCache = new Dictionary<Vector2Int, TerrainClimateData>();
            vegetationMap = new Dictionary<Vector2Int, VegetationData>();
            erosionMap = new Dictionary<Vector2Int, ErosionData>();
            climateHistory = new Queue<ClimateHistoryEntry>();
        }
        
        private void Start()
        {
            Initialize();
        }
        
        private void Update()
        {
            if (Time.time - lastFeedbackUpdate > feedbackUpdateInterval)
            {
                UpdateFeedbackSystem();
                lastFeedbackUpdate = Time.time;
            }
        }
        
        private void OnDrawGizmos()
        {
            if (showFeedbackVisualization)
            {
                DrawFeedbackVisualization();
            }
        }
        
        #endregion
        
        #region 初期化
        
        /// <summary>
        /// フィードバックシステムを初期化
        /// </summary>
        public void Initialize()
        {
            try
            {
                // システム参照を取得
                climateSystem = FindFirstObjectByType<ClimateSystem>();
                terrainManager = FindFirstObjectByType<RuntimeTerrainManager>();
                biomeModifier = FindFirstObjectByType<BiomeTerrainModifier>();
                
                if (climateSystem == null)
                {
                    Debug.LogWarning("ClimateSystem not found. Some feedback features may not work.");
                }
                
                // 初期フィードバックデータを生成
                InitializeFeedbackData();
                
                Debug.Log("ClimateTerrainFeedbackSystem initialized successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ClimateTerrainFeedbackSystem initialization failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// 初期フィードバックデータを生成
        /// </summary>
        private void InitializeFeedbackData()
        {
            // 初期植生マップを生成
            GenerateInitialVegetationMap();
            
            // 初期浸食データを生成
            GenerateInitialErosionData();
        }
        
        /// <summary>
        /// 初期植生マップを生成
        /// </summary>
        private void GenerateInitialVegetationMap()
        {
            int mapSize = 100;
            float worldSize = 10000f;
            float step = worldSize / mapSize;
            
            for (int x = 0; x < mapSize; x++)
            {
                for (int y = 0; y < mapSize; y++)
                {
                    Vector2Int key = new Vector2Int(x, y);
                    Vector3 worldPos = new Vector3(
                        (x - mapSize * 0.5f) * step,
                        0f,
                        (y - mapSize * 0.5f) * step
                    );
                    
                    // 気候データから初期植生密度を計算
                    ClimateData climate = climateSystem?.GetClimateAt(worldPos) ?? ClimateData.Default;
                    float vegetationDensity = CalculateVegetationDensity(climate);
                    
                    vegetationMap[key] = new VegetationData
                    {
                        density = vegetationDensity,
                        type = DetermineVegetationType(climate),
                        stability = vegetationDensity * 0.8f,
                        lastUpdateTime = Time.time
                    };
                }
            }
        }
        
        /// <summary>
        /// 初期浸食データを生成
        /// </summary>
        private void GenerateInitialErosionData()
        {
            foreach (var key in vegetationMap.Keys)
            {
                erosionMap[key] = new ErosionData
                {
                    waterErosion = 0f,
                    windErosion = 0f,
                    thermalErosion = 0f,
                    deposition = 0f,
                    lastUpdateTime = Time.time
                };
            }
        }
        
        #endregion
        
        #region フィードバック更新
        
        /// <summary>
        /// フィードバックシステムを更新
        /// </summary>
        private void UpdateFeedbackSystem()
        {
            // 気候履歴を更新
            UpdateClimateHistory();
            
            // 地形→気候フィードバック
            if (enableTerrainToClimate)
            {
                UpdateTerrainToClimateFeedback();
            }
            
            // 気候→地形フィードバック
            if (enableClimateToTerrain)
            {
                UpdateClimateToTerrainFeedback();
            }
            
            // 植生フィードバック
            if (enableVegetationFeedback)
            {
                UpdateVegetationFeedback();
            }
            
            // 長期変化
            if (enableLongTermChanges)
            {
                UpdateLongTermChanges();
            }
        }
        
        /// <summary>
        /// 気候履歴を更新
        /// </summary>
        private void UpdateClimateHistory()
        {
            if (climateSystem == null) return;
            
            // 現在の気候統計を記録
            var (avgTemp, avgMoisture, avgWindSpeed) = climateSystem.GetClimateStatistics();
            
            climateHistory.Enqueue(new ClimateHistoryEntry
            {
                timestamp = Time.time,
                averageTemperature = avgTemp,
                averageMoisture = avgMoisture,
                averageWindSpeed = avgWindSpeed
            });
            
            // 古い履歴を削除
            while (climateHistory.Count > 0 && 
                   Time.time - climateHistory.Peek().timestamp > climateMemoryDuration)
            {
                climateHistory.Dequeue();
            }
        }
        
        /// <summary>
        /// 地形→気候フィードバックを更新
        /// </summary>
        private void UpdateTerrainToClimateFeedback()
        {
            var keys = new List<Vector2Int>(feedbackCache.Keys);
            
            foreach (var key in keys)
            {
                Vector3 worldPos = GridKeyToWorldPosition(key);
                
                // 雨陰効果を計算
                ClimateData modifiedClimate = ApplyOrographicEffects(worldPos);
                
                // 地形による風向き変化
                modifiedClimate = ApplyTopographicWindEffects(modifiedClimate, worldPos);
                
                // 植生による気候緩和
                if (vegetationMap.ContainsKey(key))
                {
                    modifiedClimate = ApplyVegetationClimateEffects(modifiedClimate, vegetationMap[key]);
                }
                
                // フィードバックキャッシュを更新
                if (!feedbackCache.ContainsKey(key))
                {
                    feedbackCache[key] = new TerrainClimateData();
                }
                
                var feedbackData = feedbackCache[key];
                feedbackData.modifiedClimate = modifiedClimate;
                feedbackData.lastUpdateTime = Time.time;
                feedbackCache[key] = feedbackData;
                
                OnClimateModification?.Invoke(worldPos, modifiedClimate);
            }
        }
        
        /// <summary>
        /// 気候→地形フィードバックを更新
        /// </summary>
        private void UpdateClimateToTerrainFeedback()
        {
            var keys = new List<Vector2Int>(erosionMap.Keys);
            
            foreach (var key in keys)
            {
                Vector3 worldPos = GridKeyToWorldPosition(key);
                ClimateData climate = climateSystem?.GetClimateAt(worldPos) ?? ClimateData.Default;
                
                // 水による浸食
                float waterErosion = CalculateWaterErosion(climate, worldPos);
                
                // 風による浸食
                float windErosion = CalculateWindErosion(climate, worldPos);
                
                // 熱による風化
                float thermalWeathering = CalculateThermalWeathering(climate);
                
                // 植生による安定化
                float vegetationStabilization = 0f;
                if (vegetationMap.ContainsKey(key))
                {
                    vegetationStabilization = vegetationMap[key].stability * this.vegetationStabilization;
                }
                
                // 浸食データを更新
                var erosionData = erosionMap[key];
                erosionData.waterErosion += waterErosion * (1f - vegetationStabilization);
                erosionData.windErosion += windErosion * (1f - vegetationStabilization);
                erosionData.thermalErosion += thermalWeathering;
                erosionData.lastUpdateTime = Time.time;
                
                erosionMap[key] = erosionData;
                
                // 地形変化イベントを発火
                float totalErosion = erosionData.waterErosion + erosionData.windErosion + erosionData.thermalErosion;
                if (totalErosion > 0.01f)
                {
                    OnTerrainErosion?.Invoke(worldPos, totalErosion);
                }
            }
        }
        
        /// <summary>
        /// 植生フィードバックを更新
        /// </summary>
        private void UpdateVegetationFeedback()
        {
            var keys = new List<Vector2Int>(vegetationMap.Keys);
            
            foreach (var key in keys)
            {
                Vector3 worldPos = GridKeyToWorldPosition(key);
                ClimateData climate = climateSystem?.GetClimateAt(worldPos) ?? ClimateData.Default;
                
                var vegetationData = vegetationMap[key];
                float previousDensity = vegetationData.density;
                
                // 気候条件による植生成長/減衰
                float growthFactor = CalculateVegetationGrowthFactor(climate);
                
                if (growthFactor > 0f)
                {
                    // 成長
                    vegetationData.density += vegetationGrowthRate * growthFactor * Time.deltaTime;
                    vegetationData.density = Mathf.Min(vegetationData.density, 1f);
                }
                else
                {
                    // 減衰
                    vegetationData.density -= vegetationDecayRate * Mathf.Abs(growthFactor) * Time.deltaTime;
                    vegetationData.density = Mathf.Max(vegetationData.density, 0f);
                }
                
                // 植生タイプの変化
                VegetationType newType = DetermineVegetationType(climate);
                if (newType != vegetationData.type)
                {
                    // 植生タイプ変化時の密度調整
                    vegetationData.density *= 0.8f;
                    vegetationData.type = newType;
                }
                
                // 安定性の更新
                vegetationData.stability = Mathf.Lerp(vegetationData.stability, vegetationData.density, 0.1f);
                vegetationData.lastUpdateTime = Time.time;
                
                vegetationMap[key] = vegetationData;
                
                // 植生変化イベントを発火
                if (Mathf.Abs(vegetationData.density - previousDensity) > 0.01f)
                {
                    OnVegetationChange?.Invoke(worldPos, vegetationData.density);
                }
            }
        }
        
        /// <summary>
        /// 長期変化を更新
        /// </summary>
        private void UpdateLongTermChanges()
        {
            if (climateHistory.Count < 10) return; // 十分な履歴が必要
            
            // 気候トレンドを分析
            var trend = AnalyzeClimateTrend();
            
            // 長期的な地形変化を適用
            ApplyLongTermTerrainChanges(trend);
            
            // 長期的な植生変化を適用
            ApplyLongTermVegetationChanges(trend);
        }
        
        #endregion
        
        #region 地形効果計算
        
        /// <summary>
        /// 地形性効果を適用（雨陰効果など）
        /// </summary>
        private ClimateData ApplyOrographicEffects(Vector3 position)
        {
            ClimateData climate = climateSystem?.GetClimateAt(position) ?? ClimateData.Default;
            
            // 風上側の地形を調査
            Vector3 windDirection3D = new Vector3(climate.windDirection.x, 0f, climate.windDirection.y);
            
            // 複数の距離で地形高度をサンプリング
            float[] distances = { 500f, 1000f, 1500f, 2000f };
            float[] elevations = new float[distances.Length];
            
            for (int i = 0; i < distances.Length; i++)
            {
                Vector3 samplePos = position - windDirection3D * distances[i];
                elevations[i] = GetTerrainElevation(samplePos);
            }
            
            float currentElevation = GetTerrainElevation(position);
            
            // 地形性上昇による降水増加
            float orographicPrecipitation = 0f;
            for (int i = 0; i < elevations.Length; i++)
            {
                if (elevations[i] > currentElevation + orographicLiftHeight)
                {
                    float liftEffect = (elevations[i] - currentElevation) / 1000f;
                    orographicPrecipitation += liftEffect * (1f - i * 0.2f); // 距離による減衰
                }
            }
            
            // 雨陰効果による降水減少
            float rainShadowEffect = 0f;
            for (int i = 0; i < elevations.Length; i++)
            {
                if (elevations[i] > currentElevation + 200f)
                {
                    float shadowEffect = (elevations[i] - currentElevation) / 1000f;
                    rainShadowEffect += shadowEffect * rainShadowIntensity * (1f - i * 0.15f);
                }
            }
            
            // 気候データを修正
            climate.moisture += orographicPrecipitation * 300f;
            climate.moisture -= rainShadowEffect * 400f;
            climate.moisture = Mathf.Max(0f, climate.moisture);
            
            // 標高による温度低下
            climate.temperature -= currentElevation * 0.006f;
            
            return climate;
        }
        
        /// <summary>
        /// 地形による風向き効果を適用
        /// </summary>
        private ClimateData ApplyTopographicWindEffects(ClimateData climate, Vector3 position)
        {
            // 地形勾配を計算
            Vector2 terrainGradient = GetTerrainGradient(position);
            
            // 谷風・山風効果
            Vector2 valleyWind = CalculateValleyWind(position);
            
            // 地形による風向き偏向
            Vector2 deflectedWind = climate.windDirection;
            deflectedWind += terrainGradient * 0.3f;
            deflectedWind += valleyWind * 0.2f;
            
            climate.windDirection = deflectedWind.normalized;
            
            // 地形による風速変化
            float terrainRoughness = GetTerrainRoughness(position);
            float channeling = CalculateWindChanneling(position);
            
            climate.windSpeed *= (1f - terrainRoughness * 0.4f + channeling * 0.3f);
            climate.windSpeed = Mathf.Max(0f, climate.windSpeed);
            
            return climate;
        }
        
        /// <summary>
        /// 植生による気候効果を適用
        /// </summary>
        private ClimateData ApplyVegetationClimateEffects(ClimateData climate, VegetationData vegetation)
        {
            float vegetationEffect = vegetation.density * vegetation.stability;
            
            // 植生による温度緩和
            climate.temperature -= vegetationEffect * 3f;
            climate.temperatureVariation *= (1f - vegetationEffect * 0.4f);
            
            // 植生による湿度増加
            climate.humidity += vegetationEffect * 15f;
            climate.moisture += vegetationEffect * 100f;
            
            // 植生による風速減衰
            climate.windSpeed *= (1f - vegetationEffect * 0.3f);
            
            return climate;
        }
        
        #endregion
        
        #region 浸食計算
        
        /// <summary>
        /// 水による浸食を計算
        /// </summary>
        private float CalculateWaterErosion(ClimateData climate, Vector3 position)
        {
            // 降水量による浸食強度
            float precipitationFactor = climate.moisture / 2000f;
            
            // 地形勾配による浸食強度
            Vector2 gradient = GetTerrainGradient(position);
            float slopeFactor = gradient.magnitude;
            
            // 温度による浸食効率（凍結融解サイクル）
            float temperatureFactor = 1f;
            if (climate.temperature < 5f && climate.temperature > -5f)
            {
                temperatureFactor = 2f; // 凍結融解帯で浸食が活発
            }
            
            return erosionRate * precipitationFactor * slopeFactor * temperatureFactor;
        }
        
        /// <summary>
        /// 風による浸食を計算
        /// </summary>
        private float CalculateWindErosion(ClimateData climate, Vector3 position)
        {
            // 風速による浸食強度
            float windFactor = Mathf.Pow(climate.windSpeed / 10f, 2f);
            
            // 乾燥度による浸食効率
            float drynessFactor = 1f - Mathf.Clamp01(climate.moisture / 1000f);
            
            // 地表の露出度（植生による保護）
            float exposureFactor = 1f;
            Vector2Int key = WorldPositionToGridKey(position);
            if (vegetationMap.ContainsKey(key))
            {
                exposureFactor = 1f - vegetationMap[key].density * 0.8f;
            }
            
            return erosionRate * 0.5f * windFactor * drynessFactor * exposureFactor;
        }
        
        /// <summary>
        /// 熱による風化を計算
        /// </summary>
        private float CalculateThermalWeathering(ClimateData climate)
        {
            // 温度変動による風化
            float temperatureVariation = climate.temperatureVariation;
            float weatheringFactor = temperatureVariation / 20f;
            
            // 湿度による化学風化
            float chemicalWeathering = climate.humidity / 100f;
            
            return weatheringRate * (weatheringFactor + chemicalWeathering);
        }
        
        #endregion
        
        #region 植生計算
        
        /// <summary>
        /// 植生密度を計算
        /// </summary>
        private float CalculateVegetationDensity(ClimateData climate)
        {
            // 温度適性
            float tempOptimum = 25f;
            float tempFactor = 1f - Mathf.Abs(climate.temperature - tempOptimum) / temperatureToleranceRange;
            tempFactor = Mathf.Clamp01(tempFactor);
            
            // 湿度適性
            float moistureFactor = Mathf.Clamp01(climate.moisture / moistureRequirement);
            
            // 風速による制限
            float windFactor = 1f - Mathf.Clamp01((climate.windSpeed - 5f) / 20f);
            
            return tempFactor * moistureFactor * windFactor;
        }
        
        /// <summary>
        /// 植生成長因子を計算
        /// </summary>
        private float CalculateVegetationGrowthFactor(ClimateData climate)
        {
            float optimalTemp = 20f;
            float optimalMoisture = 1500f;
            
            // 温度因子
            float tempFactor = 1f - Mathf.Abs(climate.temperature - optimalTemp) / temperatureToleranceRange;
            
            // 湿度因子
            float moistureFactor = climate.moisture / optimalMoisture;
            moistureFactor = Mathf.Clamp(moistureFactor, 0f, 2f) - 1f; // -1 to 1 range
            
            // 総合成長因子
            return (tempFactor + moistureFactor) * 0.5f;
        }
        
        /// <summary>
        /// 植生タイプを決定
        /// </summary>
        private VegetationType DetermineVegetationType(ClimateData climate)
        {
            if (climate.temperature < 0f)
            {
                return VegetationType.Tundra;
            }
            else if (climate.temperature < 10f)
            {
                return climate.moisture > 800f ? VegetationType.BorealForest : VegetationType.Grassland;
            }
            else if (climate.temperature < 25f)
            {
                if (climate.moisture > 1500f)
                    return VegetationType.TemperateForest;
                else if (climate.moisture > 600f)
                    return VegetationType.Grassland;
                else
                    return VegetationType.Shrubland;
            }
            else
            {
                if (climate.moisture > 2000f)
                    return VegetationType.TropicalForest;
                else if (climate.moisture > 500f)
                    return VegetationType.Savanna;
                else
                    return VegetationType.Desert;
            }
        }
        
        #endregion
        
        #region 長期変化
        
        /// <summary>
        /// 気候トレンドを分析
        /// </summary>
        private ClimateTrend AnalyzeClimateTrend()
        {
            if (climateHistory.Count < 2) return new ClimateTrend();
            
            var entries = climateHistory.ToArray();
            int count = entries.Length;
            
            // 線形回帰で傾向を計算
            float tempTrend = CalculateLinearTrend(entries, e => e.averageTemperature);
            float moistureTrend = CalculateLinearTrend(entries, e => e.averageMoisture);
            float windTrend = CalculateLinearTrend(entries, e => e.averageWindSpeed);
            
            return new ClimateTrend
            {
                temperatureTrend = tempTrend,
                moistureTrend = moistureTrend,
                windSpeedTrend = windTrend,
                confidence = Mathf.Clamp01(count / 100f)
            };
        }
        
        /// <summary>
        /// 線形トレンドを計算
        /// </summary>
        private float CalculateLinearTrend(ClimateHistoryEntry[] entries, System.Func<ClimateHistoryEntry, float> valueSelector)
        {
            int n = entries.Length;
            if (n < 2) return 0f;
            
            float sumX = 0f, sumY = 0f, sumXY = 0f, sumX2 = 0f;
            
            for (int i = 0; i < n; i++)
            {
                float x = i;
                float y = valueSelector(entries[i]);
                
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
            }
            
            float slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            return slope;
        }
        
        /// <summary>
        /// 長期的な地形変化を適用
        /// </summary>
        private void ApplyLongTermTerrainChanges(ClimateTrend trend)
        {
            foreach (var key in erosionMap.Keys.ToList())
            {
                var erosionData = erosionMap[key];
                
                // 気候トレンドに基づく浸食率調整
                float trendEffect = (trend.temperatureTrend + trend.moistureTrend * 0.001f) * trend.confidence;
                
                erosionData.waterErosion += trendEffect * longTermChangeRate;
                erosionData.thermalErosion += Mathf.Abs(trend.temperatureTrend) * longTermChangeRate;
                
                erosionMap[key] = erosionData;
            }
        }
        
        /// <summary>
        /// 長期的な植生変化を適用
        /// </summary>
        private void ApplyLongTermVegetationChanges(ClimateTrend trend)
        {
            foreach (var key in vegetationMap.Keys.ToList())
            {
                var vegetationData = vegetationMap[key];
                
                // 気候トレンドに基づく植生変化
                float tempEffect = -Mathf.Abs(trend.temperatureTrend) * 0.1f;
                float moistureEffect = trend.moistureTrend * 0.0001f;
                
                float totalEffect = (tempEffect + moistureEffect) * trend.confidence * longTermChangeRate;
                
                vegetationData.density += totalEffect;
                vegetationData.density = Mathf.Clamp01(vegetationData.density);
                
                vegetationMap[key] = vegetationData;
            }
        }
        
        #endregion
        
        #region ユーティリティメソッド
        
        /// <summary>
        /// 地形標高を取得
        /// </summary>
        private float GetTerrainElevation(Vector3 position)
        {
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 1000f, Vector3.down, out hit, 2000f))
            {
                return hit.point.y;
            }
            return 0f;
        }
        
        /// <summary>
        /// 地形勾配を取得
        /// </summary>
        private Vector2 GetTerrainGradient(Vector3 position)
        {
            float delta = 100f;
            
            float heightCenter = GetTerrainElevation(position);
            float heightRight = GetTerrainElevation(position + Vector3.right * delta);
            float heightForward = GetTerrainElevation(position + Vector3.forward * delta);
            
            return new Vector2(
                (heightRight - heightCenter) / delta,
                (heightForward - heightCenter) / delta
            );
        }
        
        /// <summary>
        /// 地形粗さを取得
        /// </summary>
        private float GetTerrainRoughness(Vector3 position)
        {
            float roughness = 0f;
            int samples = 8;
            float radius = 200f;
            
            float centerHeight = GetTerrainElevation(position);
            
            for (int i = 0; i < samples; i++)
            {
                float angle = (i / (float)samples) * 2f * Mathf.PI;
                Vector3 samplePos = position + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );
                
                float sampleHeight = GetTerrainElevation(samplePos);
                roughness += Mathf.Abs(sampleHeight - centerHeight);
            }
            
            return roughness / (samples * 100f);
        }
        
        /// <summary>
        /// 谷風を計算
        /// </summary>
        private Vector2 CalculateValleyWind(Vector3 position)
        {
            // 周辺の地形から谷の方向を推定
            Vector2 valleyDirection = Vector2.zero;
            int samples = 8;
            float radius = 500f;
            
            float centerHeight = GetTerrainElevation(position);
            
            for (int i = 0; i < samples; i++)
            {
                float angle = (i / (float)samples) * 2f * Mathf.PI;
                Vector3 samplePos = position + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );
                
                float sampleHeight = GetTerrainElevation(samplePos);
                float heightDiff = centerHeight - sampleHeight;
                
                if (heightDiff > 0f) // 下り方向
                {
                    Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    valleyDirection += direction * heightDiff;
                }
            }
            
            return valleyDirection.normalized;
        }
        
        /// <summary>
        /// 風のチャネリング効果を計算
        /// </summary>
        private float CalculateWindChanneling(Vector3 position)
        {
            // 谷間での風速増加を計算
            Vector2 gradient = GetTerrainGradient(position);
            float channeling = 0f;
            
            // 急峻な地形での風のチャネリング
            if (gradient.magnitude > 0.1f)
            {
                channeling = gradient.magnitude * 0.5f;
            }
            
            return channeling;
        }
        
        /// <summary>
        /// グリッドキーをワールド座標に変換
        /// </summary>
        private Vector3 GridKeyToWorldPosition(Vector2Int key)
        {
            float worldSize = 10000f;
            int mapSize = 100;
            float step = worldSize / mapSize;
            
            return new Vector3(
                (key.x - mapSize * 0.5f) * step,
                0f,
                (key.y - mapSize * 0.5f) * step
            );
        }
        
        /// <summary>
        /// ワールド座標をグリッドキーに変換
        /// </summary>
        private Vector2Int WorldPositionToGridKey(Vector3 worldPos)
        {
            float worldSize = 10000f;
            int mapSize = 100;
            float step = worldSize / mapSize;
            
            return new Vector2Int(
                Mathf.RoundToInt(worldPos.x / step + mapSize * 0.5f),
                Mathf.RoundToInt(worldPos.z / step + mapSize * 0.5f)
            );
        }
        
        /// <summary>
        /// フィードバック可視化を描画
        /// </summary>
        private void DrawFeedbackVisualization()
        {
            // 植生密度の可視化
            foreach (var kvp in vegetationMap)
            {
                Vector3 pos = GridKeyToWorldPosition(kvp.Key);
                float density = kvp.Value.density;
                
                Gizmos.color = Color.Lerp(Color.red, Color.green, density);
                Gizmos.DrawCube(pos, Vector3.one * 50f);
            }
            
            // 浸食データの可視化
            foreach (var kvp in erosionMap)
            {
                Vector3 pos = GridKeyToWorldPosition(kvp.Key);
                float totalErosion = kvp.Value.waterErosion + kvp.Value.windErosion + kvp.Value.thermalErosion;
                
                if (totalErosion > 0.001f)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireCube(pos, Vector3.one * 100f);
                }
            }
        }
        
        #endregion
        
        #region パブリックメソッド
        
        /// <summary>
        /// 指定位置の植生データを取得
        /// </summary>
        public VegetationData GetVegetationAt(Vector3 position)
        {
            Vector2Int key = WorldPositionToGridKey(position);
            return vegetationMap.ContainsKey(key) ? vegetationMap[key] : new VegetationData();
        }
        
        /// <summary>
        /// 指定位置の浸食データを取得
        /// </summary>
        public ErosionData GetErosionAt(Vector3 position)
        {
            Vector2Int key = WorldPositionToGridKey(position);
            return erosionMap.ContainsKey(key) ? erosionMap[key] : new ErosionData();
        }
        
        /// <summary>
        /// フィードバック強度を設定
        /// </summary>
        public void SetFeedbackIntensity(float terrainToClimate, float climateToTerrain, float vegetation)
        {
            rainShadowIntensity = Mathf.Clamp01(terrainToClimate);
            erosionRate = Mathf.Clamp(climateToTerrain * 0.001f, 0f, 0.01f);
            vegetationGrowthRate = Mathf.Clamp(vegetation * 0.01f, 0f, 0.1f);
        }
        
        /// <summary>
        /// フィードバックデータをリセット
        /// </summary>
        public void ResetFeedbackData()
        {
            feedbackCache.Clear();
            vegetationMap.Clear();
            erosionMap.Clear();
            climateHistory.Clear();
            
            InitializeFeedbackData();
        }
        
        #endregion
    }
    
    #region データ構造
    
    /// <summary>
    /// 地形気候データ
    /// </summary>
    [System.Serializable]
    public struct TerrainClimateData
    {
        public ClimateData originalClimate;
        public ClimateData modifiedClimate;
        public float lastUpdateTime;
    }
    
    /// <summary>
    /// 植生データ
    /// </summary>
    [System.Serializable]
    public struct VegetationData
    {
        public float density;           // 植生密度 (0-1)
        public VegetationType type;     // 植生タイプ
        public float stability;         // 安定性 (0-1)
        public float lastUpdateTime;    // 最終更新時間
    }
    
    /// <summary>
    /// 植生タイプ
    /// </summary>
    public enum VegetationType
    {
        Desert,           // 砂漠
        Grassland,        // 草原
        Shrubland,        // 低木林
        TemperateForest,  // 温帯林
        BorealForest,     // 針葉樹林
        TropicalForest,   // 熱帯林
        Tundra,           // ツンドラ
        Savanna           // サバンナ
    }
    
    /// <summary>
    /// 浸食データ
    /// </summary>
    [System.Serializable]
    public struct ErosionData
    {
        public float waterErosion;      // 水による浸食
        public float windErosion;       // 風による浸食
        public float thermalErosion;    // 熱による風化
        public float deposition;        // 堆積
        public float lastUpdateTime;    // 最終更新時間
    }
    
    /// <summary>
    /// 気候履歴エントリ
    /// </summary>
    [System.Serializable]
    public struct ClimateHistoryEntry
    {
        public float timestamp;
        public float averageTemperature;
        public float averageMoisture;
        public float averageWindSpeed;
    }
    
    /// <summary>
    /// 気候トレンド
    /// </summary>
    [System.Serializable]
    public struct ClimateTrend
    {
        public float temperatureTrend;  // 温度トレンド
        public float moistureTrend;     // 湿度トレンド
        public float windSpeedTrend;    // 風速トレンド
        public float confidence;        // 信頼度
    }
    
    #endregion
}