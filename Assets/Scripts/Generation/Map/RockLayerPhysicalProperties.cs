using UnityEngine;
using System.Collections.Generic;
using System;
using Vastcore.Core;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// 岩石層の物理的特性を管理するクラス
    /// 岩石タイプ別の硬度、色彩、テクスチャ、風化・浸食パターンを実装
    /// </summary>
    public class RockLayerPhysicalProperties : MonoBehaviour
    {
        [Header("岩石物理特性設定")]
        [SerializeField] private bool enableWeathering = true;
        [SerializeField] private bool enableErosion = true;
        [SerializeField] private float weatheringRate = 0.1f;
        [SerializeField] private float erosionRate = 0.05f;
        
        [Header("地層構造設定")]
        [SerializeField] private bool enableFaultGeneration = true;
        [SerializeField] private float faultProbability = 0.2f;
        [SerializeField] private int maxFaultLayers = 3;
        
        [Header("テクスチャ設定")]
        [SerializeField] private Material[] sedimentaryMaterials;
        [SerializeField] private Material[] igneousMaterials;
        [SerializeField] private Material[] metamorphicMaterials;
        
        private Dictionary<GeologicalFormationGenerator.RockFormationType, RockTypeProperties> rockProperties;
        private System.Random physicsRandom;
        
        /// <summary>
        /// 岩石タイプ別の物理特性定義
        /// </summary>
        [System.Serializable]
        public class RockTypeProperties
        {
            [Header("基本物理特性")]
            public float baseHardness;              // モース硬度
            public float density;                   // 密度 (g/cm³)
            public float porosity;                  // 多孔率 (%)
            public float permeability;              // 透水性
            
            [Header("色彩・外観")]
            public Color baseColor;
            public Color weatheredColor;
            public float colorVariation;            // 色の変動範囲
            public float roughness;                 // 表面粗さ
            public float metallic;                  // 金属光沢
            
            [Header("風化特性")]
            public float weatheringResistance;      // 風化抵抗性
            public WeatheringType primaryWeathering;
            public float weatheringRate;
            
            [Header("浸食特性")]
            public float erosionResistance;         // 浸食抵抗性
            public ErosionType primaryErosion;
            public float erosionRate;
            
            [Header("構造特性")]
            public float jointSpacing;              // 節理間隔
            public float faultSusceptibility;       // 断層形成しやすさ
            public bool isLayered;                  // 層状構造の有無
            
            public RockTypeProperties()
            {
                baseHardness = 5f;
                density = 2.5f;
                porosity = 10f;
                permeability = 0.1f;
                baseColor = Color.gray;
                weatheredColor = Color.gray;
                colorVariation = 0.2f;
                roughness = 0.5f;
                metallic = 0f;
                weatheringResistance = 0.5f;
                primaryWeathering = WeatheringType.Chemical;
                weatheringRate = 0.1f;
                erosionResistance = 0.5f;
                primaryErosion = ErosionType.Water;
                erosionRate = 0.05f;
                jointSpacing = 1f;
                faultSusceptibility = 0.3f;
                isLayered = false;
            }
        }
        
        /// <summary>
        /// 風化タイプの分類
        /// </summary>
        public enum WeatheringType
        {
            Physical,       // 物理的風化（凍結融解、熱膨張など）
            Chemical,       // 化学的風化（酸化、溶解など）
            Biological,     // 生物的風化（植物の根、微生物など）
            Combined        // 複合的風化
        }
        
        /// <summary>
        /// 浸食タイプの分類
        /// </summary>
        public enum ErosionType
        {
            Water,          // 水による浸食
            Wind,           // 風による浸食
            Ice,            // 氷河による浸食
            Gravity,        // 重力による浸食（崩落など）
            Chemical        // 化学的浸食
        }
        
        /// <summary>
        /// 断層構造の定義
        /// </summary>
        [System.Serializable]
        public class FaultStructure
        {
            public Vector3 faultPlane;              // 断層面の法線ベクトル
            public float displacement;              // 変位量
            public FaultType faultType;
            public float age;                       // 断層形成年代
            public bool isActive;                   // 活断層かどうか
            
            public enum FaultType
            {
                Normal,         // 正断層
                Reverse,        // 逆断層
                StrikeSlip,     // 横ずれ断層
                Thrust          // 衝上断層
            }
        }
        
        /// <summary>
        /// 地層の重なり構造
        /// </summary>
        [System.Serializable]
        public class StratigraphicSequence
        {
            public List<GeologicalFormationGenerator.GeologicalLayer> layers;
            public List<FaultStructure> faults;
            public float totalThickness;
            public bool isOverturned;               // 逆転構造
            public float dip;                       // 傾斜角
            public float strike;                    // 走向
            
            public StratigraphicSequence()
            {
                layers = new List<GeologicalFormationGenerator.GeologicalLayer>();
                faults = new List<FaultStructure>();
                totalThickness = 0f;
                isOverturned = false;
                dip = 0f;
                strike = 0f;
            }
        }
        
        void Start()
        {
            Initialize();
        }
        
        /// <summary>
        /// 物理特性システムの初期化
        /// </summary>
        public void Initialize()
        {
            physicsRandom = new System.Random();
            InitializeRockProperties();
            VastcoreLogger.Instance.LogInfo("RockPhysics", "Rock layer physical properties system initialized");
        }
        
        /// <summary>
        /// 岩石タイプ別の物理特性を初期化
        /// </summary>
        private void InitializeRockProperties()
        {
            rockProperties = new Dictionary<GeologicalFormationGenerator.RockFormationType, RockTypeProperties>();
            
            // 堆積岩の特性
            rockProperties[GeologicalFormationGenerator.RockFormationType.Sedimentary] = new RockTypeProperties
            {
                baseHardness = 3.5f,
                density = 2.3f,
                porosity = 15f,
                permeability = 0.3f,
                baseColor = new Color(0.8f, 0.7f, 0.5f, 1f),
                weatheredColor = new Color(0.6f, 0.5f, 0.3f, 1f),
                colorVariation = 0.3f,
                roughness = 0.7f,
                metallic = 0f,
                weatheringResistance = 0.3f,
                primaryWeathering = WeatheringType.Chemical,
                weatheringRate = 0.15f,
                erosionResistance = 0.4f,
                primaryErosion = ErosionType.Water,
                erosionRate = 0.1f,
                jointSpacing = 0.5f,
                faultSusceptibility = 0.6f,
                isLayered = true
            };
            
            // 火成岩の特性
            rockProperties[GeologicalFormationGenerator.RockFormationType.Igneous] = new RockTypeProperties
            {
                baseHardness = 6.5f,
                density = 2.8f,
                porosity = 5f,
                permeability = 0.05f,
                baseColor = new Color(0.3f, 0.3f, 0.3f, 1f),
                weatheredColor = new Color(0.4f, 0.3f, 0.2f, 1f),
                colorVariation = 0.2f,
                roughness = 0.4f,
                metallic = 0.1f,
                weatheringResistance = 0.7f,
                primaryWeathering = WeatheringType.Physical,
                weatheringRate = 0.05f,
                erosionResistance = 0.8f,
                primaryErosion = ErosionType.Water,
                erosionRate = 0.03f,
                jointSpacing = 2f,
                faultSusceptibility = 0.3f,
                isLayered = false
            };
            
            // 変成岩の特性
            rockProperties[GeologicalFormationGenerator.RockFormationType.Metamorphic] = new RockTypeProperties
            {
                baseHardness = 7f,
                density = 2.9f,
                porosity = 3f,
                permeability = 0.02f,
                baseColor = new Color(0.5f, 0.4f, 0.6f, 1f),
                weatheredColor = new Color(0.4f, 0.3f, 0.4f, 1f),
                colorVariation = 0.25f,
                roughness = 0.3f,
                metallic = 0.2f,
                weatheringResistance = 0.8f,
                primaryWeathering = WeatheringType.Physical,
                weatheringRate = 0.03f,
                erosionResistance = 0.9f,
                primaryErosion = ErosionType.Ice,
                erosionRate = 0.02f,
                jointSpacing = 1.5f,
                faultSusceptibility = 0.2f,
                isLayered = true
            };
        }
        
        /// <summary>
        /// 岩石層に物理特性を適用
        /// </summary>
        /// <param name="layer">対象の地質層</param>
        /// <param name="environmentalConditions">環境条件</param>
        /// <param name="age">経過時間（百万年）</param>
        public void ApplyPhysicalProperties(GeologicalFormationGenerator.GeologicalLayer layer, 
            GeologicalFormationGenerator.GeologicalEnvironment environmentalConditions, float age)
        {
            if (!rockProperties.ContainsKey(layer.formationType))
            {
                VastcoreLogger.Instance.LogWarning("RockPhysics", $"Unknown rock formation type: {layer.formationType}");
                return;
            }
            
            var properties = rockProperties[layer.formationType];
            
            // 基本物理特性の適用
            ApplyBaseProperties(layer, properties);
            
            // 風化効果の適用
            if (enableWeathering)
            {
                ApplyWeatheringEffects(layer, properties, environmentalConditions, age);
            }
            
            // 浸食効果の適用
            if (enableErosion)
            {
                ApplyErosionEffects(layer, properties, environmentalConditions, age);
            }
            
            // 色彩変化の適用
            ApplyColorVariation(layer, properties, age);
            
            VastcoreLogger.Instance.LogDebug("RockPhysics", $"Applied physical properties to {layer.layerName}");
        }
        
        /// <summary>
        /// 基本物理特性の適用
        /// </summary>
        private void ApplyBaseProperties(GeologicalFormationGenerator.GeologicalLayer layer, RockTypeProperties properties)
        {
            // 硬度の設定（環境による変動を含む）
            float hardnessVariation = (float)physicsRandom.NextDouble() * 0.4f - 0.2f; // ±20%の変動
            layer.hardness = Mathf.Clamp(properties.baseHardness + hardnessVariation, 1f, 10f);
            
            // 基本色の設定
            layer.layerColor = properties.baseColor;
        }
        
        /// <summary>
        /// 風化効果の適用
        /// </summary>
        private void ApplyWeatheringEffects(GeologicalFormationGenerator.GeologicalLayer layer, 
            RockTypeProperties properties, GeologicalFormationGenerator.GeologicalEnvironment environment, float age)
        {
            float weatheringIntensity = CalculateWeatheringIntensity(properties, environment, age);
            
            // 硬度の低下
            float hardnessReduction = weatheringIntensity * properties.weatheringRate * weatheringRate;
            layer.hardness = Mathf.Max(layer.hardness - hardnessReduction, 1f);
            
            // 色の変化（風化による変色）
            float colorBlend = Mathf.Clamp01(weatheringIntensity * 0.5f);
            layer.layerColor = Color.Lerp(layer.layerColor, properties.weatheredColor, colorBlend);
            
            // 表面粗さの増加
            float roughnessIncrease = weatheringIntensity * 0.2f;
            // roughnessは将来のマテリアル適用時に使用
            
            VastcoreLogger.Instance.LogDebug("RockPhysics", 
                $"Weathering applied to {layer.layerName}: intensity={weatheringIntensity:F2}, hardness reduction={hardnessReduction:F2}");
        }
        
        /// <summary>
        /// 風化強度の計算
        /// </summary>
        private float CalculateWeatheringIntensity(RockTypeProperties properties, 
            GeologicalFormationGenerator.GeologicalEnvironment environment, float age)
        {
            float baseIntensity = 1f - properties.weatheringResistance;
            
            // 環境条件による調整
            float temperatureEffect = Mathf.Clamp01(environment.temperature / 30f); // 高温ほど風化促進
            float waterEffect = Mathf.Clamp01(environment.waterDepth / 100f);       // 水の存在で風化促進
            float pressureEffect = 1f / Mathf.Max(environment.pressure, 0.1f);     // 低圧で風化促進
            
            // 時間効果（対数的増加）
            float timeEffect = Mathf.Log10(age + 1f) / 3f; // 1000Ma で最大効果
            
            float totalIntensity = baseIntensity * temperatureEffect * waterEffect * pressureEffect * timeEffect;
            
            return Mathf.Clamp01(totalIntensity);
        }
        
        /// <summary>
        /// 浸食効果の適用
        /// </summary>
        private void ApplyErosionEffects(GeologicalFormationGenerator.GeologicalLayer layer, 
            RockTypeProperties properties, GeologicalFormationGenerator.GeologicalEnvironment environment, float age)
        {
            float erosionIntensity = CalculateErosionIntensity(properties, environment, age);
            
            // 層の厚さ減少
            float thicknessReduction = erosionIntensity * properties.erosionRate * erosionRate * age * 0.01f;
            layer.thickness = Mathf.Max(layer.thickness - thicknessReduction, layer.thickness * 0.1f); // 最低10%は残す
            
            // 表面の不規則化
            if (erosionIntensity > 0.5f)
            {
                // 浸食による表面の凹凸生成
                Vector3 surfaceIrregularity = new Vector3(
                    (float)physicsRandom.NextDouble() * erosionIntensity,
                    (float)physicsRandom.NextDouble() * erosionIntensity * 0.5f,
                    (float)physicsRandom.NextDouble() * erosionIntensity
                );
                layer.deformation += surfaceIrregularity;
            }
            
            VastcoreLogger.Instance.LogDebug("RockPhysics", 
                $"Erosion applied to {layer.layerName}: intensity={erosionIntensity:F2}, thickness reduction={thicknessReduction:F2}m");
        }
        
        /// <summary>
        /// 浸食強度の計算
        /// </summary>
        private float CalculateErosionIntensity(RockTypeProperties properties, 
            GeologicalFormationGenerator.GeologicalEnvironment environment, float age)
        {
            float baseIntensity = 1f - properties.erosionResistance;
            
            // 環境条件による調整
            float waterEffect = 1f;
            if (properties.primaryErosion == ErosionType.Water)
            {
                waterEffect = Mathf.Clamp01(environment.waterDepth / 50f + 0.2f); // 水深と基本的な水の存在
            }
            
            float temperatureEffect = 1f;
            if (properties.primaryErosion == ErosionType.Ice)
            {
                temperatureEffect = Mathf.Clamp01((10f - environment.temperature) / 10f); // 低温で氷河浸食
            }
            
            // 時間効果
            float timeEffect = Mathf.Sqrt(age) / 10f; // 平方根的増加
            
            float totalIntensity = baseIntensity * waterEffect * temperatureEffect * timeEffect;
            
            return Mathf.Clamp01(totalIntensity);
        }
        
        /// <summary>
        /// 色彩変化の適用
        /// </summary>
        private void ApplyColorVariation(GeologicalFormationGenerator.GeologicalLayer layer, RockTypeProperties properties, float age)
        {
            // 時間による色の変化
            float ageEffect = Mathf.Clamp01(age / 100f); // 100Ma で最大効果
            
            // ランダムな色の変動
            Color colorVariation = new Color(
                ((float)physicsRandom.NextDouble() - 0.5f) * properties.colorVariation,
                ((float)physicsRandom.NextDouble() - 0.5f) * properties.colorVariation,
                ((float)physicsRandom.NextDouble() - 0.5f) * properties.colorVariation,
                0f
            );
            
            // 年代による色の変化（一般的に暗くなる傾向）
            Color ageColor = layer.layerColor * (1f - ageEffect * 0.2f);
            
            layer.layerColor = ageColor + colorVariation;
            layer.layerColor.a = 1f; // アルファは常に1
        }
        
        /// <summary>
        /// 地層の重なり構造を生成
        /// </summary>
        /// <param name="layers">地質層のリスト</param>
        /// <param name="tectonicActivity">構造運動の強度</param>
        /// <returns>層序構造</returns>
        public StratigraphicSequence GenerateStratigraphicSequence(List<GeologicalFormationGenerator.GeologicalLayer> layers, float tectonicActivity)
        {
            var sequence = new StratigraphicSequence();
            sequence.layers = new List<GeologicalFormationGenerator.GeologicalLayer>(layers);
            
            // 総厚さの計算
            foreach (var layer in layers)
            {
                sequence.totalThickness += layer.thickness;
            }
            
            // 構造運動による変形
            if (tectonicActivity > 0.3f)
            {
                ApplyTectonicDeformation(sequence, tectonicActivity);
            }
            
            // 断層の生成
            if (enableFaultGeneration)
            {
                GenerateFaultStructures(sequence, tectonicActivity);
            }
            
            VastcoreLogger.Instance.LogInfo("RockPhysics", 
                $"Generated stratigraphic sequence: {layers.Count} layers, {sequence.faults.Count} faults, total thickness {sequence.totalThickness:F1}m");
            
            return sequence;
        }
        
        /// <summary>
        /// 構造変形の適用
        /// </summary>
        private void ApplyTectonicDeformation(StratigraphicSequence sequence, float tectonicActivity)
        {
            // 傾斜の設定
            sequence.dip = (float)physicsRandom.NextDouble() * tectonicActivity * 45f; // 最大45度
            sequence.strike = (float)physicsRandom.NextDouble() * 360f;
            
            // 逆転構造の判定
            if (tectonicActivity > 0.7f && physicsRandom.NextDouble() < 0.2f)
            {
                sequence.isOverturned = true;
                sequence.layers.Reverse(); // 層序の逆転
                VastcoreLogger.Instance.LogInfo("RockPhysics", "Overturned structure generated");
            }
            
            // 各層への変形適用
            foreach (var layer in sequence.layers)
            {
                float deformationIntensity = tectonicActivity * (float)physicsRandom.NextDouble();
                layer.deformation += new Vector3(
                    Mathf.Sin(sequence.dip * Mathf.Deg2Rad) * deformationIntensity,
                    0f,
                    Mathf.Cos(sequence.dip * Mathf.Deg2Rad) * deformationIntensity
                );
            }
        }
        
        /// <summary>
        /// 断層構造の生成
        /// </summary>
        private void GenerateFaultStructures(StratigraphicSequence sequence, float tectonicActivity)
        {
            int faultCount = Mathf.RoundToInt(tectonicActivity * maxFaultLayers * faultProbability);
            
            for (int i = 0; i < faultCount; i++)
            {
                var fault = new FaultStructure();
                
                // 断層面の設定
                fault.faultPlane = new Vector3(
                    (float)physicsRandom.NextDouble() - 0.5f,
                    (float)physicsRandom.NextDouble() - 0.5f,
                    (float)physicsRandom.NextDouble() - 0.5f
                ).normalized;
                
                // 変位量の設定
                fault.displacement = (float)physicsRandom.NextDouble() * tectonicActivity * 50f; // 最大50m
                
                // 断層タイプの決定
                fault.faultType = (FaultStructure.FaultType)physicsRandom.Next(4);
                
                // 断層年代
                fault.age = (float)physicsRandom.NextDouble() * 100f; // 0-100Ma
                
                // 活断層の判定
                fault.isActive = fault.age < 2f && physicsRandom.NextDouble() < 0.1f; // 2Ma以内で10%の確率
                
                sequence.faults.Add(fault);
                
                // 影響を受ける層に断層フラグを設定
                int affectedLayerIndex = physicsRandom.Next(sequence.layers.Count);
                sequence.layers[affectedLayerIndex].isFaulted = true;
            }
        }
        
        /// <summary>
        /// 岩石タイプの物理特性を取得
        /// </summary>
        public RockTypeProperties GetRockProperties(GeologicalFormationGenerator.RockFormationType rockType)
        {
            return rockProperties.ContainsKey(rockType) ? rockProperties[rockType] : new RockTypeProperties();
        }
        
        /// <summary>
        /// 適切なマテリアルを取得
        /// </summary>
        public Material GetRockMaterial(GeologicalFormationGenerator.RockFormationType rockType, float weatheringLevel = 0f)
        {
            Material[] materials = rockType switch
            {
                GeologicalFormationGenerator.RockFormationType.Sedimentary => sedimentaryMaterials,
                GeologicalFormationGenerator.RockFormationType.Igneous => igneousMaterials,
                GeologicalFormationGenerator.RockFormationType.Metamorphic => metamorphicMaterials,
                _ => sedimentaryMaterials
            };
            
            if (materials == null || materials.Length == 0)
            {
                VastcoreLogger.Instance.LogWarning("RockPhysics", $"No materials available for rock type: {rockType}");
                return null;
            }
            
            // 風化レベルに応じたマテリアル選択
            int materialIndex = Mathf.RoundToInt(weatheringLevel * (materials.Length - 1));
            materialIndex = Mathf.Clamp(materialIndex, 0, materials.Length - 1);
            
            return materials[materialIndex];
        }
        
        /// <summary>
        /// 岩石の風化パターンを取得
        /// </summary>
        public WeatheringType GetWeatheringPattern(GeologicalFormationGenerator.RockFormationType rockType)
        {
            if (rockProperties.ContainsKey(rockType))
            {
                return rockProperties[rockType].primaryWeathering;
            }
            return WeatheringType.Chemical;
        }
        
        /// <summary>
        /// 岩石の浸食パターンを取得
        /// </summary>
        public ErosionType GetErosionPattern(GeologicalFormationGenerator.RockFormationType rockType)
        {
            if (rockProperties.ContainsKey(rockType))
            {
                return rockProperties[rockType].primaryErosion;
            }
            return ErosionType.Water;
        }
    }
}