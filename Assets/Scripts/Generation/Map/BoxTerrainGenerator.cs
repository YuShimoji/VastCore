using UnityEngine;
using System.Collections.Generic;
using Vastcore.Generation;

namespace Vastcore.Generation
{
    /// <summary>
    /// 箱型地形生成器
    /// 六面体の箱型オブジェクトを地形として扱う
    /// </summary>
    public class BoxTerrainGenerator : MonoBehaviour
    {
        [Header("箱型設定")]
        public Vector3 boxSize = new Vector3(1000f, 500f, 1000f);
        public int subdivisionLevel = 4;
        public bool enableInterior = false;

        [Header("面設定")]
        public bool generateTopFace = true;
        public bool generateBottomFace = true;
        public bool generateSideFaces = true;

        [Header("テクスチャ設定")]
        public Material topMaterial;
        public Material bottomMaterial;
        public Material sideMaterial;

        [Header("地形統合")]
        public bool alignToTerrainHeight = true;
        public UnityEngine.Terrain referenceTerrain;
        public float groundOffset = 0f;

        // 生成されたメッシュ
        private Dictionary<BoxFace, Mesh> generatedMeshes = new Dictionary<BoxFace, Mesh>();
        private Dictionary<BoxFace, GameObject> faceObjects = new Dictionary<BoxFace, GameObject>();

        /// <summary>
        /// 箱型地形の生成
        /// </summary>
        public void GenerateBoxTerrain(Vector3 centerPosition)
        {
            ClearExistingMeshes();

            if (generateTopFace)
                GenerateFace(BoxFace.Top, centerPosition);
            if (generateBottomFace)
                GenerateFace(BoxFace.Bottom, centerPosition);
            if (generateSideFaces)
            {
                GenerateFace(BoxFace.Front, centerPosition);
                GenerateFace(BoxFace.Back, centerPosition);
                GenerateFace(BoxFace.Left, centerPosition);
                GenerateFace(BoxFace.Right, centerPosition);
            }

            if (enableInterior)
                GenerateInterior(centerPosition);
        }

        /// <summary>
        /// 特定の面を生成
        /// </summary>
        private void GenerateFace(BoxFace face, Vector3 centerPosition)
        {
            GameObject faceObject = new GameObject($"BoxTerrain_{face}");
            faceObject.transform.position = centerPosition;
            faceObject.transform.SetParent(transform);

            MeshFilter meshFilter = faceObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = faceObject.AddComponent<MeshRenderer>();
            MeshCollider meshCollider = faceObject.AddComponent<MeshCollider>();

            // 面ごとの位置と回転を設定
            SetupFaceTransform(faceObject.transform, face, centerPosition);

            // メッシュ生成
            Mesh mesh = GenerateFaceMesh(face);
            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;

            // マテリアル設定
            meshRenderer.material = GetFaceMaterial(face);

            // LOD設定
            if (faceObject.GetComponent<PrimitiveTerrainObject>() == null)
            {
                faceObject.AddComponent<PrimitiveTerrainObject>();
            }

            generatedMeshes[face] = mesh;
            faceObjects[face] = faceObject;
        }

