using UnityEngine;
using Vastcore.Utils;

namespace Vastcore.Testing
{
    /// <summary>
    /// Deform統合テストの実行管理クラス
    /// エディタとランタイムの両方でテストを実行可能
    /// </summary>
    public class DeformIntegrationTestRunner : MonoBehaviour
    {
        [Header("テスト実行設定")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool logDetailedResults = true;
        
        [Header("テスト対象")]
        [SerializeField] private bool testCompilation = true;
        [SerializeField] private bool testManagerInitialization = true;
        [SerializeField] private bool testPrimitiveGeneration = true;
        [SerializeField] private bool testDeformComponents = true;
        [SerializeField] private bool testQualitySwitching = true;
        [SerializeField] private bool testPerformance = true;
        [SerializeField] private bool testPresetLibrary = true;
        
        private DeformIntegrationTest testInstance;
        
        void Start()
        {
            if (runOnStart)
            {
                RunAllTests();
            }
        }
        
        /// <summary>
        /// 全テストを実行
        /// </summary>
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            VastcoreLogger.Log("=== Deform統合テスト開始 ===", VastcoreLogger.LogLevel.Info);
            
            if (testInstance == null)
            {
                testInstance = gameObject.GetComponent<DeformIntegrationTest>();
                if (testInstance == null)
                {
                    testInstance = gameObject.AddComponent<DeformIntegrationTest>();
                }
            }
            
            bool allPassed = true;
            int testCount = 0;
            int passedCount = 0;
            
            // コンパイル確認テスト
            if (testCompilation)
            {
                testCount++;
                VastcoreLogger.Log("--- コンパイル確認テスト ---", VastcoreLogger.LogLevel.Info);
                bool result = TestCompilation();
                if (result) passedCount++;
                allPassed &= result;
                LogTestResult("コンパイル確認", result);
            }
            
            // マネージャー初期化テスト
            if (testManagerInitialization)
            {
                testCount++;
                VastcoreLogger.Log("--- マネージャー初期化テスト ---", VastcoreLogger.LogLevel.Info);
                bool result = testInstance.TestManagerInitialization();
                if (result) passedCount++;
                allPassed &= result;
                LogTestResult("マネージャー初期化", result);
            }
            
            // プリミティブ生成テスト
            if (testPrimitiveGeneration)
            {
                testCount++;
                VastcoreLogger.Log("--- プリミティブ生成テスト ---", VastcoreLogger.LogLevel.Info);
                bool result = testInstance.TestPrimitiveGeneration();
                if (result) passedCount++;
                allPassed &= result;
                LogTestResult("プリミティブ生成", result);
            }
            
            // Deformコンポーネントテスト
            if (testDeformComponents)
            {
                testCount++;
                VastcoreLogger.Log("--- Deformコンポーネントテスト ---", VastcoreLogger.LogLevel.Info);
                bool result = testInstance.TestDeformComponentApplication();
                if (result) passedCount++;
                allPassed &= result;
                LogTestResult("Deformコンポーネント", result);
            }
            
            // 品質切り替えテスト
            if (testQualitySwitching)
            {
                testCount++;
                VastcoreLogger.Log("--- 品質切り替えテスト ---", VastcoreLogger.LogLevel.Info);
                bool result = testInstance.TestQualitySwitching();
                if (result) passedCount++;
                allPassed &= result;
                LogTestResult("品質切り替え", result);
            }
            
            // パフォーマンステスト
            if (testPerformance)
            {
                testCount++;
                VastcoreLogger.Log("--- パフォーマンステスト ---", VastcoreLogger.LogLevel.Info);
                bool result = testInstance.TestPerformance();
                if (result) passedCount++;
                allPassed &= result;
                LogTestResult("パフォーマンス", result);
            }
            
            // プリセットライブラリテスト
            if (testPresetLibrary)
            {
                testCount++;
                VastcoreLogger.Log("--- プリセットライブラリテスト ---", VastcoreLogger.LogLevel.Info);
                bool result = testInstance.TestPresetLibrary();
                if (result) passedCount++;
                allPassed &= result;
                LogTestResult("プリセットライブラリ", result);
            }
            
            // 最終結果
            VastcoreLogger.Log($"=== テスト結果: {passedCount}/{testCount} 成功 ===", 
                allPassed ? VastcoreLogger.LogLevel.Info : VastcoreLogger.LogLevel.Warning);
            
            if (allPassed)
            {
                VastcoreLogger.Log("🎉 全てのテストが成功しました！Deform統合システムは正常に動作しています。", 
                    VastcoreLogger.LogLevel.Info);
            }
            else
            {
                VastcoreLogger.Log("⚠️ 一部のテストが失敗しました。詳細を確認してください。", 
                    VastcoreLogger.LogLevel.Warning);
            }
        }
        
