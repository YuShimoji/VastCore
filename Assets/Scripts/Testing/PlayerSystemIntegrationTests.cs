// Disabled: AdvancedPlayerController API not yet finalized
#if VASTCORE_PLAYER_INTEGRATION_TEST_ENABLED
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Vastcore.Player;
using Vastcore.Generation;

namespace Vastcore.Testing.RuntimeTests
{
    /// <summary>
    /// プレイヤーシステム統合テストスイート
    /// 実機上で動作を確認するためのテストコード
    /// </summary>
    public class PlayerSystemIntegrationTests
    {
        private GameObject testPlayer;
        private GameObject testTerrain;
        private AdvancedPlayerController playerController;
        private EnhancedTranslocationSystem translocationSystem;
        private EnhancedGrindSystem grindSystem;

        [SetUp]
        public void Setup()
        {
            // テスト用のプレイヤーオブジェクト作成
            testPlayer = new GameObject("TestPlayer");
            testPlayer.tag = "Player";

            // プレイヤーコンポーネント追加
            playerController = testPlayer.AddComponent<AdvancedPlayerController>();
            translocationSystem = testPlayer.AddComponent<EnhancedTranslocationSystem>();
            grindSystem = testPlayer.AddComponent<EnhancedGrindSystem>();

            // テスト用の地形オブジェクト作成
            testTerrain = new GameObject("TestTerrain");
            testTerrain.layer = LayerMask.NameToLayer("Terrain");

            // プリミティブオブジェクト作成
            CreateTestPrimitiveObjects();
        }

        [TearDown]
        public void Teardown()
        {
            Object.Destroy(testPlayer);
            Object.Destroy(testTerrain);
        }

        /// <summary>
        /// プレイヤー移動システムの基本機能テスト
        /// </summary>
        [UnityTest]
        public IEnumerator PlayerMovement_BasicMovement_WorksCorrectly()
        {
            // 初期位置設定
            testPlayer.transform.position = Vector3.zero;

            // 移動入力シミュレーション
            var inputVector = new Vector3(1f, 0f, 0f);
            playerController.Move(inputVector);

            // フレーム待機
            yield return new WaitForFixedUpdate();

            // 移動を確認
            Assert.AreNotEqual(Vector3.zero, testPlayer.transform.position,
                "Player should move when input is provided");

            Debug.Log("✓ Player movement test passed");
        }

        /// <summary>
        /// ワープシステムの基本機能テスト
        /// </summary>
        [UnityTest]
        public IEnumerator TranslocationSystem_WarpFunctionality_WorksCorrectly()
        {
            // ワープ設定
            translocationSystem.translocationSpherePrefab = CreateTestSpherePrefab();

            // 初期位置
            Vector3 startPosition = testPlayer.transform.position;
            Vector3 targetPosition = startPosition + Vector3.forward * 10f;

            // ワープ実行
            translocationSystem.ExecuteWarp(targetPosition);

            yield return new WaitForFixedUpdate();

            // ワープ成功を確認
            Assert.AreNotEqual(startPosition, testPlayer.transform.position,
                "Player should warp to target position");

            Debug.Log("✓ Translocation system test passed");
        }

        /// <summary>
        /// グラインドシステムの基本機能テスト
        /// </summary>
        [UnityTest]
        public IEnumerator GrindSystem_GrindDetection_WorksCorrectly()
        {
            // グラインド可能なプリミティブを作成
            var grindableObject = CreateGrindablePrimitive(Vector3.forward * 5f);

            // プレイヤーをグラインド位置に移動
            testPlayer.transform.position = grindableObject.transform.position + Vector3.up * 2f;

            yield return new WaitForFixedUpdate();

            // グラインド可能オブジェクトが検出されることを確認
            Assert.IsTrue(grindSystem.IsGrinding || CanStartGrind(grindSystem),
                "Grind system should detect grindable surfaces");

            Debug.Log("✓ Grind system detection test passed");
        }

