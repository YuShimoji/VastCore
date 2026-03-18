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

        [Header("Variation (V1)")]
        [Tooltip("XZ平面での位置ずれ半径（ワールド単位）。0で無効")]
        [SerializeField] private float m_PositionJitter = 0f;

        [Tooltip("ランダム選択されるマテリアル候補。空の場合はPrefab既定")]
        [SerializeField] private Material[] m_MaterialVariants = new Material[0];

        [Tooltip("表示/非表示を切り替える子オブジェクト名。各配置で1つをランダム表示。空で無効")]
        [SerializeField] private string[] m_ChildToggleGroups = new string[0];
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

        /// <summary>
        /// XZ位置ジッター半径
        /// </summary>
        public float PositionJitter => m_PositionJitter;

        /// <summary>
        /// マテリアルバリアント配列
        /// </summary>
        public Material[] MaterialVariants => m_MaterialVariants;

        /// <summary>
        /// 子オブジェクト切替グループ名配列
        /// </summary>
        public string[] ChildToggleGroups => m_ChildToggleGroups;
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

        /// <summary>
        /// PositionJitter に基づくランダムなXZ位置オフセットを取得
        /// </summary>
        /// <param name="_random">乱数生成器</param>
        /// <returns>XZオフセット（Y=0）</returns>
        public Vector3 GetRandomPositionOffset(System.Random _random)
        {
            if (_random == null || m_PositionJitter <= 0f)
            {
                return Vector3.zero;
            }

            float angle = (float)(_random.NextDouble() * System.Math.PI * 2.0);
            float distance = (float)_random.NextDouble() * m_PositionJitter;
            return new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance);
        }

        /// <summary>
        /// MaterialVariants からランダムに1つ選択
        /// </summary>
        /// <param name="_random">乱数生成器</param>
        /// <returns>選択されたマテリアル。候補がない場合はnull</returns>
        public Material GetRandomMaterial(System.Random _random)
        {
            if (m_MaterialVariants == null || m_MaterialVariants.Length == 0)
            {
                return null;
            }

            if (_random == null)
            {
                return m_MaterialVariants[0];
            }

            int index = _random.Next(m_MaterialVariants.Length);
            return m_MaterialVariants[index];
        }

        /// <summary>
        /// ChildToggleGroups からランダムに1つのインデックスを選択
        /// </summary>
        /// <param name="_random">乱数生成器</param>
        /// <returns>選択されたインデックス。グループがない場合は-1</returns>
        public int GetRandomChildToggleIndex(System.Random _random)
        {
            if (m_ChildToggleGroups == null || m_ChildToggleGroups.Length == 0)
            {
                return -1;
            }

            if (_random == null)
            {
                return 0;
            }

            return _random.Next(m_ChildToggleGroups.Length);
        }

        public override string ToString()
        {
            return $"StampDef[{DisplayName}] Prefab:{(m_Prefab != null ? m_Prefab.name : "null")} Cells:{(IsSingleCell ? 1 : m_FootprintOffsets.Length)}";
        }
        #endregion
    }
}
