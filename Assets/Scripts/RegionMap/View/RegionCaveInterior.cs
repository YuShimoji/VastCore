using System.Collections.Generic;
using UnityEngine;

namespace Vastcore.RegionMap
{
    // The lower trail runs under the span and returns over the west bank.
    // Passage and chamber modules consume the same ring at each join.
    internal sealed class RegionCaveInterior : MonoBehaviour
    {
        private readonly struct Socket
        {
            internal readonly float Z, Center, HalfWidth, Rise;
            internal Socket(float z, float center, float halfWidth, float rise)
            {
                Z = z;
                Center = center;
                HalfWidth = halfWidth;
                Rise = rise;
            }

            internal Vector3 FloorPoint => new Vector3(Center, Floor(Center, Z), Z);
        }

        internal Vector3 Threshold { get; private set; }
        internal Vector3 Turn { get; private set; }
        internal int PropCount { get; private set; }

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

        internal static RegionCaveInterior Build(GameObject root, int seed)
        {
            var cave = root.AddComponent<RegionCaveInterior>();
            bool deepPassage = (seed & 1) == 0;
            float thresholdZ = deepPassage ? -32f : -22f;
            var sockets = new[]
            {
                new Socket(thresholdZ - 4f, 0f, 2.3f, 3.65f),
                new Socket(thresholdZ, 0f, 2.3f, 3.65f),
                new Socket(deepPassage ? -23f : -17.8f, deepPassage ? 3.35f : -1.4f, 3.35f, 4.4f),
                new Socket(-11f, -0.75f, 16.1f, 12.5f),
                new Socket(2f, 0.3f, 18.0f, 14.3f),
                new Socket(14f, -0.1f, 16.7f, 13.5f),
                new Socket(deepPassage ? 27f : 21f, deepPassage ? -3.1f : 1.2f, deepPassage ? 7.3f : 9.5f, 9.5f),
            };
            cave.Threshold = sockets[1].FloorPoint;
            cave.Turn = sockets[2].FloorPoint;
            int rings = cave.BuildShell(sockets);
            cave.RockShelf();
            cave.Formations(seed, rings);
            var mesh = new Mesh { name = "Connected cave modules, stone span and banks" };
            mesh.SetVertices(cave.vertices);
            mesh.SetTriangles(cave.triangles, 0);
            mesh.RecalculateNormals();
            var material = new Material(Resources.Load<Shader>("RegionMap3D/CaveStone"));
            material.SetFloat("_EntranceZ", sockets[0].Z);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = material;
            root.AddComponent<MeshCollider>().sharedMesh = mesh;
            cave.mesh = mesh;
            cave.material = material;
            cave.vertices.Clear();
            cave.triangles.Clear();
            return cave;
        }

        private static float Noise(float x, float z) => Mathf.PerlinNoise(26.1f + x, 19.6f + z) - 0.5f;
        private static float Floor(float x, float z) =>
            -0.25f + 0.38f * Noise(x * 0.23f, z * 0.24f) + 0.25f * Mathf.Sin(z * 0.3f + x * 0.14f);

        private void Quad(int a, int b, int c, int d)
        {
            triangles.Add(a); triangles.Add(b); triangles.Add(c);
            triangles.Add(a); triangles.Add(c); triangles.Add(d);
        }

        private const int FloorSteps = 20, ArchSteps = 48;
        private const int RingSize = FloorSteps + ArchSteps;

        private int BuildShell(Socket[] sockets)
        {
            AddRing(sockets[0]);
            for (int module = 1; module < sockets.Length; module++)
            {
                Socket from = sockets[module - 1], to = sockets[module];
                int rings = Mathf.CeilToInt((to.Z - from.Z) / 0.55f);
                for (int ring = 1; ring <= rings; ring++)
                {
                    float t = ring / (float)rings;
                    float bend = Mathf.SmoothStep(0f, 1f, t);
                    int prev = vertices.Count - RingSize;
                    AddRing(new Socket(Mathf.Lerp(from.Z, to.Z, t), Mathf.Lerp(from.Center, to.Center, bend),
                        Mathf.Lerp(from.HalfWidth, to.HalfWidth, bend), Mathf.Lerp(from.Rise, to.Rise, bend)));
                    for (int i = 0; i < RingSize; i++)
                    {
                        int next = (i + 1) % RingSize;
                        Quad(prev + i, prev + RingSize + i, prev + RingSize + next, prev + next);
                    }
                }
            }
            int end = vertices.Count - RingSize;
            for (int i = 1; i < RingSize - 1; i++)
            {
                triangles.Add(0); triangles.Add(i); triangles.Add(i + 1);
                triangles.Add(end); triangles.Add(end + i + 1); triangles.Add(end + i);
            }
            return vertices.Count / RingSize;
        }

