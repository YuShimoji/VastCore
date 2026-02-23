using UnityEngine;

namespace Vastcore.WorldGen.Recipe
{
    /// <summary>
    /// WorldGenRecipe のレイヤー要素。
    /// </summary>
    [CreateAssetMenu(fileName = "FieldLayer", menuName = "Vastcore/WorldGen/Field Layer")]
    public sealed class FieldLayer : ScriptableObject
    {
        [Header("Common")]
        public FieldLayerType layerType = FieldLayerType.Heightmap;
        public BooleanOp booleanOp = BooleanOp.Union;
        [Range(0f, 4f)] public float weight = 1f;
        [Range(0f, 10f)] public float smoothK;
        [Tooltip("Size == Vector3.zero の場合は無限範囲として扱う。")]
        public Bounds worldBounds = default;

        [Header("Heightmap")]
        [Tooltip("HeightmapProviderSettings (Terrain assembly) を参照する。")]
        public ScriptableObject heightmapSettings;
        [Min(0.01f)] public float heightScale = 100f;

        [Header("Noise Density")]
        [Min(0.001f)] public float noiseScale = 100f;
        [Range(1, 12)] public int octaves = 4;
        [Min(1f)] public float lacunarity = 2f;
        [Range(0f, 1f)] public float gain = 0.5f;
        public Vector3 noiseOffset = Vector3.zero;

        [Header("Cave")]
        [Min(0.001f)] public float caveNoiseScale = 30f;
        [Range(0f, 1f)] public float caveThreshold = 0.55f;
        [Range(1, 8)] public int caveOctaves = 3;
        [Min(1f)] public float caveLacunarity = 2f;
        [Range(0f, 1f)] public float caveGain = 0.5f;

        /// <summary>
        /// 有限範囲レイヤーかどうか。
        /// </summary>
        public bool HasFiniteBounds => worldBounds.size != Vector3.zero;
    }
}
