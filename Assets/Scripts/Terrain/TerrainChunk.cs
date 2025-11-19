using UnityEngine;
using Vastcore.Terrain.Config;
using Vastcore.Terrain.Providers;

namespace Vastcore.Terrain
{
    /// <summary>
    /// 1 チャンクの Terrain を生成・保持するコンポーネント。
    /// </summary>
    public sealed class TerrainChunk : MonoBehaviour
    {
        [SerializeField] private TerrainGenerationConfig _config;
        [SerializeField] private Vector2 _worldOrigin;

        public UnityEngine.Terrain UnityTerrain { get; private set; }
        public TerrainData TerrainData { get; private set; }

        /// <summary>
        /// コンフィグとプロバイダを用いて Terrain を生成します。
        /// </summary>
        public void Build(TerrainGenerationConfig config, IHeightmapProvider provider, Vector2 worldOrigin)
        {
            _config = config;
            _worldOrigin = worldOrigin;

            if (!ValidateArguments(config, provider))
            {
                return;
            }

            ComputeDerivedParameters(config, out var resolution, out var size, out var heightScale);

            // Generate normalized heights [0,1]
            var heights = GenerateHeights1D(provider, resolution, worldOrigin, size);

            // Unity Terrain expects heights in [0,1] but with 2D array [res,res]
            var heights2D = ConvertTo2D(heights, resolution);

            TerrainData = CreateTerrainData(resolution, size, heightScale, heights2D);
            UnityTerrain = EnsureTerrainComponent(TerrainData);
        }

        private static bool ValidateArguments(TerrainGenerationConfig config, IHeightmapProvider provider)
        {
            if (config == null)
            {
                Debug.LogError("TerrainChunk.Build: config is null");
                return false;
            }

            if (provider == null)
            {
                Debug.LogError("TerrainChunk.Build: provider is null");
                return false;
            }

            return true;
        }

        private static void ComputeDerivedParameters(TerrainGenerationConfig config, out int resolution, out float size, out float heightScale)
        {
            resolution = Mathf.Max(2, config.resolution);
            size = Mathf.Max(1f, config.worldSize);
            heightScale = Mathf.Max(0.1f, config.heightScale);
        }

        private static float[] GenerateHeights1D(IHeightmapProvider provider, int resolution, Vector2 worldOrigin, float size)
        {
            var heights = new float[resolution * resolution];
            var ctx = new HeightmapGenerationContext { Seed = 0 }; // seed は settings 側で適用済み
            provider.Generate(heights, resolution, worldOrigin, size, ctx);
            return heights;
        }

        private static float[,] ConvertTo2D(float[] heights, int resolution)
        {
            var heights2D = new float[resolution, resolution];
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    heights2D[y, x] = Mathf.Clamp01(heights[x + y * resolution]);
                }
            }

            return heights2D;
        }

        private static TerrainData CreateTerrainData(int resolution, float size, float heightScale, float[,] heights2D)
        {
            var data = new TerrainData
            {
                heightmapResolution = resolution,
            };
            data.size = new Vector3(size, heightScale, size);
            data.SetHeights(0, 0, heights2D);
            return data;
        }

        private UnityEngine.Terrain EnsureTerrainComponent(TerrainData data)
        {
            var go = gameObject;
            var terrain = go.GetComponent<UnityEngine.Terrain>();
            if (terrain == null)
            {
                terrain = go.AddComponent<UnityEngine.Terrain>();
            }

            terrain.terrainData = data;
            terrain.drawInstanced = true;
            return terrain;
        }

        public static TerrainChunk CreateAndBuild(TerrainGenerationConfig config, IHeightmapProvider provider, Vector2 worldOrigin)
        {
            var go = new GameObject($"TerrainChunk_{worldOrigin.x}_{worldOrigin.y}");
            var chunk = go.AddComponent<TerrainChunk>();
            chunk.Build(config, provider, worldOrigin);
            return chunk;
        }
    }
}
