using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;

namespace Vastcore.RegionMap
{
    public enum RegionRoadPreset { Country, Sweeping, Wide }

    /// <summary>Symmetric road section in metres, excluding earthworks.</summary>
    public readonly struct RegionRoadProfile
    {
        public readonly int LanesPerDirection;
        public readonly float LaneWidth, Median, InnerShoulder, OuterShoulder;
        public readonly bool ControlledAccess;

        public RegionRoadProfile(int lanesPerDirection, float laneWidth, float median,
            float innerShoulder, float outerShoulder, bool controlledAccess)
        {
            if (lanesPerDirection < 1 || lanesPerDirection > 3 || !float.IsFinite(laneWidth) ||
                !float.IsFinite(median) || !float.IsFinite(innerShoulder) || !float.IsFinite(outerShoulder) ||
                laneWidth < 2.5f || laneWidth > 4f || median < 0f || median > 12f || innerShoulder < 0f ||
                innerShoulder > 3f || outerShoulder < 0.5f || outerShoulder > 5f || (controlledAccess && median < 1f))
                throw new ArgumentException("Invalid road cross section.");

            LanesPerDirection = lanesPerDirection;
            LaneWidth = laneWidth;
            Median = median;
            InnerShoulder = innerShoulder;
            OuterShoulder = outerShoulder;
            ControlledAccess = controlledAccess;
        }

        public float InnerLaneEdge => Median * 0.5f + InnerShoulder;
        public float OuterLaneEdge => InnerLaneEdge + LanesPerDirection * LaneWidth;
        public float HalfWidth => OuterLaneEdge + OuterShoulder;
        public float EarthworkWidth => HalfWidth + 20f;
    }

    public readonly struct RegionRoadStation
    {
        public readonly float Distance, Elevation;
        public readonly Vector2 Center, Forward;
        public Vector2 Right => new Vector2(Forward.Y, -Forward.X);

        internal RegionRoadStation(float distance, float elevation, Vector2 center, Vector2 forward)
        {
            Distance = distance;
            Elevation = elevation;
            Center = center;
            Forward = forward;
        }

        public Vector2 AtOffset(float offset) => Center + Right * offset;
        public float SurfaceAt(float offset, RegionRoadProfile profile) =>
            Elevation - 0.022f * Math.Max(0f, Math.Abs(offset) - profile.Median * 0.5f);
    }

    /// <summary>A continuous road section along a smooth, metre-space alignment. Vector2.Y is world Z.</summary>
    public sealed class RegionRoadCorridor
    {
        private const float GridSize = 64f;
        private readonly Dictionary<(int, int), List<int>> cells = new Dictionary<(int, int), List<int>>();
        public string Id { get; }
        public RegionRoadProfile Profile { get; }
        public ReadOnlyCollection<RegionRoadStation> Stations { get; }
        public float Length => Stations[Stations.Count - 1].Distance;

        public RegionRoadCorridor(string id, RegionRoadProfile profile, IReadOnlyList<Vector2> alignment,
            Func<float, float, float> naturalHeight, float rise)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A corridor needs an id.", nameof(id));
            if (profile.LanesPerDirection == 0) throw new ArgumentException("A road profile is required.", nameof(profile));
            if (alignment == null || alignment.Count < 2) throw new ArgumentException("An alignment needs two points.", nameof(alignment));
            if (naturalHeight == null) throw new ArgumentNullException(nameof(naturalHeight));
            if (!float.IsFinite(rise)) throw new ArgumentOutOfRangeException(nameof(rise));
            Id = id;
            Profile = profile;

            // Keep authored bends and cap spacing so the same route can drive earthworks, paint and furniture.
            var points = new List<Vector2> { alignment[0] };
            var distance = new List<float> { 0f };
            for (int i = 1; i < alignment.Count; i++)
            {
                Vector2 a = points[points.Count - 1], b = alignment[i];
                float span = Vector2.Distance(a, b);
                if (!float.IsFinite(span)) throw new ArgumentException("Alignment coordinates must be finite.", nameof(alignment));
                if (span < 0.01f) continue;
                float start = distance[distance.Count - 1];
                int subdivisions = (int)Math.Ceiling(span / 5f);
                for (int step = 1; step <= subdivisions; step++)
                {
                    float t = step / (float)subdivisions;
                    points.Add(Vector2.Lerp(a, b, t));
                    distance.Add(start + span * t);
                }
            }
            if (points.Count < 2) throw new ArgumentException("An alignment needs a nonzero length.", nameof(alignment));

