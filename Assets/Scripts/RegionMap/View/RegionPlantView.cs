using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap
{
    internal static class RegionPlantView
    {
        // Atlas columns are pairs of projections of the same branching plant; rows are RegionPlantKind.
        private static readonly float[] Aspect = { 1.35f, 1.65f, 1.12f, 1.12f };

        public static int[] AddTo(Transform root, RegionLookScene scene, RegionAlcoveView alcove)
        {
            List<RegionPlant> plants = RegionPlantField.Build(scene);
            RegionBiomeSurfaceProfile profile = scene.BiomeSurface ?? RegionBiomeSurface.ProfileFor(scene.PrimaryBiome);
            var tint = new Color(profile.PlantTintR, profile.PlantTintG, profile.PlantTintB, 1f);
            string label = profile.Biome + " plants";
            if (alcove == null) return AddPlants(root, plants, "Vegetation", tint, label);
            var displaced = plants.FindAll(p => alcove.LostSupport(p.X, p.Y, p.Z, 0.10f));
            if (displaced.Count == 0) return AddPlants(root, plants, "Vegetation", tint, label);
            var lost = new HashSet<RegionPlant>(displaced);
            plants.RemoveAll(lost.Contains);
            var holder = new GameObject("Vegetation");
            holder.transform.SetParent(root, false);
            int[] counts = AddPlants(holder.transform, plants, "Rooted", tint, label);
            AddPlants(holder.transform, displaced, "Before carving", tint, label);
            alcove.UnsupportedPlants = holder.transform.Find("Before carving").gameObject;
            return counts;
        }

        internal static int[] AddPlants(Transform root, List<RegionPlant> plants, string name)
        {
            return AddPlants(root, plants, name, Color.white, "Desert plants");
        }

        internal static int[] AddPlants(Transform root, List<RegionPlant> plants, string name, Color tint, string materialName)
            => AddPlants(root, plants, name, tint, materialName, null, null);

        [Serializable]
        private sealed class MeadowAtlas
        {
            public Vector4[] frames;
        }

        internal static void AddMeadowTufts(Transform root, List<RegionPlant> plants, Func<float, float, float> ground,
            string name, Color tint)
        {
            var layout = Resources.Load<TextAsset>("RegionMap3D/MeadowTussocksLayout");
            var meadow = layout ? JsonUtility.FromJson<MeadowAtlas>(layout.text) : null;
            if (meadow?.frames == null || meadow.frames.Length != 12)
                throw new InvalidOperationException("The meadow atlas requires twelve cropped projections.");
            AddPlants(root, plants, name, tint, "Meadow tufts", meadow, ground);
        }

        private static int[] AddPlants(Transform root, List<RegionPlant> plants, string name, Color tint,
            string materialName, MeadowAtlas meadow, Func<float, float, float> ground)
        {
            var counts = new int[4];
            if (plants.Count == 0) return counts;
            Shader shader = Resources.Load<Shader>("RegionMap3D/RegionPlants");
            Texture2D atlas = Resources.Load<Texture2D>(meadow == null ? "RegionMap3D/DesertPlants" : "RegionMap3D/MeadowTussocks");
            if (!shader || !atlas) throw new InvalidOperationException("植生用シェーダーまたは株のアトラスが見つかりません。");
            var material = new Material(shader) { name = materialName, hideFlags = HideFlags.DontSave };
            material.SetTexture("_BaseMap", atlas);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            if (meadow != null) material.SetFloat("_MeadowTurf", 1f);
            material.EnableKeyword("_ALPHATEST_ON"); // Also cuts holes in the URP shadow and depth passes.
            var parent = new GameObject(name);
            parent.transform.SetParent(root, false);
            var tiles = new Dictionary<Vector2Int, List<RegionPlant>>();
            float tileSize = meadow == null ? 256f : 128f;
            foreach (RegionPlant plant in plants)
            {
                var key = new Vector2Int(Mathf.FloorToInt(plant.X / tileSize), Mathf.FloorToInt(plant.Z / tileSize));
                if (!tiles.TryGetValue(key, out List<RegionPlant> tile))
                    tiles.Add(key, tile = new List<RegionPlant>());
                tile.Add(plant);
                counts[(int)plant.Kind]++;
            }
            foreach (var pair in tiles)
            {
                var go = new GameObject("Plants " + pair.Key.x + "," + pair.Key.y);
                go.transform.SetParent(parent.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = TileMesh(pair.Value, meadow, ground);
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                if (meadow != null) renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            return counts;
        }

        private static Mesh TileMesh(List<RegionPlant> plants, MeadowAtlas meadow, Func<float, float, float> ground)
        {
            int faces = meadow == null ? 2 : 3;
            var positions = new Vector3[4 * faces * plants.Count];
            var uv = new Vector2[positions.Length];
            var colors = new Color32[positions.Length];
            var normals = new Vector3[positions.Length];
            var indices = new int[6 * faces * plants.Count];
            int v = 0, t = 0;
            foreach (RegionPlant plant in plants)
            {
                int kind = (int)plant.Kind;
                float width = meadow == null ? plant.Height * Aspect[kind]
                    : plant.Variant < 2 ? Mathf.Max(1.19f, plant.Height * 3.6f) : 0.44f + plant.Height * 1.27f;
                // Bury the atlas' transparent bottom margin to keep the stem on the triangle.
                float rootDepth = meadow == null ? 0.008f * plant.Height : 0.041f;
                var foot = new Vector3(plant.X, plant.Y - rootDepth, -plant.Z);
                var up = Vector3.up * plant.Height;
                var light = new Color32((byte)(255 * plant.Sun), (byte)(255 * plant.Sky), 255, 255);
                for (int face = 0; face < faces; face++)
                {
                    float angle = plant.Yaw + face * Mathf.PI * 0.5f;
                    var half = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (width * 0.5f);
                    if (meadow == null)
                    {
                        positions[v] = foot - half;
                        positions[v + 1] = foot + half;
                        positions[v + 2] = foot + half + up;
                        positions[v + 3] = foot - half + up;
                        float u = (2 * plant.Variant + face) / 8f, bottom = (3 - kind) / 4f;
                        uv[v] = new Vector2(u, bottom);
                        uv[v + 1] = new Vector2(u + 0.125f, bottom);
                        uv[v + 2] = new Vector2(u + 0.125f, bottom + 0.25f);
                        uv[v + 3] = new Vector2(u, bottom + 0.25f);
                    }
                    else
                    {
                        var frame = meadow.frames[plant.Variant * 3 + face];
                        Vector3 left = foot + half * (2f * frame.x - 1f);
                        Vector3 right = foot + half * (2f * frame.z - 1f);
                        var perpendicular = new Vector3(-half.z, 0f, half.x);
                        if (face < 2)
                        {
                            Vector3 tip = up + perpendicular * (0.2f + 0.07f * plant.Variant);
                            positions[v] = left;
                            positions[v + 1] = right;
                            positions[v + 2] = right + tip;
                            positions[v + 3] = left + tip;
                        }
                        else
                        {
                            // Let the top projection follow uneven terrain and lift it at the leaf tips.
                            Vector3 near = perpendicular * (2f * frame.y - 1f);
                            Vector3 far = perpendicular * (2f * frame.w - 1f);
                            positions[v] = Canopy(left + near, plant.Height * 0.12f);
                            positions[v + 1] = Canopy(right + near, plant.Height * 0.24f);
                            positions[v + 2] = Canopy(right + far, plant.Height * 0.35f);
                            positions[v + 3] = Canopy(left + far, plant.Height * 0.15f);
                        }
                        float row = 3 - plant.Variant;
                        uv[v] = new Vector2(face + frame.x, row + frame.y) * 0.25f;
                        uv[v + 1] = new Vector2(face + frame.z, row + frame.y) * 0.25f;
                        uv[v + 2] = new Vector2(face + frame.z, row + frame.w) * 0.25f;
                        uv[v + 3] = new Vector2(face + frame.x, row + frame.w) * 0.25f;
                    }
                    for (int j = 0; j < 4; j++)
                    {
                        normals[v + j] = Vector3.up;
                        colors[v + j] = light;
                    }
                    indices[t++] = v; indices[t++] = v + 2; indices[t++] = v + 1;
                    indices[t++] = v; indices[t++] = v + 3; indices[t++] = v + 2;
                    v += 4;
                }
            }
            var mesh = new Mesh { name = meadow == null ? "Desert plant tile" : "Meadow tuft tile", hideFlags = HideFlags.DontSave };
            if (positions.Length > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = positions;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.colors32 = colors;
            mesh.triangles = indices;
            mesh.UploadMeshData(true);
            return mesh;

            Vector3 Canopy(Vector3 p, float lift) => new Vector3(p.x, ground(p.x, p.z) + lift, p.z);
        }
    }
}
