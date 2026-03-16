using NUnit.Framework;
using UnityEngine;
using Vastcore.Terrain.DualGrid;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// Prefabスタンプ配置システムのテストスイート
    /// PrefabStampDefinition, StampPlacement, StampRegistry をテスト
    /// </summary>
    [TestFixture]
    public class PrefabStampTests
    {
        #region Helper Methods

        /// <summary>
        /// テスト用にグリッドとColumnStackを生成
        /// </summary>
        private void CreateTestGrid(int _radius, out IrregularGrid _grid, out ColumnStack _columnStack)
        {
            _grid = new IrregularGrid();
            _grid.GenerateGrid(_radius);
            _columnStack = new ColumnStack();

            // 各セルに高さ2のスタックを設定
            foreach (Cell cell in _grid.Cells)
            {
                _columnStack.SetLayer(cell.Id, 0, true);
                _columnStack.SetLayer(cell.Id, 1, true);
            }
        }

        /// <summary>
        /// テスト用PrefabStampDefinitionを生成
        /// ScriptableObject.CreateInstanceで実体化
        /// </summary>
        private PrefabStampDefinition CreateTestDefinition()
        {
            var def = ScriptableObject.CreateInstance<PrefabStampDefinition>();
            // Prefab参照はテスト内でGameObject.CreatePrimitiveで代替する
            return def;
        }

        #endregion

        #region StampRegistry Tests

        [Test]
        public void StampRegistry_NewRegistry_IsEmpty()
        {
            // Act
            var registry = new StampRegistry();

            // Assert
            Assert.AreEqual(0, registry.Count, "New registry should be empty");
        }

        [Test]
        public void StampRegistry_CanPlace_ReturnsFalseForNullDefinition()
        {
            // Arrange
            var registry = new StampRegistry();
            var cell = new Cell(0, 0, 0, 0);

            // Act & Assert
            Assert.IsFalse(registry.CanPlace(null, cell), "Should not place null definition");
        }

        [Test]
        public void StampRegistry_CanPlace_ReturnsFalseForNullCell()
        {
            // Arrange
            var registry = new StampRegistry();
            var def = CreateTestDefinition();

            // Act & Assert
            Assert.IsFalse(registry.CanPlace(def, null), "Should not place on null cell");

            Object.DestroyImmediate(def);
        }

        [Test]
        public void StampRegistry_CanPlace_ReturnsFalseForInvalidDefinition()
        {
            // Arrange
            var registry = new StampRegistry();
            var def = CreateTestDefinition(); // Prefab is null → IsValid() = false
            var cell = new Cell(0, 0, 0, 0);

            // Act & Assert
            Assert.IsFalse(registry.CanPlace(def, cell), "Should not place invalid definition (no Prefab)");

            Object.DestroyImmediate(def);
        }

        [Test]
        public void StampRegistry_Place_ReturnsFalseWhenCannotPlace()
        {
            // Arrange
            var registry = new StampRegistry();
            var cell = new Cell(0, 0, 0, 0);
            var columnStack = new ColumnStack();

            // Act
            StampPlacement result = registry.Place(null, cell, columnStack, 0f, 1f);

            // Assert
            Assert.IsNull(result, "Place should return null when definition is null");
            Assert.AreEqual(0, registry.Count, "Count should remain 0");
        }

        [Test]
        public void StampRegistry_Place_OccupiesCell()
        {
            // Arrange
            CreateTestGrid(1, out var grid, out var columnStack);
            var registry = new StampRegistry();

            // Prefabが必要なので、テスト用にPrimitiveを使用
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var def = CreateTestDefinition();
            // Reflectionでprivateフィールドに設定（テスト用）
            SetPrefabOnDefinition(def, prefab);

            Cell targetCell = grid.Cells[0];

            // Act
            StampPlacement placement = registry.Place(def, targetCell, columnStack, 0f, 1f);

            // Assert
            Assert.IsNotNull(placement, "Place should succeed");
            Assert.AreEqual(1, registry.Count, "Count should be 1");
            Assert.IsTrue(registry.IsOccupied(targetCell.Id), "Cell should be occupied");

            // Cleanup
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void StampRegistry_Place_BlocksDoubleOccupancy()
        {
            // Arrange
            CreateTestGrid(1, out var grid, out var columnStack);
            var registry = new StampRegistry();

            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var def = CreateTestDefinition();
            SetPrefabOnDefinition(def, prefab);

            Cell targetCell = grid.Cells[0];

            // Act
            StampPlacement first = registry.Place(def, targetCell, columnStack, 0f, 1f);
            StampPlacement second = registry.Place(def, targetCell, columnStack, 90f, 1f);

            // Assert
            Assert.IsNotNull(first, "First placement should succeed");
            Assert.IsNull(second, "Second placement on same cell should fail");
            Assert.AreEqual(1, registry.Count, "Count should remain 1");

            // Cleanup
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void StampRegistry_Remove_FreesCell()
        {
            // Arrange
            CreateTestGrid(1, out var grid, out var columnStack);
            var registry = new StampRegistry();

            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var def = CreateTestDefinition();
            SetPrefabOnDefinition(def, prefab);

            Cell targetCell = grid.Cells[0];
            StampPlacement placement = registry.Place(def, targetCell, columnStack, 0f, 1f);

            // Act
            bool removed = registry.Remove(placement.PlacementId);

            // Assert
            Assert.IsTrue(removed, "Remove should succeed");
            Assert.AreEqual(0, registry.Count, "Count should be 0 after removal");
            Assert.IsFalse(registry.IsOccupied(targetCell.Id), "Cell should no longer be occupied");

            // Cleanup
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void StampRegistry_Remove_ReturnsFalseForInvalidId()
        {
            // Arrange
            var registry = new StampRegistry();

            // Act & Assert
            Assert.IsFalse(registry.Remove(999), "Remove with invalid ID should return false");
        }

        [Test]
        public void StampRegistry_GetPlacementAt_ReturnsCorrectPlacement()
        {
            // Arrange
            CreateTestGrid(1, out var grid, out var columnStack);
            var registry = new StampRegistry();

            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var def = CreateTestDefinition();
            SetPrefabOnDefinition(def, prefab);

            Cell targetCell = grid.Cells[0];
            StampPlacement placed = registry.Place(def, targetCell, columnStack, 45f, 1.5f);

            // Act
            StampPlacement retrieved = registry.GetPlacementAt(targetCell.Id);

            // Assert
            Assert.IsNotNull(retrieved, "Should find placement at cell");
            Assert.AreEqual(placed.PlacementId, retrieved.PlacementId, "Placement ID should match");
            Assert.AreEqual(45f, retrieved.Rotation, 0.001f, "Rotation should match");
            Assert.AreEqual(1.5f, retrieved.Scale, 0.001f, "Scale should match");

            // Cleanup
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void StampRegistry_GetPlacementAt_ReturnsNullForEmptyCell()
        {
            // Arrange
            var registry = new StampRegistry();

            // Act & Assert
            Assert.IsNull(registry.GetPlacementAt(0), "Should return null for unoccupied cell");
        }

        [Test]
        public void StampRegistry_Clear_RemovesAll()
        {
            // Arrange
            CreateTestGrid(1, out var grid, out var columnStack);
            var registry = new StampRegistry();

            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var def = CreateTestDefinition();
            SetPrefabOnDefinition(def, prefab);

            registry.Place(def, grid.Cells[0], columnStack, 0f, 1f);
            registry.Place(def, grid.Cells[1], columnStack, 0f, 1f);
            registry.Place(def, grid.Cells[2], columnStack, 0f, 1f);

            // Act
            registry.Clear();

            // Assert
            Assert.AreEqual(0, registry.Count, "Count should be 0 after clear");
            Assert.IsFalse(registry.IsOccupied(grid.Cells[0].Id), "Cell 0 should not be occupied");
            Assert.IsFalse(registry.IsOccupied(grid.Cells[1].Id), "Cell 1 should not be occupied");
            Assert.IsFalse(registry.IsOccupied(grid.Cells[2].Id), "Cell 2 should not be occupied");

            // Cleanup
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(def);
        }

        #endregion

        #region StampPlacement Tests

        [Test]
        public void StampPlacement_StoresCorrectCellData()
        {
            // Arrange
            var def = CreateTestDefinition();
            var cell = new Cell(42, 3, -2, 1);

            // Act
            var placement = new StampPlacement(0, def, cell, 90f, 5, 1.2f);

            // Assert
            Assert.AreEqual(0, placement.PlacementId);
            Assert.AreEqual(42, placement.AnchorCellId);
            Assert.AreEqual(3, placement.AnchorHexQ);
            Assert.AreEqual(-2, placement.AnchorHexR);
            Assert.AreEqual(1, placement.AnchorSubIndex);
            Assert.AreEqual(90f, placement.Rotation, 0.001f);
            Assert.AreEqual(5, placement.Layer);
            Assert.AreEqual(1.2f, placement.Scale, 0.001f);

            Object.DestroyImmediate(def);
        }

        #endregion

        #region StampHeightRule Tests

        [Test]
        public void StampRegistry_Place_TopOfStack_ResolvesCorrectLayer()
        {
            // Arrange
            CreateTestGrid(1, out var grid, out var columnStack);
            var registry = new StampRegistry();

            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var def = CreateTestDefinition();
            SetPrefabOnDefinition(def, prefab);
            // Default HeightRule is TopOfStack

            Cell targetCell = grid.Cells[0];
            // columnStack height for this cell should be 2 (layers 0 and 1 are solid)

            // Act
            StampPlacement placement = registry.Place(def, targetCell, columnStack, 0f, 1f);

            // Assert
            Assert.IsNotNull(placement);
            Assert.AreEqual(2, placement.Layer, "TopOfStack should place at height 2 (top of 2-layer stack)");

            // Cleanup
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(def);
        }

        #endregion

        #region PrefabStampDefinition Tests

        [Test]
        public void PrefabStampDefinition_IsValid_ReturnsFalseWithoutPrefab()
        {
            // Arrange
            var def = CreateTestDefinition();

            // Act & Assert
            Assert.IsFalse(def.IsValid(), "Definition without Prefab should be invalid");

            Object.DestroyImmediate(def);
        }

        [Test]
        public void PrefabStampDefinition_IsValid_ReturnsTrueWithPrefab()
        {
            // Arrange
            var def = CreateTestDefinition();
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            SetPrefabOnDefinition(def, prefab);

            // Act & Assert
            Assert.IsTrue(def.IsValid(), "Definition with Prefab should be valid");

            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void PrefabStampDefinition_IsSingleCell_TrueByDefault()
        {
            // Arrange
            var def = CreateTestDefinition();

            // Act & Assert
            Assert.IsTrue(def.IsSingleCell, "Default definition should be single cell");

            Object.DestroyImmediate(def);
        }

        [Test]
        public void PrefabStampDefinition_GetRandomRotation_FixedReturnsZero()
        {
            // Arrange
            var def = CreateTestDefinition();
            SetRotationMode(def, StampRotationMode.Fixed);
            var random = new System.Random(42);

            // Act
            float rotation = def.GetRandomRotation(random);

            // Assert
            Assert.AreEqual(0f, rotation, 0.001f, "Fixed rotation should always return 0");

            Object.DestroyImmediate(def);
        }

        [Test]
        public void PrefabStampDefinition_GetRandomRotation_Step90ReturnsMultipleOf90()
        {
            // Arrange
            var def = CreateTestDefinition();
            SetRotationMode(def, StampRotationMode.Step90);
            var random = new System.Random(42);

            // Act & Assert (10 samples)
            for (int i = 0; i < 10; i++)
            {
                float rotation = def.GetRandomRotation(random);
                Assert.AreEqual(0f, rotation % 90f, 0.001f,
                    $"Step90 rotation should be a multiple of 90 (got {rotation})");
            }

            Object.DestroyImmediate(def);
        }

        #endregion

        #region Reflection Helpers (Test Only)

        /// <summary>
        /// テスト用: PrefabStampDefinitionのprivateフィールドm_Prefabを設定
        /// </summary>
        private void SetPrefabOnDefinition(PrefabStampDefinition _def, GameObject _prefab)
        {
            var field = typeof(PrefabStampDefinition).GetField("m_Prefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_def, _prefab);
        }

        /// <summary>
        /// テスト用: PrefabStampDefinitionのprivateフィールドm_RotationModeを設定
        /// </summary>
        private void SetRotationMode(PrefabStampDefinition _def, StampRotationMode _mode)
        {
            var field = typeof(PrefabStampDefinition).GetField("m_RotationMode",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_def, _mode);
        }

        #endregion
    }
}
