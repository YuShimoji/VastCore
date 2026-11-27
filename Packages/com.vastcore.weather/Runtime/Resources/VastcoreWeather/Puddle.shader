Shader "Vastcore/Weather/Puddle"
{
    Properties
    {
        _WetGround ("Wet ground", Color) = (0.1,0.11,0.13,1)
        _ReflectedSky ("Reflected sky", Color) = (0.55,0.64,0.72,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-20" "RenderType"="Transparent" }
        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _WetGround;
                float4 _ReflectedSky;
                float _Wetness;
                float _RainIntensity;
                float _WeatherTime;
                float _OutlineSeed;
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
                float3 positionWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                return output;
            }

            float Hash(float2 p)
            {
                float3 h = frac(float3(p.xyx) * 0.1031);
                h += dot(h, h.yzx + 33.33);
                return frac((h.x + h.y) * h.z);
            }

            float2 RainRings(float2 meters)
            {
                float2 grid = meters * 4.3;
                float2 baseCell = floor(grid);
                float2 slope = 0.0;
                [unroll]
                for (int y = -1; y <= 1; y++)
                {
                    [unroll]
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 cell = baseCell + float2(x, y);
                        float start = Hash(cell + 8.7);
                        float time = _WeatherTime * lerp(0.9, 1.9, start) + start * 22.0;
                        float age = frac(time);
                        float chance = Hash(cell + floor(time) * 0.47);
                        float2 center = cell + lerp(0.12, 0.88, float2(start, Hash(cell + 37.3)));
                        float2 delta = grid - center;
                        float distanceToDrop = length(delta);
                        float ring = distanceToDrop - age * 0.82;
                        float band = (1.0 - smoothstep(0.019, 0.14, abs(ring))) *
                            smoothstep(0.0, 0.12, age) * (1.0 - smoothstep(0.38, 1.0, age));
                        float hit = smoothstep(chance * 0.91, chance * 0.91 + 0.10, _RainIntensity);
                        slope += (delta / max(distanceToDrop, 0.01)) * sin(ring * 39.0) * band * hit;
                    }
                }
                return slope;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float outline = length(p * float2(1.05, 1.01));
                outline += sin(11.7 * p.x + 6.2 * p.y + _OutlineSeed) * 0.075
                    + sin(14.2 * p.y - 5.1 * p.x + _OutlineSeed * 1.17) * 0.068
                    + sin(18.0 * (p.x - p.y) + _OutlineSeed) * 0.025;
                float fill = 1.0 - smoothstep(0.43 + _Wetness * 0.40, 0.48 + _Wetness * 0.40, outline);
                float damp = (1.0 - smoothstep(0.74, 1.0, outline)) * smoothstep(0.03, 0.33, _Wetness);
                clip(max(fill, damp) - 0.002);
                float3 view = GetCameraPositionWS() - input.positionWS;
                float ripplesVisible = (1.0 - smoothstep(7.0, 18.0, length(view))) * fill;
                float2 rings = _RainIntensity > 0.001 && ripplesVisible > 0.002
                    ? RainRings(input.positionWS.xz) * ripplesVisible
                    : 0.0;
                float3 normal = normalize(float3(-rings.x * 0.095, 1.0, -rings.y * 0.095));
                view = normalize(view);
                float fresnel = pow(1.0 - saturate(dot(normal, view)), 3.2);
                float3 reflected = reflect(-view, normal);
                float gleam = 0.60 + 0.4 * smoothstep(-0.06, 0.20, reflected.y);
                float3 water = lerp(_WetGround.rgb, _ReflectedSky.rgb * gleam, 0.21 + fresnel * 0.76);
                water += (rings.x - rings.y) * _ReflectedSky.rgb * 0.16;
                float alpha = max(damp * 0.50, fill * smoothstep(0.05, 0.35, _Wetness) * 0.91);
                return half4(lerp(_WetGround.rgb, water, fill), alpha);
            }
            ENDHLSL
        }
    }
}
