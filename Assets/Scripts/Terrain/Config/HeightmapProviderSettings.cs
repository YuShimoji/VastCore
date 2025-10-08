using UnityEngine;
using Vastcore.Terrain.Providers;

namespace Vastcore.Terrain.Config
{
    /// <summary>
    /// Heightmap プロバイダ設定の抽象基底。
    /// 設定アセットからプロバイダを生成する Factory を提供します。
    /// </summary>
    public abstract class HeightmapProviderSettings : ScriptableObject
    {
        public abstract IHeightmapProvider CreateProvider();
    }
}
