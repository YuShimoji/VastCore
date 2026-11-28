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
        // Cross-section around the walkable tread, clockwise from the entrance-facing lip.
        // X is a fraction of the shelf width, Y is a fraction of the rock thickness.
        private static readonly Vector2[] ShelfProfile =
        {
            new Vector2(-1.00f, 0.08f), new Vector2(-0.78f, 0.025f),
            new Vector2(-0.45f, 0.002f), new Vector2(0, 0),
            new Vector2(0.45f, -0.008f), new Vector2(0.76f, 0.015f),
            new Vector2(0.98f, 0.06f), new Vector2(1.04f, 0.21f),
            new Vector2(0.90f, 0.40f), new Vector2(0.96f, 0.61f),
            new Vector2(0.77f, 0.83f), new Vector2(0.37f, 0.98f),
            new Vector2(-0.17f, 1.06f), new Vector2(-0.62f, 0.97f),
            new Vector2(-0.98f, 0.67f), new Vector2(-0.91f, 0.36f)
        };
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<int> triangles = new List<int>();
        private bool refineRockfall;
        private const float FallX = 6.2f, FallZ = -3.5f;
        private const float FallStrike = 31f * Mathf.Deg2Rad;

        internal static RegionCaveInterior Build(GameObject root, int seed, bool refine)
        {
            var cave = root.AddComponent<RegionCaveInterior>();
            cave.refineRockfall = refine;
            bool bedrockChamber = seed == 9;
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
            int rings = cave.BuildShell(sockets, bedrockChamber);
            int shellEnd = cave.triangles.Count;
            cave.RockShelf(bedrockChamber);
            if (bedrockChamber)
            {
                cave.BedrockSpur(new Vector3(10.1f, 4.53f, 0.33f), new Vector3(18.2f, 7.72f, -0.55f), 3.65f);
                cave.BedrockSpur(new Vector3(-9.2f, 4.36f, 1.40f), new Vector3(-18.25f, 7.1f, 0.32f), 2.95f);
                int shelfEnd = cave.triangles.Count;
                cave.BlendShelfIntoWall(shellEnd, shelfEnd, 1, -4.7f, 9.2f);
                cave.BlendShelfIntoWall(shellEnd, shelfEnd, -1, -8.6f, 4.6f);
            }
            cave.Formations(seed, rings, bedrockChamber);
            if (bedrockChamber) cave.ShelfScree(seed);
            int fallStart = cave.vertices.Count;
            if (refine) cave.FallenRock(seed);
            var mesh = new Mesh { name = "Connected cave modules, stone span and banks" };
            mesh.SetVertices(cave.vertices);
            mesh.SetTriangles(cave.triangles, 0);
            mesh.RecalculateNormals();
            var material = new Material(Resources.Load<Shader>("RegionMap3D/CaveStone"));
            material.SetFloat("_EntranceZ", sockets[0].Z);
            if (bedrockChamber)
            {
                material.SetFloat("_BedrockDetail", 1f);
                material.SetTexture("_CliffPhoto", Resources.Load<Texture2D>("RegionMap3D/CaveMouth/CliffColor"));
                material.SetTexture("_CliffNormal", Resources.Load<Texture2D>("RegionMap3D/CaveMouth/CliffNormal"));
                material.SetTexture("_CaveGravel", Resources.Load<Texture2D>(
                    "MapGenerator/Textures/Ground081_1K-JPG/Ground081_1K-JPG_Color"));
            }
            if (refine)
            {
                mesh.SetColors(cave.FreshFaces(rings * RingSize, fallStart));
                material.SetFloat("_FreshCollapse", 1f);
                if (deepPassage)
                    material.SetVector("_ReturnBounce", new Vector4(0.52f, 1.25f, cave.Turn.z + 0.65f, 0.92f));
            }
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

        private int BuildShell(Socket[] sockets, bool bedrock)
        {
            AddRing(sockets[0], bedrock);
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
                        Mathf.Lerp(from.HalfWidth, to.HalfWidth, bend), Mathf.Lerp(from.Rise, to.Rise, bend)), bedrock);
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

        private void AddRing(Socket section, bool bedrock)
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
                float cos = Mathf.Cos(angle);
                float irregular = Noise(i * 0.29f, z * 0.34f) + 1.35f * Noise(i * 0.11f, z * 0.15f);
                float ledge = Mathf.Sin(sin * section.Rise * 3.1f + z * 0.19f);
                float fracture = Mathf.Pow(Mathf.Abs(Noise(i * 0.18f, z * 0.24f)) * 2f, 3f);
                float x = section.Center + cos * (section.HalfWidth + sin * (irregular * 2.7f + ledge * 0.53f));
                if (bedrock)
                {
                    // These two spurs share the height of the shoulders on the span.
                    float along = Mathf.Abs(z - (cos > 0f ? 2.6f : 0.15f));
                    float local = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.0f, 6.4f, along));
                    float band = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.075f, 0.36f, sin))
                        * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.78f, 0.49f, sin));
                    x -= Mathf.Sign(cos) * local * band * (2.25f + 1.30f * irregular);
                }
                float y = Floor(x, z) + sin * (section.Rise + 2.9f * irregular - fracture * 0.82f);
                if (refineRockfall && y > 8.5f)
                    y += 2.25f * Scar(x, z);
                vertices.Add(new Vector3(x, y, z));
            }
        }

        private void RockShelf(bool bedrock)
        {
            int start = vertices.Count;
            int row = 0;
            int section = bedrock ? ShelfProfile.Length : 8;
            for (int k = 0; k < Shelf.Length - 1; k++)
            {
                Vector3 a = Shelf[k], b = Shelf[k + 1];
                int steps = Mathf.CeilToInt(Vector3.Distance(a, b) / (bedrock ? 0.34f : 1f));
                for (int j = 0; j <= steps; j++)
                {
                    if (j == 0 && k != 0) continue;
                    float t = j / (float)steps;
                    Vector3 p = Vector3.Lerp(a, b, t);
                    Vector3 tangentA = b - Shelf[Mathf.Max(0, k - 1)];
                    Vector3 tangentB = Shelf[Mathf.Min(Shelf.Length - 1, k + 2)] - a;
                    Vector3 tangent = Vector3.Lerp(tangentA.normalized, tangentB.normalized, t);
                    if (bedrock)
                    {
                        // A continuous tangent around the bends removes pinched offset edges.
                        tangentA *= 0.4f;
                        tangentB *= 0.4f;
                        float t2 = t * t;
                        p = Vector3.Lerp(a, b, t2 * (3f - 2f * t))
                            + (t * (1f - t) * (1f - t)) * tangentA - (t2 * (1f - t)) * tangentB;
                        tangent = 6f * t * (1f - t) * (b - a)
                            + (3f * t2 - 4f * t + 1f) * tangentA + (3f * t2 - 2f * t) * tangentB;
                    }
                    Vector3 side = Vector3.Cross(Vector3.up, tangent).normalized;
                    float span = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(10.5f, 4.7f, Mathf.Abs(p.x)));
                    if (bedrock) AddBedrockSection(p, side, span);
                    else
                    {
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
                    }
                    if (row > 0)
                        JoinSections(start + (row - 1) * section, section);
                    row++;
                }
            }
        }

        private void JoinSections(int previous, int section)
        {
            for (int i = 0; i < section; i++)
            {
                int next = (i + 1) % section;
                Quad(previous + i, previous + section + i, previous + section + next, previous + next);
            }
        }

        private void BedrockSpur(Vector3 inner, Vector3 wall, float wallWidth)
        {
            // The small end is buried in the span and the broad end in the cave wall.
            int section = ShelfProfile.Length;
            int start = vertices.Count;
            int steps = Mathf.CeilToInt(Mathf.Abs(wall.x - inner.x) / 0.38f);
            float side = -Mathf.Sign(wall.x - inner.x);
            for (int row = 0; row <= steps; row++)
            {
                float t = row / (float)steps;
                Vector3 p = Vector3.Lerp(inner, wall, t);
                float flare = Mathf.SmoothStep(0f, 1f, t);
                float width = Mathf.Lerp(0.26f, wallWidth, flare);
                float thickness = Mathf.Lerp(0.74f, p.y + 1.36f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.58f)));
                foreach (Vector2 cut in ShelfProfile)
                {
                    float z = p.z + side * width * cut.x - 0.52f * Mathf.Sin(t * Mathf.PI);
                    float grain = Noise(p.x * 0.62f + 2.1f * cut.y, z * 0.73f - 6.8f);
                    float brokenBed = Mathf.SmoothStep(-0.19f, 0.20f, grain) - 0.5f;
                    z += flare * (0.56f * brokenBed + 0.26f * cut.y);
                    float y = p.y - cut.y * thickness + flare * (0.44f * brokenBed + 0.3f * grain);
                    vertices.Add(new Vector3(p.x, y, z));
                }
                if (row > 0) JoinSections(start + (row - 1) * section, section);
            }
        }

        private void AddBedrockSection(Vector3 p, Vector3 side, float span)
        {
            float distance = Mathf.Abs(p.x);
            float wallward = Mathf.Sign(p.x);
            float shoulder = p.x < 0f ? 9.6f : 11.5f;
            float arch = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(shoulder, 2.9f, distance))
                * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5.2f, 6.35f, p.y));
            float relief = Noise(1.3f + p.x * 0.39f, p.z * 0.52f);
            float width = Mathf.Lerp(2.38f, 1.78f, span) + 0.45f * relief;
            float thickness = Mathf.Lerp(Mathf.Max(0.68f, p.y + 1.2f), 1.88f + 0.90f * relief, arch);
            float bank = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6.5f, 10.7f, distance))
                * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.1f, 4.1f, p.y));
            float highShelf = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5.5f, 6.2f, p.y));
            foreach (Vector2 cut in ShelfProfile)
            {
                float u = cut.x * width;
                float v = cut.y;
                Vector3 at = p + side * u;
                float outer = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.48f, 0.94f, Mathf.Abs(cut.x)));
                float wallSide = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.22f, 0.62f, cut.x * side.x * wallward));
                float grain = Noise(at.x * 0.73f + 1.25f * v, at.z * 0.68f + 3.2f * v);
                float breakage = Mathf.SmoothStep(-0.21f, 0.20f, grain) * 2f - 1f;
                float horizontal = Mathf.Sign(u) * outer * (0.72f * relief + 0.42f * breakage);
                float vertical = -thickness * v + (0.39f * breakage + 0.62f * relief) * Mathf.Max(outer * 0.84f, v);
                float lip = outer * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.37f, 0.065f, v));
                float ledge = Noise(at.x * 0.49f + 15, at.z * 0.51f - 4.2f);
                vertical += lip * (0.52f * ledge + 0.11f * breakage);
                // The same dipping joints cross the tread and continue down the rock face.
                float joint = at.x + 0.36f * (at.z - 2f) + 0.81f * v;
                float westJoint = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.69f, 0.10f, Mathf.Abs(joint + 2.58f)));
                float eastJoint = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.76f, 0.17f, Mathf.Abs(joint - 2.75f)));
                float upperRock = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.52f, 0.045f, v));
                float beds = SpanBeds(joint) * (1f + 0.23f * ledge);
                float edgeRise = Mathf.Lerp(0.66f, 0.22f, Mathf.InverseLerp(-0.45f, 0.45f, cut.x));
                float edgeRecess = 0.19f * westJoint + 0.13f * eastJoint;
                float middleRise = 0.48f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-3.7f, 0.34f, joint))
                    * Mathf.InverseLerp(4.68f, 0.63f, joint);
                // Keep the joints shallow across the central tread and expose the offsets at the rim.
                vertical += highShelf * upperRock * (middleRise + 0.20f * beds + outer * (edgeRise * beds - edgeRecess));
                horizontal -= Mathf.Sign(u) * lip * highShelf * (0.19f * westJoint + 0.28f * eastJoint);
                // Spread the outer shoulder into the shell while the middle tread stays walkable.
                float abutment = bank * wallSide * (4.6f + 0.64f * breakage);
                vertical += 0.42f * abutment * (1f - Mathf.Clamp01(v * 1.24f));
                vertices.Add(at + side * horizontal + Vector3.right * (wallward * abutment) + Vector3.up * vertical);
            }
        }

        private static float SpanBeds(float joint)
        {
            float west = Mathf.InverseLerp(-8.6f, -4.8f, joint)
                * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-2.46f, -3.72f, joint));
            float middle = Mathf.InverseLerp(-2.38f, 0.72f, joint)
                * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.81f, 1.67f, joint));
            float east = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(2.59f, 4.38f, joint))
                * Mathf.InverseLerp(8.5f, 4.83f, joint);
            return 1.12f * west + 0.68f * middle + 0.94f * east;
        }

        private readonly struct CutEdge
        {
            internal readonly Vector2 A, B;

            internal CutEdge(Vector2 a, Vector2 b)
            {
                A = a;
                B = b;
            }
        }

        private void BlendShelfIntoWall(int shellEnd, int shelfEnd, int side, float from, float to)
        {
            const int CurveSteps = 6;
            int steps = Mathf.CeilToInt((to - from) / 0.21f);
            List<int>[] wallFaces = BankFaces(0, shellEnd, from, to, steps, side, false);
            List<int>[] shelfFaces = BankFaces(shellEnd, shelfEnd, from, to, steps, side, true);
            var wall = new List<CutEdge>();
            var shelf = new List<CutEdge>();
            var contacts = new List<(Vector3 shelf, Vector3 seam, Vector3 wall)>();
            for (int step = 0; step <= steps; step++)
            {
                float z = Mathf.Lerp(from, to, step / (float)steps);
                CutAcrossRock(wall, wallFaces[step], z, side);
                CutAcrossRock(shelf, shelfFaces[step], z, side);
                Vector2 seam = FindShelfContact(wall, shelf);
                float radius = 0.98f + 0.60f * Noise(side * 9.4f, z * 0.52f);
                float x = Mathf.Max(12.65f, seam.x - radius);
                float y = SampleCut(shelf, x, false);
                float wallY = seam.y + 1.25f * radius;
                float wallX = SampleCut(wall, wallY, true);
                if (seam.x > x + 0.25f && y > 2.5f && wallX < float.PositiveInfinity)
                {
                    contacts.Add((new Vector3(side * x, y - 0.04f, z),
                        new Vector3(side * seam.x, seam.y, z), new Vector3(side * (wallX + 0.045f), wallY, z)));
                    if (step != steps) continue;
                }

                // Taper into the original surfaces when either cross-section stops supporting the join.
                for (int row = 0; contacts.Count >= 5 && row < contacts.Count; row++)
                {
                    int endDistance = Mathf.Min(row, contacts.Count - 1 - row);
                    float blend = Mathf.SmoothStep(0f, 1f, (endDistance + 1) / 6f);
                    Vector3 inset = new Vector3(side, -1f, 0f) * (0.11f * (1f - blend));
                    var (foot, seamPoint, edge) = contacts[row];
                    int start = vertices.Count;
                    for (int j = 0; j <= CurveSteps; j++)
                    {
                        float t = j / (float)CurveSteps;
                        Vector3 curve = Vector3.Lerp(Vector3.Lerp(foot, seamPoint, t), Vector3.Lerp(seamPoint, edge, t), t);
                        vertices.Add(Vector3.Lerp(seamPoint, curve, blend) + inset);
                        if (row == 0 || j == 0) continue;
                        int a = start - CurveSteps - 1 + j;
                        int b = start + j;
                        if (side > 0) Quad(a - 1, b - 1, b, a);
                        else Quad(a - 1, a, b, b - 1);
                    }
                }
                contacts.Clear();
            }
        }

        private List<int>[] BankFaces(int start, int end, float from, float to, int steps, int side, bool tread)
        {
            var slices = new List<int>[steps + 1];
            for (int i = 0; i < slices.Length; i++) slices[i] = new List<int>();
            float slicesPerMetre = steps / (to - from);
            for (int i = start; i < end; i += 3)
            {
                Vector3 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
                float lowZ = Mathf.Min(a.z, Mathf.Min(b.z, c.z)), highZ = Mathf.Max(a.z, Mathf.Max(b.z, c.z));
                if (highZ < from || lowZ > to) continue;
                if (Mathf.Max(a.x * side, Mathf.Max(b.x * side, c.x * side)) < 12f) continue;
                if (Mathf.Max(a.y, Mathf.Max(b.y, c.y)) < 2.4f || Mathf.Min(a.y, Mathf.Min(b.y, c.y)) > 11.5f) continue;
                Vector3 normal = Vector3.Cross(b - a, c - a);
                if (tread ? normal.y <= 0f : normal.x * side >= 0f) continue;
                int first = Mathf.Max(0, Mathf.FloorToInt((lowZ - from) * slicesPerMetre));
                int last = Mathf.Min(steps, Mathf.CeilToInt((highZ - from) * slicesPerMetre));
                for (int slice = first; slice <= last; slice++) slices[slice].Add(i);
            }
            return slices;
        }

        private void CutAcrossRock(List<CutEdge> edges, List<int> faces, float z, int side)
        {
            edges.Clear();
            foreach (int i in faces)
            {
                Vector3 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
                if (a.z < z && b.z < z && c.z < z || a.z >= z && b.z >= z && c.z >= z) continue;

                // Start from the lone vertex on either side of this plane.
                if ((a.z < z) == (b.z < z)) (a, c) = (c, a);
                else if ((a.z < z) == (c.z < z)) (a, b) = (b, a);
                Vector3 first = Vector3.LerpUnclamped(a, b, (z - a.z) / (b.z - a.z));
                Vector3 last = Vector3.LerpUnclamped(a, c, (z - a.z) / (c.z - a.z));
                edges.Add(new CutEdge(new Vector2(first.x * side, first.y), new Vector2(last.x * side, last.y)));
            }
        }

        private static Vector2 FindShelfContact(List<CutEdge> wall, List<CutEdge> shelf)
        {
            Vector2 highest = Vector2.zero;
            foreach (CutEdge bank in shelf)
            foreach (CutEdge face in wall)
            {
                Vector2 a = bank.B - bank.A, b = face.B - face.A, offset = face.A - bank.A;
                float determinant = a.x * b.y - a.y * b.x;
                if (Mathf.Abs(determinant) < 0.0001f) continue;
                float t = (offset.x * b.y - offset.y * b.x) / determinant;
                float u = (offset.x * a.y - offset.y * a.x) / determinant;
                if (t < 0f || t > 1f || u < 0f || u > 1f) continue;
                Vector2 crossing = bank.A + t * a;
                if (crossing.x >= 13.5f && crossing.y > highest.y) highest = crossing;
            }
            return highest;
        }

        private static float SampleCut(List<CutEdge> edges, float coordinate, bool wall)
        {
            float sample = wall ? float.PositiveInfinity : float.NegativeInfinity;
            foreach (CutEdge edge in edges)
            {
                float a = wall ? edge.A.y : edge.A.x;
                float b = wall ? edge.B.y : edge.B.x;
                if (Mathf.Abs(a - b) < 0.0001f) continue;
                float t = (coordinate - a) / (b - a);
                if (t < 0f || t > 1f) continue;
                Vector2 point = Vector2.Lerp(edge.A, edge.B, t);
                sample = wall ? Mathf.Min(sample, point.x) : Mathf.Max(sample, point.y);
            }
            return sample;
        }

        private void ShelfScree(int seed)
        {
            var random = new System.Random(unchecked(seed * 37529 + 1327));
            float Unit() => (float)random.NextDouble();
            for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 34; i++)
                {
                    float run = Unit();
                    float x = side * Mathf.Lerp(5.7f, 14.4f, run);
                    float z = Mathf.Lerp(-0.85f, 1.65f, run) + (Unit() - 0.5f) * (2.4f + run * 3.75f)
                        + (side > 0 ? -0.65f : 0.7f);
                    float size = Mathf.Lerp(0.065f, 0.39f, Mathf.Pow(Unit(), 2.5f));
                    if (i < 5)
                    {
                        size += 0.28f;
                        RockFragment(new Vector3(x, Floor(x, z) - 0.12f, z), size, size * (0.7f + Unit()),
                            Unit() * 6f, 0.58f, true);
                    }
                    else
                    {
                        AngularBlock(x, z, size, size * Mathf.Lerp(0.46f, 0.96f, Unit()),
                            size * Mathf.Lerp(0.54f, 1.12f, Unit()), side * 0.35f + Unit() * 2.65f);
                    }
                }
            }
        }

        private void Formations(int seed, int rings, bool bedrock)
        {
            // Rockfall lies along the walls; ceiling fragments stay clear of the crossing.
            var random = new System.Random(unchecked(seed * 15731 + 541));
            float Unit() => (float)random.NextDouble();
            for (int i = 0; i < 68; i++)
            {
                float z = Mathf.Lerp(-9, 14, Unit());
                float x = (i % 2 == 0 ? 1 : -1) * Mathf.Lerp(13.55f, 15.5f, Unit());
                float radius = 0.45f + 1.05f * Unit();
                RockFragment(new Vector3(x, Floor(x, z) - 0.23f, z), radius, radius * (0.6f + 2.05f * Unit()), Unit() * 6, 0.33f, bedrock);
            }
            for (int i = 0; i < 40; i++)
            {
                int slice = random.Next(1, rings - 1), arch = random.Next(10, 38);
                Vector3 ceiling = vertices[slice * RingSize + FloorSteps + arch];
                if (ceiling.z < -9f || ceiling.z > 16f || ceiling.y < 10.4f) continue;
                float radius = 0.45f + 0.76f * Unit(), height = -0.7f - 1.12f * Unit(), phase = Unit() * 6;
                if (!refineRockfall || Scar(ceiling.x, ceiling.z) <= 0f)
                    RockFragment(ceiling + Vector3.up * (bedrock ? 0.51f + 0.20f * radius : 0.38f),
                        radius, bedrock ? height * 0.82f : height, phase, 0.49f, bedrock);
            }
            for (int i = 0; i < 95; i++)
            {
                float z = Mathf.Lerp(-14, 13, Unit());
                float x = (i % 2 == 0 ? 1 : -1) * Mathf.Lerp(3.6f, 7.9f, Unit());
                float r = 0.13f + 0.65f * Unit();
                float height = r * (0.47f + Unit()), phase = Unit() * 6;
                if (!refineRockfall || Mathf.Abs(x - FallX) > 3.4f || Mathf.Abs(z - FallZ) > 4.1f)
                    RockFragment(new Vector3(x, Floor(x, z) - 0.13f, z), r, height, phase, 0.52f, bedrock);
            }
        }

        private static float Scar(float x, float z)
        {
            float dx = x - FallX, dz = z - FallZ;
            float along = dx * Mathf.Cos(FallStrike) + dz * Mathf.Sin(FallStrike);
            float across = dz * Mathf.Cos(FallStrike) - dx * Mathf.Sin(FallStrike);
            float edge = Mathf.Max(Mathf.Abs(along + 0.20f * across) / 3.0f, Mathf.Abs(across) / 2.2f);
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.49f, 1f, edge));
        }

        private Color32[] FreshFaces(int lastShellVertex, int firstFallenVertex)
        {
            var colors = new Color32[vertices.Count];
            for (int i = FloorSteps + 1; i < lastShellVertex; i++)
            {
                Vector3 p = vertices[i];
                if (p.y > 8.5f) colors[i].r = (byte)(Scar(p.x, p.z) * 225f);
            }
            for (int i = firstFallenVertex; i < colors.Length; i++)
                colors[i].r = 165;
            return colors;
        }

        private void FallenRock(int seed)
        {
            // The two large blocks share the direction of the missing ceiling plate.
            bool chipped = seed == 9;
            AngularBlock(FallX, FallZ, 1.24f, 0.79f, 1.34f, FallStrike, chipped);
            AngularBlock(FallX + 0.82f, FallZ - 1.45f, 0.78f, 0.58f, 0.84f, FallStrike - 0.23f, chipped);
            AngularBlock(FallX - 1.19f, FallZ - 1.35f, 0.63f, 0.37f, 0.55f, FallStrike + 0.30f, chipped);
            var random = new System.Random(unchecked(seed * 733 + 1723));
            for (int i = 0; i < 31; i++)
            {
                float run = Mathf.Lerp(1.0f, 4.05f, (float)random.NextDouble());
                float spread = ((float)random.NextDouble() - 0.5f) * Mathf.Lerp(1.5f, 4.45f, run / 4.05f);
                float x = Mathf.Clamp(FallX + spread - 0.27f * run, 3.75f, 8.85f);
                float z = FallZ - run;
                float size = Mathf.Lerp(0.48f, 0.15f, run / 4.05f) * Mathf.Lerp(0.68f, 1.25f, (float)random.NextDouble());
                AngularBlock(x, z, size, size * Mathf.Lerp(0.57f, 0.88f, (float)random.NextDouble()),
                    size * Mathf.Lerp(0.40f, 0.98f, (float)random.NextDouble()), FallStrike + ((float)random.NextDouble() - 0.5f) * 1.1f);
            }
        }

        private void AngularBlock(float x, float z, float length, float width, float height, float strike, bool chipped = false)
        {
            int start = vertices.Count;
            float floor = Floor(x, z) - Mathf.Max(0.09f, height * 0.25f);
            Vector2 axis = new Vector2(Mathf.Cos(strike), Mathf.Sin(strike));
            Vector2 cross = new Vector2(-axis.y, axis.x);
            Vector2 center = new Vector2(x, z);
            var corners = new[]
            {
                new Vector3(-0.93f, -0.80f, 0f), new Vector3(-0.80f, 0.84f, 0f),
                new Vector3(0.95f, 0.71f, 0f), new Vector3(0.86f, -0.92f, 0f),
                new Vector3(-0.59f, -0.77f, 0.74f), new Vector3(-0.73f, 0.48f, 0.82f),
                new Vector3(0.88f, 0.49f, 1f), new Vector3(0.55f, -0.64f, 0.84f)
            };
            int sides = chipped ? 8 : 4;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 corner = corners[i];
                if (chipped)
                {
                    int ring = i / 4 * 4;
                    float cut = 0.16f + 0.29f * Mathf.PerlinNoise(x * 0.3f + i * 1.34f, z + 5f);
                    Add(Vector3.Lerp(corner, corners[ring + (i + 3) % 4], cut));
                    Add(Vector3.Lerp(corner, corners[ring + (i + 1) % 4], cut * 0.64f));
                }
                else Add(corner);
            }
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                Quad(start + side, start + next, start + next + sides, start + side + sides);
            }
            int top = start + sides;
            for (int i = 1; i < sides - 1; i++)
            {
                triangles.Add(top);
                triangles.Add(top + i);
                triangles.Add(top + i + 1);
            }
            PropCount++;

            void Add(Vector3 corner)
            {
                Vector2 p = center + corner.x * length * axis + corner.y * width * cross;
                vertices.Add(new Vector3(p.x, floor + corner.z * height, p.y));
            }
        }

        private void RockFragment(Vector3 basePoint, float radius, float height, float phase, float cap, bool bedrock)
        {
            PropCount++;
            int first = vertices.Count;
            int sides = bedrock ? 9 + (int)(phase * 0.61f) : 7;
            float lean = bedrock ? Mathf.Cos(phase * 1.7f) * radius * 0.25f : radius * 0.13f;
            float aspect = bedrock ? 0.77f + 0.19f * Mathf.Sin(phase * 2.3f) : 0.78f;
            for (int ring = 0; ring < 3; ring++)
            {
                for (int i = 0; i < sides; i++)
                {
                    float angle = phase + i * 2f * Mathf.PI / sides;
                    float weathering = bedrock ? Noise(phase * 2.15f + i * 0.6f, ring * 0.55f + i * 0.15f) : 0f;
                    float r = radius * (1.1f + 0.19f * Mathf.Cos(angle * 3 + phase))
                        * (ring == 0 ? 1f : ring == 1 ? 0.78f : cap) * (1f + weathering * 0.63f);
                    vertices.Add(basePoint + new Vector3(r * Mathf.Cos(angle) + ring * lean,
                        height * (ring == 0 ? 0 : ring == 1 ? 0.55f : 1) * (1f + weathering * 0.42f),
                        r * Mathf.Sin(angle) * aspect));
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
