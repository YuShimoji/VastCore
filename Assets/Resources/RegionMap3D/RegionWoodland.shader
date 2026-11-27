Shader "Vastcore/RegionWoodland"
{
    Properties { _Ground ("Ground ribbon", Float) = 0 }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry+5" }
        Cull Off
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "RegionAtmosphere.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Ground;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
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

            float Grain(float2 position)
            {
                float3 p = frac(float3(position.xyx) * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float Noise(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Grain(cell), Grain(cell + float2(1,0)), f.x),
                    lerp(Grain(cell + float2(0,1)), Grain(cell + float2(1,1)), f.x), f.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 n = normalize(input.normalWS);
                half3 tint = input.color.rgb;
                if (_Ground > 0.5h)
                {
                    float2 p = input.positionWS.xz;
                    float footprint = max(length(ddx(p)), length(ddy(p)));
                    float grain = Noise(p * 20.0) * (1.0h - smoothstep(0.03, 0.13, footprint));
                    float mottling = Noise(p * 4.7 + float2(0.2, 1.1));
                    clip(input.color.a - lerp(0.21, 0.74, Noise(p * 5.5)));
                    tint *= 0.82h + grain * 0.16h + mottling * 0.25h;
                }
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half up = n.y * 0.5h + 0.5h;
                half direct = 0.77h * saturate(dot(n, sun.direction) * 0.83h + 0.17h) * sun.shadowAttenuation;
                half3 illumination = lerp(half3(0.26h, 0.28h, 0.25h), half3(0.50h, 0.53h, 0.45h), up) + direct * sun.color;
                half3 surface = RegionDisplayToLinear(tint) * illumination;
                return RegionOutput(RegionApplyAir(surface, input.positionWS), 1.0h);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
