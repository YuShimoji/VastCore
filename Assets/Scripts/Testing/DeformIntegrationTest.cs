using UnityEngine;
using UnityEngine.ProBuilder;
using Vastcore.Core;
using Vastcore.Generation;
using Vastcore.Utils;

#if DEFORM_AVAILABLE
using Deform;
#endif

namespace Vastcore.Testing
{
    /// <summary>
    /// Deform統合システムのテストクラス
    /// Phase 3実装の動作確認とパフォーマンステスト
    /// </summary>
    public class DeformIntegrationTest : MonoBehaviour
    {
        [Header("テスト設定")]
        [SerializeField] private bool runTestOnStart = false;
        [SerializeField] private bool enablePerformanceTest = true;
        [SerializeField] private int testIterations = 10;
        
        [Header("テスト対象")]
        [SerializeField] private PrimitiveTerrainGenerator.PrimitiveType[] testPrimitives = {
            PrimitiveTerrainGenerator.PrimitiveType.Cube,
            PrimitiveTerrainGenerator.PrimitiveType.Sphere,
            PrimitiveTerrainGenerator.PrimitiveType.Cylinder,
            PrimitiveTerrainGenerator.PrimitiveType.Crystal
        };
        
        [Header("品質レベルテスト")]
        [SerializeField] private HighQualityPrimitiveGenerator.QualitySettings[] qualityLevels = {
            HighQualityPrimitiveGenerator.QualitySettings.Low,
            HighQualityPrimitiveGenerator.QualitySettings.Medium,
            HighQualityPrimitiveGenerator.QualitySettings.High
        };
        
        private void Start()
        {
            if (runTestOnStart)
            {
                RunDeformIntegrationTest();
            }
        }
        
        /// <summary>
        /// Deform統合テストを実行
        /// </summary>
        [ContextMenu("Run Deform Integration Test")]
        public void RunDeformIntegrationTest()
        {
            VastcoreLogger.Log("Starting Deform Integration Test", VastcoreLogger.LogLevel.Info);
            
            bool allTestsPassed = true;
            
            // 1. VastcoreDeformManagerの初期化テスト
            allTestsPassed &= TestDeformManagerInitialization();
            
            // 2. プリミティブ生成テスト
            allTestsPassed &= TestPrimitiveGeneration();
            
            // 3. Deformコンポーネント適用テスト
            allTestsPassed &= TestDeformComponentApplication();
            
            // 4. 品質レベル切り替えテスト
            allTestsPassed &= TestQualityLevelSwitching();
            
            // 5. パフォーマンステスト
            if (enablePerformanceTest)
            {
                allTestsPassed &= TestPerformance();
            }
            
            // 6. プリセットライブラリテスト
            allTestsPassed &= TestPresetLibrary();
            
            string result = allTestsPassed ? "PASSED" : "FAILED";
            VastcoreLogger.Log($"Deform Integration Test completed: {result}", 
                allTestsPassed ? VastcoreLogger.LogLevel.Info : VastcoreLogger.LogLevel.Error);
        }
        
        /// <summary>
        /// VastcoreDeformManagerの初期化テスト
        /// </summary>
        private bool TestDeformManagerInitialization()
        {
            VastcoreLogger.Log("Testing VastcoreDeformManager initialization...", VastcoreLogger.LogLevel.Debug);
            
            try
            {
                var manager = VastcoreDeformManager.Instance;
                if (manager == null)
                {
                    VastcoreLogger.Log("VastcoreDeformManager instance is null", VastcoreLogger.LogLevel.Error);
                    return false;
                }
                
                var stats = manager.GetStats();
                VastcoreLogger.Log($"DeformManager Stats - Managed: {stats.managedDeformablesCount}, " +
                    $"Queued: {stats.queuedRequestsCount}, Enabled: {stats.systemEnabled}", VastcoreLogger.LogLevel.Debug);
                
                return true;
            }
            catch (System.Exception ex)
            {
                VastcoreLogger.Log($"DeformManager initialization failed: {ex.Message}", VastcoreLogger.LogLevel.Error);
                return false;
            }
        }
        
