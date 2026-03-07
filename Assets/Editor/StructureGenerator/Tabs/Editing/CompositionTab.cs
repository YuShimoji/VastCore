using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Vastcore.Editor.Generation.Csg;

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

                var operation = _compositionMode switch
                {
                    CompositionMode.Union => CsgOperation.Union,
                    CompositionMode.Intersection => CsgOperation.Intersect,
                    CompositionMode.Difference => CsgOperation.Subtract,
                    _ => CsgOperation.Union
                };

                if (!CsgProviderResolver.TryExecuteWithFallback(
                        objA,
                        objB,
                        operation,
                        out var resultMesh,
                        out var resultMaterials,
                        out var providerName,
                        out var error))
                {
                    Debug.LogError($"[CompositionTab] CSG {_compositionMode} failed: {error}");
                    EditorUtility.DisplayDialog("CSG Error", $"{_compositionMode} 操作が失敗しました。\n\n{error}", "OK");
                    return;
                }

                // 結果から GameObject を作成
                GameObject resultObject = CreateResultObjectFromMesh(
                    resultMesh,
                    resultMaterials,
                    $"CSG_{_compositionMode}_Result ({providerName})",
                    objA.transform);

                // Undo 登録
                Undo.RegisterCreatedObjectUndo(resultObject, $"CSG {_compositionMode}");

                // 3つ以上のオブジェクトがある場合、順次 CSG を適用
                for (int i = 2; i < _sourceObjects.Count; i++)
                {
                    var nextObj = _sourceObjects[i];
                    if (nextObj == null) continue;

                    EnsureMeshComponents(nextObj);

                    if (!CsgProviderResolver.TryExecuteWithFallback(
                            resultObject,
                            nextObj,
                            operation,
                            out var nextMesh,
                            out var nextMaterials,
                            out var nextProviderName,
                            out var nextError))
                    {
                        Debug.LogWarning($"[CompositionTab] CSG {_compositionMode} failed on chained object '{nextObj.name}': {nextError}");
                        EditorUtility.DisplayDialog("CSG Error", $"{_compositionMode} 操作が途中で失敗しました。\n\n{nextError}", "OK");
                        break;
                    }

                    // 古い結果を削除して新しい結果を作成
                    Undo.DestroyObjectImmediate(resultObject);
                    resultObject = CreateResultObjectFromMesh(
                        nextMesh,
                        nextMaterials,
                        $"CSG_{_compositionMode}_Result ({nextProviderName})",
                        objA.transform);
                    Undo.RegisterCreatedObjectUndo(resultObject, $"CSG {_compositionMode}");
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
        }

        private GameObject CreateResultObjectFromMesh(Mesh mesh, Material[] materials, string name, Transform referenceTransform)
        {
            var resultObject = new GameObject(name);
            resultObject.transform.SetPositionAndRotation(referenceTransform.position, referenceTransform.rotation);
            resultObject.transform.localScale = referenceTransform.localScale;

            var mf = resultObject.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh != null ? UnityEngine.Object.Instantiate(mesh) : null;

            var mr = resultObject.AddComponent<MeshRenderer>();
            if (materials != null && materials.Length > 0)
            {
                mr.sharedMaterials = materials;
            }
            else
            {
                mr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            }

            return resultObject;
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

            try
            {
                var objA = _sourceObjects[0];
                var objB = _sourceObjects[1];

                if (objA == null || objB == null)
                {
                    Debug.LogError("[CompositionTab] Source objects are null");
                    return;
                }

                EnsureMeshComponents(objA);
                EnsureMeshComponents(objB);

                var meshA = objA.GetComponent<MeshFilter>()?.sharedMesh;
                var meshB = objB.GetComponent<MeshFilter>()?.sharedMesh;

                if (meshA == null || meshB == null)
                {
                    Debug.LogError("[CompositionTab] Source objects have no mesh data");
                    return;
                }

                Debug.Log($"[CompositionTab] Executing {_blendMode} blend (factor={_blendFactor}) on '{objA.name}' ({meshA.vertexCount}v) and '{objB.name}' ({meshB.vertexCount}v)");

                Mesh resultMesh = BlendMeshes(meshA, meshB, objA.transform, objB.transform, _blendMode, _blendFactor);
                if (resultMesh == null)
                {
                    Debug.LogError("[CompositionTab] Blend operation produced no result");
                    return;
                }

                var materials = objA.GetComponent<MeshRenderer>()?.sharedMaterials;
                GameObject resultObject = CreateResultObjectFromMesh(
                    resultMesh,
                    materials,
                    $"Blend_{_blendMode}_Result",
                    objA.transform);

                Undo.RegisterCreatedObjectUndo(resultObject, $"Blend {_blendMode}");
                HandleSourceObjects();
                Selection.activeGameObject = resultObject;

                Debug.Log($"[CompositionTab] {_blendMode} blend completed ({resultMesh.vertexCount} vertices)");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CompositionTab] Blend failed: {ex.Message}");
                EditorUtility.DisplayDialog("Blend Error", $"Blend 操作中にエラーが発生しました。\n\n{ex.Message}", "OK");
            }
        }

        #region Mesh Blend Core

        private Mesh BlendMeshes(Mesh meshA, Mesh meshB, Transform transformA, Transform transformB, BlendMode mode, float factor)
        {
            // ワールド空間の頂点を取得
            Vector3[] vertsA = GetWorldSpaceVertices(meshA, transformA);
            Vector3[] normsA = GetWorldSpaceNormals(meshA, transformA);
            Vector3[] vertsB = GetWorldSpaceVertices(meshB, transformB);
            Vector3[] normsB = GetWorldSpaceNormals(meshB, transformB);

            // メッシュAをベースとし、Bの頂点をAの頂点にマッピング
            int[] mapping = RemapVertices(vertsA, vertsB);

            Vector3[] resultVerts = new Vector3[vertsA.Length];
            Vector3[] resultNorms = new Vector3[normsA.Length];

            for (int i = 0; i < vertsA.Length; i++)
            {
                int mappedIdx = mapping[i];
                float localFactor = ComputeLocalBlendFactor(mode, factor, vertsA, normsA, i, vertsB[mappedIdx]);

                resultVerts[i] = Vector3.Lerp(vertsA[i], vertsB[mappedIdx], localFactor);

                if (normsA.Length > i && normsB.Length > mappedIdx)
                {
                    resultNorms[i] = Vector3.Slerp(normsA[i], normsB[mappedIdx], localFactor).normalized;
                }
            }

            // ワールド空間からローカル空間に戻す (結果は transformA 基準)
            Matrix4x4 worldToLocal = transformA.worldToLocalMatrix;
            for (int i = 0; i < resultVerts.Length; i++)
            {
                resultVerts[i] = worldToLocal.MultiplyPoint3x4(resultVerts[i]);
                if (resultNorms.Length > i)
                {
                    resultNorms[i] = worldToLocal.MultiplyVector(resultNorms[i]).normalized;
                }
            }

            Mesh result = new Mesh();
            result.name = $"BlendedMesh_{mode}";
            result.vertices = resultVerts;
            result.normals = resultNorms;
            result.triangles = meshA.triangles;
            result.uv = meshA.uv;
            result.RecalculateBounds();
            if (resultNorms.Length == 0 || resultNorms.Length != resultVerts.Length)
            {
                result.RecalculateNormals();
            }

            return result;
        }

        private float ComputeLocalBlendFactor(BlendMode mode, float globalFactor, Vector3[] vertsA, Vector3[] normsA, int vertexIndex, Vector3 targetPos)
        {
            switch (mode)
            {
                case BlendMode.Layered:
                    return globalFactor;

                case BlendMode.Surface:
                {
                    // 距離に基づくフォールオフ: 近い頂点ほど強くブレンド
                    float dist = Vector3.Distance(vertsA[vertexIndex], targetPos);
                    float maxDist = 10f;
                    float proximity = 1f - Mathf.Clamp01(dist / maxDist);
                    return globalFactor * proximity;
                }

                case BlendMode.Adaptive:
                {
                    // 法線の変化率（曲率推定）に基づく: 平坦な部分はよりブレンド、エッジは保存
                    float curvature = EstimateCurvature(normsA, vertexIndex);
                    // 曲率が高い(エッジ) → ブレンド弱、曲率が低い(平坦) → ブレンド強
                    float adaptiveFactor = globalFactor * (1f - Mathf.Clamp01(curvature * 5f));
                    return adaptiveFactor;
                }

                case BlendMode.Noise:
                {
                    // Perlin noise でブレンドファクターを変調
                    Vector3 pos = vertsA[vertexIndex];
                    float noiseScale = 2f;
                    float noise = Mathf.PerlinNoise(pos.x * noiseScale + 100f, pos.z * noiseScale + 100f);
                    return globalFactor * noise;
                }

                default:
                    return globalFactor;
            }
        }

        private float EstimateCurvature(Vector3[] normals, int index)
        {
            if (normals == null || normals.Length <= 1) return 0f;

            // 周辺法線との角度差で曲率を推定
            Vector3 normal = normals[index];
            float totalAngle = 0f;
            int samples = 0;

            // 前後の頂点の法線と比較 (インデックス的に近い頂点 = メッシュ上でも近い傾向)
            for (int offset = -2; offset <= 2; offset++)
            {
                if (offset == 0) continue;
                int neighbor = index + offset;
                if (neighbor < 0 || neighbor >= normals.Length) continue;

                totalAngle += Vector3.Angle(normal, normals[neighbor]);
                samples++;
            }

            return samples > 0 ? (totalAngle / samples) / 180f : 0f;
        }

        private int[] RemapVertices(Vector3[] source, Vector3[] target)
        {
            int[] mapping = new int[source.Length];

            for (int i = 0; i < source.Length; i++)
            {
                float minDist = float.MaxValue;
                int closest = 0;

                for (int j = 0; j < target.Length; j++)
                {
                    float dist = (source[i] - target[j]).sqrMagnitude;
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = j;
                    }
                }

                mapping[i] = closest;
            }

            return mapping;
        }

        private Vector3[] GetWorldSpaceVertices(Mesh mesh, Transform transform)
        {
            Vector3[] localVerts = mesh.vertices;
            Vector3[] worldVerts = new Vector3[localVerts.Length];
            Matrix4x4 localToWorld = transform.localToWorldMatrix;

            for (int i = 0; i < localVerts.Length; i++)
            {
                worldVerts[i] = localToWorld.MultiplyPoint3x4(localVerts[i]);
            }

            return worldVerts;
        }

        private Vector3[] GetWorldSpaceNormals(Mesh mesh, Transform transform)
        {
            Vector3[] localNorms = mesh.normals;
            if (localNorms == null || localNorms.Length == 0) return new Vector3[0];

            Vector3[] worldNorms = new Vector3[localNorms.Length];
            Matrix4x4 localToWorld = transform.localToWorldMatrix;

            for (int i = 0; i < localNorms.Length; i++)
            {
                worldNorms[i] = localToWorld.MultiplyVector(localNorms[i]).normalized;
            }

            return worldNorms;
        }

        #endregion

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
