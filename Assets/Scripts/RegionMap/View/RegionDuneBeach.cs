using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Vastcore.RegionMap
{
    /// <summary>外洋の砂浜、高潮の漂着帯、風成の前砂丘を連続した高さ場に置く。</summary>
    public sealed class RegionDuneBeach : MonoBehaviour
    {
        private const int Resolution = 321;
        private const float Width = 864f;
        private const float Depth = 304f;
        private const float StepX = Width / (Resolution - 1);
        private const float StepZ = Depth / (Resolution - 1);
        private const float Low = RegionPocketBeach.LowWater;
        private const float High = RegionPocketBeach.HighWater;

        private readonly List<Object> generated = new List<Object>();
        private ShoreGround ground;
        private Mesh terrain;
        private Transform sea;
        private Material seaMaterial;
        private Tide lowTide, highTide;

        public bool IsHighTide { get; private set; }
        public bool ConnectedAtBothTides => lowTide.Open && highTide.Open;
        public int GrassCount { get; private set; }
        public int WrackCount { get; private set; }
        public int DriftwoodCount { get; private set; }
        public int CobbleCount { get; private set; }
        public float LowestWrack { get; private set; } = float.PositiveInfinity;
        public float ClosestGrassToWrack { get; private set; } = float.PositiveInfinity;
        public float LowDrySand => lowTide.DryArea;
        public float HighDrySand => highTide.DryArea;
        public float LowWaterline => lowTide.Waterline;
        public float HighWaterline => highTide.Waterline;
        public Vector2 RegionalContact { get; private set; }
        public int Seed => ground.Seed;
        public float ShoreAt(float x) => ground.ShoreAt(x);

        private sealed class Tide
        {
            public Texture2D DepthMap;
            public List<Vector4> Surface;
            public bool Open;
            public float DryArea, Waterline;
        }

        public static RegionDuneBeach Build(RegionData region, RegionPropCatalog catalog, int seed)
        {
            RegionDetail detail = RegionPocketBeach.OpenCoastDetail(region, seed, 1.22f, out Vector2 contact, out float yaw);
            var view = new GameObject("Dune Beach").AddComponent<RegionDuneBeach>();
            view.RegionalContact = new Vector2(detail.OriginMapX, detail.OriginMapY)
                + contact / (detail.Data.CellMeters * detail.Data.Resolution) * detail.SizeMap;
            view.Assemble(new ShoreGround(detail.Data, contact, yaw, seed), region, catalog);
            return view;
        }

        private void Assemble(ShoreGround shape, RegionData region, RegionPropCatalog catalog)
        {
            ground = shape;
            terrain = Own(ground.CreateMesh(out Color32[] colors));
            var surface = Own(new Material(Resources.Load<Shader>(RegionTerrainView.ShaderResource)) { hideFlags = HideFlags.DontSave });
            surface.SetTexture("_BaseMap", Own(RegionPocketBeach.MakeTexture("Dry sand and dunes", colors, Resolution)));
            surface.SetFloat("_RockStart", 0.04f);
            surface.SetFloat("_RockFull", -0.04f);
            var sand = Own(RegionRockyCoast.CoastalSurface(surface, sand: true));
            sand.SetFloat("_CoastalShelfDetail", 0f);
            sand.SetFloat("_DuneSand", 1f);
            sand.SetVector("_CoastalGroundTone", new Vector4(0.59f, 0.50f, 0.415f, 0f));
            sand.SetVector("_RegionBiomeGroundBlend", new Vector4(0.44f, 0.1f, 0f, 0f));
            // Leave contact shadows from driftwood and grass on the sand.
            RegionTerrainView.AddPart(transform, "Beach and foredunes", terrain, sand).shadowCastingMode = ShadowCastingMode.Off;
            AddRegionalBackground(region);

            lowTide = MakeTide(Low);
            highTide = MakeTide(High);
            seaMaterial = Own(RegionPocketBeach.WaterMaterial(3f));
            seaMaterial.SetVector("_ShoreSpan", new Vector4(Width, Depth, 0f, 0f));
            var seaMesh = Own(RegionPocketBeach.WaterPlane(Vector2.zero, new Vector2(9800f, 9800f)));
            var water = RegionTerrainView.AddPart(transform, "Atlantic sea", seaMesh, seaMaterial);
            water.shadowCastingMode = ShadowCastingMode.Off;
            sea = water.transform;

            SetTide(false);
            MakeWrack();
            if (catalog != null) PlaceDriftwoodAndCobbles(catalog);
            PlantDunes();
        }

        public void SetTide(bool high)
        {
            IsHighTide = high;
            Tide tide = high ? highTide : lowTide;
            sea.localPosition = Vector3.up * (high ? High : Low);
            seaMaterial.SetTexture("_Shore", tide.DepthMap);
            terrain.SetUVs(2, tide.Surface);
        }

        public void SetWaveTime(float time) => seaMaterial.SetFloat("_FixedTime", time);

        private void AddRegionalBackground(RegionData region)
        {
            const int side = 51, margin = 11, last = side - 1 - margin;
            float metresPerMap = region.SizeKm * 1000f / RegionData.MapUnits;
            var anchor = RegionalContact * metresPerMap;
            var vertices = new Vector3[side * side];
            var uv = new Vector2[vertices.Length];
            var colors = new Color32[vertices.Length];
            var shade = new Color32[vertices.Length];
            var triangles = new List<int>((side - 1) * (side - 1) * 6);
            for (int iz = 0; iz < side; iz++)
            for (int ix = 0; ix < side; ix++)
            {
                int i = ix + iz * side;
                float x = Axis(ix, Width * 0.5f - 9f, 1880f, 1390f);
                float z = Axis(iz, Depth * 0.5f - 9f, 430f, 1130f);
                float edgeX = Mathf.Clamp(x, -Width * 0.5f, Width * 0.5f);
                float edgeZ = Mathf.Clamp(z, -Depth * 0.5f, Depth * 0.5f);
                Vector2 map = anchor + ground.Along * x + ground.Inward * z;
                float original = RegionPocketBeach.SampleHeight(region.Height, region.Resolution, map.x / region.CellMeters, map.y / region.CellMeters);
                float blend = RegionNoise.Smooth(0f, 205f, Mathf.Max(Mathf.Abs(x - edgeX), Mathf.Abs(z - edgeZ)));
                float height = Mathf.Lerp(ground.Sample(edgeX, edgeZ) - 0.27f, original, blend);
                vertices[i] = new Vector3(x, height, z);
                uv[i] = new Vector2((ix + 0.5f) / side, (iz + 0.5f) / side);
                float tone = 0.5f + 0.5f * RegionNoise.FbmTurned(x * 0.018f, z * 0.016f, 2, (uint)ground.Seed);
                colors[i] = Color.Lerp(new Color(0.50f, 0.50f, 0.35f), new Color(0.39f, 0.45f, 0.32f), blend * tone);
                shade[i] = new Color32(255, 255, 255, 255);
                if (ix == side - 1 || iz == side - 1 || ix >= margin && ix < last && iz >= margin && iz < last) continue;
                triangles.Add(i); triangles.Add(i + side); triangles.Add(i + 1);
                triangles.Add(i + 1); triangles.Add(i + side); triangles.Add(i + side + 1);
            }
            var mesh = Own(new Mesh { name = "Regional coast continuation", hideFlags = HideFlags.DontSave });
            mesh.vertices = vertices;
            mesh.colors32 = shade;
            mesh.uv = uv;
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            var material = Own(new Material(Resources.Load<Shader>(RegionTerrainView.ShaderResource)) { hideFlags = HideFlags.DontSave });
            material.SetTexture("_BaseMap", Own(RegionPocketBeach.MakeTexture("Regional coast colors", colors, side)));
            material.SetFloat("_RockStart", 0.04f);
            material.SetFloat("_RockFull", -0.04f);
            RegionTerrainView.AddPart(transform, "Regional ground beyond the sample", mesh, material).shadowCastingMode = ShadowCastingMode.Off;

            float Axis(int index, float inner, float negative, float positive)
            {
                if (index <= margin) return Mathf.Lerp(-negative, -inner, index / (float)margin);
                if (index >= last) return Mathf.Lerp(inner, positive, (index - last) / (float)margin);
                return Mathf.Lerp(-inner, inner, (index - margin) / (float)(last - margin));
            }
        }

        private Tide MakeTide(float level)
        {
            var tide = new Tide { Surface = new List<Vector4>(ground.Height.Length) };
            var connected = RegionOceanMask.ConnectedWater(ground.Height, Resolution, level, i => i / Resolution < Resolution / 2);
            var pixels = new Color32[connected.Length];
            int dry = 0;
            for (int iz = 0; iz < Resolution; iz++)
            for (int ix = 0; ix < Resolution; ix++)
            {
                int i = ix + iz * Resolution;
                float x = ix * StepX - Width * 0.5f, z = iz * StepZ - Depth * 0.5f;
                float distance = z - ground.ShoreAt(x);
                float height = ground.Height[i];
                float wet = RegionPocketBeach.WetnessAt(height, level);
                tide.Surface.Add(new Vector4(0f, wet, 1f, ground.Sand[i]));
                pixels[i] = RegionPocketBeach.DepthPixel(level - height, ground.ExposureAt(x, z), ground.Sand[i], connected[i]);
                if (Mathf.Abs(x) < 210f && distance < 26f && height > level) dry++;
            }
            tide.DepthMap = Own(RegionPocketBeach.MakeTexture("Dune shore depth " + level, pixels, Resolution));
            tide.Open = connected[ground.IndexAt(0f, ground.ShoreAt(0f) - 22f)];
            tide.DryArea = dry * StepX * StepZ;
            float shore = ground.ShoreAt(0f);
            float seaward = shore - 24f, inland = shore + 24f;
            for (int i = 0; i < 12; i++)
            {
                float z = (seaward + inland) * 0.5f;
                if (ground.Sample(0f, z) <= level) seaward = z;
                else inland = z;
            }
            tide.Waterline = (seaward + inland) * 0.5f;
            return tide;
        }

        private void MakeWrack()
        {
            var rng = new System.Random(ground.Seed * 2753 + 111);
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            var dried = new List<int>();
            for (int patch = 0; patch < 110; patch++)
            {
                float anchor = Mathf.Lerp(-261f, 242f, (patch + Next(rng)) / 110f);
                float weight = ground.WrackClumps(anchor);
                if (weight < 0.25f) continue;
                int count = Mathf.RoundToInt(Mathf.Lerp(8, 36, weight));
                for (int i = 0; i < count; i++)
                {
                    float x = anchor + (Next(rng) - Next(rng)) * Mathf.Lerp(1f, 5f, weight);
                    float z = ground.WrackAt(x) + (Next(rng) - Next(rng)) * Mathf.Lerp(0.48f, 1.4f, weight);
                    float height = ground.Sample(x, z);
                    if (height < High + 0.10f) continue;
                    LowestWrack = Mathf.Min(LowestWrack, height);
                    float length = Mathf.Lerp(0.065f, 0.70f, Next(rng) * Next(rng));
                    float angle = Mathf.Lerp(-0.79f, 0.88f, Next(rng));
                    var axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * length;
                    var across = new Vector2(-axis.y, axis.x) * Mathf.Lerp(0.05f, 0.23f, Next(rng));
                    var center = new Vector2(x, z);
                    Vector2 kink = center + axis * 0.21f + across * Mathf.Lerp(-1.55f, 1.55f, Next(rng));
                    int index = vertices.Count;
                    Add(center - axis * 0.45f);
                    Add(center + across * 0.87f);
                    Add(kink + across * 0.58f);
                    Add(center + axis * 0.51f);
                    Add(kink - across * 0.48f);
                    Add(center - across * 0.63f);
                    List<int> submesh = Next(rng) > 0.34f ? indices : dried;
                    submesh.Add(index); submesh.Add(index + 1); submesh.Add(index + 5);
                    submesh.Add(index + 1); submesh.Add(index + 2); submesh.Add(index + 5);
                    submesh.Add(index + 2); submesh.Add(index + 4); submesh.Add(index + 5);
                    submesh.Add(index + 2); submesh.Add(index + 3); submesh.Add(index + 4);
                    WrackCount++;
                }
            }
            var mesh = Own(new Mesh { name = "Broken wrack line", hideFlags = HideFlags.DontSave });
            mesh.SetVertices(vertices);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(indices, 0);
            mesh.SetTriangles(dried, 1);
            mesh.RecalculateNormals();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            var weed = Own(new Material(shader) { color = new Color(0.125f, 0.095f, 0.051f), hideFlags = HideFlags.DontSave });
            var straw = Own(new Material(shader) { color = new Color(0.28f, 0.19f, 0.095f), hideFlags = HideFlags.DontSave });
            weed.SetFloat("_Smoothness", 0.18f);
            straw.SetFloat("_Smoothness", 0.22f);
            var wrack = RegionTerrainView.AddPart(transform, "Seaweed and fragments above high water", mesh, weed);
            wrack.sharedMaterials = new[] { weed, straw };
            wrack.shadowCastingMode = ShadowCastingMode.Off;

            void Add(Vector2 p) => vertices.Add(new Vector3(p.x, ground.Sample(p.x, p.y) + 0.022f, p.y));
        }

        private void PlaceDriftwoodAndCobbles(RegionPropCatalog catalog)
        {
            var wood = new RegionPropProofSlice { Biome = RegionBiome.Coast, UseRealAssets = true };
            var cobbles = new RegionPropProofSlice { Biome = RegionBiome.Coast, UseRealAssets = true };
            var log = catalog.GetProp("nat_dry_root_log");
            var stones = catalog.GetProp("nat_pebble_cluster");
            var rng = new System.Random(ground.Seed * 1747 + 409);
            for (int i = 0; i < 8; i++)
            {
                float x = 90f - 40f * i + (Next(rng) - 0.5f) * 5f;
                Place(wood, log, "nat_dry_root_log__bleached_trunk", x, ground.WrackAt(x) + 0.62f,
                    Mathf.Lerp(0.53f, 0.99f, Next(rng)), 77f + Next(rng) * 27f);
                for (int j = 0; j < 3; j++)
                {
                    float near = x + 1.8f + 3.2f * j + (Next(rng) - 0.5f) * 2f;
                    Place(cobbles, stones, "nat_pebble_cluster__wash_sorted_cobbles", near,
                        ground.WrackAt(near) + Mathf.Lerp(-0.6f, 0.5f, Next(rng)),
                        Mathf.Lerp(0.66f, 1.00f, Next(rng)), Next(rng) * 180f);
                }
            }
            DriftwoodCount = RegionPropView.AddTo(transform, wood, (x, south) => ground.Sample(x, -south)).RealModelCount;
            var deposits = RegionPropView.AddTo(transform, cobbles, (x, south) => ground.Sample(x, -south));
            CobbleCount = deposits.RealModelCount;
            RegionPropView.SettleLoosePieces(deposits, (x, south) => ground.Sample(x, -south), 0.024f);
            RegionRockyCoast.MatchDepositColor(deposits.RealRoot, wet: true, tone: 1.14f);

            void Place(RegionPropProofSlice slice, RegionPropDefinition prop, string variant, float x, float z, float scale, float yaw)
            {
                float y = ground.Sample(x, z);
                float dx = ground.Sample(x + 0.5f, z) - ground.Sample(x - 0.5f, z);
                float dz = ground.Sample(x, z - 0.5f) - ground.Sample(x, z + 0.5f);
                float slope = Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;
                if (y < High + 0.16f || slope > 20f) return;
                var item = RegionPropPlacementSolver.ResolveSingle(prop, (uint)ground.Seed, slice.Instances.Count,
                    x, y, -z, slope, dx, dz, scaleOverride: scale, yawOverride: yaw, biome: RegionBiome.Coast);
                item.X = x;
                item.Z = -z;
                item.Y = y - Mathf.Max(0.018f, item.DimensionsY * item.ScaleY * 0.23f);
                item.MeshVariant = variant;
                item.DamageState = item.ConstructionVariant = "";
                item.TopDust = 0f;
                if (slice == wood)
                {
                    item.ScaleY *= 0.74f;
                    item.TintShift += 0.11f;
                }
                slice.Instances.Add(item);
                LowestWrack = Mathf.Min(LowestWrack, y);
            }
        }

        private void PlantDunes()
        {
            var plants = new List<RegionPlant>();
            var pioneer = new List<RegionPlant>();
            var rng = new System.Random(ground.Seed * 1847 + 211);
            const float spacing = 1.16f;
            for (float x = -303f; x < 285f; x += spacing)
            for (float distance = 23f; distance < 112f; distance += spacing)
            {
                float px = x + spacing * Next(rng);
                float z = ground.ShoreAt(px) + distance + spacing * Next(rng);
                float strength = ground.GrassAt(px, z);
                if (Next(rng) > strength) continue;
                float y = ground.Sample(px, z);
                float margin = z - ground.WrackAt(px);
                if (y < High + 0.54f || margin < 3.0f) continue;
                ClosestGrassToWrack = Mathf.Min(ClosestGrassToWrack, margin);
                bool low = strength > 0.39f && Next(rng) < 0.57f;
                plants.Add(new RegionPlant
                {
                    Kind = RegionPlantKind.Grass, Variant = (low ? 0 : 2) + rng.Next(2),
                    X = px, Y = y, Z = -z,
                    Height = low ? Mathf.Lerp(0.25f, 0.50f, Next(rng))
                        : Mathf.Lerp(0.40f, 1.05f, Mathf.Clamp01(0.56f * strength + 0.57f * Next(rng))),
                    Yaw = 1.33f + (Next(rng) - 0.5f) * 1.0f, Sun = 0.95f, Sky = 0.91f,
                });
            }
            // Colonies at the dune toe spread around small sand catches landward of the wrack.
            for (int colony = 0; colony < 24; colony++)
            {
                float x = 148f - 13.8f * colony + Mathf.Lerp(-3.1f, 3.1f, Next(rng));
                float margin = Mathf.Lerp(4.3f, 9.4f, Next(rng));
                float width = Mathf.Lerp(1.8f, 6.9f, Next(rng));
                int count = 14 + rng.Next(27);
                for (int i = 0; i < count; i++)
                {
                    float a = Next(rng) * 2f * Mathf.PI;
                    float radius = Mathf.Sqrt(Next(rng));
                    float px = x + width * Mathf.Cos(a) * radius;
                    float d = margin + 1.65f * Mathf.Sin(a) * radius;
                    float z = ground.WrackAt(px) + d;
                    float y = ground.Sample(px, z);
                    if (y < High + 0.44f || d < 3.0f) continue;
                    ClosestGrassToWrack = Mathf.Min(ClosestGrassToWrack, d);
                    pioneer.Add(new RegionPlant
                    {
                        Kind = RegionPlantKind.Grass, Variant = 2 + rng.Next(2),
                        X = px, Y = y, Z = -z,
                        Height = Mathf.Lerp(0.37f, 0.98f, Next(rng)), Sun = 0.95f, Sky = 0.91f,
                    });
                }
            }
            GrassCount = plants.Count + pioneer.Count;
            RegionPlantView.AddMeadowTufts(transform, plants, ground.Sample, "Dune grass colonies", new Color(0.91f, 0.82f, 0.56f));

            // Use rooted blades on the front slope where crossed silhouettes read as spots.
            var front = plants.FindAll(plant => plant.X > -147f && plant.X < 153f &&
                plant.Variant >= 2 && -plant.Z - ground.WrackAt(plant.X) < 29f);
            front.AddRange(pioneer);
            AddFrontBlades(front);
        }

        private void AddFrontBlades(List<RegionPlant> plants)
        {
            var rng = new System.Random(ground.Seed * 4117 + 227);
            var vertices = new List<Vector3>(plants.Count * 52);
            var light = new List<Color32>(vertices.Capacity);
            var alive = new List<int>(plants.Count * 66);
            var dry = new List<int>(plants.Count * 27);
            for (int clump = 0; clump < plants.Count; clump++)
            {
                RegionPlant plant = plants[clump];
                int leaves = 11 + rng.Next(6);
                for (int leaf = 0; leaf < leaves; leaf++)
                {
                    float azimuth = Next(rng) * Mathf.PI * 2f;
                    var radial = new Vector3(Mathf.Cos(azimuth), 0f, Mathf.Sin(azimuth));
                    Vector3 baseXZ = new Vector3(plant.X, 0f, -plant.Z) + radial * (0.23f * Next(rng));
                    var basePoint = new Vector3(baseXZ.x, ground.Sample(baseXZ.x, baseXZ.z) - 0.014f, baseXZ.z);
                    float height = plant.Height * Mathf.Lerp(0.48f, 1.20f, Next(rng));
                    Vector3 lean = new Vector3(-0.34f, 0f, 0.83f) * (height * Mathf.Lerp(0.23f, 0.74f, Next(rng)))
                        + radial * (height * 0.32f);
                    Vector3 width = new Vector3(-radial.z, 0f, radial.x) * Mathf.Lerp(0.012f, 0.024f, Next(rng));
                    Vector3 bend = basePoint + Vector3.up * (0.67f * height) + lean * 0.34f;
                    Vector3 tip = basePoint + Vector3.up * (0.87f * height) + lean;
                    int start = vertices.Count;
                    vertices.Add(basePoint - width); vertices.Add(basePoint + width);
                    vertices.Add(bend - width * 0.78f); vertices.Add(bend + width * 0.78f);
                    vertices.Add(tip);
                    light.Add(new Color32(150, 178, 255, 255)); light.Add(new Color32(150, 178, 255, 255));
                    light.Add(new Color32(211, 223, 255, 255)); light.Add(new Color32(211, 223, 255, 255));
                    light.Add(new Color32(255, 255, 255, 255));
                    List<int> triangles = Next(rng) < 0.29f ? dry : alive;
                    triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
                    triangles.Add(start + 1); triangles.Add(start + 2); triangles.Add(start + 3);
                    triangles.Add(start + 2); triangles.Add(start + 4); triangles.Add(start + 3);
                }
            }
            var mesh = Own(new Mesh { name = "Wind-bent dune blades", indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.DontSave });
            mesh.SetVertices(vertices);
            mesh.SetColors(light);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(alive, 0);
            mesh.SetTriangles(dry, 1);
            mesh.RecalculateBounds();
            Shader shader = Resources.Load<Shader>("RegionMap3D/RegionPlants");
            var green = Own(new Material(shader) { hideFlags = HideFlags.DontSave });
            var straw = Own(new Material(shader) { hideFlags = HideFlags.DontSave });
            green.SetColor("_BaseColor", new Color(0.33f, 0.32f, 0.175f));
            straw.SetColor("_BaseColor", new Color(0.46f, 0.375f, 0.19f));
            var renderer = RegionTerrainView.AddPart(transform, "Wind-bent grasses on the front slope", mesh, green);
            renderer.sharedMaterials = new[] { green, straw };
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static float Next(System.Random rng) => (float)rng.NextDouble();

        private T Own<T>(T asset) where T : Object
        {
            generated.Add(asset);
            return asset;
        }

        private void OnDestroy()
        {
            foreach (Object asset in generated)
                if (Application.isPlaying) Destroy(asset);
                else DestroyImmediate(asset);
        }

        private sealed class ShoreGround
        {
            internal int Seed { get; }
            internal Vector2 Inward { get; }
            internal Vector2 Along { get; }
            internal readonly float[] Height = new float[Resolution * Resolution];
            internal readonly float[] Sand = new float[Resolution * Resolution];
            private readonly uint noise;
            private readonly float phase;

            internal ShoreGround(RegionData region, Vector2 contact, float seaYaw, int seed)
            {
                Seed = seed;
                noise = (uint)(seed * 587);
                phase = seed == 8 ? 0.25f : -0.49f;
                float yaw = seaYaw * Mathf.Deg2Rad;
                Inward = new Vector2(-Mathf.Sin(yaw), -Mathf.Cos(yaw));
                Along = new Vector2(-Inward.y, Inward.x);
                for (int z = 0; z < Resolution; z++)
                for (int x = 0; x < Resolution; x++)
                {
                    int i = x + z * Resolution;
                    float px = x * StepX - Width * 0.5f, pz = z * StepZ - Depth * 0.5f;
                    Vector2 world = contact + px * Along + pz * Inward;
                    float original = RegionPocketBeach.SampleHeight(region.Height, region.Resolution, world.x / region.CellMeters, world.y / region.CellMeters);
                    float distance = pz - ShoreAt(px);
                    float local = (1f - RegionNoise.Smooth(315f, 427f, Mathf.Abs(px)))
                        * (1f - RegionNoise.Smooth(110f, 151f, Mathf.Abs(pz)));
                    float swell = 0.65f * RegionNoise.FbmTurned(px * 0.039f, pz * 0.065f, 2, noise + 61u);
                    float ripple = 0.12f * RegionNoise.Fbm(px * 0.12f, distance * 0.32f, 2, noise + 371u);
                    float seabedSlope = Mathf.Lerp(0.09f, 0.132f, RegionNoise.Smooth(-27f, -8f, distance));
                    float height = distance < 0f ? Mathf.Max(-17f, distance * seabedSlope) :
                        Mathf.Min(distance, 22f) * 0.088f + Mathf.Max(0f, distance - 22f) * 0.022f;
                    height += swell * RegionNoise.Smooth(13f, 53f, distance) + ripple;
                    float crest = 42f + 6.3f * Mathf.Sin(px / 38f + phase) + 4.2f * Mathf.Sin(px / 17f + 1.1f);
                    float rise = 6.3f + 2.6f * Mathf.Sin(px / 46f + phase) + 1.12f * Mathf.Sin(px / 18.5f);
                    float blowout = 0.83f + 0.27f * RegionNoise.Smooth(-0.26f, 0.13f,
                        RegionNoise.FbmTurned(px * 0.078f, pz * 0.049f, 2, noise + 23u));
                    height += Mathf.Max(1.45f, rise) * Bell(distance - crest, 15f, 11.8f)
                        * RegionNoise.Smooth(19f, 29f, distance) * blowout;
                    height += (2.65f + 1.35f * Mathf.Sin(px / 59f - 0.54f)) * Bell(distance - crest - 25f, 12f, 16f);
                    height += 0.25f * Bell(distance - (WrackAt(px) - ShoreAt(px)), 4.1f, 3.6f);
                    Height[i] = Mathf.Lerp(original, height, local);
                    Sand[i] = local * (1f - RegionNoise.Smooth(107f, 140f, distance));
                }
            }

            private static float Bell(float distance, float rising, float falling)
            {
                float along = distance / (distance < 0f ? rising : falling);
                return Mathf.Exp(-along * along);
            }

            internal float ShoreAt(float x) =>
                -5f + 12f * Mathf.Sin((x + 25f) / 126f + phase) + 3.7f * Mathf.Sin(x / 63f - 0.83f)
                + 9.5f * RegionNoise.Smooth(125f, 290f, -x);

            internal float WrackAt(float x) =>
                ShoreAt(x) + 19.1f + 1.48f * Mathf.Sin(x / 15f + phase) + 0.92f * Mathf.Sin(x / 6.2f);

            internal float WrackClumps(float x) =>
                RegionNoise.Smooth(-0.24f, 0.28f, RegionNoise.Fbm(x * 0.092f, 5.18f, 3, noise + 12u));

            internal float GrassAt(float x, float z)
            {
                float d = z - WrackAt(x);
                float broad = RegionNoise.FbmTurned(x * 0.040f, z * 0.096f, 3, noise + 712u);
                float root = RegionNoise.FbmTurned(x * 0.22f, z * 0.29f, 2, noise + 115u);
                float colony = RegionNoise.Smooth(-0.20f, 0.22f, broad + 0.29f * root);
                float toe = Mathf.Lerp(7.5f, 15.5f, 0.5f + 0.5f * Mathf.Sin(x / 15f + phase));
                return RegionNoise.Smooth(toe, toe + 11f, d) * colony * (1f - RegionNoise.Smooth(69f, 91f, d));
            }

            internal float ExposureAt(float x, float z) =>
                Mathf.Lerp(0.45f, 0.80f, 0.5f + 0.5f * Mathf.Sin(x / 54f - z / 31f));

            internal float Sample(float x, float z) =>
                RegionPocketBeach.SampleHeight(Height, Resolution, (x + Width * 0.5f) / StepX, (z + Depth * 0.5f) / StepZ);

            internal int IndexAt(float x, float z) => Mathf.Clamp(Mathf.RoundToInt((x + Width * 0.5f) / StepX), 0, Resolution - 1)
                + Mathf.Clamp(Mathf.RoundToInt((z + Depth * 0.5f) / StepZ), 0, Resolution - 1) * Resolution;

            internal Mesh CreateMesh(out Color32[] colors)
            {
                colors = new Color32[Height.Length];
                var vertices = new Vector3[Height.Length];
                var uv = new Vector2[Height.Length];
                var shade = new Color32[Height.Length];
                var indices = new int[(Resolution - 1) * (Resolution - 1) * 6];
                int t = 0;
                for (int z = 0; z < Resolution; z++)
                for (int x = 0; x < Resolution; x++)
                {
                    int i = x + z * Resolution;
                    float px = x * StepX - Width * 0.5f, pz = z * StepZ - Depth * 0.5f;
                    vertices[i] = new Vector3(px, Height[i], pz);
                    uv[i] = new Vector2((x + 0.5f) / Resolution, (z + 0.5f) / Resolution);
                    float patch = 0.5f + 0.5f * RegionNoise.FbmTurned(px * 0.11f, pz * 0.115f, 3, noise + 83u);
                    float grass = GrassAt(px, pz);
                    Color beach = Color.Lerp(new Color(0.53f, 0.45f, 0.34f), new Color(0.73f, 0.65f, 0.52f), patch);
                    Color dune = Color.Lerp(beach, new Color(0.36f, 0.35f, 0.24f), grass * 0.82f);
                    colors[i] = Color.Lerp(new Color(0.50f, 0.50f, 0.35f), dune, Sand[i]);
                    shade[i] = new Color32((byte)Mathf.RoundToInt(255f - 46f * grass), 255, 255, 255);
                    if (x == Resolution - 1 || z == Resolution - 1) continue;
                    indices[t++] = i; indices[t++] = i + Resolution; indices[t++] = i + 1;
                    indices[t++] = i + 1; indices[t++] = i + Resolution; indices[t++] = i + Resolution + 1;
                }
                var mesh = new Mesh { name = "Beach and foredune ground", indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.DontSave };
                mesh.vertices = vertices;
                mesh.uv = uv;
                mesh.colors32 = shade;
                mesh.triangles = indices;
                mesh.RecalculateNormals();
                return mesh;
            }
        }
    }
}