        private void AddRing(Socket section)
        {
            float z = section.Z;
            for (int i = 0; i <= FloorSteps; i++)
            {
                float x = section.Center + section.HalfWidth * (2f * i / FloorSteps - 1f);
                vertices.Add(new Vector3(x, Floor(x, z), z));
            }
            for (int i = FloorSteps + 1; i < RingSize; i++)
            {
                float angle = (i - FloorSteps) * Mathf.PI / ArchSteps;
                float sin = Mathf.Sin(angle);
                float irregular = Noise(i * 0.29f, z * 0.34f) + 1.35f * Noise(i * 0.11f, z * 0.15f);
                float ledge = Mathf.Sin(sin * section.Rise * 3.1f + z * 0.19f);
                float fracture = Mathf.Pow(Mathf.Abs(Noise(i * 0.18f, z * 0.24f)) * 2f, 3f);
                float x = section.Center + Mathf.Cos(angle) * (section.HalfWidth + sin * (irregular * 2.7f + ledge * 0.53f));
                float y = Floor(x, z) + sin * (section.Rise + 2.9f * irregular - fracture * 0.82f);
                vertices.Add(new Vector3(x, y, z));
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

        private void Formations(int seed, int rings)
        {
            // Rockfall lies along the walls; ceiling fragments stay clear of the crossing.
            var random = new System.Random(unchecked(seed * 15731 + 541));
            float Unit() => (float)random.NextDouble();
            for (int i = 0; i < 68; i++)
            {
                float z = Mathf.Lerp(-9, 14, Unit());
                float x = (i % 2 == 0 ? 1 : -1) * Mathf.Lerp(13.55f, 15.5f, Unit());
                float radius = 0.45f + 1.05f * Unit();
                RockFragment(new Vector3(x, Floor(x, z) - 0.23f, z), radius, radius * (0.6f + 2.05f * Unit()), Unit() * 6, 0.33f);
            }
            for (int i = 0; i < 40; i++)
            {
                int slice = random.Next(1, rings - 1), arch = random.Next(10, 38);
                Vector3 ceiling = vertices[slice * RingSize + FloorSteps + arch];
                if (ceiling.z < -9f || ceiling.z > 16f || ceiling.y < 10.4f) continue;
                RockFragment(ceiling + Vector3.up * 0.38f, 0.45f + 0.76f * Unit(), -0.7f - 1.12f * Unit(), Unit() * 6, 0.49f);
            }
            for (int i = 0; i < 95; i++)
            {
                float z = Mathf.Lerp(-14, 13, Unit());
                float x = (i % 2 == 0 ? 1 : -1) * Mathf.Lerp(3.6f, 7.9f, Unit());
                float r = 0.13f + 0.65f * Unit();
                RockFragment(new Vector3(x, Floor(x, z) - 0.13f, z), r, r * (0.47f + Unit()), Unit() * 6, 0.52f);
            }
        }

        private void RockFragment(Vector3 basePoint, float radius, float height, float phase, float cap)
        {
            PropCount++;
            int first = vertices.Count;
            const int sides = 7;
            for (int ring = 0; ring < 3; ring++)
            {
                for (int i = 0; i < sides; i++)
                {
                    float angle = phase + i * 2f * Mathf.PI / sides;
                    float r = radius * (1.1f + 0.19f * Mathf.Cos(angle * 3 + phase))
                        * (ring == 0 ? 1f : ring == 1 ? 0.78f : cap);
                    vertices.Add(basePoint + new Vector3(r * Mathf.Cos(angle) + ring * radius * 0.13f,
                        height * (ring == 0 ? 0 : ring == 1 ? 0.55f : 1), r * Mathf.Sin(angle) * 0.78f));
                }
            }
            for (int j = 0; j < 2 * sides; j++)
            {
                int a = first + j, b = first + j / sides * sides + (j + 1) % sides;
                if (height > 0) Quad(a, a + sides, b + sides, b);
                else Quad(a, b, b + sides, a + sides);
            }
            int top = first + 2 * sides;
            for (int i = 1; i < sides - 1; i++)
            {
                triangles.Add(top);
                triangles.Add(top + (height > 0 ? i + 1 : i));
                triangles.Add(top + (height > 0 ? i : i + 1));
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
