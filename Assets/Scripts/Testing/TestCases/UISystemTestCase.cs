#if VASTCORE_INTEGRATION_TEST_ENABLED
using UnityEngine;
using System.Collections;
using Vastcore.UI;

namespace Vastcore.Testing
{
    /// <summary>
    /// UIシステムのテストケース
    /// 要求5: モダンUI設計システムの検証
    /// </summary>
    public class UISystemTestCase : ITestCase
    {
        public IEnumerator Execute(VastcoreIntegrationTestManager testManager)
        {
            var logger = testManager.Logger;
            var uiSystem = testManager.UISystem;
            
            logger.Log("Testing UI system...");
            
            if (uiSystem == null)
            {
                logger.LogWarning("SliderBasedUISystem is null, creating test instance");
                uiSystem = CreateTestUISystem();
            }
            
            // 要求5.1: スライドバー中心のインターフェース
            yield return TestSliderBasedInterface(uiSystem, logger);
            
            // 要求5.2: スライドバーによる制御
            yield return TestSliderControl(uiSystem, logger);
            
            // 要求5.3: 統一されたデザイン
            yield return TestUnifiedDesign(uiSystem, logger);
            
            // 要求5.4: モダンで直感的なデザイン
            yield return TestModernDesign(uiSystem, logger);
            
            // 要求5.5: リアルタイム更新
            yield return TestRealtimeUpdate(uiSystem, logger);
            
            logger.Log("UI system test completed");
        }
        
