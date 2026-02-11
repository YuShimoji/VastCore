using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Terrain.DualGrid
{
    /// <summary>
    /// 高さ生成ロジック
    /// 単純な高さマップまたはノイズ関数を使用して、各セルに「高さ（何階層までブロックがあるか）」を設定
    /// </summary>
    public static class VerticalExtrusionGenerator
    {
        #region Constants (Legacy)
        // レガシー固定レンジ（UseProfileBounds == false 時のフォールバック）
        private const float LegacyMinXZ = -10f;
        private const float LegacyMaxXZ = 10f;
        private const float LegacyRange = LegacyMaxXZ - LegacyMinXZ; // 20f
        #endregion

        #region Public Methods
        /// <summary>
        /// 高さマップを使用して高さを生成（プロファイル駆動）
        /// </summary>
        public static void GenerateFromHeightMap(IrregularGrid _grid, ColumnStack _columnStack,
            Texture2D _heightMap, int _maxHeight = 10,
            DualGridHeightSamplingSettings _samplingSettings = null)
        {
            if (_grid == null || _columnStack == null)
            {
                Debug.LogError("VerticalExtrusionGenerator: Grid or ColumnStack is null.");
                return;
            }

            if (_heightMap == null)
            {
                Debug.LogWarning("VerticalExtrusionGenerator: HeightMap is null. Using noise instead.");
                GenerateFromNoise(_grid, _columnStack, 0, _maxHeight);
                return;
            }

            int mapWidth = _heightMap.width;
            int mapHeight = _heightMap.height;

            foreach (Cell cell in _grid.Cells)
            {
                Vector3 center = cell.GetCenter();

                WorldToSampleIndex(center.x, center.z, mapWidth, mapHeight,
                    _samplingSettings, out int x, out int y);

                // 高さマップから値を取得（グレースケールとして扱う）
                Color pixel = _heightMap.GetPixel(x, y);
                float heightValue = pixel.grayscale; // 0.0～1.0

                int height = QuantizeHeight(heightValue, _maxHeight, _samplingSettings);

                for (int layer = 0; layer < height; layer++)
                {
                    _columnStack.SetLayer(cell.Id, layer, true);
                }
            }
        }

        /// <summary>
        /// ノイズ関数を使用して高さを生成
        /// </summary>
        public static void GenerateFromNoise(IrregularGrid _grid, ColumnStack _columnStack,
            int _seed, int _maxHeight = 10, float _noiseScale = 0.1f)
        {
            if (_grid == null || _columnStack == null)
            {
                Debug.LogError("VerticalExtrusionGenerator: Grid or ColumnStack is null.");
                return;
            }

            float seedOffsetX = _seed * 0.01f;
            float seedOffsetZ = _seed * 0.01f + 100f;

            foreach (Cell cell in _grid.Cells)
            {
                Vector3 center = cell.GetCenter();

                float noiseX = center.x * _noiseScale + seedOffsetX;
                float noiseZ = center.z * _noiseScale + seedOffsetZ;
                float noiseValue = Mathf.PerlinNoise(noiseX, noiseZ); // 0.0～1.0

                int height = Mathf.RoundToInt(noiseValue * _maxHeight);

                for (int layer = 0; layer < height; layer++)
                {
                    _columnStack.SetLayer(cell.Id, layer, true);
                }
            }
        }

        /// <summary>
        /// 単純な高さマップ（2D配列）を使用して高さを生成（プロファイル駆動）
        /// </summary>
        public static void GenerateFromHeightMapArray(IrregularGrid _grid, ColumnStack _columnStack,
            float[,] _heightMap, int _mapSize, int _maxHeight = 10,
            DualGridHeightSamplingSettings _samplingSettings = null)
        {
            if (_grid == null || _columnStack == null)
            {
                Debug.LogError("VerticalExtrusionGenerator: Grid or ColumnStack is null.");
                return;
            }

            if (_heightMap == null || _heightMap.GetLength(0) != _mapSize || _heightMap.GetLength(1) != _mapSize)
            {
                Debug.LogWarning("VerticalExtrusionGenerator: HeightMap array is invalid. Using noise instead.");
                GenerateFromNoise(_grid, _columnStack, 0, _maxHeight);
                return;
            }

            foreach (Cell cell in _grid.Cells)
            {
                Vector3 center = cell.GetCenter();

                WorldToSampleIndex(center.x, center.z, _mapSize, _mapSize,
                    _samplingSettings, out int x, out int y);

                float heightValue = _heightMap[x, y]; // 0.0～1.0

                int height = QuantizeHeight(heightValue, _maxHeight, _samplingSettings);

                for (int layer = 0; layer < height; layer++)
                {
                    _columnStack.SetLayer(cell.Id, layer, true);
                }
            }
        }
        #endregion

        #region Internal Mapping
        /// <summary>
        /// ワールド座標をサンプルインデックスに変換する
        /// プロファイル設定がある場合はプロファイル駆動、なければレガシーフォールバック
        /// </summary>
        internal static void WorldToSampleIndex(float worldX, float worldZ,
            int texWidth, int texHeight,
            DualGridHeightSamplingSettings settings,
            out int sampleX, out int sampleY)
        {
            float u, v;

            if (settings != null && settings.UseProfileBounds)
            {
                // プロファイル駆動マッピング
                float uRaw = Mathf.InverseLerp(settings.WorldMinXZ.x, settings.WorldMaxXZ.x, worldX);
                float vRaw = Mathf.InverseLerp(settings.WorldMinXZ.y, settings.WorldMaxXZ.y, worldZ);

                if (settings.UvAddressMode == DualGridUvAddressMode.Wrap)
                {
                    u = Mathf.Repeat(uRaw, 1f);
                    v = Mathf.Repeat(vRaw, 1f);
                }
                else // Clamp
                {
                    u = Mathf.Clamp01(uRaw);
                    v = Mathf.Clamp01(vRaw);
                }
            }
            else
            {
                // レガシーフォールバック: 固定 -10～10 レンジ
                u = Mathf.Clamp01((worldX - LegacyMinXZ) / LegacyRange);
                v = Mathf.Clamp01((worldZ - LegacyMinXZ) / LegacyRange);
            }

            sampleX = Mathf.Clamp(Mathf.FloorToInt(u * texWidth), 0, texWidth - 1);
            sampleY = Mathf.Clamp(Mathf.FloorToInt(v * texHeight), 0, texHeight - 1);
        }

        /// <summary>
        /// 高さ値を量子化する
        /// </summary>
        internal static int QuantizeHeight(float heightValue01, int maxHeight,
            DualGridHeightSamplingSettings settings)
        {
            float raw = heightValue01 * maxHeight;

            if (settings == null)
            {
                // レガシー: RoundToInt
                return Mathf.RoundToInt(raw);
            }

            switch (settings.HeightQuantization)
            {
                case DualGridHeightQuantization.FloorToInt:
                    return Mathf.FloorToInt(raw);
                case DualGridHeightQuantization.CeilToInt:
                    return Mathf.CeilToInt(raw);
                case DualGridHeightQuantization.RoundToInt:
                default:
                    return Mathf.RoundToInt(raw);
            }
        }
        #endregion
    }
}
