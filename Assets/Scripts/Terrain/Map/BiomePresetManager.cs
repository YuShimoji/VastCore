using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Vastcore.Generation.Map
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

#if UNITY_EDITOR
        /// <summary>
        /// Creates a new BiomePreset asset in the editor.
        /// </summary>
        [MenuItem("Vastcore/Create New Biome Preset")]
        public static void CreateNewPresetInEditor()
        {
            var preset = ScriptableObject.CreateInstance<BiomePreset>();
            // preset.InitializeDefault(); // Assuming BiomePreset has this method

            if (!AssetDatabase.IsValidFolder("Assets/Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Data/BiomePresets"))
            {
                AssetDatabase.CreateFolder("Assets/Data", "BiomePresets");
            }

            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Data/BiomePresets/NewBiomePreset.asset");
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = preset;
            EditorGUIUtility.PingObject(preset);

            Debug.Log($"Created a new biome preset at: {path}");
        }
#endif
    }
}
