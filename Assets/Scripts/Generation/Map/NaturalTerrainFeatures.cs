using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Generation
{
    /// <summary>
    /// 自然地形特徴システム - 河川、山脈、谷の自動生成
    /// 要求1.1, 1.5: リアルな地形生成
    /// </summary>
    public class NaturalTerrainFeatures : MonoBehaviour
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
        #endregion

        #region 河川生成システム
        /// <summary>
        /// 河川システムの生成
        /// </summary>
        public List<RiverSystem> GenerateRiverSystems(float[,] heightmap, int resolution, float tileSize)
        {
            var rivers = new List<RiverSystem>();
            
            if (!enableRiverGeneration) return rivers;

            // 1. 流域の計算
            var watersheds = CalculateWatersheds(heightmap, resolution);
            
            // 2. 河川網の生成
            foreach (var watershed in watersheds)
            {
                if (watershed.area > minWatershedSize)
                {
                    var river = GenerateRiverFromWatershed(watershed, heightmap, resolution, tileSize);
                    if (river != null)
                    {
                        rivers.Add(river);
                    }
                }
            }

            // 3. 河川による浸食シミュレーション
            ApplyRiverErosion(heightmap, rivers, resolution);

            return rivers;
        }

        /// <summary>
        /// 流域の計算
        /// </summary>
        private List<Watershed> CalculateWatersheds(float[,] heightmap, int resolution)
        {
            var watersheds = new List<Watershed>();
            var flowDirection = CalculateFlowDirection(heightmap, resolution);
            var flowAccumulation = CalculateFlowAccumulation(flowDirection, resolution);
            
            // 流域の境界を特定
            var visited = new bool[resolution, resolution];
            
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    if (!visited[x, y] && flowAccumulation[x, y] > watershedThreshold)
                    {
                        var watershed = TraceWatershed(x, y, flowDirection, visited, resolution);
                        if (watershed.cells.Count > minWatershedSize)
                        {
                            watershed.totalFlow = flowAccumulation[x, y];
                            watersheds.Add(watershed);
                        }
                    }
                }
            }

            return watersheds;
        }

        /// <summary>
        /// 流向の計算（D8アルゴリズム）
        /// </summary>
        private Vector2Int[,] CalculateFlowDirection(float[,] heightmap, int resolution)
        {
            var flowDirection = new Vector2Int[resolution, resolution];
            var directions = new Vector2Int[]
            {
                new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1),
                new Vector2Int(-1, 0),                          new Vector2Int(1, 0),
                new Vector2Int(-1, 1),  new Vector2Int(0, 1),  new Vector2Int(1, 1)
            };

            for (int x = 1; x < resolution - 1; x++)
            {
                for (int y = 1; y < resolution - 1; y++)
                {
                    float currentHeight = heightmap[x, y];
                    float maxSlope = 0f;
                    Vector2Int steepestDirection = Vector2Int.zero;

                    foreach (var dir in directions)
                    {
                        int nx = x + dir.x;
                        int ny = y + dir.y;
                        
                        if (nx >= 0 && nx < resolution && ny >= 0 && ny < resolution)
                        {
                            float neighborHeight = heightmap[nx, ny];
                            float slope = (currentHeight - neighborHeight) / Mathf.Sqrt(dir.x * dir.x + dir.y * dir.y);
                            
                            if (slope > maxSlope)
                            {
                                maxSlope = slope;
                                steepestDirection = dir;
                            }
                        }
                    }

                    flowDirection[x, y] = steepestDirection;
                }
            }

            return flowDirection;
        }

        /// <summary>
        /// 流量累積の計算
        /// </summary>
        private float[,] CalculateFlowAccumulation(Vector2Int[,] flowDirection, int resolution)
        {
            var flowAccumulation = new float[resolution, resolution];
            var processed = new bool[resolution, resolution];

            // 初期化：各セルの流量を1に設定
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    flowAccumulation[x, y] = 1f;
                }
            }

            // トポロジカルソートによる流量累積計算
            var stack = new Stack<Vector2Int>();
            
            // 境界セルから開始
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    if (x == 0 || x == resolution - 1 || y == 0 || y == resolution - 1)
                    {
                        stack.Push(new Vector2Int(x, y));
                    }
                }
            }

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (processed[current.x, current.y]) continue;

                var flowDir = flowDirection[current.x, current.y];
                if (flowDir != Vector2Int.zero)
                {
                    int nx = current.x + flowDir.x;
                    int ny = current.y + flowDir.y;
                    
                    if (nx >= 0 && nx < resolution && ny >= 0 && ny < resolution)
                    {
                        flowAccumulation[nx, ny] += flowAccumulation[current.x, current.y];
                        if (!processed[nx, ny])
                        {
                            stack.Push(new Vector2Int(nx, ny));
                        }
                    }
                }

                processed[current.x, current.y] = true;
            }

            return flowAccumulation;
        }

        /// <summary>
        /// 流域の追跡
        /// </summary>
        private Watershed TraceWatershed(int startX, int startY, Vector2Int[,] flowDirection, bool[,] visited, int resolution)
        {
            var watershed = new Watershed
            {
                cells = new List<Vector2Int>(),
                outlet = new Vector2Int(startX, startY)
            };

            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(startX, startY));

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (visited[current.x, current.y]) continue;

                visited[current.x, current.y] = true;
                watershed.cells.Add(current);

                // 隣接セルをチェック
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;

                        int nx = current.x + dx;
                        int ny = current.y + dy;

                        if (nx >= 0 && nx < resolution && ny >= 0 && ny < resolution && !visited[nx, ny])
                        {
                            var neighborFlow = flowDirection[nx, ny];
                            if (neighborFlow.x == -dx && neighborFlow.y == -dy)
                            {
                                queue.Enqueue(new Vector2Int(nx, ny));
                            }
                        }
                    }
                }
            }

            watershed.area = watershed.cells.Count;
            return watershed;
        }

        /// <summary>
        /// 流域から河川を生成
        /// </summary>
        private RiverSystem GenerateRiverFromWatershed(Watershed watershed, float[,] heightmap, int resolution, float tileSize)
        {
            var river = new RiverSystem
            {
                riverPath = new List<Vector3>(),
                tributaries = new List<Vector3>(),
                watershed = watershed
            };

            // 河川経路の生成
            var currentCell = watershed.outlet;
            float cellSize = tileSize / resolution;

            while (true)
            {
                Vector3 worldPos = new Vector3(
                    currentCell.x * cellSize - tileSize * 0.5f,
                    heightmap[currentCell.x, currentCell.y],
                    currentCell.y * cellSize - tileSize * 0.5f
                );

                river.riverPath.Add(worldPos);

                // 次のセルを見つける（最も低い隣接セル）
                Vector2Int nextCell = FindLowestNeighbor(currentCell, heightmap, resolution);
                if (nextCell == currentCell) break; // 出口に到達

                currentCell = nextCell;
            }

            // 河川の幅と深さを計算
            river.flow = watershed.totalFlow;
            river.width = Mathf.Lerp(riverWidth * 0.5f, riverWidth, river.flow / 1000f);
            river.depth = Mathf.Lerp(riverDepth * 0.5f, riverDepth, river.flow / 1000f);

            return river;
        }

        /// <summary>
        /// 最も低い隣接セルを見つける
        /// </summary>
        private Vector2Int FindLowestNeighbor(Vector2Int current, float[,] heightmap, int resolution)
        {
            Vector2Int lowest = current;
            float lowestHeight = heightmap[current.x, current.y];

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int nx = current.x + dx;
                    int ny = current.y + dy;

                    if (nx >= 0 && nx < resolution && ny >= 0 && ny < resolution)
                    {
                        float neighborHeight = heightmap[nx, ny];
                        if (neighborHeight < lowestHeight)
                        {
                            lowestHeight = neighborHeight;
                            lowest = new Vector2Int(nx, ny);
                        }
                    }
                }
            }

            return lowest;
        }

        /// <summary>
        /// 河川による浸食の適用
        /// </summary>
        private void ApplyRiverErosion(float[,] heightmap, List<RiverSystem> rivers, int resolution)
        {
            foreach (var river in rivers)
            {
                for (int i = 0; i < river.riverPath.Count; i++)
                {
                    Vector3 riverPoint = river.riverPath[i];
                    
                    // ワールド座標からハイトマップ座標に変換
                    int x = Mathf.RoundToInt((riverPoint.x + resolution * 0.5f));
                    int y = Mathf.RoundToInt((riverPoint.z + resolution * 0.5f));

                    if (x >= 0 && x < resolution && y >= 0 && y < resolution)
                    {
                        // 河川の浸食効果を適用
                        float erosionAmount = erosionStrength * river.flow * 0.001f;
                        heightmap[x, y] -= erosionAmount;

                        // 周辺への影響
                        int radius = Mathf.RoundToInt(river.width * 0.1f);
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            for (int dy = -radius; dy <= radius; dy++)
                            {
                                int nx = x + dx;
                                int ny = y + dy;

                                if (nx >= 0 && nx < resolution && ny >= 0 && ny < resolution)
                                {
                                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                                    if (distance <= radius)
                                    {
                                        float influence = 1f - (distance / radius);
                                        heightmap[nx, ny] -= erosionAmount * influence * 0.5f;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region 山脈・谷生成システム
        /// <summary>
        /// 山脈システムの生成
        /// </summary>
        public List<MountainRange> GenerateMountainRanges(float[,] heightmap, int resolution, float tileSize)
        {
            var mountainRanges = new List<MountainRange>();
            
            if (!enableMountainGeneration) return mountainRanges;

            // 1. 地質学的プロセスに基づく山脈形成
            for (int i = 0; i < maxMountainRanges; i++)
            {
                var mountainRange = GenerateMountainRange(heightmap, resolution, tileSize, i);
                if (mountainRange != null)
                {
                    mountainRanges.Add(mountainRange);
                    ApplyMountainToHeightmap(heightmap, mountainRange, resolution, tileSize);
                }
            }

            // 2. 安息角の適用
            ApplyReposeAngle(heightmap, resolution);

            return mountainRanges;
        }

        /// <summary>
        /// 単一山脈の生成
        /// </summary>
        private MountainRange GenerateMountainRange(float[,] heightmap, int resolution, float tileSize, int rangeIndex)
        {
            var mountainRange = new MountainRange
            {
                ridgeLine = new List<Vector3>(),
                peaks = new List<Vector3>(),
                formation = (GeologicalFormation)Random.Range(0, 4)
            };

            // 山脈の方向をランダムに決定
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            // 山脈の開始点と終点を決定
            Vector2 center = new Vector2(resolution * 0.5f, resolution * 0.5f);
            Vector2 offset = new Vector2(Random.Range(-resolution * 0.3f, resolution * 0.3f), 
                                       Random.Range(-resolution * 0.3f, resolution * 0.3f));
            Vector2 start = center + offset - direction * resolution * 0.4f;
            Vector2 end = center + offset + direction * resolution * 0.4f;

            // 尾根線の生成
            int ridgePoints = 20;
            for (int i = 0; i <= ridgePoints; i++)
            {
                float t = (float)i / ridgePoints;
                Vector2 ridgePoint = Vector2.Lerp(start, end, t);

                // ノイズによる自然な変動
                float noiseX = Mathf.PerlinNoise(ridgePoint.x * 0.01f, ridgePoint.y * 0.01f + rangeIndex) - 0.5f;
                float noiseY = Mathf.PerlinNoise(ridgePoint.x * 0.01f + rangeIndex, ridgePoint.y * 0.01f) - 0.5f;
                ridgePoint += new Vector2(noiseX, noiseY) * resolution * 0.1f;

                // 境界チェック
                ridgePoint.x = Mathf.Clamp(ridgePoint.x, 1, resolution - 2);
                ridgePoint.y = Mathf.Clamp(ridgePoint.y, 1, resolution - 2);

                // 高度計算
                float baseHeight = heightmap[(int)ridgePoint.x, (int)ridgePoint.y];
                float mountainHeightMultiplier = mountainProfile.Evaluate(Mathf.Abs(t - 0.5f) * 2f);
                float ridgeHeight = baseHeight + mountainHeight * mountainHeightMultiplier;

                Vector3 worldPos = new Vector3(
                    ridgePoint.x * tileSize / resolution - tileSize * 0.5f,
                    ridgeHeight,
                    ridgePoint.y * tileSize / resolution - tileSize * 0.5f
                );

                mountainRange.ridgeLine.Add(worldPos);

                // ピークの判定
                if (mountainHeightMultiplier > 0.8f)
                {
                    mountainRange.peaks.Add(worldPos);
                }
            }

            // 最大標高の計算
            mountainRange.maxElevation = mountainRange.ridgeLine.Max(p => p.y);

            // 平均傾斜の計算
            mountainRange.averageSlope = CalculateAverageSlope(mountainRange.ridgeLine);

            return mountainRange;
        }

        /// <summary>
        /// 山脈をハイトマップに適用
        /// </summary>
        private void ApplyMountainToHeightmap(float[,] heightmap, MountainRange mountainRange, int resolution, float tileSize)
        {
            float cellSize = tileSize / resolution;

            foreach (var ridgePoint in mountainRange.ridgeLine)
            {
                // ワールド座標からハイトマップ座標に変換
                int centerX = Mathf.RoundToInt((ridgePoint.x + tileSize * 0.5f) / cellSize);
                int centerY = Mathf.RoundToInt((ridgePoint.z + tileSize * 0.5f) / cellSize);

                if (centerX >= 0 && centerX < resolution && centerY >= 0 && centerY < resolution)
                {
                    float targetHeight = ridgePoint.y;
                    int influenceRadius = Mathf.RoundToInt(mountainHeight * 0.5f / cellSize);

                    // 山脈の影響範囲に高度を適用
                    for (int dx = -influenceRadius; dx <= influenceRadius; dx++)
                    {
                        for (int dy = -influenceRadius; dy <= influenceRadius; dy++)
                        {
                            int x = centerX + dx;
                            int y = centerY + dy;

                            if (x >= 0 && x < resolution && y >= 0 && y < resolution)
                            {
                                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                                if (distance <= influenceRadius)
                                {
                                    float influence = 1f - (distance / influenceRadius);
                                    influence = Mathf.Pow(influence, ridgeSharpness);

                                    float currentHeight = heightmap[x, y];
                                    float newHeight = Mathf.Lerp(currentHeight, targetHeight, influence);
                                    heightmap[x, y] = Mathf.Max(heightmap[x, y], newHeight);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 谷システムの生成
        /// </summary>
        public void GenerateValleys(float[,] heightmap, int resolution, float tileSize, List<MountainRange> mountainRanges)
        {
            if (!enableValleyGeneration) return;

            foreach (var mountainRange in mountainRanges)
            {
                GenerateValleysForMountainRange(heightmap, resolution, tileSize, mountainRange);
            }
        }

        /// <summary>
        /// 山脈に対応する谷の生成
        /// </summary>
        private void GenerateValleysForMountainRange(float[,] heightmap, int resolution, float tileSize, MountainRange mountainRange)
        {
            float cellSize = tileSize / resolution;

            for (int i = 0; i < mountainRange.ridgeLine.Count - 1; i++)
            {
                Vector3 ridgeStart = mountainRange.ridgeLine[i];
                Vector3 ridgeEnd = mountainRange.ridgeLine[i + 1];

                // 尾根線に垂直な方向に谷を生成
                Vector3 ridgeDirection = (ridgeEnd - ridgeStart).normalized;
                Vector3 valleyDirection = new Vector3(-ridgeDirection.z, 0, ridgeDirection.x);

                // 両側に谷を生成
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 valleyStart = (ridgeStart + ridgeEnd) * 0.5f;
                    Vector3 valleyEnd = valleyStart + valleyDirection * side * valleyWidth;

                    GenerateValleyPath(heightmap, resolution, tileSize, valleyStart, valleyEnd);
                }
            }
        }

        /// <summary>
        /// 谷の経路生成
        /// </summary>
        private void GenerateValleyPath(float[,] heightmap, int resolution, float tileSize, Vector3 start, Vector3 end)
        {
            float cellSize = tileSize / resolution;
            int steps = Mathf.RoundToInt(Vector3.Distance(start, end) / cellSize);

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector3 valleyPoint = Vector3.Lerp(start, end, t);

                int x = Mathf.RoundToInt((valleyPoint.x + tileSize * 0.5f) / cellSize);
                int y = Mathf.RoundToInt((valleyPoint.z + tileSize * 0.5f) / cellSize);

                if (x >= 0 && x < resolution && y >= 0 && y < resolution)
                {
                    float valleyInfluence = valleyProfile.Evaluate(t);
                    float depthReduction = valleyDepth * valleyInfluence;

                    int influenceRadius = Mathf.RoundToInt(valleyWidth * 0.5f / cellSize);

                    for (int dx = -influenceRadius; dx <= influenceRadius; dx++)
                    {
                        for (int dy = -influenceRadius; dy <= influenceRadius; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx >= 0 && nx < resolution && ny >= 0 && ny < resolution)
                            {
                                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                                if (distance <= influenceRadius)
                                {
                                    float influence = 1f - (distance / influenceRadius);
                                    influence = Mathf.Pow(influence, 2f); // より急峻な谷の形状

                                    heightmap[nx, ny] -= depthReduction * influence;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 安息角の適用
        /// </summary>
        private void ApplyReposeAngle(float[,] heightmap, int resolution)
        {
            float maxSlope = Mathf.Tan(repose_angle * Mathf.Deg2Rad);
            bool changed = true;
            int iterations = 0;
            int maxIterations = 10;

            while (changed && iterations < maxIterations)
            {
                changed = false;
                iterations++;

                for (int x = 1; x < resolution - 1; x++)
                {
                    for (int y = 1; y < resolution - 1; y++)
                    {
                        float currentHeight = heightmap[x, y];

                        // 8方向の隣接セルをチェック
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                if (dx == 0 && dy == 0) continue;

                                int nx = x + dx;
                                int ny = y + dy;
                                float neighborHeight = heightmap[nx, ny];

                                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                                float heightDiff = currentHeight - neighborHeight;
                                float slope = heightDiff / distance;

                                if (slope > maxSlope)
                                {
                                    // 安息角を超えている場合、高さを調整
                                    float maxHeightDiff = maxSlope * distance;
                                    float adjustment = (heightDiff - maxHeightDiff) * 0.5f;
                                    
                                    heightmap[x, y] -= adjustment;
                                    heightmap[nx, ny] += adjustment;
                                    changed = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 平均傾斜の計算
        /// </summary>
        private float CalculateAverageSlope(List<Vector3> ridgeLine)
        {
            if (ridgeLine.Count < 2) return 0f;

            float totalSlope = 0f;
            int slopeCount = 0;

            for (int i = 0; i < ridgeLine.Count - 1; i++)
            {
                Vector3 current = ridgeLine[i];
                Vector3 next = ridgeLine[i + 1];

                float horizontalDistance = Vector2.Distance(
                    new Vector2(current.x, current.z),
                    new Vector2(next.x, next.z)
                );

                if (horizontalDistance > 0)
                {
                    float verticalDistance = Mathf.Abs(next.y - current.y);
                    float slope = Mathf.Atan(verticalDistance / horizontalDistance) * Mathf.Rad2Deg;
                    totalSlope += slope;
                    slopeCount++;
                }
            }

            return slopeCount > 0 ? totalSlope / slopeCount : 0f;
        }

        /// <summary>
        /// 断層線の生成
        /// </summary>
        public void GenerateFaultLines(float[,] heightmap, int resolution, float tileSize)
        {
            if (!enableFaultLines) return;

            int faultCount = Mathf.RoundToInt(resolution * resolution * faultProbability * 0.0001f);

            for (int i = 0; i < faultCount; i++)
            {
                // ランダムな断層線の生成
                Vector2 faultStart = new Vector2(
                    Random.Range(resolution * 0.1f, resolution * 0.9f),
                    Random.Range(resolution * 0.1f, resolution * 0.9f)
                );

                float faultAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float faultLength = Random.Range(resolution * 0.1f, resolution * 0.3f);
                
                Vector2 faultEnd = faultStart + new Vector2(
                    Mathf.Cos(faultAngle) * faultLength,
                    Mathf.Sin(faultAngle) * faultLength
                );

                ApplyFaultLine(heightmap, resolution, faultStart, faultEnd);
            }
        }

        /// <summary>
        /// 断層線の適用
        /// </summary>
        private void ApplyFaultLine(float[,] heightmap, int resolution, Vector2 start, Vector2 end)
        {
            float displacement = Random.Range(-mountainHeight * 0.2f, mountainHeight * 0.2f);
            int steps = Mathf.RoundToInt(Vector2.Distance(start, end));

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                Vector2 faultPoint = Vector2.Lerp(start, end, t);

                int x = Mathf.RoundToInt(faultPoint.x);
                int y = Mathf.RoundToInt(faultPoint.y);

                if (x >= 0 && x < resolution && y >= 0 && y < resolution)
                {
                    // 断層の両側で高度を変更
                    Vector2 perpendicular = new Vector2(-(end.y - start.y), end.x - start.x).normalized;
                    
                    int influenceRadius = 5;
                    for (int dx = -influenceRadius; dx <= influenceRadius; dx++)
                    {
                        for (int dy = -influenceRadius; dy <= influenceRadius; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;

                            if (nx >= 0 && nx < resolution && ny >= 0 && ny < resolution)
                            {
                                Vector2 offset = new Vector2(dx, dy);
                                float side = Vector2.Dot(offset, perpendicular);
                                float distance = offset.magnitude;

                                if (distance <= influenceRadius)
                                {
                                    float influence = 1f - (distance / influenceRadius);
                                    float sideMultiplier = side > 0 ? 1f : -1f;
                                    
                                    heightmap[nx, ny] += displacement * sideMultiplier * influence;
                                }
                            }
                        }
                    }
                }
            }
        }

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
    }
}