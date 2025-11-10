using UnityEngine;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// 地形タイルコンポーネント
    /// 個別の地形タイルを生成・管理
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class TerrainTile : MonoBehaviour
    {
        private Vector2Int tileCoord;
        private int tileSize;
        private int resolution;
        private float heightScale;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private Mesh terrainMesh;

        private RuntimeTerrainManager.TerrainGenerationParams generationParams;

        /// <summary>
        /// 地形タイルを初期化
        /// </summary>
        public void Initialize(Vector2Int coord, int size, int res, float height, RuntimeTerrainManager.TerrainGenerationParams genParams)
        {
            tileCoord = coord;
            tileSize = size;
            resolution = res;
            heightScale = height;
            generationParams = genParams;

            // コンポーネント取得
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();

            // マテリアル設定
            if (meshRenderer.sharedMaterial == null)
            {
                meshRenderer.sharedMaterial = CreateTerrainMaterial();
            }

            // 地形生成
            GenerateTerrainMesh();
        }

        /// <summary>
        /// 地形メッシュを生成
        /// </summary>
        private void GenerateTerrainMesh()
        {
            terrainMesh = new Mesh();
            terrainMesh.name = $"TerrainMesh_{tileCoord.x}_{tileCoord.y}";

            // 頂点生成
            Vector3[] vertices = GenerateVertices();
            int[] triangles = GenerateTriangles();
            Vector2[] uvs = GenerateUVs();

            // メッシュ設定
            terrainMesh.vertices = vertices;
            terrainMesh.triangles = triangles;
            terrainMesh.uv = uvs;

            // 法線と境界計算
            terrainMesh.RecalculateNormals();
            terrainMesh.RecalculateBounds();

            // コンポーネントに設定
            meshFilter.sharedMesh = terrainMesh;
            meshCollider.sharedMesh = terrainMesh;
        }

        /// <summary>
        /// 頂点を生成
        /// </summary>
        private Vector3[] GenerateVertices()
        {
            Vector3[] vertices = new Vector3[resolution * resolution];

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int index = z * resolution + x;

                    // ローカル座標
                    float xPos = (float)x / (resolution - 1) * tileSize;
                    float zPos = (float)z / (resolution - 1) * tileSize;

                    // ワールド座標（ノイズ計算用）
                    float worldX = transform.position.x + xPos;
                    float worldZ = transform.position.z + zPos;

                    // 高さ計算
                    float height = GenerateHeight(worldX, worldZ);

                    vertices[index] = new Vector3(xPos, height, zPos);
                }
            }

            return vertices;
        }

        /// <summary>
        /// 三角形を生成
        /// </summary>
        private int[] GenerateTriangles()
        {
            int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];

            int triangleIndex = 0;
            for (int z = 0; z < resolution - 1; z++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    int vertexIndex = z * resolution + x;

                    // 最初の三角形
                    triangles[triangleIndex++] = vertexIndex;
                    triangles[triangleIndex++] = vertexIndex + resolution;
                    triangles[triangleIndex++] = vertexIndex + 1;

                    // 2番目の三角形
                    triangles[triangleIndex++] = vertexIndex + 1;
                    triangles[triangleIndex++] = vertexIndex + resolution;
                    triangles[triangleIndex++] = vertexIndex + resolution + 1;
                }
            }

            return triangles;
        }

        /// <summary>
        /// UV座標を生成
        /// </summary>
        private Vector2[] GenerateUVs()
        {
            Vector2[] uvs = new Vector2[resolution * resolution];

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int index = z * resolution + x;
                    uvs[index] = new Vector2(
                        (float)x / (resolution - 1),
                        (float)z / (resolution - 1)
                    );
                }
            }

            return uvs;
        }

        /// <summary>
        /// 高さを生成（Perlinノイズ使用）
        /// </summary>
        private float GenerateHeight(float worldX, float worldZ)
        {
            float height = 0f;
            float amplitude = generationParams.amplitude;
            float frequency = generationParams.frequency;

            // 複数オクターブのPerlinノイズ
            for (int i = 0; i < generationParams.octaves; i++)
            {
                float sampleX = (worldX + generationParams.offset.x) * frequency;
                float sampleZ = (worldZ + generationParams.offset.y) * frequency;

                float perlinValue = Mathf.PerlinNoise(sampleX, sampleZ) * 2f - 1f; // -1 to 1
                height += perlinValue * amplitude;

                amplitude *= generationParams.persistence;
                frequency *= generationParams.lacunarity;
            }

            return height * heightScale;
        }

        /// <summary>
        /// 地形を更新
        /// </summary>
        public void UpdateTerrain(RuntimeTerrainManager.TerrainGenerationParams newParams)
        {
            generationParams = newParams;
            GenerateTerrainMesh();
        }

        /// <summary>
        /// 指定ローカル座標の高さを取得
        /// </summary>
        public float GetHeightAtLocalPosition(Vector3 localPos)
        {
            if (terrainMesh == null || terrainMesh.vertices.Length == 0)
                return 0f;

            // 最も近い頂点の高さを返す（簡易実装）
            float normalizedX = localPos.x / tileSize;
            float normalizedZ = localPos.z / tileSize;

            int x = Mathf.Clamp(Mathf.RoundToInt(normalizedX * (resolution - 1)), 0, resolution - 1);
            int z = Mathf.Clamp(Mathf.RoundToInt(normalizedZ * (resolution - 1)), 0, resolution - 1);

            int vertexIndex = z * resolution + x;
            return terrainMesh.vertices[vertexIndex].y;
        }

        /// <summary>
        /// 地形マテリアルを作成
        /// </summary>
        private Material CreateTerrainMaterial()
        {
            Material material = new Material(Shader.Find("Standard"));

            // 基本的な地形色設定
            material.color = new Color(0.4f, 0.6f, 0.2f); // 緑がかった茶色

            // 物理ベースレンダリング設定
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", 0.1f);

            return material;
        }

        private void OnDestroy()
        {
            if (terrainMesh != null)
            {
                Destroy(terrainMesh);
            }
        }
    }
}