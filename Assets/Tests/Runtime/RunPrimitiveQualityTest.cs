using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// プリミティブ品質テスト実行用スクリプト
    /// エディタまたは実行時に16種類全てのプリミティブ品質をテスト
    /// </summary>
    public class RunPrimitiveQualityTest : MonoBehaviour
    {
        [Header("テスト設定")]
        [SerializeField] private bool runOnStart = false;
        [SerializeField] private bool saveReportToFile = true;
        [SerializeField] private bool logDetailedResults = true;
        
        [Header("テスト結果")]
        [SerializeField] private bool testCompleted = false;
        [SerializeField] private bool allPrimitivesGenerated = false;
        [SerializeField] private bool allPrimitivesHighQuality = false;
        [SerializeField] private float averageQualityScore = 0f;
        [SerializeField] private int successfulGenerations = 0;
        [SerializeField] private int totalPrimitives = 16;

        private PrimitiveQualityTestRunner.TestResults lastTestResults;

        void Start()
        {
            if (runOnStart)
            {
                RunTest();
            }
        }

        /// <summary>
        /// 品質テストを実行
        /// </summary>
        [ContextMenu("Run Quality Test")]
        public void RunTest()
        {
            Debug.Log("🚀 Starting primitive quality test...");
            
            try
            {
                // 包括的品質テストを実行
                lastTestResults = PrimitiveQualityTestRunner.RunComprehensiveQualityTest();
                
                // 結果を更新
                UpdateTestResults();
                
                // レポートを保存
                if (saveReportToFile)
                {
                    PrimitiveQualityTestRunner.SaveTestReport(lastTestResults);
                }
                
                // 成功判定
                if (lastTestResults.allPrimitivesGenerated && lastTestResults.allPrimitivesHighQuality)
                {
                    Debug.Log("🎉 SUCCESS: All 16 primitive types are generating with high quality!");
                    LogSuccessMessage();
                }
                else
                {
                    Debug.LogWarning("⚠️ Some primitives need improvement");
                    LogImprovementNeeded();
                }
                
                testCompleted = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Test execution failed: {e.Message}");
                testCompleted = false;
            }
        }

        /// <summary>
        /// テスト結果を更新
        /// </summary>
        private void UpdateTestResults()
        {
            if (lastTestResults != null)
            {
                allPrimitivesGenerated = lastTestResults.allPrimitivesGenerated;
                allPrimitivesHighQuality = lastTestResults.allPrimitivesHighQuality;
                averageQualityScore = lastTestResults.averageQualityScore;
                successfulGenerations = lastTestResults.successfulGenerations;
                totalPrimitives = lastTestResults.totalPrimitives;
            }
        }

        /// <summary>
        /// 成功メッセージをログ出力
        /// </summary>
        private void LogSuccessMessage()
        {
            Debug.Log("✅ TASK COMPLETED SUCCESSFULLY!");
            Debug.Log("📋 Summary:");
            Debug.Log($"   • All 16 primitive types generated: ✅");
            Debug.Log($"   • All primitives high quality (≥0.8): ✅");
            Debug.Log($"   • Average quality score: {averageQualityScore:F2}");
            Debug.Log($"   • Success rate: 100%");
            Debug.Log("");
            Debug.Log("🎯 The task '16種類全てのプリミティブが高品質で生成される' has been completed!");
        }

        /// <summary>
        /// 改善が必要な場合のメッセージをログ出力
        /// </summary>
        private void LogImprovementNeeded()
        {
            Debug.LogWarning("📋 Test Results Summary:");
            Debug.LogWarning($"   • Primitives generated: {successfulGenerations}/{totalPrimitives} ({(float)successfulGenerations/totalPrimitives*100:F1}%)");
            Debug.LogWarning($"   • High quality primitives: {lastTestResults.highQualityPrimitives}/{totalPrimitives} ({(float)lastTestResults.highQualityPrimitives/totalPrimitives*100:F1}%)");
            Debug.LogWarning($"   • Average quality score: {averageQualityScore:F2}");
            
            if (lastTestResults.failedPrimitives.Count > 0)
            {
                Debug.LogError($"   • Failed primitives: {string.Join(", ", lastTestResults.failedPrimitives)}");
            }
            
            if (lastTestResults.lowQualityPrimitives.Count > 0)
            {
                Debug.LogWarning($"   • Low quality primitives: {string.Join(", ", lastTestResults.lowQualityPrimitives)}");
            }
            
            // 改善推奨事項を表示
            var recommendations = PrimitiveQualityTestRunner.GenerateImprovementRecommendations(lastTestResults);
            if (recommendations.Count > 0)
            {
                Debug.Log("💡 Improvement Recommendations:");
                foreach (var recommendation in recommendations)
                {
                    Debug.Log($"   • {recommendation}");
                }
            }
        }

        /// <summary>
        /// 詳細テスト結果を表示
        /// </summary>
        [ContextMenu("Show Detailed Results")]
        public void ShowDetailedResults()
        {
            if (lastTestResults == null)
            {
                Debug.LogWarning("No test results available. Run the test first.");
                return;
            }

            Debug.Log("📊 === DETAILED TEST RESULTS ===");
            
            foreach (var kvp in lastTestResults.primitiveScores)
            {
                string primitiveType = kvp.Key;
                float score = kvp.Value;
                
                string status;
                if (score >= 0.9f) status = "🟢 EXCELLENT";
                else if (score >= 0.8f) status = "🟡 GOOD";
                else if (score >= 0.6f) status = "🟠 FAIR";
                else status = "🔴 POOR";
                
                Debug.Log($"{primitiveType}: {status} (Score: {score:F3})");
            }
        }

        /// <summary>
        /// 失敗したプリミティブのみ再テスト
        /// </summary>
        [ContextMenu("Retest Failed Primitives")]
        public void RetestFailedPrimitives()
        {
            if (lastTestResults == null)
            {
                Debug.LogWarning("No previous test results. Run full test first.");
                return;
            }

            var failedTypes = new System.Collections.Generic.List<string>();
            failedTypes.AddRange(lastTestResults.failedPrimitives);
            failedTypes.AddRange(lastTestResults.lowQualityPrimitives);

            if (failedTypes.Count == 0)
            {
                Debug.Log("✅ No failed primitives to retest!");
                return;
            }

            Debug.Log($"🔄 Retesting {failedTypes.Count} failed/low-quality primitives...");
            
            // 完全なテストを再実行（簡易実装）
            RunTest();
        }

        /// <summary>
        /// テスト統計を表示
        /// </summary>
        [ContextMenu("Show Test Statistics")]
        public void ShowTestStatistics()
        {
            if (lastTestResults == null)
            {
                Debug.LogWarning("No test results available.");
                return;
            }

            Debug.Log("📈 === TEST STATISTICS ===");
            Debug.Log($"Total Primitives: {lastTestResults.totalPrimitives}");
            Debug.Log($"Generated Successfully: {lastTestResults.successfulGenerations}");
            Debug.Log($"High Quality (≥0.8): {lastTestResults.highQualityPrimitives}");
            Debug.Log($"Medium Quality (0.6-0.8): {lastTestResults.successfulGenerations - lastTestResults.highQualityPrimitives - lastTestResults.lowQualityPrimitives.Count}");
            Debug.Log($"Low Quality (<0.6): {lastTestResults.lowQualityPrimitives.Count}");
            Debug.Log($"Failed: {lastTestResults.failedPrimitives.Count}");
            Debug.Log($"Average Score: {lastTestResults.averageQualityScore:F3}");
            Debug.Log($"Success Rate: {(float)lastTestResults.successfulGenerations/lastTestResults.totalPrimitives*100:F1}%");
            Debug.Log($"High Quality Rate: {(float)lastTestResults.highQualityPrimitives/lastTestResults.totalPrimitives*100:F1}%");
        }

        /// <summary>
        /// 品質基準を満たしているかチェック
        /// </summary>
        public bool MeetsQualityStandards()
        {
            return testCompleted && allPrimitivesGenerated && allPrimitivesHighQuality;
        }

        /// <summary>
        /// テスト結果のサマリーを取得
        /// </summary>
        public string GetTestSummary()
        {
            if (!testCompleted)
            {
                return "Test not completed";
            }

            return $"Generated: {successfulGenerations}/{totalPrimitives}, " +
                   $"High Quality: {(lastTestResults?.highQualityPrimitives ?? 0)}/{totalPrimitives}, " +
                   $"Avg Score: {averageQualityScore:F2}";
        }

        /// <summary>
        /// エディタでの情報表示
        /// </summary>
        void OnValidate()
        {
            // エディタでの値制限
            totalPrimitives = 16; // 固定値
        }

        /// <summary>
        /// Gizmoでテスト状態を表示
        /// </summary>
        void OnDrawGizmos()
        {
            if (!testCompleted) return;

            // テスト結果に応じて色を変更
            if (MeetsQualityStandards())
            {
                Gizmos.color = Color.green;
            }
            else if (allPrimitivesGenerated)
            {
                Gizmos.color = Color.yellow;
            }
            else
            {
                Gizmos.color = Color.red;
            }

            // 簡単な視覚的インジケーター
            Gizmos.DrawWireSphere(transform.position, 5f);
        }
    }
}