using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.WorldGen.GraphEngine
{
    /// <summary>
    /// グラフノード種別。
    /// </summary>
    public enum GraphNodeType
    {
        Generic,
        Junction,
        Source,
        Sink
    }

    /// <summary>
    /// グラフエッジ種別。
    /// </summary>
    public enum GraphEdgeType
    {
        Road,
        River
    }

    /// <summary>
    /// ネットワークノード。
    /// </summary>
    [Serializable]
    public sealed class GraphNode
    {
        public string id = Guid.NewGuid().ToString("N");
        public Vector3 position;
        public GraphNodeType type;
    }

    /// <summary>
    /// ネットワークエッジ。
    /// </summary>
    [Serializable]
    public sealed class GraphEdge
    {
        public string fromNodeId;
        public string toNodeId;
        public GraphEdgeType type = GraphEdgeType.Road;
        public float width = 4f;
        public List<Vector3> polyline = new List<Vector3>();
    }

    /// <summary>
    /// 連結グラフデータ。
    /// </summary>
    [Serializable]
    public sealed class ConnectivityGraph
    {
        public List<GraphNode> nodes = new List<GraphNode>();
        public List<GraphEdge> edges = new List<GraphEdge>();
    }
}
