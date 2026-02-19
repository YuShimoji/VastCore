using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Vastcore.Generation
{
    [System.Serializable]
    public class BiomeMaterialSettings
    {
        public Color terrainTint = Color.white;
        public Color ambientColor = Color.gray;
    }

    [CreateAssetMenu(fileName = "BiomePreset", menuName = "VastCore/Biome Preset", order = 2)]
    public class BiomePreset : ScriptableObject
    {
        public string biomeName = "Default";
        public float moisture = 0.5f;
        public float temperature = 0.5f;
        public float fertility = 0.5f;
        public float rockiness = 0.5f;

        public BiomeMaterialSettings materialSettings = new BiomeMaterialSettings();

        public string presetName
        {
            get => biomeName;
            set => biomeName = value;
        }

        public void InitializeDefault()
        {
            biomeName = "Default";
            moisture = 0.5f;
            temperature = 0.5f;
            fertility = 0.5f;
            rockiness = 0.5f;
            materialSettings = new BiomeMaterialSettings();
        }
    }

    /// <summary>
    /// Runtime biome preset registry.
    /// </summary>
    public class BiomePresetManager : MonoBehaviour
    {
        public static BiomePresetManager Instance { get; private set; }

        [SerializeField] private string resourcesFolder = "BiomePresets";
        public List<BiomePreset> availablePresets = new List<BiomePreset>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            RefreshAvailablePresets();
        }

        public void RefreshAvailablePresets()
        {
            var all = new List<BiomePreset>();
            all.AddRange(Resources.LoadAll<BiomePreset>(resourcesFolder));

#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets("t:BiomePreset");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var preset = AssetDatabase.LoadAssetAtPath<BiomePreset>(path);
                if (preset != null)
                {
                    all.Add(preset);
                }
            }
#endif

            availablePresets = all.Where(p => p != null).Distinct().OrderBy(p => p.name).ToList();
        }
    }
}