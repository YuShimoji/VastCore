using UnityEngine;

namespace Vastcore.Terrain.MarchingSquares
{
    /// <summary>
    /// 高さマップ処理ロジック
    /// 高さマップ（Texture2D）からHeightを設定し、崖・スロープモデルを選択
    /// </summary>
    public static class HeightMapProcessor
    {
        #region Constants
        /// <summary>
        /// スロープタイプ: 平地（高低差が小さい）
        /// </summary>
        public const int SlopeType_Flat = 0;

        /// <summary>
        /// スロープタイプ: 緩やかなスロープ（高低差が中程度）
        /// </summary>
        public const int SlopeType_Gentle = 1;

        /// <summary>
        /// スロープタイプ: 急なスロープ（高低差が大きい）
        /// </summary>
        public const int SlopeType_Steep = 2;

        /// <summary>
        /// スロープタイプ: 崖（高低差が非常に大きい）
        /// </summary>
        public const int SlopeType_Cliff = 3;

        /// <summary>
        /// 平地と判定する最大高低差（閾値）
        /// </summary>
        private const float c_FlatThreshold = 0.1f;

        /// <summary>
        /// 緩やかなスロープと判定する最大高低差（閾値）
        /// </summary>
        private const float c_GentleThreshold = 0.5f;

        /// <summary>
        /// 急なスロープと判定する最大高低差（閾値）
        /// </summary>
        private const float c_SteepThreshold = 1.5f;
        #endregion

        #region Public Methods
        /// <summary>
        /// 高さマップからHeightを設定
        /// グリッドの各頂点に対応する高さマップのピクセル値を読み取り、Heightを設定
        /// </summary>
        /// <param name="_heightMap">高さマップ（Texture2D、グレースケール）</param>
        /// <param name="_grid">グリッドデータ</param>
        /// <param name="_heightScale">高さのスケール（デフォルト: 1.0、0.0～1.0の値を実際の高さに変換）</param>
        /// <returns>処理された頂点の数。エラー時は-1</returns>
        public static int ProcessHeightMap(Texture2D _heightMap, MarchingSquaresGrid _grid, float _heightScale = 1.0f)
        {
            if (_heightMap == null)
            {
                Debug.LogError("HeightMapProcessor.ProcessHeightMap: HeightMap is null.");
                return -1;
            }

            if (_grid == null)
            {
                Debug.LogError("HeightMapProcessor.ProcessHeightMap: Grid is null.");
                return -1;
            }

            if (!_grid.UseExtendedData)
            {
                Debug.LogWarning("HeightMapProcessor.ProcessHeightMap: ExtendedData is not enabled. Enabling ExtendedData...");
                _grid.UseExtendedData = true;
            }

            int processedCount = 0;

            // グリッドの各頂点を処理
            for (int x = 0; x < _grid.Width; x++)
            {
                for (int y = 0; y < _grid.Height; y++)
                {
                    // グリッド座標での高さを計算
                    float height = CalculateHeightAt(x, y, _heightMap, _grid, _heightScale);

                    // GridPointを取得して更新
                    GridPoint point = _grid.GetGridPoint(x, y);
                    point.Height = height;
                    _grid.SetGridPoint(x, y, point);

                    processedCount++;
                }
            }

            Debug.Log($"HeightMapProcessor.ProcessHeightMap: Processed {processedCount} vertices.");
            return processedCount;
        }

        /// <summary>
        /// グリッド座標での高さを計算
        /// 高さマップのピクセル値を読み取り、グリッド座標に対応する高さを返す
        /// </summary>
        /// <param name="_x">X座標（頂点インデックス）</param>
        /// <param name="_y">Y座標（頂点インデックス）</param>
        /// <param name="_heightMap">高さマップ（Texture2D、グレースケール）</param>
        /// <param name="_grid">グリッドデータ（セルサイズを取得するため）</param>
        /// <param name="_heightScale">高さのスケール（デフォルト: 1.0）</param>
        /// <returns>高さ値（0.0～heightScale）</returns>
        public static float CalculateHeightAt(int _x, int _y, Texture2D _heightMap, MarchingSquaresGrid _grid, float _heightScale = 1.0f)
        {
            if (_heightMap == null || _grid == null)
            {
                return 0.0f;
            }

            // グリッド座標を高さマップのUV座標（0.0～1.0）に変換
            float u = (float)_x / Mathf.Max(1, _grid.Width - 1);
            float v = (float)_y / Mathf.Max(1, _grid.Height - 1);

            // UV座標をピクセル座標に変換
            int pixelX = Mathf.RoundToInt(u * (_heightMap.width - 1));
            int pixelY = Mathf.RoundToInt(v * (_heightMap.height - 1));

            // ピクセル座標を範囲内に制限
            pixelX = Mathf.Clamp(pixelX, 0, _heightMap.width - 1);
            pixelY = Mathf.Clamp(pixelY, 0, _heightMap.height - 1);

            // ピクセル値を取得（グレースケールとして扱う）
            Color pixelColor = _heightMap.GetPixel(pixelX, pixelY);
            float grayscale = pixelColor.grayscale; // 0.0～1.0

            // 高さスケールを適用
            return grayscale * _heightScale;
        }

