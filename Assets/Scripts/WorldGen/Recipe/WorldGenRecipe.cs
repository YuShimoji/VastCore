using System.Collections.Generic;
using UnityEngine;
using Vastcore.WorldGen.GraphEngine;
using Vastcore.WorldGen.Stamps;

namespace Vastcore.WorldGen.Recipe
{
    /// <summary>
    /// ワールド生成レシピ (SSOT)。
    /// </summary>
    [CreateAssetMenu(fileName = "WorldGenRecipe", menuName = "Vastcore/WorldGen/Recipe")]
    public sealed class WorldGenRecipe : ScriptableObject
    {
        [Header("Global")]
        public int seed = 42;
        [Min(0.01f)] public float worldScale = 1f;

        [Header("Chunk")]
        [Range(8, 128)] public int chunkResolution = 32;
        [Min(1f)] public float chunkWorldSize = 64f;
        [Range(1, 16)] public int chunkVerticalCount = 4;

        [Header("Field Layers")]
        public List<FieldLayer> layers = new List<FieldLayer>();

        [Header("Stamp Instances")]
        public List<StampInstanceData> stamps = new List<StampInstanceData>();

        [Header("Graph")]
        [Tooltip("Optional manual graph input. Used when graphSettings.useGraphAssetWhenAvailable is true.")]
        public GraphAsset graphAsset;
        public GraphGenerationSettings graphSettings = new GraphGenerationSettings();

        /// <summary>
        /// 決定論確認向けの簡易ハッシュを計算する。
        /// </summary>
        public int ComputeRecipeHash()
        {
            unchecked
            {
                int hash = seed;
                hash = (hash * 397) ^ worldScale.GetHashCode();
                hash = (hash * 397) ^ chunkResolution;
                hash = (hash * 397) ^ chunkWorldSize.GetHashCode();
                hash = (hash * 397) ^ chunkVerticalCount;
                hash = (hash * 397) ^ (layers != null ? layers.Count : 0);
                hash = (hash * 397) ^ (stamps != null ? stamps.Count : 0);
                hash = (hash * 397) ^ (graphAsset != null ? graphAsset.name.GetHashCode() : 0);
                hash = (hash * 397) ^ (graphSettings != null && graphSettings.enableGraph ? 1 : 0);
                return hash;
            }
        }
    }

    /// <summary>
    /// スタンプのワールド配置データ。
    /// </summary>
    [System.Serializable]
    public sealed class StampInstanceData
    {
        public StampBase stamp;
        public Vector3 position = Vector3.zero;
        public Quaternion rotation = Quaternion.identity;
        public Vector3 scale = Vector3.one;
        public BooleanOp booleanOp = BooleanOp.Union;
        [Range(0f, 10f)] public float smoothK;
        [Range(0f, 4f)] public float weight = 1f;
    }
}
