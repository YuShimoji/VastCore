// This file is disabled until Vastcore.Deform namespace is implemented (Phase 3)
#if VASTCORE_DEFORM_ENABLED
using UnityEngine;
using UnityEditor;
using Vastcore.Deform;
using Vastcore.Generation;

namespace Vastcore.Editor
{
    /// <summary>
    /// Deformエディタウィンドウ
    /// 地形変形パラメータの編集とリアルタイムプレビューを提供
    /// </summary>
    public class DeformationEditorWindow : EditorWindow
    {
        private PrimitiveTerrainObject selectedTerrainObject;
        private DeformationPreset selectedPreset;
        private Vector2 scrollPosition;

        // UIパラメータ
        private bool showNoiseSettings = true;
        private bool showDisplaceSettings = true;
        private bool showAnimationSettings = true;
        private bool showPresetSettings = true;

        // リアルタイム編集用
        private float liveNoiseIntensity = 0.1f;
        private float liveNoiseFrequency = 1f;
        private float liveDisplaceStrength = 0.5f;
        private Texture2D liveDisplaceMap;

        // プレビュー設定
        private bool enableRealtimePreview = false;
        private float previewUpdateDelay = 0.1f;
        private double lastPreviewUpdateTime;

        [MenuItem("Vastcore/Deformation Editor")]
        static void Init()
        {
            DeformationEditorWindow window = (DeformationEditorWindow)GetWindow(typeof(DeformationEditorWindow));
            window.titleContent = new GUIContent("Deformation Editor");
            window.Show();
        }

        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            DrawPreviewSettings();
            DrawSelectionSection();
            DrawPresetSection();
            DrawNoiseSection();
            DrawDisplaceSection();
            DrawAnimationSection();
            DrawControlButtons();

            EditorGUILayout.EndScrollView();
        }

