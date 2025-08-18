using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Collections.Generic;
using Vastcore.Utilities;

namespace Vastcore.Generation
{
    /// <summary>
    /// 複合建築構造生成システム
    /// 複数のアーチを組み合わせた複雑な建築構造を生成
    /// </summary>
    public static class CompoundArchitecturalGenerator
    {
        #region 複合建築タイプ定義
        public enum CompoundArchitecturalType
        {
            MultipleBridge,     // 複数アーチ橋
            AqueductSystem,     // 水道橋システム
            CathedralComplex,   // 大聖堂複合体
            FortressWall,       // 要塞壁
            Amphitheater,       // 円形劇場
            Basilica,           // バシリカ
            Cloister,           // 回廊
            TriumphalArch       // 凱旋門
        }
        #endregion

        #region 複合建築パラメータ
        [System.Serializable]
        public struct CompoundArchitecturalParams
        {
            [Header("基本設定")]
            public CompoundArchitecturalType compoundType;
            public Vector3 position;
            public Vector3 overallSize;
            public Quaternion rotation;
            
            [Header("構造配置")]
            public int structureCount;
            public float structureSpacing;
            public bool enableSymmetry;
            public float heightVariation;
            
            [Header("建築様式")]
            public ArchitecturalGenerator.ArchitecturalType baseArchType;
            public bool mixedStyles;
            public float styleVariationFactor;
            
            [Header("装飾統合")]
            public bool unifiedDecorations;
            public float decorationComplexity;
            public bool enableConnectingElements;
            
            [Header("材質設定")]
            public Material primaryMaterial;
            public Material secondaryMaterial;
            public Material decorationMaterial;
            
            public static CompoundArchitecturalParams Default(CompoundArchitecturalType type)
            {
                return new CompoundArchitecturalParams
                {
                    compoundType = type,
                    position = Vector3.zero,
                    overallSize = GetDefaultOverallSize(type),
                    rotation = Quaternion.identity,
                    structureCount = GetDefaultStructureCount(type),
                    structureSpacing = 100f,
                    enableSymmetry = true,
                    heightVariation = 0.2f,
                    baseArchType = ArchitecturalGenerator.ArchitecturalType.RomanArch,
                    mixedStyles = false,
                    styleVariationFactor = 0.1f,
                    unifiedDecorations = true,
                    decorationComplexity = 1.0f,
                    enableConnectingElements = true,
                    primaryMaterial = null,
                    secondaryMaterial = null,
                    decorationMaterial = null
                };
            }
            
            private static Vector3 GetDefaultOverallSize(CompoundArchitecturalType type)
            {
                switch (type)
                {
                    case CompoundArchitecturalType.MultipleBridge: return new Vector3(400f, 80f, 50f);
                    case CompoundArchitecturalType.AqueductSystem: return new Vector3(600f, 120f, 60f);
                    case CompoundArchitecturalType.CathedralComplex: return new Vector3(200f, 300f, 150f);
                    case CompoundArchitecturalType.FortressWall: return new Vector3(800f, 100f, 40f);
                    case CompoundArchitecturalType.Amphitheater: return new Vector3(300f, 60f, 300f);
                    case CompoundArchitecturalType.Basilica: return new Vector3(150f, 200f, 80f);
                    case CompoundArchitecturalType.Cloister: return new Vector3(120f, 50f, 120f);
                    case CompoundArchitecturalType.TriumphalArch: return new Vector3(100f, 150f, 30f);
                    default: return new Vector3(200f, 100f, 50f);
                }
            }
            
            private static int GetDefaultStructureCount(CompoundArchitecturalType type)
            {
                switch (type)
                {
                    case CompoundArchitecturalType.MultipleBridge: return 4;
                    case CompoundArchitecturalType.AqueductSystem: return 6;
                    case CompoundArchitecturalType.CathedralComplex: return 3;
                    case CompoundArchitecturalType.FortressWall: return 8;
                    case CompoundArchitecturalType.Amphitheater: return 12;
                    case CompoundArchitecturalType.Basilica: return 5;
                    case CompoundArchitecturalType.Cloister: return 16;
                    case CompoundArchitecturalType.TriumphalArch: return 3;
                    default: return 3;
                }
            }
        }
        #endregion

