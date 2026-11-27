using UnityEngine;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 土砂の地図（RegionSediment が窓ごとに作る Deposit・Wear・SedimentFlow・Scree・Coarse）を、内側・外側の窓ごとに RGBA の画像 1 枚へ入れ、
    /// 地形のマテリアルへ結び付ける（RegionSandstone.hlsl の SandSedimentMaps が m の座標で読む）。
    /// 地図は「どの層を出すか」の選択にだけ使い、色の模様には直接使わない（設計 docs/02_design/REGION_SURFACE_MATERIALS_SPEC.md 6.2 節）。
    /// 赤 = Deposit（細かい土砂の層）、緑 = 流路（SedimentFlow と Wear の大きい方。流路の礫の層）、青 = Scree（崖から落ちた岩屑の錐）、
    /// アルファ = Coarse（大きい岩の割合。岩屑の層の礫を大きくする）。画像は線形の値（sRGB にしない）。
    /// </summary>
    public static class RegionSedimentLayers
    {
        public const string MapProperty = "_SedimentMap", RectProperty = "_SedimentRect";

        /// <summary>false にすると、地図を結び付けても層を出さない（土砂の場を流した地形のまま、層だけを切った絵になる）。</summary>
        public static bool Enabled = true;

        /// <summary>直前の場面で地図を結び付けたマテリアルの数。</summary>
        public static int Bound { get; private set; }

        /// <summary>場面を作り始める時に呼ぶ。数え直し、層を出す状態にする。</summary>
        public static void Begin()
        {
            Bound = 0;
            RockGroundMaterial = null;
            _rockGround = true;
            SetActive(true);
        }

        /// <summary>
        /// 地形の部品の地図を画像にしてマテリアルへ結び付ける。頂点 (i, j) が場面の (minX + i * cell, minZ + j * cell) にある格子と同じ並び。
        /// 地図が無い窓では何もしない（シェーダーは層を出さず、今の描き方のまま）。
        /// </summary>
        public static bool Bind(Material material, RegionData data, float minX, float minZ, float cell)
        {
            if (material == null || data == null || data.Deposit == null || data.Wear == null || data.SedimentFlow == null
                || data.Scree == null || data.Coarse == null) return false;
            int n = data.Resolution;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, true, true)
            {
                name = material.name + " Sediment", hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear, anisoLevel = 4,
            };
            texture.SetPixels32(Pack(data));
            texture.Apply(true, true);
            material.SetTexture(MapProperty, texture);
            // 画素の中心が頂点に重なる。u = (x - minX) / (cell * n) + 0.5 / n（z も同じ。Core の座標で南が正）
            material.SetVector(RectProperty, new Vector4(minX, minZ, 1f / (cell * n), 0.5f / n));
            Bound++;
            return true;
        }

        /// <summary>地図 5 つを RGBA の 4 つに詰める（並びは RegionData の格子と同じ。行 0 が北）。</summary>
        public static Color32[] Pack(RegionData data)
        {
            int count = data.Resolution * data.Resolution;
            var pixels = new Color32[count];
            for (int i = 0; i < count; i++)
            {
                float wash = Mathf.Max(data.SedimentFlow[i], data.Wear[i]);
                pixels[i] = new Color32(Byte(data.Deposit[i]), Byte(wash), Byte(data.Scree[i]), Byte(data.Coarse[i]));
            }
            return pixels;
        }

        /// <summary>直前の場面で、岩の下の方を地面の色へつなぐ岩のマテリアル（無ければ null）。</summary>
        public static Material RockGroundMaterial { get; private set; }

        private static bool _rockGround = true;

        /// <summary>
        /// 岩のマテリアルに、from（内側の地形）の地図をそのまま結び付ける（T4）。岩の画素は、岩の下の方に付ける地面の色をこの地図で作る。
        /// 地図の範囲の外（外側の窓の岩）は層を出さない地面の色になる。帯の高さは岩の頂点の値（RegionRockField.GroundBand）が持つ。
        /// </summary>
        public static bool ShareWithRocks(Material from, Material rocks)
        {
            RockGroundMaterial = null;
            if (from == null || rocks == null || !from.HasTexture(MapProperty) || from.GetTexture(MapProperty) == null) return false;
            rocks.SetTexture(MapProperty, from.GetTexture(MapProperty));
            rocks.SetVector(RectProperty, from.GetVector(RectProperty));
            RockGroundMaterial = rocks;
            return true;
        }

        /// <summary>
        /// 岩の下の方を地面の色へつなぐかを切り替える（同じ場面の前後比較と、重さの計測に使う）。シェーダーの切り替えは全体の値で、
        /// マテリアルのキーワードは使わない（キーワードで切り替えると、同じコードでも 3840x2160 の近景で 6 ms 重くなった。2026-11-27）。
        /// </summary>
        public static void SetRockGround(bool active)
        {
            _rockGround = active;
            SetActive(_layersActive);
        }

        /// <summary>結び付けた地図を残したまま、層を出すかを切り替える（同じ場面の前後比較と、重さの計測に使う）。</summary>
        public static void SetActive(bool active)
        {
            _layersActive = active;
            Shader.SetGlobalVector("_RegionSedimentParams", new Vector4(active && Enabled ? 1f : 0f, _rockGround ? 1f : 0f, 0f, 0f));
        }

        private static bool _layersActive = true;

        private static byte Byte(float value)
        {
            return (byte)Mathf.RoundToInt(Mathf.Clamp01(value) * 255f);
        }
    }
}
