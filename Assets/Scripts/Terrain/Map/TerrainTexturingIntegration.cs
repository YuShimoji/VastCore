using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Vastcore.Generation.Map;

namespace Vastcore.Generation
{
    /// <summary>
    /// 地形テクスチャリングシステム統合クラス
    /// 既存のRuntimeTerrainManagerとBiomePresetManagerとの統合を管理
    /// 要求2.1: リアルタイムでの環境変化の反映
    /// </summary>
    public class TerrainTexturingIntegration : MonoBehaviour
    {
        #region 設定パラメータ
        [Header("統合設定")]
        public bool enableAutoIntegration = true;
        public float integrationUpdateInterval = 0.5f;
        public float textureUpdateRadius = 2000f;
        
        [Header("システム参照")]
        public RuntimeTerrainManager terrainManager;
        public BiomePresetManager biomePresetManager;
        public TerrainTexturingSystem texturingSystem;
        public DynamicMaterialBlendingSystem blendingSystem;
        
        [Header("自動テクスチャ適用")]
        public bool autoApplyTexturesOnTileGeneration = true;
        public bool autoApplyBiomeTextures = true;
        public bool autoUpdateEnvironmentalTextures = true;
        public bool autoUpdateLODTextures = true;
        
        [Header("パフォーマンス制御")]
        public int maxTextureUpdatesPerFrame = 3;
        public float maxFrameTimeMs = 10f;
        public bool enableFrameTimeControl = true;
        #endregion

        #region プライベート変数
        private Dictionary<TerrainTile, TextureIntegrationData> tileTextureData = new Dictionary<TerrainTile, TextureIntegrationData>();
        private Queue<TextureIntegrationRequest> integrationQueue = new Queue<TextureIntegrationRequest>();
        private Transform playerTransform;
        
        private float lastIntegrationUpdate = 0f;
        
        // パフォーマンス統計
        private IntegrationStatistics statistics = new IntegrationStatistics();
        #endregion

        #region Unity イベント
        void Start()
        {
            InitializeIntegration();
        }
        
        void Update()
        {
            if (enableAutoIntegration && Time.time - lastIntegrationUpdate >= integrationUpdateInterval)
            {
                UpdateIntegration();
                lastIntegrationUpdate = Time.time;
            }
            
            ProcessIntegrationQueue();
        }
        
        void OnDestroy()
        {
            CleanupIntegration();
        }
        #endregion

        #region 初期化
        /// <summary>
        /// 統合システムを初期化
        /// </summary>
        private void InitializeIntegration()
        {
            Debug.Log("Initializing TerrainTexturingIntegration...");
            
            // 必要なコンポーネントを取得または作成
            if (terrainManager == null)
                terrainManager = FindFirstObjectByType<RuntimeTerrainManager>();
            
            if (biomePresetManager == null)
                biomePresetManager = FindFirstObjectByType<BiomePresetManager>();
            
            if (texturingSystem == null)
            {
                texturingSystem = GetComponent<TerrainTexturingSystem>();
                if (texturingSystem == null)
                    texturingSystem = gameObject.AddComponent<TerrainTexturingSystem>();
            }
            
            if (blendingSystem == null)
            {
                blendingSystem = GetComponent<DynamicMaterialBlendingSystem>();
                if (blendingSystem == null)
                    blendingSystem = gameObject.AddComponent<DynamicMaterialBlendingSystem>();
            }
            
            // プレイヤーTransformを取得
            var playerController = FindFirstObjectByType<AdvancedPlayerController>();
            if (playerController != null)
            {
                playerTransform = playerController.transform;
            }
            else if (playerTransform == null)
            {
                playerTransform = ResolvePlayerTransform();
            }
            
            // イベントハンドラーを登録
            RegisterEventHandlers();
            
            Debug.Log("TerrainTexturingIntegration initialized successfully");
        }
        
        /// <summary>
        /// イベントハンドラーを登録
        /// </summary>
        private void RegisterEventHandlers()
        {
            // RuntimeTerrainManagerのイベントに登録（可能な場合）
            // 注意: 実際のイベントシステムが実装されている場合のみ
            
            // 代替として、定期的なポーリングで新しいタイルを検出
            StartCoroutine(MonitorNewTiles());
        }
        
