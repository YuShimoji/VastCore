using UnityEngine;

namespace Vastcore.WorldGen.Recipe
{
    /// <summary>
    /// Graph Engine の生成・焼き付け設定。
    /// </summary>
    [System.Serializable]
    public sealed class GraphGenerationSettings
    {
        [Header("Enable")]
        public bool enableGraph = true;
        public bool generateRoads = true;
        public bool generateRivers = true;
        public bool useGraphAssetWhenAvailable = true;

        [Header("Domain")]
        public Vector3 domainCenter = Vector3.zero;
        public Vector3 domainSize = new Vector3(512f, 128f, 512f);
        [Min(0f)] public float baseHeight = 0f;

        [Header("Road Auto Generation")]
        [Range(0, 8)] public int roadSpineCount = 2;
        [Range(0, 16)] public int roadBranchCount = 4;
        [Min(0.1f)] public float roadWidthMin = 6f;
        [Min(0.1f)] public float roadWidthMax = 12f;
        [Min(0f)] public float roadJitter = 22f;
        [Range(0f, 1f)] public float roadBurnBlend = 0.85f;

        [Header("River Auto Generation")]
        [Range(0, 8)] public int riverCount = 1;
        [Min(0.1f)] public float riverWidthMin = 10f;
        [Min(0.1f)] public float riverWidthMax = 22f;
        [Min(0.1f)] public float riverDepth = 5f;
        [Min(0f)] public float riverBankHeight = 1.5f;
        [Range(0f, 1f)] public float riverBurnBlend = 0.9f;

        [Header("Seeds")]
        public int roadSeedOffset = 1000;
        public int riverSeedOffset = 2000;
    }
}
