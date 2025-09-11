using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Generation
{
    /// <summary>
    /// 結晶学的構造生成エンジン
    /// 実際の結晶学に基づいた6種類の結晶系を実装
    /// </summary>
    public static class CrystalStructureGenerator
    {
        #region 結晶系定義
        /// <summary>
        /// 結晶系の種類（実際の結晶学に基づく）
        /// </summary>
        public enum CrystalSystem
        {
            Cubic,          // 立方晶系 - 等軸晶系
            Hexagonal,      // 六方晶系 - 六角形の対称性
            Tetragonal,     // 正方晶系 - 四角形の対称性
            Orthorhombic,   // 斜方晶系 - 直交する不等な軸
            Monoclinic,     // 単斜晶系 - 一つの斜軸
            Triclinic       // 三斜晶系 - 全て異なる軸と角度
        }

        /// <summary>
        /// 結晶面の種類
        /// </summary>
        public enum CrystalFace
        {
            Cube,           // 立方面 {100}
            Octahedron,     // 八面体面 {111}
            Dodecahedron,   // 十二面体面 {110}
            Rhombohedron,   // 菱面体面
            Prism,          // 柱面
            Pyramid,        // 錐面
            Pinacoid,       // 平行面
            Dome            // ドーム面
        }

        /// <summary>
        /// 結晶生成パラメータ
        /// </summary>
        [System.Serializable]
        public struct CrystalGenerationParams
        {
            [Header("結晶系設定")]
            public CrystalSystem crystalSystem;
            public Vector3 unitCellDimensions;  // 単位格子の寸法 (a, b, c)
            public Vector3 unitCellAngles;      // 単位格子の角度 (α, β, γ) in degrees
            
            [Header("結晶面設定")]
            public CrystalFace[] activeFaces;   // 発達する結晶面
            public float[] faceWeights;         // 各面の発達度
            
            [Header("成長パラメータ")]
            public Vector3 growthRates;         // 各軸方向の成長速度
            public float overallSize;           // 全体サイズ
            public int complexityLevel;         // 複雑度レベル (1-5)
            
            [Header("不完全性")]
            public bool enableImperfections;    // 不完全性を有効化
            public float imperfectionIntensity; // 不完全性の強度
            public int twinningProbability;     // 双晶の確率 (0-100)            

            public static CrystalGenerationParams Default(CrystalSystem system)
            {
                var param = new CrystalGenerationParams
                {
                    crystalSystem = system,
                    overallSize = 100f,
                    complexityLevel = 3,
                    enableImperfections = true,
                    imperfectionIntensity = 0.1f,
                    twinningProbability = 20
                };

                // 結晶系に応じたデフォルト設定
                switch (system)
                {
                    case CrystalSystem.Cubic:
                        param.unitCellDimensions = Vector3.one;
                        param.unitCellAngles = new Vector3(90, 90, 90);
                        param.activeFaces = new[] { CrystalFace.Cube, CrystalFace.Octahedron };
                        param.faceWeights = new[] { 1.0f, 0.7f };
                        param.growthRates = Vector3.one;
                        break;
                        
                    case CrystalSystem.Hexagonal:
                        param.unitCellDimensions = new Vector3(1, 1, 1.6f);
                        param.unitCellAngles = new Vector3(90, 90, 120);
                        param.activeFaces = new[] { CrystalFace.Prism, CrystalFace.Pyramid };
                        param.faceWeights = new[] { 1.0f, 0.8f };
                        param.growthRates = new Vector3(1, 1, 1.2f);
                        break;
                        
                    case CrystalSystem.Tetragonal:
                        param.unitCellDimensions = new Vector3(1, 1, 1.4f);
                        param.unitCellAngles = new Vector3(90, 90, 90);
                        param.activeFaces = new[] { CrystalFace.Prism, CrystalFace.Pyramid };
                        param.faceWeights = new[] { 1.0f, 0.6f };
                        param.growthRates = new Vector3(1, 1, 1.3f);
                        break;
                        
                    case CrystalSystem.Orthorhombic:
                        param.unitCellDimensions = new Vector3(1, 1.2f, 1.5f);
                        param.unitCellAngles = new Vector3(90, 90, 90);
                        param.activeFaces = new[] { CrystalFace.Pinacoid, CrystalFace.Dome };
                        param.faceWeights = new[] { 1.0f, 0.8f };
                        param.growthRates = new Vector3(0.8f, 1.0f, 1.2f);
                        break;
                        
                    case CrystalSystem.Monoclinic:
                        param.unitCellDimensions = new Vector3(1, 1.3f, 1.1f);
                        param.unitCellAngles = new Vector3(90, 110, 90);
                        param.activeFaces = new[] { CrystalFace.Pinacoid, CrystalFace.Dome };
                        param.faceWeights = new[] { 1.0f, 0.7f };
                        param.growthRates = new Vector3(0.9f, 1.1f, 1.0f);
                        break;
                        
                    case CrystalSystem.Triclinic:
                        param.unitCellDimensions = new Vector3(1, 1.1f, 1.3f);
                        param.unitCellAngles = new Vector3(85, 95, 105);
                        param.activeFaces = new[] { CrystalFace.Pinacoid };
                        param.faceWeights = new[] { 1.0f };
                        param.growthRates = new Vector3(0.8f, 1.0f, 1.1f);
                        break;
                }

                return param;
            }
        }
        #endregion

        #region メイン生成関数
        /// <summary>
        /// 結晶構造を生成
        /// </summary>
        public static ProBuilderMesh GenerateCrystalStructure(CrystalGenerationParams parameters)
        {
            try
            {
                Debug.Log($"Generating crystal structure: {parameters.crystalSystem}");
                
                // 基本結晶形状を生成
                ProBuilderMesh baseCrystal = GenerateBaseCrystalShape(parameters);
                
                if (baseCrystal == null)
                {
                    Debug.LogError($"Failed to generate base crystal shape for {parameters.crystalSystem}");
                    return null;
                }

                // 結晶面を適用
                ApplyCrystalFaces(baseCrystal, parameters);
                
                // 成長パターンを適用
                ApplyGrowthPattern(baseCrystal, parameters);
                
                // 不完全性を適用
                if (parameters.enableImperfections)
                {
                    ApplyImperfections(baseCrystal, parameters);
                }
                
                // 双晶を適用（確率的）
                if (Random.Range(0, 100) < parameters.twinningProbability)
                {
                    ApplyTwinning(baseCrystal, parameters);
                }

                // メッシュを最終化
                baseCrystal.ToMesh();
                baseCrystal.Refresh();

                Debug.Log($"Successfully generated {parameters.crystalSystem} crystal structure");
                return baseCrystal;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error generating crystal structure {parameters.crystalSystem}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// スケール指定での結晶構造生成（既存システムとの互換性）
        /// </summary>
        public static ProBuilderMesh GenerateCrystalStructure(Vector3 scale)
        {
            // ランダムな結晶系を選択
            var systems = System.Enum.GetValues(typeof(CrystalSystem));
            var randomSystem = (CrystalSystem)systems.GetValue(Random.Range(0, systems.Length));
            
            var parameters = CrystalGenerationParams.Default(randomSystem);
            parameters.overallSize = scale.magnitude;
            
            var crystal = GenerateCrystalStructure(parameters);
            if (crystal != null)
            {
                crystal.transform.localScale = scale.normalized * parameters.overallSize;
            }
            
            return crystal;
        }

        /// <summary>
        /// 成長シミュレーション付き結晶構造生成
        /// </summary>
        public static ProBuilderMesh GenerateCrystalWithGrowthSimulation(Vector3 scale, bool enableGrowthSimulation = true)
        {
            // ランダムな結晶系を選択
            var systems = System.Enum.GetValues(typeof(CrystalSystem));
            var randomSystem = (CrystalSystem)systems.GetValue(Random.Range(0, systems.Length));
            
            var crystalParams = CrystalGenerationParams.Default(randomSystem);
            crystalParams.overallSize = scale.magnitude;
            
            if (enableGrowthSimulation)
            {
                // 成長シミュレーションを使用
                var growthParams = CrystalGrowthSimulator.GrowthSimulationParams.Default();
                var crystal = CrystalGrowthSimulator.SimulateCrystalGrowth(crystalParams, growthParams);
                
                if (crystal != null)
                {
                    crystal.transform.localScale = scale.normalized * crystalParams.overallSize;
                }
                
                return crystal;
            }
            else
            {
                // 従来の方法を使用
                return GenerateCrystalStructure(crystalParams);
            }
        }
        #endregion
  
      #region 基本結晶形状生成
        /// <summary>
        /// 結晶系に基づく基本形状を生成
        /// </summary>
        private static ProBuilderMesh GenerateBaseCrystalShape(CrystalGenerationParams parameters)
        {
            switch (parameters.crystalSystem)
            {
                case CrystalSystem.Cubic:
                    return GenerateCubicCrystal(parameters);
                case CrystalSystem.Hexagonal:
                    return GenerateHexagonalCrystal(parameters);
                case CrystalSystem.Tetragonal:
                    return GenerateTetragonalCrystal(parameters);
                case CrystalSystem.Orthorhombic:
                    return GenerateOrthorhombicCrystal(parameters);
                case CrystalSystem.Monoclinic:
                    return GenerateMonoclinicCrystal(parameters);
                case CrystalSystem.Triclinic:
                    return GenerateTriclinicCrystal(parameters);
                default:
                    Debug.LogWarning($"Crystal system {parameters.crystalSystem} not implemented, using cubic");
                    return GenerateCubicCrystal(parameters);
            }
        }

        /// <summary>
        /// 立方晶系結晶を生成
        /// </summary>
        private static ProBuilderMesh GenerateCubicCrystal(CrystalGenerationParams parameters)
        {
            var crystal = ShapeGenerator.CreateShape(ShapeType.Cube, PivotLocation.Center, Vector3.one);
            
            // 立方晶系の特徴的な形状に変形
            var vertices = crystal.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 八面体面の影響を加える
                if (parameters.activeFaces.Contains(CrystalFace.Octahedron))
                {
                    float octahedralWeight = GetFaceWeight(CrystalFace.Octahedron, parameters);
                    float distance = Mathf.Abs(vertex.x) + Mathf.Abs(vertex.y) + Mathf.Abs(vertex.z);
                    vertex = vertex.normalized * Mathf.Lerp(vertex.magnitude, distance * 0.577f, octahedralWeight);
                }
                
                vertices[i] = vertex;
            }
            
            crystal.positions = vertices;
            return crystal;
        }

        /// <summary>
        /// 六方晶系結晶を生成
        /// </summary>
        private static ProBuilderMesh GenerateHexagonalCrystal(CrystalGenerationParams parameters)
        {
            // 六角柱をベースに生成
            var crystal = ShapeGenerator.CreateShape(ShapeType.Cylinder, PivotLocation.Center, Vector3.one);
            
            // 六角形に変形
            var vertices = crystal.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 六角形の対称性を適用
                if (Mathf.Abs(vertex.y) < 0.1f) // 側面の頂点
                {
                    float angle = Mathf.Atan2(vertex.z, vertex.x);
                    float hexAngle = Mathf.Round(angle / (Mathf.PI / 3f)) * (Mathf.PI / 3f);
                    float radius = new Vector2(vertex.x, vertex.z).magnitude;
                    
                    vertex.x = radius * Mathf.Cos(hexAngle);
                    vertex.z = radius * Mathf.Sin(hexAngle);
                }
                
                // 錐面の影響を加える
                if (parameters.activeFaces.Contains(CrystalFace.Pyramid))
                {
                    float pyramidWeight = GetFaceWeight(CrystalFace.Pyramid, parameters);
                    if (vertex.y > 0)
                    {
                        vertex.y *= (1f + pyramidWeight * 0.5f);
                        vertex.x *= (1f - pyramidWeight * 0.3f);
                        vertex.z *= (1f - pyramidWeight * 0.3f);
                    }
                }
                
                vertices[i] = vertex;
            }
            
            crystal.positions = vertices;
            return crystal;
        }

        /// <summary>
        /// 正方晶系結晶を生成
        /// </summary>
        private static ProBuilderMesh GenerateTetragonalCrystal(CrystalGenerationParams parameters)
        {
            var crystal = ShapeGenerator.CreateShape(ShapeType.Cube, PivotLocation.Center, Vector3.one);
            
            // 正方晶系の特徴（c軸が異なる）を適用
            var vertices = crystal.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // c軸方向（Y軸）を伸長
                vertex.y *= parameters.unitCellDimensions.z / parameters.unitCellDimensions.x;
                
                // 四角錐面の影響
                if (parameters.activeFaces.Contains(CrystalFace.Pyramid))
                {
                    float pyramidWeight = GetFaceWeight(CrystalFace.Pyramid, parameters);
                    if (vertex.y > 0)
                    {
                        float xyDistance = Mathf.Max(Mathf.Abs(vertex.x), Mathf.Abs(vertex.z));
                        vertex.y *= (1f + pyramidWeight * (1f - xyDistance));
                    }
                }
                
                vertices[i] = vertex;
            }
            
            crystal.positions = vertices;
            return crystal;
        }

        /// <summary>
        /// 斜方晶系結晶を生成
        /// </summary>
        private static ProBuilderMesh GenerateOrthorhombicCrystal(CrystalGenerationParams parameters)
        {
            var crystal = ShapeGenerator.CreateShape(ShapeType.Cube, PivotLocation.Center, Vector3.one);
            
            // 三つの軸がすべて異なる長さ
            var vertices = crystal.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 各軸を異なる比率でスケール
                vertex.x *= parameters.unitCellDimensions.x;
                vertex.y *= parameters.unitCellDimensions.y;
                vertex.z *= parameters.unitCellDimensions.z;
                
                vertices[i] = vertex;
            }
            
            crystal.positions = vertices;
            return crystal;
        }

        /// <summary>
        /// 単斜晶系結晶を生成
        /// </summary>
        private static ProBuilderMesh GenerateMonoclinicCrystal(CrystalGenerationParams parameters)
        {
            var crystal = ShapeGenerator.CreateShape(ShapeType.Cube, PivotLocation.Center, Vector3.one);
            
            // β角が90度でない（斜軸）
            var vertices = crystal.positions.ToArray();
            float betaAngle = parameters.unitCellAngles.y * Mathf.Deg2Rad;
            float shearFactor = Mathf.Cos(betaAngle);
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 単斜変形を適用（Y軸周りの剪断）
                vertex.x += vertex.y * shearFactor * 0.3f;
                
                // 軸の長さを調整
                vertex.x *= parameters.unitCellDimensions.x;
                vertex.y *= parameters.unitCellDimensions.y;
                vertex.z *= parameters.unitCellDimensions.z;
                
                vertices[i] = vertex;
            }
            
            crystal.positions = vertices;
            return crystal;
        }

        /// <summary>
        /// 三斜晶系結晶を生成
        /// </summary>
        private static ProBuilderMesh GenerateTriclinicCrystal(CrystalGenerationParams parameters)
        {
            var crystal = ShapeGenerator.CreateShape(ShapeType.Cube, PivotLocation.Center, Vector3.one);
            
            // すべての角度が90度でない（最も低い対称性）
            var vertices = crystal.positions.ToArray();
            
            float alphaAngle = parameters.unitCellAngles.x * Mathf.Deg2Rad;
            float betaAngle = parameters.unitCellAngles.y * Mathf.Deg2Rad;
            float gammaAngle = parameters.unitCellAngles.z * Mathf.Deg2Rad;
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 複雑な三斜変形を適用
                float newX = vertex.x * parameters.unitCellDimensions.x + 
                           vertex.y * parameters.unitCellDimensions.y * Mathf.Cos(gammaAngle) +
                           vertex.z * parameters.unitCellDimensions.z * Mathf.Cos(betaAngle);
                           
                float newY = vertex.y * parameters.unitCellDimensions.y * Mathf.Sin(gammaAngle) +
                           vertex.z * parameters.unitCellDimensions.z * 
                           (Mathf.Cos(alphaAngle) - Mathf.Cos(betaAngle) * Mathf.Cos(gammaAngle)) / Mathf.Sin(gammaAngle);
                           
                float newZ = vertex.z * parameters.unitCellDimensions.z * 
                           Mathf.Sqrt(1f - Mathf.Cos(alphaAngle) * Mathf.Cos(alphaAngle) - 
                                     Mathf.Cos(betaAngle) * Mathf.Cos(betaAngle) - 
                                     Mathf.Cos(gammaAngle) * Mathf.Cos(gammaAngle) + 
                                     2f * Mathf.Cos(alphaAngle) * Mathf.Cos(betaAngle) * Mathf.Cos(gammaAngle)) / 
                           Mathf.Sin(gammaAngle);
                
                vertices[i] = new Vector3(newX, newY, newZ);
            }
            
            crystal.positions = vertices;
            return crystal;
        }
        #endregion        
