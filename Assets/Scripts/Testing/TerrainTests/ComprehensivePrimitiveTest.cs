using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Vastcore.Generation;
using System.Linq;
using Vastcore.Generation;

namespace Vastcore.Generation
{
    /// <summary>
    /// 16遞ｮ鬘槫・縺ｦ縺ｮ繝励Μ繝溘ユ繧｣繝悶・蛹・峡逧・ユ繧ｹ繝医す繧ｹ繝・Β
    /// 鬮伜刀雉ｪ逕滓・縺ｮ菫晁ｨｼ縺ｨ蝠城｡後・閾ｪ蜍穂ｿｮ豁｣
    /// </summary>
    public class ComprehensivePrimitiveTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestOnStart = true;
        [SerializeField] private bool autoFixIssues = true;
        [SerializeField] private bool generateTestScene = false;
        [SerializeField] private Vector3 testAreaSize = new Vector3(1000f, 100f, 1000f);
        
        [Header("Quality Settings")]
        [SerializeField] private PrimitiveQualityValidator.QualityStandards qualityStandards = PrimitiveQualityValidator.QualityStandards.High;
        [SerializeField] private HighQualityPrimitiveGenerator.QualitySettings generationQuality = HighQualityPrimitiveGenerator.QualitySettings.High;
        
        [Header("繝・せ繝育ｵ先棡")]
        [SerializeField] private int totalPrimitives = 16;
        [SerializeField] private int passedPrimitives = 0;
        [SerializeField] private float overallQualityScore = 0f;
        [SerializeField] private List<string> failedPrimitiveTypes = new List<string>();
        
        private Dictionary<PrimitiveTerrainGenerator.PrimitiveType, PrimitiveQualityValidator.QualityReport> testResults;
        private List<GameObject> testObjects = new List<GameObject>();

        #region Unity Events
        void Start()
        {
            if (runTestOnStart)
            {
                StartComprehensiveTest();
            }
        }

        void OnDestroy()
        {
            CleanupTestObjects();
        }
        #endregion

        #region 繝｡繧､繝ｳ繝・せ繝磯未謨ｰ
        /// <summary>
        /// 蛹・峡逧・ユ繧ｹ繝医ｒ髢句ｧ・
        /// </summary>
        [ContextMenu("Start Comprehensive Test")]
        public void StartComprehensiveTest()
        {
            Debug.Log("噫 Starting comprehensive primitive quality test for all 16 types...");
            
            CleanupTestObjects();
            
            // 蜈ｨ繝励Μ繝溘ユ繧｣繝悶ち繧､繝励ｒ繝・せ繝・
            testResults = TestAllPrimitiveTypes();
            
            // 邨先棡繧貞・譫・
            AnalyzeTestResults();
            
            // 蝠城｡後ｒ閾ｪ蜍穂ｿｮ豁｣・域怏蜉ｹ縺ｪ蝣ｴ蜷茨ｼ・
            if (autoFixIssues)
            {
                AutoFixIssues();
            }
            
            // 繝・せ繝医す繝ｼ繝ｳ繧堤函謌撰ｼ域怏蜉ｹ縺ｪ蝣ｴ蜷茨ｼ・
            if (generateTestScene)
            {
                GenerateTestScene();
            }
            
            // 譛邨ゅΞ繝昴・繝医ｒ逕滓・
            GenerateFinalReport();
        }

