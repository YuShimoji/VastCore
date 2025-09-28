#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Vastcore.EditorTools
{
    [Serializable]
    internal class UIMigrationRules
    {
        public string[] legacyNamespaces = Array.Empty<string>();
        public string[] ignoreNamespaces = Array.Empty<string>();
        public TypeMapping[] typeMappings = Array.Empty<TypeMapping>();
        public bool heuristicsEnabled = true;
        public string notes = string.Empty;
    }

    [Serializable]
    internal class TypeMapping
    {
        public string legacyType = string.Empty;   // e.g., OldNS.OldHUD
        public string newType = string.Empty;      // e.g., Vastcore.UI.NewHUD
    }

    internal struct ScriptInfo
    {
        public string guid;
        public string path;
        public string @namespace;
        public string className;
        public string FullyQualifiedName =>
            string.IsNullOrEmpty(@namespace) ? className : ($"{@namespace}.{className}");
    }

    public static class UIMigrationScanner
    {
        private const string RulesPath = "Assets/Editor/Tools/UIMigration/UIMigrationRules.json";
        private const string ReportPath = "Documentation/QA/LEGACY_UI_MIGRATION_REPORT.md";
        private const string AutoRunFlagPath = "Documentation/QA/AUTO_RUN_UI_MIGRATION_SCAN.flag";

        [MenuItem("Vastcore/Tools/UI Migration/Scan (Dry Run)")]
        public static void RunScanMenu()
        {
            RunScan();
            EditorUtility.DisplayDialog("UI Migration Scan", "ドライランスキャンが完了しました。レポートを開きます。", "OK");
            EditorUtility.RevealInFinder(Path.GetFullPath(ReportPath));
        }

        [InitializeOnLoadMethod]
        private static void AutoRunIfFlagPresent()
        {
            try
            {
                var fullFlag = Path.GetFullPath(AutoRunFlagPath);
                if (File.Exists(fullFlag))
                {
                    Debug.Log("[UIMigrationScanner] Auto-run flag detected. Running dry-run scan...");
                    RunScan();
                    File.Delete(fullFlag);
                    AssetDatabase.Refresh();
                    Debug.Log("[UIMigrationScanner] Scan completed and flag removed. Report generated at: " + ReportPath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIMigrationScanner] Auto-run failed: {e.Message}");
            }
        }

        public static void RunScan()
        {
            var rules = LoadRules();
            var scriptIndex = BuildScriptIndex(); // guid -> ScriptInfo

            var legacyTypeSet = new HashSet<string>(StringComparer.Ordinal);
            foreach (var m in rules.typeMappings)
            {
                if (!string.IsNullOrWhiteSpace(m.legacyType)) legacyTypeSet.Add(m.legacyType.Trim());
            }

            var legacyNamespaceSet = new HashSet<string>(rules.legacyNamespaces ?? Array.Empty<string>());
            var ignoreNamespaceSet = new HashSet<string>(rules.ignoreNamespaces ?? Array.Empty<string>());

            // Collect candidates by code (MonoScript) first
            var codeCandidates = new List<ScriptInfo>();
            foreach (var kv in scriptIndex)
            {
                var si = kv.Value;
                if (IsLegacyByRules(si, legacyNamespaceSet, legacyTypeSet))
                {
                    codeCandidates.Add(si);
                }
                else if (rules.heuristicsEnabled && IsLegacyByHeuristics(si, ignoreNamespaceSet))
                {
                    codeCandidates.Add(si);
                }
            }

            // Asset scan (YAML guid reference)
            var assetMatches = new List<(string assetPath, List<ScriptInfo> hits)>();
            var prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            var sceneGuids = AssetDatabase.FindAssets("t:Scene");
            foreach (var guid in prefabGuids.Concat(sceneGuids))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var full = Path.GetFullPath(assetPath);
                try
                {
                    var text = File.ReadAllText(full);
                    var referencedGuids = ExtractGuidsFromYaml(text);
                    var hits = new List<ScriptInfo>();
                    foreach (var rg in referencedGuids)
                    {
                        if (!scriptIndex.TryGetValue(rg, out var si)) continue;
                        if (IsLegacyByRules(si, legacyNamespaceSet, legacyTypeSet) ||
                            (rules.heuristicsEnabled && IsLegacyByHeuristics(si, ignoreNamespaceSet)))
                        {
                            hits.Add(si);
                        }
                    }
                    if (hits.Count > 0)
                    {
                        // distinct by FQN
                        hits = hits
                            .GroupBy(h => h.FullyQualifiedName)
                            .Select(g => g.First())
                            .OrderBy(h => h.FullyQualifiedName)
                            .ToList();
                        assetMatches.Add((assetPath, hits));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[UIMigrationScanner] Failed to read {assetPath}: {e.Message}");
                }
            }

            WriteReport(rules, codeCandidates, assetMatches);
        }

        private static UIMigrationRules LoadRules()
        {
            try
            {
                var full = Path.GetFullPath(RulesPath);
                if (!File.Exists(full))
                {
                    Debug.LogWarning($"[UIMigrationScanner] Rules file not found: {RulesPath}. Using defaults.");
                    return new UIMigrationRules();
                }
                var json = File.ReadAllText(full);
                var rules = JsonUtility.FromJson<UIMigrationRules>(json);
                return rules ?? new UIMigrationRules();
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIMigrationScanner] Failed to load rules: {e.Message}");
                return new UIMigrationRules();
            }
        }

        private static Dictionary<string, ScriptInfo> BuildScriptIndex()
        {
            var dict = new Dictionary<string, ScriptInfo>(StringComparer.Ordinal);
            var monoScriptGuids = AssetDatabase.FindAssets("t:MonoScript");
            foreach (var guid in monoScriptGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms == null) continue;
                var type = ms.GetClass();
                var className = type != null ? type.Name : Path.GetFileNameWithoutExtension(path);
                var ns = type != null ? (type.Namespace ?? string.Empty) : ExtractNamespaceFallback(path);
                dict[guid] = new ScriptInfo
                {
                    guid = guid,
                    path = path,
                    @namespace = ns ?? string.Empty,
                    className = className ?? string.Empty
                };
            }
            return dict;
        }

        private static string ExtractNamespaceFallback(string path)
        {
            try
            {
                var full = Path.GetFullPath(path);
                var text = File.ReadAllText(full);
                var m = Regex.Match(text, @"namespace\s+([A-Za-z0-9_\.]+)");
                if (m.Success) return m.Groups[1].Value.Trim();
            }
            catch {}
            return string.Empty;
        }

        private static bool IsLegacyByRules(ScriptInfo si, HashSet<string> legacyNamespaces, HashSet<string> legacyTypes)
        {
            if (!string.IsNullOrEmpty(si.@namespace) && legacyNamespaces.Contains(si.@namespace)) return true;
            var fqn = si.FullyQualifiedName;
            if (!string.IsNullOrEmpty(fqn) && legacyTypes.Contains(fqn)) return true;
            return false;
        }

        private static bool IsLegacyByHeuristics(ScriptInfo si, HashSet<string> ignoreNamespaces)
        {
            if (!string.IsNullOrEmpty(si.@namespace))
            {
                foreach (var ign in ignoreNamespaces)
                {
                    if (!string.IsNullOrEmpty(ign) && si.@namespace.StartsWith(ign, StringComparison.Ordinal))
                        return false;
                }
            }
            var n = si.className ?? string.Empty;
            if (n.IndexOf("UI", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (n.IndexOf("HUD", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (n.IndexOf("Menu", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static readonly Regex GuidRegex = new Regex(@"guid:\s*([a-f0-9]{32})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static HashSet<string> ExtractGuidsFromYaml(string yaml)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Match m in GuidRegex.Matches(yaml))
            {
                var g = m.Groups[1].Value.ToLowerInvariant();
                if (!string.IsNullOrEmpty(g)) set.Add(g);
            }
            return set;
        }

        private static void WriteReport(UIMigrationRules rules, List<ScriptInfo> codeCandidates, List<(string assetPath, List<ScriptInfo> hits)> assetMatches)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Legacy UI Migration Report");
            sb.AppendLine();
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine($"コード上の候補スクリプト数: {codeCandidates.Count}");
            sb.AppendLine($"アセット（シーン/Prefab）での検出箇所数: {assetMatches.Count}");
            sb.AppendLine();

            // Rules excerpt
            sb.AppendLine("## 使用ルール（抜粋）");
            sb.AppendLine("- legacyNamespaces: " + string.Join(", ", rules.legacyNamespaces ?? Array.Empty<string>()));
            sb.AppendLine("- ignoreNamespaces: " + string.Join(", ", rules.ignoreNamespaces ?? Array.Empty<string>()));
            sb.AppendLine("- heuristicsEnabled: " + rules.heuristicsEnabled);
            sb.AppendLine();

            sb.AppendLine("## コード上の候補（名前空間/型）");
            if (codeCandidates.Count == 0) sb.AppendLine("(なし)");
            else
            {
                foreach (var g in codeCandidates.OrderBy(c => c.FullyQualifiedName))
                {
                    sb.AppendLine($"- {g.FullyQualifiedName}  (`{g.path}`)");
                }
            }
            sb.AppendLine();

            sb.AppendLine("## アセットでの検出（シーン/Prefab）");
            if (assetMatches.Count == 0) sb.AppendLine("(なし)");
            else
            {
                sb.AppendLine("| Asset Path | Matches |");
                sb.AppendLine("|---|---|");
                foreach (var (assetPath, hits) in assetMatches.OrderBy(a => a.assetPath))
                {
                    var joined = string.Join(", ", hits.Select(h => h.FullyQualifiedName));
                    sb.AppendLine($"| {assetPath} | {joined} |");
                }
            }

            try
            {
                var full = Path.GetFullPath(ReportPath);
                Directory.CreateDirectory(Path.GetDirectoryName(full) ?? ".");
                File.WriteAllText(full, sb.ToString(), Encoding.UTF8);
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogError($"[UIMigrationScanner] Failed to write report: {e.Message}");
            }
        }
    }
}
#endif