        /// <summary>
        /// 新しいタイルを監視
        /// </summary>
        private IEnumerator MonitorNewTiles()
        {
            while (true)
            {
                yield return new WaitForSeconds(integrationUpdateInterval);
                
                if (terrainManager != null)
                {
                    CheckForNewTiles();
                }
            }
        }
        #endregion

        #region 統合処理
        /// <summary>
        /// 統合を更新
        /// </summary>
        private void UpdateIntegration()
        {
            if (playerTransform == null)
                return;
            
            // プレイヤー周辺のタイルを更新
            UpdateNearbyTileTextures();
            
            // 環境変化を適用
            if (autoUpdateEnvironmentalTextures)
            {
                UpdateEnvironmentalTextures();
            }
            
            // LODを更新
            if (autoUpdateLODTextures)
            {
                UpdateLODTextures();
            }
            
            // 統計を更新
            UpdateStatistics();
        }
        
        /// <summary>
        /// 新しいタイルをチェック
        /// </summary>
        private void CheckForNewTiles()
        {
            // RuntimeTerrainManagerから現在アクティブなタイルを取得
            // 注意: 実際のAPIに応じて実装を調整
            
            if (playerTransform == null)
                return;
            
            Vector3 playerPos = playerTransform.position;
            
            // プレイヤー周辺の範囲でタイルをチェック
            int tileRadius = Mathf.CeilToInt(textureUpdateRadius / 1000f); // タイルサイズを1000mと仮定
            
            for (int x = -tileRadius; x <= tileRadius; x++)
            {
                for (int y = -tileRadius; y <= tileRadius; y++)
                {
                    Vector3 tileWorldPos = playerPos + new Vector3(x * 1000f, 0f, y * 1000f);
                    
                    // この位置にタイルが存在するかチェック（仮想的な実装）
                    var tile = GetTileAtWorldPosition(tileWorldPos);
                    if (tile != null && !tileTextureData.ContainsKey(tile))
                    {
                        OnNewTileDetected(tile);
                    }
                }
            }
        }
        
        /// <summary>
        /// 新しいタイルが検出された時の処理
        /// </summary>
        private void OnNewTileDetected(TerrainTile tile)
        {
            Debug.Log($"New tile detected: {tile.coordinate}");
            
            // テクスチャ統合データを作成
            var integrationData = new TextureIntegrationData(tile);
            tileTextureData[tile] = integrationData;
            
            // 自動テクスチャ適用
            if (autoApplyTexturesOnTileGeneration)
            {
                RequestTextureApplication(tile, TextureApplicationType.Initial);
            }
            
            // バイオームテクスチャ適用
            if (autoApplyBiomeTextures)
            {
                ApplyBiomeTextureToTile(tile);
            }
            
            statistics.tilesProcessed++;
        }
        
        /// <summary>
        /// 近くのタイルテクスチャを更新
        /// </summary>
        private void UpdateNearbyTileTextures()
        {
            Vector3 playerPos = playerTransform.position;
            
            foreach (var kvp in tileTextureData.ToArray())
            {
                var tile = kvp.Key;
                var integrationData = kvp.Value;
                
                if (tile == null || tile.tileObject == null)
                {
                    tileTextureData.Remove(tile);
                    continue;
                }
                
                float distance = Vector3.Distance(playerPos, tile.worldPosition);
                
                if (distance <= textureUpdateRadius)
                {
                    // 更新が必要かチェック
                    if (ShouldUpdateTileTexture(tile, integrationData))
                    {
                        RequestTextureApplication(tile, TextureApplicationType.Update);
                    }
                }
                else if (distance > textureUpdateRadius * 2f)
                {
                    // 遠すぎるタイルは削除
                    tileTextureData.Remove(tile);
                    texturingSystem.CleanupTextureData(tile);
                }
            }
        }
        
        /// <summary>
        /// タイルテクスチャの更新が必要かチェック
        /// </summary>
        private bool ShouldUpdateTileTexture(TerrainTile tile, TextureIntegrationData integrationData)
        {
            float timeSinceLastUpdate = Time.time - integrationData.lastTextureUpdate;
            
            // 定期更新
            if (timeSinceLastUpdate > 10f)
                return true;
            
            // 距離変化による更新
            float currentDistance = Vector3.Distance(playerTransform.position, tile.worldPosition);
            float distanceChange = Mathf.Abs(currentDistance - integrationData.lastUpdateDistance);
            if (distanceChange > 200f)
                return true;
            
            // バイオーム変化による更新
            if (integrationData.needsBiomeUpdate)
                return true;
            
            return false;
        }
        
