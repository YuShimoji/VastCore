using UnityEngine;
using System.Collections.Generic;
using Vastcore.Generation;
using Vastcore.Generation.Map;

namespace Vastcore.Generation
{
    /// <summary>
    /// BiomePresetManagerのテストクラス
    /// プリセット管理機能の動作確認を行う
    /// </summary>
    public class BiomePresetManagerTest : MonoBehaviour
    {
        [Header("テスト設定")]
        public bool runTestsOnStart = false;
        public bool enableDebugLogs = true;
        
        [Header("テスト対象")]
        public BiomePresetManager presetManager;
        
        [Header("テスト結果")]
        public bool allTestsPassed = false;
        public List<string> testResults = new List<string>();
        
        private void Start()
        {
            if (runTestsOnStart)
            {
                RunAllTests();
            }
        }
        
        /// <summary>
        /// すべてのテストを実行
        /// </summary>
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            testResults.Clear();
            bool allPassed = true;
            
            LogTest("=== BiomePresetManager Tests Started ===");
            
            // テスト1: 初期化テスト
            allPassed &= TestInitialization();
            
            // テスト2: プリセット作成・保存テスト
            allPassed &= TestPresetCreationAndSaving();
            
            // テスト3: プリセット読み込みテスト
            allPassed &= TestPresetLoading();
            
            // テスト4: プリセット一覧取得テスト
            allPassed &= TestPresetListing();
            
            // テスト5: プリセット削除テスト
            allPassed &= TestPresetDeletion();
            
            // テスト6: エラーハンドリングテスト
            allPassed &= TestErrorHandling();
            
            // テスト7: プリセット適用テスト
            allPassed &= TestPresetApplication();
            
            allTestsPassed = allPassed;
            LogTest($"=== All Tests {(allPassed ? "PASSED" : "FAILED")} ===");
        }
        
