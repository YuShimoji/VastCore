using UnityEditor;
using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Editor
{
    /// <summary>
    /// StructureTagPreset の Custom Inspector。
    /// プロファイル表示と他プリセットとの比較 (BlendScore) を提供する。
    /// </summary>
    [CustomEditor(typeof(StructureTagPreset))]
    public class StructureTagPresetEditor : UnityEditor.Editor
    {
        #region Serialized Properties
        private SerializedProperty m_DisplayName;
        private SerializedProperty m_Description;
        private SerializedProperty m_Profile;
        #endregion

        #region State
        private bool m_ShowComparison = false;
        private StructureTagPreset m_CompareTarget;
        #endregion

        private void OnEnable()
        {
            m_DisplayName = serializedObject.FindProperty("m_DisplayName");
            m_Description = serializedObject.FindProperty("m_Description");
            m_Profile = serializedObject.FindProperty("m_Profile");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // --- Core ---
            EditorGUILayout.LabelField("Preset Info", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_DisplayName);
            EditorGUILayout.PropertyField(m_Description);

            EditorGUILayout.Space(5);

            // --- Profile ---
            EditorGUILayout.LabelField("Tag Profile", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Profile, true);

            EditorGUILayout.Space(5);

            // --- Comparison ---
            m_ShowComparison = EditorGUILayout.Foldout(m_ShowComparison, "Preset Comparison", true);
            if (m_ShowComparison)
            {
                EditorGUI.indentLevel++;
                DrawComparisonSection();
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawComparisonSection()
        {
            m_CompareTarget = (StructureTagPreset)EditorGUILayout.ObjectField(
                "Compare With", m_CompareTarget, typeof(StructureTagPreset), false);

            if (m_CompareTarget == null)
            {
                EditorGUILayout.HelpBox(
                    "別の StructureTagPreset をドラッグして BlendScore を確認できます。",
                    MessageType.Info);
                return;
            }

            if (m_CompareTarget == target)
            {
                EditorGUILayout.HelpBox("自身との比較は常に 1.0 です。", MessageType.Info);
                return;
            }

            var selfPreset = (StructureTagPreset)target;
            var selfProfile = selfPreset.Profile;
            var otherProfile = m_CompareTarget.Profile;

            if (selfProfile == null || otherProfile == null)
            {
                EditorGUILayout.HelpBox("プロファイルが未設定です。", MessageType.Warning);
                return;
            }

            float score = selfProfile.BlendScore(otherProfile);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // BlendScore bar
            EditorGUILayout.LabelField("BlendScore", EditorStyles.miniBoldLabel);
            var rect = EditorGUILayout.GetControlRect(false, 20f);
            EditorGUI.ProgressBar(rect, score, $"{score:F3}");

            // Tag comparison table
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Tag Weights", EditorStyles.miniBoldLabel);

            // Collect all tags from both profiles
            var allTags = new System.Collections.Generic.HashSet<string>(
                System.StringComparer.OrdinalIgnoreCase);
            foreach (var entry in selfProfile.GetAllTags())
            {
                if (!string.IsNullOrEmpty(entry.tagName))
                    allTags.Add(entry.tagName);
            }
            foreach (var entry in otherProfile.GetAllTags())
            {
                if (!string.IsNullOrEmpty(entry.tagName))
                    allTags.Add(entry.tagName);
            }

            // Header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tag", EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField(selfPreset.name, EditorStyles.miniLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField(m_CompareTarget.name, EditorStyles.miniLabel, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            foreach (string tag in allTags)
            {
                float selfWeight = selfProfile.GetWeight(tag);
                float otherWeight = otherProfile.GetWeight(tag);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(tag, EditorStyles.miniLabel, GUILayout.Width(100));
                EditorGUILayout.LabelField($"{selfWeight:F2}", EditorStyles.miniLabel, GUILayout.Width(60));
                EditorGUILayout.LabelField($"{otherWeight:F2}", EditorStyles.miniLabel, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }
    }
}
