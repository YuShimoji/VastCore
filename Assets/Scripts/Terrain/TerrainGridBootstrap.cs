using UnityEngine;
using Vastcore.Terrain.Config;
using Vastcore.Terrain.Providers;

namespace Vastcore.Terrain
{
    /// <summary>
    /// シーン起動時にグリッド状の地形チャンクを生成する簡易ブートストラップ。
    /// </summary>
    public sealed class TerrainGridBootstrap : MonoBehaviour
    {
        [Header("Generation")]
        public TerrainGenerationConfig config;
        [Min(1)] public int gridX = 3;
        [Min(1)] public int gridZ = 3;
        public Vector2 worldOrigin = Vector2.zero;
        public bool autoBuildOnStart = true;

        private IHeightmapProvider _provider;

        private void Start()
        {
            if (autoBuildOnStart)
            {
                BuildGrid();
            }
        }

        public void BuildGrid()
        {
            if (config == null)
            {
                Debug.LogError("TerrainGridBootstrap: config is null");
                return;
            }
            _provider = config.CreateHeightProvider();
            if (_provider == null)
            {
                Debug.LogError("TerrainGridBootstrap: provider create failed");
                return;
            }

            float size = Mathf.Max(1f, config.worldSize);
            for (int z = 0; z < gridZ; z++)
            {
                for (int x = 0; x < gridX; x++)
                {
                    Vector2 origin = new Vector2(worldOrigin.x + x * size, worldOrigin.y + z * size);
                    var chunk = TerrainChunk.CreateAndBuild(config, _provider, origin);
                    chunk.transform.SetParent(this.transform, worldPositionStays: true);
                }
            }
        }
    }
}
