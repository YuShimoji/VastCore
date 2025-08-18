using UnityEngine;
using System.Collections.Generic;
using Vastcore.Generation;

namespace Vastcore.Generation
{
    /// <summary>
    /// 自然地形特徴システムの包括的検証テスト
    /// 要求1.1, 1.5: 地形が自然な川、山脈、谷を含むことの検証
    /// </summary>
    public class NaturalTerrainValidationTest : MonoBehaviour
    {
        [Header("検証設定")]
        public bool runValidationOnStart = true;
        public bool enableDetailedLogging = true;
        public bool createVisualDebugObjects = false;

        [Header("テスト地形設定")]
        public int testResolution = 256;
        public float testTileSize = 2000f;
        public float testMaxHeight = 100f;

        [Header("検証基準")]
        [Range(1, 10)]
        public int minimumRiverCount = 1;
        [Range(1, 5)]
        public int minimumMountainRangeCount = 1;
        public float minimumRiverLength = 100f;
        public float minimumMountainHeight = 50f;

        private NaturalTerrainFeatures naturalFeatures;
        private List<GameObject> debugObjects = new List<GameObject>();

        void Start()
        {
            if (runValidationOnStart)
            {
                StartCoroutine(RunValidationTest());
            }
        }

        /// <summary>
        /// 検証テストの実行
        /// </summary>
        public System.Collections.IEnumerator RunValidationTest()
        {
            Debug.Log("=== 自然地形特徴 包括的検証テスト開始 ===");

            // 初期化
            InitializeTestEnvironment();
            yield return null;

            // テスト用地形データを生成
            var heightmap = GenerateTestHeightmap();
            yield return null;

            // 自然地形特徴を生成
            var featureData = GenerateNaturalFeatures(heightmap);
            yield return null;

            // 検証を実行
            bool validationPassed = ValidateNaturalFeatures(featureData, heightmap);
            yield return null;

            // 結果を報告
            ReportValidationResults(validationPassed, featureData);

            Debug.Log("=== 自然地形特徴 包括的検証テスト完了 ===");
        }

        /// <summary>
        /// テスト環境の初期化
        /// </summary>
        private void InitializeTestEnvironment()
        {
            // NaturalTerrainFeaturesコンポーネントを取得または作成
            naturalFeatures = GetComponent<NaturalTerrainFeatures>();
            if (naturalFeatures == null)
            {
                naturalFeatures = gameObject.AddComponent<NaturalTerrainFeatures>();
            }

            // 設定を最適化
            naturalFeatures.enableRiverGeneration = true;
            naturalFeatures.enableMountainGeneration = true;
            naturalFeatures.enableValleyGeneration = true;
            naturalFeatures.maxRiversPerTile = 3;
            naturalFeatures.maxMountainRanges = 2;
            naturalFeatures.riverWidth = 15f;
            naturalFeatures.riverDepth = 5f;
            naturalFeatures.mountainHeight = testMaxHeight * 2f;
            naturalFeatures.valleyDepth = testMaxHeight * 0.3f;

            // デバッグオブジェクトをクリア
            ClearDebugObjects();

            if (enableDetailedLogging)
            {
                Debug.Log("テスト環境初期化完了");
            }
        }

        /// <summary>
        /// テスト用ハイトマップの生成
        /// </summary>
        private float[,] GenerateTestHeightmap()
        {
            var heightmap = new float[testResolution, testResolution];

            // 複数のノイズレイヤーを組み合わせて複雑な地形を作成
            for (int x = 0; x < testResolution; x++)
            {
                for (int y = 0; y < testResolution; y++)
                {
                    float nx = (float)x / testResolution;
                    float ny = (float)y / testResolution;

                    // ベースとなる大きな起伏
                    float baseNoise = Mathf.PerlinNoise(nx * 2f, ny * 2f);
                    
                    // 中程度の起伏
                    float mediumNoise = Mathf.PerlinNoise(nx * 8f, ny * 8f) * 0.5f;
                    
                    // 細かい起伏
                    float detailNoise = Mathf.PerlinNoise(nx * 32f, ny * 32f) * 0.25f;

                    // 組み合わせ
                    float combinedNoise = baseNoise + mediumNoise + detailNoise;
                    heightmap[x, y] = combinedNoise * testMaxHeight;
                }
            }

            if (enableDetailedLogging)
            {
                Debug.Log($"テスト用ハイトマップ生成完了: {testResolution}x{testResolution}");
            }

            return heightmap;
        }