        /// <summary>
        /// 環境テクスチャを更新
        /// </summary>
        private void UpdateEnvironmentalTextures()
        {
            var currentConditions = GetCurrentEnvironmentalConditions();
            
            foreach (var kvp in tileTextureData)
            {
                var tile = kvp.Key;
                var integrationData = kvp.Value;
                
                if (tile != null && HasEnvironmentalChange(integrationData, currentConditions))
                {
                    blendingSystem.ApplyEnvironmentalBlend(tile, currentConditions);
                    integrationData.lastEnvironmentalConditions = currentConditions;
                    integrationData.lastEnvironmentalUpdate = Time.time;
                }
            }
        }
        
        /// <summary>
        /// LODテクスチャを更新
        /// </summary>
        private void UpdateLODTextures()
        {
            Vector3 playerPos = playerTransform.position;
            
            foreach (var kvp in tileTextureData)
            {
                var tile = kvp.Key;
                var integrationData = kvp.Value;
                
                if (tile != null)
                {
                    float currentDistance = Vector3.Distance(playerPos, tile.worldPosition);
                    float distanceChange = Mathf.Abs(currentDistance - integrationData.lastLODDistance);
                    
                    if (distanceChange > 100f) // LOD更新閾値
                    {
                        tile.distanceFromPlayer = currentDistance;
                        blendingSystem.ApplyDistanceLODBlend(tile);
                        integrationData.lastLODDistance = currentDistance;
                        integrationData.lastLODUpdate = Time.time;
                    }
                }
            }
        }
        #endregion

        #region テクスチャ適用
        /// <summary>
        /// テクスチャ適用をリクエスト
        /// </summary>
        private void RequestTextureApplication(TerrainTile tile, TextureApplicationType applicationType)
        {
            var request = new TextureIntegrationRequest
            {
                tile = tile,
                applicationType = applicationType,
                priority = CalculateRequestPriority(tile),
                requestTime = Time.time
            };
            
            integrationQueue.Enqueue(request);
        }
        
        /// <summary>
        /// バイオームテクスチャをタイルに適用
        /// </summary>
        private void ApplyBiomeTextureToTile(TerrainTile tile)
        {
            if (biomePresetManager == null)
                return;
            
            // タイル位置に基づいてバイオームを決定
            var biomePreset = DetermineBiomeForTile(tile);
            
            if (biomePreset != null)
            {
                texturingSystem.ApplyBiomeTextures(tile, biomePreset);
                blendingSystem.ApplyBiomeBlend(tile, biomePreset);
                
                if (tileTextureData.ContainsKey(tile))
                {
                    tileTextureData[tile].currentBiome = biomePreset;
                    tileTextureData[tile].lastBiomeUpdate = Time.time;
                    tileTextureData[tile].needsBiomeUpdate = false;
                }
            }
        }
        
        /// <summary>
        /// タイルのバイオームを決定
        /// </summary>
        private BiomePreset DetermineBiomeForTile(TerrainTile tile)
        {
            if (biomePresetManager == null || biomePresetManager.availablePresets.Count == 0)
                return null;
            
            // 簡易実装: タイル座標に基づいてバイオームを選択
            int biomeIndex = (Mathf.Abs(tile.coordinate.x) + Mathf.Abs(tile.coordinate.y)) % biomePresetManager.availablePresets.Count;
            return biomePresetManager.availablePresets[biomeIndex];
        }
        
        /// <summary>
        /// 統合キューを処理
        /// </summary>
        private void ProcessIntegrationQueue()
        {
            float frameStartTime = Time.realtimeSinceStartup;
            int processedCount = 0;
            
            while (integrationQueue.Count > 0 && processedCount < maxTextureUpdatesPerFrame)
            {
                // フレーム時間制御
                if (enableFrameTimeControl)
                {
                    float elapsedTime = (Time.realtimeSinceStartup - frameStartTime) * 1000f;
                    if (elapsedTime > maxFrameTimeMs && processedCount > 0)
                    {
                        break;
                    }
                }
                
                var request = integrationQueue.Dequeue();
                ProcessIntegrationRequest(request);
                processedCount++;
                
                statistics.requestsProcessed++;
            }
        }
        
