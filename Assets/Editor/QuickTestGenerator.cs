using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;

namespace Vastcore.Editor.Test
{
    public class QuickTestGenerator
    {
        [MenuItem("Vastcore/Quick Test/Create Basic Cube")]
        public static void CreateBasicCube()
        {
            // ProBuilderメッシュを作成
            ProBuilderMesh pbMesh = ProBuilderMesh.Create();
            
            // 基本的なキューブの頂点
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-1, -1, -1), new Vector3(1, -1, -1), new Vector3(1, 1, -1), new Vector3(-1, 1, -1),
                new Vector3(-1, -1, 1), new Vector3(1, -1, 1), new Vector3(1, 1, 1), new Vector3(-1, 1, 1)
            };
            
            // 面を定義（三角形で構成）
            Face[] faces = new Face[]
            {
                // 前面 (2つの三角形)
                new Face(new int[] { 0, 1, 2 }), new Face(new int[] { 0, 2, 3 }),
                // 後面 (2つの三角形)
                new Face(new int[] { 4, 7, 6 }), new Face(new int[] { 4, 6, 5 }),
                // 底面 (2つの三角形)
                new Face(new int[] { 0, 4, 5 }), new Face(new int[] { 0, 5, 1 }),
                // 上面 (2つの三角形)
                new Face(new int[] { 2, 6, 7 }), new Face(new int[] { 2, 7, 3 }),
                // 左面 (2つの三角形)
                new Face(new int[] { 0, 3, 7 }), new Face(new int[] { 0, 7, 4 }),
                // 右面 (2つの三角形)
                new Face(new int[] { 1, 5, 6 }), new Face(new int[] { 1, 6, 2 })
            };
            
            // メッシュを再構築
            pbMesh.RebuildWithPositionsAndFaces(vertices, faces);
            pbMesh.ToMesh();
            pbMesh.Refresh();
            
            // 名前と位置を設定
            pbMesh.name = "QuickTestCube";
            pbMesh.transform.position = new Vector3(0, 1, 0);
            
            // 選択状態にする
            Selection.activeGameObject = pbMesh.gameObject;
            
            Debug.Log("Quick Test Cube created successfully!");
        }
        
        [MenuItem("Vastcore/Quick Test/Create Test Array")]
        public static void CreateTestArray()
        {
            GameObject parent = new GameObject("QuickTestArray");
            
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    // ProBuilderメッシュを作成
                    ProBuilderMesh pbMesh = ProBuilderMesh.Create();
                    
                    // 基本的なキューブの頂点
                    Vector3[] vertices = new Vector3[]
                    {
                        new Vector3(-0.4f, -0.4f, -0.4f), new Vector3(0.4f, -0.4f, -0.4f), 
                        new Vector3(0.4f, 0.4f, -0.4f), new Vector3(-0.4f, 0.4f, -0.4f),
                        new Vector3(-0.4f, -0.4f, 0.4f), new Vector3(0.4f, -0.4f, 0.4f), 
                        new Vector3(0.4f, 0.4f, 0.4f), new Vector3(-0.4f, 0.4f, 0.4f)
                    };
                    
                    // 面を定義（三角形で構成）
                    Face[] faces = new Face[]
                    {
                        // 前面 (2つの三角形)
                        new Face(new int[] { 0, 1, 2 }), new Face(new int[] { 0, 2, 3 }),
                        // 後面 (2つの三角形)
                        new Face(new int[] { 4, 7, 6 }), new Face(new int[] { 4, 6, 5 }),
                        // 底面 (2つの三角形)
                        new Face(new int[] { 0, 4, 5 }), new Face(new int[] { 0, 5, 1 }),
                        // 上面 (2つの三角形)
                        new Face(new int[] { 2, 6, 7 }), new Face(new int[] { 2, 7, 3 }),
                        // 左面 (2つの三角形)
                        new Face(new int[] { 0, 3, 7 }), new Face(new int[] { 0, 7, 4 }),
                        // 右面 (2つの三角形)
                        new Face(new int[] { 1, 5, 6 }), new Face(new int[] { 1, 6, 2 })
                    };
                    
                    // メッシュを再構築
                    pbMesh.RebuildWithPositionsAndFaces(vertices, faces);
                    pbMesh.ToMesh();
                    pbMesh.Refresh();
                    
                    // 名前と位置を設定
                    pbMesh.name = $"TestCube_{i}_{j}";
                    pbMesh.transform.SetParent(parent.transform);
                    pbMesh.transform.localPosition = new Vector3(i * 2, 1, j * 2);
                    
                    // ランダムな高さ
                    float randomHeight = Random.Range(0.5f, 2f);
                    pbMesh.transform.localScale = new Vector3(1, randomHeight, 1);
                }
            }
            
            Debug.Log("Quick Test Array created successfully!");
        }
        
        [MenuItem("Vastcore/Quick Test/Test Boolean Operation")]
        public static void TestBooleanOperation()
        {
            // 2つのキューブを作成してBoolean演算をテスト
            CreateBasicCube();
            GameObject cubeA = Selection.activeGameObject;
            cubeA.name = "BooleanTestA";
            cubeA.transform.position = new Vector3(-0.5f, 1, 0);
            
            CreateBasicCube();
            GameObject cubeB = Selection.activeGameObject;
            cubeB.name = "BooleanTestB";
            cubeB.transform.position = new Vector3(0.5f, 1, 0);
            
            Debug.Log("Boolean test objects created. Use Structure Generator Operations tab to test Boolean operations.");
        }
    }
} 