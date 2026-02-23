using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Vastcore.WorldGen.GraphEngine;
using Vastcore.WorldGen.Recipe;

namespace Vastcore.Editor.WorldGen
{
    /// <summary>
    /// Graph オーバーレイ表示用の EditorWindow。
    /// SceneView 上にポリライン・幅・交差点を描画し、凡例と統計を表示する。
    /// </summary>
    public sealed class WorldGenGraphOverlayWindow : EditorWindow
    {
        private WorldGenRecipe _recipe;
        private GraphAsset _graphAssetOverride;

        private bool _overlayEnabled = true;
        private bool _autoRefresh = true;
        private bool _drawPolyline = true;
        private bool _drawWidth = true;
        private bool _drawIntersections = true;
        private bool _drawNodeLabels = false;

        private Color _roadColor = new Color(0.95f, 0.75f, 0.2f, 1f);
        private Color _riverColor = new Color(0.2f, 0.7f, 1f, 1f);
        private Color _intersectionColor = new Color(1f, 0.2f, 0.2f, 1f);
        private float _lineYOffset = 0.2f;
        private float _intersectionRadius = 2f;
        private float _widthSampleStep = 8f;

        private bool _showLegend = true;
        private bool _showStats = true;
        private Vector2 _scrollPosition;

        private ConnectivityGraph _cachedGraph;
        private GraphStatistics _cachedStats;
        private int _cachedHash;

