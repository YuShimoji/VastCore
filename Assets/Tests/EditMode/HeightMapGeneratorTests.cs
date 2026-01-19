using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Vastcore.Generation;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// HeightMapGenerator のユニットテスト
    /// </summary>
    [TestFixture]
    public class HeightMapGeneratorTests
    {
        private GameObject testObject;
        private TerrainGenerator generator;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestTerrainGenerator");
            generator = testObject.AddComponent<TerrainGenerator>();
            
            // 小さい解像度でテスト（パフォーマンス考慮）
            generator.Resolution = 33;
            generator.Width = 100;
            generator.Height = 100;
            generator.Depth = 50;
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }

        #region Noise Generation Tests

        [Test]
        public void GenerateHeights_NoiseMode_ReturnsCorrectDimensions()
        {
            generator.GenerationMode = TerrainGenerationMode.Noise;
            
            float[,] heights = HeightMapGenerator.GenerateHeights(generator);
            
            Assert.AreEqual(generator.Resolution, heights.GetLength(0));
            Assert.AreEqual(generator.Resolution, heights.GetLength(1));
        }

        [Test]
        public void GenerateHeights_NoiseMode_ValuesAreNormalized()
        {
            generator.GenerationMode = TerrainGenerationMode.Noise;
            
            float[,] heights = HeightMapGenerator.GenerateHeights(generator);
            
            for (int y = 0; y < heights.GetLength(0); y++)
            {
                for (int x = 0; x < heights.GetLength(1); x++)
                {
                    Assert.GreaterOrEqual(heights[y, x], 0f, $"Height at [{y},{x}] should be >= 0");
                    Assert.LessOrEqual(heights[y, x], 1f, $"Height at [{y},{x}] should be <= 1");
                }
            }
        }

        [Test]
        public void GenerateHeights_NoiseMode_DifferentOffsetsProduceDifferentResults()
        {
            generator.GenerationMode = TerrainGenerationMode.Noise;
            generator.Offset = Vector2.zero;
            float[,] heights1 = HeightMapGenerator.GenerateHeights(generator);
            
            generator.Offset = new Vector2(1000f, 1000f);
            float[,] heights2 = HeightMapGenerator.GenerateHeights(generator);
            
            // 少なくとも1つの値が異なることを確認
            bool hasDifference = false;
            for (int y = 0; y < heights1.GetLength(0) && !hasDifference; y++)
            {
                for (int x = 0; x < heights1.GetLength(1) && !hasDifference; x++)
                {
                    if (!Mathf.Approximately(heights1[y, x], heights2[y, x]))
                    {
                        hasDifference = true;
                    }
                }
            }
            
            Assert.IsTrue(hasDifference, "Different offsets should produce different height maps");
        }

        #endregion

        #region HeightMap Mode Tests

        [Test]
        public void GenerateHeights_HeightMapMode_WithoutTexture_ReturnsZeroHeights()
        {
            generator.GenerationMode = TerrainGenerationMode.HeightMap;
            generator.HeightMap = null;
            
            // HeightMap が null の場合、Debug.LogError が出力されることを期待
            LogAssert.Expect(LogType.Error, "[TerrainGenerator] Height map is not assigned!");
            
            float[,] heights = HeightMapGenerator.GenerateHeights(generator);
            
            // テクスチャがない場合、すべて0のはず
            for (int y = 0; y < heights.GetLength(0); y++)
            {
                for (int x = 0; x < heights.GetLength(1); x++)
                {
                    Assert.AreEqual(0f, heights[y, x], $"Height at [{y},{x}] should be 0 when no heightmap");
                }
            }
        }

        [Test]
        public void GenerateHeights_HeightMapMode_WithTexture_ReturnsCorrectDimensions()
        {
            generator.GenerationMode = TerrainGenerationMode.HeightMap;
            
            // シンプルなテストテクスチャを作成
            var texture = new Texture2D(32, 32);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    texture.SetPixel(x, y, new Color(0.5f, 0.5f, 0.5f));
                }
            }
            texture.Apply();
            generator.HeightMap = texture;
            
            float[,] heights = HeightMapGenerator.GenerateHeights(generator);
            
            Assert.AreEqual(generator.Resolution, heights.GetLength(0));
            Assert.AreEqual(generator.Resolution, heights.GetLength(1));
            
            Object.DestroyImmediate(texture);
        }

        #endregion

        #region Combined Mode Tests

        [Test]
        public void GenerateHeights_CombinedMode_ReturnsCorrectDimensions()
        {
            generator.GenerationMode = TerrainGenerationMode.NoiseAndHeightMap;
            
            // Combined モードでは HeightMap が必要なのでダミーテクスチャを設定
            var texture = new Texture2D(16, 16);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    texture.SetPixel(x, y, new Color(0.5f, 0.5f, 0.5f));
                }
            }
            texture.Apply();
            generator.HeightMap = texture;
            
            float[,] heights = HeightMapGenerator.GenerateHeights(generator);
            
            Assert.AreEqual(generator.Resolution, heights.GetLength(0));
            Assert.AreEqual(generator.Resolution, heights.GetLength(1));
            
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void GenerateHeights_CombinedMode_ValuesAreNormalized()
        {
            generator.GenerationMode = TerrainGenerationMode.NoiseAndHeightMap;
            
            // テストテクスチャを作成
            var texture = new Texture2D(32, 32);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float gradient = (float)y / 32f;
                    texture.SetPixel(x, y, new Color(gradient, gradient, gradient));
                }
            }
            texture.Apply();
            generator.HeightMap = texture;
            
            float[,] heights = HeightMapGenerator.GenerateHeights(generator);
            
            for (int y = 0; y < heights.GetLength(0); y++)
            {
                for (int x = 0; x < heights.GetLength(1); x++)
                {
                    Assert.GreaterOrEqual(heights[y, x], 0f);
                    Assert.LessOrEqual(heights[y, x], 1f);
                }
            }
            
            Object.DestroyImmediate(texture);
        }

        #endregion

        #region Performance Tests

        [Test]
        public void GenerateHeights_SmallResolution_CompletesQuickly()
        {
            generator.GenerationMode = TerrainGenerationMode.Noise;
            generator.Resolution = 33;
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            HeightMapGenerator.GenerateHeights(generator);
            stopwatch.Stop();
            
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "Small resolution should complete in under 1 second");
        }

        #endregion
    }
}
