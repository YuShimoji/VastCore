Shader "Vastcore/Weather/Street Paving"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _NormalMap ("Normal", 2D) = "bump" {}
        _RoughnessMap ("Roughness", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _Horizon ("Overcast horizon", Color) = (0.63,0.66,0.67,1)
        _TileMetres ("Tile size (m)", Float) = 2.0
        _NormalStrength ("Normal strength", Range(0,2)) = 0.8
        _TextureWeight ("Albedo texture weight", Range(0,1)) = 1
        _Exposure ("Diffuse level", Range(0,2)) = 1
        _Repair ("Repair center XZ and half size", Vector) = (0,0,0,0)
        [HideInInspector] _PlanarReflection ("Runoff reflection", 2D) = "black" {}
        [Toggle] _NormalStoredAsColor ("Normal stored as sRGB", Float) = 0
        [Toggle] _RoughnessStoredAsColor ("Roughness stored as sRGB", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_RoughnessMap);
            SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_PlanarReflection);
            SAMPLER(sampler_PlanarReflection);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _Horizon;
                float4 _WetBounds;
                float4 _WetCorners;
                float4 _Repair;
                float4 _RunoffCenter;
                float4x4 _ReflectionViewProjection;
                float _TileMetres;
                float _NormalStrength;
                float _TextureWeight;
                float _Exposure;
                float _NormalStoredAsColor;
                float _RoughnessStoredAsColor;
                float _HasPlanarReflection;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 positionWS = input.positionWS;
                float3 baseNormal = normalize(input.normalWS);
                bool vertical = abs(baseNormal.y) < 0.45;
                bool alongStreet = vertical && abs(baseNormal.x) > abs(baseNormal.z);
                float2 uv = (vertical ? (alongStreet ? positionWS.zy : positionWS.xy) : positionWS.xz) / max(0.1, _TileMetres);
                float2 wetUV = saturate((positionWS.xz - _WetBounds.xy) * _WetBounds.zw);
                float wetness = lerp(lerp(_WetCorners.x, _WetCorners.y, wetUV.x),
                    lerp(_WetCorners.z, _WetCorners.w, wetUV.x), wetUV.y);
                half roughness = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uv).r;
                half3 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
                half3 albedo = lerp(half3(1.0, 1.0, 1.0), texel, _TextureWeight) * _BaseColor.rgb;
                half4 normalSample = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv);
                half strength = _NormalStrength * lerp(1.0, 0.56, wetness);
                half3 tangentNormal = UnpackNormalScale(normalSample, strength);
                if (_NormalStoredAsColor > 0.5)
                {
                    // Preserve the import settings of the older project texture set.
                    #ifndef UNITY_COLORSPACE_GAMMA
                        normalSample.rgb = LinearToSRGB(normalSample.rgb);
                    #endif
                    tangentNormal.xy = (normalSample.xy * 2.0 - 1.0) * strength;
                    tangentNormal.z = sqrt(saturate(1.0 - dot(tangentNormal.xy, tangentNormal.xy)));
                }
                #ifndef UNITY_COLORSPACE_GAMMA
                    if (_RoughnessStoredAsColor > 0.5)
                        roughness = LinearToSRGB(roughness);
                #endif
                if (_Repair.z > 0.0)
                {
                    float2 patchUV = abs(positionWS.xz - _Repair.xy) / max(0.01, _Repair.zw);
                    float waver = sin(positionWS.z * 16.71) * 0.012 + sin(positionWS.x * 27.9) * 0.014;
                    float edgeDistance = max(patchUV.x, patchUV.y) + waver;
                    float patch = 1.0 - smoothstep(0.91, 1.02, edgeDistance);
                    float seam = 1.0 - smoothstep(0.009, 0.028, abs(edgeDistance - 0.999));
                    albedo *= lerp(1.0, 0.78, patch) * (1.0 - seam * 0.20);
                    roughness = lerp(roughness, roughness * 0.70, patch);
                }
                float film = saturate(wetness * 1.23) * lerp(0.32, 1.0, saturate((1.0 - roughness) * 1.55));
                float3 axis = alongStreet ? float3(0,0,1) : float3(1,0,0);
                float3 tangent = normalize(axis - baseNormal * dot(baseNormal, axis));
                float3 bitangent = normalize(cross(tangent, baseNormal));
                float3 normal = normalize(tangent * tangentNormal.x + bitangent * tangentNormal.y + baseNormal * tangentNormal.z);
                roughness = max(0.18, roughness * lerp(1.0, 0.50, film));

                InputData lighting = (InputData)0;
                lighting.positionWS = positionWS;
                lighting.normalWS = normal;
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(positionWS);
                lighting.shadowCoord = TransformWorldToShadowCoord(positionWS);
                lighting.bakedGI = lerp(SampleSH(normal), _Horizon.rgb * 0.68, 0.62);
                lighting.shadowMask = 1.0;
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo * _Exposure * lerp(1.0, 0.63 + roughness * 0.18, wetness);
                surface.smoothness = 1.0 - roughness;
                surface.normalTS = tangentNormal;
                surface.occlusion = 1.0;
                surface.alpha = 1.0;
                half3 color = UniversalFragmentPBR(lighting, surface).rgb;

                float grazing = pow(1.0 - saturate(dot(normal, lighting.viewDirectionWS)), 4.2);
                float sheen = grazing * film * saturate(baseNormal.y);
                color += _Horizon.rgb * sheen * (0.17 + 0.28 * (1.0 - roughness));

                if (_HasPlanarReflection > 0.5 && sheen > 0.005)
                {
                    // Only the low asphalt around the drain carries a coherent, blurred reflection.
                    float2 distanceFromDrain = abs(positionWS.xz - _RunoffCenter.xz);
                    float runoff = (1.0 - smoothstep(0.4, 3.75, distanceFromDrain.x)) *
                        (1.0 - smoothstep(3.6, 10.8, distanceFromDrain.y));
                    if (runoff > 0.001)
                    {
                        float3 planePosition = float3(positionWS.x, _RunoffCenter.y, positionWS.z);
                        float4 clipPosition = mul(_ReflectionViewProjection, float4(planePosition, 1.0));
                        float2 screen = clipPosition.xy / max(0.01, clipPosition.w) * 0.5 + 0.5;
                        #if UNITY_UV_STARTS_AT_TOP
                            screen.y = 1.0 - screen.y;
                        #endif
                        screen += tangentNormal.xy * 0.008;
                        float2 border = min(screen, 1.0 - screen);
                        float coverage = smoothstep(0.0, 0.07, min(border.x, border.y));
                        half3 reflected = SAMPLE_TEXTURE2D_LOD(_PlanarReflection, sampler_PlanarReflection,
                            saturate(screen), 1.6 + roughness * 3.5).rgb;
                        color = lerp(color, reflected, sheen * runoff * coverage * 0.53);
                    }
                }
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode"="DepthOnly" }
            ColorMask R
            HLSLPROGRAM
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 DepthVertex(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }
            half DepthFragment(float4 positionCS : SV_POSITION) : SV_Target
            {
                return positionCS.z;
            }
            ENDHLSL
        }
    }
}
