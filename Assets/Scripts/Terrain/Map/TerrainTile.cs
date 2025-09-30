using UnityEngine;
using System.Collections.Generic;
using Vastcore.Generation;

namespace Vastcore.Generation
{
    /// <summary>
    /// 地形タイルのデータ構造とライフサイクル管理
    /// 要求6.1: アクティブタイルの辞書管理とタイル座標系変換
    /// </summary>
    [System.Serializable]
    public class TerrainTile
    {
        #region データ構造
        [Header("タイル基本情報")]
        public Vector2Int coordinate;           // タイル座標
        public Vector3 worldPosition;           // ワールド座標での位置
        public float tileSize;                  // タイルサイズ
        public TileState state;                 // タイルの状態
        
        [Header("地形データ")]
        public float[,] heightmap;              // ハイトマップデータ
        public MeshGenerator.TerrainGenerationParams terrainParams;  // 地形生成パラメータ
        public CircularTerrainGenerator.CircularTerrainParams circularParams;  // 円形地形パラメータ
        
        [Header("メッシュとオブジェクト")]
        public GameObject tileObject;           // タイルのGameObject
        public Mesh terrainMesh;                // 地形メッシュ
        public Material terrainMaterial;        // 地形マテリアル
        public Collider terrainCollider;        // 地形コライダー
        
        // 互換性プロパティ（他のクラスで参照されている）
        public GameObject terrainObject => tileObject;  // tileObjectのエイリアス
        public float[,] heightData => heightmap;        // heightmapのエイリアス
        public bool isActive => state == TileState.Active; // アクティブ状態
        public System.DateTime lastAccessTime => lastAccessedAt; // 最終アクセス時間のエイリアス
        public string appliedBiome { get; set; } = "Default"; // 適用されたバイオーム
        
        [Header("生成情報")]
        public float generationTime;            // 生成にかかった時間
        public System.DateTime createdAt;       // 作成日時
        public System.DateTime lastAccessedAt; // 最終アクセス日時
        public int accessCount;                 // アクセス回数
        
        [Header("最適化情報")]
        public bool isVisible;                  // 可視状態
        public bool hasCollider;                // コライダー有効状態
        public float distanceFromPlayer;        // プレイヤーからの距離
        public LODLevel currentLOD;             // 現在のLODレベル
        
        /// <summary>
        /// タイルの状態
        /// </summary>
        public enum TileState
        {
            Unloaded,       // 未読み込み
            Loading,        // 読み込み中
            Loaded,         // 読み込み完了
            Active,         // アクティブ（表示中）
            Inactive,       // 非アクティブ（非表示）
            Unloading,      // 削除中
            Error           // エラー状態
        }
        
        /// <summary>
        /// LODレベル
        /// </summary>
        public enum LODLevel
        {
            High,           // 高品質（近距離）
            Medium,         // 中品質（中距離）
            Low,            // 低品質（遠距離）
            VeryLow         // 最低品質（最遠距離）
        }
        #endregion

        #region コンストラクタ
        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public TerrainTile()
        {
            coordinate = Vector2Int.zero;
            worldPosition = Vector3.zero;
            tileSize = 2000f;
            state = TileState.Unloaded;
            createdAt = System.DateTime.Now;
            lastAccessedAt = createdAt;
            accessCount = 0;
            isVisible = false;
            hasCollider = false;
            distanceFromPlayer = float.MaxValue;
            currentLOD = LODLevel.High;
        }
        
        /// <summary>
        /// 座標指定コンストラクタ
        /// </summary>
        public TerrainTile(Vector2Int coord, float size)
        {
            coordinate = coord;
            tileSize = size;
            worldPosition = new Vector3(coord.x * size, 0f, coord.y * size);
            state = TileState.Unloaded;
            createdAt = System.DateTime.Now;
            lastAccessedAt = createdAt;
            accessCount = 0;
            isVisible = false;
            hasCollider = false;
            distanceFromPlayer = float.MaxValue;
            currentLOD = LODLevel.High;
        }
        
