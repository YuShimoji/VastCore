using UnityEngine;

namespace Vastcore.Terrain.DualGrid
{
    /// <summary>
    /// 配置済みスタンプのインスタンスデータ
    /// セルへの配置状態を保持する
    /// </summary>
    [System.Serializable]
    public class StampPlacement
    {
        #region Fields
        /// <summary>
        /// 配置の一意なID
        /// </summary>
        [SerializeField] private int m_PlacementId;

        /// <summary>
        /// スタンプ定義への参照
        /// </summary>
        [SerializeField] private PrefabStampDefinition m_Definition;

        /// <summary>
        /// アンカーセルのID
        /// </summary>
        [SerializeField] private int m_AnchorCellId;

        /// <summary>
        /// アンカーセルのHex座標 Q
        /// </summary>
        [SerializeField] private int m_AnchorHexQ;

        /// <summary>
        /// アンカーセルのHex座標 R
        /// </summary>
        [SerializeField] private int m_AnchorHexR;

        /// <summary>
        /// アンカーセルのサブインデックス
        /// </summary>
        [SerializeField] private int m_AnchorSubIndex;

        /// <summary>
        /// Y軸回転（度）
        /// </summary>
        [SerializeField] private float m_Rotation;

        /// <summary>
        /// 配置レイヤー（高さ）
        /// </summary>
        [SerializeField] private int m_Layer;

        /// <summary>
        /// スケール
        /// </summary>
        [SerializeField] private float m_Scale;

        /// <summary>
        /// 占有しているセルIDの配列（アンカーセル含む）
        /// 単一セルスタンプの場合はアンカーセルIDのみ
        /// </summary>
        [SerializeField] private int[] m_OccupiedCellIds;
        #endregion

        #region Public Properties
        public int PlacementId => m_PlacementId;
        public PrefabStampDefinition Definition => m_Definition;
        public int AnchorCellId => m_AnchorCellId;
        public int AnchorHexQ => m_AnchorHexQ;
        public int AnchorHexR => m_AnchorHexR;
        public int AnchorSubIndex => m_AnchorSubIndex;
        public float Rotation => m_Rotation;
        public int Layer => m_Layer;
        public float Scale => m_Scale;

        /// <summary>
        /// 占有しているセルIDの配列（読み取り専用）
        /// </summary>
        public int[] OccupiedCellIds => m_OccupiedCellIds;
        #endregion

        #region Constructors
        /// <summary>
        /// StampPlacementを作成
        /// </summary>
        /// <param name="_placementId">配置ID</param>
        /// <param name="_definition">スタンプ定義</param>
        /// <param name="_anchorCell">アンカーセル</param>
        /// <param name="_rotation">Y軸回転（度）</param>
        /// <param name="_layer">配置レイヤー</param>
        /// <param name="_scale">スケール</param>
        /// <param name="_occupiedCellIds">占有セルID配列（nullの場合はアンカーセルのみ）</param>
        public StampPlacement(int _placementId, PrefabStampDefinition _definition,
            Cell _anchorCell, float _rotation, int _layer, float _scale,
            int[] _occupiedCellIds = null)
        {
            m_PlacementId = _placementId;
            m_Definition = _definition;
            m_AnchorCellId = _anchorCell.Id;
            m_AnchorHexQ = _anchorCell.HexQ;
            m_AnchorHexR = _anchorCell.HexR;
            m_AnchorSubIndex = _anchorCell.SubIndex;
            m_Rotation = _rotation;
            m_Layer = _layer;
            m_Scale = _scale;
            m_OccupiedCellIds = _occupiedCellIds ?? new int[] { _anchorCell.Id };
        }
        #endregion

        #region Public Methods
        public override string ToString()
        {
            string defName = m_Definition != null ? m_Definition.DisplayName : "null";
            return $"Placement[{m_PlacementId}] Def:{defName} Cell:{m_AnchorCellId} Hex({m_AnchorHexQ},{m_AnchorHexR}) Layer:{m_Layer}";
        }
        #endregion
    }
}
