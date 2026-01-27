using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Vastcore.Terrain.MarchingSquares
{
    /// <summary>
    /// Marching Squaresグリッドのデバッグ可視化クラス（MonoBehaviour）
    /// OnDrawGizmosを使用してグリッド、頂点、セルを可視化
    /// </summary>
    public class MarchingSquaresDebugVisualizer : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Grid Settings")]
        [SerializeField] private int m_GridWidth = 10;
        [SerializeField] private int m_GridHeight = 10;
        [SerializeField] private float m_CellSize = 1.0f;

        [Header("Visualization Settings")]
        [SerializeField] private bool m_ShowGrid = true;
        [SerializeField] private bool m_ShowVertices = true;
        [SerializeField] private bool m_ShowCells = true;
        [SerializeField] private bool m_ShowCellIndices = false;

        [Header("Colors")]
        [SerializeField] private Color m_GridColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        [SerializeField] private Color m_VertexFilledColor = Color.green;
        [SerializeField] private Color m_VertexEmptyColor = Color.red;
        [SerializeField] private Color m_CellColor = new Color(1f, 1f, 0f, 0.3f);

        [Header("Sizes")]
        [SerializeField] private float m_VertexSize = 0.1f;
        [SerializeField] private float m_GridLineWidth = 0.02f;

        [Header("Grid Data Reference")]
        [Tooltip("MarchingSquaresGeneratorコンポーネントを参照（自動検出も可能）")]
        [SerializeField] private MarchingSquaresGenerator m_Generator = null;
        #endregion

        #region Private Fields
        private MarchingSquaresGrid m_Grid;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Generatorを自動検出
            if (m_Generator == null)
            {
                m_Generator = GetComponent<MarchingSquaresGenerator>();
            }

            // Generatorからグリッドを取得
            if (m_Generator != null)
            {
                m_Grid = m_Generator.Grid;
            }
        }

        private void OnValidate()
        {
            // Inspectorで値が変更された場合、グリッドを再取得
            if (m_Generator != null)
            {
                m_Grid = m_Generator.Grid;
            }
        }
        #endregion

        #region Gizmos Drawing
        private void OnDrawGizmos()
        {
            // グリッドデータを取得
            if (m_Grid == null)
            {
                if (m_Generator != null)
                {
                    m_Grid = m_Generator.Grid;
                }
                else
                {
                    // Generatorが見つからない場合は、一時的なグリッドを作成
                    m_Grid = new MarchingSquaresGrid(m_GridWidth, m_GridHeight, m_CellSize);
                }
            }

            if (m_Grid == null)
            {
                return;
            }

            // グリッド線を描画
            if (m_ShowGrid)
            {
                DrawGrid();
            }

            // 頂点を描画
            if (m_ShowVertices)
            {
                DrawVertices();
            }

            // セルを描画
            if (m_ShowCells)
            {
                DrawCells();
            }
        }

        /// <summary>
        /// グリッド線を描画
        /// </summary>
        private void DrawGrid()
        {
            Gizmos.color = m_GridColor;

            // 縦線を描画
            for (int x = 0; x <= m_Grid.Width; x++)
            {
                Vector3 start = new Vector3(x * m_Grid.CellSize, 0f, 0f);
                Vector3 end = new Vector3(x * m_Grid.CellSize, 0f, m_Grid.Height * m_Grid.CellSize);
                Gizmos.DrawLine(start, end);
            }

            // 横線を描画
            for (int y = 0; y <= m_Grid.Height; y++)
            {
                Vector3 start = new Vector3(0f, 0f, y * m_Grid.CellSize);
                Vector3 end = new Vector3(m_Grid.Width * m_Grid.CellSize, 0f, y * m_Grid.CellSize);
                Gizmos.DrawLine(start, end);
            }
        }

        /// <summary>
        /// 頂点を描画（状態に応じて色分け）
        /// </summary>
        private void DrawVertices()
        {
            for (int x = 0; x < m_Grid.Width; x++)
            {
                for (int y = 0; y < m_Grid.Height; y++)
                {
                    bool isFilled = m_Grid.GetVertex(x, y);
                    Gizmos.color = isFilled ? m_VertexFilledColor : m_VertexEmptyColor;

                    Vector3 position = m_Grid.GridToWorld(x, y);
                    Gizmos.DrawSphere(position, m_VertexSize);
                }
            }
        }

        /// <summary>
        /// セルを描画（セルの中心にキューブを描画）
        /// </summary>
        private void DrawCells()
        {
            Gizmos.color = m_CellColor;

            for (int x = 0; x < m_Grid.Width - 1; x++)
            {
                for (int y = 0; y < m_Grid.Height - 1; y++)
                {
                    if (!MarchingSquaresCalculator.IsValidCell(m_Grid, x, y))
                    {
                        continue;
                    }

                    // セルの中心座標
                    Vector3 cellCenter = new Vector3(
                        (x + 0.5f) * m_Grid.CellSize,
                        0f,
                        (y + 0.5f) * m_Grid.CellSize
                    );

                    // セルのサイズ
                    Vector3 cellSize = new Vector3(m_Grid.CellSize * 0.8f, 0.1f, m_Grid.CellSize * 0.8f);

                    // ワイヤーフレームのキューブを描画
                    Gizmos.DrawWireCube(cellCenter, cellSize);

                    // セルインデックスを表示（オプション）
                    if (m_ShowCellIndices)
                    {
                        int index = MarchingSquaresCalculator.CalculateIndex(m_Grid, x, y);
                        DrawCellIndex(cellCenter, index);
                    }
                }
            }
        }

        /// <summary>
        /// セルインデックスを描画（デバッグ用）
        /// </summary>
        private void DrawCellIndex(Vector3 _position, int _index)
        {
#if UNITY_EDITOR
            // Gizmosではテキストを直接描画できないため、Handlesを使用
            Handles.Label(
                _position + Vector3.up * 0.5f,
                _index.ToString(),
                new GUIStyle { normal = { textColor = Color.white }, fontSize = 12 }
            );
#endif
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// グリッドデータを手動で設定（デバッグ用）
        /// </summary>
        /// <param name="_grid">グリッドデータ</param>
        public void SetGrid(MarchingSquaresGrid _grid)
        {
            m_Grid = _grid;
        }

        /// <summary>
        /// Generatorコンポーネントを設定
        /// </summary>
        /// <param name="_generator">Generatorコンポーネント</param>
        public void SetGenerator(MarchingSquaresGenerator _generator)
        {
            m_Generator = _generator;
            if (_generator != null)
            {
                m_Grid = _generator.Grid;
            }
        }
        #endregion
    }
}
