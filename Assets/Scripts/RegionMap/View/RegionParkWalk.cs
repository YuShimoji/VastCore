using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Vastcore.RegionMap
{
    [DisallowMultipleComponent]
    public sealed class RegionParkWalk : MonoBehaviour
    {
        public const float EyeHeight = 1.64f;
        private const float WalkingSpeed = 2.25f;
        private const float MaximumSlope = 0.46f;

        [SerializeField] private Vector3[] groundPoints = Array.Empty<Vector3>();
        [SerializeField] private float[] distances = Array.Empty<float>();
        private float distance, yaw;
        private bool walking;

        public float Length => distances.Length == 0 ? 0f : distances[distances.Length - 1];
        public float Progress => Length > 0f ? distance / Length : 0f;

        public void StartWalking()
        {
            if (distance >= Length - 0.01f) ResetWalk();
            walking = true;
        }

        public static Vector3[] BuildRoute(RegionLookScene scene, RegionWoodlandLayout layout)
        {
            const float halfTrail = RegionWoodlandLayout.TrailLength * 0.5f;
            const float reviewExtent = 70f;
            int sections = Mathf.CeilToInt(RegionWoodlandLayout.TrailLength / RegionWoodlandLayout.TrailSectionSpacing);
            var heights = new float[RegionWoodlandLayout.TrailSectionColumns];
            var points = new List<Vector3>();
            int runStart = 0, bestStart = 0, bestCount = 0;
            for (int row = sections; row >= 0; row--)
            {
                float south = Mathf.Lerp(-halfTrail, halfTrail, row / (float)sections);
                if (south > reviewExtent) continue;
                if (south < -reviewExtent) break;
                float east = layout.TrailEast(south);
                if (!layout.TryTrailSection(scene, east, south, layout.TrailWidth(south), heights))
                {
                    runStart = points.Count;
                    continue;
                }

                Vector3 point = new Vector3(scene.CenterX + east, heights[heights.Length / 2], -scene.CenterZ - south);
                if (points.Count > runStart)
                {
                    Vector3 delta = point - points[points.Count - 1];
                    float horizontal = Mathf.Sqrt(delta.x * delta.x + delta.z * delta.z);
                    if (Mathf.Abs(delta.y) > horizontal * MaximumSlope) runStart = points.Count;
                }
                points.Add(point);
                int count = points.Count - runStart;
                if (count <= bestCount) continue;
                bestStart = runStart;
                bestCount = count;
            }
            return points.GetRange(bestStart, bestCount).ToArray();
        }

        public void Configure(Vector3[] points)
        {
            groundPoints = points;
            distances = new float[points.Length];
            for (int i = 1; i < points.Length; i++)
                distances[i] = distances[i - 1] + Vector3.Distance(points[i - 1], points[i]);
            ResetWalk();
        }

        public void ShowProgress(float progress)
        {
            if (Length > 0f) PlaceAtDistance(Mathf.Clamp01(progress) * Length);
        }

        private void OnEnable() => ResetWalk();

        private void ResetWalk()
        {
            walking = false;
            yaw = 0f;
            ShowProgress(0f);
        }

        private void Update()
        {
            Keyboard keys = Keyboard.current;
            if (Length <= 0f) return;
            if (keys != null && keys.homeKey.wasPressedThisFrame) ResetWalk();
            if (keys != null && keys.spaceKey.wasPressedThisFrame)
            {
                if (walking) walking = false;
                else StartWalking();
            }

            bool forward = keys != null && (keys.wKey.isPressed || keys.upArrowKey.isPressed);
            bool backward = keys != null && (keys.sKey.isPressed || keys.downArrowKey.isPressed);
            bool left = keys != null && (keys.aKey.isPressed || keys.leftArrowKey.isPressed);
            bool right = keys != null && (keys.dKey.isPressed || keys.rightArrowKey.isPressed);
            if (forward || backward) walking = false;
            float move = walking || forward ? 1f : 0f;
            if (backward) move -= 1f;
            int turn = (right ? 1 : 0) - (left ? 1 : 0);
            if (move == 0f && turn == 0) return;
            yaw = Mathf.Clamp(yaw + turn * 65f * Time.deltaTime, -74f, 74f);
            PlaceAtDistance(distance + move * WalkingSpeed * Time.deltaTime);
            if (distance >= Length) walking = false;
        }

        private void PlaceAtDistance(float metres)
        {
            distance = Mathf.Clamp(metres, 0f, Length);
            Vector3 ground = GroundAt(distance);
            Vector3 tangent = GroundAt(Mathf.Min(distance + 5f, Length)) - GroundAt(Mathf.Max(distance - 5f, 0f));
            tangent.y = tangent.y * 0.63f - 0.09f * new Vector2(tangent.x, tangent.z).magnitude;
            Quaternion gaze = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.LookRotation(tangent);
            transform.SetPositionAndRotation(ground + Vector3.up * EyeHeight, gaze);
        }

        private Vector3 GroundAt(float metres)
        {
            int hit = Array.BinarySearch(distances, metres);
            if (hit >= 0) return groundPoints[hit];
            int after = ~hit;
            float blend = Mathf.InverseLerp(distances[after - 1], distances[after], metres);
            return Vector3.Lerp(groundPoints[after - 1], groundPoints[after], blend);
        }
    }
}
