using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    /// <summary>
    /// Stub class for BiomePresetManager to resolve compilation errors.
    /// Manages biome settings and presets.
    /// </summary>
    public class BiomePresetManager : MonoBehaviour
    {
        public List<BiomePreset> availablePresets = new List<BiomePreset>();
    }

    [System.Serializable]
    public class BiomePreset
    {
        public string biomeName;
        // Add other fields as discovered from usage
    }
}
