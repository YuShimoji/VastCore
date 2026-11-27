Shader "Vastcore/RegionCoastWater"
{
    Properties
    {
        [NoScaleOffset] _Shore ("Shore depth and exposure", 2D) = "black" {}
        _Extent ("Terrain span in metres", Float) = 3200
        _WaveStrength ("Shore wave strength", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+10" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "RegionAtmosphere.hlsl"

            TEXTURE2D(_Shore);
            SAMPLER(sampler_Shore);
            CBUFFER_START(UnityPerMaterial)
                float _Extent;
                float _WaveStrength;
            CBUFFER_END

            struct Varyings
            {
                float4 clip : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 grid : TEXCOORD1;
            };

            Varyings Vert(float3 positionOS : POSITION)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(positionOS);
                output.clip = TransformWorldToHClip(output.positionWS);
                output.grid = positionOS.xz * float2(1,-1) / _Extent;
                return output;
            }

            float CoastHash(float2 p)
            {
                p = frac(p * float2(0.1031, 0.1099));
                p += dot(p, p.yx + 33.33);
                return frac((p.x + p.y) * p.x);
            }

            float CoastNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 t = frac(p);
                t = t * t * (3.0 - 2.0 * t);
                float a = lerp(CoastHash(cell), CoastHash(cell + float2(1,0)), t.x);
                float b = lerp(CoastHash(cell + float2(0,1)), CoastHash(cell + 1.0), t.x);
                return lerp(a, b, t.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.positionWS.xz;
                half2 bathymetry = SAMPLE_TEXTURE2D(_Shore, sampler_Shore, input.grid).rg;
                float2 distanceOutside = max(max(-input.grid, input.grid - 1.0), 0.0) * _Extent;
                half offshore = saturate(length(distanceOutside) / 90.0);
                half depth = lerp(bathymetry.r * 31.875, 32.0, offshore);
                half exposure = bathymetry.g * (1.0 - offshore);
                float time = _Time.y;
                float spread = CoastNoise(p * 0.037);
                float swell = dot(p, float2(0.42, 0.31)) + 7.0 * spread - time * 1.1;
                float capillary = dot(p, float2(-1.9, 1.6)) - time * 2.3;
                float ripple = dot(p, float2(0.19, -1.1)) + 3.1 * spread + time * 0.66;
                half fine = 1.0 - smoothstep(0.22, 1.3, max(fwidth(capillary), fwidth(ripple)));
                half broad = 1.0 - smoothstep(0.18, 1.12, fwidth(swell));
                half3 n = normalize(half3(
                    (cos(swell) * 0.025 * broad + sin(capillary) * 0.010 * fine) * _WaveStrength,
                    1.0, (sin(swell) * 0.024 * broad + cos(ripple) * 0.014 * fine) * _WaveStrength));
                half3 v = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half fresnel = 0.022 + 0.72 * pow(saturate(1.0 - dot(n, v)), 4.0);
                half shallow = exp(-depth * 0.19);
                half3 water = lerp(half3(0.015, 0.074, 0.110), half3(0.102, 0.264, 0.238), shallow);
                water = lerp(water, half3(0.39, 0.52, 0.59), fresnel);
                half3 h = normalize(v + GetMainLight().direction);
                water += pow(saturate(dot(n, h)), 90.0) * half3(0.38, 0.34, 0.27);

                half wave = 0.38 + 0.32 * sin(swell) + 0.16 * sin(ripple) * fine + 0.24 * CoastNoise(p * 0.33);
                half whitewater = smoothstep(0.62, 0.97, wave) * smoothstep(0.24, 0.72, spread + exposure * 0.26);
                half breaking = smoothstep(0.15, 0.8, depth) * (1.0 - smoothstep(0.9, 3.25, depth));
                half foam = whitewater * breaking * exposure * _WaveStrength * lerp(0.5, 1.0, broad);
                water = lerp(water, half3(0.73, 0.82, 0.8), saturate(foam * 1.28));
                return RegionOutput(RegionApplyAir(water, input.positionWS), 1.0);
            }
            ENDHLSL
        }
    }
}