        /// <summary>
        /// 面のトランスフォーム設定
        /// </summary>
        private void SetupFaceTransform(Transform faceTransform, BoxFace face, Vector3 centerPosition)
        {
            float height = alignToTerrainHeight && referenceTerrain != null ?
                referenceTerrain.SampleHeight(centerPosition) + groundOffset : centerPosition.y;

            switch (face)
            {
                case BoxFace.Top:
                    faceTransform.position = new Vector3(centerPosition.x, height + boxSize.y / 2, centerPosition.z);
                    faceTransform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                    break;

                case BoxFace.Bottom:
                    faceTransform.position = new Vector3(centerPosition.x, height - boxSize.y / 2, centerPosition.z);
                    faceTransform.rotation = Quaternion.Euler(90f, 0f, 0f);
                    break;

                case BoxFace.Front:
                    faceTransform.position = new Vector3(centerPosition.x, height, centerPosition.z + boxSize.z / 2);
                    faceTransform.rotation = Quaternion.Euler(0f, 0f, 0f);
                    break;

                case BoxFace.Back:
                    faceTransform.position = new Vector3(centerPosition.x, height, centerPosition.z - boxSize.z / 2);
                    faceTransform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    break;

                case BoxFace.Left:
                    faceTransform.position = new Vector3(centerPosition.x - boxSize.x / 2, height, centerPosition.z);
                    faceTransform.rotation = Quaternion.Euler(0f, -90f, 0f);
                    break;

                case BoxFace.Right:
                    faceTransform.position = new Vector3(centerPosition.x + boxSize.x / 2, height, centerPosition.z);
                    faceTransform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    break;
            }
        }