        /// <summary>
        /// 統合リクエストを処理
        /// </summary>
        private void ProcessIntegrationRequest(TextureIntegrationRequest request)
        {
            if (request.tile == null || request.tile.tileObject == null)
                return;
            
            try
            {
                switch (request.applicationType)
                {
                    case TextureApplicationType.Initial:
                        texturingSystem.ApplyTextureToTile(request.tile);
                        ApplyBiomeTextureToTile(request.tile);
                        break;
                        
                    case TextureApplicationType.Update:
                        texturingSystem.ApplyTextureToTile(request.tile);
                        break;
                        
                    case TextureApplicationType.BiomeChange:
                        ApplyBiomeTextureToTile(request.tile);
                        break;
                        
                    case TextureApplicationType.Environmental:
                        var conditions = GetCurrentEnvironmentalConditions();
                        blendingSystem.ApplyEnvironmentalBlend(request.tile, conditions);
                        break;
                        
                    case TextureApplicationType.LOD:
                        blendingSystem.ApplyDistanceLODBlend(request.tile);
                        break;
                }
                
                // 統合データを更新
                if (tileTextureData.ContainsKey(request.tile))
                {
                    var integrationData = tileTextureData[request.tile];
                    integrationData.lastTextureUpdate = Time.time;
                    integrationData.lastUpdateDistance = Vector3.Distance(playerTransform.position, request.tile.worldPosition);
                    integrationData.updateCount++;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to process texture integration request for tile {request.tile.coordinate}: {e.Message}");
                statistics.processingErrors++;
            }
        }
        #endregion

        #region ユーティリティ
        /// <summary>
        /// ワールド位置のタイルを取得（仮想実装）
        /// </summary>
        private TerrainTile GetTileAtWorldPosition(Vector3 worldPos)
        {
            // 実際の実装では、RuntimeTerrainManagerのAPIを使用
            // ここでは仮想的な実装
            return null;
        }
        
        /// <summary>
        /// リクエスト優先度を計算
        /// </summary>
        private int CalculateRequestPriority(TerrainTile tile)
        {
            if (playerTransform == null)
                return 1;
            
            float distance = Vector3.Distance(playerTransform.position, tile.worldPosition);
            
            if (distance < 500f)
                return 3; // 高優先度
            else if (distance < 1000f)
                return 2; // 中優先度
            else
                return 1; // 低優先度
        }
        
        /// <summary>
        /// 現在の環境条件を取得
        /// </summary>
        private EnvironmentalConditions GetCurrentEnvironmentalConditions()
        {
            return new EnvironmentalConditions
            {
                season = GetCurrentSeason(),
                temperature = GetCurrentTemperature(),
                moisture = GetCurrentMoisture(),
                timeOfDay = GetCurrentTimeOfDay()
            };
        }
        
        /// <summary>
        /// 環境変化があるかチェック
        /// </summary>
        private bool HasEnvironmentalChange(TextureIntegrationData integrationData, EnvironmentalConditions currentConditions)
        {
            var lastConditions = integrationData.lastEnvironmentalConditions;
            
            return Mathf.Abs(currentConditions.temperature - lastConditions.temperature) > 0.1f ||
                   Mathf.Abs(currentConditions.moisture - lastConditions.moisture) > 0.1f ||
                   Mathf.Abs(currentConditions.timeOfDay - lastConditions.timeOfDay) > 0.1f ||
                   currentConditions.season != lastConditions.season;
        }
        
        /// <summary>
        /// 統計を更新
        /// </summary>
        private void UpdateStatistics()
        {
            statistics.activeTiles = tileTextureData.Count;
            statistics.queuedRequests = integrationQueue.Count;
            statistics.frameRate = 1f / Time.deltaTime;
            statistics.memoryUsage = System.GC.GetTotalMemory(false) / (1024 * 1024);
        }
        
        // 環境データ取得メソッド（簡易実装）
        private Season GetCurrentSeason()
        {
            float seasonTime = (Time.time / 300f) % 4f;
            return (Season)Mathf.FloorToInt(seasonTime);
        }
        
        private float GetCurrentTemperature()
        {
            Season season = GetCurrentSeason();
            float baseTemp = season == Season.Summer ? 0.8f : season == Season.Winter ? 0.2f : 0.5f;
            return Mathf.Clamp01(baseTemp + Mathf.Sin(Time.time * 0.1f) * 0.1f);
        }
        
        private float GetCurrentMoisture()
        {
            return Mathf.Clamp01(0.5f + Mathf.Sin(Time.time * 0.05f) * 0.3f);
        }
        
        private float GetCurrentTimeOfDay()
        {
            return (Time.time * 0.01f) % 1f;
        }
        #endregion

        #region パブリックAPI
        /// <summary>
        /// 手動でタイルテクスチャを更新
        /// </summary>
        public void UpdateTileTexture(TerrainTile tile)
        {
            if (tile != null)
            {
                RequestTextureApplication(tile, TextureApplicationType.Update);
            }
        }
        
        /// <summary>
        /// バイオーム変更を適用
        /// </summary>
        public void ApplyBiomeChange(TerrainTile tile, BiomePreset newBiome)
        {
            if (tile != null && newBiome != null)
            {
                texturingSystem.ApplyBiomeTextures(tile, newBiome);
                blendingSystem.ApplyBiomeBlend(tile, newBiome);
                
                if (tileTextureData.ContainsKey(tile))
                {
                    tileTextureData[tile].currentBiome = newBiome;
                    tileTextureData[tile].needsBiomeUpdate = false;
                }
            }
        }
        
        /// <summary>
        /// 統計情報を取得
        /// </summary>
        public IntegrationStatistics GetStatistics()
        {
            return statistics;
        }
        
        /// <summary>
        /// 統合を有効/無効化
        /// </summary>
        public void SetIntegrationEnabled(bool enabled)
        {
            enableAutoIntegration = enabled;
        }
        #endregion

        #region クリーンアップ
        /// <summary>
        /// 統合をクリーンアップ
        /// </summary>
        private void CleanupIntegration()
        {
            foreach (var kvp in tileTextureData)
            {
                texturingSystem.CleanupTextureData(kvp.Key);
            }
            
            tileTextureData.Clear();
            integrationQueue.Clear();
        }
        #endregion
    }
    
