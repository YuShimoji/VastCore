using UnityEngine;
using System.Collections;

namespace Vastcore.Testing
{
    /// <summary>
    /// プレイヤーインタラクションシステムのテストケース
    /// 要求4: 独特プレイヤー操作システムの検証
    /// </summary>
    public class PlayerInteractionTestCase : ITestCase
    {
        public IEnumerator Execute(VastcoreIntegrationTestManager testManager)
        {
            var logger = testManager.Logger;
            var testPlayer = testManager.TestPlayer;
            
            logger.Log("Testing player interaction system...");
            
            // プレイヤーコントローラーを取得または作成
            var playerController = testPlayer.GetComponent<AdvancedPlayerController>();
            if (playerController == null)
            {
                playerController = testPlayer.gameObject.AddComponent<AdvancedPlayerController>();
                yield return new WaitForSeconds(0.5f); // 初期化待機
            }
            
            // 要求4.1: グラインド操作システム
            yield return TestGrindSystem(playerController, logger);
            
            // 要求4.2: 球体ワープシステム
            yield return TestTranslocationSystem(playerController, logger);
            
            // 要求4.3: 特殊移動システム
            yield return TestSpecialMovement(playerController, logger);
            
            // 要求4.4: 物理的慣性と爽快感
            yield return TestPhysicsAndResponsiveness(playerController, logger);
            
            logger.Log("Player interaction system test completed");
        }
        
        private IEnumerator TestGrindSystem(AdvancedPlayerController playerController, TestLogger logger)
        {
            logger.Log("Testing grind system...");
            
            // グラインドシステムの取得
            var grindSystem = playerController.GetComponent<EnhancedGrindSystem>();
            if (grindSystem == null)
            {
                grindSystem = playerController.gameObject.AddComponent<EnhancedGrindSystem>();
                yield return new WaitForSeconds(0.5f);
            }
            
            // テスト用のグラインド可能オブジェクトを作成
            var grindObject = CreateTestGrindObject();
            
            try
            {
                // プレイヤーをグラインドオブジェクトの近くに配置
                playerController.transform.position = grindObject.transform.position + Vector3.up * 2f;
                yield return new WaitForSeconds(0.5f);
                
                // グラインド開始をシミュレート
                bool grindStarted = grindSystem.TryStartGrind(grindObject);
                
                if (!grindStarted)
                {
                    throw new System.Exception("Failed to start grind");
                }
                
                // グラインド中の動作をテスト
                yield return new WaitForSeconds(1f);
                
                bool isGrinding = grindSystem.IsGrinding();
                if (!isGrinding)
                {
                    throw new System.Exception("Grind system not maintaining grind state");
                }
                
                // グラインド終了
                grindSystem.EndGrind();
                yield return new WaitForSeconds(0.5f);
                
                if (grindSystem.IsGrinding())
                {
                    throw new System.Exception("Grind system failed to end grind");
                }
                
                logger.Log("✓ Grind system test successful");
            }
            finally
            {
                // クリーンアップ
                if (grindObject != null)
                {
                    Object.DestroyImmediate(grindObject);
                }
            }
        }
        
