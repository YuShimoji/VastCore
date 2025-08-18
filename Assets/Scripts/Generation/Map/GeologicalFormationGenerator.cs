using UnityEngine;
using System.Collections.Generic;
using System;
using Vastcore.Core;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// 地質学的形成過程をシミュレーションして岩石層構造を生成するクラス
    /// 堆積、火成、変成の各過程を地質時間スケールで再現
    /// </summary>
    public class GeologicalFormationGenerator : MonoBehaviour
    {
        [Header("地質学的設定")]
        [SerializeField] private GeologicalTimeScale timeScale = GeologicalTimeScale.Mesozoic;
        [SerializeField] private float simulationSpeed = 1.0f;
        [SerializeField] private int maxFormationLayers = 10;
        
        [Header("形成過程設定")]
        [SerializeField] private bool enableSedimentaryFormation = true;
        [SerializeField] private bool enableIgneousFormation = true;
        [SerializeField] private bool enableMetamorphicFormation = true;
        
        [Header("環境条件")]
        [SerializeField] private float seaLevel = 0f;
        [SerializeField] private float tectonicActivity = 0.5f;
        [SerializeField] private float volcanicActivity = 0.3f;
        [SerializeField] private float erosionRate = 0.1f;
        
        private List<GeologicalLayer> formationLayers;
        private GeologicalEnvironment currentEnvironment;
        private System.Random geologicalRandom;
        
        /// <summary>
        /// 地質時代の分類
        /// </summary>
        public enum GeologicalTimeScale
        {
            Precambrian,    // 先カンブリア時代
            Paleozoic,      // 古生代
            Mesozoic,       // 中生代
            Cenozoic        // 新生代
        }
        
        /// <summary>
        /// 岩石形成タイプ
        /// </summary>
        public enum RockFormationType
        {
            Sedimentary,    // 堆積岩
            Igneous,        // 火成岩
            Metamorphic     // 変成岩
        }
        
        /// <summary>
        /// 地質学的環境条件
        /// </summary>
        [System.Serializable]
        public class GeologicalEnvironment
        {
            public float temperature;           // 温度
            public float pressure;              // 圧力
            public float waterDepth;            // 水深
            public float sedimentSupply;        // 堆積物供給量
            public float magmaActivity;         // マグマ活動
            public float metamorphicGrade;      // 変成度
            
            public GeologicalEnvironment()
            {
                temperature = 15f;
                pressure = 1f;
                waterDepth = 0f;
                sedimentSupply = 0.5f;
                magmaActivity = 0.1f;
                metamorphicGrade = 0f;
            }
        }
        
        /// <summary>
        /// 地質学的層構造
        /// </summary>
        [System.Serializable]
        public class GeologicalLayer
        {
            public RockFormationType formationType;
            public string layerName;
            public float thickness;
            public float age;                   // 地質年代（百万年）
            public Color layerColor;
            public float hardness;
            public Vector3 deformation;         // 構造変形
            public bool isFaulted;              // 断層の有無
            
            public GeologicalLayer(RockFormationType type, float thick, float geologicalAge)
            {
                formationType = type;
                thickness = thick;
                age = geologicalAge;
                layerName = GenerateLayerName(type, geologicalAge);
                layerColor = GenerateLayerColor(type);
                hardness = GenerateHardness(type);
                deformation = Vector3.zero;
                isFaulted = false;
            }
            
            private string GenerateLayerName(RockFormationType type, float age)
            {
                string baseName = type switch
                {
                    RockFormationType.Sedimentary => "堆積層",
                    RockFormationType.Igneous => "火成層",
                    RockFormationType.Metamorphic => "変成層",
                    _ => "未分類層"
                };
                return $"{baseName}_{age:F0}Ma";
            }
            
            private Color GenerateLayerColor(RockFormationType type)
            {
                return type switch
                {
                    RockFormationType.Sedimentary => new Color(0.8f, 0.7f, 0.5f, 1f),    // 砂岩色
                    RockFormationType.Igneous => new Color(0.3f, 0.3f, 0.3f, 1f),        // 玄武岩色
                    RockFormationType.Metamorphic => new Color(0.5f, 0.4f, 0.6f, 1f),    // 片麻岩色
                    _ => Color.gray
                };
            }
            
            private float GenerateHardness(RockFormationType type)
            {
                return type switch
                {
                    RockFormationType.Sedimentary => UnityEngine.Random.Range(2f, 6f),    // モース硬度2-6
                    RockFormationType.Igneous => UnityEngine.Random.Range(5f, 8f),        // モース硬度5-8
                    RockFormationType.Metamorphic => UnityEngine.Random.Range(4f, 9f),    // モース硬度4-9
                    _ => 5f
                };
            }
        }
        
        void Start()
        {
            Initialize();
        }
        
        /// <summary>
        /// 地質学的形成システムの初期化
        /// </summary>
        public void Initialize()
        {
            formationLayers = new List<GeologicalLayer>();
            currentEnvironment = new GeologicalEnvironment();
            geologicalRandom = new System.Random();
            
            SetupGeologicalTimeScale();
            VastcoreLogger.Instance.LogInfo("GeologicalFormation", "GeologicalFormationGenerator initialized");
        }
        
        /// <summary>
        /// 地質時代に応じた環境設定
        /// </summary>
        private void SetupGeologicalTimeScale()
        {
            switch (timeScale)
            {
                case GeologicalTimeScale.Precambrian:
                    currentEnvironment.temperature = 25f;
                    currentEnvironment.magmaActivity = 0.8f;
                    currentEnvironment.metamorphicGrade = 0.6f;
                    break;
                    
                case GeologicalTimeScale.Paleozoic:
                    currentEnvironment.temperature = 20f;
                    currentEnvironment.sedimentSupply = 0.7f;
                    currentEnvironment.waterDepth = 50f;
                    break;
                    
                case GeologicalTimeScale.Mesozoic:
                    currentEnvironment.temperature = 22f;
                    currentEnvironment.sedimentSupply = 0.6f;
                    currentEnvironment.magmaActivity = 0.4f;
                    break;
                    
                case GeologicalTimeScale.Cenozoic:
                    currentEnvironment.temperature = 15f;
                    currentEnvironment.sedimentSupply = 0.5f;
                    currentEnvironment.metamorphicGrade = 0.2f;
                    break;
            }
        }
        
        /// <summary>
        /// 地質学的形成過程のシミュレーション実行
        /// </summary>
        /// <param name="position">形成位置</param>
        /// <param name="simulationTime">シミュレーション時間（百万年）</param>
        /// <returns>生成された地質構造</returns>
        public GeologicalFormation SimulateFormationProcess(Vector3 position, float simulationTime)
        {
            var formation = new GeologicalFormation(position);
            float currentTime = 0f;
            
            while (currentTime < simulationTime && formationLayers.Count < maxFormationLayers)
            {
                // 環境条件の更新
                UpdateEnvironmentalConditions(currentTime);
                
                // 形成過程の決定
                RockFormationType formationType = DetermineFormationType();
                
                // 層の形成
                GeologicalLayer newLayer = FormLayer(formationType, currentTime);
                
                if (newLayer != null)
                {
                    formationLayers.Add(newLayer);
                    formation.AddLayer(newLayer);
                }
                
                // 時間の進行
                currentTime += GetTimeStep(formationType);
            }
            
            // 構造変形の適用
            ApplyStructuralDeformation(formation);
            
            VastcoreLogger.Instance.LogInfo("GeologicalFormation", $"Geological formation simulated: {formationLayers.Count} layers over {simulationTime}Ma");
            return formation;
        }
        
        /// <summary>
        /// 環境条件の時間的変化をシミュレート
        /// </summary>
        private void UpdateEnvironmentalConditions(float currentTime)
        {
            // 海水準変動
            float seaLevelVariation = Mathf.Sin(currentTime * 0.1f) * 20f;
            currentEnvironment.waterDepth = Mathf.Max(0f, seaLevel + seaLevelVariation);
            
            // 構造運動による圧力変化
            currentEnvironment.pressure = 1f + tectonicActivity * Mathf.Sin(currentTime * 0.05f);
            
            // 火山活動の周期的変化
            currentEnvironment.magmaActivity = volcanicActivity * (1f + 0.5f * Mathf.Sin(currentTime * 0.02f));
            
            // 変成作用の累積
            currentEnvironment.metamorphicGrade += tectonicActivity * 0.001f;
        }
        
        /// <summary>
        /// 環境条件に基づいて形成される岩石タイプを決定
        /// </summary>
        private RockFormationType DetermineFormationType()
        {
            float sedimentaryProbability = 0.4f;
            float igneousProbability = 0.3f;
            float metamorphicProbability = 0.3f;
            
            // 環境条件による確率調整
            if (currentEnvironment.waterDepth > 10f)
            {
                sedimentaryProbability += 0.3f;
            }
            
            if (currentEnvironment.magmaActivity > 0.5f)
            {
                igneousProbability += 0.4f;
            }
            
            if (currentEnvironment.metamorphicGrade > 0.3f)
            {
                metamorphicProbability += 0.3f;
            }
            
            // 確率の正規化
            float total = sedimentaryProbability + igneousProbability + metamorphicProbability;
            sedimentaryProbability /= total;
            igneousProbability /= total;
            
            float random = (float)geologicalRandom.NextDouble();
            
            if (random < sedimentaryProbability && enableSedimentaryFormation)
                return RockFormationType.Sedimentary;
            else if (random < sedimentaryProbability + igneousProbability && enableIgneousFormation)
                return RockFormationType.Igneous;
            else if (enableMetamorphicFormation)
                return RockFormationType.Metamorphic;
            else
                return RockFormationType.Sedimentary; // フォールバック
        }
        
        /// <summary>
        /// 指定されたタイプの地質層を形成
        /// </summary>
        private GeologicalLayer FormLayer(RockFormationType type, float currentTime)
        {
            float thickness = CalculateLayerThickness(type);
            var layer = new GeologicalLayer(type, thickness, currentTime);
            
            // タイプ別の特殊処理
            switch (type)
            {
                case RockFormationType.Sedimentary:
                    ProcessSedimentaryFormation(layer);
                    break;
                case RockFormationType.Igneous:
                    ProcessIgneousFormation(layer);
                    break;
                case RockFormationType.Metamorphic:
                    ProcessMetamorphicFormation(layer);
                    break;
            }
            
            return layer;
        }
        
        /// <summary>
        /// 堆積岩形成過程のシミュレーション
        /// </summary>
        private void ProcessSedimentaryFormation(GeologicalLayer layer)
        {
            // 堆積環境による特性調整
            if (currentEnvironment.waterDepth > 100f)
            {
                // 深海堆積
                layer.layerColor = new Color(0.4f, 0.4f, 0.5f, 1f); // 深海泥岩
                layer.hardness *= 0.8f;
            }
            else if (currentEnvironment.waterDepth > 10f)
            {
                // 浅海堆積
                layer.layerColor = new Color(0.7f, 0.6f, 0.4f, 1f); // 石灰岩
                layer.hardness *= 1.2f;
            }
            else
            {
                // 陸上堆積
                layer.layerColor = new Color(0.8f, 0.5f, 0.3f, 1f); // 砂岩
            }
            
            // 堆積物供給量による厚さ調整
            layer.thickness *= currentEnvironment.sedimentSupply;
        }
        
        /// <summary>
        /// 火成岩形成過程のシミュレーション
        /// </summary>
        private void ProcessIgneousFormation(GeologicalLayer layer)
        {
            // マグマ活動による特性調整
            if (currentEnvironment.magmaActivity > 0.7f)
            {
                // 火山岩
                layer.layerColor = new Color(0.2f, 0.2f, 0.2f, 1f); // 玄武岩
                layer.hardness *= 1.3f;
                layer.thickness *= 0.5f; // 溶岩流は薄い
            }
            else
            {
                // 深成岩
                layer.layerColor = new Color(0.6f, 0.6f, 0.6f, 1f); // 花崗岩
                layer.hardness *= 1.5f;
                layer.thickness *= 2f; // 深成岩体は厚い
            }
        }
        
        /// <summary>
        /// 変成岩形成過程のシミュレーション
        /// </summary>
        private void ProcessMetamorphicFormation(GeologicalLayer layer)
        {
            // 既存の層を変成
            if (formationLayers.Count > 0)
            {
                var parentLayer = formationLayers[formationLayers.Count - 1];
                
                // 原岩による変成岩の特性決定
                switch (parentLayer.formationType)
                {
                    case RockFormationType.Sedimentary:
                        layer.layerColor = new Color(0.5f, 0.4f, 0.3f, 1f); // 片麻岩
                        break;
                    case RockFormationType.Igneous:
                        layer.layerColor = new Color(0.3f, 0.5f, 0.4f, 1f); // 角閃岩
                        break;
                }
                
                // 変成度による硬度増加
                layer.hardness = parentLayer.hardness * (1f + currentEnvironment.metamorphicGrade);
            }
            
            // 変成作用による構造変形
            float deformationIntensity = currentEnvironment.metamorphicGrade * tectonicActivity;
            layer.deformation = new Vector3(
                (float)geologicalRandom.NextDouble() * deformationIntensity,
                (float)geologicalRandom.NextDouble() * deformationIntensity * 0.5f,
                (float)geologicalRandom.NextDouble() * deformationIntensity
            );
        }
        
        /// <summary>
        /// 層の厚さを計算
        /// </summary>
        private float CalculateLayerThickness(RockFormationType type)
        {
            float baseThickness = type switch
            {
                RockFormationType.Sedimentary => UnityEngine.Random.Range(5f, 50f),
                RockFormationType.Igneous => UnityEngine.Random.Range(10f, 100f),
                RockFormationType.Metamorphic => UnityEngine.Random.Range(20f, 80f),
                _ => 20f
            };
            
            return baseThickness * simulationSpeed;
        }
        
        /// <summary>
        /// 形成タイプに応じた時間ステップを取得
        /// </summary>
        private float GetTimeStep(RockFormationType type)
        {
            return type switch
            {
                RockFormationType.Sedimentary => UnityEngine.Random.Range(1f, 10f),    // 1-10百万年
                RockFormationType.Igneous => UnityEngine.Random.Range(0.1f, 5f),       // 0.1-5百万年
                RockFormationType.Metamorphic => UnityEngine.Random.Range(5f, 50f),    // 5-50百万年
                _ => 5f
            };
        }
        
        /// <summary>
        /// 構造変形の適用
        /// </summary>
        private void ApplyStructuralDeformation(GeologicalFormation formation)
        {
            if (tectonicActivity > 0.5f)
            {
                // 断層の生成
                int faultCount = Mathf.RoundToInt(tectonicActivity * 3f);
                for (int i = 0; i < faultCount; i++)
                {
                    int layerIndex = geologicalRandom.Next(formationLayers.Count);
                    formationLayers[layerIndex].isFaulted = true;
                }
                
                // 褶曲構造の生成
                ApplyFoldingDeformation(formation);
            }
        }
        
        /// <summary>
        /// 褶曲変形の適用
        /// </summary>
        private void ApplyFoldingDeformation(GeologicalFormation formation)
        {
            float foldIntensity = tectonicActivity * 0.5f;
            
            foreach (var layer in formationLayers)
            {
                layer.deformation += new Vector3(
                    Mathf.Sin(layer.age * 0.1f) * foldIntensity,
                    0f,
                    Mathf.Cos(layer.age * 0.1f) * foldIntensity
                );
            }
        }
        
        /// <summary>
        /// 現在の地質環境を取得
        /// </summary>
        public GeologicalEnvironment GetCurrentEnvironment()
        {
            return currentEnvironment;
        }
        
        /// <summary>
        /// 形成された地質層リストを取得
        /// </summary>
        public List<GeologicalLayer> GetFormationLayers()
        {
            return new List<GeologicalLayer>(formationLayers);
        }
    }
    
    /// <summary>
    /// 地質学的形成構造を表すクラス
    /// </summary>
    public class GeologicalFormation
    {
        public Vector3 position;
        public List<GeologicalFormationGenerator.GeologicalLayer> layers;
        public float totalThickness;
        public float formationAge;
        
        public GeologicalFormation(Vector3 pos)
        {
            position = pos;
            layers = new List<GeologicalFormationGenerator.GeologicalLayer>();
            totalThickness = 0f;
            formationAge = 0f;
        }
        
        public void AddLayer(GeologicalFormationGenerator.GeologicalLayer layer)
        {
            layers.Add(layer);
            totalThickness += layer.thickness;
            formationAge = Mathf.Max(formationAge, layer.age);
        }
        
        public GeologicalFormationGenerator.GeologicalLayer GetTopLayer()
        {
            return layers.Count > 0 ? layers[layers.Count - 1] : null;
        }
        
        public GeologicalFormationGenerator.GeologicalLayer GetBottomLayer()
        {
            return layers.Count > 0 ? layers[0] : null;
        }
    }
}