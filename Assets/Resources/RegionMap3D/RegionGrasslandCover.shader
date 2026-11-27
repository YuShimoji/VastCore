Shader "Vastcore/RegionGrasslandCover"
{
    Properties
    {
        [HideInInspector] _GrasslandContinuous ("Continuous cover", Float) = 0
        [HideInInspector] _GrasslandColonies ("Colony canopy", Float) = 0
        [HideInInspector] _GrasslandOrigin ("Geographic origin", Vector) = (0, 0, 0, 0)
        [HideInInspector] _GrasslandTrail ("Trail origin and presence", Vector) = (0, 0, 0, 0)
        [HideInInspector] _GrasslandAlphaToMask ("Alpha to coverage", Float) = 0
        [NoScaleOffset] _MeadowTussocks ("Meadow tussocks", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            AlphaToMask [_GrasslandAlphaToMask]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_local _ GRASSLAND_TUSSOCKS
            #pragma multi_compile_local_fragment _ GRASSLAND_COVERAGE_MASK
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "RegionAtmosphere.hlsl"
            #include "RegionGrasslandHabitat.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _GrasslandContinuous;
                float _GrasslandColonies;
                float4 _GrasslandOrigin;
                float4 _GrasslandTrail;
            CBUFFER_END
            #if defined(GRASSLAND_TUSSOCKS)
            TEXTURE2D(_MeadowTussocks);
            SAMPLER(sampler_MeadowTussocks);
            #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 rootAndLayer : TEXCOORD0;
                #if defined(GRASSLAND_TUSSOCKS)
                float3 normalOS : NORMAL;
                float3 tussock : TEXCOORD1; // atlas UV and geometry kind: blades, side, crown, or canopy
                #endif
                half4 plant : COLOR; // growth, variation, terrain sun, dry stem
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half4 leaf : TEXCOORD1;
                #if defined(GRASSLAND_TUSSOCKS)
                float4 atlasAndFade : TEXCOORD2;
                half4 crown : TEXCOORD3; // lighting normal and geometry kind
                #endif
            };

            #if defined(GRASSLAND_TUSSOCKS)
            float2 GrasslandMetres(float3 positionWS)
            {
                return positionWS.xz * float2(1.0, -1.0) + _GrasslandOrigin.xy;
            }

            Varyings VertColony(Attributes input, float3 rootWS, float eyeDistance)
            {
                float2 metres = GrasslandMetres(rootWS);
                float3 groundNormal = TransformObjectToWorldNormal(input.normalOS);
                float3 stand = GrasslandColony(metres, rootWS.y, GrasslandTerrainSlope(groundNormal), input.plant.r);
                float reach = GrasslandColonyAmount(eyeDistance) * (1.0 - smoothstep(317.0, 458.0, eyeDistance));
                float land = input.plant.g;
                if (_GrasslandTrail.z > 0.5)
                    land *= 1.0 - GrasslandTrailWear(metres - _GrasslandTrail.xy);
                float lift = 0.49 * GrasslandColonyHeight(stand.x) * reach * land;
                Varyings o;
                o.positionWS = rootWS + float3(0, lift - 0.025, 0);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.atlasAndFade = float4(metres, reach * land, eyeDistance);
                o.crown = half4(GrasslandColonyNormal(groundNormal, stand, 0.65 * reach * land), input.tussock.z);
                o.leaf = half4(stand.x, 0.0h, 0.0h, input.plant.b);
                return o;
            }
            #endif

            Varyings Vert(Attributes input)
            {
                float3 root = input.rootAndLayer.xyz;
                float3 offset = input.positionOS.xyz - root;
                float3 rootWS = TransformObjectToWorld(root);
                float eyeDistance = distance(rootWS, _WorldSpaceCameraPos);
                float layer = input.rootAndLayer.w;
                float2 band = layer > 1.5 ? float2(8.0, 16.0) : layer > 0.5 ? float2(18.0, 33.0) : float2(38.0, 86.0);
                float reach = lerp(0.74, 1.0, input.plant.g);
                if (_GrasslandContinuous > 0.5)
                {
                    band = layer > 2.5 ? float2(155.0, 301.0) : layer > 1.5 ? float2(13.0, 22.0)
                        : layer > 0.5 ? float2(23.0, 42.0) : float2(67.0, 124.0);
                    reach = lerp(0.84, 1.0, input.plant.g);
                }
                half fade = 1.0h - smoothstep(band.x * reach, band.y * reach, eyeDistance);
                // Close to the camera the fine leaves carry the canopy, while the wider silhouettes take over farther up the slope.
                if (layer < 0.5) fade *= lerp(0.53h, 1.0h, smoothstep(8.0, 24.0, eyeDistance));
                if (layer > 2.5) fade *= smoothstep(40.0, 94.0, eyeDistance);
                Varyings o;
                #if defined(GRASSLAND_TUSSOCKS)
                if (input.tussock.z > 2.5) return VertColony(input, rootWS, eyeDistance);
                band = layer > 2.5 ? float2(315.0, 463.0) : layer > 1.5 ? float2(13.0, 23.5)
                    : layer > 0.5 ? float2(16.0, 29.5) : float2(112.0, 176.0);
                bool distantTips = _GrasslandColonies > 0.5 && layer > 2.5;
                bool colonyTips = _GrasslandColonies > 0.5 && (layer < 0.5 || distantTips);
                if (colonyTips && layer < 0.5) band = float2(62.0, 142.0);
                reach = lerp(0.9, 1.0, input.plant.g);
                fade = 1.0h - smoothstep(band.x * reach, band.y * reach, eyeDistance);
                if (layer < 0.5) fade *= smoothstep(8.0, 24.0, eyeDistance);
                if (layer > 2.5) fade *= distantTips ? smoothstep(38.0, 108.0, eyeDistance)
                    : smoothstep(54.0, 119.0, eyeDistance);
                half amount = colonyTips ? smoothstep(28.0, 61.0, eyeDistance) : 0.0h;
                half shadeGrowth = input.plant.r;
                float3 groupNormal = float3(0, 1, 0);
                [branch]
                if (amount > 0.0h)
                {
                    float3 groundNormal = distantTips ? TransformObjectToWorldNormal(input.normalOS) : float3(0, 1, 0);
                    float2 slope = GrasslandTerrainSlope(groundNormal);
                    float3 stand = GrasslandColony(GrasslandMetres(rootWS), rootWS.y, slope, input.plant.a);
                    half growth = stand.x;
                    shadeGrowth = growth;
                    // Tips remain more numerous and stand taller in the thick part of the same colony.
                    fade *= lerp(1.0h, lerp(distantTips ? 0.72h : 0.52h, 1.0h, growth), amount);
                    float2 groundOffset = offset.xz * float2(1.0, -1.0);
                    float rootPlane = dot(slope, groundOffset);
                    float tipsHeight = distantTips ? lerp(0.54, 1.68, growth) : lerp(0.79, 1.18, growth);
                    offset.y = rootPlane + (offset.y - rootPlane) * lerp(1.0, tipsHeight, amount);
                    if (distantTips)
                    {
                        // Continue the canopy's local shade across the wide card while its height stays fixed at the root.
                        shadeGrowth = saturate(growth + clamp(dot(stand.yz, groundOffset), -0.16, 0.16));
                        groupNormal = GrasslandColonyNormal(groundNormal, stand, 0.65);
                    }
                }
                o.atlasAndFade = float4(input.tussock.xy, fade, eyeDistance);
                float2 radial = offset.xz / max(0.10, length(offset.xz) + max(0.0, offset.y) * 0.38);
                half3 cardNormal = half3(radial.x, 0.86h, radial.y);
                o.crown = half4(lerp(cardNormal, groupNormal, amount * (distantTips ? 0.88h : 0.44h)), input.tussock.z);
                if (input.tussock.z > 0.5)
                {
                    // Leave the card's height intact as its silhouette thins at the LOD boundary.
                    float scale = fade > 0.004 ? lerp(0.70, 1.0, fade) : 0.0;
                    o.positionWS = TransformObjectToWorld(root + offset * scale);
                    o.positionCS = TransformWorldToHClip(o.positionWS);
                    o.leaf = half4(lerp(input.plant.r, shadeGrowth, amount), input.plant.g, amount, input.plant.b);
                    return o;
                }
                #endif
                o.positionWS = TransformObjectToWorld(root + offset * fade);
                o.positionCS = TransformWorldToHClip(o.positionWS);

                half3 leaf = lerp(half3(0.44h, 0.43h, 0.31h), half3(0.30h, 0.39h, 0.23h), input.plant.r);
                leaf *= lerp(0.74h, 1.20h, input.plant.g);
                leaf = lerp(leaf, half3(0.57h, 0.51h, 0.34h), input.plant.a);
                // The leaves shade their bases. The clustered shadow is visible even at a low camera angle.
                leaf *= lerp(0.59h, 1.03h, smoothstep(0.0h, layer > 1.5 ? 0.12h : 0.22h, offset.y));
                half3 distant = lerp(half3(0.41h, 0.42h, 0.31h), half3(0.32h, 0.38h, 0.25h), input.plant.r);
                leaf = lerp(leaf, distant, smoothstep(25.0, 79.0, eyeDistance));
                if (_GrasslandContinuous > 0.5)
                {
                    half3 crown = GrasslandCrownColor(input.plant.r);
                    #if defined(GRASSLAND_TUSSOCKS)
                    crown = lerp(half3(0.47h, 0.45h, 0.29h), half3(0.29h, 0.40h, 0.22h), input.plant.r);
                    #endif
                    half3 dryStem = half3(0.57h, 0.50h, 0.32h);
                    bool basal = layer > 1.5 && layer < 2.5;
                    half shade = smoothstep(0.0h, basal ? 0.14h : 0.33h, offset.y);
                    half3 tips = lerp(crown, dryStem, input.plant.a) * lerp(0.76h, 1.19h, input.plant.g);
                    leaf = tips * lerp(0.48h, 1.08h, shade);
                    if (basal) leaf *= half3(0.91h, 0.85h, 0.78h);
                    leaf = lerp(leaf, crown * (0.91h + 0.14h * input.plant.g), smoothstep(54.0, 205.0, eyeDistance));
                }
                o.leaf = half4(RegionDisplayToLinear(leaf), input.plant.b);
                return o;
            }

            half4 GrasslandOutput(half3 color, float3 positionWS, half alpha)
            {
                #if defined(GRASSLAND_COVERAGE_MASK)
                return half4(1.0h, 0.0h, 1.0h, alpha);
                #else
                return RegionOutput(RegionApplyAir(color, positionWS), alpha);
                #endif
            }

            #if defined(GRASSLAND_TUSSOCKS)
            half4 ShadeColony(Varyings i)
            {
                float2 metres = i.atlasAndFade.xy;
                half growth = i.leaf.r;
                half3 albedo = RegionDisplayToLinear(GrasslandColonyColor(growth)) * GrasslandColonyGrain(metres, growth) * 0.90h;
                half coverage = i.atlasAndFade.z;
                if (_GrasslandTrail.z > 0.5)
                    coverage *= 1.0h - GrasslandTrailWear(metres - _GrasslandTrail.xy);
                // Fade with stable holes in the cover so the cutoff never draws a ring around the viewer.
                half edge = lerp(0.08h, 0.88h, GrasslandNoise(metres * 0.79 + float2(-7.0, 11.0)));
                clip(coverage - edge);
                half alpha = saturate(0.52h + (coverage - edge) * 6.0h);
                Light sun = GetMainLight();
                [branch]
                if (i.atlasAndFade.w < 72.0)
                    sun.shadowAttenuation = MainLightRealtimeShadow(TransformWorldToShadowCoord(i.positionWS));
                half3 normal = normalize(i.crown.xyz);
                half sunlit = min(sun.shadowAttenuation, i.leaf.a);
                half direct = GrasslandColonyDiffuse(saturate(dot(normal, sun.direction)),
                    GrasslandColonyTransmission(growth, sun.direction.y));
                half3 ambient = SampleSH(normal) + _RegionBounceColor.rgb * 0.16h;
                half3 color = albedo * (sun.color * sunlit * direct + ambient * GrasslandColonyOcclusion(growth));
                return GrasslandOutput(color, i.positionWS, alpha);
            }

            half4 ShadeTussock(Varyings i)
            {
                half4 photo = SAMPLE_TEXTURE2D(_MeadowTussocks, sampler_MeadowTussocks, i.atlasAndFade.xy);
                half cutoff = lerp(1.0h, 0.34h, i.atlasAndFade.z);
                half edge = max(fwidth(photo.a), 0.13h);
                clip(photo.a - cutoff);
                half alpha = saturate(0.35h + (photo.a - cutoff) / edge);
                Light sun = GetMainLight();
                [branch]
                if (i.atlasAndFade.w < 55.0)
                    sun.shadowAttenuation = MainLightRealtimeShadow(TransformWorldToShadowCoord(i.positionWS));
                half3 albedo = RegionToLinear(photo.rgb);
                half luma = dot(albedo, half3(0.2126h, 0.7152h, 0.0722h));
                half3 tint = lerp(half3(1.12h, 0.97h, 0.63h), half3(0.84h, 1.07h, 0.64h), i.leaf.r);
                albedo = lerp(albedo, luma * tint, 0.40h) * lerp(1.03h, 1.32h, i.leaf.g);
                float2 frameUv = frac(i.atlasAndFade.xy * 4.0);
                half height = i.crown.w > 1.5 ? saturate(1.31 - 2.1 * length(frameUv - 0.5))
                    : saturate((frameUv.y - 0.11) * 2.1);
                half3 normal = normalize(i.crown.xyz);
                half sunlit = min(sun.shadowAttenuation, i.leaf.a);
                half facing = saturate(dot(normal, sun.direction));
                half direct = (0.22h + 0.82h * facing) * lerp(0.68h, 1.12h, height);
                half3 ambientNormal = half3(0, 1, 0);
                half bounce = 0.23h;
                half occlusion = lerp(0.58h, 1.03h, height);
                if (i.leaf.b > 0.0h)
                {
                    half growth = i.leaf.r;
                    // Keep a little contrast on projected leaves as their lighting approaches the lower canopy.
                    half blend = 0.82h * i.leaf.b * smoothstep(56.0, 136.0, i.atlasAndFade.w);
                    half detail = lerp(lerp(0.72h, 1.15h, height), 0.90h * lerp(0.92h, 1.16h, height), blend);
                    half3 colony = RegionDisplayToLinear(GrasslandColonyColor(growth)) * detail;
                    albedo = lerp(albedo, colony, lerp(0.84h * i.leaf.b, 1.0h, blend));

                    half filtered = GrasslandColonyTransmission(growth, sun.direction.y);
                    half exposedTips = 0.67h * smoothstep(0.40h, 0.98h, height);
                    direct *= lerp(1.0h, lerp(filtered, 1.0h, exposedTips), i.leaf.b);
                    // Keep the silhouette and let subpixel tips settle into the same light as the lower canopy.
                    direct = lerp(direct, GrasslandColonyDiffuse(facing, filtered), blend);
                    ambientNormal = normalize(lerp(ambientNormal, normal, blend));
                    bounce = lerp(bounce, 0.16h, blend);
                    occlusion = lerp(occlusion, GrasslandColonyOcclusion(growth), blend);
                }
                half3 ambient = SampleSH(ambientNormal) + _RegionBounceColor.rgb * bounce;
                half3 color = albedo * (sun.color * sunlit * direct + ambient * occlusion);
                return GrasslandOutput(color, i.positionWS, alpha);
            }
            #endif

            half4 Frag(Varyings i) : SV_Target
            {
                #if defined(GRASSLAND_TUSSOCKS)
                [branch]
                if (i.crown.w > 2.5) return ShadeColony(i);
                [branch]
                if (i.crown.w > 0.5) return ShadeTussock(i);
                #endif
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half sunlit = min(sun.shadowAttenuation, i.leaf.a);
                half direct = 0.29h + 0.60h * saturate(sun.direction.y);
                if (_GrasslandContinuous > 0.5)
                {
                    // Light both sides of the curved ribbons, with the diffuse response of leaves rather than a flat tint.
                    float3 face = cross(ddx(i.positionWS), ddy(i.positionWS));
                    half facing = saturate(abs(dot(face, sun.direction)) * rsqrt(max(1e-12, dot(face, face))));
                    direct *= lerp(0.54h, 1.10h, facing);
                }
                half transmission = 0.22h * pow(saturate(dot(normalize(i.positionWS - _WorldSpaceCameraPos), sun.direction)), 3.0h);
                half3 ambient = SampleSH(half3(0, 1, 0)) + _RegionBounceColor.rgb * 0.22h;
                half3 color = i.leaf.rgb * (sun.color * sunlit * (direct + transmission) + ambient);
                return GrasslandOutput(color, i.positionWS, 1.0h);
            }
            ENDHLSL
        }
    }
}
