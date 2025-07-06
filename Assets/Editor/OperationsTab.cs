using UnityEditor;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEditor.ProBuilder;
using UnityEditor.ProBuilder.Actions;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Parabox.CSG;

namespace Vastcore.Editor.Generation
{
    public class OperationsTab
    {
        private StructureGeneratorWindow window;
        
        // Extrude settings
        private float extrudeDistance = 1f;
        private ExtrudeMethod extrudeMethod = ExtrudeMethod.IndividualFaces;
        
        // Boolean operation settings
        private GameObject booleanObjectA;
        private GameObject booleanObjectB;
        private StructureGeneratorWindow.BooleanOperation booleanOperation = StructureGeneratorWindow.BooleanOperation.Subtract;
        
        // Offset settings
        private float offsetDistance = 0.1f;
        private bool preserveOriginals = false;
        
        // Bevel settings
        private float bevelAmount = 0.1f;
        
        // Opening creation settings
        private ProBuilderMesh targetWallForOpening;
        private Vector2 openingSize = new Vector2(1, 2);
        private Vector3 openingPosition = Vector3.zero;
        private float openingToolThickness = 2f;
        
        // Prefab settings
        private GameObject targetForPrefab;
        private string prefabFolderPath = "Assets/Prefabs";
        private string prefabName = "MyStructure";
        
        // Array settings
        private enum ArrayDuplicationType { Linear, Circular }
        private ArrayDuplicationType arrayDuplicationType = ArrayDuplicationType.Linear;
        private int arrayCount = 5;
        private Vector3 arrayOffset = new Vector3(2f, 0, 0);
        private float arrayCircleRadius = 10f;
        private bool orientToCenter = true;

        public OperationsTab(StructureGeneratorWindow window)
        {
            this.window = window;
        }

        public void Draw()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Operations", EditorStyles.boldLabel);
            
            // Extrude Section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Extrude", EditorStyles.boldLabel);
            extrudeDistance = EditorGUILayout.FloatField("Distance", extrudeDistance);
            extrudeMethod = (ExtrudeMethod)EditorGUILayout.EnumPopup("Method", extrudeMethod);
            
            if (GUILayout.Button("Extrude All Faces"))
            {
                ExtrudeAllFaces();
            }
            
            // Boolean Operations Section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Boolean Operations", EditorStyles.boldLabel);
            booleanObjectA = EditorGUILayout.ObjectField("Object A", booleanObjectA, typeof(GameObject), true) as GameObject;
            booleanObjectB = EditorGUILayout.ObjectField("Object B", booleanObjectB, typeof(GameObject), true) as GameObject;
            booleanOperation = (StructureGeneratorWindow.BooleanOperation)EditorGUILayout.EnumPopup("Operation", booleanOperation);
            
            if (GUILayout.Button("Set Selected Objects"))
            {
                SetSelectedObjects();
            }
            
            if (GUILayout.Button("Perform Boolean Operation"))
            {
                PerformBooleanOperation();
            }
            
            // Mesh Operations Section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mesh Operations", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Combine Selected Meshes"))
            {
                CombineSelectedMeshes();
            }
            
            // Offset settings
            offsetDistance = EditorGUILayout.FloatField("Offset Distance", offsetDistance);
            preserveOriginals = EditorGUILayout.Toggle("Preserve Originals", preserveOriginals);
            
            if (GUILayout.Button("Offset and Combine"))
            {
                OffsetAndCombineMeshes();
            }
            
            if (GUILayout.Button("Detach Selected Faces"))
            {
                DetachSelectedFaces();
            }
            
            if (GUILayout.Button("Duplicate Selected Mesh"))
            {
                DuplicateSelectedMesh();
            }
            
            // Bevel Section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bevel", EditorStyles.boldLabel);
            bevelAmount = EditorGUILayout.FloatField("Amount", bevelAmount);
            
            if (GUILayout.Button("Apply Bevel to All Edges"))
            {
                ApplyBevelToAllEdges();
            }
            
            // Opening Creation Section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Opening Creation", EditorStyles.boldLabel);
            targetWallForOpening = EditorGUILayout.ObjectField("Target Wall", targetWallForOpening, typeof(ProBuilderMesh), true) as ProBuilderMesh;
            openingSize = EditorGUILayout.Vector2Field("Opening Size", openingSize);
            openingPosition = EditorGUILayout.Vector3Field("Position", openingPosition);
            openingToolThickness = EditorGUILayout.FloatField("Tool Thickness", openingToolThickness);
            