        #region メイン生成関数
        /// <summary>
        /// 複合建築構造を生成
        /// </summary>
        public static GameObject GenerateCompoundArchitecturalStructure(CompoundArchitecturalParams parameters)
        {
            try
            {
                GameObject compoundObject = new GameObject($"Compound_{parameters.compoundType}");
                compoundObject.transform.position = parameters.position;
                compoundObject.transform.rotation = parameters.rotation;

                // 複合建築タイプに応じた生成
                switch (parameters.compoundType)
                {
                    case CompoundArchitecturalType.MultipleBridge:
                        GenerateMultipleBridge(compoundObject, parameters);
                        break;
                    case CompoundArchitecturalType.AqueductSystem:
                        GenerateAqueductSystem(compoundObject, parameters);
                        break;
                    case CompoundArchitecturalType.CathedralComplex:
                        GenerateCathedralComplex(compoundObject, parameters);
                        break;
                    case CompoundArchitecturalType.FortressWall:
                        GenerateFortressWall(compoundObject, parameters);
                        break;
                    case CompoundArchitecturalType.Amphitheater:
                        GenerateAmphitheater(compoundObject, parameters);
                        break;
                    case CompoundArchitecturalType.Basilica:
                        GenerateBasilica(compoundObject, parameters);
                        break;
                    case CompoundArchitecturalType.Cloister:
                        GenerateCloister(compoundObject, parameters);
                        break;
                    case CompoundArchitecturalType.TriumphalArch:
                        GenerateTriumphalArch(compoundObject, parameters);
                        break;
                    default:
                        Debug.LogWarning($"Compound architectural type {parameters.compoundType} not implemented");
                        GenerateMultipleBridge(compoundObject, parameters);
                        break;
                }

                // 接続要素を追加
                if (parameters.enableConnectingElements)
                {
                    AddConnectingElements(compoundObject, parameters);
                }

                // 統一装飾を追加
                if (parameters.unifiedDecorations)
                {
                    AddUnifiedDecorations(compoundObject, parameters);
                }

                // 複合コライダーを設定
                SetupCompoundColliders(compoundObject, parameters);

                Debug.Log($"Successfully generated compound architectural structure: {parameters.compoundType}");
                return compoundObject;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error generating compound architectural structure {parameters.compoundType}: {e.Message}");
                return null;
            }
        }
        #endregion

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

        #region 接続・統合システム
        /// <summary>
        /// 接続要素を追加
        /// </summary>
        private static void AddConnectingElements(GameObject parent, CompoundArchitecturalParams parameters)
        {
            switch (parameters.compoundType)
            {
                case CompoundArchitecturalType.MultipleBridge:
                case CompoundArchitecturalType.AqueductSystem:
                    AddBridgeConnections(parent, parameters);
                    break;
                case CompoundArchitecturalType.CathedralComplex:
                    AddCathedralConnections(parent, parameters);
                    break;
                case CompoundArchitecturalType.FortressWall:
                    AddWallConnections(parent, parameters);
                    break;
                case CompoundArchitecturalType.Cloister:
                    AddCloisterConnections(parent, parameters);
                    break;
            }
        }

        /// <summary>
        /// 橋梁接続要素を追加
        /// </summary>
        private static void AddBridgeConnections(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 支柱間の接続梁
            int connectionCount = parameters.structureCount - 1;
            float archSpan = parameters.overallSize.x / parameters.structureCount;
            
            for (int i = 0; i < connectionCount; i++)
            {
                float x = (i - connectionCount * 0.5f + 0.5f) * archSpan;
                Vector3 position = new Vector3(x, parameters.overallSize.y * 0.8f, 0);
                Vector3 size = new Vector3(parameters.overallSize.z, parameters.overallSize.z * 0.5f, parameters.overallSize.z);
                
                CreateConnectionBeam(parent, $"ConnectionBeam_{i}", position, size, parameters);
            }
        }

