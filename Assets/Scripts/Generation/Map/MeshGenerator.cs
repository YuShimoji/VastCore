using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    /// <summary>
    /// 高度地形生成システム
    /// ノイズベース、円形マップ、シームレス化に対応
    /// </summary>
    public static class MeshGenerator
    {
        #region 地形生成の種類
        public enum TerrainType
        {
            Rectangular,    // 従来の矩形地形
            Circular,       // 円形地形
            Seamless        // シームレス地形
        }
        
        public enum NoiseType
        {
            Perlin,         // Perlinノイズ
            Simplex,        // Simplexノイズ
            Ridged,         // リッジノイズ
            Fractal,        // フラクタルノイズ
            Voronoi         // ボロノイノイズ
        }
        #endregion

        #region パラメータ構造体
        [System.Serializable]
        public struct TerrainGenerationParams
        {
            [Header("基本設定")]
            public TerrainType terrainType;
            public int resolution;
            public float size;
            public float maxHeight;
            
            [Header("ノイズ設定")]
            public NoiseType noiseType;
            public float noiseScale;
            public int octaves;
            public float persistence;
            public float lacunarity;
            public Vector2 offset;
            
            [Header("円形地形設定")]
            public float radius;
            public float falloffStrength;
            public AnimationCurve falloffCurve;
            
            [Header("シームレス設定")]
            public bool enableSeamless;
            public float seamlessBorder;
            
            [Header("高度加工")]
            public bool enableTerracing;
            public float terraceHeight;
            public int terraceCount;
            public bool enableErosion;
            public float erosionStrength;
            
            public static TerrainGenerationParams Default()
            {
                return new TerrainGenerationParams
                {
                    terrainType = TerrainType.Circular,
                    resolution = 512,
                    size = 2000f,           // より広大な地形
                    maxHeight = 200f,       // より高い起伏
                    noiseType = NoiseType.Fractal,  // より自然な地形
                    noiseScale = 0.005f,    // より大きなスケール
                    octaves = 8,            // より詳細な起伏
                    persistence = 0.6f,     // より強調された特徴
                    lacunarity = 2.5f,      // より複雑な形状
                    offset = Vector2.zero,
                    radius = 1000f,         // より広い円形地形
                    falloffStrength = 1.5f, // より緩やかな縁
                    falloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0),
                    enableSeamless = true,
                    seamlessBorder = 0.15f, // より広いシームレス領域
                    enableTerracing = true, // テラス化を有効化
                    terraceHeight = 20f,    // より大きなテラス
                    terraceCount = 8,       // より多くのテラス
                    enableErosion = true,   // 浸食を有効化
                    erosionStrength = 0.3f  // 適度な浸食
                };
            }
        }
        #endregion

        #region 従来のハイトマップ生成（互換性維持）
        public static Mesh GenerateTerrainMesh(Texture2D heightMap, float heightMultiplier, AnimationCurve heightCurve)
        {
            if (heightMap == null)
            {
                Debug.LogError("HeightMap is null in MeshGenerator.");
                return new Mesh();
            }

            int width = heightMap.width;
            int height = heightMap.height;

            Vector3[] vertices = new Vector3[width * height];
            int[] triangles = new int[(width - 1) * (height - 1) * 6];
            Vector2[] uvs = new Vector2[width * height];

            int triangleIndex = 0;
            int vertexIndex = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float grayValue = heightMap.GetPixel(x, y).grayscale;
                    float evaluatedHeight = heightCurve.Evaluate(grayValue);

                    vertices[vertexIndex] = new Vector3(x, evaluatedHeight * heightMultiplier, y);
                    uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

                    if (x < width - 1 && y < height - 1)
                    {
                        AddTriangle(triangles, triangleIndex, vertexIndex, vertexIndex + width + 1, vertexIndex + width);
                        triangleIndex += 3;
                        AddTriangle(triangles, triangleIndex, vertexIndex, vertexIndex + 1, vertexIndex + width + 1);
                        triangleIndex += 3;
                    }
                    vertexIndex++;
                }
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.name = "Generated Terrain Mesh";

            return mesh;
        }

        private static void AddTriangle(int[] triangles, int triangleIndex, int a, int b, int c)
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
        }
        #endregion

        #region 新しい高度地形生成システム
        /// <summary>
        /// 高度地形生成のメイン関数
        /// </summary>
        public static Mesh GenerateAdvancedTerrain(TerrainGenerationParams parameters)
        {
            // ハイトマップを生成
            float[,] heightmap = GenerateHeightmap(parameters);
            
            // 地形タイプに応じた処理
            switch (parameters.terrainType)
            {
                case TerrainType.Circular:
                    heightmap = ApplyCircularFalloff(heightmap, parameters);
                    break;
                case TerrainType.Seamless:
                    heightmap = ApplySeamlessEdges(heightmap, parameters);
                    break;
            }
            
            // 追加処理
            if (parameters.enableTerracing)
                heightmap = ApplyTerracing(heightmap, parameters);
            
            if (parameters.enableErosion)
                heightmap = ApplySimpleErosion(heightmap, parameters);
            
            // メッシュを生成
            return GenerateMeshFromHeightmap(heightmap, parameters);
        }

        /// <summary>
        /// ノイズベースハイトマップ生成
        /// </summary>
        private static float[,] GenerateHeightmap(TerrainGenerationParams parameters)
        {
            int resolution = parameters.resolution;
            float[,] heightmap = new float[resolution, resolution];
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float amplitude = 1f;
                    float frequency = parameters.noiseScale;
                    float noiseValue = 0f;
                    
                    // フラクタルノイズ（複数オクターブ）
                    for (int octave = 0; octave < parameters.octaves; octave++)
                    {
                        float sampleX = (x + parameters.offset.x) * frequency;
                        float sampleY = (y + parameters.offset.y) * frequency;
                        
                        float noiseHeight = 0f;
                        
                        switch (parameters.noiseType)
                        {
                            case NoiseType.Perlin:
                                noiseHeight = Mathf.PerlinNoise(sampleX, sampleY);
                                break;
                            case NoiseType.Simplex:
                                noiseHeight = SimplexNoise(sampleX, sampleY);
                                break;
                            case NoiseType.Ridged:
                                noiseHeight = 1f - Mathf.Abs(Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f);
                                break;
                            case NoiseType.Fractal:
                                noiseHeight = FractalNoise(sampleX, sampleY);
                                break;
                            case NoiseType.Voronoi:
                                noiseHeight = VoronoiNoise(sampleX, sampleY);
                                break;
                        }
                        
                        noiseValue += noiseHeight * amplitude;
                        amplitude *= parameters.persistence;
                        frequency *= parameters.lacunarity;
                    }
                    
                    heightmap[y, x] = Mathf.Clamp01(noiseValue);
                }
            }
            
            return heightmap;
        }

        /// <summary>
        /// 円形フォールオフ適用
        /// </summary>
        private static float[,] ApplyCircularFalloff(float[,] heightmap, TerrainGenerationParams parameters)
        {
            int resolution = parameters.resolution;
            float radius = parameters.radius;
            float falloffStrength = parameters.falloffStrength;
            
            Vector2 center = new Vector2(resolution * 0.5f, resolution * 0.5f);
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 position = new Vector2(x, y);
                    float distanceFromCenter = Vector2.Distance(position, center);
                    
                    // 正規化された距離
                    float normalizedDistance = distanceFromCenter / (resolution * 0.5f);
                    
                    // フォールオフ適用
                    float falloff = parameters.falloffCurve.Evaluate(normalizedDistance);
                    falloff = Mathf.Pow(falloff, falloffStrength);
                    
                    heightmap[y, x] *= falloff;
                }
            }
            
            return heightmap;
        }

        /// <summary>
        /// シームレスエッジ適用
        /// </summary>
        private static float[,] ApplySeamlessEdges(float[,] heightmap, TerrainGenerationParams parameters)
        {
            int resolution = parameters.resolution;
            float borderSize = parameters.seamlessBorder * resolution;
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float edgeDistance = Mathf.Min(
                        Mathf.Min(x, resolution - 1 - x),
                        Mathf.Min(y, resolution - 1 - y)
                    );
                    
                    if (edgeDistance < borderSize)
                    {
                        float falloff = edgeDistance / borderSize;
                        heightmap[y, x] *= falloff;
                    }
                }
            }
            
            return heightmap;
        }

        /// <summary>
        /// テラス化処理
        /// </summary>
        private static float[,] ApplyTerracing(float[,] heightmap, TerrainGenerationParams parameters)
        {
            int resolution = parameters.resolution;
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float height = heightmap[y, x];
                    float terraceLevel = Mathf.Floor(height * parameters.terraceCount) / parameters.terraceCount;
                    heightmap[y, x] = terraceLevel;
                }
            }
            
            return heightmap;
        }

        /// <summary>
        /// 簡易浸食処理
        /// </summary>
        private static float[,] ApplySimpleErosion(float[,] heightmap, TerrainGenerationParams parameters)
        {
            int resolution = parameters.resolution;
            float[,] erodedMap = new float[resolution, resolution];
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float totalHeight = 0f;
                    int sampleCount = 0;
                    
                    // 周囲の高さを平均化
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int sampleX = Mathf.Clamp(x + dx, 0, resolution - 1);
                            int sampleY = Mathf.Clamp(y + dy, 0, resolution - 1);
                            
                            totalHeight += heightmap[sampleY, sampleX];
                            sampleCount++;
                        }
                    }
                    
                    float averageHeight = totalHeight / sampleCount;
                    float originalHeight = heightmap[y, x];
                    
                    // 浸食強度に応じてブレンド
                    erodedMap[y, x] = Mathf.Lerp(originalHeight, averageHeight, parameters.erosionStrength);
                }
            }
            
            return erodedMap;
        }

        /// <summary>
        /// ハイトマップからメッシュを生成
        /// </summary>
        private static Mesh GenerateMeshFromHeightmap(float[,] heightmap, TerrainGenerationParams parameters)
        {
            int resolution = parameters.resolution;
            float size = parameters.size;
            float maxHeight = parameters.maxHeight;
            
            Vector3[] vertices = new Vector3[resolution * resolution];
            int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
            Vector2[] uvs = new Vector2[resolution * resolution];
            
            int triangleIndex = 0;
            int vertexIndex = 0;
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float height = heightmap[y, x] * maxHeight;
                    
                    // 座標を中心にオフセット
                    float posX = (x / (float)(resolution - 1) - 0.5f) * size;
                    float posZ = (y / (float)(resolution - 1) - 0.5f) * size;
                    
                    vertices[vertexIndex] = new Vector3(posX, height, posZ);
                    uvs[vertexIndex] = new Vector2(x / (float)(resolution - 1), y / (float)(resolution - 1));
                    
                    if (x < resolution - 1 && y < resolution - 1)
                    {
                        AddTriangle(triangles, triangleIndex, vertexIndex, vertexIndex + resolution + 1, vertexIndex + resolution);
                        triangleIndex += 3;
                        AddTriangle(triangles, triangleIndex, vertexIndex, vertexIndex + 1, vertexIndex + resolution + 1);
                        triangleIndex += 3;
                    }
                    vertexIndex++;
                }
            }
            
            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // 大きなメッシュ対応
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.name = "Advanced Generated Terrain";
            
            return mesh;
        }
        #endregion

        #region ノイズ関数群
        /// <summary>
        /// Simplexノイズの簡易実装
        /// </summary>
        private static float SimplexNoise(float x, float y)
        {
            // 簡易版 - 実際のSimplexノイズはより複雑
            return (Mathf.PerlinNoise(x, y) + Mathf.PerlinNoise(x * 2f, y * 2f) * 0.5f) / 1.5f;
        }

        /// <summary>
        /// フラクタルノイズ
        /// </summary>
        private static float FractalNoise(float x, float y)
        {
            float value = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            
            for (int i = 0; i < 4; i++)
            {
                value += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
                amplitude *= 0.5f;
                frequency *= 2f;
            }
            
            return value / 1.875f; // 正規化
        }

        /// <summary>
        /// ボロノイノイズの簡易実装
        /// </summary>
        private static float VoronoiNoise(float x, float y)
        {
            int cellX = Mathf.FloorToInt(x);
            int cellY = Mathf.FloorToInt(y);
            
            float minDistance = float.MaxValue;
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int seedX = cellX + dx;
                    int seedY = cellY + dy;
                    
                    // 疑似ランダムな点を生成
                    float pointX = seedX + Random.Range(0f, 1f);
                    float pointY = seedY + Random.Range(0f, 1f);
                    
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(pointX, pointY));
                    minDistance = Mathf.Min(minDistance, distance);
                }
            }
            
            return Mathf.Clamp01(minDistance);
        }
        #endregion

        #region ユーティリティ関数
        /// <summary>
        /// 地形の統計情報を取得
        /// </summary>
        public static TerrainStats GetTerrainStats(float[,] heightmap)
        {
            int resolution = heightmap.GetLength(0);
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;
            float totalHeight = 0f;
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float height = heightmap[y, x];
                    minHeight = Mathf.Min(minHeight, height);
                    maxHeight = Mathf.Max(maxHeight, height);
                    totalHeight += height;
                }
            }
            
            float averageHeight = totalHeight / (resolution * resolution);
            
            return new TerrainStats
            {
                minHeight = minHeight,
                maxHeight = maxHeight,
                averageHeight = averageHeight,
                resolution = resolution
            };
        }

        [System.Serializable]
        public struct TerrainStats
        {
            public float minHeight;
            public float maxHeight;
            public float averageHeight;
            public int resolution;
        }
        #endregion
    }
}