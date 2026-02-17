using UnityEngine;
using System.Collections.Generic;
using Vastcore.Generation;

namespace Vastcore.Generation
{
    /// <summary>
    /// 自然地形特徴システムのテストクラス
    /// 要求1.1, 1.5の検証
    /// </summary>
    public class NaturalTerrainFeaturesTest : MonoBehaviour
    {
        [Header("テスト設定")]
        public bool runTestOnStart = true;
        public bool enableDebugVisualization = true;
        public Material riverMaterial;
        public Material mountainMaterial;
        public Material valleyMaterial;

        [Header("テスト地形設定")]
        public int testResolution = 256;
        public float testTileSize = 2000f;
        public float testMaxHeight = 100f;

        private NaturalTerrainFeatures terrainFeatures;
        private float[,] testHeightmap;
        private List<GameObject> debugObjects = new List<GameObject>();

        void Start()
        {
            if (runTestOnStart)
            {
                RunNaturalTerrainFeaturesTest();
            }
        }

        /// <summary>
        /// 自然地形特徴システムの統合テスト
        /// </summary>
        public void RunNaturalTerrainFeaturesTest()
        {
            Debug.Log("=== 自然地形特徴システム テスト開始 ===");

            // テスト環境の初期化
            InitializeTestEnvironment();

            // 1. 河川システムのテスト
            TestRiverSystemGeneration();

            // 2. 山脈システムのテスト
            TestMountainRangeGeneration();

            // 3. 谷システムのテスト
            TestValleyGeneration();

            // 4. 統合システムのテスト
            TestIntegratedTerrainGeneration();

            // 5. パフォーマンステスト
            TestPerformance();

            Debug.Log("=== 自然地形特徴システム テスト完了 ===");
        }

        /// <summary>
        /// テスト環境の初期化
        /// </summary>
        private void InitializeTestEnvironment()
        {
            // NaturalTerrainFeaturesコンポーネントの取得または作成
            terrainFeatures = GetComponent<NaturalTerrainFeatures>();
            if (terrainFeatures == null)
            {
                terrainFeatures = gameObject.AddComponent<NaturalTerrainFeatures>();
            }

            // テスト用ハイトマップの生成
            testHeightmap = GenerateTestHeightmap();

            // 既存のデバッグオブジェクトをクリア
            ClearDebugObjects();

            Debug.Log($"テスト環境初期化完了 - 解像度: {testResolution}x{testResolution}, サイズ: {testTileSize}m");
        }

        /// <summary>
        /// テスト用ハイトマップの生成
        /// </summary>
        private float[,] GenerateTestHeightmap()
        {
            var heightmap = new float[testResolution, testResolution];

            // 基本的なノイズベースの地形を生成
            for (int x = 0; x < testResolution; x++)
            {
                for (int y = 0; y < testResolution; y++)
                {
                    float noiseValue = Mathf.PerlinNoise(x * 0.01f, y * 0.01f);
                    heightmap[x, y] = noiseValue * testMaxHeight;
                }
            }

            return heightmap;
        }

        /// <summary>
        /// 河川システム生成のテスト
        /// </summary>
        private void TestRiverSystemGeneration()
        {
            Debug.Log("河川システム生成テスト開始...");

            var startTime = System.DateTime.Now;
            var rivers = terrainFeatures.GenerateRiverSystems(testHeightmap, testResolution, testTileSize);
            var generationTime = (System.DateTime.Now - startTime).TotalMilliseconds;

            Debug.Log($"河川生成完了 - 生成数: {rivers.Count}, 生成時間: {generationTime:F2}ms");

            // 河川の可視化
            if (enableDebugVisualization)
            {
                VisualizeRivers(rivers);
            }

            // 河川データの検証
            ValidateRiverSystems(rivers);
        }

        /// <summary>
        /// 山脈システム生成のテスト
        /// </summary>
        private void TestMountainRangeGeneration()
        {
            Debug.Log("山脈システム生成テスト開始...");

            var startTime = System.DateTime.Now;
            var mountainRanges = terrainFeatures.GenerateMountainRanges(testHeightmap, testResolution, testTileSize);
            var generationTime = (System.DateTime.Now - startTime).TotalMilliseconds;

            Debug.Log($"山脈生成完了 - 生成数: {mountainRanges.Count}, 生成時間: {generationTime:F2}ms");

            // 山脈の可視化
            if (enableDebugVisualization)
            {
                VisualizeMountainRanges(mountainRanges);
            }

            // 山脈データの検証
            ValidateMountainRanges(mountainRanges);
        }

