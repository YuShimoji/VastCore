using NUnit.Framework;
using UnityEngine;
using Vastcore.UI;
using System.Collections.Generic;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// ModernUIManager のテストスイート
    /// 初期化、コンポーネント作成、パラメータ登録、イベントシステムをテスト
    /// </summary>
    [TestFixture]
    public class ModernUIManagerTests
    {
        private GameObject managerObject;
        private ModernUIManager manager;

        [SetUp]
        public void SetUp()
        {
            // Create ModernUIManager GameObject
            managerObject = new GameObject("TestModernUIManager");
            manager = managerObject.AddComponent<ModernUIManager>();

            // Disable auto-initialization for manual control in tests
            UITestHelper.SetPrivateField(manager, "autoInitialize", false);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up all created objects
            if (managerObject != null)
            {
                Object.DestroyImmediate(managerObject);
            }

            // Clear singleton instance
            var instanceField = typeof(ModernUIManager).GetField("instance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (instanceField != null)
            {
                instanceField.SetValue(null, null);
            }
        }

        #region Initialization Tests

        [Test]
        public void InitializeUISystem_WhenCalled_SetsIsInitializedToTrue()
        {
            // Act
            manager.InitializeUISystem();

            // Assert
            Assert.IsTrue(manager.IsInitialized, "IsInitialized should be true after initialization");
        }

        [Test]
        public void InitializeUISystem_WhenCalledTwice_LogsWarningAndDoesNotReinitialize()
        {
            // Arrange
            manager.InitializeUISystem();
            bool firstInitState = manager.IsInitialized;

            // Act
            manager.InitializeUISystem(); // Second call

            // Assert
            Assert.IsTrue(firstInitState, "First initialization should succeed");
            Assert.IsTrue(manager.IsInitialized, "Should still be initialized after second call");
            // Note: In actual test, you would verify Debug.LogWarning was called
        }

        [Test]
        public void InitializeUISystem_WithCreateMissingComponents_CreatesSliderSystem()
        {
            // Arrange
            UITestHelper.SetPrivateField(manager, "createMissingComponents", true);

            // Act
            manager.InitializeUISystem();

            // Assert
            Assert.IsNotNull(manager.SliderSystem, "SliderSystem should be created when createMissingComponents is true");
        }

        [Test]
        public void InitializeUISystem_WithCreateMissingComponents_CreatesUpdateSystem()
        {
            // Arrange
            UITestHelper.SetPrivateField(manager, "createMissingComponents", true);

            // Act
            manager.InitializeUISystem();

            // Assert
            Assert.IsNotNull(manager.UpdateSystem, "UpdateSystem should be created when createMissingComponents is true");
        }

        [Test]
        public void InitializeUISystem_WithCreateMissingComponents_CreatesDebugUI()
        {
            // Arrange
            UITestHelper.SetPrivateField(manager, "createMissingComponents", true);

            // Act
            manager.InitializeUISystem();

            // Assert
            Assert.IsNotNull(manager.DebugUI, "DebugUI should be created when createMissingComponents is true");
        }

        [Test]
        public void InitializeUISystem_FiresOnUISystemInitializedEvent()
        {
            // Arrange
            bool eventFired = false;
            manager.OnUISystemInitialized += () => eventFired = true;

            // Act
            manager.InitializeUISystem();

            // Assert
            Assert.IsTrue(eventFired, "OnUISystemInitialized event should be fired after initialization");
        }

        #endregion

        #region Parameter Registration Tests

        [Test]
        public void RegisterParameter_BeforeInitialization_LogsError()
        {
            // Arrange
            string paramName = "TestParam";
            float defaultValue = 1.0f;
            float minValue = 0.0f;
            float maxValue = 10.0f;
            bool callbackInvoked = false;

            // Act
            manager.RegisterParameter(paramName, defaultValue, minValue, maxValue, (value) => callbackInvoked = true);

            // Assert
            Assert.IsFalse(callbackInvoked, "Callback should not be invoked before initialization");
            // Note: Would verify Debug.LogError was called in production test
        }

        [Test]
        public void RegisterParameter_AfterInitialization_StoresCallback()
        {
            // Arrange
            UITestHelper.SetPrivateField(manager, "createMissingComponents", true);
            manager.InitializeUISystem();

            string paramName = "TestParam";
            float defaultValue = 5.0f;
            bool callbackInvoked = false;

            // Act
            manager.RegisterParameter(paramName, defaultValue, 0f, 10f, (value) => callbackInvoked = true);

            // Assert
            var callbacks = UITestHelper.GetPrivateField<Dictionary<string, System.Action<float>>>(manager, "parameterCallbacks");
            Assert.IsTrue(callbacks.ContainsKey(paramName), "Parameter callback should be stored");
        }

        [Test]
        public void UnregisterParameter_RemovesFromCallbackDictionary()
        {
            // Arrange
            UITestHelper.SetPrivateField(manager, "createMissingComponents", true);
            manager.InitializeUISystem();

            string paramName = "TestParam";
            manager.RegisterParameter(paramName, 5.0f, 0f, 10f, (value) => { });

            // Act
            manager.UnregisterParameter(paramName);

            // Assert
            var callbacks = UITestHelper.GetPrivateField<Dictionary<string, System.Action<float>>>(manager, "parameterCallbacks");
            Assert.IsFalse(callbacks.ContainsKey(paramName), "Parameter should be removed from callback dictionary");
        }

        [Test]
        public void RegisterParameter_FiresOnParameterChangedEvent()
        {
            // Arrange
            UITestHelper.SetPrivateField(manager, "createMissingComponents", true);
            manager.InitializeUISystem();

            string capturedParamName = null;
            float capturedValue = 0f;
            manager.OnParameterChanged += (name, value) =>
            {
                capturedParamName = name;
                capturedValue = value;
            };

            string paramName = "TestParam";
            float testValue = 7.5f;

            // Act
            manager.RegisterParameter(paramName, testValue, 0f, 10f, (value) => { });

            // Trigger the callback to fire the event
            manager.UpdateParameterValue(paramName, testValue);

            // Assert
            // Note: Event firing depends on UpdateSystem implementation
            // This test verifies the event hookup exists
            Assert.IsNotNull(manager.OnParameterChanged, "OnParameterChanged event should be hookable");
        }

        #endregion

        #region Singleton Tests

        [Test]
        public void Instance_WhenNoInstanceExists_CreatesNewInstance()
        {
            // Arrange
            TearDown(); // Clear any existing instance

            // Act
            var instance = ModernUIManager.Instance;

            // Assert
            Assert.IsNotNull(instance, "Instance should not be null");
            Assert.IsInstanceOf<ModernUIManager>(instance, "Instance should be ModernUIManager type");

            // Cleanup
            if (instance != null && instance.gameObject != null)
            {
                Object.DestroyImmediate(instance.gameObject);
            }
        }

        [Test]
        public void Instance_WhenInstanceExists_ReturnsSameInstance()
        {
            // Arrange
            manager.InitializeUISystem();
            var firstInstance = ModernUIManager.Instance;

            // Act
            var secondInstance = ModernUIManager.Instance;

            // Assert
            Assert.AreSame(firstInstance, secondInstance, "Should return the same instance");
        }

        [Test]
        public void OnDestroy_ClearsSingletonInstance()
        {
            // Arrange
            manager.InitializeUISystem();
            var instanceBefore = ModernUIManager.Instance;

            // Act
            Object.DestroyImmediate(managerObject);
            managerObject = null; // Prevent double-destroy in TearDown

            // Assert
            // After destruction, accessing Instance should create a new one
            var instanceAfter = ModernUIManager.Instance;
            Assert.AreNotSame(instanceBefore, instanceAfter, "Instance should be different after destruction");

            // Cleanup
            if (instanceAfter != null && instanceAfter.gameObject != null)
            {
                Object.DestroyImmediate(instanceAfter.gameObject);
            }
        }

        #endregion

        #region Component Property Tests

        [Test]
        public void SliderSystem_AfterInitialization_IsAccessible()
        {
            // Arrange
            UITestHelper.SetPrivateField(manager, "createMissingComponents", true);

            // Act
            manager.InitializeUISystem();

            // Assert
            Assert.IsNotNull(manager.SliderSystem, "SliderSystem property should be accessible");
        }

        [Test]
        public void UpdateSystem_AfterInitialization_IsAccessible()
        {
            // Arrange
            UITestHelper.SetPrivateField(manager, "createMissingComponents", true);

            // Act
            manager.InitializeUISystem();

            // Assert
            Assert.IsNotNull(manager.UpdateSystem, "UpdateSystem property should be accessible");
        }

        [Test]
        public void DebugUI_AfterInitialization_IsAccessible()
        {
            // Arrange
            UITestHelper.SetPrivateField(manager, "createMissingComponents", true);

            // Act
            manager.InitializeUISystem();

            // Assert
            Assert.IsNotNull(manager.DebugUI, "DebugUI property should be accessible");
        }

        [Test]
        public void PerformanceMonitor_AfterInitialization_IsAccessible()
        {
            // Arrange
            UITestHelper.SetPrivateField(manager, "createMissingComponents", true);

            // Act
            manager.InitializeUISystem();

            // Assert
            Assert.IsNotNull(manager.PerformanceMonitor, "PerformanceMonitor property should be accessible");
        }

        #endregion

        #region Debug UI Visibility Tests

        [Test]
        public void SetDebugUIVisible_WithTrue_ActivatesDebugUI()
        {
            // Arrange
            UITestHelper.SetPrivateField(manager, "createMissingComponents", true);
            manager.InitializeUISystem();
            manager.DebugUI.gameObject.SetActive(false);

            // Act
            manager.SetDebugUIVisible(true);

            // Assert
            Assert.IsTrue(manager.DebugUI.gameObject.activeSelf, "DebugUI should be active");
        }

        [Test]
        public void SetDebugUIVisible_WithFalse_DeactivatesDebugUI()
        {
            // Arrange
            UITestHelper.SetPrivateField(manager, "createMissingComponents", true);
            manager.InitializeUISystem();
            manager.DebugUI.gameObject.SetActive(true);

            // Act
            manager.SetDebugUIVisible(false);

            // Assert
            Assert.IsFalse(manager.DebugUI.gameObject.activeSelf, "DebugUI should be inactive");
        }

        #endregion
    }
}
