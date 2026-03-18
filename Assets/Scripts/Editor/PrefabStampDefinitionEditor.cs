using UnityEditor;
using UnityEngine;
using Vastcore.Terrain.DualGrid;

namespace Vastcore.Editor
{
    /// <summary>
    /// PrefabStampDefinition の Custom Inspector。
    /// V1 変異パラメータの視覚的な設定と、設定内容のプレビューを提供する。
    /// </summary>
    [CustomEditor(typeof(PrefabStampDefinition))]
    public class PrefabStampDefinitionEditor : UnityEditor.Editor
    {
        #region Serialized Properties
        private SerializedProperty m_Prefab;
        private SerializedProperty m_DisplayName;
        private SerializedProperty m_RotationMode;
        private SerializedProperty m_HeightRule;
        private SerializedProperty m_ScaleRange;
        private SerializedProperty m_FootprintOffsets;
        private SerializedProperty m_PositionJitter;
        private SerializedProperty m_MaterialVariants;
        private SerializedProperty m_ChildToggleGroups;
        #endregion

        #region State
        private bool m_ShowVariationSection = true;
        private bool m_ShowFootprintSection = false;
        private bool m_ShowPreview = false;
        private int m_PreviewSeed = 42;
        #endregion

        private void OnEnable()
        {
            m_Prefab = serializedObject.FindProperty("m_Prefab");
            m_DisplayName = serializedObject.FindProperty("m_DisplayName");
            m_RotationMode = serializedObject.FindProperty("m_RotationMode");
            m_HeightRule = serializedObject.FindProperty("m_HeightRule");
            m_ScaleRange = serializedObject.FindProperty("m_ScaleRange");
            m_FootprintOffsets = serializedObject.FindProperty("m_FootprintOffsets");
            m_PositionJitter = serializedObject.FindProperty("m_PositionJitter");
            m_MaterialVariants = serializedObject.FindProperty("m_MaterialVariants");
            m_ChildToggleGroups = serializedObject.FindProperty("m_ChildToggleGroups");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // --- Core ---
            EditorGUILayout.LabelField("Core", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Prefab);
            EditorGUILayout.PropertyField(m_DisplayName);

            EditorGUILayout.Space(5);

            // --- Placement Rules ---
            EditorGUILayout.LabelField("Placement Rules", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_RotationMode);
            EditorGUILayout.PropertyField(m_HeightRule);
            EditorGUILayout.PropertyField(m_ScaleRange);

            // --- Footprint ---
            m_ShowFootprintSection = EditorGUILayout.Foldout(m_ShowFootprintSection, "Footprint");
            if (m_ShowFootprintSection)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_FootprintOffsets, true);
                var def = (PrefabStampDefinition)target;
                EditorGUILayout.LabelField(
                    def.IsSingleCell ? "単一セル" : $"マルチセル ({def.FootprintOffsets.Length} offsets)",
                    EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // --- V1 Variation ---
            m_ShowVariationSection = EditorGUILayout.Foldout(m_ShowVariationSection, "Variation (V1)");
            if (m_ShowVariationSection)
            {
                EditorGUI.indentLevel++;
                DrawVariationSection();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // --- Preview ---
            m_ShowPreview = EditorGUILayout.Foldout(m_ShowPreview, "Variation Preview");
            if (m_ShowPreview)
            {
                EditorGUI.indentLevel++;
                DrawPreviewSection();
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawVariationSection()
        {
            EditorGUILayout.PropertyField(m_PositionJitter,
                new GUIContent("Position Jitter", "XZ平面での位置ずれ半径（ワールド単位）"));
            EditorGUILayout.PropertyField(m_MaterialVariants,
                new GUIContent("Material Variants", "ランダム選択されるマテリアル候補"), true);
            EditorGUILayout.PropertyField(m_ChildToggleGroups,
                new GUIContent("Child Toggle Groups", "表示/非表示を切り替える子オブジェクト名"), true);

            // 注意表示
            if (m_ChildToggleGroups.arraySize > 0 && m_Prefab.objectReferenceValue != null)
            {
                var prefab = (GameObject)m_Prefab.objectReferenceValue;
                int matchCount = 0;
                for (int i = 0; i < m_ChildToggleGroups.arraySize; i++)
                {
                    string name = m_ChildToggleGroups.GetArrayElementAtIndex(i).stringValue;
                    if (prefab.transform.Find(name) != null)
                    {
                        matchCount++;
                    }
                }

                if (matchCount < m_ChildToggleGroups.arraySize)
                {
                    EditorGUILayout.HelpBox(
                        $"Prefab内で {m_ChildToggleGroups.arraySize - matchCount} 件の子オブジェクトが見つかりません。" +
                        "名前を確認してください。",
                        MessageType.Warning);
                }
            }
        }

        private void DrawPreviewSection()
        {
            m_PreviewSeed = EditorGUILayout.IntField("Preview Seed", m_PreviewSeed);

            var def = (PrefabStampDefinition)target;
            var rng = new System.Random(m_PreviewSeed);

            // 3サンプルを生成して表示
            EditorGUILayout.LabelField("3 Sample Instances:", EditorStyles.miniLabel);

            for (int i = 0; i < 3; i++)
            {
                float scale = def.GetRandomScale(rng);
                float rotation = def.GetRandomRotation(rng);
                Vector3 offset = def.GetRandomPositionOffset(rng);
                Material mat = def.GetRandomMaterial(rng);
                int toggleIdx = def.GetRandomChildToggleIndex(rng);

                string matName = mat != null ? mat.name : "(default)";
                string toggleName = toggleIdx >= 0 && def.ChildToggleGroups.Length > toggleIdx
                    ? def.ChildToggleGroups[toggleIdx]
                    : "(none)";

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Sample {i + 1}", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField(
                    $"  Scale: {scale:F2}  Rot: {rotation:F0}°  Offset: ({offset.x:F2}, {offset.z:F2})",
                    EditorStyles.miniLabel);
                EditorGUILayout.LabelField(
                    $"  Material: {matName}  Toggle: {toggleName}",
                    EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
            }
        }
    }
}
