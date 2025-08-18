using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// 気候データ構造体
    /// 特定位置の気候情報を格納
    /// </summary>
    [System.Serializable]
    public struct ClimateData
    {
        [Header("温度データ")]
        public float temperature;           // 温度（摂氏）
        public float temperatureVariation;  // 日較差
        
        [Header("湿度データ")]
        public float moisture;              // 年間降水量（mm）
        public float humidity;              // 相対湿度（%）
        
        [Header("風データ")]
        public Vector2 windDirection;       // 風向き（正規化ベクトル）
        public float windSpeed;             // 風速（m/s）
        
        [Header("季節データ")]
        public float seasonalTemperatureVariation; // 季節温度変動
        public float seasonalMoistureVariation;    // 季節降水変動
        
        [Header("地形影響")]
        public float elevationEffect;       // 標高による影響
        public float oceanDistance;         // 海洋からの距離
        public float continentalityIndex;   // 大陸性指数
        
        /// <summary>
        /// デフォルト気候データを作成
        /// </summary>
        public static ClimateData Default => new ClimateData
        {
            temperature = 20f,
            temperatureVariation = 10f,
            moisture = 1000f,
            humidity = 60f,
            windDirection = Vector2.right,
            windSpeed = 5f,
            seasonalTemperatureVariation = 15f,
            seasonalMoistureVariation = 500f,
            elevationEffect = 0f,
            oceanDistance = 1000f,
            continentalityIndex = 0.5f
        };
        
        /// <summary>
        /// 気候データの妥当性をチェック
        /// </summary>
        public bool IsValid()
        {
            return temperature > -100f && temperature < 100f &&
                   moisture >= 0f && moisture < 10000f &&
                   humidity >= 0f && humidity <= 100f &&
                   windSpeed >= 0f && windSpeed < 200f &&
                   windDirection.magnitude > 0.1f;
        }
        
        /// <summary>
        /// 気候データを正規化
        /// </summary>
        public ClimateData Normalize()
        {
            var normalized = this;
            normalized.temperature = Mathf.Clamp(temperature, -50f, 50f);
            normalized.moisture = Mathf.Clamp(moisture, 0f, 5000f);
            normalized.humidity = Mathf.Clamp(humidity, 0f, 100f);
            normalized.windSpeed = Mathf.Clamp(windSpeed, 0f, 50f);
            normalized.windDirection = windDirection.normalized;
            return normalized;
        }
        
        /// <summary>
        /// 気候タイプを判定
        /// </summary>
        public ClimateType GetClimateType()
        {
            // ケッペンの気候区分に基づく簡易判定
            if (temperature < 0f)
            {
                return ClimateType.Polar;
            }
            else if (temperature > 30f && moisture < 500f)
            {
                return ClimateType.Desert;
            }
            else if (temperature > 25f && moisture > 2000f)
            {
                return ClimateType.Tropical;
            }
            else if (temperature > 15f && moisture > 1000f)
            {
                return ClimateType.Temperate;
            }
            else if (temperature < 10f)
            {
                return ClimateType.Subarctic;
            }
            else
            {
                return ClimateType.Continental;
            }
        }
        
        /// <summary>
        /// 文字列表現を取得
        /// </summary>
        public override string ToString()
        {
            return $"Climate: {temperature:F1}°C, {moisture:F0}mm, Wind: {windSpeed:F1}m/s {windDirection}";
        }
    }
    
    /// <summary>
    /// 気候タイプ列挙型
    /// </summary>
    public enum ClimateType
    {
        Tropical,       // 熱帯
        Desert,         // 砂漠
        Temperate,      // 温帯
        Continental,    // 大陸性
        Subarctic,      // 亜寒帯
        Polar          // 極地
    }
    
    /// <summary>
    /// 季節データ構造体
    /// </summary>
    [System.Serializable]
    public struct SeasonalData
    {
        public float spring;    // 春の値
        public float summer;    // 夏の値
        public float autumn;    // 秋の値
        public float winter;    // 冬の値
        
        /// <summary>
        /// 季節に基づいて値を取得
        /// </summary>
        public float GetValueForSeason(float seasonProgress)
        {
            // seasonProgress: 0-1の範囲で季節を表現
            float normalizedSeason = seasonProgress * 4f;
            int seasonIndex = Mathf.FloorToInt(normalizedSeason);
            float seasonLerp = normalizedSeason - seasonIndex;
            
            switch (seasonIndex % 4)
            {
                case 0: return Mathf.Lerp(spring, summer, seasonLerp);
                case 1: return Mathf.Lerp(summer, autumn, seasonLerp);
                case 2: return Mathf.Lerp(autumn, winter, seasonLerp);
                case 3: return Mathf.Lerp(winter, spring, seasonLerp);
                default: return spring;
            }
        }
    }
}