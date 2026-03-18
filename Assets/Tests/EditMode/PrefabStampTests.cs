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

        /// <summary>
        /// テスト用: PrefabStampDefinitionのprivateフィールドm_FootprintOffsetsを設定
        /// </summary>
        private void SetFootprintOffsets(PrefabStampDefinition _def, Vector2Int[] _offsets)
        {
            var field = typeof(PrefabStampDefinition).GetField("m_FootprintOffsets",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_def, _offsets);
        }

        /// <summary>
        /// テスト用: PrefabStampDefinitionのprivateフィールドm_PositionJitterを設定
        /// </summary>
        private void SetPositionJitter(PrefabStampDefinition _def, float _jitter)
        {
            var field = typeof(PrefabStampDefinition).GetField("m_PositionJitter",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_def, _jitter);
        }

        /// <summary>
        /// テスト用: PrefabStampDefinitionのprivateフィールドm_MaterialVariantsを設定
        /// </summary>
        private void SetMaterialVariants(PrefabStampDefinition _def, Material[] _variants)
        {
            var field = typeof(PrefabStampDefinition).GetField("m_MaterialVariants",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_def, _variants);
        }

        /// <summary>
        /// テスト用: PrefabStampDefinitionのprivateフィールドm_ChildToggleGroupsを設定
        /// </summary>
        private void SetChildToggleGroups(PrefabStampDefinition _def, string[] _groups)
        {
            var field = typeof(PrefabStampDefinition).GetField("m_ChildToggleGroups",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_def, _groups);
        }

        /// <summary>
        /// テスト用: 有効なPrefab付き定義を生成
        /// </summary>
        private PrefabStampDefinition CreateValidDefinition(out GameObject _prefab)
        {
            _prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var def = CreateTestDefinition();
            SetPrefabOnDefinition(def, _prefab);
            return def;
        }

        #endregion

        #region Parametric Variation (V1) Tests

        [Test]
        public void PrefabStampDefinition_PositionJitter_ZeroReturnsZeroOffset()
        {
            // Arrange
            var def = CreateTestDefinition();
            // m_PositionJitter defaults to 0
            var random = new System.Random(42);

            // Act
            Vector3 offset = def.GetRandomPositionOffset(random);

            // Assert
            Assert.AreEqual(Vector3.zero, offset, "Zero jitter should produce zero offset");

            Object.DestroyImmediate(def);
        }

        [Test]
        public void PrefabStampDefinition_PositionJitter_StaysWithinRadius()
        {
            // Arrange
            var def = CreateTestDefinition();
            SetPositionJitter(def, 0.5f);
            var random = new System.Random(42);

            // Act & Assert (100 samples)
            for (int i = 0; i < 100; i++)
            {
                Vector3 offset = def.GetRandomPositionOffset(random);
                float distance = new Vector2(offset.x, offset.z).magnitude;
                Assert.LessOrEqual(distance, 0.5f + 0.001f,
                    $"Offset distance {distance} should be within jitter radius 0.5");
                Assert.AreEqual(0f, offset.y, 0.001f, "Y offset should always be 0");
            }

            Object.DestroyImmediate(def);
        }

        [Test]
        public void PrefabStampDefinition_PositionJitter_NullRandomReturnsZero()
        {
            // Arrange
            var def = CreateTestDefinition();
            SetPositionJitter(def, 1.0f);

            // Act
            Vector3 offset = def.GetRandomPositionOffset(null);

            // Assert
            Assert.AreEqual(Vector3.zero, offset, "Null random should return zero offset");

            Object.DestroyImmediate(def);
        }

        [Test]
        public void PrefabStampDefinition_MaterialVariants_EmptyReturnsNull()
        {
            // Arrange
            var def = CreateTestDefinition();
            // m_MaterialVariants defaults to empty
            var random = new System.Random(42);

            // Act
            Material mat = def.GetRandomMaterial(random);

            // Assert
            Assert.IsNull(mat, "Empty material variants should return null");

            Object.DestroyImmediate(def);
        }

        [Test]
        public void PrefabStampDefinition_MaterialVariants_SelectsFromArray()
        {
            // Arrange
            var def = CreateTestDefinition();
            var mat1 = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var mat2 = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat1.name = "TestMat1";
            mat2.name = "TestMat2";
            SetMaterialVariants(def, new Material[] { mat1, mat2 });

            var random = new System.Random(42);

            // Act (20 samples)
            bool sawMat1 = false;
            bool sawMat2 = false;
            for (int i = 0; i < 20; i++)
            {
                Material selected = def.GetRandomMaterial(random);
                Assert.IsNotNull(selected, "Should select a material");
                if (selected == mat1) sawMat1 = true;
                if (selected == mat2) sawMat2 = true;
            }

            // Assert
            Assert.IsTrue(sawMat1 && sawMat2,
                "Both materials should be selected over 20 samples");

            Object.DestroyImmediate(mat1);
            Object.DestroyImmediate(mat2);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void PrefabStampDefinition_ChildToggleGroups_EmptyReturnsMinus1()
        {
            // Arrange
            var def = CreateTestDefinition();
            // m_ChildToggleGroups defaults to empty
            var random = new System.Random(42);

            // Act
            int index = def.GetRandomChildToggleIndex(random);

            // Assert
            Assert.AreEqual(-1, index, "Empty groups should return -1");

            Object.DestroyImmediate(def);
        }

        [Test]
        public void PrefabStampDefinition_ChildToggleGroups_IndexInRange()
        {
            // Arrange
            var def = CreateTestDefinition();
            SetChildToggleGroups(def, new string[] { "Roof_A", "Roof_B", "Roof_C" });
            var random = new System.Random(42);

            // Act & Assert (50 samples)
            for (int i = 0; i < 50; i++)
            {
                int index = def.GetRandomChildToggleIndex(random);
                Assert.GreaterOrEqual(index, 0);
                Assert.Less(index, 3, $"Index {index} should be < 3");
            }

            Object.DestroyImmediate(def);
        }

        [Test]
        public void PrefabStampDefinition_Variation_SameSeedProducesSameResult()
        {
            // Arrange
            var def = CreateTestDefinition();
            SetPositionJitter(def, 1.0f);
            SetChildToggleGroups(def, new string[] { "A", "B" });

            // Act
            var rng1 = new System.Random(12345);
            Vector3 offset1 = def.GetRandomPositionOffset(rng1);
            int toggle1 = def.GetRandomChildToggleIndex(rng1);

            var rng2 = new System.Random(12345);
            Vector3 offset2 = def.GetRandomPositionOffset(rng2);
            int toggle2 = def.GetRandomChildToggleIndex(rng2);

            // Assert
            Assert.AreEqual(offset1, offset2, "Same seed should produce same position offset");
            Assert.AreEqual(toggle1, toggle2, "Same seed should produce same toggle index");

            Object.DestroyImmediate(def);
        }

        [Test]
        public void PrefabStampDefinition_Variation_DefaultsAreBackwardCompatible()
        {
            // 全変異パラメータのデフォルト値で従来動作が変わらないことを確認
            // Arrange
            var def = CreateTestDefinition();

            // Assert
            Assert.AreEqual(0f, def.PositionJitter, "Default PositionJitter should be 0");
            Assert.IsNotNull(def.MaterialVariants, "MaterialVariants should not be null");
            Assert.AreEqual(0, def.MaterialVariants.Length, "Default MaterialVariants should be empty");
            Assert.IsNotNull(def.ChildToggleGroups, "ChildToggleGroups should not be null");
            Assert.AreEqual(0, def.ChildToggleGroups.Length, "Default ChildToggleGroups should be empty");

            Object.DestroyImmediate(def);
        }

        #endregion

        #region Multi-Cell Footprint Tests

        [Test]
        public void PrefabStampDefinition_IsSingleCell_FalseWithOffsets()
        {
            // Arrange
            var def = CreateTestDefinition();
            SetFootprintOffsets(def, new Vector2Int[] { new Vector2Int(1, 0) });

            // Act & Assert
            Assert.IsFalse(def.IsSingleCell, "Definition with offsets should not be single cell");

            Object.DestroyImmediate(def);
        }

        [Test]
        public void StampRegistry_MultiCell_CanPlace_RequiresGrid()
        {
            // Arrange
            var registry = new StampRegistry();
            var def = CreateValidDefinition(out var prefab);
            SetFootprintOffsets(def, new Vector2Int[] { new Vector2Int(1, 0) });
            var cell = new Cell(0, 0, 0, 0);

            // Act — gridなしでマルチセル配置を試行
            bool canPlace = registry.CanPlace(def, cell, null);

            // Assert
            Assert.IsFalse(canPlace, "Multi-cell CanPlace should fail without grid");

            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void StampRegistry_MultiCell_Place_OccupiesAllSubCells()
        {
            // Arrange
            CreateTestGrid(2, out var grid, out var columnStack);
            var registry = new StampRegistry();
            var def = CreateValidDefinition(out var prefab);

            // アンカーhex(0,0) + オフセットhex(1,0) の2ヘックスフットプリント
            SetFootprintOffsets(def, new Vector2Int[] { new Vector2Int(1, 0) });

            // アンカーセルを取得（hex 0,0 の最初のサブセル）
            Cell anchorCell = grid.FindCell(0, 0, 0);
            Assert.IsNotNull(anchorCell, "Anchor cell (0,0,0) should exist in radius-2 grid");

            // Act
            StampPlacement placement = registry.Place(def, anchorCell, columnStack, 0f, 1f, grid);

            // Assert
            Assert.IsNotNull(placement, "Multi-cell placement should succeed");
            Assert.AreEqual(1, registry.Count);

            // アンカーヘックス(0,0)の全サブセルが占有されている
            for (int sub = 0; sub < 3; sub++)
            {
                Cell c = grid.FindCell(0, 0, sub);
                if (c != null)
                {
                    Assert.IsTrue(registry.IsOccupied(c.Id),
                        $"Anchor hex subcell (0,0,{sub}) id={c.Id} should be occupied");
                }
            }

            // オフセットヘックス(1,0)の全サブセルが占有されている
            for (int sub = 0; sub < 3; sub++)
            {
                Cell c = grid.FindCell(1, 0, sub);
                if (c != null)
                {
                    Assert.IsTrue(registry.IsOccupied(c.Id),
                        $"Offset hex subcell (1,0,{sub}) id={c.Id} should be occupied");
                }
            }

            // OccupiedCellIds の件数確認
            Assert.IsNotNull(placement.OccupiedCellIds);
            Assert.IsTrue(placement.OccupiedCellIds.Length >= 2,
                "OccupiedCellIds should contain at least 2 cells (anchor + offset subcells)");

            // Cleanup
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void StampRegistry_MultiCell_BlocksOverlappingPlacement()
        {
            // Arrange
            CreateTestGrid(2, out var grid, out var columnStack);
            var registry = new StampRegistry();

            var def1 = CreateValidDefinition(out var prefab1);
            SetFootprintOffsets(def1, new Vector2Int[] { new Vector2Int(1, 0) }); // hex(0,0) + hex(1,0)

            var def2 = CreateValidDefinition(out var prefab2);
            SetFootprintOffsets(def2, new Vector2Int[] { new Vector2Int(-1, 0) }); // hex(1,0) + hex(0,0)

            Cell anchor1 = grid.FindCell(0, 0, 0);
            Cell anchor2 = grid.FindCell(1, 0, 0);

            // Act
            StampPlacement first = registry.Place(def1, anchor1, columnStack, 0f, 1f, grid);
            StampPlacement second = registry.Place(def2, anchor2, columnStack, 0f, 1f, grid);

            // Assert
            Assert.IsNotNull(first, "First multi-cell placement should succeed");
            Assert.IsNull(second, "Overlapping multi-cell placement should fail (hex 1,0 shared)");
            Assert.AreEqual(1, registry.Count);

            // Cleanup
            Object.DestroyImmediate(prefab1);
            Object.DestroyImmediate(def1);
            Object.DestroyImmediate(prefab2);
            Object.DestroyImmediate(def2);
        }

        [Test]
        public void StampRegistry_MultiCell_Remove_FreesAllCells()
        {
            // Arrange
            CreateTestGrid(2, out var grid, out var columnStack);
            var registry = new StampRegistry();
            var def = CreateValidDefinition(out var prefab);
            SetFootprintOffsets(def, new Vector2Int[] { new Vector2Int(1, 0) });

            Cell anchorCell = grid.FindCell(0, 0, 0);
            StampPlacement placement = registry.Place(def, anchorCell, columnStack, 0f, 1f, grid);
            int occupiedBefore = registry.OccupiedCellCount;

            // Act
            bool removed = registry.Remove(placement.PlacementId);

            // Assert
            Assert.IsTrue(removed);
            Assert.AreEqual(0, registry.Count);
            Assert.AreEqual(0, registry.OccupiedCellCount, "All occupied cells should be freed");
            Assert.IsTrue(occupiedBefore > 1, $"Should have had multiple occupied cells ({occupiedBefore})");

            // 全サブセルが解放されている
            for (int sub = 0; sub < 3; sub++)
            {
                Cell c0 = grid.FindCell(0, 0, sub);
                Cell c1 = grid.FindCell(1, 0, sub);
                if (c0 != null)
                    Assert.IsFalse(registry.IsOccupied(c0.Id), $"hex(0,0,{sub}) should be freed");
                if (c1 != null)
                    Assert.IsFalse(registry.IsOccupied(c1.Id), $"hex(1,0,{sub}) should be freed");
            }

            // Cleanup
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void StampRegistry_MultiCell_GetPlacementAt_FindsFromAnyOccupiedCell()
        {
            // Arrange
            CreateTestGrid(2, out var grid, out var columnStack);
            var registry = new StampRegistry();
            var def = CreateValidDefinition(out var prefab);
            SetFootprintOffsets(def, new Vector2Int[] { new Vector2Int(1, 0) });

            Cell anchorCell = grid.FindCell(0, 0, 0);
            StampPlacement placement = registry.Place(def, anchorCell, columnStack, 0f, 1f, grid);

            // Act — オフセットヘックスのサブセルからも検索できる
            Cell offsetCell = grid.FindCell(1, 0, 0);
            Assert.IsNotNull(offsetCell, "Offset cell should exist");
            StampPlacement found = registry.GetPlacementAt(offsetCell.Id);

            // Assert
            Assert.IsNotNull(found, "Should find placement from offset hex cell");
            Assert.AreEqual(placement.PlacementId, found.PlacementId);

            // Cleanup
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void StampRegistry_MultiCell_CanPlace_FailsIfOffsetHexMissing()
        {
            // Arrange — radius-1 grid は中心hex(0,0)とその近傍のみ
            CreateTestGrid(1, out var grid, out var columnStack);
            var registry = new StampRegistry();
            var def = CreateValidDefinition(out var prefab);

            // 遠方のオフセットを指定（radius-1グリッドには存在しない）
            SetFootprintOffsets(def, new Vector2Int[] { new Vector2Int(5, 5) });

            Cell anchorCell = grid.FindCell(0, 0, 0);
            Assert.IsNotNull(anchorCell);

            // Act
            bool canPlace = registry.CanPlace(def, anchorCell, grid);

            // Assert
            Assert.IsFalse(canPlace, "Should fail when footprint hex is outside grid");

            // Cleanup
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void StampRegistry_SingleCell_BackwardCompatible()
        {
            // 単一セルスタンプが旧APIでも新APIでも動作する
            // Arrange
            CreateTestGrid(1, out var grid, out var columnStack);
            var registry = new StampRegistry();
            var def = CreateValidDefinition(out var prefab);
            // FootprintOffsets はデフォルト空 → IsSingleCell = true

            Cell targetCell = grid.Cells[0];

            // Act — 旧API（gridなし）
            StampPlacement p1 = registry.Place(def, targetCell, columnStack, 0f, 1f);

            // Assert
            Assert.IsNotNull(p1);
            Assert.IsNotNull(p1.OccupiedCellIds);
            Assert.AreEqual(1, p1.OccupiedCellIds.Length, "Single cell should occupy exactly 1 cell");
            Assert.AreEqual(targetCell.Id, p1.OccupiedCellIds[0]);

            // Cleanup
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(def);
        }

        [Test]
        public void StampRegistry_MultiCell_ThreeHexFootprint()
        {
            // 3ヘックスの大きなフットプリント
            // Arrange
            CreateTestGrid(3, out var grid, out var columnStack);
            var registry = new StampRegistry();
            var def = CreateValidDefinition(out var prefab);

            // アンカー(0,0) + (1,0) + (0,1) の三角形フットプリント
            SetFootprintOffsets(def, new Vector2Int[] {
                new Vector2Int(1, 0),
                new Vector2Int(0, 1)
            });

            Cell anchorCell = grid.FindCell(0, 0, 0);
            Assert.IsNotNull(anchorCell);

            // Act
            StampPlacement placement = registry.Place(def, anchorCell, columnStack, 0f, 1f, grid);

            // Assert
            Assert.IsNotNull(placement, "3-hex footprint should place successfully");
            Assert.IsTrue(placement.OccupiedCellIds.Length >= 3,
                $"Should occupy cells from 3 hexes (got {placement.OccupiedCellIds.Length})");

            // 3つのヘックス全てが占有されている
            foreach (var hex in new[] { (0, 0), (1, 0), (0, 1) })
            {
                Cell c = grid.FindCell(hex.Item1, hex.Item2, 0);
                Assert.IsNotNull(c, $"hex({hex.Item1},{hex.Item2}) should exist");
                Assert.IsTrue(registry.IsOccupied(c.Id),
                    $"hex({hex.Item1},{hex.Item2}) should be occupied");
            }

            // Cleanup
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(def);
        }

        #endregion
    }
}
