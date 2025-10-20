using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Vastcore.Generation.GPU
{
    /// <summary>
    /// GPU並列処理のパフォーマンス監視システム
    /// </summary>
    public class GPUPerformanceMonitor : MonoBehaviour
    {
        [Header("監視設定")]
        [SerializeField] private bool enableMonitoring = true;
        [SerializeField] private float updateInterval = 1f;
        [SerializeField] private int maxSampleCount = 60;
        
        [Header("パフォーマンス閾値")]
        [SerializeField] private float targetFrameTime = 16.67f; // 60FPS
        [SerializeField] private float maxGPUMemoryUsage = 512f; // MB
        [SerializeField] private int maxConcurrentOperations = 8;
        
        // パフォーマンス統計
        private List<float> frameTimeSamples;
        private List<float> gpuMemorySamples;
        private List<int> generationCountSamples;
        
        // 監視対象
        private GPUTerrainGenerator terrainGenerator;
        private float lastUpdateTime;
        
#if UNITY_2021_2_OR_NEWER
        private ProfilerRecorder gpuMemoryRecorder;
        private bool gpuMemoryRecorderUnavailable;
#endif
        
        // 統計データ
        public struct PerformanceStats
        {
            public float averageFrameTime;
            public float maxFrameTime;
            public float minFrameTime;
            public float averageGPUMemory;
            public float maxGPUMemory;
            public int averageGenerationCount;
            public int maxGenerationCount;
            public bool isPerformanceGood;
        }
        
        private void Awake()
        {
            frameTimeSamples = new List<float>();
            gpuMemorySamples = new List<float>();
            generationCountSamples = new List<int>();
            
#if UNITY_2022_2_OR_NEWER
            terrainGenerator = FindFirstObjectByType<GPUTerrainGenerator>();
#else
            terrainGenerator = FindObjectOfType<GPUTerrainGenerator>();
#endif
            if (terrainGenerator == null)
            {
                Debug.LogWarning("GPUTerrainGenerator not found. Performance monitoring disabled.");
                enableMonitoring = false;
            }
        }

        private void OnEnable()
        {
#if UNITY_2021_2_OR_NEWER
            StartGpuMemoryRecorderIfNeeded();
#endif
        }

        private void OnDisable()
        {
#if UNITY_2021_2_OR_NEWER
            if (gpuMemoryRecorder.Valid)
            {
                gpuMemoryRecorder.Dispose();
            }
#endif
        }
        
        private void Update()
        {
            if (!enableMonitoring) return;
            
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                CollectPerformanceData();
                lastUpdateTime = Time.time;
            }
        }
        
        private void CollectPerformanceData()
        {
            // フレーム時間の収集
            float frameTime = Time.deltaTime * 1000f; // ms
            AddSample(frameTimeSamples, frameTime);
            
            // GPU メモリ使用量の収集
            float gpuMemory = GetGPUMemoryUsage();
            AddSample(gpuMemorySamples, gpuMemory);
            
            // 生成処理数の収集
            if (terrainGenerator != null)
            {
                var perfInfo = terrainGenerator.GetPerformanceInfo();
                int totalOperations = perfInfo.activeGenerations + perfInfo.queuedGenerations;
                AddSample(generationCountSamples, totalOperations);
                
                // パフォーマンス調整の提案
                if (frameTime > targetFrameTime * 1.5f)
                {
                    SuggestPerformanceOptimization(perfInfo);
                }
            }
        }
        
        private void AddSample<T>(List<T> samples, T value)
        {
            samples.Add(value);
            if (samples.Count > maxSampleCount)
            {
                samples.RemoveAt(0);
            }
        }
        
        private float GetGPUMemoryUsage()
        {
#if UNITY_2021_2_OR_NEWER
            StartGpuMemoryRecorderIfNeeded();

            if (gpuMemoryRecorder.Valid)
            {
                return gpuMemoryRecorder.LastValue / (1024f * 1024f);
            }
#endif
            // Fallback: グラフィックスメモリ総量を最大値として使用
            return SystemInfo.graphicsMemorySize;
        }
        
        private void SuggestPerformanceOptimization(GPUTerrainGenerator.GPUPerformanceInfo perfInfo)
        {
            if (perfInfo.activeGenerations > maxConcurrentOperations / 2)
            {
                Debug.LogWarning($"High GPU load detected. Consider reducing concurrent generations from {perfInfo.activeGenerations} to {maxConcurrentOperations / 2}");
            }
            
            if (perfInfo.textureResolution > 512 && frameTimeSamples.LastOrDefault() > targetFrameTime * 2f)
            {
                Debug.LogWarning($"Consider reducing texture resolution from {perfInfo.textureResolution} to improve performance");
            }
        }
        
        /// <summary>
        /// 現在のパフォーマンス統計を取得
        /// </summary>
        public PerformanceStats GetCurrentStats()
        {
            var stats = new PerformanceStats();
            
            if (frameTimeSamples.Count > 0)
            {
                stats.averageFrameTime = frameTimeSamples.Average();
                stats.maxFrameTime = frameTimeSamples.Max();
                stats.minFrameTime = frameTimeSamples.Min();
            }
            
            if (gpuMemorySamples.Count > 0)
            {
                stats.averageGPUMemory = gpuMemorySamples.Average();
                stats.maxGPUMemory = gpuMemorySamples.Max();
            }
            
            if (generationCountSamples.Count > 0)
            {
                stats.averageGenerationCount = (int)generationCountSamples.Average();
                stats.maxGenerationCount = generationCountSamples.Max();
            }
            
            // パフォーマンス評価
            stats.isPerformanceGood = stats.averageFrameTime < targetFrameTime * 1.2f && 
                                     stats.averageGPUMemory < maxGPUMemoryUsage * 0.8f;
            
            return stats;
        }
        
        /// <summary>
        /// パフォーマンス統計をログ出力
        /// </summary>
        public void LogPerformanceStats()
        {
            var stats = GetCurrentStats();
            
            Debug.Log($"GPU Performance Stats:\n" +
                     $"Frame Time: Avg={stats.averageFrameTime:F2}ms, Max={stats.maxFrameTime:F2}ms, Min={stats.minFrameTime:F2}ms\n" +
                     $"GPU Memory: Avg={stats.averageGPUMemory:F1}MB, Max={stats.maxGPUMemory:F1}MB\n" +
                     $"Generations: Avg={stats.averageGenerationCount}, Max={stats.maxGenerationCount}\n" +
                     $"Performance Good: {stats.isPerformanceGood}");
        }
        
        /// <summary>
        /// 自動パフォーマンス調整
        /// </summary>
        public void AutoOptimizePerformance()
        {
            var stats = GetCurrentStats();
            
            if (!stats.isPerformanceGood && terrainGenerator != null)
            {
                var perfInfo = terrainGenerator.GetPerformanceInfo();
                
                // 同時生成数の調整
                if (stats.averageFrameTime > targetFrameTime * 1.5f)
                {
                    int newMaxConcurrent = Mathf.Max(1, perfInfo.maxConcurrentGenerations - 1);
                    Debug.Log($"Auto-optimizing: Reducing max concurrent generations to {newMaxConcurrent}");
                    // terrainGenerator.SetMaxConcurrentGenerations(newMaxConcurrent);
                }
                
                // テクスチャ解像度の調整
                if (stats.averageGPUMemory > maxGPUMemoryUsage * 0.9f)
                {
                    int newResolution = Mathf.Max(256, perfInfo.textureResolution / 2);
                    Debug.Log($"Auto-optimizing: Reducing texture resolution to {newResolution}");
                    // terrainGenerator.SetTextureResolution(newResolution);
                }
            }
        }
        
        private void OnGUI()
        {
            if (!enableMonitoring) return;
            
            var stats = GetCurrentStats();
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");
            
#if UNITY_EDITOR
            GUIStyle headerStyle = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).label;
#else
            GUIStyle headerStyle = GUI.skin.label;
#endif
            GUILayout.Label("GPU Performance Monitor", headerStyle);
            
            GUILayout.Space(5);
            
            GUILayout.Label($"Frame Time: {stats.averageFrameTime:F1}ms (Target: {targetFrameTime:F1}ms)");
            GUILayout.Label($"GPU Memory: {stats.averageGPUMemory:F1}MB / {maxGPUMemoryUsage:F1}MB");
            GUILayout.Label($"Active Generations: {stats.averageGenerationCount}");
            
            GUILayout.Space(5);
            
            Color originalColor = GUI.color;
            GUI.color = stats.isPerformanceGood ? Color.green : Color.red;
            GUILayout.Label($"Performance: {(stats.isPerformanceGood ? "GOOD" : "POOR")}");
            GUI.color = originalColor;
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Log Stats"))
            {
                LogPerformanceStats();
            }
            
            if (GUILayout.Button("Auto Optimize"))
            {
                AutoOptimizePerformance();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

#if UNITY_2021_2_OR_NEWER
        private void StartGpuMemoryRecorderIfNeeded()
        {
            if (!enableMonitoring || gpuMemoryRecorderUnavailable)
            {
                return;
            }

            if (gpuMemoryRecorder.Valid)
            {
                return;
            }

            try
            {
                gpuMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Graphics Used Memory", 1);
            }
            catch (System.ArgumentException)
            {
                gpuMemoryRecorderUnavailable = true;
                Debug.LogWarning("GPUPerformanceMonitor: Graphics Used Memory profiler recorder is unavailable. GPU memory metrics will use SystemInfo.graphicsMemorySize as a fallback.");
            }
        }
#endif
    }
}