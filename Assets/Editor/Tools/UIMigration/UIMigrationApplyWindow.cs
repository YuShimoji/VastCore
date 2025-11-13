// UIMigrationApplyWindow.cs
// Editor-only window for staged application of UI migration rules (A3).
// Reads JSON rules (same format as A1/A2) and applies changes to C# files with preview and backup.
// Scope can be Limited (selected assets) or a folder under Assets/.
// This tool does NOT move files and preserves GUIDs.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Vastcore.EditorTools.UIMigration
{
    public class UIMigrationApplyWindow : EditorWindow
    {
        [MenuItem("Vastcore/Tools/UI Migration/Apply (A3 - staged)")]
        public static void Open()
        {
            var wnd = GetWindow<UIMigrationApplyWindow>("UI Migration Apply (A3)");
            wnd.minSize = new Vector2(720, 460);
            wnd.Focus();
        }

        [Serializable]
        private class RuleSet
        {
            public string version;
            public List<NamespaceMapping> namespaceMappings = new List<NamespaceMapping>();
            public List<ClassMapping> classMappings = new List<ClassMapping>();
            public MenuNameReplace menuNameRule;
        }

        [Serializable]
        private class NamespaceMapping { public string from; public string to; }
        [Serializable]
        private class ClassMapping { public string fromQualified; public string toQualified; public string note; }
        [Serializable]
        private class MenuNameReplace { public string replaceRegexFrom; public string replaceTo; }

        private Vector2 _scroll;
        private string _rulesJsonPath = "docs/ui-migration/ui_mapping_rules.template.json"; // relative to project root
        private string _reportPath = "docs/04_reports/A3-2_UI_MIGRATION_APPLY_REPORT.md";     // relative to project root
        private string _scopeFolder = "Assets"; // apply to this folder (or below)
        private bool _useSelectionOnly = true;  // limit to current selection when true
        private bool _excludeExamples = true;
        private bool _applyNamespaces = true;
        private bool _applyClassMappings = true;
        private bool _applyCreateAssetMenu = true;
        private bool _backup = true; // write .bak file before modify

        private static string ProjectRoot
        {
            get
            {
                var parent = Directory.GetParent(Application.dataPath);
                var root = parent != null ? parent.FullName : Application.dataPath;
                return root.Replace("\\", "/");
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("UI Migration Apply (A3)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Staged application of JSON rules to C# files. Supports preview and backup. Scenes/Prefabs are not modified in this step.", MessageType.Info);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                _excludeExamples = EditorGUILayout.ToggleLeft("Exclude example content (TextMesh Pro Examples)", _excludeExamples);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Rules JSON Path (relative to project root)");
                using (new EditorGUILayout.HorizontalScope())
                {
                    _rulesJsonPath = EditorGUILayout.TextField(_rulesJsonPath);
                    if (GUILayout.Button("Browse", GUILayout.Width(80)))
                    {
                        var absJ = Path.Combine(ProjectRoot, _rulesJsonPath);
                        var dirJ = Path.GetDirectoryName(absJ) ?? ProjectRoot;
                        var pickedJ = EditorUtility.OpenFilePanel("Select Rules JSON", dirJ, "json");
                        if (!string.IsNullOrEmpty(pickedJ))
                        {
                            if (pickedJ.StartsWith(ProjectRoot))
                                _rulesJsonPath = pickedJ.Substring(ProjectRoot.Length + 1).Replace("\\", "/");
                            else
                                EditorUtility.DisplayDialog("Warning", "Please select a file inside the current project.", "OK");
                        }
                    }
                }

                EditorGUILayout.LabelField("Preview Report Output (relative to project root)");
                using (new EditorGUILayout.HorizontalScope())
                {
                    _reportPath = EditorGUILayout.TextField(_reportPath);
                    if (GUILayout.Button("Browse", GUILayout.Width(80)))
                    {
                        var abs = Path.Combine(ProjectRoot, _reportPath);
                        var dir = Path.GetDirectoryName(abs) ?? ProjectRoot;
                        var picked = EditorUtility.SaveFilePanel("Select Preview Report Output", dir, "UI_MIGRATION_APPLY_PREVIEW", "md");
                        if (!string.IsNullOrEmpty(picked))
                        {
                            if (picked.StartsWith(ProjectRoot))
                                _reportPath = picked.Substring(ProjectRoot.Length + 1).Replace("\\", "/");
                            else
                                EditorUtility.DisplayDialog("Warning", "Please select a path inside the current project.", "OK");
                        }
                    }
                }

                EditorGUILayout.Space(4);
                _useSelectionOnly = EditorGUILayout.ToggleLeft("Limit to Project Selection (C# under selection)", _useSelectionOnly);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginDisabledGroup(_useSelectionOnly);
                    EditorGUILayout.LabelField("Scope Folder (under Assets/)", GUILayout.Width(180));
                    _scopeFolder = EditorGUILayout.TextField(_scopeFolder);
                    if (GUILayout.Button("Pick", GUILayout.Width(60)))
                    {
                        var picked = EditorUtility.OpenFolderPanel("Select Scope Folder (under Assets)", Application.dataPath, "");
                        if (!string.IsNullOrEmpty(picked) && picked.Replace("\\", "/").StartsWith(Application.dataPath.Replace("\\", "/")))
                        {
                            var rel = "Assets" + picked.Replace("\\", "/").Substring(Application.dataPath.Replace("\\", "/").Length);
                            _scopeFolder = string.IsNullOrEmpty(rel) ? "Assets" : rel;
                        }
                        else if (!string.IsNullOrEmpty(picked))
                        {
                            EditorUtility.DisplayDialog("Warning", "Please select a folder inside Assets/", "OK");
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Actions");
                _applyNamespaces = EditorGUILayout.ToggleLeft("Apply Namespace Mappings", _applyNamespaces);
                _applyClassMappings = EditorGUILayout.ToggleLeft("Apply Class Mappings", _applyClassMappings);
                _applyCreateAssetMenu = EditorGUILayout.ToggleLeft("Apply CreateAssetMenu.menuName Rule", _applyCreateAssetMenu);
                _backup = EditorGUILayout.ToggleLeft("Create .bak before modifying file", _backup);

                EditorGUILayout.Space(8);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Preview Changes", GUILayout.Height(28)))
                    {
                        TryPreview();
                    }
                    if (GUILayout.Button("Apply Changes (Staged)", GUILayout.Height(28)))
                    {
                        if (EditorUtility.DisplayDialog("Confirm Apply", "Apply selected rules to target C# files? A .bak will be created if enabled.", "Apply", "Cancel"))
                        {
                            TryApply();
                        }
                    }
                }
            }

            EditorGUILayout.Space();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.HelpBox("A3 staged apply: Start with a limited scope (selection or small folder), run preview, then apply. Validate compile and scene behavior.", MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private RuleSet LoadRuleSetFromJson(string absPath)
        {
            if (!File.Exists(absPath)) return null;
            var json = File.ReadAllText(absPath);
            var rs = new RuleSet();
            try
            {
                var mVer = Regex.Match(json, "\"version\"\\s*:\\s*\"([^\"]+)\"");
                if (mVer.Success) rs.version = mVer.Groups[1].Value;

                var mNs = Regex.Match(json, "\"namespaceMappings\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
                if (mNs.Success)
                {
                    var body = mNs.Groups[1].Value;
                    var itemRx = new Regex("\\{\\s*\"from\"\\s*:\\s*\"([^\"]+)\"\\s*,\\s*\"to\"\\s*:\\s*\"([^\"]+)\"\\s*\\}");
                    foreach (Match m in itemRx.Matches(body))
                        rs.namespaceMappings.Add(new NamespaceMapping { from = m.Groups[1].Value, to = m.Groups[2].Value });
                }

                var mCls = Regex.Match(json, "\"classMappings\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
                if (mCls.Success)
                {
                    var body = mCls.Groups[1].Value;
                    var itemRx = new Regex("\\{[^}]*?\"fromQualified\"\\s*:\\s*\"([^\"]+)\"[^}]*?\"toQualified\"\\s*:\\s*\"([^\"]+)\"([^}]*)\\}");
                    foreach (Match m in itemRx.Matches(body))
                    {
                        var note = Regex.Match(m.Groups[3].Value, "\"note\"\\s*:\\s*\"([^\"]*)\"");
                        rs.classMappings.Add(new ClassMapping { fromQualified = m.Groups[1].Value, toQualified = m.Groups[2].Value, note = note.Success ? note.Groups[1].Value : null });
                    }
                }

                var mAttr = Regex.Match(json, "\"attributeRules\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline);
                if (mAttr.Success)
                {
                    var body = mAttr.Groups[1].Value;
                    var mMenu = Regex.Match(body, "\\{[^}]*?\"attribute\"\\s*:\\s*\"UnityEngine\\.CreateAssetMenuAttribute\"[^}]*?\"menuName\"\\s*:\\s*\\{[^}]*?\"replaceRegexFrom\"\\s*:\\s*\"([^\"]+)\"[^}]*?\"replaceTo\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.Singleline);
                    if (mMenu.Success)
                    {
                        rs.menuNameRule = new MenuNameReplace { replaceRegexFrom = mMenu.Groups[1].Value, replaceTo = mMenu.Groups[2].Value };
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[UI Migration Apply] Failed to parse rules JSON heuristically: " + ex.Message);
            }
            return rs;
        }

        private IEnumerable<string> EnumerateTargetCsFiles()
        {
            var dataPath = Application.dataPath.Replace("\\", "/");
            bool Excluded(string p)
            {
                if (!_excludeExamples) return false;
                var path = p.Replace("\\", "/");
                return path.Contains("Assets/TextMesh Pro/Examples & Extras/");
            }

            if (_useSelectionOnly)
            {
                var guids = Selection.assetGUIDs ?? Array.Empty<string>();
                foreach (var g in guids)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(g);
                    if (string.IsNullOrEmpty(assetPath)) continue;
                    if (Excluded(assetPath)) continue;
                    if (assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                        yield return CombineToFull(assetPath);

                    // If folder, enumerate under
                    if (Directory.Exists(CombineToFull(assetPath)))
                    {
                        foreach (var full in Directory.EnumerateFiles(CombineToFull(assetPath), "*.cs", SearchOption.AllDirectories))
                        {
                            var ap = ToAssetPath(full);
                            if (!Excluded(ap)) yield return full;
                        }
                    }
                }
                yield break;
            }

            // Scope folder path
            var scope = _scopeFolder.Replace("\\", "/");
            if (!scope.StartsWith("Assets")) scope = "Assets";
            var fullScope = CombineToFull(scope);
            if (!Directory.Exists(fullScope)) fullScope = dataPath;
            foreach (var full in Directory.EnumerateFiles(fullScope, "*.cs", SearchOption.AllDirectories))
            {
                var ap = ToAssetPath(full);
                if (Excluded(ap)) continue;
                yield return full;
            }
        }

        private void TryPreview()
        {
            try
            {
                var absJson = Path.Combine(ProjectRoot, _rulesJsonPath).Replace("\\", "/");
                var rules = LoadRuleSetFromJson(absJson);
                if (rules == null)
                {
                    EditorUtility.DisplayDialog("Rules", "Failed to load rules JSON. Check path and format.", "OK");
                    return;
                }
                var result = BuildPreview(rules);
                WritePreviewReport(result);
                EditorUtility.RevealInFinder(Path.Combine(ProjectRoot, _reportPath));
                Debug.Log("[UI Migration Apply] Preview generated.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[UI Migration Apply] Preview failed: " + ex.Message);
                EditorUtility.DisplayDialog("Error", "Preview failed. See Console for details.", "OK");
            }
        }

        private void TryApply()
        {
            try
            {
                var absJson = Path.Combine(ProjectRoot, _rulesJsonPath).Replace("\\", "/");
                var rules = LoadRuleSetFromJson(absJson);
                if (rules == null)
                {
                    EditorUtility.DisplayDialog("Rules", "Failed to load rules JSON. Check path and format.", "OK");
                    return;
                }

                int changed = 0, scanned = 0;
                foreach (var full in EnumerateTargetCsFiles().Distinct())
                {
                    scanned++;
                    string txt;
                    try { txt = File.ReadAllText(full); } catch { continue; }
                    var newTxt = ApplyAll(txt, rules);
                    if (!string.Equals(txt, newTxt, StringComparison.Ordinal))
                    {
                        if (_backup)
                        {
                            var bak = full + ".bak";
                            if (!File.Exists(bak)) File.WriteAllText(bak, txt, new UTF8Encoding(false));
                        }
                        File.WriteAllText(full, newTxt, new UTF8Encoding(false));
                        changed++;
                    }
                }

                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Apply Completed", $"Scanned: {scanned}\nChanged: {changed}", "OK");
                Debug.Log($"[UI Migration Apply] Apply finished. Scanned={scanned}, Changed={changed}");
            }
            catch (Exception ex)
            {
                Debug.LogError("[UI Migration Apply] Apply failed: " + ex.Message);
                EditorUtility.DisplayDialog("Error", "Apply failed. See Console for details.", "OK");
            }
        }

        private class PreviewItem
        {
            public string assetPath;
            public List<string> changes = new List<string>(); // human-readable summaries
        }

        private List<PreviewItem> BuildPreview(RuleSet rules)
        {
            var list = new List<PreviewItem>();
            foreach (var full in EnumerateTargetCsFiles().Distinct())
            {
                string txt;
                try { txt = File.ReadAllText(full); } catch { continue; }
                var ap = ToAssetPath(full);
                var changes = DescribeChanges(txt, rules);
                if (changes.Count > 0)
                {
                    list.Add(new PreviewItem { assetPath = ap, changes = changes });
                }
            }
            return list;
        }

        private void WritePreviewReport(List<PreviewItem> items)
        {
            var abs = Path.Combine(ProjectRoot, _reportPath).Replace("\\", "/");
            var dir = Path.GetDirectoryName(abs);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var sb = new StringBuilder();
            sb.AppendLine("# UI Migration Apply Preview (A3)");
            sb.AppendLine();
            sb.AppendLine($"- Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"- Items: {items.Count}");
            sb.AppendLine();
            foreach (var it in items.OrderBy(i => i.assetPath))
            {
                sb.AppendLine($"## {it.assetPath}");
                foreach (var c in it.changes.Take(50)) sb.AppendLine("- " + c);
                if (it.changes.Count > 50) sb.AppendLine($"- (+ {it.changes.Count - 50} more)");
                sb.AppendLine();
            }
            File.WriteAllText(abs, sb.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();
        }

        private List<string> DescribeChanges(string txt, RuleSet rules)
        {
            var desc = new List<string>();

            if (_applyNamespaces)
            {
                foreach (var nm in rules.namespaceMappings)
                {
                    if (string.IsNullOrEmpty(nm.from) || string.IsNullOrEmpty(nm.to)) continue;
                    if (txt.Contains(nm.from + ".") ||
                        Regex.IsMatch(txt, @"^\s*using\s+" + Regex.Escape(nm.from) + @"\s*;", RegexOptions.Multiline) ||
                        Regex.IsMatch(txt, @"\bnamespace\s+" + Regex.Escape(nm.from) + @"(\b|\s|\{)", RegexOptions.Multiline))
                    {
                        desc.Add($"Namespace: {nm.from} -> {nm.to}");
                    }
                }
            }

            if (_applyClassMappings)
            {
                foreach (var cm in rules.classMappings)
                {
                    if (!string.IsNullOrEmpty(cm.fromQualified) && txt.Contains(cm.fromQualified))
                        desc.Add($"Class: {cm.fromQualified} -> {cm.toQualified}");
                }
            }

            if (_applyCreateAssetMenu && rules.menuNameRule != null)
            {
                var m = Regex.Match(txt, "menuName\\s*=\\s*\"([^\"]+)\"", RegexOptions.Multiline);
                if (m.Success)
                {
                    try
                    {
                        var cur = m.Groups[1].Value;
                        var rep = Regex.Replace(cur, rules.menuNameRule.replaceRegexFrom, rules.menuNameRule.replaceTo);
                        if (!string.Equals(cur, rep)) desc.Add($"CreateAssetMenu.menuName: '{cur}' -> '{rep}'");
                    }
                    catch { }
                }
            }

            return desc;
        }

        private string ApplyAll(string txt, RuleSet rules)
        {
            var newTxt = txt;

            if (_applyNamespaces)
            {
                foreach (var nm in rules.namespaceMappings)
                {
                    if (string.IsNullOrEmpty(nm.from) || string.IsNullOrEmpty(nm.to)) continue;

                    // Qualifiers first (from. -> to.)
                    newTxt = newTxt.Replace(nm.from + ".", nm.to + ".");
                    // using from; -> using to;
                    newTxt = Regex.Replace(newTxt, "(^\\s*using\\s+)" + Regex.Escape(nm.from) + "(\\s*;)$", m => m.Groups[1].Value + nm.to + m.Groups[2].Value, RegexOptions.Multiline);
                    // namespace from -> namespace to
                    newTxt = Regex.Replace(newTxt, "(\\bnamespace\\s+)" + Regex.Escape(nm.from) + "(\\b|\\s|\\{)", m => m.Groups[1].Value + nm.to + " ", RegexOptions.Multiline);
                }
            }

            if (_applyClassMappings)
            {
                foreach (var cm in rules.classMappings)
                {
                    if (!string.IsNullOrEmpty(cm.fromQualified) && !string.IsNullOrEmpty(cm.toQualified))
                        newTxt = newTxt.Replace(cm.fromQualified, cm.toQualified);
                }
            }

            if (_applyCreateAssetMenu && rules.menuNameRule != null)
            {
                newTxt = Regex.Replace(newTxt, "(menuName\\s*=\\s*)\"([^\"]+)\"", m =>
                {
                    try
                    {
                        var cur = m.Groups[2].Value;
                        var rep = Regex.Replace(cur, rules.menuNameRule.replaceRegexFrom, rules.menuNameRule.replaceTo);
                        return m.Groups[1].Value + "\"" + rep + "\"";
                    }
                    catch { return m.Value; }
                }, RegexOptions.Multiline);
            }

            return newTxt;
        }

        private static string ToAssetPath(string fullPath)
        {
            fullPath = fullPath.Replace("\\", "/");
            var dataPath = Application.dataPath.Replace("\\", "/");
            if (fullPath.StartsWith(dataPath))
                return "Assets" + fullPath.Substring(dataPath.Length);
            if (fullPath.StartsWith(ProjectRoot))
                return fullPath.Substring(ProjectRoot.Length + 1);
            return fullPath;
        }

        private static string CombineToFull(string assetPath)
        {
            assetPath = assetPath.Replace("\\", "/");
            if (assetPath.StartsWith("Assets"))
                return Path.Combine(ProjectRoot, assetPath).Replace("\\", "/");
            return assetPath;
        }
    }
}
