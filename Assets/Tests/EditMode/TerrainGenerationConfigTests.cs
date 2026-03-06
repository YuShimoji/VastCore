using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Vastcore.Terrain.Config;
using Vastcore.Terrain.Providers;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// TerrainGenerationConfig のテストスイート
    /// ScriptableObject生成、デフォルト値、HeightProvider生成をテスト
    /// </summary>
    [TestFixture]
    public class TerrainGenerationConfigTests
    {
        private TerrainGenerationConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<TerrainGenerationConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            if (config != null)
            {
                Object.DestroyImmediate(config);
            }
        }

        #region Default Value Tests

        [Test]
        public void Config_DefaultResolution_Is257()
        {
            Assert.AreEqual(257, config.resolution,
                "Default resolution should be 257 (2^8+1 for Unity Terrain)");
        }

        [Test]
        public void Config_DefaultWorldSize_Is256()
        {
            Assert.AreEqual(256f, config.worldSize,
                "Default worldSize should be 256 meters");
        }

        [Test]
        public void Config_DefaultHeightScale_Is100()
        {
            Assert.AreEqual(100f, config.heightScale,
                "Default heightScale should be 100 meters");
        }

        [Test]
        public void Config_DefaultHeightmapSettings_IsNull()
        {
            Assert.IsNull(config.heightmapSettings,
                "Default heightmapSettings should be null (not assigned)");
        }

        #endregion

        #region CreateHeightProvider Tests

        [Test]
        public void CreateHeightProvider_WithNullSettings_ReturnsNull()
        {
            // Arrange
            config.heightmapSettings = null;

            // Expect LogError
            LogAssert.Expect(LogType.Error, "TerrainGenerationConfig.heightmapSettings is null");

            // Act
            IHeightmapProvider provider = config.CreateHeightProvider();

            // Assert
            Assert.IsNull(provider, "Should return null when heightmapSettings is null");
        }

        [Test]
        public void CreateHeightProvider_WithValidSettings_ReturnsProvider()
        {
            // Arrange - create a concrete HeightmapProviderSettings
            var noiseSettings = ScriptableObject.CreateInstance<NoiseHeightmapSettings>();
            config.heightmapSettings = noiseSettings;

            // Act
            IHeightmapProvider provider = config.CreateHeightProvider();

            // Assert
            Assert.IsNotNull(provider, "Should return a provider when settings are valid");
            Assert.IsInstanceOf<NoiseHeightmapProvider>(provider,
                "Provider should be NoiseHeightmapProvider for NoiseHeightmapSettings");

            // Cleanup
            Object.DestroyImmediate(noiseSettings);
        }

        #endregion

        #region ScriptableObject Tests

        [Test]
        public void Config_CanBeCreatedAsScriptableObject()
        {
            // Assert
            Assert.IsNotNull(config, "Config should be creatable via CreateInstance");
            Assert.IsInstanceOf<ScriptableObject>(config, "Config should be a ScriptableObject");
        }

        [Test]
        public void Config_FieldsAreModifiable()
        {
            // Act
            config.resolution = 513;
            config.worldSize = 512f;
            config.heightScale = 200f;

            // Assert
            Assert.AreEqual(513, config.resolution, "Resolution should be modifiable");
            Assert.AreEqual(512f, config.worldSize, "WorldSize should be modifiable");
            Assert.AreEqual(200f, config.heightScale, "HeightScale should be modifiable");
        }

        #endregion
    }
}
