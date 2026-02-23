using UnityEngine;
using Vastcore.Terrain.Config;
using Vastcore.Terrain.Providers;
using Vastcore.Utilities;
using Vastcore.WorldGen.FieldEngine;

namespace Vastcore.Terrain.Facade
{
    /// <summary>
    /// Terrain 側の HeightmapProviderSettings を WorldGen の IDensityField へ橋渡しするファクトリ。
    /// </summary>
    public sealed class TerrainHeightmapFieldFactory : IHeightmapFieldFactory
    {
        private readonly int _samplingResolution;
        private readonly float _tileWorldSize;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public TerrainHeightmapFieldFactory(int samplingResolution = 129, float tileWorldSize = 256f)
        {
            _samplingResolution = Mathf.Max(8, samplingResolution);
            _tileWorldSize = Mathf.Max(1f, tileWorldSize);
        }

        /// <inheritdoc />
        public IDensityField CreateFromSettings(ScriptableObject heightmapSettings, float heightScale, int seed)
        {
            HeightmapProviderSettings settings = heightmapSettings as HeightmapProviderSettings;
            if (settings == null)
            {
                VastcoreLogger.Instance.LogWarning("TerrainHeightmapFieldFactory", "heightmapSettings is not HeightmapProviderSettings.");
                return null;
            }

            IHeightmapProvider provider = settings.CreateProvider();
            if (provider == null)
            {
                VastcoreLogger.Instance.LogWarning("TerrainHeightmapFieldFactory", "CreateProvider returned null.");
                return null;
            }

            return new ProviderBackedHeightmapField(
                provider,
                Mathf.Max(0.001f, heightScale),
                _samplingResolution,
                _tileWorldSize,
                seed);
        }

        /// <summary>
        /// IHeightmapProvider をタイルキャッシュ付き IDensityField として扱う実装。
        /// </summary>
        private sealed class ProviderBackedHeightmapField : IDensityField
        {
            private readonly IHeightmapProvider _provider;
            private readonly float _heightScale;
            private readonly int _resolution;
            private readonly float _tileSize;
            private readonly HeightmapGenerationContext _context;
            private readonly float[] _cachedHeights;

            private Vector2 _cachedOrigin;
            private bool _hasCache;

            public ProviderBackedHeightmapField(
                IHeightmapProvider provider,
                float heightScale,
                int resolution,
                float tileSize,
                int seed)
            {
                _provider = provider;
                _heightScale = heightScale;
                _resolution = resolution;
                _tileSize = tileSize;
                _context = new HeightmapGenerationContext { Seed = seed };
                _cachedHeights = new float[_resolution * _resolution];
            }

            public float Sample(Vector3 worldPosition)
            {
                Vector2 worldXZ = new Vector2(worldPosition.x, worldPosition.z);
                EnsureCacheFor(worldXZ);

                float u = Mathf.Clamp01((worldXZ.x - _cachedOrigin.x) / _tileSize);
                float v = Mathf.Clamp01((worldXZ.y - _cachedOrigin.y) / _tileSize);
                float h01 = BilinearSample(u, v);

                float terrainHeight = h01 * _heightScale;
                return terrainHeight - worldPosition.y;
            }

            public Bounds GetBounds()
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            private void EnsureCacheFor(Vector2 worldXZ)
            {
                Vector2 nextOrigin = new Vector2(
                    Mathf.Floor(worldXZ.x / _tileSize) * _tileSize,
                    Mathf.Floor(worldXZ.y / _tileSize) * _tileSize);

                if (_hasCache && nextOrigin == _cachedOrigin)
                    return;

                _cachedOrigin = nextOrigin;
                _provider.Generate(_cachedHeights, _resolution, _cachedOrigin, _tileSize, in _context);
                _hasCache = true;
            }

            private float BilinearSample(float u, float v)
            {
                float fx = u * (_resolution - 1);
                float fy = v * (_resolution - 1);

                int x0 = Mathf.Clamp((int)fx, 0, _resolution - 2);
                int y0 = Mathf.Clamp((int)fy, 0, _resolution - 2);
                int x1 = x0 + 1;
                int y1 = y0 + 1;

                float tx = fx - x0;
                float ty = fy - y0;

                float h00 = _cachedHeights[FlatIndex(x0, y0)];
                float h10 = _cachedHeights[FlatIndex(x1, y0)];
                float h01 = _cachedHeights[FlatIndex(x0, y1)];
                float h11 = _cachedHeights[FlatIndex(x1, y1)];

                float hx0 = Mathf.Lerp(h00, h10, tx);
                float hx1 = Mathf.Lerp(h01, h11, tx);
                return Mathf.Lerp(hx0, hx1, ty);
            }

            private int FlatIndex(int x, int y)
            {
                return x + y * _resolution;
            }
        }
    }
}
