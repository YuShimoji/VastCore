using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Generation
{
    /// <summary>
    /// プリミティブ品質テストシーン管理
    /// 16種類全てのプリミティブを生成・表示・検証するシーン
    /// </summary>
    public class PrimitiveQualityTestScene : MonoBehaviour
    {
        [Header("シーン設定")]
        [SerializeField] private bool autoGenerateOnStart = true;
        [SerializeField] private bool enableQualityLabels = true;
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private float primitiveSpacing = 200f;
        
        [Header("生成設定")]
        [SerializeField] private HighQualityPrimitiveGenerator.QualitySettings qualitySettings = HighQualityPrimitiveGenerator.QualitySettings.High;
        [SerializeField] private Vector3 baseScale = new Vector3(100f, 100f, 100f);
        [SerializeField] private bool useTypeSpecificScaling = true;
        
        [Header("表示設定")]
        [SerializeField] private Material highQualityMaterial;
        [SerializeField] private Material mediumQualityMaterial;
        [SerializeField] private Material lowQualityMaterial;
        [SerializeField] private Material failedMaterial;
        
        [Header("テスト結果")]
        [SerializeField] private int totalGenerated = 0;
        [SerializeField] private int highQualityCount = 0;
        [SerializeField] private int mediumQualityCount = 0;
        [SerializeField] private int lowQualityCount = 0;
        [SerializeField] private int failedCount = 0;
        [SerializeField] private float averageQualityScore = 0f;
        
        private Dictionary<PrimitiveTerrainGenerator.PrimitiveType, GameObject> generatedPrimitives = new Dictionary<PrimitiveTerrainGenerator.PrimitiveType, GameObject>();
        private Dictionary<PrimitiveTerrainGenerator.PrimitiveType, PrimitiveQualityValidator.QualityReport> qualityReports = new Dictionary<PrimitiveTerrainGenerator.PrimitiveType, PrimitiveQualityValidator.QualityReport>();
        private List<GameObject> qualityLabels = new List<GameObject>();
        
        #region Unity Events
        void Start()
        {
            if (autoGenerateOnStart)
            {
                GenerateAllPrimitives();
            }
        }
        
        void Update()
        {
            if (enablePerformanceMonitoring)
            {
                MonitorPerformance();
            }
        }
        
        void OnDestroy()
        {
            CleanupScene();
        }
        #endregion
        
        #region メイン機能
        /// <summary>
        /// 全16種類のプリミティブを生成
        /// </summary>
        [ContextMenu("Generate All Primitives")]
        public void GenerateAllPrimitives()
        {
            Debug.Log("🎬 Generating all 16 primitive types for quality testing...");
            
            CleanupScene();
            
            var allTypes = System.Enum.GetValues(typeof(PrimitiveTerrainGenerator.PrimitiveType))
                                .Cast<PrimitiveTerrainGenerator.PrimitiveType>()
                                .ToArray();
            
            totalGenerated = 0;
            highQualityCount = 0;
            mediumQualityCount = 0;
            lowQualityCount = 0;
            failedCount = 0;
            
            for (int i = 0; i < allTypes.Length; i++)
            {
                var primitiveType = allTypes[i];
                GenerateSinglePrimitive(primitiveType, i, allTypes.Length);
            }
            
            // 統計を更新
            UpdateStatistics();
            
            // ラベルを生成
            if (enableQualityLabels)
            {
                GenerateQualityLabels();
            }
            
            // 結果をログ出力
            LogResults();
        }
        
        /// <summary>
        /// 単一プリミティブを生成
        /// </summary>
        private void GenerateSinglePrimitive(PrimitiveTerrainGenerator.PrimitiveType primitiveType, int index, int totalCount)
        {
            try
            {
                Debug.Log($"[{index + 1}/{totalCount}] Generating {primitiveType}...");
                
                // 位置を計算
                Vector3 position = CalculatePosition(index, totalCount);
                
                // スケールを決定
                Vector3 scale = useTypeSpecificScaling ? 
                    PrimitiveTerrainGenerator.GetDefaultScale(primitiveType) : 
                    baseScale;
                
                // プリミティブを生成
                GameObject primitiveObject = HighQualityPrimitiveGenerator.GenerateHighQualityPrimitive(
                    primitiveType,
                    position,
                    scale,
                    qualitySettings
                );
                
                if (primitiveObject != null)
                {
                    // 親オブジェクトに設定
                    primitiveObject.transform.SetParent(transform);
                    
                    // 辞書に追加
                    generatedPrimitives[primitiveType] = primitiveObject;
                    
                    // 品質を検証
                    var qualityReport = PrimitiveQualityValidator.ValidatePrimitiveQuality(
                        primitiveObject,
                        primitiveType,
                        PrimitiveQualityValidator.QualityStandards.High
                    );
                    
                    qualityReports[primitiveType] = qualityReport;
                    
                    // 品質に応じたマテリアルを適用
                    ApplyQualityMaterial(primitiveObject, qualityReport);
                    
                    // 名前を設定
                    primitiveObject.name = $"{primitiveType}_Quality{qualityReport.overallScore:F2}";
                    
                    totalGenerated++;
                    
                    Debug.Log($"✅ {primitiveType} generated successfully - Quality: {qualityReport.overallScore:F2}");
                }
                else
                {
                    Debug.LogError($"❌ Failed to generate {primitiveType}");
                    failedCount++;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Error generating {primitiveType}: {e.Message}");
                failedCount++;
            }
        }
        
        /// <summary>
        /// 位置を計算（グリッド配置）
        /// </summary>
        private Vector3 CalculatePosition(int index, int totalCount)
        {
            int gridSize = Mathf.CeilToInt(Mathf.Sqrt(totalCount));
            int row = index / gridSize;
            int col = index % gridSize;
            
            float offsetX = (col - gridSize * 0.5f) * primitiveSpacing;
            float offsetZ = (row - gridSize * 0.5f) * primitiveSpacing;
            
            return transform.position + new Vector3(offsetX, 0, offsetZ);
        }
        
        /// <summary>
        /// 品質に応じたマテリアルを適用
        /// </summary>
        private void ApplyQualityMaterial(GameObject primitiveObject, PrimitiveQualityValidator.QualityReport report)
        {
            var renderer = primitiveObject.GetComponent<MeshRenderer>();
            if (renderer == null) return;
            
            Material materialToApply = null;
            
            if (!report.passedValidation)
            {
                materialToApply = failedMaterial;
                failedCount++;
            }
            else if (report.overallScore >= 0.9f)
            {
                materialToApply = highQualityMaterial;
                highQualityCount++;
            }
            else if (report.overallScore >= 0.7f)
            {
                materialToApply = mediumQualityMaterial;
                mediumQualityCount++;
            }
            else
            {
                materialToApply = lowQualityMaterial;
                lowQualityCount++;
            }
            
            if (materialToApply != null)
            {
                renderer.material = materialToApply;
            }
            else
            {
                // デフォルトマテリアルを作成
                Material defaultMat = new Material(Shader.Find("Standard"));
                defaultMat.color = GetQualityColor(report.overallScore);
                renderer.material = defaultMat;
            }
        }
        
        /// <summary>
        /// 品質スコアに応じた色を取得
        /// </summary>
        private Color GetQualityColor(float qualityScore)
        {
            if (qualityScore >= 0.9f) return Color.green;
            if (qualityScore >= 0.7f) return Color.yellow;
            if (qualityScore >= 0.5f) return Color.orange;
            return Color.red;
        }
        
        /// <summary>
        /// 品質ラベルを生成
        /// </summary>
        private void GenerateQualityLabels()
        {
            Debug.Log("📝 Generating quality labels...");
            
            foreach (var kvp in generatedPrimitives)
            {
                var primitiveType = kvp.Key;
                var primitiveObject = kvp.Value;
                
                if (qualityReports.ContainsKey(primitiveType))
                {
                    var report = qualityReports[primitiveType];
                    CreateQualityLabel(primitiveObject, primitiveType, report);
                }
            }
        }
        
        /// <summary>
        /// 品質ラベルを作成
        /// </summary>
        private void CreateQualityLabel(GameObject primitiveObject, PrimitiveTerrainGenerator.PrimitiveType primitiveType, PrimitiveQualityValidator.QualityReport report)
        {
            // ラベル用オブジェクトを作成
            GameObject labelObject = new GameObject($"Label_{primitiveType}");
            labelObject.transform.SetParent(primitiveObject.transform);
            
            // プリミティブの上部に配置
            var bounds = primitiveObject.GetComponent<MeshRenderer>().bounds;
            labelObject.transform.position = bounds.center + Vector3.up * (bounds.size.y * 0.6f);
            
            // 3Dテキストを追加
            var textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = $"{primitiveType}\nScore: {report.overallScore:F2}\n{(report.passedValidation ? "PASS" : "FAIL")}";
            textMesh.fontSize = 20;
            textMesh.color = report.passedValidation ? Color.green : Color.red;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            
            // カメラの方を向くように設定
            var billboard = labelObject.AddComponent<Billboard>();
            
            qualityLabels.Add(labelObject);
        }
        
        /// <summary>
        /// 統計を更新
        /// </summary>
        private void UpdateStatistics()
        {
            if (qualityReports.Count > 0)
            {
                averageQualityScore = qualityReports.Values.Average(r => r.overallScore);
            }
        }
        
        /// <summary>
        /// 結果をログ出力
        /// </summary>
        private void LogResults()
        {
            Debug.Log("📊 === PRIMITIVE QUALITY TEST RESULTS ===");
            Debug.Log($"Total Generated: {totalGenerated}");
            Debug.Log($"High Quality (≥0.9): {highQualityCount}");
            Debug.Log($"Medium Quality (≥0.7): {mediumQualityCount}");
            Debug.Log($"Low Quality (≥0.5): {lowQualityCount}");
            Debug.Log($"Failed: {failedCount}");
            Debug.Log($"Average Quality Score: {averageQualityScore:F2}");
            
            float successRate = totalGenerated > 0 ? (float)(totalGenerated - failedCount) / totalGenerated * 100f : 0f;
            Debug.Log($"Success Rate: {successRate:F1}%");
            
            if (totalGenerated == 16 && failedCount == 0)
            {
                Debug.Log("🎉 SUCCESS: All 16 primitive types generated successfully!");
            }
            else if (failedCount > 0)
            {
                Debug.LogWarning($"⚠️ {failedCount} primitive types failed to generate");
            }
            
            // 詳細な品質レポート
            Debug.Log("\n📋 Detailed Quality Report:");
            foreach (var kvp in qualityReports.OrderBy(x => x.Key.ToString()))
            {
                var type = kvp.Key;
                var report = kvp.Value;
                
                string status = report.passedValidation ? "✅ PASS" : "❌ FAIL";
                Debug.Log($"  {type}: {status} (Score: {report.overallScore:F2})");
                
                if (report.issues.Count > 0)
                {
                    Debug.LogWarning($"    Issues: {string.Join(", ", report.issues)}");
                }
            }
        }
        
        /// <summary>
        /// パフォーマンスを監視
        /// </summary>
        private void MonitorPerformance()
        {
            // フレームレートが低下した場合の警告
            if (Time.deltaTime > 0.033f) // 30FPS以下
            {
                Debug.LogWarning($"Performance warning: Frame time {Time.deltaTime * 1000:F1}ms (FPS: {1f / Time.deltaTime:F1})");
            }
        }
        
        /// <summary>
        /// シーンをクリーンアップ
        /// </summary>
        [ContextMenu("Cleanup Scene")]
        public void CleanupScene()
        {
            // 生成されたプリミティブを削除
            foreach (var primitive in generatedPrimitives.Values)
            {
                if (primitive != null)
                {
                    DestroyImmediate(primitive);
                }
            }
            generatedPrimitives.Clear();
            
            // ラベルを削除
            foreach (var label in qualityLabels)
            {
                if (label != null)
                {
                    DestroyImmediate(label);
                }
            }
            qualityLabels.Clear();
            
            // レポートをクリア
            qualityReports.Clear();
            
            // 統計をリセット
            totalGenerated = 0;
            highQualityCount = 0;
            mediumQualityCount = 0;
            lowQualityCount = 0;
            failedCount = 0;
            averageQualityScore = 0f;
            
            Debug.Log("🧹 Scene cleaned up");
        }
        #endregion
        
        #region エディタ用メソッド
        [ContextMenu("Regenerate Failed Primitives")]
        public void RegenerateFailedPrimitives()
        {
            var failedTypes = qualityReports.Where(kvp => !kvp.Value.passedValidation || kvp.Value.overallScore < 0.7f)
                                           .Select(kvp => kvp.Key)
                                           .ToList();
            
            if (failedTypes.Count == 0)
            {
                Debug.Log("No failed primitives to regenerate");
                return;
            }
            
            Debug.Log($"Regenerating {failedTypes.Count} failed primitives...");
            
            foreach (var primitiveType in failedTypes)
            {
                // 既存のオブジェクトを削除
                if (generatedPrimitives.ContainsKey(primitiveType))
                {
                    DestroyImmediate(generatedPrimitives[primitiveType]);
                    generatedPrimitives.Remove(primitiveType);
                }
                
                // 再生成
                int index = (int)primitiveType;
                GenerateSinglePrimitive(primitiveType, index, 16);
            }
            
            UpdateStatistics();
            LogResults();
        }
        
        [ContextMenu("Export Quality Report")]
        public void ExportQualityReport()
        {
            string report = GenerateDetailedReport();
            string fileName = $"primitive_quality_report_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
            string filePath = System.IO.Path.Combine(Application.dataPath, fileName);
            
            System.IO.File.WriteAllText(filePath, report);
            Debug.Log($"📄 Quality report exported to: {filePath}");
        }
        
        /// <summary>
        /// 詳細レポートを生成
        /// </summary>
        private string GenerateDetailedReport()
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== PRIMITIVE QUALITY TEST DETAILED REPORT ===");
            report.AppendLine($"Generated on: {System.DateTime.Now}");
            report.AppendLine($"Unity Version: {Application.unityVersion}");
            report.AppendLine();
            
            report.AppendLine("Test Configuration:");
            report.AppendLine($"  Quality Settings: Subdivision Level {qualitySettings.subdivisionLevel}");
            report.AppendLine($"  Base Scale: {baseScale}");
            report.AppendLine($"  Type Specific Scaling: {useTypeSpecificScaling}");
            report.AppendLine();
            
            report.AppendLine("Overall Statistics:");
            report.AppendLine($"  Total Generated: {totalGenerated}/16");
            report.AppendLine($"  High Quality: {highQualityCount}");
            report.AppendLine($"  Medium Quality: {mediumQualityCount}");
            report.AppendLine($"  Low Quality: {lowQualityCount}");
            report.AppendLine($"  Failed: {failedCount}");
            report.AppendLine($"  Average Score: {averageQualityScore:F3}");
            report.AppendLine($"  Success Rate: {(totalGenerated > 0 ? (float)(totalGenerated - failedCount) / totalGenerated * 100f : 0f):F1}%");
            report.AppendLine();
            
            report.AppendLine("Detailed Results:");
            foreach (var kvp in qualityReports.OrderBy(x => x.Key.ToString()))
            {
                var type = kvp.Key;
                var qualityReport = kvp.Value;
                
                report.AppendLine($"  {type}:");
                report.AppendLine($"    Status: {(qualityReport.passedValidation ? "PASS" : "FAIL")}");
                report.AppendLine($"    Overall Score: {qualityReport.overallScore:F3}");
                
                if (qualityReport.categoryScores.Count > 0)
                {
                    report.AppendLine("    Category Scores:");
                    foreach (var categoryScore in qualityReport.categoryScores)
                    {
                        report.AppendLine($"      {categoryScore.Key}: {categoryScore.Value:F3}");
                    }
                }
                
                if (qualityReport.issues.Count > 0)
                {
                    report.AppendLine("    Issues:");
                    foreach (var issue in qualityReport.issues)
                    {
                        report.AppendLine($"      - {issue}");
                    }
                }
                
                if (qualityReport.recommendations.Count > 0)
                {
                    report.AppendLine("    Recommendations:");
                    foreach (var recommendation in qualityReport.recommendations)
                    {
                        report.AppendLine($"      - {recommendation}");
                    }
                }
                
                report.AppendLine();
            }
            
            return report.ToString();
        }
        #endregion
    }
    
    /// <summary>
    /// ビルボード効果（常にカメラの方を向く）
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        void Update()
        {
            if (Camera.main != null)
            {
                transform.LookAt(Camera.main.transform);
                transform.Rotate(0, 180, 0); // テキストが反転しないように調整
            }
        }
    }
}