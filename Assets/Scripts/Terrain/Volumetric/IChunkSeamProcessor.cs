using UnityEngine;

namespace Vastcore.Terrain.Volumetric
{
    /// <summary>
    /// チャンク境界のシーム軽減処理を担うインターフェース。
    /// </summary>
    public interface IChunkSeamProcessor
    {
        /// <summary>
        /// 抽出後メッシュにシーム軽減処理を適用する。
        /// </summary>
        /// <param name="mesh">対象メッシュ。</param>
        /// <param name="chunkSize">チャンクのワールドサイズ。</param>
        /// <param name="voxelSize">ボクセルのワールドサイズ。</param>
        void Process(Mesh mesh, float chunkSize, float voxelSize);
    }
}
