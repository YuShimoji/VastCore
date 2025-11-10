using UnityEngine;

namespace Vastcore.Terrain.Providers
{
    /// <summary>
    /// ノイズ(FBM)ベースの高さ供給。ワールド座標系ベースでサンプルするため、
    /// チャンク間の継ぎ目が出にくい設計。
    /// </summary>
    public sealed class NoiseHeightmapProvider : IHeightmapProvider
    {
        private readonly int _seed;
        private readonly float _scale;
        private readonly int _octaves;
        private readonly float _lacunarity;
        private readonly float _gain;
        private readonly Vector2 _offset;
        private readonly bool _domainWarp;
        private readonly float _warpStrength;
        private readonly float _warpFrequency;

        public NoiseHeightmapProvider(
            int seed,
            float scale,
            int octaves,
            float lacunarity,
            float gain,
            Vector2 offset,
            bool domainWarp = false,
            float warpStrength = 10f,
            float warpFrequency = 0.01f)
        {
            _seed = seed;
            _scale = Mathf.Max(1e-3f, scale);
            _octaves = Mathf.Max(1, octaves);
            _lacunarity = Mathf.Max(1.0f, lacunarity);
            _gain = Mathf.Clamp01(gain);
            _offset = offset;
            _domainWarp = domainWarp;
            _warpStrength = warpStrength;
            _warpFrequency = Mathf.Max(1e-5f, warpFrequency);
        }

        public void Generate(float[] heights, int resolution, Vector2 worldOrigin, float worldSize, in HeightmapGenerationContext context)
        {
            if (heights == null || heights.Length != resolution * resolution)
                throw new System.ArgumentException("heights length must be resolution*resolution");

            // 0..res-1 を 0..1 に正規化 → worldOrigin/worldSize に射影
            float inv = 1.0f / (resolution - 1);
            float baseFreq = 1.0f / Mathf.Max(1e-3f, _scale);

            // シードをオフセットに混ぜる（Perlinに直接seed不可のため）
            float seedOffX = HashTo01(_seed) * 10000.0f;
            float seedOffY = HashTo01(_seed * 397) * 10000.0f;

            for (int y = 0; y < resolution; y++)
            {
                float vy = worldOrigin.y + (y * inv) * worldSize;
                for (int x = 0; x < resolution; x++)
                {
                    float vx = worldOrigin.x + (x * inv) * worldSize;

                    // Domain warp（任意）
                    float wx = vx;
                    float wy = vy;
                    if (_domainWarp)
                    {
                        float qx = Perlin(wx * _warpFrequency + seedOffX, wy * _warpFrequency + seedOffY);
                        float qy = Perlin((wx + 17.123f) * _warpFrequency + seedOffX, (wy - 9.87f) * _warpFrequency + seedOffY);
                        wx += (qx - 0.5f) * 2f * _warpStrength;
                        wy += (qy - 0.5f) * 2f * _warpStrength;
                    }

                    float h = FBM(wx, wy, baseFreq, _octaves, _lacunarity, _gain, seedOffX, seedOffY);
                    heights[x + y * resolution] = Mathf.Clamp01(h);
                }
            }
        }

        private static float FBM(float wx, float wy, float baseFreq, int octaves, float lacunarity, float gain, float seedX, float seedY)
        {
            float amp = 1f;
            float freq = baseFreq;
            float sum = 0f;
            float norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                float n = Perlin(wx * freq + seedX, wy * freq + seedY); // 0..1
                n = n * 2f - 1f; // -1..1
                sum += n * amp;
                norm += amp;
                amp *= gain;
                freq *= lacunarity;
            }
            if (norm < 1e-6f) return 0.5f;
            float v = sum / (2f * norm) + 0.5f; // 正規化して 0..1
            return Mathf.Clamp01(v);
        }

        private static float Perlin(float x, float y)
        {
            return Mathf.PerlinNoise(x, y);
        }

        private static float HashTo01(int v)
        {
            unchecked
            {
                uint x = (uint)v;
                x ^= x >> 16;
                x *= 0x7feb352d;
                x ^= x >> 15;
                x *= 0x846ca68b;
                x ^= x >> 16;
                return (x & 0xFFFFFF) / (float)0x1000000; // 0..1 未満
            }
        }
    }
}
