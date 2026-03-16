using UnityEngine;

namespace Vastcore.Terrain.DualGrid
{
    /// <summary>
    /// スタンプの回転モード
    /// </summary>
    public enum StampRotationMode
    {
        /// <summary>回転なし（固定方向）</summary>
        Fixed,
        /// <summary>90度刻みで回転可能</summary>
        Step90,
        /// <summary>自由回転</summary>
        Free
    }

    /// <summary>
    /// スタンプの高さ配置ルール
    /// </summary>
    public enum StampHeightRule
    {
        /// <summary>スタックの最上層に配置</summary>
        TopOfStack,
        /// <summary>地面レベル（レイヤー0）に配置</summary>
        GroundLevel,
        /// <summary>指定レイヤーに配置</summary>
        SpecificLayer
    }

    /// <summary>
    /// Prefabスタンプ定義（ScriptableObject）
    /// DualGridセルに配置するPrefabの参照と配置ルールを保持する
    /// </summary>
    [CreateAssetMenu(menuName = "Vastcore/Terrain/Prefab Stamp Definition")]
    public class PrefabStampDefinition : ScriptableObject
    {
        #region Serialized Fields
        [Header("Prefab")]
        [Tooltip("配置するPrefab")]
        [SerializeField] private GameObject m_Prefab;

        [Tooltip("表示名")]
        [SerializeField] private string m_DisplayName = "";

        [Header("Placement Rules")]
        [Tooltip("回転モード")]
        [SerializeField] private StampRotationMode m_RotationMode = StampRotationMode.Step90;

        [Tooltip("高さ配置ルール")]
        [SerializeField] private StampHeightRule m_HeightRule = StampHeightRule.TopOfStack;

        [Tooltip("スケールのバリエーション範囲 (min, max)")]
        [SerializeField] private Vector2 m_ScaleRange = new Vector2(0.8f, 1.2f);

        [Header("Footprint")]
        [Tooltip("占有するHexオフセット（アンカーセルからの相対座標）。空の場合は単一セル")]
        [SerializeField] private Vector2Int[] m_FootprintOffsets = new Vector2Int[0];
        #endregion

        #region Public Properties
        /// <summary>
        /// 配置するPrefab
        /// </summary>
        public GameObject Prefab => m_Prefab;

        /// <summary>
        /// 表示名（未設定の場合はアセット名）
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(m_DisplayName) ? name : m_DisplayName;

        /// <summary>
        /// 回転モード
        /// </summary>
        public StampRotationMode RotationMode => m_RotationMode;

        /// <summary>
        /// 高さ配置ルール
        /// </summary>
        public StampHeightRule HeightRule => m_HeightRule;

        /// <summary>
        /// スケール範囲 (x=min, y=max)
        /// </summary>
        public Vector2 ScaleRange => m_ScaleRange;

        /// <summary>
        /// フットプリントオフセット配列（読み取り専用）
        /// 空の場合は単一セル（アンカーのみ）
        /// </summary>
        public Vector2Int[] FootprintOffsets => m_FootprintOffsets;

        /// <summary>
        /// 単一セルスタンプかどうか
        /// </summary>
        public bool IsSingleCell => m_FootprintOffsets == null || m_FootprintOffsets.Length == 0;
        #endregion

        #region Public Methods
        /// <summary>
        /// 定義が有効（Prefab参照あり）かどうか
        /// </summary>
        public bool IsValid()
        {
            return m_Prefab != null;
        }

        /// <summary>
        /// スケール範囲からランダムなスケールを取得
        /// </summary>
        /// <param name="_random">乱数生成器</param>
        /// <returns>スケール値</returns>
        public float GetRandomScale(System.Random _random)
        {
            if (_random == null)
            {
                return (m_ScaleRange.x + m_ScaleRange.y) * 0.5f;
            }

            float t = (float)_random.NextDouble();
            return Mathf.Lerp(m_ScaleRange.x, m_ScaleRange.y, t);
        }

        /// <summary>
        /// 回転モードに基づいたランダムな回転角度を取得
        /// </summary>
        /// <param name="_random">乱数生成器</param>
        /// <returns>Y軸回転角度（度）</returns>
        public float GetRandomRotation(System.Random _random)
        {
            if (_random == null)
            {
                return 0f;
            }

            switch (m_RotationMode)
            {
                case StampRotationMode.Fixed:
                    return 0f;
                case StampRotationMode.Step90:
                    return _random.Next(4) * 90f;
                case StampRotationMode.Free:
                    return (float)_random.NextDouble() * 360f;
                default:
                    return 0f;
            }
        }

        public override string ToString()
        {
            return $"StampDef[{DisplayName}] Prefab:{(m_Prefab != null ? m_Prefab.name : "null")} Cells:{(IsSingleCell ? 1 : m_FootprintOffsets.Length)}";
        }
        #endregion
    }
}
