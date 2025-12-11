using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

#if HAS_PROBUILDER
using UnityEngine.ProBuilder;
#endif

#if HAS_PARABOX_CSG
using Parabox.CSG;
#endif

namespace Vastcore.Editor.Generation
{
    /// <summary>
    /// Composition Tab - CSG演算・形状合成機能
    /// Phase 5: 高度合成システム
    /// 
    /// 機能:
    /// - CSG演算: Union, Intersection, Difference
    /// - ブレンド: Layered, Surface, Adaptive, Noise
    /// - 高度合成: Morph, Volumetric Blend, Distance Field
    /// </summary>
    public class CompositionTab : IStructureTab
    {
        public TabCategory Category => TabCategory.Editing;
        public string DisplayName => "Composition";
        public string Description => "CSG演算と高度な形状合成を行います。";
        public bool SupportsRealTimeUpdate => false;

        private StructureGeneratorWindow _parent;
        private Vector2 _scrollPosition;

        #region CSG Settings
        
        private CompositionMode _compositionMode = CompositionMode.Union;
        private List<GameObject> _sourceObjects = new List<GameObject>();
        private bool _showCSGSection = true;
        
        #endregion

        #region Blend Settings
        
        private BlendMode _blendMode = BlendMode.Layered;
        private float _blendFactor = 0.5f;
        private bool _showBlendSection = false;
        
        #endregion

        #region Advanced Settings
        
        private bool _showAdvancedSection = false;
        private float _morphFactor = 0.5f;
        private int _resolution = 32;
        
        #endregion

        #region CSG Options
        
        private bool _hideSourceObjects = true;
        private bool _deleteSourceObjects = false;
        
        #endregion

        /// <summary>
        /// CSG演算モード
        /// </summary>
        public enum CompositionMode
        {
            /// <summary>結合（和集合）</summary>
            Union,
            /// <summary>交差（積集合）</summary>
            Intersection,
            /// <summary>減算（差集合）</summary>
            Difference
        }

        /// <summary>
        /// ブレンドモード
        /// </summary>
        public enum BlendMode
        {
            /// <summary>レイヤー合成</summary>
            Layered,
            /// <summary>サーフェス合成</summary>
            Surface,
            /// <summary>適応的合成</summary>
            Adaptive,
            /// <summary>ノイズ合成</summary>
            Noise
        }

        public CompositionTab(StructureGeneratorWindow parent)
        {
            _parent = parent;
        }

        public void DrawGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            
            EditorGUILayout.Space(10);
            DrawSourceObjectsSection();
            
            EditorGUILayout.Space(10);
            DrawCSGSection();
            
            EditorGUILayout.Space(10);
            DrawBlendSection();
            
            EditorGUILayout.Space(10);
            DrawAdvancedSection();
            
            EditorGUILayout.Space(10);
            DrawActionButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField(DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Description, MessageType.Info);
            
            EditorGUILayout.Space();
            
            // Phase 5 ヘッダー
            GUI.backgroundColor = new Color(0.6f, 0.4f, 0.8f);
            EditorGUILayout.LabelField("=== Phase 5: Advanced Composition ===", EditorStyles.boldLabel);
            GUI.backgroundColor = Color.white;
        }

