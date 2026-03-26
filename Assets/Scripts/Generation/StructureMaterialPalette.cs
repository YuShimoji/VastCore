using UnityEngine;

namespace Vastcore.Generation
{
    /// <summary>
    /// マテリアルパレット。建物タグ親和度に基づいてマテリアルセットを提供する。
    /// ComponentCategory ごとにマテリアルを保持し、BlendScore で自動選択される。
    /// </summary>
    [CreateAssetMenu(fileName = "NewMaterialPalette", menuName = "Vastcore/Structure Material Palette")]
    public class StructureMaterialPalette : ScriptableObject
    {
        #region Serialized Fields
        [Tooltip("パレットの表示名")]
        [SerializeField] private string m_DisplayName = "";

        [Tooltip("タグ親和度プロファイル")]
        [SerializeField] private StructureTagProfile m_Affinity = new StructureTagProfile();

        [Header("マテリアル")]
        [Tooltip("壁面マテリアル")]
        [SerializeField] private Material m_WallMaterial;

        [Tooltip("屋根マテリアル")]
        [SerializeField] private Material m_RoofMaterial;

        [Tooltip("装飾マテリアル")]
        [SerializeField] private Material m_OrnamentMaterial;

        [Tooltip("基礎マテリアル")]
        [SerializeField] private Material m_FoundationMaterial;

        [Header("風化")]
        [Tooltip("風化度 (0=新品, 1=完全風化)")]
        [Range(0f, 1f)]
        [SerializeField] private float m_WeatheringLevel;
        #endregion

        #region Public Properties
        /// <summary>
        /// 表示名 (未設定の場合はアセット名)
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(m_DisplayName) ? name : m_DisplayName;

        /// <summary>
        /// タグ親和度プロファイル
        /// </summary>
        public StructureTagProfile Affinity => m_Affinity;

        /// <summary>壁面マテリアル</summary>
        public Material WallMaterial => m_WallMaterial;

        /// <summary>屋根マテリアル</summary>
        public Material RoofMaterial => m_RoofMaterial;

        /// <summary>装飾マテリアル</summary>
        public Material OrnamentMaterial => m_OrnamentMaterial;

        /// <summary>基礎マテリアル</summary>
        public Material FoundationMaterial => m_FoundationMaterial;

        /// <summary>風化度 (0.0〜1.0)</summary>
        public float WeatheringLevel => m_WeatheringLevel;
        #endregion

        #region Public Methods
        /// <summary>
        /// 建物タグプロファイルとのブレンドスコアを算出
        /// </summary>
        /// <param name="_buildingProfile">建物のタグプロファイル</param>
        /// <returns>ブレンドスコア (0.0〜1.0)</returns>
        public float BlendScore(StructureTagProfile _buildingProfile)
        {
            if (_buildingProfile == null || m_Affinity == null)
            {
                return 0f;
            }
            return m_Affinity.BlendScore(_buildingProfile);
        }

        /// <summary>
        /// ComponentCategory に対応するマテリアルを取得
        /// </summary>
        /// <param name="_category">構成要素カテゴリ</param>
        /// <returns>対応するマテリアル (未設定なら null)</returns>
        public Material GetMaterial(ComponentCategory _category)
        {
            switch (_category)
            {
                case ComponentCategory.Shell:
                    return m_WallMaterial;
                case ComponentCategory.Aperture:
                    return m_WallMaterial; // 開口部は壁面マテリアルを共有
                case ComponentCategory.Ornament:
                    return m_OrnamentMaterial;
                default:
                    return m_WallMaterial;
            }
        }

        /// <summary>
        /// ComponentType に対応するマテリアルを取得 (より精密な選択)
        /// </summary>
        /// <param name="_componentType">構成要素の種類</param>
        /// <returns>対応するマテリアル (未設定なら null)</returns>
        public Material GetMaterial(ComponentType _componentType)
        {
            switch (_componentType)
            {
                case ComponentType.Roof:
                    return m_RoofMaterial;
                case ComponentType.Foundation:
                case ComponentType.Floor:
                    return m_FoundationMaterial;
                case ComponentType.Column:
                case ComponentType.ArchOrnament:
                case ComponentType.Carving:
                case ComponentType.Battlement:
                case ComponentType.Buttress:
                case ComponentType.Pinnacle:
                    return m_OrnamentMaterial;
                default:
                    return m_WallMaterial;
            }
        }

        public override string ToString()
        {
            return $"MaterialPalette[{DisplayName}] weathering={m_WeatheringLevel:F2}";
        }
        #endregion
    }
}
