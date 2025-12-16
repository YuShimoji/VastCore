using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Vastcore.Editor.Generation.Csg
{
    internal static class CsgReflectionExtraction
    {
        private static readonly BindingFlags AnyStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        private static readonly BindingFlags AnyInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public static bool TryExtractMesh(object model, out Mesh mesh)
        {
            mesh = null;
            if (model == null) return false;

            if (model is Mesh directMesh)
            {
                mesh = directMesh;
                return true;
            }

            var t = model.GetType();

            if (TryExtractMeshViaConversionOperator(t, model, out mesh))
            {
                return true;
            }

            var meshProp = t.GetProperties(AnyInstance)
                .FirstOrDefault(p => p.CanRead && typeof(Mesh).IsAssignableFrom(p.PropertyType));
            if (meshProp != null)
            {
                mesh = meshProp.GetValue(model) as Mesh;
                if (mesh != null) return true;
            }

            var meshField = t.GetFields(AnyInstance)
                .FirstOrDefault(f => typeof(Mesh).IsAssignableFrom(f.FieldType));
            if (meshField != null)
            {
                mesh = meshField.GetValue(model) as Mesh;
                if (mesh != null) return true;
            }

            var meshMethod = t.GetMethods(AnyInstance)
                .FirstOrDefault(m => m.GetParameters().Length == 0
                                     && typeof(Mesh).IsAssignableFrom(m.ReturnType)
                                     && ContainsIgnoreCase(m.Name, "mesh"));
            if (meshMethod != null)
            {
                mesh = meshMethod.Invoke(model, null) as Mesh;
                if (mesh != null) return true;
            }

            return false;
        }

        public static bool TryExtractMaterials(object model, out Material[] materials)
        {
            materials = Array.Empty<Material>();
            if (model == null) return false;

            var t = model.GetType();
            object value = null;

            var prop = t.GetProperties(AnyInstance)
                .FirstOrDefault(p => p.CanRead && IsMaterialCollectionType(p.PropertyType));
            if (prop != null)
            {
                value = prop.GetValue(model);
            }
            else
            {
                var field = t.GetFields(AnyInstance)
                    .FirstOrDefault(f => IsMaterialCollectionType(f.FieldType));
                if (field != null)
                {
                    value = field.GetValue(model);
                }
            }

            if (value == null) return false;

            if (value is Material[] matsArray)
            {
                materials = matsArray;
                return true;
            }

            if (value is IEnumerable enumerable)
            {
                materials = enumerable
                    .Cast<object>()
                    .OfType<Material>()
                    .ToArray();
                return true;
            }

            return false;
        }

        public static string DumpModelSummary(object model)
        {
            if (model == null) return "(null)";

            try
            {
                var t = model.GetType();
                var members = t.GetMembers(AnyInstance)
                    .Where(m => ContainsIgnoreCase(m.Name, "mesh") || ContainsIgnoreCase(m.Name, "material"))
                    .Select(m => $"- {m.MemberType} {m.Name}")
                    .Take(40)
                    .ToArray();

                return members.Length == 0
                    ? "(no Mesh/Material-like members found)"
                    : string.Join("\n", members);
            }
            catch
            {
                return "(failed to dump model members)";
            }
        }

        private static bool TryExtractMeshViaConversionOperator(Type modelType, object model, out Mesh mesh)
        {
            mesh = null;

            try
            {
                var methods = modelType.GetMethods(AnyStatic)
                    .Where(m => (m.Name == "op_Implicit" || m.Name == "op_Explicit")
                                && typeof(Mesh).IsAssignableFrom(m.ReturnType)
                                && m.GetParameters().Length == 1
                                && m.GetParameters()[0].ParameterType.IsAssignableFrom(modelType))
                    .ToArray();

                foreach (var m in methods)
                {
                    var result = m.Invoke(null, new[] { model }) as Mesh;
                    if (result != null)
                    {
                        mesh = result;
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static bool IsMaterialCollectionType(Type t)
        {
            if (t == typeof(Material[])) return true;

            if (typeof(IEnumerable).IsAssignableFrom(t))
            {
                if (t.IsGenericType)
                {
                    var args = t.GetGenericArguments();
                    if (args.Length == 1 && args[0] == typeof(Material)) return true;
                }

                if (ContainsIgnoreCase(t.Name, "material")) return true;
            }

            return false;
        }

        private static bool ContainsIgnoreCase(string haystack, string needle)
        {
            return haystack?.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
