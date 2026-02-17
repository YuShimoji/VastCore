using UnityEngine;
using UnityEngine.ProBuilder;

namespace Vastcore.Generation
{
    /// <summary>
    /// 結晶構造生成システムのテストクラス
    /// </summary>
    [System.Obsolete("Experimental crystal test harness. Not used in core terrain pipeline.")]
    public class CrystalStructureGeneratorTest : MonoBehaviour
    {
        [Header("テスト設定")]
        public bool runTestOnStart = true;
        public bool testAllCrystalSystems = true;
        public bool testGrowthSimulation = true;
        public Vector3 testScale = new Vector3(100f, 100f, 100f);
        
        [Header("生成設定")]
        public CrystalStructureGenerator.CrystalSystem specificCrystalSystem = CrystalStructureGenerator.CrystalSystem.Cubic;
        public int numberOfTestCrystals = 6;
        public float spacingBetweenCrystals = 200f;
        
        [Header("品質テスト")]
        public bool enableQualityEvaluation = true;
        public bool logDetailedResults = true;
        
        private void Start()
        {
            if (runTestOnStart)
            {
                RunCrystalGenerationTests();
            }
        }

        /// <summary>
        /// 結晶生成テストを実行
        /// </summary>
        [ContextMenu("Run Crystal Generation Tests")]
        public void RunCrystalGenerationTests()
        {
            Debug.Log("=== Starting Crystal Structure Generation Tests ===");
            
            if (testAllCrystalSystems)
            {
                TestAllCrystalSystems();
            }
            else
            {
                TestSpecificCrystalSystem();
            }
            
            if (testGrowthSimulation)
            {
                TestGrowthSimulation();
            }
            
            Debug.Log("=== Crystal Structure Generation Tests Completed ===");
        }

        /// <summary>
        /// 全結晶系をテスト
        /// </summary>
        private void TestAllCrystalSystems()
        {
            Debug.Log("Testing all crystal systems...");
            
            var crystalSystems = System.Enum.GetValues(typeof(CrystalStructureGenerator.CrystalSystem));
            
            for (int i = 0; i < crystalSystems.Length; i++)
            {
                var crystalSystem = (CrystalStructureGenerator.CrystalSystem)crystalSystems.GetValue(i);
                
                Vector3 position = new Vector3(
                    (i % 3) * spacingBetweenCrystals,
                    0,
                    (i / 3) * spacingBetweenCrystals
                );
                
                TestCrystalSystem(crystalSystem, position);
            }
        }

        /// <summary>
        /// 特定の結晶系をテスト
        /// </summary>
        private void TestSpecificCrystalSystem()
        {
            Debug.Log($"Testing specific crystal system: {specificCrystalSystem}");
            TestCrystalSystem(specificCrystalSystem, Vector3.zero);
        }

        /// <summary>
        /// 結晶系をテスト
        /// </summary>
        private void TestCrystalSystem(CrystalStructureGenerator.CrystalSystem crystalSystem, Vector3 position)
        {
            try
            {
                Debug.Log($"Generating {crystalSystem} crystal at position {position}");
                
                // 結晶パラメータを設定
                var parameters = CrystalStructureGenerator.CrystalGenerationParams.Default(crystalSystem);
                parameters.overallSize = testScale.magnitude;
                
                // 結晶を生成
                var crystal = CrystalStructureGenerator.GenerateCrystalStructure(parameters);
                
                if (crystal != null)
                {
                    // GameObjectを設定
                    crystal.transform.position = position;
                    crystal.name = $"TestCrystal_{crystalSystem}";
                    
                    // 親オブジェクトに設定
                    crystal.transform.SetParent(this.transform);
                    
                    // マテリアルを設定
                    SetupTestMaterial(crystal.gameObject, crystalSystem);
                    
                    // 品質評価
                    if (enableQualityEvaluation)
                    {
                        float quality = CrystalStructureGenerator.EvaluateCrystalQuality(crystal, parameters);
                        Debug.Log($"{crystalSystem} crystal quality: {quality:F2}");
                    }
                    
                    Debug.Log($"Successfully generated {crystalSystem} crystal");
                }
                else
                {
                    Debug.LogError($"Failed to generate {crystalSystem} crystal");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error testing {crystalSystem}: {e.Message}");
            }
        }

        /// <summary>
        /// 成長シミュレーションをテスト
        /// </summary>
        private void TestGrowthSimulation()
        {
            Debug.Log("Testing crystal growth simulation...");
            
            try
            {
                // 成長シミュレーション付きで結晶を生成
                Vector3 growthPosition = new Vector3(spacingBetweenCrystals * 2, 0, spacingBetweenCrystals * 2);
                
                var crystal = CrystalStructureGenerator.GenerateCrystalWithGrowthSimulation(testScale, true);
                
                if (crystal != null)
                {
                    crystal.transform.position = growthPosition;
                    crystal.name = "TestCrystal_GrowthSimulation";
                    crystal.transform.SetParent(this.transform);
                    
                    // 特別なマテリアルを設定
                    SetupGrowthSimulationMaterial(crystal.gameObject);
                    
                    Debug.Log("Successfully generated crystal with growth simulation");
                }
                else
                {
                    Debug.LogError("Failed to generate crystal with growth simulation");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error testing growth simulation: {e.Message}");
            }
        }

        /// <summary>
        /// テスト用マテリアルを設定
        /// </summary>
        private void SetupTestMaterial(GameObject crystal, CrystalStructureGenerator.CrystalSystem crystalSystem)
        {
            var renderer = crystal.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                // 結晶系に応じた色を設定
                Color crystalColor = GetCrystalSystemColor(crystalSystem);
                
                // 基本マテリアルを作成
                Material material = new Material(Shader.Find("Standard"));
                material.color = crystalColor;
                material.SetFloat("_Metallic", 0.2f);
                material.SetFloat("_Smoothness", 0.8f);
                
                renderer.material = material;
            }
        }

        /// <summary>
        /// 成長シミュレーション用マテリアルを設定
        /// </summary>
        private void SetupGrowthSimulationMaterial(GameObject crystal)
        {
            var renderer = crystal.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Standard"));
                material.color = Color.cyan;
                material.SetFloat("_Metallic", 0.5f);
                material.SetFloat("_Smoothness", 0.9f);
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.cyan * 0.2f);
                
                renderer.material = material;
            }
        }

