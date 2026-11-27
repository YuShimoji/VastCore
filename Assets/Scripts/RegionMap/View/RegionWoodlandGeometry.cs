using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap
{
    // Reusable plant forms have a ground pivot and unit height. Crown/branch shape and
    // foliage density vary together, while a plant's site and overall size are set by the layout.
    internal sealed class RegionWoodlandGeometry
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Color32> colours = new List<Color32>();
        private readonly List<int> indices = new List<int>();

        public int TriangleCount => indices.Count / 3;

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colours);
            mesh.SetTriangles(indices, 0);
            return mesh;
        }

        public void Append(RegionWoodlandGeometry form, Vector3 origin, float height, float yaw)
        {
            Quaternion rotation = Quaternion.Euler(0, yaw, 0);
            int start = vertices.Count;
            for (int i = 0; i < form.vertices.Count; i++)
                Add(origin + rotation * (form.vertices[i] * height), rotation * form.normals[i], form.colours[i]);
            foreach (int index in form.indices) indices.Add(index + start);
        }

        public int Add(Vector3 position, Vector3 normal, Color32 colour)
        {
            int i = vertices.Count;
            vertices.Add(position);
            normals.Add(normal);
            colours.Add(colour);
            return i;
        }

        public void Tri(int a, int b, int c)
        {
            indices.Add(a);
            indices.Add(b);
            indices.Add(c);
        }

        public static RegionWoodlandGeometry Plant(RegionWoodlandForm form, int shape, bool detailed)
        {
            var geometry = new RegionWoodlandGeometry();
            var rng = new System.Random(71 * (int)form + 1103 * shape + 509);
            switch (form)
            {
                case RegionWoodlandForm.Conifer: geometry.Tree(rng, true, false, detailed); break;
                case RegionWoodlandForm.SpreadingTree: geometry.Tree(rng, false, false, detailed); break;
                case RegionWoodlandForm.TallBroadleaf: geometry.Tree(rng, false, true, detailed); break;
                case RegionWoodlandForm.Shrub: geometry.Shrub(rng); break;
                case RegionWoodlandForm.Fern: geometry.GroundPlant(rng, detailed ? 7 : 4, true, 0.83f, detailed); break;
                case RegionWoodlandForm.BroadleafHerb: geometry.GroundPlant(rng, detailed ? 10 : 6, false, 0.92f, detailed); break;
                case RegionWoodlandForm.Grass: geometry.Grass(rng); break;
            }
            return geometry;
        }

        private static float Range(System.Random rng, float low, float high) => Mathf.Lerp(low, high, (float)rng.NextDouble());
        private static Vector3 Compass(float angle) => new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
        private static Color32 Tone(Color32 colour, float factor) => new Color32(
            (byte)Mathf.Min(255, colour.r * factor), (byte)Mathf.Min(255, colour.g * factor),
            (byte)Mathf.Min(255, colour.b * factor), colour.a);

        private void Tree(System.Random rng, bool conifer, bool tall, bool detailed)
        {
            var bark = tall ? new Color32(83, 68, 53, 255) : new Color32(97, 71, 51, 255);
            var leaf = conifer ? new Color32(52, 80, 56, 255) : tall ? new Color32(63, 96, 41, 255) : new Color32(85, 117, 60, 255);
            leaf = Tone(leaf, Range(rng, 0.83f, 1.17f));
            Vector3 lean = Compass(Range(rng, 0, 6.28f)) * Range(rng, 0.018f, tall ? 0.058f : 0.11f);
            float waist = conifer ? 0.45f : Range(rng, tall ? 0.51f : 0.36f, tall ? 0.72f : 0.61f);
            float rootRadius = Range(rng, 0.021f, tall ? 0.034f : 0.028f);
            Vector3 fork = lean * 0.54f + Vector3.up * waist;
            int crownSeed = rng.Next();
            Tube(Vector3.down * 0.02f, lean * 0.10f + Vector3.up * 0.17f, rootRadius, rootRadius * 0.66f, bark, detailed ? 8 : 5);
            Tube(lean * 0.10f + Vector3.up * 0.17f, fork, rootRadius * 0.66f, rootRadius * 0.43f, bark, detailed ? 8 : 5);
            Tube(fork, lean + Vector3.up * (conifer ? 0.99f : 0.89f), rootRadius * 0.43f, 0.0035f, bark, 5);
            if (detailed)
            {
                int roots = tall ? 6 : 4;
                for (int i = 0; i < roots; i++)
                {
                    Vector3 step = Compass(i * 6.283f / roots + Range(rng, -0.3f, 0.3f));
                    Vector3 tip = step * Range(rng, rootRadius * 1.5f, tall ? 0.092f : 0.060f) - Vector3.up * 0.035f;
                    Vector3 knee = tip * 0.35f + Vector3.up * 0.012f;
                    Tube(Vector3.up * (tall ? 0.12f : 0.042f), knee, rootRadius * 0.47f, rootRadius * 0.31f, bark, 5);
                    Tube(knee, tip, rootRadius * 0.31f, 0.0025f, bark, 5);
                }
            }

            var crown = new System.Random(crownSeed);
            if (conifer)
            {
                // Separate whorls and offset tips keep light between branches, even in the far mesh.
                for (int tier = 0; tier < 6; tier++)
                {
                    float t = tier / 6f;
                    float level = 0.32f + 0.67f * t + Range(crown, -0.018f, 0.018f);
                    float radius = (0.25f - 0.18f * t) * Range(crown, 0.72f, 1.12f);
                    Vector3 middle = level * (lean + Vector3.up);
                    var tierRng = new System.Random(crown.Next());
                    Blob(middle, new Vector3(radius * 0.54f, 0.032f + radius * 0.14f, radius * 0.47f),
                        Tone(leaf, 0.88f + t * 0.16f), tierRng, 5, 3);
                    const int branches = 5;
                    for (int j = 0; j < branches; j++)
                    {
                        float a = (tier * 0.91f + j * 6.283f / branches) + Range(tierRng, -0.20f, 0.20f);
                        Vector3 direction = Compass(a);
                        Vector3 tip = middle + direction * radius - Vector3.up * Range(tierRng, 0.005f, 0.054f);
                        if (detailed) Tube(middle, tip, 0.0065f, 0.0015f, bark, 4);
                        Color32 branchColour = Tone(leaf, Range(tierRng, 0.80f, 1.19f));
                        for (int k = detailed ? 0 : 1; k < 3; k++)
                        {
                            Vector3 c = Vector3.Lerp(middle, tip, 0.39f + k * 0.29f);
                            Vector3 s = Vector3.Cross(direction, Vector3.up) * radius * (0.41f - k * 0.102f);
                            Leaf(c - direction * radius * 0.67f + Vector3.up * 0.037f, c + direction * radius * 0.50f,
                                s, Tone(branchColour, 1.02f - k * 0.08f));
                        }
                    }
                }
                Blob((lean + Vector3.up) * 0.95f, new Vector3(0.031f, 0.06f, 0.027f), leaf, rng, 5, 3);
                return;
            }

            const int boughs = 7;
            float spin = Range(crown, 0, 6.28f);
            float crownWidth = Range(crown, tall ? 0.17f : 0.24f, tall ? 0.27f : 0.35f);
            for (int j = 0; j < boughs; j++)
            {
                var bough = new System.Random(crown.Next());
                if (!detailed && (j == 1 || j == 4)) continue;
                Vector3 radial = Compass(spin + j * 2.40f + Range(bough, -0.55f, 0.55f));
                float rise = Mathf.Pow((j + Range(bough, 0.1f, 0.65f)) / boughs, 0.69f);
                float branchHeight = Mathf.Lerp(Mathf.Max(0.58f, waist + 0.03f), 0.96f, rise);
                Vector3 knot = Vector3.Lerp(fork, lean + Vector3.up * 0.89f, 0.68f * rise);
                Vector3 tip = lean * branchHeight + Vector3.up * branchHeight + radial * crownWidth * (1.05f - 0.77f * rise);
                Vector3 elbow = Vector3.Lerp(knot, tip, 0.59f) - Vector3.up * Range(bough, 0.020f, 0.069f);
                Tube(knot, elbow, rootRadius * 0.33f, rootRadius * 0.19f, bark, 5);
                Tube(elbow, tip, rootRadius * 0.20f, 0.002f, bark, detailed ? 5 : 3);
                float spread = crownWidth * Range(bough, 0.49f, 0.71f);
                var scale = new Vector3(1.07f * spread, spread * (tall ? 0.60f : 0.87f), spread);
                Foliage(tip, scale, Tone(leaf, Range(bough, 0.83f, 1.15f)), bough, detailed);
            }
            if (tall && detailed && rng.NextDouble() < 0.86)
            {
                Vector3 from = Vector3.zero;
                float end = waist + 0.06f;
                for (int i = 0; i <= 13; i++)
                {
                    float t = i / 13f;
                    float a = spin + t * 6.8f;
                    float groundToFork = Mathf.Min(waist, 0.04f + t * end);
                    Vector3 anchor = Vector3.Lerp(new Vector3(lean.x * 0.10f, 0.17f, lean.z * 0.10f), fork,
                        Mathf.InverseLerp(0.17f, waist, groundToFork));
                    anchor.y = groundToFork;
                    float radius = groundToFork <= 0.17f
                        ? Mathf.Lerp(rootRadius, rootRadius * 0.66f, groundToFork / 0.17f)
                        : Mathf.Lerp(rootRadius * 0.66f, rootRadius * 0.43f, Mathf.InverseLerp(0.17f, waist, groundToFork));
                    Vector3 p = anchor + Compass(a) * radius * 1.04f;
                    if (i > 0) Tube(from, p, 0.0034f, 0.0031f, bark, 4);
                    if ((i & 1) == 0) Leaf(p, p + Compass(a) * 0.038f + Vector3.up * 0.026f,
                        Compass(a + 1.57f) * 0.012f, leaf);
                    from = p;
                }
            }
        }

        private void Foliage(Vector3 centre, Vector3 scale, Color32 colour, System.Random rng, bool detailed)
        {
            Blob(centre, scale * (detailed ? 0.81f : 0.99f), colour, rng, detailed ? 9 : 6, detailed ? 5 : 3);
            int count = detailed ? 37 : 6;
            for (int i = 0; i < count; i++)
            {
                float a = i * 2.40f;
                float y = Range(rng, -0.62f, 0.83f);
                Vector3 d = Compass(a);
                Vector3 p = centre + Vector3.Scale(scale, new Vector3(d.x, y, d.z)) * Range(rng, 0.62f, 1.09f);
                Vector3 w = Compass(a + 1.57f) * scale.x * Range(rng, 0.12f, 0.28f);
                Leaf(p - d * scale.x * 0.23f, p + d * scale.x * 0.48f + Vector3.up * scale.y * Range(rng, -0.3f, 0.5f),
                    w, Tone(colour, Range(rng, 0.78f, 1.31f)));
            }
        }

        private void Shrub(System.Random rng)
        {
            var colour = new Color32(85, 111, 59, 255);
            for (int j = 0; j < 5; j++)
            {
                Vector3 crown = Compass(j * 2.40f) * Range(rng, 0.20f, 0.54f) + Vector3.up * Range(rng, 0.27f, 0.75f);
                Tube(Vector3.down * 0.035f, crown, 0.026f, 0.008f, new Color32(89, 68, 45, 255), 5);
                Foliage(crown, new Vector3(0.45f, 0.30f, 0.39f), Tone(colour, Range(rng, 0.87f, 1.16f)), rng, false);
            }
        }

        private void GroundPlant(System.Random rng, int fronds, bool fern, float reach, bool detailed)
        {
            var green = fern ? new Color32(98, 133, 54, 255) : new Color32(74, 116, 54, 255);
            for (int i = 0; i < fronds; i++)
            {
                float a = i * 6.283f / fronds + Range(rng, -0.30f, 0.30f);
                Vector3 d = Compass(a);
                Vector3 side = Compass(a + 1.57f);
                float length = reach * Range(rng, 0.66f, 1.28f);
                float height = Range(rng, 0.42f, 1.02f);
                Vector3 tip = d * length + Vector3.up * height * 0.59f;
                if (!fern)
                {
                    Vector3 basePoint = d * length * Range(rng, 0.31f, 0.50f) + Vector3.up * height * Range(rng, 0.58f, 0.98f);
                    Tube(Vector3.zero, basePoint, 0.018f, 0.010f, green, 4);
                    Vector3 width = side * Range(rng, 0.15f, 0.31f);
                    Color32 tone = Tone(green, Range(rng, 0.73f, 1.27f));
                    if (detailed) BroadBlade(basePoint, tip, width, tone);
                    else Leaf(basePoint, tip, width, tone);
                    continue;
                }
                Vector3 previous = Vector3.zero;
                int segments = detailed ? 8 : 5;
                for (int j = 1; j <= segments; j++)
                {
                    float t = j / (float)segments;
                    Vector3 p = Vector3.Lerp(Vector3.zero, tip, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * height * 0.46f;
                    Tube(previous, p, 0.007f, 0.006f, green, 3);
                    if (j > 1)
                    {
                        float leafWidth = 0.22f * length * Mathf.Sin(t * 2.74f);
                        Vector3 tipOffset = d * 0.16f - Vector3.up * (leafWidth * 0.28f);
                        Leaf(p, p + side * leafWidth + tipOffset, d * 0.067f, Tone(green, Range(rng, 0.81f, 1.15f)));
                        Leaf(p, p - side * leafWidth + tipOffset, d * 0.067f, Tone(green, Range(rng, 0.81f, 1.15f)));
                    }
                    previous = p;
                }
            }
        }

        private void Grass(System.Random rng)
        {
            var green = new Color32(111, 130, 58, 255);
            for (int i = 0; i < 15; i++)
            {
                float a = i * 2.40f;
                Vector3 root = Compass(a) * Range(rng, 0.035f, 0.52f);
                Vector3 tip = root + Compass(a) * Range(rng, 0.12f, 0.56f) + Vector3.up * Range(rng, 0.28f, 1.00f);
                Leaf(root, tip, Compass(a + 1.57f) * Range(rng, 0.022f, 0.063f), Tone(green, Range(rng, 0.68f, 1.21f)));
            }
        }

        public void Tube(Vector3 a, Vector3 b, float first, float last, Color32 col, int sides)
        {
            Vector3 axis = (b - a).normalized;
            Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.85f ? Vector3.up : Vector3.forward).normalized;
            Vector3 v = Vector3.Cross(axis, u);
            int start = vertices.Count;
            for (int i = 0; i < sides; i++)
            {
                float angle = i * 6.283185f / sides;
                Vector3 n = Mathf.Cos(angle) * u + Mathf.Sin(angle) * v;
                Color32 shade = Tone(col, i % 3 == 0 ? 0.79f : i % 3 == 1 ? 1.09f : 1f);
                Add(a + n * first, n, shade);
                Add(b + n * last, n, shade);
            }
            for (int i = 0; i < sides; i++)
            {
                int side = start + 2 * i, next = start + 2 * ((i + 1) % sides);
                Tri(side, next, side + 1);
                Tri(side + 1, next, next + 1);
            }
        }

        public void Blob(Vector3 centre, Vector3 scale, Color32 colour, System.Random rng, int sides, int bands)
        {
            int start = vertices.Count;
            for (int j = 0; j <= bands; j++)
            for (int i = 0; i < sides; i++)
            {
                float angle = (i + 0.23f * (j & 1)) * 6.283185f / sides;
                float t = Mathf.Lerp(-1.50f, 1.50f, j / (float)bands);
                Vector3 n = Compass(angle) * Mathf.Cos(t) + Vector3.up * Mathf.Sin(t);
                Color32 shade = Tone(colour, Mathf.Lerp(0.74f, 1.10f, 0.5f + n.y * 0.5f) * Range(rng, 0.88f, 1.15f));
                Add(centre + Vector3.Scale(n, scale) * Range(rng, 0.83f, 1.15f),
                    new Vector3(n.x / scale.x, n.y / scale.y, n.z / scale.z).normalized, shade);
            }
            for (int j = 0; j < bands; j++)
            for (int i = 0; i < sides; i++)
            {
                int a = start + j * sides + i, b = start + j * sides + (i + 1) % sides;
                Tri(a, a + sides, b);
                Tri(b, a + sides, b + sides);
            }
        }

        public void Leaf(Vector3 root, Vector3 tip, Vector3 side, Color32 colour)
        {
            Vector3 centre = Vector3.Lerp(root, tip, 0.55f);
            Vector3 n = Vector3.Cross(tip - root, side).normalized;
            if (n.y < 0f) n = -n;
            int a = Add(root, n, Tone(colour, 0.78f));
            int b = Add(centre + side, n, colour);
            int c = Add(tip, n, Tone(colour, 1.12f));
            int d = Add(centre - side, n, Tone(colour, 0.92f));
            int e = Add(centre + Vector3.up * side.magnitude * 0.22f, n, Tone(colour, 1.09f));
            Tri(a, b, e);
            Tri(b, c, e);
            Tri(c, d, e);
            Tri(d, a, e);
        }

        private void BroadBlade(Vector3 root, Vector3 tip, Vector3 width, Color32 colour)
        {
            Vector3 up = Vector3.Cross(tip - root, width).normalized;
            if (up.y < 0f) up = -up;
            Vector3 a = Vector3.Lerp(root, tip, 0.29f);
            Vector3 b = Vector3.Lerp(root, tip, 0.67f);
            int i = Add(root, up, Tone(colour, 0.80f));
            Add(a + width * 0.88f, up, colour);
            Add(b + width * 0.72f, up, colour);
            Add(tip, up, Tone(colour, 1.09f));
            Add(b - width * 0.72f, up, Tone(colour, 0.88f));
            Add(a - width * 0.88f, up, Tone(colour, 0.89f));
            int rib = Add(Vector3.Lerp(a, b, 0.4f) + up * width.magnitude * 0.34f, up, Tone(colour, 1.16f));
            for (int k = 0; k < 6; k++) Tri(i + k, i + (k + 1) % 6, rib);
        }
    }
}