            var elevation = new float[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                elevation[i] = naturalHeight(points[i].X, points[i].Y) + rise;
                if (!float.IsFinite(elevation[i])) throw new ArgumentException("Terrain heights must be finite.", nameof(naturalHeight));
            }

            var smooth = new float[points.Count];
            float radius = profile.ControlledAccess ? 120f : 45f;
            float grade = profile.ControlledAccess ? 0.04f : 0.06f;
            int first = 0, last = 0;
            for (int i = 0; i < points.Count; i++)
            {
                while (distance[i] - distance[first] > radius) first++;
                while (last + 1 < points.Count && distance[last + 1] - distance[i] <= radius) last++;
                float sum = 0f, weight = 0f;
                for (int j = first; j <= last; j++)
                {
                    float t = 1f - Math.Abs(distance[j] - distance[i]) / radius;
                    sum += elevation[j] * t;
                    weight += t;
                }
                smooth[i] = sum / weight;
            }
            for (int i = 1; i < smooth.Length; i++)
                smooth[i] = Math.Min(smooth[i], smooth[i - 1] + grade * (distance[i] - distance[i - 1]));
            for (int i = smooth.Length - 2; i >= 0; i--)
                smooth[i] = Math.Min(smooth[i], smooth[i + 1] + grade * (distance[i + 1] - distance[i]));

            var stations = new RegionRoadStation[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 ahead = points[Math.Min(i + 1, points.Count - 1)] - points[Math.Max(i - 1, 0)];
                if (ahead.LengthSquared() < 0.000001f) throw new ArgumentException("An alignment cannot reverse at a station.", nameof(alignment));
                stations[i] = new RegionRoadStation(distance[i], smooth[i], points[i], Vector2.Normalize(ahead));
                if (i > 0) IndexSegment(i - 1, points[i - 1], points[i]);
            }
            Stations = Array.AsReadOnly(stations);
        }

        private void IndexSegment(int index, Vector2 a, Vector2 b)
        {
            float reach = Profile.EarthworkWidth;
            for (int z = Cell(Math.Min(a.Y, b.Y) - reach); z <= Cell(Math.Max(a.Y, b.Y) + reach); z++)
            for (int x = Cell(Math.Min(a.X, b.X) - reach); x <= Cell(Math.Max(a.X, b.X) + reach); x++)
            {
                var key = (x, z);
                if (!cells.TryGetValue(key, out List<int> indices)) cells[key] = indices = new List<int>();
                indices.Add(index);
            }
        }

        private static int Cell(float position) => (int)Math.Floor(position / GridSize);

