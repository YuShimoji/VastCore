using UnityEngine;
using System.Collections.Generic;

namespace NarrativeGen.UI
{
    /// <summary>
    /// Central manager for the modern UI system
    /// Coordinates all UI components and provides a unified interface for terrain/primitive systems
    /// </summary>
    public class ModernUIManager : MonoBehaviour
    {
        [Header("UI System References")]
        [SerializeField] private SliderBasedUISystem sliderSystem;
        [SerializeField] private RealtimeUpdateSystem updateSystem;
        [SerializeField] private InGameDebugUI debugUI;
        [SerializeField] private PerformanceMonitor performanceMonitor;
        [SerializeField] private ModernUIStyleSystem styleSystem;
        
        [Header("Auto-Initialize")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool createMissingComponents = true;
        
        [Header("Integration Settings")]
        [SerializeField] private bool enableTerrainIntegration = true;
        [SerializeField] private bool enablePrimitiveIntegration = true;
        [SerializeField] private bool enablePerformanceIntegration = true;
        
        // System state
        private bool isInitialized = false;
        private Dictionary<string, System.Action<float>> parameterCallbacks = new Dictionary<string, System.Action<float>>();
        
        // Events
        public System.Action OnUISystemInitialized;
        public System.Action<string, float> OnParameterChanged;
        
        private void Awake()
        {
            if (autoInitialize)
            {
                InitializeUISystem();
            }
        }
        
        private void Start()
        {
            if (isInitialized)
            {
                SetupIntegrations();
            }
        }
        
        /// <summary>
        /// Initializes the complete modern UI system
        /// </summary>
        public void InitializeUISystem()
        {
            if (isInitialized)
            {
                Debug.LogWarning("ModernUIManager: UI System already initialized");
                return;
            }
            
            Debug.Log("ModernUIManager: Initializing Modern UI System...");
            
            // Create or find required components
            SetupUIComponents();
            
            // Configure systems
            ConfigureSystems();
            
            // Setup event connections
            SetupEventConnections();
            
            isInitialized = true;
            OnUISystemInitialized?.Invoke();
            
            Debug.Log("ModernUIManager: Modern UI System initialized successfully");
        }
        
        private void SetupUIComponents()
        {
            // Slider System
            if (sliderSystem == null)
            {
                sliderSystem = FindFirstObjectByType<SliderBasedUISystem>();
                if (sliderSystem == null && createMissingComponents)
                {
                    GameObject sliderSystemObject = new GameObject("SliderBasedUISystem");
                    sliderSystemObject.transform.SetParent(transform);
                    sliderSystem = sliderSystemObject.AddComponent<SliderBasedUISystem>();
                }
            }
            
            // Update System
            if (updateSystem == null)
            {
                updateSystem = FindFirstObjectByType<RealtimeUpdateSystem>();
                if (updateSystem == null && createMissingComponents)
                {
                    GameObject updateSystemObject = new GameObject("RealtimeUpdateSystem");
                    updateSystemObject.transform.SetParent(transform);
                    updateSystem = updateSystemObject.AddComponent<RealtimeUpdateSystem>();
                }
            }
            
            // Debug UI
            if (debugUI == null)
            {
                debugUI = FindFirstObjectByType<InGameDebugUI>();
                if (debugUI == null && createMissingComponents)
                {
                    GameObject debugUIObject = new GameObject("InGameDebugUI");
                    debugUIObject.transform.SetParent(transform);
                    debugUI = debugUIObject.AddComponent<InGameDebugUI>();
                }
            }
            
            // Performance Monitor
            if (performanceMonitor == null)
            {
                performanceMonitor = FindFirstObjectByType<PerformanceMonitor>();
                if (performanceMonitor == null && createMissingComponents)
                {
                    GameObject performanceMonitorObject = new GameObject("PerformanceMonitor");
                    performanceMonitorObject.transform.SetParent(transform);
                    performanceMonitor = performanceMonitorObject.AddComponent<PerformanceMonitor>();
                }
            }
            
            // Style System
            if (styleSystem == null)
            {
                styleSystem = Resources.Load<ModernUIStyleSystem>("ModernUIStyle");
                if (styleSystem == null)
                {
                    Debug.LogWarning("ModernUIManager: ModernUIStyleSystem not found in Resources. Consider creating one.");
                }
            }
        }
        
        private void ConfigureSystems()
        {
            // Configure slider system
            if (sliderSystem != null)
            {
                sliderSystem.EnableRealtimeUpdate = true;
                sliderSystem.UpdateThrottle = 0.1f;
                
                if (styleSystem != null)
                {
                    sliderSystem.PrimaryColor = styleSystem.primaryColor;
                    sliderSystem.AccentColor = styleSystem.accentColor;
                }
            }
            
            // Configure update system
            if (updateSystem != null)
            {
                updateSystem.EnableRealtimeUpdates = true;
                updateSystem.UpdateThrottleTime = 0.1f;
            }
            
            // Configure performance monitor
            if (performanceMonitor != null)
            {
                performanceMonitor.SetMonitoring(enablePerformanceIntegration);
            }
        }
        
        private void SetupEventConnections()
        {
            // Connect performance monitor to debug UI
            if (performanceMonitor != null && debugUI != null)
            {
                performanceMonitor.OnPerformanceStateChanged += (state) =>
                {
                    Debug.Log($"Performance state changed to: {state}");
                };
                
                performanceMonitor.OnWarningsUpdated += (warnings) =>
                {
                    foreach (string warning in warnings)
                    {
                        Debug.LogWarning($"Performance Warning: {warning}");
                    }
                };
            }
        }
        
        private void SetupIntegrations()
        {
            if (enableTerrainIntegration)
            {
                SetupTerrainIntegration();
            }
            
            if (enablePrimitiveIntegration)
            {
                SetupPrimitiveIntegration();
            }
        }
        
        private void SetupTerrainIntegration()
        {
            // This would integrate with the actual terrain generation system
            // For now, we'll set up placeholder parameters
            
            RegisterParameter("Terrain_Scale", 1f, 0.1f, 5f, (value) =>
            {
                Debug.Log($"Terrain Scale updated to: {value}");
                // This would call the actual terrain generation system
            });
            
            RegisterParameter("Terrain_HeightMultiplier", 50f, 10f, 200f, (value) =>
            {
                Debug.Log($"Terrain Height Multiplier updated to: {value}");
            });
            
            RegisterParameter("Terrain_NoiseFrequency", 0.01f, 0.001f, 0.1f, (value) =>
            {
                Debug.Log($"Terrain Noise Frequency updated to: {value}");
            });
        }
        
        private void SetupPrimitiveIntegration()
        {
            // This would integrate with the actual primitive generation system
            
            RegisterParameter("Primitive_SpawnProbability", 0.05f, 0.01f, 0.2f, (value) =>
            {
                Debug.Log($"Primitive Spawn Probability updated to: {value}");
            });
            
            RegisterParameter("Primitive_MinScale", 50f, 10f, 100f, (value) =>
            {
                Debug.Log($"Primitive Min Scale updated to: {value}");
            });
            
            RegisterParameter("Primitive_MaxScale", 500f, 100f, 1000f, (value) =>
            {
                Debug.Log($"Primitive Max Scale updated to: {value}");
            });
        }
        
        /// <summary>
        /// Registers a parameter for UI control
        /// </summary>
        public void RegisterParameter(string parameterName, float defaultValue, float minValue, float maxValue, System.Action<float> callback, string category = "General")
        {
            if (!isInitialized)
            {
                Debug.LogError("ModernUIManager: Cannot register parameter before initialization");
                return;
            }
            
            // Store callback
            parameterCallbacks[parameterName] = callback;
            
            // Create wrapper callback that includes event firing
            System.Action<float> wrappedCallback = (value) =>
            {
                callback?.Invoke(value);
                OnParameterChanged?.Invoke(parameterName, value);
                
                // Record performance metric if monitoring is enabled
                if (performanceMonitor != null && performanceMonitor.IsMonitoring)
                {
                    float startTime = Time.realtimeSinceStartup;
                    // The actual callback execution time would be measured here
                    float executionTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                    performanceMonitor.RecordUIMetric($"Parameter_{parameterName}", executionTime);
                }
            };
            
            // Register with systems
            if (updateSystem != null)
            {
                updateSystem.RegisterParameter(parameterName, wrappedCallback);
            }
            
            if (debugUI != null)
            {
                debugUI.AddParameter(parameterName, defaultValue, minValue, maxValue, wrappedCallback, category);
            }
            
            Debug.Log($"ModernUIManager: Registered parameter '{parameterName}' in category '{category}'");
        }
        
        /// <summary>
        /// Unregisters a parameter from UI control
        /// </summary>
        public void UnregisterParameter(string parameterName)
        {
            if (parameterCallbacks.ContainsKey(parameterName))
            {
                parameterCallbacks.Remove(parameterName);
            }
            
            if (updateSystem != null)
            {
                updateSystem.UnregisterParameter(parameterName);
            }
            
            if (debugUI != null)
            {
                debugUI.RemoveParameter(parameterName);
            }
            
            Debug.Log($"ModernUIManager: Unregistered parameter '{parameterName}'");
        }
        
        /// <summary>
        /// Updates a parameter value programmatically
        /// </summary>
        public void UpdateParameterValue(string parameterName, float newValue)
        {
            if (debugUI != null)
            {
                debugUI.UpdateParameterValue(parameterName, newValue);
            }
            
            if (sliderSystem != null)
            {
                sliderSystem.UpdateSliderValue(parameterName, newValue);
            }
        }
        
        /// <summary>
        /// Forces an immediate update of a parameter
        /// </summary>
        public void ForceParameterUpdate(string parameterName, float newValue)
        {
            if (updateSystem != null)
            {
                updateSystem.ForceUpdate(parameterName, newValue);
            }
        }
        
        /// <summary>
        /// Gets current performance statistics
        /// </summary>
        public PerformanceReport GetPerformanceReport()
        {
            if (performanceMonitor != null)
            {
                return performanceMonitor.GenerateReport();
            }
            
            return null;
        }
        
        /// <summary>
        /// Records a terrain generation performance metric
        /// </summary>
        public void RecordTerrainPerformance(string operationName, float executionTimeMS, int objectsGenerated = 1)
        {
            if (performanceMonitor != null)
            {
                performanceMonitor.RecordTerrainMetric(operationName, executionTimeMS, objectsGenerated);
            }
        }
        
        /// <summary>
        /// Records a primitive generation performance metric
        /// </summary>
        public void RecordPrimitivePerformance(string operationName, float executionTimeMS, int objectsGenerated = 1)
        {
            if (performanceMonitor != null)
            {
                performanceMonitor.RecordPrimitiveMetric(operationName, executionTimeMS, objectsGenerated);
            }
        }
        
        /// <summary>
        /// Enables or disables the debug UI
        /// </summary>
        public void SetDebugUIVisible(bool visible)
        {
            if (debugUI != null)
            {
                if (visible)
                {
                    debugUI.gameObject.SetActive(true);
                }
                else
                {
                    debugUI.gameObject.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// Applies a new UI style configuration
        /// </summary>
        public void ApplyUIStyle(ModernUIStyleSystem newStyle)
        {
            styleSystem = newStyle;
            
            if (sliderSystem != null && styleSystem != null)
            {
                sliderSystem.PrimaryColor = styleSystem.primaryColor;
                sliderSystem.AccentColor = styleSystem.accentColor;
            }
        }
        
        // Public properties
        public bool IsInitialized => isInitialized;
        public SliderBasedUISystem SliderSystem => sliderSystem;
        public RealtimeUpdateSystem UpdateSystem => updateSystem;
        public InGameDebugUI DebugUI => debugUI;
        public PerformanceMonitor PerformanceMonitor => performanceMonitor;
        public ModernUIStyleSystem StyleSystem => styleSystem;
        
        // Static instance for easy access
        private static ModernUIManager instance;
        public static ModernUIManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<ModernUIManager>();
                    if (instance == null)
                    {
                        GameObject managerObject = new GameObject("ModernUIManager");
                        instance = managerObject.AddComponent<ModernUIManager>();
                    }
                }
                return instance;
            }
        }
        
        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}