        /// <summary>
        /// スロープモデルを選択
        /// セルを構成する4頂点のHeightを比較し、高低差に基づいてスロープタイプを判定
        /// </summary>
        /// <param name="_height1">頂点1の高さ（Top-Left）</param>
        /// <param name="_height2">頂点2の高さ（Top-Right）</param>
        /// <param name="_height3">頂点3の高さ（Bottom-Right）</param>
        /// <param name="_height4">頂点4の高さ（Bottom-Left）</param>
        /// <returns>スロープタイプ（SlopeType_*定数）</returns>
        public static int SelectSlopeModel(float _height1, float _height2, float _height3, float _height4)
        {
            // 4頂点の高さから最大・最小を計算
            float minHeight = Mathf.Min(_height1, _height2, _height3, _height4);
            float maxHeight = Mathf.Max(_height1, _height2, _height3, _height4);
            float heightDifference = maxHeight - minHeight;

            // 高低差に基づいてスロープタイプを判定
            if (heightDifference <= c_FlatThreshold)
            {
                return SlopeType_Flat;
            }
            else if (heightDifference <= c_GentleThreshold)
            {
                return SlopeType_Gentle;
            }
            else if (heightDifference <= c_SteepThreshold)
            {
                return SlopeType_Steep;
            }
            else
            {
                return SlopeType_Cliff;
            }
        }

        /// <summary>
        /// スロープモデルを選択（GridPointから）
        /// セルの4頂点のGridPointからHeightを取得し、スロープタイプを判定
        /// </summary>
        /// <param name="_x">セルのX座標（セルインデックス）</param>
        /// <param name="_y">セルのY座標（セルインデックス）</param>
        /// <param name="_grid">グリッドデータ</param>
        /// <returns>スロープタイプ（SlopeType_*定数）。拡張データが無効な場合はSlopeType_Flat</returns>
        public static int SelectSlopeModel(int _x, int _y, MarchingSquaresGrid _grid)
        {
            if (_grid == null || !_grid.UseExtendedData)
            {
                return SlopeType_Flat;
            }

            // セル (x, y) の4つの頂点データを取得
            // セルは4つの頂点で構成される:
            // - Top-Left (TL): (x, y+1)
            // - Top-Right (TR): (x+1, y+1)
            // - Bottom-Right (BR): (x+1, y)
            // - Bottom-Left (BL): (x, y)
            float heightTL = GetHeightAt(_x, _y + 1, _grid);      // Top-Left
            float heightTR = GetHeightAt(_x + 1, _y + 1, _grid);  // Top-Right
            float heightBR = GetHeightAt(_x + 1, _y, _grid);      // Bottom-Right
            float heightBL = GetHeightAt(_x, _y, _grid);          // Bottom-Left

            return SelectSlopeModel(heightTL, heightTR, heightBR, heightBL);
        }

        /// <summary>
        /// スロープモデルを選択（プレハブ配列から）
        /// スロープタイプに基づいて、プレハブ配列から適切なモデルを選択
        /// </summary>
        /// <param name="_slopeType">スロープタイプ（SlopeType_*定数）</param>
        /// <param name="_slopeModels">スロープモデル配列（スロープタイプをインデックスとして使用）</param>
        /// <returns>選択されたGameObject。該当するモデルがない場合はnull</returns>
        public static GameObject SelectSlopeModelPrefab(int _slopeType, GameObject[] _slopeModels)
        {
            if (_slopeModels == null || _slopeModels.Length == 0)
            {
                return null;
            }

            // スロープタイプが配列の範囲内かチェック
            if (_slopeType >= 0 && _slopeType < _slopeModels.Length)
            {
                return _slopeModels[_slopeType];
            }

            // 範囲外の場合はSlopeType_Flatを試す
            if (SlopeType_Flat < _slopeModels.Length)
            {
                return _slopeModels[SlopeType_Flat];
            }

            return null;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 指定座標の高さを取得
        /// </summary>
        /// <param name="_x">X座標（頂点インデックス）</param>
        /// <param name="_y">Y座標（頂点インデックス）</param>
        /// <param name="_grid">グリッドデータ</param>
        /// <returns>高さ値。範囲外の場合は0.0</returns>
        private static float GetHeightAt(int _x, int _y, MarchingSquaresGrid _grid)
        {
            if (_grid == null || !_grid.UseExtendedData)
            {
                return 0.0f;
            }

            // 範囲外チェック
            if (_x < 0 || _x >= _grid.Width || _y < 0 || _y >= _grid.Height)
            {
                return 0.0f;
            }

            return _grid.GetGridPoint(_x, _y).Height;
        }
        #endregion
    }
}