        /// <summary>
        /// 蜈ｨ繝励Μ繝溘ユ繧｣繝悶ち繧､繝励ｒ繝・せ繝・
        /// </summary>
        private Dictionary<PrimitiveTerrainGenerator.PrimitiveType, PrimitiveQualityValidator.QualityReport> TestAllPrimitiveTypes()
        {
            var results = new Dictionary<PrimitiveTerrainGenerator.PrimitiveType, PrimitiveQualityValidator.QualityReport>();
            var allTypes = System.Enum.GetValues(typeof(PrimitiveTerrainGenerator.PrimitiveType))
                                .Cast<PrimitiveTerrainGenerator.PrimitiveType>()
                                .ToArray();

            Debug.Log($"Testing {allTypes.Length} primitive types with {generationQuality.subdivisionLevel} subdivision levels");

            for (int i = 0; i < allTypes.Length; i++)
            {
                var primitiveType = allTypes[i];
                
                try
                {
                    Debug.Log($"[{i+1}/{allTypes.Length}] Testing {primitiveType}...");
                    
                    // 繝・せ繝井ｽ咲ｽｮ繧定ｨ育ｮ暦ｼ医げ繝ｪ繝・ラ驟咲ｽｮ・・
                    Vector3 testPosition = CalculateTestPosition(i, allTypes.Length);
                    Vector3 testScale = GetOptimalScaleForType(primitiveType);
                    
                    // 鬮伜刀雉ｪ繝励Μ繝溘ユ繧｣繝悶ｒ逕滓・
                    GameObject primitiveObject = HighQualityPrimitiveGenerator.GenerateHighQualityPrimitive(
                        primitiveType, 
                        testPosition, 
                        testScale, 
                        generationQuality
                    );

                    if (primitiveObject != null)
                    {
                        testObjects.Add(primitiveObject);
                        
                        // 蜩∬ｳｪ繧呈､懆ｨｼ
                        var report = PrimitiveQualityValidator.ValidatePrimitiveQuality(
                            primitiveObject, 
                            primitiveType, 
                            qualityStandards
                        );
                        
                        results[primitiveType] = report;
                        
                        // 霑ｽ蜉縺ｮ繝・せ繝医ｒ螳溯｡・
                        PerformAdditionalTests(primitiveObject, primitiveType, report);
                        
                        Debug.Log($"笨・{primitiveType} test completed - Score: {report.overallScore:F2}");
                    }
                    else
                    {
                        Debug.LogError($"笶・Failed to generate {primitiveType}");
                        var failedReport = new PrimitiveQualityValidator.QualityReport(primitiveType);
                        failedReport.issues.Add("Generation failed");
                        results[primitiveType] = failedReport;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"笶・Error testing {primitiveType}: {e.Message}");
                    var errorReport = new PrimitiveQualityValidator.QualityReport(primitiveType);
                    errorReport.issues.Add($"Test error: {e.Message}");
                    results[primitiveType] = errorReport;
                }
            }

            return results;
        }

        /// <summary>
        /// 繝・せ繝育ｵ先棡繧貞・譫・
        /// </summary>
        private void AnalyzeTestResults()
        {
            if (testResults == null || testResults.Count == 0)
            {
                Debug.LogError("No test results to analyze");
                return;
            }

            passedPrimitives = testResults.Values.Count(r => r.passedValidation);
            totalPrimitives = testResults.Count;
            overallQualityScore = testResults.Values.Average(r => r.overallScore);
            
            failedPrimitiveTypes.Clear();
            failedPrimitiveTypes.AddRange(
                testResults.Where(kvp => !kvp.Value.passedValidation)
                          .Select(kvp => kvp.Key.ToString())
            );

            Debug.Log($"投 Test Analysis Complete:");
            Debug.Log($"   Passed: {passedPrimitives}/{totalPrimitives} ({(float)passedPrimitives/totalPrimitives*100:F1}%)");
            Debug.Log($"   Overall Quality Score: {overallQualityScore:F2}");
            
            if (failedPrimitiveTypes.Count > 0)
            {
                Debug.LogWarning($"   Failed Types: {string.Join(", ", failedPrimitiveTypes)}");
            }
        }

