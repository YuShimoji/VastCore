using System;
using System.IO;
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
        private static double started;
        private static bool crossedDoor;
        private static Quaternion initialYaw;

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
                pad = InputSystem.AddDevice<Gamepad>();
                started = EditorApplication.timeSinceStartup;
                crossedDoor = false;
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
                if (EditorApplication.timeSinceStartup - started > 18)
                {
                    Finish($"Timed out with the player at cafe coordinates {localFoot:F3}.", 1);
                    return;
                }
                if (!crossedDoor && localFoot.z > 0.79f)
                {
                    if (Mathf.Abs(localFoot.y) > 0.17f)
                        throw new InvalidOperationException("The sample player did not remain on the entrance floor.");
                    crossedDoor = true;
                    initialYaw = walker.rotation;
                }
                if (crossedDoor && Quaternion.Angle(initialYaw, walker.rotation) > 26
                    && Quaternion.Angle(Quaternion.identity, camera.transform.localRotation) > 5)
                {
                    string output = SessionState.GetString(OutputKey, "");
                    CityReview.SaveFrame(camera, Path.Combine(output, "play-cafe.png"));
                    Finish($"Play mode: saved scene opened from menu. Gamepad crossed the cafe entrance "
                        + $"(foot {localFoot:F3}) and rotated yaw and pitch.", 0);
                    return;
                }
                InputSystem.QueueStateEvent(pad, new GamepadState
                {
                    leftStick = crossedDoor ? Vector2.zero : Vector2.up,
                    rightStick = crossedDoor ? new Vector2(-0.75f, 0.22f) : Vector2.zero
                });
            }
            catch (Exception error)
            {
                Finish(error.ToString(), 1);
            }
        }

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
