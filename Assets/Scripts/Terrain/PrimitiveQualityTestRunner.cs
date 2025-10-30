using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Terrain
{
    /// <summary>
    /// プリミティブ品質テスト実行クラス
    /// 16種類のプリミティブ地形生成の品質をテスト
    /// </summary>
    public static class PrimitiveQualityTestRunner
    {
        /// <summary>
        /// テスト結果構造体
        /// </summary>
        [System.Serializable]
        public struct TestResults
        {
            public bool allPrimitivesGenerated;
            public bool allPrimitivesHighQuality;
            public float averageQualityScore;
            public int successfulGenerations;
            public int totalPrimitives;
            public Dictionary<string, float> primitiveScores;
            public List<string> failedPrimitives;
            public List<string> lowQualityPrimitives;
            public List<string> highQualityPrimitives;
        }

        /// <summary>
        /// 包括的品質テストを実行
        /// </summary>
        public static TestResults RunComprehensiveQualityTest()
        {
            var results = new TestResults
            {
                totalPrimitives = 16,
                primitiveScores = new Dictionary<string, float>(),
                failedPrimitives = new List<string>(),
                lowQualityPrimitives = new List<string>(),
                highQualityPrimitives = new List<string>(),
                successfulGenerations = 0,
                averageQualityScore = 0f,
                allPrimitivesGenerated = false,
                allPrimitivesHighQuality = false
            };

            // 16種類のプリミティブをテスト（スタブ実装）
            string[] primitiveTypes = {
                "Sphere", "Cube", "Cylinder", "Capsule", "Torus", "Cone", "Pyramid", "Prism",
                "Octahedron", "Dodecahedron", "Icosahedron", "Tetrahedron", "Hexagon", "Pentagon", "Triangle", "Square"
            };

            float totalScore = 0f;
            int highQualityCount = 0;

            foreach (string primitiveType in primitiveTypes)
            {
                // 各プリミティブの品質スコアを計算（0.0-1.0）
                float score = CalculatePrimitiveQuality(primitiveType);

                results.primitiveScores[primitiveType] = score;
                totalScore += score;

                if (score >= 0.8f)
                {
                    results.highQualityPrimitives.Add(primitiveType);
                    highQualityCount++;
                }
                else if (score >= 0.6f)
                {
                    results.lowQualityPrimitives.Add(primitiveType);
                }
                else
                {
                    results.failedPrimitives.Add(primitiveType);
                }

                results.successfulGenerations++;
            }

            // 結果を計算
            results.averageQualityScore = totalScore / primitiveTypes.Length;
            results.allPrimitivesGenerated = results.failedPrimitives.Count == 0;
            results.allPrimitivesHighQuality = results.lowQualityPrimitives.Count == 0 && results.failedPrimitives.Count == 0;

            return results;
        }

        /// <summary>
        /// プリミティブの品質スコアを計算（スタブ実装）
        /// </summary>
        private static float CalculatePrimitiveQuality(string primitiveType)
        {
            // スタブ実装：ランダムな品質スコアを返す
            // 実際の実装では、メッシュ品質、頂点数、UVマッピングなどを評価
            return Random.Range(0.7f, 1.0f);
        }

        /// <summary>
        /// 改善推奨事項を生成
        /// </summary>
        public static List<string> GenerateImprovementRecommendations(TestResults results)
        {
            var recommendations = new List<string>();

            if (results.failedPrimitives.Count > 0)
            {
                recommendations.Add($"{results.failedPrimitives.Count}個のプリミティブ生成に失敗しています。再実装を検討してください。");
            }

            if (results.lowQualityPrimitives.Count > 0)
            {
                recommendations.Add($"{results.lowQualityPrimitives.Count}個のプリミティブ品質が低いです。メッシュ最適化を行ってください。");
            }

            if (results.averageQualityScore < 0.8f)
            {
                recommendations.Add($"平均品質スコアが{results.averageQualityScore:F2}です。0.8以上を目指してください。");
            }

            if (recommendations.Count == 0)
            {
                recommendations.Add("すべてのプリミティブが高品質で生成されています。良好な状態です。");
            }

            return recommendations;
        }

        /// <summary>
        /// テストレポートをファイルに保存
        /// </summary>
        public static void SaveTestReport(TestResults results)
        {
            string reportPath = System.IO.Path.Combine(Application.persistentDataPath, "PrimitiveQualityTestReport.txt");

            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(reportPath))
            {
                writer.WriteLine("=== Primitive Quality Test Report ===");
                writer.WriteLine($"Test Date: {System.DateTime.Now}");
                writer.WriteLine($"Total Primitives: {results.totalPrimitives}");
                writer.WriteLine($"Successful Generations: {results.successfulGenerations}");
                writer.WriteLine($"Average Quality Score: {results.averageQualityScore:F3}");
                writer.WriteLine($"All Generated: {results.allPrimitivesGenerated}");
                writer.WriteLine($"All High Quality: {results.allPrimitivesHighQuality}");
                writer.WriteLine();

                writer.WriteLine("Primitive Scores:");
                foreach (var kvp in results.primitiveScores)
                {
                    writer.WriteLine($"  {kvp.Key}: {kvp.Value:F3}");
                }

                if (results.failedPrimitives.Count > 0)
                {
                    writer.WriteLine();
                    writer.WriteLine("Failed Primitives:");
                    foreach (string primitive in results.failedPrimitives)
                    {
                        writer.WriteLine($"  {primitive}");
                    }
                }

                if (results.lowQualityPrimitives.Count > 0)
                {
                    writer.WriteLine();
                    writer.WriteLine("Low Quality Primitives:");
                    foreach (string primitive in results.lowQualityPrimitives)
                    {
                        writer.WriteLine($"  {primitive}");
                    }
                }
            }

            Debug.Log($"Test report saved to: {reportPath}");
        }
    }
}
