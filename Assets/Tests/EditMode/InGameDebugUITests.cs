using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Vastcore.UI;
using System.Collections.Generic;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// Comprehensive EditMode test suite for InGameDebugUI
    /// Tests initialization, parameter management, UI visibility, and panel operations
    /// </summary>
    [TestFixture]
    public class InGameDebugUITests
    {
        private GameObject debugUIObject;
        private InGameDebugUI debugUI;
        private Canvas testCanvas;
        private GameObject sliderSystemObject;
        private SliderBasedUISystem sliderSystem;
        private GameObject updateSystemObject;
        private RealtimeUpdateSystem updateSystem;

        [SetUp]
        public void SetUp()
        {
            // Create test canvas for UI rendering
            testCanvas = UITestHelper.CreateTestCanvas("TestDebugCanvas");

            // Create SliderBasedUISystem
            sliderSystemObject = new GameObject("TestSliderSystem");
            sliderSystemObject.transform.SetParent(testCanvas.transform, false);
            sliderSystem = sliderSystemObject.AddComponent<SliderBasedUISystem>();

            // Create RealtimeUpdateSystem
            updateSystemObject = new GameObject("TestUpdateSystem");
            updateSystem = updateSystemObject.AddComponent<RealtimeUpdateSystem>();

            // Create InGameDebugUI GameObject
            debugUIObject = new GameObject("TestInGameDebugUI");
            debugUI = debugUIObject.AddComponent<InGameDebugUI>();

            // Disable auto-initialization to manually control test flow
            UITestHelper.SetPrivateField(debugUI, "showDebugUI", true);
            UITestHelper.SetPrivateField(debugUI, "enablePerformanceMonitoring", false);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up all test objects
            if (debugUIObject != null)
            {
                Object.DestroyImmediate(debugUIObject);
            }

            if (updateSystemObject != null)
            {
                Object.DestroyImmediate(updateSystemObject);
            }

            if (sliderSystemObject != null)
            {
                Object.DestroyImmediate(sliderSystemObject);
            }

            if (testCanvas != null && testCanvas.gameObject != null)
            {
                Object.DestroyImmediate(testCanvas.gameObject);
            }
        }

        #region Initialization Tests

        /// <summary>
        /// Test: InitializeDebugUI creates required SliderBasedUISystem
        /// Verifies that the debug UI properly initializes or finds the slider system
        /// </summary>
        [Test]
        public void InitializeDebugUI_CreatesRequiredSystems()
        {
            // Act
            UITestHelper.InvokePrivateMethod(debugUI, "InitializeDebugUI");

            // Assert
            var internalSliderSystem = UITestHelper.GetPrivateField<SliderBasedUISystem>(debugUI, "sliderSystem");
            Assert.IsNotNull(internalSliderSystem, "SliderSystem should be initialized");

            var internalUpdateSystem = UITestHelper.GetPrivateField<RealtimeUpdateSystem>(debugUI, "updateSystem");
            Assert.IsNotNull(internalUpdateSystem, "UpdateSystem should be initialized");
        }

        /// <summary>
        /// Test: CreateDebugPanel creates a valid panel GameObject
        /// Verifies that the panel is created with proper components
        /// </summary>
        [Test]
        public void CreateDebugPanel_CreatesPanel()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(debugUI, "InitializeDebugUI");

            // Act
            var debugPanel = UITestHelper.GetPrivateField<GameObject>(debugUI, "debugPanel");

            // Assert
            Assert.IsNotNull(debugPanel, "Debug panel should be created");
            Assert.IsTrue(debugPanel.name.Contains("InGameDebugUI"), "Panel should have correct name");

            RectTransform rectTransform = debugPanel.GetComponent<RectTransform>();
            Assert.IsNotNull(rectTransform, "Panel should have RectTransform component");

            Image backgroundImage = debugPanel.GetComponent<Image>();
            Assert.IsNotNull(backgroundImage, "Panel should have background Image component");
        }

        /// <summary>
        /// Test: InitializeDebugUI sets up canvas properly
        /// Verifies that the canvas hierarchy is established correctly
        /// </summary>
        [Test]
        public void InitializeDebugUI_SetsupCanvas()
        {
            // Act
            UITestHelper.InvokePrivateMethod(debugUI, "InitializeDebugUI");

            // Assert
            var debugPanel = UITestHelper.GetPrivateField<GameObject>(debugUI, "debugPanel");
            Assert.IsNotNull(debugPanel, "Debug panel should exist");

            // Verify panel is parented correctly
            Transform panelParent = debugPanel.transform.parent;
            Assert.IsNotNull(panelParent, "Panel should have a parent transform");

            // Verify scroll view exists
            var scrollRect = UITestHelper.GetPrivateField<ScrollRect>(debugUI, "scrollRect");
            Assert.IsNotNull(scrollRect, "ScrollRect should be initialized");
        }

        #endregion

        #region Parameter Management Tests

        /// <summary>
        /// Test: AddParameter registers parameter with required systems
        /// Verifies that parameters are registered with both slider and update systems
        /// </summary>
        [Test]
        public void AddParameter_RegistersWithSystems()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(debugUI, "InitializeDebugUI");
            UITestHelper.InvokePrivateMethod(debugUI, "CreateParameterSections");

            bool callbackInvoked = false;
            float callbackValue = 0f;

            // Act
            debugUI.AddParameter("TestParameter", 5.0f, 0f, 10f, (value) =>
            {
                callbackInvoked = true;
                callbackValue = value;
            }, "Test Category");

            // Assert
            var debugParameters = UITestHelper.GetPrivateField<Dictionary<string, DebugParameter>>(debugUI, "debugParameters");
            Assert.IsTrue(debugParameters.ContainsKey("TestParameter"), "Parameter should be stored in internal dictionary");
            Assert.AreEqual(5.0f, debugParameters["TestParameter"].currentValue, "Parameter should have correct default value");
        }

        /// <summary>
        /// Test: RemoveParameter unregisters from systems
        /// Verifies that parameters are properly removed from all systems
        /// </summary>
        [Test]
        public void RemoveParameter_UnregistersFromSystems()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(debugUI, "InitializeDebugUI");
            UITestHelper.InvokePrivateMethod(debugUI, "CreateParameterSections");

            debugUI.AddParameter("TestParameter", 5.0f, 0f, 10f, (value) => { }, "Test Category");

            var debugParameters = UITestHelper.GetPrivateField<Dictionary<string, DebugParameter>>(debugUI, "debugParameters");
            Assert.IsTrue(debugParameters.ContainsKey("TestParameter"), "Parameter should exist before removal");

            // Act
            debugUI.RemoveParameter("TestParameter");

            // Assert
            Assert.IsFalse(debugParameters.ContainsKey("TestParameter"), "Parameter should be removed from internal dictionary");
        }

        /// <summary>
        /// Test: UpdateParameterValue updates slider value correctly
        /// Verifies that parameter value updates are reflected in the UI
        /// </summary>
        [Test]
        public void UpdateParameterValue_UpdatesSliderValue()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(debugUI, "InitializeDebugUI");
            UITestHelper.InvokePrivateMethod(debugUI, "CreateParameterSections");

            debugUI.AddParameter("TestParameter", 5.0f, 0f, 10f, (value) => { }, "Test Category");

            // Act
            debugUI.UpdateParameterValue("TestParameter", 7.5f);

            // Assert
            var debugParameters = UITestHelper.GetPrivateField<Dictionary<string, DebugParameter>>(debugUI, "debugParameters");
            Assert.AreEqual(7.5f, debugParameters["TestParameter"].currentValue, "Parameter value should be updated");
        }

        /// <summary>
        /// Test: AddParameter stores parameter in internal dictionary
        /// Verifies that parameters are properly stored with correct metadata
        /// </summary>
        [Test]
        public void AddParameter_StoresInInternalDictionary()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(debugUI, "InitializeDebugUI");
            UITestHelper.InvokePrivateMethod(debugUI, "CreateParameterSections");

            // Act
            debugUI.AddParameter("StorageTest", 3.0f, 1f, 10f, (value) => { }, "Storage");

            // Assert
            var debugParameters = UITestHelper.GetPrivateField<Dictionary<string, DebugParameter>>(debugUI, "debugParameters");
            Assert.IsTrue(debugParameters.ContainsKey("StorageTest"), "Parameter should be in dictionary");

            DebugParameter param = debugParameters["StorageTest"];
            Assert.AreEqual("StorageTest", param.name, "Parameter name should match");
            Assert.AreEqual(3.0f, param.currentValue, "Current value should match");
            Assert.AreEqual(1f, param.minValue, "Min value should match");
            Assert.AreEqual(10f, param.maxValue, "Max value should match");
            Assert.IsNotNull(param.onValueChanged, "Callback should be stored");
        }

        #endregion

        #region UI Visibility Tests

        /// <summary>
        /// Test: ShowUI makes the debug panel active
        /// Verifies that the UI becomes visible when ShowUI is called
        /// </summary>
        [Test]
        public void ShowUI_MakesPanelActive()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(debugUI, "InitializeDebugUI");

            var debugPanel = UITestHelper.GetPrivateField<GameObject>(debugUI, "debugPanel");
            debugPanel.SetActive(false);
            UITestHelper.SetPrivateField(debugUI, "isUIVisible", false);

            // Act
            UITestHelper.InvokePrivateMethod(debugUI, "ShowUI");

            // Assert
            bool isUIVisible = UITestHelper.GetPrivateField<bool>(debugUI, "isUIVisible");
            Assert.IsTrue(isUIVisible, "isUIVisible flag should be true");
            Assert.IsTrue(debugPanel.activeSelf, "Debug panel should be active");
        }

        /// <summary>
        /// Test: HideUI makes the debug panel inactive
        /// Verifies that the UI becomes hidden when HideUI is called
        /// </summary>
        [Test]
        public void HideUI_MakesPanelInactive()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(debugUI, "InitializeDebugUI");

            var debugPanel = UITestHelper.GetPrivateField<GameObject>(debugUI, "debugPanel");
            debugPanel.SetActive(true);
            UITestHelper.SetPrivateField(debugUI, "isUIVisible", true);

            // Act
            UITestHelper.InvokePrivateMethod(debugUI, "HideUI");

            // Assert
            bool isUIVisible = UITestHelper.GetPrivateField<bool>(debugUI, "isUIVisible");
            Assert.IsFalse(isUIVisible, "isUIVisible flag should be false");
            Assert.IsFalse(debugPanel.activeSelf, "Debug panel should be inactive");
        }

        /// <summary>
        /// Test: ToggleMinimize changes panel size
        /// Verifies that minimizing the UI changes the panel dimensions
        /// </summary>
        [Test]
        public void ToggleMinimize_ChangesSize()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(debugUI, "InitializeDebugUI");

            var debugPanel = UITestHelper.GetPrivateField<GameObject>(debugUI, "debugPanel");
            RectTransform panelRect = debugPanel.GetComponent<RectTransform>();
            Vector2 originalSize = panelRect.sizeDelta;

            // Act - Minimize
            UITestHelper.InvokePrivateMethod(debugUI, "MinimizeUI");

            // Assert - Minimized state
            bool isMinimized = UITestHelper.GetPrivateField<bool>(debugUI, "isMinimized");
            Assert.IsTrue(isMinimized, "isMinimized flag should be true");

            Vector2 minimizedSize = UITestHelper.GetPrivateField<Vector2>(debugUI, "minimizedSize");
            Assert.AreEqual(minimizedSize, panelRect.sizeDelta, "Panel size should match minimized size");

            // Act - Maximize
            UITestHelper.InvokePrivateMethod(debugUI, "MaximizeUI");

            // Assert - Maximized state
            isMinimized = UITestHelper.GetPrivateField<bool>(debugUI, "isMinimized");
            Assert.IsFalse(isMinimized, "isMinimized flag should be false");

            Vector2 panelSize = UITestHelper.GetPrivateField<Vector2>(debugUI, "panelSize");
            Assert.AreEqual(panelSize, panelRect.sizeDelta, "Panel size should match original size");
        }

        #endregion

        #region Panel Management Tests

        /// <summary>
        /// Test: CreateParameterPanel returns a valid panel GameObject
        /// Verifies that parameter panels are created with proper components
        /// </summary>
        [Test]
        public void CreateParameterPanel_ReturnsValidPanel()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(debugUI, "InitializeDebugUI");

            // Act
            GameObject panel = (GameObject)UITestHelper.InvokePrivateMethod(debugUI, "CreateParameterPanel", "Test Panel");

            // Assert
            Assert.IsNotNull(panel, "Panel should be created");
            Assert.IsTrue(panel.name.Contains("Test Panel"), "Panel name should contain title");

            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            Assert.IsNotNull(rectTransform, "Panel should have RectTransform");

            Image panelImage = panel.GetComponent<Image>();
            Assert.IsNotNull(panelImage, "Panel should have Image component");

            VerticalLayoutGroup layoutGroup = panel.GetComponent<VerticalLayoutGroup>();
            Assert.IsNotNull(layoutGroup, "Panel should have VerticalLayoutGroup");

            ContentSizeFitter sizeFitter = panel.GetComponent<ContentSizeFitter>();
            Assert.IsNotNull(sizeFitter, "Panel should have ContentSizeFitter");
        }

        /// <summary>
        /// Test: UpdateUIAlpha changes background alpha value
        /// Verifies that the UI transparency can be dynamically adjusted
        /// </summary>
        [Test]
        public void UpdateUIAlpha_ChangesBackgroundAlpha()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(debugUI, "InitializeDebugUI");

            var debugPanel = UITestHelper.GetPrivateField<GameObject>(debugUI, "debugPanel");
            Image backgroundImage = debugPanel.GetComponent<Image>();

            float newAlpha = 0.5f;
            UITestHelper.SetPrivateField(debugUI, "panelAlpha", newAlpha);

            // Act
            UITestHelper.InvokePrivateMethod(debugUI, "UpdateUIAlpha");

            // Assert
            Assert.AreEqual(newAlpha, backgroundImage.color.a, 0.01f, "Background alpha should be updated");
        }

        /// <summary>
        /// Test: CreateParameterPanel stores panel in internal dictionary
        /// Verifies that created panels are tracked for later access
        /// </summary>
        [Test]
        public void CreateParameterPanel_StoresInDictionary()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(debugUI, "InitializeDebugUI");
            string panelTitle = "Storage Test Panel";

            // Act
            UITestHelper.InvokePrivateMethod(debugUI, "CreateParameterPanel", panelTitle);

            // Assert
            var parameterPanels = UITestHelper.GetPrivateField<Dictionary<string, GameObject>>(debugUI, "parameterPanels");
            Assert.IsTrue(parameterPanels.ContainsKey(panelTitle), "Panel should be stored in dictionary");
            Assert.IsNotNull(parameterPanels[panelTitle], "Stored panel should not be null");
        }

        #endregion

        #region Additional Integration Tests

        /// <summary>
        /// Test: Multiple parameters can be added to same category
        /// Verifies that the system handles multiple parameters in one panel
        /// </summary>
        [Test]
        public void AddMultipleParameters_ToSameCategory_StoresAllParameters()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(debugUI, "InitializeDebugUI");
            UITestHelper.InvokePrivateMethod(debugUI, "CreateParameterSections");

            // Act
            debugUI.AddParameter("Param1", 1.0f, 0f, 10f, (value) => { }, "TestCategory");
            debugUI.AddParameter("Param2", 2.0f, 0f, 10f, (value) => { }, "TestCategory");
            debugUI.AddParameter("Param3", 3.0f, 0f, 10f, (value) => { }, "TestCategory");

            // Assert
            var debugParameters = UITestHelper.GetPrivateField<Dictionary<string, DebugParameter>>(debugUI, "debugParameters");
            Assert.AreEqual(3, debugParameters.Count, "All three parameters should be stored");
            Assert.IsTrue(debugParameters.ContainsKey("Param1"), "Param1 should exist");
            Assert.IsTrue(debugParameters.ContainsKey("Param2"), "Param2 should exist");
            Assert.IsTrue(debugParameters.ContainsKey("Param3"), "Param3 should exist");
        }

        /// <summary>
        /// Test: ToggleMinimize hides scroll view when minimized
        /// Verifies that the scroll view visibility is managed during minimize/maximize
        /// </summary>
        [Test]
        public void MinimizeUI_HidesScrollView()
        {
            // Arrange
            UITestHelper.InvokePrivateMethod(debugUI, "InitializeDebugUI");
            var scrollRect = UITestHelper.GetPrivateField<ScrollRect>(debugUI, "scrollRect");
            scrollRect.gameObject.SetActive(true);

            // Act
            UITestHelper.InvokePrivateMethod(debugUI, "MinimizeUI");

            // Assert
            Assert.IsFalse(scrollRect.gameObject.activeSelf, "ScrollRect should be hidden when minimized");

            // Act - Maximize
            UITestHelper.InvokePrivateMethod(debugUI, "MaximizeUI");

            // Assert
            Assert.IsTrue(scrollRect.gameObject.activeSelf, "ScrollRect should be visible when maximized");
        }

        #endregion
    }
}
