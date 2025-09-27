#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vastcore.EditorTools
{
    public class MissingScriptRepairWindow : EditorWindow
    {
        private const string ReportPath = "Documentation/QA/MISSING_SCRIPTS_REPORT.md";

        [Serializable]
        private class MissingEntry
        {
            public string assetPath;
            public string objectPath;
            public int missingCount;
        }

        private readonly List<MissingEntry> _results = new List<MissingEntry>();
        private bool _scanPrefabs = true;
        private bool _scanScenes = true;
        private bool _autoExportReport = true;
        private bool _autoFixRemoveMissing = false;

        [MenuItem("Vastcore/Tools/Missing Script Repair")] 
        public static void Open()
        {
            var win = GetWindow<MissingScriptRepairWindow>("Missing Script Repair");
            win.minSize = new Vector2(640, 420);
            win.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Missing Script 修復ツール", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope("box"))
            {
                _scanPrefabs = EditorGUILayout.ToggleLeft("Prefabs をスキャン (t:Prefab)", _scanPrefabs);
                _scanScenes = EditorGUILayout.ToggleLeft("Scenes をスキャン (t:Scene)", _scanScenes);
                _autoFixRemoveMissing = EditorGUILayout.ToggleLeft("スキャン後に Missing Script を自動削除 (リスク: コンポーネントが取り外されます)", _autoFixRemoveMissing);
                _autoExportReport = EditorGUILayout.ToggleLeft($"スキャン結果をレポート出力 ({ReportPath})", _autoExportReport);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("スキャン実行", GUILayout.Height(32)))
            {
                RunScan();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("検出結果", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (_results.Count == 0)
                {
                    EditorGUILayout.HelpBox("結果はまだありません。スキャンを実行してください。", MessageType.Info);
                }
                else
                {
                    var totalMissing = 0;
                    foreach (var r in _results) totalMissing += r.missingCount;
                    EditorGUILayout.LabelField($"エントリ数: {_results.Count}, 累計 Missing Script 数: {totalMissing}");
                    EditorGUILayout.Space(4);
                    var scroll = new Vector2(0, 0);
                    scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(200));
                    foreach (var r in _results)
                    {
                        EditorGUILayout.LabelField($"{r.assetPath} :: {r.objectPath}  (missing: {r.missingCount})");
                    }
                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.Space(4);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Missing Script を削除 (一括)", GUILayout.Height(28)))
                        {
                            if (EditorUtility.DisplayDialog("確認", "すべての対象 Prefab/Scene から Missing Script を削除します。よろしいですか？", "はい", "いいえ"))
                            {
                                RemoveAllMissingScripts();
                                EditorUtility.DisplayDialog("完了", "Missing Script の削除が完了しました。", "OK");
                            }
                        }
                        if (GUILayout.Button("レポートを出力", GUILayout.Height(28)))
                        {
                            ExportReport();
                            EditorUtility.RevealInFinder(Path.GetFullPath(ReportPath));
                        }
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "注意: Missing Script の削除は元に戻せません。実行前にバージョン管理へコミットしておくことを推奨します。\n" +
                "将来的に 1:1 の自動置換（旧→新コンポーネント）にも対応予定です。現状は検出・削除・レポートの範囲を提供します。",
                MessageType.Warning);
        }

        private void RunScan()
        {
            _results.Clear();
            try
            {
                if (_scanPrefabs) ScanPrefabs();
                if (_scanScenes) ScanScenes();

                if (_autoExportReport) ExportReport();
                if (_autoFixRemoveMissing && _results.Count > 0)
                {
                    if (EditorUtility.DisplayDialog("確認", "スキャン結果に基づき Missing Script を削除します。よろしいですか？", "はい", "いいえ"))
                    {
                        RemoveAllMissingScripts();
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        private void ScanPrefabs()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab");
            for (int i = 0; i < guids.Length; i++)
            {
                var guid = guids[i];
                var path = AssetDatabase.GUIDToAssetPath(guid);
                EditorUtility.DisplayProgressBar("Prefab スキャン", path, (float)i / Math.Max(1, guids.Length));

                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    FindMissingInHierarchy(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private void ScanScenes()
        {
            var guids = AssetDatabase.FindAssets("t:Scene");
            var currentActiveScene = SceneManager.GetActiveScene();

            for (int i = 0; i < guids.Length; i++)
            {
                var guid = guids[i];
                var path = AssetDatabase.GUIDToAssetPath(guid);
                EditorUtility.DisplayProgressBar("Scene スキャン", path, (float)i / Math.Max(1, guids.Length));

                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        FindMissingInHierarchy(root, path);
                    }
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            // アクティブシーンは変更しない方針
            if (currentActiveScene.IsValid())
            {
                SceneManager.SetActiveScene(currentActiveScene);
            }
        }

        private void FindMissingInHierarchy(GameObject root, string assetPath)
        {
            var stack = new Stack<Transform>();
            stack.Push(root.transform);

            while (stack.Count > 0)
            {
                var t = stack.Pop();
                foreach (Transform child in t) stack.Push(child);

                var comps = t.gameObject.GetComponents<Component>();
                int missing = 0;
                foreach (var c in comps)
                {
                    if (c == null) missing++;
                }

                if (missing > 0)
                {
                    _results.Add(new MissingEntry
                    {
                        assetPath = assetPath,
                        objectPath = GetHierarchyPath(t.gameObject),
                        missingCount = missing
                    });
                }
            }
        }

        private static string GetHierarchyPath(GameObject go)
        {
            var path = go.name;
            var tr = go.transform;
            while (tr.parent != null)
            {
                tr = tr.parent;
                path = tr.name + "/" + path;
            }
            return path;
        }

        private void RemoveAllMissingScripts()
        {
            // Prefabs
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                EditorUtility.DisplayProgressBar("Prefab 修復中", path, (float)i / Math.Max(1, prefabGuids.Length));
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    RemoveMissingRecursive(root.transform);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            // Scenes
            var sceneGuids = AssetDatabase.FindAssets("t:Scene");
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                EditorUtility.DisplayProgressBar("Scene 修復中", path, (float)i / Math.Max(1, sceneGuids.Length));
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        RemoveMissingRecursive(root.transform);
                    }
                    EditorSceneManager.SaveScene(scene);
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static int RemoveMissingRecursive(Transform tr)
        {
            int removed = 0;
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(tr.gameObject);
            foreach (Transform child in tr)
            {
                removed += RemoveMissingRecursive(child);
            }
            return removed;
        }

        private void ExportReport()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "Documentation/QA");
            var sb = new StringBuilder();
            sb.AppendLine("# Missing Scripts Report");
            sb.AppendLine();
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            if (_results.Count == 0)
            {
                sb.AppendLine("検出結果はありません（Missing Script 0）。");
            }
            else
            {
                int total = 0;
                foreach (var r in _results) total += r.missingCount;
                sb.AppendLine($"エントリ数: {_results.Count}");
                sb.AppendLine($"累計 Missing Script 数: {total}");
                sb.AppendLine();
                sb.AppendLine("| Asset Path | Object Path | Missing Count |");
                sb.AppendLine("|---|---:|---:|");
                foreach (var r in _results)
                {
                    sb.AppendLine($"| {r.assetPath} | {r.objectPath} | {r.missingCount} |");
                }
            }
            File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[MissingScriptRepair] レポートを出力しました: {ReportPath}");
        }
    }
}
#endif
