using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Collections;

namespace NarrativeGen.UI
{
    /// <summary>
    /// Modern slider-based UI system for terrain and primitive generation parameters
    /// Provides unified styling and real-time parameter control
    /// </summary>
    public class SliderBasedUISystem : MonoBehaviour
    {
        [Header("UI Design Settings")]
        [SerializeField] private bool useModernDesign = true;
        [SerializeField] private Color primaryColor = new Color(0.2f, 0.6f, 1f, 1f); // Modern blue
        [SerializeField] private Color accentColor = new Color(0f, 0.8f, 1f, 1f); // Cyan accent
        [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f); // Dark background
        [SerializeField] private TMP_FontAsset modernFont;
        
        [Header("Slider Configuration")]
        [SerializeField] private float sliderSensitivity = 1f;
        [SerializeField] private bool enableRealtimeUpdate = true;
        [SerializeField] private float updateThrottle = 0.1f; // Minimum time between updates
        
        [Header("UI Prefabs")]
        [SerializeField] private GameObject sliderPrefab;
        [SerializeField] private GameObject panelPrefab;
        [SerializeField] private GameObject headerPrefab;
        
        // UI Management
        private Dictionary<string, SliderUIElement> activeSliders = new Dictionary<string, SliderUIElement>();
        private Dictionary<string, GameObject> activePanels = new Dictionary<string, GameObject>();
        private Transform uiContainer;
        private Canvas mainCanvas;
        
        // Update throttling
        private Dictionary<string, float> lastUpdateTimes = new Dictionary<string, float>();
        
        private void Awake()
        {
            InitializeUISystem();
        }
        
        private void InitializeUISystem()
        {
            // Create main canvas if it doesn't exist
            mainCanvas = FindObjectOfType<Canvas>();
            if (mainCanvas == null)
            {
                CreateMainCanvas();
            }
            
            // Create UI container
            CreateUIContainer();
            
            // Initialize default prefabs if not assigned
            if (sliderPrefab == null) CreateDefaultSliderPrefab();
            if (panelPrefab == null) CreateDefaultPanelPrefab();
            if (headerPrefab == null) CreateDefaultHeaderPrefab();
        }
        
        private void CreateMainCanvas()
        {
            GameObject canvasObject = new GameObject("ModernUI_Canvas");
            mainCanvas = canvasObject.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 100;
            
            // Add Canvas Scaler for responsive design
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            // Add GraphicRaycaster for UI interaction
            canvasObject.AddComponent<GraphicRaycaster>();
        }
        
        private void CreateUIContainer()
        {
            GameObject containerObject = new GameObject("SliderUI_Container");
            containerObject.transform.SetParent(mainCanvas.transform, false);
            
            RectTransform containerRect = containerObject.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            
            uiContainer = containerObject.transform;
        }
        
        /// <summary>
        /// Creates a modern slider UI element with real-time updates
        /// </summary>
        public SliderUIElement CreateSliderUI(string parameterName, float minValue, float maxValue, float currentValue, Action<float> onValueChanged)
        {
            if (activeSliders.ContainsKey(parameterName))
            {
                Debug.LogWarning($"Slider for parameter '{parameterName}' already exists. Updating existing slider.");
                UpdateSliderValue(parameterName, currentValue);
                return activeSliders[parameterName];
            }
            
            GameObject sliderObject = Instantiate(sliderPrefab, uiContainer);
            SliderUIElement sliderElement = ConfigureSliderElement(sliderObject, parameterName, minValue, maxValue, currentValue);
            
            // Bind events with throttling
            BindSliderEvents(sliderElement, onValueChanged);
            
            activeSliders[parameterName] = sliderElement;
            lastUpdateTimes[parameterName] = 0f;
            
            return sliderElement;
        }
        
        /// <summary>
        /// Creates a UI panel for grouping related sliders
        /// </summary>
        public GameObject CreateUIPanel(string panelName, Vector2 position, Vector2 size)
        {
            if (activePanels.ContainsKey(panelName))
            {
                return activePanels[panelName];
            }
            
            GameObject panelObject = Instantiate(panelPrefab, uiContainer);
            ConfigurePanel(panelObject, panelName, position, size);
            
            activePanels[panelName] = panelObject;
            return panelObject;
        }
        
        /// <summary>
        /// Updates an existing slider's value
        /// </summary>
        public void UpdateSliderValue(string parameterName, float newValue)
        {
            if (activeSliders.ContainsKey(parameterName))
            {
                activeSliders[parameterName].slider.value = newValue;
                activeSliders[parameterName].valueText.text = newValue.ToString("F2");
            }
        }
        
        /// <summary>
        /// Removes a slider from the UI
        /// </summary>
        public void RemoveSlider(string parameterName)
        {
            if (activeSliders.ContainsKey(parameterName))
            {
                Destroy(activeSliders[parameterName].gameObject);
                activeSliders.Remove(parameterName);
                lastUpdateTimes.Remove(parameterName);
            }
        }
        
        /// <summary>
        /// Clears all UI elements
        /// </summary>
        public void ClearAllUI()
        {
            foreach (var slider in activeSliders.Values)
            {
                if (slider != null && slider.gameObject != null)
                    Destroy(slider.gameObject);
            }
            
            foreach (var panel in activePanels.Values)
            {
                if (panel != null)
                    Destroy(panel);
            }
            
            activeSliders.Clear();
            activePanels.Clear();
            lastUpdateTimes.Clear();
        }
        
