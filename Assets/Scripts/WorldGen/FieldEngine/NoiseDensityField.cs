using UnityEngine;
using Vastcore.WorldGen.Common;

namespace Vastcore.WorldGen.FieldEngine
{
    /// <summary>
    /// 疑似 3D FBM ノイズ密度場。
    /// </summary>
    public sealed class NoiseDensityField : IDensityField
    {
        private readonly float _scale;
        private readonly int _octaves;
        private readonly float _lacunarity;
        private readonly float _gain;
        private readonly Vector3 _offset;
        private readonly Vector3 _seedOffset;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public NoiseDensityField(int seed, float scale, int octaves, float lacunarity, float gain, Vector3 offset)
        {
            _scale = Mathf.Max(0.001f, scale);
            _octaves = Mathf.Clamp(octaves, 1, 12);
            _lacunarity = Mathf.Max(1f, lacunarity);
            _gain = Mathf.Clamp01(gain);
            _offset = offset;

            // seed を疑似 3D オフセットに展開する。
            _seedOffset = new Vector3(
                DeterministicRng.HashTo01(seed * 73856093) * 10000f,
                DeterministicRng.HashTo01(seed * 19349663) * 10000f,
                DeterministicRng.HashTo01(seed * 83492791) * 10000f);
        }

        /// <inheritdoc />
        public float Sample(Vector3 worldPosition)
        {
            Vector3 p = (worldPosition + _offset + _seedOffset) / _scale;

            float frequency = 1f;
            float amplitude = 1f;
            float sum = 0f;
            float weightSum = 0f;

            for (int i = 0; i < _octaves; i++)
            {
                float n = SamplePseudo3D(p * frequency); // 0..1
                sum += n * amplitude;
                weightSum += amplitude;

                frequency *= _lacunarity;
                amplitude *= _gain;
            }

            if (weightSum <= 0f)
                return -1f;

            float normalized = sum / weightSum; // 0..1
            return normalized * 2f - 1f; // -1..1
        }

        /// <inheritdoc />
        public Bounds GetBounds()
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        private static float SamplePseudo3D(Vector3 p)
        {
            float xy = Mathf.PerlinNoise(p.x, p.y);
            float yz = Mathf.PerlinNoise(p.y + 31.416f, p.z + 59.2f);
            float zx = Mathf.PerlinNoise(p.z + 12.7f, p.x + 88.1f);
            return (xy + yz + zx) / 3f;
        }
    }
}
