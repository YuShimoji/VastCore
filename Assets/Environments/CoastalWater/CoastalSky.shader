Shader "Vastcore/Water/Sample Sky"
{
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.vastcore.water/Runtime/Shaders/WaterLighting.hlsl"

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings Vert(float3 position : POSITION)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(position);
                output.direction = position;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(WaterSky(normalize(input.direction)), 1.0);
            }
            ENDHLSL
        }
    }
}
