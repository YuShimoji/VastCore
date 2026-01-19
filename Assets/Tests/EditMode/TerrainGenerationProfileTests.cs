using NUnit.Framework;
using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// TerrainGenerationProfile のユニットテスト
    /// </summary>
    [TestFixture]
    public class TerrainGenerationProfileTests
    {
        private TerrainGenerationProfile profile;

        [SetUp]
        public void SetUp()
        {
            profile = ScriptableObject.CreateInstance<TerrainGenerationProfile>();
        }

        [TearDown]
        public void TearDown()
        {
            if (profile != null)
            {
                Object.DestroyImmediate(profile);
            }
        }

        #region Default Values Tests

        [Test]
        public void DefaultValues_AreSetFromConstants()
        {
            Assert.AreEqual(TerrainGenerationConstants.DefaultTerrainWidth, profile.TerrainWidth);
            Assert.AreEqual(TerrainGenerationConstants.DefaultTerrainHeight, profile.TerrainLength);
            Assert.AreEqual(TerrainGenerationConstants.DefaultTerrainDepth, profile.TerrainHeight);
            Assert.AreEqual(TerrainGenerationConstants.DefaultHeightmapResolution, profile.HeightmapResolution);
        }

        [Test]
        public void DefaultNoiseSettings_AreSetFromConstants()
        {
            Assert.AreEqual(TerrainGenerationConstants.DefaultNoiseScale, profile.NoiseScale);
            Assert.AreEqual(TerrainGenerationConstants.DefaultOctaves, profile.Octaves);
            Assert.AreEqual(TerrainGenerationConstants.DefaultPersistence, profile.Persistence);
            Assert.AreEqual(TerrainGenerationConstants.DefaultLacunarity, profile.Lacunarity);
        }

        [Test]
        public void DefaultGenerationMode_IsNoise()
        {
            Assert.AreEqual(TerrainGenerationMode.Noise, profile.GenerationMode);
        }

        #endregion

        #region Validation Tests

        [Test]
        public void TerrainWidth_CannotBeZeroOrNegative()
        {
            profile.TerrainWidth = 0f;
            Assert.GreaterOrEqual(profile.TerrainWidth, 1f);

            profile.TerrainWidth = -100f;
            Assert.GreaterOrEqual(profile.TerrainWidth, 1f);
        }

        [Test]
        public void TerrainLength_CannotBeZeroOrNegative()
        {
            profile.TerrainLength = 0f;
            Assert.GreaterOrEqual(profile.TerrainLength, 1f);

            profile.TerrainLength = -100f;
            Assert.GreaterOrEqual(profile.TerrainLength, 1f);
        }

        [Test]
        public void TerrainHeight_CannotBeZeroOrNegative()
        {
            profile.TerrainHeight = 0f;
            Assert.GreaterOrEqual(profile.TerrainHeight, 1f);

            profile.TerrainHeight = -100f;
            Assert.GreaterOrEqual(profile.TerrainHeight, 1f);
        }

        [Test]
        public void HeightScale_IsClampedToRange()
        {
            profile.HeightScale = -1f;
            Assert.GreaterOrEqual(profile.HeightScale, 0f);

            profile.HeightScale = 10f;
            Assert.LessOrEqual(profile.HeightScale, 5f);
        }

        [Test]
        public void HeightmapResolution_IsValidated()
        {
            // 有効な解像度に設定
            profile.HeightmapResolution = 513;
            Assert.AreEqual(513, profile.HeightmapResolution);

            // 無効な解像度は最寄りの有効値に補正される
            profile.HeightmapResolution = 100;
            Assert.IsTrue(IsValidHeightmapResolution(profile.HeightmapResolution));
        }

        #endregion

        #region Property Setter Tests

        [Test]
        public void GenerationMode_CanBeChanged()
        {
            profile.GenerationMode = TerrainGenerationMode.HeightMap;
            Assert.AreEqual(TerrainGenerationMode.HeightMap, profile.GenerationMode);

            profile.GenerationMode = TerrainGenerationMode.NoiseAndHeightMap;
            Assert.AreEqual(TerrainGenerationMode.NoiseAndHeightMap, profile.GenerationMode);
        }

        [Test]
        public void NoiseOffset_CanBeSet()
        {
            var offset = new Vector2(100f, 200f);
            profile.NoiseOffset = offset;
            Assert.AreEqual(offset, profile.NoiseOffset);
        }

        [Test]
        public void HeightMapTexture_CanBeSetToNull()
        {
            profile.HeightMapTexture = null;
            Assert.IsNull(profile.HeightMapTexture);
        }

        #endregion

        #region Helper Methods

        private bool IsValidHeightmapResolution(int resolution)
        {
            // 有効な解像度: 33, 65, 129, 257, 513, 1025, 2049, 4097
            int[] validResolutions = { 33, 65, 129, 257, 513, 1025, 2049, 4097 };
            foreach (int valid in validResolutions)
            {
                if (resolution == valid) return true;
            }
            return false;
        }

        #endregion
    }
}
