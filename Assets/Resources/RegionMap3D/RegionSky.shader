// 地域の 3D 表示の空。天頂から地平線へのグラデーションと、太陽の周りの明るさを描く。
// 地平線の色は、地形に重ねる霞（RegionAtmosphere.hlsl）と同じ式で出すので、遠くの地形が空へ溶けて見える。
// 地平線より下は霞の色のまま（地形の外側の切れ目を隠す）。色は線形の値（RegionAtmosphere.cs が SetVector で渡すので、色空間で変換されない）。
Shader "Vastcore/RegionSky"
{
    Properties
    {
        _ZenithColor ("Zenith (linear)", Color) = (0.08, 0.20, 0.52, 1)
        _SunGlowColor ("Sun glow (linear)", Color) = (1.0, 0.72, 0.42, 1)
        _HorizonSharpness ("Horizon band sharpness", Range(1, 12)) = 4.5
        _SunGlowSize ("Sun glow size", Range(1, 512)) = 24
        _SunGlowStrength ("Sun glow strength", Range(0, 8)) = 0.8
        _SunDiscStrength ("Sun disc strength", Range(0, 80)) = 30
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "RegionAtmosphere.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _SunGlowColor;
                half _HorizonSharpness;
                half _SunGlowSize;
                half _SunGlowStrength;
                half _SunDiscStrength;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 view = normalize(input.direction);
                float3 sun = normalize(_MainLightPosition.xyz);
                half3 horizon = RegionFogColor(view, sun);
                // 地平線付近は厚い空気を通るので白っぽく、上ほど濃い青
                half up = saturate(view.y);
                half band = pow(1.0 - up, _HorizonSharpness);
                half3 sky = lerp(_ZenithColor.rgb, horizon, band);
                // 太陽の周りの明るさと、太陽の円盤
                half facing = saturate(dot(view, sun));
                sky += _SunGlowColor.rgb * pow(facing, _SunGlowSize) * _SunGlowStrength;
                sky += _SunGlowColor.rgb * smoothstep(0.99955, 0.99975, facing) * _SunDiscStrength;
                // 地平線より下は霞の色
                sky = lerp(sky, horizon, smoothstep(0.0, -0.03, view.y));
                return RegionOutput(sky, 1);
            }
            ENDHLSL
        }
    }
}
