using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;

namespace Vastcore.UI
{
    /// <summary>
    /// In-game debug UI system for real-time parameter adjustment and performance monitoring
    /// Provides immediate feedback for terrain and primitive generation parameters
    /// </summary>
    public class InGameDebugUI : MonoBehaviour
    {
        [Header("UI Configuration")]
        [SerializeField] private bool showDebugUI = true;
        [SerializeField] private Key toggleKey = Key.F1;
        [SerializeField] private bool startMinimized = false;
        
        [Header("UI Layout")]
        [SerializeField] private Vector2 panelSize = new Vector2(400, 600);
        [SerializeField] private Vector2 panelPosition = new Vector2(50, 50);
        [SerializeField] private float panelAlpha = 0.9f;
        
        [Header("Performance Monitoring")]
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private float performanceUpdateInterval = 0.5f;
        [SerializeField] private int maxPerformanceHistory = 60;
        
        [Header("Parameter Categories")]
        [SerializeField] private bool showTerrainParameters = true;
        [SerializeField] private bool showPrimitiveParameters = true;
        [SerializeField] private bool showPerformanceParameters = true;
        [SerializeField] private bool showSystemParameters = true;
        
        // UI Components
        private GameObject debugPanel;
        private ScrollRect scrollRect;
        private Transform contentContainer;
        private SliderBasedUISystem sliderSystem;
        private RealtimeUpdateSystem updateSystem;
        private ModernUIStyleSystem styleSystem;
        
        // Performance monitoring
        private TextMeshProUGUI fpsText;
        private TextMeshProUGUI memoryText;
        private TextMeshProUGUI performanceText;
        private List<float> fpsHistory = new List<float>();
        private List<float> memoryHistory = new List<float>();
        
        // Parameter management
        private Dictionary<string, DebugParameter> debugParameters = new Dictionary<string, DebugParameter>();
        private Dictionary<string, GameObject> parameterPanels = new Dictionary<string, GameObject>();
        
        // State management
        private bool isUIVisible = true;
        private bool isMinimized = false;
        private Vector2 minimizedSize = new Vector2(200, 100);
        
        private void Awake()
        {
            InitializeDebugUI();
        }
        
        private void Start()
        {
            SetupDefaultParameters();
            
            if (startMinimized)
            {
                MinimizeUI();
            }
            
            if (enablePerformanceMonitoring)
            {
                StartCoroutine(PerformanceMonitoringCoroutine());
            }
        }
        
        private void Update()
        {
            HandleInput();
            
            if (enablePerformanceMonitoring && isUIVisible)
            {
                UpdatePerformanceDisplay();
            }
        }
        
        private void InitializeDebugUI()
        {
            // Get or create required systems
            sliderSystem = FindObjectOfType<SliderBasedUISystem>();
            if (sliderSystem == null)
            {
                GameObject sliderSystemObject = new GameObject("SliderBasedUISystem");
                sliderSystem = sliderSystemObject.AddComponent<SliderBasedUISystem>();
            }
            
            updateSystem = FindObjectOfType<RealtimeUpdateSystem>();
            if (updateSystem == null)
            {
                GameObject updateSystemObject = new GameObject("RealtimeUpdateSystem");
                updateSystem = updateSystemObject.AddComponent<RealtimeUpdateSystem>();
            }
            
            // Load style system
            styleSystem = Resources.Load<ModernUIStyleSystem>("ModernUIStyle");
            if (styleSystem == null)
            {
                Debug.LogWarning("ModernUIStyleSystem not found in Resources. Using default styling.");
            }
            
            CreateDebugPanel();
        }
        
        private void CreateDebugPanel()
        {
            // Create main debug panel
            debugPanel = new GameObject("InGameDebugUI");
            debugPanel.transform.SetParent(sliderSystem.transform.parent, false);
            
            RectTransform panelRect = debugPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = panelPosition;
            panelRect.sizeDelta = panelSize;
            
            // Add background
            Image backgroundImage = debugPanel.AddComponent<Image>();
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, panelAlpha);
            
