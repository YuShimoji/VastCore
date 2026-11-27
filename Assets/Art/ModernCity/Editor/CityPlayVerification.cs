using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Vastcore.ModernCity.Editor
{
    // Run with -batchmode (without -quit). Reopen the saved scene and feed its real player input.
    public static class CityPlayVerification
    {
        private const string OutputKey = "Vastcore.ModernCity.PlayCheckOutput";
        private const string ResultKey = "Vastcore.ModernCity.PlayCheckExitCode";
        private const string RouteKey = "Vastcore.ModernCity.PlayCheckRoute";
        private static Gamepad pad;
        private static Camera camera;
        private static Transform walker;
        private static Transform building;
        private static string routeName;
        private static List<Vector3> route;
        private static int waypoint;
        private static int upperFloorCheckpoint;
        private static double started;

        public static void RunFromCommandLine()
        {
            CityReview.OpenSavedSample();
            var args = Environment.GetCommandLineArgs();
            int option = Array.IndexOf(args, "-cityWalkRoute");
            if (option == args.Length - 1)
                throw new ArgumentException("Provide a route after -cityWalkRoute.");
            string check = option < 0 ? "cafe" : args[option + 1];
            var (plot, waypoints, _) = BuildRoute(check);
            if (check != "cafe")
            {
                var player = Camera.main.transform.parent;
                player.SetPositionAndRotation(waypoints[0] + Vector3.up * 0.035f,
                    Quaternion.Euler(0, plot.Yaw + (check == "service" ? 180 : 0), 0));
            }
            SessionState.SetString(OutputKey, CityReview.OutputDirectory());
            SessionState.SetString(RouteKey, check);
            SessionState.SetInt(ResultKey, 1);
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void Listen()
        {
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            EditorApplication.playModeStateChanged += PlayModeChanged;
        }

        private static void PlayModeChanged(PlayModeStateChange state)
        {
            if (string.IsNullOrEmpty(SessionState.GetString(OutputKey, "")))
                return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                camera = Camera.main;
                routeName = SessionState.GetString(RouteKey, "cafe");
                var plan = BuildRoute(routeName);
                building = GameObject.Find(plan.Plot.Name)?.transform;
                walker = camera != null ? camera.transform.parent : null;
                if (building == null || walker == null || walker.GetComponent<CityWalkthrough>() == null)
                {
                    Finish("Saved scene is missing the route building or the first-person player.", 1);
                    return;
                }
                Application.runInBackground = true;
                route = plan.Waypoints;
                upperFloorCheckpoint = plan.UpperCheckpoint;
                waypoint = 0;
                pad = InputSystem.AddDevice<Gamepad>();
                started = EditorApplication.timeSinceStartup;
                EditorApplication.update += DrivePlayer;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.EraseString(OutputKey);
                SessionState.EraseString(RouteKey);
                EditorApplication.Exit(SessionState.GetInt(ResultKey, 1));
            }
        }

        private static (CityPlot Plot, List<Vector3> Waypoints, int UpperCheckpoint) BuildRoute(string check)
        {
            var plots = CityBlock.Design(CityBlock.DefaultSeed, CityStreetPlan.CornerShops);
            var plot = check switch
            {
                "cafe" => plots[0],
                "office" or "service" => plots[2],
                _ => throw new ArgumentException("Provide -cityWalkRoute cafe, office or service.")
            };
            var stairs = CityVerification.MainRoute(plot, maxClimbs: 1);
            var waypoints = new List<Vector3>();
            if (check == "cafe")
            {
                var order = CityVerification.OrderRoute(plot);
                waypoints.AddRange(order);
                waypoints.Add(order[2]);
                waypoints.Add(order[1]);
            }
            else if (check == "service")
            {
                var entrance = CityVerification.ServiceRoute(plot);
                entrance.Reverse();
                waypoints.AddRange(entrance);
            }
            else
                waypoints.AddRange(stairs.Take(2));
            waypoints.AddRange(stairs.Skip(2));
            int checkpoint = waypoints.Count - 1;
            stairs.Reverse();
            waypoints.AddRange(stairs.Skip(1).Take(stairs.Count - (check == "service" ? 2 : 1)));
            if (check == "service")
                waypoints.AddRange(CityVerification.ServiceRoute(plot).Skip(1));
            return (plot, waypoints, checkpoint);
        }

        private static void DrivePlayer()
        {
            try
            {
                Vector3 localFoot = building.InverseTransformPoint(walker.position);
                if (EditorApplication.timeSinceStartup - started > 65)
                {
                    Finish($"Timed out at waypoint {waypoint + 1}/{route.Count}. "
                        + $"Foot {localFoot:F3}; target {building.InverseTransformPoint(route[waypoint]):F3}.", 1);
                    return;
                }
                Vector3 goal = route[waypoint];
                Vector3 remaining = goal - walker.position;
                var flat = new Vector3(remaining.x, 0, remaining.z);
                if (flat.magnitude < 0.115f && Mathf.Abs(remaining.y) < 0.22f)
                {
                    if (waypoint == (routeName == "service" ? 2 : 1))
                        CaptureCheckpoint("play-" + routeName + ".png");
                    if (waypoint == upperFloorCheckpoint)
                        CaptureCheckpoint("play-upper.png");
                    waypoint++;
                    if (waypoint == route.Count)
                    {
                        CaptureCheckpoint("play-return.png");
                        string entry = routeName == "cafe" ? "Cafe: reached the order counter."
                            : routeName == "service" ? "Office: entered through the loading entrance."
                            : "Office: entered through the reception entrance.";
                        Finish($"Play mode: real gamepad input from the saved scene. {entry} "
                            + $"Walked both stair flights to +{CityBuildingKit.Storey:F1} m and returned outside "
                            + $"(foot {localFoot:F3}; {route.Count} waypoints).", 0);
                    }
                    else
                    {
                        InputSystem.QueueStateEvent(pad, new GamepadState());
                    }
                    return;
                }

                Vector3 local = walker.InverseTransformDirection(flat);
                float speed = Mathf.Clamp(flat.magnitude * 2.35f, 0.34f, 0.92f);
                float yaw = Vector3.SignedAngle(walker.forward, flat, Vector3.up);
                InputSystem.QueueStateEvent(pad, new GamepadState
                {
                    leftStick = new Vector2(local.x, local.z).normalized * speed,
                    rightStick = new Vector2(Mathf.Clamp(yaw / 43f, -0.68f, 0.68f), 0)
                });
            }
            catch (Exception error)
            {
                Finish(error.ToString(), 1);
            }
        }

        private static void CaptureCheckpoint(string name) =>
            CityReview.SaveFrame(camera, Path.Combine(SessionState.GetString(OutputKey, ""), name));

        private static void Finish(string message, int exitCode)
        {
            EditorApplication.update -= DrivePlayer;
            if (pad != null)
                InputSystem.RemoveDevice(pad);
            try
            {
                File.WriteAllText(Path.Combine(SessionState.GetString(OutputKey, ""), "play-check.txt"), message);
            }
            finally
            {
                SessionState.SetInt(ResultKey, exitCode);
                EditorApplication.ExitPlaymode();
            }
        }
    }
}
