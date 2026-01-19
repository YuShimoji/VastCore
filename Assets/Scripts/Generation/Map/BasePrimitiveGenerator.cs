using UnityEngine;
using UnityEngine.ProBuilder;
using System.Linq;
using PrimitiveType = Vastcore.Generation.PrimitiveTerrainGenerator.PrimitiveType;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// プリミティブ生成の基底クラス
    /// </summary>
    public abstract class BasePrimitiveGenerator : IPrimitiveGenerator
    {
        public abstract PrimitiveType PrimitiveType { get; }

        public abstract Vector3 GetDefaultScale();

        public abstract ProBuilderMesh GeneratePrimitive(Vector3 scale);

        /// <summary>
        /// スケール済みシェイプを生成するヘルパーメソッド
        /// </summary>
        protected ProBuilderMesh CreateScaledShape(ShapeType shapeType, Vector3 scale)
        {
            var shape = ShapeGenerator.CreateShape(shapeType);
            if (shape != null)
            {
                shape.transform.localScale = scale;
            }
            return shape;
        }

        /// <summary>
        /// 頂点を変形するヘルパーメソッド
        /// </summary>
        protected void ModifyVertices(ProBuilderMesh mesh, System.Func<Vector3, Vector3> modifier)
        {
            if (mesh == null) return;

            var vertices = mesh.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = modifier(vertices[i]);
            }
            mesh.positions = vertices;
        }
    }
}
