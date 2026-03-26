using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Editor
{
    /// <summary>
    /// AdjacencyRuleSet の Custom Inspector。
    /// タグ間の親和度をマトリクス形式で表示・編集する。
    /// </summary>
    [CustomEditor(typeof(AdjacencyRuleSet))]
    public class AdjacencyRuleSetEditor : UnityEditor.Editor
    {
        #region Constants
        private const float k_CellSize = 36f;
        private const float k_LabelWidth = 80f;
        private const float k_DefaultAffinity = 0.5f;
        #endregion

        #region State
        private SerializedProperty m_RulesProp;
        private bool m_ShowMatrix = true;
        private bool m_ShowRawList = false;
        private Vector2 m_ScrollPos;
        #endregion

        private void OnEnable()
        {
            m_RulesProp = serializedObject.FindProperty("m_Rules");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var ruleSet = (AdjacencyRuleSet)target;

            // --- Summary ---
            EditorGUILayout.LabelField("Adjacency Rule Set", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Rules: {m_RulesProp.arraySize}", EditorStyles.miniLabel);

            EditorGUILayout.Space(5);

            // --- Matrix View ---
            m_ShowMatrix = EditorGUILayout.Foldout(m_ShowMatrix, "Matrix View", true);
            if (m_ShowMatrix)
            {
                EditorGUI.indentLevel++;
                DrawMatrixView(ruleSet);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // --- Raw List ---
            m_ShowRawList = EditorGUILayout.Foldout(m_ShowRawList, "Raw Rules", true);
            if (m_ShowRawList)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_RulesProp, true);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMatrixView(AdjacencyRuleSet _ruleSet)
        {
            // Collect all unique tags
            var tags = new List<string>();
            var tagSet = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < m_RulesProp.arraySize; i++)
            {
                var rule = m_RulesProp.GetArrayElementAtIndex(i);
                string tagA = rule.FindPropertyRelative("tagA").stringValue;
                string tagB = rule.FindPropertyRelative("tagB").stringValue;

                if (!string.IsNullOrEmpty(tagA) && tagSet.Add(tagA))
                {
                    tags.Add(tagA);
                }
                if (!string.IsNullOrEmpty(tagB) && tagSet.Add(tagB))
                {
                    tags.Add(tagB);
                }
            }

            tags.Sort();

            if (tags.Count == 0)
            {
                EditorGUILayout.HelpBox("ルールがありません。Raw Rules セクションから追加してください。",
                    MessageType.Info);
                return;
            }

            int n = tags.Count;
            float matrixWidth = k_LabelWidth + n * k_CellSize + 20;
            float matrixHeight = k_LabelWidth + n * k_CellSize + 20;

            m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos,
                GUILayout.MaxHeight(Mathf.Min(matrixHeight, 400f)));

            // Header row
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(k_LabelWidth);
            for (int col = 0; col < n; col++)
            {
                var headerStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 8
                };
                GUILayout.Label(TruncateTag(tags[col]), headerStyle, GUILayout.Width(k_CellSize));
            }
            EditorGUILayout.EndHorizontal();

            // Matrix rows
            for (int row = 0; row < n; row++)
            {
                EditorGUILayout.BeginHorizontal();

                // Row label
                GUILayout.Label(TruncateTag(tags[row]), EditorStyles.miniLabel,
                    GUILayout.Width(k_LabelWidth));

                for (int col = 0; col < n; col++)
                {
                    if (col <= row)
                    {
                        // Diagonal and below: show affinity
                        float affinity = _ruleSet.GetAffinity(tags[row], tags[col]);

                        // Color-coded cell
                        Color cellColor;
                        if (row == col)
                        {
                            cellColor = new Color(0.7f, 0.7f, 0.7f, 0.3f);
                        }
                        else if (affinity >= 0.7f)
                        {
                            cellColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
                        }
                        else if (affinity <= 0.3f)
                        {
                            cellColor = new Color(0.8f, 0.2f, 0.2f, 0.3f);
                        }
                        else
                        {
                            cellColor = new Color(0.8f, 0.8f, 0.2f, 0.3f);
                        }

                        var cellRect = GUILayoutUtility.GetRect(k_CellSize, k_CellSize - 2);
                        EditorGUI.DrawRect(cellRect, cellColor);

                        var labelStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 9
                        };

                        if (row == col)
                        {
                            EditorGUI.LabelField(cellRect, "-", labelStyle);
                        }
                        else
                        {
                            EditorGUI.LabelField(cellRect, $"{affinity:F1}", labelStyle);
                        }
                    }
                    else
                    {
                        // Above diagonal: mirror
                        GUILayout.Space(k_CellSize);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            // Legend
            EditorGUILayout.BeginHorizontal();
            DrawLegendBox(new Color(0.2f, 0.8f, 0.2f, 0.3f), "0.7+ (high)");
            DrawLegendBox(new Color(0.8f, 0.8f, 0.2f, 0.3f), "0.3-0.7 (mid)");
            DrawLegendBox(new Color(0.8f, 0.2f, 0.2f, 0.3f), "0-0.3 (low)");
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"Default: {k_DefaultAffinity:F1}", EditorStyles.miniLabel,
                GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawLegendBox(Color _color, string _label)
        {
            var rect = GUILayoutUtility.GetRect(12, 12);
            EditorGUI.DrawRect(rect, _color);
            EditorGUILayout.LabelField(_label, EditorStyles.miniLabel, GUILayout.Width(80));
        }

        private static string TruncateTag(string _tag)
        {
            if (_tag.Length <= 8)
            {
                return _tag;
            }
            return _tag.Substring(0, 7) + "..";
        }
    }
}
