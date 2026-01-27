using UnityEngine;
using System.Collections.Generic;

#if UNITY_SPLINES
using Unity.Splines;
#endif

namespace Vastcore.Terrain.MarchingSquares
{
    /// <summary>
    /// Marching Squaresアルゴリズムによるプレハブ配置ロジック
    /// グリッドデータから16種類のパターンを判定し、対応するプレハブを配置
    /// </summary>
    public class MarchingSquaresGenerator : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Grid Settings")]
        [SerializeField] private int m_GridWidth = 10;
        [SerializeField] private int m_GridHeight = 10;
        [SerializeField] private float m_CellSize = 1.0f;

        [Header("Prefab Settings")]
        [Tooltip("16種類のプレハブ配列（インデックス0～15に対応）")]
        [SerializeField] private GameObject[] m_Prefabs = new GameObject[16];

        [Header("Generation Settings")]
        [SerializeField] private bool m_AutoGenerateOnStart = false;
        [SerializeField] private bool m_ClearExistingOnGenerate = true;
        [SerializeField] private Transform m_ParentTransform = null;

        [Header("Spline Input Settings")]
#if UNITY_SPLINES
        [Tooltip("スプラインコンテナへの参照（Inspectorで設定）")]
        [SerializeField] private SplineContainer m_SplineContainer = null;
#else
        [Tooltip("Unity Spline Packageがインストールされていません")]
        [SerializeField] private MonoBehaviour m_SplineContainer = null;
#endif
        [Tooltip("ブラシ半径（ワールド座標単位）")]
        [SerializeField, Range(0.1f, 10f)] private float m_BrushRadius = 0.5f;
        [Tooltip("サンプリング間隔（ワールド座標単位）")]
        [SerializeField, Range(0.01f, 1f)] private float m_SamplingInterval = 0.1f;
        [Tooltip("スプラインラスタライズ後に自動的に地形を生成する")]
        [SerializeField] private bool m_AutoGenerateAfterRasterize = true;

        [Header("Phase 3: Extended Data Settings")]
        [Tooltip("拡張データを使用するか（Height, BiomeId, RoadId, BuildingId）")]
        [SerializeField] private bool m_UseExtendedData = false;

        [Header("Phase 3: Height Map Settings")]
        [Tooltip("高さマップ（Texture2D、グレースケール、オプション）")]
        [SerializeField] private Texture2D m_HeightMap = null;
        [Tooltip("高さのスケール（0.0～1.0の値を実際の高さに変換）")]
        [SerializeField, Range(0.1f, 10f)] private float m_HeightScale = 1.0f;

        [Header("Phase 3: Biome Transition Models")]
        [Tooltip("バイオーム遷移モデル配列（遷移タイプをインデックスとして使用）")]
        [SerializeField] private GameObject[] m_BiomeTransitionModels = new GameObject[0];

        [Header("Phase 3: Slope Models")]
        [Tooltip("スロープモデル配列（スロープタイプをインデックスとして使用）")]
        [SerializeField] private GameObject[] m_SlopeModels = new GameObject[0];
        #endregion

        #region Private Fields
        private MarchingSquaresGrid m_Grid;
        private List<GameObject> m_GeneratedObjects = new List<GameObject>();
        #endregion

        #region Public Properties
        /// <summary>
        /// グリッドデータへのアクセス
        /// </summary>
        public MarchingSquaresGrid Grid => m_Grid;

