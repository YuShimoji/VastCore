using UnityEngine;
using NUnit.Framework;
using Vastcore.Generation.Map;

namespace VastCore.Testing
{
    /// <summary>
    /// 基本的な地形生成テスト
    /// 地形生成エンジンの基本機能をテスト
    /// </summary>
    public class BasicTerrainGenerationTest
    {
        private RuntimeTerrainManager terrainManager;
        private GameObject testObject;

        [SetUp]
        public void Setup()
        {
            // テスト用のゲームオブジェクト作成
            testObject = new GameObject("TestTerrainManager");
            terrainManager = testObject.AddComponent<RuntimeTerrainManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }

            // シーン内の地形タイルをクリーンアップ
            TerrainTile[] tiles = Object.FindObjectsByType<TerrainTile>(FindObjectsSortMode.None);
            foreach (var tile in tiles)
            {
                Object.DestroyImmediate(tile.gameObject);
            }
        }

        [Test]
        public void TerrainManager_CanBeCreated()
        {
            // Arrange & Act
            var manager = testObject.GetComponent<RuntimeTerrainManager>();

            // Assert
            Assert.IsNotNull(manager, "RuntimeTerrainManager should be created successfully");
        }

        [Test]
        public void TerrainTile_CanBeCreated()
        {
            // Arrange
            Vector2Int tileCoord = new Vector2Int(0, 0);
            int tileSize = 100;
            int resolution = 64;
            float heightScale = 50f;

            var genParams = new RuntimeTerrainManager.TerrainGenerationParams
            {
                frequency = 0.01f,
                amplitude = 1f,
                octaves = 4,
                persistence = 0.5f,
                lacunarity = 2f
            };

            // Act
            GameObject tileObj = new GameObject("TestTile");
            TerrainTile terrainTile = tileObj.AddComponent<TerrainTile>();
            terrainTile.Initialize(tileCoord, tileSize, resolution, heightScale, genParams);

            // Assert
            Assert.IsNotNull(terrainTile, "TerrainTile should be created successfully");
            Assert.IsNotNull(tileObj.GetComponent<MeshFilter>(), "MeshFilter should be attached");
            Assert.IsNotNull(tileObj.GetComponent<MeshRenderer>(), "MeshRenderer should be attached");
            Assert.IsNotNull(tileObj.GetComponent<MeshCollider>(), "MeshCollider should be attached");

            // メッシュが生成されていることを確認
            MeshFilter meshFilter = tileObj.GetComponent<MeshFilter>();
            Assert.IsNotNull(meshFilter.sharedMesh, "Terrain mesh should be generated");
            Assert.Greater(meshFilter.sharedMesh.vertices.Length, 0, "Mesh should have vertices");

            // クリーンアップ
            Object.DestroyImmediate(tileObj);
        }

        [Test]
        public void TerrainGeneration_ProducesValidMesh()
        {
            // Arrange
            Vector2Int tileCoord = new Vector2Int(0, 0);
            int tileSize = 50;
            int resolution = 32;
            float heightScale = 25f;

            var genParams = new RuntimeTerrainManager.TerrainGenerationParams
            {
                frequency = 0.02f,
                amplitude = 1f,
                octaves = 3,
                persistence = 0.6f,
                lacunarity = 2f
            };

            // Act
            GameObject tileObj = new GameObject("TestTile");
            TerrainTile terrainTile = tileObj.AddComponent<TerrainTile>();
            terrainTile.Initialize(tileCoord, tileSize, resolution, heightScale, genParams);

            MeshFilter meshFilter = tileObj.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter.sharedMesh;

            // Assert
            Assert.IsNotNull(mesh, "Mesh should be generated");
            Assert.AreEqual(resolution * resolution, mesh.vertices.Length, "Vertex count should match resolution");
            Assert.AreEqual((resolution - 1) * (resolution - 1) * 6, mesh.triangles.Length, "Triangle count should be correct");
            Assert.AreEqual(mesh.vertices.Length, mesh.uv.Length, "UV count should match vertex count");

            // 高さが適切な範囲内であることを確認
            bool hasValidHeights = false;
            foreach (var vertex in mesh.vertices)
            {
                if (vertex.y >= -heightScale && vertex.y <= heightScale)
                {
                    hasValidHeights = true;
                    break;
                }
            }
            Assert.IsTrue(hasValidHeights, "Terrain should have heights within expected range");

            // クリーンアップ
            Object.DestroyImmediate(tileObj);
        }

