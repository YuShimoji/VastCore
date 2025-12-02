// Disabled: VastcoreLogger.Log API not yet available
#if VASTCORE_DEFORM_INTEGRATION_ENABLED
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

        /// <summary>
        /// Deformパフォーマンスベンチマークを実行
        /// </summary>
        [ContextMenu("Run Performance Benchmark")]
        public void RunPerformanceBenchmark()
        {
            StartCoroutine(PerformanceBenchmarkCoroutine());
        }

        private System.Collections.IEnumerator PerformanceBenchmarkCoroutine()
        {
            VastcoreLogger.Log("Starting Deform Performance Benchmark", VastcoreLogger.LogLevel.Info);

            // テスト対象のプリミティブを生成
            var testPrimitives = new PrimitiveTerrainGenerator.PrimitiveType[] {
                PrimitiveTerrainGenerator.PrimitiveType.Cube,
                PrimitiveTerrainGenerator.PrimitiveType.Sphere,
                PrimitiveTerrainGenerator.PrimitiveType.Cylinder
            };

            foreach (var primitiveType in testPrimitives)
            {
                yield return StartCoroutine(BenchmarkPrimitiveType(primitiveType));
            }

            VastcoreLogger.Log("Deform Performance Benchmark completed", VastcoreLogger.LogLevel.Info);
        }

        private System.Collections.IEnumerator BenchmarkPrimitiveType(PrimitiveTerrainGenerator.PrimitiveType primitiveType)
        {
            VastcoreLogger.Log($"Benchmarking {primitiveType}", VastcoreLogger.LogLevel.Info);

            // 変形なしのベースラインベンチマーク
            float baselineTime = BenchmarkWithoutDeformation(primitiveType);
            VastcoreLogger.Log($"{primitiveType} - Baseline (no deformation): {baselineTime:F4} seconds", VastcoreLogger.LogLevel.Info);

            // 変形ありのベンチマーク
            float deformedTime = BenchmarkWithDeformation(primitiveType);
            VastcoreLogger.Log($"{primitiveType} - With deformation: {deformedTime:F4} seconds", VastcoreLogger.LogLevel.Info);

            // パフォーマンス差を計算
            float overhead = deformedTime - baselineTime;
            float overheadPercent = (overhead / baselineTime) * 100f;
            VastcoreLogger.Log($"{primitiveType} - Performance overhead: {overhead:F4}s ({overheadPercent:F1}%)", VastcoreLogger.LogLevel.Info);

            yield return null;
        }

        private float BenchmarkWithoutDeformation(PrimitiveTerrainGenerator.PrimitiveType primitiveType)
        {
            float totalTime = 0f;
            int iterations = 10;

            for (int i = 0; i < iterations; i++)
            {
                float startTime = Time.realtimeSinceStartup;

                // 変形なしでプリミティブ生成
                var parameters = PrimitiveGenerationParams.Default(primitiveType);
                parameters.enableDeformation = false;
                parameters.generateCollider = false; // コライダー生成をスキップして純粋なメッシュ生成時間を計測

                var primitive = PrimitiveTerrainGenerator.GeneratePrimitiveTerrain(parameters);

                float endTime = Time.realtimeSinceStartup;
                totalTime += (endTime - startTime);

                // 生成されたオブジェクトを破棄
                if (primitive != null)
                {
                    DestroyImmediate(primitive);
                }
            }

            return totalTime / iterations;
        }

        private float BenchmarkWithDeformation(PrimitiveTerrainGenerator.PrimitiveType primitiveType)
        {
            float totalTime = 0f;
            int iterations = 10;

            for (int i = 0; i < iterations; i++)
            {
                float startTime = Time.realtimeSinceStartup;

                // 変形ありでプリミティブ生成
                var parameters = PrimitiveGenerationParams.Default(primitiveType);
                parameters.enableDeformation = true;
                parameters.generateCollider = false;

                var primitive = PrimitiveTerrainGenerator.GeneratePrimitiveTerrain(parameters);

                // 変形適用時間を計測
                if (primitive != null)
                {
                    var terrainObj = primitive.GetComponent<PrimitiveTerrainObject>();
                    if (terrainObj != null)
                    {
                        terrainObj.ApplyTerrainSpecificDeformation();
                    }
                }

                float endTime = Time.realtimeSinceStartup;
                totalTime += (endTime - startTime);

                // 生成されたオブジェクトを破棄
                if (primitive != null)
                {
                    DestroyImmediate(primitive);
                }
            }

            return totalTime / iterations;
        }

        /// <summary>
        /// メモリ使用量ベンチマーク
        /// </summary>
        [ContextMenu("Run Memory Benchmark")]
        public void RunMemoryBenchmark()
        {
            VastcoreLogger.Log("Starting Memory Usage Benchmark", VastcoreLogger.LogLevel.Info);

            long initialMemory = System.GC.GetTotalMemory(true);

            // 変形ありで多数のプリミティブを生成
            var primitives = new System.Collections.Generic.List<GameObject>();
            for (int i = 0; i < 50; i++)
            {
                var parameters = PrimitiveGenerationParams.Default(PrimitiveTerrainGenerator.PrimitiveType.Cube);
                parameters.enableDeformation = true;
                parameters.position = new Vector3(i * 2f, 0f, 0f);

                var primitive = PrimitiveTerrainGenerator.GeneratePrimitiveTerrain(parameters);
                if (primitive != null)
                {
                    primitives.Add(primitive);
                    var terrainObj = primitive.GetComponent<PrimitiveTerrainObject>();
                    if (terrainObj != null)
                    {
                        terrainObj.ApplyTerrainSpecificDeformation();
                    }
                }
            }

            // メモリ使用量を計測
            System.GC.Collect();
            long finalMemory = System.GC.GetTotalMemory(true);
            long memoryUsage = finalMemory - initialMemory;

            VastcoreLogger.Log($"Memory usage for 50 deformed primitives: {memoryUsage / 1024 / 1024} MB", VastcoreLogger.LogLevel.Info);

            // クリーンアップ
            foreach (var primitive in primitives)
            {
                DestroyImmediate(primitive);
            }
        }

        /// <summary>
        /// 既存の地形機能との互換性テストを実行
        /// </summary>
        [ContextMenu("Run Compatibility Test")]
        public void RunCompatibilityTest()
        {
            StartCoroutine(CompatibilityTestCoroutine());
        }

        private System.Collections.IEnumerator CompatibilityTestCoroutine()
        {
            VastcoreLogger.Log("Starting Deform Compatibility Test", VastcoreLogger.LogLevel.Info);

            // LODシステムとの互換性テスト
            yield return StartCoroutine(TestLODCompatibility());

            // プーリングシステムとの互換性テスト
            yield return StartCoroutine(TestPoolingCompatibility());

            // コライダーシステムとの互換性テスト
            yield return StartCoroutine(TestColliderCompatibility());

            VastcoreLogger.Log("Deform Compatibility Test completed", VastcoreLogger.LogLevel.Info);
        }

        private System.Collections.IEnumerator TestLODCompatibility()
        {
            VastcoreLogger.Log("Testing LOD system compatibility", VastcoreLogger.LogLevel.Info);

            // 変形ありのプリミティブを生成
            var parameters = PrimitiveGenerationParams.Default(PrimitiveTerrainGenerator.PrimitiveType.Cube);
            parameters.enableDeformation = true;
            parameters.generateCollider = true;

            var primitive = PrimitiveTerrainGenerator.GeneratePrimitiveTerrain(parameters);
            if (primitive != null)
            {
                primitive.transform.position = new Vector3(0f, 0f, 10f); // 少し離れた位置

                var terrainObj = primitive.GetComponent<PrimitiveTerrainObject>();
                if (terrainObj != null)
                {
                    // 地形固有変形を適用
                    terrainObj.ApplyTerrainSpecificDeformation();

                    // カメラをシミュレート
                    var testCamera = new GameObject("TestCamera").AddComponent<Camera>();
                    testCamera.transform.position = new Vector3(0f, 0f, 0f);

                    // LOD更新をテスト
                    for (int distance = 50; distance <= 250; distance += 50)
                    {
                        testCamera.transform.position = new Vector3(0f, 0f, -distance);
                        terrainObj.UpdateLOD(Vector3.Distance(primitive.transform.position, testCamera.transform.position));

                        VastcoreLogger.Log($"LOD test at distance {distance}m: LOD level {terrainObj.GetLODInfo().currentLOD}", VastcoreLogger.LogLevel.Info);

                        yield return null;
                    }

                    DestroyImmediate(testCamera.gameObject);
                }

                DestroyImmediate(primitive);
                VastcoreLogger.Log("LOD compatibility test passed", VastcoreLogger.LogLevel.Info);
            }
            else
            {
                VastcoreLogger.Log("LOD compatibility test failed - primitive generation failed", VastcoreLogger.LogLevel.Error);
            }
        }

        private System.Collections.IEnumerator TestPoolingCompatibility()
        {
            VastcoreLogger.Log("Testing pooling system compatibility", VastcoreLogger.LogLevel.Info);

            // プーリング可能なプリミティブを生成
            var parameters = PrimitiveGenerationParams.Default(PrimitiveTerrainGenerator.PrimitiveType.Sphere);
            parameters.enableDeformation = true;

            var primitive = PrimitiveTerrainGenerator.GeneratePrimitiveTerrain(parameters);
            if (primitive != null)
            {
                var terrainObj = primitive.GetComponent<PrimitiveTerrainObject>();
                if (terrainObj != null)
                {
                    // 変形を適用
                    terrainObj.ApplyTerrainSpecificDeformation();

                    // プールに戻すテスト
                    terrainObj.PrepareForPool();

                    // プールから再利用
                    terrainObj.InitializeFromPool(
                        PrimitiveTerrainGenerator.PrimitiveType.Sphere,
                        new Vector3(5f, 0f, 0f),
                        2f);

                    // 再変形を適用
                    terrainObj.ApplyTerrainSpecificDeformation();

                    VastcoreLogger.Log("Pooling compatibility test passed", VastcoreLogger.LogLevel.Info);
                }

                DestroyImmediate(primitive);
            }
            else
            {
                VastcoreLogger.Log("Pooling compatibility test failed - primitive generation failed", VastcoreLogger.LogLevel.Error);
            }

            yield return null;
        }

        private System.Collections.IEnumerator TestColliderCompatibility()
        {
            VastcoreLogger.Log("Testing collider system compatibility", VastcoreLogger.LogLevel.Info);

            // コライダー付きプリミティブを生成
            var parameters = PrimitiveGenerationParams.Default(PrimitiveTerrainGenerator.PrimitiveType.Cylinder);
            parameters.enableDeformation = true;
            parameters.generateCollider = true;

            var primitive = PrimitiveTerrainGenerator.GeneratePrimitiveTerrain(parameters);
            if (primitive != null)
            {
                var terrainObj = primitive.GetComponent<PrimitiveTerrainObject>();
                var meshCollider = primitive.GetComponent<MeshCollider>();

                if (terrainObj != null && meshCollider != null)
                {
                    // 変形を適用
                    terrainObj.ApplyTerrainSpecificDeformation();

                    // コライダーが有効かチェック
                    if (meshCollider.enabled && meshCollider.sharedMesh != null)
                    {
                        VastcoreLogger.Log("Collider compatibility test passed", VastcoreLogger.LogLevel.Info);
                    }
                    else
                    {
                        VastcoreLogger.Log("Collider compatibility test failed - collider not properly configured", VastcoreLogger.LogLevel.Error);
                    }

                    // インタラクション機能をテスト
                    bool canClimb = terrainObj.CanInteract("climb");
                    bool canGrind = terrainObj.CanInteract("grind");

                    VastcoreLogger.Log($"Interaction test - CanClimb: {canClimb}, CanGrind: {canGrind}", VastcoreLogger.LogLevel.Info);
                }
                else
                {
                    VastcoreLogger.Log("Collider compatibility test failed - components missing", VastcoreLogger.LogLevel.Error);
                }

                DestroyImmediate(primitive);
            }
            else
            {
                VastcoreLogger.Log("Collider compatibility test failed - primitive generation failed", VastcoreLogger.LogLevel.Error);
            }

            yield return null;
        }
    }
}
#endif
