using System;
using System.Numerics;

namespace Vastcore.RegionMap
{
    /// <summary>A roadside section. Distances are measured outward from the paved edge, in metres.</summary>
    public static class RegionRoadMargin
    {
        public const float GravelWidth = 2.2f;
        public const float SlopeToe = 6.2f;
        public const float DitchCenter = 9.6f;
        public const float BankCrest = 14.4f;
        public const float FieldEdge = 20f;
        private const float DitchNearEdge = 8.1f;
        private const float DitchFarEdge = 11.1f;
        private const float BankNearEdge = 13.7f;
        private const float BankFarEdge = 15.1f;

        public static float Grade(RegionRoadCorridor road, Vector2 point, float natural, RegionRoadsideSite entrance)
        {
            if (!road.TryProject(point, out RegionRoadStation station, out float offset)) return natural;
            float edge = Math.Abs(offset) - road.Profile.HalfWidth;
            float bed = station.SurfaceAt(Math.Clamp(offset, -road.Profile.HalfWidth, road.Profile.HalfWidth), road.Profile) - 0.22f;
            if (edge <= 0f) return bed;

            float slopeToe = Math.Min(bed - 0.36f, natural + 0.03f);
            float ditch = Math.Min(slopeToe - 0.48f, natural - 0.91f);
            float bank = natural + 0.76f;
            float height = edge <= GravelWidth ? bed - 0.065f * edge / GravelWidth
                : edge <= SlopeToe ? Blend(bed - 0.065f, slopeToe, GravelWidth, SlopeToe, edge)
                : edge <= DitchNearEdge ? Blend(slopeToe, ditch, SlopeToe, DitchNearEdge, edge)
                : edge <= DitchFarEdge ? ditch
                : edge <= BankNearEdge ? Blend(ditch, bank, DitchFarEdge, BankNearEdge, edge)
                : edge <= BankFarEdge ? bank
                : Blend(bank, natural, BankFarEdge, FieldEdge, edge);
            if (entrance != null && Math.Sign(offset) == entrance.Side)
            {
                float separation = Math.Abs(station.Distance - entrance.Station.Distance);
                float enabled = RegionNoise.Smooth(RegionRoadsideSite.MouthHalfWidth + 1f, 19f, separation);
                if (enabled < 1f) height = RegionNoise.Lerp(road.GradeGround(point, natural), height, enabled);
            }
            return height;
        }

        /// <summary>Clearance for planting and surface dressing. The approach and pad keep independent footprints.</summary>
        public static bool InSite(RegionRoadsideSite site, Vector2 point, float padding = 0f)
        {
            Vector2 delta = point - site.Entrance;
            float reach = Vector2.Dot(delta, site.Outward);
            float along = Math.Abs(Vector2.Dot(delta, site.Station.Forward));
            float width = RegionNoise.Lerp(RegionRoadsideSite.MouthHalfWidth, RegionRoadsideSite.DriveHalfWidth,
                RegionNoise.Smooth(0f, 8f, reach));
            return (reach > -padding && reach < RegionRoadsideSite.Setback + padding && along < width + padding) ||
                (reach > RegionRoadsideSite.Setback - padding && reach < RegionRoadsideSite.Setback + RegionRoadsideSite.Depth + padding &&
                along < RegionRoadsideSite.Frontage * 0.5f + padding);
        }

        /// <summary>The crop parcels use the same edges for terrain relief, coverage and planting.</summary>
        public static float FieldColumn(float x, float z, int row, uint seed, out float width)
        {
            width = 105f + 35f * RegionNoise.Hash01(row, 6, seed);
            return x / width + 0.5f * RegionNoise.Hash01(row, 9, seed) - z * 0.0011f;
        }

        public static float GradedTerrain(RegionRoadPlan plan, float x, float z)
        {
            float natural = plan.NaturalHeight(x, z);
            float row = z / RegionRoadPlan.FieldDepth + x * RegionRoadPlan.FieldSkew;
            float column = FieldColumn(x, z, (int)Math.Floor(row), (uint)plan.Seed, out float width);
            float edge = (column - (float)Math.Round(column)) * width;
            float headland = 0.58f * (1f - RegionNoise.Smooth(0.75f, 5.6f, Math.Abs(edge)));
            float ditch = 0.34f * (1f - RegionNoise.Smooth(0.65f, 4.5f, Math.Abs(edge + 5f)));
            float along = Math.Abs(row - (float)Math.Round(row)) * RegionRoadPlan.FieldDepth;
            float fields = (1f - RegionNoise.Smooth(480f, 940f, Math.Abs(x + 65f * (float)Math.Sin(z / 680f)))) *
                RegionNoise.Smooth(2f, 9f, along);
            natural += (headland - ditch) * fields;
            var point = new Vector2(x, z);
            float ground = Grade(plan.Highway, point, natural, null);
            ground = Grade(plan.Arterial, point, ground, plan.Site);
            return plan.Site.GradeGround(point, ground);
        }

        private static float Blend(float a, float b, float start, float end, float distance) =>
            RegionNoise.Lerp(a, b, RegionNoise.Smooth(start, end, distance));
    }
}
