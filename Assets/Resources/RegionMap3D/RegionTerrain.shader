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
        [HideInInspector] _SunFromVertex ("Keep the baked sun (overhangs)", Float) = 0
        [HideInInspector][NoScaleOffset] _SedimentMap ("Sediment maps (deposit, wash, scree, coarse)", 2D) = "black" {}
        [HideInInspector] _SedimentRect ("Sediment map placement", Vector) = (0, 0, 0, 0)
        [HideInInspector] _GrasslandSurface ("Grassland coordinates", Vector) = (0, 0, 0, 0)
        [HideInInspector] _GrasslandTrail ("Sample trail anchor", Vector) = (0, 0, 0, 0)
        [HideInInspector][NoScaleOffset] _WoodlandTrailRows ("Woodland trail footprint", 2D) = "black" {}
        [HideInInspector] _WoodlandTrail ("Woodland trail coordinates", Vector) = (0, 0, 0, 0)
        [HideInInspector] _RegionBiomeParams ("Biome surface settings", Vector) = (0, 0, 0, 0)
        [HideInInspector] _RegionBiomeTile ("Biome surface scales", Vector) = (0.4, 0.45, 0.36, 0.88)
        [HideInInspector] _RegionBiomeCliffColor ("Biome cliff tint", Vector) = (0, 0, 0, 0)
        [HideInInspector] _RegionBiomeGroundBlend ("Biome ground blend", Vector) = (0, 0, 0, 0)
        [HideInInspector] _CoastalRockDetail ("Coastal exposed rock and wet ground", Float) = 0
        [HideInInspector] _CoastalGroundTone ("Coastal sediment tone", Vector) = (0.43, 0.47, 0.50, 0)
        [HideInInspector] _CoastalShelfDetail ("Coastal flat rock projection", Float) = 0
        [HideInInspector][NoScaleOffset] _BiomeGroundMap ("Biome turf", 2D) = "white" {}
        [HideInInspector][NoScaleOffset] _BiomeGroundNormal ("Biome turf normals", 2D) = "bump" {}
        [HideInInspector][NoScaleOffset] _BiomeGroundRoughness ("Biome turf roughness", 2D) = "white" {}
        [HideInInspector][NoScaleOffset] _BiomeSubsoilMap ("Biome soil", 2D) = "white" {}
        [HideInInspector][NoScaleOffset] _BiomeSubsoilNormal ("Biome soil normals", 2D) = "bump" {}
        [HideInInspector][NoScaleOffset] _BiomeSubsoilRoughness ("Biome soil roughness", 2D) = "white" {}
        [HideInInspector][NoScaleOffset] _BiomeCliffMap ("Biome cliff", 2D) = "white" {}
        [HideInInspector][NoScaleOffset] _BiomeCliffNormal ("Biome cliff normals", 2D) = "bump" {}
        [HideInInspector][NoScaleOffset] _BiomeCliffRoughness ("Biome cliff roughness", 2D) = "white" {}
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
            #pragma multi_compile_local_fragment _ REGION_BIOME_SURFACE
            #pragma multi_compile_local_fragment _ REGION_WOODLAND_TRAIL
            #pragma require integers : REGION_BIOME_SURFACE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "RegionAtmosphere.hlsl"
            #include "RegionShadows.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _RockColor;
                half _RockStart;
                half _RockFull;
                half _BandAmount;
                half _LooseRock;
                half _SunFromVertex;
                float4 _SedimentRect;   // 土砂の地図の置き場所（RegionSedimentLayers）。z が 0 なら地図なし
                float4 _GrasslandSurface;
                float4 _GrasslandTrail;
                float4 _WoodlandTrail;
                float4 _RegionBiomeParams;
                float4 _RegionBiomeTile;
                float4 _RegionBiomeCliffColor;
                float4 _RegionBiomeGroundBlend;
                half _CoastalRockDetail;
                half4 _CoastalGroundTone;
                half _CoastalShelfDetail;
            CBUFFER_END

            #if defined(REGION_SANDSTONE)
            #define REGION_SEDIMENT_MATERIAL
            #include "RegionSandstone.hlsl"
            #endif
            #if defined(REGION_BIOME_SURFACE)
            #include "RegionBiomeSurface.hlsl"
            #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 surface : TEXCOORD1;
                float4 biomeSurface : TEXCOORD2;
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
                float4 biomeSurface : TEXCOORD5;
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
                output.biomeSurface = input.biomeSurface;
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
                albedo = RegionToLinear(lerp(albedo, _RockColor.rgb * band, rock));

                // 光を受ける向き。凹凸が無い時は形の法線のまま（影の計算と岩肌の判定は常に形の法線）
                half3 shade = normal;
                half surfaceOcclusion = 1.0;
                half specularWeight = 0.0;
                half roughness = 1.0;
                #if defined(REGION_SANDSTONE)
                float viewDistance = distance(input.positionWS, _WorldSpaceCameraPos);
                float3 positionOS = TransformWorldToObject(input.positionWS);
                float3 normalOS = normalize(TransformWorldToObjectNormal(normal));
                // 砂岩の色はシェーダーに書いた見た目の値なので、色空間によらず線形にする
                SandstoneExtra extra;
                half3 sandstone = RegionDisplayToLinear(SandstoneSurface(positionOS, normalOS, input.surface, input.color.a, _LooseRock, viewDistance, extra));
                half sandstoneWeight = max(input.surface.w, _LooseRock);
                albedo = lerp(albedo, sandstone, sandstoneWeight);
                // 写真素材の法線、地層の起伏と岩肌の細かな凹凸（RegionSurfaceRelief が焼いた傾き）で、光を受ける向きを傾ける
                if (RegionReliefEnabled() || RegionSurfaceEnabled())
                {
                    float3 bumped = TransformObjectToWorldNormal(SandstoneReliefNormal(positionOS, normalOS, extra, _LooseRock, viewDistance));
                    shade = normalize(lerp(normal, bumped, sandstoneWeight));
                }
                surfaceOcclusion = lerp(1.0, extra.occlusion, sandstoneWeight);
                // 写真素材のある所だけ、粗さから弱い鏡面を足す（素材が無い所と遠くは今の描き方のまま）
                specularWeight = extra.specular * sandstoneWeight * _RegionSurfaceParams.y;
                roughness = extra.specular > 0.0 ? extra.roughness / extra.specular : 1.0;
                #endif
                #if defined(REGION_BIOME_SURFACE)
                float biomeWeight = input.biomeSurface.w;
                #if defined(REGION_SANDSTONE)
                biomeWeight *= (1.0 - sandstoneWeight);
                #endif
                if (biomeWeight > 0.001)
                {
                    float biomeDist = distance(input.positionWS, _WorldSpaceCameraPos);
                    BiomeSurfaceExtra bExtra;
                    half coastalRock = _CoastalRockDetail > 0.5h ? max(rock, input.biomeSurface.x) : rock;
                    half3 biomeAlbedo = EvaluateBiomeSurface(
                        input.positionWS, normal, coastalRock, albedo, input.biomeSurface, input.color.a, biomeDist, bExtra);
                    if (_CoastalRockDetail > 0.5h)
                    {
                        half wet = saturate(input.biomeSurface.y);
                        float3 p = input.positionWS;
                        half weathering = BiomeNoise(float2(p.x + 0.39 * p.z, p.y - 0.23 * p.z) * 0.16);
                        half3 rockTone = lerp(half3(0.55h, 0.62h, 0.71h), half3(0.89h, 0.83h, 0.76h), weathering);
                        rockTone *= lerp(1.0h, 0.51h, _CoastalShelfDetail);
                        biomeAlbedo *= lerp(_CoastalGroundTone.rgb, rockTone, coastalRock);
                        biomeAlbedo *= lerp(1.0h, half3(0.46h, 0.53h, 0.61h), wet);
                        half shelfWeight = _CoastalShelfDetail * coastalRock * smoothstep(0.37h, 0.85h, normal.y)
                            * (1.0h - smoothstep(35.0, 92.0, biomeDist));
                        if (shelfWeight > 0.01h)
                        {
                            // Along-bed joints run through the shelf; shorter fractures interrupt neighboring slabs.
                            float2 bed = float2(p.x + 0.47 * p.z + 0.31 * p.y, p.z - 0.35 * p.x);
                            half chip = BiomeNoise(bed * 0.49 + 6.2);
                            float bedJoint = bed.x * 0.35 + 0.75 * weathering + 0.21 * chip;
                            float bedId = floor(bedJoint + 0.5);
                            half offset = BiomeNoise(float2(0.71 * bedId, 7.2));
                            float fracture = bed.y * (0.32 + 0.25 * offset) + 2.8 * offset + 0.66 * chip;
                            float2 seam = abs(frac(float2(bedJoint, fracture)) - 0.5);
                            half jointExposure = smoothstep(0.10h, 0.62h, BiomeNoise(bed * float2(0.43, 0.22)));
                            half longJoint = (1.0h - smoothstep(0.007, 0.039 + 0.027 * chip, seam.x)) * jointExposure;
                            half shortJoint = (1.0h - smoothstep(0.008, 0.051, seam.y)) * smoothstep(0.41h, 0.76h, chip);
                            half joints = max(longJoint, shortJoint);
                            half patina = BiomeNoise(bed * 0.31 + 4.7);
                            half3 slabTone = lerp(half3(0.49h, 0.58h, 0.65h), half3(0.98h, 0.87h, 0.69h), patina);
                            biomeAlbedo *= lerp(1.0h, slabTone * lerp(1.0h, 0.56h, joints), shelfWeight);
                            bExtra.occlusion *= 1.0h - 0.23h * shelfWeight * joints;
                        }
                        bExtra.roughness = lerp(bExtra.roughness, 0.55h, wet);
                        bExtra.specular = lerp(bExtra.specular, 0.22h, wet);
                    }
                    albedo = lerp(albedo, biomeAlbedo, biomeWeight);
                    shade = normalize(lerp(shade, bExtra.normalWS, biomeWeight));
                    surfaceOcclusion *= lerp(1.0h, bExtra.occlusion, biomeWeight);
                    specularWeight = lerp(specularWeight, bExtra.specular, biomeWeight);
                    roughness = lerp(roughness, bExtra.roughness, biomeWeight);
                }
                #endif

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light light = GetMainLight(shadowCoord);
                AmbientOcclusionFactor occlusion = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(input.positionCS));
                // 影は、Unity の影（近くだけ届く）と、遠くまで届く地形の影の、暗い方。
                // 地形の影は GPU が高さの格子をたどって出す。格子が無い時と、張り出しのある所（_SunFromVertex）は頂点に持たせた値
                half terrainSun = input.color.g;
                if (RegionShadowsEnabled() && _SunFromVertex < 0.5)
                    terrainSun = RegionSunVisibility(input.positionWS, normal, _LooseRock > 0.5 ? 4.0 : 0.0);
                half sunlit = min(light.shadowAttenuation, terrainSun);
                half direct = saturate(dot(shade, light.direction)) * sunlit * occlusion.directAmbientOcclusion;
                // 岩の下では、空からの光も減らす。影の中の空の光は、空の青を強めに受ける
                half open = 0.35 + 0.65 * input.color.r;
                half3 tint = dot(_RegionShadowTint.rgb, 1.0) > 0.0 ? lerp(_RegionShadowTint.rgb, 1.0, sunlit) : 1.0;
                half3 ambient = SampleSH(shade) * open * tint;
                // 日の当たる地面と向かいの岩壁からの照り返し。下や横を向いた面、影の中ほど受ける
                half3 bounce = _RegionBounceColor.rgb * saturate(0.55 - 0.45 * shade.y) * (0.45 + 0.55 * (1.0 - sunlit)) * (0.5 + 0.5 * open);
                // 写真素材の AO は、空の光と照り返しだけを弱める（日の光は形の影が受け持つ）
                half3 color = albedo * (light.color * direct + (ambient + bounce) * occlusion.indirectAmbientOcclusion * surfaceOcclusion);
                #if defined(REGION_SANDSTONE) || defined(REGION_BIOME_SURFACE)
                [branch]
                if (specularWeight > 0.001)
                {
                    // URP の Lit と同じ近似の GGX（反射率 0.04）。乾いた砂岩・草地なので強さを絞る（設計 3.3 節）
                    float3 viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
                    float3 halfDirection = SafeNormalize(float3(light.direction) + viewDirection);
                    float r = max(roughness * roughness, 0.0078125);
                    float r2 = r * r;
                    float nh = saturate(dot(float3(shade), halfDirection));
                    float lh = saturate(dot(float3(light.direction), halfDirection));
                    float d = nh * nh * (r2 - 1.0) + 1.00001;
                    float specularTerm = r2 / ((d * d) * max(0.1, lh * lh) * (r * 4.0 + 2.0));
                    color += light.color * direct * (0.04 * specularTerm * specularWeight);
                }
                #endif
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
