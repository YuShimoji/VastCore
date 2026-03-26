using UnityEditor;
using UnityEngine;
using Vastcore.Generation;

namespace Vastcore.Editor
{
    /// <summary>
    /// StructureTagProfile の PropertyDrawer。
    /// タグ一覧を重みスライダー付きで表示し、タグの追加/削除を提供する。
    /// </summary>
    [CustomPropertyDrawer(typeof(StructureTagProfile))]
    public class StructureTagProfileDrawer : PropertyDrawer
    {
        #region Constants
        private const float k_LineHeight = 20f;
        private const float k_Padding = 2f;
        private const float k_ButtonWidth = 20f;
        private const float k_TagNameWidth = 120f;

        /// <summary>StructureTagAdapter で定義済みの既知タグ</summary>
        private static readonly string[] k_KnownTags = new string[]
        {
            "arch", "tower", "wall", "dome", "column",
            "bridge", "enclosure", "spire", "stepped", "crystal",
            "massive", "ornate", "weathered", "fortified", "sacred",
            "functional", "elegant", "primitive", "organic", "geometric"
        };
        #endregion

        public override float GetPropertyHeight(SerializedProperty _property, GUIContent _label)
        {
            var tagsProp = _property.FindPropertyRelative("m_Tags");
            if (!_property.isExpanded || tagsProp == null)
            {
                return k_LineHeight;
            }

            // Foldout + tags + add button
            return k_LineHeight + (tagsProp.arraySize * (k_LineHeight + k_Padding)) + k_LineHeight + k_Padding;
        }

        public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label)
        {
            var tagsProp = _property.FindPropertyRelative("m_Tags");
            if (tagsProp == null)
            {
                EditorGUI.LabelField(_position, _label, new GUIContent("m_Tags not found"));
                return;
            }

            EditorGUI.BeginProperty(_position, _label, _property);

            // Foldout header with tag count
            var foldoutRect = new Rect(_position.x, _position.y, _position.width, k_LineHeight);
            _property.isExpanded = EditorGUI.Foldout(foldoutRect, _property.isExpanded,
                $"{_label.text} ({tagsProp.arraySize} tags)", true);

            if (_property.isExpanded)
            {
                EditorGUI.indentLevel++;
                float y = _position.y + k_LineHeight;
                int removeIndex = -1;

                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    var entry = tagsProp.GetArrayElementAtIndex(i);
                    var tagNameProp = entry.FindPropertyRelative("tagName");
                    var weightProp = entry.FindPropertyRelative("weight");

                    float x = _position.x + EditorGUI.indentLevel * 15f;
                    float availableWidth = _position.width - EditorGUI.indentLevel * 15f;

                    // Delete button
                    var btnRect = new Rect(x, y, k_ButtonWidth, k_LineHeight);
                    if (GUI.Button(btnRect, "-", EditorStyles.miniButton))
                    {
                        removeIndex = i;
                    }

                    // Tag name
                    var nameRect = new Rect(x + k_ButtonWidth + 2, y, k_TagNameWidth, k_LineHeight);
                    tagNameProp.stringValue = EditorGUI.TextField(nameRect, tagNameProp.stringValue);

                    // Weight slider
                    float sliderX = x + k_ButtonWidth + k_TagNameWidth + 6;
                    float sliderWidth = availableWidth - k_ButtonWidth - k_TagNameWidth - 8;
                    var sliderRect = new Rect(sliderX, y, sliderWidth, k_LineHeight);
                    weightProp.floatValue = EditorGUI.Slider(sliderRect, weightProp.floatValue, 0f, 1f);

                    y += k_LineHeight + k_Padding;
                }

                if (removeIndex >= 0)
                {
                    tagsProp.DeleteArrayElementAtIndex(removeIndex);
                }

                // Add tag dropdown
                float addX = _position.x + EditorGUI.indentLevel * 15f;
                var addRect = new Rect(addX, y, _position.width - EditorGUI.indentLevel * 15f, k_LineHeight);
                DrawAddTagButton(addRect, tagsProp);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private void DrawAddTagButton(Rect _rect, SerializedProperty _tagsProp)
        {
            if (GUI.Button(new Rect(_rect.x, _rect.y, 100, k_LineHeight), "+ Add Tag", EditorStyles.miniButton))
            {
                var menu = new GenericMenu();

                // 既知タグから未使用のものを列挙
                foreach (string tag in k_KnownTags)
                {
                    bool alreadyExists = false;
                    for (int i = 0; i < _tagsProp.arraySize; i++)
                    {
                        string existing = _tagsProp.GetArrayElementAtIndex(i)
                            .FindPropertyRelative("tagName").stringValue;
                        if (string.Equals(existing, tag, System.StringComparison.OrdinalIgnoreCase))
                        {
                            alreadyExists = true;
                            break;
                        }
                    }

                    if (!alreadyExists)
                    {
                        string capturedTag = tag;
                        menu.AddItem(new GUIContent(tag), false, () =>
                        {
                            _tagsProp.serializedObject.Update();
                            int idx = _tagsProp.arraySize;
                            _tagsProp.InsertArrayElementAtIndex(idx);
                            var newEntry = _tagsProp.GetArrayElementAtIndex(idx);
                            newEntry.FindPropertyRelative("tagName").stringValue = capturedTag;
                            newEntry.FindPropertyRelative("weight").floatValue = 0.5f;
                            _tagsProp.serializedObject.ApplyModifiedProperties();
                        });
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent(tag + " (used)"));
                    }
                }

                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Custom..."), false, () =>
                {
                    _tagsProp.serializedObject.Update();
                    int idx = _tagsProp.arraySize;
                    _tagsProp.InsertArrayElementAtIndex(idx);
                    var newEntry = _tagsProp.GetArrayElementAtIndex(idx);
                    newEntry.FindPropertyRelative("tagName").stringValue = "new_tag";
                    newEntry.FindPropertyRelative("weight").floatValue = 0.5f;
                    _tagsProp.serializedObject.ApplyModifiedProperties();
                });

                menu.ShowAsContext();
            }
        }
    }
}
