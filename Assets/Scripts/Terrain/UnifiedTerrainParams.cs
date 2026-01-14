using System;
using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// 統一された地形生成パラメータ構造体。
    /// 3つの地形システム（PrimitiveTerrainGenerator, MeshGenerator, TerrainGenerator）間で
    /// 一貫したパラメータを提供するための抽象層。
    /// 
    /// T3ギャップ分析（docs/T3_TERRAIN_GAP_ANALYSIS.md）に基づく方針Aの実装。
    /// </summary>
    [Serializable]
    public struct UnifiedTerrainParams
    {
        #region Basic Settings
        
        /// <summary>
        /// ワールド座標でのサイズ（統一単位: メートル）
        /// </summary>
        public float worldSize;
        
        /// <summary>
        /// 最大標高（統一名称）
        /// - PrimitiveTerrainGenerator: scale.y に変換
        /// - MeshGenerator: maxHeight に変換
        /// - TerrainGenerator: Depth に変換
        /// </summary>
        public float maxElevation;
        
        /// <summary>
        /// メッシュ解像度
        /// - MeshGenerator: resolution に変換
        /// - TerrainGenerator: m_Resolution に変換
        /// </summary>
        public int meshResolution;
        
        #endregion
        
        #region Noise Settings
        
        /// <summary>
        /// ノイズ設定（統一）
        /// </summary>
        public NoiseSettings noiseSettings;
        
        #endregion
        
        #region Output Settings
        
        /// <summary>
        /// 出力タイプ
        /// </summary>
        public TerrainOutputType outputType;
        
        #endregion
        
        #region Factory Methods
        
        /// <summary>
        /// デフォルト設定を返す
        /// </summary>
        public static UnifiedTerrainParams Default()
        {
            return new UnifiedTerrainParams
            {
                worldSize = 1000f,
                maxElevation = 200f,
                meshResolution = 512,
                noiseSettings = NoiseSettings.Default(),
                outputType = TerrainOutputType.Mesh
            };
        }
        
        /// <summary>
        /// 小規模地形用のプリセット
        /// </summary>
        public static UnifiedTerrainParams SmallTerrain()
        {
            return new UnifiedTerrainParams
            {
                worldSize = 256f,
                maxElevation = 50f,
                meshResolution = 256,
                noiseSettings = NoiseSettings.Default(),
                outputType = TerrainOutputType.Mesh
            };
        }
        
        /// <summary>
        /// 大規模地形用のプリセット
        /// </summary>
        public static UnifiedTerrainParams LargeTerrain()
        {
            return new UnifiedTerrainParams
            {
                worldSize = 4096f,
                maxElevation = 600f,
                meshResolution = 1024,
                noiseSettings = NoiseSettings.Default(),
                outputType = TerrainOutputType.UnityTerrain
            };
        }
        
        #endregion
    }
    
    /// <summary>
    /// 統一されたノイズ設定
    /// </summary>
    [Serializable]
    public struct NoiseSettings
    {
        /// <summary>
        /// ノイズタイプ
        /// </summary>
        public NoiseType noiseType;
        
        /// <summary>
        /// ノイズスケール（正規化された値: 0.0 - 1.0）
        /// 各ジェネレータで適切な値に変換される
        /// </summary>
        [Range(0.001f, 1f)]
        public float scale;
        
        /// <summary>
        /// オクターブ数（1-16）
        /// </summary>
        [Range(1, 16)]
        public int octaves;
        
        /// <summary>
        /// 持続性（0.0 - 1.0）
        /// </summary>
        [Range(0f, 1f)]
        public float persistence;
        
        /// <summary>
        /// ラキュナリティ（1.0 - 4.0）
        /// </summary>
        [Range(1f, 4f)]
        public float lacunarity;
        
        /// <summary>
        /// オフセット
        /// </summary>
        public Vector2 offset;
        
        /// <summary>
        /// シード値
        /// </summary>
        public int seed;
        
        /// <summary>
        /// デフォルト設定を返す
        /// </summary>
        public static NoiseSettings Default()
        {
            return new NoiseSettings
            {
                noiseType = NoiseType.Perlin,
                scale = 0.1f,
                octaves = 8,
                persistence = 0.55f,  // MeshGenerator(0.6) と TerrainGenerator(0.5) の中間値
                lacunarity = 2.25f,   // MeshGenerator(2.5) と TerrainGenerator(2.0) の中間値
                offset = Vector2.zero,
                seed = 0
            };
        }
    }
    
    /// <summary>
    /// ノイズタイプ
    /// </summary>
    public enum NoiseType
    {
        Perlin,
        Simplex,
        Ridged,
        Fractal,
        Voronoi
    }
    
    /// <summary>
    /// 地形出力タイプ
    /// </summary>
    public enum TerrainOutputType
    {
        /// <summary>ProBuilderMesh（編集可能な構造物）</summary>
        ProBuilder,
        
        /// <summary>Unity Mesh（カスタム地形メッシュ）</summary>
        Mesh,
        
        /// <summary>Unity Terrain（大規模地形、LOD対応）</summary>
        UnityTerrain
    }
}
