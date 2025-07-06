using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using System.Collections.Generic;

namespace Vastcore.Editor.Generation
{
    /// <summary>
    /// 基本的な構造物生成機能を提供するタブクラス
    /// ProBuilderの標準機能を使用した安定した実装
    /// </summary>
    public class BasicStructureTab
    {
        private StructureGeneratorWindow parentWindow;
        private Vector2 scrollPosition;
        
        // 基本パラメータ
        private float structureScale = 5f; // スケールを調整
        private int structureCount = 1;
        private Material structureMaterial;
        
        // 基本構造物タイプ
        public enum BasicStructureType
        {
            Cube,
            Cylinder,
            Sphere,
            Pyramid,
            Torus,
            Arch,
            Wall
        }
        
        private BasicStructureType selectedStructureType = BasicStructureType.Cube;
        
        public BasicStructureTab(StructureGeneratorWindow parent)
        {
            parentWindow = parent;
        }
        
        public void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.LabelField("Basic Structure Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 構造物タイプ選択
            EditorGUILayout.LabelField("Structure Type", EditorStyles.boldLabel);
            selectedStructureType = (BasicStructureType)EditorGUILayout.EnumPopup("Type", selectedStructureType);
            EditorGUILayout.Space();
            
            // 基本パラメータ
            EditorGUILayout.LabelField("Basic Parameters", EditorStyles.boldLabel);
            structureScale = EditorGUILayout.Slider("Scale", structureScale, 0.1f, 20f);
            structureCount = EditorGUILayout.IntSlider("Count", structureCount, 1, 10);
            
            EditorGUILayout.Space();
            
            // マテリアル設定
            EditorGUILayout.LabelField("Material", EditorStyles.boldLabel);
            structureMaterial = (Material)EditorGUILayout.ObjectField("Material", structureMaterial, typeof(Material), false);
            
            EditorGUILayout.Space();
            
            // 生成ボタン
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Generate Structure", GUILayout.Height(40)))
            {
                GenerateBasicStructure();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space();
            
            // クイック生成ボタン
            EditorGUILayout.LabelField("Quick Generation", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Cube"))
            {
                GenerateQuickStructure(BasicStructureType.Cube);
            }
            
            if (GUILayout.Button("Cylinder"))
            {
                GenerateQuickStructure(BasicStructureType.Cylinder);
            }
            
            if (GUILayout.Button("Sphere"))
            {
                GenerateQuickStructure(BasicStructureType.Sphere);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Pyramid"))
            {
                GenerateQuickStructure(BasicStructureType.Pyramid);
            }
            
            if (GUILayout.Button("Wall"))
            {
                GenerateQuickStructure(BasicStructureType.Wall);
            }
            
            if (GUILayout.Button("Torus"))
            {
                GenerateQuickStructure(BasicStructureType.Torus);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // ヘルプ
            EditorGUILayout.HelpBox("基本的な構造物を生成します。ProBuilderの標準機能を使用。", MessageType.Info);
            
            EditorGUILayout.EndScrollView();
        }
        
        private void GenerateBasicStructure()
        {
            try
            {
                for (int i = 0; i < structureCount; i++)
                {
                    GameObject structure = CreateBasicStructure(selectedStructureType);
                    
                    if (structure != null)
                    {
                        // 位置調整（複数生成時）
                        if (structureCount > 1)
                        {
                            float spacing = structureScale * 1.5f;
                            structure.transform.position = new Vector3(i * spacing, 0, 0);
                        }
                        
                        // マテリアル適用
                        if (structureMaterial != null)
                        {
                            ApplyMaterial(structure, structureMaterial);
                        }
                        
                        Debug.Log($"Generated {selectedStructureType} successfully: {structure.name}");
                    }
                    else
                    {
                        Debug.LogError($"Failed to generate {selectedStructureType}");
                    }
                }
                
                Debug.Log($"Generated {structureCount} {selectedStructureType} structures");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to generate basic structure: {e.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to generate structure: {e.Message}", "OK");
            }
        }
        
        private void GenerateQuickStructure(BasicStructureType type)
        {
            selectedStructureType = type;
            structureCount = 1;
            GenerateBasicStructure();
        }
        
        private GameObject CreateBasicStructure(BasicStructureType type)
        {
            ProBuilderMesh pbMesh = null;
            
            try
            {
                // ProBuilderの標準機能を使用
                switch (type)
                {
                    case BasicStructureType.Cube:
                        pbMesh = ShapeGenerator.CreateShape(ShapeType.Cube);
                        break;
                        
                    case BasicStructureType.Cylinder:
                        pbMesh = ShapeGenerator.CreateShape(ShapeType.Cylinder);
                        break;
                        
                    case BasicStructureType.Sphere:
                        pbMesh = ShapeGenerator.CreateShape(ShapeType.Sphere);
                        break;
                        
                    case BasicStructureType.Pyramid:
                        // ピラミッドはProBuilderのStairを変形
                        pbMesh = CreateProBuilderPyramid();
                        break;
                        
                    case BasicStructureType.Torus:
                        pbMesh = ShapeGenerator.CreateShape(ShapeType.Torus);
                        break;
                        
                    case BasicStructureType.Arch:
                        pbMesh = ShapeGenerator.CreateShape(ShapeType.Arch);
                        break;
                        
                    case BasicStructureType.Wall:
                        // 壁はCubeを変形
                        pbMesh = CreateProBuilderWall();
                        break;
                }
                
                if (pbMesh != null)
                {
                    // ProBuilderメッシュを正しく初期化
                    ValidateAndFixProBuilderMesh(pbMesh);
                    
                    var gameObject = pbMesh.gameObject;
                    gameObject.transform.localScale = Vector3.one * structureScale;
                    gameObject.name = $"Basic_{type}";
                    
                    // Undoに登録
                    Undo.RegisterCreatedObjectUndo(gameObject, $"Create {type}");
                    
                    // 選択状態にする
                    Selection.activeGameObject = gameObject;
                    
                    return gameObject;
                }
                else
                {
                    Debug.LogError($"Failed to create ProBuilderMesh for {type}");
                    return null;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Exception creating {type}: {e.Message}");
                return null;
            }
        }
        
        private ProBuilderMesh CreateProBuilderPyramid()
        {
            try
            {
                // Cubeから開始してピラミッドに変形
                ProBuilderMesh pbMesh = ShapeGenerator.CreateShape(ShapeType.Cube);
                if (pbMesh == null) return null;
                
                // 上部の頂点を中央に移動してピラミッド化
                var positions = new List<Vector3>(pbMesh.positions);
                for (int i = 0; i < positions.Count; i++)
                {
                    Vector3 pos = positions[i];
                    if (pos.y > 0) // 上部の頂点
                    {
                        positions[i] = new Vector3(0, pos.y, 0); // 中央に移動
                    }
                }
                
                pbMesh.positions = positions;
                pbMesh.ToMesh();
                pbMesh.Refresh();
                
                return pbMesh;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create pyramid: {e.Message}");
                return null;
            }
        }
        
        private ProBuilderMesh CreateProBuilderWall()
        {
            try
            {
                // Cubeから開始して壁に変形
                ProBuilderMesh pbMesh = ShapeGenerator.CreateShape(ShapeType.Cube);
                if (pbMesh == null) return null;
                
                // Z軸方向を薄くして壁にする
                var positions = new List<Vector3>(pbMesh.positions);
                for (int i = 0; i < positions.Count; i++)
                {
                    Vector3 pos = positions[i];
                    positions[i] = new Vector3(pos.x, pos.y, pos.z * 0.1f); // Z軸を薄く
                }
                
                pbMesh.positions = positions;
                pbMesh.ToMesh();
                pbMesh.Refresh();
                
                return pbMesh;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to create wall: {e.Message}");
                return null;
            }
        }
        
        private void ValidateAndFixProBuilderMesh(ProBuilderMesh pbMesh)
        {
            try
            {
                if (pbMesh == null) return;
                
                // メッシュの基本的な検証と修正
                if (pbMesh.faces == null || pbMesh.faces.Count == 0)
                {
                    Debug.LogWarning("ProBuilderMesh has no faces, rebuilding...");
                    pbMesh.ToMesh();
                }
                
                if (pbMesh.positions == null || pbMesh.positions.Count == 0)
                {
                    Debug.LogWarning("ProBuilderMesh has no positions, this is a serious error");
                    return;
                }
                
                // メッシュを更新
                pbMesh.ToMesh();
                pbMesh.Refresh();
                
                // MeshFilterの確認
                var meshFilter = pbMesh.GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    meshFilter = pbMesh.gameObject.AddComponent<MeshFilter>();
                }
                
                if (meshFilter.sharedMesh == null)
                {
                    Debug.LogWarning("MeshFilter.sharedMesh is null, forcing mesh update");
                    pbMesh.ToMesh();
                }
                
                // MeshRendererの確認
                var meshRenderer = pbMesh.GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    meshRenderer = pbMesh.gameObject.AddComponent<MeshRenderer>();
                    meshRenderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
                }
                
                Debug.Log($"ProBuilderMesh validated: {pbMesh.positions.Count} vertices, {pbMesh.faces.Count} faces");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error validating ProBuilderMesh: {e.Message}");
            }
        }
        
        private void ApplyMaterial(GameObject obj, Material material)
        {
            var renderers = obj.GetComponentsInChildren<MeshRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.sharedMaterial = material;
            }
        }
    }
} 