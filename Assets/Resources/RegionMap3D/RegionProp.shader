// DS-011 の Props の実モデル用（RegionPropView の実アセット差し替え口）。写真素材の色・法線・粗さを貼り、
// V7 の環境順応を受ける: 上を向いた面に砂塵（TopDust）、地面の近くを地面の色へ（Ground Skirt Blend）。
// 個体ごとの値は MaterialPropertyBlock で渡す（_PropGround: xyz 足元の地面の高さ m と傾き dY/dx・dY/dz（Unity 座標）、w 帯の高さ m。
// _PropParams: x 砂塵の量、y 明るさの倍率（V4）、z 色の暖かさ（-1〜1）、w 未使用）。渡さない時は帯なし・砂塵なし・倍率 1。
// 光と空気は RegionTerrain と同じ（線形で計算し、霞を重ねて書く）。影・深度のパスは URP の Lit から借りる。
Shader "Vastcore/RegionProp"
{
    Properties
    {
        [MainTexture] _BaseMap ("Color", 2D) = "white" {}
        [Normal][NoScaleOffset] _NormalMap ("Normal (GL)", 2D) = "bump" {}
        [NoScaleOffset] _RoughnessMap ("Roughness", 2D) = "white" {}
        _BaseColor ("Tint (linear)", Color) = (1,1,1,1)
        _NormalStrength ("Normal strength", Range(0, 2)) = 1
        _Roughness ("Roughness (no map)", Range(0.05, 1)) = 0.85
        [Toggle] _UseRoughnessMap ("Use roughness map", Float) = 0
        _Metallic ("Metallic", Range(0, 1)) = 0
        _UvScale ("UV scale", Float) = 1
        [HideInInspector] _PropGround ("Ground plane and skirt (per instance)", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PropParams ("Dust, value, warmth, moss (per instance)", Vector) = (0, 1, 0, 0)
        [HideInInspector] _PropGroundColor ("Ground color (display sRGB, not converted)", Vector) = (0.72, 0.56, 0.40, 1)
        [HideInInspector] _PropMossColor ("Moss color (display sRGB, not converted)", Vector) = (0.22, 0.34, 0.14, 1)
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
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "RegionAtmosphere.hlsl"
            #include "RegionShadows.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_RoughnessMap); SAMPLER(sampler_RoughnessMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _NormalStrength;
                half _Roughness;
                half _UseRoughnessMap;
                half _Metallic;
                float _UvScale;
                float4 _PropGround;
                float4 _PropParams;
                float4 _PropGroundColor;
                float4 _PropMossColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normal = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = normal.normalWS;
                output.tangentWS = half4(normal.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap) * _UvScale;
                return output;
            }

            // 帯の上端と砂塵の縁を揺らす値の雑音（0〜1）
            float PropNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = frac(sin(dot(i, float2(127.1, 311.7))) * 43758.5453);
                float b = frac(sin(dot(i + float2(1, 0), float2(127.1, 311.7))) * 43758.5453);
                float c = frac(sin(dot(i + float2(0, 1), float2(127.1, 311.7))) * 43758.5453);
                float d = frac(sin(dot(i + float2(1, 1), float2(127.1, 311.7))) * 43758.5453);
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 geometricNormal = normalize(input.normalWS);
                half3 albedo = RegionToLinear(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb) * _BaseColor.rgb;
                half3 tangentNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalStrength);
                half3 bitangent = input.tangentWS.w * cross(geometricNormal, input.tangentWS.xyz);
                half3 shade = normalize(mul(tangentNormal, half3x3(input.tangentWS.xyz, bitangent, geometricNormal)));
                half roughness = _UseRoughnessMap > 0.5 ? SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, input.uv).r : _Roughness;

                // V4: 個体の明るさと暖かさ（同じ材質の Props が並んでも同じ色に見えないように）
                half value = _PropParams.y > 0.0 ? _PropParams.y : 1.0;
                albedo *= value * half3(1.0 + 0.06 * _PropParams.z, 1.0, 1.0 - 0.08 * _PropParams.z);

                float2 xz = input.positionWS.xz;
                float n1 = PropNoise(xz * 2.3);
                float n2 = PropNoise(xz * 7.9 + 13.1);
                half3 ground = RegionDisplayToLinear(_PropGroundColor.rgb) * (0.86 + 0.26 * n2);

                // V7 Ground Skirt Blend: 足元の地面（傾いた平面）からの高さが帯の内なら、地面の色へつなぐ。上端は雑音で揺らす
                half skirt = 0.0;
                if (_PropGround.w > 1e-4)
                {
                    float3 pivot = TransformObjectToWorld(float3(0, 0, 0));
                    float groundY = _PropGround.x + _PropGround.y * (input.positionWS.x - pivot.x) + _PropGround.z * (input.positionWS.z - pivot.z);
                    float above = input.positionWS.y - groundY;
                    float edge = above / _PropGround.w + 0.45 * (n1 - 0.5) + 0.25 * (n2 - 0.5);
                    skirt = 0.88 * (1.0 - smoothstep(0.2, 1.0, edge));
                }
                albedo = lerp(albedo, ground, skirt);
                roughness = lerp(roughness, 1.0, skirt);

                // V7 TopDust: 上を向いた面に砂塵。斑は雑音で切る
                half dust = 0.0;
                if (_PropParams.x > 1e-4)
                {
                    half up = smoothstep(0.35, 0.85, shade.y);
                    dust = saturate(_PropParams.x * 1.6 * up * smoothstep(0.25, 0.75, 0.6 * n1 + 0.4 * n2 + 0.25 * _PropParams.x));
                }
                albedo = lerp(albedo, ground * 1.08, dust);
                roughness = lerp(roughness, 1.0, dust);
                shade = normalize(lerp(shade, geometricNormal, dust * 0.6));

                // V7 MossMask: 湿潤バイオームでの上面・北/陰側の苔堆積
                half moss = 0.0;
                if (_PropParams.w > 1e-4)
                {
                    half mossUp = smoothstep(0.15, 0.78, geometricNormal.y * 0.7 + shade.y * 0.3);
                    half mossNoise = smoothstep(0.28, 0.72, 0.55 * n1 + 0.45 * n2 + 0.20 * _PropParams.w);
                    moss = saturate(_PropParams.w * 1.15 * mossUp * mossNoise);
                }
                half3 mossColor = RegionDisplayToLinear(_PropMossColor.rgb) * (0.78 + 0.40 * n1);
                albedo = lerp(albedo, mossColor, moss);
                roughness = lerp(roughness, 0.92, moss);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light light = GetMainLight(shadowCoord);
                AmbientOcclusionFactor occlusion = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(input.positionCS));
                half terrainSun = RegionShadowsEnabled() ? RegionSunVisibility(input.positionWS, geometricNormal, 0.5) : 1.0;
                half sunlit = min(light.shadowAttenuation, terrainSun);
                half direct = saturate(dot(shade, light.direction)) * sunlit * occlusion.directAmbientOcclusion;
                half3 tint = dot(_RegionShadowTint.rgb, 1.0) > 0.0 ? lerp(_RegionShadowTint.rgb, 1.0, sunlit) : 1.0;
                half3 ambient = SampleSH(shade) * tint;
                half3 bounce = _RegionBounceColor.rgb * saturate(0.55 - 0.45 * shade.y) * (0.45 + 0.55 * (1.0 - sunlit));
                // 地面に接する所は空の光が届きにくい（帯の下ほど暗く）
                half contact = 1.0 - 0.35 * skirt * skirt;
                half3 diffuseColor = albedo * (1.0 - _Metallic);
                half3 color = diffuseColor * (light.color * direct + (ambient + bounce) * occlusion.indirectAmbientOcclusion * contact);

                // URP の Lit と同じ近似の GGX。乾いた素材なので反射率 0.04（金属は色）
                float3 viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
                float3 halfDirection = SafeNormalize(float3(light.direction) + viewDirection);
                float r = max(roughness * roughness, 0.0078125);
                float r2 = r * r;
                float nh = saturate(dot(float3(shade), halfDirection));
                float lh = saturate(dot(float3(light.direction), halfDirection));
                float d = nh * nh * (r2 - 1.0) + 1.00001;
                float specularTerm = r2 / ((d * d) * max(0.1, lh * lh) * (r * 4.0 + 2.0));
                half3 f0 = lerp(half3(0.04, 0.04, 0.04), albedo, _Metallic);
                color += light.color * direct * f0 * specularTerm;

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
