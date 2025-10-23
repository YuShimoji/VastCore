using UnityEngine;

namespace Vastcore.Terrain
{
    /// <summary>
    /// プリミティブ地形生成ルール
    /// バイオームごとのプリミティブ生成条件とパラメータを定義
    /// </summary>
    [System.Serializable]
    public class PrimitiveTerrainRule
    {
        [Header("基本設定")]
        public string ruleName = "Default Rule";
        public PrimitiveType primitiveType = PrimitiveType.Sphere;
        [Range(0.1f, 10f)]
        public float scale = 1f;
        [Range(0f, 1f)]
        public float spawnProbability = 0.5f;

        [Header("位置条件")]
        public TerrainType requiredTerrainType = TerrainType.Plain;
        [Range(0f, 1f)]
        public float minHeight = 0f;
        [Range(0f, 1f)]
        public float maxHeight = 1f;
        [Range(0f, 90f)]
        public float maxSlopeAngle = 45f;

        [Header("環境条件")]
        [Range(0f, 1f)]
        public float minMoisture = 0f;
        [Range(0f, 1f)]
        public float maxMoisture = 1f;
        [Range(0f, 1f)]
        public float minTemperature = 0f;
        [Range(0f, 1f)]
        public float maxTemperature = 1f;

        [Header("配置設定")]
        [Range(1, 100)]
        public int minClusterSize = 1;
        [Range(1, 100)]
        public int maxClusterSize = 5;
        [Range(10f, 1000f)]
        public float minDistanceBetweenClusters = 100f;

        [Header("外観設定")]
        public Material materialOverride;
        public Color colorVariation = Color.white;
        [Range(0f, 1f)]
        public float colorVariationStrength = 0.2f;

        /// <summary>
        /// 指定された条件でこのルールが適用可能かを判定
        /// </summary>
        public bool CanApplyRule(TerrainType terrainType, float height, float slopeAngle,
                                float moisture = 0.5f, float temperature = 0.5f)
        {
            // 地形タイプチェック
            if (terrainType != requiredTerrainType && requiredTerrainType != TerrainType.Plain)
                return false;

            // 高さチェック
            if (height < minHeight || height > maxHeight)
                return false;

            // 斜面角度チェック
            if (slopeAngle > maxSlopeAngle)
                return false;

            // 環境条件チェック
            if (moisture < minMoisture || moisture > maxMoisture)
                return false;

            if (temperature < minTemperature || temperature > maxTemperature)
                return false;

            return true;
        }

        /// <summary>
        /// ルールの妥当性を検証
        /// </summary>
        public bool ValidateRule()
        {
            if (string.IsNullOrEmpty(ruleName))
            {
                Debug.LogWarning("PrimitiveTerrainRule: ルール名が設定されていません");
                return false;
            }

            if (scale <= 0f)
            {
                Debug.LogWarning("PrimitiveTerrainRule: スケールが無効です");
                return false;
            }

            if (minHeight > maxHeight)
            {
                Debug.LogWarning("PrimitiveTerrainRule: 高さ範囲が無効です");
                return false;
            }

            if (minMoisture > maxMoisture || minTemperature > maxTemperature)
            {
                Debug.LogWarning("PrimitiveTerrainRule: 環境条件範囲が無効です");
                return false;
            }

            if (minClusterSize > maxClusterSize)
            {
                Debug.LogWarning("PrimitiveTerrainRule: クラスタサイズ範囲が無効です");
                return false;
            }

            return true;
        }

        /// <summary>
        /// ルールのコピーを作成
        /// </summary>
        public PrimitiveTerrainRule CreateCopy()
        {
            return new PrimitiveTerrainRule
            {
                ruleName = ruleName,
                primitiveType = primitiveType,
                scale = scale,
                spawnProbability = spawnProbability,
                requiredTerrainType = requiredTerrainType,
                minHeight = minHeight,
                maxHeight = maxHeight,
                maxSlopeAngle = maxSlopeAngle,
                minMoisture = minMoisture,
                maxMoisture = maxMoisture,
                minTemperature = minTemperature,
                maxTemperature = maxTemperature,
                minClusterSize = minClusterSize,
                maxClusterSize = maxClusterSize,
                minDistanceBetweenClusters = minDistanceBetweenClusters,
                materialOverride = materialOverride,
                colorVariation = colorVariation,
                colorVariationStrength = colorVariationStrength
            };
        }

        /// <summary>
        /// デフォルト設定で初期化
        /// </summary>
        public static PrimitiveTerrainRule CreateDefault(PrimitiveType type = PrimitiveType.Sphere)
        {
            return new PrimitiveTerrainRule
            {
                ruleName = $"{type} Rule",
                primitiveType = type,
                scale = 1f,
                spawnProbability = 0.3f,
                requiredTerrainType = TerrainType.Plain,
                minHeight = 0.1f,
                maxHeight = 0.9f,
                maxSlopeAngle = 30f,
                minMoisture = 0.2f,
                maxMoisture = 0.8f,
                minTemperature = 0.3f,
                maxTemperature = 0.7f,
                minClusterSize = 1,
                maxClusterSize = 3,
                minDistanceBetweenClusters = 200f,
                colorVariation = Color.white,
                colorVariationStrength = 0.1f
            };
        }
    }
}
