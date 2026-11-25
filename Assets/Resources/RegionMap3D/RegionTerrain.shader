// 地域の 3D 表示（3D 化の試作）用。真上から見た地面の画像を貼り、急な面は岩の色にする。
// 頂点カラー: R = 真上が空なら 1、岩の下（張り出しの下・洞窟の中）なら 0。G = 近くの岩の影でなければ 1。
// 影を落とす・深度を書くパスは URP 標準の Lit のものを借りる。
Shader "Vastcore/RegionTerrain"
{
    Properties
    {
        _BaseMap ("Ground Map (top-down)", 2D) = "white" {}
        _RockColor ("Rock Color", Color) = (0.59, 0.553, 0.502, 1)
        _RockStart ("Rock Starts (normal Y)", Range(0, 1)) = 0.766
        _RockFull ("Rock Only (normal Y)", Range(0, 1)) = 0.53
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

                // 急な面ほど岩の色にする。岩には、高さに沿った地層の縞を薄く入れる
                half rock = saturate((_RockStart - normal.y) / max(0.001, _RockStart - _RockFull));
                half band = 0.9 + 0.1 * sin(input.positionWS.y * 0.55 + 1.3 * sin(input.positionWS.y * 0.13));
                albedo = lerp(albedo, _RockColor.rgb * band, rock);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light light = GetMainLight(shadowCoord);
                // 頂点の「近くの岩の影」は、岩の下でだけ使う（開けた崖の面は Unity の影に任せ、周りの地形と同じ明るさにする）
                half direct = saturate(dot(normal, light.direction)) * light.shadowAttenuation * max(input.color.r, input.color.g);
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
