using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using TMPro;

namespace Vastcore.UI
{
    /// <summary>
    /// Performance monitoring system for tracking terrain generation and system performance
    /// Provides real-time metrics and performance optimization suggestions
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        [Header("Monitoring Settings")]
        [SerializeField] private bool enableMonitoring = true;
        [SerializeField] private float updateInterval = 0.5f;
        [SerializeField] private int historySize = 120; // 60 seconds at 0.5s intervals
        
        [Header("Performance Thresholds")]
        [SerializeField] private float targetFPS = 60f;
        [SerializeField] private float warningFPS = 45f;
        [SerializeField] private float criticalFPS = 30f;
        [SerializeField] private float maxMemoryMB = 1024f;
        [SerializeField] private float warningMemoryMB = 768f;
        
        [Header("Terrain Generation Monitoring")]
        [SerializeField] private bool monitorTerrainGeneration = true;
        [SerializeField] private bool monitorPrimitiveGeneration = true;
        [SerializeField] private bool monitorUIUpdates = true;
        
        // Performance data
        private List<float> fpsHistory = new List<float>();
        private List<float> memoryHistory = new List<float>();
        private List<float> frameTimeHistory = new List<float>();
        private List<int> drawCallHistory = new List<int>();
        
        // Terrain generation metrics
        private Dictionary<string, PerformanceMetric> terrainMetrics = new Dictionary<string, PerformanceMetric>();
        private Dictionary<string, PerformanceMetric> primitiveMetrics = new Dictionary<string, PerformanceMetric>();
        private Dictionary<string, PerformanceMetric> uiMetrics = new Dictionary<string, PerformanceMetric>();
        
        // Current performance state
        private PerformanceState currentState = PerformanceState.Good;
        private List<string> activeWarnings = new List<string>();
        private List<string> optimizationSuggestions = new List<string>();
        
        // Events
        public System.Action<PerformanceState> OnPerformanceStateChanged;
        public System.Action<List<string>> OnWarningsUpdated;
        public System.Action<PerformanceReport> OnPerformanceReportGenerated;
        
        private void Start()
        {
            if (enableMonitoring)
            {
                StartCoroutine(MonitoringCoroutine());
            }
        }
        
        private void OnDestroy()
        {
            StopAllCoroutines();
        }
        
        private IEnumerator MonitoringCoroutine()
        {
            while (enableMonitoring)
            {
                CollectPerformanceData();
                AnalyzePerformance();
                GenerateOptimizationSuggestions();
                
                yield return new WaitForSeconds(updateInterval);
            }
        }
        
        private void CollectPerformanceData()
        {
            // Collect FPS data
            float currentFPS = 1f / Time.deltaTime;
            fpsHistory.Add(currentFPS);
            
            // Collect frame time data
            float frameTime = Time.deltaTime * 1000f; // Convert to milliseconds
            frameTimeHistory.Add(frameTime);
            
            // Collect memory data
            float memoryMB = System.GC.GetTotalMemory(false) / (1024f * 1024f);
            memoryHistory.Add(memoryMB);
            
            // Collect draw call data (approximation)
            int drawCalls = UnityEngine.Rendering.DebugUI.instance != null ? 
                           UnityEngine.Rendering.DebugUI.instance.GetHashCode() : 0; // Placeholder
            drawCallHistory.Add(drawCalls);
            
            // Maintain history size
            TrimHistory(fpsHistory);
            TrimHistory(memoryHistory);
            TrimHistory(frameTimeHistory);
            TrimHistory(drawCallHistory);
        }
        
        private void TrimHistory<T>(List<T> history)
        {
            while (history.Count > historySize)
            {
                history.RemoveAt(0);
            }
        }
        
