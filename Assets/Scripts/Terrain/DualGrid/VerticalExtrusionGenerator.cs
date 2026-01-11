using UnityEngine;

namespace Vastcore.Terrain.DualGrid
{
    /// <summary>
    /// 高さ生成ロジック
    /// 単純な高さマップまたはノイズ関数を使用して、各セルに「高さ（何階層までブロックがあるか）」を設定
    /// </summary>
    public static class VerticalExtrusionGenerator
    {
        #region Public Methods
        /// <summary>
        /// 高さマップを使用して高さを生成
        /// </summary>
        /// <param name="_grid">グリッド</param>
        /// <param name="_columnStack">垂直データ管理</param>
        /// <param name="_heightMap">高さマップ（テクスチャ、0.0～1.0の範囲）</param>
        /// <param name="_maxHeight">最大高さ（レイヤー数）</param>
        public static void GenerateFromHeightMap(IrregularGrid _grid, ColumnStack _columnStack, 
            Texture2D _heightMap, int _maxHeight = 10)
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
            
            // 高さマップのサイズを取得
            int mapWidth = _heightMap.width;
            int mapHeight = _heightMap.height;
            
            // 各セルに対して高さを設定
            foreach (Cell cell in _grid.Cells)
            {
                Vector3 center = cell.GetCenter();
                
                // ワールド座標を高さマップのUV座標に変換
                // 簡易実装: グリッドの範囲を高さマップのサイズにマッピング
                float u = Mathf.Clamp01((center.x + 10f) / 20f); // -10～10の範囲を0～1にマッピング
                float v = Mathf.Clamp01((center.z + 10f) / 20f);
                
                int x = Mathf.FloorToInt(u * mapWidth);
                int y = Mathf.FloorToInt(v * mapHeight);
                x = Mathf.Clamp(x, 0, mapWidth - 1);
                y = Mathf.Clamp(y, 0, mapHeight - 1);
                
                // 高さマップから値を取得（グレースケールとして扱う）
                Color pixel = _heightMap.GetPixel(x, y);
                float heightValue = pixel.grayscale; // 0.0～1.0
                
                // 高さを計算（0～maxHeightの範囲）
                int height = Mathf.RoundToInt(heightValue * _maxHeight);
                
                // セルの各レイヤーを設定
                for (int layer = 0; layer < height; layer++)
                {
                    _columnStack.SetLayer(cell.Id, layer, true);
                }
            }
        }
        
        /// <summary>
        /// ノイズ関数を使用して高さを生成
        /// </summary>
        /// <param name="_grid">グリッド</param>
        /// <param name="_columnStack">垂直データ管理</param>
        /// <param name="_seed">ランダムシード</param>
        /// <param name="_maxHeight">最大高さ（レイヤー数）</param>
        /// <param name="_noiseScale">ノイズのスケール</param>
        public static void GenerateFromNoise(IrregularGrid _grid, ColumnStack _columnStack, 
            int _seed, int _maxHeight = 10, float _noiseScale = 0.1f)
        {
            if (_grid == null || _columnStack == null)
            {
                Debug.LogError("VerticalExtrusionGenerator: Grid or ColumnStack is null.");
                return;
            }
            
            // シードを設定（UnityのPerlinNoiseは内部的にシードを持たないため、オフセットを使用）
            float seedOffsetX = _seed * 0.01f;
            float seedOffsetZ = _seed * 0.01f + 100f;
            
            // 各セルに対して高さを設定
            foreach (Cell cell in _grid.Cells)
            {
                Vector3 center = cell.GetCenter();
                
                // パーリンノイズを使用して高さを計算
                float noiseX = center.x * _noiseScale + seedOffsetX;
                float noiseZ = center.z * _noiseScale + seedOffsetZ;
                float noiseValue = Mathf.PerlinNoise(noiseX, noiseZ); // 0.0～1.0
                
                // 高さを計算（0～maxHeightの範囲）
                int height = Mathf.RoundToInt(noiseValue * _maxHeight);
                
                // セルの各レイヤーを設定
                for (int layer = 0; layer < height; layer++)
                {
                    _columnStack.SetLayer(cell.Id, layer, true);
                }
            }
        }
        
        /// <summary>
        /// 単純な高さマップ（2D配列）を使用して高さを生成
        /// </summary>
        /// <param name="_grid">グリッド</param>
        /// <param name="_columnStack">垂直データ管理</param>
        /// <param name="_heightMap">高さマップ（2D配列、0.0～1.0の範囲）</param>
        /// <param name="_mapSize">マップのサイズ（正方形）</param>
        /// <param name="_maxHeight">最大高さ（レイヤー数）</param>
        public static void GenerateFromHeightMapArray(IrregularGrid _grid, ColumnStack _columnStack, 
            float[,] _heightMap, int _mapSize, int _maxHeight = 10)
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
            
            // 各セルに対して高さを設定
            foreach (Cell cell in _grid.Cells)
            {
                Vector3 center = cell.GetCenter();
                
                // ワールド座標を高さマップのインデックスに変換
                // 簡易実装: グリッドの範囲をマップサイズにマッピング
                float u = Mathf.Clamp01((center.x + 10f) / 20f); // -10～10の範囲を0～1にマッピング
                float v = Mathf.Clamp01((center.z + 10f) / 20f);
                
                int x = Mathf.FloorToInt(u * _mapSize);
                int y = Mathf.FloorToInt(v * _mapSize);
                x = Mathf.Clamp(x, 0, _mapSize - 1);
                y = Mathf.Clamp(y, 0, _mapSize - 1);
                
                // 高さマップから値を取得
                float heightValue = _heightMap[x, y]; // 0.0～1.0
                
                // 高さを計算（0～maxHeightの範囲）
                int height = Mathf.RoundToInt(heightValue * _maxHeight);
                
                // セルの各レイヤーを設定
                for (int layer = 0; layer < height; layer++)
                {
                    _columnStack.SetLayer(cell.Id, layer, true);
                }
            }
        }
        #endregion
    }
}