        /// <summary>
        /// 蝠城｡後ｒ閾ｪ蜍穂ｿｮ豁｣
        /// </summary>
        private void AutoFixIssues()
        {
            if (testResults == null) return;

            Debug.Log("肌 Starting automatic issue fixing...");
            
            int fixedCount = 0;
            
            foreach (var kvp in testResults.ToList())
            {
                var primitiveType = kvp.Key;
                var report = kvp.Value;
                
                if (!report.passedValidation && report.issues.Count > 0)
                {
                    Debug.Log($"Attempting to fix issues for {primitiveType}...");
                    
                    if (TryFixPrimitiveIssues(primitiveType, report))
                    {
                        fixedCount++;
                        
                        // 菫ｮ豁｣蠕後↓蜀阪ユ繧ｹ繝・
                        var retestResult = RetestPrimitive(primitiveType);
                        if (retestResult.HasValue)
                        {
                            testResults[primitiveType] = retestResult.Value;
                        }
                    }
                }
            }
            
            Debug.Log($"肌 Auto-fix completed: {fixedCount} primitives fixed");
            
            // 邨先棡繧貞・蛻・梵
            AnalyzeTestResults();
        }

        /// <summary>
        /// 繝励Μ繝溘ユ繧｣繝悶・蝠城｡後ｒ菫ｮ豁｣繧定ｩｦ陦・
        /// </summary>
        private bool TryFixPrimitiveIssues(PrimitiveTerrainGenerator.PrimitiveType primitiveType, PrimitiveQualityValidator.QualityReport report)
        {
            bool isFixed = false;
            
            foreach (var issue in report.issues)
            {
                if (issue.Contains("vertex count"))
                {
                    // 鬆らせ謨ｰ縺ｮ蝠城｡後ｒ菫ｮ豁｣
                    if (issue.Contains("Insufficient"))
                    {
                        generationQuality.subdivisionLevel = Mathf.Min(generationQuality.subdivisionLevel + 1, 5);
                        isFixed = true;
                        Debug.Log($"Increased subdivision level to {generationQuality.subdivisionLevel} for {primitiveType}");
                    }
                    else if (issue.Contains("Excessive"))
                    {
                        generationQuality.subdivisionLevel = Mathf.Max(generationQuality.subdivisionLevel - 1, 0);
                        isFixed = true;
                        Debug.Log($"Decreased subdivision level to {generationQuality.subdivisionLevel} for {primitiveType}");
                    }
                }
                
                if (issue.Contains("Missing mesh normals"))
                {
                    generationQuality.enableSmoothNormals = true;
                    isFixed = true;
                    Debug.Log($"Enabled smooth normals for {primitiveType}");
                }
                
                if (issue.Contains("Missing required collider"))
                {
                    generationQuality.enablePreciseColliders = true;
                    isFixed = true;
                    Debug.Log($"Enabled precise colliders for {primitiveType}");
                }
                
                if (issue.Contains("Poor symmetry"))
                {
                    generationQuality.enableAdvancedDeformation = false;
                    isFixed = true;
                    Debug.Log($"Disabled advanced deformation for better symmetry in {primitiveType}");
                }
            }
            
            return isFixed;
        }

