using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// UI移行スキャナー - レガシーUI使用状況をスキャンしてレポートを生成
/// </summary>
public class UIMigrationScanner : EditorWindow
{
    private Vector2 scrollPosition;
    private bool scanCompleted = false;
    private Dictionary<string, List<string>> scanResults;
    private UIMigrationRules rules;
    private string reportPath = "Assets/Documentation/QA/LEGACY_UI_MIGRATION_REPORT.md";

    [MenuItem("Tools/UI Migration/Scanner", false, 1)]
    static void ShowWindow()
    {
        var window = GetWindow<UIMigrationScanner>("UI Migration Scanner");
        window.minSize = new Vector2(600, 400);
    }

    void OnEnable()
    {
        LoadRules();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("UI Migration Scanner", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("Scan Project for Legacy UI", GUILayout.Height(30)))
        {
            ScanProject();
        }

        EditorGUILayout.Space();

        if (scanCompleted && scanResults != null)
        {
            EditorGUILayout.LabelField("Scan Results:", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var category in scanResults)
            {
                EditorGUILayout.LabelField($"{category.Key} ({category.Value.Count} files):", EditorStyles.miniBoldLabel);

                foreach (var file in category.Value)
                {
                    if (GUILayout.Button(file, EditorStyles.label))
                    {
                        // ファイルをUnityで開く
                        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(file);
                        if (asset != null)
                        {
                            AssetDatabase.OpenAsset(asset);
                        }
                    }
                }

                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Report", GUILayout.Height(25)))
            {
                GenerateReport();
            }

            if (GUILayout.Button("Open Report Location", GUILayout.Height(25)))
            {
                EditorUtility.RevealInFinder(reportPath);
            }
        }
    }

    private void LoadRules()
    {
        string rulesPath = "Assets/Editor/Tools/UIMigration/UIMigrationRules.json";
        if (File.Exists(rulesPath))
        {
            string json = File.ReadAllText(rulesPath);
            rules = JsonUtility.FromJson<UIMigrationRules>(json);
        }
        else
        {
            rules = new UIMigrationRules();
            Debug.LogWarning("UIMigrationRules.json not found. Using default rules.");
        }
    }

    private void ScanProject()
    {
        scanResults = new Dictionary<string, List<string>>();
        scanCompleted = false;

        // スクリプトファイルの検索
        string[] scriptFiles = AssetDatabase.FindAssets("t:Script")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => path.EndsWith(".cs") && !path.Contains("/Editor/"))
            .ToArray();

        foreach (string filePath in scriptFiles)
        {
            string content = File.ReadAllText(filePath);

            // レガシーネームスペースの検出
            foreach (string legacyNs in rules.legacyNamespaces)
            {
                if (content.Contains(legacyNs))
                {
                    if (!scanResults.ContainsKey("Legacy Namespaces"))
                        scanResults["Legacy Namespaces"] = new List<string>();
                    scanResults["Legacy Namespaces"].Add(filePath);
                    break;
                }
            }

            // Input.GetKey系APIの検出
            if (Regex.IsMatch(content, @"Input\.GetKey"))
            {
                if (!scanResults.ContainsKey("Legacy Input API"))
                    scanResults["Legacy Input API"] = new List<string>();
                scanResults["Legacy Input API"].Add(filePath);
            }

            // OnGUIメソッドの検出
            if (content.Contains("void OnGUI()") || content.Contains("OnGUI()"))
            {
                if (!scanResults.ContainsKey("IMGUI Usage"))
                    scanResults["IMGUI Usage"] = new List<string>();
                scanResults["IMGUI Usage"].Add(filePath);
            }

            // uGUI関連の検出
            if (content.Contains("UnityEngine.UI.") || content.Contains("using UnityEngine.UI"))
            {
                if (!scanResults.ContainsKey("uGUI Usage"))
                    scanResults["uGUI Usage"] = new List<string>();
                scanResults["uGUI Usage"].Add(filePath);
            }

            // TextMeshPro関連の検出
            if (content.Contains("TMPro.") || content.Contains("using TMPro"))
            {
                if (!scanResults.ContainsKey("TextMeshPro Usage"))
                    scanResults["TextMeshPro Usage"] = new List<string>();
                scanResults["TextMeshPro Usage"].Add(filePath);
            }
        }

        scanCompleted = true;
        Debug.Log($"UI Migration scan completed. Found {scanResults.Sum(x => x.Value.Count)} files with legacy UI usage.");
    }

    private void GenerateReport()
    {
        StringBuilder report = new StringBuilder();

        report.AppendLine("# Legacy UI Migration Report");
        report.AppendLine();
        report.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();

        report.AppendLine("## Summary");
        report.AppendLine();
        report.AppendLine($"Total files scanned: {AssetDatabase.FindAssets("t:Script").Length}");
        report.AppendLine($"Files requiring migration: {scanResults.Sum(x => x.Value.Count)}");
        report.AppendLine();

        foreach (var category in scanResults)
        {
            report.AppendLine($"### {category.Key}");
            report.AppendLine();
            report.AppendLine($"Found in {category.Value.Count} files:");
            report.AppendLine();

            foreach (var file in category.Value)
            {
                report.AppendLine($"- {file}");
            }

            report.AppendLine();
        }

        report.AppendLine("## Migration Guidelines");
        report.AppendLine();
        report.AppendLine("### Input System Migration");
        report.AppendLine("- Replace `Input.GetKey*` with `Keyboard.current.*.wasPressedThisFrame`");
        report.AppendLine("- Update key codes from `KeyCode` to `Key` enum");
        report.AppendLine("- Add `UnityEngine.InputSystem` namespace");
        report.AppendLine();

        report.AppendLine("### UI Framework Migration");
        report.AppendLine("- Consider migrating from IMGUI to UITK for new UI");
        report.AppendLine("- Keep uGUI for existing components unless major refactoring");
        report.AppendLine("- TextMeshPro is recommended for text rendering");
        report.AppendLine();

        // レポートファイルの保存
        string directory = Path.GetDirectoryName(reportPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(reportPath, report.ToString());
        AssetDatabase.Refresh();

        Debug.Log($"UI Migration report generated: {reportPath}");
        EditorUtility.RevealInFinder(reportPath);
    }
}

[System.Serializable]
public class UIMigrationRules
{
    public string[] legacyNamespaces = new string[0];
    public string[] ignoreNamespaces = new string[0];
    public TypeMapping[] typeMappings = new TypeMapping[0];
    public bool heuristicsEnabled = true;
    public string notes = "";
}

[System.Serializable]
public class TypeMapping
{
    public string legacyType = "";
    public string newType = "";
}