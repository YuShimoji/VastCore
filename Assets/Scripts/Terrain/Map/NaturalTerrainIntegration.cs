using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    /// <summary>
    /// 自然地形特徴システムと既存地形生成システムの統合
    /// 要求1.1, 1.5: リアルな地形生成の統合
    /// </summary>
    public class NaturalTerrainIntegration : MonoBehaviour
    {
        [Header("統合設定")]
        public bool enableNaturalFeatures = true;
        public bool applyToExistingTerrain = true;
        public float integrationStrength = 1f;

        [Header("システム参照")]
        public RuntimeTerrainManager terrainManager;
        public NaturalTerrainFeatures naturalFeatures;

        [Header("統合パラメータ")]
        [Range(0f, 1f)]
        public float riverInfluence = 0.8f;
        [Range(0f, 1f)]
        public float mountainInfluence = 0.9f;
        [Range(0f, 1f)]
        public float valleyInfluence = 0.7f;

        private Dictionary<Vector2Int, NaturalFeatureData> tileFeatures = new Dictionary<Vector2Int, NaturalFeatureData>();

        /// <summary>
        /// タイル毎の自然地形特徴データ
        /// </summary>
        [System.Serializable]
        public class NaturalFeatureData
        {
            public List<NaturalTerrainFeatures.RiverSystem> rivers;
            public List<NaturalTerrainFeatures.MountainRange> mountainRanges;
            public bool hasValleys;
            public float[,] modifiedHeightmap;
        }

        void Start()
        {
            InitializeIntegration();
        }

        /// <summary>
        /// 統合システムの初期化
        /// </summary>
        private void InitializeIntegration()
        {
            if (terrainManager == null)
            {
                terrainManager = FindObjectOfType<RuntimeTerrainManager>();
            }

            if (naturalFeatures == null)
            {
                naturalFeatures = GetComponent<NaturalTerrainFeatures>();
                if (naturalFeatures == null)
                {
                    naturalFeatures = gameObject.AddComponent<NaturalTerrainFeatures>();
                }
            }

            // 既存の地形生成イベントにフックする
            if (terrainManager != null)
            {
                // RuntimeTerrainManagerのタイル生成イベントに統合処理を追加
                // 注: 実際の実装では、RuntimeTerrainManagerにイベントシステムを追加する必要があります
                Debug.Log("自然地形特徴統合システム初期化完了");
            }
        }

        /// <summary>
        /// 地形タイルに自然特徴を適用
        /// </summary>
        public void ApplyNaturalFeaturesToTile(TerrainTile tile)
        {
            if (!enableNaturalFeatures || tile == null) return;

            var coordinate = tile.coordinate;
            
            // 既に処理済みの場合はスキップ
            if (tileFeatures.ContainsKey(coordinate)) return;

            // ハイトマップのコピーを作成
            var heightmap = CopyHeightmap(tile.heightmap);
            var resolution = heightmap.GetLength(0);
            var tileSize = tile.tileSize;

            // 自然地形特徴を生成
            var featureData = new NaturalFeatureData();
            
            if (naturalFeatures.enableRiverGeneration)
            {
                featureData.rivers = naturalFeatures.GenerateRiverSystems(heightmap, resolution, tileSize);
                ApplyRiverInfluence(heightmap, featureData.rivers, riverInfluence);
            }

            if (naturalFeatures.enableMountainGeneration)
            {
                featureData.mountainRanges = naturalFeatures.GenerateMountainRanges(heightmap, resolution, tileSize);
                ApplyMountainInfluence(heightmap, featureData.mountainRanges, mountainInfluence);
            }

            if (naturalFeatures.enableValleyGeneration && featureData.mountainRanges != null)
            {
                naturalFeatures.GenerateValleys(heightmap, resolution, tileSize, featureData.mountainRanges);
                featureData.hasValleys = true;
                ApplyValleyInfluence(heightmap, valleyInfluence);
            }

            // 修正されたハイトマップを保存
            featureData.modifiedHeightmap = heightmap;
            tileFeatures[coordinate] = featureData;

            // タイルのメッシュを更新
            if (applyToExistingTerrain)
            {
                UpdateTileMesh(tile, heightmap);
            }

            Debug.Log($"タイル {coordinate} に自然地形特徴を適用完了");
        }

        /// <summary>
        /// ハイトマップのコピー
        /// </summary>
        private float[,] CopyHeightmap(float[,] original)
        {
            int width = original.GetLength(0);
            int height = original.GetLength(1);
            var copy = new float[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    copy[x, y] = original[x, y];
                }
            }

            return copy;
        }

        /// <summary>
        /// 河川の影響を適用
        /// </summary>
        private void ApplyRiverInfluence(float[,] heightmap, List<NaturalTerrainFeatures.RiverSystem> rivers, float influence)
        {
            if (rivers == null) return;

            foreach (var river in rivers)
            {
                // 河川による浸食効果を既存のハイトマップに適用
                // この処理は既にNaturalTerrainFeatures内で実装されているため、
                // ここでは追加の統合処理を行う
                
                foreach (var point in river.riverPath)
                {
                    int resolution = heightmap.GetLength(0);
                    int x = Mathf.RoundToInt((point.x + resolution * 0.5f));
                    int y = Mathf.RoundToInt((point.z + resolution * 0.5f));

                    if (x >= 0 && x < resolution && y >= 0 && y < resolution)
                    {
                        // 河川周辺の地形を滑らかにする
                        SmoothAroundPoint(heightmap, x, y, river.width * 0.1f, influence);
                    }
                }
            }
        }

        /// <summary>
        /// 山脈の影響を適用
        /// </summary>
        private void ApplyMountainInfluence(float[,] heightmap, List<NaturalTerrainFeatures.MountainRange> mountainRanges, float influence)
        {
            if (mountainRanges == null) return;

            foreach (var range in mountainRanges)
            {
                // 山脈周辺の地形を調整
                foreach (var ridgePoint in range.ridgeLine)
                {
                    int resolution = heightmap.GetLength(0);
                    int x = Mathf.RoundToInt((ridgePoint.x + resolution * 0.5f));
                    int y = Mathf.RoundToInt((ridgePoint.z + resolution * 0.5f));

                    if (x >= 0 && x < resolution && y >= 0 && y < resolution)
                    {
                        // 山脈周辺の傾斜を自然にする
                        ApplyNaturalSlope(heightmap, x, y, ridgePoint.y, influence);
                    }
                }
            }
        }

        /// <summary>
        /// 谷の影響を適用
        /// </summary>
        private void ApplyValleyInfluence(float[,] heightmap, float influence)
        {
            // 谷による地形の平滑化
            int resolution = heightmap.GetLength(0);
            
            for (int x = 1; x < resolution - 1; x++)
            {
                for (int y = 1; y < resolution - 1; y++)
                {
                    // 周辺の高度差が大きい場合、谷の効果を適用
                    float currentHeight = heightmap[x, y];
                    float avgNeighborHeight = 0f;
                    int neighborCount = 0;

                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            avgNeighborHeight += heightmap[x + dx, y + dy];
                            neighborCount++;
                        }
                    }

                    avgNeighborHeight /= neighborCount;
                    
                    if (currentHeight < avgNeighborHeight)
                    {
                        // 低い部分（谷）をより滑らかにする
                        heightmap[x, y] = Mathf.Lerp(currentHeight, avgNeighborHeight, influence * 0.1f);
                    }
                }
            }
        }

        /// <summary>
        /// 指定点周辺の平滑化
        /// </summary>
        private void SmoothAroundPoint(float[,] heightmap, int centerX, int centerY, float radius, float strength)
        {
            int resolution = heightmap.GetLength(0);
            int intRadius = Mathf.RoundToInt(radius);

            for (int dx = -intRadius; dx <= intRadius; dx++)
            {
                for (int dy = -intRadius; dy <= intRadius; dy++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (x >= 0 && x < resolution && y >= 0 && y < resolution)
                    {
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        if (distance <= radius)
                        {
                            float influence = (1f - distance / radius) * strength;
                            
                            // 周辺の平均高度を計算
                            float avgHeight = CalculateAverageHeight(heightmap, x, y, 2);
                            heightmap[x, y] = Mathf.Lerp(heightmap[x, y], avgHeight, influence);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 自然な傾斜の適用
        /// </summary>
        private void ApplyNaturalSlope(float[,] heightmap, int centerX, int centerY, float targetHeight, float strength)
        {
            int resolution = heightmap.GetLength(0);
            int radius = 10;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (x >= 0 && x < resolution && y >= 0 && y < resolution)
                    {
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        if (distance <= radius && distance > 0)
                        {
                            float influence = (1f - distance / radius) * strength;
                            float slopeHeight = targetHeight - (distance * 2f); // 自然な傾斜
                            
                            heightmap[x, y] = Mathf.Lerp(heightmap[x, y], 
                                                       Mathf.Max(heightmap[x, y], slopeHeight), 
                                                       influence);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 平均高度の計算
        /// </summary>
        private float CalculateAverageHeight(float[,] heightmap, int centerX, int centerY, int radius)
        {
            int resolution = heightmap.GetLength(0);
            float sum = 0f;
            int count = 0;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (x >= 0 && x < resolution && y >= 0 && y < resolution)
                    {
                        sum += heightmap[x, y];
                        count++;
                    }
                }
            }

            return count > 0 ? sum / count : 0f;
        }

        /// <summary>
        /// タイルメッシュの更新
        /// </summary>
        private void UpdateTileMesh(TerrainTile tile, float[,] newHeightmap)
        {
            if (tile.tileObject == null) return;

            // 新しいハイトマップからメッシュを生成
            var meshFilter = tile.tileObject.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                // MeshGeneratorを使用して新しいメッシュを生成
                var newMesh = GenerateMeshFromHeightmap(newHeightmap, tile.tileSize);
                if (newMesh != null)
                {
                    meshFilter.mesh = newMesh;
                    tile.terrainMesh = newMesh;
                    
                    // コライダーも更新
                    var meshCollider = tile.tileObject.GetComponent<MeshCollider>();
                    if (meshCollider != null)
                    {
                        meshCollider.sharedMesh = newMesh;
                    }
                }
            }
        }

        /// <summary>
        /// ハイトマップからメッシュを生成
        /// </summary>
        private Mesh GenerateMeshFromHeightmap(float[,] heightmap, float tileSize)
        {
            int resolution = heightmap.GetLength(0);
            var vertices = new Vector3[resolution * resolution];
            var triangles = new int[(resolution - 1) * (resolution - 1) * 6];
            var uvs = new Vector2[resolution * resolution];

            // 頂点とUVの生成
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    int index = x * resolution + y;
                    
                    vertices[index] = new Vector3(
                        (x / (float)(resolution - 1) - 0.5f) * tileSize,
                        heightmap[x, y],
                        (y / (float)(resolution - 1) - 0.5f) * tileSize
                    );
                    
                    uvs[index] = new Vector2(x / (float)(resolution - 1), y / (float)(resolution - 1));
                }
            }

            // 三角形の生成
            int triangleIndex = 0;
            for (int x = 0; x < resolution - 1; x++)
            {
                for (int y = 0; y < resolution - 1; y++)
                {
                    int bottomLeft = x * resolution + y;
                    int bottomRight = bottomLeft + 1;
                    int topLeft = (x + 1) * resolution + y;
                    int topRight = topLeft + 1;

                    // 第1三角形
                    triangles[triangleIndex] = bottomLeft;
                    triangles[triangleIndex + 1] = topLeft;
                    triangles[triangleIndex + 2] = bottomRight;

                    // 第2三角形
                    triangles[triangleIndex + 3] = bottomRight;
                    triangles[triangleIndex + 4] = topLeft;
                    triangles[triangleIndex + 5] = topRight;

                    triangleIndex += 6;
                }
            }

            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// 特定タイルの自然特徴データを取得
        /// </summary>
        public NaturalFeatureData GetTileFeatures(Vector2Int coordinate)
        {
            return tileFeatures.ContainsKey(coordinate) ? tileFeatures[coordinate] : null;
        }

        /// <summary>
        /// 全ての自然特徴データをクリア
        /// </summary>
        public void ClearAllFeatures()
        {
            tileFeatures.Clear();
        }
    }
}