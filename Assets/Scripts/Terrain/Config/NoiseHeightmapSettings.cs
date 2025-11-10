using UnityEngine;
using Vastcore.Terrain.Providers;

namespace Vastcore.Terrain.Config
{
    [CreateAssetMenu(fileName = "NoiseHeightmapSettings", menuName = "Vastcore/Terrain/Noise Heightmap Settings")]
    public sealed class NoiseHeightmapSettings : HeightmapProviderSettings
    {
        [Header("Noise (FBM)")] public int seed = 12345;
        [Min(0.001f)] public float scale = 200f;
        [Range(1, 12)] public int octaves = 5;
        [Min(1.0f)] public float lacunarity = 2.0f;
        [Range(0.0f, 1.0f)] public float gain = 0.5f;
        public Vector2 offset = Vector2.zero;

        [Header("Domain Warp (optional)")] public bool domainWarp = false;
        [Min(0f)] public float warpStrength = 10f;
        [Min(0.00001f)] public float warpFrequency = 0.01f;

        public override IHeightmapProvider CreateProvider()
        {
            return new NoiseHeightmapProvider(
                seed,
                scale,
                octaves,
                lacunarity,
                gain,
                offset,
                domainWarp,
                warpStrength,
                warpFrequency
            );
        }
    }
}
