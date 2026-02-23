using System;
using UnityEngine;

namespace Vastcore.WorldGen.FieldEngine
{
    /// <summary>
    /// Height サンプラを IDensityField に変換するアダプタ。
    /// density = H(x,z) * heightScale - y
    /// </summary>
    public sealed class HeightmapDensityAdapter : IDensityField
    {
        private readonly Func<Vector2, float> _sampleHeight01;
        private readonly float _heightScale;
        private readonly Bounds _bounds;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public HeightmapDensityAdapter(Func<Vector2, float> sampleHeight01, float heightScale)
            : this(sampleHeight01, heightScale, new Bounds(Vector3.zero, Vector3.zero))
        {
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public HeightmapDensityAdapter(Func<Vector2, float> sampleHeight01, float heightScale, Bounds bounds)
        {
            _sampleHeight01 = sampleHeight01 ?? throw new ArgumentNullException(nameof(sampleHeight01));
            _heightScale = Mathf.Max(0.001f, heightScale);
            _bounds = bounds;
        }

        /// <inheritdoc />
        public float Sample(Vector3 worldPosition)
        {
            float normalizedHeight = Mathf.Clamp01(_sampleHeight01(new Vector2(worldPosition.x, worldPosition.z)));
            float terrainHeight = normalizedHeight * _heightScale;
            return terrainHeight - worldPosition.y;
        }

        /// <inheritdoc />
        public Bounds GetBounds()
        {
            return _bounds;
        }
    }
}
