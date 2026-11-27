Shader "Vastcore/Weather/Sample Sky"
{
    Properties
    {
        _Zenith ("Zenith", Color) = (0.27,0.47,0.67,1)
        _Horizon ("Horizon", Color) = (0.70,0.76,0.81,1)
        _Sun ("Sunlight", Color) = (1,0.80,0.62,1)
        _SunDirection ("Sun direction", Vector) = (-0.7,0.35,0.6,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Zenith;
                float4 _Horizon;
                float4 _Sun;
                float4 _SunDirection;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 view = normalize(input.direction);
                float warm = 1.0 - smoothstep(0.08, 0.37, _SunDirection.y);
                float haze = exp(-max(0.0, view.y) * 6.2);
                float nearSun = saturate(dot(view, normalize(_SunDirection.xyz)));
                float3 edge = lerp(_Horizon.rgb, _Sun.rgb, warm * pow(1.0 - abs(view.y), 7.0) * 0.30);
                float3 color = lerp(_Zenith.rgb, edge, haze);
                color += _Sun.rgb * (pow(nearSun, 12.0) * 0.25 + pow(nearSun, 320.0) * 0.30);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
