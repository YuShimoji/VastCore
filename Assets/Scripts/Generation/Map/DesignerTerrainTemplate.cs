using UnityEngine;
using System.Collections.Generic;

namespace Vastcore.Generation
{
    /// <summary>
    /// デザイナーが手動で作成した地形テンプレート
    /// ハイトマップや画像から地形パターンを定義
    /// </summary>
    [CreateAssetMenu(fileName = "TerrainTemplate", menuName = "VastCore/Terrain Template", order = 1)]
    public class DesignerTerrainTemplate : ScriptableObject
    {
        [Header("基本設定")]
        public string templateName = "New Terrain Template";
        public TerrainTemplateType templateType = TerrainTemplateType.Heightmap;
        public BiomeType associatedBiome = BiomeType.Grassland;

        [Header("地形データ")]
        public Texture2D heightmapTexture; // ハイトマップ画像
        public float heightScale = 100f; // 高度スケール
        public float baseHeight = 0f; // 基準高度

        [Header("地形特徴")]
        public List<TerrainFeature> terrainFeatures = new List<TerrainFeature>();

        [Header("生成パラメータ")]
        public float noiseScale = 1f; // ノイズスケール
        public float variationStrength = 0.3f; // バリエーション強度
        public bool allowVerticalFlip = true; // 垂直反転許可
        public bool allowHorizontalFlip = true; // 水平反転許可

        [Header("適応範囲")]
        public Vector2 sizeRange = new Vector2(50f, 200f); // 適用サイズ範囲
        public Vector2 heightRange = new Vector2(-50f, 200f); // 高度範囲
        public Vector2 slopeRange = new Vector2(0f, 45f); // 斜面範囲

        // キャッシュされたデータ
        private float[,] cachedHeightmap;
        private bool isInitialized = false;

        /// <summary>
        /// テンプレートを初期化
        /// </summary>
        public void Initialize()
        {
            if (isInitialized) return;

            // ハイトマップデータをキャッシュ
            if (heightmapTexture != null)
            {
                cachedHeightmap = TextureToHeightmap(heightmapTexture);
            }

            isInitialized = true;
        }

        /// <summary>
        /// 指定位置に適応可能かチェック
        /// </summary>
        public bool CanApplyAt(Vector3 worldPosition, float terrainHeight, float terrainSlope)
        {
            return terrainHeight >= heightRange.x && terrainHeight <= heightRange.y &&
                   terrainSlope >= slopeRange.x && terrainSlope <= slopeRange.y;
        }

        /// <summary>
        /// 地形データを取得
        /// </summary>
        public float[,] GetHeightmapData()
        {
            Initialize();
            return cachedHeightmap;
        }

        /// <summary>
        /// テクスチャをハイトマップに変換
        /// </summary>
        private float[,] TextureToHeightmap(Texture2D texture)
        {
            int width = texture.width;
            int height = texture.height;
            float[,] heightmap = new float[width, height];

            Color[] pixels = texture.GetPixels();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    // グレースケール値を使用
                    float grayscale = pixels[index].grayscale;
                    heightmap[x, y] = grayscale * heightScale + baseHeight;
                }
            }

            return heightmap;
        }

        /// <summary>
        /// テンプレートのバリエーションを生成
        /// </summary>
        public DesignerTerrainTemplate CreateVariation(float seed)
        {
            DesignerTerrainTemplate variation = Instantiate(this);

            // シードに基づいてパラメータを変更
            System.Random random = new System.Random((int)(seed * 1000));

            if (allowHorizontalFlip && random.NextDouble() > 0.5f)
            {
                variation.FlipHorizontal();
            }

            if (allowVerticalFlip && random.NextDouble() > 0.5f)
            {
                variation.FlipVertical();
            }

            variation.noiseScale *= 0.8f + (float)random.NextDouble() * 0.4f;
            variation.variationStrength *= 0.7f + (float)random.NextDouble() * 0.6f;

            return variation;
        }

        /// <summary>
        /// 水平反転
        /// </summary>
        private void FlipHorizontal()
        {
            if (cachedHeightmap != null)
            {
                int width = cachedHeightmap.GetLength(0);
                int height = cachedHeightmap.GetLength(1);
                float[,] flipped = new float[width, height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        flipped[x, y] = cachedHeightmap[width - 1 - x, y];
                    }
                }

                cachedHeightmap = flipped;
            }
        }

        /// <summary>
        /// 垂直反転
        /// </summary>
        private void FlipVertical()
        {
            if (cachedHeightmap != null)
            {
                int width = cachedHeightmap.GetLength(0);
                int height = cachedHeightmap.GetLength(1);
                float[,] flipped = new float[width, height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        flipped[x, y] = cachedHeightmap[x, height - 1 - y];
                    }
                }

                cachedHeightmap = flipped;
            }
        }
    }

    /// <summary>
    /// 地形特徴データ
    /// </summary>
    [System.Serializable]
    public class TerrainFeature
    {
        public string featureName = "Feature";
        public TerrainFeatureType featureType = TerrainFeatureType.Peak;
        public Vector2 relativePosition = Vector2.zero; // テンプレート内相対位置
        public float strength = 1f;
        public float radius = 10f;
        public float height = 20f;
    }

    /// <summary>
    /// テンプレートタイプ
    /// </summary>
    public enum TerrainTemplateType
    {
        Heightmap,      // ハイトマップ画像
        Procedural,     // プロシージャル生成
        Mixed          // ハイブリッド
    }

    /// <summary>
    /// 地形特徴タイプ
    /// </summary>
    public enum TerrainFeatureType
    {
        Peak,
        Valley,
        Ridge,
        Cliff,
        Plateau,
        Depression
    }
}
