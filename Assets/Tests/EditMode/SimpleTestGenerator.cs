using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using System.Collections.Generic;

namespace Vastcore.Editor.Test
{
    public class SimpleTestGenerator
    {
        [MenuItem("Vastcore/Simple Test/Create Basic Cube")]
        public static void CreateBasicCube()
        {
            // ProBuilderのShapeGeneratorを使用
            var cube = ShapeGenerator.CreateShape(ShapeType.Cube);
            cube.name = "SimpleTestCube";
            cube.transform.position = Vector3.zero;
            
            // MeshFilterとMeshRendererを確保
            EnsureMeshComponents(cube.gameObject);
            
            Selection.activeGameObject = cube.gameObject;
            Debug.Log("Simple test cube created successfully!");
        }

        [MenuItem("Vastcore/Simple Test/Create Basic Cylinder")]
        public static void CreateBasicCylinder()
        {
            // ProBuilderのShapeGeneratorを使用
            var cylinder = ShapeGenerator.CreateShape(ShapeType.Cylinder);
            cylinder.name = "SimpleTestCylinder";
            cylinder.transform.position = Vector3.zero;
            
            // MeshFilterとMeshRendererを確保
            EnsureMeshComponents(cylinder.gameObject);
            
            Selection.activeGameObject = cylinder.gameObject;
            Debug.Log("Simple test cylinder created successfully!");
        }

        [MenuItem("Vastcore/Simple Test/Create Test Grid")]
        public static void CreateTestGrid()
        {
            GameObject parent = new GameObject("SimpleTestGrid");
            
            for (int x = 0; x < 5; x++)
            {
                for (int z = 0; z < 5; z++)
                {
                    // ProBuilderのShapeGeneratorを使用
                    var cube = ShapeGenerator.CreateShape(ShapeType.Cube);
                    cube.name = $"GridCube_{x}_{z}";
                    cube.transform.SetParent(parent.transform);
                    cube.transform.localPosition = new Vector3(x * 2.5f, 0, z * 2.5f);
                    
                    // ランダムな高さを設定
                    float randomHeight = Random.Range(0.5f, 3f);
                    cube.transform.localScale = new Vector3(1, randomHeight, 1);
                    
                    // MeshFilterとMeshRendererを確保
                    EnsureMeshComponents(cube.gameObject);
                }
            }
            
            Debug.Log("Simple test grid created successfully!");
        }

        [MenuItem("Vastcore/Simple Test/Create Custom Shapes")]
        public static void CreateCustomShapes()
        {
            GameObject parent = new GameObject("CustomShapes");
            
            // カスタムキューブ
            var customCube = CreateCustomCube("CustomCube", new Vector3(2, 1, 2));
            customCube.transform.SetParent(parent.transform);
            customCube.transform.localPosition = new Vector3(-5, 0, 0);
            
            // カスタムプリズム
            var customPrism = CreateCustomPrism("CustomPrism", 1.5f, 2f, 6);
            customPrism.transform.SetParent(parent.transform);
            customPrism.transform.localPosition = new Vector3(0, 0, 0);
            
            // カスタムピラミッド
            var customPyramid = CreateCustomPyramid("CustomPyramid", 2f, 3f);
            customPyramid.transform.SetParent(parent.transform);
            customPyramid.transform.localPosition = new Vector3(5, 0, 0);
            
            Debug.Log("Custom shapes created successfully!");
        }

        private static ProBuilderMesh CreateCustomCube(string name, Vector3 size)
        {
            Vector3 halfSize = size * 0.5f;
            
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-halfSize.x, -halfSize.y, -halfSize.z),
                new Vector3( halfSize.x, -halfSize.y, -halfSize.z),
                new Vector3( halfSize.x,  halfSize.y, -halfSize.z),
                new Vector3(-halfSize.x,  halfSize.y, -halfSize.z),
                new Vector3(-halfSize.x, -halfSize.y,  halfSize.z),
                new Vector3( halfSize.x, -halfSize.y,  halfSize.z),
                new Vector3( halfSize.x,  halfSize.y,  halfSize.z),
                new Vector3(-halfSize.x,  halfSize.y,  halfSize.z)
            };

