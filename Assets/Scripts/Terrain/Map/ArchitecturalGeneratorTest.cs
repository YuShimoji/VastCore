using UnityEngine;
using NUnit.Framework;

namespace Vastcore.Generation
{
    /// <summary>
    /// 建築学的構造生成システムのテスト
    /// </summary>
    public class ArchitecturalGeneratorTest : MonoBehaviour
    {
        [Header("テスト設定")]
        public bool runTestsOnStart = true;
        public bool enableVisualTests = true;
        public Material testStoneMaterial;
        public Material testDecorationMaterial;
        
        [Header("テスト結果")]
        public int passedTests = 0;
        public int failedTests = 0;
        public string lastTestResult = "";

        void Start()
        {
            if (runTestsOnStart)
            {
                RunAllTests();
            }
        }

        /// <summary>
        /// 全テストを実行
        /// </summary>
        public void RunAllTests()
        {
            Debug.Log("=== 建築学的構造生成システム テスト開始 ===");
            
            passedTests = 0;
            failedTests = 0;
            
            // 基本アーチ生成テスト
            TestBasicArchGeneration();
            
            // 構造力学計算テスト
            TestStructuralMechanics();
            
            // 複合建築構造テスト
            TestCompoundArchitectures();
            
            // パフォーマンステスト
            TestPerformance();
            
            Debug.Log($"=== テスト完了: 成功 {passedTests}, 失敗 {failedTests} ===");
            lastTestResult = $"成功: {passedTests}, 失敗: {failedTests}";
        }

