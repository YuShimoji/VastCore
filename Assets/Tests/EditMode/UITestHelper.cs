using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// Test helper utilities for UI testing
    /// Provides common setup, mocks, and utilities for UI component tests
    /// </summary>
    public static class UITestHelper
    {
        /// <summary>
        /// Creates a minimal Canvas setup for UI testing
        /// </summary>
        public static Canvas CreateTestCanvas(string name = "TestCanvas")
        {
            GameObject canvasObject = new GameObject(name);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObject.AddComponent<GraphicRaycaster>();

            return canvas;
        }

        /// <summary>
        /// Creates a test GameObject with UI components
        /// </summary>
        public static GameObject CreateTestUIObject(string name = "TestUIObject", Transform parent = null)
        {
            GameObject obj = new GameObject(name);
            RectTransform rectTransform = obj.AddComponent<RectTransform>();

            if (parent != null)
            {
                rectTransform.SetParent(parent, false);
            }

            return obj;
        }

        /// <summary>
        /// Creates a test Slider UI element
        /// </summary>
        public static Slider CreateTestSlider(Transform parent = null)
        {
            GameObject sliderObject = CreateTestUIObject("TestSlider", parent);
            Slider slider = sliderObject.AddComponent<Slider>();

            // Create slider components
            GameObject background = CreateTestUIObject("Background", sliderObject.transform);
            background.AddComponent<Image>();

            GameObject fillArea = CreateTestUIObject("Fill Area", sliderObject.transform);
            GameObject fill = CreateTestUIObject("Fill", fillArea.transform);
            Image fillImage = fill.AddComponent<Image>();

            GameObject handleSlideArea = CreateTestUIObject("Handle Slide Area", sliderObject.transform);
            GameObject handle = CreateTestUIObject("Handle", handleSlideArea.transform);
            handle.AddComponent<Image>();

            // Configure slider
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = fillImage;

            return slider;
        }

        /// <summary>
        /// Creates a test TextMeshPro text component
        /// </summary>
        public static TextMeshProUGUI CreateTestText(Transform parent = null, string initialText = "Test")
        {
            GameObject textObject = CreateTestUIObject("TestText", parent);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = initialText;
            return text;
        }

        /// <summary>
        /// Gets a private field value using reflection
        /// </summary>
        public static T GetPrivateField<T>(object obj, string fieldName)
        {
            FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                throw new System.Exception($"Field '{fieldName}' not found on type '{obj.GetType().Name}'");
            }
            return (T)field.GetValue(obj);
        }

        /// <summary>
        /// Sets a private field value using reflection
        /// </summary>
        public static void SetPrivateField(object obj, string fieldName, object value)
        {
            FieldInfo field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                throw new System.Exception($"Field '{fieldName}' not found on type '{obj.GetType().Name}'");
            }
            field.SetValue(obj, value);
        }

        /// <summary>
        /// Invokes a private method using reflection
        /// </summary>
        public static object InvokePrivateMethod(object obj, string methodName, params object[] parameters)
        {
            MethodInfo method = obj.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
            {
                throw new System.Exception($"Method '{methodName}' not found on type '{obj.GetType().Name}'");
            }
            return method.Invoke(obj, parameters);
        }

        /// <summary>
        /// Destroys all test GameObjects immediately
        /// </summary>
        public static void DestroyAllTestObjects(params GameObject[] objects)
        {
            foreach (var obj in objects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
        }

        /// <summary>
        /// Creates a mock PerformanceMonitor for testing
        /// </summary>
        public static GameObject CreateMockPerformanceMonitor()
        {
            GameObject perfMonitorObject = new GameObject("MockPerformanceMonitor");
            // Note: PerformanceMonitor would be added here if needed
            // For now, we return a GameObject that can be used as a placeholder
            return perfMonitorObject;
        }
    }
}