        /// <summary>
        /// コンパイル確認テスト
        /// </summary>
        private bool TestCompilation()
        {
            try
            {
                // 条件付きコンパイルの確認
#if DEFORM_AVAILABLE
                VastcoreLogger.Log("✓ DEFORM_AVAILABLEシンボルが定義されています", VastcoreLogger.LogLevel.Debug);
                
                // Deform名前空間の確認
                var deformType = System.Type.GetType("Deform.Deformable, Assembly-CSharp");
                if (deformType != null)
                {
                    VastcoreLogger.Log("✓ Deform.Deformableクラスにアクセス可能です", VastcoreLogger.LogLevel.Debug);
                }
                else
                {
                    VastcoreLogger.Log("⚠️ Deform.Deformableクラスが見つかりません", VastcoreLogger.LogLevel.Warning);
                }
#else
                VastcoreLogger.Log("✓ DEFORM_AVAILABLEシンボルが未定義（ダミーモード）", VastcoreLogger.LogLevel.Debug);
#endif
                
                // 必要なクラスの存在確認
                var managerType = typeof(Vastcore.Core.VastcoreDeformManager);
                var generatorType = typeof(Vastcore.Generation.HighQualityPrimitiveGenerator);
                var presetType = typeof(Vastcore.Core.DeformPresetLibrary);
                
                VastcoreLogger.Log($"✓ VastcoreDeformManager: {managerType.Name}", VastcoreLogger.LogLevel.Debug);
                VastcoreLogger.Log($"✓ HighQualityPrimitiveGenerator: {generatorType.Name}", VastcoreLogger.LogLevel.Debug);
                VastcoreLogger.Log($"✓ DeformPresetLibrary: {presetType.Name}", VastcoreLogger.LogLevel.Debug);
                
                return true;
            }
            catch (System.Exception ex)
            {
                VastcoreLogger.Log($"❌ コンパイル確認エラー: {ex.Message}", VastcoreLogger.LogLevel.Error);
                return false;
            }
        }
        
        /// <summary>
        /// テスト結果をログ出力
        /// </summary>
        private void LogTestResult(string testName, bool passed)
        {
            string status = passed ? "✓ 成功" : "❌ 失敗";
            var logLevel = passed ? VastcoreLogger.LogLevel.Info : VastcoreLogger.LogLevel.Error;
            
            if (logDetailedResults)
            {
                VastcoreLogger.Log($"{status} {testName}テスト", logLevel);
            }
        }
        
        /// <summary>
        /// 個別テストメソッド（エディタから実行可能）
        /// </summary>
        [ContextMenu("Test Manager Initialization")]
        public void TestManagerInitializationOnly()
        {
            if (testInstance == null) testInstance = GetComponent<DeformIntegrationTest>();
            bool result = testInstance.TestManagerInitialization();
            LogTestResult("マネージャー初期化", result);
        }
        
        [ContextMenu("Test Primitive Generation")]
        public void TestPrimitiveGenerationOnly()
        {
            if (testInstance == null) testInstance = GetComponent<DeformIntegrationTest>();
            bool result = testInstance.TestPrimitiveGeneration();
            LogTestResult("プリミティブ生成", result);
        }
        
        [ContextMenu("Test Performance")]
        public void TestPerformanceOnly()
        {
            if (testInstance == null) testInstance = GetComponent<DeformIntegrationTest>();
            bool result = testInstance.TestPerformance();
            LogTestResult("パフォーマンス", result);
        }
    }
}
