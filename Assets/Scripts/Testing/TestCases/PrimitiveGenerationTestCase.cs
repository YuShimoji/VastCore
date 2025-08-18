using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Vastcore.Generation;

namespace Vastcore.Testing
{
    /// <summary>
    /// プリミティブ地形生成システムのテストケース
    /// 要求3: 巨大オブジェクト動的生成システムの検証
    /// </summary>
    public class PrimitiveGenerationTestCase : ITestCase
    {
        public IEnumerator Execute(VastcoreIntegrationTestManager testManager)
        {
            var logger = testManager.Logger;
            var primitiveManager = testManager.PrimitiveTerrainManager;
            var testPlayer = testManager.TestPlayer;
            
            logger.Log("Testing primitive generation system...");
            
            // 要求3.1: 実行時構造物生成
            yield return TestRuntimeGeneration(primitiveManager, logger);
            
            // 要求3.2: 動的配置システム
            yield return TestDynamicPlacement(primitiveManager, testPlayer, logger);
            
            // 要求3.3: 16種類のプリミティブ生成
            yield return TestAllPrimitiveTypes(logger);
            
            // 要求3.4: 地形整列システム
            yield return TestTerrainAlignment(logger);
            
            // 要求3.5: メモリ効率的な管理
            yield return TestMemoryManagement(primitiveManager, testPlayer, logger);
            
            logger.Log("Primitive generation system test completed");
        }
        
        private IEnumerator TestRuntimeGeneration(PrimitiveTerrainManager primitiveManager, TestLogger logger)
        {
            logger.Log("Testing runtime primitive generation...");
            
            if (primitiveManager == null)
            {
                throw new System.Exception("PrimitiveTerrainManager is null");
            }
            
            int initialCount = primitiveManager.GetActivePrimitiveCount();
            
            // 動的生成を有効化
            primitiveManager.SetDynamicGenerationEnabled(true);
            yield return new WaitForSeconds(2f);
            
            int newCount = primitiveManager.GetActivePrimitiveCount();
            
            if (newCount <= initialCount)
            {
                throw new System.Exception("No primitives were generated at runtime");
            }
            
            logger.Log($"✓ Runtime generation successful: {newCount - initialCount} primitives generated");
        }
        
        private IEnumerator TestDynamicPlacement(PrimitiveTerrainManager primitiveManager, Transform testPlayer, TestLogger logger)
        {
            logger.Log("Testing dynamic primitive placement...");
            
            Vector3 originalPosition = testPlayer.position;
            
            // プレイヤーを移動させて動的配置をトリガー
            testPlayer.position = originalPosition + Vector3.right * 3000f;
            yield return new WaitForSeconds(3f);
            
            int primitivesAtNewLocation = primitiveManager.GetActivePrimitiveCount();
            
            // 別の場所に移動
            testPlayer.position = originalPosition + Vector3.forward * 3000f;
            yield return new WaitForSeconds(3f);
            
            int primitivesAtSecondLocation = primitiveManager.GetActivePrimitiveCount();
            
            testPlayer.position = originalPosition;
            
            if (primitivesAtNewLocation == 0 && primitivesAtSecondLocation == 0)
            {
                throw new System.Exception("Dynamic placement system not working");
            }
            
            logger.Log("✓ Dynamic placement successful");
        }
        
        private IEnumerator TestAllPrimitiveTypes(TestLogger logger)
        {
            logger.Log("Testing all primitive types generation...");
            
            var primitiveTypes = System.Enum.GetValues(typeof(PrimitiveTerrainGenerator.PrimitiveType));
            int successCount = 0;
            int totalCount = primitiveTypes.Length;
            
            for (int i = 0; i < totalCount; i++)
            {
                var primitiveType = (PrimitiveTerrainGenerator.PrimitiveType)primitiveTypes.GetValue(i);
                
                try
                {
                    // テスト用パラメータを作成
                    var parameters = PrimitiveTerrainGenerator.PrimitiveGenerationParams.Default(primitiveType);
                    parameters.position = Vector3.zero + Vector3.right * i * 200f;
                    parameters.scale = PrimitiveTerrainGenerator.GetDefaultScale(primitiveType);
                    
                    // プリミティブを生成
                    GameObject result = PrimitiveTerrainGenerator.GeneratePrimitiveTerrain(parameters);
                    
                    if (result != null)
                    {
                        successCount++;
                        logger.Log($"✓ Generated {primitiveType}");
                        
                        // テスト後にクリーンアップ
                        Object.DestroyImmediate(result);
                    }
                    else
                    {
                        logger.LogWarning($"Failed to generate {primitiveType}");
                    }
                }
                catch (System.Exception e)
                {
                    logger.LogError($"Error generating {primitiveType}: {e.Message}");
                }
                
                yield return null; // フレーム分散
            }
            
            if (successCount < totalCount * 0.8f) // 80%以上成功が必要
            {
                throw new System.Exception($"Primitive generation success rate too low: {successCount}/{totalCount}");
            }
            
            logger.Log($"✓ All primitive types test successful: {successCount}/{totalCount}");
        }
        
