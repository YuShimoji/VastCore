using System;
using UnityEngine;
using Vastcore.Core;
using Vastcore.Utils;

namespace Vastcore.Core
{
    /// <summary>
    /// Vastcoreシステム全体の統合管理クラス
    /// エラーハンドリング、ログ、デバッグ、診断システムの統合初期化と管理
    /// </summary>
    public class VastcoreSystemManager : MonoBehaviour
    {
        [Header("システム初期化設定")]
        public bool initializeOnAwake = true;
        public bool enableAllSystems = true;
        
        [Header("個別システム制御")]
        public bool enableErrorHandler = true;
        public bool enableLogger = true;
        public bool enableDebugVisualizer = true;
        public bool enableDiagnostics = true;
        public bool enableTerrainErrorRecovery = true;
        public bool enablePrimitiveErrorRecovery = true;
        
        [Header("システム設定")]
        public Vastcore.Utils.VastcoreLogger.LogLevel systemLogLevel = Vastcore.Utils.VastcoreLogger.LogLevel.Info;
        public bool enableSystemHealthMonitoring = true;
        public float healthCheckInterval = 60f;
        
        private static VastcoreSystemManager instance;
        public static VastcoreSystemManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var existingManager = FindFirstObjectByType<VastcoreSystemManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("VastcoreSystemManager");
                        instance = go.AddComponent<VastcoreSystemManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        public enum SystemStatus
        {
            NotInitialized,
            Initializing,
            Running,
            Error,
            Shutdown
        }
        
        private SystemStatus currentStatus = SystemStatus.NotInitialized;
        private float lastHealthCheck;
        private bool isInitialized = false;
        
        // システムコンポーネントの参照
        private VastcoreErrorHandler errorHandler;
        private VastcoreLogger logger;
        private VastcoreDebugVisualizer debugVisualizer;
        private VastcoreDiagnostics diagnostics;
        private TerrainErrorRecovery terrainErrorRecovery;
        private PrimitiveErrorRecovery primitiveErrorRecovery;
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                
                if (initializeOnAwake)
                {
                    InitializeAllSystems();
                }
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Update()
        {
            if (isInitialized && enableSystemHealthMonitoring)
            {
                if (Time.time - lastHealthCheck > healthCheckInterval)
                {
                    PerformSystemHealthCheck();
                    lastHealthCheck = Time.time;
                }
            }
        }
        
        /// <summary>
        /// 全システムの初期化
        /// </summary>
        public void InitializeAllSystems()
        {
            try
            {
                currentStatus = SystemStatus.Initializing;
                Debug.Log("Vastcoreシステム初期化を開始...");
                
                // 1. ログシステムの初期化（最優先）
                if (enableLogger)
                {
                    InitializeLogger();
                }
                
                // 2. エラーハンドラーの初期化
                if (enableErrorHandler)
                {
                    InitializeErrorHandler();
                }
                
                // 3. デバッグ可視化システムの初期化
                if (enableDebugVisualizer)
                {
                    InitializeDebugVisualizer();
                }
                
                // 4. 診断システムの初期化
                if (enableDiagnostics)
                {
                    InitializeDiagnostics();
                }
                
                // 5. 地形エラー回復システムの初期化
                if (enableTerrainErrorRecovery)
                {
                    InitializeTerrainErrorRecovery();
                }
                
                // 6. プリミティブエラー回復システムの初期化
                if (enablePrimitiveErrorRecovery)
                {
                    InitializePrimitiveErrorRecovery();
                }
                
                // システム初期化完了
                currentStatus = SystemStatus.Running;
                isInitialized = true;
                
                if (logger != null)
                {
                    logger.LogInfo("SystemManager", "Vastcoreシステムの初期化が完了しました");
                    LogSystemStatus();
                }
                else
                {
                    Debug.Log("Vastcoreシステムの初期化が完了しました");
                }
            }
            catch (Exception error)
            {
                currentStatus = SystemStatus.Error;
                Debug.LogError($"Vastcoreシステム初期化エラー: {error.Message}");
                Debug.LogException(error);
            }
        }
        
        private void InitializeLogger()
        {
            try
            {
                logger = VastcoreLogger.Instance;
                if (logger != null)
                {
                    logger.minimumLogLevel = systemLogLevel;
                    Debug.Log("VastcoreLogger初期化完了");
                }
            }
            catch (Exception error)
            {
                Debug.LogError($"Logger初期化エラー: {error.Message}");
            }
        }
        