        /// <summary>
        /// 繝励Μ繝溘ユ繧｣繝悶ｒ蜀阪ユ繧ｹ繝・
        /// </summary>
        private PrimitiveQualityValidator.QualityReport? RetestPrimitive(PrimitiveTerrainGenerator.PrimitiveType primitiveType)
        {
            try
            {
                Vector3 testPosition = Vector3.zero;
                Vector3 testScale = GetOptimalScaleForType(primitiveType);
                
                var primitiveObject = HighQualityPrimitiveGenerator.GenerateHighQualityPrimitive(
                    primitiveType, 
                    testPosition, 
                    testScale, 
                    generationQuality
                );

                if (primitiveObject != null)
                {
                    var report = PrimitiveQualityValidator.ValidatePrimitiveQuality(
                        primitiveObject, 
                        primitiveType, 
                        qualityStandards
                    );
                    
                    // 繝・せ繝育畑繧ｪ繝悶ず繧ｧ繧ｯ繝医ｒ蜑企勁
                    DestroyImmediate(primitiveObject);
                    
                    return report;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error retesting {primitiveType}: {e.Message}");
            }
            
            return default;
        }
        #endregion

        #region 霑ｽ蜉繝・せ繝・
        /// <summary>
        /// 霑ｽ蜉縺ｮ繝・せ繝医ｒ螳溯｡・
        /// </summary>
        private void PerformAdditionalTests(GameObject primitiveObject, PrimitiveTerrainGenerator.PrimitiveType primitiveType, PrimitiveQualityValidator.QualityReport? report)
        {
            // 繝代ヵ繧ｩ繝ｼ繝槭Φ繧ｹ繝・せ繝・
            TestRenderingPerformance(primitiveObject, report);
            
            // 繝｡繝｢繝ｪ菴ｿ逕ｨ驥上ユ繧ｹ繝・
            TestMemoryUsage(primitiveObject, report);
            
            // LOD繝・せ繝・
            TestLODSystem(primitiveObject, report);
            
            // 繧､繝ｳ繧ｿ繝ｩ繧ｯ繧ｷ繝ｧ繝ｳ繝・せ繝・
            TestInteractionSystems(primitiveObject, primitiveType, report);
        }

        /// <summary>
        /// 繝ｬ繝ｳ繝繝ｪ繝ｳ繧ｰ繝代ヵ繧ｩ繝ｼ繝槭Φ繧ｹ繝・せ繝・
        /// </summary>
        private void TestRenderingPerformance(GameObject primitiveObject, PrimitiveQualityValidator.QualityReport? report)
        {
            if (!report.HasValue) return;
            var rep = report.Value;
            
            var meshFilter = primitiveObject.GetComponent<MeshFilter>();
            if (meshFilter?.sharedMesh != null)
            {
                var mesh = meshFilter.sharedMesh;
                int triangleCount = mesh.triangles.Length / 3;
                
                // 荳芽ｧ貞ｽ｢謨ｰ縺悟､壹☆縺弱ｋ蝣ｴ蜷医・隴ｦ蜻・
                if (triangleCount > 2000)
                {
                    rep.issues.Add($"High triangle count may impact performance: {triangleCount}");
                }
                
                // UV蠎ｧ讓吶・遒ｺ隱・
                if (mesh.uv == null || mesh.uv.Length == 0)
                {
                    rep.issues.Add("Missing UV coordinates for texturing");
                }
            }
        }

        /// <summary>
        /// 繝｡繝｢繝ｪ菴ｿ逕ｨ驥上ユ繧ｹ繝・
        /// </summary>
        private void TestMemoryUsage(GameObject primitiveObject, PrimitiveQualityValidator.QualityReport? report)
        {
            if (!report.HasValue) return;
            var rep = report.Value;
            
            var meshFilter = primitiveObject.GetComponent<MeshFilter>();
            if (meshFilter?.sharedMesh != null)
            {
                var mesh = meshFilter.sharedMesh;
                
                // 讎らｮ励Γ繝｢繝ｪ菴ｿ逕ｨ驥上ｒ險育ｮ・
                int vertexMemory = mesh.vertexCount * 12; // Vector3 = 12 bytes
                int triangleMemory = mesh.triangles.Length * 4; // int = 4 bytes
                int totalMemory = vertexMemory + triangleMemory;
                
                // 1MB莉･荳翫・蝣ｴ蜷医・隴ｦ蜻・
                if (totalMemory > 1024 * 1024)
                {
                    rep.issues.Add($"High memory usage: {totalMemory / 1024}KB");
                }
            }
        }

        /// <summary>
        /// LOD繧ｷ繧ｹ繝・Β繝・せ繝・
        /// </summary>
        private void TestLODSystem(GameObject primitiveObject, PrimitiveQualityValidator.QualityReport? report)
        {
            if (!report.HasValue) return;
            var rep = report.Value;
            
            var primitiveComponent = primitiveObject.GetComponent<PrimitiveTerrainObject>();
            if (primitiveComponent != null)
            {
                if (!primitiveComponent.enableLOD)
                {
                    rep.recommendations.Add("Consider enabling LOD for better performance");
                }
                
                if (primitiveComponent.lodDistance0 <= 0f ||
                    primitiveComponent.lodDistance1 <= primitiveComponent.lodDistance0 ||
                    primitiveComponent.lodDistance2 <= primitiveComponent.lodDistance1)
                {
                    rep.recommendations.Add("Review LOD distance thresholds for proper progression");
                }
            }
        }

        /// <summary>
        /// 繧､繝ｳ繧ｿ繝ｩ繧ｯ繧ｷ繝ｧ繝ｳ繧ｷ繧ｹ繝・Β繝・せ繝・
        /// </summary>
        private void TestInteractionSystems(GameObject primitiveObject, PrimitiveTerrainGenerator.PrimitiveType primitiveType, PrimitiveQualityValidator.QualityReport? report)
        {
            if (!report.HasValue) return;
            var rep = report.Value;
            
            var primitiveComponent = primitiveObject.GetComponent<PrimitiveTerrainObject>();
            if (primitiveComponent != null)
            {
                // 繝励Μ繝溘ユ繧｣繝悶ち繧､繝励↓蠢懊§縺滄←蛻・↑繧､繝ｳ繧ｿ繝ｩ繧ｯ繧ｷ繝ｧ繝ｳ險ｭ螳壹ｒ繝√ぉ繝・け
                switch (primitiveType)
                {
                    case PrimitiveTerrainGenerator.PrimitiveType.Arch:
                    case PrimitiveTerrainGenerator.PrimitiveType.Ring:
                        if (!primitiveComponent.isGrindable)
                        {
                            rep.recommendations.Add("Arch/Ring structures should be grindable");
                        }
                        break;
                        
                    case PrimitiveTerrainGenerator.PrimitiveType.Mesa:
                    case PrimitiveTerrainGenerator.PrimitiveType.Formation:
                        if (!primitiveComponent.isClimbable)
                        {
                            rep.recommendations.Add("Mesa/Formation structures should be climbable");
                        }
                        break;
                }
            }
        }
        #endregion

        #region 繝ｦ繝ｼ繝・ぅ繝ｪ繝・ぅ髢｢謨ｰ
        /// <summary>
        /// 繝・せ繝井ｽ咲ｽｮ繧定ｨ育ｮ・
        /// </summary>
        private Vector3 CalculateTestPosition(int index, int totalCount)
        {
            int gridSize = Mathf.CeilToInt(Mathf.Sqrt(totalCount));
            int row = index / gridSize;
            int col = index % gridSize;
            
            float spacing = 300f; // 繝励Μ繝溘ユ繧｣繝夜俣縺ｮ髢馴囈
            float offsetX = (col - gridSize * 0.5f) * spacing;
            float offsetZ = (row - gridSize * 0.5f) * spacing;
            
            return transform.position + new Vector3(offsetX, 0, offsetZ);
        }

        /// <summary>
        /// 繝励Μ繝溘ユ繧｣繝悶ち繧､繝励↓譛驕ｩ縺ｪ繧ｹ繧ｱ繝ｼ繝ｫ繧貞叙蠕・
        /// </summary>
        private Vector3 GetOptimalScaleForType(PrimitiveTerrainGenerator.PrimitiveType primitiveType)
        {
            return PrimitiveTerrainGenerator.GetDefaultScale(primitiveType);
        }

        /// <summary>
        /// 繝・せ繝医す繝ｼ繝ｳ繧堤函謌・
        /// </summary>
        private void GenerateTestScene()
        {
            Debug.Log("汐 Generating test scene with all primitives...");
            
            // 繝・せ繝医す繝ｼ繝ｳ逕ｨ縺ｮ隕ｪ繧ｪ繝悶ず繧ｧ繧ｯ繝医ｒ菴懈・
            GameObject testSceneRoot = new GameObject("PrimitiveTestScene");
            testSceneRoot.transform.position = transform.position + Vector3.forward * 500f;
            
            foreach (var kvp in testResults)
            {
                var primitiveType = kvp.Key;
                var report = kvp.Value;
                
                // 蜩∬ｳｪ縺ｫ蠢懊§縺ｦ濶ｲ蛻・￠縺励◆繝槭ユ繝ｪ繧｢繝ｫ繧剃ｽ懈・
                Material testMaterial = CreateQualityMaterial(report.overallScore);
                
                // 繝励Μ繝溘ユ繧｣繝悶ｒ逕滓・
                Vector3 position = CalculateTestPosition((int)primitiveType, testResults.Count);
                position += testSceneRoot.transform.position;
                
                var primitiveObject = HighQualityPrimitiveGenerator.GenerateHighQualityPrimitive(
                    primitiveType, 
                    position, 
                    GetOptimalScaleForType(primitiveType), 
                    generationQuality
                );
                
                if (primitiveObject != null)
                {
                    primitiveObject.transform.SetParent(testSceneRoot.transform);
                    
                    // 蜩∬ｳｪ繝槭ユ繝ｪ繧｢繝ｫ繧帝←逕ｨ
                    var renderer = primitiveObject.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.material = testMaterial;
                    }
                    
                    // 諠・ｱ陦ｨ遉ｺ逕ｨ縺ｮ繝ｩ繝吶Ν繧定ｿｽ蜉
                    CreateInfoLabel(primitiveObject, primitiveType, report);
                }
            }
            
            Debug.Log($"汐 Test scene generated with {testResults.Count} primitives");
        }

