using NUnit.Framework;
using Vastcore.Generation;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// StructureTagAdapter の EditMode テスト。
    /// 全enumの値に対してデフォルトプロファイルが取得でき、
    /// プロファイルが期待される特性を持つことを検証する。
    /// </summary>
    public class StructureTagAdapterTests
    {
        #region ArchitecturalType Tests

        [Test]
        public void ArchitecturalType_AllValues_ReturnNonEmptyProfile()
        {
            foreach (ArchitecturalType type in System.Enum.GetValues(typeof(ArchitecturalType)))
            {
                var profile = StructureTagAdapter.GetDefaultProfile(type);
                Assert.IsFalse(profile.IsEmpty, $"ArchitecturalType.{type} のプロファイルが空");
            }
        }

        [Test]
        public void ArchitecturalType_Cathedral_HasSacredTag()
        {
            var profile = StructureTagAdapter.GetDefaultProfile(ArchitecturalType.Cathedral);
            Assert.Greater(profile.GetWeight(StructureTagAdapter.TAG_SACRED), 0.8f,
                "Cathedral は sacred タグが高い");
            Assert.Greater(profile.GetWeight(StructureTagAdapter.TAG_ORNATE), 0.7f,
                "Cathedral は ornate タグが高い");
        }

        [Test]
        public void ArchitecturalType_Bridge_HasBridgeTag()
        {
            var profile = StructureTagAdapter.GetDefaultProfile(ArchitecturalType.Bridge);
            Assert.Greater(profile.GetWeight(StructureTagAdapter.TAG_BRIDGE), 0.8f,
                "Bridge は bridge タグが高い");
            Assert.Greater(profile.GetWeight(StructureTagAdapter.TAG_FUNCTIONAL), 0.5f,
                "Bridge は functional タグがある");
        }

        #endregion

        #region CompoundArchitecturalType Tests

        [Test]
        public void CompoundArchitecturalType_AllValues_ReturnNonEmptyProfile()
        {
            foreach (CompoundArchitecturalType type in System.Enum.GetValues(typeof(CompoundArchitecturalType)))
            {
                var profile = StructureTagAdapter.GetDefaultProfile(type);
                Assert.IsFalse(profile.IsEmpty, $"CompoundArchitecturalType.{type} のプロファイルが空");
            }
        }

        [Test]
        public void CompoundArchitecturalType_FortressWall_HasFortifiedTag()
        {
            var profile = StructureTagAdapter.GetDefaultProfile(CompoundArchitecturalType.FortressWall);
            Assert.Greater(profile.GetWeight(StructureTagAdapter.TAG_FORTIFIED), 0.9f,
                "FortressWall は fortified タグが最大級");
            Assert.Greater(profile.GetWeight(StructureTagAdapter.TAG_WALL), 0.8f,
                "FortressWall は wall タグが高い");
        }

        #endregion

        #region PrimitiveType Tests

        [Test]
        public void PrimitiveType_AllValues_ReturnNonEmptyProfile()
        {
            foreach (PrimitiveType type in System.Enum.GetValues(typeof(PrimitiveType)))
            {
                var profile = StructureTagAdapter.GetDefaultProfile(type);
                Assert.IsFalse(profile.IsEmpty, $"PrimitiveType.{type} のプロファイルが空");
            }
        }

        [Test]
        public void PrimitiveType_Crystal_HasCrystalTag()
        {
            var profile = StructureTagAdapter.GetDefaultProfile(PrimitiveType.Crystal);
            Assert.Greater(profile.GetWeight(StructureTagAdapter.TAG_CRYSTAL), 0.8f,
                "Crystal は crystal タグが高い");
        }

        [Test]
        public void PrimitiveType_Monolith_HasMassiveTag()
        {
            var profile = StructureTagAdapter.GetDefaultProfile(PrimitiveType.Monolith);
            Assert.Greater(profile.GetWeight(StructureTagAdapter.TAG_MASSIVE), 0.9f,
                "Monolith は massive タグが最大級");
        }

        #endregion

        #region CrystalSystem Tests

        [Test]
        public void CrystalSystem_AllValues_ReturnNonEmptyProfile()
        {
            foreach (CrystalSystem system in System.Enum.GetValues(typeof(CrystalSystem)))
            {
                var profile = StructureTagAdapter.GetDefaultProfile(system);
                Assert.IsFalse(profile.IsEmpty, $"CrystalSystem.{system} のプロファイルが空");
            }
        }

        [Test]
        public void CrystalSystem_AllValues_HaveCrystalTag()
        {
            foreach (CrystalSystem system in System.Enum.GetValues(typeof(CrystalSystem)))
            {
                var profile = StructureTagAdapter.GetDefaultProfile(system);
                Assert.Greater(profile.GetWeight(StructureTagAdapter.TAG_CRYSTAL), 0.8f,
                    $"CrystalSystem.{system} は crystal タグが高い");
            }
        }

        #endregion

        #region Cross-Type Tests

        [Test]
        public void CathedralAndCathedralComplex_HighBlendScore()
        {
            var cathedral = StructureTagAdapter.GetDefaultProfile(ArchitecturalType.Cathedral);
            var cathedralComplex = StructureTagAdapter.GetDefaultProfile(
                CompoundArchitecturalType.CathedralComplex);

            float score = cathedral.BlendScore(cathedralComplex);
            Assert.Greater(score, 0.8f,
                "Cathedral と CathedralComplex は高いブレンドスコア");
        }

        [Test]
        public void BridgeAndMultipleBridge_HighBlendScore()
        {
            var bridge = StructureTagAdapter.GetDefaultProfile(ArchitecturalType.Bridge);
            var multipleBridge = StructureTagAdapter.GetDefaultProfile(
                CompoundArchitecturalType.MultipleBridge);

            float score = bridge.BlendScore(multipleBridge);
            Assert.Greater(score, 0.8f,
                "Bridge と MultipleBridge は高いブレンドスコア");
        }

        [Test]
        public void CathedralAndFortress_LowBlendScore()
        {
            var cathedral = StructureTagAdapter.GetDefaultProfile(ArchitecturalType.Cathedral);
            var fortress = StructureTagAdapter.GetDefaultProfile(
                CompoundArchitecturalType.FortressWall);

            float score = cathedral.BlendScore(fortress);
            Assert.Less(score, 0.5f,
                "Cathedral と FortressWall は低いブレンドスコア");
        }

        [Test]
        public void CrystalAndPrimitiveCrystal_HighBlendScore()
        {
            var crystalSystem = StructureTagAdapter.GetDefaultProfile(CrystalSystem.Cubic);
            var primitiveCrystal = StructureTagAdapter.GetDefaultProfile(PrimitiveType.Crystal);

            float score = crystalSystem.BlendScore(primitiveCrystal);
            Assert.Greater(score, 0.7f,
                "Cubic結晶系 と Crystal プリミティブは高いブレンドスコア");
        }

        #endregion

        #region Utility Tests

        [Test]
        public void GetBuiltInTagNames_Returns20Tags()
        {
            var tags = StructureTagAdapter.GetBuiltInTagNames();
            Assert.AreEqual(20, tags.Count, "組み込みタグは20種");
        }

        #endregion
    }
}