        private void AnalyzePerformance()
        {
            activeWarnings.Clear();
            
            // Analyze FPS
            if (fpsHistory.Count > 0)
            {
                float avgFPS = CalculateAverage(fpsHistory);
                
                if (avgFPS < criticalFPS)
                {
                    currentState = PerformanceState.Critical;
                    activeWarnings.Add($"Critical FPS: {avgFPS:F1} (Target: {targetFPS:F1})");
                }
                else if (avgFPS < warningFPS)
                {
                    currentState = PerformanceState.Warning;
                    activeWarnings.Add($"Low FPS: {avgFPS:F1} (Target: {targetFPS:F1})");
                }
                else if (currentState != PerformanceState.Critical)
                {
                    currentState = PerformanceState.Good;
                }
            }
            
            // Analyze memory usage
            if (memoryHistory.Count > 0)
            {
                float avgMemory = CalculateAverage(memoryHistory);
                
                if (avgMemory > maxMemoryMB)
                {
                    if (currentState != PerformanceState.Critical)
                        currentState = PerformanceState.Warning;
                    activeWarnings.Add($"High Memory Usage: {avgMemory:F1} MB (Max: {maxMemoryMB:F1} MB)");
                }
                else if (avgMemory > warningMemoryMB)
                {
                    activeWarnings.Add($"Elevated Memory Usage: {avgMemory:F1} MB");
                }
            }
            
            // Analyze frame time consistency
            if (frameTimeHistory.Count > 10)
            {
                float frameTimeVariance = CalculateVariance(frameTimeHistory);
                if (frameTimeVariance > 5f) // 5ms variance threshold
                {
                    activeWarnings.Add($"Inconsistent Frame Times (Variance: {frameTimeVariance:F1}ms)");
                }
            }
            
            // Trigger events
            OnPerformanceStateChanged?.Invoke(currentState);
            OnWarningsUpdated?.Invoke(new List<string>(activeWarnings));
        }
        
        private void GenerateOptimizationSuggestions()
        {
            optimizationSuggestions.Clear();
            
            if (currentState == PerformanceState.Warning || currentState == PerformanceState.Critical)
            {
                // FPS-based suggestions
                if (fpsHistory.Count > 0 && CalculateAverage(fpsHistory) < targetFPS)
                {
                    optimizationSuggestions.Add("Consider reducing terrain generation frequency");
                    optimizationSuggestions.Add("Enable LOD system for primitive objects");
                    optimizationSuggestions.Add("Reduce UI update frequency");
                }
                
                // Memory-based suggestions
                if (memoryHistory.Count > 0 && CalculateAverage(memoryHistory) > warningMemoryMB)
                {
                    optimizationSuggestions.Add("Enable object pooling for primitives");
                    optimizationSuggestions.Add("Reduce terrain tile cache size");
                    optimizationSuggestions.Add("Force garbage collection");
                }
                
                // Frame time variance suggestions
                if (frameTimeHistory.Count > 10 && CalculateVariance(frameTimeHistory) > 5f)
                {
                    optimizationSuggestions.Add("Enable frame time limiting for generation tasks");
                    optimizationSuggestions.Add("Implement better load balancing");
                }
            }
        }
        
        /// <summary>
        /// Records a terrain generation performance metric
        /// </summary>
        public void RecordTerrainMetric(string operationName, float executionTime, int objectsGenerated = 1)
        {
            if (!monitorTerrainGeneration) return;
            
            RecordMetric(terrainMetrics, operationName, executionTime, objectsGenerated);
        }
        
        /// <summary>
        /// Records a primitive generation performance metric
        /// </summary>
        public void RecordPrimitiveMetric(string operationName, float executionTime, int objectsGenerated = 1)
        {
            if (!monitorPrimitiveGeneration) return;
            
            RecordMetric(primitiveMetrics, operationName, executionTime, objectsGenerated);
        }
        
        /// <summary>
        /// Records a UI update performance metric
        /// </summary>
        public void RecordUIMetric(string operationName, float executionTime, int updatesProcessed = 1)
        {
            if (!monitorUIUpdates) return;
            
            RecordMetric(uiMetrics, operationName, executionTime, updatesProcessed);
        }
        
        private void RecordMetric(Dictionary<string, PerformanceMetric> metrics, string operationName, float executionTime, int objectCount)
        {
            if (!metrics.ContainsKey(operationName))
            {
                metrics[operationName] = new PerformanceMetric
                {
                    operationName = operationName,
                    executionTimes = new List<float>(),
                    objectCounts = new List<int>()
                };
            }
            
            var metric = metrics[operationName];
            metric.executionTimes.Add(executionTime);
            metric.objectCounts.Add(objectCount);
            metric.totalExecutions++;
            metric.lastExecutionTime = Time.time;
            
            // Maintain history size
            TrimHistory(metric.executionTimes);
            TrimHistory(metric.objectCounts);
        }
        
