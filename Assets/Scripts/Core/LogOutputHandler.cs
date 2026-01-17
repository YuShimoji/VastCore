using System;
using System.IO;
using System.Text;
using UnityEngine;
using Vastcore.Utilities;

namespace Vastcore.Core
{
    /// <summary>
    /// ログ出力処理を担当するクラス
    /// ファイル出力、コンソール出力、UI出力の管理
    /// </summary>
    public class LogOutputHandler
    {
        private string logFilePath;
        private FileStream logFileStream;
        private StreamWriter logWriter;
        private bool enableFileLogging;
        private bool enableConsoleLogging;
        private int maxLogFileSize;
        private int maxLogFiles;
        
        public bool IsFileLoggingEnabled => enableFileLogging && logWriter != null;
        
        public void Initialize(bool fileLogging, bool consoleLogging, string fileName, int maxFileSize, int maxFiles)
        {
            enableFileLogging = fileLogging;
            enableConsoleLogging = consoleLogging;
            maxLogFileSize = maxFileSize;
            maxLogFiles = maxFiles;
            
            if (enableFileLogging)
            {
                InitializeFileLogging(fileName);
            }
        }
        
        private void InitializeFileLogging(string fileName)
        {
            try
            {
                string logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
                
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string finalFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{timestamp}{Path.GetExtension(fileName)}";
                logFilePath = Path.Combine(logDirectory, finalFileName);
                
                CleanupOldLogFiles(logDirectory, fileName);
                
                logFileStream = new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                logWriter = new StreamWriter(logFileStream, Encoding.UTF8);
                logWriter.AutoFlush = true;
                
                WriteLogHeader();
            }
            catch (Exception error)
            {
                Debug.LogError($"ファイルログ初期化エラー: {error.Message}");
                enableFileLogging = false;
            }
        }
        
        private void WriteLogHeader()
        {
            logWriter?.WriteLine($"=== Vastcore Log Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            logWriter?.WriteLine($"Unity Version: {Application.unityVersion}");
            logWriter?.WriteLine($"Platform: {Application.platform}");
            logWriter?.WriteLine($"Device Model: {SystemInfo.deviceModel}");
            logWriter?.WriteLine($"Memory Size: {SystemInfo.systemMemorySize}MB");
            logWriter?.WriteLine("=== Log Entries ===");
        }
        
        private void CleanupOldLogFiles(string logDirectory, string baseFileName)
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
        
        public void WriteToConsole(VastcoreLogger.LogEntry entry, Exception exception = null)
        {
            if (!enableConsoleLogging) return;
            
            string logMessage = entry.ToString();
            
            switch (entry.level)
            {
                case Vastcore.Utilities.VastcoreLogger.LogLevel.Debug:
                case Vastcore.Utilities.VastcoreLogger.LogLevel.Info:
                    Debug.Log(logMessage);
                    break;
                case Vastcore.Utilities.VastcoreLogger.LogLevel.Warning:
                    Debug.LogWarning(logMessage);
                    break;
                case Vastcore.Utilities.VastcoreLogger.LogLevel.Error:
                case Vastcore.Utilities.VastcoreLogger.LogLevel.Critical:
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
        
        public void WriteToFile(VastcoreLogger.LogEntry entry)
        {
            if (!IsFileLoggingEnabled) return;
            
            try
            {
                logWriter.WriteLine(entry.ToDetailedString());
                
                // ファイルサイズチェック
                if (logFileStream.Length > maxLogFileSize * 1024 * 1024)
                {
                    RotateLogFile();
                }
            }
            catch (Exception error)
            {
                Debug.LogError($"ファイルログ出力エラー: {error.Message}");
            }
        }
        
        private void RotateLogFile()
        {
            try
            {
                CloseLogFile();
                
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string newFileName = $"vastcore_log_{timestamp}.txt";
                string newLogPath = Path.Combine(Path.GetDirectoryName(logFilePath), newFileName);
                
                logFilePath = newLogPath;
                logFileStream = new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                logWriter = new StreamWriter(logFileStream, Encoding.UTF8);
                logWriter.AutoFlush = true;
                
                WriteLogHeader();
            }
            catch (Exception error)
            {
                Debug.LogError($"ログファイルローテーションエラー: {error.Message}");
            }
        }
        
        public void Flush()
        {
            try
            {
                logWriter?.Flush();
                logFileStream?.Flush();
            }
            catch (Exception error)
            {
                Debug.LogError($"ログフラッシュエラー: {error.Message}");
            }
        }
        
        public void CloseLogFile()
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
        
        public void SaveManualLog(VastcoreLogger.LogEntry[] entries, string customFileName = null)
        {
            try
            {
                string fileName = customFileName ?? $"manual_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(Application.persistentDataPath, "Logs", fileName);
                
                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    writer.WriteLine($"=== Manual Log Export at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                    
                    foreach (var entry in entries)
                    {
                        writer.WriteLine(entry.ToDetailedString());
                    }
                }
            }
            catch (Exception error)
            {
                Debug.LogError($"ログ手動保存エラー: {error.Message}");
            }
        }
    }
}