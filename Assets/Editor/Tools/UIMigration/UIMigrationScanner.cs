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
