<<<<<<< HEAD
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class UIMigrationScannerWindow : EditorWindow
{
    [MenuItem("Vastcore/Tools/UI Migration/Scan (Dry Run)")]
    static void ShowWindow()
    {
        GetWindow<UIMigrationScannerWindow>();
    }

    void OnGUI()
    {
        GUILayout.Label("UI Migration Scanner", EditorStyles.boldLabel);
        if (GUILayout.Button("Scan for Legacy UI References"))
        {
            ScanLegacyUI();
        }
    }

    void ScanLegacyUI()
    {
        string reportPath = "Documentation/QA/LEGACY_UI_MIGRATION_REPORT.md";
        StringBuilder report = new StringBuilder();

        report.AppendLine("# LEGACY UI MIGRATION REPORT");
        report.AppendLine();
        report.AppendLine("## Scan Timestamp");
        report.AppendLine(System.DateTime.Now.ToString());
        report.AppendLine();

        // Scan scripts for NarrativeGen.UI
        string[] scriptFiles = Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories);
        List<string> findings = new List<string>();

        foreach (string file in scriptFiles)
        {
            string content = File.ReadAllText(file);
            if (content.Contains("NarrativeGen.UI"))
            {
                findings.Add(file);
            }
        }

        report.AppendLine("## Summary");
        report.AppendLine($"- Total legacy UI references found: {findings.Count}");
        report.AppendLine($"- Files scanned: {scriptFiles.Length}");
        report.AppendLine();

        report.AppendLine("## Detailed Findings");
        if (findings.Count > 0)
        {
            foreach (string file in findings)
            {
                report.AppendLine($"- {file}");
            }
        }
        else
        {
            report.AppendLine("- None");
        }

        File.WriteAllText(reportPath, report.ToString());
        Debug.Log("UI Migration scan completed. Report saved to " + reportPath);
    }
}
=======
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Vastcore.EditorTools.UIMigration
{
    [InitializeOnLoad]
    public static class UIMigrationScanner
    {
        private const string AutoRunSessionKey = "Vastcore.UIMigration.AutoRunDone";
        private const string AutoRunFlagRelative = "Assets/Editor/Tools/UIMigration/AUTO_RUN_UI_MIGRATION_SCAN.txt";

        static UIMigrationScanner()
        {
            // Delay to ensure Unity is initialized
            EditorApplication.delayCall += TryAutoRun;
        }

        private static void TryAutoRun()
        {
            if (SessionState.GetBool(AutoRunSessionKey, false)) return;

            var root = Directory.GetParent(Application.dataPath);
            var flagPath = AutoRunFlagRelative.Replace("\\", "/");
            var abs = Path.Combine(root != null ? root.FullName : Application.dataPath, flagPath).Replace("\\", "/");
            if (File.Exists(abs))
            {
                Debug.Log("[UI Migration] AUTO_RUN flag detected. Running dry scan...");
                UIMigrationScannerWindow.RunDryScanAndGenerateReport();
                SessionState.SetBool(AutoRunSessionKey, true);
                Debug.Log("[UI Migration] Dry scan completed.");
            }
        }

        [MenuItem("Vastcore/Tools/UI Migration/Run Auto-Scan Now")] 
        public static void RunNow()
        {
            UIMigrationScannerWindow.RunDryScanAndGenerateReport();
        }
    }
}
>>>>>>> origin/develop
