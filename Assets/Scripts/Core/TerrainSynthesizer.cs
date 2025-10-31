using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Vastcore.Utils;

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
        private Dictionary<TerrainType, float[,]> terrainDataMap;
        private float[,] finalHeights;
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
            var terrainDataMap = new Dictionary<TerrainType, float[,]>();

            foreach (TerrainType terrainType in System.Enum.GetValues(typeof(TerrainType)))
            {
                if (!typeDefinitions.ContainsKey(terrainType)) continue;

                var typeDef = typeDefinitions[terrainType];
                float[,] heights = new float[resolution, resolution];

                // 各タイプ固有の地形生成ロジック
                GenerateTerrainHeights(heights, terrainType, typeDef);

                terrainDataMap[terrainType] = heights;
            }

            // 内部データとして保持
            this.terrainDataMap = terrainDataMap;

            VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", $"{terrainDataMap.Count} 種類の地形データを生成しました");
        }

        /// <summary>
        /// 指定された地形タイプの高さを生成
        /// </summary>
        private void GenerateTerrainHeights(float[,] heights, TerrainType terrainType, TerrainTypeDefinition typeDef)
        {
            for (int x = 0; x < resolution; x++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    float worldX = (float)x / resolution * terrainSize;
                    float worldZ = (float)z / resolution * terrainSize;

                    // 基本の高さ生成（パーリンノイズベース）
                    float baseHeight = GenerateBaseHeight(worldX, worldZ, typeDef);

                    // タイプ固有の特徴を追加
                    float typeSpecificHeight = GenerateTypeSpecificHeight(worldX, worldZ, terrainType, typeDef);

                    // 最終的な高さを正規化
                    heights[z, x] = Mathf.Clamp01((baseHeight + typeSpecificHeight) / 500f); // 500は最大高さ
                }
            }
        }

        /// <summary>
        /// 基本の高さを生成
        /// </summary>
        private float GenerateBaseHeight(float worldX, float worldZ, TerrainTypeDefinition typeDef)
        {
            // パーリンノイズで基本的な起伏を生成
            float noise1 = Mathf.PerlinNoise(worldX * 0.001f, worldZ * 0.001f) * 100f;
            float noise2 = Mathf.PerlinNoise(worldX * 0.01f, worldZ * 0.01f) * 50f;
            float noise3 = Mathf.PerlinNoise(worldX * 0.1f, worldZ * 0.1f) * 25f;

            return noise1 + noise2 + noise3;
        }

        /// <summary>
        /// 地形タイプ固有の高さを生成
        /// </summary>
        private float GenerateTypeSpecificHeight(float worldX, float worldZ, TerrainType terrainType, TerrainTypeDefinition typeDef)
        {
            float height = 0f;

            switch (terrainType)
            {
                case TerrainType.Mountain:
                    // 山岳地帯：高い標高と急峻な斜面
                    height = Mathf.PerlinNoise(worldX * 0.005f, worldZ * 0.005f) * 200f;
                    break;

                case TerrainType.Hill:
                    // 丘陵地帯：中程度の起伏
                    height = Mathf.PerlinNoise(worldX * 0.01f, worldZ * 0.01f) * 100f;
                    break;

                case TerrainType.Plain:
                    // 平野：比較的平坦
                    height = Mathf.PerlinNoise(worldX * 0.02f, worldZ * 0.02f) * 20f;
                    break;

                case TerrainType.Valley:
                    // 谷：低い標高
                    height = -Mathf.PerlinNoise(worldX * 0.008f, worldZ * 0.008f) * 50f;
                    break;

                case TerrainType.Plateau:
                    // 高地：高いが平坦な地域
                    height = 150f + Mathf.PerlinNoise(worldX * 0.05f, worldZ * 0.05f) * 30f;
                    break;

                default:
                    height = 0f;
                    break;
            }

            return height;
        }

        /// <summary>
        /// 合成とブレンド処理
        /// </summary>
        private void SynthesizeTerrainData()
        {
            float[,] finalHeights = new float[resolution, resolution];
            int gridSize = resolution / 16; // terrainMapのグリッドサイズ

            for (int x = 0; x < resolution; x++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    // グリッド座標に変換
                    int gridX = Mathf.Clamp(x / 16, 0, gridSize - 1);
                    int gridZ = Mathf.Clamp(z / 16, 0, gridSize - 1);
                    Vector2Int gridPos = new Vector2Int(gridX, gridZ);

                    // この位置の地形タイプを取得
                    TerrainType terrainType = terrainMap.ContainsKey(gridPos) ? terrainMap[gridPos] : dominantType;

                    // 地形タイプの高さを取得
                    float typeHeight = 0f;
                    if (terrainDataMap.ContainsKey(terrainType))
                    {
                        typeHeight = terrainDataMap[terrainType][z, x];
                    }

                    // ブレンド処理（周辺のタイプとの自然な遷移）
                    float blendedHeight = BlendTerrainHeight(x, z, terrainType, typeHeight, gridSize);

                    finalHeights[z, x] = blendedHeight;
                }
            }

            this.finalHeights = finalHeights;

            VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", "地形データの合成とブレンドが完了しました");
        }

        /// <summary>
        /// 地形の高さをブレンド
        /// </summary>
        private float BlendTerrainHeight(int x, int z, TerrainType centerType, float centerHeight, int gridSize)
        {
            float totalWeight = 0f;
            float blendedHeight = 0f;

            // 3x3のグリッド範囲でブレンド
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
                {
                    int gridX = (x / 16) + offsetX;
                    int gridZ = (z / 16) + offsetZ;

                    // 境界チェック
                    if (gridX < 0 || gridX >= gridSize || gridZ < 0 || gridZ >= gridSize)
                        continue;

                    Vector2Int gridPos = new Vector2Int(gridX, gridZ);
                    TerrainType neighborType = terrainMap.ContainsKey(gridPos) ? terrainMap[gridPos] : dominantType;

                    // 距離に基づく重み計算
                    float distance = Mathf.Sqrt(offsetX * offsetX + offsetZ * offsetZ);
                    float weight = distance == 0f ? 1f : blendCurve.Evaluate(1f / (distance + 1f));

                    // 同じタイプの場合はより強い重み
                    if (neighborType == centerType)
                    {
                        weight *= 2f;
                    }

                    // 地形データから高さを取得
                    float neighborHeight = 0f;
                    if (terrainDataMap.ContainsKey(neighborType))
                    {
                        neighborHeight = terrainDataMap[neighborType][z, x];
                    }

                    blendedHeight += neighborHeight * weight;
                    totalWeight += weight;
                }
            }

            return totalWeight > 0f ? blendedHeight / totalWeight : centerHeight;
        }

        /// <summary>
        /// 最終地形の適用
        /// </summary>
        private void ApplySynthesizedTerrain()
        {
            if (finalHeights == null)
            {
                VastcoreLogger.Instance.LogError("TerrainSynthesizer", "最終地形データが生成されていません");
                return;
            }

            try
            {
                // TerrainDataの高さを設定
                terrainData.SetHeights(0, 0, finalHeights);

                // テクスチャと詳細設定の適用
                ApplyTerrainTextures();
                ApplyTerrainDetails();

                // Terrainコンポーネントの更新
                terrain.Flush();

                VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", $"最終地形が適用されました (サイズ: {terrainSize}x{terrainSize}, 解像度: {resolution})");
            }
            catch (System.Exception ex)
            {
                VastcoreLogger.Instance.LogError("TerrainSynthesizer", $"地形適用中にエラーが発生: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 地形テクスチャを適用
        /// </summary>
        private void ApplyTerrainTextures()
        {
            if (availableTypes.Count == 0) return;

            // テクスチャの設定（簡易実装）
            var terrainLayers = new TerrainLayer[availableTypes.Count];

            for (int i = 0; i < availableTypes.Count; i++)
            {
                var typeDef = availableTypes[i];
                if (typeDef.terrainTexture != null)
                {
                    var layer = new TerrainLayer();
                    layer.diffuseTexture = typeDef.terrainTexture;
                    layer.tileSize = new Vector2(terrainSize / 10f, terrainSize / 10f);
                    terrainLayers[i] = layer;
                }
            }

            if (terrainLayers.Length > 0)
            {
                terrainData.terrainLayers = terrainLayers;

                // テクスチャの重みを設定（簡易版）
                float[,,] alphamaps = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainLayers.Length];

                for (int x = 0; x < terrainData.alphamapWidth; x++)
                {
                    for (int z = 0; z < terrainData.alphamapHeight; z++)
                    {
                        // 地形タイプに基づいてテクスチャ重みを設定
                        int gridX = Mathf.Clamp(x * 16 / resolution, 0, resolution / 16 - 1);
                        int gridZ = Mathf.Clamp(z * 16 / resolution, 0, resolution / 16 - 1);
                        Vector2Int gridPos = new Vector2Int(gridX, gridZ);

                        TerrainType terrainType = terrainMap.ContainsKey(gridPos) ? terrainMap[gridPos] : dominantType;

                        // 対応するレイヤーに重みを設定
                        for (int layer = 0; layer < terrainLayers.Length; layer++)
                        {
                            if (layer < availableTypes.Count && availableTypes[layer].type == terrainType)
                            {
                                alphamaps[z, x, layer] = 1f;
                            }
                            else
                            {
                                alphamaps[z, x, layer] = 0f;
                            }
                        }
                    }
                }

                terrainData.SetAlphamaps(0, 0, alphamaps);
            }
        }

        /// <summary>
        /// 地形詳細（草木など）を適用
        /// </summary>
        private void ApplyTerrainDetails()
        {
            // 詳細設定は次のフェーズで実装
            VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", "地形詳細設定は次のフェーズで実装されます");
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
