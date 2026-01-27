using UnityEngine;

#if UNITY_SPLINES
using Unity.Splines;
#endif

namespace Vastcore.Terrain.MarchingSquares
{
    /// <summary>
    /// スプラインをグリッドデータにラスタライズするクラス
    /// Unity Spline Packageを使用してスプライン曲線をグリッド座標に変換
    /// </summary>
    public static class SplineRasterizer
    {
        #region Constants
        private const float c_DefaultSamplingInterval = 0.1f;
        #endregion

        #region Public Methods
        /// <summary>
        /// スプラインをグリッドにラスタライズ
        /// スプライン上の点を一定間隔でサンプリングし、ブラシ範囲内の頂点を設定
        /// </summary>
        /// <param name="_spline">ラスタライズするスプライン</param>
        /// <param name="_brushRadius">ブラシ半径（ワールド座標単位）</param>
        /// <param name="_grid">グリッドデータ</param>
        /// <param name="_value">設定する値（true=埋める、false=削除）</param>
        /// <param name="_samplingInterval">サンプリング間隔（ワールド座標単位、デフォルト: 0.1）</param>
        /// <returns>ラスタライズされた頂点の数</returns>
        public static int RasterizeSpline(
#if UNITY_SPLINES
            Spline _spline,
#else
            object _spline,
#endif
            float _brushRadius,
            MarchingSquaresGrid _grid,
            bool _value = true,
            float _samplingInterval = c_DefaultSamplingInterval)
        {
#if !UNITY_SPLINES
            Debug.LogError("SplineRasterizer.RasterizeSpline: Unity Spline Package is not installed. Please install 'com.unity.splines' from Package Manager.");
            return 0;
#else
            if (_spline == null)
            {
                Debug.LogError("SplineRasterizer.RasterizeSpline: Spline is null.");
                return 0;
            }

            if (_grid == null)
            {
                Debug.LogError("SplineRasterizer.RasterizeSpline: Grid is null.");
                return 0;
            }

            if (_brushRadius <= 0f)
            {
                Debug.LogWarning($"SplineRasterizer.RasterizeSpline: Invalid brush radius ({_brushRadius}). Using default value (0.5).");
                _brushRadius = 0.5f;
            }

            if (_samplingInterval <= 0f)
            {
                Debug.LogWarning($"SplineRasterizer.RasterizeSpline: Invalid sampling interval ({_samplingInterval}). Using default value ({c_DefaultSamplingInterval}).");
                _samplingInterval = c_DefaultSamplingInterval;
            }

            int rasterizedCount = 0;

            // スプラインの長さを取得
            float splineLength = _spline.GetLength();

            if (splineLength <= 0f)
            {
                Debug.LogWarning("SplineRasterizer.RasterizeSpline: Spline length is zero or negative.");
                return 0;
            }

            // スプライン上の点を一定間隔でサンプリング
            // 正規化されたt値（0.0～1.0）を使用してスプライン上の点を取得
            float currentDistance = 0f;
            while (currentDistance <= splineLength)
            {
                // 正規化されたt値を計算
                float normalizedT = Mathf.Clamp01(currentDistance / splineLength);

                // スプライン上の点を取得（ローカル座標）
                Vector3 localPoint = SplineUtility.EvaluatePosition(_spline, normalizedT);

                // ワールド座標をグリッド座標に変換
                Vector2Int gridPos = _grid.WorldToGrid(localPoint);

                // ブラシ範囲内の頂点を設定
                int affectedCount = SetGridDataInRange(gridPos.x, gridPos.y, _brushRadius, _value, _grid);
                rasterizedCount += affectedCount;

                // 次のサンプリングポイントへ
                currentDistance += _samplingInterval;
            }

            // 最後の点（スプラインの終端）も処理
            Vector3 endPoint = SplineUtility.EvaluatePosition(_spline, 1f);
            Vector2Int endGridPos = _grid.WorldToGrid(endPoint);
            rasterizedCount += SetGridDataInRange(endGridPos.x, endGridPos.y, _brushRadius, _value, _grid);

            Debug.Log($"SplineRasterizer.RasterizeSpline: Rasterized {rasterizedCount} vertices from spline (length: {splineLength}, sampling interval: {_samplingInterval}).");
            return rasterizedCount;
#endif
        }

        /// <summary>
        /// 指定されたグリッド座標の周囲（ブラシ範囲内）の頂点を設定
        /// </summary>
        /// <param name="_gridX">グリッドX座標（頂点インデックス）</param>
        /// <param name="_gridY">グリッドY座標（頂点インデックス）</param>
        /// <param name="_radius">ブラシ半径（ワールド座標単位）</param>
        /// <param name="_value">設定する値（true=埋める、false=削除）</param>
        /// <param name="_grid">グリッドデータ</param>
        /// <returns>設定された頂点の数</returns>
        public static int SetGridDataInRange(
            int _gridX,
            int _gridY,
            float _radius,
            bool _value,
            MarchingSquaresGrid _grid)
        {
            if (_grid == null)
            {
                Debug.LogError("SplineRasterizer.SetGridDataInRange: Grid is null.");
                return 0;
            }

            if (_radius <= 0f)
            {
                Debug.LogWarning($"SplineRasterizer.SetGridDataInRange: Invalid radius ({_radius}). Using default value (0.5).");
                _radius = 0.5f;
            }

            int affectedCount = 0;

            // ブラシ半径をグリッド座標単位に変換
            float cellSize = _grid.CellSize;
            float radiusInGrid = _radius / cellSize;

            // ブラシ範囲内のグリッド座標を計算
            int minX = Mathf.FloorToInt(_gridX - radiusInGrid);
            int maxX = Mathf.CeilToInt(_gridX + radiusInGrid);
            int minY = Mathf.FloorToInt(_gridY - radiusInGrid);
            int maxY = Mathf.CeilToInt(_gridY + radiusInGrid);

            // 範囲内の各頂点をチェック
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    // グリッド範囲外の場合はスキップ
                    if (x < 0 || x >= _grid.Width || y < 0 || y >= _grid.Height)
                    {
                        continue;
                    }

                    // ワールド座標に変換して距離を計算
                    Vector3 gridWorldPos = _grid.GridToWorld(x, y);
                    Vector3 centerWorldPos = _grid.GridToWorld(_gridX, _gridY);
                    float distance = Vector3.Distance(gridWorldPos, centerWorldPos);

                    // ブラシ半径内の場合、頂点を設定
                    if (distance <= _radius)
                    {
                        _grid.SetVertex(x, y, _value);
                        affectedCount++;
                    }
                }
            }

            return affectedCount;
        }
        #endregion
    }
}
