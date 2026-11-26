using UnityEngine;
using UnityEngine.Rendering;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 砂岩の台地の場面で、表面の凹凸（バンプ）を場面を作る時に GPU で一度だけ画像へ焼き、地形のシェーダーへ渡す
    /// （RegionReliefBake.shader → RegionSandstone.hlsl の SandstoneReliefNormal）。描画のたびに作り直さない。
    /// - 地層の柱：色の縞と同じ式から起伏（m）を作り、その傾きで崖の面の法線を上下に傾ける。層の境が溝、硬い層が張り出す。
    /// - 岩肌の細かな凹凸：繰り返す高さの画像から傾きを作り、崖の面と転石に biplanar で貼る。写真の素材を選ぶまでの仮。
    /// 高さの画像から傾きを出す処理（Sobel）は、どの高さの画像にも使える（設計は docs/02_design/REGION_SURFACE_MATERIALS_SPEC.md）。
    /// </summary>
    public static class RegionSurfaceRelief
    {
        /// <summary>false にすると、凹凸を焼かず、形の法線だけで光を受ける（以前の描き方）。</summary>
        public static bool Enabled = true;

        /// <summary>地層の柱が受け持つ高さ（帽子の岩の下端からの m）。この外は端の値を使う。</summary>
        public const float StrataMin = -192f, StrataRange = 384f;
        public const int StrataTexels = 8192;

        /// <summary>岩肌の細かな凹凸の繰り返しの一辺（m）と画素数。</summary>
        public const float RockTile = 4f;
        public const int RockTexels = 512;

        /// <summary>傾きに掛ける強さ。1 で焼いた起伏のまま（岩肌の細かな凹凸は、焼いた起伏が 4 cm 程度と弱いので強める）。</summary>
        public static float StrataStrength = 1f, RockStrength = 2.5f;

        /// <summary>岩肌の細かな凹凸が薄れ始める距離と、読まなくなる距離（m）。遠くでは画像を読まない。</summary>
        public static float RockFadeStart = 40f, RockFadeEnd = 80f;

        private const string BakeShader = "RegionMap3D/RegionReliefBake";
        private static RenderTexture strataSlope, rockSlope;

        /// <summary>焼いた画像があり、シェーダーが凹凸を使える状態か。</summary>
        public static bool Baked => strataSlope != null && rockSlope != null;

        public static void Bind()
        {
            Release();
            Shader shader = Resources.Load<Shader>(BakeShader);
            if (!Enabled || shader == null || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                SetActive(false);
                return;
            }
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                strataSlope = Bake(material, 0, "Region relief: strata", StrataTexels, 1, StrataRange / StrataTexels, 1f,
                    new Vector4(StrataMin, StrataRange, 0f, 0f), TextureWrapMode.Clamp);
                rockSlope = Bake(material, 1, "Region relief: rock", RockTexels, RockTexels, RockTile / RockTexels, RockTile / RockTexels,
                    new Vector4(RockTile, 0f, 0f, 0f), TextureWrapMode.Repeat);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
            Shader.SetGlobalTexture("_RegionReliefStrataSlope", strataSlope);
            Shader.SetGlobalTexture("_RegionReliefRockSlope", rockSlope);
            SetActive(true);
        }

        /// <summary>
        /// 焼いた画像を残したまま、凹凸を使うかを切り替える（同じ場面の前後比較と、重さの計測に使う）。
        /// 焼いていない時は常に切る。
        /// </summary>
        public static void SetActive(bool active)
        {
            bool on = active && Baked;
            Shader.SetGlobalVector("_RegionReliefStrata", new Vector4(StrataMin, 1f / StrataRange, StrataStrength, on ? 1f : 0f));
            Shader.SetGlobalVector("_RegionReliefRock", new Vector4(1f / RockTile, RockStrength, RockFadeStart, RockFadeEnd));
        }

        public static void Release()
        {
            if (strataSlope != null) { strataSlope.Release(); Object.DestroyImmediate(strataSlope); }
            if (rockSlope != null) { rockSlope.Release(); Object.DestroyImmediate(rockSlope); }
            strataSlope = rockSlope = null;
        }

        // 高さをパス heightPass で焼き、Sobel（パス 2）で傾き（m/m）にして、mip を付けて返す
        private static RenderTexture Bake(Material material, int heightPass, string name, int width, int height,
            float metresX, float metresY, Vector4 parameters, TextureWrapMode wrap)
        {
            var heights = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
            {
                name = name + " height", hideFlags = HideFlags.HideAndDontSave, wrapMode = wrap, filterMode = FilterMode.Point,
            };
            var slope = new RenderTexture(width, height, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear)
            {
                name = name + " slope", hideFlags = HideFlags.HideAndDontSave, wrapMode = wrap, filterMode = FilterMode.Trilinear,
                useMipMap = true, autoGenerateMips = true, anisoLevel = 4,
            };
            heights.Create();
            slope.Create();
            material.SetVector("_BakeParams", parameters);
            Graphics.Blit(null, heights, material, heightPass);
            material.SetTexture("_BakeSource", heights);
            material.SetVector("_BakeTexel", new Vector4(1f / width, 1f / height, metresX, metresY));
            Graphics.Blit(null, slope, material, 2);
            heights.Release();
            Object.DestroyImmediate(heights);
            return slope;
        }
    }
}
