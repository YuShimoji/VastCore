using System.Collections.Generic;
using UnityEngine;
using Vastcore.WorldGen.Common;
using Vastcore.WorldGen.Recipe;

namespace Vastcore.WorldGen.GraphEngine
{
    /// <summary>
    /// 決定論的な河川グラフを生成する。
    /// </summary>
    public sealed class RiverGraphGenerator
    {
        /// <summary>
        /// 河川エッジを graph に追加する。
        /// </summary>
        public void AppendRivers(ConnectivityGraph graph, GraphGenerationSettings settings, int seed)
        {
            if (graph == null || settings == null || settings.riverCount <= 0)
                return;

            DeterministicRng rng = new DeterministicRng(seed);
            Bounds domain = new Bounds(settings.domainCenter, settings.domainSize);
            float halfX = domain.extents.x;
            float halfZ = domain.extents.z;

            for (int i = 0; i < settings.riverCount; i++)
            {
                bool flowX = (i % 2) == 0;
                float startY = settings.baseHeight - rng.NextFloat(1f, 6f);
                float endY = startY - rng.NextFloat(1f, 4f);

                Vector3 start;
                Vector3 end;
                if (flowX)
                {
                    float z = domain.center.z + rng.NextFloat(-halfZ * 0.8f, halfZ * 0.8f);
                    start = new Vector3(domain.center.x - halfX, startY, z);
                    end = new Vector3(domain.center.x + halfX, endY, z + rng.NextFloat(-halfZ * 0.2f, halfZ * 0.2f));
                }
                else
                {
                    float x = domain.center.x + rng.NextFloat(-halfX * 0.8f, halfX * 0.8f);
                    start = new Vector3(x, startY, domain.center.z - halfZ);
                    end = new Vector3(x + rng.NextFloat(-halfX * 0.2f, halfX * 0.2f), endY, domain.center.z + halfZ);
                }

                start = ClampToDomainXZ(start, domain);
                end = ClampToDomainXZ(end, domain);

                string fromId = AddNode(graph, start, GraphNodeType.Source);
                string toId = AddNode(graph, end, GraphNodeType.Sink);
                float width = rng.NextFloat(settings.riverWidthMin, settings.riverWidthMax);
                List<Vector3> polyline = CreateMeanderingLine(start, end, rng, domain);
                AddEdge(graph, fromId, toId, GraphEdgeType.River, width, polyline);
            }
        }

        private static List<Vector3> CreateMeanderingLine(
            Vector3 start,
            Vector3 end,
            DeterministicRng rng,
            Bounds domain)
        {
            List<Vector3> line = new List<Vector3>(8) { start };
            int segments = 6;
            Vector3 direction = (end - start).normalized;
            Vector3 normal = new Vector3(-direction.z, 0f, direction.x);
            float span = Vector3.Distance(start, end);
            float amplitude = span * 0.08f;

            for (int i = 1; i < segments; i++)
            {
                float t = i / (float)segments;
                Vector3 p = Vector3.Lerp(start, end, t);
                float phase = t * Mathf.PI * 2f + rng.NextFloat(-0.5f, 0.5f);
                p += normal * (Mathf.Sin(phase) * amplitude);
                p = ClampToDomainXZ(p, domain);
                line.Add(p);
            }

            line.Add(end);
            return line;
        }

        private static Vector3 ClampToDomainXZ(Vector3 p, Bounds domain)
        {
            p.x = Mathf.Clamp(p.x, domain.min.x, domain.max.x);
            p.z = Mathf.Clamp(p.z, domain.min.z, domain.max.z);
            return p;
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
    }
}
