using System.Collections.Generic;
using UnityEngine;
using Vastcore.WorldGen.Common;
using Vastcore.WorldGen.Recipe;
using Vastcore.WorldGen.Stamps;

namespace Vastcore.WorldGen.FieldEngine
{
    /// <summary>
    /// Stamp インスタンス群を評価する密度場。
    /// </summary>
    public sealed class StampDensityField : IDensityField
    {
        /// <summary>
        /// 実行時評価可能な Stamp データ。
        /// </summary>
        public struct EvaluableStamp
        {
            public IStamp Stamp;
            public Matrix4x4 WorldToLocal;
            public Bounds WorldBounds;
            public BooleanOp BooleanOp;
            public float SmoothK;
            public float Weight;
        }

        private readonly List<EvaluableStamp> _stamps;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public StampDensityField(List<EvaluableStamp> stamps)
        {
            _stamps = stamps ?? new List<EvaluableStamp>();
        }

        /// <summary>
        /// Stamp 数。
        /// </summary>
        public int Count => _stamps.Count;

        /// <inheritdoc />
        public float Sample(Vector3 worldPosition)
        {
            if (_stamps.Count == 0)
                return -1f;

            EvaluableStamp first = _stamps[0];
            float result = EvaluateStamp(first, worldPosition);

            for (int i = 1; i < _stamps.Count; i++)
            {
                EvaluableStamp stamp = _stamps[i];
                float contribution = EvaluateStamp(stamp, worldPosition);

                switch (stamp.BooleanOp)
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
                        result = SdfMath.SmoothUnion(result, contribution, stamp.SmoothK);
                        break;
                    case BooleanOp.SmoothSubtract:
                        result = SdfMath.SmoothSubtract(result, contribution, stamp.SmoothK);
                        break;
                    case BooleanOp.SmoothIntersect:
                        result = SdfMath.SmoothIntersect(result, contribution, stamp.SmoothK);
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
            if (_stamps.Count == 0)
                return new Bounds(Vector3.zero, Vector3.zero);

            bool hasFiniteBounds = false;
            Bounds combined = default;

            for (int i = 0; i < _stamps.Count; i++)
            {
                Bounds b = _stamps[i].WorldBounds;
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

        private static float EvaluateStamp(EvaluableStamp stamp, Vector3 worldPosition)
        {
            if (stamp.Stamp == null)
                return 1f;

            Vector3 local = stamp.WorldToLocal.MultiplyPoint3x4(worldPosition);
            return stamp.Stamp.Evaluate(local) * Mathf.Max(0f, stamp.Weight);
        }
    }
}