        /// <summary>
        /// 蜩∬ｳｪ縺ｫ蠢懊§縺溘・繝・Μ繧｢繝ｫ繧剃ｽ懈・
        /// </summary>
        private Material CreateQualityMaterial(float qualityScore)
        {
            Material material = new Material(Shader.Find("Standard"));
            
            if (qualityScore >= 0.9f)
                material.color = Color.green;      // 蜆ｪ遘
            else if (qualityScore >= 0.7f)
                material.color = Color.yellow;     // 濶ｯ螂ｽ
            else if (qualityScore >= 0.5f)
                material.color = Color.orange;     // 譎ｮ騾・
            else
                material.color = Color.red;        // 隕∵隼蝟・
                
            return material;
        }

        /// <summary>
        /// 諠・ｱ陦ｨ遉ｺ繝ｩ繝吶Ν繧剃ｽ懈・
        /// </summary>
        private void CreateInfoLabel(GameObject primitiveObject, PrimitiveTerrainGenerator.PrimitiveType primitiveType, PrimitiveQualityValidator.QualityReport report)
        {
            // 3D繝・く繧ｹ繝医〒繝励Μ繝溘ユ繧｣繝匁ュ蝣ｱ繧定｡ｨ遉ｺ
            GameObject label = new GameObject($"Label_{primitiveType}");
            label.transform.SetParent(primitiveObject.transform);
            label.transform.localPosition = Vector3.up * 150f;
            
            var textMesh = label.AddComponent<TextMesh>();
            textMesh.text = $"{primitiveType}\nScore: {report.overallScore:F2}\n{(report.passedValidation ? "PASS" : "FAIL")}";
            textMesh.fontSize = 20;
            textMesh.color = report.passedValidation ? Color.green : Color.red;
            textMesh.anchor = TextAnchor.MiddleCenter;
        }

