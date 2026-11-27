using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap
{
    [Serializable]
    public sealed class RegionRockyCoastSpec
    {
        public float MapX = 69.97070f;
        public float MapY = 79.88281f;
        public float SeaYaw = 30.47f;
        public int ShapeSeed = 8;
        public int PropsSeed = 8;
        [Range(12f, 60f)] public float CliffHeight = 35f;
        [Range(12f, 55f)] public float ShelfWidth = 27f;
        public float FixedWaveTime = -1f;
    }

    /// <summary>広域の海岸線に合わせて、岬の岩棚・崖・海面を組み立てる。</summary>
    public sealed class RegionRockyCoast : MonoBehaviour
    {
        private UnityEngine.Object[] _generated;

        private void OnDestroy()
        {
            if (_generated == null) return;
            foreach (UnityEngine.Object item in _generated)
            {
                if (Application.isPlaying) Destroy(item);
                else DestroyImmediate(item);
            }
        }

        public sealed class Result
        {
            public GameObject Root { get; internal set; }
            public RegionTerrainView.Result View { get; internal set; }
            public RegionPropView.Result Props { get; internal set; }
            public float ShoreAgreement { get; internal set; }
            public float RaisedCliffMeters { get; internal set; }
            public int RaisedCells { get; internal set; }
            public RegionPropView.Result RockfallProps { get; internal set; }
            public RegionPropView.Result ShoreCobbles { get; internal set; }
            public float CliffFootMeters { get; internal set; }
            public float CliffRiseMeters { get; internal set; }
            public float JointYaw { get; internal set; }
            public float LocalStepMeters { get; internal set; }
            public int LocalTriangles { get; internal set; }
            public float LocalHeightDelta { get; internal set; }
        }

        public static Result Build(RegionData region, RegionRockyCoastSpec spec, bool shape, RegionPropCatalog catalog = null,
            bool refineCliff = true, bool surfaceDetail = true)
        {
            RegionDetail detail = RegionDetailBuilder.Build(region, new RegionDetailSpec
            {
                CenterMapX = spec.MapX,
                CenterMapY = spec.MapY,
                SizeKm = 3.2f,
                Resolution = 641,
            });
            RegionData terrain = detail.Data;
            int n = terrain.Resolution;
            float cell = terrain.CellMeters;
            float span = (n - 1) * cell;
            float[] ground = (float[])terrain.Height.Clone();
            float[] shore = DistancesToShore(terrain);
            byte[] albedo = RegionMapRenderer.Render(terrain, new RegionRenderOptions
            {
                Style = RegionMapStyle.Satellite, CloudCover = 0f, Exaggeration = 0f, CastShadow = 0f,
            });
            var bottom = new Color32[ground.Length];
            uint seed = unchecked((uint)spec.ShapeSeed);
            float maxRaised = 0f;
            int raised = 0;
            Vector2 focus = FindShore(terrain, shore, spec.SeaYaw);
            float originX = detail.OriginMapX * region.KmPerMapUnit * 1000f;
            float originZ = detail.OriginMapY * region.KmPerMapUnit * 1000f;

            for (int z = 0; z < n; z++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = x + z * n;
                    float east = originX + x * cell;
                    float south = originZ + z * cell;
                    float distance = shore[i] * cell + cell * 0.32f;
                    float folded = RegionNoise.Fbm(east / 246f, south / 246f, 3, seed);
                    float rock = RegionNoise.Smooth(-0.29f, 0.3f, folded);
                    if (ground[i] <= 0f)
                    {
                        if (shape)
                        {
                            float bed = -Mathf.Max(0.06f, distance * Mathf.Lerp(0.105f, 0.22f, rock));
                            ground[i] = Mathf.Lerp(bed, Mathf.Min(ground[i], -0.06f), RegionNoise.Smooth(12f, 82f, distance));
                        }
                        else ground[i] = Mathf.Min(ground[i], -0.06f);
                        bottom[i] = new Color32((byte)Mathf.Clamp(-ground[i] * 8f, 0f, 255f),
                            (byte)Mathf.Lerp(30f, 235f, rock), 0, 255);
                        continue;
                    }
                    if (!shape || distance >= 195f || terrain.Biome[i] == RegionBiome.Lake || terrain.Biome[i] == RegionBiome.River
                        || (terrain.WaterCoverage != null && terrain.WaterCoverage[i] > 0.04f)) continue;
                    float outer = Mathf.Min(Mathf.Min(x, n - 1 - x), Mathf.Min(z, n - 1 - z)) * cell;
                    float cliffMass = RegionNoise.Smooth(0.25f, 0.72f, rock + 0.12f * RegionNoise.Smooth(10f, 70f, terrain.Height[i]));
                    float blend = (1f - RegionNoise.Smooth(58f, 190f, distance)) * RegionNoise.Smooth(1f, 105f, outer);
                    float strike = (east * 0.84f + south * 0.54f) / 9.5f;
                    float cuts = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(strike + 4f * rock), 5f);
                    float shelfWidth = spec.ShelfWidth * Mathf.Lerp(0.58f, 1.38f, rock) + cuts * 6f;
                    float face = RegionNoise.Smooth(shelfWidth, shelfWidth + 9f + cuts * 15f, distance);
                    float strata = RegionNoise.Fbm(east / 23f, south / 23f, 2, seed + 481u);
                    float shelf = Mathf.Min(distance * 0.11f, 3.1f) + 0.52f * rock + strata * 0.8f;
                    float height = shelf + face * (2f + spec.CliffHeight * cliffMass * Mathf.Lerp(0.76f, 1.18f, rock))
                        - face * cuts * 4.4f * cliffMass;
                    ground[i] = Mathf.Max(0.06f, Mathf.Lerp(ground[i], height, blend));
                    float lift = ground[i] - terrain.Height[i];
                    if (lift > 1f) raised++;
                    maxRaised = Mathf.Max(maxRaised, lift);
                    float wet = 1f - RegionNoise.Smooth(0.35f, 2f, ground[i]);
                    float tone = Mathf.Lerp(1.04f, 0.52f, wet) * Mathf.Lerp(0.86f, 1.14f, rock) + strata * 0.13f;
                    Tint(albedo, i, blend * (1f - face * 0.48f), tone);
                }
            }

            var grid = RegionTerrainMesh.Build(terrain, 1, 1f);
            for (int i = 0; i < ground.Length; i++) grid.Positions[i * 3 + 1] = ground[i];
            var rockfall = shape ? RegionCoastRockfall.Locate(region, detail, ground, grid.Indices, spec.SeaYaw, surfaceDetail) : null;
            var root = new GameObject(shape ? "Rocky Coast" : "Coast Before");
            Mesh mesh = RegionTerrainView.TerrainMesh(grid, null);
            Mesh ledge = rockfall != null && refineCliff ? rockfall.Refine(mesh) : null;
            var texture = RegionMapTexture.Create(albedo, n);
            var terrainMat = new Material(Resources.Load<Shader>(RegionTerrainView.ShaderResource)) { hideFlags = HideFlags.DontSave };
            terrainMat.SetTexture("_BaseMap", texture);
            terrainMat.SetColor("_RockColor", new Color(0.49f, 0.48f, 0.425f));
            terrainMat.SetFloat("_BandAmount", shape ? 0.12f : 0.1f);
            if (shape)
            {
                var profile = RegionBiomeSurface.ProfileFor(RegionBiome.Coast);
                profile.CliffR = 0.46f;
                profile.CliffG = 0.45f;
                profile.CliffB = 0.41f;
                profile.CliffTileMetres = 3.6f;
                terrainMat.EnableKeyword("REGION_BIOME_SURFACE");
                RegionSurfaceMaterials.BindBiomeSurfaces(terrainMat, profile);
            }
            RegionTerrainView.AddPart(root.transform, "Headlands and ledges", mesh, terrainMat);
            Material rockSurface = ledge != null && surfaceDetail ? CoastalSurface(terrainMat) : null;
            if (ledge != null) RegionTerrainView.AddPart(root.transform, ledge.name, ledge, rockSurface ?? terrainMat);

            var waterMat = new Material(Resources.Load<Shader>("RegionMap3D/RegionCoastWater")) { hideFlags = HideFlags.DontSave };
            var depth = new Texture2D(n, n, TextureFormat.RGBA32, false, true)
            {
                name = "Shore Depth and Exposure", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave,
            };
            depth.SetPixels32(bottom);
            depth.Apply(false, true);
            waterMat.SetTexture("_Shore", depth);
            waterMat.SetFloat("_Extent", span);
            waterMat.SetFloat("_WaveStrength", shape ? 1f : 0f);
            waterMat.SetFloat("_FixedTime", spec.FixedWaveTime);
            Mesh water = WaterMesh(span);
            MeshRenderer waterRenderer = RegionTerrainView.AddPart(root.transform, "Sea", water, waterMat);
            waterRenderer.shadowCastingMode = ShadowCastingMode.Off;
            root.AddComponent<RegionRockyCoast>()._generated = new UnityEngine.Object[]
            {
                mesh, ledge, texture, terrainMat, rockSurface, water, depth, waterMat,
            };

            Vector2 waterline = rockfall?.Waterline ?? focus;
            var view = new RegionTerrainView.Result
            {
                Root = root, Terrain = terrain,
                Focus = new Vector3(waterline.x, shape ? 0f : 3f, -waterline.y), FacingYawDegrees = spec.SeaYaw,
            };
            var result = new Result
            {
                Root = root, View = view, ShoreAgreement = detail.CoastAgreement,
                RaisedCliffMeters = maxRaised, RaisedCells = raised,
                CliffFootMeters = rockfall?.FootDistance ?? 0f, CliffRiseMeters = rockfall?.CliffRise ?? 0f,
                JointYaw = rockfall?.JointYaw ?? 0f, LocalStepMeters = rockfall?.StepMeters ?? 0f,
                LocalTriangles = rockfall?.AddedTriangles ?? 0, LocalHeightDelta = rockfall?.HeightDelta ?? 0f,
            };
            if (shape && catalog != null)
            {
                result.Props = AddProps(root.transform, catalog, spec, terrain, ground, shore, focus, ledge != null ? rockfall : null);
                if (ledge != null)
                {
                    var deposit = new GameObject("Debris below the joint");
                    deposit.transform.SetParent(root.transform, false);
                    result.RockfallProps = RegionPropView.AddTo(deposit.transform, rockfall.Deposits(catalog, spec.PropsSeed), rockfall.HeightAt);
                    MatchDepositColor(result.RockfallProps.RealRoot);
                    if (surfaceDetail)
                    {
                        var foreshore = new GameObject("Rounded cobbles at the waterline");
                        foreshore.transform.SetParent(root.transform, false);
                        result.ShoreCobbles = RegionPropView.AddTo(foreshore.transform, rockfall.ShoreCobbles(catalog, spec.PropsSeed), rockfall.HeightAt);
                        RegionPropView.SettleLoosePieces(result.ShoreCobbles, rockfall.HeightAt, 0.024f);
                        MatchDepositColor(result.ShoreCobbles.RealRoot, wet: true);
                    }
                }
            }
            return result;
        }

        private static Material CoastalSurface(Material baseTerrain)
        {
            var material = new Material(baseTerrain) { name = "Exposed rock and wave-washed ground", hideFlags = HideFlags.DontSave };
            var profile = RegionBiomeSurface.ProfileFor(RegionBiome.Coast);
            profile.CliffR = 0.52f;
            profile.CliffG = 0.465f;
            profile.CliffB = 0.43f;
            profile.CliffTileMetres = 4.0f;
            profile.GroundTileMetres = 2.2f;
            profile.SubsoilTileMetres = 2.75f;
            profile.GroundPhotoColor = 0.86f;
            profile.DetailStrength = 0.96f;
            profile.NormalStrength = 1.4f;
            RegionSurfaceMaterials.BindBiomeSurfaces(material, profile);
            var cliff = CoastTextures("RegionMap3D/Surfaces/Cliff/rock_boulder_cracked_");
            if (cliff.IsComplete) RegionSurfaceMaterials.BindBiomeTextureSet(material, "_BiomeCliff", cliff);
            var gravel = CoastTextures("RegionMap3D/Surfaces/Talus/rocks_ground_06_");
            if (gravel.IsComplete)
            {
                RegionSurfaceMaterials.BindBiomeTextureSet(material, "_BiomeGround", gravel);
                RegionSurfaceMaterials.BindBiomeTextureSet(material, "_BiomeSubsoil", gravel);
            }
            material.SetFloat("_CoastalRockDetail", 1f);
            return material;
        }

        private static RegionBiomeSurfaceTextures.TextureSet CoastTextures(string stem) => new RegionBiomeSurfaceTextures.TextureSet
        {
            Color = Resources.Load<Texture2D>(stem + "diff_1k"),
            Normal = Resources.Load<Texture2D>(stem + "nor_gl_1k"),
            Roughness = Resources.Load<Texture2D>(stem + "rough_1k"),
        };

        private static void MatchDepositColor(GameObject roots, bool wet = false)
        {
            if (roots == null) return;
            int baseColor = Shader.PropertyToID("_BaseColor");
            var block = new MaterialPropertyBlock();
            foreach (Renderer renderer in roots.GetComponentsInChildren<Renderer>(true))
            {
                Material mat = renderer.sharedMaterial;
                if (mat == null || !mat.HasProperty(baseColor)) continue;
                Color tint = mat.GetColor(baseColor);
                renderer.GetPropertyBlock(block);
                Color coast = wet ? new Color(tint.r * 0.54f, tint.g * 0.63f, tint.b * 0.72f, tint.a)
                    : new Color(tint.r * 0.64f, tint.g * 0.91f, tint.b * 1.13f, tint.a);
                block.SetColor(baseColor, coast);
                renderer.SetPropertyBlock(block);
            }
        }

        private static void Tint(byte[] pixels, int i, float weight, float tone)
        {
            int p = i * 4;
            pixels[p] = (byte)Mathf.Lerp(pixels[p], Mathf.Min(255f, 131f * tone), weight);
            pixels[p + 1] = (byte)Mathf.Lerp(pixels[p + 1], Mathf.Min(255f, 131f * tone), weight);
            pixels[p + 2] = (byte)Mathf.Lerp(pixels[p + 2], Mathf.Min(255f, 124f * tone), weight);
        }

        private static float[] DistancesToShore(RegionData data)
        {
            int n = data.Resolution;
            var dist = new float[data.Height.Length];
            for (int z = 0; z < n; z++)
            {
                for (int x = 0; x < n; x++)
                {
                    int i = x + z * n;
                    bool land = data.IsLand(i);
                    bool shore = (x > 0 && land != data.IsLand(i - 1)) || (x + 1 < n && land != data.IsLand(i + 1))
                        || (z > 0 && land != data.IsLand(i - n)) || (z + 1 < n && land != data.IsLand(i + n));
                    dist[i] = shore ? 0f : n * 2f;
                }
            }
            Sweep(0, 0, 1);
            Sweep(n - 1, n - 1, -1);
            return dist;

            void Sweep(int startX, int startZ, int step)
            {
                for (int z = startZ; z >= 0 && z < n; z += step)
                {
                    for (int x = startX; x >= 0 && x < n; x += step)
                    {
                        int i = x + z * n;
                        bool left = x - step >= 0 && x - step < n;
                        bool right = x + step >= 0 && x + step < n;
                        bool previous = z - step >= 0 && z - step < n;
                        if (left) dist[i] = Mathf.Min(dist[i], 1f + dist[i - step]);
                        if (previous) dist[i] = Mathf.Min(dist[i], 1f + dist[i - n * step]);
                        if (left && previous) dist[i] = Mathf.Min(dist[i], 1.414214f + dist[i - n * step - step]);
                        if (right && previous) dist[i] = Mathf.Min(dist[i], 1.414214f + dist[i - n * step + step]);
                    }
                }
            }
        }

        private static Vector2 FindShore(RegionData data, float[] shore, float yawDegrees)
        {
            int n = data.Resolution;
            float center = (n - 1) * 0.5f;
            var point = new Vector2(center, center) * data.CellMeters;
            float best = float.PositiveInfinity;
            float yaw = yawDegrees * Mathf.Deg2Rad;
            int seaX = Mathf.RoundToInt(Mathf.Sin(yaw) * 48f / data.CellMeters);
            int seaZ = Mathf.RoundToInt(Mathf.Cos(yaw) * 48f / data.CellMeters);
            for (int z = Mathf.Max(1, -seaZ); z < Mathf.Min(n - 1, n - seaZ); z++)
            {
                for (int x = Mathf.Max(1, -seaX); x < Mathf.Min(n - 1, n - seaX); x++)
                {
                    int i = x + z * n;
                    if (shore[i] != 0f || !data.IsLand(i) || data.IsLand(x + seaX + (z + seaZ) * n)) continue;
                    float dist = (x - center) * (x - center) + (z - center) * (z - center);
                    if (dist >= best) continue;
                    best = dist;
                    point = new Vector2(x, z) * data.CellMeters;
                }
            }
            if (float.IsPositiveInfinity(best)) throw new ArgumentException("The local window needs a shoreline facing the sea.");
            return point;
        }

        private static Mesh WaterMesh(float span)
        {
            const float horizon = 24000f;
            var mesh = new Mesh { name = "Sea surface", hideFlags = HideFlags.DontSave };
            mesh.vertices = new[]
            {
                new Vector3(-horizon, 0f, horizon), new Vector3(span + horizon, 0f, horizon),
                new Vector3(-horizon, 0f, -span - horizon), new Vector3(span + horizon, 0f, -span - horizon),
            };
            mesh.triangles = new[] { 0, 1, 2, 2, 1, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static RegionPropView.Result AddProps(Transform parent, RegionPropCatalog catalog, RegionRockyCoastSpec spec,
            RegionData data, float[] ground, float[] shore, Vector2 focus, RegionCoastRockfall rockfall)
        {
            var slice = new RegionPropProofSlice
            {
                Biome = RegionBiome.Coast, UseRealAssets = true, HasGroundColor = true,
                GroundRed = 0.51f, GroundGreen = 0.51f, GroundBlue = 0.48f,
            };
            var random = new System.Random(spec.PropsSeed);
            int n = data.Resolution;
            float cell = data.CellMeters;
            int radius = Mathf.CeilToInt(420f / cell);
            int fx = Mathf.RoundToInt(focus.x / cell), fz = Mathf.RoundToInt(focus.y / cell);
            var slab = catalog.GetProp("nat_stratified_slab");
            var gravel = catalog.GetProp("nat_pebble_cluster");
            var driftwood = catalog.GetProp("nat_dry_root_log");
            for (int z = Mathf.Max(2, fz - radius); z < Mathf.Min(n - 2, fz + radius); z += 3)
            {
                for (int x = Mathf.Max(2, fx - radius); x < Mathf.Min(n - 2, fx + radius); x += 3)
                {
                    int i = x + z * n;
                    float h = ground[i];
                    float d = shore[i] * cell;
                    if (h < 1.6f || h > 5.8f || d < 7f || d > 44f || data.Biome[i] == RegionBiome.River
                        || data.Biome[i] == RegionBiome.Lake || (data.WaterCoverage != null && data.WaterCoverage[i] > 0.04f)) continue;
                    float dx = (ground[i + 1] - ground[i - 1]) / (2f * cell);
                    float dz = (ground[i + n] - ground[i - n]) / (2f * cell);
                    float slope = Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;
                    if (slope > 18f || random.NextDouble() > 0.42) continue;
                    double pick = random.NextDouble();
                    RegionPropDefinition prop = d > 17f && h > 2.2f && pick < 0.2 ? driftwood : pick < 0.46 ? slab : gravel;
                    float yaw = spec.SeaYaw + 90f + (float)(random.NextDouble() * 38f - 19f);
                    var item = RegionPropPlacementSolver.ResolveSingle(prop, unchecked((uint)spec.PropsSeed), i, x * cell,
                        h, z * cell, slope, dx, dz, yawOverride: yaw, biome: RegionBiome.Coast);
                    if (rockfall != null && rockfall.InDeposit(item.X, item.Z)) continue;
                    item.Y = SampleHeight(item.X, item.Z) - item.EmbedDepthM;
                    if (item.Y > 1.25f) slice.Instances.Add(item);
                }
            }
            return RegionPropView.AddTo(parent, slice, SampleHeight);

            float SampleHeight(float x, float z)
            {
                if (rockfall != null) return rockfall.HeightAt(x, z);
                float gx = Mathf.Clamp(x / cell, 0f, n - 1);
                float gz = Mathf.Clamp(z / cell, 0f, n - 1);
                int ix = Mathf.Min(n - 2, (int)gx), iz = Mathf.Min(n - 2, (int)gz);
                int i = ix + iz * n;
                return Mathf.Lerp(Mathf.Lerp(ground[i], ground[i + 1], gx - ix),
                    Mathf.Lerp(ground[i + n], ground[i + n + 1], gx - ix), gz - iz);
            }
        }
    }
}
