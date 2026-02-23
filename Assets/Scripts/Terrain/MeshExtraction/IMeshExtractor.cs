using UnityEngine;
using Vastcore.WorldGen.Common;

namespace Vastcore.Terrain.MeshExtraction
{
    /// <summary>
    /// 密度グリッドからメッシュを抽出するインターフェース。
    /// </summary>
    public interface IMeshExtractor
    {
        /// <summary>
        /// 等値面を抽出してメッシュを返す。
        /// </summary>
        Mesh ExtractMesh(DensityGrid grid, float isoLevel, float voxelSize);
    }
}
