namespace Vastcore.WorldGen.Recipe
{
    /// <summary>
    /// フィールドレイヤーの種類。
    /// </summary>
    public enum FieldLayerType
    {
        /// <summary>
        /// 2D HeightMap を密度場へ射影する大域 prior レイヤー。
        /// </summary>
        Heightmap,
        /// <summary>
        /// 3D ノイズによる密度レイヤー。
        /// </summary>
        NoiseDensity,
        /// <summary>
        /// SDF スタンプ群を適用するレイヤー。
        /// </summary>
        SDF,
        /// <summary>
        /// 3D ノイズ減算による洞窟カービング。
        /// </summary>
        Cave
    }
}
