Shader "Vastcore/Weather/Valley Surface"
{
    Properties
    {
        _Horizon ("Horizon", Color) = (0.63,0.69,0.75,1)
        _Sun ("Sunlight", Color) = (1,0.89,0.73,1)
        _SunDirection ("Sun direction", Vector) = (-0.7,0.35,0.6,0)
        _DistantHaze ("Haze per metre", Float) = 0.000025
        _SurfaceKind ("Surface kind", Float) = 0
        _ForestMap ("Forest floor albedo", 2D) = "white" {}
        _ForestNormal ("Forest floor normal", 2D) = "bump" {}
        _ForestRoughness ("Forest floor roughness", 2D) = "white" {}
        _RockMap ("Rock albedo", 2D) = "white" {}
        _RockNormal ("Rock normal", 2D) = "bump" {}
        _RockRoughness ("Rock roughness", 2D) = "white" {}
        _UseTerrainMaps ("Use terrain maps", Float) = 0
        _Track ("Track origin, grade and bend", Vector) = (-350,3160,0.185,8.2)
        _TrackFrequency ("Track bend frequency", Float) = 0.0135
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "RenderType"="Opaque" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _Horizon;
            float4 _Sun;
            float4 _SunDirection;
            float4 _Track;
            float _TrackFrequency;
            float _SurfaceKind;
            float _DistantHaze;
            float _UseTerrainMaps;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 color : COLOR;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            half4 color : COLOR;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.color = input.color;
            return output;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"

            TEXTURE2D(_ForestMap);
            SAMPLER(sampler_ForestMap);
            TEXTURE2D(_ForestNormal);
            SAMPLER(sampler_ForestNormal);
            TEXTURE2D(_ForestRoughness);
            SAMPLER(sampler_ForestRoughness);
            TEXTURE2D(_RockMap);
            SAMPLER(sampler_RockMap);
            TEXTURE2D(_RockNormal);
            SAMPLER(sampler_RockNormal);
            TEXTURE2D(_RockRoughness);
            SAMPLER(sampler_RockRoughness);

            float Hash(float2 cell)
            {
                float3 p = frac(float3(cell.xyx) * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float Grain(float2 uv)
            {
                float2 cell = floor(uv);
                float2 blend = smoothstep(0.0, 1.0, frac(uv));
                float2 a = float2(Hash(cell), Hash(cell + float2(0, 1)));
                float2 b = float2(Hash(cell + float2(1, 0)), Hash(cell + 1.0));
                return lerp(lerp(a.x, b.x, blend.x), lerp(a.y, b.y, blend.x), blend.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 view = GetCameraPositionWS() - input.positionWS;
                float depth = length(view);
                view /= max(depth, 0.001);
                float2 xz = input.positionWS.xz;
                float macro = Grain(xz * 0.011) * 0.69 + Grain(xz * 0.047 + 12.4) * 0.31;
                half3 albedo = input.color.rgb * lerp(0.84, 1.16, macro);
                float roughness = 0.82;
                if (_SurfaceKind < 0.5)
                {
                    float crowns = Grain(xz * 0.078) * 0.61 + Grain(xz * 0.219 + 9.2) * 0.39;
                    albedo *= lerp(1.0, lerp(0.68, 1.15, crowns), input.color.a);
                    float along = xz.y - _Track.y;
                    float center = _Track.x + _Track.z * along + _Track.w * sin(_TrackFrequency * along);
                    float edge = abs(xz.x - center) + (Grain(xz * 3.4) - 0.5) * 0.49;
                    float track = (1.0 - smoothstep(1.25, 2.3, edge)) *
                        smoothstep(-18.0, -8.0, along) * (1.0 - smoothstep(614.0, 990.0, along));
                    albedo = lerp(albedo, half3(0.240, 0.211, 0.164) * lerp(0.68, 1.21, macro), track);
                    float detail = 1.0 - smoothstep(64.0, 280.0, depth);
                    if (_UseTerrainMaps > 0.5 && detail > 0.001)
                    {
                        float exposure = smoothstep(0.16, 0.49, 1.0 - normal.y + (macro - 0.5) * 0.26);
                        float2 woodUV = xz / 2.1;
                        float2 rockUV = xz / 1.6;
                        half3 wood = SAMPLE_TEXTURE2D(_ForestMap, sampler_ForestMap, woodUV).rgb;
                        half3 rock = SAMPLE_TEXTURE2D(_RockMap, sampler_RockMap, rockUV).rgb;
                        half3 pattern = lerp(wood * 2.1, rock * 1.83, max(exposure, track * 0.62));
                        albedo *= lerp(1.0, clamp(pattern, 0.31, 1.72), detail * 0.77);
                        half3 woodsNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_ForestNormal, sampler_ForestNormal, woodUV), 0.67);
                        half3 rockNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_RockNormal, sampler_RockNormal, rockUV), 0.55);
                        float2 bump = lerp(woodsNormal.xy, rockNormal.xy, max(exposure, track * 0.62)) * detail;
                        float3 tangent = normalize(float3(1, 0, 0) - normal * normal.x);
                        float3 bitangent = normalize(cross(tangent, normal));
                        normal = normalize(normal + tangent * bump.x + bitangent * bump.y);
                        roughness = lerp(0.82, lerp(
                            SAMPLE_TEXTURE2D(_ForestRoughness, sampler_ForestRoughness, woodUV).r,
                            SAMPLE_TEXTURE2D(_RockRoughness, sampler_RockRoughness, rockUV).r, exposure), detail);
                    }
                }
                else if (_SurfaceKind > 1.5)
                {
                    float leafClusters = Grain(xz * 0.91 + input.positionWS.y * float2(0.48, -0.85));
                    albedo *= lerp(0.77, 1.22, leafClusters);
                }

                float lambert = saturate(dot(normal, _SunDirection.xyz));
                half occlusion = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(input.positionCS)).indirectAmbientOcclusion;
                half3 ambient = half3(0.35, 0.46, 0.58) * lerp(0.72, 1.0, saturate(normal.y)) * max(0.42, occlusion);
                half3 color = albedo * (ambient + _Sun.rgb * (0.19 + 0.98 * lambert));
                float gloss = pow(saturate(dot(normalize(view + _SunDirection.xyz), normal)), lerp(50.0, 8.0, roughness));
                color += _Sun.rgb * gloss * (1.0 - roughness) * 0.075;
                if (_SurfaceKind > 0.5 && _SurfaceKind < 1.5)
                {
                    float grazing = pow(1.0 - saturate(view.y + (macro - 0.5) * 0.064), 4.6);
                    color = lerp(albedo * 0.49, _Horizon.rgb * 0.55, grazing * 0.68);
                }
                float clearAir = exp(-depth * _DistantHaze);
                return half4(lerp(_Horizon.rgb, color, clearAir), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Depth
            half Depth(Varyings input) : SV_Target { return input.positionCS.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment DepthNormals
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            half4 DepthNormals(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct = saturate(PackNormalOctQuadEncode(normal) * 0.5 + 0.5);
                    return half4(PackFloat2To888(oct), 0);
                #else
                    return half4(normal, 0);
                #endif
            }
            ENDHLSL
        }
    }
}
