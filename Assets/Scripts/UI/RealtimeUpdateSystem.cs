using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Vastcore.UI
{
    /// <summary>
    /// Real-time update system for UI parameter changes with throttling and batching
    /// Ensures smooth performance while providing immediate visual feedback
    /// </summary>
    public class RealtimeUpdateSystem : MonoBehaviour
    {
        [Header("Update Settings")]
        [SerializeField] private bool enableRealtimeUpdates = true;
        [SerializeField] private float updateThrottleTime = 0.1f; // Minimum time between updates
        [SerializeField] private float batchUpdateInterval = 0.05f; // Batch multiple updates
        [SerializeField] private int maxUpdatesPerFrame = 5; // Limit updates per frame
        
        [Header("Performance Monitoring")]
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private float performanceCheckInterval = 1f;
        [SerializeField] private float targetFrameTime = 16.67f; // 60 FPS target
        
        // Update management
        private Dictionary<string, ParameterUpdateData> pendingUpdates = new Dictionary<string, ParameterUpdateData>();
        private Dictionary<string, float> lastUpdateTimes = new Dictionary<string, float>();
        private Queue<ParameterUpdateData> updateQueue = new Queue<ParameterUpdateData>();
        
        // Performance tracking
        private float lastPerformanceCheck = 0f;
        private List<float> recentFrameTimes = new List<float>();
        private bool isPerformanceLimited = false;
        
        // Coroutines
        private Coroutine batchUpdateCoroutine;
        private Coroutine performanceMonitorCoroutine;
        
        private void Start()
        {
            if (enableRealtimeUpdates)
            {
                StartBatchUpdateSystem();
            }
            
            if (enablePerformanceMonitoring)
            {
                StartPerformanceMonitoring();
            }
        }
        
        private void OnDestroy()
        {
            StopAllCoroutines();
        }
        
        /// <summary>
        /// Registers a parameter for real-time updates
        /// </summary>
        public void RegisterParameter(string parameterName, Action<float> updateCallback, float throttleTime = -1f)
        {
            if (throttleTime < 0f)
                throttleTime = updateThrottleTime;
            
            var updateData = new ParameterUpdateData
            {
                parameterName = parameterName,
                updateCallback = updateCallback,
                throttleTime = throttleTime,
                lastUpdateTime = 0f,
                pendingValue = null,
                priority = UpdatePriority.Normal
            };
            
            pendingUpdates[parameterName] = updateData;
            lastUpdateTimes[parameterName] = 0f;
        }
        
        /// <summary>
        /// Unregisters a parameter from real-time updates
        /// </summary>
        public void UnregisterParameter(string parameterName)
        {
            if (pendingUpdates.ContainsKey(parameterName))
            {
                pendingUpdates.Remove(parameterName);
            }
            
            if (lastUpdateTimes.ContainsKey(parameterName))
            {
                lastUpdateTimes.Remove(parameterName);
            }
        }
        
        /// <summary>
        /// Requests a parameter update with throttling
        /// </summary>
        public void RequestUpdate(string parameterName, float newValue, UpdatePriority priority = UpdatePriority.Normal)
        {
            if (!enableRealtimeUpdates || !pendingUpdates.ContainsKey(parameterName))
                return;
            
            var updateData = pendingUpdates[parameterName];
            float currentTime = Time.time;
            
            // Update the pending value
            updateData.pendingValue = newValue;
            updateData.priority = priority;
            updateData.requestTime = currentTime;
            
            // Check if we can update immediately or need to throttle
            if (CanUpdateNow(parameterName, currentTime, priority))
            {
                ExecuteUpdate(updateData);
            }
            else
            {
                // Add to queue for later processing
                if (!updateQueue.Contains(updateData))
                {
                    updateQueue.Enqueue(updateData);
                }
            }
        }
        
        /// <summary>
        /// Forces an immediate update bypassing throttling
        /// </summary>
        public void ForceUpdate(string parameterName, float newValue)
        {
            if (!pendingUpdates.ContainsKey(parameterName))
                return;
            
            var updateData = pendingUpdates[parameterName];
            updateData.pendingValue = newValue;
            updateData.priority = UpdatePriority.Immediate;
            
            ExecuteUpdate(updateData);
        }
        
        /// <summary>
        /// Sets the update priority for a parameter
        /// </summary>
        public void SetParameterPriority(string parameterName, UpdatePriority priority)
        {
            if (pendingUpdates.ContainsKey(parameterName))
            {
                pendingUpdates[parameterName].priority = priority;
            }
        }
        
        private bool CanUpdateNow(string parameterName, float currentTime, UpdatePriority priority)
        {
            // Immediate priority always updates
            if (priority == UpdatePriority.Immediate)
                return true;
            
            // Check performance limitations
            if (isPerformanceLimited && priority == UpdatePriority.Low)
                return false;
            
            // Check throttle time
            if (!lastUpdateTimes.ContainsKey(parameterName))
                return true;
            
            float timeSinceLastUpdate = currentTime - lastUpdateTimes[parameterName];
            float requiredThrottleTime = pendingUpdates[parameterName].throttleTime;
            
            return timeSinceLastUpdate >= requiredThrottleTime;
        }
        
        private void ExecuteUpdate(ParameterUpdateData updateData)
        {
            if (updateData.pendingValue.HasValue)
            {
                try
                {
                    updateData.updateCallback?.Invoke(updateData.pendingValue.Value);
                    lastUpdateTimes[updateData.parameterName] = Time.time;
                    updateData.lastUpdateTime = Time.time;
                    updateData.pendingValue = null;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error executing update for parameter '{updateData.parameterName}': {e.Message}");
                }
            }
        }
        
        private void StartBatchUpdateSystem()
        {
            if (batchUpdateCoroutine != null)
                StopCoroutine(batchUpdateCoroutine);
            
            batchUpdateCoroutine = StartCoroutine(BatchUpdateCoroutine());
        }
        
        private IEnumerator BatchUpdateCoroutine()
        {
            while (enableRealtimeUpdates)
            {
                yield return new WaitForSeconds(batchUpdateInterval);
                
                ProcessUpdateQueue();
            }
        }
        
        private void ProcessUpdateQueue()
        {
            int updatesProcessed = 0;
            float frameStartTime = Time.realtimeSinceStartup;
            
            // Process updates in priority order
            var sortedUpdates = new List<ParameterUpdateData>();
            
            while (updateQueue.Count > 0 && updatesProcessed < maxUpdatesPerFrame)
            {
                var updateData = updateQueue.Dequeue();
                
                if (updateData.pendingValue.HasValue && 
                    CanUpdateNow(updateData.parameterName, Time.time, updateData.priority))
                {
                    ExecuteUpdate(updateData);
                    updatesProcessed++;
                    
                    // Check frame time budget
                    float frameTime = (Time.realtimeSinceStartup - frameStartTime) * 1000f;
                    if (frameTime > targetFrameTime * 0.5f) // Use half the frame budget
                    {
                        break;
                    }
                }
                else if (updateData.pendingValue.HasValue)
                {
                    // Re-queue if still pending
                    sortedUpdates.Add(updateData);
                }
            }
            
            // Re-add sorted updates back to queue
            foreach (var update in sortedUpdates)
            {
                updateQueue.Enqueue(update);
            }
        }
        
        private void StartPerformanceMonitoring()
        {
            if (performanceMonitorCoroutine != null)
                StopCoroutine(performanceMonitorCoroutine);
            
            performanceMonitorCoroutine = StartCoroutine(PerformanceMonitorCoroutine());
        }
        
        private IEnumerator PerformanceMonitorCoroutine()
        {
            while (enablePerformanceMonitoring)
            {
                yield return new WaitForSeconds(performanceCheckInterval);
                
                CheckPerformance();
            }
        }
        
        private void CheckPerformance()
        {
            float currentTime = Time.time;
            
            // Only check performance at intervals to avoid overhead
            if (currentTime - lastPerformanceCheck < performanceCheckInterval)
                return;
            
            lastPerformanceCheck = currentTime;
            
            float currentFrameTime = Time.deltaTime * 1000f;
            recentFrameTimes.Add(currentFrameTime);
            
            // Keep only recent frame times (last 60 frames)
            if (recentFrameTimes.Count > 60)
            {
                recentFrameTimes.RemoveAt(0);
            }
            
            // Calculate average frame time
            float averageFrameTime = 0f;
            foreach (float frameTime in recentFrameTimes)
            {
                averageFrameTime += frameTime;
            }
            averageFrameTime /= recentFrameTimes.Count;
            
            // Adjust performance limitations
            bool wasPerformanceLimited = isPerformanceLimited;
            isPerformanceLimited = averageFrameTime > targetFrameTime * 1.2f; // 20% tolerance
            
            if (isPerformanceLimited != wasPerformanceLimited)
            {
                if (isPerformanceLimited)
                {
                    Debug.LogWarning("RealtimeUpdateSystem: Performance limited mode enabled");
                    // Reduce update frequency
                    updateThrottleTime = Mathf.Min(updateThrottleTime * 1.5f, 0.5f);
                }
                else
                {
                    Debug.Log("RealtimeUpdateSystem: Performance limitation removed");
                    // Restore normal update frequency
                    updateThrottleTime = Mathf.Max(updateThrottleTime / 1.5f, 0.05f);
                }
            }
        }
        
        /// <summary>
        /// Gets current performance statistics
        /// </summary>
        public PerformanceStats GetPerformanceStats()
        {
            float averageFrameTime = 0f;
            if (recentFrameTimes.Count > 0)
            {
                foreach (float frameTime in recentFrameTimes)
                {
                    averageFrameTime += frameTime;
                }
                averageFrameTime /= recentFrameTimes.Count;
            }
            
            return new PerformanceStats
            {
                averageFrameTime = averageFrameTime,
                isPerformanceLimited = isPerformanceLimited,
                pendingUpdatesCount = updateQueue.Count,
                registeredParametersCount = pendingUpdates.Count,
                currentThrottleTime = updateThrottleTime
            };
        }
        
        // Public properties
        public bool EnableRealtimeUpdates
        {
            get { return enableRealtimeUpdates; }
            set 
            { 
                enableRealtimeUpdates = value;
                if (value && batchUpdateCoroutine == null)
                {
                    StartBatchUpdateSystem();
                }
                else if (!value && batchUpdateCoroutine != null)
                {
                    StopCoroutine(batchUpdateCoroutine);
                    batchUpdateCoroutine = null;
                }
            }
        }
        
        public float UpdateThrottleTime
        {
            get { return updateThrottleTime; }
            set { updateThrottleTime = Mathf.Max(0.01f, value); }
        }
    }
    
    // Data structures
    [System.Serializable]
    public class ParameterUpdateData
    {
        public string parameterName;
        public Action<float> updateCallback;
        public float throttleTime;
        public float lastUpdateTime;
        public float requestTime;
        public float? pendingValue;
        public UpdatePriority priority;
    }
    
    public enum UpdatePriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Immediate = 3
    }
    
    [System.Serializable]
    public struct PerformanceStats
    {
        public float averageFrameTime;
        public bool isPerformanceLimited;
        public int pendingUpdatesCount;
        public int registeredParametersCount;
        public float currentThrottleTime;
    }
}