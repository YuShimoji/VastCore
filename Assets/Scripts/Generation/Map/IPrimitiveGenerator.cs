using UnityEngine;
using UnityEngine.ProBuilder;
using PrimitiveType = Vastcore.Generation.PrimitiveTerrainGenerator.PrimitiveType;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// プリミティブ生成インターフェース
    /// </summary>
    public interface IPrimitiveGenerator
    {
        /// <summary>
        /// プリミティブタイプ
        /// </summary>
        PrimitiveType PrimitiveType { get; }

        /// <summary>
        /// デフォルトスケールを取得
        /// </summary>
        Vector3 GetDefaultScale();

        /// <summary>
        /// プリミティブを生成
        /// </summary>
        ProBuilderMesh GeneratePrimitive(Vector3 scale);
    }
}
