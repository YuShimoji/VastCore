using System;
using System.Collections.Generic;
using NVector2 = System.Numerics.Vector2;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap
{
    public sealed class RegionRoadMaterials
    {
        public Material Asphalt, Shoulder, Gravel, WhitePaint, CenterPaint, Concrete, Metal, Delineator;
    }

    /// <summary>Builds the road kit from its metre-space section and alignment.</summary>
    public static class RegionRoadView
    {
        private const float PaintLift = 0.018f;
        internal static IReadOnlyList<float> EntranceDistances { get; } = new[]
            { -0.26f, 0f, 1f, 2f, 3.5f, 5f, 6.5f, 8f, 15f, RegionRoadsideSite.Setback + 0.35f };

        public static GameObject Build(Transform parent, RegionRoadCorridor road, RegionRoadMaterials materials,
            RegionRoadsideSite site = null, bool shapeOnly = false, bool blendedVerge = false)
        {
            if (road == null) throw new ArgumentNullException(nameof(road));
            if (materials == null) throw new ArgumentNullException(nameof(materials));
            if (site != null && site.Road != road) throw new ArgumentException("The entrance belongs to a different road.", nameof(site));

            var root = new GameObject(road.Id);
            root.transform.SetParent(parent, false);
            RegionRoadProfile section = road.Profile;
            float half = section.HalfWidth;
            float median = section.Median * 0.5f;
            float outer = section.OuterLaneEdge;
            var main = new RoadMesh();
            var shoulder = new RoadMesh();
            var gravel = new RoadMesh();
            var concrete = new RoadMesh();

            for (int side = -1; side <= 1; side += 2)
            {
                main.FlatBand(road, side * median, side * outer);
                shoulder.FlatBand(road, side * outer, side * half);
                shoulder.Band(road, side * half, 0f, side * (half + 0.16f), -0.09f, 0f, road.Length);
                if (!blendedVerge)
                    gravel.Band(road, side * (half + 0.16f), -0.09f, side * (half + 2.1f), -0.18f, 0f, road.Length);
            }
            if (median > 0f) gravel.FlatBand(road, -median, median, -0.06f);
            if (section.ControlledAccess)
            {
                // Continuous low concrete median, with a stepped Jersey-style cross section.
                float[] offsets = { -0.39f, -0.24f, -0.15f, 0.15f, 0.24f, 0.39f };
                float[] heights = { -0.06f, 0.28f, 0.84f, 0.84f, 0.28f, -0.06f };
                for (int i = 1; i < offsets.Length; i++)
                    concrete.Band(road, offsets[i - 1], heights[i - 1], offsets[i], heights[i], 0f, road.Length);
            }

            main.AddTo(root.transform, "Carriageway", materials.Asphalt, true, false);
            shoulder.AddTo(root.transform, "Paved shoulders", materials.Shoulder, true, false);
            gravel.AddTo(root.transform, "Verges and median bed", materials.Gravel, false, false);
            concrete.AddTo(root.transform, "Median barrier", materials.Concrete, true, true);
            if (!shapeOnly) BuildDetails(root.transform, road, materials, site);
            if (site != null) BuildEntrance(root.transform, site, materials);
            return root;
        }

        private static void BuildDetails(Transform parent, RegionRoadCorridor road, RegionRoadMaterials materials,
            RegionRoadsideSite site)
        {
            RegionRoadProfile section = road.Profile;
            var white = new RoadMesh();
            var yellow = new RoadMesh();
            var rail = new RoadMesh();
            var reflectors = new RoadMesh();
            for (int side = -1; side <= 1; side += 2)
            {
                float outsideLine = side * section.OuterLaneEdge;
                if (site != null && site.Side == side)
                {
                    white.Paint(road, outsideLine, 0.15f, 0f, site.Station.Distance - RegionRoadsideSite.MouthHalfWidth);
                    white.Paint(road, outsideLine, 0.15f, site.Station.Distance + RegionRoadsideSite.MouthHalfWidth, road.Length);
                }
                else white.Paint(road, outsideLine, 0.15f, 0f, road.Length);

                if (section.Median > 0f)
                    white.Paint(road, side * section.InnerLaneEdge, 0.15f, 0f, road.Length);
                for (int lane = 1; lane < section.LanesPerDirection; lane++)
                {
                    float offset = side * (section.InnerLaneEdge + lane * section.LaneWidth);
                    float period = section.ControlledAccess ? 20f : 12f;
                    for (float start = 3f; start < road.Length; start += period)
                        white.Paint(road, offset, 0.14f, start, Math.Min(start + period * 0.43f, road.Length));
                }

                if (section.ControlledAccess)
                    AddGuardrail(rail, road, side);
                for (float station = 24f; station < road.Length - 6f; station += 44f)
                {
                    if (site != null && site.Side == side &&
                        Math.Abs(station - site.Station.Distance) < RegionRoadsideSite.MouthHalfWidth + 6f) continue;
                    RegionRoadStation at = road.At(station);
                    float distance = side * (section.HalfWidth + (section.ControlledAccess ? 0.95f : 1.22f));
                    float y = at.SurfaceAt(side * section.HalfWidth, section);
                    reflectors.Post(at, distance, y - 0.22f, 0.075f, 0.94f);
                    yellow.Post(at, distance, y + 0.62f, 0.084f, 0.13f);
                }
            }
            if (section.Median == 0f) yellow.Paint(road, 0f, 0.14f, 0f, road.Length);
            white.AddTo(parent, "White edge and lane markings", materials.WhitePaint, false, false);
            yellow.AddTo(parent, "Centerline and reflectors", materials.CenterPaint, false, false);
            rail.AddTo(parent, "Galvanized guardrails and posts", materials.Metal, false, true);
            reflectors.AddTo(parent, "Delineator posts", materials.Delineator, false, true);
        }

        private static void AddGuardrail(RoadMesh mesh, RegionRoadCorridor road, int side)
        {
            float edge = side * (road.Profile.HalfWidth + 0.53f);
            float[] fold = { 0.03f, -0.085f, -0.025f, -0.085f, 0.03f };
            float[] levels = { 0.48f, 0.54f, 0.63f, 0.73f, 0.78f };
            for (int i = 1; i < fold.Length; i++)
                mesh.Band(road, edge + side * fold[i - 1], levels[i - 1], edge + side * fold[i], levels[i], 0f, road.Length);
            for (float distance = 2.5f; distance < road.Length; distance += 5f)
            {
                RegionRoadStation station = road.At(distance);
                mesh.Post(station, edge + side * 0.09f, station.SurfaceAt(edge, road.Profile) - 0.24f, 0.13f, 1.04f);
            }
        }

        private static void BuildEntrance(Transform parent, RegionRoadsideSite site, RegionRoadMaterials materials)
        {
            var entrance = new GameObject(site.Id);
            entrance.transform.SetParent(parent, false);
            var strip = new RoadMesh();
            Vector3 left = default, right = default;
            for (int i = 0; i < EntranceDistances.Count; i++)
            {
                float reach = EntranceDistances[i];
                float width = EntranceHalfWidth(reach);
                Vector3 newLeft = EntranceEdge(site, reach, -width);
                Vector3 newRight = EntranceEdge(site, reach, width);
                if (i > 0) strip.QuadUp(left, newLeft, right, newRight);
                left = newLeft;
                right = newRight;
            }
            strip.AddTo(entrance.transform, "Flared arterial entrance", materials.Shoulder, true, false);

            var plot = new RoadMesh();
            float near = RegionRoadsideSite.Setback, far = near + RegionRoadsideSite.Depth;
            float frontage = RegionRoadsideSite.Frontage * 0.5f;
            plot.QuadUp(EntranceEdge(site, near, -frontage), EntranceEdge(site, far, -frontage),
                EntranceEdge(site, near, frontage), EntranceEdge(site, far, frontage));
            plot.AddTo(entrance.transform, "Vacant graded plot", materials.Gravel, true, false);

            AddAnchor(entrance.transform, "Entrance", site.Entrance, site.EntranceElevation, site.Outward);
            AddAnchor(entrance.transform, "Plot center", site.PlotCenter, site.PlotElevation, -site.Outward);
        }

        internal static float EntranceHalfWidth(float reach) =>
            RegionNoise.Lerp(RegionRoadsideSite.MouthHalfWidth, RegionRoadsideSite.DriveHalfWidth,
                RegionNoise.Smooth(0f, 8f, reach));

        internal static Vector3 EntranceEdge(RegionRoadsideSite site, float reach, float along)
        {
            NVector2 point = site.Entrance + site.Outward * reach + site.Station.Forward * along;
            RegionRoadStation frame = site.Road.At(site.Station.Distance + along);
            NVector2 roadEdge = frame.AtOffset(site.Side * (site.Edge + reach));
            float join = 1f - RegionNoise.Smooth(0f, 12f, reach);
            point = NVector2.Lerp(point, roadEdge, join);
            float y = site.ApproachElevation(reach) + join * (frame.Elevation - site.Station.Elevation);
            return new Vector3(point.X, y + (reach < RegionRoadsideSite.Setback ? 0.023f : 0f), point.Y);
        }

        private static void AddAnchor(Transform parent, string name, NVector2 xy, float y, NVector2 facing)
        {
            var anchor = new GameObject(name);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = new Vector3(xy.X, y, xy.Y);
            anchor.transform.localRotation = Quaternion.LookRotation(new Vector3(facing.X, 0f, facing.Y));
        }

        private sealed class RoadMesh
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Vector2> uv = new List<Vector2>();
            private readonly List<int> indices = new List<int>();

            public void FlatBand(RegionRoadCorridor road, float a, float b, float lift = 0f) =>
                Band(road, a, lift, b, lift, 0f, road.Length);

            public void Paint(RegionRoadCorridor road, float offset, float width, float start, float end) =>
                Band(road, offset - width * 0.5f, PaintLift, offset + width * 0.5f, PaintLift, start, end);

            public void Band(RegionRoadCorridor road, float a, float aLift, float b, float bLift, float start, float end)
            {
                if (end <= start || a == b && aLift == bLift) return;
                if (b < a) (a, aLift, b, bLift) = (b, bLift, a, aLift);
                // Include every road station under the strip so paint cannot cut across a road bend.
                int lo = 0, hi = road.Stations.Count;
                while (lo < hi)
                {
                    int mid = (lo + hi) / 2;
                    if (road.Stations[mid].Distance <= start) lo = mid + 1;
                    else hi = mid;
                }
                int previous = Section(road.At(start), road.Profile, a, aLift, b, bLift);
                for (int i = lo; i < road.Stations.Count && road.Stations[i].Distance < end; i++)
                {
                    int next = Section(road.Stations[i], road.Profile, a, aLift, b, bLift);
                    Join(previous, next);
                    previous = next;
                }
                Join(previous, Section(road.At(end), road.Profile, a, aLift, b, bLift));
            }

            private int Section(RegionRoadStation at, RegionRoadProfile profile, float a, float yA, float b, float yB)
            {
                int start = vertices.Count;
                Add(Point(at, a, at.SurfaceAt(a, profile) + yA));
                Add(Point(at, b, at.SurfaceAt(b, profile) + yB));
                return start;
            }

            private void Join(int left, int next)
            {
                indices.Add(left);
                indices.Add(next);
                indices.Add(left + 1);
                indices.Add(left + 1);
                indices.Add(next);
                indices.Add(next + 1);
            }

            public void QuadUp(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                if (Vector3.Cross(b - a, c - a).y < 0f) (a, b, c, d) = (c, d, a, b);
                Quad(a, b, c, d);
            }

            private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int start = vertices.Count;
                Add(a);
                Add(c);
                Add(b);
                Add(d);
                Join(start, start + 2);
            }

            public void Post(RegionRoadStation at, float offset, float y, float width, float height)
            {
                NVector2 center = at.AtOffset(offset);
                float r = width * 0.5f;
                Vector3 a = new Vector3(center.X - r, y, center.Y - r);
                Vector3 b = new Vector3(center.X - r, y, center.Y + r);
                Vector3 c = new Vector3(center.X + r, y, center.Y - r);
                Vector3 d = new Vector3(center.X + r, y, center.Y + r);
                Vector3 h = Vector3.up * height;
                Quad(b, b + h, a, a + h);
                Quad(d, d + h, b, b + h);
                Quad(c, c + h, d, d + h);
                Quad(a, a + h, c, c + h);
                Quad(a + h, b + h, c + h, d + h);
            }

            private void Add(Vector3 point)
            {
                vertices.Add(point);
                uv.Add(new Vector2(point.x, point.z) * 0.48f);
            }

            private static Vector3 Point(RegionRoadStation station, float offset, float y)
            {
                NVector2 point = station.AtOffset(offset);
                return new Vector3(point.X, y, point.Y);
            }

            public void AddTo(Transform parent, string name, Material material, bool collision, bool castsShadows)
            {
                if (indices.Count == 0) return;
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32, hideFlags = HideFlags.DontSave };
                mesh.SetVertices(vertices);
                mesh.SetUVs(0, uv);
                mesh.SetTriangles(indices, 0);
                mesh.RecalculateNormals();
                if (material != null && material.IsKeywordEnabled("_NORMALMAP")) mesh.RecalculateTangents();
                var obj = new GameObject(name);
                obj.transform.SetParent(parent, false);
                obj.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = obj.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = castsShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
                if (collision) obj.AddComponent<MeshCollider>().sharedMesh = mesh;
            }
        }
    }
}
