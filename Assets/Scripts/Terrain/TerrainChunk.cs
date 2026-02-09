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
            if (config == null) { Debug.LogError("TerrainChunk.Build: config is null"); return; }
            if (provider == null) { Debug.LogError("TerrainChunk.Build: provider is null"); return; }

            int res = Mathf.Max(2, config.resolution);
            float size = Mathf.Max(1f, config.worldSize);
            float heightScale = Mathf.Max(0.1f, config.heightScale);

            // Generate normalized heights [0,1]
            var heights = new float[res * res];
            var ctx = new HeightmapGenerationContext { Seed = 0 }; // seed は settings 側で適用済み
            provider.Generate(heights, res, worldOrigin, size, ctx);

            // Unity Terrain expects heights in [0,1] but with 2D array [res,res]
            var heights2D = new float[res, res];
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    heights2D[y, x] = Mathf.Clamp01(heights[x + y * res]);
                }
            }

            TerrainData = new TerrainData
            {
                heightmapResolution = res,
            };
            TerrainData.size = new Vector3(size, heightScale, size);
            TerrainData.SetHeights(0, 0, heights2D);

            var go = gameObject;
            var terrain = go.GetComponent<UnityEngine.Terrain>();
            if (terrain == null) terrain = go.AddComponent<UnityEngine.Terrain>();
            terrain.terrainData = TerrainData;
            terrain.drawInstanced = true;
            UnityTerrain = terrain;
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
