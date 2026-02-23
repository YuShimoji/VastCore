using UnityEngine;

namespace Vastcore.WorldGen.FieldEngine
{
    /// <summary>
    /// Heightmap 設定アセットから IDensityField を構築するファクトリ。
    /// Terrain 側が実装し、WorldGen 側へ注入する。
    /// </summary>
    public interface IHeightmapFieldFactory
    {
        /// <summary>
        /// Heightmap 設定から密度場を作成する。
        /// </summary>
        IDensityField CreateFromSettings(ScriptableObject heightmapSettings, float heightScale, int seed);
    }
}
