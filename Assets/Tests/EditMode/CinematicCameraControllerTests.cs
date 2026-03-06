using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Vastcore.Camera.Cinematic;
using Vastcore.Player;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// CinematicCameraController のEditModeテストスイート
    /// デフォルト値、Setup検証、レターボックス制御、コンポーネント依存をテスト
    /// EditMode制限: コルーチン（PlayInitialCinematicSequence/FadeLetterbox）は実行不可
    /// </summary>
    [TestFixture]
    public class CinematicCameraControllerTests
    {
        private GameObject cinematicObject;
        private CinematicCameraController controller;
        private UnityEngine.Camera cinematicCamera;
        private GameObject letterboxCanvasObject;

        [SetUp]
        public void SetUp()
        {
            cinematicObject = new GameObject("TestCinematicCamera");
            cinematicCamera = cinematicObject.AddComponent<UnityEngine.Camera>();
            controller = cinematicObject.AddComponent<CinematicCameraController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (letterboxCanvasObject != null)
            {
                Object.DestroyImmediate(letterboxCanvasObject);
            }
            if (cinematicObject != null)
            {
                Object.DestroyImmediate(cinematicObject);
            }
        }

        #region Default Value Tests

        [Test]
        public void DefaultTransitionDuration_Is8Seconds()
        {
            float duration = UITestHelper.GetPrivateField<float>(controller, "m_TransitionDuration");
            Assert.AreEqual(8.0f, duration,
                "Default transition duration should be 8.0 seconds");
        }

        [Test]
        public void DefaultStartHeightOffset_Is150()
        {
            float offset = UITestHelper.GetPrivateField<float>(controller, "m_StartHeightOffset");
            Assert.AreEqual(150f, offset,
                "Default start height offset should be 150f");
        }

        [Test]
        public void DefaultStartDistanceOffset_IsNegative200()
        {
            float offset = UITestHelper.GetPrivateField<float>(controller, "m_StartDistanceOffset");
            Assert.AreEqual(-200f, offset,
                "Default start distance offset should be -200f");
        }

        [Test]
        public void DefaultEndPositionOffset_IsCorrect()
        {
            Vector3 offset = UITestHelper.GetPrivateField<Vector3>(controller, "m_EndPositionOffset");
            Assert.AreEqual(new Vector3(0, 3, -8), offset,
                "Default end position offset should be (0, 3, -8)");
        }

        [Test]
        public void DefaultMovementCurve_IsNotNull()
        {
            AnimationCurve curve = UITestHelper.GetPrivateField<AnimationCurve>(controller, "m_MovementCurve");
            Assert.IsNotNull(curve,
                "Default movement curve should not be null");
        }

        [Test]
        public void DefaultMovementCurve_StartsAtZero()
        {
            AnimationCurve curve = UITestHelper.GetPrivateField<AnimationCurve>(controller, "m_MovementCurve");
            Assert.AreEqual(0f, curve.Evaluate(0f), 0.01f,
                "Movement curve should start at 0");
        }

        [Test]
        public void DefaultMovementCurve_EndsAtOne()
        {
            AnimationCurve curve = UITestHelper.GetPrivateField<AnimationCurve>(controller, "m_MovementCurve");
            Assert.AreEqual(1f, curve.Evaluate(1f), 0.01f,
                "Movement curve should end at 1");
        }

        [Test]
        public void DefaultLetterboxFadeDuration_Is1Second()
        {
            float duration = UITestHelper.GetPrivateField<float>(controller, "m_LetterboxFadeDuration");
            Assert.AreEqual(1.0f, duration,
                "Default letterbox fade duration should be 1.0 second");
        }

        [Test]
        public void DefaultLetterboxImages_AreNull()
        {
            Image topLetterbox = UITestHelper.GetPrivateField<Image>(controller, "m_TopLetterbox");
            Image bottomLetterbox = UITestHelper.GetPrivateField<Image>(controller, "m_BottomLetterbox");
            Assert.IsNull(topLetterbox, "Default top letterbox should be null");
            Assert.IsNull(bottomLetterbox, "Default bottom letterbox should be null");
        }

        #endregion

        #region Component Dependency Tests

        [Test]
        public void CinematicCameraController_RequiresCameraComponent()
        {
            // [RequireComponent(typeof(UnityEngine.Camera))] ensures Camera is present
            var cam = cinematicObject.GetComponent<UnityEngine.Camera>();
            Assert.IsNotNull(cam,
                "CinematicCameraController should require Camera component");
        }

        [Test]
        public void CinematicCameraController_IsMonoBehaviour()
        {
            Assert.IsInstanceOf<MonoBehaviour>(controller,
                "CinematicCameraController should be a MonoBehaviour");
        }

        #endregion

        #region Initial State Tests

        [Test]
        public void InitialState_PlayerControllerIsNull()
        {
            var playerController = UITestHelper.GetPrivateField<AdvancedPlayerController>(
                controller, "m_PlayerController");
            Assert.IsNull(playerController,
                "Initial m_PlayerController should be null before Setup");
        }

        [Test]
        public void InitialState_PlayerTransformIsNull()
        {
            var playerTransform = UITestHelper.GetPrivateField<Transform>(
                controller, "m_PlayerTransform");
            Assert.IsNull(playerTransform,
                "Initial m_PlayerTransform should be null before Setup");
        }

        [Test]
        public void InitialState_TerrainTransformIsNull()
        {
            var terrainTransform = UITestHelper.GetPrivateField<Transform>(
                controller, "m_TerrainTransform");
            Assert.IsNull(terrainTransform,
                "Initial m_TerrainTransform should be null before Setup");
        }

        [Test]
        public void InitialState_CinematicCameraIsNull()
        {
            var cam = UITestHelper.GetPrivateField<UnityEngine.Camera>(
                controller, "m_CinematicCamera");
            Assert.IsNull(cam,
                "Initial m_CinematicCamera should be null before Setup");
        }

        #endregion

        #region Setup Method Tests

        [Test]
        public void Setup_WithValidInputs_SetsPlayerController()
        {
            var (playerObj, playerController, terrain) = CreateSetupDependencies();

            LogAssert.Expect(LogType.Error, "Letterbox images are not assigned in the inspector!");
            controller.Setup(playerController, terrain);

            var storedController = UITestHelper.GetPrivateField<AdvancedPlayerController>(
                controller, "m_PlayerController");
            Assert.AreEqual(playerController, storedController,
                "Setup should store the player controller reference");

            CleanupSetupDependencies(playerObj, terrain);
        }

        [Test]
        public void Setup_WithValidInputs_SetsPlayerTransform()
        {
            var (playerObj, playerController, terrain) = CreateSetupDependencies();

            LogAssert.Expect(LogType.Error, "Letterbox images are not assigned in the inspector!");
            controller.Setup(playerController, terrain);

            var storedTransform = UITestHelper.GetPrivateField<Transform>(
                controller, "m_PlayerTransform");
            Assert.AreEqual(playerController.transform, storedTransform,
                "Setup should store the player transform");

            CleanupSetupDependencies(playerObj, terrain);
        }

        [Test]
        public void Setup_WithValidInputs_SetsPlayerCamera()
        {
            var (playerObj, playerController, terrain) = CreateSetupDependencies();
            // Add a child camera to player
            GameObject cameraChild = new GameObject("PlayerCamera");
            cameraChild.transform.SetParent(playerObj.transform);
            cameraChild.AddComponent<UnityEngine.Camera>();

            LogAssert.Expect(LogType.Error, "Letterbox images are not assigned in the inspector!");
            controller.Setup(playerController, terrain);

            var storedCamera = UITestHelper.GetPrivateField<UnityEngine.Camera>(
                controller, "m_PlayerCamera");
            Assert.IsNotNull(storedCamera,
                "Setup should find and store the player's child camera");

            CleanupSetupDependencies(playerObj, terrain);
        }

        [Test]
        public void Setup_WithValidInputs_SetsCinematicCamera()
        {
            var (playerObj, playerController, terrain) = CreateSetupDependencies();

            LogAssert.Expect(LogType.Error, "Letterbox images are not assigned in the inspector!");
            controller.Setup(playerController, terrain);

            var storedCamera = UITestHelper.GetPrivateField<UnityEngine.Camera>(
                controller, "m_CinematicCamera");
            Assert.IsNotNull(storedCamera,
                "Setup should store its own Camera component as cinematic camera");
            Assert.AreEqual(cinematicCamera, storedCamera,
                "Cinematic camera should be the Camera component on the same GameObject");

            CleanupSetupDependencies(playerObj, terrain);
        }

        [Test]
        public void Setup_WithValidTerrain_SetsTerrainTransform()
        {
            var (playerObj, playerController, terrain) = CreateSetupDependencies();

            LogAssert.Expect(LogType.Error, "Letterbox images are not assigned in the inspector!");
            controller.Setup(playerController, terrain);

            var storedTerrain = UITestHelper.GetPrivateField<Transform>(
                controller, "m_TerrainTransform");
            Assert.AreEqual(terrain.transform, storedTerrain,
                "Setup should store the terrain transform");

            CleanupSetupDependencies(playerObj, terrain);
        }

        [Test]
        public void Setup_WithNullTerrain_TerrainTransformIsNull()
        {
            var (playerObj, playerController, terrain) = CreateSetupDependencies();

            LogAssert.Expect(LogType.Error, "Letterbox images are not assigned in the inspector!");
            controller.Setup(playerController, null);

            var storedTerrain = UITestHelper.GetPrivateField<Transform>(
                controller, "m_TerrainTransform");
            Assert.IsNull(storedTerrain,
                "Setup with null terrain should leave terrain transform as null");

            CleanupSetupDependencies(playerObj, terrain);
        }

        [Test]
        public void Setup_WithNoPlayerCamera_PlayerCameraIsNull()
        {
            var (playerObj, playerController, terrain) = CreateSetupDependencies();
            // No child camera added to player

            LogAssert.Expect(LogType.Error, "Letterbox images are not assigned in the inspector!");
            controller.Setup(playerController, terrain);

            var storedCamera = UITestHelper.GetPrivateField<UnityEngine.Camera>(
                controller, "m_PlayerCamera");
            Assert.IsNull(storedCamera,
                "Setup without player child camera should result in null m_PlayerCamera");

            CleanupSetupDependencies(playerObj, terrain);
        }

        [Test]
        public void Setup_WithNullLetterbox_LogsError()
        {
            var (playerObj, playerController, terrain) = CreateSetupDependencies();

            LogAssert.Expect(LogType.Error, "Letterbox images are not assigned in the inspector!");
            controller.Setup(playerController, terrain);

            CleanupSetupDependencies(playerObj, terrain);
        }

        [Test]
        public void Setup_WithLetterboxAssigned_DoesNotLogError()
        {
            var (playerObj, playerController, terrain) = CreateSetupDependencies();
            SetupLetterboxImages();

            // Should not log error when letterbox images are assigned
            controller.Setup(playerController, terrain);

            CleanupSetupDependencies(playerObj, terrain);

            // LogAssert will fail the test if any unexpected errors were logged
        }

        #endregion

        #region SetLetterboxAlpha Tests

        [Test]
        public void SetLetterboxAlpha_SetsTopLetterboxAlpha()
        {
            SetupLetterboxImages();

            UITestHelper.InvokePrivateMethod(controller, "SetLetterboxAlpha", 0.5f);

            Image topLetterbox = UITestHelper.GetPrivateField<Image>(controller, "m_TopLetterbox");
            Assert.AreEqual(0.5f, topLetterbox.color.a, 0.01f,
                "SetLetterboxAlpha should set top letterbox alpha");
        }

        [Test]
        public void SetLetterboxAlpha_SetsBottomLetterboxAlpha()
        {
            SetupLetterboxImages();

            UITestHelper.InvokePrivateMethod(controller, "SetLetterboxAlpha", 0.5f);

            Image bottomLetterbox = UITestHelper.GetPrivateField<Image>(controller, "m_BottomLetterbox");
            Assert.AreEqual(0.5f, bottomLetterbox.color.a, 0.01f,
                "SetLetterboxAlpha should set bottom letterbox alpha");
        }

        [Test]
        public void SetLetterboxAlpha_ZeroValue_MakesTransparent()
        {
            SetupLetterboxImages();

            UITestHelper.InvokePrivateMethod(controller, "SetLetterboxAlpha", 0f);

            Image topLetterbox = UITestHelper.GetPrivateField<Image>(controller, "m_TopLetterbox");
            Image bottomLetterbox = UITestHelper.GetPrivateField<Image>(controller, "m_BottomLetterbox");
            Assert.AreEqual(0f, topLetterbox.color.a, 0.01f,
                "Alpha 0 should make top letterbox fully transparent");
            Assert.AreEqual(0f, bottomLetterbox.color.a, 0.01f,
                "Alpha 0 should make bottom letterbox fully transparent");
        }

        [Test]
        public void SetLetterboxAlpha_OneValue_MakesOpaque()
        {
            SetupLetterboxImages();

            UITestHelper.InvokePrivateMethod(controller, "SetLetterboxAlpha", 1f);

            Image topLetterbox = UITestHelper.GetPrivateField<Image>(controller, "m_TopLetterbox");
            Image bottomLetterbox = UITestHelper.GetPrivateField<Image>(controller, "m_BottomLetterbox");
            Assert.AreEqual(1f, topLetterbox.color.a, 0.01f,
                "Alpha 1 should make top letterbox fully opaque");
            Assert.AreEqual(1f, bottomLetterbox.color.a, 0.01f,
                "Alpha 1 should make bottom letterbox fully opaque");
        }

        [Test]
        public void SetLetterboxAlpha_PreservesRGBValues()
        {
            SetupLetterboxImages();
            Image topLetterbox = UITestHelper.GetPrivateField<Image>(controller, "m_TopLetterbox");

            // Set a custom color first
            topLetterbox.color = new Color(0.5f, 0.3f, 0.8f, 1f);

            UITestHelper.InvokePrivateMethod(controller, "SetLetterboxAlpha", 0.2f);

            Assert.AreEqual(0.5f, topLetterbox.color.r, 0.01f, "Red channel should be preserved");
            Assert.AreEqual(0.3f, topLetterbox.color.g, 0.01f, "Green channel should be preserved");
            Assert.AreEqual(0.8f, topLetterbox.color.b, 0.01f, "Blue channel should be preserved");
            Assert.AreEqual(0.2f, topLetterbox.color.a, 0.01f, "Alpha should be updated");
        }

        [Test]
        public void SetLetterboxAlpha_BothLetterboxesMatch()
        {
            SetupLetterboxImages();

            UITestHelper.InvokePrivateMethod(controller, "SetLetterboxAlpha", 0.75f);

            Image topLetterbox = UITestHelper.GetPrivateField<Image>(controller, "m_TopLetterbox");
            Image bottomLetterbox = UITestHelper.GetPrivateField<Image>(controller, "m_BottomLetterbox");
            Assert.AreEqual(topLetterbox.color.a, bottomLetterbox.color.a, 0.01f,
                "Both letterboxes should have the same alpha value");
        }

        #endregion

        #region Parameter Modification Tests

        [Test]
        public void TransitionDuration_CanBeModified()
        {
            UITestHelper.SetPrivateField(controller, "m_TransitionDuration", 12.0f);
            float duration = UITestHelper.GetPrivateField<float>(controller, "m_TransitionDuration");
            Assert.AreEqual(12.0f, duration,
                "Transition duration should be modifiable");
        }

        [Test]
        public void StartHeightOffset_CanBeModified()
        {
            UITestHelper.SetPrivateField(controller, "m_StartHeightOffset", 300f);
            float offset = UITestHelper.GetPrivateField<float>(controller, "m_StartHeightOffset");
            Assert.AreEqual(300f, offset,
                "Start height offset should be modifiable");
        }

        [Test]
        public void LetterboxFadeDuration_CanBeModified()
        {
            UITestHelper.SetPrivateField(controller, "m_LetterboxFadeDuration", 2.5f);
            float duration = UITestHelper.GetPrivateField<float>(controller, "m_LetterboxFadeDuration");
            Assert.AreEqual(2.5f, duration,
                "Letterbox fade duration should be modifiable");
        }

        #endregion

        #region Helper Methods

        private (GameObject playerObj, AdvancedPlayerController playerController, GameObject terrain) CreateSetupDependencies()
        {
            GameObject playerObj = new GameObject("TestPlayer");
            playerObj.AddComponent<CharacterController>(); // Required by AdvancedPlayerController
            AdvancedPlayerController playerController = playerObj.AddComponent<AdvancedPlayerController>();

            GameObject terrain = new GameObject("TestTerrain");

            return (playerObj, playerController, terrain);
        }

        private void CleanupSetupDependencies(GameObject playerObj, GameObject terrain)
        {
            if (playerObj != null) Object.DestroyImmediate(playerObj);
            if (terrain != null) Object.DestroyImmediate(terrain);
        }

        private void SetupLetterboxImages()
        {
            Canvas canvas = UITestHelper.CreateTestCanvas("LetterboxCanvas");
            letterboxCanvasObject = canvas.gameObject;

            GameObject topObj = UITestHelper.CreateTestUIObject("TopLetterbox", canvas.transform);
            Image topImage = topObj.AddComponent<Image>();
            topImage.color = Color.black;

            GameObject bottomObj = UITestHelper.CreateTestUIObject("BottomLetterbox", canvas.transform);
            Image bottomImage = bottomObj.AddComponent<Image>();
            bottomImage.color = Color.black;

            UITestHelper.SetPrivateField(controller, "m_TopLetterbox", topImage);
            UITestHelper.SetPrivateField(controller, "m_BottomLetterbox", bottomImage);
        }

        #endregion
    }
}
