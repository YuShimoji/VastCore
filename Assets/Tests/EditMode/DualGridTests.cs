using NUnit.Framework;
using UnityEngine;
using Vastcore.Terrain.DualGrid;
using System.Collections.Generic;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// DualGrid システムのテストスイート
    /// GridTopology, IrregularGrid, Coordinates, Node, Cell をテスト
    /// </summary>
    [TestFixture]
    public class DualGridTests
    {
        #region Node Tests

        [Test]
        public void Node_DefaultConstructor_SetsDefaultValues()
        {
            // Act
            var node = new Node();

            // Assert
            Assert.AreEqual(-1, node.Id, "Default Id should be -1");
            Assert.AreEqual(Vector3.zero, node.Position, "Default position should be zero");
            Assert.IsFalse(node.HasGround, "HasGround should default to false");
            Assert.IsFalse(node.HasCeiling, "HasCeiling should default to false");
            Assert.AreEqual(0, node.HeightIndex, "HeightIndex should default to 0");
        }

        [Test]
        public void Node_ParameterizedConstructor_SetsValues()
        {
            // Arrange
            Vector3 position = new Vector3(1f, 2f, 3f);

            // Act
            var node = new Node(5, position);

            // Assert
            Assert.AreEqual(5, node.Id, "Id should match constructor argument");
            Assert.AreEqual(position, node.Position, "Position should match constructor argument");
        }

        [Test]
        public void Node_IsSolid_ReturnsTrueWhenHasGroundOrCeiling()
        {
            // Arrange
            var nodeGround = new Node { HasGround = true, HasCeiling = false };
            var nodeCeiling = new Node { HasGround = false, HasCeiling = true };
            var nodeBoth = new Node { HasGround = true, HasCeiling = true };
            var nodeNeither = new Node { HasGround = false, HasCeiling = false };

            // Assert
            Assert.IsTrue(nodeGround.IsSolid(), "Node with ground should be solid");
            Assert.IsTrue(nodeCeiling.IsSolid(), "Node with ceiling should be solid");
            Assert.IsTrue(nodeBoth.IsSolid(), "Node with both should be solid");
            Assert.IsFalse(nodeNeither.IsSolid(), "Node with neither should not be solid");
        }

        #endregion

        #region Cell Tests

        [Test]
        public void Cell_DefaultConstructor_SetsDefaultValues()
        {
            // Act
            var cell = new Cell();

            // Assert
            Assert.AreEqual(-1, cell.Id, "Default Id should be -1");
            Assert.IsNotNull(cell.Corners, "Corners array should be initialized");
            Assert.AreEqual(4, cell.Corners.Length, "Corners should have 4 elements");
            Assert.IsNotNull(cell.Neighbors, "Neighbors array should be initialized");
            Assert.AreEqual(4, cell.Neighbors.Length, "Neighbors should have 4 elements");
        }

        [Test]
        public void Cell_ParameterizedConstructor_SetsHexCoordinates()
        {
            // Act
            var cell = new Cell(10, 2, -3, 1);

            // Assert
            Assert.AreEqual(10, cell.Id, "Id should match");
            Assert.AreEqual(2, cell.HexQ, "HexQ should match");
            Assert.AreEqual(-3, cell.HexR, "HexR should match");
            Assert.AreEqual(1, cell.SubIndex, "SubIndex should match");
        }

        [Test]
        public void Cell_GetCenter_CalculatesAverageOfCorners()
        {
            // Arrange
            var cell = new Cell(0, 0, 0, 0);
            cell.Corners[0] = new Node(0, new Vector3(0f, 0f, 0f));
            cell.Corners[1] = new Node(1, new Vector3(2f, 0f, 0f));
            cell.Corners[2] = new Node(2, new Vector3(2f, 0f, 2f));
            cell.Corners[3] = new Node(3, new Vector3(0f, 0f, 2f));

            // Act
            Vector3 center = cell.GetCenter();

            // Assert
            Assert.AreEqual(1f, center.x, 0.001f, "Center X should be average of corners");
            Assert.AreEqual(0f, center.y, 0.001f, "Center Y should be 0");
            Assert.AreEqual(1f, center.z, 0.001f, "Center Z should be average of corners");
        }

        [Test]
        public void Cell_GetCenter_WithNullCorners_ReturnsZero()
        {
            // Arrange
            var cell = new Cell();
            cell.Corners = null;

            // Act
            Vector3 center = cell.GetCenter();

            // Assert
            Assert.AreEqual(Vector3.zero, center, "GetCenter with null corners should return zero");
        }

        #endregion

        #region Coordinates Tests

        [Test]
        public void Coordinates_AxialToWorld_OriginReturnsZero()
        {
            // Act
            Vector2 result = Coordinates.AxialToWorld(0, 0);

            // Assert
            Assert.AreEqual(0f, result.x, 0.001f, "Origin q=0,r=0 should map to x=0");
            Assert.AreEqual(0f, result.y, 0.001f, "Origin q=0,r=0 should map to z=0");
        }

        [Test]
        public void Coordinates_AxialToWorld3D_ReturnsYZero()
        {
            // Act
            Vector3 result = Coordinates.AxialToWorld3D(1, 1);

            // Assert
            Assert.AreEqual(0f, result.y, 0.001f, "Y component should always be 0");
        }

        [Test]
        public void Coordinates_WorldToAxial_RoundTrip_ReturnsOriginal()
        {
            // Arrange
            int originalQ = 2;
            int originalR = -1;

            // Act
            Vector3 worldPos = Coordinates.AxialToWorld3D(originalQ, originalR);
            Vector2Int axial = Coordinates.WorldToAxial(worldPos);

            // Assert
            Assert.AreEqual(originalQ, axial.x, "Q should survive round-trip");
            Assert.AreEqual(originalR, axial.y, "R should survive round-trip");
        }

        [Test]
        public void Coordinates_GetHexNeighbor_ReturnsCorrectOffsets()
        {
            // Arrange
            int q = 0, r = 0;

            // Act - direction 0 (right): offset (1, 0)
            Vector2Int neighbor0 = Coordinates.GetHexNeighbor(q, r, 0);
            // direction 3 (left): offset (-1, 0)
            Vector2Int neighbor3 = Coordinates.GetHexNeighbor(q, r, 3);

            // Assert
            Assert.AreEqual(new Vector2Int(1, 0), neighbor0, "Direction 0 should be (1,0)");
            Assert.AreEqual(new Vector2Int(-1, 0), neighbor3, "Direction 3 should be (-1,0)");
        }

        [Test]
        public void Coordinates_GetSubCellCenter_ReturnsOffsetFromHexCenter()
        {
            // Arrange
            int q = 0, r = 0;
            Vector3 hexCenter = Coordinates.AxialToWorld3D(q, r);

            // Act
            Vector3 subCell0 = Coordinates.GetSubCellCenter(q, r, 0);
            Vector3 subCell1 = Coordinates.GetSubCellCenter(q, r, 1);
            Vector3 subCell2 = Coordinates.GetSubCellCenter(q, r, 2);

            // Assert - each sub-cell should be offset from center
            Assert.AreNotEqual(hexCenter, subCell0, "SubCell 0 should be offset from hex center");
            Assert.AreNotEqual(subCell0, subCell1, "SubCell 0 and 1 should be at different positions");
            Assert.AreNotEqual(subCell1, subCell2, "SubCell 1 and 2 should be at different positions");
        }

        #endregion

        #region GridTopology Tests

        [Test]
        public void GridTopology_Radius0_Produces3Cells()
        {
            // Act
            GridTopology.GenerateHexToQuadGrid(0, out List<Node> nodes, out List<Cell> cells);

            // Assert - radius 0 = 1 hex = 3 sub-cells
            Assert.AreEqual(3, cells.Count, "Radius 0 should produce 3 cells (1 hex × 3 sub-cells)");
            Assert.Greater(nodes.Count, 0, "Should produce at least some nodes");
        }

        [Test]
        public void GridTopology_Radius1_Produces21Cells()
        {
            // Act
            GridTopology.GenerateHexToQuadGrid(1, out List<Node> nodes, out List<Cell> cells);

            // Assert - radius 1 = 7 hexes = 21 sub-cells
            Assert.AreEqual(21, cells.Count, "Radius 1 should produce 21 cells (7 hexes × 3 sub-cells)");
        }

        [Test]
        public void GridTopology_Radius2_Produces57Cells()
        {
            // Act
            GridTopology.GenerateHexToQuadGrid(2, out List<Node> nodes, out List<Cell> cells);

            // Assert - radius 2 = 19 hexes = 57 sub-cells
            Assert.AreEqual(57, cells.Count, "Radius 2 should produce 57 cells (19 hexes × 3 sub-cells)");
        }

        [Test]
        public void GridTopology_AllCellsHave4Corners()
        {
            // Act
            GridTopology.GenerateHexToQuadGrid(1, out List<Node> nodes, out List<Cell> cells);

            // Assert
            foreach (var cell in cells)
            {
                Assert.IsNotNull(cell.Corners, $"Cell {cell.Id} corners should not be null");
                Assert.AreEqual(4, cell.Corners.Length, $"Cell {cell.Id} should have 4 corners");
                for (int i = 0; i < 4; i++)
                {
                    Assert.IsNotNull(cell.Corners[i], $"Cell {cell.Id} corner {i} should not be null");
                }
            }
        }

        [Test]
        public void GridTopology_AllCellsHaveValidSubIndex()
        {
            // Act
            GridTopology.GenerateHexToQuadGrid(1, out List<Node> nodes, out List<Cell> cells);

            // Assert
            foreach (var cell in cells)
            {
                Assert.GreaterOrEqual(cell.SubIndex, 0, $"Cell {cell.Id} SubIndex should be >= 0");
                Assert.Less(cell.SubIndex, 3, $"Cell {cell.Id} SubIndex should be < 3");
            }
        }

        [Test]
        public void GridTopology_NodesAreSharedBetweenCells()
        {
            // Act
            GridTopology.GenerateHexToQuadGrid(1, out List<Node> nodes, out List<Cell> cells);

            // Assert - node count should be less than cells * 4 (due to sharing)
            int totalCornerReferences = cells.Count * 4; // 21 * 4 = 84
            Assert.Less(nodes.Count, totalCornerReferences,
                "Node count should be less than total corner references (nodes are shared)");
        }

        [Test]
        public void GridTopology_NeighborRelationsAreBuilt()
        {
            // Act
            GridTopology.GenerateHexToQuadGrid(1, out List<Node> nodes, out List<Cell> cells);

            // Assert - at least some cells should have neighbors
            int cellsWithNeighbors = 0;
            foreach (var cell in cells)
            {
                for (int dir = 0; dir < 4; dir++)
                {
                    if (cell.Neighbors[dir] != null)
                    {
                        cellsWithNeighbors++;
                        break;
                    }
                }
            }
            Assert.Greater(cellsWithNeighbors, 0, "At least some cells should have neighbors");
        }

        #endregion

        #region IrregularGrid Tests

        [Test]
        public void IrregularGrid_Constructor_StartsEmpty()
        {
            // Act
            var grid = new IrregularGrid();

            // Assert
            Assert.IsNotNull(grid.Nodes, "Nodes should be initialized");
            Assert.IsNotNull(grid.Cells, "Cells should be initialized");
            Assert.AreEqual(0, grid.Nodes.Count, "Nodes should start empty");
            Assert.AreEqual(0, grid.Cells.Count, "Cells should start empty");
            Assert.AreEqual(0, grid.Radius, "Radius should start at 0");
        }

        [Test]
        public void IrregularGrid_GenerateGrid_SetsRadius()
        {
            // Arrange
            var grid = new IrregularGrid();

            // Act
            grid.GenerateGrid(2);

            // Assert
            Assert.AreEqual(2, grid.Radius, "Radius should be set after GenerateGrid");
        }

        [Test]
        public void IrregularGrid_GenerateGrid_PopulatesNodesAndCells()
        {
            // Arrange
            var grid = new IrregularGrid();

            // Act
            grid.GenerateGrid(1);

            // Assert
            Assert.Greater(grid.Nodes.Count, 0, "Should have nodes after generation");
            Assert.AreEqual(21, grid.Cells.Count, "Should have 21 cells for radius 1");
        }

        [Test]
        public void IrregularGrid_Clear_EmptiesEverything()
        {
            // Arrange
            var grid = new IrregularGrid();
            grid.GenerateGrid(1);
            Assert.Greater(grid.Nodes.Count, 0, "Precondition: should have nodes");

            // Act
            grid.Clear();

            // Assert
            Assert.AreEqual(0, grid.Nodes.Count, "Nodes should be empty after Clear");
            Assert.AreEqual(0, grid.Cells.Count, "Cells should be empty after Clear");
            Assert.AreEqual(0, grid.Radius, "Radius should be reset after Clear");
        }

        [Test]
        public void IrregularGrid_ApplyRelaxation_ModifiesNodePositions()
        {
            // Arrange
            var grid = new IrregularGrid();
            grid.GenerateGrid(1);

            // Record original positions
            var originalPositions = new List<Vector3>();
            foreach (var node in grid.Nodes)
            {
                originalPositions.Add(node.Position);
            }

            // Act
            grid.ApplyRelaxation(42, 0.5f, true);

            // Assert - at least some positions should change
            bool hasChanged = false;
            for (int i = 0; i < grid.Nodes.Count; i++)
            {
                if (Vector3.Distance(originalPositions[i], grid.Nodes[i].Position) > 0.001f)
                {
                    hasChanged = true;
                    break;
                }
            }
            Assert.IsTrue(hasChanged, "Relaxation should modify at least some node positions");
        }

        [Test]
        public void IrregularGrid_ApplyRelaxation_WithZeroJitter_DoesNotChangePositions()
        {
            // Arrange
            var grid = new IrregularGrid();
            grid.GenerateGrid(1);

            var originalPositions = new List<Vector3>();
            foreach (var node in grid.Nodes)
            {
                originalPositions.Add(node.Position);
            }

            // Act - zero jitter should not change positions
            grid.ApplyRelaxation(42, 0f, false);

            // Assert
            for (int i = 0; i < grid.Nodes.Count; i++)
            {
                Assert.AreEqual(originalPositions[i].x, grid.Nodes[i].Position.x, 0.001f,
                    $"Node {i} X should not change with zero jitter");
                Assert.AreEqual(originalPositions[i].z, grid.Nodes[i].Position.z, 0.001f,
                    $"Node {i} Z should not change with zero jitter");
            }
        }

        [Test]
        public void IrregularGrid_ApplyRelaxation_OnEmptyGrid_DoesNotThrow()
        {
            // Arrange
            var grid = new IrregularGrid();

            // Act & Assert
            Assert.DoesNotThrow(() => grid.ApplyRelaxation(42),
                "Relaxation on empty grid should not throw");
        }

        #endregion
    }
}
