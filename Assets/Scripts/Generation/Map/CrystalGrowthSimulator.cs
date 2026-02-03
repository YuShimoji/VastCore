using UnityEngine;
using UnityEngine.ProBuilder;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Generation
{
    /// <summary>
    /// 自然な結晶成長シミュレーションシステム
    /// 結晶成長アルゴリズム、不完全性、自然な欠陥の生成を実装
    /// </summary>
    [System.Obsolete("Experimental crystal growth simulator. Not used in core terrain pipeline.")]
    public static class CrystalGrowthSimulator
    {
        #region 成長パラメータ定義
        /// <summary>
        /// 結晶成長パラメータ
        /// </summary>
        [System.Serializable]
        public struct GrowthSimulationParams
        {
            [Header("成長環境")]
            public float temperature;              // 温度 (K)
            public float supersaturation;          // 過飽和度
            public float nucleationRate;           // 核生成速度
            public Vector3 growthDirection;        // 主成長方向
            
            [Header("成長速度")]
            public float[] faceGrowthRates;        // 各面の成長速度
            public float diffusionRate;           // 拡散速度
            public float attachmentRate;          // 付着速度
            
            [Header("欠陥生成")]
            public float defectProbability;       // 欠陥生成確率
            public float inclusionRate;           // 包有物生成率
            public float twinningProbability;     // 双晶確率
            public float disorderIntensity;       // 無秩序度
            
            [Header("環境変動")]
            public bool enableEnvironmentalChanges; // 環境変化を有効化
            public float temperatureVariation;     // 温度変動
            public float concentrationVariation;   // 濃度変動
            public int growthCycles;               // 成長サイクル数
            
            public static GrowthSimulationParams Default()
            {
                return new GrowthSimulationParams
                {
                    temperature = 298f,              // 室温
                    supersaturation = 1.2f,          // 20%過飽和
                    nucleationRate = 0.1f,
                    growthDirection = Vector3.up,
                    faceGrowthRates = new float[] { 1.0f, 0.8f, 0.6f, 0.9f },
                    diffusionRate = 0.5f,
                    attachmentRate = 0.7f,
                    defectProbability = 0.05f,
                    inclusionRate = 0.02f,
                    twinningProbability = 0.1f,
                    disorderIntensity = 0.1f,
                    enableEnvironmentalChanges = true,
                    temperatureVariation = 10f,
                    concentrationVariation = 0.1f,
                    growthCycles = 5
                };
            }
        }

        /// <summary>
        /// 成長ステップ情報
        /// </summary>
        public struct GrowthStep
        {
            public int stepNumber;
            public float timeElapsed;
            public Vector3 crystalSize;
            public float currentTemperature;
            public float currentSupersaturation;
            public List<Vector3> newDefects;
            public List<Vector3> growthSites;
        }
        #endregion

        #region メイン成長シミュレーション
        /// <summary>
        /// 結晶成長シミュレーションを実行
        /// </summary>
        public static ProBuilderMesh SimulateCrystalGrowth(
            CrystalStructureGenerator.CrystalGenerationParams crystalParams,
            GrowthSimulationParams growthParams)
        {
            try
            {
                Debug.Log($"Starting crystal growth simulation for {crystalParams.crystalSystem}");
                
                // 初期結晶核を生成
                ProBuilderMesh crystal = GenerateInitialNucleus(crystalParams);
                
                if (crystal == null)
                {
                    Debug.LogError("Failed to generate initial crystal nucleus");
                    return null;
                }

                // 成長履歴を記録
                List<GrowthStep> growthHistory = new List<GrowthStep>();
                
                // 段階的成長シミュレーション
                for (int cycle = 0; cycle < growthParams.growthCycles; cycle++)
                {
                    GrowthStep step = SimulateGrowthCycle(crystal, crystalParams, growthParams, cycle);
                    growthHistory.Add(step);
                    
                    // 環境変化を適用
                    if (growthParams.enableEnvironmentalChanges)
                    {
                        ApplyEnvironmentalChanges(ref growthParams, cycle);
                    }
                }

                // 最終的な不完全性を適用
                ApplyFinalImperfections(crystal, crystalParams, growthParams, growthHistory);
                
                // サイズ変動を適用
                ApplySizeVariations(crystal, crystalParams, growthParams);
                
                // メッシュを最終化
                crystal.ToMesh();
                crystal.Refresh();

                Debug.Log($"Crystal growth simulation completed with {growthHistory.Count} growth cycles");
                return crystal;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in crystal growth simulation: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 初期結晶核を生成
        /// </summary>
        private static ProBuilderMesh GenerateInitialNucleus(CrystalStructureGenerator.CrystalGenerationParams crystalParams)
        {
            // 小さな初期核から開始
            var nucleusParams = crystalParams;
            nucleusParams.overallSize *= 0.1f; // 初期サイズは最終サイズの10%
            nucleusParams.complexityLevel = 1;  // 初期は単純な形状
            nucleusParams.imperfectionIntensity = 0.02f; // 初期は欠陥が少ない
            
            return CrystalStructureGenerator.GenerateCrystalStructure(nucleusParams);
        }

        /// <summary>
        /// 単一成長サイクルをシミュレート
        /// </summary>
        private static GrowthStep SimulateGrowthCycle(
            ProBuilderMesh crystal, 
            CrystalStructureGenerator.CrystalGenerationParams crystalParams,
            GrowthSimulationParams growthParams, 
            int cycleNumber)
        {
            GrowthStep step = new GrowthStep
            {
                stepNumber = cycleNumber,
                timeElapsed = cycleNumber * 1.0f,
                currentTemperature = growthParams.temperature,
                currentSupersaturation = growthParams.supersaturation,
                newDefects = new List<Vector3>(),
                growthSites = new List<Vector3>()
            };

            // 成長サイトを特定
            IdentifyGrowthSites(crystal, growthParams, step);
            
            // 面成長を適用
            ApplyFaceGrowth(crystal, crystalParams, growthParams, step);
            
            // 欠陥を生成
            GenerateGrowthDefects(crystal, growthParams, step);
            
            // 包有物を追加
            AddInclusions(crystal, growthParams, step);
            
            // 現在のサイズを記録
            step.crystalSize = CalculateCrystalSize(crystal);
            
            return step;
        }

        /// <summary>
        /// 成長サイトを特定
        /// </summary>
        private static void IdentifyGrowthSites(ProBuilderMesh crystal, GrowthSimulationParams growthParams, GrowthStep step)
        {
            var vertices = crystal.positions.ToArray();
            
            // 表面の頂点を成長サイトとして特定
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float distanceFromCenter = vertex.magnitude;
                
                // 表面に近い頂点を成長サイト候補とする
                if (distanceFromCenter > 0.8f)
                {
                    // 成長確率を計算（温度と過飽和度に依存）
                    float growthProbability = CalculateGrowthProbability(vertex, growthParams);
                    
                    if (Random.value < growthProbability)
                    {
                        step.growthSites.Add(vertex);
                    }
                }
            }
        }

        /// <summary>
        /// 成長確率を計算
        /// </summary>
        private static float CalculateGrowthProbability(Vector3 position, GrowthSimulationParams growthParams)
        {
            // Arrhenius方程式に基づく温度依存性
            float activationEnergy = 50000f; // J/mol (仮想値)
            float gasConstant = 8.314f; // J/(mol·K)
            float temperatureFactor = Mathf.Exp(-activationEnergy / (gasConstant * growthParams.temperature));
            
            // 過飽和度の影響
            float supersaturationFactor = Mathf.Pow(growthParams.supersaturation, 2f);
            
            // 拡散の影響
            float diffusionFactor = growthParams.diffusionRate;
            
            // 方向性の影響（主成長方向に近いほど成長しやすい）
            float directionFactor = Vector3.Dot(position.normalized, growthParams.growthDirection.normalized);
            directionFactor = (directionFactor + 1f) / 2f; // 0-1に正規化
            
            return temperatureFactor * supersaturationFactor * diffusionFactor * directionFactor * 0.1f;
        }

        /// <summary>
        /// 面成長を適用
        /// </summary>
        private static void ApplyFaceGrowth(
            ProBuilderMesh crystal, 
            CrystalStructureGenerator.CrystalGenerationParams crystalParams,
            GrowthSimulationParams growthParams, 
            GrowthStep step)
        {
            var vertices = crystal.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 成長サイトでの成長を適用
                if (step.growthSites.Contains(vertex))
                {
                    Vector3 growthVector = CalculateGrowthVector(vertex, crystalParams, growthParams);
                    vertices[i] = vertex + growthVector;
                }
            }
            
            crystal.positions = vertices;
        }

        /// <summary>
        /// 成長ベクトルを計算
        /// </summary>
        private static Vector3 CalculateGrowthVector(
            Vector3 position, 
            CrystalStructureGenerator.CrystalGenerationParams crystalParams,
            GrowthSimulationParams growthParams)
        {
            // 法線方向の成長
            Vector3 normal = position.normalized;
            
            // 結晶系に応じた成長異方性
            Vector3 anisotropicGrowth = ApplyGrowthAnisotropy(normal, crystalParams.crystalSystem, growthParams);
            
            // 成長速度を適用
            float growthRate = growthParams.attachmentRate * growthParams.supersaturation * 0.01f;
            
            return anisotropicGrowth * growthRate;
        }

        /// <summary>
        /// 成長異方性を適用
        /// </summary>
        private static Vector3 ApplyGrowthAnisotropy(
            Vector3 normal, 
            CrystalStructureGenerator.CrystalSystem crystalSystem,
            GrowthSimulationParams growthParams)
        {
            switch (crystalSystem)
            {
                case CrystalStructureGenerator.CrystalSystem.Cubic:
                    // 立方晶系：等方的成長
                    return normal;
                    
                case CrystalStructureGenerator.CrystalSystem.Hexagonal:
                    // 六方晶系：c軸方向（Y軸）が優先
                    Vector3 hexGrowth = normal;
                    hexGrowth.y *= 1.5f; // c軸方向を強化
                    return hexGrowth.normalized;
                    
                case CrystalStructureGenerator.CrystalSystem.Tetragonal:
                    // 正方晶系：c軸方向が異なる
                    Vector3 tetGrowth = normal;
                    tetGrowth.y *= 1.3f;
                    return tetGrowth.normalized;
                    
                default:
                    return normal;
            }
        }
        #endregion     
   #region 欠陥生成システム
        /// <summary>
        /// 成長中の欠陥を生成
        /// </summary>
        private static void GenerateGrowthDefects(ProBuilderMesh crystal, GrowthSimulationParams growthParams, GrowthStep step)
        {
            var vertices = crystal.positions.ToArray();
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 欠陥生成確率をチェック
                if (Random.value < growthParams.defectProbability)
                {
                    // 欠陥タイプを決定
                    DefectType defectType = DetermineDefectType(vertex, growthParams);
                    
                    // 欠陥を適用
                    Vector3 defectedVertex = ApplyDefect(vertex, defectType, growthParams);
                    vertices[i] = defectedVertex;
                    
                    // 欠陥位置を記録
                    step.newDefects.Add(defectedVertex);
                }
            }
            
            crystal.positions = vertices;
        }

        /// <summary>
        /// 欠陥タイプ
        /// </summary>
        private enum DefectType
        {
            Vacancy,        // 空孔
            Interstitial,   // 格子間原子
            Substitution,   // 置換原子
            Dislocation,    // 転位
            GrainBoundary,  // 粒界
            Stacking        // 積層欠陥
        }

        /// <summary>
        /// 欠陥タイプを決定
        /// </summary>
        private static DefectType DetermineDefectType(Vector3 position, GrowthSimulationParams growthParams)
        {
            float random = Random.value;
            
            // 温度が高いほど転位が発生しやすい
            float temperatureFactor = growthParams.temperature / 300f;
            
            if (random < 0.3f * temperatureFactor)
                return DefectType.Dislocation;
            else if (random < 0.5f)
                return DefectType.Vacancy;
            else if (random < 0.7f)
                return DefectType.Interstitial;
            else if (random < 0.85f)
                return DefectType.Substitution;
            else if (random < 0.95f)
                return DefectType.GrainBoundary;
            else
                return DefectType.Stacking;
        }

        /// <summary>
        /// 欠陥を適用
        /// </summary>
        private static Vector3 ApplyDefect(Vector3 vertex, DefectType defectType, GrowthSimulationParams growthParams)
        {
            float intensity = growthParams.disorderIntensity;
            
            switch (defectType)
            {
                case DefectType.Vacancy:
                    // 空孔：頂点を内側に移動
                    return vertex * (1f - intensity * 0.1f);
                    
                case DefectType.Interstitial:
                    // 格子間原子：頂点を外側に移動
                    return vertex * (1f + intensity * 0.05f);
                    
                case DefectType.Substitution:
                    // 置換原子：わずかな位置変化
                    Vector3 substitutionOffset = Random.onUnitSphere * intensity * 0.02f;
                    return vertex + substitutionOffset;
                    
                case DefectType.Dislocation:
                    // 転位：線状の歪み
                    Vector3 dislocationDirection = Random.onUnitSphere;
                    float dislocationStrength = intensity * 0.03f;
                    return vertex + dislocationDirection * dislocationStrength;
                    
                case DefectType.GrainBoundary:
                    // 粒界：境界での不連続
                    Vector3 boundaryOffset = Random.onUnitSphere * intensity * 0.04f;
                    return vertex + boundaryOffset;
                    
                case DefectType.Stacking:
                    // 積層欠陥：層方向の変位
                    Vector3 stackingOffset = Vector3.up * intensity * 0.02f;
                    return vertex + stackingOffset;
                    
                default:
                    return vertex;
            }
        }

        /// <summary>
        /// 包有物を追加
        /// </summary>
        private static void AddInclusions(ProBuilderMesh crystal, GrowthSimulationParams growthParams, GrowthStep step)
        {
            if (Random.value < growthParams.inclusionRate)
            {
                var vertices = crystal.positions.ToArray();
                
                // ランダムな位置に包有物を配置
                int inclusionCount = Random.Range(1, 4);
                
                for (int i = 0; i < inclusionCount; i++)
                {
                    Vector3 inclusionCenter = Random.insideUnitSphere * 0.8f;
                    float inclusionRadius = Random.Range(0.05f, 0.15f);
                    
                    // 包有物周辺の頂点を変形
                    for (int j = 0; j < vertices.Length; j++)
                    {
                        Vector3 vertex = vertices[j];
                        float distanceToInclusion = Vector3.Distance(vertex, inclusionCenter);
                        
                        if (distanceToInclusion < inclusionRadius)
                        {
                            // 包有物による局所的な歪み
                            Vector3 distortionDirection = (vertex - inclusionCenter).normalized;
                            float distortionStrength = (1f - distanceToInclusion / inclusionRadius) * 0.1f;
                            vertices[j] = vertex + distortionDirection * distortionStrength;
                        }
                    }
                }
                
                crystal.positions = vertices;
            }
        }
        #endregion

        #region 環境変化システム
        /// <summary>
        /// 環境変化を適用
        /// </summary>
        private static void ApplyEnvironmentalChanges(ref GrowthSimulationParams growthParams, int cycleNumber)
        {
            // 温度変動
            float temperatureNoise = Mathf.PerlinNoise(cycleNumber * 0.1f, 0f) - 0.5f;
            growthParams.temperature += temperatureNoise * growthParams.temperatureVariation;
            
            // 濃度変動
            float concentrationNoise = Mathf.PerlinNoise(0f, cycleNumber * 0.1f) - 0.5f;
            growthParams.supersaturation += concentrationNoise * growthParams.concentrationVariation;
            
            // 過飽和度の下限を設定
            growthParams.supersaturation = Mathf.Max(1.0f, growthParams.supersaturation);
            
            // 成長方向の微小変化
            Vector3 directionNoise = new Vector3(
                Mathf.PerlinNoise(cycleNumber * 0.05f, 0f) - 0.5f,
                Mathf.PerlinNoise(0f, cycleNumber * 0.05f) - 0.5f,
                Mathf.PerlinNoise(cycleNumber * 0.05f, cycleNumber * 0.05f) - 0.5f
            ) * 0.1f;
            
            growthParams.growthDirection = (growthParams.growthDirection + directionNoise).normalized;
        }

        /// <summary>
        /// 最終的な不完全性を適用
        /// </summary>
        private static void ApplyFinalImperfections(
            ProBuilderMesh crystal, 
            CrystalStructureGenerator.CrystalGenerationParams crystalParams,
            GrowthSimulationParams growthParams, 
            List<GrowthStep> growthHistory)
        {
            var vertices = crystal.positions.ToArray();
            
            // 成長履歴に基づく累積的な歪み
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                Vector3 cumulativeStrain = Vector3.zero;
                
                // 各成長ステップの影響を累積
                foreach (var step in growthHistory)
                {
                    float stepInfluence = CalculateStepInfluence(vertex, step);
                    cumulativeStrain += Random.onUnitSphere * stepInfluence * 0.01f;
                }
                
                vertices[i] = vertex + cumulativeStrain;
            }
            
            // 表面テクスチャを追加
            ApplySurfaceTexture(vertices, growthParams);
            
            crystal.positions = vertices;
        }

        /// <summary>
        /// ステップの影響を計算
        /// </summary>
        private static float CalculateStepInfluence(Vector3 vertex, GrowthStep step)
        {
            float influence = 0f;
            
            // 欠陥からの距離に基づく影響
            foreach (var defect in step.newDefects)
            {
                float distance = Vector3.Distance(vertex, defect);
                influence += Mathf.Exp(-distance * 5f) * 0.1f;
            }
            
            // 成長サイトからの距離に基づく影響
            foreach (var growthSite in step.growthSites)
            {
                float distance = Vector3.Distance(vertex, growthSite);
                influence += Mathf.Exp(-distance * 3f) * 0.05f;
            }
            
            return influence;
        }

        /// <summary>
        /// 表面テクスチャを適用
        /// </summary>
        private static void ApplySurfaceTexture(Vector3[] vertices, GrowthSimulationParams growthParams)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                float distanceFromCenter = vertex.magnitude;
                
                // 表面に近い頂点にテクスチャを適用
                if (distanceFromCenter > 0.7f)
                {
                    // 多層ノイズによる表面テクスチャ
                    float texture1 = Mathf.PerlinNoise(vertex.x * 20f, vertex.z * 20f) * 0.02f;
                    float texture2 = Mathf.PerlinNoise(vertex.x * 50f, vertex.z * 50f) * 0.01f;
                    float texture3 = Mathf.PerlinNoise(vertex.x * 100f, vertex.z * 100f) * 0.005f;
                    
                    Vector3 normal = vertex.normalized;
                    Vector3 textureOffset = normal * (texture1 + texture2 + texture3) * growthParams.disorderIntensity;
                    
                    vertices[i] = vertex + textureOffset;
                }
            }
        }
        #endregion

        #region サイズ変動システム
        /// <summary>
        /// サイズ変動を適用
        /// </summary>
        private static void ApplySizeVariations(
            ProBuilderMesh crystal, 
            CrystalStructureGenerator.CrystalGenerationParams crystalParams,
            GrowthSimulationParams growthParams)
        {
            var vertices = crystal.positions.ToArray();
            
            // 結晶系に応じた自然なサイズ変動
            Vector3 sizeVariation = CalculateNaturalSizeVariation(crystalParams.crystalSystem, growthParams);
            
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 vertex = vertices[i];
                
                // 各軸方向に異なる変動を適用
                vertex.x *= sizeVariation.x;
                vertex.y *= sizeVariation.y;
                vertex.z *= sizeVariation.z;
                
                // 局所的なサイズ変動
                float localVariation = 1f + (Mathf.PerlinNoise(vertex.x * 5f, vertex.z * 5f) - 0.5f) * 0.1f;
                vertex *= localVariation;
                
                vertices[i] = vertex;
            }
            
            crystal.positions = vertices;
        }

        /// <summary>
        /// 自然なサイズ変動を計算
        /// </summary>
        private static Vector3 CalculateNaturalSizeVariation(
            CrystalStructureGenerator.CrystalSystem crystalSystem,
            GrowthSimulationParams growthParams)
        {
            Vector3 baseVariation = Vector3.one;
            
            // 結晶系に応じた異方性
            switch (crystalSystem)
            {
                case CrystalStructureGenerator.CrystalSystem.Cubic:
                    // 立方晶系：等方的だが小さな変動
                    baseVariation = Vector3.one + Random.insideUnitSphere * 0.05f;
                    break;
                    
                case CrystalStructureGenerator.CrystalSystem.Hexagonal:
                    // 六方晶系：c軸方向が優先的に成長
                    baseVariation = new Vector3(
                        1f + Random.Range(-0.1f, 0.1f),
                        1f + Random.Range(0f, 0.3f),  // c軸方向
                        1f + Random.Range(-0.1f, 0.1f)
                    );
                    break;
                    
                case CrystalStructureGenerator.CrystalSystem.Tetragonal:
                    // 正方晶系：c軸が異なる
                    baseVariation = new Vector3(
                        1f + Random.Range(-0.08f, 0.08f),
                        1f + Random.Range(-0.05f, 0.2f),
                        1f + Random.Range(-0.08f, 0.08f)
                    );
                    break;
                    
                case CrystalStructureGenerator.CrystalSystem.Orthorhombic:
                    // 斜方晶系：三軸すべて異なる
                    baseVariation = new Vector3(
                        1f + Random.Range(-0.15f, 0.15f),
                        1f + Random.Range(-0.1f, 0.1f),
                        1f + Random.Range(-0.12f, 0.12f)
                    );
                    break;
                    
                default:
                    baseVariation = Vector3.one + Random.insideUnitSphere * 0.1f;
                    break;
            }
            
            // 成長条件による影響
            float temperatureEffect = (growthParams.temperature - 298f) / 298f * 0.1f;
            float supersaturationEffect = (growthParams.supersaturation - 1f) * 0.2f;
            
            baseVariation *= (1f + temperatureEffect + supersaturationEffect);
            
            return baseVariation;
        }
        #endregion

        #region ユーティリティ関数
        /// <summary>
        /// 結晶サイズを計算
        /// </summary>
        private static Vector3 CalculateCrystalSize(ProBuilderMesh crystal)
        {
            var vertices = crystal.positions;
            
            if (vertices.Count == 0)
                return Vector3.zero;
                
            Vector3 min = vertices[0];
            Vector3 max = vertices[0];
            
            foreach (var vertex in vertices)
            {
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
            }
            
            return max - min;
        }

        /// <summary>
        /// 成長品質を評価
        /// </summary>
        public static float EvaluateGrowthQuality(List<GrowthStep> growthHistory, GrowthSimulationParams growthParams)
        {
            if (growthHistory == null || growthHistory.Count == 0)
                return 0f;
                
            float quality = 1f;
            
            // 成長の一貫性を評価
            quality *= EvaluateGrowthConsistency(growthHistory);
            
            // 欠陥密度を評価
            quality *= EvaluateDefectDensity(growthHistory, growthParams);
            
            // サイズ変化の自然さを評価
            quality *= EvaluateSizeProgression(growthHistory);
            
            return Mathf.Clamp01(quality);
        }

        /// <summary>
        /// 成長の一貫性を評価
        /// </summary>
        private static float EvaluateGrowthConsistency(List<GrowthStep> growthHistory)
        {
            if (growthHistory.Count < 2)
                return 1f;
                
            float consistency = 1f;
            
            for (int i = 1; i < growthHistory.Count; i++)
            {
                Vector3 currentSize = growthHistory[i].crystalSize;
                Vector3 previousSize = growthHistory[i - 1].crystalSize;
                
                // サイズ変化の一貫性をチェック
                Vector3 sizeChange = currentSize - previousSize;
                float changeConsistency = 1f - Mathf.Abs(sizeChange.magnitude - 0.1f) / 0.1f;
                consistency *= Mathf.Clamp01(changeConsistency);
            }
            
            return consistency;
        }

        /// <summary>
        /// 欠陥密度を評価
        /// </summary>
        private static float EvaluateDefectDensity(List<GrowthStep> growthHistory, GrowthSimulationParams growthParams)
        {
            int totalDefects = 0;
            foreach (var step in growthHistory)
            {
                totalDefects += step.newDefects.Count;
            }
            
            // 適切な欠陥密度（5-15%が理想的）
            float defectRatio = totalDefects / (float)(growthHistory.Count * 10);
            float idealRatio = 0.1f;
            
            return 1f - Mathf.Abs(defectRatio - idealRatio) / idealRatio;
        }

        /// <summary>
        /// サイズ進行の自然さを評価
        /// </summary>
        private static float EvaluateSizeProgression(List<GrowthStep> growthHistory)
        {
            if (growthHistory.Count < 2)
                return 1f;
                
            float naturalness = 1f;
            
            for (int i = 1; i < growthHistory.Count; i++)
            {
                float currentMagnitude = growthHistory[i].crystalSize.magnitude;
                float previousMagnitude = growthHistory[i - 1].crystalSize.magnitude;
                
                // 成長は単調増加であるべき
                if (currentMagnitude < previousMagnitude)
                {
                    naturalness *= 0.8f;
                }
                
                // 成長速度の自然さ
                float growthRate = (currentMagnitude - previousMagnitude) / previousMagnitude;
                if (growthRate > 0.5f || growthRate < 0.01f) // 異常な成長速度
                {
                    naturalness *= 0.9f;
                }
            }
            
            return naturalness;
        }

        /// <summary>
        /// 成長履歴をログ出力
        /// </summary>
        public static void LogGrowthHistory(List<GrowthStep> growthHistory)
        {
            Debug.Log("=== Crystal Growth History ===");
            
            foreach (var step in growthHistory)
            {
                Debug.Log($"Step {step.stepNumber}: Size={step.crystalSize}, " +
                         $"Temp={step.currentTemperature:F1}K, " +
                         $"Supersaturation={step.currentSupersaturation:F2}, " +
                         $"Defects={step.newDefects.Count}, " +
                         $"GrowthSites={step.growthSites.Count}");
            }
            
            Debug.Log("=== End Growth History ===");
        }
        #endregion
    }
}