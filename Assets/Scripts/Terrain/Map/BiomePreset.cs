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
    public class BiomePreset
    {
        public string biomeName = "Default";
        public float moisture = 0.5f;
        public float temperature = 0.5f;

        public BiomeMaterialSettings materialSettings = new BiomeMaterialSettings();
    }

    /// <summary>
    /// シンプルなバイオームプリセット管理クラス（レガシー互換用の最小スタブ）
    /// </summary>
    public class BiomePresetManager : MonoBehaviour
    {
        public List<BiomePreset> availablePresets = new List<BiomePreset>();
    }
}
