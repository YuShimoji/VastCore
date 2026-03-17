using System.Collections.Generic;
using UnityEngine;
using Vastcore.Terrain.Config;
using Vastcore.Terrain.DualGrid;
using Vastcore.Terrain.Providers;

namespace Vastcore.Terrain
{
    /// <summary>
    /// Classic Terrain + DualGrid + Prefab スタンプを統合して生成するブートストラップ。
    /// Play 時に地形チャンクを生成し、その上に DualGrid スタンプを地形追従配置する。
    /// </summary>
    public sealed class TerrainWithStampsBootstrap : MonoBehaviour
    {
        [Header("Terrain Generation")]
        [Tooltip("地形生成設定 (ErosionSettings 込み)")]
        public TerrainGenerationConfig config;
        [Min(1)] public int gridX = 2;
        [Min(1)] public int gridZ = 2;
        public Vector2 worldOrigin = Vector2.zero;

        [Header("DualGrid")]
        [Tooltip("DualGrid の半径")]
        [Min(1)] public int dualGridRadius = 3;
        [Tooltip("DualGrid のシード")]
        public int dualGridSeed = 42;
        [Tooltip("ジッター量 (Relaxation)")]
        [Range(0f, 1f)] public float jitterAmount = 0.3f;

        [Header("Stamps")]
        [Tooltip("配置するスタンプ定義")]
        public PrefabStampDefinition stampDefinition;
        [Tooltip("配置するセルID群 (空の場合は自動配置)")]
        public int[] stampCellIds = new int[0];
        [Tooltip("自動配置時の配置確率 (0-1)")]
        [Range(0f, 1f)] public float autoPlaceProbability = 0.2f;
        [Tooltip("スタンプのシード")]
        public int stampSeed = 42;

        [Header("Options")]
        public bool autoBuildOnStart = true;

        private IrregularGrid m_Grid;
        private ColumnStack m_ColumnStack;
        private StampRegistry m_StampRegistry;
        private PrefabStampPlacer m_Placer;
        private List<TerrainChunk> m_Chunks = new List<TerrainChunk>();

        private void Start()
        {
            if (autoBuildOnStart)
            {
                Build();
            }
        }

        /// <summary>
        /// 地形 + DualGrid + スタンプの統合生成を実行。
        /// </summary>
        public void Build()
        {
            // 1. Classic Terrain チャンクを生成
            BuildTerrainChunks();

            // 2. DualGrid を生成
            BuildDualGrid();

            // 3. スタンプを配置 (地形高さ追従)
            PlaceStamps();

            Debug.Log($"[TerrainWithStamps] Build complete: " +
                      $"{m_Chunks.Count} chunks, " +
                      $"{m_Grid.Cells.Count} cells, " +
                      $"{m_StampRegistry.Count} stamps");
        }

        private void BuildTerrainChunks()
        {
            if (config == null)
            {
                Debug.LogError("[TerrainWithStamps] config is null");
                return;
            }

            var provider = config.CreateHeightProvider();
            if (provider == null)
            {
                Debug.LogError("[TerrainWithStamps] provider create failed");
                return;
            }

            float size = Mathf.Max(1f, config.worldSize);
            var terrainParent = new GameObject("Terrain_Chunks");
            terrainParent.transform.SetParent(transform);

            for (int z = 0; z < gridZ; z++)
            {
                for (int x = 0; x < gridX; x++)
                {
                    Vector2 origin = new Vector2(worldOrigin.x + x * size, worldOrigin.y + z * size);
                    var chunk = TerrainChunk.CreateAndBuild(config, provider, origin);
                    chunk.transform.SetParent(terrainParent.transform, worldPositionStays: true);
                    m_Chunks.Add(chunk);
                }
            }
        }

        private void BuildDualGrid()
        {
            m_Grid = new IrregularGrid();
            m_ColumnStack = new ColumnStack();
            m_StampRegistry = new StampRegistry();

            m_Grid.GenerateGrid(dualGridRadius);
            m_Grid.ApplyRelaxation(dualGridSeed, jitterAmount, true);

            // DualGrid の位置を地形中心に合わせる
            float terrainCenterX = worldOrigin.x + (gridX * config.worldSize) * 0.5f;
            float terrainCenterZ = worldOrigin.y + (gridZ * config.worldSize) * 0.5f;

            // DualGrid はワールド原点中心で生成されるため、親 Transform で位置調整
            var gridParent = new GameObject("DualGrid_Stamps");
            gridParent.transform.SetParent(transform);
            gridParent.transform.position = new Vector3(terrainCenterX, 0f, terrainCenterZ);

            m_Placer = new PrefabStampPlacer(gridParent.transform);

            // 地形高さサンプラーを接続
            if (m_Chunks.Count > 0 && m_Chunks[0].UnityTerrain != null)
            {
                // 最初のチャンクの Terrain を使用（マルチチャンクの高さ統合は将来課題）
                m_Placer.SetHeightSampler(new UnityTerrainHeightSampler(m_Chunks[0].UnityTerrain));
            }
        }

        private void PlaceStamps()
        {
            if (stampDefinition == null || !stampDefinition.IsValid())
            {
                Debug.Log("[TerrainWithStamps] No valid stamp definition, skipping stamp placement");
                return;
            }

            System.Random rng = new System.Random(stampSeed);

            if (stampCellIds != null && stampCellIds.Length > 0)
            {
                // 明示的なセルID指定
                PlaceStampsAtCellIds(rng);
            }
            else
            {
                // 自動配置
                PlaceStampsAuto(rng);
            }

            // GameObject インスタンス化
            m_Placer.InstantiateAll(m_StampRegistry, m_Grid);
        }

        private void PlaceStampsAtCellIds(System.Random _rng)
        {
            foreach (int cellId in stampCellIds)
            {
                Cell cell = null;
                foreach (Cell c in m_Grid.Cells)
                {
                    if (c.Id == cellId) { cell = c; break; }
                }
                if (cell == null) continue;

                float rotation = stampDefinition.GetRandomRotation(_rng);
                float scale = stampDefinition.GetRandomScale(_rng);

                if (stampDefinition.IsSingleCell)
                    m_StampRegistry.Place(stampDefinition, cell, m_ColumnStack, rotation, scale);
                else
                    m_StampRegistry.Place(stampDefinition, cell, m_ColumnStack, rotation, scale, m_Grid);
            }
        }

        private void PlaceStampsAuto(System.Random _rng)
        {
            foreach (Cell cell in m_Grid.Cells)
            {
                if (_rng.NextDouble() > autoPlaceProbability) continue;
                if (m_StampRegistry.IsOccupied(cell.Id)) continue;

                float rotation = stampDefinition.GetRandomRotation(_rng);
                float scale = stampDefinition.GetRandomScale(_rng);

                if (stampDefinition.IsSingleCell)
                    m_StampRegistry.Place(stampDefinition, cell, m_ColumnStack, rotation, scale);
                else
                    m_StampRegistry.Place(stampDefinition, cell, m_ColumnStack, rotation, scale, m_Grid);
            }
        }
    }
}
