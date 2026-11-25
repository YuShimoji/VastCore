// 地域の 3D 表示（3D 化の試作）用。真上から見た地面の画像を貼り、急な面は岩の色にする。
// 頂点カラー: R = 空の開け具合（真上が空なら 1、岩の下・割れ目の中で小さい）。G = 日が当たる度合い（先に計算した影。影の中で 0）。
//             B = 1 から「岩肌の色にする度合い」を引いた値（崖の面と岩は 0）。A = 岩肌の色のむら（1 で元の色、0 で 0.66 倍）。
// 頂点カラーの無いメッシュは全部 1 として描かれる（空は開け、影は無く、岩肌は傾きだけで決まる）。
// 影を落とす・深度を書くパスは URP 標準の Lit のものを借りる。
Shader "Vastcore/RegionTerrain"
{
    Properties
    {
        _BaseMap ("Ground Map (top-down)", 2D) = "white" {}
        _RockColor ("Rock Color", Color) = (0.59, 0.553, 0.502, 1)
        _RockStart ("Rock Starts (normal Y)", Range(0, 1)) = 0.766
        _RockFull ("Rock Only (normal Y)", Range(0, 1)) = 0.53
        _BandAmount ("Strata Band Amount", Range(0, 0.3)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _RockColor;
                half _RockStart;
                half _RockFull;
                half _BandAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(position.positionCS.z);
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normal = normalize(input.normalWS);
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;

                // 急な面と、頂点が岩と指定した所を、岩の色にする。岩には、高さに沿った地層の縞を薄く入れる
                half rock = max(saturate((_RockStart - normal.y) / max(0.001, _RockStart - _RockFull)), 1.0 - input.color.b);
                half band = (1.0 - _BandAmount) + _BandAmount * sin(input.positionWS.y * 0.55 + 1.3 * sin(input.positionWS.y * 0.13));
                band *= 0.66 + 0.34 * input.color.a;
                albedo = lerp(albedo, _RockColor.rgb * band, rock);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light light = GetMainLight(shadowCoord);
                // 影は、Unity の影（近くだけ届く）と、頂点に持たせた影の、暗い方
                half direct = saturate(dot(normal, light.direction)) * min(light.shadowAttenuation, input.color.g);
                // 岩の下では、空からの光も減らす
                half3 ambient = SampleSH(normal) * (0.35 + 0.65 * input.color.r);
                half3 color = albedo * (light.color * direct + ambient);
                color = MixFog(color, input.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
