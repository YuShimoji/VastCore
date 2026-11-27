using System;
using UnityEngine;

namespace Vastcore.Water
{
    public enum BankType { Beach, Barrier }

    /// <summary>An elliptical obstacle in shore coordinates (distance inland, then alongshore), in metres.</summary>
    public readonly struct SwashObstacle
    {
        public readonly Vector2 Centre;
        public readonly Vector2 Radii;

        public SwashObstacle(Vector2 centre, Vector2 radii)
        {
            Centre = centre;
            Radii = radii;
        }
    }

    /// <summary>
    /// Coordinates are local metres. X increases inland and Z increases alongshore.
    /// The curve moves the waterline in X without changing the cross section.
    /// </summary>
    [Serializable]
    public sealed class Shoreline
    {
        public BankType Bank;
        public float SeaLevel;
        public float StartZ = -42f;
        public float EndZ = 640f;
        public AnimationCurve Offset = AnimationCurve.Linear(-42f, 0f, 640f, 0f);

        [Min(0.012f)] public float BeachSlope = 0.085f;
        [Range(1f, 16f)] public float BreakerDistance = 6f;
        [Min(0.1f)] public float CrestHeight = 1.8f;
        [Min(0.1f)] public float BarrierWidth = 0.62f;
        public float ReceivingHeight = 1.12f;
        [Min(0.5f)] public float ReceivingWidth = 8f;
        [Range(0f, 1f), Tooltip("Amount of whitewater on top of the film.")]
        public float FilmFoam = 0.48f;
        [Range(0f, 12f), Tooltip("Seconds a receiving surface stays visibly wet after the wave recedes.")]
        public float WetHoldSeconds = 3.4f;
        [Range(0f, 1f), Tooltip("How strongly beach swash diverts around supplied rock footprints.")]
        public float SwashDeflection = 0.9f;
        public Color BedTint = new Color(0.33f, 0.27f, 0.21f);

        public float X(float distance, float alongshore) => (Offset?.Evaluate(alongshore) ?? 0f) + distance;

        // These are the surfaces that receive the animated film. The environment supplies land beyond them.
        public float ReceivingY(float distance) =>
            SeaLevel + (Bank == BankType.Beach ? Mathf.Max(0.012f, BeachSlope) * distance : ReceivingHeight);

        public float ContactX(float alongshore) =>
            X(Bank == BankType.Beach ? -SafeBreakerDistance : 0f, alongshore);

        internal float SafeBreakerDistance => Mathf.Clamp(BreakerDistance, 1f, 16f);

        // Matches the phase used by WaterWaves.hlsl at the contact point.
        public float CrestArrival(SeaState state, float alongshore)
        {
            Vector2 position = new Vector2(ContactX(alongshore), alongshore);
            float variation = 0.31f * Mathf.Sin(alongshore * 0.136f) + 0.16f * Mathf.Sin(alongshore * 0.43f + 1.5f);
            return (state.WaveNumber * Vector2.Dot(state.Direction, position) + variation)
                * state.SafePeriod / (2f * Mathf.PI);
        }

        public float Overtopping(SeaState state, float alongshore)
        {
            if (Bank != BankType.Barrier) return 0f;
            float amplitude = state.SafeAmplitude;
            float group = 1.16f + 0.34f * Mathf.Sin(alongshore * 0.145f) + 0.12f * Mathf.Sin(alongshore * 0.51f + 1.1f);
            return Mathf.Clamp01((amplitude * group - Mathf.Max(0.1f, CrestHeight)) / (0.38f + amplitude * 0.34f));
        }
    }
}