        /// <summary>
        /// 完全指定コンストラクタ
        /// </summary>
        public TerrainTile(Vector2Int coord, float size, MeshGenerator.TerrainGenerationParams terrainParams, CircularTerrainGenerator.CircularTerrainParams circularParams)
        {
            coordinate = coord;
            tileSize = size;
            worldPosition = new Vector3(coord.x * size, 0f, coord.y * size);
            this.terrainParams = terrainParams;
            this.circularParams = circularParams;
            state = TileState.Unloaded;
            createdAt = System.DateTime.Now;
            lastAccessedAt = createdAt;
            accessCount = 0;
            isVisible = false;
            hasCollider = false;
            distanceFromPlayer = float.MaxValue;
            currentLOD = LODLevel.High;
        }
        #endregion

        #region ライフサイクル管理
        /// <summary>
        /// タイルを生成する
        /// </summary>
        public void GenerateTile(Transform parent = null)
        {
            if (state != TileState.Unloaded && state != TileState.Error)
            {
                Debug.LogWarning($"Tile {coordinate} is already loaded or loading");
                return;
            }
            
            state = TileState.Loading;
            float startTime = Time.realtimeSinceStartup;
            
            try
            {
                // ハイトマップを生成
                GenerateHeightmap();
                
                // メッシュを生成
                GenerateMesh();
                
                // GameObjectを作成
                CreateGameObject(parent);
                
                // 状態を更新
                state = TileState.Loaded;
                generationTime = Time.realtimeSinceStartup - startTime;
                lastAccessedAt = System.DateTime.Now;
                accessCount++;
                
                Debug.Log($"Generated tile {coordinate} in {generationTime:F3}s");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to generate tile {coordinate}: {e.Message}");
                state = TileState.Error;
            }
        }
        
        /// <summary>
        /// タイルを削除する
        /// </summary>
        public void UnloadTile()
        {
            if (state == TileState.Unloaded || state == TileState.Unloading)
            {
                return;
            }
            
            state = TileState.Unloading;
            
            try
            {
                // GameObjectを削除
                if (tileObject != null)
                {
                    if (Application.isPlaying)
                        Object.Destroy(tileObject);
                    else
                        Object.DestroyImmediate(tileObject);
                    tileObject = null;
                }
                
                // メッシュを削除
                if (terrainMesh != null)
                {
                    if (Application.isPlaying)
                        Object.Destroy(terrainMesh);
                    else
                        Object.DestroyImmediate(terrainMesh);
                    terrainMesh = null;
                }
                
                // マテリアルを削除（動的作成されたもののみ）
                if (terrainMaterial != null && terrainMaterial.name.Contains("(Clone)"))
                {
                    if (Application.isPlaying)
                        Object.Destroy(terrainMaterial);
                    else
                        Object.DestroyImmediate(terrainMaterial);
                    terrainMaterial = null;
                }
                
                // データをクリア
                heightmap = null;
                terrainCollider = null;
                
                state = TileState.Unloaded;
                
                Debug.Log($"Unloaded tile {coordinate}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to unload tile {coordinate}: {e.Message}");
                state = TileState.Error;
            }
        }
        
        /// <summary>
        /// タイルをアクティブ化する
        /// </summary>
        public void SetActive(bool active)
        {
            if (tileObject == null)
                return;
            
            tileObject.SetActive(active);
            isVisible = active;
            
            if (active)
            {
                state = TileState.Active;
                lastAccessedAt = System.DateTime.Now;
                accessCount++;
            }
            else
            {
                state = TileState.Inactive;
            }
        }
        
        /// <summary>
        /// コライダーの有効/無効を切り替える
        /// </summary>
        public void SetColliderEnabled(bool enabled)
        {
            if (terrainCollider != null)
            {
                terrainCollider.enabled = enabled;
                hasCollider = enabled;
            }
        }
        #endregion

