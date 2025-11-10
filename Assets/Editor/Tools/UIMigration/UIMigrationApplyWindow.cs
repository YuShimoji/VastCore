using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class UIMigrationApplyWindow : EditorWindow
{
    [MenuItem("Vastcore/Tools/UI Migration/Apply Migrations")]
    static void ShowWindow()
    {
        GetWindow<UIMigrationApplyWindow>();
    }

    void OnGUI()
    {
        GUILayout.Label("UI Migration Apply", EditorStyles.boldLabel);
        if (GUILayout.Button("Apply UI Migrations"))
        {
            ApplyMigrations();
        }
    }

    void ApplyMigrations()
    {
        // Apply namespace changes
        string[] filesToMigrate = {
            "Assets/Scripts/UI/MenuManager.cs"
        };

        foreach (string file in filesToMigrate)
        {
            if (File.Exists(file))
            {
                string content = File.ReadAllText(file);
                content = content.Replace("namespace NarrativeGen.UI", "namespace Vastcore.UI");
                File.WriteAllText(file, content);
                Debug.Log("Migrated " + file);
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("UI Migration apply completed.");
    }
}
