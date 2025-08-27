using System.Collections;
using UnityEngine;

namespace Vastcore.Generation.Map
{
    /// <summary>
    /// RuntimeGenerationManagerの動作テスト用スクリプト
    /// </summary>
    public class RuntimeGenerationManagerTest : MonoBehaviour
    {
        [Header("テスト設定")]
        [SerializeField] private bool runTests = false;
        [SerializeField] private bool showPerformanceStats = true;
        [SerializeField] private float testInterval = 2f;

        [Header("テスト対象")]
        [SerializeField] private RuntimeGenerationManager generationManager;

        [Header("テスト結果")]
        [SerializeField] private int totalTasksQueued = 0;
        [SerializeField] private int totalTasksCompleted = 0;
        [SerializeField] private int totalTasksErrored = 0;

        private void Start()
        {
            // RuntimeGenerationManagerを取得
            if (generationManager == null)
            {
                generationManager = FindObjectOfType<RuntimeGenerationManager>();
            }

            if (generationManager == null)
            {
                Debug.LogError("RuntimeGenerationManagerTest: RuntimeGenerationManagerが見つかりません。");
                return;
            }

            // イベントを購読
            generationManager.OnTaskQueued += OnTaskQueued;
            generationManager.OnTaskCompleted += OnTaskCompleted;
            generationManager.OnTaskError += OnTaskError;

            // テスト開始
            if (runTests)
            {
                StartCoroutine(RunPerformanceTests());
            }
        }

        private void Update()
        {
            if (showPerformanceStats && generationManager != null)
            {
                DisplayPerformanceStats();
            }
        }

        /// <summary>
        /// パフォーマンステストを実行
        /// </summary>
        private IEnumerator RunPerformanceTests()
        {
            Debug.Log("RuntimeGenerationManagerTest: パフォーマンステストを開始します。");

            while (runTests)
            {
                // 様々な優先度のタスクを追加
                TestTaskQueuing();
                
                yield return new WaitForSeconds(testInterval);
                
                // 負荷テスト
                TestHighLoadScenario();
                
                yield return new WaitForSeconds(testInterval);
                
                // パフォーマンス統計をログ出力
                LogPerformanceStats();
                
                yield return new WaitForSeconds(testInterval);
            }
        }

        /// <summary>
        /// タスクキューイングのテスト
        /// </summary>
        private void TestTaskQueuing()
        {
            if (generationManager == null) return;

            // 異なる優先度のタスクを追加
            var positions = new Vector3[]
            {
                new Vector3(100, 0, 100),
                new Vector3(200, 0, 200),
                new Vector3(300, 0, 300),
                new Vector3(400, 0, 400),
                new Vector3(500, 0, 500)
            };

            var priorities = new GenerationTask.Priority[]
            {
                GenerationTask.Priority.Low,
                GenerationTask.Priority.Normal,
                GenerationTask.Priority.High,
                GenerationTask.Priority.Critical,
                GenerationTask.Priority.Normal
            };

            var taskTypes = new GenerationTask.TaskType[]
            {
                GenerationTask.TaskType.TerrainGeneration,
                GenerationTask.TaskType.PrimitiveSpawn,
                GenerationTask.TaskType.TileCleanup,
                GenerationTask.TaskType.BiomeApplication,
                GenerationTask.TaskType.StructureSpawn
            };

            for (int i = 0; i < positions.Length; i++)
            {
                var task = new GenerationTask(taskTypes[i], positions[i], priorities[i]);
                
                // テスト用パラメータを設定
                task.SetParameter("testId", i);
                task.SetParameter("tileSize", Random.Range(500f, 2000f));
                task.SetParameter("primitiveCount", Random.Range(1, 5));
                
                generationManager.QueueGenerationTask(task);
            }

            Debug.Log($"RuntimeGenerationManagerTest: {positions.Length}個のテストタスクをキューに追加しました。");
        }

