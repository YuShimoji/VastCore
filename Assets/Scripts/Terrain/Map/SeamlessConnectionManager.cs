using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Generation
{
    /// <summary>
    /// シームレス境界接続システム
    /// 隣接する地形タイル間の境界を滑らかに接続する
    /// </summary>
    public static class SeamlessConnectionManager
    {
        #region データ構造
        /// <summary>
        /// 地形タイルの接続データ
        /// </summary>
        [System.Serializable]
        public struct ConnectionData
        {
            public Vector2Int tileCoordinate;       // タイル座標
            public float[,] edgeHeights;            // エッジの高さデータ
            public Vector3[] borderVertices;        // 境界頂点
            public EdgeDirection[] edgeDirections;  // エッジの方向
            public float tileSize;                  // タイルサイズ
            public int resolution;                  // 解像度
            
            public ConnectionData(Vector2Int coordinate, float[,] heightmap, float tileSize, int resolution)
            {
                this.tileCoordinate = coordinate;
                this.tileSize = tileSize;
                this.resolution = resolution;
                
                // エッジの高さデータを抽出
                this.edgeHeights = ExtractEdgeHeights(heightmap);
                
                // 境界頂点を計算
                this.borderVertices = CalculateBorderVertices(heightmap, tileSize, resolution);
                
                // エッジ方向を設定
                this.edgeDirections = new EdgeDirection[] { EdgeDirection.North, EdgeDirection.East, EdgeDirection.South, EdgeDirection.West };
            }
        }
        
        /// <summary>
        /// エッジの方向
        /// </summary>
        public enum EdgeDirection
        {
            North,  // 北（Y+）
            East,   // 東（X+）
            South,  // 南（Y-）
            West    // 西（X-）
        }
        
        /// <summary>
        /// ブレンド設定
        /// </summary>
        [System.Serializable]
        public struct BlendSettings
        {
            [Header("ブレンド基本設定")]
            public float blendDistance;             // ブレンド距離（ワールド単位）
            public AnimationCurve blendCurve;       // ブレンドカーブ
            public float blendStrength;             // ブレンド強度
            
            [Header("高度補間設定")]
            public InterpolationType interpolationType;  // 補間タイプ
            public bool preserveFeatures;          // 地形特徴を保持するか
            public float featureThreshold;         // 特徴保持の閾値
            
            [Header("品質設定")]
            public int smoothingIterations;        // 平滑化反復回数
            public float smoothingStrength;        // 平滑化強度
            public bool enableNormalSmoothing;     // 法線平滑化を有効にするか
            
            public static BlendSettings Default()
            {
                return new BlendSettings
                {
                    blendDistance = 100f,
                    blendCurve = AnimationCurve.EaseInOut(0, 0, 1, 1),
                    blendStrength = 1f,
                    interpolationType = InterpolationType.CubicSpline,
                    preserveFeatures = true,
                    featureThreshold = 0.1f,
                    smoothingIterations = 3,
                    smoothingStrength = 0.5f,
                    enableNormalSmoothing = true
                };
            }
        }
        
        /// <summary>
        /// 補間タイプ
        /// </summary>
        public enum InterpolationType
        {
            Linear,         // 線形補間
            Cubic,          // 3次補間
            CubicSpline,    // 3次スプライン補間
            Hermite,        // エルミート補間
            Bezier          // ベジエ補間
        }
        #endregion

        #region メイン接続関数
        /// <summary>
        /// シームレス接続を適用する
        /// </summary>
        public static float[,] ApplySeamlessConnection(float[,] currentHeightmap, List<ConnectionData> neighborData, BlendSettings settings)
        {
            if (neighborData == null || neighborData.Count == 0)
            {
                return currentHeightmap;
            }
            
            int resolution = currentHeightmap.GetLength(0);
            float[,] blendedHeightmap = (float[,])currentHeightmap.Clone();
            
            // 各隣接タイルとの境界をブレンド
            foreach (var neighbor in neighborData)
            {
                blendedHeightmap = BlendWithNeighbor(blendedHeightmap, neighbor, settings);
            }
            
            // 後処理
            if (settings.smoothingIterations > 0)
            {
                blendedHeightmap = ApplySmoothing(blendedHeightmap, settings);
            }
            
            return blendedHeightmap;
        }
        
        /// <summary>
        /// 隣接タイルとの境界をブレンド
        /// </summary>
        private static float[,] BlendWithNeighbor(float[,] heightmap, ConnectionData neighborData, BlendSettings settings)
        {
            int resolution = heightmap.GetLength(0);
            float[,] result = (float[,])heightmap.Clone();
            
            // 隣接方向を判定
            var direction = DetermineNeighborDirection(neighborData.tileCoordinate);
            
            // 境界領域を特定
            var blendRegion = CalculateBlendRegion(resolution, direction, settings.blendDistance, neighborData.tileSize);
            
            // エッジ高さ値を補間
            InterpolateEdgeHeights(result, neighborData, blendRegion, settings);
            
            return result;
        }
        #endregion

        #region エッジ処理
        /// <summary>
        /// エッジの高さデータを抽出
        /// </summary>
        private static float[,] ExtractEdgeHeights(float[,] heightmap)
        {
            int resolution = heightmap.GetLength(0);
            float[,] edgeHeights = new float[4, resolution]; // 4方向 × 解像度
            
            // 北エッジ（上端）
            for (int x = 0; x < resolution; x++)
            {
                edgeHeights[0, x] = heightmap[resolution - 1, x];
            }
            
            // 東エッジ（右端）
            for (int y = 0; y < resolution; y++)
            {
                edgeHeights[1, y] = heightmap[y, resolution - 1];
            }
            
            // 南エッジ（下端）
            for (int x = 0; x < resolution; x++)
            {
                edgeHeights[2, x] = heightmap[0, x];
            }
            
            // 西エッジ（左端）
            for (int y = 0; y < resolution; y++)
            {
                edgeHeights[3, y] = heightmap[y, 0];
            }
            
            return edgeHeights;
        }
        
        /// <summary>
        /// 境界頂点を計算
        /// </summary>
        private static Vector3[] CalculateBorderVertices(float[,] heightmap, float tileSize, int resolution)
        {
            List<Vector3> vertices = new List<Vector3>();
            float stepSize = tileSize / (resolution - 1);
            
            // 4つのエッジの頂点を計算
            for (int edge = 0; edge < 4; edge++)
            {
                for (int i = 0; i < resolution; i++)
                {
                    Vector3 vertex = Vector3.zero;
                    float height = 0f;
                    
                    switch (edge)
                    {
                        case 0: // 北エッジ
                            vertex.x = i * stepSize - tileSize * 0.5f;
                            vertex.z = tileSize * 0.5f;
                            height = heightmap[resolution - 1, i];
                            break;
                        case 1: // 東エッジ
                            vertex.x = tileSize * 0.5f;
                            vertex.z = i * stepSize - tileSize * 0.5f;
                            height = heightmap[i, resolution - 1];
                            break;
                        case 2: // 南エッジ
                            vertex.x = i * stepSize - tileSize * 0.5f;
                            vertex.z = -tileSize * 0.5f;
                            height = heightmap[0, i];
                            break;
                        case 3: // 西エッジ
                            vertex.x = -tileSize * 0.5f;
                            vertex.z = i * stepSize - tileSize * 0.5f;
                            height = heightmap[i, 0];
                            break;
                    }
                    
                    vertex.y = height;
                    vertices.Add(vertex);
                }
            }
            
            return vertices.ToArray();
        }
        #endregion

        #region 補間処理
        /// <summary>
        /// エッジ高さ値を補間
        /// </summary>
        private static void InterpolateEdgeHeights(float[,] heightmap, ConnectionData neighborData, BlendRegion blendRegion, BlendSettings settings)
        {
            int resolution = heightmap.GetLength(0);
            
            for (int y = blendRegion.minY; y <= blendRegion.maxY; y++)
            {
                for (int x = blendRegion.minX; x <= blendRegion.maxX; x++)
                {
                    if (x < 0 || x >= resolution || y < 0 || y >= resolution)
                        continue;
                    
                    // ブレンド係数を計算
                    float blendFactor = CalculateBlendFactor(x, y, blendRegion, settings);
                    
                    if (blendFactor > 0f)
                    {
                        // 隣接タイルからの高さ値を取得
                        float neighborHeight = GetNeighborHeight(x, y, neighborData, blendRegion.direction);
                        
                        // 補間を適用
                        float currentHeight = heightmap[y, x];
                        float interpolatedHeight = InterpolateHeight(currentHeight, neighborHeight, blendFactor, settings);
                        
                        heightmap[y, x] = interpolatedHeight;
                    }
                }
            }
        }
        
        /// <summary>
        /// ブレンド係数を計算
        /// </summary>
        private static float CalculateBlendFactor(int x, int y, BlendRegion region, BlendSettings settings)
        {
            float distance = 0f;
            
            switch (region.direction)
            {
                case EdgeDirection.North:
                    distance = region.maxY - y;
                    break;
                case EdgeDirection.East:
                    distance = region.maxX - x;
                    break;
                case EdgeDirection.South:
                    distance = y - region.minY;
                    break;
                case EdgeDirection.West:
                    distance = x - region.minX;
                    break;
            }
            
            float normalizedDistance = distance / region.blendWidth;
            normalizedDistance = Mathf.Clamp01(normalizedDistance);
            
            return settings.blendCurve.Evaluate(normalizedDistance) * settings.blendStrength;
        }
        
        /// <summary>
        /// 高さ値を補間
        /// </summary>
        private static float InterpolateHeight(float currentHeight, float neighborHeight, float blendFactor, BlendSettings settings)
        {
            switch (settings.interpolationType)
            {
                case InterpolationType.Linear:
                    return Mathf.Lerp(currentHeight, neighborHeight, blendFactor);
                
                case InterpolationType.Cubic:
                    return CubicInterpolation(currentHeight, neighborHeight, blendFactor);
                
                case InterpolationType.CubicSpline:
                    return CubicSplineInterpolation(currentHeight, neighborHeight, blendFactor);
                
                case InterpolationType.Hermite:
                    return HermiteInterpolation(currentHeight, neighborHeight, blendFactor);
                
                case InterpolationType.Bezier:
                    return BezierInterpolation(currentHeight, neighborHeight, blendFactor);
                
                default:
                    return Mathf.Lerp(currentHeight, neighborHeight, blendFactor);
            }
        }
        #endregion

        #region 補間アルゴリズム
        /// <summary>
        /// 3次補間
        /// </summary>
        private static float CubicInterpolation(float a, float b, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return a * (2 * t3 - 3 * t2 + 1) + b * (3 * t2 - 2 * t3);
        }
        
        /// <summary>
        /// 3次スプライン補間
        /// </summary>
        private static float CubicSplineInterpolation(float a, float b, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            
            // 簡易版3次スプライン
            float h00 = 2 * t3 - 3 * t2 + 1;
            float h10 = t3 - 2 * t2 + t;
            float h01 = -2 * t3 + 3 * t2;
            float h11 = t3 - t2;
            
            return a * h00 + b * h01;
        }
        
        /// <summary>
        /// エルミート補間
        /// </summary>
        private static float HermiteInterpolation(float a, float b, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            
            float h1 = 2 * t3 - 3 * t2 + 1;
            float h2 = -2 * t3 + 3 * t2;
            
            return a * h1 + b * h2;
        }
        
        /// <summary>
        /// ベジエ補間
        /// </summary>
        private static float BezierInterpolation(float a, float b, float t)
        {
            float invT = 1 - t;
            return invT * invT * invT * a + 3 * invT * invT * t * a + 3 * invT * t * t * b + t * t * t * b;
        }
        #endregion

        #region ユーティリティ関数
        /// <summary>
        /// 隣接方向を判定
        /// </summary>
        private static EdgeDirection DetermineNeighborDirection(Vector2Int neighborCoordinate)
        {
            // 簡易実装：座標差から方向を判定
            if (neighborCoordinate.y > 0) return EdgeDirection.North;
            if (neighborCoordinate.x > 0) return EdgeDirection.East;
            if (neighborCoordinate.y < 0) return EdgeDirection.South;
            return EdgeDirection.West;
        }
        
        /// <summary>
        /// ブレンド領域を計算
        /// </summary>
        private static BlendRegion CalculateBlendRegion(int resolution, EdgeDirection direction, float blendDistance, float tileSize)
        {
            float pixelsPerUnit = resolution / tileSize;
            int blendWidth = Mathf.RoundToInt(blendDistance * pixelsPerUnit);
            
            BlendRegion region = new BlendRegion();
            region.direction = direction;
            region.blendWidth = blendWidth;
            
            switch (direction)
            {
                case EdgeDirection.North:
                    region.minX = 0;
                    region.maxX = resolution - 1;
                    region.minY = resolution - blendWidth;
                    region.maxY = resolution - 1;
                    break;
                case EdgeDirection.East:
                    region.minX = resolution - blendWidth;
                    region.maxX = resolution - 1;
                    region.minY = 0;
                    region.maxY = resolution - 1;
                    break;
                case EdgeDirection.South:
                    region.minX = 0;
                    region.maxX = resolution - 1;
                    region.minY = 0;
                    region.maxY = blendWidth - 1;
                    break;
                case EdgeDirection.West:
                    region.minX = 0;
                    region.maxX = blendWidth - 1;
                    region.minY = 0;
                    region.maxY = resolution - 1;
                    break;
            }
            
            return region;
        }
        
        /// <summary>
        /// 隣接タイルからの高さ値を取得
        /// </summary>
        private static float GetNeighborHeight(int x, int y, ConnectionData neighborData, EdgeDirection direction)
        {
            int resolution = neighborData.resolution;
            
            // 対応する隣接タイルの座標を計算
            int neighborX = x;
            int neighborY = y;
            
            switch (direction)
            {
                case EdgeDirection.North:
                    neighborY = 0; // 隣接タイルの南端
                    break;
                case EdgeDirection.East:
                    neighborX = 0; // 隣接タイルの西端
                    break;
                case EdgeDirection.South:
                    neighborY = resolution - 1; // 隣接タイルの北端
                    break;
                case EdgeDirection.West:
                    neighborX = resolution - 1; // 隣接タイルの東端
                    break;
            }
            
            // エッジデータから高さを取得
            int edgeIndex = (int)direction;
            int dataIndex = (direction == EdgeDirection.North || direction == EdgeDirection.South) ? neighborX : neighborY;
            
            if (dataIndex >= 0 && dataIndex < neighborData.edgeHeights.GetLength(1))
            {
                return neighborData.edgeHeights[edgeIndex, dataIndex];
            }
            
            return 0f;
        }
        
        /// <summary>
        /// 平滑化を適用
        /// </summary>
        private static float[,] ApplySmoothing(float[,] heightmap, BlendSettings settings)
        {
            int resolution = heightmap.GetLength(0);
            float[,] smoothed = (float[,])heightmap.Clone();
            
            for (int iteration = 0; iteration < settings.smoothingIterations; iteration++)
            {
                float[,] temp = (float[,])smoothed.Clone();
                
                for (int y = 1; y < resolution - 1; y++)
                {
                    for (int x = 1; x < resolution - 1; x++)
                    {
                        float sum = 0f;
                        int count = 0;
                        
                        // 3x3カーネルで平均化
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                sum += temp[y + dy, x + dx];
                                count++;
                            }
                        }
                        
                        float average = sum / count;
                        float original = temp[y, x];
                        
                        smoothed[y, x] = Mathf.Lerp(original, average, settings.smoothingStrength);
                    }
                }
            }
            
            return smoothed;
        }
        
        /// <summary>
        /// 接続データを作成
        /// </summary>
        public static ConnectionData CreateConnectionData(Vector2Int coordinate, float[,] heightmap, float tileSize)
        {
            int resolution = heightmap.GetLength(0);
            return new ConnectionData(coordinate, heightmap, tileSize, resolution);
        }
        
        /// <summary>
        /// 複数の隣接タイルとの接続を処理
        /// </summary>
        public static float[,] ProcessMultipleConnections(float[,] heightmap, Dictionary<Vector2Int, float[,]> neighborHeightmaps, float tileSize, BlendSettings settings)
        {
            List<ConnectionData> connectionDataList = new List<ConnectionData>();
            
            foreach (var kvp in neighborHeightmaps)
            {
                var connectionData = CreateConnectionData(kvp.Key, kvp.Value, tileSize);
                connectionDataList.Add(connectionData);
            }
            
            return ApplySeamlessConnection(heightmap, connectionDataList, settings);
        }
        #endregion

        #region 内部データ構造
        /// <summary>
        /// ブレンド領域
        /// </summary>
        private struct BlendRegion
        {
            public EdgeDirection direction;
            public int minX, maxX, minY, maxY;
            public int blendWidth;
        }
        #endregion
    }
}