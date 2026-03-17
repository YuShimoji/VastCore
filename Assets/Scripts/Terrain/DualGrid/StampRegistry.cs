using System.Collections.Generic;

namespace Vastcore.Terrain.DualGrid
{
    /// <summary>
    /// スタンプ配置の管理クラス
    /// 配置の追加・削除・占有チェック・検索を担当する
    /// 単一セル・マルチセル（フットプリント）両方に対応
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

        /// <summary>
        /// フットプリント解決時の一時バッファ（GC削減）
        /// </summary>
        private readonly List<Cell> m_TempCellBuffer;
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

        /// <summary>
        /// 占有されているセル数
        /// </summary>
        public int OccupiedCellCount => m_OccupancyMap.Count;
        #endregion

        #region Constructors
        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public StampRegistry()
        {
            m_Placements = new List<StampPlacement>();
            m_OccupancyMap = new Dictionary<int, int>();
            m_TempCellBuffer = new List<Cell>();
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
        /// スタンプを配置可能かどうかを検証（単一セル版、後方互換）
        /// </summary>
        /// <param name="_definition">スタンプ定義</param>
        /// <param name="_anchorCell">アンカーセル</param>
        /// <returns>配置可能な場合true</returns>
        public bool CanPlace(PrefabStampDefinition _definition, Cell _anchorCell)
        {
            return CanPlace(_definition, _anchorCell, null);
        }

        /// <summary>
        /// スタンプを配置可能かどうかを検証（マルチセル対応）
        /// 単一セルスタンプはアンカーセルのみ検証する。
        /// マルチセルスタンプはアンカーヘックスの全サブセル＋フットプリントヘックスの全サブセルを検証する。
        /// </summary>
        /// <param name="_definition">スタンプ定義</param>
        /// <param name="_anchorCell">アンカーセル</param>
        /// <param name="_grid">グリッド（マルチセルの場合必須）</param>
        /// <returns>配置可能な場合true</returns>
        public bool CanPlace(PrefabStampDefinition _definition, Cell _anchorCell,
            IrregularGrid _grid)
        {
            if (_definition == null || !_definition.IsValid())
            {
                return false;
            }

            if (_anchorCell == null)
            {
                return false;
            }

            if (_definition.IsSingleCell)
            {
                // 単一セル: アンカーセルのみ
                return !IsOccupied(_anchorCell.Id);
            }

            // マルチセル: グリッドが必須
            if (_grid == null)
            {
                return false;
            }

            // 全占有セルIDを解決して空きチェック
            List<int> cellIds = ResolveFootprintCellIds(_definition, _anchorCell, _grid);
            if (cellIds == null)
            {
                return false;
            }

            foreach (int cellId in cellIds)
            {
                if (IsOccupied(cellId))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// スタンプを配置する（単一セル版、後方互換）
        /// </summary>
        public StampPlacement Place(PrefabStampDefinition _definition, Cell _anchorCell,
            ColumnStack _columnStack, float _rotation, float _scale)
        {
            return Place(_definition, _anchorCell, _columnStack, _rotation, _scale, null);
        }

        /// <summary>
        /// スタンプを配置する（マルチセル対応）
        /// </summary>
        /// <param name="_definition">スタンプ定義</param>
        /// <param name="_anchorCell">アンカーセル</param>
        /// <param name="_columnStack">高さデータ（HeightRuleの解決に使用）</param>
        /// <param name="_rotation">Y軸回転（度）</param>
        /// <param name="_scale">スケール</param>
        /// <param name="_grid">グリッド（マルチセルの場合必須）</param>
        /// <returns>配置インスタンス。配置不可の場合null</returns>
        public StampPlacement Place(PrefabStampDefinition _definition, Cell _anchorCell,
            ColumnStack _columnStack, float _rotation, float _scale,
            IrregularGrid _grid)
        {
            if (!CanPlace(_definition, _anchorCell, _grid))
            {
                return null;
            }

            // 高さルールに基づいてレイヤーを決定
            int layer = ResolveLayer(_definition, _anchorCell, _columnStack);

            // 占有セルIDを解決
            int[] occupiedCellIds;
            if (_definition.IsSingleCell)
            {
                occupiedCellIds = new int[] { _anchorCell.Id };
            }
            else
            {
                List<int> cellIds = ResolveFootprintCellIds(_definition, _anchorCell, _grid);
                occupiedCellIds = cellIds.ToArray();
            }

            // 配置を作成
            StampPlacement placement = new StampPlacement(
                m_NextPlacementId, _definition, _anchorCell, _rotation, layer, _scale,
                occupiedCellIds);

            m_Placements.Add(placement);

            // 全占有セルを登録
            foreach (int cellId in occupiedCellIds)
            {
                m_OccupancyMap[cellId] = m_NextPlacementId;
            }

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

            // 全占有セルを占有マップから削除
            if (placement.OccupiedCellIds != null)
            {
                foreach (int cellId in placement.OccupiedCellIds)
                {
                    m_OccupancyMap.Remove(cellId);
                }
            }

            m_Placements.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// セルIDに配置されたスタンプを取得
        /// アンカーセルだけでなく、フットプリント上の任意のセルから検索可能
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
                    return 0;

                default:
                    return 0;
            }
        }

        /// <summary>
        /// フットプリント定義からグリッド上の全占有セルIDリストを解決する
        /// アンカーヘックスの全サブセル＋各フットプリントオフセットヘックスの全サブセルを返す。
        /// いずれかのヘックスがグリッド上に存在しない場合はnullを返す。
        /// </summary>
        /// <param name="_definition">スタンプ定義</param>
        /// <param name="_anchorCell">アンカーセル</param>
        /// <param name="_grid">グリッド</param>
        /// <returns>占有セルIDリスト。解決失敗時null</returns>
        private List<int> ResolveFootprintCellIds(PrefabStampDefinition _definition,
            Cell _anchorCell, IrregularGrid _grid)
        {
            List<int> cellIds = new List<int>();

            // アンカーヘックスの全サブセルを追加
            m_TempCellBuffer.Clear();
            int anchorCount = _grid.FindCellsByHex(_anchorCell.HexQ, _anchorCell.HexR, m_TempCellBuffer);
            if (anchorCount == 0)
            {
                return null;
            }

            foreach (Cell cell in m_TempCellBuffer)
            {
                cellIds.Add(cell.Id);
            }

            // 各フットプリントオフセットのヘックスの全サブセルを追加
            UnityEngine.Vector2Int[] offsets = _definition.FootprintOffsets;
            if (offsets != null)
            {
                foreach (UnityEngine.Vector2Int offset in offsets)
                {
                    int targetQ = _anchorCell.HexQ + offset.x;
                    int targetR = _anchorCell.HexR + offset.y;

                    m_TempCellBuffer.Clear();
                    int count = _grid.FindCellsByHex(targetQ, targetR, m_TempCellBuffer);
                    if (count == 0)
                    {
                        // フットプリントヘックスがグリッド上に存在しない
                        return null;
                    }

                    foreach (Cell cell in m_TempCellBuffer)
                    {
                        cellIds.Add(cell.Id);
                    }
                }
            }

            return cellIds;
        }
        #endregion
    }
}
