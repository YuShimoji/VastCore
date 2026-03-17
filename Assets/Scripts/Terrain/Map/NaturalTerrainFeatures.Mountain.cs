using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Generation
{
    /// <summary>
    /// 山脈・谷・断層生成システム
    /// </summary>
    public partial class NaturalTerrainFeatures
    {
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
        #endregion
    }
}
