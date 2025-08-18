using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// フレーム時間監視とパフォーマンス統計収集システム
    /// RuntimeGenerationManagerの負荷制御をサポート
    /// </summary>
    [System.Serializable]
    public class PerformanceMonitor
    {
        [Header("監視設定")]
        [SerializeField] private bool enableMonitoring = true;
        [SerializeField] private float targetFrameRate = 60f;
        [SerializeField] private int historySize = 120; // 2秒分のフレーム履歴
        
        [Header("統計情報")]
        [SerializeField] private float currentFPS = 60f;
        [SerializeField] private float averageFPS = 60f;
        [SerializeField] private float minFPS = 60f;
        [SerializeField] private float maxFPS = 60f;
        [SerializeField] private float frameTimeMs = 16.67f;
        [SerializeField] private float averageFrameTimeMs = 16.67f;

        [Header("負荷制御")]
        [SerializeField] private bool isOverloaded = false;
        [SerializeField] private float overloadThreshold = 0.8f; // 80%のフレーム時間を超えたら過負荷
        [SerializeField] private int consecutiveOverloadFrames = 0;
        [SerializeField] private int maxConsecutiveOverloadFrames = 5;

        // 内部データ
        private Queue<float> frameTimeHistory;
        private Queue<float> fpsHistory;
        private float lastFrameTime;
        private float targetFrameTimeMs;
        private int frameCount = 0;
        
        // 統計計算用
        private float totalFrameTime = 0f;
        private float totalFPS = 0f;

        // イベント
        public event Action OnPerformanceImproved;
        public event Action OnPerformanceDegraded;
        public event Action<PerformanceStats> OnStatsUpdated;

        /// <summary>
        /// パフォーマンス統計データ構造
        /// </summary>
        [System.Serializable]
        public struct PerformanceStats
        {
            public float currentFPS;
            public float averageFPS;
            public float minFPS;
            public float maxFPS;
            public float frameTimeMs;
            public float averageFrameTimeMs;
            public bool isOverloaded;
            public int totalFrames;
            public float monitoringDuration;
        }

        /// <summary>
        /// 初期化
        /// </summary>
        public void Initialize()
        {
            targetFrameTimeMs = 1000f / targetFrameRate;
            frameTimeHistory = new Queue<float>();
            fpsHistory = new Queue<float>();
            
            // 初期値で履歴を埋める
            for (int i = 0; i < historySize; i++)
            {
                frameTimeHistory.Enqueue(targetFrameTimeMs);
                fpsHistory.Enqueue(targetFrameRate);
            }
            
            totalFrameTime = targetFrameTimeMs * historySize;
            totalFPS = targetFrameRate * historySize;
            
            lastFrameTime = Time.realtimeSinceStartup;
            frameCount = 0;
            consecutiveOverloadFrames = 0;
            isOverloaded = false;
        }

        /// <summary>
        /// フレーム毎の更新処理
        /// </summary>
        public void UpdatePerformance()
        {
            if (!enableMonitoring) return;

            float currentTime = Time.realtimeSinceStartup;
            float deltaTime = currentTime - lastFrameTime;
            lastFrameTime = currentTime;

            // フレーム時間とFPSを計算
            frameTimeMs = deltaTime * 1000f;
            currentFPS = 1f / deltaTime;

            // 履歴を更新
            UpdateHistory();

            // 統計を計算
            CalculateStatistics();

            // 負荷状態を判定
            UpdateOverloadStatus();

            // イベント発火
            OnStatsUpdated?.Invoke(GetCurrentStats());

            frameCount++;
        }

        /// <summary>
        /// 履歴データを更新
        /// </summary>
        private void UpdateHistory()
        {
            // 古いデータを削除して新しいデータを追加
            if (frameTimeHistory.Count >= historySize)
            {
                float oldFrameTime = frameTimeHistory.Dequeue();
                float oldFPS = fpsHistory.Dequeue();
                
                totalFrameTime -= oldFrameTime;
                totalFPS -= oldFPS;
            }

            frameTimeHistory.Enqueue(frameTimeMs);
            fpsHistory.Enqueue(currentFPS);
            
            totalFrameTime += frameTimeMs;
            totalFPS += currentFPS;
        }

        /// <summary>
        /// 統計値を計算
        /// </summary>
        private void CalculateStatistics()
        {
            int count = frameTimeHistory.Count;
            if (count == 0) return;

            // 平均値
            averageFrameTimeMs = totalFrameTime / count;
            averageFPS = totalFPS / count;

            // 最小・最大値
            minFPS = float.MaxValue;
            maxFPS = float.MinValue;

            foreach (float fps in fpsHistory)
            {
                if (fps < minFPS) minFPS = fps;
                if (fps > maxFPS) maxFPS = fps;
            }
        }

        /// <summary>
        /// 過負荷状態を更新
        /// </summary>
        private void UpdateOverloadStatus()
        {
            bool wasOverloaded = isOverloaded;
            
            // 現在のフレーム時間が閾値を超えているかチェック
            bool currentFrameOverloaded = frameTimeMs > (targetFrameTimeMs * (1f + overloadThreshold));
            
            if (currentFrameOverloaded)
            {
                consecutiveOverloadFrames++;
            }
            else
            {
                consecutiveOverloadFrames = 0;
            }

            // 連続して過負荷フレームが続いた場合に過負荷状態とする
            isOverloaded = consecutiveOverloadFrames >= maxConsecutiveOverloadFrames;

            // 状態変化イベント
            if (!wasOverloaded && isOverloaded)
            {
                OnPerformanceDegraded?.Invoke();
            }
            else if (wasOverloaded && !isOverloaded)
            {
                OnPerformanceImproved?.Invoke();
            }
        }

        /// <summary>
        /// 現在の統計情報を取得
        /// </summary>
        public PerformanceStats GetCurrentStats()
        {
            return new PerformanceStats
            {
                currentFPS = this.currentFPS,
                averageFPS = this.averageFPS,
                minFPS = this.minFPS,
                maxFPS = this.maxFPS,
                frameTimeMs = this.frameTimeMs,
                averageFrameTimeMs = this.averageFrameTimeMs,
                isOverloaded = this.isOverloaded,
                totalFrames = this.frameCount,
                monitoringDuration = frameCount / targetFrameRate
            };
        }

        /// <summary>
        /// 推奨される生成タスク数を計算
        /// </summary>
        public int GetRecommendedTaskCount(int baseTaskCount, int minTasks = 1, int maxTasks = 10)
        {
            if (!enableMonitoring) return baseTaskCount;

            float performanceRatio = targetFrameRate / averageFPS;
            
            // パフォーマンスが良い場合はタスク数を増やし、悪い場合は減らす
            int recommendedCount = Mathf.RoundToInt(baseTaskCount / performanceRatio);
            
            // 過負荷状態では更に制限
            if (isOverloaded)
            {
                recommendedCount = Mathf.Max(1, recommendedCount / 2);
            }
            
            return Mathf.Clamp(recommendedCount, minTasks, maxTasks);
        }

        /// <summary>
        /// 推奨される最大実行時間を計算（ミリ秒）
        /// </summary>
        public float GetRecommendedMaxExecutionTime(float baseTimeMs, float minTimeMs = 1f, float maxTimeMs = 10f)
        {
            if (!enableMonitoring) return baseTimeMs;

            // 現在のフレーム時間に基づいて調整
            float availableTime = targetFrameTimeMs - averageFrameTimeMs;
            float recommendedTime = Mathf.Min(baseTimeMs, availableTime * 0.5f); // 利用可能時間の50%まで

            // 過負荷状態では更に制限
            if (isOverloaded)
            {
                recommendedTime *= 0.5f;
            }

            return Mathf.Clamp(recommendedTime, minTimeMs, maxTimeMs);
        }

        /// <summary>
        /// パフォーマンス状態をリセット
        /// </summary>
        public void Reset()
        {
            frameTimeHistory?.Clear();
            fpsHistory?.Clear();
            
            frameCount = 0;
            consecutiveOverloadFrames = 0;
            isOverloaded = false;
            totalFrameTime = 0f;
            totalFPS = 0f;
            
            Initialize();
        }

        /// <summary>
        /// 監視の有効/無効を切り替え
        /// </summary>
        public void SetMonitoringEnabled(bool enabled)
        {
            enableMonitoring = enabled;
            if (enabled && frameTimeHistory == null)
            {
                Initialize();
            }
        }

        /// <summary>
        /// ターゲットフレームレートを設定
        /// </summary>
        public void SetTargetFrameRate(float fps)
        {
            targetFrameRate = fps;
            targetFrameTimeMs = 1000f / fps;
        }

        /// <summary>
        /// 過負荷判定の閾値を設定
        /// </summary>
        public void SetOverloadThreshold(float threshold)
        {
            overloadThreshold = Mathf.Clamp01(threshold);
        }

        /// <summary>
        /// デバッグ情報を文字列で取得
        /// </summary>
        public string GetDebugString()
        {
            var stats = GetCurrentStats();
            return $"FPS: {stats.currentFPS:F1} (Avg: {stats.averageFPS:F1}, Min: {stats.minFPS:F1}, Max: {stats.maxFPS:F1})\n" +
                   $"Frame Time: {stats.frameTimeMs:F2}ms (Avg: {stats.averageFrameTimeMs:F2}ms)\n" +
                   $"Overloaded: {stats.isOverloaded} | Frames: {stats.totalFrames} | Duration: {stats.monitoringDuration:F1}s";
        }

        #region Properties

        public bool EnableMonitoring 
        { 
            get => enableMonitoring; 
            set => SetMonitoringEnabled(value); 
        }

        public float TargetFrameRate 
        { 
            get => targetFrameRate; 
            set => SetTargetFrameRate(value); 
        }

        public float CurrentFPS => currentFPS;
        public float AverageFPS => averageFPS;
        public float FrameTimeMs => frameTimeMs;
        public float AverageFrameTimeMs => averageFrameTimeMs;
        public bool IsOverloaded => isOverloaded;
        public int FrameCount => frameCount;

        #endregion
    }
}