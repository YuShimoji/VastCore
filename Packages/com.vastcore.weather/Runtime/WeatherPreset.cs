using System;
using UnityEngine;

namespace Vastcore.Weather
{
    public enum WeatherShape
    {
        Tower = 0,
        Anvil = 1,
        RollCloud = 2,
        RotatingUpdraft = 3,
        ValleyFog = 4,
        RainCurtain = 5,
        Lenticular = 6
    }

    [Serializable]
    public sealed class WeatherVolume
    {
        public WeatherShape Shape;
        public Vector3 CenterMeters = new Vector3(0f, 2000f, 4000f);
        public Vector3 SizeMeters = new Vector3(3000f, 4000f, 3000f);
        public Vector3 DriftMetersPerSecond;
        public float YawDegrees;
        [Min(0f)] public float ExtinctionPerKilometer = 2.5f;
        [Min(1f)] public float DetailSizeMeters = 450f;
        public float FlowRadiansPerSecond;
        public Color SunlitColor = new Color(1f, 0.97f, 0.9f);
        public Color ShadeColor = new Color(0.44f, 0.53f, 0.65f);
    }

    [Serializable]
    public sealed class WeatherRainCell
    {
        [Range(0f, 1f)] public float Intensity;
        public Vector3 CenterAtGroundMeters;
        public Vector2 RadiusMeters = new Vector2(300f, 400f);
        public Vector3 DriftMetersPerSecond;
        public Vector2 WindMetersPerSecond = new Vector2(2f, 0f);
        [Range(0f, 1f)] public float DropVisibility = 1f;
        [Min(1f)] public float CloudBaseMeters = 1000f;
        [Min(0f)] public float StartSeconds;
        [Min(0f)] public float FadeInSeconds = 5f;
        [Tooltip("Zero means the cell has no timed end.")] [Min(0f)] public float EndSeconds;
        [Min(0f)] public float FadeOutSeconds = 8f;
        [Min(1f)] public float DryOutSeconds = 120f;
        [Tooltip("Zero disables lightning.")] [Min(0f)] public float LightningIntervalSeconds;
        [Min(0f)] public float CurtainExtinctionPerKilometer = 1f;
        public Color CurtainColor = new Color(0.49f, 0.57f, 0.64f);

        internal Vector2 SafeRadius => Vector2.Max(Vector2.one, RadiusMeters);

        public Vector3 CenterAt(float weatherSeconds) =>
            CenterAtGroundMeters + weatherSeconds * DriftMetersPerSecond;

        internal float IntensityAt(float seconds)
        {
            if (seconds < StartSeconds || (EndSeconds > 0f && seconds >= EndSeconds))
                return 0f;

            float incoming = FadeInSeconds > 0f ? (seconds - StartSeconds) / FadeInSeconds : 1f;
            float outgoing = EndSeconds > 0f && FadeOutSeconds > 0f
                ? (EndSeconds - seconds) / FadeOutSeconds
                : 1f;
            return Mathf.Clamp01(Intensity) * Mathf.SmoothStep(0f, 1f, Mathf.Min(incoming, outgoing));
        }

        internal float RainAt(Vector3 fieldPosition, float seconds)
        {
            Vector3 delta = fieldPosition - CenterAt(seconds);
            Vector2 radius = SafeRadius;
            var relative = new Vector2(delta.x / radius.x, delta.z / radius.y);
            float edge = Mathf.InverseLerp(0.42f, 1f, relative.magnitude);
            return IntensityAt(seconds) * (1f - Mathf.SmoothStep(0f, 1f, edge));
        }
    }

    [CreateAssetMenu(menuName = "Vastcore/Weather Preset", fileName = "WeatherPreset")]
    public sealed class WeatherPreset : ScriptableObject
    {
        public WeatherVolume[] Volumes = Array.Empty<WeatherVolume>();
        public WeatherRainCell Rain = new WeatherRainCell();
        public Color AerialHaze = new Color(0.65f, 0.72f, 0.77f);
        [Min(0f)] public float HazePerKilometer = 0.015f;
        [Range(16, 64)] public int VolumeSteps = 32;
    }
}
