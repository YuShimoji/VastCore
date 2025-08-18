using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    /// <summary>
    /// 円形地形生成システム
    /// 既存のMeshGeneratorを拡張し、円形フォールオフ機能を強化
    /// </summary>
    public static class CircularTerrainGenerator
    {
        #region 円形地形専用パラメータ
        [System.Serializable]
        public struct CircularTerrainParams
        {
            [Header("円形地形基本設定")]
            public float radius;                    // 円形地形の半径
            public Vector2 center;                  // 円形地形の中心座標
            public float falloffStrength;           // フォールオフの強度
            public AnimationCurve falloffCurve;     // フォールオフカーブ
            
            [Header("円形マスク設定")]
            public bool useHardEdge;                // ハードエッジを使用するか
            public float edgeSharpness;             // エッジの鋭さ
            public float innerRadius;               // 内側の半径（フルハイト領域）
            public float outerRadius;               // 外側の半径（フォールオフ開始）
            
            [Header("高度調整")]
            public float baseHeight;                // 基準高度
            public float heightVariation;           // 高度のバリエーション
            public bool preserveOriginalHeight;     // 元の高度を保持するか
            
            [Header("境界ブレンド")]
            public bool enableBoundaryBlend;        // 境界ブレンドを有効にするか
            public float blendDistance;             // ブレンド距離
            public AnimationCurve blendCurve;       // ブレンドカーブ
            
            public static CircularTerrainParams Default()
            {
                return new CircularTerrainParams
                {
                    radius = 1000f,
                    center = Vector2.zero,
                    falloffStrength = 1.5f,
                    falloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0),
                    useHardEdge = false,
                    edgeSharpness = 2f,
                    innerRadius = 800f,
                    outerRadius = 1000f,
                    baseHeight = 0f,
                    heightVariation = 1f,
                    preserveOriginalHeight = false,
                    enableBoundaryBlend = true,
                    blendDistance = 100f,
                    blendCurve = AnimationCurve.EaseInOut(0, 0, 1, 1)
                };
            }
        }
        #endregion

        #region メイン生成関数
        /// <summary>
        /// 円形地形を生成する
        /// 既存のMeshGenerator.GenerateAdvancedTerrainを拡張
        /// </summary>
        public static Mesh GenerateCircularTerrain(MeshGenerator.TerrainGenerationParams baseParams, CircularTerrainParams circularParams)
        {
            // 基本地形を生成
            var heightmap = MeshGenerator.GenerateHeightmap(baseParams);
            
            // 円形マスクを適用
            heightmap = ApplyCircularMask(heightmap, baseParams, circularParams);
            
            // 境界ブレンドを適用
            if (circularParams.enableBoundaryBlend)
            {
                heightmap = ApplyBoundaryBlend(heightmap, baseParams, circularParams);
            }
            
            // メッシュを生成
            return GenerateMeshFromHeightmap(heightmap, baseParams);
        }

        /// <summary>
        /// 円形地形を生成する（簡易版）
        /// </summary>
        public static Mesh GenerateCircularTerrain(MeshGenerator.TerrainGenerationParams parameters)
        {
            var circularParams = CircularTerrainParams.Default();
            circularParams.radius = parameters.radius;
            circularParams.falloffStrength = parameters.falloffStrength;
            circularParams.falloffCurve = parameters.falloffCurve;
            
            return GenerateCircularTerrain(parameters, circularParams);
        }
        #endregion

        #region 円形マスク処理
        /// <summary>
        /// 円形マスクを適用する
        /// 中心からの距離に基づいて高さを調整
        /// </summary>
        private static float[,] ApplyCircularMask(float[,] heightmap, MeshGenerator.TerrainGenerationParams baseParams, CircularTerrainParams circularParams)
        {
            int resolution = baseParams.resolution;
            float size = baseParams.size;
            
            // 中心座標を計算（ワールド座標系）
            Vector2 worldCenter = circularParams.center;
            Vector2 mapCenter = new Vector2(resolution * 0.5f, resolution * 0.5f);
            
            // ワールド座標からマップ座標への変換係数
            float worldToMapScale = resolution / size;
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    // 現在の位置（マップ座標）
                    Vector2 currentPos = new Vector2(x, y);
                    
                    // ワールド座標での位置
                    Vector2 worldPos = (currentPos - mapCenter) / worldToMapScale;
                    
                    // 中心からの距離を計算
                    float distanceFromCenter = Vector2.Distance(worldPos, worldCenter);
                    
                    // 円形マスクを適用
                    float maskValue = CalculateCircularMask(distanceFromCenter, circularParams);
                    
                    // 高度調整
                    float originalHeight = heightmap[y, x];
                    float adjustedHeight = ApplyHeightAdjustment(originalHeight, maskValue, circularParams);
                    
                    heightmap[y, x] = adjustedHeight;
                }
            }
            
            return heightmap;
        }

        /// <summary>
        /// 円形マスク値を計算する
        /// </summary>
        private static float CalculateCircularMask(float distance, CircularTerrainParams circularParams)
        {
            float maskValue = 1f;
            
            if (circularParams.useHardEdge)
            {
                // ハードエッジの場合
                if (distance <= circularParams.innerRadius)
                {
                    maskValue = 1f;
                }
                else if (distance >= circularParams.outerRadius)
                {
                    maskValue = 0f;
                }
                else
                {
                    // 内側と外側の間でフォールオフ
                    float normalizedDistance = (distance - circularParams.innerRadius) / 
                                             (circularParams.outerRadius - circularParams.innerRadius);
                    maskValue = 1f - Mathf.Pow(normalizedDistance, circularParams.edgeSharpness);
                }
            }
            else
            {
                // ソフトエッジの場合
                float normalizedDistance = distance / circularParams.radius;
                
                if (normalizedDistance <= 1f)
                {
                    // フォールオフカーブを適用
                    float curveValue = circularParams.falloffCurve.Evaluate(normalizedDistance);
                    maskValue = Mathf.Pow(curveValue, circularParams.falloffStrength);
                }
                else
                {
                    maskValue = 0f;
                }
            }
            
            return Mathf.Clamp01(maskValue);
        }

        /// <summary>
        /// 高度調整を適用する
        /// </summary>
        private static float ApplyHeightAdjustment(float originalHeight, float maskValue, CircularTerrainParams circularParams)
        {
            if (circularParams.preserveOriginalHeight)
            {
                // 元の高度を保持しつつマスクを適用
                return originalHeight * maskValue;
            }
            else
            {
                // 基準高度とバリエーションを考慮
                float adjustedHeight = circularParams.baseHeight + 
                                     (originalHeight * circularParams.heightVariation);
                return adjustedHeight * maskValue;
            }
        }
        #endregion

        #region 境界ブレンド処理
        /// <summary>
        /// 境界ブレンドを適用する
        /// 隣接する地形タイルとの滑らかな接続のため
        /// </summary>
        private static float[,] ApplyBoundaryBlend(float[,] heightmap, MeshGenerator.TerrainGenerationParams baseParams, CircularTerrainParams circularParams)
        {
            int resolution = baseParams.resolution;
            float size = baseParams.size;
            float blendDistance = circularParams.blendDistance;
            
            // ワールド座標からマップ座標への変換係数
            float worldToMapScale = resolution / size;
            float blendPixels = blendDistance * worldToMapScale;
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    // エッジからの距離を計算
                    float edgeDistance = Mathf.Min(
                        Mathf.Min(x, resolution - 1 - x),
                        Mathf.Min(y, resolution - 1 - y)
                    );
                    
                    if (edgeDistance < blendPixels)
                    {
                        // ブレンド係数を計算
                        float blendFactor = edgeDistance / blendPixels;
                        blendFactor = circularParams.blendCurve.Evaluate(blendFactor);
                        
                        // 現在の高度とベース高度をブレンド
                        float currentHeight = heightmap[y, x];
                        float baseHeight = circularParams.baseHeight;
                        
                        heightmap[y, x] = Mathf.Lerp(baseHeight, currentHeight, blendFactor);
                    }
                }
            }
            
            return heightmap;
        }
        #endregion

        #region ユーティリティ関数
        /// <summary>
        /// ハイトマップからメッシュを生成
        /// MeshGeneratorの機能を利用
        /// </summary>
        private static Mesh GenerateMeshFromHeightmap(float[,] heightmap, MeshGenerator.TerrainGenerationParams parameters)
        {
            // 既存のMeshGeneratorの機能を利用
            return MeshGenerator.GenerateMeshFromHeightmap(heightmap, parameters);
        }

        /// <summary>
        /// 円形地形の統計情報を取得
        /// </summary>
        public static CircularTerrainStats GetCircularTerrainStats(float[,] heightmap, CircularTerrainParams circularParams)
        {
            var baseStats = MeshGenerator.GetTerrainStats(heightmap);
            
            // 円形地形特有の統計を計算
            int resolution = heightmap.GetLength(0);
            int pixelsInCircle = 0;
            float totalHeightInCircle = 0f;
            
            Vector2 center = new Vector2(resolution * 0.5f, resolution * 0.5f);
            float radiusInPixels = circularParams.radius * resolution / 1000f; // 仮の変換
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float distance = Vector2.Distance(pos, center);
                    
                    if (distance <= radiusInPixels)
                    {
                        pixelsInCircle++;
                        totalHeightInCircle += heightmap[y, x];
                    }
                }
            }
            
            float averageHeightInCircle = pixelsInCircle > 0 ? totalHeightInCircle / pixelsInCircle : 0f;
            float circularArea = Mathf.PI * circularParams.radius * circularParams.radius;
            
            return new CircularTerrainStats
            {
                baseStats = baseStats,
                radius = circularParams.radius,
                circularArea = circularArea,
                pixelsInCircle = pixelsInCircle,
                averageHeightInCircle = averageHeightInCircle,
                effectiveRadius = radiusInPixels
            };
        }

        /// <summary>
        /// 距離に基づくフォールオフ値を計算
        /// </summary>
        public static float CalculateDistanceFalloff(Vector2 position, Vector2 center, float radius, AnimationCurve falloffCurve, float falloffStrength = 1f)
        {
            float distance = Vector2.Distance(position, center);
            float normalizedDistance = distance / radius;
            
            if (normalizedDistance >= 1f)
                return 0f;
            
            float curveValue = falloffCurve.Evaluate(normalizedDistance);
            return Mathf.Pow(curveValue, falloffStrength);
        }

        /// <summary>
        /// 円形地形の境界を検出
        /// </summary>
        public static List<Vector2> DetectCircularBoundary(float[,] heightmap, float threshold = 0.1f)
        {
            List<Vector2> boundaryPoints = new List<Vector2>();
            int resolution = heightmap.GetLength(0);
            
            for (int y = 1; y < resolution - 1; y++)
            {
                for (int x = 1; x < resolution - 1; x++)
                {
                    float currentHeight = heightmap[y, x];
                    
                    // 周囲の高度差をチェック
                    bool isBoundary = false;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            
                            float neighborHeight = heightmap[y + dy, x + dx];
                            if (Mathf.Abs(currentHeight - neighborHeight) > threshold)
                            {
                                isBoundary = true;
                                break;
                            }
                        }
                        if (isBoundary) break;
                    }
                    
                    if (isBoundary)
                    {
                        boundaryPoints.Add(new Vector2(x, y));
                    }
                }
            }
            
            return boundaryPoints;
        }
        #endregion

        #region データ構造
        [System.Serializable]
        public struct CircularTerrainStats
        {
            public MeshGenerator.TerrainStats baseStats;
            public float radius;
            public float circularArea;
            public int pixelsInCircle;
            public float averageHeightInCircle;
            public float effectiveRadius;
        }
        #endregion
    }
}