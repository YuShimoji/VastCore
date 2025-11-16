using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// ランタイム地形生成マネージャー
    /// 動的な地形生成と管理を担当
    /// </summary>
    public class RuntimeTerrainManager : MonoBehaviour
    {
        [Header("地形設定")]
        [SerializeField] private int tileSize = 100;
        [SerializeField] private int tileResolution = 256;
        [SerializeField] private float heightScale = 50f;
        [SerializeField] private int renderDistance = 3;

        [Header("パフォーマンス設定")]
        [SerializeField] private float tileUnloadDistance = 200f;

        // 地形タイル管理
        private Dictionary<Vector2Int, TerrainTileComponent> activeTiles = new Dictionary<Vector2Int, TerrainTileComponent>();
        private Queue<Vector2Int> tileLoadQueue = new Queue<Vector2Int>();
        private Transform playerTransform;

        private Material sharedTerrainMaterial;

        // 地形生成パラメータ
        [System.Serializable]
        public class TerrainGenerationParams
        {
            public float frequency = 0.01f;
            public float amplitude = 1f;
            public int octaves = 4;
            public float persistence = 0.5f;
            public float lacunarity = 2f;
            public Vector2 offset = Vector2.zero;
        }

        [SerializeField] private TerrainGenerationParams generationParams = new TerrainGenerationParams();

        private void Start()
        {
            // プレイヤーTransformを取得（仮定）
            playerTransform = Camera.main.transform;

            if (sharedTerrainMaterial == null)
            {
                sharedTerrainMaterial = new Material(Shader.Find("Standard"))
                {
                    color = Color.Lerp(Color.gray, Color.green, 0.4f)
                };
            }

            // 初期地形生成
            GenerateInitialTerrain();
        }

        private void Update()
        {
            UpdateTerrainTiles();
        }

        /// <summary>
        /// 初期地形生成
        /// </summary>
        private void GenerateInitialTerrain()
        {
            Vector2Int playerTileCoord = WorldToTileCoord(playerTransform.position);

            // 中心と周囲のタイルを生成
            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    Vector2Int tileCoord = playerTileCoord + new Vector2Int(x, z);
                    LoadTerrainTile(tileCoord);
                }
            }
        }

        /// <summary>
        /// 地形タイルの更新
        /// </summary>
        private void UpdateTerrainTiles()
        {
            if (playerTransform == null) return;

            Vector2Int currentTileCoord = WorldToTileCoord(playerTransform.position);

            // 新しいタイルをロード
            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    Vector2Int tileCoord = currentTileCoord + new Vector2Int(x, z);
                    if (!activeTiles.ContainsKey(tileCoord))
                    {
                        LoadTerrainTile(tileCoord);
                    }
                }
            }

            // 遠いタイルをアンロード
            List<Vector2Int> tilesToUnload = new List<Vector2Int>();
            foreach (var tilePair in activeTiles)
            {
                float distance = Vector3.Distance(
                    playerTransform.position,
                    TileCoordToWorld(tilePair.Key) + new Vector3(tileSize / 2f, 0, tileSize / 2f)
                );

                if (distance > tileUnloadDistance)
                {
                    tilesToUnload.Add(tilePair.Key);
                }
            }

            foreach (var tileCoord in tilesToUnload)
            {
                UnloadTerrainTile(tileCoord);
            }
        }

        /// <summary>
        /// 地形タイルをロード
        /// </summary>
        private void LoadTerrainTile(Vector2Int tileCoord)
        {
            if (activeTiles.ContainsKey(tileCoord)) return;

            // TerrainTileを作成
            GameObject tileObj = new GameObject($"TerrainTile_{tileCoord.x}_{tileCoord.y}");
            tileObj.transform.parent = transform;
            tileObj.transform.position = TileCoordToWorld(tileCoord);

            TerrainTileComponent terrainTile = tileObj.AddComponent<TerrainTileComponent>();
            float[,] heightData = GenerateHeightmap(tileCoord);
            Mesh mesh = BuildMesh(heightData);

            terrainTile.Initialize(tileCoord, tileSize, heightData, tileResolution, heightScale, mesh, sharedTerrainMaterial);
            terrainTile.SetActive(true);

            activeTiles[tileCoord] = terrainTile;
        }

        /// <summary>
        /// 地形タイルをアンロード
        /// </summary>
        private void UnloadTerrainTile(Vector2Int tileCoord)
        {
            if (!activeTiles.ContainsKey(tileCoord)) return;

            Destroy(activeTiles[tileCoord].gameObject);
            activeTiles.Remove(tileCoord);
        }

        /// <summary>
        /// ワールド座標をタイル座標に変換
        /// </summary>
        private Vector2Int WorldToTileCoord(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / tileSize),
                Mathf.FloorToInt(worldPos.z / tileSize)
            );
        }

        /// <summary>
        /// タイル座標をワールド座標に変換
        /// </summary>
        private Vector3 TileCoordToWorld(Vector2Int tileCoord)
        {
            return new Vector3(
                tileCoord.x * tileSize,
                0,
                tileCoord.y * tileSize
            );
        }

        /// <summary>
        /// 指定座標の高さを取得
        /// </summary>
        public float GetHeightAtPosition(Vector3 position)
        {
            Vector2Int tileCoord = WorldToTileCoord(position);
            if (activeTiles.TryGetValue(tileCoord, out TerrainTileComponent tile))
            {
                return tile.GetHeightAtWorldPosition(position);
            }
            return 0f;
        }

        /// <summary>
        /// 地形生成パラメータを更新
        /// </summary>
        public void UpdateGenerationParams(TerrainGenerationParams newParams)
        {
            generationParams = newParams;

            // 全タイルを再生成
            foreach (var pair in activeTiles)
            {
                RegenerateTile(pair.Key, pair.Value);
            }
        }

        private void RegenerateTile(Vector2Int coord, TerrainTileComponent tile)
        {
            float[,] heightData = GenerateHeightmap(coord);
            Mesh mesh = BuildMesh(heightData);
            tile.Initialize(coord, tileSize, heightData, tileResolution, heightScale, mesh, sharedTerrainMaterial);
            tile.SetActive(true);
        }

        private float[,] GenerateHeightmap(Vector2Int tileCoord)
        {
            float[,] heights = new float[tileResolution, tileResolution];

            for (int z = 0; z < tileResolution; z++)
            {
                for (int x = 0; x < tileResolution; x++)
                {
                    float amplitude = generationParams.amplitude;
                    float frequency = generationParams.frequency;
                    float noiseValue = 0f;
                    float weight = 1f;

                    for (int octave = 0; octave < Mathf.Max(1, generationParams.octaves); octave++)
                    {
                        float sampleX = (tileCoord.x * tileSize + (x / (float)(tileResolution - 1) * tileSize)) * frequency + generationParams.offset.x;
                        float sampleZ = (tileCoord.y * tileSize + (z / (float)(tileResolution - 1) * tileSize)) * frequency + generationParams.offset.y;

                        float perlin = Mathf.PerlinNoise(sampleX, sampleZ);
                        noiseValue += perlin * amplitude * weight;

                        weight *= generationParams.persistence;
                        frequency *= generationParams.lacunarity;
                    }

                    heights[z, x] = Mathf.Clamp01(noiseValue);
                }
            }

            return heights;
        }

        private Mesh BuildMesh(float[,] heights)
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
    }
}