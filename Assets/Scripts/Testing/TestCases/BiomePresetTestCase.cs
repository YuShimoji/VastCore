using UnityEngine;
using System.Collections;
using Vastcore.Generation;

namespace Vastcore.Testing
{
    /// <summary>
    /// バイオームプリセット管理システムのテストケース
    /// 要求2: プリセット管理システムの検証
    /// </summary>
    public class BiomePresetTestCase : ITestCase
    {
        public IEnumerator Execute(VastcoreIntegrationTestManager testManager)
        {
            var logger = testManager.Logger;
            var biomeManager = testManager.BiomePresetManager;
            
            logger.Log("Testing biome preset system...");
            
            if (biomeManager == null)
            {
                logger.LogWarning("BiomePresetManager is null, skipping biome preset tests");
                return;
            }
            
            // 要求2.1: プリセット保存機能
            yield return TestPresetSaving(biomeManager, logger);
            
            // 要求2.2: バイオーム設定保存
            yield return TestBiomeSettingsSaving(biomeManager, logger);
            
            // 要求2.3: プリセット読み込み機能
            yield return TestPresetLoading(biomeManager, logger);
            
            // 要求2.4: プリセット一覧表示
            yield return TestPresetListing(biomeManager, logger);
            
            logger.Log("Biome preset system test completed");
        }
        
        private IEnumerator TestPresetSaving(BiomePresetManager biomeManager, TestLogger logger)
        {
            logger.Log("Testing preset saving functionality...");
            
            // テスト用プリセットを作成
            var testPreset = ScriptableObject.CreateInstance<BiomePreset>();
            testPreset.name = "IntegrationTestPreset";
            testPreset.terrainParams = new TerrainGenerationParams
            {
                heightScale = 150f,
                noiseScale = 0.02f,
                octaves = 6,
                persistence = 0.5f,
                lacunarity = 2f
            };
            
            try
            {
                // プリセットを保存
                bool saveResult = biomeManager.SavePreset(testPreset);
                
                if (!saveResult)
                {
                    throw new System.Exception("Failed to save preset");
                }
                
                yield return new WaitForSeconds(0.5f);
                
                // 保存されたファイルが存在するかチェック
                string expectedPath = biomeManager.GetPresetPath(testPreset.name);
                if (!System.IO.File.Exists(expectedPath))
                {
                    throw new System.Exception($"Preset file not found at {expectedPath}");
                }
                
                logger.Log("✓ Preset saving successful");
            }
            finally
            {
                // クリーンアップ
                if (testPreset != null)
                {
                    Object.DestroyImmediate(testPreset);
                }
            }
        }
        
        private IEnumerator TestBiomeSettingsSaving(BiomePresetManager biomeManager, TestLogger logger)
        {
            logger.Log("Testing biome settings saving...");
            
            // 複雑なバイオーム設定を作成
            var complexPreset = ScriptableObject.CreateInstance<BiomePreset>();
            complexPreset.name = "ComplexBiomeTest";
            complexPreset.terrainParams = new TerrainGenerationParams
            {
                heightScale = 200f,
                noiseScale = 0.015f,
                octaves = 8
            };
            
            // 構造物設定
            complexPreset.structureSettings = new StructureSpawnSettings
            {
                spawnProbability = 0.1f,
                minDistance = 500f,
                maxDistance = 2000f
            };
            
            // 環境設定
            complexPreset.environmentSettings = new EnvironmentSettings
            {
                fogColor = Color.blue,
                fogDensity = 0.02f,
                ambientColor = Color.gray
            };
            
            try
            {
                bool saveResult = biomeManager.SavePreset(complexPreset);
                
                if (!saveResult)
                {
                    throw new System.Exception("Failed to save complex biome settings");
                }
                
                yield return new WaitForSeconds(0.5f);
                
                logger.Log("✓ Biome settings saving successful");
            }
            finally
            {
                if (complexPreset != null)
                {
                    Object.DestroyImmediate(complexPreset);
                }
            }
        }
        
        private IEnumerator TestPresetLoading(BiomePresetManager biomeManager, TestLogger logger)
        {
            logger.Log("Testing preset loading functionality...");
            
            // まずテスト用プリセットを保存
            var originalPreset = ScriptableObject.CreateInstance<BiomePreset>();
            originalPreset.name = "LoadTestPreset";
            originalPreset.terrainParams = new TerrainGenerationParams
            {
                heightScale = 175f,
                noiseScale = 0.025f,
                octaves = 5
            };
            
            bool saveResult = biomeManager.SavePreset(originalPreset);
            if (!saveResult)
            {
                throw new System.Exception("Failed to save preset for loading test");
            }
            
            yield return new WaitForSeconds(0.5f);
            
            try
            {
                // プリセットを読み込み
                var loadedPreset = biomeManager.LoadPreset("LoadTestPreset");
                
                if (loadedPreset == null)
                {
                    throw new System.Exception("Failed to load preset");
                }
                
                // 設定が正確に復元されているかチェック
                if (Mathf.Abs(loadedPreset.terrainParams.heightScale - originalPreset.terrainParams.heightScale) > 0.01f)
                {
                    throw new System.Exception("Loaded preset settings do not match original");
                }
                
                if (Mathf.Abs(loadedPreset.terrainParams.noiseScale - originalPreset.terrainParams.noiseScale) > 0.001f)
                {
                    throw new System.Exception("Loaded preset noise scale does not match original");
                }
                
                if (loadedPreset.terrainParams.octaves != originalPreset.terrainParams.octaves)
                {
                    throw new System.Exception("Loaded preset octaves do not match original");
                }
                
                logger.Log("✓ Preset loading successful");
            }
            finally
            {
                if (originalPreset != null)
                {
                    Object.DestroyImmediate(originalPreset);
                }
            }
        }
        
        private IEnumerator TestPresetListing(BiomePresetManager biomeManager, TestLogger logger)
        {
            logger.Log("Testing preset listing functionality...");
            
            // 複数のテスト用プリセットを作成
            var presetNames = new string[] { "ListTest1", "ListTest2", "ListTest3" };
            
            foreach (var name in presetNames)
            {
                var preset = ScriptableObject.CreateInstance<BiomePreset>();
                preset.name = name;
                preset.terrainParams = new TerrainGenerationParams
                {
                    heightScale = Random.Range(50f, 200f),
                    noiseScale = Random.Range(0.01f, 0.05f),
                    octaves = Random.Range(3, 8)
                };
                
                biomeManager.SavePreset(preset);
                Object.DestroyImmediate(preset);
            }
            
            yield return new WaitForSeconds(1f);
            
            // プリセット一覧を取得
            var availablePresets = biomeManager.GetAvailablePresetNames();
            
            if (availablePresets == null || availablePresets.Count == 0)
            {
                throw new System.Exception("No presets found in listing");
            }
            
            // 作成したプリセットが一覧に含まれているかチェック
            int foundCount = 0;
            foreach (var name in presetNames)
            {
                if (availablePresets.Contains(name))
                {
                    foundCount++;
                }
            }
            
            if (foundCount < presetNames.Length)
            {
                throw new System.Exception($"Not all test presets found in listing: {foundCount}/{presetNames.Length}");
            }
            
            // カテゴリ別表示のテスト
            var categorizedPresets = biomeManager.GetPresetsByCategory();
            if (categorizedPresets == null)
            {
                logger.LogWarning("Categorized preset listing not implemented");
            }
            else
            {
                logger.Log($"Found {categorizedPresets.Count} preset categories");
            }
            
            logger.Log($"✓ Preset listing successful: {availablePresets.Count} presets found");
        }
    }
}