            Face[] faces = new Face[]
            {
                new Face(new int[] { 0, 3, 2, 1 }), // 前面
                new Face(new int[] { 4, 5, 6, 7 }), // 後面
                new Face(new int[] { 0, 4, 7, 3 }), // 左面
                new Face(new int[] { 1, 2, 6, 5 }), // 右面
                new Face(new int[] { 3, 7, 6, 2 }), // 上面
                new Face(new int[] { 0, 1, 5, 4 })  // 底面
            };

            ProBuilderMesh pb = ProBuilderMesh.Create(vertices, faces);
            pb.name = name;
            pb.ToMesh();
            pb.Refresh();
            
            // MeshFilterとMeshRendererを確保
            EnsureMeshComponents(pb.gameObject);
            
            return pb;
        }

        private static ProBuilderMesh CreateCustomPrism(string name, float radius, float height, int sides)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Face> faces = new List<Face>();
            
            float halfHeight = height * 0.5f;
            
            // 底面の頂点
            for (int i = 0; i < sides; i++)
            {
                float angle = (float)i / sides * Mathf.PI * 2;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, -halfHeight, Mathf.Sin(angle) * radius));
            }
            
            // 上面の頂点
            for (int i = 0; i < sides; i++)
            {
                float angle = (float)i / sides * Mathf.PI * 2;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, halfHeight, Mathf.Sin(angle) * radius));
            }
            
            // 底面
            int[] bottomFace = new int[sides];
            for (int i = 0; i < sides; i++)
            {
                bottomFace[i] = i;
            }
            faces.Add(new Face(bottomFace));
            
            // 上面
            int[] topFace = new int[sides];
            for (int i = 0; i < sides; i++)
            {
                topFace[i] = sides + (sides - 1 - i);
            }
            faces.Add(new Face(topFace));
            
            // 側面
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                faces.Add(new Face(new int[] { i, next, sides + next, sides + i }));
            }
            
            ProBuilderMesh pb = ProBuilderMesh.Create(vertices.ToArray(), faces.ToArray());
            pb.name = name;
            pb.ToMesh();
            pb.Refresh();
            
            // MeshFilterとMeshRendererを確保
            EnsureMeshComponents(pb.gameObject);
            
            return pb;
        }

        private static ProBuilderMesh CreateCustomPyramid(string name, float baseSize, float height)
        {
            float halfBase = baseSize * 0.5f;
            
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-halfBase, 0, -halfBase), // 底面
                new Vector3( halfBase, 0, -halfBase),
                new Vector3( halfBase, 0,  halfBase),
                new Vector3(-halfBase, 0,  halfBase),
                new Vector3(0, height, 0) // 頂点
            };

            Face[] faces = new Face[]
            {
                new Face(new int[] { 0, 3, 2, 1 }), // 底面
                new Face(new int[] { 0, 1, 4 }),    // 前面
                new Face(new int[] { 1, 2, 4 }),    // 右面
                new Face(new int[] { 2, 3, 4 }),    // 後面
                new Face(new int[] { 3, 0, 4 })     // 左面
            };

            ProBuilderMesh pb = ProBuilderMesh.Create(vertices, faces);
            pb.name = name;
            pb.ToMesh();
            pb.Refresh();
            
            // MeshFilterとMeshRendererを確保
            EnsureMeshComponents(pb.gameObject);
            
            return pb;
        }

        private static void EnsureMeshComponents(GameObject go)
        {
            // MeshFilterを確保
            var meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = go.AddComponent<MeshFilter>();
            }
            
            // MeshRendererを確保
            var meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = go.AddComponent<MeshRenderer>();
                // デフォルトマテリアルを設定
                meshRenderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            }
            
            // ProBuilderMeshがある場合、メッシュを更新
            var pbMesh = go.GetComponent<ProBuilderMesh>();
            if (pbMesh != null)
            {
                pbMesh.ToMesh();
                pbMesh.Refresh();
            }
        }
    }
} 