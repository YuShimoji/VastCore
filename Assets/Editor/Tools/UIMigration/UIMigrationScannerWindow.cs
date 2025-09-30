// UIMigrationScannerWindow.cs
// Editor-only tool to scan the project for legacy uGUI / IMGUI usages and (optionally) generate a migration report.
// Dry-run only: does not perform any modifications.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Vastcore.EditorTools.UIMigration
{
    public class UIMigrationScannerWindow : EditorWindow
    {
        [MenuItem("Vastcore/Tools/UI Migration/Scan (Dry Run)")]
        public static void Open()
        {
            var wnd = GetWindow<UIMigrationScannerWindow>("UI Migration Scanner");
            wnd.minSize = new Vector2(600, 420);
            wnd.Focus();
        }

        [Serializable]
        public class ScanResults
        {
            public List<string> cs_UsingUnityEngineUI = new List<string>();
            public List<string> cs_OnGUI = new List<string>();
            public List<string> cs_TMPro = new List<string>();
            public List<string> cs_UIElements = new List<string>();

            public List<string> scenes_uGUI = new List<string>();
            public List<string> scenes_uiToolkit = new List<string>();

            public List<string> prefabs_uGUI = new List<string>();
            public List<string> prefabs_uiToolkit = new List<string>();

            public int FilesScannedScripts;
            public int FilesScannedScenes;
            public int FilesScannedPrefabs;
        }

        private Vector2 _scroll;
        private bool _excludeExamples = true; // Exclude third-party example assets (TMP examples, etc.)
        private bool _scanScripts = true;
        private bool _scanScenes = true;
        private bool _scanPrefabs = true;
        private string _reportPath = "Documentation/QA/LEGACY_UI_MIGRATION_REPORT.md";
        private ScanResults _last;

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
            EditorGUILayout.LabelField("UI Migration Scanner (Dry Run)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Scans for legacy uGUI (UnityEngine.UI), IMGUI (OnGUI), TextMeshPro-UGUI usage, and UI Toolkit indicators. Generates a Markdown report. This tool does not change any assets.", MessageType.Info);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                _excludeExamples = EditorGUILayout.ToggleLeft("Exclude example content (e.g., Assets/TextMesh Pro/Examples & Extras)", _excludeExamples);
                _scanScripts = EditorGUILayout.ToggleLeft("Scan C# scripts (*.cs)", _scanScripts);
                _scanScenes = EditorGUILayout.ToggleLeft("Scan Scenes (*.unity)", _scanScenes);
                _scanPrefabs = EditorGUILayout.ToggleLeft("Scan Prefabs (*.prefab)", _scanPrefabs);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Report Output Path (relative to project root)");
                using (new EditorGUILayout.HorizontalScope())
                {
                    _reportPath = EditorGUILayout.TextField(_reportPath);
                    if (GUILayout.Button("Browse", GUILayout.Width(80)))
                    {
                        var abs = Path.Combine(ProjectRoot, _reportPath);
                        var dir = Path.GetDirectoryName(abs) ?? ProjectRoot;
                        var picked = EditorUtility.SaveFilePanel("Select Report Output", dir, "LEGACY_UI_MIGRATION_REPORT", "md");
                        if (!string.IsNullOrEmpty(picked))
                        {
                            if (picked.StartsWith(ProjectRoot))
                                _reportPath = picked.Substring(ProjectRoot.Length + 1).Replace("\\", "/");
                            else
                                EditorUtility.DisplayDialog("Warning", "Please select a path inside the current project.", "OK");
                        }
                    }
                }
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan Project", GUILayout.Height(28)))
                {
                    _last = RunScan();
                }
                if (GUILayout.Button("Scan + Generate Report", GUILayout.Height(28)))
                {
                    _last = RunScan();
                    TryGenerateReport(_last);
                }
                if (GUILayout.Button("Generate Report (Last Results)", GUILayout.Height(28)))
                {
                    if (_last == null)
                    {
                        EditorUtility.DisplayDialog("No Results", "Please run a scan first.", "OK");
                    }
                    else
                    {
                        TryGenerateReport(_last);
                    }
                }
            }

            EditorGUILayout.Space();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_last != null)
            {
                DrawResults(_last);
            }
            else
            {
                EditorGUILayout.HelpBox("No results yet. Click 'Scan Project' to analyze the repository.", MessageType.None);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawResults(ScanResults r)
        {
            EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Scripts scanned: {r.FilesScannedScripts}");
            EditorGUILayout.LabelField($"Scenes scanned: {r.FilesScannedScenes}");
            EditorGUILayout.LabelField($"Prefabs scanned: {r.FilesScannedPrefabs}");
            EditorGUILayout.Space(6);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("C# Findings", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"using UnityEngine.UI: {r.cs_UsingUnityEngineUI.Count}");
                EditorGUILayout.LabelField($"OnGUI occurrences: {r.cs_OnGUI.Count}");
                EditorGUILayout.LabelField($"TMPro API references: {r.cs_TMPro.Count}");
                EditorGUILayout.LabelField($"UI Toolkit (UIElements/UIDocument): {r.cs_UIElements.Count}");

                if (r.cs_UsingUnityEngineUI.Count > 0)
                    DrawList("UnityEngine.UI in:", r.cs_UsingUnityEngineUI);
                if (r.cs_OnGUI.Count > 0)
                    DrawList("OnGUI in:", r.cs_OnGUI);
                if (r.cs_TMPro.Count > 0)
                    DrawList("TMPro references in:", r.cs_TMPro);
                if (r.cs_UIElements.Count > 0)
                    DrawList("UI Toolkit in:", r.cs_UIElements);
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Scene Findings", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Scenes with uGUI markers (Canvas/RectTransform/CanvasRenderer): {r.scenes_uGUI.Count}");
                EditorGUILayout.LabelField($"Scenes with UI Toolkit (UIDocument): {r.scenes_uiToolkit.Count}");
                if (r.scenes_uGUI.Count > 0)
                    DrawList("uGUI Scenes:", r.scenes_uGUI);
                if (r.scenes_uiToolkit.Count > 0)
                    DrawList("UI Toolkit Scenes:", r.scenes_uiToolkit);
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("Prefab Findings", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Prefabs with uGUI markers: {r.prefabs_uGUI.Count}");
                EditorGUILayout.LabelField($"Prefabs with UI Toolkit (UIDocument): {r.prefabs_uiToolkit.Count}");
                if (r.prefabs_uGUI.Count > 0)
                    DrawList("uGUI Prefabs:", r.prefabs_uGUI);
                if (r.prefabs_uiToolkit.Count > 0)
                    DrawList("UI Toolkit Prefabs:", r.prefabs_uiToolkit);
            }
        }

        private void DrawList(string title, List<string> items)
        {
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
            foreach (var p in items.Take(100))
            {
                EditorGUILayout.LabelField("• " + p);
            }
            if (items.Count > 100)
            {
                EditorGUILayout.LabelField($"(+ {items.Count - 100} more)");
            }
        }

        private ScanResults RunScan()
        {
            var results = new ScanResults();
            var dataPath = Application.dataPath.Replace("\\", "/");

            bool Excluded(string path)
            {
                if (!_excludeExamples) return false;
                path = path.Replace("\\", "/");
                if (path.Contains("Assets/TextMesh Pro/Examples & Extras/")) return true;
                return false;
            }

            // Scripts
            if (_scanScripts)
            {
                var csFiles = Directory.EnumerateFiles(dataPath, "*.cs", SearchOption.AllDirectories).ToArray();
                results.FilesScannedScripts = csFiles.Length;
                foreach (var full in csFiles)
                {
                    var assetPath = ToAssetPath(full);
                    if (Excluded(assetPath)) continue;

                    string txt;
                    try { txt = File.ReadAllText(full); }
                    catch { continue; }

                    if (txt.Contains("UnityEngine.UI")) results.cs_UsingUnityEngineUI.Add(assetPath);
                    if (txt.Contains("OnGUI(")) results.cs_OnGUI.Add(assetPath);
                    if (txt.Contains("TMPro") || txt.Contains("TMP_Text") || txt.Contains("TextMeshProUGUI") || txt.Contains("TMP_InputField")) results.cs_TMPro.Add(assetPath);
                    if (txt.Contains("UnityEngine.UIElements") || txt.Contains("UIDocument")) results.cs_UIElements.Add(assetPath);
                }
            }

            // Scenes
            if (_scanScenes)
            {
                var sceneFiles = Directory.EnumerateFiles(dataPath, "*.unity", SearchOption.AllDirectories).ToArray();
                results.FilesScannedScenes = sceneFiles.Length;
                foreach (var full in sceneFiles)
                {
                    var assetPath = ToAssetPath(full);
                    if (Excluded(assetPath)) continue;

                    string yaml;
                    try { yaml = File.ReadAllText(full); }
                    catch { continue; }

                    if (yaml.Contains("UIDocument:")) results.scenes_uiToolkit.Add(assetPath);
                    if (yaml.Contains("Canvas:") || yaml.Contains("RectTransform:") || yaml.Contains("CanvasRenderer:") || yaml.Contains("GraphicRaycaster:"))
                        results.scenes_uGUI.Add(assetPath);
                }
            }

            // Prefabs
            if (_scanPrefabs)
            {
                var prefabFiles = Directory.EnumerateFiles(dataPath, "*.prefab", SearchOption.AllDirectories).ToArray();
                results.FilesScannedPrefabs = prefabFiles.Length;
                foreach (var full in prefabFiles)
                {
                    var assetPath = ToAssetPath(full);
                    if (Excluded(assetPath)) continue;

                    string yaml;
                    try { yaml = File.ReadAllText(full); }
                    catch { continue; }

                    if (yaml.Contains("UIDocument:")) results.prefabs_uiToolkit.Add(assetPath);
                    if (yaml.Contains("RectTransform:") || yaml.Contains("CanvasRenderer:") || yaml.Contains("GraphicRaycaster:"))
                        results.prefabs_uGUI.Add(assetPath);
                }
            }

            Debug.Log($"[UI Migration Scanner] Scan completed. Scripts={results.FilesScannedScripts}, Scenes={results.FilesScannedScenes}, Prefabs={results.FilesScannedPrefabs}");
            return results;
        }

        private void TryGenerateReport(ScanResults results)
        {
            try
            {
                var abs = Path.Combine(ProjectRoot, _reportPath).Replace("\\", "/");
                var outDir = Path.GetDirectoryName(abs);
                if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

                var md = BuildReportMarkdown(results);
                File.WriteAllText(abs, md, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                AssetDatabase.Refresh();
                EditorUtility.RevealInFinder(abs);
                Debug.Log($"[UI Migration Scanner] Report generated: {abs}");
            }
            catch (Exception ex)
            {
                Debug.LogError("[UI Migration Scanner] Failed to generate report: " + ex.Message);
                EditorUtility.DisplayDialog("Error", "Failed to generate report. See Console for details.", "OK");
            }
        }

        // Public helper for headless / auto-run scenarios
        public static void RunDryScanAndGenerateReport(
            string reportPath = "Documentation/QA/LEGACY_UI_MIGRATION_REPORT.md",
            bool excludeExamples = true,
            bool scanScripts = true,
            bool scanScenes = true,
            bool scanPrefabs = true)
        {
            var wnd = CreateInstance<UIMigrationScannerWindow>();
            wnd._reportPath = reportPath;
            wnd._excludeExamples = excludeExamples;
            wnd._scanScripts = scanScripts;
            wnd._scanScenes = scanScenes;
            wnd._scanPrefabs = scanPrefabs;
            var results = wnd.RunScan();
            wnd.TryGenerateReport(results);
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

        private string BuildReportMarkdown(ScanResults r)
        {
            var now = DateTime.Now;
            var sb = new StringBuilder();
            sb.AppendLine("# Legacy UI Migration Report (Dry Run)");
            sb.AppendLine();
            sb.AppendLine($"- Generated: {now:yyyy-MM-dd HH:mm} (local)");
            sb.AppendLine($"- Tool: UIMigrationScannerWindow.cs");
            sb.AppendLine($"- Options: ExcludeExamples={_excludeExamples}, ScanScripts={_scanScripts}, ScanScenes={_scanScenes}, ScanPrefabs={_scanPrefabs}");
            sb.AppendLine();

            sb.AppendLine("## Summary");
            sb.AppendLine($"- Scripts scanned: {r.FilesScannedScripts}");
            sb.AppendLine($"- Scenes scanned: {r.FilesScannedScenes}");
            sb.AppendLine($"- Prefabs scanned: {r.FilesScannedPrefabs}");
            sb.AppendLine($"- C# using UnityEngine.UI: {r.cs_UsingUnityEngineUI.Count}");
            sb.AppendLine($"- C# OnGUI occurrences: {r.cs_OnGUI.Count}");
            sb.AppendLine($"- C# TMPro references: {r.cs_TMPro.Count}");
            sb.AppendLine($"- C# UI Toolkit references: {r.cs_UIElements.Count}");
            sb.AppendLine($"- Scenes with uGUI markers: {r.scenes_uGUI.Count}");
            sb.AppendLine($"- Scenes with UI Toolkit: {r.scenes_uiToolkit.Count}");
            sb.AppendLine($"- Prefabs with uGUI markers: {r.prefabs_uGUI.Count}");
            sb.AppendLine($"- Prefabs with UI Toolkit: {r.prefabs_uiToolkit.Count}");
            sb.AppendLine();

            void AppendList(string title, IEnumerable<string> items)
            {
                var list = items.Distinct().OrderBy(p => p).ToList();
                sb.AppendLine($"### {title} ({list.Count})");
                if (list.Count == 0)
                {
                    sb.AppendLine("- None");
                }
                else
                {
                    foreach (var p in list.Take(200)) sb.AppendLine("- " + p);
                    if (list.Count > 200) sb.AppendLine($"- (+ {list.Count - 200} more)");
                }
                sb.AppendLine();
            }

            AppendList("C# files using UnityEngine.UI", r.cs_UsingUnityEngineUI);
            AppendList("C# files with OnGUI", r.cs_OnGUI);
            AppendList("C# files referencing TMPro", r.cs_TMPro);
            AppendList("C# files referencing UI Toolkit", r.cs_UIElements);

            AppendList("Scenes with uGUI markers", r.scenes_uGUI);
            AppendList("Scenes with UI Toolkit (UIDocument)", r.scenes_uiToolkit);

            AppendList("Prefabs with uGUI markers", r.prefabs_uGUI);
            AppendList("Prefabs with UI Toolkit (UIDocument)", r.prefabs_uiToolkit);

            sb.AppendLine("## Migration Rules (Draft)");
            sb.AppendLine("- Canvas + RectTransform -> UIDocument + UXML layout (VisualTreeAsset) + USS for styling");
            sb.AppendLine("- GraphicRaycaster -> PanelSettings (and Input System UI bindings) for UITK");
            sb.AppendLine("- EventSystem/StandaloneInputModule -> UITK input via PanelSettings (or Input System package)\n  Note: For hybrid approaches, keep EventSystem for remaining uGUI while introducing UIDocument side-by-side.");
            sb.AppendLine("- TextMeshProUGUI/TMP_InputField -> UITK Label / TextElement / TextField equivalents");
            sb.AppendLine("- Image/RawImage -> VisualElement with background-image or inline `Image` for UITK (2023.3+)");
            sb.AppendLine("- Button/Toggle/Slider -> UITK Button/Toggle/Slider; rewrite listeners using UITK callbacks");
            sb.AppendLine("- IMGUI (OnGUI) overlays -> replace with UITK runtime UI or IMGUIContainer bridge (temporary)");
            sb.AppendLine();

            sb.AppendLine("## Suggested Approach");
            sb.AppendLine("- Start with non-interactive HUD panels to validate UIDocument + PanelSettings");
            sb.AppendLine("- Introduce a hybrid scene: keep uGUI for menu while new HUD uses UITK");
            sb.AppendLine("- Migrate TMP-driven text elements last; validate font assets for UITK");
            sb.AppendLine("- Replace OnGUI-based debug overlays with a UITK Debug Window");
            sb.AppendLine();

            sb.AppendLine("## Notes");
            sb.AppendLine("- This report is generated by a dry-run scanner based on simple token heuristics. Validate findings in Editor before changes.");
            sb.AppendLine("- Example content (e.g., TextMesh Pro examples) may be excluded depending on the option.");

            return sb.ToString();
        }
    }
}
