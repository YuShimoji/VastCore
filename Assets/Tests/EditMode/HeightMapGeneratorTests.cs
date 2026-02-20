using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Vastcore.Generation;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// HeightMapGenerator 縺ｮ繝ｦ繝九ャ繝医ユ繧ｹ繝・
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
            
            // 蟆上＆縺・ｧ｣蜒丞ｺｦ縺ｧ繝・せ繝茨ｼ医ヱ繝輔か繝ｼ繝槭Φ繧ｹ閠・・・・
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
            generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.Noise;
            
            float[,] heights = HeightMapGenerator.GenerateHeights(generator);
            
            Assert.AreEqual(generator.Resolution, heights.GetLength(0));
            Assert.AreEqual(generator.Resolution, heights.GetLength(1));
        }

        [Test]
        public void GenerateHeights_NoiseMode_ValuesAreNormalized()
        {
            generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.Noise;
            
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
            generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.Noise;
            generator.Offset = Vector2.zero;
            float[,] heights1 = HeightMapGenerator.GenerateHeights(generator);
            
            generator.Offset = new Vector2(1000f, 1000f);
            float[,] heights2 = HeightMapGenerator.GenerateHeights(generator);
            
            // 蟆代↑縺上→繧・縺､縺ｮ蛟､縺檎焚縺ｪ繧九％縺ｨ繧堤｢ｺ隱・
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
            generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.HeightMap;
            generator.HeightMap = null;
            
            // HeightMap 縺・null 縺ｮ蝣ｴ蜷医．ebug.LogError 縺悟・蜉帙＆繧後ｋ縺薙→繧呈悄蠕・
            LogAssert.Expect(LogType.Error, "[TerrainGenerator] Height map is not assigned!");
            
            float[,] heights = HeightMapGenerator.GenerateHeights(generator);
            
            // 繝・け繧ｹ繝√Ε縺後↑縺・ｴ蜷医√☆縺ｹ縺ｦ0縺ｮ縺ｯ縺・
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
            generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.HeightMap;
            
            // 繧ｷ繝ｳ繝励Ν縺ｪ繝・せ繝医ユ繧ｯ繧ｹ繝√Ε繧剃ｽ懈・
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
            generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.NoiseAndHeightMap;
            
            // Combined 繝｢繝ｼ繝峨〒縺ｯ HeightMap 縺悟ｿ・ｦ√↑縺ｮ縺ｧ繝繝溘・繝・け繧ｹ繝√Ε繧定ｨｭ螳・
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
            generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.NoiseAndHeightMap;
            
            // 繝・せ繝医ユ繧ｯ繧ｹ繝√Ε繧剃ｽ懈・
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
            generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.HeightMap;
            generator.HeightMapChannel = HeightMapChannel.R;
            
            // R=1.0, G=0.0, B=0.0, A=0.0 縺ｮ繝・け繧ｹ繝√Ε繧剃ｽ懈・
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var colors = new Color[16];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color(1.0f, 0.0f, 0.0f, 0.0f); // R=1.0, 莉・0.0
            }
            texture.SetPixels(colors);
            texture.Apply();
            generator.HeightMap = texture;
            
            float[,] heights = HeightMapGenerator.GenerateHeights(generator);
            
            // R繝√Ε繝ｳ繝阪Ν縺御ｽｿ逕ｨ縺輔ｌ縺ｦ縺・ｋ縺溘ａ縲・ｫ倥＆蛟､縺ｯ1.0縺ｫ霑代＞縺ｯ縺・
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
            generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.HeightMap;
            generator.HeightMapChannel = HeightMapChannel.G;
            
            // R=0.0, G=1.0, B=0.0, A=0.0 縺ｮ繝・け繧ｹ繝√Ε繧剃ｽ懈・
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var colors = new Color[16];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color(0.0f, 1.0f, 0.0f, 0.0f); // G=1.0, 莉・0.0
            }
            texture.SetPixels(colors);
            texture.Apply();
            generator.HeightMap = texture;
            
            float[,] heights = HeightMapGenerator.GenerateHeights(generator);
            
            // G繝√Ε繝ｳ繝阪Ν縺御ｽｿ逕ｨ縺輔ｌ縺ｦ縺・ｋ縺溘ａ縲・ｫ倥＆蛟､縺ｯ1.0縺ｫ霑代＞縺ｯ縺・
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
            generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.Noise;
            generator.Seed = 12345;
            
            float[,] heights1 = HeightMapGenerator.GenerateHeights(generator);
            
            // 蜀咲函謌撰ｼ亥酔荳Seed・・
            float[,] heights2 = HeightMapGenerator.GenerateHeights(generator);
            
            // 蜷御ｸSeed縺ｪ繧牙酔荳邨先棡・亥ｮ悟・荳閾ｴ繧呈悄蠕・ｼ・
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
            generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.Noise;
            generator.Seed = 11111;
            
            float[,] heights1 = HeightMapGenerator.GenerateHeights(generator);
            
            generator.Seed = 99999;
            float[,] heights2 = HeightMapGenerator.GenerateHeights(generator);
            
            // 逡ｰ縺ｪ繧鬼eed縺ｪ繧臥焚縺ｪ繧狗ｵ先棡・亥ｰ代↑縺上→繧・縺､縺ｮ蛟､縺檎焚縺ｪ繧具ｼ・
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
            generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.HeightMap;
            generator.UVTiling = new Vector2(2.0f, 2.0f); // 2蛟阪・郢ｰ繧願ｿ斐＠
            
            // 蟾ｦ荳翫・縺ｿ逋ｽ縲∽ｻ悶・鮟偵・繝・け繧ｹ繝√Ε
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var colors = new Color[16];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.black;
            }
            colors[0] = Color.white; // 蟾ｦ荳翫・縺ｿ逋ｽ
            texture.SetPixels(colors);
            texture.Apply();
            generator.HeightMap = texture;
            
            float[,] heights = HeightMapGenerator.GenerateHeights(generator);
            
            // UVTiling=2.0 縺ｪ縺ｮ縺ｧ縲√ユ繧ｯ繧ｹ繝√Ε縺・x2縺ｧ郢ｰ繧願ｿ斐＆繧後ｋ
            // 縺昴・縺溘ａ縲∬､・焚縺ｮ菴咲ｽｮ縺ｧ鬮伜､縺檎樟繧後ｋ縺ｯ縺・
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
            
            // Tiling=2.0 縺ｪ縺ｮ縺ｧ縲∬､・焚邂・園縺ｧ鬮伜､縺檎樟繧後ｋ縺ｯ縺夲ｼ域怙菴弱〒繧・邂・園莉･荳奇ｼ・
            Assert.Greater(highValueCount, 1, 
                "UVTiling should cause texture to repeat, creating multiple high-value regions");
            
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void GenerateHeights_HeightMapMode_InvertHeight_InvertsHeights()
        {
            generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.HeightMap;
            generator.InvertHeight = false;
            
            // 繧ｰ繝ｬ繝ｼ繧ｹ繧ｱ繝ｼ繝ｫ繧ｰ繝ｩ繝・・繧ｷ繝ｧ繝ｳ縺ｮ繝・け繧ｹ繝√Ε
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    float value = (float)(x + y) / 6.0f; // 0.0 ~ 1.0 縺ｮ繧ｰ繝ｩ繝・・繧ｷ繝ｧ繝ｳ
                    texture.SetPixel(x, y, new Color(value, value, value));
                }
            }
            texture.Apply();
            generator.HeightMap = texture;
            
            float[,] heightsNormal = HeightMapGenerator.GenerateHeights(generator);
            
            // InvertHeight = true 縺ｧ蜀咲函謌・
            generator.InvertHeight = true;
            float[,] heightsInverted = HeightMapGenerator.GenerateHeights(generator);
            
            // 蜿崎ｻ｢蠕後・縲∝・縺ｮ鬮倥＞菴咲ｽｮ縺御ｽ弱￥縲∽ｽ弱＞菴咲ｽｮ縺碁ｫ倥￥縺ｪ繧九・縺・
            // 縺溘□縺励∝ｮ悟・縺ｫ蜿崎ｻ｢縺吶ｋ繧上￠縺ｧ縺ｯ縺ｪ縺・ｼ域ｭ｣隕丞喧縺ｮ蠖ｱ髻ｿ繧ゅ≠繧具ｼ峨・縺ｧ縲・
            // 蟆代↑縺上→繧ょ､縺ｮ蛻・ｸ・′螟牙喧縺吶ｋ縺薙→繧堤｢ｺ隱・
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
            generator.GenerationMode = TerrainGenerator.TerrainGenerationMode.Noise;
            generator.Resolution = 33;
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            HeightMapGenerator.GenerateHeights(generator);
            stopwatch.Stop();
            
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "Small resolution should complete in under 1 second");
        }

        #endregion
    }
}

