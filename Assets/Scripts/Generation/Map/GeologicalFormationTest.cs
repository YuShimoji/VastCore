using UnityEngine;
using System.Collections.Generic;
using Vastcore.Core;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// 地質学的岩石層生成システムの統合テストクラス
    /// GeologicalFormationGeneratorとRockLayerPhysicalPropertiesの動作を検証
    /// </summary>
    public class GeologicalFormationTest : MonoBehaviour
    {
        [Header("テスト設定")]
        [SerializeField] private bool runTestOnStart = true;
        [SerializeField] private bool enableDetailedLogging = true;
        [SerializeField] private int testIterations = 5;
        
        [Header("シミュレーション設定")]
        [SerializeField] private float testSimulationTime = 100f; // 100Ma
        [SerializeField] private Vector3 testPosition = Vector3.zero;
        [SerializeField] private float testTectonicActivity = 0.5f;
        
        [Header("テスト結果表示")]
        [SerializeField] private bool visualizeResults = true;
        [SerializeField] private GameObject layerVisualizationPrefab;
        
        private GeologicalFormationGenerator formationGenerator;
        private RockLayerPhysicalProperties physicalProperties;
        private List<GeologicalFormation> testFormations;
        private List<GameObject> visualizationObjects;
        
        void Start()
        {
            if (runTestOnStart)
            {
                RunComprehensiveTest();
            }
        }
        
        /// <summary>
        /// 包括的なテストの実行
        /// </summary>
        public void RunComprehensiveTest()
        {
            VastcoreLogger.Instance.LogInfo("GeologicalTest", "Starting comprehensive geological formation test");
            
            InitializeTestComponents();
            
            // 基本機能テスト
            TestBasicFormationGeneration();
            
            // 物理特性テスト
            TestPhysicalProperties();
            
            // 地層構造テスト
            TestStratigraphicSequences();
            
            // 風化・浸食テスト
            TestWeatheringAndErosion();
            
            // 断層構造テスト
            TestFaultGeneration();
            
            // パフォーマンステスト
            TestPerformance();
            
            // 結果の可視化
            if (visualizeResults)
            {
                VisualizeTestResults();
            }
            
            VastcoreLogger.Instance.LogInfo("GeologicalTest", "Comprehensive geological formation test completed");
        }
        
        /// <summary>
        /// テストコンポーネントの初期化
        /// </summary>
        private void InitializeTestComponents()
        {
            // GeologicalFormationGeneratorの取得または作成
            formationGenerator = GetComponent<GeologicalFormationGenerator>();
            if (formationGenerator == null)
            {
                formationGenerator = gameObject.AddComponent<GeologicalFormationGenerator>();
            }
            
            // RockLayerPhysicalPropertiesの取得または作成
            physicalProperties = GetComponent<RockLayerPhysicalProperties>();
            if (physicalProperties == null)
            {
                physicalProperties = gameObject.AddComponent<RockLayerPhysicalProperties>();
            }
            
            // 初期化
            formationGenerator.Initialize();
            physicalProperties.Initialize();
            
            testFormations = new List<GeologicalFormation>();
            visualizationObjects = new List<GameObject>();
            
            VastcoreLogger.Instance.LogInfo("GeologicalTest", "Test components initialized");
        }
        
        /// <summary>
        /// 基本的な地質形成テスト
        /// </summary>
        private void TestBasicFormationGeneration()
        {
            VastcoreLogger.Instance.LogInfo("GeologicalTest", "Testing basic formation generation");
            
            for (int i = 0; i < testIterations; i++)
            {
                Vector3 position = testPosition + new Vector3(i * 100f, 0f, 0f);
                GeologicalFormation formation = formationGenerator.SimulateFormationProcess(position, testSimulationTime);
                
                if (formation != null && formation.layers.Count > 0)
                {
                    testFormations.Add(formation);
                    
                    if (enableDetailedLogging)
                    {
                        VastcoreLogger.Instance.LogInfo("GeologicalTest", 
                            $"Formation {i}: {formation.layers.Count} layers, total thickness {formation.totalThickness:F1}m, age {formation.formationAge:F1}Ma");
                        
                        foreach (var layer in formation.layers)
                        {
                            VastcoreLogger.Instance.LogDebug("GeologicalTest", 
                                $"  Layer: {layer.layerName}, Type: {layer.formationType}, Thickness: {layer.thickness:F1}m, Hardness: {layer.hardness:F1}");
                        }
                    }
                }
                else
                {
                    VastcoreLogger.Instance.LogError("GeologicalTest", $"Failed to generate formation {i}");
                }
            }
            
            VastcoreLogger.Instance.LogInfo("GeologicalTest", $"Basic formation generation test completed: {testFormations.Count}/{testIterations} successful");
        }
        
        /// <summary>
        /// 物理特性テスト
        /// </summary>
        private void TestPhysicalProperties()
        {
            VastcoreLogger.Instance.LogInfo("GeologicalTest", "Testing physical properties application");
            
            var environment = formationGenerator.GetCurrentEnvironment();
            int propertiesApplied = 0;
            
            foreach (var formation in testFormations)
            {
                foreach (var layer in formation.layers)
                {
                    Color originalColor = layer.layerColor;
                    float originalHardness = layer.hardness;
                    
                    physicalProperties.ApplyPhysicalProperties(layer, environment, layer.age);
                    
                    // 変化の検証
                    bool colorChanged = !Mathf.Approximately(originalColor.r, layer.layerColor.r) ||
                                       !Mathf.Approximately(originalColor.g, layer.layerColor.g) ||
                                       !Mathf.Approximately(originalColor.b, layer.layerColor.b);
                    
                    bool hardnessChanged = !Mathf.Approximately(originalHardness, layer.hardness);
                    
                    if (colorChanged || hardnessChanged)
                    {
                        propertiesApplied++;
                        
                        if (enableDetailedLogging)
                        {
                            VastcoreLogger.Instance.LogDebug("GeologicalTest", 
                                $"Properties applied to {layer.layerName}: Color changed={colorChanged}, Hardness {originalHardness:F1}→{layer.hardness:F1}");
                        }
                    }
                }
            }
            
            VastcoreLogger.Instance.LogInfo("GeologicalTest", $"Physical properties test completed: {propertiesApplied} layers modified");
        }
        
        /// <summary>
        /// 地層構造テスト
        /// </summary>
        private void TestStratigraphicSequences()
        {
            VastcoreLogger.Instance.LogInfo("GeologicalTest", "Testing stratigraphic sequence generation");
            
            int sequencesGenerated = 0;
            
            foreach (var formation in testFormations)
            {
                var sequence = physicalProperties.GenerateStratigraphicSequence(formation.layers, testTectonicActivity);
                
                if (sequence != null)
                {
                    sequencesGenerated++;
                    
                    if (enableDetailedLogging)
                    {
                        VastcoreLogger.Instance.LogInfo("GeologicalTest", 
                            $"Sequence: {sequence.layers.Count} layers, {sequence.faults.Count} faults, " +
                            $"dip={sequence.dip:F1}°, overturned={sequence.isOverturned}");
                    }
                }
            }
            
            VastcoreLogger.Instance.LogInfo("GeologicalTest", $"Stratigraphic sequence test completed: {sequencesGenerated} sequences generated");
        }
        
        /// <summary>
        /// 風化・浸食テスト
        /// </summary>
        private void TestWeatheringAndErosion()
        {
            VastcoreLogger.Instance.LogInfo("GeologicalTest", "Testing weathering and erosion effects");
            
            var testEnvironments = new[]
            {
                new GeologicalFormationGenerator.GeologicalEnvironment { temperature = 30f, waterDepth = 100f }, // 高温多湿
                new GeologicalFormationGenerator.GeologicalEnvironment { temperature = 5f, waterDepth = 0f },    // 寒冷乾燥
                new GeologicalFormationGenerator.GeologicalEnvironment { temperature = 20f, waterDepth = 50f }   // 温帯
            };
            
            int weatheringTests = 0;
            
            foreach (var environment in testEnvironments)
            {
                foreach (var formation in testFormations)
                {
                    foreach (var layer in formation.layers)
                    {
                        float originalThickness = layer.thickness;
                        float originalHardness = layer.hardness;
                        
                        // 長期間の風化・浸食をシミュレート
                        physicalProperties.ApplyPhysicalProperties(layer, environment, layer.age + 50f);
                        
                        float thicknessChange = originalThickness - layer.thickness;
                        float hardnessChange = originalHardness - layer.hardness;
                        
                        if (thicknessChange > 0.1f || hardnessChange > 0.1f)
                        {
                            weatheringTests++;
                            
                            if (enableDetailedLogging)
                            {
                                VastcoreLogger.Instance.LogDebug("GeologicalTest", 
                                    $"Weathering effect on {layer.layerName}: thickness -{thicknessChange:F1}m, hardness -{hardnessChange:F1}");
                            }
                        }
                    }
                }
            }
            
            VastcoreLogger.Instance.LogInfo("GeologicalTest", $"Weathering and erosion test completed: {weatheringTests} effects observed");
        }
        
        /// <summary>
        /// 断層構造テスト
        /// </summary>
        private void TestFaultGeneration()
        {
            VastcoreLogger.Instance.LogInfo("GeologicalTest", "Testing fault structure generation");
            
            int totalFaults = 0;
            int activeFaults = 0;
            
            foreach (var formation in testFormations)
            {
                var sequence = physicalProperties.GenerateStratigraphicSequence(formation.layers, 0.8f); // 高い構造活動
                
                totalFaults += sequence.faults.Count;
                
                foreach (var fault in sequence.faults)
                {
                    if (fault.isActive)
                    {
                        activeFaults++;
                    }
                    
                    if (enableDetailedLogging)
                    {
                        VastcoreLogger.Instance.LogDebug("GeologicalTest", 
                            $"Fault: type={fault.faultType}, displacement={fault.displacement:F1}m, age={fault.age:F1}Ma, active={fault.isActive}");
                    }
                }
            }
            
            VastcoreLogger.Instance.LogInfo("GeologicalTest", $"Fault generation test completed: {totalFaults} faults generated, {activeFaults} active");
        }
        
        /// <summary>
        /// パフォーマンステスト
        /// </summary>
        private void TestPerformance()
        {
            VastcoreLogger.Instance.LogInfo("GeologicalTest", "Testing performance");
            
            float startTime = Time.realtimeSinceStartup;
            
            // 大量の地質形成をテスト
            for (int i = 0; i < 10; i++)
            {
                Vector3 position = new Vector3(i * 200f, 0f, 0f);
                var formation = formationGenerator.SimulateFormationProcess(position, 200f);
                
                if (formation != null)
                {
                    var environment = formationGenerator.GetCurrentEnvironment();
                    foreach (var layer in formation.layers)
                    {
                        physicalProperties.ApplyPhysicalProperties(layer, environment, layer.age);
                    }
                    
                    physicalProperties.GenerateStratigraphicSequence(formation.layers, 0.6f);
                }
            }
            
            float endTime = Time.realtimeSinceStartup;
            float totalTime = endTime - startTime;
            
            VastcoreLogger.Instance.LogInfo("GeologicalTest", $"Performance test completed: 10 formations in {totalTime:F2} seconds ({totalTime/10f:F3}s per formation)");
        }
        
        /// <summary>
        /// テスト結果の可視化
        /// </summary>
        private void VisualizeTestResults()
        {
            VastcoreLogger.Instance.LogInfo("GeologicalTest", "Visualizing test results");
            
            if (layerVisualizationPrefab == null)
            {
                VastcoreLogger.Instance.LogWarning("GeologicalTest", "No visualization prefab assigned");
                return;
            }
            
            // 既存の可視化オブジェクトをクリア
            foreach (var obj in visualizationObjects)
            {
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }
            visualizationObjects.Clear();
            
            // 各地質形成を可視化
            for (int i = 0; i < testFormations.Count; i++)
            {
                var formation = testFormations[i];
                Vector3 basePosition = formation.position;
                float currentHeight = 0f;
                
                for (int j = 0; j < formation.layers.Count; j++)
                {
                    var layer = formation.layers[j];
                    
                    GameObject layerObj = Instantiate(layerVisualizationPrefab);
                    layerObj.name = $"Formation_{i}_Layer_{j}_{layer.layerName}";
                    
                    // 位置設定
                    Vector3 layerPosition = basePosition + new Vector3(0f, currentHeight + layer.thickness * 0.5f, 0f);
                    layerObj.transform.position = layerPosition;
                    
                    // スケール設定
                    layerObj.transform.localScale = new Vector3(50f, layer.thickness, 50f);
                    
                    // 色設定
                    var renderer = layerObj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = layer.layerColor;
                    }
                    
                    // 変形適用
                    if (layer.deformation.magnitude > 0.1f)
                    {
                        layerObj.transform.rotation = Quaternion.Euler(layer.deformation * 10f);
                    }
                    
                    visualizationObjects.Add(layerObj);
                    currentHeight += layer.thickness;
                }
            }
            
            VastcoreLogger.Instance.LogInfo("GeologicalTest", $"Visualization completed: {visualizationObjects.Count} layer objects created");
        }
        
        /// <summary>
        /// 特定の岩石タイプのテスト
        /// </summary>
        public void TestSpecificRockType(GeologicalFormationGenerator.RockFormationType rockType)
        {
            VastcoreLogger.Instance.LogInfo("GeologicalTest", $"Testing specific rock type: {rockType}");
            
            var properties = physicalProperties.GetRockProperties(rockType);
            var weatheringPattern = physicalProperties.GetWeatheringPattern(rockType);
            var erosionPattern = physicalProperties.GetErosionPattern(rockType);
            
            VastcoreLogger.Instance.LogInfo("GeologicalTest", 
                $"Rock type {rockType}: hardness={properties.baseHardness:F1}, density={properties.density:F1}, " +
                $"weathering={weatheringPattern}, erosion={erosionPattern}");
        }
        
        /// <summary>
        /// テスト結果のクリア
        /// </summary>
        public void ClearTestResults()
        {
            foreach (var obj in visualizationObjects)
            {
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }
            
            visualizationObjects.Clear();
            testFormations.Clear();
            
            VastcoreLogger.Instance.LogInfo("GeologicalTest", "Test results cleared");
        }
        
        void OnDrawGizmos()
        {
            if (testFormations == null) return;
            
            // 地質形成の位置を可視化
            Gizmos.color = Color.yellow;
            foreach (var formation in testFormations)
            {
                Gizmos.DrawWireCube(formation.position, Vector3.one * 10f);
                
                // 層の厚さを表示
                float height = 0f;
                foreach (var layer in formation.layers)
                {
                    Gizmos.color = layer.layerColor;
                    Vector3 layerCenter = formation.position + new Vector3(0f, height + layer.thickness * 0.5f, 0f);
                    Gizmos.DrawCube(layerCenter, new Vector3(8f, layer.thickness, 8f));
                    height += layer.thickness;
                }
            }
        }
    }
}