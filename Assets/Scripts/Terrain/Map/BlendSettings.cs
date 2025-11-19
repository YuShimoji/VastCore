using UnityEngine;

namespace Vastcore.Terrain.Map
{
    public enum BlendMode
    {
        Additive,
        Multiplicative,
    }

    public class BlendSettings : ScriptableObject
    {
        public BlendMode blendMode = BlendMode.Additive;
        [Range(0f, 1f)] public float blendStrength = 1f;
        public float fadeDistance = 100f;
        public bool enableEdgeBlending = true;
        public float edgeBlendWidth = 10f;
    }
}
