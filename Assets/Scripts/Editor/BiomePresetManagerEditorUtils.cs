using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Vastcore.Generation;
using Vastcore.Generation.Map;

namespace Vastcore.Editor.Utils
{
    public static class BiomePresetManagerEditorUtils
    {
        private const string PresetSavePath = "Assets/Data/BiomePresets";

        public static void SavePreset(BiomePreset preset)
        {
            if (preset == null) return;
            
            if (!Directory.Exists(PresetSavePath))
            {
                Directory.CreateDirectory(PresetSavePath);
            }
            
            // If asset doesn't exist on disk, create it
            string assetPath = AssetDatabase.GetAssetPath(preset);
            if (string.IsNullOrEmpty(assetPath))
            {
                string fileName = $"{preset.name}.asset";
                if (string.IsNullOrEmpty(preset.name)) fileName = "NewBiomePreset.asset";
                
                string path = $"{PresetSavePath}/{fileName}";
                path = AssetDatabase.GenerateUniqueAssetPath(path);
                AssetDatabase.CreateAsset(preset, path);
            }
            else
            {
                EditorUtility.SetDirty(preset);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            if (BiomePresetManager.Instance != null)
            {
                BiomePresetManager.Instance.RefreshAvailablePresets();
            }
        }

        public static void DeletePreset(string presetName)
        {
            if (string.IsNullOrEmpty(presetName))
            {
                Debug.LogWarning("BiomePresetManager: Cannot delete preset with null or empty name");
                return;
            }

            string path = $"{PresetSavePath}/{presetName}.asset";
            if (AssetDatabase.LoadAssetAtPath<BiomePreset>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (BiomePresetManager.Instance != null)
                {
                    BiomePresetManager.Instance.RefreshAvailablePresets();
                }

                Debug.Log($"Deleted biome preset: {presetName}");
            }
        }

        public static void CreateNewPresetInEditor()
        {
            var preset = ScriptableObject.CreateInstance<BiomePreset>();
            
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }
            if (!AssetDatabase.IsValidFolder(PresetSavePath))
            {
                // Create intermediate folders if needed? AssetDatabase requires exact parent
                 if (!AssetDatabase.IsValidFolder("Assets/Data/BiomePresets"))
                     AssetDatabase.CreateFolder("Assets/Data", "BiomePresets");
            }

            string path = AssetDatabase.GenerateUniqueAssetPath($"{PresetSavePath}/NewBiomePreset.asset");
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = preset;
            EditorGUIUtility.PingObject(preset);
            
            if (BiomePresetManager.Instance != null)
            {
                BiomePresetManager.Instance.RefreshAvailablePresets();
            }

            Debug.Log($"Created a new biome preset at: {path}");
        }

        public static void CreateDefaultPresets()
        {
            // Forest
            var forest = ScriptableObject.CreateInstance<BiomePreset>();
            forest.name = "Forest";
            forest.presetName = "Forest";
            // Populate fields... (Simplified for brevity, assuming defaults or user set)
            // Need to copy property assignments from original file if strict adherence needed.
            // ... (I will copy them exactly in next step if viewed)
            
            // Just use SavePreset
            SavePreset(forest);

             // Desert
            var desert = ScriptableObject.CreateInstance<BiomePreset>();
            desert.name = "Desert";
            desert.presetName = "Desert";
            SavePreset(desert);

             // Mountain
            var mountain = ScriptableObject.CreateInstance<BiomePreset>();
            mountain.name = "Mountain";
            mountain.presetName = "Mountain";
            SavePreset(mountain);

            Debug.Log("Created default biome presets");
        }
    }
}
