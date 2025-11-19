using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    [System.Serializable]
    public class BiomeMaterialSettings
    {
        public Color terrainTint = Color.white;
        public Color ambientColor = Color.gray;
    }

    [System.Serializable]
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
    /// シンプルなバイオームプリセット管理クラス（レガシー互換用の最小スタブ）
    /// </summary>
    public class BiomePresetManagerLegacy : MonoBehaviour
    {
        public List<BiomePreset> availablePresets = new List<BiomePreset>();
    }
}
