using UnityEngine;

namespace Vastcore.Terrain.MarchingSquares
{
    /// <summary>
    /// バイオーム遷移計算ロジック
    /// 隣接セルのバイオームIDを比較し、境界を検出して適切な境界線モデルを選択
    /// </summary>
    public static class BiomeTransitionCalculator
    {
        #region Constants
        /// <summary>
        /// 遷移タイプ: 同一バイオーム（遷移なし）
        /// </summary>
        public const int TransitionType_None = 0;

        /// <summary>
        /// 遷移タイプ: 海→陸（海岸線）
        /// </summary>
        public const int TransitionType_SeaToLand = 1;

        /// <summary>
        /// 遷移タイプ: 陸→海（海岸線）
        /// </summary>
        public const int TransitionType_LandToSea = 2;

        /// <summary>
        /// 遷移タイプ: 陸→山（山麓）
        /// </summary>
        public const int TransitionType_LandToMountain = 3;

        /// <summary>
        /// 遷移タイプ: 山→陸（山麓）
        /// </summary>
        public const int TransitionType_MountainToLand = 4;

        /// <summary>
        /// 遷移タイプ: 砂→草（砂浜から草原）
        /// </summary>
        public const int TransitionType_SandToGrass = 5;

        /// <summary>
        /// 遷移タイプ: 草→砂（草原から砂浜）
        /// </summary>
        public const int TransitionType_GrassToSand = 6;

        /// <summary>
        /// 遷移タイプ: その他（汎用境界）
        /// </summary>
        public const int TransitionType_Other = 99;
        #endregion

        #region Public Methods
        /// <summary>
        /// セルのバイオーム遷移を計算
        /// 隣接セル（上下左右）のバイオームIDを比較し、遷移タイプを返す
        /// </summary>
        /// <param name="_x">セルのX座標（セルインデックス）</param>
        /// <param name="_y">セルのY座標（セルインデックス）</param>
        /// <param name="_grid">グリッドデータ</param>
        /// <returns>遷移タイプ（TransitionType_*定数）。遷移がない場合はTransitionType_None</returns>
        public static int CalculateTransition(int _x, int _y, MarchingSquaresGrid _grid)
        {
            if (_grid == null)
            {
                Debug.LogError("BiomeTransitionCalculator.CalculateTransition: Grid is null.");
                return TransitionType_None;
            }

            if (!_grid.UseExtendedData)
            {
                // 拡張データが無効な場合、遷移なし
                return TransitionType_None;
            }

            // セルの中心座標（頂点座標）を取得
            // セル (x, y) の中心は頂点 (x, y) に相当
            GridPoint centerPoint = _grid.GetGridPoint(_x, _y);
            int centerBiomeId = centerPoint.BiomeId;

            // 隣接セルのバイオームIDを取得（上下左右）
            int northBiomeId = GetNeighborBiomeId(_x, _y, 0, 1, _grid);   // 上
            int eastBiomeId = GetNeighborBiomeId(_x, _y, 1, 0, _grid);   // 右
            int southBiomeId = GetNeighborBiomeId(_x, _y, 0, -1, _grid); // 下
            int westBiomeId = GetNeighborBiomeId(_x, _y, -1, 0, _grid);  // 左

            // 隣接セルと異なるバイオームがあるかチェック
            bool hasTransition = (northBiomeId != centerBiomeId) ||
                                 (eastBiomeId != centerBiomeId) ||
                                 (southBiomeId != centerBiomeId) ||
                                 (westBiomeId != centerBiomeId);

            if (!hasTransition)
            {
                return TransitionType_None;
            }

            // 最も代表的な遷移タイプを返す（優先順位: 北 > 東 > 南 > 西）
            if (northBiomeId != centerBiomeId)
            {
                return GetTransitionType(centerBiomeId, northBiomeId);
            }
            if (eastBiomeId != centerBiomeId)
            {
                return GetTransitionType(centerBiomeId, eastBiomeId);
            }
            if (southBiomeId != centerBiomeId)
            {
                return GetTransitionType(centerBiomeId, southBiomeId);
            }
            if (westBiomeId != centerBiomeId)
            {
                return GetTransitionType(centerBiomeId, westBiomeId);
            }

            return TransitionType_None;
        }

