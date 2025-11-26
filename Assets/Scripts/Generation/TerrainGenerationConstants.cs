using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// 地形生成システムで使用される定数値を集約
    /// ハードコーディングを避け、一元管理するためのクラス
    /// </summary>
    public static class TerrainGenerationConstants
    {
        #region Terrain Size Defaults
        /// <summary>デフォルトの地形幅（メートル）</summary>
        public const int DefaultTerrainWidth = 2048;
        
        /// <summary>デフォルトの地形長さ（メートル）</summary>
        public const int DefaultTerrainHeight = 2048;
        
        /// <summary>デフォルトの地形深さ/高さ（メートル）</summary>
        public const int DefaultTerrainDepth = 600;
        
        /// <summary>デフォルトのハイトマップ解像度</summary>
        public const int DefaultHeightmapResolution = 513;
        #endregion

        #region Noise Generation Defaults
        /// <summary>デフォルトのノイズスケール</summary>
        public const float DefaultNoiseScale = 50f;
        
        /// <summary>デフォルトのオクターブ数</summary>
        public const int DefaultOctaves = 8;
        
        /// <summary>デフォルトの持続性（Persistence）</summary>
        public const float DefaultPersistence = 0.5f;
        
        /// <summary>デフォルトのラクナリティ</summary>
        public const float DefaultLacunarity = 2f;
        #endregion

        #region HeightMap Defaults
        /// <summary>デフォルトのハイトマップスケール</summary>
        public const float DefaultHeightMapScale = 1.0f;
        
        /// <summary>デフォルトのハイトマップオフセット</summary>
        public const float DefaultHeightMapOffset = 0.0f;
        #endregion

        #region Detail Settings
        /// <summary>デフォルトのディテール解像度</summary>
        public const int DefaultDetailResolution = 1024;
        
        /// <summary>デフォルトのパッチあたりディテール解像度</summary>
        public const int DefaultDetailResolutionPerPatch = 8;
        
        /// <summary>デフォルトのディテール密度</summary>
        public const float DefaultDetailDensity = 1.0f;
        
        /// <summary>デフォルトのディテール表示距離</summary>
        public const float DefaultDetailDistance = 200f;
        #endregion

        #region Tree Settings
        /// <summary>デフォルトの木表示距離</summary>
        public const int DefaultTreeDistance = 2000;
        
        /// <summary>デフォルトのビルボード切り替え距離</summary>
        public const int DefaultTreeBillboardDistance = 300;
        
        /// <summary>デフォルトのクロスフェード長</summary>
        public const int DefaultTreeCrossFadeLength = 50;
        
        /// <summary>デフォルトの最大フルLOD木数</summary>
        public const int DefaultTreeMaximumFullLODCount = 50;
        #endregion

        #region Batch Processing
        /// <summary>ハイトマップ設定時のバッチサイズ</summary>
        public const int HeightmapBatchSize = 256;
        #endregion

        #region Primitive Generation
        /// <summary>デフォルトのプリミティブサイズ（メートル）</summary>
        public const float DefaultPrimitiveScale = 100f;
        
        /// <summary>デフォルトの変形範囲</summary>
        public const float DefaultDeformationRange = 0.1f;
        
        /// <summary>デフォルトのノイズ強度</summary>
        public const float DefaultNoiseIntensity = 0.05f;
        
        /// <summary>デフォルトの細分化レベル</summary>
        public const int DefaultSubdivisionLevel = 2;
        #endregion

        #region Layer Names
        /// <summary>地形レイヤー名</summary>
        public const string TerrainLayerName = "Terrain";
        #endregion

        #region HeightMap Blending
        /// <summary>ノイズと HeightMap 合成時の正規化係数</summary>
        public const float HeightNormalizationFactor = 0.5f;
        
        /// <summary>最大ノイズ影響係数（平坦部）</summary>
        public const float MaxNoiseInfluence = 0.5f;
        
        /// <summary>最小ノイズ影響係数（急勾配部）</summary>
        public const float MinNoiseInfluence = 0.1f;
        
        /// <summary>グラデーション計算時の乗数</summary>
        public const float GradientMultiplier = 10f;
        
        /// <summary>グラデーション計算時のサンプル半径</summary>
        public const int GradientSampleRadius = 1;
        #endregion
    }
}
