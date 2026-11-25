using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Vastcore.RegionMap.Editor
{
    // Opens the actual editor menu, enters Play, walks the loop with the production
    // controller, and captures its eye camera before and after both crossings.
    [InitializeOnLoad]
    public static class RegionCaveWalkSmoke
    {
        private const string Pending = "Vastcore.CaveWalkSmoke.Pending";
        static RegionCaveWalkSmoke()
        {
            EditorApplication.update += CheckPlay;
        }

        public static void Run()
        {
            if (!EditorApplication.ExecuteMenuItem("Vastcore/Region Map/Open 3D Cliff And Cave Prototype"))
                throw new InvalidOperationException("Could not open the cliff and cave prototype");
            SessionState.SetBool(Pending, true);
            EditorApplication.EnterPlaymode();
        }

        private static void CheckPlay()
        {
            if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying || Time.frameCount < 4) return;
            SessionState.EraseBool(Pending);
            try
            {
                var walk = UnityEngine.Object.FindFirstObjectByType<RegionCaveWalk>();
                if (!walk) throw new InvalidOperationException("No tunnel controller in the menu scene");
                string output = Path.GetFullPath(RegionMapMenu.Argument("-regionOut", "artifacts/region-map/cave-walk"));
                Directory.CreateDirectory(output);
                ShaderUtil.allowAsyncCompilation = false;
                var camera = walk.GetComponent<Camera>();
                var exterior = SceneManager.GetActiveScene();
                Vector3 start = walk.Feet;
                Shot(camera, output, "01-mouth", camera.transform.forward);
                Approach(walk);
                if (System.Array.IndexOf(Environment.GetCommandLineArgs(), "-caveKeyboardCheck") >= 0)
                {
                    Debug.Log("[CaveWalk] Ready for keyboard entry in Game view");
                    return;
                }
                walk.UseDoor();
                if (!walk.Inside || SceneManager.GetActiveScene() == exterior)
                    throw new InvalidOperationException("Failed to enter a separate scene");
                var firstInterior = SceneManager.GetActiveScene();
                WalkTo(walk, 0, -10, 0);
                Shot(camera, output, "02-lower", new Vector3(0.14f, 0.23f, 1));
                WalkTo(walk, 0, 2.2f, 0);
                Vector3 below = walk.Feet;
                if (!Physics.Raycast(below + Vector3.up * 2, Vector3.up, out var underside, 5))
                    throw new InvalidOperationException("No stone span above the lower crossing");
                Shot(camera, output, "03-under-span", new Vector3(0.6f, 0.45f, 1));
                WalkTo(walk, 2, 12, 0);
                WalkTo(walk, 7, 14, 0.9f);
                WalkTo(walk, 11.3f, 11, 2.7f);
                WalkTo(walk, 12, 5, 4.9f);
                WalkTo(walk, 9.4f, 1.7f, 6.25f);
                WalkTo(walk, 4.5f, 1.4f, 6.6f);
                WalkTo(walk, 0, 2.2f, 6.8f);
                Vector3 above = walk.Feet;
                Shot(camera, output, "04-upper", new Vector3(-0.5f, -0.47f, -1));
                WalkTo(walk, -6, 2.1f, 6.45f);
                WalkTo(walk, -10.7f, 0.5f, 5.9f);
                WalkTo(walk, -11.8f, -4, 4.45f);
                WalkTo(walk, -9, -8, 2.6f);
                WalkTo(walk, -5, -11, 0.85f);
                WalkTo(walk, -2, -13, 0);
                WalkTo(walk, 0, -22, 0);
                walk.UseDoor();
                if (walk.Inside || SceneManager.GetActiveScene() != exterior || Vector3.Distance(walk.Feet,start) > 0.5f)
                    throw new InvalidOperationException("Failed to return to the same exterior approach");
                Shot(camera, output, "05-return", camera.transform.forward);
                Approach(walk);
                walk.UseDoor();
                if (!walk.Inside || SceneManager.GetActiveScene() != firstInterior)
                    throw new InvalidOperationException("Failed to re-enter the same interior");
                Debug.Log($"[CaveWalk] PASS start {start}, under {below}, underside {underside.point}, above {above}, same door twice.");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        private static void Approach(RegionCaveWalk walk)
        {
            Vector3 forward = walk.transform.parent.forward;
            for (int i = 0; i < 550 && !walk.CanUseDoor; i++)
                walk.Walk(forward, 1f / 60);
            if (!walk.CanUseDoor) throw new InvalidOperationException("Could not walk to exterior door at " + walk.Feet);
        }

        private static void WalkTo(RegionCaveWalk walk, float x, float z, float floor)
        {
            Vector3 goal = new Vector3(x, floor, z);
            for (int i = 0; i < 650; i++)
            {
                Vector3 direction = Vector3.ProjectOnPlane(goal - walk.Feet, Vector3.up);
                if (direction.magnitude < 0.14f && Mathf.Abs(walk.Feet.y - floor) < 0.85f) return;
                walk.Walk(direction.normalized, 1f / 60);
            }
            throw new InvalidOperationException($"Walk stopped at {walk.Feet} on the way to {goal}");
        }

        private static void Shot(Camera camera, string output, string name, Vector3 direction)
        {
            Quaternion previous = camera.transform.rotation;
            camera.transform.rotation = Quaternion.LookRotation(direction);
            try { RegionTerrainViewMenu.Capture(camera, output, name + ".png"); }
            finally { camera.transform.rotation = previous; }
        }
    }
}
