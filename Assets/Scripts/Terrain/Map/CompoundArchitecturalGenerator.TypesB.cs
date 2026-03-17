using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Collections.Generic;
using System.Linq;
using Vastcore.Utilities;
using Vastcore.Core;
using Vastcore.Generation;

namespace Vastcore.Generation
{
    public static partial class CompoundArchitecturalGenerator
    {
        /// </summary>
        private static void GenerateCloisters(GameObject parent, CompoundArchitecturalParams parameters)
        {
            float cloisterRadius = parameters.overallSize.x * 0.6f;
            int archCount = 12;
            
            for (int i = 0; i < archCount; i++)
            {
                float angle = i * (360f / archCount);
                Vector3 position = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * cloisterRadius,
                    0,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * cloisterRadius
                );
                
                var cloisterParams = ArchitecturalGenerator.ArchitecturalParams.Default(ArchitecturalGenerator.ArchitecturalType.Colonnade);
                cloisterParams.position = position;
                cloisterParams.span = parameters.overallSize.x * 0.1f;
                cloisterParams.height = parameters.overallSize.y * 0.4f;
                cloisterParams.thickness = parameters.overallSize.z * 0.2f;
                cloisterParams.rotation = Quaternion.Euler(0, angle, 0);
                cloisterParams.stoneMaterial = parameters.secondaryMaterial;
                
                var cloister = ArchitecturalGenerator.GenerateArchitecturalStructure(cloisterParams);
                if (cloister != null)
                {
                    cloister.transform.SetParent(parent.transform);
                }
            }
        }

        /// <summary>
        /// 壁セグメントを作成
        /// </summary>
        private static void CreateWallSegment(GameObject parent, string name, Vector3 position, Vector3 size, CompoundArchitecturalParams parameters)
        {
            var wall = ShapeGenerator.CreateShape(ShapeType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent.transform);
            wall.transform.localPosition = position;
            wall.transform.localScale = size;
            
            // 城壁らしい形状変形
            ApplyWallDeformation(wall, parameters);
            
            if (parameters.primaryMaterial != null)
            {
                wall.GetComponent<MeshRenderer>().material = parameters.primaryMaterial;
            }
        }

        /// <summary>
        /// 壁の変形を適用
        /// </summary>
        private static void ApplyWallDeformation(ProBuilderMesh wall, CompoundArchitecturalParams parameters)
        {
            var vertices = wall.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 上部に城壁の凹凸を作成
                if (vertex.y > 0.3f)
                {
                    float battlementPattern = Mathf.Sin(vertex.x * 10f) > 0 ? 1f : 0.8f;
                    vertex.y *= battlementPattern;
                }
                
                vertices[i] = vertex;
            }
            
