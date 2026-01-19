using UnityEngine;
using UnityEngine.ProBuilder;
using PrimitiveType = Vastcore.Generation.PrimitiveTerrainGenerator.PrimitiveType;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// 円柱プリミティブ生成クラス
    /// </summary>
    public class CylinderGenerator : BasePrimitiveGenerator
    {
        public override PrimitiveType PrimitiveType => PrimitiveType.Cylinder;

        public override Vector3 GetDefaultScale() => new Vector3(60f, 150f, 60f);

        public override ProBuilderMesh GeneratePrimitive(Vector3 scale)
        {
            return CreateScaledShape(ShapeType.Cylinder, scale);
        }
    }

    /// <summary>
    /// ピラミッドプリミティブ生成クラス
    /// </summary>
    public class PyramidGenerator : BasePrimitiveGenerator
    {
        public override PrimitiveType PrimitiveType => PrimitiveType.Pyramid;

        public override Vector3 GetDefaultScale() => new Vector3(120f, 200f, 120f);

        public override ProBuilderMesh GeneratePrimitive(Vector3 scale)
        {
            // ProBuilderにはピラミッドがないので、カスタム生成
            var pyramid = CreateScaledShape(ShapeType.Cube, scale);

            // 上部の頂点を中央に移動してピラミッド形状を作成
            ModifyVertices(pyramid, vertex =>
            {
                if (vertex.y > 0) // 上部の頂点
                {
                    return new Vector3(0, vertex.y, 0);
                }
                return vertex;
            });

            return pyramid;
        }
    }
}