            // Create header
            CreateHeader();
            
            // Create scroll view
            CreateScrollView();
            
            // Create performance display
            if (enablePerformanceMonitoring)
            {
                CreatePerformanceDisplay();
            }
            
            // Create parameter sections
            CreateParameterSections();
        }
        
        private void CreateHeader()
        {
            GameObject headerObject = new GameObject("Header");
            headerObject.transform.SetParent(debugPanel.transform, false);
            
            RectTransform headerRect = headerObject.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 40);
            
            Image headerBackground = headerObject.AddComponent<Image>();
            headerBackground.color = new Color(0.2f, 0.6f, 1f, 0.8f);
            
            // Header text
            GameObject headerTextObject = new GameObject("HeaderText");
            headerTextObject.transform.SetParent(headerObject.transform, false);
            
            TextMeshProUGUI headerText = headerTextObject.AddComponent<TextMeshProUGUI>();
            headerText.text = "Debug Parameters";
            headerText.fontSize = 16f;
            headerText.color = Color.white;
            headerText.alignment = TextAlignmentOptions.Center;
            
            RectTransform headerTextRect = headerTextObject.GetComponent<RectTransform>();
            headerTextRect.anchorMin = Vector2.zero;
            headerTextRect.anchorMax = Vector2.one;
            headerTextRect.offsetMin = Vector2.zero;
            headerTextRect.offsetMax = Vector2.zero;
            
            // Minimize button
            CreateMinimizeButton(headerObject);
            
