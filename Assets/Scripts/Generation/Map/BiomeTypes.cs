using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// バイオームタイプ列挙型
    /// </summary>
    public enum BiomeType
    {
        Grassland,    // 草原
        Forest,       // 森林
        Desert,       // 砂漠
        Mountain,     // 山岳
        Polar,        // 極地
        Coastal       // 海岸
    }

    /// <summary>
    /// バイオーム定義クラス
    /// </summary>
    [System.Serializable]
    public class BiomeDefinition
    {
        public BiomeType biomeType;
        public string name = "";
        public Vector2 temperatureRange = new Vector2(-20f, 40f);
        public Vector2 moistureRange = new Vector2(0f, 2000f);
        public Vector2 elevationRange = new Vector2(-50f, 1000f);
        public TerrainModificationData terrainModifiers = new TerrainModificationData();
    }

    /// <summary>
    /// 地形修正データ
    /// </summary>
    [System.Serializable]
    public class TerrainModificationData
    {
        public float heightMultiplier = 1f;
        public float roughnessMultiplier = 1f;
        public float erosionStrength = 0f;
        public float sedimentationRate = 0f;

        // 特殊地形生成パラメータ
        public bool enableDuneGeneration = false;
        public float duneFrequency = 0.02f;
        public float duneAmplitude = 15f;

        public bool enableRidgeGeneration = false;
        public float ridgeFrequency = 0.008f;
        public float ridgeAmplitude = 25f;

        public bool enablePeakGeneration = false;
        public float peakFrequency = 0.003f;
        public float peakAmplitude = 100f;

        public bool enableBeachGeneration = false;
        public float beachWidth = 50f;
        public float beachSlope = 0.1f;

        public bool enableGlacialGeneration = false;
        public float glacialSmoothness = 0.9f;
        public float glacialDepth = 20f;

        public bool enableRollingHills = false;
        public float hillFrequency = 0.01f;
        public float hillAmplitude = 20f;
    }

    /// <summary>
    /// 地形特徴データ
    /// </summary>
    [System.Serializable]
    public struct TerrainFeatures
    {
        public float averageHeight;
        public float heightRange;
        public float averageSlope;
        public float roughness;
    }
}
