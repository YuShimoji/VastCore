using System;
using System.Collections.Generic;
using UnityEngine;
using Vastcore.Utilities;
using Vastcore.WorldGen.FieldEngine;
using Vastcore.WorldGen.Pipeline;
using Vastcore.WorldGen.Recipe;

namespace Vastcore.WorldGen.GraphEngine
{
    /// <summary>
    /// M2 の Graph Engine 実装。
    /// 道路・河川ネットワークの生成と密度場への焼き付けを担当する。
    /// </summary>
    public sealed class GraphEngineManager : IGraphEngine
    {
        private readonly RoadGraphGenerator _roadGenerator = new RoadGraphGenerator();
        private readonly RiverGraphGenerator _riverGenerator = new RiverGraphGenerator();
        private readonly GraphFieldBurner _fieldBurner = new GraphFieldBurner();

        /// <summary>
        /// 外部 Graph 自動生成器。
        /// </summary>
        public IGraphAutoGeneratorAdapter AutoGeneratorAdapter { get; set; }

        /// <summary>
        /// 直近生成グラフ。
        /// </summary>
        public ConnectivityGraph LastGeneratedGraph { get; private set; }

        /// <summary>
        /// 直近生成で影響した AABB 一覧。
        /// </summary>
        public List<Bounds> LastAffectedBounds { get; } = new List<Bounds>();

        /// <summary>
        /// Graph 更新イベント。
        /// </summary>
        public event Action<GraphUpdateInfo> GraphUpdated;

        /// <inheritdoc />
        public ConnectivityGraph GenerateGraph(WorldGenRecipe recipe, WorldGenContext context)
        {
            ConnectivityGraph graph = new ConnectivityGraph();
            if (recipe == null)
                return graph;

            GraphGenerationSettings settings = recipe.graphSettings;
            if (settings == null || !settings.enableGraph)
                return graph;

            if (settings.useGraphAssetWhenAvailable && recipe.graphAsset != null)
            {
                graph = recipe.graphAsset.CreateRuntimeCopy();
            }
            else
            {
                bool handledByAdapter = false;
                if (AutoGeneratorAdapter != null)
                {
                    handledByAdapter = AutoGeneratorAdapter.TryGenerate(recipe, settings, recipe.seed, out ConnectivityGraph adapterGraph);
                    if (handledByAdapter && adapterGraph != null)
                        graph = adapterGraph;
                }

                if (!handledByAdapter)
                {
                    if (settings.generateRoads)
                        _roadGenerator.AppendRoads(graph, settings, recipe.seed + settings.roadSeedOffset);
                    if (settings.generateRivers)
                        _riverGenerator.AppendRivers(graph, settings, recipe.seed + settings.riverSeedOffset);
                }
            }

            LastGeneratedGraph = graph;
            UpdateAffectedBounds(graph, settings);
            NotifyGraphUpdated(graph);

            VastcoreLogger.Instance.LogInfo(
                "WorldGen.Graph",
                $"Graph generated: nodes={graph.nodes.Count}, edges={graph.edges.Count}");

            return graph;
        }

        /// <inheritdoc />
        public IDensityField BurnIntoField(IDensityField baseField, ConnectivityGraph graph, WorldGenRecipe recipe)
        {
            if (baseField == null || recipe == null || graph == null || graph.edges == null || graph.edges.Count == 0)
                return baseField;

            GraphGenerationSettings settings = recipe.graphSettings;
            if (settings == null || !settings.enableGraph)
                return baseField;

            return _fieldBurner.Burn(baseField, graph, settings);
        }

        private void UpdateAffectedBounds(ConnectivityGraph graph, GraphGenerationSettings settings)
        {
            LastAffectedBounds.Clear();
            if (graph == null || graph.edges == null || graph.edges.Count == 0)
                return;

            float verticalPadding = 1f;
            if (settings != null)
                verticalPadding = Mathf.Max(1f, settings.riverDepth + settings.riverBankHeight + 2f);

            for (int i = 0; i < graph.edges.Count; i++)
            {
                GraphEdge edge = graph.edges[i];
                if (edge == null || edge.polyline == null || edge.polyline.Count == 0)
                    continue;

                Vector3 min = edge.polyline[0];
                Vector3 max = edge.polyline[0];
                for (int p = 1; p < edge.polyline.Count; p++)
                {
                    Vector3 point = edge.polyline[p];
                    min = Vector3.Min(min, point);
                    max = Vector3.Max(max, point);
                }

                float expandXZ = Mathf.Max(0.5f, edge.width * 1.2f);
                min.x -= expandXZ;
                min.z -= expandXZ;
                max.x += expandXZ;
                max.z += expandXZ;
                min.y -= verticalPadding;
                max.y += verticalPadding;

                Bounds b = new Bounds((min + max) * 0.5f, max - min);
                LastAffectedBounds.Add(b);
            }
        }

        private void NotifyGraphUpdated(ConnectivityGraph graph)
        {
            if (GraphUpdated == null)
                return;

            GraphUpdateInfo info = new GraphUpdateInfo
            {
                graph = graph,
                affectedBounds = new List<Bounds>(LastAffectedBounds)
            };
            GraphUpdated.Invoke(info);
        }
    }
}