        private void InitializeErrorHandler()
        {
            try
            {
                errorHandler = VastcoreErrorHandler.Instance;
                if (errorHandler != null)
                {
                    if (logger != null)
                    {
                        logger.LogInfo("SystemManager", "VastcoreErrorHandler初期化完了");
                    }
                    else
                    {
                        Debug.Log("VastcoreErrorHandler初期化完了");
                    }
                }
            }
            catch (Exception error)
            {
                Debug.LogError($"ErrorHandler初期化エラー: {error.Message}");
            }
        }
        
        private void InitializeDebugVisualizer()
        {
            try
            {
                debugVisualizer = VastcoreDebugVisualizer.Instance;
                if (debugVisualizer != null)
                {
                    if (logger != null)
                    {
                        logger.LogInfo("SystemManager", "VastcoreDebugVisualizer初期化完了");
                    }
                    else
                    {
                        Debug.Log("VastcoreDebugVisualizer初期化完了");
                    }
                }
            }
            catch (Exception error)
            {
                Debug.LogError($"DebugVisualizer初期化エラー: {error.Message}");
            }
        }
        
        private void InitializeDiagnostics()
        {
            try
            {
                diagnostics = VastcoreDiagnostics.Instance;
                if (diagnostics != null)
                {
                    if (logger != null)
                    {
                        logger.LogInfo("SystemManager", "VastcoreDiagnostics初期化完了");
                    }
                    else
                    {
                        Debug.Log("VastcoreDiagnostics初期化完了");
                    }
                }
            }
            catch (Exception error)
            {
                Debug.LogError($"Diagnostics初期化エラー: {error.Message}");
            }
        }
        
        private void InitializeTerrainErrorRecovery()
        {
            try
            {
                terrainErrorRecovery = TerrainErrorRecovery.Instance;
                if (terrainErrorRecovery != null)
                {
                    if (logger != null)
                    {
                        logger.LogInfo("SystemManager", "TerrainErrorRecovery初期化完了");
                    }
                    else
                    {
                        Debug.Log("TerrainErrorRecovery初期化完了");
                    }
                }
            }
            catch (Exception error)
            {
                Debug.LogError($"TerrainErrorRecovery初期化エラー: {error.Message}");
            }
        }
        
        private void InitializePrimitiveErrorRecovery()
        {
            try
            {
                primitiveErrorRecovery = PrimitiveErrorRecovery.Instance;
                if (primitiveErrorRecovery != null)
                {
                    if (logger != null)
                    {
                        logger.LogInfo("SystemManager", "PrimitiveErrorRecovery初期化完了");
                    }
                    else
                    {
                        Debug.Log("PrimitiveErrorRecovery初期化完了");
                    }
                }
            }
            catch (Exception error)
            {
                Debug.LogError($"PrimitiveErrorRecovery初期化エラー: {error.Message}");
            }
        }
        
        private void LogSystemStatus()
        {
            if (logger == null) return;
            
            logger.LogInfo("SystemManager", "=== Vastcoreシステム状態 ===");
            logger.LogInfo("SystemManager", $"Status: {currentStatus}");
            logger.LogInfo("SystemManager", $"ErrorHandler: {(errorHandler != null ? "Active" : "Inactive")}");
            logger.LogInfo("SystemManager", $"Logger: {(logger != null ? "Active" : "Inactive")}");
            logger.LogInfo("SystemManager", $"DebugVisualizer: {(debugVisualizer != null ? "Active" : "Inactive")}");
            logger.LogInfo("SystemManager", $"Diagnostics: {(diagnostics != null ? "Active" : "Inactive")}");
            logger.LogInfo("SystemManager", $"TerrainErrorRecovery: {(terrainErrorRecovery != null ? "Active" : "Inactive")}");
            logger.LogInfo("SystemManager", $"PrimitiveErrorRecovery: {(primitiveErrorRecovery != null ? "Active" : "Inactive")}");
        }
        
