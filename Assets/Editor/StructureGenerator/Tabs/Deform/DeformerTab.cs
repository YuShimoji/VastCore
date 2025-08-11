using UnityEditor;
using UnityEngine;
using Vastcore.Editor.Generation;
using System;
using System.Collections.Generic;
using System.Linq;
using Deform;

namespace Vastcore.Editor.StructureGenerator.Tabs
{
    /// <summary>
    /// Represents the "Deform" tab in the Structure Generator editor window.
    /// </summary>
    public class DeformerTab : BaseStructureTab
    {
        public override TabCategory Category => TabCategory.Editing;
        public override string DisplayName => "Deform";
        public override string Description => "Apply various deformations to the generated structures.";

        private List<Type> _availableDeformers;
        private string[] _deformerNames;
        private int _selectedDeformerIndex = 0;

        public DeformerTab(StructureGeneratorWindow parent) : base(parent) 
        {
            FindAvailableDeformers();
        }

        private void FindAvailableDeformers()
        {
            _availableDeformers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(Deformer)) && !type.IsAbstract)
                .ToList();

            _deformerNames = _availableDeformers.Select(t => t.Name).ToArray();
        }

        protected override void DrawTabContent()
        {
            if (_availableDeformers == null || _availableDeformers.Count == 0)
            {
                EditorGUILayout.HelpBox("No Deformers found. Please ensure the Deform package is installed correctly.", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("Select Deformer", EditorStyles.boldLabel);
            _selectedDeformerIndex = EditorGUILayout.Popup("Deformer Type", _selectedDeformerIndex, _deformerNames);

            EditorGUILayout.Space();

            // Future implementation:
            // - Display parameters for the selected deformer.
            // - Sliders and fields to control deformer parameters.
            // - Button to apply the deformer to the selected object.
        }

        public override void ProcessSelectedObjects()
        {
            if (Selection.activeGameObject == null)
            {
                EditorUtility.DisplayDialog("No Object Selected", "Please select a GameObject in the scene to apply the deformer to.", "OK");
                return;
            }

            if (_selectedDeformerIndex < 0 || _selectedDeformerIndex >= _availableDeformers.Count)
            {
                Debug.LogError("Invalid deformer selected.");
                return;
            }

            var selectedDeformerType = _availableDeformers[_selectedDeformerIndex];
            var targetObject = Selection.activeGameObject;
            
            // DeformerManagerを介して適用する（将来的に実装）
            // DeformIntegrationManager.ApplyDeformer(targetObject, selectedDeformerType);
            
            Debug.Log($"Applying {selectedDeformerType.Name} to {targetObject.name}...");
        }
    }
}
 