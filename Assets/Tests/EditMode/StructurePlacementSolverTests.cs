using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vastcore.Generation;
using Vastcore.Terrain.DualGrid;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// StructurePlacementSolver の EditMode テスト。
    /// ゾーンバイアス・隣接親和度・密度制御・MinSpacing の統合動作を検証する。
    /// </summary>
    public class StructurePlacementSolverTests
    {
        private StampRegistry m_Registry;
        private StructurePlacementSolver m_Solver;
        private PlacementZone m_Zone;
        private AdjacencyRuleSet m_Rules;
        private List<Cell> m_Cells;

        [SetUp]
        public void SetUp()
        {
            m_Registry = new StampRegistry();
            m_Solver = new StructurePlacementSolver(m_Registry);
            m_Zone = ScriptableObject.CreateInstance<PlacementZone>();
            m_Rules = ScriptableObject.CreateInstance<AdjacencyRuleSet>();

            // テスト用のセルグリッドを作成 (10セル、直線配置)
            m_Cells = new List<Cell>();
            for (int i = 0; i < 10; i++)
            {
                Cell cell = new Cell(i, i, 0, 0);
                // Corners にダミーノードを設定 (GetCenter が動作するように)
                for (int c = 0; c < 4; c++)
                {
                    cell.Corners[c] = new Node(c, new Vector3(i + (c % 2) * 0.5f, 0, (c / 2) * 0.5f));
                }
                m_Cells.Add(cell);
            }
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_Zone);
            Object.DestroyImmediate(m_Rules);
        }

        #region Basic Solve Tests

        [Test]
        public void Solve_NullCells_ReturnsZero()
        {
            var stamp = CreateTestStamp("test");
            int placed = m_Solver.Solve(null, new[] { stamp }, m_Zone, m_Rules, new System.Random(42));
            Assert.AreEqual(0, placed);
            DestroyStamp(stamp);
        }

        [Test]
        public void Solve_NullStamps_ReturnsZero()
        {
            int placed = m_Solver.Solve(m_Cells, null, m_Zone, m_Rules, new System.Random(42));
            Assert.AreEqual(0, placed);
        }

        [Test]
        public void Solve_NullZone_ReturnsZero()
        {
            var stamp = CreateTestStamp("test");
            int placed = m_Solver.Solve(m_Cells, new[] { stamp }, null, m_Rules, new System.Random(42));
            Assert.AreEqual(0, placed);
            DestroyStamp(stamp);
        }

        #endregion

        #region Density Tests

        [Test]
        public void Solve_HighDensity_PlacesMore()
        {
            var stamp = CreateTestStamp("temple", new TagEntry("sacred", 0.9f));
            SetZoneDensity(1.0f);
            SetZoneBias(new TagEntry("sacred", 0.8f));

            int placed = m_Solver.Solve(m_Cells, new[] { stamp }, m_Zone, null, new System.Random(42));
            Assert.Greater(placed, 0, "密度1.0では少なくとも1つ配置される");
            Assert.AreEqual(placed, m_Registry.Count);

            DestroyStamp(stamp);
        }

        [Test]
        public void Solve_ZeroDensity_PlacesNone()
        {
            var stamp = CreateTestStamp("temple", new TagEntry("sacred", 0.9f));
            SetZoneDensity(0f);

            int placed = m_Solver.Solve(m_Cells, new[] { stamp }, m_Zone, null, new System.Random(42));
            Assert.AreEqual(0, placed, "密度0では配置されない");

            DestroyStamp(stamp);
        }

        #endregion

        #region Determinism Tests

        [Test]
        public void Solve_SameSeed_SameResult()
        {
            var stamp = CreateTestStamp("temple", new TagEntry("sacred", 0.9f));
            SetZoneDensity(0.5f);
            SetZoneBias(new TagEntry("sacred", 0.8f));

            int placed1 = m_Solver.Solve(m_Cells, new[] { stamp }, m_Zone, null, new System.Random(42));
            int count1 = m_Registry.Count;

            // リセット
            m_Registry.Clear();

            int placed2 = m_Solver.Solve(m_Cells, new[] { stamp }, m_Zone, null, new System.Random(42));
            Assert.AreEqual(placed1, placed2, "同じシードは同じ配置数");

            DestroyStamp(stamp);
        }

        #endregion

        #region Evaluate Tests

        [Test]
        public void Evaluate_HighBiasMatch_HighBiasScore()
        {
            var stamp = CreateTestStamp("temple", new TagEntry("sacred", 0.9f));
            SetZoneBias(new TagEntry("sacred", 0.8f));

            PlacementCandidate candidate = m_Solver.Evaluate(
                m_Cells[0], stamp, m_Zone, null, m_Cells);

            Assert.Greater(candidate.biasScore, 0.5f, "ゾーンバイアスと一致するスタンプは高スコア");

            DestroyStamp(stamp);
        }

        [Test]
        public void Evaluate_LowBiasMatch_LowBiasScore()
        {
            var stamp = CreateTestStamp("fortress", new TagEntry("fortified", 0.9f));
            SetZoneBias(new TagEntry("sacred", 0.8f));

            PlacementCandidate candidate = m_Solver.Evaluate(
                m_Cells[0], stamp, m_Zone, null, m_Cells);

            Assert.AreEqual(0f, candidate.biasScore, 0.001f, "ゾーンバイアスと不一致はスコア0");

            DestroyStamp(stamp);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// テスト用の PrefabStampDefinition を作成する。
        /// Prefab に空の GameObject を設定し、MeshFilter を追加して IsValid() = true にする。
        /// </summary>
        private PrefabStampDefinition CreateTestStamp(string _name, params TagEntry[] _tags)
        {
            var stamp = ScriptableObject.CreateInstance<PrefabStampDefinition>();

            // テスト用の Prefab を作成
            GameObject prefab = new GameObject(_name);
            prefab.AddComponent<MeshFilter>();

            // SerializedObject 経由で private フィールドを設定
            var so = new UnityEditor.SerializedObject(stamp);
            so.FindProperty("m_Prefab").objectReferenceValue = prefab;
            so.FindProperty("m_DisplayName").stringValue = _name;

            // タグプロファイルを設定
            if (_tags != null && _tags.Length > 0)
            {
                var tagProfileProp = so.FindProperty("m_TagProfile");
                var tagsProp = tagProfileProp.FindPropertyRelative("m_Tags");
                tagsProp.arraySize = _tags.Length;
                for (int i = 0; i < _tags.Length; i++)
                {
                    var element = tagsProp.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("tagName").stringValue = _tags[i].tagName;
                    element.FindPropertyRelative("weight").floatValue = _tags[i].weight;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return stamp;
        }

        private void DestroyStamp(PrefabStampDefinition _stamp)
        {
            if (_stamp != null)
            {
                // Prefab の GameObject も破棄
                var so = new UnityEditor.SerializedObject(_stamp);
                var prefabProp = so.FindProperty("m_Prefab");
                if (prefabProp.objectReferenceValue != null)
                {
                    Object.DestroyImmediate(prefabProp.objectReferenceValue);
                }
                Object.DestroyImmediate(_stamp);
            }
        }

        private void SetZoneDensity(float _density)
        {
            var so = new UnityEditor.SerializedObject(m_Zone);
            so.FindProperty("m_Density").floatValue = _density;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void SetZoneBias(params TagEntry[] _tags)
        {
            var so = new UnityEditor.SerializedObject(m_Zone);
            var biasProp = so.FindProperty("m_ZoneBias");
            var tagsProp = biasProp.FindPropertyRelative("m_Tags");
            tagsProp.arraySize = _tags.Length;
            for (int i = 0; i < _tags.Length; i++)
            {
                var element = tagsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("tagName").stringValue = _tags[i].tagName;
                element.FindPropertyRelative("weight").floatValue = _tags[i].weight;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion
    }
}
