using NUnit.Framework;
using UnityEngine;
using Vastcore.Player;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// AdvancedPlayerController の高度機能パラメータ検証テスト
    /// グライド、ドリームフライト、グラインド、慣性システムのパラメータ検証
    /// </summary>
    [TestFixture]
    public class AdvancedPlayerParameterTests
    {
        private GameObject playerObject;
        private AdvancedPlayerController controller;
        private CharacterController characterController;

        [SetUp]
        public void SetUp()
        {
            playerObject = new GameObject("TestAdvancedPlayer");
            characterController = playerObject.AddComponent<CharacterController>();
            controller = playerObject.AddComponent<AdvancedPlayerController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (playerObject != null)
            {
                Object.DestroyImmediate(playerObject);
            }
        }

        #region Basic Movement Parameters

        [Test]
        public void MoveSpeed_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(10f, controller.moveSpeed);
            Assert.Greater(controller.moveSpeed, 0f, "Move speed should be positive");
        }

        [Test]
        public void Gravity_DefaultValue_IsNegative()
        {
            Assert.AreEqual(-9.81f, controller.gravity);
            Assert.Less(controller.gravity, 0f, "Gravity should be negative (downward)");
        }

        [Test]
        public void JumpHeight_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(8f, controller.jumpHeight);
            Assert.Greater(controller.jumpHeight, 0f, "Jump height should be positive");
        }

        #endregion

        #region Momentum System Tests

        [Test]
        public void MomentumDamping_DefaultValue_IsValid()
        {
            Assert.AreEqual(0.8f, controller.momentumDamping);
            Assert.That(controller.momentumDamping, Is.InRange(0f, 1f),
                "Momentum damping should be between 0 and 1");
        }

        [Test]
        public void MomentumDamping_ZeroValue_NoDecay()
        {
            controller.momentumDamping = 0f;
            Assert.AreEqual(0f, controller.momentumDamping,
                "Zero damping means momentum never decays");
        }

        [Test]
        public void MomentumDamping_OneValue_ImmediateStop()
        {
            controller.momentumDamping = 1f;
            Assert.AreEqual(1f, controller.momentumDamping,
                "1.0 damping means immediate stop (no momentum)");
        }

        [Test]
        public void AccelerationRate_DefaultValue_IsPositive()
        {
            Assert.AreEqual(10f, controller.accelerationRate);
            Assert.Greater(controller.accelerationRate, 0f,
                "Acceleration rate should be positive");
        }

        [Test]
        public void MaxMomentumSpeed_GreaterThanBaseSpeed()
        {
            Assert.Greater(controller.maxMomentumSpeed, controller.moveSpeed,
                "Max momentum speed should be greater than base move speed");
        }

        [Test]
        public void MaxMomentumSpeed_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(25f, controller.maxMomentumSpeed);
            Assert.That(controller.maxMomentumSpeed, Is.InRange(15f, 50f),
                "Max momentum speed should be in reasonable range");
        }

        #endregion

        #region Gliding System Tests

        [Test]
        public void GlideGravity_LessThanNormalGravity()
        {
            Assert.Less(Mathf.Abs(controller.glideGravity), Mathf.Abs(controller.gravity),
                "Glide gravity should be weaker than normal gravity");
        }

        [Test]
        public void GlideGravity_DefaultValue_IsNegative()
        {
            Assert.AreEqual(-2f, controller.glideGravity);
            Assert.Less(controller.glideGravity, 0f,
                "Glide gravity should still be negative (falling, but slower)");
        }

        [Test]
        public void GlideForwardForce_DefaultValue_IsPositive()
        {
            Assert.AreEqual(5f, controller.glideForwardForce);
            Assert.Greater(controller.glideForwardForce, 0f,
                "Glide forward force should be positive");
        }

        [Test]
        public void GlideMaxSpeed_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(25f, controller.glideMaxSpeed);
            Assert.Greater(controller.glideMaxSpeed, controller.moveSpeed,
                "Glide max speed should be greater than base move speed");
        }

        [Test]
        public void GlideMaxSpeed_EqualToMomentumSpeed()
        {
            Assert.AreEqual(controller.maxMomentumSpeed, controller.glideMaxSpeed,
                "Glide and momentum speeds are typically the same for consistency");
        }

        #endregion

        #region Dream Flight System Tests

        [Test]
        public void DreamFlightBaseSpeed_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(15f, controller.dreamFlightBaseSpeed);
            Assert.Greater(controller.dreamFlightBaseSpeed, controller.moveSpeed,
                "Dream flight base speed should be faster than normal movement");
        }

        [Test]
        public void DreamFlightMaxSpeed_MuchGreaterThanBase()
        {
            Assert.AreEqual(80f, controller.dreamFlightMaxSpeed);
            Assert.Greater(controller.dreamFlightMaxSpeed, controller.dreamFlightBaseSpeed * 2,
                "Dream flight max speed should be significantly faster than base");
        }

        [Test]
        public void DreamFlightAcceleration_DefaultValue_IsPositive()
        {
            Assert.AreEqual(3f, controller.dreamFlightAcceleration);
            Assert.Greater(controller.dreamFlightAcceleration, 0f,
                "Dream flight acceleration should be positive");
        }

        [Test]
        public void DreamFlightEnergy_DefaultValue_IsPositive()
        {
            Assert.AreEqual(100f, controller.dreamFlightEnergy);
            Assert.Greater(controller.dreamFlightEnergy, 0f,
                "Dream flight energy should be positive");
        }

        [Test]
        public void DreamFlightEnergyConsumption_LessThanMaxEnergy()
        {
            Assert.AreEqual(25f, controller.dreamFlightEnergyConsumption);
            Assert.Less(controller.dreamFlightEnergyConsumption, controller.dreamFlightEnergy,
                "Energy consumption should be less than max energy");
        }

        [Test]
        public void DreamFlightEnergyRecharge_IsPositive()
        {
            Assert.AreEqual(10f, controller.dreamFlightEnergyRecharge);
            Assert.Greater(controller.dreamFlightEnergyRecharge, 0f,
                "Energy recharge rate should be positive");
        }

        [Test]
        public void DreamFlightEnergyBalance_IsReasonable()
        {
            // Consumption should be greater than recharge for limited use
            Assert.Greater(controller.dreamFlightEnergyConsumption, controller.dreamFlightEnergyRecharge,
                "Energy consumption should exceed recharge for balanced gameplay");
        }

        #endregion

        #region Grind System Tests

        [Test]
        public void GrindSpeed_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(20f, controller.grindSpeed);
            Assert.Greater(controller.grindSpeed, controller.moveSpeed,
                "Grind speed should be faster than normal movement");
        }

        [Test]
        public void GrindDetectionRadius_DefaultValue_IsPositive()
        {
            Assert.AreEqual(2f, controller.grindDetectionRadius);
            Assert.Greater(controller.grindDetectionRadius, 0f,
                "Grind detection radius should be positive");
        }

        [Test]
        public void GrindForce_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(15f, controller.grindForce);
            Assert.Greater(controller.grindForce, 0f,
                "Grind force should be positive");
        }

        [Test]
        public void GrindExitForce_DefaultValue_IsPositive()
        {
            Assert.AreEqual(10f, controller.grindExitForce);
            Assert.Greater(controller.grindExitForce, 0f,
                "Grind exit force should be positive for momentum preservation");
        }

        [Test]
        public void GrindExitForce_LessThanGrindForce()
        {
            Assert.Less(controller.grindExitForce, controller.grindForce,
                "Exit force is typically less than grind force");
        }

        #endregion

        #region Translocation System Tests

        [Test]
        public void SphereLaunchForce_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(50f, controller.sphereLaunchForce);
            Assert.Greater(controller.sphereLaunchForce, 0f,
                "Sphere launch force should be positive");
        }

        [Test]
        public void SphereLifetime_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(8f, controller.sphereLifetime);
            Assert.That(controller.sphereLifetime, Is.InRange(1f, 30f),
                "Sphere lifetime should be reasonable (1-30 seconds)");
        }

        [Test]
        public void WarpCooldown_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(2f, controller.warpCooldown);
            Assert.Greater(controller.warpCooldown, 0f,
                "Warp cooldown should be positive");
        }

        [Test]
        public void WarpCooldown_ShorterThanSphereLifetime()
        {
            Assert.Less(controller.warpCooldown, controller.sphereLifetime,
                "Warp cooldown should be shorter than sphere lifetime for continuous use");
        }

        #endregion

        #region Wall Kick System Tests

        [Test]
        public void WallKickForce_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(15f, controller.wallKickForce);
            Assert.Greater(controller.wallKickForce, 0f,
                "Wall kick force should be positive");
        }

        [Test]
        public void WallKickRadius_DefaultValue_IsPositive()
        {
            Assert.AreEqual(1f, controller.wallKickRadius);
            Assert.Greater(controller.wallKickRadius, 0f,
                "Wall kick detection radius should be positive");
        }

        [Test]
        public void WallKickCooldown_DefaultValue_IsShort()
        {
            Assert.AreEqual(0.5f, controller.wallKickCooldown);
            Assert.That(controller.wallKickCooldown, Is.InRange(0.1f, 2f),
                "Wall kick cooldown should be short (0.1-2 seconds)");
        }

        #endregion

        #region Camera Parameters Tests

        [Test]
        public void LookSpeed_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(2f, controller.lookSpeed);
            Assert.Greater(controller.lookSpeed, 0f,
                "Look speed should be positive");
        }

        [Test]
        public void LookXLimit_DefaultValue_IsReasonable()
        {
            Assert.AreEqual(45f, controller.lookXLimit);
            Assert.That(controller.lookXLimit, Is.InRange(30f, 90f),
                "Look X limit should be reasonable (30-90 degrees)");
        }

        #endregion

        #region Component Dependency Tests

        [Test]
        public void AdvancedPlayerController_RequiresCharacterController()
        {
            Assert.IsNotNull(characterController,
                "AdvancedPlayerController requires CharacterController component");
            Assert.IsTrue(playerObject.TryGetComponent<CharacterController>(out _),
                "CharacterController should be automatically added");
        }

        #endregion

        #region Speed Hierarchy Tests

        [Test]
        public void SpeedHierarchy_IsLogical()
        {
            // Normal < DreamFlightBase < Grind < Glide < DreamFlightMax
            Assert.Less(controller.moveSpeed, controller.dreamFlightBaseSpeed,
                "Normal speed < Dream flight base speed");
            Assert.Less(controller.dreamFlightBaseSpeed, controller.grindSpeed,
                "Dream flight base < Grind speed");
            Assert.Less(controller.grindSpeed, controller.glideMaxSpeed,
                "Grind speed < Glide speed");
            Assert.Less(controller.glideMaxSpeed, controller.dreamFlightMaxSpeed,
                "Glide speed < Dream flight max speed");
        }

        [Test]
        public void MaxSpeedValues_AreAllPositive()
        {
            Assert.Greater(controller.moveSpeed, 0f);
            Assert.Greater(controller.maxMomentumSpeed, 0f);
            Assert.Greater(controller.glideMaxSpeed, 0f);
            Assert.Greater(controller.grindSpeed, 0f);
            Assert.Greater(controller.dreamFlightBaseSpeed, 0f);
            Assert.Greater(controller.dreamFlightMaxSpeed, 0f);
        }

        #endregion
    }
}