#region 結晶面処理
        /// <summary>
        /// 結晶面を適用
        /// </summary>
        private static void ApplyCrystalFaces(ProBuilderMesh crystal, CrystalGenerationParams parameters)
        {
            if (parameters.activeFaces == null || parameters.activeFaces.Length == 0)
                return;

            var vertices = crystal.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                Vector3 modifiedVertex = vertex;
                
                // 各活性面の影響を適用
                for (int faceIndex = 0; faceIndex < parameters.activeFaces.Length; faceIndex++)
                {
                    CrystalFace face = parameters.activeFaces[faceIndex];
                    float weight = GetFaceWeight(face, parameters, faceIndex);
                    
                    modifiedVertex = ApplyFaceInfluence(modifiedVertex, face, weight, parameters);
                }
                
                vertices[i] = modifiedVertex;
            }
            
            crystal.positions = vertices;
        }

        /// <summary>
        /// 特定の結晶面の影響を適用
        /// </summary>
        private static Vector3 ApplyFaceInfluence(Vector3 vertex, CrystalFace face, float weight, CrystalGenerationParams parameters)
        {
            switch (face)
            {
                case CrystalFace.Cube:
                    return ApplyCubeFace(vertex, weight);
                case CrystalFace.Octahedron:
                    return ApplyOctahedronFace(vertex, weight);
                case CrystalFace.Pyramid:
                    return ApplyPyramidFace(vertex, weight);
                case CrystalFace.Prism:
                    return ApplyPrismFace(vertex, weight);
                default:
                    return vertex;
            }
        }

        /// <summary>
        /// 立方面の影響を適用
        /// </summary>
        private static Vector3 ApplyCubeFace(Vector3 vertex, float weight)
        {
            // 各軸方向の最大成分を強調
            float maxComponent = Mathf.Max(Mathf.Abs(vertex.x), Mathf.Abs(vertex.y), Mathf.Abs(vertex.z));
            
            if (Mathf.Abs(vertex.x) == maxComponent)
                vertex.x = Mathf.Sign(vertex.x) * Mathf.Lerp(Mathf.Abs(vertex.x), maxComponent, weight);
            if (Mathf.Abs(vertex.y) == maxComponent)
                vertex.y = Mathf.Sign(vertex.y) * Mathf.Lerp(Mathf.Abs(vertex.y), maxComponent, weight);
            if (Mathf.Abs(vertex.z) == maxComponent)
                vertex.z = Mathf.Sign(vertex.z) * Mathf.Lerp(Mathf.Abs(vertex.z), maxComponent, weight);
                
            return vertex;
        }

        /// <summary>
        /// 八面体面の影響を適用
        /// </summary>
        private static Vector3 ApplyOctahedronFace(Vector3 vertex, float weight)
        {
            // 八面体の条件: |x| + |y| + |z| = constant
            float octahedralDistance = Mathf.Abs(vertex.x) + Mathf.Abs(vertex.y) + Mathf.Abs(vertex.z);
            float sphericalDistance = vertex.magnitude;
            
            float targetDistance = Mathf.Lerp(sphericalDistance, octahedralDistance * 0.577f, weight);
            return vertex.normalized * targetDistance;
        }

        /// <summary>
        /// 錐面の影響を適用
        /// </summary>
        private static Vector3 ApplyPyramidFace(Vector3 vertex, float weight)
        {
            // 上部を尖らせる
            if (vertex.y > 0)
            {
                float heightFactor = vertex.y;
                float tapering = Mathf.Lerp(1f, 1f - heightFactor * 0.5f, weight);
                vertex.x *= tapering;
                vertex.z *= tapering;
                vertex.y *= Mathf.Lerp(1f, 1.2f, weight); // 高さを少し増加
            }
            
            return vertex;
        }

        /// <summary>
        /// 柱面の影響を適用
        /// </summary>
        private static Vector3 ApplyPrismFace(Vector3 vertex, float weight)
        {
            // Y軸方向は保持、XZ平面で正多角形に近づける
            if (Mathf.Abs(vertex.y) < 0.9f) // 側面の頂点のみ
            {
                float angle = Mathf.Atan2(vertex.z, vertex.x);
                int sides = 6; // 六角柱
                float snapAngle = Mathf.Round(angle / (2f * Mathf.PI / sides)) * (2f * Mathf.PI / sides);
                
                float radius = new Vector2(vertex.x, vertex.z).magnitude;
                Vector3 snappedVertex = new Vector3(
                    radius * Mathf.Cos(snapAngle),
                    vertex.y,
                    radius * Mathf.Sin(snapAngle)
                );
                
                vertex = Vector3.Lerp(vertex, snappedVertex, weight);
            }
            
            return vertex;
        }

        /// <summary>
        /// 結晶面の重みを取得
        /// </summary>
        private static float GetFaceWeight(CrystalFace face, CrystalGenerationParams parameters, int faceIndex = -1)
        {
            if (faceIndex >= 0 && faceIndex < parameters.faceWeights.Length)
            {
                return parameters.faceWeights[faceIndex];
            }
            
            // デフォルト重み
            return 0.5f;
        }
        #endregion   
     #region 成長パターン処理
        /// <summary>
        /// 結晶成長パターンを適用
        /// </summary>
        private static void ApplyGrowthPattern(ProBuilderMesh crystal, CrystalGenerationParams parameters)
        {
            var vertices = crystal.positions.ToArray();
            
            // 成長速度に基づいて各軸方向の発達度を調整
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 各軸方向の成長速度を適用
                vertex.x *= parameters.growthRates.x;
                vertex.y *= parameters.growthRates.y;
                vertex.z *= parameters.growthRates.z;
                
                // 複雑度レベルに応じた追加変形
                if (parameters.complexityLevel > 1)
                {
                    vertex = ApplyComplexGrowthPattern(vertex, parameters);
                }
                
                vertices[i] = vertex;
            }
            
            crystal.positions = vertices;
            
            // 全体サイズを調整
            crystal.transform.localScale = Vector3.one * parameters.overallSize;
        }

        /// <summary>
        /// 複雑な成長パターンを適用
        /// </summary>
        private static Vector3 ApplyComplexGrowthPattern(Vector3 vertex, CrystalGenerationParams parameters)
        {
            float complexity = parameters.complexityLevel / 5f; // 0-1に正規化
            
            // レベル2: 段階的成長
            if (parameters.complexityLevel >= 2)
            {
                float distance = vertex.magnitude;
                float stepSize = 0.2f / complexity;
                float steppedDistance = Mathf.Floor(distance / stepSize) * stepSize;
                vertex = vertex.normalized * Mathf.Lerp(distance, steppedDistance, complexity * 0.3f);
            }
            
            // レベル3: 螺旋成長
            if (parameters.complexityLevel >= 3)
            {
                float angle = Mathf.Atan2(vertex.z, vertex.x);
                float spiralOffset = vertex.y * complexity * 0.1f;
                angle += spiralOffset;
                
                float radius = new Vector2(vertex.x, vertex.z).magnitude;
                vertex.x = radius * Mathf.Cos(angle);
                vertex.z = radius * Mathf.Sin(angle);
            }
            
            return vertex;
        }
        #endregion

        #region 不完全性処理
        /// <summary>
        /// 結晶の不完全性を適用
        /// </summary>
        private static void ApplyImperfections(ProBuilderMesh crystal, CrystalGenerationParams parameters)
        {
            var vertices = crystal.positions.ToArray();
            float intensity = parameters.imperfectionIntensity;
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 格子欠陥をシミュレート
                vertex = ApplyLatticeDefects(vertex, intensity);
                
                // 表面粗さを追加
                vertex = ApplySurfaceRoughness(vertex, intensity);
                
                vertices[i] = vertex;
            }
            
            crystal.positions = vertices;
        }

        /// <summary>
        /// 格子欠陥を適用
        /// </summary>
        private static Vector3 ApplyLatticeDefects(Vector3 vertex, float intensity)
        {
            // 点欠陥（空孔、格子間原子）
            float defectProbability = intensity * 0.1f;
            if (Random.value < defectProbability)
            {
                // 小さなランダム変位
                Vector3 displacement = Random.onUnitSphere * intensity * 0.05f;
                vertex += displacement;
            }
            
            return vertex;
        }

        /// <summary>
        /// 表面粗さを適用
        /// </summary>
        private static Vector3 ApplySurfaceRoughness(Vector3 vertex, float intensity)
        {
            // 表面に近い頂点に粗さを追加
            float surfaceThreshold = 0.8f;
            float distanceFromCenter = vertex.magnitude;
            
            if (distanceFromCenter > surfaceThreshold)
            {
                Vector3 normal = vertex.normalized;
                float roughness = (Mathf.PerlinNoise(vertex.x * 50f, vertex.z * 50f) - 0.5f) * intensity * 0.03f;
                vertex += normal * roughness;
            }
            
            return vertex;
        }

        /// <summary>
        /// 双晶を適用
        /// </summary>
        private static void ApplyTwinning(ProBuilderMesh crystal, CrystalGenerationParams parameters)
        {
            var vertices = crystal.positions.ToArray();
            
            // 双晶面を定義（例：YZ平面）
            Vector3 twinPlaneNormal = Vector3.right;
            float twinPlanePosition = 0f;
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 双晶面の片側の頂点を反転
                float distanceToPlane = Vector3.Dot(vertex - Vector3.right * twinPlanePosition, twinPlaneNormal);
                
                if (distanceToPlane > 0)
                {
                    // 双晶面に対して反転
                    Vector3 reflection = vertex - 2f * distanceToPlane * twinPlaneNormal;
                    
                    // 部分的に反転（完全な双晶ではなく、自然な変形）
                    vertex = Vector3.Lerp(vertex, reflection, 0.3f);
                }
                
                vertices[i] = vertex;
            }
            
            crystal.positions = vertices;
        }
        #endregion

        #region ユーティリティ関数
        /// <summary>
        /// ランダムな結晶系を取得
        /// </summary>
        public static CrystalSystem GetRandomCrystalSystem()
        {
            var systems = System.Enum.GetValues(typeof(CrystalSystem));
            return (CrystalSystem)systems.GetValue(Random.Range(0, systems.Length));
        }

        /// <summary>
        /// 結晶系の説明を取得
        /// </summary>
        public static string GetCrystalSystemDescription(CrystalSystem system)
        {
            switch (system)
            {
                case CrystalSystem.Cubic:
                    return "立方晶系 - 最高の対称性を持つ結晶系";
                case CrystalSystem.Hexagonal:
                    return "六方晶系 - 六角形の対称性を持つ結晶系";
                case CrystalSystem.Tetragonal:
                    return "正方晶系 - 四角形の対称性を持つ結晶系";
                case CrystalSystem.Orthorhombic:
                    return "斜方晶系 - 直交する不等な軸を持つ結晶系";
                case CrystalSystem.Monoclinic:
                    return "単斜晶系 - 一つの斜軸を持つ結晶系";
                case CrystalSystem.Triclinic:
                    return "三斜晶系 - 最低の対称性を持つ結晶系";
                default:
                    return "不明な結晶系";
            }
        }

        /// <summary>
        /// 結晶の品質評価
        /// </summary>
        public static float EvaluateCrystalQuality(ProBuilderMesh crystal, CrystalGenerationParams parameters)
        {
            if (crystal == null) return 0f;
            
            float quality = 1f;
            
            // 対称性の評価
            quality *= EvaluateSymmetry(crystal, parameters.crystalSystem);
            
            // 面の発達度の評価
            quality *= EvaluateFaceDevelopment(crystal, parameters);
            
            // 不完全性の適切さの評価
            quality *= EvaluateImperfectionLevel(crystal, parameters);
            
            return Mathf.Clamp01(quality);
        }

        /// <summary>
        /// 対称性を評価
        /// </summary>
        private static float EvaluateSymmetry(ProBuilderMesh crystal, CrystalSystem system)
        {
            // 簡単な対称性チェック（実装は簡略化）
            var vertices = crystal.positions;
            
            switch (system)
            {
                case CrystalSystem.Cubic:
                    // 立方対称性をチェック
                    return 0.9f; // 簡略化された評価
                case CrystalSystem.Hexagonal:
                    // 六方対称性をチェック
                    return 0.85f;
                default:
                    return 0.8f;
            }
        }

        /// <summary>
        /// 面の発達度を評価
        /// </summary>
        private static float EvaluateFaceDevelopment(ProBuilderMesh crystal, CrystalGenerationParams parameters)
        {
            // 活性面の数と重みに基づく評価
            if (parameters.activeFaces == null || parameters.activeFaces.Length == 0)
                return 0.5f;
                
            float averageWeight = 0f;
            if (parameters.faceWeights != null && parameters.faceWeights.Length > 0)
            {
                averageWeight = parameters.faceWeights.Average();
            }
            else
            {
                averageWeight = 0.5f;
            }
            
            return Mathf.Clamp01(averageWeight);
        }

        /// <summary>
        /// 不完全性レベルを評価
        /// </summary>
        private static float EvaluateImperfectionLevel(ProBuilderMesh crystal, CrystalGenerationParams parameters)
        {
            // 適切な不完全性レベル（0.05-0.2が理想的）
            float idealRange = 0.15f;
            float deviation = Mathf.Abs(parameters.imperfectionIntensity - idealRange);
            
            return 1f - (deviation / idealRange);
        }
        #endregion
    }
}