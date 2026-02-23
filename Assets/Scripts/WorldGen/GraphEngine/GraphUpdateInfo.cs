using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.WorldGen.GraphEngine
{
    /// <summary>
    /// Graph 更新時の通知データ。
    /// </summary>
    public sealed class GraphUpdateInfo
    {
        /// <summary>生成されたグラフ。</summary>
        public ConnectivityGraph graph;

        /// <summary>更新影響領域 (AABB)。</summary>
        public List<Bounds> affectedBounds = new List<Bounds>();
    }
}
