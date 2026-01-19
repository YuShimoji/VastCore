using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// 地形生成プロファイル - 生成パラメータを ScriptableObject として保存・再利用可能にする
    /// TerrainGenerationV0_Spec.md に基づく設計
    /// </summary>
    [CreateAssetMenu(fileName = "NewTerrainGenerationProfile", menuName = "Vastcore/Terrain/Generation Profile", order = 1)]
    public class TerrainGenerationProfile : ScriptableObject
    {
        #region Generation Mode
        [Header("Generation Mode")]
        [Tooltip("地形生成モード: Noise のみ / HeightMap のみ / 両方の組み合わせ")]
        [SerializeField] private TerrainGenerationMode m_GenerationMode = TerrainGenerationMode.Noise;
        
        public TerrainGenerationMode GenerationMode
        {
            get => m_GenerationMode;
            set => m_GenerationMode = value;
        }
        #endregion

        #region Template Settings
        [Header("Template Settings")]
        [SerializeField] private DesignerTerrainTemplate m_Template;

        public DesignerTerrainTemplate Template
        {
            get => m_Template;
            set => m_Template = value;
        }
        #endregion

        #region Terrain Size & Resolution
        [Header("Terrain Size & Resolution")]
        [Tooltip("地形の幅（メートル）")]
        [SerializeField] private float m_TerrainWidth = TerrainGenerationConstants.DefaultTerrainWidth;
        
        [Tooltip("地形の長さ（メートル）")]
        [SerializeField] private float m_TerrainLength = TerrainGenerationConstants.DefaultTerrainHeight;
        
        [Tooltip("地形の高さ（メートル）")]
        [SerializeField] private float m_TerrainHeight = TerrainGenerationConstants.DefaultTerrainDepth;
        
        [Tooltip("ハイトマップ解像度（129, 257, 513 推奨）")]
        [SerializeField] private int m_HeightmapResolution = TerrainGenerationConstants.DefaultHeightmapResolution;

        public float TerrainWidth
        {
            get => m_TerrainWidth;
            set => m_TerrainWidth = Mathf.Max(1f, value);
        }

        public float TerrainLength
        {
            get => m_TerrainLength;
            set => m_TerrainLength = Mathf.Max(1f, value);
        }

        public float TerrainHeight
        {
            get => m_TerrainHeight;
            set => m_TerrainHeight = Mathf.Max(1f, value);
        }

        public int HeightmapResolution
        {
            get => m_HeightmapResolution;
            set => m_HeightmapResolution = ValidateHeightmapResolution(value);
        }

        public Vector3 TerrainSize => new Vector3(m_TerrainWidth, m_TerrainHeight, m_TerrainLength);
        #endregion

        #region HeightMap Settings
        [Header("HeightMap Settings")]
        [Tooltip("高さマップテクスチャ（グレースケール画像）")]
        [SerializeField] private Texture2D m_HeightMapTexture;
        
        [Tooltip("高さマップから読み取るチャンネル")]
        [SerializeField] private HeightMapChannel m_HeightMapChannel = HeightMapChannel.Luminance;
        
        [Tooltip("高さスケール係数")]
        [Range(0f, 5f)]
        [SerializeField] private float m_HeightScale = TerrainGenerationConstants.DefaultHeightMapScale;
        
        [Tooltip("UV オフセット")]
        [SerializeField] private Vector2 m_UVOffset = Vector2.zero;
        
        [Tooltip("UV タイリング")]
        [SerializeField] private Vector2 m_UVTiling = Vector2.one;
        
        [Tooltip("高さを反転するか")]
        [SerializeField] private bool m_InvertHeight = false;

        public Texture2D HeightMapTexture
        {
            get => m_HeightMapTexture;
            set => m_HeightMapTexture = value;
        }

        public HeightMapChannel HeightMapChannel
        {
            get => m_HeightMapChannel;
            set => m_HeightMapChannel = value;
        }

        public float HeightScale
        {
            get => m_HeightScale;
            set => m_HeightScale = Mathf.Clamp(value, 0f, 5f);
        }

        public Vector2 UVOffset
        {
            get => m_UVOffset;
            set => m_UVOffset = value;
        }

        public Vector2 UVTiling
        {
            get => m_UVTiling;
            set => m_UVTiling = value;
        }

        public bool InvertHeight
        {
            get => m_InvertHeight;
            set => m_InvertHeight = value;
        }
        #endregion

        #region Noise Settings
        [Header("Noise Settings")]
        [Tooltip("ノイズシード値")]
        [SerializeField] private int m_Seed = 0;
        
        [Tooltip("ノイズスケール")]
        [Range(1f, 1000f)]
        [SerializeField] private float m_NoiseScale = TerrainGenerationConstants.DefaultNoiseScale;
        
        [Tooltip("オクターブ数")]
        [Range(1, 8)]
        [SerializeField] private int m_Octaves = TerrainGenerationConstants.DefaultOctaves;
        
        [Tooltip("持続性（各オクターブの振幅減衰率）")]
        [Range(0f, 1f)]
        [SerializeField] private float m_Persistence = TerrainGenerationConstants.DefaultPersistence;
        
        [Tooltip("ラクナリティ（各オクターブの周波数増加率）")]
        [Range(1f, 4f)]
        [SerializeField] private float m_Lacunarity = TerrainGenerationConstants.DefaultLacunarity;
        
        [Tooltip("ノイズサンプル位置のオフセット")]
        [SerializeField] private Vector2 m_NoiseOffset = Vector2.zero;

        public int Seed
        {
            get => m_Seed;
            set => m_Seed = value;
        }

        public float NoiseScale
        {
            get => m_NoiseScale;
            set => m_NoiseScale = Mathf.Clamp(value, 1f, 1000f);
        }

        public int Octaves
        {
            get => m_Octaves;
            set => m_Octaves = Mathf.Clamp(value, 1, 8);
        }

        public float Persistence
        {
            get => m_Persistence;
            set => m_Persistence = Mathf.Clamp01(value);
        }

        public float Lacunarity
        {
            get => m_Lacunarity;
            set => m_Lacunarity = Mathf.Clamp(value, 1f, 4f);
        }

        public Vector2 NoiseOffset
        {
            get => m_NoiseOffset;
            set => m_NoiseOffset = value;
        }
        #endregion

        #region Validation
        /// <summary>
        /// ハイトマップ解像度を有効な値に丸める（2^n + 1 形式）
        /// </summary>
        private static int ValidateHeightmapResolution(int value)
        {
            int[] validResolutions = { 33, 65, 129, 257, 513, 1025, 2049, 4097 };
            
            int closest = validResolutions[0];
            int minDiff = Mathf.Abs(value - closest);
            
            foreach (int res in validResolutions)
            {
                int diff = Mathf.Abs(value - res);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = res;
                }
            }
            
            return closest;
        }
        #endregion

        #region Utility Methods
        /// <summary>
        /// プロファイルをランダムシードで初期化
        /// </summary>
        public void RandomizeSeed()
        {
            m_Seed = Random.Range(int.MinValue, int.MaxValue);
        }

        /// <summary>
        /// プロファイルをデフォルト値にリセット
        /// </summary>
        public void ResetToDefaults()
        {
            m_Template = null;
            m_GenerationMode = TerrainGenerationMode.Noise;
            m_TerrainWidth = TerrainGenerationConstants.DefaultTerrainWidth;
            m_TerrainLength = TerrainGenerationConstants.DefaultTerrainHeight;
            m_TerrainHeight = TerrainGenerationConstants.DefaultTerrainDepth;
            m_HeightmapResolution = TerrainGenerationConstants.DefaultHeightmapResolution;
            
            m_HeightMapTexture = null;
            m_HeightMapChannel = HeightMapChannel.Luminance;
            m_HeightScale = TerrainGenerationConstants.DefaultHeightMapScale;
            m_UVOffset = Vector2.zero;
            m_UVTiling = Vector2.one;
            m_InvertHeight = false;
            
            m_Seed = 0;
            m_NoiseScale = TerrainGenerationConstants.DefaultNoiseScale;
            m_Octaves = TerrainGenerationConstants.DefaultOctaves;
            m_Persistence = TerrainGenerationConstants.DefaultPersistence;
            m_Lacunarity = TerrainGenerationConstants.DefaultLacunarity;
            m_NoiseOffset = Vector2.zero;
        }

        /// <summary>
        /// 別のプロファイルから値をコピー
        /// </summary>
        public void CopyFrom(TerrainGenerationProfile other)
        {
            if (other == null) return;

            m_Template = other.m_Template;
            m_GenerationMode = other.m_GenerationMode;
            m_TerrainWidth = other.m_TerrainWidth;
            m_TerrainLength = other.m_TerrainLength;
            m_TerrainHeight = other.m_TerrainHeight;
            m_HeightmapResolution = other.m_HeightmapResolution;
            
            m_HeightMapTexture = other.m_HeightMapTexture;
            m_HeightMapChannel = other.m_HeightMapChannel;
            m_HeightScale = other.m_HeightScale;
            m_UVOffset = other.m_UVOffset;
            m_UVTiling = other.m_UVTiling;
            m_InvertHeight = other.m_InvertHeight;
            
            m_Seed = other.m_Seed;
            m_NoiseScale = other.m_NoiseScale;
            m_Octaves = other.m_Octaves;
            m_Persistence = other.m_Persistence;
            m_Lacunarity = other.m_Lacunarity;
            m_NoiseOffset = other.m_NoiseOffset;
        }
        #endregion
    }

    /// <summary>
    /// 高さマップから読み取るチャンネル
    /// </summary>
    public enum HeightMapChannel
    {
        R,
        G,
        B,
        A,
        Luminance
    }
}
