using System.Collections.Generic;
using UnityEngine;
using Vastcore.Utilities;
using Vastcore.WorldGen.Common;
using Vastcore.WorldGen.Recipe;
using Vastcore.WorldGen.Stamps;

namespace Vastcore.WorldGen.FieldEngine
{
    /// <summary>
    /// Recipe から合成密度場を構築する Field Engine 実装。
    /// </summary>
    public sealed class FieldEngineManager : IFieldEngine
    {
        /// <inheritdoc />
        public IHeightmapFieldFactory HeightmapFieldFactory { get; set; }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public FieldEngineManager(IHeightmapFieldFactory heightmapFieldFactory = null)
        {
            HeightmapFieldFactory = heightmapFieldFactory;
        }

        /// <inheritdoc />
        public IDensityField BuildField(WorldGenRecipe recipe)
        {
            CompositeDensityField composite = new CompositeDensityField();
            if (recipe == null)
            {
                VastcoreLogger.Instance.LogError("WorldGen.FieldEngine", "BuildField failed: recipe is null.");
                return composite;
            }

            List<string> errors = RecipeValidator.Validate(recipe);
            for (int i = 0; i < errors.Count; i++)
            {
                VastcoreLogger.Instance.LogWarning("WorldGen.FieldEngine", errors[i]);
            }

            if (recipe.layers != null)
            {
                for (int i = 0; i < recipe.layers.Count; i++)
                {
                    FieldLayer layer = recipe.layers[i];
                    IDensityField layerField = CreateLayerField(layer, recipe.seed + i * 997);
                    if (layerField == null)
                        continue;

                    composite.AddField(layerField, layer.booleanOp, layer.weight, layer.smoothK);
                }
            }

            StampDensityField stampField = BuildStampField(recipe);
            if (stampField != null && stampField.Count > 0)
            {
                composite.AddField(stampField, BooleanOp.Union, 1f, 0f);
            }

            return composite;
        }

        /// <inheritdoc />
        public void FillDensityGrid(IDensityField field, DensityGrid grid, ChunkBounds bounds)
        {
            FieldSampler.Fill(field, grid, bounds);
        }

        private IDensityField CreateLayerField(FieldLayer layer, int layerSeed)
        {
            if (layer == null)
                return null;

            switch (layer.layerType)
            {
                case FieldLayerType.Heightmap:
                    if (HeightmapFieldFactory == null)
                    {
                        VastcoreLogger.Instance.LogWarning("WorldGen.FieldEngine", "Heightmap layer skipped: no IHeightmapFieldFactory registered.");
                        return null;
                    }

                    return HeightmapFieldFactory.CreateFromSettings(layer.heightmapSettings, layer.heightScale, layerSeed);

                case FieldLayerType.NoiseDensity:
                    return new NoiseDensityField(
                        layerSeed,
                        layer.noiseScale,
                        layer.octaves,
                        layer.lacunarity,
                        layer.gain,
                        layer.noiseOffset);

                case FieldLayerType.Cave:
                    return new CaveDensityField(
                        layerSeed,
                        layer.caveNoiseScale,
                        layer.caveThreshold,
                        layer.caveOctaves,
                        layer.caveLacunarity,
                        layer.caveGain);

                case FieldLayerType.SDF:
                    // SDF は recipe.stamps を StampDensityField として処理する。
                    return null;

                default:
                    return null;
            }
        }

        private static StampDensityField BuildStampField(WorldGenRecipe recipe)
        {
            if (recipe == null || recipe.stamps == null || recipe.stamps.Count == 0)
                return null;

            List<StampDensityField.EvaluableStamp> evaluable = new List<StampDensityField.EvaluableStamp>(recipe.stamps.Count);
            for (int i = 0; i < recipe.stamps.Count; i++)
            {
                StampInstanceData data = recipe.stamps[i];
                if (data == null || data.stamp == null)
                    continue;

                StampBase stamp = data.stamp;
                Vector3 safeScale = SafeScale(data.scale);
                Matrix4x4 localToWorld = Matrix4x4.TRS(data.position, data.rotation, safeScale);
                Matrix4x4 worldToLocal = localToWorld.inverse;

                evaluable.Add(new StampDensityField.EvaluableStamp
                {
                    Stamp = stamp,
                    WorldToLocal = worldToLocal,
                    WorldBounds = TransformBounds(stamp.GetLocalBounds(), localToWorld),
                    BooleanOp = data.booleanOp,
                    SmoothK = data.smoothK,
                    Weight = data.weight
                });
            }

            if (evaluable.Count == 0)
                return null;

            return new StampDensityField(evaluable);
        }

        private static Vector3 SafeScale(Vector3 scale)
        {
            float x = Mathf.Abs(scale.x) < 0.0001f ? 0.0001f : scale.x;
            float y = Mathf.Abs(scale.y) < 0.0001f ? 0.0001f : scale.y;
            float z = Mathf.Abs(scale.z) < 0.0001f ? 0.0001f : scale.z;
            return new Vector3(x, y, z);
        }

        private static Bounds TransformBounds(Bounds localBounds, Matrix4x4 localToWorld)
        {
            if (localBounds.size == Vector3.zero)
                return new Bounds(Vector3.zero, Vector3.zero);

            Vector3 c = localBounds.center;
            Vector3 e = localBounds.extents;
            Vector3[] points =
            {
                new Vector3(c.x - e.x, c.y - e.y, c.z - e.z),
                new Vector3(c.x + e.x, c.y - e.y, c.z - e.z),
                new Vector3(c.x - e.x, c.y + e.y, c.z - e.z),
                new Vector3(c.x + e.x, c.y + e.y, c.z - e.z),
                new Vector3(c.x - e.x, c.y - e.y, c.z + e.z),
                new Vector3(c.x + e.x, c.y - e.y, c.z + e.z),
                new Vector3(c.x - e.x, c.y + e.y, c.z + e.z),
                new Vector3(c.x + e.x, c.y + e.y, c.z + e.z)
            };

            Vector3 min = localToWorld.MultiplyPoint3x4(points[0]);
            Vector3 max = min;

            for (int i = 1; i < points.Length; i++)
            {
                Vector3 p = localToWorld.MultiplyPoint3x4(points[i]);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }

            Bounds bounds = new Bounds((min + max) * 0.5f, max - min);
            return bounds;
        }

        /// <summary>
        /// 洞窟向けの減算ノイズ密度場。
        /// </summary>
        private sealed class CaveDensityField : IDensityField
        {
            private readonly NoiseDensityField _noiseField;
            private readonly float _threshold;

            public CaveDensityField(int seed, float scale, float threshold, int octaves, float lacunarity, float gain)
            {
                _noiseField = new NoiseDensityField(seed, scale, octaves, lacunarity, gain, Vector3.zero);
                _threshold = Mathf.Clamp01(threshold);
            }

            public float Sample(Vector3 worldPosition)
            {
                // _noiseField は -1..1。0..1 に戻してしきい値との差を返す。
                float n01 = (_noiseField.Sample(worldPosition) + 1f) * 0.5f;
                return _threshold - n01;
            }

            public Bounds GetBounds()
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }
        }
    }
}
