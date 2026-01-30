using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Editor.Generation
{
    public class RandomControlTab : IStructureTab
    {
        public TabCategory Category => TabCategory.Editing;
        public string DisplayName => "Random Control";
        public string Description => "オブジェクトにランダム要素を適用します。";
        public bool SupportsRealTimeUpdate => true;

        private StructureGeneratorWindow _parent;
        private Vector2 _scrollPosition;

        #region Basic Settings
        private bool _enableRandomization = false;
        #endregion

        #region Preview System
        private bool _previewMode = false;
        private bool _needsPreviewUpdate = false;
        private Dictionary<GameObject, ObjectState> _originalStates = new Dictionary<GameObject, ObjectState>();
        
        private struct ObjectState
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            
            public ObjectState(Transform transform)
            {
                position = transform.position;
                rotation = transform.rotation;
                scale = transform.localScale;
            }
        }
        #endregion

        #region Position Settings
        private bool _showPositionSection = true;
        private Vector3 _positionMin = new Vector3(-2f, -1f, -2f);
        private Vector3 _positionMax = new Vector3(2f, 1f, 2f);
        private bool _useRelativePosition = true;
        #endregion

        #region Rotation Settings  
        private bool _showRotationSection = false;
        private Vector3 _rotationMin = Vector3.zero;
        private Vector3 _rotationMax = new Vector3(360f, 360f, 360f);
        #endregion

        #region Scale Settings
        private bool _showScaleSection = false;
        private float _scaleMin = 0.8f;
        private float _scaleMax = 1.2f;
        private bool _useUniformScale = true;
        private Vector3 _scaleMinIndividual = new Vector3(0.8f, 0.8f, 0.8f);
        private Vector3 _scaleMaxIndividual = new Vector3(1.2f, 1.2f, 1.2f);
        #endregion

        public RandomControlTab(StructureGeneratorWindow parent)
        {
            _parent = parent;
        }

        public void DrawGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            
            if (_enableRandomization)
            {
                EditorGUILayout.Space(10);
                DrawPositionSection();
                EditorGUILayout.Space(5);
                DrawRotationSection();
                EditorGUILayout.Space(5);
                DrawScaleSection();
                EditorGUILayout.Space(10);
                DrawApplySection();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField(DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Description, MessageType.Info);
            
            EditorGUILayout.Space();

            // Phase 2B ヘッダー
            GUI.backgroundColor = Color.green;
            EditorGUILayout.LabelField("=== Phase 2B: Real-time Preview ===", EditorStyles.boldLabel);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();

            _enableRandomization = EditorGUILayout.Toggle("ランダム化を有効にする", _enableRandomization);
            
            if (_enableRandomization)
            {
                DrawPreviewControls();
            }
        }

        private void DrawPreviewControls()
        {
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            // プレビューモード切り替え
            bool newPreviewMode = EditorGUILayout.Toggle("プレビューモード", _previewMode);
            if (newPreviewMode != _previewMode)
            {
                if (newPreviewMode)
                {
                    StartPreview();
                }
                else
                {
                    StopPreview();
                }
                _previewMode = newPreviewMode;
            }
            
            // プレビュー中の場合、復元ボタンを表示
            if (_previewMode)
            {
                if (GUILayout.Button("復元", GUILayout.Width(50)))
                {
                    RestoreOriginalStates();
                }
                
                if (GUILayout.Button("適用", GUILayout.Width(50)))
                {
                    ApplyPreview();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            // プレビューモード時の情報表示
            if (_previewMode)
            {
                var selectedObjects = Selection.gameObjects;
                EditorGUILayout.HelpBox($"プレビュー中: {selectedObjects.Length} オブジェクト | スライダー操作で即座に反映", MessageType.Info);
            }
        }

        private void DrawPositionSection()
        {
            _showPositionSection = EditorGUILayout.Foldout(_showPositionSection, "▼ Position Randomization", true);
            
            if (_showPositionSection)
            {
                EditorGUI.indentLevel++;
                
                // X軸制御
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("X:", GUILayout.Width(25));
                float xMin = _positionMin.x;
                float xMax = _positionMax.x;
                EditorGUILayout.MinMaxSlider(ref xMin, ref xMax, -10f, 10f);
                xMin = EditorGUILayout.FloatField(xMin, GUILayout.Width(50));
                xMax = EditorGUILayout.FloatField(xMax, GUILayout.Width(50));
                
                // 値が変更された場合、プレビュー更新をトリガー
                if (_positionMin.x != xMin || _positionMax.x != xMax)
                {
                    _positionMin.x = xMin;
                    _positionMax.x = xMax;
                    TriggerPreviewUpdate();
                }
                EditorGUILayout.EndHorizontal();

                // Y軸制御
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Y:", GUILayout.Width(25));
                float yMin = _positionMin.y;
                float yMax = _positionMax.y;
                EditorGUILayout.MinMaxSlider(ref yMin, ref yMax, -10f, 10f);
                yMin = EditorGUILayout.FloatField(yMin, GUILayout.Width(50));
                yMax = EditorGUILayout.FloatField(yMax, GUILayout.Width(50));
                
                if (_positionMin.y != yMin || _positionMax.y != yMax)
                {
                    _positionMin.y = yMin;
                    _positionMax.y = yMax;
                    TriggerPreviewUpdate();
                }
                EditorGUILayout.EndHorizontal();

                // Z軸制御
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Z:", GUILayout.Width(25));
                float zMin = _positionMin.z;
                float zMax = _positionMax.z;
                EditorGUILayout.MinMaxSlider(ref zMin, ref zMax, -10f, 10f);
                zMin = EditorGUILayout.FloatField(zMin, GUILayout.Width(50));
                zMax = EditorGUILayout.FloatField(zMax, GUILayout.Width(50));
                
                if (_positionMin.z != zMin || _positionMax.z != zMax)
                {
                    _positionMin.z = zMin;
                    _positionMax.z = zMax;
                    TriggerPreviewUpdate();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);
                
                // オプション
                _useRelativePosition = EditorGUILayout.Toggle("相対位置を使用", _useRelativePosition);
                
                // 現在の設定表示
                EditorGUILayout.LabelField($"Range: ({_positionMin.x:F1}, {_positionMin.y:F1}, {_positionMin.z:F1}) to ({_positionMax.x:F1}, {_positionMax.y:F1}, {_positionMax.z:F1})", EditorStyles.miniLabel);
                
                EditorGUI.indentLevel--;
            }
        }

        private void DrawRotationSection()
        {
            _showRotationSection = EditorGUILayout.Foldout(_showRotationSection, "▼ Rotation Randomization", true);
            
            if (_showRotationSection)
            {
                EditorGUI.indentLevel++;
                
                // X軸回転制御 (Pitch)
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("X (Pitch):", GUILayout.Width(70));
                float xRotMin = _rotationMin.x;
                float xRotMax = _rotationMax.x;
                EditorGUILayout.MinMaxSlider(ref xRotMin, ref xRotMax, 0f, 360f);
                xRotMin = EditorGUILayout.FloatField(xRotMin, GUILayout.Width(50));
                xRotMax = EditorGUILayout.FloatField(xRotMax, GUILayout.Width(50));
                EditorGUILayout.LabelField("°", GUILayout.Width(15));
                
                float newXMin = Mathf.Clamp(xRotMin, 0f, 360f);
                float newXMax = Mathf.Clamp(xRotMax, 0f, 360f);
                if (_rotationMin.x != newXMin || _rotationMax.x != newXMax)
                {
                    _rotationMin.x = newXMin;
                    _rotationMax.x = newXMax;
                    TriggerPreviewUpdate();
                }
                EditorGUILayout.EndHorizontal();

                // Y軸回転制御 (Yaw)
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Y (Yaw):", GUILayout.Width(70));
                float yRotMin = _rotationMin.y;
                float yRotMax = _rotationMax.y;
                EditorGUILayout.MinMaxSlider(ref yRotMin, ref yRotMax, 0f, 360f);
                yRotMin = EditorGUILayout.FloatField(yRotMin, GUILayout.Width(50));
                yRotMax = EditorGUILayout.FloatField(yRotMax, GUILayout.Width(50));
                EditorGUILayout.LabelField("°", GUILayout.Width(15));
                
                float newYMin = Mathf.Clamp(yRotMin, 0f, 360f);
                float newYMax = Mathf.Clamp(yRotMax, 0f, 360f);
                if (_rotationMin.y != newYMin || _rotationMax.y != newYMax)
                {
                    _rotationMin.y = newYMin;
                    _rotationMax.y = newYMax;
                    TriggerPreviewUpdate();
                }
                EditorGUILayout.EndHorizontal();

                // Z軸回転制御 (Roll)
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Z (Roll):", GUILayout.Width(70));
                float zRotMin = _rotationMin.z;
                float zRotMax = _rotationMax.z;
                EditorGUILayout.MinMaxSlider(ref zRotMin, ref zRotMax, 0f, 360f);
                zRotMin = EditorGUILayout.FloatField(zRotMin, GUILayout.Width(50));
                zRotMax = EditorGUILayout.FloatField(zRotMax, GUILayout.Width(50));
                EditorGUILayout.LabelField("°", GUILayout.Width(15));
                
                float newZMin = Mathf.Clamp(zRotMin, 0f, 360f);
                float newZMax = Mathf.Clamp(zRotMax, 0f, 360f);
                if (_rotationMin.z != newZMin || _rotationMax.z != newZMax)
                {
                    _rotationMin.z = newZMin;
                    _rotationMax.z = newZMax;
                    TriggerPreviewUpdate();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);
                
                // 現在の設定表示
                EditorGUILayout.LabelField($"Range: X({_rotationMin.x:F0}°-{_rotationMax.x:F0}°) Y({_rotationMin.y:F0}°-{_rotationMax.y:F0}°) Z({_rotationMin.z:F0}°-{_rotationMax.z:F0}°)", EditorStyles.miniLabel);
                
                EditorGUI.indentLevel--;
            }
        }

        private void DrawScaleSection()
        {
            _showScaleSection = EditorGUILayout.Foldout(_showScaleSection, "▼ Scale Randomization", true);
            
            if (_showScaleSection)
            {
                EditorGUI.indentLevel++;
                
                // Uniform Scale制御
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Uniform:", GUILayout.Width(70));
                float oldScaleMin = _scaleMin;
                float oldScaleMax = _scaleMax;
                EditorGUILayout.MinMaxSlider(ref _scaleMin, ref _scaleMax, 0.1f, 3.0f);
                _scaleMin = EditorGUILayout.FloatField(_scaleMin, GUILayout.Width(50));
                _scaleMax = EditorGUILayout.FloatField(_scaleMax, GUILayout.Width(50));
                
                if (oldScaleMin != _scaleMin || oldScaleMax != _scaleMax)
                {
                    TriggerPreviewUpdate();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(3);
                
                // Individual axis control toggle
                _useUniformScale = !EditorGUILayout.Toggle("Individual Axis Control", !_useUniformScale);
                
                if (!_useUniformScale)
                {
                    EditorGUI.indentLevel++;
                    
                    // X軸スケール制御
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("X (Width):", GUILayout.Width(70));
                    float xScaleMin = _scaleMinIndividual.x;
                    float xScaleMax = _scaleMaxIndividual.x;
                    EditorGUILayout.MinMaxSlider(ref xScaleMin, ref xScaleMax, 0.1f, 3.0f);
                    xScaleMin = EditorGUILayout.FloatField(xScaleMin, GUILayout.Width(50));
                    xScaleMax = EditorGUILayout.FloatField(xScaleMax, GUILayout.Width(50));
                    
                    float newXScaleMin = Mathf.Max(0.01f, xScaleMin);
                    float newXScaleMax = Mathf.Max(0.01f, xScaleMax);
                    if (_scaleMinIndividual.x != newXScaleMin || _scaleMaxIndividual.x != newXScaleMax)
                    {
                        _scaleMinIndividual.x = newXScaleMin;
                        _scaleMaxIndividual.x = newXScaleMax;
                        TriggerPreviewUpdate();
                    }
                    EditorGUILayout.EndHorizontal();

                    // Y軸スケール制御
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Y (Height):", GUILayout.Width(70));
                    float yScaleMin = _scaleMinIndividual.y;
                    float yScaleMax = _scaleMaxIndividual.y;
                    EditorGUILayout.MinMaxSlider(ref yScaleMin, ref yScaleMax, 0.1f, 3.0f);
                    yScaleMin = EditorGUILayout.FloatField(yScaleMin, GUILayout.Width(50));
                    yScaleMax = EditorGUILayout.FloatField(yScaleMax, GUILayout.Width(50));
                    
                    float newYScaleMin = Mathf.Max(0.01f, yScaleMin);
                    float newYScaleMax = Mathf.Max(0.01f, yScaleMax);
                    if (_scaleMinIndividual.y != newYScaleMin || _scaleMaxIndividual.y != newYScaleMax)
                    {
                        _scaleMinIndividual.y = newYScaleMin;
                        _scaleMaxIndividual.y = newYScaleMax;
                        TriggerPreviewUpdate();
                    }
                    EditorGUILayout.EndHorizontal();

                    // Z軸スケール制御
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Z (Depth):", GUILayout.Width(70));
                    float zScaleMin = _scaleMinIndividual.z;
                    float zScaleMax = _scaleMaxIndividual.z;
                    EditorGUILayout.MinMaxSlider(ref zScaleMin, ref zScaleMax, 0.1f, 3.0f);
                    zScaleMin = EditorGUILayout.FloatField(zScaleMin, GUILayout.Width(50));
                    zScaleMax = EditorGUILayout.FloatField(zScaleMax, GUILayout.Width(50));
                    
                    float newZScaleMin = Mathf.Max(0.01f, zScaleMin);
                    float newZScaleMax = Mathf.Max(0.01f, zScaleMax);
                    if (_scaleMinIndividual.z != newZScaleMin || _scaleMaxIndividual.z != newZScaleMax)
                    {
                        _scaleMinIndividual.z = newZScaleMin;
                        _scaleMaxIndividual.z = newZScaleMax;
                        TriggerPreviewUpdate();
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space(5);
                
                // 現在の設定表示
                if (_useUniformScale)
                {
                    EditorGUILayout.LabelField($"Scale Range: {_scaleMin:F2}x to {_scaleMax:F2}x", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField($"Individual: X({_scaleMinIndividual.x:F2}-{_scaleMaxIndividual.x:F2}) Y({_scaleMinIndividual.y:F2}-{_scaleMaxIndividual.y:F2}) Z({_scaleMinIndividual.z:F2}-{_scaleMaxIndividual.z:F2})", EditorStyles.miniLabel);
                }
                
                EditorGUI.indentLevel--;
            }
        }

        private void DrawApplySection()
        {
            EditorGUILayout.Space();
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            
            EditorGUILayout.BeginHorizontal();
            
            // プレビューモード中の場合、異なるボタンを表示
            if (_previewMode)
            {
                if (GUILayout.Button("プレビューを適用", GUILayout.Height(30)))
                {
                    ApplyPreview();
                }
                if (GUILayout.Button("新しいランダム値", GUILayout.Height(30)))
                {
                    UpdatePreview(); // 新しいランダム値でプレビュー更新
                }
            }
            else
            {
                if (GUILayout.Button("Apply to Selected", GUILayout.Height(30)))
                {
                    ApplyRandomization();
                }
                if (GUILayout.Button("Preview", GUILayout.Height(30), GUILayout.Width(80)))
                {
                    // プレビューモードを開始
                    if (Selection.gameObjects.Length > 0)
                    {
                        _previewMode = true;
                        StartPreview();
                    }
                    else
                    {
                        Debug.LogWarning("プレビューにはオブジェクトを選択してください");
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Selected Objects: {Selection.gameObjects.Length}", EditorStyles.miniLabel);
        }

        private void ApplyRandomization()
        {
            var selectedObjects = Selection.gameObjects
                .Where(go => go != null)
                .ToArray();

            if (selectedObjects.Length == 0)
            {
                Debug.LogWarning("No objects selected for randomization.");
                return;
            }

            // Undo対応: 選択オブジェクトのTransform状態を記録
            var transforms = selectedObjects
                .Select(go => go.transform)
                .ToArray();
            Undo.RecordObjects(transforms, "Randomize Transform");

            foreach (GameObject obj in selectedObjects)
            {
                if (_showPositionSection) ApplyPositionRandomization(obj);
                if (_showRotationSection) ApplyRotationRandomization(obj);
                if (_showScaleSection) ApplyScaleRandomization(obj);
            }
            
            Debug.Log($"Applied randomization to {selectedObjects.Length} objects.");
        }

        private void ApplyPositionRandomization(GameObject obj)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(_positionMin.x, _positionMax.x),
                Random.Range(_positionMin.y, _positionMax.y),
                Random.Range(_positionMin.z, _positionMax.z)
            );

            if (_useRelativePosition)
            {
                obj.transform.position += randomOffset;
            }
            else
            {
                obj.transform.position = randomOffset;
            }
        }

        private void ApplyRotationRandomization(GameObject obj)
        {
            Vector3 randomRotation = new Vector3(
                Random.Range(_rotationMin.x, _rotationMax.x),
                Random.Range(_rotationMin.y, _rotationMax.y),
                Random.Range(_rotationMin.z, _rotationMax.z)
            );

            obj.transform.rotation = Quaternion.Euler(randomRotation);
        }

        private void ApplyScaleRandomization(GameObject obj)
        {
            Vector3 randomScale;

            if (_useUniformScale)
            {
                // Uniform scaling
                float uniformScale = Random.Range(_scaleMin, _scaleMax);
                randomScale = new Vector3(uniformScale, uniformScale, uniformScale);
            }
            else
            {
                // Individual axis scaling
                randomScale = new Vector3(
                    Random.Range(_scaleMinIndividual.x, _scaleMaxIndividual.x),
                    Random.Range(_scaleMinIndividual.y, _scaleMaxIndividual.y),
                    Random.Range(_scaleMinIndividual.z, _scaleMaxIndividual.z)
                );
            }

            obj.transform.localScale = randomScale;
        }

        public void HandleRealTimeUpdate() 
        {
            if (_needsPreviewUpdate && _previewMode)
            {
                UpdatePreview();
                _needsPreviewUpdate = false;
            }
        }
        
        public void OnSceneGUI() { }
        public void ProcessSelectedObjects() { }
        public void OnTabSelected() { }
        public void OnTabDeselected() 
        {
            // タブ切り替え時にプレビューモードを終了
            if (_previewMode)
            {
                StopPreview();
            }
        }

        #region Preview System Methods
        
        private void StartPreview()
        {
            var selectedObjects = Selection.gameObjects;
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                Debug.LogWarning("RandomControlTab: プレビュー開始にはオブジェクトを選択してください");
                _previewMode = false;
                return;
            }

            // 現在の状態を保存
            _originalStates.Clear();
            foreach (var obj in selectedObjects)
            {
                if (obj != null)
                {
                    _originalStates[obj] = new ObjectState(obj.transform);
                }
            }

            // 初回プレビューを適用
            UpdatePreview();
            
            Debug.Log($"RandomControlTab: プレビューモード開始 - {selectedObjects.Length} オブジェクト");
        }

        private void StopPreview()
        {
            if (_originalStates.Count > 0)
            {
                RestoreOriginalStates();
                _originalStates.Clear();
            }
            _previewMode = false;
            _needsPreviewUpdate = false;
            
            Debug.Log("RandomControlTab: プレビューモード終了");
        }

        private void UpdatePreview()
        {
            if (!_previewMode || _originalStates.Count == 0) return;

            foreach (var kvp in _originalStates.ToList())
            {
                var obj = kvp.Key;
                var originalState = kvp.Value;
                
                if (obj == null)
                {
                    _originalStates.Remove(obj);
                    continue;
                }

                // 元の状態から相対的に変更を適用
                if (_showPositionSection)
                {
                    ApplyPositionRandomizationPreview(obj, originalState);
                }
                if (_showRotationSection)
                {
                    ApplyRotationRandomizationPreview(obj, originalState);
                }
                if (_showScaleSection)
                {
                    ApplyScaleRandomizationPreview(obj, originalState);
                }
            }

            // Scene Viewを更新
            SceneView.RepaintAll();
        }

        private void RestoreOriginalStates()
        {
            foreach (var kvp in _originalStates)
            {
                var obj = kvp.Key;
                var originalState = kvp.Value;
                
                if (obj != null)
                {
                    obj.transform.position = originalState.position;
                    obj.transform.rotation = originalState.rotation;
                    obj.transform.localScale = originalState.scale;
                }
            }
            
            SceneView.RepaintAll();
        }

        private void ApplyPreview()
        {
            // 現在のプレビュー状態を確定
            _originalStates.Clear();
            _previewMode = false;
            _needsPreviewUpdate = false;
            
            Debug.Log("RandomControlTab: プレビューを適用しました");
        }

        private void TriggerPreviewUpdate()
        {
            if (_previewMode)
            {
                _needsPreviewUpdate = true;
            }
        }

        #endregion

        #region Preview Application Methods

        private void ApplyPositionRandomizationPreview(GameObject obj, ObjectState originalState)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(_positionMin.x, _positionMax.x),
                Random.Range(_positionMin.y, _positionMax.y),
                Random.Range(_positionMin.z, _positionMax.z)
            );

            if (_useRelativePosition)
            {
                obj.transform.position = originalState.position + randomOffset;
            }
            else
            {
                obj.transform.position = randomOffset;
            }
        }

        private void ApplyRotationRandomizationPreview(GameObject obj, ObjectState originalState)
        {
            Vector3 randomRotation = new Vector3(
                Random.Range(_rotationMin.x, _rotationMax.x),
                Random.Range(_rotationMin.y, _rotationMax.y),
                Random.Range(_rotationMin.z, _rotationMax.z)
            );

            obj.transform.rotation = originalState.rotation * Quaternion.Euler(randomRotation);
        }

        private void ApplyScaleRandomizationPreview(GameObject obj, ObjectState originalState)
        {
            Vector3 randomScale;
            
            if (_useUniformScale)
            {
                float uniformScale = Random.Range(_scaleMin, _scaleMax);
                randomScale = Vector3.one * uniformScale;
            }
            else
            {
                randomScale = new Vector3(
                    Random.Range(_scaleMinIndividual.x, _scaleMaxIndividual.x),
                    Random.Range(_scaleMinIndividual.y, _scaleMaxIndividual.y),
                    Random.Range(_scaleMinIndividual.z, _scaleMaxIndividual.z)
                );
            }

            obj.transform.localScale = Vector3.Scale(originalState.scale, randomScale);
        }

        #endregion
    }
} 