        /// <summary>
        /// テスト1: 初期化テスト
        /// </summary>
        private bool TestInitialization()
        {
            LogTest("Test 1: Initialization");
            
            try
            {
                if (presetManager == null)
                {
                    presetManager = FindObjectOfType<BiomePresetManager>();
                    if (presetManager == null)
                    {
                        var go = new GameObject("BiomePresetManager");
                        presetManager = go.AddComponent<BiomePresetManager>();
                    }
                }
                
                presetManager.Initialize();
                
                LogTest("✓ BiomePresetManager initialized successfully");
                return true;
            }
            catch (System.Exception e)
            {
                LogTest($"✗ Initialization failed: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// テスト2: プリセット作成・保存テスト
        /// </summary>
        private bool TestPresetCreationAndSaving()
        {
            LogTest("Test 2: Preset Creation and Saving");
            
            try
            {
                // テスト用プリセットを作成
                var testPreset = ScriptableObject.CreateInstance<BiomePreset>();
                testPreset.presetName = "TestBiome";
                testPreset.description = "Test biome for unit testing";
                testPreset.InitializeDefault();
                
                // プリセットの妥当性検証
                if (!testPreset.ValidatePreset())
                {
                    LogTest("✗ Test preset validation failed");
                    return false;
                }
                
                // プリセット保存
                bool saveResult = presetManager.SavePreset(testPreset);
                if (!saveResult)
                {
                    LogTest("✗ Preset saving failed");
                    return false;
                }
                
                LogTest("✓ Preset creation and saving successful");
                return true;
            }
            catch (System.Exception e)
            {
                LogTest($"✗ Preset creation and saving failed: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// テスト3: プリセット読み込みテスト
        /// </summary>
        private bool TestPresetLoading()
        {
            LogTest("Test 3: Preset Loading");
            
            try
            {
                // 作成したテストプリセットを読み込み
                var loadedPreset = presetManager.LoadPreset("TestBiome");
                
                if (loadedPreset == null)
                {
                    LogTest("✗ Preset loading returned null");
                    return false;
                }
                
                if (loadedPreset.presetName != "TestBiome")
                {
                    LogTest("✗ Loaded preset has incorrect name");
                    return false;
                }
                
                LogTest("✓ Preset loading successful");
                return true;
            }
            catch (System.Exception e)
            {
                LogTest($"✗ Preset loading failed: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// テスト4: プリセット一覧取得テスト
        /// </summary>
        private bool TestPresetListing()
        {
            LogTest("Test 4: Preset Listing");
            
            try
            {
                // プリセット一覧を更新
                presetManager.RefreshAvailablePresets();
                
                // プリセット名一覧を取得
                var presetNames = presetManager.GetAvailablePresetNames();
                
                if (presetNames == null)
                {
                    LogTest("✗ Preset names list is null");
                    return false;
                }
                
                if (!presetNames.Contains("TestBiome"))
                {
                    LogTest("✗ Test preset not found in list");
                    return false;
                }
                
                LogTest($"✓ Preset listing successful ({presetNames.Count} presets found)");
                return true;
            }
            catch (System.Exception e)
            {
                LogTest($"✗ Preset listing failed: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// テスト5: プリセット削除テスト
        /// </summary>
        private bool TestPresetDeletion()
        {
            LogTest("Test 5: Preset Deletion");
            
            try
            {
                // テストプリセットが存在することを確認
                if (!presetManager.PresetExists("TestBiome"))
                {
                    LogTest("✗ Test preset does not exist before deletion");
                    return false;
                }
                
                // プリセットを削除
                bool deleteResult = presetManager.DeletePreset("TestBiome");
                if (!deleteResult)
                {
                    LogTest("✗ Preset deletion failed");
                    return false;
                }
                
                // 削除されたことを確認
                if (presetManager.PresetExists("TestBiome"))
                {
                    LogTest("✗ Test preset still exists after deletion");
                    return false;
                }
                
                LogTest("✓ Preset deletion successful");
                return true;
            }
            catch (System.Exception e)
            {
                LogTest($"✗ Preset deletion failed: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// テスト6: エラーハンドリングテスト
        /// </summary>
        private bool TestErrorHandling()
        {
            LogTest("Test 6: Error Handling");
            
            try
            {
                // null プリセットの保存テスト
                bool nullSaveResult = presetManager.SavePreset(null);
                if (nullSaveResult)
                {
                    LogTest("✗ Null preset save should have failed");
                    return false;
                }
                
                // 存在しないプリセットの読み込みテスト
                var nonExistentPreset = presetManager.LoadPreset("NonExistentPreset");
                if (nonExistentPreset != null)
                {
                    LogTest("✗ Non-existent preset load should have returned null");
                    return false;
                }
                
                // 無効なプリセット名での削除テスト
                bool invalidDeleteResult = presetManager.DeletePreset("");
                if (invalidDeleteResult)
                {
                    LogTest("✗ Invalid preset name delete should have failed");
                    return false;
                }
                
                LogTest("✓ Error handling tests passed");
                return true;
            }
            catch (System.Exception e)
            {
                LogTest($"✗ Error handling test failed: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// テスト7: プリセット適用テスト
        /// </summary>
        private bool TestPresetApplication()
        {
            LogTest("Test 7: Preset Application");
            
            try
            {
                // テスト用のデフォルトプリセットを作成
                var testPreset = ScriptableObject.CreateInstance<BiomePreset>();
                testPreset.presetName = "ApplicationTest";
                testPreset.InitializeDefault();
                
                // テスト用のTerrainTileを作成
                var testTile = new TerrainTile();
                testTile.coordinate = Vector2Int.zero;
                testTile.terrainObject = new GameObject("TestTerrain");
                testTile.terrainObject.AddComponent<MeshRenderer>();
                
                // プリセットを適用
                presetManager.ApplyPresetToTerrain(testPreset, testTile);
                
                // 適用されたことを確認
                if (testTile.appliedBiome != testPreset)
                {
                    LogTest("✗ Preset was not applied to terrain tile");
                    return false;
                }
                
                // クリーンアップ
                if (testTile.terrainObject != null)
                {
                    DestroyImmediate(testTile.terrainObject);
                }
                
                LogTest("✓ Preset application successful");
                return true;
            }
            catch (System.Exception e)
            {
                LogTest($"✗ Preset application failed: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// テストログを出力
        /// </summary>
        private void LogTest(string message)
        {
            testResults.Add(message);
            
            if (enableDebugLogs)
            {
                Debug.Log($"[BiomePresetManagerTest] {message}");
            }
        }
        
        /// <summary>
        /// テスト結果をクリア
        /// </summary>
        [ContextMenu("Clear Test Results")]
        public void ClearTestResults()
        {
            testResults.Clear();
            allTestsPassed = false;
        }
    }
}