        #region 地形生成
        /// <summary>
        /// ハイトマップを生成（高度なアルゴリズムを使用）
        /// </summary>
        private void GenerateHeightmap()
        {
            // 基本ハイトマップを生成
            heightmap = MeshGenerator.GenerateHeightmap(terrainParams);
            
            // 円形地形を適用（必要に応じて）
            if (circularParams.radius > 0)
            {
                heightmap = ApplyCircularMask(heightmap);
            }
            
            // 高度な浸食アルゴリズムを適用（浸食が有効な場合）
            if (terrainParams.enableErosion && terrainParams.erosionStrength > 0)
            {
                heightmap = ApplyAdvancedErosionToTile(heightmap);
            }
        }
        
        /// <summary>
        /// タイル用の高度な浸食処理
        /// </summary>
        private float[,] ApplyAdvancedErosionToTile(float[,] originalHeightmap)
        {
            float erosionStrength = terrainParams.erosionStrength;
            
            // 浸食強度に基づいて適切なアルゴリズムを選択
            if (erosionStrength > 0.7f)
            {
                // 強い浸食：長期地形変化シミュレーション
                int evolutionSteps = Mathf.RoundToInt(erosionStrength * 5f);
                return AdvancedTerrainAlgorithms.ApplyLongTermTerrainEvolution(originalHeightmap, evolutionSteps);
            }
            else if (erosionStrength > 0.4f)
            {
                // 中程度の浸食：統合浸食システム
                var hydraulicParams = AdvancedTerrainAlgorithms.HydraulicErosionParams.Default();
                var thermalParams = AdvancedTerrainAlgorithms.ThermalErosionParams.Default();
                var climate = AdvancedTerrainAlgorithms.ClimateConditions.Temperate();
                
                // パラメータを調整
                hydraulicParams.erodeSpeed *= erosionStrength;
                hydraulicParams.iterations = Mathf.RoundToInt(hydraulicParams.iterations * erosionStrength * 0.5f);
                thermalParams.thermalErosionRate *= erosionStrength;
                
                return AdvancedTerrainAlgorithms.ApplyIntegratedErosion(originalHeightmap, hydraulicParams, thermalParams, climate);
            }
            else if (erosionStrength > 0.2f)
            {
                // 軽い浸食：水力浸食のみ
                var hydraulicParams = AdvancedTerrainAlgorithms.HydraulicErosionParams.Default();
                hydraulicParams.erodeSpeed *= erosionStrength;
                hydraulicParams.iterations = Mathf.RoundToInt(hydraulicParams.iterations * erosionStrength * 0.3f);
                
                return AdvancedTerrainAlgorithms.ApplyHydraulicErosion(originalHeightmap, hydraulicParams);
            }
            else
            {
                // 最軽量：熱浸食のみ（斜面安定化）
                var thermalParams = AdvancedTerrainAlgorithms.ThermalErosionParams.Default();
                thermalParams.thermalErosionRate *= erosionStrength;
                thermalParams.iterations = Mathf.RoundToInt(thermalParams.iterations * 0.5f);
                
                return AdvancedTerrainAlgorithms.ApplyThermalErosion(originalHeightmap, thermalParams);
            }
        }
        
