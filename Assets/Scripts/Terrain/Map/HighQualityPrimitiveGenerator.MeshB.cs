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
        /// 高品質結晶を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityCrystal(Vector3 scale, QualitySettings quality)
        {
            // 高品質結晶構造生成システムを使用
            var crystal = CrystalStructureGenerator.GenerateCrystalWithGrowthSimulation(scale, true);
            
            if (crystal != null && quality.enableAdvancedDeformation)
            {
                ApplyAdvancedCrystalDeformation(crystal, quality);
            }
            
            // Deform統合
            if (crystal != null)
            {
                ApplyDeformComponents(crystal, quality, PrimitiveTerrainGenerator.PrimitiveType.Crystal);
            }
            
            return crystal;
        }

        /// <summary>
        /// 高品質モノリスを生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityMonolith(Vector3 scale, QualitySettings quality)
        {
            var monolith = ShapeGenerator.CreateShape(ShapeType.Cube, PivotLocation.Center);
            
            // 縦長に調整
            Vector3 monolithScale = new Vector3(scale.x * 0.3f, scale.y * 2f, scale.z * 0.3f);
            monolith.transform.localScale = monolithScale;
            
            // 高品質処理
            if (quality.subdivisionLevel > 0)
            {
                for (int i = 0; i < quality.subdivisionLevel; i++)
                {
                    ConnectElements.Connect(monolith, monolith.faces);
                }
            }
            
            // 上部を細くして自然な石柱形状に
            var vertices = monolith.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].y > 0) // 上部の頂点
                {
                    float tapering = 1f - (vertices[i].y * 0.2f);
                    vertices[i] = new Vector3(vertices[i].x * tapering, vertices[i].y, vertices[i].z * tapering);
                }
            }
            monolith.positions = vertices;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedMonolithDeformation(monolith, quality);
            }
            
            // Deform統合
            ApplyDeformComponents(monolith, quality, PrimitiveTerrainGenerator.PrimitiveType.Monolith);
            
            return monolith;
        }

        /// <summary>
        /// 高品質アーチを生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityArch(Vector3 scale, QualitySettings quality)
        {
            // 建築学的生成システムを使用
            var archParams = ArchitecturalGenerator.ArchitecturalParams.Default(ArchitecturalGenerator.ArchitecturalType.SimpleArch);
            archParams.span = scale.x;
            archParams.height = scale.y;
            archParams.thickness = scale.z;
            archParams.position = Vector3.zero;
            archParams.enableStructuralOptimization = true;
            archParams.enableDecorations = quality.enableProceduralDetails;
            
            var archObject = ArchitecturalGenerator.GenerateArchitecturalStructure(archParams);
            
            if (archObject != null)
            {
                var meshFilter = archObject.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    var proBuilderMesh = archObject.GetComponent<ProBuilderMesh>();
                    if (proBuilderMesh == null)
                    {
                        proBuilderMesh = archObject.AddComponent<ProBuilderMesh>();
                        var meshRenderer = archObject.GetComponent<MeshRenderer>();
                        var importer = new MeshImporter(meshFilter.sharedMesh, meshRenderer != null ? meshRenderer.sharedMaterials : null, proBuilderMesh);
                        importer.Import();
                        proBuilderMesh.ToMesh();
                        proBuilderMesh.Refresh();
                    }
                    
                    // 一時的なオブジェクトを削除
                    UnityEngine.Object.DestroyImmediate(archObject);
                    
                    return proBuilderMesh;
                }
                
                UnityEngine.Object.DestroyImmediate(archObject);
            }
            
            // フォールバック：基本的なアーチ形状
            var fallbackArch = ShapeGenerator.CreateShape(ShapeType.Arch, PivotLocation.Center);
            fallbackArch.transform.localScale = scale;
            
            return fallbackArch;
        }

        /// <summary>
        /// 高品質リングを生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityRing(Vector3 scale, QualitySettings quality)
        {
            int rows = Mathf.Max(4, quality.subdivisionLevel + 4);
            int columns = Mathf.Max(16, quality.subdivisionLevel * 4 + 16);
            
            var ring = ShapeGenerator.CreateShape(ShapeType.Torus, PivotLocation.Center);
            
            // リング形状に調整（薄くて大きい）
            Vector3 ringScale = new Vector3(scale.x * 1.5f, scale.y * 0.2f, scale.z * 1.5f);
            ring.transform.localScale = ringScale;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedRingDeformation(ring, quality);
            }
            
            // Deform統合
            ApplyDeformComponents(ring, quality, PrimitiveTerrainGenerator.PrimitiveType.Ring);
            
            return ring;
        }

        /// <summary>
        /// 高品質メサを生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityMesa(Vector3 scale, QualitySettings quality)
        {
            int sides = Mathf.Max(12, quality.subdivisionLevel * 4 + 12);
            var mesa = ShapeGenerator.CreateShape(ShapeType.Cylinder, PivotLocation.Center);
            
            // 台地形状に調整（平たくて広い）
            Vector3 mesaScale = new Vector3(scale.x * 2f, scale.y * 0.3f, scale.z * 2f);
            mesa.transform.localScale = mesaScale;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedMesaDeformation(mesa, quality);
            }
            
            // Deform統合
            ApplyDeformComponents(mesa, quality, PrimitiveTerrainGenerator.PrimitiveType.Mesa);
            
            return mesa;
        }

        /// <summary>
        /// 高品質尖塔を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualitySpire(Vector3 scale, QualitySettings quality)
        {
            int sides = Mathf.Max(8, quality.subdivisionLevel * 2 + 8);
            var spire = ShapeGenerator.CreateShape(ShapeType.Cone, PivotLocation.Center);
            
            // 尖塔形状に調整（非常に高くて細い）
            Vector3 spireScale = new Vector3(scale.x * 0.4f, scale.y * 3f, scale.z * 0.4f);
            spire.transform.localScale = spireScale;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedSpireDeformation(spire, quality);
            }
            
            // Deform統合
            ApplyDeformComponents(spire, quality, PrimitiveTerrainGenerator.PrimitiveType.Spire);
            
            return spire;
        }

        /// <summary>
        /// 高品質巨石を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityBoulder(Vector3 scale, QualitySettings quality)
        {
            int subdivisions = Mathf.Max(2, quality.subdivisionLevel + 1);
            var boulder = ShapeGenerator.CreateShape(ShapeType.Sphere, PivotLocation.Center);
            boulder.transform.localScale = scale;
            
            // 不規則な岩石形状に変形
            var vertices = boulder.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 複数のノイズレイヤーで自然な岩石形状を作成
                float noise1 = Mathf.PerlinNoise(vertex.x * 3f, vertex.z * 3f);
                float noise2 = Mathf.PerlinNoise(vertex.x * 6f, vertex.z * 6f) * 0.5f;
                float noise3 = Mathf.PerlinNoise(vertex.x * 12f, vertex.z * 12f) * 0.25f;
                
                float combinedNoise = (noise1 + noise2 + noise3) / 1.75f;
                float randomFactor = 0.8f + combinedNoise * 0.4f;
                
                vertices[i] = vertex * randomFactor;
            }
            boulder.positions = vertices;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedBoulderDeformation(boulder, quality);
            }
            
            // Deform統合
            ApplyDeformComponents(boulder, quality, PrimitiveTerrainGenerator.PrimitiveType.Boulder);
            
            return boulder;
        }

        /// <summary>
        /// 高品質岩石層を生成
        /// </summary>
        private static ProBuilderMesh GenerateHighQualityFormation(Vector3 scale, QualitySettings quality)
        {
            var formation = ShapeGenerator.CreateShape(ShapeType.Cube, PivotLocation.Center);
            formation.transform.localScale = scale;
            
            // 高品質処理
            if (quality.subdivisionLevel > 0)
            {
                for (int i = 0; i < quality.subdivisionLevel; i++)
                {
                    ConnectElements.Connect(formation, formation.faces);
                }
            }
            
            // 層状構造に変形
            var vertices = formation.positions.ToArray();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // Y軸方向に層を作る（より詳細）
                float layerCount = 8f + quality.complexityLevel * 2f;
                float layerHeight = Mathf.Floor(vertex.y * layerCount) / layerCount;
                vertex.y = layerHeight;
                
                // 各層で少しずつずらす（地質学的な変形）
                float layerOffset = (layerHeight + 1f) * 0.05f;
                vertex.x += Mathf.Sin(layerOffset * 10f) * 0.05f;
                vertex.z += Mathf.Cos(layerOffset * 10f) * 0.05f;
                
                // 風化による不規則性
                float weathering = Mathf.PerlinNoise(vertex.x * 20f, vertex.z * 20f) * 0.02f;
                vertex += Vector3.one * weathering;
                
                vertices[i] = vertex;
            }
            formation.positions = vertices;
            
            if (quality.enableAdvancedDeformation)
            {
                ApplyAdvancedFormationDeformation(formation, quality);
            }
            
            // Deform統合
            ApplyDeformComponents(formation, quality, PrimitiveTerrainGenerator.PrimitiveType.Formation);
            
            return formation;
        }

        #region 高度な変形処理
    }
}