        /// <summary>
        /// 大聖堂接続要素を追加
        /// </summary>
        private static void AddCathedralConnections(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 身廊と側廊を接続する横断アーチ
            int transeptCount = 3;
            
            for (int i = 0; i < transeptCount; i++)
            {
                float z = (i - transeptCount * 0.5f + 0.5f) * parameters.overallSize.z * 0.6f;
                Vector3 position = new Vector3(0, parameters.overallSize.y * 0.7f, z);
                Vector3 size = new Vector3(parameters.overallSize.x * 0.8f, parameters.overallSize.z * 0.3f, parameters.overallSize.z * 0.2f);
                
                CreateTranseptArch(parent, $"TranseptArch_{i}", position, size, parameters);
            }
        }

        /// <summary>
        /// 城壁接続要素を追加
        /// </summary>
        private static void AddWallConnections(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 城壁上部の歩廊
            Vector3 position = new Vector3(0, parameters.overallSize.y * 1.05f, 0);
            Vector3 size = new Vector3(parameters.overallSize.x, parameters.overallSize.z * 0.2f, parameters.overallSize.z * 0.8f);
            
            var walkway = ShapeGenerator.CreateShape(ShapeType.Cube);
            walkway.name = "WallWalkway";
            walkway.transform.SetParent(parent.transform);
            walkway.transform.localPosition = position;
            walkway.transform.localScale = size;
            
            if (parameters.secondaryMaterial != null)
            {
                walkway.GetComponent<MeshRenderer>().material = parameters.secondaryMaterial;
            }
        }

        /// <summary>
        /// 回廊接続要素を追加
        /// </summary>
        private static void AddCloisterConnections(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 角の接続柱
            float cornerRadius = parameters.overallSize.x * 0.25f;
            
            for (int corner = 0; corner < 4; corner++)
            {
                float angle = corner * 90f + 45f;
                Vector3 position = new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * cornerRadius,
                    parameters.overallSize.y * 0.5f,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * cornerRadius
                );
                
                CreateCornerColumn(parent, $"CornerColumn_{corner}", position, parameters);
            }
        }

        /// <summary>
        /// 接続梁を作成
        /// </summary>
        private static void CreateConnectionBeam(GameObject parent, string name, Vector3 position, Vector3 size, CompoundArchitecturalParams parameters)
        {
            var beam = ShapeGenerator.CreateShape(ShapeType.Cube);
            beam.name = name;
            beam.transform.SetParent(parent.transform);
            beam.transform.localPosition = position;
            beam.transform.localScale = size;
            
            if (parameters.secondaryMaterial != null)
            {
                beam.GetComponent<MeshRenderer>().material = parameters.secondaryMaterial;
            }
        }

        /// <summary>
        /// 横断アーチを作成
        /// </summary>
        private static void CreateTranseptArch(GameObject parent, string name, Vector3 position, Vector3 size, CompoundArchitecturalParams parameters)
        {
            var transept = ShapeGenerator.CreateShape(ShapeType.Cube);
            transept.name = name;
            transept.transform.SetParent(parent.transform);
            transept.transform.localPosition = position;
            transept.transform.localScale = size;
            
            // アーチ形状に変形
            ApplyArchDeformation(transept, parameters);
            
            if (parameters.primaryMaterial != null)
            {
                transept.GetComponent<MeshRenderer>().material = parameters.primaryMaterial;
            }
        }

        /// <summary>
        /// アーチ変形を適用
        /// </summary>
        private static void ApplyArchDeformation(ProBuilderMesh mesh, CompoundArchitecturalParams parameters)
        {
            var vertices = mesh.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 下部をアーチ形状にする
                if (vertex.y < 0)
                {
                    float archCurve = 1f - (vertex.x * vertex.x) / 0.25f;
                    if (archCurve > 0)
                    {
                        vertex.y *= archCurve;
                    }
                    else
                    {
                        vertex.y = 0; // アーチの外側は削除
                    }
                }
                
                vertices[i] = vertex;
            }
            
