using UnityEngine;

namespace Vastcore.Terrain.DualGrid
{
    /// <summary>
    /// 頂点データ構造（Dual Grid方式の「角」）
    /// 洞窟・オーバーハングを実現するため、垂直方向の接続性を持つ
    /// </summary>
    public class Node
    {
        #region Public Properties
        /// <summary>
        /// ノードの一意なID
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// 緩和処理済みの座標（ワールド座標）
        /// </summary>
        public Vector3 Position { get; set; }
        
        /// <summary>
        /// 足元に地面があるか（床があるか）
        /// </summary>
        public bool HasGround { get; set; }
        
        /// <summary>
        /// 頭上に天井があるか
        /// </summary>
        public bool HasCeiling { get; set; }
        
        /// <summary>
        /// 高さの階層インデックス (0, 1, 2...)
        /// </summary>
        public int HeightIndex { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public Node()
        {
            Id = -1;
            Position = Vector3.zero;
            HasGround = false;
            HasCeiling = false;
            HeightIndex = 0;
        }
        
        /// <summary>
        /// 位置を指定してノードを作成
        /// </summary>
        public Node(int _id, Vector3 _position)
        {
            Id = _id;
            Position = _position;
            HasGround = false;
            HasCeiling = false;
            HeightIndex = 0;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// ノードが固体（Solid）かどうかを判定
        /// 地面または天井のいずれかがあれば固体とみなす
        /// </summary>
        public bool IsSolid()
        {
            return HasGround || HasCeiling;
        }
        
        /// <summary>
        /// ノードの状態を文字列で取得（デバッグ用）
        /// </summary>
        public override string ToString()
        {
            return $"Node[{Id}] Pos:{Position} Ground:{HasGround} Ceiling:{HasCeiling} Height:{HeightIndex}";
        }
        #endregion
    }
}
