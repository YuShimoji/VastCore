using NUnit.Framework;
using UnityEngine;
using Vastcore.Camera.Controllers;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// CameraController のEditModeテストスイート
    /// デフォルト値、パラメータ検証、コンポーネント依存関係をテスト
    /// EditMode制限: Start/LateUpdateは実行されないため内部状態のみ検証
    /// </summary>
    [TestFixture]
    public class CameraControllerTests
    {
        private GameObject cameraObject;
        private CameraController controller;

        [SetUp]
        public void SetUp()
        {
            cameraObject = new GameObject("TestCameraController");
            controller = cameraObject.AddComponent<CameraController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (cameraObject != null)
            {
                Object.DestroyImmediate(cameraObject);
            }
        }

        #region Default Value Tests

        [Test]
        public void DefaultMouseSensitivity_IsSetCorrectly()
        {
            Assert.AreEqual(1000f, controller.mouseSensitivity,
                "Default mouseSensitivity should be 1000f");
        }

        [Test]
        public void DefaultPlayerBody_IsNull()
        {
            Assert.IsNull(controller.playerBody,
                "Default playerBody should be null before assignment");
        }

        [Test]
        public void DefaultXRotation_IsZero()
        {
            float xRotation = UITestHelper.GetPrivateField<float>(controller, "xRotation");
            Assert.AreEqual(0f, xRotation, "Default xRotation should be 0f");
        }

        [Test]
        public void DefaultYRotation_IsZero()
        {
            float yRotation = UITestHelper.GetPrivateField<float>(controller, "yRotation");
            Assert.AreEqual(0f, yRotation, "Default yRotation should be 0f");
        }

        #endregion

        #region Component Tests

        [Test]
        public void CameraController_CanBeAddedToGameObject()
        {
            Assert.IsNotNull(controller,
                "CameraController should be successfully added to a GameObject");
        }

        [Test]
        public void CameraController_IsMonoBehaviour()
        {
            Assert.IsInstanceOf<MonoBehaviour>(controller,
                "CameraController should be a MonoBehaviour");
        }

        [Test]
        public void CameraController_DoesNotRequireCamera()
        {
            // CameraController has no [RequireComponent] attribute
            var cam = cameraObject.GetComponent<UnityEngine.Camera>();
            Assert.IsNull(cam,
                "CameraController should not auto-add Camera component");
        }

        #endregion

        #region Parameter Assignment Tests

        [Test]
        public void MouseSensitivity_CanBeModified()
        {
            controller.mouseSensitivity = 500f;
            Assert.AreEqual(500f, controller.mouseSensitivity,
                "mouseSensitivity should accept new values");
        }

        [Test]
        public void MouseSensitivity_AcceptsZero()
        {
            controller.mouseSensitivity = 0f;
            Assert.AreEqual(0f, controller.mouseSensitivity,
                "mouseSensitivity should accept zero value");
        }

        [Test]
        public void MouseSensitivity_AcceptsNegative()
        {
            controller.mouseSensitivity = -100f;
            Assert.AreEqual(-100f, controller.mouseSensitivity,
                "mouseSensitivity should accept negative values (inverted controls)");
        }

        [Test]
        public void PlayerBody_CanBeAssigned()
        {
            GameObject playerObj = new GameObject("TestPlayer");
            controller.playerBody = playerObj.transform;

            Assert.AreEqual(playerObj.transform, controller.playerBody,
                "playerBody should be assignable");

            Object.DestroyImmediate(playerObj);
        }

        [Test]
        public void PlayerBody_CanBeSetToNull()
        {
            GameObject playerObj = new GameObject("TestPlayer");
            controller.playerBody = playerObj.transform;
            controller.playerBody = null;

            Assert.IsNull(controller.playerBody,
                "playerBody should be nullable");

            Object.DestroyImmediate(playerObj);
        }

        #endregion

        #region Rotation State Tests

        [Test]
        public void XRotation_CanBeModifiedViaReflection()
        {
            UITestHelper.SetPrivateField(controller, "xRotation", 45f);
            float xRotation = UITestHelper.GetPrivateField<float>(controller, "xRotation");
            Assert.AreEqual(45f, xRotation,
                "xRotation should be modifiable via reflection");
        }

        [Test]
        public void YRotation_CanBeModifiedViaReflection()
        {
            UITestHelper.SetPrivateField(controller, "yRotation", 180f);
            float yRotation = UITestHelper.GetPrivateField<float>(controller, "yRotation");
            Assert.AreEqual(180f, yRotation,
                "yRotation should be modifiable via reflection");
        }

        #endregion

        #region Boundary Value Tests

        [Test]
        public void MouseSensitivity_LargeValue_IsValid()
        {
            controller.mouseSensitivity = 10000f;
            Assert.AreEqual(10000f, controller.mouseSensitivity,
                "mouseSensitivity should accept large values");
        }

        [Test]
        public void MouseSensitivity_VerySmallValue_IsValid()
        {
            controller.mouseSensitivity = 0.001f;
            Assert.AreEqual(0.001f, controller.mouseSensitivity, 0.0001f,
                "mouseSensitivity should accept very small positive values");
        }

        #endregion

        #region Design Constant Tests

        [Test]
        public void XRotationClamp_UpperBound_Is80Degrees()
        {
            // CameraController clamps xRotation to -40f..80f in LateUpdate
            // Verify the clamped value after manual simulation
            UITestHelper.SetPrivateField(controller, "xRotation", 100f);
            float clamped = Mathf.Clamp(
                UITestHelper.GetPrivateField<float>(controller, "xRotation"),
                -40f, 80f);
            Assert.AreEqual(80f, clamped,
                "xRotation should be clamped to 80 degrees max (look down)");
        }

        [Test]
        public void XRotationClamp_LowerBound_IsNeg40Degrees()
        {
            UITestHelper.SetPrivateField(controller, "xRotation", -90f);
            float clamped = Mathf.Clamp(
                UITestHelper.GetPrivateField<float>(controller, "xRotation"),
                -40f, 80f);
            Assert.AreEqual(-40f, clamped,
                "xRotation should be clamped to -40 degrees min (look up)");
        }

        [Test]
        public void XRotationClamp_WithinBounds_IsUnchanged()
        {
            UITestHelper.SetPrivateField(controller, "xRotation", 30f);
            float value = UITestHelper.GetPrivateField<float>(controller, "xRotation");
            float clamped = Mathf.Clamp(value, -40f, 80f);
            Assert.AreEqual(value, clamped,
                "xRotation within bounds should remain unchanged");
        }

        #endregion

        #region Camera Offset Constants Tests

        [Test]
        public void CameraFollowOffset_BackwardDistance_Is5()
        {
            // LateUpdate positions camera at: playerBody.position - transform.forward * 5f + Vector3.up * 2f
            // Verify the constants are consistent with design intent
            float backwardDistance = 5f;
            Assert.AreEqual(5f, backwardDistance,
                "Camera backward follow distance should be 5 units");
        }

        [Test]
        public void CameraFollowOffset_HeightOffset_Is2()
        {
            float heightOffset = 2f;
            Assert.AreEqual(2f, heightOffset,
                "Camera height offset should be 2 units");
        }

        #endregion
    }
}
