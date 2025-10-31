using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// UI移行適用ウィンドウ - レガシーUIの自動移行を実行
/// </summary>
public class UIMigrationApplyWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private bool applyCompleted = false;
    private Dictionary<string, List<MigrationResult>> applyResults;
    private UIMigrationRules rules;
    private string applyReportPath = "Assets/Documentation/QA/UI_MIGRATION_APPLY_REPORT.md";

    [MenuItem("Tools/UI Migration/Apply", false, 2)]
    static void ShowWindow()
    {
        var window = GetWindow<UIMigrationApplyWindow>("UI Migration Apply");
        window.minSize = new Vector2(600, 400);
    }

    void OnEnable()
    {
        LoadRules();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("UI Migration Apply", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "⚠️ このツールは自動的にコードを変更します。\n" +
            "実行前に必ずバックアップを作成してください。\n" +
            "変更内容は元に戻せない場合があります。",
            MessageType.Warning
        );

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply UI Migration (Dry Run)", GUILayout.Height(30)))
        {
            ApplyMigration(true);
        }

        if (GUILayout.Button("Apply UI Migration (Execute)", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "UI Migration Apply",
                "本当にUI移行を適用しますか？\nこの操作は取り消せません。",
                "実行",
                "キャンセル"))
            {
                ApplyMigration(false);
            }
        }

        EditorGUILayout.Space();

        if (applyCompleted && applyResults != null)
        {
            EditorGUILayout.LabelField("Apply Results:", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var category in applyResults)
            {
                EditorGUILayout.LabelField($"{category.Key} ({category.Value.Count} items):", EditorStyles.miniBoldLabel);

                foreach (var result in category.Value)
                {
                    var style = result.success ? EditorStyles.label : EditorStyles.boldLabel;
                    if (GUILayout.Button($"{result.filePath}: {result.message}", style))
                    {
                        // ファイルをUnityで開く
                        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(result.filePath);
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

            if (GUILayout.Button("Generate Apply Report", GUILayout.Height(25)))
            {
                GenerateApplyReport();
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

    private void ApplyMigration(bool dryRun)
    {
        applyResults = new Dictionary<string, List<MigrationResult>>();
        applyCompleted = false;

        // 移行対象ファイルの検索
        string[] scriptFiles = AssetDatabase.FindAssets("t:Script")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => path.EndsWith(".cs") && !path.Contains("/Editor/"))
            .ToArray();

        foreach (string filePath in scriptFiles)
        {
            string originalContent = File.ReadAllText(filePath);
            string migratedContent = originalContent;

            // Input System移行の適用
            var inputResults = ApplyInputSystemMigration(filePath, ref migratedContent, dryRun);
            if (inputResults.Count > 0)
            {
                if (!applyResults.ContainsKey("Input System Migration"))
                    applyResults["Input System Migration"] = new List<MigrationResult>();
                applyResults["Input System Migration"].AddRange(inputResults);
            }

            // タイプマッピングの適用
            var typeResults = ApplyTypeMappingMigration(filePath, ref migratedContent, dryRun);
            if (typeResults.Count > 0)
            {
                if (!applyResults.ContainsKey("Type Mapping Migration"))
                    applyResults["Type Mapping Migration"] = new List<MigrationResult>();
                applyResults["Type Mapping Migration"].AddRange(typeResults);
            }

            // ファイルの保存（dry runでない場合）
            if (!dryRun && migratedContent != originalContent)
            {
                File.WriteAllText(filePath, migratedContent);
            }
        }

        applyCompleted = true;

        if (dryRun)
        {
            Debug.Log($"UI Migration dry run completed. Found {applyResults.Sum(x => x.Value.Count)} migration opportunities.");
        }
        else
        {
            Debug.Log($"UI Migration apply completed. Applied {applyResults.Sum(x => x.Value.Count)} migrations.");
            AssetDatabase.Refresh();
        }
    }

    private List<MigrationResult> ApplyInputSystemMigration(string filePath, ref string content, bool dryRun)
    {
        var results = new List<MigrationResult>();

        // Input.GetKeyDown -> Keyboard.current.wasPressedThisFrame
        var getKeyDownPattern = @"Input\.GetKeyDown\(([^)]+)\)";
        var getKeyDownMatches = Regex.Matches(content, getKeyDownPattern);

        foreach (Match match in getKeyDownMatches)
        {
            string keyParam = match.Groups[1].Value.Trim();
            string replacement = $"Keyboard.current[{keyParam.Replace("KeyCode.", "Key.")}].wasPressedThisFrame";

            if (!content.Contains("using UnityEngine.InputSystem;"))
            {
                // Input System usingディレクティブを追加
                results.Add(new MigrationResult
                {
                    filePath = filePath,
                    message = "Added UnityEngine.InputSystem using directive",
                    success = true
                });

                if (!dryRun)
                {
                    content = "using UnityEngine.InputSystem;\n" + content;
                }
            }

            results.Add(new MigrationResult
            {
                filePath = filePath,
                message = $"Input.GetKeyDown({keyParam}) -> Keyboard.current[{keyParam.Replace("KeyCode.", "Key.")}].wasPressedThisFrame",
                success = true
            });

            if (!dryRun)
            {
                content = content.Replace(match.Value, replacement);
            }
        }

        // Input.GetAxis -> Keyboard.current input
        if (Regex.IsMatch(content, @"Input\.GetAxis\(""Horizontal""\)") || Regex.IsMatch(content, @"Input\.GetAxis\(""Vertical""\)"))
        {
            results.Add(new MigrationResult
            {
                filePath = filePath,
                message = "Input.GetAxis detected - manual migration required for movement input",
                success = false
            });
        }

        return results;
    }

    private List<MigrationResult> ApplyTypeMappingMigration(string filePath, ref string content, bool dryRun)
    {
        var results = new List<MigrationResult>();

        foreach (var mapping in rules.typeMappings)
        {
            if (content.Contains(mapping.legacyType))
            {
                results.Add(new MigrationResult
                {
                    filePath = filePath,
                    message = $"{mapping.legacyType} -> {mapping.newType}",
                    success = true
                });

                if (!dryRun)
                {
                    content = content.Replace(mapping.legacyType, mapping.newType);
                }
            }
        }

        return results;
    }

    private void GenerateApplyReport()
    {
        StringBuilder report = new StringBuilder();

        report.AppendLine("# UI Migration Apply Report");
        report.AppendLine();
        report.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();

        report.AppendLine("## Summary");
        report.AppendLine();
        report.AppendLine($"Total migrations applied: {applyResults.Sum(x => x.Value.Count)}");
        report.AppendLine($"Successful migrations: {applyResults.Sum(x => x.Value.Where(r => r.success).Count())}");
        report.AppendLine($"Manual migrations required: {applyResults.Sum(x => x.Value.Where(r => !r.success).Count())}");
        report.AppendLine();

        foreach (var category in applyResults)
        {
            report.AppendLine($"### {category.Key}");
            report.AppendLine();
            report.AppendLine($"Applied {category.Value.Count} migrations:");
            report.AppendLine();

            foreach (var result in category.Value)
            {
                string status = result.success ? "✅" : "⚠️";
                report.AppendLine($"{status} {result.filePath}: {result.message}");
            }

            report.AppendLine();
        }

        report.AppendLine("## Next Steps");
        report.AppendLine();
        report.AppendLine("1. Review all changes in version control");
        report.AppendLine("2. Test all modified components in Unity editor");
        report.AppendLine("3. Address any manual migration requirements");
        report.AppendLine("4. Run full test suite to ensure functionality");
        report.AppendLine();

        // レポートファイルの保存
        string directory = Path.GetDirectoryName(applyReportPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(applyReportPath, report.ToString());
        AssetDatabase.Refresh();

        Debug.Log($"UI Migration apply report generated: {applyReportPath}");
        EditorUtility.RevealInFinder(applyReportPath);
    }
}

public class MigrationResult
{
    public string filePath;
    public string message;
    public bool success;
}
