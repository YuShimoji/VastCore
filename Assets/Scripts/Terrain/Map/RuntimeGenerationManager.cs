using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// 最小実装の RuntimeGenerationManager
    /// タスクキューと簡易的な処理ループ、パフォーマンス統計、イベント通知を提供
    /// </summary>
    public class RuntimeGenerationManager : MonoBehaviour
    {
        [Header("Processing Settings")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private int maxTasksPerFrame = 5;
        [SerializeField] private float maxFrameTimeBudgetMs = 6f; // 目標: 60FPS 下で半分程度をバジェットに

        [Header("Overload Detection")]
        [SerializeField] private float overloadFrameTimeMs = 25f; // 40FPS 以下を過負荷とみなす目安
        [SerializeField] private int overloadWindow = 60;          // 直近フレームの観測数

        // タスクキュー
        private readonly Queue<GenerationTask> taskQueue = new Queue<GenerationTask>();
        private Coroutine processingLoop;
        private bool isProcessing = false;

        // パフォーマンス計測
        private readonly Queue<float> recentFrameTimes = new Queue<float>();
        private float avgFrameTimeMs = 16.67f;
        private bool isOverloaded = false;
        private int totalQueued = 0;
        private int totalCompleted = 0;
        private int totalErrored = 0;

        // イベント
        public event Action<GenerationTask> OnTaskQueued;
        public event Action<GenerationTask> OnTaskCompleted;
        public event Action<GenerationTask, string> OnTaskError;

        private void Start()
        {
            if (autoStart)
            {
                StartProcessing();
            }
        }

        public void StartProcessing()
        {
            if (processingLoop == null)
            {
                processingLoop = StartCoroutine(ProcessingCoroutine());
            }
        }

        public void StopProcessing()
        {
            if (processingLoop != null)
            {
                StopCoroutine(processingLoop);
                processingLoop = null;
            }
            isProcessing = false;
        }

        /// <summary>
        /// タスクをキューに追加
        /// </summary>
        public void QueueGenerationTask(GenerationTask task)
        {
            if (task == null) return;
            taskQueue.Enqueue(task);
            totalQueued++;
            OnTaskQueued?.Invoke(task);
        }

        /// <summary>
        /// キューをクリア
        /// </summary>
        public void ClearQueue()
        {
            taskQueue.Clear();
        }

        private IEnumerator ProcessingCoroutine()
        {
            var waitEndOfFrame = new WaitForEndOfFrame();
            while (true)
            {
                isProcessing = taskQueue.Count > 0;

                // 1フレームの処理制限
                int processed = 0;
                float frameStart = Time.realtimeSinceStartup;

                while (processed < maxTasksPerFrame && taskQueue.Count > 0)
                {
                    var task = taskQueue.Dequeue();
                    try
                    {
                        // 簡易処理: 実際の生成は行わず、即完了扱い
                        task.StartExecution();
                        // 推定時間を尊重して少しだけ待つ（負荷をシミュレート）
                        float estMs = Mathf.Clamp(task.GetEstimatedExecutionTime() * 5f, 0f, 10f);
                        if (estMs > 0f)
                        {
                            // 数ミリ秒相当のフレームをまたぐ処理はコルーチンで分割
                            // ただし過剰な待機はしない
                            // 実時間待機ではなく 1 フレーム譲る
                            yield return null;
                        }

                        task.CompleteTask(null);
                        totalCompleted++;
                        OnTaskCompleted?.Invoke(task);
                    }
                    catch (Exception e)
                    {
                        totalErrored++;
                        OnTaskError?.Invoke(task, e.Message);
                    }

                    processed++;

                    // フレーム予算を超えたら次フレームへ
                    float elapsedMs = (Time.realtimeSinceStartup - frameStart) * 1000f;
                    if (elapsedMs > maxFrameTimeBudgetMs)
                    {
                        break;
                    }
                }

                // フレーム時間を記録
                float frameTimeMs = Time.deltaTime * 1000f;
                recentFrameTimes.Enqueue(frameTimeMs);
                while (recentFrameTimes.Count > overloadWindow)
                {
                    recentFrameTimes.Dequeue();
                }
                // 平均算出
                float sum = 0f;
                foreach (var ft in recentFrameTimes) sum += ft;
                avgFrameTimeMs = recentFrameTimes.Count > 0 ? sum / recentFrameTimes.Count : frameTimeMs;
                isOverloaded = avgFrameTimeMs > overloadFrameTimeMs;

                yield return waitEndOfFrame;
            }
        }

        // 統計/デバッグ
        public Stats GetPerformanceStats()
        {
            return new Stats
            {
                queueSize = taskQueue.Count,
                totalQueued = totalQueued,
                totalCompleted = totalCompleted,
                totalErrored = totalErrored,
                averageFrameTimeMs = avgFrameTimeMs,
                currentFPS = avgFrameTimeMs > 0f ? 1000f / avgFrameTimeMs : 0f,
                isOverloaded = isOverloaded
            };
        }

        public string GetPerformanceDebugString()
        {
            var s = GetPerformanceStats();
            return $"Queue: {s.queueSize} | Proc: {IsProcessing} | Overloaded: {s.isOverloaded}\n" +
                   $"FPS: {s.currentFPS:F1} | AvgFrame: {s.averageFrameTimeMs:F2}ms | Done: {s.totalCompleted} | Err: {s.totalErrored}";
        }

        // プロパティ（テストで参照）
        public int QueueSize => taskQueue.Count;
        public bool IsProcessing => isProcessing;
        public bool IsOverloaded => isOverloaded;
        public float CurrentFPS => avgFrameTimeMs > 0f ? 1000f / avgFrameTimeMs : 0f;
        public float AverageFrameTime => avgFrameTimeMs;

        [Serializable]
        public struct Stats
        {
            public int queueSize;
            public int totalQueued;
            public int totalCompleted;
            public int totalErrored;
            public float averageFrameTimeMs;
            public float currentFPS;
            public bool isOverloaded;
        }
    }
}