        private IEnumerator TestSliderBasedInterface(SliderBasedUISystem uiSystem, TestLogger logger)
        {
            logger.Log("Testing slider-based interface...");
            
            // スライダーUIの作成テスト
            bool sliderCreated = uiSystem.CreateSliderUI(
                "TestParameter",
                0f, 100f, 50f,
                (value) => { /* テスト用コールバック */ }
            );
            
            if (!sliderCreated)
            {
                throw new System.Exception("Failed to create slider UI");
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // 作成されたスライダーの確認
            var sliderElements = uiSystem.GetActiveSliders();
            if (sliderElements == null || sliderElements.Count == 0)
            {
                throw new System.Exception("No active sliders found after creation");
            }
            
            logger.Log($"✓ Slider-based interface test successful: {sliderElements.Count} sliders active");
        }
        
        private IEnumerator TestSliderControl(SliderBasedUISystem uiSystem, TestLogger logger)
        {
            logger.Log("Testing slider control functionality...");
            
            bool valueChanged = false;
            float receivedValue = 0f;
            
            // 値変更コールバック付きスライダーを作成
            bool sliderCreated = uiSystem.CreateSliderUI(
                "ControlTestParameter",
                0f, 200f, 100f,
                (value) => {
                    valueChanged = true;
                    receivedValue = value;
                }
            );
            
            if (!sliderCreated)
            {
                throw new System.Exception("Failed to create control test slider");
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // スライダー値をプログラムで変更
            bool valueSet = uiSystem.SetSliderValue("ControlTestParameter", 150f);
            if (!valueSet)
            {
                throw new System.Exception("Failed to set slider value programmatically");
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // コールバックが呼ばれたかチェック
            if (!valueChanged)
            {
                throw new System.Exception("Slider value change callback not triggered");
            }
            
            if (Mathf.Abs(receivedValue - 150f) > 0.1f)
            {
                throw new System.Exception($"Slider value mismatch: expected 150, got {receivedValue}");
            }
            
            logger.Log("✓ Slider control test successful");
        }
        
        private IEnumerator TestUnifiedDesign(SliderBasedUISystem uiSystem, TestLogger logger)
        {
            logger.Log("Testing unified design system...");
            
            // 複数のUIエレメントを作成
            string[] testElements = { "Element1", "Element2", "Element3" };
            
            foreach (var elementName in testElements)
            {
                bool created = uiSystem.CreateSliderUI(
                    elementName,
                    0f, 100f, 50f,
                    (value) => { }
                );
                
                if (!created)
                {
                    throw new System.Exception($"Failed to create UI element: {elementName}");
                }
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // デザインの統一性をチェック
            var styleSystem = uiSystem.GetComponent<ModernUIStyleSystem>();
            if (styleSystem == null)
            {
                logger.LogWarning("ModernUIStyleSystem not found, cannot verify design consistency");
            }
            else
            {
                bool designConsistent = styleSystem.ValidateDesignConsistency();
                if (!designConsistent)
                {
                    throw new System.Exception("UI design consistency validation failed");
                }
            }
            
            // カラーテーマの統一性チェック
            var activeSliders = uiSystem.GetActiveSliders();
            if (activeSliders.Count > 1)
            {
                var firstSliderStyle = activeSliders[0].GetCurrentStyle();
                
                foreach (var slider in activeSliders)
                {
                    var sliderStyle = slider.GetCurrentStyle();
                    if (!StylesMatch(firstSliderStyle, sliderStyle))
                    {
                        logger.LogWarning("UI style inconsistency detected between sliders");
                        break;
                    }
                }
            }
            
            logger.Log("✓ Unified design test successful");
        }
        
        private IEnumerator TestModernDesign(SliderBasedUISystem uiSystem, TestLogger logger)
        {
            logger.Log("Testing modern design elements...");
            
            // モダンデザインの有効化
            uiSystem.SetModernDesignEnabled(true);
            yield return new WaitForSeconds(0.5f);
            
            // モダンデザインが適用されているかチェック
            if (!uiSystem.IsModernDesignEnabled())
            {
                throw new System.Exception("Modern design not enabled");
            }
            
            // デザイン要素のチェック
            var designElements = uiSystem.GetDesignElements();
            
            // 必要なデザイン要素が存在するかチェック
            bool hasModernFont = designElements.ContainsKey("ModernFont");
            bool hasColorScheme = designElements.ContainsKey("ColorScheme");
            bool hasAnimations = designElements.ContainsKey("Animations");
            
            if (!hasModernFont)
            {
                logger.LogWarning("Modern font not configured");
            }
            
            if (!hasColorScheme)
            {
                logger.LogWarning("Modern color scheme not configured");
            }
            
            if (!hasAnimations)
            {
                logger.LogWarning("UI animations not configured");
            }
            
            // 直感的なデザインのテスト（アクセシビリティ）
            bool isAccessible = uiSystem.ValidateAccessibility();
            if (!isAccessible)
            {
                logger.LogWarning("UI accessibility validation failed");
            }
            
            logger.Log("✓ Modern design test successful");
        }
        
        private IEnumerator TestRealtimeUpdate(SliderBasedUISystem uiSystem, TestLogger logger)
        {
            logger.Log("Testing realtime update functionality...");
            
            int updateCount = 0;
            float lastUpdateTime = 0f;
            
            // リアルタイム更新テスト用スライダーを作成
            bool sliderCreated = uiSystem.CreateSliderUI(
                "RealtimeTestParameter",
                0f, 100f, 0f,
                (value) => {
                    updateCount++;
                    lastUpdateTime = Time.time;
                }
            );
            
            if (!sliderCreated)
            {
                throw new System.Exception("Failed to create realtime test slider");
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // リアルタイム更新を有効化
            uiSystem.SetRealtimeUpdateEnabled(true);
            
            // 連続的な値変更をシミュレート
            float startTime = Time.time;
            for (int i = 0; i < 10; i++)
            {
                float newValue = i * 10f;
                uiSystem.SetSliderValue("RealtimeTestParameter", newValue);
                yield return new WaitForSeconds(0.1f);
            }
            
            float totalTime = Time.time - startTime;
            
            // 更新頻度をチェック
            if (updateCount < 5) // 最低5回は更新されるべき
            {
                throw new System.Exception($"Insufficient realtime updates: {updateCount} updates in {totalTime}s");
            }
            
            // 更新の即座性をチェック
            float lastUpdateDelay = Time.time - lastUpdateTime;
            if (lastUpdateDelay > 0.2f) // 200ms以内に最後の更新があるべき
            {
                throw new System.Exception($"Realtime update delay too high: {lastUpdateDelay}s");
            }
            
            logger.Log($"✓ Realtime update test successful: {updateCount} updates in {totalTime:F2}s");
        }
        
        private SliderBasedUISystem CreateTestUISystem()
        {
            var testUIObject = new GameObject("TestUISystem");
            var uiSystem = testUIObject.AddComponent<SliderBasedUISystem>();
            
            // 基本的な設定
            uiSystem.SetModernDesignEnabled(true);
            uiSystem.SetRealtimeUpdateEnabled(true);
            
            return uiSystem;
        }
        
        private bool StylesMatch(object style1, object style2)
        {
            // UIStyle is not yet implemented, using generic object comparison
            if (style1 == null || style2 == null)
                return false;
            
            // TODO: Implement proper UIStyle comparison when UIStyle class is available
            return style1.GetType() == style2.GetType();
        }
    }
}
#endif