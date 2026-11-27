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
        [HideInInspector] _PropPhoto ("Rock projection metres, timber mapping", Vector) = (0,0,0,0)
        [HideInInspector] _PropTimberAxis ("Timber axis and photo strip", Vector) = (0,1,0,0.36)
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
                float4 _PropPhoto;
                float4 _PropTimberAxis;
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
                float3 fromPivot = input.positionWS - TransformObjectToWorld(float3(0, 0, 0));
                half3 albedo;
                half3 shade;
                half roughness;
                [branch]
                if (_PropPhoto.x > 0.0)
                {
                    float3 p = fromPivot / _PropPhoto.x + float3(0.17, 0.38, 0.61);
                    half3 w = pow(abs(geometricNormal), 4.0h);
                    w /= max(1e-3h, dot(w, 1.0h));
                    float2 x = p.zy, y = p.xz, z = p.xy;
                    half3 nx = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, x), 1.27h);
                    half3 ny = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, y), 1.27h);
                    half3 nz = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, z), 1.27h);
                    half3 relief = w.x * half3(0, nx.y, nx.x) + w.y * half3(ny.x, 0, ny.y) + w.z * half3(nz.x, nz.y, 0);
                    shade = normalize(geometricNormal + relief - geometricNormal * dot(geometricNormal, relief));
                    half3 stone = RegionToLinear(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, x).rgb) * w.x
                        + RegionToLinear(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, y).rgb) * w.y
                        + RegionToLinear(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, z).rgb) * w.z;
                    half luma = dot(stone, half3(0.2126h, 0.7152h, 0.0722h));
                    albedo = lerp(stone, luma * half3(1.02h, 1.03h, 0.96h), 0.74h) * _BaseColor.rgb;
                    roughness = saturate(0.21h + 0.82h * dot(w, half3(
                        SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, x).r,
                        SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, y).r,
                        SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, z).r)));
                }
                else
                {
                    float2 uv = input.uv;
                    half3 tangent = input.tangentWS.xyz;
                    half3 bitangent = input.tangentWS.w * cross(geometricNormal, tangent);
                    if (_PropPhoto.y > 0.5 && _PropPhoto.y < 1.5)
                    {
                        half3 axis = TransformObjectToWorldDir(_PropTimberAxis.xyz);
                        half3 onFace = axis - geometricNormal * dot(geometricNormal, axis);
                        if (dot(onFace, onFace) > 0.18h)
                        {
                            tangent = normalize(onFace);
                            bitangent = cross(geometricNormal, tangent);
                            // A narrow strip inside one board keeps photographed plank seams off a single beam.
                            uv = float2(dot(fromPivot, axis) / 1.9 + frac(_PropTimberAxis.w * 7.93),
                                _PropTimberAxis.w + dot(fromPivot, bitangent) * 0.14);
                        }
                    }
                    albedo = RegionToLinear(SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb) * _BaseColor.rgb;
                    half3 tangentNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv), _NormalStrength);
                    shade = normalize(mul(tangentNormal, half3x3(tangent, bitangent, geometricNormal)));
                    roughness = _UseRoughnessMap > 0.5 ? SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uv).r : _Roughness;
                    if (_PropPhoto.y > 0.5)
                    {
                        half luma = dot(albedo, half3(0.2126h, 0.7152h, 0.0722h));
                        albedo = lerp(albedo, luma * half3(1.09h, 1.01h, 0.89h), 0.76h);
                        roughness = lerp(0.67h, 0.94h, roughness);
                    }
                }

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
                    // The scanned lichen carries its own mineral and growth contrast.
                    if (_PropPhoto.x > 0.0) moss *= 0.30h;
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
