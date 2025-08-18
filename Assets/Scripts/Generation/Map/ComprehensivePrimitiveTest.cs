using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Generation
{
    /// <summary>
    /// 16種類全てのプリミティブの包括的テストシステム
    /// 高品質生成の保証と問題の自動修正
    /// </summary>
    public class ComprehensivePrimitiveTest : MonoBehaviour
    {
        [Header("テスト設定")]
        [SerializeField] private bool runTestOnStart = true;
        [SerializeField] private bool autoFixIssues = true;
        [SerializeField] private bool generateTestScene = false;
        [SerializeField] private Vector3 testAreaSize = new Vector3(1000f, 100f, 1000f);
        
        [Header("品質設定")]
        [SerializeField] private PrimitiveQualityValidator.QualityStandards qualityStandards = PrimitiveQualityValidator.QualityStandards.High;
        [SerializeField] private HighQualityPrimitiveGenerator.QualitySettings generationQuality = HighQualityPrimitiveGenerator.QualitySettings.High;
        
        [Header("テスト結果")]
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

        #region メインテスト関数
        /// <summary>
        /// 包括的テストを開始
        /// </summary>
        [ContextMenu("Start Comprehensive Test")]
        public void StartComprehensiveTest()
        {
            Debug.Log("🚀 Starting comprehensive primitive quality test for all 16 types...");
            
            CleanupTestObjects();
            
            // 全プリミティブタイプをテスト
            testResults = TestAllPrimitiveTypes();
            
            // 結果を分析
            AnalyzeTestResults();
            
            // 問題を自動修正（有効な場合）
            if (autoFixIssues)
            {
                AutoFixIssues();
            }
            
            // テストシーンを生成（有効な場合）
            if (generateTestScene)
            {
                GenerateTestScene();
            }
            
            // 最終レポートを生成
            GenerateFinalReport();
        }

        /// <summary>
        /// 全プリミティブタイプをテスト
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
                    
                    // テスト位置を計算（グリッド配置）
                    Vector3 testPosition = CalculateTestPosition(i, allTypes.Length);
                    Vector3 testScale = GetOptimalScaleForType(primitiveType);
                    
                    // 高品質プリミティブを生成
                    GameObject primitiveObject = HighQualityPrimitiveGenerator.GenerateHighQualityPrimitive(
                        primitiveType, 
                        testPosition, 
                        testScale, 
                        generationQuality
                    );

                    if (primitiveObject != null)
                    {
                        testObjects.Add(primitiveObject);
                        
                        // 品質を検証
                        var report = PrimitiveQualityValidator.ValidatePrimitiveQuality(
                            primitiveObject, 
                            primitiveType, 
                            qualityStandards
                        );
                        
                        results[primitiveType] = report;
                        
                        // 追加のテストを実行
                        PerformAdditionalTests(primitiveObject, primitiveType, report);
                        
                        Debug.Log($"✅ {primitiveType} test completed - Score: {report.overallScore:F2}");
                    }
                    else
                    {
                        Debug.LogError($"❌ Failed to generate {primitiveType}");
                        var failedReport = new PrimitiveQualityValidator.QualityReport(primitiveType);
                        failedReport.issues.Add("Generation failed");
                        results[primitiveType] = failedReport;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ Error testing {primitiveType}: {e.Message}");
                    var errorReport = new PrimitiveQualityValidator.QualityReport(primitiveType);
                    errorReport.issues.Add($"Test error: {e.Message}");
                    results[primitiveType] = errorReport;
                }
            }

            return results;
        }

        /// <summary>
        /// テスト結果を分析
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

            Debug.Log($"📊 Test Analysis Complete:");
            Debug.Log($"   Passed: {passedPrimitives}/{totalPrimitives} ({(float)passedPrimitives/totalPrimitives*100:F1}%)");
            Debug.Log($"   Overall Quality Score: {overallQualityScore:F2}");
            
            if (failedPrimitiveTypes.Count > 0)
            {
                Debug.LogWarning($"   Failed Types: {string.Join(", ", failedPrimitiveTypes)}");
            }
        }

        /// <summary>
        /// 問題を自動修正
        /// </summary>
        private void AutoFixIssues()
        {
            if (testResults == null) return;

            Debug.Log("🔧 Starting automatic issue fixing...");
            
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
                        
                        // 修正後に再テスト
                        var retestResult = RetestPrimitive(primitiveType);
                        if (retestResult != null)
                        {
                            testResults[primitiveType] = retestResult;
                        }
                    }
                }
            }
            
            Debug.Log($"🔧 Auto-fix completed: {fixedCount} primitives fixed");
            
            // 結果を再分析
            AnalyzeTestResults();
        }

        /// <summary>
        /// プリミティブの問題を修正を試行
        /// </summary>
        private bool TryFixPrimitiveIssues(PrimitiveTerrainGenerator.PrimitiveType primitiveType, PrimitiveQualityValidator.QualityReport report)
        {
            bool fixed = false;
            
            foreach (var issue in report.issues)
            {
                if (issue.Contains("vertex count"))
                {
                    // 頂点数の問題を修正
                    if (issue.Contains("Insufficient"))
                    {
                        generationQuality.subdivisionLevel = Mathf.Min(generationQuality.subdivisionLevel + 1, 5);
                        fixed = true;
                        Debug.Log($"Increased subdivision level to {generationQuality.subdivisionLevel} for {primitiveType}");
                    }
                    else if (issue.Contains("Excessive"))
                    {
                        generationQuality.subdivisionLevel = Mathf.Max(generationQuality.subdivisionLevel - 1, 0);
                        fixed = true;
                        Debug.Log($"Decreased subdivision level to {generationQuality.subdivisionLevel} for {primitiveType}");
                    }
                }
                
                if (issue.Contains("Missing mesh normals"))
                {
                    generationQuality.enableSmoothNormals = true;
                    fixed = true;
                    Debug.Log($"Enabled smooth normals for {primitiveType}");
                }
                
                if (issue.Contains("Missing required collider"))
                {
                    generationQuality.enablePreciseColliders = true;
                    fixed = true;
                    Debug.Log($"Enabled precise colliders for {primitiveType}");
                }
                
                if (issue.Contains("Poor symmetry"))
                {
                    generationQuality.enableAdvancedDeformation = false;
                    fixed = true;
                    Debug.Log($"Disabled advanced deformation for better symmetry in {primitiveType}");
                }
            }
            
            return fixed;
        }

        /// <summary>
        /// プリミティブを再テスト
        /// </summary>
        private PrimitiveQualityValidator.QualityReport RetestPrimitive(PrimitiveTerrainGenerator.PrimitiveType primitiveType)
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
                    
                    // テスト用オブジェクトを削除
                    DestroyImmediate(primitiveObject);
                    
                    return report;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error retesting {primitiveType}: {e.Message}");
            }
            
            return null;
        }
        #endregion

        #region 追加テスト
        /// <summary>
        /// 追加のテストを実行
        /// </summary>
        private void PerformAdditionalTests(GameObject primitiveObject, PrimitiveTerrainGenerator.PrimitiveType primitiveType, PrimitiveQualityValidator.QualityReport report)
        {
            // パフォーマンステスト
            TestRenderingPerformance(primitiveObject, report);
            
            // メモリ使用量テスト
            TestMemoryUsage(primitiveObject, report);
            
            // LODテスト
            TestLODSystem(primitiveObject, report);
            
            // インタラクションテスト
            TestInteractionSystems(primitiveObject, primitiveType, report);
        }

        /// <summary>
        /// レンダリングパフォーマンステスト
        /// </summary>
        private void TestRenderingPerformance(GameObject primitiveObject, PrimitiveQualityValidator.QualityReport report)
        {
            var meshFilter = primitiveObject.GetComponent<MeshFilter>();
            if (meshFilter?.sharedMesh != null)
            {
                var mesh = meshFilter.sharedMesh;
                int triangleCount = mesh.triangles.Length / 3;
                
                // 三角形数が多すぎる場合は警告
                if (triangleCount > 2000)
                {
                    report.issues.Add($"High triangle count may impact performance: {triangleCount}");
                }
                
                // UV座標の確認
                if (mesh.uv == null || mesh.uv.Length == 0)
                {
                    report.issues.Add("Missing UV coordinates for texturing");
                }
            }
        }

        /// <summary>
        /// メモリ使用量テスト
        /// </summary>
        private void TestMemoryUsage(GameObject primitiveObject, PrimitiveQualityValidator.QualityReport report)
        {
            var meshFilter = primitiveObject.GetComponent<MeshFilter>();
            if (meshFilter?.sharedMesh != null)
            {
                var mesh = meshFilter.sharedMesh;
                
                // 概算メモリ使用量を計算
                int vertexMemory = mesh.vertexCount * 12; // Vector3 = 12 bytes
                int triangleMemory = mesh.triangles.Length * 4; // int = 4 bytes
                int totalMemory = vertexMemory + triangleMemory;
                
                // 1MB以上の場合は警告
                if (totalMemory > 1024 * 1024)
                {
                    report.issues.Add($"High memory usage: {totalMemory / 1024}KB");
                }
            }
        }

        /// <summary>
        /// LODシステムテスト
        /// </summary>
        private void TestLODSystem(GameObject primitiveObject, PrimitiveQualityValidator.QualityReport report)
        {
            var primitiveComponent = primitiveObject.GetComponent<PrimitiveTerrainObject>();
            if (primitiveComponent != null)
            {
                if (!primitiveComponent.enableLOD)
                {
                    report.recommendations.Add("Consider enabling LOD for better performance");
                }
                
                if (primitiveComponent.lodMeshes == null || primitiveComponent.lodMeshes.Length == 0)
                {
                    report.recommendations.Add("Generate LOD meshes for distance-based optimization");
                }
            }
        }

        /// <summary>
        /// インタラクションシステムテスト
        /// </summary>
        private void TestInteractionSystems(GameObject primitiveObject, PrimitiveTerrainGenerator.PrimitiveType primitiveType, PrimitiveQualityValidator.QualityReport report)
        {
            var primitiveComponent = primitiveObject.GetComponent<PrimitiveTerrainObject>();
            if (primitiveComponent != null)
            {
                // プリミティブタイプに応じた適切なインタラクション設定をチェック
                switch (primitiveType)
                {
                    case PrimitiveTerrainGenerator.PrimitiveType.Arch:
                    case PrimitiveTerrainGenerator.PrimitiveType.Ring:
                        if (!primitiveComponent.isGrindable)
                        {
                            report.recommendations.Add("Arch/Ring structures should be grindable");
                        }
                        break;
                        
                    case PrimitiveTerrainGenerator.PrimitiveType.Mesa:
                    case PrimitiveTerrainGenerator.PrimitiveType.Formation:
                        if (!primitiveComponent.isClimbable)
                        {
                            report.recommendations.Add("Mesa/Formation structures should be climbable");
                        }
                        break;
                }
            }
        }
        #endregion

        #region ユーティリティ関数
        /// <summary>
        /// テスト位置を計算
        /// </summary>
        private Vector3 CalculateTestPosition(int index, int totalCount)
        {
            int gridSize = Mathf.CeilToInt(Mathf.Sqrt(totalCount));
            int row = index / gridSize;
            int col = index % gridSize;
            
            float spacing = 300f; // プリミティブ間の間隔
            float offsetX = (col - gridSize * 0.5f) * spacing;
            float offsetZ = (row - gridSize * 0.5f) * spacing;
            
            return transform.position + new Vector3(offsetX, 0, offsetZ);
        }

        /// <summary>
        /// プリミティブタイプに最適なスケールを取得
        /// </summary>
        private Vector3 GetOptimalScaleForType(PrimitiveTerrainGenerator.PrimitiveType primitiveType)
        {
            return PrimitiveTerrainGenerator.GetDefaultScale(primitiveType);
        }

        /// <summary>
        /// テストシーンを生成
        /// </summary>
        private void GenerateTestScene()
        {
            Debug.Log("🎬 Generating test scene with all primitives...");
            
            // テストシーン用の親オブジェクトを作成
            GameObject testSceneRoot = new GameObject("PrimitiveTestScene");
            testSceneRoot.transform.position = transform.position + Vector3.forward * 500f;
            
            foreach (var kvp in testResults)
            {
                var primitiveType = kvp.Key;
                var report = kvp.Value;
                
                // 品質に応じて色分けしたマテリアルを作成
                Material testMaterial = CreateQualityMaterial(report.overallScore);
                
                // プリミティブを生成
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
                    
                    // 品質マテリアルを適用
                    var renderer = primitiveObject.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.material = testMaterial;
                    }
                    
                    // 情報表示用のラベルを追加
                    CreateInfoLabel(primitiveObject, primitiveType, report);
                }
            }
            
            Debug.Log($"🎬 Test scene generated with {testResults.Count} primitives");
        }

        /// <summary>
        /// 品質に応じたマテリアルを作成
        /// </summary>
        private Material CreateQualityMaterial(float qualityScore)
        {
            Material material = new Material(Shader.Find("Standard"));
            
            if (qualityScore >= 0.9f)
                material.color = Color.green;      // 優秀
            else if (qualityScore >= 0.7f)
                material.color = Color.yellow;     // 良好
            else if (qualityScore >= 0.5f)
                material.color = Color.orange;     // 普通
            else
                material.color = Color.red;        // 要改善
                
            return material;
        }

        /// <summary>
        /// 情報表示ラベルを作成
        /// </summary>
        private void CreateInfoLabel(GameObject primitiveObject, PrimitiveTerrainGenerator.PrimitiveType primitiveType, PrimitiveQualityValidator.QualityReport report)
        {
            // 3Dテキストでプリミティブ情報を表示
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
        /// 最終レポートを生成
        /// </summary>
        private void GenerateFinalReport()
        {
            Debug.Log("📋 Generating final test report...");
            
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
                report += "🎉 SUCCESS: All 16 primitive types are generating with high quality!\n\n";
            }
            else
            {
                report += $"⚠️  WARNING: {totalPrimitives - passedPrimitives} primitive types need attention:\n";
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
                
                report += $"  {type}: {(result.passedValidation ? "✅" : "❌")} Score: {result.overallScore:F2}\n";
                
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
            
            // ファイルに保存
            string filePath = $"Assets/primitive_quality_report_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
            System.IO.File.WriteAllText(filePath, report);
            Debug.Log($"📄 Report saved to: {filePath}");
        }

        /// <summary>
        /// テストオブジェクトをクリーンアップ
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

        #region エディタ用メソッド
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
                PrimitiveTerrainGenerator.GetDefaultScale(randomType)
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