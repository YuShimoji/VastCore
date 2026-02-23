using System.Collections.Generic;
using UnityEngine;
using Vastcore.WorldGen.Common;
using Vastcore.WorldGen.Recipe;

namespace Vastcore.WorldGen.GraphEngine
{
    /// <summary>
    /// 決定論的な道路グラフを生成する。
    /// </summary>
    public sealed class RoadGraphGenerator
    {
        /// <summary>
        /// 道路エッジを graph に追加する。
        /// </summary>
        public void AppendRoads(ConnectivityGraph graph, GraphGenerationSettings settings, int seed)
        {
            if (graph == null || settings == null || settings.roadSpineCount <= 0)
                return;

            DeterministicRng rng = new DeterministicRng(seed);
            Bounds domain = new Bounds(settings.domainCenter, settings.domainSize);
            float halfX = domain.extents.x;
            float halfZ = domain.extents.z;
            float y = settings.baseHeight;

            List<string> mainNodeIds = new List<string>();

            for (int i = 0; i < settings.roadSpineCount; i++)
            {
                bool horizontal = (i % 2) == 0;
                Vector3 start;
                Vector3 end;

                if (horizontal)
                {
                    float z = domain.center.z + rng.NextFloat(-halfZ * 0.75f, halfZ * 0.75f);
                    start = new Vector3(domain.center.x - halfX, y, z);
                    end = new Vector3(domain.center.x + halfX, y, z);
                }
                else
                {
                    float x = domain.center.x + rng.NextFloat(-halfX * 0.75f, halfX * 0.75f);
                    start = new Vector3(x, y, domain.center.z - halfZ);
                    end = new Vector3(x, y, domain.center.z + halfZ);
                }

                string fromId = AddNode(graph, start, GraphNodeType.Junction);
                string toId = AddNode(graph, end, GraphNodeType.Junction);
                mainNodeIds.Add(fromId);
                mainNodeIds.Add(toId);

                List<Vector3> polyline = CreatePolyline(start, end, settings.roadJitter, rng, domain);
                float width = rng.NextFloat(settings.roadWidthMin, settings.roadWidthMax);
                AddEdge(graph, fromId, toId, GraphEdgeType.Road, width, polyline);
            }

            int branchCount = Mathf.Max(0, settings.roadBranchCount);
            for (int i = 0; i < branchCount && mainNodeIds.Count > 0; i++)
            {
                string originNodeId = mainNodeIds[rng.NextInt(0, mainNodeIds.Count)];
                GraphNode origin = FindNodeById(graph, originNodeId);
                if (origin == null)
                    continue;

                Vector3 dir = rng.NextOnUnitSphere();
                dir.y = 0f;
                if (dir.sqrMagnitude < 1e-4f)
                    dir = Vector3.right;
                dir.Normalize();

                float length = rng.NextFloat(domain.extents.magnitude * 0.25f, domain.extents.magnitude * 0.6f);
                Vector3 end = origin.position + dir * length;
                end = ClampToDomainXZ(end, domain, settings.baseHeight);

                string endId = AddNode(graph, end, GraphNodeType.Junction);
                List<Vector3> polyline = CreatePolyline(origin.position, end, settings.roadJitter * 0.6f, rng, domain);
                float width = rng.NextFloat(settings.roadWidthMin, settings.roadWidthMax);
                AddEdge(graph, originNodeId, endId, GraphEdgeType.Road, width, polyline);
            }
        }

        private static List<Vector3> CreatePolyline(
            Vector3 start,
            Vector3 end,
            float jitter,
            DeterministicRng rng,
            Bounds domain)
        {
            List<Vector3> line = new List<Vector3>(6) { start };
            int segments = 4;
            Vector3 tangent = (end - start).normalized;
            Vector3 normal = new Vector3(-tangent.z, 0f, tangent.x);

            for (int i = 1; i < segments; i++)
            {
                float t = i / (float)segments;
                Vector3 p = Vector3.Lerp(start, end, t);
                float lateral = rng.NextFloat(-jitter, jitter);
                p += normal * lateral;
                p = ClampToDomainXZ(p, domain, start.y);
                line.Add(p);
            }

            line.Add(end);
            return line;
        }

        private static Vector3 ClampToDomainXZ(Vector3 p, Bounds domain, float y)
        {
            return new Vector3(
                Mathf.Clamp(p.x, domain.min.x, domain.max.x),
                y,
                Mathf.Clamp(p.z, domain.min.z, domain.max.z));
        }

        private static string AddNode(ConnectivityGraph graph, Vector3 position, GraphNodeType type)
        {
            GraphNode node = new GraphNode
            {
                position = position,
                type = type
            };
            graph.nodes.Add(node);
            return node.id;
        }

        private static void AddEdge(
            ConnectivityGraph graph,
            string fromNodeId,
            string toNodeId,
            GraphEdgeType type,
            float width,
            List<Vector3> polyline)
        {
            GraphEdge edge = new GraphEdge
            {
                fromNodeId = fromNodeId,
                toNodeId = toNodeId,
                type = type,
                width = Mathf.Max(0.1f, width)
            };
            if (polyline != null)
                edge.polyline.AddRange(polyline);
            graph.edges.Add(edge);
        }

        private static GraphNode FindNodeById(ConnectivityGraph graph, string nodeId)
        {
            if (graph == null || graph.nodes == null)
                return null;
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                GraphNode n = graph.nodes[i];
                if (n != null && n.id == nodeId)
                    return n;
            }
            return null;
        }
    }
}
