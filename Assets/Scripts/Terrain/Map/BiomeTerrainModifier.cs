using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Vastcore.Generation
{
    /// <summary>
    /// バイオーム地形修正システム
    /// 気候データに基づくバイオーム自動判定と地形特徴の修正を行う
    /// </summary>
    public class BiomeTerrainModifier : MonoBehaviour
    {
        [Header("バイオーム判定設定")]
        public bool enableAutomaticBiomeDetection = true;
        public float biomeTransitionDistance = 500f;
        public int biomeAnalysisResolution = 64;
        
        [Header("気候データ設定")]
        public ClimateDataGenerator climateGenerator;
        public bool useGlobalClimateData = true;
        public Vector2 globalTemperatureRange = new Vector2(-20f, 40f);
        public Vector2 globalMoistureRange = new Vector2(0f, 2000f);
        
        [Header("バイオーム定義")]
        public List<BiomeDefinition> biomeDefinitions = new List<BiomeDefinition>();
        
        [Header("デバッグ設定")]
        public bool showBiomeVisualization = false;
        public bool logBiomeDetection = false;
        
        // プライベートフィールド
        private Dictionary<BiomeType, BiomeDefinition> biomeMap;
        private float[,] temperatureMap;
        private float[,] moistureMap;
        private float[,] elevationMap;
        private BiomeType[,] biomeTypeMap;
        private bool isInitialized = false;
        
        // 依存コンポーネント
        private BiomePresetManager presetManager;
        
        // イベント
        public System.Action<Vector3, BiomeType> OnBiomeDetected;
        public System.Action<TerrainTile, BiomeType> OnBiomeApplied;
        
        #region Unity Lifecycle
        
        private void Start()
        {
            Initialize();
        }
        
        private void OnDrawGizmos()
        {
            if (showBiomeVisualization && isInitialized)
            {
                DrawBiomeVisualization();
            }
        }
        
        #endregion
        
        #region 初期化
        
        /// <summary>
        /// バイオーム地形修正システムを初期化
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;
            
            try
            {
                // 気候データ生成器の初期化
                if (climateGenerator == null)
                {
                    climateGenerator = gameObject.AddComponent<ClimateDataGenerator>();
                }
                climateGenerator.Initialize();
                
                // バイオーム定義の初期化
                InitializeBiomeDefinitions();
                
                // BiomePresetManagerの初期化
                if (presetManager == null)
                {
                    presetManager = FindFirstObjectByType<BiomePresetManager>();
                    if (presetManager == null)
                    {
                        presetManager = gameObject.AddComponent<BiomePresetManager>();
                    }
                }
                
                // バイオームマップの作成
                CreateBiomeMap();
                
                isInitialized = true;
                Debug.Log("BiomeTerrainModifier initialized successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"BiomeTerrainModifier initialization failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// デフォルトバイオーム定義を初期化
        /// </summary>
        private void InitializeBiomeDefinitions()
        {
            if (biomeDefinitions.Count == 0)
            {
                CreateDefaultBiomeDefinitions();
            }
            
            // バイオームマップの構築
            biomeMap = new Dictionary<BiomeType, BiomeDefinition>();
            foreach (var definition in biomeDefinitions)
            {
                if (!biomeMap.ContainsKey(definition.biomeType))
                {
                    biomeMap[definition.biomeType] = definition;
                }
            }
        }
        
        /// <summary>
        /// デフォルトバイオーム定義を作成
        /// </summary>
        private void CreateDefaultBiomeDefinitions()
        {
            biomeDefinitions.Clear();
            
            // 砂漠バイオーム
            biomeDefinitions.Add(new BiomeDefinition
            {
                biomeType = BiomeType.Desert,
                name = "砂漠",
                temperatureRange = new Vector2(25f, 45f),
                moistureRange = new Vector2(0f, 200f),
                elevationRange = new Vector2(-50f, 300f),
                terrainModifiers = new TerrainModificationData
                {
                    heightMultiplier = 0.6f,
                    roughnessMultiplier = 0.4f,
                    erosionStrength = 0.8f,
                    sedimentationRate = 0.3f,
                    enableDuneGeneration = true,
                    duneFrequency = 0.02f,
                    duneAmplitude = 15f
                }
            });
            
            // 森林バイオーム
            biomeDefinitions.Add(new BiomeDefinition
            {
                biomeType = BiomeType.Forest,
                name = "森林",
                temperatureRange = new Vector2(10f, 25f),
                moistureRange = new Vector2(800f, 2000f),
                elevationRange = new Vector2(0f, 800f),
                terrainModifiers = new TerrainModificationData
                {
                    heightMultiplier = 1.0f,
                    roughnessMultiplier = 0.7f,
                    erosionStrength = 0.3f,
                    sedimentationRate = 0.6f,
                    enableRidgeGeneration = true,
                    ridgeFrequency = 0.008f,
                    ridgeAmplitude = 25f
                }
            });
            
            // 山岳バイオーム
            biomeDefinitions.Add(new BiomeDefinition
            {
                biomeType = BiomeType.Mountain,
                name = "山岳",
                temperatureRange = new Vector2(-10f, 15f),
                moistureRange = new Vector2(400f, 1200f),
                elevationRange = new Vector2(500f, 3000f),
                terrainModifiers = new TerrainModificationData
                {
                    heightMultiplier = 2.5f,
                    roughnessMultiplier = 1.5f,
                    erosionStrength = 0.6f,
                    sedimentationRate = 0.2f,
                    enablePeakGeneration = true,
                    peakFrequency = 0.003f,
                    peakAmplitude = 100f
                }
            });
            
            // 海岸バイオーム
            biomeDefinitions.Add(new BiomeDefinition
            {
                biomeType = BiomeType.Coastal,
                name = "海岸",
                temperatureRange = new Vector2(15f, 30f),
                moistureRange = new Vector2(600f, 1500f),
                elevationRange = new Vector2(-10f, 100f),
                terrainModifiers = new TerrainModificationData
                {
                    heightMultiplier = 0.3f,
                    roughnessMultiplier = 0.5f,
                    erosionStrength = 0.9f,
                    sedimentationRate = 0.8f,
                    enableBeachGeneration = true,
                    beachWidth = 50f,
                    beachSlope = 0.1f
                }
            });
            
            // 極地バイオーム
            biomeDefinitions.Add(new BiomeDefinition
            {
                biomeType = BiomeType.Polar,
                name = "極地",
                temperatureRange = new Vector2(-30f, 5f),
                moistureRange = new Vector2(100f, 600f),
                elevationRange = new Vector2(0f, 1000f),
                terrainModifiers = new TerrainModificationData
                {
                    heightMultiplier = 0.8f,
                    roughnessMultiplier = 0.3f,
                    erosionStrength = 0.2f,
                    sedimentationRate = 0.1f,
                    enableGlacialGeneration = true,
                    glacialSmoothness = 0.9f,
                    glacialDepth = 20f
                }
            });
            
            // 草原バイオーム
            biomeDefinitions.Add(new BiomeDefinition
            {
                biomeType = BiomeType.Grassland,
                name = "草原",
                temperatureRange = new Vector2(5f, 20f),
                moistureRange = new Vector2(300f, 800f),
                elevationRange = new Vector2(0f, 500f),
                terrainModifiers = new TerrainModificationData
                {
                    heightMultiplier = 0.7f,
                    roughnessMultiplier = 0.4f,
                    erosionStrength = 0.4f,
                    sedimentationRate = 0.5f,
                    enableRollingHills = true,
                    hillFrequency = 0.01f,
                    hillAmplitude = 20f
                }
            });
        }
        
        #endregion
        
        #region バイオーム判定
        
        /// <summary>
        /// 指定位置のバイオームを判定
        /// </summary>
        public BiomeType DetectBiomeAtPosition(Vector3 worldPosition)
        {
            if (!isInitialized)
            {
                Debug.LogWarning("BiomeTerrainModifier not initialized");
                return BiomeType.Grassland;
            }
            
            try
            {
                // 気候データの取得
                var climateData = climateGenerator.GetClimateDataAtPosition(worldPosition);
                
                // 地形データの取得
                float elevation = GetElevationAtPosition(worldPosition);
                
                // バイオーム判定
                var biomeType = ClassifyBiome(climateData.temperature, climateData.moisture, elevation);
                
                if (logBiomeDetection)
                {
                    Debug.Log($"Biome detected at {worldPosition}: {biomeType} " +
                             $"(T:{climateData.temperature:F1}°C, M:{climateData.moisture:F0}mm, E:{elevation:F0}m)");
                }
                
                // イベント発火
                OnBiomeDetected?.Invoke(worldPosition, biomeType);
                
                return biomeType;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"DetectBiomeAtPosition failed: {e.Message}");
                return BiomeType.Grassland;
            }
        }
        
        /// <summary>
        /// 気候データに基づいてバイオームを分類
        /// </summary>
        private BiomeType ClassifyBiome(float temperature, float moisture, float elevation)
        {
            var candidates = new List<(BiomeType type, float score)>();
            
            foreach (var definition in biomeDefinitions)
            {
                float score = CalculateBiomeScore(definition, temperature, moisture, elevation);
                candidates.Add((definition.biomeType, score));
            }
            
            // 最高スコアのバイオームを選択
            var bestMatch = candidates.OrderByDescending(c => c.score).First();
            return bestMatch.type;
        }
        
        /// <summary>
        /// バイオーム適合度スコアを計算
        /// </summary>
        private float CalculateBiomeScore(BiomeDefinition definition, float temperature, float moisture, float elevation)
        {
            float tempScore = CalculateRangeScore(temperature, definition.temperatureRange);
            float moistureScore = CalculateRangeScore(moisture, definition.moistureRange);
            float elevationScore = CalculateRangeScore(elevation, definition.elevationRange);
            
            // 重み付き平均（温度と湿度を重視）
            return (tempScore * 0.4f + moistureScore * 0.4f + elevationScore * 0.2f);
        }
        
        /// <summary>
        /// 範囲内適合度スコアを計算
        /// </summary>
        private float CalculateRangeScore(float value, Vector2 range)
        {
            if (value >= range.x && value <= range.y)
            {
                // 範囲内の場合、中央に近いほど高スコア
                float center = (range.x + range.y) * 0.5f;
                float distance = Mathf.Abs(value - center);
                float maxDistance = (range.y - range.x) * 0.5f;
                return 1f - (distance / maxDistance);
            }
            else
            {
                // 範囲外の場合、距離に応じてスコア減少
                float distance = Mathf.Min(Mathf.Abs(value - range.x), Mathf.Abs(value - range.y));
                float rangeSize = range.y - range.x;
                return Mathf.Max(0f, 1f - (distance / rangeSize));
            }
        }
        
        /// <summary>
        /// 地形特徴からバイオームを推定
        /// </summary>
        public BiomeType EstimateBiomeFromTerrain(float[,] heightmap, Vector3 worldPosition)
        {
            if (heightmap == null)
            {
                return DetectBiomeAtPosition(worldPosition);
            }
            
            try
            {
                // 地形特徴の分析
                var terrainFeatures = AnalyzeTerrainFeatures(heightmap);
                
                // 気候データとの組み合わせ
                var climateData = climateGenerator.GetClimateDataAtPosition(worldPosition);
                
                // 地形特徴を考慮したバイオーム推定
                return EstimateBiomeFromFeatures(terrainFeatures, climateData);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"EstimateBiomeFromTerrain failed: {e.Message}");
                return BiomeType.Grassland;
            }
        }
        
        /// <summary>
        /// 地形特徴を分析
        /// </summary>
        private TerrainFeatures AnalyzeTerrainFeatures(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;
            float totalHeight = 0f;
            float totalSlope = 0f;
            int sampleCount = 0;
            
            // 高度と傾斜の分析
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float currentHeight = heightmap[x, y];
                    minHeight = Mathf.Min(minHeight, currentHeight);
                    maxHeight = Mathf.Max(maxHeight, currentHeight);
                    totalHeight += currentHeight;
                    
                    // 傾斜計算
                    float slopeX = heightmap[x + 1, y] - heightmap[x - 1, y];
                    float slopeY = heightmap[x, y + 1] - heightmap[x, y - 1];
                    float slope = Mathf.Sqrt(slopeX * slopeX + slopeY * slopeY);
                    totalSlope += slope;
                    
                    sampleCount++;
                }
            }
            
            return new TerrainFeatures
            {
                averageHeight = totalHeight / sampleCount,
                heightRange = maxHeight - minHeight,
                averageSlope = totalSlope / sampleCount,
                roughness = CalculateRoughness(heightmap)
            };
        }
        
        /// <summary>
        /// 地形の粗さを計算
        /// </summary>
        private float CalculateRoughness(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            float totalVariation = 0f;
            int sampleCount = 0;
            
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float center = heightmap[x, y];
                    float variation = 0f;
                    
                    // 8近傍との高度差を計算
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            variation += Mathf.Abs(heightmap[x + dx, y + dy] - center);
                        }
                    }
                    
                    totalVariation += variation / 8f;
                    sampleCount++;
                }
            }
            
            return totalVariation / sampleCount;
        }
        
        /// <summary>
        /// 地形特徴と気候データからバイオームを推定
        /// </summary>
        private BiomeType EstimateBiomeFromFeatures(TerrainFeatures features, ClimateData climate)
        {
            // 高度による判定
            if (features.averageHeight > 800f)
            {
                return BiomeType.Mountain;
            }
            
            if (features.averageHeight < 10f && climate.moisture > 1000f)
            {
                return BiomeType.Coastal;
            }
            
            // 温度による判定
            if (climate.temperature < 0f)
            {
                return BiomeType.Polar;
            }
            
            // 湿度による判定
            if (climate.moisture < 300f)
            {
                return BiomeType.Desert;
            }
            
            if (climate.moisture > 1000f && features.roughness > 0.5f)
            {
                return BiomeType.Forest;
            }
            
            // デフォルトは草原
            return BiomeType.Grassland;
        }
        
        #endregion
        
        #region 地形修正
        
        /// <summary>
        /// バイオームに基づいて地形を修正
        /// </summary>
        public void ApplyBiomeModifications(TerrainTile tile, BiomeType biomeType)
        {
            if (tile?.heightData == null || !biomeMap.ContainsKey(biomeType))
            {
                Debug.LogWarning($"Cannot apply biome modifications: invalid tile or biome type {biomeType}");
                return;
            }
            
            try
            {
                var definition = biomeMap[biomeType];
                var modifiers = definition.terrainModifiers;
                
                // 高度マップの修正
                ModifyHeightmap(tile.heightData, modifiers);
                
                // 特殊地形特徴の生成
                GenerateSpecialFeatures(tile, definition);
                
                // バイオーム情報の記録
                tile.appliedBiome = biomeType.ToString();
                
                // イベント発火
                OnBiomeApplied?.Invoke(tile, biomeType);
                
                if (logBiomeDetection)
                {
                    Debug.Log($"Applied biome modifications: {biomeType} to tile at {tile.coordinate}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ApplyBiomeModifications failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// ハイトマップを修正
        /// </summary>
        private void ModifyHeightmap(float[,] heightmap, TerrainModificationData modifiers)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float originalHeight = heightmap[x, y];
                    
                    // 基本的な高度修正
                    float modifiedHeight = originalHeight * modifiers.heightMultiplier;
                    
                    // 粗さの調整
                    if (modifiers.roughnessMultiplier != 1f)
                    {
                        float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) - 0.5f;
                        modifiedHeight += noise * modifiers.roughnessMultiplier * 10f;
                    }
                    
                    heightmap[x, y] = modifiedHeight;
                }
            }
            
            // 浸食効果の適用
            if (modifiers.erosionStrength > 0f)
            {
                ApplyErosion(heightmap, modifiers.erosionStrength);
            }
            
            // 堆積効果の適用
            if (modifiers.sedimentationRate > 0f)
            {
                ApplySedimentation(heightmap, modifiers.sedimentationRate);
            }
        }
        
        /// <summary>
        /// 浸食効果を適用
        /// </summary>
        private void ApplyErosion(float[,] heightmap, float strength)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            float[,] erosionMap = new float[width, height];
            
            // 簡単な浸食シミュレーション
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float center = heightmap[x, y];
                    float avgNeighbor = 0f;
                    int neighborCount = 0;
                    
                    // 8近傍の平均を計算
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            avgNeighbor += heightmap[x + dx, y + dy];
                            neighborCount++;
                        }
                    }
                    avgNeighbor /= neighborCount;
                    
                    // 浸食量の計算
                    float erosion = (center - avgNeighbor) * strength * 0.1f;
                    erosionMap[x, y] = erosion;
                }
            }
            
            // 浸食の適用
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    heightmap[x, y] -= erosionMap[x, y];
                }
            }
        }
        
        /// <summary>
        /// 堆積効果を適用
        /// </summary>
        private void ApplySedimentation(float[,] heightmap, float rate)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 低地への堆積シミュレーション
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float center = heightmap[x, y];
                    float minNeighbor = float.MaxValue;
                    
                    // 最低の近傍を見つける
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            minNeighbor = Mathf.Min(minNeighbor, heightmap[x + dx, y + dy]);
                        }
                    }
                    
                    // 低地の場合は堆積
                    if (center <= minNeighbor)
                    {
                        heightmap[x, y] += rate * 2f;
                    }
                }
            }
        }
        
        /// <summary>
        /// 特殊地形特徴を生成
        /// </summary>
        private void GenerateSpecialFeatures(TerrainTile tile, BiomeDefinition definition)
        {
            // バイオーム特有の地形生成を実行
            Vector3 worldPosition = tile.terrainObject != null ? tile.terrainObject.transform.position : Vector3.zero;
            BiomeSpecificTerrainGenerator.GenerateTerrainForBiome(tile.heightData, definition.biomeType, definition, worldPosition);
        }
        
        #endregion
        
        #region 特殊地形生成
        
        /// <summary>
        /// 砂丘を生成
        /// </summary>
        private void GenerateDunes(float[,] heightmap, float frequency, float amplitude)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float duneHeight = Mathf.Sin(x * frequency) * Mathf.Cos(y * frequency * 0.7f) * amplitude;
                    duneHeight = Mathf.Max(0f, duneHeight); // 正の値のみ
                    heightmap[x, y] += duneHeight;
                }
            }
        }
        
        /// <summary>
        /// 尾根を生成
        /// </summary>
        private void GenerateRidges(float[,] heightmap, float frequency, float amplitude)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float ridgeNoise = Mathf.PerlinNoise(x * frequency, y * frequency);
                    if (ridgeNoise > 0.6f) // 尾根の閾値
                    {
                        float ridgeHeight = (ridgeNoise - 0.6f) * amplitude * 2.5f;
                        heightmap[x, y] += ridgeHeight;
                    }
                }
            }
        }
        
        /// <summary>
        /// 山頂を生成
        /// </summary>
        private void GeneratePeaks(float[,] heightmap, float frequency, float amplitude)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 複数の山頂を配置
            int peakCount = Mathf.RoundToInt(width * height * frequency);
            
            for (int i = 0; i < peakCount; i++)
            {
                int peakX = Random.Range(0, width);
                int peakY = Random.Range(0, height);
                float peakRadius = Random.Range(20f, 50f);
                
                // 山頂周辺の高度を上げる
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(peakX, peakY));
                        if (distance < peakRadius)
                        {
                            float falloff = 1f - (distance / peakRadius);
                            falloff = falloff * falloff; // 二次関数的な減衰
                            heightmap[x, y] += falloff * amplitude;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 海岸線を生成
        /// </summary>
        private void GenerateBeach(float[,] heightmap, float beachWidth, float beachSlope)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 海岸線の検出と修正
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (heightmap[x, y] < 5f) // 海面近くの場合
                    {
                        // 海岸線からの距離を計算
                        float distanceFromWater = CalculateDistanceFromWater(heightmap, x, y);
                        
                        if (distanceFromWater < beachWidth)
                        {
                            float beachHeight = distanceFromWater * beachSlope;
                            heightmap[x, y] = Mathf.Max(heightmap[x, y], beachHeight);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 氷河地形を生成
        /// </summary>
        private void GenerateGlacialTerrain(float[,] heightmap, float smoothness, float depth)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 氷河による平滑化
            float[,] smoothedMap = new float[width, height];
            
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float sum = 0f;
                    int count = 0;
                    
                    // 広範囲の平均を計算
                    int radius = Mathf.RoundToInt(smoothness * 5f);
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        for (int dy = -radius; dy <= radius; dy++)
                        {
                            int nx = x + dx;
                            int ny = y + dy;
                            
                            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                            {
                                sum += heightmap[nx, ny];
                                count++;
                            }
                        }
                    }
                    
                    smoothedMap[x, y] = sum / count;
                }
            }
            
            // 平滑化の適用
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    heightmap[x, y] = Mathf.Lerp(heightmap[x, y], smoothedMap[x, y], smoothness);
                    heightmap[x, y] -= depth; // 氷河による削り取り
                }
            }
        }
        
        /// <summary>
        /// 起伏のある丘陵を生成
        /// </summary>
        private void GenerateRollingHills(float[,] heightmap, float frequency, float amplitude)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float hillNoise1 = Mathf.PerlinNoise(x * frequency, y * frequency);
                    float hillNoise2 = Mathf.PerlinNoise(x * frequency * 2f, y * frequency * 2f) * 0.5f;
                    float hillHeight = (hillNoise1 + hillNoise2) * amplitude;
                    
                    heightmap[x, y] += hillHeight;
                }
            }
        }
        
        #endregion
        
        #region ユーティリティ
        
        /// <summary>
        /// 指定位置の標高を取得
        /// </summary>
        private float GetElevationAtPosition(Vector3 worldPosition)
        {
            // 地形からの高度取得（簡易実装）
            // 実際の実装では RuntimeTerrainManager から取得
            return worldPosition.y;
        }
        
        /// <summary>
        /// 水面からの距離を計算
        /// </summary>
        private float CalculateDistanceFromWater(float[,] heightmap, int x, int y)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            float minDistance = float.MaxValue;
            
            // 周辺の水面を検索
            for (int dx = -20; dx <= 20; dx++)
            {
                for (int dy = -20; dy <= 20; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        if (heightmap[nx, ny] <= 0f) // 水面
                        {
                            float distance = Vector2.Distance(new Vector2(x, y), new Vector2(nx, ny));
                            minDistance = Mathf.Min(minDistance, distance);
                        }
                    }
                }
            }
            return minDistance == float.MaxValue ? 100f : minDistance;
        }
        
        /// <summary>
        /// バイオームタイプに対応するプリセットを取得
        /// </summary>
        private BiomePreset GetBiomePreset(BiomeType biomeType)
        {
            if (presetManager == null) return null;
            
            string presetName = biomeType.ToString();
            return presetManager.GetPreset(presetName);
        }
        
        /// <summary>
        /// バイオームマップを作成
        /// </summary>
        private void CreateBiomeMap()
        {
            // バイオームマップの構築
            biomeMap = new Dictionary<BiomeType, BiomeDefinition>();
            foreach (var definition in biomeDefinitions)
            {
                if (!biomeMap.ContainsKey(definition.biomeType))
                {
                    biomeMap[definition.biomeType] = definition;
                }
            }
        }
        
        /// <summary>
        /// 大規模バイオームマップを作成
        /// </summary>
        private void CreateLargeBiomeMap()
        {
            // 将来的な実装: 大規模なバイオームマップの生成
            // 現在は動的判定を使用
        }
        
        /// <summary>
        /// バイオーム可視化を描画
        /// </summary>
        private void DrawBiomeVisualization()
        {
            if (biomeTypeMap == null) return;
            
            int width = biomeTypeMap.GetLength(0);
            int height = biomeTypeMap.GetLength(1);
            
            for (int x = 0; x < width; x += 10)
            {
                for (int y = 0; y < height; y += 10)
                {
                    Vector3 worldPos = new Vector3(x * 10f, 0f, y * 10f);
                    Color biomeColor = GetBiomeColor(biomeTypeMap[x, y]);
                    
                    Gizmos.color = biomeColor;
                    Gizmos.DrawCube(worldPos, Vector3.one * 5f);
                }
            }
        }
        
        /// <summary>
        /// バイオームタイプに対応する色を取得
        /// </summary>
        private Color GetBiomeColor(BiomeType biomeType)
        {
            switch (biomeType)
            {
                case BiomeType.Desert: return Color.yellow;
                case BiomeType.Forest: return Color.green;
                case BiomeType.Mountain: return Color.gray;
                case BiomeType.Coastal: return Color.cyan;
                case BiomeType.Polar: return Color.white;
                case BiomeType.Grassland: return Color.green * 0.7f;
                default: return Color.magenta;
            }
        }
        
        #endregion
    }
    
    #region データ構造
    
    /// <summary>
    /// バイオームタイプ列挙
    /// </summary>
    public enum BiomeType
    {
        Desert,     // 砂漠
        Forest,     // 森林
        Mountain,   // 山岳
        Coastal,    // 海岸
        Polar,      // 極地
        Grassland   // 草原
    }
    
    /// <summary>
    /// バイオーム定義
    /// </summary>
    [System.Serializable]
    public class BiomeDefinition
    {
        public BiomeType biomeType;
        public string name;
        public Vector2 temperatureRange;    // 温度範囲（摂氏）
        public Vector2 moistureRange;       // 湿度範囲（mm/年）
        public Vector2 elevationRange;      // 標高範囲（m）
        public TerrainModificationData terrainModifiers;
    }
    
    /// <summary>
    /// 地形修正データ
    /// </summary>
    [System.Serializable]
    public class TerrainModificationData
    {
        [Header("基本修正")]
        public float heightMultiplier = 1f;
        public float roughnessMultiplier = 1f;
        public float erosionStrength = 0f;
        public float sedimentationRate = 0f;
        
        [Header("砂丘生成")]
        public bool enableDuneGeneration = false;
        public float duneFrequency = 0.02f;
        public float duneAmplitude = 15f;
        
        [Header("尾根生成")]
        public bool enableRidgeGeneration = false;
        public float ridgeFrequency = 0.008f;
        public float ridgeAmplitude = 25f;
        
        [Header("山頂生成")]
        public bool enablePeakGeneration = false;
        public float peakFrequency = 0.003f;
        public float peakAmplitude = 100f;
        
        [Header("海岸生成")]
        public bool enableBeachGeneration = false;
        public float beachWidth = 50f;
        public float beachSlope = 0.1f;
        
        [Header("氷河地形")]
        public bool enableGlacialGeneration = false;
        public float glacialSmoothness = 0.9f;
        public float glacialDepth = 20f;
        
        [Header("丘陵地形")]
        public bool enableRollingHills = false;
        public float hillFrequency = 0.01f;
        public float hillAmplitude = 20f;
    }
    
    /// <summary>
    /// 地形特徴データ
    /// </summary>
    public struct TerrainFeatures
    {
        public float averageHeight;
        public float heightRange;
        public float averageSlope;
        public float roughness;
    }
    
    #endregion
}