using UnityEngine;
// using Vastcore.Diagnostics; // Now part of Vastcore.Utils

namespace Vastcore.Utils
{
    public static class MeshCombineHelper
    {
        // 子階層の MeshFilter を収集し、CombineMeshes を実行して MeshCollider に設定
        public static void CombineChildrenToCollider(GameObject parent, MeshCollider collider, string label)
        {
            if (parent == null || collider == null)
            {
                Debug.LogError("MeshCombineHelper.CombineChildrenToCollider: parent or collider is null");
                return;
            }

            var meshFilters = parent.GetComponentsInChildren<MeshFilter>();
            if (meshFilters == null || meshFilters.Length == 0)
            {
                Debug.LogWarning("MeshCombineHelper.CombineChildrenToCollider: no MeshFilters found");
                return;
            }

            var combines = new CombineInstance[meshFilters.Length];
            int validCount = 0;
            for (int i = 0; i < meshFilters.Length; i++)
            {
                var mf = meshFilters[i];
                if (mf != null && mf.sharedMesh != null)
                {
                    combines[validCount].mesh = mf.sharedMesh;
                    combines[validCount].transform = mf.transform.localToWorldMatrix;
                    validCount++;
                }
            }

            if (validCount == 0)
            {
                Debug.LogWarning("MeshCombineHelper.CombineChildrenToCollider: no valid meshes to combine");
                return;
            }

            // 必要量に縮小
            if (validCount != combines.Length)
            {
                System.Array.Resize(ref combines, validCount);
            }

            var combinedMesh = new Mesh();
            using (LoadProfiler.Measure($"Mesh.CombineMeshes ({label})"))
            {
                combinedMesh.CombineMeshes(combines);
            }
            collider.sharedMesh = combinedMesh;
        }
    }
}
