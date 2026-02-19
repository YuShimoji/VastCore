using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Vastcore.Utilities;

namespace Vastcore.Core
{
    /// <summary>
    /// 鬯ｮ莨懶ｽｺ・ｦ陷ｷ蝓溘・郢ｧ・ｷ郢ｧ・ｹ郢昴・ﾎ・- 髫阪・辟夊舉・ｰ陟厄ｽ｢郢ｧ・ｿ郢ｧ・､郢晏干繝ｻ陷ｷ蝓溘・郢ｧ蝣､・ｮ・｡騾・・
    /// 騾｡・ｰ邵ｺ・ｪ郢ｧ蜿･諷崎厄ｽ｢郢ｧ・ｿ郢ｧ・､郢晏干・帝明・ｪ霎滂ｽｶ邵ｺ・ｫ郢晄じﾎ樒ｹ晢ｽｳ郢晏ｳｨ・邵ｺ・ｦ髫阪・蟆・ｸｺ・ｪ陜ｨ・ｰ陟厄ｽ｢郢ｧ蝣､蜃ｽ隰後・
    /// </summary>
    public class TerrainSynthesizer : MonoBehaviour
    {
        [Header("Synthesis Settings")]
        [SerializeField] private TerrainType dominantType = TerrainType.Plain;
        [SerializeField] private List<TerrainTypeDefinition> availableTypes;
        [SerializeField] private AnimationCurve blendCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("騾墓ｻ薙・郢昜ｻ｣ﾎ帷ｹ晢ｽ｡郢晢ｽｼ郢ｧ・ｿ")]
        [SerializeField] private int resolution = 256;
        [SerializeField] private float terrainSize = 1000f;
        [SerializeField] private int seed = 42;
        [SerializeField] private int regionCount = 8;

        // 陷繝ｻﾎ夂ｹ昴・繝ｻ郢ｧ・ｿ
        private Dictionary<Vector2Int, TerrainType> terrainMap;
        private Dictionary<TerrainType, TerrainTypeDefinition> typeDefinitions;
        private Dictionary<TerrainType, float[,]> terrainDataMap;
        private float[,] finalHeights;
        private Terrain terrain;
        private TerrainData terrainData;

        private void Awake()
        {
            InitializeSynthesizer();
        }

        /// <summary>
        /// 陷ｷ蝓溘・郢ｧ・ｷ郢ｧ・ｹ郢昴・ﾎ堤ｸｺ・ｮ陋ｻ譎・ｄ陋ｹ繝ｻ
        /// </summary>
        private void InitializeSynthesizer()
        {
            // Terrain郢ｧ・ｳ郢晢ｽｳ郢晄亢繝ｻ郢晞亂ﾎｦ郢晏現繝ｻ陷ｿ髢・ｾ繝ｻ
            terrain = GetComponent<Terrain>();
            if (terrain == null)
            {
                terrain = gameObject.AddComponent<Terrain>();
            }

            terrainData = terrain.terrainData;
            if (terrainData == null)
            {
                terrainData = new TerrainData();
                terrain.terrainData = terrainData;
            }

            // 郢ｧ・ｿ郢ｧ・､郢晄懶ｽｮ螟ゑｽｾ・ｩ邵ｺ・ｮ陋ｻ譎・ｄ陋ｹ繝ｻ
            InitializeTypeDefinitions();

            // 陜ｨ・ｰ陟厄ｽ｢郢昴・繝ｻ郢ｧ・ｿ邵ｺ・ｮ髫ｪ・ｭ陞ｳ繝ｻ
            terrainData.heightmapResolution = resolution;
            terrainData.size = new Vector3(terrainSize, 500f, terrainSize);

            VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", "Terrain synthesizer initialized.");
        }