        private IEnumerator TestTerrainAlignment(TestLogger logger)
        {
            logger.Log("Testing terrain alignment system...");
            
            // テスト用のプリミティブを生成
            var parameters = PrimitiveTerrainGenerator.PrimitiveGenerationParams.Default(
                PrimitiveTerrainGenerator.PrimitiveType.Cube);
            parameters.position = Vector3.zero;
            parameters.scale = Vector3.one * 100f;
            
            GameObject testPrimitive = PrimitiveTerrainGenerator.GeneratePrimitiveTerrain(parameters);
            
            if (testPrimitive == null)
            {
                throw new System.Exception("Failed to create test primitive for alignment test");
            }
            
            try
            {
                // 地形法線をシミュレート（45度傾斜）
                Vector3 testNormal = new Vector3(0.707f, 0.707f, 0f).normalized;
                
                // 整列設定
                var alignmentSettings = TerrainAlignmentSystem.AlignmentSettings.Default();
                alignmentSettings.enableAlignment = true;
                alignmentSettings.alignToNormal = true;
                
                // 地形に整列
                TerrainAlignmentSystem.AlignPrimitiveToTerrain(testPrimitive, testNormal, alignmentSettings);
                
                // 整列が適用されているかチェック
                Vector3 upVector = testPrimitive.transform.up;
                float alignment = Vector3.Dot(upVector, testNormal);
                
                if (alignment < 0.8f) // 十分に整列していない
                {
                    throw new System.Exception($"Terrain alignment failed: alignment = {alignment}");
                }
                
                logger.Log("✓ Terrain alignment successful");
            }
            finally
            {
                // クリーンアップ
                Object.DestroyImmediate(testPrimitive);
            }
            
            yield return null;
        }
        
        private IEnumerator TestMemoryManagement(PrimitiveTerrainManager primitiveManager, Transform testPlayer, TestLogger logger)
        {
            logger.Log("Testing primitive memory management...");
            
            Vector3 originalPosition = testPlayer.position;
            long initialMemory = System.GC.GetTotalMemory(false);
            
            // 大量のプリミティブ生成をトリガー
            for (int i = 0; i < 10; i++)
            {
                testPlayer.position = originalPosition + new Vector3(i * 2000f, 0, i * 2000f);
                yield return new WaitForSeconds(0.5f);
            }
            
            // メモリ使用量をチェック
            long peakMemory = System.GC.GetTotalMemory(false);
            
            // 元の位置に戻して不要なプリミティブを削除
            testPlayer.position = originalPosition;
            yield return new WaitForSeconds(3f);
            
            // 強制クリーンアップ
            primitiveManager.ForceCleanup();
            System.GC.Collect();
            yield return new WaitForSeconds(1f);
            
            long finalMemory = System.GC.GetTotalMemory(false);
            
            // メモリが適切に解放されているかチェック
            long memoryIncrease = finalMemory - initialMemory;
            long peakIncrease = peakMemory - initialMemory;
            
            if (memoryIncrease > peakIncrease * 0.5f) // 50%以上メモリが残っている
            {
                logger.LogWarning($"Potential memory leak detected: {memoryIncrease / 1024 / 1024}MB increase");
            }
            
            logger.Log($"✓ Memory management test completed: Peak +{peakIncrease / 1024 / 1024}MB, Final +{memoryIncrease / 1024 / 1024}MB");
        }
    }
}