        public RegionRoadStation At(float distance)
        {
            if (distance <= 0f) return Stations[0];
            if (distance >= Length) return Stations[Stations.Count - 1];
            int lo = 0, hi = Stations.Count - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (Stations[mid].Distance <= distance) lo = mid;
                else hi = mid;
            }
            return Interpolate(lo, (distance - Stations[lo].Distance) / (Stations[hi].Distance - Stations[lo].Distance));
        }

        private RegionRoadStation Interpolate(int i, float t)
        {
            RegionRoadStation a = Stations[i], b = Stations[i + 1];
            return new RegionRoadStation(RegionNoise.Lerp(a.Distance, b.Distance, t),
                RegionNoise.Lerp(a.Elevation, b.Elevation, t), Vector2.Lerp(a.Center, b.Center, t),
                Vector2.Normalize(Vector2.Lerp(a.Forward, b.Forward, t)));
        }

        public bool TryProject(Vector2 point, out RegionRoadStation station, out float offset)
        {
            station = default;
            offset = 0f;
            if (!cells.TryGetValue((Cell(point.X), Cell(point.Y)), out List<int> indices)) return false;
            float best = Profile.EarthworkWidth * Profile.EarthworkWidth;
            int segment = -1;
            float along = 0f;
            foreach (int i in indices)
            {
                Vector2 a = Stations[i].Center, delta = Stations[i + 1].Center - a;
                float t = Vector2.Dot(point - a, delta) / delta.LengthSquared();
                t = RegionNoise.Clamp01(t);
                float square = Vector2.DistanceSquared(point, a + delta * t);
                if (square >= best) continue;
                best = square;
                segment = i;
                along = t;
            }
            if (segment < 0) return false;
            // Do not extend the grading past an open end of the supplied route.
            RegionRoadStation start = Stations[0], end = Stations[Stations.Count - 1];
            if ((segment == 0 && along == 0f && Vector2.Dot(point - start.Center, start.Forward) < 0f) ||
                (segment == Stations.Count - 2 && along == 1f && Vector2.Dot(point - end.Center, end.Forward) > 0f)) return false;
            station = Interpolate(segment, along);
            offset = Vector2.Dot(point - station.Center, station.Right);
            return true;
        }

        public float GradeGround(Vector2 point, float naturalHeight)
        {
            if (!TryProject(point, out RegionRoadStation frame, out float offset)) return naturalHeight;
            float edge = Math.Abs(offset) - Profile.HalfWidth;
            float bed = frame.SurfaceAt(Math.Clamp(offset, -Profile.HalfWidth, Profile.HalfWidth), Profile) - 0.22f;
            if (edge <= 4f) return bed;
            float blend = RegionNoise.Smooth(4f, 20f, edge);
            float gutter = Profile.ControlledAccess ? 0.45f : 0.28f;
            float ditch = gutter * RegionNoise.Smooth(4f, 8f, edge) * (1f - RegionNoise.Smooth(9f, 15f, edge));
            return RegionNoise.Lerp(bed, naturalHeight, blend) - ditch;
        }
    }

    /// <summary>Stable placement data for an empty plot, with an entrance on the adjacent arterial shoulder.</summary>
    public sealed class RegionRoadsideSite
    {
        public const float MouthHalfWidth = 8.5f;
        public const float DriveHalfWidth = 3.8f;
        public const float Setback = 27f;
        public const float Frontage = 64f;
        public const float Depth = 42f;

        public string Id { get; }
        public RegionRoadCorridor Road { get; }
        public RegionRoadStation Station { get; }
        public int Side { get; }
        public float Edge => Road.Profile.HalfWidth;
        public Vector2 Entrance => Station.AtOffset(Side * Edge);
        public Vector2 PlotCenter => Station.AtOffset(Side * (Edge + Setback + Depth * 0.5f));
        public Vector2 Outward => Station.Right * Side;
        public float EntranceElevation => Station.SurfaceAt(Edge, Road.Profile);
        public float PlotElevation => EntranceElevation - 0.55f;

        public RegionRoadsideSite(string id, RegionRoadCorridor road, float distance, int side)
        {
            if (road == null) throw new ArgumentNullException(nameof(road));
            if (road.Profile.ControlledAccess) throw new ArgumentException("Plots require an arterial entrance.", nameof(road));
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A plot needs an id.", nameof(id));
            if (side != -1 && side != 1) throw new ArgumentOutOfRangeException(nameof(side));
            if (!(distance > Frontage && distance < road.Length - Frontage)) throw new ArgumentOutOfRangeException(nameof(distance));
            Id = road.Id + "/" + id;
            Road = road;
            Station = road.At(distance);
            Side = side;
        }

        public float ApproachElevation(float reach) => RegionNoise.Lerp(EntranceElevation, PlotElevation,
            RegionNoise.Smooth(0f, Setback, reach));

        public float GradeGround(Vector2 point, float ground)
        {
            Vector2 delta = point - Entrance;
            float reach = Vector2.Dot(delta, Outward);
            float along = Math.Abs(Vector2.Dot(delta, Station.Forward));
            if (reach < 0f || reach > Setback + Depth + 14f || along > Frontage * 0.5f + 14f) return ground;
            float plotEdge = Math.Max(Math.Max(Setback - reach, reach - Setback - Depth), along - Frontage * 0.5f);
            float plot = 1f - RegionNoise.Smooth(2f, 14f, plotEdge);
            float width = RegionNoise.Lerp(MouthHalfWidth, DriveHalfWidth, RegionNoise.Smooth(0f, 8f, reach));
            float drive = (1f - RegionNoise.Smooth(width + 2f, width + 7f, along)) *
                (1f - RegionNoise.Smooth(Setback, Setback + 5f, reach));
            return RegionNoise.Lerp(ground, ApproachElevation(reach) - 0.2f, Math.Max(plot, drive));
        }
    }

    /// <summary>Reproducible farmland demonstration in local metre coordinates.</summary>
    public sealed class RegionRoadPlan
    {
        public const float Extent = 2304f;
        public const float FieldDepth = 134f;
        public const float FieldSkew = 0.0009f;
        public RegionRoadCorridor Highway { get; }
        public RegionRoadCorridor Arterial { get; }
        public RegionRoadsideSite Site { get; }
        public int Seed { get; }

        public RegionRoadPlan(int seed, RegionRoadPreset preset)
        {
            if (preset < RegionRoadPreset.Country || preset > RegionRoadPreset.Wide) throw new ArgumentOutOfRangeException(nameof(preset));
            Seed = seed;
            bool wide = preset == RegionRoadPreset.Wide;
            var highway = new RegionRoadProfile(wide ? 3 : 2, 3.5f, wide ? 4.5f : 3f, 0.75f, 2.5f, true);
            var arterial = new RegionRoadProfile(wide ? 2 : 1, 3.25f, 0f, 0f, 1.15f, false);
            float phase = RegionNoise.Hash01(4, 32, (uint)seed) * 2.5f;
            float swing = preset == RegionRoadPreset.Sweeping ? 98f : 35f;
            var h = new List<Vector2>();
            var a = new List<Vector2>();
            for (float z = -Extent; z <= Extent; z += 16f)
            {
                float meander = swing * (float)Math.Sin(z / 410f + phase) + 32f * (float)Math.Sin(z / 970f);
                h.Add(new Vector2(-112f + meander, z));
                a.Add(new Vector2(106f + 0.62f * meander + 29f * (float)Math.Sin(z / 290f - phase), z));
            }
            Highway = new RegionRoadCorridor("highway", highway, h, NaturalHeight, 2.9f);
            Arterial = new RegionRoadCorridor("arterial", arterial, a, NaturalHeight, 0.8f);
            float fraction = 0.50f + 0.035f * RegionNoise.Hash01(2, 13, (uint)seed);
            Site = new RegionRoadsideSite("plot-01", Arterial, Arterial.Length * fraction, 1);
        }

        public float NaturalHeight(float x, float z)
        {
            float plain = 23f + 4f * RegionNoise.Fbm(x / 650f, z / 650f, 2, (uint)Seed);
            float margin = Math.Abs(x + 65f * (float)Math.Sin(z / 680f));
            float hills = RegionNoise.Smooth(245f, 1650f, margin);
            float ridge = RegionNoise.Fbm(x / 820f, z / 950f, 4, (uint)Seed ^ 0x74051u);
            float crest = 31f * (1f - RegionNoise.Smooth(0f, 1040f, Math.Abs(z - 1530f + 0.12f * x)));
            float row = z / FieldDepth + x * FieldSkew;
            float distance = (row - (float)Math.Round(row)) * FieldDepth;
            float fields = 1f - RegionNoise.Smooth(480f, 940f, margin);
            float ditch = 0.85f * (1f - RegionNoise.Smooth(0.8f, 5.7f, Math.Abs(distance)));
            float bank = 0.53f * (1f - RegionNoise.Smooth(1f, 5.2f, Math.Abs(distance - 7f)));
            return plain + crest + hills * (57f + 205f * (ridge + 0.3f)) + fields * (bank - ditch);
        }

        public float GroundHeight(float x, float z)
        {
            var point = new Vector2(x, z);
            float ground = Highway.GradeGround(point, NaturalHeight(x, z));
            ground = Arterial.GradeGround(point, ground);
            return Site.GradeGround(point, ground);
        }
    }
}
