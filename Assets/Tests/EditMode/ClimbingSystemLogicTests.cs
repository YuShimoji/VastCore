using NUnit.Framework;
using UnityEngine;
using Vastcore.Player;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// EnhancedClimbingSystem のロジック検証テスト
    /// スタミナ計算、角度判定、表面検出パラメータの検証
    /// </summary>
    [TestFixture]
    public class ClimbingSystemLogicTests
    {
        private GameObject playerObject;
        private EnhancedClimbingSystem climbingSystem;
        private CharacterController characterController;
        private AdvancedPlayerController playerController;

        [SetUp]
        public void SetUp()
        {
            playerObject = new GameObject("TestClimbingPlayer");
            characterController = playerObject.AddComponent<CharacterController>();
            playerController = playerObject.AddComponent<AdvancedPlayerController>();
            climbingSystem = playerObject.AddComponent<EnhancedClimbingSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (playerObject != null)
            {
                Object.DestroyImmediate(playerObject);
            }
        }

        #region Basic Climbing Parameters Tests

        [Test]
        public void ClimbSpeed_DefaultValue_IsReasonable()
        {
            // Using reflection to access private field
            var field = typeof(EnhancedClimbingSystem).GetField("climbSpeed",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float climbSpeed = (float)field.GetValue(climbingSystem);

            Assert.AreEqual(8f, climbSpeed);
            Assert.Greater(climbSpeed, 0f, "Climb speed should be positive");
        }

        [Test]
        public void ClimbAcceleration_DefaultValue_IsPositive()
        {
            var field = typeof(EnhancedClimbingSystem).GetField("climbAcceleration",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float climbAcceleration = (float)field.GetValue(climbingSystem);

            Assert.AreEqual(10f, climbAcceleration);
            Assert.Greater(climbAcceleration, 0f, "Climb acceleration should be positive");
        }

        [Test]
        public void MaxClimbSpeed_GreaterThanBaseSpeed()
        {
            var baseSpeedField = typeof(EnhancedClimbingSystem).GetField("climbSpeed",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxSpeedField = typeof(EnhancedClimbingSystem).GetField("maxClimbSpeed",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            float climbSpeed = (float)baseSpeedField.GetValue(climbingSystem);
            float maxClimbSpeed = (float)maxSpeedField.GetValue(climbingSystem);

            Assert.AreEqual(15f, maxClimbSpeed);
            Assert.Greater(maxClimbSpeed, climbSpeed,
                "Max climb speed should be greater than base climb speed");
        }

        [Test]
        public void ClimbDetectionRadius_DefaultValue_IsReasonable()
        {
            var field = typeof(EnhancedClimbingSystem).GetField("climbDetectionRadius",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float radius = (float)field.GetValue(climbingSystem);

            Assert.AreEqual(1.5f, radius);
            Assert.That(radius, Is.InRange(0.5f, 3f),
                "Climb detection radius should be in reasonable range");
        }

        #endregion

        #region Stamina System Tests

        [Test]
        public void ClimbStamina_DefaultValue_IsPositive()
        {
            var field = typeof(EnhancedClimbingSystem).GetField("climbStamina",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float stamina = (float)field.GetValue(climbingSystem);

            Assert.AreEqual(100f, stamina);
            Assert.Greater(stamina, 0f, "Climb stamina should be positive");
        }

        [Test]
        public void StaminaConsumption_LessThanMaxStamina()
        {
            var staminaField = typeof(EnhancedClimbingSystem).GetField("climbStamina",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var consumptionField = typeof(EnhancedClimbingSystem).GetField("staminaConsumption",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            float stamina = (float)staminaField.GetValue(climbingSystem);
            float consumption = (float)consumptionField.GetValue(climbingSystem);

            Assert.AreEqual(20f, consumption);
            Assert.Less(consumption, stamina,
                "Stamina consumption should be less than max stamina");
        }

        [Test]
        public void StaminaRegenRate_IsPositive()
        {
            var field = typeof(EnhancedClimbingSystem).GetField("staminaRegenRate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float regenRate = (float)field.GetValue(climbingSystem);

            Assert.AreEqual(15f, regenRate);
            Assert.Greater(regenRate, 0f, "Stamina regen rate should be positive");
        }

        [Test]
        public void StaminaBalance_ConsumptionExceedsRegen()
        {
            var regenField = typeof(EnhancedClimbingSystem).GetField("staminaRegenRate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var consumptionField = typeof(EnhancedClimbingSystem).GetField("staminaConsumption",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            float regenRate = (float)regenField.GetValue(climbingSystem);
            float consumption = (float)consumptionField.GetValue(climbingSystem);

            Assert.Greater(consumption, regenRate,
                "Stamina consumption should exceed regen for balanced gameplay");
        }

        #endregion

        #region Climbing Physics Tests

        [Test]
        public void WallStickForce_DefaultValue_IsPositive()
        {
            var field = typeof(EnhancedClimbingSystem).GetField("wallStickForce",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float force = (float)field.GetValue(climbingSystem);

            Assert.AreEqual(10f, force);
            Assert.Greater(force, 0f, "Wall stick force should be positive");
        }

        [Test]
        public void ClimbGravityReduction_IsValidMultiplier()
        {
            var field = typeof(EnhancedClimbingSystem).GetField("climbGravityReduction",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float reduction = (float)field.GetValue(climbingSystem);

            Assert.AreEqual(0.9f, reduction);
            Assert.That(reduction, Is.InRange(0f, 1f),
                "Gravity reduction should be between 0 and 1");
        }

        [Test]
        public void WallJumpForce_IsPositive()
        {
            var field = typeof(EnhancedClimbingSystem).GetField("wallJumpForce",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float force = (float)field.GetValue(climbingSystem);

            Assert.AreEqual(12f, force);
            Assert.Greater(force, 0f, "Wall jump force should be positive");
        }

        [Test]
        public void SurfaceSnapDistance_DefaultValue_IsReasonable()
        {
            var field = typeof(EnhancedClimbingSystem).GetField("surfaceSnapDistance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float distance = (float)field.GetValue(climbingSystem);

            Assert.AreEqual(0.5f, distance);
            Assert.That(distance, Is.InRange(0.1f, 2f),
                "Surface snap distance should be in reasonable range");
        }

        #endregion

        #region Climb Angle Tests

        [Test]
        public void MinClimbAngle_IsValid()
        {
            var field = typeof(EnhancedClimbingSystem).GetField("minClimbAngle",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float angle = (float)field.GetValue(climbingSystem);

            Assert.AreEqual(45f, angle);
            Assert.That(angle, Is.InRange(0f, 90f),
                "Min climb angle should be between 0 and 90 degrees");
        }

        [Test]
        public void MaxClimbAngle_IsValid()
        {
            var field = typeof(EnhancedClimbingSystem).GetField("maxClimbAngle",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float angle = (float)field.GetValue(climbingSystem);

            Assert.AreEqual(85f, angle);
            Assert.That(angle, Is.InRange(0f, 90f),
                "Max climb angle should be between 0 and 90 degrees");
        }

        [Test]
        public void ClimbAngleRange_IsValid()
        {
            var minField = typeof(EnhancedClimbingSystem).GetField("minClimbAngle",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxField = typeof(EnhancedClimbingSystem).GetField("maxClimbAngle",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            float minAngle = (float)minField.GetValue(climbingSystem);
            float maxAngle = (float)maxField.GetValue(climbingSystem);

            Assert.Less(minAngle, maxAngle,
                "Min climb angle should be less than max climb angle");
        }

        [Test]
        public void ClimbAngleRange_CoversVerticalSurfaces()
        {
            var minField = typeof(EnhancedClimbingSystem).GetField("minClimbAngle",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxField = typeof(EnhancedClimbingSystem).GetField("maxClimbAngle",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            float minAngle = (float)minField.GetValue(climbingSystem);
            float maxAngle = (float)maxField.GetValue(climbingSystem);

            Assert.That(maxAngle, Is.GreaterThan(80f),
                "Max angle should allow near-vertical climbing");
            Assert.That(minAngle, Is.LessThan(60f),
                "Min angle should allow moderate slopes");
        }

        #endregion

        #region Surface Detection Tests

        [Test]
        public void SurfaceCheckDistance_DefaultValue_IsPositive()
        {
            var field = typeof(EnhancedClimbingSystem).GetField("surfaceCheckDistance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float distance = (float)field.GetValue(climbingSystem);

            Assert.AreEqual(2f, distance);
            Assert.Greater(distance, 0f, "Surface check distance should be positive");
        }

        [Test]
        public void SurfaceCheckRays_IsReasonable()
        {
            var field = typeof(EnhancedClimbingSystem).GetField("surfaceCheckRays",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            int rays = (int)field.GetValue(climbingSystem);

            Assert.AreEqual(8, rays);
            Assert.That(rays, Is.InRange(4, 16),
                "Surface check rays should be between 4 and 16 for good coverage");
        }

        [Test]
        public void SurfaceCheckDistance_GreaterThanSnapDistance()
        {
            var checkField = typeof(EnhancedClimbingSystem).GetField("surfaceCheckDistance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var snapField = typeof(EnhancedClimbingSystem).GetField("surfaceSnapDistance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            float checkDistance = (float)checkField.GetValue(climbingSystem);
            float snapDistance = (float)snapField.GetValue(climbingSystem);

            Assert.Greater(checkDistance, snapDistance,
                "Surface check distance should be greater than snap distance");
        }

        #endregion

        #region Input Configuration Tests

        [Test]
        public void RequireHoldToClimb_DefaultValue_IsTrue()
        {
            var field = typeof(EnhancedClimbingSystem).GetField("requireHoldToClimb",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool requireHold = (bool)field.GetValue(climbingSystem);

            Assert.IsTrue(requireHold, "Require hold to climb should be true by default");
        }

        [Test]
        public void InputSensitivity_DefaultValue_IsNeutral()
        {
            var field = typeof(EnhancedClimbingSystem).GetField("inputSensitivity",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float sensitivity = (float)field.GetValue(climbingSystem);

            Assert.AreEqual(1f, sensitivity);
            Assert.Greater(sensitivity, 0f, "Input sensitivity should be positive");
        }

        #endregion

        #region Component Dependency Tests

        [Test]
        public void ClimbingSystem_RequiresAdvancedPlayerController()
        {
            Assert.IsNotNull(playerController,
                "EnhancedClimbingSystem requires AdvancedPlayerController");
        }

        [Test]
        public void ClimbingSystem_RequiresCharacterController()
        {
            Assert.IsNotNull(characterController,
                "EnhancedClimbingSystem requires CharacterController");
        }

        #endregion

        #region Performance Optimization Tests

        [Test]
        public void DetectionInterval_IsReasonable()
        {
            var field = typeof(EnhancedClimbingSystem).GetField("detectionInterval",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            float interval = (float)field.GetValue(null);

            Assert.AreEqual(0.1f, interval);
            Assert.That(interval, Is.InRange(0.05f, 0.2f),
                "Detection interval should be optimized (50-200ms)");
        }

        [Test]
        public void WallJumpCooldown_IsShort()
        {
            var field = typeof(EnhancedClimbingSystem).GetField("wallJumpCooldown",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float cooldown = (float)field.GetValue(climbingSystem);

            Assert.AreEqual(0.5f, cooldown);
            Assert.That(cooldown, Is.InRange(0.2f, 1f),
                "Wall jump cooldown should be short (200-1000ms)");
        }

        #endregion
    }
}