        /// <summary>
        /// 面のメッシュ生成
        /// </summary>
        private Mesh GenerateFaceMesh(BoxFace face)
        {
            Mesh mesh = new Mesh();
            mesh.name = $"BoxTerrain_{face}_Mesh";

            // 面のサイズを決定
            Vector2 faceSize = GetFaceSize(face);

            // 頂点生成（サブディビジョン対応）
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            int segmentsX = Mathf.Max(1, subdivisionLevel);
            int segmentsZ = Mathf.Max(1, subdivisionLevel);

            // 頂点生成
            for (int z = 0; z <= segmentsZ; z++)
            {
                for (int x = 0; x <= segmentsX; x++)
                {
                    float u = (float)x / segmentsX;
                    float v = (float)z / segmentsZ;

                    Vector3 vertex = new Vector3(
                        (u - 0.5f) * faceSize.x,
                        0f,
                        (v - 0.5f) * faceSize.y
                    );

                    vertices.Add(vertex);
                    uvs.Add(new Vector2(u, v));
                }
            }

            // 三角形生成
            for (int z = 0; z < segmentsZ; z++)
            {
                for (int x = 0; x < segmentsX; x++)
                {
                    int topLeft = z * (segmentsX + 1) + x;
                    int topRight = topLeft + 1;
                    int bottomLeft = (z + 1) * (segmentsX + 1) + x;
                    int bottomRight = bottomLeft + 1;

                    // 最初の三角形
                    triangles.Add(topLeft);
                    triangles.Add(bottomLeft);
                    triangles.Add(topRight);

                    // 二番目の三角形
                    triangles.Add(topRight);
                    triangles.Add(bottomLeft);
                    triangles.Add(bottomRight);
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// 面ごとのサイズ取得
        /// </summary>
        private Vector2 GetFaceSize(BoxFace face)
        {
            switch (face)
            {
                case BoxFace.Top:
                case BoxFace.Bottom:
                    return new Vector2(boxSize.x, boxSize.z);

                case BoxFace.Front:
                case BoxFace.Back:
                    return new Vector2(boxSize.x, boxSize.y);

                case BoxFace.Left:
                case BoxFace.Right:
                    return new Vector2(boxSize.z, boxSize.y);

                default:
                    return new Vector2(boxSize.x, boxSize.z);
            }
        }

        /// <summary>
        /// 面ごとのマテリアル取得
        /// </summary>
        private Material GetFaceMaterial(BoxFace face)
        {
            switch (face)
            {
                case BoxFace.Top:
                    return topMaterial != null ? topMaterial : GetDefaultMaterial();

                case BoxFace.Bottom:
                    return bottomMaterial != null ? bottomMaterial : GetDefaultMaterial();

                default:
                    return sideMaterial != null ? sideMaterial : GetDefaultMaterial();
            }
        }

        /// <summary>
        /// デフォルトマテリアル取得
        /// </summary>
        private Material GetDefaultMaterial()
        {
            // TerrainEngineからマテリアルを取得するか、デフォルトを作成
            TerrainEngine engine = FindObjectOfType<TerrainEngine>();
            if (engine != null && engine.boxTerrainMaterial != null)
            {
                return engine.boxTerrainMaterial;
            }

            // デフォルトマテリアル作成
            Material defaultMat = new Material(Shader.Find("Standard"));
            defaultMat.color = Color.gray;
            return defaultMat;
        }

        /// <summary>
        /// 内部空間生成（オプション）
        /// </summary>
        private void GenerateInterior(Vector3 centerPosition)
        {
            // 内部空間の生成（壁のない箱型空間）
            // 必要に応じて実装
        }

        /// <summary>
        /// 既存メッシュのクリア
        /// </summary>
        public void ClearExistingMeshes()
        {
            foreach (var faceObject in faceObjects.Values)
            {
                if (faceObject != null)
                {
                    DestroyImmediate(faceObject);
                }
            }

            foreach (var mesh in generatedMeshes.Values)
            {
                if (mesh != null)
                {
                    DestroyImmediate(mesh);
                }
            }

            faceObjects.Clear();
            generatedMeshes.Clear();
        }

        /// <summary>
        /// 特定の面の更新
        /// </summary>
        public void UpdateFace(BoxFace face, Vector3 centerPosition)
        {
            if (faceObjects.ContainsKey(face))
            {
                DestroyImmediate(faceObjects[face]);
                faceObjects.Remove(face);
            }

            switch (face)
            {
                case BoxFace.Top:
                    if (generateTopFace) GenerateFace(face, centerPosition);
                    break;
                case BoxFace.Bottom:
                    if (generateBottomFace) GenerateFace(face, centerPosition);
                    break;
                default:
                    if (generateSideFaces) GenerateFace(face, centerPosition);
                    break;
            }
        }

        /// <summary>
        /// 地形データ適用
        /// </summary>
        public void ApplyTerrainData(TerrainData terrainData, BoxFace face)
        {
            if (!faceObjects.ContainsKey(face)) return;

            GameObject faceObject = faceObjects[face];
            MeshFilter meshFilter = faceObject.GetComponent<MeshFilter>();

            if (meshFilter != null && terrainData != null)
            {
                // 地形データをメッシュに適用
                // 高さマップから頂点変位を計算
                ApplyHeightmapToMesh(meshFilter.sharedMesh, terrainData, face);
            }
        }

        /// <summary>
        /// ハイトマップをメッシュに適用
        /// </summary>
        private void ApplyHeightmapToMesh(Mesh mesh, TerrainData terrainData, BoxFace face)
        {
            Vector3[] vertices = mesh.vertices;
            float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

            // 面に応じた頂点変位適用
            for (int i = 0; i < vertices.Length; i++)
            {
                // ローカル座標をワールド座標に変換してハイトマップ参照
                Vector3 worldPos = transform.TransformPoint(vertices[i]);

                int x = Mathf.Clamp(Mathf.RoundToInt((worldPos.x / boxSize.x + 0.5f) * terrainData.heightmapResolution), 0, terrainData.heightmapResolution - 1);
                int z = Mathf.Clamp(Mathf.RoundToInt((worldPos.z / boxSize.z + 0.5f) * terrainData.heightmapResolution), 0, terrainData.heightmapResolution - 1);

                float height = heights[z, x] * terrainData.heightmapScale.y;

                // 面に応じた方向に変位
                switch (face)
                {
                    case BoxFace.Top:
                        vertices[i].y = height;
                        break;
                    case BoxFace.Bottom:
                        vertices[i].y = -height;
                        break;
                    // 他の面の場合は法線方向に変位
                    default:
                        vertices[i] += mesh.normals[i] * height * 0.1f;
                        break;
                }
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }

    /// <summary>
    /// 箱型の面タイプ
    /// </summary>
    public enum BoxFace
    {
        Top,
        Bottom,
        Front,
        Back,
        Left,
        Right
    }
}
