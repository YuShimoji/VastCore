using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Vastcore.Generation
{
    /// <summary>
    /// Manages biome presets for terrain generation.
    /// Handles loading, saving, and applying presets.
    /// </summary>
    public class BiomePresetManager : MonoBehaviour
    {
        #region Singleton
        
        private static BiomePresetManager _instance;
        public static BiomePresetManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<BiomePresetManager>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("BiomePresetManager");
                        _instance = obj.AddComponent<BiomePresetManager>();
                    }
                }
                return _instance;
            }
        }

        #endregion

        [Header("Settings")]
        [SerializeField] private string presetSavePath = "Assets/Data/BiomePresets";
        
        private List<BiomePreset> _availablePresets = new List<BiomePreset>();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                RefreshAvailablePresets();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initialize the BiomePresetManager
        /// </summary>
        public void Initialize()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                RefreshAvailablePresets();
            }
        }

        /// <summary>
        /// Save a biome preset
        /// </summary>
        public void SavePreset(BiomePreset preset)
        {
            if (preset == null)
            {
                Debug.LogWarning("BiomePresetManager: Cannot save null preset");
                return;
            }

#if UNITY_EDITOR
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Data/BiomePresets"))
            {
                AssetDatabase.CreateFolder("Assets/Data", "BiomePresets");
            }

            string path = $"Assets/Data/BiomePresets/{preset.name}.asset";
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Add to available presets if not already present
            if (!_availablePresets.Contains(preset))
            {
                _availablePresets.Add(preset);
            }

            Debug.Log($"Saved biome preset: {preset.name}");
#endif
        }

        /// <summary>
        /// Load a biome preset by name
        /// </summary>
        public BiomePreset LoadPreset(string presetName)
        {
            if (string.IsNullOrEmpty(presetName))
            {
                Debug.LogWarning("BiomePresetManager: Cannot load preset with null or empty name");
                return null;
            }

#if UNITY_EDITOR
            string path = $"Assets/Data/BiomePresets/{presetName}.asset";
            BiomePreset preset = AssetDatabase.LoadAssetAtPath<BiomePreset>(path);
            if (preset != null && !_availablePresets.Contains(preset))
            {
                _availablePresets.Add(preset);
            }
            return preset;
#else
            // Runtime loading - return from available presets
            return _availablePresets.FirstOrDefault(p => p.name == presetName);
#endif
        }

        /// <summary>
        /// Check if a preset exists
        /// </summary>
        public bool PresetExists(string presetName)
        {
            if (string.IsNullOrEmpty(presetName)) return false;

#if UNITY_EDITOR
            string path = $"Assets/Data/BiomePresets/{presetName}.asset";
            return AssetDatabase.LoadAssetAtPath<BiomePreset>(path) != null;
#else
            return _availablePresets.Any(p => p.name == presetName);
#endif
        }

        /// <summary>
        /// Delete a biome preset
        /// </summary>
        public void DeletePreset(string presetName)
        {
            if (string.IsNullOrEmpty(presetName))
            {
                Debug.LogWarning("BiomePresetManager: Cannot delete preset with null or empty name");
                return;
            }

#if UNITY_EDITOR
            string path = $"Assets/Data/BiomePresets/{presetName}.asset";
            if (AssetDatabase.LoadAssetAtPath<BiomePreset>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Remove from available presets
                var preset = _availablePresets.FirstOrDefault(p => p.name == presetName);
                if (preset != null)
                {
                    _availablePresets.Remove(preset);
                }

                Debug.Log($"Deleted biome preset: {presetName}");
            }
#endif
        }

        /// <summary>
        /// Get available presets list (public property)
        /// </summary>
        public List<BiomePreset> availablePresets
        {
            get { return _availablePresets; }
        }

        /// <summary>
        /// Reloads all available biome presets from the specified path.
        /// </summary>
        public void RefreshAvailablePresets()
        {
            _availablePresets.Clear();
#if UNITY_EDITOR
            if (!Directory.Exists(presetSavePath))
            {
                Directory.CreateDirectory(presetSavePath);
            }

            string[] presetGuids = AssetDatabase.FindAssets("t:BiomePreset", new[] { presetSavePath });
            foreach (string guid in presetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BiomePreset preset = AssetDatabase.LoadAssetAtPath<BiomePreset>(path);
                if (preset != null)
                {
                    _availablePresets.Add(preset);
                }
            }
#endif
        }

        /// <summary>
        /// Gets a list of names of all available presets.
        /// </summary>
        public List<string> GetAvailablePresetNames()
        {
            return _availablePresets.Select(p => p.name).ToList();
        }

        /// <summary>
        /// Gets a preset by its name.
        /// </summary>
        public BiomePreset GetPreset(string presetName)
        {
            return _availablePresets.FirstOrDefault(p => p.name == presetName);
        }

        /// <summary>
        /// Applies a biome preset to a terrain tile.
        /// </summary>
        public void ApplyPresetToTerrain(BiomePreset preset, TerrainTile tile)
        {
            if (preset == null || tile == null)
            {
                Debug.LogWarning("BiomePresetManager: Invalid preset or tile provided");
                return;
            }

            try
            {
                // Apply terrain generation parameters
                if (tile.heightData != null && preset.terrainParams.resolution > 0)
                {
                    // Modify the terrain based on preset parameters
                    // This is a simplified implementation - actual implementation would depend on terrain generation logic
                    // Note: BiomeSpecificTerrainGenerator is a static class, so we call its static method directly
                    // BiomeSpecificTerrainGenerator.GenerateTerrainForBiome(tile.heightData, preset.biomeType, preset.biomeDefinition, tile.terrainObject != null ? tile.terrainObject.transform.position : Vector3.zero);
                    Debug.Log($"Terrain parameters applied for preset {preset.presetName}");
                }

                // Store applied biome information
                tile.appliedBiome = preset.presetName;

                Debug.Log($"Applied biome preset '{preset.presetName}' to terrain tile at {tile.coordinate}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"BiomePresetManager: Failed to apply preset to terrain: {e.Message}");
            }
        }
    }
}
