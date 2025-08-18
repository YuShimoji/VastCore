using UnityEngine;
using System.Collections.Generic;
using Vastcore.Generation;

namespace Vastcore.Generation
{
    /// <summary>
    /// 円形地形生成システムとシームレス接続システムの統合クラス
    /// 要求1.2と1.3を満たす完全な実装
    /// </summary>
    public class CircularTerrainSystemIntegration : MonoBehaviour
    {
        [Header("システム設定")]
        public bool enableCircularTerrain = true;
        public bool enableSeamlessConnection = true;
        public bool generateOnStart = false;
        
        [Header("地形生成パラメータ")]
        public MeshGenerator.TerrainGenerationParams baseTerrainParams = MeshGenerator.TerrainGenerationParams.Default();
        public CircularTerrainGenerator.CircularTerrainParams circularParams = CircularTerrainGenerator.CircularTerrainParams.Default();
        public SeamlessConnectionManager.BlendSettings blendSettings = SeamlessConnectionManager.BlendSettings.Default();
        
        [Header("タイル設定")]
        public int tileGridSize = 3;
        public float tileSpacing = 2000f;
        public Material terrainMaterial;
        
        [Header("デバッグ設定")]
        public bool showDebugInfo = true;
        public bool showBoundaries = false;
        public Color boundaryColor = Color.red;
        
        [Header("生成結果")]
        public GameObject[] generatedTiles;
        public CircularTerrainGenerator.CircularTerrainStats[] tileStats;
        
        private Dictionary<Vector2Int, float[,]> tileHeightmaps = new Dictionary<Vector2Int, float[,]>();
        private Dictionary<Vector2Int, SeamlessConnectionManager.ConnectionData> connectionDataCache = new Dictionary<Vector2Int, SeamlessConnectionManager.ConnectionData>();
        
        void Start()
        {
            if (generateOnStart)
            {
                GenerateCircularTerrainSystem();
            }
        }
        
