using UnityEngine;

namespace Vastcore.Terrain.DualGrid
{
    /// <summary>
    /// XZ 座標からワールド高さを返すインターフェース。
    /// DualGrid スタンプ配置が Unity Terrain に直接依存しないよう抽象化する。
    /// </summary>
    public interface IHeightSampler
    {
        /// <summary>
        /// 指定された XZ ワールド座標の地面高さを返す。
        /// </summary>
        /// <param name="_worldX">ワールド X 座標</param>
        /// <param name="_worldZ">ワールド Z 座標</param>
        /// <returns>ワールド Y 座標 (地面高さ)</returns>
        float SampleHeight(float _worldX, float _worldZ);
    }

    /// <summary>
    /// Unity Terrain コンポーネントから高さをサンプリングする IHeightSampler 実装。
    /// </summary>
    public class UnityTerrainHeightSampler : IHeightSampler
    {
        private readonly UnityEngine.Terrain m_Terrain;

        public UnityTerrainHeightSampler(UnityEngine.Terrain _terrain)
        {
            m_Terrain = _terrain;
        }

        public float SampleHeight(float _worldX, float _worldZ)
        {
            if (m_Terrain == null) return 0f;
            return m_Terrain.SampleHeight(new Vector3(_worldX, 0f, _worldZ));
        }
    }
}