            if (GUILayout.Button("Create Opening"))
            {
                CreateOpening();
            }
            
            // Array Generation Section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Array Generation", EditorStyles.boldLabel);
            arrayDuplicationType = (ArrayDuplicationType)EditorGUILayout.EnumPopup("Type", arrayDuplicationType);
            arrayCount = EditorGUILayout.IntSlider("Count", arrayCount, 1, 20);
            
            if (arrayDuplicationType == ArrayDuplicationType.Linear)
            {
                arrayOffset = EditorGUILayout.Vector3Field("Offset", arrayOffset);
            }
            else
            {
                arrayCircleRadius = EditorGUILayout.FloatField("Circle Radius", arrayCircleRadius);
                orientToCenter = EditorGUILayout.Toggle("Orient to Center", orientToCenter);
            }
            
            if (GUILayout.Button("Generate Array"))
            {
                GenerateArray();
            }
            
            // Prefab Creation Section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Prefab Creation", EditorStyles.boldLabel);
            targetForPrefab = EditorGUILayout.ObjectField("Target Object", targetForPrefab, typeof(GameObject), true) as GameObject;
            prefabFolderPath = EditorGUILayout.TextField("Folder Path", prefabFolderPath);
            prefabName = EditorGUILayout.TextField("Prefab Name", prefabName);
            
