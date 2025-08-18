using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// 適応的地形LODシステム - プレイヤー視点と移動速度に応じた地形詳細度調整
    /// 要求: 3.5 メモリ効率とパフォーマンス
    /// </summary>
    public class AdaptiveTerrainLOD : MonoBehaviour
    {
        [Header("LOD設定")]
        [SerializeField] private bool enableAdaptiveLOD = true;
        [SerializeField] private float[] lodDistances = { 200f, 500f, 1000f, 2000f };
        [SerializeField] private int[] lodResolutions = { 512, 256, 128, 64 };
        [SerializeField] private float updateInterval = 0.5f;
        
        [Header("プレイヤー追跡")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float playerSpeedThreshold = 10f;
        [SerializeField] private float highSpeedLODBias = 1.5f;
        
        [Header("地形重要度")]
        [SerializeField] private bool useImportanceSystem = true;
        [SerializeField] private float importanceRadius = 300f;
        [SerializeField] private LayerMask importantObjectsLayer = -1;
        
        [Header("パフォーマンス制御")]
        [SerializeField] private int maxLODUpdatesPerFrame = 5;
        [SerializeField] private float targetFrameTime = 16.67f; // 60FPS
        
        // 内部状態
        private Dictionary<TerrainTile, LODData> terrainLODData;
        private Queue<TerrainTile> lodUpdateQueue;
        private Coroutine lodUpdateCoroutine;
        private Vector3 lastPlayerPosition;
        private float lastPlayerSpeed;
        private Camera playerCamera;
        
        // LODデータ構造
        [System.Serializable]
        public class LODData
        {
            public int currentLOD;
            public float lastUpdateTime;
            public float distanceToPlayer;
            public float importanceScore;
            public bool isVisible;
            public Mesh[] lodMeshes;
            public int originalResolution;
        }
        
        // 地形重要度計算用
        public enum TerrainImportance
        {
            Low = 0,
            Normal = 1,
            High = 2,
            Critical = 3
        }
        
        private void Awake()
        {
            terrainLODData = new Dictionary<TerrainTile, LODData>();
            lodUpdateQueue = new Queue<TerrainTile>();
            
            // プレイヤーカメラを取得
            if (playerTransform != null)
            {
                playerCamera = playerTransform.GetComponentInChildren<Camera>();
            }
        }
        
        private void Start()
        {
            if (enableAdaptiveLOD)
            {
                StartLODSystem();
            }
        }
        
        private void Update()
        {
            if (!enableAdaptiveLOD || playerTransform == null) return;
            
            UpdatePlayerTracking();
        }
        
        /// <summary>
        /// LODシステムを開始
        /// </summary>
        public void StartLODSystem()
        {
            if (lodUpdateCoroutine == null)
            {
                lodUpdateCoroutine = StartCoroutine(LODUpdateCoroutine());
            }
        }
        
        /// <summary>
        /// LODシステムを停止
        /// </summary>
        public void StopLODSystem()
        {
            if (lodUpdateCoroutine != null)
            {
                StopCoroutine(lodUpdateCoroutine);
                lodUpdateCoroutine = null;
            }
        }
        
        /// <summary>
        /// 地形タイルをLODシステムに登録
        /// </summary>
        public void RegisterTerrainTile(TerrainTile tile)
        {
            if (tile == null) return;
            
            var lodData = new LODData
            {
                currentLOD = 0,
                lastUpdateTime = 0f,
                distanceToPlayer = float.MaxValue,
                importanceScore = CalculateTerrainImportance(tile),
                isVisible = false,
                originalResolution = GetTerrainResolution(tile)
            };
            
            // LODメッシュを生成
            lodData.lodMeshes = GenerateLODMeshes(tile, lodData.originalResolution);
            
            terrainLODData[tile] = lodData;
            
            // 更新キューに追加
            lodUpdateQueue.Enqueue(tile);
        }
        
        /// <summary>
        /// 地形タイルをLODシステムから削除
        /// </summary>
        public void UnregisterTerrainTile(TerrainTile tile)
        {
            if (terrainLODData.ContainsKey(tile))
            {
                // LODメッシュのメモリを解放
                var lodData = terrainLODData[tile];
                if (lodData.lodMeshes != null)
                {
                    foreach (var mesh in lodData.lodMeshes)
                    {
                        if (mesh != null)
                        {
                            DestroyImmediate(mesh);
                        }
                    }
                }
                
                terrainLODData.Remove(tile);
            }
        }
        
        /// <summary>
        /// プレイヤー追跡の更新
        /// </summary>
        private void UpdatePlayerTracking()
        {
            if (playerTransform == null) return;
            
            Vector3 currentPosition = playerTransform.position;
            float currentSpeed = Vector3.Distance(currentPosition, lastPlayerPosition) / Time.deltaTime;
            
            lastPlayerPosition = currentPosition;
            lastPlayerSpeed = currentSpeed;
        }
        
        /// <summary>
        /// LOD更新コルーチン
        /// </summary>
        private IEnumerator LODUpdateCoroutine()
        {
            while (enableAdaptiveLOD)
            {
                var startTime = Time.realtimeSinceStartup;
                int updatesThisFrame = 0;
                
                // キューから地形タイルを処理
                while (lodUpdateQueue.Count > 0 && updatesThisFrame < maxLODUpdatesPerFrame)
                {
                    var tile = lodUpdateQueue.Dequeue();
                    
                    if (tile != null && terrainLODData.ContainsKey(tile))
                    {
                        UpdateTerrainLOD(tile);
                        updatesThisFrame++;
                        
                        // フレーム時間制限チェック
                        if ((Time.realtimeSinceStartup - startTime) * 1000f > targetFrameTime * 0.5f)
                        {
                            break;
                        }
                    }
                }
                
                // 処理されなかったタイルを再キューイング
                var remainingTiles = new List<TerrainTile>(terrainLODData.Keys);
                foreach (var tile in remainingTiles)
                {
                    if (ShouldUpdateLOD(tile))
                    {
                        lodUpdateQueue.Enqueue(tile);
                    }
                }
                
                yield return new WaitForSeconds(updateInterval);
            }
        }
        
        /// <summary>
        /// 地形タイルのLODを更新
        /// </summary>
        private void UpdateTerrainLOD(TerrainTile tile)
        {
            if (!terrainLODData.ContainsKey(tile)) return;
            
            var lodData = terrainLODData[tile];
            
            // プレイヤーからの距離を計算
            float distance = CalculateDistanceToPlayer(tile);
            lodData.distanceToPlayer = distance;
            
            // 視界内かどうかを判定
            lodData.isVisible = IsTerrainVisible(tile);
            
            // 新しいLODレベルを計算
            int newLOD = CalculateOptimalLOD(tile, lodData);
            
            // LODが変更された場合のみ適用
            if (newLOD != lodData.currentLOD)
            {
                ApplyLODToTerrain(tile, newLOD);
                lodData.currentLOD = newLOD;
                lodData.lastUpdateTime = Time.time;
            }
        }
        
        /// <summary>
        /// プレイヤーからの距離を計算
        /// </summary>
        private float CalculateDistanceToPlayer(TerrainTile tile)
        {
            if (playerTransform == null || tile?.terrainObject == null)
                return float.MaxValue;
            
            return Vector3.Distance(playerTransform.position, tile.terrainObject.transform.position);
        }
        
        /// <summary>
        /// 地形が視界内にあるかを判定
        /// </summary>
        private bool IsTerrainVisible(TerrainTile tile)
        {
            if (playerCamera == null || tile?.terrainObject == null)
                return false;
            
            var renderer = tile.terrainObject.GetComponent<MeshRenderer>();
            if (renderer == null) return false;
            
            // カメラの視錐台内にあるかチェック
            var planes = GeometryUtility.CalculateFrustumPlanes(playerCamera);
            return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
        }
        
        /// <summary>
        /// 最適なLODレベルを計算
        /// </summary>
        private int CalculateOptimalLOD(TerrainTile tile, LODData lodData)
        {
            float distance = lodData.distanceToPlayer;
            
            // プレイヤー速度による調整
            if (lastPlayerSpeed > playerSpeedThreshold)
            {
                distance *= highSpeedLODBias;
            }
            
            // 重要度による調整
            distance /= (1f + lodData.importanceScore);
            
            // 視界外の場合はLODを下げる
            if (!lodData.isVisible)
            {
                distance *= 2f;
            }
            
            // 距離に基づくLODレベル決定
            for (int i = 0; i < lodDistances.Length; i++)
            {
                if (distance < lodDistances[i])
                {
                    return i;
                }
            }
            
            return lodDistances.Length; // 最低LOD
        }
        
        /// <summary>
        /// 地形にLODを適用
        /// </summary>
        private void ApplyLODToTerrain(TerrainTile tile, int lodLevel)
        {
            if (!terrainLODData.ContainsKey(tile)) return;
            
            var lodData = terrainLODData[tile];
            var meshFilter = tile.terrainObject?.GetComponent<MeshFilter>();
            
            if (meshFilter == null) return;
            
            // LODメッシュを適用
            if (lodLevel < lodData.lodMeshes.Length && lodData.lodMeshes[lodLevel] != null)
            {
                meshFilter.mesh = lodData.lodMeshes[lodLevel];
            }
            else if (lodLevel >= lodData.lodMeshes.Length)
            {
                // 最低LOD - メッシュを無効化
                meshFilter.mesh = null;
                tile.terrainObject.SetActive(false);
            }
            else
            {
                // 元のメッシュを使用
                tile.terrainObject.SetActive(true);
            }
        }
        
        /// <summary>
        /// 地形の重要度を計算
        /// </summary>
        private float CalculateTerrainImportance(TerrainTile tile)
        {
            if (!useImportanceSystem || tile?.terrainObject == null)
                return 1f;
            
            float importance = 1f;
            Vector3 tilePosition = tile.terrainObject.transform.position;
            
            // 重要なオブジェクトとの距離を考慮
            var importantObjects = Physics.OverlapSphere(tilePosition, importanceRadius, importantObjectsLayer);
            
            foreach (var obj in importantObjects)
            {
                float distance = Vector3.Distance(tilePosition, obj.transform.position);
                float weight = 1f - (distance / importanceRadius);
                importance += weight;
            }
            
            return Mathf.Clamp(importance, 0.5f, 3f);
        }
        
        /// <summary>
        /// LODメッシュを生成
        /// </summary>
        private Mesh[] GenerateLODMeshes(TerrainTile tile, int originalResolution)
        {
            var lodMeshes = new Mesh[lodResolutions.Length];
            
            for (int i = 0; i < lodResolutions.Length; i++)
            {
                int targetResolution = lodResolutions[i];
                
                if (targetResolution >= originalResolution)
                {
                    // 元の解像度以上の場合は元のメッシュを使用
                    lodMeshes[i] = tile.terrainMesh;
                }
                else
                {
                    // 解像度を下げたメッシュを生成
                    lodMeshes[i] = GenerateReducedMesh(tile, targetResolution);
                }
            }
            
            return lodMeshes;
        }
        
        /// <summary>
        /// 解像度を下げたメッシュを生成
        /// </summary>
        private Mesh GenerateReducedMesh(TerrainTile tile, int targetResolution)
        {
            if (tile.heightData == null) return tile.terrainMesh;
            
            // 高さデータをダウンサンプリング
            var reducedHeightData = DownsampleHeightData(tile.heightData, targetResolution);
            
            // 新しいメッシュを生成
            return MeshGenerator.GenerateMeshFromHeightmap(reducedHeightData, tile.coordinate);
        }
        
        /// <summary>
        /// 高さデータをダウンサンプリング
        /// </summary>
        private float[,] DownsampleHeightData(float[,] originalData, int targetResolution)
        {
            int originalWidth = originalData.GetLength(0);
            int originalHeight = originalData.GetLength(1);
            
            var downsampledData = new float[targetResolution, targetResolution];
            
            float scaleX = (float)originalWidth / targetResolution;
            float scaleY = (float)originalHeight / targetResolution;
            
            for (int x = 0; x < targetResolution; x++)
            {
                for (int y = 0; y < targetResolution; y++)
                {
                    int sourceX = Mathf.FloorToInt(x * scaleX);
                    int sourceY = Mathf.FloorToInt(y * scaleY);
                    
                    sourceX = Mathf.Clamp(sourceX, 0, originalWidth - 1);
                    sourceY = Mathf.Clamp(sourceY, 0, originalHeight - 1);
                    
                    downsampledData[x, y] = originalData[sourceX, sourceY];
                }
            }
            
            return downsampledData;
        }
        
        /// <summary>
        /// 地形の解像度を取得
        /// </summary>
        private int GetTerrainResolution(TerrainTile tile)
        {
            if (tile.heightData != null)
            {
                return tile.heightData.GetLength(0);
            }
            return 512; // デフォルト解像度
        }
        
        /// <summary>
        /// LOD更新が必要かを判定
        /// </summary>
        private bool ShouldUpdateLOD(TerrainTile tile)
        {
            if (!terrainLODData.ContainsKey(tile)) return false;
            
            var lodData = terrainLODData[tile];
            return Time.time - lodData.lastUpdateTime > updateInterval;
        }
        
        /// <summary>
        /// プレイヤートランスフォームを設定
        /// </summary>
        public void SetPlayerTransform(Transform player)
        {
            playerTransform = player;
            if (player != null)
            {
                playerCamera = player.GetComponentInChildren<Camera>();
            }
        }
        
        /// <summary>
        /// LOD統計情報を取得
        /// </summary>
        public Dictionary<int, int> GetLODStatistics()
        {
            var stats = new Dictionary<int, int>();
            
            foreach (var kvp in terrainLODData)
            {
                int lod = kvp.Value.currentLOD;
                if (stats.ContainsKey(lod))
                {
                    stats[lod]++;
                }
                else
                {
                    stats[lod] = 1;
                }
            }
            
            return stats;
        }
        
        private void OnDestroy()
        {
            StopLODSystem();
            
            // メモリクリーンアップ
            foreach (var kvp in terrainLODData)
            {
                if (kvp.Value.lodMeshes != null)
                {
                    foreach (var mesh in kvp.Value.lodMeshes)
                    {
                        if (mesh != null)
                        {
                            DestroyImmediate(mesh);
                        }
                    }
                }
            }
            
            terrainLODData.Clear();
        }
    }
}