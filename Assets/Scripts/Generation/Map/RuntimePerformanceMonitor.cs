using System;
using UnityEngine;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// Stub class for PerformanceMonitor to resolve compilation errors in RuntimeGenerationManager.
    /// This replaces the missing implementation expected by the manager.
    /// </summary>
    public class PerformanceMonitor
    {
        public float AverageFrameTimeMs { get; private set; } = 16.67f;
        public float CurrentFPS { get; private set; } = 60f;
        public bool IsOverloaded { get; private set; } = false;

        public event Action OnPerformanceImproved;
        public event Action OnPerformanceDegraded;

        public void Initialize()
        {
            // Stub implementation
        }

        public void SetTargetFrameRate(float fps)
        {
            // Stub implementation
        }

        public void UpdatePerformance()
        {
            // Stub implementation: Simulate logic or do nothing
            // In a real implementation, this would track frame times
        }
        
        // Added methods based on RuntimeGenerationManager usage (AdjustPerformanceSettings)
        public int GetRecommendedTaskCount(int current, int min, int max)
        {
            return current;
        }

        public float GetRecommendedMaxExecutionTime(float current, float min, float max)
        {
            return current;
        }
        
        public PerformanceStats GetCurrentStats()
        {
            return new PerformanceStats();
        }

        public string GetDebugString()
        {
            return "PerformanceMonitor (Stub)";
        }

        public void SetMonitoringEnabled(bool enabled)
        {
            // Stub
        }

        [Serializable]
        public class PerformanceStats
        {
            // Generic stats container
        }
    }
}
