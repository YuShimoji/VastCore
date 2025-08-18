using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Generation
{
    /// <summary>
    /// 自然地形特徴システムの簡単なテストランナー
    /// 要求1.1, 1.5の検証用
    /// </summary>
    public class NaturalTerrainTestRunner : MonoBehaviour
    {
        [Header("テスト設定")]
        public bool runOnStart = true;
        public int testResolution = 128;
        public float testTileSize = 1000f;
        public float testMaxHeight = 50f;

        void Start()
        {
            if (runOnStart)
            {
                RunQuickTest();
            }
        }

        /// <summary>
        /// 簡単なテスト実行
        /// </summary>
        public void RunQuickTest()
        {
            Debug.Log("=== 自然地形特徴 簡単テスト開始 ===");

            // NaturalTerrainFeaturesコンポーネントを取得または作成
            var naturalFeatures = GetComponent<NaturalTerrainFeatures>();
            if (naturalFeatures == null)
            {
                naturalFeatures = gameObject.AddComponent<NaturalTerrainFeatures>();
            }

            // テスト用ハイトマップを生成
            var heightmap = GenerateTestHeightmap();

            try
            {
                // 自然地形特徴を生成
                var featureData = naturalFeatures.GenerateNaturalTerrainFeatures(heightmap, testResolution, testTileSize);

                // 結果を検証
                bool success = ValidateResults(featureData);

                if (success)
                {
                    Debug.Log("✓ 自然地形特徴テスト成功!");
                    Debug.Log($"  - 河川数: {featureData.riverSystems.Count}");
                    Debug.Log($"  - 山脈数: {featureData.mountainRanges.Count}");
                    Debug.Log($"  - 生成時間: {featureData.generationStats.totalGenerationTime:F2}ms");
                }
                else
                {
                    Debug.LogError("✗ 自然地形特徴テスト失敗");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ テスト中にエラーが発生: {e.Message}");
            }

            Debug.Log("=== 自然地形特徴 簡単テスト完了 ===");
        }

        /// <summary>
        /// テスト用ハイトマップ生成
        /// </summary>
        private float[,] GenerateTestHeightmap()
        {
            var heightmap = new float[testResolution, testResolution];

            for (int x = 0; x < testResolution; x++)
            {
                for (int y = 0; y < testResolution; y++)
                {
                    // 複数のノイズレイヤーを組み合わせ
                    float noise1 = Mathf.PerlinNoise(x * 0.01f, y * 0.01f);
                    float noise2 = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.5f;
                    float noise3 = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.25f;
                    
                    heightmap[x, y] = (noise1 + noise2 + noise3) * testMaxHeight;
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

            // 基本的な検証
            if (featureData.generationStats.totalGenerationTime <= 0)
            {
                Debug.LogError("生成時間が無効です");
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
    }
}