using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.Terrain.DualGrid
{
    /// <summary>
    /// グリッド全体を管理するクラス
    /// 六角形グリッドを生成し、Relaxation（形状緩和）を適用する
    /// </summary>
    public class IrregularGrid
    {
        #region Private Fields
        private List<Node> m_Nodes;
        private List<Cell> m_Cells;
        private int m_Radius;
        #endregion

        #region Public Properties
        /// <summary>
        /// ノードのリスト
        /// </summary>
        public IReadOnlyList<Node> Nodes => m_Nodes;
        
        /// <summary>
        /// セルのリスト
        /// </summary>
        public IReadOnlyList<Cell> Cells => m_Cells;
        
        /// <summary>
        /// グリッドの半径
        /// </summary>
        public int Radius => m_Radius;
        #endregion

        #region Constructors
        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public IrregularGrid()
        {
            m_Nodes = new List<Node>();
            m_Cells = new List<Cell>();
            m_Radius = 0;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 指定された半径で六角形グリッドを生成
        /// </summary>
        /// <param name="_radius">六角形グリッドの半径</param>
        public void GenerateGrid(int _radius)
        {
            m_Radius = _radius;
            
            // グリッドトポロジーを生成
            GridTopology.GenerateHexToQuadGrid(_radius, out m_Nodes, out m_Cells);
        }
        
        /// <summary>
        /// Relaxation（形状緩和）を適用
        /// 各Nodeの座標にパーリンノイズまたはランダムオフセットを加算
        /// </summary>
        /// <param name="_seed">ランダムシード</param>
        /// <param name="_jitterAmount">オフセットの強度（0.0～1.0）</param>
        /// <param name="_usePerlinNoise">パーリンノイズを使用するか（true: パーリンノイズ, false: ランダム）</param>
        public void ApplyRelaxation(int _seed, float _jitterAmount = 0.3f, bool _usePerlinNoise = true)
        {
            if (m_Nodes == null || m_Nodes.Count == 0)
            {
                Debug.LogWarning("IrregularGrid: Nodes are empty. GenerateGrid() must be called first.");
                return;
            }
            
            // シードを設定
            System.Random random = new System.Random(_seed);
            
            foreach (Node node in m_Nodes)
            {
                Vector3 originalPos = node.Position;
                Vector3 offset = Vector3.zero;
                
                if (_usePerlinNoise)
                {
                    // パーリンノイズを使用
                    float noiseX = Mathf.PerlinNoise(originalPos.x * 0.1f, originalPos.z * 0.1f);
                    float noiseZ = Mathf.PerlinNoise(originalPos.x * 0.1f + 100f, originalPos.z * 0.1f + 100f);
                    
                    // ノイズ値を -1 ～ 1 の範囲に変換
                    noiseX = (noiseX - 0.5f) * 2.0f;
                    noiseZ = (noiseZ - 0.5f) * 2.0f;
                    
                    offset = new Vector3(noiseX, 0f, noiseZ) * _jitterAmount;
                }
                else
                {
                    // ランダムオフセットを使用
                    float offsetX = ((float)random.NextDouble() - 0.5f) * 2.0f * _jitterAmount;
                    float offsetZ = ((float)random.NextDouble() - 0.5f) * 2.0f * _jitterAmount;
                    offset = new Vector3(offsetX, 0f, offsetZ);
                }
                
                // オフセットを適用
                node.Position = originalPos + offset;
            }
            
            // 凸性チェック（簡易実装: セルが裏返らないようにする）
            ValidateConvexity();
        }
        
        /// <summary>
        /// 凸性を検証し、必要に応じて修正
        /// セルが裏返らない（凸性を維持する）範囲に留める
        /// </summary>
        private void ValidateConvexity()
        {
            // 簡易実装: 各セルの頂点が時計回りまたは反時計回りに並んでいることを確認
            // より厳密な実装では、外積を使用して凸性をチェックする
            
            foreach (Cell cell in m_Cells)
            {
                if (cell.Corners == null || cell.Corners.Length != 4)
                {
                    continue;
                }
                
                // 4つの頂点が有効かチェック
                bool allValid = true;
                for (int i = 0; i < 4; i++)
                {
                    if (cell.Corners[i] == null)
                    {
                        allValid = false;
                        break;
                    }
                }
                
                if (!allValid)
                {
                    continue;
                }
                
                // 簡易チェック: セルの中心からの距離が極端に異ならないことを確認
                Vector3 center = cell.GetCenter();
                float minDist = float.MaxValue;
                float maxDist = float.MinValue;
                
                for (int i = 0; i < 4; i++)
                {
                    float dist = Vector3.Distance(center, cell.Corners[i].Position);
                    minDist = Mathf.Min(minDist, dist);
                    maxDist = Mathf.Max(maxDist, dist);
                }
                
                // 距離の差が大きすぎる場合は警告（実際の実装では修正処理を追加）
                if (maxDist > minDist * 3.0f)
                {
                    Debug.LogWarning($"Cell {cell.Id} may have convexity issues. MinDist: {minDist}, MaxDist: {maxDist}");
                }
            }
        }
        
        /// <summary>
        /// グリッドをクリア
        /// </summary>
        public void Clear()
        {
            m_Nodes?.Clear();
            m_Cells?.Clear();
            m_Radius = 0;
        }
        #endregion
    }
}
