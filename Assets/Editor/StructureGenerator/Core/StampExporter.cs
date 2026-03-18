using UnityEngine;
using UnityEditor;
using Vastcore.Terrain.DualGrid;

namespace Vastcore.Editor.Generation
{
    /// <summary>
    /// StructureGenerator で生成した構造物を Prefab + PrefabStampDefinition に変換する。
    /// パイプライン貫通: StructureGenerator → Prefab → StampDefinition → DualGrid 配置
    /// </summary>
    public static class StampExporter
    {
        #region Constants

        private const string k_DefaultStampFolder = "Assets/Resources/Stamps";
        private const string k_PrefabSubfolder = "Prefabs";
        private const string k_DefinitionSubfolder = "Definitions";

        #endregion

        #region Public Methods

        /// <summary>
        /// 選択中の GameObject を Prefab 化し、PrefabStampDefinition を自動生成する
        /// </summary>
        /// <param name="_target">エクスポート対象の GameObject</param>
        /// <param name="_positionJitter">V1: XZ位置ジッター半径（0で無効）</param>
        /// <returns>生成された PrefabStampDefinition（失敗時 null）</returns>
        public static PrefabStampDefinition ExportAsStamp(GameObject _target, float _positionJitter = 0f)
        {
            if (_target == null)
            {
                Debug.LogError("[StampExporter] エクスポート対象が null です");
                return null;
            }

            // MeshFilter の存在確認
            var meshFilter = _target.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                Debug.LogError($"[StampExporter] '{_target.name}' に有効な MeshFilter がありません");
                return null;
            }

            // フォルダ準備
            EnsureFolder(k_DefaultStampFolder);
            EnsureFolder($"{k_DefaultStampFolder}/{k_PrefabSubfolder}");
            EnsureFolder($"{k_DefaultStampFolder}/{k_DefinitionSubfolder}");

            string safeName = SanitizeName(_target.name);

            // 1. Prefab 保存
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{k_DefaultStampFolder}/{k_PrefabSubfolder}/{safeName}.prefab");

            // 位置をリセットしたコピーを作成
            GameObject tempCopy = Object.Instantiate(_target);
            tempCopy.name = safeName;
            tempCopy.transform.position = Vector3.zero;
            tempCopy.transform.rotation = Quaternion.identity;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(tempCopy, prefabPath);
            Object.DestroyImmediate(tempCopy);

            if (prefab == null)
            {
                Debug.LogError($"[StampExporter] Prefab の保存に失敗: {prefabPath}");
                return null;
            }

            // 2. PrefabStampDefinition 生成
            string defPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{k_DefaultStampFolder}/{k_DefinitionSubfolder}/{safeName}_StampDef.asset");

            var definition = ScriptableObject.CreateInstance<PrefabStampDefinition>();

            // private フィールドを SerializedObject 経由で設定
            var serializedDef = new SerializedObject(definition);
            serializedDef.FindProperty("m_Prefab").objectReferenceValue = prefab;
            serializedDef.FindProperty("m_DisplayName").stringValue = safeName;
            serializedDef.FindProperty("m_RotationMode").enumValueIndex = (int)StampRotationMode.Step90;
            serializedDef.FindProperty("m_HeightRule").enumValueIndex = (int)StampHeightRule.TopOfStack;
            serializedDef.FindProperty("m_ScaleRange").vector2Value = new Vector2(0.8f, 1.2f);

            // V1 Variation: PositionJitter を設定
            if (_positionJitter > 0f)
            {
                serializedDef.FindProperty("m_PositionJitter").floatValue = _positionJitter;
            }

            // V1 Variation: 子オブジェクト名を自動検出して ChildToggleGroups に設定
            string[] childNames = DetectChildToggleCandidates(prefab);
            if (childNames.Length > 0)
            {
                var toggleProp = serializedDef.FindProperty("m_ChildToggleGroups");
                toggleProp.arraySize = childNames.Length;
                for (int i = 0; i < childNames.Length; i++)
                {
                    toggleProp.GetArrayElementAtIndex(i).stringValue = childNames[i];
                }
                Debug.Log($"[StampExporter] ChildToggleGroups 自動検出: {string.Join(", ", childNames)}");
            }

            serializedDef.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(definition, defPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[StampExporter] エクスポート完了: Prefab={prefabPath}, Definition={defPath}");

            // Inspector で選択
            Selection.activeObject = definition;
            EditorGUIUtility.PingObject(definition);

            return definition;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Assets 配下のフォルダが存在しない場合に作成する
        /// </summary>
        private static void EnsureFolder(string _path)
        {
            if (AssetDatabase.IsValidFolder(_path)) return;

            string parent = System.IO.Path.GetDirectoryName(_path).Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(_path);

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        /// <summary>
        /// Prefab の直接子オブジェクトから ChildToggleGroups 候補を検出する。
        /// 2つ以上の直接子がある場合にのみ候補として返す（1つでは切替の意味がない）。
        /// </summary>
        private static string[] DetectChildToggleCandidates(GameObject _prefab)
        {
            if (_prefab == null) return System.Array.Empty<string>();

            Transform root = _prefab.transform;
            if (root.childCount < 2) return System.Array.Empty<string>();

            var names = new System.Collections.Generic.List<string>();
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                // MeshRenderer を持つ子のみ候補 (装飾パーツの差替用途)
                if (child.GetComponent<MeshRenderer>() != null ||
                    child.GetComponent<MeshFilter>() != null)
                {
                    names.Add(child.name);
                }
            }

            return names.Count >= 2 ? names.ToArray() : System.Array.Empty<string>();
        }

        /// <summary>
        /// ファイル名として安全な文字列に変換
        /// </summary>
        private static string SanitizeName(string _name)
        {
            char[] invalid = System.IO.Path.GetInvalidFileNameChars();
            string result = _name;
            foreach (char c in invalid)
            {
                result = result.Replace(c, '_');
            }
            return result;
        }

        #endregion
    }
}
