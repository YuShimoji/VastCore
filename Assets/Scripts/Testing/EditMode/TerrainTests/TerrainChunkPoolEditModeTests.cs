using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Vastcore.Terrain;
using Vastcore.Terrain.Config;

namespace Vastcore.Testing.EditMode.TerrainTests
{
    public class TerrainChunkPoolEditModeTests
    {
        [TearDown]
        public void TearDown()
        {
            foreach (var pool in Object.FindObjectsByType<TerrainChunkPool>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(pool.gameObject);
            }

            foreach (var chunk in Object.FindObjectsByType<TerrainChunk>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(chunk.gameObject);
            }
        }

        [Test]
        public void Acquire_WithoutInitialize_ReturnsNull()
        {
            var root = new GameObject("PoolRoot");
            var pool = root.AddComponent<TerrainChunkPool>();

            LogAssert.Expect(LogType.Error, "TerrainChunkPool.Acquire called without config/provider");
            var chunk = pool.Acquire(Vector2.zero);

            Assert.IsNull(chunk);
            Assert.AreEqual(0, pool.ActiveCount);
        }

        [Test]
        public void AcquireAndRelease_WithValidConfig_ReusesChunk()
        {
            var root = new GameObject("PoolRoot");
            var pool = root.AddComponent<TerrainChunkPool>();

            var config = ScriptableObject.CreateInstance<TerrainGenerationConfig>();
            config.heightmapSettings = ScriptableObject.CreateInstance<NoiseHeightmapSettings>();
            config.resolution = 17;
            config.worldSize = 32f;
            config.heightScale = 8f;

            pool.Initialize(config);

            var first = pool.Acquire(Vector2.zero);
            Assert.IsNotNull(first);
            Assert.AreEqual(1, pool.ActiveCount);
            Assert.AreEqual(1, pool.TotalCreated);

            pool.Release(first);
            Assert.AreEqual(0, pool.ActiveCount);

            var second = pool.Acquire(new Vector2(32f, 0f));
            Assert.IsNotNull(second);
            Assert.AreSame(first, second);
            Assert.AreEqual(1, pool.ActiveCount);
            Assert.AreEqual(1, pool.TotalCreated);

            Object.DestroyImmediate(config.heightmapSettings);
            Object.DestroyImmediate(config);
        }
    }
}
