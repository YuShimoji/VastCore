using UnityEngine;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 砂岩の台地の場面で、遠くまで届く地形の影を GPU が描画のたびに計算するための入力（RegionShadows.hlsl）。
    /// 影を落とす高さの格子を浮動小数の画像にしてシェーダーへ渡す。太陽（主光源）を回すと、場面を作り直さずに影が動く。
    /// 崖下のアルコーブの張り出しは高さの格子では表せないので、その部分は頂点に持たせた影を使い続ける（_SunFromVertex = 1）。
    /// </summary>
    public static class RegionTerrainShadows
    {
        /// <summary>false にすると、頂点に持たせた影（場面を作った時の太陽）に戻る。</summary>
        public static bool Enabled = true;

        /// <summary>一歩ごとに、たどった距離に対して伸ばす割合。大きいほど速く、細い影を飛ばしやすい。</summary>
        public const float StepGrowth = 0.035f;

        /// <summary>1 画素でたどる最大の歩数。</summary>
        public const int MaxSteps = 192;

        private static Texture2D inner, outer;

        public static void Bind(RegionLookScene scene)
        {
            Release();
            if (!Enabled || scene.ShadowInner == null || scene.ShadowOuter == null)
            {
                Shader.SetGlobalVector("_RegionShadowParams", Vector4.zero);
                return;
            }
            float top = float.MinValue;
            foreach (float h in scene.ShadowInner) if (h > top) top = h;
            foreach (float h in scene.ShadowOuter) if (h > top) top = h;

            inner = Upload("Region shadow heights (inner)", scene.ShadowInner, scene.ShadowInnerSize);
            outer = Upload("Region shadow heights (outer)", scene.ShadowOuter, scene.ShadowOuterSize);
            Shader.SetGlobalTexture("_RegionShadowInner", inner);
            Shader.SetGlobalTexture("_RegionShadowOuter", outer);
            Shader.SetGlobalVector("_RegionShadowInnerRect", new Vector4(scene.InnerMinX, scene.InnerMinZ, scene.InnerCell, scene.ShadowInnerSize));
            Shader.SetGlobalVector("_RegionShadowOuterRect", new Vector4(0.5f * scene.OuterCell, 0.5f * scene.OuterCell, scene.OuterCell, scene.ShadowOuterSize));
            Shader.SetGlobalVector("_RegionShadowParams", new Vector4(top, StepGrowth, MaxSteps, 1f));
        }

        public static void Release()
        {
            if (inner != null) Object.DestroyImmediate(inner);
            if (outer != null) Object.DestroyImmediate(outer);
            inner = outer = null;
        }

        private static Texture2D Upload(string name, float[] heights, int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RFloat, false, true)
            {
                name = name, hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixelData(heights, 0);
            texture.Apply(false, true);
            return texture;
        }
    }
}
