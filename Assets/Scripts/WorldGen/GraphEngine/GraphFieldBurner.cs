using UnityEngine;
using Vastcore.WorldGen.FieldEngine;
using Vastcore.WorldGen.Recipe;

namespace Vastcore.WorldGen.GraphEngine
{
    /// <summary>
    /// Graph を IDensityField に焼き付ける処理。
    /// </summary>
    public sealed class GraphFieldBurner
    {
        /// <summary>
        /// Graph 焼き付け後の密度場を返す。
        /// </summary>
        public IDensityField Burn(IDensityField baseField, ConnectivityGraph graph, GraphGenerationSettings settings)
        {
            if (baseField == null || graph == null || graph.edges == null || graph.edges.Count == 0)
                return baseField;

            return new GraphBurnedDensityField(baseField, graph, settings);
        }

        private sealed class GraphBurnedDensityField : IDensityField
        {
            private readonly IDensityField _baseField;
            private readonly ConnectivityGraph _graph;
            private readonly GraphGenerationSettings _settings;

            public GraphBurnedDensityField(IDensityField baseField, ConnectivityGraph graph, GraphGenerationSettings settings)
            {
                _baseField = baseField;
                _graph = graph;
                _settings = settings ?? new GraphGenerationSettings();
            }

            public float Sample(Vector3 worldPosition)
            {
                float baseDensity = _baseField.Sample(worldPosition);
                float density = baseDensity;

                for (int i = 0; i < _graph.edges.Count; i++)
                {
                    GraphEdge edge = _graph.edges[i];
                    if (edge == null || edge.polyline == null || edge.polyline.Count < 2)
                        continue;

                    SegmentProjection nearest = FindNearestOnPolylineXZ(worldPosition, edge.polyline);
                    float halfWidth = Mathf.Max(0.1f, edge.width * 0.5f);
                    if (nearest.distanceXZ > halfWidth * 1.75f)
                        continue;

                    switch (edge.type)
                    {
                        case GraphEdgeType.Road:
                            density = ApplyRoadProfile(density, worldPosition, nearest.point, nearest.distanceXZ, halfWidth);
                            break;
                        case GraphEdgeType.River:
                            density = ApplyRiverProfile(density, worldPosition, nearest.point, nearest.distanceXZ, halfWidth);
                            break;
                    }
                }

                return density;
            }

            public Bounds GetBounds()
            {
                return _baseField.GetBounds();
            }

            private float ApplyRoadProfile(float baseDensity, Vector3 worldPosition, Vector3 nearestPoint, float distanceXZ, float halfWidth)
            {
                float influence = Smooth01(1f - distanceXZ / halfWidth);
                float targetDensity = nearestPoint.y - worldPosition.y;
                float blend = Mathf.Clamp01(influence * _settings.roadBurnBlend);
                return Mathf.Lerp(baseDensity, targetDensity, blend);
            }

            private float ApplyRiverProfile(float baseDensity, Vector3 worldPosition, Vector3 nearestPoint, float distanceXZ, float halfWidth)
            {
                float centerFactor = Smooth01(1f - distanceXZ / halfWidth);
                float bankBand = Mathf.Abs(distanceXZ - halfWidth);
                float bankFactor = Smooth01(1f - bankBand / (halfWidth * 0.65f));

                float bedY = nearestPoint.y - _settings.riverDepth * centerFactor;
                float bankY = nearestPoint.y + _settings.riverBankHeight * bankFactor;
                float targetY = Mathf.Lerp(bankY, bedY, centerFactor);
                float targetDensity = targetY - worldPosition.y;

                float blend = Mathf.Clamp01(
                    centerFactor * _settings.riverBurnBlend +
                    bankFactor * (_settings.riverBurnBlend * 0.35f));

                return Mathf.Lerp(baseDensity, targetDensity, blend);
            }

            private static float Smooth01(float v)
            {
                float t = Mathf.Clamp01(v);
                return t * t * (3f - 2f * t);
            }

            private static SegmentProjection FindNearestOnPolylineXZ(Vector3 p, System.Collections.Generic.List<Vector3> polyline)
            {
                SegmentProjection best = new SegmentProjection
                {
                    point = polyline[0],
                    distanceXZ = float.PositiveInfinity
                };

                for (int i = 0; i < polyline.Count - 1; i++)
                {
                    Vector3 a = polyline[i];
                    Vector3 b = polyline[i + 1];
                    SegmentProjection proj = ProjectPointOnSegmentXZ(p, a, b);
                    if (proj.distanceXZ < best.distanceXZ)
                        best = proj;
                }

                return best;
            }

            private static SegmentProjection ProjectPointOnSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
            {
                Vector2 p2 = new Vector2(p.x, p.z);
                Vector2 a2 = new Vector2(a.x, a.z);
                Vector2 b2 = new Vector2(b.x, b.z);
                Vector2 ab = b2 - a2;
                float lenSq = ab.sqrMagnitude;
                float t = lenSq > 1e-6f ? Vector2.Dot(p2 - a2, ab) / lenSq : 0f;
                t = Mathf.Clamp01(t);

                Vector3 point = Vector3.Lerp(a, b, t);
                float dx = p.x - point.x;
                float dz = p.z - point.z;
                return new SegmentProjection
                {
                    point = point,
                    distanceXZ = Mathf.Sqrt(dx * dx + dz * dz)
                };
            }

            private struct SegmentProjection
            {
                public Vector3 point;
                public float distanceXZ;
            }
        }
    }
}
