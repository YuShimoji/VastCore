using UnityEditor;
using UnityEngine;
using Vastcore.Generation;

public static class CompoundArchitectureRetest
{
    [MenuItem("Vastcore/Tests/Run Compound Architecture Retest")]
    public static void RunRetest()
    {
        Debug.Log("[Retest] Compound Architecture generation start");

        var types = new[]
        {
            CompoundArchitecturalGenerator.CompoundArchitecturalType.MultipleBridge,
            CompoundArchitecturalGenerator.CompoundArchitecturalType.CathedralComplex,
            CompoundArchitecturalGenerator.CompoundArchitecturalType.FortressWall,
        };

        foreach (var t in types)
        {
            var p = CompoundArchitecturalGenerator.CompoundArchitecturalParams.Default(t);
            p.position = Vector3.zero + new Vector3(Random.Range(-100, 100), 0, Random.Range(-100, 100));
            p.overallSize = new Vector3(400, 120, 60);
            p.unifiedDecorations = true;
            p.enableConnectingElements = true;

            var go = CompoundArchitecturalGenerator.GenerateCompoundArchitecturalStructure(p);
            if (go == null)
            {
                Debug.LogError($"[Retest] Generation returned null for {t}");
            }
            else
            {
                Debug.Log($"[Retest] Generated {t} -> {go.name}");
            }
        }

        Debug.Log("[Retest] Compound Architecture generation finished");
    }
}
