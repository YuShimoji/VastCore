using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.WorldGen.GraphEngine
{
    /// <summary>
    /// 手入力・外部生成グラフを保持するアセット。
    /// </summary>
    [CreateAssetMenu(fileName = "GraphAsset", menuName = "Vastcore/WorldGen/Graph Asset")]
    public sealed class GraphAsset : ScriptableObject
    {
        public List<GraphNode> nodes = new List<GraphNode>();
        public List<GraphEdge> edges = new List<GraphEdge>();

        /// <summary>
        /// 実行時に破壊されないようディープコピーを返す。
        /// </summary>
        public ConnectivityGraph CreateRuntimeCopy()
        {
            ConnectivityGraph graph = new ConnectivityGraph();
            if (nodes != null)
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    GraphNode n = nodes[i];
                    if (n == null)
                        continue;
                    graph.nodes.Add(new GraphNode
                    {
                        id = n.id,
                        position = n.position,
                        type = n.type
                    });
                }
            }

            if (edges != null)
            {
                for (int i = 0; i < edges.Count; i++)
                {
                    GraphEdge e = edges[i];
                    if (e == null)
                        continue;
                    GraphEdge copy = new GraphEdge
                    {
                        fromNodeId = e.fromNodeId,
                        toNodeId = e.toNodeId,
                        type = e.type,
                        width = e.width
                    };
                    if (e.polyline != null)
                        copy.polyline.AddRange(e.polyline);
                    graph.edges.Add(copy);
                }
            }

            return graph;
        }
    }
}
