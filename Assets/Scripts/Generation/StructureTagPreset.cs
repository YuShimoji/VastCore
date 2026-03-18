using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// 建物タグの事前定義プリセット (ScriptableObject)。
    /// ユーザーはこれを複製してカスタムプリセットを作成可能。
    /// </summary>
    [CreateAssetMenu(fileName = "NewStructurePreset", menuName = "Vastcore/Structure Tag Preset")]
    public class StructureTagPreset : ScriptableObject
    {
        #region Serialized Fields
        [Tooltip("プリセットの表示名")]
        [SerializeField] private string m_DisplayName = "";

        [Tooltip("プリセットの説明")]
        [TextArea(2, 4)]
        [SerializeField] private string m_Description = "";

        [Tooltip("タグプロファイル")]
        [SerializeField] private StructureTagProfile m_Profile = new StructureTagProfile();
        #endregion

        #region Public Properties
        /// <summary>
        /// 表示名 (未設定の場合はアセット名)
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(m_DisplayName) ? name : m_DisplayName;

        /// <summary>
        /// プリセットの説明
        /// </summary>
        public string Description => m_Description;

        /// <summary>
        /// タグプロファイル
        /// </summary>
        public StructureTagProfile Profile => m_Profile;
        #endregion

        #region Public Methods
        /// <summary>
        /// 他プリセットとのブレンドスコアを算出
        /// </summary>
        /// <param name="_other">比較対象プリセット</param>
        /// <returns>ブレンドスコア (0.0〜1.0)</returns>
        public float BlendScore(StructureTagPreset _other)
        {
            if (_other == null || _other.m_Profile == null)
            {
                return 0f;
            }
            return m_Profile.BlendScore(_other.m_Profile);
        }

        /// <summary>
        /// タグプロファイルとのブレンドスコアを算出
        /// </summary>
        /// <param name="_profile">比較対象プロファイル</param>
        /// <returns>ブレンドスコア (0.0〜1.0)</returns>
        public float BlendScore(StructureTagProfile _profile)
        {
            if (_profile == null)
            {
                return 0f;
            }
            return m_Profile.BlendScore(_profile);
        }

        public override string ToString()
        {
            return $"Preset[{DisplayName}] {m_Profile}";
        }
        #endregion
    }
}