        /// <summary>
        /// 自然地形特徴の生成
        /// </summary>
        private NaturalTerrainFeatures.TerrainFeatureData GenerateNaturalFeatures(float[,] heightmap)
        {
            var startTime = System.DateTime.Now;
            
            var featureData = naturalFeatures.GenerateNaturalTerrainFeatures(heightmap, testResolution, testTileSize);
            
            var generationTime = (System.DateTime.Now - startTime).TotalMilliseconds;

            if (enableDetailedLogging)
            {
                Debug.Log($"自然地形特徴生成完了: {generationTime:F2}ms");
                Debug.Log($"  - 河川数: {featureData.riverSystems.Count}");
                Debug.Log($"  - 山脈数: {featureData.mountainRanges.Count}");
            }

            return featureData;
        }

        /// <summary>
        /// 自然地形特徴の検証
        /// </summary>
        private bool ValidateNaturalFeatures(NaturalTerrainFeatures.TerrainFeatureData featureData, float[,] heightmap)
        {
            bool allTestsPassed = true;
            var validationResults = new List<string>();

            // 1. 河川システムの検証
            bool riverValidation = ValidateRiverSystems(featureData.riverSystems, validationResults);
            allTestsPassed &= riverValidation;

            // 2. 山脈システムの検証
            bool mountainValidation = ValidateMountainRanges(featureData.mountainRanges, validationResults);
            allTestsPassed &= mountainValidation;

            // 3. 谷システムの検証
            bool valleyValidation = ValidateValleyEffects(heightmap, validationResults);
            allTestsPassed &= valleyValidation;

            // 4. 地形の連続性検証
            bool continuityValidation = ValidateTerrainContinuity(heightmap, validationResults);
            allTestsPassed &= continuityValidation;

            // 5. パフォーマンス検証
            bool performanceValidation = ValidatePerformance(featureData, validationResults);
            allTestsPassed &= performanceValidation;

            // 検証結果をログ出力
            if (enableDetailedLogging)
            {
                foreach (var result in validationResults)
                {
                    Debug.Log(result);
                }
            }

            // ビジュアルデバッグオブジェクトを作成
            if (createVisualDebugObjects)
            {
                CreateVisualDebugObjects(featureData);
            }

            return allTestsPassed;
        }

        /// <summary>
        /// 河川システムの検証
        /// </summary>
        private bool ValidateRiverSystems(List<NaturalTerrainFeatures.RiverSystem> rivers, List<string> results)
        {
            bool passed = true;

            // 河川数の検証
            if (rivers.Count < minimumRiverCount)
            {
                results.Add($"✗ 河川数不足: {rivers.Count} < {minimumRiverCount}");
                passed = false;
            }
            else
            {
                results.Add($"✓ 河川数: {rivers.Count} >= {minimumRiverCount}");
            }

            // 各河川の詳細検証
            foreach (var river in rivers)
            {
                // 河川経路の長さ検証
                float riverLength = CalculateRiverLength(river.riverPath);
                if (riverLength < minimumRiverLength)
                {
                    results.Add($"✗ 河川長不足: {riverLength:F1}m < {minimumRiverLength}m");
                    passed = false;
                }

                // 流量の妥当性検証
                if (river.flow <= 0)
                {
                    results.Add($"✗ 河川流量無効: {river.flow}");
                    passed = false;
                }

                // 河川経路の単調性検証（下流に向かって低くなる）
                bool isMonotonic = ValidateRiverMonotonicity(river.riverPath);
                if (!isMonotonic)
                {
                    results.Add($"✗ 河川の高度が逆流している");
                    passed = false;
                }
            }

            if (passed)
            {
                results.Add($"✓ 河川システム検証合格");
            }

            return passed;
        }

