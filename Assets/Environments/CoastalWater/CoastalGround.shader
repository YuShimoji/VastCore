Shader "Vastcore/Water/Sample Ground"
{
    Properties
    {
        _Surface("Photo Surface", 2D) = "white" {}
        _Turf("Turf", 2D) = "white" {}
        _Kind("Kind", Float) = 0
        _Scale("Metres per Tile", Float) = 2
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Tags { "LightMode" = "UniversalForwardOnly" }
            Cull Back
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.vastcore.water/Runtime/Shaders/WaterLighting.hlsl"

            TEXTURE2D(_Surface); SAMPLER(sampler_Surface);
            TEXTURE2D(_Turf); SAMPLER(sampler_Turf);
            CBUFFER_START(UnityPerMaterial)
                float4 _CrossSection;
                float _Kind;
                float _Scale;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 coast : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 local : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float2 coast : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.local = input.positionOS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.coast = input.coast;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 n = normalize(input.normalWS);
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float2 p = input.local.xz;
                float d = input.coast.x;
                float z = input.coast.y;
                float terrainScale = _Scale * lerp(1.0, 4.0, smoothstep(13.0, 90.0, d));
                float broad = WaterNoise(p * 0.12) * 0.65 + WaterNoise(p * 0.42) * 0.35;
                float2 tex = p / terrainScale;
                if (_Kind > 1.5)
                    tex = float2(input.local.z, input.local.y + 0.76 * input.local.x) / _Scale;
                float3 photo = SAMPLE_TEXTURE2D(_Surface, sampler_Surface, tex).rgb;
                float wet = 0.0;
                float3 baseColor;
                if (_Kind < 0.5)
                {
                    float ground = saturate(0.40 + broad * 0.60 + (1.0 - n.y) * 0.33);
                    float terrain = smoothstep(10.7, 19.5, d + broad * 8.0 + _CrossSection.w * 3.6);
                    float3 turf = SAMPLE_TEXTURE2D(_Turf, sampler_Turf, tex * 0.40).rgb;
                    float rockFace = smoothstep(0.13, 0.54, 1.0 - n.y + WaterNoise(p * 0.075) * 0.29);
                    turf = lerp(turf * float3(0.54, 0.56, 0.43), float3(0.068, 0.057, 0.046) * (0.53 + photo * 1.53), rockFace);
                    turf *= (0.63 + 0.32 * broad + 0.12 * ground);
                    wet = 1.0 - smoothstep(0.88, 6.4, d + broad * 1.45);
                    float3 sand = photo * lerp(float3(0.41, 0.38, 0.36), float3(0.96, 0.86, 0.72), 1.0 - wet);
                    baseColor = lerp(sand * (0.69 + 0.29 * broad), turf, terrain);
                }
                else if (_Kind < 1.5)
                {
                    float road = d - _CrossSection.y;
                    float width = _CrossSection.z;
                    float edge = min(abs(road - 0.38), abs(road - width + 0.38));
                    float center = abs(road - width * 0.5);
                    float dash = step(1.8, frac(z / 11.0) * 11.0) * step(frac(z / 11.0) * 11.0, 6.2);
                    float stripe = 1.0 - smoothstep(0.050, 0.11, edge);
                    float lane = (1.0 - smoothstep(0.057, 0.093, center)) * dash;
                    float nearTrack = (road - width * 0.25) * 1.7;
                    float farTrack = (road - width * 0.75) * 1.7;
                    float tire = exp(-nearTrack * nearTrack) + exp(-farTrack * farTrack);
                    float wetPatch = WaterNoise(float2(p.x * 2.4, p.y * 0.39)) * 0.68 + broad * 0.32;
                    wet = saturate(0.28 + wetPatch * 0.82 + tire * 0.16 - smoothstep(0.5, width, road) * 0.16);
                    baseColor = photo * (0.54 + broad * 0.21) * (1.0 - tire * 0.16) * (1.0 - wet * 0.29);
                    baseColor = lerp(baseColor, float3(0.57, 0.51, 0.43), stripe * (0.83 + broad * 0.12));
                    baseColor = lerp(baseColor, float3(0.59, 0.39, 0.095), lane * (0.78 + broad * 0.15));
                }
                else
                {
                    float joint = 1.0 - smoothstep(0.005, 0.028, abs(frac(z / 3.2) - 0.5));
                    wet = (1.0 - smoothstep(0.3, 2.6, input.local.y - _CrossSection.x)) * 0.52 + broad * 0.30;
                    baseColor = photo * lerp(float3(0.46, 0.49, 0.54), float3(1.07, 1.03, 0.93), broad);
                    baseColor *= 1.0 - joint * 0.35 * (1.0 - step(2.5, _Kind));
                    baseColor *= 1.0 - wet * 0.36;
                }

                Light sun = GetMainLight();
                float diffuse = saturate(dot(n, sun.direction));
                float3 color = baseColor * (0.56 + 0.26 * n.y + sun.color * diffuse * 0.21);
                float glancing = pow(1.0 - saturate(dot(n, view)), 4.0);
                float3 reflection = reflect(-view, n);
                float reflectivity = wet * glancing * (_Kind > 0.5 && _Kind < 1.5 ? 0.54 : 0.17);
                color = lerp(color, WaterSky(reflection) * 0.61, reflectivity);
                return half4(WaterDistanceFog(color, input.positionWS), 1.0);
            }
            ENDHLSL
        }
    }
}
