#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Vastcore.EditorTools
{
    public static class ProBuilderAdapter
    {
        private static bool Execute(string menuPath, GameObject[] selection)
        {
            if (selection != null && selection.Length > 0)
            {
                Selection.objects = selection;
            }
            return EditorApplication.ExecuteMenuItem(menuPath);
        }

        public static bool Union(GameObject[] selection)
        {
            return Execute("ProBuilder/Geometry/Union", selection);
        }

        public static bool Subtract(GameObject[] selection)
        {
            return Execute("ProBuilder/Geometry/Subtract", selection);
        }

        public static bool Intersect(GameObject[] selection)
        {
            return Execute("ProBuilder/Geometry/Intersect", selection);
        }
    }
}
#endif
