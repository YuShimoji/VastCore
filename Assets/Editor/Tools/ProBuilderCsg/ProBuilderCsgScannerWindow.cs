using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Vastcore.EditorTools
{
    public class ProBuilderCsgScannerWindow : EditorWindow
    {
        private const string DefaultOutputRelativePath = "docs/CT1_PROBUILDER_CSG_API_SCAN.md";

        private string _outputRelativePath = DefaultOutputRelativePath;
        private string _typeNameFilter = "Csg";
        private bool _includeNonPublicMembers = false;

        [MenuItem("Tools/Vastcore/Diagnostics/ProBuilder CSG API Scanner")]
        public static void ShowWindow()
        {
            GetWindow<ProBuilderCsgScannerWindow>("ProBuilder CSG Scanner");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Output (relative to project root)");
            _outputRelativePath = EditorGUILayout.TextField(_outputRelativePath);

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Type name filter (case-insensitive)");
            _typeNameFilter = EditorGUILayout.TextField(_typeNameFilter);

            _includeNonPublicMembers = EditorGUILayout.Toggle("Include non-public members", _includeNonPublicMembers);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Scan & Write Report", GUILayout.Height(30)))
            {
                ScanAndWriteReport();
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_outputRelativePath)))
            {
                if (GUILayout.Button("Reveal Report"))
                {
                    string fullPath = GetFullPathFromProjectRoot(_outputRelativePath);
                    if (File.Exists(fullPath))
                    {
                        EditorUtility.RevealInFinder(fullPath);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Report Not Found", fullPath, "OK");
                    }
                }
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Reset Output Path"))
            {
                _outputRelativePath = DefaultOutputRelativePath;
            }
        }

        private void ScanAndWriteReport()
        {
            WriteReport(_outputRelativePath, _typeNameFilter, _includeNonPublicMembers, showDialog: true);
        }

        public static void RunBatch()
        {
            WriteReport(DefaultOutputRelativePath, "Csg", includeNonPublicMembers: false, showDialog: false);
        }

        private static void WriteReport(string outputRelativePath, string typeNameFilter, bool includeNonPublicMembers, bool showDialog)
        {
            string safeOutputRelativePath = string.IsNullOrWhiteSpace(outputRelativePath)
                ? DefaultOutputRelativePath
                : outputRelativePath;

            string projectRoot = GetProjectRootPath();
            string fullPath = GetFullPathFromProjectRoot(safeOutputRelativePath);

            string directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string defines = string.Empty;
            try
            {
                var target = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                defines = PlayerSettings.GetScriptingDefineSymbols(target);
            }
            catch
            {
                defines = "(failed to read define symbols)";
            }

            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => new { Assembly = a, Name = a.GetName().Name ?? string.Empty })
                .Where(x => ContainsIgnoreCase(x.Name, "ProBuilder"))
                .OrderBy(x => x.Name)
                .Select(x => x.Assembly)
                .ToArray();

            var sb = new StringBuilder();
            sb.AppendLine("# CT-1: ProBuilder CSG API Scan Report");
            sb.AppendLine();
            sb.AppendLine($"- Unity: `{Application.unityVersion}`");
            sb.AppendLine($"- Generated: `{DateTime.Now:yyyy-MM-dd HH:mm:ss}`");
            sb.AppendLine("- Project root: `(omitted)`");
            sb.AppendLine();

            sb.AppendLine("## Scripting Define Symbols");
            sb.AppendLine("```");
            sb.AppendLine(defines);
            sb.AppendLine("```");
            sb.AppendLine();

            sb.AppendLine("## ProBuilder-related assemblies (loaded)");
            if (assemblies.Length == 0)
            {
                sb.AppendLine("- (none found)");
            }
            else
            {
                foreach (var asm in assemblies)
                {
                    sb.AppendLine($"- `{asm.GetName().Name}`");
                }
            }
            sb.AppendLine();

            foreach (var asm in assemblies)
            {
                sb.AppendLine($"## Assembly: `{asm.GetName().Name}`");

                Type[] types;
                string typeLoadError = null;

                try
                {
                    types = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types?.Where(t => t != null).ToArray() ?? Array.Empty<Type>();
                    typeLoadError = ex.LoaderExceptions != null
                        ? string.Join("\n", ex.LoaderExceptions.Where(e => e != null).Select(e => e.Message))
                        : "(unknown loader exceptions)";
                }
                catch (Exception ex)
                {
                    types = Array.Empty<Type>();
                    typeLoadError = ex.Message;
                }

                if (!string.IsNullOrWhiteSpace(typeLoadError))
                {
                    sb.AppendLine();
                    sb.AppendLine("**Type load warning:**");
                    sb.AppendLine("```");
                    sb.AppendLine(typeLoadError);
                    sb.AppendLine("```");
                }

                string filter = typeNameFilter ?? string.Empty;

                var matched = types
                    .Where(t => t != null)
                    .Where(t =>
                    {
                        if (string.IsNullOrWhiteSpace(filter)) return true;
                        return ContainsIgnoreCase(t.FullName ?? string.Empty, filter) || ContainsIgnoreCase(t.Name, filter);
                    })
                    .OrderBy(t => t.FullName)
                    .ToArray();

                sb.AppendLine();
                sb.AppendLine($"- Types matched: `{matched.Length}` / `{types.Length}`");

                foreach (var t in matched)
                {
                    sb.AppendLine();
                    sb.AppendLine($"### `{t.FullName}`");
                    sb.AppendLine();
                    sb.AppendLine($"- Public: `{t.IsPublic}`");
                    sb.AppendLine($"- IsAbstract: `{t.IsAbstract}`");
                    sb.AppendLine($"- IsSealed: `{t.IsSealed}`");
                    sb.AppendLine($"- BaseType: `{t.BaseType?.FullName}`");

                    var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
                    if (includeNonPublicMembers)
                    {
                        flags |= BindingFlags.NonPublic;
                    }

                    MethodInfo[] methods;
                    try
                    {
                        methods = t.GetMethods(flags)
                            .Where(m => m != null && !m.IsSpecialName)
                            .OrderBy(m => m.Name)
                            .ThenBy(m => m.GetParameters().Length)
                            .ToArray();
                    }
                    catch
                    {
                        methods = Array.Empty<MethodInfo>();
                    }

                    sb.AppendLine();
                    sb.AppendLine("#### Methods");
                    if (methods.Length == 0)
                    {
                        sb.AppendLine("- (none)");
                    }
                    else
                    {
                        const int maxLines = 200;
                        int count = 0;
                        foreach (var m in methods)
                        {
                            sb.AppendLine($"- `{FormatMethodSignature(m)}`");
                            count++;
                            if (count >= maxLines) break;
                        }
                        if (methods.Length > maxLines)
                        {
                            sb.AppendLine($"- ...(truncated {methods.Length - maxLines} more methods)");
                        }
                    }

                    FieldInfo[] fields;
                    try
                    {
                        fields = t.GetFields(flags)
                            .Where(f => f != null)
                            .OrderBy(f => f.Name)
                            .ToArray();
                    }
                    catch
                    {
                        fields = Array.Empty<FieldInfo>();
                    }

                    sb.AppendLine();
                    sb.AppendLine("#### Fields");
                    if (fields.Length == 0)
                    {
                        sb.AppendLine("- (none)");
                    }
                    else
                    {
                        const int maxLines = 200;
                        int count = 0;
                        foreach (var f in fields)
                        {
                            sb.AppendLine($"- `{FormatFieldSignature(f)}`");
                            count++;
                            if (count >= maxLines) break;
                        }
                        if (fields.Length > maxLines)
                        {
                            sb.AppendLine($"- ...(truncated {fields.Length - maxLines} more fields)");
                        }
                    }
                }

                sb.AppendLine();
            }

            File.WriteAllText(fullPath, sb.ToString(), new UTF8Encoding(false));
            AssetDatabase.Refresh();

            Debug.Log($"[ProBuilderCsgScannerWindow] Report written: {fullPath}");

            if (showDialog && !Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("ProBuilder CSG Scan", $"Report written:\n{fullPath}", "OK");
            }
        }

        private static string GetProjectRootPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string GetFullPathFromProjectRoot(string relativePath)
        {
            string projectRoot = GetProjectRootPath();
            return Path.GetFullPath(Path.Combine(projectRoot, relativePath ?? string.Empty));
        }

        private static bool ContainsIgnoreCase(string source, string value)
        {
            if (source == null || value == null) return false;
            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FormatMethodSignature(MethodInfo method)
        {
            string returnType = method.ReturnType != null ? method.ReturnType.FullName : "void";
            string staticPrefix = method.IsStatic ? "static " : string.Empty;
            var parameters = method.GetParameters();
            string parameterText = string.Join(", ", parameters.Select(p => $"{(p.ParameterType != null ? p.ParameterType.FullName : "unknown")} {p.Name}"));
            return $"{staticPrefix}{returnType} {method.Name}({parameterText})";
        }

        private static string FormatFieldSignature(FieldInfo field)
        {
            string fieldType = field.FieldType != null ? field.FieldType.FullName : "unknown";
            string staticPrefix = field.IsStatic ? "static " : string.Empty;
            return $"{staticPrefix}{fieldType} {field.Name}";
        }
    }
}
