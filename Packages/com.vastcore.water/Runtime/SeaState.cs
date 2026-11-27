using UnityEngine;

namespace Vastcore.Water
{
    [CreateAssetMenu(menuName = "Vastcore/Water/Sea State")]
    public sealed class SeaState : ScriptableObject
    {
        [Range(0f, 4f), Tooltip("Wave amplitude in metres (half the crest-to-trough height).")]
        public float Amplitude = 0.9f;
        [Min(1f)] public float Period = 6.4f;
        [Min(4f)] public float Wavelength = 33f;
        [Range(-70f, 70f), Tooltip("Degrees from the landward axis toward the alongshore axis.")]
        public float Incidence = 16f;
        [Range(0f, 0.85f)] public float Steepness = 0.64f;
        [Range(0f, 1f)] public float Chop = 0.62f;
        [Range(0f, 1f)] public float Foam = 0.72f;
        public Color DeepWater = new Color(0.10f, 0.24f, 0.28f);
        public Color ShallowWater = new Color(0.24f, 0.44f, 0.41f);
        public Texture2D RippleNormal;
        public Texture2D FoamMask;

        public Vector2 Direction
        {
            get
            {
                float angle = Mathf.Clamp(Incidence, -70f, 70f) * Mathf.Deg2Rad;
                return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
        }

        internal float SafePeriod => Mathf.Max(1f, Period);
        internal float WaveNumber => 2f * Mathf.PI / Mathf.Max(4f, Wavelength);
        internal float SafeAmplitude => Mathf.Clamp(Amplitude, 0f, 4f);
    }
}