        /// <summary>
        /// 山脈システムの検証
        /// </summary>
        private bool ValidateMountainRanges(List<NaturalTerrainFeatures.MountainRange> mountainRanges, List<string> results)
        {
            bool passed = true;

            // 山脈数の検証
            if (mountainRanges.Count < minimumMountainRangeCount)
            {
                results.Add($"✗ 山脈数不足: {mountainRanges.Count} < {minimumMountainRangeCount}");
                passed = false;
            }
            else
            {
                results.Add($"✓ 山脈数: {mountainRanges.Count} >= {minimumMountainRangeCount}");
            }

            // 各山脈の詳細検証
            foreach (var range in mountainRanges)
            {
                // 最大標高の検証
                if (range.maxElevation < minimumMountainHeight)
                {
                    results.Add($"✗ 山脈高度不足: {range.maxElevation:F1}m < {minimumMountainHeight}m");
                    passed = false;
                }

                // 尾根線の妥当性検証
                if (range.ridgeLine.Count < 3)
                {
                    results.Add($"✗ 尾根線が短すぎる: {range.ridgeLine.Count}点");
                    passed = false;
                }

                // ピークの妥当性検証
                if (range.peaks.Count == 0)
                {
                    results.Add($"✗ ピークが存在しない");
                    passed = false;
                }
            }

            if (passed)
            {
                results.Add($"✓ 山脈システム検証合格");
            }

            return passed;
        }

        /// <summary>
        /// 谷システムの検証
        /// </summary>
        private bool ValidateValleyEffects(float[,] heightmap, List<string> results)
        {
            bool passed = true;

            // 地形の高度分布を分析
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;
            float avgHeight = 0f;
            int totalPoints = heightmap.GetLength(0) * heightmap.GetLength(1);

            for (int x = 0; x < heightmap.GetLength(0); x++)
            {
                for (int y = 0; y < heightmap.GetLength(1); y++)
                {
                    float height = heightmap[x, y];
                    minHeight = Mathf.Min(minHeight, height);
                    maxHeight = Mathf.Max(maxHeight, height);
                    avgHeight += height;
                }
            }

            avgHeight /= totalPoints;

            // 高度差の妥当性検証
            float heightRange = maxHeight - minHeight;
            if (heightRange < testMaxHeight * 0.5f)
            {
                results.Add($"✗ 地形の高度差不足: {heightRange:F1}m");
                passed = false;
            }
            else
            {
                results.Add($"✓ 地形高度差: {heightRange:F1}m");
            }

            // 谷の存在確認（平均より低い領域の存在）
            int lowAreaCount = 0;
            for (int x = 0; x < heightmap.GetLength(0); x++)
            {
                for (int y = 0; y < heightmap.GetLength(1); y++)
                {
                    if (heightmap[x, y] < avgHeight - heightRange * 0.2f)
                    {
                        lowAreaCount++;
                    }
                }
            }

            float lowAreaPercentage = (float)lowAreaCount / totalPoints * 100f;
            if (lowAreaPercentage < 10f)
            {
                results.Add($"✗ 谷領域不足: {lowAreaPercentage:F1}%");
                passed = false;
            }
            else
            {
                results.Add($"✓ 谷領域: {lowAreaPercentage:F1}%");
            }

            return passed;
        }