        /// <summary>
        /// 譛邨ゅΞ繝昴・繝医ｒ逕滓・
        /// </summary>
        private void GenerateFinalReport()
        {
            Debug.Log("搭 Generating final test report...");
            
            string report = "=== COMPREHENSIVE PRIMITIVE QUALITY TEST REPORT ===\n\n";
            
            report += $"Test Configuration:\n";
            report += $"  Quality Standards: {qualityStandards.minVertexCount}-{qualityStandards.maxVertexCount} vertices\n";
            report += $"  Generation Quality: Subdivision Level {generationQuality.subdivisionLevel}\n";
            report += $"  Auto-fix Issues: {autoFixIssues}\n\n";
            
            report += $"Overall Results:\n";
            report += $"  Total Primitives: {totalPrimitives}\n";
            report += $"  Passed Validation: {passedPrimitives} ({(float)passedPrimitives/totalPrimitives*100:F1}%)\n";
            report += $"  Overall Quality Score: {overallQualityScore:F2}/1.00\n\n";
            
            if (passedPrimitives == totalPrimitives)
            {
                report += "脂 SUCCESS: All 16 primitive types are generating with high quality!\n\n";
            }
            else
            {
                report += $"笞・・ WARNING: {totalPrimitives - passedPrimitives} primitive types need attention:\n";
                foreach (var failedType in failedPrimitiveTypes)
                {
                    report += $"    - {failedType}\n";
                }
                report += "\n";
            }
            
            report += "Detailed Results:\n";
            foreach (var kvp in testResults.OrderBy(x => x.Key.ToString()))
            {
                var type = kvp.Key;
                var result = kvp.Value;
                
                report += $"  {type}: {(result.passedValidation ? "PASS" : "FAIL")} Score: {result.overallScore:F2}\n";
                
                if (result.issues.Count > 0)
                {
                    report += $"    Issues: {string.Join(", ", result.issues)}\n";
                }
                
                if (result.recommendations.Count > 0)
                {
                    report += $"    Recommendations: {string.Join(", ", result.recommendations)}\n";
                }
            }
            
            Debug.Log(report);
            
            // 繝輔ぃ繧､繝ｫ縺ｫ菫晏ｭ・
            string filePath = $"Assets/primitive_quality_report_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
            System.IO.File.WriteAllText(filePath, report);
            Debug.Log($"塘 Report saved to: {filePath}");
        }

