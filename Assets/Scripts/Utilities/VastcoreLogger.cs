using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Vastcore.Core
{
    /// <summary>
    /// Vastcore専用の強化されたログシステム
    /// 詳細なログ出力、デバッグ情報の可視化、問題診断支援機能を提供
    /// </summary>
    public class VastcoreLogger : MonoBehaviour
    {
        [Header("ログ設定")]
        public bool enableFileLogging = true;
        public bool enableConsoleLogging = true;
        public bool enableInGameLogging = true;
        public LogLevel minimumLogLevel = LogLevel.Info;
        
        [Header("ファイルログ設定")]
        public string logFileName = "vastcore_log.txt";
        public int maxLogFileSize = 10; // MB
        public int maxLogFiles = 5;
        public bool timestampInFilename = true;
        
        [Header("インゲームログ設定")]
        public int maxInGameLogEntries = 100;
        public bool showLogInUI = true;
        public KeyCode toggleLogUIKey = KeyCode.F12;
        
        [Header("パフォーマンス監視")]
        public bool enablePerformanceLogging = true;
        public float performanceLogInterval = 5f;
        
        public enum LogLevel
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
            Critical = 4
        }
        
        [System.Serializable]
        public class LogEntry
        {
            public DateTime timestamp;
            public LogLevel level;
            public string category;
            public string message;
            public string stackTrace;
            public float frameTime;
            public long memoryUsage;
            
            public override string ToString()
            {
                return $"[{timestamp:HH:mm:ss.fff}] [{level}] [{category}] {message}";
            }
            
            public string ToDetailedString()
            {
                var sb = new StringBuilder();
                sb.AppendLine($"[{timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{category}]");
                sb.AppendLine($"Message: {message}");
                sb.AppendLine($"Frame Time: {frameTime:F2}ms");
                sb.AppendLine($"Memory: {memoryUsage / (1024 * 1024)}MB");
                if (!string.IsNullOrEmpty(stackTrace))
                {
                    sb.AppendLine($"Stack Trace:\n{stackTrace}");
                }
                sb.AppendLine("---");
                return sb.ToString();
            }
        }
        
        private static VastcoreLogger instance;
        public static VastcoreLogger Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<VastcoreLogger>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("VastcoreLogger");
                        instance = go.AddComponent<VastcoreLogger>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        private Queue<LogEntry> logEntries = new Queue<LogEntry>();
        private List<LogEntry> inGameLogEntries = new List<LogEntry>();
        private string logFilePath;
        private FileStream logFileStream;
        private StreamWriter logWriter;
        private float lastPerformanceLog;
        private bool showLogUI = false;
        private Vector2 logScrollPosition;
        private string logFilter = "";
        private LogLevel filterLevel = LogLevel.Debug;
        
        [Header("ファイルバッファ設定")]
        public bool useBufferedFileLogging = true;
        public int fileBufferEntryThreshold = 50;
        public float fileFlushInterval = 2f;
        private float lastFileFlushTime = 0f;
        private readonly List<string> fileWriteBuffer = new List<string>(256);
        private bool isRotatingFile = false;
        
        // パフォーマンス統計
        private Dictionary<string, PerformanceStats> performanceStats = new Dictionary<string, PerformanceStats>();
        
        [System.Serializable]
        public class PerformanceStats
        {
            public string category;
            public int callCount;
            public float totalTime;
            public float averageTime;
            public float maxTime;
            public float minTime = float.MaxValue;
            public DateTime lastUpdate;
            
            public void AddSample(float time)
            {
                callCount++;
                totalTime += time;
                averageTime = totalTime / callCount;
                maxTime = Mathf.Max(maxTime, time);
                minTime = Mathf.Min(minTime, time);
                lastUpdate = DateTime.Now;
            }
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeLogger();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(toggleLogUIKey))
            {
                showLogUI = !showLogUI;
            }
            
            if (enablePerformanceLogging && Time.time - lastPerformanceLog > performanceLogInterval)
            {
                LogPerformanceStats();
                lastPerformanceLog = Time.time;
            }
            
            // バッファの定期フラッシュ
            if (enableFileLogging && useBufferedFileLogging && logWriter != null)
            {
                if (Time.time - lastFileFlushTime >= fileFlushInterval)
                {
                    FlushFileBuffer();
                }
            }
        }
        
        private void OnGUI()
        {
            if (showLogUI && showLogInUI)
            {
                DrawLogUI();
            }
        }
        
        private void OnDestroy()
        {
            // バッファリングされているログを失わないようにフラッシュしてからクローズ
            FlushLogs();
            CloseLogFile();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                FlushLogs();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                FlushLogs();
            }
        }
        
        private void InitializeLogger()
        {
            try
            {
                if (enableFileLogging)
                {
                    InitializeFileLogging();
                }
                
                LogInfo("Logger", "VastcoreLogger が初期化されました");
                LogSystemInfo();
            }
            catch (Exception error)
            {
                Debug.LogError($"VastcoreLogger初期化エラー: {error.Message}");
            }
        }
        
        private void InitializeFileLogging()
        {
            try
            {
                string logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                
                string fileName = logFileName;
                if (timestampInFilename)
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    fileName = $"{Path.GetFileNameWithoutExtension(logFileName)}_{timestamp}{Path.GetExtension(logFileName)}";
                }
                
                logFilePath = Path.Combine(logDirectory, fileName);
                
                // 古いログファイルのクリーンアップ
                CleanupOldLogFiles(logDirectory);
                
                logFileStream = new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                logWriter = new StreamWriter(logFileStream, Encoding.UTF8);
                logWriter.AutoFlush = !useBufferedFileLogging;
                
                // ログファイルヘッダーの書き込み
                logWriter.WriteLine($"=== Vastcore Log Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                logWriter.WriteLine($"Unity Version: {Application.unityVersion}");
                logWriter.WriteLine($"Platform: {Application.platform}");
                logWriter.WriteLine($"Device Model: {SystemInfo.deviceModel}");
                logWriter.WriteLine($"Memory Size: {SystemInfo.systemMemorySize}MB");
                logWriter.WriteLine("=== Log Entries ===");
            }
            catch (Exception error)
            {
                Debug.LogError($"ファイルログ初期化エラー: {error.Message}");
                enableFileLogging = false;
            }
        }
        
        private void CleanupOldLogFiles(string logDirectory)
        {
            try
            {
                var logFiles = Directory.GetFiles(logDirectory, "*.txt");
                if (logFiles.Length > maxLogFiles)
                {
                    Array.Sort(logFiles, (x, y) => File.GetCreationTime(x).CompareTo(File.GetCreationTime(y)));
                    
                    for (int i = 0; i < logFiles.Length - maxLogFiles; i++)
                    {
                        File.Delete(logFiles[i]);
                    }
                }
            }
            catch (Exception error)
            {
                Debug.LogWarning($"古いログファイルクリーンアップエラー: {error.Message}");
            }
        }
        
        private void LogSystemInfo()
        {
            LogInfo("System", $"デバイス: {SystemInfo.deviceModel}");
            LogInfo("System", $"OS: {SystemInfo.operatingSystem}");
            LogInfo("System", $"CPU: {SystemInfo.processorType} ({SystemInfo.processorCount} cores)");
            LogInfo("System", $"GPU: {SystemInfo.graphicsDeviceName}");
            LogInfo("System", $"メモリ: {SystemInfo.systemMemorySize}MB");
            LogInfo("System", $"VRAM: {SystemInfo.graphicsMemorySize}MB");
            LogInfo("System", $"解像度: {Screen.width}x{Screen.height}");
        }
        
        /// <summary>
        /// デバッグレベルのログ出力
        /// </summary>
        public void LogDebug(string category, string message, Exception exception = null)
        {
            Log(LogLevel.Debug, category, message, exception);
        }
        
        /// <summary>
        /// 情報レベルのログ出力
        /// </summary>
        public void LogInfo(string category, string message, Exception exception = null)
        {
            Log(LogLevel.Info, category, message, exception);
        }
        
        /// <summary>
        /// 警告レベルのログ出力
        /// </summary>
        public void LogWarning(string category, string message, Exception exception = null)
        {
            Log(LogLevel.Warning, category, message, exception);
        }
        
        /// <summary>
        /// エラーレベルのログ出力
        /// </summary>
        public void LogError(string category, string message, Exception exception = null)
        {
            Log(LogLevel.Error, category, message, exception);
        }
        
        /// <summary>
        /// 重大エラーレベルのログ出力
        /// </summary>
        public void LogCritical(string category, string message, Exception exception = null)
        {
            Log(LogLevel.Critical, category, message, exception);
        }
        
        /// <summary>
        /// パフォーマンス測定付きログ出力
        /// </summary>
        public void LogPerformance(string category, string operation, float executionTime)
        {
            if (!performanceStats.ContainsKey(category))
            {
                performanceStats[category] = new PerformanceStats { category = category };
            }
            
            performanceStats[category].AddSample(executionTime);
            
            LogDebug("Performance", $"{category}.{operation}: {executionTime:F2}ms");
        }
        
        private void Log(LogLevel level, string category, string message, Exception exception = null)
        {
            if (level < minimumLogLevel) return;
            
            try
            {
                var logEntry = new LogEntry
                {
                    timestamp = DateTime.Now,
                    level = level,
                    category = category,
                    message = message,
                    stackTrace = exception?.StackTrace ?? (level >= LogLevel.Error ? Environment.StackTrace : ""),
                    frameTime = Time.deltaTime * 1000f,
                    memoryUsage = System.GC.GetTotalMemory(false)
                };
                
                // コンソールログ
                if (enableConsoleLogging)
                {
                    LogToConsole(logEntry, exception);
                }
                
                // ファイルログ
                if (enableFileLogging && logWriter != null)
                {
                    LogToFile(logEntry);
                }
                
                // インゲームログ
                if (enableInGameLogging)
                {
                    LogToInGame(logEntry);
                }
                
                // ログエントリーをキューに追加
                logEntries.Enqueue(logEntry);
                
                // キューサイズの制限
                while (logEntries.Count > 1000)
                {
                    logEntries.Dequeue();
                }
            }
            catch (Exception error)
            {
                Debug.LogError($"ログ出力中にエラー: {error.Message}");
            }
        }
        
        private void LogToConsole(LogEntry entry, Exception exception)
        {
            string logMessage = entry.ToString();
            
            switch (entry.level)
            {
                case LogLevel.Debug:
                    Debug.Log(logMessage);
                    break;
                case LogLevel.Info:
                    Debug.Log(logMessage);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(logMessage);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    if (exception != null)
                    {
                        Debug.LogException(exception);
                    }
                    else
                    {
                        Debug.LogError(logMessage);
                    }
                    break;
            }
        }
        
        private void LogToFile(LogEntry entry)
        {
            try
            {
                if (useBufferedFileLogging)
                {
                    // バッファに蓄積
                    fileWriteBuffer.Add(entry.ToDetailedString());

                    // しきい値を超えたらフラッシュ
                    if (fileWriteBuffer.Count >= fileBufferEntryThreshold)
                    {
                        FlushFileBuffer();
                    }
                }
                else
                {
                    logWriter?.WriteLine(entry.ToDetailedString());
                    // 非バッファ時は即時サイズチェック
                    if (logFileStream != null && logFileStream.Length > maxLogFileSize * 1024 * 1024)
                    {
                        RotateLogFile();
                    }
                }
            }
            catch (Exception error)
            {
                Debug.LogError($"ファイルログ出力エラー: {error.Message}");
            }
        }
        
        private void LogToInGame(LogEntry entry)
        {
            inGameLogEntries.Add(entry);
            
            // インゲームログエントリー数の制限
            while (inGameLogEntries.Count > maxInGameLogEntries)
            {
                inGameLogEntries.RemoveAt(0);
            }
        }
        
        private void FlushFileBuffer()
        {
            try
            {
                if (!useBufferedFileLogging) // 後方互換
                {
                    logWriter?.Flush();
                    logFileStream?.Flush();
                    lastFileFlushTime = Time.time;
                    return;
                }

                if (logWriter == null || fileWriteBuffer.Count == 0) return;

                for (int i = 0; i < fileWriteBuffer.Count; i++)
                {
                    logWriter.Write(fileWriteBuffer[i]);
                }

                fileWriteBuffer.Clear();
                logWriter.Flush();
                logFileStream.Flush();
                lastFileFlushTime = Time.time;

                // サイズチェック（ローテーション）
                if (!isRotatingFile && logFileStream != null && logFileStream.Length > maxLogFileSize * 1024 * 1024)
                {
                    RotateLogFile();
                }
            }
            catch (Exception error)
            {
                Debug.LogError($"ログフラッシュエラー: {error.Message}");
            }
        }
        
        private void RotateLogFile()
        {
            try
            {
                if (isRotatingFile) return;
                isRotatingFile = true;

                // バッファ分は新ファイルに書き出すため、ここではフラッシュしない
                CloseLogFile();
                
                // 新しいログファイルを作成
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string newFileName = $"{Path.GetFileNameWithoutExtension(logFileName)}_{timestamp}{Path.GetExtension(logFileName)}";
                string newLogPath = Path.Combine(Path.GetDirectoryName(logFilePath), newFileName);
                
                logFilePath = newLogPath;
                logFileStream = new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                logWriter = new StreamWriter(logFileStream, Encoding.UTF8);
                logWriter.AutoFlush = !useBufferedFileLogging;
                
                // ヘッダー再出力
                logWriter.WriteLine($"=== Vastcore Log Rotated at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                logWriter.WriteLine($"Unity Version: {Application.unityVersion}");
                logWriter.WriteLine($"Platform: {Application.platform}");
                logWriter.WriteLine($"Device Model: {SystemInfo.deviceModel}");
                logWriter.WriteLine($"Memory Size: {SystemInfo.systemMemorySize}MB");
                logWriter.WriteLine("=== Log Entries ===");

                // 溜まっているバッファがあれば新しいファイルへ書き出す
                if (useBufferedFileLogging && fileWriteBuffer.Count > 0)
                {
                    FlushFileBuffer();
                }

                // 再帰的ログ呼び出しを避けるため Debug.Log を使用
                Debug.Log("[VastcoreLogger] ログファイルをローテーションしました");
                isRotatingFile = false;
            }
            catch (Exception error)
            {
                Debug.LogError($"ログファイルローテーションエラー: {error.Message}");
                isRotatingFile = false;
            }
        }
        
        private void LogPerformanceStats()
        {
            foreach (var stats in performanceStats.Values)
            {
                if (stats.callCount > 0)
                {
                    LogInfo("PerformanceStats", 
                        $"{stats.category}: Calls={stats.callCount}, Avg={stats.averageTime:F2}ms, " +
                        $"Max={stats.maxTime:F2}ms, Min={stats.minTime:F2}ms");
                }
            }
        }
        
        private void DrawLogUI()
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float logWindowWidth = screenWidth * 0.8f;
            float logWindowHeight = screenHeight * 0.6f;
            
            GUILayout.BeginArea(new Rect(
                (screenWidth - logWindowWidth) / 2,
                (screenHeight - logWindowHeight) / 2,
                logWindowWidth,
                logWindowHeight
            ));
            
            GUILayout.BeginVertical("box");
            
            // ヘッダー
            GUILayout.BeginHorizontal();
            GUILayout.Label("Vastcore Debug Log", GUILayout.Width(200));
            GUILayout.FlexibleSpace();
            
            // フィルター
            GUILayout.Label("Filter:", GUILayout.Width(50));
            logFilter = GUILayout.TextField(logFilter, GUILayout.Width(150));
            
            GUILayout.Label("Level:", GUILayout.Width(50));
            filterLevel = (LogLevel)GUILayout.SelectionGrid((int)filterLevel, 
                Enum.GetNames(typeof(LogLevel)), 5, GUILayout.Width(300));
            
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                inGameLogEntries.Clear();
            }
            
            if (GUILayout.Button("Close", GUILayout.Width(60)))
            {
                showLogUI = false;
            }
            
            GUILayout.EndHorizontal();
            
            // ログエントリー表示
            logScrollPosition = GUILayout.BeginScrollView(logScrollPosition);
            
            var filteredEntries = GetFilteredLogEntries();
            foreach (var entry in filteredEntries)
            {
                Color originalColor = GUI.color;
                GUI.color = GetLogLevelColor(entry.level);
                
                GUILayout.BeginHorizontal("box");
                GUILayout.Label($"[{entry.timestamp:HH:mm:ss}]", GUILayout.Width(80));
                GUILayout.Label($"[{entry.level}]", GUILayout.Width(80));
                GUILayout.Label($"[{entry.category}]", GUILayout.Width(120));
                GUILayout.Label(entry.message);
                GUILayout.EndHorizontal();
                
                GUI.color = originalColor;
            }
            
            GUILayout.EndScrollView();
            
            // 統計情報
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Total Entries: {inGameLogEntries.Count}");
            GUILayout.Label($"Filtered: {filteredEntries.Count}");
            GUILayout.Label($"Memory: {System.GC.GetTotalMemory(false) / (1024 * 1024)}MB");
            GUILayout.Label($"FPS: {1f / Time.deltaTime:F1}");
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private List<LogEntry> GetFilteredLogEntries()
        {
            var filtered = new List<LogEntry>();
            
            foreach (var entry in inGameLogEntries)
            {
                if (entry.level < filterLevel) continue;
                
                if (!string.IsNullOrEmpty(logFilter))
                {
                    if (!entry.message.ToLower().Contains(logFilter.ToLower()) &&
                        !entry.category.ToLower().Contains(logFilter.ToLower()))
                    {
                        continue;
                    }
                }
                
                filtered.Add(entry);
            }
            
            return filtered;
        }
        
        private Color GetLogLevelColor(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug: return Color.gray;
                case LogLevel.Info: return Color.white;
                case LogLevel.Warning: return Color.yellow;
                case LogLevel.Error: return Color.red;
                case LogLevel.Critical: return Color.magenta;
                default: return Color.white;
            }
        }
        
        private void FlushLogs()
        {
            try
            {
                // まずバッファをフラッシュ
                FlushFileBuffer();
                if (!useBufferedFileLogging)
                {
                    logWriter?.Flush();
                    logFileStream?.Flush();
                }
            }
            catch (Exception error)
            {
                Debug.LogError($"ログフラッシュエラー: {error.Message}");
            }
        }
        
        private void CloseLogFile()
        {
            try
            {
                logWriter?.Close();
                logFileStream?.Close();
                logWriter = null;
                logFileStream = null;
            }
            catch (Exception error)
            {
                Debug.LogError($"ログファイルクローズエラー: {error.Message}");
            }
        }
        
        /// <summary>
        /// ログエントリーの取得
        /// </summary>
        public LogEntry[] GetLogEntries()
        {
            return logEntries.ToArray();
        }
        
        /// <summary>
        /// パフォーマンス統計の取得
        /// </summary>
        public Dictionary<string, PerformanceStats> GetPerformanceStats()
        {
            return new Dictionary<string, PerformanceStats>(performanceStats);
        }
        
        /// <summary>
        /// ログの手動保存
        /// </summary>
        public void SaveLogsToFile(string customFileName = null)
        {
            try
            {
                string fileName = customFileName ?? $"manual_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(Application.persistentDataPath, "Logs", fileName);
                
                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    writer.WriteLine($"=== Manual Log Export at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                    
                    foreach (var entry in logEntries)
                    {
                        writer.WriteLine(entry.ToDetailedString());
                    }
                }
                
                LogInfo("Logger", $"ログを手動保存しました: {filePath}");
            }
            catch (Exception error)
            {
                LogError("Logger", $"ログ手動保存エラー: {error.Message}", error);
            }
        }
    }
}