#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Vastcore.Editor.Generation;
using System;
using System.Collections.Generic;
using System.Linq;

#if DEFORM_AVAILABLE
using Deform;
#endif

namespace Vastcore.Editor.StructureGenerator.Tabs
{
    /// <summary>
    /// Represents the "Deform" tab in the Structure Generator editor window.
    /// Provides UI for selecting and applying Deformers to generated structures.
    /// </summary>
    public class DeformerTab : BaseStructureTab
    {
        public override TabCategory Category => TabCategory.Editing;
        public override string DisplayName => "Deform";
        public override string Description => "Apply various deformations to the generated structures.";

        private List<Type> _availableDeformers = new List<Type>();
        private string[] _deformerNames = new string[0];
        private int _selectedDeformerIndex = 0;
        
        // Deformer parameter settings
        private float _deformStrength = 1.0f;
        private Vector3 _deformAxis = Vector3.up;
        private bool _showAdvancedSettings = false;

        public DeformerTab(StructureGeneratorWindow parent) : base(parent) 
        {
            FindAvailableDeformers();
        }

        /// <summary>
        /// Finds all available Deformer types in the project.
        /// </summary>
        private void FindAvailableDeformers()
        {
#if DEFORM_AVAILABLE
            try
            {
                _availableDeformers = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly => 
                    {
                        try { return assembly.GetTypes(); }
                        catch { return new Type[0]; }
                    })
                    .Where(type => type != null && 
                           type.IsSubclassOf(typeof(Deformer)) && 
                           !type.IsAbstract)
                    .ToList();

                _deformerNames = _availableDeformers.Select(t => t.Name).ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DeformerTab] Error finding deformers: {ex.Message}");
                _availableDeformers = new List<Type>();
                _deformerNames = new string[0];
            }
#else
            // Deformパッケージが利用できない場合
            _availableDeformers = new List<Type>();
            _deformerNames = new string[0];
#endif
        }

        protected override void DrawTabContent()
        {
#if DEFORM_AVAILABLE
            DrawDeformAvailableUI();
#else
            DrawDeformUnavailableUI();
#endif
        }

#if DEFORM_AVAILABLE
        /// <summary>
        /// Draws the UI when Deform package is available.
        /// </summary>
        private void DrawDeformAvailableUI()
        {
            if (_availableDeformers == null || _availableDeformers.Count == 0)
            {
                EditorGUILayout.HelpBox("No Deformers found. Please ensure the Deform package is installed correctly.", MessageType.Warning);
                if (GUILayout.Button("Refresh Deformer List"))
                {
                    FindAvailableDeformers();
                }
                return;
            }

            // Deformer selection
            EditorGUILayout.LabelField("Deformer Selection", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _selectedDeformerIndex = EditorGUILayout.Popup("Deformer Type", _selectedDeformerIndex, _deformerNames);
            if (EditorGUI.EndChangeCheck())
            {
                // Reset parameters when deformer type changes
                _deformStrength = 1.0f;
            }

            EditorGUILayout.Space();

            // Basic parameters
            EditorGUILayout.LabelField("Parameters", EditorStyles.boldLabel);
            _deformStrength = EditorGUILayout.Slider("Strength", _deformStrength, 0f, 2f);
            _deformAxis = EditorGUILayout.Vector3Field("Axis", _deformAxis);

            EditorGUILayout.Space();

            // Advanced settings foldout
            _showAdvancedSettings = EditorGUILayout.Foldout(_showAdvancedSettings, "Advanced Settings");
            if (_showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("Advanced deformer-specific parameters will be displayed here based on the selected deformer type.", MessageType.Info);
                // TODO: Dynamic parameter UI based on selected deformer type
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Apply button
            EditorGUI.BeginDisabledGroup(Selection.activeGameObject == null);
            if (GUILayout.Button("Apply Deformer", GUILayout.Height(30)))
            {
                ProcessSelectedObjects();
            }
            EditorGUI.EndDisabledGroup();

            if (Selection.activeGameObject == null)
            {
                EditorGUILayout.HelpBox("Select a GameObject in the scene to apply the deformer.", MessageType.Info);
            }
        }
#endif

        /// <summary>
        /// Draws the UI when Deform package is not available.
        /// </summary>
        private void DrawDeformUnavailableUI()
        {
            EditorGUILayout.HelpBox(
                "Deform package is not available.\n\n" +
                "To enable deformation features:\n" +
                "1. Install the Deform package from GitHub or Asset Store\n" +
                "2. Add 'DEFORM_AVAILABLE' to Scripting Define Symbols\n" +
                "3. Restart Unity Editor",
                MessageType.Warning);

            EditorGUILayout.Space();

            if (GUILayout.Button("Open Package Manager"))
            {
                EditorApplication.ExecuteMenuItem("Window/Package Manager");
            }

            if (GUILayout.Button("Open Player Settings"))
            {
                SettingsService.OpenProjectSettings("Project/Player");
            }
        }

        public override void ProcessSelectedObjects()
        {
#if DEFORM_AVAILABLE
            if (Selection.activeGameObject == null)
            {
                EditorUtility.DisplayDialog("No Object Selected", 
                    "Please select a GameObject in the scene to apply the deformer to.", "OK");
                return;
            }

            if (_selectedDeformerIndex < 0 || _selectedDeformerIndex >= _availableDeformers.Count)
            {
                Debug.LogError("[DeformerTab] Invalid deformer selected.");
                return;
            }

            var selectedDeformerType = _availableDeformers[_selectedDeformerIndex];
            var targetObject = Selection.activeGameObject;
            
            try
            {
                // Ensure Deformable component exists
                var deformable = targetObject.GetComponent<Deformable>();
                if (deformable == null)
                {
                    deformable = targetObject.AddComponent<Deformable>();
                    Debug.Log($"[DeformerTab] Added Deformable component to {targetObject.name}");
                }

                // Add the selected deformer
                var deformer = targetObject.AddComponent(selectedDeformerType) as Deformer;
                if (deformer != null)
                {
                    // Apply basic settings
                    // Note: Specific deformer settings would need type-specific handling
                    
                    // Register with VastcoreDeformManager if available
                    if (Vastcore.Generation.VastcoreDeformManager.Instance != null)
                    {
                        Vastcore.Generation.VastcoreDeformManager.Instance.RegisterDeformable(
                            deformable, 
                            Vastcore.Generation.VastcoreDeformManager.DeformQualityLevel.High);
                    }
                    
                    Debug.Log($"[DeformerTab] Applied {selectedDeformerType.Name} to {targetObject.name}");
                    EditorUtility.SetDirty(targetObject);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DeformerTab] Failed to apply deformer: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to apply deformer: {ex.Message}", "OK");
            }
#else
            EditorUtility.DisplayDialog("Deform Not Available", 
                "The Deform package is not installed. Please install it to use deformation features.", "OK");
#endif
        }
    }
}
#endif