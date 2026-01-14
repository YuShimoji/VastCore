using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Vastcore.Core;
using Vastcore.Utils;
using Vastcore.Generation;

namespace Vastcore.Generation
{
    /// <summary>
    /// プリミティブ地形管理システム
    /// 巨大プリミティブ地形オブジェクトの動的生成・管理を担当
    /// </summary>
    public class PrimitiveTerrainManager : MonoBehaviour
    {
        #region 設定
        [Header("プリミティブ地形設定")]
        [SerializeField] private List<PrimitiveTerrainRule> primitiveRules = new List<PrimitiveTerrainRule>();
        [SerializeField] private float primitiveCheckRadius = 2000f;
        [SerializeField] private int maxActivePrimitives = 20;
        [SerializeField] private float minPrimitiveScale = 50f;
        [SerializeField] private float maxPrimitiveScale = 500f;
        
        [Header("配置設定")]
        [SerializeField] private float minDistanceBetweenPrimitives = 200f;
        [SerializeField] private LayerMask terrainLayer = -1;
        [SerializeField] private bool alignToTerrainNormal = true;
        [SerializeField] private TerrainAlignmentSystem.AlignmentSettings alignmentSettings;
        
        [Header("パフォーマンス設定")]
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private int maxGenerationPerFrame = 2;
        [SerializeField] private float updateInterval = 1f;
        
        [Header("デバッグ")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool showPlacementGizmos = false;
        #endregion

        #region プライベート変数
        private Dictionary<Vector3, PrimitiveTerrainObject> activePrimitives = new Dictionary<Vector3, PrimitiveTerrainObject>();
        private List<Vector3> occupiedPositions = new List<Vector3>();
        private Transform playerTransform;
        private Coroutine updateCoroutine;
        private Queue<PrimitiveGenerationTask> generationQueue = new Queue<PrimitiveGenerationTask>();
        #endregion

        #region 初期化
        void Start()
        {
            Initialize();
        }

        void OnDestroy()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }
        }

        /// <summary>
        /// システムを初期化
        /// </summary>
        public void Initialize()
        {
            // プレイヤーを検索
            FindPlayerTransform();
            
            // デフォルト設定を適用
            if (alignmentSettings.Equals(default(TerrainAlignmentSystem.AlignmentSettings)))
            {
                alignmentSettings = TerrainAlignmentSystem.AlignmentSettings.Default();
                alignmentSettings.minDistanceBetweenObjects = minDistanceBetweenPrimitives;
            }
            
            // デフォルトルールを作成（ルールが設定されていない場合）
            if (primitiveRules.Count == 0)
            {
                CreateDefaultRules();
            }
            
            // 更新コルーチンを開始
            updateCoroutine = StartCoroutine(UpdatePrimitivesCoroutine());
            
            Debug.Log("PrimitiveTerrainManager initialized");
        }

