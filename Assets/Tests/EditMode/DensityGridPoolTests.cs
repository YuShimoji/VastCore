using NUnit.Framework;
using Vastcore.Terrain.Volumetric;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// DensityGridPool の基本動作を検証するテスト。
    /// </summary>
    [TestFixture]
    public class DensityGridPoolTests
    {
        [Test]
        public void Acquire_WithSameResolution_AfterRelease_ReusesInstance()
        {
            var pool = new DensityGridPool();

            var first = pool.Acquire(16);
            pool.Release(first);
            var second = pool.Acquire(16);

            Assert.AreSame(first, second);
        }

        [Test]
        public void Release_ClearsDensityValues()
        {
            var pool = new DensityGridPool();
            var grid = pool.Acquire(8);

            grid[1, 1, 1] = 42f;
            pool.Release(grid);

            var reused = pool.Acquire(8);
            Assert.AreEqual(0f, reused[1, 1, 1]);
        }
    }
}
