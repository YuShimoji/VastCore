using System.Collections;
using NUnit.Framework;
using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// TerrainGenerator と HeightMapGenerator の統合テスト
    /// 実際に Terrain を生成し、基本的な連携が機能していることを検証する。
    /// </summary>
    [TestFixture]
    public class TerrainGeneratorIntegrationTests
    {
        private GameObject _testObject;
        private TerrainGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestTerrainGenerator_Integration");
            _generator = _testObject.AddComponent<TerrainGenerator>();

            // 小さめの設定でテスト（パフォーマンス・安定性のため）
            _generator.Width = 100;
            _generator.Height = 100;
            _generator.Depth = 50;
            _generator.Resolution = 33;

            // マテリアルに依存しないよう、標準シェーダーで簡易マテリアルを設定
            var shader = Shader.Find("Standard");
            if (shader != null)
            {
                _generator.TerrainMaterial = new Material(shader);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }

            // 生成された Terrain GameObject をクリーンアップ
            var terrains = Object.FindObjectsByType<UnityEngine.Terrain>(FindObjectsSortMode.None);
            foreach (var terrain in terrains)
            {
                if (terrain != null)
                {
                    Object.DestroyImmediate(terrain.gameObject);
                }
            }
        }

        [Test]
        public void GenerateTerrain_NoiseMode_CreatesTerrainWithCorrectSize()
        {
            // Arrange
            _generator.GenerationMode = TerrainGenerationMode.Noise;

            // Act
            IEnumerator routine = _generator.GenerateTerrain();
            while (routine.MoveNext()) { }

            // Assert
            Assert.IsNotNull(_generator.GeneratedTerrain, "GeneratedTerrain should not be null after generation.");

            var data = _generator.GeneratedTerrain.terrainData;
            Assert.AreEqual(_generator.Resolution, data.heightmapResolution, "Heightmap resolution should match generator setting.");

            var expectedSize = new Vector3(_generator.Width, _generator.Depth, _generator.Height);
            Assert.AreEqual(expectedSize, data.size, "TerrainData size should match generator settings.");
        }

        [Test]
        public void GenerateTerrain_HeightMapMode_UsesProvidedHeightMap()
        {
            // Arrange: シンプルな高さマップテクスチャを用意
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var colors = new Color[16];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.black;
            }
            // 一部のピクセルだけ白にすることで非ゼロ高さを期待
            colors[5] = Color.white;
            colors[10] = Color.gray;
            texture.SetPixels(colors);
            texture.Apply();

            _generator.GenerationMode = TerrainGenerationMode.HeightMap;
            _generator.HeightMap = texture;
            _generator.HeightMapScale = 1f;
            _generator.HeightMapOffset = 0f;

            // Act
            IEnumerator routine = _generator.GenerateTerrain();
            while (routine.MoveNext()) { }

            // Assert
            Assert.IsNotNull(_generator.GeneratedTerrain, "GeneratedTerrain should not be null after heightmap generation.");
            var data = _generator.GeneratedTerrain.terrainData;

            bool hasNonZeroHeight = false;
            int res = data.heightmapResolution;
            for (int y = 0; y < res && !hasNonZeroHeight; y++)
            {
                for (int x = 0; x < res && !hasNonZeroHeight; x++)
                {
                    if (data.GetHeight(x, y) > 0f)
                    {
                        hasNonZeroHeight = true;
                    }
                }
            }

            Assert.IsTrue(hasNonZeroHeight, "Generated terrain should contain non-zero heights when a heightmap is provided.");
        }

        [Test]
        public void GetHighestPoint_ReturnsNonZeroWorldPositionAfterGeneration()
        {
            // Arrange
            _generator.GenerationMode = TerrainGenerationMode.Noise;

            // Act
            IEnumerator routine = _generator.GenerateTerrain();
            while (routine.MoveNext()) { }

            Vector3 highestPoint = _generator.GetHighestPoint();

            // Assert
            Assert.AreNotEqual(Vector3.zero, highestPoint, "Highest point should not be the zero vector after terrain generation.");
        }

        #region TASK_010/011: Integration Tests for New Features

        [Test]
        public void GenerateTerrain_NoiseMode_WithSeed_ProducesDeterministicResult()
        {
            // Arrange
            _generator.GenerationMode = TerrainGenerationMode.Noise;
            _generator.Seed = 12345;

            // Act - First generation
            IEnumerator routine1 = _generator.GenerateTerrain();
            while (routine1.MoveNext()) { }
            var terrain1 = _generator.GeneratedTerrain;
            var data1 = terrain1.terrainData;
            float[,] heights1 = data1.GetHeights(0, 0, data1.heightmapResolution, data1.heightmapResolution);

            // Clear and regenerate with same seed
            if (terrain1 != null)
            {
                Object.DestroyImmediate(terrain1.gameObject);
            }
            _generator.Seed = 12345; // Same seed
            IEnumerator routine2 = _generator.GenerateTerrain();
            while (routine2.MoveNext()) { }
            var terrain2 = _generator.GeneratedTerrain;
            var data2 = terrain2.terrainData;
            float[,] heights2 = data2.GetHeights(0, 0, data2.heightmapResolution, data2.heightmapResolution);

            // Assert - Same seed should produce same result
            Assert.AreEqual(heights1.GetLength(0), heights2.GetLength(0));
            Assert.AreEqual(heights1.GetLength(1), heights2.GetLength(1));

            for (int y = 0; y < heights1.GetLength(0); y++)
            {
                for (int x = 0; x < heights1.GetLength(1); x++)
                {
                    Assert.AreEqual(heights1[y, x], heights2[y, x], 
                        $"Height at [{y},{x}] should be identical for same seed");
                }
            }
        }

        [Test]
        public void GenerateTerrain_HeightMapMode_WithChannel_AppliesChannelSelection()
        {
            // Arrange: R=1.0, G=0.0, B=0.0 のテクスチャ
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var colors = new Color[64];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color(1.0f, 0.0f, 0.0f, 0.0f); // R=1.0, 他=0.0
            }
            texture.SetPixels(colors);
            texture.Apply();

            _generator.GenerationMode = TerrainGenerationMode.HeightMap;
            _generator.HeightMap = texture;
            _generator.HeightMapChannel = HeightMapChannel.R;

            // Act
            IEnumerator routine = _generator.GenerateTerrain();
            while (routine.MoveNext()) { }

            // Assert
            Assert.IsNotNull(_generator.GeneratedTerrain, "Terrain should be generated");
            var data = _generator.GeneratedTerrain.terrainData;
            
            // Rチャンネルが使用されているため、高さ値は高いはず
            bool hasHighValue = false;
            int res = data.heightmapResolution;
            for (int y = 0; y < res && !hasHighValue; y++)
            {
                for (int x = 0; x < res && !hasHighValue; x++)
                {
                    if (data.GetHeight(x, y) > data.size.y * 0.5f) // 高さの50%以上
                    {
                        hasHighValue = true;
                    }
                }
            }

            Assert.IsTrue(hasHighValue, "Red channel should produce high terrain when used");
            
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void GenerateTerrain_HeightMapMode_WithInvertHeight_InvertsTerrain()
        {
            // Arrange: グラデーションテクスチャ
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    float value = (float)(x + y) / 14.0f; // 0.0 ~ 1.0 のグラデーション
                    texture.SetPixel(x, y, new Color(value, value, value));
                }
            }
            texture.Apply();

            _generator.GenerationMode = TerrainGenerationMode.HeightMap;
            _generator.HeightMap = texture;
            _generator.InvertHeight = false;

            // Act - Normal generation
            IEnumerator routine1 = _generator.GenerateTerrain();
            while (routine1.MoveNext()) { }
            var terrain1 = _generator.GeneratedTerrain;
            var data1 = terrain1.terrainData;
            float maxHeight1 = 0f;
            int res = data1.heightmapResolution;
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float h = data1.GetHeight(x, y);
                    if (h > maxHeight1) maxHeight1 = h;
                }
            }

            // Clear and regenerate with InvertHeight = true
            if (terrain1 != null)
            {
                Object.DestroyImmediate(terrain1.gameObject);
            }
            _generator.InvertHeight = true;
            IEnumerator routine2 = _generator.GenerateTerrain();
            while (routine2.MoveNext()) { }
            var terrain2 = _generator.GeneratedTerrain;
            var data2 = terrain2.terrainData;
            float maxHeight2 = 0f;
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float h = data2.GetHeight(x, y);
                    if (h > maxHeight2) maxHeight2 = h;
                }
            }

            // Assert - Inverted should have different height distribution
            // (完全に反転するわけではないが、分布は変化する)
            Assert.AreNotEqual(maxHeight1, maxHeight2, 
                "InvertHeight should change the height distribution");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void GenerateTerrain_CombinedMode_WithNewFeatures_WorksCorrectly()
        {
            // Arrange: Combined mode with new features
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    texture.SetPixel(x, y, new Color(0.5f, 0.5f, 0.5f));
                }
            }
            texture.Apply();

            _generator.GenerationMode = TerrainGenerationMode.NoiseAndHeightMap;
            _generator.HeightMap = texture;
            _generator.HeightMapChannel = HeightMapChannel.Luminance;
            _generator.Seed = 54321;
            _generator.UVTiling = new Vector2(2.0f, 2.0f);
            _generator.InvertHeight = false;

            // Act
            IEnumerator routine = _generator.GenerateTerrain();
            while (routine.MoveNext()) { }

            // Assert
            Assert.IsNotNull(_generator.GeneratedTerrain, 
                "Combined mode with new features should generate terrain successfully");
            var data = _generator.GeneratedTerrain.terrainData;
            Assert.AreEqual(_generator.Resolution, data.heightmapResolution);

            Object.DestroyImmediate(texture);
        }

        #endregion
    }
}
