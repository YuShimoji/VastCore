Shader "Vastcore/RegionCoastWater"
{
    Properties
    {
        [NoScaleOffset] _Shore ("Shore depth and exposure", 2D) = "black" {}
        _Extent ("Terrain span in metres", Float) = 3200
        _WaveStrength ("Shore wave strength", Range(0,1)) = 1
        [HideInInspector] _FixedTime ("Fixed comparison time", Float) = -1
        [HideInInspector] _CoveMode ("Cove sea or retained pool", Float) = 0
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
                float _FixedTime;
                float _CoveMode;
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
                output.grid = _CoveMode > 0.5 ? positionOS.xz / _Extent + 0.5 : positionOS.xz * float2(1,-1) / _Extent;
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

            half3 CoveWater(float3 positionWS, half4 bottom, half outside, float time)
            {
                bool pool = _CoveMode > 1.5;
                if (pool || outside < 0.003) clip(bottom.a - 0.27h);
                float2 p = positionWS.xz;
                half depth = lerp(bottom.r * 12.75h, 14.0h, outside);
                half exposure = bottom.g * (1.0h - outside);
                half sand = bottom.b * (1.0h - outside);
                float grain = CoastNoise(p * 0.17 + 6.3);
                float swell = p.y * 1.06 + p.x * 0.23 + 5.8 * CoastNoise(p * 0.059) + 1.9 * grain + time * 1.28;
                float cross = dot(p, float2(0.64, -1.46)) + grain * 5.7 - time * 1.79;
                float ripples = dot(p, float2(-3.3, 3.1)) + 4.5 * CoastNoise(p * 0.27) + time * 1.94;
                half detail = 1.0h - smoothstep(0.31h, 1.9h, fwidth(ripples));
                half wave = 1.0h - smoothstep(0.23h, 2.4h, fwidth(swell));
                half agitation = pool ? 0.10h : lerp(0.33h, 0.88h, exposure);
                half3 n = normalize(half3(agitation * (0.033h * cos(cross) * wave + 0.035h * sin(ripples) * detail),
                    1.0h, agitation * (0.063h * cos(swell) * wave + 0.043h * cos(ripples * 0.83) * detail)));
                half3 v = GetWorldSpaceNormalizeViewDir(positionWS);
                half fresnel = (pool ? 0.12h : 0.026h) + 0.69h * pow(1.0h - saturate(dot(n, v)), 4.7h);
                half flecks = 0.81h + 0.22h * CoastNoise(p * 1.7) + 0.15h * sin(cross + sin(swell)) * detail;
                half grit = saturate(CoastNoise(p * 0.85) * 0.65h + CoastNoise(p * 3.35) * 0.35h);
                half3 stone = lerp(half3(0.044h, 0.065h, 0.058h), half3(0.18h, 0.134h, 0.08h), grit * grit);
                half3 bed = lerp(stone, half3(0.36h, 0.287h, 0.17h), sand) * flecks;
                half3 water = lerp(half3(0.018h, 0.067h, 0.104h), half3(0.055h, 0.221h, 0.225h), exp(-depth * 0.39h));
                water = lerp(water, bed, exp(-depth * 0.96h) * (pool ? 0.88h : 0.84h));
                water = lerp(water, half3(0.235h, 0.38h, 0.51h), fresnel);
                half3 h = normalize(v + GetMainLight().direction);
                water += pow(saturate(dot(n, h)), 100.0h) * half3(0.31h, 0.28h, 0.24h);
                if (!pool)
                {
                    half lace = smoothstep(0.27h, 0.72h, CoastNoise(p * 1.9 + time * 0.35) * 0.60h + grain * 0.40h);
                    half swash = (1.0h - smoothstep(0.065h, 0.20h + exposure * 0.18h, depth)) * lace;
                    half crests = smoothstep(0.71h, 0.98h, 0.5h + 0.31h * sin(swell) + 0.19h * CoastNoise(p * 0.63));
                    half breaking = smoothstep(0.1h, 0.55h, depth) * (1.0h - smoothstep(0.66h, 2.0h, depth));
                    half foam = swash * lerp(0.48h, 0.88h, exposure) + crests * breaking * exposure * lace * wave;
                    water = lerp(water, half3(0.74h, 0.81h, 0.82h), saturate(foam));
                }
                return water;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.positionWS.xz;
                half4 bottom = SAMPLE_TEXTURE2D(_Shore, sampler_Shore, input.grid);
                float2 distanceOutside = max(max(-input.grid, input.grid - 1.0), 0.0) * _Extent;
                half offshore = saturate(length(distanceOutside) / 90.0);
                float time = _FixedTime >= 0.0 ? _FixedTime : _Time.y;
                if (_CoveMode > 0.5)
                    return RegionOutput(RegionApplyAir(CoveWater(input.positionWS, bottom, offshore, time), input.positionWS), 1.0);
                half2 bathymetry = bottom.rg;
                half depth = lerp(bathymetry.r * 31.875, 32.0, offshore);
                half exposure = bathymetry.g * (1.0 - offshore);
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
