using NUnit.Framework;
using Vastcore.Terrain.Erosion;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// HydraulicErosion / ThermalErosion のテストスイート
    /// </summary>
    [TestFixture]
    public class ErosionTests
    {
        #region Helper Methods

        /// <summary>
        /// 平坦なハイトマップを生成
        /// </summary>
        private float[,] CreateFlatHeightmap(int _size, float _height)
        {
            float[,] map = new float[_size, _size];
            for (int x = 0; x < _size; x++)
                for (int y = 0; y < _size; y++)
                    map[x, y] = _height;
            return map;
        }

        /// <summary>
        /// 中心にピークがあるハイトマップを生成
        /// </summary>
        private float[,] CreatePeakHeightmap(int _size, float _peakHeight, float _baseHeight)
        {
            float[,] map = new float[_size, _size];
            float center = _size / 2f;
            float maxDist = center;

            for (int x = 0; x < _size; x++)
            {
                for (int y = 0; y < _size; y++)
                {
                    float dist = UnityEngine.Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    float t = UnityEngine.Mathf.Clamp01(dist / maxDist);
                    map[x, y] = UnityEngine.Mathf.Lerp(_peakHeight, _baseHeight, t);
                }
            }
            return map;
        }

        /// <summary>
        /// ハイトマップの最大高さを取得
        /// </summary>
        private float GetMaxHeight(float[,] _map)
        {
            float max = float.MinValue;
            int w = _map.GetLength(0);
            int h = _map.GetLength(1);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    if (_map[x, y] > max) max = _map[x, y];
            return max;
        }

        /// <summary>
        /// ハイトマップの最小高さを取得
        /// </summary>
        private float GetMinHeight(float[,] _map)
        {
            float min = float.MaxValue;
            int w = _map.GetLength(0);
            int h = _map.GetLength(1);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    if (_map[x, y] < min) min = _map[x, y];
            return min;
        }

        #endregion

        #region HydraulicErosion Tests

        [Test]
        public void HydraulicErosion_FlatMap_NoChange()
        {
            // Arrange
            var erosion = new HydraulicErosion { Iterations = 1000 };
            float[,] map = CreateFlatHeightmap(32, 0.5f);
            float originalCenter = map[16, 16];

            // Act
            erosion.Apply(map, 42);

            // Assert — 平坦な地形は侵食しても大きく変化しない
            Assert.AreEqual(originalCenter, map[16, 16], 0.1f, "Flat heightmap should not change significantly");
        }

        [Test]
        public void HydraulicErosion_PeakMap_ReducesPeak()
        {
            // Arrange
            var erosion = new HydraulicErosion { Iterations = 50000, ErosionRate = 0.5f };
            float[,] map = CreatePeakHeightmap(64, 10f, 0f);
            float originalPeak = map[32, 32];

            // Act
            erosion.Apply(map, 42);

            // Assert — ピークが侵食されて低くなる
            float newPeak = map[32, 32];
            Assert.Less(newPeak, originalPeak, "Peak should be eroded lower");
        }

        [Test]
        public void HydraulicErosion_NullMap_NoThrow()
        {
            // Arrange
            var erosion = new HydraulicErosion();

            // Act & Assert
            Assert.DoesNotThrow(() => erosion.Apply(null, 0));
        }

        [Test]
        public void HydraulicErosion_TinyMap_NoThrow()
        {
            // Arrange
            var erosion = new HydraulicErosion();
            float[,] map = new float[2, 2];

            // Act & Assert — 3x3 未満は早期リターン
            Assert.DoesNotThrow(() => erosion.Apply(map, 0));
        }

        [Test]
        public void HydraulicErosion_DefaultParams_AreReasonable()
        {
            // Arrange
            var erosion = new HydraulicErosion();

            // Assert
            Assert.AreEqual(50000, erosion.Iterations);
            Assert.Greater(erosion.ErosionRate, 0f);
            Assert.Greater(erosion.DepositionRate, 0f);
            Assert.Greater(erosion.EvaporationRate, 0f);
            Assert.Greater(erosion.Gravity, 0f);
        }

        #endregion

        #region ThermalErosion Tests

        [Test]
        public void ThermalErosion_FlatMap_NoChange()
        {
            // Arrange
            var erosion = new ThermalErosion { Iterations = 10 };
            float[,] map = CreateFlatHeightmap(32, 0.5f);

            // Act
            erosion.Apply(map);

            // Assert
            Assert.AreEqual(0.5f, map[16, 16], 0.001f, "Flat map should not change");
        }

        [Test]
        public void ThermalErosion_SteepPeak_ReducesSlope()
        {
            // Arrange
            var erosion = new ThermalErosion { Iterations = 100, TalusAngle = 0.3f };
            float[,] map = CreatePeakHeightmap(32, 5f, 0f);
            float originalPeak = GetMaxHeight(map);
            float originalMin = GetMinHeight(map);

            // Act
            erosion.Apply(map);

            // Assert — 急傾斜が緩和される
            float newPeak = GetMaxHeight(map);
            float newMin = GetMinHeight(map);
            Assert.Less(newPeak, originalPeak, "Peak should be reduced by thermal erosion");
            Assert.Greater(newMin, originalMin - 0.01f, "Valleys should receive deposited material");
        }

        [Test]
        public void ThermalErosion_NullMap_NoThrow()
        {
            var erosion = new ThermalErosion();
            Assert.DoesNotThrow(() => erosion.Apply(null));
        }

        [Test]
        public void ThermalErosion_DefaultParams_AreReasonable()
        {
            var erosion = new ThermalErosion();
            Assert.AreEqual(50, erosion.Iterations);
            Assert.Greater(erosion.TalusAngle, 0f);
            Assert.Greater(erosion.TransferRate, 0f);
        }

        #endregion
    }
}