        /// <summary>
        /// 陜ｨ・ｰ陟厄ｽ｢郢ｧ・ｿ郢ｧ・､郢晄懶ｽｮ螟ゑｽｾ・ｩ邵ｺ・ｮ陋ｻ譎・ｄ陋ｹ繝ｻ
        /// </summary>
        private void InitializeTypeDefinitions()
        {
            typeDefinitions = new Dictionary<TerrainType, TerrainTypeDefinition>();

            // 闖ｴ・ｿ騾包ｽｨ陷ｿ・ｯ髢ｭ・ｽ邵ｺ・ｪ郢ｧ・ｿ郢ｧ・､郢晏干・帝恷讓雁ｶ檎ｸｺ・ｫ騾具ｽｻ鬪ｭ・ｲ
            foreach (var typeDef in availableTypes)
            {
                typeDefinitions[typeDef.type] = typeDef;
            }

            // 郢昴・繝ｵ郢ｧ・ｩ郢晢ｽｫ郢晏現縺｡郢ｧ・､郢晏干繝ｻ髴托ｽｽ陷会｣ｰ繝ｻ莠包ｽｸ蟠趣ｽｶ・ｳ邵ｺ蜉ｱ窶ｻ邵ｺ繝ｻ・玖撻・ｴ陷ｷ闌ｨ・ｼ繝ｻ
            foreach (TerrainType terrainType in System.Enum.GetValues(typeof(TerrainType)))
            {
                if (!typeDefinitions.ContainsKey(terrainType))
                {
                    typeDefinitions[terrainType] = TerrainTypeDefinition.CreateDefault(terrainType);
                    VastcoreLogger.Instance.LogWarning("TerrainSynthesizer",
                        $"Terrain type {terrainType} definition was missing. Default settings were created.");
                }
            }
        }

        /// <summary>
        /// 陷ｷ蝓溘・陜ｨ・ｰ陟厄ｽ｢邵ｺ・ｮ騾墓ｻ薙・郢ｧ蟶晏ｹ戊沂繝ｻ
        /// </summary>
        [ContextMenu("Generate Synthesized Terrain")]
        public void GenerateSynthesizedTerrain()
        {
            try
            {
                VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", "Starting synthesized terrain generation.");

                // 1. 陜ｨ・ｰ陟厄ｽ｢郢ｧ・ｿ郢ｧ・､郢晄懊・陝ｶ繝ｻ繝ｻ騾墓ｻ薙・
                GenerateTerrainTypeDistribution();

                // 2. 陷ｷ繝ｻ縺｡郢ｧ・､郢晏干繝ｻ陜ｨ・ｰ陟厄ｽ｢郢昴・繝ｻ郢ｧ・ｿ騾墓ｻ薙・
                GenerateIndividualTerrainData();

                // 3. 陷ｷ蝓溘・邵ｺ・ｨ郢晄じﾎ樒ｹ晢ｽｳ郢晉甥繝ｻ騾・・
                SynthesizeTerrainData();

                // 4. 隴崢驍ｨ繧・・陟厄ｽ｢邵ｺ・ｮ鬩包ｽｩ騾包ｽｨ
                ApplySynthesizedTerrain();

                VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", "Synthesized terrain generation completed.");
            }
            catch (System.Exception ex)
            {
                VastcoreLogger.Instance.LogError("TerrainSynthesizer", $"陜ｨ・ｰ陟厄ｽ｢騾墓ｻ薙・闕ｳ・ｭ邵ｺ・ｫ郢ｧ・ｨ郢晢ｽｩ郢晢ｽｼ邵ｺ讙主験騾輔・ {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 陜ｨ・ｰ陟厄ｽ｢郢ｧ・ｿ郢ｧ・､郢晄懊・陝ｶ繝ｻ繝ｻ騾墓ｻ薙・
        /// </summary>
        private void GenerateTerrainTypeDistribution()
        {
            terrainMap = new Dictionary<Vector2Int, TerrainType>();

            // 郢晢ｽｩ郢晢ｽｳ郢敖郢晢｣ｰ郢ｧ・ｷ郢晢ｽｼ郢晏ｳｨ繝ｻ髫ｪ・ｭ陞ｳ繝ｻ
            Random.InitState(seed);

            // 郢晢ｽｪ郢晢ｽｼ郢ｧ・ｸ郢晢ｽｧ郢晢ｽｳ闕ｳ・ｭ陟｢繝ｻ縺帷ｸｺ・ｮ騾墓ｻ薙・
            List<Vector2> regionCenters = new List<Vector2>();
            for (int i = 0; i < regionCount; i++)
            {
                float x = Random.Range(0f, terrainSize);
                float z = Random.Range(0f, terrainSize);
                regionCenters.Add(new Vector2(x, z));
            }

            // 陷ｷ繝ｻ繝ｻ郢ｧ・､郢晢ｽｳ郢晏現竊楢ｭ崢郢ｧ繧奇ｽｿ莉｣・樒ｹ晢ｽｪ郢晢ｽｼ郢ｧ・ｸ郢晢ｽｧ郢晢ｽｳ邵ｺ・ｮ郢ｧ・ｿ郢ｧ・､郢晏干・定恆・ｲ郢ｧ髮・ｽｽ阮吮ｻ
            int gridSize = resolution / 16; // 16x16邵ｺ・ｮ郢ｧ・ｰ郢晢ｽｪ郢昴・繝ｩ邵ｺ・ｧ驍・ｽ｡騾｡・･陋ｹ繝ｻ
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    Vector2 worldPos = new Vector2(
                        (float)x / gridSize * terrainSize,
                        (float)z / gridSize * terrainSize
                    );

                    // 隴崢郢ｧ繧奇ｽｿ莉｣・樒ｹ晢ｽｪ郢晢ｽｼ郢ｧ・ｸ郢晢ｽｧ郢晢ｽｳ郢ｧ螳夲ｽｦ荵昶命邵ｺ莉｣・・
                    TerrainType closestType = dominantType;
                    float minDistance = float.MaxValue;

                    for (int i = 0; i < regionCenters.Count; i++)
                    {
                        float distance = Vector2.Distance(worldPos, regionCenters[i]);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            // 郢ｧ・ｿ郢ｧ・､郢晏干・帝ｬ・・蛻・ｸｺ・ｫ陷托ｽｲ郢ｧ髮・ｽｽ阮吮ｻ繝ｻ莠･・ｾ・ｪ霑ｺ・ｰ繝ｻ繝ｻ
                            closestType = (TerrainType)(i % System.Enum.GetValues(typeof(TerrainType)).Length);
                        }
                    }

                    terrainMap[new Vector2Int(x, z)] = closestType;
                }
            }

            VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", $"Generated terrain type map points: {terrainMap.Count}");
        }

