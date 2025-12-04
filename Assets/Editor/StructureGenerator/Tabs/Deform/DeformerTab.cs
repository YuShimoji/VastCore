#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Vastcore.Editor.Generation;
using Vastcore.Generation;
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
    /// Integrates with DeformIntegrationManager for unified deformer management.
    /// </summary>
    public class DeformerTab : BaseStructureTab
    {
        public override TabCategory Category => TabCategory.Editing;
        public override string DisplayName => "Deform";
        public override string Description => "Apply various deformations to the generated structures.";

        // DeformIntegrationManager ベースの設定
        private DeformIntegrationManager.DeformerSettings _currentSettings;
        private string[] _deformerTypeNames;
        
        // UI State
        private bool _showAdvancedSettings = false;
        private bool _showAnimationSettings = false;
        private Vector2 _scrollPosition;
        
        // Deformer-specific parameters (動的UI用)
        private float _bendAngle = 45f;
        private float _twistAngle = 180f;
        private float _taperFactor = 0.5f;
        private float _noiseFrequency = 1f;
        private float _waveAmplitude = 0.5f;
        private float _waveFrequency = 2f;
        private float _spherifyFactor = 1f;
        
        // Basic deformer parameters (used for both modes)
        private float _deformStrength = 1.0f;
        private Vector3 _deformAxis = Vector3.up;
        
        // Legacy fields for Deform package discovery
        private List<Type> _availableDeformers = new List<Type>();
        private string[] _deformerNames = new string[0];
        private int _selectedDeformerIndex = 0;
        
        public DeformerTab(StructureGeneratorWindow parent) : base(parent) 
        {
            InitializeDeformerTypes();
            FindAvailableDeformers();
        }
        
        /// <summary>
        /// Initializes the DeformerType names array from DeformIntegrationManager.
        /// </summary>
        private void InitializeDeformerTypes()
        {
            _deformerTypeNames = Enum.GetNames(typeof(DeformIntegrationManager.DeformerType));
            _currentSettings = DeformIntegrationManager.DeformerSettings.Default(
                DeformIntegrationManager.DeformerType.Bend);
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
                DrawDynamicDeformerParameters();
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
        /// Draws dynamic parameters based on the selected deformer type.
        /// </summary>
        private void DrawDynamicDeformerParameters()
        {
            if (_deformerNames == null || _deformerNames.Length == 0 || 
                _selectedDeformerIndex < 0 || _selectedDeformerIndex >= _deformerNames.Length)
            {
                return;
            }
            
            string deformerName = _deformerNames[_selectedDeformerIndex];
            
            EditorGUILayout.LabelField($"{deformerName} Parameters", EditorStyles.boldLabel);
            
            // Draw type-specific parameters
            switch (deformerName)
            {
                case "BendDeformer":
                    _bendAngle = EditorGUILayout.Slider("Bend Angle", _bendAngle, -180f, 180f);
                    EditorGUILayout.HelpBox("Bends the mesh along the specified axis.", MessageType.Info);
                    break;
                    
                case "TwistDeformer":
                    _twistAngle = EditorGUILayout.Slider("Twist Angle", _twistAngle, -360f, 360f);
                    EditorGUILayout.HelpBox("Twists the mesh around the specified axis.", MessageType.Info);
                    break;
                    
                case "TaperDeformer":
                    _taperFactor = EditorGUILayout.Slider("Taper Factor", _taperFactor, -2f, 2f);
                    EditorGUILayout.HelpBox("Tapers the mesh along the specified axis.", MessageType.Info);
                    break;
                    
                case "NoiseDeformer":
                    _noiseFrequency = EditorGUILayout.Slider("Noise Frequency", _noiseFrequency, 0.1f, 10f);
                    EditorGUILayout.HelpBox("Applies noise-based deformation to vertex positions.", MessageType.Info);
                    break;
                    
                case "WaveDeformer":
                    _waveAmplitude = EditorGUILayout.Slider("Wave Amplitude", _waveAmplitude, 0f, 2f);
                    _waveFrequency = EditorGUILayout.Slider("Wave Frequency", _waveFrequency, 0.1f, 10f);
                    EditorGUILayout.HelpBox("Creates wave-like deformation patterns.", MessageType.Info);
                    break;
                    
                case "SpherifyDeformer":
                    _spherifyFactor = EditorGUILayout.Slider("Spherify Factor", _spherifyFactor, 0f, 1f);
                    EditorGUILayout.HelpBox("Morphs the mesh towards a spherical shape.", MessageType.Info);
                    break;
                    
                case "RippleDeformer":
                    _waveAmplitude = EditorGUILayout.Slider("Ripple Amplitude", _waveAmplitude, 0f, 2f);
                    _waveFrequency = EditorGUILayout.Slider("Ripple Frequency", _waveFrequency, 0.1f, 10f);
                    EditorGUILayout.HelpBox("Creates ripple effects emanating from a center point.", MessageType.Info);
                    break;
                    
                case "SineDeformer":
                    _waveAmplitude = EditorGUILayout.Slider("Sine Amplitude", _waveAmplitude, 0f, 2f);
                    _waveFrequency = EditorGUILayout.Slider("Sine Frequency", _waveFrequency, 0.1f, 10f);
                    EditorGUILayout.HelpBox("Applies sinusoidal deformation along an axis.", MessageType.Info);
                    break;
                    
                default:
                    EditorGUILayout.HelpBox($"No specific parameters for {deformerName}. Use the basic Strength and Axis controls.", MessageType.Info);
                    break;
            }
            
            // Animation settings
            _showAnimationSettings = EditorGUILayout.Foldout(_showAnimationSettings, "Animation Settings");
            if (_showAnimationSettings)
            {
                EditorGUI.indentLevel++;
                _currentSettings.animate = EditorGUILayout.Toggle("Enable Animation", _currentSettings.animate);
                if (_currentSettings.animate)
                {
                    _currentSettings.animationSpeed = EditorGUILayout.Slider("Animation Speed", _currentSettings.animationSpeed, 0.1f, 5f);
                }
                EditorGUI.indentLevel--;
            }
        }

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