using NUnit.Framework;
using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Tests.EditMode
{
    /// <summary>
    /// AdjacencyRuleSet の EditMode テスト。
    /// タグ間親和度の取得・対称性・プロファイル間評価を検証する。
    /// </summary>
    public class AdjacencyRuleSetTests
    {
        private AdjacencyRuleSet m_RuleSet;

        [SetUp]
        public void SetUp()
        {
            m_RuleSet = ScriptableObject.CreateInstance<AdjacencyRuleSet>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_RuleSet);
        }

        #region GetAffinity Tests

        [Test]
        public void GetAffinity_UndefinedPair_ReturnsDefault()
        {
            Assert.AreEqual(0.5f, m_RuleSet.GetAffinity("sacred", "tower"), 0.001f);
        }

        [Test]
        public void GetAffinity_DefinedPair_ReturnsValue()
        {
            m_RuleSet.AddRule("sacred", "sacred", 0.8f);
            Assert.AreEqual(0.8f, m_RuleSet.GetAffinity("sacred", "sacred"), 0.001f);
        }

        [Test]
        public void GetAffinity_IsSymmetric()
        {
            m_RuleSet.AddRule("sacred", "fortified", 0.3f);

            float ab = m_RuleSet.GetAffinity("sacred", "fortified");
            float ba = m_RuleSet.GetAffinity("fortified", "sacred");
            Assert.AreEqual(ab, ba, 0.001f, "A-B と B-A は同じ親和度");
        }

        [Test]
        public void GetAffinity_CaseInsensitive()
        {
            m_RuleSet.AddRule("Sacred", "FORTIFIED", 0.3f);
            Assert.AreEqual(0.3f, m_RuleSet.GetAffinity("sacred", "fortified"), 0.001f);
        }

        [Test]
        public void GetAffinity_NullOrEmpty_ReturnsDefault()
        {
            Assert.AreEqual(0.5f, m_RuleSet.GetAffinity(null, "sacred"));
            Assert.AreEqual(0.5f, m_RuleSet.GetAffinity("sacred", ""));
            Assert.AreEqual(0.5f, m_RuleSet.GetAffinity(null, null));
        }

        [Test]
        public void RuleCount_TracksAddedRules()
        {
            Assert.AreEqual(0, m_RuleSet.RuleCount);
            m_RuleSet.AddRule("a", "b", 0.5f);
            Assert.AreEqual(1, m_RuleSet.RuleCount);
            m_RuleSet.AddRule("c", "d", 0.7f);
            Assert.AreEqual(2, m_RuleSet.RuleCount);
        }

        [Test]
        public void ClearRules_RemovesAll()
        {
            m_RuleSet.AddRule("a", "b", 0.5f);
            m_RuleSet.AddRule("c", "d", 0.7f);
            m_RuleSet.ClearRules();
            Assert.AreEqual(0, m_RuleSet.RuleCount);
            Assert.AreEqual(0.5f, m_RuleSet.GetAffinity("a", "b"), 0.001f);
        }

        #endregion

        #region EvaluateAdjacency Tests

        [Test]
        public void EvaluateAdjacency_NullProfiles_ReturnsDefault()
        {
            Assert.AreEqual(0.5f, m_RuleSet.EvaluateAdjacency(null, null));
        }

        [Test]
        public void EvaluateAdjacency_EmptyProfiles_ReturnsDefault()
        {
            var a = new StructureTagProfile();
            var b = new StructureTagProfile();
            Assert.AreEqual(0.5f, m_RuleSet.EvaluateAdjacency(a, b));
        }

        [Test]
        public void EvaluateAdjacency_SacredBuildings_HighAffinity()
        {
            m_RuleSet.AddRule("sacred", "sacred", 0.8f);

            var cathedral = new StructureTagProfile(
                new TagEntry("sacred", 0.9f),
                new TagEntry("ornate", 0.7f));
            var basilica = new StructureTagProfile(
                new TagEntry("sacred", 0.8f),
                new TagEntry("ornate", 0.6f));

            float affinity = m_RuleSet.EvaluateAdjacency(cathedral, basilica);
            // sacred-sacred = 0.8, sacred-ornate は未定義(0.5), ornate-sacred は未定義(0.5), ornate-ornate は未定義(0.5)
            // 重み付き平均なので sacred-sacred の影響が大きい
            Assert.Greater(affinity, 0.5f, "聖域同士は高い親和度");
        }

        [Test]
        public void EvaluateAdjacency_SacredVsFortress_LowAffinity()
        {
            m_RuleSet.AddRule("sacred", "fortified", 0.1f);
            m_RuleSet.AddRule("ornate", "fortified", 0.2f);

            var cathedral = new StructureTagProfile(
                new TagEntry("sacred", 0.9f),
                new TagEntry("ornate", 0.8f));
            var fortress = new StructureTagProfile(
                new TagEntry("fortified", 0.95f),
                new TagEntry("wall", 0.9f));

            float affinity = m_RuleSet.EvaluateAdjacency(cathedral, fortress);
            Assert.Less(affinity, 0.5f, "聖域と要塞は低い親和度");
        }

        [Test]
        public void EvaluateAdjacency_IsSymmetric()
        {
            m_RuleSet.AddRule("sacred", "fortified", 0.3f);

            var a = new StructureTagProfile(
                new TagEntry("sacred", 0.9f));
            var b = new StructureTagProfile(
                new TagEntry("fortified", 0.8f));

            float ab = m_RuleSet.EvaluateAdjacency(a, b);
            float ba = m_RuleSet.EvaluateAdjacency(b, a);
            Assert.AreEqual(ab, ba, 0.001f);
        }

        #endregion

        #region AdjacencyRule Struct Tests

        [Test]
        public void AdjacencyRule_ClampsAffinity()
        {
            var rule = new AdjacencyRule("a", "b", 1.5f);
            Assert.AreEqual(1.0f, rule.affinity, 0.001f);

            var rule2 = new AdjacencyRule("a", "b", -0.5f);
            Assert.AreEqual(0.0f, rule2.affinity, 0.001f);
        }

        #endregion
    }
}
