using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Vastcore.RegionMap;

namespace Vastcore.Tests.EditMode
{
    [TestFixture]
    public class RegionSeamlessWorldTests
    {
        private static RegionData _cachedRegion;

        private static RegionData GetTestRegion()
        {
            if (_cachedRegion == null)
            {
                _cachedRegion = RegionGenerator.Generate(new RegionSpec
                {
                    Seed = 9,
                    Resolution = 256,
                    SizeKm = 256f,
                });
            }
            return _cachedRegion;
        }

        // ---- Phase 1: Coordinates, Floating Origin, Chunk Planner & Builder ----

        [Test]
        public void Coordinates_MapWorldLocal_RoundTripIsAccurate()
        {
            var coords = new RegionWorldCoordinates(256f, 128000.0, 128000.0);
            float mapX = 43.61f;
            float mapY = 76.92f;

            coords.MapToWorldMeters(mapX, mapY, out double eastM, out double southM);
            coords.WorldMetersToMap(eastM, southM, out float rtMapX, out float rtMapY);
            Assert.That(rtMapX, Is.EqualTo(mapX).Within(1e-4f));
            Assert.That(rtMapY, Is.EqualTo(mapY).Within(1e-4f));

            float heightM = 312.5f;
            coords.WorldToLocal(eastM, heightM, southM, out float lx, out float ly, out float lz);
            coords.LocalToWorld(lx, ly, lz, out double rtEast, out float rtHeight, out double rtSouth);
            Assert.That(rtEast, Is.EqualTo(eastM).Within(0.05));
            Assert.That(rtSouth, Is.EqualTo(southM).Within(0.05));
            Assert.That(rtHeight, Is.EqualTo(heightM).Within(1e-4f));
        }

        [Test]
        public void Coordinates_FloatingOriginRecenter_PreservesWorldPosition()
        {
            var coords = new RegionWorldCoordinates(256f, 100000.0, 100000.0, 2000.0);
            double targetEast = 102500.0;
            double targetSouth = 101800.0;
            float height = 120f;

            coords.WorldToLocal(targetEast, height, targetSouth, out float lx, out float ly, out float lz);
            Assert.IsTrue(coords.NeedsRecenter(lx, lz));

            RegionWorldCoordinates.SnapToGrid(targetEast, targetSouth, 512.0, out double newOriginE, out double newOriginS);
            bool shifted = coords.Recenter(newOriginE, newOriginS, out float shiftX, out float shiftZ);
            Assert.IsTrue(shifted);

            float shiftedLx = lx + shiftX;
            float shiftedLz = lz + shiftZ;
            coords.LocalToWorld(shiftedLx, ly, shiftedLz, out double afterE, out float afterH, out double afterS);

            Assert.That(afterE, Is.EqualTo(targetEast).Within(0.01));
            Assert.That(afterS, Is.EqualTo(targetSouth).Within(0.01));
            Assert.That(afterH, Is.EqualTo(height).Within(1e-4f));
            Assert.IsFalse(coords.NeedsRecenter(shiftedLx, shiftedLz));
        }

        [Test]
        public void ChunkPlanner_BoundsAndPlanRequiredChunks_AreValidAndOrdered()
        {
            var planner = new RegionChunkPlanner();
            double focusE = 110000.0;
            double focusS = 140000.0;

            RegionChunkId near = planner.ChunkAt(RegionChunkRing.Near, focusE, focusS);
            planner.ChunkBoundsMeters(near, out double minE, out double minS, out float size);
            Assert.That(size, Is.EqualTo(512f));
            Assert.That(focusE, Is.GreaterThanOrEqualTo(minE).And.LessThan(minE + size));
            Assert.That(focusS, Is.GreaterThanOrEqualTo(minS).And.LessThan(minS + size));

            List<RegionChunkId> plan = planner.PlanRequiredChunks(focusE, focusS, 256000.0);
            Assert.That(plan.Count, Is.GreaterThan(20));
            Assert.That(plan[0], Is.EqualTo(near));

            int lastRing = 0;
            foreach (RegionChunkId id in plan)
            {
                Assert.That((int)id.Ring, Is.GreaterThanOrEqualTo(lastRing));
                lastRing = (int)id.Ring;
            }

            List<RegionChunkId> edge = planner.PlanDivePrefetch(256000.0, 256000.0, 256000.0);
            Assert.That(edge[0], Is.EqualTo(new RegionChunkId(RegionChunkRing.Near, 499, 499)));
            Assert.That(edge, Does.Contain(planner.ChunkWithinWorld(RegionChunkRing.Near, 256000.0, 256000.0, 256000.0)));
        }

