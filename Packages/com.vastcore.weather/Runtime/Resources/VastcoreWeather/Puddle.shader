Shader "Vastcore/Weather/Puddle"
{
    Properties
    {
        _WetGround ("Wet ground", Color) = (0.1,0.11,0.13,1)
        _ReflectedSky ("Reflected sky", Color) = (0.55,0.64,0.72,1)
        [HideInInspector] _PlanarReflection ("Scene reflection", 2D) = "black" {}
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_PlanarReflection);
            SAMPLER(sampler_PlanarReflection);

            CBUFFER_START(UnityPerMaterial)
                float4 _WetGround;
                float4 _ReflectedSky;
                float _Wetness;
                float _RainIntensity;
                float _WeatherTime;
                float _OutlineSeed;
                float _GroundDefinesShore;
                float _HasPlanarReflection;
                float4x4 _ReflectionViewProjection;
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

            float2 RainRings(float2 meters, bool basin)
            {
                float2 grid = meters * (basin ? 8.6 : 4.3);
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
                        float radius = basin ? lerp(0.33, 0.89, chance) : 0.82;
                        float ring = distanceToDrop - age * radius;
                        float band = (1.0 - smoothstep(0.019, 0.14, abs(ring))) *
                            smoothstep(0.0, 0.12, age) * (1.0 - smoothstep(0.38, 1.0, age));
                        float hit = smoothstep(chance * 0.91, chance * 0.91 + 0.10, _RainIntensity);
                        slope += (delta / max(distanceToDrop, 0.01)) * sin(ring * 39.0) * band * hit;
                    }
                }
                return slope;
            }

            float3 ReflectScene(float3 worldPosition, float2 rings, float3 sky)
            {
                if (_HasPlanarReflection < 0.5)
                    return sky;
                float4 clipPosition = mul(_ReflectionViewProjection, float4(worldPosition, 1.0));
                if (clipPosition.w <= 0.0)
                    return sky;

                float2 screen = clipPosition.xy / clipPosition.w * 0.5 + 0.5;
                #if UNITY_UV_STARTS_AT_TOP
                    screen.y = 1.0 - screen.y;
                #endif
                screen += rings * float2(0.004, 0.009);
                float2 border = min(screen, 1.0 - screen);
                float coverage = smoothstep(0.0, 0.035, min(border.x, border.y));
                float3 reflection = SAMPLE_TEXTURE2D(_PlanarReflection, sampler_PlanarReflection, saturate(screen)).rgb;
                return lerp(sky, reflection, coverage);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                bool basin = _GroundDefinesShore > 0.5;
                float2 p = input.uv * 2.0 - 1.0;
                float outline = length(p * float2(1.05, 1.01));
                outline += sin(11.7 * p.x + 6.2 * p.y + _OutlineSeed) * 0.075
                    + sin(14.2 * p.y - 5.1 * p.x + _OutlineSeed * 1.17) * 0.068
                    + sin(18.0 * (p.x - p.y) + _OutlineSeed) * 0.025;
                float fill = 1.0 - smoothstep(0.43 + _Wetness * 0.40, 0.48 + _Wetness * 0.40, outline);
                float damp = (1.0 - smoothstep(0.74, 1.0, outline)) * smoothstep(0.03, 0.33, _Wetness);
                float3 view = GetCameraPositionWS() - input.positionWS;
                float distanceToEye = length(view);
                view /= max(distanceToEye, 0.001);
                if (basin)
                {
                    // The depth test contains the pool. Fade the remaining water into the pavement at its physical edge.
                    float2 screen = input.positionCS.xy / _ScaledScreenParams.xy;
                    float groundDepth = LinearEyeDepth(SampleSceneDepth(screen), _ZBufferParams);
                    float surfaceDepth = -TransformWorldToView(input.positionWS).z;
                    float verticalDepth = max(0.0, groundDepth - surfaceDepth) *
                        saturate(view.y) / max(0.2, dot(-view, GetViewForwardDir()));
                    fill = smoothstep(0.00025, 0.012, verticalDepth);
                    damp = 0.0;
                }
                clip(max(fill, damp) - 0.002);
                float ripplesVisible = (1.0 - smoothstep(7.0, 18.0, distanceToEye)) * fill;
                float2 rings = _RainIntensity > 0.001 && ripplesVisible > 0.002
                    ? RainRings(input.positionWS.xz, basin) * ripplesVisible
                    : 0.0;
                float rippleStrength = basin ? 0.034 : 0.095;
                float3 normal = normalize(float3(-rings.x * rippleStrength, 1.0, -rings.y * rippleStrength));
                float fresnel = pow(1.0 - saturate(dot(normal, view)), 3.2);
                float3 reflected = reflect(-view, normal);
                float gleam = 0.60 + 0.4 * smoothstep(-0.06, 0.20, reflected.y);
                float3 sky = _ReflectedSky.rgb * gleam;
                float3 environment = basin ? ReflectScene(input.positionWS, rings, sky) : sky;
                float3 water = lerp(_WetGround.rgb, environment, 0.21 + fresnel * 0.76);
                water += (rings.x - rings.y) * _ReflectedSky.rgb * (basin ? 0.052 : 0.16);
                float alpha = max(damp * 0.50, fill * smoothstep(0.05, 0.35, _Wetness) * 0.91);
                return half4(lerp(_WetGround.rgb, water, fill), alpha);
            }
            ENDHLSL
        }
    }
}
