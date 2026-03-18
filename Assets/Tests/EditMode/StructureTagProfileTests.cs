using NUnit.Framework;
using Vastcore.Generation;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// StructureTagProfile および StructureTagPreset の EditMode テスト。
    /// BlendScore (コサイン類似度) の正確性を検証する。
    /// </summary>
    public class StructureTagProfileTests
    {
        #region TagEntry Tests

        [Test]
        public void TagEntry_Constructor_ClampsWeight()
        {
            var entry = new TagEntry("test", 1.5f);
            Assert.AreEqual(1.0f, entry.weight, 0.001f);

            var entry2 = new TagEntry("test", -0.5f);
            Assert.AreEqual(0.0f, entry2.weight, 0.001f);
        }

        #endregion

        #region GetWeight / SetWeight Tests

        [Test]
        public void GetWeight_UndefinedTag_ReturnsZero()
        {
            var profile = new StructureTagProfile();
            Assert.AreEqual(0f, profile.GetWeight("nonexistent"));
        }

        [Test]
        public void GetWeight_NullOrEmpty_ReturnsZero()
        {
            var profile = new StructureTagProfile();
            Assert.AreEqual(0f, profile.GetWeight(null));
            Assert.AreEqual(0f, profile.GetWeight(""));
        }

        [Test]
        public void SetWeight_AddsNewTag()
        {
            var profile = new StructureTagProfile();
            profile.SetWeight("arch", 0.8f);

            Assert.AreEqual(0.8f, profile.GetWeight("arch"), 0.001f);
            Assert.AreEqual(1, profile.TagCount);
        }

        [Test]
        public void SetWeight_UpdatesExistingTag()
        {
            var profile = new StructureTagProfile();
            profile.SetWeight("arch", 0.5f);
            profile.SetWeight("arch", 0.9f);

            Assert.AreEqual(0.9f, profile.GetWeight("arch"), 0.001f);
            Assert.AreEqual(1, profile.TagCount);
        }

        [Test]
        public void SetWeight_ZeroRemovesTag()
        {
            var profile = new StructureTagProfile();
            profile.SetWeight("arch", 0.8f);
            Assert.AreEqual(1, profile.TagCount);

            profile.SetWeight("arch", 0f);
            Assert.AreEqual(0, profile.TagCount);
            Assert.AreEqual(0f, profile.GetWeight("arch"));
        }

        [Test]
        public void SetWeight_CaseInsensitive()
        {
            var profile = new StructureTagProfile();
            profile.SetWeight("Arch", 0.8f);

            Assert.AreEqual(0.8f, profile.GetWeight("arch"), 0.001f);
            Assert.AreEqual(0.8f, profile.GetWeight("ARCH"), 0.001f);
        }

        [Test]
        public void SetWeight_ClampsValue()
        {
            var profile = new StructureTagProfile();
            profile.SetWeight("test", 2.0f);
            Assert.AreEqual(1.0f, profile.GetWeight("test"), 0.001f);

            profile.SetWeight("test2", -1.0f);
            // -1.0 is clamped to 0, so tag is removed
            Assert.AreEqual(0f, profile.GetWeight("test2"));
        }

        #endregion

        #region BlendScore Tests

        [Test]
        public void BlendScore_IdenticalProfiles_ReturnsOne()
        {
            var a = new StructureTagProfile(
                new TagEntry("arch", 0.8f),
                new TagEntry("massive", 0.6f));
            var b = new StructureTagProfile(
                new TagEntry("arch", 0.8f),
                new TagEntry("massive", 0.6f));

            float score = a.BlendScore(b);
            Assert.AreEqual(1.0f, score, 0.001f);
        }

        [Test]
        public void BlendScore_OrthogonalProfiles_ReturnsZero()
        {
            var a = new StructureTagProfile(
                new TagEntry("arch", 1.0f));
            var b = new StructureTagProfile(
                new TagEntry("tower", 1.0f));

            float score = a.BlendScore(b);
            Assert.AreEqual(0f, score, 0.001f);
        }

        [Test]
        public void BlendScore_NullOther_ReturnsZero()
        {
            var a = new StructureTagProfile(
                new TagEntry("arch", 0.8f));

            Assert.AreEqual(0f, a.BlendScore(null));
        }

        [Test]
        public void BlendScore_EmptyProfile_ReturnsZero()
        {
            var a = new StructureTagProfile(
                new TagEntry("arch", 0.8f));
            var b = new StructureTagProfile();

            Assert.AreEqual(0f, a.BlendScore(b));
        }

        [Test]
        public void BlendScore_BothEmpty_ReturnsZero()
        {
            var a = new StructureTagProfile();
            var b = new StructureTagProfile();

            Assert.AreEqual(0f, a.BlendScore(b));
        }

        [Test]
        public void BlendScore_PartialOverlap_ReturnsBetweenZeroAndOne()
        {
            // A: arch=0.8, massive=0.6
            // B: arch=0.8, ornate=0.7
            // 共通: arch のみ (0.8*0.8 = 0.64)
            // |A| = sqrt(0.64 + 0.36) = 1.0
            // |B| = sqrt(0.64 + 0.49) = sqrt(1.13) ≈ 1.063
            // score = 0.64 / (1.0 * 1.063) ≈ 0.602
            var a = new StructureTagProfile(
                new TagEntry("arch", 0.8f),
                new TagEntry("massive", 0.6f));
            var b = new StructureTagProfile(
                new TagEntry("arch", 0.8f),
                new TagEntry("ornate", 0.7f));

            float score = a.BlendScore(b);
            Assert.Greater(score, 0f);
            Assert.Less(score, 1f);
            Assert.AreEqual(0.602f, score, 0.01f);
        }

        [Test]
        public void BlendScore_IsSymmetric()
        {
            var a = new StructureTagProfile(
                new TagEntry("arch", 0.8f),
                new TagEntry("massive", 0.6f));
            var b = new StructureTagProfile(
                new TagEntry("massive", 0.9f),
                new TagEntry("ornate", 0.5f));

            float scoreAB = a.BlendScore(b);
            float scoreBA = b.BlendScore(a);
            Assert.AreEqual(scoreAB, scoreBA, 0.001f);
        }

        [Test]
        public void BlendScore_ProportionalProfiles_ReturnsOne()
        {
            // コサイン類似度は方向のみ比較 → 比例ベクトルは類似度1.0
            var a = new StructureTagProfile(
                new TagEntry("arch", 0.4f),
                new TagEntry("massive", 0.3f));
            var b = new StructureTagProfile(
                new TagEntry("arch", 0.8f),
                new TagEntry("massive", 0.6f));

            float score = a.BlendScore(b);
            Assert.AreEqual(1.0f, score, 0.001f);
        }

        [Test]
        public void BlendScore_CathedralVsFortress_LowScore()
        {
            // 大聖堂プリセット
            var cathedral = new StructureTagProfile(
                new TagEntry("arch", 0.7f),
                new TagEntry("dome", 0.5f),
                new TagEntry("massive", 0.8f),
                new TagEntry("ornate", 0.9f),
                new TagEntry("sacred", 0.95f));

            // 要塞プリセット
            var fortress = new StructureTagProfile(
                new TagEntry("wall", 0.9f),
                new TagEntry("tower", 0.7f),
                new TagEntry("massive", 0.95f),
                new TagEntry("fortified", 0.95f),
                new TagEntry("functional", 0.6f));

            float score = cathedral.BlendScore(fortress);
            // 共通タグは massive のみ → 低スコア
            Assert.Less(score, 0.5f);
        }

        [Test]
        public void BlendScore_CathedralVsBasilica_HighScore()
        {
            var cathedral = new StructureTagProfile(
                new TagEntry("arch", 0.7f),
                new TagEntry("dome", 0.5f),
                new TagEntry("massive", 0.8f),
                new TagEntry("ornate", 0.9f),
                new TagEntry("sacred", 0.95f));

            var basilica = new StructureTagProfile(
                new TagEntry("arch", 0.6f),
                new TagEntry("dome", 0.4f),
                new TagEntry("massive", 0.7f),
                new TagEntry("ornate", 0.7f),
                new TagEntry("sacred", 0.8f));

            float score = cathedral.BlendScore(basilica);
            // ほぼ同じタグ構成 → 高スコア
            Assert.Greater(score, 0.9f);
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Constructor_ParamsArray_SetsAllTags()
        {
            var profile = new StructureTagProfile(
                new TagEntry("arch", 0.8f),
                new TagEntry("massive", 0.6f),
                new TagEntry("ornate", 0.4f));

            Assert.AreEqual(3, profile.TagCount);
            Assert.AreEqual(0.8f, profile.GetWeight("arch"), 0.001f);
            Assert.AreEqual(0.6f, profile.GetWeight("massive"), 0.001f);
            Assert.AreEqual(0.4f, profile.GetWeight("ornate"), 0.001f);
        }

        [Test]
        public void Constructor_Default_IsEmpty()
        {
            var profile = new StructureTagProfile();
            Assert.IsTrue(profile.IsEmpty);
            Assert.AreEqual(0, profile.TagCount);
        }

        #endregion

        #region Utility Tests

        [Test]
        public void Clear_RemovesAllTags()
        {
            var profile = new StructureTagProfile(
                new TagEntry("arch", 0.8f),
                new TagEntry("massive", 0.6f));

            profile.Clear();
            Assert.IsTrue(profile.IsEmpty);
            Assert.AreEqual(0, profile.TagCount);
        }

        [Test]
        public void ToString_EmptyProfile()
        {
            var profile = new StructureTagProfile();
            Assert.AreEqual("TagProfile[empty]", profile.ToString());
        }

        [Test]
        public void ToString_WithTags()
        {
            var profile = new StructureTagProfile(new TagEntry("arch", 0.8f));
            string result = profile.ToString();
            Assert.IsTrue(result.Contains("arch"));
            Assert.IsTrue(result.Contains("0.80"));
        }

        [Test]
        public void GetAllTags_ReturnsReadOnlyList()
        {
            var profile = new StructureTagProfile(
                new TagEntry("arch", 0.8f),
                new TagEntry("massive", 0.6f));

            var tags = profile.GetAllTags();
            Assert.AreEqual(2, tags.Count);
        }

        #endregion
    }
}
