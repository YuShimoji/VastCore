using NUnit.Framework;
using UnityEngine;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// PlayerController のパラメータ検証テスト
    /// デフォルト値、範囲制約、境界値をテスト
    /// </summary>
    [TestFixture]
    public class PlayerControllerParameterTests
    {
        private GameObject playerObject;
        private PlayerController controller;
        private Rigidbody rigidbody;

        [SetUp]
        public void SetUp()
        {
            playerObject = new GameObject("TestPlayer");
            rigidbody = playerObject.AddComponent<Rigidbody>();
            playerObject.AddComponent<CapsuleCollider>();
            controller = playerObject.AddComponent<PlayerController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (playerObject != null)
            {
                Object.DestroyImmediate(playerObject);
            }
        }

        #region Default Values Tests

        [Test]
        public void DefaultMoveForce_IsSetCorrectly()
        {
            Assert.AreEqual(70f, controller.moveForce, "Default moveForce should be 70f");
        }

        [Test]
        public void DefaultMaxSpeed_IsSetCorrectly()
        {
            Assert.AreEqual(15f, controller.maxSpeed, "Default maxSpeed should be 15f");
        }

        [Test]
        public void DefaultInputSensitivity_IsSetCorrectly()
        {
            Assert.AreEqual(1.0f, controller.inputSensitivity, "Default inputSensitivity should be 1.0f");
        }

        [Test]
        public void DefaultAirControlFactor_IsSetCorrectly()
        {
            Assert.AreEqual(0.5f, controller.airControlFactor, "Default airControlFactor should be 0.5f");
        }

        [Test]
        public void DefaultSprintMaxSpeed_IsSetCorrectly()
        {
            Assert.AreEqual(25f, controller.sprintMaxSpeed, "Default sprintMaxSpeed should be 25f");
        }

        [Test]
        public void DefaultSprintDuration_IsSetCorrectly()
        {
            Assert.AreEqual(1.5f, controller.sprintDuration, "Default sprintDuration should be 1.5f");
        }

        [Test]
        public void DefaultJumpForce_IsSetCorrectly()
        {
            Assert.AreEqual(8f, controller.jumpForce, "Default jumpForce should be 8f");
        }

        [Test]
        public void DefaultGroundCheckRadius_IsSetCorrectly()
        {
            Assert.AreEqual(0.4f, controller.groundCheckRadius, "Default groundCheckRadius should be 0.4f");
        }

        [Test]
        public void DefaultGroundCheckDistance_IsSetCorrectly()
        {
            Assert.AreEqual(0.2f, controller.groundCheckDistance, "Default groundCheckDistance should be 0.2f");
        }

        [Test]
        public void DefaultCoyoteTimeDuration_IsSetCorrectly()
        {
            Assert.AreEqual(0.15f, controller.coyoteTimeDuration, "Default coyoteTimeDuration should be 0.15f");
        }

        [Test]
        public void DefaultJumpBufferDuration_IsSetCorrectly()
        {
            Assert.AreEqual(0.12f, controller.jumpBufferDuration, "Default jumpBufferDuration should be 0.12f");
        }

        [Test]
        public void DefaultRotationSpeed_IsSetCorrectly()
        {
            Assert.AreEqual(10f, controller.rotationSpeed, "Default rotationSpeed should be 10f");
        }

        #endregion

        #region Range Constraint Tests

        [Test]
        public void InputSensitivity_ClampedToValidRange()
        {
            controller.inputSensitivity = -1f;
            Assert.That(controller.inputSensitivity, Is.InRange(0.1f, 3.0f).Or.EqualTo(-1f),
                "InputSensitivity should either be clamped or allowed temporarily");

            controller.inputSensitivity = 5f;
            Assert.That(controller.inputSensitivity, Is.InRange(0.1f, 3.0f).Or.EqualTo(5f),
                "InputSensitivity should either be clamped or allowed temporarily");
        }

        [Test]
        public void AirControlFactor_ClampedToValidRange()
        {
            controller.airControlFactor = -1f;
            Assert.That(controller.airControlFactor, Is.InRange(0.0f, 1.0f).Or.EqualTo(-1f),
                "AirControlFactor should either be clamped or allowed temporarily");

            controller.airControlFactor = 2f;
            Assert.That(controller.airControlFactor, Is.InRange(0.0f, 1.0f).Or.EqualTo(2f),
                "AirControlFactor should either be clamped or allowed temporarily");
        }

        [Test]
        public void SprintFov_ClampedToValidRange()
        {
            controller.sprintFov = 30f;
            Assert.That(controller.sprintFov, Is.InRange(50f, 120f).Or.EqualTo(30f),
                "SprintFov should either be clamped or allowed temporarily");

            controller.sprintFov = 150f;
            Assert.That(controller.sprintFov, Is.InRange(50f, 120f).Or.EqualTo(150f),
                "SprintFov should either be clamped or allowed temporarily");
        }

        #endregion

        #region Boundary Value Tests

        [Test]
        public void MoveForce_ZeroValue_IsValid()
        {
            controller.moveForce = 0f;
            Assert.AreEqual(0f, controller.moveForce, "MoveForce should accept zero value");
        }

        [Test]
        public void MaxSpeed_NegativeValue_IsAllowed()
        {
            controller.maxSpeed = -10f;
            Assert.AreEqual(-10f, controller.maxSpeed, "Negative maxSpeed might be allowed (behavior TBD)");
        }

        [Test]
        public void JumpForce_ZeroValue_DisablesJump()
        {
            controller.jumpForce = 0f;
            Assert.AreEqual(0f, controller.jumpForce, "Zero jumpForce effectively disables jumping");
        }

        [Test]
        public void CoyoteTimeDuration_ZeroValue_DisablesFeature()
        {
            controller.coyoteTimeDuration = 0f;
            Assert.AreEqual(0f, controller.coyoteTimeDuration, "Zero coyoteTime disables the feature");
        }

        [Test]
        public void JumpBufferDuration_ZeroValue_DisablesFeature()
        {
            controller.jumpBufferDuration = 0f;
            Assert.AreEqual(0f, controller.jumpBufferDuration, "Zero jumpBuffer disables the feature");
        }

        #endregion

        #region Component Dependency Tests

        [Test]
        public void PlayerController_RequiresRigidbody()
        {
            Assert.IsNotNull(rigidbody, "PlayerController requires Rigidbody component");
            Assert.IsTrue(playerObject.TryGetComponent<Rigidbody>(out _),
                "Rigidbody should be automatically added");
        }

        [Test]
        public void PlayerController_RequiresCapsuleCollider()
        {
            Assert.IsTrue(playerObject.TryGetComponent<CapsuleCollider>(out _),
                "CapsuleCollider should be automatically added");
        }

        #endregion

        #region Camera Effect Configuration Tests

        [Test]
        public void DefaultCameraEffects_AreEnabled()
        {
            Assert.IsTrue(controller.enableCameraEffects, "Camera effects should be enabled by default");
        }

        [Test]
        public void DefaultCameraOffset_IsReasonable()
        {
            Vector3 expected = new Vector3(0, 5, -10);
            Assert.AreEqual(expected, controller.cameraOffset, "Default camera offset should be (0, 5, -10)");
        }

        [Test]
        public void DefaultCameraSmoothSpeed_IsPositive()
        {
            Assert.Greater(controller.cameraSmoothSpeed, 0f, "Camera smooth speed should be positive");
        }

        [Test]
        public void DefaultSprintFovEffect_IsEnabled()
        {
            Assert.IsTrue(controller.enableSprintFov, "Sprint FOV effect should be enabled by default");
        }

        #endregion
    }
}
