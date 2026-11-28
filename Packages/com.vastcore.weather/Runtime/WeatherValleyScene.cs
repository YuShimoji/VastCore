using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.Weather
{
    [Serializable]
    public sealed class WeatherValleyAssets
    {
        public Texture2D ForestColor;
        public Texture2D ForestNormal;
        public Texture2D ForestRoughness;
        public Texture2D RockColor;
        public Texture2D RockNormal;
        public Texture2D RockRoughness;

        internal bool Available => ForestColor && ForestNormal && ForestRoughness && RockColor && RockNormal && RockRoughness;
    }

    internal sealed class WeatherValleyScene
    {
        private const float FloorEyeX = -350f;
        private const float FloorEyeZ = 3160f;
        private const float TrackGrade = 0.185f;
        private const float TrackBend = 8.2f;
        private const float TrackFrequency = 0.0135f;
        private readonly WeatherSampleGeometry geometry;
        private readonly Material surface;
        private readonly float seedOffset;
        private readonly float floorElevation;
        private readonly float[] east;
        private readonly float[] north;
        private readonly Vector3[] terrain;

        public WeatherValleyScene(WeatherSampleGeometry geometry, WeatherValleyAssets assets, int seed)
        {
            this.geometry = geometry;
            seedOffset = (seed & 255) * 0.413f;
            floorElevation = geometry.MountainHeight(FloorEyeX, FloorEyeZ);
            east = Axis(-23500f, (-6500f, 325f), (-3500f, 128f), (-1400f, 44f),
                (-620f, 30f), (FloorEyeX, 9f), (-110f, 9f), (960f, 30f), (5000f, 62f), (23500f, 296f));
            north = Axis(-6200f, (-3900f, 193f), (2200f, 68f), (FloorEyeZ, 20f),
                (3940f, 16f), (11800f, 84f), (25500f, 173f), (41600f, 290f));
            terrain = new Vector3[east.Length * north.Length];
            surface = geometry.CreateSurface("ValleySurface");
            surface.SetVector("_Track", new Vector4(FloorEyeX, FloorEyeZ, TrackGrade, TrackBend));
            surface.SetFloat("_TrackFrequency", TrackFrequency);
            if (assets != null && assets.Available)
            {
                surface.SetTexture("_ForestMap", assets.ForestColor);
                surface.SetTexture("_ForestNormal", assets.ForestNormal);
                surface.SetTexture("_ForestRoughness", assets.ForestRoughness);
                surface.SetTexture("_RockMap", assets.RockColor);
                surface.SetTexture("_RockNormal", assets.RockNormal);
                surface.SetTexture("_RockRoughness", assets.RockRoughness);
                surface.SetFloat("_UseTerrainMaps", 1f);
            }
            geometry.Render("Valley slopes and overlapping ridges", BuildTerrain(), 0f, surface);
            BuildWater();
            BuildForest(seed);
        }

        public float HeightAt(float x, float z)
        {
            int ix = Cell(east, x);
            int iz = Cell(north, z);
            float u = Mathf.InverseLerp(east[ix], east[ix + 1], x);
            float v = Mathf.InverseLerp(north[iz], north[iz + 1], z);
            int first = iz * east.Length + ix;
            float a = terrain[first].y;
            float c = terrain[first + east.Length + 1].y;
            return u < v
                ? a * (1f - v) + terrain[first + east.Length].y * (v - u) + c * u
                : a * (1f - u) + terrain[first + 1].y * (u - v) + c * v;
        }

        private Mesh BuildTerrain()
        {
            int columns = east.Length - 1;
            int rows = north.Length - 1;
            var colors = new Color[terrain.Length];
            var indices = new int[rows * columns * 6];
            for (int row = 0; row <= rows; row++)
            for (int column = 0; column <= columns; column++)
            {
                int vertex = row * east.Length + column;
                float x = east[column];
                float z = north[row];
                float y = Landform(x, z);
                terrain[vertex] = new Vector3(x, y, z);
                float forest = ForestCover(x, z);
                float colorShift = Noise(x * 0.0018f, z * 0.0018f);
                Color meadow = Color.Lerp(Tone(0x59634A), Tone(0x9A9270), colorShift);
                Color trees = Color.Lerp(Tone(0x344D3E), Tone(0x526749), colorShift);
                Color stone = Color.Lerp(Tone(0x646964), Tone(0x837C6A), colorShift);
                Color ground = Color.Lerp(meadow, trees, forest);
                colors[vertex] = Color.Lerp(ground, stone, Smooth(1270f, 2260f, y) * 0.92f).linear;
                colors[vertex].a = forest;
                if (column == columns || row == rows)
                    continue;

                int triangle = (row * columns + column) * 6;
                indices[triangle] = vertex;
                indices[triangle + 1] = vertex + east.Length;
                indices[triangle + 2] = vertex + east.Length + 1;
                indices[triangle + 3] = vertex;
                indices[triangle + 4] = vertex + east.Length + 1;
                indices[triangle + 5] = vertex + 1;
            }
            var mesh = new Mesh
            {
                indexFormat = terrain.Length > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16,
                vertices = terrain,
                colors = colors,
                triangles = indices
            };
            mesh.RecalculateNormals();
            return mesh;
        }

        private float Landform(float x, float z)
        {
            // The old far ridges supply the horizon. Lower meanders and alternating spurs separate the middle slopes.
            float original = geometry.MountainLandform(x, z);
            float cross = x - CenterOfValley(z);
            float grade = Mathf.Clamp((z - FloorEyeZ) * 0.0075f, -39f, 70f);
            float disturbance = (Noise(x * 0.0041f, z * 0.0041f) - 0.5f) * 12f;
            float floor = floorElevation + grade + cross * cross * 0.00012f + disturbance;
            float drainage = (x - StreamCenter(z)) / 56f;
            floor -= Mathf.Exp(-drainage * drainage) * 3.5f;

            float lowland = (1f - Smooth(380f, 1490f, Mathf.Abs(cross))) *
                (1f - Smooth(8600f, 13600f, z));
            float spurs = Mathf.Max(
                Spur(x, z, -1840f, 1110f, 640f, 1900f, 246f, -0.25f),
                Spur(x, z, 1540f, 4750f, 840f, 2140f, 370f, -0.18f));
            spurs = Mathf.Max(spurs, Spur(x, z, -2350f, 7260f, 1060f, 1900f, 314f, 0.22f));
            spurs += Spur(x, z, 2840f, 9060f, 1010f, 2180f, 230f, 0.23f);
            spurs *= 0.82f + 0.34f * Noise(x * 0.0024f, z * 0.0027f);
            float shoulder = (1f - Smooth(12800f, 17800f, Mathf.Abs(z - 4800f))) * Smooth(240f, 940f, Mathf.Abs(cross));
            float gullies = (Noise(x * 0.0102f + z * 0.0045f, z * 0.0034f) - 0.5f) * 42f * shoulder;
            float adjusted = Mathf.Lerp(original, floor, lowland) + spurs + gullies;
            adjusted += Spur(x, z, -2650f, -1620f, 410f, 510f, 161f, -0.13f);
            float fromFloorEye = Vector2.Distance(new Vector2(x, z), new Vector2(FloorEyeX, FloorEyeZ));
            return Mathf.Lerp(floorElevation + grade, adjusted, Smooth(1f, 30f, fromFloorEye));
        }

        private void BuildWater()
        {
            var water = new WeatherSampleGeometry.Blocks();
            const int streamSteps = 250;
            for (int index = 0; index < streamSteps; index++)
            {
                float z0 = -2600f + index * 47f;
                float z1 = z0 + 47f;
                float x0 = StreamCenter(z0);
                float x1 = StreamCenter(z1);
                float width0 = 22f + 13f * Noise(4.2f, z0 * 0.0016f);
                float width1 = 22f + 13f * Noise(4.2f, z1 * 0.0016f);
                float y0 = HeightAt(x0, z0) + 0.37f;
                float y1 = HeightAt(x1, z1) + 0.37f;
                water.Quad(new Vector3(x0 - width0, y0, z0), new Vector3(x1 - width1, y1, z1),
                    new Vector3(x1 + width1, y1, z1), new Vector3(x0 + width0, y0, z0), Tone(0x597171));
            }
            geometry.Render("Valley stream", water.Mesh(), 1f, surface);
        }

        private void BuildForest(int seed)
        {
            var forest = new WeatherSampleGeometry.Blocks();
            var random = new System.Random(seed ^ 6103);
            for (float z = -3150f; z <= 12200f; z += 61f)
            for (float x = -6200f; x <= 6300f; x += 61f)
            {
                float px = x + Between(random, -27f, 27f);
                float pz = z + Between(random, -27f, 27f);
                float y = HeightAt(px, pz);
                float cover = ForestCover(px, pz);
                if (y > 1470f || random.NextDouble() > cover * 0.65f ||
                    Mathf.Abs(px - StreamCenter(pz)) < 38f ||
                    (pz >= FloorEyeZ - 45f && pz < FloorEyeZ + 1050f && Mathf.Abs(px - PathCenter(pz)) < 9f))
                    continue;

                bool near = (pz < 500f && px < -1200f && px > -3400f) ||
                    (Mathf.Abs(pz - FloorEyeZ) < 500f && Mathf.Abs(px - FloorEyeX) < 490f);
                Tree(forest, random, new Vector3(px, y, pz), Between(random, 7f, 21f), near);
                if (cover > 0.57f)
                {
                    float extraX = px + Between(random, -18f, 18f);
                    float extraZ = pz + Between(random, -15f, 15f);
                    Tree(forest, random, new Vector3(extraX, HeightAt(extraX, extraZ), extraZ),
                        Between(random, 6.8f, 17f), near);
                }
            }
            for (float z = -1930f; z <= -970f; z += 12f)
            for (float x = -2880f; x <= -2150f; x += 12f)
            {
                float px = x + Between(random, -5.0f, 5.0f);
                float pz = z + Between(random, -5.0f, 5.0f);
                float grove = Noise(px * 0.0094f, pz * 0.0083f);
                if (grove < 0.31f || random.NextDouble() > 0.34 + ForestCover(px, pz) * 0.60)
                    continue;
                bool nearby = pz < -1510f && px > -2750f;
                Tree(forest, random, new Vector3(px, HeightAt(px, pz), pz), Between(random, 10f, 24f), nearby);
            }
            AddFloorUndergrowth(forest, random);
            geometry.Render("Valley forest stands", forest.Mesh(), 2f, surface);

            // Equal-height trees at different distances make the fog falloff readable from the path.
            foreach ((float distance, float lateral) in new[] { (28f, -12f), (96f, 18f), (220f, -28f), (420f, 34f), (690f, 98f) })
            {
                var marker = new WeatherSampleGeometry.Blocks();
                float z = FloorEyeZ + distance;
                float x = PathCenter(z) + lateral;
                var specimen = new Vector3(x, HeightAt(x, z), z);
                var treeSeed = new System.Random(seed ^ 7343);
                if (distance < 110f)
                    BranchAndLeafTree(marker, treeSeed, specimen, 12f);
                else
                    Tree(marker, treeSeed, specimen, 12f, true, true);
                geometry.Render("Valley landmark " + (int)distance, marker.Mesh(), 2f, surface);
            }
        }

        private static void BranchAndLeafTree(WeatherSampleGeometry.Blocks tree, System.Random random, Vector3 root, float height)
        {
            Color bark = Tone(0x514A3E);
            Color dark = Tone(0x304936);
            Color light = Tone(0x687A42);
            var bend = new Vector3(0.36f, height * 0.49f, 0.28f);
            Vector3 crown = root + new Vector3(-0.15f, height * 0.95f, 0.46f);
            TaperedBranch(tree, root, root + bend, 0.29f, 0.15f, bark);
            TaperedBranch(tree, root + bend, crown, 0.15f, 0.036f, bark);
            for (int branch = 0; branch < 13; branch++)
            {
                float t = branch / 12f;
                float rotated = branch * 2.39996f;
                var facing = new Vector3(Mathf.Cos(rotated), 0f, Mathf.Sin(rotated));
                float reach = Mathf.Lerp(3.1f, 0.82f, t) * Between(random, 0.82f, 1.15f);
                Vector3 joint = root + new Vector3(bend.x * (1f - t), height * Mathf.Lerp(0.40f, 0.88f, t), bend.z);
                Vector3 limb = joint + facing * reach + Vector3.up * Mathf.Lerp(1.85f, 0.52f, t);
                TaperedBranch(tree, joint, limb, Mathf.Lerp(0.12f, 0.041f, t), 0.015f, bark);
                for (int spray = 0; spray < 6; spray++)
                {
                    float angle = rotated + Mathf.PI * (spray - 2.5f) / 4.6f;
                    Vector3 fork = Vector3.Lerp(joint, limb, (spray + 2f) / 8f);
                    Vector3 end = fork + new Vector3(Mathf.Cos(angle), 0.55f, Mathf.Sin(angle)) *
                        Between(random, 0.52f, 1.29f);
                    TaperedBranch(tree, fork, end, 0.016f, 0.006f, bark);
                    for (int leaf = 0; leaf < 57; leaf++)
                    {
                        Vector3 stem = Vector3.Lerp(fork, end, Between(random, 0.32f, 1f));
                        stem += new Vector3(Between(random, -0.56f, 0.56f), Between(random, -0.37f, 0.48f),
                            Between(random, -0.56f, 0.56f));
                        float yaw = Between(random, 0f, Mathf.PI * 2f);
                        Vector3 length = new Vector3(Mathf.Cos(yaw), Between(random, -0.61f, 0.57f), Mathf.Sin(yaw)) *
                            Between(random, 0.12f, 0.26f);
                        Vector3 width = new Vector3(-Mathf.Sin(yaw), 0.17f, Mathf.Cos(yaw)) * Between(random, 0.05f, 0.13f);
                        Color tint = Color.Lerp(dark, light, Between(random, 0.05f, 0.90f));
                        tree.Quad(stem - length, stem - width, stem + length, stem + width, tint);
                        tree.Quad(stem + width, stem + length, stem - width, stem - length, tint);
                    }
                }
            }
        }

        private static void TaperedBranch(WeatherSampleGeometry.Blocks tree, Vector3 from, Vector3 to,
            float baseRadius, float tipRadius, Color color)
        {
            Vector3 axis = (to - from).normalized;
            Vector3 tangent = Vector3.Cross(axis, Mathf.Abs(axis.z) < 0.93f ? Vector3.forward : Vector3.right).normalized;
            Vector3 bitangent = Vector3.Cross(axis, tangent);
            const int sides = 6;
            for (int side = 0; side < sides; side++)
            {
                Vector3 a = Radial(side);
                Vector3 b = Radial(side + 1);
                tree.Quad(from + a * baseRadius, from + b * baseRadius, to + b * tipRadius, to + a * tipRadius, color);
            }

            Vector3 Radial(int side)
            {
                float angle = side * Mathf.PI * 2f / sides;
                return tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle);
            }
        }

        private void AddFloorUndergrowth(WeatherSampleGeometry.Blocks plants, System.Random random)
        {
            var stones = new WeatherSampleGeometry.Blocks();
            for (int index = 0; index < 1380; index++)
            {
                float z = FloorEyeZ + Between(random, 0.7f, 89f);
                float x = PathCenter(z) + Between(random, -20f, 24f);
                if (Mathf.Abs(x - PathCenter(z)) < 2.1f || Noise(x * 0.53f, z * 0.23f) < 0.34f)
                    continue;
                Vector3 p = new Vector3(x, HeightAt(x, z), z);
                float stem = Between(random, 0.14f, 0.59f);
                Color tint = Color.Lerp(Tone(0x354932), Tone(0x7B8251), Between(random, 0f, 1f));
                plants.Triangle(p + Vector3.left * 0.038f, p + new Vector3(0.10f, stem, 0.08f),
                    p + Vector3.right * 0.053f, tint);
                plants.Triangle(p + Vector3.forward * 0.046f, p + new Vector3(-0.15f, stem * 0.80f, -0.04f),
                    p + Vector3.back * 0.048f, tint);
                if (index % 94 == 0)
                    RoundCrown(stones, p - Vector3.up * 0.12f, Between(random, 0.31f, 0.79f),
                        Between(random, 0.25f, 0.76f), Between(random, 0f, 6.28f), 8, Tone(0x4B5350), Tone(0x6E7163));
            }
            geometry.Render("Valley floor rock and scree", stones.Mesh(), 0f, surface);
        }

        private static void Tree(WeatherSampleGeometry.Blocks forest, System.Random random, Vector3 root, float height,
            bool near, bool deciduous = false)
        {
            Color dark = Color.Lerp(Tone(0x27382E), Tone(0x43503A), Between(random, 0f, 1f));
            Color tips = Color.Lerp(Tone(0x3E5744), Tone(0x687551), Between(random, 0f, 1f));
            float reach = height * Between(random, 0.20f, 0.30f);
            float phase = Between(random, 0f, Mathf.PI * 2f);
            bool rounded = deciduous || random.NextDouble() < 0.47;
            if (near)
            {
                float diameter = height * 0.036f;
                forest.Box(root + Vector3.up * (height * 0.30f), new Vector3(diameter, height * 0.60f, diameter), Tone(0x554C3F));
            }
            if (rounded)
            {
                RoundCrown(forest, root + Vector3.up * (height * 0.34f), reach * 1.08f, height * 0.64f,
                    phase, near ? 9 : 6, dark, tips);
                if (near)
                {
                    int clusters = deciduous ? 7 : 3;
                    for (int cluster = 0; cluster < clusters; cluster++)
                    {
                        float azimuth = phase + cluster * 2.4f;
                        float lift = Between(random, 0.32f, 0.58f);
                        float taper = Between(random, 0.39f, 0.68f);
                        Vector3 limb = new Vector3(Mathf.Cos(azimuth) * reach * 0.56f, height * lift,
                            Mathf.Sin(azimuth) * reach * 0.63f);
                        RoundCrown(forest, root + limb, reach * taper, height * Between(random, 0.21f, 0.36f),
                            azimuth, deciduous ? 9 : 7, dark, Color.Lerp(dark, tips, Between(random, 0.57f, 0.87f)));
                    }
                }
            }
            else
            {
                int layers = near ? 5 : 3;
                for (int layer = 0; layer < layers; layer++)
                {
                    float t = layer / (float)(layers - 1);
                    float y = height * Mathf.Lerp(0.20f, 0.71f, t);
                    float h = height * Mathf.Lerp(0.49f, 0.29f, t);
                    Skirt(forest, root + Vector3.up * y, reach * Mathf.Lerp(1f, 0.41f, t), h,
                        phase + layer * 1.11f, near ? 11 : 7, Color.Lerp(dark, tips, t * 0.78f));
                }
            }
        }

        private static void Skirt(WeatherSampleGeometry.Blocks forest, Vector3 root, float radius, float height, float phase,
            int sides, Color color)
        {
            Vector3 top = root + new Vector3(Mathf.Cos(phase) * radius * 0.11f, height, Mathf.Sin(phase) * radius * 0.14f);
            for (int side = 0; side < sides; side++)
                forest.Triangle(Point(side), top, Point((side + 1) % sides), color);

            Vector3 Point(int side)
            {
                float angle = phase + 2f * Mathf.PI * side / sides;
                float length = radius * (0.86f + 0.17f * Mathf.Sin(angle * 3f + phase));
                return root + new Vector3(Mathf.Cos(angle) * length, 0.10f * height * Mathf.Cos(angle * 2f), Mathf.Sin(angle) * length);
            }
        }

        private static void RoundCrown(WeatherSampleGeometry.Blocks forest, Vector3 root, float radius, float height,
            float phase, int sides, Color dark, Color light)
        {
            int rings = sides >= 8 ? 5 : 3;
            int bottom = forest.AddVertex(root, dark);
            int first = bottom + 1;
            for (int ring = 0; ring < rings; ring++)
            for (int side = 0; side < sides; side++)
            {
                float y = (ring + 1f) / (rings + 1f);
                float angle = phase + 2f * Mathf.PI * side / sides;
                float irregular = Mathf.Sin(side * 2.7f + phase + ring * 1.25f);
                float r = radius * Mathf.Sqrt(1f - Mathf.Pow(y * 2f - 1f, 2f)) * (1f + irregular * 0.18f);
                Vector3 point = root + new Vector3(Mathf.Cos(angle) * r, height * (y + irregular * 0.049f), Mathf.Sin(angle) * r);
                forest.AddVertex(point, Color.Lerp(dark, light, y * 0.85f + 0.13f * irregular));
            }
            int top = forest.AddVertex(root + new Vector3(0.12f * Mathf.Cos(phase) * radius, height,
                0.14f * Mathf.Sin(phase) * radius), light);
            int last = first + sides * (rings - 1);
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                forest.AddFace(bottom, first + side, first + next);
                forest.AddFace(last + side, top, last + next);
                for (int ring = 0; ring < rings - 1; ring++)
                {
                    int start = first + ring * sides;
                    forest.AddFace(start + side, start + sides + side, start + sides + next);
                    forest.AddFace(start + side, start + sides + next, start + next);
                }
            }
        }

        private float ForestCover(float x, float z)
        {
            float bank = Smooth(80f, 430f, Mathf.Abs(x - StreamCenter(z)));
            float stands = Noise(x * 0.00084f, z * 0.00108f);
            float gaps = Noise(x * 0.0071f, z * 0.0064f);
            return Smooth(0.26f, 0.67f, stands + (gaps - 0.5f) * 0.23f) * (0.32f + 0.68f * bank);
        }

        private static float CenterOfValley(float z)
        {
            float along = z - FloorEyeZ;
            return FloorEyeX + 492f * Mathf.Sin(along * 0.00059f) + 172f * Mathf.Sin(along * 0.00118f);
        }

        private static float PathCenter(float z) =>
            FloorEyeX + (z - FloorEyeZ) * TrackGrade + Mathf.Sin((z - FloorEyeZ) * TrackFrequency) * TrackBend;
        private static float StreamCenter(float z) => CenterOfValley(z) + 250f + 98f * Mathf.Sin(z * 0.00172f + 1.1f);

        private static float Spur(float x, float z, float east, float north, float width, float length, float height, float angle)
        {
            float v = (z - north) / length;
            float u = (x - east - (z - north) * angle) / width;
            return height * Mathf.Exp(-u * u - v * v);
        }

        private float Noise(float x, float z) => Mathf.PerlinNoise(x + 19.14f + seedOffset, z + 33.28f - seedOffset);
        private static float Smooth(float low, float high, float value) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(low, high, value));
        private static Color Tone(int rgb) => WeatherSamplePresets.Tone(rgb);
        private static float Between(System.Random random, float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

        private static int Cell(float[] axis, float position)
        {
            int match = Array.BinarySearch(axis, position);
            return Mathf.Clamp(match >= 0 ? match : ~match - 1, 0, axis.Length - 2);
        }

        private static float[] Axis(float start, params (float end, float step)[] spans)
        {
            var axis = new List<float> { start };
            foreach ((float end, float step) in spans)
            {
                int count = Mathf.CeilToInt((end - start) / step);
                for (int index = 1; index <= count; index++)
                    axis.Add(Mathf.Lerp(start, end, (float)index / count));
                start = end;
            }
            return axis.ToArray();
        }
    }
}
