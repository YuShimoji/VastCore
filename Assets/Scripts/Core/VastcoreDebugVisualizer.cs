using System;
using System.Collections.Generic;
using UnityEngine;
using Vastcore.Core;

namespace Vastcore.Core
{
    /// <summary>
    /// Vastcore専用のデバッグ情報可視化システム
    /// 地形生成、プリミティブ配置、パフォーマンス情報の視覚的デバッグ機能
    /// </summary>
    public class VastcoreDebugVisualizer : MonoBehaviour
    {
        [Header("デバッグ表示設定")]
        public bool enableDebugVisualization = true;
        public bool showTerrainDebugInfo = true;
        public bool showPrimitiveDebugInfo = true;
        public bool showPerformanceDebugInfo = true;
        public bool showMemoryDebugInfo = true;
        
        [Header("視覚化設定")]
        public Color terrainBoundsColor = Color.green;
        public Color primitiveSpawnPointColor = Color.red;
        public Color errorLocationColor = Color.magenta;
        public Color performanceWarningColor = Color.yellow;
        
        [Header("UI設定")]
        public KeyCode toggleDebugKey = KeyCode.F11;
        public bool showDebugUI = true;
        public int maxDebugEntries = 50;
        
        [Header("Gizmo設定")]
        public bool showGizmosInSceneView = true;
        public bool showGizmosInGameView = false;
        public float gizmoScale = 1f;
        
