using System.Collections.Generic;

namespace Vastcore.Terrain.DualGrid
{
    /// <summary>
    /// スタンプ配置の管理クラス
    /// 配置の追加・削除・占有チェック・検索を担当する
    /// </summary>
    public class StampRegistry
    {
        #region Private Fields
        /// <summary>
        /// 全配置リスト
        /// </summary>
        private readonly List<StampPlacement> m_Placements;

        /// <summary>
        /// セルID → 配置ID のマップ（占有追跡）
        /// </summary>
        private readonly Dictionary<int, int> m_OccupancyMap;

        /// <summary>
        /// 次に割り当てる配置ID
        /// </summary>
        private int m_NextPlacementId;
        #endregion

        #region Public Properties
        /// <summary>
        /// 配置リスト（読み取り専用）
        /// </summary>
        public IReadOnlyList<StampPlacement> Placements => m_Placements;

        /// <summary>
        /// 配置数
        /// </summary>
        public int Count => m_Placements.Count;
        #endregion

        #region Constructors
        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public StampRegistry()
        {
            m_Placements = new List<StampPlacement>();
            m_OccupancyMap = new Dictionary<int, int>();
            m_NextPlacementId = 0;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// セルが占有されているかどうかを確認
        /// </summary>
        /// <param name="_cellId">セルID</param>
        /// <returns>占有されている場合true</returns>
        public bool IsOccupied(int _cellId)
        {
            return m_OccupancyMap.ContainsKey(_cellId);
        }

        /// <summary>
        /// スタンプを配置可能かどうかを検証
        /// </summary>
        /// <param name="_definition">スタンプ定義</param>
        /// <param name="_anchorCell">アンカーセル</param>
        /// <returns>配置可能な場合true</returns>
        public bool CanPlace(PrefabStampDefinition _definition, Cell _anchorCell)
        {
            if (_definition == null || !_definition.IsValid())
            {
                return false;
            }

            if (_anchorCell == null)
            {
                return false;
            }

            // アンカーセルが空いているか
            if (IsOccupied(_anchorCell.Id))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// スタンプを配置する
        /// </summary>
        /// <param name="_definition">スタンプ定義</param>
        /// <param name="_anchorCell">アンカーセル</param>
        /// <param name="_columnStack">高さデータ（HeightRuleの解決に使用）</param>
        /// <param name="_rotation">Y軸回転（度）</param>
        /// <param name="_scale">スケール</param>
        /// <returns>配置インスタンス。配置不可の場合null</returns>
        public StampPlacement Place(PrefabStampDefinition _definition, Cell _anchorCell,
            ColumnStack _columnStack, float _rotation, float _scale)
        {
            if (!CanPlace(_definition, _anchorCell))
            {
                return null;
            }

            // 高さルールに基づいてレイヤーを決定
            int layer = ResolveLayer(_definition, _anchorCell, _columnStack);

            // 配置を作成
            StampPlacement placement = new StampPlacement(
                m_NextPlacementId, _definition, _anchorCell, _rotation, layer, _scale);

            m_Placements.Add(placement);
            m_OccupancyMap[_anchorCell.Id] = m_NextPlacementId;
            m_NextPlacementId++;

            return placement;
        }

        /// <summary>
        /// 配置IDを指定して削除
        /// </summary>
        /// <param name="_placementId">配置ID</param>
        /// <returns>削除に成功した場合true</returns>
        public bool Remove(int _placementId)
        {
            int index = m_Placements.FindIndex(p => p.PlacementId == _placementId);
            if (index < 0)
            {
                return false;
            }

            StampPlacement placement = m_Placements[index];

            // 占有マップから削除
            m_OccupancyMap.Remove(placement.AnchorCellId);

            m_Placements.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// セルIDに配置されたスタンプを取得
        /// </summary>
        /// <param name="_cellId">セルID</param>
        /// <returns>配置インスタンス。なければnull</returns>
        public StampPlacement GetPlacementAt(int _cellId)
        {
            if (!m_OccupancyMap.TryGetValue(_cellId, out int placementId))
            {
                return null;
            }

            return m_Placements.Find(p => p.PlacementId == placementId);
        }

        /// <summary>
        /// 配置IDでスタンプを取得
        /// </summary>
        /// <param name="_placementId">配置ID</param>
        /// <returns>配置インスタンス。なければnull</returns>
        public StampPlacement GetPlacementById(int _placementId)
        {
            return m_Placements.Find(p => p.PlacementId == _placementId);
        }

        /// <summary>
        /// 全配置をクリア
        /// </summary>
        public void Clear()
        {
            m_Placements.Clear();
            m_OccupancyMap.Clear();
            m_NextPlacementId = 0;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// HeightRuleに基づいて配置レイヤーを決定
        /// </summary>
        private int ResolveLayer(PrefabStampDefinition _definition, Cell _anchorCell,
            ColumnStack _columnStack)
        {
            if (_columnStack == null)
            {
                return 0;
            }

            switch (_definition.HeightRule)
            {
                case StampHeightRule.TopOfStack:
                    return _columnStack.GetHeight(_anchorCell.Id);

                case StampHeightRule.GroundLevel:
                    return 0;

                case StampHeightRule.SpecificLayer:
                    // SpecificLayerの場合、呼び出し側がlayerを直接指定するケース
                    // デフォルトは0とし、将来の拡張で外部から指定可能にする
                    return 0;

                default:
                    return 0;
            }
        }
        #endregion
    }
}