        /// <summary>
        /// 地形の連続性検証
        /// </summary>
        private bool ValidateTerrainContinuity(float[,] heightmap, List<string> results)
        {
            bool passed = true;
            int resolution = heightmap.GetLength(0);
            
            float maxSlope = 0f;
            int steepSlopeCount = 0;
            float slopeThreshold = 60f; // 60度

            for (int x = 1; x < resolution - 1; x++)
            {
                for (int y = 1; y < resolution - 1; y++)
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

            float steepSlopePercentage = (float)steepSlopeCount / (resolution * resolution * 8) * 100f;

            results.Add($"地形連続性: 最大傾斜={maxSlope:F1}度, 急傾斜率={steepSlopePercentage:F2}%");

            if (steepSlopePercentage > 10f)
            {
                results.Add($"✗ 急傾斜が多すぎる: {steepSlopePercentage:F2}%");
                passed = false;
            }
            else
            {
                results.Add($"✓ 地形連続性良好");
            }

            return passed;
        }

        /// <summary>
        /// パフォーマンス検証
        /// </summary>
        private bool ValidatePerformance(NaturalTerrainFeatures.TerrainFeatureData featureData, List<string> results)
        {
            bool passed = true;

            // 生成時間の検証
            float generationTime = featureData.generationStats.totalGenerationTime;
            float maxAllowedTime = 500f; // 500ms

            if (generationTime > maxAllowedTime)
            {
                results.Add($"✗ 生成時間超過: {generationTime:F2}ms > {maxAllowedTime}ms");
                passed = false;
            }
            else
            {
                results.Add($"✓ 生成時間: {generationTime:F2}ms");
            }

            return passed;
        }

        /// <summary>
        /// 河川の長さを計算
        /// </summary>
        private float CalculateRiverLength(List<Vector3> riverPath)
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
        /// 河川の単調性を検証
        /// </summary>
        private bool ValidateRiverMonotonicity(List<Vector3> riverPath)
        {
            if (riverPath.Count < 2) return true;

            int violationCount = 0;
            for (int i = 0; i < riverPath.Count - 1; i++)
            {
                if (riverPath[i].y < riverPath[i + 1].y)
                {
                    violationCount++;
                }
            }

            // 10%以下の逆流は許容
            return (float)violationCount / riverPath.Count < 0.1f;
        }

        /// <summary>
        /// ビジュアルデバッグオブジェクトの作成
        /// </summary>
        private void CreateVisualDebugObjects(NaturalTerrainFeatures.TerrainFeatureData featureData)
        {
            // 河川の可視化
            foreach (var river in featureData.riverSystems)
            {
                var riverObject = new GameObject($"DebugRiver_{featureData.riverSystems.IndexOf(river)}");
                var lineRenderer = riverObject.AddComponent<LineRenderer>();
                
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.color = Color.blue;
                lineRenderer.startWidth = river.width * 0.1f;
                lineRenderer.endWidth = river.width * 0.05f;
                lineRenderer.positionCount = river.riverPath.Count;

                for (int i = 0; i < river.riverPath.Count; i++)
                {
                    lineRenderer.SetPosition(i, river.riverPath[i]);
                }

                debugObjects.Add(riverObject);
            }

            // 山脈の可視化
            foreach (var range in featureData.mountainRanges)
            {
                var rangeObject = new GameObject($"DebugMountainRange_{featureData.mountainRanges.IndexOf(range)}");
                var lineRenderer = rangeObject.AddComponent<LineRenderer>();
                
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.color = Color.red;
                lineRenderer.startWidth = 20f;
                lineRenderer.endWidth = 20f;
                lineRenderer.positionCount = range.ridgeLine.Count;

                for (int i = 0; i < range.ridgeLine.Count; i++)
                {
                    lineRenderer.SetPosition(i, range.ridgeLine[i]);
                }

                debugObjects.Add(rangeObject);

                // ピークの可視化
                foreach (var peak in range.peaks)
                {
                    var peakObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    peakObject.name = $"DebugPeak_{range.peaks.IndexOf(peak)}";
                    peakObject.transform.position = peak;
                    peakObject.transform.localScale = Vector3.one * 30f;
                    peakObject.GetComponent<Renderer>().material.color = Color.yellow;
                    
                    debugObjects.Add(peakObject);
                }
            }
        }

        /// <summary>
        /// 検証結果の報告
        /// </summary>
        private void ReportValidationResults(bool passed, NaturalTerrainFeatures.TerrainFeatureData featureData)
        {
            if (passed)
            {
                Debug.Log("🎉 自然地形特徴検証テスト 合格!");
                Debug.Log("地形が自然な川、山脈、谷を含むことが確認されました。");
            }
            else
            {
                Debug.LogError("❌ 自然地形特徴検証テスト 不合格");
                Debug.LogError("一部の検証項目で基準を満たしていません。");
            }

            // 統計情報の表示
            Debug.Log($"検証統計:");
            Debug.Log($"  河川数: {featureData.riverSystems.Count}");
            Debug.Log($"  山脈数: {featureData.mountainRanges.Count}");
            Debug.Log($"  生成時間: {featureData.generationStats.totalGenerationTime:F2}ms");
            Debug.Log($"  解像度: {featureData.generationStats.resolution}x{featureData.generationStats.resolution}");
            Debug.Log($"  タイルサイズ: {featureData.generationStats.tileSize}m");
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
                    if (Application.isPlaying)
                        Destroy(obj);
                    else
                        DestroyImmediate(obj);
                }
            }
            debugObjects.Clear();
        }

        void OnDestroy()
        {
            ClearDebugObjects();
        }

        /// <summary>
        /// 手動でテストを実行
        /// </summary>
        [ContextMenu("Run Validation Test")]
        public void RunManualValidationTest()
        {
            StartCoroutine(RunValidationTest());
        }
    }
}