        private void DrawSourceObjectsSection()
        {
            EditorGUILayout.LabelField("Source Objects", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // 選択中のオブジェクト数を表示
            int selectedCount = Selection.gameObjects.Length;
            EditorGUILayout.LabelField($"Selected: {selectedCount} objects");
            
            if (GUILayout.Button("Add Selected Objects"))
            {
                AddSelectedObjects();
            }
            
            if (GUILayout.Button("Clear Source Objects"))
            {
                _sourceObjects.Clear();
            }
            
            // ソースオブジェクトリスト表示
            if (_sourceObjects.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Source Objects ({_sourceObjects.Count}):", EditorStyles.miniLabel);
                
                EditorGUI.indentLevel++;
                for (int i = 0; i < _sourceObjects.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    _sourceObjects[i] = (GameObject)EditorGUILayout.ObjectField(
                        _sourceObjects[i], typeof(GameObject), true);
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        _sourceObjects.RemoveAt(i);
                        i--;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawCSGSection()
        {
            _showCSGSection = EditorGUILayout.Foldout(_showCSGSection, "▼ CSG Operations", true);
            
            if (_showCSGSection)
            {
                EditorGUI.indentLevel++;
                
                _compositionMode = (CompositionMode)EditorGUILayout.EnumPopup("Operation", _compositionMode);
                
                EditorGUILayout.Space(5);
                
                // 操作説明
                string description = _compositionMode switch
                {
                    CompositionMode.Union => "2つ以上のオブジェクトを結合します（和集合）",
                    CompositionMode.Intersection => "オブジェクトの交差部分のみを残します（積集合）",
                    CompositionMode.Difference => "最初のオブジェクトから他のオブジェクトを減算します（差集合）",
                    _ => ""
                };
                EditorGUILayout.HelpBox(description, MessageType.Info);
                
                EditorGUILayout.Space(5);
                
                // CSG オプション
                _hideSourceObjects = EditorGUILayout.Toggle("元オブジェクトを非表示", _hideSourceObjects);
                _deleteSourceObjects = EditorGUILayout.Toggle("元オブジェクトを削除", _deleteSourceObjects);
                
                EditorGUILayout.Space(5);
                
                EditorGUI.BeginDisabledGroup(_sourceObjects.Count < 2);
                if (GUILayout.Button($"Execute {_compositionMode}", GUILayout.Height(25)))
                {
                    ExecuteCSGOperation();
                }
                EditorGUI.EndDisabledGroup();
                
                if (_sourceObjects.Count < 2)
                {
                    EditorGUILayout.HelpBox("CSG操作には2つ以上のオブジェクトが必要です", MessageType.Warning);
                }
#if !HAS_PROBUILDER
                EditorGUILayout.HelpBox("ProBuilder がインストールされていないため、CSG 機能は利用できません", MessageType.Error);
#endif
                
                EditorGUI.indentLevel--;
            }
        }

        private void DrawBlendSection()
        {
            _showBlendSection = EditorGUILayout.Foldout(_showBlendSection, "▼ Blend Operations", true);
            
            if (_showBlendSection)
            {
                EditorGUI.indentLevel++;
                
                _blendMode = (BlendMode)EditorGUILayout.EnumPopup("Blend Mode", _blendMode);
                _blendFactor = EditorGUILayout.Slider("Blend Factor", _blendFactor, 0f, 1f);
                
                EditorGUILayout.Space(5);
                
                // 操作説明
                string description = _blendMode switch
                {
                    BlendMode.Layered => "透明度に基づいてレイヤーを合成します",
                    BlendMode.Surface => "サーフェスに沿って形状を合成します",
                    BlendMode.Adaptive => "形状の特徴に適応して合成します",
                    BlendMode.Noise => "ノイズパターンで合成境界を制御します",
                    _ => ""
                };
                EditorGUILayout.HelpBox(description, MessageType.Info);
                
                EditorGUILayout.Space(5);
                
                EditorGUI.BeginDisabledGroup(_sourceObjects.Count < 2);
                if (GUILayout.Button($"Execute {_blendMode} Blend", GUILayout.Height(25)))
                {
                    ExecuteBlendOperation();
                }
                EditorGUI.EndDisabledGroup();
                
                EditorGUI.indentLevel--;
            }
        }

        private void DrawAdvancedSection()
        {
            _showAdvancedSection = EditorGUILayout.Foldout(_showAdvancedSection, "▼ Advanced Operations", true);
            
            if (_showAdvancedSection)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField("Morph Settings", EditorStyles.boldLabel);
                _morphFactor = EditorGUILayout.Slider("Morph Factor", _morphFactor, 0f, 1f);
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("Resolution Settings", EditorStyles.boldLabel);
                _resolution = EditorGUILayout.IntSlider("Voxel Resolution", _resolution, 8, 128);
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.HelpBox(
                    "高度な合成機能:\n" +
                    "• Morph: 2つのメッシュ間でモーフィング\n" +
                    "• Volumetric: ボリュームベースの合成\n" +
                    "• Distance Field: SDF合成",
                    MessageType.Info);
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                
                EditorGUI.BeginDisabledGroup(_sourceObjects.Count < 2);
                if (GUILayout.Button("Morph"))
                {
                    ExecuteMorph();
                }
                if (GUILayout.Button("Volumetric"))
                {
                    ExecuteVolumetricBlend();
                }
                if (GUILayout.Button("Distance Field"))
                {
                    ExecuteDistanceFieldBlend();
                }
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("Preview", GUILayout.Height(30)))
            {
                PreviewOperation();
            }
            
            GUI.backgroundColor = new Color(0.8f, 0.4f, 0.4f);
            if (GUILayout.Button("Undo Preview", GUILayout.Height(30)))
            {
                UndoPreview();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
        }

        #region Operations

        private void AddSelectedObjects()
        {
            foreach (var obj in Selection.gameObjects)
            {
                if (obj != null && !_sourceObjects.Contains(obj))
                {
                    // メッシュを持つオブジェクトのみ追加
                    if (obj.GetComponent<MeshFilter>() != null || obj.GetComponent<MeshRenderer>() != null)
                    {
                        _sourceObjects.Add(obj);
                    }
                }
            }
            Debug.Log($"[CompositionTab] Added {Selection.gameObjects.Length} objects. Total: {_sourceObjects.Count}");
        }

        private void ExecuteCSGOperation()
        {
            if (_sourceObjects.Count < 2)
            {
                Debug.LogWarning("[CompositionTab] CSG requires at least 2 objects");
                return;
            }

#if HAS_PARABOX_CSG
            try
            {
                Debug.Log($"[CompositionTab] Executing {_compositionMode} on {_sourceObjects.Count} objects");
                
                // 最初の2オブジェクトで CSG を実行
                var objA = _sourceObjects[0];
                var objB = _sourceObjects[1];
                
                if (objA == null || objB == null)
                {
                    Debug.LogError("[CompositionTab] Source objects are null");
                    return;
                }
                
                // MeshFilter/MeshRenderer を確保
                EnsureMeshComponents(objA);
                EnsureMeshComponents(objB);
                
                // CSG 演算を実行
                Model csgResult = _compositionMode switch
                {
                    CompositionMode.Union => CSG.Union(objA, objB),
                    CompositionMode.Intersection => CSG.Intersect(objA, objB),
                    CompositionMode.Difference => CSG.Subtract(objA, objB),
                    _ => null
                };
                
                if (csgResult == null)
                {
                    Debug.LogError($"[CompositionTab] CSG {_compositionMode} failed - result is null");
                    EditorUtility.DisplayDialog("CSG Error", $"{_compositionMode} 操作が失敗しました。\n\nオブジェクトが交差しているか確認してください。", "OK");
                    return;
                }
                
                // 結果から GameObject を作成
                GameObject resultObject = CreateResultObject(csgResult, $"CSG_{_compositionMode}_Result");
                
                // Undo 登録
                Undo.RegisterCreatedObjectUndo(resultObject, $"CSG {_compositionMode}");
                
                // 3つ以上のオブジェクトがある場合、順次 CSG を適用
                for (int i = 2; i < _sourceObjects.Count; i++)
                {
                    var nextObj = _sourceObjects[i];
                    if (nextObj == null) continue;
                    
                    EnsureMeshComponents(nextObj);
                    
                    Model nextResult = _compositionMode switch
                    {
                        CompositionMode.Union => CSG.Union(resultObject, nextObj),
                        CompositionMode.Intersection => CSG.Intersect(resultObject, nextObj),
                        CompositionMode.Difference => CSG.Subtract(resultObject, nextObj),
                        _ => null
                    };
                    
                    if (nextResult != null)
                    {
                        // 古い結果を削除して新しい結果を作成
                        Object.DestroyImmediate(resultObject);
                        resultObject = CreateResultObject(nextResult, $"CSG_{_compositionMode}_Result");
                        Undo.RegisterCreatedObjectUndo(resultObject, $"CSG {_compositionMode}");
                    }
                }
                
                // 元オブジェクトの処理
                HandleSourceObjects();
                
                // 結果を選択
                Selection.activeGameObject = resultObject;
                
                Debug.Log($"[CompositionTab] CSG {_compositionMode} completed successfully!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CompositionTab] CSG {_compositionMode} failed: {ex.Message}");
                EditorUtility.DisplayDialog("CSG Error", $"CSG 操作中にエラーが発生しました。\n\n{ex.Message}", "OK");
            }
#else
            // Parabox.CSG が利用できない場合のフォールバック
            Debug.Log($"[CompositionTab] CSG {_compositionMode} requested but Parabox.CSG is not available");
            EditorUtility.DisplayDialog(
                "CSG Not Available",
                "Parabox.CSG パッケージがインストールされていないため、CSG 機能は利用できません。\n\n" +
                "ProBuilder の CSG 拡張パッケージをインストールしてください。",
                "OK");
#endif
        }

#if HAS_PARABOX_CSG && HAS_PROBUILDER
        private GameObject CreateResultObject(Model csgResult, string name)
        {
            var materials = csgResult.materials?.ToArray() ?? new Material[0];
            
            // ProBuilderMesh を作成
            ProBuilderMesh pb = ProBuilderMesh.Create();
            pb.GetComponent<MeshFilter>().sharedMesh = (Mesh)csgResult;
            pb.GetComponent<MeshRenderer>().sharedMaterials = materials;
            
            // メッシュを更新
            pb.ToMesh();
            pb.Refresh();
            
            pb.gameObject.name = name;
            
            return pb.gameObject;
        }
#endif
        
        private void EnsureMeshComponents(GameObject go)
        {
            if (go == null) return;
            
            var meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = go.AddComponent<MeshFilter>();
            }
            
            var meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = go.AddComponent<MeshRenderer>();
                meshRenderer.material = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            }
            
#if HAS_PROBUILDER
            var pbMesh = go.GetComponent<ProBuilderMesh>();
            if (pbMesh != null)
            {
                pbMesh.ToMesh();
                pbMesh.Refresh();
            }
#endif
        }
        
        private void HandleSourceObjects()
        {
            foreach (var obj in _sourceObjects)
            {
                if (obj == null) continue;
                
                if (_deleteSourceObjects)
                {
                    Undo.DestroyObjectImmediate(obj);
                }
                else if (_hideSourceObjects)
                {
                    Undo.RecordObject(obj, "Hide Source Object");
                    obj.SetActive(false);
                }
            }
            
            if (_deleteSourceObjects)
            {
                _sourceObjects.Clear();
            }
        }

        private void ExecuteBlendOperation()
        {
            if (_sourceObjects.Count < 2)
            {
                Debug.LogWarning("[CompositionTab] Blend requires at least 2 objects");
                return;
            }

            Debug.Log($"[CompositionTab] Executing {_blendMode} blend with factor {_blendFactor}");
            
            // TODO: ブレンド実装
            EditorUtility.DisplayDialog(
                "Blend Operation",
                $"{_blendMode} blend is not yet implemented.\n\n" +
                "Blend Factor: {_blendFactor}",
                "OK");
        }

        private void ExecuteMorph()
        {
            Debug.Log($"[CompositionTab] Executing Morph with factor {_morphFactor}");
            EditorUtility.DisplayDialog("Morph", "Morph operation is not yet implemented.", "OK");
        }

        private void ExecuteVolumetricBlend()
        {
            Debug.Log($"[CompositionTab] Executing Volumetric Blend with resolution {_resolution}");
            EditorUtility.DisplayDialog("Volumetric Blend", "Volumetric blend is not yet implemented.", "OK");
        }

        private void ExecuteDistanceFieldBlend()
        {
            Debug.Log($"[CompositionTab] Executing Distance Field Blend with resolution {_resolution}");
            EditorUtility.DisplayDialog("Distance Field", "Distance field blend is not yet implemented.", "OK");
        }

        private void PreviewOperation()
        {
            Debug.Log("[CompositionTab] Preview requested");
        }

        private void UndoPreview()
        {
            Debug.Log("[CompositionTab] Undo preview requested");
        }

        #endregion

        #region IStructureTab Implementation

        public void HandleRealTimeUpdate() { }
        public void OnSceneGUI() { }
        public void ProcessSelectedObjects() { }
        public void OnTabSelected() { }
        public void OnTabDeselected() { }

        #endregion
    }
}
