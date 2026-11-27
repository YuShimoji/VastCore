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
        {
            var counts = new int[4];
            if (plants.Count == 0) return counts;
            Shader shader = Resources.Load<Shader>("RegionMap3D/RegionPlants");
            Texture2D atlas = Resources.Load<Texture2D>("RegionMap3D/DesertPlants");
            if (!shader || !atlas) throw new InvalidOperationException("植生用シェーダーまたは株のアトラスが見つかりません。");
            var material = new Material(shader) { name = materialName, hideFlags = HideFlags.DontSave };
            material.SetTexture("_BaseMap", atlas);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            material.EnableKeyword("_ALPHATEST_ON"); // Also cuts holes in the URP shadow and depth passes.
            var parent = new GameObject(name);
            parent.transform.SetParent(root, false);
            var tiles = new Dictionary<Vector2Int, List<RegionPlant>>();
            foreach (RegionPlant plant in plants)
            {
                var key = new Vector2Int(Mathf.FloorToInt(plant.X / 256f), Mathf.FloorToInt(plant.Z / 256f));
                if (!tiles.TryGetValue(key, out List<RegionPlant> tile))
                    tiles.Add(key, tile = new List<RegionPlant>());
                tile.Add(plant);
                counts[(int)plant.Kind]++;
            }
            foreach (var pair in tiles)
            {
                var go = new GameObject("Plants " + pair.Key.x + "," + pair.Key.y);
                go.transform.SetParent(parent.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = TileMesh(pair.Value);
                go.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
            return counts;
        }

        private static Mesh TileMesh(List<RegionPlant> plants)
        {
            var positions = new Vector3[8 * plants.Count];
            var uv = new Vector2[positions.Length];
            var colors = new Color32[positions.Length];
            var normals = new Vector3[positions.Length];
            var indices = new int[12 * plants.Count];
            int v = 0, t = 0;
            foreach (RegionPlant plant in plants)
            {
                int kind = (int)plant.Kind;
                float width = plant.Height * Aspect[kind];
                // Bury the atlas' transparent bottom margin to keep the stem on the triangle.
                var foot = new Vector3(plant.X, plant.Y - 0.008f * plant.Height, -plant.Z);
                var up = Vector3.up * plant.Height;
                for (int face = 0; face < 2; face++)
                {
                    float angle = plant.Yaw + face * Mathf.PI * 0.5f;
                    var half = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (width * 0.5f);
                    positions[v] = foot - half;
                    positions[v + 1] = foot + half;
                    positions[v + 2] = foot + half + up;
                    positions[v + 3] = foot - half + up;
                    float u = (2 * plant.Variant + face) / 8f, bottom = (3 - kind) / 4f;
                    uv[v] = new Vector2(u, bottom);
                    uv[v + 1] = new Vector2(u + 0.125f, bottom);
                    uv[v + 2] = new Vector2(u + 0.125f, bottom + 0.25f);
                    uv[v + 3] = new Vector2(u, bottom + 0.25f);
                    var light = new Color32((byte)(255 * plant.Sun), (byte)(255 * plant.Sky), 255, 255);
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
            var mesh = new Mesh { name = "Desert plant tile", hideFlags = HideFlags.DontSave };
            if (positions.Length > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = positions;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.colors32 = colors;
            mesh.triangles = indices;
            mesh.UploadMeshData(true);
            return mesh;
        }
    }
}
