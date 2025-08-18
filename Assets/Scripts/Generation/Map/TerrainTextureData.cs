using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// 地形テクスチャデータ構造
    /// テクスチャウェイト、ブレンド情報、環境変化データを管理
    /// </summary>
    [System.Serializable]
    public class TerrainTextureData
    {
        [Header("テクスチャウェイト")]
        public float[,,] textureWeights;        // [y, x, blend] テクスチャウェイト配列
        public int[,,] textureIndices;          // [y, x, blend] テクスチャインデックス配列
        
        [Header("生成テクスチャ")]
        public Texture2D weightTexture;         // ウェイトテクスチャ
        public Texture2D blendTexture;          // ブレンドテクスチャ
        public Texture2D normalTexture;         // 法線テクスチャ
        
        [Header("色調修正")]
        public Color colorModifier = Color.white;           // 色調修正
        public Color ambientColor = Color.gray;             // 環境色
        public Color fogColor = Color.gray;                 // 霧色
        public float saturationModifier = 1f;               // 彩度修正
        public float brightnessModifier = 1f;               // 明度修正
        public float contrastModifier = 1f;                 // コントラスト修正
        
        [Header("環境データ")]
        public float temperature = 0.5f;        // 温度 (0-1)
        public float moisture = 0.5f;           // 湿度 (0-1)
        public float fertility = 0.5f;          // 肥沃度 (0-1)
        public float rockiness = 0.5f;          // 岩石度 (0-1)
        
        [Header("時間変化")]
        public float timeOfDay = 0.5f;          // 時刻 (0-1)
        public Season currentSeason = Season.Spring;
        
        [Header("メタデータ")]
        public float generationTime;            // 生成時間
        public System.DateTime lastUpdate;      // 最終更新時刻
        public int updateCount;                 // 更新回数
        
        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public TerrainTextureData()
        {
            colorModifier = Color.white;
            ambientColor = Color.gray;
            fogColor = Color.gray;
            saturationModifier = 1f;
            brightnessModifier = 1f;
            contrastModifier = 1f;
            temperature = 0.5f;
            moisture = 0.5f;
            fertility = 0.5f;
            rockiness = 0.5f;
            timeOfDay = 0.5f;
            currentSeason = Season.Spring;
            lastUpdate = System.DateTime.Now;
            updateCount = 0;
        }
        
        /// <summary>
        /// テクスチャデータをクリーンアップ
        /// </summary>
        public void Cleanup()
        {
            if (weightTexture != null)
            {
                Object.DestroyImmediate(weightTexture);
                weightTexture = null;
            }
            
            if (blendTexture != null && blendTexture != weightTexture)
            {
                Object.DestroyImmediate(blendTexture);
                blendTexture = null;
            }
            
            if (normalTexture != null)
            {
                Object.DestroyImmediate(normalTexture);
                normalTexture = null;
            }
            
            textureWeights = null;
            textureIndices = null;
        }
        
        /// <summary>
        /// メモリ使用量を取得（概算）
        /// </summary>
        public long GetMemoryUsage()
        {
            long usage = 0;
            
            if (textureWeights != null)
            {
                usage += textureWeights.Length * sizeof(float);
            }
            
            if (textureIndices != null)
            {
                usage += textureIndices.Length * sizeof(int);
            }
            
            if (weightTexture != null)
            {
                usage += weightTexture.width * weightTexture.height * 4; // RGBA32
            }
            
            if (blendTexture != null && blendTexture != weightTexture)
            {
                usage += blendTexture.width * blendTexture.height * 4;
            }
            
            if (normalTexture != null)
            {
                usage += normalTexture.width * normalTexture.height * 4;
            }
            
            return usage;
        }
        
        /// <summary>
        /// デバッグ情報を取得
        /// </summary>
        public string GetDebugInfo()
        {
            return $"TextureData: Memory={GetMemoryUsage() / 1024}KB, Updates={updateCount}, Season={currentSeason}";
        }
    }
    
    /// <summary>
    /// 高度テクスチャレイヤー
    /// </summary>
    [System.Serializable]
    public class AltitudeTextureLayer
    {
        public string name = "Altitude Layer";
        public float minAltitude = 0f;
        public float maxAltitude = 100f;
        public TerrainTextureType textureType = TerrainTextureType.Grass;
        public float blendStrength = 1f;
        public Vector2 tiling = Vector2.one;
        public Color colorTint = Color.white;
        public bool enabled = true;
    }
    
    /// <summary>
    /// 傾斜テクスチャレイヤー
    /// </summary>
    [System.Serializable]
    public class SlopeTextureLayer
    {
        public string name = "Slope Layer";
        public float minSlope = 0f;
        public float maxSlope = 45f;
        public TerrainTextureType textureType = TerrainTextureType.Rock;
        public float blendStrength = 1f;
        public bool overrideAltitude = false;
        public Color colorTint = Color.white;
        public bool enabled = true;
    }
    
    /// <summary>
    /// LODテクスチャセット
    /// </summary>
    [System.Serializable]
    public class LODTextureSet
    {
        public string name = "LOD Set";
        public int textureResolution = 512;
        public float normalMapStrength = 1f;
        public float detailStrength = 1f;
        public float maxDistance = 1000f;
        public bool enableMipmaps = true;
        public FilterMode filterMode = FilterMode.Bilinear;
    }
    
    /// <summary>
    /// テクスチャウェイト構造
    /// </summary>
    [System.Serializable]
    public struct TextureWeight
    {
        public TerrainTextureType textureType;
        public float weight;
    }
    
    /// <summary>
    /// テクスチャ更新リクエスト
    /// </summary>
    [System.Serializable]
    public class TextureUpdateRequest
    {
        public TerrainTile tile;
        public TextureUpdateType updateType;
        public int priority;
        public float requestTime;
        public object additionalData;
    }
    
    /// <summary>
    /// 環境条件
    /// </summary>
    [System.Serializable]
    public class EnvironmentalConditions
    {
        public Season season = Season.Spring;
        public float temperature = 0.5f;       // 0-1 (寒い-暑い)
        public float moisture = 0.5f;          // 0-1 (乾燥-湿潤)
        public float timeOfDay = 0.5f;         // 0-1 (深夜-正午)
        public float windStrength = 0.5f;      // 0-1 (無風-強風)
        public float precipitation = 0f;       // 0-1 (晴れ-雨)
    }
    
    /// <summary>
    /// 季節変化テクスチャ設定
    /// </summary>
    [System.Serializable]
    public class SeasonalTextureSettings
    {
        [Header("現在の季節")]
        public Season currentSeason = Season.Spring;
        public float currentTemperature = 0.5f;
        public float currentMoisture = 0.5f;
        
        [Header("季節変化速度")]
        public float seasonTransitionSpeed = 1f;
        public bool enableAutomaticSeasonChange = false;
        public float seasonDuration = 300f; // 秒
        
        [Header("季節別設定")]
        public SeasonSettings springSettings = new SeasonSettings();
        public SeasonSettings summerSettings = new SeasonSettings();
        public SeasonSettings autumnSettings = new SeasonSettings();
        public SeasonSettings winterSettings = new SeasonSettings();
        
        /// <summary>
        /// 現在の季節設定を取得
        /// </summary>
        public SeasonSettings GetCurrentSeasonSettings()
        {
            switch (currentSeason)
            {
                case Season.Spring: return springSettings;
                case Season.Summer: return summerSettings;
                case Season.Autumn: return autumnSettings;
                case Season.Winter: return winterSettings;
                default: return springSettings;
            }
        }
    }
    
    /// <summary>
    /// 季節設定
    /// </summary>
    [System.Serializable]
    public class SeasonSettings
    {
        public Color colorTint = Color.white;
        public float temperatureModifier = 0f;
        public float moistureModifier = 0f;
        public float brightnessModifier = 1f;
        public float saturationModifier = 1f;
    }
    
    /// <summary>
    /// 地形テクスチャタイプ
    /// </summary>
    public enum TerrainTextureType
    {
        Grass = 0,      // 草地
        Sand = 1,       // 砂地
        Rock = 2,       // 岩石
        Snow = 3,       // 雪
        Dirt = 4,       // 土壌
        Cliff = 5,      // 崖
        Water = 6,      // 水面
        Ice = 7,        // 氷
        Mud = 8,        // 泥
        Gravel = 9      // 砂利
    }
    
    /// <summary>
    /// テクスチャ更新タイプ
    /// </summary>
    public enum TextureUpdateType
    {
        Full,           // 完全更新
        Environmental,  // 環境変化
        LOD,           // LOD変更
        Biome,         // バイオーム変更
        Seasonal       // 季節変化
    }
    
    /// <summary>
    /// 季節
    /// </summary>
    public enum Season
    {
        Spring,     // 春
        Summer,     // 夏
        Autumn,     // 秋
        Winter      // 冬
    }
}