        /// <summary>
        /// 生成されたオブジェクトのリスト
        /// </summary>
        public IReadOnlyList<GameObject> GeneratedObjects => m_GeneratedObjects;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeGrid();
        }

        private void Start()
        {
            if (m_AutoGenerateOnStart)
            {
                GenerateMap();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// グリッドを初期化
        /// </summary>
        public void InitializeGrid()
        {
            if (m_GridWidth <= 0 || m_GridHeight <= 0)
            {
                Debug.LogError($"MarchingSquaresGenerator.InitializeGrid: Invalid grid size ({m_GridWidth}, {m_GridHeight}). Using default size (10, 10).");
                m_GridWidth = 10;
                m_GridHeight = 10;
            }

            if (m_CellSize <= 0f)
            {
                Debug.LogError($"MarchingSquaresGenerator.InitializeGrid: Invalid cell size ({m_CellSize}). Using default size (1.0).");
                m_CellSize = 1.0f;
            }

            m_Grid = new MarchingSquaresGrid(m_GridWidth, m_GridHeight, m_CellSize, m_UseExtendedData);

            // 高さマップが設定されている場合、処理する
            if (m_UseExtendedData && m_HeightMap != null)
            {
                HeightMapProcessor.ProcessHeightMap(m_HeightMap, m_Grid, m_HeightScale);
            }
        }

        /// <summary>
        /// マップを生成（全セルを走査してプレハブを配置）
        /// </summary>
        public void GenerateMap()
        {
            if (m_Grid == null)
            {
                Debug.LogError("MarchingSquaresGenerator.GenerateMap: Grid is not initialized. Call InitializeGrid() first.");
                return;
            }

            // 既存のプレハブを破棄
            if (m_ClearExistingOnGenerate)
            {
                ClearGeneratedObjects();
            }

            // 全セルを走査
            for (int x = 0; x < m_Grid.Width - 1; x++)
            {
                for (int y = 0; y < m_Grid.Height - 1; y++)
                {
                    // セルが有効かチェック
                    if (!MarchingSquaresCalculator.IsValidCell(m_Grid, x, y))
                    {
                        continue;
                    }

                    // ビットマスク計算
                    int index = MarchingSquaresCalculator.CalculateIndex(m_Grid, x, y);

                    // インデックスに対応するプレハブを配置
                    if (index >= 0 && index < m_Prefabs.Length && m_Prefabs[index] != null)
                    {
                        // セル位置の計算（セルの中心座標）
                        Vector3 cellPosition = new Vector3(
                            (x + 0.5f) * m_CellSize,
                            0f,
                            (y + 0.5f) * m_CellSize
                        );

                        // プレハブを生成
                        GameObject instance = Instantiate(m_Prefabs[index], cellPosition, Quaternion.identity);

                        // 親オブジェクトを設定
                        if (m_ParentTransform != null)
                        {
                            instance.transform.SetParent(m_ParentTransform);
                        }
                        else
                        {
                            instance.transform.SetParent(transform);
                        }

                        // 生成されたオブジェクトをリストに追加
                        m_GeneratedObjects.Add(instance);
                    }
                }
            }

            Debug.Log($"MarchingSquaresGenerator.GenerateMap: Generated {m_GeneratedObjects.Count} objects.");
        }

        /// <summary>
        /// 生成されたオブジェクトを全て破棄
        /// </summary>
        public void ClearGeneratedObjects()
        {
            foreach (GameObject obj in m_GeneratedObjects)
            {
                if (obj != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(obj);
                    }
                    else
                    {
                        DestroyImmediate(obj);
                    }
            }
            }

            m_GeneratedObjects.Clear();
        }

        /// <summary>
        /// グリッドサイズを変更して再初期化
        /// </summary>
        /// <param name="_width">新しい幅</param>
        /// <param name="_height">新しい高さ</param>
        /// <param name="_cellSize">新しいセルサイズ</param>
        public void ResizeGrid(int _width, int _height, float _cellSize = 1.0f)
        {
            m_GridWidth = _width;
            m_GridHeight = _height;
            m_CellSize = _cellSize;

            if (m_Grid != null)
            {
                m_Grid.Resize(_width, _height, _cellSize);
            }
            else
            {
                InitializeGrid();
            }
        }

        /// <summary>
        /// スプラインからグリッドデータをラスタライズ
        /// SplineContainerからスプラインを取得し、グリッドデータに焼き込む
        /// </summary>
        /// <param name="_value">設定する値（true=埋める、false=削除）</param>
        /// <returns>ラスタライズされた頂点の数。エラー時は-1</returns>
        public int RasterizeFromSpline(bool _value = true)
        {
#if !UNITY_SPLINES
            Debug.LogError("MarchingSquaresGenerator.RasterizeFromSpline: Unity Spline Package is not installed. Please install 'com.unity.splines' from Package Manager.");
            return -1;
#else
            if (m_Grid == null)
            {
                Debug.LogError("MarchingSquaresGenerator.RasterizeFromSpline: Grid is not initialized. Call InitializeGrid() first.");
                return -1;
            }

            if (m_SplineContainer == null)
            {
                Debug.LogError("MarchingSquaresGenerator.RasterizeFromSpline: SplineContainer is not assigned. Please assign a SplineContainer in the Inspector.");
                return -1;
            }

            // SplineContainerからスプラインを取得
            var splines = m_SplineContainer.Splines;
            if (splines == null || splines.Count == 0)
            {
                Debug.LogWarning("MarchingSquaresGenerator.RasterizeFromSpline: SplineContainer has no splines or the spline is empty.");
                return 0;
            }

            int totalRasterized = 0;

            // SplineContainer内の全スプラインを処理
            for (int i = 0; i < splines.Count; i++)
            {
                var spline = splines[i];
                if (spline == null || spline.Count == 0)
                {
                    continue;
                }

                int rasterized = SplineRasterizer.RasterizeSpline(
                    spline,
                    m_BrushRadius,
                    m_Grid,
                    _value,
                    m_SamplingInterval
                );

                totalRasterized += rasterized;
            }

            Debug.Log($"MarchingSquaresGenerator.RasterizeFromSpline: Rasterized {totalRasterized} vertices from {splines.Count} spline(s).");

            // 自動生成オプションが有効な場合、地形を生成
            if (m_AutoGenerateAfterRasterize && totalRasterized > 0)
            {
                GenerateMap();
            }

            return totalRasterized;
#endif
        }

        /// <summary>
        /// レイヤー構造を考慮した地形生成
        /// レイヤー優先順位: 地形ベース → 道路レイヤー → 建物レイヤー
        /// </summary>
        public void GenerateMapWithLayers()
        {
            if (m_Grid == null)
            {
                Debug.LogError("MarchingSquaresGenerator.GenerateMapWithLayers: Grid is not initialized. Call InitializeGrid() first.");
                return;
            }

            // 既存のプレハブを破棄
            if (m_ClearExistingOnGenerate)
            {
                ClearGeneratedObjects();
            }

            // 高さマップが設定されている場合、処理する
            if (m_UseExtendedData && m_HeightMap != null)
            {
                HeightMapProcessor.ProcessHeightMap(m_HeightMap, m_Grid, m_HeightScale);
            }

            // 全セルを走査
            for (int x = 0; x < m_Grid.Width - 1; x++)
            {
                for (int y = 0; y < m_Grid.Height - 1; y++)
                {
                    // セルが有効かチェック
                    if (!MarchingSquaresCalculator.IsValidCell(m_Grid, x, y))
                    {
                        continue;
                    }

                    GameObject prefabToInstantiate = null;
                    Vector3 cellPosition = new Vector3(
                        (x + 0.5f) * m_CellSize,
                        0f,
                        (y + 0.5f) * m_CellSize
                    );

                    // レイヤー優先順位: 建物レイヤー → 道路レイヤー → 地形ベース
                    if (m_UseExtendedData)
                    {
                        // セルの4頂点のGridPointを取得
                        GridPoint pointTL = m_Grid.GetGridPoint(x, y + 1);      // Top-Left
                        GridPoint pointTR = m_Grid.GetGridPoint(x + 1, y + 1);  // Top-Right
                        GridPoint pointBR = m_Grid.GetGridPoint(x + 1, y);      // Bottom-Right
                        GridPoint pointBL = m_Grid.GetGridPoint(x, y);          // Bottom-Left

                        // 1. 建物レイヤー（最優先）
                        if (pointBL.BuildingId > 0 || pointBR.BuildingId > 0 || pointTL.BuildingId > 0 || pointTR.BuildingId > 0)
                        {
                            // 建物がある場合は、建物用のプレハブを選択（将来実装）
                            // 現時点では地形ベースにフォールバック
                        }

                        // 2. 道路レイヤー
                        if (pointBL.RoadId > 0 || pointBR.RoadId > 0 || pointTL.RoadId > 0 || pointTR.RoadId > 0)
                        {
                            // 道路がある場合は、道路用のプレハブを選択（将来実装）
                            // 現時点では地形ベースにフォールバック
                        }

                        // 3. 地形ベース（バイオーム遷移、スロープ、基本パターン）
                        // バイオーム遷移をチェック
                        int transitionType = BiomeTransitionCalculator.CalculateTransition(x, y, m_Grid);
                        if (transitionType != BiomeTransitionCalculator.TransitionType_None)
                        {
                            prefabToInstantiate = BiomeTransitionCalculator.SelectBoundaryModel(transitionType, m_BiomeTransitionModels);
                        }

                        // バイオーム遷移モデルがない場合、スロープモデルをチェック
                        if (prefabToInstantiate == null && m_SlopeModels != null && m_SlopeModels.Length > 0)
                        {
                            int slopeType = HeightMapProcessor.SelectSlopeModel(x, y, m_Grid);
                            prefabToInstantiate = HeightMapProcessor.SelectSlopeModelPrefab(slopeType, m_SlopeModels);
                        }

                        // 高さをセル位置に反映
                        float averageHeight = (pointTL.Height + pointTR.Height + pointBR.Height + pointBL.Height) / 4.0f;
                        cellPosition.y = averageHeight;
                    }

                    // バイオーム遷移モデル、スロープモデルがない場合、基本パターンを使用
                    if (prefabToInstantiate == null)
                    {
                        // ビットマスク計算
                        int index = MarchingSquaresCalculator.CalculateIndex(m_Grid, x, y);

                        // インデックスに対応するプレハブを配置
                        if (index >= 0 && index < m_Prefabs.Length && m_Prefabs[index] != null)
                        {
                            prefabToInstantiate = m_Prefabs[index];
                        }
                    }

                    // プレハブを生成
                    if (prefabToInstantiate != null)
                    {
                        GameObject instance = Instantiate(prefabToInstantiate, cellPosition, Quaternion.identity);

                        // 親オブジェクトを設定
                        if (m_ParentTransform != null)
                        {
                            instance.transform.SetParent(m_ParentTransform);
                        }
                        else
                        {
                            instance.transform.SetParent(transform);
                        }

                        // 生成されたオブジェクトをリストに追加
                        m_GeneratedObjects.Add(instance);
                    }
                }
            }

            Debug.Log($"MarchingSquaresGenerator.GenerateMapWithLayers: Generated {m_GeneratedObjects.Count} objects.");
        }
        #endregion

        #region Editor Methods
#if UNITY_EDITOR
        /// <summary>
        /// エディタ用: マップを生成（Context Menu）
        /// </summary>
        [ContextMenu("Generate Map")]
        private void GenerateMapEditor()
        {
            InitializeGrid();
            GenerateMap();
        }

        /// <summary>
        /// エディタ用: レイヤー構造を考慮したマップを生成（Context Menu）
        /// </summary>
        [ContextMenu("Generate Map With Layers")]
        private void GenerateMapWithLayersEditor()
        {
            InitializeGrid();
            GenerateMapWithLayers();
        }

        /// <summary>
        /// エディタ用: 生成されたオブジェクトをクリア（Context Menu）
        /// </summary>
        [ContextMenu("Clear Generated Objects")]
        private void ClearGeneratedObjectsEditor()
        {
            ClearGeneratedObjects();
        }

        /// <summary>
        /// エディタ用: スプラインからラスタライズ（Context Menu）
        /// </summary>
        [ContextMenu("Rasterize From Spline")]
        private void RasterizeFromSplineEditor()
        {
            InitializeGrid();
            RasterizeFromSpline(true);
        }

        /// <summary>
        /// エディタ用: スプラインから削除（Context Menu）
        /// </summary>
        [ContextMenu("Erase From Spline")]
        private void EraseFromSplineEditor()
        {
            if (m_Grid == null)
            {
                InitializeGrid();
            }
            RasterizeFromSpline(false);
        }
#endif
        #endregion
    }
}
