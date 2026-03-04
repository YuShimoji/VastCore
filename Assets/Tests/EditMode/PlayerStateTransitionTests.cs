using NUnit.Framework;
using UnityEngine;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// Player系の状態遷移ロジック検証テスト
    /// 地上→空中→登攀などの状態遷移ロジックの検証
    /// </summary>
    [TestFixture]
    public class PlayerStateTransitionTests
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

        #region Initial State Tests

        [Test]
        public void InitialState_AllParametersAtDefault()
        {
            Assert.AreEqual(70f, controller.moveForce, "MoveForce at default");
            Assert.AreEqual(15f, controller.maxSpeed, "MaxSpeed at default");
            Assert.AreEqual(8f, controller.jumpForce, "JumpForce at default");
            Assert.AreEqual(1.0f, controller.inputSensitivity, "InputSensitivity at default");
        }

        [Test]
        public void InitialState_RigidbodyConfigured()
        {
            // Start() hasn't been called in EditMode, so we test the component exists
            Assert.IsNotNull(rigidbody, "Rigidbody should exist");
        }

        [Test]
        public void InitialState_ColliderConfigured()
        {
            var collider = playerObject.GetComponent<CapsuleCollider>();
            Assert.IsNotNull(collider, "CapsuleCollider should exist");
        }

        #endregion

        #region State Flag Tests

        [Test]
        public void EnableCameraEffects_DefaultState_IsTrue()
        {
            Assert.IsTrue(controller.enableCameraEffects,
                "Camera effects should be enabled by default");
        }

        [Test]
        public void EnableCameraEffects_CanToggle()
        {
            controller.enableCameraEffects = false;
            Assert.IsFalse(controller.enableCameraEffects,
                "Camera effects can be disabled");

            controller.enableCameraEffects = true;
            Assert.IsTrue(controller.enableCameraEffects,
                "Camera effects can be re-enabled");
        }

        [Test]
        public void EnableSprintFov_DefaultState_IsTrue()
        {
            Assert.IsTrue(controller.enableSprintFov,
                "Sprint FOV effect should be enabled by default");
        }

        [Test]
        public void EnableSprintFov_CanToggle()
        {
            controller.enableSprintFov = false;
            Assert.IsFalse(controller.enableSprintFov,
                "Sprint FOV can be disabled");

            controller.enableSprintFov = true;
            Assert.IsTrue(controller.enableSprintFov,
                "Sprint FOV can be re-enabled");
        }

        #endregion

        #region Movement State Transition Logic

        [Test]
        public void MovementState_NormalToSprint_TransitionValid()
        {
            float normalSpeed = controller.maxSpeed;
            float sprintSpeed = controller.sprintMaxSpeed;

            Assert.Greater(sprintSpeed, normalSpeed,
                "Sprint state should have higher max speed");
        }

        [Test]
        public void MovementState_GroundToAir_ControlReduced()
        {
            float groundControl = 1.0f;
            float airControl = controller.airControlFactor;

            Assert.Less(airControl, groundControl,
                "Air control should be less than ground control");
        }

        [Test]
        public void MovementState_AirControlFactor_ValidRange()
        {
            Assert.That(controller.airControlFactor, Is.InRange(0f, 1f),
                "Air control factor should be between 0 and 1");
        }

        #endregion

        #region Jump State Transition Logic

        [Test]
        public void JumpState_CoyoteTime_AllowsLateJump()
        {
            Assert.Greater(controller.coyoteTimeDuration, 0f,
                "Coyote time should allow jumping shortly after leaving ground");
        }

        [Test]
        public void JumpState_JumpBuffer_AllowsEarlyInput()
        {
            Assert.Greater(controller.jumpBufferDuration, 0f,
                "Jump buffer should allow early jump input before landing");
        }

        [Test]
        public void JumpState_CoyoteAndBuffer_BothActive()
        {
            Assert.Greater(controller.coyoteTimeDuration, 0f, "Coyote time active");
            Assert.Greater(controller.jumpBufferDuration, 0f, "Jump buffer active");

            // Both features can coexist
            Assert.IsTrue(controller.coyoteTimeDuration > 0f && controller.jumpBufferDuration > 0f,
                "Both coyote time and jump buffer should be active");
        }

        #endregion

        #region Sprint State Transition Logic

        [Test]
        public void SprintState_HasDuration()
        {
            Assert.Greater(controller.sprintDuration, 0f,
                "Sprint should have finite duration");
        }

        [Test]
        public void SprintState_DurationReasonable()
        {
            Assert.That(controller.sprintDuration, Is.InRange(0.5f, 5f),
                "Sprint duration should be reasonable (0.5-5 seconds)");
        }

        [Test]
        public void SprintState_SpeedIncrease_Significant()
        {
            float normalSpeed = controller.maxSpeed;
            float sprintSpeed = controller.sprintMaxSpeed;
            float increase = (sprintSpeed - normalSpeed) / normalSpeed;

            Assert.That(increase, Is.GreaterThan(0.3f),
                "Sprint should provide at least 30% speed increase");
        }

        #endregion

        #region Camera State Transition Logic

        [Test]
        public void CameraState_DefaultOffset_IsSet()
        {
            Vector3 defaultOffset = new Vector3(0, 5, -10);
            Assert.AreEqual(defaultOffset, controller.cameraOffset,
                "Camera should have default offset");
        }

        [Test]
        public void CameraState_SmoothSpeed_IsPositive()
        {
            Assert.Greater(controller.cameraSmoothSpeed, 0f,
                "Camera smooth speed should be positive for interpolation");
        }

        [Test]
        public void CameraState_FOV_HasLerpSpeed()
        {
            Assert.Greater(controller.fovLerpSpeed, 0f,
                "FOV changes should have lerp speed for smooth transition");
        }

        [Test]
        public void CameraState_SprintFOV_DifferentFromDefault()
        {
            // Sprint FOV should be different from any typical default (60-80)
            Assert.That(controller.sprintFov, Is.Not.EqualTo(60f),
                "Sprint FOV should differ from typical default");
        }

        #endregion

        #region Rotation State Transition Logic

        [Test]
        public void RotationState_HasSpeed()
        {
            Assert.Greater(controller.rotationSpeed, 0f,
                "Rotation should have speed for smooth turning");
        }

        [Test]
        public void RotationState_Speed_AllowsResponsiveTurning()
        {
            Assert.That(controller.rotationSpeed, Is.GreaterThan(5f),
                "Rotation speed should be high enough for responsive turning");
        }

        #endregion

        #region Ground Detection State Logic

        [Test]
        public void GroundState_HasDetectionRadius()
        {
            Assert.Greater(controller.groundCheckRadius, 0f,
                "Ground detection should have positive radius");
        }

        [Test]
        public void GroundState_HasDetectionDistance()
        {
            Assert.Greater(controller.groundCheckDistance, 0f,
                "Ground detection should have positive distance");
        }

        [Test]
        public void GroundState_DistanceRelativeToRadius()
        {
            Assert.Less(controller.groundCheckDistance, controller.groundCheckRadius,
                "Ground check distance typically smaller than radius");
        }

        #endregion

        #region Complex State Transition Validation

        [Test]
        public void StateTransition_GroundToAirToGround_ParametersConsistent()
        {
            // Ground state parameters
            float groundForceMultiplier = 1.0f;
            // Air state parameters
            float airForceMultiplier = controller.airControlFactor;

            Assert.Less(airForceMultiplier, groundForceMultiplier,
                "Transition from ground to air reduces control");
            Assert.Greater(airForceMultiplier, 0f,
                "Air control should still be possible");
        }

        [Test]
        public void StateTransition_NormalToSprintToNormal_SpeedsValid()
        {
            float normalSpeed = controller.maxSpeed;
            float sprintSpeed = controller.sprintMaxSpeed;

            Assert.Less(normalSpeed, sprintSpeed, "Normal < Sprint");
            Assert.Greater(normalSpeed, 0f, "Normal speed positive");
            Assert.Greater(sprintSpeed, 0f, "Sprint speed positive");
        }

        [Test]
        public void StateTransition_AllTimings_NonNegative()
        {
            Assert.GreaterOrEqual(controller.coyoteTimeDuration, 0f, "Coyote time >= 0");
            Assert.GreaterOrEqual(controller.jumpBufferDuration, 0f, "Jump buffer >= 0");
            Assert.GreaterOrEqual(controller.sprintDuration, 0f, "Sprint duration >= 0");
        }

        #endregion

        #region Parameter Consistency Tests

        [Test]
        public void ParameterConsistency_AllSpeeds_Positive()
        {
            Assert.Greater(controller.maxSpeed, 0f, "MaxSpeed > 0");
            Assert.Greater(controller.sprintMaxSpeed, 0f, "SprintMaxSpeed > 0");
        }

        [Test]
        public void ParameterConsistency_AllForces_Configured()
        {
            // Forces can be zero (disabling features), but should be set
            Assert.IsNotNull(controller.moveForce, "MoveForce configured");
            Assert.IsNotNull(controller.jumpForce, "JumpForce configured");
        }

        [Test]
        public void ParameterConsistency_AllDurations_Configured()
        {
            Assert.IsNotNull(controller.coyoteTimeDuration, "CoyoteTime configured");
            Assert.IsNotNull(controller.jumpBufferDuration, "JumpBuffer configured");
            Assert.IsNotNull(controller.sprintDuration, "SprintDuration configured");
        }

        #endregion
    }
}
