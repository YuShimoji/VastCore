Shader "Vastcore/RegionGrasslandCover"
{
    Properties
    {
        [HideInInspector] _GrasslandContinuous ("Continuous cover", Float) = 0
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
                float3 tussock : TEXCOORD1; // atlas UV and card flag
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
                half4 crown : TEXCOORD3; // radial lighting normal and card flag
                #endif
            };

            Varyings Vert(Attributes input)
            {
                float3 root = input.rootAndLayer.xyz;
                float3 offset = input.positionOS.xyz - root;
                float eyeDistance = distance(TransformObjectToWorld(root), _WorldSpaceCameraPos);
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
                band = layer > 2.5 ? float2(315.0, 463.0) : layer > 1.5 ? float2(13.0, 23.5)
                    : layer > 0.5 ? float2(16.0, 29.5) : float2(112.0, 176.0);
                reach = lerp(0.9, 1.0, input.plant.g);
                fade = 1.0h - smoothstep(band.x * reach, band.y * reach, eyeDistance);
                if (layer < 0.5) fade *= smoothstep(8.0, 24.0, eyeDistance);
                if (layer > 2.5) fade *= smoothstep(54.0, 119.0, eyeDistance);
                o.atlasAndFade = float4(input.tussock.xy, fade, eyeDistance);
                float2 radial = offset.xz / max(0.10, length(offset.xz) + max(0.0, offset.y) * 0.38);
                o.crown = half4(radial.x, 0.86h, radial.y, input.tussock.z);
                if (input.tussock.z > 0.5)
                {
                    // Leave the card's height intact as its silhouette thins at the LOD boundary.
                    float scale = fade > 0.004 ? lerp(0.70, 1.0, fade) : 0.0;
                    o.positionWS = TransformObjectToWorld(root + offset * scale);
                    o.positionCS = TransformWorldToHClip(o.positionWS);
                    o.leaf = half4(input.plant.rg, 0.0h, input.plant.b);
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
                half direct = (0.22h + 0.82h * saturate(dot(normal, sun.direction))) * lerp(0.68h, 1.12h, height);
                half3 ambient = SampleSH(half3(0, 1, 0)) + _RegionBounceColor.rgb * 0.23h;
                half3 color = albedo * (sun.color * sunlit * direct + ambient * lerp(0.58h, 1.03h, height));
                return GrasslandOutput(color, i.positionWS, alpha);
            }
            #endif

            half4 Frag(Varyings i) : SV_Target
            {
                #if defined(GRASSLAND_TUSSOCKS)
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
