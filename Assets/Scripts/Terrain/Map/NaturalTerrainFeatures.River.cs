using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    /// <summary>
    /// 河川生成システム
    /// </summary>
    public partial class NaturalTerrainFeatures
    {
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
    }
}
