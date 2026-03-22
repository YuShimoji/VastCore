using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// エリアごとの建物配置密度と傾向を定義する (ScriptableObject)。
    /// DualGridセル群に適用してゾーン的な配置制御を行う。
    /// </summary>
    [CreateAssetMenu(fileName = "NewPlacementZone", menuName = "Vastcore/Placement Zone")]
    public class PlacementZone : ScriptableObject
    {
        #region Serialized Fields

        [Tooltip("ゾーンの表示名")]
        [SerializeField]
        private string m_DisplayName = "";

        [Tooltip("建物密度 (0.0=なし 〜 1.0=最密)")]
        [Range(0f, 1f)]
        [SerializeField]
        private float m_Density = 0.3f;

        [Tooltip("このゾーンで生まれやすい建物のタグ傾向")]
        [SerializeField]
        private StructureTagProfile m_ZoneBias = new StructureTagProfile();

        [Tooltip("最小建物間距離 (DualGridセル単位)")]
        [Min(0)]
        [SerializeField]
        private int m_MinSpacing = 1;

        [Tooltip("最大建物数 (0=無制限)")]
        [Min(0)]
        [SerializeField]
        private int m_MaxCount = 0;

        #endregion

        #region Public Properties

        /// <summary>ゾーンの表示名</summary>
        public string DisplayName => string.IsNullOrEmpty(m_DisplayName) ? name : m_DisplayName;

        /// <summary>建物密度 (0.0〜1.0)</summary>
        public float Density => m_Density;

        /// <summary>このゾーンで生まれやすい建物のタグ傾向</summary>
        public StructureTagProfile ZoneBias => m_ZoneBias;

        /// <summary>最小建物間距離 (DualGridセル単位)</summary>
        public int MinSpacing => m_MinSpacing;

        /// <summary>最大建物数 (0=無制限)</summary>
        public int MaxCount => m_MaxCount;

        #endregion
    }
}
