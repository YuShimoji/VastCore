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
        #region 複合構造生成
        /// <summary>
        /// 複数アーチ橋を生成
        /// </summary>
        private static void GenerateMultipleBridge(GameObject parent, CompoundArchitecturalParams parameters)
        {
            float archSpan = parameters.overallSize.x / parameters.structureCount;
            
            for (int i = 0; i < parameters.structureCount; i++)
            {
                float x = (i - parameters.structureCount * 0.5f + 0.5f) * archSpan;
                float heightVariation = Random.Range(-parameters.heightVariation, parameters.heightVariation);
                
                var archParams = ArchitecturalGenerator.ArchitecturalParams.Default(parameters.baseArchType);
                archParams.position = new Vector3(x, heightVariation * parameters.overallSize.y, 0);
                archParams.span = archSpan * 0.9f; // 少し重複させる
                archParams.height = parameters.overallSize.y * (1f + heightVariation);
                archParams.thickness = parameters.overallSize.z;
                archParams.stoneMaterial = parameters.primaryMaterial;
                archParams.decorationMaterial = parameters.decorationMaterial;
                
                var archObject = ArchitecturalGenerator.GenerateArchitecturalStructure(archParams);
                if (archObject != null)
                {
                    archObject.transform.SetParent(parent.transform);
                }
            }
            
            // 橋面を追加
            GenerateContinuousBridgeDeck(parent, parameters);
        }

        /// <summary>
        /// 水道橋システムを生成
        /// </summary>
        private static void GenerateAqueductSystem(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 複数レベルの水道橋
            int levels = 2;
            
            for (int level = 0; level < levels; level++)
            {
                float levelHeight = level * parameters.overallSize.y * 0.6f;
                float archSpan = parameters.overallSize.x / parameters.structureCount;
                
                for (int i = 0; i < parameters.structureCount; i++)
                {
                    float x = (i - parameters.structureCount * 0.5f + 0.5f) * archSpan;
                    
                    var archParams = ArchitecturalGenerator.ArchitecturalParams.Default(ArchitecturalGenerator.ArchitecturalType.Aqueduct);
                    archParams.position = new Vector3(x, levelHeight, 0);
                    archParams.span = archSpan * 0.9f;
                    archParams.height = parameters.overallSize.y * (0.8f - level * 0.2f);
                    archParams.thickness = parameters.overallSize.z * (1f - level * 0.2f);
                    archParams.stoneMaterial = parameters.primaryMaterial;
                    
                    var archObject = ArchitecturalGenerator.GenerateArchitecturalStructure(archParams);
                    if (archObject != null)
                    {
                        archObject.transform.SetParent(parent.transform);
                    }
                }
            }
            
            // 水路システムを追加
            GenerateWaterChannelSystem(parent, parameters);
        }

        /// <summary>
        /// 大聖堂複合体を生成
        /// </summary>
        private static void GenerateCathedralComplex(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 中央の大聖堂
            var mainCathedralParams = ArchitecturalGenerator.ArchitecturalParams.Default(ArchitecturalGenerator.ArchitecturalType.Cathedral);
            mainCathedralParams.position = Vector3.zero;
            mainCathedralParams.span = parameters.overallSize.x * 0.6f;
            mainCathedralParams.height = parameters.overallSize.y;
            mainCathedralParams.thickness = parameters.overallSize.z * 0.8f;
            mainCathedralParams.stoneMaterial = parameters.primaryMaterial;
            mainCathedralParams.decorationMaterial = parameters.decorationMaterial;
            
            var mainCathedral = ArchitecturalGenerator.GenerateArchitecturalStructure(mainCathedralParams);
            if (mainCathedral != null)
            {
                mainCathedral.transform.SetParent(parent.transform);
            }
            
            // 側面の礼拝堂
            GenerateSideChapels(parent, parameters);
            
            // 鐘楼
            GenerateBellTowers(parent, parameters);
            
            // 回廊
            GenerateCloisters(parent, parameters);
        }

        /// <summary>
        /// 要塞壁を生成
        /// </summary>
        private static void GenerateFortressWall(GameObject parent, CompoundArchitecturalParams parameters)
        {
            float segmentLength = parameters.overallSize.x / parameters.structureCount;
            
            for (int i = 0; i < parameters.structureCount; i++)
            {
                float x = (i - parameters.structureCount * 0.5f + 0.5f) * segmentLength;
                
                // 壁のセグメント
                CreateWallSegment(parent, $"WallSegment_{i}", 
                    new Vector3(x, parameters.overallSize.y * 0.5f, 0),
                    new Vector3(segmentLength, parameters.overallSize.y, parameters.overallSize.z),
                    parameters);
                
                // 定期的に門を配置
                if (i % 3 == 1)
                {
                    CreateGateway(parent, $"Gateway_{i}", 
                        new Vector3(x, parameters.overallSize.y * 0.4f, 0),
                        parameters);
                }
            }
            
            // 塔を追加
            GenerateFortressTowers(parent, parameters);
        }

        /// <summary>
        /// 円形劇場を生成
        /// </summary>
        private static void GenerateAmphitheater(GameObject parent, CompoundArchitecturalParams parameters)
        {
            float radius = parameters.overallSize.x * 0.4f;
            float angleStep = 360f / parameters.structureCount;
            
            for (int i = 0; i < parameters.structureCount; i++)
            {
                float angle = i * angleStep;
                Vector3 position = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                    0,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius
                );
                
                var archParams = ArchitecturalGenerator.ArchitecturalParams.Default(parameters.baseArchType);
                archParams.position = position;
                archParams.span = parameters.overallSize.x * 0.15f;
                archParams.height = parameters.overallSize.y;
                archParams.thickness = parameters.overallSize.z * 0.3f;
                archParams.rotation = Quaternion.Euler(0, angle, 0);
                archParams.stoneMaterial = parameters.primaryMaterial;
                
                var archObject = ArchitecturalGenerator.GenerateArchitecturalStructure(archParams);
                if (archObject != null)
                {
                    archObject.transform.SetParent(parent.transform);
                }
            }
            
            // 座席を追加
            GenerateAmphitheaterSeating(parent, parameters);
        }

        /// <summary>
        /// バシリカを生成
        /// </summary>
        private static void GenerateBasilica(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 中央身廊
            var naveParams = ArchitecturalGenerator.ArchitecturalParams.Default(ArchitecturalGenerator.ArchitecturalType.RomanArch);
            naveParams.position = Vector3.zero;
            naveParams.span = parameters.overallSize.x * 0.4f;
            naveParams.height = parameters.overallSize.y;
            naveParams.thickness = parameters.overallSize.z;
            naveParams.stoneMaterial = parameters.primaryMaterial;
            
            var nave = ArchitecturalGenerator.GenerateArchitecturalStructure(naveParams);
            if (nave != null)
            {
                nave.transform.SetParent(parent.transform);
            }
            
            // 側廊
            for (int side = -1; side <= 1; side += 2)
            {
                var aisleParams = naveParams;
                aisleParams.position = new Vector3(side * parameters.overallSize.x * 0.3f, 0, 0);
                aisleParams.span = parameters.overallSize.x * 0.2f;
                aisleParams.height = parameters.overallSize.y * 0.7f;
                
                var aisle = ArchitecturalGenerator.GenerateArchitecturalStructure(aisleParams);
                if (aisle != null)
                {
                    aisle.transform.SetParent(parent.transform);
                }
            }
            
            // アプス（後陣）
            GenerateApse(parent, parameters);
        }

        /// <summary>
        /// 回廊を生成
        /// </summary>
        private static void GenerateCloister(GameObject parent, CompoundArchitecturalParams parameters)
        {
            float sideLength = parameters.overallSize.x * 0.25f;
            int archesPerSide = parameters.structureCount / 4;
            
            // 4つの側面を生成
            for (int side = 0; side < 4; side++)
            {
                Vector3 sideCenter = Vector3.zero;
                Vector3 direction = Vector3.forward;
                
                switch (side)
                {
                    case 0: // 北
                        sideCenter = new Vector3(0, 0, sideLength);
                        direction = Vector3.right;
                        break;
                    case 1: // 東
                        sideCenter = new Vector3(sideLength, 0, 0);
                        direction = Vector3.back;
                        break;
                    case 2: // 南
                        sideCenter = new Vector3(0, 0, -sideLength);
                        direction = Vector3.left;
                        break;
                    case 3: // 西
                        sideCenter = new Vector3(-sideLength, 0, 0);
                        direction = Vector3.forward;
                        break;
                }
                
                GenerateCloisterSide(parent, side, sideCenter, direction, archesPerSide, parameters);
            }
            
            // 中央の庭園
            GenerateCloisterGarden(parent, parameters);
        }

        /// <summary>
        /// 凱旋門を生成
        /// </summary>
        private static void GenerateTriumphalArch(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 中央の大きなアーチ
            var mainArchParams = ArchitecturalGenerator.ArchitecturalParams.Default(ArchitecturalGenerator.ArchitecturalType.RomanArch);
            mainArchParams.position = Vector3.zero;
            mainArchParams.span = parameters.overallSize.x * 0.6f;
            mainArchParams.height = parameters.overallSize.y;
            mainArchParams.thickness = parameters.overallSize.z;
            mainArchParams.stoneMaterial = parameters.primaryMaterial;
            mainArchParams.decorationMaterial = parameters.decorationMaterial;
            mainArchParams.enableDecorations = true;
            
            var mainArch = ArchitecturalGenerator.GenerateArchitecturalStructure(mainArchParams);
            if (mainArch != null)
            {
                mainArch.transform.SetParent(parent.transform);
            }
            
            // 側面の小さなアーチ
            for (int side = -1; side <= 1; side += 2)
            {
                var sideArchParams = mainArchParams;
                sideArchParams.position = new Vector3(side * parameters.overallSize.x * 0.4f, 0, 0);
                sideArchParams.span = parameters.overallSize.x * 0.25f;
                sideArchParams.height = parameters.overallSize.y * 0.7f;
                
                var sideArch = ArchitecturalGenerator.GenerateArchitecturalStructure(sideArchParams);
                if (sideArch != null)
                {
                    sideArch.transform.SetParent(parent.transform);
                }
            }
            
            // 上部の装飾
            GenerateTriumphalArchDecorations(parent, parameters);
        }

        /// <summary>
        /// 連続橋面を生成
        /// </summary>
        private static void GenerateContinuousBridgeDeck(GameObject parent, CompoundArchitecturalParams parameters)
        {
            Vector3 position = new Vector3(0, parameters.overallSize.y * 1.1f, 0);
            Vector3 size = new Vector3(parameters.overallSize.x, parameters.overallSize.z * 0.3f, parameters.overallSize.z);
            
            var deck = ShapeGenerator.CreateShape(ShapeType.Cube);
            deck.name = "ContinuousBridgeDeck";
            deck.transform.SetParent(parent.transform);
            deck.transform.localPosition = position;
            deck.transform.localScale = size;
            
            if (parameters.primaryMaterial != null)
            {
                deck.GetComponent<MeshRenderer>().material = parameters.primaryMaterial;
            }
        }

        /// <summary>
        /// 水路システムを生成
        /// </summary>
        private static void GenerateWaterChannelSystem(GameObject parent, CompoundArchitecturalParams parameters)
        {
            Vector3 position = new Vector3(0, parameters.overallSize.y * 1.05f, 0);
            Vector3 size = new Vector3(parameters.overallSize.x * 0.9f, parameters.overallSize.z * 0.2f, parameters.overallSize.z * 0.4f);
            
            var channel = ShapeGenerator.CreateShape(ShapeType.Cube);
            channel.name = "WaterChannelSystem";
            channel.transform.SetParent(parent.transform);
            channel.transform.localPosition = position;
            channel.transform.localScale = size;
            
            // U字型の水路形状を作成
            ApplyChannelDeformation(channel, parameters);
            
            if (parameters.secondaryMaterial != null)
            {
                channel.GetComponent<MeshRenderer>().material = parameters.secondaryMaterial;
            }
        }

        /// <summary>
        /// 水路の変形を適用
        /// </summary>
        private static void ApplyChannelDeformation(ProBuilderMesh channel, CompoundArchitecturalParams parameters)
        {
            var vertices = channel.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // U字型の水路を作成
                if (vertex.y > 0)
                {
                    float battlementPattern = Mathf.Sin(vertex.x * 10f) > 0 ? 1f : 0.8f;
                    vertex.y *= battlementPattern;
                }
                if (Mathf.Abs(vertex.z) < 0.3f && vertex.y < 0)
                {
                    vertex.y *= 0.7f; // 中央部分を浅くする
                }
                
                vertices[i] = vertex;
            }
            
            channel.positions = vertices;
            channel.ToMesh();
            channel.Refresh();
        }

        /// <summary>
        /// 側面礼拝堂を生成
        /// </summary>
        private static void GenerateSideChapels(GameObject parent, CompoundArchitecturalParams parameters)
        {
            int chapelCount = 4;
            
            for (int i = 0; i < chapelCount; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float z = (i / 2 - 0.5f) * parameters.overallSize.z * 0.6f;
                
                var chapelParams = ArchitecturalGenerator.ArchitecturalParams.Default(ArchitecturalGenerator.ArchitecturalType.GothicArch);
                chapelParams.position = new Vector3(side * parameters.overallSize.x * 0.4f, 0, z);
                chapelParams.span = parameters.overallSize.x * 0.2f;
                chapelParams.height = parameters.overallSize.y * 0.6f;
                chapelParams.thickness = parameters.overallSize.z * 0.3f;
                chapelParams.stoneMaterial = parameters.primaryMaterial;
                
                var chapel = ArchitecturalGenerator.GenerateArchitecturalStructure(chapelParams);
                if (chapel != null)
                {
                    chapel.transform.SetParent(parent.transform);
                }
            }
        }

        /// <summary>
        /// 鐘楼を生成
        /// </summary>
        private static void GenerateBellTowers(GameObject parent, CompoundArchitecturalParams parameters)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 position = new Vector3(side * parameters.overallSize.x * 0.4f, parameters.overallSize.y * 1.2f, parameters.overallSize.z * 0.4f);
                Vector3 size = new Vector3(parameters.overallSize.x * 0.15f, parameters.overallSize.y * 0.8f, parameters.overallSize.z * 0.15f);
                
                CreateBellTower(parent, $"BellTower_{(side > 0 ? "Right" : "Left")}", position, size, parameters);
            }
        }

        /// <summary>
        /// 鐘楼を作成
        /// </summary>
        private static void CreateBellTower(GameObject parent, string name, Vector3 position, Vector3 size, CompoundArchitecturalParams parameters)
        {
            var tower = ShapeGenerator.CreateShape(ShapeType.Cube);
            tower.name = name;
            tower.transform.SetParent(parent.transform);
            tower.transform.localPosition = position;
            tower.transform.localScale = size;
            
            // 尖塔を追加
            var spire = ShapeGenerator.CreateShape(ShapeType.Cone);
            spire.name = "Spire";
            spire.transform.SetParent(tower.transform);
            spire.transform.localPosition = new Vector3(0, 0.6f, 0);
            spire.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);
            
            if (parameters.primaryMaterial != null)
            {
                tower.GetComponent<MeshRenderer>().material = parameters.primaryMaterial;
                spire.GetComponent<MeshRenderer>().material = parameters.primaryMaterial;
            }
        }

        /// <summary>
        /// 回廊を生成
    }
}