        [ContextMenu("Generate Circular Terrain System")]
        public void GenerateCircularTerrainSystem()
        {
            Debug.Log("=== Generating Circular Terrain System ===");
            
            try
            {
                // 1. 既存の地形をクリア
                ClearExistingTerrain();
                
                // 2. 基本ハイトマップを生成
                GenerateBaseHeightmaps();
                
                // 3. 円形地形を適用
                if (enableCircularTerrain)
                {
                    ApplyCircularTerrainGeneration();
                }
                
                // 4. シームレス接続を適用
                if (enableSeamlessConnection)
                {
                    ApplySeamlessConnections();
                }
                
                // 5. メッシュを生成してシーンに配置
                CreateTerrainMeshes();
                
                // 6. 統計情報を計算
                CalculateTerrainStatistics();
                
                Debug.Log($"✓ Circular Terrain System generated successfully with {generatedTiles.Length} tiles");
                
                if (showDebugInfo)
                {
                    LogSystemStatistics();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to generate Circular Terrain System: {e.Message}");
                Debug.LogError(e.StackTrace);
            }
        }
        
        #region 地形生成プロセス
        
        private void ClearExistingTerrain()
        {
            if (generatedTiles != null)
            {
                foreach (var tile in generatedTiles)
                {
                    if (tile != null)
                        DestroyImmediate(tile);
                }
            }
            
            tileHeightmaps.Clear();
            connectionDataCache.Clear();
            generatedTiles = new GameObject[tileGridSize * tileGridSize];
            tileStats = new CircularTerrainGenerator.CircularTerrainStats[tileGridSize * tileGridSize];
        }
        
        private void GenerateBaseHeightmaps()
        {
            Debug.Log("Generating base heightmaps...");
            
            for (int y = 0; y < tileGridSize; y++)
            {
                for (int x = 0; x < tileGridSize; x++)
                {
                    var tileCoord = new Vector2Int(x - tileGridSize / 2, y - tileGridSize / 2);
                    
                    // 各タイルに異なるオフセットを適用して多様性を作る
                    var tileParams = baseTerrainParams;
                    tileParams.offset = new Vector2(
                        tileCoord.x * 123.45f,  // 疑似ランダムオフセット
                        tileCoord.y * 67.89f
                    );
                    
                    var heightmap = MeshGenerator.GenerateHeightmap(tileParams);
                    tileHeightmaps[tileCoord] = heightmap;
                }
            }
            
            Debug.Log($"✓ Generated {tileHeightmaps.Count} base heightmaps");
        }
        
        private void ApplyCircularTerrainGeneration()
        {
            Debug.Log("Applying circular terrain generation...");
            
            var processedHeightmaps = new Dictionary<Vector2Int, float[,]>();
            
            foreach (var kvp in tileHeightmaps)
            {
                var tileCoord = kvp.Key;
                var heightmap = kvp.Value;
                
                // 各タイルの中心位置を計算
                Vector2 tileCenter = new Vector2(
                    tileCoord.x * tileSpacing,
                    tileCoord.y * tileSpacing
                );
                
                // 円形パラメータを調整
                var tileCircularParams = circularParams;
                tileCircularParams.center = tileCenter;
                
                // 円形地形を生成（ハイトマップレベルで処理）
                var circularHeightmap = ApplyCircularMaskToHeightmap(heightmap, tileCircularParams);
                processedHeightmaps[tileCoord] = circularHeightmap;
            }
            
            // 処理済みハイトマップで置き換え
            tileHeightmaps = processedHeightmaps;
            
            Debug.Log("✓ Applied circular terrain generation to all tiles");
        }
        
        private float[,] ApplyCircularMaskToHeightmap(float[,] heightmap, CircularTerrainGenerator.CircularTerrainParams circularParams)
        {
            int resolution = heightmap.GetLength(0);
            float[,] result = (float[,])heightmap.Clone();
            
            Vector2 center = new Vector2(resolution * 0.5f, resolution * 0.5f);
            float radiusInPixels = circularParams.radius * resolution / baseTerrainParams.size;
            
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector2 position = new Vector2(x, y);
                    float distance = Vector2.Distance(position, center);
                    
                    // 円形フォールオフを計算
                    float falloff = CircularTerrainGenerator.CalculateDistanceFalloff(
                        position, center, radiusInPixels, 
                        circularParams.falloffCurve, circularParams.falloffStrength);
                    
                    result[y, x] *= falloff;
                }
            }
            
            return result;
        }
        
        private void ApplySeamlessConnections()
        {
            Debug.Log("Applying seamless connections...");
            
            var processedHeightmaps = new Dictionary<Vector2Int, float[,]>();
            
            foreach (var kvp in tileHeightmaps)
            {
                var tileCoord = kvp.Key;
                var heightmap = kvp.Value;
                
                // 隣接タイルを特定
                var neighbors = GetNeighborHeightmaps(tileCoord);
                
                // シームレス接続を適用
                var seamlessHeightmap = SeamlessConnectionManager.ProcessMultipleConnections(
                    heightmap, neighbors, baseTerrainParams.size, blendSettings);
                
                processedHeightmaps[tileCoord] = seamlessHeightmap;
                
                // 接続データをキャッシュ
                var connectionData = SeamlessConnectionManager.CreateConnectionData(
                    tileCoord, seamlessHeightmap, baseTerrainParams.size);
                connectionDataCache[tileCoord] = connectionData;
            }
            
            // 処理済みハイトマップで置き換え
            tileHeightmaps = processedHeightmaps;
            
            Debug.Log("✓ Applied seamless connections to all tiles");
        }
        
