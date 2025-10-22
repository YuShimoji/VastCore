using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.Core
{
    /// <summary>
    /// 堅牢なエラーハンドリングシステム
    /// 地形生成失敗、プリミティブ配置エラー、メモリ不足時の自動調整を処理
    /// </summary>
    public class VastcoreErrorHandler : MonoBehaviour
    {
        [Header("エラーハンドリング設定")]
        public bool enableErrorRecovery = true;
        public bool enableFallbackGeneration = true;
        public bool enableMemoryManagement = true;
        public int maxRetryAttempts = 3;
        
        [Header("メモリ管理")]
        public long memoryThresholdMB = 1024; // 1GB
        public float memoryCheckInterval = 5f;
        
        [Header("フォールバック設定")]
        public bool useLowQualityFallback = true;
        public float fallbackQualityMultiplier = 0.5f;
        
        private static VastcoreErrorHandler instance;
        public static VastcoreErrorHandler Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<VastcoreErrorHandler>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("VastcoreErrorHandler");
                        instance = go.AddComponent<VastcoreErrorHandler>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        private Dictionary<Type, int> errorCounts = new Dictionary<Type, int>();
        private Queue<ErrorReport> recentErrors = new Queue<ErrorReport>();
        private float lastMemoryCheck;
        
        public struct ErrorReport
        {
            public DateTime timestamp;
            public string errorType;
            public string message;
            public string stackTrace;
            public bool wasRecovered;
        }
        
        public enum ErrorSeverity
        {
            Low,      // 警告レベル
            Medium,   // 回復可能なエラー
            High,     // 重大なエラー
            Critical  // システム停止レベル
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeErrorHandler();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Update()
        {
            if (enableMemoryManagement && Time.time - lastMemoryCheck > memoryCheckInterval)
            {
                CheckMemoryUsage();
                lastMemoryCheck = Time.time;
            }
        }
        
        private void InitializeErrorHandler()
        {
            Application.logMessageReceived += HandleUnityLog;
            VastcoreLogger.Instance.LogInfo("VastcoreErrorHandler", "エラーハンドリングシステムが初期化されました");
        }
        
        /// <summary>
        /// 地形生成エラーのハンドリング
        /// </summary>
        public bool HandleTerrainGenerationError(Exception error, TerrainGenerationParams parameters, out GameObject fallbackTerrain)
        {
            fallbackTerrain = null;
            
            try
            {
                VastcoreLogger.Instance.LogError("TerrainGeneration", $"地形生成エラー: {error.Message}", error);
                RecordError(error, ErrorSeverity.Medium);
                
                if (!enableFallbackGeneration)
                {
                    return false;
                }
                
                // フォールバック地形の生成を試行
                for (int attempt = 0; attempt < maxRetryAttempts; attempt++)
                {
                    try
                    {
                        fallbackTerrain = GenerateFallbackTerrain(parameters, attempt);
                        if (fallbackTerrain != null)
                        {
                            VastcoreLogger.Instance.LogWarning("TerrainGeneration", 
                                $"フォールバック地形を生成しました (試行 {attempt + 1})");
                            return true;
                        }
                    }
                    catch (Exception fallbackError)
                    {
                        VastcoreLogger.Instance.LogError("TerrainGeneration", 
                            $"フォールバック地形生成失敗 (試行 {attempt + 1}): {fallbackError.Message}");
                    }
                }
                
                return false;
            }
            catch (Exception handlerError)
            {
                Debug.LogError($"エラーハンドラー自体でエラーが発生: {handlerError.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// メモリ不足時の自動調整
        /// </summary>
        public void HandleMemoryPressure()
        {
            try
            {
                VastcoreLogger.Instance.LogWarning("MemoryManagement", "メモリ不足を検出、自動調整を開始");
                
                // ガベージコレクションを強制実行
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();
                
                // 不要なオブジェクトを削除
                CleanupUnusedObjects();
                
                // 品質設定を下げる
                ReduceQualitySettings();
                
                VastcoreLogger.Instance.LogInfo("MemoryManagement", "メモリ自動調整が完了しました");
            }
            catch (Exception error)
            {
                VastcoreLogger.Instance.LogError("MemoryManagement", 
                    $"メモリ調整中にエラーが発生: {error.Message}", error);
            }
        }
        
        private void CheckMemoryUsage()
        {
            try
            {
                long memoryUsage = System.GC.GetTotalMemory(false) / (1024 * 1024); // MB
                
                if (memoryUsage > memoryThresholdMB)
                {
                    HandleMemoryPressure();
                }
            }
            catch (Exception error)
            {
                VastcoreLogger.Instance.LogError("MemoryManagement", 
                    $"メモリ使用量チェック中にエラー: {error.Message}");
            }
        }
        
        private GameObject GenerateFallbackTerrain(TerrainGenerationParams parameters, int attempt)
        {
            try
            {
                // 試行回数に応じて品質を下げる
                float qualityReduction = 1f - (attempt * 0.2f);
                qualityReduction = Mathf.Max(qualityReduction, 0.2f);

                // 基本的な地形オブジェクトを生成
                GameObject terrainObject = new GameObject($"FallbackTerrain_{attempt}");

                // Terrainコンポーネントの追加
                var terrain = terrainObject.AddComponent<Terrain>();
                var terrainCollider = terrainObject.AddComponent<TerrainCollider>();

                // TerrainDataの作成
                var terrainData = new TerrainData();
                terrainData.heightmapResolution = Mathf.RoundToInt(parameters.resolution * qualityReduction);
                terrainData.size = new Vector3(parameters.terrainSize * qualityReduction, parameters.heightScale, parameters.terrainSize * qualityReduction);

                // シンプルな平面地形データを生成
                float[,] heights = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {
                    for (int y = 0; y < terrainData.heightmapResolution; y++)
                    {
                        // 非常にシンプルな高さ（ほぼ平坦）
                        heights[x, y] = 0.1f + Random.Range(-0.05f, 0.05f) * qualityReduction;
                    }
                }
                terrainData.SetHeights(0, 0, heights);

                // Terrainにデータを設定
                terrain.terrainData = terrainData;
                terrainCollider.terrainData = terrainData;

                // 基本的なマテリアル設定
                var material = Resources.Load<Material>("Materials/TerrainMaterial");
                if (material != null)
                {
                    terrain.materialTemplate = material;
                }

                VastcoreLogger.Instance.LogInfo("FallbackTerrain", $"フォールバック地形生成完了 (試行 {attempt + 1}, 品質: {qualityReduction:F2})");

                return terrainObject;
            }
            catch (Exception error)
            {
                VastcoreLogger.Instance.LogError("FallbackTerrain", $"フォールバック地形生成中にエラー: {error.Message}");
                return null;
            }
        }
        
        private void CleanupUnusedObjects()
        {
            // 非アクティブなテレインタイルを削除
            var terrainTiles = FindObjectsByType<TerrainTile>(FindObjectsSortMode.None);
            foreach (var tile in terrainTiles)
            {
                if (!tile.isActive && Time.time - tile.lastAccessTime > 60f)
                {
                    DestroyImmediate(tile.gameObject);
                }
            }
            
            // リソースのアンロード
            Resources.UnloadUnusedAssets();
        }
        
        private void ReduceQualitySettings()
        {
            // 描画品質を下げる
            QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount - 1, 1);
            QualitySettings.shadowDistance *= 0.8f;
            QualitySettings.lodBias *= 0.8f;
            
            VastcoreLogger.Instance.LogInfo("QualityManagement", "品質設定を下げました");
        }
        
        private void RecordError(Exception error, ErrorSeverity severity)
        {
            var errorReport = new ErrorReport
            {
                timestamp = DateTime.Now,
                errorType = error.GetType().Name,
                message = error.Message,
                stackTrace = error.StackTrace,
                wasRecovered = enableErrorRecovery
            };
            
            recentErrors.Enqueue(errorReport);
            
            // 最新の100件のエラーのみ保持
            while (recentErrors.Count > 100)
            {
                recentErrors.Dequeue();
            }
            
            // エラー回数をカウント
            Type errorType = error.GetType();
            if (errorCounts.ContainsKey(errorType))
            {
                errorCounts[errorType]++;
            }
            else
            {
                errorCounts[errorType] = 1;
            }
        }
        
        private void HandleUnityLog(string logString, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception)
            {
                var errorReport = new ErrorReport
                {
                    timestamp = DateTime.Now,
                    errorType = type.ToString(),
                    message = logString,
                    stackTrace = stackTrace,
                    wasRecovered = false
                };
                
                recentErrors.Enqueue(errorReport);
            }
        }
        
        /// <summary>
        /// エラー統計の取得
        /// </summary>
        public Dictionary<Type, int> GetErrorStatistics()
        {
            return new Dictionary<Type, int>(errorCounts);
        }
        
        /// <summary>
        /// 最近のエラーレポートの取得
        /// </summary>
        public ErrorReport[] GetRecentErrors()
        {
            return recentErrors.ToArray();
        }
        
        /// <summary>
        /// エラーハンドラーのリセット
        /// </summary>
        public void ResetErrorHandler()
        {
            errorCounts.Clear();
            recentErrors.Clear();
            VastcoreLogger.Instance.LogInfo("ErrorHandler", "エラーハンドラーがリセットされました");
        }
    }
    
    // 地形生成パラメータの定義（他のスクリプトとの互換性のため）
    [System.Serializable]
    public class TerrainGenerationParams
    {
        public float terrainSize = 1000f;
        public int resolution = 256;
        public float heightScale = 100f;
        public float noiseScale = 0.01f;
        public int octaves = 4;
        public float persistence = 0.5f;
        public float lacunarity = 2f;
    }
    
    // テレインタイルの基本定義
    public class TerrainTile : MonoBehaviour
    {
        public bool isActive = true;
        public float lastAccessTime;
        
        private void Start()
        {
            lastAccessTime = Time.time;
        }
    }
    
    // プリミティブ地形オブジェクトの基本定義
    public class PrimitiveTerrainObject : MonoBehaviour
    {
        // 基本的なプリミティブオブジェクトの実装
    }
}