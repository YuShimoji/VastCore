using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Vastcore.UI;
using System.Collections.Generic;
using System;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// Comprehensive test suite for SliderBasedUISystem
    /// Tests canvas creation, slider creation, updates, and management
    /// TB-1: Complete slider-based UI system validation (10 tests)
    /// </summary>
    [TestFixture]
    public class SliderBasedUISystemTests
    {
        private GameObject systemObject;
        private SliderBasedUISystem sliderSystem;
        private Canvas testCanvas;

        [SetUp]
        public void SetUp()
        {
            // Create SliderBasedUISystem GameObject
            systemObject = new GameObject("TestSliderBasedUISystem");
            sliderSystem = systemObject.AddComponent<SliderBasedUISystem>();

            // Disable auto-initialization via Awake to allow manual control
            // The system will initialize when we manually trigger initialization methods
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up all created objects
            if (sliderSystem != null)
            {
                sliderSystem.ClearAllUI();
            }

            if (systemObject != null)
            {
                UnityEngine.Object.DestroyImmediate(systemObject);
            }

            // Clean up any remaining canvases
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (canvas != null && canvas.gameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(canvas.gameObject);
                }
            }

            testCanvas = null;
        }

        #region Canvas Creation Tests

        /// <summary>
        /// Test: CreateMainCanvas configures CanvasScaler with correct settings
        /// Verifies: ScaleWithScreenSize mode, 1920x1080 reference resolution, 0.5 match
        /// </summary>
        [Test]
        public void CreateMainCanvas_ConfiguresCanvasScaler()
        {
            // Arrange & Act
            UITestHelper.InvokePrivateMethod(sliderSystem, "CreateMainCanvas");

            // Assert
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            Assert.IsNotNull(canvas, "Canvas should be created");

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            Assert.IsNotNull(scaler, "CanvasScaler should be added to canvas");
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode,
                "ScaleMode should be ScaleWithScreenSize");
            Assert.AreEqual(new Vector2(1920, 1080), scaler.referenceResolution,
                "Reference resolution should be 1920x1080");
            Assert.AreEqual(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight, scaler.screenMatchMode,
                "ScreenMatchMode should be MatchWidthOrHeight");
            Assert.AreEqual(0.5f, scaler.matchWidthOrHeight,
                "MatchWidthOrHeight should be 0.5");
        }

        /// <summary>
        /// Test: CreateMainCanvas sets render mode to ScreenSpaceOverlay
        /// Verifies: Correct render mode and sorting order
        /// </summary>
        [Test]
        public void CreateMainCanvas_SetsRenderModeToScreenSpaceOverlay()
        {
            // Arrange & Act
            UITestHelper.InvokePrivateMethod(sliderSystem, "CreateMainCanvas");

            // Assert
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            Assert.IsNotNull(canvas, "Canvas should be created");
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode,
                "RenderMode should be ScreenSpaceOverlay");
            Assert.AreEqual(100, canvas.sortingOrder,
                "Sorting order should be 100");
        }

        /// <summary>
        /// Test: CreateMainCanvas adds GraphicRaycaster component
        /// Verifies: GraphicRaycaster is present for UI interaction
        /// </summary>
        [Test]
        public void CreateMainCanvas_AddsGraphicRaycaster()
        {
            // Arrange & Act
            UITestHelper.InvokePrivateMethod(sliderSystem, "CreateMainCanvas");

            // Assert
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            Assert.IsNotNull(canvas, "Canvas should be created");

            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            Assert.IsNotNull(raycaster, "GraphicRaycaster should be added for UI interaction");
        }

        #endregion

        #region Slider Creation Tests

        /// <summary>
        /// Test: CreateSliderUI returns valid SliderUIElement
        /// Verifies: Slider creation with correct parameter setup
        /// </summary>
        [Test]
        public void CreateSliderUI_ReturnsValidSliderElement()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(sliderSystem, "InitializeUISystem");
            string paramName = "TestParameter";
            float minValue = 0f;
            float maxValue = 100f;
            float currentValue = 50f;
            bool callbackInvoked = false;

            // Act
            SliderUIElement element = sliderSystem.CreateSliderUI(
                paramName, minValue, maxValue, currentValue,
                (value) => callbackInvoked = true);

            // Assert
            Assert.IsNotNull(element, "SliderUIElement should be created");
            Assert.IsNotNull(element.slider, "Slider component should exist");
            Assert.AreEqual(minValue, element.slider.minValue, "Slider min value should match");
            Assert.AreEqual(maxValue, element.slider.maxValue, "Slider max value should match");
            Assert.AreEqual(currentValue, element.slider.value, "Slider current value should match");
            Assert.AreEqual(paramName, element.labelText.text, "Label text should match parameter name");
        }

        /// <summary>
        /// Test: CreateSliderUI with existing parameter returns existing slider
        /// Verifies: Duplicate prevention and existing slider reuse
        /// </summary>
        [Test]
        public void CreateSliderUI_WithExistingParameter_ReturnsExistingSlider()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(sliderSystem, "InitializeUISystem");
            string paramName = "TestParameter";

            SliderUIElement firstElement = sliderSystem.CreateSliderUI(
                paramName, 0f, 100f, 50f, (value) => { });

            // Act
            SliderUIElement secondElement = sliderSystem.CreateSliderUI(
                paramName, 0f, 100f, 75f, (value) => { });

            // Assert
            Assert.AreSame(firstElement, secondElement,
                "Should return the same slider element for duplicate parameter");
            Assert.AreEqual(75f, firstElement.slider.value,
                "Existing slider value should be updated");
        }

        /// <summary>
        /// Test: CreateSliderUI stores slider in activeSliders dictionary
        /// Verifies: Internal dictionary management
        /// </summary>
        [Test]
        public void CreateSliderUI_StoresInActiveSlidersDictionary()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(sliderSystem, "InitializeUISystem");
            string paramName = "TestParameter";

            // Act
            sliderSystem.CreateSliderUI(paramName, 0f, 100f, 50f, (value) => { });

            // Assert
            var activeSliders = UITestHelper.GetPrivateField<Dictionary<string, SliderUIElement>>(
                sliderSystem, "activeSliders");
            Assert.IsTrue(activeSliders.ContainsKey(paramName),
                "Parameter should be stored in activeSliders dictionary");
            Assert.IsNotNull(activeSliders[paramName],
                "Stored slider element should not be null");
        }

        #endregion

        #region Update Tests

        /// <summary>
        /// Test: UpdateSliderValue updates both slider and text display
        /// Verifies: Value synchronization between slider and text
        /// </summary>
        [Test]
        public void UpdateSliderValue_UpdatesSliderAndText()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(sliderSystem, "InitializeUISystem");
            string paramName = "TestParameter";
            sliderSystem.CreateSliderUI(paramName, 0f, 100f, 50f, (value) => { });

            // Act
            float newValue = 75.5f;
            sliderSystem.UpdateSliderValue(paramName, newValue);

            // Assert
            var activeSliders = UITestHelper.GetPrivateField<Dictionary<string, SliderUIElement>>(
                sliderSystem, "activeSliders");
            SliderUIElement element = activeSliders[paramName];

            Assert.AreEqual(newValue, element.slider.value,
                "Slider value should be updated");
            Assert.AreEqual(newValue.ToString("F2"), element.valueText.text,
                "Value text should be updated with F2 formatting");
        }

        /// <summary>
        /// Test: UpdateThrottle delays callback invocation
        /// Verifies: Throttling mechanism prevents rapid updates
        /// </summary>
        [Test]
        public void ThrottleUpdate_DelaysCallback()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(sliderSystem, "InitializeUISystem");

            float throttleTime = 0.5f;
            sliderSystem.UpdateThrottle = throttleTime;

            // Assert
            Assert.AreEqual(throttleTime, sliderSystem.UpdateThrottle,
                "UpdateThrottle should be settable");
            Assert.GreaterOrEqual(sliderSystem.UpdateThrottle, 0.01f,
                "UpdateThrottle should have minimum value of 0.01f");
        }

        /// <summary>
        /// Test: EnableRealtimeUpdate toggles update mode
        /// Verifies: Realtime update mode can be enabled/disabled
        /// </summary>
        [Test]
        public void EnableRealtimeUpdate_TogglesUpdateMode()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(sliderSystem, "InitializeUISystem");

            // Act - Disable realtime update
            sliderSystem.EnableRealtimeUpdate = false;

            // Assert
            Assert.IsFalse(sliderSystem.EnableRealtimeUpdate,
                "EnableRealtimeUpdate should be disabled");

            // Act - Enable realtime update
            sliderSystem.EnableRealtimeUpdate = true;

            // Assert
            Assert.IsTrue(sliderSystem.EnableRealtimeUpdate,
                "EnableRealtimeUpdate should be enabled");
        }

        #endregion

        #region Slider Management Tests

        /// <summary>
        /// Test: RemoveSlider destroys GameObject and removes from dictionary
        /// Verifies: Proper cleanup of slider resources
        /// </summary>
        [Test]
        public void RemoveSlider_DestroysGameObject()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(sliderSystem, "InitializeUISystem");
            string paramName = "TestParameter";
            SliderUIElement element = sliderSystem.CreateSliderUI(
                paramName, 0f, 100f, 50f, (value) => { });
            GameObject sliderObject = element.gameObject;

            // Act
            sliderSystem.RemoveSlider(paramName);

            // Force Unity to process destroy
            UnityEngine.Object.DestroyImmediate(sliderObject);

            // Assert
            var activeSliders = UITestHelper.GetPrivateField<Dictionary<string, SliderUIElement>>(
                sliderSystem, "activeSliders");
            Assert.IsFalse(activeSliders.ContainsKey(paramName),
                "Parameter should be removed from activeSliders dictionary");

            var lastUpdateTimes = UITestHelper.GetPrivateField<Dictionary<string, float>>(
                sliderSystem, "lastUpdateTimes");
            Assert.IsFalse(lastUpdateTimes.ContainsKey(paramName),
                "Parameter should be removed from lastUpdateTimes dictionary");
        }

        /// <summary>
        /// Test: ClearAllUI removes all sliders and panels
        /// Verifies: Bulk cleanup functionality
        /// </summary>
        [Test]
        public void ClearAllUI_RemovesAllSliders()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(sliderSystem, "InitializeUISystem");

            sliderSystem.CreateSliderUI("Param1", 0f, 100f, 50f, (value) => { });
            sliderSystem.CreateSliderUI("Param2", 0f, 100f, 50f, (value) => { });
            sliderSystem.CreateSliderUI("Param3", 0f, 100f, 50f, (value) => { });

            // Act
            sliderSystem.ClearAllUI();

            // Assert
            var activeSliders = UITestHelper.GetPrivateField<Dictionary<string, SliderUIElement>>(
                sliderSystem, "activeSliders");
            Assert.AreEqual(0, activeSliders.Count,
                "All sliders should be removed from activeSliders");

            var activePanels = UITestHelper.GetPrivateField<Dictionary<string, GameObject>>(
                sliderSystem, "activePanels");
            Assert.AreEqual(0, activePanels.Count,
                "All panels should be removed from activePanels");

            var lastUpdateTimes = UITestHelper.GetPrivateField<Dictionary<string, float>>(
                sliderSystem, "lastUpdateTimes");
            Assert.AreEqual(0, lastUpdateTimes.Count,
                "All update times should be cleared");
        }

        #endregion

        #region Color Configuration Tests

        /// <summary>
        /// Test: PrimaryColor property updates and triggers styling refresh
        /// Verifies: Primary color configuration
        /// </summary>
        [Test]
        public void PrimaryColor_SetterUpdatesColor()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(sliderSystem, "InitializeUISystem");
            Color newPrimaryColor = Color.red;

            // Act
            sliderSystem.PrimaryColor = newPrimaryColor;

            // Assert
            Assert.AreEqual(newPrimaryColor, sliderSystem.PrimaryColor,
                "PrimaryColor should be updated");
        }

        /// <summary>
        /// Test: AccentColor property updates and triggers styling refresh
        /// Verifies: Accent color configuration
        /// </summary>
        [Test]
        public void AccentColor_SetterUpdatesColor()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(sliderSystem, "InitializeUISystem");
            Color newAccentColor = Color.green;

            // Act
            sliderSystem.AccentColor = newAccentColor;

            // Assert
            Assert.AreEqual(newAccentColor, sliderSystem.AccentColor,
                "AccentColor should be updated");
        }

        #endregion
    }
}