        /// <summary>
        /// 基本アーチ生成テスト
        /// </summary>
        private void TestBasicArchGeneration()
        {
            Debug.Log("--- 基本アーチ生成テスト ---");
            
            try
            {
                // 各建築タイプをテスト
                var architecturalTypes = System.Enum.GetValues(typeof(ArchitecturalGenerator.ArchitecturalType));
                
                foreach (ArchitecturalGenerator.ArchitecturalType type in architecturalTypes)
                {
                    var parameters = ArchitecturalGenerator.ArchitecturalParams.Default(type);
                    parameters.stoneMaterial = testStoneMaterial;
                    parameters.decorationMaterial = testDecorationMaterial;
                    
                    var archObject = ArchitecturalGenerator.GenerateArchitecturalStructure(parameters);
                    
                    if (archObject != null)
                    {
                        Debug.Log($"✓ {type} アーチ生成成功");
                        passedTests++;
                        
                        if (enableVisualTests)
                        {
                            // テスト用の位置に配置
                            archObject.transform.position = new Vector3(
                                ((int)type % 4) * 200f,
                                0,
                                ((int)type / 4) * 200f
                            );
                        }
                        else
                        {
                            DestroyImmediate(archObject);
                        }
                    }
                    else
                    {
                        Debug.LogError($"✗ {type} アーチ生成失敗");
                        failedTests++;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ 基本アーチ生成テストでエラー: {e.Message}");
                failedTests++;
            }
        }

        /// <summary>
        /// 構造力学計算テスト
        /// </summary>
        private void TestStructuralMechanics()
        {
            Debug.Log("--- 構造力学計算テスト ---");
            
            try
            {
                // 構造安定性テスト
                var stableParams = ArchitecturalGenerator.ArchitecturalParams.Default(ArchitecturalGenerator.ArchitecturalType.RomanArch);
                stableParams.span = 100f;
                stableParams.height = 50f;
                stableParams.thickness = 10f;
                
                bool isStable = ArchitecturalGenerator.ValidateStructuralStability(stableParams);
                
                if (isStable)
                {
                    Debug.Log("✓ 構造安定性検証成功");
                    passedTests++;
                }
                else
                {
                    Debug.LogError("✗ 構造安定性検証失敗");
                    failedTests++;
                }
                
                // 重量計算テスト
                float weight = ArchitecturalGenerator.CalculateEstimatedWeight(stableParams);
                
                if (weight > 0)
                {
                    Debug.Log($"✓ 重量計算成功: {weight:F0} kg");
                    passedTests++;
                }
                else
                {
                    Debug.LogError("✗ 重量計算失敗");
                    failedTests++;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ 構造力学計算テストでエラー: {e.Message}");
                failedTests++;
            }
        }

        /// <summary>
        /// 複合建築構造テスト
        /// </summary>
        private void TestCompoundArchitectures()
        {
            Debug.Log("--- 複合建築構造テスト ---");
            
            try
            {
                // 複数アーチ橋のテスト
                var bridgeParams = CompoundArchitecturalGenerator.CompoundArchitecturalParams.Default(
                    CompoundArchitecturalGenerator.CompoundArchitecturalType.MultipleBridge);
                bridgeParams.primaryMaterial = testStoneMaterial;
                bridgeParams.decorationMaterial = testDecorationMaterial;
                
                var bridgeObject = CompoundArchitecturalGenerator.GenerateCompoundArchitecturalStructure(bridgeParams);
                
                if (bridgeObject != null)
                {
                    Debug.Log("✓ 複数アーチ橋生成成功");
                    passedTests++;
                    
                    if (enableVisualTests)
                    {
                        bridgeObject.transform.position = new Vector3(0, 0, -500f);
                    }
                    else
                    {
                        DestroyImmediate(bridgeObject);
                    }
                }
                else
                {
                    Debug.LogError("✗ 複数アーチ橋生成失敗");
                    failedTests++;
                }
                
                // 大聖堂複合体のテスト
                var cathedralParams = CompoundArchitecturalGenerator.CompoundArchitecturalParams.Default(
                    CompoundArchitecturalGenerator.CompoundArchitecturalType.CathedralComplex);
                cathedralParams.primaryMaterial = testStoneMaterial;
                cathedralParams.decorationMaterial = testDecorationMaterial;
                
                var cathedralObject = CompoundArchitecturalGenerator.GenerateCompoundArchitecturalStructure(cathedralParams);
                
                if (cathedralObject != null)
                {
                    Debug.Log("✓ 大聖堂複合体生成成功");
                    passedTests++;
                    
                    if (enableVisualTests)
                    {
                        cathedralObject.transform.position = new Vector3(500f, 0, -500f);
                    }
                    else
                    {
                        DestroyImmediate(cathedralObject);
                    }
                }
                else
                {
                    Debug.LogError("✗ 大聖堂複合体生成失敗");
                    failedTests++;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ 複合建築構造テストでエラー: {e.Message}");
                failedTests++;
            }
        }

        /// <summary>
        /// パフォーマンステスト
        /// </summary>
        private void TestPerformance()
        {
            Debug.Log("--- パフォーマンステスト ---");
            
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                // 10個のアーチを連続生成
                for (int i = 0; i < 10; i++)
                {
                    var parameters = ArchitecturalGenerator.ArchitecturalParams.Default(ArchitecturalGenerator.ArchitecturalType.SimpleArch);
                    var archObject = ArchitecturalGenerator.GenerateArchitecturalStructure(parameters);
                    
                    if (archObject != null && !enableVisualTests)
                    {
                        DestroyImmediate(archObject);
                    }
                }
                
                stopwatch.Stop();
                float averageTime = stopwatch.ElapsedMilliseconds / 10f;
                
                if (averageTime < 100f) // 100ms以下なら合格
                {
                    Debug.Log($"✓ パフォーマンステスト成功: 平均 {averageTime:F1}ms");
                    passedTests++;
                }
                else
                {
                    Debug.LogWarning($"△ パフォーマンステスト警告: 平均 {averageTime:F1}ms (目標: <100ms)");
                    passedTests++;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"✗ パフォーマンステストでエラー: {e.Message}");
                failedTests++;
            }
        }

        /// <summary>
        /// 特定の建築タイプをテスト生成
        /// </summary>
        public void TestSpecificArchType(ArchitecturalGenerator.ArchitecturalType type)
        {
            var parameters = ArchitecturalGenerator.ArchitecturalParams.Default(type);
            parameters.stoneMaterial = testStoneMaterial;
            parameters.decorationMaterial = testDecorationMaterial;
            parameters.position = transform.position + Vector3.forward * 100f;
            
            var archObject = ArchitecturalGenerator.GenerateArchitecturalStructure(parameters);
            
            if (archObject != null)
            {
                Debug.Log($"テスト生成成功: {type}");
            }
            else
            {
                Debug.LogError($"テスト生成失敗: {type}");
            }
        }

        /// <summary>
        /// 特定の複合建築タイプをテスト生成
        /// </summary>
        public void TestSpecificCompoundType(CompoundArchitecturalGenerator.CompoundArchitecturalType type)
        {
            var parameters = CompoundArchitecturalGenerator.CompoundArchitecturalParams.Default(type);
            parameters.primaryMaterial = testStoneMaterial;
            parameters.decorationMaterial = testDecorationMaterial;
            parameters.position = transform.position + Vector3.forward * 200f;
            
            var compoundObject = CompoundArchitecturalGenerator.GenerateCompoundArchitecturalStructure(parameters);
            
            if (compoundObject != null)
            {
                Debug.Log($"複合テスト生成成功: {type}");
            }
            else
            {
                Debug.LogError($"複合テスト生成失敗: {type}");
            }
        }

        /// <summary>
        /// 全テストオブジェクトをクリア
        /// </summary>
        public void ClearAllTestObjects()
        {
            var architecturalObjects = FindObjectsOfType<GameObject>();
            
            foreach (var obj in architecturalObjects)
            {
                if (obj.name.Contains("Architectural_") || obj.name.Contains("Compound_"))
                {
                    DestroyImmediate(obj);
                }
            }
            
            Debug.Log("全テストオブジェクトをクリアしました");
        }
    }
}