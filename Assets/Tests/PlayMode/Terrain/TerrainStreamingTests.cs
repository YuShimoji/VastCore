using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vastcore.Terrain;
using Vastcore.Terrain.Config;
using Vastcore.Terrain.Providers;

namespace Vastcore.Tests.PlayMode.Terrain
{
    public class TerrainStreamingTests
    {
        private const int DefaultRadius = 1;

        private static TerrainGenerationConfig CreateConfig(float constantHeight = 0.35f)
        {
            var config = ScriptableObject.CreateInstance<TerrainGenerationConfig>();
            var settings = ScriptableObject.CreateInstance<ConstantHeightmapSettings>();
            settings.constantValue = constantHeight;
            config.heightmapSettings = settings;
            config.resolution = 33;
            config.worldSize = 64f;
            config.heightScale = 40f;
            return config;
        }

        private static TerrainStreamingController CreateController(int radius = DefaultRadius)
        {
            var go = new GameObject("TerrainStreamingController_Test");
            var controller = go.AddComponent<TerrainStreamingController>();
            controller.config = CreateConfig();
            controller.loadRadius = radius;
            controller.target = go.transform; // 自身を基準に移動検証
            controller.updateThreshold = 0.1f; // テストで容易に更新
            controller.maxLoadPerFrame = 0;
            controller.worldOrigin = Vector2.zero;
            controller.Initialize();
            return controller;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var controller in Object.FindObjectsOfType<TerrainStreamingController>())
            {
                controller.ClearAll();
                Object.DestroyImmediate(controller.gameObject);
            }
        }

        [Test]
        public void StreamingLoadsInitialRadius()
        {
            var controller = CreateController();
            int expected = Mathf.Pow((DefaultRadius * 2) + 1, 2);
            Assert.AreEqual(expected, controller.ActiveChunks.Count, "Initial active chunk count mismatch");
            Assert.AreEqual(expected, controller.Pool.ActiveCount, "Pool active count mismatch");
            Assert.AreEqual(expected, controller.Pool.TotalCreated, "Unexpected number of created chunks");
        }

        [Test]
        public void StreamingReactsToMovement()
        {
            var controller = CreateController();
            float moveDist = controller.config.worldSize * 1.1f;
            controller.UpdateStreaming(new Vector3(moveDist, 0f, 0f));

            Assert.AreEqual(new Vector2Int(1, 0), controller.CurrentCenter, "Center should shift by one chunk on X axis");
            int expected = Mathf.Pow((DefaultRadius * 2) + 1, 2);
            Assert.AreEqual(expected, controller.ActiveChunks.Count, "Active chunk count should remain constant after move");
        }

        [Test]
        public void PoolReusesChunksWhenMovingBackAndForth()
        {
            var controller = CreateController();
            int expected = Mathf.Pow((DefaultRadius * 2) + 1, 2);
            int initialCreated = controller.Pool.TotalCreated;
            Assert.AreEqual(expected, initialCreated, "Initial created chunks should match required ring");

            float size = controller.config.worldSize;
            controller.UpdateStreaming(new Vector3(size * 1.2f, 0f, 0f)); // move +X
            controller.UpdateStreaming(new Vector3(0f, 0f, size * 1.2f)); // move +Z
            controller.UpdateStreaming(Vector3.zero); // back to origin

            Assert.AreEqual(expected, controller.ActiveChunks.Count, "Active chunk count should remain constant after moves");
            Assert.AreEqual(initialCreated, controller.Pool.TotalCreated, "Pool should reuse chunks instead of creating new ones");
        }

        #region Helpers

        private sealed class ConstantHeightmapSettings : HeightmapProviderSettings
        {
            public float constantValue = 0.5f;

            public override IHeightmapProvider CreateProvider()
            {
                return new ConstantHeightmapProvider(Mathf.Clamp01(constantValue));
            }
        }

        private sealed class ConstantHeightmapProvider : IHeightmapProvider
        {
            private readonly float _value;

            public ConstantHeightmapProvider(float value)
            {
                _value = Mathf.Clamp01(value);
            }

            public void Generate(float[] heights, int resolution, Vector2 worldOrigin, float worldSize, in HeightmapGenerationContext context)
            {
                if (heights == null || heights.Length != resolution * resolution)
                {
                    throw new System.ArgumentException("Invalid height array");
                }

                for (int i = 0; i < heights.Length; i++)
                {
                    heights[i] = _value;
                }
            }
        }

        #endregion
    }
}