        /// <summary>
        /// プリミティブインタラクションシステムテスト
        /// </summary>
        [UnityTest]
        public IEnumerator PrimitiveInteraction_ClimbableDetection_WorksCorrectly()
        {
            // 登攀可能なプリミティブを作成
            var climbableObject = CreateClimbablePrimitive(Vector3.right * 5f);

            // インタラクションシステム取得
            var interactionSystem = testPlayer.GetComponent<PrimitiveInteractionSystem>();
            if (interactionSystem == null)
            {
                interactionSystem = testPlayer.AddComponent<PrimitiveInteractionSystem>();
            }

            yield return new WaitForFixedUpdate();

            // 登攀可能なプリミティブが検出されることを確認
            var nearbyClimbables = interactionSystem.GetNearbyClimbableObjects(testPlayer.transform.position, 10f);
            Assert.IsTrue(nearbyClimbables.Count > 0,
                "Should detect climbable primitive objects");

            Debug.Log("✓ Primitive interaction test passed");
        }

        /// <summary>
        /// プレイヤーシステムの統合パフォーマンステスト
        /// </summary>
        [UnityTest]
        public IEnumerator PlayerSystem_Performance_UnderLoad()
        {
            // 複数のプリミティブオブジェクトを作成
            for (int i = 0; i < 10; i++)
            {
                CreateTestPrimitiveObjects();
            }

            // パフォーマンス測定開始
            float startTime = Time.realtimeSinceStartup;

            // システム更新をシミュレート
            for (int frame = 0; frame < 100; frame++)
            {
                playerController.Move(Vector3.forward);
                translocationSystem.UpdateTranslocationSystem();
                grindSystem.UpdateGrindSystem();

                yield return new WaitForFixedUpdate();
            }

            float endTime = Time.realtimeSinceStartup;
            float totalTime = endTime - startTime;

            // パフォーマンスが許容範囲内か確認
            Assert.Less(totalTime, 5.0f,
                $"Performance test failed: {totalTime:F2}s for 100 frames");

            Debug.Log($"✓ Performance test passed: {totalTime:F2}s for 100 frames");
        }

        #region ヘルパーメソッド

        private GameObject CreateTestSpherePrefab()
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.AddComponent<Rigidbody>();
            return sphere;
        }

        private void CreateTestPrimitiveObjects()
        {
            // テスト用のプリミティブ地形オブジェクトを作成
            for (int i = 0; i < 5; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-50f, 50f),
                    0f,
                    Random.Range(-50f, 50f)
                );

                var primitive = CreateTestPrimitive(position);
                primitive.transform.SetParent(testTerrain.transform);
            }
        }

        private GameObject CreateTestPrimitive(Vector3 position)
        {
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primitive.transform.position = position;
            primitive.transform.localScale = Vector3.one * Random.Range(2f, 5f);

            // PrimitiveTerrainObjectコンポーネントを追加
            var primitiveComponent = primitive.AddComponent<PrimitiveTerrainObject>();
            primitiveComponent.primitiveType = PrimitiveTerrainGenerator.PrimitiveType.Cube;
            primitiveComponent.isClimbable = Random.value > 0.5f;
            primitiveComponent.isGrindable = Random.value > 0.5f;
            primitiveComponent.scale = primitive.transform.localScale.magnitude;

            return primitive;
        }

        private GameObject CreateGrindablePrimitive(Vector3 position)
        {
            var primitive = CreateTestPrimitive(position);
            var primitiveComponent = primitive.GetComponent<PrimitiveTerrainObject>();
            primitiveComponent.isGrindable = true;
            return primitive;
        }

        private GameObject CreateClimbablePrimitive(Vector3 position)
        {
            var primitive = CreateTestPrimitive(position);
            var primitiveComponent = primitive.GetComponent<PrimitiveTerrainObject>();
            primitiveComponent.isClimbable = true;
            return primitive;
        }

        private bool CanStartGrind(EnhancedGrindSystem grindSystem)
        {
            // リフレクションでプライベートメソッドを呼び出し
            var method = typeof(EnhancedGrindSystem).GetMethod("CanStartGrind",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (method != null)
            {
                return (bool)method.Invoke(grindSystem, null);
            }

            return false;
        }

        #endregion
    }
}
#endif