        /// <summary>
        /// 結晶系に応じた色を取得
        /// </summary>
        private Color GetCrystalSystemColor(CrystalStructureGenerator.CrystalSystem crystalSystem)
        {
            switch (crystalSystem)
            {
                case CrystalStructureGenerator.CrystalSystem.Cubic:
                    return Color.red;
                case CrystalStructureGenerator.CrystalSystem.Hexagonal:
                    return Color.green;
                case CrystalStructureGenerator.CrystalSystem.Tetragonal:
                    return Color.blue;
                case CrystalStructureGenerator.CrystalSystem.Orthorhombic:
                    return Color.yellow;
                case CrystalStructureGenerator.CrystalSystem.Monoclinic:
                    return Color.magenta;
                case CrystalStructureGenerator.CrystalSystem.Triclinic:
                    return Color.white;
                default:
                    return Color.gray;
            }
        }

        /// <summary>
        /// パフォーマンステストを実行
        /// </summary>
        [ContextMenu("Run Performance Test")]
        public void RunPerformanceTest()
        {
            Debug.Log("=== Starting Crystal Generation Performance Test ===");
            
            int testCount = 10;
            float totalTime = 0f;
            
            for (int i = 0; i < testCount; i++)
            {
                float startTime = Time.realtimeSinceStartup;
                
                var crystal = CrystalStructureGenerator.GenerateCrystalStructure(testScale);
                
                float endTime = Time.realtimeSinceStartup;
                float generationTime = endTime - startTime;
                totalTime += generationTime;
                
                if (crystal != null)
                {
                    DestroyImmediate(crystal.gameObject);
                }
                
                Debug.Log($"Crystal {i + 1} generation time: {generationTime * 1000f:F2}ms");
            }
            
            float averageTime = totalTime / testCount;
            Debug.Log($"Average crystal generation time: {averageTime * 1000f:F2}ms");
            Debug.Log("=== Performance Test Completed ===");
        }

        /// <summary>
        /// 生成された結晶をクリア
        /// </summary>
        [ContextMenu("Clear Generated Crystals")]
        public void ClearGeneratedCrystals()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
            
            Debug.Log("Cleared all generated crystals");
        }

        /// <summary>
        /// 詳細な品質レポートを生成
        /// </summary>
        [ContextMenu("Generate Quality Report")]
        public void GenerateQualityReport()
        {
            Debug.Log("=== Crystal Quality Report ===");
            
            var crystalSystems = System.Enum.GetValues(typeof(CrystalStructureGenerator.CrystalSystem));
            
            foreach (CrystalStructureGenerator.CrystalSystem system in crystalSystems)
            {
                var parameters = CrystalStructureGenerator.CrystalGenerationParams.Default(system);
                var crystal = CrystalStructureGenerator.GenerateCrystalStructure(parameters);
                
                if (crystal != null)
                {
                    float quality = CrystalStructureGenerator.EvaluateCrystalQuality(crystal, parameters);
                    string description = CrystalStructureGenerator.GetCrystalSystemDescription(system);
                    
                    Debug.Log($"{system}: Quality={quality:F3}, {description}");
                    
                    DestroyImmediate(crystal.gameObject);
                }
            }
            
            Debug.Log("=== End Quality Report ===");
        }
    }
}