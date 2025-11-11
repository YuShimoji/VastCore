using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    /// <summary>
    /// バイオーム特有地形生成システム
    /// デザイナーテンプレートとプロシージャル生成を組み合わせたハイブリッドアプローチ
    /// </summary>
    public static class BiomeSpecificTerrainGenerator
    {
        #region デザイナーテンプレート管理

        /// <summary>
        /// バイオームごとのデザイナーテンプレートライブラリ
        /// </summary>
        private static readonly Dictionary<BiomeType, List<DesignerTerrainTemplate>> templateLibraries = new Dictionary<BiomeType, List<DesignerTerrainTemplate>>();

        /// <summary>
        /// テンプレートをライブラリに登録
        /// </summary>
        public static void RegisterTemplate(DesignerTerrainTemplate template)
        {
            if (template == null) return;

            if (!templateLibraries.ContainsKey(template.associatedBiome))
            {
                templateLibraries[template.associatedBiome] = new List<DesignerTerrainTemplate>();
            }

            if (!templateLibraries[template.associatedBiome].Contains(template))
            {
                templateLibraries[template.associatedBiome].Add(template);
            }
        }

        /// <summary>
        /// バイオームに適したテンプレートを取得
        /// </summary>
        public static DesignerTerrainTemplate GetRandomTemplateForBiome(BiomeType biomeType, Vector3 worldPosition)
        {
            if (!templateLibraries.ContainsKey(biomeType) || templateLibraries[biomeType].Count == 0)
            {
                return null;
            }

            var templates = templateLibraries[biomeType];
            int seed = Mathf.FloorToInt(worldPosition.x + worldPosition.z);
            System.Random random = new System.Random(seed);

            return templates[random.Next(templates.Count)];
        }

        #endregion

        #region メイン生成メソッド

        /// <summary>
        /// バイオームタイプに応じた地形生成を実行
        /// デザイナーテンプレート優先、フォールバックでプロシージャル生成
        /// </summary>
        public static void GenerateTerrainForBiome(float[,] heightmap, BiomeType biomeType, BiomeDefinition biomeDefinition, Vector3 worldPosition)
        {
            // デザイナーテンプレートベースの生成を試行
            bool templateApplied = TryApplyDesignerTemplates(heightmap, biomeType, worldPosition);

            // テンプレートが適用できなかった場合のみプロシージャル生成
            if (!templateApplied)
            {
                GenerateProceduralTerrain(heightmap, biomeType, biomeDefinition, worldPosition);
            }

            // 共通の後処理
            ApplyBiomePostProcessing(heightmap, biomeDefinition, worldPosition);
        }

        /// <summary>
        /// デザイナーテンプレートの適用を試行
        /// </summary>
        private static bool TryApplyDesignerTemplates(float[,] heightmap, BiomeType biomeType, Vector3 worldPosition)
        {
            var template = GetRandomTemplateForBiome(biomeType, worldPosition);

            if (template != null && template.CanApplyAt(worldPosition, GetTerrainHeightAt(worldPosition), GetTerrainSlopeAt(worldPosition)))
            {
                // テンプレートベース生成を実行
                float seed = worldPosition.x * 0.01f + worldPosition.z * 0.01f;
                TerrainSynthesizer.SynthesizeTerrain(heightmap, template, worldPosition, seed);
                return true;
            }

            return false;
        }

        /// <summary>
        /// プロシージャル地形生成（フォールバック）
        /// </summary>
        private static void GenerateProceduralTerrain(float[,] heightmap, BiomeType biomeType, BiomeDefinition biomeDefinition, Vector3 worldPosition)
        {
            switch (biomeType)
            {
                case BiomeType.Desert:
                    GenerateDesertTerrain(heightmap, biomeDefinition, worldPosition);
                    break;
                case BiomeType.Forest:
                    GenerateForestTerrain(heightmap, biomeDefinition, worldPosition);
                    break;
                case BiomeType.Mountain:
                    GenerateMountainTerrain(heightmap, biomeDefinition, worldPosition);
                    break;
                case BiomeType.Coastal:
                    GenerateCoastalTerrain(heightmap, biomeDefinition, worldPosition);
                    break;
                case BiomeType.Polar:
                    GeneratePolarTerrain(heightmap, biomeDefinition, worldPosition);
                    break;
                case BiomeType.Grassland:
                    GenerateGrasslandTerrain(heightmap, biomeDefinition, worldPosition);
                    break;
                default:
                    Debug.LogWarning($"Unknown biome type: {biomeType}");
                    break;
            }
        }

        /// <summary>
        /// 地形修正を適用
        /// </summary>
        private static void ApplyTerrainModifiers(float[,] heightmap, TerrainModificationData modifiers)
        {
            if (modifiers == null) return;

            // 高度乗数を適用
            if (modifiers.heightMultiplier != 1f)
            {
                ApplyHeightMultiplier(heightmap, modifiers.heightMultiplier);
            }

            // 粗さ調整
            if (modifiers.roughnessMultiplier != 1f)
            {
                ApplyRoughness(heightmap, modifiers.roughnessMultiplier);
            }

            // 特殊地形生成
            if (modifiers.enableDuneGeneration)
            {
                GenerateDunesForBiome(heightmap, modifiers);
            }

            if (modifiers.enableRidgeGeneration)
            {
                GenerateRidgesForBiome(heightmap, modifiers);
            }

            if (modifiers.enablePeakGeneration)
            {
                GeneratePeaksForBiome(heightmap, modifiers);
            }

            if (modifiers.enableBeachGeneration)
            {
                GenerateBeachForBiome(heightmap, modifiers);
            }

            if (modifiers.enableGlacialGeneration)
            {
                GenerateGlacialTerrainForBiome(heightmap, modifiers);
            }

            if (modifiers.enableRollingHills)
            {
                GenerateRollingHillsForBiome(heightmap, modifiers);
            }

            // 浸食・堆積
            if (modifiers.erosionStrength > 0f)
            {
                ApplyErosion(heightmap, modifiers.erosionStrength);
            }

            if (modifiers.sedimentationRate > 0f)
            {
                ApplySedimentation(heightmap, modifiers.sedimentationRate);
            }
        }

        /// <summary>
        /// 高度乗数を適用
        /// </summary>
        private static void ApplyHeightMultiplier(float[,] heightmap, float multiplier)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    heightmap[x, y] *= multiplier;
                }
            }
        }

        /// <summary>
        /// 粗さを適用
        /// </summary>
        private static void ApplyRoughness(float[,] heightmap, float roughnessMultiplier)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) - 0.5f;
                    heightmap[x, y] += noise * roughnessMultiplier * 10f;
                }
            }
        }
        #endregion

        #region ユーティリティメソッド

        /// <summary>
        /// 指定位置の地形高度を取得（簡易実装）
        /// </summary>
        private static float GetTerrainHeightAt(Vector3 worldPosition)
        {
            // 実際の実装ではTerrain.SampleHeightを使用
            return worldPosition.y;
        }

        /// <summary>
        /// 指定位置の地形傾斜を取得（簡易実装）
        /// </summary>
        private static float GetTerrainSlopeAt(Vector3 worldPosition)
        {
            // 実際の実装ではTerrain.SampleSlopeを使用
            return 0f;
        }

        /// <summary>
        /// 浸食を適用
        /// </summary>
        private static void ApplyErosion(float[,] heightmap, float strength)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float currentHeight = heightmap[x, y];
                    float avgNeighbor = (
                        heightmap[x-1, y] + heightmap[x+1, y] +
                        heightmap[x, y-1] + heightmap[x, y+1]
                    ) / 4f;

                    if (currentHeight > avgNeighbor)
                    {
                        float erosion = (currentHeight - avgNeighbor) * strength * 0.1f;
                        heightmap[x, y] -= erosion;
                    }
                }
            }
        }

        /// <summary>
        /// 堆積を適用
        /// </summary>
        private static void ApplySedimentation(float[,] heightmap, float rate)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float currentHeight = heightmap[x, y];
                    float avgNeighbor = (
                        heightmap[x-1, y] + heightmap[x+1, y] +
                        heightmap[x, y-1] + heightmap[x, y+1]
                    ) / 4f;

                    if (currentHeight < avgNeighbor)
                    {
                        float deposition = (avgNeighbor - currentHeight) * rate * 0.1f;
                        heightmap[x, y] += deposition;
                    }
                }
            }
        }

        #endregion

        #region 特殊地形生成メソッド

        /// <summary>
        /// バイオーム用の砂丘生成
        /// </summary>
        private static void GenerateDunesForBiome(float[,] heightmap, TerrainModificationData modifiers)
        {
            Vector3 worldPosition = Vector3.zero; // 仮定値
            GenerateDuneSystems(heightmap, worldPosition);
        }

        /// <summary>
        /// バイオーム用の尾根生成
        /// </summary>
        private static void GenerateRidgesForBiome(float[,] heightmap, TerrainModificationData modifiers)
        {
            Vector3 worldPosition = Vector3.zero;
            GenerateForestRidges(heightmap, worldPosition);
        }

        /// <summary>
        /// バイオーム用の山頂生成
        /// </summary>
        private static void GeneratePeaksForBiome(float[,] heightmap, TerrainModificationData modifiers)
        {
            Vector3 worldPosition = Vector3.zero;
            GenerateMountainPeaks(heightmap, worldPosition);
        }

        /// <summary>
        /// バイオーム用の海岸生成
        /// </summary>
        private static void GenerateBeachForBiome(float[,] heightmap, TerrainModificationData modifiers)
        {
            GenerateCoastalTerrain(heightmap, null, Vector3.zero);
        }

        /// <summary>
        /// バイオーム用の氷河地形生成
        /// </summary>
        private static void GenerateGlacialTerrainForBiome(float[,] heightmap, TerrainModificationData modifiers)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            // 簡易的な氷河平滑化
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float avg = (
                        heightmap[x-1, y] + heightmap[x+1, y] +
                        heightmap[x, y-1] + heightmap[x, y+1]
                    ) / 4f;

                    heightmap[x, y] = Mathf.Lerp(heightmap[x, y], avg, modifiers.glacialSmoothness);
                    heightmap[x, y] -= modifiers.glacialDepth;
                }
            }
        }

        /// <summary>
        /// バイオーム用の起伏丘陵生成
        /// </summary>
        private static void GenerateRollingHillsForBiome(float[,] heightmap, TerrainModificationData modifiers)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float hillNoise1 = Mathf.PerlinNoise(x * modifiers.hillFrequency, y * modifiers.hillFrequency);
                    float hillNoise2 = Mathf.PerlinNoise(x * modifiers.hillFrequency * 2f, y * modifiers.hillFrequency * 2f) * 0.5f;
                    float hillHeight = (hillNoise1 + hillNoise2) * modifiers.hillAmplitude;

                    heightmap[x, y] += hillHeight;
                }
            }
        }

        #endregion
        
        #region 砂漠地形生成
        
        /// <summary>
        /// 砂漠地形を生成（砂丘、オアシス、岩石露出）
        /// </summary>
        public static void GenerateDesertTerrain(float[,] heightmap, BiomeDefinition biomeDefinition, Vector3 worldPosition)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 砂丘システムの生成
            GenerateDuneSystems(heightmap, worldPosition);
            
            // 岩石露出の生成
            GenerateRockOutcrops(heightmap, worldPosition, 0.05f);
            
            // オアシスの生成
            GenerateOases(heightmap, worldPosition, 0.02f);
            
            // 砂漠特有の浸食パターン
            ApplyDesertErosion(heightmap, biomeDefinition.terrainModifiers.erosionStrength);
            
            // 風による砂の堆積
            ApplyWindDeposition(heightmap, worldPosition);
        }
        
        /// <summary>
        /// 砂丘システムを生成
        /// </summary>
        private static void GenerateDuneSystems(float[,] heightmap, Vector3 worldPosition)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 主要風向き（西から東へ）
            Vector2 windDirection = new Vector2(1f, 0.2f).normalized;
            
            // 複数の砂丘チェーンを生成
            int duneChainCount = Random.Range(3, 7);
            
            for (int chain = 0; chain < duneChainCount; chain++)
            {
                // 砂丘チェーンの開始点
                Vector2 chainStart = new Vector2(
                    Random.Range(0f, width * 0.3f),
                    Random.Range(0f, height)
                );
                
                // 砂丘の数
                int dunesInChain = Random.Range(5, 12);
                float duneSpacing = Random.Range(80f, 150f);
                
                for (int dune = 0; dune < dunesInChain; dune++)
                {
                    Vector2 duneCenter = chainStart + windDirection * (dune * duneSpacing);
                    
                    // 境界チェック
                    if (duneCenter.x < 0 || duneCenter.x >= width || duneCenter.y < 0 || duneCenter.y >= height)
                        continue;
                    
                    // 砂丘の形状パラメータ
                    float duneHeight = Random.Range(15f, 35f);
                    float duneWidth = Random.Range(60f, 120f);
                    float duneLength = Random.Range(100f, 200f);
                    
                    // 非対称な砂丘形状（風上側は緩やか、風下側は急）
                    GenerateAsymmetricDune(heightmap, duneCenter, duneHeight, duneWidth, duneLength, windDirection);
                }
            }
        }
        
        /// <summary>
        /// 非対称砂丘を生成
        /// </summary>
        private static void GenerateAsymmetricDune(float[,] heightmap, Vector2 center, float height, float width, float length, Vector2 windDirection)
        {
            int mapWidth = heightmap.GetLength(0);
            int mapHeight = heightmap.GetLength(1);
            
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    Vector2 point = new Vector2(x, y);
                    Vector2 offset = point - center;
                    
                    // 風向きに対する相対位置
                    float alongWind = Vector2.Dot(offset, windDirection);
                    float acrossWind = Vector2.Dot(offset, new Vector2(-windDirection.y, windDirection.x));
                    
                    // 砂丘の影響範囲チェック
                    if (Mathf.Abs(alongWind) > length * 0.5f || Mathf.Abs(acrossWind) > width * 0.5f)
                        continue;
                    
                    // 非対称プロファイル
                    float windwardSlope = 0.3f;  // 風上側の緩やかな傾斜
                    float leewardSlope = 0.8f;   // 風下側の急な傾斜
                    
                    float normalizedAlong = alongWind / (length * 0.5f);
                    float normalizedAcross = acrossWind / (width * 0.5f);
                    
                    // 横方向のプロファイル（ガウシアン）
                    float crossProfile = Mathf.Exp(-normalizedAcross * normalizedAcross * 2f);
                    
                    // 縦方向のプロファイル（非対称）
                    float longProfile;
                    if (normalizedAlong < 0) // 風上側
                    {
                        longProfile = Mathf.Exp(-normalizedAlong * normalizedAlong / (windwardSlope * windwardSlope));
                    }
                    else // 風下側
                    {
                        longProfile = Mathf.Exp(-normalizedAlong * normalizedAlong / (leewardSlope * leewardSlope));
                    }
                    
                    float duneHeight = height * crossProfile * longProfile;
                    heightmap[x, y] += duneHeight;
                }
            }
        }
        
        /// <summary>
        /// 岩石露出を生成
        /// </summary>
        private static void GenerateRockOutcrops(float[,] heightmap, Vector3 worldPosition, float density)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            int outcropCount = Mathf.RoundToInt(width * height * density / 10000f);
            
            for (int i = 0; i < outcropCount; i++)
            {
                Vector2 outcropCenter = new Vector2(
                    Random.Range(0, width),
                    Random.Range(0, height)
                );
                
                float outcropRadius = Random.Range(20f, 60f);
                float outcropHeight = Random.Range(25f, 80f);
                
                // 不規則な岩石形状
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), outcropCenter);
                        
                        if (distance < outcropRadius)
                        {
                            // ノイズによる不規則性
                            float noise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                            float effectiveRadius = outcropRadius * (0.7f + noise * 0.6f);
                            
                            if (distance < effectiveRadius)
                            {
                                float falloff = 1f - (distance / effectiveRadius);
                                falloff = falloff * falloff; // 二次関数的減衰
                                
                                float rockHeight = outcropHeight * falloff * noise;
                                heightmap[x, y] += rockHeight;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// オアシスを生成
        /// </summary>
        private static void GenerateOases(float[,] heightmap, Vector3 worldPosition, float density)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            int oasisCount = Mathf.RoundToInt(width * height * density / 10000f);
            
            for (int i = 0; i < oasisCount; i++)
            {
                Vector2 oasisCenter = new Vector2(
                    Random.Range(width * 0.2f, width * 0.8f),
                    Random.Range(height * 0.2f, height * 0.8f)
                );
                
                float oasisRadius = Random.Range(40f, 100f);
                float depressionDepth = Random.Range(5f, 15f);
                
                // オアシス周辺の窪地
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), oasisCenter);
                        
                        if (distance < oasisRadius)
                        {
                            float falloff = 1f - (distance / oasisRadius);
                            falloff = Mathf.SmoothStep(0f, 1f, falloff);
                            
                            heightmap[x, y] -= depressionDepth * falloff;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 砂漠特有の浸食を適用
        /// </summary>
        private static void ApplyDesertErosion(float[,] heightmap, float erosionStrength)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 風による浸食（主に高い部分）
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float currentHeight = heightmap[x, y];
                    
                    // 周辺の平均高度
                    float avgHeight = (
                        heightmap[x-1, y] + heightmap[x+1, y] +
                        heightmap[x, y-1] + heightmap[x, y+1]
                    ) / 4f;
                    
                    // 高い部分ほど浸食されやすい
                    if (currentHeight > avgHeight)
                    {
                        float erosion = (currentHeight - avgHeight) * erosionStrength * 0.1f;
                        heightmap[x, y] -= erosion;
                    }
                }
            }
        }
        
        /// <summary>
        /// 風による砂の堆積を適用
        /// </summary>
        private static void ApplyWindDeposition(float[,] heightmap, Vector3 worldPosition)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            Vector2 windDirection = new Vector2(1f, 0.2f).normalized;
            
            // 風下側への砂の堆積
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    // 風上側の高度をチェック
                    int windUpX = x - Mathf.RoundToInt(windDirection.x * 3f);
                    int windUpY = y - Mathf.RoundToInt(windDirection.y * 3f);
                    
                    if (windUpX >= 0 && windUpX < width && windUpY >= 0 && windUpY < height)
                    {
                        float windUpHeight = heightmap[windUpX, windUpY];
                        float currentHeight = heightmap[x, y];
                        
                        // 風上が高い場合、砂が堆積
                        if (windUpHeight > currentHeight + 5f)
                        {
                            float deposition = (windUpHeight - currentHeight) * 0.02f;
                            heightmap[x, y] += deposition;
                        }
                    }
                }
            }
        }
        
        #endregion
        
        #region 森林地形生成
        
        /// <summary>
        /// 森林地形を生成（尾根、谷、森林の起伏）
        /// </summary>
        public static void GenerateForestTerrain(float[,] heightmap, BiomeDefinition biomeDefinition, Vector3 worldPosition)
        {
            // 森林特有の起伏パターン
            GenerateForestRidges(heightmap, worldPosition);
            
            // 河川谷の生成
            GenerateForestValleys(heightmap, worldPosition);
            
            // 森林の小さな起伏
            GenerateForestUndulation(heightmap);
            
            // 水による浸食（森林は浸食が少ない）
            ApplyForestErosion(heightmap, biomeDefinition.terrainModifiers.erosionStrength);
            
            // 有機物による土壌堆積
            ApplyOrganicSedimentation(heightmap);
        }
        
        /// <summary>
        /// 森林の尾根を生成
        /// </summary>
        private static void GenerateForestRidges(float[,] heightmap, Vector3 worldPosition)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 複数の尾根システム
            int ridgeCount = Random.Range(2, 5);
            
            for (int ridge = 0; ridge < ridgeCount; ridge++)
            {
                // 尾根の方向（ランダム）
                float ridgeAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector2 ridgeDirection = new Vector2(Mathf.Cos(ridgeAngle), Mathf.Sin(ridgeAngle));
                
                // 尾根の開始点
                Vector2 ridgeStart = new Vector2(
                    Random.Range(0f, width),
                    Random.Range(0f, height)
                );
                
                float ridgeLength = Random.Range(200f, 400f);
                float ridgeHeight = Random.Range(30f, 60f);
                float ridgeWidth = Random.Range(80f, 150f);
                
                // 尾根に沿って高度を追加
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Vector2 point = new Vector2(x, y);
                        Vector2 toPoint = point - ridgeStart;
                        
                        // 尾根方向への投影
                        float alongRidge = Vector2.Dot(toPoint, ridgeDirection);
                        float acrossRidge = Vector2.Dot(toPoint, new Vector2(-ridgeDirection.y, ridgeDirection.x));
                        
                        // 尾根の範囲内かチェック
                        if (alongRidge >= 0 && alongRidge <= ridgeLength && Mathf.Abs(acrossRidge) <= ridgeWidth * 0.5f)
                        {
                            // 尾根プロファイル
                            float longProfile = 1f - Mathf.Abs(alongRidge - ridgeLength * 0.5f) / (ridgeLength * 0.5f);
                            float crossProfile = 1f - Mathf.Abs(acrossRidge) / (ridgeWidth * 0.5f);
                            
                            // ノイズによる自然な変動
                            float noise = Mathf.PerlinNoise(x * 0.02f, y * 0.02f);
                            
                            float elevation = ridgeHeight * longProfile * crossProfile * (0.7f + noise * 0.6f);
                            heightmap[x, y] += elevation;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 森林の谷を生成
        /// </summary>
        private static void GenerateForestValleys(float[,] heightmap, Vector3 worldPosition)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 河川谷の生成
            int valleyCount = Random.Range(1, 3);
            
            for (int valley = 0; valley < valleyCount; valley++)
            {
                // 谷の経路を生成（蛇行する川）
                List<Vector2> valleyPath = GenerateMeanderingPath(width, height, Random.Range(5, 10));
                
                float valleyDepth = Random.Range(15f, 30f);
                float valleyWidth = Random.Range(60f, 120f);
                
                // 谷に沿って地形を削る
                foreach (Vector2 pathPoint in valleyPath)
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            float distance = Vector2.Distance(new Vector2(x, y), pathPoint);
                            
                            if (distance < valleyWidth * 0.5f)
                            {
                                float falloff = 1f - (distance / (valleyWidth * 0.5f));
                                falloff = Mathf.SmoothStep(0f, 1f, falloff);
                                
                                heightmap[x, y] -= valleyDepth * falloff;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 蛇行する経路を生成
        /// </summary>
        private static List<Vector2> GenerateMeanderingPath(int width, int height, int segmentCount)
        {
            List<Vector2> path = new List<Vector2>();
            
            Vector2 start = new Vector2(Random.Range(0f, width * 0.2f), Random.Range(height * 0.2f, height * 0.8f));
            Vector2 end = new Vector2(Random.Range(width * 0.8f, width), Random.Range(height * 0.2f, height * 0.8f));
            
            path.Add(start);
            
            for (int i = 1; i < segmentCount; i++)
            {
                float t = (float)i / segmentCount;
                Vector2 basePoint = Vector2.Lerp(start, end, t);
                
                // 蛇行のためのオフセット
                float meanderAmplitude = Vector2.Distance(start, end) * 0.2f;
                float meanderFreq = 3f;
                Vector2 perpendicular = Vector2.Perpendicular(end - start).normalized;
                Vector2 meanderOffset = perpendicular * Mathf.Sin(t * meanderFreq * Mathf.PI) * meanderAmplitude;
                
                path.Add(basePoint + meanderOffset);
            }
            
            path.Add(end);
            return path;
        }
        
        /// <summary>
        /// 森林の小さな起伏を生成
        /// </summary>
        private static void GenerateForestUndulation(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 複数スケールのノイズを重ね合わせ
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float undulation = 0f;
                    
                    // 大きな起伏
                    undulation += Mathf.PerlinNoise(x * 0.008f, y * 0.008f) * 20f;
                    
                    // 中程度の起伏
                    undulation += Mathf.PerlinNoise(x * 0.02f, y * 0.02f) * 8f;
                    
                    // 小さな起伏
                    undulation += Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 3f;
                    
                    heightmap[x, y] += undulation - 15f; // 平均を下げる
                }
            }
        }
        
        /// <summary>
        /// 森林特有の浸食を適用
        /// </summary>
        private static void ApplyForestErosion(float[,] heightmap, float erosionStrength)
        {
            // 森林は植生により浸食が抑制される
            float reducedErosion = erosionStrength * 0.3f;
            
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float currentHeight = heightmap[x, y];
                    float avgNeighbor = (
                        heightmap[x-1, y] + heightmap[x+1, y] +
                        heightmap[x, y-1] + heightmap[x, y+1]
                    ) / 4f;
                    
                    if (currentHeight > avgNeighbor)
                    {
                        float erosion = (currentHeight - avgNeighbor) * reducedErosion * 0.05f;
                        heightmap[x, y] -= erosion;
                    }
                }
            }
        }
        
        /// <summary>
        /// 有機物による土壌堆積を適用
        /// </summary>
        private static void ApplyOrganicSedimentation(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 低地への有機物堆積
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float currentHeight = heightmap[x, y];
                    float avgNeighbor = (
                        heightmap[x-1, y] + heightmap[x+1, y] +
                        heightmap[x, y-1] + heightmap[x, y+1]
                    ) / 4f;
                    
                    // 低地に有機物が堆積
                    if (currentHeight < avgNeighbor)
                    {
                        float deposition = (avgNeighbor - currentHeight) * 0.1f;
                        heightmap[x, y] += deposition;
                    }
                }
            }
        }
        
        #endregion
        
        #region 山岳地形生成
        
        /// <summary>
        /// 山岳地形を生成（山頂、尾根、急峻な谷）
        /// </summary>
        public static void GenerateMountainTerrain(float[,] heightmap, BiomeDefinition biomeDefinition, Vector3 worldPosition)
        {
            // 主要山頂の生成
            GenerateMountainPeaks(heightmap, worldPosition);
            
            // 山脈尾根の生成
            GenerateMountainRidges(heightmap, worldPosition);
            
            // 急峻な谷の生成
            GenerateMountainValleys(heightmap, worldPosition);
            
            // 岩石崩落地形
            GenerateRockfallTerrain(heightmap);
            
            // 山岳特有の浸食
            ApplyMountainErosion(heightmap, biomeDefinition.terrainModifiers.erosionStrength);
        }
        
        /// <summary>
        /// 山頂を生成
        /// </summary>
        private static void GenerateMountainPeaks(float[,] heightmap, Vector3 worldPosition)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            int peakCount = Random.Range(2, 5);
            
            for (int peak = 0; peak < peakCount; peak++)
            {
                Vector2 peakCenter = new Vector2(
                    Random.Range(width * 0.2f, width * 0.8f),
                    Random.Range(height * 0.2f, height * 0.8f)
                );
                
                float peakHeight = Random.Range(150f, 300f);
                float peakRadius = Random.Range(80f, 150f);
                
                // 山頂の形状（急峻）
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), peakCenter);
                        
                        if (distance < peakRadius)
                        {
                            // 急峻な山頂プロファイル
                            float falloff = 1f - (distance / peakRadius);
                            falloff = Mathf.Pow(falloff, 0.5f); // より急峻に
                            
                            // ノイズによる岩石の不規則性
                            float rockNoise = Mathf.PerlinNoise(x * 0.03f, y * 0.03f);
                            
                            float elevation = peakHeight * falloff * (0.8f + rockNoise * 0.4f);
                            heightmap[x, y] += elevation;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 山脈尾根を生成
        /// </summary>
        private static void GenerateMountainRidges(float[,] heightmap, Vector3 worldPosition)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 主要尾根の生成
            int ridgeCount = Random.Range(1, 3);
            
            for (int ridge = 0; ridge < ridgeCount; ridge++)
            {
                // 尾根の経路（より直線的）
                Vector2 start = new Vector2(Random.Range(0f, width), Random.Range(0f, height * 0.3f));
                Vector2 end = new Vector2(Random.Range(0f, width), Random.Range(height * 0.7f, height));
                
                float ridgeHeight = Random.Range(80f, 150f);
                float ridgeWidth = Random.Range(40f, 80f);
                
                // 尾根に沿って高度を追加
                int segments = 20;
                for (int seg = 0; seg < segments; seg++)
                {
                    float t = (float)seg / (segments - 1);
                    Vector2 ridgePoint = Vector2.Lerp(start, end, t);
                    
                    // 各セグメントで円形の高度を追加
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            float distance = Vector2.Distance(new Vector2(x, y), ridgePoint);
                            
                            if (distance < ridgeWidth * 0.5f)
                            {
                                float falloff = 1f - (distance / (ridgeWidth * 0.5f));
                                falloff = falloff * falloff; // 二次関数的減衰
                                
                                float segmentHeight = ridgeHeight * (1f - t * 0.3f); // 高度が徐々に下がる
                                heightmap[x, y] += segmentHeight * falloff;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 山岳の急峻な谷を生成
        /// </summary>
        private static void GenerateMountainValleys(float[,] heightmap, Vector3 worldPosition)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            int valleyCount = Random.Range(2, 4);
            
            for (int valley = 0; valley < valleyCount; valley++)
            {
                // V字谷の経路
                Vector2 start = new Vector2(Random.Range(width * 0.2f, width * 0.8f), 0f);
                Vector2 end = new Vector2(Random.Range(width * 0.2f, width * 0.8f), height);
                
                float valleyDepth = Random.Range(60f, 120f);
                float valleyWidth = Random.Range(30f, 60f); // 狭い谷
                
                // 谷に沿って地形を削る（V字型）
                int segments = 30;
                for (int seg = 0; seg < segments; seg++)
                {
                    float t = (float)seg / (segments - 1);
                    Vector2 valleyPoint = Vector2.Lerp(start, end, t);
                    
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            float distance = Vector2.Distance(new Vector2(x, y), valleyPoint);
                            
                            if (distance < valleyWidth * 0.5f)
                            {
                                // V字型プロファイル
                                float normalizedDistance = distance / (valleyWidth * 0.5f);
                                float vProfile = 1f - normalizedDistance * normalizedDistance;
                                
                                heightmap[x, y] -= valleyDepth * vProfile;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 岩石崩落地形を生成
        /// </summary>
        private static void GenerateRockfallTerrain(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 急斜面での岩石崩落をシミュレート
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    // 傾斜を計算
                    float slopeX = heightmap[x + 1, y] - heightmap[x - 1, y];
                    float slopeY = heightmap[x, y + 1] - heightmap[x, y - 1];
                    float slope = Mathf.Sqrt(slopeX * slopeX + slopeY * slopeY);
                    
                    // 急斜面（slope > 30）で崩落
                    if (slope > 30f)
                    {
                        // 崩落による平滑化
                        float avgHeight = (
                            heightmap[x-1, y] + heightmap[x+1, y] +
                            heightmap[x, y-1] + heightmap[x, y+1]
                        ) / 4f;
                        
                        heightmap[x, y] = Mathf.Lerp(heightmap[x, y], avgHeight, 0.3f);
                    }
                }
            }
        }
        
        /// <summary>
        /// 山岳特有の浸食を適用
        /// </summary>
        private static void ApplyMountainErosion(float[,] heightmap, float erosionStrength)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 氷河浸食と水による浸食
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float currentHeight = heightmap[x, y];
                    
                    // 高度による浸食強度
                    float altitudeFactor = Mathf.InverseLerp(0, 500, currentHeight); // 0-500mの範囲で正規化
                    float altitudeErosion = erosionStrength * (1f + altitudeFactor * 2f);
                    
                    // 周辺の平均高度
                    float avgNeighbor = (
                        heightmap[x-1, y] + heightmap[x+1, y] +
                        heightmap[x, y-1] + heightmap[x, y+1]
                    ) / 4f;
                    
                    // 高い部分ほど強く浸食
                    if (currentHeight > avgNeighbor)
                    {
                        float erosion = (currentHeight - avgNeighbor) * altitudeErosion * 0.08f;
                        heightmap[x, y] -= erosion;
                    }
                }
            }
        }
        
        #endregion
        
        #region 草原地形生成
        
        /// <summary>
        /// 草原地形を生成（なだらかな丘陵、河川、湿地）
        /// </summary>
        public static void GenerateGrasslandTerrain(float[,] heightmap, BiomeDefinition biomeDefinition, Vector3 worldPosition)
        {
            // なだらかな丘陵の生成
            GenerateRollingHills(heightmap);
            
            // 河川の生成
            GenerateGrasslandRivers(heightmap);
            
            // 湿地の生成
            GenerateWetlands(heightmap);
            
            // 草原特有の浸食
            ApplyGrasslandErosion(heightmap, biomeDefinition.terrainModifiers.erosionStrength);
        }
        
        /// <summary>
        /// なだらかな丘陵を生成
        /// </summary>
        private static void GenerateRollingHills(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 複数スケールの丘陵
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float hills = 0f;
                    
                    // 大きな丘陵
                    hills += Mathf.PerlinNoise(x * 0.005f, y * 0.005f) * 30f;
                    
                    // 中程度の丘陵
                    hills += Mathf.PerlinNoise(x * 0.012f, y * 0.012f) * 15f;
                    
                    // 小さな起伏
                    hills += Mathf.PerlinNoise(x * 0.025f, y * 0.025f) * 5f;
                    
                    heightmap[x, y] += hills;
                }
            }
        }
        
        /// <summary>
        /// 草原の河川を生成
        /// </summary>
        private static void GenerateGrasslandRivers(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 蛇行する河川
            int riverCount = Random.Range(1, 3);
            
            for (int river = 0; river < riverCount; river++)
            {
                List<Vector2> riverPath = GenerateMeanderingPath(width, height, Random.Range(8, 15));
                
                float riverDepth = Random.Range(8f, 15f);
                float riverWidth = Random.Range(20f, 40f);
                
                // 河川に沿って地形を削る
                foreach (Vector2 pathPoint in riverPath)
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            float distance = Vector2.Distance(new Vector2(x, y), pathPoint);
                            
                            if (distance < riverWidth * 0.5f)
                            {
                                float falloff = 1f - (distance / (riverWidth * 0.5f));
                                falloff = Mathf.SmoothStep(0f, 1f, falloff);
                                
                                heightmap[x, y] -= riverDepth * falloff;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 湿地を生成
        /// </summary>
        private static void GenerateWetlands(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            int wetlandCount = Random.Range(2, 5);
            
            for (int wetland = 0; wetland < wetlandCount; wetland++)
            {
                Vector2 wetlandCenter = new Vector2(
                    Random.Range(width * 0.2f, width * 0.8f),
                    Random.Range(height * 0.2f, height * 0.8f)
                );
                
                float wetlandRadius = Random.Range(60f, 120f);
                float wetlandDepth = Random.Range(3f, 8f);
                
                // 不規則な湿地形状
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float distance = Vector2.Distance(new Vector2(x, y), wetlandCenter);
                        
                        if (distance < wetlandRadius)
                        {
                            // ノイズによる不規則な境界
                            float noise = Mathf.PerlinNoise(x * 0.02f, y * 0.02f);
                            float effectiveRadius = wetlandRadius * (0.6f + noise * 0.8f);
                            
                            if (distance < effectiveRadius)
                            {
                                float falloff = 1f - (distance / effectiveRadius);
                                falloff = Mathf.SmoothStep(0f, 1f, falloff);
                                
                                heightmap[x, y] -= wetlandDepth * falloff;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 草原特有の浸食を適用
        /// </summary>
        private static void ApplyGrasslandErosion(float[,] heightmap, float erosionStrength)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 草による浸食抑制（森林ほどではない）
            float grassErosion = erosionStrength * 0.6f;
            
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float currentHeight = heightmap[x, y];
                    float avgNeighbor = (
                        heightmap[x-1, y] + heightmap[x+1, y] +
                        heightmap[x, y-1] + heightmap[x, y+1]
                    ) / 4f;
                    
                    if (currentHeight > avgNeighbor)
                    {
                        float erosion = (currentHeight - avgNeighbor) * grassErosion * 0.06f;
                        heightmap[x, y] -= erosion;
                    }
                }
            }
        }
        
        #endregion
        
        #region 海岸地形生成
        
        /// <summary>
        /// 海岸地形を生成（海岸線、砂浜、海食崖）
        /// </summary>
        public static void GenerateCoastalTerrain(float[,] heightmap, BiomeDefinition biomeDefinition, Vector3 worldPosition)
        {
            // 海岸線の生成
            GenerateCoastline(heightmap, worldPosition);
            
            // 砂浜の生成
            GenerateBeaches(heightmap);
            
            // 海食崖の生成
            GenerateSeaCliffs(heightmap);
            
            // 海岸浸食の適用
            ApplyCoastalErosion(heightmap, biomeDefinition.terrainModifiers.erosionStrength);
        }
        
        /// <summary>
        /// 海岸線を生成
        /// </summary>
        private static void GenerateCoastline(float[,] heightmap, Vector3 worldPosition)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 海面レベル（0m）を基準とした海岸線
            float seaLevel = 0f;
            
            // 海岸線の大まかな位置（地図の一辺）
            bool isWestCoast = Random.value > 0.5f;
            
            for (int y = 0; y < height; y++)
            {
                // 海岸線の蛇行
                float coastlineNoise = Mathf.PerlinNoise(y * 0.01f, worldPosition.z * 0.001f);
                int coastlineX = isWestCoast ? 
                    Mathf.RoundToInt(width * 0.2f + coastlineNoise * width * 0.3f) :
                    Mathf.RoundToInt(width * 0.8f + coastlineNoise * width * 0.3f);
                
                coastlineX = Mathf.Clamp(coastlineX, 0, width - 1);
                
                // 海岸線から海側を海面レベルに設定
                for (int x = 0; x < width; x++)
                {
                    bool isSeaSide = isWestCoast ? (x < coastlineX) : (x > coastlineX);
                    
                    if (isSeaSide)
                    {
                        heightmap[x, y] = Mathf.Min(heightmap[x, y], seaLevel);
                    }
                }
            }
        }
        
        /// <summary>
        /// 砂浜を生成
        /// </summary>
        private static void GenerateBeaches(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float currentHeight = heightmap[x, y];
                    
                    // 海面近くの低地を砂浜に
                    if (currentHeight >= -2f && currentHeight <= 5f)
                    {
                        // 海からの距離を計算
                        float distanceFromSea = CalculateDistanceFromSea(heightmap, x, y);
                        
                        if (distanceFromSea < 50f)
                        {
                            // 砂浜の緩やかな傾斜
                            float beachSlope = distanceFromSea * 0.1f;
                            heightmap[x, y] = Mathf.Max(heightmap[x, y], beachSlope);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 海食崖を生成
        /// </summary>
        private static void GenerateSeaCliffs(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float currentHeight = heightmap[x, y];
                    
                    // 海に近い高地を崖に
                    if (currentHeight > 20f)
                    {
                        float distanceFromSea = CalculateDistanceFromSea(heightmap, x, y);
                        
                        if (distanceFromSea < 30f)
                        {
                            // 急峻な崖を作成
                            float cliffHeight = currentHeight + Random.Range(20f, 60f);
                            heightmap[x, y] = cliffHeight;
                            
                            // 崖の浸食による不規則性
                            float erosionNoise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f);
                            heightmap[x, y] += erosionNoise * 15f;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// 海からの距離を計算
        /// </summary>
        private static float CalculateDistanceFromSea(float[,] heightmap, int x, int y)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            float minDistance = float.MaxValue;
            
            // 周辺の海面レベル（0m以下）の点を検索
            int searchRadius = 50;
            for (int dx = -searchRadius; dx <= searchRadius; dx++)
            {
                for (int dy = -searchRadius; dy <= searchRadius; dy++)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        if (heightmap[nx, ny] <= 0f)
                        {
                            float distance = Mathf.Sqrt(dx * dx + dy * dy);
                            minDistance = Mathf.Min(minDistance, distance);
                        }
                    }
                }
            }
            
            return minDistance == float.MaxValue ? 1000f : minDistance;
        }
        
        /// <summary>
        /// 海岸浸食を適用
        /// </summary>
        private static void ApplyCoastalErosion(float[,] heightmap, float erosionStrength)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 波による浸食
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float distanceFromSea = CalculateDistanceFromSea(heightmap, x, y);
                    
                    // 海に近いほど浸食が強い
                    if (distanceFromSea < 100f)
                    {
                        float waveErosion = erosionStrength * (1f - distanceFromSea / 100f);
                        
                        float currentHeight = heightmap[x, y];
                        float avgNeighbor = (
                            heightmap[x-1, y] + heightmap[x+1, y] +
                            heightmap[x, y-1] + heightmap[x, y+1]
                        ) / 4f;
                        
                        if (currentHeight > avgNeighbor)
                        {
                            float erosion = (currentHeight - avgNeighbor) * waveErosion * 0.15f;
                            heightmap[x, y] -= erosion;
                        }
                    }
                }
            }
        }
        
        #endregion
        
        #region 極地地形生成
        
        /// <summary>
        /// 極地地形を生成（氷河地形、ツンドラ、永久凍土）
        /// </summary>
        public static void GeneratePolarTerrain(float[,] heightmap, BiomeDefinition biomeDefinition, Vector3 worldPosition)
        {
            // 氷河地形の生成
            GenerateGlacialTerrain(heightmap);
            
            // ツンドラの起伏
            GenerateTundraUndulation(heightmap);
            
            // 永久凍土の影響
            ApplyPermafrostEffects(heightmap);
            
            // 氷河浸食
            ApplyGlacialErosion(heightmap, biomeDefinition.terrainModifiers.erosionStrength);
        }
        
        /// <summary>
        /// 氷河地形を生成
        /// </summary>
        private static void GenerateGlacialTerrain(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 氷河による平滑化
            float[,] smoothedMap = new float[width, height];
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float sum = 0f;
                    int count = 0;
                    int radius = 8; // 大きな平滑化半径
                    
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
            
            // 平滑化を適用
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    heightmap[x, y] = Mathf.Lerp(heightmap[x, y], smoothedMap[x, y], 0.7f);
                }
            }
            
            // 氷河谷（U字谷）の生成
            GenerateGlacialValleys(heightmap);
        }
        
        /// <summary>
        /// 氷河谷を生成
        /// </summary>
        private static void GenerateGlacialValleys(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            int valleyCount = Random.Range(1, 3);
            
            for (int valley = 0; valley < valleyCount; valley++)
            {
                Vector2 start = new Vector2(Random.Range(0f, width), 0f);
                Vector2 end = new Vector2(Random.Range(0f, width), height);
                
                float valleyDepth = Random.Range(40f, 80f);
                float valleyWidth = Random.Range(100f, 200f);
                
                // U字型の谷を作成
                int segments = 20;
                for (int seg = 0; seg < segments; seg++)
                {
                    float t = (float)seg / (segments - 1);
                    Vector2 valleyPoint = Vector2.Lerp(start, end, t);
                    
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            float distance = Vector2.Distance(new Vector2(x, y), valleyPoint);
                            
                            if (distance < valleyWidth * 0.5f)
                            {
                                // U字型プロファイル
                                float normalizedDistance = distance / (valleyWidth * 0.5f);
                                float uProfile = 1f - normalizedDistance * normalizedDistance;
                                uProfile = Mathf.Sqrt(uProfile); // より平らな底
                                
                                heightmap[x, y] -= valleyDepth * uProfile;
                            }
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// ツンドラの起伏を生成
        /// </summary>
        private static void GenerateTundraUndulation(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 小さな起伏（ポリゴン地形）
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // 永久凍土による小さな隆起
                    float polygonNoise = Mathf.PerlinNoise(x * 0.03f, y * 0.03f);
                    if (polygonNoise > 0.6f)
                    {
                        heightmap[x, y] += (polygonNoise - 0.6f) * 10f;
                    }
                    
                    // 全体的に低い起伏
                    float tundraUndulation = Mathf.PerlinNoise(x * 0.01f, y * 0.01f) * 5f;
                    heightmap[x, y] += tundraUndulation;
                }
            }
        }
        
        /// <summary>
        /// 永久凍土の影響を適用
        /// </summary>
        private static void ApplyPermafrostEffects(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            // 永久凍土による地形の安定化
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    // 急激な高度変化を抑制
                    float currentHeight = heightmap[x, y];
                    float avgNeighbor = (
                        heightmap[x-1, y] + heightmap[x+1, y] +
                        heightmap[x, y-1] + heightmap[x, y+1]
                    ) / 4f;
                    
                    float heightDiff = Mathf.Abs(currentHeight - avgNeighbor);
                    if (heightDiff > 10f)
                    {
                        // 永久凍土による安定化
                        heightmap[x, y] = Mathf.Lerp(currentHeight, avgNeighbor, 0.3f);
                    }
                }
            }
        }
        
        /// <summary>
        /// 氷河浸食を適用
        /// </summary>
        private static void ApplyGlacialErosion(float[,] heightmap, float erosionStrength)
        {
            // 氷河浸食は非常に強力だが、現在は氷に覆われているため浸食は少ない
            float glacialErosion = erosionStrength * 0.1f;
            
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);
            
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float currentHeight = heightmap[x, y];
                    float avgNeighbor = (
                        heightmap[x-1, y] + heightmap[x+1, y] +
                        heightmap[x, y-1] + heightmap[x, y+1]
                    ) / 4f;
                    
                    if (currentHeight > avgNeighbor)
                    {
                        float erosion = (currentHeight - avgNeighbor) * glacialErosion * 0.02f;
                        heightmap[x, y] -= erosion;
                    }
                }
            }
        }
        
        #endregion
    }
}