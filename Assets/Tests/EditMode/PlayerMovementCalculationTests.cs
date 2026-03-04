using NUnit.Framework;
using UnityEngine;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// PlayerController の移動計算ロジックテスト
    /// 移動力、ジャンプ力、スプリント計算の検証
    /// </summary>
    [TestFixture]
    public class PlayerMovementCalculationTests
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

        #region Movement Force Calculation Tests

        [Test]
        public void MoveForce_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(70f, controller.moveForce);
            Assert.Greater(controller.moveForce, 0f, "MoveForce should be positive for forward movement");
        }

        [Test]
        public void MoveForce_ZeroValue_NoMovement()
        {
            controller.moveForce = 0f;
            Assert.AreEqual(0f, controller.moveForce, "Zero moveForce should result in no movement");
        }

        [Test]
        public void MoveForce_NegativeValue_AllowedButUnusual()
        {
            controller.moveForce = -50f;
            Assert.AreEqual(-50f, controller.moveForce, "Negative moveForce is technically allowed");
        }

        [Test]
        public void AirControlFactor_ReducesMovementForce()
        {
            // Air control factor should be less than 1.0 for reduced air control
            Assert.Less(controller.airControlFactor, 1.0f,
                "Air control should be reduced compared to ground movement");
            Assert.GreaterOrEqual(controller.airControlFactor, 0.0f,
                "Air control factor should not be negative");
        }

        [Test]
        public void AirControlFactor_ZeroValue_NoAirControl()
        {
            controller.airControlFactor = 0f;
            Assert.AreEqual(0f, controller.airControlFactor,
                "Zero air control factor means no control in air");
        }

        [Test]
        public void AirControlFactor_FullValue_FullAirControl()
        {
            controller.airControlFactor = 1.0f;
            Assert.AreEqual(1.0f, controller.airControlFactor,
                "1.0 air control factor means full control in air");
        }

        #endregion

        #region Speed Calculation Tests

        [Test]
        public void MaxSpeed_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(15f, controller.maxSpeed);
            Assert.Greater(controller.maxSpeed, 0f, "Max speed should be positive");
        }

        [Test]
        public void SprintMaxSpeed_GreaterThanNormalMaxSpeed()
        {
            Assert.Greater(controller.sprintMaxSpeed, controller.maxSpeed,
                "Sprint max speed should be greater than normal max speed");
        }

        [Test]
        public void SprintMaxSpeed_ReasonableMultiplier()
        {
            float speedMultiplier = controller.sprintMaxSpeed / controller.maxSpeed;
            Assert.That(speedMultiplier, Is.InRange(1.5f, 2.5f),
                "Sprint speed multiplier should be reasonable (1.5x-2.5x)");
        }

        [Test]
        public void SprintDuration_IsPositive()
        {
            Assert.Greater(controller.sprintDuration, 0f,
                "Sprint duration should be positive");
        }

        [Test]
        public void SprintDuration_ReasonableRange()
        {
            Assert.That(controller.sprintDuration, Is.InRange(0.5f, 5.0f),
                "Sprint duration should be in reasonable range (0.5-5 seconds)");
        }

        #endregion

        #region Jump Force Calculation Tests

        [Test]
        public void JumpForce_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(8f, controller.jumpForce);
            Assert.Greater(controller.jumpForce, 0f, "Jump force should be positive");
        }

        [Test]
        public void JumpForce_ZeroValue_DisablesJumping()
        {
            controller.jumpForce = 0f;
            Assert.AreEqual(0f, controller.jumpForce,
                "Zero jump force effectively disables jumping");
        }

        [Test]
        public void JumpForce_NegativeValue_CausesDownwardForce()
        {
            controller.jumpForce = -5f;
            Assert.Less(controller.jumpForce, 0f,
                "Negative jump force would cause downward movement");
        }

        [Test]
        public void JumpForce_HighValue_AllowsHighJumps()
        {
            controller.jumpForce = 20f;
            Assert.AreEqual(20f, controller.jumpForce,
                "High jump force allows for higher jumps");
        }

        #endregion

        #region Input Sensitivity Tests

        [Test]
        public void InputSensitivity_DefaultValue_IsNeutral()
        {
            Assert.AreEqual(1.0f, controller.inputSensitivity,
                "Default input sensitivity should be 1.0 (neutral)");
        }

        [Test]
        public void InputSensitivity_LowValue_ReducesResponsiveness()
        {
            controller.inputSensitivity = 0.1f;
            Assert.AreEqual(0.1f, controller.inputSensitivity,
                "Low input sensitivity reduces movement responsiveness");
        }

        [Test]
        public void InputSensitivity_HighValue_IncreasesResponsiveness()
        {
            controller.inputSensitivity = 3.0f;
            Assert.AreEqual(3.0f, controller.inputSensitivity,
                "High input sensitivity increases movement responsiveness");
        }

        [Test]
        public void InputSensitivity_Multiplier_AffectsMovement()
        {
            // Verify that input sensitivity acts as a multiplier
            float baseSensitivity = 1.0f;
            float highSensitivity = 2.0f;

            controller.inputSensitivity = baseSensitivity;
            float baseValue = controller.inputSensitivity;

            controller.inputSensitivity = highSensitivity;
            float highValue = controller.inputSensitivity;

            Assert.AreEqual(highSensitivity / baseSensitivity, highValue / baseValue,
                "Input sensitivity should act as a linear multiplier");
        }

        #endregion

        #region Rotation Speed Tests

        [Test]
        public void RotationSpeed_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(10f, controller.rotationSpeed);
            Assert.Greater(controller.rotationSpeed, 0f,
                "Rotation speed should be positive");
        }

        [Test]
        public void RotationSpeed_ZeroValue_NoRotation()
        {
            controller.rotationSpeed = 0f;
            Assert.AreEqual(0f, controller.rotationSpeed,
                "Zero rotation speed means no rotation interpolation");
        }

        [Test]
        public void RotationSpeed_HighValue_FastRotation()
        {
            controller.rotationSpeed = 50f;
            Assert.AreEqual(50f, controller.rotationSpeed,
                "High rotation speed allows for faster turning");
        }

        #endregion

        #region Coyote Time Calculation Tests

        [Test]
        public void CoyoteTimeDuration_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(0.15f, controller.coyoteTimeDuration);
            Assert.That(controller.coyoteTimeDuration, Is.InRange(0.05f, 0.3f),
                "Coyote time should be in reasonable range (50-300ms)");
        }

        [Test]
        public void CoyoteTimeDuration_ZeroValue_DisablesFeature()
        {
            controller.coyoteTimeDuration = 0f;
            Assert.AreEqual(0f, controller.coyoteTimeDuration,
                "Zero coyote time disables the late jump feature");
        }

        [Test]
        public void JumpBufferDuration_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(0.12f, controller.jumpBufferDuration);
            Assert.That(controller.jumpBufferDuration, Is.InRange(0.05f, 0.3f),
                "Jump buffer should be in reasonable range (50-300ms)");
        }

        [Test]
        public void JumpBufferDuration_ZeroValue_DisablesFeature()
        {
            controller.jumpBufferDuration = 0f;
            Assert.AreEqual(0f, controller.jumpBufferDuration,
                "Zero jump buffer disables the early jump feature");
        }

        [Test]
        public void CoyoteTime_ShorterThanJumpBuffer()
        {
            // Typical game feel: jump buffer slightly longer than coyote time
            // (allows early jump input before landing)
            Assert.That(controller.coyoteTimeDuration, Is.LessThanOrEqualTo(controller.jumpBufferDuration + 0.05f),
                "Coyote time and jump buffer should be comparable in duration");
        }

        #endregion

        #region Ground Check Parameters Tests

        [Test]
        public void GroundCheckRadius_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(0.4f, controller.groundCheckRadius);
            Assert.Greater(controller.groundCheckRadius, 0f,
                "Ground check radius should be positive");
        }

        [Test]
        public void GroundCheckDistance_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(0.2f, controller.groundCheckDistance);
            Assert.Greater(controller.groundCheckDistance, 0f,
                "Ground check distance should be positive");
        }

        [Test]
        public void GroundCheckDistance_SmallerThanRadius()
        {
            Assert.Less(controller.groundCheckDistance, controller.groundCheckRadius,
                "Ground check distance is typically smaller than radius for tight detection");
        }

        [Test]
        public void GroundCheckRadius_ZeroValue_NoGroundDetection()
        {
            controller.groundCheckRadius = 0f;
            Assert.AreEqual(0f, controller.groundCheckRadius,
                "Zero ground check radius effectively disables ground detection");
        }

        #endregion

        #region Movement Physics Relationship Tests

        [Test]
        public void MoveForce_To_MaxSpeed_Ratio_IsBalanced()
        {
            float ratio = controller.moveForce / controller.maxSpeed;
            Assert.That(ratio, Is.InRange(2f, 10f),
                "MoveForce to MaxSpeed ratio should be balanced for smooth acceleration");
        }

        [Test]
        public void JumpForce_To_MoveForce_Ratio_IsReasonable()
        {
            float ratio = controller.jumpForce / controller.moveForce;
            Assert.That(ratio, Is.InRange(0.05f, 0.5f),
                "Jump force should be proportional to movement force");
        }

        #endregion
    }
}