        private Dictionary<Vector2Int, float[,]> GetNeighborHeightmaps(Vector2Int tileCoord)
        {
            var neighbors = new Dictionary<Vector2Int, float[,]>();
            
            Vector2Int[] neighborOffsets = {
                new Vector2Int(0, 1),   // 北
                new Vector2Int(1, 0),   // 東
                new Vector2Int(0, -1),  // 南
                new Vector2Int(-1, 0)   // 西
            };
            
            foreach (var offset in neighborOffsets)
            {
                var neighborCoord = tileCoord + offset;
                if (tileHeightmaps.ContainsKey(neighborCoord))
                {
                    neighbors[offset] = tileHeightmaps[neighborCoord];
                }
            }
            
            return neighbors;
        }
        
        private void CreateTerrainMeshes()
        {
            Debug.Log("Creating terrain meshes...");
            
            int tileIndex = 0;
            
            foreach (var kvp in tileHeightmaps)
            {
                var tileCoord = kvp.Key;
                var heightmap = kvp.Value;
                
                // メッシュを生成
                var mesh = MeshGenerator.GenerateMeshFromHeightmap(heightmap, baseTerrainParams);
                
                // GameObjectを作成
                var tileObject = new GameObject($"CircularTerrain_Tile_{tileCoord.x}_{tileCoord.y}");
                tileObject.transform.position = new Vector3(
                    tileCoord.x * tileSpacing,
                    0f,
                    tileCoord.y * tileSpacing
                );
                tileObject.transform.parent = this.transform;
                
                // コンポーネントを設定
                var meshFilter = tileObject.AddComponent<MeshFilter>();
                var meshRenderer = tileObject.AddComponent<MeshRenderer>();
                var meshCollider = tileObject.AddComponent<MeshCollider>();
                
                meshFilter.mesh = mesh;
                meshCollider.sharedMesh = mesh;
                
                // マテリアルを設定
                if (terrainMaterial != null)
                {
                    meshRenderer.material = terrainMaterial;
                }
                else
                {
                    var material = CreateDefaultTerrainMaterial(tileCoord);
                    meshRenderer.material = material;
                }
                
                // 境界表示
                if (showBoundaries)
                {
                    CreateBoundaryVisualization(tileObject, tileCoord);
                }
                
                generatedTiles[tileIndex] = tileObject;
                tileIndex++;
            }
            
            Debug.Log($"✓ Created {tileIndex} terrain meshes");
        }
        
        private Material CreateDefaultTerrainMaterial(Vector2Int tileCoord)
        {
            var material = new Material(Shader.Find("Standard"));
            
            // タイル座標に基づいて色を変化
            float hue = (tileCoord.x + tileCoord.y * 0.5f) * 0.1f;
            material.color = Color.HSVToRGB(Mathf.Repeat(hue, 1f), 0.3f, 0.8f);
            
            return material;
        }
        
        private void CreateBoundaryVisualization(GameObject tileObject, Vector2Int tileCoord)
        {
            if (!connectionDataCache.ContainsKey(tileCoord))
                return;
            
            var connectionData = connectionDataCache[tileCoord];
            var boundaryObject = new GameObject("Boundary_Visualization");
            boundaryObject.transform.parent = tileObject.transform;
            boundaryObject.transform.localPosition = Vector3.zero;
            
            var lineRenderer = boundaryObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.color = boundaryColor;
            lineRenderer.startWidth = 2f;
            lineRenderer.endWidth = 2f;
            lineRenderer.positionCount = connectionData.borderVertices.Length;
            lineRenderer.useWorldSpace = false;
            
            lineRenderer.SetPositions(connectionData.borderVertices);
        }
        #endregion
        
        #region 統計と情報
        
        private void CalculateTerrainStatistics()
        {
            Debug.Log("Calculating terrain statistics...");
            
            int index = 0;
            foreach (var kvp in tileHeightmaps)
            {
                var tileCoord = kvp.Key;
                var heightmap = kvp.Value;
                
                var stats = CircularTerrainGenerator.GetCircularTerrainStats(heightmap, circularParams);
                tileStats[index] = stats;
                index++;
            }
            
            Debug.Log("✓ Calculated statistics for all tiles");
        }
        
