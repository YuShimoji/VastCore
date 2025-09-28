#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Vastcore.EditorTools
{
    public static class TargetedMissingScriptFixes
    {
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string DemoScenePath = "Assets/Scenes/VastcoreDemoScene.unity";
        private const string AutoFlagPath = "Documentation/QA/AUTO_FIX_CORE_SCENES.flag";

        [MenuItem("Vastcore/Tools/Missing Script Repair/Apply Targeted Fixes for Core Scenes")]
        public static void ApplyTargetedFixesMenu()
        {
            ApplyMainSceneFixes();
            ApplyDemoSceneFixes();
            EditorUtility.DisplayDialog("完了", "コアシーン向けのターゲット修復を適用しました。", "OK");
        }

        [InitializeOnLoadMethod]
        private static void AutoRunIfFlagPresent()
        {
            try
            {
                var fullFlag = Path.GetFullPath(AutoFlagPath);
                if (File.Exists(fullFlag))
                {
                    Debug.Log("[TargetedMissingScriptFixes] Auto flag detected. Applying core scene targeted fixes...");
                    ApplyMainSceneFixes();
                    ApplyDemoSceneFixes();
                    File.Delete(fullFlag);
                    AssetDatabase.Refresh();
                    Debug.Log("[TargetedMissingScriptFixes] Auto fixes completed and flag removed.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[TargetedMissingScriptFixes] Auto-run failed: {e.Message}");
            }
        }

        public static void ApplyMainSceneFixes()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);
            try
            {
                var player = FindRoot(scene, "Player");
                var mainCamera = FindRoot(scene, "Main Camera");
                var mapProvider = FindRoot(scene, "MapProvider");

                if (player != null)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(player);
                    AddOrGetComponent(player, "Vastcore.Player.AdvancedPlayerController, Vastcore.Player");
                }

                if (mainCamera != null)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(mainCamera);
                    var camComp = AddOrGetComponent(mainCamera, "Vastcore.Camera.Controllers.CameraController, Vastcore.Camera");
                    if (player != null && camComp != null)
                    {
                        var field = camComp.GetType().GetField("playerBody");
                        if (field != null) field.SetValue(camComp, player.transform);
                    }
                }

                if (mapProvider != null)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(mapProvider);
                    AddOrGetComponent(mapProvider, "Vastcore.Generation.MapProvider, Vastcore.Generation");
                }

                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static void ApplyDemoSceneFixes()
        {
            var scene = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Additive);
            try
            {
                var managerGo = FindRoot(scene, "VastcoreGameManager");
                if (managerGo != null)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(managerGo);
                    AddOrGetComponent(managerGo, "Vastcore.Game.Managers.VastcoreGameManager, Vastcore.Game");
                }
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static GameObject FindRoot(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (var go in scene.GetRootGameObjects())
            {
                if (go.name == name) return go;
            }
            return null;
        }

        private static Component AddOrGetComponent(GameObject go, string qualifiedTypeName)
        {
            var t = Type.GetType(qualifiedTypeName);
            if (t == null)
            {
                Debug.LogWarning($"[TargetedMissingScriptFixes] Type not found: {qualifiedTypeName}");
                return null;
            }
            var c = go.GetComponent(t);
            if (c == null) c = go.AddComponent(t);
            return c;
        }
    }
}
#endif
