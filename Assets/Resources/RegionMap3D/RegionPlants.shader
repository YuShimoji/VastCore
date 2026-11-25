Shader "Vastcore/RegionPlants"
{
    Properties
    {
        [MainTexture] _BaseMap ("Plant atlas", 2D) = "white" {}
        [HideInInspector] _BaseColor ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Cutoff", Range(0,1)) = 0.34
        [HideInInspector] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            AlphaToMask On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Cutoff;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 light : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; half3 lightFog : TEXCOORD2; };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.uv = input.uv;
                o.lightFog = half3(input.light.rg, ComputeFogFactor(p.positionCS.z));
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                half4 leaf = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                clip(leaf.a - _Cutoff);
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half direct = min(sun.shadowAttenuation, i.lightFog.x) * saturate(0.30h + 0.55h * sun.direction.y);
                half3 sky = SampleSH(half3(0,1,0)) * (0.4h + 0.6h * i.lightFog.y);
                return half4(MixFog(leaf.rgb * (sun.color * direct + sky), i.lightFog.z), leaf.a);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
