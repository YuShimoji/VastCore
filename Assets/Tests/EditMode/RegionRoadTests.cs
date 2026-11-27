using NUnit.Framework;
using Vastcore.RegionMap;
using Vector2 = System.Numerics.Vector2;

namespace Vastcore.Tests.EditMode
{
    public class RegionRoadTests
    {
        [TestCase(8, RegionRoadPreset.Country, 2, 1)]
        [TestCase(19, RegionRoadPreset.Sweeping, 2, 1)]
        [TestCase(8, RegionRoadPreset.Wide, 3, 2)]
        public void RegionRoadPlan_RebuildsSeparatedRoadsAndArterialEntrance(int seed, RegionRoadPreset preset,
            int highwayLanes, int arterialLanes)
        {
            var plan = new RegionRoadPlan(seed, preset);
            var repeat = new RegionRoadPlan(seed, preset);
            Assert.That(plan.Highway.Profile.LanesPerDirection, Is.EqualTo(highwayLanes));
            Assert.That(plan.Arterial.Profile.LanesPerDirection, Is.EqualTo(arterialLanes));
            Assert.That(plan.Highway.Profile.ControlledAccess, Is.True);
            Assert.That(plan.Site.Road, Is.SameAs(plan.Arterial));
            Assert.That(plan.Site.Id, Is.EqualTo(repeat.Site.Id));
            Assert.That(plan.Site.Entrance, Is.EqualTo(repeat.Site.Entrance));
            Assert.That(plan.Site.PlotCenter, Is.EqualTo(repeat.Site.PlotCenter));
            Assert.That(plan.Arterial.TryProject(plan.Site.Entrance, out _, out float shoulder), Is.True);
            Assert.That(shoulder, Is.EqualTo(plan.Arterial.Profile.HalfWidth).Within(0.04f));
            Assert.That(Vector2.Dot(plan.Site.PlotCenter - plan.Site.Entrance, plan.Site.Outward),
                Is.EqualTo(RegionRoadsideSite.Setback + RegionRoadsideSite.Depth * 0.5f).Within(0.01f));
            Assert.That(plan.Highway.TryProject(plan.Site.PlotCenter, out _, out _), Is.False);
            Assert.Throws<System.ArgumentException>(() => new RegionRoadsideSite("invalid", plan.Highway, 200f, 1));
            foreach (RegionRoadStation station in plan.Arterial.Stations)
                Assert.That(plan.Highway.TryProject(station.Center, out _, out _), Is.False);
        }

        [Test]
        public void RegionRoadCorridor_LimitsGradeAndStopsEarthworksAtRouteEnds()
        {
            var profile = new RegionRoadProfile(2, 3.5f, 3f, 0.75f, 2.5f, true);
            var road = new RegionRoadCorridor("crest", profile,
                new[] { new Vector2(0f, 0f), new Vector2(0f, 280f), new Vector2(10f, 700f) },
                (x, z) => z < 225f ? 23f : 53f, 0.8f);
            for (int i = 1; i < road.Stations.Count; i++)
            {
                RegionRoadStation a = road.Stations[i - 1], b = road.Stations[i];
                Assert.That(System.Math.Abs(b.Elevation - a.Elevation), Is.LessThanOrEqualTo(0.04f * (b.Distance - a.Distance) + 0.00001f));
            }
            RegionRoadStation frame = road.At(170f);
            Assert.That(road.GradeGround(frame.AtOffset(0f), 23f), Is.EqualTo(frame.Elevation - 0.22f).Within(0.01f));
            Assert.That(road.GradeGround(frame.AtOffset(profile.EarthworkWidth + 1f), 19f), Is.EqualTo(19f));
            Assert.That(road.TryProject(new Vector2(0f, -1f), out _, out _), Is.False);
            Assert.That(road.TryProject(new Vector2(10f, 701f), out _, out _), Is.False);
        }

        [TestCase(8, RegionRoadPreset.Country)]
        [TestCase(19, RegionRoadPreset.Sweeping)]
        public void RoadsideRelief_KeepsRoadAndSiteClearWithADitchBelowTheBank(int seed, RegionRoadPreset preset)
        {
            var plan = new RegionRoadPlan(seed, preset);
            RegionRoadCorridor road = plan.Arterial;
            RegionRoadStation station = road.At(plan.Site.Station.Distance + 32f);

            float Slope(float fromPaving)
            {
                Vector2 point = station.AtOffset(-road.Profile.HalfWidth - fromPaving);
                return RegionRoadMargin.GradedTerrain(plan, point.X, point.Y);
            }

            Vector2 pavement = station.AtOffset(-road.Profile.HalfWidth + 1f);
            Assert.That(RegionRoadMargin.GradedTerrain(plan, pavement.X, pavement.Y),
                Is.EqualTo(plan.GroundHeight(pavement.X, pavement.Y)).Within(0.01f));
            Assert.That(Slope(RegionRoadMargin.DitchCenter), Is.LessThan(Slope(RegionRoadMargin.SlopeToe) - 0.30f));
            Assert.That(Slope(RegionRoadMargin.BankCrest), Is.GreaterThan(Slope(RegionRoadMargin.DitchCenter) + 0.65f));

            Vector2 approach = plan.Site.Entrance + plan.Site.Outward * 12f;
            Assert.That(RegionRoadMargin.InSite(plan.Site, approach), Is.True);
            Assert.That(RegionRoadMargin.InSite(plan.Site, approach + plan.Site.Station.Forward * 15f), Is.False);
            Vector2 plot = plan.Site.PlotCenter;
            Assert.That(RegionRoadMargin.InSite(plan.Site, plot), Is.True);
            Assert.That(RegionRoadMargin.GradedTerrain(plan, plot.X, plot.Y),
                Is.EqualTo(plan.Site.PlotElevation - 0.2f).Within(0.01f));
        }
    }
}
