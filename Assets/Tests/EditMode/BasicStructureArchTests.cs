using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.ProBuilder;
using Vastcore.Editor.Generation;

namespace Vastcore.Tests.EditMode
{
    public class BasicStructureArchTests
    {
        [TestCase(4)]
        [TestCase(16)]
        [TestCase(64)]
        public void Arch_ProducesClosedOutwardMeshWithExpectedBounds(int segments)
        {
            var window = ScriptableObject.CreateInstance<StructureGeneratorWindow>();
            ProBuilderMesh arch = null;
            try
            {
                window.GlobalSettings.GlobalStructureScale = 5f;
                var tab = new BasicStructureTab(window);
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var parameters = typeof(BasicStructureTab).GetField("_archParams", flags).GetValue(tab);
                parameters.GetType().GetField("segments").SetValue(parameters, segments);
                arch = (ProBuilderMesh)typeof(BasicStructureTab)
                    .GetMethod("CreateManualArch", flags).Invoke(tab, null);
                arch.ToMesh();
                arch.Refresh();

                var mesh = arch.GetComponent<MeshFilter>().sharedMesh;
                Assert.AreEqual(4 * segments + 2, arch.faceCount);
                Assert.AreEqual((8 * segments + 4) * 3, mesh.triangles.Length);
                Assert.That(Vector3.Distance(mesh.bounds.size, new Vector3(3f, 4f, 5f)), Is.LessThan(0.0001f));
                Assert.AreEqual(Vector3.one * 5f, arch.transform.localScale);

                // Weld by position so the check also works across ProBuilder UV seams.
                var ids = new Dictionary<Vector3, int>();
                var edges = new Dictionary<(int, int), int>();
                var uses = new Dictionary<(int, int), int>();
                float volume = 0f;
                var vertices = mesh.vertices;
                var triangles = mesh.triangles;
                int Id(Vector3 p)
                {
                    if (!ids.TryGetValue(p, out int id)) ids[p] = id = ids.Count;
                    return id;
                }
                void Edge(int a, int b)
                {
                    var key = a < b ? (a, b) : (b, a);
                    edges.TryGetValue(key, out int balance);
                    edges[key] = balance + (a < b ? 1 : -1);
                    uses.TryGetValue(key, out int count);
                    uses[key] = count + 1;
                }
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
                    Assert.That(Vector3.Cross(b - a, c - a).sqrMagnitude, Is.GreaterThan(0.0000001f), "Degenerate triangle");
                    volume += Vector3.Dot(a, Vector3.Cross(b, c)) / 6f;
                    Edge(Id(a), Id(b)); Edge(Id(b), Id(c)); Edge(Id(c), Id(a));
                }
                foreach (var edge in edges)
                {
                    Assert.AreEqual(2, uses[edge.Key], "Mesh must be closed and manifold");
                    Assert.AreEqual(0, edge.Value, "Adjacent triangles must have consistent winding");
                }
                Assert.That(volume, Is.GreaterThan(0f), "Closed mesh must face outward");
            }
            finally
            {
                if (arch != null) Object.DestroyImmediate(arch.gameObject);
                Object.DestroyImmediate(window);
            }
        }
    }
}
