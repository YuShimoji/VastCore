using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.Weather
{
    internal sealed class WeatherSampleGeometry : IDisposable
    {
        private static readonly Rect MountainBounds = new Rect(-23500f, -6200f, 47000f, 47800f);
        private static readonly Rect CoastBounds = new Rect(-23300f, -7100f, 46600f, 49200f);
        private static readonly Rect PlainBounds = new Rect(-31000f, -3800f, 62000f, 47000f);
        private const int MountainCells = 192;
        private const int CoastCells = 192;
        private const int PlainCells = 128;
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> materials = new List<Material>();
        private readonly List<WeatherPuddle> puddles = new List<WeatherPuddle>();
        private readonly List<MeshRenderer> road = new List<MeshRenderer>();
        private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();
        private readonly Transform root;
        private readonly WeatherField weather;
        private readonly Material material;
        private readonly float variation;
        private readonly int seed;

        public WeatherSampleGeometry(Transform root, WeatherField weather, Color haze, Color sunlight,
            Vector3 toSun, float hazePerKilometer, int seed)
        {
            this.root = root;
            this.weather = weather;
            this.seed = seed;
            variation = (seed & 255) * 0.0727f;
            material = WeatherField.NewMaterial("SampleSurface");
            materials.Add(material);
            material.SetColor("_Horizon", haze);
            material.SetColor("_Sun", sunlight);
            material.SetVector("_SunDirection", toSun);
            material.SetFloat("_DistantHaze", hazePerKilometer * 0.001f);
        }

        public void Build(WeatherSampleSite site)
        {
            switch (site)
            {
                case WeatherSampleSite.StreetThunderstorm:
                    BuildStreet();
                    break;
                case WeatherSampleSite.CumulonimbusAcrossSea:
                case WeatherSampleSite.MorningGlory:
                    BuildCoast();
                    break;
                case WeatherSampleSite.GreatPlainsSupercell:
                    BuildPlains();
                    break;
                default:
                    BuildMountains();
                    break;
            }
        }

        public float MountainHeight(float x, float z) => MeshHeight(MountainBounds, MountainCells, MountainTerrain, x, z);
        internal float MountainLandform(float x, float z) => MountainTerrain(x, z);
        public float CoastHeight(float x, float z) => MeshHeight(CoastBounds, CoastCells, CoastTerrain, x, z);
        public float PlainHeight(float x, float z) => MeshHeight(PlainBounds, PlainCells, PlainTerrain, x, z);

        private void BuildMountains()
        {
            Heightfield("Mountain ridges and valley", MountainBounds, MountainCells, MountainTerrain,
                (x, y, z) =>
                {
                    float shift = Noise(x * 0.00088f, z * 0.00061f);
                    Color grass = Color.Lerp(Tone(0x47604E), Tone(0x7D8160), shift);
                    Color rock = Color.Lerp(Tone(0x5D6465), Tone(0xA2A49B), shift);
                    return Color.Lerp(grass, rock, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(890f, 2340f, y)));
                });
            var stands = new Blocks();
            var random = new System.Random(seed ^ 611);
            for (int index = 0; index < 245; index++)
            {
                float x = RandomRange(random, -3500f, 2800f);
                float z = RandomRange(random, -2600f, 2800f);
                float y = MountainHeight(x, z);
                if (y > 1590f || Mathf.Abs(x + 350f) < 30f)
                    continue;
                float height = RandomRange(random, 4.2f, 19f);
                Vector3 at = new Vector3(x, y, z);
                stands.Cone(at + Vector3.up * height * 0.13f, height * 0.26f, height * 0.70f, Tone(0x253D34));
                stands.Cone(at + Vector3.up * height * 0.42f, height * 0.17f, height * 0.58f, Tone(0x334D40));
            }
            Render("Pines", stands.Mesh());
        }

        private float MountainTerrain(float x, float z)
        {
            float bend = Mathf.Sin(z * 0.00031f + variation * 0.3f) * 690f + Mathf.Sin(z * 0.00011f) * 580f;
            float cross = x - bend;
            float saddle = 0.62f + 0.38f * Noise(x * 0.00029f, z * 0.00042f);
            float broad = Mathf.Max(
                Bell((cross + 4550f + Mathf.Sin(z * 0.00037f) * 1200f) / 2080f) *
                    (1470f + Mathf.Cos(z * 0.00091f) * 365f),
                Bell((cross - 4580f - Mathf.Cos(z * 0.00034f) * 1580f) / 2280f) *
                    (1890f + Mathf.Sin(z * 0.00073f + 2.7f) * 380f));
            float far = Bell((z - 14600f) / 6700f) *
                (780f + Noise(x * 0.00019f, z * 0.00079f) * 1410f);
            float backRidge = Bell((z - 25700f + x * 0.19f) / 5100f) *
                (1450f + Noise(x * 0.00024f + 6.4f, z * 0.00058f) * 1270f);
            float foothills = Bell((cross - 1330f) / 1600f) * (140f + 340f * Noise(x * 0.00024f, z * 0.00049f));
            float body = Mathf.Max(broad * saddle, Mathf.Max(far, backRidge));
            float rugged = (Noise(x * 0.00118f, z * 0.00122f) - 0.47f) * Mathf.Min(570f, body * 0.37f);
            return 29f + foothills * 0.48f + body + rugged;
        }

        private void BuildCoast()
        {
            Heightfield("Coastal headlands", CoastBounds, CoastCells, CoastTerrain,
                (x, y, z) =>
                {
                    float t = Mathf.InverseLerp(0f, 122f, y);
                    Color grass = Color.Lerp(Tone(0x45594A), Tone(0x83805D), Noise(x * 0.001f, z * 0.001f));
                    return Color.Lerp(Tone(0xB3A58D), grass, Mathf.SmoothStep(0f, 1f, t));
                });
            var sea = new Blocks();
            sea.Box(new Vector3(0f, -2.5f, 45000f), new Vector3(180000f, 5f, 104000f), Tone(0x456778));
            Render("Open water", sea.Mesh(), 1f);

            var landmarks = new Blocks();
            for (int index = 0; index < 19; index++)
            {
                float x = -1210f + index * 23f;
                float z = -650f - index * 4f;
                float y = CoastHeight(x, z);
                if (y < 1f)
                    continue;
                landmarks.Box(new Vector3(x, y + 0.66f, z), new Vector3(0.20f, 1.31f, 0.23f), Tone(0xAAA892));
            }
            float headland = CoastHeight(-1190f, -810f);
            landmarks.Box(new Vector3(-1190f, headland + 6f, -810f), new Vector3(5f, 12f, 5f), Tone(0xD6CEBC));
            landmarks.Box(new Vector3(-1190f, headland + 12.8f, -810f), new Vector3(6f, 2.1f, 6f), Tone(0x523C37));
            Render("Coast features", landmarks.Mesh());
        }

        private float CoastTerrain(float x, float z)
        {
            float edge = Mathf.Sin(x * 0.00087f + variation) * 435f + Mathf.Sin(x * 0.00182f + 2.3f) * 190f - 170f;
            float slope = Mathf.Clamp((edge - z) * 0.18f, -58f, 160f);
            float land = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-60f, 650f, edge - z));
            float nearby = Mathf.Max(0f, 1f - Mathf.Abs(x + 2280f) / 5400f) * land *
                Bell((z + 790f) / 1920f) * (110f + Noise(x * 0.0015f, z * 0.0012f) * 260f);
            float far = Bell((z - 26200f - Mathf.Sin(x * 0.0003f + variation) * 1900f) / 3180f);
            far *= Bell((x + 1700f) / 11000f) * (440f + Noise(x * 0.0005f, z * 0.0007f) * 1690f);
            return Mathf.Max(slope + nearby, far - 62f);
        }

        private void BuildPlains()
        {
            Heightfield("Plains", PlainBounds, PlainCells, PlainTerrain, (x, y, z) =>
            {
                float field = Noise(x * 0.0018f, z * 0.0016f);
                return Color.Lerp(Tone(0x455348), Tone(0x8B805D), field);
            });
            var roadbed = new Blocks();
            for (int index = 0; index < 88; index++)
            {
                float z0 = -650f + index * 42f;
                float z1 = z0 + 42f;
                Vector3 a = new Vector3(-2.1f, PlainHeight(-2.1f, z0) + 0.13f, z0);
                Vector3 b = new Vector3(2.1f, PlainHeight(2.1f, z0) + 0.13f, z0);
                Vector3 c = new Vector3(2.1f, PlainHeight(2.1f, z1) + 0.13f, z1);
                Vector3 d = new Vector3(-2.1f, PlainHeight(-2.1f, z1) + 0.13f, z1);
                roadbed.Quad(a, d, c, b, Tone(0xAD9780));
                if (index % 2 == 0 && index > 4)
                {
                    float ground = PlainHeight(-12f, z0);
                    roadbed.Box(new Vector3(-12f, ground + 4.1f, z0), new Vector3(0.19f, 8.2f, 0.2f), Tone(0x51473F));
                    roadbed.Box(new Vector3(-12f, ground + 7.31f, z0), new Vector3(2.35f, 0.13f, 0.14f), Tone(0x51473F));
                }
            }
            Render("Rural road", roadbed.Mesh());
            var farm = new Blocks();
            float hill = PlainHeight(87f, 167f);
            farm.Box(new Vector3(87f, hill + 2.6f, 167f), new Vector3(17f, 5.2f, 9f), Tone(0x6E5951));
            farm.Box(new Vector3(87f, hill + 5.4f, 167f), new Vector3(18.4f, 0.56f, 10.4f), Tone(0x504E48));
            farm.Cone(new Vector3(105f, hill, 171f), 3.9f, 13.1f, Tone(0xA7A695));
            Render("Farm buildings", farm.Mesh());
        }

        private float PlainTerrain(float x, float z)
        {
            float local = Noise(x * 0.00092f, z * 0.00055f) * 8f;
            float hills = Noise(x * 0.00022f + 13f, z * 0.00016f) * 79f;
            return local + Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(45f, 3150f, Mathf.Abs(x))) * hills;
        }

        private void BuildStreet()
        {
            var walkway = new Blocks();
            walkway.Box(new Vector3(0f, -0.61f, 58f), new Vector3(900f, 0.8f, 800f), Tone(0x5C6267));
            var random = new System.Random(seed);
            for (int index = 0; index < 27; index++)
            {
                float z = -86f + index * 13f;
                var asphalt = new Blocks();
                asphalt.Box(new Vector3(0f, -0.085f, z), new Vector3(12.4f, 0.15f, 13.03f), Tone(0x525761));
                road.Add(Render("Asphalt", asphalt.Mesh(), 2f));
                walkway.Box(new Vector3(-7.9f, -0.016f, z), new Vector3(3.45f, 0.34f, 13.03f), Tone(0x83838A));
                walkway.Box(new Vector3(7.9f, -0.016f, z), new Vector3(3.45f, 0.34f, 13.03f), Tone(0x87858A));
                walkway.Box(new Vector3(0f, -0.003f, z - 3f), new Vector3(0.14f, 0.009f, 3.5f), Tone(0xB9AE92));
                walkway.Box(new Vector3(-5.79f, 0.002f, z), new Vector3(0.10f, 0.009f, 12.9f), Tone(0xABA697));
                walkway.Box(new Vector3(5.79f, 0.002f, z), new Vector3(0.10f, 0.009f, 12.9f), Tone(0xABA697));
            }
            for (int stripe = 0; stripe < 13; stripe++)
                walkway.Box(new Vector3(-5.47f + stripe * 0.91f, 0.005f, -8f), new Vector3(0.43f, 0.013f, 2.35f), Tone(0xC2BEB0));

            var buildings = new Blocks();
            for (int side = -1; side <= 1; side += 2)
            {
                for (int index = 0; index < 17; index++)
                {
                    float z = -57f + index * 18.7f;
                    float height = RandomRange(random, 8.5f, 22f) + (index > 7 ? RandomRange(random, 1f, 16f) : 0f);
                    float width = RandomRange(random, 5.9f, 10.5f);
                    float length = RandomRange(random, 12.7f, 16.5f);
                    float inner = 9.7f + RandomRange(random, 0f, 1.6f);
                    Color plaster = Color.Lerp(Tone(0x5C6572), Tone(0xA49789), RandomRange(random, 0f, 1f));
                    var mid = new Vector3(side * (inner + width * 0.5f), height * 0.5f, z);
                    buildings.Box(mid, new Vector3(width, height, length), plaster);
                    buildings.Box(mid + Vector3.up * height * 0.5f, new Vector3(width + 0.34f, 0.47f, length + 0.37f),
                        Color.Lerp(plaster, Tone(0x464D55), 0.25f));
                    buildings.Box(mid + new Vector3(side * 0.7f, height * 0.5f + 0.72f, -3.6f),
                        new Vector3(width * 0.38f, 1.55f, length * 0.42f), Color.Lerp(plaster, Tone(0x464D55), 0.38f));
                    buildings.Box(new Vector3(side * (inner - 0.091f), 1.05f, z), new Vector3(0.19f, 2.15f, length * 0.89f),
                        Color.Lerp(plaster, Tone(0x363C45), 0.49f));
                    for (float floor = 2.72f; floor < height - 1.1f; floor += 2.7f)
                    {
                        for (float along = -length * 0.5f + 1.28f; along < length * 0.5f - 0.54f; along += 2.21f)
                        {
                            Color glass = random.NextDouble() > 0.87 ? Tone(0xC9A27A) : Tone(0x353E4B);
                            buildings.Box(new Vector3(side * (inner - 0.027f), floor, z + along), new Vector3(0.07f, 1.44f, 1.14f), glass);
                            buildings.Box(new Vector3(side * (inner - 0.069f), floor - 0.73f, z + along),
                                new Vector3(0.17f, 0.085f, 1.26f), Tone(0xA7A29D));
                        }
                        for (float across = 1.25f; across < width - 0.5f; across += 2.07f)
                        {
                            Color glass = random.NextDouble() > 0.94 ? Tone(0xBA986F) : Tone(0x454B57);
                            buildings.Box(new Vector3(side * (inner + across), floor, z - length * 0.5f - 0.025f),
                                new Vector3(1.15f, 1.48f, 0.075f), glass);
                        }
                    }
                    if (index % 2 == 0)
                    {
                        float sign = side * (inner - 0.43f);
                        buildings.Box(new Vector3(sign, 3.02f, z - 1.5f), new Vector3(1.16f, 0.26f, 5.1f),
                            index % 3 == 0 ? Tone(0x734641) : Tone(0x456367));
                    }
                    if (index % 2 == 1)
                    {
                        float pole = side * 7.05f;
                        buildings.Box(new Vector3(pole, 2.86f, z + 5.9f), new Vector3(0.14f, 5.72f, 0.14f), Tone(0x384249));
                        buildings.Box(new Vector3(pole - side * 0.55f, 5.66f, z + 5.9f),
                            new Vector3(1.16f, 0.13f, 0.25f), Tone(0xAAA293));
                    }
                }
            }
            for (int index = 0; index < 44; index++)
            {
                float x = RandomRange(random, -151f, 161f);
                float z = RandomRange(random, 220f, 535f);
                float y = RandomRange(random, 13f, 44f) * (index % 4 == 0 ? 1.8f : 1f);
                buildings.Box(new Vector3(x, y * 0.5f, z), new Vector3(RandomRange(random, 8f, 20f), y,
                    RandomRange(random, 10f, 28f)), Color.Lerp(Tone(0x516271), Tone(0x8E918E), RandomRange(random, 0f, 1f)));
            }
            Render("Sidewalks and street markings", walkway.Mesh());
            Render("Buildings and street fixtures", buildings.Mesh());

            var grates = new Blocks();
            foreach (float x in new[] { -5.42f, 5.39f })
            {
                grates.Box(new Vector3(x, 0.017f, -25.9f), new Vector3(0.42f, 0.03f, 0.64f), Tone(0x302F34));
                for (int bar = 0; bar < 6; bar++)
                    grates.Box(new Vector3(x - 0.15f + 0.06f * bar, 0.038f, -25.9f),
                        new Vector3(0.022f, 0.012f, 0.58f), Tone(0x65696C));
            }
            Render("Storm drains", grates.Mesh());

            AddPuddle(-4.12f, -26.5f, 2.87f, 7.6f, 5f, 1);
            AddPuddle(-3.82f, -15.9f, 2.22f, 4.1f, -12f, 2);
            AddPuddle(3.95f, -23.6f, 3.57f, 8.4f, -9f, 3);
            AddPuddle(3.56f, 7.9f, 2.8f, 5.9f, 14f, 4);
            AddPuddle(-4.8f, 15f, 1.42f, 7.01f, 12f, 5);
            AddPuddle(4.83f, 25f, 1.43f, 7.85f, -9f, 6);
        }

        private void AddPuddle(float x, float z, float width, float length, float yaw, int order)
        {
            var owner = NewObject("Curb puddle " + order);
            owner.transform.localPosition = new Vector3(x, 0.014f, z);
            owner.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var puddle = owner.AddComponent<WeatherPuddle>();
            puddle.Configure(weather, new Vector2(width, length), Tone(0x292E38), Tone(0x8EA1B3), seed + order * 83);
            puddles.Add(puddle);
        }

        public void UpdateWetness()
        {
            foreach (WeatherPuddle puddle in puddles)
                puddle.Refresh();
            foreach (MeshRenderer segment in road)
            {
                block.Clear();
                block.SetFloat("_SurfaceKind", 2f);
                block.SetFloat("_Wetness", weather.WetnessAt(segment.bounds.center));
                segment.SetPropertyBlock(block);
            }
        }

        private void Heightfield(string label, Rect region, int cells, Func<float, float, float> elevation,
            Func<float, float, float, Color> color)
        {
            int stride = cells + 1;
            var positions = new Vector3[stride * stride];
            var colors = new Color[positions.Length];
            var indices = new int[cells * cells * 6];
            for (int z = 0; z <= cells; z++)
            for (int x = 0; x <= cells; x++)
            {
                int vertex = z * stride + x;
                float east = region.xMin + region.width * x / cells;
                float north = region.yMin + region.height * z / cells;
                float y = elevation(east, north);
                positions[vertex] = new Vector3(east, y, north);
                colors[vertex] = color(east, y, north).linear;
                if (x == cells || z == cells)
                    continue;
                int triangle = (z * cells + x) * 6;
                indices[triangle] = vertex;
                indices[triangle + 1] = vertex + stride;
                indices[triangle + 2] = vertex + stride + 1;
                indices[triangle + 3] = vertex;
                indices[triangle + 4] = vertex + stride + 1;
                indices[triangle + 5] = vertex + 1;
            }
            var mesh = new Mesh { vertices = positions, colors = colors, triangles = indices };
            mesh.RecalculateNormals();
            Render(label, mesh);
        }

        private static float MeshHeight(Rect region, int cells, Func<float, float, float> height, float x, float z)
        {
            float width = region.width / cells;
            float length = region.height / cells;
            float gx = Mathf.Clamp((x - region.xMin) / width, 0f, cells - 0.001f);
            float gz = Mathf.Clamp((z - region.yMin) / length, 0f, cells - 0.001f);
            float fx = gx - Mathf.Floor(gx);
            float fz = gz - Mathf.Floor(gz);
            float x0 = region.xMin + Mathf.Floor(gx) * width;
            float z0 = region.yMin + Mathf.Floor(gz) * length;
            float a = height(x0, z0);
            float c = height(x0 + width, z0 + length);
            return fx < fz
                ? a * (1f - fz) + height(x0, z0 + length) * (fz - fx) + c * fx
                : a * (1f - fx) + height(x0 + width, z0) * (fx - fz) + c * fz;
        }

        private GameObject NewObject(string label)
        {
            var child = new GameObject(label) { hideFlags = HideFlags.DontSave };
            child.transform.SetParent(root, false);
            return child;
        }

        internal Material CreateSurface(string shader)
        {
            Material surface = WeatherField.NewMaterial(shader);
            surface.CopyMatchingPropertiesFromMaterial(material);
            materials.Add(surface);
            return surface;
        }

        internal MeshRenderer Render(string label, Mesh mesh, float kind = 0f, Material surface = null)
        {
            mesh.name = label;
            mesh.hideFlags = HideFlags.HideAndDontSave;
            meshes.Add(mesh);
            GameObject child = NewObject(label);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = surface ? surface : material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            block.Clear();
            block.SetFloat("_SurfaceKind", kind);
            renderer.SetPropertyBlock(block);
            return renderer;
        }

        public void Dispose()
        {
            foreach (Mesh mesh in meshes)
                WeatherField.Dispose(mesh);
            foreach (Material surface in materials)
                WeatherField.Dispose(surface);
        }

        private float Noise(float x, float z) => Mathf.PerlinNoise(x + 18.3f + variation, z + 23.7f - variation * 1.7f);
        private static float Bell(float value) => Mathf.Exp(-value * value);
        private static float RandomRange(System.Random random, float low, float high) =>
            Mathf.Lerp(low, high, (float)random.NextDouble());
        private static Color Tone(int rgb) => WeatherSamplePresets.Tone(rgb);

        internal sealed class Blocks
        {
            private readonly List<Vector3> positions = new List<Vector3>();
            private readonly List<Color> colors = new List<Color>();
            private readonly List<int> triangles = new List<int>();

            public void Box(Vector3 center, Vector3 size, Color color)
            {
                Vector3 half = size * 0.5f;
                Vector3 a = center + new Vector3(-half.x, -half.y, -half.z);
                Vector3 b = center + new Vector3(half.x, -half.y, -half.z);
                Vector3 c = center + new Vector3(half.x, half.y, -half.z);
                Vector3 d = center + new Vector3(-half.x, half.y, -half.z);
                Vector3 e = center + new Vector3(-half.x, -half.y, half.z);
                Vector3 f = center + new Vector3(half.x, -half.y, half.z);
                Vector3 g = center + new Vector3(half.x, half.y, half.z);
                Vector3 h = center + new Vector3(-half.x, half.y, half.z);
                Quad(a, d, c, b, color);
                Quad(e, f, g, h, color);
                Quad(a, e, h, d, color);
                Quad(b, c, g, f, color);
                Quad(d, h, g, c, color);
                Quad(a, b, f, e, color);
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
            {
                int first = positions.Count;
                positions.Add(a);
                positions.Add(b);
                positions.Add(c);
                positions.Add(d);
                Color linear = color.linear;
                for (int index = 0; index < 4; index++)
                    colors.Add(linear);
                triangles.Add(first);
                triangles.Add(first + 1);
                triangles.Add(first + 2);
                triangles.Add(first);
                triangles.Add(first + 2);
                triangles.Add(first + 3);
            }

            public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color)
            {
                int first = positions.Count;
                positions.Add(a);
                positions.Add(b);
                positions.Add(c);
                Color linear = color.linear;
                colors.Add(linear);
                colors.Add(linear);
                colors.Add(linear);
                triangles.Add(first);
                triangles.Add(first + 1);
                triangles.Add(first + 2);
            }

            public int AddVertex(Vector3 position, Color color)
            {
                positions.Add(position);
                colors.Add(color.linear);
                return positions.Count - 1;
            }

            public void AddFace(int a, int b, int c)
            {
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);
            }

            public void Cone(Vector3 baseCenter, float radius, float height, Color color)
            {
                const int facets = 7;
                for (int index = 0; index < facets; index++)
                {
                    float a = 2f * Mathf.PI * index / facets;
                    float b = 2f * Mathf.PI * (index + 1) / facets;
                    Vector3 left = baseCenter + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                    Vector3 right = baseCenter + new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b)) * radius;
                    Triangle(left, baseCenter + Vector3.up * height, right, color);
                }
            }

            public Mesh Mesh()
            {
                var mesh = new Mesh { indexFormat = positions.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(positions);
                mesh.SetColors(colors);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                return mesh;
            }
        }
    }
}
