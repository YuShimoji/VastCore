using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Vastcore.EditorTools
{
    internal static class ProBuilderInternalCsgReflection
    {
        private const string CsgAssemblyName = "Unity.ProBuilder.Csg";
        private const string CsgTypeFullName = "UnityEngine.ProBuilder.Csg.CSG";

        private static readonly BindingFlags AnyStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        private static readonly BindingFlags AnyInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        public static bool TryUnion(GameObject lhs, GameObject rhs, out Mesh mesh, out Material[] materials, out string error)
        {
            return TryExecuteBinaryOperation("Union", lhs, rhs, out mesh, out materials, out error);
        }

        public static bool TrySubtract(GameObject lhs, GameObject rhs, out Mesh mesh, out Material[] materials, out string error)
        {
            return TryExecuteBinaryOperation("Subtract", lhs, rhs, out mesh, out materials, out error);
        }

        public static bool TryIntersect(GameObject lhs, GameObject rhs, out Mesh mesh, out Material[] materials, out string error)
        {
            return TryExecuteBinaryOperation("Intersect", lhs, rhs, out mesh, out materials, out error);
        }

        private static bool TryExecuteBinaryOperation(string methodName, GameObject lhs, GameObject rhs, out Mesh mesh, out Material[] materials, out string error)
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

            if (!TryExtractMesh(model, out mesh))
            {
                error = $"Failed to extract Mesh from model type: {model.GetType().FullName}\n{DumpModelSummary(model)}";
                return false;
            }

            TryExtractMaterials(model, out materials);
            return true;
        }

        private static bool TryGetCsgType(out Type csgType, out string error)
        {
            csgType = null;
            error = string.Empty;

            Assembly csgAssembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, CsgAssemblyName, StringComparison.Ordinal));

            if (csgAssembly == null)
            {
                error = $"Assembly not loaded: {CsgAssemblyName}. ProBuilder CSG package may be missing or Unity needs restart.";
                return false;
            }

            csgType = csgAssembly.GetType(CsgTypeFullName, throwOnError: false);
            if (csgType == null)
            {
                error = $"Type not found: {CsgTypeFullName} in {CsgAssemblyName}.";
                return false;
            }

            return true;
        }

        private static bool TryExtractMesh(object model, out Mesh mesh)
        {
            mesh = null;

            if (model is Mesh directMesh)
            {
                mesh = directMesh;
                return true;
            }

            Type t = model.GetType();

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
                .FirstOrDefault(m => m.GetParameters().Length == 0 && typeof(Mesh).IsAssignableFrom(m.ReturnType) && ContainsIgnoreCase(m.Name, "mesh"));
            if (meshMethod != null)
            {
                mesh = meshMethod.Invoke(model, null) as Mesh;
                if (mesh != null) return true;
            }

            return false;
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

        private static bool TryExtractMaterials(object model, out Material[] materials)
        {
            materials = Array.Empty<Material>();
            if (model == null) return false;

            Type t = model.GetType();

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

        private static string DumpModelSummary(object model)
        {
            try
            {
                Type t = model.GetType();
                var meshMembers = t.GetMembers(AnyInstance)
                    .Where(m => ContainsIgnoreCase(m.Name, "mesh") || ContainsIgnoreCase(m.Name, "material"))
                    .Select(m => $"- {m.MemberType} {m.Name}")
                    .Take(40)
                    .ToArray();

                return meshMembers.Length == 0
                    ? "(no Mesh/Material-like members found)"
                    : string.Join("\n", meshMembers);
            }
            catch
            {
                return "(failed to dump model members)";
            }
        }

        private static bool ContainsIgnoreCase(string haystack, string needle)
        {
            return haystack?.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    internal static class ProBuilderInternalCsgPoC
    {
        private const string MenuRoot = "Tools/Vastcore/Diagnostics/ProBuilder Internal CSG PoC/";
        private const string NamePrefix = "PBInternalCSG_";

        public static void RunBatchUnion()
        {
            RunBatch("Union");
        }

        public static void RunBatchSubtract()
        {
            RunBatch("Subtract");
        }

        public static void RunBatchIntersect()
        {
            RunBatch("Intersect");
        }

        [MenuItem(MenuRoot + "Test Union")]
        private static void TestUnion()
        {
            RunTest("Union");
        }

        [MenuItem(MenuRoot + "Test Subtract")]
        private static void TestSubtract()
        {
            RunTest("Subtract");
        }

        [MenuItem(MenuRoot + "Test Intersect")]
        private static void TestIntersect()
        {
            RunTest("Intersect");
        }

        [MenuItem(MenuRoot + "Clean Test Objects")]
        private static void CleanTestObjects()
        {
            CleanTestObjectsInternal(useUndo: true);
        }

        private static void RunTest(string op)
        {
            CleanTestObjectsInternal(useUndo: true);

            var a = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);

            Undo.RegisterCreatedObjectUndo(a, $"{NamePrefix}{op}_CreateA");
            Undo.RegisterCreatedObjectUndo(b, $"{NamePrefix}{op}_CreateB");

            a.name = $"{NamePrefix}{op}_A";
            b.name = $"{NamePrefix}{op}_B";

            a.transform.position = new Vector3(-0.25f, 0f, 0f);
            b.transform.position = new Vector3(0.25f, 0f, 0f);

            EnsureMeshComponents(a);
            EnsureMeshComponents(b);

            bool ok;
            Mesh mesh;
            Material[] materials;
            string error;

            switch (op)
            {
                case "Union":
                    ok = ProBuilderInternalCsgReflection.TryUnion(a, b, out mesh, out materials, out error);
                    break;
                case "Subtract":
                    ok = ProBuilderInternalCsgReflection.TrySubtract(a, b, out mesh, out materials, out error);
                    break;
                case "Intersect":
                    ok = ProBuilderInternalCsgReflection.TryIntersect(a, b, out mesh, out materials, out error);
                    break;
                default:
                    EditorUtility.DisplayDialog("Invalid Operation", op, "OK");
                    return;
            }

            if (!ok)
            {
                Debug.LogError($"[{nameof(ProBuilderInternalCsgPoC)}] {op} failed: {error}");
                EditorUtility.DisplayDialog($"ProBuilder Internal CSG {op} Failed", error, "OK");
                return;
            }

            var result = new GameObject($"{NamePrefix}{op}_Result");
            Undo.RegisterCreatedObjectUndo(result, $"{NamePrefix}{op}_CreateResult");

            var mf = result.AddComponent<MeshFilter>();
            var mr = result.AddComponent<MeshRenderer>();

            mf.sharedMesh = mesh != null ? UnityEngine.Object.Instantiate(mesh) : null;
            if (materials != null && materials.Length > 0)
            {
                mr.sharedMaterials = materials;
            }

            result.transform.position = new Vector3(3f, 0f, 0f);

            Selection.activeGameObject = result;
            var resultMesh = mf.sharedMesh;
            Debug.Log($"[{nameof(ProBuilderInternalCsgPoC)}] {op} succeeded. Result mesh: {resultMesh?.name ?? "(null)"} (verts: {resultMesh?.vertexCount ?? 0})");
        }

        private static void RunBatch(string op)
        {
            try
            {
                CleanTestObjectsInternal(useUndo: false);

                var a = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);

                a.name = $"{NamePrefix}{op}_A";
                b.name = $"{NamePrefix}{op}_B";

                a.transform.position = new Vector3(-0.25f, 0f, 0f);
                b.transform.position = new Vector3(0.25f, 0f, 0f);

                EnsureMeshComponents(a);
                EnsureMeshComponents(b);

                bool ok;
                Mesh mesh;
                Material[] materials;
                string error;

                switch (op)
                {
                    case "Union":
                        ok = ProBuilderInternalCsgReflection.TryUnion(a, b, out mesh, out materials, out error);
                        break;
                    case "Subtract":
                        ok = ProBuilderInternalCsgReflection.TrySubtract(a, b, out mesh, out materials, out error);
                        break;
                    case "Intersect":
                        ok = ProBuilderInternalCsgReflection.TryIntersect(a, b, out mesh, out materials, out error);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(op), op, "Unsupported operation");
                }

                if (!ok)
                {
                    throw new InvalidOperationException($"{op} failed: {error}");
                }

                var result = new GameObject($"{NamePrefix}{op}_Result");
                var mf = result.AddComponent<MeshFilter>();
                var mr = result.AddComponent<MeshRenderer>();
                mf.sharedMesh = mesh != null ? UnityEngine.Object.Instantiate(mesh) : null;
                if (materials != null && materials.Length > 0)
                {
                    mr.sharedMaterials = materials;
                }

                Debug.Log($"[{nameof(ProBuilderInternalCsgPoC)}] Batch {op} succeeded. Result mesh verts: {mf.sharedMesh?.vertexCount ?? 0}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(ProBuilderInternalCsgPoC)}] Batch {op} failed: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private static void CleanTestObjectsInternal(bool useUndo)
        {
            var targets = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(go => go != null && go.name.StartsWith(NamePrefix, StringComparison.Ordinal))
                .ToArray();

            foreach (var go in targets)
            {
                if (useUndo)
                {
                    Undo.DestroyObjectImmediate(go);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            Debug.Log($"[{nameof(ProBuilderInternalCsgPoC)}] Cleaned {targets.Length} objects. (useUndo={useUndo})");
        }

        private static void EnsureMeshComponents(GameObject go)
        {
            if (go == null) return;

            var mf = go.GetComponent<MeshFilter>();
            var mr = go.GetComponent<MeshRenderer>();

            if (mf == null) mf = go.AddComponent<MeshFilter>();
            if (mr == null) mr = go.AddComponent<MeshRenderer>();

            if (mr.sharedMaterial == null)
            {
                mr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            }
        }
    }
}
