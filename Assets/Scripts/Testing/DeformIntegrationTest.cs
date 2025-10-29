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
            Debug.Log("Starting Deform Integration Test");
            
            bool allTestsPassed = true;
            
            // 1. VastcoreDeformManagerの初期化テスト
            allTestsPassed &= TestManagerInitialization();
            
            // 2. プリミティブ生成テスト
            allTestsPassed &= TestPrimitiveGeneration();
            
            // 3. Deformコンポーネント適用テスト
            allTestsPassed &= TestDeformComponentApplication();
            
            // 4. 品質レベル切り替えテスト
            allTestsPassed &= TestQualitySwitching();
            
            // 5. パフォーマンステスト
            if (enablePerformanceTest)
            {
                allTestsPassed &= TestPerformance();
            }
            
            // 6. プリセットライブラリテスト
            allTestsPassed &= TestPresetLibrary();
            
            string result = allTestsPassed ? "PASSED" : "FAILED";
            Debug.Log($"Deform Integration Test completed: {result}");
        }
        
        /// <summary>
        /// VastcoreDeformManagerの初期化テスト
        /// </summary>
        public bool TestManagerInitialization()
        {
            Debug.Log("Testing VastcoreDeformManager initialization...");
            
            try
            {
                var manager = VastcoreDeformManager.Instance;
                if (manager == null)
                {
                    Debug.Log("VastcoreDeformManager instance is null");
                    return false;
                }
                
                var stats = manager.GetStats();
                Debug.Log($"DeformManager Stats - Managed: {stats.managedDeformablesCount}, Queued: {stats.queuedRequestsCount}, Enabled: {stats.systemEnabled}");
                
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.Log($"DeformManager initialization failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// プリミティブ生成テスト
        /// </summary>
        public bool TestPrimitiveGeneration()
        {
            Debug.Log("Testing primitive generation with Deform integration...");
            
            bool allPassed = true;
            
            foreach (var primitiveType in testPrimitives)
            {
                try
                {
                    var quality = HighQualityPrimitiveGenerator.QualitySettings.High;
                    // var primitive = HighQualityPrimitiveGenerator.GeneratePrimitive(
                    //     primitiveType, Vector3.one, quality);
                    GameObject primitive = null; // TODO: 未実装のためコメント化
                    
                    if (primitive == null)
                    {
                        Debug.Log($"Failed to generate {primitiveType}");
                        allPassed = false;
                        continue;
                    }
                    
#if DEFORM_AVAILABLE
                    // Deformableコンポーネントの確認
                    var deformable = primitive.GetComponent<Deformable>();
                    if (quality.enableDeformSystem && deformable == null)
                    {
                        Debug.Log($"Deformable component missing on {primitiveType}");
                    }
                    
                    // Deformerコンポーネントの確認
                    var deformers = primitive.GetComponents<Deformer>();
                    if (quality.enableDeformSystem && deformers.Length == 0)
                    {
                        Debug.Log($"No Deformer components found on {primitiveType}");
                    }
                    
                    Debug.Log($"Successfully generated {primitiveType} with {deformers.Length} deformers");
#else
                    Debug.Log($"Successfully generated {primitiveType} (Deform disabled)");
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
                    Debug.Log($"Exception testing {primitiveType}: {ex.Message}");
                    allPassed = false;
                }
            }
            
            return allPassed;
        }
        
        /// <summary>
        /// Deformコンポーネント適用テスト
        /// </summary>
        public bool TestDeformComponentApplication()
        {
            Debug.Log("Testing Deform component application...");
            
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
                Debug.Log($"Deform component application test failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 品質レベル切り替えテスト
        /// </summary>
        public bool TestQualitySwitching()
        {
            Debug.Log("Testing quality level switching...");
            
            bool allPassed = true;
            
            foreach (var quality in qualityLevels)
            {
                try
                {
                    // var primitive = HighQualityPrimitiveGenerator.GeneratePrimitive(
                    //     PrimitiveTerrainGenerator.PrimitiveType.Cube, Vector3.one, quality);
                    GameObject primitive = null; // TODO: 未実装のためコメント化
                    
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
                    
                    Debug.Log($"Quality {quality.deformQuality}: Expected {expectedDeformers}, Found {deformers.Length} deformers");
                    
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
                    Debug.Log($"Quality level test failed: {ex.Message}");
                    allPassed = false;
                }
            }
            
            return allPassed;
        }
        
        /// <summary>
        /// パフォーマンステスト
        /// </summary>
        public bool TestPerformance()
        {
            Debug.Log("Running performance test...");
            
            try
            {
                float totalTime = 0f;
                int successCount = 0;
                
                for (int i = 0; i < testIterations; i++)
                {
                    float startTime = Time.realtimeSinceStartup;
                    
                    // var primitive = HighQualityPrimitiveGenerator.GeneratePrimitive(
                    //     PrimitiveTerrainGenerator.PrimitiveType.Sphere, 
                    //     Vector3.one, 
                    //     HighQualityPrimitiveGenerator.QualitySettings.High);
                    GameObject primitive = null; // TODO: 未実装のためコメント化
                    
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
                Debug.Log($"Performance Test Results: {successCount}/{testIterations} successful, Average time: {averageTime * 1000f:F2}ms");
                
                return successCount == testIterations && averageTime < 0.1f; // 100ms以下を目標
            }
            catch (System.Exception ex)
            {
                Debug.Log($"Performance test failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// プリセットライブラリテスト
        /// </summary>
        public bool TestPresetLibrary()
        {
            Debug.Log("Testing DeformPresetLibrary...");
            
            try
            {
                // ScriptableObjectとしてプリセットライブラリを作成
                var library = ScriptableObject.CreateInstance<DeformPresetLibrary>();
                
                // テスト用GameObjectを作成
                var testObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                
                // プリセット適用テスト
                library.ApplyPresetToGameObject(testObject, "風化侵食", 0.5f);
                
                // Deformerが追加されたかチェック
#if DEFORM_AVAILABLE
                var deformers = testObject.GetComponents<Deformer>();
                bool hasDeformers = deformers.Length > 0;
#else
                bool hasDeformers = false; // Deform disabled
#endif
                
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
                
                Debug.Log($"Preset library test: {(hasDeformers ? "PASSED" : "FAILED")}");
                
                return hasDeformers;
            }
            catch (System.Exception ex)
            {
                Debug.Log($"Preset library test failed: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 個別プリミティブテスト
        /// </summary>
        [ContextMenu("Test Single Primitive")]
        public void TestSinglePrimitive()
        {
            // var primitive = HighQualityPrimitiveGenerator.GeneratePrimitive(
            //     PrimitiveTerrainGenerator.PrimitiveType.Crystal,
            //     new Vector3(2f, 3f, 2f),
            //     HighQualityPrimitiveGenerator.QualitySettings.High);
            GameObject primitive = null; // TODO: 未実装のためコメント化
            
            if (primitive != null)
            {
                primitive.transform.position = transform.position;
                Debug.Log("Test primitive generated successfully");
            }
        }
    }
}
