Shader "Vastcore/Weather/Precipitation"
{
    Properties
    {
        _RenderBolt ("Lightning geometry", Float) = 0
        _BoltColor ("Lightning color", Color) = (0.72,0.83,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+40" "RenderType"="Transparent" "DisableBatching"="True" }
        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4x4 _WeatherWorldToLocal;
                float4 _RainRadius;
                float4 _BoltColor;
                float3 _RainCenter;
                float3 _RainWind;
                float3 _RainObserver;
                float _WeatherTime;
                float _RainIntensity;
                float _DropVisibility;
                float _WeatherSeed;
                float _RenderBolt;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half opacity : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.uv = input.uv;
                if (_RenderBolt > 0.5)
                {
                    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                    output.opacity = _BoltColor.a;
                    return output;
                }

                float3 seed = input.positionOS.xyz;
                float variant = frac(dot(seed, float3(81.7, 138.1, 12.8)) + _WeatherSeed * 0.13);
                float3 motion = _RainWind + float3(0, -8.4 - 3.0 * variant, 0);
                float3 box = float3(36, 23, 36);
                float3 grid = floor(_RainObserver / box + 0.5) * box;
                float3 p = grid + (frac(seed + (_WeatherTime * motion - grid) / box) - 0.5) * box;
                // Wrap relative to the observer without translating the drops when the camera moves.
                p += round((_RainObserver - p) / box) * box;
                float3 local = mul(_WeatherWorldToLocal, float4(p, 1)).xyz - _RainCenter;
                float radius = length(local.xz / max(_RainRadius.xy, 1.0));
                float strength = _RainIntensity * (1.0 - smoothstep(0.42, 1.0, radius));
                float3 relative = p - GetCameraPositionWS();
                float distanceToEye = length(relative);
                float3 side = normalize(cross(motion, relative) + float3(0.0001, 0, 0));
                float width = min(0.021, 0.007 + distanceToEye * 0.00062);
                p += motion * (input.uv.y - 0.5) * lerp(0.037, 0.079, variant) + side * input.uv.x * width;
                output.positionCS = TransformWorldToHClip(p);
                output.opacity = smoothstep(0.55, 2.0, distanceToEye) * (1.0 - smoothstep(12.0, 18.0, distanceToEye)) *
                    smoothstep(variant * 0.82, variant * 0.82 + 0.13, strength) * 0.52 * _DropVisibility;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                if (_RenderBolt > 0.5)
                    return half4(_BoltColor.rgb * _BoltColor.a * 1.8, _BoltColor.a);
                half falloff = saturate(1.0 - abs(input.uv.x)) * sin(input.uv.y * 3.14159);
                half opacity = input.opacity * falloff;
                return half4(half3(0.61, 0.72, 0.83) * opacity, opacity);
            }
            ENDHLSL
        }
    }
}
