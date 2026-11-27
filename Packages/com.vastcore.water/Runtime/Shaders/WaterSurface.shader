Shader "Vastcore/Water/Surface"
{
    Properties
    {
        _DeepWater("Deep Water", Color) = (0.10, 0.24, 0.28, 1)
        _ShallowWater("Shallow Water", Color) = (0.24, 0.44, 0.41, 1)
        _BedTint("Shallow Bed", Color) = (0.33, 0.27, 0.21, 1)
        [NoScaleOffset] _RippleNormal("Ripple Normal", 2D) = "bump" {}
        [NoScaleOffset] _FoamMask("Foam Mask", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "WaterSurface"
            Tags { "LightMode" = "UniversalForwardOnly" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "WaterLighting.hlsl"
            #include "WaterWaves.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 shoreUV : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 coast : TEXCOORD2; // local XZ and distance across the shoreline
                float elevation : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float distance = input.shoreUV.x;
                float3 normal;
                float3 wave = SeaDisplacement(input.positionOS.xz, distance, normal);
                output.positionWS = TransformObjectToWorld(input.positionOS + wave);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(normal);
                output.coast = float3(input.positionOS.xz, distance);
                output.elevation = wave.y;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float2 p = input.coast.xy;
                float time = _WaterTime;
                float d = input.coast.z;
                float2 dx = ddx(p);
                float2 dy = ddy(p);
                float footprint = max(length(dx), length(dy));
                float2 side = float2(-_Motion.y, _Motion.x);
                float2 aligned = float2(dot(p, _Motion.xy), dot(p, side));
                float2 warp = float2(WaterNoise(p * 0.171), WaterNoise(p * 0.142 + 4.0)) * 0.45;
                float2 crestRipples = UnpackNormal(SAMPLE_TEXTURE2D(_RippleNormal, sampler_RippleNormal,
                    (aligned + warp - float2(0.68, 0.08) * time) / 2.8)).xy;
                float2 crossRipples = UnpackNormal(SAMPLE_TEXTURE2D(_RippleNormal, sampler_RippleNormal,
                    (float2(aligned.x + 0.52 * aligned.y, aligned.y - 0.52 * aligned.x)
                    - float2(0.25, -0.17) * time) * 0.199)).xy;
                float2 ripples = (crestRipples * 0.70 + crossRipples * 0.61);
                float2 facets = _Motion.xy * ripples.x + side * ripples.y;
                float3 rippleNormal = TransformObjectToWorldDir(float3(facets.x, 0, facets.y), false);
                float3 n = normalize(input.normalWS + rippleNormal * (1.0 - smoothstep(0.13, 0.82, footprint)) * _Motion.z);

                float2 shoreDrift = p - _Motion.xy * time * 0.43;
                float scatter = WaterNoise(shoreDrift * float2(0.3, 0.37));
                float depth = max(0.01, ShoreDepth(d) + (1.0 - _Bank.x) * input.elevation);
                float attenuation = 1.0 - exp2(-depth * 0.43);
                float3 body = lerp(_ShallowWater.rgb, _DeepWater.rgb, attenuation);
                float seeBed = (1.0 - _Bank.x) * exp2(-depth * 5.4) * 0.84;
                body = lerp(body, _BedTint.rgb, seeBed);
                float thinCrest = saturate(input.elevation / max(0.05, _Wave.x)) * (1.0 - attenuation * 0.78);
                body = body * (0.68 + 0.32 * saturate(n.y) + 0.18 * scatter)
                    + thinCrest * float3(0.018, 0.083, 0.070);

                float3 reflected = reflect(-view, n);
                float3 sky = WaterSky(reflected);
                float fresnel = 0.025 + 0.70 * pow(1.0 - saturate(dot(view, n)), 5.4);
                float3 color = lerp(body, sky * 0.86, fresnel);
                Light sun = GetMainLight();
                float spec = pow(saturate(dot(n, SafeNormalize(view + sun.direction))), 65.0);
                color += sun.color * spec * (0.024 + fresnel * 0.057);

                float steep = saturate(input.elevation / max(0.08, _Wave.x) + 0.33 * (1.0 - n.y));
                float spume = ShoreFoam(shoreDrift);
                float broken = smoothstep(0.67, 0.98, steep + scatter * 0.29)
                    * smoothstep(0.47, 0.73, scatter) * (0.21 + 0.56 * spume);
                float surf = (1.0 - _Bank.x) * smoothstep(-18.0, -_Shore.w, d) * (1.0 - smoothstep(-0.95, 0.0, d));
                float2 breaker = float2(p.x - d - _Shore.w, p.y);
                float age = frac(-MainWavePhase(breaker) * 0.15915494);
                float spilled = smoothstep(0.028, 0.11, age) * (1.0 - smoothstep(0.48, 0.75, age));
                broken *= 1.0 - surf * (1.0 - spilled) * 0.68;
                float crest = cos(MainWavePhase(p) - 0.25);
                float foamShape = smoothstep(0.70 - scatter * 0.24, 0.96, crest)
                    * smoothstep(0.14, 0.64, spume + 0.23 * scatter);
                float foam = saturate(broken + surf * foamShape * spilled * 0.78) * _Motion.w
                    * smoothstep(0.075, 0.55, _Wave.x) * (1.0 - smoothstep(0.4, 1.35, footprint));
                color = lerp(color, float3(0.69, 0.75, 0.74), foam);
                return half4(WaterDistanceFog(color, input.positionWS), 1.0);
            }
            ENDHLSL
        }
    }
}