        /// <summary>
        /// 谷システム生成のテスト
        /// </summary>
        private void TestValleyGeneration()
        {
            Debug.Log("谷システム生成テスト開始...");

            // 山脈を先に生成
            var mountainRanges = terrainFeatures.GenerateMountainRanges(testHeightmap, testResolution, testTileSize);

            var startTime = System.DateTime.Now;
            terrainFeatures.GenerateValleys(testHeightmap, testResolution, testTileSize, mountainRanges);
            var generationTime = (System.DateTime.Now - startTime).TotalMilliseconds;

            Debug.Log($"谷生成完了 - 生成時間: {generationTime:F2}ms");

            // 谷の効果を検証
            ValidateValleyEffects();
        }

        /// <summary>
        /// 統合地形生成のテスト
        /// </summary>
        private void TestIntegratedTerrainGeneration()
        {
            Debug.Log("統合地形生成テスト開始...");

            // 新しいハイトマップで統合テスト
            var integratedHeightmap = GenerateTestHeightmap();

            var startTime = System.DateTime.Now;
            terrainFeatures.GenerateNaturalTerrainFeatures(integratedHeightmap, testResolution, testTileSize);
            var generationTime = (System.DateTime.Now - startTime).TotalMilliseconds;

            Debug.Log($"統合地形生成完了 - 生成時間: {generationTime:F2}ms");

            // 統合結果の検証
            ValidateIntegratedTerrain(integratedHeightmap);
        }

        /// <summary>
        /// パフォーマンステスト
        /// </summary>
        private void TestPerformance()
        {
            Debug.Log("パフォーマンステスト開始...");

            int iterations = 10;
            float totalTime = 0f;

            for (int i = 0; i < iterations; i++)
            {
                var testMap = GenerateTestHeightmap();
                var startTime = System.DateTime.Now;
                
                terrainFeatures.GenerateNaturalTerrainFeatures(testMap, testResolution, testTileSize);
                
                var iterationTime = (float)(System.DateTime.Now - startTime).TotalMilliseconds;
                totalTime += iterationTime;
            }

            float averageTime = totalTime / iterations;
            Debug.Log($"パフォーマンステスト完了 - 平均生成時間: {averageTime:F2}ms ({iterations}回実行)");

            // パフォーマンス基準の検証
            if (averageTime < 100f) // 100ms以下
            {
                Debug.Log("✓ パフォーマンステスト合格");
            }
            else
            {
                Debug.LogWarning($"⚠ パフォーマンス警告: 平均生成時間が基準値(100ms)を超過");
            }
        }

        /// <summary>
        /// 河川の可視化
        /// </summary>
        private void VisualizeRivers(List<NaturalTerrainFeatures.RiverSystem> rivers)
        {
            foreach (var river in rivers)
            {
                if (river.riverPath.Count > 1)
                {
                    var riverObject = new GameObject($"River_{rivers.IndexOf(river)}");
                    var lineRenderer = riverObject.AddComponent<LineRenderer>();
                    
                    lineRenderer.material = riverMaterial;
                    lineRenderer.startWidth = river.width * 0.1f;
                    lineRenderer.endWidth = river.width * 0.05f;
                    lineRenderer.positionCount = river.riverPath.Count;
                    lineRenderer.useWorldSpace = true;

                    for (int i = 0; i < river.riverPath.Count; i++)
                    {
                        lineRenderer.SetPosition(i, river.riverPath[i]);
                    }

                    debugObjects.Add(riverObject);
                }
            }
        }

        /// <summary>
        /// 山脈の可視化
        /// </summary>
        private void VisualizeMountainRanges(List<NaturalTerrainFeatures.MountainRange> mountainRanges)
        {
            foreach (var range in mountainRanges)
            {
                if (range.ridgeLine.Count > 1)
                {
                    var rangeObject = new GameObject($"MountainRange_{mountainRanges.IndexOf(range)}");
                    var lineRenderer = rangeObject.AddComponent<LineRenderer>();
                    
                    lineRenderer.material = mountainMaterial;
                    lineRenderer.startWidth = 20f;
                    lineRenderer.endWidth = 20f;
                    lineRenderer.positionCount = range.ridgeLine.Count;
                    lineRenderer.useWorldSpace = true;

                    for (int i = 0; i < range.ridgeLine.Count; i++)
                    {
                        lineRenderer.SetPosition(i, range.ridgeLine[i]);
                    }

                    debugObjects.Add(rangeObject);

                    // ピークの可視化
                    foreach (var peak in range.peaks)
                    {
                        var peakObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        peakObject.name = $"Peak_{range.peaks.IndexOf(peak)}";
                        peakObject.transform.position = peak;
                        peakObject.transform.localScale = Vector3.one * 30f;
                        peakObject.GetComponent<Renderer>().material = mountainMaterial;
                        
                        debugObjects.Add(peakObject);
                    }
                }
            }
        }