            // Close button
            CreateCloseButton(headerObject);
        }
        
        private void CreateMinimizeButton(GameObject header)
        {
            GameObject minimizeButton = new GameObject("MinimizeButton");
            minimizeButton.transform.SetParent(header.transform, false);
            
            RectTransform buttonRect = minimizeButton.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1, 0);
            buttonRect.anchorMax = new Vector2(1, 1);
            buttonRect.pivot = new Vector2(1, 0.5f);
            buttonRect.anchoredPosition = new Vector2(-40, 0);
            buttonRect.sizeDelta = new Vector2(30, 30);
            
            Button button = minimizeButton.AddComponent<Button>();
            Image buttonImage = minimizeButton.AddComponent<Image>();
            buttonImage.color = new Color(1f, 1f, 1f, 0.3f);
            
            // Button text
            GameObject buttonTextObject = new GameObject("Text");
            buttonTextObject.transform.SetParent(minimizeButton.transform, false);
            
            TextMeshProUGUI buttonText = buttonTextObject.AddComponent<TextMeshProUGUI>();
            buttonText.text = "-";
            buttonText.fontSize = 20f;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            
            RectTransform textRect = buttonTextObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            button.onClick.AddListener(ToggleMinimize);
        }
        
        private void CreateCloseButton(GameObject header)
        {
            GameObject closeButton = new GameObject("CloseButton");
            closeButton.transform.SetParent(header.transform, false);
            
            RectTransform buttonRect = closeButton.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1, 0);
            buttonRect.anchorMax = new Vector2(1, 1);
            buttonRect.pivot = new Vector2(1, 0.5f);
            buttonRect.anchoredPosition = new Vector2(-5, 0);
            buttonRect.sizeDelta = new Vector2(30, 30);
            
            Button button = closeButton.AddComponent<Button>();
            Image buttonImage = closeButton.AddComponent<Image>();
            buttonImage.color = new Color(1f, 0.3f, 0.3f, 0.7f);
            
            // Button text
            GameObject buttonTextObject = new GameObject("Text");
            buttonTextObject.transform.SetParent(closeButton.transform, false);
            
            TextMeshProUGUI buttonText = buttonTextObject.AddComponent<TextMeshProUGUI>();
            buttonText.text = "×";
            buttonText.fontSize = 16f;
            buttonText.color = Color.white;
            buttonText.alignment = TextAlignmentOptions.Center;
            
            RectTransform textRect = buttonTextObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            button.onClick.AddListener(HideUI);
        }
        
        private void CreateScrollView()
        {
            GameObject scrollViewObject = new GameObject("ScrollView");
            scrollViewObject.transform.SetParent(debugPanel.transform, false);
            
            RectTransform scrollRect = scrollViewObject.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.offsetMin = new Vector2(5, 5);
            scrollRect.offsetMax = new Vector2(-5, -45); // Account for header
            
            this.scrollRect = scrollViewObject.AddComponent<ScrollRect>();
            this.scrollRect.horizontal = false;
            this.scrollRect.vertical = true;
            
            // Create viewport
            GameObject viewportObject = new GameObject("Viewport");
            viewportObject.transform.SetParent(scrollViewObject.transform, false);
            
            RectTransform viewportRect = viewportObject.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            
            Image viewportImage = viewportObject.AddComponent<Image>();
            viewportImage.color = Color.clear;
            
            Mask viewportMask = viewportObject.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            
            this.scrollRect.viewport = viewportRect;
            
            // Create content container
            GameObject contentObject = new GameObject("Content");
            contentObject.transform.SetParent(viewportObject.transform, false);
            
            RectTransform contentRect = contentObject.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);
            
            VerticalLayoutGroup layoutGroup = contentObject.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 10f;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childForceExpandWidth = true;
            
            ContentSizeFitter sizeFitter = contentObject.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            this.scrollRect.content = contentRect;
            contentContainer = contentObject.transform;
        }
        
        private void CreatePerformanceDisplay()
        {
            GameObject performancePanel = CreateParameterPanel("Performance Monitor");
            
            // FPS Display
            GameObject fpsObject = new GameObject("FPS");
            fpsObject.transform.SetParent(performancePanel.transform, false);
            
            fpsText = fpsObject.AddComponent<TextMeshProUGUI>();
            fpsText.text = "FPS: 60";
            fpsText.fontSize = 12f;
            fpsText.color = Color.green;
            
            // Memory Display
            GameObject memoryObject = new GameObject("Memory");
            memoryObject.transform.SetParent(performancePanel.transform, false);
            
            memoryText = memoryObject.AddComponent<TextMeshProUGUI>();
            memoryText.text = "Memory: 0 MB";
            memoryText.fontSize = 12f;
            memoryText.color = Color.yellow;
            
            // Performance Stats
            GameObject performanceObject = new GameObject("Performance");
            performanceObject.transform.SetParent(performancePanel.transform, false);
            
            performanceText = performanceObject.AddComponent<TextMeshProUGUI>();
            performanceText.text = "Performance: Normal";
            performanceText.fontSize = 12f;
            performanceText.color = Color.white;
        }
        
        private void CreateParameterSections()
        {
            if (showTerrainParameters)
            {
                CreateTerrainParameterSection();
            }
            
            if (showPrimitiveParameters)
            {
                CreatePrimitiveParameterSection();
            }
            
            if (showSystemParameters)
            {
                CreateSystemParameterSection();
            }
        }
        
        private void CreateTerrainParameterSection()
        {
            GameObject terrainPanel = CreateParameterPanel("Terrain Generation");
            
            // Add terrain-specific parameters
            AddDebugParameter("Terrain Scale", 1f, 0.1f, 5f, terrainPanel, (value) => {
                // This would connect to the actual terrain generation system
                Debug.Log($"Terrain Scale changed to: {value}");
            });
            
            AddDebugParameter("Height Multiplier", 50f, 10f, 200f, terrainPanel, (value) => {
                Debug.Log($"Height Multiplier changed to: {value}");
            });
            
            AddDebugParameter("Noise Frequency", 0.01f, 0.001f, 0.1f, terrainPanel, (value) => {
                Debug.Log($"Noise Frequency changed to: {value}");
            });
            
            AddDebugParameter("Circular Radius", 1000f, 500f, 2000f, terrainPanel, (value) => {
                Debug.Log($"Circular Radius changed to: {value}");
            });
        }
        
        private void CreatePrimitiveParameterSection()
        {
            GameObject primitivePanel = CreateParameterPanel("Primitive Generation");
            
            AddDebugParameter("Spawn Probability", 0.05f, 0.01f, 0.2f, primitivePanel, (value) => {
                Debug.Log($"Spawn Probability changed to: {value}");
            });
            
            AddDebugParameter("Min Scale", 50f, 10f, 100f, primitivePanel, (value) => {
                Debug.Log($"Min Scale changed to: {value}");
            });
            
            AddDebugParameter("Max Scale", 500f, 100f, 1000f, primitivePanel, (value) => {
                Debug.Log($"Max Scale changed to: {value}");
            });
            
            AddDebugParameter("Min Distance", 200f, 50f, 500f, primitivePanel, (value) => {
                Debug.Log($"Min Distance changed to: {value}");
            });
        }
        
        private void CreateSystemParameterSection()
        {
            GameObject systemPanel = CreateParameterPanel("System Settings");
            
            AddDebugParameter("Update Throttle", 0.1f, 0.01f, 1f, systemPanel, (value) => {
                if (updateSystem != null)
                {
                    updateSystem.UpdateThrottleTime = value;
                }
            });
            
            AddDebugParameter("UI Alpha", panelAlpha, 0.3f, 1f, systemPanel, (value) => {
                panelAlpha = value;
                UpdateUIAlpha();
            });
        }
        
        private GameObject CreateParameterPanel(string title)
        {
            GameObject panel = new GameObject($"Panel_{title}");
            panel.transform.SetParent(contentContainer, false);
            
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(0, 0); // Will be sized by layout group
            
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            
            VerticalLayoutGroup layoutGroup = panel.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 5f;
            layoutGroup.padding = new RectOffset(10, 10, 10, 10);
            layoutGroup.childControlHeight = false;
            layoutGroup.childControlWidth = true;
            
            ContentSizeFitter sizeFitter = panel.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Add title
            GameObject titleObject = new GameObject("Title");
            titleObject.transform.SetParent(panel.transform, false);
            
            TextMeshProUGUI titleText = titleObject.AddComponent<TextMeshProUGUI>();
            titleText.text = title;
            titleText.fontSize = 14f;
            titleText.color = new Color(0.2f, 0.6f, 1f, 1f);
            titleText.fontStyle = FontStyles.Bold;
            
            parameterPanels[title] = panel;
            return panel;
        }
        
        private void AddDebugParameter(string parameterName, float defaultValue, float minValue, float maxValue, GameObject parentPanel, System.Action<float> onValueChanged)
        {
            var parameter = new DebugParameter
            {
                name = parameterName,
                currentValue = defaultValue,
                minValue = minValue,
                maxValue = maxValue,
                onValueChanged = onValueChanged
            };
            
            debugParameters[parameterName] = parameter;
            
            // Create slider UI element
            var sliderElement = sliderSystem.CreateSliderUI(parameterName, minValue, maxValue, defaultValue, onValueChanged);
            sliderElement.transform.SetParent(parentPanel.transform, false);
            
            // Register with update system
            updateSystem.RegisterParameter(parameterName, onValueChanged);
        }
        
        private void HandleInput()
        {
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                ToggleUI();
            }
        }
        
        private void UpdatePerformanceDisplay()
        {
            if (fpsText != null)
            {
                float fps = 1f / Time.deltaTime;
                fpsHistory.Add(fps);
                
                if (fpsHistory.Count > maxPerformanceHistory)
                {
                    fpsHistory.RemoveAt(0);
                }
                
                float avgFps = 0f;
                foreach (float f in fpsHistory)
                {
                    avgFps += f;
                }
                avgFps /= fpsHistory.Count;
                
                fpsText.text = $"FPS: {avgFps:F1}";
                fpsText.color = avgFps > 45f ? Color.green : avgFps > 25f ? Color.yellow : Color.red;
            }
            
            if (memoryText != null)
            {
                float memoryMB = System.GC.GetTotalMemory(false) / (1024f * 1024f);
                memoryText.text = $"Memory: {memoryMB:F1} MB";
            }
            
            if (performanceText != null && updateSystem != null)
            {
                var stats = updateSystem.GetPerformanceStats();
                performanceText.text = $"Updates: {stats.pendingUpdatesCount} | Throttle: {stats.currentThrottleTime:F3}s";
                performanceText.color = stats.isPerformanceLimited ? Color.red : Color.white;
            }
        }
        
        private IEnumerator PerformanceMonitoringCoroutine()
        {
            while (enablePerformanceMonitoring)
            {
                yield return new WaitForSeconds(performanceUpdateInterval);
                
                if (isUIVisible)
                {
                    UpdatePerformanceDisplay();
                }
            }
        }
        
        private void ToggleUI()
        {
            if (isUIVisible)
            {
                HideUI();
            }
            else
            {
                ShowUI();
            }
        }
        
        private void ShowUI()
        {
            if (debugPanel != null)
            {
                debugPanel.SetActive(true);
                isUIVisible = true;
            }
        }
        
        private void HideUI()
        {
            if (debugPanel != null)
            {
                debugPanel.SetActive(false);
                isUIVisible = false;
            }
        }
        
        private void ToggleMinimize()
        {
            if (isMinimized)
            {
                MaximizeUI();
            }
            else
            {
                MinimizeUI();
            }
        }
        
        private void MinimizeUI()
        {
            if (debugPanel != null)
            {
                RectTransform panelRect = debugPanel.GetComponent<RectTransform>();
                panelRect.sizeDelta = minimizedSize;
                
                // Hide scroll view
                if (scrollRect != null)
                {
                    scrollRect.gameObject.SetActive(false);
                }
                
                isMinimized = true;
            }
        }
        
        private void MaximizeUI()
        {
            if (debugPanel != null)
            {
                RectTransform panelRect = debugPanel.GetComponent<RectTransform>();
                panelRect.sizeDelta = panelSize;
                
                // Show scroll view
                if (scrollRect != null)
                {
                    scrollRect.gameObject.SetActive(true);
                }
                
                isMinimized = false;
            }
        }
        
        private void UpdateUIAlpha()
        {
            if (debugPanel != null)
            {
                Image backgroundImage = debugPanel.GetComponent<Image>();
                if (backgroundImage != null)
                {
                    Color color = backgroundImage.color;
                    color.a = panelAlpha;
                    backgroundImage.color = color;
                }
            }
        }
        
        private void SetupDefaultParameters()
        {
            // This method can be extended to load default parameters from a configuration file
            // or connect to existing terrain/primitive generation systems
        }
        
        // Public API for external systems
        public void AddParameter(string name, float defaultValue, float minValue, float maxValue, System.Action<float> callback, string category = "Custom")
        {
            GameObject panel = null;
            if (parameterPanels.ContainsKey(category))
            {
                panel = parameterPanels[category];
            }
            else
            {
                panel = CreateParameterPanel(category);
            }
            
            AddDebugParameter(name, defaultValue, minValue, maxValue, panel, callback);
        }
        
        public void RemoveParameter(string name)
        {
            if (debugParameters.ContainsKey(name))
            {
                debugParameters.Remove(name);
                updateSystem.UnregisterParameter(name);
                sliderSystem.RemoveSlider(name);
            }
        }
        
        public void UpdateParameterValue(string name, float newValue)
        {
            if (debugParameters.ContainsKey(name))
            {
                debugParameters[name].currentValue = newValue;
                sliderSystem.UpdateSliderValue(name, newValue);
            }
        }
    }
    
    [System.Serializable]
    public class DebugParameter
    {
        public string name;
        public float currentValue;
        public float minValue;
        public float maxValue;
        public System.Action<float> onValueChanged;
    }
}