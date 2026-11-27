using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.Weather
{
    [Serializable]
    public sealed class WeatherStreetAssets
    {
        public Texture2D AsphaltColor;
        public Texture2D AsphaltNormal;
        public Texture2D AsphaltRoughness;
        public Texture2D PavingColor;
        public Texture2D PavingNormal;
        public Texture2D PavingRoughness;
        public GameObject CornerCafe;
        public GameObject BrickShops;
        public GameObject CornerOffices;
        public GameObject Drain;
        public GameObject StreetLamp;

        internal bool Available => AsphaltColor && AsphaltNormal && AsphaltRoughness &&
            PavingColor && PavingNormal && PavingRoughness && CornerCafe && BrickShops && CornerOffices && Drain;
    }

    internal sealed class WeatherStreetScene : IDisposable
    {
        private const float RoadHalfWidth = 6.15f;
        private const float WalkTop = 0.145f;
        private const float WaterLevel = -0.028f;
        private static readonly Vector3 PoolCenter = new Vector3(-5.05f, WaterLevel, -25.5f);
        private static readonly Vector4 AsphaltRepair = new Vector4(1.27f, -23.8f, 1.35f, 4.4f);
        private readonly Transform root;
        private readonly WeatherField weather;
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> materials = new List<Material>();
        private readonly List<MeshRenderer> wetSurfaces = new List<MeshRenderer>();
        private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();
        private readonly Material asphalt;
        private readonly WeatherPuddle pool;
        private readonly WeatherStreetReflection reflection;
        private readonly int seed;

        public WeatherStreetScene(Transform root, WeatherField field, WeatherStreetAssets assets, Color haze, int seed)
        {
            this.root = root;
            weather = field;
            this.seed = seed;
            asphalt = MakeSurface(assets.AsphaltColor, assets.AsphaltNormal, assets.AsphaltRoughness,
                2f, haze, Color.white, 1.0f);
            asphalt.SetVector("_Repair", AsphaltRepair);
            Vector3 basin = root.TransformPoint(PoolCenter);
            asphalt.SetVector("_RunoffCenter", new Vector4(basin.x, basin.y, basin.z, 0f));
            asphalt.SetFloat("_Exposure", 1.66f);
            Material paving = MakeSurface(assets.PavingColor, assets.PavingNormal, assets.PavingRoughness,
                2f, haze, WeatherSamplePresets.Tone(0xD9D4C9), 0.66f);
            paving.SetFloat("_Exposure", 1.27f);
            Material curb = MakeSurface(assets.PavingColor, assets.PavingNormal, assets.PavingRoughness,
                0.64f, haze, WeatherSamplePresets.Tone(0xB7ADA0), 0.69f);
            curb.SetFloat("_TextureWeight", 0.47f);
            BuildRoad(asphalt);
            BuildWalkways(paving, curb);
            BuildMarkings();
            BuildBlocks(assets, haze);

            var puddle = PlaceObject("Curb depression water");
            puddle.transform.localPosition = PoolCenter;
            pool = puddle.AddComponent<WeatherPuddle>();
            pool.Configure(weather, new Vector2(2.60f, 10.4f), WeatherSamplePresets.Tone(0x323331),
                haze, seed);
            pool.UseGroundShore(0.013f);
            reflection = new WeatherStreetReflection(root);
        }

        public bool ReflectionAvailable { get; private set; }

        public void RefreshWeather()
        {
            pool.Refresh();
            foreach (MeshRenderer surface in wetSurfaces)
            {
                Bounds bounds = surface.bounds;
                Vector3 min = bounds.min;
                Vector3 max = bounds.max;
                properties.Clear();
                properties.SetVector("_WetBounds", new Vector4(min.x, min.z, 1f / Mathf.Max(0.01f, bounds.size.x),
                    1f / Mathf.Max(0.01f, bounds.size.z)));
                properties.SetVector("_WetCorners", new Vector4(
                    weather.WetnessAt(new Vector3(min.x, 0, min.z)), weather.WetnessAt(new Vector3(max.x, 0, min.z)),
                    weather.WetnessAt(new Vector3(min.x, 0, max.z)), weather.WetnessAt(new Vector3(max.x, 0, max.z))));
                surface.SetPropertyBlock(properties);
            }
        }

        public void RenderReflection(Camera camera, bool enabled, bool forceRefresh)
        {
            ReflectionAvailable = enabled && reflection.Render(camera, pool, asphalt, weather.gameObject.layer,
                weather.WeatherSeconds, forceRefresh);
            if (!ReflectionAvailable)
            {
                pool.SetReflection(null, Matrix4x4.identity);
                WeatherStreetReflection.ClearFilm(asphalt);
            }
        }

        internal static float RoadHeight(float x, float z, int seed)
        {
            // The crown drains toward both edges. A local settlement beside the left drain retains water.
            float crown = 0.067f - 0.0135f * Mathf.Abs(x);
            float gutter = Mathf.Exp(-Mathf.Pow((Mathf.Abs(x) - 5.79f) / 0.32f, 2f)) * 0.011f;
            float along = z - PoolCenter.z;
            float offset = Mathf.Sin(along * 1.54f + seed * 0.12f) * 0.09f + along * -0.029f;
            float side = (x - PoolCenter.x - offset) / 1.07f;
            float length = along / 5.2f;
            float rim = Mathf.Sqrt(side * side + length * length) +
                Mathf.Sin(5.7f * along + seed * 0.37f) * 0.051f + Mathf.Sin(1.8f * along + 1.1f) * 0.065f;
            float lowPoint = (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 1.10f, rim))) * 0.063f;
            float patch = Mathf.Max(Mathf.Abs(x - AsphaltRepair.x) / AsphaltRepair.z, Mathf.Abs(z - AsphaltRepair.y) / AsphaltRepair.w);
            float settlement = (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.79f, 1.06f, patch))) * 0.007f;
            return crown - gutter - lowPoint - settlement + Mathf.Sin(z * 2.06f + x * 0.41f) * 0.0011f;
        }

        private void BuildRoad(Material material)
        {
            float[] boundaries = { -113f, -62f, -42f, -12f, 2f, 22f, 54f, 96f, 166f, 271f };
            for (int index = 0; index < boundaries.Length - 1; index++)
            {
                float start = boundaries[index];
                float end = boundaries[index + 1];
                int steps = index == 2 ? 172 : Mathf.CeilToInt((end - start) * 1.15f);
                Mesh road = CreateRoadSegment(start, end, steps);
                wetSurfaces.Add(RenderMesh("Graded asphalt", road, material));
            }
        }

        private Mesh CreateRoadSegment(float start, float end, int rows)
        {
            const int columns = 64;
            int stride = columns + 1;
            var positions = new Vector3[stride * (rows + 1)];
            var indices = new int[rows * columns * 6];
            for (int row = 0; row <= rows; row++)
            for (int column = 0; column <= columns; column++)
            {
                int vertex = row * stride + column;
                float x = Mathf.Lerp(-RoadHalfWidth, RoadHalfWidth, column / (float)columns);
                float z = Mathf.Lerp(start, end, row / (float)rows);
                positions[vertex] = new Vector3(x, RoadHeight(x, z, seed), z);
                if (column == columns || row == rows)
                    continue;
                int triangle = (row * columns + column) * 6;
                indices[triangle] = vertex;
                indices[triangle + 1] = vertex + stride;
                indices[triangle + 2] = vertex + stride + 1;
                indices[triangle + 3] = vertex;
                indices[triangle + 4] = vertex + stride + 1;
                indices[triangle + 5] = vertex + 1;
            }
            var mesh = new Mesh { vertices = positions, triangles = indices };
            mesh.RecalculateNormals();
            return mesh;
        }

        private void BuildWalkways(Material paving, Material curb)
        {
            Color white = Color.white;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int segment = 0; segment < 14; segment++)
                {
                    float middle = -86f + segment * 25f;
                    var walk = new WeatherSampleGeometry.Blocks();
                    walk.Box(new Vector3(side * 8.42f, WalkTop - 0.135f, middle), new Vector3(4.27f, 0.27f, 25.005f), white);
                    wetSurfaces.Add(RenderMesh("Paved sidewalk", walk.Mesh(), paving));
                    var edge = new WeatherSampleGeometry.Blocks();
                    const int blocksPerSegment = 20;
                    const float blockLength = 25f / blocksPerSegment;
                    for (int block = 0; block < blocksPerSegment; block++)
                    {
                        float start = middle - 12.5f + block * blockLength;
                        if (segment >= 1 && segment <= 3)
                        {
                            float top = WalkTop + 0.009f + Mathf.Sin(start * 3.31f + side) * 0.004f;
                            BuildCurbStone(edge, side, start + 0.007f, start + blockLength - 0.007f, top);
                        }
                        else
                        {
                            edge.Box(new Vector3(side * 6.245f, 0.043f, start + blockLength * 0.5f),
                                new Vector3(0.192f, 0.214f, blockLength - 0.014f), white);
                        }
                    }
                    wetSurfaces.Add(RenderMesh("Jointed curb", edge.Mesh(), curb));
                }
            }
        }

        private static void BuildCurbStone(WeatherSampleGeometry.Blocks blocks, int side, float start, float end, float top)
        {
            float inner = side * 6.148f;
            float outer = side * 6.337f;
            float bevel = top - 0.028f;
            float topEdge = inner + side * 0.034f;
            Vector3[] section =
            {
                new Vector3(inner, -0.064f, start), new Vector3(outer, -0.064f, start),
                new Vector3(outer, bevel, start), new Vector3(outer, top, start),
                new Vector3(topEdge, top, start), new Vector3(inner, bevel, start)
            };
            Vector3 length = Vector3.forward * (end - start);
            for (int index = 0; index < section.Length; index++)
            {
                Vector3 a = section[index];
                Vector3 b = section[(index + 1) % section.Length];
                Face(a, b, b + length, a + length);
            }
            Face(section[0], section[5], section[2], section[1]);
            Face(section[5], section[4], section[3], section[2]);
            Face(section[1] + length, section[2] + length, section[5] + length, section[0] + length);
            Face(section[2] + length, section[3] + length, section[4] + length, section[5] + length);

            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                if (side > 0)
                    blocks.Quad(a, b, c, d, Color.white);
                else
                    blocks.Quad(d, c, b, a, Color.white);
            }
        }

        private void BuildMarkings()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            var paint = new Material(shader) { name = "Street Paint", hideFlags = HideFlags.HideAndDontSave };
            paint.SetColor("_BaseColor", WeatherSamplePresets.Tone(0xB4A889));
            paint.SetFloat("_Smoothness", 0.17f);
            materials.Add(paint);
            var marks = new WeatherSampleGeometry.Blocks();
            for (int index = 0; index < 34; index++)
            {
                float z = -93f + index * 8.4f;
                Mark(marks, -0.07f, 0.07f, z, z + 3.0f);
            }
            int crossingCount = 11;
            for (int stripe = 0; stripe < crossingCount; stripe++)
            {
                float x = -5.25f + stripe * 0.97f;
                Mark(marks, x, x + 0.47f, -8.7f, -6.13f);
            }
            RenderMesh("Street paint", marks.Mesh(), paint);
        }

        private void BuildBackground(Color haze, float left, float right)
        {
            var ground = new WeatherSampleGeometry.Blocks();
            ground.Box(new Vector3(0f, -0.60f, 52f), new Vector3(860f, 0.72f, 780f),
                WeatherSamplePresets.Tone(0x444644));
            Material distant = WeatherField.NewMaterial("SampleSurface");
            distant.SetColor("_Horizon", haze);
            distant.SetFloat("_DistantHaze", 0.0018f);
            materials.Add(distant);
            RenderMesh("Block ground", ground.Mesh(), distant);
            var skyline = new WeatherSampleGeometry.Blocks();
            var random = new System.Random(seed ^ 334);
            for (int side = -1; side <= 1; side += 2)
            {
                float z = (side < 0 ? left : right) + 3.8f;
                for (int index = 0; index < 5; index++)
                {
                    float height = 11.3f + (float)random.NextDouble() * 11f;
                    float length = 12f + (float)random.NextDouble() * 11f;
                    float x = side * (13.67f + (float)random.NextDouble() * 2.2f);
                    float nearWall = x - side * 4.75f;
                    float middle = z + length * 0.5f;
                    Color wall = Color.Lerp(WeatherSamplePresets.Tone(0x6C6F69), WeatherSamplePresets.Tone(0xA09C90),
                        (float)random.NextDouble());
                    skyline.Box(new Vector3(x, WalkTop + height * 0.5f, middle), new Vector3(9.5f, height, length), wall);
                    skyline.Box(new Vector3(x, WalkTop + height + 0.34f, middle + 1.3f),
                        new Vector3(6.9f, 0.68f, length * 0.76f), wall);
                    for (float floor = 2.2f; floor < height - 1f; floor += 3.05f)
                    {
                        for (float bay = z + 1.6f; bay < z + length - 0.92f; bay += 3.0f)
                        {
                            skyline.Box(new Vector3(nearWall - side * 0.025f, WalkTop + floor, bay),
                                new Vector3(0.063f, 1.34f, 1.65f), WeatherSamplePresets.Tone(0x4E5C60));
                        }
                    }
                    z += length + 1.6f + (float)random.NextDouble() * 3.1f;
                }
            }
            for (int index = 0; index < 43; index++)
            {
                float x = ((float)random.NextDouble() - 0.5f) * 235f;
                float z = 79f + (float)random.NextDouble() * 258f;
                if (Mathf.Abs(x) < 13f)
                    x += x < 0f ? -19f : 19f;
                float height = 10f + (float)random.NextDouble() * 41f;
                Color color = Color.Lerp(WeatherSamplePresets.Tone(0x545955), WeatherSamplePresets.Tone(0x8A8E85),
                    (float)random.NextDouble());
                skyline.Box(new Vector3(x, height * 0.5f, z), new Vector3(10f + index % 6, height, 13f), color);
            }
            RenderMesh("Distant city massing", skyline.Mesh(), distant);
        }

        private void Mark(WeatherSampleGeometry.Blocks marks, float x0, float x1, float z0, float z1)
        {
            Vector3 Point(float x, float z) => new Vector3(x, RoadHeight(x, z, seed) + 0.0031f, z);
            marks.Quad(Point(x0, z0), Point(x0, z1), Point(x1, z1), Point(x1, z0), Color.white);
        }

        private void BuildBlocks(WeatherStreetAssets assets, Color haze)
        {
            float left = -46f;
            float right = -49f;
            left = PlaceBuilding(assets.CornerCafe, -1, left, 0.03f);
            left = PlaceBuilding(assets.BrickShops, -1, left + 1.25f, 0.49f);
            left = PlaceBuilding(assets.CornerOffices, -1, left + 2.35f, 1.11f);
            right = PlaceBuilding(assets.BrickShops, 1, right, 0.72f);
            right = PlaceBuilding(assets.CornerOffices, 1, right + 0.88f, 0.09f);
            BuildBackground(haze, left, right);

            var drain = UnityEngine.Object.Instantiate(assets.Drain, root);
            drain.hideFlags = HideFlags.DontSave;
            drain.transform.SetLocalPositionAndRotation(new Vector3(-5.82f, RoadHeight(-5.82f, -26.5f, seed) - 0.004f, -26.5f),
                Quaternion.Euler(0f, 90f, 0f));
            properties.Clear();
            properties.SetColor("_BaseColor", WeatherSamplePresets.Tone(0x414241));
            properties.SetFloat("_Metallic", 0.64f);
            foreach (Renderer renderer in drain.GetComponentsInChildren<Renderer>())
                renderer.SetPropertyBlock(properties);
            if (!assets.StreetLamp)
                return;
            for (int order = 0; order < 8; order++)
            {
                int side = order % 2 == 0 ? -1 : 1;
                var lamp = UnityEngine.Object.Instantiate(assets.StreetLamp, root);
                lamp.hideFlags = HideFlags.DontSave;
                lamp.transform.SetLocalPositionAndRotation(new Vector3(side * 6.87f, WalkTop, -19.6f + 11.5f * order),
                    Quaternion.Euler(0f, side * 90f, 0f));
            }
        }

        private float PlaceBuilding(GameObject prefab, int side, float beginning, float setback)
        {
            var building = UnityEngine.Object.Instantiate(prefab, root);
            building.hideFlags = HideFlags.DontSave;
            Transform transform = building.transform;
            transform.SetLocalPositionAndRotation(new Vector3(0, WalkTop, 0), Quaternion.Euler(0f, side * 90f, 0f));
            Renderer[] renderers = building.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                throw new InvalidOperationException("The street building has no renderer: " + prefab.name);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
                Material[] slots = renderer.sharedMaterials;
                for (int index = 0; index < slots.Length; index++)
                {
                    if (!slots[index] || slots[index].name != "Glazing")
                        continue;
                    properties.Clear();
                    var glazing = WeatherSamplePresets.Tone(0x59625F);
                    glazing.a = 0.45f;
                    properties.SetColor("_BaseColor", glazing);
                    renderer.SetPropertyBlock(properties, index);
                }
            }
            foreach (Light light in building.GetComponentsInChildren<Light>())
            {
                light.intensity *= 0.62f;
                light.color = WeatherSamplePresets.Tone(0xF3DFC1);
            }
            float facade = side < 0 ? bounds.max.x : bounds.min.x;
            transform.position += new Vector3(side * (8.95f + setback) - facade, 0f, beginning - bounds.min.z);
            return beginning + bounds.size.z;
        }

        private Material MakeSurface(Texture2D color, Texture2D normal, Texture2D roughness,
            float tileMetres, Color haze, Color tint, float normalStrength)
        {
            Material material = WeatherField.NewMaterial("StreetPaving");
            material.SetTexture("_BaseMap", color);
            material.SetTexture("_NormalMap", normal);
            material.SetTexture("_RoughnessMap", roughness);
            material.SetFloat("_TileMetres", tileMetres);
            material.SetColor("_Horizon", haze);
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_NormalStrength", normalStrength);
            material.SetFloat("_NormalStoredAsColor",
                UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(normal.graphicsFormat) ? 1f : 0f);
            material.SetFloat("_RoughnessStoredAsColor",
                UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(roughness.graphicsFormat) ? 1f : 0f);
            materials.Add(material);
            return material;
        }

        private GameObject PlaceObject(string label)
        {
            var child = new GameObject(label) { hideFlags = HideFlags.DontSave };
            child.transform.SetParent(root, false);
            return child;
        }

        private MeshRenderer RenderMesh(string label, Mesh mesh, Material material)
        {
            meshes.Add(mesh);
            mesh.name = label;
            mesh.hideFlags = HideFlags.HideAndDontSave;
            GameObject child = PlaceObject(label);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return renderer;
        }

        public void Dispose()
        {
            reflection.Dispose();
            foreach (Mesh mesh in meshes)
                WeatherField.Dispose(mesh);
            foreach (Material material in materials)
                WeatherField.Dispose(material);
        }
    }
}
