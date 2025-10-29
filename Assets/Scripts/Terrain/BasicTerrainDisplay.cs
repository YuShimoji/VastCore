using UnityEngine;
using System.Collections;

namespace Vastcore.Terrain
{
    /// <summary>
    /// 基本的な地形表示システム
    /// シンプルな地形をシーンに表示する
    /// </summary>
    public class BasicTerrainDisplay : MonoBehaviour
    {
        [Header("地形設定")]
        [SerializeField] private int terrainSize = 256;
        [SerializeField] private float terrainHeight = 100f;
        [SerializeField] private float noiseScale = 0.1f;
        [SerializeField] private Material terrainMaterial;

        [Header("表示設定")]
        [SerializeField] private bool autoGenerate = true;
        [SerializeField] private Vector2Int tileCoordinate = Vector2Int.zero;

        private GameObject terrainObject;
        private Mesh terrainMesh;

        void Start()
        {
            if (autoGenerate)
            {
                GenerateBasicTerrain();
            }
        }

        /// <summary>
        /// 基本的な地形を生成して表示
        /// </summary>
        [ContextMenu("Generate Basic Terrain")]
        public void GenerateBasicTerrain()
        {
            Debug.Log("Generating basic terrain...");

            // 既存の地形を削除
            if (terrainObject != null)
            {
                DestroyImmediate(terrainObject);
            }

            // 地形データを生成
            float[,] heightmap = GenerateHeightmap();

            // メッシュを生成
            terrainMesh = CreateTerrainMesh(heightmap);

            // GameObjectを作成
            terrainObject = new GameObject("BasicTerrain");
            terrainObject.transform.position = new Vector3(
                tileCoordinate.x * terrainSize,
                0,
                tileCoordinate.y * terrainSize
            );

            // コンポーネントを追加
            var meshFilter = terrainObject.AddComponent<MeshFilter>();
            meshFilter.mesh = terrainMesh;

            var meshRenderer = terrainObject.AddComponent<MeshRenderer>();
            if (terrainMaterial != null)
            {
                meshRenderer.material = terrainMaterial;
            }
            else
            {
                // デフォルトマテリアルを作成
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.3f, 0.6f, 0.2f);
                meshRenderer.material = material;
            }

            var meshCollider = terrainObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = terrainMesh;

            Debug.Log("Basic terrain generated successfully!");
        }

        /// <summary>
        /// シンプルなハイトマップを生成
        /// </summary>
        private float[,] GenerateHeightmap()
        {
            float[,] heightmap = new float[terrainSize, terrainSize];

            for (int x = 0; x < terrainSize; x++)
            {
                for (int y = 0; y < terrainSize; y++)
                {
                    // シンプルなPerlinノイズ
                    float nx = (float)x / terrainSize + tileCoordinate.x;
                    float ny = (float)y / terrainSize + tileCoordinate.y;

                    float height = Mathf.PerlinNoise(nx * noiseScale, ny * noiseScale);
                    heightmap[x, y] = height * terrainHeight;
                }
            }

            return heightmap;
        }

        /// <summary>
        /// 地形メッシュを作成
        /// </summary>
        private Mesh CreateTerrainMesh(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            Vector3[] vertices = new Vector3[width * height];
            int[] triangles = new int[(width - 1) * (height - 1) * 6];
            Vector2[] uvs = new Vector2[width * height];

            // 頂点を設定
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    vertices[index] = new Vector3(x, heightmap[x, y], y);
                    uvs[index] = new Vector2((float)x / width, (float)y / height);
                }
            }

            // 三角形を設定
            int triangleIndex = 0;
            for (int y = 0; y < height - 1; y++)
            {
                for (int x = 0; x < width - 1; x++)
                {
                    int bottomLeft = y * width + x;
                    int bottomRight = bottomLeft + 1;
                    int topLeft = (y + 1) * width + x;
                    int topRight = topLeft + 1;

                    // 第1三角形
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = bottomRight;

                    // 第2三角形
                    triangles[triangleIndex++] = bottomRight;
                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = topRight;
                }
            }

            // メッシュを作成
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// 地形をクリア
        /// </summary>
        [ContextMenu("Clear Terrain")]
        public void ClearTerrain()
        {
            if (terrainObject != null)
            {
                DestroyImmediate(terrainObject);
                terrainObject = null;
            }

            if (terrainMesh != null)
            {
                DestroyImmediate(terrainMesh);
                terrainMesh = null;
            }

            Debug.Log("Terrain cleared");
        }

        void OnDestroy()
        {
            ClearTerrain();
        }
    }
}
