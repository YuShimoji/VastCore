using System.Diagnostics;
using Vastcore.WorldGen.DeformationEngine;
using Vastcore.WorldGen.FieldEngine;
using Vastcore.WorldGen.GraphEngine;
using Vastcore.WorldGen.GrammarEngine;
using Vastcore.WorldGen.Recipe;

namespace Vastcore.WorldGen.Pipeline
{
    /// <summary>
    /// 4 エンジン合成のパイプライン入口。
    /// M1 時点では Field Engine のみ実処理を持つ。
    /// </summary>
    public sealed class WorldGenPipeline
    {
        private readonly IFieldEngine _fieldEngine;
        private readonly IGraphEngine _graphEngine;
        private readonly IGrammarEngine _grammarEngine;
        private readonly IDeformationEngine _deformationEngine;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public WorldGenPipeline(
            IFieldEngine fieldEngine,
            IGraphEngine graphEngine = null,
            IGrammarEngine grammarEngine = null,
            IDeformationEngine deformationEngine = null)
        {
            _fieldEngine = fieldEngine;
            _graphEngine = graphEngine ?? new GraphEngineManager();
            _grammarEngine = grammarEngine ?? new GrammarEngineStub();
            _deformationEngine = deformationEngine ?? new DeformationEngineStub();
        }

        /// <summary>
        /// Recipe を実行してコンテキストを返す。
        /// </summary>
        public WorldGenContext Execute(WorldGenRecipe recipe)
        {
            WorldGenContext context = new WorldGenContext
            {
                Recipe = recipe
            };

            if (_fieldEngine == null || recipe == null)
                return context;

            Stopwatch sw = Stopwatch.StartNew();
            context.DensityField = _fieldEngine.BuildField(recipe);
            sw.Stop();
            context.Stats.RecordFieldBuild((float)sw.Elapsed.TotalMilliseconds);

            // M2: Graph Engine
            ConnectivityGraph graph = _graphEngine.GenerateGraph(recipe, context);
            context.GraphData = graph;
            if (_graphEngine is GraphEngineManager manager)
                context.GraphAffectedBounds = new System.Collections.Generic.List<UnityEngine.Bounds>(manager.LastAffectedBounds);
            context.DensityField = _graphEngine.BurnIntoField(context.DensityField, graph, recipe);

            // M4: Grammar slot
            context.GrammarData = _grammarEngine.GenerateStructures(recipe, context);

            // M5: Deformation slot (physical only in context stage)
            _deformationEngine.ApplyPhysical(recipe, context, context.DensityField);

            return context;
        }
    }
}
