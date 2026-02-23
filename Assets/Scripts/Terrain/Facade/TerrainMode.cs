namespace Vastcore.Terrain.Facade
{
    /// <summary>
    /// 地形生成モード。
    /// </summary>
    public enum TerrainMode
    {
        /// <summary>
        /// 既存の HeightMap + Unity Terrain フローを使用する。
        /// </summary>
        Classic,
        /// <summary>
        /// 新規の IDensityField ベース生成を使用する。
        /// </summary>
        Volumetric,
        /// <summary>
        /// HeightMap を大域 prior として、局所 Volumetric を重ねる。
        /// </summary>
        Hybrid
    }
}
