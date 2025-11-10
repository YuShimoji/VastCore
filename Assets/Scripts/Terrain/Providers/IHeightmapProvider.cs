using UnityEngine;

namespace Vastcore.Terrain.Providers
{
    /// <summary>
    /// 抽象化された高さデータ供給源。出力は [0,1] 正規化。
    /// worldOrigin/worldSize はワールド座標系で一貫。
    /// </summary>
    public interface IHeightmapProvider
    {
        /// <summary>
        /// 出力配列 heights の長さは resolution*resolution を期待。
        /// インデクスは x + y*resolution（x:0..res-1, y:0..res-1）。
        /// </summary>
        void Generate(float[] heights, int resolution, Vector2 worldOrigin, float worldSize, in HeightmapGenerationContext context);
    }

    /// <summary>
    /// 生成時のコンテキスト（seed など）。
    /// </summary>
    public struct HeightmapGenerationContext
    {
        public int Seed;
    }
}
