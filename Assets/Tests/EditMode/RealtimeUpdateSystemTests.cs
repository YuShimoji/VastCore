using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Vastcore.UI;
using System.Collections.Generic;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// RealtimeUpdateSystem のテストスイート
    /// 登録、スロットリング、優先度キュー、パフォーマンス適応をテスト
    ///
    /// EditMode 制限事項:
    /// - コルーチンは実行されない（内部状態の変更のみをテスト）
    /// - Time.time は固定値を返す可能性がある
    /// - 非同期動作は直接テストできない
    /// </summary>
    [TestFixture]
    public class RealtimeUpdateSystemTests
    {
        private GameObject systemObject;
        private RealtimeUpdateSystem updateSystem;

        [SetUp]
        public void SetUp()
        {
            // Create RealtimeUpdateSystem GameObject
            systemObject = new GameObject("TestRealtimeUpdateSystem");
            updateSystem = systemObject.AddComponent<RealtimeUpdateSystem>();

            // Disable auto-start for manual control in tests
            UITestHelper.SetPrivateField(updateSystem, "enableRealtimeUpdates", false);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up all created objects
            if (systemObject != null)
            {
                Object.DestroyImmediate(systemObject);
            }
        }

        #region Registration Tests

        [Test]
        public void RegisterParameter_AddsToUpdateDictionary()
        {
            // Arrange
            string paramName = "TestParam";
            bool callbackInvoked = false;
            System.Action<float> callback = (value) => callbackInvoked = true;

            // Act
            updateSystem.RegisterParameter(paramName, callback);

            // Assert
            var pendingUpdates = UITestHelper.GetPrivateField<Dictionary<string, ParameterUpdateData>>(
                updateSystem, "pendingUpdates");
            Assert.IsTrue(pendingUpdates.ContainsKey(paramName),
                "Parameter should be added to pendingUpdates dictionary");
            Assert.AreEqual(paramName, pendingUpdates[paramName].parameterName,
                "Parameter name should match");
            Assert.IsNotNull(pendingUpdates[paramName].updateCallback,
                "Callback should be stored");
        }

        [Test]
        public void RegisterParameter_AddsToLastUpdateTimesDictionary()
        {
            // Arrange
            string paramName = "TestParam";
            System.Action<float> callback = (value) => { };

            // Act
            updateSystem.RegisterParameter(paramName, callback);

            // Assert
            var lastUpdateTimes = UITestHelper.GetPrivateField<Dictionary<string, float>>(
                updateSystem, "lastUpdateTimes");
            Assert.IsTrue(lastUpdateTimes.ContainsKey(paramName),
                "Parameter should be added to lastUpdateTimes dictionary");
            Assert.AreEqual(0f, lastUpdateTimes[paramName],
                "Initial last update time should be 0");
        }

        [Test]
        public void UnregisterParameter_RemovesFromDictionary()
        {
            // Arrange
            string paramName = "TestParam";
            updateSystem.RegisterParameter(paramName, (value) => { });

            // Act
            updateSystem.UnregisterParameter(paramName);

            // Assert
            var pendingUpdates = UITestHelper.GetPrivateField<Dictionary<string, ParameterUpdateData>>(
                updateSystem, "pendingUpdates");
            var lastUpdateTimes = UITestHelper.GetPrivateField<Dictionary<string, float>>(
                updateSystem, "lastUpdateTimes");

            Assert.IsFalse(pendingUpdates.ContainsKey(paramName),
                "Parameter should be removed from pendingUpdates dictionary");
            Assert.IsFalse(lastUpdateTimes.ContainsKey(paramName),
                "Parameter should be removed from lastUpdateTimes dictionary");
        }

        [Test]
        public void RegisterParameter_WithCustomThrottle_UsesCustomValue()
        {
            // Arrange
            string paramName = "TestParam";
            float customThrottle = 0.5f;
            System.Action<float> callback = (value) => { };

            // Act
            updateSystem.RegisterParameter(paramName, callback, customThrottle);

            // Assert
            var pendingUpdates = UITestHelper.GetPrivateField<Dictionary<string, ParameterUpdateData>>(
                updateSystem, "pendingUpdates");
            Assert.AreEqual(customThrottle, pendingUpdates[paramName].throttleTime,
                "Custom throttle time should be used");
        }

        [Test]
        public void RegisterParameter_WithoutCustomThrottle_UsesDefaultValue()
        {
            // Arrange
            string paramName = "TestParam";
            float defaultThrottle = 0.1f; // Default from RealtimeUpdateSystem
            UITestHelper.SetPrivateField(updateSystem, "updateThrottleTime", defaultThrottle);
            System.Action<float> callback = (value) => { };

            // Act
            updateSystem.RegisterParameter(paramName, callback);

            // Assert
            var pendingUpdates = UITestHelper.GetPrivateField<Dictionary<string, ParameterUpdateData>>(
                updateSystem, "pendingUpdates");
            Assert.AreEqual(defaultThrottle, pendingUpdates[paramName].throttleTime,
                "Default throttle time should be used when not specified");
        }

        #endregion

        #region Update Throttling Tests

        [Test]
        public void RequestUpdate_WithThrottle_StoresValueForLater()
        {
            // Arrange
            UITestHelper.SetPrivateField(updateSystem, "enableRealtimeUpdates", true);
            string paramName = "TestParam";
            float testValue = 5.5f;
            float throttle = 0.1f;
            bool callbackInvoked = false;

            updateSystem.RegisterParameter(paramName, (value) => callbackInvoked = true, throttle);

            // Simulate that an update just happened
            var lastUpdateTimes = UITestHelper.GetPrivateField<Dictionary<string, float>>(
                updateSystem, "lastUpdateTimes");
            lastUpdateTimes[paramName] = Time.time;

            // Act - Request update while throttled
            updateSystem.RequestUpdate(paramName, testValue);

            // Assert - Value should be stored as pending
            var pendingUpdates = UITestHelper.GetPrivateField<Dictionary<string, ParameterUpdateData>>(
                updateSystem, "pendingUpdates");
            Assert.AreEqual(testValue, pendingUpdates[paramName].pendingValue,
                "Pending value should be stored when throttled");

            // Note: Callback won't be invoked immediately due to throttling
            // In EditMode, the queue processing coroutine won't run
        }

        [Test]
        public void RequestUpdate_ImmediatePriority_ExecutesImmediately()
        {
            // Arrange
            UITestHelper.SetPrivateField(updateSystem, "enableRealtimeUpdates", true);
            string paramName = "TestParam";
            float testValue = 7.5f;
            float receivedValue = 0f;

            updateSystem.RegisterParameter(paramName, (value) => receivedValue = value);

            // Simulate that an update just happened (would normally throttle)
            var lastUpdateTimes = UITestHelper.GetPrivateField<Dictionary<string, float>>(
                updateSystem, "lastUpdateTimes");
            lastUpdateTimes[paramName] = Time.time;

            // Act - Request with immediate priority bypasses throttle
            updateSystem.RequestUpdate(paramName, testValue, UpdatePriority.Immediate);

            // Assert
            Assert.AreEqual(testValue, receivedValue,
                "Immediate priority should execute callback immediately");
        }

        [Test]
        public void ForceUpdate_BypassesThrottle()
        {
            // Arrange
            string paramName = "TestParam";
            float testValue = 9.5f;
            float receivedValue = 0f;

            updateSystem.RegisterParameter(paramName, (value) => receivedValue = value);

            // Simulate that an update just happened (would normally throttle)
            var lastUpdateTimes = UITestHelper.GetPrivateField<Dictionary<string, float>>(
                updateSystem, "lastUpdateTimes");
            lastUpdateTimes[paramName] = Time.time;

            // Act - Force update bypasses throttle
            updateSystem.ForceUpdate(paramName, testValue);

            // Assert
            Assert.AreEqual(testValue, receivedValue,
                "ForceUpdate should execute callback immediately regardless of throttle");
        }

        [Test]
        public void CanUpdateNow_ImmediatePriority_ReturnsTrue()
        {
            // Arrange
            string paramName = "TestParam";
            updateSystem.RegisterParameter(paramName, (value) => { });

            // Set last update time to recent (would normally throttle)
            var lastUpdateTimes = UITestHelper.GetPrivateField<Dictionary<string, float>>(
                updateSystem, "lastUpdateTimes");
            lastUpdateTimes[paramName] = Time.time;

            // Act - Test via reflection since CanUpdateNow is private
            bool canUpdate = (bool)UITestHelper.InvokePrivateMethod(
                updateSystem, "CanUpdateNow", paramName, Time.time, UpdatePriority.Immediate);

            // Assert
            Assert.IsTrue(canUpdate,
                "CanUpdateNow should return true for Immediate priority regardless of throttle");
        }

        [Test]
        public void CanUpdateNow_WithinThrottle_ReturnsFalse()
        {
            // Arrange
            string paramName = "TestParam";
            float throttle = 0.1f;
            updateSystem.RegisterParameter(paramName, (value) => { }, throttle);

            // Set last update time to very recent
            var lastUpdateTimes = UITestHelper.GetPrivateField<Dictionary<string, float>>(
                updateSystem, "lastUpdateTimes");
            float currentTime = Time.time;
            lastUpdateTimes[paramName] = currentTime - 0.01f; // 0.01s ago, within 0.1s throttle

            // Act
            bool canUpdate = (bool)UITestHelper.InvokePrivateMethod(
                updateSystem, "CanUpdateNow", paramName, currentTime, UpdatePriority.Normal);

            // Assert
            Assert.IsFalse(canUpdate,
                "CanUpdateNow should return false when within throttle period");
        }

        [Test]
        public void CanUpdateNow_AfterThrottle_ReturnsTrue()
        {
            // Arrange
            string paramName = "TestParam";
            float throttle = 0.1f;
            updateSystem.RegisterParameter(paramName, (value) => { }, throttle);

            // Set last update time to past throttle period
            var lastUpdateTimes = UITestHelper.GetPrivateField<Dictionary<string, float>>(
                updateSystem, "lastUpdateTimes");
            float currentTime = Time.time;
            lastUpdateTimes[paramName] = currentTime - 0.2f; // 0.2s ago, past 0.1s throttle

            // Act
            bool canUpdate = (bool)UITestHelper.InvokePrivateMethod(
                updateSystem, "CanUpdateNow", paramName, currentTime, UpdatePriority.Normal);

            // Assert
            Assert.IsTrue(canUpdate,
                "CanUpdateNow should return true when past throttle period");
        }

        #endregion

        #region Priority Queue Tests

        [Test]
        public void SetParameterPriority_UpdatesPriority()
        {
            // Arrange
            string paramName = "TestParam";
            updateSystem.RegisterParameter(paramName, (value) => { });

            var pendingUpdates = UITestHelper.GetPrivateField<Dictionary<string, ParameterUpdateData>>(
                updateSystem, "pendingUpdates");
            UpdatePriority initialPriority = pendingUpdates[paramName].priority;

            // Act
            updateSystem.SetParameterPriority(paramName, UpdatePriority.High);

            // Assert
            Assert.AreEqual(UpdatePriority.Normal, initialPriority,
                "Initial priority should be Normal");
            Assert.AreEqual(UpdatePriority.High, pendingUpdates[paramName].priority,
                "Priority should be updated to High");
        }

        [Test]
        public void SetParameterPriority_ForUnregisteredParameter_DoesNotThrow()
        {
            // Arrange
            string paramName = "NonExistentParam";

            // Act & Assert - Should not throw exception
            Assert.DoesNotThrow(() => updateSystem.SetParameterPriority(paramName, UpdatePriority.High),
                "Setting priority for unregistered parameter should not throw");
        }

        [Test]
        public void UpdateQueue_StoresRequestedUpdates()
        {
            // Arrange
            UITestHelper.SetPrivateField(updateSystem, "enableRealtimeUpdates", true);
            string paramName = "TestParam";
            float testValue = 3.5f;

            updateSystem.RegisterParameter(paramName, (value) => { }, throttleTime: 0.1f);

            // Set last update time to recent to trigger queuing
            var lastUpdateTimes = UITestHelper.GetPrivateField<Dictionary<string, float>>(
                updateSystem, "lastUpdateTimes");
            lastUpdateTimes[paramName] = Time.time;

            // Act
            updateSystem.RequestUpdate(paramName, testValue);

            // Assert - Check that update was queued
            var updateQueue = UITestHelper.GetPrivateField<Queue<ParameterUpdateData>>(
                updateSystem, "updateQueue");

            // Queue should contain the pending update
            // Note: In EditMode, the queue won't be processed by coroutine
            Assert.Greater(updateQueue.Count, 0,
                "Update should be added to queue when throttled");
        }

        #endregion

        #region Performance Tests

        [Test]
        public void PerformanceLimited_ReducesThrottle()
        {
            // Arrange
            float originalThrottle = 0.1f;
            UITestHelper.SetPrivateField(updateSystem, "updateThrottleTime", originalThrottle);
            UITestHelper.SetPrivateField(updateSystem, "enablePerformanceMonitoring", true);

            // Simulate performance-limited state
            UITestHelper.SetPrivateField(updateSystem, "isPerformanceLimited", false);

            // Add high frame times to trigger performance limitation
            var recentFrameTimes = UITestHelper.GetPrivateField<List<float>>(
                updateSystem, "recentFrameTimes");
            for (int i = 0; i < 60; i++)
            {
                recentFrameTimes.Add(25f); // 25ms per frame (exceeds 16.67ms target)
            }

            // Act - Invoke performance check
            UITestHelper.InvokePrivateMethod(updateSystem, "CheckPerformance");

            // Assert
            bool isPerformanceLimited = UITestHelper.GetPrivateField<bool>(
                updateSystem, "isPerformanceLimited");
            float currentThrottle = UITestHelper.GetPrivateField<float>(
                updateSystem, "updateThrottleTime");

            Assert.IsTrue(isPerformanceLimited,
                "System should enter performance-limited mode with high frame times");
            Assert.Greater(currentThrottle, originalThrottle,
                "Throttle time should increase when performance-limited");
        }

        [Test]
        public void PerformanceLimited_ReducesMaxUpdatesPerFrame()
        {
            // Arrange
            int originalMaxUpdates = 5;
            UITestHelper.SetPrivateField(updateSystem, "maxUpdatesPerFrame", originalMaxUpdates);
            UITestHelper.SetPrivateField(updateSystem, "baseMaxUpdatesPerFrame", originalMaxUpdates);
            UITestHelper.SetPrivateField(updateSystem, "enablePerformanceMonitoring", true);
            UITestHelper.SetPrivateField(updateSystem, "isPerformanceLimited", false);

            // Add high frame times
            var recentFrameTimes = UITestHelper.GetPrivateField<List<float>>(
                updateSystem, "recentFrameTimes");
            for (int i = 0; i < 60; i++)
            {
                recentFrameTimes.Add(25f); // Exceeds target
            }

            // Act
            UITestHelper.InvokePrivateMethod(updateSystem, "CheckPerformance");

            // Assert
            int currentMaxUpdates = UITestHelper.GetPrivateField<int>(
                updateSystem, "maxUpdatesPerFrame");

            Assert.Less(currentMaxUpdates, originalMaxUpdates,
                "maxUpdatesPerFrame should decrease when performance-limited");
        }

        [Test]
        public void GetPerformanceStats_ReturnsValidStats()
        {
            // Arrange
            UITestHelper.SetPrivateField(updateSystem, "enableRealtimeUpdates", true);

            // Add some frame time data
            var recentFrameTimes = UITestHelper.GetPrivateField<List<float>>(
                updateSystem, "recentFrameTimes");
            recentFrameTimes.Add(16f);
            recentFrameTimes.Add(17f);
            recentFrameTimes.Add(15f);

            // Register some parameters
            updateSystem.RegisterParameter("Param1", (v) => { });
            updateSystem.RegisterParameter("Param2", (v) => { });

            // Act
            var stats = updateSystem.GetPerformanceStats();

            // Assert
            Assert.Greater(stats.averageFrameTime, 0f,
                "Average frame time should be calculated");
            Assert.AreEqual(2, stats.registeredParametersCount,
                "Should report correct number of registered parameters");
            Assert.GreaterOrEqual(stats.pendingUpdatesCount, 0,
                "Pending updates count should be non-negative");
            Assert.Greater(stats.currentThrottleTime, 0f,
                "Current throttle time should be positive");
        }

        [Test]
        public void GetPerformanceStats_WithNoFrameData_ReturnsZeroAverageFrameTime()
        {
            // Act
            var stats = updateSystem.GetPerformanceStats();

            // Assert
            Assert.AreEqual(0f, stats.averageFrameTime,
                "Average frame time should be 0 when no frame data exists");
        }

        #endregion

        #region Property Tests

        [Test]
        public void EnableRealtimeUpdates_Property_CanBeSet()
        {
            // Act
            updateSystem.EnableRealtimeUpdates = true;

            // Assert
            Assert.IsTrue(updateSystem.EnableRealtimeUpdates,
                "EnableRealtimeUpdates property should be settable");
        }

        [Test]
        public void UpdateThrottleTime_Property_ClampsToMinimum()
        {
            // Act
            updateSystem.UpdateThrottleTime = 0.005f; // Below minimum

            // Assert
            Assert.GreaterOrEqual(updateSystem.UpdateThrottleTime, 0.01f,
                "UpdateThrottleTime should be clamped to minimum of 0.01f");
        }

        [Test]
        public void UpdateThrottleTime_Property_AcceptsValidValues()
        {
            // Arrange
            float testValue = 0.25f;

            // Act
            updateSystem.UpdateThrottleTime = testValue;

            // Assert
            Assert.AreEqual(testValue, updateSystem.UpdateThrottleTime,
                "UpdateThrottleTime should accept valid values");
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void RequestUpdate_WithDisabledSystem_DoesNotExecute()
        {
            // Arrange
            UITestHelper.SetPrivateField(updateSystem, "enableRealtimeUpdates", false);
            string paramName = "TestParam";
            bool callbackInvoked = false;

            updateSystem.RegisterParameter(paramName, (value) => callbackInvoked = true);

            // Act
            updateSystem.RequestUpdate(paramName, 5.0f);

            // Assert
            Assert.IsFalse(callbackInvoked,
                "Update should not execute when system is disabled");
        }

        [Test]
        public void RequestUpdate_UnregisteredParameter_DoesNotThrow()
        {
            // Arrange
            UITestHelper.SetPrivateField(updateSystem, "enableRealtimeUpdates", true);
            string paramName = "NonExistentParam";

            // Act & Assert
            Assert.DoesNotThrow(() => updateSystem.RequestUpdate(paramName, 5.0f),
                "Requesting update for unregistered parameter should not throw");
        }

        [Test]
        public void ForceUpdate_UnregisteredParameter_DoesNotThrow()
        {
            // Arrange
            string paramName = "NonExistentParam";

            // Act & Assert
            Assert.DoesNotThrow(() => updateSystem.ForceUpdate(paramName, 5.0f),
                "Force update for unregistered parameter should not throw");
        }

        [Test]
        public void ExecuteUpdate_WithNullCallback_DoesNotThrow()
        {
            // Arrange
            string paramName = "TestParam";
            updateSystem.RegisterParameter(paramName, null);

            var pendingUpdates = UITestHelper.GetPrivateField<Dictionary<string, ParameterUpdateData>>(
                updateSystem, "pendingUpdates");
            pendingUpdates[paramName].pendingValue = 5.0f;

            // Act & Assert
            Assert.DoesNotThrow(() => UITestHelper.InvokePrivateMethod(
                updateSystem, "ExecuteUpdate", pendingUpdates[paramName]),
                "ExecuteUpdate with null callback should not throw");
        }

        [Test]
        public void ExecuteUpdate_WithExceptionInCallback_CatchesAndLogs()
        {
            // Arrange
            string paramName = "TestParam";
            bool exceptionThrown = false;

            updateSystem.RegisterParameter(paramName, (value) =>
            {
                exceptionThrown = true;
                throw new System.Exception("Test exception");
            });

            var pendingUpdates = UITestHelper.GetPrivateField<Dictionary<string, ParameterUpdateData>>(
                updateSystem, "pendingUpdates");
            pendingUpdates[paramName].pendingValue = 5.0f;

            // Expect the Debug.LogError that ExecuteUpdate emits when catching the exception
            LogAssert.Expect(LogType.Error, "Error executing update for parameter 'TestParam': Test exception");

            // Act & Assert - Should catch exception internally
            Assert.DoesNotThrow(() => UITestHelper.InvokePrivateMethod(
                updateSystem, "ExecuteUpdate", pendingUpdates[paramName]),
                "ExecuteUpdate should catch callback exceptions");

            Assert.IsTrue(exceptionThrown,
                "Callback should have been invoked despite exception");
        }

        [Test]
        public void MultipleParameters_CanBeRegisteredIndependently()
        {
            // Arrange
            string param1 = "Param1";
            string param2 = "Param2";
            float value1 = 0f;
            float value2 = 0f;

            // Act
            updateSystem.RegisterParameter(param1, (v) => value1 = v, throttleTime: 0.1f);
            updateSystem.RegisterParameter(param2, (v) => value2 = v, throttleTime: 0.2f);

            updateSystem.ForceUpdate(param1, 10f);
            updateSystem.ForceUpdate(param2, 20f);

            // Assert
            var pendingUpdates = UITestHelper.GetPrivateField<Dictionary<string, ParameterUpdateData>>(
                updateSystem, "pendingUpdates");

            Assert.AreEqual(2, pendingUpdates.Count,
                "Both parameters should be registered");
            Assert.AreEqual(10f, value1,
                "First parameter should be updated independently");
            Assert.AreEqual(20f, value2,
                "Second parameter should be updated independently");
            Assert.AreEqual(0.1f, pendingUpdates[param1].throttleTime,
                "First parameter should have its own throttle time");
            Assert.AreEqual(0.2f, pendingUpdates[param2].throttleTime,
                "Second parameter should have its own throttle time");
        }

        #endregion
    }
}
