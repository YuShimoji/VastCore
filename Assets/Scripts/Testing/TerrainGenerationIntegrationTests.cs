using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Vastcore.Generation;
using Vastcore.Terrain;

namespace Vastcore.Testing.RuntimeTests
{
    /// <summary>
    /// 地形生成システム統合テストスイート
    /// 実機上で動作を確認するためのテストコード
    /// </summary>
    public class TerrainGenerationIntegrationTests
    {
        private GameObject testTerrainManager;
        private RuntimeTerrainManager terrainManager;
        private PlayerTrackingSystem playerTracker;
        private DynamicMaterialBlendingSystem materialBlender;

        [SetUp]
        public void Setup()
        {
            // テスト用の地形マネージャー作成
            testTerrainManager = new GameObject("TestTerrainManager");

            // 地形関連コンポーネント追加
            terrainManager = testTerrainManager.AddComponent<RuntimeTerrainManager>();
            playerTracker = testTerrainManager.AddComponent<PlayerTrackingSystem>();
            materialBlender = testTerrainManager.AddComponent<DynamicMaterialBlendingSystem>();

            // プレイヤーオブジェクト作成
            CreateTestPlayer();

            // テスト用の地形データを設定
            SetupTestTerrainData();
        }

        [TearDown]
        public void Teardown()
        {
            Object.Destroy(testTerrainManager);
        }

        /// <summary>
        /// 地形生成の基本機能テスト
        /// </summary>
        [UnityTest]
        public IEnumerator TerrainGeneration_BasicTileGeneration_WorksCorrectly()
        {
            // テスト用のタイル座標
            Vector2Int testTileCoord = new Vector2Int(0, 0);

            // タイル生成
            var tile = terrainManager.GenerateTile(testTileCoord);

            yield return new WaitForFixedUpdate();

            // タイルが正常に生成されたことを確認
            Assert.IsNotNull(tile, "Tile should be generated");
            Assert.IsNotNull(tile.tileObject, "Tile object should exist");

            Debug.Log("✓ Basic tile generation test passed");
        }

        /// <summary>
        /// プリミティブ地形オブジェクト生成テスト
        /// </summary>
        [UnityTest]
        public IEnumerator PrimitiveGeneration_ObjectCreation_WorksCorrectly()
        {
            // プリミティブ生成パラメータ
            var primitiveParams = new PrimitiveTerrainGenerator.PrimitiveParams
            {
                primitiveType = PrimitiveTerrainGenerator.PrimitiveType.Cube,
                position = Vector3.zero,
                scale = Vector3.one * 3f,
                rotation = Quaternion.identity,
                material = null
            };

            // プリミティブ生成
            var primitive = PrimitiveTerrainGenerator.GeneratePrimitive(primitiveParams);

            yield return new WaitForFixedUpdate();

            // プリミティブが正常に生成されたことを確認
            Assert.IsNotNull(primitive, "Primitive should be generated");

            var primitiveComponent = primitive.GetComponent<PrimitiveTerrainObject>();
            Assert.IsNotNull(primitiveComponent, "PrimitiveTerrainObject component should exist");

            Debug.Log("✓ Primitive generation test passed");
        }

        /// <summary>
        /// LODシステムテスト
        /// </summary>
        [UnityTest]
        public IEnumerator LODSystem_DistanceBasedLOD_WorksCorrectly()
        {
            // プリミティブ生成
            var primitive = CreateTestPrimitiveWithLOD(Vector3.zero);

            // プレイヤーを近くに配置
            var player = GameObject.FindGameObjectWithTag("Player");
            player.transform.position = Vector3.zero;

            yield return new WaitForSeconds(0.5f);

            // LODが更新されていることを確認
            var primitiveComponent = primitive.GetComponent<PrimitiveTerrainObject>();
            Assert.IsTrue(primitiveComponent.currentLOD >= 0, "LOD should be set");

            // プレイヤーを遠くに移動
            player.transform.position = Vector3.forward * 3000f;

            yield return new WaitForSeconds(0.5f);

            // LODが変化していることを確認
            Assert.AreNotEqual(0, primitiveComponent.currentLOD, "LOD should change with distance");

            Debug.Log("✓ LOD system test passed");
        }

        /// <summary>
        /// 建築構造生成テスト
        /// </summary>
        [UnityTest]
        public IEnumerator ArchitecturalGeneration_StructureCreation_WorksCorrectly()
        {
            // アーチ構造生成パラメータ
            var archParams = ArchitecturalGenerator.ArchitecturalParams.Default(ArchitecturalGenerator.ArchitecturalType.RomanArch);
            archParams.position = Vector3.zero;
            archParams.span = 10f;
            archParams.height = 5f;
            archParams.thickness = 2f;

            // アーチ生成
            var arch = ArchitecturalGenerator.GenerateArchitecturalStructure(archParams);

            yield return new WaitForFixedUpdate();

            // アーチが正常に生成されたことを確認
            Assert.IsNotNull(arch, "Architectural structure should be generated");

            Debug.Log("✓ Architectural generation test passed");
        }

