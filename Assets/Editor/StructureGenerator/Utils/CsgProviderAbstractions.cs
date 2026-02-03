using UnityEngine;

namespace Vastcore.Editor.Generation.Csg
{
    internal enum CsgOperation
    {
        Union,
        Intersect,
        Subtract
    }

    internal interface ICsgProvider
    {
        string Name { get; }
        bool IsAvailable(out string reason);
        bool TryExecute(GameObject lhs, GameObject rhs, CsgOperation operation, out Mesh mesh, out Material[] materials, out string error);
    }
}
