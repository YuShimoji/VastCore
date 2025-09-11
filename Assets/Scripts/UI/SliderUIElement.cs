using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NarrativeGen.UI
{
    /// <summary>
    /// Individual slider UI element component with modern styling
    /// Contains all UI components for a single parameter slider
    /// </summary>
    public class SliderUIElement : MonoBehaviour
    {
        [Header("UI Components")]
        public Slider slider;
        public TextMeshProUGUI labelText;
        public TextMeshProUGUI valueText;
        public TextMeshProUGUI minValueText;
        public TextMeshProUGUI maxValueText;
        
        [Header("Layout Settings")]
        public float elementHeight = 60f;
        public float elementWidth = 400f;
        public float spacing = 10f;
        
        [Header("Animation")]
        public bool enableHoverEffects = true;
        public float hoverScale = 1.05f;
        public float animationDuration = 0.2f;
        
        private RectTransform rectTransform;
        private Vector3 originalScale;
        private bool isHovered = false;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }
            
            CreateSliderUI();
            originalScale = transform.localScale;
        }
        
        private void CreateSliderUI()
        {
            // Set up the main container
            rectTransform.sizeDelta = new Vector2(elementWidth, elementHeight);
            
            // Create background panel
            CreateBackgroundPanel();
            
            // Create label text
            CreateLabelText();
            
            // Create slider
            CreateSlider();
            
            // Create value text
            CreateValueText();
            
            // Create min/max value texts
            CreateMinMaxTexts();
        }
        
        private void CreateBackgroundPanel()
        {
            Image backgroundImage = gameObject.GetComponent<Image>();
            if (backgroundImage == null)
            {
                backgroundImage = gameObject.AddComponent<Image>();
            }
            
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            backgroundImage.raycastTarget = true;
            
            // Add hover detection
            if (enableHoverEffects)
            {
                var eventTrigger = gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                if (eventTrigger == null)
                {
                    eventTrigger = gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                }
                
                // Add hover enter event
                var pointerEnter = new UnityEngine.EventSystems.EventTrigger.Entry();
                pointerEnter.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
                pointerEnter.callback.AddListener((data) => OnPointerEnter());
                eventTrigger.triggers.Add(pointerEnter);
                
                // Add hover exit event
                var pointerExit = new UnityEngine.EventSystems.EventTrigger.Entry();
                pointerExit.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
                pointerExit.callback.AddListener((data) => OnPointerExit());
                eventTrigger.triggers.Add(pointerExit);
            }
        }
        
        private void CreateLabelText()
        {
            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(transform, false);
            
            labelText = labelObject.AddComponent<TextMeshProUGUI>();
            labelText.text = "Parameter";
            labelText.fontSize = 14f;
            labelText.color = Color.white;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.6f);
            labelRect.anchorMax = new Vector2(0.6f, 1f);
            labelRect.offsetMin = new Vector2(spacing, 0);
            labelRect.offsetMax = new Vector2(-spacing, -5);
        }
        
        private void CreateSlider()
        {
            GameObject sliderObject = new GameObject("Slider");
            sliderObject.transform.SetParent(transform, false);
            
            slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.5f;
            slider.wholeNumbers = false;
            
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0.1f);
            sliderRect.anchorMax = new Vector2(0.7f, 0.5f);
            sliderRect.offsetMin = new Vector2(spacing, 0);
            sliderRect.offsetMax = new Vector2(-spacing, 0);
            
            // Create slider background
            CreateSliderBackground(sliderObject);
            
            // Create slider fill area
            CreateSliderFillArea(sliderObject);
            
            // Create slider handle
            CreateSliderHandle(sliderObject);
        }
        
        private void CreateSliderBackground(GameObject sliderObject)
        {
            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(sliderObject.transform, false);
            
            Image backgroundImage = backgroundObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);
            backgroundImage.type = Image.Type.Sliced;
            
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
        }
        
        private void CreateSliderFillArea(GameObject sliderObject)
        {
            GameObject fillAreaObject = new GameObject("Fill Area");
            fillAreaObject.transform.SetParent(sliderObject.transform, false);
            
            RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;
            
            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(fillAreaObject.transform, false);
            
            Image fillImage = fillObject.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.6f, 1f, 1f); // Primary blue color
            fillImage.type = Image.Type.Sliced;
            
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            
            slider.fillRect = fillRect;
        }
        
        private void CreateSliderHandle(GameObject sliderObject)
        {
            GameObject handleSlideAreaObject = new GameObject("Handle Slide Area");
            handleSlideAreaObject.transform.SetParent(sliderObject.transform, false);
            
            RectTransform handleSlideAreaRect = handleSlideAreaObject.GetComponent<RectTransform>();
            handleSlideAreaRect.anchorMin = Vector2.zero;
            handleSlideAreaRect.anchorMax = Vector2.one;
            handleSlideAreaRect.offsetMin = new Vector2(10, 0);
            handleSlideAreaRect.offsetMax = new Vector2(-10, 0);
            
            GameObject handleObject = new GameObject("Handle");
            handleObject.transform.SetParent(handleSlideAreaObject.transform, false);
            
            Image handleImage = handleObject.AddComponent<Image>();
            handleImage.color = new Color(0f, 0.8f, 1f, 1f); // Accent cyan color
            
            RectTransform handleRect = handleObject.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 20);
            
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
        }
        
        private void CreateValueText()
        {
            GameObject valueObject = new GameObject("Value");
            valueObject.transform.SetParent(transform, false);
            
            valueText = valueObject.AddComponent<TextMeshProUGUI>();
            valueText.text = "0.50";
            valueText.fontSize = 12f;
            valueText.color = new Color(0f, 0.8f, 1f, 1f); // Accent color
            valueText.alignment = TextAlignmentOptions.Center;
            
            RectTransform valueRect = valueObject.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0.7f, 0.6f);
            valueRect.anchorMax = new Vector2(1f, 1f);
            valueRect.offsetMin = new Vector2(spacing, 0);
            valueRect.offsetMax = new Vector2(-spacing, -5);
        }
        
        private void CreateMinMaxTexts()
        {
            // Min value text
            GameObject minObject = new GameObject("MinValue");
            minObject.transform.SetParent(transform, false);
            
            minValueText = minObject.AddComponent<TextMeshProUGUI>();
            minValueText.text = "0.0";
            minValueText.fontSize = 10f;
            minValueText.color = Color.gray;
            minValueText.alignment = TextAlignmentOptions.MidlineLeft;
            
            RectTransform minRect = minObject.GetComponent<RectTransform>();
            minRect.anchorMin = new Vector2(0, 0);
            minRect.anchorMax = new Vector2(0.3f, 0.3f);
            minRect.offsetMin = new Vector2(spacing, 0);
            minRect.offsetMax = new Vector2(-spacing, 0);
            
            // Max value text
            GameObject maxObject = new GameObject("MaxValue");
            maxObject.transform.SetParent(transform, false);
            
            maxValueText = maxObject.AddComponent<TextMeshProUGUI>();
            maxValueText.text = "1.0";
            maxValueText.fontSize = 10f;
            maxValueText.color = Color.gray;
            maxValueText.alignment = TextAlignmentOptions.MidlineRight;
            
            RectTransform maxRect = maxObject.GetComponent<RectTransform>();
            maxRect.anchorMin = new Vector2(0.4f, 0);
            maxRect.anchorMax = new Vector2(0.7f, 0.3f);
            maxRect.offsetMin = new Vector2(spacing, 0);
            maxRect.offsetMax = new Vector2(-spacing, 0);
        }
        
        private void OnPointerEnter()
        {
            if (!enableHoverEffects || isHovered) return;
            
            isHovered = true;
            StopAllCoroutines();
            StartCoroutine(AnimateScale(originalScale * hoverScale));
        }
        
        private void OnPointerExit()
        {
            if (!enableHoverEffects || !isHovered) return;
            
            isHovered = false;
            StopAllCoroutines();
            StartCoroutine(AnimateScale(originalScale));
        }
        
        private System.Collections.IEnumerator AnimateScale(Vector3 targetScale)
        {
            Vector3 startScale = transform.localScale;
            float elapsedTime = 0f;
            
            while (elapsedTime < animationDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / animationDuration;
                
                // Use smooth curve for animation
                progress = Mathf.SmoothStep(0f, 1f, progress);
                
                transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
                yield return null;
            }
            
            transform.localScale = targetScale;
        }
        
        /// <summary>
        /// Updates the slider's visual styling
        /// </summary>
        public void UpdateStyling(Color primaryColor, Color accentColor)
        {
            // Update fill color
            if (slider.fillRect != null)
            {
                Image fillImage = slider.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = primaryColor;
                }
            }
            
            // Update handle color
            if (slider.handleRect != null)
            {
                Image handleImage = slider.handleRect.GetComponent<Image>();
                if (handleImage != null)
                {
                    handleImage.color = accentColor;
                }
            }
            
            // Update value text color
            if (valueText != null)
            {
                valueText.color = accentColor;
            }
        }
        
        /// <summary>
        /// Sets the slider's parameter range and current value
        /// </summary>
        public void SetSliderRange(float minValue, float maxValue, float currentValue)
        {
            if (slider != null)
            {
                slider.minValue = minValue;
                slider.maxValue = maxValue;
                slider.value = currentValue;
            }
            
            if (minValueText != null)
                minValueText.text = minValue.ToString("F1");
            
            if (maxValueText != null)
                maxValueText.text = maxValue.ToString("F1");
            
            if (valueText != null)
                valueText.text = currentValue.ToString("F2");
        }
        
        /// <summary>
        /// Sets the parameter name displayed on the slider
        /// </summary>
        public void SetParameterName(string parameterName)
        {
            if (labelText != null)
            {
                labelText.text = parameterName;
            }
        }
    }
}