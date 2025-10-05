// UIMigrationRulesDryRunWindow.cs
// Editor-only window to read JSON rules and generate a rules-driven dry-run report (no modifications).
// This complements UIMigrationScannerWindow.

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
    public class UIMigrationRulesDryRunWindow : EditorWindow
    {
        [MenuItem("Vastcore/Tools/UI Migration/Rules Dry-Run (JSON)")]
        public static void Open()
        {
            var wnd = GetWindow<UIMigrationRulesDryRunWindow>("UI Migration Rules Dry-Run");
            wnd.minSize = new Vector2(640, 420);
            wnd.Focus();
        }

        [Serializable]
        private class RuleSet
        {
            public string version;
            public List<NamespaceMapping> namespaceMappings = new List<NamespaceMapping>();
            public List<ClassMapping> classMappings = new List<ClassMapping>();
            public MenuNameReplace menuNameRule; // Simplified support for CreateAssetMenu.menuName
        }

        [Serializable]
        private class NamespaceMapping { public string from; public string to; }
        [Serializable]
        private class ClassMapping { public string fromQualified; public string toQualified; public string note; }
        [Serializable]
        private class MenuNameReplace { public string replaceRegexFrom; public string replaceTo; }

        private Vector2 _scroll;
        private string _rulesJsonPath = "docs/ui-migration/ui_mapping_rules.template.json"; // relative to project root
        private string _reportPath = "Documentation/QA/UI_MIGRATION_RULES_DRYRUN.md";     // relative to project root
        private bool _excludeExamples = true;

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
            EditorGUILayout.LabelField("UI Migration Rules Dry-Run", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Reads JSON rules and performs a dry-run plan (no modifications). Outputs a Markdown report with matched files and proposed replacements.", MessageType.Info);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                _excludeExamples = EditorGUILayout.ToggleLeft("Exclude example content (e.g., Assets/TextMesh Pro/Examples & Extras)", _excludeExamples);

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

                EditorGUILayout.LabelField("Dry-run Report Output (relative to project root)");
                using (new EditorGUILayout.HorizontalScope())
                {
                    _reportPath = EditorGUILayout.TextField(_reportPath);
                    if (GUILayout.Button("Browse", GUILayout.Width(80)))
                    {
                        var abs = Path.Combine(ProjectRoot, _reportPath);
                        var dir = Path.GetDirectoryName(abs) ?? ProjectRoot;
                        var picked = EditorUtility.SaveFilePanel("Select Rules Report Output", dir, "UI_MIGRATION_RULES_DRYRUN", "md");
                        if (!string.IsNullOrEmpty(picked))
                        {
                            if (picked.StartsWith(ProjectRoot))
                                _reportPath = picked.Substring(ProjectRoot.Length + 1).Replace("\\", "/");
                            else
                                EditorUtility.DisplayDialog("Warning", "Please select a path inside the current project.", "OK");
                        }
                    }
                }

                EditorGUILayout.Space(6);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Run Dry-Run", GUILayout.Height(26)))
                    {
                        TryRun();
                    }
                }
            }

            EditorGUILayout.Space();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.HelpBox("This tool does not modify assets. Review the generated report and plan staged changes for the next step.", MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private void TryRun()
        {
            try
            {
                var absJson = Path.Combine(ProjectRoot, _rulesJsonPath).Replace("\\", "/");
                var absOut = Path.Combine(ProjectRoot, _reportPath).Replace("\\", "/");
                var rules = LoadRuleSetFromJson(absJson);
                if (rules == null)
                {
                    EditorUtility.DisplayDialog("Rules", "Failed to load rules JSON. Check path and format.", "OK");
                    return;
                }
                var res = RunRulesDrivenScan(rules);
                TryGenerateRulesReport(res, absOut, rules);
                EditorUtility.RevealInFinder(absOut);
                Debug.Log("[UI Migration Rules] Dry run completed.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[UI Migration Rules] Dry run failed: " + ex.Message);
                EditorUtility.DisplayDialog("Error", "Dry-run failed. See Console for details.", "OK");
            }
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
                    var itemRx = new Regex("\\{\\s*\"from\"\\s*:\\s*\"([^\"]+)\",\\s*\"to\"\\s*:\\s*\"([^\"]+)\"\\s*\\}");
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

                // Attribute: UnityEngine.CreateAssetMenuAttribute -> fields.menuName { replaceRegexFrom, replaceTo }
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
                Debug.LogWarning("[UI Migration Rules] Failed to parse rules JSON heuristically: " + ex.Message);
            }
            return rs;
        }

        private class RulesDryRunResults
        {
            public Dictionary<string, List<string>> namespaceHits = new Dictionary<string, List<string>>(); // key: from->to label
            public Dictionary<string, List<string>> classHits = new Dictionary<string, List<string>>(); // key: fromQualified->toQualified
            public List<string> menuNameHits = new List<string>(); // file list
            public int filesScanned;
            public string rulesVersion;
        }

        private RulesDryRunResults RunRulesDrivenScan(RuleSet rules)
        {
            var res = new RulesDryRunResults { rulesVersion = rules.version };
            var dataPath = Application.dataPath.Replace("\\", "/");
            var csFiles = Directory.EnumerateFiles(dataPath, "*.cs", SearchOption.AllDirectories).ToArray();
            res.filesScanned = csFiles.Length;

            foreach (var full in csFiles)
            {
                var assetPath = ToAssetPath(full);
                if (_excludeExamples && assetPath.Replace("\\", "/").Contains("Assets/TextMesh Pro/Examples & Extras/")) continue;
                string txt; try { txt = File.ReadAllText(full); } catch { continue; }

                // Namespace mappings
                foreach (var nm in rules.namespaceMappings)
                {
                    if (string.IsNullOrEmpty(nm.from) || string.IsNullOrEmpty(nm.to)) continue;
                    if (txt.Contains("using " + nm.from) || txt.Contains(nm.from + ".") || Regex.IsMatch(txt, @"\bnamespace\s+" + Regex.Escape(nm.from)))
                    {
                        var key = nm.from + " -> " + nm.to;
                        if (!res.namespaceHits.TryGetValue(key, out var list)) { list = new List<string>(); res.namespaceHits[key] = list; }
                        list.Add(assetPath);
                    }
                }

                // Class mappings
                foreach (var cm in rules.classMappings)
                {
                    if (string.IsNullOrEmpty(cm.fromQualified)) continue;
                    if (txt.Contains(cm.fromQualified))
                    {
                        var key = cm.fromQualified + " -> " + (cm.toQualified ?? "<unchanged>");
                        if (!res.classHits.TryGetValue(key, out var list)) { list = new List<string>(); res.classHits[key] = list; }
                        list.Add(assetPath);
                    }
                }

                // Attribute menuName rule
                if (rules.menuNameRule != null)
                {
                    // Rough match of CreateAssetMenu with menuName
                    if (Regex.IsMatch(txt, @"\[\s*CreateAssetMenu[^\]]*menuName\s*=\s*""[^""]+""", RegexOptions.Multiline))
                    {
                        // Optional: test if replacement would change
                        var m = Regex.Match(txt, @"menuName\s*=\s*""([^""]+)""");
                        if (m.Success)
                        {
                            try
                            {
                                var current = m.Groups[1].Value;
                                var replaced = Regex.Replace(current, rules.menuNameRule.replaceRegexFrom, rules.menuNameRule.replaceTo);
                                if (!string.Equals(current, replaced))
                                {
                                    res.menuNameHits.Add(assetPath);
                                }
                            }
                            catch { /* ignore invalid regex */ }
                        }
                    }
                }
            }

            return res;
        }

        private void TryGenerateRulesReport(RulesDryRunResults r, string absOut, RuleSet rules)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# UI Migration Rules Dry-Run Report");
            sb.AppendLine();
            sb.AppendLine($"- Rules Version: {r.rulesVersion ?? "n/a"}");
            sb.AppendLine($"- Files Scanned: {r.filesScanned}");
            sb.AppendLine();

            sb.AppendLine("## Namespace Mappings Matches");
            if (r.namespaceHits.Count == 0) sb.AppendLine("- None");
            foreach (var kv in r.namespaceHits.OrderBy(k => k.Key))
            {
                sb.AppendLine($"### {kv.Key} ({kv.Value.Count})");
                foreach (var f in kv.Value.Distinct().OrderBy(p => p).Take(200)) sb.AppendLine("- " + f);
                sb.AppendLine();
            }

            sb.AppendLine("## Class Mappings Matches");
            if (r.classHits.Count == 0) sb.AppendLine("- None");
            foreach (var kv in r.classHits.OrderBy(k => k.Key))
            {
                sb.AppendLine($"### {kv.Key} ({kv.Value.Count})");
                foreach (var f in kv.Value.Distinct().OrderBy(p => p).Take(200)) sb.AppendLine("- " + f);
                sb.AppendLine();
            }

            sb.AppendLine("## CreateAssetMenu 'menuName' Candidates");
            if (r.menuNameHits.Count == 0) sb.AppendLine("- None");
            foreach (var f in r.menuNameHits.Distinct().OrderBy(p => p).Take(200)) sb.AppendLine("- " + f);

            try
            {
                var outDir = Path.GetDirectoryName(absOut);
                if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
                File.WriteAllText(absOut, sb.ToString(), new UTF8Encoding(false));
                AssetDatabase.Refresh();
                Debug.Log($"[UI Migration Rules] Report generated: {absOut}");
            }
            catch (Exception ex)
            {
                Debug.LogError("[UI Migration Rules] Failed to write report: " + ex.Message);
            }
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
    }
}
