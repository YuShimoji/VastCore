using UnityEngine;
using Vastcore.Generation.Map;

namespace Vastcore.Generation
{
    /// <summary>
    /// 地形タイルコンポーネント - MonoBehaviourとして実装
    /// TerrainTile データを保持し、Unity のコンポーネントシステムと連携する
    /// </summary>
    public class TerrainTileComponent : MonoBehaviour
    {
        #region タイルデータ
        /// <summary>タイル座標</summary>
        public Vector2Int coordinate;
        
        /// <summary>ワールド座標での位置</summary>
        public Vector3 worldPosition;
        
        /// <summary>タイルサイズ</summary>
        public float tileSize = 2000f;
        
        /// <summary>高度解像度</summary>
        public int heightResolution = 256;
        
        /// <summary>高さスケール</summary>
        public float heightScale = 200f;
        
        /// <summary>ハイトマップデータ</summary>
        public float[,] heightData;
        
        /// <summary>地形メッシュ</summary>
        public Mesh terrainMesh;
        
        /// <summary>地形マテリアル</summary>
        public Material terrainMaterial;
        
        /// <summary>内部 TerrainTile データ</summary>
        public TerrainTile tileData { get; private set; }
        
        /// <summary>タイル状態</summary>
        public TerrainTile.TileState state { get; private set; } = TerrainTile.TileState.Unloaded;
        #endregion

        #region プロパティ
        /// <summary>タイル座標 (Coordinate プロパティ)</summary>
        public Vector2Int Coordinate => coordinate;
        
        /// <summary>タイルサイズ (TileSize プロパティ)</summary>
        public float TileSize => tileSize;
        
        /// <summary>高さ解像度 (HeightResolution プロパティ)</summary>
        public int HeightResolution => heightResolution;
        
        /// <summary>高さスケール (HeightScale プロパティ)</summary>
        public float HeightScale => heightScale;
        
        /// <summary>地形マテリアル (TerrainMaterial プロパティ)</summary>
        public Material TerrainMaterial => terrainMaterial;
        #endregion
        #region 初期化
        /// <summary>
        /// TerrainTileComponent を初期化
        /// </summary>
        public void Initialize(
            Vector2Int tileCoordinate,
            int size,
            int resolution,
            float scale,
            RuntimeTerrainManager.TerrainGenerationParams genParams)
        {
            coordinate = tileCoordinate;
            tileSize = size;
            heightResolution = resolution;
            heightScale = scale;
            worldPosition = new Vector3(
                tileCoordinate.x * size + size * 0.5f,
                0f,
                tileCoordinate.y * size + size * 0.5f
            );

            // ハイトマップとメッシュを生成
            heightData = GenerateHeightmap(tileCoordinate, size, resolution, genParams);
            terrainMesh = BuildMesh(heightData, size, scale);
            terrainMaterial = GetOrCreateDefaultMaterial();

            // TerrainTile データも作成（互換性のため）
            var terrainParams = MeshGenerator.TerrainGenerationParams.Default();
            terrainParams.size = size;
            terrainParams.resolution = resolution;
            terrainParams.maxHeight = scale;
            
            var circularParams = CircularTerrainGenerator.CircularTerrainParams.Default();
            circularParams.radius = 0f; // 円形マスク無効

            tileData = TerrainTile.Create(tileCoordinate, size, terrainParams, circularParams);
            tileData.worldPosition = worldPosition;
            tileData.heightmap = heightData;
            tileData.terrainMesh = terrainMesh;
            tileData.terrainMaterial = terrainMaterial;
            tileData.tileObject = gameObject;
            
            state = TerrainTile.TileState.Loaded;

            // メッシュフィルターとレンダラーを設定
            SetupMeshComponents();
            SetTileActive(true);
        }

        /// <summary>
        /// TerrainTile データで初期化（互換性のため）
        /// </summary>
        public void InitializeFromTerrainTile(TerrainTile tile)
        {
            tileData = tile;
            coordinate = tile.coordinate;
            tileSize = tile.tileSize;
            worldPosition = tile.worldPosition;
            heightData = tile.heightmap;
            terrainMesh = tile.terrainMesh;
            terrainMaterial = tile.terrainMaterial;
            
            if (heightData != null)
            {
                heightResolution = heightData.GetLength(0);
            }
            
            SetupMeshComponents();
        }
        #endregion

        #region メッシュコンポーネント設定
        /// <summary>
        /// MeshFilter と MeshRenderer を設定
        /// </summary>
        private void SetupMeshComponents()
        {
            if (terrainMesh == null) return;

            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
            meshFilter.mesh = terrainMesh;

            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
            meshRenderer.material = terrainMaterial ?? GetOrCreateDefaultMaterial();

            var meshCollider = GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }
            meshCollider.sharedMesh = terrainMesh;
        }
        #endregion

        #region 地形更新
        /// <summary>
        /// 生成パラメータを変更してタイルを再生成する
        /// </summary>
        public void UpdateTerrain(RuntimeTerrainManager.TerrainGenerationParams newParams)
        {
            int size = Mathf.RoundToInt(tileSize);
            int resolution = heightResolution > 0 ? heightResolution : 64;
            float scale = heightScale > 0f ? heightScale : 50f;

            heightData = GenerateHeightmap(coordinate, size, resolution, newParams);
            terrainMesh = BuildMesh(heightData, size, scale);
            
            // メッシュを更新
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.mesh = terrainMesh;
            }
            
            var meshCollider = GetComponent<MeshCollider>();
            if (meshCollider != null)
            {
                meshCollider.sharedMesh = terrainMesh;
            }

            // TerrainTile データも更新
            if (tileData != null)
            {
                tileData.heightmap = heightData;
                tileData.terrainMesh = terrainMesh;
            }
            
            SetTileActive(true);
        }
        #endregion

        #region 地形生成ヘルパー
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
        #endregion

        #region ユーティリティ
        /// <summary>
        /// タイルをアクティブ化/非アクティブ化
        /// </summary>
        public void SetTileActive(bool active)
        {
            gameObject.SetActive(active);
            if (tileData != null)
            {
                tileData.SetActive(active);
            }
        }

        /// <summary>
        /// タイルを削除
        /// </summary>
        public void Unload()
        {
            if (tileData != null)
            {
                tileData.UnloadTile();
            }
            
            if (gameObject != null)
            {
                Destroy(gameObject);
            }
        }
        #endregion
    }
}
