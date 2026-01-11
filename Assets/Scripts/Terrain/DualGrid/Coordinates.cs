using UnityEngine;

namespace Vastcore.Terrain.DualGrid
{
    /// <summary>
    /// 六角形グリッドと四角形グリッドの座標変換ロジック
    /// Axial座標系 (q, r) を使用して六角形を管理する
    /// </summary>
    public static class Coordinates
    {
        #region Constants
        /// <summary>
        /// 六角形のサイズ（半径）
        /// </summary>
        private const float c_HexSize = 1.0f;
        
        /// <summary>
        /// 六角形の高さ（垂直方向の距離）
        /// </summary>
        private const float c_HexHeight = c_HexSize * 2.0f;
        
        /// <summary>
        /// 六角形の幅（水平方向の距離）
        /// sqrt(3) ≈ 1.73205080757
        /// </summary>
        private static readonly float c_HexWidth = Mathf.Sqrt(3.0f) * c_HexSize;
        #endregion

        #region Axial to World
        /// <summary>
        /// Axial座標 (q, r) をワールド座標 (x, z) に変換
        /// </summary>
        public static Vector2 AxialToWorld(int _q, int _r)
        {
            float x = c_HexWidth * (_q + _r * 0.5f);
            float z = c_HexHeight * _r * 0.75f;
            return new Vector2(x, z);
        }
        
        /// <summary>
        /// Axial座標 (q, r) をワールド座標 Vector3 (x, 0, z) に変換
        /// </summary>
        public static Vector3 AxialToWorld3D(int _q, int _r)
        {
            Vector2 world2D = AxialToWorld(_q, _r);
            return new Vector3(world2D.x, 0f, world2D.y);
        }
        #endregion

        #region World to Axial
        /// <summary>
        /// ワールド座標 (x, z) を Axial座標 (q, r) に変換
        /// </summary>
        public static Vector2Int WorldToAxial(float _x, float _z)
        {
            float q = (_x / c_HexWidth) - (_z / c_HexHeight) * (1.0f / 3.0f);
            float r = (_z / c_HexHeight) * (2.0f / 3.0f);
            
            // 丸め処理（最も近い六角形を取得）
            return HexRound(q, r);
        }
        
        /// <summary>
        /// ワールド座標 Vector3 を Axial座標 (q, r) に変換
        /// </summary>
        public static Vector2Int WorldToAxial(Vector3 _worldPos)
        {
            return WorldToAxial(_worldPos.x, _worldPos.z);
        }
        #endregion

        #region Hex Rounding
        /// <summary>
        /// 浮動小数点のAxial座標を最も近い整数座標に丸める
        /// </summary>
        private static Vector2Int HexRound(float _q, float _r)
        {
            float s = -_q - _r;
            
            int qRounded = Mathf.RoundToInt(_q);
            int rRounded = Mathf.RoundToInt(_r);
            int sRounded = Mathf.RoundToInt(s);
            
            float qDiff = Mathf.Abs(qRounded - _q);
            float rDiff = Mathf.Abs(rRounded - _r);
            float sDiff = Mathf.Abs(sRounded - s);
            
            if (qDiff > rDiff && qDiff > sDiff)
            {
                qRounded = -rRounded - sRounded;
            }
            else if (rDiff > sDiff)
            {
                rRounded = -qRounded - sRounded;
            }
            
            return new Vector2Int(qRounded, rRounded);
        }
        #endregion

        #region Hex Neighbors
        /// <summary>
        /// 六角形の6方向の隣接座標オフセット（Axial座標系）
        /// </summary>
        private static readonly Vector2Int[] s_HexDirections = new Vector2Int[]
        {
            new Vector2Int(1, 0),   // 右
            new Vector2Int(1, -1),  // 右上
            new Vector2Int(0, -1),  // 左上
            new Vector2Int(-1, 0), // 左
            new Vector2Int(-1, 1), // 左下
            new Vector2Int(0, 1)   // 右下
        };
        
        /// <summary>
        /// 指定されたAxial座標の隣接座標を取得
        /// </summary>
        public static Vector2Int GetHexNeighbor(int _q, int _r, int _direction)
        {
            if (_direction < 0 || _direction >= s_HexDirections.Length)
            {
                Debug.LogWarning($"Invalid hex direction: {_direction}");
                return new Vector2Int(_q, _r);
            }
            
            Vector2Int offset = s_HexDirections[_direction];
            return new Vector2Int(_q + offset.x, _r + offset.y);
        }
        #endregion

        #region Subdivision
        /// <summary>
        /// 六角形を3つの四角形サブセルに分割する際の識別子
        /// 各サブセルは (q, r, index) で識別される（index: 0, 1, 2）
        /// </summary>
        /// <param name="_q">六角形のAxial座標 q</param>
        /// <param name="_r">六角形のAxial座標 r</param>
        /// <param name="_subIndex">サブセルのインデックス (0, 1, 2)</param>
        /// <returns>サブセルの中心座標（ワールド座標）</returns>
        public static Vector3 GetSubCellCenter(int _q, int _r, int _subIndex)
        {
            if (_subIndex < 0 || _subIndex > 2)
            {
                Debug.LogWarning($"Invalid sub cell index: {_subIndex}");
                return AxialToWorld3D(_q, _r);
            }
            
            // 六角形の中心座標
            Vector3 hexCenter = AxialToWorld3D(_q, _r);
            
            // 六角形の6つの頂点の角度（60度間隔）
            float angleStep = Mathf.PI / 3.0f;
            float startAngle = -Mathf.PI / 2.0f; // 上から開始
            
            // サブセルの中心は、六角形の中心と2つの隣接する頂点の中点を結ぶ線上の点
            // 簡易実装: 六角形の中心から、各サブセル方向へのオフセットを計算
            float subAngle = startAngle + (_subIndex * 2.0f * angleStep);
            float offsetDistance = c_HexSize * 0.5f; // 中心から少し外側
            
            float offsetX = Mathf.Cos(subAngle) * offsetDistance;
            float offsetZ = Mathf.Sin(subAngle) * offsetDistance;
            
            return hexCenter + new Vector3(offsetX, 0f, offsetZ);
        }
        #endregion
    }
}
