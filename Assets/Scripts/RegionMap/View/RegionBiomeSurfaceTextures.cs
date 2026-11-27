using System;
using UnityEngine;

namespace Vastcore.RegionMap
{
    /// <summary>非砂岩の地表が Editor と Player で共通に使う既存 PBR 素材。</summary>
    public sealed class RegionBiomeSurfaceTextures : ScriptableObject
    {
        public const string Resource = "RegionMap3D/BiomeSurfaceTextures";

        [Serializable]
        public struct TextureSet
        {
            public Texture2D Color;
            public Texture2D Normal;
            public Texture2D Roughness;

            public bool IsComplete => Color != null && Normal != null && Roughness != null;
        }

        public TextureSet Turf;
        public TextureSet Loam;
        public TextureSet Cliff;
    }
}
