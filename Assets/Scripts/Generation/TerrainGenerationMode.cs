namespace Vastcore.Generation
{
    /// <summary>
    /// 地形生成モード
    /// Noise / HeightMap / 両方の組み合わせ
    /// </summary>
    public enum TerrainGenerationMode
    {
        /// <summary>Perlin Noise のみで地形を生成</summary>
        Noise,
        
        /// <summary>HeightMap テクスチャのみで地形を生成</summary>
        HeightMap,
        
        /// <summary>HeightMap と Noise を組み合わせて生成</summary>
        NoiseAndHeightMap
    }
}