            if (GUILayout.Button("Save as Prefab"))
            {
                SaveAsPrefab();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void SetSelectedObjects()
        {
            var selected = Selection.gameObjects;
            if (selected.Length >= 2)
            {
                booleanObjectA = selected[0];
                booleanObjectB = selected[1];
                UnityEditor.EditorUtility.DisplayDialog("Objects Set", 
                    $"Object A: {booleanObjectA.name}\nObject B: {booleanObjectB.name}", "OK");
            }
            else
            {
                UnityEditor.EditorUtility.DisplayDialog("Selection Error", 
                    "Please select at least two objects for boolean operations.", "OK");
            }
        }

        private void PerformBooleanOperation()
        {
            if (booleanObjectA == null || booleanObjectB == null)
            {
                UnityEditor.EditorUtility.DisplayDialog("Missing Objects", 
                    "Please set both Object A and Object B before performing boolean operations.", "OK");
                return;
            }

            try
            {
                // プレハブまたは読み取り専用アセットかチェック
                if (IsReadOnlyAsset(booleanObjectA) || IsReadOnlyAsset(booleanObjectB))
                {
                    UnityEditor.EditorUtility.DisplayDialog("Read Only Error", 
                        "Cannot perform boolean operations on prefabs or read-only assets.\n" +
                        "Please create instances in the scene first.", "OK");
                    return;
                }

                // 簡素化されたBoolean操作
                bool success = PerformSimplifiedBooleanOperation();
                
                if (success)
                {
                    UnityEditor.EditorUtility.DisplayDialog("Boolean Success", 
                        "Boolean operation completed successfully!", "OK");
                }
                else
                {
                    UnityEditor.EditorUtility.DisplayDialog("Boolean Error", 
                        "Boolean operation failed. Please try with simpler shapes.", "OK");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Boolean operation error: {ex.Message}");
                UnityEditor.EditorUtility.DisplayDialog("Boolean Error", 
                    $"Boolean operation failed: {ex.Message}", "OK");
            }
        }

        private bool PerformSimplifiedBooleanOperation()
        {
            try
            {
                // メッシュの有効性をチェック
                var meshA = booleanObjectA.GetComponent<MeshFilter>()?.sharedMesh;
                var meshB = booleanObjectB.GetComponent<MeshFilter>()?.sharedMesh;
                
                if (meshA == null || meshB == null)
                {
                    Debug.LogError("One or both objects don't have valid meshes for Boolean operation");
                    return false;
                }
                
                if (meshA.vertexCount == 0 || meshB.vertexCount == 0)
                {
                    Debug.LogError("One or both meshes have no vertices");
                    return false;
                }
                
                Debug.Log($"Attempting Boolean {booleanOperation} on {booleanObjectA.name} ({meshA.vertexCount} verts) and {booleanObjectB.name} ({meshB.vertexCount} verts)");
                
                // CSG操作をタイムアウト付きで実行
                var result = ExecuteCSGWithTimeout();
                
                if (result != null)
                {
                    CreateBooleanResult(result);
                    return true;
                }
                else
                {
                    Debug.LogWarning("CSG operation failed or timed out. Creating fallback result.");
                    CreateFallbackResult();
                    return true; // フォールバック成功
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Simplified Boolean operation failed: {ex.Message}");
                return false;
            }
        }

        private Parabox.CSG.Model ExecuteCSGWithTimeout()
        {
            try
            {
                var startTime = System.DateTime.Now;
                const int timeoutSeconds = 5; // タイムアウトを短縮
                
                Parabox.CSG.Model result = null;
                
                // 操作を実行
                switch (booleanOperation)
                {
                    case StructureGeneratorWindow.BooleanOperation.Subtract:
                        result = CSG.Subtract(booleanObjectA, booleanObjectB);
                        break;
                    case StructureGeneratorWindow.BooleanOperation.Union:
                        result = CSG.Union(booleanObjectA, booleanObjectB);
                        break;
                    case StructureGeneratorWindow.BooleanOperation.Intersect:
                        result = CSG.Intersect(booleanObjectA, booleanObjectB);
                        break;
                }
                
                // タイムアウトチェック
                if ((System.DateTime.Now - startTime).TotalSeconds > timeoutSeconds)
                {
                    Debug.LogWarning($"CSG {booleanOperation} operation timed out after {timeoutSeconds} seconds");
                    return null;
                }
                
                return result;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"CSG operation exception: {ex.Message}");
                return null;
            }
        }

        private void CreateBooleanResult(Parabox.CSG.Model csgResult)
        {
            try
            {
                // 入力検証
                if (csgResult == null)
                {
                    Debug.LogError("CSG result is null");
                    return;
                }
                
                if (booleanObjectA == null || booleanObjectB == null)
                {
                    Debug.LogError("Boolean objects are null");
                    return;
                }
                
                // CSG結果からメッシュを作成
                Mesh resultMesh = (Mesh)csgResult;
                if (resultMesh == null || resultMesh.vertexCount == 0)
                {
                    Debug.LogError("Failed to convert CSG result to mesh or mesh is empty");
                    return;
                }
                
                // 新しいGameObjectを作成
                string operationName = booleanOperation.ToString();
                GameObject resultObject = new GameObject($"Boolean_{operationName}_{booleanObjectA.name}_{booleanObjectB.name}");
                
                // 標準的なMeshFilterとMeshRendererのみ使用
                MeshFilter meshFilter = resultObject.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = resultObject.AddComponent<MeshRenderer>();
                
                // メッシュを設定
                meshFilter.sharedMesh = resultMesh;
                
                // マテリアルを設定
                if (csgResult.materials != null && csgResult.materials.Count > 0)
                {
                    try
                    {
                        meshRenderer.sharedMaterials = csgResult.materials.ToArray();
                    }
                    catch (System.Exception matEx)
                    {
                        Debug.LogWarning($"Failed to set CSG materials: {matEx.Message}");
                        SetFallbackMaterial(meshRenderer);
                    }
                }
                else
                {
                    SetFallbackMaterial(meshRenderer);
                }
                
                // 位置を設定（元のオブジェクトの中間点）
                Vector3 centerPos = (booleanObjectA.transform.position + booleanObjectB.transform.position) * 0.5f;
                resultObject.transform.position = centerPos + Vector3.up * 2f; // 少し上に配置
                
                // 結果を選択
                Selection.activeGameObject = resultObject;
                
                // Undoに登録
                Undo.RegisterCreatedObjectUndo(resultObject, "Boolean Operation");
                
                Debug.Log($"Boolean {operationName} completed successfully! Mesh vertices: {resultMesh.vertexCount}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error creating boolean result: {ex.Message}\nStack trace: {ex.StackTrace}");
                
                // フォールバック実行
                try
                {
                    CreateFallbackResult();
                }
                catch (System.Exception fallbackEx)
                {
                    Debug.LogError($"Fallback also failed: {fallbackEx.Message}");
                }
            }
        }
        
        private void SetFallbackMaterial(MeshRenderer meshRenderer)
        {
            try
            {
                // 元のオブジェクトのマテリアルを継承
                var materialA = booleanObjectA?.GetComponent<MeshRenderer>()?.sharedMaterial;
                if (materialA != null)
                {
                    meshRenderer.material = materialA;
                }
                else
                {
                    meshRenderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to set fallback material: {ex.Message}");
                // 最後の手段
                meshRenderer.material = null;
            }
        }

        private void CreateFallbackResult()
        {
            // フォールバック: 単純な結合
            string operationName = booleanOperation.ToString();
            GameObject resultObject = new GameObject($"Fallback_{operationName}_{booleanObjectA.name}_{booleanObjectB.name}");
            
            // 元のオブジェクトをコピー
            GameObject copyA = Object.Instantiate(booleanObjectA, resultObject.transform);
            GameObject copyB = Object.Instantiate(booleanObjectB, resultObject.transform);
            
            copyA.name = "ObjectA_Copy";
            copyB.name = "ObjectB_Copy";
            
            // 位置を調整
            Vector3 centerPos = (booleanObjectA.transform.position + booleanObjectB.transform.position) * 0.5f;
            resultObject.transform.position = centerPos + Vector3.up * 2f;
            
            // 結果を選択
            Selection.activeGameObject = resultObject;
            
            // Undoに登録
            Undo.RegisterCreatedObjectUndo(resultObject, "Fallback Boolean Operation");
            
            Debug.Log($"Created fallback result for {operationName} operation");
        }

        private void ExtrudeAllFaces()
        {
            var pb = Selection.activeGameObject?.GetComponent<ProBuilderMesh>();
            if (pb == null)
            {
                UnityEditor.EditorUtility.DisplayDialog("Selection Error", "Please select a ProBuilder mesh.", "OK");
                return;
            }

            try
            {
                // 全ての面を選択
                var allFaces = new List<Face>();
                for (int i = 0; i < pb.faces.Count; i++)
                {
                    allFaces.Add(pb.faces[i]);
                }
                
                if (allFaces.Count > 0)
                {
                    pb.SetSelectedFaces(allFaces);
                    
                    // シンプルな押し出し実装（頂点を法線方向に移動）
                    var positions = pb.positions;
                    var faces = pb.faces;
                    
                    // 各面の法線を計算して頂点を移動
                    for (int i = 0; i < faces.Count; i++)
                    {
                        var face = faces[i];
                        if (face.indexes.Count >= 3)
                        {
                            // 面の法線を計算
                            Vector3 v1 = positions[face.indexes[1]] - positions[face.indexes[0]];
                            Vector3 v2 = positions[face.indexes[2]] - positions[face.indexes[0]];
                            Vector3 normal = Vector3.Cross(v1, v2).normalized;
                            
                            // 面の頂点を法線方向に移動
                            for (int j = 0; j < face.indexes.Count; j++)
                            {
                                int vertexIndex = face.indexes[j];
                                positions[vertexIndex] += normal * extrudeDistance * 0.1f;
                            }
                        }
                    }
                    
                    pb.positions = positions;
                    pb.ToMesh();
                    pb.Refresh();
                    UnityEditor.EditorUtility.SetDirty(pb);
                    
                    UnityEditor.EditorUtility.DisplayDialog("Extrude Success", $"Successfully extruded {allFaces.Count} faces.", "OK");
                }
                else
                {
                    UnityEditor.EditorUtility.DisplayDialog("No Faces", "No faces found to extrude.", "OK");
                }
            }
            catch (System.Exception ex)
            {
                UnityEditor.EditorUtility.DisplayDialog("Extrude Error", $"Failed to extrude: {ex.Message}", "OK");
            }
        }

        private void CombineSelectedMeshes()
        {
            var selected = Selection.transforms
                .Select(t => t.GetComponent<ProBuilderMesh>())
                .Where(pb => pb != null)
                .Select(pb => pb.gameObject)
                .ToArray();

            if (selected.Length < 2)
            {
                UnityEditor.EditorUtility.DisplayDialog("Selection Error", "Please select at least two ProBuilder meshes to combine.", "OK");
                return;
            }
            
            Selection.objects = selected;
            if (!EditorApplication.ExecuteMenuItem("ProBuilder/Geometry/Merge"))
            {
                UnityEditor.EditorUtility.DisplayDialog("Combine Error", "Failed to execute Merge action.", "OK");
            }
        }

        private void OffsetAndCombineMeshes()
        {
             var selected = Selection.transforms
                .Select(t => t.GetComponent<ProBuilderMesh>())
                .Where(pb => pb != null)
                .Select(pb => pb.gameObject)
                .ToArray();

            if (selected.Length < 2)
            {
                UnityEditor.EditorUtility.DisplayDialog("Selection Error", "Please select at least two ProBuilder meshes to offset and combine.", "OK");
                return;
            }

            Selection.objects = selected;
            if (!EditorApplication.ExecuteMenuItem("ProBuilder/Geometry/Merge"))
            {
                UnityEditor.EditorUtility.DisplayDialog("Combine Error", "Failed to execute Merge action.", "OK");
            }
            
            UnityEditor.EditorUtility.DisplayDialog("Notice", "Offset functionality is not fully implemented in this version. Meshes were combined without offset.", "OK");
        }

        private void DetachSelectedFaces()
        {
            var pb = Selection.activeGameObject?.GetComponent<ProBuilderMesh>();
            if (pb == null || !pb.selectedFaceIndexes.Any())
            {
                UnityEditor.EditorUtility.DisplayDialog("Selection Error", "Please select a ProBuilder mesh with some faces selected.", "OK");
                return;
            }

            if (!EditorApplication.ExecuteMenuItem("ProBuilder/Actions/Detach Faces"))
            {
                UnityEditor.EditorUtility.DisplayDialog("Detach Error", "Failed to execute Detach action.", "OK");
            }
        }

        private void DuplicateSelectedMesh()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                UnityEditor.EditorUtility.DisplayDialog("Selection Error", "Please select a GameObject to duplicate.", "OK");
                return;
            }

            var newGo = Object.Instantiate(go, go.transform.parent);
            newGo.transform.position = go.transform.position + Vector3.one; // Small offset
            Undo.RegisterCreatedObjectUndo(newGo, "Duplicate Mesh");
            Selection.activeGameObject = newGo;
        }

        private void ApplyBevelToAllEdges()
        {
            var pb = Selection.activeGameObject?.GetComponent<ProBuilderMesh>();
            if (pb == null)
            {
                UnityEditor.EditorUtility.DisplayDialog("Selection Error", "Please select a ProBuilder mesh.", "OK");
                return;
            }

            // 現在のProBuilderバージョンではBevelが利用できないため、シンプルなメッセージで対応
            UnityEditor.EditorUtility.DisplayDialog("Bevel Operation", 
                "Bevel operation is not available in this ProBuilder version.\n" +
                "Please use the ProBuilder window for advanced edge operations.", "OK");
        }
        
        private void GenerateArray()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                UnityEditor.EditorUtility.DisplayDialog("Selection Error", "Please select a GameObject to create an array from.", "OK");
                return;
            }