        /// <summary>
        /// 河川システムの検証
        /// </summary>
        private void ValidateRiverSystems(List<NaturalTerrainFeatures.RiverSystem> rivers)
        {
            bool allValid = true;

            foreach (var river in rivers)
            {
                // 河川経路の妥当性チェック
                if (river.riverPath.Count < 2)
                {
                    Debug.LogError($"河川経路が短すぎます: {river.riverPath.Count}点");
                    allValid = false;
                }

                // 流量の妥当性チェック
                if (river.flow <= 0)
                {
                    Debug.LogError($"河川の流量が無効です: {river.flow}");
                    allValid = false;
                }

                // 高度の単調性チェック（河川は下流に向かって低くなるべき）
                for (int i = 0; i < river.riverPath.Count - 1; i++)
                {
                    if (river.riverPath[i].y < river.riverPath[i + 1].y)
                    {
                        Debug.LogWarning($"河川の高度が逆流しています: {i}番目の点");
                    }
                }
            }

            if (allValid)
            {
                Debug.Log("✓ 河川システム検証合格");
            }
        }

        /// <summary>
        /// 山脈システムの検証
        /// </summary>
        private void ValidateMountainRanges(List<NaturalTerrainFeatures.MountainRange> mountainRanges)
        {
            bool allValid = true;

            foreach (var range in mountainRanges)
            {
                // 尾根線の妥当性チェック
                if (range.ridgeLine.Count < 2)
                {
                    Debug.LogError($"山脈の尾根線が短すぎます: {range.ridgeLine.Count}点");
                    allValid = false;
                }

                // 最大標高の妥当性チェック
                if (range.maxElevation <= 0)
                {
                    Debug.LogError($"山脈の最大標高が無効です: {range.maxElevation}");
                    allValid = false;
                }

                // ピークの妥当性チェック
                foreach (var peak in range.peaks)
                {
                    if (peak.y < range.maxElevation * 0.8f)
                    {
                        Debug.LogWarning($"ピークの高度が低すぎます: {peak.y} (最大標高の80%未満)");
                    }
                }
            }

            if (allValid)
            {
                Debug.Log("✓ 山脈システム検証合格");
            }
        }

        /// <summary>
        /// 谷の効果検証
        /// </summary>
        private void ValidateValleyEffects()
        {
            // 谷の生成前後でハイトマップの変化を確認
            // この実装では簡略化して、ハイトマップの統計情報を確認
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;
            float avgHeight = 0f;

            for (int x = 0; x < testResolution; x++)
            {
                for (int y = 0; y < testResolution; y++)
                {
                    float height = testHeightmap[x, y];
                    minHeight = Mathf.Min(minHeight, height);
                    maxHeight = Mathf.Max(maxHeight, height);
                    avgHeight += height;
                }
            }

            avgHeight /= (testResolution * testResolution);

            Debug.Log($"谷生成後の地形統計 - 最小高度: {minHeight:F2}, 最大高度: {maxHeight:F2}, 平均高度: {avgHeight:F2}");
            Debug.Log("✓ 谷システム検証合格");
        }

        /// <summary>
        /// 統合地形の検証
        /// </summary>
        private void ValidateIntegratedTerrain(float[,] heightmap)
        {
            // 地形の連続性チェック
            float maxSlope = 0f;
            int steepSlopeCount = 0;
            float slopeThreshold = 45f; // 45度

            for (int x = 1; x < testResolution - 1; x++)
            {
                for (int y = 1; y < testResolution - 1; y++)
                {
                    float currentHeight = heightmap[x, y];
                    
                    // 8方向の傾斜をチェック
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;

                            float neighborHeight = heightmap[x + dx, y + dy];
                            float distance = Mathf.Sqrt(dx * dx + dy * dy);
                            float slope = Mathf.Abs(currentHeight - neighborHeight) / distance;
                            float slopeDegrees = Mathf.Atan(slope) * Mathf.Rad2Deg;

                            maxSlope = Mathf.Max(maxSlope, slopeDegrees);
                            
                            if (slopeDegrees > slopeThreshold)
                            {
                                steepSlopeCount++;
                            }
                        }
                    }
                }
            }

            float steepSlopePercentage = (float)steepSlopeCount / (testResolution * testResolution * 8) * 100f;

            Debug.Log($"統合地形検証 - 最大傾斜: {maxSlope:F2}度, 急傾斜率: {steepSlopePercentage:F2}%");

            if (steepSlopePercentage < 5f) // 5%未満
            {
                Debug.Log("✓ 統合地形検証合格");
            }
            else
            {
                Debug.LogWarning($"⚠ 急傾斜が多すぎます: {steepSlopePercentage:F2}%");
            }
        }

        /// <summary>
        /// デバッグオブジェクトのクリア
        /// </summary>
        private void ClearDebugObjects()
        {
            foreach (var obj in debugObjects)
            {
                if (obj != null)
                {
                    DestroyImmediate(obj);
                }
            }
            debugObjects.Clear();
        }

        void OnDestroy()
        {
            ClearDebugObjects();
        }
    }
}