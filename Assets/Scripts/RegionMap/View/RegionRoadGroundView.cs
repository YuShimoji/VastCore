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
        private static readonly float[] VergeOffsets = { 0f, 0.16f, 0.68f, 1.3f, 2.12f, 3.25f, 4.4f, 5.55f };
        private static readonly float[] PadOffsets = { -0.11f, 0.22f, 0.74f, 1.45f, 2.4f, 3.55f };
        private static readonly float[] ApproachOffsets = { 0.025f, 0.33f, 0.75f, 1.18f, 1.8f, 2.45f };

        public static void AddMargins(Transform parent, RegionRoadPlan plan, Func<float, float, float> height, Material material,
            bool refined = false, bool closeDetail = false, bool variedGravel = false, bool wornEdge = false)
        {
            var detail = closeDetail ? new RegionRoadVergeDetail(plan, height, variedGravel) : null;
            int firstColumn = wornEdge ? 0 : 1;
            int columns = VergeOffsets.Length - firstColumn;
            foreach (RegionRoadCorridor road in new[] { plan.Highway, plan.Arterial })
            {
                const int tileSegments = 96;
                for (int start = 0; start < road.Stations.Count - 1; start += tileSegments)
                {
                    int end = Math.Min(road.Stations.Count - 1, start + tileSegments);
                    var vertices = new List<Vector3>();
                    var indices = new List<int>();
                    var uv = new List<Vector4>();
                    for (int side = -1; side <= 1; side += 2)
                    {
                        int first = vertices.Count;
                        for (int i = start; i <= end; i++)
                        {
                            RegionRoadStation station = road.Stations[i];
                            float clearance = road != plan.Site.Road || side != plan.Site.Side ? 1f :
                                RegionNoise.Smooth(9.35f, 11.85f, Math.Abs(station.Distance - plan.Site.Station.Distance));
                            for (int column = firstColumn; column < VergeOffsets.Length; column++)
                            {
                                float edge = VergeOffsets[column];
                                float offset = side * (road.Profile.HalfWidth + edge);
                                NVector2 point = station.AtOffset(offset);
                                // The wear starts just above the top of the bevel and meets its existing outer vertex.
                                float platform = station.SurfaceAt(offset, road.Profile) + (column == 0 ? 0.008f : -0.09f);
                                float y = Mathf.Lerp(platform, height(point.X, point.Y) + 0.057f,
                                    RegionNoise.Smooth(0.16f, 2.12f, edge));
                                vertices.Add(new Vector3(point.X, y, point.Y));
                                float growth = refined ? RegionRoadMargin.VergeGrowth(point, plan.Seed) : 0f;
                                uv.Add(new Vector4(edge, clearance, growth, 1f));
                            }
                        }

                        for (int row = 0; row < end - start; row++)
                        for (int col = 0; col < columns - 1; col++)
                        {
                            int vertex = first + row * columns + col;
                            if (side > 0) AddQuad(indices, vertex, vertex + columns, vertex + 1, vertex + columns + 1);
                            else AddQuad(indices, vertex + 1, vertex + columns + 1, vertex, vertex + columns);
                        }
                    }
                    AddMesh(parent, "Mixed gravel and grass " + road.Id, material, vertices, indices, uv);
                    detail?.Scatter(vertices, indices, uv, road == plan.Site.Road);
                }
            }
            AddPadMargin(parent, plan, height, material, refined, detail);
            if (refined) AddApproachMargins(parent, plan, height, material, detail);
            if (wornEdge) AddApproachLip(parent, plan.Site, material);
            detail?.AddTo(parent);
        }

        private static void AddApproachMargins(Transform parent, RegionRoadPlan plan, Func<float, float, float> height,
            Material material, RegionRoadVergeDetail detail)
        {
            const int segments = 28;
            RegionRoadsideSite site = plan.Site;
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            var uv = new List<Vector4>();
            for (int side = -1; side <= 1; side += 2)
            {
                int first = vertices.Count;
                for (int i = 0; i <= segments; i++)
                {
                    float reach = Mathf.Lerp(1.3f, RegionRoadsideSite.Setback, i / (float)segments);
                    float width = RegionRoadView.EntranceHalfWidth(reach);
                    Vector3 pavedEdge = RegionRoadView.EntranceEdge(site, reach, side * width);
                    float fade = RegionNoise.Smooth(1.3f, 4.2f, reach) *
                        (1f - RegionNoise.Smooth(RegionRoadsideSite.Setback - 1.3f, RegionRoadsideSite.Setback, reach));
                    foreach (float offset in ApproachOffsets)
                    {
                        Vector3 position = RegionRoadView.EntranceEdge(site, reach, side * (width + offset));
                        position.y = Mathf.Lerp(pavedEdge.y - 0.027f, height(position.x, position.z) + 0.05f,
                            RegionNoise.Smooth(0f, 1.5f, offset));
                        vertices.Add(position);
                        float growth = RegionRoadMargin.VergeGrowth(new NVector2(position.x, position.z), plan.Seed);
                        uv.Add(new Vector4(0.44f + 2.1f * offset, fade, growth, 0f));
                    }
                }
                int columns = ApproachOffsets.Length;
                for (int row = 0; row < segments; row++)
                for (int col = 0; col < columns - 1; col++)
                {
                    int v = first + row * columns + col;
                    if (side < 0) AddQuad(indices, v, v + columns, v + 1, v + columns + 1);
                    else AddQuad(indices, v + 1, v + columns + 1, v, v + columns);
                }
            }
            AddMesh(parent, "Gravel fringe alongside the approach", material, vertices, indices, uv);
            detail?.Scatter(vertices, indices, uv, true, approach: true);
        }

        private static void AddApproachLip(Transform parent, RegionRoadsideSite site, Material material)
        {
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            var uv = new List<Vector4>();
            const int columns = 3;
            for (int side = -1; side <= 1; side += 2)
            {
                int first = vertices.Count;
                foreach (float reach in RegionRoadView.EntranceDistances)
                {
                    float span = side * RegionRoadView.EntranceHalfWidth(reach);
                    Vector3 edge = RegionRoadView.EntranceEdge(site, reach, span);
                    Vector3 across = (edge - RegionRoadView.EntranceEdge(site, reach, -span)).normalized;
                    float fade = RegionNoise.Smooth(1f, 3.5f, reach) *
                        (1f - RegionNoise.Smooth(23f, RegionRoadsideSite.Setback, reach));
                    AddPoint(-0.09f, 0.009f);
                    AddPoint(0f, 0.009f);
                    AddPoint(0.14f, -0.014f);

                    void AddPoint(float offset, float lift)
                    {
                        vertices.Add(edge + across * offset + Vector3.up * lift);
                        uv.Add(new Vector4(offset, fade, 0.5f, 2f));
                    }
                }
                for (int row = 0; row < RegionRoadView.EntranceDistances.Count - 1; row++)
                for (int col = 0; col < columns - 1; col++)
                {
                    int v = first + row * columns + col;
                    if (side < 0) AddQuad(indices, v, v + columns, v + 1, v + columns + 1);
                    else AddQuad(indices, v + 1, v + columns + 1, v, v + columns);
                }
            }
            AddMesh(parent, "Gravel overlap at the paved entrance edge", material, vertices, indices, uv);
        }

        private static void AddPadMargin(Transform parent, RegionRoadPlan plan, Func<float, float, float> height,
            Material material, bool refined, RegionRoadVergeDetail detail)
        {
            RegionRoadsideSite site = plan.Site;
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            var uv = new List<Vector4>();
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
            detail?.Scatter(vertices, indices, uv, true);

            NVector2 Point(float reach, float span) => site.Entrance + outward * reach + along * span;
            void AddEdge(NVector2 from, NVector2 to, NVector2 right)
            {
                int first = vertices.Count;
                int segments = Mathf.CeilToInt(NVector2.Distance(from, to) / 2.2f);
                for (int i = 0; i <= segments; i++)
                {
                    NVector2 location = NVector2.Lerp(from, to, i / (float)segments);
                    foreach (float sample in PadOffsets)
                    {
                        float offset = refined && sample < 0f ? -0.72f : sample;
                        NVector2 point = location + right * offset;
                        float y = Mathf.Lerp(site.PlotElevation + 0.014f, height(point.X, point.Y) + 0.052f,
                            RegionNoise.Smooth(-0.05f, 1.6f, offset));
                        vertices.Add(new Vector3(point.X, y, point.Y));
                        float end = Mathf.Min(i, segments - i);
                        float growth = refined ? RegionRoadMargin.VergeGrowth(point, plan.Seed) : 0f;
                        uv.Add(new Vector4(0.78f + 1.45f * offset, Mathf.Lerp(0.65f, 1f, Mathf.Clamp01(end)), growth, 0f));
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
            List<int> indices, List<Vector4> uv = null)
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
