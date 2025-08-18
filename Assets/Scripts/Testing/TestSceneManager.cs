using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VastCore.Testing
{
    /// <summary>
    /// テストシーン管理システム
    /// 統合テストの実行環境を提供し、全テストシステムを統合管理
    /// </summary>
    public class TestSceneManager : MonoBehaviour
    {
        [Header("テストシーン設定")]
        [SerializeField] private bool autoStartTests = false;
        [SerializeField] private bool enableAllTests = true;
        [SerializeField] private float testStartDelay = 2f;
        
        [Header("テストシステム参照")]
        [SerializeField] private ComprehensiveSystemTest stabilityTest;
        [SerializeField] private QualityAssuranceTestSuite qaTestSuite;
        [SerializeField] private MemoryMonitor memoryMonitor;
        
        [Header("テスト環境設定")]
        [SerializeField] private Camera testCamera;
        [SerializeField] private Light testLight;
        [SerializeField] private Transform testPlayerPosition;
        
        // テスト状態管理
        private bool testsRunning = false;
        private List<ITestSystem> activeTestSystems;
        private TestExecutionPlan currentPlan;
        
        private void Start()
        {
            InitializeTestEnvironment();
            
            if (autoStartTests)
            {
                StartCoroutine(DelayedTestStart());
            }
        }
        
        private void InitializeTestEnvironment()
        {
            Debug.Log("Initializing test environment...");
            
            // テストシステムの初期化
            activeTestSystems = new List<ITestSystem>();
            
            // 必要なコンポーネントの自動追加
            if (stabilityTest == null)
            {
                stabilityTest = gameObject.AddComponent<ComprehensiveSystemTest>();
            }
            
            if (qaTestSuite == null)
            {
                qaTestSuite = gameObject.AddComponent<QualityAssuranceTestSuite>();
            }
            
            if (memoryMonitor == null)
            {
                memoryMonitor = gameObject.AddComponent<MemoryMonitor>();
            }
            
            // テスト環境の設定
            SetupTestEnvironment();
            
            Debug.Log("Test environment initialized");
        }
        
        private void SetupTestEnvironment()
        {
            // テスト用カメラの設定
            if (testCamera == null)
            {
                GameObject cameraObj = new GameObject("TestCamera");
                testCamera = cameraObj.AddComponent<Camera>();
                testCamera.transform.position = new Vector3(0, 100, 0);
                testCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
            }
            
            // テスト用ライトの設定
            if (testLight == null)
            {
                GameObject lightObj = new GameObject("TestLight");
                testLight = lightObj.AddComponent<Light>();
                testLight.type = LightType.Directional;
                testLight.transform.rotation = Quaternion.Euler(45, 45, 0);
            }
            
            // テスト用プレイヤー位置の設定
            if (testPlayerPosition == null)
            {
                GameObject playerObj = new GameObject("TestPlayerPosition");
                testPlayerPosition = playerObj.transform;
                testPlayerPosition.position = Vector3.zero;
            }
            
            // 品質設定の調整（テスト用）
            QualitySettings.vSyncCount = 0; // VSync無効化でパフォーマンステストの精度向上
            Application.targetFrameRate = -1; // フレームレート制限解除
        }
        
        private IEnumerator DelayedTestStart()
        {
            yield return new WaitForSeconds(testStartDelay);
            StartAllTests();
        }
        
        public void StartAllTests()
        {
            if (testsRunning)
            {
                Debug.LogWarning("Tests are already running");
                return;
            }
            
            Debug.Log("Starting comprehensive test suite...");
            testsRunning = true;
            
            StartCoroutine(ExecuteTestPlan());
        }
        
        public void StopAllTests()
        {
            if (!testsRunning)
            {
                Debug.LogWarning("No tests are currently running");
                return;
            }
            
            Debug.Log("Stopping all tests...");
            testsRunning = false;
            
            // 各テストシステムの停止
            if (stabilityTest != null)
            {
                stabilityTest.StopLongTermStabilityTest();
            }
            
            StopAllCoroutines();
        }
        
        private IEnumerator ExecuteTestPlan()
        {
            currentPlan = CreateTestExecutionPlan();
            
            Debug.Log($"Executing test plan with {currentPlan.testPhases.Count} phases");
            
            foreach (var phase in currentPlan.testPhases)
            {
                if (!testsRunning) break;
                
                Debug.Log($"Starting test phase: {phase.phaseName}");
                yield return StartCoroutine(ExecuteTestPhase(phase));
                
                if (phase.waitBetweenPhases > 0)
                {
                    Debug.Log($"Waiting {phase.waitBetweenPhases} seconds before next phase...");
                    yield return new WaitForSeconds(phase.waitBetweenPhases);
                }
            }
            
            if (testsRunning)
            {
                Debug.Log("All test phases completed successfully");
                GenerateComprehensiveReport();
            }
            
            testsRunning = false;
        }
        
        private TestExecutionPlan CreateTestExecutionPlan()
        {
            var plan = new TestExecutionPlan();
            
            // フェーズ1: 品質保証テスト
            plan.testPhases.Add(new TestPhase
            {
                phaseName = "Quality Assurance Tests",
                testActions = new List<System.Func<IEnumerator>>
                {
                    () => ExecuteQualityAssuranceTests()
                },
                waitBetweenPhases = 5f
            });
            
            // フェーズ2: 短期安定性テスト
            plan.testPhases.Add(new TestPhase
            {
                phaseName = "Short-term Stability Test",
                testActions = new List<System.Func<IEnumerator>>
                {
                    () => ExecuteShortTermStabilityTest()
                },
                waitBetweenPhases = 10f
            });
            
            // フェーズ3: 長期安定性テスト（オプション）
            if (enableAllTests)
            {
                plan.testPhases.Add(new TestPhase
                {
                    phaseName = "Long-term Stability Test",
                    testActions = new List<System.Func<IEnumerator>>
                    {
                        () => ExecuteLongTermStabilityTest()
                    },
                    waitBetweenPhases = 0f
                });
            }
            
            return plan;
        }
        
        private IEnumerator ExecuteTestPhase(TestPhase phase)
        {
            foreach (var testAction in phase.testActions)
            {
                if (!testsRunning) break;
                
                yield return StartCoroutine(testAction());
            }
        }
        
        private IEnumerator ExecuteQualityAssuranceTests()
        {
            if (qaTestSuite != null)
            {
                qaTestSuite.StartQualityAssuranceTests();
                
                // QAテストの完了を待機
                while (qaTestSuite.enabled && testsRunning)
                {
                    yield return new WaitForSeconds(1f);
                }
            }
        }
        
        private IEnumerator ExecuteShortTermStabilityTest()
        {
            if (stabilityTest != null)
            {
                // 短期安定性テスト（1時間）
                var originalDuration = stabilityTest.testDurationHours;
                stabilityTest.testDurationHours = 1f;
                
                stabilityTest.StartLongTermStabilityTest();
                
                // テスト完了を待機
                while (stabilityTest.isTestRunning && testsRunning)
                {
                    yield return new WaitForSeconds(10f);
                }
                
                // 元の設定に戻す
                stabilityTest.testDurationHours = originalDuration;
            }
        }
        
        private IEnumerator ExecuteLongTermStabilityTest()
        {
            if (stabilityTest != null)
            {
                stabilityTest.StartLongTermStabilityTest();
                
                // 長期テストは別途管理されるため、開始のみ
                yield return new WaitForSeconds(1f);
            }
        }
        
        private void GenerateComprehensiveReport()
        {
            Debug.Log("\n=== COMPREHENSIVE TEST EXECUTION REPORT ===");
            
            // メモリ監視レポート
            if (memoryMonitor != null)
            {
                var memoryReport = memoryMonitor.GenerateMemoryReport();
                if (memoryReport.isValid)
                {
                    Debug.Log($"Memory Report:");
                    Debug.Log($"  Monitoring Duration: {memoryReport.monitoringDuration:F2} hours");
                    Debug.Log($"  Memory Increase: {memoryReport.memoryIncreaseMB}MB");
                    Debug.Log($"  Peak Memory: {memoryReport.peakMemoryMB}MB");
                    Debug.Log($"  Leak Detected: {memoryReport.leakDetected}");
                }
            }
            
            // システム情報
            Debug.Log($"System Information:");
            Debug.Log($"  Unity Version: {Application.unityVersion}");
            Debug.Log($"  Platform: {Application.platform}");
            Debug.Log($"  Graphics Device: {SystemInfo.graphicsDeviceName}");
            Debug.Log($"  System Memory: {SystemInfo.systemMemorySize}MB");
            Debug.Log($"  Graphics Memory: {SystemInfo.graphicsMemorySize}MB");
            
            Debug.Log("=== END COMPREHENSIVE REPORT ===\n");
        }
        
        private void Update()
        {
            // テスト実行中のキーボードショートカット
            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (!testsRunning)
                    StartAllTests();
                else
                    StopAllTests();
            }
            
            if (Input.GetKeyDown(KeyCode.F2) && qaTestSuite != null)
            {
                qaTestSuite.StartQualityAssuranceTests();
            }
            
            if (Input.GetKeyDown(KeyCode.F3) && stabilityTest != null)
            {
                stabilityTest.StartLongTermStabilityTest();
            }
            
            if (Input.GetKeyDown(KeyCode.F4) && memoryMonitor != null)
            {
                memoryMonitor.ForceGarbageCollection();
            }
        }
        
        private void OnGUI()
        {
            // テスト実行状態の表示
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("=== Test Scene Manager ===");
            GUILayout.Label($"Tests Running: {testsRunning}");
            
            if (currentPlan != null)
            {
                GUILayout.Label($"Current Plan: {currentPlan.testPhases.Count} phases");
            }
            
            GUILayout.Space(10);
            GUILayout.Label("Controls:");
            GUILayout.Label("F1: Start/Stop All Tests");
            GUILayout.Label("F2: Start QA Tests");
            GUILayout.Label("F3: Start Stability Test");
            GUILayout.Label("F4: Force GC");
            
            GUILayout.EndArea();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && testsRunning)
            {
                Debug.Log("Application paused during testing");
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && testsRunning)
            {
                Debug.Log("Application lost focus during testing");
            }
        }
        
        private void OnDestroy()
        {
            if (testsRunning)
            {
                StopAllTests();
            }
        }
    }
    
    [System.Serializable]
    public class TestExecutionPlan
    {
        public List<TestPhase> testPhases = new List<TestPhase>();
    }
    
    [System.Serializable]
    public class TestPhase
    {
        public string phaseName;
        public List<System.Func<IEnumerator>> testActions;
        public float waitBetweenPhases;
    }
    
    public interface ITestSystem
    {
        void StartTest();
        void StopTest();
        bool IsTestRunning();
    }
}