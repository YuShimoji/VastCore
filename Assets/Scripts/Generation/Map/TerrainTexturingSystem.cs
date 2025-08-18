using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Generation
{
    /// <summary>
    /// 地形テクスチャリングシステム
    /// 要求1.5, 2.1: 高度・傾斜に応じた自動テクスチャリングとバイオーム設定
    /// </summary>
    public class TerrainTexturingSystem : MonoBehaviour
    {
        #region 設定パラメータ
        [Header("高度ベーステクスチャリング")]
        public List<AltitudeTextureLayer> altitudeLayers = new List<AltitudeTextureLayer>();
        public AnimationCurve altitudeBlendCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public float altitudeBlendSmoothness = 0.1f;
        
        [Header("傾斜ベーステクスチャリング")]
        public List<SlopeTextureLayer> slopeLayers = new List<SlopeTextureLayer>();
        public AnimationCurve slopeBlendCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public float slopeBlendSmoothness = 5f;
        
        [Header("動的マテリアルブレンディング")]
        public bool enableDynamicBlending = true;
        public float blendTransitionSpeed = 2f;
        public int maxTextureBlends = 4;
        
        [Header("LODテクスチャシステム")]
        public bool enableLODTextures = true;
        public float[] lodDistances = { 500f, 1000f, 2000f };
        public List<LODTextureSet> lodTextureSets = new List<LODTextureSet>();
        #endregion

        #region プライベート変数
        private Dictionary<TerrainTile, TerrainTextureData> tileTextureData = new Dictionary<TerrainTile, TerrainTextureData>();
        private Queue<TextureUpdateRequest> textureUpdateQueue = new Queue<TextureUpdateRequest>();
        private Transform playerTransform;
        private MaterialPropertyBlock materialPropertyBlock;
        private Shader terrainShader;
        #endregion

        #region Unity イベント
        void Start()
        {
            InitializeTexturingSystem();
        }
        
        void Update()
        {
            ProcessTextureUpdateQueue();
        }
        #endregion

        #region 初期化
        /// <summary>
        /// テクスチャリングシステムを初期化
        /// </summary>
        private void InitializeTexturingSystem()
        {
            Debug.Log("Initializing TerrainTexturingSystem...");
            
            // プレイヤーTransformを取得
            var playerController = FindObjectOfType<AdvancedPlayerController>();
            if (playerController != null)
            {
                playerTransform = playerController.transform;
            }
            
            // MaterialPropertyBlockを初期化
            materialPropertyBlock = new MaterialPropertyBlock();
            
            // 地形シェーダーを取得
            terrainShader = Shader.Find("Standard");
            
            // デフォルト設定を初期化
            InitializeDefaultSettings();
            
            Debug.Log("TerrainTexturingSystem initialized successfully");
        }
        
        /// <summary>
        /// デフォルト設定を初期化
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

        #region パブリックAPI
        /// <summary>
        /// 地形タイルにテクスチャを適用
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
        /// バイオームプリセットに基づいてテクスチャを適用
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

        // 残りのメソッドは次のファイルで実装
    }
}       
 #region デフォルト設定作成
        /// <summary>
        /// デフォルト高度レイヤーを作成
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
        /// デフォルト傾斜レイヤーを作成
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
        /// デフォルトLODテクスチャセットを作成
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

        #region テクスチャ生成
        /// <summary>
        /// タイル用のテクスチャデータを生成
        /// </summary>
        private TerrainTextureData GenerateTextureDataForTile(TerrainTile tile)
        {
            var textureData = new TerrainTextureData();
            
            if (tile.heightmap == null)
                return textureData;
            
            int resolution = tile.heightmap.GetLength(0);
            textureData.textureWeights = new float[resolution, resolution, maxTextureBlends];
            textureData.textureIndices = new int[resolution, resolution, maxTextureBlends];
            
            // 各ピクセルのテクスチャウェイトを計算
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    var weights = CalculateTextureWeightsAtPosition(tile, x, y, resolution);
                    
                    // 上位4つのテクスチャを選択
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
        /// バイオーム用のテクスチャデータを生成
        /// </summary>
        private TerrainTextureData GenerateBiomeTextureData(TerrainTile tile, BiomePreset biomePreset)
        {
            var textureData = GenerateTextureDataForTile(tile);
            
            // バイオーム特性を適用
            ApplyBiomeModifications(textureData, biomePreset);
            
            return textureData;
        }
        
        /// <summary>
        /// 指定位置でのテクスチャウェイトを計算
        /// </summary>
        private List<TextureWeight> CalculateTextureWeightsAtPosition(TerrainTile tile, int x, int y, int resolution)
        {
            var weights = new List<TextureWeight>();
            
            // 高度を取得
            float height = tile.heightmap[y, x] * tile.terrainParams.maxHeight;
            
            // 傾斜を計算
            float slope = CalculateSlopeAtPosition(tile.heightmap, x, y, resolution);
            
            // 高度ベースのテクスチャウェイトを計算
            foreach (var layer in altitudeLayers)
            {
                float weight = CalculateAltitudeWeight(height, layer);
                if (weight > 0f)
                {
                    weights.Add(new TextureWeight { textureType = layer.textureType, weight = weight * layer.blendStrength });
                }
            }
            
            // 傾斜ベースのテクスチャウェイトを計算
            foreach (var layer in slopeLayers)
            {
                float weight = CalculateSlopeWeight(slope, layer);
                if (weight > 0f)
                {
                    var textureWeight = new TextureWeight { textureType = layer.textureType, weight = weight * layer.blendStrength };
                    
                    if (layer.overrideAltitude)
                    {
                        // 傾斜テクスチャが高度テクスチャを上書き
                        weights.RemoveAll(w => w.textureType != layer.textureType);
                        weights.Add(textureWeight);
                    }
                    else
                    {
                        weights.Add(textureWeight);
                    }
                }
            }
            
            // ウェイトを正規化
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
        /// 高度ウェイトを計算
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
        /// 傾斜ウェイトを計算
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
        /// 指定位置での傾斜を計算
        /// </summary>
        private float CalculateSlopeAtPosition(float[,] heightmap, int x, int y, int resolution)
        {
            // 隣接ピクセルの高度差から傾斜を計算
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

        #region テクスチャ適用
        /// <summary>
        /// テクスチャデータをタイルに適用
        /// </summary>
        private void ApplyTextureDataToTile(TerrainTile tile, TerrainTextureData textureData)
        {
            if (tile.tileObject == null)
                return;
            
            var meshRenderer = tile.tileObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                return;
            
            // LODレベルに応じたテクスチャセットを選択
            var lodSet = GetLODTextureSetForDistance(tile.distanceFromPlayer);
            
            // マテリアルを作成または更新
            Material material = CreateOrUpdateMaterial(tile, textureData, lodSet);
            meshRenderer.material = material;
        }
        
        /// <summary>
        /// マテリアルを作成または更新
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
            
            // LOD設定を適用
            material.SetFloat("_Glossiness", lodSet.detailStrength * 0.5f);
            
            // 高度に基づく色調整
            ApplyAltitudeBasedColoring(material, tile);
            
            return material;
        }
        
        /// <summary>
        /// 高度に基づく色調整を適用
        /// </summary>
        private void ApplyAltitudeBasedColoring(Material material, TerrainTile tile)
        {
            if (tile.heightmap == null)
                return;
            
            // 平均高度を計算
            float averageHeight = CalculateAverageHeight(tile.heightmap) * tile.terrainParams.maxHeight;
            
            // 高度に基づいて色を調整
            Color baseColor = Color.white;
            
            if (averageHeight < 10f)
            {
                baseColor = Color.Lerp(Color.blue, Color.yellow, 0.3f); // 水辺
            }
            else if (averageHeight < 50f)
            {
                baseColor = Color.Lerp(Color.green, Color.yellow, 0.2f); // 低地
            }
            else if (averageHeight < 150f)
            {
                baseColor = Color.Lerp(Color.gray, Color.brown, 0.5f); // 高地
            }
            else
            {
                baseColor = Color.Lerp(Color.white, Color.gray, 0.3f); // 山地
            }
            
            material.color = baseColor;
        }
        
        /// <summary>
        /// 平均高度を計算
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

        #region バイオーム処理
        /// <summary>
        /// バイオーム変更をテクスチャデータに適用
        /// </summary>
        private void ApplyBiomeModifications(TerrainTextureData textureData, BiomePreset biomePreset)
        {
            // バイオーム特性に基づいてテクスチャウェイトを調整
            ModifyTextureWeightsForBiome(textureData, biomePreset);
            
            // バイオーム固有の色調を適用
            ApplyBiomeColorModifications(textureData, biomePreset);
        }
        
        /// <summary>
        /// バイオーム用のテクスチャウェイトを調整
        /// </summary>
        private void ModifyTextureWeightsForBiome(TerrainTextureData textureData, BiomePreset biomePreset)
        {
            // バイオーム特性に基づいてウェイトを調整
            float moistureFactor = biomePreset.moisture;
            float temperatureFactor = biomePreset.temperature;
            float fertilityFactor = biomePreset.fertility;
            float rockinessFactor = biomePreset.rockiness;
            
            // 各テクスチャタイプのウェイトを調整
            AdjustTextureWeightsByBiomeFactors(textureData, moistureFactor, temperatureFactor, fertilityFactor, rockinessFactor);
        }
        
        /// <summary>
        /// バイオーム要因によるテクスチャウェイト調整
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
            
            // ウェイトを再正規化
            NormalizeTextureWeights(textureData);
        }
        
        /// <summary>
        /// バイオーム調整係数を計算
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
        /// テクスチャウェイトを正規化
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
        /// バイオーム色調変更を適用
        /// </summary>
        private void ApplyBiomeColorModifications(TerrainTextureData textureData, BiomePreset biomePreset)
        {
            if (biomePreset.materialSettings != null)
            {
                textureData.colorModifier = biomePreset.materialSettings.terrainTint;
            }
        }
        #endregion

        #region テクスチャ更新キュー処理
        /// <summary>
        /// テクスチャ更新キューを処理
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
        /// テクスチャ更新リクエストを処理
        /// </summary>
        private void ProcessTextureUpdateRequest(TextureUpdateRequest request)
        {
            if (request.tile == null || request.tile.tileObject == null)
                return;
            
            ApplyTextureToTile(request.tile);
        }
        #endregion

        #region ユーティリティ
        /// <summary>
        /// 距離に応じたLODテクスチャセットを取得
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
        /// テクスチャデータをクリーンアップ
        /// </summary>
        public void CleanupTextureData(TerrainTile tile)
        {
            if (tileTextureData.ContainsKey(tile))
            {
                var textureData = tileTextureData[tile];
                
                // テクスチャを削除
                if (textureData.weightTexture != null)
                {
                    DestroyImmediate(textureData.weightTexture);
                }
                if (textureData.blendTexture != null && textureData.blendTexture != textureData.weightTexture)
                {
                    DestroyImmediate(textureData.blendTexture);
                }
                
                tileTextureData.Remove(tile);
            }
        }
        #endregion
    }
}