        private void PerformSystemHealthCheck()
        {
            try
            {
                if (logger != null)
                {
                    logger.LogDebug("SystemManager", "システム健全性チェックを実行中...");
                }
                
                // 各システムの健全性をチェック
                bool allSystemsHealthy = true;
                
                if (enableErrorHandler && errorHandler == null)
                {
                    allSystemsHealthy = false;
                    if (logger != null)
                    {
                        logger.LogWarning("SystemManager", "ErrorHandlerが無効になっています");
                    }
                }
                
                if (enableLogger && logger == null)
                {
                    allSystemsHealthy = false;
                    Debug.LogWarning("Loggerが無効になっています");
                }
                
                if (enableDebugVisualizer && debugVisualizer == null)
                {
                    allSystemsHealthy = false;
                    if (logger != null)
                    {
                        logger.LogWarning("SystemManager", "DebugVisualizerが無効になっています");
                    }
                }
                
                if (enableDiagnostics && diagnostics == null)
                {
                    allSystemsHealthy = false;
                    if (logger != null)
                    {
                        logger.LogWarning("SystemManager", "Diagnosticsが無効になっています");
                    }
                }
                
                // システム状態の更新
                if (!allSystemsHealthy && currentStatus == SystemStatus.Running)
                {
                    currentStatus = SystemStatus.Error;
                    if (logger != null)
                    {
                        logger.LogError("SystemManager", "システム健全性チェックで問題が検出されました");
                    }
                }
                else if (allSystemsHealthy && currentStatus == SystemStatus.Error)
                {
                    currentStatus = SystemStatus.Running;
                    if (logger != null)
                    {
                        logger.LogInfo("SystemManager", "システムが正常状態に回復しました");
                    }
                }
            }
            catch (Exception error)
            {
                if (logger != null)
                {
                    logger.LogError("SystemManager", $"システム健全性チェック中にエラー: {error.Message}", error);
                }
                else
                {
                    Debug.LogError($"システム健全性チェック中にエラー: {error.Message}");
                }
            }
        }
        
        /// <summary>
        /// システムの手動再初期化
        /// </summary>
        public void ReinitializeSystems()
        {
            if (logger != null)
            {
                logger.LogInfo("SystemManager", "システムの再初期化を開始");
            }
            
            isInitialized = false;
            InitializeAllSystems();
        }
        
        /// <summary>
        /// システムのシャットダウン
        /// </summary>
        public void ShutdownSystems()
        {
            try
            {
                currentStatus = SystemStatus.Shutdown;
                
                if (logger != null)
                {
                    logger.LogInfo("SystemManager", "Vastcoreシステムをシャットダウン中...");
                }
                
                // 各システムのクリーンアップ
                if (diagnostics != null)
                {
                    diagnostics.ClearDiagnosticHistory();
                }
                
                if (debugVisualizer != null)
                {
                    debugVisualizer.ClearDebugInfo();
                }
                
                if (errorHandler != null)
                {
                    errorHandler.ResetErrorHandler();
                }
                
                if (logger != null)
                {
                    logger.LogInfo("SystemManager", "Vastcoreシステムのシャットダウンが完了しました");
                }
                
                isInitialized = false;
            }
            catch (Exception error)
            {
                Debug.LogError($"システムシャットダウン中にエラー: {error.Message}");
            }
        }
        
        /// <summary>
        /// システム状態の取得
        /// </summary>
        public SystemStatus GetSystemStatus()
        {
            return currentStatus;
        }
        
        /// <summary>
        /// システム情報の取得
        /// </summary>
        public string GetSystemInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine("=== Vastcore System Information ===");
            info.AppendLine($"Status: {currentStatus}");
            info.AppendLine($"Initialized: {isInitialized}");
            info.AppendLine($"ErrorHandler: {(errorHandler != null ? "Active" : "Inactive")}");
            info.AppendLine($"Logger: {(logger != null ? "Active" : "Inactive")}");
            info.AppendLine($"DebugVisualizer: {(debugVisualizer != null ? "Active" : "Inactive")}");
            info.AppendLine($"Diagnostics: {(diagnostics != null ? "Active" : "Inactive")}");
            info.AppendLine($"TerrainErrorRecovery: {(terrainErrorRecovery != null ? "Active" : "Inactive")}");
            info.AppendLine($"PrimitiveErrorRecovery: {(primitiveErrorRecovery != null ? "Active" : "Inactive")}");
            info.AppendLine($"Health Monitoring: {enableSystemHealthMonitoring}");
            info.AppendLine($"Last Health Check: {lastHealthCheck}");
            
            return info.ToString();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (logger != null)
            {
                logger.LogInfo("SystemManager", $"Application pause: {pauseStatus}");
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (logger != null)
            {
                logger.LogInfo("SystemManager", $"Application focus: {hasFocus}");
            }
        }
        
        private void OnApplicationQuit()
        {
            ShutdownSystems();
        }
    }
}