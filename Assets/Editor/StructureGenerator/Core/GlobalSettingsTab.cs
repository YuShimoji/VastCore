using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Vastcore.Editor.Generation
{
    /// <summary>
    /// 全てのタブで共有されるグローバル設定を管理するタブ
    /// </summary>
    public class GlobalSettingsTab : IStructureTab
    {
        public TabCategory Category => TabCategory.Settings;
        public string DisplayName => "Global Settings";
        public string Description => "プロジェクト全体の設定を一元管理します。";
        public bool SupportsRealTimeUpdate => true;

        private StructureGeneratorWindow _parent;

        private const string PrefsKeyPrefix = "Vastcore_StructureGenerator_";
        private const string PrefsKeyScale = PrefsKeyPrefix + "GlobalScale";
        private const string PrefsKeySpawnX = PrefsKeyPrefix + "SpawnX";
        private const string PrefsKeySpawnY = PrefsKeyPrefix + "SpawnY";
        private const string PrefsKeySpawnZ = PrefsKeyPrefix + "SpawnZ";
        private const string PrefsKeyMaterialCount = PrefsKeyPrefix + "MaterialCount";
        private const string PrefsKeyMaterialPrefix = PrefsKeyPrefix + "Material_";
        private const string PrefsKeySelectedMaterial = PrefsKeyPrefix + "SelectedMaterialIndex";

        // --- 設定項目 ---
        public List<Material> MaterialPalette { get; private set; } = new List<Material>();
        public int SelectedMaterialIndex { get; set; } = 0;
        public Vector3 DefaultSpawnPosition { get; set; } = Vector3.zero;
        public float GlobalStructureScale { get; set; } = 5f;

        // --- UI用変数 ---
        private Vector2 _scrollPosition;

        private const string k_PrefsPrefix = "VastcoreGlobalSettings_";

        public GlobalSettingsTab(StructureGeneratorWindow parent)
        {
            _parent = parent;
            LoadFromEditorPrefs();
        }

        public void DrawGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField(DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(Description, MessageType.Info);

            DrawMaterialSettings();
            DrawGenerationSettings();

            EditorGUILayout.Space(10);
            DrawSaveLoadSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawMaterialSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("マテリアル設定", EditorStyles.boldLabel);

            // マテリアルパレットのUI
            for (int i = 0; i < MaterialPalette.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                var newMat = (Material)EditorGUILayout.ObjectField($"マテリアル {i+1}", MaterialPalette[i], typeof(Material), false);
                if (newMat != MaterialPalette[i])
                {
                    MaterialPalette[i] = newMat;
                    SaveToEditorPrefs();
                }
                if (GUILayout.Button("削除", GUILayout.Width(50)))
                {
                    MaterialPalette.RemoveAt(i);
                    i--;
                    SaveToEditorPrefs();
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("マテリアルを追加"))
            {
                MaterialPalette.Add(null);
                SaveToEditorPrefs();
            }
        }

        private void DrawGenerationSettings()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("生成設定", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            DefaultSpawnPosition = EditorGUILayout.Vector3Field("デフォルト生成位置", DefaultSpawnPosition);
            GlobalStructureScale = EditorGUILayout.Slider("グローバルスケール", GlobalStructureScale, 0.1f, 50f);
            if (EditorGUI.EndChangeCheck())
            {
                SaveToEditorPrefs();
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("設定を保存"))
            {
                SaveToEditorPrefs();
            }
            if (GUILayout.Button("設定をリセット"))
            {
                DefaultSpawnPosition = Vector3.zero;
                GlobalStructureScale = 5f;
                MaterialPalette.Clear();
                SelectedMaterialIndex = 0;
                SaveToEditorPrefs();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSaveLoadSection()
        {
            EditorGUILayout.LabelField("設定の保存/読込", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("JSON エクスポート", GUILayout.Height(25)))
            {
                ExportToJson();
            }
            if (GUILayout.Button("JSON インポート", GUILayout.Height(25)))
            {
                ImportFromJson();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("設定は自動的に EditorPrefs に保存され、セッション間で永続化されます。", MessageType.None);
        }

        #region Persistence

        private void SaveToEditorPrefs()
        {
            EditorPrefs.SetFloat(PrefsKeyScale, GlobalStructureScale);
            EditorPrefs.SetFloat(PrefsKeySpawnX, DefaultSpawnPosition.x);
            EditorPrefs.SetFloat(PrefsKeySpawnY, DefaultSpawnPosition.y);
            EditorPrefs.SetFloat(PrefsKeySpawnZ, DefaultSpawnPosition.z);
            EditorPrefs.SetInt(PrefsKeySelectedMaterial, SelectedMaterialIndex);

            EditorPrefs.SetInt(PrefsKeyMaterialCount, MaterialPalette.Count);
            for (int i = 0; i < MaterialPalette.Count; i++)
            {
                string path = MaterialPalette[i] != null ? AssetDatabase.GetAssetPath(MaterialPalette[i]) : "";
                EditorPrefs.SetString(PrefsKeyMaterialPrefix + i, path);
            }
        }

        private void LoadFromEditorPrefs()
        {
            if (!EditorPrefs.HasKey(PrefsKeyScale)) return;

            GlobalStructureScale = EditorPrefs.GetFloat(PrefsKeyScale, 5f);
            DefaultSpawnPosition = new Vector3(
                EditorPrefs.GetFloat(PrefsKeySpawnX, 0f),
                EditorPrefs.GetFloat(PrefsKeySpawnY, 0f),
                EditorPrefs.GetFloat(PrefsKeySpawnZ, 0f)
            );
            SelectedMaterialIndex = EditorPrefs.GetInt(PrefsKeySelectedMaterial, 0);

            int matCount = EditorPrefs.GetInt(PrefsKeyMaterialCount, 0);
            MaterialPalette.Clear();
            for (int i = 0; i < matCount; i++)
            {
                string path = EditorPrefs.GetString(PrefsKeyMaterialPrefix + i, "");
                Material mat = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Material>(path);
                MaterialPalette.Add(mat);
            }
        }

        [System.Serializable]
        private class SettingsExportData
        {
            public float globalScale;
            public float spawnX, spawnY, spawnZ;
            public int selectedMaterialIndex;
            public List<string> materialPaths = new List<string>();
        }

        private void ExportToJson()
        {
            string path = EditorUtility.SaveFilePanel("設定をエクスポート", "", "StructureGeneratorSettings", "json");
            if (string.IsNullOrEmpty(path)) return;

            var data = new SettingsExportData
            {
                globalScale = GlobalStructureScale,
                spawnX = DefaultSpawnPosition.x,
                spawnY = DefaultSpawnPosition.y,
                spawnZ = DefaultSpawnPosition.z,
                selectedMaterialIndex = SelectedMaterialIndex
            };
            foreach (var mat in MaterialPalette)
            {
                data.materialPaths.Add(mat != null ? AssetDatabase.GetAssetPath(mat) : "");
            }

            string json = JsonUtility.ToJson(data, true);
            System.IO.File.WriteAllText(path, json);
            Debug.Log($"[GlobalSettings] 設定をエクスポートしました: {path}");
        }

        private void ImportFromJson()
        {
            string path = EditorUtility.OpenFilePanel("設定をインポート", "", "json");
            if (string.IsNullOrEmpty(path)) return;

            string json = System.IO.File.ReadAllText(path);
            var data = JsonUtility.FromJson<SettingsExportData>(json);
            if (data == null)
            {
                Debug.LogError("[GlobalSettings] 設定ファイルの読み込みに失敗しました");
                return;
            }

            GlobalStructureScale = data.globalScale;
            DefaultSpawnPosition = new Vector3(data.spawnX, data.spawnY, data.spawnZ);
            SelectedMaterialIndex = data.selectedMaterialIndex;

            MaterialPalette.Clear();
            foreach (string matPath in data.materialPaths)
            {
                Material mat = string.IsNullOrEmpty(matPath) ? null : AssetDatabase.LoadAssetAtPath<Material>(matPath);
                MaterialPalette.Add(mat);
            }

            SaveToEditorPrefs();
            Debug.Log($"[GlobalSettings] 設定をインポートしました: {path}");
        }

        #endregion

        public void OnSceneGUI() { }
        public void HandleRealTimeUpdate() { }
        public void ProcessSelectedObjects() { }
        public void OnTabSelected() { }
        public void OnTabDeselected() { }
    }
}
