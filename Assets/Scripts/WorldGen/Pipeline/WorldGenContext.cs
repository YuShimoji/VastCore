using Vastcore.WorldGen.FieldEngine;
using Vastcore.WorldGen.GraphEngine;
using Vastcore.WorldGen.GrammarEngine;
using Vastcore.WorldGen.Observability;
using Vastcore.WorldGen.Recipe;
using UnityEngine;

namespace Vastcore.WorldGen.Pipeline
{
    /// <summary>
    /// WorldGen 実行結果コンテキスト。
    /// </summary>
    public sealed class WorldGenContext
    {
        /// <summary>入力 Recipe。</summary>
        public WorldGenRecipe Recipe { get; set; }

        /// <summary>構築済み密度場。</summary>
        public IDensityField DensityField { get; set; }

        /// <summary>統計情報。</summary>
        public WorldGenStats Stats { get; } = new WorldGenStats();

        /// <summary>生成済みグラフ。</summary>
        public ConnectivityGraph GraphData { get; set; }

        /// <summary>生成済み構造設計図。</summary>
        public System.Collections.Generic.List<StructureBlueprint> GrammarData { get; set; }

        /// <summary>Graph 更新により影響を受ける領域。</summary>
        public System.Collections.Generic.List<Bounds> GraphAffectedBounds { get; set; }
    }
}
