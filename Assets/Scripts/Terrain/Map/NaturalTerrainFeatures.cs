using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    /// <summary>
    /// 自然地形特徴システム - 河川、山脈、谷の自動生成
    /// 要求1.1, 1.5: リアルな地形生成
    ///
    /// partial class 分割:
    ///   NaturalTerrainFeatures.cs       — 設定・データ構造・統合オーケストレータ
    ///   NaturalTerrainFeatures.River.cs  — 河川生成 (流域計算・D8アルゴリズム・浸食)
    ///   NaturalTerrainFeatures.Mountain.cs — 山脈・谷・断層生成 (尾根線・安息角・地質)
    /// </summary>
    public partial class NaturalTerrainFeatures : MonoBehaviour
    {
        #region 河川システム設定
        [Header("河川システム設定")]
        public bool enableRiverGeneration = true;
        public int maxRiversPerTile = 3;
        public float riverWidth = 10f;
        public float riverDepth = 5f;
        public float riverFlowStrength = 1f;
        public AnimationCurve riverWidthCurve = AnimationCurve.Linear(0, 0.5f, 1, 1f);
        public AnimationCurve riverDepthCurve = AnimationCurve.Linear(0, 0.3f, 1, 1f);

        [Header("流域設定")]
        public float watershedThreshold = 0.1f;
        public int minWatershedSize = 100;
        public float drainageIntensity = 0.8f;
        public bool enableTributaries = true;
        public int maxTributaryLevels = 3;

        [Header("浸食・堆積設定")]
        public float erosionStrength = 0.5f;
        public float depositionStrength = 0.3f;
        public int erosionIterations = 5;
        public float sedimentCapacity = 1f;
        public float evaporationRate = 0.1f;
        #endregion

        #region 山脈・谷システム設定
        [Header("山脈システム設定")]
        public bool enableMountainGeneration = true;
        public int maxMountainRanges = 2;
        public float mountainHeight = 200f;
        public float ridgeSharpness = 0.7f;
        public AnimationCurve mountainProfile = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("谷システム設定")]
        public bool enableValleyGeneration = true;
        public float valleyDepth = 50f;
        public float valleyWidth = 100f;
        public AnimationCurve valleyProfile = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("地質学的設定")]
        public float repose_angle = 35f; // 安息角（度）
        public float tectonicStrength = 1f;
        public bool enableFaultLines = true;
        public float faultProbability = 0.1f;
        #endregion

        #region データ構造
        /// <summary>
        /// 河川データ構造
        /// </summary>
        [System.Serializable]
        public class RiverSystem
        {
            public List<Vector3> riverPath;
            public List<Vector3> tributaries;
            public float flow;
            public float width;
            public float depth;
            public int order; // Strahler order
            public Watershed watershed;
        }

        /// <summary>
        /// 流域データ構造
        /// </summary>
        [System.Serializable]
        public class Watershed
        {
            public List<Vector2Int> cells;
            public Vector2Int outlet;
            public float totalFlow;
            public float area;
        }

        /// <summary>
        /// 山脈データ構造
        /// </summary>
        [System.Serializable]
        public class MountainRange
        {
            public List<Vector3> ridgeLine;
            public List<Vector3> peaks;
            public float maxElevation;
            public float averageSlope;
            public GeologicalFormation formation;
        }

        /// <summary>
        /// 地質構造
        /// </summary>
        public enum GeologicalFormation
        {
            Fold,       // 褶曲
            Fault,      // 断層
            Volcanic,   // 火山性
            Erosional   // 浸食性
        }

        /// <summary>
        /// 地形特徴データ構造
        /// </summary>
        [System.Serializable]
        public class TerrainFeatureData
        {
            public List<RiverSystem> riverSystems = new List<RiverSystem>();
            public List<MountainRange> mountainRanges = new List<MountainRange>();
            public TerrainGenerationStats generationStats;
        }

        /// <summary>
        /// 地形生成統計情報
        /// </summary>
        [System.Serializable]
        public class TerrainGenerationStats
        {
            public float totalGenerationTime;
            public int riverCount;
            public int mountainRangeCount;
            public int resolution;
            public float tileSize;
        }
        #endregion

        #region 統合システム
        /// <summary>
        /// 自然地形特徴の統合生成
        /// 河川、山脈、谷を含む自然な地形を生成
        /// 要求1.1, 1.5: リアルな地形生成
        /// </summary>
        public TerrainFeatureData GenerateNaturalTerrainFeatures(float[,] heightmap, int resolution, float tileSize)
        {
            var featureData = new TerrainFeatureData();

            Debug.Log("自然地形特徴生成開始...");
            var totalStartTime = System.DateTime.Now;

            try
            {
                // 1. 山脈システムの生成（最初に実行して地形の骨格を作る）
                if (enableMountainGeneration)
                {
                    var mountainStartTime = System.DateTime.Now;
                    featureData.mountainRanges = GenerateMountainRanges(heightmap, resolution, tileSize);
                    var mountainTime = (System.DateTime.Now - mountainStartTime).TotalMilliseconds;
                    Debug.Log($"山脈生成完了: {featureData.mountainRanges.Count}個, {mountainTime:F2}ms");
                }

                // 2. 谷システムの生成（山脈に基づいて谷を作る）
                if (enableValleyGeneration && featureData.mountainRanges.Count > 0)
                {
                    var valleyStartTime = System.DateTime.Now;
                    GenerateValleys(heightmap, resolution, tileSize, featureData.mountainRanges);
                    var valleyTime = (System.DateTime.Now - valleyStartTime).TotalMilliseconds;
                    Debug.Log($"谷生成完了: {valleyTime:F2}ms");
                }

                // 3. 断層線の生成（地質学的特徴を追加）
                if (enableFaultLines)
                {
                    var faultStartTime = System.DateTime.Now;
                    GenerateFaultLines(heightmap, resolution, tileSize);
                    var faultTime = (System.DateTime.Now - faultStartTime).TotalMilliseconds;
                    Debug.Log($"断層線生成完了: {faultTime:F2}ms");
                }

                // 4. 河川システムの生成（最後に実行して水の流れを作る）
                if (enableRiverGeneration)
                {
                    var riverStartTime = System.DateTime.Now;
                    featureData.riverSystems = GenerateRiverSystems(heightmap, resolution, tileSize);
                    var riverTime = (System.DateTime.Now - riverStartTime).TotalMilliseconds;
                    Debug.Log($"河川生成完了: {featureData.riverSystems.Count}個, {riverTime:F2}ms");
                }

                // 5. 地形の最終調整（安息角の適用など）
                var adjustmentStartTime = System.DateTime.Now;
                ApplyFinalTerrainAdjustments(heightmap, resolution);
                var adjustmentTime = (System.DateTime.Now - adjustmentStartTime).TotalMilliseconds;
                Debug.Log($"地形調整完了: {adjustmentTime:F2}ms");

                var totalTime = (System.DateTime.Now - totalStartTime).TotalMilliseconds;
                Debug.Log($"自然地形特徴生成完了 - 総時間: {totalTime:F2}ms");

                // 生成結果の統計情報を記録
                featureData.generationStats = new TerrainGenerationStats
                {
                    totalGenerationTime = (float)totalTime,
                    riverCount = featureData.riverSystems?.Count ?? 0,
                    mountainRangeCount = featureData.mountainRanges?.Count ?? 0,
                    resolution = resolution,
                    tileSize = tileSize
                };

                return featureData;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"自然地形特徴生成中にエラーが発生しました: {e.Message}");
                Debug.LogError($"スタックトレース: {e.StackTrace}");

                // エラー時でも基本的なデータ構造を返す
                return featureData ?? new TerrainFeatureData();
            }
        }

        /// <summary>
        /// 地形の最終調整
        /// </summary>
        private void ApplyFinalTerrainAdjustments(float[,] heightmap, int resolution)
        {
            // 安息角の適用
            ApplyReposeAngle(heightmap, resolution);

            // 地形の平滑化（急激な変化を緩和）
            ApplyTerrainSmoothing(heightmap, resolution);

            // 地形の連続性確保
            EnsureTerrainContinuity(heightmap, resolution);
        }

        /// <summary>
        /// 地形の平滑化
        /// </summary>
        private void ApplyTerrainSmoothing(float[,] heightmap, int resolution)
        {
            float[,] smoothedMap = new float[resolution, resolution];
            int kernelSize = 3;
            float[,] kernel = GenerateGaussianKernel(kernelSize, 1.0f);

            for (int x = kernelSize / 2; x < resolution - kernelSize / 2; x++)
            {
                for (int y = kernelSize / 2; y < resolution - kernelSize / 2; y++)
                {
                    float smoothedValue = 0f;

                    for (int kx = 0; kx < kernelSize; kx++)
                    {
                        for (int ky = 0; ky < kernelSize; ky++)
                        {
                            int mapX = x - kernelSize / 2 + kx;
                            int mapY = y - kernelSize / 2 + ky;
                            smoothedValue += heightmap[mapX, mapY] * kernel[kx, ky];
                        }
                    }

                    smoothedMap[x, y] = smoothedValue;
                }
            }

            // 境界部分は元の値を保持
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    if (x < kernelSize / 2 || x >= resolution - kernelSize / 2 ||
                        y < kernelSize / 2 || y >= resolution - kernelSize / 2)
                    {
                        smoothedMap[x, y] = heightmap[x, y];
                    }
                }
            }

            // 結果を元のハイトマップにコピー
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    heightmap[x, y] = smoothedMap[x, y];
                }
            }
        }

        /// <summary>
        /// 地形の連続性確保
        /// </summary>
        private void EnsureTerrainContinuity(float[,] heightmap, int resolution)
        {
            // 極端な高度差を検出して修正
            float maxAllowedDifference = mountainHeight * 0.1f; // 最大高度の10%

            for (int x = 1; x < resolution - 1; x++)
            {
                for (int y = 1; y < resolution - 1; y++)
                {
                    float currentHeight = heightmap[x, y];
                    float averageNeighborHeight = 0f;
                    int neighborCount = 0;

                    // 8方向の隣接セルの平均高度を計算
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;

                            averageNeighborHeight += heightmap[x + dx, y + dy];
                            neighborCount++;
                        }
                    }

                    averageNeighborHeight /= neighborCount;

                    // 極端な差がある場合は調整
                    float difference = Mathf.Abs(currentHeight - averageNeighborHeight);
                    if (difference > maxAllowedDifference)
                    {
                        float adjustmentFactor = 0.3f; // 30%調整
                        heightmap[x, y] = Mathf.Lerp(currentHeight, averageNeighborHeight, adjustmentFactor);
                    }
                }
            }
        }
        #endregion

        private float[,] GenerateGaussianKernel(int size, float sigma)
        {
            float[,] kernel = new float[size, size];
            float sum = 0f;
            int halfSize = size / 2;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    int kernelX = x - halfSize;
                    int kernelY = y - halfSize;
                    float value = (1f / (2f * Mathf.PI * sigma * sigma)) * Mathf.Exp(-(kernelX * kernelX + kernelY * kernelY) / (2f * sigma * sigma));
                    kernel[x, y] = value;
                    sum += value;
                }
            }

            // 正規化
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    kernel[x, y] /= sum;
                }
            }

            return kernel;
        }
    } // class NaturalTerrainFeatures
} // namespace Vastcore.Generation