        private static VastcoreDebugVisualizer instance;
        public static VastcoreDebugVisualizer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<VastcoreDebugVisualizer>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("VastcoreDebugVisualizer");
                        instance = go.AddComponent<VastcoreDebugVisualizer>();
                    }
                }
                return instance;
            }
        }
        
        // デバッグ情報の構造体
        [System.Serializable]
        public class DebugInfo
        {
            public DateTime timestamp;
            public string category;
            public string message;
            public Vector3 worldPosition;
            public Color visualColor;
            public float duration;
            public DebugInfoType type;
        }
        
        public enum DebugInfoType
        {
            TerrainGeneration,
            PrimitiveSpawn,
            Error,
            Performance,
            Memory,
            General
        }
        
        private List<DebugInfo> debugInfoList = new List<DebugInfo>();
        private Dictionary<string, float> performanceMetrics = new Dictionary<string, float>();
        private Dictionary<string, int> errorCounts = new Dictionary<string, int>();
        private bool showDebugOverlay = false;
        private Vector2 debugScrollPosition;
        
        // パフォーマンス監視
        private float lastFrameTime;
        private float averageFrameTime;
        private int frameCount;
        private float memoryUsage;
        private float lastMemoryCheck;
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDebugVisualizer();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(toggleDebugKey))
            {
                showDebugOverlay = !showDebugOverlay;
            }
            
            UpdatePerformanceMetrics();
            CleanupOldDebugInfo();
        }
        
        private void OnGUI()
        {
            if (showDebugOverlay && showDebugUI)
            {
                DrawDebugOverlay();
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!enableDebugVisualization) return;
            if (!showGizmosInSceneView && !Application.isPlaying) return;
            if (!showGizmosInGameView && Application.isPlaying) return;
            
            DrawDebugGizmos();
        }
        
        private void InitializeDebugVisualizer()
        {
            VastcoreLogger.Instance.LogInfo("DebugVisualizer", "デバッグ可視化システムが初期化されました");
        }
        
        /// <summary>
        /// 地形生成デバッグ情報の追加
        /// </summary>
        public void AddTerrainDebugInfo(string message, Vector3 position, float duration = 5f)
        {
            if (!showTerrainDebugInfo) return;
            
            var debugInfo = new DebugInfo
            {
                timestamp = DateTime.Now,
                category = "Terrain",
                message = message,
                worldPosition = position,
                visualColor = terrainBoundsColor,
                duration = duration,
                type = DebugInfoType.TerrainGeneration
            };
            
            AddDebugInfo(debugInfo);
        }
        
        /// <summary>
        /// プリミティブ配置デバッグ情報の追加
        /// </summary>
        public void AddPrimitiveDebugInfo(string message, Vector3 position, float duration = 5f)
        {
            if (!showPrimitiveDebugInfo) return;
            
            var debugInfo = new DebugInfo
            {
                timestamp = DateTime.Now,
                category = "Primitive",
                message = message,
                worldPosition = position,
                visualColor = primitiveSpawnPointColor,
                duration = duration,
                type = DebugInfoType.PrimitiveSpawn
            };
            
            AddDebugInfo(debugInfo);
        }
        
        /// <summary>
        /// エラー位置デバッグ情報の追加
        /// </summary>
        public void AddErrorDebugInfo(string message, Vector3 position, float duration = 10f)
        {
            var debugInfo = new DebugInfo
            {
                timestamp = DateTime.Now,
                category = "Error",
                message = message,
                worldPosition = position,
                visualColor = errorLocationColor,
                duration = duration,
                type = DebugInfoType.Error
            };
            
            AddDebugInfo(debugInfo);
            
            // エラー回数をカウント
            string errorKey = message.Split(':')[0]; // エラーメッセージの最初の部分をキーとする
            if (errorCounts.ContainsKey(errorKey))
            {
                errorCounts[errorKey]++;
            }
            else
            {
                errorCounts[errorKey] = 1;
            }
        }
        
        /// <summary>
        /// パフォーマンス警告デバッグ情報の追加
        /// </summary>
        public void AddPerformanceWarning(string message, Vector3 position, float duration = 3f)
        {
            if (!showPerformanceDebugInfo) return;
            
            var debugInfo = new DebugInfo
            {
                timestamp = DateTime.Now,
                category = "Performance",
                message = message,
                worldPosition = position,
                visualColor = performanceWarningColor,
                duration = duration,
                type = DebugInfoType.Performance
            };
            
            AddDebugInfo(debugInfo);
        }
        
        /// <summary>
        /// 一般的なデバッグ情報の追加
        /// </summary>
        public void AddGeneralDebugInfo(string category, string message, Vector3 position, Color color, float duration = 5f)
        {
            var debugInfo = new DebugInfo
            {
                timestamp = DateTime.Now,
                category = category,
                message = message,
                worldPosition = position,
                visualColor = color,
                duration = duration,
                type = DebugInfoType.General
            };
            
            AddDebugInfo(debugInfo);
        }
        
        private void AddDebugInfo(DebugInfo info)
        {
            debugInfoList.Add(info);
            
            // 最大エントリー数の制限
            while (debugInfoList.Count > maxDebugEntries)
            {
                debugInfoList.RemoveAt(0);
            }
            
            VastcoreLogger.Instance.LogDebug("DebugVisualizer", 
                $"[{info.category}] {info.message} at {info.worldPosition}");
        }
        
        private void UpdatePerformanceMetrics()
        {
            // フレーム時間の計算
            lastFrameTime = Time.deltaTime * 1000f; // ms
            frameCount++;
            averageFrameTime = (averageFrameTime * (frameCount - 1) + lastFrameTime) / frameCount;
            
            // メモリ使用量の更新（1秒ごと）
            if (Time.time - lastMemoryCheck > 1f)
            {
                memoryUsage = System.GC.GetTotalMemory(false) / (1024f * 1024f); // MB
                lastMemoryCheck = Time.time;
                
                // パフォーマンス警告のチェック
                CheckPerformanceWarnings();
            }
            
            // パフォーマンスメトリクスの更新
            performanceMetrics["FrameTime"] = lastFrameTime;
            performanceMetrics["AverageFrameTime"] = averageFrameTime;
            performanceMetrics["FPS"] = 1f / Time.deltaTime;
            performanceMetrics["MemoryUsage"] = memoryUsage;
        }
        
        private void CheckPerformanceWarnings()
        {
            Vector3 cameraPosition = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            
            // フレーム時間警告
            if (lastFrameTime > 33.33f) // 30FPS以下
            {
                AddPerformanceWarning($"Low FPS: {1f / Time.deltaTime:F1}", cameraPosition);
            }
            
            // メモリ使用量警告
            if (memoryUsage > 1024f) // 1GB以上
            {
                AddPerformanceWarning($"High Memory Usage: {memoryUsage:F1}MB", cameraPosition);
            }
        }
        
        private void CleanupOldDebugInfo()
        {
            float currentTime = Time.time;
            debugInfoList.RemoveAll(info => 
                (DateTime.Now - info.timestamp).TotalSeconds > info.duration);
        }
        
        private void DrawDebugGizmos()
        {
            foreach (var info in debugInfoList)
            {
                Gizmos.color = info.visualColor;
                
                switch (info.type)
                {
                    case DebugInfoType.TerrainGeneration:
                        DrawTerrainGizmo(info);
                        break;
                    case DebugInfoType.PrimitiveSpawn:
                        DrawPrimitiveGizmo(info);
                        break;
                    case DebugInfoType.Error:
                        DrawErrorGizmo(info);
                        break;
                    case DebugInfoType.Performance:
                        DrawPerformanceGizmo(info);
                        break;
                    default:
                        DrawGeneralGizmo(info);
                        break;
                }
            }
        }
        
        private void DrawTerrainGizmo(DebugInfo info)
        {
            // 地形境界を表示
            Gizmos.DrawWireCube(info.worldPosition, Vector3.one * 100f * gizmoScale);
            Gizmos.DrawSphere(info.worldPosition, 5f * gizmoScale);
        }
        
        private void DrawPrimitiveGizmo(DebugInfo info)
        {
            // プリミティブ配置点を表示
            Gizmos.DrawSphere(info.worldPosition, 10f * gizmoScale);
            Gizmos.DrawLine(info.worldPosition, info.worldPosition + Vector3.up * 50f * gizmoScale);
        }
        
        private void DrawErrorGizmo(DebugInfo info)
        {
            // エラー位置を強調表示
            Gizmos.DrawWireSphere(info.worldPosition, 20f * gizmoScale);
            
            // X印を描画
            Vector3 pos = info.worldPosition;
            float size = 15f * gizmoScale;
            Gizmos.DrawLine(pos + new Vector3(-size, 0, -size), pos + new Vector3(size, 0, size));
            Gizmos.DrawLine(pos + new Vector3(-size, 0, size), pos + new Vector3(size, 0, -size));
        }
        
        private void DrawPerformanceGizmo(DebugInfo info)
        {
            // パフォーマンス警告を表示
            Gizmos.DrawWireCube(info.worldPosition, Vector3.one * 30f * gizmoScale);
        }
        
        private void DrawGeneralGizmo(DebugInfo info)
        {
            // 一般的なデバッグ情報を表示
            Gizmos.DrawSphere(info.worldPosition, 8f * gizmoScale);
        }
        
        private void DrawDebugOverlay()
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;
            float overlayWidth = 400f;
            float overlayHeight = screenHeight * 0.8f;
            
            GUILayout.BeginArea(new Rect(screenWidth - overlayWidth - 10, 10, overlayWidth, overlayHeight));
            GUILayout.BeginVertical("box");
            
            // ヘッダー
            GUILayout.BeginHorizontal();
            GUILayout.Label("Vastcore Debug Info", GUILayout.Width(200));
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                debugInfoList.Clear();
                errorCounts.Clear();
            }
            if (GUILayout.Button("Close", GUILayout.Width(60)))
            {
                showDebugOverlay = false;
            }
            GUILayout.EndHorizontal();
            
            // パフォーマンス情報
            if (showPerformanceDebugInfo)
            {
                GUILayout.Label("=== Performance ===");
                GUILayout.Label($"FPS: {performanceMetrics.GetValueOrDefault("FPS", 0):F1}");
                GUILayout.Label($"Frame Time: {performanceMetrics.GetValueOrDefault("FrameTime", 0):F2}ms");
                GUILayout.Label($"Avg Frame Time: {performanceMetrics.GetValueOrDefault("AverageFrameTime", 0):F2}ms");
                GUILayout.Space(10);
            }
            
            // メモリ情報
            if (showMemoryDebugInfo)
            {
                GUILayout.Label("=== Memory ===");
                GUILayout.Label($"Memory Usage: {performanceMetrics.GetValueOrDefault("MemoryUsage", 0):F1}MB");
                GUILayout.Label($"GC Memory: {System.GC.GetTotalMemory(false) / (1024 * 1024):F1}MB");
                GUILayout.Space(10);
            }
            
            // エラー統計
            if (errorCounts.Count > 0)
            {
                GUILayout.Label("=== Error Statistics ===");
                foreach (var error in errorCounts)
                {
                    GUILayout.Label($"{error.Key}: {error.Value}");
                }
                GUILayout.Space(10);
            }
            
            // デバッグ情報リスト
            GUILayout.Label("=== Debug Info ===");
            debugScrollPosition = GUILayout.BeginScrollView(debugScrollPosition);
            
            foreach (var info in debugInfoList)
            {
                Color originalColor = GUI.color;
                GUI.color = info.visualColor;
                
                GUILayout.BeginVertical("box");
                GUILayout.Label($"[{info.timestamp:HH:mm:ss}] [{info.category}]");
                GUILayout.Label(info.message);
                GUILayout.Label($"Position: {info.worldPosition}");
                GUILayout.EndVertical();
                
                GUI.color = originalColor;
            }
            
            GUILayout.EndScrollView();
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// デバッグ情報のクリア
        /// </summary>
        public void ClearDebugInfo()
        {
            debugInfoList.Clear();
            errorCounts.Clear();
            VastcoreLogger.Instance.LogInfo("DebugVisualizer", "デバッグ情報をクリアしました");
        }
        
        /// <summary>
        /// デバッグ情報の取得
        /// </summary>
        public List<DebugInfo> GetDebugInfo()
        {
            return new List<DebugInfo>(debugInfoList);
        }
        
        /// <summary>
        /// パフォーマンスメトリクスの取得
        /// </summary>
        public Dictionary<string, float> GetPerformanceMetrics()
        {
            return new Dictionary<string, float>(performanceMetrics);
        }
        
        /// <summary>
        /// エラー統計の取得
        /// </summary>
        public Dictionary<string, int> GetErrorStatistics()
        {
            return new Dictionary<string, int>(errorCounts);
        }
        
        /// <summary>
        /// デバッグレポートの生成
        /// </summary>
        public string GenerateDebugReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine($"=== Vastcore Debug Report - {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            report.AppendLine();
            
            // パフォーマンス情報
            report.AppendLine("=== Performance Metrics ===");
            foreach (var metric in performanceMetrics)
            {
                report.AppendLine($"{metric.Key}: {metric.Value:F2}");
            }
            report.AppendLine();
            
            // エラー統計
            if (errorCounts.Count > 0)
            {
                report.AppendLine("=== Error Statistics ===");
                foreach (var error in errorCounts)
                {
                    report.AppendLine($"{error.Key}: {error.Value} occurrences");
                }
                report.AppendLine();
            }
            
            // 最近のデバッグ情報
            report.AppendLine("=== Recent Debug Info ===");
            foreach (var info in debugInfoList)
            {
                report.AppendLine($"[{info.timestamp:HH:mm:ss}] [{info.category}] {info.message} at {info.worldPosition}");
            }
            
            return report.ToString();
        }
    }
}