        /// <summary>
        /// マテリアルブレンディングシステムテスト
        /// </summary>
        [UnityTest]
        public IEnumerator MaterialBlending_DynamicBlending_WorksCorrectly()
        {
            // テスト用のタイル作成
            var tile = CreateTestTile();

            // マテリアルブレンドリクエスト
            materialBlender.RequestMaterialBlend(tile, MaterialBlendType.DistanceLOD, 1);

            yield return new WaitForSeconds(1f);

            // マテリアルがブレンドされていることを確認
            var renderer = tile.tileObject.GetComponent<MeshRenderer>();
            Assert.IsNotNull(renderer, "Tile should have renderer");
            Assert.IsNotNull(renderer.material, "Tile should have material");

            Debug.Log("✓ Material blending test passed");
        }

        /// <summary>
        /// パフォーマンステスト：多数のプリミティブ生成
        /// </summary>
        [UnityTest]
        public IEnumerator TerrainGeneration_Performance_MassGeneration()
        {
            // パフォーマンス測定開始
            float startTime = Time.realtimeSinceStartup;

            // 多数のプリミティブを生成
            const int primitiveCount = 50;
            for (int i = 0; i < primitiveCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-100f, 100f),
                    0f,
                    Random.Range(-100f, 100f)
                );

                CreateTestPrimitive(position);
            }

            // フレーム更新を待機
            yield return new WaitForSeconds(0.5f);

            float endTime = Time.realtimeSinceStartup;
            float generationTime = endTime - startTime;

            // パフォーマンスが許容範囲内か確認
            Assert.Less(generationTime, 10.0f,
                $"Mass generation too slow: {generationTime:F2}s for {primitiveCount} primitives");

            Debug.Log($"✓ Mass generation performance test passed: {generationTime:F2}s");
        }

        /// <summary>
        /// 地形システム統合テスト
        /// </summary>
        [UnityTest]
        public IEnumerator TerrainSystem_Integration_PlayerMovementTerrainInteraction()
        {
            // プレイヤー追跡システムテスト
            var player = GameObject.FindGameObjectWithTag("Player");
            player.transform.position = Vector3.zero;

            yield return new WaitForFixedUpdate();

            // プレイヤータイルが正しく追跡されていることを確認
            Assert.AreEqual(new Vector2Int(0, 0), playerTracker.CurrentPlayerTile,
                "Player tile should be tracked correctly");

            // 周辺タイルがロードされていることを確認
            var nearbyTiles = terrainManager.GetLoadedTiles();
            Assert.IsTrue(nearbyTiles.Count > 0, "Nearby tiles should be loaded");

            Debug.Log("✓ Terrain system integration test passed");
        }

        #region ヘルパーメソッド

        private void CreateTestPlayer()
        {
            var player = new GameObject("TestPlayer");
            player.tag = "Player";
            player.transform.position = Vector3.zero;

            // プレイヤーに必要なコンポーネント追加
            player.AddComponent<CharacterController>();
            var camera = new GameObject("Camera");
            camera.AddComponent<Camera>();
            camera.transform.SetParent(player.transform);
            camera.transform.localPosition = new Vector3(0, 1.6f, 0);
        }

        private void SetupTestTerrainData()
        {
            // テスト用の地形設定
            terrainManager.tileSize = 1000f;
            terrainManager.maxLoadedTiles = 9;
            playerTracker.playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        }

        private TerrainTile CreateTestTile()
        {
            var tileCoord = new Vector2Int(0, 0);
            return terrainManager.GenerateTile(tileCoord);
        }

        private GameObject CreateTestPrimitiveWithLOD(Vector3 position)
        {
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primitive.transform.position = position;

            var primitiveComponent = primitive.AddComponent<PrimitiveTerrainObject>();
            primitiveComponent.primitiveType = PrimitiveTerrainGenerator.PrimitiveType.Cube;
            primitiveComponent.enableLOD = true;
            primitiveComponent.scale = 3f;

            return primitive;
        }

        private GameObject CreateTestPrimitive(Vector3 position)
        {
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            primitive.transform.position = position;
            primitive.transform.localScale = Vector3.one * Random.Range(1f, 3f);

            var primitiveComponent = primitive.AddComponent<PrimitiveTerrainObject>();
            primitiveComponent.primitiveType = PrimitiveTerrainGenerator.PrimitiveType.Cube;
            primitiveComponent.scale = primitive.transform.localScale.magnitude;

            return primitive;
        }

        #endregion
    }

    /// <summary>
    /// マテリアルブレンドタイプ（テスト用）
    /// </summary>
    public enum MaterialBlendType
    {
        DistanceLOD,
        Environmental,
        Seasonal,
        Biome,
        Texture
    }
}
