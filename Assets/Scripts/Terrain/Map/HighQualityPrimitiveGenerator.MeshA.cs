using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Collections.Generic;
using System.Linq;
using Vastcore.Core;

#if DEFORM_AVAILABLE
using Deform;
#endif

namespace Vastcore.Generation
{
    public static partial class HighQualityPrimitiveGenerator
    {
        #region 高品質メッシュ生成
        /// <summary>
        /// 高品質メッシュを生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityMesh(
            PrimitiveTerrainGenerator.PrimitiveType primitiveType,
            Vector3 scale,
            QualitySettings quality)
        {
            ProBuilderMesh mesh = null;

            switch (primitiveType)
            {
                case PrimitiveTerrainGenerator.PrimitiveType.Cube:
                    mesh = GenerateHighQualityCube(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Sphere:
                    mesh = GenerateHighQualitySphere(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Cylinder:
                    mesh = GenerateHighQualityCylinder(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Pyramid:
                    mesh = GenerateHighQualityPyramid(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Torus:
                    mesh = GenerateHighQualityTorus(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Prism:
                    mesh = GenerateHighQualityPrism(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Cone:
                    mesh = GenerateHighQualityCone(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Octahedron:
                    mesh = GenerateHighQualityOctahedron(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Crystal:
                    mesh = GenerateHighQualityCrystal(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Monolith:
                    mesh = GenerateHighQualityMonolith(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Arch:
                    mesh = GenerateHighQualityArch(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Ring:
                    mesh = GenerateHighQualityRing(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Mesa:
                    mesh = GenerateHighQualityMesa(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Spire:
                    mesh = GenerateHighQualitySpire(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Boulder:
                    mesh = GenerateHighQualityBoulder(scale, quality);
                    break;
                case PrimitiveTerrainGenerator.PrimitiveType.Formation:
                    mesh = GenerateHighQualityFormation(scale, quality);
                    break;
                default:
                    Debug.LogWarning($"Primitive type {primitiveType} not implemented, using high-quality cube");
                    mesh = GenerateHighQualityCube(scale, quality);
                    break;
            }

            return mesh;
        }

        /// <summary>
        /// 高品質立方体を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityCube(Vector3 scale, QualitySettings quality)
        {
            var cube = ShapeGenerator.CreateShape(ShapeType.Cube, PivotLocation.Center);
            cube.transform.localScale = scale;
            
            // 高品質処理
            if (quality.subdivisionLevel > 0)
            {
                for (int i = 0; i < quality.subdivisionLevel; i++)
                {
                    ConnectElements.Connect(cube, cube.faces);
                }
            }
            
            // 詳細な変形
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedCubeDeformation(cube, quality);
            }
            
            // Deform統合
            ApplyDeformComponents(cube, quality, PrimitiveTerrainGenerator.PrimitiveType.Cube);
            
            return cube;
        }

        /// <summary>
        /// 高品質球体を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualitySphere(Vector3 scale, QualitySettings quality)
        {
            // より高解像度の球体を生成
            int subdivisions = Mathf.Max(2, quality.subdivisionLevel + 2);
            var sphere = ShapeGenerator.CreateShape(ShapeType.Sphere, PivotLocation.Center);
            sphere.transform.localScale = scale;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedSphereDeformation(sphere, quality);
            }
            
            // Deform統合
            ApplyDeformComponents(sphere, quality, PrimitiveTerrainGenerator.PrimitiveType.Sphere);
            
            return sphere;
        }

        /// <summary>
        /// 高品質円柱を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityCylinder(Vector3 scale, QualitySettings quality)
        {
            int sides = Mathf.Max(8, quality.subdivisionLevel * 4 + 8);
            var cylinder = ShapeGenerator.CreateShape(ShapeType.Cylinder, PivotLocation.Center);
            cylinder.transform.localScale = scale;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedCylinderDeformation(cylinder, quality);
            }
            
            // Deform統合
            ApplyDeformComponents(cylinder, quality, PrimitiveTerrainGenerator.PrimitiveType.Cylinder);
            
            return cylinder;
        }

        /// <summary>
        /// 高品質ピラミッドを生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityPyramid(Vector3 scale, QualitySettings quality)
        {
            var pyramid = ShapeGenerator.CreateShape(ShapeType.Cube, PivotLocation.Center);
            
            // ピラミッド形状に変形
            var vertices = pyramid.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].y > 0) // 上部の頂点
                {
                    vertices[i] = new Vector3(0, vertices[i].y, 0);
                }
            }
            pyramid.positions = vertices;
            pyramid.transform.localScale = scale;
            
            // 高品質処理
            if (quality.subdivisionLevel > 0)
            {
                for (int i = 0; i < quality.subdivisionLevel; i++)
                {
                    ConnectElements.Connect(pyramid, pyramid.faces);
                }
            }
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedPyramidDeformation(pyramid, quality);
            }
            
            // Deform統合
            ApplyDeformComponents(pyramid, quality, PrimitiveTerrainGenerator.PrimitiveType.Pyramid);
            
            return pyramid;
        }

        /// <summary>
        /// 高品質トーラスを生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityTorus(Vector3 scale, QualitySettings quality)
        {
            int rows = Mathf.Max(8, quality.subdivisionLevel * 2 + 8);
            int columns = Mathf.Max(12, quality.subdivisionLevel * 3 + 12);
            
            var torus = ShapeGenerator.CreateShape(ShapeType.Torus, PivotLocation.Center);
            torus.transform.localScale = scale;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedTorusDeformation(torus, quality);
            }
            
            // Deform統合
            ApplyDeformComponents(torus, quality, PrimitiveTerrainGenerator.PrimitiveType.Torus);
            
            return torus;
        }

        /// <summary>
        /// 高品質角柱を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityPrism(Vector3 scale, QualitySettings quality)
        {
            int sides = Mathf.Max(6, quality.subdivisionLevel * 2 + 6);
            var prism = ShapeGenerator.CreateShape(ShapeType.Prism, PivotLocation.Center);
            prism.transform.localScale = scale;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedPrismDeformation(prism, quality);
            }
            
            // Deform統合
            ApplyDeformComponents(prism, quality, PrimitiveTerrainGenerator.PrimitiveType.Prism);
            
            return prism;
        }

        /// <summary>
        /// 高品質円錐を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityCone(Vector3 scale, QualitySettings quality)
        {
            int sides = Mathf.Max(8, quality.subdivisionLevel * 4 + 8);
            var cone = ShapeGenerator.CreateShape(ShapeType.Cone, PivotLocation.Center);
            cone.transform.localScale = scale;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedConeDeformation(cone, quality);
            }
            
            // Deform統合
            ApplyDeformComponents(cone, quality, PrimitiveTerrainGenerator.PrimitiveType.Cone);
            
            return cone;
        }

        /// <summary>
        /// 高品質八面体を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityOctahedron(Vector3 scale, QualitySettings quality)
        {
            var octahedron = ShapeGenerator.CreateShape(ShapeType.Cube, PivotLocation.Center);
            
            // 八面体形状に変形
            var vertices = octahedron.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = vertices[i].normalized * scale.magnitude;
            }
            octahedron.positions = vertices;
            octahedron.transform.localScale = Vector3.one;
            
            // 高品質処理
            if (quality.subdivisionLevel > 0)
            {
                for (int i = 0; i < quality.subdivisionLevel; i++)
                {
                    ConnectElements.Connect(octahedron, octahedron.faces);
                }
            }
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedOctahedronDeformation(octahedron, quality);
            }
            
            // Deform統合
            ApplyDeformComponents(octahedron, quality, PrimitiveTerrainGenerator.PrimitiveType.Octahedron);
            
            return octahedron;
        }

        /// <summary>
    }
}
