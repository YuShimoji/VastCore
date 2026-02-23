using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.WorldGen.GraphEngine
{
    /// <summary>
    /// Graph 可視化向けの集計値。
    /// </summary>
    public sealed class GraphStatistics
    {
        public int nodeCount;
        public int edgeCount;
        public int roadEdgeCount;
        public int riverEdgeCount;
        public int intersectionCount;
        public float totalRoadLength;
        public float totalRiverLength;
        public float averageRoadWidth;
        public float averageRiverWidth;
    }

    /// <summary>
    /// Graph 統計計算ユーティリティ。
    /// </summary>
    public static class GraphStatisticsUtility
    {
        /// <summary>
        /// グラフ統計を計算する。
        /// </summary>
        public static GraphStatistics Compute(ConnectivityGraph graph)
        {
            GraphStatistics stats = new GraphStatistics();
            if (graph == null)
                return stats;

            stats.nodeCount = graph.nodes != null ? graph.nodes.Count : 0;
            stats.edgeCount = graph.edges != null ? graph.edges.Count : 0;

            Dictionary<string, int> degree = new Dictionary<string, int>();
            float roadWidthSum = 0f;
            float riverWidthSum = 0f;

            if (graph.edges != null)
            {
                for (int i = 0; i < graph.edges.Count; i++)
                {
                    GraphEdge edge = graph.edges[i];
                    if (edge == null)
                        continue;

                    IncrementDegree(degree, edge.fromNodeId);
                    IncrementDegree(degree, edge.toNodeId);

                    float length = ComputePolylineLength(edge.polyline);
                    if (edge.type == GraphEdgeType.Road)
                    {
                        stats.roadEdgeCount++;
                        stats.totalRoadLength += length;
                        roadWidthSum += edge.width;
                    }
                    else if (edge.type == GraphEdgeType.River)
                    {
                        stats.riverEdgeCount++;
                        stats.totalRiverLength += length;
                        riverWidthSum += edge.width;
                    }
                }
            }

            if (stats.roadEdgeCount > 0)
                stats.averageRoadWidth = roadWidthSum / stats.roadEdgeCount;
            if (stats.riverEdgeCount > 0)
                stats.averageRiverWidth = riverWidthSum / stats.riverEdgeCount;

            if (graph.nodes != null)
            {
                for (int i = 0; i < graph.nodes.Count; i++)
                {
                    GraphNode node = graph.nodes[i];
                    if (node == null || string.IsNullOrEmpty(node.id))
                        continue;

                    int d = degree.TryGetValue(node.id, out int value) ? value : 0;
                    if (d >= 3 || node.type == GraphNodeType.Junction)
                        stats.intersectionCount++;
                }
            }

            return stats;
        }

        private static float ComputePolylineLength(List<Vector3> polyline)
        {
            if (polyline == null || polyline.Count < 2)
                return 0f;

            float length = 0f;
            for (int i = 0; i < polyline.Count - 1; i++)
                length += Vector3.Distance(polyline[i], polyline[i + 1]);
            return length;
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
    }
}
