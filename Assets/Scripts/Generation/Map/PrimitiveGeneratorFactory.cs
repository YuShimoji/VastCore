using System;
using System.Collections.Generic;
using UnityEngine;
using PrimitiveType = Vastcore.Generation.PrimitiveTerrainGenerator.PrimitiveType;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// プリミティブ生成ファクトリクラス
    /// </summary>
    public static class PrimitiveGeneratorFactory
    {
        private static readonly Dictionary<PrimitiveType, Type> generatorTypes = new Dictionary<PrimitiveType, Type>
        {
            { PrimitiveType.Cube, typeof(CubeGenerator) },
            { PrimitiveType.Sphere, typeof(SphereGenerator) },
            { PrimitiveType.Cylinder, typeof(CylinderGenerator) },
            { PrimitiveType.Pyramid, typeof(PyramidGenerator) },
        };

        /// <summary>
        /// 指定されたプリミティブタイプの生成器を作成
        /// </summary>
        public static IPrimitiveGenerator CreateGenerator(PrimitiveType type)
        {
            if (generatorTypes.TryGetValue(type, out var generatorType))
            {
                return (IPrimitiveGenerator)Activator.CreateInstance(generatorType);
            }

            throw new NotSupportedException($"Primitive type {type} is not supported.");
        }

        /// <summary>
        /// デフォルトスケールを取得
        /// </summary>
        public static Vector3 GetDefaultScale(PrimitiveType type)
        {
            var generator = CreateGenerator(type);
            return generator.GetDefaultScale();
        }
    }
}
