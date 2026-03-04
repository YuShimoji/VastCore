using NUnit.Framework;
using Vastcore.Terrain.MarchingSquares;

namespace Vastcore.Testing.EditMode.TerrainTests
{
    public class MarchingSquaresCalculatorEditModeTests
    {
        [Test]
        public void CalculateIndex_ComputesExpectedBitPattern()
        {
            var grid = new MarchingSquaresGrid(2, 2);
            grid.SetVertex(0, 1, true);  // TL
            grid.SetVertex(1, 1, true);  // TR
            grid.SetVertex(1, 0, false); // BR
            grid.SetVertex(0, 0, true);  // BL

            var index = MarchingSquaresCalculator.CalculateIndex(grid, 0, 0);

            Assert.AreEqual(13, index);
        }

        [Test]
        public void IsValidCell_UsesGridBounds()
        {
            var grid = new MarchingSquaresGrid(4, 3);

            Assert.IsTrue(MarchingSquaresCalculator.IsValidCell(grid, 0, 0));
            Assert.IsTrue(MarchingSquaresCalculator.IsValidCell(grid, 2, 1));
            Assert.IsFalse(MarchingSquaresCalculator.IsValidCell(grid, 3, 1));
            Assert.IsFalse(MarchingSquaresCalculator.IsValidCell(grid, -1, 0));
        }
    }
}