        /// <summary>
        /// 高負荷シナリオのテスト
        /// </summary>
        private void TestHighLoadScenario()
        {
            if (generationManager == null) return;

            // 大量のタスクを一度に追加して負荷分散をテスト
            int taskCount = 20;
            
            for (int i = 0; i < taskCount; i++)
            {
                Vector3 randomPos = new Vector3(
                    Random.Range(-1000f, 1000f),
                    0,
                    Random.Range(-1000f, 1000f)
                );

                var task = new GenerationTask(
                    GenerationTask.TaskType.TerrainGeneration,
                    randomPos,
                    GenerationTask.Priority.Normal
                );

                task.SetParameter("highLoadTest", true);
                task.SetParameter("tileSize", 1000f);
                
                generationManager.QueueGenerationTask(task);
            }

            Debug.Log($"RuntimeGenerationManagerTest: 高負荷テスト - {taskCount}個のタスクを追加しました。");
        }

        /// <summary>
        /// パフォーマンス統計をログ出力
        /// </summary>
        private void LogPerformanceStats()
        {
            if (generationManager == null) return;

            var stats = generationManager.GetPerformanceStats();
            string debugString = generationManager.GetPerformanceDebugString();

            Debug.Log($"RuntimeGenerationManagerTest: パフォーマンス統計\n{debugString}");
            Debug.Log($"RuntimeGenerationManagerTest: キュー状況 - サイズ: {generationManager.QueueSize}, 処理中: {generationManager.IsProcessing}, 過負荷: {generationManager.IsOverloaded}");
            Debug.Log($"RuntimeGenerationManagerTest: タスク統計 - キュー済み: {totalTasksQueued}, 完了: {totalTasksCompleted}, エラー: {totalTasksErrored}");
        }

        /// <summary>
        /// パフォーマンス統計を画面に表示
        /// </summary>
        private void DisplayPerformanceStats()
        {
            // OnGUIで表示するための準備（実装は後で追加可能）
        }

        #region Event Handlers

        private void OnTaskQueued(GenerationTask task)
        {
            totalTasksQueued++;
            
            if (showPerformanceStats)
            {
                Debug.Log($"RuntimeGenerationManagerTest: タスクキュー追加 - {task.taskId} ({task.type}, {task.priority})");
            }
        }

        private void OnTaskCompleted(GenerationTask task)
        {
            totalTasksCompleted++;
            
            if (showPerformanceStats)
            {
                Debug.Log($"RuntimeGenerationManagerTest: タスク完了 - {task.taskId}");
            }
        }

        private void OnTaskError(GenerationTask task, string error)
        {
            totalTasksErrored++;
            
            Debug.LogError($"RuntimeGenerationManagerTest: タスクエラー - {task.taskId}: {error}");
        }

        #endregion

        #region GUI Display

        private void OnGUI()
        {
            if (!showPerformanceStats || generationManager == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 400, 300));
            GUILayout.BeginVertical("box");

            GUILayout.Label("Runtime Generation Manager - Performance Stats");
            
            GUILayout.Space(10);
            
            // 基本統計
            GUILayout.Label($"Queue Size: {generationManager.QueueSize}");
            GUILayout.Label($"Processing: {generationManager.IsProcessing}");
            GUILayout.Label($"Overloaded: {generationManager.IsOverloaded}");
            GUILayout.Label($"Current FPS: {generationManager.CurrentFPS:F1}");
            GUILayout.Label($"Avg Frame Time: {generationManager.AverageFrameTime:F2}ms");
            
            GUILayout.Space(10);
            
            // タスク統計
            GUILayout.Label($"Tasks Queued: {totalTasksQueued}");
            GUILayout.Label($"Tasks Completed: {totalTasksCompleted}");
            GUILayout.Label($"Tasks Errored: {totalTasksErrored}");
            
            GUILayout.Space(10);
            
            // テスト制御
            if (GUILayout.Button(runTests ? "Stop Tests" : "Start Tests"))
            {
                runTests = !runTests;
                if (runTests)
                {
                    StartCoroutine(RunPerformanceTests());
                }
            }
            
            if (GUILayout.Button("Clear Queue"))
            {
                generationManager.ClearQueue();
            }
            
            if (GUILayout.Button("Reset Stats"))
            {
                totalTasksQueued = 0;
                totalTasksCompleted = 0;
                totalTasksErrored = 0;
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        #endregion

        private void OnDestroy()
        {
            // イベントの購読を解除
            if (generationManager != null)
            {
                generationManager.OnTaskQueued -= OnTaskQueued;
                generationManager.OnTaskCompleted -= OnTaskCompleted;
                generationManager.OnTaskError -= OnTaskError;
            }
        }
    }
}