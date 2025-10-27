using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// 気候データ生成システム
    /// 地理的位置に基づいて温度、湿度、風向きなどの気候データを生成
    /// </summary>
    public class ClimateDataGenerator : MonoBehaviour
    {
        [Header("気候生成設定")]
        public bool useRealisticClimate = true;
        public float worldScale = 1000f; // ワールドスケール（1単位 = 1km）
        
        [Header("温度設定")]
        public Vector2 temperatureRange = new Vector2(-30f, 45f);
        public float latitudeTemperatureEffect = 0.8f; // 緯度による温度変化
        public float elevationTemperatureEffect = 0.006f; // 標高による温度変化（°C/m）
        public float seasonalVariation = 15f; // 季節変動
        
        [Header("湿度設定")]
        public Vector2 moistureRange = new Vector2(0f, 3000f);
        public float oceanDistanceEffect = 0.5f; // 海洋からの距離による湿度変化
        public float elevationMoistureEffect = 0.3f; // 標高による湿度変化
        
        [Header("風設定")]
        public Vector2 prevailingWindDirection = new Vector2(1f, 0f);
        public float windSpeedRange = 10f;
        public bool enableSeasonalWinds = true;
        
        [Header("ノイズ設定")]
        public float temperatureNoiseScale = 0.001f;
        public float moistureNoiseScale = 0.0015f;
        public float windNoiseScale = 0.002f;
        
        [Header("デバッグ設定")]
        public bool showClimateVisualization = false;
        public bool logClimateData = false;
        
        // プライベートフィールド
        private float currentSeason = 0f; // 0-1の範囲で季節を表現
        private Vector2 worldCenter = Vector2.zero;
        private bool isInitialized = false;
        
        // 海洋位置（簡易実装）
        private Vector2[] oceanPositions = new Vector2[]
        {
            new Vector2(-5000f, 0f),
            new Vector2(5000f, 0f),
            new Vector2(0f, -5000f),
            new Vector2(0f, 5000f)
        };
        
        #region Unity Lifecycle
        
        private void Start()
        {
            Initialize();
        }
        
        private void Update()
        {
            if (isInitialized)
            {
                UpdateSeason();
            }
        }
        
        private void OnDrawGizmos()
        {
            if (showClimateVisualization && isInitialized)
            {
                DrawClimateVisualization();
            }
        }
        
        #endregion
        
        #region 初期化
        
        /// <summary>
        /// 気候データ生成システムを初期化
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;
            
            try
            {
                // ワールド中心の設定
                worldCenter = new Vector2(transform.position.x, transform.position.z);
                
                // 季節の初期化
                currentSeason = 0f;
                
                isInitialized = true;
                Debug.Log("ClimateDataGenerator initialized successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ClimateDataGenerator initialization failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// 季節を更新
        /// </summary>
        private void UpdateSeason()
        {
            // 1年を360秒として季節を循環
            currentSeason = (Time.time / 360f) % 1f;
        }
        
        #endregion
        
        #region 気候データ生成
        
        /// <summary>
        /// 指定位置の気候データを取得
        /// </summary>
        public ClimateData GetClimateDataAtPosition(Vector3 worldPosition)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("ClimateDataGenerator not initialized");
                return ClimateData.Default;
            }
            
            try
            {
                Vector2 position2D = new Vector2(worldPosition.x, worldPosition.z);
                float elevation = worldPosition.y;
                
                // 各気候要素を計算
                float temperature = CalculateTemperature(position2D, elevation);
                float moisture = CalculateMoisture(position2D, elevation);
                float windSpeed = CalculateWindSpeed(position2D);
                Vector2 windDirection = CalculateWindDirection(position2D);
                
                var climateData = new ClimateData
                {
                    temperature = temperature,
                    moisture = moisture,
                    windSpeed = windSpeed,
                    windDirection = windDirection,
                    humidity = Mathf.Clamp(moisture / 30f, 0f, 100f),
                    temperatureVariation = 10f,
                    seasonalTemperatureVariation = 0f,
                    seasonalMoistureVariation = 0f,
                    elevationEffect = worldPosition.y * -0.006f,
                    oceanDistance = GetDistanceToNearestOcean(position2D),
                    continentalityIndex = Mathf.Clamp01(GetDistanceToNearestOcean(position2D) / 2000f)
                };
                
                if (logClimateData)
                {
                    Debug.Log($"Climate at {worldPosition}: T={temperature:F1}°C, M={moisture:F0}mm, " +
                             $"Wind={windSpeed:F1}m/s {windDirection}");
                }
                
                return climateData;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"GetClimateDataAtPosition failed: {e.Message}");
                return ClimateData.Default;
            }
        }
        
        /// <summary>
        /// 温度を計算
        /// </summary>
        private float CalculateTemperature(Vector2 position, float elevation)
        {
            // 基準温度
            float baseTemperature = (temperatureRange.x + temperatureRange.y) * 0.5f;
            
            // 緯度による影響（北に行くほど寒い）
            float latitude = (position.y - worldCenter.y) / worldScale;
            float latitudeEffect = -Mathf.Abs(latitude) * latitudeTemperatureEffect * 30f;
            
            // 標高による影響（高いほど寒い）
            float elevationEffect = -elevation * elevationTemperatureEffect;
            
            // 季節による影響
            float seasonEffect = Mathf.Sin(currentSeason * 2f * Mathf.PI) * seasonalVariation;
            
            // ノイズによる地域的変動
            float noiseEffect = (Mathf.PerlinNoise(position.x * temperatureNoiseScale, position.y * temperatureNoiseScale) - 0.5f) * 10f;
            
            // 海洋からの距離による影響（海洋性気候）
            float oceanDistance = GetDistanceToNearestOcean(position);
            float oceanEffect = -oceanDistance / worldScale * 5f; // 内陸ほど寒暖差が大きい
            
            float finalTemperature = baseTemperature + latitudeEffect + elevationEffect + seasonEffect + noiseEffect + oceanEffect;
            
            return Mathf.Clamp(finalTemperature, temperatureRange.x, temperatureRange.y);
        }
        
        /// <summary>
        /// 湿度を計算
        /// </summary>
        private float CalculateMoisture(Vector2 position, float elevation)
        {
            // 基準湿度
            float baseMoisture = (moistureRange.x + moistureRange.y) * 0.5f;
            
            // 海洋からの距離による影響
            float oceanDistance = GetDistanceToNearestOcean(position);
            float oceanEffect = -oceanDistance / worldScale * oceanDistanceEffect * 1000f;
            
            // 標高による影響（雨陰効果）
            float elevationEffect = -elevation * elevationMoistureEffect;
            
            // 風向きによる影響（風上側は湿潤）
            Vector2 windDir = CalculateWindDirection(position);
            float windEffect = Vector2.Dot(windDir, (GetNearestOceanPosition(position) - position).normalized) * 200f;
            
            // ノイズによる地域的変動
            float noiseEffect = (Mathf.PerlinNoise(position.x * moistureNoiseScale, position.y * moistureNoiseScale) - 0.5f) * 500f;
            
            // 季節による影響（雨季・乾季）
            float seasonEffect = Mathf.Sin((currentSeason + 0.25f) * 2f * Mathf.PI) * 300f;
            
            float finalMoisture = baseMoisture + oceanEffect + elevationEffect + windEffect + noiseEffect + seasonEffect;
            
            return Mathf.Clamp(finalMoisture, moistureRange.x, moistureRange.y);
        }
        
        /// <summary>
        /// 風速を計算
        /// </summary>
        private float CalculateWindSpeed(Vector2 position)
        {
            // 基準風速
            float baseWindSpeed = windSpeedRange * 0.5f;
            
            // 地形による影響（平地は風が強い）
            float terrainEffect = -GetTerrainRoughness(position) * 3f;
            
            // 季節による影響
            float seasonEffect = Mathf.Sin(currentSeason * 2f * Mathf.PI) * windSpeedRange * 0.3f;
            
            // ノイズによる変動
            float noiseEffect = (Mathf.PerlinNoise(position.x * windNoiseScale, position.y * windNoiseScale) - 0.5f) * windSpeedRange * 0.5f;
            
            float finalWindSpeed = baseWindSpeed + terrainEffect + seasonEffect + noiseEffect;
            
            return Mathf.Clamp(finalWindSpeed, 0f, windSpeedRange);
        }
        
        /// <summary>
        /// 風向きを計算
        /// </summary>
        private Vector2 CalculateWindDirection(Vector2 position)
        {
            // 基本風向き
            Vector2 baseDirection = prevailingWindDirection.normalized;
            
            // 季節風の影響
            if (enableSeasonalWinds)
            {
                float seasonalAngle = Mathf.Sin(currentSeason * 2f * Mathf.PI) * 45f * Mathf.Deg2Rad;
                float cos = Mathf.Cos(seasonalAngle);
                float sin = Mathf.Sin(seasonalAngle);
                
                Vector2 seasonalDirection = new Vector2(
                    baseDirection.x * cos - baseDirection.y * sin,
                    baseDirection.x * sin + baseDirection.y * cos
                );
                
                baseDirection = seasonalDirection;
            }
            
            // 地形による風向きの変化
            Vector2 terrainGradient = GetTerrainGradient(position);
            Vector2 terrainEffect = terrainGradient * 0.3f;
            
            // ノイズによる局所的変動
            float noiseAngle = (Mathf.PerlinNoise(position.x * windNoiseScale * 2f, position.y * windNoiseScale * 2f) - 0.5f) * 30f * Mathf.Deg2Rad;
            Vector2 noiseDirection = new Vector2(Mathf.Cos(noiseAngle), Mathf.Sin(noiseAngle));
            
            Vector2 finalDirection = (baseDirection + terrainEffect + noiseDirection * 0.2f).normalized;
            
            return finalDirection;
        }
        
        #endregion
        
        #region ユーティリティメソッド
        
        /// <summary>
        /// 最寄りの海洋までの距離を取得
        /// </summary>
        private float GetDistanceToNearestOcean(Vector2 position)
        {
            float minDistance = float.MaxValue;
            
            foreach (var oceanPos in oceanPositions)
            {
                float distance = Vector2.Distance(position, oceanPos);
                minDistance = Mathf.Min(minDistance, distance);
            }
            
            return minDistance;
        }
        
        /// <summary>
        /// 最寄りの海洋位置を取得
        /// </summary>
        private Vector2 GetNearestOceanPosition(Vector2 position)
        {
            Vector2 nearestOcean = oceanPositions[0];
            float minDistance = Vector2.Distance(position, nearestOcean);
            
            for (int i = 1; i < oceanPositions.Length; i++)
            {
                float distance = Vector2.Distance(position, oceanPositions[i]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestOcean = oceanPositions[i];
                }
            }
            
            return nearestOcean;
        }
        
        /// <summary>
        /// 地形の粗さを取得（簡易実装）
        /// </summary>
        private float GetTerrainRoughness(Vector2 position)
        {
            // ノイズベースの地形粗さ計算
            float roughness = 0f;
            
            // 複数オクターブのノイズで地形の複雑さを計算
            for (int i = 0; i < 4; i++)
            {
                float frequency = 0.001f * Mathf.Pow(2f, i);
                float amplitude = 1f / Mathf.Pow(2f, i);
                
                roughness += Mathf.PerlinNoise(position.x * frequency, position.y * frequency) * amplitude;
            }
            
            return roughness;
        }
        
        /// <summary>
        /// 地形勾配を取得（簡易実装）
        /// </summary>
        private Vector2 GetTerrainGradient(Vector2 position)
        {
            float delta = 10f;
            
            float heightCenter = GetTerrainHeight(position);
            float heightRight = GetTerrainHeight(position + Vector2.right * delta);
            float heightUp = GetTerrainHeight(position + Vector2.up * delta);
            
            Vector2 gradient = new Vector2(
                (heightRight - heightCenter) / delta,
                (heightUp - heightCenter) / delta
            );
            
            return gradient;
        }
        
        /// <summary>
        /// 地形高度を取得（簡易実装）
        /// </summary>
        private float GetTerrainHeight(Vector2 position)
        {
            // ノイズベースの高度計算
            float height = 0f;
            
            for (int i = 0; i < 6; i++)
            {
                float frequency = 0.001f * Mathf.Pow(2f, i);
                float amplitude = 100f / Mathf.Pow(2f, i);
                
                height += Mathf.PerlinNoise(position.x * frequency, position.y * frequency) * amplitude;
            }
            
            return height;
        }
        
        /// <summary>
        /// 気候可視化を描画
        /// </summary>
        private void DrawClimateVisualization()
        {
            Vector3 center = transform.position;
            float range = 1000f;
            int resolution = 20;
            
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    Vector3 pos = center + new Vector3(
                        (x - resolution * 0.5f) * range / resolution,
                        0f,
                        (y - resolution * 0.5f) * range / resolution
                    );
                    
                    var climate = GetClimateDataAtPosition(pos);
                    
                    // 温度による色分け
                    Color tempColor = Color.Lerp(Color.blue, Color.red, 
                        Mathf.InverseLerp(temperatureRange.x, temperatureRange.y, climate.temperature));
                    
                    Gizmos.color = tempColor;
                    Gizmos.DrawCube(pos, Vector3.one * 20f);
                    
                    // 風向きの表示
                    Gizmos.color = Color.white;
                    Vector3 windDir3D = new Vector3(climate.windDirection.x, 0f, climate.windDirection.y);
                    Gizmos.DrawRay(pos, windDir3D * climate.windSpeed * 5f);
                }
            }
        }
        
        #endregion
        
        #region パブリックメソッド
        
        /// <summary>
        /// 海洋位置を設定
        /// </summary>
        public void SetOceanPositions(Vector2[] positions)
        {
            if (positions != null && positions.Length > 0)
            {
                oceanPositions = positions;
            }
        }
        
        /// <summary>
        /// 季節を設定
        /// </summary>
        public void SetSeason(float season)
        {
            currentSeason = Mathf.Clamp01(season);
        }
        
        /// <summary>
        /// 現在の季節を取得
        /// </summary>
        public float GetCurrentSeason()
        {
            return currentSeason;
        }
        
        /// <summary>
        /// 気候データの範囲を取得
        /// </summary>
        public (Vector2 temperature, Vector2 moisture) GetClimateRanges()
        {
            return (temperatureRange, moistureRange);
        }
        
        #endregion
    }
}