Shader "Vastcore/Weather/Sample Surface"
{
    Properties
    {
        _Horizon ("Horizon", Color) = (0.67,0.72,0.77,1)
        _Sun ("Sunlight", Color) = (1,0.89,0.73,1)
        _SunDirection ("Sun direction", Vector) = (-0.7,0.35,0.6,0)
        _DistantHaze ("Haze per metre", Float) = 0.000018
        _SurfaceKind ("Surface kind", Float) = 0
        _Wetness ("Wetness", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "RenderType"="Opaque" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _Horizon;
            float4 _Sun;
            float4 _SunDirection;
            float _DistantHaze;
            float _SurfaceKind;
            float _Wetness;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

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
                float3 normalWS : TEXCOORD1;
                half3 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color.rgb;
                return output;
            }

            float Hash(float2 uv)
            {
                float3 cell = frac(float3(uv.xyx) * 0.1031);
                cell += dot(cell, cell.yzx + 33.33);
                return frac((cell.x + cell.y) * cell.z);
            }

            float Grain(float2 uv)
            {
                float2 baseCell = floor(uv);
                float2 blend = smoothstep(0.0, 1.0, frac(uv));
                float a = lerp(Hash(baseCell), Hash(baseCell + float2(1, 0)), blend.x);
                float b = lerp(Hash(baseCell + float2(0, 1)), Hash(baseCell + 1.0), blend.x);
                return lerp(a, b, blend.y) - 0.5;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 worldPosition = input.positionWS;
                float3 normal = normalize(input.normalWS);
                float3 view = GetCameraPositionWS() - worldPosition;
                float distanceToEye = length(view);
                view /= max(distanceToEye, 0.001);
                float sun = saturate(dot(normal, _SunDirection.xyz));
                float3 light = lerp(float3(0.56, 0.68, 0.86), _Sun.rgb * 1.21 + 0.24, sun);
                float stripe = sin(worldPosition.x * 0.112 + worldPosition.z * 0.173)
                    * sin(worldPosition.z * 0.095 - worldPosition.x * 0.074);
                float nearbyGrain = Grain(worldPosition.xz * 38.0) * (1.0 - smoothstep(5.0, 19.0, distanceToEye));
                float blotches = Grain(worldPosition.xz * 0.74) * (1.0 - smoothstep(37.0, 100.0, distanceToEye));
                float3 color = input.color * light * (0.99 + stripe * 0.055 + nearbyGrain * 0.19 + blotches * 0.12);
                if (_SurfaceKind > 1.5)
                {
                    float grazing = pow(1.0 - saturate(view.y), 5.1) * (0.09 + stripe * 0.015) * _Wetness;
                    color = color * (1.0 - _Wetness * 0.43) + grazing * _Horizon.rgb;
                }
                else if (_SurfaceKind > 0.5)
                {
                    float wave = stripe * 0.31 + sin(worldPosition.x * 0.44 + worldPosition.z * 0.79) * 0.08;
                    float attenuation = 1.0 - smoothstep(80.0, 2300.0, distanceToEye);
                    float fresnel = pow(1.0 - saturate(view.y + wave * 0.11 * attenuation), 5.5);
                    float sunGlint = pow(saturate(dot(reflect(-view, normal), _SunDirection.xyz)), 88.0);
                    color = lerp(input.color * (0.70 + 0.53 * wave * attenuation), _Horizon.rgb * 0.60, fresnel * 0.62)
                        + _Sun.rgb * sunGlint * 0.26;
                }
                float air = exp(-distanceToEye * _DistantHaze);
                return half4(lerp(_Horizon.rgb, color, air), 1.0);
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
