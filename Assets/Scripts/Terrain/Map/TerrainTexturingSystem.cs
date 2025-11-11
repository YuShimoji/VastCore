using UnityEngine;

using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Generation
{
    /// <summary>
    /// 蝨ｰ蠖｢繝・け繧ｹ繝√Ε繝ｪ繝ｳ繧ｰ繧ｷ繧ｹ繝・Β
    /// 隕∵ｱ・.5, 2.1: 鬮伜ｺｦ繝ｻ蛯ｾ譁懊↓蠢懊§縺溯・蜍輔ユ繧ｯ繧ｹ繝√Ε繝ｪ繝ｳ繧ｰ縺ｨ繝舌う繧ｪ繝ｼ繝險ｭ螳・
    /// </summary>
    public class TerrainTexturingSystem : MonoBehaviour
    {
        #region 險ｭ螳壹ヱ繝ｩ繝｡繝ｼ繧ｿ
        [Header("鬮伜ｺｦ繝吶・繧ｹ繝・け繧ｹ繝√Ε繝ｪ繝ｳ繧ｰ")]
        public List<AltitudeTextureLayer> altitudeLayers = new List<AltitudeTextureLayer>();
        public AnimationCurve altitudeBlendCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public float altitudeBlendSmoothness = 0.1f;
        
        [Header("蛯ｾ譁懊・繝ｼ繧ｹ繝・け繧ｹ繝√Ε繝ｪ繝ｳ繧ｰ")]
        public List<SlopeTextureLayer> slopeLayers = new List<SlopeTextureLayer>();
        public AnimationCurve slopeBlendCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public float slopeBlendSmoothness = 5f;
        
        [Header("蜍慕噪繝槭ユ繝ｪ繧｢繝ｫ繝悶Ξ繝ｳ繝・ぅ繝ｳ繧ｰ")]
        public bool enableDynamicBlending = true;
        public float blendTransitionSpeed = 2f;
        public int maxTextureBlends = 4;
        
        [Header("LOD繝・け繧ｹ繝√Ε繧ｷ繧ｹ繝・Β")]
        public bool enableLODTextures = true;
        public float[] lodDistances = { 500f, 1000f, 2000f };
        public List<LODTextureSet> lodTextureSets = new List<LODTextureSet>();
        #endregion

        #region 繝励Λ繧､繝吶・繝亥､画焚
        private Dictionary<TerrainTile, TerrainTextureData> tileTextureData = new Dictionary<TerrainTile, TerrainTextureData>();
        private Queue<TextureUpdateRequest> textureUpdateQueue = new Queue<TextureUpdateRequest>();
        private Transform playerTransform;
        private MaterialPropertyBlock materialPropertyBlock;
        private Shader terrainShader;
        #endregion

        #region Unity 繧､繝吶Φ繝・
        void Start()
        {
            InitializeTexturingSystem();
        }
        
        void Update()
        {
            ProcessTextureUpdateQueue();
        }
        #endregion

        #region 蛻晄悄蛹・
        /// <summary>
        /// 繝・け繧ｹ繝√Ε繝ｪ繝ｳ繧ｰ繧ｷ繧ｹ繝・Β繧貞・譛溷喧
        /// </summary>
        private void InitializeTexturingSystem()
        {
            Debug.Log("Initializing TerrainTexturingSystem...");
            
            // 繝励Ξ繧､繝､繝ｼTransform繧貞叙蠕・
            var playerController = FindFirstObjectByType<AdvancedPlayerController>();
            if (playerController != null)
            {
                playerTransform = playerController.transform;
            }
            
            // MaterialPropertyBlock繧貞・譛溷喧
            materialPropertyBlock = new MaterialPropertyBlock();
            
            // 蝨ｰ蠖｢繧ｷ繧ｧ繝ｼ繝繝ｼ繧貞叙蠕・
            terrainShader = Shader.Find("Standard");
            
            // 繝・ヵ繧ｩ繝ｫ繝郁ｨｭ螳壹ｒ蛻晄悄蛹・
            InitializeDefaultSettings();
            
            Debug.Log("TerrainTexturingSystem initialized successfully");
        }
        
        /// <summary>
        /// 繝・ヵ繧ｩ繝ｫ繝郁ｨｭ螳壹ｒ蛻晄悄蛹・
        /// </summary>
        private void InitializeDefaultSettings()
        {
            if (altitudeLayers.Count == 0)
            {
                CreateDefaultAltitudeLayers();
            }
            
            if (slopeLayers.Count == 0)
            {
                CreateDefaultSlopeLayers();
            }
            
            if (lodTextureSets.Count == 0)
            {
                CreateDefaultLODTextureSets();
            }
        }
        #endregion

        #region 繝代ヶ繝ｪ繝・けAPI
        /// <summary>
        /// 蝨ｰ蠖｢繧ｿ繧､繝ｫ縺ｫ繝・け繧ｹ繝√Ε繧帝←逕ｨ
        /// </summary>
        public void ApplyTextureToTile(TerrainTile tile)
        {
            if (tile == null || tile.tileObject == null)
                return;
            
            var textureData = GenerateTextureDataForTile(tile);
            tileTextureData[tile] = textureData;
            
            ApplyTextureDataToTile(tile, textureData);
        }
        
        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝繝励Μ繧ｻ繝・ヨ縺ｫ蝓ｺ縺･縺・※繝・け繧ｹ繝√Ε繧帝←逕ｨ
        /// </summary>
        public void ApplyBiomeTextures(TerrainTile tile, BiomePreset biomePreset)
        {
            if (tile == null || biomePreset == null)
                return;
            
            var textureData = GenerateBiomeTextureData(tile, biomePreset);
            tileTextureData[tile] = textureData;
            
            ApplyTextureDataToTile(tile, textureData);
        }
        #endregion

        #region 繝・ヵ繧ｩ繝ｫ繝郁ｨｭ螳壻ｽ懈・
        /// <summary>
        /// 繝・ヵ繧ｩ繝ｫ繝磯ｫ伜ｺｦ繝ｬ繧､繝､繝ｼ繧剃ｽ懈・
        /// </summary>
        private void CreateDefaultAltitudeLayers()
        {
            altitudeLayers.Add(new AltitudeTextureLayer
            {
                name = "Water Level",
                minAltitude = -10f,
                maxAltitude = 5f,
                textureType = TerrainTextureType.Sand,
                blendStrength = 1f,
                tiling = Vector2.one * 10f
            });
            
            altitudeLayers.Add(new AltitudeTextureLayer
            {
                name = "Lowlands",
                minAltitude = 0f,
                maxAltitude = 50f,
                textureType = TerrainTextureType.Grass,
                blendStrength = 1f,
                tiling = Vector2.one * 15f
            });
            
            altitudeLayers.Add(new AltitudeTextureLayer
            {
                name = "Highlands",
                minAltitude = 40f,
                maxAltitude = 150f,
                textureType = TerrainTextureType.Rock,
                blendStrength = 0.8f,
                tiling = Vector2.one * 8f
            });
            
            altitudeLayers.Add(new AltitudeTextureLayer
            {
                name = "Mountains",
                minAltitude = 120f,
                maxAltitude = 300f,
                textureType = TerrainTextureType.Snow,
                blendStrength = 1f,
                tiling = Vector2.one * 5f
            });
        }
        
        /// <summary>
        /// 繝・ヵ繧ｩ繝ｫ繝亥だ譁懊Ξ繧､繝､繝ｼ繧剃ｽ懈・
        /// </summary>
        private void CreateDefaultSlopeLayers()
        {
            slopeLayers.Add(new SlopeTextureLayer
            {
                name = "Flat Terrain",
                minSlope = 0f,
                maxSlope = 15f,
                textureType = TerrainTextureType.Grass,
                blendStrength = 1f,
                overrideAltitude = false
            });
            
            slopeLayers.Add(new SlopeTextureLayer
            {
                name = "Gentle Slopes",
                minSlope = 10f,
                maxSlope = 35f,
                textureType = TerrainTextureType.Dirt,
                blendStrength = 0.7f,
                overrideAltitude = false
            });
            
            slopeLayers.Add(new SlopeTextureLayer
            {
                name = "Steep Slopes",
                minSlope = 30f,
                maxSlope = 60f,
                textureType = TerrainTextureType.Rock,
                blendStrength = 0.9f,
                overrideAltitude = true
            });
            
            slopeLayers.Add(new SlopeTextureLayer
            {
                name = "Cliffs",
                minSlope = 55f,
                maxSlope = 90f,
                textureType = TerrainTextureType.Cliff,
                blendStrength = 1f,
                overrideAltitude = true
            });
        }
        
        /// <summary>
        /// 繝・ヵ繧ｩ繝ｫ繝・OD繝・け繧ｹ繝√Ε繧ｻ繝・ヨ繧剃ｽ懈・
        /// </summary>
        private void CreateDefaultLODTextureSets()
        {
            lodTextureSets.Add(new LODTextureSet
            {
                name = "High Quality",
                textureResolution = 1024,
                normalMapStrength = 1f,
                detailStrength = 1f,
                maxDistance = lodDistances[0]
            });
            
            lodTextureSets.Add(new LODTextureSet
            {
                name = "Medium Quality",
                textureResolution = 512,
                normalMapStrength = 0.7f,
                detailStrength = 0.5f,
                maxDistance = lodDistances[1]
            });
            
            lodTextureSets.Add(new LODTextureSet
            {
                name = "Low Quality",
                textureResolution = 256,
                normalMapStrength = 0.3f,
                detailStrength = 0.2f,
                maxDistance = lodDistances[2]
            });
        }
        #endregion

        #region 繝・け繧ｹ繝√Ε逕滓・
        /// <summary>
        /// 繧ｿ繧､繝ｫ逕ｨ縺ｮ繝・け繧ｹ繝√Ε繝・・繧ｿ繧堤函謌・
        /// </summary>
        private TerrainTextureData GenerateTextureDataForTile(TerrainTile tile)
        {
            var textureData = new TerrainTextureData();
            
            if (tile.heightmap == null)
                return textureData;
            
            int resolution = tile.heightmap.GetLength(0);
            textureData.textureWeights = new float[resolution, resolution, maxTextureBlends];
            textureData.textureIndices = new int[resolution, resolution, maxTextureBlends];
            
            // 蜷・ヴ繧ｯ繧ｻ繝ｫ縺ｮ繝・け繧ｹ繝√Ε繧ｦ繧ｧ繧､繝医ｒ險育ｮ・
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    var weights = CalculateTextureWeightsAtPosition(tile, x, y, resolution);
                    
                    // 荳贋ｽ・縺､縺ｮ繝・け繧ｹ繝√Ε繧帝∈謚・
                    var sortedWeights = weights.OrderByDescending(w => w.weight).Take(maxTextureBlends).ToArray();
                    
                    for (int i = 0; i < maxTextureBlends && i < sortedWeights.Length; i++)
                    {
                        textureData.textureWeights[y, x, i] = sortedWeights[i].weight;
                        textureData.textureIndices[y, x, i] = (int)sortedWeights[i].textureType;
                    }
                }
            }
            
            return textureData;
        }
        
        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝逕ｨ縺ｮ繝・け繧ｹ繝√Ε繝・・繧ｿ繧堤函謌・
        /// </summary>
        private TerrainTextureData GenerateBiomeTextureData(TerrainTile tile, BiomePreset biomePreset)
        {
            var textureData = GenerateTextureDataForTile(tile);
            
            // 繝舌う繧ｪ繝ｼ繝迚ｹ諤ｧ繧帝←逕ｨ
            ApplyBiomeModifications(textureData, biomePreset);
            
            return textureData;
        }
        
        /// <summary>
        /// 謖・ｮ壻ｽ咲ｽｮ縺ｧ縺ｮ繝・け繧ｹ繝√Ε繧ｦ繧ｧ繧､繝医ｒ險育ｮ・
        /// </summary>
        private List<TextureWeight> CalculateTextureWeightsAtPosition(TerrainTile tile, int x, int y, int resolution)
        {
            var weights = new List<TextureWeight>();
            
            // 鬮伜ｺｦ繧貞叙蠕・
            float height = tile.heightmap[y, x] * tile.terrainParams.maxHeight;
            
            // 蛯ｾ譁懊ｒ險育ｮ・
            float slope = CalculateSlopeAtPosition(tile.heightmap, x, y, resolution);
            
            // 鬮伜ｺｦ繝吶・繧ｹ縺ｮ繝・け繧ｹ繝√Ε繧ｦ繧ｧ繧､繝医ｒ險育ｮ・
            foreach (var layer in altitudeLayers)
            {
                float weight = CalculateAltitudeWeight(height, layer);
                if (weight > 0f)
                {
                    weights.Add(new TextureWeight { textureType = layer.textureType, weight = weight * layer.blendStrength });
                }
            }
            
            // 蛯ｾ譁懊・繝ｼ繧ｹ縺ｮ繝・け繧ｹ繝√Ε繧ｦ繧ｧ繧､繝医ｒ險育ｮ・
            foreach (var layer in slopeLayers)
            {
                float weight = CalculateSlopeWeight(slope, layer);
                if (weight > 0f)
                {
                    var textureWeight = new TextureWeight { textureType = layer.textureType, weight = weight * layer.blendStrength };
                    
                    if (layer.overrideAltitude)
                    {
                        // 蛯ｾ譁懊ユ繧ｯ繧ｹ繝√Ε縺碁ｫ伜ｺｦ繝・け繧ｹ繝√Ε繧剃ｸ頑嶌縺・
                        weights.RemoveAll(w => w.textureType != layer.textureType);
                        weights.Add(textureWeight);
                    }
                    else
                    {
                        weights.Add(textureWeight);
                    }
                }
            }
            
            // 繧ｦ繧ｧ繧､繝医ｒ豁｣隕丞喧
            float totalWeight = weights.Sum(w => w.weight);
            if (totalWeight > 0f)
            {
                for (int i = 0; i < weights.Count; i++)
                {
                    var weight = weights[i];
                    weight.weight /= totalWeight;
                    weights[i] = weight;
                }
            }
            
            return weights;
        }
        
        /// <summary>
        /// 鬮伜ｺｦ繧ｦ繧ｧ繧､繝医ｒ險育ｮ・
        /// </summary>
        private float CalculateAltitudeWeight(float height, AltitudeTextureLayer layer)
        {
            if (height < layer.minAltitude || height > layer.maxAltitude)
                return 0f;
            
            float range = layer.maxAltitude - layer.minAltitude;
            float normalizedHeight = (height - layer.minAltitude) / range;
            
            return altitudeBlendCurve.Evaluate(normalizedHeight);
        }
        
        /// <summary>
        /// 蛯ｾ譁懊え繧ｧ繧､繝医ｒ險育ｮ・
        /// </summary>
        private float CalculateSlopeWeight(float slope, SlopeTextureLayer layer)
        {
            if (slope < layer.minSlope || slope > layer.maxSlope)
                return 0f;
            
            float range = layer.maxSlope - layer.minSlope;
            float normalizedSlope = (slope - layer.minSlope) / range;
            
            return slopeBlendCurve.Evaluate(normalizedSlope);
        }
        
        /// <summary>
        /// 謖・ｮ壻ｽ咲ｽｮ縺ｧ縺ｮ蛯ｾ譁懊ｒ險育ｮ・
        /// </summary>
        private float CalculateSlopeAtPosition(float[,] heightmap, int x, int y, int resolution)
        {
            // 髫｣謗･繝斐け繧ｻ繝ｫ縺ｮ鬮伜ｺｦ蟾ｮ縺九ｉ蛯ｾ譁懊ｒ險育ｮ・
            float currentHeight = heightmap[y, x];
            
            float leftHeight = x > 0 ? heightmap[y, x - 1] : currentHeight;
            float rightHeight = x < resolution - 1 ? heightmap[y, x + 1] : currentHeight;
            float topHeight = y > 0 ? heightmap[y - 1, x] : currentHeight;
            float bottomHeight = y < resolution - 1 ? heightmap[y + 1, x] : currentHeight;
            
            float dx = (rightHeight - leftHeight) * 0.5f;
            float dy = (bottomHeight - topHeight) * 0.5f;
            
            float slope = Mathf.Sqrt(dx * dx + dy * dy);
            return Mathf.Atan(slope) * Mathf.Rad2Deg;
        }
        #endregion

        #region 繝・け繧ｹ繝√Ε驕ｩ逕ｨ
        /// <summary>
        /// 繝・け繧ｹ繝√Ε繝・・繧ｿ繧偵ち繧､繝ｫ縺ｫ驕ｩ逕ｨ
        /// </summary>
        private void ApplyTextureDataToTile(TerrainTile tile, TerrainTextureData textureData)
        {
            if (tile.tileObject == null)
                return;
            
            var meshRenderer = tile.tileObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                return;
            
            // LOD繝ｬ繝吶Ν縺ｫ蠢懊§縺溘ユ繧ｯ繧ｹ繝√Ε繧ｻ繝・ヨ繧帝∈謚・
            var lodSet = GetLODTextureSetForDistance(tile.distanceFromPlayer);
            
            // 繝槭ユ繝ｪ繧｢繝ｫ繧剃ｽ懈・縺ｾ縺溘・譖ｴ譁ｰ
            Material material = CreateOrUpdateMaterial(tile, textureData, lodSet);
            meshRenderer.material = material;
        }
        
        /// <summary>
        /// 繝槭ユ繝ｪ繧｢繝ｫ繧剃ｽ懈・縺ｾ縺溘・譖ｴ譁ｰ
        /// </summary>
        private Material CreateOrUpdateMaterial(TerrainTile tile, TerrainTextureData textureData, LODTextureSet lodSet)
        {
            Material material = tile.terrainMaterial;
            
            if (material == null || material.shader != terrainShader)
            {
                material = new Material(terrainShader);
                material.name = $"TerrainMaterial_{tile.coordinate.x}_{tile.coordinate.y}";
                tile.terrainMaterial = material;
            }
            
            // LOD險ｭ螳壹ｒ驕ｩ逕ｨ
            material.SetFloat("_Glossiness", lodSet.detailStrength * 0.5f);
            
            // 鬮伜ｺｦ縺ｫ蝓ｺ縺･縺剰牡隱ｿ謨ｴ
            ApplyAltitudeBasedColoring(material, tile);
            
            return material;
        }
        
        /// <summary>
        /// 鬮伜ｺｦ縺ｫ蝓ｺ縺･縺剰牡隱ｿ謨ｴ繧帝←逕ｨ
        /// </summary>
        private void ApplyAltitudeBasedColoring(Material material, TerrainTile tile)
        {
            if (tile.heightmap == null)
                return;
            
            // 蟷ｳ蝮・ｫ伜ｺｦ繧定ｨ育ｮ・
            float averageHeight = CalculateAverageHeight(tile.heightmap) * tile.terrainParams.maxHeight;
            
            // 鬮伜ｺｦ縺ｫ蝓ｺ縺･縺・※濶ｲ繧定ｪｿ謨ｴ
            Color baseColor = Color.white;
            
            if (averageHeight < 10f)
            {
                baseColor = Color.Lerp(Color.blue, Color.yellow, 0.3f); // 豌ｴ霎ｺ
            }
            else if (averageHeight < 50f)
            {
                baseColor = Color.Lerp(Color.green, Color.yellow, 0.2f); // 菴主慍
            }
            else if (averageHeight < 150f)
            {
                baseColor = Color.Lerp(Color.gray, Color.brown, 0.5f); // 鬮伜慍
            }
            else
            {
                baseColor = Color.Lerp(Color.white, Color.gray, 0.3f); // 螻ｱ蝨ｰ
            }
            
            material.color = baseColor;
        }
        
        /// <summary>
        /// 蟷ｳ蝮・ｫ伜ｺｦ繧定ｨ育ｮ・
        /// </summary>
        private float CalculateAverageHeight(float[,] heightmap)
        {
            float sum = 0f;
            int count = 0;
            
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    sum += heightmap[y, x];
                    count++;
                }
            }
            
            return count > 0 ? sum / count : 0f;
        }
        #endregion

        #region 繝舌う繧ｪ繝ｼ繝蜃ｦ逅・
        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝螟画峩繧偵ユ繧ｯ繧ｹ繝√Ε繝・・繧ｿ縺ｫ驕ｩ逕ｨ
        /// </summary>
        private void ApplyBiomeModifications(TerrainTextureData textureData, BiomePreset biomePreset)
        {
            // 繝舌う繧ｪ繝ｼ繝迚ｹ諤ｧ縺ｫ蝓ｺ縺･縺・※繝・け繧ｹ繝√Ε繧ｦ繧ｧ繧､繝医ｒ隱ｿ謨ｴ
            ModifyTextureWeightsForBiome(textureData, biomePreset);
            
            // 繝舌う繧ｪ繝ｼ繝蝗ｺ譛峨・濶ｲ隱ｿ繧帝←逕ｨ
            ApplyBiomeColorModifications(textureData, biomePreset);
        }
        
        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝逕ｨ縺ｮ繝・け繧ｹ繝√Ε繧ｦ繧ｧ繧､繝医ｒ隱ｿ謨ｴ
        /// </summary>
        private void ModifyTextureWeightsForBiome(TerrainTextureData textureData, BiomePreset biomePreset)
        {
            // 繝舌う繧ｪ繝ｼ繝迚ｹ諤ｧ縺ｫ蝓ｺ縺･縺・※繧ｦ繧ｧ繧､繝医ｒ隱ｿ謨ｴ
            float moistureFactor = biomePreset.moisture;
            float temperatureFactor = biomePreset.temperature;
            float fertilityFactor = biomePreset.fertility;
            float rockinessFactor = biomePreset.rockiness;
            
            // 蜷・ユ繧ｯ繧ｹ繝√Ε繧ｿ繧､繝励・繧ｦ繧ｧ繧､繝医ｒ隱ｿ謨ｴ
            AdjustTextureWeightsByBiomeFactors(textureData, moistureFactor, temperatureFactor, fertilityFactor, rockinessFactor);
        }
        
        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝隕∝屏縺ｫ繧医ｋ繝・け繧ｹ繝√Ε繧ｦ繧ｧ繧､繝郁ｪｿ謨ｴ
        /// </summary>
        private void AdjustTextureWeightsByBiomeFactors(TerrainTextureData textureData, float moisture, float temperature, float fertility, float rockiness)
        {
            if (textureData.textureWeights == null)
                return;
            
            int resX = textureData.textureWeights.GetLength(0);
            int resY = textureData.textureWeights.GetLength(1);
            int blends = textureData.textureWeights.GetLength(2);
            
            for (int y = 0; y < resY; y++)
            {
                for (int x = 0; x < resX; x++)
                {
                    for (int b = 0; b < blends; b++)
                    {
                        var textureType = (TerrainTextureType)textureData.textureIndices[y, x, b];
                        float adjustmentFactor = CalculateBiomeAdjustmentFactor(textureType, moisture, temperature, fertility, rockiness);
                        
                        textureData.textureWeights[y, x, b] *= adjustmentFactor;
                    }
                }
            }
            
            // 繧ｦ繧ｧ繧､繝医ｒ蜀肴ｭ｣隕丞喧
            NormalizeTextureWeights(textureData);
        }
        
        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝隱ｿ謨ｴ菫よ焚繧定ｨ育ｮ・
        /// </summary>
        private float CalculateBiomeAdjustmentFactor(TerrainTextureType textureType, float moisture, float temperature, float fertility, float rockiness)
        {
            switch (textureType)
            {
                case TerrainTextureType.Grass:
                    return Mathf.Lerp(0.5f, 1.5f, moisture * fertility * (1f - rockiness));
                    
                case TerrainTextureType.Sand:
                    return Mathf.Lerp(0.3f, 1.2f, (1f - moisture) * temperature);
                    
                case TerrainTextureType.Rock:
                    return Mathf.Lerp(0.7f, 1.3f, rockiness);
                    
                case TerrainTextureType.Snow:
                    return Mathf.Lerp(0.1f, 1.5f, 1f - temperature);
                    
                case TerrainTextureType.Dirt:
                    return Mathf.Lerp(0.8f, 1.2f, fertility * (1f - rockiness));
                    
                case TerrainTextureType.Cliff:
                    return Mathf.Lerp(0.9f, 1.1f, rockiness);
                    
                default:
                    return 1f;
            }
        }
        
        /// <summary>
        /// 繝・け繧ｹ繝√Ε繧ｦ繧ｧ繧､繝医ｒ豁｣隕丞喧
        /// </summary>
        private void NormalizeTextureWeights(TerrainTextureData textureData)
        {
            if (textureData.textureWeights == null)
                return;
            
            int resX = textureData.textureWeights.GetLength(0);
            int resY = textureData.textureWeights.GetLength(1);
            int blends = textureData.textureWeights.GetLength(2);
            
            for (int y = 0; y < resY; y++)
            {
                for (int x = 0; x < resX; x++)
                {
                    float totalWeight = 0f;
                    for (int b = 0; b < blends; b++)
                    {
                        totalWeight += textureData.textureWeights[y, x, b];
                    }
                    
                    if (totalWeight > 0f)
                    {
                        for (int b = 0; b < blends; b++)
                        {
                            textureData.textureWeights[y, x, b] /= totalWeight;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 繝舌う繧ｪ繝ｼ繝濶ｲ隱ｿ螟画峩繧帝←逕ｨ
        /// </summary>
        private void ApplyBiomeColorModifications(TerrainTextureData textureData, BiomePreset biomePreset)
        {
            if (biomePreset.materialSettings != null)
            {
                textureData.colorModifier = biomePreset.materialSettings.terrainTint;
            }
        }
        #endregion

        #region 繝・け繧ｹ繝√Ε譖ｴ譁ｰ繧ｭ繝･繝ｼ蜃ｦ逅・
        /// <summary>
        /// 繝・け繧ｹ繝√Ε譖ｴ譁ｰ繧ｭ繝･繝ｼ繧貞・逅・
        /// </summary>
        private void ProcessTextureUpdateQueue()
        {
            int processedCount = 0;
            int maxUpdatesPerFrame = 3;
            
            while (textureUpdateQueue.Count > 0 && processedCount < maxUpdatesPerFrame)
            {
                var request = textureUpdateQueue.Dequeue();
                ProcessTextureUpdateRequest(request);
                processedCount++;
            }
        }
        
        /// <summary>
        /// 繝・け繧ｹ繝√Ε譖ｴ譁ｰ繝ｪ繧ｯ繧ｨ繧ｹ繝医ｒ蜃ｦ逅・
        /// </summary>
        private void ProcessTextureUpdateRequest(TextureUpdateRequest request)
        {
            if (request.tile == null || request.tile.tileObject == null)
                return;
            
            ApplyTextureToTile(request.tile);
        }
        #endregion

        #region 繝ｦ繝ｼ繝・ぅ繝ｪ繝・ぅ
        /// <summary>
        /// 霍晞屬縺ｫ蠢懊§縺櫚OD繝・け繧ｹ繝√Ε繧ｻ繝・ヨ繧貞叙蠕・
        /// </summary>
        private LODTextureSet GetLODTextureSetForDistance(float distance)
        {
            for (int i = 0; i < lodTextureSets.Count; i++)
            {
                if (distance <= lodTextureSets[i].maxDistance)
                {
                    return lodTextureSets[i];
                }
            }
            
            return lodTextureSets.LastOrDefault() ?? new LODTextureSet();
        }
        
        /// <summary>
        /// 繝・け繧ｹ繝√Ε繝・・繧ｿ繧偵け繝ｪ繝ｼ繝ｳ繧｢繝・・
        /// </summary>
        public void CleanupTextureData(TerrainTile tile)
        {
            if (tileTextureData.ContainsKey(tile))
            {
                var textureData = tileTextureData[tile];
                
                // 繝・け繧ｹ繝√Ε繧貞炎髯､
                if (textureData.weightTexture != null)
                {
                    Destroy(textureData.weightTexture);
                }
                if (textureData.blendTexture != null && textureData.blendTexture != textureData.weightTexture)
                {
                    Destroy(textureData.blendTexture);
                }
                
                tileTextureData.Remove(tile);
            }
        }
        #endregion
    }
}
