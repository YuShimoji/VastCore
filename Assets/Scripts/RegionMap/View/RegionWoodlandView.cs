using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap
{
    public static class RegionWoodlandView
    {
        public sealed class Result
        {
            internal GameObject Baseline, Canopy, Trail, Understory;
            internal MeshRenderer Ground;
            internal Material BaselineGround, WoodlandGround;
            public int CanopyTriangles { get; internal set; }
            public int UnderstoryTriangles { get; internal set; }
            public int RealProps { get; internal set; }

            public void Show(bool woodland, bool detail)
            {
                if (Baseline) Baseline.SetActive(!woodland);
                Canopy.SetActive(woodland);
                Trail.SetActive(woodland);
                Understory.SetActive(woodland && detail);
                Ground.sharedMaterial = woodland && detail ? WoodlandGround : BaselineGround;
            }
        }

        public static Result AddTo(RegionTerrainView.LookResult look, RegionWoodlandLayout layout)
        {
            RegionLookScene scene = look.Scene;
            Transform parent = look.Root.transform;
            Shader shader = Resources.Load<Shader>("RegionMap3D/RegionWoodland");
            if (!shader || !shader.isSupported) throw new InvalidOperationException("The woodland sample needs the RegionWoodland URP shader.");
            var plants = new Material(shader) { name = "Woodland", hideFlags = HideFlags.DontSave };
            var trail = new Material(shader) { name = "Woodland Trail", hideFlags = HideFlags.DontSave };
            trail.SetFloat("_Ground", 1f);
            MeshRenderer ground = parent.Find(scene.InnerTerrain.Name).GetComponent<MeshRenderer>();
            var result = new Result
            {
                Baseline = parent.Find("Vegetation")?.gameObject,
                Canopy = Group(parent, "Trees"),
                Trail = Group(parent, "Footpath"),
                Understory = Group(parent, "Understory and deadwood"),
                Ground = ground,
                BaselineGround = ground.sharedMaterial,
                WoodlandGround = BakeForestFloor(scene, layout, ground.sharedMaterial),
            };
            result.CanopyTriangles = BuildPlants(result.Canopy.transform, plants, scene, layout.Canopy, true);
            result.UnderstoryTriangles = BuildPlants(result.Understory.transform, plants, scene, layout.Understory, false);
            result.RealProps = AddForestFloor(result.Understory.transform, plants, scene, layout);
            AddMesh(result.Trail.transform, "Ground ribbon", BuildTrail(scene, layout), trail, ShadowCastingMode.Off);
            result.Show(true, true);
            return result;
        }

        private static GameObject Group(Transform parent, string name)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group;
        }

        private static void AddMesh(Transform parent, string name, RegionWoodlandGeometry geometry, Material material, ShadowCastingMode cast)
        {
            if (geometry.TriangleCount == 0) return;
            GameObject go = Group(parent, name);
            go.AddComponent<MeshFilter>().sharedMesh = geometry.ToMesh(name);
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = cast;
        }

        private static int BuildPlants(Transform parent, Material material, RegionLookScene scene,
            List<RegionWoodlandPlant> plants, bool tall)
        {
            var forms = new Dictionary<(RegionWoodlandForm, int, bool), RegionWoodlandGeometry>();
            var tiles = new Dictionary<Vector2Int, RegionWoodlandGeometry>();
            foreach (RegionWoodlandPlant plant in plants)
            {
                // Keep the denser silhouettes around the part of the trail used for inspection.
                // These are fixed review meshes. Camera-driven LOD selection belongs to the ground view.
                bool detailed = tall
                    ? Mathf.Abs(plant.East) < 54f && Mathf.Abs(plant.South) < 82f
                    : Mathf.Abs(plant.East) < 29f && Mathf.Abs(plant.South) < 52f;
                var form = (plant.Form, plant.Shape, detailed);
                if (!forms.TryGetValue(form, out RegionWoodlandGeometry template))
                {
                    template = RegionWoodlandGeometry.Plant(plant.Form, plant.Shape, detailed);
                    forms.Add(form, template);
                }
                float size = tall ? 52f : 23f;
                var cell = new Vector2Int(Mathf.FloorToInt(plant.East / size), Mathf.FloorToInt(plant.South / size));
                if (!tiles.TryGetValue(cell, out RegionWoodlandGeometry tile))
                {
                    tile = new RegionWoodlandGeometry();
                    tiles.Add(cell, tile);
                }
                var basePoint = new Vector3(scene.CenterX + plant.East, plant.Ground - (tall ? 0f : 0.035f), -scene.CenterZ - plant.South);
                tile.Append(template, basePoint, plant.Height, plant.Yaw);
            }
            int triangles = 0;
            foreach (var pair in tiles)
            {
                AddMesh(parent, $"{pair.Key.x},{pair.Key.y}", pair.Value, material, ShadowCastingMode.On);
                triangles += pair.Value.TriangleCount;
            }
            return triangles;
        }

        private static Material BakeForestFloor(RegionLookScene scene, RegionWoodlandLayout layout, Material original)
        {
            var baseline = (Texture2D)original.GetTexture("_BaseMap");
            int n = baseline.width;
            Color32[] pixels = baseline.GetPixels32();
            var shelter = new float[pixels.Length];
            float cell = (scene.InnerMaxX - scene.InnerMinX) / (n - 1);
            float eastStart = scene.InnerMinX - scene.CenterX;
            float southStart = scene.InnerMinZ - scene.CenterZ;
            foreach (RegionWoodlandPlant plant in layout.Canopy)
            {
                float radius = plant.Height * (plant.Form == RegionWoodlandForm.Conifer ? 0.25f : 0.33f);
                int ix0 = Mathf.Max(0, Mathf.FloorToInt((plant.East - eastStart - radius) / cell));
                int ix1 = Mathf.Min(n - 1, Mathf.CeilToInt((plant.East - eastStart + radius) / cell));
                int iz0 = Mathf.Max(0, Mathf.FloorToInt((plant.South - southStart - radius) / cell));
                int iz1 = Mathf.Min(n - 1, Mathf.CeilToInt((plant.South - southStart + radius) / cell));
                for (int z = iz0; z <= iz1; z++)
                for (int x = ix0; x <= ix1; x++)
                {
                    float dx = eastStart + x * cell - plant.East, dz = southStart + z * cell - plant.South;
                    float influence = Mathf.Clamp01(1.22f - (dx * dx + dz * dz) / (radius * radius));
                    int index = x + z * n;
                    shelter[index] = Mathf.Max(influence, shelter[index]);
                }
            }
            bool tropical = layout.Preset == RegionWoodlandPreset.Rainforest;
            var dry = tropical ? new Color32(111, 99, 70, 255) : new Color32(133, 111, 76, 255);
            var shaded = tropical ? new Color32(73, 65, 47, 255) : new Color32(95, 74, 51, 255);
            var grass = new Color32(113, 129, 68, 255);
            RegionData ground = scene.Inner.Data;
            for (int z = 0; z < n; z++)
            for (int x = 0; x < n; x++)
            {
                int index = x + z * n;
                if (ground.Height[index] <= 0f || ground.LakeDepth[index] > 0f || ground.Biome[index] == RegionBiome.River) continue;
                float east = eastStart + x * cell, south = southStart + z * cell;
                float patch = RegionNoise.Gradient(east / 15f, south / 17f, 1851u);
                float area = 1f - RegionNoise.Smooth(320f, 386f, Mathf.Max(Mathf.Abs(east), Mathf.Abs(south)));
                float cover = Mathf.Clamp01(shelter[index] * 0.75f + patch * 0.30f + (tropical ? 0.26f : 0.11f));
                Color32 soil = Color32.Lerp(dry, shaded, cover);
                float meadow = layout.Meadow(east, south);
                float sunlight = Mathf.Clamp01(0.2f + patch * 0.65f - shelter[index] * 0.20f);
                Color32 floor = Color32.Lerp(soil, grass, Mathf.Max(meadow * 0.88f, sunlight));
                int pixel = x + (n - 1 - z) * n;
                pixels[pixel] = Color32.Lerp(pixels[pixel], floor, area * 0.92f);
            }
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                name = "Woodland floor", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var material = new Material(original) { name = "Woodland ground", hideFlags = HideFlags.DontSave };
            material.SetTexture("_BaseMap", texture);
            Vector4 blend = material.GetVector("_RegionBiomeGroundBlend");
            blend.x = Mathf.Min(blend.x, 0.35f);
            if (layout.Preset == RegionWoodlandPreset.Forest)
            {
                const string folder = "RegionMap3D/Woodland/forest_floor_";
                var litter = new RegionBiomeSurfaceTextures.TextureSet
                {
                    Color = Resources.Load<Texture2D>(folder + "diff_1k"),
                    Normal = Resources.Load<Texture2D>(folder + "nor_gl_1k"),
                    Roughness = Resources.Load<Texture2D>(folder + "rough_1k"),
                };
                if (!litter.IsComplete) throw new InvalidOperationException("The forest leaf-litter material is incomplete.");
                RegionSurfaceMaterials.BindBiomeTextureSet(material, "_BiomeGround", litter);
                Vector4 tile = material.GetVector("_RegionBiomeTile");
                tile.x = 1f / 2.1f;
                material.SetVector("_RegionBiomeTile", tile);
                blend.x = 0.58f;
            }
            material.SetVector("_RegionBiomeGroundBlend", blend);
            return material;
        }

        private static RegionWoodlandGeometry BuildTrail(RegionLookScene scene, RegionWoodlandLayout layout)
        {
            var mesh = new RegionWoodlandGeometry();
            const int columns = 7;
            const float halfLength = RegionWoodlandLayout.TrailLength / 2f;
            var heights = new float[columns];
            Color32 trailColour = layout.Preset switch
            {
                RegionWoodlandPreset.Rainforest => new Color32(122, 108, 86, 255),
                RegionWoodlandPreset.NaturePark => new Color32(157, 141, 107, 255),
                _ => new Color32(143, 122, 94, 255),
            };
            int previous = -1;
            for (float south = -halfLength; south <= halfLength; south += 0.62f)
            {
                float centre = layout.TrailEast(south);
                float width = layout.TrailHalfWidth * (1f + 0.25f * RegionNoise.Gradient(south / 13f, 2.6f, 917u));
                bool supported = true;
                for (int j = 0; j < columns; j++)
                {
                    float t = j * (2f / (columns - 1)) - 1f;
                    supported &= layout.TryGround(scene, centre + t * width * 1.34f, south, out heights[j], out _);
                }
                if (!supported)
                {
                    previous = -1;
                    continue;
                }
                int row = -1;
                byte cap = (byte)(255f * Mathf.Clamp01((halfLength - Mathf.Abs(south)) / 5f));
                for (int j = 0; j < columns; j++)
                {
                    float t = j * (2f / (columns - 1)) - 1f;
                    float x = scene.CenterX + centre + t * width * 1.34f;
                    float z = scene.CenterZ + south;
                    float y = heights[j] + 0.041f;
                    bool edge = j == 0 || j == columns - 1;
                    Color32 color = trailColour;
                    color.a = edge ? (byte)0 : cap;
                    int v = mesh.Add(new Vector3(x, y, -z), Vector3.up, color);
                    if (row < 0) row = v;
                }
                if (previous >= 0)
                {
                    for (int j = 0; j < columns - 1; j++)
                    {
                        mesh.Tri(previous + j, previous + j + 1, row + j);
                        mesh.Tri(row + j, previous + j + 1, row + j + 1);
                    }
                }
                previous = row;
            }
            return mesh;
        }

        private static int AddForestFloor(Transform parent, Material plants, RegionLookScene scene, RegionWoodlandLayout layout)
        {
            RegionBiomeSurfaceProfile biome = scene.BiomeSurface;
            var slice = new RegionPropProofSlice
            {
                Biome = scene.PrimaryBiome, RenderedHeightAt = scene.RenderedHeightAt, UseRealAssets = true, UseGrasslandVariants = true,
                HasGroundColor = true, GroundRed = biome.GroundSkirtR, GroundGreen = biome.GroundSkirtG, GroundBlue = biome.GroundSkirtB,
            };
            var debris = new RegionWoodlandGeometry();
            var rng = new System.Random(5021);
            bool tropical = layout.Preset == RegionWoodlandPreset.Rainforest;
            float[] stations = { -64f, -34f, -17f, 1.5f, 16f, 33f, 61f };
            for (int i = 0; i < stations.Length; i++)
            {
                float z = stations[i];
                float x = layout.TrailEast(z) + (i % 2 == 0 ? -1f : 1f) * (layout.TrailHalfWidth + 2.6f + i % 3);
                if (!layout.TryGround(scene, x, z, out float y, out float moisture)) continue;
                bool rock = (i & 1) == 0;
                float moss = Mathf.Clamp01((moisture - (tropical ? 0.36f : 0.49f)) * 2.25f);
                string id = rock ? "nat_grassland_outcrop" : "nat_dry_root_log";
                string variant = rock ? moss > 0.27f ? "moss_capped_boulder" : "rounded_granite_shelf"
                    : moss > 0.27f ? "mossy_fallen_branch" : "bleached_trunk";
                slice.Instances.Add(new RegionPropInstance
                {
                    PropId = id, MeshVariant = id + "__" + variant, ClusterIndex = i + 1,
                    X = scene.CenterX + x, Y = y - 0.12f, Z = scene.CenterZ + z,
                    YawDeg = i * 49f, DimensionsX = rock ? 1.8f : 2.6f, DimensionsY = rock ? 0.87f : 0.49f, DimensionsZ = rock ? 1.4f : 0.69f,
                    EmbedDepthM = 0.12f, GroundSkirtBlendM = 0.11f, MossMask = moss, PivotRule = RegionPropPivotRule.NaturalEmbed,
                });
                // Small brackets share the support geometry of this broken-off piece of deadwood.
                // They are absent from bare soil and never need a separate terrain scatter pass.
                if (rock || moisture < 0.47f) continue;
                float logX = scene.CenterX + x + (i % 2 == 0 ? -0.6f : 0.6f);
                float logZ = scene.CenterZ + z + 0.8f;
                float ground = scene.RenderedHeightAt(logX, logZ);
                var start = new Vector3(logX - 0.51f, ground + 0.02f, -logZ);
                var end = new Vector3(logX + 0.51f, ground + 0.105f, -logZ);
                debris.Tube(start, end, 0.10f, 0.065f, new Color32(91, 64, 42, 255), 7);
                for (int j = 0; j < 3; j++)
                {
                    Vector3 p = Vector3.Lerp(start, end, 0.34f + 0.19f * j) + new Vector3(0, 0.054f + 0.016f * j, -0.069f);
                    debris.Blob(p, new Vector3(0.089f, 0.023f, 0.065f), new Color32(169, 129, 85, 255), rng, 7, 3);
                }
            }
            AddLeafLitter(debris, scene, layout, rng);
            AddMesh(parent, "Fallen twigs and bracket fungi", debris, plants, ShadowCastingMode.On);
            return RegionPropView.AddTo(parent, slice, scene.RenderedHeightAt, true).RealModelCount;
        }

        private static void AddLeafLitter(RegionWoodlandGeometry mesh, RegionLookScene scene, RegionWoodlandLayout layout, System.Random rng)
        {
            for (int i = 0; i < 88; i++)
            {
                float south = -19f + 64f * (float)rng.NextDouble();
                float fringe = layout.TrailHalfWidth + 0.10f + 3.1f * (float)rng.NextDouble();
                float east = layout.TrailEast(south) + ((i & 1) == 0 ? fringe : -fringe);
                if (!layout.TryGround(scene, east, south, out _, out _)) continue;
                for (int j = 0; j < 13; j++)
                {
                    float angle = Mathf.PI * 2f * (float)rng.NextDouble();
                    float x = scene.CenterX + east + 0.66f * ((float)rng.NextDouble() - 0.5f);
                    float z = scene.CenterZ + south + 0.88f * ((float)rng.NextDouble() - 0.5f);
                    float size = 0.05f + 0.16f * (float)rng.NextDouble();
                    var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * size;
                    var a = new Vector3(x, scene.RenderedHeightAt(x, z) + 0.032f, -z);
                    var b = a + direction;
                    b.y = scene.RenderedHeightAt(b.x, -b.z) + 0.019f;
                    var colour = Color32.Lerp(new Color32(91, 62, 40, 255), new Color32(168, 126, 72, 255), (float)rng.NextDouble());
                    mesh.Leaf(a, b, Vector3.Cross(direction, Vector3.up) * 0.23f, colour);
                }
            }
        }
    }
}
