using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// 実行時地形・オブジェクト生成の負荷分散管理システム
    /// 優先度付きタスクキューとフレーム時間制限による負荷制御を実装
    /// </summary>
    public class RuntimeGenerationManager : MonoBehaviour
    {
        [Header("実行時生成設定")]
        [SerializeField] private bool enableRuntimeGeneration = true;
        [SerializeField] private float generationRadius = 1000f;
        [SerializeField] private int maxGenerationPerFrame = 5;

        [Header("負荷分散設定")]
        [SerializeField] private bool enableLoadBalancing = true;
        [SerializeField] private float targetFrameTime = 16.67f; // 60FPS target
        [SerializeField] private int maxGenerationTimeMs = 5; // 最大実行時間（ミリ秒）

        [Header("キュー管理")]
        [SerializeField] private int maxQueueSize = 100;
        [SerializeField] private bool enableQueuePrioritization = true;
        [SerializeField] private float taskTimeoutSeconds = 30f;

        [Header("デバッグ情報")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private int currentQueueSize = 0;
        [SerializeField] private int tasksProcessedThisFrame = 0;
        [SerializeField] private float lastFrameProcessingTime = 0f;

        // 優先度付きタスクキュー
        private SortedSet<GenerationTask> priorityQueue;
        private Dictionary<string, GenerationTask> activeTasksById;
        
        // コルーチン管理
        private Coroutine generationCoroutine;
        private bool isProcessingQueue = false;

        // パフォーマンス統計
        private PerformanceMonitor performanceMonitor;
        private Queue<float> frameTimeHistory;
        private const int FRAME_TIME_HISTORY_SIZE = 60;
        private float averageFrameTime = 16.67f;

        // 参照コンポーネント
        private Transform playerTransform;
        private RuntimeTerrainManager terrainManager;
        private PrimitiveTerrainManager primitiveManager;

        // イベント
        public event Action<GenerationTask> OnTaskQueued;
        public event Action<GenerationTask> OnTaskStarted;
        public event Action<GenerationTask> OnTaskCompleted;
        public event Action<GenerationTask, string> OnTaskError;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeQueue();
            InitializePerformanceTracking();
        }

        private void Start()
        {
            FindReferences();
            StartGenerationSystem();
        }

        private void Update()
        {
            UpdatePerformanceStats();
            UpdateDebugInfo();
            
            if (enableRuntimeGeneration && !isProcessingQueue)
            {
                StartGenerationCoroutine();
            }
        }

        private void OnDestroy()
        {
            StopGenerationSystem();
        }

        #endregion

        #region Initialization

        private void InitializeQueue()
        {
            priorityQueue = new SortedSet<GenerationTask>();
            activeTasksById = new Dictionary<string, GenerationTask>();
        }

        private void InitializePerformanceTracking()
        {
            frameTimeHistory = new Queue<float>();
            for (int i = 0; i < FRAME_TIME_HISTORY_SIZE; i++)
            {
                frameTimeHistory.Enqueue(16.67f);
            }

            // PerformanceMonitorを初期化
            performanceMonitor = new PerformanceMonitor();
            performanceMonitor.SetTargetFrameRate(1000f / targetFrameTime);
            performanceMonitor.Initialize();

            // パフォーマンス変化イベントを購読
            performanceMonitor.OnPerformanceImproved += OnPerformanceImproved;
            performanceMonitor.OnPerformanceDegraded += OnPerformanceDegraded;
        }

        private void FindReferences()
        {
            // プレイヤー参照を取得
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }

            // 地形管理システムを取得
            terrainManager = FindObjectOfType<RuntimeTerrainManager>();
            primitiveManager = FindObjectOfType<PrimitiveTerrainManager>();
        }

        #endregion

        #region Public API

        /// <summary>
        /// 生成タスクをキューに追加
        /// </summary>
        public bool QueueGenerationTask(GenerationTask task)
        {
            if (!enableRuntimeGeneration || task == null || !task.IsValid())
            {
                return false;
            }

            // キューサイズ制限チェック
            if (priorityQueue.Count >= maxQueueSize)
            {
                Debug.LogWarning("RuntimeGenerationManager: キューが満杯です。古いタスクを削除します。");
                RemoveOldestLowPriorityTask();
            }

            // 重複チェック
            if (activeTasksById.ContainsKey(task.taskId))
            {
                Debug.LogWarning($"RuntimeGenerationManager: 重複タスクID {task.taskId} をスキップします。");
                return false;
            }

            // キューに追加
            priorityQueue.Add(task);
            activeTasksById[task.taskId] = task;
            
            OnTaskQueued?.Invoke(task);

            if (showDebugInfo)
            {
                Debug.Log($"RuntimeGenerationManager: タスクをキューに追加 - {task}");
            }

            return true;
        }

        /// <summary>
        /// 特定タイプのタスクをキューに追加（簡易版）
        /// </summary>
        public bool QueueTask(GenerationTask.TaskType type, Vector3 position, GenerationTask.Priority priority = GenerationTask.Priority.Normal)
        {
            var task = new GenerationTask(type, position, priority);
            return QueueGenerationTask(task);
        }

        /// <summary>
        /// タスクをキャンセル
        /// </summary>
        public bool CancelTask(string taskId)
        {
            if (activeTasksById.TryGetValue(taskId, out GenerationTask task))
            {
                priorityQueue.Remove(task);
                activeTasksById.Remove(taskId);
                
                if (showDebugInfo)
                {
                    Debug.Log($"RuntimeGenerationManager: タスクをキャンセル - {taskId}");
                }
                
                return true;
            }
            return false;
        }

        /// <summary>
        /// 指定位置周辺のタスクをクリア
        /// </summary>
        public int ClearTasksAroundPosition(Vector3 position, float radius)
        {
            var tasksToRemove = new List<GenerationTask>();
            
            foreach (var task in priorityQueue)
            {
                if (Vector3.Distance(task.position, position) <= radius)
                {
                    tasksToRemove.Add(task);
                }
            }

            foreach (var task in tasksToRemove)
            {
                priorityQueue.Remove(task);
                activeTasksById.Remove(task.taskId);
            }

            return tasksToRemove.Count;
        }

        /// <summary>
        /// キューをクリア
        /// </summary>
        public void ClearQueue()
        {
            priorityQueue.Clear();
            activeTasksById.Clear();
        }

        #endregion

        #region Generation System Control

        private void StartGenerationSystem()
        {
            if (enableRuntimeGeneration && generationCoroutine == null)
            {
                StartGenerationCoroutine();
            }
        }

        private void StopGenerationSystem()
        {
            if (generationCoroutine != null)
            {
                StopCoroutine(generationCoroutine);
                generationCoroutine = null;
                isProcessingQueue = false;
            }
        }

        private void StartGenerationCoroutine()
        {
            if (generationCoroutine == null && priorityQueue.Count > 0)
            {
                generationCoroutine = StartCoroutine(ProcessGenerationQueue());
            }
        }

        #endregion

        #region Queue Processing

        /// <summary>
        /// 生成キューを処理するメインコルーチン
        /// </summary>
        private IEnumerator ProcessGenerationQueue()
        {
            isProcessingQueue = true;
            
            while (priorityQueue.Count > 0 && enableRuntimeGeneration)
            {
                var frameStartTime = Time.realtimeSinceStartup;
                tasksProcessedThisFrame = 0;

                // フレーム時間制限内でタスクを処理
                while (priorityQueue.Count > 0 && 
                       tasksProcessedThisFrame < maxGenerationPerFrame &&
                       ShouldContinueProcessing(frameStartTime))
                {
                    var task = GetNextTask();
                    if (task != null)
                    {
                        yield return StartCoroutine(ProcessSingleTask(task));
                        tasksProcessedThisFrame++;
                    }
                    else
                    {
                        break;
                    }
                }

                lastFrameProcessingTime = (Time.realtimeSinceStartup - frameStartTime) * 1000f;
                
                // 次のフレームまで待機
                yield return null;
            }

            generationCoroutine = null;
            isProcessingQueue = false;
        }

        /// <summary>
        /// 次に処理するタスクを取得
        /// </summary>
        private GenerationTask GetNextTask()
        {
            // タイムアウトしたタスクを削除
            RemoveTimedOutTasks();

            if (priorityQueue.Count == 0)
                return null;

            // 最高優先度のタスクを取得
            var task = priorityQueue.Min;
            priorityQueue.Remove(task);
            
            return task;
        }

        /// <summary>
        /// 単一タスクを処理
        /// </summary>
        private IEnumerator ProcessSingleTask(GenerationTask task)
        {
            if (task == null || !task.IsValid())
                yield break;

            try
            {
                task.StartExecution();
                OnTaskStarted?.Invoke(task);

                if (showDebugInfo)
                {
                    Debug.Log($"RuntimeGenerationManager: タスク実行開始 - {task}");
                }

                // タスクタイプに応じて処理を分岐
                GameObject result = null;
                yield return StartCoroutine(ExecuteTaskByType(task, (r) => result = r));

                // タスク完了
                task.CompleteTask(result);
                activeTasksById.Remove(task.taskId);
                OnTaskCompleted?.Invoke(task);

                if (showDebugInfo)
                {
                    Debug.Log($"RuntimeGenerationManager: タスク完了 - {task.taskId}");
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"タスク実行エラー: {ex.Message}";
                task.ErrorTask(errorMessage);
                activeTasksById.Remove(task.taskId);
                OnTaskError?.Invoke(task, errorMessage);
                
                Debug.LogError($"RuntimeGenerationManager: {errorMessage}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// タスクタイプに応じた実行処理
        /// </summary>
        private IEnumerator ExecuteTaskByType(GenerationTask task, Action<GameObject> onResult)
        {
            GameObject result = null;

            switch (task.type)
            {
                case GenerationTask.TaskType.TerrainGeneration:
                    if (terrainManager != null)
                    {
                        var coordinate = task.GetParameter<Vector2Int>("coordinate");
                        result = terrainManager.GenerateTerrainTile(coordinate)?.terrainObject;
                    }
                    break;

                case GenerationTask.TaskType.PrimitiveSpawn:
                    if (primitiveManager != null)
                    {
                        var rule = task.GetParameter<PrimitiveTerrainRule>("rule");
                        if (rule != null)
                        {
                            result = primitiveManager.SpawnPrimitiveTerrain(rule, task.position);
                        }
                    }
                    break;

                case GenerationTask.TaskType.TileCleanup:
                    var tileToClean = task.GetParameter<TerrainTile>("tile");
                    if (tileToClean != null && terrainManager != null)
                    {
                        // タイル削除処理（実装は RuntimeTerrainManager に依存）
                        if (tileToClean.terrainObject != null)
                        {
                            DestroyImmediate(tileToClean.terrainObject);
                        }
                    }
                    break;

                default:
                    Debug.LogWarning($"RuntimeGenerationManager: 未対応のタスクタイプ - {task.type}");
                    break;
            }

            onResult?.Invoke(result);
            yield return null;
        }

        #endregion

        #region Performance Management

        /// <summary>
        /// 処理を継続すべきかどうかを判定
        /// </summary>
        private bool ShouldContinueProcessing(float frameStartTime)
        {
            if (!enableLoadBalancing)
                return true;

            float elapsedMs = (Time.realtimeSinceStartup - frameStartTime) * 1000f;
            return elapsedMs < maxGenerationTimeMs;
        }

        /// <summary>
        /// パフォーマンス統計を更新
        /// </summary>
        private void UpdatePerformanceStats()
        {
            // PerformanceMonitorを更新
            performanceMonitor?.UpdatePerformance();

            // 従来の統計も維持（後方互換性のため）
            float currentFrameTime = Time.deltaTime * 1000f;
            
            frameTimeHistory.Enqueue(currentFrameTime);
            if (frameTimeHistory.Count > FRAME_TIME_HISTORY_SIZE)
            {
                frameTimeHistory.Dequeue();
            }

            // 平均フレーム時間を計算
            float total = 0f;
            foreach (float time in frameTimeHistory)
            {
                total += time;
            }
            averageFrameTime = total / frameTimeHistory.Count;

            // 動的調整
            if (enableLoadBalancing)
            {
                AdjustPerformanceSettings();
            }
        }

        /// <summary>
        /// パフォーマンスに基づいて設定を動的調整
        /// </summary>
        private void AdjustPerformanceSettings()
        {
            if (performanceMonitor != null)
            {
                // PerformanceMonitorの推奨値を使用
                int recommendedTaskCount = performanceMonitor.GetRecommendedTaskCount(maxGenerationPerFrame, 1, 10);
                float recommendedTimeMs = performanceMonitor.GetRecommendedMaxExecutionTime(maxGenerationTimeMs, 1f, 10f);

                maxGenerationPerFrame = recommendedTaskCount;
                maxGenerationTimeMs = Mathf.RoundToInt(recommendedTimeMs);

                if (showDebugInfo && (maxGenerationPerFrame != recommendedTaskCount || maxGenerationTimeMs != recommendedTimeMs))
                {
                    Debug.Log($"RuntimeGenerationManager: パフォーマンス調整 - TaskCount: {maxGenerationPerFrame}, TimeMs: {maxGenerationTimeMs}");
                }
            }
            else
            {
                // フォールバック: 従来の調整方法
                if (averageFrameTime > targetFrameTime * 1.2f) // 20%超過
                {
                    // 負荷軽減
                    maxGenerationPerFrame = Mathf.Max(1, maxGenerationPerFrame - 1);
                    maxGenerationTimeMs = Mathf.Max(1, maxGenerationTimeMs - 1);
                }
                else if (averageFrameTime < targetFrameTime * 0.8f) // 20%未満
                {
                    // 処理能力向上
                    maxGenerationPerFrame = Mathf.Min(10, maxGenerationPerFrame + 1);
                    maxGenerationTimeMs = Mathf.Min(10, maxGenerationTimeMs + 1);
                }
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// タイムアウトしたタスクを削除
        /// </summary>
        private void RemoveTimedOutTasks()
        {
            var currentTime = Time.time;
            var tasksToRemove = new List<GenerationTask>();

            foreach (var task in priorityQueue)
            {
                if (currentTime - task.creationTime > taskTimeoutSeconds)
                {
                    tasksToRemove.Add(task);
                }
            }

            foreach (var task in tasksToRemove)
            {
                priorityQueue.Remove(task);
                activeTasksById.Remove(task.taskId);
                
                if (showDebugInfo)
                {
                    Debug.Log($"RuntimeGenerationManager: タイムアウトタスクを削除 - {task.taskId}");
                }
            }
        }

        /// <summary>
        /// 最も古い低優先度タスクを削除
        /// </summary>
        private void RemoveOldestLowPriorityTask()
        {
            GenerationTask oldestLowPriority = null;
            
            foreach (var task in priorityQueue)
            {
                if (task.priority == GenerationTask.Priority.Low)
                {
                    if (oldestLowPriority == null || task.creationTime < oldestLowPriority.creationTime)
                    {
                        oldestLowPriority = task;
                    }
                }
            }

            if (oldestLowPriority != null)
            {
                priorityQueue.Remove(oldestLowPriority);
                activeTasksById.Remove(oldestLowPriority.taskId);
            }
        }

        /// <summary>
        /// デバッグ情報を更新
        /// </summary>
        private void UpdateDebugInfo()
        {
            currentQueueSize = priorityQueue.Count;
        }

        #endregion

        #region Performance Event Handlers

        /// <summary>
        /// パフォーマンス改善時の処理
        /// </summary>
        private void OnPerformanceImproved()
        {
            if (showDebugInfo)
            {
                Debug.Log("RuntimeGenerationManager: パフォーマンスが改善されました。生成処理を増加します。");
            }
        }

        /// <summary>
        /// パフォーマンス悪化時の処理
        /// </summary>
        private void OnPerformanceDegraded()
        {
            if (showDebugInfo)
            {
                Debug.Log("RuntimeGenerationManager: パフォーマンスが悪化しました。生成処理を制限します。");
            }
        }

        #endregion

        #region Public Performance API

        /// <summary>
        /// 現在のパフォーマンス統計を取得
        /// </summary>
        public PerformanceMonitor.PerformanceStats GetPerformanceStats()
        {
            return performanceMonitor?.GetCurrentStats() ?? new PerformanceMonitor.PerformanceStats();
        }

        /// <summary>
        /// パフォーマンス監視のデバッグ文字列を取得
        /// </summary>
        public string GetPerformanceDebugString()
        {
            return performanceMonitor?.GetDebugString() ?? "Performance Monitor not initialized";
        }

        /// <summary>
        /// パフォーマンス監視の有効/無効を設定
        /// </summary>
        public void SetPerformanceMonitoringEnabled(bool enabled)
        {
            performanceMonitor?.SetMonitoringEnabled(enabled);
        }

        /// <summary>
        /// ターゲットフレームレートを設定
        /// </summary>
        public void SetTargetFrameRate(float fps)
        {
            targetFrameTime = 1000f / fps;
            performanceMonitor?.SetTargetFrameRate(fps);
        }

        #endregion

        #region Public Properties

        public bool IsProcessing => isProcessingQueue;
        public int QueueSize => priorityQueue.Count;
        public float AverageFrameTime => performanceMonitor?.AverageFrameTimeMs ?? averageFrameTime;
        public int TasksProcessedThisFrame => tasksProcessedThisFrame;
        public float LastFrameProcessingTime => lastFrameProcessingTime;
        public bool IsOverloaded => performanceMonitor?.IsOverloaded ?? false;
        public float CurrentFPS => performanceMonitor?.CurrentFPS ?? (1000f / averageFrameTime);

        #endregion
    }
}