        /// <summary>
        /// 円形マスクを適用
        /// </summary>
        private float[,] ApplyCircularMask(float[,] originalHeightmap)
        {
            int resolution = originalHeightmap.GetLength(0);
            float[,] result = (float[,])originalHeightmap.Clone();
            
            Vector2 center = new Vector2(resolution * 0.5f, resolution * 0.5f);
            float radiusInPixels = circularParams.radius * resolution / tileSize;
            
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
        
        /// <summary>
        /// メッシュを生成
        /// </summary>
        private void GenerateMesh()
        {
            if (heightmap == null)
            {
                throw new System.Exception("Heightmap is null");
            }
            
            terrainMesh = MeshGenerator.GenerateMeshFromHeightmap(heightmap, terrainParams);
            terrainMesh.name = $"TerrainTile_{coordinate.x}_{coordinate.y}";
        }
        
        /// <summary>
        /// GameObjectを作成
        /// </summary>
        private void CreateGameObject(Transform parent)
        {
            if (terrainMesh == null)
            {
                throw new System.Exception("Terrain mesh is null");
            }
            
            // GameObjectを作成
            tileObject = new GameObject($"TerrainTile_{coordinate.x}_{coordinate.y}");
            tileObject.transform.position = worldPosition;
            
            if (parent != null)
            {
                tileObject.transform.parent = parent;
            }
            
            // コンポーネントを追加
            var meshFilter = tileObject.AddComponent<MeshFilter>();
            var meshRenderer = tileObject.AddComponent<MeshRenderer>();
            var meshCollider = tileObject.AddComponent<MeshCollider>();
            
            meshFilter.mesh = terrainMesh;
            meshCollider.sharedMesh = terrainMesh;
            terrainCollider = meshCollider;
            
            // マテリアルを設定
            if (terrainMaterial != null)
            {
                meshRenderer.material = terrainMaterial;
            }
            else
            {
                // デフォルトマテリアルを作成
                terrainMaterial = CreateDefaultMaterial();
                meshRenderer.material = terrainMaterial;
            }
            
            // タイル情報コンポーネントを追加
            var tileInfo = tileObject.AddComponent<TerrainTileInfo>();
            tileInfo.Initialize(this);
        }
        
        /// <summary>
        /// デフォルトマテリアルを作成
        /// </summary>
        private Material CreateDefaultMaterial()
        {
            var material = new Material(Shader.Find("Standard"));
            
            // タイル座標に基づいて色を変化
            float hue = (coordinate.x + coordinate.y * 0.5f) * 0.1f;
            material.color = Color.HSVToRGB(Mathf.Repeat(hue, 1f), 0.3f, 0.8f);
            material.name = $"TerrainTile_Material_{coordinate.x}_{coordinate.y}";
            
            return material;
        }
        #endregion

        #region LOD管理
        /// <summary>
        /// LODレベルを更新
        /// </summary>
        public void UpdateLOD(float playerDistance)
        {
            distanceFromPlayer = playerDistance;
            
            LODLevel newLOD = CalculateLODLevel(playerDistance);
            
            if (newLOD != currentLOD)
            {
                ApplyLOD(newLOD);
                currentLOD = newLOD;
            }
        }
        
        /// <summary>
        /// 距離に基づいてLODレベルを計算
        /// </summary>
        private LODLevel CalculateLODLevel(float distance)
        {
            if (distance < tileSize * 1.5f)
                return LODLevel.High;
            else if (distance < tileSize * 3f)
                return LODLevel.Medium;
            else if (distance < tileSize * 5f)
                return LODLevel.Low;
            else
                return LODLevel.VeryLow;
        }
        
        /// <summary>
        /// LODを適用
        /// </summary>
        private void ApplyLOD(LODLevel lodLevel)
        {
            if (tileObject == null)
                return;
            
            switch (lodLevel)
            {
                case LODLevel.High:
                    SetColliderEnabled(true);
                    SetActive(true);
                    break;
                    
                case LODLevel.Medium:
                    SetColliderEnabled(true);
                    SetActive(true);
                    break;
                    
                case LODLevel.Low:
                    SetColliderEnabled(false);
                    SetActive(true);
                    break;
                    
                case LODLevel.VeryLow:
                    SetColliderEnabled(false);
                    SetActive(false);
                    break;
            }
        }
        #endregion

        #region ユーティリティ
        /// <summary>
        /// ワールド座標をタイル内ローカル座標に変換
        /// </summary>
        public Vector2 WorldToLocalPosition(Vector3 worldPos)
        {
            Vector3 localPos = worldPos - worldPosition;
            return new Vector2(
                (localPos.x / tileSize) + 0.5f,
                (localPos.z / tileSize) + 0.5f
            );
        }
        
        /// <summary>
        /// タイル内ローカル座標をワールド座標に変換
        /// </summary>
        public Vector3 LocalToWorldPosition(Vector2 localPos)
        {
            return worldPosition + new Vector3(
                (localPos.x - 0.5f) * tileSize,
                0f,
                (localPos.y - 0.5f) * tileSize
            );
        }
        
        /// <summary>
        /// 指定座標の高度を取得
        /// </summary>
        public float GetHeightAtLocalPosition(Vector2 localPos)
        {
            if (heightmap == null)
                return 0f;
            
            int resolution = heightmap.GetLength(0);
            
            float x = Mathf.Clamp01(localPos.x) * (resolution - 1);
            float y = Mathf.Clamp01(localPos.y) * (resolution - 1);
            
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
            float h1 = Mathf.Lerp(h01, h11, fy);
            
            return Mathf.Lerp(h0, h1, fy) * terrainParams.maxHeight;
        }
        
        /// <summary>
        /// ワールド座標での高度を取得
        /// </summary>
        public float GetHeightAtWorldPosition(Vector3 worldPos)
        {
            Vector2 localPos = WorldToLocalPosition(worldPos);
            return GetHeightAtLocalPosition(localPos);
        }
        
        /// <summary>
        /// タイルの境界内かどうかを判定
        /// </summary>
        public bool ContainsWorldPosition(Vector3 worldPos)
        {
            Vector2 localPos = WorldToLocalPosition(worldPos);
            return localPos.x >= 0f && localPos.x <= 1f && localPos.y >= 0f && localPos.y <= 1f;
        }
        
        /// <summary>
        /// メモリ使用量を取得（概算）
        /// </summary>
        public long GetMemoryUsage()
        {
            long usage = 0;
            
            // ハイトマップのメモリ使用量
            if (heightmap != null)
            {
                usage += heightmap.Length * sizeof(float);
            }
            
            // メッシュのメモリ使用量（概算）
            if (terrainMesh != null)
            {
                usage += terrainMesh.vertices.Length * 12; // Vector3 = 12 bytes
                usage += terrainMesh.triangles.Length * 4; // int = 4 bytes
                usage += terrainMesh.uv.Length * 8; // Vector2 = 8 bytes
            }
            
            return usage;
        }
        
        /// <summary>
        /// デバッグ情報を取得
        /// </summary>
        public string GetDebugInfo()
        {
            return $"Tile {coordinate}: State={state}, LOD={currentLOD}, Distance={distanceFromPlayer:F1}m, Memory={GetMemoryUsage() / 1024}KB";
        }
        #endregion
    }
    
    /// <summary>
    /// TerrainTileの情報を保持するMonoBehaviourコンポーネント
    /// </summary>
    public class TerrainTileInfo : MonoBehaviour
    {
        [SerializeField] private TerrainTile tileData;
        
        public TerrainTile TileData => tileData;
        
        public void Initialize(TerrainTile tile)
        {
            tileData = tile;
        }
        
        void OnDrawGizmosSelected()
        {
            if (tileData != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(tileData.worldPosition, new Vector3(tileData.tileSize, 0f, tileData.tileSize));
                
                // LODレベルに応じて色を変更
                switch (tileData.currentLOD)
                {
                    case TerrainTile.LODLevel.High:
                        Gizmos.color = Color.green;
                        break;
                    case TerrainTile.LODLevel.Medium:
                        Gizmos.color = Color.yellow;
                        break;
                    case TerrainTile.LODLevel.Low:
                        Gizmos.color = Color.orange;
                        break;
                    case TerrainTile.LODLevel.VeryLow:
                        Gizmos.color = Color.red;
                        break;
                }
                
                Gizmos.DrawWireSphere(tileData.worldPosition, tileData.tileSize * 0.1f);
            }
        }
    }
}