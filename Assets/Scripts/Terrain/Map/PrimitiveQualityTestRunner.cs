using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Generation
{
    /// <summary>
    /// プリミティブ品質テスト実行システム
    /// 16種類全てのプリミティブの高品質生成を検証・保証
    /// </summary>
    [System.Serializable]
    public class PrimitiveQualityTestRunner
    {
        #region テスト結果データ
        [System.Serializable]
        public class TestResults
        {
            public bool allPrimitivesGenerated;
            public bool allPrimitivesHighQuality;
            public int totalPrimitives;
            public int successfulGenerations;
            public int highQualityPrimitives;
            public float averageQualityScore;
            public List<string> failedPrimitives;
            public List<string> lowQualityPrimitives;
            public Dictionary<string, float> primitiveScores;
            
            public TestResults()
            {
                failedPrimitives = new List<string>();
                lowQualityPrimitives = new List<string>();
                primitiveScores = new Dictionary<string, float>();
            }
        }
        #endregion

        /// <summary>
        /// 包括的品質テストを実行
        /// </summary>
        public static TestResults RunComprehensiveQualityTest()
        {
            Debug.Log("🚀 Starting comprehensive primitive quality test...");
            
            var results = new TestResults();
            var allTypes = System.Enum.GetValues(typeof(PrimitiveTerrainGenerator.PrimitiveType))
                                .Cast<PrimitiveTerrainGenerator.PrimitiveType>()
                                .ToArray();
            
            results.totalPrimitives = allTypes.Length;
            
            // 各プリミティブタイプをテスト
            foreach (var primitiveType in allTypes)
            {
                var testResult = TestSinglePrimitive(primitiveType);
                
                if (testResult.generated)
                {
                    results.successfulGenerations++;
                    results.primitiveScores[primitiveType.ToString()] = testResult.qualityScore;
                    
                    if (testResult.qualityScore >= 0.8f)
                    {
                        results.highQualityPrimitives++;
                    }
                    else
                    {
                        results.lowQualityPrimitives.Add(primitiveType.ToString());
                    }
                }
                else
                {
                    results.failedPrimitives.Add(primitiveType.ToString());
                }
            }
            
            // 結果を計算
            results.allPrimitivesGenerated = results.successfulGenerations == results.totalPrimitives;
            results.allPrimitivesHighQuality = results.highQualityPrimitives == results.totalPrimitives;
            results.averageQualityScore = results.primitiveScores.Values.Count > 0 ? 
                results.primitiveScores.Values.Average() : 0f;
            
            // 結果をログ出力
            LogTestResults(results);
            
            return results;
        }

        /// <summary>
        /// 単一プリミティブのテスト結果
        /// </summary>
        private struct SinglePrimitiveTestResult
        {
            public bool generated;
            public float qualityScore;
            public List<string> issues;
        }

        /// <summary>
        /// 単一プリミティブをテスト
        /// </summary>
        private static SinglePrimitiveTestResult TestSinglePrimitive(PrimitiveTerrainGenerator.PrimitiveType primitiveType)
        {
            var result = new SinglePrimitiveTestResult
            {
                generated = false,
                qualityScore = 0f,
                issues = new List<string>()
            };

            try
            {
                Debug.Log($"Testing {primitiveType}...");
                
                // テスト用の位置とスケール
                Vector3 testPosition = Vector3.zero;
                Vector3 testScale = PrimitiveTerrainGenerator.GetDefaultScale(primitiveType);
                
                // 高品質設定でプリミティブを生成
                var qualitySettings = HighQualityPrimitiveGenerator.QualitySettings.High;
                
                GameObject primitiveObject = HighQualityPrimitiveGenerator.GenerateHighQualityPrimitive(
                    primitiveType,
                    testPosition,
                    testScale,
                    qualitySettings
                );

                if (primitiveObject != null)
                {
                    result.generated = true;
                    
                    // 品質を検証
                    var qualityReport = PrimitiveQualityValidator.ValidatePrimitiveQuality(
                        primitiveObject,
                        primitiveType,
                        PrimitiveQualityValidator.QualityStandards.High
                    );
                    
                    result.qualityScore = qualityReport.overallScore;
                    result.issues = qualityReport.issues;
                    
                    // 追加の品質チェック
                    PerformAdditionalQualityChecks(primitiveObject, primitiveType, result);
                    
                    // テスト用オブジェクトを削除
                    Object.DestroyImmediate(primitiveObject);
                    
                    Debug.Log($"✅ {primitiveType} - Quality: {result.qualityScore:F2}");
                }
                else
                {
                    result.issues.Add("Failed to generate primitive");
                    Debug.LogError($"❌ {primitiveType} - Generation failed");
                }
            }
            catch (System.Exception e)
            {
                result.issues.Add($"Exception: {e.Message}");
                Debug.LogError($"❌ {primitiveType} - Exception: {e.Message}");
            }

            return result;
        }

        /// <summary>
        /// 追加の品質チェックを実行
        /// </summary>
        private static void PerformAdditionalQualityChecks(GameObject primitiveObject, PrimitiveTerrainGenerator.PrimitiveType primitiveType, SinglePrimitiveTestResult result)
        {
            // メッシュの基本チェック
            var meshFilter = primitiveObject.GetComponent<MeshFilter>();
            if (meshFilter?.sharedMesh != null)
            {
                var mesh = meshFilter.sharedMesh;
                
                // 頂点数チェック
                if (mesh.vertexCount < 8)
                {
                    result.issues.Add($"Too few vertices: {mesh.vertexCount}");
                    result.qualityScore *= 0.8f;
                }
                
                // 三角形数チェック
                int triangleCount = mesh.triangles.Length / 3;
                if (triangleCount < 4)
                {
                    result.issues.Add($"Too few triangles: {triangleCount}");
                    result.qualityScore *= 0.8f;
                }
                
                // 法線チェック
                if (mesh.normals == null || mesh.normals.Length == 0)
                {
                    result.issues.Add("Missing normals");
                    result.qualityScore *= 0.9f;
                }
                
                // 境界ボックスチェック
                if (mesh.bounds.size.magnitude < 1f)
                {
                    result.issues.Add("Bounds too small");
                    result.qualityScore *= 0.9f;
                }
            }
            else
            {
                result.issues.Add("Missing mesh");
                result.qualityScore = 0f;
            }
            
            // コンポーネントチェック
            var renderer = primitiveObject.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                result.issues.Add("Missing MeshRenderer");
                result.qualityScore *= 0.9f;
            }
            
            // プリミティブ固有のチェック
            PerformTypeSpecificChecks(primitiveObject, primitiveType, result);
        }

        /// <summary>
        /// プリミティブタイプ固有のチェック
        /// </summary>
        private static void PerformTypeSpecificChecks(GameObject primitiveObject, PrimitiveTerrainGenerator.PrimitiveType primitiveType, SinglePrimitiveTestResult result)
        {
            var meshFilter = primitiveObject.GetComponent<MeshFilter>();
            if (meshFilter?.sharedMesh == null) return;
            
            var mesh = meshFilter.sharedMesh;
            var bounds = mesh.bounds;
            
            switch (primitiveType)
            {
                case PrimitiveTerrainGenerator.PrimitiveType.Sphere:
                    // 球体は各軸がほぼ等しいはず
                    CheckSphericalShape(bounds, result);
                    break;
                    
                case PrimitiveTerrainGenerator.PrimitiveType.Cube:
                    // 立方体は各軸がほぼ等しいはず
                    CheckCubicalShape(bounds, result);
                    break;
                    
                case PrimitiveTerrainGenerator.PrimitiveType.Cylinder:
                    // 円柱は2つの軸が等しく、1つが異なるはず
                    CheckCylindricalShape(bounds, result);
                    break;
                    
                case PrimitiveTerrainGenerator.PrimitiveType.Pyramid:
                    // ピラミッドは底面が正方形で高さがあるはず
                    CheckPyramidalShape(bounds, result);
                    break;
                    
                case PrimitiveTerrainGenerator.PrimitiveType.Monolith:
                    // モノリスは縦長であるはず
                    CheckMonolithShape(bounds, result);
                    break;
                    
                case PrimitiveTerrainGenerator.PrimitiveType.Mesa:
                    // メサは平たく広いはず
                    CheckMesaShape(bounds, result);
                    break;
                    
                case PrimitiveTerrainGenerator.PrimitiveType.Spire:
                    // 尖塔は非常に高いはず
                    CheckSpireShape(bounds, result);
                    break;
            }
        }

        /// <summary>
        /// 球体形状をチェック
        /// </summary>
        private static void CheckSphericalShape(Bounds bounds, SinglePrimitiveTestResult result)
        {
            var size = bounds.size;
            float maxDiff = Mathf.Max(
                Mathf.Abs(size.x - size.y),
                Mathf.Abs(size.y - size.z),
                Mathf.Abs(size.z - size.x)
            );
            
            if (maxDiff > size.magnitude * 0.2f)
            {
                result.issues.Add("Poor spherical proportions");
                result.qualityScore *= 0.9f;
            }
        }

        /// <summary>
        /// 立方体形状をチェック
        /// </summary>
        private static void CheckCubicalShape(Bounds bounds, SinglePrimitiveTestResult result)
        {
            var size = bounds.size;
            float maxDiff = Mathf.Max(
                Mathf.Abs(size.x - size.y),
                Mathf.Abs(size.y - size.z),
                Mathf.Abs(size.z - size.x)
            );
            
            if (maxDiff > size.magnitude * 0.15f)
            {
                result.issues.Add("Poor cubical proportions");
                result.qualityScore *= 0.9f;
            }
        }

        /// <summary>
        /// 円柱形状をチェック
        /// </summary>
        private static void CheckCylindricalShape(Bounds bounds, SinglePrimitiveTestResult result)
        {
            var size = bounds.size;
            
            // XとZが等しく、Yが異なるはず
            if (Mathf.Abs(size.x - size.z) > size.magnitude * 0.1f)
            {
                result.issues.Add("Poor cylindrical base proportions");
                result.qualityScore *= 0.9f;
            }
        }

        /// <summary>
        /// ピラミッド形状をチェック
        /// </summary>
        private static void CheckPyramidalShape(Bounds bounds, SinglePrimitiveTestResult result)
        {
            var size = bounds.size;
            
            // 底面（XZ）が正方形で、高さ（Y）があるはず
            if (Mathf.Abs(size.x - size.z) > size.magnitude * 0.1f)
            {
                result.issues.Add("Poor pyramidal base proportions");
                result.qualityScore *= 0.9f;
            }
            
            if (size.y < size.x * 0.5f)
            {
                result.issues.Add("Pyramid too flat");
                result.qualityScore *= 0.9f;
            }
        }

        /// <summary>
        /// モノリス形状をチェック
        /// </summary>
        private static void CheckMonolithShape(Bounds bounds, SinglePrimitiveTestResult result)
        {
            var size = bounds.size;
            
            // 縦長（Y軸が最大）であるはず
            if (size.y < Mathf.Max(size.x, size.z) * 1.5f)
            {
                result.issues.Add("Monolith not tall enough");
                result.qualityScore *= 0.9f;
            }
        }

        /// <summary>
        /// メサ形状をチェック
        /// </summary>
        private static void CheckMesaShape(Bounds bounds, SinglePrimitiveTestResult result)
        {
            var size = bounds.size;
            
            // 平たく広い（Y軸が最小）はず
            if (size.y > Mathf.Min(size.x, size.z) * 0.5f)
            {
                result.issues.Add("Mesa too tall");
                result.qualityScore *= 0.9f;
            }
        }

        /// <summary>
        /// 尖塔形状をチェック
        /// </summary>
        private static void CheckSpireShape(Bounds bounds, SinglePrimitiveTestResult result)
        {
            var size = bounds.size;
            
            // 非常に高い（Y軸が他の軸の2倍以上）はず
            if (size.y < Mathf.Max(size.x, size.z) * 2f)
            {
                result.issues.Add("Spire not tall enough");
                result.qualityScore *= 0.9f;
            }
        }

        /// <summary>
        /// テスト結果をログ出力
        /// </summary>
        private static void LogTestResults(TestResults results)
        {
            Debug.Log("📊 === COMPREHENSIVE PRIMITIVE QUALITY TEST RESULTS ===");
            Debug.Log($"Total Primitives: {results.totalPrimitives}");
            Debug.Log($"Successful Generations: {results.successfulGenerations}/{results.totalPrimitives} ({(float)results.successfulGenerations/results.totalPrimitives*100:F1}%)");
            Debug.Log($"High Quality Primitives: {results.highQualityPrimitives}/{results.totalPrimitives} ({(float)results.highQualityPrimitives/results.totalPrimitives*100:F1}%)");
            Debug.Log($"Average Quality Score: {results.averageQualityScore:F3}");
            
            if (results.allPrimitivesGenerated && results.allPrimitivesHighQuality)
            {
                Debug.Log("🎉 SUCCESS: All 16 primitive types are generating with high quality!");
            }
            else
            {
                if (results.failedPrimitives.Count > 0)
                {
                    Debug.LogWarning($"❌ Failed Primitives ({results.failedPrimitives.Count}): {string.Join(", ", results.failedPrimitives)}");
                }
                
                if (results.lowQualityPrimitives.Count > 0)
                {
                    Debug.LogWarning($"⚠️ Low Quality Primitives ({results.lowQualityPrimitives.Count}): {string.Join(", ", results.lowQualityPrimitives)}");
                }
            }
            
            // 詳細スコア
            Debug.Log("\n📋 Individual Primitive Scores:");
            foreach (var kvp in results.primitiveScores.OrderBy(x => x.Key))
            {
                string status = kvp.Value >= 0.8f ? "✅" : kvp.Value >= 0.6f ? "⚠️" : "❌";
                Debug.Log($"  {kvp.Key}: {status} {kvp.Value:F3}");
            }
        }

        /// <summary>
        /// 品質改善の推奨事項を生成
        /// </summary>
        public static List<string> GenerateImprovementRecommendations(TestResults results)
        {
            var recommendations = new List<string>();
            
            if (!results.allPrimitivesGenerated)
            {
                recommendations.Add("Fix primitive generation failures - check ProBuilder dependencies and mesh generation logic");
            }
            
            if (results.averageQualityScore < 0.8f)
            {
                recommendations.Add("Increase subdivision levels or improve deformation algorithms for better quality");
            }
            
            if (results.lowQualityPrimitives.Count > 0)
            {
                recommendations.Add($"Focus on improving these specific primitives: {string.Join(", ", results.lowQualityPrimitives)}");
            }
            
            if (results.failedPrimitives.Count > 0)
            {
                recommendations.Add("Implement fallback generation methods for failed primitive types");
            }
            
            return recommendations;
        }

        /// <summary>
        /// 品質テストレポートをファイルに保存
        /// </summary>
        public static void SaveTestReport(TestResults results, string filePath = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = $"Assets/primitive_quality_test_report_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
            }
            
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== PRIMITIVE QUALITY TEST REPORT ===");
            report.AppendLine($"Generated: {System.DateTime.Now}");
            report.AppendLine($"Unity Version: {Application.unityVersion}");
            report.AppendLine();
            
            report.AppendLine("SUMMARY:");
            report.AppendLine($"  Total Primitives: {results.totalPrimitives}");
            report.AppendLine($"  Successful Generations: {results.successfulGenerations} ({(float)results.successfulGenerations/results.totalPrimitives*100:F1}%)");
            report.AppendLine($"  High Quality: {results.highQualityPrimitives} ({(float)results.highQualityPrimitives/results.totalPrimitives*100:F1}%)");
            report.AppendLine($"  Average Score: {results.averageQualityScore:F3}");
            report.AppendLine($"  All Generated: {results.allPrimitivesGenerated}");
            report.AppendLine($"  All High Quality: {results.allPrimitivesHighQuality}");
            report.AppendLine();
            
            if (results.failedPrimitives.Count > 0)
            {
                report.AppendLine("FAILED PRIMITIVES:");
                foreach (var failed in results.failedPrimitives)
                {
                    report.AppendLine($"  - {failed}");
                }
                report.AppendLine();
            }
            
            if (results.lowQualityPrimitives.Count > 0)
            {
                report.AppendLine("LOW QUALITY PRIMITIVES:");
                foreach (var lowQuality in results.lowQualityPrimitives)
                {
                    report.AppendLine($"  - {lowQuality}");
                }
                report.AppendLine();
            }
            
            report.AppendLine("DETAILED SCORES:");
            foreach (var kvp in results.primitiveScores.OrderBy(x => x.Key))
            {
                report.AppendLine($"  {kvp.Key}: {kvp.Value:F3}");
            }
            report.AppendLine();
            
            var recommendations = GenerateImprovementRecommendations(results);
            if (recommendations.Count > 0)
            {
                report.AppendLine("RECOMMENDATIONS:");
                foreach (var recommendation in recommendations)
                {
                    report.AppendLine($"  - {recommendation}");
                }
            }
            
            System.IO.File.WriteAllText(filePath, report.ToString());
            Debug.Log($"📄 Test report saved to: {filePath}");
        }
    }
}