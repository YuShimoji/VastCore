using UnityEngine;

namespace Vastcore.Terrain.MarchingSquares
{
    /// <summary>
    /// Marching Squares用の拡張データ構造
    /// 各グリッド頂点に高さ・バイオーム・道路・建物の情報を保持
    /// </summary>
    [System.Serializable]
    public struct GridPoint
    {
        #region Public Fields
        /// <summary>
        /// 高さ値（float、デフォルト: 0.0）
        /// </summary>
        public float Height;

        /// <summary>
        /// バイオームID（int、デフォルト: 0）
        /// </summary>
        public int BiomeId;

        /// <summary>
        /// 道路ID（int、デフォルト: 0、0=道路なし）
        /// </summary>
        public int RoadId;

        /// <summary>
        /// 建物ID（int、デフォルト: 0、0=建物なし）
        /// </summary>
        public int BuildingId;
        #endregion

        #region Constructor
        /// <summary>
        /// GridPointを初期化
        /// </summary>
        /// <param name="_height">高さ値（デフォルト: 0.0）</param>
        /// <param name="_biomeId">バイオームID（デフォルト: 0）</param>
        /// <param name="_roadId">道路ID（デフォルト: 0）</param>
        /// <param name="_buildingId">建物ID（デフォルト: 0）</param>
        public GridPoint(float _height = 0.0f, int _biomeId = 0, int _roadId = 0, int _buildingId = 0)
        {
            Height = _height;
            BiomeId = _biomeId;
            RoadId = _roadId;
            BuildingId = _buildingId;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// 埋まっているか（既存のbool値と互換）
        /// 高さが0より大きい、または建物IDが0より大きい場合にTrue
        /// </summary>
        public bool IsFilled
        {
            get
            {
                // 高さが0より大きい、または建物IDが0より大きい場合にTrue
                return Height > 0.0f || BuildingId > 0;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// デフォルトのGridPointを取得（全て0）
        /// </summary>
        public static GridPoint Default => new GridPoint(0.0f, 0, 0, 0);

        /// <summary>
        /// 既存のbool値からGridPointを作成
        /// </summary>
        /// <param name="_isFilled">埋まっているか</param>
        /// <returns>GridPoint（IsFilledがTrueの場合はHeight=1.0、Falseの場合はHeight=0.0）</returns>
        public static GridPoint FromBool(bool _isFilled)
        {
            return new GridPoint(_isFilled ? 1.0f : 0.0f, 0, 0, 0);
        }

        /// <summary>
        /// 文字列表現を取得（デバッグ用）
        /// </summary>
        public override string ToString()
        {
            return $"GridPoint(Height={Height:F2}, BiomeId={BiomeId}, RoadId={RoadId}, BuildingId={BuildingId}, IsFilled={IsFilled})";
        }
        #endregion
    }
}
