using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Vastcore.RegionMap
{
    /// <summary>地域の外洋岸に、岩礁・砂浜・潮だまりがある小さな湾を組み立てる。</summary>
    public sealed class RegionPocketBeach : MonoBehaviour
    {
        public const float LowWater = -0.55f;
        public const float HighWater = 1.02f;
        public const float RetainedWater = 0.27f;
        private const int Resolution = 321;
        private const float Span = 384f;
        private const float HalfSpan = Span * 0.5f;
        private const float Step = Span / (Resolution - 1);
        private static readonly Vector2 Basin = new Vector2(61f, -16f);

        private Object[] generated;
        private Mesh terrain;
        private Transform ocean;
        private GameObject pool;
        private Material seaMaterial;
        private Material poolMaterial;
        private Texture2D lowDepth, highDepth;
        private List<Vector4> lowSurface, highSurface;
        private CoveTerrain groundShape;

        public bool IsHighTide { get; private set; }
        public bool BasinConnectedAtLow { get; private set; }
        public bool BasinConnectedAtHigh { get; private set; }
        public float LowDrySand { get; private set; }
        public float HighDrySand { get; private set; }
        public float ReefHeight { get; private set; }
        public bool ReefChannelAtLow { get; private set; }
        public int ShelfModels { get; private set; }
        public int CobbleModels { get; private set; }
        public Vector2 RegionalContact { get; private set; }
        public int Seed => groundShape.Seed;
        public Vector2 TidePool => Basin;
        public float GroundHeight(float x, float z) => groundShape.HeightAt(x, z);

        public static RegionPocketBeach Build(RegionData region, RegionPropCatalog catalog, int seed)
        {
            RegionDetail detail = OpenCoastDetail(region, seed, 0.92f, out Vector2 contact, out float seaYaw);
            var patch = new CoveTerrain(detail.Data, contact, seaYaw, seed);
            return new GameObject("Pocket Beach").AddComponent<RegionPocketBeach>().Assemble(patch, detail, contact, catalog);
        }

        internal static RegionDetail OpenCoastDetail(RegionData region, int seed, float sizeKm, out Vector2 contact, out float seaYaw)
        {
            // Shore survey: low, open Atlantic-facing candidates for each region seed.
            var site = seed switch
            {
                8 => new Vector3(82.177734f, 79.78516f, 18.43f),
                9 => new Vector3(38.23242f, 81.73828f, 0f),
                _ => throw new ArgumentOutOfRangeException(nameof(seed)),
            };
            var detail = RegionDetailBuilder.Build(region, new RegionDetailSpec
            {
                CenterMapX = site.x, CenterMapY = site.y, SizeKm = sizeKm, Resolution = 257,
            });
            contact = FindOpenShore(region, detail);
            seaYaw = site.z;
            return detail;
        }

        private RegionPocketBeach Assemble(CoveTerrain patch, RegionDetail detail, Vector2 contact, RegionPropCatalog catalog)
        {
            groundShape = patch;
            var resources = new List<Object>();
            terrain = patch.CreateMesh(out Color32[] baseColors);
            resources.Add(terrain);
            var groundMap = MakeTexture("Cove ground colors", baseColors);
            resources.Add(groundMap);
            Shader groundShader = Resources.Load<Shader>(RegionTerrainView.ShaderResource);
            var baseMaterial = new Material(groundShader) { hideFlags = HideFlags.DontSave };
            baseMaterial.SetTexture("_BaseMap", groundMap);
            baseMaterial.SetColor("_RockColor", new Color(0.39f, 0.37f, 0.33f));
            baseMaterial.SetFloat("_BandAmount", 0.12f);
            var groundMaterial = RegionRockyCoast.CoastalSurface(baseMaterial, sand: true);
            resources.Add(baseMaterial);
            resources.Add(groundMaterial);
            var shoreRenderer = RegionTerrainView.AddPart(transform, "Rock shelves and sand pocket", terrain, groundMaterial);
            // On this height field the terrain's own shadow produces a thin vertical streak across the cove.
            // Keep the rocks' contact shadows on the terrain.
            shoreRenderer.shadowCastingMode = ShadowCastingMode.Off;

            bool[] low = patch.SeaAt(LowWater);
            bool[] high = patch.SeaAt(HighWater);
            int basin = patch.IndexAt(Basin.x, Basin.y);
            BasinConnectedAtLow = low[basin];
            BasinConnectedAtHigh = high[basin];
            LowDrySand = patch.DrySandArea(LowWater);
            HighDrySand = patch.DrySandArea(HighWater);
            ReefHeight = patch.HeightAt(patch.Reef.x, patch.Reef.y);
            ReefChannelAtLow = low[patch.IndexAt(patch.Reef.x, (patch.Reef.y + patch.Shore(patch.Reef.x)) * 0.5f)];
            RegionalContact = new Vector2(detail.OriginMapX, detail.OriginMapY) + contact / (detail.Data.CellMeters * detail.Data.Resolution) * detail.SizeMap;

            lowSurface = patch.SurfaceAt(LowWater);
            highSurface = patch.SurfaceAt(HighWater);
            lowDepth = patch.DepthAt(LowWater, low, "Low-tide bathymetry");
            highDepth = patch.DepthAt(HighWater, high, "High-tide bathymetry");
            var retainedDepth = patch.DepthAt(RetainedWater, patch.PoolAt(RetainedWater, basin), "Retained tide pool depth");
            resources.Add(lowDepth);
            resources.Add(highDepth);
            resources.Add(retainedDepth);

            seaMaterial = WaterMaterial(1f);
            poolMaterial = WaterMaterial(2f);
            poolMaterial.SetTexture("_Shore", retainedDepth);
            resources.Add(seaMaterial);
            resources.Add(poolMaterial);
            Mesh seaMesh = WaterPlane(Vector2.zero, new Vector2(9800f, 9800f));
            Mesh poolMesh = WaterPlane(Basin, new Vector2(15f, 11f));
            resources.Add(seaMesh);
            resources.Add(poolMesh);
            var seaRenderer = RegionTerrainView.AddPart(transform, "Sea", seaMesh, seaMaterial);
            var poolRenderer = RegionTerrainView.AddPart(transform, "Retained tide pool", poolMesh, poolMaterial);
            seaRenderer.shadowCastingMode = poolRenderer.shadowCastingMode = ShadowCastingMode.Off;
            ocean = seaRenderer.transform;
            pool = poolRenderer.gameObject;
            pool.transform.localPosition = Vector3.up * RetainedWater;
            generated = resources.ToArray();

            if (catalog != null) AddShoreProps(patch, catalog);
            SetTide(false);
            return this;
        }

        public void SetTide(bool high)
        {
            IsHighTide = high;
            ocean.localPosition = Vector3.up * (high ? HighWater : LowWater);
            seaMaterial.SetTexture("_Shore", high ? highDepth : lowDepth);
            terrain.SetUVs(2, high ? highSurface : lowSurface);
            pool.SetActive(!(high ? BasinConnectedAtHigh : BasinConnectedAtLow));
        }

        public void SetWaveTime(float time)
        {
            seaMaterial.SetFloat("_FixedTime", time);
            poolMaterial.SetFloat("_FixedTime", time);
        }

        private void OnDestroy()
        {
            if (generated == null) return;
            foreach (Object asset in generated)
                if (Application.isPlaying) Destroy(asset);
                else DestroyImmediate(asset);
        }

        private void AddShoreProps(CoveTerrain patch, RegionPropCatalog catalog)
        {
            var shelves = new RegionPropProofSlice { Biome = RegionBiome.Coast, UseRealAssets = true };
            var stones = new RegionPropProofSlice { Biome = RegionBiome.Coast, UseRealAssets = true };
            var slab = catalog.GetProp("nat_stratified_slab");
            var gravel = catalog.GetProp("nat_pebble_cluster");
            var rng = new System.Random(unchecked(patch.Seed * 1447 + 103));
            for (int i = 0; i < 36; i++)
            {
                float x = (i % 2 == 0 ? 1f : -1f) * (49f + 44f * (float)rng.NextDouble());
                float z = patch.Shore(x) + Mathf.Lerp(1f, 12f, (float)rng.NextDouble());
                if (Vector2.Distance(new Vector2(x, z), Basin) < 14f) continue;
                Place(shelves, slab, i % 3 == 0 ? "stratified_ledge__fractured_edge" : "stratified_ledge__horizontal_step",
                    x, z, Mathf.Lerp(0.65f, 1.45f, (float)rng.NextDouble()), 23f + (float)rng.NextDouble() * 17f, 0.23f);
            }
            for (int i = -3; i <= 3; i++)
                Place(shelves, slab, i % 2 == 0 ? "weathered_slab__tapered_shale" : "stratified_ledge__fractured_edge",
                    patch.Reef.x + 4.2f * i, patch.Reef.y + 1.06f * i + 1.4f * Mathf.Sin(i * 1.33f),
                    1.38f + 0.59f * Mathf.Cos(i * 0.7f), -17f + 5f * i, 0.37f, 0.52f);

            // Continue the exposed beds on both sides of the basin while leaving the spillway clear.
            RimShelf(5.3f, -5.2f, 1.64f, 19f, "stratified_ledge__fractured_edge");
            RimShelf(2.3f, -7.3f, 1.64f, 25f, "stratified_ledge__horizontal_step");
            RimShelf(-4.9f, -7.2f, 1.72f, 17f, "stratified_ledge__fractured_edge");
            RimShelf(-8.5f, -5.8f, 1.35f, 30f, "weathered_slab__tapered_shale");
            RimShelf(-10.7f, -1.2f, 1.73f, 24f, "stratified_ledge__fractured_edge");
            RimShelf(-8.4f, 5.1f, 1.39f, 18f, "stratified_ledge__horizontal_step");
            RimShelf(-3.8f, 7.1f, 1.79f, 29f, "stratified_ledge__fractured_edge");
            RimShelf(2.9f, 7.4f, 1.36f, 15f, "weathered_slab__tapered_shale");
            RimShelf(-7.8f, -11.2f, 1.76f, 18f, "stratified_ledge__fractured_edge");
            RimShelf(-3.9f, -11.5f, 1.56f, 15f, "stratified_ledge__horizontal_step");
            RimShelf(5.7f, -9.2f, 1.77f, 24f, "stratified_ledge__fractured_edge");
            RimShelf(7.9f, 6.6f, 1.66f, 29f, "stratified_ledge__horizontal_step");
            for (int i = 0; i < 16; i++)
            {
                float x = (i % 2 == 0 ? 1f : -1f) * (40f + (float)rng.NextDouble() * 16f);
                float z = patch.Shore(x) + Mathf.Lerp(4f, 12f, (float)rng.NextDouble());
                Place(stones, gravel, "nat_pebble_cluster__wash_sorted_cobbles", x, z,
                    Mathf.Lerp(0.85f, 1.95f, (float)rng.NextDouble()), (float)rng.NextDouble() * 180f, 0.035f);
            }

            var ledges = RegionPropView.AddTo(transform, shelves, Sample);
            var cobbles = RegionPropView.AddTo(transform, stones, Sample);
            ShelfModels = ledges.RealModelCount;
            CobbleModels = cobbles.RealModelCount;
            RegionRockyCoast.MatchDepositColor(ledges.RealRoot, tone: 0.73f);
            RegionPropView.SettleLoosePieces(cobbles, Sample, 0.024f);
            RegionRockyCoast.MatchDepositColor(cobbles.RealRoot, wet: true);

            void RimShelf(float across, float inland, float scale, float yaw, string variant) =>
                Place(shelves, slab, variant, Basin.x + across, Basin.y + inland, scale, yaw, 0.31f, 0.53f);

            // Props store the south-positive Z coordinate, whereas this mesh is north-positive.
            float Sample(float x, float south) => patch.HeightAt(x, -south);
            void Place(RegionPropProofSlice slice, RegionPropDefinition def, string variant, float x, float z, float scale,
                float yaw, float embed, float verticalScale = 0.72f)
            {
                float y = patch.HeightAt(x, z);
                float dx = (patch.HeightAt(x + 0.6f, z) - patch.HeightAt(x - 0.6f, z)) / 1.2f;
                float dz = (patch.HeightAt(x, z - 0.6f) - patch.HeightAt(x, z + 0.6f)) / 1.2f;
                float slope = Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;
                if (y < 0.1f || slope > 27f || slice.Instances.Exists(other =>
                    (other.X - x) * (other.X - x) + (other.Z + z) * (other.Z + z) < 1.9f)) return;
                var item = RegionPropPlacementSolver.ResolveSingle(def, unchecked((uint)patch.Seed), slice.Instances.Count,
                    x, y, -z, slope, dx, dz, scaleOverride: scale, yawOverride: yaw, biome: RegionBiome.Coast);
                Vector3 rotation = (Quaternion.FromToRotation(Vector3.up, new Vector3(-dx, 1f, dz).normalized)
                    * Quaternion.Euler(0f, yaw, 0f)).eulerAngles;
                item.X = x; item.Z = -z;
                item.Y = y - embed;
                item.PitchDeg = rotation.x; item.YawDeg = rotation.y; item.RollDeg = -rotation.z;
                item.ScaleY *= slice == shelves ? verticalScale : 0.85f;
                item.MeshVariant = variant;
                item.DamageState = item.ConstructionVariant = "";
                item.TopDust = 0f;
                slice.Instances.Add(item);
            }
        }

        internal static Texture2D MakeTexture(string name, Color32[] colors, int resolution = Resolution)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true)
            {
                name = name, hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(colors);
            texture.Apply(false, true);
            return texture;
        }

        internal static Material WaterMaterial(float mode)
        {
            var material = new Material(Resources.Load<Shader>("RegionMap3D/RegionCoastWater")) { hideFlags = HideFlags.DontSave };
            material.SetFloat("_Extent", Span);
            material.SetFloat("_CoveMode", mode);
            return material;
        }

        internal static Mesh WaterPlane(Vector2 center, Vector2 extent)
        {
            var mesh = new Mesh { name = "Coastal water plane", hideFlags = HideFlags.DontSave };
            mesh.vertices = new[]
            {
                new Vector3(center.x - extent.x, 0f, center.y - extent.y), new Vector3(center.x + extent.x, 0f, center.y - extent.y),
                new Vector3(center.x - extent.x, 0f, center.y + extent.y), new Vector3(center.x + extent.x, 0f, center.y + extent.y),
            };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        internal static float WetnessAt(float height, float waterLevel, float rockWeight = 0f) =>
            1f - RegionNoise.Smooth(0.03f, 0.83f + 0.30f * rockWeight, height - waterLevel);

        internal static Color32 DepthPixel(float depth, float exposure, float sand, bool connected) => new Color32(
            (byte)Mathf.Clamp(Mathf.RoundToInt(depth * 20f), 0, 255),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(exposure) * 255f),
            (byte)Mathf.RoundToInt(Mathf.Clamp01(sand) * 255f),
            connected ? (byte)255 : (byte)0);

        internal static float SampleHeight(float[] heights, int n, float x, float z)
        {
            x = Mathf.Clamp(x, 0f, n - 1f);
            z = Mathf.Clamp(z, 0f, n - 1f);
            int ix = Mathf.Min(n - 2, (int)x), iz = Mathf.Min(n - 2, (int)z);
            int i = ix + iz * n;
            float dx = x - ix, dz = z - iz;
            float a = heights[i], b = heights[i + 1], c = heights[i + n], d = heights[i + n + 1];
            return dx + dz <= 1f ? a + (b - a) * dx + (c - a) * dz : d + (c - d) * (1f - dx) + (b - d) * (1f - dz);
        }

        private static Vector2 FindOpenShore(RegionData region, RegionDetail detail)
        {
            RegionData data = detail.Data;
            int n = data.Resolution;
            float cell = data.CellMeters, mapPerCell = detail.SizeMap / n;
            bool[] ocean = RegionOceanMask.ConnectedSea(region);
            bool[] connected = RegionOceanMask.ConnectedSea(data, i => ocean[region.CellFromMap(
                detail.OriginMapX + (i % n + 0.5f) * mapPerCell,
                detail.OriginMapY + (i / n + 0.5f) * mapPerCell)]);
            Vector2 center = Vector2.one * (n / 2f);
            Vector2 best = default;
            float distance = float.PositiveInfinity;
            int middle = n / 2;
            for (int z = middle - 23; z <= middle + 23; z++)
            for (int x = middle - 23; x <= middle + 23; x++)
            {
                int a = x + z * n;
                Consider(a, a + 1, new Vector2(x, z), Vector2.right);
                Consider(a, a + n, new Vector2(x, z), Vector2.up);
            }
            if (float.IsPositiveInfinity(distance)) throw new InvalidOperationException("The selected coast window has no open-ocean shore.");
            return best * cell;

            void Consider(int a, int b, Vector2 point, Vector2 edge)
            {
                if (!(connected[a] && data.IsLand(b)) && !(connected[b] && data.IsLand(a))) return;
                float t = data.Height[a] / (data.Height[a] - data.Height[b]);
                Vector2 shore = point + edge * t;
                float d = (shore - center).sqrMagnitude;
                if (d >= distance) return;
                distance = d;
                best = shore;
            }
        }

        private sealed class CoveTerrain
        {
            private readonly float[] ground = new float[Resolution * Resolution];
            private readonly float[] sand = new float[Resolution * Resolution];
            private readonly float[] stone = new float[Resolution * Resolution];
            private readonly float[] exposure = new float[Resolution * Resolution];
            private readonly float[] materialWeight = new float[Resolution * Resolution];
            private readonly float sweep;
            private readonly float beachWidth;
            internal readonly int Seed;
            internal readonly Vector2 Reef;

            internal CoveTerrain(RegionData region, Vector2 contact, float seaYaw, int seed)
            {
                Seed = seed;
                sweep = seed == 8 ? -6f : 3f;
                beachWidth = seed == 8 ? 48f : 52f;
                Reef = new Vector2(seed == 8 ? -12f : -23f, seed == 8 ? -61f : -67f);
                float yaw = seaYaw * Mathf.Deg2Rad;
                Vector2 inland = new Vector2(-Mathf.Sin(yaw), -Mathf.Cos(yaw));
                Vector2 right = new Vector2(-inland.y, inland.x);
                uint noise = unchecked((uint)seed * 13u);
                for (int z = 0; z < Resolution; z++)
                for (int x = 0; x < Resolution; x++)
                {
                    int i = x + z * Resolution;
                    float px = x * Step - HalfSpan, pz = z * Step - HalfSpan;
                    Vector2 original = contact + right * px + inland * pz;
                    float y = SampleHeight(region.Height, region.Resolution, original.x / region.CellMeters, original.y / region.CellMeters);
                    float shore = Shore(px);
                    float distance = pz - shore;
                    float seam = RegionNoise.Fbm(px / 8.5f, pz / 9f, 2, noise + 3u);
                    float wide = RegionNoise.Fbm(px / 36f, pz / 32f, 3, noise);
                    float lateral = px - sweep;
                    float bank = RegionNoise.Smooth(beachWidth * 0.79f, beachWidth + 17f, Mathf.Abs(lateral) + 5f * seam);
                    float bare = 1f - RegionNoise.Smooth(28f, 53f, distance);
                    float lift = (lateral < 0f ? 12.6f : 7.6f) + 4f * wide;
                    float shoal = (1f - bank) * RegionNoise.Smooth(-64f, -37f, distance);
                    float sandHeight = Mathf.Max(-12f, distance * (distance < 0f ? Mathf.Lerp(0.093f, 0.063f, shoal) : 0.103f))
                        + 1.3f * RegionNoise.Smooth(19f, 42f, distance);
                    float stoneHeight = Mathf.Max(-12f, distance * 0.19f)
                        + lift * RegionNoise.Smooth(8f, 40f, distance + 7f * wide)
                        + (0.68f * seam + wide) * RegionNoise.Smooth(-13f, 2f, distance);
                    float headland = (1f - RegionNoise.Smooth(-80f, -39f, px)) * RegionNoise.Smooth(28f, 48f, distance);
                    stoneHeight -= 0.71f * Mathf.Max(0f, stoneHeight - 15.5f - 2.4f * wide) * headland;
                    float strataHeight = Mathf.Lerp(1.8f, 2.45f, 0.5f + 0.5f * Mathf.Sin(px * 0.045f));
                    float shelf = (stoneHeight - 0.031f * px + 0.016f * pz + 0.47f * wide) / strataHeight;
                    float tread = Mathf.Floor(shelf);
                    float beds = strataHeight * (tread + RegionNoise.Smooth(0.34f, 0.80f, shelf - tread))
                        + 0.031f * px - 0.016f * pz - 0.47f * wide;
                    stoneHeight = Mathf.Lerp(stoneHeight, beds, 0.88f);
                    float joint = (1f - RegionNoise.Smooth(0.16f, 0.82f, Mathf.Abs(Mathf.Repeat(px + 0.63f * pz + 2.2f * wide, 11.4f) - 4.6f)));
                    stoneHeight -= 0.48f * joint * RegionNoise.Smooth(-4f, 5f, distance) * bare;
                    float shaped = Mathf.Lerp(sandHeight, stoneHeight, bank);

                    // Multiple low beds share a submerged apron; the channel toward the beach stays open.
                    float reef = ReefHill(px, pz, Reef, 23f, 10.1f);
                    reef = Mathf.Max(reef, ReefHill(px, pz, Reef + new Vector2(-23f, 3f), 15f, 7.4f) * 0.82f);
                    reef = Mathf.Max(reef, ReefHill(px, pz, Reef + new Vector2(14f, -4.5f), 12f, 6.3f) * 0.77f);
                    reef *= 0.93f + 0.17f * seam;
                    if (reef > 0.02f)
                    {
                        float crest = -4.2f + 7.2f * reef;
                        float tilt = 0.032f * (px - Reef.x) - 0.034f * (pz - Reef.y) + 0.40f * seam;
                        float tier = (crest - tilt) / 1.17f;
                        float slab = Mathf.Floor(tier);
                        float ledge = 1.17f * (slab + RegionNoise.Smooth(0.32f, 0.77f, tier - slab)) + tilt;
                        shaped = Mathf.Max(shaped, Mathf.Lerp(crest, ledge, 0.89f) - joint * 0.37f + seam * 0.23f);
                    }
                    float reefRock = RegionNoise.Smooth(0.16f, 0.55f, reef);
                    float foreground = RegionNoise.Smooth(53f, 69f, px) * (1f - RegionNoise.Smooth(101f, 126f, px))
                        * RegionNoise.Smooth(-14f, -7f, pz) * (1f - RegionNoise.Smooth(26f, 51f, pz));
                    float bench = 1.85f + 0.036f * (px - Basin.x) + 0.062f * Mathf.Max(0f, pz + 7f)
                        + 0.61f * seam - 0.29f * joint;
                    shaped = Mathf.Lerp(shaped, Mathf.Min(shaped, bench), foreground);
                    float basinRock = ShapePool(px, pz, seam, ref shaped);
                    float rock = Mathf.Max(bank, Mathf.Max(reefRock, Mathf.Max(basinRock, foreground)));
                    float local = (1f - RegionNoise.Smooth(124f, 181f, Mathf.Abs(px)))
                        * (1f - RegionNoise.Smooth(103f, 183f, Mathf.Abs(pz)));
                    ground[i] = Mathf.Lerp(y, shaped, local);
                    stone[i] = Mathf.Max(rock * bare, foreground * 0.87f) * local;
                    sand[i] = (1f - rock) * (1f - RegionNoise.Smooth(25f, 43f, distance)) * local;
                    exposure[i] = Mathf.Lerp(0.16f, 0.64f, bank) * local + 0.36f * reefRock;
                    materialWeight[i] = Mathf.Clamp01(stone[i] + sand[i]);
                }
            }

            internal float Shore(float x)
            {
                float q = (x - sweep) / beachWidth;
                return 17f - Mathf.Min(47f, 18.5f * q * q) + 2.4f * Mathf.Sin(x / 23f);
            }

            private static float ReefHill(float x, float z, Vector2 center, float width, float length)
            {
                float u = x - center.x, v = z - center.y;
                float dx = Mathf.Abs((u + 0.30f * v) / width), dz = Mathf.Abs((v - 0.17f * u) / length);
                float edge = Mathf.Max(0.94f * dx + 0.37f * dz, 0.86f * dz + 0.38f * dx);
                return 1f - RegionNoise.Smooth(0.33f, 1.20f, edge);
            }

            private static float ShapePool(float x, float z, float grit, ref float height)
            {
                float u = x - Basin.x, v = z - Basin.y;
                float a = Mathf.Abs((u + 0.27f * v) / 10.2f), b = Mathf.Abs((v - 0.31f * u) / 5.3f);
                float radius = Mathf.Max(0.89f * a + 0.29f * b, 0.92f * b + 0.45f * a) + 0.17f * grit;
                float rock = 1f - RegionNoise.Smooth(1.02f, 1.62f, radius);
                float terraceEdge = Mathf.Max(Mathf.Abs(u - 0.27f * v) / 26f, Mathf.Abs(v + 0.19f * u) / 17.2f);
                float platform = 1f - RegionNoise.Smooth(0.72f, 1.18f, terraceEdge);
                float deck = 1.55f + 0.026f * u + 0.028f * v + 0.34f * grit;
                height = Mathf.Lerp(height, deck, platform);
                float bowl = Mathf.Lerp(-0.23f, 1.23f, RegionNoise.Smooth(0.52f, 1.17f, radius)) + 0.22f * grit;
                height = Mathf.Lerp(height, bowl, rock);
                // The shallow spillway connects at high tide and retains the isolated basin at low tide.
                float gutter = (1f - RegionNoise.Smooth(0.85f, 2.45f, Mathf.Abs(u + v * 0.17f)))
                    * (1f - RegionNoise.Smooth(-2f, 0f, v)) * RegionNoise.Smooth(-28f, -22f, v);
                height = Mathf.Lerp(height, Mathf.Min(height, 0.57f + 0.10f * grit), gutter);
                return Mathf.Max(platform, Mathf.Max(rock, gutter));
            }

            internal Mesh CreateMesh(out Color32[] colors)
            {
                colors = new Color32[ground.Length];
                var vertices = new Vector3[ground.Length];
                var uv = new Vector2[ground.Length];
                var indices = new int[(Resolution - 1) * (Resolution - 1) * 6];
                uint seed = unchecked((uint)Seed);
                int k = 0;
                for (int z = 0; z < Resolution; z++)
                for (int x = 0; x < Resolution; x++)
                {
                    int i = x + z * Resolution;
                    float px = x * Step - HalfSpan, pz = z * Step - HalfSpan;
                    vertices[i] = new Vector3(px, ground[i], pz);
                    uv[i] = new Vector2((x + 0.5f) / Resolution, (z + 0.5f) / Resolution);
                    float grain = RegionNoise.Fbm(px / 9f, pz / 13f, 3, seed + 38u);
                    float heath = RegionNoise.Smooth(-0.29f, 0.33f, grain);
                    Color bare = Color.Lerp(new Color(0.29f, 0.27f, 0.19f), new Color(0.16f, 0.21f, 0.13f), heath);
                    Color sediment = Color.Lerp(new Color(0.55f, 0.53f, 0.48f), new Color(0.68f, 0.61f, 0.46f), sand[i]);
                    colors[i] = Color.Lerp(bare, sediment, materialWeight[i]);
                    if (x == Resolution - 1 || z == Resolution - 1) continue;
                    indices[k++] = i; indices[k++] = i + Resolution; indices[k++] = i + 1;
                    indices[k++] = i + 1; indices[k++] = i + Resolution; indices[k++] = i + Resolution + 1;
                }
                var result = new Mesh { name = "Cove terrain", indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.DontSave };
                result.vertices = vertices;
                result.uv = uv;
                result.triangles = indices;
                result.RecalculateNormals();
                return result;
            }

            internal bool[] SeaAt(float level) => RegionOceanMask.ConnectedWater(ground, Resolution, level, i => i / Resolution < Resolution / 2);

            internal bool[] PoolAt(float level, int basin) => RegionOceanMask.ConnectedWater(ground, Resolution, level, source: basin);

            internal float DrySandArea(float level)
            {
                int count = 0;
                for (int i = 0; i < ground.Length; i++)
                    if (sand[i] > 0.65f && ground[i] > level && ground[i] < 2.7f) count++;
                return count * Step * Step;
            }

            internal List<Vector4> SurfaceAt(float level)
            {
                var channels = new List<Vector4>(ground.Length);
                for (int i = 0; i < ground.Length; i++)
                {
                    int x = i % Resolution, z = i / Resolution;
                    float toBasin = (new Vector2(x * Step - HalfSpan, z * Step - HalfSpan) - Basin).magnitude;
                    float surface = Mathf.Lerp(Mathf.Max(level, RetainedWater), level, RegionNoise.Smooth(7f, 15f, toBasin));
                    float wet = WetnessAt(ground[i], surface, stone[i]);
                    channels.Add(new Vector4(stone[i], wet, 1f, materialWeight[i]));
                }
                return channels;
            }

            internal Texture2D DepthAt(float level, bool[] connected, string name)
            {
                var pixels = new Color32[ground.Length];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = DepthPixel(level - ground[i], exposure[i], sand[i], connected == null || connected[i]);
                return MakeTexture(name, pixels);
            }

            internal int IndexAt(float x, float z) => Mathf.Clamp(Mathf.RoundToInt((x + HalfSpan) / Step), 0, Resolution - 1)
                + Mathf.Clamp(Mathf.RoundToInt((z + HalfSpan) / Step), 0, Resolution - 1) * Resolution;

            internal float HeightAt(float x, float z) => SampleHeight(ground, Resolution, (x + HalfSpan) / Step, (z + HalfSpan) / Step);
        }
    }
}