        private IEnumerator TestTranslocationSystem(AdvancedPlayerController playerController, TestLogger logger)
        {
            logger.Log("Testing translocation system...");
            
            // トランスロケーションシステムの取得
            var translocSystem = playerController.GetComponent<EnhancedTranslocationSystem>();
            if (translocSystem == null)
            {
                translocSystem = playerController.gameObject.AddComponent<EnhancedTranslocationSystem>();
                yield return new WaitForSeconds(0.5f);
            }
            
            Vector3 originalPosition = playerController.transform.position;
            Vector3 targetPosition = originalPosition + Vector3.forward * 50f;
            
            // 軌道予測のテスト
            bool predictionActive = translocSystem.StartTrajectoryPrediction(targetPosition);
            if (!predictionActive)
            {
                throw new System.Exception("Failed to start trajectory prediction");
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // 軌道データの取得
            var trajectoryData = translocSystem.GetTrajectoryData();
            if (trajectoryData == null || trajectoryData.Length == 0)
            {
                throw new System.Exception("No trajectory data generated");
            }
            
            // 着地プレビューのテスト
            bool previewActive = translocSystem.IsLandingPreviewActive();
            if (!previewActive)
            {
                logger.LogWarning("Landing preview not active");
            }
            
            // ワープ実行のテスト
            bool warpExecuted = translocSystem.ExecuteTranslocation(targetPosition);
            if (!warpExecuted)
            {
                throw new System.Exception("Failed to execute translocation");
            }
            
            yield return new WaitForSeconds(1f);
            
            // 位置の確認
            float distance = Vector3.Distance(playerController.transform.position, targetPosition);
            if (distance > 5f) // 5m以内の誤差は許容
            {
                throw new System.Exception($"Translocation accuracy too low: {distance}m from target");
            }
            
            // 元の位置に戻す
            playerController.transform.position = originalPosition;
            
            logger.Log("✓ Translocation system test successful");
        }
        
        private IEnumerator TestSpecialMovement(AdvancedPlayerController playerController, TestLogger logger)
        {
            logger.Log("Testing special movement system...");
            
            Vector3 originalPosition = playerController.transform.position;
            
            // 浮遊感のテスト（重力調整）
            var rigidbody = playerController.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = playerController.gameObject.AddComponent<Rigidbody>();
            }
            
            float originalGravity = rigidbody.mass;
            
            // 特殊移動モードを有効化
            playerController.SetSpecialMovementMode(true);
            yield return new WaitForSeconds(0.5f);
            
            // 浮遊感が適用されているかチェック
            if (Mathf.Abs(rigidbody.mass - originalGravity) < 0.1f)
            {
                logger.LogWarning("Special movement may not be affecting physics properties");
            }
            
            // 独特な操作レスポンスのテスト
            Vector3 inputDirection = Vector3.forward;
            playerController.ApplyMovementInput(inputDirection);
            yield return new WaitForSeconds(1f);
            
            // 移動が発生しているかチェック
            float movementDistance = Vector3.Distance(originalPosition, playerController.transform.position);
            if (movementDistance < 1f)
            {
                throw new System.Exception("Special movement not responding to input");
            }
            
            // 特殊移動モードを無効化
            playerController.SetSpecialMovementMode(false);
            playerController.transform.position = originalPosition;
            
            logger.Log("✓ Special movement system test successful");
        }
        
        private IEnumerator TestPhysicsAndResponsiveness(AdvancedPlayerController playerController, TestLogger logger)
        {
            logger.Log("Testing physics and responsiveness...");
            
            var rigidbody = playerController.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                throw new System.Exception("Rigidbody component required for physics test");
            }
            
            Vector3 originalPosition = playerController.transform.position;
            
            // 慣性テスト
            Vector3 forceDirection = Vector3.forward;
            float forceAmount = 1000f;
            
            rigidbody.AddForce(forceDirection * forceAmount);
            yield return new WaitForSeconds(0.5f);
            
            // 慣性による移動をチェック
            float inertialMovement = Vector3.Distance(originalPosition, playerController.transform.position);
            if (inertialMovement < 1f)
            {
                throw new System.Exception("Physics inertia not working properly");
            }
            
            // レスポンシブネステスト
            float responseStartTime = Time.time;
            playerController.ApplyMovementInput(Vector3.right);
            
            // 入力に対する応答時間を測定
            Vector3 positionBeforeInput = playerController.transform.position;
            yield return new WaitForSeconds(0.1f);
            Vector3 positionAfterInput = playerController.transform.position;
            
            float responseTime = Time.time - responseStartTime;
            float responseDistance = Vector3.Distance(positionBeforeInput, positionAfterInput);
            
            if (responseTime > 0.2f || responseDistance < 0.1f)
            {
                logger.LogWarning($"Movement responsiveness may be suboptimal: {responseTime}s, {responseDistance}m");
            }
            
            // 爽快感の指標（速度変化の滑らかさ）
            float velocityMagnitude = rigidbody.velocity.magnitude;
            if (velocityMagnitude < 1f)
            {
                logger.LogWarning("Movement velocity may be too low for satisfying gameplay");
            }
            
            // 元の状態に戻す
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            playerController.transform.position = originalPosition;
            
            logger.Log("✓ Physics and responsiveness test successful");
        }
        
        private GameObject CreateTestGrindObject()
        {
            var grindObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grindObject.name = "TestGrindObject";
            grindObject.transform.localScale = new Vector3(10f, 1f, 1f);
            grindObject.transform.position = Vector3.zero;
            
            // グラインド可能タグを追加
            grindObject.tag = "Grindable";
            
            // グラインド用のコライダー設定
            var collider = grindObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = false;
            }
            
            return grindObject;
        }
    }
}