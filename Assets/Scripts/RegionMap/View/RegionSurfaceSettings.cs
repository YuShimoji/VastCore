using System;
using UnityEngine;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 表面の写真素材の、役ごとの実寸と強さ。<c>Assets/Resources/RegionMap3D/Surfaces/SurfaceSettings.asset</c> に置くと使われ、
    /// 無ければ既定の値（Defaults）で描く。並びは RegionSurfaceRole と同じ。
    /// </summary>
    [CreateAssetMenu(fileName = "SurfaceSettings", menuName = "Vastcore/Region Map/Surface Settings")]
    public sealed class RegionSurfaceSettings : ScriptableObject
    {
        /// <summary>1 つの役の値。</summary>
        [Serializable]
        public struct Entry
        {
            /// <summary>画像の横 1 枚が覆う長さ（m）。縦は画像の縦横の比で決まる。</summary>
            public float TileMetres;
            /// <summary>高さの画像の 0〜1 の幅、色から作る高さの幅（m）。法線を色や高さから作る時の強さになる。</summary>
            public float HeightMetres;
            /// <summary>法線の傾きに掛ける強さ（1 で画像のまま）。</summary>
            public float Strength;
            /// <summary>粗さの画像が無い時の粗さ。</summary>
            public float Roughness;
            /// <summary>
            /// 写真の平均の色（Linear）。取り込み時の処理（RegionSurfaceImport）が色の画像から書く。a が 0 の役は色を混ぜない
            /// （RegionSurfaceMaterials.PhotoColor、設計 3.1 節）。
            /// </summary>
            public Color Tint;
        }

        /// <summary>役ごとの値（RegionSurfaceRole の並び）。</summary>
        public Entry[] Roles = Defaults();

        /// <summary>高さによる層の混合の境の深さ（m）。0 で切。大きいほど境がぼける（設計 6.2 節）。</summary>
        public float BlendDepthMetres = DefaultBlendDepth;

        /// <summary>層の重み 1 を何 m の高さとみなすか（高さの画像の幅 HeightMetres と比べる単位）。</summary>
        public float BlendSpanMetres = DefaultBlendSpan;

        /// <summary>BlendDepthMetres・BlendSpanMetres の既定。</summary>
        public const float DefaultBlendDepth = 0.01f, DefaultBlendSpan = 0.04f;

        /// <summary>混合の値を Resources から読む。無ければ既定。</summary>
        public static (float depth, float span) LoadBlend()
        {
            var settings = Resources.Load<RegionSurfaceSettings>(RegionSurfaceMaps.Root + "/SurfaceSettings");
            return settings == null ? (DefaultBlendDepth, DefaultBlendSpan) : (settings.BlendDepthMetres, settings.BlendSpanMetres);
        }

        /// <summary>既定の値。実寸は ambientCG・Poly Haven の地面と岩の素材で多い 2 m 前後（開発担当の仮定）。</summary>
        public static Entry[] Defaults()
        {
            return new[]
            {
                new Entry { TileMetres = 2f, HeightMetres = 0.04f, Strength = 1f, Roughness = 0.85f },
                new Entry { TileMetres = 2f, HeightMetres = 0.03f, Strength = 1f, Roughness = 0.9f },
                new Entry { TileMetres = 2f, HeightMetres = 0.06f, Strength = 1f, Roughness = 0.9f },
                new Entry { TileMetres = 2f, HeightMetres = 0.02f, Strength = 1f, Roughness = 0.95f },
                new Entry { TileMetres = 2f, HeightMetres = 0.02f, Strength = 1f, Roughness = 0.95f },
                new Entry { TileMetres = 1.5f, HeightMetres = 0.04f, Strength = 1f, Roughness = 0.85f },
                new Entry { TileMetres = 2f, HeightMetres = 0.015f, Strength = 1f, Roughness = 0.95f },
                new Entry { TileMetres = 1.5f, HeightMetres = 0.05f, Strength = 1f, Roughness = 0.9f },
            };
        }

        /// <summary>Resources から読む。無い・数が合わない時は既定の値。</summary>
        public static Entry[] Load()
        {
            var settings = Resources.Load<RegionSurfaceSettings>(RegionSurfaceMaps.Root + "/SurfaceSettings");
            Entry[] defaults = Defaults();
            if (settings == null || settings.Roles == null) return defaults;
            for (int i = 0; i < Math.Min(defaults.Length, settings.Roles.Length); i++) defaults[i] = settings.Roles[i];
            return defaults;
        }
    }
}
