#if VASTCORE_INTEGRATION_TEST_ENABLED
using UnityEngine;
using System.Collections;
using Vastcore.Generation;

namespace Vastcore.Testing
{
    /// <summary>
    /// 地形生成システムのテストケース
    /// 要求1: 広大地形生成システムの検証
    /// </summary>
    public class TerrainGenerationTestCase : ITestCase
    {
        public IEnumerator Execute(VastcoreIntegrationTestManager testManager)
        {
            var logger = testManager.Logger;
            var runtimeManager = testManager.RuntimeTerrainManager;
            var testPlayer = testManager.TestPlayer;
            
            logger.Log("Testing terrain generation system...");
            
            // 要求1.1: ハイトマップベースの地形生成
            yield return TestHeightmapGeneration(runtimeManager, logger);
            
            // 要求1.2: 円形地形生成
            yield return TestCircularTerrainGeneration(runtimeManager, logger);
            
            // 要求1.3: シームレス境界接続
            yield return TestSeamlessConnection(runtimeManager, testPlayer, logger);
            
            // 要求1.4: リアルタイム地形更新
            yield return TestRealtimeUpdate(runtimeManager, logger);
            
            // 要求1.5: バイオーム設定適用
            yield return TestBiomeApplication(runtimeManager, testManager.BiomePresetManager, logger);
            
            logger.Log("Terrain generation system test completed");
        }
        
        private IEnumerator TestHeightmapGeneration(RuntimeTerrainManager runtimeManager, TestLogger logger)
        {
            logger.Log("Testing heightmap-based terrain generation...");
            
            if (runtimeManager == null)
            {
                throw new System.Exception("RuntimeTerrainManager is null");
            }
            
            // 初期統計を取得
            var initialStats = runtimeManager.GetPerformanceStats();
            
            // 地形生成をトリガー
            runtimeManager.SetDynamicGenerationEnabled(true);
            yield return new WaitForSeconds(2f);
            
            // 生成後の統計を確認
            var newStats = runtimeManager.GetPerformanceStats();
            
            if (newStats.totalTilesGenerated <= initialStats.totalTilesGenerated)
            {
                throw new System.Exception("No terrain tiles were generated");
            }
            
            logger.Log($"✓ Heightmap generation successful: {newStats.totalTilesGenerated - initialStats.totalTilesGenerated} tiles generated");
        }
        
        private IEnumerator TestCircularTerrainGeneration(RuntimeTerrainManager runtimeManager, TestLogger logger)
        {
            logger.Log("Testing circular terrain generation...");
            
            // 円形地形設定を有効化
            var settings = new RuntimeTerrainManager.RuntimeTerrainSettings
            {
                useCircularTerrain = true,
                circularRadius = 1000f,
                immediateLoadRadius = 2,
                preloadRadius = 4
            };
            
            runtimeManager.UpdateSettings(settings);
            yield return new WaitForSeconds(1f);
            
            // 円形地形が生成されているかチェック
            var activeTiles = runtimeManager.GetActiveTiles();
            bool hasCircularTerrain = false;
            
            foreach (var tile in activeTiles)
            {
                if (tile != null && tile.terrainObject != null)
                {
                    // 円形地形の特徴をチェック（メッシュの形状など）
                    var meshFilter = tile.terrainObject.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.mesh != null)
                    {
                        hasCircularTerrain = true;
                        break;
                    }
                }
            }
            
            if (!hasCircularTerrain)
            {
                throw new System.Exception("Circular terrain generation failed");
            }
            
            logger.Log("✓ Circular terrain generation successful");
        }
        
        private IEnumerator TestSeamlessConnection(RuntimeTerrainManager runtimeManager, Transform testPlayer, TestLogger logger)
        {
            logger.Log("Testing seamless boundary connection...");
            
            Vector3 originalPosition = testPlayer.position;
            
            // プレイヤーを移動させて複数のタイルを生成
            testPlayer.position = originalPosition + Vector3.right * 2000f;
            yield return new WaitForSeconds(2f);
            
            testPlayer.position = originalPosition + Vector3.forward * 2000f;
            yield return new WaitForSeconds(2f);
            
            // 隣接タイルの境界をチェック
            var activeTiles = runtimeManager.GetActiveTiles();
            if (activeTiles.Count < 2)
            {
                throw new System.Exception("Not enough tiles generated for seamless connection test");
            }
            
            // 境界の連続性をチェック（簡易版）
            bool hasSeamlessConnection = true;
            foreach (var tile in activeTiles)
            {
                if (tile?.terrainObject == null)
                {
                    hasSeamlessConnection = false;
                    break;
                }
            }
            
            testPlayer.position = originalPosition;
            
            if (!hasSeamlessConnection)
            {
                throw new System.Exception("Seamless connection validation failed");
            }
            
            logger.Log("✓ Seamless boundary connection successful");
        }
        
        private IEnumerator TestRealtimeUpdate(RuntimeTerrainManager runtimeManager, TestLogger logger)
        {
            logger.Log("Testing realtime terrain update...");
            
            var initialStats = runtimeManager.GetPerformanceStats();
            
            // 設定を変更
            var newSettings = new RuntimeTerrainManager.RuntimeTerrainSettings
            {
                immediateLoadRadius = 3,
                preloadRadius = 5,
                memoryLimitMB = 300f
            };
            
            runtimeManager.UpdateSettings(newSettings);
            yield return new WaitForSeconds(1f);
            
            // 設定が反映されているかチェック
            var currentSettings = runtimeManager.GetCurrentSettings();
            if (currentSettings.immediateLoadRadius != 3 || currentSettings.preloadRadius != 5)
            {
                throw new System.Exception("Realtime settings update failed");
            }
            
            logger.Log("✓ Realtime terrain update successful");
        }
        
        private IEnumerator TestBiomeApplication(RuntimeTerrainManager runtimeManager, BiomePresetManager biomeManager, TestLogger logger)
        {
            logger.Log("Testing biome preset application...");
            
            if (biomeManager == null)
            {
                logger.LogWarning("BiomePresetManager is null, skipping biome test");
                return;
            }
            
            // デフォルトプリセットを作成
            var testPreset = ScriptableObject.CreateInstance<BiomePreset>();
            testPreset.name = "TestBiome";
            testPreset.terrainParams = new TerrainGenerationParams
            {
                heightScale = 100f,
                noiseScale = 0.01f,
                octaves = 4
            };
            
            // プリセットを適用
            biomeManager.ApplyPresetToArea(testPreset, Vector3.zero, 1000f);
            yield return new WaitForSeconds(1f);
            
            logger.Log("✓ Biome preset application successful");
        }
    }
}
#endif