        private SliderUIElement ConfigureSliderElement(GameObject sliderObject, string parameterName, float minValue, float maxValue, float currentValue)
        {
            SliderUIElement element = sliderObject.GetComponent<SliderUIElement>();
            if (element == null)
            {
                element = sliderObject.AddComponent<SliderUIElement>();
            }
            
            // Configure slider component
            element.slider.minValue = minValue;
            element.slider.maxValue = maxValue;
            element.slider.value = currentValue;
            element.slider.wholeNumbers = false;
            
            // Configure text elements
            element.labelText.text = parameterName;
            element.valueText.text = currentValue.ToString("F2");
            element.minValueText.text = minValue.ToString("F1");
            element.maxValueText.text = maxValue.ToString("F1");
            
            // Apply modern styling
            ApplyModernStyling(element);
            
            return element;
        }
        
        private void ConfigurePanel(GameObject panelObject, string panelName, Vector2 position, Vector2 size)
        {
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchoredPosition = position;
            panelRect.sizeDelta = size;
            
            // Configure background
            Image backgroundImage = panelObject.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = backgroundColor;
            }
            
            // Add header if it exists
            Transform headerTransform = panelObject.transform.Find("Header");
            if (headerTransform != null)
            {
                TextMeshProUGUI headerText = headerTransform.GetComponent<TextMeshProUGUI>();
                if (headerText != null)
                {
                    headerText.text = panelName;
                    headerText.color = primaryColor;
                }
            }
        }
        
        private void BindSliderEvents(SliderUIElement element, Action<float> onValueChanged)
        {
            element.slider.onValueChanged.AddListener((float value) =>
            {
                // Update value display immediately
                element.valueText.text = value.ToString("F2");
                
                // Throttle the actual parameter updates if enabled
                if (enableRealtimeUpdate)
                {
                    string parameterName = element.labelText.text;
                    float currentTime = Time.time;
                    
                    if (!lastUpdateTimes.ContainsKey(parameterName) || 
                        currentTime - lastUpdateTimes[parameterName] >= updateThrottle)
                    {
                        onValueChanged?.Invoke(value);
                        lastUpdateTimes[parameterName] = currentTime;
                    }
                    else
                    {
                        // Schedule delayed update
                        StartCoroutine(DelayedUpdate(parameterName, value, onValueChanged));
                    }
                }
                else
                {
                    onValueChanged?.Invoke(value);
                }
            });
        }
        
        private IEnumerator DelayedUpdate(string parameterName, float value, Action<float> onValueChanged)
        {
            yield return new WaitForSeconds(updateThrottle);
            
            // Check if this is still the most recent value
            if (activeSliders.ContainsKey(parameterName) && 
                Mathf.Approximately(activeSliders[parameterName].slider.value, value))
            {
                onValueChanged?.Invoke(value);
                lastUpdateTimes[parameterName] = Time.time;
            }
        }
        
        private void ApplyModernStyling(SliderUIElement element)
        {
            if (!useModernDesign) return;
            
            // Style the slider handle
            if (element.slider.handleRect != null)
            {
                Image handleImage = element.slider.handleRect.GetComponent<Image>();
                if (handleImage != null)
                {
                    handleImage.color = accentColor;
                }
            }
            
            // Style the slider fill
            if (element.slider.fillRect != null)
            {
                Image fillImage = element.slider.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = primaryColor;
                }
            }
            
            // Style text elements
            if (element.labelText != null)
            {
                element.labelText.color = Color.white;
                if (modernFont != null) element.labelText.font = modernFont;
            }
            
            if (element.valueText != null)
            {
                element.valueText.color = accentColor;
                if (modernFont != null) element.valueText.font = modernFont;
            }
            
            if (element.minValueText != null)
            {
                element.minValueText.color = Color.gray;
                if (modernFont != null) element.minValueText.font = modernFont;
            }
            
            if (element.maxValueText != null)
            {
                element.maxValueText.color = Color.gray;
                if (modernFont != null) element.maxValueText.font = modernFont;
            }
        }
        
        private void CreateDefaultSliderPrefab()
        {
            // This will be implemented in the SliderUIElement class
            // For now, we'll create a basic structure
            sliderPrefab = CreateBasicSliderPrefab();
        }
        
        private void CreateDefaultPanelPrefab()
        {
            panelPrefab = CreateBasicPanelPrefab();
        }
        
        private void CreateDefaultHeaderPrefab()
        {
            headerPrefab = CreateBasicHeaderPrefab();
        }
        
        private GameObject CreateBasicSliderPrefab()
        {
            GameObject sliderObject = new GameObject("ModernSlider");
            sliderObject.AddComponent<SliderUIElement>();
            return sliderObject;
        }
        
        private GameObject CreateBasicPanelPrefab()
        {
            GameObject panelObject = new GameObject("ModernPanel");
            panelObject.AddComponent<RectTransform>();
            panelObject.AddComponent<Image>();
            return panelObject;
        }
        
        private GameObject CreateBasicHeaderPrefab()
        {
            GameObject headerObject = new GameObject("ModernHeader");
            headerObject.AddComponent<RectTransform>();
            headerObject.AddComponent<TextMeshProUGUI>();
            return headerObject;
        }
        
        // Public properties for external access
        public bool EnableRealtimeUpdate
        {
            get { return enableRealtimeUpdate; }
            set { enableRealtimeUpdate = value; }
        }
        
        public float UpdateThrottle
        {
            get { return updateThrottle; }
            set { updateThrottle = Mathf.Max(0.01f, value); }
        }
        
        public Color PrimaryColor
        {
            get { return primaryColor; }
            set 
            { 
                primaryColor = value;
                RefreshAllSliderStyling();
            }
        }
        
        public Color AccentColor
        {
            get { return accentColor; }
            set 
            { 
                accentColor = value;
                RefreshAllSliderStyling();
            }
        }
        
        private void RefreshAllSliderStyling()
        {
            foreach (var slider in activeSliders.Values)
            {
                ApplyModernStyling(slider);
            }
        }
    }
}