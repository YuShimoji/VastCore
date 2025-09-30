using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Vastcore.UI
{
    /// <summary>
    /// Provides centralized styling configuration and application
    /// </summary>
    [CreateAssetMenu(fileName = "ModernUIStyle", menuName = "Vastcore/UI/Modern UI Style")]
    public class ModernUIStyleSystem : ScriptableObject
    {
        [Header("Color Palette")]
        public Color primaryColor = new Color(0.2f, 0.6f, 1f, 1f);        // Modern blue
        public Color accentColor = new Color(0f, 0.8f, 1f, 1f);           // Cyan accent
        public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);  // Dark background
        public Color surfaceColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);     // Surface elements
        public Color textPrimaryColor = Color.white;                        // Primary text
        public Color textSecondaryColor = Color.gray;                       // Secondary text
        public Color successColor = new Color(0.2f, 0.8f, 0.2f, 1f);       // Success/positive
        public Color warningColor = new Color(1f, 0.8f, 0.2f, 1f);         // Warning
        public Color errorColor = new Color(1f, 0.3f, 0.3f, 1f);           // Error/negative
        
        [Header("Typography")]
        public TMP_FontAsset primaryFont;
        public TMP_FontAsset secondaryFont;
        public float headerFontSize = 18f;
        public float bodyFontSize = 14f;
        public float captionFontSize = 12f;
        public float smallFontSize = 10f;
        
        [Header("Spacing & Layout")]
        public float smallSpacing = 5f;
        public float mediumSpacing = 10f;
        public float largeSpacing = 20f;
        public float extraLargeSpacing = 30f;
        
        [Header("Component Sizes")]
        public float sliderHeight = 60f;
        public float buttonHeight = 40f;
        public float inputFieldHeight = 35f;
        public float panelPadding = 15f;
        
        [Header("Animation Settings")]
        public float fastAnimationDuration = 0.15f;
        public float normalAnimationDuration = 0.25f;
        public float slowAnimationDuration = 0.4f;
        public AnimationCurve defaultEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Effects")]
        public float hoverScaleMultiplier = 1.05f;
        public float pressScaleMultiplier = 0.95f;
        public float glowIntensity = 0.3f;
        public bool enableParticleEffects = true;
        
        /// <summary>
        /// Applies modern styling to a slider component
        /// </summary>
        public void ApplySliderStyle(Slider slider)
        {
            if (slider == null) return;
            
            // Style the background
            Image backgroundImage = slider.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.color = surfaceColor;
            }
            
            // Style the fill
            if (slider.fillRect != null)
            {
                Image fillImage = slider.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    fillImage.color = primaryColor;
                }
            }
            
            // Style the handle
            if (slider.handleRect != null)
            {
                Image handleImage = slider.handleRect.GetComponent<Image>();
                if (handleImage != null)
                {
                    handleImage.color = accentColor;
                }
            }
        }
        
        /// <summary>
        /// Applies modern styling to a button component
        /// </summary>
        public void ApplyButtonStyle(Button button)
        {
            if (button == null) return;
            
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = primaryColor;
            }
            
            // Configure button colors
            ColorBlock colors = button.colors;
            colors.normalColor = primaryColor;
            colors.highlightedColor = accentColor;
            colors.pressedColor = primaryColor * 0.8f;
            colors.selectedColor = accentColor;
            colors.disabledColor = Color.gray;
            button.colors = colors;
            
            // Style button text
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                ApplyTextStyle(buttonText, TextStyle.Button);
            }
        }
        
        /// <summary>
        /// Applies modern styling to text components
        /// </summary>
        public void ApplyTextStyle(TextMeshProUGUI textComponent, TextStyle style)
        {
            if (textComponent == null) return;
            
            // Apply font
            if (primaryFont != null)
            {
                textComponent.font = primaryFont;
            }
            
            // Apply style-specific settings
            switch (style)
            {
                case TextStyle.Header:
                    textComponent.fontSize = headerFontSize;
                    textComponent.color = textPrimaryColor;
                    textComponent.fontStyle = FontStyles.Bold;
                    break;
                    
                case TextStyle.Body:
                    textComponent.fontSize = bodyFontSize;
                    textComponent.color = textPrimaryColor;
                    textComponent.fontStyle = FontStyles.Normal;
                    break;
                    
                case TextStyle.Caption:
                    textComponent.fontSize = captionFontSize;
                    textComponent.color = textSecondaryColor;
                    textComponent.fontStyle = FontStyles.Normal;
                    break;
                    
                case TextStyle.Small:
                    textComponent.fontSize = smallFontSize;
                    textComponent.color = textSecondaryColor;
                    textComponent.fontStyle = FontStyles.Normal;
                    break;
                    
                case TextStyle.Button:
                    textComponent.fontSize = bodyFontSize;
                    textComponent.color = Color.white;
                    textComponent.fontStyle = FontStyles.Bold;
                    textComponent.alignment = TextAlignmentOptions.Center;
                    break;
                    
                case TextStyle.Value:
                    textComponent.fontSize = bodyFontSize;
                    textComponent.color = accentColor;
                    textComponent.fontStyle = FontStyles.Bold;
                    break;
            }
        }
        
        /// <summary>
        /// Applies modern styling to a panel/background image
        /// </summary>
        public void ApplyPanelStyle(Image panelImage, PanelStyle style = PanelStyle.Default)
        {
            if (panelImage == null) return;
            
            switch (style)
            {
                case PanelStyle.Default:
                    panelImage.color = backgroundColor;
                    break;
                    
                case PanelStyle.Surface:
                    panelImage.color = surfaceColor;
                    break;
                    
                case PanelStyle.Transparent:
                    Color transparentColor = backgroundColor;
                    transparentColor.a = 0.5f;
                    panelImage.color = transparentColor;
                    break;
                    
                case PanelStyle.Accent:
                    Color accentBackground = accentColor;
                    accentBackground.a = 0.2f;
                    panelImage.color = accentBackground;
                    break;
            }
        }
        
        /// <summary>
        /// Creates a modern styled UI panel GameObject
        /// </summary>
        public GameObject CreateStyledPanel(string name, Transform parent, PanelStyle style = PanelStyle.Default)
        {
            GameObject panelObject = new GameObject(name);
            panelObject.transform.SetParent(parent, false);
            
            RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
            Image panelImage = panelObject.AddComponent<Image>();
            
            ApplyPanelStyle(panelImage, style);
            
            // Add padding via ContentSizeFitter if needed
            ContentSizeFitter sizeFitter = panelObject.AddComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            return panelObject;
        }
        
        /// <summary>
        /// Creates a modern styled text GameObject
        /// </summary>
        public GameObject CreateStyledText(string name, string text, Transform parent, TextStyle style = TextStyle.Body)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            
            TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            
            ApplyTextStyle(textComponent, style);
            
            return textObject;
        }
        
        /// <summary>
        /// Applies consistent spacing to a layout group
        /// </summary>
        public void ApplyLayoutSpacing(HorizontalOrVerticalLayoutGroup layoutGroup, SpacingSize spacingSize = SpacingSize.Medium)
        {
            if (layoutGroup == null) return;
            
            float spacing = GetSpacingValue(spacingSize);
            layoutGroup.spacing = spacing;
            layoutGroup.padding = new RectOffset((int)spacing, (int)spacing, (int)spacing, (int)spacing);
        }
        
        /// <summary>
        /// Gets spacing value based on size enum
        /// </summary>
        public float GetSpacingValue(SpacingSize size)
        {
            switch (size)
            {
                case SpacingSize.Small: return smallSpacing;
                case SpacingSize.Medium: return mediumSpacing;
                case SpacingSize.Large: return largeSpacing;
                case SpacingSize.ExtraLarge: return extraLargeSpacing;
                default: return mediumSpacing;
            }
        }
        
        /// <summary>
        /// Gets animation duration based on speed enum
        /// </summary>
        public float GetAnimationDuration(AnimationSpeed speed)
        {
            switch (speed)
            {
                case AnimationSpeed.Fast: return fastAnimationDuration;
                case AnimationSpeed.Normal: return normalAnimationDuration;
                case AnimationSpeed.Slow: return slowAnimationDuration;
                default: return normalAnimationDuration;
            }
        }
    }
    
    // Enums for styling options
    public enum TextStyle
    {
        Header,
        Body,
        Caption,
        Small,
        Button,
        Value
    }
    
    public enum PanelStyle
    {
        Default,
        Surface,
        Transparent,
        Accent
    }
    
    public enum SpacingSize
    {
        Small,
        Medium,
        Large,
        ExtraLarge
    }
    
    public enum AnimationSpeed
    {
        Fast,
        Normal,
        Slow
    }
}