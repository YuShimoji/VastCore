// 地域の 3D 表示（3D 化の試作）用。真上から見た地面の画像を貼り、急な面は岩の色にする。
// 砂岩の台地では RegionSandstone.hlsl が、乾いた陸と転石に地層・岩屑・砂地・地被を描く。
// 頂点カラー: R = 空の開け具合（真上が空なら 1、岩の下・割れ目の中で小さい）。G = 日が当たる度合い（先に計算した影。影の中で 0）。
//             B = 1 から「岩肌の色にする度合い」を引いた値（崖の面と岩は 0）。A = 岩肌の色のむら（1 で元の色、0 で 0.66 倍）。
// 頂点カラーの無いメッシュは全部 1 として描かれる（空は開け、影は無く、岩肌は傾きだけで決まる）。
// 影を落とす・深度を書くパスは URP 標準の Lit のものを借りる。
// 光は線形の値で計算し、空気の霞（RegionAtmosphere.hlsl）を重ねてから sRGB で書く。
Shader "Vastcore/RegionTerrain"
{
    Properties
    {
        _BaseMap ("Ground Map (top-down)", 2D) = "white" {}
        _RockColor ("Rock Color", Color) = (0.59, 0.553, 0.502, 1)
        _RockStart ("Rock Starts (normal Y)", Range(0, 1)) = 0.766
        _RockFull ("Rock Only (normal Y)", Range(0, 1)) = 0.53
        _BandAmount ("Strata Band Amount", Range(0, 0.3)) = 0.1
        [HideInInspector] _LooseRock ("Detached rock", Float) = 0
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
            #pragma multi_compile_local_fragment _ REGION_SANDSTONE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "RegionAtmosphere.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _RockColor;
                half _RockStart;
                half _RockFull;
                half _BandAmount;
                half _LooseRock;
            CBUFFER_END

            #if defined(REGION_SANDSTONE)
            #include "RegionSandstone.hlsl"
            #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 surface : TEXCOORD1;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half4 color : COLOR;
                float4 surface : TEXCOORD4;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.color = input.color;
                output.surface = input.surface;
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

                #if defined(REGION_SANDSTONE)
                float3 positionOS = TransformWorldToObject(input.positionWS);
                float3 normalOS = normalize(TransformWorldToObjectNormal(normal));
                half3 sandstone = SandstoneSurface(positionOS, normalOS, input.surface, input.color.a, _LooseRock);
                albedo = lerp(albedo, sandstone, max(input.surface.w, _LooseRock));
                #endif

                albedo = RegionToLinear(albedo);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light light = GetMainLight(shadowCoord);
                AmbientOcclusionFactor occlusion = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(input.positionCS));
                // 影は、Unity の影（近くだけ届く）と、頂点に持たせた影の、暗い方
                half sunlit = min(light.shadowAttenuation, input.color.g);
                half direct = saturate(dot(normal, light.direction)) * sunlit * occlusion.directAmbientOcclusion;
                // 岩の下では、空からの光も減らす。影の中の空の光は、空の青を強めに受ける
                half open = 0.35 + 0.65 * input.color.r;
                half3 tint = dot(_RegionShadowTint.rgb, 1.0) > 0.0 ? lerp(_RegionShadowTint.rgb, 1.0, sunlit) : 1.0;
                half3 ambient = SampleSH(normal) * open * tint;
                // 日の当たる地面と向かいの岩壁からの照り返し。下や横を向いた面、影の中ほど受ける
                half3 bounce = _RegionBounceColor.rgb * saturate(0.55 - 0.45 * normal.y) * (0.45 + 0.55 * (1.0 - sunlit)) * (0.5 + 0.5 * open);
                half3 color = albedo * (light.color * direct + (ambient + bounce) * occlusion.indirectAmbientOcclusion);
                color = RegionApplyAir(color, input.positionWS);
                return RegionOutput(color, 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
