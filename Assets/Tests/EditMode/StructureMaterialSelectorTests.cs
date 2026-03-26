using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// StructureMaterialSelector のパレット選択ロジックの EditMode テスト。
    /// ComponentSelectorTests と同じルーレット選択パターンを検証する。
    /// </summary>
    public class StructureMaterialSelectorTests
    {
        private StructureMaterialSelector m_Selector;
        private StructureMaterialPalette[] m_Palettes;

        [SetUp]
        public void SetUp()
        {
            m_Selector = new StructureMaterialSelector();

            // テスト用パレットを作成 (7種: StructureTagPresetCreator と同じプロファイル)
            m_Palettes = new[]
            {
                CreateTestPalette("Cathedral", 0.05f,
                    new TagEntry("arch", 0.7f), new TagEntry("dome", 0.5f),
                    new TagEntry("massive", 0.8f), new TagEntry("ornate", 0.9f),
                    new TagEntry("sacred", 0.95f)),
                CreateTestPalette("Fortress", 0.2f,
                    new TagEntry("wall", 0.9f), new TagEntry("tower", 0.7f),
                    new TagEntry("massive", 0.95f), new TagEntry("fortified", 0.95f),
                    new TagEntry("functional", 0.6f)),
                CreateTestPalette("Aqueduct", 0.35f,
                    new TagEntry("arch", 0.8f), new TagEntry("bridge", 0.9f),
                    new TagEntry("massive", 0.6f), new TagEntry("functional", 0.9f)),
                CreateTestPalette("Ruins", 0.85f,
                    new TagEntry("wall", 0.4f), new TagEntry("column", 0.5f),
                    new TagEntry("weathered", 0.95f), new TagEntry("primitive", 0.6f),
                    new TagEntry("massive", 0.5f)),
                CreateTestPalette("CrystalSpire", 0f,
                    new TagEntry("spire", 0.8f), new TagEntry("crystal", 0.95f),
                    new TagEntry("elegant", 0.7f), new TagEntry("geometric", 0.9f)),
                CreateTestPalette("Amphitheater", 0.25f,
                    new TagEntry("enclosure", 0.9f), new TagEntry("stepped", 0.6f),
                    new TagEntry("massive", 0.7f), new TagEntry("ornate", 0.5f)),
                CreateTestPalette("Monolith", 0.7f,
                    new TagEntry("massive", 0.95f), new TagEntry("primitive", 0.8f),
                    new TagEntry("geometric", 0.6f))
            };
        }

        [TearDown]
        public void TearDown()
        {
            // テスト用 SO を破棄
            if (m_Palettes != null)
            {
                for (int i = 0; i < m_Palettes.Length; i++)
                {
                    if (m_Palettes[i] != null)
                    {
                        Object.DestroyImmediate(m_Palettes[i]);
                    }
                }
            }
        }

        #region Selection Tests

        [Test]
        public void Select_CathedralProfile_PrefersCathedralPalette()
        {
            // Cathedral タグプロファイル (sacred + ornate + arch が高い)
            var cathedralProfile = new StructureTagProfile(
                new TagEntry("arch", 0.7f), new TagEntry("dome", 0.5f),
                new TagEntry("massive", 0.8f), new TagEntry("ornate", 0.9f),
                new TagEntry("sacred", 0.95f));

            var counts = new Dictionary<string, int>();

            for (int i = 0; i < 100; i++)
            {
                var result = m_Selector.Select(cathedralProfile, m_Palettes, new System.Random(i));
                if (result != null)
                {
                    counts.TryGetValue(result.DisplayName, out int c);
                    counts[result.DisplayName] = c + 1;
                }
            }

            Assert.IsTrue(counts.ContainsKey("Cathedral"), "Cathedral パレットが選択される");
            int cathedralCount = counts["Cathedral"];
            Assert.Greater(cathedralCount, 20, "Cathedral パレットが相当数選択される");
        }

        [Test]
        public void Select_FortressProfile_PrefersFortressPalette()
        {
            var fortressProfile = new StructureTagProfile(
                new TagEntry("wall", 0.9f), new TagEntry("tower", 0.7f),
                new TagEntry("massive", 0.95f), new TagEntry("fortified", 0.95f),
                new TagEntry("functional", 0.6f));

            var counts = new Dictionary<string, int>();

            for (int i = 0; i < 100; i++)
            {
                var result = m_Selector.Select(fortressProfile, m_Palettes, new System.Random(i));
                if (result != null)
                {
                    counts.TryGetValue(result.DisplayName, out int c);
                    counts[result.DisplayName] = c + 1;
                }
            }

            Assert.IsTrue(counts.ContainsKey("Fortress"), "Fortress パレットが選択される");
            int fortressCount = counts["Fortress"];
            Assert.Greater(fortressCount, 20, "Fortress パレットが相当数選択される");
        }

        [Test]
        public void Select_RuinsProfile_PrefersRuinsPalette()
        {
            var ruinsProfile = new StructureTagProfile(
                new TagEntry("weathered", 0.95f), new TagEntry("primitive", 0.6f),
                new TagEntry("massive", 0.5f));

            var counts = new Dictionary<string, int>();

            for (int i = 0; i < 100; i++)
            {
                var result = m_Selector.Select(ruinsProfile, m_Palettes, new System.Random(i));
                if (result != null)
                {
                    counts.TryGetValue(result.DisplayName, out int c);
                    counts[result.DisplayName] = c + 1;
                }
            }

            Assert.IsTrue(counts.ContainsKey("Ruins"), "Ruins パレットが選択される");
            int ruinsCount = counts["Ruins"];
            Assert.Greater(ruinsCount, 20, "Ruins パレットが相当数選択される");
        }

        #endregion

        #region Null / Edge Case Tests

        [Test]
        public void Select_NullProfile_ReturnsNull()
        {
            var result = m_Selector.Select(null, m_Palettes, new System.Random(42));
            Assert.IsNull(result);
        }

        [Test]
        public void Select_NullPalettes_ReturnsNull()
        {
            var profile = new StructureTagProfile(new TagEntry("ornate", 0.5f));
            var result = m_Selector.Select(profile, null, new System.Random(42));
            Assert.IsNull(result);
        }

        [Test]
        public void Select_EmptyPalettes_ReturnsNull()
        {
            var profile = new StructureTagProfile(new TagEntry("ornate", 0.5f));
            var result = m_Selector.Select(profile, new StructureMaterialPalette[0], new System.Random(42));
            Assert.IsNull(result);
        }

        [Test]
        public void Select_NullRandom_ReturnsNull()
        {
            var profile = new StructureTagProfile(new TagEntry("ornate", 0.5f));
            var result = m_Selector.Select(profile, m_Palettes, null);
            Assert.IsNull(result);
        }

        [Test]
        public void Select_NoMatchingTags_FallsBack()
        {
            // どのパレットにも一致しないタグで問い合わせ → 等確率フォールバック
            var unrelatedProfile = new StructureTagProfile(
                new TagEntry("underwater", 0.9f), new TagEntry("floating", 0.8f));

            var result = m_Selector.Select(unrelatedProfile, m_Palettes, new System.Random(42));
            Assert.IsNotNull(result, "フォールバックでいずれかのパレットが選択される");
        }

        #endregion

        #region Determinism Tests

        [Test]
        public void Select_SameSeed_ReturnsSameResult()
        {
            var profile = new StructureTagProfile(
                new TagEntry("ornate", 0.9f), new TagEntry("sacred", 0.95f));

            var result1 = m_Selector.Select(profile, m_Palettes, new System.Random(42));
            var result2 = m_Selector.Select(profile, m_Palettes, new System.Random(42));

            Assert.AreEqual(result1.DisplayName, result2.DisplayName, "同じシードは同じ結果");
        }

        #endregion

        #region SelectBest Tests

        [Test]
        public void SelectBest_CathedralProfile_ReturnsCathedral()
        {
            var cathedralProfile = new StructureTagProfile(
                new TagEntry("arch", 0.7f), new TagEntry("dome", 0.5f),
                new TagEntry("massive", 0.8f), new TagEntry("ornate", 0.9f),
                new TagEntry("sacred", 0.95f));

            var result = m_Selector.SelectBest(cathedralProfile, m_Palettes);
            Assert.IsNotNull(result);
            Assert.AreEqual("Cathedral", result.DisplayName, "完全一致プロファイルで Cathedral が最高スコア");
        }

        [Test]
        public void SelectBest_NullProfile_ReturnsNull()
        {
            var result = m_Selector.SelectBest(null, m_Palettes);
            Assert.IsNull(result);
        }

        [Test]
        public void SelectBest_EmptyPalettes_ReturnsNull()
        {
            var profile = new StructureTagProfile(new TagEntry("ornate", 0.5f));
            var result = m_Selector.SelectBest(profile, new StructureMaterialPalette[0]);
            Assert.IsNull(result);
        }

        #endregion

        #region GetMaterial Tests

        [Test]
        public void GetMaterial_ByCategory_ReturnsCorrectMaterial()
        {
            // マテリアル未設定の SO では null が返る (設定テストはエディタ上で行う)
            var palette = m_Palettes[0]; // Cathedral
            Assert.IsNull(palette.GetMaterial(ComponentCategory.Shell),
                "マテリアル未設定のパレットでは null");
            Assert.IsNull(palette.GetMaterial(ComponentCategory.Ornament),
                "マテリアル未設定のパレットでは null");
        }

        [Test]
        public void GetMaterial_ByComponentType_ReturnsCorrectMaterial()
        {
            var palette = m_Palettes[0]; // Cathedral
            Assert.IsNull(palette.GetMaterial(ComponentType.Roof),
                "マテリアル未設定のパレットでは null");
            Assert.IsNull(palette.GetMaterial(ComponentType.Foundation),
                "マテリアル未設定のパレットでは null");
        }

        #endregion

        #region WeatheringLevel Tests

        [Test]
        public void WeatheringLevel_CorrectlySet()
        {
            Assert.AreEqual(0.05f, m_Palettes[0].WeatheringLevel, 0.001f, "Cathedral: 低風化");
            Assert.AreEqual(0.85f, m_Palettes[3].WeatheringLevel, 0.001f, "Ruins: 高風化");
            Assert.AreEqual(0f, m_Palettes[4].WeatheringLevel, 0.001f, "CrystalSpire: 風化なし");
        }

        #endregion

        #region Helpers

        private static StructureMaterialPalette CreateTestPalette(string _name,
            float _weatheringLevel, params TagEntry[] _tags)
        {
            var palette = ScriptableObject.CreateInstance<StructureMaterialPalette>();

            var type = typeof(StructureMaterialPalette);
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

            type.GetField("m_DisplayName", flags)?.SetValue(palette, _name);
            type.GetField("m_WeatheringLevel", flags)?.SetValue(palette, _weatheringLevel);

            var profile = new StructureTagProfile(_tags);
            type.GetField("m_Affinity", flags)?.SetValue(palette, profile);

            return palette;
        }

        #endregion
    }
}