        /// <summary>
        /// 遷移タイプを取得（2つのバイオームIDから）
        /// </summary>
        /// <param name="_biomeId1">バイオームID1（現在のセル）</param>
        /// <param name="_biomeId2">バイオームID2（隣接セル）</param>
        /// <returns>遷移タイプ（TransitionType_*定数）</returns>
        public static int GetTransitionType(int _biomeId1, int _biomeId2)
        {
            // バイオームIDの定義（仕様書に基づく）
            // 0=水, 1=砂, 2=草, 3=岩, 4=山, 5=雪...

            // 同一バイオームの場合は遷移なし
            if (_biomeId1 == _biomeId2)
            {
                return TransitionType_None;
            }

            // 海→陸（水→砂/草/岩）
            if (_biomeId1 == 0 && (_biomeId2 == 1 || _biomeId2 == 2 || _biomeId2 == 3))
            {
                return TransitionType_SeaToLand;
            }

            // 陸→海（砂/草/岩→水）
            if ((_biomeId1 == 1 || _biomeId1 == 2 || _biomeId1 == 3) && _biomeId2 == 0)
            {
                return TransitionType_LandToSea;
            }

            // 陸→山（砂/草/岩→山）
            if ((_biomeId1 == 1 || _biomeId1 == 2 || _biomeId1 == 3) && _biomeId2 == 4)
            {
                return TransitionType_LandToMountain;
            }

            // 山→陸（山→砂/草/岩）
            if (_biomeId1 == 4 && (_biomeId2 == 1 || _biomeId2 == 2 || _biomeId2 == 3))
            {
                return TransitionType_MountainToLand;
            }

            // 砂→草（砂→草）
            if (_biomeId1 == 1 && _biomeId2 == 2)
            {
                return TransitionType_SandToGrass;
            }

            // 草→砂（草→砂）
            if (_biomeId1 == 2 && _biomeId2 == 1)
            {
                return TransitionType_GrassToSand;
            }

            // その他の遷移
            return TransitionType_Other;
        }

        /// <summary>
        /// 境界線モデルを選択
        /// 遷移タイプに基づいて、プレハブ配列から適切なモデルを選択
        /// </summary>
        /// <param name="_transitionType">遷移タイプ（TransitionType_*定数）</param>
        /// <param name="_boundaryModels">境界線モデル配列（遷移タイプをインデックスとして使用）</param>
        /// <returns>選択されたGameObject。該当するモデルがない場合はnull</returns>
        public static GameObject SelectBoundaryModel(int _transitionType, GameObject[] _boundaryModels)
        {
            if (_boundaryModels == null || _boundaryModels.Length == 0)
            {
                return null;
            }

            // 遷移タイプが配列の範囲内かチェック
            if (_transitionType >= 0 && _transitionType < _boundaryModels.Length)
            {
                return _boundaryModels[_transitionType];
            }

            // 範囲外の場合はTransitionType_Otherを試す
            if (TransitionType_Other < _boundaryModels.Length)
            {
                return _boundaryModels[TransitionType_Other];
            }

            return null;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 隣接セルのバイオームIDを取得
        /// </summary>
        /// <param name="_x">セルのX座標（セルインデックス）</param>
        /// <param name="_y">セルのY座標（セルインデックス）</param>
        /// <param name="_offsetX">X方向のオフセット（-1, 0, 1）</param>
        /// <param name="_offsetY">Y方向のオフセット（-1, 0, 1）</param>
        /// <param name="_grid">グリッドデータ</param>
        /// <returns>隣接セルのバイオームID。範囲外の場合は現在のセルのバイオームID</returns>
        private static int GetNeighborBiomeId(int _x, int _y, int _offsetX, int _offsetY, MarchingSquaresGrid _grid)
        {
            int neighborX = _x + _offsetX;
            int neighborY = _y + _offsetY;

            // 範囲外チェック
            if (neighborX < 0 || neighborX >= _grid.Width || neighborY < 0 || neighborY >= _grid.Height)
            {
                // 範囲外の場合は現在のセルのバイオームIDを返す（遷移なしとして扱う）
                return _grid.GetGridPoint(_x, _y).BiomeId;
            }

            return _grid.GetGridPoint(neighborX, neighborY).BiomeId;
        }
        #endregion
    }
}
