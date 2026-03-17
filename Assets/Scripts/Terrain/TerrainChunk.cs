using UnityEngine;
using Vastcore.Terrain.Config;
using Vastcore.Terrain.Erosion;
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

            // Apply erosion if configured
            if (config.erosionSettings != null && config.erosionSettings.enabled)
            {
                ApplyErosion(heights, res, config.erosionSettings);
            }

            // Unity Terrain expects heights in [0,1] with 2D array [z, x]
            var heights2D = new float[res, res];
            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    heights2D[z, x] = Mathf.Clamp01(heights[x + z * res]);
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

        /// <summary>
        /// 1D ハイトマップにエロージョンを適用する。
        /// heights は [0,1] 正規化された 1D 配列 (x + z * res indexing)。
        /// </summary>
        private void ApplyErosion(float[] _heights, int _res, ErosionSettings _settings)
        {
            // 1D → 2D [x, z] (エロージョンクラスの期待レイアウト)
            var map = new float[_res, _res];
            for (int z = 0; z < _res; z++)
                for (int x = 0; x < _res; x++)
                    map[x, z] = _heights[x + z * _res];

            // Hydraulic erosion
            if (_settings.enableHydraulic)
            {
                var hydraulic = new HydraulicErosion
                {
                    Iterations = _settings.hydraulicIterations,
                    ErosionRate = _settings.erosionRate,
                    DepositionRate = _settings.depositionRate
                };
                hydraulic.Apply(map, _settings.erosionSeed);
            }

            // Thermal erosion
            if (_settings.enableThermal)
            {
                var thermal = new ThermalErosion
                {
                    Iterations = _settings.thermalIterations,
                    TalusAngle = _settings.talusAngle
                };
                thermal.Apply(map);
            }

            // 2D → 1D に書き戻し (クランプ付き)
            for (int z = 0; z < _res; z++)
                for (int x = 0; x < _res; x++)
                    _heights[x + z * _res] = Mathf.Clamp01(map[x, z]);
        }
    }
}
