using UnityEngine;
using UnityEditor;
using UnityEngine.ProBuilder;
using System.Collections.Generic;
using Vastcore.Generation;
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
        private const string k_MeshSubfolder = "Meshes";

        #endregion

        #region Public Methods

        /// <summary>
        /// 選択中の GameObject を Prefab 化し、PrefabStampDefinition を自動生成する
        /// </summary>
        /// <param name="_target">エクスポート対象の GameObject</param>
        /// <param name="_positionJitter">V1: XZ位置ジッター半径（0で無効）</param>
        /// <param name="_tagProfile">タグプロファイル（nullの場合はタグなし）</param>
        /// <returns>生成された PrefabStampDefinition（失敗時 null）</returns>
        public static PrefabStampDefinition ExportAsStamp(GameObject _target, float _positionJitter = 0f,
            StructureTagProfile _tagProfile = null)
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
            EnsureFolder($"{k_DefaultStampFolder}/{k_MeshSubfolder}");

            string safeName = SanitizeName(_target.name);

            // 1. Prefab 保存
            string prefabPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{k_DefaultStampFolder}/{k_PrefabSubfolder}/{safeName}.prefab");

            // 位置をリセットしたコピーを作成
            GameObject tempCopy = Object.Instantiate(_target);
            GameObject prefab;
            try
            {
                tempCopy.name = safeName;
                tempCopy.transform.position = Vector3.zero;
                tempCopy.transform.rotation = Quaternion.identity;
                PreparePersistentMeshes(_target, tempCopy, safeName);
                prefab = PrefabUtility.SaveAsPrefabAsset(tempCopy, prefabPath);
            }
            finally
            {
                Object.DestroyImmediate(tempCopy);
            }

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

            // タグプロファイルを設定
            if (_tagProfile != null && !_tagProfile.IsEmpty)
            {
                ApplyTagProfile(serializedDef, _tagProfile);
                Debug.Log($"[StampExporter] タグプロファイル設定: {_tagProfile}");
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

        private static void PreparePersistentMeshes(GameObject sourceObject, GameObject copy, string safeName)
        {
            // A cloned ProBuilder component initially shares the source mesh. Its
            // OnDestroy must not destroy that mesh, including on an export failure.
            var builders = copy.GetComponentsInChildren<ProBuilderMesh>(true);
            foreach (var builder in builders) builder.preserveMeshAssetOnDestroy = true;
            foreach (var builder in builders) Object.DestroyImmediate(builder);

            // Export a regular mesh prefab; the editable ProBuilder source stays
            // in the scene. Transient meshes need their own persistent assets.
            var savedMeshes = new Dictionary<Mesh, Mesh>();
            Mesh PersistMesh(Mesh source)
            {
                if (source == null || AssetDatabase.Contains(source)) return source;
                if (!savedMeshes.TryGetValue(source, out var saved))
                {
                    saved = Object.Instantiate(source);
                    saved.name = $"{safeName}_Mesh{savedMeshes.Count}";
                    saved.hideFlags = HideFlags.None;
                    string meshPath = AssetDatabase.GenerateUniqueAssetPath(
                        $"{k_DefaultStampFolder}/{k_MeshSubfolder}/{saved.name}.asset");
                    AssetDatabase.CreateAsset(saved, meshPath);
                    savedMeshes.Add(source, saved);
                }
                return saved;
            }

            // Read the original references: ProBuilder can rebuild the clone's
            // mesh during OnEnable, even when the original mesh is already an asset.
            var sourceFilters = sourceObject.GetComponentsInChildren<MeshFilter>(true);
            var copyFilters = copy.GetComponentsInChildren<MeshFilter>(true);
            var sourceColliders = sourceObject.GetComponentsInChildren<MeshCollider>(true);
            var copyColliders = copy.GetComponentsInChildren<MeshCollider>(true);
            if (sourceFilters.Length != copyFilters.Length || sourceColliders.Length != copyColliders.Length)
                throw new System.InvalidOperationException("Stamp export copy changed its mesh component hierarchy.");
            for (int i = 0; i < copyFilters.Length; i++)
                copyFilters[i].sharedMesh = PersistMesh(sourceFilters[i].sharedMesh);
            for (int i = 0; i < copyColliders.Length; i++)
                copyColliders[i].sharedMesh = PersistMesh(sourceColliders[i].sharedMesh);
        }

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
        /// StructureTagProfile を SerializedObject 経由で PrefabStampDefinition に適用する
        /// </summary>
        private static void ApplyTagProfile(SerializedObject _serializedDef, StructureTagProfile _profile)
        {
            var tagProfileProp = _serializedDef.FindProperty("m_TagProfile");
            if (tagProfileProp == null) return;

            var tagsProp = tagProfileProp.FindPropertyRelative("m_Tags");
            if (tagsProp == null) return;

            var allTags = _profile.GetAllTags();
            tagsProp.arraySize = allTags.Count;
            for (int i = 0; i < allTags.Count; i++)
            {
                var element = tagsProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("tagName").stringValue = allTags[i].tagName;
                element.FindPropertyRelative("weight").floatValue = allTags[i].weight;
            }
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
