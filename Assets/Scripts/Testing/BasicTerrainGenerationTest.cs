using UnityEngine;
using NUnit.Framework;
using Vastcore.Generation;

namespace VastCore.Testing
{
    /// <summary>
    /// 蝓ｺ譛ｬ逧・↑蝨ｰ蠖｢逕滓・繝・せ繝・    /// 蝨ｰ蠖｢逕滓・繧ｨ繝ｳ繧ｸ繝ｳ縺ｮ蝓ｺ譛ｬ讖溯・繧偵ユ繧ｹ繝・    /// </summary>
    public class BasicTerrainGenerationTest
    {
        private RuntimeTerrainManager terrainManager;
        private GameObject testObject;

        [SetUp]
        public void Setup()
        {
            // 繝・せ繝育畑縺ｮ繧ｲ繝ｼ繝繧ｪ繝悶ず繧ｧ繧ｯ繝井ｽ懈・
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

            // 繧ｷ繝ｼ繝ｳ蜀・・蝨ｰ蠖｢繧ｿ繧､繝ｫ繧偵け繝ｪ繝ｼ繝ｳ繧｢繝・・
            Vastcore.Generation.TerrainTileComponent[] tiles = Object.FindObjectsByType<Vastcore.Generation.TerrainTileComponent>(FindObjectsSortMode.None);
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
            var terrainTile = tileObj.AddComponent<Vastcore.Generation.TerrainTileComponent>();
            terrainTile.Initialize(tileCoord, tileSize, resolution, heightScale, genParams);

            // Assert
            Assert.IsNotNull(terrainTile, "TerrainTile should be created successfully");
            Assert.IsNotNull(tileObj.GetComponent<MeshFilter>(), "MeshFilter should be attached");
            Assert.IsNotNull(tileObj.GetComponent<MeshRenderer>(), "MeshRenderer should be attached");
            Assert.IsNotNull(tileObj.GetComponent<MeshCollider>(), "MeshCollider should be attached");

            // 繝｡繝・す繝･縺檎函謌舌＆繧後※縺・ｋ縺薙→繧堤｢ｺ隱・
            MeshFilter meshFilter = tileObj.GetComponent<MeshFilter>();
            Assert.IsNotNull(meshFilter.sharedMesh, "Terrain mesh should be generated");
            Assert.Greater(meshFilter.sharedMesh.vertices.Length, 0, "Mesh should have vertices");

            // 繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・
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
            var terrainTile = tileObj.AddComponent<Vastcore.Generation.TerrainTileComponent>();
            terrainTile.Initialize(tileCoord, tileSize, resolution, heightScale, genParams);

            MeshFilter meshFilter = tileObj.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter.sharedMesh;

            // Assert
            Assert.IsNotNull(mesh, "Mesh should be generated");
            Assert.AreEqual(resolution * resolution, mesh.vertices.Length, "Vertex count should match resolution");
            Assert.AreEqual((resolution - 1) * (resolution - 1) * 6, mesh.triangles.Length, "Triangle count should be correct");
            Assert.AreEqual(mesh.vertices.Length, mesh.uv.Length, "UV count should match vertex count");

            // 鬮倥＆縺碁←蛻・↑遽・峇蜀・〒縺ゅｋ縺薙→繧堤｢ｺ隱・
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

            // 繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・
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
            var terrainTile = tileObj.AddComponent<Vastcore.Generation.TerrainTileComponent>();
            terrainTile.Initialize(tileCoord, tileSize, resolution, heightScale, genParams);

            // Act
            Vector3 testPosition = new Vector3(25f, 0f, 25f); // 繧ｿ繧､繝ｫ縺ｮ荳ｭ蠢・
            float height = terrainTile.tileData.GetHeightAtWorldPosition(testPosition);

            // Assert
            Assert.IsFalse(float.IsNaN(height), "Height should not be NaN");
            Assert.IsFalse(float.IsInfinity(height), "Height should not be infinite");
            Assert.GreaterOrEqual(height, -heightScale, "Height should be within minimum range");
            Assert.LessOrEqual(height, heightScale, "Height should be within maximum range");

            // 繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・
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
            var terrainTile = tileObj.AddComponent<Vastcore.Generation.TerrainTileComponent>();
            terrainTile.Initialize(tileCoord, tileSize, resolution, heightScale, initialParams);

            MeshFilter meshFilter = tileObj.GetComponent<MeshFilter>();
            Mesh initialMesh = meshFilter.sharedMesh;
            Vector3[] initialVertices = initialMesh.vertices;

            // Act - 繝代Λ繝｡繝ｼ繧ｿ繧呈峩譁ｰ
            var newParams = new RuntimeTerrainManager.TerrainGenerationParams
            {
                frequency = 0.05f, // 繧医ｊ邏ｰ縺九＞繝弱う繧ｺ
                amplitude = 0.5f,  // 繧医ｊ菴弱＞謖ｯ蟷・                octaves = 3,
                persistence = 0.7f,
                lacunarity = 2.5f
            };

            terrainTile.UpdateTerrain(newParams);
            Mesh updatedMesh = meshFilter.sharedMesh;
            Vector3[] updatedVertices = updatedMesh.vertices;

            // Assert
            Assert.IsNotNull(updatedMesh, "Updated mesh should exist");
            Assert.AreEqual(initialVertices.Length, updatedVertices.Length, "Vertex count should remain the same");

            // 蟆代↑縺上→繧ゅ＞縺上▽縺九・鬆らせ縺ｮ鬮倥＆縺悟､牙喧縺励※縺・ｋ縺薙→繧堤｢ｺ隱・
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

            // 繧ｯ繝ｪ繝ｼ繝ｳ繧｢繝・・
            Object.DestroyImmediate(tileObj);
        }
    }
}

