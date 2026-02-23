using System.Collections.Generic;
using UnityEngine;
using Vastcore.WorldGen.Common;
using Vastcore.WorldGen.Recipe;

namespace Vastcore.WorldGen.FieldEngine
{
    /// <summary>
    /// 複数の密度場を Boolean 演算で逐次合成する。
    /// </summary>
    public sealed class CompositeDensityField : IDensityField
    {
        private sealed class Entry
        {
            public IDensityField Field;
            public BooleanOp Op;
            public float Weight;
            public float SmoothK;
        }

        private readonly List<Entry> _entries = new List<Entry>();

        /// <summary>
        /// 合成対象フィールド数。
        /// </summary>
        public int Count => _entries.Count;

        /// <summary>
        /// 合成対象フィールドを追加する。
        /// </summary>
        public void AddField(IDensityField field, BooleanOp op, float weight, float smoothK)
        {
            if (field == null)
                return;

            _entries.Add(new Entry
            {
                Field = field,
                Op = op,
                Weight = Mathf.Max(0f, weight),
                SmoothK = Mathf.Max(0f, smoothK)
            });
        }

        /// <inheritdoc />
        public float Sample(Vector3 worldPosition)
        {
            if (_entries.Count == 0)
                return -1f;

            float result = _entries[0].Field.Sample(worldPosition) * _entries[0].Weight;

            for (int i = 1; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                float contribution = entry.Field.Sample(worldPosition) * entry.Weight;

                switch (entry.Op)
                {
                    case BooleanOp.Union:
                        result = SdfMath.Union(result, contribution);
                        break;
                    case BooleanOp.Subtract:
                        result = SdfMath.Subtract(result, contribution);
                        break;
                    case BooleanOp.Intersect:
                        result = SdfMath.Intersect(result, contribution);
                        break;
                    case BooleanOp.SmoothUnion:
                        result = SdfMath.SmoothUnion(result, contribution, entry.SmoothK);
                        break;
                    case BooleanOp.SmoothSubtract:
                        result = SdfMath.SmoothSubtract(result, contribution, entry.SmoothK);
                        break;
                    case BooleanOp.SmoothIntersect:
                        result = SdfMath.SmoothIntersect(result, contribution, entry.SmoothK);
                        break;
                    default:
                        result = SdfMath.Union(result, contribution);
                        break;
                }
            }

            return result;
        }

        /// <inheritdoc />
        public Bounds GetBounds()
        {
            if (_entries.Count == 0)
                return new Bounds(Vector3.zero, Vector3.zero);

            bool hasFiniteBounds = false;
            Bounds combined = default;

            for (int i = 0; i < _entries.Count; i++)
            {
                Bounds b = _entries[i].Field.GetBounds();
                if (b.size == Vector3.zero)
                    continue;

                if (!hasFiniteBounds)
                {
                    combined = b;
                    hasFiniteBounds = true;
                }
                else
                {
                    combined.Encapsulate(b.min);
                    combined.Encapsulate(b.max);
                }
            }

            return hasFiniteBounds ? combined : new Bounds(Vector3.zero, Vector3.zero);
        }
    }
}
