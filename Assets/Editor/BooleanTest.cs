using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using Parabox.CSG;
using System.Linq;

namespace Vastcore.Editor.Test
{
    public class BooleanTest
    {
        [MenuItem("Vastcore/Boolean Test/Test CSG Subtract")]
        public static void TestCSGSubtract()
        {
            try
            {
                // 2つのキューブを作成
                var cubeA = ShapeGenerator.CreateShape(ShapeType.Cube);
                if (cubeA == null)
                {
                    Debug.LogError("Failed to create cubeA");
                    return;
                }
                cubeA.name = "CubeA";
                cubeA.transform.position = new Vector3(-0.5f, 0, 0);
                
                var cubeB = ShapeGenerator.CreateShape(ShapeType.Cube);
                if (cubeB == null)
                {
                    Debug.LogError("Failed to create cubeB");
                    return;
                }
                cubeB.name = "CubeB";
                cubeB.transform.position = new Vector3(0.5f, 0, 0);
                
                // MeshFilterとMeshRendererを確保
                EnsureMeshComponents(cubeA.gameObject);
                EnsureMeshComponents(cubeB.gameObject);
                
                // CSG演算を実行
                Parabox.CSG.Model csgResult = CSG.Subtract(cubeA.gameObject, cubeB.gameObject);
                
                if (csgResult != null)
                {
                    // シンプルな方法でGameObjectを作成
                    var materials = csgResult.materials?.ToArray() ?? new Material[0];
                    ProBuilderMesh pb = ProBuilderMesh.Create();
                    pb.GetComponent<MeshFilter>().sharedMesh = (Mesh)csgResult;
                    pb.GetComponent<MeshRenderer>().sharedMaterials = materials;
                    
                    // 基本的なメッシュ更新のみ
                    pb.ToMesh();
                    pb.Refresh();
                    
                    // 名前を設定
                    pb.gameObject.name = "Boolean_Subtract_Result";
                    pb.transform.position = new Vector3(3, 0, 0);
                    
                    // 結果を選択
                    Selection.activeGameObject = pb.gameObject;
                    
                    Debug.Log("Boolean Subtract test completed successfully!");
                }
                else
                {
                    Debug.LogError("CSG Subtract failed - result is null");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"CSG Subtract test failed: {ex.Message}");
                if (ex.StackTrace != null)
                {
                    Debug.LogError($"Stack trace: {ex.StackTrace}");
                }
            }
        }
        
        [MenuItem("Vastcore/Boolean Test/Test CSG Union")]
        public static void TestCSGUnion()
        {
            try
            {
                // 2つのキューブを作成
                var cubeA = ShapeGenerator.CreateShape(ShapeType.Cube);
                if (cubeA == null)
                {
                    Debug.LogError("Failed to create cubeA");
                    return;
                }
                cubeA.name = "CubeA_Union";
                cubeA.transform.position = new Vector3(-0.5f, 2, 0);
                
                var cubeB = ShapeGenerator.CreateShape(ShapeType.Cube);
                if (cubeB == null)
                {
                    Debug.LogError("Failed to create cubeB");
                    return;
                }
                cubeB.name = "CubeB_Union";
                cubeB.transform.position = new Vector3(0.5f, 2, 0);
                
                // MeshFilterとMeshRendererを確保
                EnsureMeshComponents(cubeA.gameObject);
                EnsureMeshComponents(cubeB.gameObject);
                
                // CSG演算を実行
                Parabox.CSG.Model csgResult = CSG.Union(cubeA.gameObject, cubeB.gameObject);
                
                if (csgResult != null)
                {
                    // シンプルな方法でGameObjectを作成
                    var materials = csgResult.materials?.ToArray() ?? new Material[0];
                    ProBuilderMesh pb = ProBuilderMesh.Create();
                    pb.GetComponent<MeshFilter>().sharedMesh = (Mesh)csgResult;
                    pb.GetComponent<MeshRenderer>().sharedMaterials = materials;
                    
                    // 基本的なメッシュ更新のみ
                    pb.ToMesh();
                    pb.Refresh();
                    
                    // 名前を設定
                    pb.gameObject.name = "Boolean_Union_Result";
                    pb.transform.position = new Vector3(3, 2, 0);
                    
                    // 結果を選択
                    Selection.activeGameObject = pb.gameObject;
                    
                    Debug.Log("Boolean Union test completed successfully!");
                }
                else
                {
                    Debug.LogError("CSG Union failed - result is null");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"CSG Union test failed: {ex.Message}");
                if (ex.StackTrace != null)
                {
                    Debug.LogError($"Stack trace: {ex.StackTrace}");
                }
            }
        }
        
        [MenuItem("Vastcore/Boolean Test/Test CSG Intersect")]
        public static void TestCSGIntersect()
        {
            try
            {
                // 2つのキューブを作成
                var cubeA = ShapeGenerator.CreateShape(ShapeType.Cube);
                if (cubeA == null)
                {
                    Debug.LogError("Failed to create cubeA");
                    return;
                }
                cubeA.name = "CubeA_Intersect";
                cubeA.transform.position = new Vector3(-0.5f, 4, 0);
                
                var cubeB = ShapeGenerator.CreateShape(ShapeType.Cube);
                if (cubeB == null)
                {
                    Debug.LogError("Failed to create cubeB");
                    return;
                }
                cubeB.name = "CubeB_Intersect";
                cubeB.transform.position = new Vector3(0.5f, 4, 0);
                
                // MeshFilterとMeshRendererを確保
                EnsureMeshComponents(cubeA.gameObject);
                EnsureMeshComponents(cubeB.gameObject);
                
                // CSG演算を実行
                Parabox.CSG.Model csgResult = CSG.Intersect(cubeA.gameObject, cubeB.gameObject);
                
                if (csgResult != null)
                {
                    // シンプルな方法でGameObjectを作成
                    var materials = csgResult.materials?.ToArray() ?? new Material[0];
                    ProBuilderMesh pb = ProBuilderMesh.Create();
                    pb.GetComponent<MeshFilter>().sharedMesh = (Mesh)csgResult;
                    pb.GetComponent<MeshRenderer>().sharedMaterials = materials;
                    
                    // 基本的なメッシュ更新のみ
                    pb.ToMesh();
                    pb.Refresh();
                    
                    // 名前を設定
                    pb.gameObject.name = "Boolean_Intersect_Result";
                    pb.transform.position = new Vector3(3, 4, 0);
                    
                    // 結果を選択
                    Selection.activeGameObject = pb.gameObject;
                    
                    Debug.Log("Boolean Intersect test completed successfully!");
                }
                else
                {
                    Debug.LogError("CSG Intersect failed - result is null");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"CSG Intersect test failed: {ex.Message}");
                if (ex.StackTrace != null)
                {
                    Debug.LogError($"Stack trace: {ex.StackTrace}");
                }
            }
        }
        
        [MenuItem("Vastcore/Boolean Test/Clean Test Objects")]
        public static void CleanTestObjects()
        {
            // テストオブジェクトを削除（新しいAPI使用）
            var testObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(go => go.name.Contains("Cube") || go.name.Contains("Boolean_"))
                .ToArray();
                
            foreach (var obj in testObjects)
            {
                Object.DestroyImmediate(obj);
            }
            
            Debug.Log($"Cleaned {testObjects.Length} test objects");
        }
        
        private static void EnsureMeshComponents(GameObject go)
        {
            if (go == null) return;
            
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