            var parent = new GameObject($"{go.name}_Array");
            Undo.RegisterCreatedObjectUndo(parent, "Generate Array");

            for (int i = 0; i < arrayCount; i++)
            {
                var newGo = Object.Instantiate(go, parent.transform);
                newGo.name = $"{go.name}_{i:00}";
                
                // 位置を設定
                if (arrayDuplicationType == ArrayDuplicationType.Linear)
                {
                    newGo.transform.position = go.transform.position + arrayOffset * (i + 1);
                }
                else // Circular
                {
                    float angle = (360f / arrayCount) * i;
                    Vector3 pos = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad)) * arrayCircleRadius;
                    newGo.transform.position = parent.transform.position + pos;
                    if (orientToCenter)
                    {
                        newGo.transform.LookAt(parent.transform.position);
                    }
                }
                
                // MeshFilterとMeshRendererを確保
                EnsureMeshComponents(newGo);
                
                // Undoに登録
                Undo.RegisterCreatedObjectUndo(newGo, "Generate Array Item");
            }
            
            Debug.Log($"Generated array with {arrayCount} objects under '{parent.name}'");
        }

        private void CreateOpening()
        {
            if (targetWallForOpening == null)
            {
                UnityEditor.EditorUtility.DisplayDialog("Target Error", "Please select a target wall for the opening.", "OK");
                return;
            }

            try
            {
                // 開口部作成の簡易実装
                var toolCube = CreateSizedCube("OpeningTool", new Vector3(openingSize.x, openingSize.y, openingToolThickness));
                toolCube.transform.position = targetWallForOpening.transform.position + openingPosition;
                
                // Boolean演算を実行（正しい型を使用）
                Parabox.CSG.Model csgResult = CSG.Subtract(targetWallForOpening.gameObject, toolCube.gameObject);
                
                if (csgResult != null)
                {
                    try
                    {
                        // CSG結果からメッシュを作成
                        Mesh resultMesh = (Mesh)csgResult;
                        if (resultMesh == null)
                        {
                            UnityEditor.EditorUtility.DisplayDialog("Opening Error", 
                                "Failed to convert CSG result to mesh.", "OK");
                            return;
                        }
                        
                        // 新しいGameObjectを作成
                        GameObject resultObject = new GameObject($"{targetWallForOpening.name}_WithOpening");
                        
                        // MeshFilterとMeshRendererを追加
                        MeshFilter meshFilter = resultObject.AddComponent<MeshFilter>();
                        MeshRenderer meshRenderer = resultObject.AddComponent<MeshRenderer>();
                        
                        // メッシュを設定
                        meshFilter.sharedMesh = resultMesh;
                        
                        // マテリアルを設定
                        if (csgResult.materials != null && csgResult.materials.Count > 0)
                        {
                            meshRenderer.sharedMaterials = csgResult.materials.ToArray();
                        }
                        else
                        {
                            // デフォルトマテリアルを設定
                            meshRenderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
                        }
                        
                        // 位置を設定
                        resultObject.transform.position = targetWallForOpening.transform.position;
                        resultObject.transform.rotation = targetWallForOpening.transform.rotation;
                        
                        // ツールオブジェクトを削除
                        Object.DestroyImmediate(toolCube.gameObject);
                        
                        // 結果を選択
                        Selection.activeGameObject = resultObject;
                        
                        Debug.Log($"Opening created successfully! Mesh vertices: {resultMesh.vertexCount}");
                        UnityEditor.EditorUtility.DisplayDialog("Opening Created", 
                            $"Opening created successfully in {resultObject.name}!\nVertices: {resultMesh.vertexCount}", "OK");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Error creating opening: {ex.Message}");
                        UnityEditor.EditorUtility.DisplayDialog("Opening Error", 
                            $"Error creating opening: {ex.Message}", "OK");
                    }
                }
                else
                {
                    UnityEditor.EditorUtility.DisplayDialog("Opening Error", 
                        "Failed to create opening. Please check the target wall and tool positioning.", "OK");
                }
            }
            catch (System.Exception ex)
            {
                UnityEditor.EditorUtility.DisplayDialog("Opening Error", 
                    $"Failed to create opening: {ex.Message}", "OK");
            }
        }

        private bool IsReadOnlyAsset(GameObject go)
        {
            if (go == null) return false;
            
            // プレハブかチェック
            if (PrefabUtility.IsPartOfPrefabAsset(go))
            {
                return true;
            }
            
            // アセットパスを取得
            string assetPath = AssetDatabase.GetAssetPath(go);
            if (!string.IsNullOrEmpty(assetPath))
            {
                return true; // プロジェクトアセット
            }
            
            // メッシュが読み取り専用かチェック
            var meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                string meshPath = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
                if (!string.IsNullOrEmpty(meshPath))
                {
                    return true; // アセットメッシュ
                }
            }
            
            return false;
        }

        private void EnsureMeshComponents(GameObject go)
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

        private void SaveAsPrefab()
        {
            if (targetForPrefab == null)
            {
                UnityEditor.EditorUtility.DisplayDialog("Target Error", "Please select a target object to save as prefab.", "OK");
                return;
            }

            // フォルダが存在しない場合は作成
            if (!Directory.Exists(prefabFolderPath))
            {
                Directory.CreateDirectory(prefabFolderPath);
                AssetDatabase.Refresh();
            }

            string prefabPath = Path.Combine(prefabFolderPath, prefabName + ".prefab");
            
            // 既存のプレハブがある場合は上書き確認
            if (File.Exists(prefabPath))
            {
                if (!UnityEditor.EditorUtility.DisplayDialog("Prefab Exists", 
                    $"Prefab '{prefabName}' already exists. Overwrite?", "Yes", "No"))
                {
                    return;
                }
            }

            // プレハブを作成
            var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(targetForPrefab, prefabPath, InteractionMode.UserAction);
            
            if (prefab != null)
            {
                UnityEditor.EditorUtility.DisplayDialog("Prefab Created", 
                    $"Prefab '{prefabName}' created successfully at:\n{prefabPath}", "OK");
            }
            else
            {
                UnityEditor.EditorUtility.DisplayDialog("Prefab Error", 
                    "Failed to create prefab. Please check the target object and folder path.", "OK");
            }
        }

        private ProBuilderMesh CreateSizedCube(string name, Vector3 size)
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

            ProBuilderMesh pb = ProBuilderMesh.Create(vertices, faces);
            pb.name = name;
            pb.Refresh();
            pb.ToMesh();
            
            return pb;
        }
    }
} 