        private void LogSystemStatistics()
        {
            Debug.Log("=== Circular Terrain System Statistics ===");
            Debug.Log($"Grid Size: {tileGridSize}x{tileGridSize}");
            Debug.Log($"Tile Spacing: {tileSpacing}m");
            Debug.Log($"Total Area: {(tileGridSize * tileSpacing) * (tileGridSize * tileSpacing) / 1000000f:F2} km²");
            Debug.Log($"Circular Terrain: {(enableCircularTerrain ? "Enabled" : "Disabled")}");
            Debug.Log($"Seamless Connection: {(enableSeamlessConnection ? "Enabled" : "Disabled")}");
            
            if (tileStats != null && tileStats.Length > 0)
            {
                float totalCircularArea = 0f;
                float avgHeight = 0f;
                
                foreach (var stats in tileStats)
                {
                    totalCircularArea += stats.circularArea;
                    avgHeight += stats.averageHeightInCircle;
                }
                
                avgHeight /= tileStats.Length;
                
                Debug.Log($"Total Circular Area: {totalCircularArea / 1000000f:F2} km²");
                Debug.Log($"Average Height: {avgHeight:F2}");
            }
            
            Debug.Log("==========================================");
        }
        #endregion
        
        #region パブリックAPI
        
        /// <summary>
        /// 特定のタイル座標の地形を再生成
        /// </summary>
        public void RegenerateTile(Vector2Int tileCoord)
        {
            if (!tileHeightmaps.ContainsKey(tileCoord))
            {
                Debug.LogWarning($"Tile at {tileCoord} does not exist");
                return;
            }
            
            Debug.Log($"Regenerating tile at {tileCoord}");
            
            // 該当タイルのGameObjectを削除
            var tileIndex = GetTileIndex(tileCoord);
            if (tileIndex >= 0 && generatedTiles[tileIndex] != null)
            {
                DestroyImmediate(generatedTiles[tileIndex]);
            }
            
            // ハイトマップを再生成
            var tileParams = baseTerrainParams;
            tileParams.offset = new Vector2(tileCoord.x * 123.45f, tileCoord.y * 67.89f);
            var heightmap = MeshGenerator.GenerateHeightmap(tileParams);
            
            // 円形地形を適用
            if (enableCircularTerrain)
            {
                var tileCircularParams = circularParams;
                tileCircularParams.center = new Vector2(tileCoord.x * tileSpacing, tileCoord.y * tileSpacing);
                heightmap = ApplyCircularMaskToHeightmap(heightmap, tileCircularParams);
            }
            
            // シームレス接続を適用
            if (enableSeamlessConnection)
            {
                var neighbors = GetNeighborHeightmaps(tileCoord);
                heightmap = SeamlessConnectionManager.ProcessMultipleConnections(
                    heightmap, neighbors, baseTerrainParams.size, blendSettings);
            }
            
            tileHeightmaps[tileCoord] = heightmap;
            
            // メッシュを再作成
            var mesh = MeshGenerator.GenerateMeshFromHeightmap(heightmap, baseTerrainParams);
            var tileObject = new GameObject($"CircularTerrain_Tile_{tileCoord.x}_{tileCoord.y}");
            tileObject.transform.position = new Vector3(tileCoord.x * tileSpacing, 0f, tileCoord.y * tileSpacing);
            tileObject.transform.parent = this.transform;
            
            var meshFilter = tileObject.AddComponent<MeshFilter>();
            var meshRenderer = tileObject.AddComponent<MeshRenderer>();
            var meshCollider = tileObject.AddComponent<MeshCollider>();
            
            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;
            meshRenderer.material = terrainMaterial ?? CreateDefaultTerrainMaterial(tileCoord);
            
            if (tileIndex >= 0)
            {
                generatedTiles[tileIndex] = tileObject;
            }
            
            Debug.Log($"✓ Regenerated tile at {tileCoord}");
        }
        
        /// <summary>
        /// 地形パラメータを更新して全体を再生成
        /// </summary>
        public void UpdateTerrainParameters()
        {
            Debug.Log("Updating terrain parameters and regenerating...");
            GenerateCircularTerrainSystem();
        }
        
