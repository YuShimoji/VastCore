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

        #region TASK_010/011: New Feature Tests (HeightMapChannel, Seed, UV, Invert)

        [Test]
        public void GenerateHeights_HeightMapMode_ChannelR_UsesRedChannel()
        {
            generator.GenerationMode = TerrainGenerationMode.HeightMap;
            generator.HeightMapChannel = HeightMapChannel.R;
            
            // R=1.0, G=0.0, B=0.0, A=0.0 のテクスチャを作成
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var colors = new Color[16];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color(1.0f, 0.0f, 0.0f, 0.0f); // R=1.0, 他=0.0
            }
            texture.SetPixels(colors);
            texture.Apply();
            generator.HeightMap = texture;
            
            float[,] heights = HeightMapGenerator.GenerateHeights(generator);
            
            // Rチャンネルが使用されているため、高さ値は1.0に近いはず
            bool hasHighValue = false;
            for (int y = 0; y < heights.GetLength(0) && !hasHighValue; y++)
            {
                for (int x = 0; x < heights.GetLength(1) && !hasHighValue; x++)
                {
                    if (heights[y, x] > 0.9f)
                    {
                        hasHighValue = true;
                    }
                }
            }
            
            Assert.IsTrue(hasHighValue, "Red channel should be used when HeightMapChannel is R");
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void GenerateHeights_HeightMapMode_ChannelG_UsesGreenChannel()
        {
            generator.GenerationMode = TerrainGenerationMode.HeightMap;
            generator.HeightMapChannel = HeightMapChannel.G;
            
            // R=0.0, G=1.0, B=0.0, A=0.0 のテクスチャを作成
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var colors = new Color[16];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color(0.0f, 1.0f, 0.0f, 0.0f); // G=1.0, 他=0.0
            }
            texture.SetPixels(colors);
            texture.Apply();
            generator.HeightMap = texture;
            
            float[,] heights = HeightMapGenerator.GenerateHeights(generator);
            
            // Gチャンネルが使用されているため、高さ値は1.0に近いはず
            bool hasHighValue = false;
            for (int y = 0; y < heights.GetLength(0) && !hasHighValue; y++)
            {
                for (int x = 0; x < heights.GetLength(1) && !hasHighValue; x++)
                {
                    if (heights[y, x] > 0.9f)
                    {
                        hasHighValue = true;
                    }
                }
            }
            
            Assert.IsTrue(hasHighValue, "Green channel should be used when HeightMapChannel is G");
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void GenerateHeights_NoiseMode_SameSeed_ProducesSameResult()
        {
            generator.GenerationMode = TerrainGenerationMode.Noise;
            generator.Seed = 12345;
            
            float[,] heights1 = HeightMapGenerator.GenerateHeights(generator);
            
            // 再生成（同一Seed）
            float[,] heights2 = HeightMapGenerator.GenerateHeights(generator);
            
            // 同一Seedなら同一結果（完全一致を期待）
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
        public void GenerateHeights_NoiseMode_DifferentSeed_ProducesDifferentResult()
        {
            generator.GenerationMode = TerrainGenerationMode.Noise;
            generator.Seed = 11111;
            
            float[,] heights1 = HeightMapGenerator.GenerateHeights(generator);
            
            generator.Seed = 99999;
            float[,] heights2 = HeightMapGenerator.GenerateHeights(generator);
            
            // 異なるSeedなら異なる結果（少なくとも1つの値が異なる）
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
            
            Assert.IsTrue(hasDifference, "Different seeds should produce different height maps");
        }

        [Test]
        public void GenerateHeights_HeightMapMode_UVTiling_AppliesTiling()
        {
            generator.GenerationMode = TerrainGenerationMode.HeightMap;
            generator.UVTiling = new Vector2(2.0f, 2.0f); // 2倍の繰り返し
            
            // 左上のみ白、他は黒のテクスチャ
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var colors = new Color[16];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.black;
            }
            colors[0] = Color.white; // 左上のみ白
            texture.SetPixels(colors);
            texture.Apply();
            generator.HeightMap = texture;
            
            float[,] heights = HeightMapGenerator.GenerateHeights(generator);
            
            // UVTiling=2.0 なので、テクスチャが2x2で繰り返される
            // そのため、複数の位置で高値が現れるはず
            int highValueCount = 0;
            for (int y = 0; y < heights.GetLength(0); y++)
            {
                for (int x = 0; x < heights.GetLength(1); x++)
                {
                    if (heights[y, x] > 0.5f)
                    {
                        highValueCount++;
                    }
                }
            }
            
            // Tiling=2.0 なので、複数箇所で高値が現れるはず（最低でも2箇所以上）
            Assert.Greater(highValueCount, 1, 
                "UVTiling should cause texture to repeat, creating multiple high-value regions");
            
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void GenerateHeights_HeightMapMode_InvertHeight_InvertsHeights()
        {
            generator.GenerationMode = TerrainGenerationMode.HeightMap;
            generator.InvertHeight = false;
            
            // グレースケールグラデーションのテクスチャ
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    float value = (float)(x + y) / 6.0f; // 0.0 ~ 1.0 のグラデーション
                    texture.SetPixel(x, y, new Color(value, value, value));
                }
            }
            texture.Apply();
            generator.HeightMap = texture;
            
            float[,] heightsNormal = HeightMapGenerator.GenerateHeights(generator);
            
            // InvertHeight = true で再生成
            generator.InvertHeight = true;
            float[,] heightsInverted = HeightMapGenerator.GenerateHeights(generator);
            
            // 反転後は、元の高い位置が低く、低い位置が高くなるはず
            // ただし、完全に反転するわけではない（正規化の影響もある）ので、
            // 少なくとも値の分布が変化することを確認
            bool hasDifference = false;
            for (int y = 0; y < heightsNormal.GetLength(0) && !hasDifference; y++)
            {
                for (int x = 0; x < heightsNormal.GetLength(1) && !hasDifference; x++)
                {
                    if (!Mathf.Approximately(heightsNormal[y, x], heightsInverted[y, x]))
                    {
                        hasDifference = true;
                    }
                }
            }
            
            Assert.IsTrue(hasDifference, "InvertHeight should change the height values");
            
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
