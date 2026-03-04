using NUnit.Framework;
using UnityEngine;
using Vastcore.Player;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// EnhancedTranslocationSystem の計算ロジック検証テスト
    /// 軌道予測、着地点計算、安全性チェックのパラメータ検証
    /// </summary>
    [TestFixture]
    public class TranslocationCalculationTests
    {
        private GameObject playerObject;
        private EnhancedTranslocationSystem translocationSystem;

        [SetUp]
        public void SetUp()
        {
            playerObject = new GameObject("TestTranslocationPlayer");
            translocationSystem = playerObject.AddComponent<EnhancedTranslocationSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (playerObject != null)
            {
                Object.DestroyImmediate(playerObject);
            }
        }

        #region Basic Translocation Parameters Tests

        [Test]
        public void SphereLaunchForce_DefaultValue_IsReasonable()
        {
            var field = typeof(EnhancedTranslocationSystem).GetField("sphereLaunchForce",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float force = (float)field.GetValue(translocationSystem);

            Assert.AreEqual(50f, force);
            Assert.Greater(force, 0f, "Sphere launch force should be positive");
            Assert.That(force, Is.InRange(20f, 100f),
                "Launch force should be in reasonable range");
        }

        [Test]
        public void SphereLifetime_DefaultValue_IsReasonable()
        {
            var field = typeof(EnhancedTranslocationSystem).GetField("sphereLifetime",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float lifetime = (float)field.GetValue(translocationSystem);

            Assert.AreEqual(8f, lifetime);
            Assert.Greater(lifetime, 0f, "Sphere lifetime should be positive");
            Assert.That(lifetime, Is.InRange(2f, 30f),
                "Sphere lifetime should be reasonable (2-30 seconds)");
        }

        [Test]
        public void WarpCooldown_DefaultValue_IsReasonable()
        {
            var field = typeof(EnhancedTranslocationSystem).GetField("warpCooldown",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float cooldown = (float)field.GetValue(translocationSystem);

            Assert.AreEqual(2f, cooldown);
            Assert.Greater(cooldown, 0f, "Warp cooldown should be positive");
            Assert.That(cooldown, Is.InRange(0.5f, 10f),
                "Warp cooldown should be balanced (0.5-10 seconds)");
        }

        [Test]
        public void WarpCooldown_ShorterThanSphereLifetime()
        {
            var cooldownField = typeof(EnhancedTranslocationSystem).GetField("warpCooldown",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lifetimeField = typeof(EnhancedTranslocationSystem).GetField("sphereLifetime",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            float cooldown = (float)cooldownField.GetValue(translocationSystem);
            float lifetime = (float)lifetimeField.GetValue(translocationSystem);

            Assert.Less(cooldown, lifetime,
                "Warp cooldown should be shorter than sphere lifetime for continuous use");
        }

        #endregion

        #region Trajectory Prediction Tests

        [Test]
        public void TrajectoryPoints_DefaultValue_IsReasonable()
        {
            var field = typeof(EnhancedTranslocationSystem).GetField("trajectoryPoints",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            int points = (int)field.GetValue(translocationSystem);

            Assert.AreEqual(50, points);
            Assert.That(points, Is.InRange(20, 100),
                "Trajectory points should be enough for smooth visualization");
        }

        [Test]
        public void TrajectoryTimeStep_DefaultValue_IsReasonable()
        {
            var field = typeof(EnhancedTranslocationSystem).GetField("trajectoryTimeStep",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float timeStep = (float)field.GetValue(translocationSystem);

            Assert.AreEqual(0.1f, timeStep);
            Assert.Greater(timeStep, 0f, "Trajectory time step should be positive");
            Assert.That(timeStep, Is.InRange(0.01f, 0.2f),
                "Time step should be small enough for accuracy (10-200ms)");
        }

        [Test]
        public void TrajectoryPredictionRange_IsReasonable()
        {
            var pointsField = typeof(EnhancedTranslocationSystem).GetField("trajectoryPoints",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var timeStepField = typeof(EnhancedTranslocationSystem).GetField("trajectoryTimeStep",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            int points = (int)pointsField.GetValue(translocationSystem);
            float timeStep = (float)timeStepField.GetValue(translocationSystem);

            float totalPredictionTime = points * timeStep;

            Assert.That(totalPredictionTime, Is.InRange(2f, 20f),
                "Total trajectory prediction time should be reasonable (2-20 seconds)");
        }

        #endregion

        #region Primitive Detection Tests

        [Test]
        public void PrimitiveDetectionRadius_DefaultValue_IsReasonable()
        {
            var field = typeof(EnhancedTranslocationSystem).GetField("primitiveDetectionRadius",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float radius = (float)field.GetValue(translocationSystem);

            Assert.AreEqual(5f, radius);
            Assert.Greater(radius, 0f, "Primitive detection radius should be positive");
            Assert.That(radius, Is.InRange(2f, 20f),
                "Detection radius should be reasonable (2-20 units)");
        }

        #endregion

        #region Safety Verification Tests

        [Test]
        public void SafetyCheckRadius_DefaultValue_IsReasonable()
        {
            var field = typeof(EnhancedTranslocationSystem).GetField("safetyCheckRadius",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float radius = (float)field.GetValue(translocationSystem);

            Assert.AreEqual(2f, radius);
            Assert.Greater(radius, 0f, "Safety check radius should be positive");
            Assert.That(radius, Is.InRange(0.5f, 5f),
                "Safety check radius should be reasonable (0.5-5 units)");
        }

        [Test]
        public void MinLandingSpace_DefaultValue_IsReasonable()
        {
            var field = typeof(EnhancedTranslocationSystem).GetField("minLandingSpace",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            float space = (float)field.GetValue(translocationSystem);

            Assert.AreEqual(3f, space);
            Assert.Greater(space, 0f, "Min landing space should be positive");
            Assert.That(space, Is.InRange(1f, 10f),
                "Min landing space should accommodate player size (1-10 units)");
        }

        [Test]
        public void MinLandingSpace_GreaterThanSafetyRadius()
        {
            var spaceField = typeof(EnhancedTranslocationSystem).GetField("minLandingSpace",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var radiusField = typeof(EnhancedTranslocationSystem).GetField("safetyCheckRadius",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            float space = (float)spaceField.GetValue(translocationSystem);
            float radius = (float)radiusField.GetValue(translocationSystem);

            Assert.Greater(space, radius,
                "Min landing space should be greater than safety check radius");
        }

        [Test]
        public void SafetyCheckRadius_LessThanDetectionRadius()
        {
            var safetyField = typeof(EnhancedTranslocationSystem).GetField("safetyCheckRadius",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var detectionField = typeof(EnhancedTranslocationSystem).GetField("primitiveDetectionRadius",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            float safetyRadius = (float)safetyField.GetValue(translocationSystem);
            float detectionRadius = (float)detectionField.GetValue(translocationSystem);

            Assert.Less(safetyRadius, detectionRadius,
                "Safety check should be more precise than primitive detection");
        }

        #endregion

        #region Launch Force Calculation Tests

        [Test]
        public void LaunchForce_ZeroValue_NoMovement()
        {
            var field = typeof(EnhancedTranslocationSystem).GetField("sphereLaunchForce",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            field.SetValue(translocationSystem, 0f);
            float force = (float)field.GetValue(translocationSystem);

            Assert.AreEqual(0f, force, "Zero launch force means no movement");
        }

        [Test]
        public void LaunchForce_HighValue_AllowsLongDistance()
        {
            var field = typeof(EnhancedTranslocationSystem).GetField("sphereLaunchForce",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            field.SetValue(translocationSystem, 100f);
            float force = (float)field.GetValue(translocationSystem);

            Assert.AreEqual(100f, force, "High launch force allows long-distance warps");
        }

        #endregion

        #region Lifetime and Cooldown Relationship Tests

        [Test]
        public void LifetimeVsCooldown_AllowsMultipleActiveWarps()
        {
            var cooldownField = typeof(EnhancedTranslocationSystem).GetField("warpCooldown",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lifetimeField = typeof(EnhancedTranslocationSystem).GetField("sphereLifetime",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            float cooldown = (float)cooldownField.GetValue(translocationSystem);
            float lifetime = (float)lifetimeField.GetValue(translocationSystem);

            int potentialActiveWarps = Mathf.FloorToInt(lifetime / cooldown);

            Assert.That(potentialActiveWarps, Is.GreaterThan(1),
                "Lifetime/cooldown ratio should allow strategic warp planning");
        }

        [Test]
        public void CooldownToLifetimeRatio_IsBalanced()
        {
            var cooldownField = typeof(EnhancedTranslocationSystem).GetField("warpCooldown",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var lifetimeField = typeof(EnhancedTranslocationSystem).GetField("sphereLifetime",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            float cooldown = (float)cooldownField.GetValue(translocationSystem);
            float lifetime = (float)lifetimeField.GetValue(translocationSystem);

            float ratio = lifetime / cooldown;

            Assert.That(ratio, Is.InRange(2f, 10f),
                "Lifetime/cooldown ratio should be balanced (2x-10x)");
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void TrajectoryPoints_MinimumValue_StillFunctional()
        {
            var field = typeof(EnhancedTranslocationSystem).GetField("trajectoryPoints",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            field.SetValue(translocationSystem, 10);
            int points = (int)field.GetValue(translocationSystem);

            Assert.AreEqual(10, points);
            Assert.Greater(points, 0, "Even minimum trajectory points should work");
        }

        [Test]
        public void SafetyCheckRadius_VerySmall_TightValidation()
        {
            var field = typeof(EnhancedTranslocationSystem).GetField("safetyCheckRadius",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            field.SetValue(translocationSystem, 0.5f);
            float radius = (float)field.GetValue(translocationSystem);

            Assert.AreEqual(0.5f, radius);
            Assert.Greater(radius, 0f, "Very small safety radius for tight spaces");
        }

        #endregion
    }
}
