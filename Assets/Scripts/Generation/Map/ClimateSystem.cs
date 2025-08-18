using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Vastcore.Generation
{
    /// <summary>
    /// 気候システム - 気候データ生成と地形相互作用を管理
    /// 要求: 2.3, 1.4 リアルタイム更新
    /// </summary>
    public class ClimateSystem : MonoBehaviour
    {
        [Header("システム設定")]
        public bool enableClimateSystem = true;
        public bool enableRealtimeUpdates = true;
        public float updateInterval = 1f;
        
        [Header("世界設定")]
        public float worldRadius = 10000f;          // ワールド半径（m）
        public Vector2 worldCenter = Vector2.zero;  // ワールド中心座標
        public float equatorLatitude = 0f;          // 赤道緯度
        
        [Header("気候パラメータ")]
        [Range(0f, 1f)]
        public float globalTemperature = 0.5f;      // 全球平均温度調整
        [Range(0f, 2f)]
        public float globalMoisture = 1f;           // 全球湿度調整
        [Range(0f, 2f)]
        public float globalWindStrength = 1f;       // 全球風力調整
        
        [Header("季節設定")]
        public bool enableSeasons = true;
        public float seasonCycleLength = 360f;      // 季節サイクル長（秒）
        [Range(0f, 1f)]
        public float currentSeason = 0f;            // 現在の季節（0-1）
        public float seasonalIntensity = 1f;        // 季節変動の強度
        
        [Header("長期変動")]
        public bool enableLongTermVariation = true;
        public float climateChangeRate = 0.001f;    // 気候変動率
        public float variationCycleLength = 3600f;  // 長期変動サイクル（秒）
        
        [Header("海洋設定")]
        public Transform[] oceanPoints;             // 海洋位置
        public float oceanInfluenceRadius = 2000f; // 海洋影響半径
        public float oceanTemperatureModeration = 0.7f; // 海洋性気候の温度緩和
        
        [Header("デバッグ設定")]
        public bool showClimateVisualization = false;
        public bool logClimateUpdates = false;
        public int visualizationResolution = 20;
        
        // プライベートフィールド
        private ClimateDataGenerator climateGenerator;
        private Dictionary<Vector2Int, ClimateData> climateCache;
        private float lastUpdateTime;
        private bool isInitialized = false;
        private Coroutine updateCoroutine;
        
        // 気候変動データ
        private float longTermTemperatureOffset = 0f;
        private float longTermMoistureOffset = 0f;
        
        // イベント
        public System.Action<ClimateData> OnClimateDataUpdated;
        public System.Action<float> OnSeasonChanged;
        public System.Action<float> OnLongTermClimateChanged;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            climateCache = new Dictionary<Vector2Int, ClimateData>();
        }
        
        private void Start()
        {
            Initialize();
        }
        
        private void Update()
        {
            if (!isInitialized || !enableClimateSystem) return;
            
            UpdateSeasonalCycle();
            UpdateLongTermVariation();
            
            if (enableRealtimeUpdates && Time.time - lastUpdateTime > updateInterval)
            {
                UpdateClimateSystem();
                lastUpdateTime = Time.time;
            }
        }
        
        private void OnDrawGizmos()
        {
            if (showClimateVisualization && isInitialized)
            {
                DrawClimateVisualization();
            }
        }
        
        private void OnDestroy()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
        }
        
        #endregion
        
        #region 初期化
        
        /// <summary>
        /// 気候システムを初期化
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;
            
            try
            {
                // ClimateDataGeneratorを取得または作成
                climateGenerator = GetComponent<ClimateDataGenerator>();
                if (climateGenerator == null)
                {
                    climateGenerator = gameObject.AddComponent<ClimateDataGenerator>();
                }
                
                // 海洋位置を設定
                SetupOceanPositions();
                
                // 初期気候データを生成
                GenerateInitialClimateData();
                
                // 更新コルーチンを開始
                if (enableRealtimeUpdates)
                {
                    updateCoroutine = StartCoroutine(ClimateUpdateCoroutine());
                }
                
                isInitialized = true;
                Debug.Log("ClimateSystem initialized successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ClimateSystem initialization failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// 海洋位置を設定
        /// </summary>
        private void SetupOceanPositions()
        {
            if (oceanPoints != null && oceanPoints.Length > 0)
            {
                Vector2[] oceanPositions = new Vector2[oceanPoints.Length];
                for (int i = 0; i < oceanPoints.Length; i++)
                {
                    if (oceanPoints[i] != null)
                    {
                        oceanPositions[i] = new Vector2(oceanPoints[i].position.x, oceanPoints[i].position.z);
                    }
                }
                climateGenerator.SetOceanPositions(oceanPositions);
            }
        }
        
        /// <summary>
        /// 初期気候データを生成
        /// </summary>
        private void GenerateInitialClimateData()
        {
            // ワールド全体の気候データを事前計算
            int cacheResolution = 50;
            float step = worldRadius * 2f / cacheResolution;
            
            for (int x = 0; x < cacheResolution; x++)
            {
                for (int y = 0; y < cacheResolution; y++)
                {
                    Vector2 worldPos = worldCenter + new Vector2(
                        (x - cacheResolution * 0.5f) * step,
                        (y - cacheResolution * 0.5f) * step
                    );
                    
                    Vector2Int cacheKey = new Vector2Int(x, y);
                    Vector3 worldPos3D = new Vector3(worldPos.x, 0f, worldPos.y);
                    
                    ClimateData climate = climateGenerator.GetClimateDataAtPosition(worldPos3D);
                    climate = ApplyGlobalModifiers(climate);
                    
                    climateCache[cacheKey] = climate;
                }
            }
        }
        
        #endregion
        
        #region 気候データ取得
        
        /// <summary>
        /// 指定位置の気候データを取得
        /// </summary>
        public ClimateData GetClimateAt(Vector3 worldPosition)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("ClimateSystem not initialized");
                return ClimateData.Default;
            }
            
            try
            {
                // 基本気候データを取得
                ClimateData baseClimate = climateGenerator.GetClimateDataAtPosition(worldPosition);
                
                // グローバル修正を適用
                ClimateData modifiedClimate = ApplyGlobalModifiers(baseClimate);
                
                // 季節変動を適用
                modifiedClimate = ApplySeasonalVariation(modifiedClimate, worldPosition);
                
                // 長期変動を適用
                modifiedClimate = ApplyLongTermVariation(modifiedClimate);
                
                // 地形フィードバックを適用
                modifiedClimate = ApplyTerrainFeedback(modifiedClimate, worldPosition);
                
                return modifiedClimate.Normalize();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"GetClimateAt failed: {e.Message}");
                return ClimateData.Default;
            }
        }
        
        /// <summary>
        /// 緯度・高度・海洋距離に基づく気候計算
        /// 要求: 2.3
        /// </summary>
        public ClimateData CalculateClimateFromGeography(Vector3 position)
        {
            // 緯度計算（ワールド中心からの距離に基づく）
            float latitude = CalculateLatitude(position);
            
            // 高度取得
            float elevation = position.y;
            
            // 海洋からの距離計算
            float oceanDistance = CalculateOceanDistance(position);
            
            // 基本気候パラメータを計算
            float temperature = CalculateTemperatureFromGeography(latitude, elevation, oceanDistance);
            float moisture = CalculateMoistureFromGeography(latitude, elevation, oceanDistance);
            Vector2 windDirection = CalculateWindFromGeography(latitude, position);
            float windSpeed = CalculateWindSpeedFromGeography(latitude, elevation);
            
            return new ClimateData
            {
                temperature = temperature,
                moisture = moisture,
                windDirection = windDirection,
                windSpeed = windSpeed,
                elevationEffect = elevation * -0.006f,
                oceanDistance = oceanDistance,
                continentalityIndex = Mathf.Clamp01(oceanDistance / oceanInfluenceRadius)
            };
        }
        
        #endregion
        
        #region 季節サイクル
        
        /// <summary>
        /// 季節サイクルを更新
        /// </summary>
        private void UpdateSeasonalCycle()
        {
            if (!enableSeasons) return;
            
            float previousSeason = currentSeason;
            currentSeason = (Time.time / seasonCycleLength) % 1f;
            
            // 季節変化イベントを発火
            if (Mathf.Abs(currentSeason - previousSeason) > 0.01f)
            {
                OnSeasonChanged?.Invoke(currentSeason);
                climateGenerator.SetSeason(currentSeason);
            }
        }
        
        /// <summary>
        /// 季節変動を適用
        /// </summary>
        private ClimateData ApplySeasonalVariation(ClimateData baseClimate, Vector3 position)
        {
            if (!enableSeasons) return baseClimate;
            
            float latitude = CalculateLatitude(position);
            float seasonalEffect = Mathf.Cos(latitude * Mathf.PI / 180f) * seasonalIntensity;
            
            // 季節による温度変動
            float seasonalTemp = Mathf.Sin(currentSeason * 2f * Mathf.PI) * seasonalEffect * 15f;
            
            // 季節による降水変動
            float seasonalMoisture = Mathf.Sin((currentSeason + 0.25f) * 2f * Mathf.PI) * seasonalEffect * 300f;
            
            var seasonalClimate = baseClimate;
            seasonalClimate.temperature += seasonalTemp;
            seasonalClimate.moisture += seasonalMoisture;
            seasonalClimate.seasonalTemperatureVariation = seasonalTemp;
            seasonalClimate.seasonalMoistureVariation = seasonalMoisture;
            
            return seasonalClimate;
        }
        
        #endregion
        
        #region 長期変動
        
        /// <summary>
        /// 長期気候変動を更新
        /// </summary>
        private void UpdateLongTermVariation()
        {
            if (!enableLongTermVariation) return;
            
            float variationProgress = (Time.time / variationCycleLength) % 1f;
            
            float previousTempOffset = longTermTemperatureOffset;
            longTermTemperatureOffset = Mathf.Sin(variationProgress * 2f * Mathf.PI) * climateChangeRate * 100f;
            longTermMoistureOffset = Mathf.Cos(variationProgress * 2f * Mathf.PI) * climateChangeRate * 500f;
            
            // 長期変動イベントを発火
            if (Mathf.Abs(longTermTemperatureOffset - previousTempOffset) > 0.1f)
            {
                OnLongTermClimateChanged?.Invoke(longTermTemperatureOffset);
            }
        }
        
        /// <summary>
        /// 長期変動を適用
        /// </summary>
        private ClimateData ApplyLongTermVariation(ClimateData baseClimate)
        {
            if (!enableLongTermVariation) return baseClimate;
            
            var longTermClimate = baseClimate;
            longTermClimate.temperature += longTermTemperatureOffset;
            longTermClimate.moisture += longTermMoistureOffset;
            
            return longTermClimate;
        }
        
        #endregion
        
        #region 地形フィードバック
        
        /// <summary>
        /// 地形フィードバックを適用
        /// 要求: 1.4
        /// </summary>
        private ClimateData ApplyTerrainFeedback(ClimateData baseClimate, Vector3 position)
        {
            var feedbackClimate = baseClimate;
            
            // 雨陰効果の計算
            feedbackClimate = ApplyRainShadowEffect(feedbackClimate, position);
            
            // 地形による風向き変化
            feedbackClimate = ApplyTerrainWindEffect(feedbackClimate, position);
            
            // 植生による気候安定化
            feedbackClimate = ApplyVegetationEffect(feedbackClimate, position);
            
            return feedbackClimate;
        }
        
        /// <summary>
        /// 雨陰効果を適用
        /// </summary>
        private ClimateData ApplyRainShadowEffect(ClimateData climate, Vector3 position)
        {
            // 風上側の地形高度を調査
            Vector3 windDirection3D = new Vector3(climate.windDirection.x, 0f, climate.windDirection.y);
            Vector3 windwardPosition = position - windDirection3D * 1000f;
            
            // 風上側の地形が高い場合、雨陰効果を適用
            float windwardElevation = GetTerrainElevation(windwardPosition);
            float currentElevation = GetTerrainElevation(position);
            
            if (windwardElevation > currentElevation + 100f)
            {
                float rainShadowEffect = Mathf.Clamp01((windwardElevation - currentElevation) / 500f);
                climate.moisture *= (1f - rainShadowEffect * 0.6f);
                climate.humidity *= (1f - rainShadowEffect * 0.4f);
            }
            
            return climate;
        }
        
        /// <summary>
        /// 地形による風向き効果を適用
        /// </summary>
        private ClimateData ApplyTerrainWindEffect(ClimateData climate, Vector3 position)
        {
            // 地形勾配を計算
            Vector2 terrainGradient = GetTerrainGradient(position);
            
            // 地形勾配による風向きの偏向
            Vector2 deflectedWind = climate.windDirection + terrainGradient * 0.3f;
            climate.windDirection = deflectedWind.normalized;
            
            // 地形の粗さによる風速減衰
            float terrainRoughness = GetTerrainRoughness(position);
            climate.windSpeed *= (1f - terrainRoughness * 0.3f);
            
            return climate;
        }
        
        /// <summary>
        /// 植生による効果を適用
        /// </summary>
        private ClimateData ApplyVegetationEffect(ClimateData climate, Vector3 position)
        {
            // 植生密度を推定（気候条件から）
            float vegetationDensity = EstimateVegetationDensity(climate);
            
            // 植生による温度緩和
            climate.temperature -= vegetationDensity * 2f;
            climate.temperatureVariation *= (1f - vegetationDensity * 0.3f);
            
            // 植生による湿度増加
            climate.humidity += vegetationDensity * 10f;
            
            // 植生による風速減衰
            climate.windSpeed *= (1f - vegetationDensity * 0.2f);
            
            return climate;
        }
        
        #endregion
        
        #region ユーティリティメソッド
        
        /// <summary>
        /// グローバル修正を適用
        /// </summary>
        private ClimateData ApplyGlobalModifiers(ClimateData baseClimate)
        {
            var modified = baseClimate;
            
            // 全球温度調整
            float tempRange = 50f; // -25°C to +25°C
            modified.temperature = Mathf.Lerp(-25f, 25f, globalTemperature) + 
                                 (baseClimate.temperature - 0f) * 0.5f;
            
            // 全球湿度調整
            modified.moisture *= globalMoisture;
            
            // 全球風力調整
            modified.windSpeed *= globalWindStrength;
            
            return modified;
        }
        
        /// <summary>
        /// 緯度を計算
        /// </summary>
        private float CalculateLatitude(Vector3 position)
        {
            Vector2 pos2D = new Vector2(position.x, position.z);
            float distanceFromEquator = Vector2.Distance(pos2D, worldCenter);
            return (distanceFromEquator / worldRadius) * 90f; // 0-90度
        }
        
        /// <summary>
        /// 海洋からの距離を計算
        /// </summary>
        private float CalculateOceanDistance(Vector3 position)
        {
            if (oceanPoints == null || oceanPoints.Length == 0)
                return 1000f;
            
            float minDistance = float.MaxValue;
            Vector2 pos2D = new Vector2(position.x, position.z);
            
            foreach (var ocean in oceanPoints)
            {
                if (ocean != null)
                {
                    Vector2 oceanPos = new Vector2(ocean.position.x, ocean.position.z);
                    float distance = Vector2.Distance(pos2D, oceanPos);
                    minDistance = Mathf.Min(minDistance, distance);
                }
            }
            
            return minDistance;
        }
        
        /// <summary>
        /// 地理的条件から温度を計算
        /// </summary>
        private float CalculateTemperatureFromGeography(float latitude, float elevation, float oceanDistance)
        {
            // 基準温度（赤道）
            float baseTemp = 30f;
            
            // 緯度による温度低下
            float latitudeEffect = -latitude * 0.6f;
            
            // 標高による温度低下
            float elevationEffect = -elevation * 0.006f;
            
            // 海洋性気候による温度緩和
            float oceanEffect = Mathf.Exp(-oceanDistance / oceanInfluenceRadius) * oceanTemperatureModeration * 5f;
            
            return baseTemp + latitudeEffect + elevationEffect + oceanEffect;
        }
        
        /// <summary>
        /// 地理的条件から湿度を計算
        /// </summary>
        private float CalculateMoistureFromGeography(float latitude, float elevation, float oceanDistance)
        {
            // 基準湿度
            float baseMoisture = 1500f;
            
            // 緯度による湿度変化（赤道付近が最も湿潤）
            float latitudeEffect = -Mathf.Abs(latitude - 30f) * 10f;
            
            // 海洋からの距離による湿度減少
            float oceanEffect = -oceanDistance * 0.5f;
            
            // 標高による湿度減少（雨陰効果）
            float elevationEffect = -elevation * 0.3f;
            
            return Mathf.Max(0f, baseMoisture + latitudeEffect + oceanEffect + elevationEffect);
        }
        
        /// <summary>
        /// 地理的条件から風向きを計算
        /// </summary>
        private Vector2 CalculateWindFromGeography(float latitude, Vector3 position)
        {
            // 基本的な偏西風・貿易風パターン
            Vector2 baseWind;
            
            if (latitude < 30f)
            {
                // 貿易風（東風）
                baseWind = new Vector2(-1f, 0f);
            }
            else if (latitude < 60f)
            {
                // 偏西風（西風）
                baseWind = new Vector2(1f, 0f);
            }
            else
            {
                // 極東風（東風）
                baseWind = new Vector2(-1f, 0f);
            }
            
            // 地形による局所的な風向き変化
            Vector2 terrainGradient = GetTerrainGradient(position);
            Vector2 modifiedWind = baseWind + terrainGradient * 0.2f;
            
            return modifiedWind.normalized;
        }
        
        /// <summary>
        /// 地理的条件から風速を計算
        /// </summary>
        private float CalculateWindSpeedFromGeography(float latitude, float elevation)
        {
            // 基準風速
            float baseWindSpeed = 8f;
            
            // 緯度による風速変化（中緯度が最も強い）
            float latitudeEffect = -Mathf.Abs(latitude - 45f) * 0.1f;
            
            // 標高による風速増加
            float elevationEffect = elevation * 0.01f;
            
            return Mathf.Max(0f, baseWindSpeed + latitudeEffect + elevationEffect);
        }
        
        /// <summary>
        /// 地形標高を取得
        /// </summary>
        private float GetTerrainElevation(Vector3 position)
        {
            // レイキャストで地形高度を取得
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
            // 周辺の高度変化から粗さを計算
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
            
            return roughness / (samples * 100f); // 正規化
        }
        
        /// <summary>
        /// 植生密度を推定
        /// </summary>
        private float EstimateVegetationDensity(ClimateData climate)
        {
            // 温度と湿度から植生密度を推定
            float tempFactor = 1f - Mathf.Abs(climate.temperature - 25f) / 50f;
            float moistureFactor = Mathf.Clamp01(climate.moisture / 2000f);
            
            return Mathf.Clamp01(tempFactor * moistureFactor);
        }
        
        /// <summary>
        /// 気候システムを更新
        /// </summary>
        private void UpdateClimateSystem()
        {
            // キャッシュされた気候データを更新
            var keys = new List<Vector2Int>(climateCache.Keys);
            foreach (var key in keys)
            {
                // 一部のキャッシュを更新（負荷分散）
                if (Random.value < 0.1f) // 10%の確率で更新
                {
                    Vector3 worldPos = CacheKeyToWorldPosition(key);
                    ClimateData updatedClimate = GetClimateAt(worldPos);
                    climateCache[key] = updatedClimate;
                    
                    OnClimateDataUpdated?.Invoke(updatedClimate);
                }
            }
        }
        
        /// <summary>
        /// キャッシュキーをワールド座標に変換
        /// </summary>
        private Vector3 CacheKeyToWorldPosition(Vector2Int key)
        {
            int cacheResolution = 50;
            float step = worldRadius * 2f / cacheResolution;
            
            Vector2 worldPos = worldCenter + new Vector2(
                (key.x - cacheResolution * 0.5f) * step,
                (key.y - cacheResolution * 0.5f) * step
            );
            
            return new Vector3(worldPos.x, 0f, worldPos.y);
        }
        
        /// <summary>
        /// 気候更新コルーチン
        /// </summary>
        private IEnumerator ClimateUpdateCoroutine()
        {
            while (enableClimateSystem)
            {
                yield return new WaitForSeconds(updateInterval);
                
                if (enableRealtimeUpdates)
                {
                    UpdateClimateSystem();
                }
            }
        }
        
        /// <summary>
        /// 気候可視化を描画
        /// </summary>
        private void DrawClimateVisualization()
        {
            Vector3 center = transform.position;
            float range = 2000f;
            
            for (int x = 0; x < visualizationResolution; x++)
            {
                for (int y = 0; y < visualizationResolution; y++)
                {
                    Vector3 pos = center + new Vector3(
                        (x - visualizationResolution * 0.5f) * range / visualizationResolution,
                        0f,
                        (y - visualizationResolution * 0.5f) * range / visualizationResolution
                    );
                    
                    ClimateData climate = GetClimateAt(pos);
                    
                    // 温度による色分け
                    Color tempColor = Color.Lerp(Color.blue, Color.red, 
                        Mathf.InverseLerp(-20f, 40f, climate.temperature));
                    
                    Gizmos.color = tempColor;
                    Gizmos.DrawCube(pos, Vector3.one * 50f);
                    
                    // 風向きの表示
                    Gizmos.color = Color.white;
                    Vector3 windDir3D = new Vector3(climate.windDirection.x, 0f, climate.windDirection.y);
                    Gizmos.DrawRay(pos, windDir3D * climate.windSpeed * 10f);
                }
            }
        }
        
        #endregion
        
        #region パブリックメソッド
        
        /// <summary>
        /// 季節を手動設定
        /// </summary>
        public void SetSeason(float season)
        {
            currentSeason = Mathf.Clamp01(season);
            OnSeasonChanged?.Invoke(currentSeason);
        }
        
        /// <summary>
        /// 気候パラメータを設定
        /// </summary>
        public void SetGlobalClimateParameters(float temperature, float moisture, float windStrength)
        {
            globalTemperature = Mathf.Clamp01(temperature);
            globalMoisture = Mathf.Clamp(moisture, 0f, 2f);
            globalWindStrength = Mathf.Clamp(windStrength, 0f, 2f);
        }
        
        /// <summary>
        /// 海洋位置を動的に設定
        /// </summary>
        public void SetOceanPositions(Transform[] oceans)
        {
            oceanPoints = oceans;
            SetupOceanPositions();
        }
        
        /// <summary>
        /// 気候データキャッシュをクリア
        /// </summary>
        public void ClearClimateCache()
        {
            climateCache.Clear();
            GenerateInitialClimateData();
        }
        
        /// <summary>
        /// 現在の気候統計を取得
        /// </summary>
        public (float avgTemp, float avgMoisture, float avgWindSpeed) GetClimateStatistics()
        {
            if (climateCache.Count == 0) return (20f, 1000f, 5f);
            
            float totalTemp = 0f;
            float totalMoisture = 0f;
            float totalWindSpeed = 0f;
            
            foreach (var climate in climateCache.Values)
            {
                totalTemp += climate.temperature;
                totalMoisture += climate.moisture;
                totalWindSpeed += climate.windSpeed;
            }
            
            int count = climateCache.Count;
            return (totalTemp / count, totalMoisture / count, totalWindSpeed / count);
        }
        
        #endregion
    }
}