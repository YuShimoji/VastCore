using UnityEditor;
using UnityEngine;
using Vastcore.Terrain.DualGrid;
using Vastcore.Terrain.Erosion;
using Vastcore.Game.Managers;

namespace Vastcore.Editor
{
    /// <summary>
    /// Phase C の各機能を検証するためのシーン要素を一括セットアップする Editor ユーティリティ。
    /// メニュー: Vastcore > Verification > Setup Phase C Scene
    /// </summary>
    public static class PhaseCVerificationSetup
    {
        [MenuItem("Vastcore/Verification/Setup Phase C Scene")]
        public static void SetupScene()
        {
            Undo.SetCurrentGroupName("Setup Phase C Verification Scene");

            SetupDualGridVisualizer();
            SetupErosionPreview();
            SetupGameManager();
            SetupLighting();

            Debug.Log("[PhaseCVerification] Scene setup complete. Select objects in Hierarchy to inspect.");
            EditorUtility.DisplayDialog(
                "Phase C Verification Scene",
                "セットアップ完了:\n\n" +
                "1. DualGrid_Visualizer — SG-1/SG-2 スタンプ検証\n" +
                "   Inspector で PrefabStampDefinition をアサインし CellIDs を指定\n\n" +
                "2. ErosionPreview — PC-4 エロージョン視覚化\n" +
                "   Play モードで地形メッシュが生成される\n\n" +
                "3. GameManager — PC-5 起動シーケンス検証\n" +
                "   PlayerPrefab をアサインして Play\n\n" +
                "PC-3 (Arch/Pyramid): メニュー Window > Vastcore > Structure Generator\n" +
                "PC-2 (Blend): Structure Generator > Composition タブ",
                "OK");
        }

        private static void SetupDualGridVisualizer()
        {
            if (Object.FindFirstObjectByType<GridDebugVisualizer>() != null)
            {
                Debug.Log("[PhaseCVerification] GridDebugVisualizer already exists, skipping.");
                return;
            }

            var go = new GameObject("DualGrid_Visualizer");
            var viz = go.AddComponent<GridDebugVisualizer>();
            Undo.RegisterCreatedObjectUndo(go, "Create DualGrid Visualizer");

            // Inspector で手動設定が必要な項目:
            // - m_TestStampDefinition: PrefabStampDefinition アセット
            // - m_TestStampCellIds: 配置先セルID
            Debug.Log("[PhaseCVerification] Created DualGrid_Visualizer. " +
                      "Assign a PrefabStampDefinition in Inspector to test stamps.");
        }

        private static void SetupErosionPreview()
        {
            if (Object.FindFirstObjectByType<ErosionPreview>() != null)
            {
                Debug.Log("[PhaseCVerification] ErosionPreview already exists, skipping.");
                return;
            }

            var go = new GameObject("ErosionPreview");
            go.transform.position = new Vector3(30f, 0f, 0f); // DualGrid の横に配置
            var preview = go.AddComponent<ErosionPreview>();
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
            Undo.RegisterCreatedObjectUndo(go, "Create Erosion Preview");

            Debug.Log("[PhaseCVerification] Created ErosionPreview at (30,0,0). " +
                      "Enter Play mode to see eroded terrain mesh.");
        }

        private static void SetupGameManager()
        {
            if (Object.FindFirstObjectByType<VastcoreGameManager>() != null)
            {
                Debug.Log("[PhaseCVerification] VastcoreGameManager already exists, skipping.");
                return;
            }

            var go = new GameObject("VastcoreGameManager");
            go.AddComponent<VastcoreGameManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create GameManager");

            Debug.Log("[PhaseCVerification] Created VastcoreGameManager. " +
                      "Assign PlayerPrefab in Inspector for PC-5 startup test.");
        }

        private static void SetupLighting()
        {
            // Directional Light がなければ作成
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            bool hasDirectional = false;
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    hasDirectional = true;
                    break;
                }
            }

            if (!hasDirectional)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                Undo.RegisterCreatedObjectUndo(lightGo, "Create Directional Light");
            }

            // Camera がなければ作成
            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.transform.position = new Vector3(0f, 10f, -15f);
                cam.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
                Undo.RegisterCreatedObjectUndo(camGo, "Create Main Camera");
            }
        }
    }
}
