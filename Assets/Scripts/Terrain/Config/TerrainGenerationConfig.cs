using UnityEngine;
using Vastcore.Terrain.Erosion;
using Vastcore.Terrain.Providers;
using Vastcore.Utilities;

namespace Vastcore.Terrain.Config
{
    [CreateAssetMenu(fileName = "TerrainGenerationConfig", menuName = "Vastcore/Terrain/Generation Config")]
    public sealed class TerrainGenerationConfig : ScriptableObject
    {
        [Header("Heightmap")]
        public HeightmapProviderSettings heightmapSettings;
        [Min(2)] public int resolution = 257; // Unity Terrain は 2^n+1 が扱いやすい
        [Min(1f)] public float worldSize = 256f; // 1 チャンクの横幅（m）
        [Min(1f)] public float heightScale = 100f; // 地形の高さ（m）

        [Header("Erosion")]
        [Tooltip("エロージョン設定（null の場合はエロージョンなし）")]
        public ErosionSettings erosionSettings;

        public IHeightmapProvider CreateHeightProvider()
        {
            if (heightmapSettings == null)
            {
                VastcoreLogger.Instance.LogError("TerrainConfig", "TerrainGenerationConfig.heightmapSettings is null");
                return null;
            }
            return heightmapSettings.CreateProvider();
        }
    }
}