            mesh.positions = vertices;
            mesh.ToMesh();
            mesh.Refresh();
        }

        /// <summary>
        /// 角柱を作成
        /// </summary>
        private static void CreateCornerColumn(GameObject parent, string name, Vector3 position, CompoundArchitecturalParams parameters)
        {
            var column = ShapeGenerator.CreateShape(ShapeType.Cylinder);
            column.name = name;
            column.transform.SetParent(parent.transform);
            column.transform.localPosition = position;
            column.transform.localScale = new Vector3(parameters.overallSize.x * 0.04f, parameters.overallSize.y, parameters.overallSize.x * 0.04f);
            
            if (parameters.primaryMaterial != null)
            {
                column.GetComponent<MeshRenderer>().material = parameters.primaryMaterial;
            }
        }

        /// <summary>
        /// 統一装飾を追加
        /// </summary>
        private static void AddUnifiedDecorations(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 全体的な装飾テーマを適用
            ApplyDecorativeTheme(parent, parameters);
            
            // 装飾的な要素を追加
            if (parameters.decorationComplexity > 0.5f)
            {
                AddComplexDecorations(parent, parameters);
            }
        }

        /// <summary>
        /// 装飾テーマを適用
        /// </summary>
        private static void ApplyDecorativeTheme(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 全ての子オブジェクトに統一された装飾スタイルを適用
            var renderers = parent.GetComponentsInChildren<MeshRenderer>();
            
            foreach (var renderer in renderers)
            {
                if (renderer.gameObject.name.Contains("Decoration") || renderer.gameObject.name.Contains("Keystone"))
                {
                    if (parameters.decorationMaterial != null)
                    {
                        renderer.material = parameters.decorationMaterial;
                    }
                }
            }
        }

        /// <summary>
        /// 複雑な装飾を追加
        /// </summary>
        private static void AddComplexDecorations(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 装飾的な尖塔を追加
            int spireCount = Mathf.RoundToInt(parameters.decorationComplexity * 6f);
            
            for (int i = 0; i < spireCount; i++)
            {
                Vector3 position = GetRandomDecorationPosition(parameters);
                CreateDecorativeSpire(parent, $"DecorativeSpire_{i}", position, parameters);
            }
        }

        /// <summary>
        /// ランダムな装飾位置を取得
        /// </summary>
        private static Vector3 GetRandomDecorationPosition(CompoundArchitecturalParams parameters)
        {
            float x = Random.Range(-parameters.overallSize.x * 0.4f, parameters.overallSize.x * 0.4f);
            float y = parameters.overallSize.y * Random.Range(0.8f, 1.2f);
            float z = Random.Range(-parameters.overallSize.z * 0.4f, parameters.overallSize.z * 0.4f);
            
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// 装飾的な尖塔を作成
        /// </summary>
        private static void CreateDecorativeSpire(GameObject parent, string name, Vector3 position, CompoundArchitecturalParams parameters)
        {
            var spire = ShapeGenerator.CreateShape(ShapeType.Cone);
            spire.name = name;
            spire.transform.SetParent(parent.transform);
            spire.transform.localPosition = position;
            spire.transform.localScale = Vector3.one * parameters.overallSize.x * 0.03f * parameters.decorationComplexity;
            
            if (parameters.decorationMaterial != null)
            {
                spire.GetComponent<MeshRenderer>().material = parameters.decorationMaterial;
            }
        }

        /// <summary>
        /// 複合コライダーを設定
        /// </summary>
        private static void SetupCompoundColliders(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // 親オブジェクトに複合コライダーを追加
            var meshCollider = parent.AddComponent<MeshCollider>();
            meshCollider.convex = false;
            
            // 全ての子メッシュを結合してコライダーを作成
            CombineAllMeshesForCollider(parent, meshCollider);
            
            // インタラクション設定
            SetupCompoundInteractions(parent, parameters);
        }

        /// <summary>
        /// 全メッシュを結合してコライダーを作成
        /// </summary>
        private static void CombineAllMeshesForCollider(GameObject parent, MeshCollider collider)
        {
            MeshCombineHelper.CombineChildrenToCollider(parent, collider, "CompoundArchitecturalGenerator");
        }

        /// <summary>
        /// 複合インタラクションを設定
        /// </summary>
        private static void SetupCompoundInteractions(GameObject parent, CompoundArchitecturalParams parameters)
        {
            // プリミティブ地形オブジェクトコンポーネントを追加
            var compoundComponent = parent.AddComponent<PrimitiveTerrainObject>();
            compoundComponent.primitiveType = PrimitiveTerrainGenerator.PrimitiveType.Arch;
            compoundComponent.isClimbable = true;
            compoundComponent.isGrindable = true;
            compoundComponent.hasCollision = true;
            
            // 複合建築物専用のタグを設定
            try
            {
                parent.tag = "CompoundArchitecture";
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Tag 'CompoundArchitecture' is not defined. Skipping tag assignment. Details: {ex.Message}");
            }
        }

        #endregion

        #region ユーティリティ関数
        /// <summary>
        /// 複合建築タイプの説明を取得
        /// </summary>
        public static string GetCompoundArchitecturalDescription(CompoundArchitecturalType type)
        {
            switch (type)
            {
                case CompoundArchitecturalType.MultipleBridge: return "複数アーチ橋梁";
                case CompoundArchitecturalType.AqueductSystem: return "古代水道橋システム";
                case CompoundArchitecturalType.CathedralComplex: return "大聖堂建築複合体";
                case CompoundArchitecturalType.FortressWall: return "要塞城壁";
                case CompoundArchitecturalType.Amphitheater: return "古代円形劇場";
                case CompoundArchitecturalType.Basilica: return "バシリカ建築";
                case CompoundArchitecturalType.Cloister: return "修道院回廊";
                case CompoundArchitecturalType.TriumphalArch: return "凱旋門";
                default: return "不明な複合建築構造";
            }
        }

        /// <summary>
        /// ランダムな複合建築タイプを取得
        /// </summary>
        public static CompoundArchitecturalType GetRandomCompoundArchitecturalType()
        {
            var values = System.Enum.GetValues(typeof(CompoundArchitecturalType));
            return (CompoundArchitecturalType)values.GetValue(Random.Range(0, values.Length));
        }

        /// <summary>
        /// 複合建築構造の複雑度を計算
        /// </summary>
        public static float CalculateComplexityScore(CompoundArchitecturalParams parameters)
        {
            float baseComplexity = parameters.structureCount * 0.1f;
            float decorationComplexity = parameters.decorationComplexity * 0.3f;
            float sizeComplexity = (parameters.overallSize.magnitude / 1000f) * 0.2f;
            float connectionComplexity = parameters.enableConnectingElements ? 0.2f : 0f;
            float unificationComplexity = parameters.unifiedDecorations ? 0.2f : 0f;
            
            return baseComplexity + decorationComplexity + sizeComplexity + connectionComplexity + unificationComplexity;
        }

        /// <summary>
        /// 建築様式の互換性をチェック
        /// </summary>
        public static bool CheckStyleCompatibility(ArchitecturalGenerator.ArchitecturalType style1, ArchitecturalGenerator.ArchitecturalType style2)
        {
            // 同じ時代・地域の建築様式は互換性が高い
            var romanStyles = new[] { ArchitecturalGenerator.ArchitecturalType.RomanArch, ArchitecturalGenerator.ArchitecturalType.Colonnade };
            var gothicStyles = new[] { ArchitecturalGenerator.ArchitecturalType.GothicArch, ArchitecturalGenerator.ArchitecturalType.Cathedral };
            
            bool bothRoman = System.Array.IndexOf(romanStyles, style1) >= 0 && System.Array.IndexOf(romanStyles, style2) >= 0;
            bool bothGothic = System.Array.IndexOf(gothicStyles, style1) >= 0 && System.Array.IndexOf(gothicStyles, style2) >= 0;
            
            return bothRoman || bothGothic || style1 == style2;
        }

        /// <summary>
        /// 推定建設時間を計算（ゲーム内時間）
        /// </summary>
        public static float CalculateEstimatedConstructionTime(CompoundArchitecturalParams parameters)
        {
            float baseTime = parameters.structureCount * 10f; // 基本時間（秒）
            float complexityMultiplier = CalculateComplexityScore(parameters);
            float sizeMultiplier = parameters.overallSize.magnitude / 100f;
            
            return baseTime * complexityMultiplier * sizeMultiplier;
        }
        #endregion
    }
}