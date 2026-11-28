using System;
using System.Collections.Generic;
using NVector2 = System.Numerics.Vector2;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap
{
    /// <summary>Small grass clumps and loose stones supported by the rendered shoulder.</summary>
    internal sealed class RegionRoadVergeDetail
    {
        private const float DetailReach = 420f;
        private const float TileSize = 20f;
        private readonly RegionRoadPlan plan;
        private readonly Func<float, float, float> ground;
        private readonly uint seed;
        private readonly NVector2 pocketOffset;
        private readonly Dictionary<Vector2Int, RegionWoodlandGeometry> tiles = new Dictionary<Vector2Int, RegionWoodlandGeometry>();
        private readonly RegionWoodlandGeometry[] grass = new RegionWoodlandGeometry[5];
        private readonly RegionWoodlandGeometry[] stones = new RegionWoodlandGeometry[7];

        internal RegionRoadVergeDetail(RegionRoadPlan plan, Func<float, float, float> height)
        {
            this.plan = plan;
            ground = height;
            seed = unchecked((uint)plan.Seed) ^ 0x0a94d32bu;
            pocketOffset = RegionRoadMargin.VergePocketOffset(plan.Seed);
            for (int i = 0; i < grass.Length; i++) grass[i] = GrassClump(i);
            for (int i = 0; i < stones.Length; i++) stones[i] = Stone(i);
        }

        internal void Scatter(List<Vector3> vertices, List<int> indices, List<Vector3> uv, bool entranceSide)
        {
            float siteNorth = plan.Site.Entrance.Y;
            for (int triangle = 0; triangle < indices.Count; triangle += 3)
            {
                int ia = indices[triangle], ib = indices[triangle + 1], ic = indices[triangle + 2];
                Vector3 a = vertices[ia], b = vertices[ib], c = vertices[ic];
                Vector3 center = (a + b + c) / 3f;
                float distance = Mathf.Abs(center.z - siteNorth);
                if (distance >= DetailReach || Mathf.Min(uv[ia].x, Mathf.Min(uv[ib].x, uv[ic].x)) >= 4.65f) continue;

                float area = Mathf.Abs((b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x)) * 0.5f;
                float entrance = entranceSide ? 1f - RegionNoise.Smooth(43f, 78f, distance) : 0f;
                float fade = 1f - RegionNoise.Smooth(370f, DetailReach, distance);
                float density = (6f + 18f * entrance) * fade;
                uint salt = RegionNoise.Hash(Mathf.RoundToInt(center.x * 40f), Mathf.RoundToInt(center.z * 40f), seed);
                int count = Mathf.FloorToInt(area * density + RegionNoise.Hash01(0, 9, salt));
                for (int i = 0; i < count; i++)
                {
                    float u = RegionNoise.Hash01(i, 1, salt), v = RegionNoise.Hash01(i, 2, salt);
                    if (u + v > 1f) { u = 1f - u; v = 1f - v; }
                    Vector3 attributes = uv[ia] + u * (uv[ib] - uv[ia]) + v * (uv[ic] - uv[ia]);
                    float edge = attributes.x;
                    if (edge < 0.24f || attributes.y < 0.82f || edge > 4.55f) continue;
                    Vector3 point = a + u * (b - a) + v * (c - a);
                    var xz = new NVector2(point.x, point.z);

                    float pocket = RegionRoadMargin.VergePocket(xz, pocketOffset, attributes.z);
                    float fringe = RegionNoise.Smooth(0.50f, 1.38f, edge) *
                        (1f - RegionNoise.Smooth(3.08f, 4.55f, edge));
                    float cover = fringe * pocket;
                    float roll = RegionNoise.Hash01(i, 3, salt);
                    bool grows = roll < cover * 0.90f;
                    float gravel = (1f - RegionNoise.Smooth(1.44f, 3.72f, edge)) * (0.78f - 0.48f * cover);
                    if (!grows && RegionNoise.Hash01(i, 4, salt) > gravel) continue;
                    if (RegionRoadMargin.InSite(plan.Site, xz, grows ? 0.38f : 0.12f)) continue;

                    float size = RegionNoise.Hash01(i, 5, salt);
                    float scale = grows ? Mathf.Lerp(0.23f, 0.49f, size)
                        : Mathf.Lerp(0.022f, 0.096f, size * size);
                    RegionWoodlandGeometry[] forms = grows ? grass : stones;
                    int form = (int)(RegionNoise.Hash01(i, 6, salt) * forms.Length);
                    point.y = Mathf.Max(point.y, ground(point.x, point.z)) + (grows ? 0f : 0.002f);
                    var key = new Vector2Int(Mathf.FloorToInt(point.x / TileSize), Mathf.FloorToInt(point.z / TileSize));
                    if (!tiles.TryGetValue(key, out RegionWoodlandGeometry tile))
                        tiles.Add(key, tile = new RegionWoodlandGeometry());
                    tile.Append(forms[form], point, scale, 360f * RegionNoise.Hash01(i, 7, salt));
                }
            }
        }

        internal void AddTo(Transform parent)
        {
            if (tiles.Count == 0) return;
            var shader = Resources.Load<Shader>("RegionMap3D/RegionWoodland");
            if (!shader) throw new InvalidOperationException("The roadside detail shader is missing.");
            var material = new Material(shader) { name = "Short roadside grass and stones", hideFlags = HideFlags.DontSave };
            var group = new GameObject("Short grass and loose stones");
            group.transform.SetParent(parent, false);
            foreach (var tile in tiles)
            {
                var node = new GameObject($"{tile.Key.x}, {tile.Key.y}");
                node.transform.SetParent(group.transform, false);
                Mesh mesh = tile.Value.ToMesh(node.name);
                Bounds bounds = mesh.bounds;
                mesh.UploadMeshData(true);
                node.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = node.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                var lod = node.AddComponent<LODGroup>();
                lod.SetLODs(new[] { new LOD(0.22f, new Renderer[] { renderer }) });
                lod.localReferencePoint = bounds.center;
                lod.size = 8f;
            }
            tiles.Clear();
        }

        private static RegionWoodlandGeometry GrassClump(int variant)
        {
            var shape = new RegionWoodlandGeometry();
            uint salt = (uint)(2843 + 717 * variant);
            var green = Color32.Lerp(new Color32(101, 124, 58, 255), new Color32(120, 130, 69, 255), variant / 4f);
            for (int i = 0; i < 11; i++)
            {
                float angle = 2.39996f * i + 0.63f * RegionNoise.Hash01(i, 0, salt);
                Vector3 outward = Direction(angle);
                Vector3 root = outward * (0.07f + 0.39f * RegionNoise.Hash01(i, 1, salt)) - Vector3.up * 0.035f;
                Vector3 tip = root + outward * (0.08f + 0.25f * RegionNoise.Hash01(i, 2, salt)) +
                    Vector3.up * (0.15f + 0.15f * RegionNoise.Hash01(i, 3, salt));
                var leaf = Color32.Lerp(green, new Color32(155, 150, 90, 255), 0.42f * RegionNoise.Hash01(i, 4, salt));
                shape.Leaf(root, tip, Direction(angle + Mathf.PI * 0.5f) * (0.013f + 0.019f * RegionNoise.Hash01(i, 5, salt)), leaf);
            }
            return shape;
        }

        private static RegionWoodlandGeometry Stone(int variant)
        {
            var shape = new RegionWoodlandGeometry();
            uint salt = (uint)(891 + 1579 * variant);
            const int sides = 5;
            var baseRing = new Vector3[sides];
            var bevel = new Vector3[sides];
            float squash = Mathf.Lerp(0.67f, 1f, RegionNoise.Hash01(2, 8, salt));
            var tint = Color32.Lerp(new Color32(117, 109, 96, 255), new Color32(173, 164, 145, 255),
                RegionNoise.Hash01(13, 6, salt));
            for (int i = 0; i < sides; i++)
            {
                float angle = (2f * Mathf.PI) * i / sides + 0.36f * RegionNoise.Hash01(i, 10, salt);
                Vector3 d = Direction(angle);
                d.z *= squash;
                baseRing[i] = d * (0.42f + 0.15f * RegionNoise.Hash01(i, 11, salt)) - Vector3.up * 0.20f;
                bevel[i] = d * (0.31f + 0.13f * RegionNoise.Hash01(i, 12, salt)) +
                    Vector3.up * (0.07f + 0.16f * RegionNoise.Hash01(i, 13, salt));
            }
            Vector3 crown = new Vector3(0.11f - 0.22f * RegionNoise.Hash01(2, 14, salt),
                0.16f + 0.24f * RegionNoise.Hash01(2, 15, salt), 0.08f);
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                var shade = Color32.Lerp(tint, new Color32(72, 67, 57, 255), 0.19f + 0.22f * RegionNoise.Hash01(i, 16, salt));
                Face(baseRing[i], bevel[i], bevel[j], shade);
                Face(baseRing[i], bevel[j], baseRing[j], shade);
                Face(bevel[i], crown, bevel[j], tint);
            }
            return shape;

            void Face(Vector3 a, Vector3 b, Vector3 c, Color32 color)
            {
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                shape.Tri(shape.Add(a, normal, color), shape.Add(b, normal, color), shape.Add(c, normal, color));
            }
        }

        private static Vector3 Direction(float angle) => new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
    }
}
