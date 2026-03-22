using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// タグ間の隣接親和度ルール。
    /// タグペアに対して 0.0 (排他) 〜 1.0 (共存) の親和度を定義する。
    /// </summary>
    [Serializable]
    public struct AdjacencyRule
    {
        /// <summary>タグA</summary>
        public string tagA;

        /// <summary>タグB</summary>
        public string tagB;

        /// <summary>隣接親和度 (0.0=排他 〜 1.0=共存)</summary>
        [Range(0f, 1f)]
        public float affinity;

        public AdjacencyRule(string _tagA, string _tagB, float _affinity)
        {
            tagA = _tagA;
            tagB = _tagB;
            affinity = Mathf.Clamp01(_affinity);
        }
    }

    /// <summary>
    /// タグ間の隣接親和度を定義するマトリクス (ScriptableObject)。
    /// 配置時に隣接セルの建物タグとの相性を評価するために使用する。
    /// </summary>
    [CreateAssetMenu(fileName = "NewAdjacencyRules", menuName = "Vastcore/Adjacency Rule Set")]
    public class AdjacencyRuleSet : ScriptableObject
    {
        #region Constants

        /// <summary>未定義ペアのデフォルト親和度</summary>
        private const float k_DefaultAffinity = 0.5f;

        #endregion

        #region Serialized Fields

        [SerializeField]
        [Tooltip("タグペアごとの隣接親和度ルール")]
        private List<AdjacencyRule> m_Rules = new List<AdjacencyRule>();

        #endregion

        #region Public Properties

        /// <summary>定義済みルール数</summary>
        public int RuleCount => m_Rules.Count;

        #endregion

        #region Public Methods

        /// <summary>
        /// 2つのタグ間の隣接親和度を取得する。
        /// ルールは対称 (tagA-tagB と tagB-tagA は同じ)。
        /// 未定義ペアはデフォルト値 (0.5) を返す。
        /// </summary>
        /// <param name="_tagA">タグA</param>
        /// <param name="_tagB">タグB</param>
        /// <returns>隣接親和度 (0.0〜1.0)</returns>
        public float GetAffinity(string _tagA, string _tagB)
        {
            if (string.IsNullOrEmpty(_tagA) || string.IsNullOrEmpty(_tagB))
            {
                return k_DefaultAffinity;
            }

            for (int i = 0; i < m_Rules.Count; i++)
            {
                AdjacencyRule rule = m_Rules[i];
                bool match = (string.Equals(rule.tagA, _tagA, StringComparison.OrdinalIgnoreCase)
                              && string.Equals(rule.tagB, _tagB, StringComparison.OrdinalIgnoreCase))
                             || (string.Equals(rule.tagA, _tagB, StringComparison.OrdinalIgnoreCase)
                                 && string.Equals(rule.tagB, _tagA, StringComparison.OrdinalIgnoreCase));

                if (match)
                {
                    return rule.affinity;
                }
            }

            return k_DefaultAffinity;
        }

        /// <summary>
        /// 2つの StructureTagProfile 間の総合隣接親和度を算出する。
        /// 両プロファイルの全タグペアについて、重み付き親和度の平均を返す。
        /// </summary>
        /// <param name="_profileA">建物Aのプロファイル</param>
        /// <param name="_profileB">建物Bのプロファイル</param>
        /// <returns>総合隣接親和度 (0.0〜1.0)。プロファイルが空の場合はデフォルト値</returns>
        public float EvaluateAdjacency(StructureTagProfile _profileA, StructureTagProfile _profileB)
        {
            if (_profileA == null || _profileB == null
                || _profileA.IsEmpty || _profileB.IsEmpty)
            {
                return k_DefaultAffinity;
            }

            var tagsA = _profileA.GetAllTags();
            var tagsB = _profileB.GetAllTags();

            float weightedSum = 0f;
            float weightSum = 0f;

            for (int i = 0; i < tagsA.Count; i++)
            {
                for (int j = 0; j < tagsB.Count; j++)
                {
                    float pairWeight = tagsA[i].weight * tagsB[j].weight;
                    float affinity = GetAffinity(tagsA[i].tagName, tagsB[j].tagName);
                    weightedSum += pairWeight * affinity;
                    weightSum += pairWeight;
                }
            }

            if (weightSum <= 0f)
            {
                return k_DefaultAffinity;
            }

            return Mathf.Clamp01(weightedSum / weightSum);
        }

        /// <summary>
        /// ルールを追加する (ランタイム / テスト用)
        /// </summary>
        public void AddRule(string _tagA, string _tagB, float _affinity)
        {
            m_Rules.Add(new AdjacencyRule(_tagA, _tagB, _affinity));
        }

        /// <summary>
        /// 全ルールをクリアする
        /// </summary>
        public void ClearRules()
        {
            m_Rules.Clear();
        }

        #endregion
    }
}
