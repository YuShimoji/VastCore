using System.Collections.Generic;
using NUnit.Framework;
using Vastcore.Generation;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// ComponentSelector のバリエーション選択ロジックの EditMode テスト。
    /// </summary>
    public class ComponentSelectorTests
    {
        private ComponentSelector m_Selector;

        [SetUp]
        public void SetUp()
        {
            m_Selector = BuiltInComponentVariants.CreateDefaultSelector();
        }

        #region Registration Tests

        [Test]
        public void CreateDefaultSelector_HasVariants()
        {
            Assert.Greater(m_Selector.Count, 0, "初期バリエーションが登録されている");
        }

        [Test]
        public void CreateDefaultSelector_HasWindowVariants()
        {
            var profile = new StructureTagProfile(new TagEntry("ornate", 0.5f));
            var result = m_Selector.Select(profile, ComponentType.Window, new System.Random(42));
            Assert.IsNotNull(result, "Windowバリエーションが選択可能");
        }

        #endregion

        #region Selection Tests

        [Test]
        public void Select_CathedralProfile_PrefersGothicWindow()
        {
            var cathedral = StructureTagAdapter.GetDefaultProfile(ArchitecturalType.Cathedral);
            var counts = new Dictionary<string, int>();

            // 100回選択して傾向を見る
            for (int i = 0; i < 100; i++)
            {
                var result = m_Selector.Select(cathedral, ComponentType.Window, new System.Random(i));
                if (result != null)
                {
                    counts.TryGetValue(result.variantName, out int c);
                    counts[result.variantName] = c + 1;
                }
            }

            // GothicWindow が最も多く選択されるはず (sacred+ornate が高い)
            Assert.IsTrue(counts.ContainsKey("GothicWindow"), "GothicWindow が選択される");
            int gothicCount = counts["GothicWindow"];
            Assert.Greater(gothicCount, 20, "GothicWindow が相当数選択される");
        }

        [Test]
        public void Select_FortressProfile_PrefersSlitWindow()
        {
            var fortress = StructureTagAdapter.GetDefaultProfile(CompoundArchitecturalType.FortressWall);
            var counts = new Dictionary<string, int>();

            for (int i = 0; i < 100; i++)
            {
                var result = m_Selector.Select(fortress, ComponentType.Window, new System.Random(i));
                if (result != null)
                {
                    counts.TryGetValue(result.variantName, out int c);
                    counts[result.variantName] = c + 1;
                }
            }

            Assert.IsTrue(counts.ContainsKey("SlitWindow"), "SlitWindow が選択される");
            int slitCount = counts["SlitWindow"];
            Assert.Greater(slitCount, 20, "SlitWindow が相当数選択される");
        }

        [Test]
        public void Select_CathedralProfile_PrefersDomeRoof()
        {
            var cathedral = StructureTagAdapter.GetDefaultProfile(ArchitecturalType.Cathedral);
            var counts = new Dictionary<string, int>();

            for (int i = 0; i < 100; i++)
            {
                var result = m_Selector.Select(cathedral, ComponentType.Roof, new System.Random(i));
                if (result != null)
                {
                    counts.TryGetValue(result.variantName, out int c);
                    counts[result.variantName] = c + 1;
                }
            }

            // DomeRoof か SpireRoof が多いはず
            int domeOrSpire = 0;
            counts.TryGetValue("DomeRoof", out int d);
            counts.TryGetValue("SpireRoof", out int s);
            domeOrSpire = d + s;
            Assert.Greater(domeOrSpire, 40, "DomeRoof/SpireRoof が多く選択される");
        }

        [Test]
        public void Select_FortressProfile_PrefersBattlementRoof()
        {
            var fortress = StructureTagAdapter.GetDefaultProfile(CompoundArchitecturalType.FortressWall);
            var counts = new Dictionary<string, int>();

            for (int i = 0; i < 100; i++)
            {
                var result = m_Selector.Select(fortress, ComponentType.Roof, new System.Random(i));
                if (result != null)
                {
                    counts.TryGetValue(result.variantName, out int c);
                    counts[result.variantName] = c + 1;
                }
            }

            Assert.IsTrue(counts.ContainsKey("BattlementRoof"), "BattlementRoof が選択される");
            int battlementCount = counts["BattlementRoof"];
            Assert.Greater(battlementCount, 20, "BattlementRoof が相当数選択される");
        }

        [Test]
        public void Select_NullProfile_ReturnsNull()
        {
            var result = m_Selector.Select(null, ComponentType.Window, new System.Random(42));
            Assert.IsNull(result);
        }

        [Test]
        public void Select_NullRandom_ReturnsNull()
        {
            var profile = new StructureTagProfile(new TagEntry("ornate", 0.5f));
            var result = m_Selector.Select(profile, ComponentType.Window, null);
            Assert.IsNull(result);
        }

        [Test]
        public void Select_NoVariantsForType_ReturnsNull()
        {
            // Vent には組み込みバリエーションがない
            var profile = new StructureTagProfile(new TagEntry("ornate", 0.5f));
            var result = m_Selector.Select(profile, ComponentType.Vent, new System.Random(42));
            Assert.IsNull(result, "Vent にはバリエーションがないため null");
        }

        #endregion

        #region Category Selection Tests

        [Test]
        public void SelectCategory_Shell_ReturnsMultipleTypes()
        {
            var cathedral = StructureTagAdapter.GetDefaultProfile(ArchitecturalType.Cathedral);
            var result = m_Selector.SelectCategory(cathedral, ComponentCategory.Shell, new System.Random(42));

            Assert.Greater(result.Count, 0, "Shell カテゴリでバリエーションが選択される");
            Assert.IsTrue(result.ContainsKey(ComponentType.Wall), "Wall が選択される");
            Assert.IsTrue(result.ContainsKey(ComponentType.Roof), "Roof が選択される");
        }

        [Test]
        public void SelectAll_ReturnsMultipleCategories()
        {
            var cathedral = StructureTagAdapter.GetDefaultProfile(ArchitecturalType.Cathedral);
            var result = m_Selector.SelectAll(cathedral, new System.Random(42));

            Assert.Greater(result.Count, 3, "複数カテゴリからバリエーションが選択される");
        }

        #endregion

        #region Determinism Tests

        [Test]
        public void Select_SameSeed_ReturnsSameResult()
        {
            var profile = StructureTagAdapter.GetDefaultProfile(ArchitecturalType.Cathedral);

            var result1 = m_Selector.Select(profile, ComponentType.Window, new System.Random(42));
            var result2 = m_Selector.Select(profile, ComponentType.Window, new System.Random(42));

            Assert.AreEqual(result1.variantName, result2.variantName, "同じシードは同じ結果");
        }

        #endregion

        #region Helper Tests

        [Test]
        public void GetTypesForCategory_Shell_Returns4()
        {
            var types = ComponentSelector.GetTypesForCategory(ComponentCategory.Shell);
            Assert.AreEqual(4, types.Length);
        }

        [Test]
        public void GetTypesForCategory_Aperture_Returns3()
        {
            var types = ComponentSelector.GetTypesForCategory(ComponentCategory.Aperture);
            Assert.AreEqual(3, types.Length);
        }

        [Test]
        public void GetTypesForCategory_Ornament_Returns6()
        {
            var types = ComponentSelector.GetTypesForCategory(ComponentCategory.Ornament);
            Assert.AreEqual(6, types.Length);
        }

        #endregion
    }
}
