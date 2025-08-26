using UnityEngine;
using UnityEditor;

namespace Vastcore.Generation
{
    /// <summary>
    /// 自然地形特徴テスト用シーンの作成
    /// </summary>
    public class CreateNaturalTerrainTestScene
    {
        [MenuItem("Vastcore/Create Natural Terrain Test Scene")]
        public static void CreateTestScene()
        {
            // 新しいシーンを作成
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, 
                UnityEditor.SceneManagement.NewSceneMode.Single);

            // メインカメラを作成
            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 100, -200);
            camera.transform.LookAt(Vector3.zero);
            cameraObject.tag = "MainCamera";

            // ライトを作成
            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            // テストランナーオブジェクトを作成
            var testRunnerObject = new GameObject("Natural Terrain Test Runner");
            var testRunner = testRunnerObject.AddComponent<NaturalTerrainTestRunner>();
            testRunner.runOnStart = true;
            testRunner.testResolution = 128;
            testRunner.testTileSize = 1000f;
            testRunner.testMaxHeight = 50f;

            // 自然地形特徴コンポーネントを追加
            var naturalFeatures = testRunnerObject.AddComponent<NaturalTerrainFeatures>();
            
            // デフォルト設定
            naturalFeatures.enableRiverGeneration = true;
            naturalFeatures.enableMountainGeneration = true;
            naturalFeatures.enableValleyGeneration = true;
            naturalFeatures.maxRiversPerTile = 2;
            naturalFeatures.maxMountainRanges = 1;
            naturalFeatures.riverWidth = 10f;
            naturalFeatures.riverDepth = 3f;
            naturalFeatures.mountainHeight = 100f;
            naturalFeatures.valleyDepth = 20f;

            Debug.Log("自然地形特徴テストシーンを作成しました");
            Debug.Log("Playボタンを押してテストを実行してください");

            // シーンを保存
            string scenePath = "Assets/Scenes/NaturalTerrainTest.unity";
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, scenePath);
            
            Debug.Log($"シーンを保存しました: {scenePath}");
        }
    }
}