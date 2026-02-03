using UnityEngine;

namespace Vastcore.Terrain.MarchingSquares
{
    /// <summary>
    /// Marching Squaresアルゴリズムのビットマスク計算ロジック
    /// セルの4頂点の状態からインデックス（0～15）を算出
    /// </summary>
    public static class MarchingSquaresCalculator
    {
        #region Public Methods
        /// <summary>
        /// セルの4頂点からMarching Squaresインデックス（0～15）を算出
        /// </summary>
        /// <param name="_grid">グリッドデータ</param>
        /// <param name="_x">セルのX座標（セルインデックス、頂点インデックスではない）</param>
        /// <param name="_y">セルのY座標（セルインデックス、頂点インデックスではない）</param>
        /// <returns>Marching Squaresインデックス（0～15）</returns>
        public static int CalculateIndex(MarchingSquaresGrid _grid, int _x, int _y)
        {
            if (_grid == null)
            {
                Debug.LogError("MarchingSquaresCalculator.CalculateIndex: Grid is null.");
                return 0;
            }

            // セル (x, y) の4つの頂点データを取得
            // セルは4つの頂点で構成される:
            // - Top-Left (TL): (x, y+1)
            // - Top-Right (TR): (x+1, y+1)
            // - Bottom-Right (BR): (x+1, y)
            // - Bottom-Left (BL): (x, y)
            bool tl = _grid.GetVertex(_x, _y + 1);      // Top-Left
            bool tr = _grid.GetVertex(_x + 1, _y + 1);  // Top-Right
            bool br = _grid.GetVertex(_x + 1, _y);      // Bottom-Right
            bool bl = _grid.GetVertex(_x, _y);          // Bottom-Left

            // ビット演算でインデックス化
            // 仕様書通り: Index = (TL << 3) | (TR << 2) | (BR << 1) | BL
            int index = (tl ? 1 : 0) << 3 | (tr ? 1 : 0) << 2 | (br ? 1 : 0) << 1 | (bl ? 1 : 0);

            return index;
        }

        /// <summary>
        /// セルの4頂点の状態を取得
        /// </summary>
        /// <param name="_grid">グリッドデータ</param>
        /// <param name="_x">セルのX座標（セルインデックス）</param>
        /// <param name="_y">セルのY座標（セルインデックス）</param>
        /// <returns>4頂点の状態（TL, TR, BR, BLの順）</returns>
        public static (bool tl, bool tr, bool br, bool bl) GetCellCorners(MarchingSquaresGrid _grid, int _x, int _y)
        {
            if (_grid == null)
            {
                Debug.LogError("MarchingSquaresCalculator.GetCellCorners: Grid is null.");
                return (false, false, false, false);
            }

            bool tl = _grid.GetVertex(_x, _y + 1);      // Top-Left
            bool tr = _grid.GetVertex(_x + 1, _y + 1);  // Top-Right
            bool br = _grid.GetVertex(_x + 1, _y);      // Bottom-Right
            bool bl = _grid.GetVertex(_x, _y);          // Bottom-Left

            return (tl, tr, br, bl);
        }

        /// <summary>
        /// セルが有効な範囲内かチェック
        /// セル (x, y) は頂点 (x, y), (x+1, y), (x, y+1), (x+1, y+1) を参照するため、
        /// 範囲チェックは (x+1, y+1) が有効かどうかを確認する
        /// </summary>
        /// <param name="_grid">グリッドデータ</param>
        /// <param name="_x">セルのX座標</param>
        /// <param name="_y">セルのY座標</param>
        /// <returns>有効な範囲内の場合はTrue</returns>
        public static bool IsValidCell(MarchingSquaresGrid _grid, int _x, int _y)
        {
            if (_grid == null)
            {
                return false;
            }

            // セル (x, y) は頂点 (x+1, y+1) まで参照するため、
            // 有効なセルは x < width-1 かつ y < height-1
            return _x >= 0 && _x < _grid.Width - 1 && _y >= 0 && _y < _grid.Height - 1;
        }
        #endregion
    }
}