        /// <summary>
        /// 陷ｷ繝ｻ縺｡郢ｧ・､郢晏干繝ｻ陜ｨ・ｰ陟厄ｽ｢郢昴・繝ｻ郢ｧ・ｿ騾墓ｻ薙・
        /// </summary>
        private void GenerateIndividualTerrainData()
        {
            var terrainDataMap = new Dictionary<TerrainType, float[,]>();

            foreach (TerrainType terrainType in System.Enum.GetValues(typeof(TerrainType)))
            {
                if (!typeDefinitions.ContainsKey(terrainType)) continue;

                var typeDef = typeDefinitions[terrainType];
                float[,] heights = new float[resolution, resolution];

                // 陷ｷ繝ｻ縺｡郢ｧ・､郢晄懷ｴ玖ｭ帛ｳｨ繝ｻ陜ｨ・ｰ陟厄ｽ｢騾墓ｻ薙・郢晢ｽｭ郢ｧ・ｸ郢昴・縺・
                GenerateTerrainHeights(heights, terrainType, typeDef);

                terrainDataMap[terrainType] = heights;
            }

            // 陷繝ｻﾎ夂ｹ昴・繝ｻ郢ｧ・ｿ邵ｺ・ｨ邵ｺ蜉ｱ窶ｻ闖ｫ譎・亜
            this.terrainDataMap = terrainDataMap;

            VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", $"Generated per-type terrain datasets: {terrainDataMap.Count}");
        }

