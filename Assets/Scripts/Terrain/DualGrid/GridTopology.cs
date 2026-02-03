using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.Terrain.DualGrid
{
    /// <summary>
    /// グリッドトポロジー生成ロジック
    /// 六角形グリッドを3分割四角形に変換し、隣接関係を構築する
    /// </summary>
    public static class GridTopology
    {
        #region Public Methods
        /// <summary>
        /// 指定された半径で六角形グリッドを生成し、各六角形を3つの四角形に分割
        /// </summary>
        /// <param name="_radius">六角形グリッドの半径</param>
        /// <param name="_nodes">生成されたノードのリスト（出力）</param>
        /// <param name="_cells">生成されたセルのリスト（出力）</param>
        public static void GenerateHexToQuadGrid(int _radius, out List<Node> _nodes, out List<Cell> _cells)
        {
            _nodes = new List<Node>();
            _cells = new List<Cell>();
            
            // ノードIDとセルIDのカウンター
            int nodeIdCounter = 0;
            int cellIdCounter = 0;
            
            // ノードの重複を避けるための辞書（座標→ノードID）
            Dictionary<Vector2Int, int> nodeMap = new Dictionary<Vector2Int, int>();
            
            // 六角形グリッドを生成
            for (int q = -_radius; q <= _radius; q++)
            {
                int r1 = Mathf.Max(-_radius, -q - _radius);
                int r2 = Mathf.Min(_radius, -q + _radius);
                
                for (int r = r1; r <= r2; r++)
                {
                    // 各六角形を3つの四角形に分割
                    for (int subIndex = 0; subIndex < 3; subIndex++)
                    {
                        Cell cell = new Cell(cellIdCounter++, q, r, subIndex);
                        
                        // このセルの4つの頂点を取得または作成
                        Node[] corners = GetOrCreateCellCorners(q, r, subIndex, ref nodeIdCounter, nodeMap, _nodes);
                        cell.Corners = corners;
                        
                        _cells.Add(cell);
                    }
                }
            }
            
            // 隣接関係を構築
            BuildNeighborRelations(_cells);
        }
        
        /// <summary>
        /// セルの4つの頂点を取得または作成
        /// 各六角形を中心点と各辺の中点で3つの四角形（菱形）に分割
        /// </summary>
        private static Node[] GetOrCreateCellCorners(int _q, int _r, int _subIndex, ref int _nodeIdCounter, 
            Dictionary<Vector2Int, int> _nodeMap, List<Node> _nodes)
        {
            Node[] corners = new Node[4];
            
            // 六角形の中心座標
            Vector3 hexCenter = Coordinates.AxialToWorld3D(_q, _r);
            
            // 六角形の6つの頂点の角度（60度間隔）
            float angleStep = Mathf.PI / 3.0f;
            float startAngle = -Mathf.PI / 2.0f; // 上から開始
            float hexSize = 1.0f;
            
            // 各六角形を3つの四角形に分割:
            // サブセル0: 中心, 頂点0, 辺0-1の中点, 頂点1
            // サブセル1: 中心, 頂点2, 辺2-3の中点, 頂点3
            // サブセル2: 中心, 頂点4, 辺4-5の中点, 頂点5
            
            int baseVertexIndex = _subIndex * 2; // 0, 2, 4
            int nextVertexIndex = (baseVertexIndex + 1) % 6; // 1, 3, 5
            
            // 頂点0: 六角形の中心
            corners[0] = GetOrCreateNode(hexCenter, ref _nodeIdCounter, _nodeMap, _nodes);
            
            // 頂点1: 六角形の頂点（baseVertexIndex）
            float angle1 = startAngle + (baseVertexIndex * angleStep);
            Vector3 vertex1 = hexCenter + new Vector3(
                Mathf.Cos(angle1) * hexSize,
                0f,
                Mathf.Sin(angle1) * hexSize
            );
            corners[1] = GetOrCreateNode(vertex1, ref _nodeIdCounter, _nodeMap, _nodes);
            
            // 頂点2: 辺の中点（baseVertexIndex と nextVertexIndex の中点）
            float angle2 = startAngle + (nextVertexIndex * angleStep);
            Vector3 vertex2 = hexCenter + new Vector3(
                Mathf.Cos(angle2) * hexSize,
                0f,
                Mathf.Sin(angle2) * hexSize
            );
            Vector3 edgeMidpoint = (vertex1 + vertex2) * 0.5f;
            corners[2] = GetOrCreateNode(edgeMidpoint, ref _nodeIdCounter, _nodeMap, _nodes);
            
            // 頂点3: 六角形の頂点（nextVertexIndex）
            corners[3] = GetOrCreateNode(vertex2, ref _nodeIdCounter, _nodeMap, _nodes);
            
            return corners;
        }
        
        /// <summary>
        /// ノードを取得または作成（重複チェック付き）
        /// </summary>
        private static Node GetOrCreateNode(Vector3 _position, ref int _nodeIdCounter, 
            Dictionary<Vector2Int, int> _nodeMap, List<Node> _nodes)
        {
            // 座標を整数化して重複チェック（簡易実装）
            Vector2Int key = new Vector2Int(Mathf.RoundToInt(_position.x * 100f), Mathf.RoundToInt(_position.z * 100f));
            
            if (_nodeMap.ContainsKey(key))
            {
                // 既存のノードを取得
                int existingNodeId = _nodeMap[key];
                return _nodes.Find(n => n.Id == existingNodeId);
            }
            else
            {
                // 新しいノードを作成
                Node node = new Node(_nodeIdCounter++, _position);
                _nodes.Add(node);
                _nodeMap[key] = node.Id;
                return node;
            }
        }
        
        /// <summary>
        /// 隣接関係を構築
        /// </summary>
        private static void BuildNeighborRelations(List<Cell> _cells)
        {
            // セルを辞書に登録（検索高速化）
            Dictionary<string, Cell> cellMap = new Dictionary<string, Cell>();
            foreach (Cell cell in _cells)
            {
                string key = GetCellKey(cell.HexQ, cell.HexR, cell.SubIndex);
                cellMap[key] = cell;
            }
            
            // 各セルの隣接セルを検索
            foreach (Cell cell in _cells)
            {
                // 4方向の隣接セルを検索
                for (int dir = 0; dir < 4; dir++)
                {
                    Cell neighbor = FindNeighborCell(cell, dir, cellMap);
                    if (neighbor != null)
                    {
                        cell.Neighbors[dir] = neighbor;
                    }
                }
            }
        }
        
        /// <summary>
        /// セルのキーを生成（辞書検索用）
        /// </summary>
        private static string GetCellKey(int _q, int _r, int _subIndex)
        {
            return $"{_q},{_r},{_subIndex}";
        }
        
        /// <summary>
        /// 指定方向の隣接セルを検索
        /// </summary>
        private static Cell FindNeighborCell(Cell _cell, int _direction, Dictionary<string, Cell> _cellMap)
        {
            // 簡易実装: 六角形グリッドの隣接関係を考慮して検索
            // 実際の実装では、六角形の隣接関係とサブセルの位置関係を考慮する必要がある
            
            // 方向に応じて六角形座標をオフセット
            Vector2Int hexOffset = GetHexOffsetForDirection(_direction);
            int newQ = _cell.HexQ + hexOffset.x;
            int newR = _cell.HexR + hexOffset.y;
            
            // 同じ六角形内の別のサブセル、または隣接する六角形のサブセルを検索
            for (int subIndex = 0; subIndex < 3; subIndex++)
            {
                string key = GetCellKey(newQ, newR, subIndex);
                if (_cellMap.ContainsKey(key))
                {
                    return _cellMap[key];
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// 方向に応じた六角形座標のオフセットを取得
        /// </summary>
        private static Vector2Int GetHexOffsetForDirection(int _direction)
        {
            // 4方向（上下左右）を六角形の6方向にマッピング
            switch (_direction)
            {
                case 0: // 上
                    return new Vector2Int(0, -1);
                case 1: // 右
                    return new Vector2Int(1, 0);
                case 2: // 下
                    return new Vector2Int(0, 1);
                case 3: // 左
                    return new Vector2Int(-1, 0);
                default:
                    return Vector2Int.zero;
            }
        }
        #endregion
    }
}