        /// <summary>
        /// Generates a comprehensive performance report
        /// </summary>
        public PerformanceReport GenerateReport()
        {
            var report = new PerformanceReport
            {
                timestamp = System.DateTime.Now,
                currentState = currentState,
                warnings = new List<string>(activeWarnings),
                suggestions = new List<string>(optimizationSuggestions)
            };
            
            // System metrics
            if (fpsHistory.Count > 0)
            {
                report.averageFPS = CalculateAverage(fpsHistory);
                report.minFPS = CalculateMin(fpsHistory);
                report.maxFPS = CalculateMax(fpsHistory);
            }
            
            if (memoryHistory.Count > 0)
            {
                report.averageMemoryMB = CalculateAverage(memoryHistory);
                report.peakMemoryMB = CalculateMax(memoryHistory);
            }
            
            if (frameTimeHistory.Count > 0)
            {
                report.averageFrameTimeMS = CalculateAverage(frameTimeHistory);
                report.frameTimeVarianceMS = CalculateVariance(frameTimeHistory);
            }
            
            // Operation metrics
            report.terrainMetrics = GenerateMetricSummaries(terrainMetrics);
            report.primitiveMetrics = GenerateMetricSummaries(primitiveMetrics);
            report.uiMetrics = GenerateMetricSummaries(uiMetrics);
            
            OnPerformanceReportGenerated?.Invoke(report);
            return report;
        }
        
        private List<MetricSummary> GenerateMetricSummaries(Dictionary<string, PerformanceMetric> metrics)
        {
            var summaries = new List<MetricSummary>();
            
            foreach (var kvp in metrics)
            {
                var metric = kvp.Value;
                var summary = new MetricSummary
                {
                    operationName = metric.operationName,
                    totalExecutions = metric.totalExecutions,
                    averageExecutionTime = CalculateAverage(metric.executionTimes),
                    minExecutionTime = CalculateMin(metric.executionTimes),
                    maxExecutionTime = CalculateMax(metric.executionTimes),
                    averageObjectCount = CalculateAverage(metric.objectCounts.ConvertAll(x => (float)x)),
                    lastExecutionTime = metric.lastExecutionTime
                };
                
                summaries.Add(summary);
            }
            
            return summaries;
        }
        
        // Utility methods
        private float CalculateAverage(List<float> values)
        {
            if (values.Count == 0) return 0f;
            
            float sum = 0f;
            foreach (float value in values)
            {
                sum += value;
            }
            return sum / values.Count;
        }
        
        private float CalculateMin(List<float> values)
        {
            if (values.Count == 0) return 0f;
            
            float min = float.MaxValue;
            foreach (float value in values)
            {
                if (value < min) min = value;
            }
            return min;
        }
        
        private float CalculateMax(List<float> values)
        {
            if (values.Count == 0) return 0f;
            
            float max = float.MinValue;
            foreach (float value in values)
            {
                if (value > max) max = value;
            }
            return max;
        }
        
        private float CalculateVariance(List<float> values)
        {
            if (values.Count < 2) return 0f;
            
            float average = CalculateAverage(values);
            float sumSquaredDifferences = 0f;
            
            foreach (float value in values)
            {
                float difference = value - average;
                sumSquaredDifferences += difference * difference;
            }
            
            return sumSquaredDifferences / (values.Count - 1);
        }
        
        // Public properties
        public PerformanceState CurrentState => currentState;
        public List<string> ActiveWarnings => new List<string>(activeWarnings);
        public List<string> OptimizationSuggestions => new List<string>(optimizationSuggestions);
        public bool IsMonitoring => enableMonitoring;
        
        public void SetMonitoring(bool enabled)
        {
            enableMonitoring = enabled;
            
            if (enabled && !IsInvoking(nameof(MonitoringCoroutine)))
            {
                StartCoroutine(MonitoringCoroutine());
            }
        }
    }
    
    // Data structures
    public enum PerformanceState
    {
        Good,
        Warning,
        Critical
    }
    
    [System.Serializable]
    public class PerformanceMetric
    {
        public string operationName;
        public List<float> executionTimes = new List<float>();
        public List<int> objectCounts = new List<int>();
        public int totalExecutions;
        public float lastExecutionTime;
    }
    
    [System.Serializable]
    public class MetricSummary
    {
        public string operationName;
        public int totalExecutions;
        public float averageExecutionTime;
        public float minExecutionTime;
        public float maxExecutionTime;
        public float averageObjectCount;
        public float lastExecutionTime;
    }
    
    [System.Serializable]
    public class PerformanceReport
    {
        public System.DateTime timestamp;
        public PerformanceState currentState;
        public List<string> warnings;
        public List<string> suggestions;
        
        // System metrics
        public float averageFPS;
        public float minFPS;
        public float maxFPS;
        public float averageMemoryMB;
        public float peakMemoryMB;
        public float averageFrameTimeMS;
        public float frameTimeVarianceMS;
        
        // Operation metrics
        public List<MetricSummary> terrainMetrics;
        public List<MetricSummary> primitiveMetrics;
        public List<MetricSummary> uiMetrics;
    }
}