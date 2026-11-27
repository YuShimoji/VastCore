Shader "Vastcore/RegionMap/Road Margin"
{
    Properties
    {
        _Gravel ("Gravel", 2D) = "white" {}
        _Soil ("Loam", 2D) = "white" {}
        _Turf ("Turf", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent-10" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Gravel); SAMPLER(sampler_Gravel);
            TEXTURE2D(_Soil); SAMPLER(sampler_Soil);
            TEXTURE2D(_Turf); SAMPLER(sampler_Turf);

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 edge : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 edge : TEXCOORD2;
                half fog : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fog = ComputeFogFactor(position.positionCS.z);
                output.edge = input.edge;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 world = input.positionWS.xz;
                float fringe = 0.44 * sin(0.27 * world.x + 0.63 * world.y) + 0.18 * sin(1.01 * world.x - 1.89 * world.y);
                half alpha = input.edge.y * (1.0h - smoothstep(3.75, 5.5, input.edge.x + fringe));
                clip(alpha - 0.006h);
                half3 gravel = SAMPLE_TEXTURE2D(_Gravel, sampler_Gravel, world * 0.48).rgb * half3(0.73h, 0.70h, 0.65h);
                half3 loam = SAMPLE_TEXTURE2D(_Soil, sampler_Soil, world * float2(0.42, 0.32)).rgb;
                half3 turf = SAMPLE_TEXTURE2D(_Turf, sampler_Turf, world / 3.3).rgb * half3(0.55h, 0.68h, 0.48h);
                half grass = smoothstep(1.95, 4.0, input.edge.x + fringe);
                half soil = smoothstep(0.65, 2.65, input.edge.x - fringe);
                half3 baseColor = lerp(lerp(gravel, loam, soil), turf, grass);
                half3 normal = normalize(input.normalWS);
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 light = SampleSH(normal) + sun.color * (saturate(dot(normal, sun.direction)) * sun.shadowAttenuation);
                return half4(MixFog(baseColor * light, input.fog), alpha);
            }
            ENDHLSL
        }
    }
}
