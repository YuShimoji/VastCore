using UnityEngine;

namespace Vastcore.Terrain.DualGrid
{
    /// <summary>
    /// グリッドのデバッグ可視化クラス（MonoBehaviour）
    /// OnDrawGizmosを使用してNodes/Edges/Cellsを描画
    /// </summary>
    public class GridDebugVisualizer : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Grid Settings")]
        [SerializeField] private int m_GridRadius = 3;
        [SerializeField] private int m_Seed = 42;
        [SerializeField] private float m_JitterAmount = 0.3f;
        [SerializeField] private bool m_UsePerlinNoise = true;
        
        [Header("Height Settings")]
        [SerializeField] private int m_MaxHeight = 5;
        [SerializeField] private bool m_UseHeightMap = false;
        [SerializeField] private Texture2D m_HeightMap;
        
        [Header("Visualization Settings")]
        [SerializeField] private bool m_ShowNodes = true;
        [SerializeField] private bool m_ShowEdges = true;
        [SerializeField] private bool m_ShowCells = true;
        [SerializeField] private bool m_ShowVerticalStacks = true;
        [SerializeField] private float m_NodeSize = 0.1f;
        [SerializeField] private float m_EdgeWidth = 0.02f;
        [SerializeField] private Color m_NodeColor = Color.yellow;
        [SerializeField] private Color m_EdgeColor = Color.white;
        [SerializeField] private Color m_CellColor = new Color(1f, 0f, 0f, 0.3f);
        [SerializeField] private Color m_StackColor = new Color(0f, 1f, 0f, 0.5f);
        #endregion

        #region Private Fields
        private IrregularGrid m_Grid;
        private ColumnStack m_ColumnStack;
        private bool m_IsInitialized = false;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeGrid();
        }
        
        private void OnValidate()
        {
            // Inspectorで値が変更された場合、グリッドを再生成
            if (Application.isPlaying && m_IsInitialized)
            {
                InitializeGrid();
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// グリッドを初期化
        /// </summary>
        private void InitializeGrid()
        {
            m_Grid = new IrregularGrid();
            m_ColumnStack = new ColumnStack();
            
            // グリッドを生成
            m_Grid.GenerateGrid(m_GridRadius);
            
            // Relaxationを適用
            m_Grid.ApplyRelaxation(m_Seed, m_JitterAmount, m_UsePerlinNoise);
            
            // 高さを生成
            if (m_UseHeightMap && m_HeightMap != null)
            {
                VerticalExtrusionGenerator.GenerateFromHeightMap(m_Grid, m_ColumnStack, m_HeightMap, m_MaxHeight);
            }
            else
            {
                VerticalExtrusionGenerator.GenerateFromNoise(m_Grid, m_ColumnStack, m_Seed, m_MaxHeight);
            }
            
            m_IsInitialized = true;
        }
        #endregion

        #region Gizmos Drawing
        private void OnDrawGizmos()
        {
            if (!m_IsInitialized)
            {
                InitializeGrid();
            }
            
            if (m_Grid == null || m_ColumnStack == null)
            {
                return;
            }
            
            // ノードを描画
            if (m_ShowNodes)
            {
                DrawNodes();
            }
            
            // エッジを描画
            if (m_ShowEdges)
            {
                DrawEdges();
            }
            
            // セルを描画
            if (m_ShowCells)
            {
                DrawCells();
            }
            
            // 垂直スタックを描画
            if (m_ShowVerticalStacks)
            {
                DrawVerticalStacks();
            }
        }
        
        /// <summary>
        /// ノードを描画（小さいスフィア）
        /// </summary>
        private void DrawNodes()
        {
            Gizmos.color = m_NodeColor;
            
            foreach (Node node in m_Grid.Nodes)
            {
                if (node != null)
                {
                    Gizmos.DrawSphere(node.Position, m_NodeSize);
                }
            }
        }
        
        /// <summary>
        /// エッジを描画（Node同士を結ぶ線）
        /// </summary>
        private void DrawEdges()
        {
            Gizmos.color = m_EdgeColor;
            
            foreach (Cell cell in m_Grid.Cells)
            {
                if (cell.Corners == null || cell.Corners.Length != 4)
                {
                    continue;
                }
                
                // 4つの頂点を結ぶ線を描画
                for (int i = 0; i < 4; i++)
                {
                    int nextIndex = (i + 1) % 4;
                    
                    if (cell.Corners[i] != null && cell.Corners[nextIndex] != null)
                    {
                        Vector3 start = cell.Corners[i].Position;
                        Vector3 end = cell.Corners[nextIndex].Position;
                        
                        // 簡易実装: 線の太さは考慮しない（UnityのGizmos.DrawLineは太さを指定できない）
                        Gizmos.DrawLine(start, end);
                    }
                }
            }
        }
        
        /// <summary>
        /// セルを描画（セルの中心に色付きの面）
        /// </summary>
        private void DrawCells()
        {
            Gizmos.color = m_CellColor;
            
            foreach (Cell cell in m_Grid.Cells)
            {
                Vector3 center = cell.GetCenter();
                
                // セルの中心に小さなキューブを描画
                Gizmos.DrawCube(center, Vector3.one * 0.2f);
            }
        }
        
        /// <summary>
        /// 垂直スタックを描画（高さ方向の積み上げ）
        /// </summary>
        private void DrawVerticalStacks()
        {
            Gizmos.color = m_StackColor;
            
            foreach (Cell cell in m_Grid.Cells)
            {
                int height = m_ColumnStack.GetHeight(cell.Id);
                
                if (height > 0)
                {
                    Vector3 center = cell.GetCenter();
                    
                    // 各レイヤーごとにワイヤーフレームのボックスを描画
                    for (int layer = 0; layer < height; layer++)
                    {
                        Vector3 layerCenter = center + new Vector3(0f, layer + 0.5f, 0f);
                        
                        // セルの形状に合わせたボックスのサイズを計算（簡易実装: 固定サイズ）
                        Vector3 boxSize = new Vector3(0.8f, 1f, 0.8f);
                        
                        // ワイヤーフレームのボックスを描画
                        Gizmos.DrawWireCube(layerCenter, boxSize);
                    }
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// グリッドを再生成（実行時に呼び出し可能）
        /// </summary>
        public void RegenerateGrid()
        {
            InitializeGrid();
        }
        #endregion
    }
}