        void OnSelectionChange()
        {
            var selected = Selection.activeGameObject;
            if (selected != null)
            {
                selectedTerrainObject = selected.GetComponent<PrimitiveTerrainObject>();
                if (selectedTerrainObject != null)
                {
                    UpdateLiveParameters();
                }
            }
            Repaint();
        }

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Vastcore Deformation Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space();
        }

        private void DrawPreviewSettings()
        {
            EditorGUILayout.LabelField("Preview Settings", EditorStyles.boldLabel);
            enableRealtimePreview = EditorGUILayout.Toggle("Enable Realtime Preview", enableRealtimePreview);
            if (enableRealtimePreview)
            {
                previewUpdateDelay = EditorGUILayout.Slider("Update Delay (seconds)", previewUpdateDelay, 0.01f, 1f);
            }
            EditorGUILayout.Space();
        }

        private void DrawSelectionSection()
        {
            EditorGUILayout.LabelField("Selected Terrain Object", EditorStyles.boldLabel);
            selectedTerrainObject = (PrimitiveTerrainObject)EditorGUILayout.ObjectField(
                "Terrain Object", selectedTerrainObject, typeof(PrimitiveTerrainObject), true);

            if (selectedTerrainObject != null)
            {
                EditorGUILayout.LabelField($"Type: {selectedTerrainObject.primitiveType}");
                EditorGUILayout.LabelField($"Deform Enabled: {selectedTerrainObject.enableDeform}");
            }
            EditorGUILayout.Space();
        }

        private void DrawPresetSection()
        {
            showPresetSettings = EditorGUILayout.Foldout(showPresetSettings, "Preset Settings");
            if (showPresetSettings)
            {
                EditorGUI.indentLevel++;

                selectedPreset = (DeformationPreset)EditorGUILayout.ObjectField(
                    "Deformation Preset", selectedPreset, typeof(DeformationPreset), false);

                if (GUILayout.Button("Apply Preset"))
                {
                    ApplyPreset();
                }

                if (GUILayout.Button("Create Preset from Current"))
                {
                    CreatePresetFromCurrent();
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }

        private void DrawNoiseSection()
        {
            showNoiseSettings = EditorGUILayout.Foldout(showNoiseSettings, "Noise Deformation");
            if (showNoiseSettings)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                liveNoiseIntensity = EditorGUILayout.Slider("Intensity", liveNoiseIntensity, 0f, 1f);
                liveNoiseFrequency = EditorGUILayout.Slider("Frequency", liveNoiseFrequency, 0.1f, 5f);
                if (EditorGUI.EndChangeCheck() && enableRealtimePreview)
                {
                    UpdatePreview();
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply Noise"))
                {
                    ApplyNoiseDeformation();
                }
                if (GUILayout.Button("Clear Noise"))
                {
                    ClearNoiseDeformation();
                }
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Animate Noise"))
                {
                    AnimateNoiseDeformation();
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }

        private void DrawDisplaceSection()
        {
            showDisplaceSettings = EditorGUILayout.Foldout(showDisplaceSettings, "Displace Deformation");
            if (showDisplaceSettings)
            {
                EditorGUI.indentLevel++;

                EditorGUI.BeginChangeCheck();
                liveDisplaceStrength = EditorGUILayout.Slider("Strength", liveDisplaceStrength, 0f, 2f);
                liveDisplaceMap = (Texture2D)EditorGUILayout.ObjectField(
                    "Displace Map", liveDisplaceMap, typeof(Texture2D), false);
                if (EditorGUI.EndChangeCheck() && enableRealtimePreview)
                {
                    UpdatePreview();
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Apply Displace"))
                {
                    ApplyDisplaceDeformation();
                }
                if (GUILayout.Button("Clear Displace"))
                {
                    ClearDisplaceDeformation();
                }
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Animate Displace"))
                {
                    AnimateDisplaceDeformation();
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }

        private void DrawAnimationSection()
        {
            showAnimationSettings = EditorGUILayout.Foldout(showAnimationSettings, "Animation Settings");
            if (showAnimationSettings)
            {
                EditorGUI.indentLevel++;

                if (GUILayout.Button("Apply Terrain Specific Deformation"))
                {
                    ApplyTerrainSpecificDeformation();
                }

                if (GUILayout.Button("Clear All Deformations"))
                {
                    ClearAllDeformations();
                }

                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }

        private void DrawControlButtons()
        {
            EditorGUILayout.LabelField("Batch Operations", EditorStyles.boldLabel);

            if (GUILayout.Button("Apply to All Selected Terrain Objects"))
            {
                ApplyToAllSelected();
            }

            if (GUILayout.Button("Reset All Selected Terrain Objects"))
            {
                ResetAllSelected();
            }
        }

        private void UpdateLiveParameters()
        {
            if (selectedTerrainObject == null) return;

            // 現在のDeformerパラメータを取得
#if DEFORM_AVAILABLE
            var noiseDeformer = selectedTerrainObject.GetComponent<Deform.NoiseDeformer>();
            if (noiseDeformer != null)
            {
                liveNoiseIntensity = noiseDeformer.Intensity;
                liveNoiseFrequency = noiseDeformer.Frequency;
            }

            var displaceDeformer = selectedTerrainObject.GetComponent<Deform.DisplaceDeformer>();
            if (displaceDeformer != null)
            {
                liveDisplaceStrength = displaceDeformer.Strength;
                liveDisplaceMap = displaceDeformer.Texture;
            }
#endif
        }

        private void ApplyPreset()
        {
            if (selectedTerrainObject != null && selectedPreset != null)
            {
                selectedTerrainObject.ApplyDeformationPreset(selectedPreset);
                UpdateLiveParameters();
            }
        }

        private void CreatePresetFromCurrent()
        {
            if (selectedTerrainObject == null) return;

            var newPreset = DeformationPreset.CreateDefaultPreset(selectedTerrainObject.primitiveType);
            newPreset.noiseIntensity = liveNoiseIntensity;
            newPreset.noiseFrequency = liveNoiseFrequency;
            newPreset.displaceStrength = liveDisplaceStrength;
            newPreset.displaceMap = liveDisplaceMap;

            // アセットとして保存
            string path = $"Assets/DeformationPresets/{selectedTerrainObject.primitiveType}_Preset.asset";
            AssetDatabase.CreateAsset(newPreset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            selectedPreset = newPreset;
        }

        private void ApplyNoiseDeformation()
        {
            if (selectedTerrainObject != null)
            {
                selectedTerrainObject.ApplyNoiseDeformation(liveNoiseIntensity, liveNoiseFrequency);
            }
        }

        private void ClearNoiseDeformation()
        {
            if (selectedTerrainObject != null)
            {
                selectedTerrainObject.RemoveDeformer<Deform.NoiseDeformer>();
            }
        }

        private void AnimateNoiseDeformation()
        {
            if (selectedTerrainObject != null)
            {
                selectedTerrainObject.ApplyNoiseDeformationAnimated(liveNoiseIntensity, liveNoiseFrequency, 2f);
            }
        }

        private void ApplyDisplaceDeformation()
        {
            if (selectedTerrainObject != null)
            {
                selectedTerrainObject.ApplyDisplaceDeformation(liveDisplaceStrength, liveDisplaceMap);
            }
        }

        private void ClearDisplaceDeformation()
        {
            if (selectedTerrainObject != null)
            {
                selectedTerrainObject.RemoveDeformer<Deform.DisplaceDeformer>();
            }
        }

        private void AnimateDisplaceDeformation()
        {
            if (selectedTerrainObject != null)
            {
                selectedTerrainObject.ApplyDisplaceDeformationAnimated(liveDisplaceStrength, liveDisplaceMap, 2f);
            }
        }

        private void ApplyTerrainSpecificDeformation()
        {
            if (selectedTerrainObject != null)
            {
                selectedTerrainObject.ApplyTerrainSpecificDeformation();
                UpdateLiveParameters();
            }
        }

        private void ClearAllDeformations()
        {
            if (selectedTerrainObject != null)
            {
                selectedTerrainObject.ClearAllDeformers();
            }
        }

        private void ApplyToAllSelected()
        {
            foreach (var obj in Selection.gameObjects)
            {
                var terrainObj = obj.GetComponent<PrimitiveTerrainObject>();
                if (terrainObj != null)
                {
                    if (selectedPreset != null)
                    {
                        terrainObj.ApplyDeformationPreset(selectedPreset);
                    }
                    else
                    {
                        terrainObj.ApplyTerrainSpecificDeformation();
                    }
                }
            }
        }

        private void ResetAllSelected()
        {
            foreach (var obj in Selection.gameObjects)
            {
                var terrainObj = obj.GetComponent<PrimitiveTerrainObject>();
                if (terrainObj != null)
                {
                    terrainObj.ClearAllDeformers();
                }
            }
        }

        private void UpdatePreview()
        {
            if (selectedTerrainObject == null) return;

            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - lastPreviewUpdateTime >= previewUpdateDelay)
            {
                // ノイズ変形を適用
                selectedTerrainObject.ApplyNoiseDeformation(liveNoiseIntensity, liveNoiseFrequency);

                // ディスプレイス変形を適用
                if (liveDisplaceMap != null || liveDisplaceStrength > 0f)
                {
                    selectedTerrainObject.ApplyDisplaceDeformation(liveDisplaceStrength, liveDisplaceMap);
                }

                lastPreviewUpdateTime = currentTime;
                SceneView.RepaintAll();
            }
        }
    }
}
#endif