    /// <summary>
    /// テクスチャ統合データ
    /// </summary>
    [System.Serializable]
    public class TextureIntegrationData
    {
        public TerrainTile tile;
        public BiomePreset currentBiome;
        public EnvironmentalConditions lastEnvironmentalConditions = new EnvironmentalConditions();
        
        public float lastTextureUpdate = 0f;
        public float lastBiomeUpdate = 0f;
        public float lastEnvironmentalUpdate = 0f;
        public float lastLODUpdate = 0f;
        
        public float lastUpdateDistance = 0f;
        public float lastLODDistance = 0f;
        
        public bool needsBiomeUpdate = false;
        public int updateCount = 0;
        
        public TextureIntegrationData(TerrainTile associatedTile)
        {
            tile = associatedTile;
            lastTextureUpdate = Time.time;
        }
    }
    
    /// <summary>
    /// テクスチャ統合リクエスト
    /// </summary>
    [System.Serializable]
    public class TextureIntegrationRequest
    {
        public TerrainTile tile;
        public TextureApplicationType applicationType;
        public int priority;
        public float requestTime;
    }
    
    /// <summary>
    /// 統合統計
    /// </summary>
    [System.Serializable]
    public class IntegrationStatistics
    {
        public int tilesProcessed = 0;
        public int requestsProcessed = 0;
        public int processingErrors = 0;
        public int activeTiles = 0;
        public int queuedRequests = 0;
        public float frameRate = 0f;
        public long memoryUsage = 0;
    }
    
    /// <summary>
    /// テクスチャ適用タイプ
    /// </summary>
    public enum TextureApplicationType
    {
        Initial,        // 初期適用
        Update,         // 更新
        BiomeChange,    // バイオーム変更
        Environmental,  // 環境変化
        LOD            // LOD変更
    }
}