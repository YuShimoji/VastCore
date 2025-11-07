using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    /// <summary>
    /// 地形シンセサイザー - デザイナーテンプレートを基にした自動地形生成
    /// </summary>
    public static class TerrainSynthesizer
    {
        #region 公開メソッド

        /// <summary>
        /// デザイナーテンプレートを基に地形を生成
        /// </summary>
        public static void SynthesizeTerrain(float[,] heightmap, DesignerTerrainTemplate template, Vector3 worldPosition, float seed = 0f)
        {
            if (template == null || heightmap == null) return;

            // テンプレートの初期化
            template.Initialize();

            // バリエーション生成
            DesignerTerrainTemplate variation = template.CreateVariation(seed);

            // テンプレート適用
            ApplyTemplateToHeightmap(heightmap, variation, worldPosition);

            // 地形特徴の追加
            ApplyTerrainFeatures(heightmap, variation, worldPosition);

            // 自然なバリエーション追加
            AddNaturalVariation(heightmap, variation, seed);

            // 遷移処理
            ApplyTransitions(heightmap, variation, worldPosition);
        }

        /// <summary>
        /// 複数のテンプレートをブレンド
        /// </summary>
        public static void BlendTemplates(float[,] heightmap, List<TemplateBlend> blends, Vector3 worldPosition)
        {
            foreach (var blend in blends)
            {
                if (blend.template == null) continue;

                // ブレンド強度に応じて適用
                float strength = CalculateBlendStrength(blend, worldPosition);
                if (strength > 0.01f)
                {
                    SynthesizeTerrain(heightmap, blend.template, worldPosition, blend.seed);
                    BlendHeightmap(heightmap, blend.template, strength);
                }
            }
        }

        /// <summary>
        /// ハイトマップから地形を生成（画像ベース）
        /// </summary>
        public static void GenerateFromHeightmapImage(float[,] heightmap, Texture2D sourceImage, Vector3 worldPosition, TerrainSynthesisSettings settings)
        {
            if (sourceImage == null || heightmap == null) return;

            int mapWidth = heightmap.GetLength(0);
            int mapHeight = heightmap.GetLength(1);
            int imgWidth = sourceImage.width;
            int imgHeight = sourceImage.height;

            // 画像をハイトマップに変換
            Color[] pixels = sourceImage.GetPixels();

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    // 画像座標にマッピング
                    int imgX = Mathf.FloorToInt((float)x / mapWidth * imgWidth);
                    int imgY = Mathf.FloorToInt((float)y / mapHeight * imgHeight);

                    imgX = Mathf.Clamp(imgX, 0, imgWidth - 1);
                    imgY = Mathf.Clamp(imgY, 0, imgHeight - 1);

                    Color pixel = pixels[imgY * imgWidth + imgX];
                    float height = pixel.grayscale * settings.heightScale + settings.baseHeight;

                    // 既存の地形にブレンド
                    heightmap[x, y] = Mathf.Lerp(heightmap[x, y], height, settings.blendStrength);
                }
            }

            // 後処理
            ApplyPostProcessing(heightmap, settings);
        }

        #endregion

        #region テンプレート適用

        /// <summary>
        /// テンプレートをハイトマップに適用
        /// </summary>
        private static void ApplyTemplateToHeightmap(float[,] heightmap, DesignerTerrainTemplate template, Vector3 worldPosition)
        {
            float[,] templateData = template.GetHeightmapData();
            if (templateData == null) return;

            int mapWidth = heightmap.GetLength(0);
            int mapHeight = heightmap.GetLength(1);
            int templateWidth = templateData.GetLength(0);
            int templateHeight = templateData.GetLength(1);

            // テンプレート適用位置の計算
            Vector2Int applyPosition = CalculateTemplatePosition(worldPosition, mapWidth, mapHeight, templateWidth, templateHeight);

            // テンプレート適用サイズの計算
            float applySize = CalculateApplySize(template, worldPosition);

            // テンプレートを適用
            for (int templateX = 0; templateX < templateWidth; templateX++)
            {
                for (int templateY = 0; templateY < templateHeight; templateY++)
                {
                    // ワールド座標に変換
                    int mapX = applyPosition.x + Mathf.FloorToInt((float)templateX / templateWidth * applySize);
                    int mapY = applyPosition.y + Mathf.FloorToInt((float)templateY / templateHeight * applySize);

                    if (mapX >= 0 && mapX < mapWidth && mapY >= 0 && mapY < mapHeight)
                    {
                        float templateHeight = templateData[templateX, templateY];
                        float blendStrength = CalculateBlendStrength(template, worldPosition, new Vector2Int(mapX, mapY));

                        heightmap[mapX, mapY] = Mathf.Lerp(heightmap[mapX, mapY], templateHeight, blendStrength);
                    }
                }
            }
        }

        /// <summary>
        /// テンプレート適用位置を計算
        /// </summary>
        private static Vector2Int CalculateTemplatePosition(Vector3 worldPosition, int mapWidth, int mapHeight, int templateWidth, int templateHeight)
        {
            // ワールド位置に基づいてタイル内の相対位置を計算
            float tileSize = 100f; // 仮定値
            Vector2 tileOffset = new Vector2(worldPosition.x / tileSize, worldPosition.z / tileSize);
            tileOffset = new Vector2(tileOffset.x - Mathf.Floor(tileOffset.x), tileOffset.y - Mathf.Floor(tileOffset.y));

            int mapX = Mathf.FloorToInt(tileOffset.x * mapWidth);
            int mapY = Mathf.FloorToInt(tileOffset.y * mapHeight);

            return new Vector2Int(mapX, mapY);
        }

        /// <summary>
        /// 適用サイズを計算
        /// </summary>
        private static float CalculateApplySize(DesignerTerrainTemplate template, Vector3 worldPosition)
        {
            // テンプレートのサイズ範囲に基づいてランダムに決定
            float sizeRange = template.sizeRange.y - template.sizeRange.x;
            float normalizedPos = (worldPosition.x + worldPosition.z) * 0.01f; // シードとして使用
            float randomValue = Mathf.PerlinNoise(normalizedPos, normalizedPos * 0.5f);

            return template.sizeRange.x + randomValue * sizeRange;
        }

        /// <summary>
        /// ブレンド強度を計算
        /// </summary>
        private static float CalculateBlendStrength(DesignerTerrainTemplate template, Vector3 worldPosition, Vector2Int mapPosition)
        {
            // 距離ベースの減衰
            Vector2 center = new Vector2(template.sizeRange.y * 0.5f, template.sizeRange.y * 0.5f);
            Vector2 pos = new Vector2(mapPosition.x, mapPosition.y);
            float distance = Vector2.Distance(pos, center);
            float maxDistance = template.sizeRange.y * 0.5f;

            float distanceFalloff = 1f - Mathf.Clamp01(distance / maxDistance);
            return distanceFalloff * template.variationStrength;
        }

        #endregion

        #region 地形特徴適用

        /// <summary>
        /// 地形特徴を適用
        /// </summary>
        private static void ApplyTerrainFeatures(float[,] heightmap, DesignerTerrainTemplate template, Vector3 worldPosition)
        {
            foreach (var feature in template.terrainFeatures)
            {
                ApplyTerrainFeature(heightmap, feature, template, worldPosition);
            }
        }

        /// <summary>
        /// 個別の地形特徴を適用
        /// </summary>
        private static void ApplyTerrainFeature(float[,] heightmap, TerrainFeature feature, DesignerTerrainTemplate template, Vector3 worldPosition)
        {
            int mapWidth = heightmap.GetLength(0);
            int mapHeight = heightmap.GetLength(1);

            // 特徴のワールド位置を計算
            Vector2 featureWorldPos = new Vector2(worldPosition.x, worldPosition.z) + feature.relativePosition;

            // マップ座標に変換
            int centerX = Mathf.FloorToInt((featureWorldPos.x / 100f) * mapWidth) % mapWidth; // 仮定値
            int centerY = Mathf.FloorToInt((featureWorldPos.y / 100f) * mapHeight) % mapHeight;

            // 特徴を適用
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));

                    if (distance < feature.radius)
                    {
                        float falloff = 1f - (distance / feature.radius);
                        falloff = Mathf.SmoothStep(0f, 1f, falloff);

                        float featureHeight = CalculateFeatureHeight(feature, distance);
                        heightmap[x, y] += featureHeight * falloff * feature.strength;
                    }
                }
            }
        }

        /// <summary>
        /// 特徴の高さを計算
        /// </summary>
        private static float CalculateFeatureHeight(TerrainFeature feature, float distance)
        {
            switch (feature.featureType)
            {
                case TerrainFeatureType.Peak:
                    return feature.height * (1f - distance / feature.radius);
                case TerrainFeatureType.Valley:
                    return -feature.height * (1f - distance / feature.radius);
                case TerrainFeatureType.Ridge:
                    return feature.height * Mathf.Sin(distance / feature.radius * Mathf.PI);
                case TerrainFeatureType.Cliff:
                    return distance < feature.radius * 0.8f ? feature.height : -feature.height * 0.5f;
                case TerrainFeatureType.Plateau:
                    return feature.height;
                case TerrainFeatureType.Depression:
                    return -feature.height;
                default:
                    return 0f;
            }
        }

        #endregion

        #region 自然バリエーション

        /// <summary>
        /// 自然なバリエーションを追加
        /// </summary>
        private static void AddNaturalVariation(float[,] heightmap, DesignerTerrainTemplate template, float seed)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // ノイズベースの微小変動
                    float noise1 = Mathf.PerlinNoise(x * template.noiseScale * 0.01f + seed, y * template.noiseScale * 0.01f + seed);
                    float noise2 = Mathf.PerlinNoise(x * template.noiseScale * 0.02f + seed * 2f, y * template.noiseScale * 0.02f + seed * 2f) * 0.5f;

                    float variation = (noise1 + noise2 - 1f) * template.variationStrength * 10f;
                    heightmap[x, y] += variation;
                }
            }
        }

        #endregion

        #region 遷移処理

        /// <summary>
        /// 遷移処理を適用
        /// </summary>
        private static void ApplyTransitions(float[,] heightmap, DesignerTerrainTemplate template, Vector3 worldPosition)
        {
            // エッジでのスムージング
            SmoothEdges(heightmap);

            // 地形の正規化
            NormalizeTerrain(heightmap, template);
        }

        /// <summary>
        /// エッジをスムージング
        /// </summary>
        private static void SmoothEdges(float[,] heightmap)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            float[,] smoothed = new float[width, height];

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    // ガウシアンぼかし
                    float sum = heightmap[x, y] * 4f;
                    sum += heightmap[x-1, y] * 2f;
                    sum += heightmap[x+1, y] * 2f;
                    sum += heightmap[x, y-1] * 2f;
                    sum += heightmap[x, y+1] * 2f;
                    sum += heightmap[x-1, y-1] * 1f;
                    sum += heightmap[x-1, y+1] * 1f;
                    sum += heightmap[x+1, y-1] * 1f;
                    sum += heightmap[x+1, y+1] * 1f;

                    smoothed[x, y] = sum / 16f;
                }
            }

            // 結果をコピー
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    heightmap[x, y] = smoothed[x, y];
                }
            }
        }

        /// <summary>
        /// 地形を正規化
        /// </summary>
        private static void NormalizeTerrain(float[,] heightmap, DesignerTerrainTemplate template)
        {
            // 必要に応じて高度範囲を調整
            // ここではシンプルな実装
        }

        #endregion

        #region 後処理

        /// <summary>
        /// 後処理を適用
        /// </summary>
        private static void ApplyPostProcessing(float[,] heightmap, TerrainSynthesisSettings settings)
        {
            if (settings.applySmoothing)
            {
                SmoothEdges(heightmap);
            }

            if (settings.applyErosion > 0f)
            {
                ApplySimpleErosion(heightmap, settings.applyErosion);
            }
        }

        /// <summary>
        /// シンプルな浸食を適用
        /// </summary>
        private static void ApplySimpleErosion(float[,] heightmap, float strength)
        {
            int width = heightmap.GetLength(0);
            int height = heightmap.GetLength(1);

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float current = heightmap[x, y];
                    float avg = (heightmap[x-1, y] + heightmap[x+1, y] + heightmap[x, y-1] + heightmap[x, y+1]) / 4f;

                    if (current > avg)
                    {
                        heightmap[x, y] = Mathf.Lerp(current, avg, strength * 0.1f);
                    }
                }
            }
        }

        #endregion

        #region ブレンド計算

        /// <summary>
        /// ブレンド強度を計算
        /// </summary>
        private static float CalculateBlendStrength(TemplateBlend blend, Vector3 worldPosition)
        {
            // 距離ベースのブレンド
            float distance = Vector3.Distance(worldPosition, blend.position);
            float falloff = 1f - Mathf.Clamp01(distance / blend.radius);

            return falloff * blend.strength;
        }

        /// <summary>
        /// ハイトマップをブレンド
        /// </summary>
        private static void BlendHeightmap(float[,] heightmap, DesignerTerrainTemplate template, float strength)
        {
            // 必要に応じて実装
        }

        #endregion
    }

    /// <summary>
    /// テンプレートブレンドデータ
    /// </summary>
    [System.Serializable]
    public class TemplateBlend
    {
        public DesignerTerrainTemplate template;
        public Vector3 position;
        public float radius = 100f;
        public float strength = 1f;
        public float seed = 0f;
    }

    /// <summary>
    /// 地形合成設定
    /// </summary>
    [System.Serializable]
    public class TerrainSynthesisSettings
    {
        public float heightScale = 100f;
        public float baseHeight = 0f;
        public float blendStrength = 0.8f;
        public bool applySmoothing = true;
        public float applyErosion = 0.1f;
    }
}
