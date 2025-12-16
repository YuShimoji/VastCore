using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Vastcore.Editor.Generation.Csg
{
    internal sealed class ParaboxCsgProvider : ICsgProvider
    {
        private const string AssemblyName = "Parabox.CSG";
        private const string TypeFullName = "Parabox.CSG.CSG";

        private static readonly BindingFlags AnyStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        public string Name => "Parabox.CSG";

        public bool IsAvailable(out string reason)
        {
            reason = string.Empty;

            var asm = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, AssemblyName, StringComparison.Ordinal));

            if (asm == null)
            {
                reason = $"Assembly not loaded: {AssemblyName}";
                return false;
            }

            var type = asm.GetType(TypeFullName, throwOnError: false);
            if (type == null)
            {
                reason = $"Type not found: {TypeFullName}";
                return false;
            }

            return true;
        }

        public bool TryExecute(GameObject lhs, GameObject rhs, CsgOperation operation, out Mesh mesh, out Material[] materials, out string error)
        {
            mesh = null;
            materials = Array.Empty<Material>();
            error = string.Empty;

            if (lhs == null || rhs == null)
            {
                error = "lhs/rhs is null.";
                return false;
            }

            if (!TryGetCsgType(out var csgType, out error))
            {
                return false;
            }

            var methodName = operation switch
            {
                CsgOperation.Union => "Union",
                CsgOperation.Intersect => "Intersect",
                CsgOperation.Subtract => "Subtract",
                _ => null
            };

            if (string.IsNullOrWhiteSpace(methodName))
            {
                error = $"Unsupported operation: {operation}";
                return false;
            }

            MethodInfo method = csgType.GetMethod(methodName, AnyStatic, binder: null, types: new[] { typeof(GameObject), typeof(GameObject) }, modifiers: null);
            if (method == null)
            {
                error = $"CSG method not found: {csgType.FullName}.{methodName}(GameObject, GameObject)";
                return false;
            }

            object model;
            try
            {
                model = method.Invoke(null, new object[] { lhs, rhs });
            }
            catch (TargetInvocationException tie)
            {
                error = tie.InnerException != null
                    ? $"CSG invoke failed: {tie.InnerException.GetType().Name}: {tie.InnerException.Message}"
                    : $"CSG invoke failed: {tie.GetType().Name}: {tie.Message}";
                return false;
            }
            catch (Exception ex)
            {
                error = $"CSG invoke failed: {ex.GetType().Name}: {ex.Message}";
                return false;
            }

            if (model == null)
            {
                error = "CSG returned null.";
                return false;
            }

            if (!CsgReflectionExtraction.TryExtractMesh(model, out mesh))
            {
                error = $"Failed to extract Mesh from model type: {model.GetType().FullName}\n{CsgReflectionExtraction.DumpModelSummary(model)}";
                return false;
            }

            CsgReflectionExtraction.TryExtractMaterials(model, out materials);
            return true;
        }

        private static bool TryGetCsgType(out Type csgType, out string error)
        {
            csgType = null;
            error = string.Empty;

            var asm = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, AssemblyName, StringComparison.Ordinal));

            if (asm == null)
            {
                error = $"Assembly not loaded: {AssemblyName}";
                return false;
            }

            csgType = asm.GetType(TypeFullName, throwOnError: false);
            if (csgType == null)
            {
                error = $"Type not found: {TypeFullName}";
                return false;
            }

            return true;
        }
    }
}