        [Test]
        public void ChunkHeight_FollowsTheSameDiagonalAsItsCollider()
        {
            var terrain = new RegionData(new RegionSpec { Resolution = 2 });
            terrain.Height[0] = terrain.Height[3] = 10f;
            terrain.Height[1] = terrain.Height[2] = 14f;
            var chunk = new RegionChunkData
            {
                TerrainData = terrain,
                Resolution = 2,
                SizeMeters = 2f,
                CellMeters = 2f,
                MeshPart = new RegionLookPart
                {
                    Positions = new[] { 0f, 10f, 0f, 2f, 14f, 0f, 0f, 14f, 2f, 2f, 10f, 2f },
                    Indices = new[] { 0, 1, 3, 0, 3, 2 },
                },
            };
            Assert.That(chunk.HeightAt(1, 1), Is.EqualTo(10f));
            Assert.That(chunk.HeightAt(1.5, 0.5), Is.EqualTo(12f));
        }

        [Test]
        public void ChunkBuilder_AdjacentNearChunks_HaveSeamlessSharedEdgeAndFastBuild()
        {
            RegionData region = GetTestRegion();
            var planner = new RegionChunkPlanner();
            RegionChunkId leftId = planner.ChunkAt(RegionChunkRing.Near, 112000.0, 140000.0);
            var rightId = new RegionChunkId(RegionChunkRing.Near, leftId.X + 1, leftId.Z);

            var sw = Stopwatch.StartNew();
            RegionChunkData left = RegionChunkBuilder.Build(region, planner, leftId, renderAlbedo: true);
            sw.Stop();
            RegionChunkData right = RegionChunkBuilder.Build(region, planner, rightId, renderAlbedo: false);

            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(1500), "Single 512m chunk build should be fast");
            Assert.That(left.Resolution, Is.EqualTo(257));
            Assert.That(left.MeshPart.VertexCount, Is.EqualTo(257 * 257 + 4 * 257));

            int n = left.Resolution;
            float maxDiff = 0f;
            for (int y = 0; y < n; y++)
            {
                float hLeft = left.TerrainData.Height[(n - 1) + y * n];
                float hRight = right.TerrainData.Height[0 + y * n];
                float diff = Math.Abs(hLeft - hRight);
                if (diff > maxDiff) maxDiff = diff;
            }
            Assert.That(maxDiff, Is.LessThan(1e-3f), "Adjacent chunks must share identical border heights");
        }

        // ---- Phase 2: Dive Transition Curve ----

