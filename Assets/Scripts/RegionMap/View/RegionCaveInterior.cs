using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.RegionMap
{
    // One chamber. The lower trail runs north under the span, climbs the east bank,
    // crosses west on the span and descends back to the entrance.
    internal sealed class RegionCaveInterior : MonoBehaviour
    {
        internal static readonly Vector3 Threshold = new Vector3(0, 0, -22);
        private static readonly Vector3[] Shelf =
        {
            new Vector3(2, -0.55f, 12), new Vector3(7, 0.9f, 14),
            new Vector3(11.3f, 2.7f, 11), new Vector3(12, 4.9f, 5),
            new Vector3(9.4f, 6.25f, 1.7f), new Vector3(4.5f, 6.6f, 1.4f),
            new Vector3(-1, 6.8f, 2.3f), new Vector3(-6, 6.45f, 2.1f),
            new Vector3(-10.7f, 5.9f, 0.5f), new Vector3(-11.8f, 4.45f, -4),
            new Vector3(-9, 2.6f, -8), new Vector3(-5, 0.85f, -11),
            new Vector3(-2, -0.55f, -13)
        };
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<int> triangles = new List<int>();

        internal static void Build(GameObject root)
        {
            var cave = root.AddComponent<RegionCaveInterior>();
            cave.Chamber();
            cave.RockShelf();
            cave.Formations();
            var mesh = new Mesh { name = "Chamber, crossing and rock banks" };
            mesh.SetVertices(cave.vertices);
            mesh.SetTriangles(cave.triangles, 0);
            mesh.RecalculateNormals();
            var material = new Material(Resources.Load<Shader>("RegionMap3D/CaveStone"));
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = material;
            root.AddComponent<MeshCollider>().sharedMesh = mesh;
            cave.mesh = mesh;
            cave.material = material;
            cave.vertices.Clear();
            cave.triangles.Clear();
        }

        private static float Noise(float x, float z) => Mathf.PerlinNoise(26.1f + x, 19.6f + z) - 0.5f;
        private static float Floor(float x, float z) =>
            -0.25f + 0.38f * Noise(x * 0.23f, z * 0.24f) + 0.25f * Mathf.Sin(z * 0.3f + x * 0.14f);

        private void Quad(int a, int b, int c, int d)
        {
            triangles.Add(a); triangles.Add(b); triangles.Add(c);
            triangles.Add(a); triangles.Add(c); triangles.Add(d);
        }

        private const int FloorSteps = 20, ArchSteps = 48, Slices = 84;
        private const int RingSize = FloorSteps + ArchSteps;

        private void Chamber()
        {
            const int sides = RingSize;
            for (int s = 0; s <= Slices; s++)
            {
                float z = Mathf.Lerp(-26f, 21f, s / (float)Slices);
                float hall = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-22, -11, z));
                float farEnd = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(14, 22, z));
                float width = Mathf.Lerp(2.3f, 16.6f + 1.4f * Mathf.Sin(z * 0.22f), hall) - 8 * farEnd;
                float height = Mathf.Lerp(3.6f, 13.7f + 1.3f * Mathf.Cos(z * 0.34f), hall) - 4 * farEnd;
                float center = 0.9f * Mathf.Sin(z * 0.19f);
                for (int i = 0; i < sides; i++)
                {
                    float x, y;
                    if (i <= FloorSteps)
                    {
                        x = center + width * (2f * i / FloorSteps - 1f);
                        y = Floor(x, z);
                    }
                    else
                    {
                        float angle = (i - FloorSteps) * Mathf.PI / ArchSteps;
                        float sin = Mathf.Sin(angle);
                        float irregular = Noise(i * 0.29f, z * 0.34f) + 1.35f * Noise(i * 0.11f, z * 0.15f);
                        float ledge = Mathf.Sin(sin * height * 3.1f + z * 0.19f);
                        x = center + Mathf.Cos(angle) * (width + sin * (irregular * 2.7f + ledge * 0.45f));
                        y = Floor(x, z) + sin * (height + 2.9f * irregular);
                    }
                    vertices.Add(new Vector3(x, y, z));
                }
                if (s == 0) continue;
                int prev = (s - 1) * sides;
                for (int i = 0; i < sides; i++)
                {
                    int next = (i + 1) % sides;
                    Quad(prev + i, prev + sides + i, prev + sides + next, prev + next);
                }
            }
            // The entrance passage and the far end both terminate in rock.
            for (int i = 1; i < sides - 1; i++)
            {
                triangles.Add(0); triangles.Add(i); triangles.Add(i + 1);
                int end = Slices * sides;
                triangles.Add(end); triangles.Add(end + i + 1); triangles.Add(end + i);
            }
        }

        private void RockShelf()
        {
            int start = vertices.Count;
            int row = 0;
            const int section = 8;
            for (int k = 0; k < Shelf.Length - 1; k++)
            {
                Vector3 a = Shelf[k], b = Shelf[k + 1];
                int steps = Mathf.CeilToInt(Vector3.Distance(a, b));
                for (int j = 0; j <= steps; j++)
                {
                    if (j == 0 && k != 0) continue;
                    float t = j / (float)steps;
                    Vector3 p = Vector3.Lerp(a, b, t);
                    Vector3 tangentA = b - Shelf[Mathf.Max(0, k - 1)];
                    Vector3 tangentB = Shelf[Mathf.Min(Shelf.Length - 1, k + 2)] - a;
                    Vector3 side = Vector3.Cross(Vector3.up, Vector3.Lerp(tangentA.normalized, tangentB.normalized, t)).normalized;
                    float span = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(10.5f, 4.7f, Mathf.Abs(p.x)));
                    float baseY = k >= 4 && k <= 7 ? Mathf.Lerp(-1.2f, p.y - 1.35f, span) : -1.2f;
                    float width = Mathf.Lerp(2.4f, 1.55f, span) + 0.28f * Mathf.Sin(row * 0.58f);
                    float depth = Mathf.Max(0.7f, p.y - baseY);
                    vertices.Add(p - side * width - Vector3.up * 0.15f);
                    vertices.Add(p - side * width * 0.44f);
                    vertices.Add(p + side * width * 0.4f + Vector3.up * 0.06f);
                    vertices.Add(p + side * width - Vector3.up * 0.12f);
                    vertices.Add(p + side * (width + 0.45f) - Vector3.up * depth * 0.43f);
                    vertices.Add(p + side * width * 0.70f - Vector3.up * depth);
                    vertices.Add(p - side * width * 0.48f - Vector3.up * (depth + 0.25f));
                    vertices.Add(p - side * (width + 0.25f) - Vector3.up * depth * 0.55f);
                    if (row > 0)
                    {
                        int prev = start + (row - 1) * section;
                        for (int i = 0; i < section; i++)
                        {
                            int next = (i + 1) % section;
                            Quad(prev + i, prev + section + i, prev + section + next, prev + next);
                        }
                    }
                    row++;
                }
            }
        }

        private void Formations()
        {
            // Broken talus and taller ribs along the walls, leaving both paths clear.
            var random = new System.Random(541);
            float Unit() => (float)random.NextDouble();
            for (int i = 0; i < 105; i++)
            {
                float z = Mathf.Lerp(-12, 16, Unit());
                float x = (i % 2 == 0 ? 1 : -1) * Mathf.Lerp(13.5f, 15.8f, Unit());
                float radius = 0.35f + 1.5f * Unit();
                Spire(new Vector3(x, Floor(x, z) - 0.4f, z), radius, radius * (1.1f + 3.9f * Unit()), Unit() * 6);
            }
            // Pendants cluster beside the crossing where silhouettes mark the height.
            for (int i = 0; i < 65; i++)
            {
                int slice = random.Next(28, 72), arch = random.Next(10, 38);
                Vector3 ceiling = vertices[slice * RingSize + FloorSteps + arch];
                Spire(ceiling + Vector3.up * 0.38f, 0.35f + 0.8f * Unit(), -1.1f - 2.8f * Unit(), Unit() * 6);
            }
            // Scattered scree beside the trail makes the scale of the crossing visible.
            for (int i = 0; i < 95; i++)
            {
                float z = Mathf.Lerp(-14, 13, Unit());
                float x = (i % 2 == 0 ? 1 : -1) * Mathf.Lerp(3.6f, 7.9f, Unit());
                float r = 0.13f + 0.65f * Unit();
                Spire(new Vector3(x, Floor(x, z) - 0.13f, z), r, r * (0.6f + Unit()), Unit() * 6);
            }
        }

        private void Spire(Vector3 basePoint, float radius, float height, float phase)
        {
            int first = vertices.Count;
            const int sides = 7;
            for (int ring = 0; ring < 3; ring++)
            {
                for (int i = 0; i < sides; i++)
                {
                    float angle = phase + i * 2f * Mathf.PI / sides;
                    float r = radius * (1.1f + 0.19f * Mathf.Cos(angle * 3 + phase))
                        * (ring == 0 ? 1f : ring == 1 ? 0.69f : 0.07f);
                    vertices.Add(basePoint + new Vector3(r * Mathf.Cos(angle) + ring * radius * 0.13f,
                        height * (ring == 0 ? 0 : ring == 1 ? 0.55f : 1), r * Mathf.Sin(angle)));
                }
            }
            for (int j = 0; j < 2 * sides; j++)
            {
                int a = first + j, b = first + j / sides * sides + (j + 1) % sides;
                if (height > 0) Quad(a, a + sides, b + sides, b);
                else Quad(a, b, b + sides, a + sides);
            }
        }

        private Mesh mesh;
        private Material material;
        private void OnDestroy()
        {
            Destroy(mesh);
            Destroy(material);
        }
    }
}
