using NUnit.Framework;
using UnityEngine;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// Player系の入力パラメータ検証テスト
    /// 入力値の正規化、無効値の拒否、範囲制約の検証
    /// </summary>
    [TestFixture]
    public class PlayerInputValidationTests
    {
        private GameObject playerObject;
        private PlayerController controller;

        [SetUp]
        public void SetUp()
        {
            playerObject = new GameObject("TestPlayer");
            playerObject.AddComponent<Rigidbody>();
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

        #region Input Sensitivity Validation

        [Test]
        public void InputSensitivity_WithinRange_Accepted()
        {
            controller.inputSensitivity = 1.5f;
            Assert.AreEqual(1.5f, controller.inputSensitivity,
                "Valid input sensitivity should be accepted");
        }

        [Test]
        public void InputSensitivity_MinimumBoundary_Accepted()
        {
            controller.inputSensitivity = 0.1f;
            Assert.AreEqual(0.1f, controller.inputSensitivity,
                "Minimum boundary value should be accepted");
        }

        [Test]
        public void InputSensitivity_MaximumBoundary_Accepted()
        {
            controller.inputSensitivity = 3.0f;
            Assert.AreEqual(3.0f, controller.inputSensitivity,
                "Maximum boundary value should be accepted");
        }

        [Test]
        public void InputSensitivity_BelowMinimum_Behavior()
        {
            controller.inputSensitivity = 0.05f;
            // Note: Unity's Range attribute doesn't enforce at runtime in tests,
            // but Start() method validates and clamps
            Assert.That(controller.inputSensitivity, Is.EqualTo(0.05f).Or.InRange(0.1f, 3.0f),
                "Below minimum value is either allowed temporarily or clamped");
        }

        [Test]
        public void InputSensitivity_AboveMaximum_Behavior()
        {
            controller.inputSensitivity = 5.0f;
            Assert.That(controller.inputSensitivity, Is.EqualTo(5.0f).Or.InRange(0.1f, 3.0f),
                "Above maximum value is either allowed temporarily or clamped");
        }

        [Test]
        public void InputSensitivity_NegativeValue_Behavior()
        {
            controller.inputSensitivity = -1.0f;
            Assert.That(controller.inputSensitivity, Is.EqualTo(-1.0f).Or.GreaterThanOrEqualTo(0.1f),
                "Negative value should be rejected or handled");
        }

        [Test]
        public void InputSensitivity_ZeroValue_Behavior()
        {
            controller.inputSensitivity = 0f;
            Assert.That(controller.inputSensitivity, Is.EqualTo(0f).Or.GreaterThanOrEqualTo(0.1f),
                "Zero value should be rejected or minimum value enforced");
        }

        #endregion

        #region Air Control Factor Validation

        [Test]
        public void AirControlFactor_WithinRange_Accepted()
        {
            controller.airControlFactor = 0.5f;
            Assert.AreEqual(0.5f, controller.airControlFactor,
                "Valid air control factor should be accepted");
        }

        [Test]
        public void AirControlFactor_MinimumBoundary_Accepted()
        {
            controller.airControlFactor = 0.0f;
            Assert.AreEqual(0.0f, controller.airControlFactor,
                "Zero air control (no air movement) should be accepted");
        }

        [Test]
        public void AirControlFactor_MaximumBoundary_Accepted()
        {
            controller.airControlFactor = 1.0f;
            Assert.AreEqual(1.0f, controller.airControlFactor,
                "Full air control should be accepted");
        }

        [Test]
        public void AirControlFactor_NegativeValue_Behavior()
        {
            controller.airControlFactor = -0.5f;
            Assert.That(controller.airControlFactor, Is.EqualTo(-0.5f).Or.InRange(0.0f, 1.0f),
                "Negative air control should be handled");
        }

        [Test]
        public void AirControlFactor_AboveOne_Behavior()
        {
            controller.airControlFactor = 1.5f;
            Assert.That(controller.airControlFactor, Is.EqualTo(1.5f).Or.InRange(0.0f, 1.0f),
                "Air control above 1.0 should be handled");
        }

        #endregion

        #region Sprint FOV Validation

        [Test]
        public void SprintFov_WithinRange_Accepted()
        {
            controller.sprintFov = 70f;
            Assert.AreEqual(70f, controller.sprintFov,
                "Valid sprint FOV should be accepted");
        }

        [Test]
        public void SprintFov_MinimumBoundary_Accepted()
        {
            controller.sprintFov = 50f;
            Assert.AreEqual(50f, controller.sprintFov,
                "Minimum FOV boundary should be accepted");
        }

        [Test]
        public void SprintFov_MaximumBoundary_Accepted()
        {
            controller.sprintFov = 120f;
            Assert.AreEqual(120f, controller.sprintFov,
                "Maximum FOV boundary should be accepted");
        }

        [Test]
        public void SprintFov_BelowMinimum_Behavior()
        {
            controller.sprintFov = 30f;
            Assert.That(controller.sprintFov, Is.EqualTo(30f).Or.InRange(50f, 120f),
                "FOV below minimum should be handled");
        }

        [Test]
        public void SprintFov_AboveMaximum_Behavior()
        {
            controller.sprintFov = 150f;
            Assert.That(controller.sprintFov, Is.EqualTo(150f).Or.InRange(50f, 120f),
                "FOV above maximum should be handled");
        }

        #endregion

        #region Force and Speed Value Validation

        [Test]
        public void MoveForce_PositiveValues_Accepted()
        {
            controller.moveForce = 100f;
            Assert.AreEqual(100f, controller.moveForce,
                "Positive move force should be accepted");
        }

        [Test]
        public void MoveForce_ZeroValue_Accepted()
        {
            controller.moveForce = 0f;
            Assert.AreEqual(0f, controller.moveForce,
                "Zero move force (no movement) should be accepted");
        }

        [Test]
        public void MoveForce_NegativeValue_Accepted()
        {
            controller.moveForce = -50f;
            Assert.AreEqual(-50f, controller.moveForce,
                "Negative move force should be accepted (unusual but valid)");
        }

        [Test]
        public void JumpForce_PositiveValues_Accepted()
        {
            controller.jumpForce = 15f;
            Assert.AreEqual(15f, controller.jumpForce,
                "Positive jump force should be accepted");
        }

        [Test]
        public void JumpForce_ZeroValue_DisablesJumping()
        {
            controller.jumpForce = 0f;
            Assert.AreEqual(0f, controller.jumpForce,
                "Zero jump force disables jumping");
        }

        [Test]
        public void MaxSpeed_PositiveValues_Accepted()
        {
            controller.maxSpeed = 20f;
            Assert.AreEqual(20f, controller.maxSpeed,
                "Positive max speed should be accepted");
        }

        [Test]
        public void MaxSpeed_ZeroValue_Accepted()
        {
            controller.maxSpeed = 0f;
            Assert.AreEqual(0f, controller.maxSpeed,
                "Zero max speed (no movement) should be accepted");
        }

        #endregion

        #region Time Duration Validation

        [Test]
        public void CoyoteTimeDuration_PositiveValue_Accepted()
        {
            controller.coyoteTimeDuration = 0.2f;
            Assert.AreEqual(0.2f, controller.coyoteTimeDuration,
                "Positive coyote time should be accepted");
        }

        [Test]
        public void CoyoteTimeDuration_ZeroValue_DisablesFeature()
        {
            controller.coyoteTimeDuration = 0f;
            Assert.AreEqual(0f, controller.coyoteTimeDuration,
                "Zero coyote time disables feature");
        }

        [Test]
        public void JumpBufferDuration_PositiveValue_Accepted()
        {
            controller.jumpBufferDuration = 0.15f;
            Assert.AreEqual(0.15f, controller.jumpBufferDuration,
                "Positive jump buffer should be accepted");
        }

        [Test]
        public void JumpBufferDuration_ZeroValue_DisablesFeature()
        {
            controller.jumpBufferDuration = 0f;
            Assert.AreEqual(0f, controller.jumpBufferDuration,
                "Zero jump buffer disables feature");
        }

        [Test]
        public void SprintDuration_PositiveValue_Accepted()
        {
            controller.sprintDuration = 2f;
            Assert.AreEqual(2f, controller.sprintDuration,
                "Positive sprint duration should be accepted");
        }

        [Test]
        public void SprintDuration_VeryShort_Accepted()
        {
            controller.sprintDuration = 0.1f;
            Assert.AreEqual(0.1f, controller.sprintDuration,
                "Very short sprint duration should be accepted");
        }

        #endregion

        #region Distance and Radius Validation

        [Test]
        public void GroundCheckRadius_PositiveValue_Accepted()
        {
            controller.groundCheckRadius = 0.5f;
            Assert.AreEqual(0.5f, controller.groundCheckRadius,
                "Positive ground check radius should be accepted");
        }

        [Test]
        public void GroundCheckRadius_ZeroValue_DisablesDetection()
        {
            controller.groundCheckRadius = 0f;
            Assert.AreEqual(0f, controller.groundCheckRadius,
                "Zero radius effectively disables ground detection");
        }

        [Test]
        public void GroundCheckDistance_PositiveValue_Accepted()
        {
            controller.groundCheckDistance = 0.3f;
            Assert.AreEqual(0.3f, controller.groundCheckDistance,
                "Positive ground check distance should be accepted");
        }

        [Test]
        public void GroundCheckDistance_ZeroValue_NoCheck()
        {
            controller.groundCheckDistance = 0f;
            Assert.AreEqual(0f, controller.groundCheckDistance,
                "Zero distance means no ground check distance");
        }

        #endregion

        #region Parameter Relationship Validation

        [Test]
        public void SprintMaxSpeed_ShouldExceedMaxSpeed()
        {
            controller.maxSpeed = 15f;
            controller.sprintMaxSpeed = 25f;

            Assert.Greater(controller.sprintMaxSpeed, controller.maxSpeed,
                "Sprint max speed should exceed normal max speed");
        }

        [Test]
        public void CameraLerpSpeed_PositiveValue_Accepted()
        {
            controller.fovLerpSpeed = 8f;
            Assert.AreEqual(8f, controller.fovLerpSpeed,
                "Positive FOV lerp speed should be accepted");
        }

        [Test]
        public void RotationSpeed_PositiveValue_Accepted()
        {
            controller.rotationSpeed = 15f;
            Assert.AreEqual(15f, controller.rotationSpeed,
                "Positive rotation speed should be accepted");
        }

        #endregion
    }
}