        [Test]
        public void TerrainTile_GetHeightAtPosition_ReturnsValidValue()
        {
            // Arrange
            Vector2Int tileCoord = new Vector2Int(0, 0);
            int tileSize = 50;
            int resolution = 32;
            float heightScale = 25f;

            var genParams = new RuntimeTerrainManager.TerrainGenerationParams
            {
                frequency = 0.02f,
                amplitude = 1f,
                octaves = 2,
                persistence = 0.5f,
                lacunarity = 2f
            };

            GameObject tileObj = new GameObject("TestTile");
            TerrainTile terrainTile = tileObj.AddComponent<TerrainTile>();
            terrainTile.Initialize(tileCoord, tileSize, resolution, heightScale, genParams);

            // Act
            Vector3 testPosition = new Vector3(25f, 0f, 25f); // タイルの中心
            float height = terrainTile.GetHeightAtLocalPosition(testPosition);

            // Assert
            Assert.IsFalse(float.IsNaN(height), "Height should not be NaN");
            Assert.IsFalse(float.IsInfinity(height), "Height should not be infinite");
            Assert.GreaterOrEqual(height, -heightScale, "Height should be within minimum range");
            Assert.LessOrEqual(height, heightScale, "Height should be within maximum range");

            // クリーンアップ
            Object.DestroyImmediate(tileObj);
        }

        [Test]
        public void TerrainGenerationParams_CanBeUpdated()
        {
            // Arrange
            Vector2Int tileCoord = new Vector2Int(0, 0);
            int tileSize = 50;
            int resolution = 32;
            float heightScale = 25f;

            var initialParams = new RuntimeTerrainManager.TerrainGenerationParams
            {
                frequency = 0.01f,
                amplitude = 1f,
                octaves = 2,
                persistence = 0.5f,
                lacunarity = 2f
            };

            GameObject tileObj = new GameObject("TestTile");
            TerrainTile terrainTile = tileObj.AddComponent<TerrainTile>();
            terrainTile.Initialize(tileCoord, tileSize, resolution, heightScale, initialParams);

            MeshFilter meshFilter = tileObj.GetComponent<MeshFilter>();
            Mesh initialMesh = meshFilter.sharedMesh;
            Vector3[] initialVertices = initialMesh.vertices;

            // Act - パラメータを更新
            var newParams = new RuntimeTerrainManager.TerrainGenerationParams
            {
                frequency = 0.05f, // より細かいノイズ
                amplitude = 0.5f,  // より低い振幅
                octaves = 3,
                persistence = 0.7f,
                lacunarity = 2.5f
            };

            terrainTile.UpdateTerrain(newParams);
            Mesh updatedMesh = meshFilter.sharedMesh;
            Vector3[] updatedVertices = updatedMesh.vertices;

            // Assert
            Assert.IsNotNull(updatedMesh, "Updated mesh should exist");
            Assert.AreEqual(initialVertices.Length, updatedVertices.Length, "Vertex count should remain the same");

            // 少なくともいくつかの頂点の高さが変化していることを確認
            bool hasChanged = false;
            for (int i = 0; i < initialVertices.Length; i++)
            {
                if (!Mathf.Approximately(initialVertices[i].y, updatedVertices[i].y))
                {
                    hasChanged = true;
                    break;
                }
            }
            Assert.IsTrue(hasChanged, "Terrain should change after parameter update");

            // クリーンアップ
            Object.DestroyImmediate(tileObj);
        }
    }
}
