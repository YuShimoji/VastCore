using System.Collections.Generic;
using UnityEngine;
using Vastcore.WorldGen.Recipe;

namespace Vastcore.WorldGen.GraphEngine
{
    /// <summary>
    /// Graph のポリライン・幅・交差点を Scene 上に可視化する Gizmo 描画コンポーネント。
    /// </summary>
    [ExecuteAlways]
    public sealed class WorldGenGraphGizmoVisualizer : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private WorldGenRecipe _recipe;
        [SerializeField] private GraphAsset _graphAssetOverride;
        [SerializeField] private bool _autoRegenerate = true;

        [Header("Visibility")]
        [SerializeField] private bool _drawAlways = false;
        [SerializeField] private bool _drawPolyline = true;
        [SerializeField] private bool _drawWidth = true;
        [SerializeField] private bool _drawIntersections = true;
        [SerializeField] private bool _drawNodeLabels = false;

        [Header("Style")]
        [SerializeField] private Color _roadColor = new Color(0.95f, 0.75f, 0.2f, 1f);
        [SerializeField] private Color _riverColor = new Color(0.2f, 0.7f, 1f, 1f);
        [SerializeField] private Color _intersectionColor = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField, Min(0.01f)] private float _lineYOffset = 0.2f;
        [SerializeField, Min(0.01f)] private float _intersectionRadius = 2f;
        [SerializeField, Min(0.01f)] private float _widthSampleStep = 8f;

        private ConnectivityGraph _cachedGraph;
        private int _cachedHash;

        /// <summary>
        /// 可視化対象レシピ。
        /// </summary>
        public WorldGenRecipe Recipe
        {
            get => _recipe;
            set => _recipe = value;
        }

        private void OnDrawGizmos()
        {
            if (!_drawAlways)
                return;
            DrawGraphGizmos();
        }

        private void OnDrawGizmosSelected()
        {
            DrawGraphGizmos();
        }

        private void DrawGraphGizmos()
        {
            ConnectivityGraph graph = ResolveGraph();
            if (graph == null || graph.edges == null || graph.edges.Count == 0)
                return;

            if (_drawPolyline || _drawWidth)
                DrawEdges(graph);
            if (_drawIntersections)
                DrawIntersections(graph);
        }

        private ConnectivityGraph ResolveGraph()
        {
            int nextHash = ComputeSourceHash();
            if (_autoRegenerate && (_cachedGraph == null || _cachedHash != nextHash))
            {
                _cachedGraph = GraphPreviewUtility.BuildPreviewGraph(_recipe, _graphAssetOverride);
                _cachedHash = nextHash;
            }

            if (_cachedGraph != null)
                return _cachedGraph;

            _cachedGraph = GraphPreviewUtility.BuildPreviewGraph(_recipe, _graphAssetOverride);
            _cachedHash = nextHash;
            return _cachedGraph;
        }

        private int ComputeSourceHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (_recipe != null ? _recipe.ComputeRecipeHash() : 0);
                hash = hash * 31 + (_graphAssetOverride != null ? _graphAssetOverride.name.GetHashCode() : 0);
                return hash;
            }
        }

        private void DrawEdges(ConnectivityGraph graph)
        {
            for (int i = 0; i < graph.edges.Count; i++)
            {
                GraphEdge edge = graph.edges[i];
                if (edge == null || edge.polyline == null || edge.polyline.Count < 2)
                    continue;

                Color c = edge.type == GraphEdgeType.River ? _riverColor : _roadColor;
                float halfWidth = Mathf.Max(0.1f, edge.width * 0.5f);

                for (int p = 0; p < edge.polyline.Count - 1; p++)
                {
                    Vector3 a = edge.polyline[p] + Vector3.up * _lineYOffset;
                    Vector3 b = edge.polyline[p + 1] + Vector3.up * _lineYOffset;
                    Vector3 dir = (b - a);
                    dir.y = 0f;
                    if (dir.sqrMagnitude < 1e-6f)
                        continue;
                    dir.Normalize();
                    Vector3 normal = new Vector3(-dir.z, 0f, dir.x);

                    if (_drawPolyline)
                    {
                        Gizmos.color = c;
                        Gizmos.DrawLine(a, b);
                    }

                    if (_drawWidth)
                    {
                        Gizmos.color = new Color(c.r, c.g, c.b, 0.8f);
                        Gizmos.DrawLine(a + normal * halfWidth, b + normal * halfWidth);
                        Gizmos.DrawLine(a - normal * halfWidth, b - normal * halfWidth);

                        float length = Vector3.Distance(a, b);
                        int samples = Mathf.Max(1, Mathf.CeilToInt(length / _widthSampleStep));
                        for (int s = 0; s <= samples; s++)
                        {
                            float t = s / (float)samples;
                            Vector3 center = Vector3.Lerp(a, b, t);
                            Gizmos.DrawWireSphere(center, halfWidth);
                        }
                    }
                }

                if (_drawNodeLabels)
                {
                    Vector3 at = edge.polyline[0] + Vector3.up * (_lineYOffset + 1f);
                    DrawLabel(at, $"{edge.type} w={edge.width:F1}");
                }
            }
        }

        private void DrawIntersections(ConnectivityGraph graph)
        {
            Dictionary<string, int> degree = new Dictionary<string, int>();
            if (graph.edges != null)
            {
                for (int i = 0; i < graph.edges.Count; i++)
                {
                    GraphEdge edge = graph.edges[i];
                    if (edge == null)
                        continue;
                    IncrementDegree(degree, edge.fromNodeId);
                    IncrementDegree(degree, edge.toNodeId);
                }
            }

            if (graph.nodes == null)
                return;

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                GraphNode node = graph.nodes[i];
                if (node == null || string.IsNullOrEmpty(node.id))
                    continue;

                int d = degree.TryGetValue(node.id, out int value) ? value : 0;
                bool intersection = d >= 3 || node.type == GraphNodeType.Junction;
                if (!intersection)
                    continue;

                Gizmos.color = _intersectionColor;
                Vector3 p = node.position + Vector3.up * (_lineYOffset + 0.1f);
                Gizmos.DrawSphere(p, _intersectionRadius);

                if (_drawNodeLabels)
                    DrawLabel(p + Vector3.up * (_intersectionRadius + 0.3f), $"deg={d}");
            }
        }

        private static void IncrementDegree(Dictionary<string, int> degree, string id)
        {
            if (string.IsNullOrEmpty(id))
                return;
            if (!degree.ContainsKey(id))
                degree[id] = 1;
            else
                degree[id]++;
        }

        private static void DrawLabel(Vector3 position, string text)
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(position, text);
#endif
        }
    }
}
