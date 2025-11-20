using UnityEngine;
using UnityEngine.ProBuilder;
using PrimitiveType = Vastcore.Generation.PrimitiveTerrainGenerator.PrimitiveType;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// 立方体プリミティブ生成クラス
    /// </summary>
    public class CubeGenerator : BasePrimitiveGenerator
    {
        public override PrimitiveType PrimitiveType => PrimitiveType.Cube;

        public override Vector3 GetDefaultScale() => new Vector3(100f, 100f, 100f);

        public override ProBuilderMesh GeneratePrimitive(Vector3 scale)
        {
            return CreateScaledShape(ShapeType.Cube, scale);
        }
    }
}
