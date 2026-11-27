using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Vastcore.RegionMap
{
    /// <summary>
    /// 3D 表示の「光と空気」。天気・時刻の条件（Preset）ごとに、太陽・空・霞・空からの光・照り返しと、後処理（トーンマッピング・露出・色・ブルーム・周辺減光）をまとめて決める。
    /// 条件は見た目の方向ではなく、場面が受け持つ状況。空と霞の色を条件ごとに手で決めている点は暫定で、計算方式（物理ベースの大気など）は本人の選定待ち。
    /// 太陽の高さは頂点に持たせる影の計算にも使うので、場面を作る前に <see cref="PrepareSun"/> を呼び、作った後に <see cref="Apply"/> を呼ぶ。
    /// 色は見た目の値（sRGB）で書き、シェーダーへは線形に直して渡す（RegionAtmosphere.hlsl）。
    /// </summary>
    public sealed class RegionAtmosphere
    {
        /// <summary>カメラの後処理を切り替え、前の状態を返す（後処理の前の HDR の値を読む計測用）。</summary>
        public static bool SetPostProcessing(Camera camera, bool on)
        {
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            bool previous = data.renderPostProcessing;
            data.renderPostProcessing = on;
            return previous;
        }

        public enum Preset
        {
            /// <summary>澄んだ午後。太陽 38 度、薄い青い霞、色は素直。現地写真に近い記録の見え方</summary>
            ClearAfternoon,
            /// <summary>夕方の斜光。太陽 12 度、橙の光と青紫の影、太陽の側の地平線が明るい。地層と割れ目が浮く</summary>
            GoldenHour,
            /// <summary>砂ぼこりの霞。太陽 26 度、暖かく濃い霞で奥ほど淡い。台地の前後関係が段で分かれる</summary>
            DustHaze,
        }

        public const string SkyShaderResource = "RegionMap3D/RegionSky";

        public string Name;
        public float SunAltitudeDegrees;
        public Color SunColor;
        public float SunIntensity;
        public Color AmbientSky, AmbientEquator, AmbientGround;
        public float AmbientIntensity = 1f;
        public Color Zenith;
        public float SkyIntensity = 1f;
        public Color Fog, FogTowardSun;
        public float FogTowardSunSharpness = 6f;
        public float FogDensity;
        public float FogFalloffMeters = 600f;
        public float FogMax = 0.92f;
        public Color Bounce;
        public float BounceIntensity;
        public Color ShadowTint = Color.white;
        public Color SunGlow;
        public float SunGlowSize = 24f, SunGlowStrength = 0.8f;

        public TonemappingMode Tonemap = TonemappingMode.ACES;
        public float PostExposure, Contrast, Saturation;
        public float Temperature, Tint;
        public float BloomIntensity, BloomThreshold = 1.1f;
        public float VignetteIntensity;

        public static RegionAtmosphere Create(Preset preset)
        {
            switch (preset)
            {
                case Preset.GoldenHour:
                    return new RegionAtmosphere
                    {
                        Name = "golden",
                        SunAltitudeDegrees = 12f,
                        SunColor = new Color(1f, 0.66f, 0.38f), SunIntensity = 3.6f,
                        AmbientSky = new Color(0.50f, 0.56f, 0.72f), AmbientEquator = new Color(0.66f, 0.54f, 0.50f), AmbientGround = new Color(0.48f, 0.33f, 0.24f),
                        AmbientIntensity = 1.1f,
                        Zenith = new Color(0.25f, 0.40f, 0.72f), SkyIntensity = 1.05f,
                        Fog = new Color(0.66f, 0.68f, 0.78f), FogTowardSun = new Color(1f, 0.86f, 0.64f), FogTowardSunSharpness = 5f,
                        FogDensity = 0.00014f, FogMax = 0.88f,
                        Bounce = new Color(0.95f, 0.52f, 0.30f), BounceIntensity = 0.85f,
                        ShadowTint = new Color(0.93f, 0.96f, 1.06f),
                        SunGlow = new Color(1f, 0.78f, 0.48f), SunGlowSize = 8f, SunGlowStrength = 0.9f,
                        Tonemap = TonemappingMode.ACES, PostExposure = 0.75f, Contrast = 6f, Saturation = 4f,
                        Temperature = 6f, BloomIntensity = 0.35f, BloomThreshold = 1.0f, VignetteIntensity = 0.22f,
                    };
                case Preset.DustHaze:
                    return new RegionAtmosphere
                    {
                        Name = "dust",
                        SunAltitudeDegrees = 26f,
                        // 参照写真の砂塵（黄土色の空、弱い太陽、明暗の差が小さい）に寄せた値（2026-11-27、Linear で調整）。
                        // 天頂も黄土色にし、太陽を弱め、空からの光を強めて影を浅くし、後処理のコントラストを下げる
                        SunColor = new Color(1f, 0.82f, 0.60f), SunIntensity = 1.7f,
                        AmbientSky = new Color(0.72f, 0.64f, 0.52f), AmbientEquator = new Color(0.66f, 0.57f, 0.46f), AmbientGround = new Color(0.46f, 0.36f, 0.27f),
                        AmbientIntensity = 1.3f,
                        Zenith = new Color(0.66f, 0.58f, 0.46f), SkyIntensity = 1.15f,
                        Fog = new Color(0.82f, 0.70f, 0.54f), FogTowardSun = new Color(0.98f, 0.86f, 0.66f), FogTowardSunSharpness = 3f,
                        FogDensity = 0.00030f, FogFalloffMeters = 900f, FogMax = 0.93f,
                        Bounce = new Color(0.90f, 0.62f, 0.42f), BounceIntensity = 0.35f,
                        ShadowTint = new Color(0.97f, 0.97f, 1.0f),
                        SunGlow = new Color(1f, 0.86f, 0.64f), SunGlowSize = 4f, SunGlowStrength = 0.6f,
                        Tonemap = TonemappingMode.ACES, PostExposure = 0.35f, Contrast = -12f, Saturation = -2f,
                        Temperature = 6f, BloomIntensity = 0.25f, BloomThreshold = 1.0f, VignetteIntensity = 0.16f,
                    };
                default:
                    return new RegionAtmosphere
                    {
                        Name = "clear",
                        SunAltitudeDegrees = RegionVolumePatch.DefaultSunAltitudeDegrees,
                        SunColor = new Color(1f, 0.95f, 0.87f), SunIntensity = 3.0f,
                        AmbientSky = new Color(0.55f, 0.64f, 0.80f), AmbientEquator = new Color(0.62f, 0.58f, 0.54f), AmbientGround = new Color(0.47f, 0.37f, 0.28f),
                        AmbientIntensity = 0.85f,
                        Zenith = new Color(0.26f, 0.47f, 0.82f), SkyIntensity = 1.0f,
                        Fog = new Color(0.72f, 0.79f, 0.88f), FogTowardSun = new Color(0.92f, 0.89f, 0.82f), FogTowardSunSharpness = 6f,
                        FogDensity = 0.00008f, FogMax = 0.85f,
                        Bounce = new Color(0.90f, 0.62f, 0.42f), BounceIntensity = 0.35f,
                        ShadowTint = new Color(0.92f, 0.96f, 1.06f),
                        SunGlow = new Color(1f, 0.92f, 0.78f), SunGlowSize = 32f, SunGlowStrength = 0.6f,
                        Tonemap = TonemappingMode.Neutral, PostExposure = 0.05f, Contrast = 12f, Saturation = 4f,
                        Temperature = 0f, BloomIntensity = 0.15f, BloomThreshold = 1.2f, VignetteIntensity = 0.15f,
                    };
            }
        }

        /// <summary>
        /// 線形の値で決めた色を、Unity の光・空からの光の色として渡す値にする。
        /// Linear 色空間では Unity が渡された色を線形に直すので、先にガンマへ戻しておく。ガンマ色空間ではそのまま（シェーダーが線形として扱う）。
        /// </summary>
        private static Color ForEngine(Color linearValue)
        {
            return QualitySettings.activeColorSpace == ColorSpace.Linear ? linearValue.gamma : linearValue;
        }

        public static bool TryParse(string name, out Preset preset)
        {
            switch ((name ?? "").Trim().ToLowerInvariant())
            {
                case "clear": preset = Preset.ClearAfternoon; return true;
                case "golden": preset = Preset.GoldenHour; return true;
                case "dust": preset = Preset.DustHaze; return true;
                default: preset = Preset.ClearAfternoon; return false;
            }
        }

        /// <summary>場面を作る前に呼ぶ。頂点の影と主光源が同じ太陽の高さになる。</summary>
        public void PrepareSun()
        {
            RegionVolumePatch.SunAltitudeDegrees = SunAltitudeDegrees;
        }

        /// <summary>
        /// 太陽・空・霞・空からの光と、後処理の Volume を場面に置く。groundHeight は霞が最も濃い高さ（場面の地面の高さ）。
        /// 返すのは後処理の Volume を持つ GameObject（シーンと一緒に消える）。
        /// </summary>
        public GameObject Apply(Camera camera, Light sun, float groundHeight) => Apply(camera, sun, groundHeight, null);

        public GameObject Apply(Camera camera, Light sun, float groundHeight, System.Action<Object> ownAsset)
        {
            // 太陽の色の数値は、シェーダーが受け取る線形の値として決めてある
            sun.color = ForEngine(SunColor);
            sun.intensity = SunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = RegionTerrainView.SunRotation();

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ForEngine(AmbientSky.linear * AmbientIntensity);
            RenderSettings.ambientEquatorColor = ForEngine(AmbientEquator.linear * AmbientIntensity);
            RenderSettings.ambientGroundColor = ForEngine(AmbientGround.linear * AmbientIntensity);
            RenderSettings.fog = false;
            RenderSettings.sun = sun;

            Shader.SetGlobalVector("_RegionFogColor", (Vector4)(Fog.linear * SkyIntensity));
            Color toward = FogTowardSun.linear * SkyIntensity;
            Shader.SetGlobalVector("_RegionFogSunColor", new Vector4(toward.r, toward.g, toward.b, FogTowardSunSharpness));
            Shader.SetGlobalVector("_RegionFogParams", new Vector4(FogDensity, FogFalloffMeters, groundHeight, FogMax));
            Shader.SetGlobalVector("_RegionBounceColor", (Vector4)(Bounce.linear * BounceIntensity));
            Shader.SetGlobalVector("_RegionShadowTint", (Vector4)ShadowTint);

            Shader skyShader = Resources.Load<Shader>(SkyShaderResource);
            if (skyShader != null)
            {
                var sky = new Material(skyShader) { name = "Region Sky", hideFlags = HideFlags.DontSave };
                ownAsset?.Invoke(sky);
                // SetVector は色空間で変換されないので、線形の値をそのまま渡せる
                sky.SetVector("_ZenithColor", (Vector4)(Zenith.linear * SkyIntensity));
                sky.SetVector("_SunGlowColor", (Vector4)SunGlow.linear);
                sky.SetFloat("_SunGlowSize", SunGlowSize);
                sky.SetFloat("_SunGlowStrength", SunGlowStrength);
                RenderSettings.skybox = sky;
                camera.clearFlags = CameraClearFlags.Skybox;
            }
            DynamicGI.UpdateEnvironment();

            camera.allowHDR = true;
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.renderShadows = true;

            var volumeObject = new GameObject("Region Atmosphere (" + Name + ")");
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "Region Atmosphere " + Name;
            profile.hideFlags = HideFlags.DontSave;
            ownAsset?.Invoke(profile);

            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(Tonemap);
            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(PostExposure);
            color.contrast.Override(Contrast);
            color.saturation.Override(Saturation);
            WhiteBalance balance = profile.Add<WhiteBalance>(true);
            balance.temperature.Override(Temperature);
            balance.tint.Override(Tint);
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(BloomIntensity);
            bloom.threshold.Override(BloomThreshold);
            bloom.scatter.Override(0.65f);
            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(VignetteIntensity);
            vignette.smoothness.Override(0.45f);
            if (ownAsset != null)
                foreach (VolumeComponent component in profile.components) ownAsset(component);
            volume.sharedProfile = profile;
            return volumeObject;
        }
    }
}