        /// <summary>
        /// プレイヤーのTransformを検索
        /// </summary>
        private void FindPlayerTransform()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogWarning("Player not found. Using camera as fallback.");
                var camera = Camera.main;
                if (camera != null)
                {
                    playerTransform = camera.transform;
                }
            }
        }

        /// <summary>
        /// デフォルトルールを作成
        /// </summary>
        private void CreateDefaultRules()
        {
            var basicTypes = new PrimitiveTerrainGenerator.PrimitiveType[]
            {
                PrimitiveTerrainGenerator.PrimitiveType.Cube,
                PrimitiveTerrainGenerator.PrimitiveType.Sphere,
                PrimitiveTerrainGenerator.PrimitiveType.Cylinder,
                PrimitiveTerrainGenerator.PrimitiveType.Pyramid,
                PrimitiveTerrainGenerator.PrimitiveType.Crystal,
                PrimitiveTerrainGenerator.PrimitiveType.Monolith,
                PrimitiveTerrainGenerator.PrimitiveType.Mesa,
                PrimitiveTerrainGenerator.PrimitiveType.Boulder
            };

            foreach (var type in basicTypes)
            {
                var rule = PrimitiveTerrainRule.CreateDefault(type);
                // minPrimitiveScaleとmaxPrimitiveScaleを使用したスケール範囲設定
                rule.scaleRange = new Vector2(minPrimitiveScale, maxPrimitiveScale);
                primitiveRules.Add(rule);
            }
        }
        #endregion

        #region メイン更新ループ
        /// <summary>
        /// プリミティブ更新コルーチン
        /// </summary>
        private IEnumerator UpdatePrimitivesCoroutine()
        {
            while (true)
            {
                if (playerTransform != null)
                {
                    UpdatePrimitivesAroundPlayer(playerTransform.position);
                    ProcessGenerationQueue();
                }
                
                yield return new WaitForSeconds(updateInterval);
            }
        }

        /// <summary>
        /// プレイヤー周辺のプリミティブを更新
        /// </summary>
        public void UpdatePrimitivesAroundPlayer(Vector3 playerPosition)
        {
            // 遠すぎるプリミティブを削除
            DespawnDistantPrimitives(playerPosition);
            
            // 新しいプリミティブの生成をキューに追加
            QueueNewPrimitiveGeneration(playerPosition);
        }

        /// <summary>
        /// 遠距離のプリミティブを削除（プール使用）
        /// </summary>
        public void DespawnDistantPrimitives(Vector3 playerPosition)
        {
            var positionsToRemove = new List<Vector3>();
            var objectsToReturn = new List<PrimitiveTerrainObject>();
            
            foreach (var kvp in activePrimitives)
            {
                Vector3 primitivePosition = kvp.Key;
                float distance = Vector3.Distance(playerPosition, primitivePosition);
                
                if (distance > primitiveCheckRadius * 1.5f) // 少し余裕を持って削除
                {
                    positionsToRemove.Add(primitivePosition);
                    if (kvp.Value != null)
                    {
                        objectsToReturn.Add(kvp.Value);
                    }
                }
            }
            
            // メモリマネージャーから一括登録解除
            var memoryManager = PrimitiveMemoryManager.Instance;
            foreach (var obj in objectsToReturn)
            {
                memoryManager.UnregisterObject(obj);
            }
            
            // プールに一括返却
            var pool = PrimitiveTerrainObjectPool.Instance;
            pool.ReturnMultipleToPool(objectsToReturn);
            
            // 管理リストから削除
            foreach (var position in positionsToRemove)
            {
                activePrimitives.Remove(position);
                occupiedPositions.Remove(position);
            }
            
            if (showDebugInfo && positionsToRemove.Count > 0)
            {
                Debug.Log($"Returned {positionsToRemove.Count} distant primitives to pool");
            }
        }

        /// <summary>
        /// 新しいプリミティブ生成をキューに追加
        /// </summary>
        private void QueueNewPrimitiveGeneration(Vector3 playerPosition)
        {
            if (activePrimitives.Count >= maxActivePrimitives)
            {
                return;
            }
            
            // 生成候補位置を計算
            var candidatePositions = TerrainAlignmentSystem.GenerateOptimalPlacements(
                playerPosition, 
                primitiveCheckRadius, 
                maxActivePrimitives - activePrimitives.Count,
                minDistanceBetweenPrimitives,
                alignmentSettings
            );
            
            foreach (var position in candidatePositions)
            {
                // 既に存在するかチェック
                if (IsPrimitiveNearPosition(position, minDistanceBetweenPrimitives))
                {
                    continue;
                }
                
                // 適切なルールを選択
                var rule = SelectAppropriateRule(position);
                if (rule != null)
                {
                    var task = new PrimitiveGenerationTask
                    {
                        position = position,
                        rule = rule,
                        priority = CalculatePriority(position, playerPosition)
                    };
                    
                    generationQueue.Enqueue(task);
                }
            }
        }

        /// <summary>
        /// 生成キューを処理
        /// </summary>
        private void ProcessGenerationQueue()
        {
            int generated = 0;
            
            while (generationQueue.Count > 0 && generated < maxGenerationPerFrame)
            {
                var task = generationQueue.Dequeue();
                
                if (SpawnPrimitiveTerrain(task.rule, task.position) != null)
                {
                    generated++;
                }
            }
        }
        #endregion

        #region プリミティブ生成・削除
        /// <summary>
        /// プリミティブ地形を生成（プール使用）
        /// </summary>
        public PrimitiveTerrainObject SpawnPrimitiveTerrain(PrimitiveTerrainRule rule, Vector3 position)
        {
            try
            {
                // 地形情報を取得
                var terrainInfo = TerrainAlignmentSystem.GetTerrainInfoAtPosition(position);
                if (!terrainInfo.hasValidTerrain)
                {
                    Debug.LogWarning($"No valid terrain found at position {position}");
                    return null;
                }
                
                // 配置可能かチェック
                if (!rule.CanSpawnAt(position, terrainInfo.height, Vector3.Angle(Vector3.up, terrainInfo.normal)))
                {
                    return null;
                }
                
                // プールからオブジェクトを取得
                var pool = PrimitiveTerrainObjectPool.Instance;
                var primitiveComponent = pool.GetFromPool(rule.primitiveType, terrainInfo.position, Random.Range(rule.scaleRange.x, rule.scaleRange.y));
                
                if (primitiveComponent == null)
                {
                    Debug.LogWarning("Failed to get object from pool");
                    return null;
                }
                
                // 地形に整列
                if (alignToTerrainNormal)
                {
                    TerrainAlignmentSystem.AlignPrimitiveToTerrain(primitiveComponent.gameObject, terrainInfo.normal, alignmentSettings);
                }
                
                // LOD設定
                if (primitiveComponent != null)
                {
                    primitiveComponent.enableLOD = enableLOD;
                }
                
                // メモリマネージャーに登録
                var memoryManager = PrimitiveMemoryManager.Instance;
                memoryManager.RegisterObject(primitiveComponent);
                
                // 管理リストに追加
                activePrimitives[position] = primitiveComponent;
                occupiedPositions.Add(position);
                
                if (showDebugInfo)
                {
                    Debug.Log($"Spawned {rule.primitiveName} at {position} (from pool)");
                }
                
                return primitiveComponent;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error spawning primitive terrain: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// プリミティブを削除（プールに戻す）
        /// </summary>
        private void RemovePrimitive(Vector3 position)
        {
            if (activePrimitives.TryGetValue(position, out PrimitiveTerrainObject primitive))
            {
                if (primitive != null)
                {
                    // メモリマネージャーから登録解除
                    var memoryManager = PrimitiveMemoryManager.Instance;
                    memoryManager.UnregisterObject(primitive);
                    
                    // プールに戻す
                    var pool = PrimitiveTerrainObjectPool.Instance;
                    pool.ReturnToPool(primitive);
                }
                
                activePrimitives.Remove(position);
                occupiedPositions.Remove(position);
                
                if (showDebugInfo)
                {
                    Debug.Log($"Returned primitive to pool at {position}");
                }
            }
        }
        #endregion

        #region ヘルパーメソッド
        /// <summary>
        /// 生成パラメータを作成
        /// </summary>
        private PrimitiveTerrainGenerator.PrimitiveGenerationParams CreateGenerationParameters(PrimitiveTerrainRule rule, Vector3 position)
        {
            var parameters = PrimitiveTerrainGenerator.PrimitiveGenerationParams.Default(rule.primitiveType);
            
            parameters.position = position;
            parameters.scale = Vector3.one * Random.Range(rule.scaleRange.x, rule.scaleRange.y);
            parameters.enableDeformation = rule.enableDeformation;
            parameters.deformationRange = rule.deformationRange;
            parameters.noiseIntensity = rule.noiseIntensity;
            parameters.subdivisionLevel = rule.subdivisionLevel;
            
            // マテリアル設定
            if (rule.possibleMaterials != null && rule.possibleMaterials.Length > 0)
            {
                parameters.material = rule.possibleMaterials[Random.Range(0, rule.possibleMaterials.Length)];
                parameters.randomizeMaterial = rule.randomizeMaterial;
                parameters.colorVariation = rule.colorVariation;
            }
            
            return parameters;
        }

        /// <summary>
        /// 適切なルールを選択
        /// </summary>
        private PrimitiveTerrainRule SelectAppropriateRule(Vector3 position)
        {
            var terrainInfo = TerrainAlignmentSystem.GetTerrainInfoAtPosition(position);
            if (!terrainInfo.hasValidTerrain)
            {
                return null;
            }
            
            float terrainSlope = Vector3.Angle(Vector3.up, terrainInfo.normal);
            
            // 条件に合うルールをフィルタリング
            var validRules = new List<PrimitiveTerrainRule>();
            foreach (var rule in primitiveRules)
            {
                if (rule.CanSpawnAt(position, terrainInfo.height, terrainSlope))
                {
                    validRules.Add(rule);
                }
            }
            
            if (validRules.Count == 0)
            {
                return null;
            }
            
            // 確率に基づいて選択
            float totalProbability = 0f;
            foreach (var rule in validRules)
            {
                totalProbability += rule.spawnProbability;
            }
            
            float randomValue = Random.Range(0f, totalProbability);
            float currentProbability = 0f;
            
            foreach (var rule in validRules)
            {
                currentProbability += rule.spawnProbability;
                if (randomValue <= currentProbability)
                {
                    return rule;
                }
            }
            
            return validRules[validRules.Count - 1]; // フォールバック
        }

        /// <summary>
        /// 指定位置近くにプリミティブが存在するかチェック
        /// </summary>
        private bool IsPrimitiveNearPosition(Vector3 position, float minDistance)
        {
            foreach (var occupiedPosition in occupiedPositions)
            {
                if (Vector3.Distance(position, occupiedPosition) < minDistance)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 生成優先度を計算
        /// </summary>
        private int CalculatePriority(Vector3 position, Vector3 playerPosition)
        {
            float distance = Vector3.Distance(position, playerPosition);
            return Mathf.RoundToInt(1000f - distance); // 近いほど高優先度
        }
        #endregion

        #region デバッグ・可視化
        void OnDrawGizmos()
        {
            if (!showPlacementGizmos || playerTransform == null)
                return;
            
            // チェック範囲を表示
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerTransform.position, primitiveCheckRadius);
            
            // アクティブなプリミティブを表示
            Gizmos.color = Color.green;
            foreach (var position in occupiedPositions)
            {
                Gizmos.DrawWireSphere(position, minDistanceBetweenPrimitives * 0.5f);
            }
        }

        /// <summary>
        /// 統計情報を取得
        /// </summary>
        public PrimitiveTerrainStats GetStats()
        {
            var poolStats = PrimitiveTerrainObjectPool.Instance?.GetPoolStatistics() ?? new PoolStatistics();
            var memoryStats = PrimitiveMemoryManager.Instance?.GetPerformanceMetrics() ?? new PerformanceMetrics();
            
            return new PrimitiveTerrainStats
            {
                activePrimitiveCount = activePrimitives.Count,
                maxPrimitives = maxActivePrimitives,
                queuedGenerations = generationQueue.Count,
                checkRadius = primitiveCheckRadius,
                poolStats = poolStats,
                memoryStats = memoryStats
            };
        }

        /// <summary>
        /// メモリ最適化を強制実行
        /// </summary>
        public void ForceMemoryOptimization()
        {
            var memoryManager = PrimitiveMemoryManager.Instance;
            if (memoryManager != null)
            {
                memoryManager.ForceMemoryOptimization();
            }
            
            Debug.Log("Forced memory optimization for PrimitiveTerrainManager");
        }
        #endregion

        #region データ構造
        [System.Serializable]
        public struct PrimitiveGenerationTask
        {
            public Vector3 position;
            public PrimitiveTerrainRule rule;
            public int priority;
        }

        [System.Serializable]
        public struct PrimitiveTerrainStats
        {
            public int activePrimitiveCount;
            public int maxPrimitives;
            public int queuedGenerations;
            public float checkRadius;
            public PoolStatistics poolStats;
            public PerformanceMetrics memoryStats;
        }
        #endregion
    }
}