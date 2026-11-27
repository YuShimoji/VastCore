using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Vastcore.ModernCity.Editor
{
    internal static class CityVerification
    {
        internal static string Walk(Scene scene, CityPlot[] plots)
        {
            Physics.SyncTransforms();
            var walker = new GameObject("Capsule for city route verification") { hideFlags = HideFlags.HideAndDontSave };
            walker.transform.position = Vector3.up * 100;
            var capsule = CityBlock.AddWalkerCollider(walker);
            int verticalConnections = 0;
            try
            {
                foreach (var plot in plots)
                {
                    var main = MainRoute(plot);
                    WalkRoute(capsule, plot.Name + " front entrance and stairs", main);
                    main.Reverse();
                    WalkRoute(capsule, plot.Name + " stairs and front exit", main);
                    var service = new List<Vector3>
                    {
                        plot.World(plot.EntranceX, 0, 3.02f),
                        plot.World(2, 0, 3.02f),
                        plot.World(2, 0, plot.Length - 0.66f),
                        plot.World(2, 0, plot.Length + 1.44f)
                    };
                    WalkRoute(capsule, plot.Name + " service exit", service);
                    if (!Physics.Raycast(plot.World(2, 1.5f, -0.42f),
                            (plot.World(2, 1.5f, 1) - plot.World(2, 1.5f, 0)).normalized, 0.7f))
                        throw new InvalidOperationException("Missing window collision at " + plot.Name);
                    verticalConnections += plot.Floors - 1;
                }
                return $"{scene.name}: {plots.Length} buildings, {verticalConnections} stair connections. "
                    + "Front doors, upper floors, return routes, service exits and window collisions pass.";
            }
            finally
            {
                Object.DestroyImmediate(walker);
            }
        }

        private static List<Vector3> MainRoute(CityPlot plot)
        {
            var waypoints = new List<Vector3>();
            void At(float x, float y, float z) => waypoints.Add(plot.World(x, y, z));
            float leftFlight = plot.StairX - CityBuildingKit.FlightOffset;
            float rightFlight = plot.StairX + CityBuildingKit.FlightOffset;
            At(plot.EntranceX, 0, -2.0f);
            At(plot.EntranceX, 0, 3.0f);
            At(leftFlight, 0, 3.0f);
            At(leftFlight, 0, plot.StairZ - 0.7f);
            for (int f = 0; f < plot.Floors - 1; f++)
            {
                float y = f * CityBuildingKit.Storey;
                float halfStorey = y + CityBuildingKit.Storey * 0.5f;
                float nextStorey = y + CityBuildingKit.Storey;
                At(leftFlight, y, plot.StairZ);
                At(leftFlight, halfStorey, plot.StairZ + CityBuildingKit.StairLanding);
                At(leftFlight, halfStorey, plot.StairZ + 3.31f);
                At(rightFlight, halfStorey, plot.StairZ + 3.31f);
                At(rightFlight, halfStorey, plot.StairZ + CityBuildingKit.StairLanding);
                At(rightFlight, nextStorey, plot.StairZ);
                At(rightFlight, nextStorey, plot.StairZ - 0.7f);
                if (f < plot.Floors - 2)
                    At(leftFlight, nextStorey, plot.StairZ - 0.7f);
            }
            float floor = (plot.Floors - 1) * CityBuildingKit.Storey;
            At(plot.Floors > 1 ? rightFlight : leftFlight, floor, 3f);
            At(plot.EntranceX, floor, 3f);
            return waypoints;
        }

        private static void WalkRoute(CharacterController capsule, string name, List<Vector3> route)
        {
            capsule.enabled = false;
            capsule.transform.position = route[0] + Vector3.up * 0.045f;
            capsule.enabled = true;
            Physics.SyncTransforms();
            capsule.Move(Vector3.down * 0.08f);
            for (int index = 1; index < route.Count; index++)
            {
                Vector3 start = route[index - 1];
                Vector3 goal = route[index];
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(start, goal) / 0.075f));
                var motion = (goal - start) / steps;
                // Negative vertical input keeps the controller on ramps. The actual collision surfaces supply the rise.
                motion.y = -0.08f;
                for (int step = 0; step < steps; step++)
                    capsule.Move(motion);
                Vector3 actual = capsule.transform.position;
                Vector2 miss = new Vector2(actual.x - goal.x, actual.z - goal.z);
                if (miss.magnitude > 0.12f || Mathf.Abs(actual.y - goal.y) > 0.21f)
                {
                    throw new InvalidOperationException($"{name}, waypoint {index}. "
                        + $"Expected {goal:F3}; capsule reached {actual:F3}.");
                }
            }
        }

        internal static Hash128 Signature(Transform root)
        {
            var description = new StringBuilder();
            foreach (var item in root.GetComponentsInChildren<Transform>())
            {
                if (item == root)
                    continue;
                var culture = CultureInfo.InvariantCulture;
                description.Append(item.name).Append('|')
                    .Append(item.localPosition.ToString("F4", culture)).Append('|')
                    .Append(item.localRotation.ToString("F4", culture)).Append('\n');
            }
            return Hash128.Compute(description.ToString());
        }
    }
}
