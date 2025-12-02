// This file is disabled until Vastcore.Deform namespace is implemented (Phase 3)
#if VASTCORE_DEFORM_ENABLED
using UnityEngine;
using UnityEditor;
using Vastcore.Deform;
using Vastcore.Generation;

namespace Vastcore.Editor
{
    /// <summary>
    /// 変形ブラシツール
    /// シーン内でクリックして地形オブジェクトに変形を適用
    /// </summary>
    [InitializeOnLoad]
    public class DeformationBrushTool : EditorWindow
    {
        private static bool isBrushActive = false;
        private static float brushSize = 5f;
        private static float brushStrength = 0.5f;
        private static float brushFalloff = 1f;
        private static DeformationPreset brushPreset;

        // ツール設定
        private static bool showBrushSettings = true;
        private static bool useTerrainSpecific = true;

        static DeformationBrushTool()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        [MenuItem("Vastcore/Deformation Brush Tool")]
        static void Init()
        {
            DeformationBrushTool window = (DeformationBrushTool)GetWindow(typeof(DeformationBrushTool));
            window.titleContent = new GUIContent("Deformation Brush");
            window.Show();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Deformation Brush Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // ブラシツール有効化
            bool newActive = EditorGUILayout.Toggle("Brush Active", isBrushActive);
            if (newActive != isBrushActive)
            {
                isBrushActive = newActive;
                SceneView.RepaintAll();
            }

            if (isBrushActive)
            {
                EditorGUILayout.Space();

                // ブラシ設定
                showBrushSettings = EditorGUILayout.Foldout(showBrushSettings, "Brush Settings");
                if (showBrushSettings)
                {
                    EditorGUI.indentLevel++;

                    brushSize = EditorGUILayout.Slider("Brush Size", brushSize, 0.1f, 50f);
                    brushStrength = EditorGUILayout.Slider("Brush Strength", brushStrength, 0.01f, 2f);
                    brushFalloff = EditorGUILayout.Slider("Brush Falloff", brushFalloff, 0.1f, 5f);

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();

                // プリセット設定
                useTerrainSpecific = EditorGUILayout.Toggle("Use Terrain Specific", useTerrainSpecific);
                if (!useTerrainSpecific)
                {
                    brushPreset = (DeformationPreset)EditorGUILayout.ObjectField(
                        "Deformation Preset", brushPreset, typeof(DeformationPreset), false);
                }

                EditorGUILayout.Space();

                // ヘルプテキスト
                EditorGUILayout.HelpBox(
                    "Click on terrain objects in the Scene view to apply deformation.\n" +
                    "Hold Shift to erase deformations.\n" +
                    "Use mouse wheel to adjust brush size.",
                    MessageType.Info);
            }
        }

        static void OnSceneGUI(SceneView sceneView)
        {
            if (!isBrushActive) return;

            // マウス位置を取得
            Vector2 mousePos = Event.current.mousePosition;
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);

            // 地面との交差を計算
            Plane groundPlane = new Plane(Vector3.up, 0f);
            float enter;
            if (groundPlane.Raycast(ray, out enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);

                // ブラシサイズの可視化
                Handles.color = Color.yellow;
                Handles.DrawWireDisc(hitPoint, Vector3.up, brushSize);

                // ブラシ強度の可視化
                Handles.color = new Color(1f, 1f, 0f, 0.3f);
                Handles.DrawSolidDisc(hitPoint, Vector3.up, brushSize * brushFalloff);

                // ラベル
                Handles.Label(hitPoint + Vector3.up * 0.5f,
                    $"Size: {brushSize:F1}\nStrength: {brushStrength:F2}");

                // マウスホイールでブラシサイズ調整
                if (Event.current.type == EventType.ScrollWheel)
                {
                    brushSize = Mathf.Clamp(brushSize - Event.current.delta.y * 0.5f, 0.1f, 50f);
                    Event.current.Use();
                    SceneView.RepaintAll();
                }

                // クリック処理
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    ApplyBrushDeformation(hitPoint, Event.current.shift);
                    Event.current.Use();
                }
            }
        }

        static void ApplyBrushDeformation(Vector3 center, bool erase)
        {
            // 範囲内の地形オブジェクトを検索
            Collider[] colliders = Physics.OverlapSphere(center, brushSize);
            foreach (var collider in colliders)
            {
                PrimitiveTerrainObject terrainObj = collider.GetComponent<PrimitiveTerrainObject>();
                if (terrainObj != null && terrainObj.enableDeform)
                {
                    float distance = Vector3.Distance(collider.transform.position, center);
                    float falloffFactor = Mathf.Clamp01(1f - (distance / (brushSize * brushFalloff)));

                    if (falloffFactor > 0.01f)
                    {
                        if (erase)
                        {
                            // 消去モード
                            terrainObj.ClearAllDeformers();
                        }
                        else
                        {
                            // 適用モード
                            if (useTerrainSpecific)
                            {
                                terrainObj.ApplyTerrainSpecificDeformation();
                            }
                            else if (brushPreset != null)
                            {
                                terrainObj.ApplyDeformationPreset(brushPreset);
                            }
                            else
                            {
                                // デフォルトノイズ変形
                                terrainObj.ApplyNoiseDeformation(brushStrength * falloffFactor, 1f);
                            }
                        }
                    }
                }
            }
        }

        void OnDestroy()
        {
            isBrushActive = false;
            SceneView.RepaintAll();
        }
    }
}
#endif