        [MenuItem("Tools/Vastcore/WorldGen/Graph Overlay")]
        public static void Open()
        {
            WorldGenGraphOverlayWindow window = GetWindow<WorldGenGraphOverlayWindow>("Graph Overlay");
            window.minSize = new Vector2(360f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EnsurePreviewGraph(true);
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("WorldGen Graph Overlay", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("SceneView に Graph を重ね描画します。Recipe 変更時は Refresh または Auto Refresh を使用してください。", MessageType.Info);

            EditorGUILayout.Space(4f);
            DrawSourceSection();
            EditorGUILayout.Space(6f);
            DrawToggleSection();
            EditorGUILayout.Space(6f);
            DrawStyleSection();
            EditorGUILayout.Space(6f);
            DrawLegendSection();
            EditorGUILayout.Space(6f);
            DrawStatsSection();

            EditorGUILayout.EndScrollView();

            if (_autoRefresh && Event.current.type == EventType.Layout)
            {
                EnsurePreviewGraph(false);
            }
        }

        private void DrawSourceSection()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _recipe = (WorldGenRecipe)EditorGUILayout.ObjectField("Recipe", _recipe, typeof(WorldGenRecipe), false);
            _graphAssetOverride = (GraphAsset)EditorGUILayout.ObjectField("GraphAsset Override", _graphAssetOverride, typeof(GraphAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                EnsurePreviewGraph(true);
            }

            EditorGUILayout.BeginHorizontal();
            _autoRefresh = EditorGUILayout.ToggleLeft("Auto Refresh", _autoRefresh, GUILayout.Width(110f));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", GUILayout.Width(90f)))
            {
                EnsurePreviewGraph(true);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToggleSection()
        {
            EditorGUILayout.LabelField("Draw Toggles", EditorStyles.boldLabel);
            _overlayEnabled = EditorGUILayout.Toggle("Enable Overlay", _overlayEnabled);
            _drawPolyline = EditorGUILayout.Toggle("Polyline", _drawPolyline);
            _drawWidth = EditorGUILayout.Toggle("Width", _drawWidth);
            _drawIntersections = EditorGUILayout.Toggle("Intersections", _drawIntersections);
            _drawNodeLabels = EditorGUILayout.Toggle("Node Labels", _drawNodeLabels);
        }

        private void DrawStyleSection()
        {
            EditorGUILayout.LabelField("Style", EditorStyles.boldLabel);
            _roadColor = EditorGUILayout.ColorField("Road Color", _roadColor);
            _riverColor = EditorGUILayout.ColorField("River Color", _riverColor);
            _intersectionColor = EditorGUILayout.ColorField("Intersection Color", _intersectionColor);
            _lineYOffset = EditorGUILayout.Slider("Line Y Offset", _lineYOffset, 0f, 5f);
            _intersectionRadius = EditorGUILayout.Slider("Intersection Radius", _intersectionRadius, 0.1f, 20f);
            _widthSampleStep = EditorGUILayout.Slider("Width Sample Step", _widthSampleStep, 1f, 30f);
        }

        private void DrawLegendSection()
        {
            _showLegend = EditorGUILayout.Foldout(_showLegend, "Legend", true);
            if (!_showLegend)
                return;

            DrawLegendRow(_roadColor, "Road");
            DrawLegendRow(_riverColor, "River");
            DrawLegendRow(_intersectionColor, "Intersection");
        }

        private void DrawLegendRow(Color color, string label)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            Rect swatchRect = new Rect(rect.x, rect.y + 2f, 16f, rect.height - 4f);
            Rect labelRect = new Rect(rect.x + 24f, rect.y, rect.width - 24f, rect.height);
            EditorGUI.DrawRect(swatchRect, color);
            EditorGUI.LabelField(labelRect, label);
        }

        private void DrawStatsSection()
        {
            _showStats = EditorGUILayout.Foldout(_showStats, "Statistics", true);
            if (!_showStats)
                return;

            if (_cachedGraph == null || _cachedStats == null)
            {
                EditorGUILayout.HelpBox("No graph preview available.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField($"Nodes: {_cachedStats.nodeCount}");
            EditorGUILayout.LabelField($"Edges: {_cachedStats.edgeCount}");
            EditorGUILayout.LabelField($"Road Edges: {_cachedStats.roadEdgeCount}");
            EditorGUILayout.LabelField($"River Edges: {_cachedStats.riverEdgeCount}");
            EditorGUILayout.LabelField($"Intersections: {_cachedStats.intersectionCount}");
            EditorGUILayout.LabelField($"Road Length: {_cachedStats.totalRoadLength:F1}");
            EditorGUILayout.LabelField($"River Length: {_cachedStats.totalRiverLength:F1}");
            EditorGUILayout.LabelField($"Avg Road Width: {_cachedStats.averageRoadWidth:F2}");
            EditorGUILayout.LabelField($"Avg River Width: {_cachedStats.averageRiverWidth:F2}");
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_overlayEnabled)
                return;

            EnsurePreviewGraph(false);
            if (_cachedGraph == null || _cachedGraph.edges == null || _cachedGraph.edges.Count == 0)
                return;

            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            DrawEdges(_cachedGraph);
            DrawIntersections(_cachedGraph);
        }

        private void DrawEdges(ConnectivityGraph graph)
        {
            if (!_drawPolyline && !_drawWidth)
                return;

            for (int i = 0; i < graph.edges.Count; i++)
            {
                GraphEdge edge = graph.edges[i];
                if (edge == null || edge.polyline == null || edge.polyline.Count < 2)
                    continue;

                Color color = edge.type == GraphEdgeType.River ? _riverColor : _roadColor;
                float halfWidth = Mathf.Max(0.1f, edge.width * 0.5f);

                for (int p = 0; p < edge.polyline.Count - 1; p++)
                {
                    Vector3 a = edge.polyline[p] + Vector3.up * _lineYOffset;
                    Vector3 b = edge.polyline[p + 1] + Vector3.up * _lineYOffset;

                    Vector3 dir = b - a;
                    dir.y = 0f;
                    if (dir.sqrMagnitude < 1e-6f)
                        continue;
                    dir.Normalize();
                    Vector3 normal = new Vector3(-dir.z, 0f, dir.x);

                    if (_drawPolyline)
                    {
                        Handles.color = color;
                        Handles.DrawAAPolyLine(2f, a, b);
                    }

                    if (_drawWidth)
                    {
                        Handles.color = new Color(color.r, color.g, color.b, 0.85f);
                        Handles.DrawAAPolyLine(1.4f, a + normal * halfWidth, b + normal * halfWidth);
                        Handles.DrawAAPolyLine(1.4f, a - normal * halfWidth, b - normal * halfWidth);

                        float len = Vector3.Distance(a, b);
                        int samples = Mathf.Max(1, Mathf.CeilToInt(len / Mathf.Max(0.1f, _widthSampleStep)));
                        for (int s = 0; s <= samples; s++)
                        {
                            float t = s / (float)samples;
                            Vector3 center = Vector3.Lerp(a, b, t);
                            Handles.DrawWireDisc(center, Vector3.up, halfWidth);
                        }
                    }
                }

                if (_drawNodeLabels)
                {
                    Vector3 labelPos = edge.polyline[0] + Vector3.up * (_lineYOffset + 0.6f);
                    Handles.Label(labelPos, $"{edge.type} w={edge.width:F1}");
                }
            }
        }

        private void DrawIntersections(ConnectivityGraph graph)
        {
            if (!_drawIntersections || graph.nodes == null)
                return;

            Dictionary<string, int> degree = BuildDegreeMap(graph);

            for (int i = 0; i < graph.nodes.Count; i++)
            {
                GraphNode node = graph.nodes[i];
                if (node == null || string.IsNullOrEmpty(node.id))
                    continue;

                int d = degree.TryGetValue(node.id, out int value) ? value : 0;
                bool isIntersection = d >= 3 || node.type == GraphNodeType.Junction;
                if (!isIntersection)
                    continue;

                Vector3 pos = node.position + Vector3.up * (_lineYOffset + 0.1f);
                Handles.color = _intersectionColor;
                Handles.SphereHandleCap(0, pos, Quaternion.identity, _intersectionRadius * 2f, EventType.Repaint);

                if (_drawNodeLabels)
                    Handles.Label(pos + Vector3.up * (_intersectionRadius + 0.3f), $"deg={d}");
            }
        }

        private static Dictionary<string, int> BuildDegreeMap(ConnectivityGraph graph)
        {
            Dictionary<string, int> degree = new Dictionary<string, int>();
            if (graph == null || graph.edges == null)
                return degree;

            for (int i = 0; i < graph.edges.Count; i++)
            {
                GraphEdge edge = graph.edges[i];
                if (edge == null)
                    continue;
                IncrementDegree(degree, edge.fromNodeId);
                IncrementDegree(degree, edge.toNodeId);
            }
            return degree;
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

        private void EnsurePreviewGraph(bool force)
        {
            int sourceHash = ComputeSourceHash();
            if (!force && !_autoRefresh && _cachedGraph != null)
                return;
            if (!force && _cachedHash == sourceHash && _cachedGraph != null)
                return;

            _cachedGraph = GraphPreviewUtility.BuildPreviewGraph(_recipe, _graphAssetOverride);
            _cachedStats = GraphStatisticsUtility.Compute(_cachedGraph);
            _cachedHash = sourceHash;

            SceneView.RepaintAll();
            Repaint();
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
    }
}
