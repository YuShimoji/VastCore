using UnityEngine;

namespace Vastcore.Core
{
    /// <summary>
    /// 地形タイプの定義
    /// 高度合成システムで使用される地形の種類
    /// </summary>
    public enum TerrainType
    {
        Plain,      // 平原 - ベース地形
        Mountain,   // 山岳 - 高高度、高起伏
        Valley,     // 峡谷 - 低高度、大起伏
        Lake,       // 湖 - 水面レベル、平坦
        Forest,     // 森林 - 中高度、中起伏、木生成
        Desert,     // 砂漠 - 低高度、低起伏
        Coast       // 海岸 - 水面近辺、特殊処理
    }

    /// <summary>
    /// 地形タイプの詳細定義
    /// 各地形タイプの特性とパラメータを定義
    /// </summary>
    [System.Serializable]
    public class TerrainTypeDefinition
    {
        [Header("基本情報")]
        public TerrainType type;
        public string displayName;
        public string description;

        [Header("高さ設定")]
        [Range(0f, 1000f)]
        public float baseHeight = 0f;
        [Range(0f, 500f)]
        public float heightVariation = 50f;
        [Range(0.001f, 0.1f)]
        public float noiseScale = 0.01f;

        [Header("外観設定")]
        public Color baseColor = Color.white;
        public Material terrainMaterial;
        public Texture2D heightMapTexture;

        [Header("特殊設定")]
        public bool generatesTrees = false;
        public bool generatesWater = false;
        public float treeDensity = 0.1f;
        [Range(0f, 1f)]
        public float blendStrength = 0.5f;

        /// <summary>
        /// 高さを計算する
        /// </summary>
        public float CalculateHeight(float x, float z, float noiseValue)
        {
            return baseHeight + (noiseValue * heightVariation);
        }

        /// <summary>
        /// デフォルト設定で初期化
        /// </summary>
        public static TerrainTypeDefinition CreateDefault(TerrainType terrainType)
        {
            var definition = new TerrainTypeDefinition();
            definition.type = terrainType;

            switch (terrainType)
            {
                case TerrainType.Plain:
                    definition.displayName = "平原";
                    definition.description = "平坦な基本地形";
                    definition.baseHeight = 10f;
                    definition.heightVariation = 5f;
                    definition.noiseScale = 0.005f;
                    definition.baseColor = new Color(0.4f, 0.6f, 0.2f);
                    definition.blendStrength = 0.8f;
                    break;

                case TerrainType.Mountain:
                    definition.displayName = "山岳";
                    definition.description = "高く起伏のある地形";
                    definition.baseHeight = 200f;
                    definition.heightVariation = 150f;
                    definition.noiseScale = 0.02f;
                    definition.baseColor = new Color(0.5f, 0.5f, 0.5f);
                    definition.blendStrength = 0.6f;
                    break;

                case TerrainType.Valley:
                    definition.displayName = "峡谷";
                    definition.description = "深く刻まれた谷";
                    definition.baseHeight = -50f;
                    definition.heightVariation = 30f;
                    definition.noiseScale = 0.015f;
                    definition.baseColor = new Color(0.3f, 0.4f, 0.2f);
                    definition.blendStrength = 0.7f;
                    break;

                case TerrainType.Lake:
                    definition.displayName = "湖";
                    definition.description = "水面のある平坦なエリア";
                    definition.baseHeight = 0f;
                    definition.heightVariation = 2f;
                    definition.noiseScale = 0.001f;
                    definition.baseColor = new Color(0.2f, 0.4f, 0.8f);
                    definition.generatesWater = true;
                    definition.blendStrength = 0.9f;
                    break;

                case TerrainType.Forest:
                    definition.displayName = "森林";
                    definition.description = "木々が生い茂るエリア";
                    definition.baseHeight = 20f;
                    definition.heightVariation = 15f;
                    definition.noiseScale = 0.008f;
                    definition.baseColor = new Color(0.3f, 0.5f, 0.2f);
                    definition.generatesTrees = true;
                    definition.treeDensity = 0.3f;
                    definition.blendStrength = 0.7f;
                    break;

                case TerrainType.Desert:
                    definition.displayName = "砂漠";
                    definition.description = "乾燥した平坦な地形";
                    definition.baseHeight = 5f;
                    definition.heightVariation = 3f;
                    definition.noiseScale = 0.003f;
                    definition.baseColor = new Color(0.9f, 0.8f, 0.6f);
                    definition.blendStrength = 0.6f;
                    break;

                case TerrainType.Coast:
                    definition.displayName = "海岸";
                    definition.description = "水辺の特殊地形";
                    definition.baseHeight = 2f;
                    definition.heightVariation = 8f;
                    definition.noiseScale = 0.01f;
                    definition.baseColor = new Color(0.8f, 0.7f, 0.5f);
                    definition.blendStrength = 0.8f;
                    break;
            }

            return definition;
        }
    }
}
