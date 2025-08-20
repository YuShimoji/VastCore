using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vastcore.Generation;
using Vastcore.UI;

namespace Vastcore.Testing
{
    /// <summary>
    /// Vastcore地形・オブジェクト生成システムの統合テストマネージャー
    /// 全システムの統合テストと自動検証を実行
    /// </summary>
    public class VastcoreIntegrationTestManager : MonoBehaviour
    {
        [Header("テスト設定")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private bool enableStressTest = false;
        [SerializeField] private float stressTestDuration = 120f;
        
        [Header("テストシーン設定")]
        [SerializeField] private Transform testPlayerTransform;
        [SerializeField] private Camera testCamera;
        [SerializeField] private Canvas testUICanvas;
        
        [Header("システム参照")]
        [SerializeField] private RuntimeTerrainManager runtimeTerrainManager;
        [SerializeField] private PrimitiveTerrainManager primitiveTerrainManager;
        [SerializeField] private BiomePresetManager biomePresetManager;
        [SerializeField] private SliderBasedUISystem uiSystem;
        [SerializeField] private PerformanceMonitor performanceMonitor;
        
        [Header("テスト結果")]
        [SerializeField] private TestResults testResults;
        
        // テスト状態
        private bool isTestRunning = false;
        private List<ITestCase> testCases;
        private TestLogger testLogger;
        
        void Start()
        {
            InitializeTestEnvironment();
            
            if (runTestsOnStart)
            {
                StartCoroutine(RunIntegrationTests());
            }
        }
        
        /// <summary>
        /// テスト環境の初期化
        /// </summary>
        private void InitializeTestEnvironment()
        {
            testLogger = new TestLogger();
            testResults = new TestResults();
            
            // テストケースの初期化
            testCases = new List<ITestCase>
            {
                new TerrainGenerationTestCase(),
                new PrimitiveGenerationTestCase(),
                new BiomePresetTestCase(),
                new PlayerInteractionTestCase(),
                new UISystemTestCase(),
                new PerformanceTestCase(),
                new MemoryManagementTestCase(),
                new SystemIntegrationTestCase()
            };
            
            // テストプレイヤーの設定
            if (testPlayerTransform == null)
            {
                CreateTestPlayer();
            }
            
            // パフォーマンス監視の開始
            if (enablePerformanceMonitoring && performanceMonitor != null)
            {
                performanceMonitor.StartMonitoring();
            }
            
            testLogger.Log("Integration test environment initialized");
        }
        
        /// <summary>
        /// 統合テストの実行
        /// </summary>
        public IEnumerator RunIntegrationTests()
        {
            if (isTestRunning)
            {
                testLogger.LogWarning("Tests are already running");
                yield break;
            }
            
            isTestRunning = true;
            testLogger.Log("=== Vastcore Integration Test Suite Started ===");
            
            try
            {
                // 各テストケースを順次実行
                foreach (var testCase in testCases)
                {
                    yield return StartCoroutine(RunTestCase(testCase));
                }
                
                // ストレステスト（オプション）
                if (enableStressTest)
                {
                    yield return StartCoroutine(RunStressTest());
                }
                
                // 最終検証
                yield return StartCoroutine(RunFinalValidation());
                
                // 結果の出力
                LogTestResults();
            }
            catch (System.Exception e)
            {
                testLogger.LogError($"Integration test suite failed: {e.Message}");
                testResults.AddFailure("Integration Test Suite", e.Message);
            }
            finally
            {
                isTestRunning = false;
                testLogger.Log("=== Vastcore Integration Test Suite Completed ===");
            }
        }
        
        /// <summary>
        /// 個別テストケースの実行
        /// </summary>
        private IEnumerator RunTestCase(ITestCase testCase)
        {
            testLogger.Log($"Running test case: {testCase.GetType().Name}");
            
            try
            {
                yield return StartCoroutine(testCase.Execute(this));
                testResults.AddSuccess(testCase.GetType().Name);
                testLogger.Log($"✓ {testCase.GetType().Name} passed");
            }
            catch (System.Exception e)
            {
                testResults.AddFailure(testCase.GetType().Name, e.Message);
                testLogger.LogError($"✗ {testCase.GetType().Name} failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// ストレステストの実行
        /// </summary>
        private IEnumerator RunStressTest()
        {
            testLogger.Log($"Running stress test for {stressTestDuration} seconds...");
            
            float endTime = Time.time + stressTestDuration;
            Vector3 centerPosition = testPlayerTransform.position;
            
            while (Time.time < endTime)
            {
                // ランダムな移動
                Vector3 randomOffset = new Vector3(
                    Random.Range(-5000f, 5000f),
                    0,
                    Random.Range(-5000f, 5000f)
                );
                
                testPlayerTransform.position = centerPosition + randomOffset;
                
                // システムの状態をチェック
                CheckSystemHealth();
                
                yield return new WaitForSeconds(Random.Range(0.5f, 2f));
            }
            
            testPlayerTransform.position = centerPosition;
            testLogger.Log("Stress test completed");
        }
        
        /// <summary>
        /// 最終検証
        /// </summary>
        private IEnumerator RunFinalValidation()
        {
            testLogger.Log("Running final validation...");
            
            // メモリリークチェック
            System.GC.Collect();
            yield return new WaitForSeconds(1f);
            
            long memoryUsage = System.GC.GetTotalMemory(false);
            testLogger.Log($"Final memory usage: {memoryUsage / 1024 / 1024}MB");
            
            // システム状態の最終確認
            ValidateSystemStates();
            
            testLogger.Log("Final validation completed");
        }
        
        /// <summary>
        /// システムの健全性チェック
        /// </summary>
        private void CheckSystemHealth()
        {
            // フレームレートチェック
            if (Time.deltaTime > 0.05f) // 20FPS以下
            {
                testLogger.LogWarning($"Low framerate detected: {1f / Time.deltaTime:F1}FPS");
            }
            
            // メモリ使用量チェック
            long memoryUsage = System.GC.GetTotalMemory(false);
            if (memoryUsage > 1024 * 1024 * 1024) // 1GB以上
            {
                testLogger.LogWarning($"High memory usage: {memoryUsage / 1024 / 1024}MB");
            }
        }
        
        /// <summary>
        /// システム状態の検証
        /// </summary>
        private void ValidateSystemStates()
        {
            // RuntimeTerrainManagerの状態確認
            if (runtimeTerrainManager != null)
            {
                var stats = runtimeTerrainManager.GetPerformanceStats();
                testLogger.Log($"Terrain Manager - Generated: {stats.totalTilesGenerated}, Deleted: {stats.totalTilesDeleted}");
            }
            
            // PrimitiveTerrainManagerの状態確認
            if (primitiveTerrainManager != null)
            {
                int activePrimitives = primitiveTerrainManager.GetActivePrimitiveCount();
                testLogger.Log($"Primitive Manager - Active primitives: {activePrimitives}");
            }
            
            // UIシステムの状態確認
            if (uiSystem != null)
            {
                bool isResponsive = uiSystem.IsResponsive();
                testLogger.Log($"UI System - Responsive: {isResponsive}");
            }
        }
        
        /// <summary>
        /// テストプレイヤーの作成
        /// </summary>
        private void CreateTestPlayer()
        {
            var playerObject = new GameObject("TestPlayer");
            testPlayerTransform = playerObject.transform;
            testPlayerTransform.position = Vector3.zero;
            
            // 基本的なコンポーネントを追加
            var rigidbody = playerObject.AddComponent<Rigidbody>();
            rigidbody.mass = 1f;
            rigidbody.linearDamping = 1f;
            
            var collider = playerObject.AddComponent<CapsuleCollider>();
            collider.height = 2f;
            collider.radius = 0.5f;
            
            testLogger.Log("Test player created");
        }
        
        /// <summary>
        /// テスト結果のログ出力
        /// </summary>
        private void LogTestResults()
        {
            testLogger.Log("=== Integration Test Results ===");
            testLogger.Log($"Total Tests: {testResults.TotalTests}");
            testLogger.Log($"Passed: {testResults.PassedTests}");
            testLogger.Log($"Failed: {testResults.FailedTests}");
            testLogger.Log($"Success Rate: {testResults.SuccessRate:F1}%");
            
            if (testResults.FailedTests > 0)
            {
                testLogger.Log("=== Failed Tests ===");
                foreach (var failure in testResults.Failures)
                {
                    testLogger.LogError($"{failure.Key}: {failure.Value}");
                }
            }
            
            testLogger.Log("================================");
        }
        
        // パブリックアクセサー（テストケースから使用）
        public RuntimeTerrainManager RuntimeTerrainManager => runtimeTerrainManager;
        public PrimitiveTerrainManager PrimitiveTerrainManager => primitiveTerrainManager;
        public BiomePresetManager BiomePresetManager => biomePresetManager;
        public SliderBasedUISystem UISystem => uiSystem;
        public PerformanceMonitor PerformanceMonitor => performanceMonitor;
        public Transform TestPlayer => testPlayerTransform;
        public TestLogger Logger => testLogger;
        
        // コンテキストメニュー
        [ContextMenu("Run Integration Tests")]
        public void RunTestsManually()
        {
            StartCoroutine(RunIntegrationTests());
        }
        
        [ContextMenu("Run Stress Test Only")]
        public void RunStressTestOnly()
        {
            StartCoroutine(RunStressTest());
        }
        
        [ContextMenu("Log Current System States")]
        public void LogCurrentSystemStates()
        {
            ValidateSystemStates();
        }
    }
    
    /// <summary>
    /// テスト結果を格納するクラス
    /// </summary>
    [System.Serializable]
    public class TestResults
    {
        [SerializeField] private List<string> passedTests = new List<string>();
        [SerializeField] private Dictionary<string, string> failures = new Dictionary<string, string>();
        
        public int TotalTests => passedTests.Count + failures.Count;
        public int PassedTests => passedTests.Count;
        public int FailedTests => failures.Count;
        public float SuccessRate => TotalTests > 0 ? (float)PassedTests / TotalTests * 100f : 0f;
        public Dictionary<string, string> Failures => failures;
        
        public void AddSuccess(string testName)
        {
            passedTests.Add(testName);
        }
        
        public void AddFailure(string testName, string error)
        {
            failures[testName] = error;
        }
        
        public void Clear()
        {
            passedTests.Clear();
            failures.Clear();
        }
    }
    
    /// <summary>
    /// テストログ出力クラス
    /// </summary>
    public class TestLogger
    {
        private List<string> logs = new List<string>();
        
        public void Log(string message)
        {
            string timestampedMessage = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
            logs.Add(timestampedMessage);
            Debug.Log(timestampedMessage);
        }
        
        public void LogWarning(string message)
        {
            string timestampedMessage = $"[{System.DateTime.Now:HH:mm:ss}] WARNING: {message}";
            logs.Add(timestampedMessage);
            Debug.LogWarning(timestampedMessage);
        }
        
        public void LogError(string message)
        {
            string timestampedMessage = $"[{System.DateTime.Now:HH:mm:ss}] ERROR: {message}";
            logs.Add(timestampedMessage);
            Debug.LogError(timestampedMessage);
        }
        
        public List<string> GetLogs() => new List<string>(logs);
        
        public void SaveLogsToFile(string filePath)
        {
            System.IO.File.WriteAllLines(filePath, logs);
        }
    }
}