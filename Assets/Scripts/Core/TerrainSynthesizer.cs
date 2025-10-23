using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Core
{
    /// <summary>
    /// 高度合成システム - 複数地形タイプの合成を管理
    /// 異なる地形タイプを自然にブレンドして複雑な地形を生成
    /// </summary>
    public class TerrainSynthesizer : MonoBehaviour
    {
        [Header("合成設定")]
        [SerializeField] private TerrainType dominantType = TerrainType.Plain;
        [SerializeField] private List<TerrainTypeDefinition> availableTypes;
        [SerializeField] private AnimationCurve blendCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("生成パラメータ")]
        [SerializeField] private int resolution = 256;
        [SerializeField] private float terrainSize = 1000f;
        [SerializeField] private int seed = 42;
        [SerializeField] private int regionCount = 8;

        // 内部データ
        private Dictionary<Vector2Int, TerrainType> terrainMap;
        private Dictionary<TerrainType, TerrainTypeDefinition> typeDefinitions;
        private Terrain terrain;
        private TerrainData terrainData;

        private void Awake()
        {
            InitializeSynthesizer();
        }

        /// <summary>
        /// 合成システムの初期化
        /// </summary>
        private void InitializeSynthesizer()
        {
            // Terrainコンポーネントの取得
            terrain = GetComponent<Terrain>();
            if (terrain == null)
            {
                terrain = gameObject.AddComponent<Terrain>();
            }

            terrainData = terrain.terrainData;
            if (terrainData == null)
            {
                terrainData = new TerrainData();
                terrain.terrainData = terrainData;
            }

            // タイプ定義の初期化
            InitializeTypeDefinitions();

            // 地形データの設定
            terrainData.heightmapResolution = resolution;
            terrainData.size = new Vector3(terrainSize, 500f, terrainSize);

            VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", "高度合成システムが初期化されました");
        }

        /// <summary>
        /// 地形タイプ定義の初期化
        /// </summary>
        private void InitializeTypeDefinitions()
        {
            typeDefinitions = new Dictionary<TerrainType, TerrainTypeDefinition>();

            // 使用可能なタイプを辞書に登録
            foreach (var typeDef in availableTypes)
            {
                typeDefinitions[typeDef.type] = typeDef;
            }

            // デフォルトタイプの追加（不足している場合）
            foreach (TerrainType terrainType in System.Enum.GetValues(typeof(TerrainType)))
            {
                if (!typeDefinitions.ContainsKey(terrainType))
                {
                    typeDefinitions[terrainType] = TerrainTypeDefinition.CreateDefault(terrainType);
                    VastcoreLogger.Instance.LogWarning("TerrainSynthesizer",
                        $"{terrainType} の定義が見つからないため、デフォルト設定を使用します");
                }
            }
        }

        /// <summary>
        /// 合成地形の生成を開始
        /// </summary>
        [ContextMenu("Generate Synthesized Terrain")]
        public void GenerateSynthesizedTerrain()
        {
            try
            {
                VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", "高度合成地形生成を開始します");

                // 1. 地形タイプ分布の生成
                GenerateTerrainTypeDistribution();

                // 2. 各タイプの地形データ生成
                GenerateIndividualTerrainData();

                // 3. 合成とブレンド処理
                SynthesizeTerrainData();

                // 4. 最終地形の適用
                ApplySynthesizedTerrain();

                VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", "高度合成地形生成が完了しました");
            }
            catch (System.Exception ex)
            {
                VastcoreLogger.Instance.LogError("TerrainSynthesizer", $"地形生成中にエラーが発生: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 地形タイプ分布の生成
        /// </summary>
        private void GenerateTerrainTypeDistribution()
        {
            terrainMap = new Dictionary<Vector2Int, TerrainType>();

            // ランダムシードの設定
            Random.InitState(seed);

            // リージョン中心点の生成
            List<Vector2> regionCenters = new List<Vector2>();
            for (int i = 0; i < regionCount; i++)
            {
                float x = Random.Range(0f, terrainSize);
                float z = Random.Range(0f, terrainSize);
                regionCenters.Add(new Vector2(x, z));
            }

            // 各ポイントに最も近いリージョンのタイプを割り当て
            int gridSize = resolution / 16; // 16x16のグリッドで簡略化
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    Vector2 worldPos = new Vector2(
                        (float)x / gridSize * terrainSize,
                        (float)z / gridSize * terrainSize
                    );

                    // 最も近いリージョンを見つける
                    TerrainType closestType = dominantType;
                    float minDistance = float.MaxValue;

                    for (int i = 0; i < regionCenters.Count; i++)
                    {
                        float distance = Vector2.Distance(worldPos, regionCenters[i]);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            // タイプを順番に割り当て（循環）
                            closestType = (TerrainType)(i % System.Enum.GetValues(typeof(TerrainType)).Length);
                        }
                    }

                    terrainMap[new Vector2Int(x, z)] = closestType;
                }
            }

            VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", $"{terrainMap.Count} 個のグリッドポイントに地形タイプを割り当てました");
        }

        /// <summary>
        /// 各タイプの地形データ生成
        /// </summary>
        private void GenerateIndividualTerrainData()
        {
            // このメソッドは次のフェーズで実装
            VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", "個別地形データ生成は次のフェーズで実装されます");
        }

        /// <summary>
        /// 合成とブレンド処理
        /// </summary>
        private void SynthesizeTerrainData()
        {
            // このメソッドは次のフェーズで実装
            VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", "合成・ブレンド処理は次のフェーズで実装されます");
        }

        /// <summary>
        /// 最終地形の適用
        /// </summary>
        private void ApplySynthesizedTerrain()
        {
            // このメソッドは次のフェーズで実装
            VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", "最終地形適用は次のフェーズで実装されます");
        }

        /// <summary>
        /// デバッグ用の地形タイプ分布を表示
        /// </summary>
        [ContextMenu("Debug Terrain Type Distribution")]
        public void DebugTerrainTypeDistribution()
        {
            if (terrainMap == null || terrainMap.Count == 0)
            {
                VastcoreLogger.Instance.LogWarning("TerrainSynthesizer", "地形タイプ分布が生成されていません");
                return;
            }

            var typeCounts = new Dictionary<TerrainType, int>();
            foreach (var kvp in terrainMap)
            {
                if (!typeCounts.ContainsKey(kvp.Value))
                    typeCounts[kvp.Value] = 0;
                typeCounts[kvp.Value]++;
            }

            VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", "地形タイプ分布:");
            foreach (var kvp in typeCounts)
            {
                float percentage = (float)kvp.Value / terrainMap.Count * 100f;
                VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", $"{kvp.Key}: {kvp.Value} ポイント ({percentage:F1}%)");
            }
        }
    }
}
