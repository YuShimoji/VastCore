using System.Collections;
using NUnit.Framework;
using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// TerrainGenerator 縺ｨ HeightMapGenerator 縺ｮ邨ｱ蜷医ユ繧ｹ繝・
    /// 螳滄圀縺ｫ Terrain 繧堤函謌舌＠縲∝渕譛ｬ逧・↑騾｣謳ｺ縺梧ｩ溯・縺励※縺・ｋ縺薙→繧呈､懆ｨｼ縺吶ｋ縲・
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

            // 蟆上＆繧√・險ｭ螳壹〒繝・せ繝茨ｼ医ヱ繝輔か繝ｼ繝槭Φ繧ｹ繝ｻ螳牙ｮ壽ｧ縺ｮ縺溘ａ・・
            _generator.Width = 100;
            _generator.Height = 100;
            _generator.Depth = 50;
            _generator.Resolution = 33;

            // 繝槭ユ繝ｪ繧｢繝ｫ縺ｫ萓晏ｭ倥＠縺ｪ縺・ｈ縺・∵ｨ呎ｺ悶す繧ｧ繝ｼ繝繝ｼ縺ｧ邁｡譏薙・繝・Μ繧｢繝ｫ繧定ｨｭ螳・
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

            // 逕滓・縺輔ｌ縺・Terrain GameObject 繧偵け繝ｪ繝ｼ繝ｳ繧｢繝・・
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
            _generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.Noise;

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
            // Arrange: 繧ｷ繝ｳ繝励Ν縺ｪ鬮倥＆繝槭ャ繝励ユ繧ｯ繧ｹ繝√Ε繧堤畑諢・
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var colors = new Color[16];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.black;
            }
            // 荳驛ｨ縺ｮ繝斐け繧ｻ繝ｫ縺縺醍區縺ｫ縺吶ｋ縺薙→縺ｧ髱槭ぞ繝ｭ鬮倥＆繧呈悄蠕・
            colors[5] = Color.white;
            colors[10] = Color.gray;
            texture.SetPixels(colors);
            texture.Apply();

            _generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.HeightMap;
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
            _generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.Noise;

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
            _generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.Noise;
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
            // Arrange: R=1.0, G=0.0, B=0.0 縺ｮ繝・け繧ｹ繝√Ε
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var colors = new Color[64];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color(1.0f, 0.0f, 0.0f, 0.0f); // R=1.0, 莉・0.0
            }
            texture.SetPixels(colors);
            texture.Apply();

            _generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.HeightMap;
            _generator.HeightMap = texture;
            _generator.HeightMapChannel = HeightMapChannel.R;

            // Act
            IEnumerator routine = _generator.GenerateTerrain();
            while (routine.MoveNext()) { }

            // Assert
            Assert.IsNotNull(_generator.GeneratedTerrain, "Terrain should be generated");
            var data = _generator.GeneratedTerrain.terrainData;
            
            // R繝√Ε繝ｳ繝阪Ν縺御ｽｿ逕ｨ縺輔ｌ縺ｦ縺・ｋ縺溘ａ縲・ｫ倥＆蛟､縺ｯ鬮倥＞縺ｯ縺・
            bool hasHighValue = false;
            int res = data.heightmapResolution;
            for (int y = 0; y < res && !hasHighValue; y++)
            {
                for (int x = 0; x < res && !hasHighValue; x++)
                {
                    if (data.GetHeight(x, y) > data.size.y * 0.5f) // 鬮倥＆縺ｮ50%莉･荳・
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
            // Arrange: 繧ｰ繝ｩ繝・・繧ｷ繝ｧ繝ｳ繝・け繧ｹ繝√Ε
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    float value = (float)(x + y) / 14.0f; // 0.0 ~ 1.0 縺ｮ繧ｰ繝ｩ繝・・繧ｷ繝ｧ繝ｳ
                    texture.SetPixel(x, y, new Color(value, value, value));
                }
            }
            texture.Apply();

            _generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.HeightMap;
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
            // (螳悟・縺ｫ蜿崎ｻ｢縺吶ｋ繧上￠縺ｧ縺ｯ縺ｪ縺・′縲∝・蟶・・螟牙喧縺吶ｋ)
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

            _generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.NoiseAndHeightMap;
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

