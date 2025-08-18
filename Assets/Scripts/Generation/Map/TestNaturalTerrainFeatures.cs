using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Generation
{
    /// <summary>
    /// 自然地形特徴の簡単なテスト実行
    /// 要求1.1, 1.5: 地形が自然な川、山脈、谷を含むことの確認
    /// </summary>
    public class TestNaturalTerrainFeatures : MonoBehaviour
    {
        [Header("テスト実行")]
        [SerializeField] private bool runTestOnStart = true;
        
        void Start()
        {
            if (runTestOnStart)
            {
                TestNaturalFeatures();
            }
        }

        /// <summary>
        /// 自然地形特徴のテスト実行
        /// </summary>
        public void TestNaturalFeatures()
        {
            Debug.Log("=== 自然地形特徴テスト開始 ===");

            try
            {
                // NaturalTerrainFeaturesコンポーネントを取得または作成
                var naturalFeatures = GetComponent<NaturalTerrainFeatures>();
                if (naturalFeatures == null)
                {
                    naturalFeatures = gameObject.AddComponent<NaturalTerrainFeatures>();
                    Debug.Log("NaturalTerrainFeaturesコンポーネントを作成しました");
                }

                // テスト用ハイトマップを生成
                int resolution = 128;
                float tileSize = 1000f;
                var heightmap = GenerateTestHeightmap(resolution, 50f);
                Debug.Log($"テスト用ハイトマップを生成しました: {resolution}x{resolution}");

                // 自然地形特徴を生成
                var startTime = System.DateTime.Now;
                var featureData = naturalFeatures.GenerateNaturalTerrainFeatures(heightmap, resolution, tileSize);
                var generationTime = (System.DateTime.Now - startTime).TotalMilliseconds;

                // 結果を検証
                bool success = ValidateResults(featureData);

                if (success)
                {
                    Debug.Log("✅ 自然地形特徴テスト成功!");
                    Debug.Log($"  🏔️ 山脈数: {featureData.mountainRanges.Count}");
                    Debug.Log($"  🏞️ 河川数: {featureData.riverSystems.Count}");
                    Debug.Log($"  ⏱️ 生成時間: {generationTime:F2}ms");
                    
                    // 詳細情報を表示
                    ShowDetailedResults(featureData);
                }
                else
                {
                    Debug.LogError("❌ 自然地形特徴テスト失敗");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ テスト中にエラーが発生しました: {e.Message}");
                Debug.LogError($"スタックトレース: {e.StackTrace}");
            }

            Debug.Log("=== 自然地形特徴テスト完了 ===");
        }

        /// <summary>
        /// テスト用ハイトマップの生成
        /// </summary>
        private float[,] GenerateTestHeightmap(int resolution, float maxHeight)
        {
            var heightmap = new float[resolution, resolution];

            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    float nx = (float)x / resolution;
                    float ny = (float)y / resolution;

                    // 複数のノイズレイヤーを組み合わせ
                    float noise1 = Mathf.PerlinNoise(nx * 3f, ny * 3f);
                    float noise2 = Mathf.PerlinNoise(nx * 8f, ny * 8f) * 0.5f;
                    float noise3 = Mathf.PerlinNoise(nx * 16f, ny * 16f) * 0.25f;
                    
                    heightmap[x, y] = (noise1 + noise2 + noise3) * maxHeight;
                }
            }

            return heightmap;
        }

        /// <summary>
        /// 結果の検証
        /// </summary>
        private bool ValidateResults(NaturalTerrainFeatures.TerrainFeatureData featureData)
        {
            if (featureData == null)
            {
                Debug.LogError("featureDataがnullです");
                return false;
            }

            if (featureData.generationStats == null)
            {
                Debug.LogError("generationStatsがnullです");
                return false;
            }

            if (featureData.riverSystems == null)
            {
                Debug.LogError("riverSystemsがnullです");
                return false;
            }

            if (featureData.mountainRanges == null)
            {
                Debug.LogError("mountainRangesがnullです");
                return false;
            }

            // 基本的な検証
            if (featureData.generationStats.totalGenerationTime <= 0)
            {
                Debug.LogError("生成時間が無効です");
                return false;
            }

            // 河川の検証
            foreach (var river in featureData.riverSystems)
            {
                if (river.riverPath == null || river.riverPath.Count < 2)
                {
                    Debug.LogError("河川経路が無効です");
                    return false;
                }

                if (river.flow <= 0)
                {
                    Debug.LogError("河川の流量が無効です");
                    return false;
                }
            }

            // 山脈の検証
            foreach (var range in featureData.mountainRanges)
            {
                if (range.ridgeLine == null || range.ridgeLine.Count < 2)
                {
                    Debug.LogError("山脈の尾根線が無効です");
                    return false;
                }

                if (range.maxElevation <= 0)
                {
                    Debug.LogError("山脈の最大標高が無効です");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 詳細結果の表示
        /// </summary>
        private void ShowDetailedResults(NaturalTerrainFeatures.TerrainFeatureData featureData)
        {
            Debug.Log("=== 詳細結果 ===");

            // 河川の詳細
            for (int i = 0; i < featureData.riverSystems.Count; i++)
            {
                var river = featureData.riverSystems[i];
                float riverLength = CalculateRiverLength(river.riverPath);
                Debug.Log($"河川 {i + 1}: 長さ={riverLength:F1}m, 流量={river.flow:F2}, 幅={river.width:F1}m, 深さ={river.depth:F1}m");
            }

            // 山脈の詳細
            for (int i = 0; i < featureData.mountainRanges.Count; i++)
            {
                var range = featureData.mountainRanges[i];
                Debug.Log($"山脈 {i + 1}: 最大標高={range.maxElevation:F1}m, ピーク数={range.peaks.Count}, 平均傾斜={range.averageSlope:F1}度");
            }

            Debug.Log("================");
        }

        /// <summary>
        /// 河川の長さを計算
        /// </summary>
        private float CalculateRiverLength(System.Collections.Generic.List<Vector3> riverPath)
        {
            if (riverPath.Count < 2) return 0f;

            float totalLength = 0f;
            for (int i = 0; i < riverPath.Count - 1; i++)
            {
                totalLength += Vector3.Distance(riverPath[i], riverPath[i + 1]);
            }

            return totalLength;
        }

        /// <summary>
        /// 手動でテストを実行
        /// </summary>
        [ContextMenu("Run Test")]
        public void RunManualTest()
        {
            TestNaturalFeatures();
        }
    }
}