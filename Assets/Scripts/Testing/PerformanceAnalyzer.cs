using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace Vastcore.Testing
{
    /// <summary>
    /// パフォーマンス分析ツール
    /// 収集されたパフォーマンスデータの詳細分析と可視化
    /// </summary>
    public class PerformanceAnalyzer : MonoBehaviour
    {
        [Header("分析設定")]
        [SerializeField] private PerformanceTestingSystem performanceSystem;
        [SerializeField] private bool enableRealTimeAnalysis = true;
        [SerializeField] private float analysisInterval = 5f;
        
        [Header("分析パラメータ")]
        [SerializeField] private int trendAnalysisWindow = 30; // データポイント数
        [SerializeField] private float performanceThresholdFPS = 45f;
        [SerializeField] private float memoryLeakThresholdMB = 50f;
        [SerializeField] private float generationTimeThreshold = 2f;
        
        [Header("レポート設定")]
        [SerializeField] private bool generateDetailedReports = true;
        [SerializeField] private bool includeRecommendations = true;
        [SerializeField] private string reportDirectory = "PerformanceReports";
        
        // 分析結果
        private PerformanceAnalysisResult currentAnalysis;
        private List<PerformanceIssue> detectedIssues = new List<PerformanceIssue>();
        private Dictionary<string, TrendData> performanceTrends = new Dictionary<string, TrendData>();
        
        void Start()
        {
            if (performanceSystem == null)
            {
                performanceSystem = FindObjectOfType<PerformanceTestingSystem>();
            }
            
            if (performanceSystem == null)
            {
                Debug.LogError("PerformanceTestingSystem not found!");
                return;
            }
            
            InitializeAnalyzer();
            
            if (enableRealTimeAnalysis)
            {
                InvokeRepeating(nameof(PerformRealTimeAnalysis), analysisInterval, analysisInterval);
            }
        }
        
        /// <summary>
        /// アナライザーの初期化
        /// </summary>
        private void InitializeAnalyzer()
        {
            currentAnalysis = new PerformanceAnalysisResult();
            
            // レポートディレクトリの作成
            if (generateDetailedReports)
            {
                string fullReportPath = Path.Combine(Application.persistentDataPath, reportDirectory);
                if (!Directory.Exists(fullReportPath))
                {
                    Directory.CreateDirectory(fullReportPath);
                }
            }
            
            Debug.Log("Performance Analyzer initialized");
        }
        
        /// <summary>
        /// リアルタイム分析の実行
        /// </summary>
        private void PerformRealTimeAnalysis()
        {
            if (performanceSystem == null)
                return;
            
            var performanceData = performanceSystem.GetPerformanceData();
            var generationData = performanceSystem.GetGenerationTimeData();
            
            if (performanceData.Count == 0)
                return;
            
            // 分析の実行
            AnalyzeFrameRatePerformance(performanceData);
            AnalyzeMemoryUsage(performanceData);
            AnalyzeGenerationTimes(generationData);
            AnalyzePerformanceTrends(performanceData);
            DetectPerformanceIssues(performanceData);
            
            // 分析結果の更新
            UpdateAnalysisResult();
        }
        
        /// <summary>
        /// フレームレートパフォーマンスの分析
        /// </summary>
        private void AnalyzeFrameRatePerformance(List<PerformanceDataPoint> data)
        {
            if (data.Count == 0) return;
            
            var recentData = data.TakeLast(trendAnalysisWindow).ToList();
            
            float avgFrameRate = recentData.Average(d => d.frameRate);
            float minFrameRate = recentData.Min(d => d.frameRate);
            float maxFrameRate = recentData.Max(d => d.frameRate);
            
            // フレームレートの安定性を計算
            float frameRateVariance = CalculateVariance(recentData.Select(d => d.frameRate).ToList());
            float frameRateStability = 1f - (frameRateVariance / (avgFrameRate * avgFrameRate));
            
            // 低フレームレートの頻度
            int lowFrameCount = recentData.Count(d => d.frameRate < performanceThresholdFPS);
            float lowFramePercentage = (float)lowFrameCount / recentData.Count * 100f;
            
            currentAnalysis.frameRateAnalysis = new FrameRateAnalysis
            {
                averageFrameRate = avgFrameRate,
                minFrameRate = minFrameRate,
                maxFrameRate = maxFrameRate,
                frameRateStability = frameRateStability,
                lowFramePercentage = lowFramePercentage
            };
            
            // フレームレート問題の検出
            if (avgFrameRate < performanceThresholdFPS)
            {
                AddPerformanceIssue(PerformanceIssueType.LowFrameRate, 
                    $"Average frame rate below threshold: {avgFrameRate:F1}FPS < {performanceThresholdFPS}FPS",
                    PerformanceIssueSeverity.High);
            }
            
            if (frameRateStability < 0.8f)
            {
                AddPerformanceIssue(PerformanceIssueType.FrameRateInstability,
                    $"Frame rate instability detected: stability {frameRateStability:F2}",
                    PerformanceIssueSeverity.Medium);
            }
        }
        
        /// <summary>
        /// メモリ使用量の分析
        /// </summary>
        private void AnalyzeMemoryUsage(List<PerformanceDataPoint> data)
        {
            if (data.Count == 0) return;
            
            var recentData = data.TakeLast(trendAnalysisWindow).ToList();
            
            float avgMemoryUsage = recentData.Average(d => d.memoryUsageMB);
            float minMemoryUsage = recentData.Min(d => d.memoryUsageMB);
            float maxMemoryUsage = recentData.Max(d => d.memoryUsageMB);
            
            // メモリリークの検出
            float memoryTrend = CalculateLinearTrend(recentData.Select(d => d.memoryUsageMB).ToList());
            bool potentialMemoryLeak = memoryTrend > memoryLeakThresholdMB / trendAnalysisWindow;
            
            // メモリ使用量の変動
            float memoryVariance = CalculateVariance(recentData.Select(d => d.memoryUsageMB).ToList());
            
            currentAnalysis.memoryAnalysis = new MemoryAnalysis
            {
                averageMemoryUsage = avgMemoryUsage,
                minMemoryUsage = minMemoryUsage,
                maxMemoryUsage = maxMemoryUsage,
                memoryTrend = memoryTrend,
                memoryVariance = memoryVariance,
                potentialMemoryLeak = potentialMemoryLeak
            };
            
            // メモリ問題の検出
            if (potentialMemoryLeak)
            {
                AddPerformanceIssue(PerformanceIssueType.MemoryLeak,
                    $"Potential memory leak detected: trend +{memoryTrend:F2}MB per measurement",
                    PerformanceIssueSeverity.High);
            }
            
            if (maxMemoryUsage > 1000f) // 1GB以上
            {
                AddPerformanceIssue(PerformanceIssueType.HighMemoryUsage,
                    $"High memory usage detected: {maxMemoryUsage:F1}MB",
                    PerformanceIssueSeverity.Medium);
            }
        }
        
        /// <summary>
        /// 生成時間の分析
        /// </summary>
        private void AnalyzeGenerationTimes(List<GenerationTimeData> data)
        {
            if (data.Count == 0) return;
            
            var recentData = data.Where(d => Time.time - d.timestamp < analysisInterval * 2f).ToList();
            if (recentData.Count == 0) return;
            
            // タスク別の分析
            var taskGroups = recentData.GroupBy(d => d.taskName);
            var taskAnalyses = new Dictionary<string, GenerationTaskAnalysis>();
            
            foreach (var group in taskGroups)
            {
                var times = group.Select(g => g.generationTime).ToList();
                
                taskAnalyses[group.Key] = new GenerationTaskAnalysis
                {
                    taskName = group.Key,
                    averageTime = times.Average(),
                    minTime = times.Min(),
                    maxTime = times.Max(),
                    sampleCount = times.Count,
                    timeVariance = CalculateVariance(times)
                };
                
                // 生成時間問題の検出
                if (times.Average() > generationTimeThreshold)
                {
                    AddPerformanceIssue(PerformanceIssueType.SlowGeneration,
                        $"Slow generation detected for {group.Key}: {times.Average():F2}s average",
                        PerformanceIssueSeverity.Medium);
                }
            }
            
            currentAnalysis.generationAnalysis = new GenerationAnalysis
            {
                taskAnalyses = taskAnalyses,
                totalGenerationTasks = recentData.Count,
                averageGenerationTime = recentData.Average(d => d.generationTime)
            };
        }
        
        /// <summary>
        /// パフォーマンストレンドの分析
        /// </summary>
        private void AnalyzePerformanceTrends(List<PerformanceDataPoint> data)
        {
            if (data.Count < trendAnalysisWindow) return;
            
            var recentData = data.TakeLast(trendAnalysisWindow).ToList();
            
            // フレームレートトレンド
            var frameRates = recentData.Select(d => d.frameRate).ToList();
            float frameRateTrend = CalculateLinearTrend(frameRates);
            
            performanceTrends["FrameRate"] = new TrendData
            {
                trend = frameRateTrend,
                isImproving = frameRateTrend > 0,
                confidence = CalculateTrendConfidence(frameRates)
            };
            
            // メモリ使用量トレンド
            var memoryUsages = recentData.Select(d => d.memoryUsageMB).ToList();
            float memoryTrend = CalculateLinearTrend(memoryUsages);
            
            performanceTrends["Memory"] = new TrendData
            {
                trend = memoryTrend,
                isImproving = memoryTrend < 0, // メモリは減少が改善
                confidence = CalculateTrendConfidence(memoryUsages)
            };
            
            // フレーム時間トレンド
            var frameTimes = recentData.Select(d => d.frameTime).ToList();
            float frameTimeTrend = CalculateLinearTrend(frameTimes);
            
            performanceTrends["FrameTime"] = new TrendData
            {
                trend = frameTimeTrend,
                isImproving = frameTimeTrend < 0, // フレーム時間は減少が改善
                confidence = CalculateTrendConfidence(frameTimes)
            };
        }
        
        /// <summary>
        /// パフォーマンス問題の検出
        /// </summary>
        private void DetectPerformanceIssues(List<PerformanceDataPoint> data)
        {
            // 古い問題をクリア
            detectedIssues.RemoveAll(issue => Time.time - issue.detectionTime > 60f);
            
            if (data.Count == 0) return;
            
            var recentData = data.TakeLast(10).ToList(); // 最新10データポイント
            
            // 急激なパフォーマンス低下の検出
            if (recentData.Count >= 5)
            {
                var firstHalf = recentData.Take(recentData.Count / 2);
                var secondHalf = recentData.Skip(recentData.Count / 2);
                
                float firstHalfAvgFPS = firstHalf.Average(d => d.frameRate);
                float secondHalfAvgFPS = secondHalf.Average(d => d.frameRate);
                
                if (firstHalfAvgFPS - secondHalfAvgFPS > 10f) // 10FPS以上の低下
                {
                    AddPerformanceIssue(PerformanceIssueType.PerformanceDrop,
                        $"Sudden performance drop detected: {firstHalfAvgFPS:F1}FPS → {secondHalfAvgFPS:F1}FPS",
                        PerformanceIssueSeverity.High);
                }
            }
            
            // メモリスパイクの検出
            var memoryUsages = recentData.Select(d => d.memoryUsageMB).ToList();
            float memoryVariance = CalculateVariance(memoryUsages);
            
            if (memoryVariance > 10000f) // 100MB^2以上の分散
            {
                AddPerformanceIssue(PerformanceIssueType.MemorySpike,
                    $"Memory usage spike detected: variance {Mathf.Sqrt(memoryVariance):F1}MB",
                    PerformanceIssueSeverity.Medium);
            }
        }
        
        /// <summary>
        /// 分析結果の更新
        /// </summary>
        private void UpdateAnalysisResult()
        {
            currentAnalysis.analysisTimestamp = Time.time;
            currentAnalysis.detectedIssues = new List<PerformanceIssue>(detectedIssues);
            currentAnalysis.performanceTrends = new Dictionary<string, TrendData>(performanceTrends);
            
            // 全体的なパフォーマンススコアの計算
            currentAnalysis.overallPerformanceScore = CalculateOverallPerformanceScore();
        }
        
        /// <summary>
        /// 全体的なパフォーマンススコアの計算
        /// </summary>
        private float CalculateOverallPerformanceScore()
        {
            float score = 100f;
            
            // フレームレートスコア
            if (currentAnalysis.frameRateAnalysis != null)
            {
                float frameRateScore = Mathf.Clamp01(currentAnalysis.frameRateAnalysis.averageFrameRate / 60f) * 30f;
                float stabilityScore = currentAnalysis.frameRateAnalysis.frameRateStability * 20f;
                score = frameRateScore + stabilityScore;
            }
            
            // メモリスコア
            if (currentAnalysis.memoryAnalysis != null)
            {
                float memoryScore = 25f;
                if (currentAnalysis.memoryAnalysis.potentialMemoryLeak)
                    memoryScore -= 15f;
                if (currentAnalysis.memoryAnalysis.maxMemoryUsage > 800f)
                    memoryScore -= 10f;
                score += memoryScore;
            }
            
            // 生成時間スコア
            if (currentAnalysis.generationAnalysis != null)
            {
                float generationScore = 25f;
                if (currentAnalysis.generationAnalysis.averageGenerationTime > generationTimeThreshold)
                    generationScore -= 15f;
                score += generationScore;
            }
            
            // 問題による減点
            foreach (var issue in detectedIssues)
            {
                switch (issue.severity)
                {
                    case PerformanceIssueSeverity.High:
                        score -= 10f;
                        break;
                    case PerformanceIssueSeverity.Medium:
                        score -= 5f;
                        break;
                    case PerformanceIssueSeverity.Low:
                        score -= 2f;
                        break;
                }
            }
            
            return Mathf.Clamp(score, 0f, 100f);
        }
        
        /// <summary>
        /// パフォーマンス問題の追加
        /// </summary>
        private void AddPerformanceIssue(PerformanceIssueType type, string description, PerformanceIssueSeverity severity)
        {
            // 重複チェック
            if (detectedIssues.Any(issue => issue.type == type && issue.description == description))
                return;
            
            var newIssue = new PerformanceIssue
            {
                type = type,
                description = description,
                severity = severity,
                detectionTime = Time.time
            };
            
            detectedIssues.Add(newIssue);
            
            // ログ出力
            string severityText = severity.ToString().ToUpper();
            Debug.Log($"[Performance Issue - {severityText}] {description}");
        }
        
        /// <summary>
        /// 詳細レポートの生成
        /// </summary>
        public void GenerateDetailedReport()
        {
            if (!generateDetailedReports || currentAnalysis == null)
                return;
            
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string reportPath = Path.Combine(Application.persistentDataPath, reportDirectory);
            string reportFile = Path.Combine(reportPath, $"performance_analysis_{timestamp}.txt");
            
            var sb = new StringBuilder();
            sb.AppendLine("=== Vastcore Performance Analysis Report ===");
            sb.AppendLine($"Generated: {System.DateTime.Now}");
            sb.AppendLine($"Overall Performance Score: {currentAnalysis.overallPerformanceScore:F1}/100");
            sb.AppendLine();
            
            // フレームレート分析
            if (currentAnalysis.frameRateAnalysis != null)
            {
                var fra = currentAnalysis.frameRateAnalysis;
                sb.AppendLine("=== Frame Rate Analysis ===");
                sb.AppendLine($"Average Frame Rate: {fra.averageFrameRate:F1}FPS");
                sb.AppendLine($"Min Frame Rate: {fra.minFrameRate:F1}FPS");
                sb.AppendLine($"Max Frame Rate: {fra.maxFrameRate:F1}FPS");
                sb.AppendLine($"Frame Rate Stability: {fra.frameRateStability:F2}");
                sb.AppendLine($"Low Frame Rate Percentage: {fra.lowFramePercentage:F1}%");
                sb.AppendLine();
            }
            
            // メモリ分析
            if (currentAnalysis.memoryAnalysis != null)
            {
                var ma = currentAnalysis.memoryAnalysis;
                sb.AppendLine("=== Memory Analysis ===");
                sb.AppendLine($"Average Memory Usage: {ma.averageMemoryUsage:F1}MB");
                sb.AppendLine($"Min Memory Usage: {ma.minMemoryUsage:F1}MB");
                sb.AppendLine($"Max Memory Usage: {ma.maxMemoryUsage:F1}MB");
                sb.AppendLine($"Memory Trend: {ma.memoryTrend:F2}MB per measurement");
                sb.AppendLine($"Potential Memory Leak: {(ma.potentialMemoryLeak ? "Yes" : "No")}");
                sb.AppendLine();
            }
            
            // 生成時間分析
            if (currentAnalysis.generationAnalysis != null)
            {
                var ga = currentAnalysis.generationAnalysis;
                sb.AppendLine("=== Generation Time Analysis ===");
                sb.AppendLine($"Total Generation Tasks: {ga.totalGenerationTasks}");
                sb.AppendLine($"Average Generation Time: {ga.averageGenerationTime:F2}s");
                sb.AppendLine();
                
                foreach (var taskAnalysis in ga.taskAnalyses)
                {
                    var ta = taskAnalysis.Value;
                    sb.AppendLine($"{ta.taskName}:");
                    sb.AppendLine($"  Average: {ta.averageTime:F2}s");
                    sb.AppendLine($"  Min: {ta.minTime:F2}s");
                    sb.AppendLine($"  Max: {ta.maxTime:F2}s");
                    sb.AppendLine($"  Samples: {ta.sampleCount}");
                }
                sb.AppendLine();
            }
            
            // 検出された問題
            if (detectedIssues.Count > 0)
            {
                sb.AppendLine("=== Detected Issues ===");
                foreach (var issue in detectedIssues.OrderByDescending(i => i.severity))
                {
                    sb.AppendLine($"[{issue.severity}] {issue.type}: {issue.description}");
                }
                sb.AppendLine();
            }
            
            // パフォーマンストレンド
            if (performanceTrends.Count > 0)
            {
                sb.AppendLine("=== Performance Trends ===");
                foreach (var trend in performanceTrends)
                {
                    string direction = trend.Value.isImproving ? "Improving" : "Degrading";
                    sb.AppendLine($"{trend.Key}: {direction} (trend: {trend.Value.trend:F3}, confidence: {trend.Value.confidence:F2})");
                }
                sb.AppendLine();
            }
            
            // 推奨事項
            if (includeRecommendations)
            {
                sb.AppendLine("=== Recommendations ===");
                GenerateRecommendations(sb);
            }
            
            File.WriteAllText(reportFile, sb.ToString());
            Debug.Log($"Detailed performance report saved: {reportFile}");
        }
        
        /// <summary>
        /// 推奨事項の生成
        /// </summary>
        private void GenerateRecommendations(StringBuilder sb)
        {
            bool hasRecommendations = false;
            
            // フレームレート関連の推奨事項
            if (currentAnalysis.frameRateAnalysis != null)
            {
                if (currentAnalysis.frameRateAnalysis.averageFrameRate < performanceThresholdFPS)
                {
                    sb.AppendLine("• Consider reducing terrain generation complexity or implementing more aggressive LOD");
                    sb.AppendLine("• Optimize primitive generation algorithms");
                    sb.AppendLine("• Implement frame time budgeting for generation tasks");
                    hasRecommendations = true;
                }
                
                if (currentAnalysis.frameRateAnalysis.frameRateStability < 0.8f)
                {
                    sb.AppendLine("• Implement load balancing for generation tasks");
                    sb.AppendLine("• Consider using coroutines for heavy operations");
                    hasRecommendations = true;
                }
            }
            
            // メモリ関連の推奨事項
            if (currentAnalysis.memoryAnalysis != null)
            {
                if (currentAnalysis.memoryAnalysis.potentialMemoryLeak)
                {
                    sb.AppendLine("• Investigate potential memory leaks in terrain/primitive generation");
                    sb.AppendLine("• Ensure proper cleanup of generated objects");
                    sb.AppendLine("• Implement object pooling for frequently created/destroyed objects");
                    hasRecommendations = true;
                }
                
                if (currentAnalysis.memoryAnalysis.maxMemoryUsage > 800f)
                {
                    sb.AppendLine("• Implement more aggressive memory management");
                    sb.AppendLine("• Reduce terrain tile cache size");
                    sb.AppendLine("• Optimize mesh data structures");
                    hasRecommendations = true;
                }
            }
            
            // 生成時間関連の推奨事項
            if (currentAnalysis.generationAnalysis != null)
            {
                if (currentAnalysis.generationAnalysis.averageGenerationTime > generationTimeThreshold)
                {
                    sb.AppendLine("• Optimize generation algorithms");
                    sb.AppendLine("• Consider pre-computing or caching generation results");
                    sb.AppendLine("• Implement progressive generation");
                    hasRecommendations = true;
                }
            }
            
            if (!hasRecommendations)
            {
                sb.AppendLine("• Performance is within acceptable parameters");
                sb.AppendLine("• Continue monitoring for potential issues");
            }
        }
        
        // ユーティリティメソッド
        private float CalculateVariance(List<float> values)
        {
            if (values.Count == 0) return 0f;
            
            float mean = values.Average();
            float variance = values.Sum(v => (v - mean) * (v - mean)) / values.Count;
            return variance;
        }
        
        private float CalculateLinearTrend(List<float> values)
        {
            if (values.Count < 2) return 0f;
            
            float n = values.Count;
            float sumX = 0f, sumY = 0f, sumXY = 0f, sumX2 = 0f;
            
            for (int i = 0; i < values.Count; i++)
            {
                float x = i;
                float y = values[i];
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
            }
            
            float slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            return slope;
        }
        
        private float CalculateTrendConfidence(List<float> values)
        {
            if (values.Count < 3) return 0f;
            
            float trend = CalculateLinearTrend(values);
            float variance = CalculateVariance(values);
            
            // 簡単な信頼度計算（トレンドの強さ vs データの分散）
            return Mathf.Clamp01(Mathf.Abs(trend) / (Mathf.Sqrt(variance) + 0.001f));
        }
        
        /// <summary>
        /// 現在の分析結果を取得
        /// </summary>
        public PerformanceAnalysisResult GetCurrentAnalysis()
        {
            return currentAnalysis;
        }
        
        /// <summary>
        /// 検出された問題を取得
        /// </summary>
        public List<PerformanceIssue> GetDetectedIssues()
        {
            return new List<PerformanceIssue>(detectedIssues);
        }
        
        // コンテキストメニュー
        [ContextMenu("Perform Analysis")]
        public void PerformAnalysisManual()
        {
            PerformRealTimeAnalysis();
        }
        
        [ContextMenu("Generate Report")]
        public void GenerateReportManual()
        {
            GenerateDetailedReport();
        }
        
        [ContextMenu("Clear Issues")]
        public void ClearIssues()
        {
            detectedIssues.Clear();
            Debug.Log("Performance issues cleared");
        }
    }
    
    // データ構造
    [System.Serializable]
    public class PerformanceAnalysisResult
    {
        public float analysisTimestamp;
        public float overallPerformanceScore;
        public FrameRateAnalysis frameRateAnalysis;
        public MemoryAnalysis memoryAnalysis;
        public GenerationAnalysis generationAnalysis;
        public List<PerformanceIssue> detectedIssues;
        public Dictionary<string, TrendData> performanceTrends;
    }
    
    [System.Serializable]
    public class FrameRateAnalysis
    {
        public float averageFrameRate;
        public float minFrameRate;
        public float maxFrameRate;
        public float frameRateStability;
        public float lowFramePercentage;
    }
    
    [System.Serializable]
    public class MemoryAnalysis
    {
        public float averageMemoryUsage;
        public float minMemoryUsage;
        public float maxMemoryUsage;
        public float memoryTrend;
        public float memoryVariance;
        public bool potentialMemoryLeak;
    }
    
    [System.Serializable]
    public class GenerationAnalysis
    {
        public Dictionary<string, GenerationTaskAnalysis> taskAnalyses;
        public int totalGenerationTasks;
        public float averageGenerationTime;
    }
    
    [System.Serializable]
    public class GenerationTaskAnalysis
    {
        public string taskName;
        public float averageTime;
        public float minTime;
        public float maxTime;
        public int sampleCount;
        public float timeVariance;
    }
    
    [System.Serializable]
    public class PerformanceIssue
    {
        public PerformanceIssueType type;
        public string description;
        public PerformanceIssueSeverity severity;
        public float detectionTime;
    }
    
    [System.Serializable]
    public class TrendData
    {
        public float trend;
        public bool isImproving;
        public float confidence;
    }
    
    public enum PerformanceIssueType
    {
        LowFrameRate,
        FrameRateInstability,
        MemoryLeak,
        HighMemoryUsage,
        MemorySpike,
        SlowGeneration,
        PerformanceDrop
    }
    
    public enum PerformanceIssueSeverity
    {
        Low,
        Medium,
        High
    }
}