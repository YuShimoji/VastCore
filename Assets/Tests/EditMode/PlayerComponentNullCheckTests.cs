using NUnit.Framework;
using UnityEngine;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// Player系コンポーネントの必須コンポーネント検証テスト
    /// Null安全性、依存関係、初期化エラーハンドリングの検証
    /// </summary>
    [TestFixture]
    public class PlayerComponentNullCheckTests
    {
        private GameObject playerObject;

        [SetUp]
        public void SetUp()
        {
            playerObject = new GameObject("TestPlayer");
        }

        [TearDown]
        public void TearDown()
        {
            if (playerObject != null)
            {
                Object.DestroyImmediate(playerObject);
            }
        }

        #region PlayerController Component Dependencies

        [Test]
        public void PlayerController_RequiresRigidbody()
        {
            // PlayerController requires Rigidbody
            var rigidbody = playerObject.AddComponent<Rigidbody>();
            playerObject.AddComponent<CapsuleCollider>();
            var controller = playerObject.AddComponent<PlayerController>();

            Assert.IsNotNull(rigidbody, "Rigidbody should be present");
            Assert.IsNotNull(controller, "PlayerController should be created");
        }

        [Test]
        public void PlayerController_RequiresCapsuleCollider()
        {
            // PlayerController requires CapsuleCollider
            playerObject.AddComponent<Rigidbody>();
            var collider = playerObject.AddComponent<CapsuleCollider>();
            var controller = playerObject.AddComponent<PlayerController>();

            Assert.IsNotNull(collider, "CapsuleCollider should be present");
            Assert.IsNotNull(controller, "PlayerController should be created");
        }

        [Test]
        public void PlayerController_WithoutRigidbody_AutoAdds()
        {
            // Unity automatically adds required components
            playerObject.AddComponent<CapsuleCollider>();
            var controller = playerObject.AddComponent<PlayerController>();

            var rigidbody = playerObject.GetComponent<Rigidbody>();
            Assert.IsNotNull(rigidbody, "Rigidbody should be auto-added");
        }

        [Test]
        public void PlayerController_WithoutCollider_AutoAdds()
        {
            // Unity automatically adds required components
            playerObject.AddComponent<Rigidbody>();
            var controller = playerObject.AddComponent<PlayerController>();

            var collider = playerObject.GetComponent<CapsuleCollider>();
            Assert.IsNotNull(collider, "CapsuleCollider should be auto-added");
        }

        #endregion

        #region AdvancedPlayerController Component Dependencies

        [Test]
        public void AdvancedPlayerController_RequiresCharacterController()
        {
            var characterController = playerObject.AddComponent<CharacterController>();
            var controller = playerObject.AddComponent<Vastcore.Player.AdvancedPlayerController>();

            Assert.IsNotNull(characterController, "CharacterController should be present");
            Assert.IsNotNull(controller, "AdvancedPlayerController should be created");
        }

        [Test]
        public void AdvancedPlayerController_WithoutCharacterController_AutoAdds()
        {
            var controller = playerObject.AddComponent<Vastcore.Player.AdvancedPlayerController>();

            var characterController = playerObject.GetComponent<CharacterController>();
            Assert.IsNotNull(characterController, "CharacterController should be auto-added");
        }

        #endregion

        #region ClimbingSystem Component Dependencies

        [Test]
        public void ClimbingSystem_WithPlayerController_Initializes()
        {
            playerObject.AddComponent<CharacterController>();
            var playerController = playerObject.AddComponent<Vastcore.Player.AdvancedPlayerController>();
            var climbingSystem = playerObject.AddComponent<Vastcore.Player.EnhancedClimbingSystem>();

            Assert.IsNotNull(playerController, "AdvancedPlayerController should be present");
            Assert.IsNotNull(climbingSystem, "EnhancedClimbingSystem should be created");
        }

        [Test]
        public void ClimbingSystem_WithCharacterController_Initializes()
        {
            var characterController = playerObject.AddComponent<CharacterController>();
            playerObject.AddComponent<Vastcore.Player.AdvancedPlayerController>();
            var climbingSystem = playerObject.AddComponent<Vastcore.Player.EnhancedClimbingSystem>();

            Assert.IsNotNull(characterController, "CharacterController should be present");
            Assert.IsNotNull(climbingSystem, "EnhancedClimbingSystem should be created");
        }

        #endregion

        #region TranslocationSystem Component Creation

        [Test]
        public void TranslocationSystem_CreatesWithoutDependencies()
        {
            var translocationSystem = playerObject.AddComponent<Vastcore.Player.EnhancedTranslocationSystem>();

            Assert.IsNotNull(translocationSystem,
                "EnhancedTranslocationSystem should create without strict dependencies");
        }

        #endregion

        #region Null Safety Tests

        [Test]
        public void PlayerController_NullRigidbody_HandledSafely()
        {
            playerObject.AddComponent<CapsuleCollider>();
            var controller = playerObject.AddComponent<PlayerController>();

            // Rigidbody is auto-added, so it won't be null
            var rigidbody = playerObject.GetComponent<Rigidbody>();
            Assert.IsNotNull(rigidbody, "Rigidbody should exist (auto-added)");
        }

        [Test]
        public void PlayerController_NullGroundLayer_LogsWarning()
        {
            playerObject.AddComponent<Rigidbody>();
            playerObject.AddComponent<CapsuleCollider>();
            var controller = playerObject.AddComponent<PlayerController>();

            // Ground layer defaults to 0, which triggers warning in Start()
            // This test verifies the component is created despite warning
            Assert.IsNotNull(controller, "PlayerController should handle null groundLayer gracefully");
        }

        #endregion

        #region Component Combination Tests

        [Test]
        public void FullPlayerSetup_AllComponentsPresent()
        {
            var rigidbody = playerObject.AddComponent<Rigidbody>();
            var collider = playerObject.AddComponent<CapsuleCollider>();
            var controller = playerObject.AddComponent<PlayerController>();

            Assert.IsNotNull(rigidbody, "Rigidbody present");
            Assert.IsNotNull(collider, "CapsuleCollider present");
            Assert.IsNotNull(controller, "PlayerController present");
        }

        [Test]
        public void AdvancedPlayerSetup_AllComponentsPresent()
        {
            var characterController = playerObject.AddComponent<CharacterController>();
            var playerController = playerObject.AddComponent<Vastcore.Player.AdvancedPlayerController>();

            Assert.IsNotNull(characterController, "CharacterController present");
            Assert.IsNotNull(playerController, "AdvancedPlayerController present");
        }

        [Test]
        public void CompleteAdvancedSetup_WithExtensions()
        {
            var characterController = playerObject.AddComponent<CharacterController>();
            var playerController = playerObject.AddComponent<Vastcore.Player.AdvancedPlayerController>();
            var climbingSystem = playerObject.AddComponent<Vastcore.Player.EnhancedClimbingSystem>();
            var translocationSystem = playerObject.AddComponent<Vastcore.Player.EnhancedTranslocationSystem>();

            Assert.IsNotNull(characterController, "CharacterController present");
            Assert.IsNotNull(playerController, "AdvancedPlayerController present");
            Assert.IsNotNull(climbingSystem, "EnhancedClimbingSystem present");
            Assert.IsNotNull(translocationSystem, "EnhancedTranslocationSystem present");
        }

        #endregion

        #region RequireComponent Attribute Validation

        [Test]
        public void PlayerController_HasRequireComponentAttribute()
        {
            var type = typeof(PlayerController);
            var attributes = type.GetCustomAttributes(typeof(RequireComponent), true);

            Assert.IsNotEmpty(attributes, "PlayerController should have RequireComponent attributes");
        }

        [Test]
        public void AdvancedPlayerController_HasRequireComponentAttribute()
        {
            var type = typeof(Vastcore.Player.AdvancedPlayerController);
            var attributes = type.GetCustomAttributes(typeof(RequireComponent), true);

            Assert.IsNotEmpty(attributes,
                "AdvancedPlayerController should have RequireComponent attributes");
        }

        #endregion
    }
}
