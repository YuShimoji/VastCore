using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// 地形・オブジェクト生成タスクを表すクラス
    /// 優先度付きキューシステムで管理される
    /// </summary>
    [System.Serializable]
    public class GenerationTask : IComparable<GenerationTask>
    {
        public enum TaskType
        {
            TerrainGeneration,      // 地形生成
            StructureSpawn,         // 構造物生成
            PrimitiveSpawn,         // プリミティブ地形生成
            BiomeApplication,       // バイオーム適用
            TileCleanup            // タイル削除・クリーンアップ
        }

        public enum Priority
        {
            Low = 0,
            Normal = 1,
            High = 2,
            Critical = 3
        }

        [Header("タスク基本情報")]
        public TaskType type;
        public Priority priority = Priority.Normal;
        public Vector3 position;
        public string taskId;

        [Header("実行制御")]
        public float creationTime;
        public float estimatedExecutionTime = 1f; // 推定実行時間（秒）
        public bool isExecuting = false;
        public bool isCompleted = false;

        [Header("パラメータ")]
        public Dictionary<string, object> parameters;

        [Header("コールバック")]
        public Action<GameObject> onComplete;
        public Action<string> onError;

        /// <summary>
        /// GenerationTaskのコンストラクタ
        /// </summary>
        public GenerationTask(TaskType taskType, Vector3 pos, Priority taskPriority = Priority.Normal)
        {
            type = taskType;
            position = pos;
            priority = taskPriority;
            taskId = GenerateTaskId();
            creationTime = Time.time;
            parameters = new Dictionary<string, object>();
        }

        /// <summary>
        /// タスクパラメータを設定
        /// </summary>
        public void SetParameter(string key, object value)
        {
            if (parameters == null)
                parameters = new Dictionary<string, object>();
            
            parameters[key] = value;
        }

        /// <summary>
        /// タスクパラメータを取得
        /// </summary>
        public T GetParameter<T>(string key, T defaultValue = default(T))
        {
            if (parameters == null || !parameters.ContainsKey(key))
                return defaultValue;

            try
            {
                return (T)parameters[key];
            }
            catch (InvalidCastException)
            {
                Debug.LogWarning($"GenerationTask: パラメータ '{key}' の型変換に失敗しました。デフォルト値を返します。");
                return defaultValue;
            }
        }

        /// <summary>
        /// タスクの実行開始
        /// </summary>
        public void StartExecution()
        {
            isExecuting = true;
        }

        /// <summary>
        /// タスクの完了
        /// </summary>
        public void CompleteTask(GameObject result = null)
        {
            isExecuting = false;
            isCompleted = true;
            onComplete?.Invoke(result);
        }

        /// <summary>
        /// タスクのエラー終了
        /// </summary>
        public void ErrorTask(string errorMessage)
        {
            isExecuting = false;
            isCompleted = true;
            onError?.Invoke(errorMessage);
            Debug.LogError($"GenerationTask Error [{taskId}]: {errorMessage}");
        }

        /// <summary>
        /// 優先度による比較（高優先度が先に処理される）
        /// </summary>
        public int CompareTo(GenerationTask other)
        {
            if (other == null) return 1;

            // 優先度の比較（高い方が先）
            int priorityComparison = other.priority.CompareTo(this.priority);
            if (priorityComparison != 0)
                return priorityComparison;

            // 優先度が同じ場合は作成時間で比較（古い方が先）
            return this.creationTime.CompareTo(other.creationTime);
        }

        /// <summary>
        /// タスクの実行時間を推定
        /// </summary>
        public float GetEstimatedExecutionTime()
        {
            switch (type)
            {
                case TaskType.TerrainGeneration:
                    return GetParameter("tileSize", 1000f) / 1000f * 2f; // サイズに応じて調整
                case TaskType.PrimitiveSpawn:
                    return GetParameter("primitiveCount", 1) * 0.5f;
                case TaskType.StructureSpawn:
                    return GetParameter("structureComplexity", 1f) * 1.5f;
                case TaskType.BiomeApplication:
                    return 0.5f;
                case TaskType.TileCleanup:
                    return 0.2f;
                default:
                    return estimatedExecutionTime;
            }
        }

        /// <summary>
        /// プレイヤーからの距離を計算
        /// </summary>
        public float GetDistanceFromPlayer(Vector3 playerPosition)
        {
            return Vector3.Distance(position, playerPosition);
        }

        /// <summary>
        /// タスクが有効かどうかを判定
        /// </summary>
        public bool IsValid()
        {
            return !isCompleted && !string.IsNullOrEmpty(taskId);
        }

        /// <summary>
        /// ユニークなタスクIDを生成
        /// </summary>
        private string GenerateTaskId()
        {
            return $"{type}_{position.x:F0}_{position.z:F0}_{Time.time:F2}_{UnityEngine.Random.Range(1000, 9999)}";
        }

        /// <summary>
        /// タスク情報を文字列で取得（デバッグ用）
        /// </summary>
        public override string ToString()
        {
            return $"GenerationTask[{taskId}] Type:{type}, Priority:{priority}, Position:{position}, Executing:{isExecuting}, Completed:{isCompleted}";
        }
    }
}