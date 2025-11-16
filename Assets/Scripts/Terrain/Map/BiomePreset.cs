using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    public class BiomePreset : ScriptableObject
    {
        public string presetName = "Default Biome";

        [Range(0f, 1f)] public float moisture = 0.5f;
        [Range(0f, 1f)] public float temperature = 0.5f;
        [Range(0f, 1f)] public float fertility = 0.5f;
        [Range(0f, 1f)] public float rockiness = 0.5f;

        [System.Serializable]
        public class MaterialSettings
        {
            public Color terrainTint = Color.white;
            public Color ambientColor = Color.gray;
        }

        public MaterialSettings materialSettings = new MaterialSettings();

        public void InitializeDefault()
        {
            if (string.IsNullOrEmpty(presetName))
            {
                presetName = "Default Biome";
            }
        }
    }
}
