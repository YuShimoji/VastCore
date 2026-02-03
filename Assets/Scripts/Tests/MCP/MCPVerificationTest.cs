using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Text;

namespace Vastcore.Tests.MCP
{
    /// <summary>
    /// MCP (Model Context Protocol) パッケージの検証テスト
    /// Task 027: MCP Unity Verification
    /// </summary>
    public static class MCPVerificationTest
    {
        private const string TEST_CATEGORY = "MCPVerification";
        private static StringBuilder _logBuilder = new StringBuilder();

        [MenuItem("VastCore/Tests/MCP Verification", priority = 1000)]
        public static void RunVerification()
        {
            _logBuilder.Clear();
            _logBuilder.AppendLine("=== MCP Unity Verification Report ===");
            _logBuilder.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _logBuilder.AppendLine();

            // Test 1: Package presence check
            CheckPackagePresence();

            // Test 2: Assembly check
            CheckAssemblies();

            // Test 3: Type availability check
            CheckMcpTypes();

            // Output results
            string report = _logBuilder.ToString();
            Debug.Log(report);
            
            EditorUtility.DisplayDialog(
                "MCP Verification Complete",
                "Verification complete. Check the console for detailed report.",
                "OK"
            );
        }

        private static void CheckPackagePresence()
        {
            _logBuilder.AppendLine("[Test 1] Package Presence Check");
            _logBuilder.AppendLine("--------------------------------");

            // Check manifest.json
            string manifestPath = "Packages/manifest.json";
            bool manifestExists = System.IO.File.Exists(manifestPath);
            _logBuilder.AppendLine($"manifest.json exists: {manifestExists}");

            if (manifestExists)
            {
                string manifestContent = System.IO.File.ReadAllText(manifestPath);
                bool hasMcpPackage = manifestContent.Contains("com.coplaydev.unity-mcp");
                _logBuilder.AppendLine($"MCP package registered: {hasMcpPackage}");

                if (hasMcpPackage)
                {
                    _logBuilder.AppendLine("  Package ID: com.coplaydev.unity-mcp");
                    _logBuilder.AppendLine("  Source: GitHub (justinpbarnett/unity-mcp)");
                }
            }

            // Check packages-lock.json
            string lockPath = "Packages/packages-lock.json";
            bool lockExists = System.IO.File.Exists(lockPath);
            _logBuilder.AppendLine($"packages-lock.json exists: {lockExists}");

            if (lockExists)
            {
                string lockContent = System.IO.File.ReadAllText(lockPath);
                bool hasLockedMcp = lockContent.Contains("com.coplaydev.unity-mcp");
                _logBuilder.AppendLine($"MCP package resolved: {hasLockedMcp}");
            }

            _logBuilder.AppendLine();
        }

        private static void CheckAssemblies()
        {
            _logBuilder.AppendLine("[Test 2] Assembly Check");
            _logBuilder.AppendLine("--------------------------------");

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            bool foundRuntime = false;
            bool foundEditor = false;

            foreach (var assembly in assemblies)
            {
                string name = assembly.GetName().Name;
                if (name == "MCPForUnity.Runtime")
                {
                    foundRuntime = true;
                    _logBuilder.AppendLine($"Found: {name}");
                    _logBuilder.AppendLine($"  Location: {assembly.Location}");
                    _logBuilder.AppendLine($"  Version: {assembly.GetName().Version}");
                }
                if (name == "MCPForUnity.Editor")
                {
                    foundEditor = true;
                    _logBuilder.AppendLine($"Found: {name}");
                    _logBuilder.AppendLine($"  Location: {assembly.Location}");
                }
            }

            if (!foundRuntime && !foundEditor)
            {
                _logBuilder.AppendLine("WARNING: No MCPForUnity assemblies found in current domain.");
                _logBuilder.AppendLine("  This may indicate:");
                _logBuilder.AppendLine("  - Package is not yet imported (open Unity Editor)");
                _logBuilder.AppendLine("  - Compilation errors prevent assembly loading");
                _logBuilder.AppendLine("  - Package was removed from manifest");
            }

            _logBuilder.AppendLine($"\nRuntime assembly loaded: {foundRuntime}");
            _logBuilder.AppendLine($"Editor assembly loaded: {foundEditor}");
            _logBuilder.AppendLine();
        }

        private static void CheckMcpTypes()
        {
            _logBuilder.AppendLine("[Test 3] MCP Type Availability Check");
            _logBuilder.AppendLine("--------------------------------");

            // Try to find common MCP types
            string[] expectedTypes = new[]
            {
                "MCPForUnity.MCPBridge",
                "MCPForUnity.MCPService",
                "MCPForUnity.IMCPConnection",
                "MCPForUnity.Runtime.MCPBridge"
            };

            int foundCount = 0;
            foreach (string typeName in expectedTypes)
            {
                Type type = Type.GetType(typeName);
                if (type == null)
                {
                    // Try with assembly name
                    type = Type.GetType($"{typeName}, MCPForUnity.Runtime");
                }

                bool found = type != null;
                _logBuilder.AppendLine($"  {typeName}: {(found ? "FOUND" : "NOT FOUND")}");
                if (found) foundCount++;
            }

            _logBuilder.AppendLine($"\nTypes found: {foundCount}/{expectedTypes.Length}");

            if (foundCount == 0)
            {
                _logBuilder.AppendLine("\nNOTE: Types not available in current context.");
                _logBuilder.AppendLine("This is expected if Unity Editor is not running.");
            }

            _logBuilder.AppendLine();
        }

        [MenuItem("VastCore/Tests/MCP Generate Report", priority = 1001)]
        public static void GenerateReport()
        {
            RunVerification();
            
            string report = _logBuilder.ToString();
            string reportPath = "docs/inbox/REPORT_TASK_027_MCP_Verification.md";
            
            string directory = System.IO.Path.GetDirectoryName(reportPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            System.IO.File.WriteAllText(reportPath, report);
            Debug.Log($"Report saved to: {reportPath}");
            
            EditorUtility.RevealInFinder(reportPath);
        }
    }
}
