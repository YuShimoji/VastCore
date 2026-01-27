using UnityEngine;

namespace Vastcore.Terrain.MarchingSquares
{
    /// <summary>
    /// Marching Squares用のグリッドデータ管理クラス
    /// 各頂点が「埋まっている(True)」か「空(False)」かを保持する
    /// Phase 3: 拡張データ構造（GridPoint）をサポート
    /// </summary>
    public class MarchingSquaresGrid
    {
        #region Private Fields
        private bool[,] m_MapData;
        private GridPoint[,] m_ExtendedData;
        private int m_Width;
        private int m_Height;
        private float m_CellSize;
        private bool m_UseExtendedData;
        #endregion

        #region Public Properties
        /// <summary>
        /// グリッドの幅（頂点数）
        /// </summary>
        public int Width => m_Width;

        /// <summary>
        /// グリッドの高さ（頂点数）
        /// </summary>
        public int Height => m_Height;

        /// <summary>
        /// セルサイズ（ワールド座標単位）
        /// </summary>
        public float CellSize => m_CellSize;

        /// <summary>
        /// マップデータへの直接アクセス（読み取り専用）
        /// </summary>
        public bool[,] MapData => m_MapData;

        /// <summary>
        /// 拡張データを使用するか（デフォルト: false、後方互換性）
        /// </summary>
        public bool UseExtendedData
        {
            get => m_UseExtendedData;
            set
            {
                if (m_UseExtendedData != value)
                {
                    m_UseExtendedData = value;
                    if (m_UseExtendedData && m_ExtendedData == null)
                    {
                        // 拡張データを有効化する場合、配列を初期化
                        InitializeExtendedData();
                    }
                }
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// グリッドを初期化
        /// </summary>
        /// <param name="_width">グリッドの幅（頂点数）</param>
        /// <param name="_height">グリッドの高さ（頂点数）</param>
        /// <param name="_cellSize">セルサイズ（ワールド座標単位）</param>
        /// <param name="_useExtendedData">拡張データを使用するか（デフォルト: false）</param>
        public MarchingSquaresGrid(int _width, int _height, float _cellSize = 1.0f, bool _useExtendedData = false)
        {
            if (_width <= 0 || _height <= 0)
            {
                Debug.LogError($"MarchingSquaresGrid: Invalid grid size ({_width}, {_height}). Using default size (10, 10).");
                _width = 10;
                _height = 10;
            }

            if (_cellSize <= 0f)
            {
                Debug.LogError($"MarchingSquaresGrid: Invalid cell size ({_cellSize}). Using default size (1.0).");
                _cellSize = 1.0f;
            }

            m_Width = _width;
            m_Height = _height;
            m_CellSize = _cellSize;
            m_UseExtendedData = _useExtendedData;
            m_MapData = new bool[m_Width, m_Height];

            // 拡張データを使用する場合、配列を初期化
            if (m_UseExtendedData)
            {
                InitializeExtendedData();
            }

            // 初期化時は全てFalse（空）で初期化
            Clear();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 頂点データを設定
        /// </summary>
        /// <param name="_x">X座標（頂点インデックス）</param>
        /// <param name="_y">Y座標（頂点インデックス）</param>
        /// <param name="_value">埋まっている場合はTrue、空の場合はFalse</param>
        public void SetVertex(int _x, int _y, bool _value)
        {
            if (!IsValidIndex(_x, _y))
            {
                Debug.LogWarning($"MarchingSquaresGrid.SetVertex: Invalid index ({_x}, {_y}). Grid size is ({m_Width}, {m_Height}).");
                return;
            }

            m_MapData[_x, _y] = _value;

            // 拡張データを使用している場合、IsFilledを同期
            if (m_UseExtendedData && m_ExtendedData != null)
            {
                var point = m_ExtendedData[_x, _y];
                if (_value)
                {
                    // Trueの場合はHeight=1.0に設定（既存の動作と互換）
                    if (point.Height <= 0.0f)
                    {
                        point.Height = 1.0f;
                    }
                }
                else
                {
                    // Falseの場合はHeight=0.0に設定（BuildingIdが0の場合のみ）
                    if (point.BuildingId == 0)
                    {
                        point.Height = 0.0f;
                    }
                }
                m_ExtendedData[_x, _y] = point;
            }
        }

        /// <summary>
        /// 頂点データを取得
        /// </summary>
        /// <param name="_x">X座標（頂点インデックス）</param>
        /// <param name="_y">Y座標（頂点インデックス）</param>
        /// <returns>埋まっている場合はTrue、空の場合はFalse。範囲外の場合はFalse</returns>
        public bool GetVertex(int _x, int _y)
        {
            if (!IsValidIndex(_x, _y))
            {
                Debug.LogWarning($"MarchingSquaresGrid.GetVertex: Invalid index ({_x}, {_y}). Grid size is ({m_Width}, {m_Height}). Returning false.");
                return false;
            }

            // 拡張データを使用している場合、IsFilledを返す
            if (m_UseExtendedData && m_ExtendedData != null)
            {
                return m_ExtendedData[_x, _y].IsFilled;
            }

            return m_MapData[_x, _y];
        }

        /// <summary>
        /// GridPointを取得
        /// </summary>
        /// <param name="_x">X座標（頂点インデックス）</param>
        /// <param name="_y">Y座標（頂点インデックス）</param>
        /// <returns>GridPoint。拡張データが無効な場合は既存のbool値から変換</returns>
        public GridPoint GetGridPoint(int _x, int _y)
        {
            if (!IsValidIndex(_x, _y))
            {
                Debug.LogWarning($"MarchingSquaresGrid.GetGridPoint: Invalid index ({_x}, {_y}). Grid size is ({m_Width}, {m_Height}). Returning default.");
                return GridPoint.Default;
            }

            if (m_UseExtendedData && m_ExtendedData != null)
            {
                return m_ExtendedData[_x, _y];
            }

            // 拡張データが無効な場合、既存のbool値から変換
            return GridPoint.FromBool(m_MapData[_x, _y]);
        }

        /// <summary>
        /// GridPointを設定
        /// </summary>
        /// <param name="_x">X座標（頂点インデックス）</param>
        /// <param name="_y">Y座標（頂点インデックス）</param>
        /// <param name="_point">設定するGridPoint</param>
        public void SetGridPoint(int _x, int _y, GridPoint _point)
        {
            if (!IsValidIndex(_x, _y))
            {
                Debug.LogWarning($"MarchingSquaresGrid.SetGridPoint: Invalid index ({_x}, {_y}). Grid size is ({m_Width}, {m_Height}).");
                return;
            }

            // 拡張データを使用している場合
            if (m_UseExtendedData)
            {
                if (m_ExtendedData == null)
                {
                    InitializeExtendedData();
                }
                m_ExtendedData[_x, _y] = _point;
            }

            // 既存のbool値も同期（後方互換性）
            m_MapData[_x, _y] = _point.IsFilled;
        }

        /// <summary>
        /// 全ての頂点データをクリア（全てFalseに設定）
        /// </summary>
        public void Clear()
        {
            for (int x = 0; x < m_Width; x++)
            {
                for (int y = 0; y < m_Height; y++)
                {
                    m_MapData[x, y] = false;
                }
            }

            // 拡張データもクリア
            if (m_UseExtendedData && m_ExtendedData != null)
            {
                for (int x = 0; x < m_Width; x++)
                {
                    for (int y = 0; y < m_Height; y++)
                    {
                        m_ExtendedData[x, y] = GridPoint.Default;
                    }
                }
            }
        }

        /// <summary>
        /// グリッドを再初期化（サイズ変更）
        /// </summary>
        /// <param name="_width">新しい幅</param>
        /// <param name="_height">新しい高さ</param>
        /// <param name="_cellSize">新しいセルサイズ</param>
        public void Resize(int _width, int _height, float _cellSize = 1.0f)
        {
            if (_width <= 0 || _height <= 0)
            {
                Debug.LogError($"MarchingSquaresGrid.Resize: Invalid grid size ({_width}, {_height}).");
                return;
            }

            if (_cellSize <= 0f)
            {
                Debug.LogError($"MarchingSquaresGrid.Resize: Invalid cell size ({_cellSize}).");
                return;
            }

            m_Width = _width;
            m_Height = _height;
            m_CellSize = _cellSize;
            m_MapData = new bool[m_Width, m_Height];

            // 拡張データも再初期化
            if (m_UseExtendedData)
            {
                InitializeExtendedData();
            }
            else
            {
                m_ExtendedData = null;
            }

            Clear();
        }

        /// <summary>
        /// グリッド座標をワールド座標に変換
        /// </summary>
        /// <param name="_x">X座標（頂点インデックス）</param>
        /// <param name="_y">Y座標（頂点インデックス）</param>
        /// <returns>ワールド座標</returns>
        public Vector3 GridToWorld(int _x, int _y)
        {
            return new Vector3(_x * m_CellSize, 0f, _y * m_CellSize);
        }

        /// <summary>
        /// ワールド座標をグリッド座標に変換
        /// </summary>
        /// <param name="_worldPos">ワールド座標</param>
        /// <returns>グリッド座標（頂点インデックス）</returns>
        public Vector2Int WorldToGrid(Vector3 _worldPos)
        {
            int x = Mathf.RoundToInt(_worldPos.x / m_CellSize);
            int y = Mathf.RoundToInt(_worldPos.z / m_CellSize);
            return new Vector2Int(x, y);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// インデックスが有効かチェック
        /// </summary>
        private bool IsValidIndex(int _x, int _y)
        {
            return _x >= 0 && _x < m_Width && _y >= 0 && _y < m_Height;
        }

        /// <summary>
        /// 拡張データ配列を初期化
        /// </summary>
        private void InitializeExtendedData()
        {
            m_ExtendedData = new GridPoint[m_Width, m_Height];
            for (int x = 0; x < m_Width; x++)
            {
                for (int y = 0; y < m_Height; y++)
                {
                    m_ExtendedData[x, y] = GridPoint.Default;
                }
            }
        }
        #endregion
    }
}