        /// <summary>
        /// 隰悶・・ｮ螢ｹ・・ｹｧ蠕娯螺陜ｨ・ｰ陟厄ｽ｢郢ｧ・ｿ郢ｧ・､郢晏干繝ｻ鬯ｮ蛟･・・ｹｧ蝣､蜃ｽ隰後・
        /// </summary>
        private void GenerateTerrainHeights(float[,] heights, TerrainType terrainType, TerrainTypeDefinition typeDef)
        {
            for (int x = 0; x < resolution; x++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    float worldX = (float)x / resolution * terrainSize;
                    float worldZ = (float)z / resolution * terrainSize;

                    // 陜難ｽｺ隴幢ｽｬ邵ｺ・ｮ鬯ｮ蛟･・・墓ｻ薙・繝ｻ蛹ｻ繝ｱ郢晢ｽｼ郢晢ｽｪ郢晢ｽｳ郢晏ｼｱ縺・ｹｧ・ｺ郢晏生繝ｻ郢ｧ・ｹ繝ｻ繝ｻ
                    float baseHeight = GenerateBaseHeight(worldX, worldZ, typeDef);

                    // 郢ｧ・ｿ郢ｧ・､郢晄懷ｴ玖ｭ帛ｳｨ繝ｻ霑夲ｽｹ陟包ｽｴ郢ｧ螳夲ｽｿ・ｽ陷会｣ｰ
                    float typeSpecificHeight = GenerateTypeSpecificHeight(worldX, worldZ, terrainType, typeDef);

                    // 隴崢驍ｨ繧牙飭邵ｺ・ｪ鬯ｮ蛟･・・ｹｧ蜻茨ｽｭ・｣髫穂ｸ槫密
                    heights[z, x] = Mathf.Clamp01((baseHeight + typeSpecificHeight) / 500f); // 500邵ｺ・ｯ隴崢陞滂ｽｧ鬯ｮ蛟･・・
                }
            }
        }

        /// <summary>
        /// 陜難ｽｺ隴幢ｽｬ邵ｺ・ｮ鬯ｮ蛟･・・ｹｧ蝣､蜃ｽ隰後・
        /// </summary>
        private float GenerateBaseHeight(float worldX, float worldZ, TerrainTypeDefinition typeDef)
        {
            // 郢昜ｻ｣繝ｻ郢晢ｽｪ郢晢ｽｳ郢晏ｼｱ縺・ｹｧ・ｺ邵ｺ・ｧ陜難ｽｺ隴幢ｽｬ騾ｧ繝ｻ竊題･搾ｽｷ闔ｨ荳奇ｽ帝墓ｻ薙・
            float noise1 = Mathf.PerlinNoise(worldX * 0.001f, worldZ * 0.001f) * 100f;
            float noise2 = Mathf.PerlinNoise(worldX * 0.01f, worldZ * 0.01f) * 50f;
            float noise3 = Mathf.PerlinNoise(worldX * 0.1f, worldZ * 0.1f) * 25f;

            return noise1 + noise2 + noise3;
        }

        /// <summary>
        /// 陜ｨ・ｰ陟厄ｽ｢郢ｧ・ｿ郢ｧ・､郢晄懷ｴ玖ｭ帛ｳｨ繝ｻ鬯ｮ蛟･・・ｹｧ蝣､蜃ｽ隰後・
        /// </summary>
        private float GenerateTypeSpecificHeight(float worldX, float worldZ, TerrainType terrainType, TerrainTypeDefinition typeDef)
        {
            float height = 0f;

            switch (terrainType)
            {
                case TerrainType.Mountain:
                    // 陞ｻ・ｱ陝ｯ・ｳ陜ｨ・ｰ陝ｶ・ｯ繝ｻ螟撰ｽｫ蛟･・櫁ｮ灘虫・ｫ蛟･竊定ｫ､・･陝ｲ・ｻ邵ｺ・ｪ隴∵・謫・
                    height = Mathf.PerlinNoise(worldX * 0.005f, worldZ * 0.005f) * 200f;
                    break;

                case TerrainType.Hill:
                    // 闕ｳ蛟ｬ蛹剰舉・ｰ陝ｶ・ｯ繝ｻ螢ｻ・ｸ・ｭ驕槫唱・ｺ・ｦ邵ｺ・ｮ隘搾ｽｷ闔ｨ繝ｻ
                    height = Mathf.PerlinNoise(worldX * 0.01f, worldZ * 0.01f) * 100f;
                    break;

                case TerrainType.Plain:
                    // 陝ｷ・ｳ鬩･雜｣・ｼ螢ｽ・ｯ遒托ｽｼ繝ｻ蝎ｪ陝ｷ・ｳ陜ｮ・ｦ
                    height = Mathf.PerlinNoise(worldX * 0.02f, worldZ * 0.02f) * 20f;
                    break;

                case TerrainType.Valley:
                    // 髫ｹ・ｷ繝ｻ螢ｻ・ｽ蠑ｱ・櫁ｮ灘虫・ｫ繝ｻ
                    height = -Mathf.PerlinNoise(worldX * 0.008f, worldZ * 0.008f) * 50f;
                    break;

                case TerrainType.Plateau:
                    // 鬯ｮ莨懈・繝ｻ螟撰ｽｫ蛟･・樒ｸｺ謔滂ｽｹ・ｳ陜ｮ・ｦ邵ｺ・ｪ陜ｨ・ｰ陜薙・
                    height = 150f + Mathf.PerlinNoise(worldX * 0.05f, worldZ * 0.05f) * 30f;
                    break;

                default:
                    height = 0f;
                    break;
            }

            return height;
        }

        /// <summary>
        /// 陷ｷ蝓溘・邵ｺ・ｨ郢晄じﾎ樒ｹ晢ｽｳ郢晉甥繝ｻ騾・・
        /// </summary>
        private void SynthesizeTerrainData()
        {
            float[,] finalHeights = new float[resolution, resolution];
            int gridSize = resolution / 16; // terrainMap邵ｺ・ｮ郢ｧ・ｰ郢晢ｽｪ郢昴・繝ｩ郢ｧ・ｵ郢ｧ・､郢ｧ・ｺ

            for (int x = 0; x < resolution; x++)
            {
                for (int z = 0; z < resolution; z++)
                {
                    // 郢ｧ・ｰ郢晢ｽｪ郢昴・繝ｩ陟趣ｽｧ隶灘生竊楢棔逕ｻ驪､
                    int gridX = Mathf.Clamp(x / 16, 0, gridSize - 1);
                    int gridZ = Mathf.Clamp(z / 16, 0, gridSize - 1);
                    Vector2Int gridPos = new Vector2Int(gridX, gridZ);

                    // 邵ｺ阮吶・闖ｴ蜥ｲ・ｽ・ｮ邵ｺ・ｮ陜ｨ・ｰ陟厄ｽ｢郢ｧ・ｿ郢ｧ・､郢晏干・定愾髢・ｾ繝ｻ
                    TerrainType terrainType = terrainMap.ContainsKey(gridPos) ? terrainMap[gridPos] : dominantType;

                    // 陜ｨ・ｰ陟厄ｽ｢郢ｧ・ｿ郢ｧ・､郢晏干繝ｻ鬯ｮ蛟･・・ｹｧ雋槫徐陟輔・
                    float typeHeight = 0f;
                    if (terrainDataMap.ContainsKey(terrainType))
                    {
                        typeHeight = terrainDataMap[terrainType][z, x];
                    }

                    // 郢晄じﾎ樒ｹ晢ｽｳ郢晉甥繝ｻ騾・・・ｼ莠･謐蛾恷・ｺ邵ｺ・ｮ郢ｧ・ｿ郢ｧ・､郢晏干竊堤ｸｺ・ｮ髢ｾ・ｪ霎滂ｽｶ邵ｺ・ｪ鬩包ｽｷ驕假ｽｻ繝ｻ繝ｻ
                    float blendedHeight = BlendTerrainHeight(x, z, terrainType, typeHeight, gridSize);

                    finalHeights[z, x] = blendedHeight;
                }
            }

            this.finalHeights = finalHeights;

            VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", "Terrain data synthesis and blending completed.");
        }

        /// <summary>
        /// 陜ｨ・ｰ陟厄ｽ｢邵ｺ・ｮ鬯ｮ蛟･・・ｹｧ蛛ｵ繝ｶ郢晢ｽｬ郢晢ｽｳ郢昴・
        /// </summary>
        private float BlendTerrainHeight(int x, int z, TerrainType centerType, float centerHeight, int gridSize)
        {
            float totalWeight = 0f;
            float blendedHeight = 0f;

            // 3x3邵ｺ・ｮ郢ｧ・ｰ郢晢ｽｪ郢昴・繝ｩ驕ｽ繝ｻ蟲・ｸｺ・ｧ郢晄じﾎ樒ｹ晢ｽｳ郢昴・
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
                {
                    int gridX = (x / 16) + offsetX;
                    int gridZ = (z / 16) + offsetZ;

                    // 陟・・髦懃ｹ昶・縺臥ｹ昴・縺・
                    if (gridX < 0 || gridX >= gridSize || gridZ < 0 || gridZ >= gridSize)
                        continue;

                    Vector2Int gridPos = new Vector2Int(gridX, gridZ);
                    TerrainType neighborType = terrainMap.ContainsKey(gridPos) ? terrainMap[gridPos] : dominantType;

                    // 髴肴辨螻ｬ邵ｺ・ｫ陜難ｽｺ邵ｺ・･邵ｺ蝓弱裟邵ｺ・ｿ髫ｪ閧ｲ・ｮ繝ｻ
                    float distance = Mathf.Sqrt(offsetX * offsetX + offsetZ * offsetZ);
                    float weight = distance == 0f ? 1f : blendCurve.Evaluate(1f / (distance + 1f));

                    // 陷ｷ蠕個ｧ郢ｧ・ｿ郢ｧ・､郢晏干繝ｻ陜｣・ｴ陷ｷ蛹ｻ繝ｻ郢ｧ蛹ｻ・願托ｽｷ邵ｺ繝ｻ纃ｾ邵ｺ・ｿ
                    if (neighborType == centerType)
                    {
                        weight *= 2f;
                    }

                    // 陜ｨ・ｰ陟厄ｽ｢郢昴・繝ｻ郢ｧ・ｿ邵ｺ荵晢ｽ蛾ｬｮ蛟･・・ｹｧ雋槫徐陟輔・
                    float neighborHeight = 0f;
                    if (terrainDataMap.ContainsKey(neighborType))
                    {
                        neighborHeight = terrainDataMap[neighborType][z, x];
                    }

                    blendedHeight += neighborHeight * weight;
                    totalWeight += weight;
                }
            }

            return totalWeight > 0f ? blendedHeight / totalWeight : centerHeight;
        }

        /// <summary>
        /// 隴崢驍ｨ繧・・陟厄ｽ｢邵ｺ・ｮ鬩包ｽｩ騾包ｽｨ
        /// </summary>
        private void ApplySynthesizedTerrain()
        {
            if (finalHeights == null)
            {
                VastcoreLogger.Instance.LogError("TerrainSynthesizer", "Final terrain height data is null.");
                return;
            }

            try
            {
                // TerrainData邵ｺ・ｮ鬯ｮ蛟･・・ｹｧ螳夲ｽｨ・ｭ陞ｳ繝ｻ
                terrainData.SetHeights(0, 0, finalHeights);

                // 郢昴・縺醍ｹｧ・ｹ郢昶・ﾎ慕ｸｺ・ｨ髫ｧ・ｳ驍擾ｽｰ髫ｪ・ｭ陞ｳ螢ｹ繝ｻ鬩包ｽｩ騾包ｽｨ
                ApplyTerrainTextures();
                ApplyTerrainDetails();

                // Terrain郢ｧ・ｳ郢晢ｽｳ郢晄亢繝ｻ郢晞亂ﾎｦ郢晏現繝ｻ隴厄ｽｴ隴・ｽｰ
                terrain.Flush();

                VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", $"隴崢驍ｨ繧・・陟厄ｽ｢邵ｺ遒≫・騾包ｽｨ邵ｺ霈費ｽ檎ｸｺ・ｾ邵ｺ蜉ｱ笳・(郢ｧ・ｵ郢ｧ・､郢ｧ・ｺ: {terrainSize}x{terrainSize}, 髫暦ｽ｣陷剃ｸ橸ｽｺ・ｦ: {resolution})");
            }
            catch (System.Exception ex)
            {
                VastcoreLogger.Instance.LogError("TerrainSynthesizer", $"陜ｨ・ｰ陟厄ｽ｢鬩包ｽｩ騾包ｽｨ闕ｳ・ｭ邵ｺ・ｫ郢ｧ・ｨ郢晢ｽｩ郢晢ｽｼ邵ｺ讙主験騾輔・ {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 陜ｨ・ｰ陟厄ｽ｢郢昴・縺醍ｹｧ・ｹ郢昶・ﾎ慕ｹｧ蟶昶・騾包ｽｨ
        /// </summary>
        private void ApplyTerrainTextures()
        {
            if (availableTypes.Count == 0) return;

            // 郢昴・縺醍ｹｧ・ｹ郢昶・ﾎ慕ｸｺ・ｮ髫ｪ・ｭ陞ｳ螟ｲ・ｼ閧ｲ・ｰ・｡隴冗§・ｮ貅ｯ・｣繝ｻ・ｼ繝ｻ
            var terrainLayers = new TerrainLayer[availableTypes.Count];

            for (int i = 0; i < availableTypes.Count; i++)
            {
                var typeDef = availableTypes[i];
                if (typeDef.terrainTexture != null)
                {
                    var layer = new TerrainLayer();
                    layer.diffuseTexture = typeDef.terrainTexture;
                    layer.tileSize = new Vector2(terrainSize / 10f, terrainSize / 10f);
                    terrainLayers[i] = layer;
                }
            }

            if (terrainLayers.Length > 0)
            {
                terrainData.terrainLayers = terrainLayers;

                // 郢昴・縺醍ｹｧ・ｹ郢昶・ﾎ慕ｸｺ・ｮ鬩･髦ｪ竏ｩ郢ｧ螳夲ｽｨ・ｭ陞ｳ螟ｲ・ｼ閧ｲ・ｰ・｡隴城豪豐ｿ繝ｻ繝ｻ
                float[,,] alphamaps = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainLayers.Length];

                for (int x = 0; x < terrainData.alphamapWidth; x++)
                {
                    for (int z = 0; z < terrainData.alphamapHeight; z++)
                    {
                        // 陜ｨ・ｰ陟厄ｽ｢郢ｧ・ｿ郢ｧ・､郢晏干竊楢搏・ｺ邵ｺ・･邵ｺ繝ｻ窶ｻ郢昴・縺醍ｹｧ・ｹ郢昶・ﾎ暮ｩ･髦ｪ竏ｩ郢ｧ螳夲ｽｨ・ｭ陞ｳ繝ｻ
                        int gridX = Mathf.Clamp(x * 16 / resolution, 0, resolution / 16 - 1);
                        int gridZ = Mathf.Clamp(z * 16 / resolution, 0, resolution / 16 - 1);
                        Vector2Int gridPos = new Vector2Int(gridX, gridZ);

                        TerrainType terrainType = terrainMap.ContainsKey(gridPos) ? terrainMap[gridPos] : dominantType;

                        // 陝・ｽｾ陟｢諛岩・郢ｧ荵斟樒ｹｧ・､郢晢ｽ､郢晢ｽｼ邵ｺ・ｫ鬩･髦ｪ竏ｩ郢ｧ螳夲ｽｨ・ｭ陞ｳ繝ｻ
                        for (int layer = 0; layer < terrainLayers.Length; layer++)
                        {
                            if (layer < availableTypes.Count && availableTypes[layer].type == terrainType)
                            {
                                alphamaps[z, x, layer] = 1f;
                            }
                            else
                            {
                                alphamaps[z, x, layer] = 0f;
                            }
                        }
                    }
                }

                terrainData.SetAlphamaps(0, 0, alphamaps);
            }
        }

        /// <summary>
        /// 陜ｨ・ｰ陟厄ｽ｢髫ｧ・ｳ驍擾ｽｰ繝ｻ驛∵狭隴幢ｽｨ邵ｺ・ｪ邵ｺ・ｩ繝ｻ蟲ｨ・帝ｩ包ｽｩ騾包ｽｨ
        /// </summary>
        private void ApplyTerrainDetails()
        {
            // 髫ｧ・ｳ驍擾ｽｰ髫ｪ・ｭ陞ｳ螢ｹ繝ｻ隹ｺ・｡邵ｺ・ｮ郢晁ｼ斐♂郢晢ｽｼ郢ｧ・ｺ邵ｺ・ｧ陞ｳ貅ｯ・｣繝ｻ
            VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", "Terrain detail settings are currently placeholders.");
        }

        /// <summary>
        /// 郢昴・繝ｰ郢昴・縺帝包ｽｨ邵ｺ・ｮ陜ｨ・ｰ陟厄ｽ｢郢ｧ・ｿ郢ｧ・､郢晄懊・陝ｶ繝ｻ・帝勗・ｨ驕会ｽｺ
        /// </summary>
        [ContextMenu("Debug Terrain Type Distribution")]
        public void DebugTerrainTypeDistribution()
        {
            if (terrainMap == null || terrainMap.Count == 0)
            {
                VastcoreLogger.Instance.LogWarning("TerrainSynthesizer", "Terrain type map is empty. Generate terrain first.");
                return;
            }

            var typeCounts = new Dictionary<TerrainType, int>();
            foreach (var kvp in terrainMap)
            {
                if (!typeCounts.ContainsKey(kvp.Value))
                    typeCounts[kvp.Value] = 0;
                typeCounts[kvp.Value]++;
            }

            VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", "陜ｨ・ｰ陟厄ｽ｢郢ｧ・ｿ郢ｧ・､郢晄懊・陝ｶ繝ｻ");
            foreach (var kvp in typeCounts)
            {
                float percentage = (float)kvp.Value / terrainMap.Count * 100f;
                VastcoreLogger.Instance.LogInfo("TerrainSynthesizer", $"{kvp.Key}: {kvp.Value} 郢晄亢縺・ｹ晢ｽｳ郢昴・({percentage:F1}%)");
            }
        }
    }
}
