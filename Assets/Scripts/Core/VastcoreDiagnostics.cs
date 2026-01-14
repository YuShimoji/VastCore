using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Vastcore.Utilities;

namespace Vastcore
{
    /// <summary>
    /// Vastcore専用の問題診断支援システム
    /// システムの健全性チェック、問題の自動検出、解決策の提案機能
    /// </summary>
    public class VastcoreDiagnostics : MonoBehaviour
    {
        [Header("診断設定")]
        public bool enableAutoDiagnostics = true;
        public bool enableHealthChecks = true;
        public bool enablePerformanceAnalysis = true;
        public float diagnosticInterval = 30f;
        
        [Header("健全性チェック設定")]
        public float memoryWarningThreshold = 512f; // MB
        public float memoryCriticalThreshold = 1024f; // MB
        public float fpsWarningThreshold = 30f;
        public float fpsCriticalThreshold = 15f;
        
        [Header("問題検出設定")]
        public int maxErrorsPerMinute = 10;
        public int maxWarningsPerMinute = 20;
        public float performanceDegradationThreshold = 0.5f; // 50%のパフォーマンス低下
        
        private static VastcoreDiagnostics instance;
        public static VastcoreDiagnostics Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<VastcoreDiagnostics>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("VastcoreDiagnostics");
                        instance = go.AddComponent<VastcoreDiagnostics>();
                    }
                }
                return instance;
            }
        }
        
        public enum DiagnosticSeverity
        {
            Info,
            Warning,
            Error,
            Critical
        }
        
        public enum DiagnosticCategory
        {
            System,
            Memory,
            Performance,
            TerrainGeneration,
            PrimitiveSpawn,
            UserInterface,
            PlayerController
        }
        
        [System.Serializable]
        public class DiagnosticResult
        {
            public DateTime timestamp;
            public DiagnosticCategory category;
            public DiagnosticSeverity severity;
            public string issue;
            public string description;
            public List<string> suggestedSolutions;
            public Dictionary<string, object> diagnosticData;
            
            public DiagnosticResult()
            {
                timestamp = DateTime.Now;
                suggestedSolutions = new List<string>();
                diagnosticData = new Dictionary<string, object>();
            }
        }
        
        private List<DiagnosticResult> diagnosticHistory = new List<DiagnosticResult>();
        private Dictionary<DiagnosticCategory, float> lastDiagnosticTime = new Dictionary<DiagnosticCategory, float>();
        private Dictionary<string, int> recentErrorCounts = new Dictionary<string, int>();
        private Queue<float> recentFrameTimes = new Queue<float>();
        private float baselinePerformance = 0f;
        private bool isInitialized = false;
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDiagnostics();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            if (enableAutoDiagnostics)
            {
                StartCoroutine(AutoDiagnosticRoutine());
            }
        }
        
        private void Update()
        {
            if (isInitialized)
            {
                UpdatePerformanceMetrics();
                CheckCriticalIssues();
            }
        }
        
        private void InitializeDiagnostics()
        {
            try
            {
                VastcoreLogger.Instance.LogInfo("Diagnostics", "診断システムを初期化中...");
                
                // ベースラインパフォーマンスの設定
                StartCoroutine(EstablishPerformanceBaseline());
                
                // 初期システムチェック
                PerformInitialSystemCheck();
                
                isInitialized = true;
                VastcoreLogger.Instance.LogInfo("Diagnostics", "診断システムが初期化されました");
            }
            catch (Exception error)
            {
                VastcoreLogger.Instance.LogError("Diagnostics", 
                    $"診断システム初期化エラー: {error.Message}", error);
            }
        }
        
        private IEnumerator AutoDiagnosticRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(diagnosticInterval);
                
                if (enableAutoDiagnostics)
                {
                    PerformComprehensiveDiagnostics();
                }
            }
        }
        
        private IEnumerator EstablishPerformanceBaseline()
        {
            VastcoreLogger.Instance.LogInfo("Diagnostics", "パフォーマンスベースラインを確立中...");
            
            List<float> baselineFrameTimes = new List<float>();
            
            // 10秒間のフレーム時間を収集
            for (int i = 0; i < 600; i++) // 60FPS * 10秒
            {
                baselineFrameTimes.Add(Time.deltaTime);
                yield return null;
            }
            
            baselinePerformance = baselineFrameTimes.Average();
            VastcoreLogger.Instance.LogInfo("Diagnostics", 
                $"ベースラインパフォーマンス確立: {baselinePerformance * 1000f:F2}ms");
        }
        
        private void UpdatePerformanceMetrics()
        {
            // 最近のフレーム時間を記録
            recentFrameTimes.Enqueue(Time.deltaTime);
            
            // 最新の100フレームのみ保持
            while (recentFrameTimes.Count > 100)
            {
                recentFrameTimes.Dequeue();
            }
        }
        
        private void CheckCriticalIssues()
        {
            // メモリ使用量の緊急チェック
            float memoryUsage = System.GC.GetTotalMemory(false) / (1024f * 1024f);
            if (memoryUsage > memoryCriticalThreshold)
            {
                var result = new DiagnosticResult
                {
                    category = DiagnosticCategory.Memory,
                    severity = DiagnosticSeverity.Critical,
                    issue = "Critical Memory Usage",
                    description = $"メモリ使用量が危険レベルに達しています: {memoryUsage:F1}MB"
                };
                result.suggestedSolutions.Add("即座にガベージコレクションを実行");
                result.suggestedSolutions.Add("不要なオブジェクトを削除");
                result.suggestedSolutions.Add("品質設定を下げる");
                
                AddDiagnosticResult(result);
                
                // 緊急メモリクリーンアップ
                VastcoreErrorHandler.Instance.HandleMemoryPressure();
            }
            
            // FPSの緊急チェック
            if (recentFrameTimes.Count > 10)
            {
                float currentFPS = 1f / recentFrameTimes.Average();
                if (currentFPS < fpsCriticalThreshold)
                {
                    var result = new DiagnosticResult
                    {
                        category = DiagnosticCategory.Performance,
                        severity = DiagnosticSeverity.Critical,
                        issue = "Critical Performance Degradation",
                        description = $"FPSが危険レベルまで低下: {currentFPS:F1}"
                    };
                    result.suggestedSolutions.Add("品質設定を最低レベルに下げる");
                    result.suggestedSolutions.Add("不要な処理を停止");
                    result.suggestedSolutions.Add("LODレベルを最低に設定");
                    
                    AddDiagnosticResult(result);
                }
            }
        }
        
        /// <summary>
        /// 包括的な診断の実行
        /// </summary>
        public void PerformComprehensiveDiagnostics()
        {
            try
            {
                VastcoreLogger.Instance.LogInfo("Diagnostics", "包括的診断を開始");
                
                if (enableHealthChecks)
                {
                    PerformSystemHealthCheck();
                    PerformMemoryHealthCheck();
                    PerformPerformanceHealthCheck();
                }
                
                CheckTerrainGenerationHealth();
                CheckPrimitiveSpawnHealth();
                CheckUISystemHealth();
                CheckPlayerControllerHealth();
                
                VastcoreLogger.Instance.LogInfo("Diagnostics", "包括的診断が完了");
            }
            catch (Exception error)
            {
                VastcoreLogger.Instance.LogError("Diagnostics", 
                    $"包括的診断中にエラー: {error.Message}", error);
            }
        }
        
        private void PerformInitialSystemCheck()
        {
            var result = new DiagnosticResult
            {
                category = DiagnosticCategory.System,
                severity = DiagnosticSeverity.Info,
                issue = "System Initialization",
                description = "システム初期化チェック"
            };
            
            // システム情報の収集
            result.diagnosticData["UnityVersion"] = Application.unityVersion;
            result.diagnosticData["Platform"] = Application.platform.ToString();
            result.diagnosticData["DeviceModel"] = SystemInfo.deviceModel;
            result.diagnosticData["SystemMemory"] = SystemInfo.systemMemorySize;
            result.diagnosticData["GraphicsMemory"] = SystemInfo.graphicsMemorySize;
            result.diagnosticData["ProcessorCount"] = SystemInfo.processorCount;
            
            AddDiagnosticResult(result);
        }
        
        private void PerformSystemHealthCheck()
        {
            var result = new DiagnosticResult
            {
                category = DiagnosticCategory.System,
                severity = DiagnosticSeverity.Info,
                issue = "System Health Check",
                description = "システム健全性チェック"
            };
            
            // 必要なコンポーネントの存在確認
            bool hasErrorHandler = VastcoreErrorHandler.Instance != null;
            bool hasLogger = VastcoreLogger.Instance != null;
            bool hasDebugVisualizer = VastcoreDebugVisualizer.Instance != null;
            
            result.diagnosticData["ErrorHandlerPresent"] = hasErrorHandler;
            result.diagnosticData["LoggerPresent"] = hasLogger;
            result.diagnosticData["DebugVisualizerPresent"] = hasDebugVisualizer;
            
            if (!hasErrorHandler || !hasLogger)
            {
                result.severity = DiagnosticSeverity.Warning;
                result.description = "重要なシステムコンポーネントが不足しています";
                result.suggestedSolutions.Add("不足しているコンポーネントを追加");
            }
            
            AddDiagnosticResult(result);
        }
        
        private void PerformMemoryHealthCheck()
        {
            float memoryUsage = System.GC.GetTotalMemory(false) / (1024f * 1024f);
            
            var result = new DiagnosticResult
            {
                category = DiagnosticCategory.Memory,
                severity = DiagnosticSeverity.Info,
                issue = "Memory Health Check",
                description = $"現在のメモリ使用量: {memoryUsage:F1}MB"
            };
            
            result.diagnosticData["MemoryUsage"] = memoryUsage;
            result.diagnosticData["SystemMemory"] = SystemInfo.systemMemorySize;
            result.diagnosticData["MemoryUsagePercentage"] = (memoryUsage / SystemInfo.systemMemorySize) * 100f;
            
            if (memoryUsage > memoryCriticalThreshold)
            {
                result.severity = DiagnosticSeverity.Critical;
                result.description = "メモリ使用量が危険レベルです";
                result.suggestedSolutions.Add("即座にメモリクリーンアップを実行");
                result.suggestedSolutions.Add("不要なオブジェクトを削除");
            }
            else if (memoryUsage > memoryWarningThreshold)
            {
                result.severity = DiagnosticSeverity.Warning;
                result.description = "メモリ使用量が警告レベルです";
                result.suggestedSolutions.Add("定期的なガベージコレクションを実行");
                result.suggestedSolutions.Add("オブジェクトプールの使用を検討");
            }
            
            AddDiagnosticResult(result);
        }
        
        private void PerformPerformanceHealthCheck()
        {
            if (recentFrameTimes.Count < 10) return;
            
            float currentFPS = 1f / recentFrameTimes.Average();
            float performanceDegradation = 0f;
            
            if (baselinePerformance > 0f)
            {
                float currentFrameTime = recentFrameTimes.Average();
                performanceDegradation = (currentFrameTime - baselinePerformance) / baselinePerformance;
            }
            
            var result = new DiagnosticResult
            {
                category = DiagnosticCategory.Performance,
                severity = DiagnosticSeverity.Info,
                issue = "Performance Health Check",
                description = $"現在のFPS: {currentFPS:F1}"
            };
            
            result.diagnosticData["CurrentFPS"] = currentFPS;
            result.diagnosticData["BaselineFrameTime"] = baselinePerformance * 1000f;
            result.diagnosticData["CurrentFrameTime"] = recentFrameTimes.Average() * 1000f;
            result.diagnosticData["PerformanceDegradation"] = performanceDegradation * 100f;
            
            if (currentFPS < fpsCriticalThreshold)
            {
                result.severity = DiagnosticSeverity.Critical;
                result.description = "パフォーマンスが危険レベルまで低下";
                result.suggestedSolutions.Add("品質設定を最低レベルに下げる");
                result.suggestedSolutions.Add("不要な処理を停止");
            }
            else if (currentFPS < fpsWarningThreshold)
            {
                result.severity = DiagnosticSeverity.Warning;
                result.description = "パフォーマンスが警告レベルまで低下";
                result.suggestedSolutions.Add("品質設定を下げる");
                result.suggestedSolutions.Add("LODレベルを調整");
            }
            else if (performanceDegradation > performanceDegradationThreshold)
            {
                result.severity = DiagnosticSeverity.Warning;
                result.description = $"ベースラインから{performanceDegradation * 100f:F1}%のパフォーマンス低下";
                result.suggestedSolutions.Add("パフォーマンスプロファイリングを実行");
                result.suggestedSolutions.Add("重い処理を特定して最適化");
            }
            
            AddDiagnosticResult(result);
        }
        
        private void CheckTerrainGenerationHealth()
        {
            // 地形生成システムの健全性チェック
            var terrainManagers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(mb => mb.GetType().Name.Contains("Terrain")).ToArray();
            
            var result = new DiagnosticResult
            {
                category = DiagnosticCategory.TerrainGeneration,
                severity = DiagnosticSeverity.Info,
                issue = "Terrain Generation Health",
                description = $"地形管理コンポーネント数: {terrainManagers.Length}"
            };
            
            result.diagnosticData["TerrainManagerCount"] = terrainManagers.Length;
            
            if (terrainManagers.Length == 0)
            {
                result.severity = DiagnosticSeverity.Warning;
                result.description = "地形管理コンポーネントが見つかりません";
                result.suggestedSolutions.Add("地形管理システムを初期化");
            }
            
            AddDiagnosticResult(result);
        }
        
        private void CheckPrimitiveSpawnHealth()
        {
            // プリミティブ生成システムの健全性チェック
            var primitiveObjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(mb => mb.GetType().Name.Contains("Primitive")).ToArray();
            
            var result = new DiagnosticResult
            {
                category = DiagnosticCategory.PrimitiveSpawn,
                severity = DiagnosticSeverity.Info,
                issue = "Primitive Spawn Health",
                description = $"プリミティブオブジェクト数: {primitiveObjects.Length}"
            };
            
            result.diagnosticData["PrimitiveObjectCount"] = primitiveObjects.Length;
            
            if (primitiveObjects.Length > 100)
            {
                result.severity = DiagnosticSeverity.Warning;
                result.description = "プリミティブオブジェクトが多すぎます";
                result.suggestedSolutions.Add("不要なプリミティブオブジェクトを削除");
                result.suggestedSolutions.Add("オブジェクトプールを使用");
            }
            
            AddDiagnosticResult(result);
        }
        
        private void CheckUISystemHealth()
        {
            // UIシステムの健全性チェック
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            
            var result = new DiagnosticResult
            {
                category = DiagnosticCategory.UserInterface,
                severity = DiagnosticSeverity.Info,
                issue = "UI System Health",
                description = $"アクティブなCanvas数: {canvases.Length}"
            };
            
            result.diagnosticData["CanvasCount"] = canvases.Length;
            
            if (canvases.Length > 5)
            {
                result.severity = DiagnosticSeverity.Warning;
                result.description = "Canvas数が多すぎる可能性があります";
                result.suggestedSolutions.Add("不要なCanvasを統合または削除");
            }
            
            AddDiagnosticResult(result);
        }
        
        private void CheckPlayerControllerHealth()
        {
            // プレイヤーコントローラーの健全性チェック
            var playerControllers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .Where(mb => mb.GetType().Name.Contains("Player")).ToArray();
            
            var result = new DiagnosticResult
            {
                category = DiagnosticCategory.PlayerController,
                severity = DiagnosticSeverity.Info,
                issue = "Player Controller Health",
                description = $"プレイヤーコントローラー数: {playerControllers.Length}"
            };
            
            result.diagnosticData["PlayerControllerCount"] = playerControllers.Length;
            
            if (playerControllers.Length == 0)
            {
                result.severity = DiagnosticSeverity.Error;
                result.description = "プレイヤーコントローラーが見つかりません";
                result.suggestedSolutions.Add("プレイヤーコントローラーを追加");
            }
            else if (playerControllers.Length > 1)
            {
                result.severity = DiagnosticSeverity.Warning;
                result.description = "複数のプレイヤーコントローラーが存在します";
                result.suggestedSolutions.Add("不要なプレイヤーコントローラーを削除");
            }
            
            AddDiagnosticResult(result);
        }
        
        private void AddDiagnosticResult(DiagnosticResult result)
        {
            diagnosticHistory.Add(result);
            
            // 履歴の制限（最新の1000件のみ保持）
            while (diagnosticHistory.Count > 1000)
            {
                diagnosticHistory.RemoveAt(0);
            }
            
            // ログ出力
            string logMessage = $"{result.issue}: {result.description}";
            switch (result.severity)
            {
                case DiagnosticSeverity.Info:
                    VastcoreLogger.Instance.LogInfo("Diagnostics", logMessage);
                    break;
                case DiagnosticSeverity.Warning:
                    VastcoreLogger.Instance.LogWarning("Diagnostics", logMessage);
                    break;
                case DiagnosticSeverity.Error:
                case DiagnosticSeverity.Critical:
                    VastcoreLogger.Instance.LogError("Diagnostics", logMessage);
                    break;
            }
            
            // デバッグ可視化
            if (VastcoreDebugVisualizer.Instance != null)
            {
                Vector3 position = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
                Color color = GetSeverityColor(result.severity);
                
                VastcoreDebugVisualizer.Instance.AddGeneralDebugInfo(
                    result.category.ToString(), 
                    result.issue, 
                    position, 
                    color, 
                    10f
                );
            }
        }
        
        private Color GetSeverityColor(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Info: return Color.white;
                case DiagnosticSeverity.Warning: return Color.yellow;
                case DiagnosticSeverity.Error: return Color.red;
                case DiagnosticSeverity.Critical: return Color.magenta;
                default: return Color.gray;
            }
        }
        
        /// <summary>
        /// 診断履歴の取得
        /// </summary>
        public List<DiagnosticResult> GetDiagnosticHistory()
        {
            return new List<DiagnosticResult>(diagnosticHistory);
        }
        
        /// <summary>
        /// 特定カテゴリの診断結果取得
        /// </summary>
        public List<DiagnosticResult> GetDiagnosticsByCategory(DiagnosticCategory category)
        {
            return diagnosticHistory.Where(d => d.category == category).ToList();
        }
        
        /// <summary>
        /// 特定重要度の診断結果取得
        /// </summary>
        public List<DiagnosticResult> GetDiagnosticsBySeverity(DiagnosticSeverity severity)
        {
            return diagnosticHistory.Where(d => d.severity == severity).ToList();
        }
        
        /// <summary>
        /// 診断レポートの生成
        /// </summary>
        public string GenerateDiagnosticReport()
        {
            var report = new StringBuilder();
            report.AppendLine($"=== Vastcore Diagnostic Report - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            report.AppendLine();
            
            // 重要度別統計
            var severityGroups = diagnosticHistory.GroupBy(d => d.severity);
            report.AppendLine("=== Issue Summary ===");
            foreach (var group in severityGroups)
            {
                report.AppendLine($"{group.Key}: {group.Count()} issues");
            }
            report.AppendLine();
            
            // カテゴリ別統計
            var categoryGroups = diagnosticHistory.GroupBy(d => d.category);
            report.AppendLine("=== Category Summary ===");
            foreach (var group in categoryGroups)
            {
                report.AppendLine($"{group.Key}: {group.Count()} issues");
            }
            report.AppendLine();
            
            // 最近の重要な問題
            var recentCritical = diagnosticHistory
                .Where(d => d.severity >= DiagnosticSeverity.Error)
                .OrderByDescending(d => d.timestamp)
                .Take(10);
            
            if (recentCritical.Any())
            {
                report.AppendLine("=== Recent Critical Issues ===");
                foreach (var issue in recentCritical)
                {
                    report.AppendLine($"[{issue.timestamp:HH:mm:ss}] [{issue.severity}] {issue.issue}");
                    report.AppendLine($"  Description: {issue.description}");
                    if (issue.suggestedSolutions.Any())
                    {
                        report.AppendLine($"  Solutions: {string.Join(", ", issue.suggestedSolutions)}");
                    }
                    report.AppendLine();
                }
            }
            
            return report.ToString();
        }
        
        /// <summary>
        /// 診断履歴のクリア
        /// </summary>
        public void ClearDiagnosticHistory()
        {
            diagnosticHistory.Clear();
            VastcoreLogger.Instance.LogInfo("Diagnostics", "診断履歴をクリアしました");
        }
    }
}