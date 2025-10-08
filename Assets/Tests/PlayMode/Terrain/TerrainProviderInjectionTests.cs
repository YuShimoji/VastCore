using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Vastcore.Terrain.Config;
using Vastcore.Terrain.Providers;

namespace Vastcore.Tests.PlayMode.Terrain
{
    public class TerrainProviderInjectionTests
    {
        private static NoiseHeightmapSettings CreateNoiseSettings(int seed = 12345)
        {
            var s = ScriptableObject.CreateInstance<NoiseHeightmapSettings>();
            s.seed = seed;
            s.scale = 200f;
            s.octaves = 5;
            s.lacunarity = 2.0f;
            s.gain = 0.5f;
            s.offset = Vector2.zero;
            s.domainWarp = false;
            return s;
        }

        [Test]
        public void Provider_Reproducibility_SameSeed()
        {
            int res = 129;
            float worldSize = 256f;
            var origin = Vector2.zero;
            var settings = CreateNoiseSettings(111);
            var p1 = settings.CreateProvider();
            var p2 = settings.CreateProvider();

            var h1 = new float[res * res];
            var h2 = new float[res * res];
            var ctx = new HeightmapGenerationContext { Seed = 0 };
            p1.Generate(h1, res, origin, worldSize, ctx);
            p2.Generate(h2, res, origin, worldSize, ctx);

            for (int i = 0; i < h1.Length; i++)
            {
                Assert.That(Mathf.Abs(h1[i] - h2[i]) < 1e-6f, $"Index {i} differs: {h1[i]} vs {h2[i]}");
            }
        }

        [Test]
        public void Provider_DifferentSeed_ProducesDifferent()
        {
            int res = 129;
            float worldSize = 256f;
            var origin = Vector2.zero;
            var s1 = CreateNoiseSettings(111);
            var s2 = CreateNoiseSettings(222);
            var p1 = s1.CreateProvider();
            var p2 = s2.CreateProvider();

            var h1 = new float[res * res];
            var h2 = new float[res * res];
            var ctx = new HeightmapGenerationContext { Seed = 0 };
            p1.Generate(h1, res, origin, worldSize, ctx);
            p2.Generate(h2, res, origin, worldSize, ctx);

            bool anyDiff = false;
            for (int i = 0; i < h1.Length; i++)
            {
                if (Mathf.Abs(h1[i] - h2[i]) > 1e-4f) { anyDiff = true; break; }
            }
            Assert.IsTrue(anyDiff, "Different seeds should produce different height samples.");
        }

        [Test]
        public void Provider_Seamless_On_Adjacent_Chunks()
        {
            int res = 129;
            float worldSize = 256f;
            var leftOrigin = Vector2.zero;
            var rightOrigin = new Vector2(worldSize, 0f);
            var settings = CreateNoiseSettings(333);
            var provider = settings.CreateProvider();
            var ctx = new HeightmapGenerationContext { Seed = 0 };

            var left = new float[res * res];
            var right = new float[res * res];
            provider.Generate(left, res, leftOrigin, worldSize, ctx);
            provider.Generate(right, res, rightOrigin, worldSize, ctx);

            // 共有エッジ: 左の最右列(x=res-1) と 右の最左列(x=0) が一致
            for (int y = 0; y < res; y++)
            {
                float l = left[(res - 1) + y * res];
                float r = right[0 + y * res];
                Assert.That(Mathf.Abs(l - r) < 1e-4f, $"Seam mismatch at y={y}: {l} vs {r}");
            }
        }

        [Test]
        public void TerrainChunk_Builds_From_Provider()
        {
            // ランタイム生成が例外なく完了し、TerrainData が設定されることを確認
            var cfg = ScriptableObject.CreateInstance<TerrainGenerationConfig>();
            cfg.heightmapSettings = CreateNoiseSettings(444);
            cfg.resolution = 129;
            cfg.worldSize = 256f;
            cfg.heightScale = 80f;
            var provider = cfg.CreateHeightProvider();

            var chunk = new GameObject("TestChunk").AddComponent<Vastcore.Terrain.TerrainChunk>();
            chunk.Build(cfg, provider, Vector2.zero);
            Assert.IsNotNull(chunk.TerrainData);
            Assert.IsNotNull(chunk.UnityTerrain);
        }
    }
}
