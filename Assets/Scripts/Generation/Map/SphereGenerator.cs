using UnityEngine;
using UnityEngine.ProBuilder;
using PrimitiveType = Vastcore.Generation.PrimitiveTerrainGenerator.PrimitiveType;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// 球体プリミティブ生成クラス
    /// </summary>
    public class SphereGenerator : BasePrimitiveGenerator
    {
        public override PrimitiveType PrimitiveType => PrimitiveType.Sphere;

        public override Vector3 GetDefaultScale() => new Vector3(80f, 80f, 80f);

        public override ProBuilderMesh GeneratePrimitive(Vector3 scale)
        {
            return CreateScaledShape(ShapeType.Sphere, scale);
        }
    }
}
