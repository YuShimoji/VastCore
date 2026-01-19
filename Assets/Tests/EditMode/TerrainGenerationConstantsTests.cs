using NUnit.Framework;
using Vastcore.Generation;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// TerrainGenerationConstants のユニットテスト
    /// 定数値が期待通りに設定されていることを検証
    /// </summary>
    [TestFixture]
    public class TerrainGenerationConstantsTests
    {
        #region Terrain Size Constants

        [Test]
        public void DefaultTerrainWidth_IsPositive()
        {
            Assert.Greater(TerrainGenerationConstants.DefaultTerrainWidth, 0);
        }

        [Test]
        public void DefaultTerrainHeight_IsPositive()
        {
            Assert.Greater(TerrainGenerationConstants.DefaultTerrainHeight, 0);
        }

        [Test]
        public void DefaultTerrainDepth_IsPositive()
        {
            Assert.Greater(TerrainGenerationConstants.DefaultTerrainDepth, 0);
        }

        [Test]
        public void DefaultHeightmapResolution_IsValidPowerOfTwoPlusOne()
        {
            // 有効な解像度は 2^n + 1 形式
            int resolution = TerrainGenerationConstants.DefaultHeightmapResolution;
            int check = resolution - 1;
            Assert.IsTrue(IsPowerOfTwo(check), $"Resolution {resolution} should be 2^n + 1");
        }

        #endregion

        #region Noise Generation Constants

        [Test]
        public void DefaultNoiseScale_IsPositive()
        {
            Assert.Greater(TerrainGenerationConstants.DefaultNoiseScale, 0f);
        }

        [Test]
        public void DefaultOctaves_IsInValidRange()
        {
            Assert.GreaterOrEqual(TerrainGenerationConstants.DefaultOctaves, 1);
            Assert.LessOrEqual(TerrainGenerationConstants.DefaultOctaves, 16);
        }

        [Test]
        public void DefaultPersistence_IsInValidRange()
        {
            Assert.GreaterOrEqual(TerrainGenerationConstants.DefaultPersistence, 0f);
            Assert.LessOrEqual(TerrainGenerationConstants.DefaultPersistence, 1f);
        }

        [Test]
        public void DefaultLacunarity_IsGreaterThanOne()
        {
            Assert.Greater(TerrainGenerationConstants.DefaultLacunarity, 1f);
        }

        #endregion

        #region HeightMap Constants

        [Test]
        public void DefaultHeightMapScale_IsPositive()
        {
            Assert.Greater(TerrainGenerationConstants.DefaultHeightMapScale, 0f);
        }

        [Test]
        public void HeightNormalizationFactor_IsInValidRange()
        {
            Assert.GreaterOrEqual(TerrainGenerationConstants.HeightNormalizationFactor, 0f);
            Assert.LessOrEqual(TerrainGenerationConstants.HeightNormalizationFactor, 1f);
        }

        [Test]
        public void MaxNoiseInfluence_IsGreaterThanMinNoiseInfluence()
        {
            Assert.Greater(
                TerrainGenerationConstants.MaxNoiseInfluence,
                TerrainGenerationConstants.MinNoiseInfluence
            );
        }

        [Test]
        public void GradientMultiplier_IsPositive()
        {
            Assert.Greater(TerrainGenerationConstants.GradientMultiplier, 0f);
        }

        [Test]
        public void GradientSampleRadius_IsPositive()
        {
            Assert.Greater(TerrainGenerationConstants.GradientSampleRadius, 0);
        }

        #endregion

        #region Batch Processing Constants

        [Test]
        public void HeightmapBatchSize_IsPositive()
        {
            Assert.Greater(TerrainGenerationConstants.HeightmapBatchSize, 0);
        }

        [Test]
        public void HeightmapBatchSize_IsPowerOfTwo()
        {
            Assert.IsTrue(
                IsPowerOfTwo(TerrainGenerationConstants.HeightmapBatchSize),
                "Batch size should be power of two for optimal performance"
            );
        }

        #endregion

        #region Layer Names

        [Test]
        public void TerrainLayerName_IsNotEmpty()
        {
            Assert.IsFalse(string.IsNullOrEmpty(TerrainGenerationConstants.TerrainLayerName));
        }

        #endregion

        #region Primitive Generation Constants

        [Test]
        public void DefaultPrimitiveScale_IsPositive()
        {
            Assert.Greater(TerrainGenerationConstants.DefaultPrimitiveScale, 0f);
        }

        [Test]
        public void DefaultSubdivisionLevel_IsNonNegative()
        {
            Assert.GreaterOrEqual(TerrainGenerationConstants.DefaultSubdivisionLevel, 0);
        }

        #endregion

        #region Helper Methods

        private bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }

        #endregion
    }
}
