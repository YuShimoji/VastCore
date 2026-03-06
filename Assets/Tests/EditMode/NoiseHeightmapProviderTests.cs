using NUnit.Framework;
using UnityEngine;
using Vastcore.Terrain.Providers;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// NoiseHeightmapProvider のテストスイート
    /// FBMノイズ生成、シード決定論、パラメータバリデーションをテスト
    /// </summary>
    [TestFixture]
    public class NoiseHeightmapProviderTests
    {
        private const int DefaultSeed = 42;
        private const float DefaultScale = 100f;
        private const int DefaultOctaves = 4;
        private const float DefaultLacunarity = 2f;
        private const float DefaultGain = 0.5f;

        private NoiseHeightmapProvider CreateDefaultProvider(
            int seed = DefaultSeed,
            float scale = DefaultScale,
            int octaves = DefaultOctaves,
            float lacunarity = DefaultLacunarity,
            float gain = DefaultGain,
            bool domainWarp = false)
        {
            return new NoiseHeightmapProvider(
                seed, scale, octaves, lacunarity, gain,
                Vector2.zero, domainWarp);
        }

        #region Constructor Validation Tests

        [Test]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var provider = CreateDefaultProvider();

            // Assert
            Assert.IsNotNull(provider, "Provider should be created with valid parameters");
        }

        [Test]
        public void Constructor_WithNegativeScale_ClampsToMinimum()
        {
            // Arrange - negative scale should be clamped internally
            var provider = new NoiseHeightmapProvider(
                DefaultSeed, -1f, DefaultOctaves, DefaultLacunarity, DefaultGain, Vector2.zero);

            // Act - should not throw, should produce valid output
            int resolution = 4;
            float[] heights = new float[resolution * resolution];
            var context = new HeightmapGenerationContext { Seed = DefaultSeed };

            Assert.DoesNotThrow(() =>
                provider.Generate(heights, resolution, Vector2.zero, 256f, context),
                "Provider with clamped scale should still generate valid heights");
        }

        [Test]
        public void Constructor_WithZeroOctaves_ClampsToOne()
        {
            // Arrange - zero octaves should be clamped to 1
            var provider = new NoiseHeightmapProvider(
                DefaultSeed, DefaultScale, 0, DefaultLacunarity, DefaultGain, Vector2.zero);

            // Act
            int resolution = 4;
            float[] heights = new float[resolution * resolution];
            var context = new HeightmapGenerationContext { Seed = DefaultSeed };

            Assert.DoesNotThrow(() =>
                provider.Generate(heights, resolution, Vector2.zero, 256f, context),
                "Provider with clamped octaves should still generate valid heights");
        }

        #endregion

        #region Generate Output Validation Tests

        [Test]
        public void Generate_WithValidInput_ProducesHeightsInZeroOneRange()
        {
            // Arrange
            var provider = CreateDefaultProvider();
            int resolution = 16;
            float[] heights = new float[resolution * resolution];
            var context = new HeightmapGenerationContext { Seed = DefaultSeed };

            // Act
            provider.Generate(heights, resolution, Vector2.zero, 256f, context);

            // Assert - all values should be in [0, 1]
            for (int i = 0; i < heights.Length; i++)
            {
                Assert.GreaterOrEqual(heights[i], 0f, $"Height at index {i} should be >= 0");
                Assert.LessOrEqual(heights[i], 1f, $"Height at index {i} should be <= 1");
            }
        }

        [Test]
        public void Generate_WithValidInput_ProducesNonUniformValues()
        {
            // Arrange
            var provider = CreateDefaultProvider();
            int resolution = 16;
            float[] heights = new float[resolution * resolution];
            var context = new HeightmapGenerationContext { Seed = DefaultSeed };

            // Act
            provider.Generate(heights, resolution, Vector2.zero, 256f, context);

            // Assert - values should not all be the same
            float firstValue = heights[0];
            bool hasDifferentValue = false;
            for (int i = 1; i < heights.Length; i++)
            {
                if (!Mathf.Approximately(heights[i], firstValue))
                {
                    hasDifferentValue = true;
                    break;
                }
            }
            Assert.IsTrue(hasDifferentValue, "Generated heights should contain varying values");
        }

        [Test]
        public void Generate_WithMinimumResolution_ProducesValidOutput()
        {
            // Arrange - resolution=2 is the minimum valid (resolution-1 used as divisor)
            var provider = CreateDefaultProvider();
            int resolution = 2;
            float[] heights = new float[resolution * resolution];
            var context = new HeightmapGenerationContext { Seed = DefaultSeed };

            // Act
            provider.Generate(heights, resolution, Vector2.zero, 256f, context);

            // Assert
            for (int i = 0; i < heights.Length; i++)
            {
                Assert.GreaterOrEqual(heights[i], 0f, $"Height at {i} should be >= 0");
                Assert.LessOrEqual(heights[i], 1f, $"Height at {i} should be <= 1");
            }
        }

        #endregion

        #region Seed Determinism Tests

        [Test]
        public void Generate_WithSameSeed_ProducesIdenticalOutput()
        {
            // Arrange
            int resolution = 8;
            float[] heights1 = new float[resolution * resolution];
            float[] heights2 = new float[resolution * resolution];
            var context = new HeightmapGenerationContext { Seed = DefaultSeed };

            var provider1 = CreateDefaultProvider(seed: 12345);
            var provider2 = CreateDefaultProvider(seed: 12345);

            // Act
            provider1.Generate(heights1, resolution, Vector2.zero, 256f, context);
            provider2.Generate(heights2, resolution, Vector2.zero, 256f, context);

            // Assert
            for (int i = 0; i < heights1.Length; i++)
            {
                Assert.AreEqual(heights1[i], heights2[i], 1e-6f,
                    $"Heights at index {i} should be identical for same seed");
            }
        }

        [Test]
        public void Generate_WithDifferentSeeds_ProducesDifferentOutput()
        {
            // Arrange
            int resolution = 8;
            float[] heights1 = new float[resolution * resolution];
            float[] heights2 = new float[resolution * resolution];
            var context = new HeightmapGenerationContext { Seed = DefaultSeed };

            var provider1 = CreateDefaultProvider(seed: 100);
            var provider2 = CreateDefaultProvider(seed: 999);

            // Act
            provider1.Generate(heights1, resolution, Vector2.zero, 256f, context);
            provider2.Generate(heights2, resolution, Vector2.zero, 256f, context);

            // Assert - at least some values should differ
            bool hasDifference = false;
            for (int i = 0; i < heights1.Length; i++)
            {
                if (!Mathf.Approximately(heights1[i], heights2[i]))
                {
                    hasDifference = true;
                    break;
                }
            }
            Assert.IsTrue(hasDifference, "Different seeds should produce different height values");
        }

        #endregion

        #region World Origin / Size Tests

        [Test]
        public void Generate_WithDifferentWorldOrigins_ProducesDifferentOutput()
        {
            // Arrange
            var provider = CreateDefaultProvider();
            int resolution = 8;
            float[] heights1 = new float[resolution * resolution];
            float[] heights2 = new float[resolution * resolution];
            var context = new HeightmapGenerationContext { Seed = DefaultSeed };

            // Act
            provider.Generate(heights1, resolution, Vector2.zero, 256f, context);
            provider.Generate(heights2, resolution, new Vector2(1000f, 1000f), 256f, context);

            // Assert
            bool hasDifference = false;
            for (int i = 0; i < heights1.Length; i++)
            {
                if (!Mathf.Approximately(heights1[i], heights2[i]))
                {
                    hasDifference = true;
                    break;
                }
            }
            Assert.IsTrue(hasDifference, "Different world origins should produce different heights");
        }

        [Test]
        public void Generate_WithSameOriginAndSize_IsDeterministic()
        {
            // Arrange
            var provider = CreateDefaultProvider();
            int resolution = 8;
            float[] heights1 = new float[resolution * resolution];
            float[] heights2 = new float[resolution * resolution];
            var context = new HeightmapGenerationContext { Seed = DefaultSeed };
            Vector2 origin = new Vector2(500f, 300f);

            // Act
            provider.Generate(heights1, resolution, origin, 256f, context);
            provider.Generate(heights2, resolution, origin, 256f, context);

            // Assert
            for (int i = 0; i < heights1.Length; i++)
            {
                Assert.AreEqual(heights1[i], heights2[i], 1e-6f,
                    $"Same origin should produce identical results at index {i}");
            }
        }

        #endregion

        #region Domain Warp Tests

        [Test]
        public void Generate_WithDomainWarp_ProducesDifferentOutputThanWithout()
        {
            // Arrange
            int resolution = 8;
            float[] heightsNoWarp = new float[resolution * resolution];
            float[] heightsWarp = new float[resolution * resolution];
            var context = new HeightmapGenerationContext { Seed = DefaultSeed };

            var providerNoWarp = CreateDefaultProvider(domainWarp: false);
            var providerWarp = new NoiseHeightmapProvider(
                DefaultSeed, DefaultScale, DefaultOctaves, DefaultLacunarity, DefaultGain,
                Vector2.zero, domainWarp: true, warpStrength: 50f, warpFrequency: 0.05f);

            // Act
            providerNoWarp.Generate(heightsNoWarp, resolution, Vector2.zero, 256f, context);
            providerWarp.Generate(heightsWarp, resolution, Vector2.zero, 256f, context);

            // Assert - domain warp should change the output
            bool hasDifference = false;
            for (int i = 0; i < heightsNoWarp.Length; i++)
            {
                if (!Mathf.Approximately(heightsNoWarp[i], heightsWarp[i]))
                {
                    hasDifference = true;
                    break;
                }
            }
            Assert.IsTrue(hasDifference, "Domain warp should produce different heights");
        }

        [Test]
        public void Generate_WithDomainWarp_StillProducesValidRange()
        {
            // Arrange
            var provider = new NoiseHeightmapProvider(
                DefaultSeed, DefaultScale, DefaultOctaves, DefaultLacunarity, DefaultGain,
                Vector2.zero, domainWarp: true, warpStrength: 100f, warpFrequency: 0.1f);
            int resolution = 16;
            float[] heights = new float[resolution * resolution];
            var context = new HeightmapGenerationContext { Seed = DefaultSeed };

            // Act
            provider.Generate(heights, resolution, Vector2.zero, 256f, context);

            // Assert
            for (int i = 0; i < heights.Length; i++)
            {
                Assert.GreaterOrEqual(heights[i], 0f, $"Warped height at {i} should be >= 0");
                Assert.LessOrEqual(heights[i], 1f, $"Warped height at {i} should be <= 1");
            }
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void Generate_WithNullArray_ThrowsArgumentException()
        {
            // Arrange
            var provider = CreateDefaultProvider();
            var context = new HeightmapGenerationContext { Seed = DefaultSeed };

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() =>
                provider.Generate(null, 4, Vector2.zero, 256f, context),
                "Null heights array should throw ArgumentException");
        }

        [Test]
        public void Generate_WithWrongArrayLength_ThrowsArgumentException()
        {
            // Arrange
            var provider = CreateDefaultProvider();
            int resolution = 4;
            float[] wrongSizeArray = new float[resolution * resolution + 1]; // Wrong size
            var context = new HeightmapGenerationContext { Seed = DefaultSeed };

            // Act & Assert
            Assert.Throws<System.ArgumentException>(() =>
                provider.Generate(wrongSizeArray, resolution, Vector2.zero, 256f, context),
                "Wrong array length should throw ArgumentException");
        }

        #endregion
    }
}
