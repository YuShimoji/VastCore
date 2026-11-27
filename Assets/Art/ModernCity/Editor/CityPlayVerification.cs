using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
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
        private static Gamepad pad;
        private static Camera camera;
        private static Transform walker;
        private static Transform cafe;
        private static List<Vector3> route;
        private static int waypoint;
        private static int upperFloorCheckpoint;
        private static double started;

        public static void RunFromCommandLine()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!EditorApplication.ExecuteMenuItem("Vastcore/Modern City/Open Sample"))
                throw new InvalidOperationException("The Modern City menu did not open the sample.");
            SessionState.SetString(OutputKey, CityReview.OutputDirectory());
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
                cafe = GameObject.Find("Corner cafe")?.transform;
                walker = camera != null ? camera.transform.parent : null;
                if (cafe == null || walker == null || walker.GetComponent<CityWalkthrough>() == null)
                {
                    Finish("Saved scene is missing the cafe or the first-person player.", 1);
                    return;
                }
                Application.runInBackground = true;
                var plot = CityBlock.Design(CityBlock.DefaultSeed, CityStreetPlan.CornerShops)[0];
                var stairs = CityVerification.MainRoute(plot, maxClimbs: 1);
                var order = CityVerification.OrderRoute(plot);
                route = new List<Vector3>(order);
                route.Add(order[2]);
                route.Add(order[1]);
                route.AddRange(stairs.Skip(2));
                upperFloorCheckpoint = route.Count - 1;
                stairs.Reverse();
                route.AddRange(stairs.Skip(1));
                waypoint = 0;
                pad = InputSystem.AddDevice<Gamepad>();
                started = EditorApplication.timeSinceStartup;
                EditorApplication.update += DrivePlayer;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.EraseString(OutputKey);
                EditorApplication.Exit(SessionState.GetInt(ResultKey, 1));
            }
        }

        private static void DrivePlayer()
        {
            try
            {
                Vector3 localFoot = cafe.InverseTransformPoint(walker.position);
                if (EditorApplication.timeSinceStartup - started > 65)
                {
                    Finish($"Timed out at waypoint {waypoint + 1}/{route.Count}. "
                        + $"Foot {localFoot:F3}; target {cafe.InverseTransformPoint(route[waypoint]):F3}.", 1);
                    return;
                }
                Vector3 goal = route[waypoint];
                Vector3 remaining = goal - walker.position;
                var flat = new Vector3(remaining.x, 0, remaining.z);
                if (flat.magnitude < 0.115f && Mathf.Abs(remaining.y) < 0.22f)
                {
                    if (waypoint == 1)
                        CaptureCheckpoint("play-cafe.png");
                    if (waypoint == upperFloorCheckpoint)
                        CaptureCheckpoint("play-upper.png");
                    waypoint++;
                    if (waypoint == route.Count)
                    {
                        CaptureCheckpoint("play-return.png");
                        Finish($"Play mode: real gamepad input from the saved scene. Reached the order counter, "
                            + $"walked both stair flights to +{CityBuildingKit.Storey:F1} m, and returned to the sidewalk "
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