        /// <summary>
        /// 特定座標の高度を取得
        /// </summary>
        public float GetHeightAtWorldPosition(Vector3 worldPosition)
        {
            var tileCoord = WorldPositionToTileCoordinate(worldPosition);
            
            if (!tileHeightmaps.ContainsKey(tileCoord))
                return 0f;
            
            var heightmap = tileHeightmaps[tileCoord];
            var localPos = WorldPositionToLocalHeightmapPosition(worldPosition, tileCoord);
            
            return SampleHeightmap(heightmap, localPos);
        }
        
        private Vector2Int WorldPositionToTileCoordinate(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPosition.x / tileSpacing),
                Mathf.RoundToInt(worldPosition.z / tileSpacing)
            );
        }
        
        private Vector2 WorldPositionToLocalHeightmapPosition(Vector3 worldPosition, Vector2Int tileCoord)
        {
            var tileCenter = new Vector3(tileCoord.x * tileSpacing, 0f, tileCoord.y * tileSpacing);
            var localPos = worldPosition - tileCenter;
            
            var normalizedPos = new Vector2(
                (localPos.x / baseTerrainParams.size) + 0.5f,
                (localPos.z / baseTerrainParams.size) + 0.5f
            );
            
            return normalizedPos;
        }
        
        private float SampleHeightmap(float[,] heightmap, Vector2 normalizedPosition)
        {
            int resolution = heightmap.GetLength(0);
            
            float x = Mathf.Clamp01(normalizedPosition.x) * (resolution - 1);
            float y = Mathf.Clamp01(normalizedPosition.y) * (resolution - 1);
            
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            int x1 = Mathf.Min(x0 + 1, resolution - 1);
            int y1 = Mathf.Min(y0 + 1, resolution - 1);
            
            float fx = x - x0;
            float fy = y - y0;
            
            float h00 = heightmap[y0, x0];
            float h10 = heightmap[y0, x1];
            float h01 = heightmap[y1, x0];
            float h11 = heightmap[y1, x1];
            
            float h0 = Mathf.Lerp(h00, h10, fx);
            float h1 = Mathf.Lerp(h01, h11, fx);
            
            return Mathf.Lerp(h0, h1, fy) * baseTerrainParams.maxHeight;
        }
        
        private int GetTileIndex(Vector2Int tileCoord)
        {
            int index = 0;
            foreach (var kvp in tileHeightmaps)
            {
                if (kvp.Key == tileCoord)
                    return index;
                index++;
            }
            return -1;
        }
        #endregion
        
        #region デバッグ機能
        
        void OnDrawGizmos()
        {
            if (!showDebugInfo || tileHeightmaps == null)
                return;
            
            Gizmos.color = Color.yellow;
            
            foreach (var kvp in tileHeightmaps)
            {
                var tileCoord = kvp.Key;
                var center = new Vector3(tileCoord.x * tileSpacing, 0f, tileCoord.y * tileSpacing);
                
                // タイル境界を描画
                Gizmos.DrawWireCube(center, new Vector3(tileSpacing, 0f, tileSpacing));
                
                // 円形範囲を描画
                if (enableCircularTerrain)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireSphere(center, circularParams.radius);
                }
            }
        }
        
        [ContextMenu("Log Detailed Statistics")]
        public void LogDetailedStatistics()
        {
            if (tileStats == null || tileStats.Length == 0)
            {
                Debug.LogWarning("No statistics available. Generate terrain first.");
                return;
            }
            
            Debug.Log("=== Detailed Terrain Statistics ===");
            
            for (int i = 0; i < tileStats.Length; i++)
            {
                var stats = tileStats[i];
                Debug.Log($"Tile {i}: Radius={stats.radius:F1}m, Area={stats.circularArea/1000000f:F3}km², AvgHeight={stats.averageHeightInCircle:F2}m");
            }
        }
        #endregion
    }
}