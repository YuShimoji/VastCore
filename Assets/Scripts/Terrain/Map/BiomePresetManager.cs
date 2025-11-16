using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    public class BiomePresetManager : MonoBehaviour
    {
        public List<BiomePreset> availablePresets = new List<BiomePreset>();

        public BiomePreset GetPresetByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            for (int i = 0; i < availablePresets.Count; i++)
            {
                var preset = availablePresets[i];
                if (preset != null && preset.presetName == name)
                {
                    return preset;
                }
            }

            return null;
        }
    }
}
