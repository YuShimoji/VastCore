using System;
using System.Collections.Generic;

namespace Vastcore.RegionMap
{
    public readonly struct RegionPosition
    {
        public readonly float X;
        public readonly float Y;

        public RegionPosition(float x, float y) { X = x; Y = y; }

        internal float DistanceTo(RegionPosition other)
        {
            float dx = other.X - X;
            float dy = other.Y - Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public enum RegionDevelopmentFeatureKind { Block, Roof, AirportGround, Runway, Apron, RuralLot, RuralRoof }

    public readonly struct RegionDevelopmentFeature
    {
        public readonly RegionDevelopmentFeatureKind Kind;
        public readonly RegionPosition Center;
        public readonly float LengthMeters;
        public readonly float WidthMeters;
        public readonly float Angle;

        internal RegionDevelopmentFeature(RegionDevelopmentFeatureKind kind, RegionPosition center, float length, float width, float angle)
        {
            Kind = kind;
            Center = center;
            LengthMeters = length;
            WidthMeters = width;
            Angle = angle;
        }

        internal RegionPosition Offset(float length, float width)
        {
            float sin = (float)Math.Sin(Angle);
            float cos = (float)Math.Cos(Angle);
            return new RegionPosition(Center.X + length * cos - width * sin, Center.Y + length * sin + width * cos);
        }

        internal bool Contains(RegionPosition point)
        {
            float x = point.X - Center.X;
            float y = point.Y - Center.Y;
            float sin = (float)Math.Sin(Angle);
            float cos = (float)Math.Cos(Angle);
            return Math.Abs(x * cos + y * sin) <= LengthMeters * 0.5f &&
                Math.Abs(y * cos - x * sin) <= WidthMeters * 0.5f;
        }
    }

    /// <summary>同じ地域・拡大窓で共有する配置。座標は地域北西端からのメートル。</summary>
    public sealed class RegionDevelopment
    {
        public RegionPosition PrimaryCity { get; }
        public RegionPosition SecondaryCity { get; }
        public RegionDevelopmentFeature Airport { get; }
        public RegionPosition RuralBuildings { get; }
        public IReadOnlyList<RegionPosition> Highway { get; }
        public IReadOnlyList<RegionPosition> AirportAccess { get; }
        public IReadOnlyList<(RegionPosition Start, RegionPosition End, float Width)> Streets { get; }
        public IReadOnlyList<RegionDevelopmentFeature> Features { get; }

        internal RegionDevelopment(RegionPosition primary, RegionPosition secondary, RegionDevelopmentFeature airport,
            RegionPosition rural, List<RegionPosition> highway, List<RegionPosition> access,
            List<(RegionPosition, RegionPosition, float)> streets, List<RegionDevelopmentFeature> features)
        {
            PrimaryCity = primary;
            SecondaryCity = secondary;
            Airport = airport;
            RuralBuildings = rural;
            Highway = highway.AsReadOnly();
            AirportAccess = access.AsReadOnly();
            Streets = streets.AsReadOnly();
            Features = features.AsReadOnly();
        }

        public IEnumerable<(string Name, RegionPosition Center)> Views()
        {
            yield return ("city-1", PrimaryCity);
            yield return ("city-2", SecondaryCity);
            yield return ("airport", Airport.Center);
            yield return ("rural", RuralBuildings);
        }
    }

    internal sealed class RegionDevelopmentBuilder
    {
        private const float PrimaryRadius = 8600f;
        private const float SecondaryRadius = 5500f;
        private const float HighwayReserve = 42f;
        private const float AccessReserve = 24f;
        private readonly RegionData _terrain;
        private readonly int _grid;
        private readonly float _step;
        private readonly float _river;
        private readonly uint _seed;

        private RegionDevelopmentBuilder(RegionData terrain)
        {
            _terrain = terrain;
            _grid = Math.Max(16, Math.Min(256, (int)Math.Ceiling(terrain.SizeKm)));
            _step = terrain.SizeKm * 1000f / _grid;
            _river = RegionGenerator.RiverThreshold(terrain);
            _seed = unchecked((uint)terrain.Spec.Seed * 2654435761u + 7829u);
        }

        internal static RegionDevelopment Build(RegionData terrain)
        {
            return new RegionDevelopmentBuilder(terrain).Build();
        }

        private RegionDevelopment Build()
        {
            var sites = new List<(int Node, float Score)>();
            for (int i = 0; i < _grid * _grid; i++)
            {
                RegionPosition point = Position(i);
                if (!Dry(point, false, out float height) || height > 900f) continue;
                int suitable = 0;
                for (int k = 0; k < 8; k++)
                {
                    double angle = k * Math.PI / 4;
                    var sample = new RegionPosition(point.X + 4200f * (float)Math.Cos(angle), point.Y + 4200f * (float)Math.Sin(angle));
                    if (Dry(sample, false, out float h) && Math.Abs(h - height) < 240f) suitable++;
                }
                if (suitable < 6) continue;
                float score = suitable * 14f - height * 0.06f + RegionNoise.Hash01(i, 0, _seed) * 8f;
                sites.Add((i, score));
            }
            sites.Sort((a, b) => b.Score != a.Score ? b.Score.CompareTo(a.Score) : a.Node.CompareTo(b.Node));

            var tried = new List<RegionPosition>();
            foreach (var first in sites)
            {
                RegionPosition primary = Position(first.Node);
                if (tried.Exists(p => p.DistanceTo(primary) < 12000f)) continue;
                tried.Add(primary);
                var partners = new List<RegionPosition>();
                foreach (var second in sites)
                {
                    RegionPosition secondary = Position(second.Node);
                    float distance = primary.DistanceTo(secondary);
                    if (distance < 30000f || distance > 95000f || partners.Exists(p => p.DistanceTo(secondary) < 10000f)) continue;
                    partners.Add(secondary);
                    List<RegionPosition> highway = Route(first.Node, second.Node, null);
                    if (highway != null && TryAirport(primary, secondary, highway, out var airport, out var access))
                    {
                        var features = new List<RegionDevelopmentFeature>();
                        var streets = new List<(RegionPosition, RegionPosition, float)>();
                        if (AddCity(features, streets, primary, PrimaryRadius, 0, highway, access) &&
                            AddCity(features, streets, secondary, SecondaryRadius, 1, highway, access) &&
                            AddRural(features, primary, secondary, airport, highway, access, out RegionPosition rural))
                        {
                            AddAirport(features, airport);
                            return new RegionDevelopment(primary, secondary, airport, rural, highway, access, streets, features);
                        }
                    }
                    if (partners.Count == 5) break;
                }
                if (tried.Count == 6) break;
            }
            return null;
        }

        private bool TryAirport(RegionPosition primary, RegionPosition secondary, List<RegionPosition> highway,
            out RegionDevelopmentFeature airport, out List<RegionPosition> access)
        {
            var candidates = new List<(RegionDevelopmentFeature Ground, int Join, float Score)>();
            for (int i = 0; i < _grid * _grid; i++)
            {
                RegionPosition p = Position(i);
                if (p.DistanceTo(primary) < PrimaryRadius + 2500f || p.DistanceTo(secondary) < SecondaryRadius + 2500f) continue;
                int join = Nearest(highway, p);
                float distance = p.DistanceTo(highway[join]);
                if (distance < 2800f || distance > 14000f || NearRoad(highway, p, 2400f)) continue;
                for (int direction = 0; direction < 4; direction++)
                {
                    var ground = new RegionDevelopmentFeature(RegionDevelopmentFeatureKind.AirportGround, p, 4300f, 1050f,
                        direction * (float)Math.PI / 4f);
                    if (!DryFootprint(ground, 55f, 6, out float relief)) continue;
                    candidates.Add((ground, join, relief + distance * 0.0035f + p.DistanceTo(primary) * 0.00035f));
                }
            }
            // List.Sort is unstable, so preserve a deterministic order for equal-cost orientations and sites.
            candidates.Sort((a, b) =>
            {
                int score = a.Score.CompareTo(b.Score);
                if (score != 0) return score;
                int index = Node(a.Ground.Center).CompareTo(Node(b.Ground.Center));
                return index != 0 ? index : a.Ground.Angle.CompareTo(b.Ground.Angle);
            });
            for (int i = 0; i < Math.Min(24, candidates.Count); i++)
            {
                var candidate = candidates[i];
                int end = Node(candidate.Ground.Offset(0f, 1320f));
                access = Route(Node(highway[candidate.Join]), end, candidate.Ground);
                if (access == null) continue;
                RegionPosition entrance = candidate.Ground.Offset(0f, 470f);
                if (!RoadSegment(access[access.Count - 1], entrance, null, out _)) continue;
                access.Add(entrance);
                airport = candidate.Ground;
                return true;
            }
            airport = default;
            access = null;
            return false;
        }

        private bool AddCity(List<RegionDevelopmentFeature> features, List<(RegionPosition Start, RegionPosition End, float Width)> streets,
            RegionPosition center, float radius, int city,
            List<RegionPosition> highway, List<RegionPosition> access)
        {
            const float pitch = 570f;
            int extent = (int)Math.Ceiling(radius / pitch);
            float angle = (RegionNoise.Hash01(city, 0, _seed) - 0.5f) * 1.15f;
            var grid = new RegionDevelopmentFeature(RegionDevelopmentFeatureKind.Block, center, 0f, 0f, angle);
            var edges = new HashSet<(int X, int Y, bool Vertical)>();
            int firstStreet = streets.Count;
            int firstFeature = features.Count;
            for (int y = -extent; y <= extent; y++)
            for (int x = -extent; x <= extent; x++)
            {
                float radial = (x * x + y * y) * pitch * pitch / (radius * radius);
                float edge = 0.73f + 0.45f * RegionNoise.Hash01(x, y, _seed + (uint)city);
                if (radial > edge) continue;
                RegionPosition p = grid.Offset(x * pitch, y * pitch);
                var block = new RegionDevelopmentFeature(RegionDevelopmentFeatureKind.Block, p, 506f, 480f, angle);
                if (!DryFootprint(block, 58f, 2, out _)) continue;
                features.Add(block);
                Street(x, y, false);
                Street(x, y + 1, false);
                Street(x, y, true);
                Street(x + 1, y, true);
                for (int lot = 0; lot < 9; lot++)
                {
                    float random = RegionNoise.Hash01(x * 11 + lot, y, _seed + (uint)city + 53u);
                    if (random < 0.10f + 0.48f * radial) continue;
                    float offset = (RegionNoise.Hash01(lot, x - y, _seed + 9263u) - 0.5f) * 37f;
                    RegionPosition roof = block.Offset((lot % 3 - 1) * 154f + offset, (lot / 3 - 1) * 144f - offset);
                    if (NearRoad(highway, roof, HighwayReserve + 105f) || NearRoad(access, roof, AccessReserve + 105f)) continue;
                    float length = 48f + 87f * random;
                    float width = 38f + 77f * RegionNoise.Hash01(x, y * 11 + lot, _seed + 291u);
                    features.Add(new RegionDevelopmentFeature(RegionDevelopmentFeatureKind.Roof, roof, length, width, angle));
                }
            }

            // 接続可能な最寄りの街路を選び、幹線の端から中心部へつなぐ。
            float bestDistance = 3100f;
            RegionPosition endpoint = default;
            for (int i = firstStreet; i < streets.Count; i++)
            {
                Consider(streets[i].Start);
                Consider(streets[i].End);
            }
            if (bestDistance >= 3100f) return false;
            streets.Add((center, endpoint, 28f));
            var collector = new List<RegionPosition> { center, endpoint };
            for (int i = features.Count - 1; i >= firstFeature; i--)
                if (features[i].Kind == RegionDevelopmentFeatureKind.Roof && NearRoad(collector, features[i].Center, 106f))
                    features.RemoveAt(i);
            return true;

            void Street(int x, int y, bool vertical)
            {
                if (!edges.Add((x, y, vertical))) return;
                RegionPosition a = grid.Offset((x - 0.5f) * pitch, (y - 0.5f) * pitch);
                RegionPosition b = grid.Offset((x - 0.5f + (vertical ? 0 : 1)) * pitch,
                    (y - 0.5f + (vertical ? 1 : 0)) * pitch);
                if (RoadSegment(a, b, null, out _)) streets.Add((a, b, 16f));
            }

            void Consider(RegionPosition point)
            {
                float distance = point.DistanceTo(center);
                if (distance >= bestDistance || distance < 1f || !RoadSegment(center, point, null, out _)) return;
                bestDistance = distance;
                endpoint = point;
            }
        }

        private static void AddAirport(List<RegionDevelopmentFeature> features, RegionDevelopmentFeature ground)
        {
            features.Add(ground);
            features.Add(new RegionDevelopmentFeature(RegionDevelopmentFeatureKind.Runway, ground.Offset(0f, -165f), 3550f, 85f, ground.Angle));
            features.Add(new RegionDevelopmentFeature(RegionDevelopmentFeatureKind.Apron, ground.Offset(0f, 180f), 2100f, 240f, ground.Angle));
            features.Add(new RegionDevelopmentFeature(RegionDevelopmentFeatureKind.Apron, ground.Offset(0f, 0f), 55f, 360f, ground.Angle));
            features.Add(new RegionDevelopmentFeature(RegionDevelopmentFeatureKind.Apron, ground.Offset(0f, 461f), 560f, 115f, ground.Angle));
            features.Add(new RegionDevelopmentFeature(RegionDevelopmentFeatureKind.Roof, ground.Offset(0f, 350f), 440f, 146f, ground.Angle));
            features.Add(new RegionDevelopmentFeature(RegionDevelopmentFeatureKind.Roof, ground.Offset(-574f, 347f), 160f, 106f, ground.Angle));
            features.Add(new RegionDevelopmentFeature(RegionDevelopmentFeatureKind.Roof, ground.Offset(695f, 342f), 195f, 130f, ground.Angle));
        }

        private bool AddRural(List<RegionDevelopmentFeature> features, RegionPosition primary, RegionPosition secondary,
            RegionDevelopmentFeature airport, List<RegionPosition> highway, List<RegionPosition> access, out RegionPosition first)
        {
            first = default;
            int placed = 0;
            float nextCluster = 0f;
            for (int i = 1; i < highway.Count; i++)
            {
                RegionPosition a = highway[i - 1];
                RegionPosition b = highway[i];
                nextCluster -= a.DistanceTo(b);
                if (nextCluster > 0f || b.DistanceTo(primary) < PrimaryRadius + 1500f ||
                    b.DistanceTo(secondary) < SecondaryRadius + 1500f || b.DistanceTo(airport.Center) < 4300f) continue;
                float angle = (float)Math.Atan2(b.Y - a.Y, b.X - a.X);
                var neighborhood = new RegionDevelopmentFeature(RegionDevelopmentFeatureKind.RuralLot, b, 0f, 0f, angle);
                int before = placed;
                for (int k = 0; k < 7; k++)
                {
                    float offset = (330f + 930f * RegionNoise.Hash01(i, k, _seed)) * (k % 2 == 0 ? 1f : -1f);
                    RegionPosition center = neighborhood.Offset((k - 3) * 235f, offset);
                    var lot = new RegionDevelopmentFeature(RegionDevelopmentFeatureKind.RuralLot, center, 161f, 110f, angle);
                    if (!DryFootprint(lot, 13f, 2, out _) || NearRoad(highway, center, 135f) || NearRoad(access, center, 135f)) continue;
                    if (placed++ == 0) first = center;
                    features.Add(lot);
                    features.Add(new RegionDevelopmentFeature(RegionDevelopmentFeatureKind.RuralRoof, center, 68f, 45f, angle));
                }
                if (placed > before) nextCluster = 2840f;
            }
            return placed > 0;
        }

        private bool DryFootprint(RegionDevelopmentFeature feature, float maximumRelief, int steps, out float relief)
        {
            float low = float.MaxValue;
            float high = float.MinValue;
            for (int y = 0; y <= 2; y++)
            for (int x = 0; x <= steps; x++)
            {
                RegionPosition p = feature.Offset((x / (float)steps - 0.5f) * feature.LengthMeters, (y * 0.5f - 0.5f) * feature.WidthMeters);
                if (!Dry(p, false, out float height)) { relief = 0f; return false; }
                low = Math.Min(low, height);
                high = Math.Max(high, height);
            }
            relief = high - low;
            return relief <= maximumRelief;
        }

        private bool Dry(RegionPosition point, bool allowRiver, out float height)
        {
            int x = (int)(point.X / _terrain.CellMeters);
            int y = (int)(point.Y / _terrain.CellMeters);
            if (x < 0 || y < 0 || x >= _terrain.Resolution || y >= _terrain.Resolution) { height = 0f; return false; }
            int i = _terrain.Index(x, y);
            height = _terrain.Height[i];
            return height > 0f && _terrain.LakeDepth[i] <= 0f && (allowRiver || _terrain.Flow[i] <= _river);
        }

        private List<RegionPosition> Route(int start, int end, RegionDevelopmentFeature? exclusion)
        {
            var cost = new float[_grid * _grid];
            var parent = new int[cost.Length];
            for (int i = 0; i < cost.Length; i++) { cost[i] = float.PositiveInfinity; parent[i] = -1; }
            var pending = new SortedSet<(float Priority, int Node)>();
            cost[start] = 0f;
            RegionPosition goal = Position(end);
            pending.Add((Position(start).DistanceTo(goal), start));
            while (pending.Count != 0)
            {
                var next = pending.Min;
                pending.Remove(next);
                int i = next.Node;
                RegionPosition from = Position(i);
                if (next.Priority > cost[i] + from.DistanceTo(goal)) continue;
                if (i == end) return Outline(parent, end, exclusion);
                int x = i % _grid;
                int y = i / _grid;
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = x + dx, ny = y + dy;
                    if ((dx == 0 && dy == 0) || nx < 0 || ny < 0 || nx >= _grid || ny >= _grid) continue;
                    int j = nx + ny * _grid;
                    RegionPosition to = Position(j);
                    if (!RoadSegment(from, to, exclusion, out float edge)) continue;
                    float value = cost[i] + edge;
                    if (value >= cost[j]) continue;
                    cost[j] = value;
                    parent[j] = i;
                    pending.Add((value + to.DistanceTo(goal), j));
                }
            }
            return null;
        }

        private List<RegionPosition> Outline(int[] parent, int end, RegionDevelopmentFeature? exclusion)
        {
            var path = new List<RegionPosition>();
            for (int i = end; i >= 0; i = parent[i]) path.Add(Position(i));
            path.Reverse();
            var outline = new List<RegionPosition> { path[0] };
            for (int i = 0; i < path.Count - 1;)
            {
                int farthest = Math.Min(i + 3, path.Count - 1);
                while (farthest > i + 1 && !RoadSegment(path[i], path[farthest], exclusion, out _)) farthest--;
                i = farthest;
                outline.Add(path[i]);
            }
            return outline;
        }

        private bool RoadSegment(RegionPosition a, RegionPosition b, RegionDevelopmentFeature? exclusion, out float cost)
        {
            float length = a.DistanceTo(b);
            int steps = Math.Max(1, (int)Math.Ceiling(length / (_terrain.CellMeters * 0.5f)));
            float grade = 0f;
            float previous = 0f;
            int channels = 0;
            cost = 0f;
            for (int k = 0; k <= steps; k++)
            {
                float t = k / (float)steps;
                var p = new RegionPosition(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
                if ((exclusion.HasValue && exclusion.Value.Contains(p)) || !Dry(p, true, out float h)) return false;
                if (k > 0) grade += Math.Abs(h - previous);
                previous = h;
                if (!Dry(p, false, out _)) channels++;
            }
            if (grade > length * 0.24f) return false;
            cost = length + grade * 14f + length * channels / (steps + 1) * 2.3f;
            return true;
        }

        private int Node(RegionPosition point)
        {
            int x = Math.Max(0, Math.Min(_grid - 1, (int)(point.X / _step)));
            int y = Math.Max(0, Math.Min(_grid - 1, (int)(point.Y / _step)));
            return x + y * _grid;
        }
        private RegionPosition Position(int node) => new RegionPosition((node % _grid + 0.5f) * _step, (node / _grid + 0.5f) * _step);

        private static int Nearest(List<RegionPosition> points, RegionPosition p)
        {
            int closest = 0;
            float distance = float.MaxValue;
            for (int i = 0; i < points.Count; i++)
            {
                float candidate = p.DistanceTo(points[i]);
                if (candidate >= distance) continue;
                distance = candidate;
                closest = i;
            }
            return closest;
        }

        private static bool NearRoad(List<RegionPosition> points, RegionPosition p, float distance)
        {
            for (int i = 1; i < points.Count; i++)
            {
                RegionPosition a = points[i - 1], b = points[i];
                if (p.X < Math.Min(a.X, b.X) - distance || p.X > Math.Max(a.X, b.X) + distance ||
                    p.Y < Math.Min(a.Y, b.Y) - distance || p.Y > Math.Max(a.Y, b.Y) + distance) continue;
                float dx = b.X - a.X, dy = b.Y - a.Y;
                float t = RegionNoise.Clamp01(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / Math.Max(1f, dx * dx + dy * dy));
                if (p.DistanceTo(new RegionPosition(a.X + dx * t, a.Y + dy * t)) < distance) return true;
            }
            return false;
        }
    }
}