        /// <summary>
        /// 繝・せ繝医が繝悶ず繧ｧ繧ｯ繝医ｒ繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・
        /// </summary>
        private void CleanupTestObjects()
        {
            foreach (var obj in testObjects)
            {
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }
            testObjects.Clear();
        }
        #endregion

        #region 繧ｨ繝・ぅ繧ｿ逕ｨ繝｡繧ｽ繝・ラ
        [ContextMenu("Quick Quality Check")]
        public void QuickQualityCheck()
        {
            var results = PrimitiveQualityValidator.ValidateAllPrimitives();
            
            int passed = results.Values.Count(r => r.passedValidation);
            float avgScore = results.Values.Average(r => r.overallScore);
            
            Debug.Log($"Quick Check: {passed}/{results.Count} passed, Avg Score: {avgScore:F2}");
        }

        [ContextMenu("Generate Single Test Primitive")]
        public void GenerateSingleTestPrimitive()
        {
            var randomType = (PrimitiveTerrainGenerator.PrimitiveType)Random.Range(0, 16);
            
            var primitive = HighQualityPrimitiveGenerator.GenerateHighQualityPrimitive(
                randomType,
                transform.position + Vector3.forward * 100f,
                PrimitiveTerrainGenerator.GetDefaultScale(randomType),
                generationQuality
            );
            
            if (primitive != null)
            {
                testObjects.Add(primitive);
                Debug.Log($"Generated test {randomType} at {primitive.transform.position}");
            }
        }

        [ContextMenu("Clear Test Objects")]
        public void ClearTestObjects()
        {
            CleanupTestObjects();
            Debug.Log("Test objects cleared");
        }
        #endregion
    }
}
