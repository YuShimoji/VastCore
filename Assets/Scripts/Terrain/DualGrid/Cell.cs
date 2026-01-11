namespace Vastcore.Terrain.DualGrid
{
    /// <summary>
    /// セルデータ構造（4つのNodeに囲まれた領域）
    /// レンダリングの単位となる
    /// </summary>
    public class Cell
    {
        #region Public Properties
        /// <summary>
        /// セルの一意なID
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// 構成する4つの頂点（時計回りまたは反時計回りに並んでいる）
        /// </summary>
        public Node[] Corners { get; set; }
        
        /// <summary>
        /// 隣接する4つのセル（上下左右）
        /// nullの場合は隣接セルが存在しない（境界）
        /// </summary>
        public Cell[] Neighbors { get; set; }
        
        /// <summary>
        /// 六角形のAxial座標 q
        /// </summary>
        public int HexQ { get; set; }
        
        /// <summary>
        /// 六角形のAxial座標 r
        /// </summary>
        public int HexR { get; set; }
        
        /// <summary>
        /// サブセルのインデックス (0, 1, 2)
        /// </summary>
        public int SubIndex { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public Cell()
        {
            Id = -1;
            Corners = new Node[4];
            Neighbors = new Cell[4];
            HexQ = 0;
            HexR = 0;
            SubIndex = 0;
        }
        
        /// <summary>
        /// IDと六角形座標を指定してセルを作成
        /// </summary>
        public Cell(int _id, int _hexQ, int _hexR, int _subIndex)
        {
            Id = _id;
            Corners = new Node[4];
            Neighbors = new Cell[4];
            HexQ = _hexQ;
            HexR = _hexR;
            SubIndex = _subIndex;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// セルの中心座標を計算（4つの頂点の平均）
        /// </summary>
        public UnityEngine.Vector3 GetCenter()
        {
            if (Corners == null || Corners.Length != 4)
            {
                return UnityEngine.Vector3.zero;
            }
            
            UnityEngine.Vector3 center = UnityEngine.Vector3.zero;
            int validCorners = 0;
            
            for (int i = 0; i < 4; i++)
            {
                if (Corners[i] != null)
                {
                    center += Corners[i].Position;
                    validCorners++;
                }
            }
            
            if (validCorners > 0)
            {
                center /= validCorners;
            }
            
            return center;
        }
        
        /// <summary>
        /// セルの状態を文字列で取得（デバッグ用）
        /// </summary>
        public override string ToString()
        {
            return $"Cell[{Id}] Hex({HexQ},{HexR}) Sub[{SubIndex}] Corners:{Corners?.Length ?? 0}";
        }
        #endregion
    }
}