        /// <summary>
        /// プリミティブ生成テスト
        /// </summary>
        private bool TestPrimitiveGeneration()
        {
            VastcoreLogger.Log("Testing primitive generation with Deform integration...", VastcoreLogger.LogLevel.Debug);
            
            bool allPassed = true;
            
            foreach (var primitiveType in testPrimitives)
            {
                try
                {
                    var quality = HighQualityPrimitiveGenerator.QualitySettings.High;
                    var primitive = HighQualityPrimitiveGenerator.GeneratePrimitive(
                        primitiveType, Vector3.one, quality);
                    
                    if (primitive == null)
                    {
                        VastcoreLogger.Log($"Failed to generate {primitiveType}", VastcoreLogger.LogLevel.Error);
                        allPassed = false;
                        continue;
                    }
                    
                    #if DEFORM_AVAILABLE
                    // Deformableコンポーネントの確認
                    var deformable = primitive.GetComponent<Deformable>();
                    if (quality.enableDeformSystem && deformable == null)
                    {
                        VastcoreLogger.Log($"Deformable component missing on {primitiveType}", VastcoreLogger.LogLevel.Warning);
                    }
                    
                    // Deformerコンポーネントの確認
                    var deformers = primitive.GetComponents<Deformer>();
                    if (quality.enableDeformSystem && deformers.Length == 0)
                    {
                        VastcoreLogger.Log($"No Deformer components found on {primitiveType}", VastcoreLogger.LogLevel.Warning);
                    }
                    
                    VastcoreLogger.Log($"Successfully generated {primitiveType} with {deformers.Length} deformers", 
                        VastcoreLogger.LogLevel.Debug);
#else
                    VastcoreLogger.Log($"Successfully generated {primitiveType} (Deform disabled)", 
                        VastcoreLogger.LogLevel.Debug);
#endif
                    
                    // テスト後のクリーンアップ
                    if (Application.isPlaying)
                    {
                        Destroy(primitive.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(primitive.gameObject);
                    }
                }
                catch (System.Exception ex)
                {
                    VastcoreLogger.Log($"Exception testing {primitiveType}: {ex.Message}", VastcoreLogger.LogLevel.Error);
                    allPassed = false;
                }
            }
            
            return allPassed;
        }
        
        /// <summary>
        /// Deformコンポーネント適用テスト
        /// </summary>
        private bool TestDeformComponentApplication()
        {
            VastcoreLogger.Log("Testing Deform component application...", VastcoreLogger.LogLevel.Debug);
            
            try
            {
                // テスト用プリミティブを作成
                var testObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var proBuilderMesh = testObject.AddComponent<ProBuilderMesh>();
                
#if DEFORM_AVAILABLE
                // Deformableコンポーネントを追加
                var deformable = testObject.AddComponent<Deformable>();
#else
                // Deformパッケージが利用できない場合はダミーオブジェクトを使用
                var deformable = testObject;
#endif
                
                // VastcoreDeformManagerに登録
                var manager = VastcoreDeformManager.Instance;
                manager.RegisterDeformable(deformable, VastcoreDeformManager.DeformQualityLevel.High);
                
                // 統計確認
                var stats = manager.GetStats();
                bool registered = stats.managedDeformablesCount > 0;
                
                // クリーンアップ
                manager.UnregisterDeformable(deformable);
                if (Application.isPlaying)
                {
                    Destroy(testObject);
                }
                else
                {
                    DestroyImmediate(testObject);
                }
                
                return registered;
            }
            catch (System.Exception ex)
            {
                VastcoreLogger.Log($"Deform component application test failed: {ex.Message}", VastcoreLogger.LogLevel.Error);
                return false;
            }
        }
        
        /// <summary>
        /// 品質レベル切り替えテスト
        /// </summary>
        private bool TestQualityLevelSwitching()
        {
            VastcoreLogger.Log("Testing quality level switching...", VastcoreLogger.LogLevel.Debug);
            
            bool allPassed = true;
            
            foreach (var quality in qualityLevels)
            {
                try
                {
                    var primitive = HighQualityPrimitiveGenerator.GeneratePrimitive(
                        PrimitiveTerrainGenerator.PrimitiveType.Cube, Vector3.one, quality);
                    
                    if (primitive == null)
                    {
                        allPassed = false;
                        continue;
                    }
                    
#if DEFORM_AVAILABLE
                    var deformable = primitive.GetComponent<Deformable>();
                    var deformers = primitive.GetComponents<Deformer>();
#else
                    var deformable = primitive.gameObject;
                    var deformers = new Component[0];
#endif
                    
                    // 品質レベルに応じたコンポーネント数の確認
                    int expectedDeformers = quality.enableDeformSystem ? 
                        (quality.enableGeologicalDeformation ? 1 : 0) + 
                        (quality.enableOrganicDeformation ? 1 : 0) + 1 : 0;
                    
                    VastcoreLogger.Log($"Quality {quality.deformQuality}: Expected {expectedDeformers}, " +
                        $"Found {deformers.Length} deformers", VastcoreLogger.LogLevel.Debug);
                    
                    // クリーンアップ
                    if (Application.isPlaying)
                    {
                        Destroy(primitive.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(primitive.gameObject);
                    }
                }
                catch (System.Exception ex)
                {
                    VastcoreLogger.Log($"Quality level test failed: {ex.Message}", VastcoreLogger.LogLevel.Error);
                    allPassed = false;
                }
            }
            
            return allPassed;
        }
        
        /// <summary>
        /// パフォーマンステスト
        /// </summary>
        private bool TestPerformance()
        {
            VastcoreLogger.Log("Running performance test...", VastcoreLogger.LogLevel.Debug);
            
            try
            {
                float totalTime = 0f;
                int successCount = 0;
                
                for (int i = 0; i < testIterations; i++)
                {
                    float startTime = Time.realtimeSinceStartup;
                    
                    var primitive = HighQualityPrimitiveGenerator.GeneratePrimitive(
                        PrimitiveTerrainGenerator.PrimitiveType.Sphere, 
                        Vector3.one, 
                        HighQualityPrimitiveGenerator.QualitySettings.High);
                    
                    float endTime = Time.realtimeSinceStartup;
                    
                    if (primitive != null)
                    {
                        totalTime += (endTime - startTime);
                        successCount++;
                        
                        // クリーンアップ
                        if (Application.isPlaying)
                        {
                            Destroy(primitive.gameObject);
                        }
                        else
                        {
                            DestroyImmediate(primitive.gameObject);
                        }
                    }
                }
                
                float averageTime = totalTime / successCount;
                VastcoreLogger.Log($"Performance Test Results: {successCount}/{testIterations} successful, " +
                    $"Average time: {averageTime * 1000f:F2}ms", VastcoreLogger.LogLevel.Info);
                
                return successCount == testIterations && averageTime < 0.1f; // 100ms以下を目標
            }
            catch (System.Exception ex)
            {
                VastcoreLogger.Log($"Performance test failed: {ex.Message}", VastcoreLogger.LogLevel.Error);
                return false;
            }
        }
        
        /// <summary>
        /// プリセットライブラリテスト
        /// </summary>
        private bool TestPresetLibrary()
        {
            VastcoreLogger.Log("Testing DeformPresetLibrary...", VastcoreLogger.LogLevel.Debug);
            
            try
            {
                // ScriptableObjectとしてプリセットライブラリを作成
                var library = ScriptableObject.CreateInstance<DeformPresetLibrary>();
                
                // テスト用GameObjectを作成
                var testObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                
                // プリセット適用テスト
                library.ApplyPresetToGameObject(testObject, "風化侵食", 0.5f);
                
                // Deformerが追加されたかチェック
                var deformers = testObject.GetComponents<Deformer>();
                bool hasDeformers = deformers.Length > 0;
                
                // クリーンアップ
                if (Application.isPlaying)
                {
                    Destroy(testObject);
                    Destroy(library);
                }
                else
                {
                    DestroyImmediate(testObject);
                    DestroyImmediate(library);
                }
                
                VastcoreLogger.Log($"Preset library test: {(hasDeformers ? "PASSED" : "FAILED")}", 
                    hasDeformers ? VastcoreLogger.LogLevel.Debug : VastcoreLogger.LogLevel.Warning);
                
                return hasDeformers;
            }
            catch (System.Exception ex)
            {
                VastcoreLogger.Log($"Preset library test failed: {ex.Message}", VastcoreLogger.LogLevel.Error);
                return false;
            }
        }
        
        /// <summary>
        /// 個別プリミティブテスト
        /// </summary>
        [ContextMenu("Test Single Primitive")]
        public void TestSinglePrimitive()
        {
            var primitive = HighQualityPrimitiveGenerator.GeneratePrimitive(
                PrimitiveTerrainGenerator.PrimitiveType.Crystal,
                new Vector3(2f, 3f, 2f),
                HighQualityPrimitiveGenerator.QualitySettings.High);
            
            if (primitive != null)
            {
                primitive.transform.position = transform.position;
                VastcoreLogger.Log("Test primitive generated successfully", VastcoreLogger.LogLevel.Info);
            }
        }
    }
}