        [Test]
        public void DiveTransitionCurve_DescendsMonotonicallyAndPeaksCloudFogInCloudBand()
        {
            var curve = new DiveTransitionCurve
            {
                StartAglMeters = 16000f,
                TargetAglMeters = 5f,
                TopPitchDegrees = 89f,
                TargetPitchDegrees = 18f,
            };

            curve.Evaluate(0f, 100f, 0.7f, false, out float alt0, out float pitch0, out float alpha0, out float fog0);
            curve.Evaluate(1f, 100f, 0.7f, false, out float alt1, out float pitch1, out float alpha1, out float fog1);

            Assert.That(alt0, Is.EqualTo(16100f).Within(1f));
            Assert.That(alt1, Is.EqualTo(105f).Within(0.5f));
            Assert.That(pitch0, Is.EqualTo(89f).Within(0.1f));
            Assert.That(pitch1, Is.EqualTo(18f).Within(0.1f));
            Assert.That(alpha0, Is.EqualTo(1f).Within(1e-3f));
            Assert.That(alpha1, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(fog0, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(fog1, Is.EqualTo(0f).Within(1e-3f));

            float maxFog = 0f;
            float prevAlt = alt0 + 1f;
            for (int i = 1; i <= 40; i++)
            {
                curve.Evaluate(i / 40f, 100f, 0.7f, false, out float alt, out _, out _, out float fog);
                Assert.That(alt, Is.LessThan(prevAlt));
                prevAlt = alt;
                if (fog > maxFog) maxFog = fog;
            }
            Assert.That(maxFog, Is.GreaterThan(0.3f), "Cloud pass fog should peak when descending through cloud band");

            // Ascend is exact reverse
            curve.Evaluate(0f, 100f, 0.7f, true, out float ascAlt0, out float ascPitch0, out float ascAlpha0, out _);
            curve.Evaluate(1f, 100f, 0.7f, true, out float ascAlt1, out float ascPitch1, out float ascAlpha1, out _);
            Assert.That(ascAlt0, Is.EqualTo(alt1).Within(0.5f));
            Assert.That(ascAlt1, Is.EqualTo(alt0).Within(1f));
            Assert.That(ascAlpha0, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(ascAlpha1, Is.EqualTo(1f).Within(1e-3f));
        }

        // ---- Phase 3: Drone Autopilot, Terrain Avoidance, Orbit Loiter & Swappable Idle Behavior ----

        [Test]
        public void DroneAutopilot_AvoidsMountainRidgeAndContinuouslyOrbitsTarget()
        {
            var autopilot = new DroneAutopilot(new OrbitLoiterBehavior());
            double startE = 10000.0;
            double startS = 10000.0;
            double targetE = 11500.0;
            double targetS = 10000.0;

            // Steep 400m mountain ridge halfway between start and target
            Func<double, double, float> ridgeTerrain = (e, s) =>
            {
                double distToRidge = Math.Abs(e - 10750.0);
                float ridge = distToRidge < 250.0 ? (float)(400.0 * (1.0 - distToRidge / 250.0)) : 0f;
                return 100f + ridge;
            };

            autopilot.Dispatch(startE, startS, 150f, targetE, targetS);
            float minClearance = float.MaxValue;
            bool enteredLoiter = false;

            // Simulate 60 seconds at 20Hz
            for (int step = 0; step < 1200; step++)
            {
                autopilot.Step(0.05f, ridgeTerrain);
                DroneTelemetry t = autopilot.Telemetry;
                float clearance = t.AltitudeM - ridgeTerrain(t.EastM, t.SouthM);
                if (clearance < minClearance) minClearance = clearance;
                if (t.State == DroneFlightState.ArrivedLoitering) enteredLoiter = true;
            }

            Assert.IsTrue(enteredLoiter, "Drone should reach target and enter ArrivedLoitering");
            Assert.That(minClearance, Is.GreaterThanOrEqualTo(8f), "Drone must never collide with mountain ridge");

            // Verify continuous orbital motion (does not stop at target)
            double prevE = autopilot.Telemetry.EastM;
            double prevS = autopilot.Telemetry.SouthM;
            double totalTravel = 0.0;
            for (int step = 0; step < 200; step++) // 10 seconds of loitering
            {
                autopilot.Step(0.05f, ridgeTerrain);
                double de = autopilot.Telemetry.EastM - prevE;
                double ds = autopilot.Telemetry.SouthM - prevS;
                totalTravel += Math.Sqrt(de * de + ds * ds);
                prevE = autopilot.Telemetry.EastM;
                prevS = autopilot.Telemetry.SouthM;
            }
            Assert.That(totalTravel, Is.GreaterThan(150.0), "OrbitLoiterBehavior must keep moving continuously");
            Assert.That(autopilot.Telemetry.HorizontalSpeed, Is.GreaterThan(15f));
        }

        private sealed class CustomTestIdleBehavior : IDroneIdleBehavior
        {
            public string Id { get { return "custom-test"; } }
            public string DisplayName { get { return "Custom Test"; } }
            public int EnterCount { get; private set; }
            public int EvaluateCount { get; private set; }

            public void Enter(DroneTelemetry state, double targetEastM, double targetSouthM, DroneIdleSettings settings)
            {
                EnterCount++;
            }

            public DroneControlCommand Evaluate(DroneTelemetry state, float deltaTime, Func<double, double, float> sampleGroundHeight)
            {
                EvaluateCount++;
                return new DroneControlCommand
                {
                    DesiredVelEast = 12f,
                    DesiredVelSouth = 0f,
                    DesiredAltitudeM = sampleGroundHeight(state.EastM, state.SouthM) + 90f,
                    HasLookTarget = false,
                };
            }
        }

        [Test]
        public void DroneAutopilot_CanHotSwapIdleBehaviorAtRuntime()
        {
            var autopilot = new DroneAutopilot(new OrbitLoiterBehavior());
            Func<double, double, float> flat = (e, s) => 50f;
            autopilot.Dispatch(5000.0, 5000.0, 170f, 5100.0, 5000.0);

            for (int i = 0; i < 60; i++) autopilot.Step(0.05f, flat);
            Assert.That(autopilot.Telemetry.State, Is.EqualTo(DroneFlightState.ArrivedLoitering));
            Assert.That(autopilot.IdleBehavior.Id, Is.EqualTo("orbit"));

            // Swap to built-in FigureEightLoiterBehavior
            autopilot.SetIdleBehavior(new FigureEightLoiterBehavior());
            Assert.That(autopilot.IdleBehavior.Id, Is.EqualTo("figure8"));
            for (int i = 0; i < 40; i++) autopilot.Step(0.05f, flat);
            Assert.That(autopilot.Telemetry.HorizontalSpeed, Is.GreaterThan(5f));

            // Swap to custom IDroneIdleBehavior implementation
            var custom = new CustomTestIdleBehavior();
            autopilot.SetIdleBehavior(custom);
            Assert.That(custom.EnterCount, Is.EqualTo(1));
            for (int i = 0; i < 10; i++) autopilot.Step(0.05f, flat);
            Assert.That(custom.EvaluateCount, Is.EqualTo(10));
        }

        // ---- Phase 4: Locomotion Ground Rules & Nearest Standable ----

        [Test]
        public void LocomotionMath_ClassifiesSlopesWaterAndFindsStandableGround()
        {
            var spec = new RegionTraversalSpec();
            Assert.That(RegionLocomotionMath.ClassifyGround(0f, 0f, 0f, 0f, spec), Is.EqualTo(RegionGround.Water));
            Assert.That(RegionLocomotionMath.ClassifyGround(50f, 0f, 0f, 0.1f, spec), Is.EqualTo(RegionGround.Walk));
            Assert.That(RegionLocomotionMath.ClassifyGround(50f, 0f, 0f, 0.5f, spec), Is.EqualTo(RegionGround.Steep));
            Assert.That(RegionLocomotionMath.ClassifyGround(50f, 0f, 0f, 1.5f, spec), Is.EqualTo(RegionGround.Cliff));

            Assert.That(RegionLocomotionMath.SpeedMultiplier(RegionGround.Walk, true), Is.EqualTo(1f));
            Assert.That(RegionLocomotionMath.SpeedMultiplier(RegionGround.Cliff, true), Is.EqualTo(0f));
            Assert.That(RegionLocomotionMath.SpeedMultiplier(RegionGround.Water, false), Is.EqualTo(0f));

            // Center is water (r < 8m), surrounding is walkable
            bool found = RegionLocomotionMath.FindNearestStandable(1000.0, 1000.0, 2f, 20f, 3f,
                (e, s) =>
                {
                    double d = Math.Sqrt((e - 1000.0) * (e - 1000.0) + (s - 1000.0) * (s - 1000.0));
                    return d < 8.0 ? RegionGround.Water : RegionGround.Walk;
                },
                out double fe, out double fs);
            Assert.IsTrue(found);
            double dist = Math.Sqrt((fe - 1000.0) * (fe - 1000.0) + (fs - 1000.0) * (fs - 1000.0));
            Assert.That(dist, Is.GreaterThanOrEqualTo(8.0));
        }
    }
}
