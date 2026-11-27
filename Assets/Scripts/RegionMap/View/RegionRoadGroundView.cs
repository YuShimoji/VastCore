using System;
using System.Collections.Generic;
using NVector2 = System.Numerics.Vector2;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap
{
    /// <summary>Terrain dressing added outside the road's collision surface.</summary>
    public static class RegionRoadGroundView
    {
        private static readonly float[] VergeOffsets = { 0.16f, 0.68f, 1.3f, 2.12f, 3.25f, 4.4f, 5.55f };
        private static readonly float[] PadOffsets = { -0.11f, 0.22f, 0.74f, 1.45f, 2.4f, 3.55f };

        public static void AddMargins(Transform parent, RegionRoadPlan plan, Func<float, float, float> height, Material material)
        {
            foreach (RegionRoadCorridor road in new[] { plan.Highway, plan.Arterial })
            {
                const int tileSegments = 96;
                for (int start = 0; start < road.Stations.Count - 1; start += tileSegments)
                {
                    int end = Math.Min(road.Stations.Count - 1, start + tileSegments);
                    var vertices = new List<Vector3>();
                    var indices = new List<int>();
                    var uv = new List<Vector2>();
                    for (int side = -1; side <= 1; side += 2)
                    {
                        int first = vertices.Count;
                        for (int i = start; i <= end; i++)
                        {
                            RegionRoadStation station = road.Stations[i];
                            foreach (float edge in VergeOffsets)
                            {
                                float offset = side * (road.Profile.HalfWidth + edge);
                                NVector2 point = station.AtOffset(offset);
                                float platform = station.SurfaceAt(offset, road.Profile) - 0.09f;
                                float y = Mathf.Lerp(platform, height(point.X, point.Y) + 0.057f,
                                    RegionNoise.Smooth(0.16f, 2.12f, edge));
                                vertices.Add(new Vector3(point.X, y, point.Y));
                                float clearance = road != plan.Site.Road || side != plan.Site.Side ? 1f :
                                    RegionNoise.Smooth(9.35f, 11.85f, Math.Abs(station.Distance - plan.Site.Station.Distance));
                                uv.Add(new Vector2(edge, clearance));
                            }
                        }

                        int columns = VergeOffsets.Length;
                        for (int row = 0; row < end - start; row++)
                        for (int col = 0; col < columns - 1; col++)
                        {
                            int vertex = first + row * columns + col;
                            if (side > 0) AddQuad(indices, vertex, vertex + columns, vertex + 1, vertex + columns + 1);
                            else AddQuad(indices, vertex + 1, vertex + columns + 1, vertex, vertex + columns);
                        }
                    }
                    AddMesh(parent, "Mixed gravel and grass " + road.Id, material, vertices, indices, uv);
                }
            }
            AddPadMargin(parent, plan.Site, height, material);
        }

        private static void AddPadMargin(Transform parent, RegionRoadsideSite site, Func<float, float, float> height, Material material)
        {
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            var uv = new List<Vector2>();
            float near = RegionRoadsideSite.Setback, far = near + RegionRoadsideSite.Depth;
            float half = RegionRoadsideSite.Frontage * 0.5f;
            float drive = RegionRoadsideSite.DriveHalfWidth + 1.7f;
            NVector2 along = site.Station.Forward, outward = site.Outward;

            AddEdge(Point(near, -half), Point(far, -half), -along);
            AddEdge(Point(far, -half), Point(far, half), outward);
            AddEdge(Point(far, half), Point(near, half), along);
            AddEdge(Point(near, half), Point(near, drive), -outward);
            AddEdge(Point(near, -drive), Point(near, -half), -outward);
            AddMesh(parent, "Gravel feather around plot", material, vertices, indices, uv);

            NVector2 Point(float reach, float span) => site.Entrance + outward * reach + along * span;
            void AddEdge(NVector2 from, NVector2 to, NVector2 right)
            {
                int first = vertices.Count;
                int segments = Mathf.CeilToInt(NVector2.Distance(from, to) / 2.2f);
                for (int i = 0; i <= segments; i++)
                {
                    NVector2 location = NVector2.Lerp(from, to, i / (float)segments);
                    foreach (float offset in PadOffsets)
                    {
                        NVector2 point = location + right * offset;
                        float y = Mathf.Lerp(site.PlotElevation + 0.014f, height(point.X, point.Y) + 0.052f,
                            RegionNoise.Smooth(-0.05f, 1.6f, offset));
                        vertices.Add(new Vector3(point.X, y, point.Y));
                        float end = Mathf.Min(i, segments - i);
                        uv.Add(new Vector2(0.78f + 1.45f * offset, Mathf.Lerp(0.65f, 1f, Mathf.Clamp01(end))));
                    }
                }
                int columns = PadOffsets.Length;
                for (int i = 0; i < segments; i++)
                for (int j = 0; j < columns - 1; j++)
                {
                    int v = first + i * columns + j;
                    AddQuad(indices, v, v + columns, v + 1, v + columns + 1);
                }
            }
        }

        public static void AddRidges(Transform parent, RegionRoadPlan plan, Func<float, float, float> height, Material material)
        {
            const float step = 72f;
            const int inner = 32, outer = 70;
            const int width = 2 * outer + 1;
            var vertices = new List<Vector3>(width * width);
            var indices = new List<int>();
            uint seed = (uint)plan.Seed ^ 0x679eu;
            for (int z = -outer; z <= outer; z++)
            for (int x = -outer; x <= outer; x++)
            {
                float wx = x * step, wz = z * step;
                float beyond = Mathf.Max(Math.Abs(wx), Math.Abs(wz)) - RegionRoadPlan.Extent;
                float anchor = height(Mathf.Clamp(wx, -RegionRoadPlan.Extent, RegionRoadPlan.Extent),
                    Mathf.Clamp(wz, -RegionRoadPlan.Extent, RegionRoadPlan.Extent));
                float change = RegionNoise.Fbm(wx / 890f, wz / 970f, 3, seed);
                float farChange = RegionNoise.Fbm(wx / 1270f, wz / 1420f, 3, seed + 81u);
                float front = 1f - RegionNoise.Smooth(0f, 430f, Math.Abs(beyond - 530f - 168f * change));
                float back = 1f - RegionNoise.Smooth(0f, 670f, Math.Abs(beyond - 1500f - 230f * farChange));
                float y = Mathf.Lerp(anchor, 26f, RegionNoise.Smooth(0f, 520f, beyond)) +
                    front * (147f + 248f * change) + back * (289f + 326f * farChange);
                vertices.Add(new Vector3(wx, y - 0.4f, wz));
                if (z == outer || x == outer || (x >= -inner && x < inner && z >= -inner && z < inner)) continue;
                int i = (z + outer) * width + x + outer;
                AddQuad(indices, i, i + width, i + 1, i + width + 1);
            }
            AddMesh(parent, "Two distant ridges", material, vertices, indices);
        }

        private static void AddQuad(List<int> indices, int a, int b, int c, int d)
        {
            indices.Add(a);
            indices.Add(b);
            indices.Add(c);
            indices.Add(c);
            indices.Add(b);
            indices.Add(d);
        }

        private static void AddMesh(Transform parent, string name, Material material, List<Vector3> vertices,
            List<int> indices, List<Vector2> uv = null)
        {
            var mesh = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            mesh.SetVertices(vertices);
            if (uv != null) mesh.SetUVs(0, uv);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
