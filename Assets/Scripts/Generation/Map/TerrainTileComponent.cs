using UnityEngine;
using Vastcore.Generation.Map;

namespace Vastcore.Generation
{
    /// <summary>
    /// 互換用の地形タイルコンポーネント。
    /// 新しい TerrainTile 実装を継承しつつ、
    /// RuntimeTerrainManager / テストで使用されている TerrainTileComponent API を提供する。
    /// </summary>
    public class TerrainTileComponent : TerrainTile
    {
        /// <summary>
        /// RuntimeTerrainManager.TerrainGenerationParams を用いてタイルを初期化する
        /// （BasicTerrainGenerationTest から利用されるレガシーAPI）。
        /// </summary>
        public void Initialize(
            Vector2Int tileCoordinate,
            int tileSize,
            int resolution,
            float heightScale,
            RuntimeTerrainManager.TerrainGenerationParams genParams)
        {
            float[,] heights = GenerateHeightmap(tileCoordinate, tileSize, resolution, genParams);
            Mesh mesh = BuildMesh(heights, tileSize, heightScale);
            Material material = GetOrCreateDefaultMaterial();

            // 新しい TerrainTile API で初期化
            Initialize(tileCoordinate, tileSize, heights, resolution, heightScale, mesh, material);
            SetActive(true);
        }

        /// <summary>
        /// 生成パラメータを変更してタイルを再生成する（テスト用互換API）。
        /// </summary>
        public void UpdateTerrain(RuntimeTerrainManager.TerrainGenerationParams newParams)
        {
            int size = Mathf.RoundToInt(TileSize);
            int resolution = HeightResolution > 0 ? HeightResolution : 64;
            float scale = HeightScale > 0f ? HeightScale : 50f;

            float[,] heights = GenerateHeightmap(Coordinate, size, resolution, newParams);
            Mesh mesh = BuildMesh(heights, size, scale);
            Material material = TerrainMaterial != null ? TerrainMaterial : GetOrCreateDefaultMaterial();

            Initialize(Coordinate, size, heights, resolution, scale, mesh, material);
            SetActive(true);
        }

        /// <summary>
        /// RuntimeTerrainManager の実装に準拠したハイトマップ生成ヘルパー。
        /// </summary>
        private float[,] GenerateHeightmap(
            Vector2Int tileCoord,
            int tileSize,
            int resolution,
            RuntimeTerrainManager.TerrainGenerationParams genParams)
        {
            float[,] heights = new float[resolution, resolution];

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float amplitude = genParams.amplitude;
                    float frequency = genParams.frequency;
                    float noiseValue = 0f;
                    float weight = 1f;

                    for (int octave = 0; octave < Mathf.Max(1, genParams.octaves); octave++)
                    {
                        float sampleX = (tileCoord.x * tileSize + (x / (float)(resolution - 1) * tileSize)) * frequency + genParams.offset.x;
                        float sampleZ = (tileCoord.y * tileSize + (z / (float)(resolution - 1) * tileSize)) * frequency + genParams.offset.y;

                        float perlin = Mathf.PerlinNoise(sampleX, sampleZ);
                        noiseValue += perlin * amplitude * weight;

                        weight *= genParams.persistence;
                        frequency *= genParams.lacunarity;
                    }

                    heights[z, x] = Mathf.Clamp01(noiseValue);
                }
            }

            return heights;
        }

        /// <summary>
        /// RuntimeTerrainManager の実装に準拠したメッシュ生成ヘルパー。
        /// </summary>
        private Mesh BuildMesh(float[,] heights, int tileSize, float heightScale)
        {
            int resolution = heights.GetLength(0);
            Vector3[] vertices = new Vector3[resolution * resolution];
            Vector2[] uvs = new Vector2[resolution * resolution];
            int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];

            int vertexIndex = 0;
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float normalizedX = x / (float)(resolution - 1);
                    float normalizedZ = z / (float)(resolution - 1);

                    float posX = (normalizedX - 0.5f) * tileSize;
                    float posZ = (normalizedZ - 0.5f) * tileSize;
                    float posY = heights[z, x] * heightScale;

                    vertices[vertexIndex] = new Vector3(posX, posY, posZ);
                    uvs[vertexIndex] = new Vector2(normalizedX, normalizedZ);
                    vertexIndex++;
                }
            }

            int triangleIndex = 0;
            for (int z = 0; z < resolution - 1; z++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    int topLeft = z * resolution + x;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + resolution;
                    int bottomRight = bottomLeft + 1;

                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = bottomRight;
                    triangles[triangleIndex++] = bottomLeft;

                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = topRight;
                    triangles[triangleIndex++] = bottomRight;
                }
            }

            var mesh = new Mesh
            {
                vertices = vertices,
                triangles = triangles,
                uv = uvs
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// デフォルトマテリアルを取得または生成する。
        /// </summary>
        private Material GetOrCreateDefaultMaterial()
        {
            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                return renderer.sharedMaterial;
            }

            var material = new Material(Shader.Find("Standard"))
            {
                color = Color.Lerp(Color.gray, Color.green, 0.4f)
            };

            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return material;
        }
    }
}
