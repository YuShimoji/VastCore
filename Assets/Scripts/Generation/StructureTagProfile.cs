using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// タグエントリ。タグ名と重み (0.0〜1.0) のペア。
    /// </summary>
    [Serializable]
    public struct TagEntry
    {
        /// <summary>タグ名 (小文字英数字+アンダースコア)</summary>
        public string tagName;

        /// <summary>重み (0.0=無関係 〜 1.0=完全適合)</summary>
        [Range(0f, 1f)]
        public float weight;

        public TagEntry(string _tagName, float _weight)
        {
            tagName = _tagName;
            weight = Mathf.Clamp01(_weight);
        }
    }

    /// <summary>
    /// 建物の性質を記述するタグ重み複合体。
    /// 全タグはフラット構造で、重みは 0.0 (無関係) 〜 1.0 (完全適合) の範囲。
    /// ブレンドスコア (コサイン類似度) で他プロファイルとのマッチングを行う。
    /// </summary>
    [Serializable]
    public class StructureTagProfile
    {
        #region Serialized Fields
        [SerializeField]
        private List<TagEntry> m_Tags = new List<TagEntry>();
        #endregion

        #region Constructors
        /// <summary>
        /// 空のプロファイルを作成
        /// </summary>
        public StructureTagProfile()
        {
        }

        /// <summary>
        /// タグエントリ配列からプロファイルを作成
        /// </summary>
        public StructureTagProfile(params TagEntry[] _entries)
        {
            if (_entries != null)
            {
                m_Tags = new List<TagEntry>(_entries);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// タグの重みを取得。未定義タグは 0.0 を返す。
        /// </summary>
        /// <param name="_tagName">タグ名</param>
        /// <returns>重み (0.0〜1.0)</returns>
        public float GetWeight(string _tagName)
        {
            if (string.IsNullOrEmpty(_tagName))
            {
                return 0f;
            }

            for (int i = 0; i < m_Tags.Count; i++)
            {
                if (string.Equals(m_Tags[i].tagName, _tagName, StringComparison.OrdinalIgnoreCase))
                {
                    return m_Tags[i].weight;
                }
            }

            return 0f;
        }

        /// <summary>
        /// タグの重みを設定。既存タグは上書き、新規タグは追加。
        /// 重みが 0.0 の場合はタグを削除する。
        /// </summary>
        /// <param name="_tagName">タグ名</param>
        /// <param name="_weight">重み (0.0〜1.0)</param>
        public void SetWeight(string _tagName, float _weight)
        {
            if (string.IsNullOrEmpty(_tagName))
            {
                return;
            }

            _weight = Mathf.Clamp01(_weight);

            for (int i = 0; i < m_Tags.Count; i++)
            {
                if (string.Equals(m_Tags[i].tagName, _tagName, StringComparison.OrdinalIgnoreCase))
                {
                    if (_weight <= 0f)
                    {
                        m_Tags.RemoveAt(i);
                    }
                    else
                    {
                        m_Tags[i] = new TagEntry(_tagName, _weight);
                    }
                    return;
                }
            }

            if (_weight > 0f)
            {
                m_Tags.Add(new TagEntry(_tagName, _weight));
            }
        }

        /// <summary>
        /// 定義済みタグの数を取得
        /// </summary>
        public int TagCount => m_Tags.Count;

        /// <summary>
        /// 定義済みタグの一覧を取得 (読み取り専用)
        /// </summary>
        public IReadOnlyList<TagEntry> GetAllTags()
        {
            return m_Tags;
        }

        /// <summary>
        /// 他プロファイルとのブレンドスコアを算出 (コサイン類似度)。
        /// 両方に存在するタグの重みベクトルの内積を正規化して返す。
        /// </summary>
        /// <param name="_other">比較対象プロファイル</param>
        /// <returns>ブレンドスコア (0.0〜1.0)</returns>
        public float BlendScore(StructureTagProfile _other)
        {
            if (_other == null || m_Tags.Count == 0 || _other.m_Tags.Count == 0)
            {
                return 0f;
            }

            // 全タグ名を収集
            var allTagNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < m_Tags.Count; i++)
            {
                if (!string.IsNullOrEmpty(m_Tags[i].tagName))
                {
                    allTagNames.Add(m_Tags[i].tagName);
                }
            }
            for (int i = 0; i < _other.m_Tags.Count; i++)
            {
                if (!string.IsNullOrEmpty(_other.m_Tags[i].tagName))
                {
                    allTagNames.Add(_other.m_Tags[i].tagName);
                }
            }

            if (allTagNames.Count == 0)
            {
                return 0f;
            }

            float dotProduct = 0f;
            float magnitudeA = 0f;
            float magnitudeB = 0f;

            foreach (string tagName in allTagNames)
            {
                float a = GetWeight(tagName);
                float b = _other.GetWeight(tagName);
                dotProduct += a * b;
                magnitudeA += a * a;
                magnitudeB += b * b;
            }

            float denominator = Mathf.Sqrt(magnitudeA) * Mathf.Sqrt(magnitudeB);
            if (denominator <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(dotProduct / denominator);
        }

        /// <summary>
        /// プロファイルが空（タグなし）かどうか
        /// </summary>
        public bool IsEmpty => m_Tags.Count == 0;

        /// <summary>
        /// 全タグをクリア
        /// </summary>
        public void Clear()
        {
            m_Tags.Clear();
        }

        public override string ToString()
        {
            if (m_Tags.Count == 0)
            {
                return "TagProfile[empty]";
            }

            var parts = new string[m_Tags.Count];
            for (int i = 0; i < m_Tags.Count; i++)
            {
                parts[i] = $"{m_Tags[i].tagName}:{m_Tags[i].weight:F2}";
            }
            return $"TagProfile[{string.Join(", ", parts)}]";
        }
        #endregion
    }
}