            wall.positions = vertices;
            wall.ToMesh();
            wall.Refresh();
        }

        /// <summary>
        /// 門を作成
        /// </summary>
        private static void CreateGateway(GameObject parent, string name, Vector3 position, CompoundArchitecturalParams parameters)
        {
            var gatewayParams = ArchitecturalGenerator.ArchitecturalParams.Default(ArchitecturalGenerator.ArchitecturalType.SimpleArch);
            gatewayParams.position = position;
            gatewayParams.span = parameters.overallSize.x * 0.08f;
            gatewayParams.height = parameters.overallSize.y * 0.6f;
            gatewayParams.thickness = parameters.overallSize.z;
            gatewayParams.stoneMaterial = parameters.primaryMaterial;
            
            var gateway = ArchitecturalGenerator.GenerateArchitecturalStructure(gatewayParams);
            if (gateway != null)
            {
                gateway.name = name;
                gateway.transform.SetParent(parent.transform);
            }
        }

        /// <summary>
        /// 要塞塔を生成
        /// </summary>
        private static void GenerateFortressTowers(GameObject parent, CompoundArchitecturalParams parameters)
        {
            int towerCount = 4;
            
            for (int i = 0; i < towerCount; i++)
            {
                float x = (i - towerCount * 0.5f + 0.5f) * (parameters.overallSize.x / towerCount) * 2f;
                Vector3 position = new Vector3(x, parameters.overallSize.y * 1.2f, 0);
                Vector3 size = new Vector3(parameters.overallSize.x * 0.08f, parameters.overallSize.y * 0.6f, parameters.overallSize.z * 1.5f);
                
                CreateFortressTower(parent, $"FortressTower_{i}", position, size, parameters);
            }
        }

        /// <summary>
        /// 要塞塔を作成
        /// </summary>
        private static void CreateFortressTower(GameObject parent, string name, Vector3 position, Vector3 size, CompoundArchitecturalParams parameters)
        {
            var tower = ShapeGenerator.CreateShape(ShapeType.Cylinder);
            tower.name = name;
            tower.transform.SetParent(parent.transform);
            tower.transform.localPosition = position;
            tower.transform.localScale = size;
            
            // 円錐屋根を追加
            var roof = ShapeGenerator.CreateShape(ShapeType.Cone);
            roof.name = "TowerRoof";
            roof.transform.SetParent(tower.transform);
            roof.transform.localPosition = new Vector3(0, 0.6f, 0);
            roof.transform.localScale = new Vector3(1.2f, 0.4f, 1.2f);
            
            if (parameters.primaryMaterial != null)
            {
                tower.GetComponent<MeshRenderer>().material = parameters.primaryMaterial;
                roof.GetComponent<MeshRenderer>().material = parameters.secondaryMaterial;
            }
        }

        /// <summary>
        /// 円形劇場の座席を生成
        /// </summary>
        private static void GenerateAmphitheaterSeating(GameObject parent, CompoundArchitecturalParams parameters)
        {
            int seatLevels = 5;
            float baseRadius = parameters.overallSize.x * 0.2f;
            
            for (int level = 0; level < seatLevels; level++)
            {
                float radius = baseRadius + level * parameters.overallSize.x * 0.05f;
                float height = level * parameters.overallSize.y * 0.1f;
                
                var seating = ShapeGenerator.CreateShape(ShapeType.Cylinder);
                seating.name = $"SeatingLevel_{level}";
                seating.transform.SetParent(parent.transform);
                seating.transform.localPosition = new Vector3(0, height, 0);
                seating.transform.localScale = new Vector3(radius * 2f, parameters.overallSize.y * 0.05f, radius * 2f);
                
                if (parameters.secondaryMaterial != null)
                {
                    seating.GetComponent<MeshRenderer>().material = parameters.secondaryMaterial;
                }
            }
        }

        /// <summary>
        /// アプス（後陣）を生成
        /// </summary>
        private static void GenerateApse(GameObject parent, CompoundArchitecturalParams parameters)
        {
            Vector3 position = new Vector3(0, 0, parameters.overallSize.z * 0.4f);
            Vector3 size = new Vector3(parameters.overallSize.x * 0.3f, parameters.overallSize.y * 0.8f, parameters.overallSize.z * 0.3f);
            
            var apse = ShapeGenerator.CreateShape(ShapeType.Cylinder);
            apse.name = "Apse";
            apse.transform.SetParent(parent.transform);
            apse.transform.localPosition = position;
            apse.transform.localScale = size;
            
            // 半円形に変形
            ApplyApseDeformation(apse, parameters);
            
            if (parameters.primaryMaterial != null)
            {
                apse.GetComponent<MeshRenderer>().material = parameters.primaryMaterial;
            }
        }

        /// <summary>
        /// アプスの変形を適用
        /// </summary>
        private static void ApplyApseDeformation(ProBuilderMesh apse, CompoundArchitecturalParams parameters)
        {
            var vertices = apse.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 前面を平らにして半円形にする
                if (vertex.z < 0)
                {
                    vertex.z = 0;
                }
                
                vertices[i] = vertex;
            }
            
            apse.positions = vertices;
            apse.ToMesh();
            apse.Refresh();
        }

        /// <summary>
        /// 回廊の一辺を生成
        /// </summary>
        private static void GenerateCloisterSide(GameObject parent, int sideIndex, Vector3 center, Vector3 direction, int archCount, CompoundArchitecturalParams parameters)
        {
            float archSpacing = parameters.overallSize.x * 0.5f / archCount;
            
            for (int i = 0; i < archCount; i++)
            {
                Vector3 position = center + direction * (i - archCount * 0.5f + 0.5f) * archSpacing;
                
                var archParams = ArchitecturalGenerator.ArchitecturalParams.Default(ArchitecturalGenerator.ArchitecturalType.Colonnade);
                archParams.position = position;
                archParams.span = archSpacing * 0.8f;
                archParams.height = parameters.overallSize.y;
                archParams.thickness = parameters.overallSize.z * 0.2f;
                archParams.rotation = Quaternion.LookRotation(direction);
                archParams.stoneMaterial = parameters.primaryMaterial;
                
                var arch = ArchitecturalGenerator.GenerateArchitecturalStructure(archParams);
                if (arch != null)
                {
                    arch.name = $"CloisterArch_{sideIndex}_{i}";
                    arch.transform.SetParent(parent.transform);
                }
            }
        }

        /// <summary>
        /// 回廊の庭園を生成
        /// </summary>
        private static void GenerateCloisterGarden(GameObject parent, CompoundArchitecturalParams parameters)
        {
            Vector3 size = new Vector3(parameters.overallSize.x * 0.4f, parameters.overallSize.y * 0.05f, parameters.overallSize.x * 0.4f);
            
            var garden = ShapeGenerator.CreateShape(ShapeType.Cube);
            garden.name = "CloisterGarden";
            garden.transform.SetParent(parent.transform);
            garden.transform.localPosition = new Vector3(0, -parameters.overallSize.y * 0.1f, 0);
            garden.transform.localScale = size;
            
            if (parameters.secondaryMaterial != null)
            {
                garden.GetComponent<MeshRenderer>().material = parameters.secondaryMaterial;
            }
        }

        /// <summary>
        /// 凱旋門の装飾を生成
        /// </summary>
        private static void GenerateTriumphalArchDecorations(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 上部の装飾パネル
            Vector3 position = new Vector3(0, parameters.overallSize.y * 1.2f, 0);
            Vector3 size = new Vector3(parameters.overallSize.x * 0.8f, parameters.overallSize.y * 0.3f, parameters.overallSize.z * 0.1f);
            
            var decorativePanel = ShapeGenerator.CreateShape(ShapeType.Cube);
            decorativePanel.name = "DecorativePanel";
            decorativePanel.transform.SetParent(parent.transform);
            decorativePanel.transform.localPosition = position;
            decorativePanel.transform.localScale = size;
            
            if (parameters.decorationMaterial != null)
            {
                decorativePanel.GetComponent<MeshRenderer>().material = parameters.decorationMaterial;
            }
            
            // 装飾的な柱
            for (int i = -1; i <= 1; i += 2)
            {
                Vector3 columnPos = new Vector3(i * parameters.overallSize.x * 0.35f, parameters.overallSize.y * 0.6f, parameters.overallSize.z * 0.6f);
                CreateDecorativeColumn(parent, $"DecorativeColumn_{(i > 0 ? "Right" : "Left")}", columnPos, parameters);
            }
        }

        /// <summary>
        /// 装飾的な柱を作成
        /// </summary>
        private static void CreateDecorativeColumn(GameObject parent, string name, Vector3 position, CompoundArchitecturalParams parameters)
        {
            var column = ShapeGenerator.CreateShape(ShapeType.Cylinder);
            column.name = name;
            column.transform.SetParent(parent.transform);
            column.transform.localPosition = position;
            column.transform.localScale = new Vector3(parameters.overallSize.x * 0.05f, parameters.overallSize.y * 1.2f, parameters.overallSize.x * 0.05f);
            
            if (parameters.decorationMaterial != null)
            {
                column.GetComponent<MeshRenderer>().material = parameters.decorationMaterial;
            }
        }
        #endregion

    }
}
